#pragma warning disable 1591

using System;

namespace Rhino.Render
{
  sealed class ThumbArray : IDisposable
  {
    private IntPtr m_cpp;

    public IntPtr CppPointer
    {
      get { return m_cpp; }
    }

    public ThumbArray(IntPtr pThumbArray)
    {
      m_cpp = pThumbArray;
    }

    ~ThumbArray()
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

    public int Count()
    {
      int value = -1;

      if (m_cpp != IntPtr.Zero)
      {
        value = UnsafeNativeMethods.CRhRdkThumbArray_Count(m_cpp);
      }

      return value;
    }

    public Guid ContentAtIndex(int i)
    {
      Guid uuid = new Guid();

      if (m_cpp != IntPtr.Zero)
      {
        uuid = UnsafeNativeMethods.CRhRdkThumbArray_ContentAtIndex(m_cpp, i);
      }

      return uuid;
    }
  }
}
