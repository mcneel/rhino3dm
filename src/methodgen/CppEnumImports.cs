using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;

namespace MethodGen
{
  class CppFileLink
  {
    public string AbsoluteImportingCSourceFile { get; set; }
    public string CompositeImportedLocation { get; set; }
    public string SectionPrefix { get; set; }
    public bool HasSectionPrefix { get { return !string.IsNullOrEmpty(SectionPrefix); } }
    public string SectionSuffix { get; set; }
    public bool HasSectionSuffix { get { return !string.IsNullOrEmpty(SectionPrefix); } }
  }

  internal class CppSharedEnums
  {
    internal const string RH_C_SHARED_ENUM_PARSE_FILE = "RH_C_SHARED_ENUM_PARSE_FILE";
    internal const string RH_C_ANY_REGION = "#pragma region";
    internal const string RH_C_SHARED_ENUM = RH_C_ANY_REGION + " RH_C_SHARED_ENUM";
    internal const string RH_C_EXCLUDE = RH_C_ANY_REGION + " RH_C_SHARED_ENUM_EXCLUDE_FROM_DOT_NET";
    internal const string RH_C_SHARED_ENUM_END = "#pragma endregion";

    private readonly List<CppEnumSection> m_pieces = new List<CppEnumSection>();
    private readonly Dictionary<string, string> m_cpp_to_cs_translations = new Dictionary<string, string>();

    public string Translate(string fullCppTypeName, out bool translated)
    {
      string rc;

      if (!(translated = m_cpp_to_cs_translations.TryGetValue(fullCppTypeName, out rc)))
        rc = fullCppTypeName;

      return rc;
    }

    public void Add(string absoluteImportingCSourceFile, string importLiteral)
    {
      var imports = importLiteral.Split(new[] { '\"' }, StringSplitOptions.None);
      List<string> args = new List<string>();
      bool even = true;
      for (int i = 0; i < imports.Length; i++)
      {
        if (imports[i].EndsWith(@"\") && !imports[i].EndsWith(@"\\") && imports.Length > i + 1)
        {
          even = !even;
          imports[i + 1] = imports[i] + "\"" + imports[i + 1];
          continue;
        }

        if (!even) args.Add(imports[i]);
        even = !even;
      }

      if (args.Count < 1 || args.Count > 3)
        throw new NotSupportedException("The " + CppSharedEnums.RH_C_SHARED_ENUM_PARSE_FILE +
          " call has to contain at least a relative location and optionally a prefix, and a suffix for the string.");

      //in case of backslashes and similar escape characters, we need the program to behave as though this was actually a parsed escaped C file.
      //the Regex.Unescape method seems to behave as expected for our needs. Uri.UnescapeDataString does not work for this.
      string relative_cpp_definition_file = Regex.Unescape(args[0]);
      string section_prefix = args.Count > 1 ? args[1] : string.Empty;
      string section_suffix = args.Count > 2 ? args[2] : string.Empty;

      if (Path.IsPathRooted(relative_cpp_definition_file))
      {
        throw new NotSupportedException(
          string.Format("relativeCppDefinitionFile\n{0}\n is rooted. It needs to be relative for this program to work in general.",
          relative_cpp_definition_file));
      }

      var absolute_folder = Path.GetDirectoryName(absoluteImportingCSourceFile);

      if (string.IsNullOrEmpty(absolute_folder))
      {
        throw new InvalidOperationException("absoluteImportingCSourceFile is not a file in a folder.");
      }

      var composite = Path.Combine(absolute_folder, relative_cpp_definition_file);
      composite = Path.GetFullPath(composite);

      if (!File.Exists(composite))
      {
        throw new FileNotFoundException(
          string.Format("File was not found;\n {0} \nand\n {1} was resolved to:\n {2}.",
          absoluteImportingCSourceFile,
          relative_cpp_definition_file,
          composite
          ),
          composite);
      }

      var cpp_info = new CppFileLink
      {
        AbsoluteImportingCSourceFile = absoluteImportingCSourceFile,
        CompositeImportedLocation = composite,
        SectionPrefix = section_prefix,
        SectionSuffix = section_suffix
      };


      ReadFileLinkIntoSections(cpp_info);
    }

    private void ReadFileLinkIntoSections(CppFileLink cppInfo)
    {
      bool region_written = false;
      using (var text_stream = File.OpenText(cppInfo.CompositeImportedLocation))
      {
        int line_count = 0;
        int last_enum_start = -1;
        int nesting = 0;

        while (!text_stream.EndOfStream)
        {
          string shared_enum_start = string.Empty;
          string line;
          while ((line = text_stream.ReadLine()) != null)
          {
            line_count++;

            int enum_start = line.IndexOf(RH_C_SHARED_ENUM);
            if (enum_start != -1)
            {
              shared_enum_start = line.Substring(enum_start + RH_C_SHARED_ENUM.Length);
              last_enum_start = line_count;
              nesting = 1;
              break;
            }
          }
          if (!string.IsNullOrEmpty(shared_enum_start))
          {
            StringBuilder enum_text = new StringBuilder();
            while ((line = text_stream.ReadLine()) != null)
            {
              line_count++;
              int begin_end = line.IndexOf(RH_C_SHARED_ENUM_END);
              if (begin_end != -1)
              {
                nesting--;
                if(nesting <= 0) break;
              }
              int any_start = line.IndexOf(RH_C_ANY_REGION);
              if (any_start != -1)
                nesting++;
              enum_text.AppendLine(line);
            }

            string enum_body = enum_text.ToString();

            var csharp_enum = CppEnumSection.FromParsedEnumBody(
              shared_enum_start,
              enum_body,
              last_enum_start,
              cppInfo.CompositeImportedLocation,
              m_cpp_to_cs_translations);

            if (!region_written)
            {
              this.m_pieces.Add(new CppEnumSection(@"#region "
                + Path.GetFileName(cppInfo.CompositeImportedLocation) + Environment.NewLine
                ));

              if (cppInfo.HasSectionPrefix)
              {
                this.m_pieces.Add(new CppEnumSection(cppInfo.SectionPrefix + Environment.NewLine));
              }
              region_written = true;
            }

            this.m_pieces.Add(csharp_enum);
          }
        }
      }
      if (region_written)
      {
        this.m_pieces.Add(new CppEnumSection(Environment.NewLine));
        if (cppInfo.HasSectionSuffix)
        {
          this.m_pieces.Add(new CppEnumSection(
            Environment.NewLine + @"// (" + cppInfo.SectionPrefix + ")" + Environment.NewLine +
            cppInfo.SectionSuffix + Environment.NewLine));
        }
        this.m_pieces.Add(new CppEnumSection("#endregion" + Environment.NewLine + Environment.NewLine));
      }
    }

    public void Write(TextWriter writer)
    {
      if (m_pieces.Count == 0) return;

      writer.Write(
@"// NATIVE ENUMS
// Automatically generated enums from C++ libraries.
// DO NOT EDIT THIS FILE BY HAND!

#define ON_ENUM_SENTINEL_VALUES_RHINOCOMMON_REGION

");

      foreach (var piece in m_pieces)
      {
        piece.Write(writer);
      }
    }
  }

  class CppEnumSection
  {
    private static readonly HashSet<string> g_valid_type = new HashSet<string> { "public", "internal", };
    private static readonly HashSet<string> g_valid_embedding = new HashSet<string> { "unnested", "nested", };
    private static readonly HashSet<string> g_valid_inheritance = new HashSet<string> { "byte", "sbyte", "short", "ushort", "int", "uint", "long", "ulong", };

    private const string g_valid_flags_request = "flags";
    private const string g_valid_clsfalse_request = "clsfalse";

    private string m_to_write;

    public CppEnumSection(string toWrite)
    {
      m_to_write = toWrite;
    }

    public void Write(TextWriter writer)
    {
      writer.Write(m_to_write);
    }

    public static CppEnumSection FromParsedEnumBody(string pragmaRegionOptions, string enumBody, int line, string file, Dictionary<string, string> cppToCsTranslationsToAppend)
    {
      var option_parts = pragmaRegionOptions.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
      if (option_parts.Length < 2 || !option_parts[0].StartsWith("[") || !option_parts[0].EndsWith("]")
          || !option_parts[1].StartsWith("[") || !option_parts[1].EndsWith("]") || option_parts.Length > 3
          || (option_parts.Length == 3 && (!option_parts[2].StartsWith("[") || !option_parts[1].EndsWith("]"))))
      {
        throw new InvalidOperationException(
          string.Format(
            "File {0}, line {1}: parse enum region needs to have at least two declarations in square brackets: name of C++ type and full name of .Net type.\n"
            + "An optional declaration of exported type visibility and inheriting type can follow.\n"
            + "For example, [SubD::MethodEnum] [Rhino.Geometry.MethodEnum] [public:byte]",
            file,
            line));
      }

      //exported type name
      string exported_type = option_parts[0].Substring(1, option_parts[0].Length - 2);

      //namespace
      string imported_namespace;
      string imported_typename;
      {
        int namespace_position = option_parts[1].LastIndexOf(".");
        imported_namespace = namespace_position == -1 ? string.Empty : option_parts[1].Substring(1, namespace_position - 1);

        //imported type name
        if (namespace_position == -1)
        {
          imported_typename = option_parts[1].Substring(1, option_parts[1].Length - 1);
        }
        else
        {
          imported_typename = option_parts[1].Substring(
            namespace_position + 1,
            option_parts[1].Length - 2 - namespace_position);
        }
        if (string.IsNullOrEmpty(imported_typename))
        {
          throw new ArgumentException(string.Format("File {0}, line {1}: exported type has no name.", file, line));
        }
      }

      //options
      string valid_type_choice = null;
      string valid_embedding_choice = null;
      string valid_inheritance_choice = null;
      bool add_cls_compliant_attribute = false;
      bool add_flags_attribute = false;

      if (option_parts.Length > 2)
      {
        var all_options = option_parts[2].Substring(1, option_parts[2].Length - 2);
        var options = all_options.Split(':');

        foreach (var option in options)
        {
          if (g_valid_type.Contains(option))
          {
            if (!string.IsNullOrEmpty(valid_type_choice))
            {
              throw new Exception("Too many valid type modifiers: choose among " + MakeHashTableReadable(g_valid_type));
            }
            valid_type_choice = option;
            continue;
          }
          if (g_valid_embedding.Contains(option))
          {
            if (!string.IsNullOrEmpty(valid_embedding_choice))
            {
              throw new Exception("Nested option is repeated: choose among " + MakeHashTableReadable(g_valid_embedding));
            }
            valid_embedding_choice = option;
            continue;
          }
          if (g_valid_inheritance.Contains(option))
          {
            if (!string.IsNullOrEmpty(valid_inheritance_choice))
            {
              throw new Exception(
                "More than one ihneriting type: choose among " + MakeHashTableReadable(g_valid_inheritance));
            }
            valid_inheritance_choice = option;
            continue;
          }
          if (option == g_valid_flags_request)
          {
            if (add_flags_attribute)
            {
              throw new Exception(
                "More than one flags requests.");
            }
            add_flags_attribute = true;
            continue;
          }
          if (option == g_valid_clsfalse_request)
          {
            if (add_cls_compliant_attribute)
            {
              throw new Exception(
                "More than one flags requests.");
            }
            add_cls_compliant_attribute = true;
            continue;
          }

          Program.VsSendConsoleMessage(true, file, line, 2,
            "Unrecognized option \"" + option + "\". Use a colon for separation and choose among "
            + MakeHashTableReadable(g_valid_type) + MakeHashTableReadable(g_valid_embedding)
            + MakeHashTableReadable(g_valid_inheritance) + " " + g_valid_flags_request + " " +
            g_valid_clsfalse_request + ".");
          break;
        }
      }
      valid_type_choice = valid_type_choice ?? "public";
      valid_embedding_choice = valid_embedding_choice ?? "unnested";
      valid_inheritance_choice = (valid_inheritance_choice == null) ? string.Empty : (": " + valid_inheritance_choice);

      //nesting typename
      string nesting_typename = string.Empty;
      if (valid_embedding_choice == "nested")
      {
        int index_of_dot = imported_namespace.LastIndexOf(".");
        if (index_of_dot == -1)
        {
          throw new NotSupportedException(
            "There is no name for the nesting type in " + imported_namespace + ". A period is expected.");
        }
        nesting_typename = imported_namespace.Substring(index_of_dot + 1);
        imported_namespace = imported_namespace.Substring(0, index_of_dot);
      }

      const string s_enum = @"([\s\S]*)(enum\s+class)\s+([a-zA-Z0-9_]*)\s*(:*\s*[a-zA-Z0-9 ]*)([\s\/]*\{[\s\S]*)";

      if (!Regex.IsMatch(enumBody, s_enum))
      {
        throw new InvalidOperationException(
          file + " line " + line + ": Enum body:\n" + enumBody + "\ndoes not match enum class pattern. Check: \n" + s_enum
          + "\n against a pattern matcher like " + "the ones at http://myregexp.com/ or https://www.myregextester.com/");
      }

      string attributes = string.Empty;
      {
        var either_active = add_flags_attribute || add_cls_compliant_attribute;

        if (either_active) attributes += "\r\n";
        if (add_cls_compliant_attribute) attributes += "  [System.CLSCompliant(false)]\r\n";
        if (add_flags_attribute) attributes += "  [System.Flags]\r\n";
        if (either_active) attributes += "  ";
      }

      string replacement_head = string.Format(
        "$1{0}{1} enum {2} {3} ",
        attributes,
        valid_type_choice,
        imported_typename,
        valid_inheritance_choice
        );

      const string replacement_body = "$5";

      var new_head = Regex.Replace(enumBody, s_enum, replacement_head);
      var new_body = Regex.Replace(enumBody, s_enum, replacement_body);

      //translate uint enum to int enum if requested
      string old_deriving_type = Regex.Match(enumBody, s_enum).Groups[4].Value;
      old_deriving_type = old_deriving_type.Replace(":", "").Trim();
      if (old_deriving_type == "unsigned int" || old_deriving_type == "unsigned long int")
      {
        if (valid_inheritance_choice == ": int")
        {
          new_body = RewriteUnsigned<uint, int>(line, file, new_body, uint.TryParse, n => unchecked((int)n));
        }
        else if (valid_inheritance_choice == ": long")
        {
          new_body = RewriteUnsigned<ulong, long>(line, file, new_body, ulong.TryParse, n => unchecked((long)n));
        }
      }

      new_body = new_body.TrimEnd(';', ' ', '\t', '\r', '\n'); //removes semicolon and other weird things

      var writer = new StringBuilder();

      //check lower-case enum fields and mark as DONTS
      {
        //note: inner is not used. It is only here to validate field names.
        var inner = Regex.Replace(new_body, @"(.*\{)([\s\S]*)(\})", "$2");
        foreach (var enum_line in inner.Split('\n'))
        {
          var enum_line_clean = enum_line.Trim(' ', '\r', '\t');
          if (enum_line_clean.StartsWith("//")) continue;
          if (enum_line_clean.Length < 1) continue;
          if (char.IsLower(enum_line_clean[0]))
          {
            var begin = enum_line_clean.Split(' ', '\t', '=', '/', ',')[0];

            var message = string.Format(
              "Shared enum \"{0}\" contains lower-case field names. The first one is: \"{1}\"",
              imported_typename,
              begin);

            Program.VsSendConsoleMessage(true, file, line, 1, message
              );

            writer.AppendLine("/" + "/" + message);

            break;
          }
        }
      }

      new_body = Regex.Replace(new_body,
        Regex.Escape(CppSharedEnums.RH_C_EXCLUDE) + @"( COMMENT)?([\s\S]+)" + Regex.Escape(CppSharedEnums.RH_C_SHARED_ENUM_END),
        m =>
        {
          if (m.Groups[1].Length == 0)
            return string.Empty;
          else
          {
            var tokens = m.Groups[2].Value.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            string text = "// " + string.Join("\n// ", tokens);
            return text;
          }
        }
        ,
        RegexOptions.Multiline);


      writer.AppendFormat(@"
// line {0}: {1}
", line.ToString(), file);

      var has_namespace = !string.IsNullOrEmpty(imported_namespace);
      var has_nesting_typename = !string.IsNullOrEmpty(nesting_typename);
      if (has_namespace)
      {
        writer.AppendLine("namespace " + imported_namespace);
        writer.AppendLine("{");
      }
      if (has_nesting_typename)
      {
        writer.AppendLine("  partial class " + nesting_typename);
        writer.AppendLine("  {");
      }

      writer.Append(new_head);
      writer.AppendLine(new_body);

      if (has_nesting_typename)
      {
        writer.AppendLine("  }");
      }
      if (has_namespace)
      {
        writer.AppendLine("}");
      }

      cppToCsTranslationsToAppend.Add(exported_type, imported_namespace + "." +
        (string.IsNullOrEmpty(nesting_typename) ? imported_typename : (nesting_typename + ".") + imported_typename));
      return new CppEnumSection(writer.ToString());
    }

    private delegate bool TryParseMethod<TFrom>(string s, NumberStyles style, IFormatProvider provider, out TFrom result);
    private static string RewriteUnsigned<TFrom,TTo>(int line, string file, string new_body, TryParseMethod<TFrom> try_parse, Converter<TFrom, TTo> converter)
      where TFrom : struct
      where TTo : struct
    {
      // see https://msdn.microsoft.com/en-us/library/aa664674%28v=vs.71%29.aspx for C# details
      // see http://en.cppreference.com/w/cpp/language/integer_literal for C++ details
      // C++ writer: please note that C# DOES NOT have octal, nor binary, integer literals.

      new_body = Regex.Replace(new_body, @"\b(0X)?([0-9a-f]+)([UL]+)\b", // find all integers. \b is a word boundary
       (m) =>
       {
         string prefix = m.Groups[1].Value; // 0x || 0X
             string number = m.Groups[2].Value; // 1234...
             string suffix = m.Groups[3].Value; // u || U

             if (number.StartsWith("0") && number.Length > 1 && !string.IsNullOrEmpty(number.Replace("0", "")))
         {
           const string no_octals = "No octal or binary integer literals in CSharp";
           Program.VsSendConsoleMessage(true, file, line, 3, no_octals);
           return "\n\n#error " + no_octals + "\n\n";
         }

         NumberStyles style = string.IsNullOrEmpty(prefix) ?
            NumberStyles.None : NumberStyles.AllowHexSpecifier;

         TFrom parsed;
         if (try_parse(number, style, null, out parsed))
         {
           //if (parsed <= int.MaxValue && string.IsNullOrEmpty(prefix) && string.IsNullOrEmpty(suffix))
           //  return m.Value; // do not edit values that are already fine

           TTo as_int = converter(parsed);
           string as_int_string = as_int.ToString();
           string comment = string.Empty;
           if (!string.IsNullOrEmpty(prefix) || number != as_int_string)
           {
             comment = " /" + "* C++: " + m.Value + "*" + "/";
           }

           return as_int_string + comment;
         }
         else
         {
           return m.Value; //cannot parse. bail out
             }
       }
       , RegexOptions.Multiline | RegexOptions.IgnoreCase);
      return new_body;
    }

    static string MakeHashTableReadable(HashSet<string> hashTable)
    {
      StringBuilder sb = new StringBuilder();
      foreach (var text in hashTable)
      {
        sb.Append(text).Append(" ");
      }
      return sb.ToString();
    }
  }
}