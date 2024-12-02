#pragma warning disable 1591
using System;
using System.Collections.Generic;
using System.Reflection;
#if RHINO_SDK
using Rhino.Runtime.InteropWrappers;
#endif

// RMA_DONT_LOCALIZE (Tells the build process string parser to ignore this file)

namespace Rhino.UI
{
  /// <summary>
  /// Used a placeholder which is used by LocalizationProcessor application to create contextId
  /// mapped localized strings.
  /// </summary>
  public static class LOC
  {
    ///<summary>
    /// Strings that need to be localized should call this function. The STR function doesn't actually
    /// do anything but return the original string. The LocalizationProcessor application walks
    /// through the source code of a project and looks for LOC.STR. The function is then replaced with a
    /// call to Localization.LocalizeString using a unique context ID.
    ///</summary>
    ///<param name='english'>[in] The English string to localize.</param>
    /// <since>5.0</since>
    public static string STR(string english)
    {
      return english;
    }

    /// <summary>
    /// Similar to <see cref="string.Format(string, object)"/> function.
    /// </summary>
    /// <param name="english">The English name.</param>
    /// <param name="assemblyOrObject">Unused.</param>
    /// <returns>English name.</returns>
    /// <since>5.0</since>
    public static string STR(string english, object assemblyOrObject)
    {
      return english;
    }

    /// <summary>DO NOT use this function, it is a trap to determine
    /// where context IDs have been copied from other, already extracted,
    /// strings.
    /// </summary>
    /// <param name="english">The English name.</param>
    /// <param name="contextid">Copied context id.</param>
    /// <returns>English name.</returns>
    /// <since>7.0</since>
    /// <deprecated>7.1</deprecated>
    [Obsolete("Don't copy and paste context IDs", true)]
    public static string STR(string english, int contextid)
    {
      return english;
    }

    ///<summary>
    /// Command names that need to be localized should call this function. The COMMANDNAME function doesn't actually
    /// do anything but return the original string. The LocalizationProcessor application walks
    /// through the source code of a project and looks for LOC.COMMANDNAME and builds a record for each command
    /// name for the translators that can be used by developers in a commands overridden Rhino.Commands.Command.LocalName
    /// which should call Rhino.UI.Localization.LocalizeCommandName(EnglishName)
    ///</summary>
    ///<param name='english'>[in] The English string to localize.</param>
    /// <since>5.0</since>
    public static string COMMANDNAME(string english)
    {
      return english;
    }

    /// <summary>
    /// Command option name strings that need to be localized should call this function. The CON function
    /// doesn't actually do anything but return the original string. The LocalizationProcessor application walks
    /// through the source code of a project and looks for LOC.CON. The function is then replaced with a
    /// call to Localization.LocalizeCommandOptionName using a unique context ID.
    ///</summary>
    ///<param name='english'>[in] The English string to localize.</param>
    /// <returns>Returns localized string pair with both the English and local names set to the English value.</returns>
    /// <since>5.0</since>
    public static LocalizeStringPair CON(string english)
    {
      return new LocalizeStringPair(english, english);
    }

    /// <summary>
    /// Command option name strings that need to be localized should call this function. The CON function
    /// doesn't actually do anything but return the original string. The LocalizationProcessor application walks
    /// through the source code of a project and looks for LOC.CON. The function is then replaced with a
    /// call to Localization.LocalizeCommandOptionName using a unique context ID.
    ///</summary>
    ///<param name='english'>[in] The English string to localize.</param>
    ///<param name='assemblyFromObject'>[in] The object that identifies the assembly that owns the command option name.</param>
    /// <returns>Returns localized string pair with both the English and local names set to the English value.</returns>
    /// <since>5.0</since>
    public static LocalizeStringPair CON(string english, object assemblyFromObject)
    {
      return new LocalizeStringPair(english, english);
    }

    /// <summary>DO NOT use this function, it is a trap to determine
    /// where context IDs have been copied from other, already extracted,
    /// strings.
    /// </summary>
    /// <param name="english">The English name.</param>
    /// <param name="contextid">Copied context id.</param>
    /// <returns>English name.</returns>
    /// <since>7.0</since>
    /// <deprecated>7.1</deprecated>
    [Obsolete("Don't copy and paste context IDs", true)]
    public static LocalizeStringPair CON(string english, int contextid)
    {
      return new LocalizeStringPair(english, english);
    }

    /// <summary>
    /// Command option name strings that need to be localized should call this function. The COV function
    /// doesn't actually do anything but return the original string. The LocalizationProcessor application walks
    /// through the source code of a project and looks for LOC.COV. The function is then replaced with a
    /// call to Localization.LocalizeCommandOptionValue using a unique context ID.
    ///</summary>
    ///<param name='english'>[in] The English string to localize.</param>
    /// <returns>Returns localized string pair with both the English and local names set to the English value.</returns>
    /// <since>5.0</since>
    public static LocalizeStringPair COV(string english)
    {
      return new LocalizeStringPair(english, english);
    }

    /// <summary>
    /// Command option name strings that need to be localized should call this function. The COV function
    /// doesn't actually do anything but return the original string. The LocalizationProcessor application walks
    /// through the source code of a project and looks for LOC.COV. The function is then replaced with a
    /// call to Localization.LocalizeCommandOptionValue using a unique context ID.
    ///</summary>
    ///<param name='english'>[in] The English string to localize.</param>
    ///<param name='assemblyFromObject'>[in] The object that identifies the assembly that owns the command option value.</param>
    /// <returns>Returns localized string pair with both the English and local names set to the English value.</returns>
    /// <since>5.0</since>
    public static LocalizeStringPair COV(string english, object assemblyFromObject)
    {
      return new LocalizeStringPair(english, english);
    }
  }

  /// <since>5.0</since>
  public enum DistanceDisplayMode
  {
    Decimal = 0,
    Fractional = 1,
    FeetInches = 2
  }

  public interface ILocalizationService
  {
    /// <since>6.0</since>
    string LocalizeCommandName(Assembly assembly, int languageId, string english);
    /// <since>6.0</since>
    string LocalizeDialogItem(Assembly assembly, int languageId, string key, string english);
    /// <since>6.0</since>
    void LocalizeForm(Assembly assembly, int languageId, object formOrUserControl);
    /// <since>6.0</since>
    string LocalizeString(Assembly assembly, int languageId, string english, int contextId);
  }

  class DoNothingLocalizer : ILocalizationService
  {
    public string LocalizeCommandName(Assembly assembly, int languageId, string english) { return english; }
    public string LocalizeDialogItem(Assembly assembly, int languageId, string key, string english) { return english; }
    public void LocalizeForm(Assembly assembly, int languageId, object formOrUserControl) {  }
    public string LocalizeString(Assembly assembly, int languageId, string english, int contextId) { return english; }
  }

  public static class Localization
  {
    static ILocalizationService g_service_implementation;
    static ILocalizationService Service
    {
      get
      {
        if (g_service_implementation == null)
        {
          // This line doesn't work in the InstallLicense project when the file is used 
          // via an SVN external. Localization.cs is used outside RhinoCommon in InstallLicense.
          g_service_implementation = Runtime.HostUtils.GetPlatformService<ILocalizationService>();
          if (g_service_implementation == null)
            g_service_implementation = new DoNothingLocalizer();
        }
        return g_service_implementation;
      }
    }




#if RHINO_SDK
    /// <summary>
    /// Gets localized unit system name.  Uses current application locale id.
    /// </summary>
    /// <param name="units">The unit system.</param>
    /// <param name="capitalize">true if the name should be capitalized.</param>
    /// <param name="singular">true if the name is expressed for a singular element.</param>
    /// <param name="abbreviate">true if name should be the abbreviation.</param>
    /// <returns>The unit system name.</returns>
    /// <since>5.0</since>
    public static string UnitSystemName(UnitSystem units, bool capitalize, bool singular, bool abbreviate)
    {
      using (var sh = new StringHolder())
      {
        IntPtr pString = sh.NonConstPointer();
        UnsafeNativeMethods.CRhinoApp_UnitSystemName((int)units, capitalize, singular, abbreviate, pString);
        return sh.ToString();
      }
    }

    /// <summary>
    /// Get a string version of a number in a given unit system / display mode.
    /// </summary>
    /// <param name="x">The number to format into a string.</param>
    /// <param name="units">The unit system for the number.</param>
    /// <param name="mode">How the number should be formatted.</param>
    /// <param name="precision">The precision of the number.</param>
    /// <param name="appendUnitSystemName">Adds unit system name to the end of the number.</param>
    /// <returns>The formatted number.</returns>
    /// <since>5.0</since>
    public static string FormatNumber( double x, UnitSystem units, DistanceDisplayMode mode, int precision, bool appendUnitSystemName )
    {
      using (var sh = new StringHolder())
      {
        IntPtr pString = sh.NonConstPointer();
        UnsafeNativeMethods.RHC_RhinoFormatNumber(x, (int)units, (int)mode, precision, appendUnitSystemName, pString);
        return sh.ToString();
      }
    }

    /// <summary>
    /// Get a string version of a number.
    /// </summary>
    /// <param name="x">The number to format into a string.</param>
    /// <returns>The formatted number.</returns>
    /// <since>8.0</since>
    public static string FormatNumber(double x)
    {
      using (var sh = new StringHolder())
      {
        IntPtr pString = sh.NonConstPointer();
        UnsafeNativeMethods.RHC_RhinoFormatNumber2(x, pString);
        return sh.ToString();
      }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="distance"></param>
    /// <param name="units"></param>
    /// <param name="dimStyle"></param>
    /// <param name="alternate">primary or alternate</param>
    /// <returns></returns>
    /// <since>7.0</since>
    public static string FormatDistanceAndTolerance(double distance, UnitSystem units, Rhino.DocObjects.DimensionStyle dimStyle, bool alternate)
    {
      using (var sw = new StringWrapper())
      {
        IntPtr pString = sw.NonConstPointer;
        IntPtr const_ptr_dimstyle = dimStyle == null ? IntPtr.Zero : dimStyle.ConstPointer();
        UnsafeNativeMethods.ON_TextContext_FormatDistanceAndTolerance(distance, units, const_ptr_dimstyle, alternate, pString);
        return sw.ToString();
      }
    }

    /// <summary>
    /// Format an Area string from a number
    /// </summary>
    /// <param name="area"></param>
    /// <param name="units"></param>
    /// <param name="dimStyle"></param>
    /// <param name="alternate"></param>
    /// <returns></returns>
    /// <since>7.0</since>
    public static string FormatArea(double area, UnitSystem units, Rhino.DocObjects.DimensionStyle dimStyle, bool alternate)
    {
      using (var sw = new StringWrapper())
      {
        IntPtr pString = sw.NonConstPointer;
        IntPtr const_ptr_dimstyle = dimStyle == null ? IntPtr.Zero : dimStyle.ConstPointer();
        UnsafeNativeMethods.ON_TextContext_FormatArea(area, units, const_ptr_dimstyle, alternate, pString);
        return sw.ToString();
      }
    }

    /// <summary>
    /// Format a Volume string from a number
    /// </summary>
    /// <param name="volume"></param>
    /// <param name="units"></param>
    /// <param name="dimStyle"></param>
    /// <param name="alternate"></param>
    /// <returns></returns>
    /// <since>7.0</since>
    public static string FormatVolume(double volume, UnitSystem units, Rhino.DocObjects.DimensionStyle dimStyle, bool alternate)
    {
      using (var sw = new StringWrapper())
      {
        IntPtr pString = sw.NonConstPointer;
        IntPtr const_ptr_dimstyle = dimStyle == null ? IntPtr.Zero : dimStyle.ConstPointer();
        UnsafeNativeMethods.ON_TextContext_FormatVolume(volume, units, const_ptr_dimstyle, alternate, pString);
        return sw.ToString();
      }
    }



#endif
    /// <summary>
    /// Returns localized version of a given English string. This function should be auto-generated by the
    /// RmaLDotNetLocalizationProcessor application for every function that uses RMASTR.
    /// </summary>
    /// <param name="english">The text in English.</param>
    /// <param name="contextId">The context ID.</param>
    /// <returns>The localized string.</returns>
    /// <since>5.0</since>
    public static string LocalizeString(string english, int contextId)
    {
      if( RunningAsEnglish )
        return english;
      Assembly assembly = Assembly.GetCallingAssembly();
      return Service.LocalizeString(assembly, CurrentLanguageId, english, contextId);
    }



    /// <summary>
    /// DO NOT use this function, it is a trap to determine
    /// where context IDs have been copied from other, already extracted,
    /// strings.
    /// </summary>
    /// <param name="english">The text in English.</param>
    /// <param name="wrongcontextId">The copied ID.</param>
    /// <param name="contextId">The context ID.</param>
    /// <returns>The english string.</returns>
    /// <since>7.0</since>
    /// <deprecated>7.1</deprecated>
    [Obsolete("Don't copy and paste context IDs", true)]
    public static string LocalizeString(string english, int wrongcontextId, int contextId)
    {
      return english;
    }

    /// <summary>
    /// Returns localized version of a given English string. This function should be auto-generated by the
    /// RmaLDotNetLocalizationProcessor application for every function that uses RMASTR.
    /// </summary>
    /// <param name="english">The text in English.</param>
    /// <param name="assemblyOrObject">An assembly or an object from an assembly.</param>
    /// <param name="contextId">The context ID.</param>
    /// <returns>The localized string.</returns>
    /// <since>5.0</since>
    public static string LocalizeString(string english, object assemblyOrObject, int contextId)
    {
      if( RunningAsEnglish )
        return english;
      Assembly assembly = GetAssemblyFromObject(assemblyOrObject);
      return Service.LocalizeString(assembly, CurrentLanguageId, english, contextId);
    }
    /// <summary>
    /// Look in the dialog item list for the specified key and return the translated
    /// localized string if the key is found otherwise return the English string.
    /// </summary>
    /// <param name="assemblyOrObject">An assembly or an object from an assembly.</param>
    /// <param name="key"></param>
    /// <param name="english">The text in English.</param>
    /// <returns>
    /// Look in the dialog item list for the specified key and return the translated
    /// localized string if the key is found otherwise return the English string.
    /// </returns>
    /// <since>5.5</since>
    public static string LocalizeDialogItem(object assemblyOrObject, string key, string english)
    {
      if( RunningAsEnglish )
        return english;
      Assembly assembly = GetAssemblyFromObject(assemblyOrObject);
      return Service.LocalizeDialogItem(assembly, CurrentLanguageId, key, english);
    }
    /// <summary>
    /// Check to see if the passed object is an assembly, if not then get the assembly that owns the object type.
    /// </summary>
    /// <param name="assemblyOrObject">An assembly or an object from an assembly.</param>
    /// <returns>The localized string.</returns>
    /// <since>5.0</since>
    private static Assembly GetAssemblyFromObject(object assemblyOrObject)
    {
      Assembly assembly = assemblyOrObject as Assembly;
      if (null == assembly)
      {
        Type type = assemblyOrObject.GetType();
        assembly = type.Assembly;
      }
      return assembly;
    }

    /// <summary>
    /// Look in the dialog item list for the specified key and return the translated
    /// localized string if the key is found otherwise return the English string.
    /// </summary>
    /// <param name="formOrUserControl"></param>
    /// <since>6.0</since>
    public static void LocalizeForm(object formOrUserControl)
    {
      if (RunningAsEnglish)
        return;
      var assembly = GetAssemblyFromObject(formOrUserControl);
      Service.LocalizeForm(assembly, CurrentLanguageId, formOrUserControl);
    }
    ///<summary>
    /// Commands that need to be localized should call this function.
    ///</summary>
    ///<param name='english'>The localized command name.</param>
    /// <since>5.0</since>
    public static string LocalizeCommandName(string english)
    {
      if( RunningAsEnglish )
        return english;
      Assembly assembly = Assembly.GetCallingAssembly();
      return Service.LocalizeCommandName(assembly, CurrentLanguageId, english);
    }

    /// <since>5.0</since>
    public static string LocalizeCommandName(string english, object assemblyOrObject)
    {
      if( RunningAsEnglish )
        return english;
      Assembly assembly = GetAssemblyFromObject(assemblyOrObject);
      return Service.LocalizeCommandName(assembly, CurrentLanguageId, english);
    }

    /// <since>5.0</since>
    public static LocalizeStringPair LocalizeCommandOptionName(string english, int contextId)
    {
      Assembly assembly = Assembly.GetCallingAssembly();
      return LocalizeCommandOptionName(english, assembly, contextId);
    }

    /// <since>5.0</since>
    public static LocalizeStringPair LocalizeCommandOptionName(string english, object assemblyOrObject, int contextId)
    {
      string local = LocalizeString(english, assemblyOrObject, contextId);
      return new LocalizeStringPair(english, local);
    }

    /// <summary>
    /// DO NOT use this function, it is a trap to determine
    /// where context IDs have been copied from other, already extracted,
    /// strings.
    /// </summary>
    /// <param name="english">The text in English.</param>
    /// <param name="wrongcontextId">The copied ID.</param>
    /// <param name="contextId">The context ID.</param>
    /// <returns>The english string.</returns>
    /// <since>7.0</since>
    /// <deprecated>7.1</deprecated>
    [Obsolete("Don't copy and paste context IDs", true)]
    public static LocalizeStringPair LocalizeCommandOptionName(string english, int wrongcontextId, int contextId)
    {
      return new LocalizeStringPair(english, english);
    }

    /// <since>5.0</since>
    public static LocalizeStringPair LocalizeCommandOptionValue(string english, int contextId)
    {
      Assembly assembly = Assembly.GetCallingAssembly();
      return LocalizeCommandOptionValue(english, assembly, contextId);
    }

    /// <since>5.0</since>
    public static LocalizeStringPair LocalizeCommandOptionValue(string english, object assemblyOrObject, int contextId)
    {
      string local = LocalizeString(english, assemblyOrObject, contextId);
      return new LocalizeStringPair(english, local);
    }

    internal static void SetHooks()
    {
#if RHINO_SDK
      UnsafeNativeMethods.CRhinoUiHooks_SetLocalizationLocaleId(g_set_current_language_id);
#endif
    }
    internal delegate void SetCurrentLanguageIdDelegate(int localeId);
    internal static readonly SetCurrentLanguageIdDelegate g_set_current_language_id = SetCurrentLanguageId;
    [MonoPInvokeCallback(typeof(SetCurrentLanguageIdDelegate))]
    private static void SetCurrentLanguageId(int localeId)
    {
      g_language_id = localeId;
    }
    static int g_language_id = -1;

    /// <since>6.0</since>
    public static int CurrentLanguageId
    {
      get
      {
        // we don't want the language id to change since Rhino in general does not
        // support swapping localizations on the fly. Use a cached language id after the
        // initial language id has been read 
        if (g_language_id == -1)
        {
#if RHINO_SDK
          // 22 March 2021 John Morse
          // https://mcneel.myjetbrains.com/youtrack/issue/ZO-269
          // Localization is used by the Zoo so needs to run without RhinoCommon_c which
          // means PersistentSettings.RhinoAppSettings is not available. Added exception
          // handling for DllNotFoundException which will just cause g_language_id to
          // get set to AppearanceSettings.LanguageIdentifier like it did prior to the
          // RH-62744 fix.
          try
          {
            // This code is commonly called while working in theVisual Studio designer
            // and we want to try and not throw exceptions in order to show the winform
            var commandLineLocale = UnsafeNativeMethods.RHC_RhLocaleSpecifiedOnCommandLine();
            if (commandLineLocale > 0)
              return g_language_id = (int)commandLineLocale;

            // 11 Feb 2021 John Morse
            // https://mcneel.myjetbrains.com/youtrack/issue/RH-62744
            // There is core Rhino UI that is registered when the Rhino.UI is initialized
            // which happens prior to CRhinoAppearanceSettings::LoadProfile.  Calling 
            // AppearanceSettings.LanguageIdentifier before LoadProfile is called will always
            // return the default language of the OS causing strings to get localized in that
            // language until LoadProfile gets called.  This will check to see if the user 
            // changed the language and use the user provided value.
            PersistentSettings.RhinoAppSettings.TryGetChild("Options", out PersistentSettings options);
            if (options != null)
            {
              options.TryGetChild("Appearance", out PersistentSettings appearance);
              if (appearance != null && appearance.TryGetInteger("LanguageIdentifier", out int language))
                g_language_id = language;
            }
          }
          catch (DllNotFoundException)
          {
            g_language_id = -1;
          }
          // Did not find a value in settings so just use the default language value
          if (g_language_id == -1)
            g_language_id = ApplicationSettings.AppearanceSettings.LanguageIdentifier;
#else
          g_language_id = 1033;
#endif
        }
        return g_language_id;
      }
    }

    /// <since>6.0</since>
    public static bool RunningAsEnglish
    {
      get { return CurrentLanguageId == 1033; }
    }

    /// <summary>
    /// Sets the Id used for Localization in RhinoCommon.  Only useful for when
    /// using RhinoCommon outside of the Rhino process
    /// </summary>
    /// <param name="id"></param>
    /// <returns>true if the language id could be set</returns>
    /// <since>5.0</since>
    public static bool SetLanguageId(int id)
    {
#if RHINO_SDK
      if (Rhino.Runtime.HostUtils.RunningInRhino)
        return false;
#endif
      g_language_id = id;
      return true;
    }

#if RHINO_SDK
    /// <since>8.0</since>
    public static bool GetLanguages(out SimpleArrayInt ids, out ClassArrayString names)
    {
      ids = new SimpleArrayInt();
      names = new ClassArrayString();

      var ids_ptr_list = ids.NonConstPointer();
      var names_ptr_list = names.NonConstPointer();

      if (0 < UnsafeNativeMethods.CRhinoApp_GetInstalledLanguages(ids_ptr_list, names_ptr_list))
        return true;

      return false;
    }
#endif
  }

  /// <summary>Pair of strings used for localization.</summary>
  public sealed class LocalizeStringPair
  {
    /// <since>5.0</since>
    public LocalizeStringPair(string english, string local)
    {
      English = english;
      Local = local;
    }

    /// <since>5.0</since>
    public string English { get; private set; }
    /// <since>5.0</since>
    public string Local { get; private set; }

    public override string ToString() { return Local; }
    public static implicit operator string(LocalizeStringPair lcp){ return lcp.Local; }
  }
}
