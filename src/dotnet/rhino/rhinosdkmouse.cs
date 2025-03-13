using System.Reflection;
#pragma warning disable 1591
using System;

#if RHINO_SDK

namespace Rhino.UI
{
  /// <summary>
  /// Keyboard key recognized by shortcuts
  /// </summary>
  public enum KeyboardKey : int
  {
    /// <summary>No key</summary>
    None = 0,
    /// <summary>Tab key</summary>
    Tab = 0x09,
    /// <summary>PageUp key</summary>
    PageUp = 0x21,
    /// <summary>PageDown key</summary>
    PageDown = 0x22,
    /// <summary>End key</summary>
    End = 0x23,
    /// <summary>Home key</summary>
    Home = 0x24,
    /// <summary>0 key</summary>
    Num0 = 48,
    /// <summary>1 key</summary>
    Num1 = 49,
    /// <summary>2 key</summary>
    Num2 = 50,
    /// <summary>3 key</summary>
    Num3 = 51,
    /// <summary>4 key</summary>
    Num4 = 52,
    /// <summary>5 key</summary>
    Num5 = 53,
    /// <summary>6 key</summary>
    Num6 = 54,
    /// <summary>7 key</summary>
    Num7 = 55,
    /// <summary>8 key</summary>
    Num8 = 56,
    /// <summary>9 key</summary>
    Num9 = 57,
    /// <summary>A key</summary>
    A = 65,
    /// <summary>B key</summary>
    B = 66,
    /// <summary>C key</summary>
    C = 67,
    /// <summary>D key</summary>
    D = 68,
    /// <summary>E key</summary>
    E = 69,
    /// <summary>F key</summary>
    F = 70,
    /// <summary>G key</summary>
    G = 71,
    /// <summary>H key</summary>
    H = 72,
    /// <summary>I key</summary>
    I = 73,
    /// <summary>J key</summary>
    J = 74,
    /// <summary>K key</summary>
    K = 75,
    /// <summary>L key</summary>
    L = 76,
    /// <summary>M key</summary>
    M = 77,
    /// <summary>N key</summary>
    N = 78,
    /// <summary>O key</summary>
    O = 79,
    /// <summary>P key</summary>
    P = 80,
    /// <summary>Q key</summary>
    Q = 81,
    /// <summary>R key</summary>
    R = 82,
    /// <summary>S key</summary>
    S = 83,
    /// <summary>T key</summary>
    T = 84,
    /// <summary>U key</summary>
    U = 85,
    /// <summary>V key</summary>
    V = 86,
    /// <summary>W key</summary>
    W = 87,
    /// <summary>X key</summary>
    X = 88,
    /// <summary>Y key</summary>
    Y = 89,
    /// <summary>Z key</summary>
    Z = 90,
    /// <summary>F1 key</summary>
    F1 = 0x70,
    /// <summary>F2 key</summary>
    F2 = 0x71,
    /// <summary>F3 key</summary>
    F3 = 0x72,
    /// <summary>F4 key</summary>
    F4 = 0x73,
    /// <summary>F5 key</summary>
    F5 = 0x74,
    /// <summary>F6 key</summary>
    F6 = 0x75,
    /// <summary>F7 key</summary>
    F7 = 0x76,
    /// <summary>F8 key</summary>
    F8 = 0x77,
    /// <summary>F9 key</summary>
    F9 = 0x78,
    /// <summary>F10 key</summary>
    F10 = 0x79,
    /// <summary>F11 key</summary>
    F11 = 0x7A,
    /// <summary>F12 key</summary>
    F12 = 0x7B,
    /// <summary>; key</summary>
    Semicolon = 0xBA,
    /// <summary>+ key</summary>
    Equal = 0xBB,
    /// <summary>, key</summary>
    Comma = 0xBC,
    /// <summary>- key</summary>
    Minus = 0xBD,
    /// <summary>. key</summary>
    Period = 0xBE,
    /// <summary>/ key</summary>
    Slash = 0xBF,
    /// <summary>Backtick key</summary>
    Grave = 0xC0,
    /// <summary>[ key</summary>
    LeftBracket = 0xDB,
    /// <summary>Back slash key</summary>
    BackSlash = 0xDC,
    /// <summary>] key</summary>
    RightBracket = 0xDD,
    /// <summary>Quote key</summary>
    Quote = 0xDE,
  }

  /// <since>6.0</since>
  [Flags]
  public enum MouseButton
  {
    None = 0,
    Left = 1,
    Right = 2,
    Middle = 4,
  }

  /// <summary>
  /// Keyboard keys typically used in combination with other keys
  /// </summary>
  /// <since>6.0</since>
  [Flags]
  public enum ModifierKey
  {
    /// <summary>No key</summary>
    None = 0,
    /// <summary>Ctrl key on Windows</summary>
    Control = 1,
    /// <summary>Command key on Mac. This is treated the same as Control key on Windows</summary>
    MacCommand = 1,
    /// <summary>Shift key</summary>
    Shift = 2,
    /// <summary>Alt key</summary>
    Alt = 4,
    /// <summary>Control key on Mac</summary>
    MacControl = 8
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

    /// <since>5.0</since>
    public Display.RhinoView View
    {
      get { return m_view ?? (m_view = Display.RhinoView.FromRuntimeSerialNumber(m_view_serial_number)); }
    }

    /// <since>6.0</since>
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

    /// <since>5.0</since>
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

    /// <since>6.0</since>
    public bool ShiftKeyDown
    {
      get { return (m_flags & MK_SHIFT) == MK_SHIFT; }
    }

    /// <since>6.0</since>
    public bool CtrlKeyDown
    {
      get { return (m_flags & MK_CONTROL) == MK_CONTROL; }
    }
    
    public uint Flags
    {
      get { return m_flags; }
    }

    /// <since>5.0</since>
    public System.Drawing.Point ViewportPoint
    {
      get { return m_point; }
    }

    /// <since>8.8</since>
    public Gumball.GumballMode IsOverGumball()
    {
      return (Gumball.GumballMode)UnsafeNativeMethods.RHC_Gumball_MouseOverMode(m_view_serial_number);
    }
  }

  /// <summary>Used for intercepting mouse events in the Rhino views.</summary>
  public abstract class MouseCallback
  {
    /// <summary>
    /// Called at the beginning of handling of a mouse move event in Rhino.
    /// If you don't want the default Rhino functionality to be run, then set
    /// Cancel to true on the passed in event arguments.
    /// Base class implementation of this function does nothing.
    /// </summary>
    /// <param name="e"></param>
    protected virtual void OnMouseMove(MouseCallbackEventArgs e) { }

    /// <summary>
    /// Called at the end of handling of a mouse move event in Rhino.
    /// All of the default Rhino mouse move functionality has already been
    /// executed unless a MouseCallback has set Cancel to true for the event arguments.
    /// You can tell if this is the case by inspecting the Cancel property in
    /// the event arguments parameter.
    /// Base class implementation of this function does nothing.
    /// </summary>
    /// <param name="e"></param>
    protected virtual void OnEndMouseMove(MouseCallbackEventArgs e) { }

    /// <summary>
    /// Called at the beginning of handling of a mouse down event in Rhino.
    /// If you don't want the default Rhino functionality to be run, then set
    /// Cancel to true on the passed in event arguments
    /// Base class implementation of this function does nothing
    /// </summary>
    /// <param name="e"></param>
    protected virtual void OnMouseDown(MouseCallbackEventArgs e) { }

    /// <summary>
    /// Called at the end of handling of a mouse down event in Rhino.
    /// All of the default Rhino mouse down functionality has already been
    /// executed unless a MouseCallback has set Cancel to true for the event arguments.
    /// You can tell if this is the case by inspecting the Cancel property in
    /// the event arguments parameter.
    /// Base class implementation of this function does nothing
    /// </summary>
    /// <param name="e"></param>
    protected virtual void OnEndMouseDown(MouseCallbackEventArgs e) { }

    /// <summary>
    /// Called at the beginning of handling of a mouse up event in Rhino.
    /// If you don't want the default Rhino functionality to be run, then set
    /// Cancel to true on the passed in event arguments
    /// Base class implementation of this function does nothing
    /// </summary>
    /// <param name="e"></param>
    protected virtual void OnMouseUp(MouseCallbackEventArgs e) { }

    /// <summary>
    /// Called at the end of handling of a mouse up event in Rhino.
    /// All of the default Rhino mouse down functionality has already been
    /// executed unless a MouseCallback has set Cancel to true for the event arguments.
    /// You can tell if this is the case by inspecting the Cancel property in
    /// the event arguments parameter.
    /// Base class implementation of this function does nothing
    /// </summary>
    /// <param name="e"></param>
    protected virtual void OnEndMouseUp(MouseCallbackEventArgs e) { }

    
    protected virtual void OnMouseDoubleClick(MouseCallbackEventArgs e) { }

    protected virtual void OnMouseEnter(MouseCallbackEventArgs e) { }
    protected virtual void OnMouseLeave(MouseCallbackEventArgs e) { }
    protected virtual void OnMouseHover(MouseCallbackEventArgs e) { }
    
    bool m_enabled;
    /// <since>5.0</since>
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
