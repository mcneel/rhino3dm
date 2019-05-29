#pragma warning disable 1591
using System;
using System.Collections.Generic;
using System.Drawing;

// none of the UI name-space needs to be in the stand-alone opennurbs library
#if RHINO_SDK

namespace Rhino.UI
{
  /// <summary>
  /// A single instance of this class is created in Rhino.UI.Runtime.Initialization.Initialize
  /// located in  ...\src4\DotNetSDK\Rhino.UI\Runtime\Initialization.cs
  /// </summary>
  static class RhinoUiServiceLocater
  {
    /// <summary>
    /// Get the Rhino.UI assemblies IRhinoUiDialogService or return NotImplementedRhinoUiDialogService
    /// </summary>
    internal static IRhinoUiDialogService DialogService  =>
      g_dialog_service
      ?? (g_dialog_service = Runtime.HostUtils.GetPlatformService<IRhinoUiDialogService>("Rhino.UI.dll") ?? new NotImplementedRhinoUiDialogService());
    private static IRhinoUiDialogService g_dialog_service;
  }

  /// <summary>
  /// Used by Rhino.UI.Dialogs to access generic Eto dialogs from Rhino Common 
  /// </summary>
  public interface IRhinoUiDialogService
  {
    object ShowComboListBox(string title, string message, System.Collections.IList items);
    object ShowListBox(string title, string message, System.Collections.IList items, object selectedItem);
    bool[] ShowCheckListBox(string title, string message, System.Collections.IList items, IList<bool> checkState);
    bool ShowEditBox(string title, string message, string defaultText, bool multiline, out string text);
    bool ShowNumberBox(string title, string message, ref double number, double minimum, double maximum);
    int ShowPopupMenu(string[] arrItems, int[] arrModes, int? screenPointX,int? screenPointY);
    object ShowLineTypes(string title,string message,RhinoDoc doc);

    string[] ShowMultiListBox(IList<string> items, string message, string title,
    IList<string> defaults);
  }

    /// <summary>
    /// This should not be needed if the Rhino.UI assembly is loaded and initialized properly
    /// </summary>
    class NotImplementedRhinoUiDialogService : IRhinoUiDialogService
    {
    public object ShowLineTypes(string title, string message, RhinoDoc doc)
    {
        throw new NotImplementedException();
    }

    public object ShowComboListBox(string title, string message, System.Collections.IList items)
    {
      throw new NotImplementedException();
    }
    
    public object ShowListBox(string title, string message, System.Collections.IList items, object selectedItem)
    {
      throw new NotImplementedException();
    }

    public bool[] ShowCheckListBox(string title, string message, System.Collections.IList items,
      IList<bool> checkState)
    {
      throw new NotImplementedException();
    }

    public bool ShowEditBox(string title, string message, string defaultText, bool multiline, out string text)
    {
      throw new NotImplementedException();
    }

    public bool ShowNumberBox(string title, string message, ref double number, double minimum, double maximum)
    {
      throw new NotImplementedException();
    }

    public int ShowPopupMenu(string[] arrItems, int[] arrModes, int? screenPointX, int? screenPointY)
    {
        throw new NotImplementedException();
    }

    public string[] ShowMultiListBox(IList<string> items, string message, string title, IList<string> defaults)
    {
      throw new NotImplementedException();
    }
  }
}

#endif