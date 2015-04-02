using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Xsl;
using Microsoft.VisualStudio.Coverage.Analysis;
using System.Diagnostics;

namespace Toneri
{
    class Program
    {
        /// <summary>site url</summary>
        private const string SITE_URL = "https://github.com/yasu-s/CoverageConverter";

        /// <summary>argument prefix: Input File Path</summary>
        private const string ARGS_PREFIX_TRX_PATH = "/trx:";

        /// <summary>argument prefix: Input File Path</summary>
        private const string ARGS_PREFIX_INPUT_PATH = "/in:";

        /// <summary>argument prefix: Output File Path</summary>
        private const string ARGS_PREFIX_OUTPUT_PATH = "/out:";

        /// <summary>argument prefix: Symbols Directory</summary>
        private const string ARGS_PREFIX_SYMBOLS_DIR = "/symbols:";

        /// <summary>argument prefix: Exe Directory</summary>
        private const string ARGS_PREFIX_EXE_DIR = "/exedir:";

        /// <summary>argument prefix: Convert Xsl Path</summary>
        private const string ARGS_PREFIX_XSL_PATH = "/xsl:";

        /// <summary>argument prefix: Help</summary>
        private const string ARGS_HELP = "/?";

        /// <summary>file name: work file</summary>
        private const string FILE_NAME_WORK = "CoverageConverterWork.coverage";

        /// <summary>file extension: coverage file</summary>
        private const string FILE_EXTENSION_COVERAGE = "coverage";

        /// <summary>
        /// Main process
        /// </summary>
        /// <param name="args">Command Line Arguments</param>
        static int Main(string[] args)
        {
            bool result = false;

			AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(CurrentDomain_AssemblyResolve);
            
            try
            {
                // cosole write header.
                ConsoleWriteHeader();

                if (IsConsoleWriteHelp(args))
                {
                    ConsoleWriteHelp();
                    return;
                }

                IEnumerable<string> trxFiles  = ConvertArgToIEnumerable(args, ARGS_PREFIX_TRX_PATH);
                
                string inputPath  = ConvertArg(args, ARGS_PREFIX_INPUT_PATH);
                string outputPath = ConvertArg(args, ARGS_PREFIX_OUTPUT_PATH);
                string symbolsDir = ConvertArg(args, ARGS_PREFIX_SYMBOLS_DIR);
                string exeDir     = ConvertArg(args, ARGS_PREFIX_EXE_DIR);
                IEnumerable<string> xslPaths    = ConvertArgToIEnumerable(args, ARGS_PREFIX_XSL_PATH);
                string tmpPath    = Path.Combine(Path.GetTempPath(), FILE_NAME_WORK);

                var cs = CoverageSet.FromTrx(trxFiles);

                if (!CreateWorkFile(tmpPath, inputPath))
                    return;

                IList<string> symPaths = new List<string>();
                IList<string> exePaths = new List<string>();

                if (!string.IsNullOrWhiteSpace(symbolsDir))
                    symPaths.Add(symbolsDir);

                if (!string.IsNullOrWhiteSpace(exeDir))
                    exePaths.Add(exeDir);

                var outputWk = convertToXml(tmpPath, exePaths, symPaths);

                Console.WriteLine("output file: {0}", outputWk);

                foreach (var xsl in xslPaths)
                    ApplyXsl(data, Path.ChangeExtension(outputWk, Path.GetFileNameWithoutExtension(xsl) + ".xml"), xsl);

                result = true;

                Console.WriteLine("convert success.");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                return result;
            }
        }

        private static string convertToXml(string tmpPath, IEnumerable<string> exePaths, IEnumerable<string> symPaths)
        {
    	    CoverageInfo ci = CoverageInfo.CreateFromFile(tmpPath, exePaths, symPaths);
            CoverageDS data = ci.BuildDataSet(null);
            
            string outputWk = string.IsNullOrEmpty(outputPath) ? Path.ChangeExtension(inputPath, "xml") : outputPath;
            data.WriteXml(outputWk);
            return outputWk;
        }

		//http://blogs.msdn.com/b/hippietim/archive/2006/03/24/560010.aspx
		static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
	    {
            String[] assemblyParams = args.Name.Split(',');
            
            Debug.Assert(assemblyParams.Length > 0 && !String.IsNullOrEmpty(assemblyParams[0]), "Invalid assembly name arguments passed to domain_AssemblyResolve");
 
            //  Note that there are additional fields passed that indicate the 
            //  version, public key token, etc.  For this demonstration, we           
            //  are just looking at the assembly name.
 
            String assemblyName = assemblyParams[0];
            Assembly loadedAssembly = null;
 
            switch (assemblyName)
            {
                case "Microsoft.VisualStudio.Coverage.Analysis":
                    // etc.
                    loadedAssembly = LoadVSPrivateAssembly(assemblyName);
                    break;
 
                default:
                    Debug.Fail(assemblyName + " loading from private assemblies is not supported!");
                    break;
            }
 
            return loadedAssembly;
        }

        const string[] vs_versions = new [] {"8.0", "9.0", "10.0", "11.0", "12.0", "14.0"};
        //  This function will load the named assembly from the Visual Studio PrivateAssemblies 
        //  directory.  This is where a number of Team Foundation assemblies are located that are
        //  not easily accessible to an app.  Fortunately, the .NET loader gives us a shot at 
        //  finding them and we just happen to know where to look.
        static Assembly LoadVSPrivateAssembly(String assemblyName)
        {
            Assembly loadedAssembly = null;
 
            foreach (var version in vs_versions)
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\VisualStudio\" + version))
                {
                    if (key != null)
                    {
                        Object obj = key.GetValue("InstallDir");
     
                        if ((obj != null) && (obj is String))
                        {
                            String vsInstallDir = obj as String;
                            String privateAssembliesDir = Path.Combine(vsInstallDir, "PrivateAssemblies");
                            String assemblyFile = Path.Combine(privateAssembliesDir, assemblyName + ".dll");
     
                            loadedAssembly = Assembly.LoadFile(assemblyFile);
                            break;
                        }
                        else
                        {
                            Debug.Fail("VS " + version + " InstallDir value is missing or invalid");
                        }
                    }
                    else
                    {
                        Debug.Fail("Could not open VS " + version + " registry key");
                    }
                }
 
            return loadedAssembly;
        }

        private class CoverageSet
        {
			HashSet<String> CoverageFiles { get; private set; }
			HashSet<String> BinaryPaths  { get; private set; }

			private CoverageSet(HashSet<String> coverageFiles, HashSet<String> binaryPaths)
			{
				this.CoverageFiles = coverageFiles;
				this.BinaryPaths = binaryPaths;
			}

			public static CoverageSet FromTrx(IEnumerable<string> trxFiles)
	        {
				HashSet<String> coverageFiles = new HashSet<String>();
				HashSet<String> binaryPaths = new HashSet<String>();

	        	foreach (var filename in trxFiles)
	        	{
					const string ns = "http://microsoft.com/schemas/VisualStudio/TeamTest/2010";
					var doc = new XmlDocument();
					doc.Load(filename);
					XmlNamespaceManager nsmanager = new XmlNamespaceManager(doc.NameTable);
					nsmanager.AddNamespace("x", ns);
					var udrNode = doc.SelectSingleNode("/x:TestRun/x:TestSettings/x:Deployment/@userDeploymentRoot", nsmanager);
					var rdrNode = doc.SelectSingleNode("/x:TestRun/x:TestSettings/x:Deployment/@runDeploymentRoot", nsmanager);
					var msTestCoverageNodes = doc.SelectNodes("/x:TestRun/x:ResultSummary/x:ResultFiles/x:ResultFile/@path", nsmanager);
					var vsTestCoverageNodes = doc.SelectNodes("/x:TestRun/x:ResultSummary/x:CollectorDataEntries/x:Collector[@uri='datacollector://microsoft/CodeCoverage/2.0']/x:UriAttachments/x:UriAttachment/x:A/@href", nsmanager);
					var descriptionNode = doc.SelectSingleNode("/x:TestRun/x:TestSettings/x:Description", nsmanager);
					var userDeploymentRoot = System.IO.Path.GetDirectoryName(filename);
					userDeploymentRoot = udrNode == null? userDeploymentRoot : udrNode.Value;
					var testsFolder = System.IO.Path.Combine(userDeploymentRoot, rdrNode.Value);
					
					foreach (XmlNode node in msTestCoverageNodes)
						coverageFiles.Add(node.Value);
					foreach (XmlNode node in vsTestCoverageNodes)
						coverageFiles.Add(node.Value);
					
					binaryPaths.Add(testsFolder);
				}
				return new CoverageSet(coverageFiles, binaryPaths);
	        }
        }


        /// <summary>
        /// Convert Command Line Argument to IEnumerable.
        /// </summary>
        /// <param name="args">Command Line Argument</param>
        /// <param name="prefix">target prefix</param>
        /// <returns></returns>
        private static IEnumerable<string> ConvertArgToIEnumerable(string[] args, string prefix)
        {
            if (args != null)
                foreach (string arg in args)
                    if ((arg != null) && arg.StartsWith(prefix))
                        yield return arg.Replace(prefix, string.Empty).Trim("\"");
        }

        /// <summary>
        /// Convert Command Line Argument.
        /// </summary>
        /// <param name="args">Command Line Argument</param>
        /// <param name="prefix">target prefix</param>
        /// <returns></returns>
        private static string ConvertArg(string[] args, string prefix)
        {
            if (args != null)

                foreach (string arg in args)
                    if ((arg != null) && arg.StartsWith(prefix))
                        return arg.Replace(prefix, string.Empty).Trim("\"");
            return string.Empty;
        }

        private static void ApplyXsl(string inputPath, string outputPath, string xslPath)
        {
            using (XmlReader reader = new XmlTextReader(inputPath))
            {
                using (XmlWriter writer = new XmlTextWriter(outputPath, Encoding.UTF8))
                {
                    XslCompiledTransform transform = new XslCompiledTransform();
                    transform.Load(xslPath);
                    transform.Transform(reader, writer);
                }
            }
        }

        /// <summary>
        /// check write help.
        /// </summary>
        /// <param name="args">Command Line Arguments</param>
        /// <returns>true:write help</returns>
        private static bool IsConsoleWriteHelp(string[] args)
        {
            if ((args == null) || (args.Length == 0))
                return true;

            foreach (string arg in args)
            {
                if (ARGS_HELP.Equals(arg))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Cosole Write Application Header.
        /// </summary>
        private static void ConsoleWriteHeader()
        {
            Assembly asm = Assembly.GetExecutingAssembly();
            AssemblyCopyrightAttribute asmcpy = (AssemblyCopyrightAttribute)Attribute.GetCustomAttribute(asm, typeof(AssemblyCopyrightAttribute));
            Console.WriteLine("{0} [Version {1}]", asm.GetName().Name, asm.GetName().Version);
            Console.WriteLine(asmcpy.Copyright);
            Console.WriteLine("URL: {0}", SITE_URL);
        }

        /// <summary>
        /// Console Write Application Help. 
        /// </summary>
        private static void ConsoleWriteHelp()
        {
            Console.WriteLine();

            if (System.Threading.Thread.CurrentThread.CurrentCulture.Name.IndexOf("ja") >= 0)
            {
                Console.WriteLine("/in:[ ファイルパス ]       入力対象のファイルパスを指定します。");
                Console.WriteLine("/out:[ ファイルパス ]      出力対象のファイルパスを指定します。");
                Console.WriteLine("/symbols:[ ディレクトリ ]  デバッグシンボルが配置されているディレクトリを指定します。");
                Console.WriteLine("/exedir:[ ディレクトリ ]   カバレッジ取得対象の実行ファイルが配置されているディレクトリを指定します。");
                Console.WriteLine("/xsl:[ ファイルパス ]      XML出力時に変換を行いたい場合、XSL形式のファイルを指定します。");
                Console.WriteLine("/?                         ヘルプを表示します。");
            }
            else
            {
                Console.WriteLine("/in:[ file path ]       specify a file path in which you want to enter.");
                Console.WriteLine("/out:[ file path ]      specify the file path of the output target.");
                Console.WriteLine("/symbols:[ directory ]  specifies the directory where the debug symbols are located.");
                Console.WriteLine("/exedir:[ directory ]   specifies the directory where the executable file to be retrieved coverage is located.");
                Console.WriteLine("/xsl:[ file path ]      If you want to convert the output XML, I want to specify the file format of XSL.");
                Console.WriteLine("/?                      Displays the help.");
            }
        }

        /// <summary>
        /// work file create.
        /// </summary>
        /// <param name="tmpPath">Temporary file path.</param>
        /// <param name="inputPath">Input file path.</param>
        /// <returns>true:create file</returns>
        private static bool CreateWorkFile(string tmpPath, string inputPath)
        {
            if (string.IsNullOrWhiteSpace(inputPath))
            {
                Console.WriteLine("input file not input.");
                return false;
            }

            if (File.Exists(inputPath))
            {
                File.Copy(inputPath, tmpPath, true);
                Console.WriteLine("input file: {0}", inputPath);
            }
            else
            {
                string dirPath     = Path.GetDirectoryName(inputPath);
                string filePattern = Path.GetFileName(inputPath);

                if (string.IsNullOrWhiteSpace(filePattern))
                {
                    Console.WriteLine("File pattern is a non-input. ({0})", inputPath);
                    return false;
                }

                if (string.IsNullOrWhiteSpace(dirPath))
                    dirPath = ".";

                string[] paths = Directory.GetFiles(dirPath, filePattern);

                if (!paths.Any())
                {
                    Console.WriteLine("Search results by file pattern can not be found.");
                    return false;
                }
                else
                {
                    if (paths.Length == 1)
                        File.Copy(paths[0], tmpPath, true);
                    else
                    {
                        string firstPath  = paths[0];
                        string outputPath = string.Empty;

                        for (int i = 1; i < paths.Length; i++)
                        {
                            if (i == paths.Length - 1)
                                outputPath = tmpPath;
                            else
                                outputPath = Path.ChangeExtension(Path.GetTempFileName(), FILE_EXTENSION_COVERAGE);

                            CoverageInfo.MergeCoverageFiles(firstPath, paths[i], outputPath, true);

                            firstPath = outputPath;
                        }
                    }
                }
            }

            return true;
        }
    }
}
