using System;
using Rhino.Geometry;

namespace Rhino.FileIO
{
  /// <summary>
  /// Represents the ground plane in this file.
  /// </summary>
  public class File3dmGroundPlane
  {
    readonly File3dm _parent;

    internal File3dmGroundPlane(File3dm parent)
    {
      _parent = parent;
    }

    /// <summary>
    /// Gets or sets if the ground plane is enabled.
    /// </summary>
    /// <since>8.0</since>
    public bool On
    {
      get { return UnsafeNativeMethods.ON_GroundPlane_GetOn(_parent.ConstPointer()); }
      set {        UnsafeNativeMethods.ON_GroundPlane_SetOn(_parent.NonConstPointer(), value); }
    }

    /// <summary>
    /// Gets or sets if the ground plane backface is enabled.
    /// </summary>
    /// <since>8.0</since>
    public bool ShowUnderside
    {
      get { return UnsafeNativeMethods.ON_GroundPlane_GetShowUnderside(_parent.ConstPointer()); }
      set {        UnsafeNativeMethods.ON_GroundPlane_SetShowUnderside(_parent.NonConstPointer(), value); }
    }

    /// <summary>
    /// Gets or sets the altitude of the ground plane.
    /// </summary>
    /// <since>8.0</since>
    public double Altitude
    {
      get { return UnsafeNativeMethods.ON_GroundPlane_GetAltitude(_parent.ConstPointer()); }
      set {        UnsafeNativeMethods.ON_GroundPlane_SetAltitude(_parent.NonConstPointer(), value); }
    }

    /// <summary>
    /// Gets or sets if auto-altitude is enabled.
    /// </summary>
    /// <since>8.0</since>
    public bool AutoAltitude
    {
      get { return UnsafeNativeMethods.ON_GroundPlane_GetAutoAltitude(_parent.ConstPointer()); }
      set {        UnsafeNativeMethods.ON_GroundPlane_SetAutoAltitude(_parent.NonConstPointer(), value); }
    }

    /// <summary>
    /// Gets or sets if the ground plane is set to shadow-only.
    /// </summary>
    /// <since>8.0</since>
    public bool ShadowOnly
    {
      get { return UnsafeNativeMethods.ON_GroundPlane_GetShadowOnly(_parent.ConstPointer()); }
      set {        UnsafeNativeMethods.ON_GroundPlane_SetShadowOnly(_parent.NonConstPointer(), value); }
    }

    /// <summary>
    /// Gets or sets the instance id of the ground plane's material.
    /// </summary>
    /// <since>8.0</since>
    public Guid MaterialInstanceId
    {
      get { return UnsafeNativeMethods.ON_GroundPlane_GetMaterialInstanceId(_parent.ConstPointer()); }
      set {        UnsafeNativeMethods.ON_GroundPlane_SetMaterialInstanceId(_parent.NonConstPointer(), value); }
    }

    /// <summary>
    /// Gets or sets the texture offset of the ground plane in model units.
    /// </summary>
    /// <since>8.0</since>
    public Vector2d TextureOffset
    {
      get
      {
        var vec = new Vector2d();
        UnsafeNativeMethods.ON_GroundPlane_GetTextureOffset(_parent.ConstPointer(), ref vec);
        return vec;
      }
      set { UnsafeNativeMethods.ON_GroundPlane_SetTextureOffset(_parent.NonConstPointer(), ref value); }
    }

    /// <summary>
    /// Gets or sets if the texture offset x and y are locked together.
    /// </summary>
    /// <since>8.0</since>
    public bool TextureOffsetLocked
    {
      get { return UnsafeNativeMethods.ON_GroundPlane_GetTextureOffsetLocked(_parent.ConstPointer()); }
      set {        UnsafeNativeMethods.ON_GroundPlane_SetTextureOffsetLocked(_parent.NonConstPointer(), value); }
    }

    /// <summary>
    /// Gets or sets if the texture repeat x and y are locked together.
    /// </summary>
    /// <since>8.0</since>
    public bool TextureRepeatLocked
    {
      get { return UnsafeNativeMethods.ON_GroundPlane_GetTextureRepeatLocked(_parent.ConstPointer()); }
      set {        UnsafeNativeMethods.ON_GroundPlane_SetTextureRepeatLocked(_parent.NonConstPointer(), value); }
    }

    /// <summary>
    /// Gets or sets the texture size of the ground plane in model units.
    /// </summary>
    /// <since>8.0</since>
    public Vector2d TextureSize
    {
      get
      {
        var vec = new Vector2d();
        UnsafeNativeMethods.ON_GroundPlane_GetTextureSize(_parent.ConstPointer(), ref vec);
        return vec;
      }
      set { UnsafeNativeMethods.ON_GroundPlane_SetTextureSize(_parent.NonConstPointer(), ref value); }
    }

    /// <summary>
    /// Gets or sets the texture rotation of the ground plane in degrees.
    /// </summary>
    /// <since>8.0</since>
    public double TextureRotation
    {
      get { return UnsafeNativeMethods.ON_GroundPlane_GetTextureRotation(_parent.ConstPointer()); }
      set {        UnsafeNativeMethods.ON_GroundPlane_SetTextureRotation(_parent.NonConstPointer(), value); }
    }
  }
}
