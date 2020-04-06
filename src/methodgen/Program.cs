using System;
using System.Collections.Generic;


namespace MethodGen
{
  /// <summary>
  /// Purpose of this application is to auto-generate the function declarations
  /// for the UnsafeNativeMethods in RhinoCommon by parsing exported
  /// C functions in rhcommon_c
  /// 
  /// This application can also be used for generating UnsafeNatimeMethod C#
  /// declarations for other sets of C++ source code by using command line
  /// parameters or a config file which contains the directory for reading the
  /// C++ files followed by the directory for writing the AutoNativeMethods.cs
  /// file
  /// </summary>
  class Program
  {
    public static string m_namespace;
    public static bool m_includeRhinoDeclarations = true;
    public static List<string> m_extra_usings = new List<string>();

    static int Main(string[] args)
    {
      bool rhino3dm_build = false;
      bool rhinocommon_build = false;
      string dir_cpp;
      string dir_cs;
      string opennurbsPathReplacement = "";

      if (1 == args.Length && string.Equals(args[0], "rhino3dm", StringComparison.InvariantCultureIgnoreCase))
      {
        rhino3dm_build = true;
        // find directories for rhcommon_c and RhinoCommon
        GetProjectDirectories(out dir_cpp, out dir_cs, false);
      }
      else if (args.Length >= 2)
      {
        dir_cpp = args[0];
        dir_cs = args[1];
        if (args.Length > 2)
          opennurbsPathReplacement = args[2];
        if (args.Length > 3)
        {
          if (string.Equals(args[3], "rhino3dm", StringComparison.InvariantCultureIgnoreCase))
            rhino3dm_build = true;
        }
      }
      else
      {
        // See if there is a configuration file sitting in the same directory
        string location = System.Reflection.Assembly.GetExecutingAssembly().Location;
        string path = System.IO.Path.GetDirectoryName(location);
        path = System.IO.Path.Combine(path, "methodgen.cfg.txt");
        if (System.IO.File.Exists(path))
        {
          string[] lines = System.IO.File.ReadAllLines(path);
          dir_cpp = System.IO.Path.GetFullPath(lines[0]);
          dir_cs = System.IO.Path.GetFullPath(lines[1]);
          for (int i = 2; i < lines.Length; i++)
          {
            if (lines[i].StartsWith("using"))
            {
              m_includeRhinoDeclarations = false;
              m_extra_usings.Add(lines[i].Trim());
            }
          }
        }
        else
        {
          rhinocommon_build = true;
          // find directories for rhcommon_c and RhinoCommon
          GetProjectDirectories(out dir_cpp, out dir_cs, false);
        }
      }

      if (System.IO.Directory.Exists(dir_cpp) && !string.IsNullOrWhiteSpace(dir_cs))
      {
        Console.WriteLine("Parsing C files from {0}", dir_cpp);
        Console.WriteLine("Saving AutoNativeMethods.cs to {0}", dir_cs);
      }
      else
      {
        var color = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("ERROR: Unable to locate project directories");
        Console.ForegroundColor = color;
        Console.WriteLine("Press any key to exit");
        Console.Read();
        return 0;
      }

      var cmd = new CommandlineParser(args);
      m_namespace = cmd["namespace"];
      if (cmd.ContainsKey("IncludeRhinoDeclarations"))
        m_includeRhinoDeclarations = bool.Parse(cmd["IncludeRhinoDeclarations"]);

      var nmd = new NativeMethodDeclareFile();

      // get all of the .cpp files
      string[] files = System.IO.Directory.GetFiles(dir_cpp, "*.cpp");
      foreach (var file in files)
      {
        if (rhino3dm_build && (System.IO.Path.GetFileName(file).StartsWith("rh_") || System.IO.Path.GetFileName(file).StartsWith("tl_")))
          continue;
        nmd.BuildDeclarations(file, rhino3dm_build, opennurbsPathReplacement);
      }
      // get all of the .h files
      files = System.IO.Directory.GetFiles(dir_cpp, "*.h");
      foreach (var file in files)
        nmd.BuildDeclarations(file, rhino3dm_build, opennurbsPathReplacement);

      string output_file_methods = System.IO.Path.Combine(dir_cs, "AutoNativeMethods.cs");
      nmd.Write(output_file_methods, "lib");

      string output_file_enums = System.IO.Path.Combine(dir_cs, "AutoNativeEnums.cs");

      nmd.WriteEnums(output_file_enums);

      if (rhinocommon_build)
      {
        if (!GetProjectDirectories(out dir_cpp, out dir_cs, true))
        {
          System.Console.WriteLine("Can't locate RDK project directories. This is OK if you are compiling for standalone openNURBS build");
          return 0;
        }
        //write native methods for rdk
        nmd = new NativeMethodDeclareFile();

        // get all of the .cpp files
        files = System.IO.Directory.GetFiles(dir_cpp, "*.cpp");
        foreach (var file in files)
          nmd.BuildDeclarations(file, false, "");
        // get all of the .h files
        files = System.IO.Directory.GetFiles(dir_cpp, "*.h");
        foreach (var file in files)
          nmd.BuildDeclarations(file, false, "");

        output_file_methods = System.IO.Path.Combine(dir_cs, "AutoNativeMethodsRdk.cs");
        nmd.Write(output_file_methods, "librdk");

        output_file_enums = System.IO.Path.Combine(dir_cs, "AutoNativeEnumsRdk.cs");

        nmd.WriteEnums(output_file_enums);
      }

      return 0;
    }

    static bool GetProjectDirectories(out string c, out string dotnet, bool rdk)
    {
			c = null;
			dotnet = null;

      bool rc = false;
      // start with the directory that this executable is located in and work up
      string path = System.Reflection.Assembly.GetExecutingAssembly().Location;
      path = System.IO.Path.GetDirectoryName(path);
      var dir_info = new System.IO.DirectoryInfo(path);
      while(true)
      {
        if( string.Compare(dir_info.Name, "RhinoCommon", true)==0 )
        {
          c = System.IO.Path.Combine(dir_info.FullName, rdk ? "c_rdk" : "c");
          dotnet = System.IO.Path.Combine( dir_info.FullName, "dotnet");
          if( System.IO.Directory.Exists(c) && System.IO.Directory.Exists(dotnet) )
          {
            rc = true;
            break;
          }
        }
        // get parent directory
        dir_info = dir_info.Parent;
				if (dir_info == null)
					break;
      }

      if (!rc)
      {
        c = null;
        dotnet = null;
      }
      return rc;
    }

    // [Giulio] Sept 20, 2015: Brian suggested to use this VS-aware format
    // http://blogs.msdn.com/b/msbuild/archive/2006/11/03/msbuild-visual-studio-aware-error-messages-and-message-formats.aspx
    public static void VsSendConsoleMessage(bool error, string origin, int line, int errorCode, string message)
    {
      string err = error ? "error" : "warning";
      var result = string.Format("{1}({2}) : {3} {0} {4} : {5}",
        err, origin, line, "methodgen"/*subc*/, "mg"+errorCode.ToString("x6"), message
        );

      if (error)
        Console.Error.WriteLine(result);
      else
        Console.WriteLine(result);
    }
  }
}
