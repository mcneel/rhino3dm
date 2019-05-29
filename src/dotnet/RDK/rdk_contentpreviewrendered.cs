#pragma warning disable 1591

using System;

namespace Rhino.Render
{

  sealed class ContentPreviewRendered : IDisposable
  {
    private IntPtr m_cpp;

    public IntPtr CppPointer
    {
      get { return m_cpp; }
    }

    public ContentPreviewRendered(IntPtr pContentPreviewRendered)
    {
      m_cpp = pContentPreviewRendered;
    }

    ~ContentPreviewRendered()
    {
      Dispose(false);
    }

    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    void Dispose(bool bDisposing)
    {
      if (m_cpp != IntPtr.Zero)
      {
        m_cpp = IntPtr.Zero;
      }
    }

    public System.Drawing.Bitmap Dib()
    {

      if (m_cpp != IntPtr.Zero)
      {
        IntPtr pDib = UnsafeNativeMethods.IRhRdkContentPreviewRendered_Dib(m_cpp);

        if (pDib == IntPtr.Zero)
          return null;

        var bitmap = Rhino.Runtime.InteropWrappers.RhinoDib.ToBitmap(pDib, true);
        return bitmap;
      }

      return null;
    }

    public PreviewJobSignature JobSignature()
    {
      if (m_cpp != IntPtr.Zero)
      {
        PreviewJobSignature pjs = new PreviewJobSignature();
        UnsafeNativeMethods.IRhRdkContentPreviewRendered_JobSignature(m_cpp, pjs.CppPointer);
        return pjs;
      }

      return null;
    }

   
  }
}
