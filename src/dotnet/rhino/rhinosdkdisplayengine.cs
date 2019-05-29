#pragma warning disable 1591
using System;
#if RHINO_SDK

namespace Rhino.Display
{
  #region NEW IN V6, OK TO BREAK
  public abstract class DisplayEngine : IDisposable
  {
    IntPtr m_ptr_display_engine;

    internal IntPtr NonConstPointer() { return m_ptr_display_engine; }
    internal IntPtr ConstPointer() { return m_ptr_display_engine; }

    protected DisplayEngine()
    {
      m_ptr_display_engine = UnsafeNativeMethods.CRhinoCommonDisplayEngine_New();
    }



    /// <summary>Actively reclaims unmanaged resources that this instance uses.</summary>
    public void Dispose()
    {
      Dispose(true);
    }

    /// <summary>
    /// Passively reclaims unmanaged resources when the class user did not explicitly call Dispose().
    /// </summary>
    ~DisplayEngine()
    {
      Dispose(false);
    }

    private void Dispose(bool isDisposing)
    {
      if( m_ptr_display_engine != IntPtr.Zero )
      {
        UnsafeNativeMethods.CRhinoDisplayEngine_Delete(m_ptr_display_engine);
      }
      m_ptr_display_engine = IntPtr.Zero;

      if (isDisposing)
        GC.SuppressFinalize(this);
    }
  }
  #endregion
}

#endif