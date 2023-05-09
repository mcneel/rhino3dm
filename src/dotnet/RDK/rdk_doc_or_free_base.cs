
using System;
using System.Diagnostics;

namespace Rhino.Render
{
  /// <summary>
  /// Base class for Rhino.Render objects that are owned by the document, or can be delivered separately
  /// from other functions.  In general, you cannot create these objects yourself.
  /// </summary>
  ///
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
    /// <since>6.0</since>
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

    internal virtual IntPtr CppPointer { get => m_cpp_ptr; }

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

    private readonly IntPtr m_cpp_ptr = IntPtr.Zero;
    private readonly bool m_bAutoDelete = false;
  }

  /// <summary>
  /// Base class for Rhino.Render objects that are owned by the document, or can be delivered separately
  /// from other functions.  In general, you cannot create these objects yourself.
  /// </summary>
  public abstract class DocumentOrFreeFloatingBase : FreeFloatingBase
  {
    internal DocumentOrFreeFloatingBase(IntPtr nativePtr) : base(nativePtr) { }

    internal DocumentOrFreeFloatingBase(FileIO.File3dm f) : base(IntPtr.Zero)
    {
      m_file3dm = f;
    }

    internal DocumentOrFreeFloatingBase(uint docSerial) : base(IntPtr.Zero)
    {
      m_doc_serial_number = docSerial;
    }

    internal DocumentOrFreeFloatingBase(IntPtr nativePtr, bool write) : base(nativePtr)
    {
      if (write)
      {
        m_cpp_non_const_ptr = nativePtr;
      }
      else
      {
        m_cpp_non_const_ptr = IntPtr.Zero;
      }
    }

    internal DocumentOrFreeFloatingBase() : base() { }
    internal DocumentOrFreeFloatingBase(DocumentOrFreeFloatingBase src) : base(src) { }

    internal virtual IntPtr CppFromFile3dm(FileIO.File3dm f)
    {
      return IntPtr.Zero;
    }

    internal virtual IntPtr RS_CppFromDocSerial(uint sn, out bool returned_ptr_is_rs)
    {
      returned_ptr_is_rs = false;
      return CppFromDocSerial(sn);
    }

    // Required overrides.
    internal abstract IntPtr CppFromDocSerial(uint sn);
#if RHINO_SDK
    internal abstract IntPtr BeginChangeImpl(IntPtr const_ptr, RenderContent.ChangeContexts cc, bool const_ptr_is_rs);
#endif
    internal abstract bool   EndChangeImpl(IntPtr non_const_pt, uint rhino_doc_sn);

    internal override IntPtr CppPointer
    {
      get
      {
        if (m_cpp_non_const_ptr != IntPtr.Zero)
          return m_cpp_non_const_ptr; // When in write mode.

        if (m_doc_serial_number != 0)
          return CppFromDocSerial(m_doc_serial_number);

        if (m_file3dm != null)
          return CppFromFile3dm(m_file3dm);

        return base.CppPointer;
      }
    }

#if RHINO_SDK
    /// <summary>
    /// Call this function before making any change to this object (calling a setter) otherwise
    /// changes to the document and undo will not work correctly. Calls to BeginChange() must be
    /// paired with a call to EndChange().
    /// </summary>
    /// <param name="cc">Change context</param>
    /// <since>6.0</since>
    public void BeginChange(Rhino.Render.RenderContent.ChangeContexts cc)
    {
      if (m_doc_serial_number != 0)
      {
        // Document objects that exist in the render settings (Ground Plane, Skylight, etc) override
        // the following virtual function to return a pointer to ON_3dmRenderSettings instead of the
        // native object that this is for. These overrides set cpp_is_rs to true.
        var cpp = RS_CppFromDocSerial(m_doc_serial_number, out bool cpp_is_rs);

        m_cpp_non_const_ptr = BeginChangeImpl(cpp, cc, cpp_is_rs);
      }
      else
      {
        // The File3dm and free-floating cases are the same.
        m_cpp_non_const_ptr = BeginChangeImpl(CppPointer, cc, const_ptr_is_rs: false);
      }

      Debug.Assert(IntPtr.Zero != m_cpp_non_const_ptr);
    }
#endif

    /// <summary>
    /// See BeginChange
    /// </summary>
    /// <returns>true if the object has returned to read-only mode.</returns>
    /// <since>6.0</since>
    public bool EndChange()
    {
      Debug.Assert(IntPtr.Zero != m_cpp_non_const_ptr);
      if (IntPtr.Zero != m_cpp_non_const_ptr)
      {
        if (EndChangeImpl(m_cpp_non_const_ptr, m_doc_serial_number))
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
        // We only need the m_cpp_non_const_ptr to be set when doing document changes (Begin/EndChange).
        if (0 != m_doc_serial_number)
          return m_cpp_non_const_ptr != IntPtr.Zero;

        // In the file3dm and free-floating cases, writing is always OK.
        return true;
      }
    }

    internal uint DocumentSerial_ForObsoleteFunctionality { get => m_doc_serial_number; }

    private IntPtr m_cpp_non_const_ptr = IntPtr.Zero;
    private readonly uint m_doc_serial_number = 0;
    private readonly FileIO.File3dm m_file3dm;
  }
}
