using Rhino.Render.DataSources;
using System;

namespace Rhino.Render
{
  /// <summary>
  /// c# version of IRhRdkPreviewSceneServer eRotationType enum
  /// </summary>
  public enum IRhRdkPreviewSceneServer_eRotationType
  {
    ///<summary>Camera</summary>
    Camera,
    ///<summary>Object</summary>
    Object
  };

  /// <summary>
  /// PreviewAppearance class
  /// </summary>
  public sealed class PreviewAppearance : IDisposable
  {
    private IntPtr m_cpp;
    private MetaData m_md;

    /// <summary>
    /// Previewappearances c++ pointer
    /// </summary>
    public IntPtr CppPointer
    {
      get { return m_cpp; }
    }

    /// <summary>
    /// Previewappearance MetaData
    /// </summary>
    public MetaData MetaData
    {
      get { return m_md; }
    }
    /// <summary>
    /// Constructor for previewappearance
    /// </summary>
    public PreviewAppearance(IntPtr pRenderContent)
    {
      m_cpp = UnsafeNativeMethods.CRhRdkPreviewAppearance_New(pRenderContent);
    }

    /// <summary>
    /// Destructor for previewappearance
    /// </summary>
    ~PreviewAppearance()
    {
      Dispose(false);
    }

    /// <summary>
    /// Dispose for previewappearance
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
        UnsafeNativeMethods.CRhRdkPreviewAppearance_Delete(m_cpp);
        m_cpp = IntPtr.Zero;
      }
    }

    /// <summary>
    /// RotationX
    /// </summary>
    public double RotationX()
    {
      double value = 0;

      if (m_cpp != IntPtr.Zero)
      {
        value = UnsafeNativeMethods.CRhRdkPreviewAppearance_RotationX(m_cpp);
      }

      return value;
    }

    /// <summary>
    /// SetRotationX
    /// </summary>
    public void SetRotationX(double d)
    {
      if (m_cpp != IntPtr.Zero)
      {
        UnsafeNativeMethods.CRhRdkPreviewAppearance_SetRotationX(m_cpp, d);
      }
    }

    /// <summary>
    /// RotationY
    /// </summary>
    public double RotationY()
    {
      double value = 0;

      if (m_cpp != IntPtr.Zero)
      {
        value = UnsafeNativeMethods.CRhRdkPreviewAppearance_RotationY(m_cpp);
      }

      return value;
    }

    /// <summary>
    /// SetRotationY
    /// </summary>
    public void SetRotationY(double d)
    {
      if (m_cpp != IntPtr.Zero)
      {
        UnsafeNativeMethods.CRhRdkPreviewAppearance_SetRotationY(m_cpp, d);
      }
    }

    /// <summary>
    /// Size
    /// </summary>
    public double Size()
    {
      double value = 0;

      if (m_cpp != IntPtr.Zero)
      {
        value = UnsafeNativeMethods.CRhRdkPreviewAppearance_Size(m_cpp);
      }

      return value;
    }

    /// <summary>
    /// SetSize
    /// </summary>
    public void SetSize(double d)
    {
      if (m_cpp != IntPtr.Zero)
      {
        UnsafeNativeMethods.CRhRdkPreviewAppearance_SetSize(m_cpp, d);
      }
    }

    /// <summary>
    /// RotationType
    /// </summary>
    public IRhRdkPreviewSceneServer_eRotationType RotationType()
    {
      IRhRdkPreviewSceneServer_eRotationType value = IRhRdkPreviewSceneServer_eRotationType.Camera;

      if (m_cpp != IntPtr.Zero)
      {
        int integerValue = UnsafeNativeMethods.CRhRdkPreviewAppearance_RotationType(m_cpp);
        value = (IRhRdkPreviewSceneServer_eRotationType)integerValue;
      }

      return value;
    }

    /// <summary>
    /// SetRotationType
    /// </summary>
    public void SetRotationType(IRhRdkPreviewSceneServer_eRotationType type)
    {
      if (m_cpp != IntPtr.Zero)
      {
        UnsafeNativeMethods.CRhRdkPreviewAppearance_SetRotationType(m_cpp, (int)type);
      }
    }

    /// <summary>
    /// Geometry
    /// </summary>
    public PreviewGeometry Geometry()
    {
      if (m_cpp != IntPtr.Zero)
      {
        IntPtr p = UnsafeNativeMethods.CRhRdkPreviewAppearance_Geometry(m_cpp);
        var ge = new PreviewGeometry(p);
        return ge;
      }

      return null;
    }

    /// <summary>
    /// Copy data from MetaData to PreviewAppearance
    /// </summary>
    public void FromMetaData(MetaData md)
    {
      if (m_cpp != IntPtr.Zero)
      {
        m_md = md;
        UnsafeNativeMethods.CRhRdkPreviewAppearance_FromMetaData(m_cpp, md.CppPointer);
      }
    }

    /// <summary>
    /// Copy PreviewAppearance to MetaData
    /// </summary>
    public void ToMetaData()
    {
      if (m_cpp != IntPtr.Zero && MetaData != null)
      {
        UnsafeNativeMethods.CRhRdkPreviewAppearance_ToMetaData(m_cpp, MetaData.CppPointer);
      }
    }

    /// <summary>
    /// Copy PreviewAppearance to MetaData
    /// </summary>
    public void ToMetaData(MetaDataProxy mdp)
    {
      if (m_cpp != IntPtr.Zero && MetaData != null)
      {
        UnsafeNativeMethods.CRhRdkPreviewAppearance_ToMetaData(m_cpp, mdp.CppPointer);
      }
    }

    /// <summary>
    /// Background
    /// </summary>
    public PreviewBackground Background()
    {
      if (m_cpp != IntPtr.Zero)
      {
        IntPtr p = UnsafeNativeMethods.CRhRdkPreviewAppearance_Background(m_cpp);
        var ge = new PreviewBackground(p);
        return ge;
      }

      return null;
    }
    /*
    void SetBackground(PreviewBackground background)
    {
        if (m_cpp != IntPtr.Zero)
        {
            UnsafeNativeMethods.CRhRdkPreviewAppearance_SetBackground(m_cpp, background);
        }
    }
    */

    /// <summary>
    /// Lighting
    /// </summary>
    public PreviewLighting Lighting()
    {
      if (m_cpp != IntPtr.Zero)
      {
        IntPtr p = UnsafeNativeMethods.CRhRdkPreviewAppearance_Lighting(m_cpp);
        var ge = new PreviewLighting(p);
        return ge;
      }

      return null;
    }
    /*
    void SetLighting(PreviewLighting lighting)
    {
        if (m_cpp != IntPtr.Zero)
        {
            UnsafeNativeMethods.CRhRdkPreviewAppearance_SetLighting(m_cpp, lighting);
        }
    }
    */

    // Not yet wrapped
    // void LoadFromDefaults(const IRhRdkPreviewAppearanceDefaults& viewDefaults);

  }

  /// <summary>
  /// ProxyClass for MetaData
  /// </summary>
  public class MetaDataProxy : IDisposable
  {
    private IntPtr m_cpp;

    /// <summary>
    /// MetaDataProxy c++ pointer
    /// </summary>
    public IntPtr CppPointer
    {
      get { return m_cpp; }
    }

    /// <summary>
    /// Constructor for MetaDataProxy
    /// </summary>
    public MetaDataProxy()
    {
      m_cpp = UnsafeNativeMethods.RdkMetaDataProxy_New();
    }

    /// <summary>
    /// Destructor for MetaDataProxy
    /// </summary>
    ~MetaDataProxy()
    {
      Dispose(false);
    }

    /// <summary>
    /// Dispose for MetaDataProxy
    /// </summary>
    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Dispose for MetaDataProxy
    /// </summary>
    void Dispose(bool bDisposing)
    {
      if (m_cpp != IntPtr.Zero)
      {
        UnsafeNativeMethods.RdkMetaDataProxy_Delete(m_cpp);
        m_cpp = IntPtr.Zero;
      }
    }

    /// <summary>
    /// Set Content instance id for meta data
    /// </summary>
    public void SetContentInstanceId(Guid uuid)
    {
      UnsafeNativeMethods.RdkMetaDataProxy_SetContentInstanceId(m_cpp, uuid);
    }
  }
}
