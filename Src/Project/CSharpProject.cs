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
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ICSharpCode.Core;
using ICSharpCode.NRefactory.Al;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.SharpDevelop;
using ICSharpCode.SharpDevelop.Project;
using ICSharpCode.SharpDevelop.Project.Converter;
using ICSharpCode.SharpDevelop.Refactoring;
using Microsoft.CSharp;
using System.Windows;
using System.Collections;
using System.Reflection;

namespace AlBinding
{
	/// <summary>
	/// IProject implementation for .alproj files.
	/// </summary>
   
    public class AlProject : CompilableProject, ICloneable
	{
        /// <summary>
        /// Clone the object, and returning a reference to a cloned object.
        /// </summary>
        /// <returns>Reference to the new cloned 
        /// object.</returns>
        public object Clone()
        {
            //First we create an instance of this specific type.
            object newObject = Activator.CreateInstance(this.GetType(),  this.info);

            //We get the array of fields for the new type instance.
            FieldInfo[] fields = newObject.GetType().GetFields();

            int i = 0;

            foreach (FieldInfo fi in this.GetType().GetFields())
            {
                //We query if the fiels support the ICloneable interface.
                Type ICloneType = fi.FieldType.
                            GetInterface("ICloneable", true);

                if (ICloneType != null)
                {
                    //Getting the ICloneable interface from the object.
                    ICloneable IClone = (ICloneable)fi.GetValue(this);

                    //We use the clone method to set the new value to the field.
                    fields[i].SetValue(newObject, IClone.Clone());
                }
                else
                {
                    // If the field doesn't support the ICloneable 
                    // interface then just set it.
                    fields[i].SetValue(newObject, fi.GetValue(this));
                }

                //Now we check if the object support the 
                //IEnumerable interface, so if it does
                //we need to enumerate all its items and check if 
                //they support the ICloneable interface.
                Type IEnumerableType = fi.FieldType.GetInterface
                                ("IEnumerable", true);
                if (IEnumerableType != null)
                {
                    //Get the IEnumerable interface from the field.
                    IEnumerable IEnum = (IEnumerable)fi.GetValue(this);

                    //This version support the IList and the 
                    //IDictionary interfaces to iterate on collections.
                    Type IListType = fields[i].FieldType.GetInterface
                                        ("IList", true);
                    Type IDicType = fields[i].FieldType.GetInterface
                                        ("IDictionary", true);

                    int j = 0;
                    if (IListType != null)
                    {
                        //Getting the IList interface.
                        IList list = (IList)fields[i].GetValue(newObject);

                        foreach (object obj in IEnum)
                        {
                            //Checking to see if the current item 
                            //support the ICloneable interface.
                            ICloneType = obj.GetType().
                                GetInterface("ICloneable", true);

                            if (ICloneType != null)
                            {
                                //If it does support the ICloneable interface, 
                                //we use it to set the clone of
                                //the object in the list.
                                ICloneable clone = (ICloneable)obj;

                                list[j] = clone.Clone();
                            }

                            //NOTE: If the item in the list is not 
                            //support the ICloneable interface then in the 
                            //cloned list this item will be the same 
                            //item as in the original list
                            //(as long as this type is a reference type).

                            j++;
                        }
                    }
                    else if (IDicType != null)
                    {
                        //Getting the dictionary interface.
                        IDictionary dic = (IDictionary)fields[i].
                                            GetValue(newObject);
                        j = 0;

                        foreach (DictionaryEntry de in IEnum)
                        {
                            //Checking to see if the item 
                            //support the ICloneable interface.
                            ICloneType = de.Value.GetType().
                                GetInterface("ICloneable", true);

                            if (ICloneType != null)
                            {
                                ICloneable clone = (ICloneable)de.Value;

                                dic[de.Key] = clone.Clone();
                            }
                            j++;
                        }
                    }
                }
                i++;
            }
            return newObject;
        }



		Properties globalPreferences;
		FileName globalSettingsFileName;
		
		public override IAmbience GetAmbience()
		{
			return new AlAmbience();
		}
		
		public override string Language {
			get { return AlProjectBinding.LanguageName; }
		}
		
		public Version LanguageVersion {
			get {
				string toolsVersion;
				lock (SyncRoot) toolsVersion = this.ToolsVersion;
				Version version;
				if (!Version.TryParse(toolsVersion, out version))
					version = new Version(4, 0); // use 4.0 as default if ToolsVersion attribute is missing/malformed
				if (version == new Version(4, 0) && DotnetDetection.IsDotnet45Installed())
					return new Version(5, 0);
				return version;
			}
		}
		
		void Init()
		{
			globalPreferences = new Properties();
			
			reparseReferencesSensitiveProperties.Add("TargetFrameworkVersion");
			reparseCodeSensitiveProperties.Add("DefineConstants");
			reparseCodeSensitiveProperties.Add("AllowUnsafeBlocks");
			reparseCodeSensitiveProperties.Add("CheckForOverflowUnderflow");
		}
        ProjectLoadInformation info;
		public AlProject(ProjectLoadInformation loadInformation)
			: base(loadInformation)
		{
            info = loadInformation;
			Init();
			if (loadInformation.InitializeTypeSystem)
				InitializeProjectContent(new AlProjectContent());
		}
		
		public const string DefaultTargetsFile = @"$(MSBuildToolsPath)\Arsslensoft.Al.targets";
		
		public AlProject(ProjectCreateInformation info)
			: base(info)
		{
			Init();
			
			this.AddImport(DefaultTargetsFile, null);
			
			SetProperty("Debug", null, "CheckForOverflowUnderflow", "True",
			            PropertyStorageLocations.ConfigurationSpecific, true);
			SetProperty("Release", null, "CheckForOverflowUnderflow", "False",
			            PropertyStorageLocations.ConfigurationSpecific, true);
			
			SetProperty("Debug", null, "DefineConstants", "DEBUG;TRACE",
			            PropertyStorageLocations.ConfigurationSpecific, false);
			SetProperty("Release", null, "DefineConstants", "TRACE",
			            PropertyStorageLocations.ConfigurationSpecific, false);
			
			if (info.InitializeTypeSystem)
				InitializeProjectContent(new AlProjectContent());
		}
        public Task<bool> BaseBuild(ProjectBuildOptions options, IBuildFeedbackSink feedbackSink, IProgressMonitor progressMonitor)
        {
        
           return base.BuildAsync(options, feedbackSink, progressMonitor);
        }
		public override Task<bool> BuildAsync(ProjectBuildOptions options, IBuildFeedbackSink feedbackSink, IProgressMonitor progressMonitor)
		{


      
			if (this.MinimumSolutionVersion == SolutionFormatVersion.VS2005) {
				return SD.MSBuildEngine.BuildAsync(
					this, options, feedbackSink, progressMonitor.CancellationToken,
					new [] { Path.Combine(FileUtility.ApplicationRootPath, @"bin\SharpDevelop.CheckMSBuild35Features.targets") });
			} else {
				return base.BuildAsync(options, feedbackSink, progressMonitor);
			}
           
     
		}
		
		volatile CompilerSettings compilerSettings;
		
		public CompilerSettings CompilerSettings {
			get {
				if (compilerSettings == null)
					CreateCompilerSettings();
				return compilerSettings;
			}
		}
		
		public Properties GlobalPreferences
		{
			get {
				return globalPreferences;
			}
		}
		
		public override void ProjectLoaded()
		{
			base.ProjectLoaded();
			
			// Load SD settings file
			globalSettingsFileName = new FileName(FileName + ".sdsettings");
			if (File.Exists(globalSettingsFileName)) {
				globalPreferences = Properties.Load(globalSettingsFileName);
			}
			if (globalPreferences == null)
				globalPreferences = new Properties();
		}
		
		public override void Save(string fileName)
		{
			// Save project extensions
			if (globalPreferences != null && globalPreferences.IsDirty) {
				globalPreferences.Save(new FileName(fileName + ".sdsettings"));
				globalPreferences.IsDirty = false;
			}
			base.Save(fileName);
		}
		
		protected override object CreateCompilerSettings()
		{
			// This method gets called when the project content is first created;
			// or when any of the ReparseSensitiveProperties has changed.
			CompilerSettings settings = new CompilerSettings();
			settings.AllowUnsafeBlocks = GetBoolProperty("AllowUnsafeBlocks") ?? false;
			settings.CheckForOverflow = GetBoolProperty("CheckForOverflowUnderflow") ?? false;
			
			string symbols = GetEvaluatedProperty("DefineConstants");
			if (symbols != null) {
				foreach (string symbol in symbols.Split(new char[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries)) {
					settings.ConditionalSymbols.Add(symbol.Trim());
				}
			}
			settings.Freeze();
			compilerSettings = settings;
			return settings;
		}
		
		bool? GetBoolProperty(string propertyName)
		{
			string val = GetEvaluatedProperty(propertyName);
			if ("true".Equals(val, StringComparison.OrdinalIgnoreCase))
				return true;
			if ("false".Equals(val, StringComparison.OrdinalIgnoreCase))
				return false;
			return null;
		}
		
		protected override ProjectBehavior CreateDefaultBehavior()
		{
			return new AlProjectBehavior(this, base.CreateDefaultBehavior());
		}
		
		public override CodeDomProvider CreateCodeDomProvider()
		{
            return new Arsslensoft.Al.AlCodeProvider();
		}
		
		ILanguageBinding language;
		
		public override ILanguageBinding LanguageBinding {
			get {
				if (language == null)
					language = SD.LanguageService.GetLanguageByName("Al");
				return language;
			}
		}
	}
	
	public class AlProjectBehavior : ProjectBehavior
	{
		public AlProjectBehavior(AlProject project, ProjectBehavior next = null)
			: base(project, next)
		{
			
		}
		
		public override ItemType GetDefaultItemType(string fileName)
		{
			if (string.Equals(Path.GetExtension(fileName), ".al", StringComparison.OrdinalIgnoreCase))
				return ItemType.Compile;
			else
				return base.GetDefaultItemType(fileName);
		}
		
		static readonly CompilerVersion msbuild20 = new CompilerVersion(new Version(2, 0), "AL 2.0");
		static readonly CompilerVersion msbuild35 = new CompilerVersion(new Version(3, 5), "AL 3.0");
        static readonly CompilerVersion msbuild40 = new CompilerVersion(new Version(4, 0), DotnetDetection.IsDotnet45Installed() ? "AL 5.0" : "AL 4.0");
		
		public override CompilerVersion CurrentCompilerVersion {
			get {
				switch (Project.MinimumSolutionVersion) {
					case SolutionFormatVersion.VS2005:
						return msbuild20;
					case SolutionFormatVersion.VS2008:
						return msbuild35;
					case SolutionFormatVersion.VS2010:
					case SolutionFormatVersion.VS2012:
						return msbuild40;
					default:
						throw new NotSupportedException();
				}
			}
		}
		
		public override IEnumerable<CompilerVersion> GetAvailableCompilerVersions()
		{
			List<CompilerVersion> versions = new List<CompilerVersion>();
			if (DotnetDetection.IsDotnet35SP1Installed()) {
				versions.Add(msbuild20);
				versions.Add(msbuild35);
			}
			versions.Add(msbuild40);
			return versions;
		}
		
		public override ISymbolSearch PrepareSymbolSearch(ISymbol entity)
		{
			return CompositeSymbolSearch.Create(new AlSymbolSearch(Project, entity), base.PrepareSymbolSearch(entity));
		}
	}
}
