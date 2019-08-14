using System;

namespace docgen
{
    class Program
    {
        static void Main(string[] args)
        {
            // read all RhinoCommon classes into memory
            const string rhinocommonPath = @"C:\dev\github\mcneel\rhino\src4\DotNetSDK\rhinocommon\dotnet";
            Console.WriteLine("[Parse RhinoCommon source]");
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            RhinoCommonClass.BuildClassDictionary(rhinocommonPath);
            Console.ResetColor();

            Console.WriteLine("[Parse C++ Bindings]");
            Console.ForegroundColor = ConsoleColor.Green;
            const string bindingPath = @"../../../../src/bindings";
            BindingClass.BuildClassDictionary(bindingPath);
            Console.ResetColor();
            Console.WriteLine("[END PARSE]");


            Console.ForegroundColor = ConsoleColor.Blue;
            //Console.WriteLine("Writing javascript");
            //JavascriptClass.Write("rh3dm_temp.js");
            Console.WriteLine("Writing python");
            PythonClass.Write();
        }
    }
}
