#pragma warning disable 1591
#if RHINO_SDK
using System;
using System.Collections.Generic;
using Rhino.PlugIns;
using Rhino.Runtime.InteropWrappers;

namespace Rhino.Render
{
  /// <summary>Quality levels when creating preview images</summary>
  public enum PreviewSceneQuality : int
  {
    /// <summary>Very fast preview. Typically using the internal OpenGL preview generator.</summary>
    [Obsolete("use PreviewSceneQuality.Low")]
    RealtimeQuick = Low,
    /// <summary>Low quality rendering for quick preview.</summary>
    [Obsolete("use PreviewSceneQuality.Low")]
    RefineFirstPass = Low,
    /// <summary>Medium quality rendering for intermediate preview.</summary>
    [Obsolete("use PreviewSceneQuality.Medium")]
    RefineSecondPass = Medium,
    /// <summary>Full quality rendering (quality comes from user settings)</summary>
    [Obsolete("use PreviewSceneQuality.Full")]
    RefineThirdPass = Full,
    /// <summary>
    /// No quality set.
    /// </summary>
    None = 0,
    /// <summary>
    /// Low quality rendering for quick preview.
    /// </summary>
    Low = 1,
    /// <summary>
    /// Medium quality rendering for intermediate preview.
    /// </summary>
    Medium = 2,
    /// <summary>
    /// Intermediate update, always considered better quality than the previous
    /// IntermediateProgressive, but not as high as Full.
    /// </summary>
    IntermediateProgressive = 3,
    /// <summary>
    /// Full quality rendering (quality comes from user settings).
    /// </summary>
    Full = 4,
  }

  /// <summary>
  /// Reason the content preview is being generated
  /// </summary>
  public enum CreatePreviewReason
  {
    ContentChanged = UnsafeNativeMethods.CRhRdkPlugInQuickPreviewReason.ContentChanged,
    ViewChanged = UnsafeNativeMethods.CRhRdkPlugInQuickPreviewReason.ViewChanged,
    RefreshDisplay = UnsafeNativeMethods.CRhRdkPlugInQuickPreviewReason.RefreshDisplay,
    UpdateBitmap = UnsafeNativeMethods.CRhRdkPlugInQuickPreviewReason.UpdateBitmap,
    Other = UnsafeNativeMethods.CRhRdkPlugInQuickPreviewReason.Other,
  }


  /// <summary>Used in RenderPlugIn virtual CreatePreview function</summary>
  public class CreatePreviewEventArgs : EventArgs
  {
    IntPtr m_scene_server_pointer;
    private bool m_is_initialised;
    private bool m_skip_initialisation;
    readonly System.Drawing.Size m_preview_size;
    readonly PreviewSceneQuality m_quality;
    int m_sig;
    DocObjects.ViewportInfo m_viewport;
    readonly CreatePreviewReason m_reason = CreatePreviewReason.Other;

    public PreviewNotification PreviewNotifier { get; private set; }

    internal Guid RuntimeId { get; } = Guid.NewGuid();
    internal CreatePreviewEventArgs(IntPtr sceneServerPointer, IntPtr pPreviewCallbacks, System.Drawing.Size preview_size, PreviewSceneQuality quality, UnsafeNativeMethods.CRhRdkPlugInQuickPreviewReason unsafeReason)
    {
      m_scene_server_pointer = sceneServerPointer;
      PreviewNotifier = new PreviewNotification(pPreviewCallbacks);
      m_preview_size = preview_size;
      m_quality = quality;
      m_is_initialised = false;
      m_skip_initialisation = false;
      switch (unsafeReason)
      {
        case UnsafeNativeMethods.CRhRdkPlugInQuickPreviewReason.ContentChanged:
        case UnsafeNativeMethods.CRhRdkPlugInQuickPreviewReason.Other:
        case UnsafeNativeMethods.CRhRdkPlugInQuickPreviewReason.RefreshDisplay:
        case UnsafeNativeMethods.CRhRdkPlugInQuickPreviewReason.ViewChanged:
        case UnsafeNativeMethods.CRhRdkPlugInQuickPreviewReason.UpdateBitmap:
          m_reason = (CreatePreviewReason) unsafeReason;
          break;
        default:
          throw new Exception("Unknown UnsafeNativeMethods.CRhRdkPlugInQuickPreviewReason");
      }
    }

    /// <summary>
    /// Access to the preview scene server pointer. Used by ChangeQueue when created from
    /// CreatePreviewEventArgs
    /// </summary>
    internal IntPtr SceneServerPtr
    {
      get { return m_scene_server_pointer; }
    }

    /// <summary>
    /// Call this if you don't want the argument to handle data initialisation.
    /// 
    /// This is for use with the ChangeQueue
    /// </summary>
    public void SkipInitialisation()
    {
      m_skip_initialisation = true;
    }

    /// <summary>
    /// Reason the preview is getting generated
    /// </summary>
    public CreatePreviewReason Reason { get { return m_reason; } }

    /// <summary>
    /// Obsolete, will return always null
    /// </summary>
    [Obsolete("No one should really know what they are rendering for, just that they should render.")]
    public RenderContent PreviewContent { get { return null; } }

    /// <summary>
    /// Pixel size of the image that is being requested for the preview scene
    /// </summary>
    public System.Drawing.Size PreviewImageSize
    {
      get { return m_preview_size; }
    }

    /// <summary>
    /// Quality of the preview image that is being requested for the preview scene
    /// </summary>
    public PreviewSceneQuality Quality
    {
      get { return m_quality; }
    }

    /// <summary>
    /// Initially null.  If this image is set, then this image will be used for
    /// the preview.  If never set, the default internal simulation preview will
    /// be used.
    /// </summary>
    public System.Drawing.Bitmap PreviewImage { get; set; }

    /// <summary>
    /// Get set by Rhino if the preview generation should be canceled for this 
    /// </summary>
    public bool Cancel { get; internal set; }

    void Initialize()
    {
      IntPtr pSceneServer = m_scene_server_pointer;

      if (!m_is_initialised && !m_skip_initialisation && pSceneServer != IntPtr.Zero)
      {
        //Pull in the list of objects
        m_scene_objects = new List<SceneObject>();

        UnsafeNativeMethods.Rdk_SceneServer_ResetObjectEnum(pSceneServer);

        IntPtr pObject = UnsafeNativeMethods.Rdk_SceneServer_NextObject(pSceneServer);
        while (pObject != IntPtr.Zero)
        {
          Rhino.Geometry.Mesh mesh = new Rhino.Geometry.Mesh();

          IntPtr pMaterial = UnsafeNativeMethods.Rdk_SceneServer_ObjectDetails(pObject, mesh.NonConstPointer());
          if (pMaterial != IntPtr.Zero)
          {
            SceneObject o = new SceneObject(mesh, RenderContent.FromPointer(pMaterial) as RenderMaterial);
            m_scene_objects.Add(o);
          }

          pObject = UnsafeNativeMethods.Rdk_SceneServer_NextObject(pSceneServer);
        }

        //Now get the lights
        m_scene_lights = new List<Rhino.Geometry.Light>();

        UnsafeNativeMethods.Rdk_SceneServer_ResetLightEnum(pSceneServer);

        var light_pointer = UnsafeNativeMethods.Rdk_SceneServer_NextLight(pSceneServer);
        while (light_pointer != IntPtr.Zero)
        {
          var light = new Geometry.Light(light_pointer, null);
          // The light pointer returned is managed by the scene and should not
          // be deleted by Rhino Common
          light.DoNotDestructOnDispose();
          //UnsafeNativeMethods.Rdk_SceneServer_LightDetails(pLight, light.NonConstPointer());

          m_scene_lights.Add(light);

          light_pointer = UnsafeNativeMethods.Rdk_SceneServer_NextLight(pSceneServer);
        }

        //And then fill in the blanks
        IntPtr pEnvironment = UnsafeNativeMethods.Rdk_SceneServer_Environment(pSceneServer);
        if (pEnvironment != IntPtr.Zero)
        {
          m_environment = RenderContent.FromPointer(pEnvironment) as RenderEnvironment;
        }
        else
        {
          m_environment = null;
        }

        //m_content_instance_id = UnsafeNativeMethods.Rdk_SceneServer_InstanceId(sceneServerPointer);
        m_sig = UnsafeNativeMethods.Rdk_SceneServer_Signature(pSceneServer);

        //Just the view left...

        m_viewport = new Rhino.DocObjects.ViewportInfo();
        UnsafeNativeMethods.Rdk_SceneServer_View(pSceneServer, m_viewport.NonConstPointer());

        using (var string_holder = new StringHolder())
        {
          var string_pointer = string_holder.NonConstPointer();
          UnsafeNativeMethods.Rdk_SceneServer_ContentKind(pSceneServer, string_pointer);
          var conent_kind_string = string_holder.ToString();
          var content_kind = UnsafeNativeMethods.Rdk_RenderContent_KindFromString(conent_kind_string);
          m_content_kind = RenderContent.ConvertConentKind(content_kind);
        }

        m_content_type_id = UnsafeNativeMethods.Rdk_SceneServer_ContentType(pSceneServer);
      }

      //m_scene_server_pointer = IntPtr.Zero;
      m_is_initialised = true;
    }

    /// <summary>Unique Id for this scene.</summary>
    public int Id
    {
      get
      {
        Initialize();
        return m_sig;
      }
    }

    /// <summary>
    /// Description of content that preview is being generated for.
    /// </summary>
    public RenderContentKind ContentKind
    {
      get
      {
        Initialize();
        return m_content_kind;
      }
    }
    private RenderContentKind m_content_kind = RenderContentKind.None;

    /// <summary>
    /// The class Id of content that preview is being generated for.
    /// </summary>
    public Guid ContentTypeId
    {
      get
      {
        Initialize();
        return m_content_type_id;
      }
    }
    private Guid m_content_type_id = Guid.Empty;

    Rhino.Render.RenderEnvironment m_environment;

    /// <summary>
    /// The environment that the previewed object is rendered in.
    /// </summary>
    public Rhino.Render.RenderEnvironment Environment
    {
      get
      {
        Initialize();
        return m_environment;
      }
    }

    //TODO_RDK Get information about the view.
    //virtual bool GetView(ON_Viewport& view) const = 0;

    public class SceneObject
    {
      internal SceneObject(Rhino.Geometry.Mesh mesh, Rhino.Render.RenderMaterial material)
      {
        m_mesh = mesh;
        m_material = material;
      }

      public Rhino.Geometry.Mesh Mesh { get { return m_mesh; } }
      public Rhino.Render.RenderMaterial Material { get { return m_material; } }

      private readonly Rhino.Geometry.Mesh m_mesh;
      private readonly Rhino.Render.RenderMaterial m_material;
    }

    private List<SceneObject> m_scene_objects;
    public List<SceneObject> Objects
    {
      get
      {
        Initialize();
        return m_scene_objects;
      }
    }

    private List<Rhino.Geometry.Light> m_scene_lights;

    public List<Rhino.Geometry.Light> Lights
    {
      get
      {
        Initialize();
        return m_scene_lights;
      }
    }

    public Rhino.DocObjects.ViewportInfo Viewport
    {
      get
      {
        Initialize();
        return m_viewport;
      }
    }
  }

  public class CreateTexture2dPreviewEventArgs : EventArgs
  {
    readonly System.Drawing.Size m_preview_size;
    readonly RenderTexture m_render_texture;

    internal CreateTexture2dPreviewEventArgs(RenderTexture texture, System.Drawing.Size size)
    {
      m_preview_size = size;
      m_render_texture = texture;
    }

    RenderTexture Texture { get { return m_render_texture; } }

    /// <summary>
    /// Pixel size of the image that is being requested for the preview scene
    /// </summary>
    public System.Drawing.Size PreviewImageSize
    {
      get { return m_preview_size; }
    }

    /// <summary>
    /// Initially null.  If this image is set, then this image will be used for
    /// the preview.  If never set, the default internal simulation preview will
    /// be used.
    /// </summary>
    public System.Drawing.Bitmap PreviewImage { get; set; }
  }

  /// <summary>
  /// SceneServerData Usage (Synchronous or Asynchronous)
  /// </summary>
  public enum SceneServerDataUsage 
  {
      Synchronous,
		  Asynchronous,
	}

  /// <summary>
  /// The Scene Server Data used by the PreviewSceneServer
  /// </summary>
  public class SceneServerData
  {
    private IntPtr m_cpp;

    /// <summary>
    /// The CppPointer of SceneServerData
    /// </summary>
    public IntPtr CppPointer
    {
      get { return m_cpp; }
    }

    /// <summary>
    /// Constructor for SceneServerData
    /// </summary>
    public SceneServerData(PreviewGeometry geo, PreviewBackground back, PreviewLighting light, SceneServerDataUsage usage)
    {
      m_cpp = UnsafeNativeMethods.Rdk_SceneServerData_New(geo.CppPointer, back.CppPointer, light.CppPointer, (int)usage);
    }

    /// <summary>
    /// Destructor for SceneServerData
    /// </summary>
    ~SceneServerData()
    {
      Dispose(false);
    }

    /// <summary>
    /// Dispose for SceneServerData
    /// </summary>
    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Dispose for PreivewLighting
    /// </summary>
    void Dispose(bool bDisposing)
    {
      if (m_cpp != IntPtr.Zero)
      {
        UnsafeNativeMethods.Rdk_SceneServerData_Delete(m_cpp);
        m_cpp = IntPtr.Zero;
      }
    }
  }

  /// <summary>
  /// PreviewSceneServer
  /// </summary>
  public class PreviewSceneServer
  {
    private IntPtr m_cpp;

    /// <summary>
    /// The CppPointer of PreviewSceneServer
    /// </summary>
    public IntPtr CppPointer
    {
      get { return m_cpp; }
    }

    /// <summary>
    /// Constructor for PreviewSceneServer
    /// </summary>
    public PreviewSceneServer(IntPtr pPreviewSceneServer)
    {
      m_cpp = pPreviewSceneServer;
    }

    /// <summary>
    /// Destructor for PreviewSceneServer
    /// </summary>
    ~PreviewSceneServer()
    {
      Dispose(false);
    }

    /// <summary>
    /// Dispose for PreviewSceneServer
    /// </summary>
    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Dispose for PreviewSceneServer
    /// </summary>
    void Dispose(bool bDisposing)
    {
      if (m_cpp != IntPtr.Zero)
      {
        m_cpp = IntPtr.Zero;
      }
    }

    /// <summary>
    /// Set Scene Server Rotation
    /// </summary>
    public void ApplyRotation(double X, double Y, IRhRdkPreviewSceneServer_eRotationType type)
    {
      UnsafeNativeMethods.Rdk_SceneServer_ApplyRotation(m_cpp, X, Y, (int)type);
    }
  }
}

#endif

 
