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
    public static Dictionary<string, PythonClass> AllPythonClasses { get; } = new Dictionary<string, PythonClass>();

    public static void BuildClassDictionary(string sourcePath)
    {
      BindingClass activeClass = null;
      foreach (var file in AllSourceFiles(sourcePath))
      {
        string[] lines = System.IO.File.ReadAllLines(file);
        for(int i=0; i<lines.Length; i++)
        {
          string line = lines[i].Trim();
          if (line.StartsWith("class_"))
          {
            string name = (line.Split(new char[] { '"' }))[1];
            var activeJavascriptClass = new JavascriptClass(name);
            int baseIndex = line.IndexOf("base<BND_");
            if( baseIndex>0 )
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

          if( line.StartsWith("py::class_"))
          {
            string name = (line.Split(new char[] { '"' }))[1];
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

          if (activeClass != null)
          {
            if(line.StartsWith(".constructor"))
            {
              int startIndex = line.IndexOf("<");
              int endIndex = line.IndexOf(">", startIndex);
              string types = line.Substring(startIndex + 1, endIndex - startIndex - 1);
              activeClass.AddConstructor(types);
              continue;
            }
            if ( line.IndexOf("py::init<") > 0)
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

            if(line.StartsWith(".function") || line.StartsWith(".def("))
            {
              string funcName = (line.Split(new char[] { '"' }))[1];
              activeClass.AddMethod(funcName, false);
            }

            if(line.StartsWith(".class_function") ||
              line.StartsWith(".def_static"))
            {
              string funcName = (line.Split(new char[] { '"' }))[1];
              activeClass.AddMethod(funcName, true);
            }
            if (line.StartsWith(";"))
            {
              activeClass = null;
            }
          }
        }
        Console.WriteLine($"parse: {file}");
      }
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

    public void AddMethod(string name, bool isStatic)
    {
      Methods.Add(new Tuple<bool, string>(isStatic, name));
    }

    public string[] GetParamNames(ParameterListSyntax p, bool pythonSafe = false)
    {
      List<string> paramNames = new List<string>();
      for( int i=0; i<p.Parameters.Count; i++ )
      {
        var parameter = p.Parameters[i].Identifier.ToString();
        if( pythonSafe)
        {
          if (parameter.Equals("from"))
            parameter = "_" + parameter;
        }
        paramNames.Add(parameter);
      }
      return paramNames.ToArray();
    }

    public List<Tuple<bool, string>> Methods { get; } = new List<Tuple<bool, string>>();
    public List<string> Properties { get; } = new List<string>();
    public List<string[]> Constructors { get; } = new List<string[]>();
  }

}
