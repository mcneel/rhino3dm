using System;
using System.Collections.Generic;

namespace docgen
{
  class BindingClass
  {
    protected BindingClass(string className)
    {
      ClassName = className;
    }

    public string ClassName { get; set; }

    public static Dictionary<string, JavascriptClass> AllJavascriptClasses { get; private set; }

    public static void BuildClassDictionary(string sourcePath)
    {
      AllJavascriptClasses = new Dictionary<string, JavascriptClass>();
      JavascriptClass activeJavascriptClass = null;
      foreach (var file in AllSourceFiles(sourcePath))
      {
        string[] lines = System.IO.File.ReadAllLines(file);
        for(int i=0; i<lines.Length; i++)
        {
          string line = lines[i].Trim();
          if (line.StartsWith("class_"))
          {
            string name = (line.Split(new char[] { '"' }))[1];
            activeJavascriptClass = new JavascriptClass(name);
            int baseIndex = line.IndexOf("base<BND_");
            if( baseIndex>0 )
            {
              int baseEnd = line.IndexOf(">", baseIndex);
              baseIndex += "base<BND_".Length;
              string baseClass = line.Substring(baseIndex, baseEnd - baseIndex);
              activeJavascriptClass.BaseClass = baseClass;
            }
            AllJavascriptClasses.Add(name.ToLowerInvariant(), activeJavascriptClass);
            continue;
          }
          if(activeJavascriptClass != null)
          {
            if(line.StartsWith(".constructor"))
            {
              int startIndex = line.IndexOf("<");
              int endIndex = line.IndexOf(">", startIndex);
              string types = line.Substring(startIndex + 1, endIndex - startIndex - 1);
              activeJavascriptClass.AddConstructor(types);
            }
            if(line.StartsWith(".property"))
            {
              string propName = (line.Split(new char[] { '"' }))[1];
              activeJavascriptClass.AddProperty(propName);
            }
            if(line.StartsWith(".function"))
            {
              string funcName = (line.Split(new char[] { '"' }))[1];
              activeJavascriptClass.AddMethod(funcName, false);
            }
            if(line.StartsWith(".class_function"))
            {
              string funcName = (line.Split(new char[] { '"' }))[1];
              activeJavascriptClass.AddMethod(funcName, true);
            }
            if (line.StartsWith(";"))
            {
              activeJavascriptClass = null;
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


    static IEnumerable<string> AllSourceFiles(string sourcePath)
    {
      foreach (string file in System.IO.Directory.EnumerateFiles(sourcePath, "*.cpp", System.IO.SearchOption.AllDirectories))
      {
        if (file.Contains("\\obj\\"))
          continue;
        yield return file;
      }
    }
  }

  class JavascriptClass : BindingClass
  {
    public JavascriptClass(string name) : base(name)
    {
    }

    public string BaseClass { get; set; }

    public void AddConstructor(string types)
    {
      string[] t = types.Split(new char[] { ',' });
      for(int i=0; i<t.Length; i++)
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

    public List<Tuple<bool, string>> Methods { get; } = new List<Tuple<bool, string>>();
    public List<string> Properties { get; } = new List<string>();
    public List<string[]> Constructors { get; } = new List<string[]>();
  }
}
