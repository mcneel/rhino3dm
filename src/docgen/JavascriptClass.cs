using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace docgen
{
    class JavascriptClass : BindingClass
    {
        public JavascriptClass(string name) : base(name)
        {
        }

        PythonClass _sisterPythonClass;

        public override void AddMethod(string name, bool isStatic, string[] argList)
        {
            if( _sisterPythonClass==null)
            {
                _sisterPythonClass = AllPythonClasses[this.ClassName.ToLowerInvariant()];
            }
            for( int i=0; i<_sisterPythonClass.Methods.Count; i++)
            {
                var pyMethod = _sisterPythonClass.Methods[i];
                if( pyMethod.Item2.Equals(name, StringComparison.InvariantCultureIgnoreCase))
                {
                    argList = pyMethod.Item3;
                    break;
                }
            }
            base.AddMethod(name, isStatic, argList);
        }

        /// <summary>
        /// Create API documentation file(s). Currently javascript help is created
        /// by first creating a fake javascript file that mocks rhino3dm wasm and
        /// then runing jsdoc on this file.
        /// TODO: My plan is to switch this over to the RST+sphinx technique that
        /// I would like to use for all languages
        /// </summary>
        /// <param name="directory">Where to write the API help</param>
        public static void GenerateApiHelp(string directory)
        {
            if (!System.IO.Directory.Exists(directory))
                System.IO.Directory.CreateDirectory(directory);

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

                        // jsdoc doesn't allow multiple constructors. Skip constructor
                        // overloads for now
                        break;
                    }
                }
                foreach (var (isStatic, method, args) in jsclass.Methods)
                {
                    MethodDeclarationSyntax methodDecl = null;
                    doccomment = null;

                    if( method.Equals("rotation"))
                    {
                        int bh = 0;
                    }

                    for (int i = 0; i < rhcommon.Methods.Count; i++)
                    {
                        if (rhcommon.Methods[i].Item1.ParameterList.Parameters.Count != args.Length)
                            continue;

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
                        string comment = DocCommentToJsDoc(doccomment, propDecl);
                        js.Append(comment);
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

            string path = System.IO.Path.Combine(directory, "rh3dm_temp.js");
            System.IO.File.WriteAllText(path, js.ToString());
        }


        public static void GenerateTypescriptDefinition(string directory)
        {
            if (!System.IO.Directory.Exists(directory))
                System.IO.Directory.CreateDirectory(directory);

            var js = new StringBuilder();
            js.AppendLine("declare module 'rhino3dm' {");

            // Declare default export promise
            js.AppendLine();
            js.AppendLine("\texport default function rhino3dm() : Promise<RhinoModule>;");

            // Declare enums
            var keys = BindingClass.AllJavascriptEnums.Keys.ToList();
            keys.Sort();
            foreach(var key in keys)
            {
                js.AppendLine();
                JavascriptEnum jsenum = BindingClass.AllJavascriptEnums[key];
                js.AppendLine($"\tenum {jsenum.Name} {{");
                for( int i=0; i<jsenum.Elements.Count; i++)
                {
                    if (i < (jsenum.Elements.Count - 1))
                        js.AppendLine($"\t\t{jsenum.Elements[i]},");
                    else
                        js.AppendLine($"\t\t{jsenum.Elements[i]}");
                }
                js.AppendLine("\t}");
            }

            // Declare parent module
            js.AppendLine();
            js.AppendLine("\tclass RhinoModule {");

            // Add enum property helpers
            foreach (var key in keys)
            {
                var jsenum = BindingClass.AllJavascriptEnums[key];
                js.AppendLine($"\t\t{jsenum.Name}: typeof {jsenum.Name}");
            }

            keys = BindingClass.AllJavascriptClasses.Keys.ToList();
            keys.Sort();

            // Add class property helpers
            foreach (var key in keys)
            {
                var jsclass = GetJS(key);
                js.AppendLine($"\t\t{jsclass.ClassName}: typeof {jsclass.ClassName};");
            }
            js.AppendLine("\t}");

            // Declare all classes
            foreach (var key in keys)
            {
                js.AppendLine();
                var jsclass = GetJS(key);
                var rhcommon = RhinoCommonClass.Get(key);

                js.Append($"\tclass {jsclass.ClassName}");
                if (!string.IsNullOrWhiteSpace(jsclass.BaseClass))
                    js.Append($" extends {jsclass.BaseClass}");
                js.AppendLine(" {");

                // Declare properties
                foreach (var prop in jsclass.Properties)
                {
                    PropertyDeclarationSyntax propDecl = null;
                    DocumentationCommentTriviaSyntax doccomment = null;
                    for (int i = 0; i < rhcommon.Properties.Count; i++)
                    {
                        if (prop.Equals(rhcommon.Properties[i].Item1.Identifier.ToString(), StringComparison.InvariantCultureIgnoreCase))
                        {
                            propDecl = rhcommon.Properties[i].Item1;
                            doccomment = rhcommon.Properties[i].Item2;
                            break;
                        }
                    }
                    js.AppendLine("\t\t/**");
                    if (doccomment != null)
                    {
                        string[] comments = DocCommentToTypeScript(doccomment, propDecl);
                        foreach (var comment in comments)
                        {
                            if( !string.IsNullOrWhiteSpace(comment))
                                js.AppendLine($"\t\t * {comment}");
                        }
                    }
                    string proptype = "any";
                    if (propDecl != null)
                    {
                        proptype = ToTypeScriptType(propDecl.Type.ToString());
                    }
                    js.AppendLine("\t\t */");
                    js.AppendLine($"\t\t{prop}: {proptype};");
                }

                // Declare constructor
                foreach (var constructor in jsclass.Constructors)
                {
                    var c = rhcommon.GetConstructor(constructor);
                    var doccomment = rhcommon.DocComment;
                    if (c == null)
                        continue;
                    ConstructorDeclarationSyntax constructorDecl = c.Item1;
                    doccomment = c.Item2;

                    if (constructorDecl != null)
                    {
                        js.AppendLine();
                        js.Append("\t\tconstructor(");

                        if (constructorDecl.ParameterList != null)
                        {
                            string parameters = "";
                            var p = constructorDecl.ParameterList.Parameters;
                            for (int i = 0; i < p.Count; i++)
                            {
                                parameters += p[i].Identifier + ": " + ToTypeScriptType(p[i].Type.ToString()) + (i == p.Count - 1 ? "" : ", ");
                            }

                            js.Append(parameters);
                        }

                        js.Append(");");
                        
                    }

                    js.AppendLine();
                }

                // Declare methods
                foreach (var (isStatic, method, args) in jsclass.Methods)
                {
                    MethodDeclarationSyntax methodDecl = null;
                    DocumentationCommentTriviaSyntax doccomment = null;
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
                    List<string> paramTypes = new List<string>();
                    if (doccomment == null)
                    {
                        js.AppendLine("\t\t/** ... */");
                    }
                    else
                    {
                        js.AppendLine("\t\t/**");
                        string s = DocCommentToTypeScript(doccomment, methodDecl, methodDecl.ParameterList, out paramNames, out paramTypes);
                        string[] lines = s.Split(new char[] { '\n' });
                        for( int i=0; i<lines.Length; i++ )
                        {
                            string line = lines[i].Trim();
                            if (string.IsNullOrWhiteSpace(line))
                                continue;
                            if (line.StartsWith("*"))
                                line = " " + line;
                            js.AppendLine($"\t\t{line}");
                        }
                        js.AppendLine("\t\t */");
                    }

                    string parameters = "";
                    for( int i=0; i<paramNames.Count; i++)
                    {
                        parameters += $"{paramNames[i]}:{paramTypes[i]},";
                    }
                    if (!string.IsNullOrEmpty(parameters))
                        parameters = parameters.Substring(0, parameters.Length - 1);

                    string returnType = "void";
                    if (methodDecl != null)
                        returnType = ToTypeScriptType(methodDecl.ReturnType.ToString());

                    if (isStatic)
                        js.AppendLine($"\t\tstatic {method}({parameters}): {returnType};");
                    else
                        js.AppendLine($"\t\t{method}({parameters}): {returnType};");
                }


                js.AppendLine("\t}");
            }

            js.AppendLine("}");
            string path = System.IO.Path.Combine(directory, "rhino3dm.d.ts");
            System.IO.File.WriteAllText(path, js.ToString());

        }

        static string ToJavascriptType(string type)
        {
            if (type.Equals("Point3d") || type.Equals("Vector3d"))
                return "Array.<x,y,z>";
            return type;
        }

        static string ToTypeScriptType(string type)
        {
            if (type.Equals("Point3d") || type.Equals("Vector3d"))
                return "number[]";
            if (type.Equals("bool"))
                return "boolean";
            if (type.Equals("double"))
                return "number";
            if (type.Equals("int") || type.Equals("uint"))
                return "number";
            if (type.Equals("Guid"))
                return "string";
            if (type.Equals("Interval"))
                return "number[]";
            if (type.Equals("Geometry.Surface"))
                return "Surface";
            if (type.Equals("Geometry.Extrusion"))
                return "Extrusion";
            if (type.Equals("MeshFace"))
                // ( Chuck ) MeshFace does not currently generate its typescript class.
                return "any";
            if (type.Equals("float"))
                return "number";
            if (type.Equals("float[]"))
                return "number[]";
            if (type.Equals("int[]"))
                return "number[]";
            if (type.Equals("IEnumberable<Point3d>"))
                return "Point3d[]";
            if (type.Equals("Vector3d[]"))
                // ( Chuck ) Vector3d does not currently generate its typescript class.
                return "any[]";
            if (type.Equals("System.Drawing.Color") || type.Equals("System.Drawing.Rectangle"))
                return "number[]";
            if (type.Equals("Color[]"))
                return "number[][]";
            if (type.Equals("Color"))
                return "number[]";
            if (type.Equals("DocObjects.Material"))
                return "Material";
            if (type.Equals("Rhino.Geometry.BoundingBox"))
                return "BoundingBox";
            if (type.Equals("MeshingParameterTextureRange"))
                return "number";
            if (type.Equals("System.Collections.Generic.IEnumerable<Point3d>"))
                return "Point3dList";

            if (type.Contains("Geometry."))
                return type.Replace("Geometry.", "");
            if (type.Contains("System.Collections.Generic.IEnumerable"))
                return type.Replace("System.Collections.Generic.IEnumerable<", "").Replace(">", "[]");
            if (type.Contains("IEnumerable"))
                return type.Replace("IEnumerable<", "").Replace(">", "[]");
            
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

        static string DocCommentToJsDoc(DocumentationCommentTriviaSyntax doccomment,
          PropertyDeclarationSyntax propertyDecl)
        {
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
                    var returnType = propertyDecl.Type;

                    js.AppendLine($"   * @returns {{{ToJavascriptType(returnType.ToString())}}} {elementText}");
                }
            }
            return js.ToString();
        }

        static string DocCommentToTypeScript(DocumentationCommentTriviaSyntax doccomment,
          MethodDeclarationSyntax methodDecl,
          ParameterListSyntax parameters,
          out List<string> paramNames, out List<string> paramTypes)
        {
            paramNames = new List<string>();
            paramTypes = new List<string>();
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

                    js.AppendLine($"   * @returns {{{ToTypeScriptType(returnType.ToString())}}} {elementText}");
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
                    paramTypes.Add(ToTypeScriptType(paramType));
                    js.AppendLine($"   * @param {{{ToTypeScriptType(paramType)}}} {paramName} {elementText}");
                }
            }
            return js.ToString();
        }

        static string[] DocCommentToTypeScript(DocumentationCommentTriviaSyntax doccomment,
          PropertyDeclarationSyntax propertyDecl)
        {
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
                    js.AppendLine($"{elementText}");
                }
                //else if (element.Name.Equals("returns", StringComparison.OrdinalIgnoreCase))
                //{
                //    var returnType = propertyDecl.Type;

                //    js.AppendLine($"   * @returns {{{ToJavascriptType(returnType.ToString())}}} {elementText}");
                //}
            }
            string[] lines = js.ToString().Split(new char[] { '\n' });
            for (int i = 0; i < lines.Length; i++)
                lines[i] = lines[i].Trim();
            return lines;
        }
    }
}
