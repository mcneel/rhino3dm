using System.Reflection;
#pragma warning disable 1591
using System;

#if RHINO_SDK

namespace Rhino.UI
{
  [Flags]
  public enum MouseButton
  {
    None = 0,
    Left = 1,
    Right = 2,
    Middle = 4,
  }

  [Flags]
  public enum ModifierKey
  {
    None = 0,
    Control = 1,
    Shift = 2
  }


  public class MouseCallbackEventArgs : System.ComponentModel.CancelEventArgs
  {
    private readonly uint m_view_serial_number;
    private Display.RhinoView m_view;
    private readonly uint m_flags;
    private readonly System.Drawing.Point m_point;

    static uint MK_LBUTTON = 0;//0x0001;
    static uint MK_RBUTTON = 0;//0x0002;
    static uint MK_SHIFT = 0;//0x0004;
    static uint MK_CONTROL = 0;//0x0008;
    static uint MK_MBUTTON = 0;//0x0010;

    internal MouseCallbackEventArgs(uint viewSerialNumber, uint flags, int x, int y)
    {
      if( MK_LBUTTON == 0 && MK_RBUTTON == 0)
      {
        UnsafeNativeMethods.CRhCmnMouseCallback_GetConstants(ref MK_LBUTTON, ref MK_RBUTTON, ref MK_SHIFT, ref MK_CONTROL, ref MK_MBUTTON);
      }
      m_view_serial_number = viewSerialNumber;
      m_flags = flags;
      m_point = new System.Drawing.Point(x, y);
    }

    public Display.RhinoView View
    {
      get { return m_view ?? (m_view = Display.RhinoView.FromRuntimeSerialNumber(m_view_serial_number)); }
    }

    public MouseButton MouseButton
    {
      get
      {
        if ((m_flags & MK_LBUTTON) == MK_LBUTTON)
          return MouseButton.Left;
        if ((m_flags & MK_RBUTTON) == MK_RBUTTON)
          return MouseButton.Right;
        if ((m_flags & MK_MBUTTON) == MK_MBUTTON)
          return MouseButton.Middle;

        return MouseButton.None;
      }
    }

    [System.ComponentModel.Browsable(false), System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public System.Windows.Forms.MouseButtons Button
    {
      get
      {
        var btn = MouseButton;
        if (btn == UI.MouseButton.Left)
          return System.Windows.Forms.MouseButtons.Left;
        if (btn == UI.MouseButton.Right)
          return System.Windows.Forms.MouseButtons.Right;
        if (btn == UI.MouseButton.Middle)
          return System.Windows.Forms.MouseButtons.Middle;
        return System.Windows.Forms.MouseButtons.None;
      }
    }

    public bool ShiftKeyDown
    {
      get { return (m_flags & MK_SHIFT) == MK_SHIFT; }
    }

    public bool CtrlKeyDown
    {
      get { return (m_flags & MK_CONTROL) == MK_CONTROL; }
    }

    public System.Drawing.Point ViewportPoint
    {
      get { return m_point; }
    }

  }

  /// <summary>Used for intercepting mouse events in the Rhino views.</summary>
  public abstract class MouseCallback
  {
    /// <summary>
    /// Called at the beginning of handling of a mouse move event in Rhino.
    /// If you don't want the default Rhino functionality to be run, then set
    /// Cancel to true on the passed in event args.
    /// Base class implementation of this function does nothing.
    /// </summary>
    /// <param name="e"></param>
    protected virtual void OnMouseMove(MouseCallbackEventArgs e) { }

    /// <summary>
    /// Called at the end of handling of a mouse move event in Rhino.
    /// All of the default Rhino mouse move functionality has already been
    /// executed unless a MouseCallback has set Cancel to true for the event args.
    /// You can tell if this is the case by inpecting the Cancel property in
    /// the event args parameter.
    /// Base class implementation of this function does nothing.
    /// </summary>
    /// <param name="e"></param>
    protected virtual void OnEndMouseMove(MouseCallbackEventArgs e) { }

    /// <summary>
    /// Called at the beginning of handling of a mouse down event in Rhino.
    /// If you don't want the default Rhino functionality to be run, then set
    /// Cancel to true on the passed in event args
    /// Base class implementation of this function does nothing
    /// </summary>
    /// <param name="e"></param>
    protected virtual void OnMouseDown(MouseCallbackEventArgs e) { }

    /// <summary>
    /// Called at the end of handling of a mouse down event in Rhino.
    /// All of the default Rhino mouse down functionality has already been
    /// executed unless a MouseCallback has set Cancel to true for the event args.
    /// You can tell if this is the case by inpecting the Cancel property in
    /// the event args parameter.
    /// Base class implementation of this function does nothing
    /// </summary>
    /// <param name="e"></param>
    protected virtual void OnEndMouseDown(MouseCallbackEventArgs e) { }

    /// <summary>
    /// Called at the beginning of handling of a mouse up event in Rhino.
    /// If you don't want the default Rhino functionality to be run, then set
    /// Cancel to true on the passed in event args
    /// Base class implementation of this function does nothing
    /// </summary>
    /// <param name="e"></param>
    protected virtual void OnMouseUp(MouseCallbackEventArgs e) { }

    /// <summary>
    /// Called at the end of handling of a mouse up event in Rhino.
    /// All of the default Rhino mouse down functionality has already been
    /// executed unless a MouseCallback has set Cancel to true for the event args.
    /// You can tell if this is the case by inpecting the Cancel property in
    /// the event args parameter.
    /// Base class implementation of this function does nothing
    /// </summary>
    /// <param name="e"></param>
    protected virtual void OnEndMouseUp(MouseCallbackEventArgs e) { }

    
    protected virtual void OnMouseDoubleClick(MouseCallbackEventArgs e) { }

    protected virtual void OnMouseEnter(MouseCallbackEventArgs e) { }
    protected virtual void OnMouseLeave(MouseCallbackEventArgs e) { }
    protected virtual void OnMouseHover(MouseCallbackEventArgs e) { }
    
    bool m_enabled;
    public bool Enabled
    {
      get
      {
        // see if this class is in the enabled list
        return m_enabled;
      }
      set
      {
        m_enabled = value;

        if (m_enabled)
        {
          Type t = GetType();

          if (IsOverridden(t, "OnMouseMove"))
            Display.RhinoView.BeginMouseMove += BeginMouseMoveHandler;
          if (IsOverridden(t, "OnEndMouseMove"))
            Display.RhinoView.EndMouseMove += EndMouseMoveHandler;

          if (IsOverridden(t, "OnMouseDown"))
            Display.RhinoView.BeginMouseDown += BeginMouseDownHandler;
          if (IsOverridden(t, "OnEndMouseDown"))
            Display.RhinoView.EndMouseDown += EndMouseDownHandler;

          if (IsOverridden(t, "OnMouseUp"))
            Display.RhinoView.BeginMouseUp += BeginMouseUpHandler;
          if (IsOverridden(t, "OnEndMouseUp"))
            Display.RhinoView.EndMouseUp += EndMouseUpHandler;

          if (IsOverridden(t, "OnMouseDoubleClick"))
            Display.RhinoView.BeginMouseDoubleClick += BeginMouseDblClkHandler;


          if (IsOverridden(t, "OnMouseEnter"))
            Display.RhinoView.MouseEnter += MouseEnterHandler;
          if (IsOverridden(t, "OnMouseHover"))
            Display.RhinoView.MouseHover += MouseHoverHandler;
          if (IsOverridden(t, "OnMouseLeave"))
            Display.RhinoView.MouseLeave += MouseLeaveHandler;
        }
        else
        {
          Display.RhinoView.BeginMouseMove -= BeginMouseMoveHandler;
          Display.RhinoView.EndMouseMove -= EndMouseMoveHandler;
          Display.RhinoView.BeginMouseDown -= BeginMouseDownHandler;
          Display.RhinoView.EndMouseDown -= EndMouseDownHandler;
          Display.RhinoView.BeginMouseUp -= BeginMouseUpHandler;
          Display.RhinoView.EndMouseUp -= EndMouseUpHandler;
          Display.RhinoView.BeginMouseDoubleClick -= BeginMouseDblClkHandler;
          Display.RhinoView.MouseEnter -= MouseEnterHandler;
          Display.RhinoView.MouseHover -= MouseHoverHandler;
          Display.RhinoView.MouseLeave -= MouseLeaveHandler;
        }
      }
    }

    static bool IsOverridden(Type derivedType, string methodName)
    {
      Type base_type = typeof(MouseCallback);
      // The virtual functions are protected, so we need to call the overload
      // of GetMethod that takes some binding flags
      const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public;

      MethodInfo mi = derivedType.GetMethod(methodName, flags);
      return (mi.DeclaringType != base_type);
    }

    void BeginMouseMoveHandler(object sender, MouseCallbackEventArgs e){ OnMouseMove(e); }
    void EndMouseMoveHandler(object sender, MouseCallbackEventArgs e) { OnEndMouseMove(e); }
    void BeginMouseDownHandler(object sender, MouseCallbackEventArgs e) { OnMouseDown(e); }
    void EndMouseDownHandler(object sender, MouseCallbackEventArgs e) { OnEndMouseDown(e); }
    void BeginMouseUpHandler(object sender, MouseCallbackEventArgs e) { OnMouseUp(e); }
    void EndMouseUpHandler(object sender, MouseCallbackEventArgs e) { OnEndMouseUp(e); }
    void BeginMouseDblClkHandler(object sender, MouseCallbackEventArgs e) { OnMouseDoubleClick(e); }

    void MouseEnterHandler(object sender, MouseCallbackEventArgs e) { OnMouseEnter(e); }
    void MouseHoverHandler(object sender, MouseCallbackEventArgs e) { OnMouseHover(e); }
    void MouseLeaveHandler(object sender, MouseCallbackEventArgs e) { OnMouseLeave(e); }
  }
}

#endif
