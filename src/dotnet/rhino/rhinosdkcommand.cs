#pragma warning disable 1591

using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using Rhino.Runtime.InteropWrappers;
using System.Diagnostics.Eventing.Reader;

#if RHINO_SDK
namespace Rhino.Commands
{
  /// <summary>
  /// Defines bitwise mask flags for different styles of commands, such as
  /// <see cref="Style.Hidden">Hidden</see> or <see cref="Style.DoNotRepeat">DoNotRepeat</see>.
  /// </summary>
  /// <since>5.0</since>
  [Flags]
  public enum Style
  {
    /// <summary>
    /// No flag is defined.
    /// </summary>
    None = 0,
    /// <summary>
    /// Also known as a "test" command. The command name does not auto-complete
    /// when typed on the command line an is therefore not discoverable. Useful
    /// for writing commands that users don't normally have access to.
    /// </summary>
    Hidden = 1,
    /// <summary>
    /// For commands that want to run scripts as if they were typed at the command
    /// line (like RhinoScript's RunScript command)
    /// </summary>
    ScriptRunner = 2,
    /// <summary>
    /// Transparent commands can be run inside of other commands.
    /// The command does not modify the contents of the model's geometry in any way.
    /// Examples of transparent commands include commands that change views and toggle
    /// snap states.  Any command that adds or deletes, a view cannot be transparent.
    /// </summary>
    Transparent = 4,
    /// <summary>
    /// The command should not be repeated by pressing "ENTER" immediately after
    /// the command finishes.
    /// </summary>
    DoNotRepeat = 8,
    /// <summary>
    /// By default, all commands are undo-able.
    /// </summary>
    NotUndoable = 16
  }

  /// <summary>
  /// Provides enumerated constants for a command running mode. This is currently interactive or scripted.
  /// </summary>
  /// <since>5.0</since>
  public enum RunMode
  {
    /// <summary>
    /// Can use dialogs for input. Must use message boxes to
    /// report serious error conditions.
    /// </summary>
    Interactive = 0,
    /// <summary>
    /// All input must come from command line, GetPoint, GetObject,
    /// GetString, etc.  Must use message boxes to report serious
    /// error conditions.  Script mode gets used when a command is
    /// run with a hyphen (-) prefix.
    /// </summary>
    Scripted = 1
  }

  /// <summary>
  /// Decorates <see cref="Command">commands</see> to provide styles.
  /// </summary>
  [AttributeUsage(AttributeTargets.Class)]
  public sealed class CommandStyleAttribute : Attribute
  {
    private readonly Style m_style;
    /// <summary>
    /// Initializes a new command style attribute class.
    /// </summary>
    /// <param name="styles">
    /// Set of values combined using a bitwise OR operation to get the desired combination
    /// of command styles.
    /// </param>
    /// <since>5.0</since>
    public CommandStyleAttribute(Style styles)
    {
      m_style = styles;
    }

    /// <summary>
    /// Gets the associated style.
    /// </summary>
    /// <since>5.0</since>
    public Style Styles
    {
      get { return m_style; }
    }
  }

  /// <summary>
  /// Defines enumerated constant values for several command result types.
  /// </summary>
  /// <since>5.0</since>
  public enum Result
  {
    /// <summary>Command worked.</summary>
    Success = 0,
    /// <summary>User canceled command.</summary>
    Cancel  = 1, 
    /// <summary>Command did nothing but cancel was not pressed.</summary>
    Nothing = 2,
    /// <summary>Command failed (bad input, computational problem, etc.)</summary>
    Failure,
    /// <summary>Command not found (user probably had a typo in command name).</summary>
    UnknownCommand,
    /// <summary>Commands canceled and modeless dialog.</summary>
    CancelModelessDialog,
    /// <summary>exit RhinoCommon.</summary>
    ExitRhino = 0x0FFFFFFF
  }

  /// <summary>
  /// Stores the macro and display string of the most recent command.
  /// </summary>
  public class MostRecentCommandDescription
  {
    /// <since>5.0</since>
    public string DisplayString { get; set; }
    /// <since>5.0</since>
    public string Macro { get; set; }
  }

  /// <summary>
  /// Defines a base class for all commands. This class is abstract.
  /// </summary>
  public abstract class Command
  {
    /// <summary>
    /// Determines if a string is a valid command name.
    /// </summary>
    /// <param name="name">A string.</param>
    /// <returns>true if the string is a valid command name.</returns>
    /// <since>5.0</since>
    public static bool IsValidCommandName(string name)
    {
      return UnsafeNativeMethods.CRhinoCommand_IsValidCommandName(name);
    }

    /// <summary>
    /// Gets the ID of the last commands.
    /// </summary>
    /// <since>5.0</since>
    public static Guid LastCommandId
    {
      get
      {
        return UnsafeNativeMethods.CRhinoEventWatcher_LastCommandId();
      }
    }

    /// <summary>
    /// Gets the result code of the last command.
    /// </summary>
    /// <since>5.0</since>
    public static Result LastCommandResult
    {
      get
      {
        int rc = UnsafeNativeMethods.CRhinoEventWatcher_LastCommandResult();
        return (Result)rc;
      }
    }

    /// <summary>
    /// Gets an array of most recent command descriptions.
    /// </summary>
    /// <returns>An array of command descriptions.</returns>
    /// <since>5.0</since>
    public static MostRecentCommandDescription[] GetMostRecentCommands()
    {
      using(var display_strings = new ClassArrayString())
      using(var macros = new ClassArrayString())
      {
        IntPtr ptr_display_strings = display_strings.NonConstPointer();
        IntPtr ptr_macros = macros.NonConstPointer();
        int count = UnsafeNativeMethods.CRhinoApp_GetMRUCommands(ptr_display_strings, ptr_macros);
        var d = display_strings.ToArray();
        var m = macros.ToArray();

        MostRecentCommandDescription[] rc = new MostRecentCommandDescription[count];
        for (int i = 0; i < count; i++)
        {
          var mru = new MostRecentCommandDescription {DisplayString = d[i], Macro = m[i]};
          rc[i] = mru;
        }
        return rc;
      }
    }

    /// <summary>
    /// Execute some code as if it were running in a command
    /// </summary>
    /// <param name="doc"></param>
    /// <param name="commandCallback"></param>
    /// <param name="data">optional extra data to pass to callback</param>
    /// <since>6.13</since>
    public static void RunProxyCommand(RunCommandDelegate commandCallback, RhinoDoc doc, object data)
    {
      const string commandName = "RhinoCommonProxyCommand";
      Rhino.Runtime.HostUtils.RegisterNamedCallback(commandName, ProxyCommandCallback);
      _proxyCommandCallback = commandCallback;
      _proxyCommandDoc = doc;
      _proxyObject = data;
      RhinoApp.RunScript(commandName, false);
      _proxyCommandCallback = null;
      _proxyCommandDoc = null;
      _proxyObject = null;
    }

    public delegate Result RunCommandDelegate(RhinoDoc doc, RunMode mode, object data);
    static RunCommandDelegate _proxyCommandCallback;
    static RhinoDoc _proxyCommandDoc;
    static object _proxyObject;
    static void ProxyCommandCallback(object sender, Rhino.Runtime.NamedParametersEventArgs args)
    {
      if (null == _proxyCommandCallback)
        return;
      RhinoDoc doc = _proxyCommandDoc;
      if (doc == null)
        doc = RhinoDoc.ActiveDoc;
      var rc = _proxyCommandCallback(doc, RunMode.Interactive, _proxyObject);
      args.Set("result", (int)rc);
    }


    static readonly Collections.RhinoList<Command> m_all_commands = new Rhino.Collections.RhinoList<Command>();

    internal int m_runtime_serial_number;
    internal Style m_style_flags;
    Rhino.PlugIns.PlugIn m_plugin; 
    Guid m_id = Guid.Empty;

    internal static Command LookUpBySerialNumber(int sn)
    {
      for (int i = 0; i < m_all_commands.Count; i++)
      {
        Command cmd = m_all_commands[i];
        if (cmd.m_runtime_serial_number == sn)
          return cmd;
      }
      return null;
    }

    /// <summary>
    /// Default protected constructor. It only allows instantiation through sub-classing.
    /// </summary>
    protected Command()
    {
      m_all_commands.Add(this);

      // set RunCommand and callback if it hasn't already been set
      if (null == m_RunCommand)
      {
        m_RunCommand = OnRunCommand;
        m_DoHelp = OnDoHelp;
        m_ContextHelp = OnCommandContextHelpUrl;
        m_ReplayHistory = OnReplayHistory;
        m_SelFilter = SelCommand.OnSelFilter;
        m_SubObjectSelFilter = SelCommand.OnSelSubObjectFilter;
        UnsafeNativeMethods.CRhinoCommand_SetCallbacks(0, m_RunCommand, m_DoHelp, m_ContextHelp, m_ReplayHistory, m_SelFilter, m_SubObjectSelFilter);
        EndCommand += Rhino.Runtime.HostUtils.DeleteObjectsOnMainThread;
      }
    }

    #region properties
    /// <summary>
    /// Gets the plug-in where this commands is placed.
    /// </summary>
    /// <since>5.0</since>
    public PlugIns.PlugIn PlugIn
    {
      get
      {
        if (null == m_plugin)
          return Rhino.PlugIns.PlugIn.m_active_plugin_at_command_creation;
        return m_plugin;
      }
      internal set
      {
        m_plugin = value;
      }
    }

    /// <summary>
    /// Gets the  unique ID of this command. It is best to use a Guid
    /// attribute for each custom derived command class since this will
    /// keep the id consistent between sessions of Rhino
    /// <see cref="System.Runtime.InteropServices.GuidAttribute">GuidAttribute</see>
    /// </summary>
    /// <since>5.0</since>
    public virtual Guid Id
    {
      get
      {
        if( Guid.Empty == m_id )
        {
          m_id = GetType().GUID;
          if( Guid.Empty== m_id )
            m_id = Guid.NewGuid();
        }
        return m_id;
      }
    }

    /// <summary>
    /// Gets the name of the command.
    /// This method is abstract.
    /// </summary>
    /// <since>5.0</since>
    public abstract string EnglishName{ get; }

    /// <summary>
    /// Gets the local name of the command.
    /// </summary>
    /// <since>5.0</since>
    public virtual string LocalName
    {
      get { return Rhino.UI.Localization.LocalizeCommandName(EnglishName, this); }
    }

    /// <summary>
    /// Gets the settings of the command.
    /// </summary>
    /// <since>5.0</since>
    public PersistentSettings Settings
    {
      get { return PlugIn.CommandSettings( EnglishName ); }
    }

#endregion

    /// <summary>
    /// Executes the command.
    /// </summary>
    /// <param name="doc">The current document.</param>
    /// <param name="mode">The command running mode.</param>
    /// <returns>The command result code.</returns>
    protected abstract Result RunCommand(RhinoDoc doc, RunMode mode);
    internal int OnRunCommand(int commandSerialNumber, uint docSerialNumber, int mode)
    {
      Result rc = Result.Failure;
      try
      {
        Command cmd = LookUpBySerialNumber(commandSerialNumber);
        RhinoDoc doc = RhinoDoc.FromRuntimeSerialNumber(docSerialNumber);
        RunMode rm = RunMode.Interactive;
        if (mode > 0)
          rm = RunMode.Scripted;
        if (cmd == null || doc == null)
          return (int)Result.Failure;

        rc = cmd.RunCommand(doc, rm);
      }
      catch (Exception ex)
      {
        Runtime.HostUtils.DebugString("Exception caught during RunCommand");
        Runtime.HostUtils.ExceptionReport(ex);
      }
      return (int)rc;
    }

    /// <summary>
    /// Is called when the user needs assistance with this command.
    /// </summary>
    protected virtual void OnHelp()
    {
      // 22 Feb 2019 S. Baer (RH-51106)
      // Help should open a web page for all built-in commands
      var info = Rhino.PlugIns.PlugIn.GetPlugInInfo(this.PlugIn.Id);
      if (info != null && info.ShipsWithRhino)
        Rhino.UI.RhinoHelp.Show(null);
    }

    /// <summary>
    /// Gets the URL of the command contextual help. This is usually a location of a local CHM file.
    /// <para>The default implementation return an empty string.</para>
    /// </summary>
    protected virtual string CommandContextHelpUrl{ get { return string.Empty; } }
    static void OnDoHelp(int command_serial_number)
    {
      try
      {
        Command cmd = LookUpBySerialNumber(command_serial_number);
        if (cmd != null)
          cmd.OnHelp();
      }
      catch (Exception ex)
      {
        Rhino.Runtime.HostUtils.ExceptionReport(ex);
      }
    }
    static int OnCommandContextHelpUrl(int command_serial_number, IntPtr pON_wString)
    {
      int rc = 0;
      try
      {
        Command cmd = LookUpBySerialNumber(command_serial_number);
        if (cmd != null && IntPtr.Zero != pON_wString)
        {
          string url = cmd.CommandContextHelpUrl;
          if (!string.IsNullOrEmpty(url))
          {
            rc = 1;
            UnsafeNativeMethods.ON_wString_Set(pON_wString, url);
          }
        }
      }
      catch (Exception ex)
      {
        Rhino.Runtime.HostUtils.ExceptionReport(ex);
        rc = 0;
      }
      return rc;
    }
    
    /// <summary>
    /// Determines if Rhino is currently running a command. Because Rhino allow for transparent commands
    /// (commands that can be run from inside of other commands), this method returns the total ids of
    /// active commands.
    /// </summary>
    /// <returns>
    /// Ids of running commands or null if no commands are currently running. 
    /// The "active" command is at the end of this list.
    /// </returns>
    /// <since>5.0</since>
    public static Guid[] GetCommandStack()
    {
      System.Collections.Generic.List<Guid> ids = new System.Collections.Generic.List<Guid>();
      int i = 0;
      while (true)
      {
        Guid id = UnsafeNativeMethods.CRhinoApp_GetRunningCommandId(i);
        if (id == Guid.Empty)
          break;
        ids.Add(id);
        i++;
      }
      return ids.Count < 1 ? null : ids.ToArray();
    }

    /// <summary>
    /// Determines if Rhino is currently running a command.
    /// </summary>
    /// <returns>true if a command is currently running, false if no commands are currently running.</returns>
    /// <since>5.0</since>
    public static bool InCommand()
    {
      return GetCommandStack() != null;
    }

    /// <summary>
    /// This is a low level tool to determine if Rhino is currently running
    /// a script running command like "ReadCommandFile" or the RhinoScript
    /// plug-in's "RunScript".
    /// </summary>
    /// <returns>true if a script running command is active.</returns>
    /// <since>5.0</since>
    public static bool InScriptRunnerCommand()
    {
      int rc = RhinoApp.GetInt(UnsafeNativeMethods.RhinoAppInt.InScriptRunner);
      return (1 == rc);
    }

    /// <summary>
    /// Determines is a string is a command.
    /// </summary>
    /// <param name="name">A string.</param>
    /// <returns>true if the string is a command.</returns>
    /// <since>5.0</since>
    public static bool IsCommand(string name)
    {
      return UnsafeNativeMethods.RhCommand_IsCommand(name);
    }

    /// <summary>
    /// Returns the ID of a command.
    /// </summary>
    /// <param name="name">The name of the command.</param>
    /// <param name="searchForEnglishName">true if the name is to searched in English. This ensures that a '_' is prepended to the name.</param>
    /// <returns>An of the command, or <see cref="Guid.Empty"/> on error.</returns>
    /// <since>5.0</since>
    public static Guid LookupCommandId(string name, bool searchForEnglishName)
    {
      if( searchForEnglishName && !name.StartsWith("_", StringComparison.Ordinal))
        name = "_" + name;
      
      Guid rc = UnsafeNativeMethods.CRhinoApp_LookupCommandByName(name);
      return rc;
    }

    /// <summary>
    /// Returns the command name given a command ID.
    /// </summary>
    /// <param name="commandId">A command ID.</param>
    /// <param name="englishName">true if the requested command is in English.</param>
    /// <returns>The command name, or null on error.</returns>
    /// <since>5.0</since>
    public static string LookupCommandName(Guid commandId, bool englishName)
    {
      IntPtr pName = UnsafeNativeMethods.CRhinoApp_LookupCommandById(commandId, englishName);
      if (IntPtr.Zero == pName)
        return null;
      return Marshal.PtrToStringUni(pName);
    }

    /// <summary>
    /// Gets list of command names in Rhino. This list does not include Test, Alpha, or System commands.
    /// </summary>
    /// <param name="english">
    ///  if true, retrieve the English name for every command.
    ///  if false, retrieve the local name for every command.
    /// </param>
    /// <param name="loaded">
    /// if true, only get names of currently loaded commands.
    /// if false, get names of all registered (may not be currently loaded) commands.
    /// </param>
    /// <returns>An array instance with command names. This array could be empty, but not null.</returns>
    /// <since>5.0</since>
    public static string[] GetCommandNames(bool english, bool loaded)
    {
      using (var strings = new ClassArrayString())
      {
        IntPtr ptr_strings = strings.NonConstPointer();
        UnsafeNativeMethods.CRhinoCommandManager_GetCommandNames(ptr_strings, english, loaded);
        return strings.ToArray();
      }
    }

    /// <summary>
    /// Displays help for a command.
    /// </summary>
    /// <param name="commandId">A command ID.</param>
    /// <since>5.0</since>
    public static void DisplayHelp(Guid commandId)
    {
      UnsafeNativeMethods.CRhinoApp_DisplayCommandHelp(commandId);
    }

    #region events
    internal delegate int RunCommandCallback(int commandSerialNumber, uint docSerialNumber, int mode);
    private static RunCommandCallback m_RunCommand;
    internal delegate void DoHelpCallback(int command_serial_number);
    private static DoHelpCallback m_DoHelp;
    internal delegate int ContextHelpCallback(int command_serial_number, IntPtr pON_wString);
    private static ContextHelpCallback m_ContextHelp;
    internal delegate int ReplayHistoryCallback(int command_serial_number, IntPtr pConstRhinoHistoryRecord, IntPtr pObjectPairArray);
    private static ReplayHistoryCallback m_ReplayHistory;
    private static SelCommand.SelFilterCallback m_SelFilter;
    private static SelCommand.SelSubObjectCallback m_SubObjectSelFilter;

    internal delegate void CommandCallback(IntPtr pCommand, int rc, uint docRuntimeSerialNumber);
    private static CommandCallback m_OnBeginCommand;
    private static CommandCallback m_OnEndCommand;
    private static void OnBeginCommand(IntPtr pCommand, int rc, uint docRuntimeSerialNumber)
    {
      if (m_begin_command != null)
      {
        try
        {
          CommandEventArgs e = new CommandEventArgs(pCommand, rc, docRuntimeSerialNumber);
          m_begin_command(null, e);
          e.m_pCommand = IntPtr.Zero;
        }
        catch (Exception ex)
        {
          Runtime.HostUtils.ExceptionReport(ex);
        }
      }
    }
    private static void OnEndCommand(IntPtr pCommand, int rc, uint docRuntimeSerialNumber)
    {
      if (m_end_command != null)
      {
        try
        {
          CommandEventArgs e = new CommandEventArgs(pCommand, rc, docRuntimeSerialNumber);
          m_end_command(null, e);
          e.m_pCommand = IntPtr.Zero;
        }
        catch (Exception ex)
        {
          Runtime.HostUtils.ExceptionReport(ex);
        }
      }
    }

    internal static EventHandler<CommandEventArgs> m_begin_command;

    /// <summary>
    /// Called just before command.RunCommand().
    /// </summary>
    /// <since>5.0</since>
    public static event EventHandler<CommandEventArgs> BeginCommand
    {
      add
      {
        if (m_begin_command == null)
        {
          m_OnBeginCommand = OnBeginCommand;
          UnsafeNativeMethods.CRhinoEventWatcher_SetBeginCommandCallback(m_OnBeginCommand, Rhino.Runtime.HostUtils.m_ew_report);
        }
        m_begin_command -= value;
        m_begin_command += value;
      }
      remove
      {
        m_begin_command -= value;
        if (m_begin_command == null)
        {
          UnsafeNativeMethods.CRhinoEventWatcher_SetBeginCommandCallback(null, Rhino.Runtime.HostUtils.m_ew_report);
          m_OnBeginCommand = null;
        }
      }
    }

    internal static EventHandler<CommandEventArgs> m_end_command;

    /// <summary>
    /// Called immediately after command.RunCommand().
    /// </summary>
    /// <since>5.0</since>
    public static event EventHandler<CommandEventArgs> EndCommand
    {
      add
      {
        if (m_end_command == null)
        {
          m_OnEndCommand = OnEndCommand;
          UnsafeNativeMethods.CRhinoEventWatcher_SetEndCommandCallback(m_OnEndCommand, Rhino.Runtime.HostUtils.m_ew_report);
        }
        m_end_command -= value;
        m_end_command += value;
      }
      remove
      {
        m_end_command -= value;
        if (m_end_command == null)
        {
          UnsafeNativeMethods.CRhinoEventWatcher_SetEndCommandCallback(null, Rhino.Runtime.HostUtils.m_ew_report);
          m_OnEndCommand = null;
        }
      }
    }


    internal delegate void UndoCallback(int undo_event, uint undo_record_sn, Guid command_id);
    private static UndoCallback m_OnUndoEvent;
    private static void OnUndoEvent(int undo_event, uint undo_record_sn, Guid command_id)
    {
      if (m_undo_event != null)
      {
        try
        {
          m_undo_event(null, new UndoRedoEventArgs(undo_event, undo_record_sn, command_id));
        }
        catch (Exception ex)
        {
          Runtime.HostUtils.ExceptionReport(ex);
        }
      }
    }
    internal static EventHandler<UndoRedoEventArgs> m_undo_event;
    /// <summary>
    /// Used to monitor Rhino's built in undo/redo support.
    /// </summary>
    /// <since>5.0</since>
    public static event EventHandler<UndoRedoEventArgs> UndoRedo
    {
      add
      {
        if (m_undo_event == null)
        {
          m_OnUndoEvent = OnUndoEvent;
          UnsafeNativeMethods.CRhinoEventWatcher_SetUndoEventCallback(m_OnUndoEvent, Rhino.Runtime.HostUtils.m_ew_report);
        }
        m_undo_event -= value;
        m_undo_event += value;
      }
      remove
      {
        m_undo_event -= value;
        if (m_undo_event == null)
        {
          UnsafeNativeMethods.CRhinoEventWatcher_SetUndoEventCallback(null, Rhino.Runtime.HostUtils.m_ew_report);
          m_OnUndoEvent = null;
        }
      }
    }

    #endregion

    /// <summary>
    /// Repeats an operation of a command.
    /// <para>In order to make this function work, you will likely need to grab the Result property that gives the 
    /// list of input objects. Then, you will be able to replace these inputs by using one of the UpdateToX() methods 
    /// of the ReplayHistoryResult.</para>
    /// <para>You should NOT use any document AddX() or ReplaceX() functions, as they will break history.</para>
    /// </summary>
    /// <param name="replayData">The replay history information.</param>
    /// <returns>true if the operation succeeded.
    /// <para>The default implementation always returns false.</para></returns>
    protected virtual bool ReplayHistory(Rhino.DocObjects.ReplayHistoryData replayData)
    {
      return false;
    }
    private static int OnReplayHistory(int command_serial_number, IntPtr pConstRhinoHistoryRecord, IntPtr pObjectPairArray)
    {
      int rc = 0;
      try
      {
        Command cmd = LookUpBySerialNumber(command_serial_number);
        using (var replayData = new DocObjects.ReplayHistoryData(pConstRhinoHistoryRecord, pObjectPairArray))
        {
          rc = cmd.ReplayHistory(replayData) ? 1 : 0;
        }
      }
      catch (Exception ex)
      {
        Rhino.Runtime.HostUtils.ExceptionReport("Command.ReplayHistory", ex);
      }
      return rc;
    }
  }

  public class CommandEventArgs : EventArgs
  {
    internal IntPtr m_pCommand;
    readonly Result m_result;

    internal CommandEventArgs(IntPtr pCommand, int result, uint documentRuntimeSerialNumber)
    {
      m_pCommand = pCommand;
      m_result = (Result)result;
      DocumentRuntimeSerialNumber = documentRuntimeSerialNumber;
    }

    /// <summary>
    /// Gets the ID of the command that raised this event.
    /// </summary>
    /// <since>5.0</since>
    public Guid CommandId
    {
      get { return UnsafeNativeMethods.CRhinoCommand_Id(m_pCommand); }
    }

    string m_english_name;
    string m_local_name;
    /// <summary>
    /// Gets the English name of the command that raised this event.
    /// </summary>
    /// <since>5.0</since>
    public string CommandEnglishName
    {
      get
      {
        if (m_english_name == null)
        {
          using (var sh = new StringHolder())
          {
            IntPtr pStringHolder = sh.NonConstPointer();
            UnsafeNativeMethods.CRhinoCommand_Name(m_pCommand, true, pStringHolder);
            m_english_name = sh.ToString();
          }
        }
        return m_english_name;
      }
    }

    /// <summary>
    /// Gets the name of the command that raised this event in the local language.
    /// </summary>
    /// <since>5.0</since>
    public string CommandLocalName
    {
      get
      {
        if (m_local_name == null)
        {
          using (var sh = new StringHolder())
          {
            IntPtr pStringHolder = sh.NonConstPointer();
            UnsafeNativeMethods.CRhinoCommand_Name(m_pCommand, false, pStringHolder);
            m_local_name = sh.ToString();
          }
        }
        return m_local_name;
      }
    }

    string m_plugin_name;
    /// <summary>
    /// Gets the name of the plug-in that this command belongs to.  If the command is internal
    /// to Rhino, then this property is an empty string.
    /// </summary>
    /// <since>5.0</since>
    public string CommandPluginName
    {
      get
      {
        if (m_plugin_name == null)
        {
          using (var sh = new StringHolder())
          {
            IntPtr pStringHolder = sh.NonConstPointer();
            UnsafeNativeMethods.CRhinoCommand_PlugInName(m_pCommand, pStringHolder);
            m_plugin_name = sh.ToString();
          }
        }
        return m_plugin_name;
      }
    }

    /// <summary>
    /// Gets the result of the command that raised this event. 
    /// This value is only meaningful during EndCommand events.
    /// </summary>
    /// <since>5.0</since>
    public Result CommandResult
    {
      get { return m_result; }
    }

    /// <since>6.0</since>
    [CLSCompliant(false)]
    public uint DocumentRuntimeSerialNumber { get; private set; }
    /// <since>6.0</since>
    public RhinoDoc Document { get { return RhinoDoc.FromRuntimeSerialNumber(DocumentRuntimeSerialNumber); } }
  }

  public class UndoRedoEventArgs : EventArgs
  {
    readonly int m_event_type;
    readonly uint m_serial_number;
    readonly Guid m_command_id;
    internal UndoRedoEventArgs(int undo_event, uint sn, Guid id)
    {
      m_event_type = undo_event;
      m_serial_number = sn;
      m_command_id = id;
    }

    /// <since>5.0</since>
    public Guid CommandId
    {
      get { return m_command_id; }
    }

    /// <since>5.0</since>
    [CLSCompliant(false)]
    public uint UndoSerialNumber
    {
      get { return m_serial_number; }
    }

    /// <since>8.0</since>
    public bool IsBeforeBeginRecording { get { return 7 == m_event_type; } }
    /// <since>5.0</since>
    public bool IsBeginRecording { get { return 1 == m_event_type; } }
    /// <since>8.0</since>
    public bool IsBeforeEndRecording { get { return 8 == m_event_type; } }
    /// <since>5.0</since>
    public bool IsEndRecording { get { return 2 == m_event_type; } }

    /// <since>5.0</since>
    public bool IsBeginUndo { get { return 3 == m_event_type; } }
    /// <since>5.0</since>
    public bool IsEndUndo { get { return 4 == m_event_type; } }

    /// <since>5.0</since>
    public bool IsBeginRedo { get { return 5 == m_event_type; } }
    /// <since>5.0</since>
    public bool IsEndRedo { get { return 6 == m_event_type; } }

    /// <since>5.0</since>
    public bool IsPurgeRecord { get { return 86 == m_event_type; } }
  }


  /// <summary>
  /// For adding nestable whole object and subobject selection commands, derive your command from
  /// SelCommand and override the abstract SelFilter and virtual SelSubObjectFilter functions.
  /// </summary>
  public abstract class SelCommand : Command
  {
    protected override Result RunCommand(RhinoDoc doc, RunMode mode) { return Result.Success; }

    /// <summary>
    /// Override this abstract function and return true if object should be selected.
    /// </summary>
    /// <param name="rhObj">The object to check regarding selection status.</param>
    /// <returns>true if the object should be selected; false otherwise.</returns>
    protected abstract bool SelFilter(Rhino.DocObjects.RhinoObject rhObj);
    internal static int OnSelFilter(int commandSerialNumber, IntPtr pRhinoObject)
    {
      int rc = 0;
      try
      {
        SelCommand cmd = Command.LookUpBySerialNumber(commandSerialNumber) as SelCommand;
        if( cmd!=null )
          rc = cmd.SelFilter(Rhino.DocObjects.RhinoObject.CreateRhinoObjectHelper(pRhinoObject)) ? 1:0;
      }
      catch (Exception ex)
      {
        Runtime.HostUtils.DebugString("Exception caught during SelFilter");
        Rhino.Runtime.HostUtils.ExceptionReport(ex);
      }
      return rc;
    }

    /// <summary>
    /// To select subobjects, override this virtual function, add component indices of the subobjects that 
    /// should get selected to indicesToSelect list and return true.
    /// This is called only if the SelFilter returns false and the whole object does not get selected.
    /// </summary>
    /// <param name="rhObj">The object to check regarding selection status.</param>
    /// <param name="indicesToSelect">The component indices of the subobjects to select.</param>
    /// <returns>
    /// true if components added to indicesToSelect should get selected.
    /// </returns>
    /// <since>7.9</since>
    protected virtual bool SelSubObjectFilter(Rhino.DocObjects.RhinoObject rhObj, List<Rhino.Geometry.ComponentIndex> indicesToSelect) { return false; }
    internal static int OnSelSubObjectFilter(int commandSerialNumber, IntPtr pRhinoObject, IntPtr pComponentIndices)
    {
      bool rc = false;
      try
      {
        SelCommand cmd = Command.LookUpBySerialNumber(commandSerialNumber) as SelCommand;
        if (cmd != null)
        {
          List<Rhino.Geometry.ComponentIndex> indicesToSelect = new List<Geometry.ComponentIndex>();
          rc = cmd.SelSubObjectFilter(Rhino.DocObjects.RhinoObject.CreateRhinoObjectHelper(pRhinoObject), indicesToSelect);
          for (int i = 0; i < indicesToSelect.Count; i++)
          {
            Rhino.Geometry.ComponentIndex ci = indicesToSelect[i];
            UnsafeNativeMethods.ON_ComponentIndexArray_Add(pComponentIndices, ref ci);
          }
        }
      }
      catch (Exception ex)
      {
        Runtime.HostUtils.DebugString("Exception caught during SelSubObjectFilter");
        Rhino.Runtime.HostUtils.ExceptionReport(ex);
      }
      return rc ? 1 : 0;
    }

    const int idxTestLights = 0;
    const int idxTestGrips = 1;
    const int idxBeQuite = 2;

    /// <since>5.0</since>
    public bool TestLights
    {
      get { return UnsafeNativeMethods.CRhinoSelCommand_GetBool(Id, idxTestLights); }
      set { UnsafeNativeMethods.CRhinoSelCommand_SetBool(Id, idxTestLights, value); }
    }
    /// <since>5.0</since>
    public bool TestGrips
    {
      get { return UnsafeNativeMethods.CRhinoSelCommand_GetBool(Id, idxTestGrips); }
      set { UnsafeNativeMethods.CRhinoSelCommand_SetBool(Id, idxTestGrips, value); }
    }
    /// <since>5.0</since>
    public bool BeQuiet
    {
      get { return UnsafeNativeMethods.CRhinoSelCommand_GetBool(Id, idxBeQuite); }
      set { UnsafeNativeMethods.CRhinoSelCommand_SetBool(Id, idxBeQuite, value); }
    }

    internal delegate int SelFilterCallback(int command_id, IntPtr pRhinoObject);
    internal delegate int SelSubObjectCallback(int command_id, IntPtr pRhinoObject, IntPtr pComponentIndices);
  }


  public abstract class TransformCommand : Command
  {
    /// <summary>
    /// Selects objects within the command.
    /// </summary>
    /// <param name="prompt">The selection prompt.</param>
    /// <param name="list">A list of objects to transform. This is a special list type.</param>
    /// <returns>The operation result.</returns>
    protected Result SelectObjects(string prompt, Rhino.Collections.TransformObjectList list)
    {
      IntPtr pList = list.NonConstPointer();
      int rc = UnsafeNativeMethods.CRhinoTransformCommand_SelectObjects(Id, prompt, pList);
      GC.KeepAlive(list);
      return (Result)rc;
    }

    /// <summary>
    /// Selects objects within the command.
    /// </summary>
    /// <param name="prompt">The selection prompt.</param>
    /// <param name="filter">Geometry filter to limit selection. Use function above if you do not need specific types.</param>
    /// <param name="list">A list of objects to transform. This is a special list type.</param>
    /// <returns>The operation result.</returns>
    [CLSCompliant(false)]
    protected Result SelectObjects(string prompt, DocObjects.ObjectType filter, Rhino.Collections.TransformObjectList list)
    {
      IntPtr pList = list.NonConstPointer();
      int rc = UnsafeNativeMethods.CRhinoTransformCommand_SelectFilteredObjects(Id, prompt, (uint)filter, pList);
      return (Result)rc;
    }

    protected void TransformObjects(Rhino.Collections.TransformObjectList list, Rhino.Geometry.Transform xform, bool copy, bool autoHistory)
    {
      IntPtr pList = list.NonConstPointer();
      UnsafeNativeMethods.CRhinoTransformCommand_TransformObjects(Id, pList, ref xform, copy, autoHistory);
      GC.KeepAlive(list);
    }

    protected void DuplicateObjects(Rhino.Collections.TransformObjectList list)
    {
      IntPtr pList = list.NonConstPointer();
      UnsafeNativeMethods.CRhinoTransformCommand_DuplicateObjects(Id, pList);
      GC.KeepAlive(list);
    }

    /// <summary>
    /// Sets dynamic grip locations back to starting grip locations. This makes things
    /// like the Copy command work when grips are "copied".
    /// </summary>
    /// <param name="list">A list of object to transform. This is a special list type.</param>
    protected void ResetGrips(Rhino.Collections.TransformObjectList list)
    {
      IntPtr pList = list.NonConstPointer();
      UnsafeNativeMethods.CRhinoTransformCommand_ResetGrips(Id, pList);
      GC.KeepAlive(list);
    }

    //CRhinoView* View() { return m_view; }
    //bool ObjectsWerePreSelected() { return m_objects_were_preselected; }
  }
}


namespace Rhino.DocObjects
{
  /// <summary>
  /// Provides a single bundling of information to be passed to Rhino when setting up history for an object.
  /// </summary>
  /// <remarks>To use this object, just pass it to a RhinoDoc.Add() method along with the needed geometry.
  /// Do not reuse this class for more than one history addition.</remarks>
  public class HistoryRecord : IDisposable
  // this is the same as CRhinoHistory
  {
    private IntPtr m_pRhinoHistory; // CRhinoHistory*

    /// <summary>
    /// Wrapped native C++ pointer to CRhinoHistory instance
    /// </summary>
    /// <since>5.0</since>
    public IntPtr Handle { get { return m_pRhinoHistory; } }

    /// <since>5.0</since>
    public HistoryRecord(Commands.Command command, int version)
    {
      m_pRhinoHistory = UnsafeNativeMethods.CRhinoHistory_New(command.Id, version);
    }

    IntPtr NonConstPointer()
    {
      return m_pRhinoHistory;
    }

    /// <summary>
    /// Passively reclaims unmanaged resources when the class user did not explicitly call Dispose().
    /// </summary>
    ~HistoryRecord() { Dispose(false); }

    /// <summary>
    /// Actively reclaims unmanaged resources that this instance uses.
    /// </summary>
    /// <since>5.0</since>
    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    /// <summary>
    /// For derived class implementers.
    /// <para>This method is called with argument true when class user calls Dispose(), while with argument false when
    /// the Garbage Collector invokes the finalizer, or Finalize() method.</para>
    /// <para>You must reclaim all used unmanaged resources in both cases, and can use this chance to call Dispose on disposable fields if the argument is true.</para>
    /// <para>Also, you must call the base virtual method within your overriding method.</para>
    /// </summary>
    /// <param name="disposing">true if the call comes from the Dispose() method; false if it comes from the Garbage Collector finalizer.</param>
    protected virtual void Dispose(bool disposing)
    {
      if (IntPtr.Zero != m_pRhinoHistory)
      {
        UnsafeNativeMethods.CRhinoHistory_Delete(m_pRhinoHistory);
      }
      m_pRhinoHistory = IntPtr.Zero;
    }
    
    /// <since>5.0</since>
    public bool SetBool( int id, bool value )
    {
      IntPtr pThis = NonConstPointer();
      return UnsafeNativeMethods.CRhinoHistory_SetBool(pThis, id, value);
    }
    /// <since>5.0</since>
    public bool SetInt(int id, int value)
    {
      IntPtr pThis = NonConstPointer();
      return UnsafeNativeMethods.CRhinoHistory_SetInt(pThis, id, value);
    }
    /// <since>5.0</since>
    public bool SetDouble(int id, double value)
    {
      IntPtr pThis = NonConstPointer();
      return UnsafeNativeMethods.CRhinoHistory_SetDouble(pThis, id, value);
    }
    /// <since>5.0</since>
    public bool SetPoint3d(int id, Rhino.Geometry.Point3d value)
    {
      IntPtr pThis = NonConstPointer();
      return UnsafeNativeMethods.CRhinoHistory_SetPoint3d(pThis, id, value);
    }
    /// <since>5.0</since>
    public bool SetVector3d(int id, Rhino.Geometry.Vector3d value)
    {
      IntPtr pThis = NonConstPointer();
      return UnsafeNativeMethods.CRhinoHistory_SetVector3d(pThis, id, value);
    }
    /// <since>5.0</since>
    public bool SetTransorm(int id, Rhino.Geometry.Transform value)
    {
      IntPtr pThis = NonConstPointer();
      return UnsafeNativeMethods.CRhinoHistory_SetXform(pThis, id, ref value);
    }
    /// <since>5.0</since>
    public bool SetColor(int id, System.Drawing.Color value)
    {
      IntPtr pThis = NonConstPointer();
      return UnsafeNativeMethods.CRhinoHistory_SetColor(pThis, id, value.ToArgb());
    }
    /// <since>5.0</since>
    public bool SetObjRef(int id, ObjRef value)
    {
      IntPtr pThis = NonConstPointer();
      IntPtr pConstObjRef = value.ConstPointer();
      return UnsafeNativeMethods.CRhinoHistory_SetObjRef(pThis, id, pConstObjRef);
    }
    /// <since>5.0</since>
    public bool SetPoint3dOnObject(int id, ObjRef objref, Rhino.Geometry.Point3d value)
    {
      IntPtr pThis = NonConstPointer();
      IntPtr pConstObjRef = objref.ConstPointer();
      return UnsafeNativeMethods.CRhinoHistory_SetPoint3dOnObject(pThis, id, pConstObjRef, value);
    }
    /// <since>5.0</since>
    public bool SetGuid(int id, Guid value)
    {
      IntPtr pThis = NonConstPointer();
      return UnsafeNativeMethods.CRhinoHistory_SetUuid(pThis, id, value);
    }
    /// <since>5.0</since>
    public bool SetString(int id, string value)
    {
      IntPtr pThis = NonConstPointer();
      return UnsafeNativeMethods.CRhinoHistory_SetString(pThis, id, value);
    }
    // ON_Geometry* is non-const. I think we can't delete it from under the history record.
    // Don't wrap until we really need it
    //public bool SetGeometry( int id, Geometry.GeometryBase value){ return false; }

    /// <since>5.0</since>
    public bool SetCurve(int id, Geometry.Curve value)
    {
      IntPtr pThis = NonConstPointer();
      IntPtr pConstCurve = value.ConstPointer();
      return UnsafeNativeMethods.CRhinoHistory_SetCurve(pThis, id, pConstCurve);
    }
    /// <since>5.0</since>
    public bool SetSurface(int id, Geometry.Surface value)
    {
      IntPtr pThis = NonConstPointer();
      IntPtr pConstSurface = value.ConstPointer();
      return UnsafeNativeMethods.CRhinoHistory_SetSurface(pThis, id, pConstSurface);
    }
    /// <since>5.0</since>
    public bool SetBrep(int id, Geometry.Brep value)
    {
      IntPtr pThis = NonConstPointer();
      IntPtr pConstBrep = value.ConstPointer();
      return UnsafeNativeMethods.CRhinoHistory_SetBrep(pThis, id, pConstBrep);
    }
    /// <since>5.0</since>
    public bool SetMesh(int id, Geometry.Mesh value)
    {
      IntPtr pThis = NonConstPointer();
      IntPtr pConstMesh = value.ConstPointer();
      return UnsafeNativeMethods.CRhinoHistory_SetMesh(pThis, id, pConstMesh);
    }

    // PolyEdge not wrapped yet
    //bool SetPolyEdgeValue( CRhinoDoc& doc, int value_id, const class CRhinoPolyEdge& polyedge );

    /// <since>5.0</since>
    public bool SetBools(int id, IEnumerable<bool> values)
    {
      List<bool> v = new List<bool>(values);
      var _v = v.ToArray();
      IntPtr pThis = NonConstPointer();
      return UnsafeNativeMethods.CRhinoHistory_SetBools(pThis, id, _v.Length, _v);
    }
    /// <since>5.0</since>
    public bool SetInts(int id, IEnumerable<int> values)
    {
      List<int> v = new List<int>(values);
      var _v = v.ToArray();
      IntPtr pThis = NonConstPointer();
      return UnsafeNativeMethods.CRhinoHistory_SetInts(pThis, id, _v.Length, _v);
    }
    /// <since>5.0</since>
    public bool SetDoubles(int id, IEnumerable<double> values)
    {
      List<double> v = new List<double>(values);
      var _v = v.ToArray();
      IntPtr pThis = NonConstPointer();
      return UnsafeNativeMethods.CRhinoHistory_SetDoubles(pThis, id, _v.Length, _v);
    }
    /// <since>5.0</since>
    public bool SetPoint3ds(int id, IEnumerable<Rhino.Geometry.Point3d> values)
    {
      List<Geometry.Point3d> v = new List<Geometry.Point3d>(values);
      var _v = v.ToArray();
      IntPtr pThis = NonConstPointer();
      return UnsafeNativeMethods.CRhinoHistory_SetPoints(pThis, id, _v.Length, _v);
    }
    /// <since>5.0</since>
    public bool SetVector3ds(int id, IEnumerable<Rhino.Geometry.Vector3d> values)
    {
      List<Geometry.Vector3d> v = new List<Geometry.Vector3d>(values);
      var _v = v.ToArray();
      IntPtr pThis = NonConstPointer();
      return UnsafeNativeMethods.CRhinoHistory_SetVectors(pThis, id, _v.Length, _v);
    }
    //public bool SetTransorms(int id, IEnumerable<Rhino.Geometry.Transform> values)
    //{
    //  List<Geometry.Transform> v = new List<Geometry.Transform>(values);
    //  var _v = v.ToArray();
    //  IntPtr pThis = NonConstPointer();
    //  return UnsafeNativeMethods.CRhinoHistory_SetXforms(pThis, id, _v.Length, _v);
    //}

    /// <since>5.0</since>
    public bool SetColors(int id, IEnumerable<System.Drawing.Color> values)
    {
      List<int> argb = new List<int>();
      foreach (System.Drawing.Color c in values)
        argb.Add(c.ToArgb());
      var _v = argb.ToArray();
      IntPtr pThis = NonConstPointer();
      return UnsafeNativeMethods.CRhinoHistory_SetColors(pThis, id, _v.Length, _v);
    }

    // need ON_ClassArray<CRhinoObjRef>* wrapper
    //public bool SetObjRefs(int id, IEnumerable<ObjRef> values);

    /// <since>5.0</since>
    public bool SetGuids(int id, IEnumerable<Guid> values)
    {
      List<Guid> v = new List<Guid>(values);
      var _v = v.ToArray();
      IntPtr pThis = NonConstPointer();
      return UnsafeNativeMethods.CRhinoHistory_SetUuids(pThis, id, _v.Length, _v);
    }

    /// <since>5.0</since>
    public bool SetStrings(int id, IEnumerable<string> values)
    {
      using (var strings = new ClassArrayString())
      {
        foreach (string v in values)
          strings.Add(v);
        IntPtr ptr_strings = strings.NonConstPointer();
        IntPtr ptr_this = NonConstPointer();
        return UnsafeNativeMethods.CRhinoHistory_SetStrings(ptr_this, id, ptr_strings);
      }
    }
    // ON_Geometry* is non-const. I think we can't delete it from under the history record.
    // Don't wrap until we really need it
    //bool SetGeometryValues( int value_id, const ON_SimpleArray<ON_Geometry*> a);

    // PolyEdge not wrapped yet
    //bool SetPolyEdgeValues( CRhinoDoc& doc, int value_id, int count, const class CRhinoPolyEdge* const* polyedges );

    /// <summary>
    /// Specifies a non-zero integer that identifies the version of
    /// this history record. The virtual ReplayHistory() functions
    /// can check this version to avoid replaying history using
    /// information created by earlier versions of the command.
    /// </summary>
    /// <param name="historyVersion">Any non-zero integer.
    /// It is strongly suggested that something like YYYYMMDD be used.</param>
    /// <returns>True if successful.</returns>
    /// <since>6.0</since>
    public bool SetHistoryVersion(int historyVersion)
    {
      IntPtr p_this = NonConstPointer();
      return UnsafeNativeMethods.CRhinoHistory_SetHistoryVersion(p_this, historyVersion);
    }

    /// <summary>
    /// When an object is replaced and the old object has a history record with
    /// this field set, the history record is copied and attached to the new object.
    /// That allows a descendant object to continue the history linkage after
    /// it is edited.
    /// </summary>
    /// <since>6.0</since>
    public bool CopyOnReplaceObject
    {
      get
      {
        IntPtr p_this = NonConstPointer();
        return UnsafeNativeMethods.CRhinoHistory_CopyOnReplaceObject(p_this);
      }
      set
      {
        IntPtr p_this = NonConstPointer();
        UnsafeNativeMethods.CRhinoHistory_SetCopyOnReplaceObject(p_this, value);
      }
    }
  }



  // TODO: Implement ReplayHistoryData in order to support history on commands
  // this is the same as CRhinoHistoryRecord
  /// <summary>
  /// Provides history information to commands that will repeat history construction.
  /// Generally, a developer using this class will not construct a new instance, but receive one.
  /// </summary>
  public class ReplayHistoryData : IDisposable
  {
    IntPtr m_pConstRhinoHistoryRecord;
    internal IntPtr m_pObjectPairArray;
    // this should only be constructed in the ReplayHistory callback
    internal ReplayHistoryData(IntPtr pConstRhinoHistoryRecord, IntPtr pObjectPairArray)
    {
      m_pConstRhinoHistoryRecord = pConstRhinoHistoryRecord;
      m_pObjectPairArray = pObjectPairArray;
    }

    /// <since>5.0</since>
    public void Dispose()
    {
      m_pConstRhinoHistoryRecord = IntPtr.Zero;
      m_pObjectPairArray = IntPtr.Zero;
    }

    /// <summary>
    /// In ReplayHistory, use GetRhinoObjRef to convert the information
    /// in a history record into the ObjRef that has up to date
    /// RhinoObject pointers
    /// </summary>
    /// <param name="id">HistoryRecord value id</param>
    /// <returns>ObjRef on success, null if not successful</returns>
    /// <since>5.0</since>
    public Rhino.DocObjects.ObjRef GetRhinoObjRef(int id)
    {
      ObjRef objref = new ObjRef();
      IntPtr pObjRef = objref.NonConstPointer();
      if (UnsafeNativeMethods.CRhinoHistoryRecord_GetRhinoObjRef(m_pConstRhinoHistoryRecord, id, pObjRef))
        return objref;
      return null;
    }

    // <summary>The command associated with this history record</summary>
    // public Commands.Command Command{ get { return null; } }

    /// <summary>The document this record belongs to</summary>
    /// <since>5.0</since>
    public RhinoDoc Document
    {
      get
      {
        uint serial_number = UnsafeNativeMethods.CRhinoHistoryRecord_Document(m_pConstRhinoHistoryRecord);
        return RhinoDoc.FromRuntimeSerialNumber(serial_number);
      }
    }

    /// <summary>
    /// ReplayHistory overrides check the version number to ensure the information
    /// saved in the history record is compatible with the current implementation
    /// of ReplayHistory
    /// </summary>
    /// <since>5.0</since>
    public int HistoryVersion
    {
      get { return UnsafeNativeMethods.CRhinoHistoryRecord_HistoryVersion(m_pConstRhinoHistoryRecord); }
    }

    /// <summary>
    /// Each history record has a unique id that Rhino assigns when it adds the
    /// history record to the history record table
    /// </summary>
    /// <since>5.0</since>
    public Guid RecordId
    {
      get { return UnsafeNativeMethods.CRhinoHistoryRecord_HistoryRecordId(m_pConstRhinoHistoryRecord); }
    }

    ReplayHistoryResult[] m_results;

    /// <summary>
    /// Provides access to BOTH inputs and outputs of the replay history operation.
    /// <para>Use this property to then call an appropriate UpdateToX() method and make your
    /// custom history support work.</para>
    /// </summary>
    /// <since>5.0</since>
    public ReplayHistoryResult[] Results
    {
      get
      {
        if (m_results == null)
        {
          int count = UnsafeNativeMethods.CRhinoObjectPairArray_Count(m_pObjectPairArray);
          m_results = new ReplayHistoryResult[count];
          for (int i = 0; i < count; i++)
            m_results[i] = new ReplayHistoryResult(this, i);
        }
        return m_results;
      }
    }

    /// <summary>
    /// Create an empty history result and add it to the end of the Results array.
    /// Note that you should call Results again if you need them as the old Results
    /// array will be out of sync with this class.
    /// </summary>
    /// <returns></returns>
    public ReplayHistoryResult AppendHistoryResult()
    {
      int index = UnsafeNativeMethods.CRhinoObjectPairArray_Append(m_pObjectPairArray);
      if (index < 0)
        return null;

      var rc = new ReplayHistoryResult(this, index);
      var oldResults = m_results;
      m_results = null;
      if (oldResults != null && index == oldResults.Length)
      {
        int count = index + 1;
        m_results = new ReplayHistoryResult[count];
        for (int i=0; i < (count-1); i++)
        {
          m_results[i] = oldResults[i];
        }
        m_results[count - 1] = rc;
      }
      return rc;
    }

    /// <summary>
    /// Update the Results array with a different set of values. Null entries in the newResults
    /// will result in empty ReplayHistoryResult elements
    /// </summary>
    /// <param name="newResults"></param>
    public void UpdateResultArray(IEnumerable<ReplayHistoryResult> newResults)
    {
      if (newResults == null)
      {
        UnsafeNativeMethods.CRhinoObjectPairArray_UpdateElements(m_pObjectPairArray, null, 0);
        m_results = null;
        return;
      }

      List<int> updateIndices = new List<int>();
      foreach(var result in newResults)
      {
        if (result == null)
        {
          updateIndices.Add(-1);
          continue;
        }
        updateIndices.Add(result.m_index);
      }

      int[] indices = updateIndices.ToArray();
      UnsafeNativeMethods.CRhinoObjectPairArray_UpdateElements(m_pObjectPairArray, indices, indices.Length);
      m_results = null;
    }

    /// <since>5.0</since>
    public bool TryGetBool(int id, out bool value)
    {
      value = false;
      return UnsafeNativeMethods.CRhinoHistoryRecord_GetBool(m_pConstRhinoHistoryRecord, id, ref value);
    }

    /// <since>5.0</since>
    public bool TryGetInt(int id, out int value)
    {
      value = 0;
      return UnsafeNativeMethods.CRhinoHistoryRecord_GetInt(m_pConstRhinoHistoryRecord, id, ref value);
    }

    /// <since>5.0</since>
    public bool TryGetDouble(int id, out double value)
    {
      value = 0;
      return UnsafeNativeMethods.CRhinoHistoryRecord_GetDouble(m_pConstRhinoHistoryRecord, id, ref value);
    }

    /// <since>5.0</since>
    public bool TryGetPoint3d(int id, out Geometry.Point3d value)
    {
      value = new Geometry.Point3d();
      return UnsafeNativeMethods.CRhinoHistoryRecord_GetPoint3d(m_pConstRhinoHistoryRecord, id, ref value);
    }

    public bool TryGetGuids(int id, out Guid[] values)
    {
      values = new Guid[] { };
      using (var array = new SimpleArrayGuid())
      {
        IntPtr ptr_array = array.NonConstPointer();
        bool rc = UnsafeNativeMethods.CRhinoHistoryRecord_GetUuids(m_pConstRhinoHistoryRecord, id, ptr_array);
        if (rc)
          values = array.ToArray();
        return rc;
      }
    }
    /// <since>5.0</since>
    public bool TryGetVector3d(int id, out Geometry.Vector3d value)
    {
      value = new Geometry.Vector3d();
      return UnsafeNativeMethods.CRhinoHistoryRecord_GetVector3d(m_pConstRhinoHistoryRecord, id, ref value);
    }

    /// <since>5.0</since>
    public bool TryGetTransform(int id, out Geometry.Transform value)
    {
      value = Geometry.Transform.Identity;
      return UnsafeNativeMethods.CRhinoHistoryRecord_GetTransform(m_pConstRhinoHistoryRecord, id, ref value);
    }

    /// <since>5.0</since>
    public bool TryGetColor(int id, out System.Drawing.Color value)
    {
      value = System.Drawing.Color.Empty;
      int argb = 0;
      bool rc = UnsafeNativeMethods.CRhinoHistoryRecord_GetColor(m_pConstRhinoHistoryRecord, id, ref argb);
      if (rc)
        value = System.Drawing.Color.FromArgb(argb);
      return rc;
    }

    /// <since>6.0</since>
    public bool TryGetPoint3dOnObject(int id, out Geometry.Point3d value)
    {
      value = new Geometry.Point3d();
      return UnsafeNativeMethods.CRhinoHistoryRecord_GetPoint3dOnObject(m_pConstRhinoHistoryRecord, id, ref value);
    }


    /*
    public bool SetObjRef(int id, ObjRef value)
    {
      IntPtr pThis = NonConstPointer();
      IntPtr pConstObjRef = value.ConstPointer();
      return UnsafeNativeMethods.CRhinoHistory_SetObjRef(pThis, id, pConstObjRef);
    }
    public bool SetPoint3dOnObject(int id, ObjRef objref, Rhino.Geometry.Point3d value)
    {
      IntPtr pThis = NonConstPointer();
      IntPtr pConstObjRef = objref.ConstPointer();
      return UnsafeNativeMethods.CRhinoHistory_SetPoint3dOnObject(pThis, id, pConstObjRef, value);
    }
    */

    /// <since>5.0</since>
    public bool TryGetGuid(int id, out Guid value)
    {
      value = Guid.Empty;
      return UnsafeNativeMethods.CRhinoHistoryRecord_GetGuid(m_pConstRhinoHistoryRecord, id, ref value);
    }

    /// <since>5.0</since>
    public bool TryGetString(int id, out string value)
    {
      value = string.Empty;
      using (var sh = new StringHolder())
      {
        IntPtr pString = sh.NonConstPointer();
        bool rc = UnsafeNativeMethods.CRhinoHistoryRecord_GetString(m_pConstRhinoHistoryRecord, id, pString);
        if (rc)
          value = sh.ToString();
        return rc;
      }
    }

    /*
    // ON_Geometry* is non-const. I think we can't delete it from under the history record.
    // Don't wrap until we really need it
    //public bool SetGeometry( int id, Geometry.GeometryBase value){ return false; }

    public bool SetCurve(int id, Geometry.Curve value)
    {
      IntPtr pThis = NonConstPointer();
      IntPtr pConstCurve = value.ConstPointer();
      return UnsafeNativeMethods.CRhinoHistory_SetCurve(pThis, id, pConstCurve);
    }
    public bool SetSurface(int id, Geometry.Surface value)
    {
      IntPtr pThis = NonConstPointer();
      IntPtr pConstSurface = value.ConstPointer();
      return UnsafeNativeMethods.CRhinoHistory_SetSurface(pThis, id, pConstSurface);
    }
    public bool SetBrep(int id, Geometry.Brep value)
    {
      IntPtr pThis = NonConstPointer();
      IntPtr pConstBrep = value.ConstPointer();
      return UnsafeNativeMethods.CRhinoHistory_SetBrep(pThis, id, pConstBrep);
    }
    public bool SetMesh(int id, Geometry.Mesh value)
    {
      IntPtr pThis = NonConstPointer();
      IntPtr pConstMesh = value.ConstPointer();
      return UnsafeNativeMethods.CRhinoHistory_SetMesh(pThis, id, pConstMesh);
    }

    // PolyEdge not wrapped yet
    //bool SetPolyEdgeValue( CRhinoDoc& doc, int value_id, const class CRhinoPolyEdge& polyedge );

    public bool SetBools(int id, IEnumerable<bool> values)
    {
      List<bool> v = new List<bool>(values);
      var _v = v.ToArray();
      IntPtr pThis = NonConstPointer();
      return UnsafeNativeMethods.CRhinoHistory_SetBools(pThis, id, _v.Length, _v);
    }
    public bool SetInts(int id, IEnumerable<int> values)
    {
      List<int> v = new List<int>(values);
      var _v = v.ToArray();
      IntPtr pThis = NonConstPointer();
      return UnsafeNativeMethods.CRhinoHistory_SetInts(pThis, id, _v.Length, _v);
    }
    */
    /// <since>6.10</since>
    public bool TryGetDoubles(int id, out double[] values)
    {
      values = new double[]{ };
      using (var array = new SimpleArrayDouble())
      {
        IntPtr ptr_array = array.NonConstPointer();
        bool rc = UnsafeNativeMethods.CRhinoHistoryRecord_GetDoubles(m_pConstRhinoHistoryRecord, id, ptr_array);
        if (rc)
          values = array.ToArray();
        return rc;
      }
    }
    /*
    public bool SetPoint3ds(int id, IEnumerable<Rhino.Geometry.Point3d> values)
    {
      List<Geometry.Point3d> v = new List<Geometry.Point3d>(values);
      var _v = v.ToArray();
      IntPtr pThis = NonConstPointer();
      return UnsafeNativeMethods.CRhinoHistory_SetPoints(pThis, id, _v.Length, _v);
    }
    public bool SetVector3ds(int id, IEnumerable<Rhino.Geometry.Vector3d> values)
    {
      List<Geometry.Vector3d> v = new List<Geometry.Vector3d>(values);
      var _v = v.ToArray();
      IntPtr pThis = NonConstPointer();
      return UnsafeNativeMethods.CRhinoHistory_SetVectors(pThis, id, _v.Length, _v);
    }
    //public bool SetTransorms(int id, IEnumerable<Rhino.Geometry.Transform> values)
    //{
    //  List<Geometry.Transform> v = new List<Geometry.Transform>(values);
    //  var _v = v.ToArray();
    //  IntPtr pThis = NonConstPointer();
    //  return UnsafeNativeMethods.CRhinoHistory_SetXforms(pThis, id, _v.Length, _v);
    //}

    public bool SetColors(int id, IEnumerable<System.Drawing.Color> values)
    {
      List<int> argb = new List<int>();
      foreach (System.Drawing.Color c in values)
        argb.Add(c.ToArgb());
      var _v = argb.ToArray();
      IntPtr pThis = NonConstPointer();
      return UnsafeNativeMethods.CRhinoHistory_SetColors(pThis, id, _v.Length, _v);
    }

    // need ON_ClassArray<CRhinoObjRef>* wrapper
    //public bool SetObjRefs(int id, IEnumerable<ObjRef> values);

    public bool SetGuids(int id, IEnumerable<Guid> values)
    {
      List<Guid> v = new List<Guid>(values);
      var _v = v.ToArray();
      IntPtr pThis = NonConstPointer();
      return UnsafeNativeMethods.CRhinoHistory_SetUuids(pThis, id, _v.Length, _v);
    }

    public bool SetStrings(int id, IEnumerable<string> values)
    {
      IntPtr pStrings = UnsafeNativeMethods.ON_StringArray_New();
      foreach (string v in values)
        UnsafeNativeMethods.ON_StringArray_Append(pStrings, v);
      IntPtr pThis = NonConstPointer();
      bool rc = UnsafeNativeMethods.CRhinoHistory_SetStrings(pThis, id, pStrings);
      UnsafeNativeMethods.ON_StringArray_Delete(pStrings);
      return rc;
    }
     */

  }

  public class ReplayHistoryResult
  {
    readonly ReplayHistoryData m_parent;
    internal int m_index;
    internal ReplayHistoryResult(ReplayHistoryData parent, int index)
    {
      m_parent = parent;
      m_index = index;
    }

    RhinoObject m_existing;

    /// <summary>
    /// The previously existing object.
    /// <para>Do not attempt to edit this object. It might have been already deleted by, for example, dragging.</para>
    /// </summary>
    /// <since>5.0</since>
    public RhinoObject ExistingObject
    {
      get
      {
        if (m_existing == null)
        {
          IntPtr pRhinoObject = UnsafeNativeMethods.CRhinoObjectPairArray_ItemAt(m_parent.m_pObjectPairArray, m_index, true);
          m_existing = RhinoObject.CreateRhinoObjectHelper(pRhinoObject);
        }
        return m_existing;
      }
    }


    /// <since>5.0</since>
    public bool UpdateToPoint(Rhino.Geometry.Point3d point, ObjectAttributes attributes)
    {
      IntPtr pConstAttributes = (attributes == null) ? IntPtr.Zero : attributes.ConstPointer();
      return UnsafeNativeMethods.CRhinoObjectPairArray_UpdateResult1(m_parent.m_pObjectPairArray, m_index, point, pConstAttributes);
    }

    /// <since>5.0</since>
    public bool UpdateToPointCloud(Rhino.Geometry.PointCloud cloud, ObjectAttributes attributes)
    {
      IntPtr pCloud = cloud.ConstPointer();
      IntPtr pConstAttributes = (attributes == null) ? IntPtr.Zero : attributes.ConstPointer();
      return UnsafeNativeMethods.CRhinoObjectPairArray_UpdateResult2(m_parent.m_pObjectPairArray, m_index, pCloud, pConstAttributes);
    }

    /// <since>5.0</since>
    public bool UpdateToPointCloud(IEnumerable<Rhino.Geometry.Point3d> points, DocObjects.ObjectAttributes attributes)
    {
      int count;
      Rhino.Geometry.Point3d[] ptArray = Collections.RhinoListHelpers.GetConstArray(points, out count);
      if (null == ptArray || count < 1)
        return false;

      IntPtr pAttrs = (attributes == null) ? IntPtr.Zero : attributes.ConstPointer();
      return UnsafeNativeMethods.CRhinoObjectPairArray_UpdateResult3(m_parent.m_pObjectPairArray, m_index, count, ptArray, pAttrs);
    }

    /// <since>5.0</since>
    public bool UpdateToClippingPlane(Geometry.Plane plane, double uMagnitude, double vMagnitude, Guid clippedViewportId, ObjectAttributes attributes)
    {
      return UpdateToClippingPlane(plane, uMagnitude, vMagnitude, new Guid[] { clippedViewportId }, attributes);
    }
    /// <since>5.0</since>
    public bool UpdateToClippingPlane(Geometry.Plane plane, double uMagnitude, double vMagnitude, IEnumerable<Guid> clippedViewportIds, ObjectAttributes attributes)
    {
      List<Guid> ids = new List<Guid>(clippedViewportIds);
      Guid[] clippedIds = ids.ToArray();
      int count = clippedIds.Length;
      if (count < 1)
        return false;

      IntPtr pConstAttributes = (attributes == null) ? IntPtr.Zero : attributes.ConstPointer();
      return UnsafeNativeMethods.CRhinoObjectPairArray_UpdateResult4(m_parent.m_pObjectPairArray, m_index, ref plane, uMagnitude, vMagnitude, count, clippedIds, pConstAttributes);
    }

    /// <since>5.0</since>
    public bool UpdateToLinearDimension(Geometry.LinearDimension dimension, ObjectAttributes attributes)
    {
      IntPtr pConstDimension = dimension.ConstPointer();
      IntPtr pConstAttributes = (attributes == null) ? IntPtr.Zero : attributes.ConstPointer();

      return UnsafeNativeMethods.CRhinoObjectPairArray_UpdateResult5(m_parent.m_pObjectPairArray, m_index, pConstDimension, pConstAttributes);
    }

    /// <since>5.0</since>
    public bool UpdateToRadialDimension(Geometry.RadialDimension dimension, ObjectAttributes attributes)
    {
      IntPtr pConstDimension = dimension.ConstPointer();
      IntPtr pConstAttributes = (attributes == null) ? IntPtr.Zero : attributes.ConstPointer();

      return UnsafeNativeMethods.CRhinoObjectPairArray_UpdateResult6(m_parent.m_pObjectPairArray, m_index, pConstDimension, pConstAttributes);
    }

    /// <since>5.0</since>
    public bool UpdateToAngularDimension(Geometry.AngularDimension dimension, ObjectAttributes attributes)
    {
      IntPtr pConstDimension = dimension.ConstPointer();
      IntPtr pConstAttributes = (attributes == null) ? IntPtr.Zero : attributes.ConstPointer();

      return UnsafeNativeMethods.CRhinoObjectPairArray_UpdateResult7(m_parent.m_pObjectPairArray, m_index, pConstDimension, pConstAttributes);
    }

    /// <since>5.0</since>
    public bool UpdateToLine(Geometry.Point3d from, Geometry.Point3d to, DocObjects.ObjectAttributes attributes)
    {
      IntPtr pConstAttributes = (attributes == null) ? IntPtr.Zero : attributes.ConstPointer();
      return UnsafeNativeMethods.CRhinoObjectPairArray_UpdateResult8(m_parent.m_pObjectPairArray, m_index, from, to, pConstAttributes);
    }

    /// <since>5.0</since>
    public bool UpdateToPolyline(IEnumerable<Geometry.Point3d> points, DocObjects.ObjectAttributes attributes)
    {
      int count;
      Geometry.Point3d[] ptArray = Collections.RhinoListHelpers.GetConstArray(points, out count);
      if (null == ptArray || count < 1)
        return false;
      IntPtr pConstAttributes = (attributes == null) ? IntPtr.Zero : attributes.ConstPointer();
      return UnsafeNativeMethods.CRhinoObjectPairArray_UpdateToPolyline(m_parent.m_pObjectPairArray, m_index, count, ptArray, pConstAttributes);
    }

    /// <since>5.0</since>
    public bool UpdateToArc(Geometry.Arc arc, DocObjects.ObjectAttributes attributes)
    {
      IntPtr pConstAttributes = (attributes == null) ? IntPtr.Zero : attributes.ConstPointer();
      return UnsafeNativeMethods.CRhinoObjectPairArray_UpdateToArc(m_parent.m_pObjectPairArray, m_index, ref arc, pConstAttributes);
    }

    /// <since>5.0</since>
    public bool UpdateToCircle(Geometry.Circle circle, DocObjects.ObjectAttributes attributes)
    {
      IntPtr pConstAttributes = (attributes == null) ? IntPtr.Zero : attributes.ConstPointer();
      return UnsafeNativeMethods.CRhinoObjectPairArray_UpdateToCircle(m_parent.m_pObjectPairArray, m_index, ref circle, pConstAttributes);
    }

    /// <since>5.0</since>
    public bool UpdateToEllipse(Geometry.Ellipse ellipse, DocObjects.ObjectAttributes attributes)
    {
      IntPtr pConstAttributes = (attributes == null) ? IntPtr.Zero : attributes.ConstPointer();
      return UnsafeNativeMethods.CRhinoObjectPairArray_UpdateToEllipse(m_parent.m_pObjectPairArray, m_index, ref ellipse, pConstAttributes);
    }

    /// <since>5.0</since>
    public bool UpdateToSphere(Geometry.Sphere sphere, DocObjects.ObjectAttributes attributes)
    {
      IntPtr pConstAttributes = (attributes == null) ? IntPtr.Zero : attributes.ConstPointer();
      return UnsafeNativeMethods.CRhinoObjectPairArray_UpdateToSphere(m_parent.m_pObjectPairArray, m_index, ref sphere, pConstAttributes);
    }

    /// <since>5.0</since>
    public bool UpdateToCurve(Geometry.Curve curve, DocObjects.ObjectAttributes attributes)
    {
      IntPtr pConstAttributes = (attributes == null) ? IntPtr.Zero : attributes.ConstPointer();
      IntPtr pConstCurve = curve.ConstPointer();
      return UnsafeNativeMethods.CRhinoObjectPairArray_UpdateToCurve(m_parent.m_pObjectPairArray, m_index, pConstCurve, pConstAttributes);
    }

    /// <since>5.0</since>
    public bool UpdateToTextDot(Geometry.TextDot dot, DocObjects.ObjectAttributes attributes)
    {
      IntPtr pConstAttributes = (attributes == null) ? IntPtr.Zero : attributes.ConstPointer();
      IntPtr pConstDot = dot.ConstPointer();
      return UnsafeNativeMethods.CRhinoObjectPairArray_UpdateToTextDot(m_parent.m_pObjectPairArray, m_index, pConstDot, pConstAttributes);
    }

    /// <since>5.0</since>
    public bool UpdateToText(string text, Geometry.Plane plane, double height, string fontName, bool bold, bool italic, Geometry.TextJustification justification, DocObjects.ObjectAttributes attributes)
    {
      if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(fontName))
        return false;
      int fontStyle = 0;
      if (bold)
        fontStyle |= 1;
      if (italic)
        fontStyle |= 2;
      IntPtr pConstAttributes = (attributes == null) ? IntPtr.Zero : attributes.ConstPointer();
      RhinoDoc doc = m_parent.Document;
      uint docId = (doc == null) ? 0 : doc.RuntimeSerialNumber;
      return UnsafeNativeMethods.CRhinoObjectPairArray_UpdateToText(m_parent.m_pObjectPairArray, docId, m_index, text, ref plane, height, fontName, fontStyle, (int)justification, pConstAttributes);
    }

    /// <since>5.0</since>
    public bool UpdateToText(Geometry.TextEntity text, DocObjects.ObjectAttributes attributes)
    {
      IntPtr pConstAttributes = (attributes == null) ? IntPtr.Zero : attributes.ConstPointer();
      IntPtr pConstText = text.ConstPointer();
      return UnsafeNativeMethods.CRhinoObjectPairArray_UpdateToText2(m_parent.m_pObjectPairArray, m_index, pConstText, pConstAttributes);
    }

    /// <since>5.0</since>
    public bool UpdateToSurface(Geometry.Surface surface, DocObjects.ObjectAttributes attributes)
    {
      IntPtr pConstAttributes = (attributes == null) ? IntPtr.Zero : attributes.ConstPointer();
      IntPtr pConstSurface = surface.ConstPointer();
      return UnsafeNativeMethods.CRhinoObjectPairArray_UpdateToSurface(m_parent.m_pObjectPairArray, m_index, pConstSurface, pConstAttributes);
    }

    /// <since>5.0</since>
    public bool UpdateToExtrusion(Geometry.Extrusion extrusion, DocObjects.ObjectAttributes attributes)
    {
      IntPtr pConstAttributes = (attributes == null) ? IntPtr.Zero : attributes.ConstPointer();
      IntPtr pConstExtrusion = extrusion.ConstPointer();
      return UnsafeNativeMethods.CRhinoObjectPairArray_UpdateToExtrusion(m_parent.m_pObjectPairArray, m_index, pConstExtrusion, pConstAttributes);
    }

    /// <since>5.0</since>
    public bool UpdateToMesh(Geometry.Mesh mesh, DocObjects.ObjectAttributes attributes)
    {
      IntPtr pConstAttributes = (attributes == null) ? IntPtr.Zero : attributes.ConstPointer();
      IntPtr pConstMesh = mesh.ConstPointer();
      return UnsafeNativeMethods.CRhinoObjectPairArray_UpdateToMesh(m_parent.m_pObjectPairArray, m_index, pConstMesh, pConstAttributes);
    }

    /// <since>7.18</since>
    public bool UpdateToSubD(Geometry.SubD subD, DocObjects.ObjectAttributes attributes)
    {
      IntPtr pConstAttributes = (attributes == null) ? IntPtr.Zero : attributes.ConstPointer();
      IntPtr pConstSubD = subD.ConstPointer();
      return UnsafeNativeMethods.CRhinoObjectPairArray_UpdateToSubD(m_parent.m_pObjectPairArray, m_index, pConstSubD, pConstAttributes);
    }

    /// <since>5.0</since>
    public bool UpdateToBrep(Geometry.Brep brep, DocObjects.ObjectAttributes attributes)
    {
      IntPtr pConstAttributes = (attributes == null) ? IntPtr.Zero : attributes.ConstPointer();
      IntPtr pConstBrep = brep.ConstPointer();
      return UnsafeNativeMethods.CRhinoObjectPairArray_UpdateToBrep(m_parent.m_pObjectPairArray, m_index, pConstBrep, pConstAttributes);
    }

    /// <since>5.0</since>
    public bool UpdateToLeader(Geometry.Leader leader, DocObjects.ObjectAttributes attributes)
    {
      IntPtr pConstAttributes = (attributes == null) ? IntPtr.Zero : attributes.ConstPointer();
      IntPtr pConstLeader = leader.ConstPointer();

      return UnsafeNativeMethods.CRhinoObjectPairArray_UpdateToLeader(m_parent.m_pObjectPairArray, m_index, pConstLeader, pConstAttributes);
    }

    /// <since>5.0</since>
    public bool UpdateToHatch(Geometry.Hatch hatch, DocObjects.ObjectAttributes attributes)
    {
      IntPtr pConstAttributes = (attributes == null) ? IntPtr.Zero : attributes.ConstPointer();
      IntPtr pConstHatch = hatch.ConstPointer();
      return UnsafeNativeMethods.CRhinoObjectPairArray_UpdateToHatch(m_parent.m_pObjectPairArray, m_index, pConstHatch, pConstAttributes);
    }
  }
}
#endif
