using System;
using System.Collections.Generic;
using Rhino.UI;
using System.IO;
using System.Xml;
using System.Linq;

namespace Rhino.Render
{
  /// <summary>
  /// RenderContentManager's RestoreRenderContents method unpacks the 
  /// default render contents from the from the application and places them 
  /// in the User's folder.  Only available on Mac at the moment.
  /// </summary>
  public static class RenderContentManager
  {
    private class Language
    {
      public uint LCID;
      public string WinAbb;
      public string MacAbb;
      public string MacFolder
      {
        get { return MacAbb + ".lproj"; }
      }
    }

    private class RenderContentAsset
    {
      public string FullPath;
      public string EnglishName;
      public string TranslatedName;
      public bool IsDefault;
    }

    #region members
    private static int m_conflictCount;
    private static System.Reflection.MethodInfo m_getCreationTimeMethod;
    #endregion

    #region properties
    private static Dictionary<string, string> TranslationDictionary { get; set; }

    private static List<Language> Languages
    {
      get
      {
        var languages = new List<Language> {
          new Language { LCID = 1029, WinAbb = "cs-cz", MacAbb = "cs" },
          new Language { LCID = 1031, WinAbb = "de-de", MacAbb = "de" },
          new Language { LCID = 1033, WinAbb = "en-us", MacAbb = "en" },
          new Language { LCID = 3082, WinAbb = "es-es", MacAbb = "es" },
          new Language { LCID = 1036, WinAbb = "fr-fr", MacAbb = "fr" },
          new Language { LCID = 1040, WinAbb = "it-it", MacAbb = "it" },
          new Language { LCID = 1041, WinAbb = "ja-jp", MacAbb = "ja" },
          new Language { LCID = 1042, WinAbb = "ko-kr", MacAbb = "ko" },
          new Language { LCID = 1045, WinAbb = "pl-pl", MacAbb = "pl" },
          new Language { LCID = 2070, WinAbb = "pt-pt", MacAbb = "pt" },
          new Language { LCID = 1049, WinAbb = "ru-ru", MacAbb = "ru" },
          new Language { LCID = 2052, WinAbb = "zh-cn", MacAbb = "zh-Hans" },
          new Language { LCID = 1028, WinAbb = "zh-tw", MacAbb = "zh-Hant" }
        };
        return languages;
      }
    }

    private static string ResourcesPath
    {
      get
      {
        string resourcesPath = String.Empty;

        if (Rhino.Runtime.HostUtils.RunningOnOSX)
        {
          var rhinoExecutablePath = RhinoApp.GetExecutableDirectory().ToString();
          var rhinoRenderPath = Path.Combine(rhinoExecutablePath, "../PlugIns/RhinoRender.rhp");
          resourcesPath = Path.Combine(rhinoRenderPath, "Contents/Resources");
        }
        else if (Rhino.Runtime.HostUtils.RunningOnWindows)
        {
          //TODO: Currently the Render Content on Windows is copied during the installation.  We'll define this later
          //if we end up using this on Windows.
          resourcesPath = String.Empty;
        }

        return resourcesPath;
      }
    }

    private static string DefaultRenderContentPath
    {
      get
      {
        string renderContentPath = String.Empty;

        // Get the path to the RhinoRender.rhp assembly in the app bundle...
        if (Rhino.Runtime.HostUtils.RunningOnOSX)
        {
          var rhinoExecutablePath = RhinoApp.GetExecutableDirectory().ToString();
          var rhinoRenderPath = Path.Combine(rhinoExecutablePath, "../PlugIns/RhinoRender.rhp");
          renderContentPath = Path.Combine(rhinoRenderPath, "Contents/Resources/en.lproj/Render Content/");
        }
        else if (Rhino.Runtime.HostUtils.RunningOnWindows)
        {
          //TODO: Currently the Render Content on Windows is copied during the installation.  We'll define this later
          //if we end up using this on Windows.
          renderContentPath = String.Empty;
        }

        return renderContentPath;
      }
    }

    private static string VersionMarkerFilePath
    {
      get
      {
        return Path.Combine(UserRenderContentPath, "version.txt");
      }
    }

    /// <summary>
    /// Get the path to: 
    /// Windows: C:\Users\user\AppData\Roaming\McNeel\Rhinoceros\6.0\Localization\en-US\Render Content
    /// macOS: ~/Library/Application Support/McNeel/Rhinoceros/6.0/Render Content
    /// If a CustomLibraryPath is set, this is returned
    /// </summary>
    public static string UserRenderContentPath
    {
      get
      {
        string userRenderContentPath = String.Empty;

        // Check to see if Render Development Kit.Settings.RendererSupport.CustomLibraryPath is set
        var rdkId = Rhino.Render.InternalUtilities.RdkPlugin.Uuid;
        Rhino.PersistentSettings rdkSettings = null;
        Rhino.PersistentSettings.FromPlugInId(rdkId)?.TryGetChild("RendererSupport", out rdkSettings);
        string customLibraryPath = rdkSettings?.GetString("CustomLibraryPath");
        bool customLibraryPathSet = !String.IsNullOrEmpty(customLibraryPath);

        if (customLibraryPathSet)
        {
          userRenderContentPath = customLibraryPath;
        }
        else
        {
          var appDataDir = RhinoApp.GetDataDirectory(true, true);

          if (Rhino.Runtime.HostUtils.RunningOnOSX)
          {
            userRenderContentPath = Path.Combine(appDataDir, "Render Content");
          }
          else if (Rhino.Runtime.HostUtils.RunningOnWindows)
          {
            userRenderContentPath = Path.Combine(appDataDir, "Localization", "en-US", "Render Content");
          }
        }

        return userRenderContentPath;
      }
    }

    private static Language RhinoLanguage
    {
      get
      {
        Language rhinoLanguage = null;
        int LCID = Rhino.ApplicationSettings.AppearanceSettings.LanguageIdentifier;
        // The LCID for Spanish that Rhino is returning is throwing an exception for Spanish, so force it to get it "right"...
        System.Globalization.CultureInfo culture = LCID == 1034 ? new System.Globalization.CultureInfo(3082, false) : new System.Globalization.CultureInfo(LCID);
        string rhinoUserLanguage = culture.Name.ToLower();

        if (rhinoUserLanguage.StartsWith("en-", StringComparison.InvariantCulture))
        {
          rhinoLanguage = new Language { LCID = 1033, WinAbb = "en-us", MacAbb = "en" };
        }
        else
        {
          foreach (var language in Languages)
          {
            if (String.Equals(rhinoUserLanguage, language.WinAbb, StringComparison.InvariantCulture))
              rhinoLanguage = new Language { LCID = language.LCID, WinAbb = language.WinAbb, MacAbb = language.MacAbb };
          }
        }
        return rhinoLanguage;
      }
    }

    private static Language OSLanguage
    {
      get
      {
        Language osLanguage = null;
        uint osLCID = Rhino.Runtime.HostUtils.CurrentOSLanguage;
        var culture = new System.Globalization.CultureInfo((int)osLCID);
        string macOSlanguage = culture.Name.ToLower();

        if (macOSlanguage.StartsWith("en-", StringComparison.InvariantCulture))
        {
          osLanguage = new Language { LCID = 1033, WinAbb = "en-us", MacAbb = "en" };
        }
        else
        {
          foreach (var language in Languages)
          {
            if (String.Equals(macOSlanguage, language.WinAbb, StringComparison.InvariantCulture))
              osLanguage = new Language { LCID = language.LCID, WinAbb = language.WinAbb, MacAbb = language.MacAbb };
          }
        }
        return osLanguage;
      }
    }

    private static bool ShouldLocalize
    {
      get
      {
        return (RhinoLanguage != null) && !String.Equals(RhinoLanguage.MacAbb, "en", StringComparison.InvariantCulture);
      }
    }

    private static string OldSuffix => Localization.LocalizeString("old", 1809);

    private static System.Reflection.MethodInfo GetCreationTimeMethod
    {
      get
      {
        if (m_getCreationTimeMethod != null)
        {
          return m_getCreationTimeMethod;
        }
        else
        {
          m_getCreationTimeMethod = GetStaticMethod("RhinoMac", "RhinoMac.Runtime.MacPlatformService", "GetCreationTime");
          return m_getCreationTimeMethod;
        }
      }
    }
    #endregion

    #region methods
    /// <summary>
    /// Unpacks the default render contents from the from the application and places them in the User's folder.
    /// This will restore the default versions if the version of Rhino that is running is newer than the contents
    /// of the last Rhino that wrote the version.txt file.  If the version.txt file is not present, it will
    /// automatically restore the default contents.  This does not overwrite files that the user has changed.
    /// </summary>
    public static bool RestoreRenderContent()
    {
      if (Rhino.Runtime.HostUtils.RunningOnWindows)  // No support on Windows just yet
        return false;

      bool rc = false;
      m_conflictCount = 0;

      if (String.IsNullOrEmpty(DefaultRenderContentPath) || String.IsNullOrEmpty(UserRenderContentPath))
        return false;

      bool shouldRestoreContents = false;
      if (!File.Exists(VersionMarkerFilePath)) {
        shouldRestoreContents = true;
      } else {
        // Read version marker file and compare the versions
        string versionMarker = ReadVersionMarkerFile();
        if (!String.IsNullOrEmpty(versionMarker))
        {
          Version currentRunningVersion = RhinoApp.Version;
          bool didParseVersion = Version.TryParse(versionMarker, out Version versionMarkerVersion);
          if (didParseVersion)
          {
            var result = currentRunningVersion.CompareTo(versionMarkerVersion);
            if (result > 0) //currentRunningVersion is greater            
              shouldRestoreContents = true;
          }
          else
          {
            var ex = new Exception("rdk_render_content_manager: Could not parse the version in the Render Content version marker file");
            Rhino.Runtime.HostUtils.ExceptionReport(ex);
            return false;
          }
        }
        else
        {
          shouldRestoreContents = true;
        }
      }

      if (!shouldRestoreContents)
        return false;

      try
      {
        if (!Directory.Exists(UserRenderContentPath))
        {
          Directory.CreateDirectory(UserRenderContentPath);
        }
      }
      catch (Exception ex)
      {
        Rhino.Runtime.HostUtils.ExceptionReport(ex);
        return false;
      }

      // Read the materials xml
      if (ShouldLocalize)
      {
        //TODO: This next part is macOS specific, and we need a Windows counterpart...
        var materialsXMLPath = Path.Combine(ResourcesPath, Languages.Find(item => item.MacAbb == RhinoLanguage.MacAbb).MacFolder, "materials.xml");
        TranslationDictionary = ReadTranslationXML(materialsXMLPath);
        var textureTranslationTable = ReadTextureXML(materialsXMLPath);
      }

      // Populate a list of sourceRenderAssets with appropriate translated names if applicable
      string[] sourceRenderContentDirectories = Directory.GetDirectories(DefaultRenderContentPath, "*.*", SearchOption.AllDirectories);

      List<RenderContentAsset> sourceRenderAssets = new List<RenderContentAsset>();
      string[] sourceRenderContentFiles = Directory.GetFiles(DefaultRenderContentPath, "*.*", SearchOption.AllDirectories);
      foreach (string sourceRenderContentsFile in sourceRenderContentFiles)
      {
        RenderContentAsset asset = new RenderContentAsset { FullPath = sourceRenderContentsFile, IsDefault = true };
        sourceRenderAssets.Add(asset);
      }

      foreach (RenderContentAsset asset in sourceRenderAssets)
      {
        var splitSourceBasePath = asset.FullPath.Split(new string[] { "Render Content/" }, StringSplitOptions.None);
        var sourceBasePath = Path.Combine(splitSourceBasePath[0], "Render Content");
        asset.EnglishName = splitSourceBasePath[1];

        if (ShouldLocalize)
        {
          if (TranslationDictionary.ContainsKey(asset.EnglishName))
          {

            string translatedName = String.Empty;
            try
            {
              translatedName = TranslationDictionary[asset.EnglishName];
            }
            catch (Exception ex)
            {
              Rhino.Runtime.HostUtils.ExceptionReport(ex);
              return false;
            }

            asset.TranslatedName = translatedName;
          }
        }
      }

      bool userRenderContentPathWasEmpty = !Directory.EnumerateFileSystemEntries(UserRenderContentPath).Any();

      // Create all the destination dirs
      foreach (var sourceDirPath in sourceRenderContentDirectories)
      {
        var splitSourceBasePath = sourceDirPath.Split(new string[] { "Render Content/" }, StringSplitOptions.None);
        var sourceBasePath = Path.Combine(splitSourceBasePath[0], "Render Content");
        var sourceSubDir = splitSourceBasePath[1];

        string resolvedTargetDirName = Path.Combine(UserRenderContentPath, sourceSubDir);

        if (ShouldLocalize)
        {
          if (TranslationDictionary.ContainsKey(sourceSubDir))
          {
            var translatedSubDir = TranslationDictionary[sourceSubDir];
            resolvedTargetDirName = Path.Combine(UserRenderContentPath, translatedSubDir);
          }
        }

        try
        {
          if (!Directory.Exists(resolvedTargetDirName))
          {
            Directory.CreateDirectory(resolvedTargetDirName);
          }
        }
        catch (Exception ex)
        {
          Rhino.Runtime.HostUtils.ExceptionReport(ex);
          return false;
        }
      }

      if (userRenderContentPathWasEmpty)
      {
        rc = CopyAllSourceAssets(sourceRenderAssets);
      }
      else
      {
        // Now we need to build up a list of targets...
        List<RenderContentAsset> targetRenderAssets = new List<RenderContentAsset>();
        foreach (RenderContentAsset sourceRenderAsset in sourceRenderAssets)
        {
          string targetPath = String.Empty;

          if (ShouldLocalize)
            targetPath = Path.Combine(UserRenderContentPath, sourceRenderAsset.TranslatedName);
          else
            targetPath = Path.Combine(UserRenderContentPath, sourceRenderAsset.EnglishName);

          var targetRenderAsset = new RenderContentAsset
          {
            EnglishName = sourceRenderAsset.EnglishName,
            TranslatedName = sourceRenderAsset.TranslatedName,
            FullPath = targetPath
          };
          targetRenderAssets.Add(targetRenderAsset);
        }

        bool potentialConflicts = targetRenderAssets.Count > 0;

        // Now we need to check to see if the source and the target are the same file or not (isDefault)
        if (potentialConflicts)
        {
          foreach (RenderContentAsset targetAsset in targetRenderAssets)
          {
            if (!File.Exists(targetAsset.FullPath))
            {
              targetAsset.IsDefault = false;
            }
            else
            {
              // If the created and modifed times are the same, we can presume it is the default material
              DateTime creationTime = new DateTime();
              DateTime modifiedTime = new DateTime();
              // see: https://bugzilla.xamarin.com/show_bug.cgi?id=6118
              if (Rhino.Runtime.HostUtils.RunningOnWindows)
              {
                creationTime = File.GetCreationTime(targetAsset.FullPath);
              }
              else if (Rhino.Runtime.HostUtils.RunningOnOSX)
              {
                creationTime = (DateTime)GetCreationTimeMethod.Invoke(null, new object[] { targetAsset.FullPath });
              }
              modifiedTime = File.GetLastWriteTime(targetAsset.FullPath);

              if (TrimMilliseconds(creationTime) == TrimMilliseconds(modifiedTime))
              {
                targetAsset.IsDefault = true;
              }
              else
              {
                targetAsset.IsDefault = false;
                m_conflictCount++;
              }
            }
          }
        }

        if (m_conflictCount == 0)
        {
          rc = CopyAllSourceAssets(sourceRenderAssets);
        }
        else
        {
          List<RenderContentAsset> targetsToRestore = targetRenderAssets.Where(t => t.IsDefault).ToList();
          if (targetsToRestore.Count > 0)
            rc = RestoreTargetAssets(targetsToRestore, false);
        }
      }

      if (rc)
      {       
        WriteVersionMarkerFile();
        return true;
      }
      else
      {
        return false;
      }
    }

    private static Dictionary<string, string> ReadTranslationXML(string pathToXML)
    {
      Dictionary<string, string> translationDict = new Dictionary<string, string>();

      XmlDocument docXML = new XmlDocument();
      docXML.Load(pathToXML);

      foreach (XmlNode folderNode in docXML.GetElementsByTagName("Folder"))
      {
        var englishKey = String.Empty;
        var localizedValue = String.Empty;

        if (Rhino.Runtime.HostUtils.RunningOnWindows)
        {
          englishKey = folderNode.Attributes["English"]?.Value.Trim();
          localizedValue = folderNode.Attributes["Localized"]?.Value.Trim();
        }
        else if (Rhino.Runtime.HostUtils.RunningOnOSX)
        {
          englishKey = folderNode.Attributes["English"]?.Value.Replace("\\", "/").Trim();
          localizedValue = folderNode.Attributes["Localized"]?.Value.Replace("\\", "/").Trim();
        }

        translationDict.Add(englishKey, localizedValue);
      }

      foreach (XmlNode fileNode in docXML.GetElementsByTagName("File"))
      {
        var englishKey = String.Empty;
        var localizedValue = String.Empty;

        if (Rhino.Runtime.HostUtils.RunningOnWindows)
        {
          englishKey = fileNode.Attributes["English"]?.Value.Trim();
          localizedValue = fileNode.Attributes["Localized"]?.Value.Trim();
        }
        else if (Rhino.Runtime.HostUtils.RunningOnOSX)
        {
          englishKey = fileNode.Attributes["English"]?.Value.Replace("\\", "/").Trim();
          localizedValue = fileNode.Attributes["Localized"]?.Value.Replace("\\", "/").Trim();
        }

        translationDict.Add(englishKey, localizedValue);
      }

      return translationDict;
    }

    /// <summary>
    /// Read a materials.xml file and return a texture filename translation dictionary
    /// </summary>
    private static Dictionary<string, string> ReadTextureXML(string pathToXML)
    {
      Dictionary<string, string> translationDict = new Dictionary<string, string>();

      XmlDocument docXML = new XmlDocument();
      docXML.Load(pathToXML);

      foreach (XmlNode fileNode in docXML.GetElementsByTagName("File"))
      {
        var englishKey = fileNode.Attributes["English"]?.Value.Replace("\\", "/").Trim(); ;
        if (englishKey.Contains("Textures"))
        {
          var localizedValue = fileNode.Attributes["Localized"]?.Value.Replace("\\", "/").Trim();
          translationDict.Add(englishKey, localizedValue);
        }
      }

      return translationDict;
    }

    private static bool LocalizeEnvironmentFile(string sourcePath, string destinationPath, Dictionary<string, string> translations)
    {
      // NOTE: There are lots of .renv files and - rather than read each one - we know which need to be translated so just do
      // those.  This is mainly a performance concern when unpacking and translating the files themselves.  If we can tune up
      // the performance, this check should be removed.
      if (!(sourcePath.Contains("Studio.renv") ||
          sourcePath.Contains("Rhino Interior.renv") ||
          sourcePath.Contains("Rhino Sky.renv") ||
          sourcePath.Contains("Rhino Studio.renv") ||
          sourcePath.Contains("PreviewStudio.renv")))
        return true;

      var fileStream = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
      var file = new StreamReader(fileStream, System.Text.Encoding.UTF8, true, 128);

      List<string> keysFound = new List<string>();
      string line;
      while ((line = file.ReadLine()) != null)
      {
        if (line.Contains("<filename type=\"string\">..\\Textures"))
        {
          string englishKey = line.Split(new string[] { "..\\" }, StringSplitOptions.None)[1].Split('<')[0].Replace("\\", "/").Trim();
          keysFound.Add(englishKey);
        }
      }

      string contents = String.Empty;
      try
      {
        contents = File.ReadAllText(sourcePath);
      }
      catch (Exception ex)
      {
        Rhino.Runtime.HostUtils.ExceptionReport(ex);
        return false;
      }

      foreach (string key in keysFound)
      {
        if (translations.ContainsKey(key))
        {
          try
          {
            string pathToReplace = "..\\" + key.Replace("/", "\\");
            string replacementPath = "../" + translations[key];
            contents = contents.Replace(pathToReplace, replacementPath);
          }
          catch (Exception ex)
          {
            Rhino.Runtime.HostUtils.ExceptionReport(ex);
            return false;
          }
        }
      }

      try
      {
        File.WriteAllText(destinationPath, contents);
      }
      catch (Exception ex)
      {
        Rhino.Runtime.HostUtils.ExceptionReport(ex);
        return false;
      }

      return true;
    }

    private static bool RestoreTargetAssets(List<RenderContentAsset> targetRenderAssets, bool shouldKeepOldAssets)
    {
      bool rc = true;

      foreach (RenderContentAsset targetAsset in targetRenderAssets)
      {
        var resolvedSourceFilePath = Path.Combine(DefaultRenderContentPath, targetAsset.EnglishName);
        var resolvedTargetFilePath = ShouldLocalize ? Path.Combine(UserRenderContentPath, targetAsset.TranslatedName) : Path.Combine(UserRenderContentPath, targetAsset.EnglishName);

        if (shouldKeepOldAssets)
        {
          try
          {
            if (File.Exists(resolvedTargetFilePath))
            {
              var oldAssetFilename = Path.GetFileNameWithoutExtension(resolvedTargetFilePath) + "_" + OldSuffix + Path.GetExtension(resolvedTargetFilePath);
              var oldAssetPath = Path.Combine(Path.GetDirectoryName(resolvedTargetFilePath), oldAssetFilename);
              File.Copy(resolvedTargetFilePath, oldAssetPath, true);
            }
          }
          catch (Exception ex)
          {
            Rhino.Runtime.HostUtils.ExceptionReport(ex);
            return false;
          }
        }

        try
        {
          if (ShouldLocalize && String.Equals(Path.GetExtension(resolvedSourceFilePath), ".renv", StringComparison.InvariantCulture))
          {
            rc = LocalizeEnvironmentFile(resolvedSourceFilePath, resolvedTargetFilePath, TranslationDictionary);
          }
          else
          {
            File.Copy(resolvedSourceFilePath, resolvedTargetFilePath, true);
          }
        }
        catch (Exception ex)
        {
          Rhino.Runtime.HostUtils.ExceptionReport(ex);
          return false;
        }
      }

      return rc;
    }

    private static bool CopyAllSourceAssets(List<RenderContentAsset> sourceRenderAssets)
    {
      bool rc = true;
      foreach (RenderContentAsset sourceAsset in sourceRenderAssets)
      {
        var resolvedTargetFilePath = ShouldLocalize ? Path.Combine(UserRenderContentPath, sourceAsset.TranslatedName) : Path.Combine(UserRenderContentPath, sourceAsset.EnglishName);

        // If the asset is an environment (renv) file we need to translate the contents of the file too...
        if (ShouldLocalize && String.Equals(Path.GetExtension(sourceAsset.FullPath), ".renv", StringComparison.InvariantCulture))
        {
          rc = LocalizeEnvironmentFile(sourceAsset.FullPath, resolvedTargetFilePath, TranslationDictionary);
        }
        else
        {
          try
          {
            if (!File.Exists(resolvedTargetFilePath))
              File.Copy(sourceAsset.FullPath, resolvedTargetFilePath, false);
          }
          catch (Exception ex)
          {
            Rhino.Runtime.HostUtils.ExceptionReport(ex);
            rc = false;
          }
        }
      }

      return rc;
    }

    private static bool WriteVersionMarkerFile()
    {
      bool rc = false;

      FileInfo markerFile = new FileInfo(VersionMarkerFilePath);

      if (File.Exists(VersionMarkerFilePath))
      {
        try
        {
          File.Delete(VersionMarkerFilePath);
        }
        catch (Exception ex)
        {
          Rhino.Runtime.HostUtils.ExceptionReport(ex);
          rc = false;
        }
      }

      if (!File.Exists(VersionMarkerFilePath))
      {
        try
        {
          File.WriteAllText(VersionMarkerFilePath, RhinoApp.Version.ToString());
          return true;
        }
        catch (Exception ex)
        {
          Rhino.Runtime.HostUtils.ExceptionReport(ex);
          rc = false;
        }
      }

      return rc;
    }

    private static string ReadVersionMarkerFile()
    {
      string version = String.Empty;

      FileInfo markerFile = new FileInfo(VersionMarkerFilePath);

      if (File.Exists(VersionMarkerFilePath))
      {
        try
        {
          string[] lines = File.ReadAllLines(VersionMarkerFilePath);
          if (lines[0] != null)
            version = lines[0].Trim();
        }
        catch (Exception ex)
        {
          Rhino.Runtime.HostUtils.ExceptionReport(ex);
        }
      }

      return version;
    }

    private static System.Reflection.MethodInfo GetStaticMethod(string assemblyName, string className, string methodName)
    {
      var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
      foreach (var assembly in assemblies)
      {
        if (assembly.FullName.Contains(assemblyName))
        {
          Type t = assembly.GetType(className);
          if (t != null)
          {
            var mi = t.GetMethod(methodName, System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
            if (mi != null)
              return mi;
          }
        }
      }
      return null;
    }

    private static DateTime TrimMilliseconds(DateTime dt)
    {
      return new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second, 0, dt.Kind);
    }
    #endregion

  }
}
