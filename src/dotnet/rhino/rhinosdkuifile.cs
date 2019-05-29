using System.Drawing;
#pragma warning disable 1591
using System;
using System.Collections.Generic;
using Rhino.Runtime.InteropWrappers;

// none of the UI namespace needs to be in the stand-alone opennurbs library
#if RHINO_SDK

namespace Rhino
{
  namespace UI
  {
    public sealed class ToolbarFile
    {
      readonly Guid m_id;
      internal ToolbarFile(Guid id)
      {
        m_id = id;
      }

      public Guid Id { get { return m_id; } }

      /// <summary>Full path to this file on disk</summary>
      public string Path
      {
        get
        {
          using (var sh = new StringHolder())
          {
            IntPtr ptr_string = sh.NonConstPointer();
            UnsafeNativeMethods.CRhinoUiFile_FileName(m_id, ptr_string, false);
            return sh.ToString();
          }
        }
      }

      public string Name
      {
        get
        {
          using (var sh = new StringHolder())
          {
            IntPtr ptr_string = sh.NonConstPointer();
            UnsafeNativeMethods.CRhinoUiFile_FileName(m_id, ptr_string, true);
            return sh.ToString();
          }
        }
      }

      public bool Close(bool prompt)
      {
        if (prompt)
        {
          var rc = Dialogs.ShowMessage("Close toolbar file?", "Close",
            ShowMessageButton.YesNo,
            ShowMessageIcon.Question);
          if (rc == ShowMessageResult.No)
            return false;
        }
        return UnsafeNativeMethods.CRhinoUiFile_FileClose(m_id);
      }

      public bool Save()
      {
        return UnsafeNativeMethods.CRhinoUiFile_FileSave(m_id);
      }

      public bool SaveAs(string path)
      {
        return UnsafeNativeMethods.CRhinoUiFile_FileSaveAs(m_id, path);
      }

      public int GroupCount
      {
        get { return UnsafeNativeMethods.CRhinoUiFile_GroupCount(m_id); }
      }

      public int ToolbarCount
      {
        get { return UnsafeNativeMethods.CRhinoUiFile_ToolbarCount(m_id); }
      }

      public Toolbar GetToolbar(int index)
      {
        Guid id = UnsafeNativeMethods.CRhinoUiFile_ToolBarID(m_id, index);
        if (id == Guid.Empty)
          return null;
        return new Toolbar(this, id);
      }

      public ToolbarGroup GetGroup(int index)
      {
        Guid id = UnsafeNativeMethods.CRhinoUiFile_GroupID(m_id, index);
        if (id == Guid.Empty)
          return null;
        return new ToolbarGroup(this, id);
      }

      public ToolbarGroup GetGroup(string name)
      {
        int count = GroupCount;
        for (int i = 0; i < count; i++)
        {
          ToolbarGroup group = GetGroup(i);
          if (string.Compare(group.Name, name) == 0)
            return group;
        }
        return null;
      }
    }

    public sealed class Toolbar
    {
      readonly ToolbarFile m_parent;
      readonly Guid m_id;

      internal Toolbar(ToolbarFile parent, Guid id)
      {
        m_parent = parent;
        m_id = id;
      }

      public Guid Id
      {
        get { return m_id; }
      }

      public string Name
      {
        get
        {
          using (var sh = new StringHolder())
          {
            IntPtr ptr_string = sh.NonConstPointer();
            UnsafeNativeMethods.CRhinoUiFile_ToolBarName(m_parent.Id, m_id, ptr_string);
            return sh.ToString();
          }
        }
      }

      public static Size BitmapSize
      {
        get
        {
          var width = 0;
          var height = 0;
          UnsafeNativeMethods.CRhinoUiFile_ToolBarBitmapSize(ref width, ref height);
          return new Size(width, height);
        }
        set
        {
          UnsafeNativeMethods.CRhinoUiFile_SetToolBarBitmapSize(value.Width, value.Height);
        }
      }

      public static Size TabSize
      {
        get
        {
          var width = 0;
          var height = 0;
          UnsafeNativeMethods.CRhinoUiFile_TabBitmapSize(ref width, ref height);
          return new Size(width, height);
        }
        set
        {
          UnsafeNativeMethods.CRhinoUiFile_SetTabBitmapSize(value.Width, value.Height);
        }
      }
    }

    public sealed class ToolbarGroup
    {
      readonly ToolbarFile m_parent;
      readonly Guid m_id;

      internal ToolbarGroup(ToolbarFile parent, Guid id)
      {
        m_parent = parent;
        m_id = id;
      }

      public Guid Id
      {
        get { return m_id; }
      }

      public string Name
      {
        get
        {
          using (var sh = new StringHolder())
          {
            IntPtr ptr_string = sh.NonConstPointer();
            UnsafeNativeMethods.CRhinoUiFile_GroupName(m_parent.Id, m_id, ptr_string);
            return sh.ToString();
          }
        }
      }

      public bool Visible
      {
        get
        {
          return UnsafeNativeMethods.CRhinoUiFile_GroupIsVisible(m_parent.Id, m_id);
        }
        set
        {
          UnsafeNativeMethods.CRhinoUiFile_GroupShow(m_parent.Id, m_id, value);
        }
      }

      public bool IsDocked
      {
        get
        {
          return UnsafeNativeMethods.CRhinoUiFile_GroupIsDocked(m_parent.Id, m_id);
        }
      }
    }

    public sealed class ToolbarFileCollection : IEnumerable<ToolbarFile>
    {
      internal ToolbarFileCollection() { }

      /// <summary>
      /// Number of open toolbar files
      /// </summary>
      public int Count
      {
        get{ return UnsafeNativeMethods.CRhinoUiFile_FileCount(); }
      }

      public ToolbarFile this[int index]
      {
        get
        {
          Guid id = UnsafeNativeMethods.CRhinoUiFile_FileID(index);
          if( Guid.Empty==id )
            throw new IndexOutOfRangeException();
          return new ToolbarFile(id);
        }
      }

      public ToolbarFile FindByName(string name, bool ignoreCase)
      {
        foreach (ToolbarFile tb in this)
        {
          if (string.Compare(tb.Name, name, ignoreCase) == 0)
            return tb;
        }
        return null;
      }

      public ToolbarFile FindByPath(string path)
      {
        foreach (ToolbarFile tb in this)
        {
          if (string.Compare(tb.Path, path, StringComparison.InvariantCultureIgnoreCase) == 0)
            return tb;
        }
        return null;
      }

      public ToolbarFile Open(string path)
      {
        if( !System.IO.File.Exists(path) )
          throw new System.IO.FileNotFoundException();
        Guid id = UnsafeNativeMethods.CRhinoUiFile_FileOpen(path);
        if( id==Guid.Empty )
          return null;
        return new ToolbarFile(id);
      }

      public IEnumerator<ToolbarFile> GetEnumerator()
      {
        int count = Count;
        for( int i=0; i<count; i++ ) yield return this[i];
      }

      System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
      {
        int count = Count;
        for (int i = 0; i < count; i++) yield return this[i];
      }

      public static bool SidebarIsVisible
      {
        get { return UnsafeNativeMethods.CRhinoUiFile_SidebarIsVisible(false); }
        set { UnsafeNativeMethods.CRhinoUiFile_ShowSidebar(false, value); }
      }

      public static bool MruSidebarIsVisible
      {
        get { return UnsafeNativeMethods.CRhinoUiFile_SidebarIsVisible(true); }
        set { UnsafeNativeMethods.CRhinoUiFile_ShowSidebar(true, value); }
      }
    }


    // RUI Menu specific
    [CLSCompliant(false)]
    public class RuiUpdateUi : EventArgs
    {
      ///<summary>Return true if initialize correctly</summary>
      ///<returns> true if initialize correctly</returns>
      bool IsValid
      {
        get
        {
          var result = UnsafeNativeMethods.CRuiUpdateUi_GetBool(m_ptr, (int)UnsafeNativeMethods.CRuiUpdateBoolConsts.IsValid, false);
          return result;
        }
      }
      ///<summary>Set to true to enable menu item or false to disable menu item</summary>
      public bool Enabled
      {
        set { UnsafeNativeMethods.CRuiUpdateUi_SetBool(m_ptr, (int)UnsafeNativeMethods.CRuiUpdateBoolConsts.Enabled, value); }
        get
        {
          var result = UnsafeNativeMethods.CRuiUpdateUi_GetBool(m_ptr, (int)UnsafeNativeMethods.CRuiUpdateBoolConsts.Enabled, false);
          return result;
        }
      }
      ///<summary>Set to true to enable menu item or false to check menu item</summary>
      public bool Checked
      {
        set { UnsafeNativeMethods.CRuiUpdateUi_SetBool(m_ptr, (int)UnsafeNativeMethods.CRuiUpdateBoolConsts.Checked, value); }
        get
        {
          var result = UnsafeNativeMethods.CRuiUpdateUi_GetBool(m_ptr, (int)UnsafeNativeMethods.CRuiUpdateBoolConsts.Checked, false);
          return result;
        }
      }
      ///<summary>Set to true to enable menu item or false to check menu item</summary>
      public bool RadioChecked
      {
        set { UnsafeNativeMethods.CRuiUpdateUi_SetBool(m_ptr, (int)UnsafeNativeMethods.CRuiUpdateBoolConsts.RadioChecked, value); }
        get
        { return UnsafeNativeMethods.CRuiUpdateUi_GetBool(m_ptr, (int)UnsafeNativeMethods.CRuiUpdateBoolConsts.RadioChecked, false); }
      }
      ///<summary>Menu item text</summary>
      public string Text
      {
        set { UnsafeNativeMethods.CRuiUpdateUi_SetString(m_ptr, (int)UnsafeNativeMethods.CRuiUpdateStringConsts.Text, value); }
        get
        {
          using (var s = new StringHolder())
          {
            var string_pointer = s.ConstPointer();
            UnsafeNativeMethods.CRuiUpdateUi_GetString(m_ptr, (int)UnsafeNativeMethods.CRuiUpdateBoolConsts.RadioChecked, string_pointer);
            var result = string_pointer.ToString();
            return result;
          }
        }
      }
      ///<summary>Id of the RUI file that owns this menu item</summary>
      public Guid FileId
      {
        get { return UnsafeNativeMethods.CRuiUpdateUi_GetGuid(m_ptr, (int)UnsafeNativeMethods.CRuiUpdateGetUuidConsts.FileId); }
      }
      ///<summary>Id of the menu that owns this menu item</summary>
      public Guid MenuId
      {
        get { return UnsafeNativeMethods.CRuiUpdateUi_GetGuid(m_ptr, (int)UnsafeNativeMethods.CRuiUpdateGetUuidConsts.MenuId); }
      }
      ///<summary>Id of the menu item that owns this menu item</summary>
      public Guid MenuItemId
      {
        get { return UnsafeNativeMethods.CRuiUpdateUi_GetGuid(m_ptr, (int)UnsafeNativeMethods.CRuiUpdateGetUuidConsts.MenuItemId); }
      }
      ///<summary>Windows menu handle of menu that contains this item</summary>
      public IntPtr MenuHandle
      {
        get { return UnsafeNativeMethods.CRuiUpdateUi_GetMenuHandle(m_ptr); }
      }
      ///<summary>Zero based index of item in the Windows menu</summary>
      public int MenuIndex
      {
        get { return UnsafeNativeMethods.CRuiUpdateUi_MenuItemIndex(m_ptr); }
      }
      ///<summary>Windows menu item ID</summary>
      [CLSCompliant(false)]
      public uint WindowsMenuItemId
      {
        get { return UnsafeNativeMethods.CRuiUpdateUi_MenuItemWinID(m_ptr); }
      }

      ///<summary>Menu item update delegate</summary>
      ///<param name="sender">Unused, null</param>
      ///<param name="cmdui">Used to identify the menu item and modify its state</param>
      ///<returns></returns>
      public delegate void UpdateMenuItemEventHandler(object sender, RuiUpdateUi cmdui);
      static readonly RuiOnUpdateMenuItems g_rui_on_update_menu_items = new RuiOnUpdateMenuItems();

      /// <summary>Register menu item update delegate</summary>
      /// <param name="file">Menu file Id</param>
      /// <param name="menu">Menu Id</param>
      /// <param name="item">Menu item Id</param>
      /// <param name="callBack"></param>
      /// <returns>true if Registered otherwise false</returns>
      static public bool RegisterMenuItem(Guid file, Guid menu, Guid item, UpdateMenuItemEventHandler callBack)
      {
        SetHooks();
        return g_rui_on_update_menu_items.RegisterMenuItem(file, menu, item, callBack);
      }

      /// <summary>Register menu item update delegate</summary>
      /// <param name="fileId">Menu file Id</param>
      /// <param name="menuId">Menu Id</param>
      /// <param name="itemId">Menu item Id</param>
      /// <param name="callBack"></param>
      /// <returns>true if Registered otherwise false</returns>
      static public bool RegisterMenuItem(string fileId, string menuId, string itemId, UpdateMenuItemEventHandler callBack)
      {
        SetHooks();
        return g_rui_on_update_menu_items.RegisterMenuItem(fileId, menuId, itemId, callBack);
      }
      // For internal use only!
      internal RuiUpdateUi(IntPtr ptr)
      {
        m_ptr = ptr;
      }

      static private void SetHooks()
      {
        if (g_hooks_set) return;
        UnsafeNativeMethods.CRuiOnUpdateMenuItems_SetHooks(RuiOnUpdateMenuItems.OnUpdateMenuItemHook);
        g_hooks_set = true;
      }
      private static bool g_hooks_set;
      private readonly IntPtr m_ptr;
      static internal readonly List<UpdateMenuItemEventHandler> g_update_menu_item_delegates = new List<UpdateMenuItemEventHandler>();
    }

    class RuiOnUpdateMenuItems
    {
      public bool RegisterMenuItem(Guid idFile, Guid idMenu, Guid idItem, RuiUpdateUi.UpdateMenuItemEventHandler callBack)
      {
        var count = UnsafeNativeMethods.CRuiOnUpdateMenuItems_GetMenuItemsCount();
        if (!UnsafeNativeMethods.CRuiOnUpdateMenuItems_RegisterMenuItem(idFile, idMenu, idItem) || UnsafeNativeMethods.CRuiOnUpdateMenuItems_GetMenuItemsCount() <= count)
          return false;
        RuiUpdateUi.g_update_menu_item_delegates.Add(callBack);
        return true;
      }
      public bool RegisterMenuItem(string idFile, string idMenu, string idItem, RuiUpdateUi.UpdateMenuItemEventHandler callBack)
      {
        var count = UnsafeNativeMethods.CRuiOnUpdateMenuItems_GetMenuItemsCount();
        if (!UnsafeNativeMethods.CRuiOnUpdateMenuItems_RegisterMenuItemString(idFile, idMenu, idItem) || UnsafeNativeMethods.CRuiOnUpdateMenuItems_GetMenuItemsCount() <= count)
          return false;
        RuiUpdateUi.g_update_menu_item_delegates.Add(callBack);
        return true;
      }

      internal delegate void OnUpdateMenuItemCallback(int index, IntPtr udateUiPointer);
      internal static OnUpdateMenuItemCallback OnUpdateMenuItemHook = OnUpdateMenuItem;
      static void OnUpdateMenuItem(int index, IntPtr udateUiPointer)
      {
        if (index < 0 || index >= RuiUpdateUi.g_update_menu_item_delegates.Count)
          return;
        var cmdui = new RuiUpdateUi(udateUiPointer);
        RuiUpdateUi.g_update_menu_item_delegates[index].Invoke(null, cmdui);
      }
    }
  }
}
#endif