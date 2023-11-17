#pragma warning disable 1591
using System;
using System.Reflection;
using System.Collections.Generic;
using System.IO;

namespace Rhino.Runtime
{
#if RHINO_SDK
  /// <summary> Assembly Resolver for the Rhino App Domain. </summary>
  public static class AssemblyResolver
  {
    static bool Initialized = false;
    private static readonly ResolverContext ExecutionContext = new ResolverContext(false);
    private static readonly ResolverContext ReflectionContext = new ResolverContext(true);

    private static string _logFilePath;
    private static string LogFilePath => _logFilePath ?? (_logFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "RhinoAssemblyResolveLog.txt"));
    static readonly bool LogEnabled = File.Exists(LogFilePath);
    private static void Log(params string[] contents) => File.AppendAllLines(LogFilePath, contents);

    internal static void InitializeAssemblyResolving()
    {
      if (!Initialized)
      {
        Initialized = true;
        //Rhino.Runtime.HostUtils.DebugString("Assembly Resolver initialized\n");

        if (LogEnabled)
        {
          // Curtis: Temporary logging to debug assembly directs when running rhino inside
          // to enable, create the file "RhinoAssemblyResolveLog.txt" on the desktop.
          Log($"*** Started Rhino: {DateTime.Now}");
        }

        AppDomain.CurrentDomain.AssemblyResolve += CurrentDomainAssemblyResolve;
        AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve += CurrentDomainReflectionOnlyAssemblyResolve;
      }
    }

    /// <summary>
    /// Standard resolver function used by Rhino in execution context.
    /// This is added to the Current <see cref="AppDomain.AssemblyResolve"/>.
    /// </summary>
    /// <since>7.9</since>
    public static ResolveEventHandler CurrentDomainAssemblyResolve => CurrentDomain_AssemblyResolve;

    /// <summary>
    /// Standard resolver function used by Rhino in reflection-only context.
    /// This is added to the Current <see cref="AppDomain.ReflectionOnlyAssemblyResolve"/>.
    /// </summary>
    /// <since>7.9</since>
    public static ResolveEventHandler CurrentDomainReflectionOnlyAssemblyResolve => CurrentDomain_ReflectionOnlyAssemblyResolve;

    private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
    {
      var assembly = ResolveAssembly(ExecutionContext, args);
      if (LogEnabled && assembly is object)
        Log
        (
          $"Assembly {args.RequestingAssembly?.FullName}",
          $"\trequires: '{args.Name}'",
          $"\tgot: '{assembly?.FullName}'"
        );

      return assembly;
    }

    private static Assembly CurrentDomain_ReflectionOnlyAssemblyResolve(object sender, ResolveEventArgs args)
    {
      var assembly = ResolveAssembly(ReflectionContext, args);
      if (LogEnabled && assembly is object)
        Log
        (
          $"(*) Assembly {args.RequestingAssembly?.FullName}",
          $"\trequires: '{args.Name}'",
          $"\tgot: '{assembly?.FullName}'"
        );

      return assembly;
    }

    private static Assembly ResolveAssembly(ResolverContext context, ResolveEventArgs args)
    {
      if (context.ReflectionOnly == false)
      {
        // Handle the RhinoCommon case.
        if (args.Name.StartsWith("RhinoCommon", StringComparison.OrdinalIgnoreCase) &&
           !args.Name.StartsWith("RhinoCommon.resources", StringComparison.OrdinalIgnoreCase))
          return Assembly.GetExecutingAssembly();
      }

      // Get the significant name of the assembly we're looking for.
      string searchname = args.Name;

      // Do not attempt to handle resource searching.
      int index = searchname.IndexOf(".resources", StringComparison.Ordinal);
      if (index > 0)
        return null;

      List<string> potential_files;

      lock (context)
      {
        // The resolver is commonly called multiple times with the same search name.
        // Keep the results around so we don't keep doing the same job over and over.
        if (context.Assemblies.TryGetValue(args.Name, out var match))
          return match;

        bool probably_python = false;
        index = searchname.IndexOf(',');
        if (index > 0)
        {
          searchname = searchname.Substring(0, index);
        }
        else
        {
          // Python scripts typically just look for very short names, like "MyFunctions.dll"
          if (searchname.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            searchname = searchname.Substring(0, searchname.Length - ".dll".Length);
          probably_python = true;
        }

        // See if an assembly with the same partial-name is already loaded.
        var loaded_assemblies = context.GetCurrentDomainAssemblies();
        foreach (var loaded_assembly in loaded_assemblies)
        {
          if (loaded_assembly.FullName.StartsWith(searchname + ",", StringComparison.OrdinalIgnoreCase))
            return loaded_assembly;
        }

        potential_files = new List<string>();

        // Collect all potential files in the plug-in directories.
#if RHINO_SDK
        string[] plugin_folders = HostUtils.GetAssemblySearchPaths();
        if (plugin_folders != null)
        {
          foreach (string plugin_folder in plugin_folders)
          {
            string[] files = Directory.GetFiles(plugin_folder, @"*.dll", SearchOption.TopDirectoryOnly); //Why TopDirectoryOnly?
            if (files != null) { potential_files.AddRange(files); }

            files = Directory.GetFiles(plugin_folder, @"*.rhp", SearchOption.TopDirectoryOnly); //Why TopDirectoryOnly?
            if (files != null) { potential_files.AddRange(files); }
          }
        }
#endif
        if (probably_python)
        {
          string current_dir = Directory.GetCurrentDirectory();
          if (!string.IsNullOrEmpty(current_dir))
          {
            string[] files = Directory.GetFiles(current_dir, @"*.dll", SearchOption.TopDirectoryOnly); //Why TopDirectoryOnly?
            if (files != null) { potential_files.AddRange(files); }
          }
        }

        // Collect all potential files in the custom directories.
        if (m_custom_folders != null)
        {
          foreach (string custom_folder in m_custom_folders)
          {
            string[] files = Directory.GetFiles(custom_folder, @"*.dll", SearchOption.TopDirectoryOnly); //Why TopDirectoryOnly?
            if (files != null) { potential_files.AddRange(files); }

            files = Directory.GetFiles(custom_folder, @"*.rhp", SearchOption.TopDirectoryOnly); //Why TopDirectoryOnly?
            if (files != null) { potential_files.AddRange(files); }
          }
        }

        // Collect all potential files in the custom file list.
        if (m_custom_files != null) { potential_files.AddRange(m_custom_files); }

        // Remove the already loaded assemblies from the "potential" list.
        // We've already tested these.
        for (int i = 0; i < loaded_assemblies.Length; i++)
        {
#if NETFRAMEWORK
          if (loaded_assemblies[i].GlobalAssemblyCache)
            continue;
#endif
          if (loaded_assemblies[i].IsDynamic)
            continue; //dynamic assemblies won't have a Location anyways.
          try
          {
            // 6 Feb 2017 S. Baer (RH-35794)
            // Remove loaded assemblies using filename as comparison. On systems
            // where the same assembly is located in multiple search path locations
            // we will start throwing exceptions when attempting to load those
            // assemblies (even if they are not the ones we are looking for).
            string filename = Path.GetFileName(loaded_assemblies[i].Location);
            for (int j = potential_files.Count - 1; j >= 0; j--)
            {
              string potential_filename = Path.GetFileName(potential_files[j]);
              if (filename.Equals(potential_filename, StringComparison.OrdinalIgnoreCase))
                potential_files.RemoveAt(j);
            }
          }
          catch { }
        }

#if RHINO_SDK
        // 7 Feb 2017 S. Baer (RH-30818)
        // Remove native DLLs from the list of potentials
        for (int i = potential_files.Count - 1; i >= 0; i--)
        {
          if (!HostUtils.IsManagedDll(potential_files[i]))
            potential_files.RemoveAt(i);
        }
#endif

        try
        {
          // Sort all potential files based on fuzzy name matches.
          FuzzyComparer fuzzy = new FuzzyComparer(searchname);
          potential_files.Sort(fuzzy);
        }
        catch (Exception ex)
        {
          // in case any other errors pop up (See RH-66941)
          HostUtils.ExceptionReport("FuzzyComparer", ex);
        }
      }
      
      // Curtis: RH-75970
      // Don't execute this within the context lock, we can find ourselves back
      // here when loading from different threads.

      // 23 August 2012 S. Baer
      // Make sure that at least part of the searchname matches part of the filename
      // Just use the first 5 characters as a required pattern in the filename
      const int length_match = 5;
      string must_be_in_filename = searchname.Substring(0, Math.Min(searchname.Length, length_match));

      Assembly asm = null;
      foreach (string file in potential_files)
      {
        if (file.IndexOf(must_be_in_filename, StringComparison.InvariantCultureIgnoreCase) == -1)
          continue;

        asm = TryLoadAssembly(context, file, searchname);
        if (asm != null)
          break;
      }

      lock (context)
      {

        // Keep the results around so we don't keep doing the same job over and over.
        context.Assemblies[args.Name] = asm;
        return asm;
      }
    }

    private static Assembly TryLoadAssembly(ResolverContext context, string filename, string searchname)
    {
      // Don't try to load an assembly that already failed to load in the past.
      lock (context.LoadFailures)
      {
        if (context.LoadFailures.Contains(filename))
          return null;
      }

      // David: restrict loading to known file-types. Steve, you'll need to handle rhp loading as I have no idea how.
      if (filename.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)) //TODO: implement .rhp loading
      {
        try
        {
          // First get assembly display-name so we can trivially reject incompatible assemblies.
          var asm_name = AssemblyName.GetAssemblyName(filename);
          if (!asm_name.Name.Equals(searchname, StringComparison.OrdinalIgnoreCase))
            return null;

          // Load for real.
          Assembly asm = context.LoadFile(filename);
          return asm;
        }
        catch
        {
          lock (context.LoadFailures)
          {
            // If the assembly fails to load, don't try again.
            context.LoadFailures.Add(filename);
          }
        }
      }

      return null;
    }

    class ResolverContext
    {
      public readonly bool ReflectionOnly;

      public ResolverContext(bool reflectionOnly) => ReflectionOnly = reflectionOnly;

      /// <summary>
      /// Dictionary of the Assembly instances loaded by this AssemblyResolverContext.
      /// </summary>
      public Dictionary<string, Assembly> Assemblies = new Dictionary<string, Assembly>();

      /// <summary>
      /// Dictionary of the Assembly locations that failed to load.
      /// </summary>
      public HashSet<string> LoadFailures = new HashSet<string>(default(PathEqualityComparer));
      struct PathEqualityComparer : IEqualityComparer<string>
      {
        public bool Equals(string x, string y) => x.ToLowerInvariant().Equals(y.ToLowerInvariant());
        public int GetHashCode(string obj) => obj.ToLowerInvariant().GetHashCode();
      }

      /// <summary>
      /// Gets the assemblies that have been loaded into the appropiate context of the
      /// current application domain.
      /// </summary>
      /// <returns>An array of assemblies in this application domain.</returns>
      public Assembly[] GetCurrentDomainAssemblies()
      {
        if (ReflectionOnly && !HostUtils.RunningInNetCore)
          return AppDomain.CurrentDomain.ReflectionOnlyGetAssemblies();
        else
          return AppDomain.CurrentDomain.GetAssemblies();
      }

      /// <summary>
      /// Loads an assembly into the appropiate context, given its path.
      /// </summary>
      /// <param name="path">The fully qualified path of the file to load.</param>
      /// <returns>The loaded assembly.</returns>
      public Assembly LoadFile(string path)
      {
#if NETFRAMEWORK
        if (ReflectionOnly)
          return Assembly.ReflectionOnlyLoad(File.ReadAllBytes(path));
        else
          return Rhino.Runtime.HostUtils.LoadAssemblyFrom(path);
#else 
          return Rhino.Runtime.HostUtils.LoadAssemblyFrom(path);
#endif
      }
    }

    private class FuzzyComparer : IComparer<string>
    {
      private readonly string m_search;
      private readonly string m_rhinoCommonPath;
      internal FuzzyComparer(string search)
      {
        m_search = search;
        m_rhinoCommonPath = HostUtils.RhinoAssemblyDirectory;
      }

      #region IComparer<string> Members
      public int Compare(string x, string y)
      {
        int rc;
        bool xExists = File.Exists(x);
        bool yExists = File.Exists(y);

        if (!xExists && !yExists)
          return 0;

        if (!xExists) return +1;
        if (!yExists) return -1;

        // This can be made a lot smarter with substring searches.
        string xFileName = Path.GetFileNameWithoutExtension(x);
        string yFileName = Path.GetFileNameWithoutExtension(y);

        int xIndex = xFileName == null ? -1 : xFileName.IndexOf(m_search, StringComparison.OrdinalIgnoreCase);
        int yIndex = yFileName == null ? -1 : yFileName.IndexOf(m_search, StringComparison.OrdinalIgnoreCase);

        // 4 April 2012 - S. Baer
        // If the file names are the same, the highest version number or most
        // recent file date is sorted to the top.  Plug-ins like PanelingTools
        // have historically moved around on where they are installed on a user's
        // computer, so we may end up with duplicates
        if (xIndex >= 0 && yIndex >= 0 && string.Compare(xFileName, yFileName, StringComparison.OrdinalIgnoreCase) == 0)
        {
          try
          {
            // 25 August 2015 - David Rutten:
            // Error reports:
            // http://mcneel.myjetbrains.com/youtrack/issue/RH-30818
            // https://app.raygun.io/dashboard/6bsfxg/errors/561473847 
            // seem to originate on one of the ReflectionOnlyLoadFrom calls below, 
            // but they're already in a try_catch block. I'm really not sure how to 
            // determine whether an assembly will load prior to loading it.

            // 3 Oct 2017 - S. Baer (RH-35794)
            // Reflection loading two assemblies into the same appdomain is asking for exceptions
            // Luckily, there is another way... Use AssemblyName.GetAssemblyName

            // 28 Apr 2022 - Curtis W (RH-66941)
            // Reworked this to do the following:
            //   - Prefer the assembly with the exact assembly name we are looking for
            //        E.g. searching for "Newtonsoft.Json" should prefer that over "Newtonsoft.Json.Bson"
            //   - If they are both exact names, prefer the LATEST version of an assembly since any code requiring the new changes would then break.
            //        E.g. Assembly with version 1.4 should be preferred over 1.2
            //   - If they are both the same version, prefer the copy that we ship with Rhino
            //   - If all else fails, check write time and prefer the latest file written (hopefully rare case)

            var xAssemblyName = AssemblyName.GetAssemblyName(x);
            var yAssemblyName = AssemblyName.GetAssemblyName(y);

            // Prefer the one that actually has the correct assembly name, vs. just containing the name
            var xIsAssemblyName = string.Compare(xAssemblyName.Name, m_search, StringComparison.OrdinalIgnoreCase) == 0;
            var yIsAssemblyName = string.Compare(yAssemblyName.Name, m_search, StringComparison.OrdinalIgnoreCase) == 0;
            rc = yIsAssemblyName.CompareTo(xIsAssemblyName);
            if (rc != 0)
              return rc;

            // Prefer the LATEST version
            var xVersion = xAssemblyName.Version;
            var yVersion = yAssemblyName.Version;
            rc = yVersion.CompareTo(xVersion);
            if (rc != 0)
              return rc;

            // Same version. Prefer Rhino's copy
            var xIsRhino = x.StartsWith(m_rhinoCommonPath);
            var yIsRhino = y.StartsWith(m_rhinoCommonPath);
            rc = yIsRhino.CompareTo(xIsRhino);
            if (rc != 0)
              return rc;

            // No Rhino copy of the dll (or we shipped two copies of the same dll),
            // so use the file write date and use the latest one written.
            // Use LastWriteTimeUtc as LastAccessTimeUtc gives inconsistent results
            // since we are acessing the files during this comparison.
            var xInfo = new FileInfo(x);
            var yInfo = new FileInfo(y);
            rc = yInfo.LastWriteTimeUtc.CompareTo(xInfo.LastWriteTimeUtc);
            if (rc != 0)
              return rc;
          }
          catch (Exception ex)
          {
            HostUtils.ExceptionReport("duplicate assembly resolve", ex);
          }
        }

        // file names are not the same, prefer the one that actually matches what we're looking for.
        // note when we actually load the assembly we still check to ensure the assembly name matches
        var xIsMatchingFileName = string.Compare(xFileName, m_search, StringComparison.OrdinalIgnoreCase) == 0;
        var yIsMatchingFileName = string.Compare(yFileName, m_search, StringComparison.OrdinalIgnoreCase) == 0;
        rc = yIsMatchingFileName.CompareTo(xIsMatchingFileName);
        if (rc != 0)
          return rc;

        // prefer the one where the search string is earliest in the file name
        // but at this point we really didn't find anything and it shouldn't be loaded.
        if (xIndex < 0) xIndex = int.MaxValue;
        if (yIndex < 0) yIndex = int.MaxValue;
        return xIndex.CompareTo(yIndex);
      }
      #endregion
    }

#region Additional Resolver Locations
    static readonly List<string> m_custom_folders = new List<string>();
    static readonly List<string> m_custom_files = new List<string>();

    /// <summary>
    /// Register a custom folder with the Assembly Resolver. Folders will be 
    /// searched recursively, so this could potentially be a very expensive operation. 
    /// If at all possible, you should consider only registering individual files.
    /// </summary>
    /// <param name="folder">Path of folder to include during Assembly Resolver events.</param>
    /// <since>5.0</since>
    public static void AddSearchFolder(string folder)
    {
      // ? is it smart to discard the folder this early on?
      if (!System.IO.Directory.Exists(folder)) { return; }

      // Don't add duplicate folders
      foreach (string existing_folder in m_custom_folders)
      {
        if (existing_folder.Equals(folder, StringComparison.OrdinalIgnoreCase)) { return; }
      }

      m_custom_folders.Add(folder);
    }
    /// <summary>
    /// Register another file with the Assembly Resolver. File must be a .NET assembly, 
    /// so it should probably be a dll, rhp or exe.
    /// </summary>
    /// <param name="file">Path of file to include during Assembly Resolver events.</param>
    /// <since>5.0</since>
    public static void AddSearchFile(string file)
    {
      // ? is it smart to discard the file this early on?
      if (!System.IO.File.Exists(file)) { return; }

      // Don't add duplicate files
      foreach (string existing_file in m_custom_files)
      {
        if (existing_file.Equals(file, StringComparison.OrdinalIgnoreCase)) { return; }
      }

      m_custom_files.Add(file);
    }
#endregion
  }
#endif
}
