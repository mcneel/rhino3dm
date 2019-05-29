#pragma warning disable 1591
#if RHINO_SDK
using System;
using System.Collections;
using System.Collections.Generic;
using Rhino.Geometry;

namespace Rhino.Render
{
  public class RenderPrimitive : IDisposable
  {
    private IntPtr m_ptr_render_mesh = IntPtr.Zero;
    private RenderPrimitiveEnumerator m_parent;

    internal RenderPrimitive(RenderPrimitiveEnumerator parent, IntPtr ptrRenderMesh)
    {
      m_parent = parent;
      m_parent.IncrementDependencyCount();
      m_ptr_render_mesh = ptrRenderMesh;
    }

    ~RenderPrimitive()
    {
      Dispose(false);
      GC.SuppressFinalize(this);
    }

    public void Dispose()
    {
      Dispose(true);
    }

    protected void Dispose(bool isDisposing)
    {
      if( m_parent != null )
      {
        m_parent.DecrementDependencyCount();
      }
      m_parent = null;
      m_ptr_render_mesh = IntPtr.Zero;
    }

    /// <summary>
    /// The Rhino object associated with this render primitive.
    /// </summary>
    public DocObjects.RhinoObject RhinoObject
    {
      get
      {
        var const_pointer = ConstPointer();
        var mesh_object = UnsafeNativeMethods.Rdk_RenderMesh_Object(const_pointer);
        return DocObjects.RhinoObject.CreateRhinoObjectHelper(mesh_object);
      }
    }

    /// <summary>
    /// Call this before extracting meshes if you support render primitives to
    /// get the <see cref="RenderPrimitiveType"/> of this mesh then call the
    /// associated <see cref="TryGetSphere"/>, <see cref="TryGetPlane"/>, <see cref="TryGetCone"/>, or
    /// <see cref="TryGetBox"/> method.  Calling the <see cref="Mesh"/> property
    /// will mesh the primitive and return a mesh always.
    /// </summary>
    public RenderPrimitiveType PrimitiveType
    {
      get
      {
        var const_pointer = ConstPointer();
        return (RenderPrimitiveType)UnsafeNativeMethods.Rdk_RenderMesh_PrimitiveType(const_pointer);
      }
    }

    /// <summary>
    /// Returns the mesh associated with the object, this will mesh primitives
    /// and always return a mesh.
    /// </summary>
    /// <returns></returns>
    public Mesh Mesh()
    {
      var const_pointer = ConstPointer();
      var mesh = UnsafeNativeMethods.Rdk_RenderMesh_Mesh(const_pointer);
      MeshHolder mh = new MeshHolder(this);
      return (mesh == IntPtr.Zero ? null : new Mesh(mesh, mh));
    }

    /// <summary>
    /// Call this method to get a sphere primitive for this mesh.  If this
    /// meshes <see cref="PrimitiveType"/> is not a <see cref="Rhino.Render.RenderPrimitiveType.Sphere"/>
    /// then the sphere parameter is set to <see cref="Sphere.Unset"/>.
    /// </summary>
    /// <param name="sphere">
    /// Gets set to the primitive sphere for this object on success. 
    /// </param>
    /// <returns>
    /// Returns true if <see cref="PrimitiveType"/> is <see cref="Rhino.Render.RenderPrimitiveType.Sphere"/> and
    /// the sphere parameter was initialized otherwise returns false.
    /// </returns>
    public bool TryGetSphere(out Sphere sphere)
    {
      var radius = 1.0;
      var center = new Point3d();
      var const_pointer = ConstPointer();
      var success = (1 == UnsafeNativeMethods.Rdk_RenderMesh_Sphere(const_pointer, ref center, ref radius));
      sphere = (success ? new Sphere(center, radius) : Sphere.Unset);
      return success;
    }

    /// <summary>
    /// Call this method to get a <see cref="Box"/> primitive for this mesh.  If this
    /// meshes <see cref="PrimitiveType"/> is not a <see cref="Rhino.Render.RenderPrimitiveType.Box"/>
    /// then the box parameter is set to <see cref="Box.Empty"/>.
    /// </summary>
    /// <param name="box">
    /// Gets set to the box primitive for this object on success or <see cref="Box.Empty"/> on error.
    /// </param>
    /// <returns>
    /// Returns true if <see cref="PrimitiveType"/> is <see cref="Rhino.Render.RenderPrimitiveType.Box"/> and
    /// the box parameter was initialized otherwise returns false.
    /// </returns>
    public bool TryGetBox(out Box box)
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
      var const_pointer = ConstPointer();
      var success = (1 == UnsafeNativeMethods.Rdk_RenderMesh_Box(const_pointer, ref origin, ref xaxis, ref yaxis, ref min_x, ref max_x, ref min_y, ref max_y, ref min_z, ref max_z));
      if (success)
      {
        box = new Box(new Plane(origin, xaxis, yaxis),
                      new Interval(min_x, max_x),
                      new Interval(min_y, max_y),
                      new Interval(min_z, max_z));
      }
      else
      {
        box = Box.Empty;
      }
      return success;
    }

    /// <summary>
    /// Call this method to get a <see cref="Plane"/> primitive for this mesh.  If this
    /// meshes <see cref="PrimitiveType"/> is not a <see cref="Rhino.Render.RenderPrimitiveType.Plane"/>
    /// then the plane parameter is set to null.
    /// </summary>
    /// <param name="plane">
    /// Gets set to the plane primitive for this object on success or null on error.
    /// </param>
    /// <returns>
    /// Returns true if <see cref="PrimitiveType"/> is <see cref="Rhino.Render.RenderPrimitiveType.Plane"/> and
    /// the plane parameter was initialized otherwise returns false.
    /// </returns>
    public bool TryGetPlane(out PlaneSurface plane)
    {
      var origin = new Point3d();
      var xaxis = new Vector3d();
      var yaxis = new Vector3d();
      var min_x = 0.0;
      var max_x = 0.0;
      var min_y = 0.0;
      var max_y = 0.0;
      var const_pointer = ConstPointer();
      var success = (1 == UnsafeNativeMethods.Rdk_RenderMesh_Plane(const_pointer, ref origin, ref xaxis, ref yaxis, ref min_x, ref max_x, ref min_y, ref max_y));
      if (success)
        plane = new PlaneSurface(new Plane(origin, xaxis, yaxis),
                                 new Interval(min_x, max_x),
                                 new Interval(min_y, max_y));
      else
        plane = null;
      return success;
    }

    /// <summary>
    /// Call this method to get a <see cref="Cone"/> primitive for this mesh.  If this
    /// meshes <see cref="PrimitiveType"/> is not a <see cref="Rhino.Render.RenderPrimitiveType.Cone"/>
    /// then the cone parameter is set to <see cref="Cone.Unset"/> and the truncation
    /// parameter is set to <see cref="Plane.Unset"/>.
    /// </summary>
    /// <param name="cone">
    /// Gets set to the cone primitive for this object on success or <see cref="Cone.Unset"/> on error.
    /// </param>
    /// <param name="truncation">
    /// Gets set to the truncation plane for this object on success or <see cref="Plane.Unset"/> on error.
    /// </param>
    /// <returns>
    /// Returns true if <see cref="PrimitiveType"/> is <see cref="Rhino.Render.RenderPrimitiveType.Cone"/> and
    /// the cone and truncation parameters were initialized otherwise returns false.
    /// </returns>
    public bool TryGetCone(out Cone cone, out Plane truncation)
    {
      var origin = new Point3d();
      var xaxis = new Vector3d();
      var yaxis = new Vector3d();

      var height = 0.0;
      var radius = 0.0;

      var t_origin = new Point3d();
      var t_xaxis = new Vector3d();
      var t_yaxis = new Vector3d();

      var const_pointer = ConstPointer();

      var success = (1 == UnsafeNativeMethods.Rdk_RenderMesh_Cone(const_pointer, ref origin, ref xaxis, ref yaxis, ref height, ref radius, ref origin, ref xaxis, ref yaxis));
      if (success)
      {
        cone = new Cone(new Plane(origin, xaxis, yaxis), height, radius);
        truncation = new Plane(t_origin, t_xaxis, t_yaxis);
      }
      else
      {
        cone = Cone.Unset;
        truncation = Plane.Unset;
      }
      return success;
    }

    /// <summary>
    /// Instance reference transform or Identity if not an instance reference.
    /// </summary>
    public Transform InstanceTransform
    {
      get
      {
        var transform = new Transform();
        var const_pointer = ConstPointer();
        UnsafeNativeMethods.Rdk_RenderMesh_XformInstance(const_pointer, ref transform);
        return transform;
      }
    }

    /*/// <summary>
    /// This property will be true if this mesh has a <see cref="RenderMaterial"/> associated with
    /// it or false if there is a <see cref="Material"/>.
    /// </summary>
    public bool HasRenderMaterial
    {
      get
      {
        var const_pointer = ConstPointer();
        return (1 == UnsafeNativeMethods.Rdk_RenderMesh_IsRdkMaterial(const_pointer));
      }
    }*/

    /// <summary>
    /// The <see cref="RenderMaterial"/> associated with this mesh or null if there is not one.
    /// </summary>
    public RenderMaterial RenderMaterial
    {
      get
      {
        var const_pointer = ConstPointer();
        var material = UnsafeNativeMethods.Rdk_RenderMesh_RdkMaterial(const_pointer);
        var result = (material == IntPtr.Zero ? RenderMaterial.CreateBasicMaterial(Material) : RenderContent.FromPointer(material) as RenderMaterial);
        return result;
      }
    }

    /// <summary>
    /// The <see cref="Material"/>associated with this mesh or null if the
    /// <see cref="RenderPrimitive"/> property has a value.
    /// </summary>
    internal DocObjects.Material Material
    {
      get
      {
        var const_pointer = ConstPointer();
        var material = UnsafeNativeMethods.Rdk_RenderMesh_OnMaterial(const_pointer);
        var result = (material == IntPtr.Zero ? DocObjects.Material.DefaultMaterial : DocObjects.Material.NewTemporaryMaterial(material, m_parent.Document));
        return result;
      }
    }

    /// <summary>
    /// The bounding box for this primitive.
    /// </summary>
    public BoundingBox BoundingBox
    {
      get
      {
        var min = new Point3d();
        var max = new Point3d();
        var const_pointer = ConstPointer();

        UnsafeNativeMethods.Rdk_RenderMesh_BoundingBox(const_pointer, ref min, ref max);

        return new BoundingBox(min, max);
      }
    }

    #region internals
    internal IntPtr ConstPointer()
    {
      return m_ptr_render_mesh;
    }
    internal IntPtr NonConstPointer()
    {
      return m_ptr_render_mesh;
    }
    #endregion
  }

  class RenderPrimitiveEnumerable : IEnumerable<RenderPrimitive>
  {
    readonly uint m_doc_id;
    readonly Guid m_plugin_id;
    readonly DocObjects.ViewportInfo m_viewport;
    readonly bool m_force_triangles;
    private readonly bool m_quiet;
    /// <summary>
    /// 
    /// </summary>
    /// <param name="documentSerialNumber">
    /// Runtime serial number of the Rhino document.
    /// </param>
    /// <param name="plugInId">
    /// The Id of the plug-in creating the iterator.
    /// </param>
    /// <param name="viewport">
    /// The rendering view camera.
    /// </param>
    /// <param name="forceTriangleMeshes">
    /// If true quad meshes will be triangulated
    /// </param>
    /// <param name="quiet">
    /// Iterate quietly, if true then no user interface will be displayed
    /// </param>
    public RenderPrimitiveEnumerable(uint documentSerialNumber, Guid plugInId, DocObjects.ViewportInfo viewport, bool forceTriangleMeshes, bool quiet)
    {
      m_doc_id = documentSerialNumber;
      m_plugin_id = plugInId;
      m_viewport = viewport;
      m_force_triangles = forceTriangleMeshes;
      m_quiet = quiet;
    }
    public IEnumerator<RenderPrimitive> GetEnumerator()
    {
      return new RenderPrimitiveEnumerator(m_doc_id, m_plugin_id, m_viewport, m_force_triangles, m_quiet);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
      return new RenderPrimitiveEnumerator(m_doc_id, m_plugin_id, m_viewport, m_force_triangles, m_quiet);
    }
  }

  class RenderPrimitiveEnumerator : IEnumerator<RenderPrimitive>
  {
    /// <summary></summary>
    /// <param name="documentSerialNumber">
    /// Runtime serial number of the Rhino document.
    /// </param>
    /// <param name="plugInId">
    /// The Id of the plug-in creating the iterator.
    /// </param>
    /// <param name="viewport">
    /// The rendering view camera.
    /// </param>
    /// <param name="forceTriangleMeshes">
    /// If true quad meshes will be triangulated
    /// </param>
    /// <param name="quiet">
    /// Iterate quietly, if true then no user interface will be displayed
    /// </param>
    public RenderPrimitiveEnumerator(uint documentSerialNumber, Guid plugInId, DocObjects.ViewportInfo viewport, bool forceTriangleMeshes, bool quiet)
    {
      var viewport_pointer = null == viewport ? IntPtr.Zero : viewport.ConstPointer();
      var iterator_pointer = UnsafeNativeMethods.Rdk_RenderMeshIterator_New(documentSerialNumber, plugInId, forceTriangleMeshes, viewport_pointer, quiet);
      if (iterator_pointer != IntPtr.Zero)
        m_iterator_pointer = iterator_pointer;
      m_doc_serial = documentSerialNumber;
    }

    private uint m_doc_serial;

    internal RhinoDoc Document
    {
      get
      {
        return RhinoDoc.FromRuntimeSerialNumber(m_doc_serial);
      }
    }

    private IntPtr m_iterator_pointer = IntPtr.Zero;
    
    internal RenderPrimitiveEnumerator(IntPtr iteratorPointer)
    {
      m_iterator_pointer = iteratorPointer;
    }

    public void EnsureRenderMeshesCreated()
    {
      var const_pointer = ConstPointer();
      UnsafeNativeMethods.Rdk_RenderMeshIterator_EnsureRenderMeshesCreated(const_pointer);
    }

    /// <summary>
    /// Bounding box containing all meshes in the scene.
    /// </summary>
    public BoundingBox SceneBoundingBox
    {
      get
      {
        var min = new Point3d();
        var max = new Point3d();
        var const_pointer = ConstPointer();

        UnsafeNativeMethods.Rdk_RenderMeshIterator_SceneBoundingBox(const_pointer, ref min, ref max);

        return new BoundingBox(min, max);
      }
    }

    public bool SupportsAutomaticInstancing
    {
      get
      {
        var const_pointer = ConstPointer();
        return 1 == UnsafeNativeMethods.Rdk_RenderMeshIterator_SupportsAutomaticInstancing(const_pointer);
      }
    }

    #region IDisposable Members
    
    bool m_disposed;
    int m_dependency_count;

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
      if (m_disposed && m_dependency_count < 1 && m_iterator_pointer != IntPtr.Zero)
      {
        UnsafeNativeMethods.Rdk_RenderMeshIterator_Delete(m_iterator_pointer);
        m_iterator_pointer = IntPtr.Zero;
      }
    }

    ~RenderPrimitiveEnumerator()
    {
      Dispose(false);
      GC.SuppressFinalize(this);
    }

    public void Dispose()
    {
      Dispose(true);
    }

    protected void Dispose(bool isDisposing)
    {
      m_disposed = true;
      DoCleanUpTest();
    }

    #endregion

    #region internals
    internal IntPtr ConstPointer()
    {
      return m_iterator_pointer;
    }
    #endregion

    #region IEnumerator Members

    private RenderPrimitive m_current;

    public bool MoveNext()
    {
      IntPtr ptr_rendermesh = UnsafeNativeMethods.Rdk_RenderMesh_New();
      IntPtr const_ptr_this = ConstPointer();
      bool success = (1 == UnsafeNativeMethods.Rdk_RenderMeshIterator_Next(const_ptr_this, ptr_rendermesh));
      m_current = success ? new RenderPrimitive(this, ptr_rendermesh) : null;
      return success;
    }

    public void Reset()
    {
      m_current = null;
      IntPtr const_ptr_this = ConstPointer();
      UnsafeNativeMethods.Rdk_RenderMeshIterator_Reset(const_ptr_this);
    }

    public object Current
    {
      get { return m_current; }
    }

    RenderPrimitive IEnumerator<RenderPrimitive>.Current
    {
      get { return m_current; }
    }

    #endregion
  }
}

#endif
