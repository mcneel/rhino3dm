#pragma warning disable 1591
#if RHINO_SDK
using System;
using System.Collections.Generic;
using Rhino.Geometry;
using Rhino.DocObjects;

namespace Rhino.Render
{
  public enum RenderPrimitiveType : int
  {
      None = 0,
      Mesh = 1,
      Sphere = 2,
      Plane = 3,
      Box = 4,
      Cone = 5,
  }

  public class RenderPrimitiveList : IDisposable
  {
    #region internals
    private IntPtr m_ptr_custom_render_meshes = IntPtr.Zero;
    internal IntPtr ConstPointer() { return m_ptr_custom_render_meshes; }
    internal IntPtr NonConstPointer() { return m_ptr_custom_render_meshes; }
    #endregion

    // Marked as internal, RhinoObject.GetRenderPrimitiveList will create one
    // but there is currently no reason for anyone else to make one.
    internal RenderPrimitiveList(RhinoObject obj)
    {
      m_ptr_custom_render_meshes = UnsafeNativeMethods.Rdk_CustomMeshes_New(obj==null ? IntPtr.Zero : obj.ConstPointer());
    }

    /* 29 Aug 2013 S. Baer
     * I'm commenting out this constructor until we need it. Pointer tracking for this class
     * is complicated and I don't want to deal with this case yet.
    */
    /// <summary>
    /// This constructor gets called when attaching to an unmanaged pointer that the RDK
    /// passes to a virtual function, there is no clean up need in this case because the
    /// RDK takes care of freeing the pointer when it is done calling the virtual method.
    /// </summary>
    /// <param name="nativePointer"></param>
    internal RenderPrimitiveList(IntPtr nativePointer)
    {
      m_ptr_custom_render_meshes = nativePointer;
    }

    internal void DetachPointer()
    {
      m_ptr_custom_render_meshes = IntPtr.Zero;
    }

    // See comments in RhinoObject.GetRenderPrimitiveList concerning provider Id
    //public Guid ProviderId
    //{
    //    get { return UnsafeNativeMethods.Rdk_CustomMeshes_ProviderId(ConstPointer()); }
    //    set { UnsafeNativeMethods.Rdk_CustomMeshes_SetProviderId(NonConstPointer(), value); }
    //}

    /// <summary>
    /// Call this method to get a array of meshes, all primitives will get
    /// meshed and the meshes will get included in the returned array.
    /// </summary>
    /// <returns>
    /// Return an array of meshes from this list, this will convert all
    /// primitives to meshes.
    /// </returns>
    public Mesh[] ToMeshArray()
    {
      var mesh_list = new List<Mesh>(Count);
      for (var i = 0; i < Count; i++)
      {
        var mesh = Mesh(i);
        mesh_list.Add(mesh);
      }
      return mesh_list.ToArray();
    }

    /// <summary>
    /// Call this method to see if there are any RenderMaterials associated
    /// with the meshes.  Each primitive can optionally have a RenderMaterial
    /// associated with it, if the RenderMaterial is null then check for a
    /// RhinoObject.RenderMaterial.
    /// </summary>
    /// <returns>
    /// Return an array that of the same size as the ToMeshArray() containing
    /// the RenderMaterial associated with the mesh, may contain null entries
    /// if there is no RenderMaterial associated with the custom mesh.
    /// </returns>
    public RenderMaterial[] ToMaterialArray()
    {
      var material_list = new List<RenderMaterial>(Count);
      for (var i = 0; i < Count; i++)
      {
        var material = Material(i);
        material_list.Add(material);
      }
      return material_list.ToArray();
    }
    /// <summary>
    /// Add mesh and material.
    /// </summary>
    /// <param name="mesh">Mesh to add.</param>
    /// <param name="material">
    /// Material to add, may be null if not needed.
    /// </param>
    public void Add(Mesh mesh, RenderMaterial material)
    {
      var material_pointer = (null == material ? IntPtr.Zero : material.ConstPointer());
      var pointer = NonConstPointer();
      UnsafeNativeMethods.Rdk_CustomMeshes_AddMesh(pointer, mesh.ConstPointer(), material_pointer);
    }

    /// <summary>
    /// Add mesh and material.
    /// </summary>
    /// <param name="mesh">Mesh to add.</param>
    /// <param name="material">
    /// <param name="t">Transformation of this mesh.</param>
    /// Material to add, may be null if not needed.
    /// </param>
    public void Add(Mesh mesh, RenderMaterial material, Transform t)
    {
      var material_pointer = (null == material ? IntPtr.Zero : material.ConstPointer());
      var pointer = NonConstPointer();

      mesh.DoNotDestructOnDispose();

      UnsafeNativeMethods.Rdk_CustomMeshes_AddMeshWithInstancingSupport(pointer, mesh.ConstPointer(), material_pointer, t);
    }

    /// <summary>
    /// Add primitive sphere and material.
    /// </summary>
    /// <param name="sphere">Sphere to add.</param>
    /// <param name="material">
    /// Material to add, may be null if not needed.
    /// </param>
    public void Add(Sphere sphere, RenderMaterial material)
    {
      var material_pointer = (null == material ? IntPtr.Zero : material.ConstPointer());
      var pointer = NonConstPointer();
      UnsafeNativeMethods.Rdk_CustomMeshes_AddSphere(pointer,
                                                     sphere.Center,
                                                     sphere.EquatorialPlane.XAxis,
                                                     sphere.EquatorialPlane.YAxis,
                                                     sphere.Radius,
                                                     material_pointer);
    }
    /// <summary>
    /// Add primitive cone and material.
    /// </summary>
    /// <param name="cone">Cone to add.</param>
    /// <param name="truncation">
    /// The plane used to cut the cone (the non-apex end is kept). Should be
    /// equal to cone.plane if not truncated.
    /// </param>
    /// <param name="material">
    /// Material to add, may be null if not needed.
    /// </param>
    public void Add(Cone cone, Plane truncation, RenderMaterial material)
    {
      var material_pointer = (null == material ? IntPtr.Zero : material.ConstPointer());
      var pointer = NonConstPointer();
      UnsafeNativeMethods.Rdk_CustomMeshes_AddCone(pointer,
                                                   cone.BasePoint, 
                                                   cone.Plane.XAxis, 
                                                   cone.Plane.YAxis, 
                                                   cone.Height,
                                                   cone.Radius,
                                                   truncation.Origin, 
                                                   truncation.XAxis, 
                                                   truncation.YAxis,
                                                   material_pointer);
    }
    /// <summary>
    /// Add primitive plane and material.
    /// </summary>
    /// <param name="plane">Plane to add.</param>
    /// <param name="material">
    /// Material to add, may be null if not needed.
    /// </param>
    public void Add(PlaneSurface plane, RenderMaterial material)
    {
      var material_pointer = (null == material ? IntPtr.Zero : material.ConstPointer());
      var pointer = NonConstPointer();
      UnsafeNativeMethods.Rdk_CustomMeshes_AddPlane(pointer, plane.ConstPointer(), material_pointer);
    }
    /// <summary>
    /// Add primitive box and material.
    /// </summary>
    /// <param name="box">Box to add.</param>
    /// <param name="material">
    /// Material to add, may be null if not needed.
    /// </param>
    public void Add(Box box, RenderMaterial material)
    {
      var material_pointer = (null == material ? IntPtr.Zero : material.ConstPointer());
      var pointer = NonConstPointer();
      UnsafeNativeMethods.Rdk_CustomMeshes_AddBox(pointer, 
                                                  box.Plane.Origin, 
                                                  box.Plane.XAxis, 
                                                  box.Plane.YAxis, 
                                                  box.X.Min, box.X.Max, 
                                                  box.Y.Min, box.Y.Max, 
                                                  box.Z.Min, box.Z.Max,
                                                  material_pointer);
    }
    /// <summary>
    /// Returns true if the texture mapping will be taken from the Rhino
    /// object otherwise; the texture mapping will use the texture coordinates
    /// on the mesh only.
    /// </summary>
    public bool UseObjectsMappingChannels
    {
      get
      {
        return UnsafeNativeMethods.Rdk_CustomMeshes_UseObjectsMappingChannels(ConstPointer());
      }
      set
      {
        UnsafeNativeMethods.Rdk_CustomMeshes_SetUseObjectsMappingChannels(NonConstPointer(), value);
      }
    }
    /// <summary>
    /// Number of meshes in this list
    /// </summary>
    public int Count
    {
      get { return UnsafeNativeMethods.Rdk_CustomMeshes_Count(ConstPointer()); }
    }
    /// <summary>
    /// Type of primitive object at this index.
    /// </summary>
    /// <param name="index">
    /// The zero based index of the item in the list.  Valid values are greater
    /// than or equal to 0 and less than Count.
    /// </param>
    /// <returns>
    /// Primitive type of the item at this index.
    /// </returns>
    public RenderPrimitiveType PrimitiveType(int index)
    {
      return (RenderPrimitiveType)UnsafeNativeMethods.Rdk_CustomMeshes_PrimitiveType(ConstPointer(), index);
    }
    /// <summary>
    /// Get the mesh for the primitive at the specified index. If the item at
    /// this index is a primitive type other than a mesh then it mesh
    /// representation is returned.
    /// </summary>
    /// <param name="index">
    /// The zero based index of the item in the list.  Valid values are greater
    /// than or equal to 0 and less than Count.
    /// </param>
    /// <returns>
    /// Returns the mesh for the primitive at the specified index. If the item
    /// at this index is a primitive type other than a mesh then it mesh
    /// representation is returned.
    /// </returns>
    public Mesh Mesh(int index)
    {
      var const_ptr_mesh = UnsafeNativeMethods.Rdk_CustomMeshes_Mesh(ConstPointer(), index);
      if (const_ptr_mesh != IntPtr.Zero)
      {
        MeshHolder mh = new MeshHolder(this, index);
        return new Mesh(const_ptr_mesh, mh);
      }
      return null;
    }
    /// <summary>
    /// Get the mesh for the primitive at the specified index. If the item at
    /// this index is a primitive type other than a mesh then it mesh
    /// representation is returned.
    /// </summary>
    /// <param name="index">
    /// The zero based index of the item in the list.  Valid values are greater
    /// than or equal to 0 and less than Count.
    /// </param>
    /// <param name="instance_transform">
    /// Receives the transformation of this mesh.
    /// </param>
    /// <returns>
    /// Returns the mesh for the primitive at the specified index. If the item
    /// at this index is a primitive type other than a mesh then it mesh
    /// representation is returned.
    /// </returns>
    public Mesh MeshInstance(int index, out Transform instance_transform)
    {
      instance_transform = new Transform(1.0);
      var const_ptr_mesh = UnsafeNativeMethods.Rdk_CustomMeshes_MeshInstance(ConstPointer(), index, ref instance_transform);
      if (const_ptr_mesh != IntPtr.Zero)
      {
        MeshHolder mh = new MeshHolder(this, index);
        return new Mesh(const_ptr_mesh, mh);
      }
      return null;
    }
    /// <summary>
    /// Call this method to get a box at the specified index.
    /// </summary>
    /// <param name="index">
    /// The zero based index of the item in the list.  Valid values are greater
    /// than or equal to 0 and less than Count.
    /// </param>
    /// <param name="sphere">
    /// Will contain the sphere at the requested index if the index is in range
    /// and the primitive at the requested index is a box.
    /// </param>
    /// <returns>
    /// Return true if the index is in range and the primitive at the requested
    /// index is a box otherwise returns false.
    /// </returns>
    public bool TryGetSphere(int index, out Sphere sphere)
    {
      var origin = new Point3d();
      var xaxis = new Vector3d();
      var yaxis = new Vector3d();
      var radius = 0.0;

      if (UnsafeNativeMethods.Rdk_CustomMeshes_Sphere(ConstPointer(), index, ref origin, ref xaxis, ref yaxis, ref radius))
      {
        sphere = new Sphere(new Plane(origin, xaxis, yaxis), radius);
        return true;
      }
      sphere = new Sphere();
      return false;
    }
    /// <summary>
    /// Call this method to get a box at the specified index.
    /// </summary>
    /// <param name="index">
    /// The zero based index of the item in the list.  Valid values are greater
    /// than or equal to 0 and less than Count.
    /// </param>
    /// <param name="box">
    /// Will contain the box at the requested index if the index is in range
    /// and the primitive at the requested index is a box.
    /// </param>
    /// <returns>
    /// Return true if the index is in range and the primitive at the requested
    /// index is a box otherwise returns false.
    /// </returns>
    public bool TryGetBox(int index, out Box box)
    {
      var origin = new Point3d();
      var xaxis = new Vector3d();
      var yaxis = new Vector3d();
      var min_x = 0.0;
      var max_x = 0.0;
      var min_y = 0.0;
      var max_y = 0.0;
      var min_z = 0.0;
      var max_z = 0.0;

      if (UnsafeNativeMethods.Rdk_CustomMeshes_Box(ConstPointer(), index, ref origin, ref xaxis, ref yaxis, ref min_x, ref max_x, ref min_y, ref max_y, ref min_z, ref max_z))
      {
        box = new Box(new Plane(origin, xaxis, yaxis),
                      new Interval(min_x, max_x),
                      new Interval(min_y, max_y),
                      new Interval(min_z, max_z));
        return true;
      }
      box = new Box();
      return false;
    }
    /// <summary>
    /// Call this method to get a box at the specified index.
    /// </summary>
    /// <param name="index">
    /// The zero based index of the item in the list.  Valid values are greater
    /// than or equal to 0 and less than Count.
    /// </param>
    /// <param name="plane">
    /// Will contain the plane at the requested index if the index is in range
    /// and the primitive at the requested index is a plane.
    /// </param>
    /// <returns>
    /// Return true if the index is in range and the primitive at the requested
    /// index is a plane otherwise returns false.
    /// </returns>
    public bool TryGetPlane(int index, out PlaneSurface plane)
    {
      var origin = new Point3d();
      var xaxis = new Vector3d();
      var yaxis = new Vector3d();
      var min_x = 0.0;
      var max_x = 0.0;
      var min_y = 0.0;
      var max_y = 0.0;

      if (UnsafeNativeMethods.Rdk_CustomMeshes_Plane(ConstPointer(), index, ref origin, ref xaxis, ref yaxis, ref min_x, ref max_x, ref min_y, ref max_y))
      {
        plane = new PlaneSurface(new Plane(origin, xaxis, yaxis),
                                 new Interval(min_x, max_x),
                                 new Interval(min_y, max_y));
        return true;
      }
      plane = new PlaneSurface(new Plane(Point3d.Origin, Vector3d.XAxis, Vector3d.YAxis),
                               new Interval(0.0, 0.0),
                               new Interval(0.0, 0.0));
      return false;
    }
    /// <summary>
    /// Call this method to get a box at the specified index.
    /// </summary>
    /// <param name="index">
    /// The zero based index of the item in the list.  Valid values are greater
    /// than or equal to 0 and less than Count.
    /// </param>
    /// <param name="cone">
    /// Will contain the cone at the requested index if the index is in range
    /// and the primitive at the requested index is a box.
    /// </param>
    /// <param name="truncation">
    /// </param>
    /// <returns>
    /// Return true if the index is in range and the primitive at the requested
    /// index is a box otherwise returns false.
    /// </returns>
    public bool TryGetCone(int index, out Cone cone, out Plane truncation)
    {
      var origin = new Point3d();
      var xaxis = new Vector3d();
      var yaxis = new Vector3d();

      var height = 0.0;
      var radius = 0.0;

      var t_origin = new Point3d();
      var t_xaxis = new Vector3d();
      var t_yaxis = new Vector3d();


      if (UnsafeNativeMethods.Rdk_CustomMeshes_Cone(ConstPointer(), index, 
                                                    ref origin, 
                                                    ref xaxis, 
                                                    ref yaxis, 
                                                    ref height,
                                                    ref radius,
                                                    ref t_origin,
                                                    ref t_xaxis,
                                                    ref t_yaxis))
      {
        cone = new Cone(new Plane(origin, xaxis, yaxis), height, radius);
        truncation = new Plane(t_origin, t_xaxis, t_yaxis);
        return true;
      }
      cone = new Cone();
      truncation = new Plane();
      return false;
    }
    /// <summary>
    /// Call this method to get the render material associated with the mesh at
    /// the specified index.  Will return null if there is no
    /// material associated with the requested mesh.
    /// </summary>
    /// <param name="index">
    /// The zero based index of the item in the list.  Valid values are greater
    /// than or equal to 0 and less than Count.
    /// </param>
    /// <returns>
    /// If there is a render material associated at the requested index then
    /// the material is returned otherwise null is returned.
    /// </returns>
    public RenderMaterial Material(int index)
    {
      var material_pointer = UnsafeNativeMethods.Rdk_CustomMeshes_Material(ConstPointer(), index);
      if (material_pointer != IntPtr.Zero)
      {
        var material = RenderContent.FromPointer(material_pointer) as RenderMaterial;
        return material;
      }
      return null;
    }
    /// <summary>
    /// The Rhino object associated with this list
    /// </summary>
    public RhinoObject RhinoObject
    {
      get
      {
        var object_pointer = UnsafeNativeMethods.Rdk_CustomMeshes_Object(ConstPointer());
        return (object_pointer != IntPtr.Zero ? RhinoObject.CreateRhinoObjectHelper(object_pointer) : null);
      }
    }
    /// <summary>
    /// Convert mesh quad faces to triangle faces.
    /// </summary>
    public void ConvertMeshesToTriangles()
    {
      UnsafeNativeMethods.Rdk_CustomMeshes_ConvertMeshesToTriangles(NonConstPointer());
    }
    /// <summary>
    /// Remove all primitives from this list
    /// </summary>
    public void Clear()
    {
      UnsafeNativeMethods.Rdk_CustomMeshes_Clear(NonConstPointer());
    }

    public Transform GetInstanceTransform(int index)
    {
      Transform xform = new Transform();
      UnsafeNativeMethods.Rdk_CustomMeshes_GetInstanceTransform(ConstPointer(), index, ref xform);
      return xform;
    }

    public void SetInstanceTransform(int index, Transform xform)
    {
      UnsafeNativeMethods.Rdk_CustomMeshes_SetInstanceTransform(NonConstPointer(), index, xform);
    }

    public bool AutoDeleteMeshesOn()
    {
      return UnsafeNativeMethods.Rdk_CustomMeshes_AutoDeleteMeshesOn(ConstPointer());
    }

    public bool AutoDeleteMaterialsOn()
    {
      return UnsafeNativeMethods.Rdk_CustomMeshes_AutoDeleteMaterialsOn(ConstPointer());
    }

    #region IDisposable pattern implementation

    bool m_disposed;
    int m_dependency_count = 0;

    internal void IncrementDependencyCount()
    {
      m_dependency_count++;
    }
    internal void DecrementDependencyCount()
    {
      m_dependency_count--;
      DoCleanUpTest();
    }

    void DoCleanUpTest()
    {
      if (m_disposed && m_dependency_count < 1 && m_ptr_custom_render_meshes != IntPtr.Zero)
      {
        UnsafeNativeMethods.Rdk_CustomMeshes_Delete(m_ptr_custom_render_meshes);
        m_ptr_custom_render_meshes = IntPtr.Zero;
      }
    }

    ~RenderPrimitiveList()
    {
      Dispose(false);
    }
    public void Dispose()
    {
      Dispose(true);
    }
    private void Dispose(bool disposing)
    {
      m_disposed = true;
      DoCleanUpTest();
    }
    #endregion
  }

  // John Morse:
  // Per conversation with Andy this is going to get removed soon, the events will get added
  // to RhinoDoc or the objects table
  //class CustomRenderMeshManagerDepricated
  //{
  //  public static RenderPrimitiveList PreviousMeshes()
  //  {
  //    IntPtr pMeshes = UnsafeNativeMethods.Rdk_CRMManager_EVF("PreviousMeshes", IntPtr.Zero);
  //    if (pMeshes != IntPtr.Zero)
  //    {
  //      return new RenderPrimitiveList(pMeshes);
  //    }
  //    return null;
  //  }
  //  public static void ForceObjectIntoPreviewCache(RhinoObject obj)
  //  {
  //    UnsafeNativeMethods.Rdk_CRMManager_EVF("ForceObjectIntoPreviewCache", obj.ConstPointer());
  //  }
  //}

  /// <summary>
  /// You must call CustomRenderMeshProvider.RegisterProviders() from your
  /// plug-ins OnLoad override for each assembly containing a custom mesh
  /// provider.  Only publicly exported classes derived from
  /// CustomRenderMeshProvider with a public constructor that has no parameters
  /// will get registered.
  /// </summary>
  /// 
  [Obsolete]
  public abstract class CustomRenderMeshProvider
  {
    public static Guid EdgeSofteningId
    {
      get
      {
        return UnsafeNativeMethods.Rdk_CRMId(0);
      }
    }

    public static Guid DisplacementId
    {
      get
      {
        return UnsafeNativeMethods.Rdk_CRMId(1);
      }
    }

    public static Guid CurvePipingId
    {
      get
      {
        return UnsafeNativeMethods.Rdk_CRMId(2);
      }
    }

    public static Guid ShutLiningId
    {
      get
      {
        return UnsafeNativeMethods.Rdk_CRMId(3);
      }
    }

    public static Guid ThickeningId
    {
      get
      {
        return UnsafeNativeMethods.Rdk_CRMId(4);
      }
    }


    /// <summary>
    /// Default constructor
    /// </summary>
    protected CustomRenderMeshProvider()
    {
    }

    #region abstract properties
    /// <summary>
    /// The name of the provider for UI display.
    /// </summary>
    public abstract String Name { get; }
    #endregion abstract properties

    #region abstract methods
    /// <summary>
    /// Determines if custom render meshes will be built for a particular object.
    /// </summary>
    /// <param name="vp">The viewport being rendered.</param>
    /// <param name="obj">The Rhino object of interest.  This can be null in the case where document meshes (not associated with any object) are being requested.</param>
    /// <param name="requestingPlugIn">UUID of the RDK plug-in requesting the meshes.</param>
    /// <param name="preview">Type of mesh to build.</param>
    /// <returns>true if custom meshes will be built.</returns>
    public abstract bool WillBuildCustomMeshes(ViewportInfo vp, RhinoObject obj, Guid requestingPlugIn, bool preview);
    /// <summary>
    /// Build custom render mesh(es).
    /// </summary>
    /// <param name="vp">The viewport being rendered.</param>
    /// <param name="objMeshes">The meshes class to populate with custom meshes.</param>
    /// <param name="requestingPlugIn">UUID of the RDK plug-in requesting the meshes.</param>
    /// <param name="meshType">Type of mesh to build.</param>
    /// <returns>true if operation was successful.</returns>
    public abstract bool BuildCustomMeshes(ViewportInfo vp, RenderPrimitiveList objMeshes, Guid requestingPlugIn, bool meshType);
    #endregion abstract methods

    #region members
    private int m_runtime_serial_number;
    private static int g_current_serial_number = 1;
    private static readonly Dictionary<int, CustomRenderMeshProvider> g_all_providers = new Dictionary<int, CustomRenderMeshProvider>();
    #endregion members

    #region private methods
    /// <summary>
    /// RegisterProviders calls this method to create a runtime unmanaged
    /// pointer for each custom provider registered.
    /// </summary>
    /// <param name="pluginId"></param>
    /// <returns></returns>
    private IntPtr CreateCppObject(Guid pluginId)
    {
      var type = GetType();
      var provider_id = type.GUID;
      m_runtime_serial_number = g_current_serial_number++;
      
      return UnsafeNativeMethods.CRhCmnCRMProvider_New(m_runtime_serial_number, provider_id, Name, pluginId);
    }
    /// <summary>
    /// This method is called by the virtual hooks to get the runtime managed
    /// object from a serial number.
    /// </summary>
    /// <param name="serialNumber"></param>
    /// <returns></returns>
    private static CustomRenderMeshProvider FromSerialNumber(int serialNumber)
    {
      CustomRenderMeshProvider rc;
      g_all_providers.TryGetValue(serialNumber, out rc);
      return rc;
    }
    #endregion private methods

    #region public static methods
    /// <summary>
    /// Call this method once from your plug-ins OnLoad override for each
    /// assembly containing a custom mesh provider.  Only publicly exported
    /// classes derived from CustomRenderMeshProvider with a public constructor
    /// that has no parameters will get registered.
    /// </summary>
    /// <param name="assembly">
    /// Assembly to search for valid CustomRenderMeshProvider derived classes.
    /// </param>
    /// <param name="pluginId">
    /// The plug-in that owns the custom mesh providers.
    /// </param>
    public static void RegisterProviders(System.Reflection.Assembly assembly, Guid pluginId)
    {
      var plugin = PlugIns.PlugIn.GetLoadedPlugIn(pluginId);
      if (plugin == null)
        return;

      var exported_types = assembly.GetExportedTypes();
      var provider_types = new List<Type>();
      var custom_type = typeof(CustomRenderMeshProvider);
      var options = new Type[] {};

      foreach (var type in exported_types)
        if (!type.IsAbstract && type.IsSubclassOf(custom_type) && type.GetConstructor(options) != null)
          provider_types.Add(type);

      if (provider_types.Count == 0)
        return;

      var rdk_plugin = RdkPlugIn.GetRdkPlugIn(plugin);
      if (rdk_plugin == null)
        return;

      foreach (var type in provider_types)
      {
        var provider = Activator.CreateInstance(type) as CustomRenderMeshProvider;
        if (provider == null) continue;
        var cpp_object = provider.CreateCppObject(pluginId);
        if (cpp_object == IntPtr.Zero) continue;
        g_all_providers.Add(provider.m_runtime_serial_number, provider);
        UnsafeNativeMethods.Rdk_RegisterCRMProvider(cpp_object);
      }
    }

    /// <summary>
    /// Call this method if your render meshes change.
    /// </summary>
    [Obsolete("Use version that requires a document")]
    public static void AllObjectsChanged() // 2nd July 2014, John Croudy. Added document parameter.
    {
      var doc = RhinoDoc.ActiveDoc;
      AllObjectsChanged(doc);
    }

    /// <summary>
    /// Call this method if your render meshes change.
    /// </summary>
    public static void AllObjectsChanged(RhinoDoc doc) // 2nd July 2014, John Croudy. Added document parameter.
    {
      UnsafeNativeMethods.Rdk_CRMManager_RhinoDocumentChanged(doc.RuntimeSerialNumber);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="doc"></param>
    public static void ObjectChanged(RhinoDoc doc, RhinoObject obj)
    {
      UnsafeNativeMethods.Rdk_CRMManager_RhinoObjectChanged(doc.RuntimeSerialNumber, obj.ConstPointer());
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="doc"></param>
    public static void DocumentBasedMeshesChanged(RhinoDoc doc)
    {
        UnsafeNativeMethods.Rdk_CRMManager_RhinoObjectChanged(doc.RuntimeSerialNumber, IntPtr.Zero);
    }
    #endregion public static methods

    #region virtual methods
    /// <summary>
    /// Returns a bounding box for the custom render meshes for the given object.
    /// </summary>
    /// <param name="vp">The viewport being rendered.</param>
    /// <param name="obj">The Rhino object of interest.  This can be null in the case where document meshes (not associated with any object) are being requested.</param>
    /// <param name="requestingPlugIn">UUID of the RDK plug-in requesting the meshes.</param>
    /// <param name="preview">Type of mesh to build.</param>
    /// <returns>A bounding box value.</returns>
    public virtual BoundingBox BoundingBox(ViewportInfo vp, RhinoObject obj, Guid requestingPlugIn, bool preview)
    {
      var min = new Point3d();
      var max = new Point3d();

      var da = new Rhino.Display.DisplayPipelineAttributes(IntPtr.Zero);

      var doc = (obj == null) ? null : obj.Document;

      if (UnsafeNativeMethods.Rdk_RMPBoundingBoxImpl(m_runtime_serial_number, vp.ConstPointer(), obj == null ? IntPtr.Zero : obj.ConstPointer(), (doc==null) ? 0 : doc.RuntimeSerialNumber, requestingPlugIn, preview ? da.ConstPointer() : IntPtr.Zero, ref min, ref max))
      {
        return new BoundingBox(min, max);
      }
      GC.KeepAlive(vp);
      GC.KeepAlive(obj);

      return new BoundingBox();
    }

    public BoundingBox BoundingBox(ViewportInfo vp, RhinoObject obj, RhinoDoc doc, Guid requestingPlugIn, Rhino.Display.DisplayPipelineAttributes attrs)
    {
      var min = new Point3d();
      var max = new Point3d();

      if (UnsafeNativeMethods.Rdk_RMPBoundingBoxImpl(m_runtime_serial_number, vp.ConstPointer(), obj == null ? IntPtr.Zero : obj.ConstPointer(), doc.RuntimeSerialNumber, requestingPlugIn, attrs.ConstPointer(), ref min, ref max))
      {
        return new BoundingBox(min, max);
      }
      GC.KeepAlive(vp);
      GC.KeepAlive(obj);
      GC.KeepAlive(attrs);

      return new BoundingBox();
    }
    #endregion virtual methods

    #region callbacks
    internal delegate void CrmProviderDeleteThisCallback(int serialNumber);
    internal static CrmProviderDeleteThisCallback DeleteThis = OnDeleteRhCmnCrmProvider;
    static private void OnDeleteRhCmnCrmProvider(int serialNumber)
    {
      try
      {
        var p = FromSerialNumber(serialNumber);
        if (p == null) return;
        p.m_runtime_serial_number = -1;
        g_all_providers.Remove(serialNumber);
      }
      catch (Exception ex)
      {
        Runtime.HostUtils.ExceptionReport(ex);
      }
    }
    //-------------------------------------------------------------------------
    internal delegate int CrmProviderWillBuildCallback(int serialNumber, IntPtr pViewport, IntPtr pObject, uint docSerial, Guid plugInId, IntPtr pAttributes);
    internal static CrmProviderWillBuildCallback WillBuild = OnWillBuild;
    static private int OnWillBuild(int serialNumber, IntPtr pViewport, IntPtr pObject, uint docSerial, Guid plugInId, IntPtr pAttributes)
    {
      try
      {
        var p = FromSerialNumber(serialNumber);
        if (p != null)
        {
          var p2 = p as CustomRenderMeshProvider2;
          if (null != p2)
          {
            var da = pAttributes!=IntPtr.Zero ? new Rhino.Display.DisplayPipelineAttributes(pAttributes, true) : null;
            return p2.WillBuildCustomMeshes(new ViewportInfo(pViewport), RhinoObject.CreateRhinoObjectHelper(pObject), RhinoDoc.FromRuntimeSerialNumber(docSerial), plugInId, da) ? 1 : 0;
          }
          else
          {
            return p.WillBuildCustomMeshes(new ViewportInfo(pViewport), RhinoObject.CreateRhinoObjectHelper(pObject), plugInId, pAttributes != IntPtr.Zero) ? 1 : 0;
          }
        }
      }
      catch (Exception ex)
      {
        Runtime.HostUtils.ExceptionReport(ex);
      }
      return 0;
    }
    //-------------------------------------------------------------------------
    internal delegate int CrmProviderBuildCallback(int serialNumber, IntPtr viewportPointer, uint docSerial, IntPtr customRenderMeshPointer, Guid plugInId, IntPtr pAttributes);
    internal static CrmProviderBuildCallback Build = OnBuild;
    static private int OnBuild(int serialNumber, IntPtr viewportPointer, uint docSerial, IntPtr customRenderMeshPointer, Guid plugInId, IntPtr pAttributes)
    {
      var list = new RenderPrimitiveList(customRenderMeshPointer);
      try
      {
        var p = FromSerialNumber(serialNumber);
        if (p == null) return 0;
        // Make a temporary list, you MUST CALL DetachPointer() to avoid a double delete
        
        var p2 = p as CustomRenderMeshProvider2;
        bool success = false;

        if (null != p2)
        {
          var da = pAttributes != IntPtr.Zero ? new Rhino.Display.DisplayPipelineAttributes(pAttributes, true) : null;
          success = p2.BuildCustomMeshes(new ViewportInfo(viewportPointer), RhinoDoc.FromRuntimeSerialNumber(docSerial), list, plugInId, da);
        }
        else
        {
          success = p.BuildCustomMeshes(new ViewportInfo(viewportPointer), list, plugInId, pAttributes != IntPtr.Zero);
        }

        return (success ? 1 : 0);
      }
      catch (Exception ex)
      {
        Runtime.HostUtils.ExceptionReport(ex);
      }
      finally
      {
        // This MUST be called because we created the list and attached it to a
        // pointer owned by the display.
        list.DetachPointer();
      }
      return 0;
    }
    //-------------------------------------------------------------------------
    internal delegate int CrmProviderBBoxCallback(int serialNumber, IntPtr pViewport, IntPtr pObject, uint docSerial, Guid plugInId, IntPtr pAttributes, ref Point3d min, ref Point3d max);
    internal static CrmProviderBBoxCallback BBox = OnBBox;
    static int OnBBox(int serialNumber, IntPtr pViewport, IntPtr pObject, uint docSerial, Guid plugInId, IntPtr pAttributes, ref Point3d min, ref Point3d max)
    {
      try
      {
        var provider = FromSerialNumber(serialNumber);
        if (provider != null)
        {
          var p2 = provider as CustomRenderMeshProvider2;
          Geometry.BoundingBox bbox;
          if (null != p2)
          {
            var da = new Rhino.Display.DisplayPipelineAttributes(pAttributes, true);
            bbox = p2.BoundingBox(new ViewportInfo(pViewport), RhinoObject.CreateRhinoObjectHelper(pObject), RhinoDoc.FromRuntimeSerialNumber(docSerial), plugInId, da);
          }
          else
          {
            bbox = provider.BoundingBox(new ViewportInfo(pViewport), RhinoObject.CreateRhinoObjectHelper(pObject), plugInId, pAttributes != null);
          }

          min = bbox.Min;
          max = bbox.Max;
          return 1;
        }
      }
      catch (Exception ex)
      {
        Runtime.HostUtils.ExceptionReport(ex);
      }
      return 0;
    }

    #endregion callbacks
  }

#pragma warning disable 612
  public abstract class CustomRenderMeshProvider2 : CustomRenderMeshProvider
  {
    public override bool WillBuildCustomMeshes(ViewportInfo vp, RhinoObject obj, Guid requestingPlugIn, bool preview)
    {
      return WillBuildCustomMeshes(vp, obj, obj==null ? null : obj.Document, requestingPlugIn, preview ? new Rhino.Display.DisplayPipelineAttributes(IntPtr.Zero) : null);
    }

    public override bool BuildCustomMeshes(ViewportInfo vp, RenderPrimitiveList objMeshes, Guid requestingPlugIn, bool preview)
    {
      var obj = null==objMeshes ? null : objMeshes.RhinoObject;
      return BuildCustomMeshes(vp, obj == null ? null : obj.Document, objMeshes, requestingPlugIn, preview ? new Rhino.Display.DisplayPipelineAttributes(IntPtr.Zero) : null);
    }

    public override BoundingBox BoundingBox(ViewportInfo vp, RhinoObject obj, Guid requestingPlugIn, bool preview)
    {
      return BoundingBox(vp, obj, obj == null ? null : obj.Document, requestingPlugIn, preview ? new Rhino.Display.DisplayPipelineAttributes(IntPtr.Zero) : null);
    }

    public abstract bool WillBuildCustomMeshes(ViewportInfo vp, RhinoObject obj, RhinoDoc doc, Guid requestingPlugIn, Rhino.Display.DisplayPipelineAttributes attrs);
    public abstract bool BuildCustomMeshes(ViewportInfo vp, RhinoDoc doc, RenderPrimitiveList objMeshes, Guid requestingPlugIn, Rhino.Display.DisplayPipelineAttributes attrs);
    
    public new virtual BoundingBox BoundingBox(ViewportInfo vp, RhinoObject obj, RhinoDoc doc, Guid requestingPlugIn, Rhino.Display.DisplayPipelineAttributes attrs)
    {
      return base.BoundingBox(vp, obj, doc, requestingPlugIn, attrs);
    }
  }
}
#pragma warning restore 612

#endif
