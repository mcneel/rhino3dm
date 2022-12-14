using System;

namespace Rhino.FileIO
{
  /// <summary>
  /// Represents the skylight in this file.
  /// </summary>
  public class File3dmSkylight
  {
    readonly File3dm _parent;

    internal File3dmSkylight(File3dm parent)
    {
      _parent = parent;
    }

    /// <summary>
    /// Skylight enabled state.
    /// </summary>
    public bool On
    {
      get { return UnsafeNativeMethods.ON_Skylight_GetOn(_parent.ConstPointer()); }
      set {        UnsafeNativeMethods.ON_Skylight_SetOn(_parent.NonConstPointer(), value); }
    }

    /// <summary>
    /// Skylight custom environment state.
    /// </summary>
    public bool CustomEnvironmentOn
    {
      get { return UnsafeNativeMethods.ON_Skylight_GetCustomEnvironmentOn(_parent.ConstPointer()); }
      set {        UnsafeNativeMethods.ON_Skylight_SetCustomEnvironmentOn(_parent.NonConstPointer(), value); }
    }

    /// <summary>
    /// Skylight custom environment instance id.
    /// </summary>
    public Guid CustomEnvironment
    {
      get { return UnsafeNativeMethods.ON_Skylight_GetCustomEnvironment(_parent.ConstPointer()); }
      set {        UnsafeNativeMethods.ON_Skylight_SetCustomEnvironment(_parent.NonConstPointer(), value); }
    }

    /// <summary>
    /// Skylight shadow intensity. This is unused at present.
    /// </summary>
    public double ShadowIntensity
    {
      get { return UnsafeNativeMethods.ON_Skylight_GetShadowIntensity(_parent.ConstPointer()); }
      set {        UnsafeNativeMethods.ON_Skylight_SetShadowIntensity(_parent.NonConstPointer(), value); }
    }
  }
}
