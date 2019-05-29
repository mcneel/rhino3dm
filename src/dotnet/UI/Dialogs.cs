#pragma warning disable 1591
using System;
using System.Collections.Generic;
using System.Linq;
using Rhino.Runtime;
using Rhino.Runtime.InteropWrappers;
// none of the UI namespace needs to be in the stand-alone opennurbs library
#if RHINO_SDK

namespace Rhino
{
  namespace UI
  {
    public interface IDialogService
    {
      string[] ShowMultiListBox(string title, string message, IList<string> items, IList<string> defaults = null);
      object WrapAsIWin32Window(IntPtr handle);
      IntPtr ObjectToWindowHandle(object window, bool useMainRhinoWindowWhenNull);
    }

    class NotImplementedDialogService : IDialogService
    {
      public string[] ShowMultiListBox(string title, string message, IList<string> items, IList<string> defaults = null)
      {
        throw new NotImplementedException();
      }
      public object WrapAsIWin32Window(IntPtr handle)
      {
        throw new NotImplementedException();
      }
      public IntPtr ObjectToWindowHandle(object window, bool useMainRhinoWindowWhenNull)
      {
        throw new NotImplementedException();
      }
    }


    public class GetColorEventArgs : EventArgs
    {
      readonly System.Drawing.Color m_input_color;
      System.Drawing.Color m_selected_color = System.Drawing.Color.Empty;
      readonly bool m_include_button_colors;
      readonly string m_title;
      internal GetColorEventArgs(System.Drawing.Color inputColor, bool includeButtonColors, string title)
      {
        m_input_color = inputColor;
        m_include_button_colors = includeButtonColors;
        m_title = title;
      }

      public System.Drawing.Color InputColor
      {
        get { return m_input_color; }
      }

      public System.Drawing.Color SelectedColor
      {
        get { return m_selected_color; }
        set { m_selected_color = value; }
      }

      public bool IncludeButtonColors
      {
        get { return m_include_button_colors; }
      }

      public string Title
      {
        get { return m_title; }
      }
    }

    public class WaitCursor : IDisposable
    {
      const int IDX_DEFAULT_CURSOR = 0;
      const int IDX_WAIT_CURSOR = 1;

      public WaitCursor()
      {
        Set();
      }

      public void Set() { UnsafeNativeMethods.CRhinoApp_SetCursor(IDX_WAIT_CURSOR); }
      public void Clear() { UnsafeNativeMethods.CRhinoApp_SetCursor(IDX_DEFAULT_CURSOR); }

      public void Dispose()
      {
        Clear();
      }
    }


    public enum ShowMessageResult
    {
      None = 0,
      OK = 1,
      Cancel = 2,
      Abort = 3,
      Retry = 4,
      Ignore = 5,
      Yes = 6,
      No = 7,
    }

    public enum ShowMessageButton
    {
      OK = 0,
      OKCancel = 1,
      AbortRetryIgnore = 2,
      YesNoCancel = 3,
      YesNo = 4,
      RetryCancel = 5,
    }

    public enum ShowMessageIcon
    {
      None = 0,
      Error = 16,
      Hand = 16,
      Stop = 16,
      Question = 32,
      Exclamation = 48,
      Warning = 48,
      Information = 64,
      Asterisk = 64,
    }

    public enum ShowMessageDefaultButton
    {
      Button1 = 0,
      Button2 = 256,
      Button3 = 512,
    }

    [Flags]
    public enum ShowMessageOptions
    {
      None = 0,
      SetForeground = 65536,
      DefaultDesktopOnly = 131072,
      TopMost = 262144,
      RightAlign = 524288,
      RtlReading = 1048576,
      ServiceNotification = 2097152,
    }

    public enum ShowMessageMode
    {
      ApplicationModal = 0,
      SystemModal = 4096,
      TaskModal = 8192,
    }

    public static class Dialogs
    {
      static IDialogService g_service_implementation;
      internal static IDialogService Service
      {
        get
        {
          if (g_service_implementation == null)
          {
            g_service_implementation = Runtime.HostUtils.GetPlatformService<IDialogService>();
            if (g_service_implementation == null)
              g_service_implementation = new NotImplementedDialogService();
          }
          return g_service_implementation;
        }
      }

      public static void ShowAboutDialog(bool forceSimpleDialog)
      {
        UnsafeNativeMethods.CRhinoApp_About(forceSimpleDialog);
      }

      /// <summary>
      /// Creates an ETO ContextMenu from an array of strings. Use the modes array to enable/disable menu items
      /// </summary>
      /// <param name="items"></param>
      /// <param name="screenPoint"></param>
      /// <param name="modes"></param>
      /// <returns></returns>
      public static int ShowContextMenu(IEnumerable<string> items, System.Drawing.Point screenPoint, IEnumerable<int> modes)
      {
        using (var strings = new ClassArrayString())
        {
        var enumerable = items as string[] ?? items.ToArray();
        foreach (string item in enumerable)
        strings.Add(item);
          
          int[] array_modes = null;
          if (modes != null)
          {
            List<int> list_modes = new List<int>(modes);
            array_modes = list_modes.ToArray();
          }

            var mnuselectedItem = RhinoUiServiceLocater.DialogService.ShowPopupMenu(enumerable?.ToArray(), array_modes, screenPoint.X,screenPoint.Y);
            return mnuselectedItem;
        }
      }

      /// <example>
      /// <code source='examples\vbnet\ex_replacecolordialog.vb' lang='vbnet'/>
      /// <code source='examples\cs\ex_replacecolordialog.cs' lang='cs'/>
      /// <code source='examples\py\ex_replacecolordialog.py' lang='py'/>
      /// </example>
      public static void SetCustomColorDialog( EventHandler<GetColorEventArgs> handler)
      {
        g_show_custom_color_dialog = handler;
        UnsafeNativeMethods.RHC_SetReplaceColorDialogCallback(handler == null ? null : g_callback);
      }

      private static EventHandler<GetColorEventArgs> g_show_custom_color_dialog;
      private static readonly ColorDialogCallback g_callback = OnCustomColorDialog;
      internal delegate int ColorDialogCallback(ref int argn, int colorButtons, IntPtr titleAsStringHolder, IntPtr hParent);

      private static int OnCustomColorDialog(ref int argb, int colorButtons, IntPtr titleAsStringHolder, IntPtr hParent)
      {
        int rc = 0;
        if (g_show_custom_color_dialog != null)
        {
          try
          {
            var color = System.Drawing.Color.FromArgb(argb);
            string title = StringHolder.GetString(titleAsStringHolder);
            GetColorEventArgs e = new GetColorEventArgs(color, colorButtons==1, title);

            g_show_custom_color_dialog(null, e);
            if( e.SelectedColor != System.Drawing.Color.Empty )
            {
              argb = e.SelectedColor.ToArgb();
              rc = 1;
            }
          }
          catch (Exception ex)
          {
            Runtime.HostUtils.ExceptionReport(ex);
          }
        }
        return rc;
      }

      // Functions to add
      //[in rhinosdkutilities.h]
      //  RhinoLineTypeDialog
      //  RhinoPrintWidthDialog
      //  RhinoYesNoMessageBox


      /// <summary>
      /// Destroy the splash screen if it is being displayed.
      /// </summary>
      public static void KillSplash()
      {
        if (Rhino.Runtime.HostUtils.RunningInRhino)
          UnsafeNativeMethods.RHC_RhinoKillSplash();
      }

      /// <summary>
      /// Show a windows form that is modal in the sense that this function does not return until
      /// the form is closed, but also allows for interaction with other elements of the Rhino
      /// user interface.
      /// </summary>
      /// <param name="form">
      /// The form must have buttons that are assigned to the "AcceptButton" and "CancelButton".
      /// </param>
      /// <returns>One of the System.Windows.Forms.DialogResult values.</returns>
      [System.ComponentModel.Browsable(false), System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
      [Obsolete]
      public static System.Windows.Forms.DialogResult ShowSemiModal(System.Windows.Forms.Form form)
      {
        if (Runtime.HostUtils.RunningOnWindows)
          form.Load += SemiModalFormLoad;
        return form.ShowDialog(RhinoApp.MainWindow());
      }

      static void SemiModalFormLoad(object sender, EventArgs e)
      {
        if (Runtime.HostUtils.RunningOnWindows)
        {
          IntPtr handle_mainwnd = RhinoApp.MainWindowHandle();
          UnsafeNativeMethods.EnableWindow(handle_mainwnd, true);
        }
      }


      /// <summary>
      /// Display a text dialog similar to the dialog used for the "What" command.
      /// </summary>
      /// <param name="message">Text to display as the message content.</param>
      /// <param name="title">Test to display as the form title.</param>
      public static void ShowTextDialog(string message, string title)
      {
        UnsafeNativeMethods.CRhinoTextOut_ShowDialog(message, title);
      }

      /// <summary>
      /// Same as System.Windows.Forms.MessageBox.Show but using a message box tailored to Rhino.
      /// </summary>
      /// <param name="message"></param>
      /// <param name="title"></param>
      /// <returns></returns>
      [System.ComponentModel.Browsable(false), System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
      [Obsolete("Use ShowMessage")]
      public static System.Windows.Forms.DialogResult ShowMessageBox(string message, string title)
      {
        var rc = ShowMessage(message, title);
        switch(rc)
        {
          case ShowMessageResult.Abort:
            return System.Windows.Forms.DialogResult.Abort;
          case ShowMessageResult.Cancel:
            return System.Windows.Forms.DialogResult.Cancel;
          case ShowMessageResult.Ignore:
            return System.Windows.Forms.DialogResult.Ignore;
          case ShowMessageResult.No:
            return System.Windows.Forms.DialogResult.No;
          case ShowMessageResult.None:
            return System.Windows.Forms.DialogResult.None;
          case ShowMessageResult.OK:
            return System.Windows.Forms.DialogResult.OK;
          case ShowMessageResult.Retry:
            return System.Windows.Forms.DialogResult.Retry;
          case ShowMessageResult.Yes:
            return System.Windows.Forms.DialogResult.Yes;
        }
        return System.Windows.Forms.DialogResult.None;
      }

      /// <summary>
      /// Same as System.Windows.Forms.MessageBox.Show but using a message box tailored to Rhino.
      /// </summary>
      /// <param name="message"></param>
      /// <param name="title"></param>
      /// <param name="buttons"></param>
      /// <param name="icon"></param>
      /// <returns></returns>
      [System.ComponentModel.Browsable(false), System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
      [Obsolete("Use ShowMessage")]
      public static System.Windows.Forms.DialogResult ShowMessageBox(string message, string title, System.Windows.Forms.MessageBoxButtons buttons, System.Windows.Forms.MessageBoxIcon icon)
      {
        ShowMessageButton _buttons = ShowMessageButton.OK;
        switch(buttons)
        {
          case System.Windows.Forms.MessageBoxButtons.AbortRetryIgnore:
            _buttons = ShowMessageButton.AbortRetryIgnore;
            break;
          case System.Windows.Forms.MessageBoxButtons.OK:
            _buttons = ShowMessageButton.OK;
            break;
          case System.Windows.Forms.MessageBoxButtons.OKCancel:
            _buttons = ShowMessageButton.OKCancel;
            break;
          case System.Windows.Forms.MessageBoxButtons.RetryCancel:
            _buttons = ShowMessageButton.RetryCancel;
            break;
          case System.Windows.Forms.MessageBoxButtons.YesNo:
            _buttons = ShowMessageButton.YesNo;
            break;
          case System.Windows.Forms.MessageBoxButtons.YesNoCancel:
            _buttons = ShowMessageButton.YesNoCancel;
            break;
        }
        ShowMessageIcon _icon = ShowMessageIcon.None;
        switch (icon)
        {
          //case System.Windows.Forms.MessageBoxIcon.Asterisk: // same as information
          //  _icon = ShowMessageIcon.Asterisk;
          //  break;
          case System.Windows.Forms.MessageBoxIcon.Error:
            _icon = ShowMessageIcon.Error;
            break;
          //case System.Windows.Forms.MessageBoxIcon.Exclamation: // same as warning
          //  _icon = ShowMessageIcon.Exclamation;
          //  break;
          //case System.Windows.Forms.MessageBoxIcon.Hand: //same as stop
          //  _icon = ShowMessageIcon.Hand;
          //  break;
          case System.Windows.Forms.MessageBoxIcon.Information:
            _icon = ShowMessageIcon.Information;
            break;
          case System.Windows.Forms.MessageBoxIcon.Question:
            _icon = ShowMessageIcon.Question;
            break;
          //case System.Windows.Forms.MessageBoxIcon.Stop: // same as error
          //  _icon = ShowMessageIcon.Stop;
          //  break;
          case System.Windows.Forms.MessageBoxIcon.Warning:
            _icon = ShowMessageIcon.Warning;
            break;
        }
        var rc = ShowMessage(message, title, _buttons, _icon);
        switch (rc)
        {
          case ShowMessageResult.Abort:
            return System.Windows.Forms.DialogResult.Abort;
          case ShowMessageResult.Cancel:
            return System.Windows.Forms.DialogResult.Cancel;
          case ShowMessageResult.Ignore:
            return System.Windows.Forms.DialogResult.Ignore;
          case ShowMessageResult.No:
            return System.Windows.Forms.DialogResult.No;
          case ShowMessageResult.None:
            return System.Windows.Forms.DialogResult.None;
          case ShowMessageResult.OK:
            return System.Windows.Forms.DialogResult.OK;
          case ShowMessageResult.Retry:
            return System.Windows.Forms.DialogResult.Retry;
          case ShowMessageResult.Yes:
            return System.Windows.Forms.DialogResult.Yes;
        }
        return System.Windows.Forms.DialogResult.None;
      }
      
      /// <summary>
      /// Same as System.Windows.Forms.MessageBox.Show but using a message box tailored to Rhino.
      /// </summary>
      /// <param name="message">Message box text content.</param>
      /// <param name="title">Message box title text.</param>
      /// <returns>One of the ShowMessageBoxResult values.</returns>
      public static ShowMessageResult ShowMessage(string message, string title)
      {
        return ShowMessage(null, message, title, ShowMessageButton.OK, ShowMessageIcon.None, ShowMessageDefaultButton.Button1, ShowMessageOptions.None, ShowMessageMode.ApplicationModal);
      }

      /// <summary>
      /// Same as System.Windows.Forms.MessageBox.Show but using a message box tailored to Rhino.
      /// </summary>
      /// <param name="message">Message box text content.</param>
      /// <param name="title">Message box title text.</param>
      /// <param name="buttons">Which buttons to display in the message box.</param>
      /// <param name="icon">Which icon to display in the message box.</param>
      /// <returns>One of the ShowMessageBoxResult values.</returns>
      public static ShowMessageResult ShowMessage(string message, string title, ShowMessageButton buttons, ShowMessageIcon icon)
      {
        return ShowMessage(null, message, title, buttons, icon, ShowMessageDefaultButton.Button1, ShowMessageOptions.None, ShowMessageMode.ApplicationModal);
      }

      /// <summary>
      /// Same as System.Windows.Forms.MessageBox.Show but using a message box tailored to Rhino.
      /// </summary>
      /// <param name="parent">Parent window</param>
      /// <param name="message">Message box text content.</param>
      /// <param name="title">Message box title text.</param>
      /// <param name="buttons">Which buttons to display in the message box.</param>
      /// <param name="icon">Which icon to display in the message box.</param>
      /// <param name="defaultButton">Which button is the default button.</param>
      /// <param name="mode">The modality of the message box.</param>
      /// <param name="options">Additional message box options.</param>
      /// <returns>One of the ShowMessageBoxResult values.</returns>
      public static ShowMessageResult ShowMessage(
        object parent,
        string message,
        string title,
        ShowMessageButton buttons,
        ShowMessageIcon icon,
        ShowMessageDefaultButton defaultButton,
        ShowMessageOptions options,
        ShowMessageMode mode
        )
      {
        // Convert the parent to a Window handle if possible, this will work
        // with Eto, WPF or Win Forms on Windows and be IntPtr.Zero on Mac. the
        // true at the end will cause the main Rhino window handle to get
        // returned on windows when parent is null.
        var parent_handle = Dialogs.Service.ObjectToWindowHandle(parent, true);

        // 26-Mar-2015 Dale Fugier. I've modified this function to allow for more message box flags.
        // This stems from, all things, working on ZooClient on MacRhino. I know that most of these
        // modification do not apply on the Mac. But having more flags available will allow ZooClient
        // to use RhinoCommom's mesage box and, thus, make it 'more' cross platform. 

        // Button
        const uint MB_OK = 0x00000000;
        const uint MB_OKCANCEL = 0x00000001;
        const uint MB_ABORTRETRYIGNORE = 0x00000002;
        const uint MB_YESNOCANCEL = 0x00000003;
        const uint MB_YESNO = 0x00000004;
        const uint MB_RETRYCANCEL = 0x00000005;
        //const uint MB_CANCELTRYCONTINUE = 0x00000006;
        
        // Icon
        const uint MB_ICONHAND = 0x00000010;
        const uint MB_ICONQUESTION = 0x00000020;
        const uint MB_ICONEXCLAMATION = 0x00000030;
        const uint MB_ICONASTERISK = 0x00000040;
        //const uint MB_USERICON = 0x00000080;
        const uint MB_ICONWARNING = MB_ICONEXCLAMATION;
        const uint MB_ICONERROR = MB_ICONHAND;
        const uint MB_ICONINFORMATION = MB_ICONASTERISK;
        const uint MB_ICONSTOP = MB_ICONHAND;
        
        // Default button
        const uint MB_DEFBUTTON1 = 0x00000000;
        const uint MB_DEFBUTTON2 = 0x00000100;
        const uint MB_DEFBUTTON3 = 0x00000200;
        //const uint MB_DEFBUTTON4 = 0x00000300;
        
        // Mode
        const uint MB_APPLMODAL = 0x00000000;
        const uint MB_SYSTEMMODAL = 0x00001000;
        const uint MB_TASKMODAL = 0x00002000;

        // Options
        //const uint MB_HELP = 0x00004000;
        //const uint MB_NOFOCUS = 0x00008000;
        const uint MB_SETFOREGROUND = 0x00010000;
        const uint MB_DEFAULT_DESKTOP_ONLY = 0x00020000;
        const uint MB_TOPMOST = 0x00040000;
        const uint MB_RIGHT = 0x00080000;
        const uint MB_RTLREADING = 0x00100000;
        const uint MB_SERVICE_NOTIFICATION = 0x00200000;

        uint button_flag = MB_OK;
        if (ShowMessageButton.OKCancel == buttons)
          button_flag = MB_OKCANCEL;
        else if (ShowMessageButton.AbortRetryIgnore == buttons)
          button_flag = MB_ABORTRETRYIGNORE;
        else if (ShowMessageButton.YesNoCancel == buttons)
          button_flag = MB_YESNOCANCEL;
        else if (ShowMessageButton.YesNo == buttons)
          button_flag = MB_YESNO;
        else if (ShowMessageButton.RetryCancel == buttons)
          button_flag = MB_RETRYCANCEL;

        uint icon_flag = 0; // No icon
        if (ShowMessageIcon.Asterisk == icon)
          icon_flag = MB_ICONASTERISK;
        else if (ShowMessageIcon.Error == icon)
          icon_flag = MB_ICONERROR;
        else if (ShowMessageIcon.Exclamation == icon)
          icon_flag = MB_ICONEXCLAMATION;
        else if (ShowMessageIcon.Hand == icon)
          icon_flag = MB_ICONHAND;
        else if (ShowMessageIcon.Information == icon)
          icon_flag = MB_ICONINFORMATION;
        else if (ShowMessageIcon.Question == icon)
          icon_flag = MB_ICONQUESTION;
        else if (ShowMessageIcon.Stop == icon)
          icon_flag = MB_ICONSTOP;
        else if (ShowMessageIcon.Warning == icon)
          icon_flag = MB_ICONWARNING;

        uint default_button_flag = MB_DEFBUTTON1;
        if (ShowMessageDefaultButton.Button2 == defaultButton)
          default_button_flag = MB_DEFBUTTON2;
        else if (ShowMessageDefaultButton.Button3 == defaultButton)
          default_button_flag = MB_DEFBUTTON3;

        uint option_flags = 0;
        if (options.HasFlag(ShowMessageOptions.SetForeground))
          option_flags = option_flags | MB_SETFOREGROUND;
        if (options.HasFlag(ShowMessageOptions.DefaultDesktopOnly))
          option_flags = option_flags | MB_DEFAULT_DESKTOP_ONLY;
        if (options.HasFlag(ShowMessageOptions.TopMost))
          option_flags = option_flags | MB_TOPMOST;
        if (options.HasFlag(ShowMessageOptions.RightAlign))
          option_flags = option_flags | MB_RIGHT;
        if (options.HasFlag(ShowMessageOptions.RtlReading))
          option_flags = option_flags | MB_RTLREADING;
        if (options.HasFlag(ShowMessageOptions.ServiceNotification))
          option_flags = option_flags | MB_SERVICE_NOTIFICATION;

        uint mode_flag = MB_APPLMODAL;
        if (ShowMessageMode.SystemModal == mode)
          mode_flag = MB_SYSTEMMODAL;
        else if (ShowMessageMode.TaskModal == mode)
          mode_flag = MB_TASKMODAL;

        uint flags = button_flag | icon_flag | default_button_flag | option_flags | mode_flag;

        int rc = UnsafeNativeMethods.RHC_RhinoMessageBox(parent_handle, message, title, flags);
        return ResultFromInt(rc);
      }
      
      /// <summary>
      /// Display Rhino's color selection dialog.
      /// </summary>
      /// <param name="color">
      /// [in/out] Default color for dialog, and will receive new color if function returns true.
      /// </param>
      /// <returns>true if the color changed. false if the color has not changed or the user pressed cancel.</returns>
      /// <example>
      /// <code source='examples\vbnet\ex_modifylightcolor.vb' lang='vbnet'/>
      /// <code source='examples\cs\ex_modifylightcolor.cs' lang='cs'/>
      /// <code source='examples\py\ex_modifylightcolor.py' lang='py'/>
      /// </example>
      public static bool ShowColorDialog(ref System.Drawing.Color color)
      {
        return ShowColorDialog(ref color, false, null);
      }

      /// <summary>
      /// Display Rhino's color selection dialog.
      /// </summary>
      /// <param name="color">
      /// [in/out] Default color for dialog, and will receive new color if function returns true.
      /// </param>
      /// <param name="includeButtonColors">
      /// Display button face and text options at top of named color list.
      /// </param>
      /// <param name="dialogTitle">The title of the dialog.</param>
      /// <returns>true if the color changed. false if the color has not changed or the user pressed cancel.</returns>
      public static bool ShowColorDialog(ref System.Drawing.Color color, bool includeButtonColors, string dialogTitle)
      {
        int argb = color.ToArgb();
        var rc = UnsafeNativeMethods.RHC_RhinoColorDialog(ref argb, includeButtonColors, dialogTitle);
        if (rc)
          color = System.Drawing.Color.FromArgb(argb);
        return rc;
      }

      /// <summary>
      /// Displays the standard modal color picker dialog for floating point colors.
      /// </summary>
      /// <param name="parent">Parent window for this dialog, should always pass this if calling from a form or user control.</param>
      /// <param name="color">The initial color to set the picker to and also accepts the user's choice.</param>
      /// <param name="allowAlpha">Specifies if the color picker should allow changes to the alpha channel or not.</param>
      /// <returns>true if a color was picked, false if the user canceled the picker dialog.</returns>
      [Obsolete("Use ShowColorDialog(object parent, ref Color4f color, bool allowAlpha)")]
      public static bool ShowColorDialog(System.Windows.Forms.IWin32Window parent, ref Display.Color4f color, bool allowAlpha)
      {
        return ShowColorDialog((object)parent, ref color, allowAlpha);
      }

      /// <summary>
      /// Displays the standard modal color picker dialog for floating point colors.
      /// </summary>
      /// <param name="parent">Parent window for this dialog, should always pass this if calling from a form or user control.</param>
      /// <param name="color">The initial color to set the picker to and also accepts the user's choice.</param>
      /// <param name="allowAlpha">Specifies if the color picker should allow changes to the alpha channel or not.</param>
      /// <returns>true if a color was picked, false if the user canceled the picker dialog.</returns>
      public static bool ShowColorDialog(object parent, ref Display.Color4f color, bool allowAlpha)
      {
        return ShowColorDialog(parent, ref color, allowAlpha, null);
      }

      /// <summary>
      /// May be optionally passed to ShowColorDialog and will get called when
      /// the color value changes in the color dialog.
      /// </summary>
      /// <param name="color"></param>
      public delegate void OnColorChangedEvent(Display.Color4f color);

      /// <summary>
      /// Displays the standard modal color picker dialog for floating point colors.
      /// </summary>
      /// <param name="parent">Parent window for this dialog, should always pass this if calling from a form or user control.</param>
      /// <param name="color">The initial color to set the picker to and also accepts the user's choice.</param>
      /// <param name="allowAlpha">Specifies if the color picker should allow changes to the alpha channel or not.</param>
      /// <param name="colorCallback">
      /// May be optionally passed to ShowColorDialog and will get called when
      /// the color value changes in the color dialog.
      /// </param>
      /// <returns>true if a color was picked, false if the user canceled the picker dialog.</returns>
      public static bool ShowColorDialog(object parent, ref Display.Color4f color, bool allowAlpha, OnColorChangedEvent colorCallback)
      {
        var handle_parent = Dialogs.Service.ObjectToWindowHandle(parent, true);
        var c = Display.Color4f.Empty;
        var rc = (1 == UnsafeNativeMethods.Rdk_Globals_ShowColorPickerEx(handle_parent, color, allowAlpha, ref c, colorCallback));
        if (rc)
          color = c;
        return rc;
      }
      /// <summary>
      /// Displays the standard modal color picker dialog for floating point colors.
      /// </summary>
      /// <param name="color">The initial color to set the picker to and also accepts the user's choice.</param>
      /// <param name="allowAlpha">Specifies if the color picker should allow changes to the alpha channel or not.</param>
      /// <returns>true if a color was picked, false if the user canceled the picker dialog.</returns>
      public static bool ShowColorDialog(ref Display.Color4f color, bool allowAlpha)
      {
        return ShowColorDialog((object)null, ref color, allowAlpha);
      }

      /// <summary>Show the tabbed sun dialog.</summary>
      /// <returns>
      /// Returns true if the user clicked OK, or false if the user cancelled.
      /// </returns>
      public static bool ShowSunDialog(Render.Sun sun)
      {
        return sun.ShowDialogEx();
      }

      /// <summary>
      /// Displays Rhino's single layer selection dialog.
      /// </summary>
      /// <param name="layerIndex">
      /// Initial layer for the dialog, and will receive selected
      /// layer if function returns DialogResult.OK.
      /// </param>
      /// <param name="dialogTitle">The dialog title.</param>
      /// <param name="showNewLayerButton">true if the new layer button will be visible.</param>
      /// <param name="showSetCurrentButton">true if the set current button will be visible.</param>
      /// <param name="initialSetCurrentState">true if the current state will be initially set.</param>
      /// <returns>
      /// True if the dialog was closed with the OK button. False if the dialog was closed with escape.
      /// </returns>
      public static bool ShowSelectLayerDialog(ref int layerIndex, string dialogTitle, bool showNewLayerButton, bool showSetCurrentButton, ref bool initialSetCurrentState)
      {
        return UnsafeNativeMethods.RHC_RhinoSelectLayerDialog(dialogTitle, ref layerIndex, showNewLayerButton, showSetCurrentButton, ref initialSetCurrentState);
      }

      /// <returns>
      /// True if the dialog was closed with the OK button. False if the dialog was closed with escape.
      /// </returns>
      public static bool ShowSelectMultipleLayersDialog(IEnumerable<int> defaultLayerIndices, string dialogTitle, bool showNewLayerButton, out int[] layerIndices)
      {
        using (var array_int = new SimpleArrayInt(defaultLayerIndices))
        {
          IntPtr ptr_array_int = array_int.NonConstPointer();
          bool rc = UnsafeNativeMethods.RHC_RhinoSelectMultipleLayersDialog(dialogTitle, ptr_array_int, true, showNewLayerButton);
          layerIndices = rc ? array_int.ToArray() : new int[0];
          return rc;
        }
      }

      /// <summary>
      /// Displays Rhino's single linetype selection dialog.
      /// </summary>
      /// <param name="linetypeIndex">
      /// Initial linetype for the dialog, and will receive selected
      /// linetype if function returns true.
      /// </param>
      /// <param name="displayByLayer">Displays the "ByLayer" linetype in the list. Defaults to false.</param>
      /// <returns>
      /// True if the dialog was closed with the OK button. False if the dialog was closed with escape.
      /// </returns>
      public static bool ShowSelectLinetypeDialog(ref int linetypeIndex, bool displayByLayer)
      {
        return UnsafeNativeMethods.RHC_RhinoSelectLinetypeDialog(ref linetypeIndex, displayByLayer);
      }

      /// <summary>
      /// Displays Rhino's combo list box.
      /// </summary>
      /// <param name="title">The dialog title.</param>
      /// <param name="message">The dialog message.</param>
      /// <param name="items">A list of items to show.</param>
      /// <returns>
      /// <para>selected item.</para>
      /// <para>null if the user canceled.</para>
      /// </returns>
      public static object ShowComboListBox(string title, string message, System.Collections.IList items)
      {
        return RhinoUiServiceLocater.DialogService.ShowComboListBox(title, message, items);
      }

      public static object ShowListBox(string title, string message, System.Collections.IList items)
      {
        return ShowListBox(title, message, items, null);
      }

      public static object ShowListBox(string title, string message, System.Collections.IList items, object selectedItem)
      {
        return RhinoUiServiceLocater.DialogService.ShowListBox(title, message, items, selectedItem);
      }

      /// <summary>
      /// Displayes Rhino's Linetype list and returns the ID of the selected line type as a string.
      /// </summary>
      /// <param name="title"></param>
      /// <param name="message"></param>
      /// <param name="doc"></param>
      /// <returns></returns>
        public static object ShowLineTypes(string title, string message, RhinoDoc doc)
        {
            return RhinoUiServiceLocater.DialogService.ShowLineTypes(title, message, doc);
        }
      /// <summary>
      /// Displays Rhino's check list box.
      /// </summary>
      /// <param name="title">The dialog title.</param>
      /// <param name="message">The dialog message.</param>
      /// <param name="items">A list of items to show.</param>
      /// <param name="checkState">A list of true/false boolean values.</param>
      /// <returns>An array or boolean values determining if the user checked the corresponding box. On error, null.</returns>
      public static bool[] ShowCheckListBox(string title, string message, System.Collections.IList items, IList<bool> checkState)
      {
        return RhinoUiServiceLocater.DialogService.ShowCheckListBox(title, message, items, checkState);
      }

      /// <summary>Display dialog prompting the user to enter a string.</summary>
      /// <returns>
      /// True if the dialog was closed with the OK button. False if the dialog was closed with escape.
      /// </returns>
      public static bool ShowEditBox(string title, string message, string defaultText, bool multiline, out string text)
      {
        return RhinoUiServiceLocater.DialogService.ShowEditBox(title, message, defaultText, multiline, out text);
      }

      public static bool ShowNumberBox(string title, string message, ref double number)
      {
        return ShowNumberBox(title, message, ref number, double.MinValue, double.MaxValue);
      }

      /// <returns>
      /// True if the dialog was closed with the OK button. False if the dialog was closed with escape.
      /// </returns>
      public static bool ShowNumberBox(string title, string message, ref double number, double minimum, double maximum)
      {
        return RhinoUiServiceLocater.DialogService.ShowNumberBox(title, message, ref number, minimum, maximum);
      }

      public static string[] ShowPropertyListBox(string title, string message, System.Collections.IList items, IList<string> values)
      {
        if (!Runtime.HostUtils.RunningOnWindows)
          throw new NotImplementedException("Not implemented on OS X yet");

        if (null == items || null == values || items.Count < 1 || items.Count != values.Count)
          return null;

        if( Runtime.HostUtils.RunningOnWindows )
        {
          object rs = Runtime.HostUtils.GetRhinoScriptObject();
          if (rs != null)
          {
            string[] _items = new string[items.Count];
            for (int i = 0; i < items.Count; i++)
            {
              object item = items[i];
              if (item == null)
                _items[i] = String.Empty;
              else
                _items[i] = item.ToString();
            }
            string[] _values = new string[values.Count];
            values.CopyTo(_values, 0);
            for (int i = 0; i < values.Count; i++)
            {
              if (string.IsNullOrEmpty(_values[i]))
                _values[i] = " ";
            }
            if (string.IsNullOrEmpty(title))
              title = "Rhino";
            if (string.IsNullOrEmpty(message))
              message = "Items";
            object[] args = new object[] {_items, _values, message, title };
            object invoke_result = rs.GetType().InvokeMember("PropertyListBox", System.Reflection.BindingFlags.InvokeMethod, null, rs, args);
            object[] results = invoke_result as object[];
            if (results != null)
            {
              string[] rc = new string[results.Length];
              for (int i = 0; i < rc.Length; i++)
              {
                object o = results[i];
                if (o != null)
                  rc[i] = o.ToString();
              }
              return rc;
            }
          }
        }
        return null;
      }

      public static string[] ShowMultiListBox(string title, string message, IList<string> items, IList<string> defaults = null)
      {
        return RhinoUiServiceLocater.DialogService.ShowMultiListBox(items, message, title, defaults);
        //OLD MFC VERSION
        //return Service.ShowMultiListBox(title, message, items, defaults);
      }

      internal static ShowMessageResult ResultFromInt(int val)
      {
        const int IDOK = 1;
        const int IDCANCEL = 2;
        const int IDABORT = 3;
        const int IDRETRY = 4;
        const int IDIGNORE = 5;
        const int IDYES = 6;
        const int IDNO = 7;
        var result = ShowMessageResult.None;
        if (IDOK == val)
          result = ShowMessageResult.OK;
        else if (IDCANCEL == val)
          result = ShowMessageResult.Cancel;
        else if (IDABORT == val)
          result = ShowMessageResult.Abort;
        else if (IDRETRY == val)
          result = ShowMessageResult.Retry;
        else if (IDIGNORE == val)
          result = ShowMessageResult.Ignore;
        else if (IDYES == val)
          result = ShowMessageResult.Yes;
        else if (IDNO == val)
          result = ShowMessageResult.No;
        return result;
      }
    }
  }
}

#endif