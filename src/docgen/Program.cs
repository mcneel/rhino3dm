using System;
using System.IO;

namespace docgen
{
    class Program
    {
        static void Main(string[] args)
        {
            // read all RhinoCommon classes into memory
            string rhinocommonPath = SourceDirectory("dotnet");
            Console.WriteLine("[Parse RhinoCommon source]");
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            RhinoCommonClass.BuildClassDictionary(rhinocommonPath);
            Console.ResetColor();

            Console.WriteLine("[Parse C++ Bindings]");
            Console.ForegroundColor = ConsoleColor.Green;
            string bindingPath = SourceDirectory("bindings");
            BindingClass.BuildDictionary(bindingPath);
            Console.ResetColor();
            Console.WriteLine("[END PARSE]");

            string dir = System.IO.Directory.GetCurrentDirectory();
            int index = dir.IndexOf("docgen");
            dir = dir.Substring(0, index + "docgen".Length);
            string outDirName = Path.Combine(dir, "out");
            var outDir = new DirectoryInfo(outDirName);
            if (!outDir.Exists)
                outDir.Create();

            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("Writing javascript API help");
            JavascriptClass.GenerateApiHelp(Path.Combine(outDir.FullName, "js_apidocs"));
            Console.WriteLine("Writing javascript typescript definition file");
            JavascriptClass.GenerateTypescriptDefinition(Path.Combine(outDir.FullName, "js_tsdef"));

            Console.WriteLine("Writing python API help");
            PythonClass.GenerateApiHelp(Path.Combine(outDir.FullName, "py_apidocs"));
            Console.WriteLine("Writing python type stubs file");
            PythonClass.GenerateTypeStubs(Path.Combine(outDir.FullName, "py_stubs"));
        }

        static string SourceDirectory(string subdirectoryName)
        {
            string dir = System.IO.Directory.GetCurrentDirectory();
            int index = dir.IndexOf("docgen");
            dir = dir.Substring(0, index);
            string path = System.IO.Path.Combine(dir, subdirectoryName);
            return path;
        }
    }
}
