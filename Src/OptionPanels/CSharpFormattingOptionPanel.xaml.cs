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
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using ICSharpCode.Core;
using ICSharpCode.NRefactory.Al;
using ICSharpCode.SharpDevelop;
using ICSharpCode.SharpDevelop.Gui;
using AlBinding.FormattingStrategy;

namespace AlBinding.OptionPanels
{
	/// <summary>
	/// Option panel for global formatting settings.
	/// </summary>
	internal class AlGlobalFormattingOptionPanel : AlFormattingOptionPanel
	{
		public AlGlobalFormattingOptionPanel()
			: base(AlFormattingPolicies.Instance.GlobalOptions, true, false)
		{
			autoFormattingCheckBox.Visibility = Visibility.Visible;
		}
	}
	
	/// <summary>
	/// Option panel for solution-wide formatting settings.
	/// </summary>
	internal class AlSolutionFormattingOptionPanel : AlFormattingOptionPanel
	{
		public AlSolutionFormattingOptionPanel()
			: base(AlFormattingPolicies.Instance.SolutionOptions, true, true)
		{
			autoFormattingCheckBox.Visibility = Visibility.Collapsed;
		}
	}
	
	/// <summary>
	/// Interaction logic for AlFormattingOptionPanel.xaml
	/// </summary>
	internal partial class AlFormattingOptionPanel : OptionPanel
	{
		readonly AlFormattingPolicy formattingPolicy;
		bool isDirty;
		
		public AlFormattingOptionPanel(AlFormattingPolicy persistenceHelper, bool allowPresets, bool overrideGlobalIndentation)
		{
			if (persistenceHelper == null)
				throw new ArgumentNullException("persistenceHelper");
			
			this.formattingPolicy = persistenceHelper;
			this.isDirty = false;
			InitializeComponent();			
			
			formattingEditor.AllowPresets = allowPresets;
			formattingEditor.OverrideGlobalIndentation = overrideGlobalIndentation;
		}
		
		public override void LoadOptions()
		{
			base.LoadOptions();
			formattingEditor.OptionsContainer = formattingPolicy.StartEditing();
			formattingEditor.OptionsContainer.PropertyChanged += ContainerPropertyChanged;
		}

		void ContainerPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			isDirty = true;
		}
		
		public override bool SaveOptions()
		{
			// Only save container, if some option really has changed
			formattingEditor.OptionsContainer.PropertyChanged -= ContainerPropertyChanged;
			return (!isDirty || formattingPolicy.Save()) && base.SaveOptions();
		}
	}
}