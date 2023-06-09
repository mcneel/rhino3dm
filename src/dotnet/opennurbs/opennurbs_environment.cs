using System;

namespace Rhino.DocObjects
{
  /// <summary>
  /// Represents an environment.
  /// </summary>
  public class Environment : Runtime.CommonObject
  {
    /// <summary>
    /// The available background projections.
    /// </summary>
    /// 
    /// <since>8.0</since>
    public enum BackgroundProjections : int
    {
      /// <summary>Planar.</summary>
      Planar = 0,
      /// <summary>Spherical.</summary>
      Spherical = 1,
      /// <summary>Environment map.</summary>
      Emap = 2,
      /// <summary>Box.</summary>
      Box = 3,
      /// <summary>Automatic.</summary>
      Automatic = 4,
      /// <summary>Light probe.</summary>
      LightProbe = 5,
      /// <summary>Cube map.</summary>
      CubeMap = 6,
      /// <summary>Vertical cross cube map.</summary>
      VerticalCrossCubeMap = 7,
      /// <summary>Horizontal cross cube map.</summary>
      HorizontalCrossCubeMap = 8,
      /// <summary>Hemispherical.</summary>
      Hemispherical = 9,
    }

    /// <summary>
    /// Initializes a new environment.
    /// </summary>
    /// <since>8.0</since>
    public Environment()
    {
      var ptr = UnsafeNativeMethods.ON_Environment_New();
      ConstructNonConstObject(ptr);
    }

    /// <summary>
    /// <return>The background color.</return>
    /// </summary>
    /// <since>8.0</since>
    public System.Drawing.Color BackgroundColor
    {
      get
      {
        var col = UnsafeNativeMethods.ON_Environment_BackgroundColor(ConstPointer());
        return Runtime.Interop.ColorFromWin32(col);
      }
    }

    /// <summary>
    /// <return>The background image texture.</return>
    /// </summary>
    /// <since>8.0</since>
    public Texture BackgroundImage
    {
      get
      {
        var tex = new Texture();
        UnsafeNativeMethods.ON_Environment_BackgroundImage(ConstPointer(), tex.NonConstPointer());
        return tex;
      }
    }

    /// <summary>
    /// <return>The background projection.</return>
    /// </summary>
    /// <since>8.0</since>
    public BackgroundProjections BackgroundProjection
    {
      get
      {
        var p = UnsafeNativeMethods.ON_Environment_BackgroundProjection(ConstPointer());
        return (BackgroundProjections)p;
      }
    }

    internal override IntPtr _InternalGetConstPointer() { return IntPtr.Zero; } // Not called.

    internal override IntPtr _InternalDuplicate(out bool applymempressure)
    {
      applymempressure = false;
      IntPtr pConstPointer = ConstPointer();
      return UnsafeNativeMethods.ON_Object_Duplicate(pConstPointer);
    }
  }
}
