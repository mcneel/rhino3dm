#pragma warning disable 1591
#if RHINO_SDK
using System;
using Rhino.Runtime.InteropWrappers;

namespace Rhino.Render
{
  public class SimulatedEnvironment : IDisposable
  {
    private IntPtr m_pSim = IntPtr.Zero;
    private readonly bool m_bAutoDelete = true;

    public SimulatedEnvironment()
    {
      m_pSim = UnsafeNativeMethods.Rdk_SimulatedEnvironment_New();
    }

    internal SimulatedEnvironment(IntPtr p)
    {
      m_pSim = p;
      m_bAutoDelete = false;
    }

    ~SimulatedEnvironment()
    {
      Dispose(false);
    }

    public System.Drawing.Color BackgroundColor
    {
      get
      {
        return System.Drawing.Color.FromArgb(UnsafeNativeMethods.Rdk_SimulatedEnvironment_BackgroundColor(m_pSim));
      }
      set
      {
        UnsafeNativeMethods.Rdk_SimulatedEnvironment_SetBackgroundColor(m_pSim, value.ToArgb());
      }
    }

    public Rhino.Render.SimulatedTexture BackgroundImage
    {
      get
      {
        return new Rhino.Render.SimulatedTexture(this);
      }
      set
      {
        IntPtr p = value.ConstPointer();
        UnsafeNativeMethods.Rdk_SimulatedEnvironment_SetBackgroundImage(m_pSim, p);

      }
    }

    public enum BackgroundProjections : int
    {
      Planar = 0,
      Spherical = 1,	//equirectangular projection
      Emap = 2,	//mirrorball
      Box = 3,
      Automatic = 4,
      Lightprobe = 5,
      Cubemap = 6,
      VerticalCrossCubemap = 7,
      HorizontalCrossCubemap = 8,
    }


    public BackgroundProjections BackgroundProjection
    {
      get
      {
        return (BackgroundProjections)UnsafeNativeMethods.Rdk_SimulatedEnvironment_BackgroundProjection(m_pSim);
      }
      set
      {
        UnsafeNativeMethods.Rdk_SimulatedEnvironment_SetBackgroundProjection(m_pSim, (int)value);
      }
    }

    public static BackgroundProjections ProjectionFromString(String projection)
    {
      return (BackgroundProjections)UnsafeNativeMethods.Rdk_SimulatedEnvironment_ProjectionFromString(projection);
    }

    public static string StringFromProjection(BackgroundProjections projection)
    {
      using (var sh = new StringHolder())
      {
        IntPtr pString = sh.NonConstPointer();
        UnsafeNativeMethods.Rdk_SimulatedTexture_StringFromProjection(pString, (int)projection);
        return sh.ToString();
      }
    }
    #region TODO
    //public static eBackgroundProjection AutomaticProjectionFromChildTexture(Rhino.Render.RenderTexture texture)
    //{
    //TODO:return (eBackgroundProjection)UnsafeNativeMethods.Rdk_SimulatedEnvironment_AutomaticProjectionFromChildTexture(texture.Id);
    //}
    #endregion

    #region pointer tracking
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
          UnsafeNativeMethods.Rdk_SimulatedEnvironment_Delete(m_pSim);
        }
        m_pSim = IntPtr.Zero;
      }
    }

    public IntPtr ConstPointer()
    {
      return m_pSim;
    }
    #endregion
  }
}



#endif

