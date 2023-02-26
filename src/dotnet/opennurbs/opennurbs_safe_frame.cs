
namespace Rhino.FileIO
{
  /// <summary>
  /// Represents the safe frame in this file.
  /// </summary>
  public class File3dmSafeFrame
  {
    readonly File3dm _parent;

    internal File3dmSafeFrame(File3dm parent)
    {
      _parent = parent;
    }

    /// <summary>
    /// Safe frame enabled state.
    /// </summary>
    /// <since>8.0</since>
    public bool On
    {
      get { return UnsafeNativeMethods.ON_SafeFrame_GetOn(_parent.ConstPointer()); }
      set {        UnsafeNativeMethods.ON_SafeFrame_SetOn(_parent.NonConstPointer(), value); }
    }

    /// <summary>
    /// If safe frame is only displayed in perspective views.
    /// </summary>
    /// <since>8.0</since>
    public bool PerspectiveOnly
    {
      get { return UnsafeNativeMethods.ON_SafeFrame_GetPerspectiveOnly(_parent.ConstPointer()); }
      set {        UnsafeNativeMethods.ON_SafeFrame_SetPerspectiveOnly(_parent.NonConstPointer(), value); }
    }

    /// <summary>
    /// 4x3 field grid state.
    /// </summary>
    /// <since>8.0</since>
    public bool FieldGridOn
    {
      get { return UnsafeNativeMethods.ON_SafeFrame_GetFieldGridOn(_parent.ConstPointer()); }
      set {        UnsafeNativeMethods.ON_SafeFrame_SetFieldGridOn(_parent.NonConstPointer(), value); }
    }

    /// <summary>
    /// Action frame state.
    /// </summary>
    /// <since>8.0</since>
    public bool ActionFrameOn
    {
      get { return UnsafeNativeMethods.ON_SafeFrame_GetActionFrameOn(_parent.ConstPointer()); }
      set {        UnsafeNativeMethods.ON_SafeFrame_SetActionFrameOn(_parent.NonConstPointer(), value); }
    }

    /// <summary>
    /// If action frame X and Y scales are linked.
    /// </summary>
    /// <since>8.0</since>
    public bool ActionFrameLinked
    {
      get { return UnsafeNativeMethods.ON_SafeFrame_GetActionFrameLinked(_parent.ConstPointer()); }
      set {        UnsafeNativeMethods.ON_SafeFrame_SetActionFrameLinked(_parent.NonConstPointer(), value); }
    }

    /// <summary>
    /// Action frame X scale.
    /// </summary>
    /// <since>8.0</since>
    public double ActionFrameXScale
    {
      get { return UnsafeNativeMethods.ON_SafeFrame_GetActionFrameXScale(_parent.ConstPointer()); }
      set {        UnsafeNativeMethods.ON_SafeFrame_SetActionFrameXScale(_parent.NonConstPointer(), value); }
    }

    /// <summary>
    /// Action frame Y scale.
    /// </summary>
    /// <since>8.0</since>
    public double ActionFrameYScale
    {
      get { return UnsafeNativeMethods.ON_SafeFrame_GetActionFrameYScale(_parent.ConstPointer()); }
      set {        UnsafeNativeMethods.ON_SafeFrame_SetActionFrameYScale(_parent.NonConstPointer(), value); }
    }

    /// <summary>
    /// Title frame state.
    /// </summary>
    /// <since>8.0</since>
    public bool TitleFrameOn
    {
      get { return UnsafeNativeMethods.ON_SafeFrame_GetTitleFrameOn(_parent.ConstPointer()); }
      set {        UnsafeNativeMethods.ON_SafeFrame_SetTitleFrameOn(_parent.NonConstPointer(), value); }
    }

    /// <summary>
    /// If title frame X and Y scales are linked.
    /// </summary>
    /// <since>8.0</since>
    public bool TitleFrameLinked
    {
      get { return UnsafeNativeMethods.ON_SafeFrame_GetTitleFrameLinked(_parent.ConstPointer()); }
      set {        UnsafeNativeMethods.ON_SafeFrame_SetTitleFrameLinked(_parent.NonConstPointer(), value); }
    }

    /// <summary>
    /// Title frame X scale.
    /// </summary>
    /// <since>8.0</since>
    public double TitleFrameXScale
    {
      get { return UnsafeNativeMethods.ON_SafeFrame_GetTitleFrameXScale(_parent.ConstPointer()); }
      set {        UnsafeNativeMethods.ON_SafeFrame_SetTitleFrameXScale(_parent.NonConstPointer(), value); }
    }

    /// <summary>
    /// Title frame Y scale.
    /// </summary>
    /// <since>8.0</since>
    public double TitleFrameYScale
    {
      get { return UnsafeNativeMethods.ON_SafeFrame_GetTitleFrameYScale(_parent.ConstPointer()); }
      set {        UnsafeNativeMethods.ON_SafeFrame_SetTitleFrameYScale(_parent.NonConstPointer(), value); }
    }

  }
}
