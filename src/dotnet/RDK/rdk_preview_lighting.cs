using System;
using System.Runtime.InteropServices;

namespace Rhino.Render
{
  /// <summary>
  /// PreviewAppearance takes care of constucting and desctrutction of PreviewLight
  /// </summary>
  public class PreviewLighting
  {
    private IntPtr m_cpp;

    /// <summary>
    /// CppPointer for PreivewLighting
    /// </summary>
    public IntPtr CppPointer
    {
      get { return m_cpp; }
    }

    /// <summary>
    /// Constructor for PreivewLighting
    /// </summary>
    public PreviewLighting(IntPtr pPreviewLighting)
    {
      m_cpp = pPreviewLighting;
    }

    /// <summary>
    /// SetUpPreview
    /// </summary>
    public void SetUpPreview(IntPtr sceneServerPointer)
    {
      UnsafeNativeMethods.CRhRdkPreviewLighting_SetUpPreview(m_cpp, sceneServerPointer);
    }

    /// <summary>
    /// ElementKind
    /// </summary>
    public string ElementKind()
    {
      IntPtr rc = UnsafeNativeMethods.CRhRdkPreviewLighting_ElementKind(m_cpp);
      return rc == IntPtr.Zero ? string.Empty : Marshal.PtrToStringUni(rc);
    }

  }
}
