using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;

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
      Console.WriteLine("Writing javascript");

      StringBuilder js = new StringBuilder();
      var keys = BindingClass.AllJavascriptClasses.Keys.ToList();
      keys.Sort();
      
      foreach(var key in keys)
      {
        var jsclass = BindingClass.GetJS(key);
        var rhcommon = RhinoCommonClass.Get(key);
        var doccomment = rhcommon.DocComment;
        js.AppendLine("/**");
        if (doccomment == null)
        {
          js.AppendLine($" * {jsclass.ClassName}");
        }
        else
        {
          string comment = doccomment.ToString();
          comment = comment.Replace("///", "");
          js.Append(comment);
        }
        if (!string.IsNullOrEmpty(jsclass.BaseClass))
          js.AppendLine($" * @extends {jsclass.BaseClass}");
        js.AppendLine(" */");
        js.AppendLine($"class {jsclass.ClassName} {{");
        foreach(var method in jsclass.Methods)
        {
          MethodDeclarationSyntax methodDecl = null;
          doccomment = null;
          for(int i=0; i<rhcommon.Methods.Count; i++)
          {
            if( method.Equals(rhcommon.Methods[i].Item1.Identifier.ToString(), StringComparison.InvariantCultureIgnoreCase))
            {
              methodDecl = rhcommon.Methods[i].Item1;
              doccomment = rhcommon.Methods[i].Item2;
              break;
            }
          }

          List<string> paramNames = new List<string>();
          if ( doccomment==null )
          {
            js.AppendLine("  /** ... */");
          }
          else
          {
            string comment = doccomment.ToString();
            comment = comment.Replace("///", "");
            var doc = new System.Xml.XmlDocument();
            doc.LoadXml("<doc>"+comment+"</doc>");
            js.AppendLine("  /**");
            var nodes = doc.FirstChild.ChildNodes;
            foreach( var node in nodes)
            {
              var element = node as System.Xml.XmlElement;
              string elementText = element.InnerText.Trim();
              if (string.IsNullOrWhiteSpace(elementText))
                continue;
              if( element.Name.Equals("summary", StringComparison.OrdinalIgnoreCase))
              {
                js.AppendLine($"   * @description {elementText}");
              }
              else if (element.Name.Equals("returns", StringComparison.OrdinalIgnoreCase))
              {
                var returnType = methodDecl.ReturnType;

                js.AppendLine($"   * @returns {{{ToJavascriptType(returnType.ToString())}}} {elementText}");
              }
              else if (element.Name.Equals("param", StringComparison.OrdinalIgnoreCase))
              {
                string paramType = "";
                string paramName = element.Attributes["name"].Value;
                paramNames.Add(paramName);
                for(int j=0; j<methodDecl.ParameterList.Parameters.Count; j++ )
                {
                  if( paramName.Equals(methodDecl.ParameterList.Parameters[j].Identifier.ToString()))
                  {
                    paramType = methodDecl.ParameterList.Parameters[j].Type.ToString();
                    break;
                  }
                }
                js.AppendLine($"   * @param {{{ToJavascriptType(paramType)}}} {paramName} {elementText}");
              }
              //else
              //  js.AppendLine(node.ToString());
            }
            js.AppendLine("   */");
          }

          string parameters = "";
          foreach(var p in paramNames)
          {
            parameters += p + ",";
          }
          if (!string.IsNullOrEmpty(parameters))
            parameters = parameters.Substring(0, parameters.Length - 1);

          js.Append($"  {method}({parameters}) {{");
          js.AppendLine("  }");
        }
        js.AppendLine("}");
      }

      System.IO.File.WriteAllText("rh3dm_temp.js", js.ToString());
      /*

      // just do a small number of classes to get started
      string[] filter = new string[] {
                ".Mesh", ".Brep", ".Curve", ".BezierCurve", ".Extrusion", ".NurbsCurve"
            };

      var js = new JavascriptClient();
      js.Write(RhinoCommonClasses.AllClasses, "compute.rhino3d.js", filter);
      Console.WriteLine("Writing python client");
      var py = new PythonClient();
      py.Write(RhinoCommonClasses.AllClasses, "", filter);
      Console.WriteLine("Writing C# client");
      var cs = new DotNetClient();
      cs.Write(RhinoCommonClasses.AllClasses, "RhinoCompute.cs", filter);
      */
    }

    static string ToJavascriptType(string type)
    {
      if (type.Equals("Point3d") || type.Equals("Vector3d"))
        return "Array.<x,y,z>";
      return type;
    }
  }
}
