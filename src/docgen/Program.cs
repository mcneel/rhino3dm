using System;
using System.IO;

namespace docgen
{
    class Program
    {
        static void Main(string[] args)
        {
            // read all RhinoCommon classes into memory
            const string rhinocommonPath = @"../../dotnet";
            Console.WriteLine("[Parse RhinoCommon source]");
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            RhinoCommonClass.BuildClassDictionary(rhinocommonPath);
            Console.ResetColor();

            Console.WriteLine("[Parse C++ Bindings]");
            Console.ForegroundColor = ConsoleColor.Green;
            const string bindingPath = @"../../bindings";
            BindingClass.BuildDictionary(bindingPath);
            Console.ResetColor();
            Console.WriteLine("[END PARSE]");


            var outDir = new DirectoryInfo("../out");
            if (!outDir.Exists)
                outDir.Create();

            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("Writing javascript API help");
            JavascriptClass.GenerateApiHelp(Path.Combine(outDir.FullName, "js_apidocs"));
            Console.WriteLine("Writing javascript typescript definition file");
            JavascriptClass.GenerateTypescriptDefinition(Path.Combine(outDir.FullName, "js_tsdef"));

            Console.WriteLine("Writing python");
            PythonClass.Write(Path.Combine(outDir.FullName, "py_apidocs"));
        }
    }
}
