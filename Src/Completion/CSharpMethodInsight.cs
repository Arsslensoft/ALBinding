// Copyright (c) 2014 AlphaSierraPapa for the SharpDevelop Team
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
using System.Linq;

using ICSharpCode.NRefactory.Al.Resolver;
using ICSharpCode.NRefactory.Completion;
using ICSharpCode.NRefactory.Al.Completion;
using ICSharpCode.SharpDevelop;
using ICSharpCode.SharpDevelop.Editor;
using ICSharpCode.SharpDevelop.Editor.CodeCompletion;

namespace AlBinding.Completion
{
	sealed class AlMethodInsight : IParameterDataProvider
	{
		readonly int startOffset;
		internal readonly IReadOnlyList<AlInsightItem> items;
		readonly ITextEditor editor;
		IInsightWindow window;
		AlInsightItem initiallySelectedItem;
		
		public AlMethodInsight(ITextEditor editor, int startOffset, IEnumerable<AlInsightItem> items)
		{
			Debug.Assert(editor != null);
			Debug.Assert(items != null);
			this.editor = editor;
			this.startOffset = startOffset;
			this.items = items.ToList();
		}
		
		public void Show()
		{
			window = editor.ShowInsightWindow(items);
			// Set startOffset so that window always appears below the caret line
			var startLocation = editor.Document.GetLocation(startOffset);
			window.StartOffset = editor.Document.GetOffset(editor.Caret.Line, startLocation.Column);
			// closing the window at the end of the parameter list is handled by the CaretPositionChanged event
			window.EndOffset = editor.Document.TextLength;
			if (initiallySelectedItem != null)
				window.SelectedItem = initiallySelectedItem;
			window.CaretPositionChanged += window_CaretPositionChanged;
		}
		
		void window_CaretPositionChanged(object sender, EventArgs e)
		{
			var completionContext = AlCompletionContext.Get(editor);
			if (completionContext == null) {
				window.Close();
				return;
			}
			var completionFactory = new AlCompletionDataFactory(completionContext, new AlResolver(completionContext.TypeResolveContextAtCaret));
			var pce = new AlParameterCompletionEngine(
				editor.Document,
				completionContext.CompletionContextProvider,
				completionFactory,
				completionContext.ProjectContent,
				completionContext.TypeResolveContextAtCaret
			);
			UpdateHighlightedParameter(pce);
		}
		
		public void UpdateHighlightedParameter(AlParameterCompletionEngine pce)
		{
			int parameterIndex = pce.GetCurrentParameterIndex(window != null ? window.StartOffset : startOffset, editor.Caret.Offset);
			if (parameterIndex < 0 && window != null) {
				window.Close();
			} else {
				if (window == null || parameterIndex > ((AlInsightItem)window.SelectedItem).Method.Parameters.Count) {
					var newItem = items.FirstOrDefault(i => parameterIndex <= i.Method.Parameters.Count);
					if (newItem != null) {
						if (window != null)
							window.SelectedItem = newItem;
						else
							initiallySelectedItem = newItem;
					}
				}
				if (parameterIndex > 0)
					parameterIndex--; // NR returns 1-based parameter index
				foreach (var item in items)
					item.HighlightParameter(parameterIndex);
			}
		}
		
		#region IParameterDataProvider implementation
		int IParameterDataProvider.Count {
			get { return items.Count; }
		}
		
		int IParameterDataProvider.StartOffset {
			get { return startOffset; }
		}
		
		string IParameterDataProvider.GetHeading(int overload, string[] parameterDescription, int currentParameter)
		{
			throw new NotImplementedException();
		}
		
		string IParameterDataProvider.GetDescription(int overload, int currentParameter)
		{
			throw new NotImplementedException();
		}
		
		string IParameterDataProvider.GetParameterDescription(int overload, int paramIndex)
		{
			throw new NotImplementedException();
		}
		
		string IParameterDataProvider.GetParameterName(int overload, int currentParameter)
		{
			throw new NotImplementedException();
		}
		
		int IParameterDataProvider.GetParameterCount(int overload)
		{
			throw new NotImplementedException();
		}
		
		bool IParameterDataProvider.AllowParameterList(int overload)
		{
			throw new NotImplementedException();
		}
		#endregion
	}
}
