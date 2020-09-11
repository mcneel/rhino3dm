#pragma warning disable 1591
using System;
using Rhino.Runtime.InteropWrappers;

#if RHINO_SDK
namespace Rhino.Input.Custom
{
  /// <since>5.1</since>
  public enum GetLineMode : int
  {
    TwoPoint = 0,
    SurfaceNormal = 1,
    Angled = 2,
    Vertical = 3,
    FourPoint = 4,
    Bisector = 5,
    Perpendicular = 6,
    Tangent = 7,
    CurveEnd = 8,
    CPlaneNormalVector = 9
  };

  /// <summary>
  /// Use to interactively get a line.  The Rhino "Line" command uses GetLine.
  /// </summary>
  public class GetLine : IDisposable
  {
    IntPtr m_ptr_argsrhinogetline;
    /// <since>5.1</since>
    public GetLine()
    {
      m_ptr_argsrhinogetline = UnsafeNativeMethods.CArgsRhinoGetLine_New();
    }

    IntPtr ConstPointer() { return m_ptr_argsrhinogetline; }
    IntPtr NonConstPointer() { return m_ptr_argsrhinogetline; }

    /// <summary>
    /// Passively reclaims unmanaged resources when the class user did not explicitly call Dispose().
    /// </summary>
    ~GetLine()
    {
      Dispose(false);
    }

    /// <summary>
    /// Actively reclaims unmanaged resources that this instance uses.
    /// </summary>
    /// <since>5.1</since>
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
      if (IntPtr.Zero != m_ptr_argsrhinogetline)
      {
        UnsafeNativeMethods.CArgsRhinoGetLine_Delete(m_ptr_argsrhinogetline);
        m_ptr_argsrhinogetline = IntPtr.Zero;
      }
    }

    /// <summary>
    /// Perform the 'get' operation.
    /// </summary>
    /// <param name="line"></param>
    /// <returns></returns>
    /// <since>5.1</since>
    public Commands.Result Get(out Geometry.Line line)
    {
      IntPtr ptr_this = NonConstPointer();
      line = Geometry.Line.Unset;
      int rc = UnsafeNativeMethods.RHC_RhinoGetLine2(ptr_this, ref line, IntPtr.Zero);
      return (Commands.Result)rc;
    }

    string GetStringHelper(UnsafeNativeMethods.ArgsGetLineStringConsts which)
    {
      IntPtr ptr_const_this = ConstPointer();
      using (var sh = new StringHolder())
      {
        IntPtr ptr_string = sh.NonConstPointer();
        UnsafeNativeMethods.CArgsRhinoGetLine_GetString(ptr_const_this, which, ptr_string);
        return sh.ToString();
      }
    }
    void SetStringHelper(UnsafeNativeMethods.ArgsGetLineStringConsts which, string s)
    {
      IntPtr ptr_this = NonConstPointer();
      UnsafeNativeMethods.CArgsRhinoGetLine_SetString(ptr_this, which, s);
    }

    /// <summary>Prompt when getting first point</summary>
    /// <since>5.1</since>
    public string FirstPointPrompt
    {
      get { return GetStringHelper(UnsafeNativeMethods.ArgsGetLineStringConsts.FirstPointPrompt); }
      set { SetStringHelper(UnsafeNativeMethods.ArgsGetLineStringConsts.FirstPointPrompt, value); }
    }

    /// <summary>Prompt when getting midpoint</summary>
    /// <since>5.1</since>
    public string MidPointPrompt
    {
      get { return GetStringHelper(UnsafeNativeMethods.ArgsGetLineStringConsts.MidPointPrompt); }
      set { SetStringHelper(UnsafeNativeMethods.ArgsGetLineStringConsts.MidPointPrompt, value); }
    }

    /// <summary>Prompt when getting second point</summary>
    /// <since>5.1</since>
    public string SecondPointPrompt
    {
      get { return GetStringHelper(UnsafeNativeMethods.ArgsGetLineStringConsts.SecondPointPrompt); }
      set { SetStringHelper(UnsafeNativeMethods.ArgsGetLineStringConsts.SecondPointPrompt, value); }
    }

    bool GetBoolHelper(UnsafeNativeMethods.ArgsGetLineBoolConsts which)
    {
      IntPtr ptr_const_this = ConstPointer();
      return UnsafeNativeMethods.CArgsRhinoGetLine_GetBool(ptr_const_this, which);
    }

    void SetBoolHelper(UnsafeNativeMethods.ArgsGetLineBoolConsts which, bool value)
    {
      IntPtr ptr_this = NonConstPointer();
      UnsafeNativeMethods.CArgsRhinoGetLine_SetBool(ptr_this, which, value);
    }


    /// <summary>
    /// Controls whether or not a zero length line is acceptable.
    /// The default is to require the user to keep picking the end
    /// point until we get a point different than the start point.
    /// </summary>
    /// <since>5.1</since>
    public bool AcceptZeroLengthLine
    {
      get { return GetBoolHelper(UnsafeNativeMethods.ArgsGetLineBoolConsts.AcceptZeroLengthLine); }
      set { SetBoolHelper(UnsafeNativeMethods.ArgsGetLineBoolConsts.AcceptZeroLengthLine, value); }
    }

    /// <summary>
    /// If true, the feedback color is used to draw the dynamic
    /// line when the second point is begin picked.  If false,
    /// the active layer color is used.
    /// </summary>
    /// <since>5.1</since>
    public bool HaveFeedbackColor
    {
      get { return GetBoolHelper(UnsafeNativeMethods.ArgsGetLineBoolConsts.HaveFeedbackColor); }
    }

    /// <summary>
    /// If set, the feedback color is used to draw the dynamic
    /// line when the second point is begin picked.  If not set,
    /// the active layer color is used.
    /// </summary>
    /// <since>5.1</since>
    public System.Drawing.Color FeedbackColor
    {
      get
      {
        IntPtr ptr_const_this = ConstPointer();
        int argb = UnsafeNativeMethods.CArgsRhinoGetLine_GetFeedbackColor(ptr_const_this);
        return System.Drawing.Color.FromArgb(argb);
      }
      set
      {
        IntPtr ptr_this = NonConstPointer();
        int argb = value.ToArgb();
        UnsafeNativeMethods.CArgsRhinoGetLine_SetFeedbackColor(ptr_this, argb);
      }
    }

    /// <summary>
    /// If FixedLength > 0, the line must have the specified length
    /// </summary>
    /// <since>5.1</since>
    public double FixedLength
    {
      get
      {
        IntPtr ptr_const_this = ConstPointer();
        return UnsafeNativeMethods.CArgsRhinoGetLine_GetFixedLength(ptr_const_this);
      }
      set
      {
        IntPtr ptr_this = NonConstPointer();
        UnsafeNativeMethods.CArgsRhinoGetLine_SetFixedLength(ptr_this, value);
      }
    }

    /// <summary>
    /// If true, then the "BothSides" option shows up when the
    /// start point is interactively picked.
    /// </summary>
    /// <param name="on"></param>
    /// <since>5.1</since>
    public void EnableFromBothSidesOption(bool on)
    {
      SetBoolHelper(UnsafeNativeMethods.ArgsGetLineBoolConsts.EnableFromBothSidesOption, on);
    }

    /// <summary>
    /// If true, the "MidPoint" options shows up
    /// </summary>
    /// <param name="on"></param>
    /// <since>5.1</since>
    public void EnableFromMidPointOption(bool on)
    {
      SetBoolHelper(UnsafeNativeMethods.ArgsGetLineBoolConsts.EnableFromMidPointOption, on);
    }

    /// <summary>
    /// If true, then all line variations are shown if the default line mode is used
    /// </summary>
    /// <param name="on"></param>
    /// <since>5.1</since>
    public void EnableAllVariations(bool on)
    {
      SetBoolHelper(UnsafeNativeMethods.ArgsGetLineBoolConsts.EnableAllVariations, on);
    }

    /// <summary>
    /// Use SetFirstPoint to specify the line's starting point and skip
    /// the start point interactive picking
    /// </summary>
    /// <param name="point"></param>
    /// <since>5.1</since>
    public void SetFirstPoint(Geometry.Point3d point)
    {
      IntPtr ptr_this = NonConstPointer();
      UnsafeNativeMethods.CArgsRhinoGetLine_SetFirstPoint(ptr_this, point);
    }

    /// <summary>
    /// Mode used
    /// </summary>
    /// <since>5.1</since>
    public GetLineMode GetLineMode
    {
      get
      {
        IntPtr ptr_const_this = ConstPointer();
        int rc = UnsafeNativeMethods.CArgsRhinoGetLine_GetLineMode(ptr_const_this);
        return (GetLineMode)rc;
      }
      set
      {
        IntPtr ptr_this = NonConstPointer();
        UnsafeNativeMethods.CArgsRhinoGetLine_SetLineMode(ptr_this, (int)value);
      }
    }

    //public Rhino.DocObjects.ObjRef Point1ObjRef() { }
    //public Rhino.DocObjects.ObjRef Point2ObjRef() { }
  }
}
#endif
