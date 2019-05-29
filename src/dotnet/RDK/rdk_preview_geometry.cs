using System;
using System.Runtime.InteropServices;

namespace Rhino.Render
{
  /// <summary>
  /// PreviewAppearance takes care of constucting and desctrutction of PreivewGeometry
  /// </summary>
  public class PreviewGeometry
  {
    private IntPtr m_cpp;

    /// <summary>
    /// CppPointer for PreviewGeometry
    /// </summary>
    public IntPtr CppPointer
    {
      get { return m_cpp; }
    }

    /// <summary>
    /// Constructor for PreviewGeometry
    /// </summary>
    public PreviewGeometry(IntPtr pPreviewGeometry)
    {
      m_cpp = pPreviewGeometry;
    }

    /// <summary>
    /// SetUpPreview
    /// </summary>
    public void SetUpPreview(IntPtr sceneServerPointer, IntPtr pRenderContent, bool bCopy)
    {
      UnsafeNativeMethods.CRhRdkPreviewGeometry_SetUpPreview(m_cpp, sceneServerPointer, pRenderContent, bCopy);
    }

    /// <summary>
    /// ElementKind
    /// </summary>
    public string ElementKind()
    {
      IntPtr rc = UnsafeNativeMethods.CRhRdkPreviewGeometry_ElementKind(m_cpp);
      return rc == IntPtr.Zero ? string.Empty : Marshal.PtrToStringUni(rc);
    }

  }
}
