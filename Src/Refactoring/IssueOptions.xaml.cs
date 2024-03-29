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
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using ICSharpCode.Core.Presentation;
using ICSharpCode.NRefactory.Al;
using ICSharpCode.SharpDevelop.Gui;
using ICSharpCode.SharpDevelop.Refactoring;

namespace AlBinding.Refactoring
{
	/// <summary>
	/// Interaction logic for IssueOptions.xaml
	/// </summary>
	public partial class IssueOptions : OptionPanel
	{
		ObservableCollection<IssueOptionsViewModel> viewModels;
		
		public IssueOptions()
		{
			InitializeComponent();
			viewModels = new ObservableCollection<IssueOptionsViewModel>(
				from p in IssueManager.IssueProviders
				where p.Attribute != null
				select new IssueOptionsViewModel(p)
			);
			ICollectionView view = CollectionViewSource.GetDefaultView(viewModels);
			if (viewModels.Any(p => !string.IsNullOrEmpty(p.Category)))
				view.GroupDescriptions.Add(new PropertyGroupDescription("Category"));
			listBox.ItemsSource = view;
		}
		
		public override void LoadOptions()
		{
			base.LoadOptions();
			foreach (var m in viewModels) {
				m.Severity = m.Provider.CurrentSeverity;
			}
		}
		
		public override bool SaveOptions()
		{
			foreach (var m in viewModels) {
				m.Provider.CurrentSeverity = m.Severity;
			}
			IssueManager.SaveIssueSeveritySettings();
			return base.SaveOptions();
		}
		
		void ComboBox_GotFocus(object sender, RoutedEventArgs e)
		{
			var item = WpfTreeNavigation.TryFindParent<ListBoxItem>((ComboBox)sender);
			if (item != null) {
				item.IsSelected = true;
			}
		}
	}
}
