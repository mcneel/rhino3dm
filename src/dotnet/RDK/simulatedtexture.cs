#if RHINO_SDK
#pragma warning disable 1591
using System;
using Rhino.Runtime.InteropWrappers;

namespace Rhino.Render
{
  public class SimulatedTexture : IDisposable
  {
    private IntPtr m_pSim = IntPtr.Zero;
    private readonly Rhino.Render.SimulatedEnvironment m_parent_simulated_environment;
    private readonly bool m_bAutoDelete = true;

    public SimulatedTexture()
    {
      m_pSim = UnsafeNativeMethods.Rdk_SimulatedTexture_New();
    }

    internal SimulatedTexture(Rhino.Render.SimulatedEnvironment parent)
    {
      m_parent_simulated_environment = parent;
    }

    internal SimulatedTexture(IntPtr p)
    {
      m_pSim = p;
      m_bAutoDelete = false;
    }

    ~SimulatedTexture()
    {
      Dispose(false);
    }

    public static int BitmapSize
    {
      get
      {
        return UnsafeNativeMethods.Rdk_SimulatedTexture_TextureSize();
      }
      set
      {
        UnsafeNativeMethods.Rdk_SimulatedTexture_SetTextureSize(value);
      }
    }

    public Rhino.Geometry.Transform LocalMappingTransform
    {
      get
      {
        Rhino.Geometry.Transform xform = new Rhino.Geometry.Transform();
        UnsafeNativeMethods.Rdk_SimulatedTexture_LocalMappingTransform(ConstPointer(), ref xform);
        return xform;
      }
    }

    public String Filename
    {
      get
      {
        using (var sh = new StringHolder())
        {
          IntPtr pString = sh.NonConstPointer();
          UnsafeNativeMethods.Rdk_SimulatedTexture_Filename(ConstPointer(), pString);
          return sh.ToString();
        }
      }
      set
      {
        UnsafeNativeMethods.Rdk_SimulatedTexture_SetFilename(ConstPointer(), IntPtr.Zero, value);
      }
    }

    public String OriginalFilename
    {
      get
      {
        using (var sh = new StringHolder())
        {
          IntPtr pString = sh.NonConstPointer();
          UnsafeNativeMethods.Rdk_SimulatedTexture_OriginalFilename(ConstPointer(), pString);
          return sh.ToString();
        }
      }
    }

    public Rhino.Geometry.Vector2d Repeat
    {
      get
      {
        Rhino.Geometry.Vector2d v = new Rhino.Geometry.Vector2d();
        UnsafeNativeMethods.Rdk_SimulatedTexture_Repeat(ConstPointer(), ref v);
        return v;
      }
      set
      {
        UnsafeNativeMethods.Rdk_SimulatedTexture_SetRepeat(ConstPointer(), value);
      }
    }

    public Rhino.Geometry.Vector2d Offset
    {
      get
      {
        Rhino.Geometry.Vector2d v = new Rhino.Geometry.Vector2d();
        UnsafeNativeMethods.Rdk_SimulatedTexture_Offset(ConstPointer(), ref v);
        return v;
      }
      set
      {
        UnsafeNativeMethods.Rdk_SimulatedTexture_SetOffset(ConstPointer(), value);
      }
    }

    public double Rotation
    {
      get
      {
        return UnsafeNativeMethods.Rdk_SimulatedTexture_Rotation(ConstPointer());
      }
      set
      {
        UnsafeNativeMethods.Rdk_SimulatedTexture_SetRotation(ConstPointer(), value);
      }
    }

    public bool Repeating
    {
      get
      {
        return UnsafeNativeMethods.Rdk_SimulatedTexture_Repeating(ConstPointer());
      }
      set
      {
        UnsafeNativeMethods.Rdk_SimulatedTexture_SetRepeating(ConstPointer(), value);
      }
    }

    public int MappingChannel
    {
      get
      {
        return UnsafeNativeMethods.Rdk_SimulatedTexture_MappingChannel(ConstPointer());
      }

      //Setting only the mapping channel is not recommended.  Prefer SetMappingChannelAndProjectionModeMode
      set
      {
        UnsafeNativeMethods.Rdk_SimulatedTexture_SetMappingChannel(ConstPointer(), value);
      }
    }

    public enum ProjectionModes : int
    {
      MappingChannel = 0,
      View = 1,
      Wcs = 2,
      Emap = 3,
      WcsBox = 4,
      Screen = 5,
    }

    public ProjectionModes ProjectionMode
    {
      get
      {
        return (ProjectionModes)UnsafeNativeMethods.Rdk_SimulatedTexture_ProjectionMode(m_pSim);
      }

      //Setting only the projection mode is not recommended.  Prefer SetMappingChannelAndProjectionModeMode
      set
      {
        UnsafeNativeMethods.Rdk_SimulatedTexture_SetProjectionMode(ConstPointer(), (int)value);
      }
    }

    public enum EnvironmentMappingModes : int
    {
      Automatic = 0,
      Spherical = 1, // Equirectangular projection.
      Emap = 2, // Mirrorball.
      Box = 3,
      Lightprobe = 5,
      Cubemap = 6,
      VerticalCrossCubemap = 7,
      HorizontalCrossCubemap = 8,
      Hemispherical = 9
    };

    public void SetMappingChannelAndProjectionMode(ProjectionModes pm, int mappingChannel, EnvironmentMappingModes emm)
    {
      UnsafeNativeMethods.Rdk_SimulatedTexture_SetMappingAndProjection(ConstPointer(), (int)pm, mappingChannel, (int)emm);
    }

    public bool HasTransparentColor
    {
      get
      {
        return UnsafeNativeMethods.Rdk_SimulatedTexture_HasTransparentColor(ConstPointer());
      }
      set
      {
        UnsafeNativeMethods.Rdk_SimulatedTexture_SetHasTransparentColor(ConstPointer(), value);
      }
    }

    public Rhino.Display.Color4f TransparentColor
    {
      get
      {
        Rhino.Display.Color4f color = new Rhino.Display.Color4f();
        UnsafeNativeMethods.Rdk_SimulatedTexture_TransparentColor(ConstPointer(), ref color);
        return color;
      }
      set
      {
        UnsafeNativeMethods.Rdk_SimulatedTexture_SetTransparentColor(ConstPointer(), value);
      }
    }

    public double TransparentColorSensitivity
    {
      get
      {
        return UnsafeNativeMethods.Rdk_SimulatedTexture_TransparentColorSensitivity(ConstPointer());
      }
      set
      {
        UnsafeNativeMethods.Rdk_SimulatedTexture_SetTransparentColorSensitivity(ConstPointer(), value);
      }
    }

    public bool Filtered
    {
      get
      {
        return UnsafeNativeMethods.Rdk_SimulatedTexture_Filtered(ConstPointer());
      }
      set
      {
        UnsafeNativeMethods.Rdk_SimulatedTexture_SetFiltered(ConstPointer(), value);
      }
    }

    //TODO:
    /* Calling GetColorAdjuster() can only be done once - it will return NULL the second time.
      The call transfers ownership of the IColorAdjuster object to the caller. */
    //CRhRdkTexture::IColorAdjuster* GetColorAdjuster(void) const;

    /* The call transfers ownership of the IColorAdjuster object to this object. */
    //void SetColorAdjuster(CRhRdkTexture::IColorAdjuster*);

    [Obsolete("Obsolete, use version that requires a document")]
    public double UnitsToMeters(double units)
    {
      var doc = RhinoDoc.ActiveDoc;
      return UnitsToMeters(doc, units);
    }
    public double UnitsToMeters(RhinoDoc doc, double units)
    {
      return UnsafeNativeMethods.Rdk_SimulatedTexture_UnitsToMeters(doc.RuntimeSerialNumber, ConstPointer(), units);
    }

    [Obsolete("Obsolete, use version that requires a document")]
    public double MetersToUnits(double units)
    {
      var doc = RhinoDoc.ActiveDoc;
      return MetersToUnits(doc, units);
    }

    public double MetersToUnits(RhinoDoc doc, double units)
    {
      return UnsafeNativeMethods.Rdk_SimulatedTexture_MetersToUnits(doc.RuntimeSerialNumber, ConstPointer(), units);
    }

    public Rhino.DocObjects.Texture Texture()
    {
      Rhino.DocObjects.Texture texture = new Rhino.DocObjects.Texture(this);

      return texture;
    }

    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
      if (IntPtr.Zero != m_pSim)
      {
        if (m_bAutoDelete)
        {
          UnsafeNativeMethods.Rdk_SimulatedTexture_Delete(m_pSim);
        }
        m_pSim = IntPtr.Zero;
      }
    }

    public IntPtr ConstPointer()
    {
      if (m_pSim != IntPtr.Zero)
      {
        return m_pSim;
      }
      return UnsafeNativeMethods.Rdk_SimulatedEnvironment_Texture(m_parent_simulated_environment.ConstPointer());
    }



  }
}
#endif