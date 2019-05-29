using System;
using System.Collections.Generic;

namespace MethodGen
{
  public class CommandlineParser
  {
    private readonly Dictionary<string, string> m_name_value_pairs;

    public CommandlineParser(string[] args)
    {
      m_name_value_pairs = new Dictionary<string, string>();
      ProcessArguments(args);
    }

    private void ProcessArguments(string[] args)
    {
      for (int i=0; i<args.Length; i++)
      {
        string current_arg = args[i];

        if (current_arg.Contains("="))
        {
          // This is a name-value pair
          //   color=white
          string[] pair = current_arg.Split("=".ToCharArray(), 2, StringSplitOptions.RemoveEmptyEntries);
          string name = pair[0].ToUpperInvariant();
          this[name] = pair[1];

        }
        else if (current_arg.StartsWith("/") && current_arg.Length > 2)
        {
          // This is also a name-value pair
          //   /color white
          // but not
          //   /x  <== these are just a "switch"; see below
          string name = current_arg.TrimStart("/".ToCharArray()).ToUpperInvariant();
          if (i + 1 < args.Length)
          {
            string value = args[++i];
            this[name] = value;
          }
          else
          {
            throw new CommandlineParserException("Unexpected Command Line Argument '" + current_arg + "' - expected another argument after as value.");
          }
        }
        else if (current_arg.StartsWith("/") && current_arg.Length == 2)
        {
          string name = current_arg.TrimStart("/".ToCharArray());
          this[name] = "";
        }
        else
        {
          // This is just a single parameter
          this[current_arg] = "";
        }
      }
    }

    public string this[string index]
    {
      get
      {
        if (m_name_value_pairs.ContainsKey(index.ToUpperInvariant()))
          return m_name_value_pairs[index.ToUpperInvariant()];
        if (m_name_value_pairs.ContainsKey(index))
          return m_name_value_pairs[index];
          return null;
      }
      set
      {
        if (!m_name_value_pairs.ContainsKey(index))
        {
          m_name_value_pairs.Add(index, value);
        }
        else
        {
          throw new CommandlineParserException("Skipping duplicate command line parameter: '" + index + "' with value '" + value + "'");
        }
      }
    }

    public bool ContainsKey(string key)
    {
      if (string.IsNullOrEmpty(this[key]))
        return false;

      return true;
    }
  }

  public class CommandlineParserException : Exception
  {
    public CommandlineParserException()
    {}
    public CommandlineParserException(string message) : base (message) {}
    public CommandlineParserException(string message, Exception innerException) : base (message, innerException) {}
  }
}
