using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace docgen
{
    class PythonClass : BindingClass
    {
        public PythonClass(string name) : base(name)
        {
        }

        const string T1 = "    ";
        const string T2 = "        ";
        const string T3 = "            ";
        static string _T(int amount)
        {
            string rc = "";
            for (int i = 0; i < amount; i++)
                rc += "    ";
            return rc;
        }

        static int ClassValue(PythonClass c)
        {
            if (string.IsNullOrWhiteSpace(c.BaseClass))
                return 0;
            if (c.BaseClass.Equals("CommonObject"))
                return 1;
            if (c.BaseClass.Equals("GeometryBase"))
                return 2;
            if (c.BaseClass.Equals("Curve") || c.BaseClass.Equals("Surface"))
                return 3;
            return 4;
        }

        public static void Write(string directory)
        {
            if (!System.IO.Directory.Exists(directory))
                System.IO.Directory.CreateDirectory(directory);
            StringBuilder py = new StringBuilder();
            var keys = AllPythonClasses.Keys.ToList();
            keys.Sort((a, b) =>
            {
                var rhcmnA = GetPY(a);
                int aVal = ClassValue(rhcmnA);
                var rhcmnB = GetPY(b);
                int bVal = ClassValue(rhcmnB);
                if (aVal < bVal)
                    return -1;
                if (bVal < aVal)
                    return 1;
                return a.CompareTo(b);
            });

            var rstDirectory = new System.IO.DirectoryInfo(directory);
            foreach (var key in keys)
            {
                var pyclass = GetPY(key);
                var rhcommon = RhinoCommonClass.Get(key);
                RstFile.WriteRst(rstDirectory, rhcommon, pyclass);
                var doccomment = rhcommon.DocComment;
                if (string.IsNullOrWhiteSpace(pyclass.BaseClass))
                    py.AppendLine($"class {pyclass.ClassName}:");
                else
                    py.AppendLine($"class {pyclass.ClassName}({pyclass.BaseClass}):");
                if (doccomment == null)
                {
                    py.AppendLine(T1 + "\"\"\" ... \"\"\"");
                }
                else
                {
                    string comment = DocCommentToPythonDoc(doccomment, 1);
                    py.Append(comment);
                }

                foreach (var constructor in pyclass.Constructors)
                {
                    var c = rhcommon.GetConstructor(constructor);
                    if (c == null)
                        continue;

                    var constructorDecl = c.Item1;
                    if (constructorDecl != null)
                    {
                        py.Append(T1 + "def __init__(self");
                        var paramnames = pyclass.GetParamNames(constructorDecl.ParameterList, true);
                        foreach (var paramname in paramnames)
                        {
                            py.Append(", " + paramname);
                        }
                        py.AppendLine("):");
                        if (c.Item2 != null)
                        {
                            py.Append(DocCommentToPythonDoc(c.Item2, 2));
                        }
                        py.AppendLine(T2 + "pass");
                    }
                }
                foreach (var (isStatic, method, args) in pyclass.Methods)
                {
                    var m = rhcommon.GetMethod(method);
                    if (m == null)
                        continue;

                    if (isStatic)
                        py.AppendLine(T1 + "@staticmethod");
                    py.Append(T1 + $"def {method}(");
                    bool addComma = false;
                    if (!isStatic)
                    {
                        py.Append("self");
                        addComma = true;
                    }
                    var paramnames = pyclass.GetParamNames(m.Item1.ParameterList, true);
                    foreach (var paramname in paramnames)
                    {
                        if (addComma)
                            py.Append(", ");
                        addComma = true;
                        py.Append(paramname);
                    }
                    py.AppendLine("):");
                    if (m.Item2 != null)
                    {
                        py.Append(DocCommentToPythonDoc(m.Item2, 2));
                    }
                    py.AppendLine(T2 + "pass");
                    py.AppendLine();
                }
                foreach (var propName in pyclass.Properties)
                {
                    var p = rhcommon.GetProperty(propName);
                    if (null == p)
                        continue;

                    py.AppendLine(T1 + "@property");
                    py.AppendLine(T1 + $"def {propName}(self):");
                    if (p.Item2 != null)
                    {
                        py.Append(DocCommentToPythonDoc(p.Item2, 2));
                    }
                    py.AppendLine(T2 + "return 0");
                    py.AppendLine();
                }
                py.AppendLine();
            }

            // Create an rst file for each class
            //foreach (var key in keys)
            //{
            //    var pyclass = GetPY(key);
            //    StringBuilder rst = new StringBuilder();
            //    rst.AppendLine(pyclass.ClassName);
            //    for (int i = 0; i < pyclass.ClassName.Length; i++)
            //        rst.Append("*");
            //    rst.AppendLine();
            //    rst.AppendLine();
            //    rst.AppendLine($".. autoclass:: rhino3dm.{pyclass.ClassName}");
            //    rst.AppendLine("   :members:");
            //    rst.AppendLine("   :undoc-members:");
            //    rst.AppendLine("   :show-inheritance:");
            //    System.IO.File.WriteAllText($"../{pyclass.ClassName}.rst", rst.ToString());
            //}
            // write the index
            StringBuilder indexRst = new StringBuilder();
            indexRst.Append(
      @".. rhino3dm documentation master file, created by
   sphinx-quickstart on Fri Oct 19 16:07:18 2018.
   You can adapt this file completely to your liking, but it should at least
   contain the root `toctree` directive.

Welcome to rhino3dm's documentation!
====================================

.. toctree::
   :maxdepth: 2
   :caption: Contents:

");
            List<string> names = new List<string>();
            foreach (var key in keys)
            {
                var pyclass = GetPY(key);
                names.Add(pyclass.ClassName);
            }
            names.Sort();
            foreach (var name in names)
            {
                indexRst.AppendLine($"   {name}");
            }
            indexRst.Append(
            @"

Indices and tables
==================

* :ref:`genindex`
* :ref:`modindex`
* :ref:`search`");
            System.IO.File.WriteAllText(System.IO.Path.Combine(directory, "index.rst"), indexRst.ToString());

            System.IO.File.WriteAllText(System.IO.Path.Combine(directory, "rhino3dm.py"), py.ToString());
        }

        static string DocCommentToPythonDoc(DocumentationCommentTriviaSyntax doccomment, int indentLevel)
        {
            StringBuilder summary = new StringBuilder();
            StringBuilder args = new StringBuilder();
            StringBuilder returns = new StringBuilder();


            string comment = doccomment.ToString();
            comment = comment.Replace("///", "");
            comment = comment.Replace("\t", " ");
            var doc = new System.Xml.XmlDocument();
            doc.LoadXml("<doc>" + comment + "</doc>");
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
                    foreach (var line in lines)
                        returns.AppendLine(_T(indentLevel + 1) + line.Trim());
                }
                else if (element.Name.Equals("param", StringComparison.OrdinalIgnoreCase))
                {
                    if (args.Length == 0)
                    {
                        args.AppendLine();
                        args.AppendLine(_T(indentLevel) + "Args:");
                    }
                    string parameterName = element.GetAttribute("name");

                    bool added = false;
                    foreach (var line in lines)
                    {
                        if (!added)
                        {
                            args.AppendLine(_T(indentLevel + 1) + parameterName + " : " + line.Trim());
                            continue;
                        }
                        added = true;
                        args.AppendLine(_T(indentLevel + 1) + line.Trim());
                    }
                }
            }
            StringBuilder rc = new StringBuilder();
            rc.AppendLine(_T(indentLevel) + "\"\"\"");
            rc.Append(summary.ToString());
            rc.Append(args.ToString());
            rc.Append(returns.ToString());
            rc.AppendLine(_T(indentLevel) + "\"\"\"");
            return rc.ToString();
        }
    }
}
