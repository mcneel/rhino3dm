using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Rhino.Render
{
  /// <summary>
  /// Base class for Rhino.Render objects that are owned by the document, or can be delivered separately
  /// from other functions.  In general, you cannot create these objects yourself.
  /// </summary>
  /// '
  /// 
  //TODO:  This class should also support creation of the object, disposal, and it should clean up when
  //the document is closed...and if possible, when the underlying C++ object is deleted.
  public abstract class FreeFloatingBase
  {
    /// <summary>
    /// Implementation must provide a function that calls into C++ to default construct an object of this type.
    /// </summary>
    /// <returns></returns>
    internal abstract IntPtr DefaultCppConstructor();

    /// <summary>
    /// Copy from another object
    /// </summary>
    /// <param name="src"></param>
    public abstract void CopyFrom(FreeFloatingBase src);

    internal abstract void DeleteCpp();

    /// <summary>
    /// Handle destruction of the un-managed CPP object.
    /// </summary>
    ~FreeFloatingBase()
    {
      if (m_bAutoDelete)
      {
        DeleteCpp();
      }
    }

    internal FreeFloatingBase(IntPtr nativePtr)
    {
      m_cpp_ptr = nativePtr;
    }

    internal virtual IntPtr CppPointer
    {
      get
      {
        return m_cpp_ptr;
      }
    }

    internal FreeFloatingBase()
    {
      m_cpp_ptr = DefaultCppConstructor();
      m_bAutoDelete = true;
    }

    internal FreeFloatingBase(FreeFloatingBase src)
    {
      m_cpp_ptr = DefaultCppConstructor();
      m_bAutoDelete = true;

      CopyFrom(src);
    }


    private IntPtr m_cpp_ptr = IntPtr.Zero;
    private bool m_bAutoDelete = false;
  }

  /// <summary>
  /// Base class for Rhino.Render objects that are owned by the document, or can be delivered separately
  /// from other functions.  In general, you cannot create these objects yourself.
  /// </summary>
  public abstract class DocumentOrFreeFloatingBase : FreeFloatingBase
  {
    internal DocumentOrFreeFloatingBase(uint docSerial) : base(IntPtr.Zero)
    {
      m_doc_serial_number = docSerial;
    }

    internal DocumentOrFreeFloatingBase(IntPtr nativePtr) : base(nativePtr) { }
    internal DocumentOrFreeFloatingBase(IntPtr nativePtr, bool write) : base(nativePtr)
    {
      if (write == true)
        m_cpp_non_const_ptr = nativePtr;
      else
        m_cpp_non_const_ptr = IntPtr.Zero;
    }

    internal DocumentOrFreeFloatingBase() : base() { }
    internal DocumentOrFreeFloatingBase(DocumentOrFreeFloatingBase src) : base(src) { }

    //Required overrides
    internal abstract IntPtr CppFromDocSerial(uint sn);
    internal abstract IntPtr BeginChangeImpl(IntPtr const_ptr, Rhino.Render.RenderContent.ChangeContexts cc);
    internal abstract bool   EndChangeImpl(IntPtr non_const_ptr);

    internal override IntPtr CppPointer
    {
      get
      {
        if (m_cpp_non_const_ptr != IntPtr.Zero)
        {
          return m_cpp_non_const_ptr;
        }

        if (m_doc_serial_number != 0)
        {
          return CppFromDocSerial(m_doc_serial_number);
        }
        return base.CppPointer;
      }
    }

    /// <summary>
    /// Call this function before making any change to this object (calling a setter) otherwise undo will not work correctly.  Calls to BeginChange must be paired with a call to EndChange.
    /// </summary>
    /// <param name="cc">Change context</param>
    public void BeginChange(Rhino.Render.RenderContent.ChangeContexts cc)
    {
      m_cpp_non_const_ptr = BeginChangeImpl(CppFromDocSerial(m_doc_serial_number), cc);
      Debug.Assert(IntPtr.Zero != m_cpp_non_const_ptr);
    }

    /// <summary>
    /// See BeginChange
    /// </summary>
    /// <returns>true if the object has returned to no-changes mode.</returns>
    public bool EndChange()
    {
      Debug.Assert(IntPtr.Zero != m_cpp_non_const_ptr);
      if (IntPtr.Zero != m_cpp_non_const_ptr)
      {
        if (EndChangeImpl(m_cpp_non_const_ptr))
        {
          m_cpp_non_const_ptr = IntPtr.Zero;
          return true;
        }
      }
      return false;
    }

    internal bool CanChange
    {
      get
      {
        return m_cpp_non_const_ptr != IntPtr.Zero;
      }
    }

    internal uint DocumentSerial_ForObsoleteFunctionality
    {
      get
      {
        return m_doc_serial_number;
      }
    }

    private IntPtr m_cpp_non_const_ptr = IntPtr.Zero;
    private readonly uint m_doc_serial_number = 0;
  }
}
