using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rhino.Render
{
  /// <summary>
  /// RdkUndoRecord class
  /// </summary>
  public sealed class RdkUndoRecord : IDisposable
  {
    private IntPtr m_cpp;

    /// <summary>
    /// Constructor for RdkUndoRecord
    /// </summary>
    public RdkUndoRecord(IntPtr pUndoRecord)
    {
      m_cpp = pUndoRecord;
    }

    /// <summary>
    /// Destructor for RdkUndoRecord
    /// </summary>
    ~RdkUndoRecord()
    {
      Dispose(false);
    }

    /// <summary>
    /// Dispose for RdkUndoRecord
    /// </summary>
    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Set description for RdkUndoRecord
    /// </summary>
    public void SetDescription(String description)
    {
      UnsafeNativeMethods.RhRdkUndoRecord_SetDescription(m_cpp, description);
    }

    void Dispose(bool bDisposing)
    {
      if (m_cpp != IntPtr.Zero)
      {
        UnsafeNativeMethods.RhRdkUndoRecord_DeleteThis(m_cpp);
        m_cpp = IntPtr.Zero;
      }
    }
  }

  /// <summary>
  /// RdkUndo class, which is used to get the 
  /// RdkUndoRecord
  /// </summary>
  public sealed class RdkUndo : IDisposable
  {
    private IntPtr m_cpp;

    /// <summary>
    /// Constructor for RdkUndo
    /// </summary>
    public RdkUndo(IntPtr pUndoRecord)
    {
      m_cpp = pUndoRecord;
    }

    /// <summary>
    /// Destructor for RdkUndo
    /// </summary>
    ~RdkUndo()
    {
      Dispose(false);
    }

    /// <summary>
    /// Dispose for RdkUndo
    /// </summary>
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

    /// <summary>
    /// Get a new UndoRecord
    /// </summary>
    public RdkUndoRecord NewUndoRecord()
    {
      IntPtr pUndoRecord = UnsafeNativeMethods.RhRdkUndo_NewUndoRecord(m_cpp);

      if(pUndoRecord != IntPtr.Zero)
        return new RdkUndoRecord(pUndoRecord);

      return null;
    }
  }
}
