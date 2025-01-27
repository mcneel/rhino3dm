#if RHINO_SDK

using System;
using System.Collections.Generic;
using System.Text;
using Rhino.ApplicationSettings;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Runtime;
using Rhino.Runtime.InteropWrappers;

namespace Rhino.ApplicationSettings
{
  /// <summary>License node types.</summary>
  /// <since>5.0</since>
  public enum LicenseNode
  {
    /// <summary>An independent node.</summary>
    Standalone = 0,
    /// <summary>Network (obtains license from Zoo server)</summary>
    Network = 1,
    /// <summary>Network (has license checked out from Zoo server)</summary>
    NetworkCheckedOut = 2
  }

  /// <summary>The type of Rhino executable that is executing</summary>
  /// <since>5.0</since>
  public enum Installation
  {
    ///<summary>Unknown</summary>
    Undefined = 0,
    ///<summary></summary>
    Commercial,
    ///<summary></summary>
    Educational,
    ///<summary></summary>
    EducationalLab,
    ///<summary></summary>
    NotForResale,
    ///<summary></summary>
    NotForResaleLab,
    ///<summary></summary>
    Beta,
    ///<summary></summary>
    BetaLab,
    ///<summary>25 Save limit evaluation version of Rhino</summary>
    Evaluation,
    ///<summary></summary>
    Corporate,
    ///<summary>90 day time limit evaluation version of Rhino</summary>
    EvaluationTimed
  }
}

namespace Rhino
{
  internal class InvokeHelper
  {
    class CallbackWithArgs
    {
      public CallbackWithArgs(Delegate callback, params object[] args)
      {
        Callback = callback;
        Args = args;
      }
      public Delegate Callback { get; private set; }
      public object[] Args { get; private set; }
    }
    static readonly object g_invoke_lock = new object();
    static List<CallbackWithArgs> g_callbacks;
    internal delegate void InvokeAction();
    private static InvokeAction g_on_invoke_callback;

    public void Invoke(Delegate method, params object[] args)
    {
      lock (g_invoke_lock)
      {
        if (g_callbacks == null)
          g_callbacks = new List<CallbackWithArgs>();
        g_callbacks.Add(new CallbackWithArgs(method, args));
      }

      if (g_on_invoke_callback == null)
        g_on_invoke_callback = InvokeCallback;
      UnsafeNativeMethods.CRhMainFrame_Invoke(g_on_invoke_callback);
    }

    /// <summary>
    /// See Control.InvokeRequired
    /// </summary>
    public bool InvokeRequired
    {
      get
      {
        return UnsafeNativeMethods.CRhMainFrame_InvokeRequired();
      }
    }

    private static void InvokeCallback()
    {
      try
      {
        CallbackWithArgs[] actions = null;
        lock (g_invoke_lock)
        {
          if (g_callbacks != null)
          {
            actions = g_callbacks.ToArray();
            g_callbacks.Clear();
          }
        }
        if (actions == null || actions.Length < 1)
          return;

        // 14 Dec 2020 S. Baer (RH-61572)
        // Attempt to track down cause of crash for this method by adding a risky action spy
        string name = actions[0].Callback.Method.Name;
        using (var riskyAction = new Rhino.Runtime.RiskyAction(name, "InvokeHelper.InvokeCallback"))
        {
          foreach (var item in actions)
            item.Callback.DynamicInvoke(item.Args);
        }
      }
      catch (Exception ex)
      {
        Runtime.HostUtils.ExceptionReport(ex);
      }
    }
  }

  /// <summary> Represents the top level window in Rhino </summary>
  [System.ComponentModel.Browsable(false), System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
  public class RhinoWindow : System.Windows.Forms.IWin32Window
  {
    readonly IntPtr m_handle;

    internal RhinoWindow(IntPtr handle)
    {
      m_handle = handle;
    }

    /// <summary></summary>
    /// <since>5.0</since>
    public IntPtr Handle
    {
      get { return m_handle; }
    }

    /// <summary>
    /// </summary>
    /// <param name="method"></param>
    /// <returns></returns>
    /// <since>5.0</since>
    public void Invoke(Delegate method)
    {
      RhinoApp.InvokeOnUiThread(method);
    }

    /// <summary> See Control.InvokeRequired </summary>
    /// <since>5.0</since>
    public bool InvokeRequired
    {
      get
      {
        return RhinoApp.InvokeRequired;
      }
    }
  }

  ///<summary>.NET RhinoApp is parallel to C++ CRhinoApp.</summary>
  public static class RhinoApp
  {
    internal static int GetInt(UnsafeNativeMethods.RhinoAppInt which)
    {
      return UnsafeNativeMethods.CRhinoApp_GetInt(which);
    }

    static bool GetBool(UnsafeNativeMethods.RhinoAppBool which)
    {
      return UnsafeNativeMethods.CRhinoApp_GetBool(which);
    }


    ///<summary>
    ///Rhino SDK 9 digit SDK version number in the form YYYYMMDDn
    ///
    ///Rhino will only load plug-ins that were build with exactly the
    ///same version of the SDK.
    ///</summary>
    /// <since>5.0</since>
    public static int SdkVersion
    {
      get { return GetInt(UnsafeNativeMethods.RhinoAppInt.SdkVersion); }
    }

    ///<summary>
    ///Rhino SDK 9 digit SDK service release number in the form YYYYMMDDn
    ///
    ///Service release of the Rhino SDK supported by this executable. Rhino will only
    ///load plug-ins that require a service release of &lt;= this release number.
    ///For example, SR1 will load all plug-ins made with any SDK released up through and including
    ///the SR1 SDK. But, SR1 will not load a plug-in built using the SR2 SDK. If an &quot;old&quot; Rhino
    ///tries to load a &quot;new&quot; plug-in, the user is told that they have to get a free Rhino.exe
    ///update in order for the plug-in to load. Rhino.exe updates are available from http://www.rhino3d.com.
    ///</summary>
    /// <since>5.0</since>
    public static int SdkServiceRelease
    {
      get { return GetInt(UnsafeNativeMethods.RhinoAppInt.SdkServiceRelease); }
    }

    ///<summary>
    ///Major version of Rhino executable 4, 5, ...
    ///</summary>
    /// <since>5.0</since>
    public static int ExeVersion
    {
      get { return GetInt(UnsafeNativeMethods.RhinoAppInt.ExeVersion); }
    }

    ///<summary>
    ///Service release version of Rhino executable (0, 1, 2, ...)  
    ///The integer is the service release number of Rhino.  For example,
    ///this function returns &quot;0&quot; if Rhino V4SR0 is running and returns
    ///&quot;1&quot; if Rhino V4SR1 is running.
    ///</summary>
    /// <since>5.0</since>
    public static int ExeServiceRelease
    {
      get { return GetInt(UnsafeNativeMethods.RhinoAppInt.ExeServiceRelease); }
    }

    /// <summary>
    /// Gets the build date.
    /// </summary>
    /// <since>5.0</since>
    public static DateTime BuildDate
    {
      get
      {
        int year = 0;
        int month = 0;
        int day = 0;
        UnsafeNativeMethods.CRhinoApp_GetBuildDate(ref year, ref month, ref day);
        // debug builds are 0000-00-00
        if( year==0 && month==0 && day==0 )
          return DateTime.MinValue;
        return new DateTime(year, month, day);
      }
    }

    /// <summary>
    /// McNeel version control revision identifier at the time this version
    /// of Rhino was built.
    /// </summary>
    /// <since>5.0</since>
    public static string VersionControlRevision
    {
      get
      {
        using (var sh = new StringHolder())
        {
          IntPtr ptr_string = sh.NonConstPointer();
          UnsafeNativeMethods.ON_Revision(ptr_string);
          return sh.ToString();
        }
      }
    }

    static Version g_version;
    /// <summary> File version of the main Rhino process </summary>
    /// <since>5.9</since>
    public static Version Version
    {
      get { return g_version ?? (g_version = new Version(RhinoBuildConstants.VERSION_STRING)); }
    }

    /// <summary>Gets the product serial number, as seen in Rhino's ABOUT dialog box.</summary>
    /// <since>5.0</since>
    public static string SerialNumber
    {
      get
      {
        using (var sh = new StringHolder())
        {
          IntPtr ptr_string = sh.NonConstPointer();
          UnsafeNativeMethods.CRhinoApp_GetString(UnsafeNativeMethods.RhinoAppString.SerialNumber, ptr_string);
          return sh.ToString();
        }
      }
    }

    /// <summary>Gets the name of the user that owns the license or lease.</summary>
    /// <since>6.0</since>
    public static string LicenseUserName
    {
      get
      {
        using (var sh = new StringHolder())
        {
          IntPtr ptr_string = sh.NonConstPointer();
          UnsafeNativeMethods.CRhinoApp_GetString(UnsafeNativeMethods.RhinoAppString.LicenseUserName, ptr_string);
          return sh.ToString();
        }
      }
    }

    /// <summary>Gets the name of the organization of the user that owns the license or lease.</summary>
    /// <since>6.0</since>
    public static string LicenseUserOrganization
    {
      get
      {
        using (var sh = new StringHolder())
        {
          IntPtr ptr_string = sh.NonConstPointer();
          UnsafeNativeMethods.CRhinoApp_GetString(UnsafeNativeMethods.RhinoAppString.LicenseUserOrganization, ptr_string);
          return sh.ToString();
        }
      }
    }

    /// <summary>Gets the type of installation (product edition) of the license or lease.</summary>
    /// <since>6.0</since>
    public static string InstallationTypeString
    {
      get
      {
        using (var sh = new StringHolder())
        {
          IntPtr ptr_string = sh.NonConstPointer();
          UnsafeNativeMethods.CRhinoApp_GetString(UnsafeNativeMethods.RhinoAppString.InstallationTypeString, ptr_string);
          return sh.ToString();
        }
      }
    }

    /// <summary>Gets the application name.</summary>
    /// <since>5.0</since>
    public static string Name
    {
      get
      {
        using (var sh = new StringHolder())
        {
          IntPtr ptr_string = sh.NonConstPointer();
          UnsafeNativeMethods.CRhinoApp_GetString(UnsafeNativeMethods.RhinoAppString.ApplicationName, ptr_string);
          return sh.ToString();
        }
      }
    }

    /// <summary>Gets license the node type.</summary>
    /// <since>5.0</since>
    public static LicenseNode NodeType
    {
      get
      {
        int rc = GetInt(UnsafeNativeMethods.RhinoAppInt.NodeType);
        return (LicenseNode)rc;
      }
    }

    ///<summary>Gets the product installation type, as seen in Rhino's ABOUT dialog box.</summary>
    /// <since>5.0</since>
    public static Installation InstallationType
    {
      get
      {
        int rc = GetInt(UnsafeNativeMethods.RhinoAppInt.Installation);
        return (Installation)rc;
      }
    }

    /// <summary>
    /// Gets the current Registry scheme name.
    /// </summary>
    /// <since>6.0</since>
    public static string SchemeName
    {
      get
      {
        using (var sh = new StringHolder())
        {
          IntPtr ptr_string = sh.NonConstPointer();
          UnsafeNativeMethods.CRhinoApp_GetString(UnsafeNativeMethods.RhinoAppString.RegistrySchemeName, ptr_string);
          return sh.ToString();
        }
      }
    }

    /// <summary>
    /// Returns true if Rhino will validate each object added to the document.
    /// This can be time consuming but is valuable for debugging.
    /// </summary>
    /// <since>8.7</since>
    public static bool CheckNewObject
    {
      get => UnsafeNativeMethods.CRhinoApp_CheckNewObjects(false, true);
      set => UnsafeNativeMethods.CRhinoApp_CheckNewObjects(true, value);
    }

    /// <summary>
    /// Gets the data directory.
    /// </summary>
    /// <returns>The data directory.</returns>
    /// <param name="localUser">If set to <c>true</c> local user.</param>
    /// <param name="forceDirectoryCreation">If set to <c>true</c> force directory creation.</param>
    /// <since>6.0</since>
    public static string GetDataDirectory(bool localUser, bool forceDirectoryCreation)
    {
      return GetDataDirectory (localUser, forceDirectoryCreation, string.Empty);
    }

    /// <summary>
    /// Gets the data directory.
    /// </summary>
    /// <returns>The data directory.</returns>
    /// <param name="localUser">If set to <c>true</c> local user.</param>
    /// <param name="forceDirectoryCreation">If set to <c>true</c> force directory creation.</param>
    /// <param name="subDirectory">
    /// Sub directory, will get appended to the end of the data directory.  if forceDirectoryCreation
    /// is true then this directory will get created and writable.
    /// </param>
    /// <since>6.0</since>
    public static string GetDataDirectory(bool localUser, bool forceDirectoryCreation, string subDirectory)
    {
      if (Runtime.HostUtils.RunningOnOSX)
      {
        using (var string_holder = new StringHolder ())
        {
          IntPtr ptr = string_holder.NonConstPointer();
          if (localUser)
            UnsafeNativeMethods.RhCmn_UserRhinocerosApplicationSupportDirectory(forceDirectoryCreation, subDirectory, ptr);
          else
            UnsafeNativeMethods.RhCmn_LocalRhinocerosApplicationSupportDirectory(forceDirectoryCreation, subDirectory, ptr);
          var value = string_holder.ToString();
          return value;
        }
      }
      var special_folder = System.Environment.GetFolderPath (localUser ? System.Environment.SpecialFolder.ApplicationData : System.Environment.SpecialFolder.CommonApplicationData);
      var version = Version;
      var result = System.IO.Path.Combine(System.IO.Path.Combine(System.IO.Path.Combine(special_folder, "McNeel"), "Rhinoceros"), string.Format("{0}.{1}", version.Major, 0));
      return result;
    }

    /// <summary>
    /// directory
    /// </summary>
    /// <since>6.7</since>
    public static System.IO.DirectoryInfo GetExecutableDirectory()
    {
      using (var sw = new StringWrapper())
      {
        IntPtr ptrString = sw.NonConstPointer;
        bool rc = UnsafeNativeMethods.CRhinoApp_ExecutableFolder(ptrString);
        if (!rc)
          throw new Exception("ExecutableDirectory call failed");
        string directoryName = sw.ToString();
        return new System.IO.DirectoryInfo(directoryName);
      }
    }

    //static property System::String^ Name{ System::String^ get(); }
    //static property System::String^ RegistryKeyName{ System::String^ get(); }


    ///<summary>Gets the ID of Rhino 2.</summary>
    /// <since>5.0</since>
    public static Guid Rhino2Id
    {
      get { return UnsafeNativeMethods.CRhinoApp_GetGUID(UnsafeNativeMethods.RhinoAppGuid.Rhino2Id); }
    }

    ///<summary>Gets the ID of Rhino 3.</summary>
    /// <since>5.0</since>
    public static Guid Rhino3Id
    {
      get { return UnsafeNativeMethods.CRhinoApp_GetGUID(UnsafeNativeMethods.RhinoAppGuid.Rhino3Id); }
    }

    ///<summary>Gets the ID of Rhino 4.</summary>
    /// <since>5.0</since>
    public static Guid Rhino4Id
    {
      get { return UnsafeNativeMethods.CRhinoApp_GetGUID(UnsafeNativeMethods.RhinoAppGuid.Rhino4Id); }
    }

    ///<summary>Gets the ID of Rhino 5</summary>
    /// <since>5.0</since>
    public static Guid Rhino5Id
    {
      get { return UnsafeNativeMethods.CRhinoApp_GetGUID(UnsafeNativeMethods.RhinoAppGuid.Rhino5Id); }
    }

    ///<summary>Gets the ID of Rhino 6</summary>
    /// <since>7.0</since>
    public static Guid Rhino6Id
    {
      get { return UnsafeNativeMethods.CRhinoApp_GetGUID(UnsafeNativeMethods.RhinoAppGuid.Rhino6Id); }
    }

    ///<summary>Gets the ID of Rhino 7</summary>
    /// <since>7.3</since>
    public static Guid Rhino7Id
    {
      get { return UnsafeNativeMethods.CRhinoApp_GetGUID(UnsafeNativeMethods.RhinoAppGuid.Rhino7Id); }
    }

    ///<summary>Gets the current ID of Rhino.</summary>
    /// <since>5.0</since>
    public static Guid CurrentRhinoId
    {
      get { return UnsafeNativeMethods.CRhinoApp_GetGUID(UnsafeNativeMethods.RhinoAppGuid.CurrentRhinoId); }
    }

    /// <summary>
    /// Returns true if Rhino's parent window is the desktop, false otherwise.
    /// </summary>
    /// <remarks>
    /// The parent of a top-level window is the desktop window.
    /// Some applications like to re-parent Rhino's main window to a window they provide.
    /// Use this property to determine if Rhino is in such a condition. 
    /// Note, this property is only valid on Windows.
    /// </remarks>
    /// <since>8.14</since>
    public static bool IsParentDesktop
    {
      get { return UnsafeNativeMethods.CRhinoApp_IsParentDesktop(); }
    }

    /// <summary>Is Rhino currently being executed through automation</summary>
    /// <since>5.0</since>
    public static bool IsRunningAutomated
    {
      get { return UnsafeNativeMethods.CRhinoApp_IsAutomated(); }
    }

    /// <summary>Is Rhino currently being executed in headless mode</summary>
    /// <since>6.1</since>
    public static bool IsRunningHeadless
    {
      get { return UnsafeNativeMethods.CRhinoApp_IsHeadless(); }
    }

    /// <summary>Is Rhino being executed in safe mode</summary>
    /// <since>7.9</since>
    public static bool IsSafeModeEnabled
    {
      get { return UnsafeNativeMethods.CRhinoApp_IsSafeModeEnabled(); }
    }

    /// <summary>
    /// Is Rhino currently using custom, user-interface Skin.
    /// </summary>
    /// <since>6.2</since>
    public static bool IsSkinned
    {
      get { return UnsafeNativeMethods.CRhinoApp_IsSkinned(); }
    }

    //static bool IsRhinoId( System::Guid id );
    static readonly object g_lock_object = new object();
    ///<summary>Print formatted text in the command window.</summary>
    /// <since>5.0</since>
    public static void Write(string message)
    {
      lock (g_lock_object)
      {
        // don't allow '%' characters to be misinterpreted as format codes
        message = message.Replace("%", "%%");
        UnsafeNativeMethods.CRhinoApp_Print(message);
      }
    }

    /// <summary>
    /// Provides a text writer that writes to the command line.
    /// </summary>
    public class CommandLineTextWriter : System.IO.TextWriter
    {
      /// <summary>
      /// Returns Encoding Unicode.
      /// </summary>
      /// <since>6.0</since>
      public override Encoding Encoding
      {
        get
        {
          return Encoding.Unicode;
        }
      }

      /// <summary>
      /// Writes a string to the command line.
      /// </summary>
      /// <since>6.0</since>
      public override void Write(string value)
      {
        RhinoApp.Write(value);
      }

      /// <summary>
      /// Writes a char to the command line.
      /// </summary>
      /// <since>6.0</since>
      public override void Write(char value)
      {
        RhinoApp.Write(char.ToString(value));
      }

      /// <summary>
      /// Writes a char buffer to the command line.
      /// </summary>
      /// <since>6.0</since>
      public override void Write(char[] buffer, int index, int count)
      {
        RhinoApp.Write(new string(buffer, index, count));
      }

      /// <summary>
      /// Provided to give a simple way to IronPython to call this class.
      /// </summary>
      /// <param name="str">The text.</param>
      /// <since>6.0</since>
      [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
      [CLSCompliant(false)]
      public void write(string str)
      {
        Write(str);
      }
    }
    static CommandLineTextWriter g_writer = new CommandLineTextWriter();
    /// <summary>
    /// Provides a TextWriter that can write to the command line.
    /// </summary>
    /// <since>6.0</since>
    public static CommandLineTextWriter CommandLineOut
    {
      get
      {
        return g_writer;
      }
    }


    ///<summary>Print formatted text in the command window.</summary>
    /// <since>5.0</since>
    public static void Write(string format, object arg0)
    {
      Write(String.Format(System.Globalization.CultureInfo.InvariantCulture, format, arg0));
    }
    ///<summary>Print formatted text in the command window.</summary>
    /// <since>5.0</since>
    public static void Write(string format, object arg0, object arg1)
    {
      Write(String.Format(System.Globalization.CultureInfo.InvariantCulture, format, arg0, arg1));
    }
    ///<summary>Print formatted text in the command window.</summary>
    /// <since>5.0</since>
    public static void Write(string format, object arg0, object arg1, object arg2)
    {
      Write(String.Format(System.Globalization.CultureInfo.InvariantCulture, format, arg0, arg1, arg2));
    }

    ///<summary>Print a newline in the command window.</summary>
    /// <since>5.0</since>
    public static void WriteLine()
    {
      Write("\n");
    }
    ///<summary>Print text in the command window.</summary>
    /// <example>
    /// <code source='examples\vbnet\ex_addlayer.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_addlayer.cs' lang='cs'/>
    /// <code source='examples\py\ex_addlayer.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    public static void WriteLine(string message)
    {
      Write(message + "\n");
    }
    ///<summary>Print formatted text with a newline in the command window.</summary>
    /// <example>
    /// <code source='examples\vbnet\ex_addlayer.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_addlayer.cs' lang='cs'/>
    /// <code source='examples\py\ex_addlayer.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    public static void WriteLine(string format, object arg0)
    {
      Write(format + "\n", arg0);
    }
    ///<summary>Print formatted text with a newline in the command window.</summary>
    /// <since>5.0</since>
    public static void WriteLine(string format, object arg0, object arg1)
    {
      Write(format + "\n", arg0, arg1);
    }
    ///<summary>Print formatted text with a newline in the command window.</summary>
    /// <since>5.0</since>
    public static void WriteLine(string format, object arg0, object arg1, object arg2)
    {
      Write(format + "\n", arg0, arg1, arg2);
    }

    /// <summary>
    /// Enable or disable capturing of the strings sent to the CommandWindow through
    /// Write and WriteLine calls
    /// </summary>
    /// <since>7.0</since>
    public static bool CommandWindowCaptureEnabled
    {
      get
      {
        return UnsafeNativeMethods.CRhinoApp_CaptureCommandWindowPrintEnabled();
      }
      set
      {
        UnsafeNativeMethods.CRhinoApp_CaptureCommandWindowPrint(value);
      }
    }

    /// <summary>
    /// Enable or disable sending command window strings to the console
    /// RhinoApp.Write(...) calls would be sent to the console when this
    /// is enabled
    /// </summary>
    /// <since>7.5</since>
    public static bool SendWriteToConsole
    {
      get
      {
        return UnsafeNativeMethods.CRhinoApp_SendCommandWindowPrintToConsoleEnabled();
      }
      set
      {
        UnsafeNativeMethods.CRhinoApp_SendCommandWindowPrintToConsole(value);
      }
    }

    /// <summary>
    /// Get list of strings sent to the command window through calls to Write or WriteLine
    /// when capturing has been enabled
    /// </summary>
    /// <param name="clearBuffer">Clear the captured buffer after this call</param>
    /// <returns>array of captured strings</returns>
    /// <since>7.0</since>
    public static string[] CapturedCommandWindowStrings(bool clearBuffer)
    {
      using(var strings = new ClassArrayString())
      {
        IntPtr pStrings = strings.NonConstPointer();
        UnsafeNativeMethods.CRhinoApp_CommandWindowCapturedStrings(pStrings, clearBuffer);
        return strings.ToArray();
      }
    }

    /// <summary>
    /// Gets the nested command count.
    /// </summary>
    /// <since>8.0</since>
    public static int InCommand
    {
      get
      {
        return UnsafeNativeMethods.CRhinoApp_InCommand();
      }
    }

    /// <summary>
    /// Print a string to the Visual Studio Output window, if a debugger is attached.
    ///
    /// Note that the developer needs to add a newline manually if the next output should
    /// come on a separate line.
    /// </summary>
    /// <param name="str">The string to print to the Output window.</param>
    /// <since>6.0</since>
    public static void OutputDebugString(string str)
    {
      UnsafeNativeMethods.RHC_OutputDebugString(str);
    }

    /// <summary>
    /// Set the text that appears in the Rhino command prompt.
    /// In general, you should use the SetCommandPrompt functions. 
    /// In rare cases, like worker thread messages, the message that 
    /// appears in the prompt has non-standard formatting. In these 
    /// rare cases, SetCommandPromptMessage can be used to literally 
    /// specify the text that appears in the command prompt window.
    /// </summary>
    /// <param name="prompt">A literal text for the command prompt window.</param>
    /// <since>6.0</since>
    public static void SetCommandPromptMessage(string prompt)
    {
      UnsafeNativeMethods.CRhinoApp_SetCommandPromptMessage(prompt);
    }

    ///<summary>Sets the command prompt in Rhino.</summary>
    ///<param name="prompt">The new prompt text.</param>
    ///<param name="promptDefault">
    /// Text that appears in angle brackets and indicates what will happen if the user pressed ENTER.
    ///</param>
    /// <since>5.0</since>
    public static void SetCommandPrompt(string prompt, string promptDefault)
    {
      UnsafeNativeMethods.CRhinoApp_SetCommandPrompt(prompt, promptDefault);
    }
    ///<summary>Set Rhino command prompt.</summary>
    ///<param name="prompt">The new prompt text.</param>
    /// <since>5.0</since>
    public static void SetCommandPrompt(string prompt)
    {
      UnsafeNativeMethods.CRhinoApp_SetCommandPrompt(prompt, null);
    }

    ///<summary>Rhino command prompt.</summary>
    /// <since>5.0</since>
    public static string CommandPrompt
    {
      get
      {
        using (var sh = new StringHolder())
        {
          IntPtr ptr_string = sh.NonConstPointer();
          UnsafeNativeMethods.CRhinoApp_GetString(UnsafeNativeMethods.RhinoAppString.CommandPrompt, ptr_string);
          return sh.ToString();
        }
      }
      set
      {
        UnsafeNativeMethods.CRhinoApp_SetCommandPrompt(value, null);
      }
    }

    /// <summary>
    /// Text in Rhino's command history window.
    /// </summary>
    /// <since>5.0</since>
    public static string CommandHistoryWindowText
    {
      get
      {
        using (var holder = new StringHolder())
        {
          UnsafeNativeMethods.CRhinoApp_GetCommandHistoryWindowText(holder.NonConstPointer());
          string rc = holder.ToString();
          if (string.IsNullOrEmpty(rc))
            return string.Empty;
          rc = rc.Replace('\r', '\n');
          return rc;
        }
      }
    }
    /// <summary>
    /// Clear the text in Rhino's command history window.
    /// </summary>
    /// <since>5.0</since>
    public static void ClearCommandHistoryWindow()
    {
      UnsafeNativeMethods.CRhinoApp_ClearCommandHistoryWindowText();
    }

    ///<summary>Sends a string of printable characters, including spaces, to Rhino&apos;s command line.</summary>
    ///<param name='characters'>[in] A string to characters to send to the command line. This can be null.</param>
    ///<param name='appendReturn'>[in] Append a return character to the end of the string.</param>
    /// <since>5.0</since>
    public static void SendKeystrokes(string characters, bool appendReturn)
    {
      UnsafeNativeMethods.CRhinoApp_SendKeystrokes(characters, appendReturn);
    }

    /// <summary>
    /// Sets the focus to the main window. This function attempts to use the
    /// ActiveDoc on Mac to figure out which window to set focus to.
    /// </summary>
    /// <since>5.0</since>
    public static void SetFocusToMainWindow()
    {
      SetFocusToMainWindow(RhinoDoc.ActiveDoc);
    }

    /// <summary>
    /// Sets the focus to the main windows for a given document
    /// </summary>
    /// <param name="doc">
    /// the document to use for determining a "main window"
    /// </param>
    /// <since>6.16</since>
    public static void SetFocusToMainWindow(RhinoDoc doc)
    {
      uint docSerialNumber = doc == null ? 0 : doc.RuntimeSerialNumber;
      UnsafeNativeMethods.CRhinoApp_SetFocusToMainWindow(docSerialNumber);
    }

    ///<summary>Releases the mouse capture.</summary>
    /// <since>5.0</since>
    public static bool ReleaseMouseCapture()
    {
      return UnsafeNativeMethods.CRhinoApp_ReleaseCapture();
    }

    //[DllImport(Import.lib)]
    //static extern IntPtr CRhinoApp_DefaultRenderer([MarshalAs(UnmanagedType.LPWStr)] string str);
    /////<summary>Rhino's current, or default, render plug-in.</summary>
    //public static string DefaultRenderer
    //{
    //  get
    //  {
    //    IntPtr rc = CRhinoApp_DefaultRenderer(null);
    //    if (IntPtr.Zero == rc)
    //      return null;
    //    return Marshal.PtrToStringUni(rc);
    //  }
    //  set
    //  {
    //    CRhinoApp_DefaultRenderer(value);
    //  }
    //}

    ///<summary>Exits, or closes, Rhino.</summary>
    /// <since>5.0</since>
    public static void Exit()
    {
      Exit(true);
    }

    /// <summary>
    /// Exits, or forcefully closes Rhino.
    /// A prompt to allow saving will appear if necessary when forcefully exiting
    /// Works on Windows and MacOS
    /// <param name ='allowCancel'> true to allow the user to cancel exiting false to force exit</param>
    /// </summary>
    public static void Exit(bool allowCancel)
    {
      UnsafeNativeMethods.CRhinoApp_Exit(allowCancel);
    }

    internal static bool InEventWatcher { get; set; }

    ///<summary>Runs a Rhino command script.</summary>
    ///<param name="documentSerialNumber">[in] Document serial number for the document to run the script for.</param>
    ///<param name="script">[in] script to run.</param>
    ///<param name="echo"> [in]
    /// Controls how the script is echoed in the command output window.
    /// false = silent - nothing is echoed.
    /// true = verbatim - the script is echoed literally.
    ///</param>
    ///<remarks>
    /// Rhino acts as if each character in the script string had been typed in the command prompt.
    /// When RunScript is called from a &quot;script runner&quot; command, it completely runs the
    /// script before returning. When RunScript is called outside of a command, it returns and the
    /// script is run. This way menus and buttons can use RunScript to execute complicated functions.
    ///</remarks>
    ///<exception cref="System.ApplicationException">
    /// If RunScript is being called while inside an event watcher.
    ///</exception>
    /// <since>7.12</since>
    [CLSCompliant(false)]
    public static bool RunScript(uint documentSerialNumber, string script, bool echo)
    {
      if (InEventWatcher)
      {
        const string msg = "Do not call RunScript inside of an event watcher.  Contact steve@mcneel.com to discuss why you need to do this.";
        throw new ApplicationException(msg);
      }
      return UnsafeNativeMethods.CRhinoApp_RunMenuScript_WithDocumentContext(documentSerialNumber, script, "", echo);
    }

    ///<summary>Runs a Rhino command script.</summary>
    ///<param name="documentSerialNumber">[in] Document serial number for the document to run the script for.</param>
    ///<param name="script">[in] script to run.</param>
    ///<param name="mruDisplayString">[in] String to display in the most recent command list.</param>
    ///<param name="echo"> [in]
    /// Controls how the script is echoed in the command output window.
    /// false = silent - nothing is echoed.
    /// true = verbatim - the script is echoed literally.
    ///</param>
    ///<remarks>
    /// Rhino acts as if each character in the script string had been typed in the command prompt.
    /// When RunScript is called from a &quot;script runner&quot; command, it completely runs the
    /// script before returning. When RunScript is called outside of a command, it returns and the
    /// script is run. This way menus and buttons can use RunScript to execute complicated functions.
    ///</remarks>
    ///<exception cref="System.ApplicationException">
    /// If RunScript is being called while inside an event watcher.
    ///</exception>
    /// <since>7.12</since>
    [CLSCompliant(false)]
    public static bool RunScript(uint documentSerialNumber, string script, string mruDisplayString, bool echo)
    {
      if (InEventWatcher)
      {
        const string msg = "Do not call RunScript inside of an event watcher.  Contact steve@mcneel.com to discuss why you need to do this.";
        throw new ApplicationException(msg);
      }
      return UnsafeNativeMethods.CRhinoApp_RunMenuScript_WithDocumentContext(documentSerialNumber, script, mruDisplayString, echo);
    }
    ///<summary>Runs a Rhino command script.</summary>
    ///<param name="script">[in] script to run.</param>
    ///<param name="echo">
    /// Controls how the script is echoed in the command output window.
    /// false = silent - nothing is echoed.
    /// true = verbatim - the script is echoed literally.
    ///</param>
    ///<remarks>
    /// Rhino acts as if each character in the script string had been typed in the command prompt.
    /// When RunScript is called from a &quot;script runner&quot; command, it completely runs the
    /// script before returning. When RunScript is called outside of a command, it returns and the
    /// script is run. This way menus and buttons can use RunScript to execute complicated functions.
    ///</remarks>
    ///<exception cref="System.ApplicationException">
    /// If RunScript is being called while inside an event watcher.
    ///</exception>
    /// <since>5.0</since>
    public static bool RunScript(string script, bool echo)
    {
      if (InEventWatcher)
      {
        const string msg = "Do not call RunScript inside of an event watcher.  Contact steve@mcneel.com to discuss why you need to do this.";
        throw new ApplicationException(msg);
      }
      int echo_mode = echo ? 1 : 0;
      return UnsafeNativeMethods.CRhinoApp_RunScript1(script, echo_mode);
    }

    /// <summary>
    /// Execute a Rhino command.
    /// </summary>
    /// <param name="document">Document to execute the command for</param>
    /// <param name="commandName">Name of command to run.  Use command's localized name or preface with an underscore.</param>
    /// <returns>Returns the result of the command.</returns>
    /// <since>6.0</since>
    public static Commands.Result ExecuteCommand(RhinoDoc document, string commandName)
    {
      return (Commands.Result)UnsafeNativeMethods.CRhinoApp_ExecuteCommand(document.RuntimeSerialNumber, commandName);
    }

    ///<summary>Runs a Rhino command script.</summary>
    ///<param name="script">[in] script to run.</param>
    ///<param name="mruDisplayString">[in] String to display in the most recent command list.</param>
    ///<param name="echo">
    /// Controls how the script is echoed in the command output window.
    /// false = silent - nothing is echoed.
    /// true = verbatim - the script is echoed literally.
    ///</param>
    ///<remarks>
    /// Rhino acts as if each character in the script string had been typed in the command prompt.
    /// When RunScript is called from a &quot;script runner&quot; command, it completely runs the
    /// script before returning. When RunScript is called outside of a command, it returns and the
    /// script is run. This way menus and buttons can use RunScript to execute complicated functions.
    ///</remarks>
    ///<exception cref="System.ApplicationException">
    /// If RunScript is being called while inside an event watcher.
    ///</exception>
    /// <since>5.0</since>
    public static bool RunScript(string script, string mruDisplayString, bool echo)
    {
      if (InEventWatcher)
      {
        const string msg = "Do not call RunScript inside of an event watcher.  Contact steve@mcneel.com to discuss why you need to do this.";
        throw new ApplicationException(msg);
      }
      int echo_mode = echo ? 1 : 0;
      return UnsafeNativeMethods.CRhinoApp_RunScript2(script, mruDisplayString, echo_mode);
    }

    /// <summary>
    ///   Run a Rhino menu item script.  Will add the selected menu string to the MRU command menu.
    /// </summary>
    ///<param name="script">[in] script to run.</param>
    ///<remarks>
    /// Rhino acts as if each character in the script string had been typed in the command prompt.
    /// When RunScript is called from a &quot;script runner&quot; command, it completely runs the
    /// script before returning. When RunScript is called outside of a command, it returns and the
    /// script is run. This way menus and buttons can use RunScript to execute complicated functions.
    ///</remarks>
    ///<exception cref="System.ApplicationException">
    /// If RunScript is being called while inside an event watcher.
    ///</exception>
    /// <returns></returns>
    /// <since>6.0</since>
    public static bool RunMenuScript(string script)
    {
      if (InEventWatcher)
      {
        const string msg = "Do not call RunMenuScript inside of an event watcher.  Contact steve@mcneel.com to discuss why you need to do this.";
        throw new ApplicationException(msg);
      }
      return UnsafeNativeMethods.CRhinoApp_RunMenuScript(script);
    }

    /// <summary>
    /// Pauses to keep Windows message pump alive so views will update
    /// and windows will repaint.
    /// </summary>
    /// <since>5.0</since>
    public static void Wait()
    {
      UnsafeNativeMethods.CRhinoApp_Wait(0);
    }


    static readonly InvokeHelper g_invoke_helper = new InvokeHelper();

    /// <summary>
    /// Execute a function on the main UI thread.
    /// </summary>
    /// <param name="method">function to execute</param>
    /// <param name="args">parameters to pass to the function</param>
    /// <since>6.0</since>
    public static void InvokeOnUiThread(Delegate method, params object[] args)
    {
      // 10 Jan 2020 S. Baer (RH-48366)
      // We get quite a few crashes in our persistent settings save routine which
      // ends up calling this function. Adding a test for InvokeRequired so we can
      // just directly call the method when on a UI thread. This may not fix the
      // crash, but it should simplify the callstack to hopefully give us a better
      // idea of what is going on.
      if( InvokeRequired )
      {
        g_invoke_helper.Invoke(method, args);
      }
      else
      {
        try
        {
          method.DynamicInvoke(args);
        }
        catch(Exception ex)
        {
          Rhino.Runtime.HostUtils.ExceptionReport(ex);
        }
      }
    }

    static bool m_done;
    static Exception m_ex;
    /// <summary>
    /// Work-In-Progress. Testing this with our unit test framework
    /// </summary>
    /// <param name="action"></param>
    /// <since>6.0</since>
    public static void InvokeAndWait(Action action)
    {
      // lame implementation, just a start
      m_done = false;
      m_ex = null;
      InvokeOnUiThread(new Action<Action>(ActionWrapper), action);
      while (!m_done)
        System.Threading.Thread.Sleep(100);
      m_done = false;
      if (m_ex != null)
        throw m_ex;
    }

    static void ActionWrapper(Action action)
    {
      try
      {
        action();
      }
      catch(Exception ex)
      {
        m_ex = ex;
      }
      m_done = true;
    }

    /// <summary>
    /// Returns true if we are currently not running on the main user interface thread
    /// </summary>
    /// <since>6.0</since>
    public static bool InvokeRequired
    {
      get
      {
        return g_invoke_helper.InvokeRequired;
      }
    }


    /// <summary>
    /// Gets the HWND of the Rhino main window.
    /// </summary>
    /// <since>5.0</since>
    public static IntPtr MainWindowHandle()
    {
      IntPtr hMainWnd = UnsafeNativeMethods.CRhinoApp_GetMainFrameHWND();
      if (IntPtr.Zero == hMainWnd && Rhino.Runtime.HostUtils.RunningOnWindows)
        hMainWnd = System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;
      return hMainWnd;
    }

    static RhinoWindow g_main_window;

    /// <summary> Main Rhino Window </summary>
    /// <since>5.0</since>
    /// <deprecated>6.0</deprecated>
    [System.ComponentModel.Browsable(false), System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    [Obsolete("Use MainWindowHandle or RhinoEtoApp.MainWindow in Rhino.UI")]
    public static System.Windows.Forms.IWin32Window MainWindow()
    {
      if (null == g_main_window)
      {
        IntPtr handle = MainWindowHandle();
        if (IntPtr.Zero != handle)
          g_main_window = new RhinoWindow(handle);
      }
      return g_main_window;
    }

    /// <summary>
    /// Same as MainWindow function, but provides the concrete class instead of an interface
    /// </summary>
    /// <since>5.0</since>
    [System.ComponentModel.Browsable(false), System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    [Obsolete("Use MainWindowHandle or RhinoEtoApp.MainWindow in Rhino.UI")]
    public static RhinoWindow MainApplicationWindow
    {
      get { return MainWindow() as RhinoWindow; }
    }

    /// <summary>
    /// Gets the object that is returned by PlugIn.GetPlugInObject for a given
    /// plug-in. This function attempts to find and load a plug-in with a given Id.
    /// When a plug-in is found, it's GetPlugInObject function is called and the
    /// result is returned here.
    /// Note the plug-in must have already been installed in Rhino or the plug-in manager
    /// will not know where to look for a plug-in with a matching id.
    /// </summary>
    /// <param name="pluginId">Guid for a given plug-in.</param>
    /// <returns>
    /// Result of PlugIn.GetPlugInObject for a given plug-in on success.
    /// </returns>
    /// <since>5.0</since>
    public static object GetPlugInObject(Guid pluginId)
    {
      if (pluginId == Guid.Empty)
        return null;

      // see if the plug-in is already loaded before doing any heavy lifting
      PlugIns.PlugIn p = PlugIns.PlugIn.GetLoadedPlugIn(pluginId);
      if (p != null)
        return p.GetPlugInObject();


      // load plug-in
      UnsafeNativeMethods.CRhinoPlugInManager_LoadPlugIn(pluginId, true, false);
      p = PlugIns.PlugIn.GetLoadedPlugIn(pluginId);
      if (p != null)
        return p.GetPlugInObject();

      IntPtr iunknown = UnsafeNativeMethods.CRhinoApp_GetPlugInObject(pluginId);
      if (IntPtr.Zero == iunknown)
        return null;

      object rc;
      try
      {
        rc = System.Runtime.InteropServices.Marshal.GetObjectForIUnknown(iunknown);
      }
      catch (Exception)
      {
        rc = null;
      }
      return rc;
    }

    /// <summary>
    /// Gets the object that is returned by PlugIn.GetPlugInObject for a given
    /// plug-in. This function attempts to find and load a plug-in with a given name.
    /// When a plug-in is found, it's GetPlugInObject function is called and the
    /// result is returned here.
    /// Note the plug-in must have already been installed in Rhino or the plug-in manager
    /// will not know where to look for a plug-in with a matching name.
    /// </summary>
    /// <param name="plugin">Name of a plug-in.</param>
    /// <returns>
    /// Result of PlugIn.GetPlugInObject for a given plug-in on success.
    /// </returns>
    /// <since>5.0</since>
    public static object GetPlugInObject(string plugin)
    {
      Guid plugin_id;
      if (!Guid.TryParse(plugin, out plugin_id))
        plugin_id = Guid.Empty;

      if (plugin_id == Guid.Empty)
        plugin_id = UnsafeNativeMethods.CRhinoPlugInManager_GetPlugInId(plugin);

      return GetPlugInObject(plugin_id);
    }

    /// <summary>
    /// If licenseType is an evaluation license, returns true. An evaluation license limits the ability of
    /// Rhino to save based on either the number of saves or a fixed period of time.
    /// </summary>
    /// <seealso cref="Installation"/>
    /// <param name="licenseType"></param>
    /// <returns>true if licenseType is an evaluation license. false otherwise</returns>
    /// <since>5.6</since>
    public static bool IsInstallationEvaluation(Installation licenseType)
    {
      return (licenseType == Installation.Evaluation ||
              licenseType == Installation.EvaluationTimed);
    }

    /// <summary>
    /// If licenseType is a commercial license, returns true. A commercial license grants
    /// full use of the product.
    /// </summary>
    /// <param name="licenseType"></param>
    /// <seealso cref="Installation"/>
    /// <returns>true if licenseType is a commercial license. false otherwise</returns>
    /// <since>5.6</since>
    public static bool IsInstallationCommercial(Installation licenseType)
    {
      return (licenseType == Installation.Commercial     ||
              licenseType == Installation.Corporate      ||
              licenseType == Installation.Educational    ||
              licenseType == Installation.EducationalLab ||
              licenseType == Installation.NotForResale   ||
              licenseType == Installation.NotForResaleLab);
    }

    /// <summary>
    /// If licenseType is a beta license, returns true. A beta license grants
    /// full use of the product during the pre-release development period.
    /// </summary>
    /// <param name="licenseType"></param>
    /// <seealso cref="Installation"/>
    /// <returns>true if licenseType is a beta license. false otherwise</returns>
    /// <since>5.6</since>
    public static bool IsInstallationBeta(Installation licenseType)
    {
      return (licenseType == Installation.Beta || licenseType == Installation.BetaLab);
    }

    /// <summary>
    /// Returns 
    ///   true if the license will expire
    ///   false otherwise
    /// </summary>
    /// <since>5.6</since>
    public static bool LicenseExpires
    {
      get { return GetBool(UnsafeNativeMethods.RhinoAppBool.LicenseExpires); }
    }

    /// <summary>
    /// Returns
    ///   true if Rhino is compiled a s pre-release build (Beta, WIP)
    ///   false otherwise
    /// </summary>
    /// <since>6.0</since>
    public static bool IsPreRelease
    {
      get { return GetBool(UnsafeNativeMethods.RhinoAppBool.IsPreRelease); }
    }

    /// <summary>
    /// Returns 
    ///   true if the license is validated
    ///   false otherwise
    /// </summary>
    /// <since>5.6</since>
    public static bool IsLicenseValidated
    {
      get { return GetBool(UnsafeNativeMethods.RhinoAppBool.IsLicenseValidated); }
    }

    /// <summary>
    /// Returns 
    ///   true if rhino is currently using the Cloud Zoo
    ///   false otherwise
    /// </summary>
    /// <since>6.0</since>
    public static bool IsCloudZooNode
    {
      get { return GetBool(UnsafeNativeMethods.RhinoAppBool.IsCloudZooNode); }
    }

    /// <summary>
    /// Returns true when Rhino is allowed to access the Internet, false otherwise.
    /// Note, this does not test if Internet access is available.
    /// </summary>
    /// <since>6.15</since>
    public static bool IsInternetAccessAllowed
    {
      get {
        try
        {
          return GetBool(UnsafeNativeMethods.RhinoAppBool.IsInternetAccessAllowed);
        }
        catch 
        {
          // This Catch used to be specific; it turns out there are several ways for 
          // RhinoCommon to fail to load, and we don't need to take everything down
          // because of it.
          return true;
        }
      }
    }

    /// <summary>
    /// Is the current thread the main thread
    /// </summary>
    /// <since>8.10</since>
    public static bool IsOnMainThread
    {
      get { return GetBool(UnsafeNativeMethods.RhinoAppBool.IsOnMainThread); }
    }

    /// <summary>
    /// Returns true when Rhino is allowed to save, false otherwise
    /// Conditions where Rhino is not allowed to save are: when evaluation licenses are expired;
    /// when a Cloud Zoo lease is expired; when a license is shared by a single user on multiple
    /// computers, and the current computer is not active.
    /// </summary>
    /// <since>7.0</since>
    public static bool CanSave
    {
      get { return GetBool(UnsafeNativeMethods.RhinoAppBool.RhinoCanSave); }
    }

    /// <summary>
    /// Returns true when Rhino is allowed to access the Internet, false otherwise.
    /// Note, this does not test if Internet access is available.
    /// </summary>
    /// <since>6.15</since>
    public static int UpdatesAndStatisticsStatus
    {
      get { return GetInt(UnsafeNativeMethods.RhinoAppInt.UpdatesAndStatisticsStatus); }
    }

    /// <summary>
    /// Returns number of days within which validation must occur. Zero when
    ///   validation grace period has expired.
    /// Raises InvalidLicenseTypeException if LicenseType is one of:
    ///   EvaluationSaveLimited
    ///   EvaluationTimeLimited
    ///   Viewer
    ///   Unknown
    /// </summary>
    /// <since>5.6</since>
    public static int ValidationGracePeriodDaysLeft
    {
      get { return GetInt(UnsafeNativeMethods.RhinoAppInt.ValidationGracePeriodDaysLeft); }
    }

    /// <summary>
    /// Returns number of days until license expires. Zero when
    ///   license is expired.
    /// Raises InvalidLicenseTypeException if LicenseExpires
    /// would return false.
    /// </summary>
    /// <since>5.6</since>
    public static int DaysUntilExpiration
    {
      get { return GetInt(UnsafeNativeMethods.RhinoAppInt.DaysUntilExpiration); }
    }

    /// <summary>
    /// Display UI asking the user to enter a license for Rhino or use one from the Zoo.
    /// </summary>
    /// <param name="standAlone">True to ask for a stand-alone license, false to ask the user for a license from the Zoo</param>
    /// <param name="parentWindow">Parent window for the user interface dialog.</param>
    /// <returns></returns>
    /// <since>6.0</since>
    public static bool AskUserForRhinoLicense(bool standAlone, object parentWindow)
    {
      var handle_parent = UI.Dialogs.Service.ObjectToWindowHandle(parentWindow, true);
      return UnsafeNativeMethods.CRhinoApp_AskUserForRhinoLicense(standAlone, handle_parent);
    }


    /// <summary>
    /// Display UI asking the user to enter a license for the product specified by licenseId.
    /// </summary>
    /// <param name="pluginId">Guid identifying the plug-in that is requesting a change of license key</param>
    /// <returns>true on success, false otherwise</returns>
    /// <since>6.0</since>
    public static bool ChangeLicenseKey(Guid pluginId)
    {
      return UnsafeNativeMethods.CRhinoApp_ChangeLicenseKey(pluginId);
    }

    /// <summary>
    /// Refresh the license used by Rhino. This allows any part of Rhino to ensure that the most current version of the license file on disk is in use.
    /// </summary>
    /// <returns></returns>
    /// <since>6.0</since>
    public static bool RefreshRhinoLicense()
    {
      return UnsafeNativeMethods.CRhinoApp_RefreshRhinoLicense();
    }

    /// <summary>
    /// Logs in to the cloud zoo.
    /// </summary>
    /// <since>6.0</since>
    public static bool LoginToCloudZoo()
    {
      return Rhino.PlugIns.LicenseUtils.ZooClient.LoginToCloudZoo();
    }

    /// <summary>
    /// Returns the name of the logged in user, or null if the user is not logged in.
    /// </summary>
    /// <since>6.0</since>
    public static string LoggedInUserName
    {
      get
      {
        return Rhino.PlugIns.LicenseUtils.ZooClient.LoggedInUserName;
      }
    }

    /// <summary>
    /// Call this method to determine if the current user is logged in using a
    /// @mcneel.com email. Use this to turn on features for internal McNeel 
    /// users for testing.
    /// </summary>
    internal static bool IsLoggedInAsMcNeelUser
    {
      get
      {
        if (_loggedInAsMcNeelUser == null)
        {
          var name = LoggedInUserName ?? string.Empty;
          _loggedInAsMcNeelUser = name.ToLower().Contains("@mcneel.com");
        }
        return _loggedInAsMcNeelUser.Value;
      }
    }
    private static bool? _loggedInAsMcNeelUser;

    /// <summary>
    /// Call this method to see if logged in with McNeel email address and if
    /// the Rhino.Options.Advanced.EnableMcNeelOnlyFeatures flag is set
    /// </summary>
    internal static bool AreMcNeelOnlyFeaturesEnabled
    {
      get
      {
        if (_areMcNeelOnlyFeaturesEnabled == null)
          _areMcNeelOnlyFeaturesEnabled = PersistentSettings.RhinoAppSettings.AddChild("Options").AddChild("Advanced").GetBool("EnableMcNeelOnlyFeatures", IsLoggedInAsMcNeelUser);
        return _areMcNeelOnlyFeaturesEnabled.Value;
      }
    }
    private static bool? _areMcNeelOnlyFeaturesEnabled;

    /// <summary>
    /// Returns the logged in user's avatar picture. 
    /// Returns a default avatar if the user does not have an avatar or if the avatar could not be fetched.
    /// </summary>
    /// <since>6.0</since>
    public static System.Drawing.Image LoggedInUserAvatar
    {
      get
      {
        return Rhino.PlugIns.LicenseUtils.ZooClient.LoggedInUserAvatar;
      }
    }

    /// <summary>
    /// Returns true if the user is logged in; else returns false.
    /// A logged in user does not guarantee that the auth tokens managed by the CloudZooManager instance are valid.
    /// </summary>
    /// <since>6.0</since>
    public static bool UserIsLoggedIn
    {
      get
      {
        return Rhino.PlugIns.LicenseUtils.ZooClient.UserIsLoggedIn;
      }
    }

    /// <summary>
    /// Returns true if Rhino is in the process of closing, false otherwise.
    /// This can be true even before the Closing event fires, such as when RhinoDoc.CloseDocument event is called.
    /// </summary>
    /// <since>6.26</since>
    public static bool IsClosing
    {
      get { return GetBool(UnsafeNativeMethods.RhinoAppBool.IsClosing); }
    }

    /// <summary>
    /// Returns true if Rhino is in the process of exiting, false otherwise.
    /// This can be true even before the Closing event fires, such as when RhinoDoc.CloseDocument event is called.
    /// </summary>
    /// <since>6.26</since>
    public static bool IsExiting
    {
      get { return GetBool(UnsafeNativeMethods.RhinoAppBool.IsExiting); }
    }

#region events
    // Callback that doesn't pass any parameters or return values
    internal delegate void RhCmnEmptyCallback();

    private static RhCmnEmptyCallback m_OnEscapeKey;
    private static void OnEscapeKey() => m_escape_key?.SafeInvoke();

    private static EventHandler m_escape_key;

    /// <summary>
    /// Can add or removed delegates that are raised when the escape key is clicked.
    /// </summary>
    /// <since>5.0</since>
    public static event EventHandler EscapeKeyPressed
    {
      add
      {
        if (Runtime.HostUtils.ContainsDelegate(m_escape_key, value))
          return;

        m_escape_key += value;
        m_OnEscapeKey = OnEscapeKey;
        UnsafeNativeMethods.RHC_SetEscapeKeyCallback(m_OnEscapeKey);
      }
      remove
      {
        m_escape_key -= value;
        if (null == m_escape_key)
        {
          UnsafeNativeMethods.RHC_SetEscapeKeyCallback(null);
          m_OnEscapeKey = null;
        }
      }
    }

    // Callback that doesn't pass any parameters or return values
    /// <summary>
    /// KeyboardEvent delegate
    /// </summary>
    /// <param name="key"></param>
    public delegate void KeyboardHookEvent(int key);

    private static KeyboardHookEvent m_OnKeyboardEvent;
    private static void OnKeyboardEvent(int key)
    {
      if (m_keyboard_event != null)
      {
        try
        {
          m_keyboard_event(key);
        }
        catch (Exception ex)
        {
          Runtime.HostUtils.ExceptionReport(ex);
        }
      }
    }

    /// <summary>
    /// 
    /// </summary>
    private static KeyboardHookEvent m_keyboard_event;

    /// <summary>
    /// Can add or removed delegates that are raised by a keyboard event.
    /// </summary>
    /// <since>5.2</since>
    public static event KeyboardHookEvent KeyboardEvent
    {
      add
      {
        if (Runtime.HostUtils.ContainsDelegate(m_escape_key, value))
          return;

        m_keyboard_event += value;
        m_OnKeyboardEvent = OnKeyboardEvent;
        UnsafeNativeMethods.RHC_SetKeyboardCallback(m_OnKeyboardEvent);
      }
      remove
      {
        m_keyboard_event -= value;
        if (null == m_escape_key)
        {
          UnsafeNativeMethods.RHC_SetKeyboardCallback(null);
          m_OnEscapeKey = null;
        }
      }
    }

    private static RhCmnEmptyCallback m_OnInitApp;
    [MonoPInvokeCallback(typeof(RhCmnEmptyCallback))]
    private static void OnInitApp() => m_init_app.SafeInvoke();

    internal static EventHandler m_init_app;

    private static readonly object m_event_lock = new object();

    /// <summary>
    /// Is raised when the application is fully initialized.
    /// </summary>
    /// <since>5.0</since>
    public static event EventHandler Initialized
    {
      add
      {
        lock (m_event_lock)
        {
          if (m_init_app == null)
          {
            m_OnInitApp = OnInitApp;
            UnsafeNativeMethods.CRhinoEventWatcher_SetInitAppCallback(m_OnInitApp, Runtime.HostUtils.m_ew_report);
          }
          m_init_app -= value;
          m_init_app += value;
        }
      }
      remove
      {
        lock (m_event_lock)
        {
          m_init_app -= value;
          if (m_init_app == null)
          {
            UnsafeNativeMethods.CRhinoEventWatcher_SetInitAppCallback(null, Runtime.HostUtils.m_ew_report);
            m_OnInitApp = null;
          }
        }
      }
    }


    private static RhCmnEmptyCallback m_OnCloseApp;
    [MonoPInvokeCallback(typeof(RhCmnEmptyCallback))]
    private static void OnCloseApp() => m_close_app?.SafeInvoke();

    internal static EventHandler m_close_app;

    /// <summary>
    /// Is raised when the application is about to close.
    /// </summary>
    /// <since>5.0</since>
    public static event EventHandler Closing
    {
      add
      {
        lock (m_event_lock)
        {
          if (m_close_app == null)
          {
            m_OnCloseApp = OnCloseApp;
            UnsafeNativeMethods.CRhinoEventWatcher_SetCloseAppCallback(m_OnCloseApp, Runtime.HostUtils.m_ew_report);
          }
          m_close_app -= value;
          m_close_app += value;
        }
      }
      remove
      {
        lock (m_event_lock)
        {
          m_close_app -= value;
          if (m_close_app == null)
          {
            UnsafeNativeMethods.CRhinoEventWatcher_SetCloseAppCallback(null, Runtime.HostUtils.m_ew_report);
            m_OnCloseApp = null;
          }
        }
      }
    }


    private static RhCmnEmptyCallback m_OnAppSettingsChanged;
    [MonoPInvokeCallback(typeof(RhCmnEmptyCallback))]
    private static void OnAppSettingsChanged() => m_appsettings_changed?.SafeInvoke();

    internal static EventHandler m_appsettings_changed;

    /// <summary>
    /// Is raised when settings are changed.
    /// </summary>
    /// <since>5.0</since>
    public static event EventHandler AppSettingsChanged
    {
      add
      {
        lock (m_event_lock)
        {
          if (m_appsettings_changed == null)
          {
            m_OnAppSettingsChanged = OnAppSettingsChanged;
            UnsafeNativeMethods.CRhinoEventWatcher_SetAppSettingsChangeCallback(m_OnAppSettingsChanged, Runtime.HostUtils.m_ew_report);
          }
          m_appsettings_changed -= value;
          m_appsettings_changed += value;
        }
      }
      remove
      {
        lock (m_event_lock)
        {
          m_appsettings_changed -= value;
          if (m_appsettings_changed == null)
          {
            UnsafeNativeMethods.CRhinoEventWatcher_SetAppSettingsChangeCallback(null, Runtime.HostUtils.m_ew_report);
            m_OnAppSettingsChanged = null;
          }
        }
      }
    }

    private static RhCmnEmptyCallback m_OnIdle;
    [MonoPInvokeCallback(typeof(RhCmnEmptyCallback))]
    private static void OnIdle() => m_idle_occured?.SafeInvoke();

    private static EventHandler m_idle_occured;

    internal static void OnSettingsSaved(bool writing, bool dirty)
    {
      if (SettingsSaved == null)
        return;
      var manager = PersistentSettingsManager.Create(CurrentRhinoId);
      var old_settings = PersistentSettingsManager.Create(CurrentRhinoId).InternalPlugInSettings.Duplicate(false);
      var e = new PersistentSettingsSavedEventArgs(writing, old_settings);
      SettingsSaved(manager.InternalPlugInSettings, e);
    }
    internal static event EventHandler<PersistentSettingsSavedEventArgs> SettingsSaved;

    /// <summary>
    /// Occurs when the application finishes processing and is about to enter the idle state
    /// </summary>
    /// <since>5.1</since>
    public static event EventHandler Idle
    {
      add
      {
        lock (m_event_lock)
        {
          if (m_idle_occured == null)
          {
            m_OnIdle = OnIdle;
            UnsafeNativeMethods.CRhinoEventWatcher_SetOnIdleCallback(m_OnIdle);
          }
          m_idle_occured -= value;
          m_idle_occured += value;
        }
      }
      remove
      {
        lock (m_event_lock)
        {
          m_idle_occured -= value;
          if (m_idle_occured == null)
          {
            UnsafeNativeMethods.CRhinoEventWatcher_SetOnIdleCallback(null);
            m_OnIdle = null;
          }
        }
      }
    }

    private static RhCmnEmptyCallback m_OnMainLoop;
    [MonoPInvokeCallback(typeof(RhCmnEmptyCallback))]
    private static void OnMainLoop() => m_main_loop_occured?.SafeInvoke();

    private static EventHandler m_main_loop_occured;

    /// <summary>
    /// Gets called every loop iteration inside Rhino's main message loop.
    /// </summary>
    /// <since>7.0</since>
    public static event EventHandler MainLoop
    {
      add
      {
        lock (m_event_lock)
        {
          if (m_main_loop_occured == null)
          {
            m_OnMainLoop = OnMainLoop;
            UnsafeNativeMethods.CRhinoEventWatcher_SetOnMainLoopCallback(m_OnMainLoop);
          }
          m_main_loop_occured -= value;
          m_main_loop_occured += value;
        }
      }
      remove
      {
        lock (m_event_lock)
        {
          m_main_loop_occured -= value;
          if (m_main_loop_occured == null)
          {
            UnsafeNativeMethods.CRhinoEventWatcher_SetOnMainLoopCallback(null);
            m_OnMainLoop = null;
          }
        }
      }
    }

    /// <summary>
    /// Fires when the license state has changed
    /// </summary>
    /// <since>7.0</since>
    public static event EventHandler<Rhino.Runtime.LicenseStateChangedEventArgs> LicenseStateChanged;

    internal static void FireLicenseStateChangedEvent(object sender, Rhino.Runtime.NamedParametersEventArgs args)
    {
      bool canSave = false;
      args.TryGetBool("CanSave", out canSave);
      var licenseStateChangedArgs = new Runtime.LicenseStateChangedEventArgs(canSave);

      LicenseStateChanged?.Invoke(null, licenseStateChangedArgs);
    }

#endregion

#region RDK events

    internal delegate void RhCmnOneUintCallback(uint docSerialNumber);
    private static RhCmnOneUintCallback m_OnNewRdkDocument;
    [MonoPInvokeCallback(typeof(RhCmnOneUintCallback))]
    private static void OnNewRdkDocument(uint docSerialNumber)
    {
      m_new_rdk_document?.SafeInvoke(RhinoDoc.FromRuntimeSerialNumber(docSerialNumber));
    }
    internal static EventHandler m_new_rdk_document;

    /// <summary>
    /// Monitors when RDK document information is rebuilt.
    /// </summary>
    /// <since>5.1</since>
    public static event EventHandler RdkNewDocument
    {
      add
      {
        if (m_new_rdk_document == null)
        {
          m_OnNewRdkDocument = OnNewRdkDocument;
          UnsafeNativeMethods.CRdkCmnEventWatcher_SetNewRdkDocumentEventCallback(m_OnNewRdkDocument, Rhino.Runtime.HostUtils.m_rdk_ew_report);
        }
        m_new_rdk_document += value;
      }
      remove
      {
        m_new_rdk_document -= value;
        if (m_new_rdk_document == null)
        {
          UnsafeNativeMethods.CRdkCmnEventWatcher_SetNewRdkDocumentEventCallback(null, Rhino.Runtime.HostUtils.m_rdk_ew_report);
          m_OnNewRdkDocument = null;
        }
      }
    }



    private static RhCmnEmptyCallback m_OnRdkGlobalSettingsChanged;
    [MonoPInvokeCallback(typeof(RhCmnEmptyCallback))]
    private static void OnRdkGlobalSettingsChanged() => m_rdk_global_settings_changed?.SafeInvoke();
    internal static EventHandler m_rdk_global_settings_changed;

    /// <summary>
    /// Monitors when RDK global settings are modified.
    /// </summary>
    /// <since>5.1</since>
    public static event EventHandler RdkGlobalSettingsChanged
    {
      add
      {
        if (m_rdk_global_settings_changed == null)
        {
          m_OnRdkGlobalSettingsChanged = OnRdkGlobalSettingsChanged;
          UnsafeNativeMethods.CRdkCmnEventWatcher_SetGlobalSettingsChangedEventCallback(m_OnRdkGlobalSettingsChanged, Rhino.Runtime.HostUtils.m_rdk_ew_report);
        }
        m_rdk_global_settings_changed += value;
      }
      remove
      {
        m_rdk_global_settings_changed -= value;
        if (m_rdk_global_settings_changed == null)
        {
          UnsafeNativeMethods.CRdkCmnEventWatcher_SetGlobalSettingsChangedEventCallback(null, Rhino.Runtime.HostUtils.m_rdk_ew_report);
          m_OnRdkGlobalSettingsChanged = null;
        }
      }
    }
    

    private static RhCmnOneUintCallback m_OnRdkUpdateAllPreviews;
    [MonoPInvokeCallback(typeof(RhCmnOneUintCallback))]
    private static void OnRdkUpdateAllPreviews(uint docSerialNumber)
    {
      m_rdk_update_all_previews?.SafeInvoke(RhinoDoc.FromRuntimeSerialNumber(docSerialNumber));
    }
    internal static EventHandler m_rdk_update_all_previews;

    /// <summary>
    /// Monitors when RDK thumbnails are updated.
    /// </summary>
    /// <since>5.1</since>
    public static event EventHandler RdkUpdateAllPreviews
    {
      add
      {
        if (m_rdk_update_all_previews == null)
        {
          m_OnRdkUpdateAllPreviews = OnRdkUpdateAllPreviews;
          UnsafeNativeMethods.CRdkCmnEventWatcher_SetUpdateAllPreviewsEventCallback(m_OnRdkUpdateAllPreviews, Rhino.Runtime.HostUtils.m_rdk_ew_report);
        }
        m_rdk_update_all_previews += value;
      }
      remove
      {
        m_rdk_update_all_previews -= value;
        if (m_rdk_update_all_previews == null)
        {
          UnsafeNativeMethods.CRdkCmnEventWatcher_SetUpdateAllPreviewsEventCallback(null, Rhino.Runtime.HostUtils.m_rdk_ew_report);
          m_OnRdkUpdateAllPreviews = null;
        }
      }
    }
    

    private static RhCmnEmptyCallback m_OnCacheImageChanged;
    [MonoPInvokeCallback(typeof(RhCmnEmptyCallback))]
    private static void OnRdkCacheImageChanged() => m_rdk_cache_image_changed?.SafeInvoke();
    internal static EventHandler m_rdk_cache_image_changed;

    /// <summary>
    /// Monitors when the RDK thumbnail cache images are changed.
    /// </summary>
    /// <since>5.1</since>
    public static event EventHandler RdkCacheImageChanged
    {
      add
      {
        if (m_rdk_cache_image_changed == null)
        {
          m_OnCacheImageChanged = OnRdkCacheImageChanged;
          UnsafeNativeMethods.CRdkCmnEventWatcher_SetCacheImageChangedEventCallback(m_OnCacheImageChanged, Rhino.Runtime.HostUtils.m_rdk_ew_report);
        }
        m_rdk_cache_image_changed += value;
      }
      remove
      {
        m_rdk_cache_image_changed -= value;
        if (m_rdk_cache_image_changed == null)
        {
          UnsafeNativeMethods.CRdkCmnEventWatcher_SetCacheImageChangedEventCallback(null, Rhino.Runtime.HostUtils.m_rdk_ew_report);
          m_OnCacheImageChanged = null;
        }
      }
    }

    private static RhCmnEmptyCallback m_OnRendererChanged;
    [MonoPInvokeCallback(typeof(RhCmnEmptyCallback))]
    private static void OnRendererChanged() => m_renderer_changed?.SafeInvoke();
    internal static EventHandler m_renderer_changed;

    /// <summary>
    /// Monitors when Rhino's current renderer changes.
    /// </summary>
    /// <since>5.1</since>
    public static event EventHandler RendererChanged
    {
      add
      {
        if (m_renderer_changed == null)
        {
          m_OnRendererChanged = OnRendererChanged;
          UnsafeNativeMethods.CRdkCmnEventWatcher_SetRendererChangedEventCallback(m_OnRendererChanged, Rhino.Runtime.HostUtils.m_rdk_ew_report);
        }
        m_renderer_changed += value;
      }
      remove
      {
        m_renderer_changed -= value;
        if (m_renderer_changed == null)
        {
          UnsafeNativeMethods.CRdkCmnEventWatcher_SetRendererChangedEventCallback(null, Rhino.Runtime.HostUtils.m_rdk_ew_report);
          m_OnRendererChanged = null;
        }
      }
    }



    internal delegate void ClientPlugInUnloadingCallback(Guid plugIn);
    private static ClientPlugInUnloadingCallback m_OnClientPlugInUnloading;
    [MonoPInvokeCallback(typeof(ClientPlugInUnloadingCallback))]
    private static void OnClientPlugInUnloading(Guid plugIn) => m_client_plugin_unloading?.SafeInvoke();
    internal static EventHandler m_client_plugin_unloading;

    /// <summary>
    /// Monitors when RDK client plug-ins are unloaded.
    /// </summary>
    /// <since>5.1</since>
    public static event EventHandler RdkPlugInUnloading
    {
      add
      {
        if (m_client_plugin_unloading == null)
        {
          m_OnClientPlugInUnloading = OnClientPlugInUnloading;
          UnsafeNativeMethods.CRdkCmnEventWatcher_SetClientPlugInUnloadingEventCallback(m_OnClientPlugInUnloading, Rhino.Runtime.HostUtils.m_rdk_ew_report);
        }
        m_renderer_changed += value;
      }
      remove
      {
        m_client_plugin_unloading -= value;
        if (m_client_plugin_unloading == null)
        {
          UnsafeNativeMethods.CRdkCmnEventWatcher_SetClientPlugInUnloadingEventCallback(null, Rhino.Runtime.HostUtils.m_rdk_ew_report);
          m_OnClientPlugInUnloading = null;
        }
      }
    }

#endregion

    static UI.ToolbarFileCollection m_toolbar_files;
    /// <summary>
    /// Collection of currently open toolbar files in the application
    /// </summary>
    /// <since>5.0</since>
    public static UI.ToolbarFileCollection ToolbarFiles
    {
      get { return m_toolbar_files ?? (m_toolbar_files = new Rhino.UI.ToolbarFileCollection()); }
    }

    /// <summary>
    /// Verifies that Rhino is running in full screen mode. 
    /// </summary>
    /// <returns>true if Rhino is running full screen, false otherwise.</returns>
    /// <since>6.0</since>
    public static bool InFullScreen()
    {
      return UnsafeNativeMethods.CRhinoApp_InFullscreen();
    }

    /// <summary>
    /// Default font used to render user interface
    /// </summary>
    /// <since>6.0</since>
    public static Font DefaultUiFont
    {
      get { return null; }
    }

    /// <summary>
    /// Verifies that Rhino is running on VMWare
    /// </summary>
    /// <returns>true if Rhino is running in Windows on VMWare, false otherwise</returns>
    /// <since>6.0</since>
    public static bool RunningOnVMWare()
    {
      return UnsafeNativeMethods.Rh_RunningOnVMWare();
    }

    /// <summary>
    /// Find out if Rhino is running in a remote session
    /// </summary>
    /// <returns>true if Rhino is running in a RDP session, false otherwise</returns>
    /// <since>6.0</since>
    public static bool RunningInRdp()
    {
      return UnsafeNativeMethods.Rh_RunningInRdp();
    }

    /// <summary>
    /// Parses a text field string.
    /// </summary>
    /// <param name="formula">The text formula.</param>
    /// <param name="obj">The Rhino object. Value can be IntPtr.Zero.</param>
    /// <param name="topParentObject">The parent Rhino object. Value can be IntPtr.Zero.</param>
    /// <returns>The parsed text field if sucessful.</returns>
    /// <since>6.0</since>
    public static string ParseTextField(string formula, RhinoObject obj, RhinoObject topParentObject )
    {
      if (topParentObject == null)
        topParentObject = obj;
      using (var sh = new Runtime.InteropWrappers.StringWrapper())
      {
        IntPtr ptr_string = sh.NonConstPointer;
        IntPtr ptr_obj = IntPtr.Zero;
        if (obj != null)
          ptr_obj = obj.ConstPointer();
        IntPtr ptr_parent_obj = IntPtr.Zero;
        if (topParentObject != null)
          ptr_parent_obj = topParentObject.ConstPointer();
        UnsafeNativeMethods.CRhinoApp_RhParseTextField(formula, ptr_string, ptr_obj, ptr_parent_obj);
        return sh.ToString();
      }
    }

    /// <summary>
    /// Parses a text field string.
    /// </summary>
    /// <param name="formula">The text formula.</param>
    /// <param name="obj">The Rhino object. Value can be IntPtr.Zero.</param>
    /// <param name="topParentObject">The parent Rhino object. Value can be IntPtr.Zero.</param>
    /// <param name="immediateParent">The immediate parent instance object. Value can be IntPtr.Zero.</param>
    /// <returns>The parsed text field if sucessful.</returns>
    /// <since>8.10</since>
    public static string ParseTextField(string formula, RhinoObject obj, RhinoObject topParentObject, InstanceObject immediateParent)
    {
      if (topParentObject == null)
        topParentObject = obj;
      using (var sh = new Runtime.InteropWrappers.StringWrapper())
      {
        IntPtr ptr_string = sh.NonConstPointer;
        IntPtr ptr_obj = IntPtr.Zero;
        if (obj != null)
          ptr_obj = obj.ConstPointer();
        IntPtr ptr_parent_obj = IntPtr.Zero;
        if (topParentObject != null)
          ptr_parent_obj = topParentObject.ConstPointer();
        IntPtr ptr_immediate_parent_obj = IntPtr.Zero;
        if (immediateParent != null)
          ptr_immediate_parent_obj = immediateParent.ConstPointer();
        UnsafeNativeMethods.CRhinoApp_RhParseTextField2(formula, ptr_string, ptr_obj, ptr_parent_obj, ptr_immediate_parent_obj);
        return sh.ToString();
      }
    }

    /// <summary>
    /// Post an enter event to the command line
    /// </summary>
    /// <param name="runtimeDocSerialNumber">Unique serialNumber for the document to post the event to.</param>
    /// <param name="bRepeatedEnter">if true, allow multiple enter events to be posted simultaneouslyt.</param>
    /// <since>8.0</since>
    [CLSCompliant(false)]
    public static void PostEnterEvent(uint runtimeDocSerialNumber, bool bRepeatedEnter)
    {
      UnsafeNativeMethods.CRhinoApp_PostEnterEvent(runtimeDocSerialNumber, bRepeatedEnter);
    }

    /// <summary>
    /// Post a cancel event to the command line
    /// </summary>
    /// <param name="runtimeDocSerialNumber">Unique serialNumber for the document to post the event to.</param>
    /// <since>8.0</since>
    [CLSCompliant(false)]
    public static void PostCancelEvent(uint runtimeDocSerialNumber)
    {
      UnsafeNativeMethods.CRhinoApp_PostCancelEvent(runtimeDocSerialNumber);
    }
  }
}

namespace Rhino.UI
{
  /// <summary>
  /// Contains static methods to control the mouse icon.
  /// </summary>
  public static class MouseCursor
  {
    /// <summary>
    /// Sets a cursor tooltip string shown next to the mouse cursor.
    /// Overrides all cursor tooltip panes.
    /// </summary>
    /// <param name="tooltip">The text to show.</param>
    /// <since>5.0</since>
    public static void SetToolTip(string tooltip)
    {
      UnsafeNativeMethods.CRhinoApp_SetCursorTooltip(tooltip);
    }

    /// <summary>
    /// Retrieves the position of the mouse cursor, in screen coordinates
    /// </summary>
    /// <since>5.8</since>
    public static Point2d Location
    {
      get
      {
        UnsafeNativeMethods.Point pt;
        UnsafeNativeMethods.GetCursorPos(out pt);
        return new Point2d(pt.X, pt.Y);
      }
    }
  }

  /// <summary>
  /// Contains static methods to control the application status bar.
  /// </summary>
  public static class StatusBar
  {
    /// <summary>
    /// Sets the distance pane to a distance value.
    /// </summary>
    /// <param name="distance">The distance value.</param>
    /// <since>5.0</since>
    public static void SetDistancePane(double distance)
    {
      UnsafeNativeMethods.CRhinoApp_SetStatusBarDistancePane(distance);
    }

    /// <summary>
    /// Sets the number pane to a number value
    /// </summary>
    /// <param name="number"></param>
    /// <since>6.0</since>
    public static void SetNumberPane(double number)
    {
      UnsafeNativeMethods.CRhinoApp_SetStatusBarNumberPane(number);
    }

    /// <summary>
    /// Sets the point pane to a point value.
    /// </summary>
    /// <param name="point">The point value.</param>
    /// <since>5.0</since>
    public static void SetPointPane(Point3d point)
    {
      UnsafeNativeMethods.CRhinoApp_SetStatusBarPointPane(point);
    }

    /// <summary>
    /// Sets the message pane to a message.
    /// </summary>
    /// <param name="message">The message value.</param>
    /// <since>5.0</since>
    public static void SetMessagePane(string message)
    {
      UnsafeNativeMethods.CRhinoApp_SetStatusBarMessagePane(message);
    }

    /// <summary>
    /// Removes the message from the message pane.
    /// </summary>
    /// <since>5.0</since>
    public static void ClearMessagePane()
    {
      SetMessagePane(null);
    }

    /// <summary>
    /// Starts, or shows, Rhino's status bar progress meter.
    /// </summary>
    /// <param name="lowerLimit">The lower limit of the progress meter's range.</param>
    /// <param name="upperLimit">The upper limit of the progress meter's range.</param>
    /// <param name="label">The short description of the progress (e.g. "Calculating", "Meshing", etc)</param>
    /// <param name="embedLabel">
    /// If true, then the label will be embedded in the progress meter.
    /// If false, then the label will appear to the left of the progress meter.
    /// </param>
    /// <param name="showPercentComplete">
    /// If true, then the percent complete will appear in the progress meter.
    /// </param>
    /// <returns>
    /// 1 - The progress meter was created successfully.
    /// 0 - The progress meter was not created.
    /// -1 - The progress meter was not created because some other process has already created it.
    /// </returns>
    /// <since>5.0</since>
    public static int ShowProgressMeter(int lowerLimit, int upperLimit, string label, bool embedLabel, bool showPercentComplete)
    {
      return ShowProgressMeter(0, lowerLimit, upperLimit, label, embedLabel, showPercentComplete);
    }

    /// <summary>
    /// Starts, or shows, Rhino's status bar progress meter.
    /// </summary>
    /// <param name="docSerialNumber">The document runtime serial number.</param>
    /// <param name="lowerLimit">The lower limit of the progress meter's range.</param>
    /// <param name="upperLimit">The upper limit of the progress meter's range.</param>
    /// <param name="label">The short description of the progress (e.g. "Calculating", "Meshing", etc)</param>
    /// <param name="embedLabel">
    /// If true, then the label will be embedded in the progress meter.
    /// If false, then the label will appear to the left of the progress meter.
    /// </param>
    /// <param name="showPercentComplete">
    /// If true, then the percent complete will appear in the progress meter.
    /// </param>
    /// <returns>
    /// 1 - The progress meter was created successfully.
    /// 0 - The progress meter was not created.
    /// -1 - The progress meter was not created because some other process has already created it.
    /// </returns>
    /// <since>6.12</since>
    [CLSCompliant(false)]
    public static int ShowProgressMeter(uint docSerialNumber, int lowerLimit, int upperLimit, string label, bool embedLabel, bool showPercentComplete)
    {
      return UnsafeNativeMethods.CRhinoApp_StatusBarProgressMeterStart(docSerialNumber, lowerLimit, upperLimit, label, embedLabel, showPercentComplete);
    }

    /// <summary>
    /// Sets the current position of Rhino's status bar progress meter.
    /// </summary>
    /// <param name="position">The new value. This can be stated in absolute terms, or relative compared to the current position.
    /// <para>The interval bounds are specified when you first show the bar.</para></param>
    /// <param name="absolute">
    /// If true, then the progress meter is moved to position.
    /// If false, then the progress meter is moved position from the current position (relative).
    /// </param>
    /// <returns>
    /// The previous position if successful.
    /// </returns>
    /// <since>5.0</since>
    public static int UpdateProgressMeter(int position, bool absolute)
    {
      return UpdateProgressMeter(0, position, absolute);
    }

    /// <summary>
    /// Sets the current position of Rhino's status bar progress meter.
    /// </summary>
    /// <param name="docSerialNumber">The document runtime serial number.</param>
    /// <param name="position">The new value. This can be stated in absolute terms, or relative compared to the current position.
    /// <para>The interval bounds are specified when you first show the bar.</para></param>
    /// <param name="absolute">
    /// If true, then the progress meter is moved to position.
    /// If false, then the progress meter is moved position from the current position (relative).
    /// </param>
    /// <returns>
    /// The previous position if successful.
    /// </returns>
    /// <since>6.12</since>
    [CLSCompliant(false)]
    public static int UpdateProgressMeter(uint docSerialNumber, int position, bool absolute)
    {
      return UnsafeNativeMethods.CRhinoApp_StatusBarProgressMeterPos(docSerialNumber, position, absolute);
    }

    /// <summary>
    /// Sets the label and current position of Rhino's status bar progress meter.
    /// </summary>
    /// <param name="label">
    /// The short description of the progress (e.g. "Calculating", "Meshing", etc)
    /// </param>
    /// <param name="position">
    /// The new value. This can be stated in absolute terms, or relative compared to the current position.
    /// The interval bounds are specified when you first show the bar. 
    /// Note, if value is <seealso cref="RhinoMath.UnsetIntIndex"/>, only the label is updated.
    /// </param>
    /// <param name="absolute">
    /// If true, then the progress meter is moved to position.
    /// If false, then the progress meter is moved position from the current position (relative).
    /// </param>
    /// <returns>
    /// The previous position if successful.
    /// </returns>
    /// <since>8.6</since>
    public static int UpdateProgressMeter(string label, int position, bool absolute)
    {
      return UpdateProgressMeter(0, label, position, absolute);
    }

    /// <summary>
    /// Sets the label and current position of Rhino's status bar progress meter.
    /// </summary>
    /// <param name="docSerialNumber">The document runtime serial number.</param>
    /// <param name="label">The short description of the progress (e.g. "Calculating", "Meshing", etc)</param>
    /// <param name="position">
    /// The new value. This can be stated in absolute terms, or relative compared to the current position.
    /// The interval bounds are specified when you first show the bar. 
    /// Note, if value is <seealso cref="RhinoMath.UnsetIntIndex"/>, only the label is updated.
    /// </param>
    /// <param name="absolute">
    /// If true, then the progress meter is moved to position.
    /// If false, then the progress meter is moved position from the current position (relative).
    /// </param>
    /// <returns>
    /// The previous position if successful.
    /// </returns>
    /// <since>8.6</since>
    [CLSCompliant(false)]
    public static int UpdateProgressMeter(uint docSerialNumber, string label, int position, bool absolute)
    {
      return UnsafeNativeMethods.CRhinoApp_StatusBarProgressMeterPos2(docSerialNumber, label, position, absolute);
    }

    /// <summary>
    /// Ends, or hides, Rhino's status bar progress meter.
    /// </summary>
    /// <since>5.0</since>
    public static void HideProgressMeter()
    {
      HideProgressMeter(0);
    }

    /// <summary>
    /// Ends, or hides, Rhino's status bar progress meter.
    /// </summary>
    /// <param name="docSerialNumber">The document runtime serial number.</param>
    /// <since>6.12</since>
    [CLSCompliant(false)]
    public static void HideProgressMeter(uint docSerialNumber)
    {
      UnsafeNativeMethods.CRhinoApp_StatusBarProgressMeterEnd(docSerialNumber);
    }

  }
}
#endif
