﻿// Copyright (c) 2014 AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using ICSharpCode.Core;
using ICSharpCode.NRefactory;
using ICSharpCode.NRefactory.Al;
using ICSharpCode.NRefactory.Al.Completion;
using ICSharpCode.NRefactory.Al.TypeSystem;
using ICSharpCode.NRefactory.Editor;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.SharpDevelop;
using ICSharpCode.SharpDevelop.Editor;
using AlBinding.Parser;

namespace AlBinding.Completion
{
	sealed class AlCompletionContext
	{
		public readonly ITextEditor Editor;
		public readonly IDocument Document;
		public readonly IList<string> ConditionalSymbols;
		public readonly ICompilation Compilation;
		public readonly IProjectContent ProjectContent;
		public readonly AlTypeResolveContext TypeResolveContextAtCaret;
		public readonly ICompletionContextProvider CompletionContextProvider;
		
		public static AlCompletionContext Get(ITextEditor editor)
		{
			// Don't require the very latest parse information, an older cached version is OK.
			var parseInfo = SD.ParserService.GetCachedParseInformation(editor.FileName) as AlFullParseInformation;
			if (parseInfo == null) {
				parseInfo = SD.ParserService.Parse(editor.FileName, editor.Document) as AlFullParseInformation;
			}
			if (parseInfo == null)
				return null;
			
			ICompilation compilation = SD.ParserService.GetCompilationForFile(editor.FileName);
			var projectContent = compilation.MainAssembly.UnresolvedAssembly as IProjectContent;
			if (projectContent == null)
				return null;
			
			return new AlCompletionContext(editor, parseInfo.SyntaxTree.ConditionalSymbols, compilation, projectContent, editor.Document, parseInfo.UnresolvedFile, editor.Caret.Location);
		}
		
		public static AlCompletionContext Get(ITextEditor editor, ICodeContext context, TextLocation currentLocation, ITextSource fileContent)
		{
			IDocument document = new ReadOnlyDocument(fileContent);
			
			var projectContent = context.Compilation.MainAssembly.UnresolvedAssembly as IProjectContent;
			if (projectContent == null)
				return null;
			
			AlParser parser = new AlParser();
			parser.GenerateTypeSystemMode = false;
			
			SyntaxTree cu = parser.Parse(fileContent, Path.GetRandomFileName() + ".al");
			cu.Freeze();
			
			AlUnresolvedFile unresolvedFile = cu.ToTypeSystem();
			ICompilation compilation = projectContent.AddOrUpdateFiles(unresolvedFile).CreateCompilation(SD.ParserService.GetCurrentSolutionSnapshot());
			
			return new AlCompletionContext(editor, EmptyList<string>.Instance, compilation, projectContent, document, unresolvedFile, currentLocation);
		}
		
		private AlCompletionContext(ITextEditor editor, IList<string> conditionalSymbols, ICompilation compilation, IProjectContent projectContent, IDocument document, AlUnresolvedFile unresolvedFile, TextLocation caretLocation)
		{
			Debug.Assert(editor != null);
			Debug.Assert(unresolvedFile != null);
			Debug.Assert(compilation != null);
			Debug.Assert(projectContent != null);
			Debug.Assert(document != null);
			this.Editor = editor;
			this.Document = document;
			this.ConditionalSymbols = conditionalSymbols;
			this.Compilation = compilation;
			this.ProjectContent = projectContent;
			this.TypeResolveContextAtCaret = unresolvedFile.GetTypeResolveContext(compilation, caretLocation);
			this.CompletionContextProvider = new DefaultCompletionContextProvider(document, unresolvedFile);
			this.CompletionContextProvider.ConditionalSymbols.AddRange(conditionalSymbols);
		}
	}
}
