using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace docgen
{
    class BindingClass
    {
        protected BindingClass(string className)
        {
            ClassName = className;
        }

        public string ClassName { get; set; }

        public static Dictionary<string, JavascriptClass> AllJavascriptClasses { get; } = new Dictionary<string, JavascriptClass>();
        public static Dictionary<string, JavascriptEnum> AllJavascriptEnums { get; } = new Dictionary<string, JavascriptEnum>();
        public static Dictionary<string, PythonClass> AllPythonClasses { get; } = new Dictionary<string, PythonClass>();

        public static void BuildDictionary(string sourcePath)
        {
            BindingClass activeClass = null;
            foreach (var file in AllSourceFiles(sourcePath))
            {
                string[] lines = System.IO.File.ReadAllLines(file);
                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i].Trim();
                    if (line.StartsWith("class_"))
                    {
                        string name = (line.Split(new char[] { '"' }))[1];
                        if (name.StartsWith("__"))
                            continue;
                        var activeJavascriptClass = new JavascriptClass(name);
                        int baseIndex = line.IndexOf("base<BND_");
                        if (baseIndex > 0)
                        {
                            int baseEnd = line.IndexOf(">", baseIndex);
                            baseIndex += "base<BND_".Length;
                            string baseClass = line.Substring(baseIndex, baseEnd - baseIndex);
                            activeJavascriptClass.BaseClass = baseClass;
                        }
                        AllJavascriptClasses.Add(name.ToLowerInvariant(), activeJavascriptClass);
                        activeClass = activeJavascriptClass;
                        continue;
                    }

                    if (line.StartsWith("py::class_"))
                    {
                        string name = (line.Split(new char[] { '"' }))[1];
                        if (name.StartsWith("__"))
                            continue;
                        var activePythonClass = new PythonClass(name);
                        int baseIndex = line.IndexOf(",");
                        if (baseIndex > 0)
                            baseIndex = line.IndexOf("BND_", baseIndex);
                        if (baseIndex > 0)
                        {
                            int baseEnd = line.IndexOf(">", baseIndex);
                            baseIndex += "BND_".Length;
                            if (baseEnd > baseIndex)
                            {
                                string baseClass = line.Substring(baseIndex, baseEnd - baseIndex);
                                activePythonClass.BaseClass = baseClass;
                            }
                        }
                        AllPythonClasses.Add(name.ToLowerInvariant(), activePythonClass);
                        activeClass = activePythonClass;
                        continue;
                    }

                    if (line.StartsWith("enum_"))
                    {
                        string name = (line.Split(new char[] { '"' }))[1];
                        if (name.StartsWith("__"))
                            continue;
                        var jsenum = new JavascriptEnum(name);
                        AllJavascriptEnums.Add(name.ToLowerInvariant(), jsenum);
                        activeClass = jsenum;
                        continue;
                    }

                    if (activeClass != null)
                    {
                        if (line.StartsWith(".constructor"))
                        {
                            int startIndex = line.IndexOf("<");
                            int endIndex = line.IndexOf(">", startIndex);
                            string types = line.Substring(startIndex + 1, endIndex - startIndex - 1);
                            activeClass.AddConstructor(types);
                            continue;
                        }
                        if (line.IndexOf("py::init<") > 0)
                        {
                            int startIndex = line.IndexOf("py::init<") + "py::init<".Length;
                            int endIndex = line.IndexOf(">", startIndex);
                            string types = line.Substring(startIndex, endIndex - startIndex);
                            activeClass.AddConstructor(types);
                            continue;
                        }

                        if (line.StartsWith(".property")
                          || line.StartsWith(".def_property"))
                        {
                            string propName = (line.Split(new char[] { '"' }))[1];
                            activeClass.AddProperty(propName);
                            continue;
                        }

                        if (line.Contains("py::self"))
                            continue;

                        if (line.StartsWith(".function") || line.StartsWith(".def("))
                        {
                            string funcName = (line.Split(new char[] { '"' }))[1];
                            string cppFunction = GetCppFunctionName(line);
                            activeClass.AddMethod(funcName, false, cppFunction, GetArgList(line));
                        }

                        if (line.StartsWith(".class_function") || line.StartsWith(".def_static"))
                        {
                            string funcName = (line.Split(new char[] { '"' }))[1];
                            string cppFunction = GetCppFunctionName(line);
                            activeClass.AddMethod(funcName, true, cppFunction, GetArgList(line));
                        }
                        if (line.StartsWith(";"))
                        {
                            activeClass = null;
                        }
                        if( activeClass is JavascriptEnum && line.StartsWith(".value"))
                        {
                            string enumValue = (line.Split(new char[] { '"' }))[1];
                            (activeClass as JavascriptEnum).Elements.Add(enumValue);
                        }
                    }
                }
                Console.WriteLine($"parse: {file}");
            }
        }

        static string[] GetArgList(string line)
        {
            List<string> args = new List<string>();
            int index = line.IndexOf("py::arg");
            while(index > 0)
            {
                int start = line.IndexOf("\"", index);
                int end = line.IndexOf("\"", start+1);
                string argName = line.Substring(start + 1, end - start - 1);
                args.Add(argName);
                index = line.IndexOf("py::arg", index+1);
            }
            return args.ToArray();
        }

        static string GetCppFunctionName(string line)
        {
            int start = line.IndexOf('&');
            if (-1 == start)
                return string.Empty;
            int end = line.IndexOf(',', start);
            if (-1 == end)
                end = line.IndexOf(')', start);
            string function = line.Substring(start, end - start).Trim();
            return function;
        }

        public static JavascriptClass GetJS(string className)
        {
            className = className.ToLowerInvariant();
            return AllJavascriptClasses[className];
        }

        public static PythonClass GetPY(string className)
        {
            className = className.ToLowerInvariant();
            return AllPythonClasses[className];
        }

        static IEnumerable<string> AllSourceFiles(string sourcePath)
        {
            foreach (string file in System.IO.Directory.EnumerateFiles(sourcePath, "*.cpp", System.IO.SearchOption.AllDirectories))
            {
                if (file.Contains("\\obj\\"))
                    continue;
                yield return file;
            }
        }

        public string BaseClass { get; set; }

        public void AddConstructor(string types)
        {
            string[] t = types.Split(new char[] { ',' });
            for (int i = 0; i < t.Length; i++)
            {
                t[i] = t[i].Trim();
                if (t[i].Equals("ON_3dPoint"))
                    t[i] = "Point3d";
                if (t[i].Equals("ON_3dVector"))
                    t[i] = "Vector3d";
            }
            Constructors.Add(t);
        }

        public void AddProperty(string name)
        {
            Properties.Add(name);
        }

        public virtual void AddMethod(string name, bool isStatic, string cppFunction, string[] argList)
        {
            var method = new Method
            {
                Name = name,
                IsStatic = isStatic,
                CppFunction = cppFunction,
                ArgList = argList
            };
            Methods.Add(method);
        }

        public string[] GetParamNames(ParameterListSyntax p, bool pythonSafe = false)
        {
            List<string> paramNames = new List<string>();
            for (int i = 0; i < p.Parameters.Count; i++)
            {
                var parameter = p.Parameters[i].Identifier.ToString();
                if (pythonSafe)
                {
                    if (parameter.Equals("from"))
                        parameter = "_" + parameter;
                }
                paramNames.Add(parameter);
            }
            return paramNames.ToArray();
        }

        public List<Method> Methods { get; } = new List<Method>();
        public List<string> Properties { get; } = new List<string>();
        public List<string[]> Constructors { get; } = new List<string[]>();

        public class Method
        {
            public bool IsStatic { get; set; }
            public string Name { get; set; }
            public string CppFunction { get; set; }
            public string[] ArgList { get; set; }
        }
    }
}
