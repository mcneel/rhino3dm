using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;


namespace docgen
{
    class ParameterInfo
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public List<string> Description { get; } = new List<string>();
    }

    class ReturnInfo
    {
        public string Type { get; set; }
        public List<string> Description { get; } = new List<string>();
    }

    class RstFile
    {
        public static void WriteRst(DirectoryInfo rstDirectory, RhinoCommonClass rhinocommonClass, PythonClass pythonClass)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(pythonClass.ClassName);
            sb.AppendLine("".PadLeft(pythonClass.ClassName.Length, '='));
            sb.AppendLine();
            sb.AppendLine($".. py:module:: rhino3dm");
            sb.AppendLine();
            sb.AppendLine($".. py:class:: {pythonClass.ClassName}");

            foreach (var constructor in pythonClass.Constructors)
            {
                var rhcommonConstructor = rhinocommonClass.GetConstructor(constructor);
                sb.AppendLine();
                sb.Append($"   .. py:method:: {pythonClass.ClassName}(");

                StringBuilder summary = null;
                List<ParameterInfo> parameters = new List<ParameterInfo>();
                if (rhcommonConstructor != null)
                {
                    DocCommentToPythonDoc(rhcommonConstructor.Item2, rhcommonConstructor.Item1, 2, out summary, out parameters);
                }

                for( int i=0; i<parameters.Count; i++ )
                {
                    if (i > 0)
                        sb.Append(", ");
                    sb.Append(parameters[i].Name);
                }
                sb.AppendLine(")");
                sb.AppendLine();
                if (null == summary)
                    sb.AppendLine(_T(2) + $"{pythonClass.ClassName} constructor");
                else
                    sb.Append(summary.ToString());

                //if (parameters.Count > 0 && parameters.Count == args.Length)
                {
                    sb.AppendLine();
                    foreach (var p in parameters)
                    {
                        if (p.Description.Count == 0)
                            continue;
                        string type = ToPythonType(p.Type);
                        if (type.IndexOf(' ') < 0)
                            sb.Append($"      :param {type} {p.Name}: {p.Description[0]}");
                        else
                            sb.Append($"      :param {p.Name}: {p.Description[0]}");

                        if (p.Description.Count > 1)
                            sb.AppendLine(" \\");
                        else
                            sb.AppendLine();
                        for (int i = 1; i < p.Description.Count; i++)
                        {
                            if (i == (p.Description.Count - 1))
                                sb.AppendLine($"         {p.Description[i]}");
                            else
                                sb.AppendLine($"         {p.Description[i]} \\");
                        }
                        if (type.IndexOf(' ') > 0)
                            sb.AppendLine($"      :type {p.Name}: {type}");
                    }
                }


            }

            foreach (var property in pythonClass.Properties)
            {
                sb.AppendLine();
                sb.AppendLine($"   .. py:attribute:: {property}");
                sb.AppendLine();
                var rhcommonProperty = rhinocommonClass.GetProperty(property);
                if (null == rhcommonProperty)
                    continue;
                StringBuilder summary = null;
                DocCommentToPythonDoc(rhcommonProperty.Item2, rhcommonProperty.Item1, 2, out summary);
                sb.Append(summary.ToString());
            }

            foreach (var pyMethod in pythonClass.Methods)
            {
                bool isStatic = pyMethod.IsStatic;
                string method = pyMethod.Name;
                string[] args = pyMethod.ArgList;
                sb.AppendLine();
                if (isStatic)
                    sb.Append($"   .. py:staticmethod:: {method}(");
                else
                    sb.Append($"   .. py:method:: {method}(");
                for (int i = 0; i < args.Length; i++)
                {
                    if (i > 0)
                        sb.Append(", ");
                    sb.Append(args[i]);
                }
                sb.AppendLine(")");
                sb.AppendLine();

                var rhcommonMethod = rhinocommonClass.GetMethod(method);
                if (rhcommonMethod == null)
                {
                    sb.AppendLine("      [todo] add documentation");
                    continue;
                }

                StringBuilder summary;
                List<ParameterInfo> parameters;
                ReturnInfo returnInfo;
                string s = DocCommentToPythonDoc(rhcommonMethod.Item2, rhcommonMethod.Item1, 2, out summary, out parameters, out returnInfo);
                sb.Append(summary.ToString());
                if( parameters.Count > 0 && parameters.Count == args.Length)
                {
                    sb.AppendLine();
                    foreach (var p in parameters)
                    {
                        if (p.Description.Count == 0)
                            continue;
                        string type = ToPythonType(p.Type);
                        if (type.IndexOf(' ') < 0)
                            sb.Append($"      :param {type} {p.Name}: {p.Description[0]}");
                        else
                            sb.Append($"      :param {p.Name}: {p.Description[0]}");

                        if (p.Description.Count > 1)
                            sb.AppendLine(" \\");
                        else
                            sb.AppendLine();
                        for (int i = 1; i < p.Description.Count; i++)
                        {
                            if (i == (p.Description.Count - 1))
                                sb.AppendLine($"         {p.Description[i]}");
                            else
                                sb.AppendLine($"         {p.Description[i]} \\");
                        }
                        if (type.IndexOf(' ') > 0)
                            sb.AppendLine($"      :type {p.Name}: {type}");
                    }
                }

                sb.AppendLine();
                if (returnInfo.Description.Count > 0)
                {
                    sb.Append($"      :return: {returnInfo.Description[0]}");
                    if (returnInfo.Description.Count > 1)
                        sb.AppendLine(" \\");
                    else
                        sb.AppendLine();
                    for (int i = 1; i < returnInfo.Description.Count; i++)
                    {
                        if (i == (returnInfo.Description.Count - 1))
                            sb.AppendLine($"         {returnInfo.Description[i]}");
                        else
                            sb.AppendLine($"         {returnInfo.Description[i]} \\");
                    }
                }
                sb.AppendLine($"      :rtype: {ToPythonType(returnInfo.Type)}");

            }

            string path = Path.Combine(rstDirectory.FullName, $"{pythonClass.ClassName}.rst");
            File.WriteAllText(path, sb.ToString());
        }

        public static string ToPythonType(string type)
        {
            bool isArray = type.EndsWith("[]");
            if (isArray)
                return ToPythonType(type.Substring(0, type.Length - 2)) + "[]";

            if (type.Equals("double"))
                return "float";
            if (type.Equals($"IEnumerable<double>"))
                return "list[float]";
            if (type.Equals($"IEnumerable<int>"))
                return "list[int]";

            if (type.Equals("string", StringComparison.InvariantCultureIgnoreCase))
                return "str";
            string[] rhino3dm = { "BezierCurve", "BoundingBox", "Box", "Brep", "BrepEdge", "BrepFace", "Curve",
                "GeometryBase", "Interval", "Mesh", "MeshingParameters", "NurbsCurve", "Plane", "Point2d", "Point3d",
                "Polyline", "Sphere", "Surface", "Vector3d" };
            foreach (var item in rhino3dm)
            {
                if (type.Equals(item))
                    return "rhino3dm." + type;
                if (type.Equals($"IEnumerable<{item}>"))
                {
                    int startIndex = "IEnumerable<".Length;
                    int endIndex = type.IndexOf('>');
                    return "list[rhino3dm." + type.Substring(startIndex, endIndex - startIndex) + "]";
                }
            }
            return type;
        }

        static System.Xml.XmlDocument DocCommentToXml(DocumentationCommentTriviaSyntax doccomment)
        {
            string comment = doccomment.ToString();
            comment = comment.Replace("///", "");
            comment = comment.Replace("\t", " ");
            comment = comment.Replace("null ", "None ");
            comment = comment.Replace("true ", "True ");
            comment = comment.Replace("false ", "False ");
            var doc = new System.Xml.XmlDocument();
            doc.LoadXml("<doc>" + comment + "</doc>");
            return doc;
        }
        public static int SpacesPerTab { get; set; } = 3;
        static string _T(int amount)
        {
            return "".PadLeft(amount * SpacesPerTab);
        }

        static bool IsOutParameter(ParameterSyntax parameter)
        {
            foreach (var modifier in parameter.Modifiers)
            {
                if (modifier.Text == "out")
                    return true;
            }
            return false;
        }


        public static string DocCommentToPythonDoc(
        DocumentationCommentTriviaSyntax doccomment, MethodDeclarationSyntax method,
        int indentLevel, out StringBuilder summary, out List<ParameterInfo> parameters, out ReturnInfo returnInfo)
        {
            // See https://sphinxcontrib-napoleon.readthedocs.io/en/latest/example_google.html
            // for docstring examples

            summary = new StringBuilder();
            StringBuilder args = new StringBuilder();
            StringBuilder returns = new StringBuilder();
            StringBuilder outArgs = new StringBuilder();
            parameters = new List<ParameterInfo>();
            returnInfo = new ReturnInfo() { Type = method.ReturnType.ToString() };
            if (doccomment == null)
                return "";
            var doc = DocCommentToXml(doccomment);
            var nodes = doc.FirstChild.ChildNodes;
            foreach (var node in nodes)
            {
                var element = node as System.Xml.XmlElement;
                string elementText = element.InnerText.Trim();
                if (string.IsNullOrWhiteSpace(elementText))
                    continue;
                string[] lines = elementText.Split(new char[] { '\n' });

                if (element.Name.Equals("summary", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var line in lines)
                        summary.AppendLine(_T(indentLevel) + line.Trim());
                }
                else if (element.Name.Equals("returns", StringComparison.OrdinalIgnoreCase))
                {
                    returns.AppendLine();
                    returns.AppendLine(_T(indentLevel) + "Returns:");
                    returns.Append(_T(indentLevel + 1) + $"{method.ReturnType}: ");
                    bool firstLine = true;
                    foreach (var line in lines)
                    {
                        returnInfo.Description.Add(line.Trim());
                        if (!firstLine)
                            returns.Append(_T(indentLevel + 1));
                        firstLine = false;
                        returns.AppendLine(line.Trim());
                    }
                }
                else if (element.Name.Equals("param", StringComparison.OrdinalIgnoreCase))
                {
                    string parameterName = element.GetAttribute("name");
                    ParameterInfo pinfo = new ParameterInfo { Name = parameterName };
                    string paramType = "";
                    bool isOutParam = false;
                    foreach (var param in method.ParameterList.Parameters)
                    {
                        if (param.Identifier.ToString().Equals(parameterName, StringComparison.Ordinal))
                        {
                            isOutParam = IsOutParameter(param);
                            paramType = $" ({param.Type})";
                            pinfo.Type = param.Type.ToString();
                        }
                    }

                    if (args.Length == 0 && !isOutParam)
                    {
                        args.AppendLine();
                        args.AppendLine(_T(indentLevel) + "Args:");
                    }

                    bool added = false;
                    StringBuilder sb = isOutParam ? outArgs : args;
                    foreach (var line in lines)
                    {
                        pinfo.Description.Add(line.Trim());
                        if (!added)
                        {
                            added = true;
                            sb.AppendLine(_T(indentLevel + 1) + parameterName + paramType + ": " + line.Trim());
                            continue;
                        }
                        sb.AppendLine(_T(indentLevel + 2) + line.Trim());
                    }
                    if (!isOutParam)
                        parameters.Add(pinfo);
                }
            }

            StringBuilder rc = new StringBuilder();
            rc.AppendLine(_T(indentLevel) + "\"\"\"");
            rc.Append(summary.ToString());
            rc.Append(args.ToString());
            rc.Append(returns.ToString());
            rc.Append(outArgs.ToString());
            rc.AppendLine(_T(indentLevel) + "\"\"\"");
            return rc.ToString();
        }

        public static string DocCommentToPythonDoc(
        DocumentationCommentTriviaSyntax doccomment, ConstructorDeclarationSyntax constructor,
        int indentLevel, out StringBuilder summary, out List<ParameterInfo> parameters)
        {
            // See https://sphinxcontrib-napoleon.readthedocs.io/en/latest/example_google.html
            // for docstring examples

            summary = new StringBuilder();
            StringBuilder args = new StringBuilder();
            StringBuilder returns = new StringBuilder();
            StringBuilder outArgs = new StringBuilder();
            parameters = new List<ParameterInfo>();
            if (doccomment == null)
                return "";
            var doc = DocCommentToXml(doccomment);
            var nodes = doc.FirstChild.ChildNodes;
            foreach (var node in nodes)
            {
                var element = node as System.Xml.XmlElement;
                string elementText = element.InnerText.Trim();
                if (string.IsNullOrWhiteSpace(elementText))
                    continue;
                string[] lines = elementText.Split(new char[] { '\n' });

                if (element.Name.Equals("summary", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var line in lines)
                        summary.AppendLine(_T(indentLevel) + line.Trim());
                }
                else if (element.Name.Equals("param", StringComparison.OrdinalIgnoreCase))
                {
                    string parameterName = element.GetAttribute("name");
                    ParameterInfo pinfo = new ParameterInfo { Name = parameterName };
                    string paramType = "";
                    bool isOutParam = false;
                    foreach (var param in constructor.ParameterList.Parameters)
                    {
                        if (param.Identifier.ToString().Equals(parameterName, StringComparison.Ordinal))
                        {
                            isOutParam = IsOutParameter(param);
                            paramType = $" ({param.Type})";
                            pinfo.Type = param.Type.ToString();
                        }
                    }

                    if (args.Length == 0 && !isOutParam)
                    {
                        args.AppendLine();
                        args.AppendLine(_T(indentLevel) + "Args:");
                    }

                    bool added = false;
                    StringBuilder sb = isOutParam ? outArgs : args;
                    foreach (var line in lines)
                    {
                        pinfo.Description.Add(line.Trim());
                        if (!added)
                        {
                            added = true;
                            sb.AppendLine(_T(indentLevel + 1) + parameterName + paramType + ": " + line.Trim());
                            continue;
                        }
                        sb.AppendLine(_T(indentLevel + 2) + line.Trim());
                    }
                    if (!isOutParam)
                        parameters.Add(pinfo);
                }
            }

            StringBuilder rc = new StringBuilder();
            rc.AppendLine(_T(indentLevel) + "\"\"\"");
            rc.Append(summary.ToString());
            rc.Append(args.ToString());
            rc.Append(returns.ToString());
            rc.Append(outArgs.ToString());
            rc.AppendLine(_T(indentLevel) + "\"\"\"");
            return rc.ToString();
        }

        public static string DocCommentToPythonDoc(
        DocumentationCommentTriviaSyntax doccomment, PropertyDeclarationSyntax property,
        int indentLevel, out StringBuilder summary)
        {
            // See https://sphinxcontrib-napoleon.readthedocs.io/en/latest/example_google.html
            // for docstring examples

            summary = new StringBuilder();
            StringBuilder args = new StringBuilder();
            StringBuilder returns = new StringBuilder();
            StringBuilder outArgs = new StringBuilder();
            if (doccomment == null)
                return "";
            var doc = DocCommentToXml(doccomment);
            var nodes = doc.FirstChild.ChildNodes;
            foreach (var node in nodes)
            {
                var element = node as System.Xml.XmlElement;
                string elementText = element.InnerText.Trim();
                if (string.IsNullOrWhiteSpace(elementText))
                    continue;
                string[] lines = elementText.Split(new char[] { '\n' });

                if (element.Name.Equals("summary", StringComparison.OrdinalIgnoreCase))
                {
                    for( int i=0; i<lines.Length; i++)
                    {
                        if( 0==i )
                        {
                            string rhtype = ToPythonType($"{property.Type}");
                            summary.AppendLine(_T(indentLevel) + $"{rhtype}: {lines[0].Trim()}");
                        }
                        else
                            summary.AppendLine(_T(indentLevel) + lines[i].Trim());
                    }
                }
            }

            StringBuilder rc = new StringBuilder();
            rc.AppendLine(_T(indentLevel) + "\"\"\"");
            rc.Append(summary.ToString());
            rc.Append(args.ToString());
            rc.Append(returns.ToString());
            rc.Append(outArgs.ToString());
            rc.AppendLine(_T(indentLevel) + "\"\"\"");
            return rc.ToString();
        }
    }
}
