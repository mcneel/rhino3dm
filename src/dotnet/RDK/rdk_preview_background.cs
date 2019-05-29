using System;
using System.Runtime.InteropServices;

namespace Rhino.Render
{
  /// <summary>
  /// PreviewBackGround takes care of constucting and desctrutction of PreviewLight
  /// </summary>
  public class PreviewBackground
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
    public PreviewBackground(IntPtr pPreviewBackground)
    {
      m_cpp = pPreviewBackground;
    }

    /// <summary>
    /// EnvironmentInstanceId
    /// </summary>
    public Guid EnvironmentInstanceId()
    {
      return UnsafeNativeMethods.CRhRdkPreviewBackground_EnvironmentInstanceId(m_cpp);
    }

    /// <summary>
    /// SetEnvironmentInstanceId
    /// </summary>
    public void SetEnvironmentInstanceId(Guid guid)
    {
      UnsafeNativeMethods.CRhRdkPreviewBackground_SetEnvironmentInstanceId(m_cpp, guid);
    }

    /// <summary>
    /// SetUpPreview
    /// </summary>
    public void SetUpPreview(IntPtr sceneServerPointer, Guid guid)
    {
      UnsafeNativeMethods.CRhRdkPreviewBackground_SetUpPreview(m_cpp, sceneServerPointer, guid);
    }

    /// <summary>
    /// ElementKind
    /// </summary>
    public string ElementKind()
    {
      IntPtr rc = UnsafeNativeMethods.CRhRdkPreviewBackground_ElementKind(m_cpp);
      return rc == IntPtr.Zero ? string.Empty : Marshal.PtrToStringUni(rc);
    }

  }
}
