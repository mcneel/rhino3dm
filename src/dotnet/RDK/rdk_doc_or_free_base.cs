
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
    internal DocumentOrFreeFloatingBase() : base() { }
    internal DocumentOrFreeFloatingBase(DocumentOrFreeFloatingBase src) : base(src) { }
    internal DocumentOrFreeFloatingBase(IntPtr nativePtr) : base(nativePtr) { }
    internal DocumentOrFreeFloatingBase(FileIO.File3dm f) : base(IntPtr.Zero) { m_file3dm = f; }

#if RHINO_SDK
    internal DocumentOrFreeFloatingBase(uint doc_sn) : base(IntPtr.Zero)
    {
      m_doc_serial_number = doc_sn;
    }

    internal RenderSettings GetDocumentRenderSettings()
    {
      return GetRenderSettings(m_doc_serial_number);
    }

    internal abstract IntPtr CppFromDocSerial(uint doc_sn);
#endif

    internal abstract IntPtr CppFromFile3dm(FileIO.File3dm f);

    internal static RenderSettings GetRenderSettings(uint doc_sn)
    {
#if RHINO_SDK
      var doc = RhinoDoc.FromRuntimeSerialNumber(doc_sn);
      if (doc != null)
        return doc.RenderSettings;
#endif
      return null;
    }

    internal override IntPtr CppPointer
    {
      get
      {
#if RHINO_SDK
        if (m_doc_serial_number != 0)
          return CppFromDocSerial(m_doc_serial_number);
#endif
        if (m_file3dm != null)
          return CppFromFile3dm(m_file3dm);

        return base.CppPointer;
      }
    }

#if RHINO_SDK
    /// <summary>
    /// </summary>
    /// <since>6.0</since>
    /// <obsolete>8.0</obsolete>
    ///[Obsolete("This is no longer needed and is not implemented")]
    public void BeginChange(Rhino.Render.RenderContent.ChangeContexts cc)
    {
    }

    /// <summary>
    /// </summary>
    /// <since>6.0</since>
    /// <obsolete>8.0</obsolete>
    ///[Obsolete("This is no longer needed and is not implemented")]
    public bool EndChange()
    {
      return false;
    }

    internal uint DocumentSerial_ForObsoleteFunctionality { get => m_doc_serial_number; }

    private readonly uint m_doc_serial_number = 0;
#endif
    private readonly FileIO.File3dm m_file3dm;
  }
}
