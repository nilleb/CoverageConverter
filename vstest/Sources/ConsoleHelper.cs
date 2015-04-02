using System.Collections.Generic;

namespace CoverageConverter
{
    internal class ConsoleHelper
    {
	        /// <summary>
        /// Convert Command Line Argument to IEnumerable.
        /// </summary>
        /// <param name="args">Command Line Argument</param>
        /// <param name="prefix">target prefix</param>
        /// <returns></returns>
        public static IEnumerable<string> ConvertArgToIEnumerable(string[] args, string prefix)
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
        public static string ConvertArg(string[] args, string prefix)
        {
            if (args != null)

                foreach (string arg in args)
                    if ((arg != null) && arg.StartsWith(prefix))
                        return arg.Replace(prefix, string.Empty).Trim("\"");
            return string.Empty;
        }
        
        /// <summary>
        /// check write help.
        /// </summary>
        /// <param name="args">Command Line Arguments</param>
        /// <returns>true:write help</returns>
        public static bool IsConsoleWriteHelp(string[] args)
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
        public static void ConsoleWriteHeader()
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
        public static void ConsoleWriteHelp()
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
    }
}