using System;

namespace Rhino
{
  /// <summary>
  /// This class probably does not have to be publicly exposed
  /// </summary>
  class ThreadTerminator : IDisposable
  {
    IntPtr m_ptr_terminator; // ON_Terminator*
    public ThreadTerminator()
    {
      m_ptr_terminator = UnsafeNativeMethods.ON_Terminator_New();
    }

    ~ThreadTerminator() { Dispose(false); }

    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
      if (IntPtr.Zero != m_ptr_terminator)
      {
        UnsafeNativeMethods.ON_Terminator_Delete(m_ptr_terminator);
      }
      m_ptr_terminator = IntPtr.Zero;
    }

    public void RequestCancel()
    {
      UnsafeNativeMethods.ON_Terminator_Cancel(m_ptr_terminator);
    }

    public IntPtr NonConstPointer()
    {
      return m_ptr_terminator;
    }
  }
}