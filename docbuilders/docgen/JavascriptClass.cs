using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace docgen
{
  class JavascriptClass : BindingClass
  {
    public JavascriptClass(string name) : base(name)
    {
    }

    public static void Write(string path)
    {
      StringBuilder js = new StringBuilder();
      var keys = BindingClass.AllJavascriptClasses.Keys.ToList();
      keys.Sort();

      foreach (var key in keys)
      {
        var jsclass = GetJS(key);
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
        if (jsclass.Constructors.Count == 0)
          js.AppendLine(" * @hideconstructor");
        js.AppendLine(" */");
        js.AppendLine($"class {jsclass.ClassName} {{");
        foreach (var constructor in jsclass.Constructors)
        {
          var c = rhcommon.GetConstructor(constructor);
          if (c == null)
            continue;
          ConstructorDeclarationSyntax constructorDecl = c.Item1;
          doccomment = c.Item2;

          if (constructorDecl != null)
          {
            List<string> paramNames = null;
            js.AppendLine("  /**");
            if (doccomment != null)
            {
              string s = DocCommentToJsDoc(doccomment, null, constructorDecl.ParameterList, out paramNames);
              js.Append(s);
            }
            js.AppendLine("   */");
            js.Append("  constructor(");
            if (paramNames != null)
            {
              string parameters = "";
              foreach (var p in paramNames)
              {
                parameters += p + ",";
              }
              if (!string.IsNullOrWhiteSpace(parameters))
              {
                parameters = parameters.Substring(0, parameters.Length - 1);
                js.Append(parameters);
              }
            }
            js.AppendLine("){}");
          }
        }
        foreach (var (isStatic, method) in jsclass.Methods)
        {
          MethodDeclarationSyntax methodDecl = null;
          doccomment = null;
          for (int i = 0; i < rhcommon.Methods.Count; i++)
          {
            if (method.Equals(rhcommon.Methods[i].Item1.Identifier.ToString(), StringComparison.InvariantCultureIgnoreCase))
            {
              methodDecl = rhcommon.Methods[i].Item1;
              doccomment = rhcommon.Methods[i].Item2;
              break;
            }
          }

          List<string> paramNames = new List<string>();
          if (doccomment == null)
          {
            js.AppendLine("  /** ... */");
          }
          else
          {
            js.AppendLine("  /**");
            string s = DocCommentToJsDoc(doccomment, methodDecl, methodDecl.ParameterList, out paramNames);
            js.Append(s);
            js.AppendLine("   */");
          }

          string parameters = "";
          foreach (var p in paramNames)
          {
            parameters += p + ",";
          }
          if (!string.IsNullOrEmpty(parameters))
            parameters = parameters.Substring(0, parameters.Length - 1);

          if (isStatic)
            js.Append($"  static {method}({parameters}) {{");
          else
            js.Append($"  {method}({parameters}) {{");
          js.AppendLine("  }");
        }
        foreach (var prop in jsclass.Properties)
        {
          PropertyDeclarationSyntax propDecl = null;
          doccomment = null;
          for (int i = 0; i < rhcommon.Properties.Count; i++)
          {
            if (prop.Equals(rhcommon.Properties[i].Item1.Identifier.ToString(), StringComparison.InvariantCultureIgnoreCase))
            {
              propDecl = rhcommon.Properties[i].Item1;
              doccomment = rhcommon.Properties[i].Item2;
              break;
            }
          }
          js.AppendLine("  /**");
          if (doccomment != null)
          {
            string comment = doccomment.ToString();
            comment = comment.Replace("///", "");
            js.AppendLine($"   * {comment}");
          }
          if (propDecl != null)
          {
            js.AppendLine($"   * @type {{{ToJavascriptType(propDecl.Type.ToString())}}}");
          }
          js.AppendLine("   */");
          js.AppendLine($"  get {prop}() {{ return null;}}");
        }
        js.AppendLine("}");
      }
      System.IO.File.WriteAllText(path, js.ToString());
    }


    static string ToJavascriptType(string type)
    {
      if (type.Equals("Point3d") || type.Equals("Vector3d"))
        return "Array.<x,y,z>";
      return type;
    }

    static string DocCommentToJsDoc(DocumentationCommentTriviaSyntax doccomment,
      MethodDeclarationSyntax methodDecl,
      ParameterListSyntax parameters,
      out List<string> paramNames)
    {
      paramNames = new List<string>();
      StringBuilder js = new StringBuilder();
      string comment = doccomment.ToString();
      comment = comment.Replace("///", "");
      var doc = new System.Xml.XmlDocument();
      doc.LoadXml("<doc>" + comment + "</doc>");
      var nodes = doc.FirstChild.ChildNodes;
      foreach (var node in nodes)
      {
        var element = node as System.Xml.XmlElement;
        string elementText = element.InnerText.Trim();
        if (string.IsNullOrWhiteSpace(elementText))
          continue;
        if (element.Name.Equals("summary", StringComparison.OrdinalIgnoreCase))
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
          for (int j = 0; j < parameters.Parameters.Count; j++)
          {
            if (paramName.Equals(parameters.Parameters[j].Identifier.ToString()))
            {
              paramType = parameters.Parameters[j].Type.ToString();
              break;
            }
          }
          js.AppendLine($"   * @param {{{ToJavascriptType(paramType)}}} {paramName} {elementText}");
        }
      }
      return js.ToString();
    }
  }
}
