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
using Microsoft.Win32;

namespace CoverageConverter
{
    class Program
    {
        /// <summary>site url</summary>
        private const string SITE_URL = "https://github.com/nilleb/CoverageConverter";

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
                ConsoleHelper.ConsoleWriteHeader();

                if (ConsoleHelper.IsConsoleWriteHelp(args))
                {
                    ConsoleHelper.ConsoleWriteHelp();
                    return 0;
                }

				IEnumerable<string> trxFiles  = ConsoleHelper.ConvertArgToIEnumerable(args, ARGS_PREFIX_TRX_PATH);
                
				string inputPath  = ConsoleHelper.ConvertArg(args, ARGS_PREFIX_INPUT_PATH);
				string outputPath = ConsoleHelper.ConvertArg(args, ARGS_PREFIX_OUTPUT_PATH);
				string symbolsDir = ConsoleHelper.ConvertArg(args, ARGS_PREFIX_SYMBOLS_DIR);
				string exeDir     = ConsoleHelper.ConvertArg(args, ARGS_PREFIX_EXE_DIR);
				IEnumerable<string> xslPaths    = ConsoleHelper.ConvertArgToIEnumerable(args, ARGS_PREFIX_XSL_PATH);

                var cs = CoverageSet.FromTrx(trxFiles);

				if (!string.IsNullOrWhiteSpace(inputPath))
					cs.Add(CoverageSet.FromCoverageFile(inputPath, symbolsDir, exeDir));

                foreach (var xsl in xslPaths)
                    ApplyXsl(data, Path.ChangeExtension(outputWk, Path.GetFileNameWithoutExtension(xsl) + ".xml"), xsl);

                result = true;

                Console.WriteLine("convert success.");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
			return result ? 0 : 1;
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

		static readonly string[] vs_versions = new [] {"8.0", "9.0", "10.0", "11.0", "12.0", "14.0"};
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
     
						var vsInstallFolder = obj as String;
						if (vsInstallFolder != null) {
							String privateAssembliesDir = Path.Combine (vsInstallFolder, "PrivateAssemblies");
							String assemblyFile = Path.Combine (privateAssembliesDir, assemblyName + ".dll");
     
							loadedAssembly = Assembly.LoadFile (assemblyFile);
							break;
						} else {
							Debug.Fail ("VS " + version + " InstallDir value is missing or invalid");
						}
                    }
                    else
                    {
                        Debug.Fail("Could not open VS " + version + " registry key");
                    }
                }
 
            return loadedAssembly;
        }

        public class CoverageSet
        {
			public HashSet<String> CoverageFiles { get; private set; }
			public HashSet<String> BinaryPaths  { get; private set; }

			private CoverageSet(HashSet<String> coverageFiles, HashSet<String> binaryPaths)
			{
				this.CoverageFiles = coverageFiles;
				this.BinaryPaths = binaryPaths;
			}

			public static CoverageSet FromCoverageFile(string coverageFile, params string[] binaryPath)
			{
				var coverageFiles = new HashSet<String>();
				var binaryPaths = new HashSet<String>();
				coverageFiles.Add(coverageFile);
				foreach (var binPath in binaryPath)
					if (!string.IsNullOrEmpty(binPath))
					binaryPaths.Add(binPath);
				return new CoverageSet(coverageFiles, binaryPaths);
			}

			public CoverageSet AddBinaryPath(params string[] binaryPaths)
			{
				foreach (var binPath in binaryPath)
					BinaryPaths.Add(binPath);
				return this;
			}

			public static CoverageSet FromTrx(IEnumerable<string> trxFiles)
	        {
				var coverageFiles = new HashSet<String>();
				var binaryPaths = new HashSet<String>();

	        	foreach (var filename in trxFiles)
	        	{
					const string ns = "http://microsoft.com/schemas/VisualStudio/TeamTest/2010";
					var doc = new XmlDocument();
					doc.Load(filename);
					var nsmanager = new XmlNamespaceManager(doc.NameTable);
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

			public void Add(CoverageSet coverageSet) {
				foreach (var file in coverageSet.CoverageFiles)
					CoverageFiles.Add (file);
				foreach (var path in coverageSet.BinaryPaths)
					BinaryPaths.Add (path);
			}

			public void ConvertToXml(string outputFile)
			{
				CoverageInfo cumulated = null;
				foreach (var cf in CoverageFiles) {
					CoverageInfo ci = CoverageInfo.CreateFromFile(cf, BinaryPaths, BinaryPaths);
					cumulated == null ? ci : CoverageInfo.Join(cumulated, ci);
				}
				CoverageDS data = cumulated.BuildDataSet();
				data.WriteXml(outputFile);
			}
        }
			
		public static void ApplyXsl(string inputPath, string outputPath, IEnumerable<string> xslPaths)
		{
			//var xslHash = new HashSet<string> (xslPaths);
			var xslA = xslPaths.ToArray ();
			var lastXsl = xslA.Last();
			//var others = xslHash.Except(lastXsl);
			var others = xslA.SkipLastN(1);
			var tempFiles = new List<string>();
			var input = inputPath;
			var temp = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
			foreach (var xsl in others) {
				ApplyXsl(input, temp, xsl);
				tempFiles.Add (temp);
				input = temp;
				temp = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
			}
			ApplyXsl (temp, outputPath, lastXsl);
		}

        public static void ApplyXsl(string inputPath, string outputPath, string xslPath)
        {
            using (XmlReader reader = new XmlTextReader(inputPath))
            {
                using (XmlWriter writer = new XmlTextWriter(outputPath, Encoding.UTF8))
                {
                    var transform = new XslCompiledTransform();
                    transform.Load(xslPath);
                    transform.Transform(reader, writer);
                }
            }
        }
    }

	public static class LinqExtensions
	{
		public static IEnumerable<T> SkipLastN<T>(this IEnumerable<T> source, int n) {
			var  it = source.GetEnumerator();
			bool hasRemainingItems = false;
			var  cache = new Queue<T>(n + 1);

			do {
				if (hasRemainingItems = it.MoveNext()) {
					cache.Enqueue(it.Current);
					if (cache.Count > n)
						yield return cache.Dequeue();
				}
			} while (hasRemainingItems);
		}
	}
}
