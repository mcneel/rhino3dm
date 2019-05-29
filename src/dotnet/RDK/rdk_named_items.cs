
#pragma warning disable 1591

using Rhino.Display;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using Rhino.Render;
using Rhino.Runtime.InteropWrappers;
using System.Windows.Forms;

namespace Rhino.Render.DataSources
{
  class NamedItems : IDisposable
  {
    private IntPtr m_cpp;

    public IntPtr CppPointer
    {
      get { return m_cpp; }
    }

    public NamedItems(IntPtr pNamedItems)
    {
      m_cpp = pNamedItems;
    }

    ~NamedItems()
    {
      Dispose(false);
    }

    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    void Dispose(bool bDisposing)
    {
      if (m_cpp != IntPtr.Zero)
      {
        m_cpp = IntPtr.Zero;
      }
    }
    public string[] GetSelection()
    {
      using (var list = new ClassArrayString())
      {
        var ptr_list = list.NonConstPointer();
        UnsafeNativeMethods.NamedItems_GetSelection(m_cpp, ptr_list);
        return list.ToArray();
      }
    }

    public void SetSelection(IEnumerable<string> items)
    {
      using (var strings = new ClassArrayString())
      {
        foreach (string item in items)
          strings.Add(item);
        IntPtr ptr_string_array = strings.NonConstPointer();

        UnsafeNativeMethods.NamedItems_SetSelection(m_cpp, ptr_string_array);
      }
    }

    public enum DisplayModes : uint
    {
      List = UnsafeNativeMethods.NamedItems_DisplayMode.List,
      Thumbnail = UnsafeNativeMethods.NamedItems_DisplayMode.Thumbnail,
    };

    public DisplayModes DisplayMode
    {
      get
      {
        var dm = UnsafeNativeMethods.NamedItems_DisplayModes(m_cpp);
        switch (dm)
        {
          case UnsafeNativeMethods.NamedItems_DisplayMode.List:
            return DisplayModes.List;
          case UnsafeNativeMethods.NamedItems_DisplayMode.Thumbnail:
            return DisplayModes.Thumbnail;
        }
        throw new Exception("Unknown DisplayMode type");
      }
      set
      {
        switch (value)
        {
          case DisplayModes.List:
            UnsafeNativeMethods.NamedItems_SetDisplayModes(m_cpp, UnsafeNativeMethods.NamedItems_DisplayMode.List);
            return;
          case DisplayModes.Thumbnail:
            UnsafeNativeMethods.NamedItems_SetDisplayModes(m_cpp, UnsafeNativeMethods.NamedItems_DisplayMode.Thumbnail);
            return;
        }
      }
    }

    public string NoItemsText()
    {
      string name = "";
      var sh = new Rhino.Runtime.InteropWrappers.StringWrapper(name);
      var p_string = sh.NonConstPointer;

      UnsafeNativeMethods.NamedItems_NoItemsText(CppPointer, p_string);

      return sh.ToString();
    }

    public Bitmap GetTabIcon(System.Drawing.Size s)
    {
      IntPtr pDib = IntPtr.Zero;
      pDib = UnsafeNativeMethods.NamedItems_GetTabIcon(m_cpp, s.Width, s.Height);

      if (pDib == IntPtr.Zero)
        return null;

      var bitmap = Rhino.Runtime.InteropWrappers.RhinoDib.ToBitmap(pDib, true);
      return bitmap;
    }

    public bool ImageSizes(out Size szList, out Size szThumb)
    {
      szList = Size.Empty;
      szThumb = Size.Empty;

      int widthList = 0, heightList = 0, widthThumb = 0, heightThumb = 0;
      if (!UnsafeNativeMethods.NamedItems_ImageSizes(m_cpp, ref widthList, ref heightList, ref widthThumb, ref heightThumb))
        return false;

      szList.Width = widthList;
      szList.Height = heightList;
      szThumb.Width = widthThumb;
      szThumb.Height = heightThumb;

      return true;
    }

    public void SetImageSizes(Size szList, Size szThumb)
    {
      int widthList = szList.Width, heightList = szList.Height, widthThumb = szThumb.Width, heightThumb = szThumb.Height;
      UnsafeNativeMethods.NamedItems_SetImageSizes(m_cpp, ref widthList, ref heightList, ref widthThumb, ref heightThumb);
    }

    public bool Renaming
    {
      get { return UnsafeNativeMethods.NamedItems_RenamingEnabled(m_cpp); }
      set { UnsafeNativeMethods.NamedItems_EnableRenaming(m_cpp, value); }
    }

    public bool Rename(string oldName, string newName)
    {
      return UnsafeNativeMethods.NamedItems_Rename(m_cpp, oldName, newName);
    }

    public bool AllowDragAndDrop
    {
      get { return UnsafeNativeMethods.NamedItem_AllowDragAndDrop(m_cpp); }
    }

    public bool MoveBefore(IEnumerable<string> items, string before)
    {
      var sh = new Rhino.Runtime.InteropWrappers.StringWrapper(before);
      var p_string = (before.Length <= 0) ? IntPtr.Zero : sh.NonConstPointer;

      using (var strings = new ClassArrayString())
      {
        foreach (string item in items)
          strings.Add(item);
        IntPtr ptr_string_array = strings.NonConstPointer();

        return UnsafeNativeMethods.NamedItem_MoveBefore(m_cpp, ptr_string_array, p_string);
      }
    }

    public enum CommandFilters
    {
      ToolButton = UnsafeNativeMethods.NamedItems_CommandFilters.ToolButton,
      MenuItem = UnsafeNativeMethods.NamedItems_CommandFilters.MenuItem,
      CheckBox = UnsafeNativeMethods.NamedItems_CommandFilters.CheckBox,
      All = UnsafeNativeMethods.NamedItems_CommandFilters.All
    };

    public NamedItemCommandArray GetCommands(CommandFilters f, bool bMenuCheckSel = true)
    {
      NamedItemCommandArray cmds = new NamedItemCommandArray();

      switch (f)
      {
        case CommandFilters.ToolButton:
        {
          UnsafeNativeMethods.NamedItems_GetCommands(m_cpp, UnsafeNativeMethods.NamedItems_CommandFilters.ToolButton, cmds.CppPointer, bMenuCheckSel);
          break;
        }
        case CommandFilters.MenuItem:
        {
          UnsafeNativeMethods.NamedItems_GetCommands(m_cpp, UnsafeNativeMethods.NamedItems_CommandFilters.MenuItem, cmds.CppPointer, bMenuCheckSel);
          break;
        }
        case CommandFilters.CheckBox:
        {
          UnsafeNativeMethods.NamedItems_GetCommands(m_cpp, UnsafeNativeMethods.NamedItems_CommandFilters.CheckBox, cmds.CppPointer, bMenuCheckSel);
          break;
        }
        default:
        {
          UnsafeNativeMethods.NamedItems_GetCommands(m_cpp, UnsafeNativeMethods.NamedItems_CommandFilters.All, cmds.CppPointer, bMenuCheckSel);
          break;
        }
      }
      return cmds;
    }

    public NamedItemCommand GetDefaultCommand()
    {
      IntPtr pCmd = IntPtr.Zero;
      pCmd = UnsafeNativeMethods.NamedItems_GetDefaultCommand(m_cpp);

      if (pCmd == IntPtr.Zero)
        return null;

      var nic = new NamedItemCommand(pCmd);
      return nic;
    }

    public NamedItemsIterator Iterator()
    {
      var it = UnsafeNativeMethods.NamedItems_IIterator(m_cpp);
      return new NamedItemsIterator(it);
    }
  }




  class NamedItem : IDisposable
  {
    private IntPtr m_cpp;

    public IntPtr CppPointer
    {
      get { return m_cpp; }
    }

    public NamedItem(IntPtr pNamedItem)
    {
      m_cpp = pNamedItem;
    }

    ~NamedItem()
    {
      Dispose(false);
    }

    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    void Dispose(bool bDisposing)
    {
      if (m_cpp != IntPtr.Zero)
      {
        m_cpp = IntPtr.Zero;
      }
    }
    public String Name
    {
      get
      {
        string name = "";
        var sh = new Rhino.Runtime.InteropWrappers.StringWrapper(name);
        var p_string = sh.NonConstPointer;

        UnsafeNativeMethods.NamedItem_Name(m_cpp, p_string);

        return sh.ToString();
      }
      set
      {
        UnsafeNativeMethods.NamedItem_SetName(m_cpp, value);
      }
    }

    public String DisplayName
    {
      get
      {
        string name = "";
        var sh = new Rhino.Runtime.InteropWrappers.StringWrapper(name);
        var p_string = sh.NonConstPointer;

        UnsafeNativeMethods.NamedItem_DisplayName(m_cpp, p_string);

        return sh.ToString();
      }
    }

    public Bitmap Image()
    {
      IntPtr pDib = IntPtr.Zero;
      pDib = UnsafeNativeMethods.NamedItem_Image(m_cpp);

      if (pDib == IntPtr.Zero)
        return null;

      IntPtr native_bitmap = UnsafeNativeMethods.CRhinoDib_Bitmap(pDib);
      if (IntPtr.Zero == native_bitmap)
        return null;

      var bitmap = Rhino.Runtime.InteropWrappers.RhinoDib.ToBitmap(pDib, true);
      return bitmap;

      //Bitmap b = Rhino.Runtime.InteropWrappers.RhinoDib.ToBitmap(pDib, true);
      //var bmpClone = b.Clone(new Rectangle(0, 0, b.Width, b.Height), System.Drawing.Imaging.PixelFormat.Format32bppRgb);
      //return bmpClone;
    }

  }




  sealed class NamedItemsIterator : IDisposable
  {
    private IntPtr m_cpp;

    public IntPtr CppPointer
    {
      get { return m_cpp; }
    }

    public NamedItemsIterator(IntPtr pNamedItems)
    {
      m_cpp = pNamedItems;
    }

    ~NamedItemsIterator()
    {
      Dispose(false);
    }

    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    void Dispose(bool bDisposing)
    {
      if (m_cpp != IntPtr.Zero)
      {
        m_cpp = IntPtr.Zero;
        UnsafeNativeMethods.NamedItems_DeleteThis(m_cpp);
      }
    }

    public void DeleteThis()
    {
      IntPtr value = IntPtr.Zero;

      if (m_cpp != IntPtr.Zero)
      {
        UnsafeNativeMethods.NamedItems_DeleteThis(m_cpp);
      }
    }

    public NamedItem First()
    {
      if (m_cpp != IntPtr.Zero)
      {
        UnsafeNativeMethods.NamedItems_Reset(m_cpp);
        return Next();
      }

      return null;
    }

    public NamedItem Next()
    {
      if (m_cpp != IntPtr.Zero)
      {
        IntPtr pNamedItem = UnsafeNativeMethods.NamedItems_Next(m_cpp);
        if (IntPtr.Zero != pNamedItem)
        {
          return new NamedItem(pNamedItem);
        }
      }

      return null;
    }
  }




  class NamedItemCommand : NamedItemInterfaceCommand
  {
    public NamedItemCommand(IntPtr pNamedItemsCmd) : base(pNamedItemsCmd)
    {
    }

    public string CheckBoxString()
    {
      string name = "";
      var sh = new Rhino.Runtime.InteropWrappers.StringWrapper(name);
      var p_string = sh.NonConstPointer;

      UnsafeNativeMethods.NamedItemCommand_CheckBoxString(CppPointer, p_string);

      return sh.ToString();
    }

    public enum States
    {
      Off = UnsafeNativeMethods.NamedItemCommand_States.Off,
      On = UnsafeNativeMethods.NamedItemCommand_States.On,
      Varies = UnsafeNativeMethods.NamedItemCommand_States.Varies,
      Unknown = UnsafeNativeMethods.NamedItemCommand_States.Unknown,
    };

    public States GetState(IEnumerable<string> items)
    {
      using (var strings = new ClassArrayString())
      {
        foreach (string item in items)
          strings.Add(item);
        IntPtr ptr_string_array = strings.NonConstPointer();

        var state = UnsafeNativeMethods.NamedItemCommand_GetState(CppPointer, ptr_string_array);
        switch (state)
        {
          case UnsafeNativeMethods.NamedItemCommand_States.Off:
            return States.Off;
          case UnsafeNativeMethods.NamedItemCommand_States.On:
            return States.On;
          case UnsafeNativeMethods.NamedItemCommand_States.Varies:
            return States.Varies;
          case UnsafeNativeMethods.NamedItemCommand_States.Unknown:
            return States.Unknown;
        }
        throw new Exception("Unknown Command State type");
      }
    }

    public bool IsRadio
    {
      get
      {
        return UnsafeNativeMethods.NamedItemCommand_IsRadio(CppPointer);
      }
    }

    public NamedItemCommandArray GetSubCommands()
    {
      NamedItemCommandArray cmds = new NamedItemCommandArray();
      UnsafeNativeMethods.NamedItemCommand_GetSubCommands(CppPointer, cmds.CppPointer);
      return cmds;
    }
  }


  class NamedItemInterfaceCommand : IDisposable
  {
    private IntPtr m_cpp;

    public IntPtr CppPointer
    {
      get { return m_cpp; }
    }

    public NamedItemInterfaceCommand(IntPtr pNamedItemsCmd)
    {
      m_cpp = pNamedItemsCmd;
    }

    ~NamedItemInterfaceCommand()
    {
      Dispose(false);
    }

    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    void Dispose(bool bDisposing)
    {
      if (m_cpp != IntPtr.Zero)
      {
        m_cpp = IntPtr.Zero;
      }
    }

    public uint Id
    {
      get { return UnsafeNativeMethods.NamedItemInterfaceCommand_Id(m_cpp); }
    }

    public Bitmap Image(System.Drawing.Size s)
    {
      IntPtr pDib = IntPtr.Zero;
      pDib = UnsafeNativeMethods.NamedItemInterfaceCommand_Image(m_cpp, s.Width, s.Height);

      if (pDib == IntPtr.Zero)
        return null;

      var bitmap = Rhino.Runtime.InteropWrappers.RhinoDib.ToBitmap(pDib, true);
      return bitmap;
    }

    public string MenuString()
    {
      string name = "";
      var sh = new Rhino.Runtime.InteropWrappers.StringWrapper(name);
      var p_string = sh.NonConstPointer;

      UnsafeNativeMethods.NamedItemInterfaceCommand_MenuString(m_cpp, p_string);

      return sh.ToString();
    }

    public string ToolTipString()
    {
      string name = "";
      var sh = new Rhino.Runtime.InteropWrappers.StringWrapper(name);
      var p_string = sh.NonConstPointer;

      UnsafeNativeMethods.NamedItemInterfaceCommand_ToolTipString(m_cpp, p_string);

      return sh.ToString();
    }

    public string UndoString()
    {
      string name = "";
      var sh = new Rhino.Runtime.InteropWrappers.StringWrapper(name);
      var p_string = sh.NonConstPointer;

      UnsafeNativeMethods.NamedItemInterfaceCommand_UndoString(m_cpp, p_string);

      return sh.ToString();
    }

    public bool Enabled
    { 
      get { return UnsafeNativeMethods.NamedItemInterfaceCommand_IsEnabled(m_cpp); }
    }

    public bool NeedsSelection
    {
      get { return UnsafeNativeMethods.NamedItemInterfaceCommand_NeedsSelection(m_cpp); }
    }

    public bool MenuSeparatorAfter
    {
      get { return UnsafeNativeMethods.NamedItemInterfaceCommand_MenuSeparatorAfter(m_cpp); }
    }

    public bool Execute()
    {
      return UnsafeNativeMethods.NamedItemInterfaceCommand_Execute(m_cpp);
    }
  }




  sealed class NamedItemCommandArray : IDisposable
  {
    private IntPtr m_cpp;

    public IntPtr CppPointer
    {
      get { return m_cpp; }
    }

    public NamedItemCommandArray()
    {
      m_cpp = UnsafeNativeMethods.NamedItemCommandArray_New();
    }

    ~NamedItemCommandArray()
    {
      Dispose(false);
    }

    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    void Dispose(bool bDisposing)
    {
      if (m_cpp != IntPtr.Zero)
      {
        UnsafeNativeMethods.NamedItemCommandArray_Delete(m_cpp);
        m_cpp = IntPtr.Zero;
      }
    }

    public int Count()
    {
      IntPtr value = IntPtr.Zero;

      if (m_cpp != IntPtr.Zero)
      {
        return UnsafeNativeMethods.NamedItemCommandArray_Count(m_cpp);
      }

      return 0;
    }

    public NamedItemCommand At(int index)
    {
      if (m_cpp != IntPtr.Zero)
      {
        IntPtr pContentUI = UnsafeNativeMethods.NamedItemCommandArray_At(m_cpp, index);
        return new NamedItemCommand(pContentUI);
      }

      return null;
    }
    public NamedItemCommand Find(uint id)
    {
      if (m_cpp != IntPtr.Zero)
      {
        IntPtr pContentUI = UnsafeNativeMethods.NamedItemCommandArray_Find(m_cpp, id);
        return new NamedItemCommand(pContentUI);
      }

      return null;
    }
  }



  sealed class NamedItemInterfaceCommandArray : IDisposable
  {
    private IntPtr m_cpp;

    public IntPtr CppPointer
    {
      get { return m_cpp; }
    }

    public NamedItemInterfaceCommandArray()
    {
      m_cpp = UnsafeNativeMethods.NamedItemInterfaceCommandArray_New();
    }

    ~NamedItemInterfaceCommandArray()
    {
      Dispose(false);
    }

    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    void Dispose(bool bDisposing)
    {
      if (m_cpp != IntPtr.Zero)
      {
        UnsafeNativeMethods.NamedItemInterfaceCommandArray_Delete(m_cpp);
        m_cpp = IntPtr.Zero;
      }
    }

    public int Count()
    {
      IntPtr value = IntPtr.Zero;

      if (m_cpp != IntPtr.Zero)
      {
        return UnsafeNativeMethods.NamedItemInterfaceCommandArray_Count(m_cpp);
      }

      return 0;
    }

    public NamedItemInterfaceCommand At(int index)
    {
      if (m_cpp != IntPtr.Zero)
      {
        IntPtr pContentUI = UnsafeNativeMethods.NamedItemInterfaceCommandArray_At(m_cpp, index);
        return new NamedItemInterfaceCommand(pContentUI);
      }

      return null;
    }
    public NamedItemInterfaceCommand Find(uint id)
    {
      if (m_cpp != IntPtr.Zero)
      {
        IntPtr pContentUI = UnsafeNativeMethods.NamedItemInterfaceCommandArray_Find(m_cpp, id);
        return new NamedItemInterfaceCommand(pContentUI);
      }

      return null;
    }
  }





  internal class SnapShotsAnimationSettings : IDisposable
  {
    private IntPtr m_cpp;

    public IntPtr CppPointer
    {
      get { return m_cpp; }
    }

    public SnapShotsAnimationSettings(IntPtr p)
    {
      m_cpp = p;
    }

    ~SnapShotsAnimationSettings()
    {
      Dispose(false);
    }

    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    void Dispose(bool bDisposing)
    {
      if (m_cpp != IntPtr.Zero)
      {
        m_cpp = IntPtr.Zero;
      }
    }

    public int Delay
    {
      get { return UnsafeNativeMethods.SnapShotsAnimationSettings_Delay(m_cpp); }
      set { UnsafeNativeMethods.SnapShotsAnimationSettings_SetDelay(m_cpp, value); }
    }
    
    public bool Animate
    {
      get { return UnsafeNativeMethods.SnapShotsAnimationSettings_Animate(m_cpp); }
      set { UnsafeNativeMethods.SnapShotsAnimationSettings_SetAnimate(m_cpp, value); }
    }

    public bool ConstantSpeed
    {
      get { return UnsafeNativeMethods.SnapShotsAnimationSettings_ConstantSpeed(m_cpp); }
      set { UnsafeNativeMethods.SnapShotsAnimationSettings_SetConstantSpeed(m_cpp, value); }
    }

    public int Frames
    {
      get { return UnsafeNativeMethods.SnapShotsAnimationSettings_Frames(m_cpp); }
      set { UnsafeNativeMethods.SnapShotsAnimationSettings_SetFrames(m_cpp, value); }
    }

    public double UnitsPerFrame
    {
      get { return UnsafeNativeMethods.SnapShotsAnimationSettings_UnitsPerFrame(m_cpp); }
      set { UnsafeNativeMethods.SnapShotsAnimationSettings_SetUnitsPerFrame(m_cpp, value); }
    }
  }




  internal class SnapShotsSlideshowSettings : IDisposable
  {
    private IntPtr m_cpp;

    public IntPtr CppPointer
    {
      get { return m_cpp; }
    }

    public SnapShotsSlideshowSettings(IntPtr p)
    {
      m_cpp = p;
    }

    ~SnapShotsSlideshowSettings()
    {
      Dispose(false);
    }

    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    void Dispose(bool bDisposing)
    {
      if (m_cpp != IntPtr.Zero)
      {
        m_cpp = IntPtr.Zero;
      }
    }

    public int Delay
    {
      get { return UnsafeNativeMethods.SnapShotsSlideshowSettings_Delay(m_cpp); }
      set { UnsafeNativeMethods.SnapShotsSlideshowSettings_SetDelay(m_cpp, value); }
    }

    public bool Fullscreen
    {
      get { return UnsafeNativeMethods.SnapShotsSlideshowSettings_Fullscreen(m_cpp); }
      set { UnsafeNativeMethods.SnapShotsSlideshowSettings_SetFullscreen(m_cpp, value); }
    }

    public bool MaximizedView
    {
      get { return UnsafeNativeMethods.SnapShotsSlideshowSettings_MaximizedView(m_cpp); }
      set { UnsafeNativeMethods.SnapShotsSlideshowSettings_SetMaximizedView(m_cpp, value); }
    }

    public bool RepeatAtEnd
    {
      get { return UnsafeNativeMethods.SnapShotsSlideshowSettings_RepeatAtEnd(m_cpp); }
      set { UnsafeNativeMethods.SnapShotsSlideshowSettings_SetRepeatAtEnd(m_cpp, value); }
    }

    public bool StepAutomatic
    {
      get { return UnsafeNativeMethods.SnapShotsSlideshowSettings_StepAutomatic(m_cpp); }
      set { UnsafeNativeMethods.SnapShotsSlideshowSettings_SetStepAutomatic(m_cpp, value); }
    }

    public bool FloatingView
    {
      get { return UnsafeNativeMethods.SnapShotsSlideshowSettings_FloatingView(m_cpp); }
      set { UnsafeNativeMethods.SnapShotsSlideshowSettings_SetFloatingView(m_cpp, value); }
    }
  }




  internal class SnapShotsSlideshowController : IDisposable
  {
    private IntPtr m_cpp;

    public IntPtr CppPointer
    {
      get { return m_cpp; }
    }

    public SnapShotsSlideshowController(IntPtr p)
    {
      m_cpp = p;
    }

    ~SnapShotsSlideshowController()
    {
      Dispose(false);
    }

    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    void Dispose(bool bDisposing)
    {
      if (m_cpp != IntPtr.Zero)
      {
        m_cpp = IntPtr.Zero;
      }
    }

    public int Delay
    {
      get { return UnsafeNativeMethods.SnapShotsSlideshowController_Delay(m_cpp); }
    }

    public bool StepAutomatic
    {
      get { return UnsafeNativeMethods.SnapShotsSlideshowController_StepAutomatic(m_cpp); }
    }

    public bool RepeatAtEnd
    {
      get { return UnsafeNativeMethods.SnapShotsSlideshowController_StepAutomatic(m_cpp); }
    }

    public bool Animate
    {
      get { return UnsafeNativeMethods.SnapShotsSlideshowController_Animate(m_cpp); }
      set { UnsafeNativeMethods.SnapShotsSlideshowController_SetAnimate(m_cpp, value); }
    }

    public string[] Snapshots()
    {
      using (var list = new ClassArrayString())
      {
        var ptr_list = list.NonConstPointer();
        UnsafeNativeMethods.SnapShotsSlideshowController_Snapshots(m_cpp, ptr_list);
        return list.ToArray();
      }
    }

    public string StartSnapshot()
    {
      string name = "";
      var sh = new Rhino.Runtime.InteropWrappers.StringWrapper(name);
      var p_string = sh.NonConstPointer;

      UnsafeNativeMethods.SnapShotsSlideshowController_StartSnapshot(CppPointer, p_string);

      return sh.ToString();
    }

    public bool Restore(string name, bool bNoAnimation)
    {
      return UnsafeNativeMethods.SnapShotsSlideshowController_Restore(CppPointer, name, bNoAnimation);
    }

    public bool IsCancelled()
    {
      return UnsafeNativeMethods.SnapShotsSlideshowController_IsRestoreCancelled(CppPointer);
    }

    public void Cancel()
    {
      UnsafeNativeMethods.SnapShotsSlideshowController_CancelRestore(CppPointer);
    }
  }




  internal class SnapShotsUnsavedDataController : IDisposable
  {
    private IntPtr m_cpp;

    public IntPtr CppPointer
    {
      get { return m_cpp; }
    }

    public SnapShotsUnsavedDataController(IntPtr p)
    {
      m_cpp = p;
    }

    ~SnapShotsUnsavedDataController()
    {
      Dispose(false);
    }

    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    void Dispose(bool bDisposing)
    {
      if (m_cpp != IntPtr.Zero)
      {
        m_cpp = IntPtr.Zero;
      }
    }

    public bool UnsavedModelWarning
    {
      get { return UnsafeNativeMethods.SnapShotsUnsavedDataController_UnsavedModelWarning(m_cpp); }
      set { UnsafeNativeMethods.SnapShotsUnsavedDataController_SetUnsavedModelWarning(m_cpp, value); }
    }

    public bool Report(ref string s)
    {
      string name = "";
      var sh = new Rhino.Runtime.InteropWrappers.StringWrapper(name);
      var p_string = sh.NonConstPointer;

      var ret = UnsafeNativeMethods.SnapShotsUnsavedDataController_Report(CppPointer, p_string);
      if (ret)
      {
        s = sh.ToString();
      }
      return ret;
    }
  }
}
