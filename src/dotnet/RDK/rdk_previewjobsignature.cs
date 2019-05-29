#pragma warning disable 1591

using System;

namespace Rhino.Render
{
  sealed class PreviewJobSignature : IDisposable
  {
    private IntPtr m_cpp;

    public IntPtr CppPointer
    {
      get { return m_cpp; }
    }

    public PreviewJobSignature()
    {
      m_cpp = UnsafeNativeMethods.CRhRdkPreviewJobSignature_New_Def();
    }

    //[CLSCompliant(false)]
    public PreviewJobSignature(int width, int height, uint sig)
    {
      m_cpp = UnsafeNativeMethods.CRhRdkPreviewJobSignature_New(width, height, sig);
    }

    ~PreviewJobSignature()
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
        UnsafeNativeMethods.CRhRdkPreviewJobSignature_Delete(m_cpp);
        m_cpp = IntPtr.Zero;
      }
    }

    public bool Comapre(PreviewJobSignature pjs)
    { 
      return UnsafeNativeMethods.CRhRdkPreviewJobSignature_Compare(m_cpp, pjs.CppPointer);
    }

    public int ImageWidth()
    {
      return UnsafeNativeMethods.CRhRdkPreviewJobSignature_ImageWidth(m_cpp);
    }

    public int ImageHeight()
    {
      return UnsafeNativeMethods.CRhRdkPreviewJobSignature_ImageHeight(m_cpp);
    }
  }
}