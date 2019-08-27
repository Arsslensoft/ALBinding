using System.Diagnostics;
using System;
using System.IO;
using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Reflection;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Text;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Security.Permissions;
using System.Security.Principal;
using System.Collections.Generic;
using System.Runtime.Versioning;
using System.Threading;

namespace Arsslensoft.Al
{
    [PermissionSet(SecurityAction.LinkDemand, Unrestricted = true)]
    [PermissionSet(SecurityAction.InheritanceDemand, Unrestricted = true)]
    public class AlCodeProvider : CodeDomProvider
    {
        IDictionary<string, string> providerOptions;
        private AlCodeGenerator generator;
        private AlCodeCompiler compiler;
        //
        // Constructors
        //
        public AlCodeProvider()
        {
            generator = new AlCodeGenerator();
            compiler = new AlCodeCompiler();
        }

        public AlCodeProvider(IDictionary<string, string> providerOptions)
        {
            this.providerOptions = providerOptions;
            compiler = new AlCodeCompiler(providerOptions);
            generator = new AlCodeGenerator(providerOptions);
        }

        //
        // Properties
        //
        public override string FileExtension
        {
            get
            {
                return "al";
            }
        }

        //
        // Methods
        //
        [Obsolete("Use CodeDomProvider class")]
        public override ICodeCompiler CreateCompiler()
        {
           
                return ( AlCodeCompiler)compiler;
      
        }

        [Obsolete("Use CodeDomProvider class")]
        public override ICodeGenerator CreateGenerator()
        {
           
                return ( AlCodeGenerator)generator;
            
        }

      
        public override TypeConverter GetConverter(Type type)
        {
            throw new NotImplementedException();
        }

       
        public override void GenerateCodeFromMember(CodeTypeMember member, TextWriter writer, CodeGeneratorOptions options)
        {

            generator.GenerateCodeFromMember(member, writer, options);
        }
    }


    internal class AlCodeCompiler : AlCodeGenerator, ICodeCompiler
    {
        static string windowsMcsPath;
    //    static string windowsMonoPath;

        Mutex mcsOutMutex;
        StringCollection mcsOutput;

        static AlCodeCompiler()
        {
            if (Path.DirectorySeparatorChar == '\\')
            {
                //PropertyInfo gac = typeof(Environment).GetProperty("GacPath", BindingFlags.Static | BindingFlags.NonPublic);
                //MethodInfo get_gac = gac.GetGetMethod(true);
                //string p = Path.GetDirectoryName(
                //    (string)get_gac.Invoke(null, null));
                //windowsMonoPath = Path.Combine(
                //    Path.GetDirectoryName(
                //        Path.GetDirectoryName(p)),
                //    "bin\\mono.bat");
                //if (!File.Exists(windowsMonoPath))
                //    windowsMonoPath = Path.Combine(
                //        Path.GetDirectoryName(
                //            Path.GetDirectoryName(p)),
                //        "bin\\mono.exe");
                //if (!File.Exists(windowsMonoPath))
                //    windowsMonoPath = Path.Combine(
                //        Path.GetDirectoryName(
                //            Path.GetDirectoryName(
                //                Path.GetDirectoryName(p))),
                //        "mono\\mono\\mini\\mono.exe");
             
                //if (!File.Exists(windowsMonoPath))
                //    throw new FileNotFoundException("Windows mono path not found: " + windowsMonoPath);

                //windowsMcsPath = Path.Combine(p, "4.5\\mcs.exe");
                //if (!File.Exists(windowsMcsPath))
                //    windowsMcsPath = Path.Combine(Path.GetDirectoryName(p), "lib\\build\\mcs.exe");

                //if (!File.Exists(windowsMcsPath))
                //    throw new FileNotFoundException("Windows mcs path not found: " + windowsMcsPath);

                windowsMcsPath = AppDomain.CurrentDomain.BaseDirectory + "ALC.exe";
            }
        }

        //
        // Constructors
        //
        public AlCodeCompiler()
        {
        }

        public AlCodeCompiler(IDictionary<string, string> providerOptions) :
            base(providerOptions)
        {
        }

        //
        // Methods
        //
        public CompilerResults CompileAssemblyFromDom(CompilerParameters options, CodeCompileUnit e)
        {
            return CompileAssemblyFromDomBatch(options, new CodeCompileUnit[] { e });
        }

        public CompilerResults CompileAssemblyFromDomBatch(CompilerParameters options, CodeCompileUnit[] ea)
        {
            if (options == null)
            {
                throw new ArgumentNullException("options");
            }

            try
            {
                return CompileFromDomBatch(options, ea);
            }
            finally
            {
                options.TempFiles.Delete();
            }
        }

        public CompilerResults CompileAssemblyFromFile(CompilerParameters options, string fileName)
        {
            return CompileAssemblyFromFileBatch(options, new string[] { fileName });
        }

        public CompilerResults CompileAssemblyFromFileBatch(CompilerParameters options, string[] fileNames)
        {
            if (options == null)
            {
                throw new ArgumentNullException("options");
            }

            try
            {
                return CompileFromFileBatch(options, fileNames);
            }
            finally
            {
                options.TempFiles.Delete();
            }
        }

        public CompilerResults CompileAssemblyFromSource(CompilerParameters options, string source)
        {
            return CompileAssemblyFromSourceBatch(options, new string[] { source });
        }

        public CompilerResults CompileAssemblyFromSourceBatch(CompilerParameters options, string[] sources)
        {
            if (options == null)
            {
                throw new ArgumentNullException("options");
            }

            try
            {
                return CompileFromSourceBatch(options, sources);
            }
            finally
            {
                options.TempFiles.Delete();
            }
        }

        private CompilerResults CompileFromFileBatch(CompilerParameters options, string[] fileNames)
        {
            if (null == options)
                throw new ArgumentNullException("options");
            if (null == fileNames)
                throw new ArgumentNullException("fileNames");

            CompilerResults results = new CompilerResults(options.TempFiles);
            Process mcs = new Process();

            // FIXME: these lines had better be platform independent.
            //if (Path.DirectorySeparatorChar == '\\')
            //{
            //    mcs.StartInfo.FileName = windowsMcsPath;
            //    mcs.StartInfo.Arguments = "\"" + windowsMcsPath + "\" " +
            //        BuildArgs(options, fileNames, ProviderOptions);
            //}
            //else
            //{
                mcs.StartInfo.FileName = "alc";
                mcs.StartInfo.Arguments = BuildArgs(options, fileNames, ProviderOptions);
            //}

            mcsOutput = new StringCollection();
            mcsOutMutex = new Mutex();
            /*		       
                        string monoPath = Environment.GetEnvironmentVariable ("MONO_PATH");
                        if (monoPath != null)
                            monoPath = String.Empty;
                        string privateBinPath = AppDomain.CurrentDomain.SetupInformation.PrivateBinPath;
                        if (privateBinPath != null && privateBinPath.Length > 0)
                            monoPath = String.Format ("{0}:{1}", privateBinPath, monoPath);
                        if (monoPath.Length > 0) {
                            StringDictionary dict = mcs.StartInfo.EnvironmentVariables;
                            if (dict.ContainsKey ("MONO_PATH"))
                                dict ["MONO_PATH"] = monoPath;
                            else
                                dict.Add ("MONO_PATH", monoPath);
                        }
            */
            /*
             * reset MONO_GC_PARAMS - we are invoking compiler possibly with another GC that
             * may not handle some of the options causing compilation failure
             */
            mcs.StartInfo.EnvironmentVariables["MONO_GC_PARAMS"] = String.Empty;

            mcs.StartInfo.CreateNoWindow = true;
            mcs.StartInfo.UseShellExecute = false;
            mcs.StartInfo.RedirectStandardOutput = true;
            mcs.StartInfo.RedirectStandardError = true;
            mcs.ErrorDataReceived += new DataReceivedEventHandler(McsStderrDataReceived);

            try
            {
                mcs.Start();
            }
            catch (Exception e)
            {
                Win32Exception exc = e as Win32Exception;
                if (exc != null)
                {
                    throw new SystemException(String.Format("Error running {0}: {1}", mcs.StartInfo.FileName,
                                   exc.NativeErrorCode));
                }
                throw;
            }

            try
            {
                mcs.BeginOutputReadLine();
                mcs.BeginErrorReadLine();
                mcs.WaitForExit();

                results.NativeCompilerReturnValue = mcs.ExitCode;
            }
            finally
            {
                mcs.CancelErrorRead();
                mcs.CancelOutputRead();
                mcs.Close();
            }

            StringCollection sc = mcsOutput;

            bool loadIt = true;
            foreach (string error_line in mcsOutput)
            {
                CompilerError error = CreateErrorFromString(error_line);
                if (error != null)
                {
                    results.Errors.Add(error);
                    if (!error.IsWarning)
                        loadIt = false;
                }
            }

            if (sc.Count > 0)
            {
                sc.Insert(0, mcs.StartInfo.FileName + " " + mcs.StartInfo.Arguments + Environment.NewLine);
             
            
                foreach(string s in sc)
                results.Output.Add(s);
            }

            if (loadIt)
            {
                if (!File.Exists(options.OutputAssembly))
                {
                    StringBuilder sb = new StringBuilder();
                    foreach (string s in sc)
                        sb.Append(s + Environment.NewLine);

                    throw new Exception("Compiler failed to produce the assembly. Output: '" + sb.ToString() + "'");
                }

                if (options.GenerateInMemory)
                {
                    using (FileStream fs = File.OpenRead(options.OutputAssembly))
                    {
                        byte[] buffer = new byte[fs.Length];
                        fs.Read(buffer, 0, buffer.Length);
                        results.CompiledAssembly = Assembly.Load(buffer, null);
                        fs.Close();
                    }
                }
                else
                {
                    // Avoid setting CompiledAssembly right now since the output might be a netmodule
                    results.PathToAssembly = options.OutputAssembly;
                }
            }
            else
            {
                results.CompiledAssembly = null;
            }

            return results;
        }

        void McsStderrDataReceived(object sender, DataReceivedEventArgs args)
        {
            if (args.Data != null)
            {
                mcsOutMutex.WaitOne();
                mcsOutput.Add(args.Data);
                mcsOutMutex.ReleaseMutex();
            }
        }

        private static string BuildArgs(CompilerParameters options, string[] fileNames, IDictionary<string, string> providerOptions)
        {
            StringBuilder args = new StringBuilder();
            if (options.GenerateExecutable)
                args.Append("/target:exe ");
            else
                args.Append("/target:library ");

            string privateBinPath = AppDomain.CurrentDomain.SetupInformation.PrivateBinPath;
            if (privateBinPath != null && privateBinPath.Length > 0)
                args.AppendFormat("/lib:\"{0}\" ", privateBinPath);

            if (options.Win32Resource != null)
                args.AppendFormat("/win32res:\"{0}\" ",
                    options.Win32Resource);

            if (options.IncludeDebugInformation)
                args.Append("/debug+ /optimize- ");
            else
                args.Append("/debug- /optimize+ ");

            if (options.TreatWarningsAsErrors)
                args.Append("/warnaserror ");

            if (options.WarningLevel >= 0)
                args.AppendFormat("/warn:{0} ", options.WarningLevel);

            if (options.OutputAssembly == null || options.OutputAssembly.Length == 0)
            {
                string extension = (options.GenerateExecutable ? "exe" : "dll");
                options.OutputAssembly = GetTempFileNameWithExtension(options.TempFiles, extension,
                    !options.GenerateInMemory);
            }
            args.AppendFormat("/out:\"{0}\" ", options.OutputAssembly);

            foreach (string import in options.ReferencedAssemblies)
            {
                if (import == null || import.Length == 0)
                    continue;

                args.AppendFormat("/r:\"{0}\" ", import);
            }

            if (options.CompilerOptions != null)
            {
                args.Append(options.CompilerOptions);
                args.Append(" ");
            }

            foreach (string embeddedResource in options.EmbeddedResources)
            {
                args.AppendFormat("/resource:\"{0}\" ", embeddedResource);
            }

            foreach (string linkedResource in options.LinkedResources)
            {
                args.AppendFormat("/linkresource:\"{0}\" ", linkedResource);
            }

            if (providerOptions != null && providerOptions.Count > 0)
            {
                string langver;

                if (!providerOptions.TryGetValue("CompilerVersion", out langver))
                    langver = "3.5";

                if (langver.Length >= 1 && langver[0] == 'v')
                    langver = langver.Substring(1);

                switch (langver)
                {
                    case "2.0":
                        args.Append("/langversion:ISO-2 ");
                        break;

                    case "3.5":
                        // current default, omit the switch
                        break;
                }
            }

            args.Append("/sdk:4.5");

            args.Append(" -- ");
            foreach (string source in fileNames)
                args.AppendFormat("\"{0}\" ", source);
            return args.ToString();
        }

        // Keep in sync with mcs/class/Microsoft.Build.Utilities/Microsoft.Build.Utilities/ToolTask.cs
        const string ErrorRegexPattern = @"
			^
			(\s*(?<file>[^\(]+)                         # filename (optional)
			 (\((?<line>\d*)(,(?<column>\d*[\+]*))?\))? # line+column (optional)
			 :\s+)?
			(?<level>\w+)                               # error|warning
			\s+
			(?<number>[^:]*\d)                          # CS1234
			:
			\s*
			(?<message>.*)$";

        private static CompilerError CreateErrorFromString(string error_string)
        {
            if (error_string.StartsWith("BETA"))
                return null;

            if (error_string == null || error_string == "")
                return null;

            CompilerError error = new CompilerError();
            Regex reg = new Regex(ErrorRegexPattern, RegexOptions.Compiled | RegexOptions.ExplicitCapture | RegexOptions.IgnorePatternWhitespace);
            Match match = reg.Match(error_string);
            if (!match.Success)
            {
                // We had some sort of runtime crash
                error.ErrorText = error_string;
                error.IsWarning = false;
                error.ErrorNumber = "";
                return error;
            }
            if (String.Empty != match.Result("${file}"))
                error.FileName = match.Result("${file}");
            if (String.Empty != match.Result("${line}"))
                error.Line = Int32.Parse(match.Result("${line}"));
            if (String.Empty != match.Result("${column}"))
                error.Column = Int32.Parse(match.Result("${column}").Trim('+'));

            string level = match.Result("${level}");
            if (level == "warning")
                error.IsWarning = true;
            else if (level != "error")
                return null; // error CS8028 will confuse the regex.

            error.ErrorNumber = match.Result("${number}");
            error.ErrorText = match.Result("${message}");
            return error;
        }

        private static string GetTempFileNameWithExtension(TempFileCollection temp_files, string extension, bool keepFile)
        {
            return temp_files.AddExtension(extension, keepFile);
        }

        private static string GetTempFileNameWithExtension(TempFileCollection temp_files, string extension)
        {
            return temp_files.AddExtension(extension);
        }

        private CompilerResults CompileFromDomBatch(CompilerParameters options, CodeCompileUnit[] ea)
        {
            if (options == null)
            {
                throw new ArgumentNullException("options");
            }

            if (ea == null)
            {
                throw new ArgumentNullException("ea");
            }

            string[] fileNames = new string[ea.Length];
            StringCollection assemblies = options.ReferencedAssemblies;

            for (int i = 0; i < ea.Length; i++)
            {
                CodeCompileUnit compileUnit = ea[i];
                fileNames[i] = GetTempFileNameWithExtension(options.TempFiles, i + ".al");
                FileStream f = new FileStream(fileNames[i], FileMode.OpenOrCreate);
                StreamWriter s = new StreamWriter(f, Encoding.UTF8);
                if (compileUnit.ReferencedAssemblies != null)
                {
                    foreach (string str in compileUnit.ReferencedAssemblies)
                    {
                        if (!assemblies.Contains(str))
                            assemblies.Add(str);
                    }
                }

                ((ICodeGenerator)this).GenerateCodeFromCompileUnit(compileUnit, s, new CodeGeneratorOptions());
                s.Close();
                f.Close();
            }
            return CompileAssemblyFromFileBatch(options, fileNames);
        }

        private CompilerResults CompileFromSourceBatch(CompilerParameters options, string[] sources)
        {
            if (options == null)
            {
                throw new ArgumentNullException("options");
            }

            if (sources == null)
            {
                throw new ArgumentNullException("sources");
            }

            string[] fileNames = new string[sources.Length];

            for (int i = 0; i < sources.Length; i++)
            {
                fileNames[i] = GetTempFileNameWithExtension(options.TempFiles, i + ".al");
                FileStream f = new FileStream(fileNames[i], FileMode.OpenOrCreate);
                using (StreamWriter s = new StreamWriter(f, Encoding.UTF8))
                {
                    s.Write(sources[i]);
                    s.Close();
                }
                f.Close();
            }
            return CompileFromFileBatch(options, fileNames);
        }
    }

//    internal class AlCodeGenerator
//        : CodeGenerator
//    {
//        IDictionary<string, string> providerOptions;
//        private IndentedTextWriter output;
//        private CodeGeneratorOptions options;
//        private CodeTypeDeclaration currentClass;
//        private CodeTypeMember currentMember;
//        private bool inNestedBinary = false;

//        private const int ParameterMultilineThreshold = 15;
//        private const int MaxLineLength = 80;
//        private const GeneratorSupport LanguageSupport = GeneratorSupport.ArraysOfArrays |
//                                                         GeneratorSupport.EntryPointMethod |
//                                                         GeneratorSupport.GotoStatements |
//                                                         GeneratorSupport.MultidimensionalArrays |
//                                                         GeneratorSupport.StaticConstructors |
//                                                         GeneratorSupport.TryCatchStatements |
//                                                         GeneratorSupport.ReturnTypeAttributes |
//                                                         GeneratorSupport.AssemblyAttributes |
//                                                         GeneratorSupport.DeclareValueTypes |
//                                                         GeneratorSupport.DeclareEnums |
//                                                         GeneratorSupport.DeclareEvents |
//                                                         GeneratorSupport.DeclareDelegates |
//                                                         GeneratorSupport.DeclareInterfaces |
//                                                         GeneratorSupport.ParameterAttributes |
//                                                         GeneratorSupport.ReferenceParameters |
//                                                         GeneratorSupport.ChainedConstructorArguments |
//                                                         GeneratorSupport.NestedTypes |
//                                                         GeneratorSupport.MultipleInterfaceMembers |
//                                                         GeneratorSupport.PublicStaticMembers |
//                                                         GeneratorSupport.ComplexExpressions |
//#if !FEATURE_PAL
// GeneratorSupport.Win32Resources |
//#endif // !FEATURE_PAL
// GeneratorSupport.Resources |
//                                                         GeneratorSupport.PartialTypes |
//                                                         GeneratorSupport.GenericTypeReference |
//                                                         GeneratorSupport.GenericTypeDeclaration |
//                                                         GeneratorSupport.DeclareIndexerProperties;
//        private static volatile Regex outputRegWithFileAndLine;
//        private static volatile Regex outputRegSimple;
 

//         It is used for beautiful "for" syntax
//        bool dont_write_semicolon;

        
//         Constructors
        
//        public AlCodeGenerator()
//        {
//            dont_write_semicolon = false;
//        }

//        public AlCodeGenerator(IDictionary<string, string> providerOptions)
//        {
//            this.providerOptions = providerOptions;
//        }


//        private int Indent
//        {
//            get
//            {
//                return output.Indent;
//            }
//            set
//            {
//                output.Indent = value;
//            }
//        }

//         <devdoc>
//            <para>
//               Gets or sets a value indicating whether the current object being
//               generated is an interface.
//            </para>
//         </devdoc>
//        private bool IsCurrentInterface
//        {
//            get
//            {
//                if (currentClass != null && !(currentClass is CodeTypeDelegate))
//                {
//                    return currentClass.IsInterface;
//                }
//                return false;
//            }
//        }

//         <devdoc>
//            <para>
//               Gets or sets a value indicating whether the current object being generated
//               is a class.
//            </para>
//         </devdoc>
//        private bool IsCurrentClass
//        {
//            get
//            {
//                if (currentClass != null && !(currentClass is CodeTypeDelegate))
//                {
//                    return currentClass.IsClass;
//                }
//                return false;
//            }
//        }

//         <devdoc>
//            <para>
//               Gets or sets a value indicating whether the current object being generated
//               is a struct.
//            </para>
//         </devdoc>
//        private bool IsCurrentStruct
//        {
//            get
//            {
//                if (currentClass != null && !(currentClass is CodeTypeDelegate))
//                {
//                    return currentClass.IsStruct;
//                }
//                return false;
//            }
//        }

//         <devdoc>
//            <para>
//               Gets or sets a value indicating whether the current object being generated
//               is an enumeration.
//            </para>
//         </devdoc>
//        private bool IsCurrentEnum
//        {
//            get
//            {
//                if (currentClass != null && !(currentClass is CodeTypeDelegate))
//                {
//                    return currentClass.IsEnum;
//                }
//                return false;
//            }
//        }

//         <devdoc>
//            <para>
//               Gets or sets a value indicating whether the current object being generated
//               is a delegate.
//            </para>
//         </devdoc>
//        private bool IsCurrentDelegate
//        {
//            get
//            {
//                if (currentClass != null && currentClass is CodeTypeDelegate)
//                {
//                    return true;
//                }
//                return false;
//            }
//        }

    

//         <devdoc>
//            <para>[To be supplied.]</para>
//         </devdoc>
//        private CodeGeneratorOptions Options
//        {
//            get
//            {
//                return options;
//            }
//        }

//        private TextWriter Output
//        {
//            get
//            {
//                return output;
//            }
//        }
 
//        protected IDictionary<string, string> ProviderOptions
//        {
//            get { return providerOptions; }
//        }

        
//         Properties
        
//        protected override string NullToken
//        {
//            get
//            {
//                return "nothing";
//            }
//        }

        
//         Methods
        

//        protected override void GenerateArrayCreateExpression(CodeArrayCreateExpression expression)
//        {
            
//             This tries to replicate MS behavior as good as
//             possible.
            
//             The Code-Array stuff in ms.net seems to be broken
//             anyways, or I'm too stupid to understand it.
            
//             I'm sick of it. If you try to develop array
//             creations, test them on windows. If it works there
//             but not in mono, drop me a note.  I'd be especially
//             interested in jagged-multidimensional combinations
//             with proper initialization :}
            

//            TextWriter output = Output;

//            output.Write("new ");

//            CodeExpressionCollection initializers = expression.Initializers;
//            CodeTypeReference createType = expression.CreateType;

//            if (initializers.Count > 0)
//            {

//                OutputType(createType);

//                if (expression.CreateType.ArrayRank == 0)
//                {
//                    output.Write("[]");
//                }

//                OutputStartBrace();
//                ++Indent;
//                OutputExpressionList(initializers, true);
//                --Indent;
//                output.Write("}");
//            }
//            else
//            {
//                CodeTypeReference arrayType = createType.ArrayElementType;
//                while (arrayType != null)
//                {
//                    createType = arrayType;
//                    arrayType = arrayType.ArrayElementType;
//                }

//                OutputType(createType);

//                output.Write('[');

//                CodeExpression size = expression.SizeExpression;
//                if (size != null)
//                    GenerateExpression(size);
//                else
//                    output.Write(expression.Size);

//                output.Write(']');
//            }
//        }

//        protected override void GenerateBaseReferenceExpression(CodeBaseReferenceExpression expression)
//        {
//            Output.Write("me");
//        }

//        protected override void GenerateCastExpression(CodeCastExpression expression)
//        {
//            TextWriter output = Output;
//            output.Write("((");
//            OutputType(expression.TargetType);
//            output.Write(")(");
//            GenerateExpression(expression.Expression);
//            output.Write("))");
//        }


//        protected override void GenerateCompileUnitStart(CodeCompileUnit compileUnit)
//        {
//            GenerateComment(new CodeComment("------------------------------------------------------------------------------"));
//            GenerateComment(new CodeComment(" <autogenerated>"));
//            GenerateComment(new CodeComment("     This code was generated by a tool."));
//            GenerateComment(new CodeComment("     .Net Runtime Version: " + System.Environment.Version));
//            GenerateComment(new CodeComment(""));
//            GenerateComment(new CodeComment("     Changes to this file may cause incorrect behavior and will be lost if "));
//            GenerateComment(new CodeComment("     the code is regenerated."));
//            GenerateComment(new CodeComment(" </autogenerated>"));
//            GenerateComment(new CodeComment("------------------------------------------------------------------------------"));
//            Output.WriteLine();
//            base.GenerateCompileUnitStart(compileUnit);
//        }

//        protected override void GenerateCompileUnit(CodeCompileUnit compileUnit)
//        {
//            GenerateCompileUnitStart(compileUnit);

//            List<CodeNamespaceImport> imports = null;
//            foreach (CodeNamespace codeNamespace in compileUnit.Namespaces)
//            {
//                if (!string.IsNullOrEmpty(codeNamespace.Name))
//                    continue;

//                if (codeNamespace.Imports.Count == 0)
//                    continue;

//                if (imports == null)
//                    imports = new List<CodeNamespaceImport>();

//                foreach (CodeNamespaceImport i in codeNamespace.Imports)
//                    imports.Add(i);
//            }

//            if (imports != null)
//            {
//                imports.Sort((a, b) => a.Namespace.CompareTo(b.Namespace));
//                foreach (var import in imports)
//                    GenerateNamespaceImport(import);

//                Output.WriteLine();
//            }

//            if (compileUnit.AssemblyCustomAttributes.Count > 0)
//            {
//                OutputAttributes(compileUnit.AssemblyCustomAttributes,
//                    "assembly: ", false);
//                Output.WriteLine("");
//            }

//            CodeNamespaceImportCollection global_imports = null;
//            foreach (CodeNamespace codeNamespace in compileUnit.Namespaces)
//            {
//                if (string.IsNullOrEmpty(codeNamespace.Name))
//                {
//                    global_imports = codeNamespace.Imports;
//                    if(global_imports != null)
//                    codeNamespace.Imports.Clear();

//                }

//                GenerateNamespace(codeNamespace);

//                if (global_imports != null)
//                {
//                    if (codeNamespace.Imports != null)
//                        codeNamespace.Imports.Clear();
               
//                    foreach (CodeNamespaceImport cns in global_imports)
//                        codeNamespace.Imports.Add(cns);

//                    global_imports = null;
//                }
//            }

//            GenerateCompileUnitEnd(compileUnit);
//        }

//        protected override void GenerateDefaultValueExpression(CodeDefaultValueExpression e)
//        {
//            Output.Write("default(");
//            OutputType(e.Type);
//            Output.Write(')');
//        }

//        protected override void GenerateDelegateCreateExpression(CodeDelegateCreateExpression expression)
//        {
//            TextWriter output = Output;

//            output.Write("new ");
//            OutputType(expression.DelegateType);
//            output.Write('(');

//            CodeExpression targetObject = expression.TargetObject;
//            if (targetObject != null)
//            {
//                GenerateExpression(targetObject);
//                Output.Write('.');
//            }
//            output.Write(GetSafeName(expression.MethodName));

//            output.Write(')');
//        }

//        protected override void GenerateFieldReferenceExpression(CodeFieldReferenceExpression expression)
//        {
//            CodeExpression targetObject = expression.TargetObject;
//            if (targetObject != null)
//            {
//                GenerateExpression(targetObject);
//                Output.Write('.');
//            }
//            Output.Write(GetSafeName(expression.FieldName));
//        }

//        protected override void GenerateArgumentReferenceExpression(CodeArgumentReferenceExpression expression)
//        {
//            Output.Write(GetSafeName(expression.ParameterName));
//        }

//        protected override void GenerateVariableReferenceExpression(CodeVariableReferenceExpression expression)
//        {
//            Output.Write(GetSafeName(expression.VariableName));
//        }

//        protected override void GenerateIndexerExpression(CodeIndexerExpression expression)
//        {
//            TextWriter output = Output;

//            GenerateExpression(expression.TargetObject);
//            output.Write('[');
//            OutputExpressionList(expression.Indices);
//            output.Write(']');
//        }

//        protected override void GenerateArrayIndexerExpression(CodeArrayIndexerExpression expression)
//        {
//            TextWriter output = Output;

//            GenerateExpression(expression.TargetObject);
//            output.Write('[');
//            OutputExpressionList(expression.Indices);
//            output.Write(']');
//        }

//        protected override void GenerateSnippetExpression(CodeSnippetExpression expression)
//        {
//            Output.Write(expression.Value);
//        }

//        protected override void GenerateMethodInvokeExpression(CodeMethodInvokeExpression expression)
//        {
//            TextWriter output = Output;

//            GenerateMethodReferenceExpression(expression.Method);

//            output.Write('(');
//            OutputExpressionList(expression.Parameters);
//            output.Write(')');
//        }

//        protected override void GenerateMethodReferenceExpression(CodeMethodReferenceExpression expression)
//        {
//            if (expression.TargetObject != null)
//            {
//                GenerateExpression(expression.TargetObject);
//                Output.Write('.');
//            };
//            Output.Write(GetSafeName(expression.MethodName));
//            if (expression.TypeArguments.Count > 0)
//                Output.Write(GetTypeArguments(expression.TypeArguments));
//        }

//        protected override void GenerateEventReferenceExpression(CodeEventReferenceExpression expression)
//        {
//            if (expression.TargetObject != null)
//            {
//                GenerateExpression(expression.TargetObject);
//                Output.Write('.');
//            }
//            Output.Write(GetSafeName(expression.EventName));
//        }

//        protected override void GenerateDelegateInvokeExpression(CodeDelegateInvokeExpression expression)
//        {
//            if (expression.TargetObject != null)
//                GenerateExpression(expression.TargetObject);
//            Output.Write('(');
//            OutputExpressionList(expression.Parameters);
//            Output.Write(')');
//        }

//        protected override void GenerateObjectCreateExpression(CodeObjectCreateExpression expression)
//        {
//            Output.Write("new ");
//            OutputType(expression.CreateType);
//            Output.Write('(');
//            OutputExpressionList(expression.Parameters);
//            Output.Write(')');
//        }

//        protected override void GeneratePropertyReferenceExpression(CodePropertyReferenceExpression expression)
//        {
//            CodeExpression targetObject = expression.TargetObject;
//            if (targetObject != null)
//            {
//                GenerateExpression(targetObject);
//                Output.Write('.');
//            }
//            Output.Write(GetSafeName(expression.PropertyName));
//        }

//        protected override void GeneratePropertySetValueReferenceExpression(CodePropertySetValueReferenceExpression expression)
//        {
//            Output.Write("value");
//        }

//        protected override void GenerateThisReferenceExpression(CodeThisReferenceExpression expression)
//        {
//            Output.Write("self");
//        }

//        protected override void GenerateExpressionStatement(CodeExpressionStatement statement)
//        {
//            GenerateExpression(statement.Expression);
//            if (dont_write_semicolon)
//                return;
//            Output.WriteLine(';');
//        }

//        protected override void GenerateIterationStatement(CodeIterationStatement statement)
//        {
//            TextWriter output = Output;

//            dont_write_semicolon = true;
//            output.Write("for (");
//            GenerateStatement(statement.InitStatement);
//            output.Write("; ");
//            GenerateExpression(statement.TestExpression);
//            output.Write("; ");
//            GenerateStatement(statement.IncrementStatement);
//            output.Write(")");
//            dont_write_semicolon = false;
//            OutputStartBrace();
//            ++Indent;
//            GenerateStatements(statement.Statements);
//            --Indent;
//            output.WriteLine('}');
//        }

//        protected override void GenerateThrowExceptionStatement(CodeThrowExceptionStatement statement)
//        {
//            Output.Write("throw");
//            if (statement.ToThrow != null)
//            {
//                Output.Write(' ');
//                GenerateExpression(statement.ToThrow);
//            }
//            Output.WriteLine(";");
//        }

//        protected override void GenerateComment(CodeComment comment)
//        {
//            TextWriter output = Output;

//            string commentChars = null;

//            if (comment.DocComment)
//            {
//                commentChars = "///";
//            }
//            else
//            {
//                commentChars = "//";
//            }

//            output.Write(commentChars);
//            output.Write(' ');
//            string text = comment.Text;

//            for (int i = 0; i < text.Length; i++)
//            {
//                output.Write(text[i]);
//                if (text[i] == '\r')
//                {
//                    if (i < (text.Length - 1) && text[i + 1] == '\n')
//                    {
//                        continue;
//                    }
//                    output.Write(commentChars);
//                }
//                else if (text[i] == '\n')
//                {
//                    output.Write(commentChars);
//                }
//            }

//            output.WriteLine();
//        }

//        protected override void GenerateMethodReturnStatement(CodeMethodReturnStatement statement)
//        {
//            TextWriter output = Output;

//            if (statement.Expression != null)
//            {
//                output.Write("ret ");
//                GenerateExpression(statement.Expression);
//                output.WriteLine(";");
//            }
//            else
//            {
//                output.WriteLine("ret;");
//            }
//        }

//        protected override void GenerateConditionStatement(CodeConditionStatement statement)
//        {
//            TextWriter output = Output;
//            output.Write("if (");
//            GenerateExpression(statement.Condition);
//            output.Write(")");
//            OutputStartBrace();

//            ++Indent;
//            GenerateStatements(statement.TrueStatements);
//            --Indent;

//            CodeStatementCollection falses = statement.FalseStatements;
//            if (falses.Count > 0)
//            {
//                output.Write('}');
//                if (Options.ElseOnClosing)
//                    output.Write(' ');
//                else
//                    output.WriteLine();
//                output.Write("else");
//                OutputStartBrace();
//                ++Indent;
//                GenerateStatements(falses);
//                --Indent;
//            }
//            output.WriteLine('}');
//        }

//        protected override void GenerateTryCatchFinallyStatement(CodeTryCatchFinallyStatement statement)
//        {
//            TextWriter output = Output;
//            CodeGeneratorOptions options = Options;

//            output.Write("try");
//            OutputStartBrace();
//            ++Indent;
//            GenerateStatements(statement.TryStatements);
//            --Indent;

//            foreach (CodeCatchClause clause in statement.CatchClauses)
//            {
//                output.Write('}');
//                if (options.ElseOnClosing)
//                    output.Write(' ');
//                else
//                    output.WriteLine();
//                output.Write("catch (");
//                OutputTypeNamePair(clause.CatchExceptionType, GetSafeName(clause.LocalName));
//                output.Write(")");
//                OutputStartBrace();
//                ++Indent;
//                GenerateStatements(clause.Statements);
//                --Indent;
//            }

//            CodeStatementCollection finallies = statement.FinallyStatements;
//            if (finallies.Count > 0)
//            {
//                output.Write('}');
//                if (options.ElseOnClosing)
//                    output.Write(' ');
//                else
//                    output.WriteLine();
//                output.Write("finally");
//                OutputStartBrace();
//                ++Indent;
//                GenerateStatements(finallies);
//                --Indent;
//            }

//            output.WriteLine('}');
//        }

//        protected override void GenerateAssignStatement(CodeAssignStatement statement)
//        {
//            TextWriter output = Output;
//            GenerateExpression(statement.Left);
//            output.Write(" = ");
//            GenerateExpression(statement.Right);
//            if (dont_write_semicolon)
//                return;
//            output.WriteLine(';');
//        }

//        protected override void GenerateAttachEventStatement(CodeAttachEventStatement statement)
//        {
//            TextWriter output = Output;

//            GenerateEventReferenceExpression(statement.Event);
//            output.Write(" += ");
//            GenerateExpression(statement.Listener);
//            output.WriteLine(';');
//        }

//        protected override void GenerateRemoveEventStatement(CodeRemoveEventStatement statement)
//        {
//            TextWriter output = Output;
//            GenerateEventReferenceExpression(statement.Event);
//            output.Write(" -= ");
//            GenerateExpression(statement.Listener);
//            output.WriteLine(';');
//        }

//        protected override void GenerateGotoStatement(CodeGotoStatement statement)
//        {
//            TextWriter output = Output;

//            output.Write("jump ");
//            output.Write(GetSafeName(statement.Label));
//            output.WriteLine(";");
//        }

//        protected override void GenerateLabeledStatement(CodeLabeledStatement statement)
//        {
//            Indent--;
//            Output.Write(statement.Label);
//            Output.WriteLine(":");
//            Indent++;

//            if (statement.Statement != null)
//            {
//                GenerateStatement(statement.Statement);
//            }
//        }

//        protected override void GenerateVariableDeclarationStatement(CodeVariableDeclarationStatement statement)
//        {
//            TextWriter output = Output;

//            OutputTypeNamePair(statement.Type, GetSafeName(statement.Name));

//            CodeExpression initExpression = statement.InitExpression;
//            if (initExpression != null)
//            {
//                output.Write(" = ");
//                GenerateExpression(initExpression);
//            }

//            if (!dont_write_semicolon)
//            {
//                output.WriteLine(';');
//            }
//        }

//        protected override void GenerateLinePragmaStart(CodeLinePragma linePragma)
//        {
//            Output.WriteLine();
//            Output.Write("#line ");
//            Output.Write(linePragma.LineNumber);
//            Output.Write(" \"");
//            Output.Write(linePragma.FileName);
//            Output.Write("\"");
//            Output.WriteLine();
//        }

//        protected override void GenerateLinePragmaEnd(CodeLinePragma linePragma)
//        {
//            Output.WriteLine();
//            Output.WriteLine("#line default");
//            Output.WriteLine("#line hidden");
//        }

//        protected override void GenerateEvent(CodeMemberEvent eventRef, CodeTypeDeclaration declaration)
//        {
//            if (IsCurrentDelegate || IsCurrentEnum)
//            {
//                return;
//            }

//            OutputAttributes(eventRef.CustomAttributes, null, false);

//            if (eventRef.PrivateImplementationType == null)
//            {
//                OutputMemberAccessModifier(eventRef.Attributes);
//            }

//            Output.Write("event ");

//            if (eventRef.PrivateImplementationType != null)
//            {
//                OutputTypeNamePair(eventRef.Type,
//                    eventRef.PrivateImplementationType.BaseType + "." +
//                    eventRef.Name);
//            }
//            else
//            {
//                OutputTypeNamePair(eventRef.Type, GetSafeName(eventRef.Name));
//            }
//            Output.WriteLine(';');
//        }

//        protected override void GenerateField(CodeMemberField field)
//        {
//            if (IsCurrentDelegate || IsCurrentInterface)
//            {
//                return;
//            }

//            TextWriter output = Output;

//            OutputAttributes(field.CustomAttributes, null, false);

//            if (IsCurrentEnum)
//            {
//                Output.Write(GetSafeName(field.Name));
//            }
//            else
//            {
//                MemberAttributes attributes = field.Attributes;
//                OutputMemberAccessModifier(attributes);
//                OutputVTableModifier(attributes);
//                OutputFieldScopeModifier(attributes);

//                OutputTypeNamePair(field.Type, GetSafeName(field.Name));
//            }

//            CodeExpression initExpression = field.InitExpression;
//            if (initExpression != null)
//            {
//                output.Write(" = ");
//                GenerateExpression(initExpression);
//            }

//            if (IsCurrentEnum)
//                output.WriteLine(',');
//            else
//                output.WriteLine(';');
//        }

//        protected override void GenerateSnippetMember(CodeSnippetTypeMember member)
//        {
//            Output.Write(member.Text);
//        }

//        protected override void GenerateEntryPointMethod(CodeEntryPointMethod method,
//                                  CodeTypeDeclaration declaration)
//        {
//            OutputAttributes(method.CustomAttributes, null, false);

//            Output.Write("public static ");
//            OutputType(method.ReturnType);
//            Output.Write(" Main()");
//            OutputStartBrace();
//            Indent++;
//            GenerateStatements(method.Statements);
//            Indent--;
//            Output.WriteLine("}");
//        }

//        protected override void GenerateMethod(CodeMemberMethod method,
//                            CodeTypeDeclaration declaration)
//        {
//            if (IsCurrentDelegate || IsCurrentEnum)
//            {
//                return;
//            }

//            TextWriter output = Output;

//            OutputAttributes(method.CustomAttributes, null, false);

//            OutputAttributes(method.ReturnTypeCustomAttributes,
//                "ret: ", false);

//            MemberAttributes attributes = method.Attributes;

//            if (!IsCurrentInterface)
//            {
//                if (method.PrivateImplementationType == null)
//                {
//                    OutputMemberAccessModifier(attributes);
//                    OutputVTableModifier(attributes);
//                    OutputMemberScopeModifier(attributes);
//                }
//            }
//            else
//            {
//                OutputVTableModifier(attributes);
//            }

//            OutputType(method.ReturnType);
//            output.Write(' ');

//            CodeTypeReference privateType = method.PrivateImplementationType;
//            if (privateType != null)
//            {
//                output.Write(privateType.BaseType);
//                output.Write('.');
//            }
//            output.Write(GetSafeName(method.Name));

//            GenerateGenericsParameters(method.TypeParameters);

//            output.Write('(');
//            OutputParameters(method.Parameters);
//            output.Write(')');

//            GenerateGenericsConstraints(method.TypeParameters);

//            if (IsAbstract(attributes) || declaration.IsInterface)
//                output.WriteLine(';');
//            else
//            {
//                OutputStartBrace();
//                ++Indent;
//                GenerateStatements(method.Statements);
//                --Indent;
//                output.WriteLine('}');
//            }
//        }

//        static bool IsAbstract(MemberAttributes attributes)
//        {
//            return (attributes & MemberAttributes.ScopeMask) == MemberAttributes.Abstract;
//        }

//        protected override void GenerateProperty(CodeMemberProperty property,
//                              CodeTypeDeclaration declaration)
//        {
//            if (IsCurrentDelegate || IsCurrentEnum)
//            {
//                return;
//            }

//            TextWriter output = Output;

//            OutputAttributes(property.CustomAttributes, null, false);

//            MemberAttributes attributes = property.Attributes;

//            if (!IsCurrentInterface)
//            {
//                if (property.PrivateImplementationType == null)
//                {
//                    OutputMemberAccessModifier(attributes);
//                    OutputVTableModifier(attributes);
//                    OutputMemberScopeModifier(attributes);
//                }
//            }
//            else
//            {
//                OutputVTableModifier(attributes);
//            }

//            OutputType(property.Type);
//            output.Write(' ');

//            if (!IsCurrentInterface && property.PrivateImplementationType != null)
//            {
//                output.Write(property.PrivateImplementationType.BaseType);
//                output.Write('.');
//            }

//             only consider property indexer if name is Item (case-insensitive 
//             comparison) AND property has parameters
//            if (string.Compare(property.Name, "Item", true, CultureInfo.InvariantCulture) == 0 && property.Parameters.Count > 0)
//            {
//                output.Write("self[");
//                OutputParameters(property.Parameters);
//                output.Write(']');
//            }
//            else
//            {
//                output.Write(GetSafeName(property.Name));
//            }
//            OutputStartBrace();
//            ++Indent;

//            if (declaration.IsInterface || IsAbstract(property.Attributes))
//            {
//                if (property.HasGet) output.WriteLine("get;");
//                if (property.HasSet) output.WriteLine("put;");
//            }
//            else
//            {
//                if (property.HasGet)
//                {
//                    output.Write("get");
//                    OutputStartBrace();
//                    ++Indent;

//                    GenerateStatements(property.GetStatements);

//                    --Indent;
//                    output.WriteLine('}');
//                }

//                if (property.HasSet)
//                {
//                    output.Write("put");
//                    OutputStartBrace();
//                    ++Indent;

//                    GenerateStatements(property.SetStatements);

//                    --Indent;
//                    output.WriteLine('}');
//                }
//            }

//            --Indent;
//            output.WriteLine('}');
//        }

//        protected override void GenerateConstructor(CodeConstructor constructor, CodeTypeDeclaration declaration)
//        {
//            if (IsCurrentDelegate || IsCurrentEnum || IsCurrentInterface)
//            {
//                return;
//            }

//            OutputAttributes(constructor.CustomAttributes, null, false);

//            OutputMemberAccessModifier(constructor.Attributes);
//            Output.Write(GetSafeName(CurrentTypeName) + "(");
//            OutputParameters(constructor.Parameters);
//            Output.Write(")");
//            if (constructor.BaseConstructorArgs.Count > 0)
//            {
//                Output.WriteLine(" : ");
//                Indent += 2;
//                Output.Write("me(");
//                OutputExpressionList(constructor.BaseConstructorArgs);
//                Output.Write(')');
//                Indent -= 2;
//            }
//            if (constructor.ChainedConstructorArgs.Count > 0)
//            {
//                Output.WriteLine(" : ");
//                Indent += 2;
//                Output.Write("self(");
//                OutputExpressionList(constructor.ChainedConstructorArgs);
//                Output.Write(')');
//                Indent -= 2;
//            }
//            OutputStartBrace();
//            Indent++;
//            GenerateStatements(constructor.Statements);
//            Indent--;
//            Output.WriteLine('}');
//        }

//        protected override void GenerateTypeConstructor(CodeTypeConstructor constructor)
//        {
//            if (IsCurrentDelegate || IsCurrentEnum || IsCurrentInterface)
//            {
//                return;
//            }

//            OutputAttributes(constructor.CustomAttributes, null, false);

//            Output.Write("static " + GetSafeName(CurrentTypeName) + "()");
//            OutputStartBrace();
//            Indent++;
//            GenerateStatements(constructor.Statements);
//            Indent--;
//            Output.WriteLine('}');
//        }

//        protected override void GenerateTypeStart(CodeTypeDeclaration declaration)
//        {
//            TextWriter output = Output;

//            OutputAttributes(declaration.CustomAttributes, null, false);

//            if (!IsCurrentDelegate)
//            {
//                OutputTypeAttributes(declaration);

//                output.Write(GetSafeName(declaration.Name));

//                GenerateGenericsParameters(declaration.TypeParameters);

//                IEnumerator enumerator = declaration.BaseTypes.GetEnumerator();
//                if (enumerator.MoveNext())
//                {
//                    CodeTypeReference type = (CodeTypeReference)enumerator.Current;

//                    output.Write(" : ");
//                    OutputType(type);

//                    while (enumerator.MoveNext())
//                    {
//                        type = (CodeTypeReference)enumerator.Current;

//                        output.Write(", ");
//                        OutputType(type);
//                    }
//                }

//                GenerateGenericsConstraints(declaration.TypeParameters);
//                OutputStartBrace();
//                ++Indent;
//            }
//            else
//            {
//                if ((declaration.TypeAttributes & TypeAttributes.VisibilityMask) == TypeAttributes.Public)
//                {
//                    output.Write("public ");
//                }

//                CodeTypeDelegate delegateDecl = (CodeTypeDelegate)declaration;
//                output.Write("delegate ");
//                OutputType(delegateDecl.ReturnType);
//                output.Write(" ");
//                output.Write(GetSafeName(declaration.Name));
//                output.Write("(");
//                OutputParameters(delegateDecl.Parameters);
//                output.WriteLine(");");
//            }
//        }

//        protected override void GenerateTypeEnd(CodeTypeDeclaration declaration)
//        {
//            if (!IsCurrentDelegate)
//            {
//                --Indent;
//                Output.WriteLine("}");
//            }
//        }

//        protected override void GenerateNamespaceStart(CodeNamespace ns)
//        {
//            TextWriter output = Output;

//            string name = ns.Name;
//            if (name != null && name.Length != 0)
//            {
//                output.Write("program ");
//                output.Write(GetSafeName(name));
//                OutputStartBrace();
//                ++Indent;
//            }
//        }

//        protected override void GenerateNamespaceEnd(CodeNamespace ns)
//        {
//            string name = ns.Name;
//            if (name != null && name.Length != 0)
//            {
//                --Indent;
//                Output.WriteLine("}");
//            }
//        }

//        protected override void GenerateNamespaceImport(CodeNamespaceImport import)
//        {
//            TextWriter output = Output;

//            output.Write("include ");
//            output.Write(GetSafeName(import.Namespace));
//            output.WriteLine(';');
//        }

//        protected override void GenerateAttributeDeclarationsStart(CodeAttributeDeclarationCollection attributes)
//        {
//            Output.Write('[');
//        }

//        protected override void GenerateAttributeDeclarationsEnd(CodeAttributeDeclarationCollection attributes)
//        {
//            Output.Write(']');
//        }

//        private void OutputStartBrace()
//        {
//            if (Options.BracingStyle == "C")
//            {
//                Output.WriteLine("");
//                Output.WriteLine("{");
//            }
//            else
//            {
//                Output.WriteLine(" {");
//            }
//        }

//        private void OutputAttributes(CodeAttributeDeclarationCollection attributes, string prefix, bool inline)
//        {
//            bool params_set = false;

//            foreach (CodeAttributeDeclaration att in attributes)
//            {
//                if (att.Name == "System.ParamArrayAttribute")
//                {
//                    params_set = true;
//                    continue;
//                }

//                GenerateAttributeDeclarationsStart(attributes);
//                if (prefix != null)
//                {
//                    Output.Write(prefix);
//                }
//                OutputAttributeDeclaration(att);
//                GenerateAttributeDeclarationsEnd(attributes);
//                if (inline)
//                {
//                    Output.Write(" ");
//                }
//                else
//                {
//                    Output.WriteLine();
//                }
//            }

//            if (params_set)
//            {
//                if (prefix != null)
//                    Output.Write(prefix);
//                Output.Write("params");
//                if (inline)
//                    Output.Write(" ");
//                else
//                    Output.WriteLine();
//            }
//        }

//        private void OutputAttributeDeclaration(CodeAttributeDeclaration attribute)
//        {
//            Output.Write(attribute.Name.Replace('+', '.'));
//            Output.Write('(');
//            IEnumerator enumerator = attribute.Arguments.GetEnumerator();
//            if (enumerator.MoveNext())
//            {
//                CodeAttributeArgument argument = (CodeAttributeArgument)enumerator.Current;
//                OutputAttributeArgument(argument);

//                while (enumerator.MoveNext())
//                {
//                    Output.Write(", ");
//                    argument = (CodeAttributeArgument)enumerator.Current;
//                    OutputAttributeArgument(argument);
//                }
//            }
//            Output.Write(')');
//        }

//        protected override void OutputType(CodeTypeReference type)
//        {
//            Output.Write(GetTypeOutput(type));
//        }

//        private void OutputVTableModifier(MemberAttributes attributes)
//        {
//            if ((attributes & MemberAttributes.VTableMask) == MemberAttributes.New)
//            {
//                Output.Write("new ");
//            }
//        }

//        protected override void OutputFieldScopeModifier(MemberAttributes attributes)
//        {
//            switch (attributes & MemberAttributes.ScopeMask)
//            {
//                case MemberAttributes.Static:
//                    Output.Write("static ");
//                    break;
//                case MemberAttributes.Const:
//                    Output.Write("constant ");
//                    break;
//            }
//        }


//         Note: this method should in fact be private as in .NET 2.0, the 
//         CSharpCodeGenerator no longer derives from CodeGenerator but we
//         still need to make this change.
//        protected override void OutputMemberAccessModifier(MemberAttributes attributes)
//        {
//            switch (attributes & MemberAttributes.AccessMask)
//            {
//                case MemberAttributes.Assembly:
//                case MemberAttributes.FamilyAndAssembly:
//                    Output.Write("internal ");
//                    break;
//                case MemberAttributes.Family:
//                    Output.Write("protected ");
//                    break;
//                case MemberAttributes.FamilyOrAssembly:
//                    Output.Write("protected internal ");
//                    break;
//                case MemberAttributes.Private:
//                    Output.Write("private ");
//                    break;
//                case MemberAttributes.Public:
//                    Output.Write("public ");
//                    break;
//            }
//        }

//         Note: this method should in fact be private as in .NET 2.0, the 
//         CSharpCodeGenerator no longer derives from CodeGenerator but we
//         still need to make this change.
//        protected override void OutputMemberScopeModifier(MemberAttributes attributes)
//        {
//            switch (attributes & MemberAttributes.ScopeMask)
//            {
//                case MemberAttributes.Abstract:
//                    Output.Write("abstract ");
//                    break;
//                case MemberAttributes.Final:
//                     do nothing
//                    break;
//                case MemberAttributes.Static:
//                    Output.Write("static ");
//                    break;
//                case MemberAttributes.Override:
//                    Output.Write("override ");
//                    break;
//                default:
//                    MemberAttributes access = attributes & MemberAttributes.AccessMask;
//                    if (access == MemberAttributes.Assembly || access == MemberAttributes.Family || access == MemberAttributes.Public)
//                    {
//                        Output.Write("virtual ");
//                    }
//                    break;
//            }
//        }

//        private void OutputTypeAttributes(CodeTypeDeclaration declaration)
//        {
//            TextWriter output = Output;
//            TypeAttributes attributes = declaration.TypeAttributes;

//            switch (attributes & TypeAttributes.VisibilityMask)
//            {
//                case TypeAttributes.Public:
//                case TypeAttributes.NestedPublic:
//                    output.Write("public ");
//                    break;
//                case TypeAttributes.NestedPrivate:
//                    output.Write("private ");
//                    break;
//                case TypeAttributes.NotPublic:
//                case TypeAttributes.NestedFamANDAssem:
//                case TypeAttributes.NestedAssembly:
//                    output.Write("internal ");
//                    break;
//                case TypeAttributes.NestedFamily:
//                    output.Write("protected ");
//                    break;
//                case TypeAttributes.NestedFamORAssem:
//                    output.Write("protected internal ");
//                    break;
//            }

//            if ((declaration.Attributes & MemberAttributes.New) != 0)
//                output.Write("new ");

//            if (declaration.IsStruct)
//            {
//                if (declaration.IsPartial)
//                {
//                    output.Write("shared ");
//                }
//                output.Write("struct ");
//            }
//            else if (declaration.IsEnum)
//            {
//                output.Write("enum ");
//            }
//            else
//            {
//                if ((attributes & TypeAttributes.Interface) != 0)
//                {
//                    if (declaration.IsPartial)
//                    {
//                        output.Write("shared ");
//                    }
//                    output.Write("interface ");
//                }
//                else
//                {
//                    if ((attributes & TypeAttributes.Sealed) != 0)
//                        output.Write("final ");
//                    if ((attributes & TypeAttributes.Abstract) != 0)
//                        output.Write("abstract ");
//                    if (declaration.IsPartial)
//                    {
//                        output.Write("shared ");
//                    }
//                    output.Write("class ");
//                }
//            }
//        }

//        [MonoTODO("Implement missing special characters")]
//        protected override string QuoteSnippetString(string value)
//        {
//             FIXME: this is weird, but works.
//            string output = value.Replace("\\", "\\\\");
//            output = output.Replace("\"", "\\\"");
//            output = output.Replace("\t", "\\t");
//            output = output.Replace("\r", "\\r");
//            output = output.Replace("\n", "\\n");

//            return "\"" + output + "\"";
//        }




//         <devdoc>
//            <para>
//               Outputs an argument in a attribute block.
//            </para>
//         </devdoc>
//        private void OutputAttributeArgument(CodeAttributeArgument arg)
//        {
//            if (arg.Name != null && arg.Name.Length > 0)
//            {
//                OutputIdentifier(arg.Name);
//                Output.Write("=");
//            }
//            ((ICodeGenerator)this).GenerateCodeFromExpression(arg.Value, output.InnerWriter, options);
//        }

//         <devdoc>
//            <para>
//               Generates code for the specified System.CodeDom.FieldDirection.
//            </para>
//         </devdoc>
//        private void OutputDirection(FieldDirection dir)
//        {
//            switch (dir)
//            {
//                case FieldDirection.In:
//                    break;
//                case FieldDirection.Out:
//                    Output.Write("out ");
//                    break;
//                case FieldDirection.Ref:
//                    Output.Write("ref ");
//                    break;
//            }
//        }

//         <devdoc>
//            <para>
//               Generates code for the specified expression list.
//            </para>
//         </devdoc>
//        private void OutputExpressionList(CodeExpressionCollection expressions)
//        {
//            OutputExpressionList(expressions, false /*newlineBetweenItems*/);
//        }

//         <devdoc>
//            <para>
//               Generates code for the specified expression list.
//            </para>
//         </devdoc>
//        private void OutputExpressionList(CodeExpressionCollection expressions, bool newlineBetweenItems)
//        {
//            bool first = true;
//            IEnumerator en = expressions.GetEnumerator();
//            Indent++;
//            while (en.MoveNext())
//            {
//                if (first)
//                {
//                    first = false;
//                }
//                else
//                {
//                    if (newlineBetweenItems)
//                        ContinueOnNewLine(",");
//                    else
//                        Output.Write(", ");
//                }
//                ((ICodeGenerator)this).GenerateCodeFromExpression((CodeExpression)en.Current, output.InnerWriter, options);
//            }
//            Indent--;
//        }

//         <devdoc>
//            <para>
//               Generates code for the specified parameters.
//            </para>
//         </devdoc>
//        private void OutputParameters(CodeParameterDeclarationExpressionCollection parameters)
//        {
//            bool first = true;
//            bool multiline = parameters.Count > ParameterMultilineThreshold;
//            if (multiline)
//            {
//                Indent += 3;
//            }
//            IEnumerator en = parameters.GetEnumerator();
//            while (en.MoveNext())
//            {
//                CodeParameterDeclarationExpression current = (CodeParameterDeclarationExpression)en.Current;
//                if (first)
//                {
//                    first = false;
//                }
//                else
//                {
//                    Output.Write(", ");
//                }
//                if (multiline)
//                {
//                    ContinueOnNewLine("");
//                }
//                GenerateExpression(current);
//            }
//            if (multiline)
//            {
//                Indent -= 3;
//            }
//        }

//         <devdoc>
//            <para>
//               Generates code for the specified object type and name pair.
//            </para>
//         </devdoc>
//        private void OutputTypeNamePair(CodeTypeReference typeRef, string name)
//        {
//            OutputType(typeRef);
//            Output.Write(" ");
//            OutputIdentifier(name);
//        }

//        private void OutputIdentifier(string ident)
//        {
//            Output.Write(CreateEscapedIdentifier(ident));
//        }

       

//         <devdoc>
//            <para>
//               Generates code for the specified operator.
//            </para>
//         </devdoc>
//        private void OutputOperator(CodeBinaryOperatorType op)
//        {
//            switch (op)
//            {
//                case CodeBinaryOperatorType.Add:
//                    Output.Write("+");
//                    break;
//                case CodeBinaryOperatorType.Subtract:
//                    Output.Write("-");
//                    break;
//                case CodeBinaryOperatorType.Multiply:
//                    Output.Write("*");
//                    break;
//                case CodeBinaryOperatorType.Divide:
//                    Output.Write("/");
//                    break;
//                case CodeBinaryOperatorType.Modulus:
//                    Output.Write("%");
//                    break;
//                case CodeBinaryOperatorType.Assign:
//                    Output.Write("=");
//                    break;
//                case CodeBinaryOperatorType.IdentityInequality:
//                    Output.Write("!=");
//                    break;
//                case CodeBinaryOperatorType.IdentityEquality:
//                    Output.Write("==");
//                    break;
//                case CodeBinaryOperatorType.ValueEquality:
//                    Output.Write("==");
//                    break;
//                case CodeBinaryOperatorType.BitwiseOr:
//                    Output.Write("|");
//                    break;
//                case CodeBinaryOperatorType.BitwiseAnd:
//                    Output.Write("&");
//                    break;
//                case CodeBinaryOperatorType.BooleanOr:
//                    Output.Write("||");
//                    break;
//                case CodeBinaryOperatorType.BooleanAnd:
//                    Output.Write("&&");
//                    break;
//                case CodeBinaryOperatorType.LessThan:
//                    Output.Write("<");
//                    break;
//                case CodeBinaryOperatorType.LessThanOrEqual:
//                    Output.Write("<=");
//                    break;
//                case CodeBinaryOperatorType.GreaterThan:
//                    Output.Write(">");
//                    break;
//                case CodeBinaryOperatorType.GreaterThanOrEqual:
//                    Output.Write(">=");
//                    break;
//            }
//        }

    
//        private void OutputTypeParameters(CodeTypeParameterCollection typeParameters)
//        {
//            if (typeParameters.Count == 0)
//            {
//                return;
//            }

//            Output.Write('<');
//            bool first = true;
//            for (int i = 0; i < typeParameters.Count; i++)
//            {
//                if (first)
//                {
//                    first = false;
//                }
//                else
//                {
//                    Output.Write(", ");
//                }

//                if (typeParameters[i].CustomAttributes.Count > 0)
//                {
//                    GenerateAttributes(typeParameters[i].CustomAttributes, null, true);
//                    Output.Write(' ');
//                }

//                Output.Write(typeParameters[i].Name);
//            }

//            Output.Write('>');
//        }

//        private void OutputTypeParameterConstraints(CodeTypeParameterCollection typeParameters)
//        {
//            if (typeParameters.Count == 0)
//            {
//                return;
//            }

//            for (int i = 0; i < typeParameters.Count; i++)
//            {
//                 generating something like: "where KeyType: IComparable, IEnumerable"

//                Output.WriteLine();
//                Indent++;

//                bool first = true;
//                if (typeParameters[i].Constraints.Count > 0)
//                {
//                    foreach (CodeTypeReference typeRef in typeParameters[i].Constraints)
//                    {
//                        if (first)
//                        {
//                            Output.Write("where ");
//                            Output.Write(typeParameters[i].Name);
//                            Output.Write(" : ");
//                            first = false;
//                        }
//                        else
//                        {
//                            Output.Write(", ");
//                        }
//                        OutputType(typeRef);
//                    }
//                }

//                if (typeParameters[i].HasConstructorConstraint)
//                {
//                    if (first)
//                    {
//                        Output.Write("where ");
//                        Output.Write(typeParameters[i].Name);
//                        Output.Write(" : new()");
//                    }
//                    else
//                    {
//                        Output.Write(", new ()");
//                    }
//                }

//                Indent--;
//            }
//        }


      
//        private void GenerateAttributes(CodeAttributeDeclarationCollection attributes)
//        {
//            GenerateAttributes(attributes, null, false);
//        }

//        private void GenerateAttributes(CodeAttributeDeclarationCollection attributes, string prefix)
//        {
//            GenerateAttributes(attributes, prefix, false);
//        }

//        private void GenerateAttributes(CodeAttributeDeclarationCollection attributes, string prefix, bool inLine)
//        {
//            if (attributes.Count == 0) return;
//            IEnumerator en = attributes.GetEnumerator();
//            bool paramArray = false;

//            while (en.MoveNext())
//            {
//                 we need to convert paramArrayAttribute to params keyword to 
//                 make csharp compiler happy. In addition, params keyword needs to be after 
//                 other attributes.

//                CodeAttributeDeclaration current = (CodeAttributeDeclaration)en.Current;

//                if (current.Name.Equals("system.paramarrayattribute", StringComparison.OrdinalIgnoreCase))
//                {
//                    paramArray = true;
//                    continue;
//                }

//                GenerateAttributeDeclarationsStart(attributes);
//                if (prefix != null)
//                {
//                    Output.Write(prefix);
//                }

//                if (current.AttributeType != null)
//                {
//                    Output.Write(GetTypeOutput(current.AttributeType));
//                }
//                Output.Write("(");

//                bool firstArg = true;
//                foreach (CodeAttributeArgument arg in current.Arguments)
//                {
//                    if (firstArg)
//                    {
//                        firstArg = false;
//                    }
//                    else
//                    {
//                        Output.Write(", ");
//                    }

//                    OutputAttributeArgument(arg);
//                }

//                Output.Write(")");
//                GenerateAttributeDeclarationsEnd(attributes);
//                if (inLine)
//                {
//                    Output.Write(" ");
//                }
//                else
//                {
//                    Output.WriteLine();
//                }
//            }

//            if (paramArray)
//            {
//                if (prefix != null)
//                {
//                    Output.Write(prefix);
//                }
//                Output.Write("params");

//                if (inLine)
//                {
//                    Output.Write(" ");
//                }
//                else
//                {
//                    Output.WriteLine();
//                }
//            }


//        }
 

//        protected override void GeneratePrimitiveExpression(CodePrimitiveExpression e)
//        {
//            if (e.Value is char)
//            {
//                this.GenerateCharValue((char)e.Value);
//            }
//            else if (e.Value is ushort)
//            {
//                ushort uc = (ushort)e.Value;
//                Output.Write(uc.ToString(CultureInfo.InvariantCulture));
//            }
//            else if (e.Value is uint)
//            {
//                uint ui = (uint)e.Value;
//                Output.Write(ui.ToString(CultureInfo.InvariantCulture));
//                Output.Write("u");
//            }
//            else if (e.Value is ulong)
//            {
//                ulong ul = (ulong)e.Value;
//                Output.Write(ul.ToString(CultureInfo.InvariantCulture));
//                Output.Write("ul");
//            }
//            else if (e.Value is sbyte)
//            {
//                sbyte sb = (sbyte)e.Value;
//                Output.Write(sb.ToString(CultureInfo.InvariantCulture));
//            }
//            else
//            {
//                base.GeneratePrimitiveExpression(e);
//            }
//        }

//        private void GenerateCharValue(char c)
//        {
//            Output.Write('\'');

//            switch (c)
//            {
//                case '\0':
//                    Output.Write("\\0");
//                    break;
//                case '\t':
//                    Output.Write("\\t");
//                    break;
//                case '\n':
//                    Output.Write("\\n");
//                    break;
//                case '\r':
//                    Output.Write("\\r");
//                    break;
//                case '"':
//                    Output.Write("\\\"");
//                    break;
//                case '\'':
//                    Output.Write("\\'");
//                    break;
//                case '\\':
//                    Output.Write("\\\\");
//                    break;
//                case '\u2028':
//                    Output.Write("\\u");
//                    Output.Write(((int)c).ToString("X4", CultureInfo.InvariantCulture));
//                    break;
//                case '\u2029':
//                    Output.Write("\\u");
//                    Output.Write(((int)c).ToString("X4", CultureInfo.InvariantCulture));
//                    break;
//                default:
//                    Output.Write(c);
//                    break;
//            }

//            Output.Write('\'');
//        }

//        protected override void GenerateSingleFloatValue(float f)
//        {
//            base.GenerateSingleFloatValue(f);
//            base.Output.Write('F');
//        }

//        protected override void GenerateDecimalValue(decimal d)
//        {
//            base.GenerateDecimalValue(d);
//            base.Output.Write('m');
//        }

//        protected override void GenerateParameterDeclarationExpression(CodeParameterDeclarationExpression e)
//        {
//            OutputAttributes(e.CustomAttributes, null, true);
//            OutputDirection(e.Direction);
//            OutputType(e.Type);
//            Output.Write(' ');
//            Output.Write(GetSafeName(e.Name));
//        }

//        protected override void GenerateTypeOfExpression(CodeTypeOfExpression e)
//        {
//            Output.Write("typeof(");
//            OutputType(e.Type);
//            Output.Write(")");
//        }

//        /* 
//         * ICodeGenerator
//         */

//        protected override string CreateEscapedIdentifier(string value)
//        {
//            if (value == null)
//                throw new NullReferenceException("Argument identifier is null.");
//            return GetSafeName(value);
//        }

//        protected override string CreateValidIdentifier(string value)
//        {
//            if (value == null)
//                throw new NullReferenceException();

//            if (keywordsTable == null)
//                FillKeywordTable();

//            if (keywordsTable.Contains(value))
//                return "_" + value;
//            else
//                return value;
//        }

//        protected override string GetTypeOutput(CodeTypeReference type)
//        {
//            if ((type.Options & CodeTypeReferenceOptions.GenericTypeParameter) != 0)
//                return type.BaseType;

//            string typeOutput = null;

//            if (type.ArrayElementType != null)
//            {
//                typeOutput = GetTypeOutput(type.ArrayElementType);
//            }
//            else
//            {
//                typeOutput = DetermineTypeOutput(type);
//            }

//            int rank = type.ArrayRank;
//            if (rank > 0)
//            {
//                typeOutput += '[';
//                for (--rank; rank > 0; --rank)
//                {
//                    typeOutput += ',';
//                }
//                typeOutput += ']';
//            }

//            return typeOutput;
//        }

//        private string DetermineTypeOutput(CodeTypeReference type)
//        {
//            string typeOutput = null;
//            string baseType = type.BaseType;

//            switch (baseType.ToLower(System.Globalization.CultureInfo.InvariantCulture))
//            {
//                case "system.int32":
//                    typeOutput = "integer";
//                    break;
//                case "system.int64":
//                    typeOutput = "longint";
//                    break;
//                case "system.int16":
//                    typeOutput = "shortint";
//                    break;
//                case "system.boolean":
//                    typeOutput = "bool";
//                    break;
//                case "system.char":
//                    typeOutput = "char";
//                    break;
//                case "system.string":
//                    typeOutput = "string";
//                    break;
//                case "system.object":
//                    typeOutput = "object";
//                    break;
//                case "system.void":
//                    typeOutput = "sub";
//                    break;
//                case "system.byte":
//                    typeOutput = "byte";
//                    break;
//                case "system.sbyte":
//                    typeOutput = "sbyte";
//                    break;
//                case "system.decimal":
//                    typeOutput = "decimal";
//                    break;
//                case "system.double":
//                    typeOutput = "real";
//                    break;
//                case "system.single":
//                    typeOutput = "float";
//                    break;
//                case "system.uint16":
//                    typeOutput = "ushortint";
//                    break;
//                case "system.uint32":
//                    typeOutput = "uinteger";
//                    break;
//                case "system.uint64":
//                    typeOutput = "ulongint";
//                    break;
//                default:
//                    StringBuilder sb = new StringBuilder(baseType.Length);
//                    if ((type.Options & CodeTypeReferenceOptions.GlobalReference) != 0)
//                    {
//                        sb.Append("global::");
//                    }

//                    int lastProcessedChar = 0;
//                    for (int i = 0; i < baseType.Length; i++)
//                    {
//                        char currentChar = baseType[i];
//                        if (currentChar != '+' && currentChar != '.')
//                        {
//                            if (currentChar == '`')
//                            {
//                                sb.Append(CreateEscapedIdentifier(baseType.Substring(
//                                    lastProcessedChar, i - lastProcessedChar)));
//                                 skip ` character
//                                i++;
//                                 determine number of type arguments to output
//                                int end = i;
//                                while (end < baseType.Length && Char.IsDigit(baseType[end]))
//                                    end++;
//                                int typeArgCount = Int32.Parse(baseType.Substring(i, end - i));
//                                 output type arguments
//                                OutputTypeArguments(type.TypeArguments, sb, typeArgCount);
//                                 skip type argument indicator
//                                i = end;
//                                 if next character is . or +, then append .
//                                if ((i < baseType.Length) && ((baseType[i] == '+') || (baseType[i] == '.')))
//                                {
//                                    sb.Append('.');
//                                     skip character that we just processed
//                                    i++;
//                                }
//                                 save postion of last processed character
//                                lastProcessedChar = i;
//                            }
//                        }
//                        else
//                        {
//                            sb.Append(CreateEscapedIdentifier(baseType.Substring(
//                                lastProcessedChar, i - lastProcessedChar)));
//                            sb.Append('.');
//                             skip separator
//                            i++;
//                             save postion of last processed character
//                            lastProcessedChar = i;
//                        }
//                    }

//                     add characters that have not yet been processed 
//                    if (lastProcessedChar < baseType.Length)
//                    {
//                        sb.Append(CreateEscapedIdentifier(baseType.Substring(lastProcessedChar)));
//                    }

//                    typeOutput = sb.ToString();
//                    break;
//            }
//            return typeOutput;
//        }

//        static bool is_identifier_start_character(char c)
//        {
//            return (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || c == '_' || c == '@' || Char.IsLetter(c);
//        }

//        static bool is_identifier_part_character(char c)
//        {
//            return (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || c == '_' || (c >= '0' && c <= '9') || Char.IsLetter(c);
//        }

//        protected override bool IsValidIdentifier(string identifier)
//        {
//            if (identifier == null || identifier.Length == 0)
//                return false;

//            if (keywordsTable == null)
//                FillKeywordTable();

//            if (keywordsTable.Contains(identifier))
//                return false;

//            if (!is_identifier_start_character(identifier[0]))
//                return false;

//            for (int i = 1; i < identifier.Length; i++)
//                if (!is_identifier_part_character(identifier[i]))
//                    return false;

//            return true;
//        }

//        protected override bool Supports(GeneratorSupport supports)
//        {
//            return true;
//        }

//        protected override void GenerateDirectives(CodeDirectiveCollection directives)
//        {
//            foreach (CodeDirective d in directives)
//            {
//                if (d is CodeChecksumPragma)
//                {
//                    GenerateCodeChecksumPragma((CodeChecksumPragma)d);
//                    continue;
//                }
//                if (d is CodeRegionDirective)
//                {
//                    GenerateCodeRegionDirective((CodeRegionDirective)d);
//                    continue;
//                }
//                throw new NotImplementedException("Unknown CodeDirective");
//            }
//        }

//        void GenerateCodeChecksumPragma(CodeChecksumPragma pragma)
//        {
//            Output.Write("#pragma checksum ");
//            Output.Write(QuoteSnippetString(pragma.FileName));
//            Output.Write(" \"");
//            Output.Write(pragma.ChecksumAlgorithmId.ToString("B"));
//            Output.Write("\" \"");
//            if (pragma.ChecksumData != null)
//            {
//                foreach (byte b in pragma.ChecksumData)
//                {
//                    Output.Write(b.ToString("X2"));
//                }
//            }
//            Output.WriteLine("\"");
//        }

//        void GenerateCodeRegionDirective(CodeRegionDirective region)
//        {
//            switch (region.RegionMode)
//            {
//                case CodeRegionMode.Start:
//                    Output.Write("#region ");
//                    Output.WriteLine(region.RegionText);
//                    return;
//                case CodeRegionMode.End:
//                    Output.WriteLine("#endregion");
//                    return;
//            }
//        }

//        void GenerateGenericsParameters(CodeTypeParameterCollection parameters)
//        {
//            int count = parameters.Count;
//            if (count == 0)
//                return;

//            Output.Write('<');
//            for (int i = 0; i < count - 1; ++i)
//            {
//                Output.Write(parameters[i].Name);
//                Output.Write(", ");
//            }
//            Output.Write(parameters[count - 1].Name);
//            Output.Write('>');
//        }

//        void GenerateGenericsConstraints(CodeTypeParameterCollection parameters)
//        {
//            int count = parameters.Count;
//            if (count == 0)
//                return;

//            bool indented = false;

//            for (int i = 0; i < count; i++)
//            {
//                CodeTypeParameter p = parameters[i];
//                bool hasConstraints = (p.Constraints.Count != 0);
//                Output.WriteLine();
//                if (!hasConstraints && !p.HasConstructorConstraint)
//                    continue;

//                if (!indented)
//                {
//                    ++Indent;
//                    indented = true;
//                }

//                Output.Write("where ");
//                Output.Write(p.Name);
//                Output.Write(" : ");

//                for (int j = 0; j < p.Constraints.Count; j++)
//                {
//                    if (j > 0)
//                        Output.Write(", ");
//                    OutputType(p.Constraints[j]);
//                }

//                if (p.HasConstructorConstraint)
//                {
//                    if (hasConstraints)
//                        Output.Write(", ");
//                    Output.Write("new");
//                    if (hasConstraints)
//                        Output.Write(" ");
//                    Output.Write("()");
//                }
//            }

//            if (indented)
//                --Indent;
//        }

//        string GetTypeArguments(CodeTypeReferenceCollection collection)
//        {
//            StringBuilder sb = new StringBuilder(" <");
//            foreach (CodeTypeReference r in collection)
//            {
//                sb.Append(GetTypeOutput(r));
//                sb.Append(", ");
//            }
//            sb.Length--;
//            sb[sb.Length - 1] = '>';
//            return sb.ToString();
//        }

//        private void OutputTypeArguments(CodeTypeReferenceCollection typeArguments, StringBuilder sb, int count)
//        {
//            if (count == 0)
//            {
//                return;
//            }
//            else if (typeArguments.Count == 0)
//            {
//                 generic type definition
//                sb.Append("<>");
//                return;
//            }

//            sb.Append('<');

//             write first type argument
//            sb.Append(GetTypeOutput(typeArguments[0]));
//             subsequent type argument are prefixed by ', ' separator
//            for (int i = 1; i < count; i++)
//            {
//                sb.Append(", ");
//                sb.Append(GetTypeOutput(typeArguments[i]));
//            }

//            sb.Append('>');
//        }

//#if false
//        [MonoTODO]
//        public override void ValidateIdentifier (string identifier)
//        {
//        }
//#endif

//        private string GetSafeName(string id)
//        {
//            if (keywordsTable == null)
//            {
//                FillKeywordTable();
//            }
//            if (keywordsTable.Contains(id))
//            {
//                return "@" + id;
//            }
//            else
//            {
//                return id;
//            }
//        }

//        static void FillKeywordTable()
//        {
//            lock (keywords)
//            {
//                if (keywordsTable == null)
//                {
//                    keywordsTable = new Hashtable();
//                    foreach (string keyword in keywords)
//                    {
//                        keywordsTable.Add(keyword, keyword);
//                    }
//                }
//            }
//        }

//        private static Hashtable keywordsTable;
//        private static string[] keywords = new string[] {
//            "abstract","event","new","struct","as","explicit","nothing","match","me","native",
//            "self","false","operator","throw","leave","finally","out","true",
//            "fixed","override","try","val","params","typeof","catch","for",
//            "private","foreach","protected","checked","jump","public",
//            "unchecked","class","if","readonly","unsafe","const","implicit","ref",
//            "persist","in","ret","using","virtual","default",
//            "interface","final","volatile","delegate","internal","do","is",
//            "sizeof","while","block","stackalloc","else","static","enum",
//            "program",
//            "object","bool","byte","float","uinteger","char","ulongint","ushortint",
//            "decimal","integer","sbyte","shortint","real","longint","string","sub",
//            "shared", "yield", "where"
//        };


//        public override void GenerateCodeFromMember(CodeTypeMember member, TextWriter writer, CodeGeneratorOptions options)
//        {
//            if (this.output != null)
//            {
//                throw new InvalidOperationException("Output isn't null");
//            }
//            this.options = (options == null) ? new CodeGeneratorOptions() : options;
//            this.output = new IndentedTextWriter(writer, this.options.IndentString);

//            try
//            {
//                CodeTypeDeclaration dummyClass = new CodeTypeDeclaration();
//                this.currentClass = dummyClass;
//                GenerateTypeMember(member, dummyClass);
//            }
//            finally
//            {
//                this.currentClass = null;
//                this.output = null;
//                this.options = null;
//            }
//        }


//        private void GenerateTypeMember(CodeTypeMember member, CodeTypeDeclaration declaredType)
//        {

//            if (options.BlankLinesBetweenMembers)
//            {
//                Output.WriteLine();
//            }

//            if (member is CodeTypeDeclaration)
//            {
//                ((ICodeGenerator)this).GenerateCodeFromType((CodeTypeDeclaration)member, output.InnerWriter, options);

//                 Nested types clobber the current class, so reset it.
//                currentClass = declaredType;

//                 For nested types, comments and line pragmas are handled separately, so return here
//                return;
//            }

//            if (member.StartDirectives.Count > 0)
//            {
//                GenerateDirectives(member.StartDirectives);
//            }

//            GenerateCommentStatements(member.Comments);

//            if (member.LinePragma != null)
//            {
//                GenerateLinePragmaStart(member.LinePragma);
//            }

//            if (member is CodeMemberField)
//            {
//                GenerateField((CodeMemberField)member);
//            }
//            else if (member is CodeMemberProperty)
//            {
//                GenerateProperty((CodeMemberProperty)member, declaredType);
//            }
//            else if (member is CodeMemberMethod)
//            {
//                if (member is CodeConstructor)
//                {
//                    GenerateConstructor((CodeConstructor)member, declaredType);
//                }
//                else if (member is CodeTypeConstructor)
//                {
//                    GenerateTypeConstructor((CodeTypeConstructor)member);
//                }
//                else if (member is CodeEntryPointMethod)
//                {
//                    GenerateEntryPointMethod((CodeEntryPointMethod)member, declaredType);
//                }
//                else
//                {
//                    GenerateMethod((CodeMemberMethod)member, declaredType);
//                }
//            }
//            else if (member is CodeMemberEvent)
//            {
//                GenerateEvent((CodeMemberEvent)member, declaredType);
//            }
//            else if (member is CodeSnippetTypeMember)
//            {

//                 Don't indent snippets, in order to preserve the column
//                 information from the original code.  This improves the debugging
//                 experience.
//                int savedIndent = Indent;
//                Indent = 0;

//                GenerateSnippetMember((CodeSnippetTypeMember)member);

//                 Restore the indent
//                Indent = savedIndent;

//                 Generate an extra new line at the end of the snippet.
//                 If the snippet is comment and this type only contains comments.
//                 The generated code will not compile. 
//                Output.WriteLine();
//            }

//            if (member.LinePragma != null)
//            {
//                GenerateLinePragmaEnd(member.LinePragma);
//            }

//            if (member.EndDirectives.Count > 0)
//            {
//                GenerateDirectives(member.EndDirectives);
//            }
//        }
 
        
//    }


    internal class AlCodeGenerator : ICodeGenerator
    {
        private IndentedTextWriter output;
        private CodeGeneratorOptions options;
        private CodeTypeDeclaration currentClass;
        private CodeTypeMember currentMember;
        private bool inNestedBinary = false;
        private IDictionary<string, string> provOptions;

        public IDictionary<string, string> ProviderOptions
        {
            get { return provOptions; }
        }
        private const int ParameterMultilineThreshold = 15;
        private const int MaxLineLength = 80;
        private const GeneratorSupport LanguageSupport = GeneratorSupport.ArraysOfArrays |
                                                         GeneratorSupport.EntryPointMethod |
                                                         GeneratorSupport.GotoStatements |
                                                         GeneratorSupport.MultidimensionalArrays |
                                                         GeneratorSupport.StaticConstructors |
                                                         GeneratorSupport.TryCatchStatements |
                                                         GeneratorSupport.ReturnTypeAttributes |
                                                         GeneratorSupport.AssemblyAttributes |
                                                         GeneratorSupport.DeclareValueTypes |
                                                         GeneratorSupport.DeclareEnums |
                                                         GeneratorSupport.DeclareEvents |
                                                         GeneratorSupport.DeclareDelegates |
                                                         GeneratorSupport.DeclareInterfaces |
                                                         GeneratorSupport.ParameterAttributes |
                                                         GeneratorSupport.ReferenceParameters |
                                                         GeneratorSupport.ChainedConstructorArguments |
                                                         GeneratorSupport.NestedTypes |
                                                         GeneratorSupport.MultipleInterfaceMembers |
                                                         GeneratorSupport.PublicStaticMembers |
                                                         GeneratorSupport.ComplexExpressions |
#if !FEATURE_PAL
 GeneratorSupport.Win32Resources |
#endif // !FEATURE_PAL
 GeneratorSupport.Resources |
                                                         GeneratorSupport.PartialTypes |
                                                         GeneratorSupport.GenericTypeReference |
                                                         GeneratorSupport.GenericTypeDeclaration |
                                                         GeneratorSupport.DeclareIndexerProperties;
        private static volatile Regex outputRegWithFileAndLine;
        private static volatile Regex outputRegSimple;

        private static Hashtable keywordsTable;
        private static string[] keywords = new string[] {
			"abstract","event","new","struct","as","explicit","null","caseof","me","native",
			"self","false","operator","throw","leave","finally","out","true",
			"fixed","override","try","val","params","typeof","except","for",
			"private","foreach","protected","checked","jmp","public",
			"unchecked","class","if","readonly","unsafe","const","implicit","ref",
            "safe","loop","restrict",	"persist","in","ret","using","include","virt","default",
			"interface","final","volatile","delegate","internal","do","is",
			"sizeof","while","lock","stackalloc","else","static","enum",
			"program",
			"object","bool","byte","float","uinteger","char","ulongint","ushortint",
			"decimal","integer","sbyte","shortint","real","longint","string","sub","date","time","pointer","upointer", "guid",
			"shared", "yield", "where"
		};


        static void FillKeywordTable()
        {
            lock (keywords)
            {
                if (keywordsTable == null)
                {
                    keywordsTable = new Hashtable();
                    foreach (string keyword in keywords)
                    {
                        keywordsTable.Add(keyword, keyword);
                    }
                }
            }
        }


      
        internal AlCodeGenerator()
        {
            FillKeywordTable();
        }

        internal AlCodeGenerator(IDictionary<string, string> providerOptions)
        {
            provOptions = providerOptions;
            FillKeywordTable();
        }



        private bool generatingForLoop = false;

        /// <devdoc>
        ///    <para>
        ///       Gets
        ///       or sets the file extension to use for source files.
        ///    </para>
        /// </devdoc>
        private string FileExtension { get { return ".al"; } }

        /// <devdoc>
        ///    <para>
        ///       Gets or
        ///       sets the name of the compiler executable.
        ///    </para>
        /// </devdoc>
#if !PLATFORM_UNIX
        private string CompilerName { get { return "alc.exe"; } }
#else // !PLATFORM_UNIX
        private string CompilerName { get { return "csc"; } }
#endif // !PLATFORM_UNIX

        /// <devdoc>
        ///    <para>
        ///       Gets or sets the current class name.
        ///    </para>
        /// </devdoc>
        private string CurrentTypeName
        {
            get
            {
                if (currentClass != null)
                {
                    return currentClass.Name;
                }
                return "<% unknown %>";
            }
        }

        private int Indent
        {
            get
            {
                return output.Indent;
            }
            set
            {
                output.Indent = value;
            }
        }

        /// <devdoc>
        ///    <para>
        ///       Gets or sets a value indicating whether the current object being
        ///       generated is an interface.
        ///    </para>
        /// </devdoc>
        private bool IsCurrentInterface
        {
            get
            {
                if (currentClass != null && !(currentClass is CodeTypeDelegate))
                {
                    return currentClass.IsInterface;
                }
                return false;
            }
        }

        /// <devdoc>
        ///    <para>
        ///       Gets or sets a value indicating whether the current object being generated
        ///       is a class.
        ///    </para>
        /// </devdoc>
        private bool IsCurrentClass
        {
            get
            {
                if (currentClass != null && !(currentClass is CodeTypeDelegate))
                {
                    return currentClass.IsClass;
                }
                return false;
            }
        }

        /// <devdoc>
        ///    <para>
        ///       Gets or sets a value indicating whether the current object being generated
        ///       is a struct.
        ///    </para>
        /// </devdoc>
        private bool IsCurrentStruct
        {
            get
            {
                if (currentClass != null && !(currentClass is CodeTypeDelegate))
                {
                    return currentClass.IsStruct;
                }
                return false;
            }
        }

        /// <devdoc>
        ///    <para>
        ///       Gets or sets a value indicating whether the current object being generated
        ///       is an enumeration.
        ///    </para>
        /// </devdoc>
        private bool IsCurrentEnum
        {
            get
            {
                if (currentClass != null && !(currentClass is CodeTypeDelegate))
                {
                    return currentClass.IsEnum;
                }
                return false;
            }
        }

        /// <devdoc>
        ///    <para>
        ///       Gets or sets a value indicating whether the current object being generated
        ///       is a delegate.
        ///    </para>
        /// </devdoc>
        private bool IsCurrentDelegate
        {
            get
            {
                if (currentClass != null && currentClass is CodeTypeDelegate)
                {
                    return true;
                }
                return false;
            }
        }

        /// <devdoc>
        ///    <para>
        ///       Gets the token used to represent <see langword='null'/>.
        ///    </para>
        /// </devdoc>
        private string NullToken
        {
            get
            {
                return "nothing";
            }
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        private CodeGeneratorOptions Options
        {
            get
            {
                return options;
            }
        }

        private TextWriter Output
        {
            get
            {
                return output;
            }
        }

        /// <devdoc>
        ///    <para>
        ///       Provides conversion to C-style formatting with escape codes.
        ///    </para>
        /// </devdoc>
        private string QuoteSnippetStringCStyle(string value)
        {
            StringBuilder b = new StringBuilder(value.Length + 5);
            //   Indentation indentObj = new Indentation((IndentedTextWriter)Output, Indent + 1);

            b.Append("\"");

            int i = 0;
            while (i < value.Length)
            {
                switch (value[i])
                {
                    case '\r':
                        b.Append("\\r");
                        break;
                    case '\t':
                        b.Append("\\t");
                        break;
                    case '\"':
                        b.Append("\\\"");
                        break;
                    case '\'':
                        b.Append("\\\'");
                        break;
                    case '\\':
                        b.Append("\\\\");
                        break;
                    case '\0':
                        b.Append("\\0");
                        break;
                    case '\n':
                        b.Append("\\n");
                        break;
                    case '\u2028':
                    case '\u2029':
                        AppendEscapedChar(b, value[i]);
                        break;

                    default:
                        b.Append(value[i]);
                        break;
                }

                if (i > 0 && i % MaxLineLength == 0)
                {
                    //
                    // If current character is a high surrogate and the following 
                    // character is a low surrogate, don't break them. 
                    // Otherwise when we write the string to a file, we might lose 
                    // the characters.
                    // 
                    if (Char.IsHighSurrogate(value[i])
                        && (i < value.Length - 1)
                        && Char.IsLowSurrogate(value[i + 1]))
                    {
                        b.Append(value[++i]);
                    }

                    b.Append("\" +");
                    b.Append(Environment.NewLine);
                    //  b.Append(indentObj.IndentationString);
                    b.Append('\"');
                }
                ++i;
            }

            b.Append("\"");

            return b.ToString();
        }

        private string QuoteSnippetStringVerbatimStyle(string value)
        {
            StringBuilder b = new StringBuilder(value.Length + 5);

            b.Append("@\"");

            for (int i = 0; i < value.Length; i++)
            {
                if (value[i] == '\"')
                    b.Append("\"\"");
                else
                    b.Append(value[i]);
            }

            b.Append("\"");

            return b.ToString();
        }

        /// <devdoc>
        ///    <para>
        ///       Provides conversion to formatting with escape codes.
        ///    </para>
        /// </devdoc>
        private string QuoteSnippetString(string value)
        {
            // If the string is short, use C style quoting (e.g "\r\n")
            // Also do it if it is too long to fit in one line
            // If the string contains '\0', verbatim style won't work.
            if (value.Length < 256 || value.Length > 1500 || (value.IndexOf('\0') != -1))
                return QuoteSnippetStringCStyle(value);

            // Otherwise, use 'verbatim' style quoting (e.g. @"foo")
            return QuoteSnippetStringVerbatimStyle(value);
        }

        /// <devdoc>
        ///    <para>
        ///       Processes the <see cref='System.CodeDom.Compiler.CompilerResults'/> returned from compilation.
        ///    </para>
        /// </devdoc>
        private void ProcessCompilerOutputLine(CompilerResults results, string line)
        {
            if (outputRegSimple == null)
            {
                outputRegWithFileAndLine =
                    new Regex(@"(^(.*)(\(([0-9]+),([0-9]+)\)): )(error|warning) ([A-Z]+[0-9]+) ?: (.*)");
                outputRegSimple =
                    new Regex(@"(error|warning) ([A-Z]+[0-9]+) ?: (.*)");
            }

            //First look for full file info
            Match m = outputRegWithFileAndLine.Match(line);
            bool full;
            if (m.Success)
            {
                full = true;
            }
            else
            {
                m = outputRegSimple.Match(line);
                full = false;
            }

            if (m.Success)
            {
                CompilerError ce = new CompilerError();
                if (full)
                {
                    ce.FileName = m.Groups[2].Value;
                    ce.Line = int.Parse(m.Groups[4].Value, CultureInfo.InvariantCulture);
                    ce.Column = int.Parse(m.Groups[5].Value, CultureInfo.InvariantCulture);
                }
                if (string.Compare(m.Groups[full ? 6 : 1].Value, "warning", StringComparison.OrdinalIgnoreCase) == 0)
                {
                    ce.IsWarning = true;
                }
                ce.ErrorNumber = m.Groups[full ? 7 : 2].Value;
                ce.ErrorText = m.Groups[full ? 8 : 3].Value;

                results.Errors.Add(ce);
            }
        }



        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        private void ContinueOnNewLine(string st)
        {
            Output.WriteLine(st);
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        [ResourceExposure(ResourceScope.None)]
        [ResourceConsumption(ResourceScope.Machine, ResourceScope.Machine)]
        private string GetResponseFileCmdArgs(CompilerParameters options, string cmdArgs)
        {

            string responseFileName = options.TempFiles.AddExtension("cmdline");

            Stream temp = new FileStream(responseFileName, FileMode.Create, FileAccess.Write, FileShare.Read);
            try
            {
                using (StreamWriter sw = new StreamWriter(temp, Encoding.UTF8))
                {
                    sw.Write(cmdArgs);
                    sw.Flush();
                }
            }
            finally
            {
                temp.Close();
            }

            // Always specify the /noconfig flag (outside of the response file)
            return "/noconfig /fullpaths @\"" + responseFileName + "\"";
        }

        private void OutputIdentifier(string ident)
        {
            Output.Write(CreateEscapedIdentifier(ident));
        }

        /// <devdoc>
        ///    <para>
        ///       Sets the output type.
        ///    </para>
        /// </devdoc>
        private void OutputType(CodeTypeReference typeRef)
        {
            Output.Write(GetTypeOutput(typeRef));
        }


        private void OutputStartBrace()
        {
            if (Options.BracingStyle == "C")
            {
                Output.WriteLine("");
                Output.WriteLine("{");
            }
            else
            {
                Output.WriteLine(" {");
            }
        }

        private void GenerateArrayCreateExpression(CodeArrayCreateExpression expression)
        {
            //
            // This tries to replicate MS behavior as good as
            // possible.
            //
            // The Code-Array stuff in ms.net seems to be broken
            // anyways, or I'm too stupid to understand it.
            //
            // I'm sick of it. If you try to develop array
            // creations, test them on windows. If it works there
            // but not in mono, drop me a note.  I'd be especially
            // interested in jagged-multidimensional combinations
            // with proper initialization :}
            //

            TextWriter output = Output;

            output.Write("new ");

            CodeExpressionCollection initializers = expression.Initializers;
            CodeTypeReference createType = expression.CreateType;

            if (initializers.Count > 0)
            {

                OutputType(createType);

                if (expression.CreateType.ArrayRank == 0)
                {
                    output.Write("[]");
                }

                OutputStartBrace();
                ++Indent;
                OutputExpressionList(initializers, true);
                --Indent;
                output.Write("}");
            }
            else
            {
                CodeTypeReference arrayType = createType.ArrayElementType;
                while (arrayType != null)
                {
                    createType = arrayType;
                    arrayType = arrayType.ArrayElementType;
                }

                OutputType(createType);

                output.Write('[');

                CodeExpression size = expression.SizeExpression;
                if (size != null)
                    GenerateExpression(size);
                else
                    output.Write(expression.Size);

                output.Write(']');
            }
        }

        /// <devdoc>
        ///    <para>
        ///       Generates
        ///       code for the specified CodeDom based base reference expression
        ///       representation.
        ///    </para>
        /// </devdoc>
        private void GenerateBaseReferenceExpression(CodeBaseReferenceExpression e)
        {
            Output.Write("me");
        }

        /// <devdoc>
        ///    <para>
        ///       Generates code for the specified CodeDom based binary operator
        ///       expression representation.
        ///    </para>
        /// </devdoc>
        private void GenerateBinaryOperatorExpression(CodeBinaryOperatorExpression e)
        {
            bool indentedExpression = false;
            Output.Write("(");

            GenerateExpression(e.Left);
            Output.Write(" ");

            if (e.Left is CodeBinaryOperatorExpression || e.Right is CodeBinaryOperatorExpression)
            {
                // In case the line gets too long with nested binary operators, we need to output them on
                // different lines. However we want to indent them to maintain readability, but this needs
                // to be done only once;
                if (!inNestedBinary)
                {
                    indentedExpression = true;
                    inNestedBinary = true;
                    Indent += 3;
                }
                ContinueOnNewLine("");
            }

            OutputOperator(e.Operator);

            Output.Write(" ");
            GenerateExpression(e.Right);

            Output.Write(")");
            if (indentedExpression)
            {
                Indent -= 3;
                inNestedBinary = false;
            }
        }

        /// <devdoc>
        ///    <para>
        ///       Generates code for the specified CodeDom based cast expression
        ///       representation.
        ///    </para>
        /// </devdoc>
        private void GenerateCastExpression(CodeCastExpression e)
        {
            Output.Write("((");
            OutputType(e.TargetType);
            Output.Write(")(");
            GenerateExpression(e.Expression);
            Output.Write("))");
        }

        public void GenerateCodeFromMember(CodeTypeMember member, TextWriter writer, CodeGeneratorOptions options)
        {
            if (this.output != null)
            {
                throw new InvalidOperationException("Output isn't null");
            }
            this.options = (options == null) ? new CodeGeneratorOptions() : options;
            this.output = new IndentedTextWriter(writer, this.options.IndentString);

            try
            {
                CodeTypeDeclaration dummyClass = new CodeTypeDeclaration();
                this.currentClass = dummyClass;
                GenerateTypeMember(member, dummyClass);
            }
            finally
            {
                this.currentClass = null;
                this.output = null;
                this.options = null;
            }
        }

        private void GenerateDefaultValueExpression(CodeDefaultValueExpression e)
        {
            Output.Write("default(");
            OutputType(e.Type);
            Output.Write(")");
        }

        /// <devdoc>
        ///    <para>
        ///       Generates code for the specified CodeDom based delegate creation
        ///       expression representation.
        ///    </para>
        /// </devdoc>
        private void GenerateDelegateCreateExpression(CodeDelegateCreateExpression e)
        {
            Output.Write("new ");
            OutputType(e.DelegateType);
            Output.Write("(");
            GenerateExpression(e.TargetObject);
            Output.Write(".");
            OutputIdentifier(e.MethodName);
            Output.Write(")");
        }

        private void GenerateEvents(CodeTypeDeclaration e)
        {
            IEnumerator en = e.Members.GetEnumerator();
            while (en.MoveNext())
            {
                if (en.Current is CodeMemberEvent)
                {
                    currentMember = (CodeTypeMember)en.Current;

                    if (options.BlankLinesBetweenMembers)
                    {
                        Output.WriteLine();
                    }
                    if (currentMember.StartDirectives.Count > 0)
                    {
                        GenerateDirectives(currentMember.StartDirectives);
                    }
                    GenerateCommentStatements(currentMember.Comments);
                    CodeMemberEvent imp = (CodeMemberEvent)en.Current;
                    if (imp.LinePragma != null) GenerateLinePragmaStart(imp.LinePragma);
                    GenerateEvent(imp, e);
                    if (imp.LinePragma != null) GenerateLinePragmaEnd(imp.LinePragma);
                    if (currentMember.EndDirectives.Count > 0)
                    {
                        GenerateDirectives(currentMember.EndDirectives);
                    }
                }
            }
        }


        private void GenerateFields(CodeTypeDeclaration e)
        {
            IEnumerator en = e.Members.GetEnumerator();
            while (en.MoveNext())
            {
                if (en.Current is CodeMemberField)
                {
                    currentMember = (CodeTypeMember)en.Current;

                    if (options.BlankLinesBetweenMembers)
                    {
                        Output.WriteLine();
                    }
                    if (currentMember.StartDirectives.Count > 0)
                    {
                        GenerateDirectives(currentMember.StartDirectives);
                    }
                    GenerateCommentStatements(currentMember.Comments);
                    CodeMemberField imp = (CodeMemberField)en.Current;
                    if (imp.LinePragma != null) GenerateLinePragmaStart(imp.LinePragma);
                    GenerateField(imp);
                    if (imp.LinePragma != null) GenerateLinePragmaEnd(imp.LinePragma);
                    if (currentMember.EndDirectives.Count > 0)
                    {
                        GenerateDirectives(currentMember.EndDirectives);
                    }
                }
            }
        }

        /// <devdoc>
        ///    <para>
        ///       Generates code for the specified CodeDom based field reference expression
        ///       representation.
        ///    </para>
        /// </devdoc>
        private void GenerateFieldReferenceExpression(CodeFieldReferenceExpression e)
        {
            if (e.TargetObject != null)
            {
                GenerateExpression(e.TargetObject);
                Output.Write(".");
            }
            OutputIdentifier(e.FieldName);
        }

        private void GenerateArgumentReferenceExpression(CodeArgumentReferenceExpression e)
        {
            OutputIdentifier(e.ParameterName);
        }

        private void GenerateVariableReferenceExpression(CodeVariableReferenceExpression e)
        {
            OutputIdentifier(e.VariableName);
        }

        /// <devdoc>
        ///    <para>
        ///       Generates code for the specified CodeDom based indexer expression
        ///       representation.
        ///    </para>
        /// </devdoc>
        private void GenerateIndexerExpression(CodeIndexerExpression e)
        {
            GenerateExpression(e.TargetObject);
            Output.Write("[");
            bool first = true;
            foreach (CodeExpression exp in e.Indices)
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    Output.Write(", ");
                }
                GenerateExpression(exp);
            }
            Output.Write("]");

        }

        private void GenerateArrayIndexerExpression(CodeArrayIndexerExpression e)
        {
            GenerateExpression(e.TargetObject);
            Output.Write("[");
            bool first = true;
            foreach (CodeExpression exp in e.Indices)
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    Output.Write(", ");
                }
                GenerateExpression(exp);
            }
            Output.Write("]");

        }

        /// <devdoc>
        ///    <para> Generates code for the specified snippet code block
        ///       </para>
        /// </devdoc>
        private void GenerateSnippetCompileUnit(CodeSnippetCompileUnit e)
        {

            GenerateDirectives(e.StartDirectives);

            if (e.LinePragma != null) GenerateLinePragmaStart(e.LinePragma);
            Output.WriteLine(e.Value);
            if (e.LinePragma != null) GenerateLinePragmaEnd(e.LinePragma);

            if (e.EndDirectives.Count > 0)
            {
                GenerateDirectives(e.EndDirectives);
            }
        }

        /// <devdoc>
        ///    <para>
        ///       Generates code for the specified CodeDom based snippet expression
        ///       representation.
        ///    </para>
        /// </devdoc>
        private void GenerateSnippetExpression(CodeSnippetExpression e)
        {
            Output.Write(e.Value);
        }
        /// <devdoc>
        ///    <para>
        ///       Generates code for the specified CodeDom based method invoke expression
        ///       representation.
        ///    </para>
        /// </devdoc>
        private void GenerateMethodInvokeExpression(CodeMethodInvokeExpression e)
        {
            GenerateMethodReferenceExpression(e.Method);
            Output.Write("(");
            OutputExpressionList(e.Parameters);
            Output.Write(")");
        }

        private void GenerateMethodReferenceExpression(CodeMethodReferenceExpression e)
        {
            if (e.TargetObject != null)
            {
                if (e.TargetObject is CodeBinaryOperatorExpression)
                {
                    Output.Write("(");
                    GenerateExpression(e.TargetObject);
                    Output.Write(")");
                }
                else
                {
                    GenerateExpression(e.TargetObject);
                }
                Output.Write(".");
            }
            OutputIdentifier(e.MethodName);

            if (e.TypeArguments.Count > 0)
            {
                Output.Write(GetTypeArgumentsOutput(e.TypeArguments));
            }

        }

        private bool GetUserData(CodeObject e, string property, bool defaultValue)
        {
            object o = e.UserData[property];
            if (o != null && o is bool)
            {
                return (bool)o;
            }
            return defaultValue;
        }

        private void GenerateNamespace(CodeNamespace e)
        {
            GenerateCommentStatements(e.Comments);
            GenerateNamespaceStart(e);

            if (GetUserData(e, "GenerateImports", true))
            {
                GenerateNamespaceImports(e);
            }

            Output.WriteLine("");

            GenerateTypes(e);
            GenerateNamespaceEnd(e);
        }

        /// <devdoc>
        ///    <para>
        ///       Generates code for
        ///       the specified CodeDom based statement representation.
        ///    </para>
        /// </devdoc>
        private void GenerateStatement(CodeStatement e)
        {
            if (e.StartDirectives.Count > 0)
            {
                GenerateDirectives(e.StartDirectives);
            }

            if (e.LinePragma != null)
            {
                GenerateLinePragmaStart(e.LinePragma);
            }

            if (e is CodeCommentStatement)
            {
                GenerateCommentStatement((CodeCommentStatement)e);
            }
            else if (e is CodeMethodReturnStatement)
            {
                GenerateMethodReturnStatement((CodeMethodReturnStatement)e);
            }
            else if (e is CodeConditionStatement)
            {
                GenerateConditionStatement((CodeConditionStatement)e);
            }
            else if (e is CodeTryCatchFinallyStatement)
            {
                GenerateTryCatchFinallyStatement((CodeTryCatchFinallyStatement)e);
            }
            else if (e is CodeAssignStatement)
            {
                GenerateAssignStatement((CodeAssignStatement)e);
            }
            else if (e is CodeExpressionStatement)
            {
                GenerateExpressionStatement((CodeExpressionStatement)e);
            }
            else if (e is CodeIterationStatement)
            {
                GenerateIterationStatement((CodeIterationStatement)e);
            }
            else if (e is CodeThrowExceptionStatement)
            {
                GenerateThrowExceptionStatement((CodeThrowExceptionStatement)e);
            }
            else if (e is CodeSnippetStatement)
            {
                // Don't indent snippet statements, in order to preserve the column
                // information from the original code.  This improves the debugging
                // experience.
                int savedIndent = Indent;
                Indent = 0;

                GenerateSnippetStatement((CodeSnippetStatement)e);

                // Restore the indent
                Indent = savedIndent;
            }
            else if (e is CodeVariableDeclarationStatement)
            {
                GenerateVariableDeclarationStatement((CodeVariableDeclarationStatement)e);
            }
            else if (e is CodeAttachEventStatement)
            {
                GenerateAttachEventStatement((CodeAttachEventStatement)e);
            }
            else if (e is CodeRemoveEventStatement)
            {
                GenerateRemoveEventStatement((CodeRemoveEventStatement)e);
            }
            else if (e is CodeGotoStatement)
            {
                GenerateGotoStatement((CodeGotoStatement)e);
            }
            else if (e is CodeLabeledStatement)
            {
                GenerateLabeledStatement((CodeLabeledStatement)e);
            }
            else
            {
                throw new ArgumentException("Invalid element type");
            }

            if (e.LinePragma != null)
            {
                GenerateLinePragmaEnd(e.LinePragma);
            }
            if (e.EndDirectives.Count > 0)
            {
                GenerateDirectives(e.EndDirectives);
            }
        }

        /// <devdoc>
        ///    <para>
        ///       Generates code for the specified CodeDom based statement representations.
        ///    </para>
        /// </devdoc>
        private void GenerateStatements(CodeStatementCollection stms)
        {
            IEnumerator en = stms.GetEnumerator();
            while (en.MoveNext())
            {
                ((ICodeGenerator)this).GenerateCodeFromStatement((CodeStatement)en.Current, output.InnerWriter, options);
            }
        }

        /// <devdoc>
        ///    <para>
        ///       Generates code for the specified CodeDom based namespace import
        ///       representation.
        ///    </para>
        /// </devdoc>
        private void GenerateNamespaceImports(CodeNamespace e)
        {
            IEnumerator en = e.Imports.GetEnumerator();
            while (en.MoveNext())
            {
                CodeNamespaceImport imp = (CodeNamespaceImport)en.Current;
                if (imp.LinePragma != null) GenerateLinePragmaStart(imp.LinePragma);
                GenerateNamespaceImport(imp);
                if (imp.LinePragma != null) GenerateLinePragmaEnd(imp.LinePragma);
            }
        }

        private void GenerateEventReferenceExpression(CodeEventReferenceExpression e)
        {
            if (e.TargetObject != null)
            {
                GenerateExpression(e.TargetObject);
                Output.Write(".");
            }
            OutputIdentifier(e.EventName);
        }

        /// <devdoc>
        ///    <para>
        ///       Generates code for the specified CodeDom based delegate invoke
        ///       expression representation.
        ///    </para>
        /// </devdoc>
        private void GenerateDelegateInvokeExpression(CodeDelegateInvokeExpression e)
        {
            if (e.TargetObject != null)
            {
                GenerateExpression(e.TargetObject);
            }
            Output.Write("(");
            OutputExpressionList(e.Parameters);
            Output.Write(")");
        }
        /// <devdoc>
        ///    <para>
        ///       Generates code for the specified CodeDom based object creation expression
        ///       representation.
        ///    </para>
        /// </devdoc>
        private void GenerateObjectCreateExpression(CodeObjectCreateExpression e)
        {
            Output.Write("new ");
            OutputType(e.CreateType);
            Output.Write("(");
            OutputExpressionList(e.Parameters);
            Output.Write(")");
        }

        /// <devdoc>
        ///    <para>
        ///       Generates code for the specified CodeDom based primitive expression
        ///       representation.
        ///    </para>
        /// </devdoc>
        private void GeneratePrimitiveExpression(CodePrimitiveExpression e)
        {
            if (e.Value is char)
            {
                GeneratePrimitiveChar((char)e.Value);
            }
            else if (e.Value is SByte)
            {
                // C# has no literal marker for types smaller than Int32                
                Output.Write(((SByte)e.Value).ToString(CultureInfo.InvariantCulture));
            }
            else if (e.Value is UInt16)
            {
                // C# has no literal marker for types smaller than Int32, and you will
                // get a conversion error if you use "u" here.
                Output.Write(((UInt16)e.Value).ToString(CultureInfo.InvariantCulture));
            }
            else if (e.Value is UInt32)
            {
                Output.Write(((UInt32)e.Value).ToString(CultureInfo.InvariantCulture));
                Output.Write("u");
            }
            else if (e.Value is UInt64)
            {
                Output.Write(((UInt64)e.Value).ToString(CultureInfo.InvariantCulture));
                Output.Write("ul");
            }
            else
            {
                GeneratePrimitiveExpressionBase(e);
            }
        }

        /// <devdoc>
        ///    <para>
        ///       Generates code for the specified CodeDom based primitive expression
        ///       representation.
        ///    </para>
        /// </devdoc>
        private void GeneratePrimitiveExpressionBase(CodePrimitiveExpression e)
        {
            if (e.Value == null)
            {
                Output.Write(NullToken);
            }
            else if (e.Value is string)
            {
                Output.Write(QuoteSnippetString((string)e.Value));
            }
            else if (e.Value is char)
            {
                Output.Write("'" + e.Value.ToString() + "'");
            }
            else if (e.Value is byte)
            {
                Output.Write(((byte)e.Value).ToString(CultureInfo.InvariantCulture));
            }
            else if (e.Value is Int16)
            {
                Output.Write(((Int16)e.Value).ToString(CultureInfo.InvariantCulture));
            }
            else if (e.Value is Int32)
            {
                Output.Write(((Int32)e.Value).ToString(CultureInfo.InvariantCulture));
            }
            else if (e.Value is Int64)
            {
                Output.Write(((Int64)e.Value).ToString(CultureInfo.InvariantCulture));
            }
            else if (e.Value is Single)
            {
                GenerateSingleFloatValue((Single)e.Value);
            }
            else if (e.Value is Double)
            {
                GenerateDoubleValue((Double)e.Value);
            }
            else if (e.Value is Decimal)
            {
                GenerateDecimalValue((Decimal)e.Value);
            }
            else if (e.Value is bool)
            {
                if ((bool)e.Value)
                {
                    Output.Write("true");
                }
                else
                {
                    Output.Write("false");
                }
            }
            else
            {
                throw new ArgumentException("Invalid primitive");
            }
        }

        private void GeneratePrimitiveChar(char c)
        {
            Output.Write('\'');
            switch (c)
            {
                case '\r':
                    Output.Write("\\r");
                    break;
                case '\t':
                    Output.Write("\\t");
                    break;
                case '\"':
                    Output.Write("\\\"");
                    break;
                case '\'':
                    Output.Write("\\\'");
                    break;
                case '\\':
                    Output.Write("\\\\");
                    break;
                case '\0':
                    Output.Write("\\0");
                    break;
                case '\n':
                    Output.Write("\\n");
                    break;
                case '\u2028':
                case '\u2029':
                case '\u0084':
                case '\u0085':
                    AppendEscapedChar(null, c);
                    break;

                default:
                    if (Char.IsSurrogate(c))
                    {
                        AppendEscapedChar(null, c);
                    }
                    else
                    {
                        Output.Write(c);
                    }
                    break;
            }
            Output.Write('\'');
        }

        private void AppendEscapedChar(StringBuilder b, char value)
        {
            if (b == null)
            {
                Output.Write("\\u");
                Output.Write(((int)value).ToString("X4", CultureInfo.InvariantCulture));
            }
            else
            {
                b.Append("\\u");
                b.Append(((int)value).ToString("X4", CultureInfo.InvariantCulture));
            }
        }

        private void GeneratePropertySetValueReferenceExpression(CodePropertySetValueReferenceExpression e)
        {
            Output.Write("value");
        }

        /// <devdoc>
        ///    <para>
        ///       Generates code for the specified CodeDom based this reference expression
        ///       representation.
        ///    </para>
        /// </devdoc>
        private void GenerateThisReferenceExpression(CodeThisReferenceExpression e)
        {
            Output.Write("self");
        }

        /// <devdoc>
        ///    <para>
        ///       Generates code for the specified CodeDom based method invoke statement
        ///       representation.
        ///    </para>
        /// </devdoc>
        private void GenerateExpressionStatement(CodeExpressionStatement e)
        {
            GenerateExpression(e.Expression);
            if (!generatingForLoop)
            {
                Output.WriteLine(";");
            }
        }

        /// <devdoc>
        ///    <para>
        ///       Generates code for the specified CodeDom based for loop statement
        ///       representation.
        ///    </para>
        /// </devdoc>
        private void GenerateIterationStatement(CodeIterationStatement e)
        {
            generatingForLoop = true;
            Output.Write("for (");
            GenerateStatement(e.InitStatement);
            Output.Write("; ");
            GenerateExpression(e.TestExpression);
            Output.Write("; ");
            GenerateStatement(e.IncrementStatement);
            Output.Write(")");
            OutputStartingBrace();
            generatingForLoop = false;
            Indent++;
            GenerateStatements(e.Statements);
            Indent--;
            Output.WriteLine("}");
        }
        /// <devdoc>
        ///    <para>
        ///       Generates code for the specified CodeDom based throw exception statement
        ///       representation.
        ///    </para>
        /// </devdoc>
        private void GenerateThrowExceptionStatement(CodeThrowExceptionStatement e)
        {
            Output.Write("throw");
            if (e.ToThrow != null)
            {
                Output.Write(" ");
                GenerateExpression(e.ToThrow);
            }
            Output.WriteLine(";");
        }

        private void GenerateComment(CodeComment comment)
        {
            TextWriter output = Output;

            string commentChars = null;

            if (comment.DocComment)
            {
                commentChars = "///";
            }
            else
            {
                commentChars = "//";
            }

            output.Write(commentChars);
            output.Write(' ');
            string text = comment.Text;

            for (int i = 0; i < text.Length; i++)
            {
                output.Write(text[i]);
                if (text[i] == '\r')
                {
                    if (i < (text.Length - 1) && text[i + 1] == '\n')
                    {
                        continue;
                    }
                    output.Write(commentChars);
                }
                else if (text[i] == '\n')
                {
                    output.Write(commentChars);
                }
            }

            output.WriteLine();
        }

        /// <devdoc>
        ///    <para>
        ///       Generates code for the specified CodeDom based comment statement
        ///       representation.
        ///    </para>
        /// </devdoc>
        private void GenerateCommentStatement(CodeCommentStatement e)
        {
            if (e.Comment == null)
                throw new ArgumentException("null comment");
            GenerateComment(e.Comment);
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        private void GenerateCommentStatements(CodeCommentStatementCollection e)
        {
            foreach (CodeCommentStatement comment in e)
            {
                GenerateCommentStatement(comment);
            }
        }

        /// <devdoc>
        ///    <para>
        ///       Generates code for the specified CodeDom based method return statement
        ///       representation.
        ///    </para>
        /// </devdoc>
        private void GenerateMethodReturnStatement(CodeMethodReturnStatement e)
        {
            Output.Write("ret");
            if (e.Expression != null)
            {
                Output.Write(" ");
                GenerateExpression(e.Expression);
            }
            Output.WriteLine(";");
        }
        /// <devdoc>
        ///    <para>
        ///       Generates code for the specified CodeDom based if statement
        ///       representation.
        ///    </para>
        /// </devdoc>
        private void GenerateConditionStatement(CodeConditionStatement e)
        {
            Output.Write("if (");
            GenerateExpression(e.Condition);
            Output.Write(")");
            OutputStartingBrace();
            Indent++;
            GenerateStatements(e.TrueStatements);
            Indent--;

            CodeStatementCollection falseStatemetns = e.FalseStatements;
            if (falseStatemetns.Count > 0)
            {
                Output.Write("}");
                if (Options.ElseOnClosing)
                {
                    Output.Write(" ");
                }
                else
                {
                    Output.WriteLine("");
                }
                Output.Write("else");
                OutputStartingBrace();
                Indent++;
                GenerateStatements(e.FalseStatements);
                Indent--;
            }
            Output.WriteLine("}");
        }
        /// <devdoc>
        ///    <para>
        ///       Generates code for the specified CodeDom based try catch finally
        ///       statement representation.
        ///    </para>
        /// </devdoc>
        private void GenerateTryCatchFinallyStatement(CodeTryCatchFinallyStatement e)
        {
            Output.Write("try");
            OutputStartingBrace();
            Indent++;
            GenerateStatements(e.TryStatements);
            Indent--;
            CodeCatchClauseCollection catches = e.CatchClauses;
            if (catches.Count > 0)
            {
                IEnumerator en = catches.GetEnumerator();
                while (en.MoveNext())
                {
                    Output.Write("}");
                    if (Options.ElseOnClosing)
                    {
                        Output.Write(" ");
                    }
                    else
                    {
                        Output.WriteLine("");
                    }
                    CodeCatchClause current = (CodeCatchClause)en.Current;
                    Output.Write("catch (");
                    OutputType(current.CatchExceptionType);
                    Output.Write(" ");
                    OutputIdentifier(current.LocalName);
                    Output.Write(")");
                    OutputStartingBrace();
                    Indent++;
                    GenerateStatements(current.Statements);
                    Indent--;
                }
            }

            CodeStatementCollection finallyStatements = e.FinallyStatements;
            if (finallyStatements.Count > 0)
            {
                Output.Write("}");
                if (Options.ElseOnClosing)
                {
                    Output.Write(" ");
                }
                else
                {
                    Output.WriteLine("");
                }
                Output.Write("finally");
                OutputStartingBrace();
                Indent++;
                GenerateStatements(finallyStatements);
                Indent--;
            }
            Output.WriteLine("}");
        }
        /// <devdoc>
        ///    <para>
        ///       Generates code for the specified CodeDom based assignment statement
        ///       representation.
        ///    </para>
        /// </devdoc>
        private void GenerateAssignStatement(CodeAssignStatement e)
        {
            GenerateExpression(e.Left);
            Output.Write(" = ");
            GenerateExpression(e.Right);
            if (!generatingForLoop)
            {
                Output.WriteLine(";");
            }
        }

        /// <devdoc>
        ///    <para>
        ///       Generates code for the specified CodeDom based attach event statement
        ///       representation.
        ///    </para>
        /// </devdoc>
        private void GenerateAttachEventStatement(CodeAttachEventStatement e)
        {
            GenerateEventReferenceExpression(e.Event);
            Output.Write(" += ");
            GenerateExpression(e.Listener);
            Output.WriteLine(";");
        }

        /// <devdoc>
        ///    <para>
        ///       Generates code for the specified CodeDom based detach event statement
        ///       representation.
        ///    </para>
        /// </devdoc>
        private void GenerateRemoveEventStatement(CodeRemoveEventStatement e)
        {
            GenerateEventReferenceExpression(e.Event);
            Output.Write(" -= ");
            GenerateExpression(e.Listener);
            Output.WriteLine(";");
        }

        private void GenerateSnippetStatement(CodeSnippetStatement e)
        {
            Output.WriteLine(e.Value);
        }

        private void GenerateGotoStatement(CodeGotoStatement e)
        {
            Output.Write("jump ");
            Output.Write(e.Label);
            Output.WriteLine(";");
        }

        private void GenerateLabeledStatement(CodeLabeledStatement e)
        {
            Indent--;
            Output.Write(e.Label);
            Output.WriteLine(":");
            Indent++;
            if (e.Statement != null)
            {
                GenerateStatement(e.Statement);
            }
        }

        /// <devdoc>
        ///    <para>
        ///       Generates code for the specified CodeDom based variable declaration
        ///       statement representation.
        ///    </para>
        /// </devdoc>
        private void GenerateVariableDeclarationStatement(CodeVariableDeclarationStatement e)
        {
            OutputTypeNamePair(e.Type, e.Name);
            if (e.InitExpression != null)
            {
                Output.Write(" = ");
                GenerateExpression(e.InitExpression);
            }
            if (!generatingForLoop)
            {
                Output.WriteLine(";");
            }
        }
        /// <devdoc>
        ///    <para>
        ///       Generates code for the specified CodeDom based line pragma start
        ///       representation.
        ///    </para>
        /// </devdoc>
        private void GenerateLinePragmaStart(CodeLinePragma e)
        {
            Output.WriteLine("");
            Output.Write("#line ");
            Output.Write(e.LineNumber);
            Output.Write(" \"");
            Output.Write(e.FileName);
            Output.Write("\"");
            Output.WriteLine("");
        }
        /// <devdoc>
        ///    <para>
        ///       Generates code for the specified CodeDom based line pragma end
        ///       representation.
        ///    </para>
        /// </devdoc>
        private void GenerateLinePragmaEnd(CodeLinePragma e)
        {
            Output.WriteLine();
            Output.WriteLine("#line default");
            Output.WriteLine("#line hidden");
        }

        private void GenerateEvent(CodeMemberEvent e, CodeTypeDeclaration c)
        {
            if (IsCurrentDelegate || IsCurrentEnum) return;

            if (e.CustomAttributes.Count > 0)
            {
                GenerateAttributes(e.CustomAttributes);
            }

            if (e.PrivateImplementationType == null)
            {
                OutputMemberAccessModifier(e.Attributes);
            }
            Output.Write("event ");
            string name = e.Name;
            if (e.PrivateImplementationType != null)
            {
                name = GetBaseTypeOutput(e.PrivateImplementationType) + "." + name;
            }
            OutputTypeNamePair(e.Type, name);
            Output.WriteLine(";");
        }

        /// <devdoc>
        ///    <para>Generates code for the specified CodeDom code expression representation.</para>
        /// </devdoc>
        private void GenerateExpression(CodeExpression e)
        {
            if (e is CodeArrayCreateExpression)
            {
                GenerateArrayCreateExpression((CodeArrayCreateExpression)e);
            }
            else if (e is CodeBaseReferenceExpression)
            {
                GenerateBaseReferenceExpression((CodeBaseReferenceExpression)e);
            }
            else if (e is CodeBinaryOperatorExpression)
            {
                GenerateBinaryOperatorExpression((CodeBinaryOperatorExpression)e);
            }
            else if (e is CodeCastExpression)
            {
                GenerateCastExpression((CodeCastExpression)e);
            }
            else if (e is CodeDelegateCreateExpression)
            {
                GenerateDelegateCreateExpression((CodeDelegateCreateExpression)e);
            }
            else if (e is CodeFieldReferenceExpression)
            {
                GenerateFieldReferenceExpression((CodeFieldReferenceExpression)e);
            }
            else if (e is CodeArgumentReferenceExpression)
            {
                GenerateArgumentReferenceExpression((CodeArgumentReferenceExpression)e);
            }
            else if (e is CodeVariableReferenceExpression)
            {
                GenerateVariableReferenceExpression((CodeVariableReferenceExpression)e);
            }
            else if (e is CodeIndexerExpression)
            {
                GenerateIndexerExpression((CodeIndexerExpression)e);
            }
            else if (e is CodeArrayIndexerExpression)
            {
                GenerateArrayIndexerExpression((CodeArrayIndexerExpression)e);
            }
            else if (e is CodeSnippetExpression)
            {
                GenerateSnippetExpression((CodeSnippetExpression)e);
            }
            else if (e is CodeMethodInvokeExpression)
            {
                GenerateMethodInvokeExpression((CodeMethodInvokeExpression)e);
            }
            else if (e is CodeMethodReferenceExpression)
            {
                GenerateMethodReferenceExpression((CodeMethodReferenceExpression)e);
            }
            else if (e is CodeEventReferenceExpression)
            {
                GenerateEventReferenceExpression((CodeEventReferenceExpression)e);
            }
            else if (e is CodeDelegateInvokeExpression)
            {
                GenerateDelegateInvokeExpression((CodeDelegateInvokeExpression)e);
            }
            else if (e is CodeObjectCreateExpression)
            {
                GenerateObjectCreateExpression((CodeObjectCreateExpression)e);
            }
            else if (e is CodeParameterDeclarationExpression)
            {
                GenerateParameterDeclarationExpression((CodeParameterDeclarationExpression)e);
            }
            else if (e is CodeDirectionExpression)
            {
                GenerateDirectionExpression((CodeDirectionExpression)e);
            }
            else if (e is CodePrimitiveExpression)
            {
                GeneratePrimitiveExpression((CodePrimitiveExpression)e);
            }
            else if (e is CodePropertyReferenceExpression)
            {
                GeneratePropertyReferenceExpression((CodePropertyReferenceExpression)e);
            }
            else if (e is CodePropertySetValueReferenceExpression)
            {
                GeneratePropertySetValueReferenceExpression((CodePropertySetValueReferenceExpression)e);
            }
            else if (e is CodeThisReferenceExpression)
            {
                GenerateThisReferenceExpression((CodeThisReferenceExpression)e);
            }
            else if (e is CodeTypeReferenceExpression)
            {
                GenerateTypeReferenceExpression((CodeTypeReferenceExpression)e);
            }
            else if (e is CodeTypeOfExpression)
            {
                GenerateTypeOfExpression((CodeTypeOfExpression)e);
            }
            else if (e is CodeDefaultValueExpression)
            {
                GenerateDefaultValueExpression((CodeDefaultValueExpression)e);
            }
            else
            {
                if (e == null)
                {
                    throw new ArgumentNullException("e");
                }
                else
                {
                    throw new ArgumentException("Invalid element type");
                }
            }
        }

        /// <devdoc>
        ///    <para>
        ///       Generates code for the specified CodeDom
        ///       based field representation.
        ///    </para>
        /// </devdoc>
        private void GenerateField(CodeMemberField e)
        {
            if (IsCurrentDelegate || IsCurrentInterface) return;

            if (IsCurrentEnum)
            {
                if (e.CustomAttributes.Count > 0)
                {
                    GenerateAttributes(e.CustomAttributes);
                }
                OutputIdentifier(e.Name);
                if (e.InitExpression != null)
                {
                    Output.Write(" = ");
                    GenerateExpression(e.InitExpression);
                }
                Output.WriteLine(",");
            }
            else
            {
                if (e.CustomAttributes.Count > 0)
                {
                    GenerateAttributes(e.CustomAttributes);
                }

                OutputMemberAccessModifier(e.Attributes);
                OutputVTableModifier(e.Attributes);
                OutputFieldScopeModifier(e.Attributes);
                OutputTypeNamePair(e.Type, e.Name);
                if (e.InitExpression != null)
                {
                    Output.Write(" = ");
                    GenerateExpression(e.InitExpression);
                }
                Output.WriteLine(";");
            }
        }
        /// <devdoc>
        ///    <para>
        ///       Generates code for the specified CodeDom based snippet class member
        ///       representation.
        ///    </para>
        /// </devdoc>
        private void GenerateSnippetMember(CodeSnippetTypeMember e)
        {
            Output.Write(e.Text);
        }

        private void GenerateParameterDeclarationExpression(CodeParameterDeclarationExpression e)
        {
            if (e.CustomAttributes.Count > 0)
            {
                // Parameter attributes should be in-line for readability
                GenerateAttributes(e.CustomAttributes, null, true);
            }

            OutputDirection(e.Direction);
            OutputTypeNamePair(e.Type, e.Name);
        }

        private void GenerateEntryPointMethod(CodeEntryPointMethod e, CodeTypeDeclaration c)
        {

            if (e.CustomAttributes.Count > 0)
            {
                GenerateAttributes(e.CustomAttributes);
            }
            Output.Write("public static ");
            OutputType(e.ReturnType);
            Output.Write(" Main()");
            OutputStartingBrace();
            Indent++;

            GenerateStatements(e.Statements);

            Indent--;
            Output.WriteLine("}");
        }

        private void GenerateMethods(CodeTypeDeclaration e)
        {
            IEnumerator en = e.Members.GetEnumerator();
            while (en.MoveNext())
            {
                if (en.Current is CodeMemberMethod
                    && !(en.Current is CodeTypeConstructor)
                    && !(en.Current is CodeConstructor))
                {
                    currentMember = (CodeTypeMember)en.Current;

                    if (options.BlankLinesBetweenMembers)
                    {
                        Output.WriteLine();
                    }
                    if (currentMember.StartDirectives.Count > 0)
                    {
                        GenerateDirectives(currentMember.StartDirectives);
                    }
                    GenerateCommentStatements(currentMember.Comments);
                    CodeMemberMethod imp = (CodeMemberMethod)en.Current;
                    if (imp.LinePragma != null) GenerateLinePragmaStart(imp.LinePragma);
                    if (en.Current is CodeEntryPointMethod)
                    {
                        GenerateEntryPointMethod((CodeEntryPointMethod)en.Current, e);
                    }
                    else
                    {
                        GenerateMethod(imp, e);
                    }
                    if (imp.LinePragma != null) GenerateLinePragmaEnd(imp.LinePragma);
                    if (currentMember.EndDirectives.Count > 0)
                    {
                        GenerateDirectives(currentMember.EndDirectives);
                    }
                }
            }
        }

        /// <devdoc>
        ///    <para>
        ///       Generates code for the specified CodeDom based member method
        ///       representation.
        ///    </para>
        /// </devdoc>
        private void GenerateMethod(CodeMemberMethod e, CodeTypeDeclaration c)
        {
            if (!(IsCurrentClass || IsCurrentStruct || IsCurrentInterface)) return;

            if (e.CustomAttributes.Count > 0)
            {
                GenerateAttributes(e.CustomAttributes);
            }
            if (e.ReturnTypeCustomAttributes.Count > 0)
            {
                GenerateAttributes(e.ReturnTypeCustomAttributes, "ret: ");
            }

            if (!IsCurrentInterface)
            {
                if (e.PrivateImplementationType == null)
                {
                    OutputMemberAccessModifier(e.Attributes);
                    OutputVTableModifier(e.Attributes);
                    OutputMemberScopeModifier(e.Attributes);
                }
            }
            else
            {
                // interfaces still need "new"
                OutputVTableModifier(e.Attributes);
            }
            OutputType(e.ReturnType);
            Output.Write(" ");
            if (e.PrivateImplementationType != null)
            {
                Output.Write(GetBaseTypeOutput(e.PrivateImplementationType));
                Output.Write(".");
            }
            OutputIdentifier(e.Name);

            OutputTypeParameters(e.TypeParameters);

            Output.Write("(");
            OutputParameters(e.Parameters);
            Output.Write(")");

            OutputTypeParameterConstraints(e.TypeParameters);

            if (!IsCurrentInterface
                && (e.Attributes & MemberAttributes.ScopeMask) != MemberAttributes.Abstract)
            {

                OutputStartingBrace();
                Indent++;

                GenerateStatements(e.Statements);

                Indent--;
                Output.WriteLine("}");
            }
            else
            {
                Output.WriteLine(";");
            }
        }

        private void GenerateProperties(CodeTypeDeclaration e)
        {
            IEnumerator en = e.Members.GetEnumerator();
            while (en.MoveNext())
            {
                if (en.Current is CodeMemberProperty)
                {
                    currentMember = (CodeTypeMember)en.Current;

                    if (options.BlankLinesBetweenMembers)
                    {
                        Output.WriteLine();
                    }
                    if (currentMember.StartDirectives.Count > 0)
                    {
                        GenerateDirectives(currentMember.StartDirectives);
                    }
                    GenerateCommentStatements(currentMember.Comments);
                    CodeMemberProperty imp = (CodeMemberProperty)en.Current;
                    if (imp.LinePragma != null) GenerateLinePragmaStart(imp.LinePragma);
                    GenerateProperty(imp, e);
                    if (imp.LinePragma != null) GenerateLinePragmaEnd(imp.LinePragma);
                    if (currentMember.EndDirectives.Count > 0)
                    {
                        GenerateDirectives(currentMember.EndDirectives);
                    }
                }
            }
        }

        /// <devdoc>
        ///    <para>
        ///       Generates code for the specified CodeDom based property representation.
        ///    </para>
        /// </devdoc>
        private void GenerateProperty(CodeMemberProperty e, CodeTypeDeclaration c)
        {
            if (!(IsCurrentClass || IsCurrentStruct || IsCurrentInterface)) return;

            if (e.CustomAttributes.Count > 0)
            {
                GenerateAttributes(e.CustomAttributes);
            }

            if (!IsCurrentInterface)
            {
                if (e.PrivateImplementationType == null)
                {
                    OutputMemberAccessModifier(e.Attributes);
                    OutputVTableModifier(e.Attributes);
                    OutputMemberScopeModifier(e.Attributes);
                }
            }
            else
            {
                OutputVTableModifier(e.Attributes);
            }
            OutputType(e.Type);
            Output.Write(" ");

            if (e.PrivateImplementationType != null && !IsCurrentInterface)
            {
                Output.Write(GetBaseTypeOutput(e.PrivateImplementationType));
                Output.Write(".");
            }

            if (e.Parameters.Count > 0 && String.Compare(e.Name, "Item", StringComparison.OrdinalIgnoreCase) == 0)
            {
                Output.Write("this[");
                OutputParameters(e.Parameters);
                Output.Write("]");
            }
            else
            {
                OutputIdentifier(e.Name);
            }

            OutputStartingBrace();
            Indent++;

            if (e.HasGet)
            {
                if (IsCurrentInterface || (e.Attributes & MemberAttributes.ScopeMask) == MemberAttributes.Abstract)
                {
                    Output.WriteLine("get;");
                }
                else
                {
                    Output.Write("get");
                    OutputStartingBrace();
                    Indent++;
                    GenerateStatements(e.GetStatements);
                    Indent--;
                    Output.WriteLine("}");
                }
            }
            if (e.HasSet)
            {
                if (IsCurrentInterface || (e.Attributes & MemberAttributes.ScopeMask) == MemberAttributes.Abstract)
                {
                    Output.WriteLine("put;");
                }
                else
                {
                    Output.Write("put");
                    OutputStartingBrace();
                    Indent++;
                    GenerateStatements(e.SetStatements);
                    Indent--;
                    Output.WriteLine("}");
                }
            }

            Indent--;
            Output.WriteLine("}");
        }

        private void GenerateSingleFloatValue(Single s)
        {
            if (float.IsNaN(s))
            {
                Output.Write("float.NaN");
            }
            else if (float.IsNegativeInfinity(s))
            {
                Output.Write("float.NegativeInfinity");
            }
            else if (float.IsPositiveInfinity(s))
            {
                Output.Write("float.PositiveInfinity");
            }
            else
            {
                Output.Write(s.ToString(CultureInfo.InvariantCulture));
                Output.Write('F');
            }
        }

        private void GenerateDoubleValue(double d)
        {
            if (double.IsNaN(d))
            {
                Output.Write("real.NaN");
            }
            else if (double.IsNegativeInfinity(d))
            {
                Output.Write("real.NegativeInfinity");
            }
            else if (double.IsPositiveInfinity(d))
            {
                Output.Write("real.PositiveInfinity");
            }
            else
            {
                Output.Write(d.ToString("R", CultureInfo.InvariantCulture));
                // always mark a double as being a double in case we have no decimal portion (e.g write 1D instead of 1 which is an int)
                Output.Write("D");
            }
        }

        private void GenerateDecimalValue(Decimal d)
        {
            Output.Write(d.ToString(CultureInfo.InvariantCulture));
            Output.Write('m');
        }

        private void OutputVTableModifier(MemberAttributes attributes)
        {
            switch (attributes & MemberAttributes.VTableMask)
            {
                case MemberAttributes.New:
                    Output.Write("new ");
                    break;
            }
        }

        /// <devdoc>
        ///    <para>
        ///       Generates code for the specified member access modifier.
        ///    </para>
        /// </devdoc>
        private void OutputMemberAccessModifier(MemberAttributes attributes)
        {
            switch (attributes & MemberAttributes.AccessMask)
            {
                case MemberAttributes.Assembly:
                    Output.Write("internal ");
                    break;
                case MemberAttributes.FamilyAndAssembly:
                    Output.Write("internal ");  /*FamANDAssem*/
                    break;
                case MemberAttributes.Family:
                    Output.Write("protected ");
                    break;
                case MemberAttributes.FamilyOrAssembly:
                    Output.Write("protected internal ");
                    break;
                case MemberAttributes.Private:
                    Output.Write("private ");
                    break;
                case MemberAttributes.Public:
                    Output.Write("public ");
                    break;
            }
        }

        private void OutputMemberScopeModifier(MemberAttributes attributes)
        {
            switch (attributes & MemberAttributes.ScopeMask)
            {
                case MemberAttributes.Abstract:
                    Output.Write("abstract ");
                    break;
                case MemberAttributes.Final:
                    Output.Write("");
                    break;
                case MemberAttributes.Static:
                    Output.Write("static ");
                    break;
                case MemberAttributes.Override:
                    Output.Write("override ");
                    break;
                default:
                    switch (attributes & MemberAttributes.AccessMask)
                    {
                        case MemberAttributes.Family:
                        case MemberAttributes.Public:
                        case MemberAttributes.Assembly:
                            Output.Write("virtual ");
                            break;
                        default:
                            // nothing;
                            break;
                    }
                    break;
            }
        }

        /// <devdoc>
        ///    <para>
        ///       Generates code for the specified operator.
        ///    </para>
        /// </devdoc>
        private void OutputOperator(CodeBinaryOperatorType op)
        {
            switch (op)
            {
                case CodeBinaryOperatorType.Add:
                    Output.Write("+");
                    break;
                case CodeBinaryOperatorType.Subtract:
                    Output.Write("-");
                    break;
                case CodeBinaryOperatorType.Multiply:
                    Output.Write("*");
                    break;
                case CodeBinaryOperatorType.Divide:
                    Output.Write("/");
                    break;
                case CodeBinaryOperatorType.Modulus:
                    Output.Write("%");
                    break;
                case CodeBinaryOperatorType.Assign:
                    Output.Write("=");
                    break;
                case CodeBinaryOperatorType.IdentityInequality:
                    Output.Write("!=");
                    break;
                case CodeBinaryOperatorType.IdentityEquality:
                    Output.Write("==");
                    break;
                case CodeBinaryOperatorType.ValueEquality:
                    Output.Write("==");
                    break;
                case CodeBinaryOperatorType.BitwiseOr:
                    Output.Write("|");
                    break;
                case CodeBinaryOperatorType.BitwiseAnd:
                    Output.Write("&");
                    break;
                case CodeBinaryOperatorType.BooleanOr:
                    Output.Write("||");
                    break;
                case CodeBinaryOperatorType.BooleanAnd:
                    Output.Write("&&");
                    break;
                case CodeBinaryOperatorType.LessThan:
                    Output.Write("<");
                    break;
                case CodeBinaryOperatorType.LessThanOrEqual:
                    Output.Write("<=");
                    break;
                case CodeBinaryOperatorType.GreaterThan:
                    Output.Write(">");
                    break;
                case CodeBinaryOperatorType.GreaterThanOrEqual:
                    Output.Write(">=");
                    break;
            }
        }

        private void OutputFieldScopeModifier(MemberAttributes attributes)
        {
            switch (attributes & MemberAttributes.ScopeMask)
            {
                case MemberAttributes.Final:
                    break;
                case MemberAttributes.Static:
                    Output.Write("static ");
                    break;
                case MemberAttributes.Const:
                    Output.Write("constant ");
                    break;
                default:
                    break;
            }
        }

        /// <devdoc>
        ///    <para>
        ///       Generates code for the specified CodeDom based property reference
        ///       expression representation.
        ///    </para>
        /// </devdoc>
        private void GeneratePropertyReferenceExpression(CodePropertyReferenceExpression e)
        {

            if (e.TargetObject != null)
            {
                GenerateExpression(e.TargetObject);
                Output.Write(".");
            }
            OutputIdentifier(e.PropertyName);
        }

        private void GenerateConstructors(CodeTypeDeclaration e)
        {
            IEnumerator en = e.Members.GetEnumerator();
            while (en.MoveNext())
            {
                if (en.Current is CodeConstructor)
                {
                    currentMember = (CodeTypeMember)en.Current;

                    if (options.BlankLinesBetweenMembers)
                    {
                        Output.WriteLine();
                    }
                    if (currentMember.StartDirectives.Count > 0)
                    {
                        GenerateDirectives(currentMember.StartDirectives);
                    }
                    GenerateCommentStatements(currentMember.Comments);
                    CodeConstructor imp = (CodeConstructor)en.Current;
                    if (imp.LinePragma != null) GenerateLinePragmaStart(imp.LinePragma);
                    GenerateConstructor(imp, e);
                    if (imp.LinePragma != null) GenerateLinePragmaEnd(imp.LinePragma);
                    if (currentMember.EndDirectives.Count > 0)
                    {
                        GenerateDirectives(currentMember.EndDirectives);
                    }
                }
            }
        }

        /// <devdoc>
        ///    <para>
        ///       Generates code for the specified CodeDom based constructor
        ///       representation.
        ///    </para>
        /// </devdoc>
        private void GenerateConstructor(CodeConstructor e, CodeTypeDeclaration c)
        {
            if (!(IsCurrentClass || IsCurrentStruct)) return;

            if (e.CustomAttributes.Count > 0)
            {
                GenerateAttributes(e.CustomAttributes);
            }

            OutputMemberAccessModifier(e.Attributes);
            OutputIdentifier(CurrentTypeName);
            Output.Write("(");
            OutputParameters(e.Parameters);
            Output.Write(")");

            CodeExpressionCollection baseArgs = e.BaseConstructorArgs;
            CodeExpressionCollection thisArgs = e.ChainedConstructorArgs;

            if (baseArgs.Count > 0)
            {
                Output.WriteLine(" : ");
                Indent++;
                Indent++;
                Output.Write("me(");
                OutputExpressionList(baseArgs);
                Output.Write(")");
                Indent--;
                Indent--;
            }

            if (thisArgs.Count > 0)
            {
                Output.WriteLine(" : ");
                Indent++;
                Indent++;
                Output.Write("self(");
                OutputExpressionList(thisArgs);
                Output.Write(")");
                Indent--;
                Indent--;
            }

            OutputStartingBrace();
            Indent++;
            GenerateStatements(e.Statements);
            Indent--;
            Output.WriteLine("}");
        }
        /// <devdoc>
        ///    <para>
        ///       Generates code for the specified CodeDom based class constructor
        ///       representation.
        ///    </para>
        /// </devdoc>
        private void GenerateTypeConstructor(CodeTypeConstructor e)
        {
            if (!(IsCurrentClass || IsCurrentStruct)) return;

            if (e.CustomAttributes.Count > 0)
            {
                GenerateAttributes(e.CustomAttributes);
            }
            Output.Write("static ");
            Output.Write(CurrentTypeName);
            Output.Write("()");
            OutputStartingBrace();
            Indent++;
            GenerateStatements(e.Statements);
            Indent--;
            Output.WriteLine("}");
        }

        /// <devdoc>
        ///    <para>
        ///       Generates code for the specified CodeDom based type reference expression
        ///       representation.
        ///    </para>
        /// </devdoc>
        private void GenerateTypeReferenceExpression(CodeTypeReferenceExpression e)
        {
            OutputType(e.Type);
        }

        /// <devdoc>
        ///    <para>
        ///       Generates code for the specified CodeDom based type of expression
        ///       representation.
        ///    </para>
        /// </devdoc>
        private void GenerateTypeOfExpression(CodeTypeOfExpression e)
        {
            Output.Write("typeof(");
            OutputType(e.Type);
            Output.Write(")");
        }

        private void GenerateType(CodeTypeDeclaration e)
        {
            currentClass = e;

            if (e.StartDirectives.Count > 0)
            {
                GenerateDirectives(e.StartDirectives);
            }

            GenerateCommentStatements(e.Comments);

            if (e.LinePragma != null) GenerateLinePragmaStart(e.LinePragma);

            GenerateTypeStart(e);

            if (Options.VerbatimOrder)
            {
                foreach (CodeTypeMember member in e.Members)
                {
                    GenerateTypeMember(member, e);
                }
            }
            else
            {

                GenerateFields(e);

                GenerateSnippetMembers(e);

                GenerateTypeConstructors(e);

                GenerateConstructors(e);

                GenerateProperties(e);

                GenerateEvents(e);

                GenerateMethods(e);

                GenerateNestedTypes(e);
            }
            // Nested types clobber the current class, so reset it.
            currentClass = e;

            GenerateTypeEnd(e);
            if (e.LinePragma != null) GenerateLinePragmaEnd(e.LinePragma);

            if (e.EndDirectives.Count > 0)
            {
                GenerateDirectives(e.EndDirectives);
            }

        }

        /// <devdoc>
        ///    <para> Generates code for the specified CodeDom namespace representation and the classes it
        ///       contains.</para>
        /// </devdoc>
        private void GenerateTypes(CodeNamespace e)
        {
            foreach (CodeTypeDeclaration c in e.Types)
            {
                if (options.BlankLinesBetweenMembers)
                {
                    Output.WriteLine();
                }
                ((ICodeGenerator)this).GenerateCodeFromType(c, output.InnerWriter, options);
            }
        }

        /// <devdoc>
        ///    <para>
        ///       Generates code for the specified CodeDom based class start
        ///       representation.
        ///    </para>
        /// </devdoc>
        private void GenerateTypeStart(CodeTypeDeclaration e)
        {
            if (e.CustomAttributes.Count > 0)
            {
                GenerateAttributes(e.CustomAttributes);
            }

            if (IsCurrentDelegate)
            {
                switch (e.TypeAttributes & TypeAttributes.VisibilityMask)
                {
                    case TypeAttributes.Public:
                        Output.Write("public ");
                        break;
                    case TypeAttributes.NotPublic:
                    default:
                        break;
                }

                CodeTypeDelegate del = (CodeTypeDelegate)e;
                Output.Write("delegate ");
                OutputType(del.ReturnType);
                Output.Write(" ");
                OutputIdentifier(e.Name);
                Output.Write("(");
                OutputParameters(del.Parameters);
                Output.WriteLine(");");
            }
            else
            {
                OutputTypeAttributes(e);
                OutputIdentifier(e.Name);

                OutputTypeParameters(e.TypeParameters);

                bool first = true;
                foreach (CodeTypeReference typeRef in e.BaseTypes)
                {
                    if (first)
                    {
                        Output.Write(" : ");
                        first = false;
                    }
                    else
                    {
                        Output.Write(", ");
                    }
                    OutputType(typeRef);
                }

                OutputTypeParameterConstraints(e.TypeParameters);

                OutputStartingBrace();
                Indent++;
            }
        }

        private void GenerateTypeMember(CodeTypeMember member, CodeTypeDeclaration declaredType)
        {

            if (options.BlankLinesBetweenMembers)
            {
                Output.WriteLine();
            }

            if (member is CodeTypeDeclaration)
            {
                ((ICodeGenerator)this).GenerateCodeFromType((CodeTypeDeclaration)member, output.InnerWriter, options);

                // Nested types clobber the current class, so reset it.
                currentClass = declaredType;

                // For nested types, comments and line pragmas are handled separately, so return here
                return;
            }

            if (member.StartDirectives.Count > 0)
            {
                GenerateDirectives(member.StartDirectives);
            }

            GenerateCommentStatements(member.Comments);

            if (member.LinePragma != null)
            {
                GenerateLinePragmaStart(member.LinePragma);
            }

            if (member is CodeMemberField)
            {
                GenerateField((CodeMemberField)member);
            }
            else if (member is CodeMemberProperty)
            {
                GenerateProperty((CodeMemberProperty)member, declaredType);
            }
            else if (member is CodeMemberMethod)
            {
                if (member is CodeConstructor)
                {
                    GenerateConstructor((CodeConstructor)member, declaredType);
                }
                else if (member is CodeTypeConstructor)
                {
                    GenerateTypeConstructor((CodeTypeConstructor)member);
                }
                else if (member is CodeEntryPointMethod)
                {
                    GenerateEntryPointMethod((CodeEntryPointMethod)member, declaredType);
                }
                else
                {
                    GenerateMethod((CodeMemberMethod)member, declaredType);
                }
            }
            else if (member is CodeMemberEvent)
            {
                GenerateEvent((CodeMemberEvent)member, declaredType);
            }
            else if (member is CodeSnippetTypeMember)
            {

                // Don't indent snippets, in order to preserve the column
                // information from the original code.  This improves the debugging
                // experience.
                int savedIndent = Indent;
                Indent = 0;

                GenerateSnippetMember((CodeSnippetTypeMember)member);

                // Restore the indent
                Indent = savedIndent;

                // Generate an extra new line at the end of the snippet.
                // If the snippet is comment and this type only contains comments.
                // The generated code will not compile. 
                Output.WriteLine();
            }

            if (member.LinePragma != null)
            {
                GenerateLinePragmaEnd(member.LinePragma);
            }

            if (member.EndDirectives.Count > 0)
            {
                GenerateDirectives(member.EndDirectives);
            }
        }

        private void GenerateTypeConstructors(CodeTypeDeclaration e)
        {
            IEnumerator en = e.Members.GetEnumerator();
            while (en.MoveNext())
            {
                if (en.Current is CodeTypeConstructor)
                {
                    currentMember = (CodeTypeMember)en.Current;

                    if (options.BlankLinesBetweenMembers)
                    {
                        Output.WriteLine();
                    }
                    if (currentMember.StartDirectives.Count > 0)
                    {
                        GenerateDirectives(currentMember.StartDirectives);
                    }
                    GenerateCommentStatements(currentMember.Comments);
                    CodeTypeConstructor imp = (CodeTypeConstructor)en.Current;
                    if (imp.LinePragma != null) GenerateLinePragmaStart(imp.LinePragma);
                    GenerateTypeConstructor(imp);
                    if (imp.LinePragma != null) GenerateLinePragmaEnd(imp.LinePragma);
                    if (currentMember.EndDirectives.Count > 0)
                    {
                        GenerateDirectives(currentMember.EndDirectives);
                    }
                }
            }
        }

        private void GenerateSnippetMembers(CodeTypeDeclaration e)
        {
            IEnumerator en = e.Members.GetEnumerator();
            bool hasSnippet = false;
            while (en.MoveNext())
            {
                if (en.Current is CodeSnippetTypeMember)
                {
                    hasSnippet = true;
                    currentMember = (CodeTypeMember)en.Current;

                    if (options.BlankLinesBetweenMembers)
                    {
                        Output.WriteLine();
                    }
                    if (currentMember.StartDirectives.Count > 0)
                    {
                        GenerateDirectives(currentMember.StartDirectives);
                    }
                    GenerateCommentStatements(currentMember.Comments);
                    CodeSnippetTypeMember imp = (CodeSnippetTypeMember)en.Current;
                    if (imp.LinePragma != null) GenerateLinePragmaStart(imp.LinePragma);

                    // Don't indent snippets, in order to preserve the column
                    // information from the original code.  This improves the debugging
                    // experience.
                    int savedIndent = Indent;
                    Indent = 0;

                    GenerateSnippetMember(imp);

                    // Restore the indent
                    Indent = savedIndent;

                    if (imp.LinePragma != null) GenerateLinePragmaEnd(imp.LinePragma);
                    if (currentMember.EndDirectives.Count > 0)
                    {
                        GenerateDirectives(currentMember.EndDirectives);
                    }

                }
            }
            // Generate an extra new line at the end of the snippet.
            // If the snippet is comment and this type only contains comments.
            // The generated code will not compile. 
            if (hasSnippet)
            {
                Output.WriteLine();
            }
        }

        private void GenerateNestedTypes(CodeTypeDeclaration e)
        {
            IEnumerator en = e.Members.GetEnumerator();
            while (en.MoveNext())
            {
                if (en.Current is CodeTypeDeclaration)
                {
                    if (options.BlankLinesBetweenMembers)
                    {
                        Output.WriteLine();
                    }
                    CodeTypeDeclaration currentClass = (CodeTypeDeclaration)en.Current;
                    ((ICodeGenerator)this).GenerateCodeFromType(currentClass, output.InnerWriter, options);
                }
            }
        }

        /// <devdoc>
        ///    <para> Generates code for the namepsaces in the specifield CodeDom compile unit.
        ///     </para>
        /// </devdoc>
        private void GenerateNamespaces(CodeCompileUnit e)
        {
            foreach (CodeNamespace n in e.Namespaces)
            {
                ((ICodeGenerator)this).GenerateCodeFromNamespace(n, output.InnerWriter, options);
            }
        }



        /// <devdoc>
        ///    <para>
        ///       Outputs an argument in a attribute block.
        ///    </para>
        /// </devdoc>
        private void OutputAttributeArgument(CodeAttributeArgument arg)
        {
            if (arg.Name != null && arg.Name.Length > 0)
            {
                OutputIdentifier(arg.Name);
                Output.Write("=");
            }
            ((ICodeGenerator)this).GenerateCodeFromExpression(arg.Value, output.InnerWriter, options);
        }

        /// <devdoc>
        ///    <para>
        ///       Generates code for the specified System.CodeDom.FieldDirection.
        ///    </para>
        /// </devdoc>
        private void OutputDirection(FieldDirection dir)
        {
            switch (dir)
            {
                case FieldDirection.In:
                    break;
                case FieldDirection.Out:
                    Output.Write("out ");
                    break;
                case FieldDirection.Ref:
                    Output.Write("ref ");
                    break;
            }
        }

        /// <devdoc>
        ///    <para>
        ///       Generates code for the specified expression list.
        ///    </para>
        /// </devdoc>
        private void OutputExpressionList(CodeExpressionCollection expressions)
        {
            OutputExpressionList(expressions, false /*newlineBetweenItems*/);
        }

        /// <devdoc>
        ///    <para>
        ///       Generates code for the specified expression list.
        ///    </para>
        /// </devdoc>
        private void OutputExpressionList(CodeExpressionCollection expressions, bool newlineBetweenItems)
        {
            bool first = true;
            IEnumerator en = expressions.GetEnumerator();
            Indent++;
            while (en.MoveNext())
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    if (newlineBetweenItems)
                        ContinueOnNewLine(",");
                    else
                        Output.Write(", ");
                }
                ((ICodeGenerator)this).GenerateCodeFromExpression((CodeExpression)en.Current, output.InnerWriter, options);
            }
            Indent--;
        }

        /// <devdoc>
        ///    <para>
        ///       Generates code for the specified parameters.
        ///    </para>
        /// </devdoc>
        private void OutputParameters(CodeParameterDeclarationExpressionCollection parameters)
        {
            bool first = true;
            bool multiline = parameters.Count > ParameterMultilineThreshold;
            if (multiline)
            {
                Indent += 3;
            }
            IEnumerator en = parameters.GetEnumerator();
            while (en.MoveNext())
            {
                CodeParameterDeclarationExpression current = (CodeParameterDeclarationExpression)en.Current;
                if (first)
                {
                    first = false;
                }
                else
                {
                    Output.Write(", ");
                }
                if (multiline)
                {
                    ContinueOnNewLine("");
                }
                GenerateExpression(current);
            }
            if (multiline)
            {
                Indent -= 3;
            }
        }

        /// <devdoc>
        ///    <para>
        ///       Generates code for the specified object type and name pair.
        ///    </para>
        /// </devdoc>
        private void OutputTypeNamePair(CodeTypeReference typeRef, string name)
        {
            OutputType(typeRef);
            Output.Write(" ");
            OutputIdentifier(name);
        }

        private void OutputTypeParameters(CodeTypeParameterCollection typeParameters)
        {
            if (typeParameters.Count == 0)
            {
                return;
            }

            Output.Write('<');
            bool first = true;
            for (int i = 0; i < typeParameters.Count; i++)
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    Output.Write(", ");
                }

                if (typeParameters[i].CustomAttributes.Count > 0)
                {
                    GenerateAttributes(typeParameters[i].CustomAttributes, null, true);
                    Output.Write(' ');
                }

                Output.Write(typeParameters[i].Name);
            }

            Output.Write('>');
        }

        private void OutputTypeParameterConstraints(CodeTypeParameterCollection typeParameters)
        {
            if (typeParameters.Count == 0)
            {
                return;
            }

            for (int i = 0; i < typeParameters.Count; i++)
            {
                // generating something like: "where KeyType: IComparable, IEnumerable"

                Output.WriteLine();
                Indent++;

                bool first = true;
                if (typeParameters[i].Constraints.Count > 0)
                {
                    foreach (CodeTypeReference typeRef in typeParameters[i].Constraints)
                    {
                        if (first)
                        {
                            Output.Write("where ");
                            Output.Write(typeParameters[i].Name);
                            Output.Write(" : ");
                            first = false;
                        }
                        else
                        {
                            Output.Write(", ");
                        }
                        OutputType(typeRef);
                    }
                }

                if (typeParameters[i].HasConstructorConstraint)
                {
                    if (first)
                    {
                        Output.Write("where ");
                        Output.Write(typeParameters[i].Name);
                        Output.Write(" : new()");
                    }
                    else
                    {
                        Output.Write(", new ()");
                    }
                }

                Indent--;
            }
        }


        private void OutputTypeAttributes(CodeTypeDeclaration e)
        {
            if ((e.Attributes & MemberAttributes.New) != 0)
            {
                Output.Write("new ");
            }

            TypeAttributes attributes = e.TypeAttributes;
            switch (attributes & TypeAttributes.VisibilityMask)
            {
                case TypeAttributes.Public:
                case TypeAttributes.NestedPublic:
                    Output.Write("public ");
                    break;
                case TypeAttributes.NestedPrivate:
                    Output.Write("private ");
                    break;
                case TypeAttributes.NestedFamily:
                    Output.Write("protected ");
                    break;
                case TypeAttributes.NotPublic:
                case TypeAttributes.NestedAssembly:
                case TypeAttributes.NestedFamANDAssem:
                    Output.Write("internal ");
                    break;
                case TypeAttributes.NestedFamORAssem:
                    Output.Write("protected internal ");
                    break;
            }

            if (e.IsStruct)
            {
                if (e.IsPartial)
                {
                    Output.Write("shared ");
                }
                Output.Write("struct ");
            }
            else if (e.IsEnum)
            {
                Output.Write("enum ");
            }
            else
            {
                switch (attributes & TypeAttributes.ClassSemanticsMask)
                {
                    case TypeAttributes.Class:
                        if ((attributes & TypeAttributes.Sealed) == TypeAttributes.Sealed)
                        {
                            Output.Write("final ");
                        }
                        if ((attributes & TypeAttributes.Abstract) == TypeAttributes.Abstract)
                        {
                            Output.Write("abstract ");
                        }
                        if (e.IsPartial)
                        {
                            Output.Write("shared ");
                        }

                        Output.Write("class ");

                        break;
                    case TypeAttributes.Interface:
                        if (e.IsPartial)
                        {
                            Output.Write("shared ");
                        }
                        Output.Write("interface ");
                        break;
                }
            }
        }

        /// <devdoc>
        ///    <para>
        ///       Generates code for the specified CodeDom based class end representation.
        ///    </para>
        /// </devdoc>
        private void GenerateTypeEnd(CodeTypeDeclaration e)
        {
            if (!IsCurrentDelegate)
            {
                Indent--;
                Output.WriteLine("}");
            }
        }
        /// <devdoc>
        ///    <para>
        ///       Generates code for the specified CodeDom based namespace start
        ///       representation.
        ///    </para>
        /// </devdoc>
        private void GenerateNamespaceStart(CodeNamespace e)
        {

            if (e.Name != null && e.Name.Length > 0)
            {
                Output.Write("program ");
                string[] names = e.Name.Split('.');
                Debug.Assert(names.Length > 0);
                OutputIdentifier(names[0]);
                for (int i = 1; i < names.Length; i++)
                {
                    Output.Write(".");
                    OutputIdentifier(names[i]);
                }
                OutputStartingBrace();
                Indent++;
            }
        }

        /// <devdoc>
        ///    <para> Generates code for the specified CodeDom
        ///       compile unit representation.</para>
        /// </devdoc>
        private void GenerateCompileUnit(CodeCompileUnit e)
        {
            GenerateCompileUnitStart(e);
            GenerateNamespaces(e);
            GenerateCompileUnitEnd(e);
        }

        /// <devdoc>
        ///    <para>
        ///       Generates code for the specified CodeDom based compile unit start
        ///       representation.
        ///    </para>
        /// </devdoc>
        private void GenerateCompileUnitStart(CodeCompileUnit e)
        {

            if (e.StartDirectives.Count > 0)
            {
                GenerateDirectives(e.StartDirectives);
            }

            Output.WriteLine("//------------------------------------------------------------------------------");
            Output.Write("// Arsslen Language Auto-Genenerated");

            Output.WriteLine("//------------------------------------------------------------------------------");
            Output.WriteLine("");

            SortedList importList;
            // CSharp needs to put assembly attributes after using statements.
            // Since we need to create a empty namespace even if we don't need it,
            // using will generated after assembly attributes.
            importList = new SortedList(StringComparer.Ordinal);
            foreach (CodeNamespace nspace in e.Namespaces)
            {
                if (String.IsNullOrEmpty(nspace.Name))
                {
                    // mark the namespace to stop it generating its own import list
                    nspace.UserData["GenerateImports"] = false;

                    // Collect the unique list of imports
                    foreach (CodeNamespaceImport import in nspace.Imports)
                    {
                        if (!importList.Contains(import.Namespace))
                        {
                            importList.Add(import.Namespace, import.Namespace);
                        }
                    }
                }
            }

            // now output the imports
            foreach (string import in importList.Keys)
            {
                Output.Write("include ");
                OutputIdentifier(import);
                Output.WriteLine(";");
            }
            if (importList.Keys.Count > 0)
            {
                Output.WriteLine("");
            }

            // in C# the best place to put these is at the top level.
            if (e.AssemblyCustomAttributes.Count > 0)
            {
                GenerateAttributes(e.AssemblyCustomAttributes, "assembly: ");
                Output.WriteLine("");
            }

        }

        /// <devdoc>
        ///    <para>
        ///       Generates code for the specified CodeDom based compile unit end
        ///       representation.
        ///    </para>
        /// </devdoc>
        private void GenerateCompileUnitEnd(CodeCompileUnit e)
        {
            if (e.EndDirectives.Count > 0)
            {
                GenerateDirectives(e.EndDirectives);
            }
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        private void GenerateDirectionExpression(CodeDirectionExpression e)
        {
            OutputDirection(e.Direction);
            GenerateExpression(e.Expression);
        }

        private void GenerateDirectives(CodeDirectiveCollection directives)
        {
            for (int i = 0; i < directives.Count; i++)
            {
                CodeDirective directive = directives[i];
                if (directive is CodeChecksumPragma)
                {
                    GenerateChecksumPragma((CodeChecksumPragma)directive);
                }
                else if (directive is CodeRegionDirective)
                {
                    GenerateCodeRegionDirective((CodeRegionDirective)directive);
                }
            }
        }

        private void GenerateChecksumPragma(CodeChecksumPragma checksumPragma)
        {
            Output.Write("#pragma checksum \"");
            Output.Write(checksumPragma.FileName);
            Output.Write("\" \"");
            Output.Write(checksumPragma.ChecksumAlgorithmId.ToString("B", CultureInfo.InvariantCulture));
            Output.Write("\" \"");
            if (checksumPragma.ChecksumData != null)
            {
                foreach (Byte b in checksumPragma.ChecksumData)
                {
                    Output.Write(b.ToString("X2", CultureInfo.InvariantCulture));
                }
            }
            Output.WriteLine("\"");
        }

        private void GenerateCodeRegionDirective(CodeRegionDirective regionDirective)
        {
            if (regionDirective.RegionMode == CodeRegionMode.Start)
            {
                Output.Write("#region ");
                Output.WriteLine(regionDirective.RegionText);
            }
            else if (regionDirective.RegionMode == CodeRegionMode.End)
            {
                Output.WriteLine("#endregion");
            }
        }

        /// <devdoc>
        ///    <para>
        ///       Generates code for the specified CodeDom based namespace end
        ///       representation.
        ///    </para>
        /// </devdoc>
        private void GenerateNamespaceEnd(CodeNamespace e)
        {
            if (e.Name != null && e.Name.Length > 0)
            {
                Indent--;
                Output.WriteLine("}");
            }
        }
        /// <devdoc>
        ///    <para>
        ///       Generates code for the specified CodeDom based namespace import
        ///       representation.
        ///    </para>
        /// </devdoc>
        private void GenerateNamespaceImport(CodeNamespaceImport e)
        {
            Output.Write("include ");
            OutputIdentifier(e.Namespace);
            Output.WriteLine(";");
        }
        /// <devdoc>
        ///    <para>
        ///       Generates code for the specified CodeDom based attribute block start
        ///       representation.
        ///    </para>
        /// </devdoc>
        private void GenerateAttributeDeclarationsStart(CodeAttributeDeclarationCollection attributes)
        {
            Output.Write("[");
        }
        /// <devdoc>
        ///    <para>
        ///       Generates code for the specified CodeDom based attribute block end
        ///       representation.
        ///    </para>
        /// </devdoc>
        private void GenerateAttributeDeclarationsEnd(CodeAttributeDeclarationCollection attributes)
        {
            Output.Write("]");
        }

        private void GenerateAttributes(CodeAttributeDeclarationCollection attributes)
        {
            GenerateAttributes(attributes, null, false);
        }

        private void GenerateAttributes(CodeAttributeDeclarationCollection attributes, string prefix)
        {
            GenerateAttributes(attributes, prefix, false);
        }

        private void GenerateAttributes(CodeAttributeDeclarationCollection attributes, string prefix, bool inLine)
        {
            if (attributes.Count == 0) return;
            IEnumerator en = attributes.GetEnumerator();
            bool paramArray = false;

            while (en.MoveNext())
            {
                // we need to convert paramArrayAttribute to params keyword to 
                // make csharp compiler happy. In addition, params keyword needs to be after 
                // other attributes.

                CodeAttributeDeclaration current = (CodeAttributeDeclaration)en.Current;

                if (current.Name.Equals("system.paramarrayattribute", StringComparison.OrdinalIgnoreCase))
                {
                    paramArray = true;
                    continue;
                }

                GenerateAttributeDeclarationsStart(attributes);
                if (prefix != null)
                {
                    Output.Write(prefix);
                }

                if (current.AttributeType != null)
                {
                    Output.Write(GetTypeOutput(current.AttributeType));
                }
                Output.Write("(");

                bool firstArg = true;
                foreach (CodeAttributeArgument arg in current.Arguments)
                {
                    if (firstArg)
                    {
                        firstArg = false;
                    }
                    else
                    {
                        Output.Write(", ");
                    }

                    OutputAttributeArgument(arg);
                }

                Output.Write(")");
                GenerateAttributeDeclarationsEnd(attributes);
                if (inLine)
                {
                    Output.Write(" ");
                }
                else
                {
                    Output.WriteLine();
                }
            }

            if (paramArray)
            {
                if (prefix != null)
                {
                    Output.Write(prefix);
                }
                Output.Write("params");

                if (inLine)
                {
                    Output.Write(" ");
                }
                else
                {
                    Output.WriteLine();
                }
            }


        }

        static bool IsKeyword(string value)
        {

            return keywordsTable.ContainsKey(value);
        }

        static bool IsPrefixTwoUnderscore(string value)
        {
            if (value.Length < 3)
            {
                return false;
            }
            else
            {
                return ((value[0] == '_') && (value[1] == '_') && (value[2] != '_'));
            }
        }

        public bool Supports(GeneratorSupport support)
        {
            return ((support & LanguageSupport) == support);
        }

        /// <devdoc>
        ///    <para>
        ///       Gets whether the specified value is a valid identifier.
        ///    </para>
        /// </devdoc>
        public bool IsValidIdentifier(string value)
        {

            // identifiers must be 1 char or longer
            //
            if (value == null || value.Length == 0)
            {
                return false;
            }

            if (value.Length > 512)
                return false;

            // identifiers cannot be a keyword, unless they are escaped with an '@'
            //
            if (value[0] != '@')
            {
                if (IsKeyword(value))
                    return false;
            }
            else
            {
                value = value.Substring(1);
            }

            return CodeGenerator.IsValidLanguageIndependentIdentifier(value);
        }

        public void ValidateIdentifier(string value)
        {
            if (!IsValidIdentifier(value))
            {
                throw new ArgumentException("invalid identifier");
            }
        }

        public string CreateValidIdentifier(string name)
        {
            if (IsPrefixTwoUnderscore(name))
            {
                name = "_" + name;
            }

            while (IsKeyword(name))
            {
                name = "_" + name;
            }

            return name;
        }

        public string CreateEscapedIdentifier(string name)
        {
            // Any identifier started with two consecutive underscores are 
            // reserved by CSharp.
            if (IsKeyword(name) || IsPrefixTwoUnderscore(name))
            {
                return "@" + name;
            }
            return name;
        }

        // returns the type name without any array declaration.
        private string GetBaseTypeOutput(CodeTypeReference typeRef)
        {
            string s = typeRef.BaseType;
            if (s.Length == 0)
            {
                s = "sub";
                return s;
            }

            string lowerCaseString = s.ToLower(CultureInfo.InvariantCulture).Trim();

            switch (lowerCaseString)
            {
                case "system.int16":
                    s = "shortint";
                    break;
                case "system.int32":
                    s = "integer";
                    break;
                case "system.int64":
                    s = "longint";
                    break;
                case "system.string":
                    s = "string";
                    break;
                case "system.object":
                    s = "object";
                    break;
                case "system.boolean":
                    s = "bool";
                    break;
                case "system.void":
                    s = "sub";
                    break;
                case "system.char":
                    s = "char";
                    break;
                case "system.byte":
                    s = "byte";
                    break;
                case "system.uint16":
                    s = "ushortint";
                    break;
                case "system.uint32":
                    s = "uinteger";
                    break;
                case "system.uint64":
                    s = "ulongint";
                    break;
                case "system.sbyte":
                    s = "sbyte";
                    break;
                case "system.single":
                    s = "float";
                    break;
                case "system.double":
                    s = "real";
                    break;
                case "system.decimal":
                    s = "decimal";
                    break;
                default:
                    // replace + with . for nested classes.
                    //
                    StringBuilder sb = new StringBuilder(s.Length + 10);
                    if ((typeRef.Options & CodeTypeReferenceOptions.GlobalReference) != 0)
                    {
                        sb.Append("global::");
                    }

                    string baseType = typeRef.BaseType;

                    int lastIndex = 0;
                    int currentTypeArgStart = 0;
                    for (int i = 0; i < baseType.Length; i++)
                    {
                        switch (baseType[i])
                        {
                            case '+':
                            case '.':
                                sb.Append(CreateEscapedIdentifier(baseType.Substring(lastIndex, i - lastIndex)));
                                sb.Append('.');
                                i++;
                                lastIndex = i;
                                break;

                            case '`':
                                sb.Append(CreateEscapedIdentifier(baseType.Substring(lastIndex, i - lastIndex)));
                                i++;    // skip the '
                                int numTypeArgs = 0;
                                while (i < baseType.Length && baseType[i] >= '0' && baseType[i] <= '9')
                                {
                                    numTypeArgs = numTypeArgs * 10 + (baseType[i] - '0');
                                    i++;
                                }

                                GetTypeArgumentsOutput(typeRef.TypeArguments, currentTypeArgStart, numTypeArgs, sb);
                                currentTypeArgStart += numTypeArgs;

                                // Arity can be in the middle of a nested type name, so we might have a . or + after it. 
                                // Skip it if so. 
                                if (i < baseType.Length && (baseType[i] == '+' || baseType[i] == '.'))
                                {
                                    sb.Append('.');
                                    i++;
                                }

                                lastIndex = i;
                                break;
                        }
                    }

                    if (lastIndex < baseType.Length)
                        sb.Append(CreateEscapedIdentifier(baseType.Substring(lastIndex)));

                    return sb.ToString();
            }
            return s;
        }


        private String GetTypeArgumentsOutput(CodeTypeReferenceCollection typeArguments)
        {
            StringBuilder sb = new StringBuilder(128);
            GetTypeArgumentsOutput(typeArguments, 0, typeArguments.Count, sb);
            return sb.ToString();
        }

        private void GetTypeArgumentsOutput(CodeTypeReferenceCollection typeArguments, int start, int length, StringBuilder sb)
        {
            sb.Append('<');
            bool first = true;
            for (int i = start; i < start + length; i++)
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    sb.Append(", ");
                }

                // it's possible that we call GetTypeArgumentsOutput with an empty typeArguments collection.  This is the case
                // for open types, so we want to just output the brackets and commas. 
                if (i < typeArguments.Count)
                    sb.Append(GetTypeOutput(typeArguments[i]));
            }
            sb.Append('>');
        }

        public string GetTypeOutput(CodeTypeReference typeRef)
        {
            string s = String.Empty;

            CodeTypeReference baseTypeRef = typeRef;
            while (baseTypeRef.ArrayElementType != null)
            {
                baseTypeRef = baseTypeRef.ArrayElementType;
            }
            s += GetBaseTypeOutput(baseTypeRef);

            while (typeRef != null && typeRef.ArrayRank > 0)
            {
                char[] results = new char[typeRef.ArrayRank + 1];
                results[0] = '[';
                results[typeRef.ArrayRank] = ']';
                for (int i = 1; i < typeRef.ArrayRank; i++)
                {
                    results[i] = ',';
                }
                s += new string(results);
                typeRef = typeRef.ArrayElementType;
            }

            return s;
        }

        private void OutputStartingBrace()
        {
            if (Options.BracingStyle == "C")
            {
                Output.WriteLine("");
                Output.WriteLine("{");
            }
            else
            {
                Output.WriteLine(" {");
            }
        }



        private static string[] ReadAllLines(String file, Encoding encoding, FileShare share)
        {
            using (FileStream stream = File.Open(file, FileMode.Open, FileAccess.Read, share))
            {
                String line;
                List<String> lines = new List<String>();

                using (StreamReader sr = new StreamReader(stream, encoding))
                    while ((line = sr.ReadLine()) != null)
                        lines.Add(line);

                return lines.ToArray();
            }
        }





        /// <devdoc>
        ///    <para>
        ///       Because CodeCompileUnit and CompilerParameters both have a referenced assemblies 
        ///       property, they must be reconciled. However, because you can compile multiple
        ///       compile units with one set of options, it will simply merge them.
        ///    </para>
        /// </devdoc>
        private void ResolveReferencedAssemblies(CompilerParameters options, CodeCompileUnit e)
        {
            if (e.ReferencedAssemblies.Count > 0)
            {
                foreach (string assemblyName in e.ReferencedAssemblies)
                {
                    if (!options.ReferencedAssemblies.Contains(assemblyName))
                    {
                        options.ReferencedAssemblies.Add(assemblyName);
                    }
                }
            }
        }



        /// <devdoc>
        ///    <para>Joins the specified string arrays.</para>
        /// </devdoc>
        private static string JoinStringArray(string[] sa, string separator)
        {
            if (sa == null || sa.Length == 0)
                return String.Empty;

            if (sa.Length == 1)
            {
                return "\"" + sa[0] + "\"";
            }

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < sa.Length - 1; i++)
            {
                sb.Append("\"");
                sb.Append(sa[i]);
                sb.Append("\"");
                sb.Append(separator);
            }
            sb.Append("\"");
            sb.Append(sa[sa.Length - 1]);
            sb.Append("\"");

            return sb.ToString();
        }

        /// <internalonly/>
        void ICodeGenerator.GenerateCodeFromType(CodeTypeDeclaration e, TextWriter w, CodeGeneratorOptions o)
        {
            bool setLocal = false;
            if (output != null && w != output.InnerWriter)
            {
                throw new InvalidOperationException("Output isn't null");
            }
            if (output == null)
            {
                setLocal = true;
                options = (o == null) ? new CodeGeneratorOptions() : o;
                output = new IndentedTextWriter(w, options.IndentString);
            }

            try
            {
                GenerateType(e);
            }
            finally
            {
                if (setLocal)
                {
                    output = null;
                    options = null;
                }
            }
        }

        /// <internalonly/>
        void ICodeGenerator.GenerateCodeFromExpression(CodeExpression e, TextWriter w, CodeGeneratorOptions o)
        {
            bool setLocal = false;
            if (output != null && w != output.InnerWriter)
            {
                throw new InvalidOperationException("Output isn't null");
            }
            if (output == null)
            {
                setLocal = true;
                options = (o == null) ? new CodeGeneratorOptions() : o;
                output = new IndentedTextWriter(w, options.IndentString);
            }

            try
            {
                GenerateExpression(e);
            }
            finally
            {
                if (setLocal)
                {
                    output = null;
                    options = null;
                }
            }
        }

        /// <internalonly/>
        void ICodeGenerator.GenerateCodeFromCompileUnit(CodeCompileUnit e, TextWriter w, CodeGeneratorOptions o)
        {
            bool setLocal = false;
            if (output != null && w != output.InnerWriter)
            {
                throw new InvalidOperationException("Output isn't null");
            }
            if (output == null)
            {
                setLocal = true;
                options = (o == null) ? new CodeGeneratorOptions() : o;
                output = new IndentedTextWriter(w, options.IndentString);
            }

            try
            {
                if (e is CodeSnippetCompileUnit)
                {
                    GenerateSnippetCompileUnit((CodeSnippetCompileUnit)e);
                }
                else
                {
                    GenerateCompileUnit(e);
                }
            }
            finally
            {
                if (setLocal)
                {
                    output = null;
                    options = null;
                }
            }
        }

        /// <internalonly/>
        void ICodeGenerator.GenerateCodeFromNamespace(CodeNamespace e, TextWriter w, CodeGeneratorOptions o)
        {
            bool setLocal = false;
            if (output != null && w != output.InnerWriter)
            {
                throw new InvalidOperationException("Output isn't null");
            }
            if (output == null)
            {
                setLocal = true;
                options = (o == null) ? new CodeGeneratorOptions() : o;
                output = new IndentedTextWriter(w, options.IndentString);
            }

            try
            {
                GenerateNamespace(e);
            }
            finally
            {
                if (setLocal)
                {
                    output = null;
                    options = null;
                }
            }
        }

        /// <internalonly/>
        void ICodeGenerator.GenerateCodeFromStatement(CodeStatement e, TextWriter w, CodeGeneratorOptions o)
        {
            bool setLocal = false;
            if (output != null && w != output.InnerWriter)
            {
                throw new InvalidOperationException("Output isn't null");
            }
            if (output == null)
            {
                setLocal = true;
                options = (o == null) ? new CodeGeneratorOptions() : o;
                output = new IndentedTextWriter(w, options.IndentString);
            }

            try
            {
                GenerateStatement(e);
            }
            finally
            {
                if (setLocal)
                {
                    output = null;
                    options = null;
                }
            }
        }



    }  // AlCodeGenerator
 



    #region class CSharpTypeAttributeConverter

    internal class CSharpTypeAttributeConverter : CSharpModifierAttributeConverter
    {
        private static volatile string[] names;
        private static volatile object[] values;
        private static volatile CSharpTypeAttributeConverter defaultConverter;

        private CSharpTypeAttributeConverter()
        {
            // no  need to create an instance; use Default
        }

        public static CSharpTypeAttributeConverter Default
        {
            get
            {
                if (defaultConverter == null)
                {
                    defaultConverter = new CSharpTypeAttributeConverter();
                }
                return defaultConverter;
            }
        }

        /// <devdoc>
        ///      Retrieves an array of names for attributes.
        /// </devdoc>
        protected override string[] Names
        {
            get
            {
                if (names == null)
                {
                    names = new string[] {
                        "Public",
                        "Internal"
                    };
                }

                return names;
            }
        }

        /// <devdoc>
        ///      Retrieves an array of values for attributes.
        /// </devdoc>
        protected override object[] Values
        {
            get
            {
                if (values == null)
                {
                    values = new object[] {
                        (object)TypeAttributes.Public,
                        (object)TypeAttributes.NotPublic                       
                    };
                }

                return values;
            }
        }

        protected override object DefaultValue
        {
            get
            {
                return TypeAttributes.NotPublic;
            }
        }
    }  // CSharpTypeAttributeConverter

    #endregion class CSharpTypeAttributeConverter


    #region class CSharpMemberAttributeConverter

    internal class CSharpMemberAttributeConverter : CSharpModifierAttributeConverter
    {
        private static volatile string[] names;
        private static volatile object[] values;
        private static volatile CSharpMemberAttributeConverter defaultConverter;

        private CSharpMemberAttributeConverter()
        {
            // no  need to create an instance; use Default
        }

        public static CSharpMemberAttributeConverter Default
        {
            get
            {
                if (defaultConverter == null)
                {
                    defaultConverter = new CSharpMemberAttributeConverter();
                }
                return defaultConverter;
            }
        }

        /// <devdoc>
        ///      Retrieves an array of names for attributes.
        /// </devdoc>
        protected override string[] Names
        {
            get
            {
                if (names == null)
                {
                    names = new string[] {
                        "Public",
                        "Protected",
                        "Protected Internal",
                        "Internal",
                        "Private"
                    };
                }

                return names;
            }
        }

        /// <devdoc>
        ///      Retrieves an array of values for attributes.
        /// </devdoc>
        protected override object[] Values
        {
            get
            {
                if (values == null)
                {
                    values = new object[] {
                        (object)MemberAttributes.Public,
                        (object)MemberAttributes.Family,
                        (object)MemberAttributes.FamilyOrAssembly,
                        (object)MemberAttributes.Assembly,
                        (object)MemberAttributes.Private
                    };
                }

                return values;
            }
        }

        protected override object DefaultValue
        {
            get
            {
                return MemberAttributes.Private;
            }
        }
    }  // CSharpMemberAttributeConverter

    #endregion class CSharpMemberAttributeConverter


    #region class CSharpModifierAttributeConverter

    /// <devdoc>
    ///      This type converter provides common values for MemberAttributes
    /// </devdoc>
    internal abstract class CSharpModifierAttributeConverter : TypeConverter
    {

        protected abstract object[] Values { get; }
        protected abstract string[] Names { get; }
        protected abstract object DefaultValue { get; }



        /// <devdoc>
        ///      We override this because we can convert from string types.
        /// </devdoc>
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            if (sourceType == typeof(string))
            {
                return true;
            }

            return base.CanConvertFrom(context, sourceType);
        }

        /// <devdoc>
        ///      Converts the given object to the converter's native type.
        /// </devdoc>
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (value is string)
            {
                string name = (string)value;
                string[] names = Names;
                for (int i = 0; i < names.Length; i++)
                {
                    if (names[i].Equals(name))
                    {
                        return Values[i];
                    }
                }
            }

            return DefaultValue;
        }

        /// <devdoc>
        ///      Converts the given object to another type.  The most common types to convert
        ///      are to and from a string object.  The default implementation will make a call
        ///      to ToString on the object if the object is valid and if the destination
        ///      type is string.  If this cannot convert to the desitnation type, this will
        ///      throw a NotSupportedException.
        /// </devdoc>
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == null)
            {
                throw new ArgumentNullException("destinationType");
            }

            if (destinationType == typeof(string))
            {
                object[] modifiers = Values;
                for (int i = 0; i < modifiers.Length; i++)
                {
                    if (modifiers[i].Equals(value))
                    {
                        return Names[i];
                    }
                }

                return "unkown";
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }

        /// <devdoc>
        ///      Determines if the list of standard values returned from
        ///      GetStandardValues is an exclusive list.  If the list
        ///      is exclusive, then no other values are valid, such as
        ///      in an enum data type.  If the list is not exclusive,
        ///      then there are other valid values besides the list of
        ///      standard values GetStandardValues provides.
        /// </devdoc>
        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
        {
            return true;
        }

        /// <devdoc>
        ///      Determines if this object supports a standard set of values
        ///      that can be picked from a list.
        /// </devdoc>
        public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
        {
            return true;
        }

        /// <devdoc>
        ///      Retrieves a collection containing a set of standard values
        ///      for the data type this validator is designed for.  This
        ///      will return null if the data type does not support a
        ///      standard set of values.
        /// </devdoc>
        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            return new StandardValuesCollection(Values);
        }
    }  // CSharpModifierAttributeConverter

    #endregion class CSharpModifierAttributeConverter


}
