#pragma warning disable 1591
using System;
using System.Reflection;
using System.Collections.Generic;
using System.IO;

namespace Rhino.Runtime
{
  /// <summary> Assembly Resolver for the Rhino App Domain. </summary>
  public static class AssemblyResolver
  {
    internal static void InitializeAssemblyResolving()
    {
      if (null == m_assembly_resolve)
      {
        //Rhino.Runtime.HostUtils.DebugString("Assembly Resolver initialized\n");
        m_assembly_resolve = CurrentDomain_AssemblyResolve;
        AppDomain.CurrentDomain.AssemblyResolve += m_assembly_resolve;
      }
    }

    private static Dictionary<string, Assembly> m_match_dictionary;
    private static ResolveEventHandler m_assembly_resolve;
    private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
    {
      // Handle the RhinoCommon case.
      if (args.Name.StartsWith("RhinoCommon", StringComparison.OrdinalIgnoreCase) &&
         !args.Name.StartsWith("RhinoCommon.resources", StringComparison.OrdinalIgnoreCase))
        return Assembly.GetExecutingAssembly();

      // Get the significant name of the assembly we're looking for.
      string searchname = args.Name;
      // Do not attempt to handle resource searching.
      int index = searchname.IndexOf(".resources", StringComparison.Ordinal);
      if (index > 0)
        return null;

      // The resolver is commonly called multiple times with the same search name.
      // Keep the results around so we don't keep doing the same job over and over
      if (m_match_dictionary != null && m_match_dictionary.ContainsKey(args.Name))
        return m_match_dictionary[args.Name];

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

      // See if the assembly is already loaded.
      var loaded_assemblies = AppDomain.CurrentDomain.GetAssemblies();
      foreach (var loaded_assembly in loaded_assemblies)
      {
        if (loaded_assembly.FullName.StartsWith(searchname + ",", StringComparison.OrdinalIgnoreCase))
          return loaded_assembly;
      }



      List<string> potential_files = new List<string>();

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

      // Remove the already loaded assemblies from the "potential" list. We've
      // already tested these.
      for (int i = 0; i < loaded_assemblies.Length; i++)
      {
        if (loaded_assemblies[i].GlobalAssemblyCache)
          continue;
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
          for( int j=potential_files.Count-1; j>=0; j--)
          {
            string potential_filename = Path.GetFileName(potential_files[j]);
            if (string.Compare(filename, potential_filename, StringComparison.OrdinalIgnoreCase) == 0)
              potential_files.RemoveAt(j);
          }
        }
        catch { }
      }

#if RHINO_SDK
      // 7 Feb 2017 S. Baer (RH-30818)
      // Remove native DLLs from the list of potentials
      for( int i=potential_files.Count-1; i>=0; i-- )
      {
        if (!HostUtils.IsManagedDll(potential_files[i]))
          potential_files.RemoveAt(i);
      }
#endif

      // Sort all potential files based on fuzzy name matches.
      FuzzyComparer fuzzy = new FuzzyComparer(searchname);
      potential_files.Sort(fuzzy);

      // 23 August 2012 S. Baer
      // Make sure that at least part of the searchname matches part of the filename
      // Just use the first 5 characters as a required pattern in the filename
      const int length_match = 5;
      string must_be_in_filename = searchname.Substring(0, searchname.Length > length_match ? length_match : searchname.Length);

      Assembly asm = null;
      foreach (string file in potential_files)
      {
        if (file.IndexOf(must_be_in_filename, StringComparison.InvariantCultureIgnoreCase) == -1)
          continue;
        asm = TryLoadAssembly(file, searchname);
        if (asm != null)
          break;
      }

      if (m_match_dictionary == null)
        m_match_dictionary = new Dictionary<string, Assembly>();
      if (!m_match_dictionary.ContainsKey(args.Name))
        m_match_dictionary.Add(args.Name, asm);

      return asm;
    }

    private static Assembly TryLoadAssembly(string filename, string searchname)
    {
      // Don't try to load an assembly that already failed to load in the past.
      if (m_loadfailures != null && m_loadfailures.ContainsKey(filename))
        return null;


      // David: restrict loading to known file-types. Steve, you'll need to handle rhp loading as I have no idea how.
      if (filename.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)) //TODO: implement .rhp loading
      {
        try
        {
          // First load a Reflection Only flavour so we can trivially reject incompatible assemblies.
          Assembly ro_asm = Assembly.ReflectionOnlyLoadFrom(filename);

          if (!ro_asm.FullName.StartsWith(searchname + ",", StringComparison.OrdinalIgnoreCase))
            return null;

          // Load for real.
          Assembly asm = Assembly.LoadFile(filename);
          return asm;
        }
        catch (Exception)
        {
          // If the assembly fails to load, don't try again.
          if (m_loadfailures != null && !m_loadfailures.ContainsKey(filename))
          {
            m_loadfailures.Add(filename, false);
          }
        }
      }

      return null;
    }

    private class FuzzyComparer : IComparer<string>
    {
      private readonly string m_search;
      internal FuzzyComparer(string search)
      {
        m_search = search;
      }

#region IComparer<string> Members
      public int Compare(string x, string y)
      {
        bool xExists = File.Exists(x);
        bool yExists = File.Exists(y);

        if (!xExists && !yExists)
          return 0;

        if (!xExists) return +1;
        if (!yExists) return -1;

        // This can be made a lot smarter with substring searches.
        string xFileName = Path.GetFileName(x);
        string yFileName = Path.GetFileName(y);

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

            var xAssemblyName = AssemblyName.GetAssemblyName(x);
            var yAssemblyName = AssemblyName.GetAssemblyName(y);
            Version xVersion = xAssemblyName.Version;
            Version yVersion = yAssemblyName.Version;
            int rc = xVersion.CompareTo(yVersion);
            if (rc != 0)
              return rc;

            // Same version. Try using the file date
            FileInfo xInfo = new FileInfo(x);
            FileInfo yInfo = new FileInfo(y);
            rc = xInfo.LastAccessTimeUtc.CompareTo(yInfo.LastAccessTimeUtc);
            if (rc != 0)
              return rc;
          }
          catch (Exception ex)
          {
            HostUtils.ExceptionReport("duplicate assembly resolve", ex);
          }
        }

        if (xIndex < 0) xIndex = int.MaxValue;
        if (yIndex < 0) yIndex = int.MaxValue;
        return xIndex.CompareTo(yIndex);
      }
#endregion
    }

#region Additional Resolver Locations
    static readonly SortedList<string, bool> m_loadfailures = new SortedList<string, bool>();
    static readonly List<string> m_custom_folders = new List<string>();
    static readonly List<string> m_custom_files = new List<string>();

    /// <summary>
    /// Register a custom folder with the Assembly Resolver. Folders will be 
    /// searched recursively, so this could potentially be a very expensive operation. 
    /// If at all possible, you should consider only registering individual files.
    /// </summary>
    /// <param name="folder">Path of folder to include during Assembly Resolver events.</param>
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
}