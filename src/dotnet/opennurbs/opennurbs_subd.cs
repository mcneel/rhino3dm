using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using Rhino.Runtime;
using Rhino.Runtime.InteropWrappers;

namespace Rhino.Geometry
{
  /// <summary>
  /// Subdivision surface
  /// </summary>
  [Serializable]
  public partial class SubD : GeometryBase
  {
    IntPtr m_ptr_subd_ref;  // ON_SubDRef*
    IntPtr m_ptr_subd_display; // CRhinoSubDDisplay*

    internal IntPtr SubDRefPointer()
    {
      return m_ptr_subd_ref;
    }

    internal IntPtr SubDDisplay()
    {
      if (m_ptr_subd_display != IntPtr.Zero)
        return m_ptr_subd_display;

#if RHINO_SDK
      m_ptr_subd_display = UnsafeNativeMethods.CRhinoSubDDisplay_New(m_ptr_subd_ref);
#endif
      return m_ptr_subd_display;
    }

    void DestroySubDDisplay()
    {
#if RHINO_SDK
      if (m_ptr_subd_display != IntPtr.Zero)
        UnsafeNativeMethods.CRhinoSubDDisplay_Delete(m_ptr_subd_display);
#endif
      m_ptr_subd_display = IntPtr.Zero;
    }

    /// <summary>
    /// Destroy cache handle
    /// </summary>
    protected override void NonConstOperation()
    {
      DestroySubDDisplay();
      base.NonConstOperation();
    }



    /// <summary>
    /// Create a new instance of SubD geometry
    /// </summary>
    /// <since>7.0</since>
    public SubD()
    {
      m_ptr_subd_ref = UnsafeNativeMethods.ON_SubDRef_New();
      IntPtr ptrSubD = UnsafeNativeMethods.ON_SubDRef_NewSubD(m_ptr_subd_ref);
      RuntimeSerialNumber = UnsafeNativeMethods.ON_SubD_RuntimeSerialNumber(ptrSubD);
      ConstructNonConstObject(ptrSubD);
      ApplyMemoryPressure();
    }

    internal SubD(IntPtr nativePointer, object parent)
      : base(nativePointer, parent, -1)
    {
      if (null == parent && IntPtr.Zero == nativePointer) return;
      if (null == parent)
      {
        m_ptr_subd_ref = UnsafeNativeMethods.ON_SubDRef_CreateAndAttach(nativePointer);
        ApplyMemoryPressure();
      }

#if RHINO_SDK
      var rhsubd = parent as DocObjects.SubDObject;
      if (rhsubd != null)
      {
        // get a SubD ref from the parent object so we are safe to continue to use
        // the SubD even if the parent object is destroyed
        IntPtr ptrSubDObject = rhsubd.ConstPointer();
        m_ptr_subd_ref = UnsafeNativeMethods.CRhinoSubDObject_SubDRefCopy(ptrSubDObject);
      }
#endif

      IntPtr const_ptr_subd = UnsafeNativeMethods.ON_SubDRef_ConstPointerSubD(m_ptr_subd_ref);
      RuntimeSerialNumber = UnsafeNativeMethods.ON_SubD_RuntimeSerialNumber(const_ptr_subd);
    }

    /// <summary>
    /// Protected constructor used in serialization.
    /// </summary>
    protected SubD(SerializationInfo info, StreamingContext context)
      : base(info, context)
    {
    }

    ///// <summary>
    ///// Copies this SubD, including its evaluation cache.
    ///// </summary>
    ///// <returns>A SubD.</returns>
    ///// <since>8.8</since>
    //[ConstOperation]
    //public override GeometryBase Duplicate()
    //{
    //  IntPtr const_ptr = ConstPointer();
    //  GeometryBase rc = base.Duplicate();
    //  SubD subd = rc as SubD;
    //  if (null != subd)
    //  {
    //    subd.CopyEvaluationCache(this);
    //  }
    //  return rc;
    //}

    internal override IntPtr _InternalGetConstPointer()
    {
      IntPtr const_ptr = UnsafeNativeMethods.ON_SubDRef_ConstPointerSubD(m_ptr_subd_ref);
      if (const_ptr != IntPtr.Zero)
        return const_ptr;
      return base._InternalGetConstPointer();
    }

    /// <summary>
    /// Called when this object switches from being considered "owned by the document"
    /// to being an independent instance.
    /// </summary>
    protected override void OnSwitchToNonConst()
    {
      base.OnSwitchToNonConst();

      // make sure m_ptr_subd_ref points at the correct m_ptr
      IntPtr ptr = NonConstPointer();
      if (ptr != IntPtr.Zero)
      {
        IntPtr ptr_subd_from_ref = UnsafeNativeMethods.ON_SubDRef_ConstPointerSubD(m_ptr_subd_ref);
        if( ptr != ptr_subd_from_ref )
        {
          UnsafeNativeMethods.ON_SubDRef_Delete(m_ptr_subd_ref);
          m_ptr_subd_ref = UnsafeNativeMethods.ON_SubDRef_CreateAndAttach(ptr);
        }
      }

      // update runtime serial number
      IntPtr const_ptr_subd = UnsafeNativeMethods.ON_SubDRef_ConstPointerSubD(m_ptr_subd_ref);
      RuntimeSerialNumber = UnsafeNativeMethods.ON_SubD_RuntimeSerialNumber(const_ptr_subd);
    }

    internal override GeometryBase DuplicateShallowHelper()
    {
      return new SubD(IntPtr.Zero, null);
    }

#if RHINO_SDK
    /// <summary>
    /// Gets Nurbs form of all edges in this SubD, with clamped knots.
    /// NB: Does not update the SubD evaluation cache before getting the edges.
    /// </summary>
    /// <returns>An array of edge curves.</returns>
    /// <seealso cref="SubD.UpdateSurfaceMeshCache(bool)"/>
    /// <since>8.7</since>
    [ConstOperation]
    public Curve[] DuplicateEdgeCurves()
    {
      return DuplicateEdgeCurves(false, false, false, false, false, true);
    }

    /// <summary>
    /// Gets Nurbs form of edges in this SubD.
    /// NB: Does not update the SubD evaluation cache before getting the edges.
    /// </summary>
    /// <param name="boundaryOnly">
    /// If true, then only the boundary edges are duplicated.
    /// If false, then all edges are duplicated.
    /// If both boundaryOnly and interiorOnly are true, an empty array is returned.
    /// </param>
    /// <param name="interiorOnly">
    /// If true, then only the interior edges are duplicated.
    /// If false, then all edges are duplicated.
    /// Note: interior edges with faces of different orientations are also returned.
    /// If both boundaryOnly and interiorOnly are true, an empty array is returned.
    /// </param>
    /// <param name="smoothOnly">
    /// If true, then only the smooth (and not sharp) edges are duplicated.
    /// If false, then all edges are duplicated.
    /// If both smoothOnly and sharpOnly and creaseOnly are true, an empty array is returned.
    /// </param>
    /// <param name="sharpOnly">
    /// If true, then only the sharp edges are duplicated.
    /// If false, then all edges are duplicated.
    /// If both smoothOnly and sharpOnly and creaseOnly are true, an empty array is returned.
    /// </param>
    /// <param name="creaseOnly">
    /// If true, then only the creased edges are duplicated.
    /// If false, then all edges are duplicated.
    /// If both smoothOnly and sharpOnly and creaseOnly are true, an empty array is returned.
    /// </param>
    /// <param name="clampEnds">
    /// If true, the end knots are clamped.
    /// Otherwise the end knots are(-2,-1,0,...., k1, k1+1, k1+2).
    /// </param>
    /// <returns>Array of edge curves on success.</returns>
    /// <seealso cref="SubD.UpdateSurfaceMeshCache(bool)"/>
    /// <since>8.7</since>
    public Curve[] DuplicateEdgeCurves(bool boundaryOnly, bool interiorOnly, bool smoothOnly, bool sharpOnly, bool creaseOnly, bool clampEnds)
    {
      // TODO: Should this be non-const, it might change the surface mesh cache and delete the display handle?
      IntPtr const_ptr_this = ConstPointer();
      //if (UpdateSurfaceMeshCache(true) > 0) DestroySubDDisplay();

      using (var output = new SimpleArrayCurvePointer())
      {
        IntPtr ptr_output = output.NonConstPointer();

        UnsafeNativeMethods.ON_SubD_DuplicateEdgeCurves(const_ptr_this, ptr_output, boundaryOnly, interiorOnly, smoothOnly, sharpOnly, creaseOnly, clampEnds);
        GC.KeepAlive(this);
        return output.ToNonConstArray();
      }
    }

    /// <summary>
    /// Transforms an enumerable of SubD components.
    /// </summary>
    /// <param name="components">The SubD components to transform.</param>
    /// <param name="xform">The transformation to apply.</param>
    /// <param name="componentLocation">
    /// Select between applying the transform to the control net (faster) or the surface points (slower).
    /// </param>
    /// <returns>The number of vertex locations that changed.</returns>
    /// <remarks>
    /// This method does not clear the evaluation cache.
    /// </remarks>
    /// <since>8.12</since>
    [CLSCompliant(false)]
    public uint TransformComponents(IEnumerable<ComponentIndex> components, Transform xform, SubDComponentLocation componentLocation)
    {
      IntPtr ptr_this = NonConstPointer();
      ComponentIndex[] arr_components = components.ToArray();
      uint rc = UnsafeNativeMethods.ON_SubD_TransformComponents(ptr_this, ref xform, arr_components.Length, arr_components, componentLocation);
      GC.KeepAlive(this);
      return rc;
    }

#endif

    /// <summary>
    /// Deletes the underlying native pointer during a Dispose call or GC collection
    /// </summary>
    /// <param name="disposing"></param>
    protected override void Dispose(bool disposing)
    {
      DestroySubDDisplay();
      ReleaseNonConstPointer();

      IntPtr subd_ref_ptr = m_ptr_subd_ref;
      m_ptr_subd_ref = IntPtr.Zero;

      if (IntPtr.Zero != subd_ref_ptr)
      {
        bool in_finalizer = !disposing;
        if (in_finalizer)
        {
          // 11 Feb 2013 (S. Baer) RH-16157
          // When running in the finalizer, the destructor is being called on the GC
          // thread which results in nearly impossible to track down exceptions.
          // Mask the exception in this case and post information to our logging system
          // about the exception so we can better analyze and try to figure out what
          // is going on
          try
          {
            UnsafeNativeMethods.ON_SubDRef_Delete(subd_ref_ptr);
          }
          catch (Exception ex)
          {
            HostUtils.ExceptionReport(ex);
          }
        }
        else
        {
          // See above. In this case we are running on the main thread of execution
          // and throwing an exception is a good thing so we can analyze and quickly
          // fix whatever is going wrong
          UnsafeNativeMethods.ON_SubDRef_Delete(subd_ref_ptr);
        }
      }

      base.Dispose(disposing);
    }

    internal ulong RuntimeSerialNumber { get; private set; }

    Collections.SubDFaceList m_faces;
    /// <summary>
    /// All faces in this SubD
    /// </summary>
    /// <since>7.0</since>
    public Collections.SubDFaceList Faces
    {
      get
      {
        if (m_faces == null)
          m_faces = new Collections.SubDFaceList(this);
        return m_faces;
      }
    }

    Collections.SubDVertexList m_vertices;
    /// <summary>
    /// All vertices in this SubD
    /// </summary>
    /// <since>7.0</since>
    public Collections.SubDVertexList Vertices
    {
      get
      {
        if (m_vertices == null)
          m_vertices = new Collections.SubDVertexList(this);
        return m_vertices;
      }
    }

    Collections.SubDEdgeList m_edges;
    /// <summary>
    /// All edges in this SubD
    /// </summary>
    /// <since>7.0</since>
    public Collections.SubDEdgeList Edges
    {
      get
      {
        if (m_edges == null)
        {
          m_edges = new Collections.SubDEdgeList(this);
          // 2024-03-14, Pierre, RH-80602:
          // After careful consideration, I do not think this is the proper place
          // to update the surface mesh cache. GH now does it when needed, and 
          // users of the RhinoCommon SDK should do the same.
          /*
#if RHINO_SDK
          if (IsNonConst)
          {
            IntPtr ptr_this = NonConstPointer();
            // 29 April 2020 S. Baer (RH-53342)
            // The following call ensures that edge curves will exist. We may want
            // move this call to another location.
            UnsafeNativeMethods.ON_SubD_UpdateSurfaceMeshCache(ptr_this);
          }
          else
          {
            // 2024-03-12, Pierre, RH-80602
            // GH internaliazed data uses a const ref to the SubD, make sure these
            // also have a surface mesh cached.
            IntPtr const_ptr_this = ConstPointer();
            UnsafeNativeMethods.ON_SubD_ConstUpdateSurfaceMeshCache(const_ptr_this);
          }
#endif
          */
        }
        return m_edges;
      }
    }

    /// <summary>
    /// Test SubD to see if the active level is a solid.  
    /// A "solid" is a closed oriented manifold, or a closed oriented manifold.
    /// </summary>
    /// <since>7.0</since>
    public bool IsSolid
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        bool rc = UnsafeNativeMethods.ON_SubD_IsSolid(const_ptr_this);
        GC.KeepAlive(this);
        return rc;
      }
    }

    /// <summary>
    /// Gets the texture cordinate type.
    /// </summary>
    /// <since>8.28</since>
    public SubDTextureCoordinateType TextureCoordinateType
    {
      get
      {
        var const_ptr_vertex = ConstPointer();
        SubDTextureCoordinateType rc = UnsafeNativeMethods.ON_SubD_GetTextureCoordinateType(const_ptr_vertex);
        GC.KeepAlive(this);
        return rc;
      }
    }

    /// <summary>
    /// Get a new, empty SubD object.
    /// </summary>
    /// <since>7.18</since>
    public static SubD Empty
    {
      get
      {
        IntPtr ptr_subd = UnsafeNativeMethods.ON_SubD_Empty();
        return new SubD(ptr_subd, null);
      }
    }


    /// <summary>
    /// Reverses the orientation of all SubD normals.
    /// </summary>
    /// <returns>True if successful.</returns>
    /// <since>8.7</since>
    public bool Flip()
    {
      IntPtr ptr_this = NonConstPointer();
      bool rc = UnsafeNativeMethods.ON_SubD_Flip(ptr_this);
      GC.KeepAlive(this);
      return rc;
    }

#if RHINO_SDK

    /// <summary>
    /// Joins an enumeration of SubDs to form as few as possible resulting SubDs.
    /// There may be more than one SubD in the result array.
    /// </summary>
    /// <param name="subdsToJoin">An enumeration of SubDs to join.</param>
    /// <param name="tolerance">The join tolerance.</param>
    /// <param name="joinedEdgesAreCreases">
    /// If true, merged boundary edges will be creases.
    /// If false, merged boundary edges will be smooth.
    /// </param>
    /// <returns></returns>
    /// <remarks>
    /// NOTE: All of the input SubDs are copied and added to the result array in one form or another.
    /// NOTE: Symmetry information is removed from newly joined SubDs.See also comments in
    /// SubD.JoinSubDs with the bPreserveSymmetry parameter.
    /// </remarks>
    /// <since>7.14</since>
    public static SubD[] JoinSubDs(IEnumerable<SubD> subdsToJoin, double tolerance, bool joinedEdgesAreCreases)
    {
      if (null == subdsToJoin)
        return null;

      using (var input = new SimpleArraySubDPointer())
      using (var output = new SimpleArraySubDPointer())
      {
        foreach (SubD subd in subdsToJoin)
          input.Add(subd, true);

        IntPtr ptr_input = input.NonConstPointer();
        IntPtr ptr_output = output.NonConstPointer();

        SubD[] rc = null;
        if (UnsafeNativeMethods.RHC_RhinoJoinSubDs(ptr_input, tolerance, joinedEdgesAreCreases, ptr_output) > 0)
        {
          rc = output.ToNonConstArray();
        }
        GC.KeepAlive(subdsToJoin);
        return rc;
      }
    }

    /// <summary>
    /// Joins an enumeration of SubDs to form as few as possible resulting SubDs.
    /// There may be more than one SubD in the result array.
    /// </summary>
    /// <param name="subdsToJoin">An enumeration of SubDs to join.</param>
    /// <param name="tolerance">The join tolerance.</param>
    /// <param name="joinedEdgesAreCreases">
    /// If true, merged boundary edges will be creases.
    /// If false, merged boundary edges will be smooth.
    /// </param>
    /// <param name="preserveSymmetry">
    /// If true, and if all inputs share the same symmetry, the output will also be symmetrical wrt. that symmetry.
    /// If false, or true but no common symmetry exists, symmetry information is removed from all newly joined SubDs.
    /// </param>
    /// <returns></returns>
    /// <remarks>
    /// NOTE: All of the input SubDs are copied and added to the result array in one form or another.
    /// </remarks>
    /// <since>7.16</since>
    public static SubD[] JoinSubDs(IEnumerable<SubD> subdsToJoin, double tolerance, bool joinedEdgesAreCreases, bool preserveSymmetry)
    {
      if (null == subdsToJoin)
        return null;

      using (var input = new SimpleArraySubDPointer())
      using (var output = new SimpleArraySubDPointer())
      {
        foreach (SubD subd in subdsToJoin)
          input.Add(subd, true);

        IntPtr ptr_input = input.NonConstPointer();
        IntPtr ptr_output = output.NonConstPointer();

        SubD[] rc = null;
        if (UnsafeNativeMethods.RHC_RhinoJoinSubDs2(ptr_input, tolerance, joinedEdgesAreCreases, preserveSymmetry, ptr_output) > 0)
        {
          rc = output.ToNonConstArray();
        }
        GC.KeepAlive(subdsToJoin);
        return rc;
      }
    }

    /// <summary>
    /// Create a Brep based on this SubD geometry.
    /// </summary>
    /// <param name="options">
    /// The SubD to Brep conversion options. Use SubDToBrepOptions.Default 
    /// for sensible defaults. Currently, these return unpacked faces 
    /// and locally-G1 vertices in the output Brep.
    /// </param>
    /// <returns>A new Brep if successful, or null on failure.</returns>
    /// <since>7.0</since>
    public Brep ToBrep(SubDToBrepOptions options)
    {
      IntPtr ptr_const_this = ConstPointer();
      IntPtr const_ptr_options = options != null ? options.ConstPointer() : IntPtr.Zero;
      IntPtr ptr_brep = UnsafeNativeMethods.ON_SubD_GetSurfaceBrep(ptr_const_this, const_ptr_options);
      GC.KeepAlive(options);
      GC.KeepAlive(this);
      return CreateGeometryHelper(ptr_brep, null) as Brep;
    }

    /// <summary>
    /// Create a Brep based on this SubD geometry, based on SubDToBrepOptions.Default options.
    /// </summary>
    /// <returns>A new Brep if successful, or null on failure.</returns>
    /// <since>7.6</since>
    public Brep ToBrep()
    {
      SubDToBrepOptions options = SubDToBrepOptions.Default;
      return ToBrep(options);
    }

    /// <summary>
    /// Create a new SubD from a mesh.
    /// </summary>
    /// <param name="mesh">The input mesh.</param>
    /// <returns>A new SubD if successful, or null on failure.</returns>
    /// <since>7.0</since>
    public static SubD CreateFromMesh(Mesh mesh)
    {
      return CreateFromMesh(mesh, null);
    }

    /// <summary>
    /// Create a new SubD from a mesh.
    /// </summary>
    /// <param name="mesh">The input mesh.</param>
    /// <param name="options">The SubD creation options.</param>
    /// <returns>A new SubD if successful, or null on failure.</returns>
    /// <since>7.0</since>
    public static SubD CreateFromMesh(Mesh mesh, SubDCreationOptions options)
    {
      IntPtr const_ptr_mesh = mesh.ConstPointer();
      IntPtr const_ptr_options = options != null ? options.ConstPointer() : IntPtr.Zero;
      IntPtr ptr_subd = UnsafeNativeMethods.ON_SubD_CreateFromMesh(const_ptr_mesh, const_ptr_options);
      if (IntPtr.Zero != ptr_subd)
        return new SubD(ptr_subd, null);
      GC.KeepAlive(mesh);
      GC.KeepAlive(options);
      return null;
    }

    /// <summary>
    /// Create a SubD that approximates the surface. If the surface is a SubD
    /// friendly NURBS surface and withCorners is true, then the SubD and input
    /// surface will have the same geometry.
    /// </summary>
    /// <param name="surface"></param>
    /// <param name="method">Selects the method used to calculate the SubD.</param>
    /// <param name="corners">
    /// If the surface is open, then the corner vertices with be tagged as
    /// VertexTagCorner. This makes the resulting SubD have sharp corners to
    /// match the appearance of the input surface.
    /// </param>
    /// <returns></returns>
    /// <since>7.9</since>
    public static SubD CreateFromSurface(Surface surface, SubDFromSurfaceMethods method, bool corners)
    {
      IntPtr const_ptr_surface = surface.ConstPointer();
      IntPtr ptr_subd = UnsafeNativeMethods.ON_SubD_CreateFromSurface(const_ptr_surface, method, corners);
      if (IntPtr.Zero != ptr_subd)
        return new SubD(ptr_subd, null);
      GC.KeepAlive(surface);
      return null;
    }

#if RHINO_SDK

    /// <summary>
    ///  Creates a SubD sphere made from quad faces.
    /// </summary>
    /// <param name="sphere">Location, size and orientation of the sphere.</param>
    /// <param name="vertexLocation">
    /// If vertexLocation = SubDComponentLocation::ControlNet, 
    /// then the control net points will be on the surface of the sphere.
    /// Otherwise the limit surface points will be on the sphere.
    /// </param>
    /// <param name="quadSubdivisionLevel">
    /// The resulting sphere will have 6*4^subdivision level quads.
    /// (0 for 6 quads, 1 for 24 quads, 2 for 96 quads, ...).
    /// </param>
    /// <returns>
    /// If the input parameters are valid, a SubD quad sphere is returned. Otherwise null is returned.
    /// </returns>
    /// <since>8.4</since>
    [CLSCompliant(false)]
    public static SubD CreateQuadSphere(Sphere sphere, SubDComponentLocation vertexLocation, uint quadSubdivisionLevel)
    {
      IntPtr ptr_subd = UnsafeNativeMethods.ON_SubD_CreateSubDQuadSphere(ref sphere, vertexLocation, quadSubdivisionLevel);
      if (IntPtr.Zero != ptr_subd)
        return new SubD(ptr_subd, null);
      return null;
    }

    /// <summary>
    /// Creates a SubD sphere made from polar triangle fans and bands of quads.
    /// The result resembles a globe with triangle fans at the poles and the
    /// edges forming latitude parallels and longitude meridians.
    /// </summary>
    /// <param name="sphere">Location, size and orientation of the sphere.</param>
    /// <param name="vertexLocation">
    /// If vertexLocation = SubDComponentLocation::ControlNet, 
    /// then the control net points will be on the surface of the sphere.
    /// Otherwise the limit surface points will be on the sphere.
    /// </param>
    /// <param name="axialFaceCount">
    /// Number of faces along the sphere's meridians. (axialFaceCount &gt;= 2)
    /// For example, if you wanted each face to span 30 degrees of latitude, 
    /// you would pass 6 (=180 degrees/30 degrees) for axialFaceCount.
    /// </param>
    /// <param name="equatorialFaceCount">
    /// Number of faces around the sphere's parallels. (equatorialFaceCount &gt;= 3)
    /// For example, if you wanted each face to span 30 degrees of longitude, 
    /// you would pass 12 (=360 degrees/30 degrees) for equatorialFaceCount.
    /// </param>
    /// <returns>
    /// If the input parameters are valid, a SubD globe sphere is returned. Otherwise null is returned.
    /// </returns>
    /// <since>8.4</since>
    [CLSCompliant(false)]
    public static SubD CreateGlobeSphere(Sphere sphere, SubDComponentLocation vertexLocation, uint axialFaceCount, uint equatorialFaceCount)
    {
      IntPtr ptr_subd = UnsafeNativeMethods.ON_SubD_CreateSubDGlobeSphere(ref sphere, vertexLocation, axialFaceCount, equatorialFaceCount);
      if (IntPtr.Zero != ptr_subd)
        return new SubD(ptr_subd, null);
      return null;
    }

    /// <summary>
    /// Creates a SubD sphere made from triangular faces.
    /// This is a goofy topology for a Catmull-Clark subdivision surface
    /// (all triangles and all vertices have 5 or 6 edges).
    /// You may want to consider using the much behaved result from
    /// CreateSubDQuadSphere() or even the result from CreateSubDGlobeSphere().
    /// </summary>
    /// <param name="sphere">Location, size and orientation of the sphere.</param>
    /// <param name="vertexLocation">
    /// If vertexLocation = SubDComponentLocation::ControlNet, 
    /// then the control net points will be on the surface of the sphere.
    /// Otherwise the limit surface points will be on the sphere.
    /// </param>
    /// <param name="triSubdivisionLevel">
    /// The resulting sphere will have 20*4^subdivision level triangles.
    /// (0 for 20 triangles, 1 for 80 triangles, 2 for 320 triangles, ...).
    /// </param>
    /// <returns>
    /// If the input parameters are valid, a SubD tri sphere is returned. Otherwise null is returned.
    /// </returns>
    /// <since>8.4</since>
    [CLSCompliant(false)]
    public static SubD CreateTriSphere(Sphere sphere, SubDComponentLocation vertexLocation, uint triSubdivisionLevel)
    {
      IntPtr ptr_subd = UnsafeNativeMethods.ON_SubD_CreateSubDTriSphere(ref sphere, vertexLocation, triSubdivisionLevel);
      if (IntPtr.Zero != ptr_subd)
        return new SubD(ptr_subd, null);
      return null;
    }

    /// <summary>
    /// Creates a SubD sphere based on an icosohedron (20 triangular faces and 5 valent vertices).
    /// This is a goofy topology for a Catmull-Clark subdivision surface
    /// (all triangles, all vertices have 5 edges).
    /// You may want to consider using the much behaved result from
    /// CreateSubDQuadSphere(sphere, vertexLocation, 1) 
    /// or even the result from CreateSubDGlobeSphere().
    /// </summary>
    /// <param name="sphere">Location, size and orientation of the sphere.</param>
    /// <param name="vertexLocation">
    /// If vertexLocation = SubDComponentLocation::ControlNet, 
    /// then the control net points will be on the surface of the sphere.
    /// Otherwise the limit surface points will be on the sphere.
    /// </param>
    /// <returns>
    /// If the input parameters are valid, a SubD icosahedron is returned. Otherwise null is returned.
    /// </returns>
    /// <since>8.4</since>
    [CLSCompliant(false)]
    public static SubD CreateIcosahedron(Sphere sphere, SubDComponentLocation vertexLocation)
    {
      IntPtr ptr_subd = UnsafeNativeMethods.ON_SubD_CreateSubDIcosahedron(ref sphere, vertexLocation);
      if (IntPtr.Zero != ptr_subd)
        return new SubD(ptr_subd, null);
      return null;
    }

#endif

    /// <summary>
    /// Deletes components from this SubD.
    /// <para>
    /// Deleting a vertex deletes every edge and face attached to it. Deleting an edge
    /// deletes every face attached to it. Deleting a face deletes only that face.
    /// </para>
    /// </summary>
    /// <param name="components">The components to delete.</param>
    /// <param name="markDeletedFaceEdges">
    /// If true, edges that survive the deletion of a face they bounded get their runtime
    /// mark set, so the caller can find the boundary of the hole that was opened.
    /// </param>
    /// <returns>True if the deletion succeeded.</returns>
    /// <since>8.36</since>
    public bool DeleteComponents(IEnumerable<SubDComponent> components, bool markDeletedFaceEdges)
    {
      if (null == components)
        throw new ArgumentNullException(nameof(components));

      IntPtr ptr_this = NonConstPointer();
      bool rc;
      using (var ciArray = new INTERNAL_ComponentIndexArray())
      {
        foreach (var component in components)
          ciArray.Add(ComponentIndexOf(component));
        if (0 == ciArray.Count)
          return true; // nothing to delete
        rc = UnsafeNativeMethods.ON_SubD_DeleteComponents(ptr_this, ciArray.NonConstPointer(), markDeletedFaceEdges);
      }
      GC.KeepAlive(components);
      GC.KeepAlive(this);
      return rc;
    }

    /// <summary>
    /// Deletes components from this SubD, leaving the edges that bounded any deleted face
    /// unmarked. See
    /// <see cref="DeleteComponents(IEnumerable{SubDComponent}, bool)"/>.
    /// </summary>
    /// <param name="components">The components to delete.</param>
    /// <returns>True if the deletion succeeded.</returns>
    /// <since>8.36</since>
    public bool DeleteComponents(IEnumerable<SubDComponent> components)
    {
      return DeleteComponents(components, false);
    }

    /// <summary>
    /// Deletes components from this SubD, identified by component index.
    /// </summary>
    /// <param name="componentIndices">The component indices to delete.</param>
    /// <param name="markDeletedFaceEdges">
    /// If true, edges that survive the deletion of a face they bounded get their runtime
    /// mark set.
    /// </param>
    /// <returns>True if the deletion succeeded.</returns>
    /// <since>8.36</since>
    public bool DeleteComponents(IEnumerable<ComponentIndex> componentIndices, bool markDeletedFaceEdges)
    {
      if (null == componentIndices)
        throw new ArgumentNullException(nameof(componentIndices));

      IntPtr ptr_this = NonConstPointer();
      bool rc;
      using (var ciArray = new INTERNAL_ComponentIndexArray())
      {
        foreach (var ci in componentIndices)
          ciArray.Add(ci);
        if (0 == ciArray.Count)
          return true; // nothing to delete
        rc = UnsafeNativeMethods.ON_SubD_DeleteComponents(ptr_this, ciArray.NonConstPointer(), markDeletedFaceEdges);
      }
      GC.KeepAlive(this);
      return rc;
    }

    /// <summary>
    /// Dissolves the given edges and vertices, merging the faces around them, and deletes
    /// the given faces.
    /// <para>
    /// This is the difference between removing an edge between two quads to get one bigger
    /// face, and removing the two quads to leave a hole. Use
    /// <see cref="DeleteComponents(IEnumerable{SubDComponent}, bool)"/> when a hole is what
    /// you want.
    /// </para>
    /// </summary>
    /// <param name="components">The vertices, edges and faces to dissolve or delete.</param>
    /// <returns>The number of merged faces created by dissolving edges and vertices.</returns>
    /// <since>8.36</since>
    [CLSCompliant(false)]
    public uint DissolveOrDeleteComponents(IEnumerable<SubDComponent> components)
    {
      if (null == components)
        throw new ArgumentNullException(nameof(components));

      IntPtr ptr_this = NonConstPointer();
      uint rc;
      using (var ciArray = new INTERNAL_ComponentIndexArray())
      {
        foreach (var component in components)
          ciArray.Add(ComponentIndexOf(component));
        if (0 == ciArray.Count)
          return 0;
        rc = UnsafeNativeMethods.ON_SubD_DissolveOrDelete(ptr_this, ciArray.NonConstPointer());
      }
      GC.KeepAlive(components);
      GC.KeepAlive(this);
      return rc;
    }

    /// <summary>
    /// Dissolves or deletes components, identified by component index.
    /// See <see cref="DissolveOrDeleteComponents(IEnumerable{SubDComponent})"/>.
    /// </summary>
    /// <param name="componentIndices">The component indices to dissolve or delete.</param>
    /// <returns>The number of merged faces created by dissolving edges and vertices.</returns>
    /// <since>8.36</since>
    [CLSCompliant(false)]
    public uint DissolveOrDeleteComponents(IEnumerable<ComponentIndex> componentIndices)
    {
      if (null == componentIndices)
        throw new ArgumentNullException(nameof(componentIndices));

      IntPtr ptr_this = NonConstPointer();
      uint rc;
      using (var ciArray = new INTERNAL_ComponentIndexArray())
      {
        foreach (var ci in componentIndices)
          ciArray.Add(ci);
        if (0 == ciArray.Count)
          return 0;
        rc = UnsafeNativeMethods.ON_SubD_DissolveOrDelete(ptr_this, ciArray.NonConstPointer());
      }
      GC.KeepAlive(this);
      return rc;
    }

    // Every SubDComponent knows its own component index; this keeps the callers above from
    // having to switch on the concrete type.
    static ComponentIndex ComponentIndexOf(SubDComponent component)
    {
      if (null == component)
        throw new ArgumentNullException(nameof(component));
      var vertex = component as SubDVertex;
      if (null != vertex)
        return vertex.ComponentIndex();
      var edge = component as SubDEdge;
      if (null != edge)
        return edge.ComponentIndex();
      var face = component as SubDFace;
      if (null != face)
        return face.ComponentIndex();
      throw new NotSupportedException("Unknown SubDComponent type.");
    }

    /// <summary>
    /// Creates a SubD box.
    /// </summary>
    /// <param name="corners">
    /// The eight box corners. The first four are the bottom face, in order around it, and
    /// the last four are the top face, in the same order.
    /// </param>
    /// <param name="edgeSharpness">
    /// The sharpness to give the edges where box sides meet.
    /// <see cref="SubDEdgeSharpness.SmoothValue"/> leaves them smooth and
    /// <see cref="SubDEdgeSharpness.CreaseValue"/> makes them creases.
    /// </param>
    /// <param name="faceCountX">Number of faces along the first bottom edge.</param>
    /// <param name="faceCountY">Number of faces along the second bottom edge.</param>
    /// <param name="faceCountZ">Number of faces from the bottom face to the top face.</param>
    /// <returns>A new SubD box, or null if the input is not valid.</returns>
    /// <since>8.36</since>
    [CLSCompliant(false)]
    public static SubD CreateSubDBox(IEnumerable<Point3d> corners, double edgeSharpness, uint faceCountX, uint faceCountY, uint faceCountZ)
    {
      if (null == corners)
        throw new ArgumentNullException(nameof(corners));

      Point3d[] pts = corners as Point3d[] ?? corners.ToArray();
      if (8 != pts.Length)
        throw new ArgumentException("corners must have exactly 8 points.", nameof(corners));

      IntPtr ptr_subd = UnsafeNativeMethods.ON_SubD_CreateSubDBox(pts, edgeSharpness, faceCountX, faceCountY, faceCountZ);
      if (IntPtr.Zero == ptr_subd)
        return null;
      return new SubD(ptr_subd, null);
    }

    /// <summary>
    /// Creates a SubD box from a <see cref="Box"/>.
    /// </summary>
    /// <param name="box">The box to build from. It must be valid.</param>
    /// <param name="edgeSharpness">
    /// The sharpness to give the edges where box sides meet. See
    /// <see cref="CreateSubDBox(IEnumerable{Point3d}, double, uint, uint, uint)"/>.
    /// </param>
    /// <param name="faceCountX">Number of faces in the box X direction.</param>
    /// <param name="faceCountY">Number of faces in the box Y direction.</param>
    /// <param name="faceCountZ">Number of faces in the box Z direction.</param>
    /// <returns>A new SubD box, or null if the input is not valid.</returns>
    /// <since>8.36</since>
    [CLSCompliant(false)]
    public static SubD CreateSubDBox(Box box, double edgeSharpness, uint faceCountX, uint faceCountY, uint faceCountZ)
    {
      if (!box.IsValid)
        throw new ArgumentException("box is not valid.", nameof(box));

      // ON_SubD::CreateSubDBox wants the bottom face first, then the top face in the same
      // order. Box.GetCorners() already returns them that way.
      return CreateSubDBox(box.GetCorners(), edgeSharpness, faceCountX, faceCountY, faceCountZ);
    }

    /// <summary>
    /// The number of errors the SubD code has trapped since the process started.
    /// <para>
    /// This is a diagnostic counter, not a per-SubD property. Sample it before and after an
    /// operation to find out whether that operation hit an internal error, which usually
    /// means the SubD is not valid.
    /// </para>
    /// </summary>
    /// <since>8.36</since>
    [CLSCompliant(false)]
    public static uint ErrorCount
    {
      get { return UnsafeNativeMethods.ON_SubD_ErrorCount(); }
    }

    /// <summary>
    /// Gets the number of sharp edges in this SubD.
    /// See <see cref="SubDEdge.IsSharp"/> for what makes an edge sharp.
    /// </summary>
    /// <returns>The number of sharp edges.</returns>
    /// <since>8.36</since>
    [CLSCompliant(false)]
    [ConstOperation]
    public uint SharpEdgeCount()
    {
      SubDEdgeSharpness range = default(SubDEdgeSharpness);
      return SharpEdgeCount(out range);
    }

    /// <summary>
    /// Gets the number of sharp edges in this SubD, and the range of sharpness values used.
    /// </summary>
    /// <param name="sharpnessRange">
    /// The smallest and largest sharpness found on the sharp edges. If there are no sharp
    /// edges, this is <see cref="SubDEdgeSharpness.Smooth"/>.
    /// </param>
    /// <returns>The number of sharp edges.</returns>
    /// <since>8.36</since>
    [CLSCompliant(false)]
    [ConstOperation]
    public uint SharpEdgeCount(out SubDEdgeSharpness sharpnessRange)
    {
      sharpnessRange = default(SubDEdgeSharpness);
      IntPtr const_ptr_this = ConstPointer();
      uint rc = UnsafeNativeMethods.ON_SubD_SharpEdgeCount(const_ptr_this, ref sharpnessRange);
      GC.KeepAlive(this);
      return rc;
    }

    /// <summary>
    /// Converts every sharp edge in this SubD to a smooth edge.
    /// </summary>
    /// <returns>The number of edges that were changed.</returns>
    /// <since>8.36</since>
    [CLSCompliant(false)]
    public uint ClearEdgeSharpness()
    {
      IntPtr ptr_this = NonConstPointer();
      uint rc = UnsafeNativeMethods.ON_SubD_ClearEdgeSharpness(ptr_this);
      GC.KeepAlive(this);
      return rc;
    }

#if RHINO_SDK
    /// <summary>
    /// Sets the sharpness of a list of edges.
    /// </summary>
    /// <param name="edges">The edges to change.</param>
    /// <param name="sharpness">
    /// The sharpness to apply to every edge in the list. Pass
    /// <see cref="SubDEdgeSharpness.Smooth"/> to make the edges smooth again.
    /// </param>
    /// <param name="preserveSymmetry">
    /// If true and this SubD has symmetric content, the change tries to keep it symmetric.
    /// </param>
    /// <returns>The number of edges that were changed.</returns>
    /// <since>8.36</since>
    [CLSCompliant(false)]
    public uint SetEdgeSharpness(IEnumerable<SubDEdge> edges, SubDEdgeSharpness sharpness, bool preserveSymmetry)
    {
      if (null == edges)
        throw new ArgumentNullException(nameof(edges));
      if (sharpness.IsNotValidNorCrease)
        throw new ArgumentException("sharpness is not a valid edge sharpness.", nameof(sharpness));

      IntPtr ptr_this = NonConstPointer();
      uint rc;
      using (var ciArray = new INTERNAL_ComponentIndexArray())
      {
        foreach (var edge in edges)
        {
          IntPtr const_ptr_edge = edge.ConstPointer();
          var ci = new ComponentIndex();
          UnsafeNativeMethods.ON_SubDEdge_ComponentIndex(const_ptr_edge, ref ci);
          ciArray.Add(ci);
        }
        IntPtr ptr_ci_array = ciArray.NonConstPointer();
        rc = UnsafeNativeMethods.ON_SubD_SetEdgeSharpness(ptr_this, sharpness, ptr_ci_array, preserveSymmetry);
      }
      GC.KeepAlive(edges);
      GC.KeepAlive(this);
      return rc;
    }

    /// <summary>
    /// Sets the sharpness of a list of edges, one sharpness per edge.
    /// <para>
    /// Use this with <see cref="SubDEdgeSharpness.CreateEdgeChainSharpness(Interval, int)"/>
    /// to give a chain of edges a sharpness that varies evenly along it.
    /// </para>
    /// </summary>
    /// <param name="edges">The edges to change.</param>
    /// <param name="sharpnesses">
    /// One sharpness per edge, in the same order as edges. Must be the same length.
    /// </param>
    /// <param name="preserveSymmetry">
    /// If true and this SubD has symmetric content, the change tries to keep it symmetric.
    /// </param>
    /// <returns>The number of edges that were changed.</returns>
    /// <since>8.36</since>
    [CLSCompliant(false)]
    public uint SetEdgeSharpness(IEnumerable<SubDEdge> edges, IEnumerable<SubDEdgeSharpness> sharpnesses, bool preserveSymmetry)
    {
      if (null == edges)
        throw new ArgumentNullException(nameof(edges));
      if (null == sharpnesses)
        throw new ArgumentNullException(nameof(sharpnesses));

      SubDEdge[] edge_array = edges as SubDEdge[] ?? edges.ToArray();
      SubDEdgeSharpness[] sharp_array = sharpnesses as SubDEdgeSharpness[] ?? sharpnesses.ToArray();
      if (edge_array.Length != sharp_array.Length)
        throw new ArgumentException("edges and sharpnesses must have the same length.", nameof(sharpnesses));
      if (0 == edge_array.Length)
        return 0;

      var cptrs = new SubDComponent.SubDComponentPtr[edge_array.Length];
      for (int i = 0; i < edge_array.Length; i++)
      {
        if (null == edge_array[i])
          throw new ArgumentException("edges contains a null edge.", nameof(edges));
        if (sharp_array[i].IsNotValidNorCrease)
          throw new ArgumentException("sharpnesses contains a value that is not a valid edge sharpness.", nameof(sharpnesses));
        cptrs[i] = edge_array[i].NonConstComponentPtr();
      }

      IntPtr ptr_this = NonConstPointer();
      uint rc = UnsafeNativeMethods.ON_SubD_SetEdgeSharpnessArray(ptr_this, (uint)cptrs.Length, cptrs, sharp_array, preserveSymmetry);
      GC.KeepAlive(edges);
      GC.KeepAlive(this);
      return rc;
    }

    /// <summary>
    /// Sets the sharpness of a list of edges, identified by their ids.
    /// </summary>
    /// <param name="edgeIds">The ids of the edges to change.</param>
    /// <param name="sharpness">
    /// The sharpness to apply to every edge in the list. Pass
    /// <see cref="SubDEdgeSharpness.Smooth"/> to make the edges smooth again.
    /// </param>
    /// <param name="preserveSymmetry">
    /// If true and this SubD has symmetric content, the change tries to keep it symmetric.
    /// </param>
    /// <returns>The number of edges that were changed.</returns>
    /// <since>8.36</since>
    [CLSCompliant(false)]
    public uint SetEdgeSharpness(IEnumerable<int> edgeIds, SubDEdgeSharpness sharpness, bool preserveSymmetry)
    {
      if (null == edgeIds)
        throw new ArgumentNullException(nameof(edgeIds));
      if (sharpness.IsNotValidNorCrease)
        throw new ArgumentException("sharpness is not a valid edge sharpness.", nameof(sharpness));

      IntPtr ptr_this = NonConstPointer();
      uint rc;
      using (var ciArray = new INTERNAL_ComponentIndexArray())
      {
        foreach (var id in edgeIds)
          ciArray.Add(new ComponentIndex(ComponentIndexType.SubdEdge, id));
        IntPtr ptr_ci_array = ciArray.NonConstPointer();
        rc = UnsafeNativeMethods.ON_SubD_SetEdgeSharpness(ptr_this, sharpness, ptr_ci_array, preserveSymmetry);
      }
      GC.KeepAlive(this);
      return rc;
    }
#endif

    /// <summary>
    /// Resets the SubD to the default face packing if adding creases or deleting faces breaks the quad grids.
    /// It does not change the topology or geometry of the SubD. SubD face packs always stop at creases.
    /// </summary>
    /// <returns>The number of face packs.</returns>
    /// <since>7.23</since>
    [CLSCompliant(false)]
    public uint PackFaces()
    {
      IntPtr ptr_this = NonConstPointer();
      uint rc = UnsafeNativeMethods.ON_SubD_PackFaces(ptr_this);
      GC.KeepAlive(this);
      return rc;
    }

    /// <summary>
    /// Makes a new SubD with vertices offset at distance in the direction of the control net vertex normals.
    /// Optionally, based on the value of solidify, adds the input SubD and a ribbon of faces along any naked edges.
    /// </summary>
    /// <param name="distance">The distance to offset.</param>
    /// <param name="solidify">true if the output SubD should be turned into a closed SubD.</param>
    /// <returns>A new SubD if successful, or null on failure.</returns>
    /// <since>7.0</since>
    [ConstOperation]
    public SubD Offset(double distance, bool solidify)
    {
      IntPtr const_ptr_this = ConstPointer();
      IntPtr ptr_subd = UnsafeNativeMethods.RHC_RhinoOffsetSubD(const_ptr_this, distance, solidify);
      if (IntPtr.Zero == ptr_subd)
        return null;
      GC.KeepAlive(this);
      return new SubD(ptr_subd, null);
    }

    /// <summary>
    /// Creates a SubD lofted through shape curves.
    /// </summary>
    /// <param name="curves">An enumeration of SubD-friendly NURBS curves to loft through.</param>
    /// <param name="closed">Creates a SubD that is closed in the lofting direction. Must have three or more shape curves.</param>
    /// <param name="addCorners">With open curves, adds creased vertices to the SubD at both ends of the first and last curves.</param>
    /// <param name="addCreases">With kinked curves, adds creased edges to the SubD along the kinks.</param>
    /// <param name="divisions">The segment number between adjacent input curves.</param>
    /// <returns>A new SubD if successful, or null on failure.</returns>
    /// <remarks>
    /// Shape curves must be in the proper order and orientation and have point counts for the desired surface.
    /// Shape curves must be either all open or all closed.
    /// </remarks>
    /// <since>7.0</since>
    public static SubD CreateFromLoft(IEnumerable<NurbsCurve> curves, bool closed, bool addCorners, bool addCreases, int divisions)
    {
      using (var curves_array = new SimpleArrayCurvePointer(curves))
      {
        IntPtr const_ptr_curves = curves_array.ConstPointer();
        IntPtr ptr_subd = UnsafeNativeMethods.RHC_RhinoSubDLoft(const_ptr_curves, closed, addCorners, addCreases, divisions);
        GC.KeepAlive(curves);
        if (IntPtr.Zero == ptr_subd)
          return null;
        return new SubD(ptr_subd, null);
      }
    }

    /// <summary>
    /// Fits a SubD through a series of profile curves that define the SubD cross-sections and one curve that defines a SubD edge.
    /// </summary>
    /// <param name="rail1">A SubD-friendly NURBS curve to sweep along.</param>
    /// <param name="shapes">An enumeration of SubD-friendly NURBS curves to sweep through.</param>
    /// <param name="closed">Creates a SubD that is closed in the rail curve direction.</param>
    /// <param name="addCorners">With open curves, adds creased vertices to the SubD at both ends of the first and last curves.</param>
    /// <param name="roadlikeFrame">
    /// Determines how sweep frame rotations are calculated.
    /// If false (Freeform), frame are propagated based on a reference direction taken from the rail curve curvature direction.
    /// If true (Roadlike), frame rotations are calculated based on a vector supplied in "roadlikeNormal" and the world coordinate system.
    /// </param>
    /// <param name="roadlikeNormal">
    /// If roadlikeFrame = true, provide 3D vector used to calculate the frame rotations for sweep shapes.
    /// If roadlikeFrame = false, then pass <see cref=" Vector3d.Unset"/>.
    /// </param>
    /// <returns>A new SubD if successful, or null on failure.</returns>
    /// <remarks>
    /// Shape curves must be in the proper order and orientation.
    /// Shape curves must have the same point counts and rail curves must have the same point counts.
    /// Shape curves will relocated to the nearest pair of Greville points on the rails.
    /// Shape curves will be made at each pair of rail edit points where there isn't an input shape.
    /// </remarks>
    /// <since>7.0</since>
    public static SubD CreateFromSweep(NurbsCurve rail1, IEnumerable<NurbsCurve> shapes, bool closed, bool addCorners, bool roadlikeFrame, Vector3d roadlikeNormal)
    {
      if (null == rail1)
        throw new ArgumentNullException(nameof(rail1));
      using (var curves_array = new SimpleArrayCurvePointer(shapes))
      {
        IntPtr const_ptr_rail1 = rail1.ConstPointer();
        IntPtr const_ptr_curves = curves_array.ConstPointer();
        IntPtr ptr_subd = UnsafeNativeMethods.RHC_RhinoSubDSweep1(const_ptr_rail1, const_ptr_curves, closed, addCorners, roadlikeFrame, roadlikeNormal);
        GC.KeepAlive(rail1);
        GC.KeepAlive(shapes);
        if (IntPtr.Zero == ptr_subd)
          return null;
        return new SubD(ptr_subd, null);
      }
    }

    /// <summary>
    /// Fits a SubD through a series of profile curves that define the SubD cross-sections and two curves that defines SubD edges.
    /// </summary>
    /// <param name="rail1">The first SubD-friendly NURBS curve to sweep along.</param>
    /// <param name="rail2">The second SubD-friendly NURBS curve to sweep along.</param>
    /// <param name="shapes">An enumeration of SubD-friendly NURBS curves to sweep through.</param>
    /// <param name="closed">Creates a SubD that is closed in the rail curve direction.</param>
    /// <param name="addCorners">With open curves, adds creased vertices to the SubD at both ends of the first and last curves.</param>
    /// <returns>A new SubD if successful, or null on failure.</returns>
    /// <remarks>
    /// Shape curves must be in the proper order and orientation.
    /// Shape curves must have the same point counts and rail curves must have the same point counts.
    /// Shape curves will relocated to the nearest pair of Greville points on the rails.
    /// Shape curves will be made at each pair of rail edit points where there isn't an input shape.
    /// </remarks>
    /// <since>7.0</since>
    public static SubD CreateFromSweep(NurbsCurve rail1, NurbsCurve rail2, IEnumerable<NurbsCurve> shapes, bool closed, bool addCorners)
    {
      if (null == rail1)
        throw new ArgumentNullException(nameof(rail1));
      if (null == rail2)
        throw new ArgumentNullException(nameof(rail2));
      using (var curves_array = new SimpleArrayCurvePointer(shapes))
      {
        IntPtr const_ptr_rail1 = rail1.ConstPointer();
        IntPtr const_ptr_rail2 = rail2.ConstPointer();
        IntPtr const_ptr_curves = curves_array.ConstPointer();
        IntPtr ptr_subd = UnsafeNativeMethods.RHC_RhinoSubDSweep2(const_ptr_rail1, const_ptr_rail2, const_ptr_curves, closed, addCorners);
        Runtime.CommonObject.GcProtect(rail1, rail2);
        GC.KeepAlive(shapes);
        if (IntPtr.Zero == ptr_subd)
          return null;
        return new SubD(ptr_subd, null);
      }
    }

    /// <summary>
    /// Merges adjacent coplanar faces into single faces.
    /// </summary>
    /// <param name="tolerance">
    /// Tolerance for determining when edges are adjacent.
    /// When in doubt, use the document's ModelAbsoluteTolerance property.
    /// </param>
    /// <returns>true if faces were merged, false if no faces were merged.</returns>
    /// <since>7.9</since>
    public bool MergeAllCoplanarFaces(double tolerance)
    {
      return MergeAllCoplanarFaces(tolerance, RhinoMath.UnsetValue);
    }

    /// <summary>
    /// Merges adjacent coplanar faces into single faces.
    /// </summary>
    /// <param name="tolerance">
    /// Tolerance for determining when edges are adjacent.
    /// When in doubt, use the document's ModelAbsoluteTolerance property.
    /// </param>
    /// <param name="angleTolerance">
    /// Angle tolerance, in radians, for determining when faces are parallel.
    /// When in doubt, use the document's ModelAngleToleranceRadians property.
    /// </param>
    /// <returns>true if faces were merged, false if no faces were merged.</returns>
    /// <since>7.9</since>
    public bool MergeAllCoplanarFaces(double tolerance, double angleTolerance)
    {
      IntPtr ptrThis = NonConstPointer();
      bool rc = UnsafeNativeMethods.RHC_RhinoMergeAllCoplanarFaces(ptrThis, tolerance, angleTolerance);
      GC.KeepAlive(this);
      return rc;
    }
#endif

    /// <summary>
    /// Creates a SubD form of a cylinder.
    /// </summary>
    /// <param name="cylinder">The defining cylinder.</param>
    /// <param name="circumferenceFaceCount">Number of faces around the cylinder.</param>
    /// <param name="heightFaceCount">Number of faces in the top-to-bottom direction.</param>
    /// <param name="endCapStyle">The end cap style.</param>
    /// <param name="endCapEdgeTag">The end cap edge tag.</param>
    /// <param name="radiusLocation">The SubD component location.</param>
    /// <returns>A new SubD if successful, or null on failure.</returns>
    /// <since>7.6</since>
    [CLSCompliant(false)]
    public static SubD CreateFromCylinder(Cylinder cylinder, uint circumferenceFaceCount, uint heightFaceCount, SubDEndCapStyle endCapStyle, SubDEdgeTag endCapEdgeTag, SubDComponentLocation radiusLocation)
    {
      IntPtr ptr_subd = UnsafeNativeMethods.ON_SubD_CreateCylinder(ref cylinder, circumferenceFaceCount, heightFaceCount, endCapStyle, endCapEdgeTag, radiusLocation);
      return IntPtr.Zero == ptr_subd ? null : new SubD(ptr_subd, null);
    }

    /// <summary>
    /// Clear all cached evaluation information (meshes, surface points, bounding boxes, ...) 
    /// that depends on edge tags, vertex tags, and the location of vertex control points.
    /// </summary>
    /// <seealso cref="SubD.CopyEvaluationCache(in SubD)"/>
    /// <seealso cref="SubD.UpdateSurfaceMeshCache(bool)"/>
    /// <since>7.0</since>
    public void ClearEvaluationCache()
    {
      // TODO: Can this be const and not delete the RhinoCommon display cache?
      var ptr_this = NonConstPointer();
      UnsafeNativeMethods.ON_SubD_ClearEvaluationCache(ptr_this);
      GC.KeepAlive(this);
    }

    /// <summary>
    /// Expert function that copies cached evaluations of component subdivision points and
    /// limit surface information from src to this. Typically this is done for performance
    /// critical situations like control point editing:
    ///   - Copy a SubD to be modified (this does not copy the evaluation cache)
    ///   - Copy the evaluation cache from the unmodified SubD
    ///   - Modify the SubD copy
    ///   - Update the surface mesh cache so that only the modified parts are recalculated
    ///   - Display, meshing, bounding boxes on the modified SubD are now available
    /// </summary>
    /// <param name="src">The SubD from which to copy cached evaluations</param>
    /// <returns>True if the cache was fully copied, false otherwise</returns>
    /// <seealso cref="SubD.ClearEvaluationCache()"/>
    /// <seealso cref="SubD.UpdateSurfaceMeshCache(bool)"/>
    /// <since>8.8</since>
    public bool CopyEvaluationCache(in SubD src)
    {
      if (IsNonConst)
      {
        // TODO: This could keep the display cache when the copy is successful as it meant SubDs were identical
        var ptr_this = NonConstPointer();
        bool rc = UnsafeNativeMethods.ON_SubD_CopyEvaluationCache(ptr_this, src.ConstPointer());
        GC.KeepAlive(src);
        GC.KeepAlive(this);
        return rc;
      }
      else
      {
        // TODO: Should this be non const and delete the RhinoCommon display cache?
        var const_ptr_this = ConstPointer();
        // If the cache is copied, SubDs were identical, no need to delete the display
        // DestroySubDDisplay();
        bool rc = UnsafeNativeMethods.ON_SubD_ConstCopyEvaluationCache(const_ptr_this, src.ConstPointer());
        GC.KeepAlive(src);
        GC.KeepAlive(this);
        return rc;
      }
    }

    // These two are backed by ON_SubD::UpdateSurfaceMeshCache / SurfaceMeshCacheExists,
    // which are declared inside #if defined(OPENNURBS_PLUS) and implemented only in
    // opennurbs_plus_subd_*.cpp. They are therefore unavailable to an opennurbs-only
    // build such as Rhino3dm, where the corresponding C exports do not exist.
#if RHINO_SDK
    /// <summary>
    /// Updates limit surface information returned by
    ///   - <see cref="SubDVertex.SurfacePoint()"/>, 
    ///   - <see cref="SubDEdge.ToNurbsCurve(bool)"/>, and
    ///   - <see cref="Mesh.CreateFromSubD(SubD, int)"/>.
    /// The density of the mesh cache is <see cref="SubDDisplayParameters.Default"/>.
    /// </summary>
    /// <param name="lazyUpdate">
    /// If false, all information is updated.
    /// If true, only missing information is updated. If a relatively small subset
    /// of a SubD has been modified and care was taken to mark cached subdivision
    /// information as stale, then passing true can substantially improve performance.
    /// </param>
    /// <returns>The number of elements that were updated.</returns>
    /// <seealso cref="SubD.ClearEvaluationCache()"/>
    /// <seealso cref="SubD.CopyEvaluationCache(in SubD)"/>
    /// <since>8.7</since>
    [CLSCompliant(false)]
    public uint UpdateSurfaceMeshCache(bool lazyUpdate)
    {
      if (IsNonConst)
      {
        // TODO: This could keep the display cache when ON_SubD_UpdateSurfaceMeshCache returns 0
        var ptr_this = NonConstPointer();
        uint rc = UnsafeNativeMethods.ON_SubD_UpdateSurfaceMeshCache(ptr_this, lazyUpdate);
        GC.KeepAlive(this);
        return rc;
      }
      else
      {
        // TODO: Should this be non const and automatically delete the RhinoCommon display cache?
        var const_ptr_this = ConstPointer();
        uint rc = UnsafeNativeMethods.ON_SubD_ConstUpdateSurfaceMeshCache(const_ptr_this, lazyUpdate);
        //if (rc > 0) DestroySubDDisplay();
        GC.KeepAlive(this);
        return rc;
      }
    }

    /// <summary>
    /// Checks that a surface mesh evaluation cache exists, and that it has the required options.
    /// This cache is used by
    ///   - <see cref="SubDVertex.SurfacePoint()"/>, 
    ///   - <see cref="SubDEdge.ToNurbsCurve(bool)"/>, and
    ///   - <see cref="Mesh.CreateFromSubD(SubD, int)"/>.
    /// </summary>
    /// <param name="bTextureCoordinatesExist">If True, the cache must contain texture coordinates information.</param>
    /// <param name="bCurvaturesExist">If True, the cache must contain curvature information.</param>
    /// <param name="bColorsExist">If True, the cache must contain color information.</param>
    /// <returns>True if the cache exists on all face fragments and has the required options.</returns>
    /// <remarks>This does not check that the cache is up to date, <see cref="SubD.UpdateSurfaceMeshCache(bool)"/>.</remarks>
    /// <seealso cref="SubD.ClearEvaluationCache()"/>
    /// <seealso cref="SubD.CopyEvaluationCache(in SubD)"/>
    /// <since>8.9</since>
    [CLSCompliant(false)]
    public bool SurfaceMeshCacheExists(bool bTextureCoordinatesExist, bool bCurvaturesExist, bool bColorsExist)
    {
      var const_ptr_this = ConstPointer();
      bool rc = UnsafeNativeMethods.ON_SubD_SurfaceMeshCacheExists(const_ptr_this, bTextureCoordinatesExist, bCurvaturesExist, bColorsExist);
      GC.KeepAlive(this);
      return rc;
    }
#endif

#if RHINO_SDK
    /// <summary>
    /// Gets the point on this SubD's surface that is closest to a test point.
    /// </summary>
    /// <param name="testPoint">The point to project onto this SubD.</param>
    /// <param name="closestPoint">
    /// The closest point is returned here, or <see cref="Point3d.Unset"/> if this
    /// fails.
    /// </param>
    /// <param name="parameter">
    /// The parameter of the closest point is returned here, or
    /// <see cref="SubDComponentParameter.Unset"/> if this fails. Evaluating it
    /// with <see cref="Evaluate(SubDComponentParameter, out Point3d)"/> returns
    /// <paramref name="closestPoint"/>.
    /// </param>
    /// <returns>True if a closest point was found.</returns>
    /// <remarks>
    /// The search is refined on the surface itself, so the result is not limited
    /// to the surface mesh. The one exception is the corner of an extraordinary
    /// vertex, where the surface derivatives needed to refine are not available;
    /// there the result is the closest surface mesh point, so its accuracy is the
    /// accuracy of the surface mesh. Call
    /// <see cref="UpdateSurfaceMeshCache(bool)"/> first if you need more accuracy
    /// near extraordinary vertices.
    /// </remarks>
    /// <since>9.0</since>
    [ConstOperation]
    public bool ClosestPoint(Point3d testPoint, out Point3d closestPoint, out SubDComponentParameter parameter)
    {
      return ClosestPoint(testPoint, out closestPoint, out parameter, 0.0);
    }

    /// <summary>
    /// Gets the point on this SubD's surface that is closest to a test point.
    /// </summary>
    /// <param name="testPoint">The point to project onto this SubD.</param>
    /// <param name="closestPoint">
    /// The closest point is returned here, or <see cref="Point3d.Unset"/> if this
    /// fails.
    /// </param>
    /// <param name="parameter">
    /// The parameter of the closest point is returned here, or
    /// <see cref="SubDComponentParameter.Unset"/> if this fails.
    /// </param>
    /// <param name="maximumDistance">
    /// If larger than 0 and the distance from <paramref name="testPoint"/> to this
    /// SubD is larger than this, then false is returned. Otherwise this is
    /// ignored.
    /// </param>
    /// <returns>True if a closest point was found.</returns>
    /// <since>9.0</since>
    [ConstOperation]
    public bool ClosestPoint(Point3d testPoint, out Point3d closestPoint, out SubDComponentParameter parameter, double maximumDistance)
    {
      closestPoint = Point3d.Unset;
      parameter = SubDComponentParameter.Unset;
      IntPtr const_ptr_this = ConstPointer();
      bool rc = UnsafeNativeMethods.ON_SubD_GetClosestPoint(
        const_ptr_this, testPoint, maximumDistance, ref closestPoint, ref parameter);
      GC.KeepAlive(this);
      return rc;
    }

    /// <summary>
    /// Gets the point on this SubD's surface that is closest to a test point.
    /// </summary>
    /// <param name="testPoint">The point to project onto this SubD.</param>
    /// <returns>
    /// The closest point, or <see cref="Point3d.Unset"/> if none was found.
    /// </returns>
    /// <since>9.0</since>
    [ConstOperation]
    public Point3d ClosestPoint(Point3d testPoint)
    {
      return ClosestPoint(testPoint, out Point3d closestPoint, out _, 0.0)
        ? closestPoint
        : Point3d.Unset;
    }

    /// <summary>
    /// Gets the points on this SubD's surface that are closest to each of a list
    /// of test points. This is faster than calling
    /// <see cref="ClosestPoint(Point3d, out Point3d, out SubDComponentParameter)"/>
    /// in a loop because the spatial index over the surface is built once.
    /// </summary>
    /// <param name="testPoints">The points to project onto this SubD.</param>
    /// <param name="closestPoints">
    /// One point per test point. Entries where no closest point was found are
    /// <see cref="Point3d.Unset"/>.
    /// </param>
    /// <param name="parameters">
    /// One parameter per test point. Entries where no closest point was found are
    /// <see cref="SubDComponentParameter.Unset"/>.
    /// </param>
    /// <param name="maximumDistance">
    /// If larger than 0, test points farther than this from the SubD get no
    /// closest point. Otherwise this is ignored.
    /// </param>
    /// <returns>The number of closest points that were found.</returns>
    /// <since>9.0</since>
    [ConstOperation]
    public int ClosestPoints(
      IEnumerable<Point3d> testPoints,
      out Point3d[] closestPoints,
      out SubDComponentParameter[] parameters,
      double maximumDistance)
    {
      if (testPoints == null)
        throw new ArgumentNullException(nameof(testPoints));

      Point3d[] input = testPoints as Point3d[] ?? testPoints.ToArray();
      closestPoints = new Point3d[input.Length];
      parameters = new SubDComponentParameter[input.Length];
      for (int i = 0; i < input.Length; i++)
      {
        closestPoints[i] = Point3d.Unset;
        parameters[i] = SubDComponentParameter.Unset;
      }
      if (input.Length == 0)
        return 0;

      IntPtr const_ptr_this = ConstPointer();
      uint rc = UnsafeNativeMethods.ON_SubD_GetClosestPoints(
        const_ptr_this, maximumDistance, input.Length, input, closestPoints, parameters);
      GC.KeepAlive(this);
      return (int)rc;
    }

    /// <summary>
    /// Evaluates the location of a point on this SubD's surface.
    /// </summary>
    /// <param name="parameter">The parameter to evaluate.</param>
    /// <param name="point">
    /// The surface point is returned here, or <see cref="Point3d.Unset"/> if this
    /// fails.
    /// </param>
    /// <returns>True if the evaluation succeeded.</returns>
    /// <since>9.0</since>
    [ConstOperation]
    public bool Evaluate(SubDComponentParameter parameter, out Point3d point)
    {
      point = Point3d.Unset;
      IntPtr const_ptr_this = ConstPointer();
      bool rc = UnsafeNativeMethods.ON_SubD_EvaluateSurfacePoint(const_ptr_this, parameter, ref point);
      GC.KeepAlive(this);
      return rc;
    }

    /// <summary>
    /// Evaluates the location and unit normal of a point on this SubD's surface.
    /// </summary>
    /// <param name="parameter">The parameter to evaluate.</param>
    /// <param name="point">
    /// The surface point is returned here, or <see cref="Point3d.Unset"/> if this
    /// fails.
    /// </param>
    /// <param name="normal">
    /// The unit surface normal is returned here, or
    /// <see cref="Vector3d.Unset"/> if this fails.
    /// </param>
    /// <returns>True if the evaluation succeeded.</returns>
    /// <remarks>
    /// The surface derivatives this needs are not available in the corner of an
    /// extraordinary vertex unless the parameter is exactly at the vertex, at the
    /// midpoint of one of its edges, or at the center of the face.
    /// </remarks>
    /// <since>9.0</since>
    [ConstOperation]
    public bool Evaluate(SubDComponentParameter parameter, out Point3d point, out Vector3d normal)
    {
      return Evaluate(parameter, out point, out _, out _, out normal);
    }

    /// <summary>
    /// Evaluates the location, first derivatives and unit normal of a point on
    /// this SubD's surface.
    /// </summary>
    /// <param name="parameter">The parameter to evaluate.</param>
    /// <param name="point">
    /// The surface point is returned here, or <see cref="Point3d.Unset"/> if this
    /// fails.
    /// </param>
    /// <param name="ds">
    /// The first derivative in the direction of the first face corner parameter is
    /// returned here.
    /// </param>
    /// <param name="dt">
    /// The first derivative in the direction of the second face corner parameter
    /// is returned here.
    /// </param>
    /// <param name="normal">
    /// The unit surface normal is returned here. It is parallel to the cross
    /// product of <paramref name="ds"/> and <paramref name="dt"/>.
    /// </param>
    /// <returns>True if the evaluation succeeded.</returns>
    /// <since>9.0</since>
    [ConstOperation]
    public bool Evaluate(
      SubDComponentParameter parameter,
      out Point3d point,
      out Vector3d ds,
      out Vector3d dt,
      out Vector3d normal)
    {
      point = Point3d.Unset;
      ds = Vector3d.Unset;
      dt = Vector3d.Unset;
      normal = Vector3d.Unset;
      IntPtr const_ptr_this = ConstPointer();
      bool rc = UnsafeNativeMethods.ON_SubD_EvaluateSurface(
        const_ptr_this, parameter, false, ref point, ref ds, ref dt, ref normal, null);
      GC.KeepAlive(this);
      return rc;
    }

    /// <summary>
    /// Evaluates the principal curvatures of a point on this SubD's surface.
    /// </summary>
    /// <param name="parameter">The parameter to evaluate.</param>
    /// <param name="point">
    /// The surface point is returned here, or <see cref="Point3d.Unset"/> if this
    /// fails.
    /// </param>
    /// <param name="normal">The unit surface normal is returned here.</param>
    /// <param name="kappa1">
    /// The largest principal curvature, in absolute value, is returned here.
    /// </param>
    /// <param name="kappa2">
    /// The smallest principal curvature, in absolute value, is returned here.
    /// </param>
    /// <returns>True if the evaluation succeeded.</returns>
    /// <remarks>
    /// This needs second order surface derivatives. They are available throughout
    /// the corner quad of an extraordinary vertex, but not exactly on such a
    /// vertex: a SubD limit surface is C1 there but generally not C2, so it has no
    /// curvature. This returns false in that case, and <paramref name="kappa1"/>
    /// and <paramref name="kappa2"/> are <see cref="double.NaN"/>.
    /// </remarks>
    /// <since>9.0</since>
    [ConstOperation]
    public bool EvaluateCurvature(
      SubDComponentParameter parameter,
      out Point3d point,
      out Vector3d normal,
      out double kappa1,
      out double kappa2)
    {
      point = Point3d.Unset;
      normal = Vector3d.Unset;
      kappa1 = double.NaN;
      kappa2 = double.NaN;
      var ds = Vector3d.Unset;
      var dt = Vector3d.Unset;
      var kappa = new double[2] { double.NaN, double.NaN };
      IntPtr const_ptr_this = ConstPointer();
      bool rc = UnsafeNativeMethods.ON_SubD_EvaluateSurface(
        const_ptr_this, parameter, true, ref point, ref ds, ref dt, ref normal, kappa);
      GC.KeepAlive(this);
      if (rc)
      {
        kappa1 = kappa[0];
        kappa2 = kappa[1];
      }
      return rc;
    }

    /// <summary>
    /// Evaluates the curvature of a point on this SubD's surface.
    /// </summary>
    /// <param name="parameter">The parameter to evaluate.</param>
    /// <param name="curvature">
    /// The surface curvature is returned here, or null if this fails.
    /// </param>
    /// <returns>True if the evaluation succeeded.</returns>
    /// <remarks>
    /// Unlike the overload that returns the two principal curvature values, this
    /// one also reports their directions. See
    /// <see cref="SurfaceCurvature.CreateFromSubD(SubD, SubDComponentParameter)"/>
    /// for when the curvature exists.
    /// </remarks>
    /// <since>9.0</since>
    [ConstOperation]
    public bool EvaluateCurvature(SubDComponentParameter parameter, out SurfaceCurvature curvature)
    {
      return EvaluateCurvature(parameter, ExtraordinaryVertexCurvature.None, out curvature);
    }

    /// <summary>
    /// Evaluates the curvature of a point on this SubD's surface, choosing what
    /// to report exactly on an extraordinary vertex.
    /// </summary>
    /// <param name="parameter">The parameter to evaluate.</param>
    /// <param name="extraordinaryVertexCurvature">
    /// What to report when the parameter is exactly on an extraordinary vertex,
    /// where the limit surface has no curvature of its own.
    /// </param>
    /// <param name="curvature">
    /// The surface curvature is returned here, or null if this fails.
    /// </param>
    /// <returns>True if the evaluation succeeded.</returns>
    /// <remarks>
    /// See <see cref="SurfaceCurvature.CreateFromSubD(SubD, SubDComponentParameter, ExtraordinaryVertexCurvature)"/>.
    /// </remarks>
    /// <since>9.0</since>
    [ConstOperation]
    public bool EvaluateCurvature(
      SubDComponentParameter parameter,
      ExtraordinaryVertexCurvature extraordinaryVertexCurvature,
      out SurfaceCurvature curvature)
    {
      curvature = SurfaceCurvature.CreateFromSubD(this, parameter, extraordinaryVertexCurvature);
      return null != curvature;
    }

#endif

    /// <summary>
    /// Returns a SubDComponent, either a SubDEdge, SubDFace, or SubDVertex, from a component index.
    /// </summary>
    /// <param name="componentIndex">The component index.</param>
    /// <returns>The SubDComponent if successful, null otherwise.</returns>
    /// <since>7.6</since>
    public SubDComponent ComponentFromComponentIndex(ComponentIndex componentIndex)
    {
      var const_ptr_this = ConstPointer();
      uint componentId = 0;
      switch (componentIndex.ComponentIndexType)
      {
        case ComponentIndexType.SubdVertex:
          {
            IntPtr ptr = UnsafeNativeMethods.ON_SubD_SubDVertexFromComponentIndex(const_ptr_this, componentIndex, ref componentId);
            return ptr != IntPtr.Zero ? new SubDVertex(this, ptr, componentId) : null;
          }
        case ComponentIndexType.SubdFace:
          {
            IntPtr ptr = UnsafeNativeMethods.ON_SubD_SubDFaceFromComponentIndex(const_ptr_this, componentIndex, ref componentId);
            return ptr != IntPtr.Zero ? new SubDFace(this, ptr, componentId) : null;
          }
        case ComponentIndexType.SubdEdge:
          {
            IntPtr ptr = UnsafeNativeMethods.ON_SubD_SubDEdgeFromComponentIndex(const_ptr_this, componentIndex, ref componentId);
            return ptr != IntPtr.Zero ? new SubDEdge(this, ptr, componentId) : null;
          }
      }
      GC.KeepAlive(this);
      return null;
    }

    /// <summary>
    /// Updates vertex tag, edge tag, and edge coefficient values on the active
    /// level. After completing custom editing operations that modify the
    /// topology of the SubD control net or changing values of vertex or edge
    /// tags, the tag and sector coefficients information on nearby components
    /// in the edited areas need to be updated.
    /// </summary>
    /// <returns>
    /// Number of vertices and edges that were changed during the update.
    /// </returns>
    /// <since>7.0</since>
    [CLSCompliant(false)]
    public uint UpdateAllTagsAndSectorCoefficients()
    {
      var ptr_this = NonConstPointer();
      uint rc = UnsafeNativeMethods.ON_SubD_UpdateAllTagsAndSectorCoefficients(ptr_this, false);
      GC.KeepAlive(this);
      return rc;
    }
    internal static bool IsSubDEdgeTagDefined(SubDEdgeTag tag)
    {
      return tag > SubDEdgeTag.Unset && tag <= SubDEdgeTag.SmoothX;
    }

    internal static bool IsSubDVertexTagDefined(SubDVertexTag tag)
    {
      return tag > SubDVertexTag.Unset && tag <= SubDVertexTag.Dart;
    }

    /// <summary>
    /// Apply the Catmull-Clark subdivision algorithm and save the results in this SubD.
    /// </summary>
    /// <returns>true on success</returns>
    /// <since>8.0</since>
    public bool Subdivide()
    {
      return Subdivide(1);
    }

    /// <summary>
    /// Apply the Catmull-Clark subdivision algorithm and save the results in this SubD.
    /// </summary>
    /// <param name="count">Number of times to subdivide (must be greater than 0)</param>
    /// <returns>true on success</returns>
    /// <since>7.0</since>
    public bool Subdivide(int count)
    {
      if (count < 1)
        return false;
      IntPtr ptrSubD = NonConstPointer();
      bool rc = UnsafeNativeMethods.ON_SubD_GlobalSubdivide(ptrSubD, (uint)count);
      GC.KeepAlive(this);
      return rc;
    }

    /// <summary>
    /// Apply the Catmull-Clark subdivision algorithm and save the results in this SubD.
    /// </summary>
    /// <param name="faceIndices">Indices of the faces to subdivide.</param>
    /// <returns>true on success</returns>
    /// <since>8.0</since>
    public bool Subdivide(IEnumerable<int> faceIndices)
    {
      IntPtr ptr_subd = NonConstPointer();
      using (var ciArray = new INTERNAL_ComponentIndexArray())
      {
        foreach (var index in faceIndices)
          ciArray.Add(new ComponentIndex(ComponentIndexType.SubdFace, index));
        IntPtr pCiArray = ciArray.NonConstPointer();
        GC.KeepAlive(this);
        return UnsafeNativeMethods.ON_SubD_LocalSubdivide(ptr_subd, pCiArray);
      }
    }

#if RHINO_SDK
    /// <summary>
    /// Modifies the SubD so that the SubD vertex limit surface points are
    /// equal to surface_points[]
    /// </summary>
    /// <param name="surfacePoints">
    /// Points for limit surface to interpolate. surface_points[i] is the
    /// location for the i-th vertex returned by SubVertexIterator vit(this)
    /// </param>
    /// <returns>True on success</returns>
    /// <seealso cref="SubD.SetVertexSurfacePoint(uint, Point3d)"/>
    /// <seealso cref="SubDVertex.SurfacePoint()"/>
    /// <seealso cref="SubDSurfaceInterpolator"/>
    /// <since>7.1</since>
    public bool InterpolateSurfacePoints(Point3d[] surfacePoints)
    {
      IntPtr ptrThis = NonConstPointer();
      bool rc = UnsafeNativeMethods.ON_SubD_InterpolateSurfacePoints(ptrThis, surfacePoints.Length, surfacePoints);
      GC.KeepAlive(this);
      return rc;
    }
#endif

#if RHINO_SDK
    /// <summary>
    /// Modifies the SubD so that the SubD vertex limit surface points of the listed vertices are
    /// equal to surface_points[].
    /// </summary>
    /// <param name="vertexIndices">
    /// Ids of the vertices to interpolate. Other vertices remain fixed.
    /// </param>
    /// <param name="surfacePoints">
    /// Points for limit surface to interpolate. surface_points[i] is the
    /// location for the vertex returned by this.Vertices.Find(vertexIndices[i]).
    /// </param>
    /// <returns>True on success</returns>
    /// <seealso cref="SubD.SetVertexSurfacePoint(uint, Point3d)"/>
    /// <seealso cref="SubDVertex.SurfacePoint()"/>
    /// <seealso cref="SubDSurfaceInterpolator"/>
    /// <since>8.0</since>
    [CLSCompliant(false)]
    public bool InterpolateSurfacePoints(uint[] vertexIndices, Point3d[] surfacePoints)
    {
      using (SubDSurfaceInterpolator interpolator = SubDSurfaceInterpolator.CreateFromVertexIdList(this, vertexIndices, out uint freeVertexCount))
      {
        if (freeVertexCount != vertexIndices.Length)
          return false;
        return interpolator.Solve(surfacePoints);
      }
    }

    /// <summary>
    /// Set the location of a single vertex surface point.
    /// This function is not suitable for setting the locations of multiple vertex surface points that are topologically near to each other.
    /// </summary>
    /// <param name="vertexIndex">
    /// Index of the vertex to modify
    /// </param>
    /// <param name="surfacePoint">
    /// New surface point location for that vertex
    /// </param>
    /// <returns>
    /// True if a vertex was modified, false otherwise.
    /// </returns>
    /// <seealso cref="SubD.InterpolateSurfacePoints(Point3d[])"/>
    /// <seealso cref="SubD.InterpolateSurfacePoints(uint[], Point3d[])"/>
    /// <seealso cref="SubDVertex.SurfacePoint()"/>
    /// <seealso cref="SubDSurfaceInterpolator"/>
    /// <since>8.0</since>
    [CLSCompliant(false)]
    public bool SetVertexSurfacePoint(uint vertexIndex, Point3d surfacePoint)
    {
      IntPtr ptrThis = NonConstPointer();
      bool rc = UnsafeNativeMethods.ON_SubD_SetVertexSurfacePoint(ptrThis, vertexIndex, surfacePoint);
      GC.KeepAlive(this);
      return rc;
    }
#endif
  }

#if RHINO_SDK
  /// <summary>
  /// Interpolate some or all of the vertices limit surface positions in a SubD to specified locations.
  /// NB: It is recommended not to use these methods to interpolate more than 1000 vertices.
  /// <seealso cref="SubD.SetVertexSurfacePoint(uint, Point3d)"/>
  /// <seealso cref="SubD.InterpolateSurfacePoints(Point3d[])"/>
  /// <seealso cref="SubD.InterpolateSurfacePoints(uint[], Point3d[])"/>
  /// <seealso cref="SubDVertex.SurfacePoint()"/>
  /// </summary>
  public partial class SubDSurfaceInterpolator : IDisposable
  {
    IntPtr m_ptr;  // ON_SubDSurfaceInterpolator
    internal IntPtr ConstPointer() { return m_ptr; }
    internal IntPtr NonConstPointer() { return m_ptr; }

    /// <summary>
    /// Initialize an empty SubDSurfaceInterpolator.
    /// </summary>
    /// <since>8.0</since>
    public SubDSurfaceInterpolator()
    {
      m_ptr = UnsafeNativeMethods.ON_SubD_SubDSurfaceInterpolator_New();
    }

    /// <summary>
    /// Passively reclaims unmanaged resources when the class user did not explicitly call Dispose().
    /// </summary>
    /// <since>8.0</since>
    ~SubDSurfaceInterpolator()
    {
      Dispose(false);
    }

    /// <summary>
    /// Actively reclaims unmanaged resources that this instance uses.
    /// </summary>
    /// <since>8.0</since>
    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    /// <summary>
    /// For derived class implementers.
    /// <para>This method is called with argument true when class user calls Dispose(), while with argument false when
    /// the Garbage Collector invokes the finalizer, or Finalize() method.</para>
    /// <para>You must reclaim all used unmanaged resources in both cases, and can use this chance to call Dispose on disposable fields if the argument is true.</para>
    /// <para>Also, you must call the base virtual method within your overriding method.</para>
    /// </summary>
    /// <param name="disposing">true if the call comes from the Dispose() method; false if it comes from the Garbage Collector finalizer.</param>
    /// <since>8.0</since>
    protected virtual void Dispose(bool disposing)
    {
      if (IntPtr.Zero != m_ptr)
      {
        UnsafeNativeMethods.ON_SubD_SubDSurfaceInterpolator_Delete(m_ptr);
        m_ptr = IntPtr.Zero;
      }
    }

    /// <summary>
    /// Interpolation requires building a solver.
    /// We estimate that this solver will work in reasonnable time if the number of
    /// interplolated vertices is smaller than MaximumInterpolatedVertexCount.
    /// However, given sufficient time, memory, and CPU resources, the code will work with
    /// any value.
    /// In version 8.0, this value is 1000.
    /// </summary>
    /// <since>8.0</since>
    [CLSCompliant(false)]
    public static uint MaximumRecommendedInterpolatedVertexCount
    {
      get
      {
        return (uint)(SubDSurfaceInterpolator.MaximumCounts.MaximumRecommendedInterpolatedVertexCount); 
      } 
    }

    /// <summary>
    /// Create an interpolator where all the vertices in the SubD are free vertices in the
    /// linear system used for interpolation (i.e. can move as a result of the
    /// interpolation, and can receive an interpolation target location).
    /// </summary>
    /// <param name="subd">The SubD to use for interpolation</param>
    /// <param name="freeVertexCount">The number of free vertices in the system</param>
    /// <returns>A new SubDSurfaceInterpolator</returns>
    /// <remarks>
    /// Sets <see cref="ContextId"/> to the Guid of the Rhino SubD object for subd.
    /// </remarks>
    /// <since>8.0</since>
    [CLSCompliant(false)]
    public static SubDSurfaceInterpolator CreateFromSubD(SubD subd, out uint freeVertexCount)
    {
      SubDSurfaceInterpolator interpolator = new SubDSurfaceInterpolator();
      IntPtr nonConstPtrThis = interpolator.NonConstPointer();
      interpolator.ContextId = subd.ParentRhinoObject().Id;
      freeVertexCount = UnsafeNativeMethods.ON_SubD_SubDSurfaceInterpolator_CreateFromSubD(nonConstPtrThis, subd.NonConstPointer());
      GC.KeepAlive(interpolator);
      GC.KeepAlive(subd);
      return interpolator;
    }

    /// <summary>
    /// Create an interpolator where all the marked vertices (unmarked if
    /// interpolatedVerticesMark is false) in the SubD are free vertices in the linear
    /// system used for interpolation, and the unmarked (marked if interpolatedVerticesMark
    /// is false) are fixed to their initial positions. Free vertices are can move as a
    /// result of the interpolation, and can receive an interpolation target location.
    /// </summary>
    /// <param name="subd">The SubD to use for interpolation</param>
    /// <param name="interpolatedVerticesMark">If True, marked vertices will be considered free, and unmarked vertices will be fixed.</param>
    /// <param name="freeVertexCount">The number of free vertices in the system</param>
    /// <returns>A new SubDSurfaceInterpolator</returns>
    /// <remarks>
    /// Sets <see cref="ContextId"/> to the Guid of the Rhino SubD object for subd.
    /// </remarks>
    /// <since>8.0</since>
    [CLSCompliant(false)]
    public static SubDSurfaceInterpolator CreateFromMarkedVertices(SubD subd, bool interpolatedVerticesMark, out uint freeVertexCount)
    {
      SubDSurfaceInterpolator interpolator = new SubDSurfaceInterpolator();
      IntPtr nonConstPtrThis = interpolator.NonConstPointer();
      interpolator.ContextId = subd.ParentRhinoObject().Id;
      freeVertexCount = UnsafeNativeMethods.ON_SubD_SubDSurfaceInterpolator_CreateFromMarkedVertices(nonConstPtrThis, subd.NonConstPointer(), interpolatedVerticesMark);
      GC.KeepAlive(interpolator);
      GC.KeepAlive(subd);
      return interpolator;
    }

    /// <summary>
    /// Create an interpolator where all the selected vertices in the SubD are free
    /// vertices in the linear system used for interpolation, and the unselected are fixed
    /// to their initial positions. Free vertices are can move as a result of the
    /// interpolation, and can receive an interpolation target location.
    /// </summary>
    /// <param name="subd">The SubD to use for interpolation</param>
    /// <param name="freeVertexCount">The number of free vertices in the system</param>
    /// <returns>A new SubDSurfaceInterpolator</returns>
    /// <remarks>
    /// Sets <see cref="ContextId"/> to the Guid of the Rhino SubD object for subd.
    /// </remarks>
    /// <since>8.0</since>
    [CLSCompliant(false)]
    public static SubDSurfaceInterpolator CreateFromSelectedVertices(SubD subd, out uint freeVertexCount)
    {
      SubDSurfaceInterpolator interpolator = new SubDSurfaceInterpolator();
      IntPtr nonConstPtrThis = interpolator.NonConstPointer();
      interpolator.ContextId = subd.ParentRhinoObject().Id;
      freeVertexCount = UnsafeNativeMethods.ON_SubD_SubDSurfaceInterpolator_CreateFromSelectedVertices(nonConstPtrThis, subd.NonConstPointer());
      GC.KeepAlive(interpolator);
      GC.KeepAlive(subd);
      return interpolator;
    }

    /// <summary>
    /// Create an interpolator where all the listed vertices in the SubD are free
    /// vertices in the linear system used for interpolation, and the unselected are fixed
    /// to their initial positions. Free vertices are can move as a result of the
    /// interpolation, and can receive an interpolation target location.
    /// </summary>
    /// <param name="subd">The SubD to use for interpolation</param>
    /// <param name="vertexIndices">Indices of the vertices to be interpolated</param>
    /// <param name="freeVertexCount">The number of free vertices in the system</param>
    /// <returns>A new SubDSurfaceInterpolator</returns>
    /// <remarks>
    /// Sets <see cref="ContextId"/> to the Guid of the Rhino SubD object for subd.
    /// </remarks>
    /// <since>8.0</since>
    [CLSCompliant(false)]
    public static SubDSurfaceInterpolator CreateFromVertexIdList(SubD subd, IEnumerable<uint> vertexIndices, out uint freeVertexCount)
    {
      SubDSurfaceInterpolator interpolator = new SubDSurfaceInterpolator();
      IntPtr nonConstPtrThis = interpolator.NonConstPointer();
      interpolator.ContextId = subd.ParentRhinoObject().Id;
      using (var simpleArray = new SimpleArrayUint(vertexIndices))
      {
        IntPtr ptrConstArray = simpleArray.ConstPointer();
        freeVertexCount = UnsafeNativeMethods.ON_SubD_SubDSurfaceInterpolator_CreateFromVertexIdList(nonConstPtrThis, subd.NonConstPointer(), ptrConstArray);
      }
      GC.KeepAlive(interpolator);
      GC.KeepAlive(subd);
      return interpolator;
    }

    /// <summary>
    /// Destroys the information needed to solve the interpolation.
    /// </summary>
    /// <since>8.0</since>
    public void Clear()
    {
      IntPtr nonConstPtrThis = NonConstPointer();
      UnsafeNativeMethods.ON_SubD_SubDSurfaceInterpolator_Clear(nonConstPtrThis);
      GC.KeepAlive(this);
    }

    /// <returns>Number of vertices with interpolated surface points.</returns>
    /// <since>8.0</since>
    [CLSCompliant(false), ConstOperation]
    public uint InterpolatedVertexCount()
    {
      IntPtr constPtrThis = ConstPointer();
      uint rc = UnsafeNativeMethods.ON_SubD_SubDSurfaceInterpolator_InterpolatedVertexCount(constPtrThis);
      GC.KeepAlive(this);
      return rc;
    }

    /// <returns>Number of vertices with fixed surface points.</returns>
    /// <since>8.0</since>
    [CLSCompliant(false), ConstOperation]
    public uint FixedVertexCount()
    {
      IntPtr constPtrThis = ConstPointer();
      uint rc = UnsafeNativeMethods.ON_SubD_SubDSurfaceInterpolator_FixedVertexCount(constPtrThis);
      GC.KeepAlive(this);
      return rc;
    }

    /// <returns>True if the vertex surface point is being interpolated.</returns>
    /// <since>8.0</since>
    [CLSCompliant(false), ConstOperation]
    public bool IsInterpolatedVertex(uint vertexId)
    {
      IntPtr constPtrThis = ConstPointer();
      bool rc = UnsafeNativeMethods.ON_SubD_SubDSurfaceInterpolator_IsInterpolatedVertex(constPtrThis, vertexId);
      GC.KeepAlive(this);
      return rc;
    }

    /// <returns>True if the vertex surface point is being interpolated.</returns>
    /// <since>8.0</since>
    [CLSCompliant(false), ConstOperation]
    public bool IsInterpolatedVertex(SubDVertex vertex)
    {
      IntPtr constPtrThis = ConstPointer();
      bool rc = UnsafeNativeMethods.ON_SubD_SubDSurfaceInterpolator_IsInterpolatedVertex(constPtrThis, vertex.Id);
      GC.KeepAlive(this);
      return rc;
    }

    /// <summary>
    /// Solve the interpolation system, given target interpolation locations for the free
    /// vertices in the system. Updates the subd referenced by this system so the corresponding
    /// surface points are at the locations given by surfacePoints.
    /// </summary>
    /// <param name="surfacePoints">
    /// The limit surface locations for the interpolated vertices. The number of desired locations
    /// needs to match the <see cref="InterpolatedVertexCount()"/>.
    /// </param>
    /// <returns>True if a solution was found.</returns>
    /// <since>8.0</since>
    public bool Solve(Point3d[] surfacePoints)
    {
      IntPtr nonConstPtrThis = NonConstPointer();
      bool rc = UnsafeNativeMethods.ON_SubD_SubDSurfaceInterpolator_Solve(nonConstPtrThis, surfacePoints);
      GC.KeepAlive(this);
      return rc;
    }

    /// <returns>
    /// If vertex is an interpolated vertex, returns the index of the vertex in the array returned
    /// by <see cref="VertexIdList()"/>. Otherwise, returns ON_UNSET_UINT_INDEX.
    /// </returns>
    /// <since>8.0</since>
    [CLSCompliant(false), ConstOperation]
    public uint InterpolatedVertexIndex(uint vertexId)
    {
      IntPtr constPtrThis = ConstPointer();
      uint rc = UnsafeNativeMethods.ON_SubD_SubDSurfaceInterpolator_InterpolatedVertexIndex(constPtrThis, vertexId);
      GC.KeepAlive(this);
      return rc;
    }

    /// <summary>
    /// The context assigned id. This id is provided for applications using
    /// ON_SubDSurfaceInterpolator. It is not inspected or used in any part of the interpolation
    /// setup or calculations.
    /// </summary>
    /// <remarks>
    /// In Rhino, when an interpolator is being used to modify a CRhinoSubDObject, this id
    /// is often the Rhino object id.
    /// </remarks>
    /// <since>8.0</since>
    public Guid ContextId
    {
      get
      {
        IntPtr constPtrThis = ConstPointer();
        Guid rc = UnsafeNativeMethods.ON_SubD_SubDSurfaceInterpolator_ContextId(constPtrThis);
        GC.KeepAlive(this);
        return rc;
      }
      set
      {
        IntPtr nonConstPtrThis = NonConstPointer();
        UnsafeNativeMethods.ON_SubD_SubDSurfaceInterpolator_SetContextId(nonConstPtrThis, value);
        GC.KeepAlive(this);
      }
    }

    /// <returns>
    /// List of indices of the vertices with interpolated surface points. Vertices that are not in
    /// this vertex list have unchanged control net points.
    /// </returns>
    /// <since>8.0</since>
    [CLSCompliant(false), ConstOperation]
    public uint[] VertexIdList()
    {
      IntPtr constPtrThis = ConstPointer();
      using (SimpleArrayUint vertexIds = new SimpleArrayUint())
      {
        UnsafeNativeMethods.ON_SubD_SubDSurfaceInterpolator_VertexIdList(constPtrThis, vertexIds.NonConstPointer());
        GC.KeepAlive(this);
        return vertexIds.ToArray();
      }
    }

    /// <summary>
    /// Apply an arbitrary transformation to the target interpolation points.
    /// </summary>
    /// <param name="transform">The transformation to apply.</param>
    /// <since>8.0</since>
    public void Transform(Transform transform)
    {
      IntPtr nonConstPtrThis = NonConstPointer();
      UnsafeNativeMethods.ON_SubD_SubDSurfaceInterpolator_Transform(nonConstPtrThis, ref transform);
      GC.KeepAlive(this);
    }
  }
#endif



  /// <summary>
  /// Options used for creating a SubD
  /// </summary>
  public partial class SubDCreationOptions : IDisposable
  {
    IntPtr m_ptr; // ON_ToSubDParameters*
    internal IntPtr ConstPointer() { return m_ptr; }
    internal IntPtr NonConstPointer() { return m_ptr; }

    /// <summary>
    /// Create default options
    /// </summary>
    /// <since>7.0</since>
    public SubDCreationOptions() : this(0)
    {
    }

    SubDCreationOptions(UnsafeNativeMethods.OnSubDMeshParameterTypeConsts which)
    {

      m_ptr = UnsafeNativeMethods.ON_ToSubDParameters_New(which);
    }

    /// <summary>
    /// No interior creases and no corners.
    /// </summary>
    /// <since>7.0</since>
    public static SubDCreationOptions Smooth
    {
      get
      {
        return new SubDCreationOptions(UnsafeNativeMethods.OnSubDMeshParameterTypeConsts.Smooth);
      }
    }

    /// <summary>
    /// Create an interior sub-D crease along all input mesh double edges
    /// </summary>
    /// <since>7.0</since>
    public static SubDCreationOptions InteriorCreases
    {
      get
      {
        return new SubDCreationOptions(UnsafeNativeMethods.OnSubDMeshParameterTypeConsts.InteriorCreases);
      }
    }

    /// <summary>
    /// Look for convex corners at sub-D vertices with 2 edges or fewer that have an
    /// included angle ≤ 120 degrees.
    /// </summary>
    /// <since>7.0</since>
    public static SubDCreationOptions ConvexCornersAndInteriorCreases
    {
      get
      {
        return new SubDCreationOptions(UnsafeNativeMethods.OnSubDMeshParameterTypeConsts.ConvexCornersAndInteriorCreases);
      }
    }

    /// <summary>
    /// Look for convex corners at sub-D vertices with 2 edges or fewer that have an
    /// included angle ≤ 120 degrees.
    /// Look for concave corners at sub-D vertices with 3 edges or more that have an
    /// included angle ≥ 240 degrees.
    /// </summary>
    /// <since>8.0</since>
    public static SubDCreationOptions ConvexAndConcaveCornersAndInteriorCreases
    {
      get
      {
        return new SubDCreationOptions(UnsafeNativeMethods.OnSubDMeshParameterTypeConsts.ConvexAndConcaveCornersAndInteriorCreases);
      }
    }

    /// <summary>
    /// Finalizer
    /// </summary>
    ~SubDCreationOptions()
    {
      Dispose();
    }

    /// <summary>
    /// Delete unmanaged pointer for this
    /// </summary>
    /// <since>7.0</since>
    public void Dispose()
    {
      if (m_ptr != IntPtr.Zero)
        UnsafeNativeMethods.ON_ToSubDParameters_Delete(m_ptr);
      m_ptr = IntPtr.Zero;
    }

    /// <summary>
    /// Get or sets the interior crease test option.
    /// </summary>
    /// <since>7.0</since>
    public InteriorCreaseOption InteriorCreaseTest
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        uint rc = UnsafeNativeMethods.ON_ToSubDParameters_InteriorCreaseOption(const_ptr_this);
        GC.KeepAlive(this);
        return (InteriorCreaseOption)rc;
      }
      set
      {
        IntPtr ptr_this = NonConstPointer();
        UnsafeNativeMethods.ON_ToSubDParameters_SetInteriorCreaseOption(ptr_this, (uint)value);
        GC.KeepAlive(this);
      }
    }

    /// <summary>
    /// Get or sets the convex corner test option.
    /// </summary>
    /// <since>7.0</since>
    public ConvexCornerOption ConvexCornerTest
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        uint rc = UnsafeNativeMethods.ON_ToSubDParameters_ConvexCornerOption(const_ptr_this);
        GC.KeepAlive(this);
        return (ConvexCornerOption)rc;
      }
      set
      {
        IntPtr ptr_this = NonConstPointer();
        UnsafeNativeMethods.ON_ToSubDParameters_SetConvexCornerOption(ptr_this, (uint)value);
        GC.KeepAlive(this);
      }
    }

    /// <summary>
    /// Gets or sets the texture coordinate test option.
    /// </summary>
    /// <since>8.28</since>
    public TextureCoordinateOption TextureCoordinateTest
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        uint rc = UnsafeNativeMethods.ON_ToSubDParameters_TextureCoordinatesOption(const_ptr_this);
        GC.KeepAlive(this);
        return (TextureCoordinateOption)rc;
      }
      set
      {
        IntPtr ptr_this = NonConstPointer();
        UnsafeNativeMethods.ON_ToSubDParameters_SetTextureCoordinatesOption(ptr_this, (uint)value);
        GC.KeepAlive(this);
      }
    }

    /// <summary>
    /// If ConvexCornerTest == ConvexCornerOption.AtMeshCorner, then an input mesh boundary
    /// vertex becomes a SubD corner when the number of edges that end at the
    /// vertex is &lt;= MaximumConvexCornerEdgeCount edges and the corner angle
    /// is &lt;= MaximumConvexCornerAngleRadians.
    /// </summary>
    /// <since>7.0</since>
    [CLSCompliant(false)]
    public uint MaximumConvexCornerEdgeCount
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        uint rc = UnsafeNativeMethods.ON_ToSubDParameters_MaximumConvexCornerEdgeCount(const_ptr_this);
        GC.KeepAlive(this);
        return rc;
      }
      set
      {
        IntPtr ptr_this = NonConstPointer();
        UnsafeNativeMethods.ON_ToSubDParameters_SetMaximumConvexCornerEdgeCount(ptr_this, value);
        GC.KeepAlive(this);
      }
    }

    /// <summary>
    /// If ConvexCornerTest == ConvexCornerOption.AtMeshCorner, then an input mesh boundary
    /// vertex becomes a SubD corner when the number of edges that end at the
    /// vertex is &lt;= MaximumConvexCornerEdgeCount edges and the corner angle
    /// is &lt;= MaximumConvexCornerAngleRadians.
    /// </summary>
    /// <since>7.0</since>
    public double MaximumConvexCornerAngleRadians
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        double rc = UnsafeNativeMethods.ON_ToSubDParameters_MaximumConvexCornerAngleRadians(const_ptr_this);
        GC.KeepAlive(this);
        return rc;
      }
      set
      {
        IntPtr ptr_this = NonConstPointer();
        UnsafeNativeMethods.ON_ToSubDParameters_SetMaximumConvexCornerAngleRadians(ptr_this, value);
        GC.KeepAlive(this);
      }
    }

    /// <summary>
    /// Get or sets the concave corner test option.
    /// </summary>
    /// <since>7.0</since>
    public SubDCreationOptions.ConcaveCornerOption ConcaveCornerTest
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        uint rc = UnsafeNativeMethods.ON_ToSubDParameters_ConcaveCornerOption(const_ptr_this);
        GC.KeepAlive(this);
        return (ConcaveCornerOption)rc;
      }
      set
      {
        IntPtr ptr_this = NonConstPointer();
        UnsafeNativeMethods.ON_ToSubDParameters_SetConcaveCornerOption(ptr_this, (uint)value);
        GC.KeepAlive(this);
      }
    }

    /// <summary>
    /// If ConcaveCornerTest == ConcaveCornerOption.AtMeshCorner, then an
    /// input mesh boundary vertex becomes a SubD corner when the number of
    /// edges that end at the vertex is &gt;= MinimumConcaveCornerEdgeCount edges
    /// and the corner angle is &gt;= MinimumConcaveCornerAngleRadians.
    /// </summary>
    /// <since>7.0</since>
    public double MinimumConcaveCornerAngleRadians
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        double rc = UnsafeNativeMethods.ON_ToSubDParameters_MinimumConcaveCornerAngleRadians(const_ptr_this);
        GC.KeepAlive(this);
        return rc;
      }
      set
      {
        IntPtr ptr_this = NonConstPointer();
        UnsafeNativeMethods.ON_ToSubDParameters_SetMinimumConcaveCornerAngleRadians(ptr_this, value);
        GC.KeepAlive(this);
      }
    }

    /// <summary>
    /// If ConcaveCornerTest == ConcaveCornerOption.AtMeshCorner, then an
    /// input mesh boundary vertex becomes a SubD corner when the number of
    /// edges that end at the vertex is &gt;= MinimumConcaveCornerEdgeCount edges
    /// and the corner angle is &gt;= MinimumConcaveCornerAngleRadians.
    /// </summary>
    /// <since>7.0</since>
    [CLSCompliant(false)]
    public uint MinimumConcaveCornerEdgeCount
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        uint rc = UnsafeNativeMethods.ON_ToSubDParameters_MinimumConcaveCornerEdgeCount(const_ptr_this);
        GC.KeepAlive(this);
        return rc;
      }
      set
      {
        IntPtr ptr_this = NonConstPointer();
        UnsafeNativeMethods.ON_ToSubDParameters_SetMinimumConcaveCornerEdgeCount(ptr_this, value);
        GC.KeepAlive(this);
      }
    }

#if RHINO_SDK
    /// <summary>
    /// If false, input mesh vertex locations will be used to set SubD vertex control net locations.
    /// If true, input mesh vertex locations will be used to set SubD vertex limit surface locations.
    /// </summary>
    /// <since>7.0</since>
    public bool InterpolateMeshVertices
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        bool rc = UnsafeNativeMethods.ON_ToSubDParameters_InterpolateMeshVertices(const_ptr_this);
        GC.KeepAlive(this);
        return rc;
      }
      set
      {
        IntPtr ptr_this = NonConstPointer();
        UnsafeNativeMethods.ON_ToSubDParameters_SetInterpolateMeshVertices(ptr_this, value);
        GC.KeepAlive(this);
      }
    }
#endif

  }

  /// <summary>
  /// Options used for converting a SubD to a Brep
  /// </summary>
  public partial class SubDToBrepOptions : IDisposable
  {
    IntPtr m_ptr; // ON_SubDToBrepParameters*
    internal IntPtr ConstPointer() { return m_ptr; }
    IntPtr NonConstPointer() { return m_ptr; }

    /// <summary>
    /// Create default options
    /// </summary>
    /// <since>7.0</since>
    public SubDToBrepOptions() : this(UnsafeNativeMethods.OnSubDToBrepParameterTypeConsts.Default)
    {
    }

    SubDToBrepOptions(UnsafeNativeMethods.OnSubDToBrepParameterTypeConsts which)
    {
      m_ptr = UnsafeNativeMethods.ON_SubDToBrepParameters_New(which);
    }

    /// <summary>
    /// Create options from the given packFaces and vertexProcess values.
    /// </summary>
    /// <param name="packFaces">Sets the pack faces options.</param>
    /// <param name="vertexProcess">Sets the extraordinary vertex process option.</param>
    /// <since>7.1</since>
    public SubDToBrepOptions(bool packFaces, ExtraordinaryVertexProcessOption vertexProcess) : this(UnsafeNativeMethods.OnSubDToBrepParameterTypeConsts.Default)
    {
      PackFaces = packFaces;
      ExtraordinaryVertexProcess = vertexProcess;
    }

    /// <summary>
    /// Default SubDToBrepOptions settings.
    /// Currently selects the same options as DefaultUnpacked:
    /// Locally-G1 smoothing of extraordinary vertices, unpacked faces.
    /// </summary>
    /// <remarks>
    /// These are the settings used by ON_SubD::BrepForm()
    /// </remarks>
    /// <since>7.0</since>
    public static SubDToBrepOptions Default
    {
      get
      {
        return new SubDToBrepOptions(UnsafeNativeMethods.OnSubDToBrepParameterTypeConsts.Default);
      }
    }

    /// <summary>
    /// Default ON_SubDToBrepParameters settings for creating a packed brep.
    /// Locally-G1 smoothing of extraordinary vertices, packed faces.
    /// </summary>
    /// <since>7.0</since>
    public static SubDToBrepOptions DefaultPacked
    {
      get
      {
        return new SubDToBrepOptions(UnsafeNativeMethods.OnSubDToBrepParameterTypeConsts.DefaultPacked);
      }
    }

    /// <summary>
    /// Default ON_SubDToBrepParameters settings for creating an unpacked brep.
    /// Locally-G1 smoothing of extraordinary vertices, unpacked faces.
    /// </summary>
    /// <since>7.0</since>
    public static SubDToBrepOptions DefaultUnpacked
    {
      get
      {
        return new SubDToBrepOptions(UnsafeNativeMethods.OnSubDToBrepParameterTypeConsts.DefaultUnpacked);
      }
    }

    /// <summary>
    /// Passively reclaims unmanaged resources when the class user did not explicitly call Dispose().
    /// </summary>
    ~SubDToBrepOptions()
    {
      Dispose(false);
    }

    /// <summary>
    /// Actively reclaims unmanaged resources that this instance uses.
    /// </summary>
    /// <since>7.0</since>
    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    /// <summary>
    /// For derived class implementers.
    /// <para>This method is called with argument true when class user calls Dispose(), while with argument false when
    /// the Garbage Collector invokes the finalizer, or Finalize() method.</para>
    /// <para>You must reclaim all used unmanaged resources in both cases, and can use this chance to call Dispose on disposable fields if the argument is true.</para>
    /// <para>Also, you must call the base virtual method within your overriding method.</para>
    /// </summary>
    /// <param name="disposing">true if the call comes from the Dispose() method; false if it comes from the Garbage Collector finalizer.</param>
    protected virtual void Dispose(bool disposing)
    {
      if (IntPtr.Zero != m_ptr)
      {
        UnsafeNativeMethods.ON_SubDToBrepParameters_Delete(m_ptr);
        m_ptr = IntPtr.Zero;
      }
    }

    /// <summary>
    /// Get or sets the pack faces option.
    /// </summary>
    /// <since>7.0</since>
    public bool PackFaces
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        bool rc = UnsafeNativeMethods.ON_SubDToBrepParameters_PackFaces(const_ptr_this);
        GC.KeepAlive(this);
        return rc;
      }
      set
      {
        IntPtr ptr_this = NonConstPointer();
        UnsafeNativeMethods.ON_SubDToBrepParameters_SetPackFaces(ptr_this, value);
        GC.KeepAlive(this);
      }
    }

    /// <summary>
    /// Get or sets the extraordinary vertex process option.
    /// </summary>
    /// <since>7.0</since>
    public ExtraordinaryVertexProcessOption ExtraordinaryVertexProcess
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        ExtraordinaryVertexProcessOption rc = (ExtraordinaryVertexProcessOption)UnsafeNativeMethods.ON_SubDToBrepParameters_ExtraordinaryVertexProcess(const_ptr_this);
        GC.KeepAlive(this);
        return rc;
      }
      set
      {
        IntPtr ptr_this = NonConstPointer();
        UnsafeNativeMethods.ON_SubDToBrepParameters_SetExtraordinaryVertexProcess(ptr_this, (uint)value);
        GC.KeepAlive(this);
      }
    }
  }

  /// <summary>
  /// A part of SubD geometry. Common base class for vertices, faces, and edges
  /// </summary>
  [DebuggerDisplay("{ToString()}")]
  public abstract partial class SubDComponent : IEquatable<SubDComponent>
  {
    /// <summary>
    /// An ON_SubDComponentPtr: a component pointer, a direction bit and a component type
    /// packed into one 8 byte integer. This is not a general purpose type; it exists so
    /// that a SubDComponent can hold everything the unmanaged side needs, including the
    /// orientation an edge or face is being referenced with.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [StructLayout(LayoutKind.Sequential, Pack = 8, Size = 8)]
    internal struct SubDComponentPtr : IEquatable<SubDComponentPtr>
    {
      // Must stay one 8 byte integer: this is marshalled by value as ON_SubDComponentPtr,
      // which packs an ON_SubDVertex/Edge/Face pointer into the high 61 bits.
      private readonly ulong m_cptr;

      internal SubDComponentPtr(ulong cptr) { m_cptr = cptr; }

      internal static readonly SubDComponentPtr Null =
        new SubDComponentPtr((ulong)SubDComponentPtrTypesAndMasks.UnsetType);

      internal ulong Value { get { return m_cptr; } }

      /// <summary>The component pointer, with the type and direction bits cleared.</summary>
      internal IntPtr BasePointer
      {
        get { return (IntPtr)(long)(m_cptr & (ulong)SubDComponentPtrTypesAndMasks.PointerMask); }
      }

      /// <summary>True when the component is referenced reversed from its natural orientation.</summary>
      internal bool Direction
      {
        get { return 0 != (m_cptr & (ulong)SubDComponentPtrTypesAndMasks.DirectionMask); }
      }

      internal SubDComponentPtrTypesAndMasks Type
      {
        get { return (SubDComponentPtrTypesAndMasks)(m_cptr & (ulong)SubDComponentPtrTypesAndMasks.TypeMask); }
      }

      internal bool IsNull { get { return IntPtr.Zero == BasePointer; } }

      /// <summary>Packs a raw component pointer with a type and a direction.</summary>
      internal static SubDComponentPtr Create(IntPtr componentPointer, SubDComponentPtrTypesAndMasks type, bool direction)
      {
        ulong bits = (ulong)componentPointer.ToInt64() & (ulong)SubDComponentPtrTypesAndMasks.PointerMask;
        bits |= (ulong)type;
        if (direction)
          bits |= (ulong)SubDComponentPtrTypesAndMasks.DirectionMask;
        return new SubDComponentPtr(bits);
      }

      /// <summary>Same component and type, with the direction bit flipped.</summary>
      internal SubDComponentPtr Reversed()
      {
        return new SubDComponentPtr(m_cptr ^ (ulong)SubDComponentPtrTypesAndMasks.DirectionMask);
      }

      public bool Equals(SubDComponentPtr other) { return m_cptr == other.m_cptr; }
      public override bool Equals(object obj) { return obj is SubDComponentPtr && Equals((SubDComponentPtr)obj); }
      public override int GetHashCode() { return m_cptr.GetHashCode(); }
      public static bool operator ==(SubDComponentPtr a, SubDComponentPtr b) { return a.m_cptr == b.m_cptr; }
      public static bool operator !=(SubDComponentPtr a, SubDComponentPtr b) { return a.m_cptr != b.m_cptr; }
      public override string ToString() { return string.Format("0x{0:X16}", m_cptr); }
    }

    // The component pointer, its type and the orientation it is referenced with.
    SubDComponentPtr m_cptr;
    ulong m_subd_serial_number;

    internal SubDComponent(SubD subd, IntPtr ptr, uint id)
    {
      ParentSubD = subd;
      Id = id;
      m_subd_serial_number = subd.RuntimeSerialNumber;
      m_cptr = SubDComponentPtr.Create(ptr, ComponentPtrType, false);
    }

    internal SubDComponent(SubD subd, SubDComponentPtr cptr, uint id)
    {
      ParentSubD = subd;
      Id = id;
      m_subd_serial_number = subd.RuntimeSerialNumber;
      m_cptr = cptr;
    }

    /// <summary>
    /// The type bits this component sets in its component pointer. Every concrete
    /// SubDComponent knows what it is, so the type bits never have to be inspected to
    /// decide how to interpret the pointer.
    /// </summary>
    internal abstract SubDComponentPtrTypesAndMasks ComponentPtrType { get; }

    /// <summary>
    /// Unique id within the parent SubD for this item
    /// </summary>
    /// <since>7.0</since>
    [CLSCompliant(false)]
    public uint Id { get; }

    /// <summary>
    /// SubD that this component belongs to
    /// </summary>
    /// <since>7.0</since>
    public SubD ParentSubD { get; }

    /// <summary>
    /// True when this component is referenced with the reverse of its natural orientation.
    /// This is always false for vertices, and for edges and faces obtained directly from
    /// the parent SubD; it is only set on components reached through an oriented
    /// reference, such as the face on a given side of an oriented edge.
    /// </summary>
    /// <since>8.36</since>
    public bool ComponentDirection
    {
      get { return m_cptr.Direction; }
      set
      {
        RefreshIfStale();
        if (value != m_cptr.Direction)
          m_cptr = m_cptr.Reversed();
      }
    }

    /// <summary>
    /// Flips <see cref="ComponentDirection"/> and returns this component, so it can be used
    /// inline where an oppositely oriented reference is wanted.
    /// </summary>
    /// <returns>This component.</returns>
    /// <since>8.36</since>
    public SubDComponent ReverseComponentDirection()
    {
      ComponentDirection = !ComponentDirection;
      return this;
    }

    void RefreshIfStale()
    {
      if (m_subd_serial_number != ParentSubD.RuntimeSerialNumber)
      {
        m_subd_serial_number = ParentSubD.RuntimeSerialNumber;
        // The id survives edits to the parent SubD, the address does not. Look it up again
        // and re-pack, keeping the direction this component was referenced with.
        m_cptr = SubDComponentPtr.Create(UpdatePointer(), ComponentPtrType, m_cptr.Direction);
      }
    }

    internal IntPtr ConstPointer()
    {
      RefreshIfStale();
      return m_cptr.BasePointer;
    }

    internal IntPtr NonConstPointer()
    {
      // make sure the parent SubD is non-const
      ParentSubD.NonConstPointer();
      RefreshIfStale();
      return m_cptr.BasePointer;
    }

    internal SubDComponentPtr ConstComponentPtr()
    {
      RefreshIfStale();
      return m_cptr;
    }

    internal SubDComponentPtr NonConstComponentPtr()
    {
      // make sure the parent SubD is non-const
      ParentSubD.NonConstPointer();
      RefreshIfStale();
      return m_cptr;
    }

    internal abstract IntPtr UpdatePointer();

    /// <summary>
    /// Determines whether this component and another refer to the same component, with the
    /// same orientation, in the same SubD.
    /// </summary>
    /// <param name="other">The component to compare with.</param>
    /// <since>8.36</since>
    public bool Equals(SubDComponent other)
    {
      if (other is null)
        return false;
      if (ReferenceEquals(this, other))
        return true;
      return GetType() == other.GetType()
        && Id == other.Id
        && ComponentDirection == other.ComponentDirection
        && ReferenceEquals(ParentSubD, other.ParentSubD);
    }

    /// <summary>
    /// Determines whether an object is a SubDComponent referring to the same component,
    /// with the same orientation, in the same SubD.
    /// </summary>
    /// <param name="obj">The object to compare with.</param>
    /// <since>8.36</since>
    public override bool Equals(object obj)
    {
      return Equals(obj as SubDComponent);
    }

    /// <summary>
    /// Gets a hash code for this component.
    /// </summary>
    /// <since>8.36</since>
    public override int GetHashCode()
    {
      int hc = (int)ComponentPtrType;
      hc = hc * -1521134295 + Id.GetHashCode();
      hc = hc * -1521134295 + ComponentDirection.GetHashCode();
      hc = hc * -1521134295 + (ParentSubD is null ? 0 : ParentSubD.GetHashCode());
      return hc;
    }

    /// <summary>
    /// Determines whether two components refer to the same component, with the same
    /// orientation, in the same SubD.
    /// </summary>
    /// <param name="a">The first component.</param>
    /// <param name="b">The second component.</param>
    /// <since>8.36</since>
    public static bool operator ==(SubDComponent a, SubDComponent b)
    {
      return a is null ? b is null : a.Equals(b);
    }

    /// <summary>
    /// Determines whether two components refer to different components, orientations, or SubDs.
    /// </summary>
    /// <param name="a">The first component.</param>
    /// <param name="b">The second component.</param>
    /// <since>8.36</since>
    public static bool operator !=(SubDComponent a, SubDComponent b)
    {
      return !(a == b);
    }

    /// <summary>
    /// Returns a string of the form "SubDEdge(+12)", naming the component type, the
    /// orientation it is referenced with, and its id.
    /// </summary>
    /// <since>8.36</since>
    public override string ToString()
    {
      string type;
      switch (ComponentPtrType)
      {
        case SubDComponentPtrTypesAndMasks.VertexType: type = "SubDVertex"; break;
        case SubDComponentPtrTypesAndMasks.EdgeType: type = "SubDEdge"; break;
        case SubDComponentPtrTypesAndMasks.FaceType: type = "SubDFace"; break;
        default: type = "SubDComponent"; break;
      }
      return string.Format("{0}({1}{2})", type, ComponentDirection ? "-" : "+", Id);
    }

    const int idx_cs_selected = 0;
    const int idx_cs_highlighted = 1;
    const int idx_cs_hidden = 2;
    const int idx_cs_locked = 3;
    const int idx_cs_deleted = 4;
    const int idx_cs_damaged = 5;

    internal bool GetComponentStatusBool(int which)
    {
      var const_ptr_this = ConstPointer();
      bool rc = UnsafeNativeMethods.ON_SubD_ComponentStatusBool(const_ptr_this, which);
      GC.KeepAlive(this);
      return rc;
    }

    /// <summary>
    /// Returns true if the SubD component is selected.
    /// </summary>
    /// <since>7.6</since>
    public bool IsSelected
    {
      get { return GetComponentStatusBool(idx_cs_selected); }
    }

    /// <summary>
    /// Returns true if the SubD component is highlighted.
    /// </summary>
    /// <since>7.6</since>
    public bool IsHighlighted
    {
      get { return GetComponentStatusBool(idx_cs_highlighted); }
    }

    /// <summary>
    /// Returns true if the SubD component is hidden.
    /// </summary>
    /// <since>7.6</since>
    public bool IsHidden
    {
      get { return GetComponentStatusBool(idx_cs_hidden); }
    }

    /// <summary>
    /// Returns true if the SubD component is locked.
    /// </summary>
    /// <since>7.6</since>
    public bool IsLocked
    {
      get { return GetComponentStatusBool(idx_cs_locked); }
    }

    /// <summary>
    /// Returns true if the SubD component is deleted.
    /// </summary>
    /// <since>7.6</since>
    public bool IsDeleted
    {
      get { return GetComponentStatusBool(idx_cs_deleted); }
    }

    /// <summary>
    /// Returns true if the SubD component is damaged.
    /// </summary>
    /// <since>7.6</since>
    public bool IsDamaged
    {
      get { return GetComponentStatusBool(idx_cs_damaged); }
    }

  }


  /// <summary> Single face of a SubD </summary>
  public sealed class SubDFace : SubDComponent
  {
    internal SubDFace(SubD subd, IntPtr pointer, uint id) : base(subd, pointer, id)
    {
    }

    internal SubDFace(SubD subd, SubDComponentPtr cptr, uint id) : base(subd, cptr, id)
    {
    }

    internal override IntPtr UpdatePointer()
    {
      IntPtr const_ptr_subd = ParentSubD.ConstPointer();
      return UnsafeNativeMethods.ON_SubD_FaceFromId(const_ptr_subd, Id);
    }

    internal override SubDComponentPtrTypesAndMasks ComponentPtrType
    {
      get { return SubDComponentPtrTypesAndMasks.FaceType; }
    }

    /// <summary>
    /// Discards the cached subdivision and surface points for this face, so they are
    /// recomputed on next use.
    /// </summary>
    /// <param name="clearNeighborhood">
    /// If true, the cached points of the neighboring components are discarded too. Moving a
    /// control net point changes the surface around it, not just at it, so pass true after
    /// an edit unless you know only this face is affected.
    /// </param>
    /// <since>8.36</since>
    [ConstOperation]
    public void ClearSavedSubdivisionPoints(bool clearNeighborhood)
    {
      var const_ptr_this = ConstPointer();
      UnsafeNativeMethods.ON_SubDFace_ClearSavedSubdivisionPoints(const_ptr_this, clearNeighborhood);
      GC.KeepAlive(this);
    }

    #region properties
    /// <summary>
    /// Number of edges for this face. Note that EdgeCount is always the same
    /// as VertexCount. Two properties are provided simply for clarity.
    /// </summary>
    /// <since>7.0</since>
    public int EdgeCount
    {
      get
      {
        var const_ptr_this = ConstPointer();
        int rc = UnsafeNativeMethods.ON_SubDFace_EdgeCount(const_ptr_this);
        GC.KeepAlive(this);
        return rc;
      }
    }

    /// <summary>
    /// Number of vertices for this face. Note that EdgeCount is always the same
    /// as VertexCount. Two properties are provided simply for clarity.
    /// </summary>
    /// <since>7.0</since>
    public int VertexCount
    {
      get { return EdgeCount; }
    }

    /// <summary>
    /// If per-face color is "Empty", then this face does not have a custom color
    /// </summary>
    /// <since>7.0</since>
    public System.Drawing.Color PerFaceColor
    {
      get
      {
        IntPtr const_face_ptr = ConstPointer();
        int argb = 0;
        if (!UnsafeNativeMethods.ON_SubDFace_GetPerFaceColor(const_face_ptr, ref argb))
          return System.Drawing.Color.Empty;
        GC.KeepAlive(this);
        return System.Drawing.Color.FromArgb(argb);
      }
      set
      {
        IntPtr ptr_face = NonConstPointer();
        int argb = value.ToArgb();
        UnsafeNativeMethods.ON_SubDFace_SetPerFaceColor(ptr_face, argb);
        GC.KeepAlive(this);
      }
    }

    /// <summary>
    /// Gets the component index of this face.
    /// </summary>
    /// <returns>The component index.</returns>
    /// <since>7.9</since>
    [ConstOperation]
    public ComponentIndex ComponentIndex()
    {
      ComponentIndex ci = new ComponentIndex();
      IntPtr const_face_ptr = ConstPointer();
      UnsafeNativeMethods.ON_SubDFace_ComponentIndex(const_face_ptr, ref ci);
      GC.KeepAlive(this);
      return ci;
    }


#if RHINO_SDK
    /// <summary>
    /// Get the limit surface point location at the center of the face
    /// </summary>
    /// <since>7.0</since>
    public Point3d LimitSurfaceCenterPoint
    {
      get
      {
        Point3d res = Point3d.Unset;
        var const_face_ptr = ConstPointer();
        UnsafeNativeMethods.ON_SubDFace_LimitSurfaceCenterPoint(const_face_ptr, ref res);
        GC.KeepAlive(this);
        return res;
      }
    }

    /// <summary>
    /// The face's control net center point is the average of the face's
    /// vertex control net points. This is the same point as the face's
    /// subdivision point.
    /// </summary>
    /// <returns>
    /// The average of the face's vertex control net points
    /// </returns>
    /// <since>8.0</since>
    public Point3d ControlNetCenterPoint
    {
      get
      {
        Point3d res = Point3d.Unset;
        var const_face_ptr = ConstPointer();
        UnsafeNativeMethods.ON_SubDFace_ControlNetCenterPoint(const_face_ptr, ref res);
        GC.KeepAlive(this);
        return res;
      }
    }

    /// <summary>
    /// Get the limit surface normal vector at the center of the face.
    /// </summary>
    /// <returns>
    /// Limit surface normal vector at the face's center. 
    /// </returns>
    /// <since>8.0</since>
    public Vector3d SurfaceCenterNormal
    {
      get
      {
        Vector3d res = Vector3d.Unset;
        var const_face_ptr = ConstPointer();
        UnsafeNativeMethods.ON_SubDFace_SurfaceCenterNormal(const_face_ptr, ref res);
        GC.KeepAlive(this);
        return res;
      }
    }

    /// <summary>
    /// When the face's control net polygon is planar, the face's
    /// control net normal is a unit vector perpendicular to the plane
    /// that points outwards. If the control net polygon is not
    /// planar, the control net normal is control net normal is a unit
    /// vector that is the average of the control polygon's corner normals.
    /// </summary>
    /// <returns>
    /// A unit vector that is normal to planar control net polygons and a good
    /// compromise for nonplanar control net polygons.
    /// </returns> 
    /// <since>8.0</since>
    public Vector3d ControlNetCenterNormal
    {
      get
      {
        Vector3d res = Vector3d.Unset;
        var const_face_ptr = ConstPointer();
        UnsafeNativeMethods.ON_SubDFace_ControlNetCenterNormal(const_face_ptr, ref res);
        GC.KeepAlive(this);
        return res;
      }
    }

    /// <summary>
    /// Get the limit surface tangent plane at the center of the face. 
    /// The plane's origin is the point on the limit surface at the center of the face.
    /// The plane's z axis is the limit surface normal vector at the center of the face.
    /// </summary>
    /// <returns>
    /// Limit surface tanget plane at the face's center. 
    /// </returns>
    /// <since>8.0</since>
    public Plane SurfaceCenterFrame
    {
      get
      {
        Plane plane = Plane.Unset;
        IntPtr const_face_ptr = ConstPointer();
        UnsafeNativeMethods.ON_SubDFace_SurfaceCenterFrame(const_face_ptr, ref plane);
        GC.KeepAlive(this);
        return plane;
      }
    }

    /// <summary>
    /// The face's control net center frame is a plane 
    /// with normal equal to ControlNetCenterNormal
    /// and origin equal to ControlNetCenterPoint. 
    /// The x and y axes of the frame have no predictable relationship 
    /// to the face or SubD control net topology.
    /// </summary>
    /// <returns>
    /// A plane with unit normal equal to ControlNetCenterNormal
    /// and origin equal to ControlNetCenterPoint.
    /// </returns> 
    /// <since>8.0</since>
    public Plane ControlNetCenterFrame
    {
      get
      {
        Plane plane = Plane.Unset;
        IntPtr const_face_ptr = ConstPointer();
        UnsafeNativeMethods.ON_SubDFace_ControlNetCenterFrame(const_face_ptr, ref plane);
        GC.KeepAlive(this);
        return plane;
      }
    }

#endif
    #endregion

    /// <summary>
    /// Get an edge at a given index
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    /// <since>7.0</since>
    public SubDEdge EdgeAt(int index)
    {
      IntPtr const_ptr_this = ConstPointer();
      uint edgeId = 0;
      IntPtr edgePtr = UnsafeNativeMethods.ON_SubDFace_EdgeAt(const_ptr_this, (uint)index, ref edgeId);
      GC.KeepAlive(this);
      if (edgePtr != IntPtr.Zero)
        return new SubDEdge(ParentSubD, edgePtr, edgeId);

      if (index < 0 || index > EdgeCount)
        throw new IndexOutOfRangeException("index");

      throw new InvalidOperationException("SubDFace.EdgeAt call failed unexpectedly.");
    }

    /// <summary>
    /// Check if a given edge in this face has the same direction as the face orientation
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    /// <since>7.0</since>
    public bool EdgeDirectionMatchesFaceOrientation(int index)
    {
      IntPtr const_ptr_this = ConstPointer();
      bool rc = UnsafeNativeMethods.ON_SubDFace_EdgeDirectionMatches(const_ptr_this, (uint)index);
      GC.KeepAlive(this);
      return rc;
    }

    /// <summary>
    /// Get a vertex that this face uses by index
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    /// <since>7.0</since>
    public SubDVertex VertexAt(int index)
    {
      var const_ptr_this = ConstPointer();
      uint componentId = 0;
      IntPtr ptr_vertex = UnsafeNativeMethods.ON_SubDFace_VertexAt(const_ptr_this, (uint)index, ref componentId);
      GC.KeepAlive(this);
      if (ptr_vertex != IntPtr.Zero)
        return new SubDVertex(ParentSubD, ptr_vertex, componentId);

      if (index < 0 || index > EdgeCount)
        throw new IndexOutOfRangeException("index");

      throw new InvalidOperationException("SubDFace.VertexAt call failed unexpectedly.");
    }

  }

  /// <summary> Single vertex of a SubD </summary>
  public sealed class SubDVertex : SubDComponent
  {
    internal SubDVertex(SubD subd, IntPtr pointer, uint id): base(subd, pointer, id)
    {
    }

    internal SubDVertex(SubD subd, SubDComponentPtr cptr, uint id) : base(subd, cptr, id)
    {
    }

    internal override IntPtr UpdatePointer()
    {
      IntPtr const_ptr_subd = ParentSubD.ConstPointer();
      return UnsafeNativeMethods.ON_SubD_VertexFromId(const_ptr_subd, Id);
    }

    internal override SubDComponentPtrTypesAndMasks ComponentPtrType
    {
      get { return SubDComponentPtrTypesAndMasks.VertexType; }
    }

    /// <summary>
    /// Discards the cached subdivision and surface points for this vertex, so they are
    /// recomputed on next use.
    /// </summary>
    /// <param name="clearNeighborhood">
    /// If true, the cached points of the neighboring components are discarded too. Moving a
    /// control net point changes the surface around it, not just at it, so pass true after
    /// an edit unless you know only this vertex is affected.
    /// </param>
    /// <since>8.36</since>
    [ConstOperation]
    public void ClearSavedSubdivisionPoints(bool clearNeighborhood)
    {
      var const_ptr_this = ConstPointer();
      UnsafeNativeMethods.ON_SubDVertex_ClearSavedSubdivisionPoints(const_ptr_this, clearNeighborhood);
      GC.KeepAlive(this);
    }

    #region properties
    /// <summary>
    /// Location of the "control net" point that this SubDVertex represents
    /// </summary>
    /// <remarks>
    /// The setter of this property will refresh the neighborhood cache around the vertex 
    /// everytime it is called. This is not efficient if you have multiple vertices to
    /// modify. In that case, call vertex.SetControlNetPoint(position, false) for all the
    /// vertices you want to modify, then call subd.ClearEvaluationCache()
    /// </remarks>
    /// <since>7.0</since>
    public Point3d ControlNetPoint
    {
      get
      {
        IntPtr const_vertex_ptr = ConstPointer();
        Point3d rc = default(Point3d);
        UnsafeNativeMethods.ON_SubDVertex_ControlNetPoint(const_vertex_ptr, ref rc);
        GC.KeepAlive(this);
        return rc;
      }
      set
      {
        IntPtr ptr_vertex = NonConstPointer();
        UnsafeNativeMethods.ON_SubDVertex_SetControlNetPoint(ptr_vertex, value);
        GC.KeepAlive(this);
      }
    }

    /// <summary> Number of edges for this vertex </summary>
    /// <since>7.0</since>
    public int EdgeCount
    {
      get
      {
        var const_ptr_this = ConstPointer();
        int rc = UnsafeNativeMethods.ON_SubDVertex_EdgeCount(const_ptr_this);
        GC.KeepAlive(this);
        return rc;
      }
    }
    /// <summary> Number of faces for this vertex </summary>
    /// <since>7.0</since>
    public int FaceCount
    {
      get
      {
        var const_ptr_this = ConstPointer();
        int rc = UnsafeNativeMethods.ON_SubDVertex_FaceCount(const_ptr_this);
        GC.KeepAlive(this);
        return rc;
      }
    }

    /// <summary>
    /// Gets the component index of this vertex.
    /// </summary>
    /// <returns>The component index.</returns>
    /// <since>8.30</since>
    [ConstOperation]
    public ComponentIndex ComponentIndex()
    {
      ComponentIndex ci = new ComponentIndex();
      IntPtr const_vertex_ptr = ConstPointer();
      UnsafeNativeMethods.ON_SubDVertex_ComponentIndex(const_vertex_ptr, ref ci);
      GC.KeepAlive(this);
      return ci;
    }

    /// <summary>
    /// Next vertex in linked list of vertices on this level
    /// </summary>
    /// <since>7.0</since>
    public SubDVertex Next
    {
      get
      {
        IntPtr const_ptr_vertex = ConstPointer();
        uint id = 0;
        IntPtr const_ptr_next = UnsafeNativeMethods.ON_SubDVertex_PreviousOrNext(const_ptr_vertex, true, ref id);
        if (const_ptr_next != IntPtr.Zero)
          return new SubDVertex(ParentSubD, const_ptr_next, id);
        GC.KeepAlive(this);
        return null;
      }
    }

    /// <summary>
    /// Previous vertex in linked list of vertices on this level
    /// </summary>
    /// <since>7.0</since>
    public SubDVertex Previous
    {
      get
      {
        IntPtr const_ptr_vertex = ConstPointer();
        uint id = 0;
        IntPtr const_ptr_next = UnsafeNativeMethods.ON_SubDVertex_PreviousOrNext(const_ptr_vertex, false, ref id);
        if (const_ptr_next != IntPtr.Zero)
          return new SubDVertex(ParentSubD, const_ptr_next, id);
        GC.KeepAlive(this);
        return null;
      }
    }

    /// <summary>
    /// identifies the type of subdivision vertex
    /// </summary>
    /// <since>7.5</since>
    public SubDVertexTag Tag
    {
      get
      {
        var const_ptr_vertex = ConstPointer();
        SubDVertexTag rc = UnsafeNativeMethods.ON_SubDVertex_GetVertexTag(const_ptr_vertex);
        GC.KeepAlive(this);
        return rc;
      }
      set
      {
        // 2026-02-13, Pierre, RH-92469
        // Much safer to set the tags this way so neighboring tags are checked and updated too
        var ptr_subd = ParentSubD.NonConstPointer();
        using (var ciArray = new INTERNAL_ComponentIndexArray())
        {
          ciArray.Add(ComponentIndex());
          IntPtr pCiArray = ciArray.NonConstPointer();
          UnsafeNativeMethods.ON_SubD_SetVertexTags(ptr_subd, value, pCiArray);
          GC.KeepAlive(this);
        }
        // var ptr_vertex = NonConstPointer();
        // UnsafeNativeMethods.ON_SubDVertex_SetVertexTag(ptr_vertex, value);
        // GC.KeepAlive(this);
      }
    }
    #endregion


    /// <summary>
    /// Retrieve a SubDEdge from this vertex
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    /// <since>7.0</since>
    public SubDEdge EdgeAt(int index)
    {
      if (index < 0)
        throw new IndexOutOfRangeException("index cannot be negative.");

      IntPtr const_ptr_this = ConstPointer();
      uint edgeId = 0;
      IntPtr const_ptr_edge = UnsafeNativeMethods.ON_SubDVertex_EdgeAt(const_ptr_this, (uint)index, ref edgeId);
      if (const_ptr_edge != IntPtr.Zero)
        return new SubDEdge(ParentSubD, const_ptr_edge, edgeId);
      GC.KeepAlive(this);

      // failure if we hit this line
      if (index >= EdgeCount) throw new
        IndexOutOfRangeException("index is greater than or equal to EdgeCount");

      throw new NotSupportedException("Edge retrieval failed. This is a RhinoCommon library error.");
    }

    /// <summary>
    /// Retrieve a SubDFace from this vertex
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    /// <since>7.7</since>
    public SubDFace FaceAt(int index)
    {
      if (index < 0)
        throw new IndexOutOfRangeException("index cannot be negative.");

      IntPtr const_ptr_this = ConstPointer();
      uint faceId = 0;
      IntPtr const_ptr_face = UnsafeNativeMethods.ON_SubDVertex_FaceAt(const_ptr_this, (uint)index, ref faceId);
      if (const_ptr_face != IntPtr.Zero)
        return new SubDFace(ParentSubD, const_ptr_face, faceId);
      GC.KeepAlive(this);

      // failure if we hit this line
      if (index >= FaceCount) throw new
        IndexOutOfRangeException("index is greater than or equal to FaceCount");

      throw new NotSupportedException("Face retrieval failed. This is a RhinoCommon library error.");
    }

    /// <summary>
    /// All edges that this vertex is part of
    /// </summary>
    /// <since>7.0</since>
    public IEnumerable<SubDEdge> Edges
    {
      get
      {
        int count = EdgeCount;
        for( int i=0; i<count; i++ )
        {
          yield return EdgeAt(i);
        }
      }
    }

    /// <summary>
    /// The SubD surface point
    /// </summary>
    /// <returns></returns>
    /// <seealso cref="SubD.SetVertexSurfacePoint(uint, Point3d)"/>
    /// <seealso cref="SubD.InterpolateSurfacePoints(Point3d[])"/>
    /// <seealso cref="SubD.InterpolateSurfacePoints(uint[], Point3d[])"/>
    /// <seealso cref="SubDSurfaceInterpolator"/>
    /// <seealso cref="SubD.UpdateSurfaceMeshCache(bool)"/>
    /// <since>7.1</since>
    public Point3d SurfacePoint()
    {
      IntPtr const_vertex_ptr = ConstPointer();
      Point3d rc = default(Point3d);
      UnsafeNativeMethods.ON_SubDVertex_SurfacePoint(const_vertex_ptr, ref rc);
      GC.KeepAlive(this);
      return rc;
    }


    /// <summary>
    /// Change the location of the "control net" point that this SubDVertex represents
    /// </summary>
    /// <param name="position">
    /// New position for the vertex' control net point.
    /// </param>
    /// <param name="bClearNeighborhoodCache">
    /// If true, clear the evaluation cache in the faces around the modified vertex.
    /// </param>
    /// <returns>
    /// true if the vertex' control net point was modified.
    /// </returns>
    /// <remarks>
    /// This method is provided to be able to set multiple control vertices, without clearing
    /// the neighborhood cache everytime as the ControlNetPoint property setter does. When you
    /// are done modifying your SubD, call subd.UpdateEvaluationCache() to refresh
    /// all caches.
    /// </remarks>
    /// <since>8.0</since>
    public bool SetControlNetPoint(Point3d position, bool bClearNeighborhoodCache)
    {
      if (ControlNetPoint != position)
      {
        IntPtr ptr_vertex = NonConstPointer();
        UnsafeNativeMethods.ON_SubDVertex_SetControlNetPoint_ClearCache(ptr_vertex, position, bClearNeighborhoodCache);
        GC.KeepAlive(this);
        return true;
      }
      else
      {
        return false;
      }
    }
  }

  /// <summary> Single edge of a SubD </summary>
  public sealed class SubDEdge : SubDComponent
  {
    internal SubDEdge(SubD subd, IntPtr pointer, uint id) : base(subd, pointer, id)
    {
    }

    internal SubDEdge(SubD subd, SubDComponentPtr cptr, uint id) : base(subd, cptr, id)
    {
    }

    internal override IntPtr UpdatePointer()
    {
      IntPtr const_ptr_subd = ParentSubD.ConstPointer();
      return UnsafeNativeMethods.ON_SubD_EdgeFromId(const_ptr_subd, Id);
    }

    internal override SubDComponentPtrTypesAndMasks ComponentPtrType
    {
      get { return SubDComponentPtrTypesAndMasks.EdgeType; }
    }

    /// <summary>
    /// Discards the cached subdivision and surface points for this edge, so they are
    /// recomputed on next use.
    /// </summary>
    /// <param name="clearNeighborhood">
    /// If true, the cached points of the neighboring components are discarded too. Moving a
    /// control net point changes the surface around it, not just at it, so pass true after
    /// an edit unless you know only this edge is affected.
    /// </param>
    /// <since>8.36</since>
    [ConstOperation]
    public void ClearSavedSubdivisionPoints(bool clearNeighborhood)
    {
      var const_ptr_this = ConstPointer();
      UnsafeNativeMethods.ON_SubDEdge_ClearSavedSubdivisionPoints(const_ptr_this, clearNeighborhood);
      GC.KeepAlive(this);
    }

    #region properties
    /// <summary>Number of faces for this edge.</summary>
    /// <since>7.0</since>
    public int FaceCount
    {
      get
      {
        var const_ptr_this = ConstPointer();
        int rc = UnsafeNativeMethods.ON_SubDEdge_FaceCount(const_ptr_this);
        GC.KeepAlive(this);
        return rc;
      }
    }

    /// <summary>
    /// Gets the component index of this edge.
    /// </summary>
    /// <returns>The component index.</returns>
    /// <since>8.12</since>
    [ConstOperation]
    public ComponentIndex ComponentIndex()
    {
      ComponentIndex ci = new ComponentIndex();
      IntPtr const_edge_ptr = ConstPointer();
      UnsafeNativeMethods.ON_SubDEdge_ComponentIndex(const_edge_ptr, ref ci);
      GC.KeepAlive(this);
      return ci;
    }

    /// <summary>
    /// Line representing the control net end points.
    /// </summary>
    /// <since>7.0</since>
    public Line ControlNetLine
    {
      get
      {
        return new Line(VertexFrom.ControlNetPoint, VertexTo.ControlNetPoint);
      }
    }

    /// <summary>
    /// Start vertex for this edge.
    /// </summary>
    /// <since>7.0</since>
    public SubDVertex VertexFrom
    {
      get
      {
        IntPtr const_pointer_edge = ConstPointer();
        uint id = 0;
        IntPtr vertex_ptr = UnsafeNativeMethods.ON_SubDEdge_GetVertex(const_pointer_edge, true, ref id);
        if(vertex_ptr != IntPtr.Zero )
          return new SubDVertex(ParentSubD, vertex_ptr, id);
        GC.KeepAlive(this);
        return null;
      }
    }

    /// <summary>
    /// End vertex for this edge.
    /// </summary>
    /// <since>7.0</since>
    public SubDVertex VertexTo
    {
      get
      {
        IntPtr const_pointer_edge = ConstPointer();
        uint id = 0;
        IntPtr vertex_ptr = UnsafeNativeMethods.ON_SubDEdge_GetVertex(const_pointer_edge, false, ref id);
        if (vertex_ptr != IntPtr.Zero)
          return new SubDVertex(ParentSubD, vertex_ptr, id);
        GC.KeepAlive(this);
        return null;
      }
    }

    /// <summary>
    /// identifies the type of subdivision edge.
    /// </summary>
    /// <since>7.0</since>
    public SubDEdgeTag Tag
    {
      get
      {
        var const_ptr_edge = ConstPointer();
        SubDEdgeTag rc = UnsafeNativeMethods.ON_SubDEdge_GetEdgeTag(const_ptr_edge);
        GC.KeepAlive(this);
        return rc;
      }
      set
      {
        // 2026-02-13, Pierre, RH-92469
        // Much safer to set the tags this way so neighboring tags are checked and updated too
        var ptr_subd = ParentSubD.NonConstPointer();
        using (var ciArray = new INTERNAL_ComponentIndexArray())
        {
          ciArray.Add(ComponentIndex());
          IntPtr pCiArray = ciArray.NonConstPointer();
          UnsafeNativeMethods.ON_SubD_SetEdgeTags(ptr_subd, value, pCiArray);
          GC.KeepAlive(this);
        }
        // var ptr_edge = NonConstPointer();
        // UnsafeNativeMethods.ON_SubDEdge_SetEdgeTag(ptr_edge, value);
        // GC.KeepAlive(this);
      }
    }

    /// <summary>
    /// Gets or sets the sharpness of this edge.
    /// <para>
    /// The getter reports <see cref="SubDEdgeSharpness.Crease"/> for a crease edge; use
    /// <see cref="GetSharpness(bool)">GetSharpness(false)</see> to get
    /// <see cref="SubDEdgeSharpness.Smooth"/> for creases instead.
    /// </para>
    /// </summary>
    /// <remarks>
    /// Setting this goes through the parent SubD so that the neighboring edges and
    /// vertices are updated too. Assign <see cref="SubDEdgeSharpness.Smooth"/> to make the
    /// edge smooth again.
    /// </remarks>
    /// <since>8.36</since>
    public SubDEdgeSharpness Sharpness
    {
      get { return GetSharpness(true); }
#if RHINO_SDK
      set { ParentSubD.SetEdgeSharpness(new SubDEdge[] { this }, value, false); }
#endif
    }

    /// <summary>
    /// Gets true if this edge is smooth and has a nonzero sharpness.
    /// A crease edge is not a sharp edge.
    /// </summary>
    /// <since>8.36</since>
    public bool IsSharp
    {
      get
      {
        var const_ptr_this = ConstPointer();
        bool rc = UnsafeNativeMethods.ON_SubDEdge_IsSharp(const_ptr_this);
        GC.KeepAlive(this);
        return rc;
      }
    }

#endregion

    /// <summary>
    /// The vertex this edge starts at, taking <see cref="SubDComponent.ComponentDirection"/>
    /// into account. This is <see cref="VertexFrom"/> for an edge in its natural
    /// orientation and <see cref="VertexTo"/> for a reversed one.
    /// </summary>
    /// <since>8.36</since>
    public SubDVertex RelativeVertexFrom
    {
      get { return RelativeVertexAt(0); }
    }

    /// <summary>
    /// The vertex this edge ends at, taking <see cref="SubDComponent.ComponentDirection"/>
    /// into account.
    /// </summary>
    /// <since>8.36</since>
    public SubDVertex RelativeVertexTo
    {
      get { return RelativeVertexAt(1); }
    }

    /// <summary>
    /// Gets one of this edge's two vertices, relative to the direction this edge is
    /// referenced with.
    /// </summary>
    /// <param name="endIndex">0 for the start of the edge, 1 for the end.</param>
    /// <returns>The vertex, or null if endIndex is out of range.</returns>
    /// <since>8.36</since>
    [ConstOperation]
    public SubDVertex RelativeVertexAt(int endIndex)
    {
      if (endIndex < 0 || endIndex > 1)
        throw new ArgumentOutOfRangeException(nameof(endIndex), "endIndex must be 0 or 1.");

      uint id = 0;
      IntPtr ptr_vertex = UnsafeNativeMethods.ON_SubDEdgePtr_RelativeVertex(ConstComponentPtr(), endIndex, ref id);
      GC.KeepAlive(this);
      if (IntPtr.Zero == ptr_vertex)
        return null;
      return new SubDVertex(ParentSubD, ptr_vertex, id);
    }

    /// <summary>
    /// The face on the left of this edge, with respect to the direction this edge is
    /// referenced with. Null for a boundary edge with no face on that side, and for a
    /// nonmanifold edge.
    /// </summary>
    /// <since>8.36</since>
    public SubDFace RelativeFaceLeft
    {
      get { return RelativeFaceAt(0); }
    }

    /// <summary>
    /// The face on the right of this edge, with respect to the direction this edge is
    /// referenced with.
    /// </summary>
    /// <since>8.36</since>
    public SubDFace RelativeFaceRight
    {
      get { return RelativeFaceAt(1); }
    }

    /// <summary>
    /// Gets the face on one side of this edge, relative to the direction this edge is
    /// referenced with.
    /// </summary>
    /// <param name="relativeFaceIndex">0 for the left side, 1 for the right side.</param>
    /// <returns>
    /// The face, with its <see cref="SubDComponent.ComponentDirection"/> set so its boundary
    /// runs the same way as this edge. Null if there is no such face, or if the edge is
    /// nonmanifold.
    /// </returns>
    /// <since>8.36</since>
    [ConstOperation]
    public SubDFace RelativeFaceAt(int relativeFaceIndex)
    {
      if (relativeFaceIndex < 0 || relativeFaceIndex > 1)
        throw new ArgumentOutOfRangeException(nameof(relativeFaceIndex), "relativeFaceIndex must be 0 or 1.");

      uint id = 0;
      var cptr = UnsafeNativeMethods.ON_SubDEdgePtr_RelativeFacePtr(ConstComponentPtr(), relativeFaceIndex, ref id);
      GC.KeepAlive(this);
      if (0 == id)
        return null;
      return new SubDFace(ParentSubD, cptr, id);
    }

    /// <summary>
    /// Gets the sharpness of this edge, reporting
    /// <see cref="SubDEdgeSharpness.Crease"/> for a crease edge.
    /// </summary>
    /// <returns>The sharpness of this edge.</returns>
    /// <since>8.36</since>
    [ConstOperation]
    public SubDEdgeSharpness GetSharpness()
    {
      return GetSharpness(true);
    }

    /// <summary>
    /// Gets the sharpness of this edge.
    /// </summary>
    /// <param name="useCreaseSharpness">
    /// If this edge is a crease and useCreaseSharpness is true, then
    /// <see cref="SubDEdgeSharpness.Crease"/> is returned. If it is a crease and
    /// useCreaseSharpness is false, then <see cref="SubDEdgeSharpness.Smooth"/> is
    /// returned.
    /// </param>
    /// <returns>
    /// The sharpness of a smooth edge, <see cref="SubDEdgeSharpness.Crease"/> for a crease
    /// edge when useCreaseSharpness is true, and <see cref="SubDEdgeSharpness.Smooth"/> in
    /// every other case.
    /// </returns>
    /// <since>8.36</since>
    [ConstOperation]
    public SubDEdgeSharpness GetSharpness(bool useCreaseSharpness)
    {
      SubDEdgeSharpness rc = default(SubDEdgeSharpness);
      var const_ptr_this = ConstPointer();
      UnsafeNativeMethods.ON_SubDEdge_GetSharpness(const_ptr_this, useCreaseSharpness, ref rc);
      GC.KeepAlive(this);
      return rc;
    }

    /// <summary>
    /// Gets the sharpness of this edge at one of its ends.
    /// </summary>
    /// <param name="endIndex">
    /// 0 for the <see cref="VertexFrom"/> end, 1 for the <see cref="VertexTo"/> end.
    /// </param>
    /// <param name="useCreaseSharpness">
    /// If this edge is a crease and useCreaseSharpness is true, then
    /// <see cref="SubDEdgeSharpness.CreaseValue"/> is returned.
    /// </param>
    /// <returns>The sharpness at that end, or 0.0 if this edge is not sharp.</returns>
    /// <since>8.36</since>
    [ConstOperation]
    public double EndSharpness(int endIndex, bool useCreaseSharpness)
    {
      if (endIndex < 0 || endIndex > 1)
        throw new ArgumentOutOfRangeException(nameof(endIndex), "endIndex must be 0 or 1.");

      var const_ptr_this = ConstPointer();
      double rc = UnsafeNativeMethods.ON_SubDEdge_EndSharpness(const_ptr_this, (uint)endIndex, useCreaseSharpness);
      GC.KeepAlive(this);
      return rc;
    }

    /// <summary>
    /// Retrieve a SubDFace from this edge.
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    /// <since>7.0</since>
    public SubDFace FaceAt(int index)
    {
      if (index < 0)
        throw new IndexOutOfRangeException("index cannot be negative.");

      IntPtr const_ptr_this = ConstPointer();
      uint faceId = 0;
      IntPtr const_ptr_face = UnsafeNativeMethods.ON_SubDEdge_FaceAt(const_ptr_this, (uint)index, ref faceId);
      if (const_ptr_face != IntPtr.Zero)
        return new SubDFace(ParentSubD, const_ptr_face, faceId);
      GC.KeepAlive(this);

      // failure if we hit this line
      if (index >= FaceCount) throw new
        IndexOutOfRangeException("index is greater than or equal to FaceCount");

      throw new NotSupportedException("Face retrieval failed. This is a RhinoCommon library error.");
    }

#if RHINO_SDK
    /// <summary>
    /// Get a cubic, uniform, non-rational, NURBS curve that is on the
    /// edge's limit curve.
    /// </summary>
    /// <remarks>
    /// If some edges in your SubD are not able to be converted to NURBS,
    /// you might need to run <see cref="SubD.UpdateSurfaceMeshCache(bool)"/>.
    /// </remarks>
    /// <param name="clampEnds">
    /// If true, the end knots are clamped.
    /// Otherwise the end knots are(-2,-1,0,...., k1, k1+1, k1+2).
    /// </param>
    /// <returns>The Nurbs form of this edge.</returns>
    /// <seealso cref="SubD.UpdateSurfaceMeshCache(bool)"/>
    /// <since>7.0</since>
    public NurbsCurve ToNurbsCurve(bool clampEnds)
    {
      IntPtr const_ptr_this = ConstPointer();
      IntPtr ptr_nurbscurve = UnsafeNativeMethods.ON_SubDEdge_LimitCurve(const_ptr_this, clampEnds);
      GC.KeepAlive(this);
      return GeometryBase.CreateGeometryHelper(ptr_nurbscurve, null) as NurbsCurve;
    }
#endif

  }

  /// <summary>
  /// Sharpness values for the two ends of a SubD edge.
  /// <para>
  /// A sharp SubD edge is an edge where the limit surface makes a tighter "fillet radius"
  /// around the edge than a smooth edge would, while still staying smooth, unlike a crease
  /// edge. It is achieved by applying the crease edge subdivision rules for the number of
  /// subdivisions given by the sharpness value, and the smooth edge subdivision rules after
  /// that. Sharpness can differ at the two ends of an edge.
  /// </para>
  /// </summary>
  /// <remarks>
  /// This is a value type wrapping two floats, and the default value is
  /// <see cref="Smooth"/>. It is passed to and from the unmanaged library by value.
  /// </remarks>
  /// <seealso cref="SubDEdge.Sharpness"/>
  [StructLayout(LayoutKind.Sequential, Pack = 4, Size = 8)]
  [DebuggerDisplay("{m_sharpness0}, {m_sharpness1}")]
  public struct SubDEdgeSharpness : IEquatable<SubDEdgeSharpness>
  {
    #region Members
    // These must stay two floats in this order: the unmanaged side marshals this struct
    // by value as ON_SUBD_EDGE_SHARPNESS_STRUCT, which mirrors ON_SubDEdgeSharpness.
    private float m_sharpness0;
    private float m_sharpness1;
    #endregion

    #region Constants
    // These mirror the ON_SubDEdgeSharpness statics. The native values are pinned by the
    // SubDSharpness.StaticValues test in src4/rhino4/tests/subd/rhtest_subd_sharpness.cpp.

    /// <summary>
    /// The largest valid sharpness value, 4.0.
    /// Valid SubD edge sharpness values are between 0.0 and MaximumValue.
    /// </summary>
    /// <since>8.36</since>
    public const double MaximumValue = 4.0;

    /// <summary>
    /// The sharpness value of a smooth edge, 0.0.
    /// </summary>
    /// <since>8.36</since>
    public const double SmoothValue = 0.0;

    /// <summary>
    /// The value used to indicate that an edge is a crease, <see cref="MaximumValue"/> + 1.0.
    /// This is deliberately not a valid sharpness value: a crease edge is not a sharp edge.
    /// It exists because it is often convenient to use a single value to describe both.
    /// </summary>
    /// <since>8.36</since>
    public const double CreaseValue = MaximumValue + 1.0;

    /// <summary>
    /// If a sharpness is within Tolerance of an integer value, it is snapped to that
    /// integer value. See <see cref="Sanitize(double)"/>.
    /// </summary>
    /// <since>8.36</since>
    public const double Tolerance = 0.01;
    #endregion

    #region Constructors
    /// <summary>
    /// Creates a sharpness with the same value at both ends.
    /// </summary>
    /// <param name="sharpness">
    /// Between 0.0 and <see cref="MaximumValue"/>, or <see cref="CreaseValue"/>.
    /// </param>
    /// <remarks>
    /// If sharpness is not valid, the result is <see cref="Nan"/>.
    /// </remarks>
    /// <since>8.36</since>
    public SubDEdgeSharpness(double sharpness)
    {
      SubDEdgeSharpness rc = default(SubDEdgeSharpness);
      UnsafeNativeMethods.ON_SubDEdgeSharpness_FromConstant(sharpness, ref rc);
      m_sharpness0 = rc.m_sharpness0;
      m_sharpness1 = rc.m_sharpness1;
    }

    /// <summary>
    /// Creates a sharpness that varies from one end of the edge to the other.
    /// </summary>
    /// <param name="sharpness0">
    /// Sharpness at the start of the edge, between 0.0 and <see cref="MaximumValue"/>.
    /// </param>
    /// <param name="sharpness1">
    /// Sharpness at the end of the edge, between 0.0 and <see cref="MaximumValue"/>.
    /// </param>
    /// <remarks>
    /// If either value is not valid, the result is <see cref="Nan"/>.
    /// Passing <see cref="CreaseValue"/> for both values gives <see cref="Crease"/>.
    /// </remarks>
    /// <since>8.36</since>
    public SubDEdgeSharpness(double sharpness0, double sharpness1)
    {
      SubDEdgeSharpness rc = default(SubDEdgeSharpness);
      UnsafeNativeMethods.ON_SubDEdgeSharpness_FromInterval(sharpness0, sharpness1, ref rc);
      m_sharpness0 = rc.m_sharpness0;
      m_sharpness1 = rc.m_sharpness1;
    }

    /// <summary>
    /// Creates a sharpness that varies from one end of the edge to the other.
    /// </summary>
    /// <param name="sharpnessInterval">
    /// Sharpness at the start and end of the edge. Both values must be between 0.0 and
    /// <see cref="MaximumValue"/>.
    /// </param>
    /// <remarks>
    /// If either value is not valid, the result is <see cref="Nan"/>.
    /// </remarks>
    /// <since>8.36</since>
    public SubDEdgeSharpness(Interval sharpnessInterval)
      : this(sharpnessInterval.T0, sharpnessInterval.T1)
    {
    }

    /// <summary>
    /// Creates a sharpness with the same value at both ends, from a percentage.
    /// This is useful in user interface code that expresses sharpness as a percentage.
    /// </summary>
    /// <param name="percentage">
    /// Between 0.0 and 100.0, or <see cref="double.MaxValue"/> for a crease.
    /// </param>
    /// <returns>
    /// A sharpness with constant value (percentage * <see cref="MaximumValue"/> / 100.0),
    /// <see cref="Crease"/> if percentage is <see cref="double.MaxValue"/>,
    /// or <see cref="Nan"/> if percentage is out of range.
    /// </returns>
    /// <since>8.36</since>
    public static SubDEdgeSharpness FromConstantPercentage(double percentage)
    {
      SubDEdgeSharpness rc = default(SubDEdgeSharpness);
      UnsafeNativeMethods.ON_SubDEdgeSharpness_FromConstantPercentage(percentage, ref rc);
      return rc;
    }

    /// <summary>
    /// Creates a sharpness that varies from one end of the edge to the other, from percentages.
    /// This is useful in user interface code that expresses sharpness as a percentage.
    /// </summary>
    /// <param name="percentage0">
    /// Percentage at the start of the edge, between 0.0 and 100.0,
    /// or <see cref="double.MaxValue"/> for a crease.
    /// </param>
    /// <param name="percentage1">
    /// Percentage at the end of the edge, between 0.0 and 100.0,
    /// or <see cref="double.MaxValue"/> for a crease.
    /// </param>
    /// <returns>
    /// A sharpness running from (percentage0 * <see cref="MaximumValue"/> / 100.0) to
    /// (percentage1 * <see cref="MaximumValue"/> / 100.0),
    /// <see cref="Crease"/> if both percentages are <see cref="double.MaxValue"/>,
    /// or <see cref="Nan"/> otherwise. An edge is either a crease or it is not, so mixing
    /// <see cref="double.MaxValue"/> with a percentage gives <see cref="Nan"/>.
    /// </returns>
    /// <since>8.36</since>
    public static SubDEdgeSharpness FromIntervalPercentage(double percentage0, double percentage1)
    {
      SubDEdgeSharpness rc = default(SubDEdgeSharpness);
      UnsafeNativeMethods.ON_SubDEdgeSharpness_FromIntervalPercentage(percentage0, percentage1, ref rc);
      return rc;
    }

    /// <summary>
    /// Creates a sharpness that varies from one end of the edge to the other, from percentages.
    /// This is useful in user interface code that expresses sharpness as a percentage.
    /// </summary>
    /// <param name="percentageInterval">
    /// Percentages at the start and end of the edge, each between 0.0 and 100.0,
    /// or <see cref="double.MaxValue"/> for a crease.
    /// </param>
    /// <returns>See <see cref="FromIntervalPercentage(double, double)"/>.</returns>
    /// <since>8.36</since>
    public static SubDEdgeSharpness FromIntervalPercentage(Interval percentageInterval)
    {
      return FromIntervalPercentage(percentageInterval.T0, percentageInterval.T1);
    }

    /// <summary>
    /// A sharpness of 0.0 at both ends, which is what a smooth edge has.
    /// This is also the default value of a <see cref="SubDEdgeSharpness"/>.
    /// </summary>
    /// <since>8.36</since>
    public static SubDEdgeSharpness Smooth
    {
      get { return default(SubDEdgeSharpness); }
    }

    /// <summary>
    /// A sharpness of <see cref="MaximumValue"/> at both ends. This is the sharpest an
    /// edge can be without being a crease.
    /// </summary>
    /// <since>8.36</since>
    public static SubDEdgeSharpness Maximum
    {
      get { return new SubDEdgeSharpness(MaximumValue); }
    }

    /// <summary>
    /// A sharpness of <see cref="CreaseValue"/> at both ends, used to indicate that an
    /// edge is a crease rather than a sharp edge. <see cref="IsValid"/> is false for this
    /// value; see <see cref="IsValidOrCrease"/>.
    /// </summary>
    /// <since>8.36</since>
    public static SubDEdgeSharpness Crease
    {
      get { return new SubDEdgeSharpness(CreaseValue, CreaseValue); }
    }

    /// <summary>
    /// A sharpness whose ends are both NaN. This is what the factory methods return when
    /// their input is not valid.
    /// </summary>
    /// <since>8.36</since>
    public static SubDEdgeSharpness Nan
    {
      get
      {
        SubDEdgeSharpness rc = default(SubDEdgeSharpness);
        rc.m_sharpness0 = float.NaN;
        rc.m_sharpness1 = float.NaN;
        return rc;
      }
    }
    #endregion

    #region Properties
    /// <summary>
    /// Gets the sharpness at the start or the end of the edge.
    /// </summary>
    /// <param name="endIndex">0 for the start of the edge, 1 for the end.</param>
    /// <returns>The sharpness, or NaN if endIndex is out of range.</returns>
    /// <since>8.36</since>
    public double this[int endIndex]
    {
      get
      {
        if (0 == endIndex)
          return m_sharpness0;
        if (1 == endIndex)
          return m_sharpness1;
        return double.NaN;
      }
    }

    /// <summary>
    /// Gets the sharpness at the start or the end of the edge.
    /// </summary>
    /// <param name="endIndex">0 for the start of the edge, 1 for the end.</param>
    /// <returns>The sharpness, or NaN if endIndex is out of range.</returns>
    /// <since>8.36</since>
    public double EndSharpness(int endIndex)
    {
      return this[endIndex];
    }

    /// <summary>
    /// Gets the average of the two end sharpness values.
    /// </summary>
    /// <since>8.36</since>
    public double Average
    {
      get { return 0.5 * (m_sharpness0 + m_sharpness1); }
    }

    /// <summary>
    /// Gets the smaller of the two end sharpness values.
    /// </summary>
    /// <since>8.36</since>
    public double MinimumEndSharpness
    {
      get { return m_sharpness0 <= m_sharpness1 ? m_sharpness0 : m_sharpness1; }
    }

    /// <summary>
    /// Gets the larger of the two end sharpness values.
    /// </summary>
    /// <since>8.36</since>
    public double MaximumEndSharpness
    {
      get { return m_sharpness0 >= m_sharpness1 ? m_sharpness0 : m_sharpness1; }
    }

    /// <summary>
    /// Gets <see cref="EndSharpness(int)">EndSharpness(1)</see> - EndSharpness(0),
    /// or NaN if this is neither valid nor a crease.
    /// </summary>
    /// <since>8.36</since>
    public double Delta
    {
      get { return UnsafeNativeMethods.ON_SubDEdgeSharpness_Delta(this); }
    }

    /// <summary>
    /// Gets +1 if the sharpness increases along the edge, -1 if it decreases, and 0 if it
    /// is constant. Returns <see cref="RhinoMath.UnsetIntIndex"/> if this is not valid.
    /// </summary>
    /// <since>8.36</since>
    public int Trend
    {
      get { return UnsafeNativeMethods.ON_SubDEdgeSharpness_Trend(this); }
    }

    /// <summary>
    /// Gets true if both ends have the same valid sharpness value.
    /// <see cref="Crease"/> and <see cref="Nan"/> are both false; see
    /// <see cref="IsConstantOrCrease"/>.
    /// </summary>
    /// <since>8.36</since>
    public bool IsConstant
    {
      get { return GetBool(idxIsConstant, false); }
    }

    /// <summary>
    /// Gets true if both ends have the same valid sharpness value, or if this is
    /// <see cref="Crease"/>.
    /// </summary>
    /// <since>8.36</since>
    public bool IsConstantOrCrease
    {
      get { return GetBool(idxIsConstant, true); }
    }

    /// <summary>
    /// Gets true if this is valid and the two ends differ.
    /// <see cref="Crease"/> and <see cref="Nan"/> are both false.
    /// </summary>
    /// <since>8.36</since>
    public bool IsVariable
    {
      get { return GetBool(idxIsVariable, false); }
    }

    /// <summary>
    /// Gets true if <see cref="EndSharpness(int)">EndSharpness(0)</see> is less than
    /// EndSharpness(1).
    /// </summary>
    /// <since>8.36</since>
    public bool IsIncreasing
    {
      get { return GetBool(idxIsIncreasing, false); }
    }

    /// <summary>
    /// Gets true if <see cref="EndSharpness(int)">EndSharpness(0)</see> is greater than
    /// EndSharpness(1).
    /// </summary>
    /// <since>8.36</since>
    public bool IsDecreasing
    {
      get { return GetBool(idxIsDecreasing, false); }
    }

    /// <summary>
    /// Gets true if both ends are 0.0, which is the sharpness of a smooth edge.
    /// </summary>
    /// <since>8.36</since>
    public bool IsZero
    {
      get { return GetBool(idxIsZero, false); }
    }

    /// <summary>
    /// Gets true if both ends are valid and at least one is greater than 0.0.
    /// <see cref="Crease"/> and <see cref="Nan"/> are both false: a crease edge is not a
    /// sharp edge.
    /// </summary>
    /// <since>8.36</since>
    public bool IsSharp
    {
      get { return GetBool(idxIsSharp, false); }
    }

    /// <summary>
    /// Gets true if this is <see cref="Crease"/>.
    /// </summary>
    /// <since>8.36</since>
    public bool IsCrease
    {
      get { return GetBool(idxIsCrease, false); }
    }

    /// <summary>
    /// Gets (<see cref="IsCrease"/> || <see cref="IsSharp"/>).
    /// </summary>
    /// <since>8.36</since>
    public bool IsCreaseOrSharp
    {
      get { return GetBool(idxIsCreaseOrSharp, false); }
    }

    /// <summary>
    /// Gets true if both ends are between 0.0 and <see cref="MaximumValue"/>.
    /// <see cref="Crease"/> and <see cref="Nan"/> are both false; see
    /// <see cref="IsValidOrCrease"/>.
    /// </summary>
    /// <since>8.36</since>
    public bool IsValid
    {
      get { return GetBool(idxIsValid, false); }
    }

    /// <summary>
    /// Gets true if both ends are between 0.0 and <see cref="MaximumValue"/>, or if this
    /// is <see cref="Crease"/>.
    /// </summary>
    /// <since>8.36</since>
    public bool IsValidOrCrease
    {
      get { return GetBool(idxIsValid, true); }
    }

    /// <summary>
    /// Gets the opposite of <see cref="IsValid"/>.
    /// <see cref="Crease"/> and <see cref="Nan"/> are both true.
    /// </summary>
    /// <since>8.36</since>
    public bool IsNotValid
    {
      get { return !IsValid; }
    }

    /// <summary>
    /// Gets the opposite of <see cref="IsValidOrCrease"/>.
    /// <see cref="Nan"/> is true and <see cref="Crease"/> is false.
    /// </summary>
    /// <since>8.36</since>
    public bool IsNotValidNorCrease
    {
      get { return !IsValidOrCrease; }
    }
    #endregion

    #region Methods
    /// <summary>
    /// Gets the sharpness this edge would have after one subdivision.
    /// </summary>
    /// <param name="endIndex">0 for the start of the edge, 1 for the end.</param>
    /// <returns>
    /// The subdivided sharpness, or <see cref="Smooth"/> if endIndex is out of range.
    /// </returns>
    /// <since>8.36</since>
    public SubDEdgeSharpness Subdivided(int endIndex)
    {
      SubDEdgeSharpness rc = default(SubDEdgeSharpness);
      UnsafeNativeMethods.ON_SubDEdgeSharpness_Subdivided(this, endIndex, ref rc);
      return rc;
    }

    /// <summary>
    /// Gets this sharpness with its two end values swapped.
    /// </summary>
    /// <since>8.36</since>
    public SubDEdgeSharpness Reversed()
    {
      SubDEdgeSharpness rc = default(SubDEdgeSharpness);
      UnsafeNativeMethods.ON_SubDEdgeSharpness_Reversed(this, ref rc);
      return rc;
    }

    /// <summary>
    /// Describes this sharpness as a percentage, for user interface code.
    /// A constant sharpness gives a single percentage, a variable one gives a range, and
    /// an invalid one gives a warning sign.
    /// </summary>
    /// <param name="orderMinToMax">
    /// If true, a variable sharpness is formatted as min%-max%. If false, it is formatted
    /// as <see cref="EndSharpness(int)">EndSharpness(0)</see>%-EndSharpness(1)%.
    /// </param>
    /// <since>8.36</since>
    public string ToPercentageText(bool orderMinToMax)
    {
      using (var sw = new StringWrapper())
      {
        UnsafeNativeMethods.ON_SubDEdgeSharpness_ToPercentageText(this, orderMinToMax, sw.NonConstPointer);
        return sw.ToString();
      }
    }

    /// <summary>
    /// Returns a string that represents this sharpness as a percentage.
    /// </summary>
    /// <since>8.36</since>
    public override string ToString()
    {
      return ToPercentageText(false);
    }

    /// <summary>
    /// Describes a single end sharpness value as a percentage, for user interface code.
    /// </summary>
    /// <param name="sharpness">
    /// Between 0.0 and <see cref="MaximumValue"/>, or <see cref="CreaseValue"/>.
    /// </param>
    /// <returns>
    /// A number followed by a percent sign, "crease" for <see cref="CreaseValue"/>, or a
    /// warning sign if sharpness is not valid.
    /// </returns>
    /// <since>8.36</since>
    public static string ToPercentageText(double sharpness)
    {
      using (var sw = new StringWrapper())
      {
        UnsafeNativeMethods.ON_SubDEdgeSharpness_EndValueToPercentageText(sharpness, sw.NonConstPointer);
        return sw.ToString();
      }
    }

    /// <summary>
    /// Converts a sharpness value to a percentage between 0.0 and 100.0.
    /// </summary>
    /// <param name="sharpness">
    /// Between 0.0 and <see cref="MaximumValue"/>, or <see cref="CreaseValue"/>.
    /// </param>
    /// <param name="creasePercentage">
    /// The value to return when sharpness is <see cref="CreaseValue"/>.
    /// </param>
    /// <returns>
    /// 100.0 * sharpness / <see cref="MaximumValue"/>, creasePercentage for
    /// <see cref="CreaseValue"/>, or NaN if sharpness is not valid.
    /// </returns>
    /// <since>8.36</since>
    public static double ToPercentage(double sharpness, double creasePercentage)
    {
      return UnsafeNativeMethods.ON_SubDEdgeSharpness_ToPercentage(sharpness, creasePercentage);
    }

    /// <summary>
    /// Determines whether a value can be used as an edge end sharpness.
    /// </summary>
    /// <param name="candidateValue">The value to check.</param>
    /// <param name="creaseResult">
    /// The value to return when candidateValue is <see cref="CreaseValue"/>.
    /// </param>
    /// <since>8.36</since>
    public static bool IsValidValue(double candidateValue, bool creaseResult)
    {
      return UnsafeNativeMethods.ON_SubDEdgeSharpness_IsValidValue(candidateValue, creaseResult);
    }

    /// <summary>
    /// Verifies that sharpness is between 0.0 and <see cref="MaximumValue"/>, and snaps it
    /// to an integer when it is within <see cref="Tolerance"/> of one.
    /// </summary>
    /// <param name="sharpness">The value to sanitize.</param>
    /// <returns>A usable sharpness value, or 0.0 if sharpness is not valid.</returns>
    /// <since>8.36</since>
    public static double Sanitize(double sharpness)
    {
      return Sanitize(sharpness, 0.0);
    }

    /// <summary>
    /// Verifies that sharpness is between 0.0 and <see cref="MaximumValue"/>, and snaps it
    /// to an integer when it is within <see cref="Tolerance"/> of one.
    /// </summary>
    /// <param name="sharpness">The value to sanitize.</param>
    /// <param name="invalidInputResult">The value to return when sharpness is not valid.</param>
    /// <returns>A usable sharpness value, or invalidInputResult if sharpness is not valid.</returns>
    /// <since>8.36</since>
    public static double Sanitize(double sharpness, double invalidInputResult)
    {
      return UnsafeNativeMethods.ON_SubDEdgeSharpness_Sanitize(sharpness, invalidInputResult);
    }

    /// <summary>
    /// Converts a user facing slider value to an edge end sharpness value.
    /// </summary>
    /// <param name="sliderDomain">
    /// The non empty domain of the slider, often <see cref="Interval.ZeroToOne"/>.
    /// </param>
    /// <param name="sliderValue">
    /// A value in sliderDomain. sliderDomain.T0 maps to 0.0 and sliderDomain.T1 maps to
    /// <see cref="MaximumValue"/>.
    /// </param>
    /// <param name="invalidInputResult">The value to return when the input is not valid.</param>
    /// <since>8.36</since>
    public static double SharpnessFromSliderValue(Interval sliderDomain, double sliderValue, double invalidInputResult)
    {
      return UnsafeNativeMethods.ON_SubDEdgeSharpness_SharpnessFromSliderValue(sliderDomain, sliderValue, invalidInputResult);
    }

    /// <summary>
    /// Converts a normalized slider value to an edge end sharpness value.
    /// </summary>
    /// <param name="normalizedSliderValue">Between 0.0 and 1.0.</param>
    /// <returns>
    /// normalizedSliderValue scaled to the range 0.0 to <see cref="MaximumValue"/>, or NaN
    /// if it is out of range.
    /// </returns>
    /// <since>8.36</since>
    public static double SharpnessFromNormalizedValue(double normalizedSliderValue)
    {
      return UnsafeNativeMethods.ON_SubDEdgeSharpness_SharpnessFromNormalizedValue(normalizedSliderValue);
    }

    /// <summary>
    /// Gets the union of two sharpness ranges, ignoring the ones that are zero, a crease,
    /// or not valid.
    /// </summary>
    /// <param name="sharpness0">The first sharpness.</param>
    /// <param name="sharpness1">The second sharpness.</param>
    /// <returns>
    /// The union of the nonzero valid inputs, or <see cref="Smooth"/> if there are none.
    /// </returns>
    /// <since>8.36</since>
    public static SubDEdgeSharpness Union(SubDEdgeSharpness sharpness0, SubDEdgeSharpness sharpness1)
    {
      SubDEdgeSharpness rc = default(SubDEdgeSharpness);
      UnsafeNativeMethods.ON_SubDEdgeSharpness_Union(sharpness0, sharpness1, ref rc);
      return rc;
    }

    /// <summary>
    /// Determines whether two sharpnesses meet with the same value, that is, whether
    /// s0.<see cref="EndSharpness(int)">EndSharpness(1)</see> equals s1.EndSharpness(0).
    /// </summary>
    /// <param name="s0">The sharpness of the first edge.</param>
    /// <param name="s1">The sharpness of the second edge.</param>
    /// <since>8.36</since>
    public static bool EqualEndSharpness(SubDEdgeSharpness s0, SubDEdgeSharpness s1)
    {
      return UnsafeNativeMethods.ON_SubDEdgeSharpness_EqualEndSharpness(s0, s1);
    }

    /// <summary>
    /// Determines whether two sharpnesses have the same <see cref="Trend"/> and meet with
    /// the same value.
    /// </summary>
    /// <param name="s0">The sharpness of the first edge.</param>
    /// <param name="s1">The sharpness of the second edge.</param>
    /// <since>8.36</since>
    public static bool EqualTrend(SubDEdgeSharpness s0, SubDEdgeSharpness s1)
    {
      return UnsafeNativeMethods.ON_SubDEdgeSharpness_EqualTrend(s0, s1);
    }

    /// <summary>
    /// Determines whether two sharpnesses have the same <see cref="Delta"/> and meet with
    /// the same value.
    /// </summary>
    /// <param name="s0">The sharpness of the first edge.</param>
    /// <param name="s1">The sharpness of the second edge.</param>
    /// <since>8.36</since>
    public static bool EqualDelta(SubDEdgeSharpness s0, SubDEdgeSharpness s1)
    {
      return UnsafeNativeMethods.ON_SubDEdgeSharpness_EqualDelta(s0, s1);
    }

    /// <summary>
    /// Builds a sequence of evenly changing sharpnesses for the edges of a chain.
    /// </summary>
    /// <param name="chainSharpnessRange">
    /// The sharpness at the start of the chain and at the end of the chain.
    /// </param>
    /// <param name="edgeCount">The number of edges in the chain.</param>
    /// <returns>
    /// One sharpness per edge, or an empty array if the input is not valid.
    /// </returns>
    /// <since>8.36</since>
    public static SubDEdgeSharpness[] CreateEdgeChainSharpness(Interval chainSharpnessRange, int edgeCount)
    {
      if (edgeCount <= 0)
        return new SubDEdgeSharpness[0];

      var rc = new SubDEdgeSharpness[edgeCount];
      uint count = UnsafeNativeMethods.ON_SubDEdgeSharpness_SetEdgeChainSharpness(chainSharpnessRange, (uint)edgeCount, rc);
      if (0 == count)
        return new SubDEdgeSharpness[0];
      return rc;
    }

    /// <summary>
    /// Calculates the sharpness of a vertex from the sharp edges attached to it.
    /// Vertices tagged as corners always have a sharpness of 0.0.
    /// </summary>
    /// <param name="vertexTag">The vertex tag.</param>
    /// <param name="interiorCreaseVertexSharpness">
    /// Only meaningful for an interior crease vertex, where it is the largest sharpness at
    /// this vertex over the smooth edges of both sectors. This matters in low level SubD
    /// evaluation code that only has information about one sector. When in doubt, pass 0.0.
    /// </param>
    /// <param name="sharpEdgeEndCount">
    /// The number of sharp edges at the vertex whose sharpness at this vertex is nonzero.
    /// </param>
    /// <param name="maximumEdgeEndSharpness">
    /// The largest sharp edge end sharpness at the vertex.
    /// </param>
    /// <returns>The sharpness to use when subdividing the vertex.</returns>
    /// <since>8.36</since>
    [CLSCompliant(false)]
    public static double VertexSharpness(SubDVertexTag vertexTag, double interiorCreaseVertexSharpness, uint sharpEdgeEndCount, double maximumEdgeEndSharpness)
    {
      return UnsafeNativeMethods.ON_SubDEdgeSharpness_VertexSharpness(vertexTag, interiorCreaseVertexSharpness, sharpEdgeEndCount, maximumEdgeEndSharpness);
    }
    #endregion

    #region Equality
    /// <summary>
    /// Determines whether this sharpness has the same end values as another one.
    /// </summary>
    /// <param name="other">The sharpness to compare with.</param>
    /// <since>8.36</since>
    public bool Equals(SubDEdgeSharpness other)
    {
      return m_sharpness0 == other.m_sharpness0 && m_sharpness1 == other.m_sharpness1;
    }

    /// <summary>
    /// Determines whether an object is a sharpness with the same end values as this one.
    /// </summary>
    /// <param name="obj">The object to compare with.</param>
    /// <since>8.36</since>
    public override bool Equals(object obj)
    {
      return obj is SubDEdgeSharpness && Equals((SubDEdgeSharpness)obj);
    }

    /// <summary>
    /// Gets a hash code for this sharpness.
    /// </summary>
    /// <since>8.36</since>
    public override int GetHashCode()
    {
      return m_sharpness0.GetHashCode() ^ (m_sharpness1.GetHashCode() << 1);
    }

    /// <summary>
    /// Determines whether two sharpnesses have the same end values.
    /// </summary>
    /// <param name="a">The first sharpness.</param>
    /// <param name="b">The second sharpness.</param>
    /// <since>8.36</since>
    public static bool operator ==(SubDEdgeSharpness a, SubDEdgeSharpness b)
    {
      return a.Equals(b);
    }

    /// <summary>
    /// Determines whether two sharpnesses have different end values.
    /// </summary>
    /// <param name="a">The first sharpness.</param>
    /// <param name="b">The second sharpness.</param>
    /// <since>8.36</since>
    public static bool operator !=(SubDEdgeSharpness a, SubDEdgeSharpness b)
    {
      return !a.Equals(b);
    }
    #endregion

    #region Internals
    // Must match the indices in ON_SubDEdgeSharpness_GetBool in on_subd.cpp.
    const int idxIsConstant = 0;
    const int idxIsVariable = 1;
    const int idxIsIncreasing = 2;
    const int idxIsDecreasing = 3;
    const int idxIsZero = 4;
    const int idxIsSharp = 5;
    const int idxIsCrease = 6;
    const int idxIsCreaseOrSharp = 7;
    const int idxIsValid = 8;

    bool GetBool(int which, bool creaseResult)
    {
      return UnsafeNativeMethods.ON_SubDEdgeSharpness_GetBool(this, which, creaseResult);
    }
    #endregion
  }

  /// <summary>
  /// An ordered run of connected SubD edges.
  /// <para>
  /// Consecutive edges in a chain share a vertex, and each edge carries the direction it is
  /// traversed with, so the chain has a start and an end even when the individual edges do
  /// not agree on orientation. Chains are what commands like Bridge, Loft and edge
  /// sharpening operate on.
  /// </para>
  /// </summary>
  /// <remarks>
  /// A chain holds a reference to its SubD and does not keep it alive across edits that
  /// change the SubD topology; rebuild the chain after editing.
  /// </remarks>
  /// <since>8.36</since>
  public sealed class SubDEdgeChain : IDisposable
  {
    IntPtr m_ptr; // ON_SubDEdgeChain*
    readonly SubD m_subd;

    internal IntPtr ConstPointer() { return m_ptr; }
    internal IntPtr NonConstPointer() { return m_ptr; }

    /// <summary>
    /// Creates an empty chain in the given SubD.
    /// </summary>
    /// <param name="subd">The SubD the chain will run through.</param>
    /// <since>8.36</since>
    public SubDEdgeChain(SubD subd)
    {
      if (null == subd)
        throw new ArgumentNullException(nameof(subd));
      m_subd = subd;
      m_ptr = UnsafeNativeMethods.ON_SubDEdgeChain_New();
    }

    /// <summary>
    /// Creates a chain containing a single starting edge. Grow it with
    /// <see cref="AddAllNeighbors(ChainDirection, SubDChainType)"/> or
    /// <see cref="AddOneNeighbor(ChainDirection, SubDChainType)"/>.
    /// </summary>
    /// <param name="subd">The SubD the chain runs through.</param>
    /// <param name="startEdge">The edge to start from.</param>
    /// <since>8.36</since>
    public SubDEdgeChain(SubD subd, SubDEdge startEdge)
      : this(subd)
    {
      if (null == startEdge)
        throw new ArgumentNullException(nameof(startEdge));
      Begin(startEdge);
    }

    /// <summary>
    /// The SubD this chain runs through.
    /// </summary>
    /// <since>8.36</since>
    public SubD ParentSubD
    {
      get { return m_subd; }
    }

    /// <summary>
    /// Clears the chain and restarts it from a single edge.
    /// </summary>
    /// <param name="startEdge">The edge to start from.</param>
    /// <returns>The number of edges in the chain, so 1 on success and 0 on failure.</returns>
    /// <since>8.36</since>
    [CLSCompliant(false)]
    public uint Begin(SubDEdge startEdge)
    {
      if (null == startEdge)
        throw new ArgumentNullException(nameof(startEdge));
      IntPtr ptr_subd_ref = m_subd.SubDRefPointer();
      IntPtr const_ptr_edge = startEdge.ConstPointer();
      uint rc = UnsafeNativeMethods.ON_SubDEdgeChain_BeginEdgeChain(m_ptr, ptr_subd_ref, const_ptr_edge);
      GC.KeepAlive(startEdge);
      GC.KeepAlive(m_subd);
      return rc;
    }

    /// <summary>
    /// The number of edges in this chain.
    /// </summary>
    /// <since>8.36</since>
    [CLSCompliant(false)]
    public uint EdgeCount
    {
      get
      {
        uint rc = UnsafeNativeMethods.ON_SubDEdgeChain_EdgeCount(m_ptr);
        GC.KeepAlive(this);
        return rc;
      }
    }

    /// <summary>
    /// True when the chain closes back on itself.
    /// </summary>
    /// <since>8.36</since>
    public bool IsClosedLoop
    {
      get
      {
        bool rc = UnsafeNativeMethods.ON_SubDEdgeChain_IsClosedLoop(m_ptr);
        GC.KeepAlive(this);
        return rc;
      }
    }

    /// <summary>
    /// True when the chain is a closed loop that is convex.
    /// </summary>
    /// <param name="strictlyConvex">
    /// If true, a loop with three or more colinear points in a row is not convex.
    /// </param>
    /// <since>8.36</since>
    public bool IsConvexLoop(bool strictlyConvex)
    {
      bool rc = UnsafeNativeMethods.ON_SubDEdgeChain_IsConvexLoop(m_ptr, strictlyConvex);
      GC.KeepAlive(this);
      return rc;
    }

    /// <summary>
    /// Gets the edge at the given position in the chain. The returned edge carries the
    /// direction the chain traverses it with, in
    /// <see cref="SubDComponent.ComponentDirection"/>.
    /// </summary>
    /// <param name="index">The position in the chain.</param>
    /// <returns>The edge, or null if index is out of range.</returns>
    /// <since>8.36</since>
    public SubDEdge EdgeAt(int index)
    {
      uint id = 0;
      var cptr = UnsafeNativeMethods.ON_SubDEdgeChain_EdgePtrAt(m_ptr, index, ref id);
      GC.KeepAlive(this);
      if (0 == id)
        return null;
      return new SubDEdge(m_subd, cptr, id);
    }

    /// <summary>
    /// Gets the vertex at the given position along the chain. A chain of N edges has N+1
    /// vertices, or N when it is a closed loop.
    /// </summary>
    /// <param name="index">The position along the chain.</param>
    /// <returns>The vertex, or null if index is out of range.</returns>
    /// <since>8.36</since>
    public SubDVertex VertexAt(int index)
    {
      uint id = 0;
      IntPtr ptr_vertex = UnsafeNativeMethods.ON_SubDEdgeChain_VertexAt(m_ptr, index, ref id);
      GC.KeepAlive(this);
      if (IntPtr.Zero == ptr_vertex)
        return null;
      return new SubDVertex(m_subd, ptr_vertex, id);
    }

    /// <summary>
    /// The edges of this chain, in order.
    /// </summary>
    /// <since>8.36</since>
    public SubDEdge[] Edges
    {
      get
      {
        uint count = EdgeCount;
        var rc = new SubDEdge[count];
        for (int i = 0; i < rc.Length; i++)
          rc[i] = EdgeAt(i);
        return rc;
      }
    }

    /// <summary>
    /// Reverses the direction of the chain.
    /// </summary>
    /// <since>8.36</since>
    public void Reverse()
    {
      UnsafeNativeMethods.ON_SubDEdgeChain_Reverse(m_ptr);
      GC.KeepAlive(this);
    }

    /// <summary>
    /// Removes every edge from the chain.
    /// </summary>
    /// <since>8.36</since>
    public void Clear()
    {
      UnsafeNativeMethods.ON_SubDEdgeChain_ClearEdgeChain(m_ptr);
      GC.KeepAlive(this);
    }

    /// <summary>
    /// Extends the chain by one edge at one or both ends.
    /// </summary>
    /// <param name="direction">Which end or ends to extend.</param>
    /// <param name="chainType">Which edges and vertices the chain is allowed through.</param>
    /// <returns>The number of edges that were added.</returns>
    /// <since>8.36</since>
    [CLSCompliant(false)]
    public uint AddOneNeighbor(ChainDirection direction, SubDChainType chainType)
    {
      uint rc = UnsafeNativeMethods.ON_SubDEdgeChain_AddOneNeighbor(m_ptr, direction, chainType);
      GC.KeepAlive(this);
      return rc;
    }

    /// <summary>
    /// Extends the chain as far as it will go at one or both ends.
    /// </summary>
    /// <param name="direction">Which end or ends to extend.</param>
    /// <param name="chainType">Which edges and vertices the chain is allowed through.</param>
    /// <returns>The number of edges that were added.</returns>
    /// <since>8.36</since>
    [CLSCompliant(false)]
    public uint AddAllNeighbors(ChainDirection direction, SubDChainType chainType)
    {
      uint rc = UnsafeNativeMethods.ON_SubDEdgeChain_AddAllNeighbors(m_ptr, direction, chainType);
      GC.KeepAlive(this);
      return rc;
    }

    /// <summary>
    /// Adds an edge to whichever end of the chain it connects to.
    /// </summary>
    /// <param name="edge">The edge to add.</param>
    /// <returns>
    /// The number of edges that were added, so 1 on success and 0 if the edge does not
    /// connect to either end, is already in the chain, or the chain is empty.
    /// </returns>
    /// <remarks>
    /// This cannot start a chain: adding to an empty chain does nothing and returns 0.
    /// Use <see cref="Begin(SubDEdge)"/> or the
    /// <see cref="SubDEdgeChain(SubD, SubDEdge)"/> constructor for the first edge.
    /// </remarks>
    /// <since>8.36</since>
    [CLSCompliant(false)]
    public uint AddEdge(SubDEdge edge)
    {
      if (null == edge)
        throw new ArgumentNullException(nameof(edge));
      IntPtr const_ptr_edge = edge.ConstPointer();
      uint rc = UnsafeNativeMethods.ON_SubDEdgeChain_AddEdge(m_ptr, const_ptr_edge);
      GC.KeepAlive(edge);
      GC.KeepAlive(this);
      return rc;
    }

    /// <summary>
    /// Trims the chain down to the run of edges between firstEdge and lastEdge, removing
    /// everything before firstEdge and everything after lastEdge.
    /// </summary>
    /// <param name="firstEdge">
    /// The edge to keep as the new start of the chain. Pass null to keep the current start.
    /// </param>
    /// <param name="lastEdge">
    /// The edge to keep as the new end of the chain. Pass null to keep the current end.
    /// </param>
    /// <returns>The number of edges that were removed.</returns>
    /// <remarks>
    /// This wraps ON_SubDEdgeChain::RemoveEdges, whose name reads as though the given
    /// range is what gets removed. It is the other way round: the range is what survives.
    /// Passing null for both edges therefore removes nothing and returns 0; use
    /// <see cref="Clear"/> to empty the chain.
    /// </remarks>
    /// <since>8.36</since>
    [CLSCompliant(false)]
    public uint TrimToRange(SubDEdge firstEdge, SubDEdge lastEdge)
    {
      IntPtr ptr_first = null == firstEdge ? IntPtr.Zero : firstEdge.ConstPointer();
      IntPtr ptr_last = null == lastEdge ? IntPtr.Zero : lastEdge.ConstPointer();
      uint rc = UnsafeNativeMethods.ON_SubDEdgeChain_RemoveEdges(m_ptr, ptr_first, ptr_last);
      GC.KeepAlive(firstEdge);
      GC.KeepAlive(lastEdge);
      GC.KeepAlive(this);
      return rc;
    }

    /// <summary>
    /// Sorts a set of edges into chains.
    /// </summary>
    /// <param name="subd">The SubD the edges belong to.</param>
    /// <param name="edges">
    /// The edges to sort. Where three or more of them meet at one vertex, no chain passes
    /// through that vertex.
    /// </param>
    /// <returns>
    /// One array of edges per chain, in chain order. Each returned edge carries the
    /// direction its chain traverses it with.
    /// </returns>
    /// <remarks>
    /// This uses the mark bits on the edges and vertices involved, so it must not run on
    /// the same SubD from more than one thread at a time.
    /// </remarks>
    /// <since>8.36</since>
    public static SubDEdge[][] SortEdgesIntoEdgeChains(SubD subd, IEnumerable<SubDEdge> edges)
    {
      if (null == subd)
        throw new ArgumentNullException(nameof(subd));
      if (null == edges)
        throw new ArgumentNullException(nameof(edges));

      SubDEdge[] edge_array = edges as SubDEdge[] ?? edges.ToArray();
      if (0 == edge_array.Length)
        return new SubDEdge[0][];

      var unsorted = new SubDComponent.SubDComponentPtr[edge_array.Length];
      for (int i = 0; i < edge_array.Length; i++)
      {
        if (null == edge_array[i])
          throw new ArgumentException("edges contains a null edge.", nameof(edges));
        unsorted[i] = edge_array[i].ConstComponentPtr();
      }

      // Every edge can end up in its own chain, and each chain adds a null separator.
      var sorted = new SubDComponent.SubDComponentPtr[2 * unsorted.Length];
      uint sorted_count = 0;
      UnsafeNativeMethods.ON_SubDEdgeChain_SortEdgesIntoEdgeChains(
        (uint)unsorted.Length, unsorted, (uint)sorted.Length, sorted, ref sorted_count);

      // The result is one flat run with a null separator after each chain.
      var chains = new List<SubDEdge[]>();
      var current = new List<SubDEdge>();
      for (uint i = 0; i < sorted_count; i++)
      {
        uint id = UnsafeNativeMethods.ON_SubDComponentPtr_ComponentId(sorted[i]);
        if (0 == id)
        {
          if (current.Count > 0)
          {
            chains.Add(current.ToArray());
            current.Clear();
          }
          continue;
        }
        current.Add(new SubDEdge(subd, sorted[i], id));
      }
      if (current.Count > 0)
        chains.Add(current.ToArray());

      GC.KeepAlive(edges);
      GC.KeepAlive(subd);
      return chains.ToArray();
    }

#if RHINO_SDK
    /// <summary>
    /// Gets a NURBS curve on the SubD surface following this chain.
    /// </summary>
    /// <returns>The curve, or null if the chain is not valid.</returns>
    /// <since>8.36</since>
    public NurbsCurve ToNurbsCurve()
    {
      IntPtr ptr_curve = UnsafeNativeMethods.ON_SubDEdgeChain_EdgeSurfaceCurve(m_ptr);
      GC.KeepAlive(this);
      return GeometryBase.CreateGeometryHelper(ptr_curve, null) as NurbsCurve;
    }

    /// <summary>
    /// Gets a NURBS curve suitable for lofting SubDs, following this chain.
    /// </summary>
    /// <returns>The curve, or null if the chain is not valid.</returns>
    /// <since>8.36</since>
    public NurbsCurve ToLoftCurve()
    {
      IntPtr ptr_curve = UnsafeNativeMethods.ON_SubDEdgeChain_LoftCurve(m_ptr);
      GC.KeepAlive(this);
      return GeometryBase.CreateGeometryHelper(ptr_curve, null) as NurbsCurve;
    }
#endif

    /// <summary>
    /// Frees the unmanaged chain.
    /// </summary>
    /// <since>8.36</since>
    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Finalizer.
    /// </summary>
    ~SubDEdgeChain()
    {
      Dispose(false);
    }

    void Dispose(bool disposing)
    {
      if (IntPtr.Zero != m_ptr)
      {
        UnsafeNativeMethods.ON_SubDEdgeChain_Delete(m_ptr);
        m_ptr = IntPtr.Zero;
      }
    }
  }
  
  /// <summary>
  /// Identifies a point on the surface of a <see cref="SubD"/>.
  /// <para>
  /// SubDs have no global (u,v) parameterization. Instead a point on the surface
  /// is identified relative to a component of the SubD: a vertex, a point on an
  /// edge, or a point inside a face. A point inside a face is given by the index
  /// of the face corner it belongs to plus two parameters inside that corner,
  /// because a face with five or more sides has no single well behaved
  /// (u,v) domain.
  /// </para>
  /// <para>
  /// Use <see cref="SubD.ClosestPoint(Point3d, out Point3d, out SubDComponentParameter)"/>
  /// to find a parameter and <see cref="SubD.Evaluate(SubDComponentParameter, out Point3d)"/>
  /// and its overloads to evaluate one.
  /// </para>
  /// </summary>
  [StructLayout(LayoutKind.Sequential)]
  public struct SubDComponentParameter : IEquatable<SubDComponentParameter>
  {
    // These fields mirror ON_SUBD_COMPONENT_PARAMETER_STRUCT and are marshalled
    // to and from ON_SubDComponentParameter. Do not reorder them.
    internal uint m_component_type; // ON_SubDComponentPtr::Type: 0 unset, 2 vertex, 4 edge, 6 face
    internal uint m_component_id;
    internal uint m_component_dir;
    internal uint m_value_a;
    internal uint m_value_b;
    internal uint m_reserved;
    internal double m_p0;
    internal double m_p1;

    private const uint component_type_unset = 0;
    private const uint component_type_vertex = 2;
    private const uint component_type_edge = 4;
    private const uint component_type_face = 6;

    /// <summary>
    /// The unset parameter. It does not identify a point on any SubD.
    /// </summary>
    /// <since>9.0</since>
    public static SubDComponentParameter Unset
    {
      get
      {
        var rc = new SubDComponentParameter
        {
          m_component_type = component_type_unset,
          m_p0 = RhinoMath.UnsetValue,
          m_p1 = RhinoMath.UnsetValue
        };
        return rc;
      }
    }

    /// <summary>
    /// Creates a parameter that identifies the surface point at a SubD vertex.
    /// </summary>
    /// <param name="vertexId">The id of a SubD vertex.</param>
    /// <param name="activeFaceId">
    /// The id of a face attached to the vertex, or 0 to let the SubD choose one.
    /// A vertex where several faces meet with a crease has a different surface
    /// normal in each sector, so evaluation needs to know which face is meant.
    /// </param>
    /// <since>9.0</since>
    [CLSCompliant(false)]
    public static SubDComponentParameter CreateVertexParameter(uint vertexId, uint activeFaceId)
    {
      var rc = Unset;
      if (vertexId == 0)
        return rc;
      rc.m_component_type = component_type_vertex;
      rc.m_component_id = vertexId;
      rc.m_value_b = activeFaceId;
      return rc;
    }

    /// <summary>
    /// Creates a parameter that identifies a surface point on a SubD edge.
    /// </summary>
    /// <param name="edgeId">The id of a SubD edge.</param>
    /// <param name="edgeParameter">
    /// A value between 0 and 1. 0 is the edge's start vertex and 1 is its end
    /// vertex.
    /// </param>
    /// <param name="activeFaceId">
    /// The id of a face attached to the edge, or 0 to let the SubD choose one.
    /// A crease edge has a different surface normal on each side.
    /// </param>
    /// <since>9.0</since>
    [CLSCompliant(false)]
    public static SubDComponentParameter CreateEdgeParameter(uint edgeId, double edgeParameter, uint activeFaceId)
    {
      var rc = Unset;
      if (edgeId == 0 || !RhinoMath.IsValidDouble(edgeParameter) || edgeParameter < 0.0 || edgeParameter > 1.0)
        return rc;
      rc.m_component_type = component_type_edge;
      rc.m_component_id = edgeId;
      rc.m_value_a = activeFaceId;
      rc.m_p0 = edgeParameter;
      return rc;
    }

    /// <summary>
    /// Creates a parameter that identifies a surface point inside a SubD face.
    /// </summary>
    /// <param name="faceId">The id of a SubD face.</param>
    /// <param name="faceEdgeCount">
    /// The number of edges of the face. Must be 3 or more.
    /// </param>
    /// <param name="cornerIndex">
    /// The index of the face corner the parameters are measured in.
    /// Must be less than <paramref name="faceEdgeCount"/>.
    /// </param>
    /// <param name="cornerS">
    /// A value between 0 and 1/2 measured from the corner vertex towards the
    /// midpoint of the edge that leaves the corner.
    /// </param>
    /// <param name="cornerT">
    /// A value between 0 and 1/2 measured from the corner vertex towards the
    /// midpoint of the edge that enters the corner.
    /// </param>
    /// <remarks>
    /// (0,0) is the corner vertex and (1/2,1/2) is the center of the face.
    /// </remarks>
    /// <since>9.0</since>
    [CLSCompliant(false)]
    public static SubDComponentParameter CreateFaceParameter(
      uint faceId,
      int faceEdgeCount,
      int cornerIndex,
      double cornerS,
      double cornerT)
    {
      var rc = Unset;
      if (faceId == 0 || faceEdgeCount < 3 || cornerIndex < 0 || cornerIndex >= faceEdgeCount)
        return rc;
      if (!RhinoMath.IsValidDouble(cornerS) || cornerS < 0.0 || cornerS > 0.5)
        return rc;
      if (!RhinoMath.IsValidDouble(cornerT) || cornerT < 0.0 || cornerT > 0.5)
        return rc;
      rc.m_component_type = component_type_face;
      rc.m_component_id = faceId;
      rc.m_value_a = (uint)cornerIndex;
      rc.m_value_b = (uint)faceEdgeCount;
      rc.m_p0 = cornerS;
      rc.m_p1 = cornerT;
      return rc;
    }

    /// <summary>
    /// Gets true if this parameter identifies a point on a SubD.
    /// </summary>
    /// <since>9.0</since>
    public bool IsSet
    {
      get { return m_component_id != 0 && m_component_type != component_type_unset; }
    }

    /// <summary>
    /// Gets the id of the SubD component this parameter is relative to, or 0 if
    /// this parameter is not set.
    /// </summary>
    /// <since>9.0</since>
    [CLSCompliant(false)]
    public uint ComponentId
    {
      get { return IsSet ? m_component_id : 0; }
    }

    /// <summary>
    /// Gets the component index of the SubD component this parameter is relative
    /// to. It is <see cref="ComponentIndex.Unset"/> if this parameter is not set.
    /// </summary>
    /// <since>9.0</since>
    public ComponentIndex ComponentIndex
    {
      get
      {
        if (IsSet)
        {
          switch (m_component_type)
          {
            case component_type_vertex:
              return new ComponentIndex(ComponentIndexType.SubdVertex, (int)m_component_id);
            case component_type_edge:
              return new ComponentIndex(ComponentIndexType.SubdEdge, (int)m_component_id);
            case component_type_face:
              return new ComponentIndex(ComponentIndexType.SubdFace, (int)m_component_id);
          }
        }
        return ComponentIndex.Unset;
      }
    }
    
    /// <summary>
    /// Gets true if this parameter identifies the surface point at a SubD vertex.
    /// </summary>
    /// <since>9.0</since>
    public bool IsVertexParameter
    {
      get { return IsSet && m_component_type == component_type_vertex; }
    }

    /// <summary>
    /// Gets true if this parameter identifies a surface point on a SubD edge.
    /// </summary>
    /// <since>9.0</since>
    public bool IsEdgeParameter
    {
      get { return IsSet && m_component_type == component_type_edge; }
    }

    /// <summary>
    /// Gets true if this parameter identifies a surface point inside a SubD face.
    /// </summary>
    /// <since>9.0</since>
    public bool IsFaceParameter
    {
      get { return IsSet && m_component_type == component_type_face; }
    }

    /// <summary>
    /// Gets the id of the face this parameter uses to resolve which sector of a
    /// vertex or which side of an edge it means, or 0 if there is none.
    /// It is always 0 when <see cref="IsFaceParameter"/> is true; use
    /// <see cref="ComponentId"/> in that case.
    /// </summary>
    /// <since>9.0</since>
    [CLSCompliant(false)]
    public uint ActiveFaceId
    {
      get
      {
        if (IsVertexParameter) return m_value_b;
        if (IsEdgeParameter) return m_value_a;
        return 0;
      }
    }
    
    /// <summary>
    /// Gets a value between 0 and 1 identifying a point on the edge, or
    /// <see cref="double.NaN"/> if <see cref="IsEdgeParameter"/> is false.
    /// </summary>
    /// <since>9.0</since>
    public double EdgeParameter
    {
      get { return IsEdgeParameter ? m_p0 : double.NaN; }
    }

    /// <summary>
    /// Gets the number of edges of the face, or 0 if
    /// <see cref="IsFaceParameter"/> is false.
    /// </summary>
    /// <since>9.0</since>
    public int FaceEdgeCount
    {
      get { return IsFaceParameter ? (int)m_value_b : 0; }
    }

    /// <summary>
    /// Gets the index of the face corner the face parameters are measured in, or
    /// -1 if <see cref="IsFaceParameter"/> is false.
    /// </summary>
    /// <since>9.0</since>
    public int FaceCornerIndex
    {
      get { return IsFaceParameter ? (int)m_value_a : -1; }
    }

    /// <summary>
    /// Gets the two parameters inside the face corner identified by
    /// <see cref="FaceCornerIndex"/>. Both run from 0 to 1/2: (0,0) is the corner
    /// vertex and (1/2,1/2) is the center of the face. X runs towards the
    /// midpoint of the edge leaving the corner and Y towards the midpoint of the
    /// edge entering it. The point is <see cref="Point2d.Unset"/> if
    /// <see cref="IsFaceParameter"/> is false.
    /// </summary>
    /// <since>9.0</since>
    public Point2d FaceCornerParameters
    {
      get { return IsFaceParameter ? new Point2d(m_p0, m_p1) : Point2d.Unset; }
    }

    /// <inheritdoc/>
    public override string ToString()
    {
      if (!IsSet)
        return "Unset";
      if (IsVertexParameter)
        return string.Format(System.Globalization.CultureInfo.InvariantCulture, "v{0}", m_component_id);
      if (IsEdgeParameter)
        return string.Format(System.Globalization.CultureInfo.InvariantCulture, "e{0}({1})", m_component_id, m_p0);
      return string.Format(
        System.Globalization.CultureInfo.InvariantCulture,
        "f{0}.{1}({2},{3})", m_component_id, m_value_a, m_p0, m_p1);
    }

    /// <inheritdoc/>
    /// <since>9.0</since>
    public bool Equals(SubDComponentParameter other)
    {
      if (!IsSet || !other.IsSet)
        return IsSet == other.IsSet;
      return m_component_type == other.m_component_type
        && m_component_id == other.m_component_id
        && m_component_dir == other.m_component_dir
        && m_value_a == other.m_value_a
        && m_value_b == other.m_value_b
        && m_p0.Equals(other.m_p0)
        && m_p1.Equals(other.m_p1);
    }

    /// <inheritdoc/>
    public override bool Equals(object obj)
    {
      return obj is SubDComponentParameter other && Equals(other);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
      if (!IsSet)
        return 0;
      return m_component_type.GetHashCode()
        ^ m_component_id.GetHashCode()
        ^ m_value_a.GetHashCode()
        ^ m_p0.GetHashCode()
        ^ m_p1.GetHashCode();
    }

    /// <summary>
    /// Determines whether two parameters are equal.
    /// </summary>
    /// <since>9.0</since>
    public static bool operator ==(SubDComponentParameter a, SubDComponentParameter b)
    {
      return a.Equals(b);
    }
    
    /// <summary>
    /// Determines whether two parameters are different.
    /// </summary>
    /// <since>9.0</since>
    public static bool operator !=(SubDComponentParameter a, SubDComponentParameter b)
    {
      return !a.Equals(b);
    }

  }
}

namespace Rhino.Geometry.Collections
{
  /// <summary>
  /// Provides access to all the vertices and vertex-related functionality of a SubD
  /// </summary>
  public class SubDVertexList : IEnumerable<SubDVertex>
  {
    SubD m_subd;
    internal SubDVertexList(SubD parent)
    {
      m_subd = parent;
    }

    #region properties
    /// <summary>
    /// Gets the number of SubD vertices.
    /// </summary>
    /// <since>7.0</since>
    public int Count
    {
      get
      {
        IntPtr const_ptr_subd = m_subd.ConstPointer();
        int rc = UnsafeNativeMethods.ON_SubD_GetInt(const_ptr_subd, UnsafeNativeMethods.SubDIntConst.VertexCount);
        GC.KeepAlive(m_subd);
        return rc;
      }
    }
    
    /// <summary>
    /// First vertex in this linked list of vertices
    /// </summary>
    /// <since>7.0</since>
    public SubDVertex First
    {
      get
      {
        IntPtr const_ptr_subd = m_subd.ConstPointer();
        uint id = 0;
        IntPtr const_ptr_vertex = UnsafeNativeMethods.ON_SubD_FirstVertex(const_ptr_subd, ref id);
        if (const_ptr_vertex != IntPtr.Zero)
          return new SubDVertex(m_subd, const_ptr_vertex, id);
        GC.KeepAlive(m_subd);
        return null;
      }
    }
    #endregion

    /// <summary>
    /// Find a vertex in this SubD with a given id
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    /// <since>7.0</since>
    [CLSCompliant(false)]
    public SubDVertex Find(uint id)
    {
      IntPtr const_subd_pointer = m_subd.ConstPointer();
      IntPtr ptr_vertex = UnsafeNativeMethods.ON_SubD_VertexFromId(const_subd_pointer, id);
      if (ptr_vertex != IntPtr.Zero)
        return new SubDVertex(m_subd, ptr_vertex, id);
      GC.KeepAlive(m_subd);
      return null;
    }

    /// <summary>
    /// Find a vertex in this SubD with a given id
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    /// <since>7.0</since>
    public SubDVertex Find(int id)
    {
      if (id < 0)
        throw new IndexOutOfRangeException();
      return Find((uint)id);
    }

    /// <summary>
    /// Implementation of IEnumerable, so a vertex list can be used with foreach and LINQ
    /// the same way <see cref="SubDEdgeList"/> and <see cref="SubDFaceList"/> can.
    /// </summary>
    /// <remarks>
    /// Note that this list has both a <see cref="First"/> property and, now that it is
    /// enumerable, a LINQ First() extension method. They disagree on an empty SubD: the
    /// property returns null and First() throws.
    /// </remarks>
    /// <since>8.36</since>
    public IEnumerator<SubDVertex> GetEnumerator()
    {
      return VertexEnumerator().GetEnumerator();
    }

    /// <since>8.36</since>
    IEnumerator IEnumerable.GetEnumerator()
    {
      return VertexEnumerator().GetEnumerator();
    }

    /// <summary>
    /// Walks the SubD's linked list of vertices on the active level.
    /// </summary>
    IEnumerable<SubDVertex> VertexEnumerator()
    {
      IntPtr const_ptr_subd = m_subd.ConstPointer();
      uint id = 0;
      IntPtr const_ptr_vertex = UnsafeNativeMethods.ON_SubD_FirstVertex(const_ptr_subd, ref id);
      while (const_ptr_vertex != IntPtr.Zero)
      {
        yield return new SubDVertex(m_subd, const_ptr_vertex, id);
        const_ptr_vertex = UnsafeNativeMethods.ON_SubDVertex_GetNext(const_ptr_vertex, ref id);
      }
      GC.KeepAlive(m_subd);
    }

    /// <summary>
    /// Add a new vertex to the end of the Vertex list.
    /// </summary>
    /// <param name="tag">The type of vertex tag, such as smooth or corner.</param>
    /// <param name="vertex">Location of new vertex.</param>
    /// <returns>The newly added vertex.</returns>
    /// <exception cref="ArgumentOutOfRangeException">If tag is unset or non-defined.</exception>
    /// <since>7.0</since>
    public SubDVertex Add(SubDVertexTag tag, Point3d vertex)
    {
      if (!SubD.IsSubDVertexTagDefined(tag))
        throw new ArgumentOutOfRangeException("tag");

      IntPtr ptr_subd = m_subd.NonConstPointer();
      uint id = 0;
      IntPtr ptr_vertex = UnsafeNativeMethods.ON_SubD_AddVertex(ptr_subd, tag, vertex, ref id);
      if (ptr_vertex != IntPtr.Zero)
        return new SubDVertex(m_subd, ptr_vertex, id);

      GC.KeepAlive(m_subd);
      return null;
    }

    /// <summary>
    /// Set vertex tags for a list of vertices. Useful for adding creases to SubDs
    /// </summary>
    /// <param name="vertexIndices">list of indices for the vertices to set tags on</param>
    /// <param name="tag">The type of vertex tag</param>
    /// <since>8.30</since>
    public void SetVertexTags(IEnumerable<int> vertexIndices, SubDVertexTag tag)
    {
      if (!SubD.IsSubDVertexTagDefined(tag))
        throw new ArgumentOutOfRangeException(nameof(tag));

      IntPtr ptr_subd = m_subd.NonConstPointer();

      using (var ciArray = new INTERNAL_ComponentIndexArray())
      {
        foreach (var index in vertexIndices)
        {
          ciArray.Add(new ComponentIndex(ComponentIndexType.SubdVertex, index));
        }
        IntPtr pCiArray = ciArray.NonConstPointer();
        UnsafeNativeMethods.ON_SubD_SetVertexTags(ptr_subd, tag, pCiArray);
        GC.KeepAlive(m_subd);
      }
    }

    /// <summary>
    /// Set vertex tags for a list of vertices. Useful for adding creases to SubDs
    /// </summary>
    /// <param name="vertices">list of vertices to set a specific tag on</param>
    /// <param name="tag">The type of vertex tag</param>
    /// <since>8.30</since>
    public void SetVertexTags(IEnumerable<SubDVertex> vertices, SubDVertexTag tag)
    {
      if (!SubD.IsSubDVertexTagDefined(tag))
        throw new ArgumentOutOfRangeException(nameof(tag));

      IntPtr ptr_subd = m_subd.NonConstPointer();

      using (var ciArray = new INTERNAL_ComponentIndexArray())
      {
        foreach (var vertex in vertices)
        {
          IntPtr constPtrVertex = vertex.ConstPointer();
          var ci = new ComponentIndex();
          UnsafeNativeMethods.ON_SubDVertex_ComponentIndex(constPtrVertex, ref ci);
          ciArray.Add(ci);
        }
        IntPtr pCiArray = ciArray.NonConstPointer();
        UnsafeNativeMethods.ON_SubD_SetVertexTags(ptr_subd, tag, pCiArray);
      }
      GC.KeepAlive(vertices);
      GC.KeepAlive(m_subd);
    }
  }

  /// <summary>
  /// All edges in a SubD
  /// </summary>
  public class SubDEdgeList : IEnumerable<SubDEdge>
  {
    SubD m_subd;
    internal SubDEdgeList(SubD parent)
    {
      m_subd = parent;
    }

    #region properties
    /// <summary>
    /// Gets the number of SubD edges.
    /// </summary>
    /// <since>7.0</since>
    public int Count
    {
      get
      {
        IntPtr const_ptr_subd = m_subd.ConstPointer();
        int rc = UnsafeNativeMethods.ON_SubD_GetInt(const_ptr_subd, UnsafeNativeMethods.SubDIntConst.EdgeCount);
        GC.KeepAlive(m_subd);
        return rc;
      }
    }
    #endregion

    /// <summary>
    /// Find an edge in this SubD with a given id
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    /// <since>7.0</since>
    [CLSCompliant(false)]
    public SubDEdge Find(uint id)
    {
      IntPtr const_subd_pointer = m_subd.ConstPointer();
      IntPtr ptr_edge = UnsafeNativeMethods.ON_SubD_EdgeFromId(const_subd_pointer, id);
      if (ptr_edge != IntPtr.Zero)
        return new SubDEdge(m_subd, ptr_edge, id);
      GC.KeepAlive(m_subd);
      return null;
    }

    /// <summary>
    /// Find an edge in this SubD with a given id
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    /// <since>7.0</since>
    public SubDEdge Find(int id)
    {
      if (id < 0)
        throw new IndexOutOfRangeException();
      return Find((uint)id);
    }

    /// <summary>
    /// Implementation of IEnumerable
    /// </summary>
    /// <returns></returns>
    /// <since>7.0</since>
    public IEnumerator<SubDEdge> GetEnumerator()
    {
      return EdgeEnumerator().GetEnumerator();
    }

    /// <since>7.0</since>
    IEnumerator IEnumerable.GetEnumerator()
    {
      return EdgeEnumerator().GetEnumerator();
    }

    /// <summary>
    /// All edges associated with this vertex
    /// </summary>
    IEnumerable<SubDEdge> EdgeEnumerator()
    {
      IntPtr const_ptr_subd = m_subd.ConstPointer();
      uint id = 0;
      IntPtr const_ptr_edge = UnsafeNativeMethods.ON_SubD_FirstEdge(const_ptr_subd, ref id);
      while( const_ptr_edge != IntPtr.Zero )
      {
        yield return new SubDEdge(m_subd, const_ptr_edge, id);
        const_ptr_edge = UnsafeNativeMethods.ON_SubDEdge_GetNext(const_ptr_edge, ref id);
      }
      GC.KeepAlive(m_subd);
    }

    /// <summary>
    /// Add a new edge to the list.
    /// </summary>
    /// <param name="tag">The type of edge tag, such as smooth or corner.</param>
    /// <param name="v0">First vertex.</param>
    /// <param name="v1">Second vertex.</param>
    /// <exception cref="ArgumentOutOfRangeException">If tag is unset or non-defined.</exception>
    /// <since>7.0</since>
    public SubDEdge Add(SubDEdgeTag tag, SubDVertex v0, SubDVertex v1)
    {
      if (!SubD.IsSubDEdgeTagDefined(tag))
        throw new ArgumentOutOfRangeException("tag");
      
      IntPtr ptr_subd = m_subd.NonConstPointer();

      IntPtr v0_ptr = v0.NonConstPointer();
      IntPtr v1_ptr = v1.NonConstPointer();

      uint id = 0;
      IntPtr ptr_edge = UnsafeNativeMethods.ON_SubD_AddEdge(ptr_subd, tag, v0_ptr, v1_ptr, ref id);
      if (ptr_edge != IntPtr.Zero)
        return new SubDEdge(m_subd, ptr_edge, id);

      GC.KeepAlive(v0);
      GC.KeepAlive(v1);
      GC.KeepAlive(m_subd);
      return null;
    }

    /// <summary>
    /// Set edge tags for a list of edges. Useful for adding creases to SubDs
    /// </summary>
    /// <param name="edgeIndices">list of indices for the edges to set tags on</param>
    /// <param name="tag">The type of edge tag</param>
    /// <since>7.7</since>
    public void SetEdgeTags(IEnumerable<int> edgeIndices, SubDEdgeTag tag)
    {
      if (!SubD.IsSubDEdgeTagDefined(tag))
        throw new ArgumentOutOfRangeException(nameof(tag));

      IntPtr ptr_subd = m_subd.NonConstPointer();

      using(var ciArray = new INTERNAL_ComponentIndexArray())
      {
        foreach(var index in edgeIndices)
        {
          ciArray.Add(new ComponentIndex(ComponentIndexType.SubdEdge, index));
        }
        IntPtr pCiArray = ciArray.NonConstPointer();
        UnsafeNativeMethods.ON_SubD_SetEdgeTags(ptr_subd, tag, pCiArray);
        GC.KeepAlive(m_subd);
      }
    }

    /// <summary>
    /// Set edge tags for a list of edges. Useful for adding creases to SubDs
    /// </summary>
    /// <param name="edges">list of edges to set a specific tag on</param>
    /// <param name="tag">The type of edge tag</param>
    /// <since>7.7</since>
    public void SetEdgeTags(IEnumerable<SubDEdge> edges, SubDEdgeTag tag)
    {
      if (!SubD.IsSubDEdgeTagDefined(tag))
        throw new ArgumentOutOfRangeException(nameof(tag));

      IntPtr ptr_subd = m_subd.NonConstPointer();

      using (var ciArray = new INTERNAL_ComponentIndexArray())
      {
        foreach (var edge in edges)
        {
          IntPtr constPtrEdge = edge.ConstPointer();
          var ci = new ComponentIndex();
          UnsafeNativeMethods.ON_SubDEdge_ComponentIndex(constPtrEdge, ref ci);
          ciArray.Add(ci);
        }
        IntPtr pCiArray = ciArray.NonConstPointer();
        UnsafeNativeMethods.ON_SubD_SetEdgeTags(ptr_subd, tag, pCiArray);
      }
      GC.KeepAlive(edges);
      GC.KeepAlive(m_subd);
    }

    /// <summary>
    /// Gets the edge between two vertices, adding a smooth edge if there is not one yet.
    /// </summary>
    /// <param name="v0">First vertex.</param>
    /// <param name="v1">Second vertex.</param>
    /// <returns>The existing or newly added edge, or null on failure.</returns>
    /// <since>8.36</since>
    public SubDEdge FindOrAdd(SubDVertex v0, SubDVertex v1)
    {
      if (null == v0)
        throw new ArgumentNullException(nameof(v0));
      if (null == v1)
        throw new ArgumentNullException(nameof(v1));

      IntPtr ptr_subd = m_subd.NonConstPointer();
      IntPtr ptr_v0 = v0.NonConstPointer();
      IntPtr ptr_v1 = v1.NonConstPointer();
      uint id = 0;
      var cptr = UnsafeNativeMethods.ON_SubD_FindOrAddEdge(ptr_subd, ptr_v0, ptr_v1, ref id);
      GC.KeepAlive(v0);
      GC.KeepAlive(v1);
      GC.KeepAlive(m_subd);
      if (0 == id)
        return null;
      return new SubDEdge(m_subd, cptr, id);
    }
  }

  /// <summary> All faces in a SubD </summary>
  public class SubDFaceList : IEnumerable<SubDFace>
  {
    SubD m_subd;
    internal SubDFaceList(SubD parent)
    {
      m_subd = parent;
    }

    #region properties
    /// <summary>
    /// Gets the number of SubD faces.
    /// </summary>
    /// <since>7.0</since>
    public int Count
    {
      get
      {
        IntPtr const_ptr_subd = m_subd.ConstPointer();
        int rc = UnsafeNativeMethods.ON_SubD_GetInt(const_ptr_subd, UnsafeNativeMethods.SubDIntConst.FaceCount);
        GC.KeepAlive(m_subd);
        return rc;
      }
    }
    #endregion

    /// <summary>
    /// Find a face in this SubD with a given id
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    /// <since>7.0</since>
    [CLSCompliant(false)]
    public SubDFace Find(uint id)
    {
      IntPtr const_subd_pointer = m_subd.ConstPointer();
      IntPtr ptr_face = UnsafeNativeMethods.ON_SubD_FaceFromId(const_subd_pointer, id);
      if (ptr_face != IntPtr.Zero)
        return new SubDFace(m_subd, ptr_face, id);
      GC.KeepAlive(m_subd);
      return null;
    }

    /// <summary>
    /// Find a face in this SubD with a given id
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    /// <since>7.0</since>
    public SubDFace Find(int id)
    {
      if (id < 0)
        throw new IndexOutOfRangeException();
      return Find((uint)id);
    }

    /// <summary>
    /// True if one or more faces on the active level have per face color overrides.
    /// </summary>
    /// <remarks>
    /// Per face colors are a mutable property on <see cref="SubDFace"/> and are set with  <see cref="SubDFace.PerFaceColor"/>.
    /// </remarks>
    /// <since>8.18</since>
    public bool HasPerFaceColors
    {
      get
      {
        IntPtr const_ptr_this = m_subd.ConstPointer();
        bool rc = UnsafeNativeMethods.ON_SubD_HasPerFaceColors(const_ptr_this);
        GC.KeepAlive(m_subd);
        return rc;
      }
    }

    /// <summary>
    /// Removes all per face color overrides on the active level.
    /// </summary>
    /// <returns>Number of changed faces.</returns>
    /// <remarks>
    /// Per face colors are a mutable property on <see cref="SubDFace"/> and are set with  <see cref="SubDFace.PerFaceColor"/>.
    /// </remarks>
    /// <since>8.18</since>
    [CLSCompliant(false)]
    public uint ClearPerFaceColors()
    {
      IntPtr ptr_this = m_subd.NonConstPointer();
      uint rc = UnsafeNativeMethods.ON_SubD_ClearPerFaceColors(ptr_this);
      GC.KeepAlive(m_subd);
      return rc;
    }

    /// <summary>
    /// Implementation of IEnumerable
    /// </summary>
    /// <returns></returns>
    /// <since>7.0</since>
    public IEnumerator<SubDFace> GetEnumerator()
    {
      return GetFaceEnumerator().GetEnumerator();
    }

    /// <since>7.0</since>
    IEnumerator IEnumerable.GetEnumerator()
    {
      return GetFaceEnumerator().GetEnumerator();
    }

    IEnumerable<SubDFace> GetFaceEnumerator()
    {
      IntPtr const_ptr_subd = m_subd.ConstPointer();
      bool parentSubdIsNonConst = m_subd.IsNonConst;
      uint id = 0;
      IntPtr const_ptr_face = UnsafeNativeMethods.ON_SubD_FirstFace(const_ptr_subd, ref id);
      while (const_ptr_face != IntPtr.Zero)
      {
        var face = new SubDFace(m_subd, const_ptr_face, id);
        yield return face;
        if(parentSubdIsNonConst != m_subd.IsNonConst)
        {
          // 2024-03-14, Pierre: WHY IS THAT THE ONLY PLACE WE CARE ABOUT THIS??
          // Other SubD enumerators do not care about that, neither do other objects' subobjects enumerators.

          // subd changed from const to non-const. This enumerator needs
          // to update to reflect this change. The face will have a different
          // pointer value
          const_ptr_face = face.ConstPointer();
        }
        const_ptr_face = UnsafeNativeMethods.ON_SubDFace_GetNext(const_ptr_face, ref id);
      }
      GC.KeepAlive(m_subd);
    }

    /// <summary>
    /// Adds a new edge to the end of the edge list.
    /// </summary>
    /// <param name="edges">edges to add</param>
    /// <param name="directions">The direction each of these edges has, related to the face.
    /// True means that the edge is reversed compared to the counter-clockwise order of the face.
    /// <para>This argument can be null. In this case, no edge is considered reversed.</para></param>
    /// <exception cref="ArgumentOutOfRangeException">If tag is unset or non-defined.</exception>
    internal SubDFace Add(SubDEdge[] edges, bool[] directions)
    {
      // private on purpose for now. I'm not happy with how this function is set up in that
      // you are required to pass parallel arrays of edges and directions.
      if (edges == null) throw new ArgumentNullException("edges");
      if (directions == null) directions = new bool[edges.Length];
      if (edges.Length != directions.Length) throw new ArgumentOutOfRangeException("directions", "Length of edges and directions must match.");
      if (edges.Length < 3) throw new ArgumentOutOfRangeException("edges", "There must be at least 3 edges in a SubD face.");

      //check to make sure all of the edges were created for this subd
      for( int i=0; i<edges.Length; i++)
      {
        if (edges[i].ParentSubD != m_subd)
          throw new Exception("SubD edge must be created for a specific SubD");
      }

      IntPtr ptr_subd = m_subd.NonConstPointer();

      IntPtr ptr_face;
      uint id = 0;
      unsafe
      {
        IntPtr* block = stackalloc IntPtr[edges.Length];
        for (int i = 0; i < edges.Length; i++)
          block[i] = edges[i].NonConstPointer();
        var ptr_ptr = new IntPtr(block);
        ptr_face = UnsafeNativeMethods.ON_SubD_AddFace(ptr_subd, (uint)edges.Length, ptr_ptr, directions, ref id);
      }

      //this should never happen...
      if (ptr_face == IntPtr.Zero) throw new InvalidOperationException(
        "Impossible to add this face to this SubD.");

      GC.KeepAlive(m_subd);
      return new SubDFace(m_subd, ptr_face, id);
    }

    /// <summary>
    /// Adds a face bounded by the given edges.
    /// </summary>
    /// <param name="edges">
    /// The edges of the new face, in order around its boundary. There must be at least
    /// three. Each edge is traversed according to its
    /// <see cref="SubDComponent.ComponentDirection"/>, so consecutive edges have to meet:
    /// the end vertex of one is the start vertex of the next. Set ComponentDirection on the
    /// edges before calling this to orient the loop.
    /// </param>
    /// <returns>The new face, or null if the edges do not form a usable boundary.</returns>
    /// <since>8.36</since>
    public SubDFace Add(IEnumerable<SubDEdge> edges)
    {
      if (null == edges)
        throw new ArgumentNullException(nameof(edges));

      SubDEdge[] edge_array = edges as SubDEdge[] ?? edges.ToArray();
      if (edge_array.Length < 3)
        throw new ArgumentException("A SubD face needs at least 3 edges.", nameof(edges));

      IntPtr ptr_subd = m_subd.NonConstPointer();
      var cptrs = new SubDComponent.SubDComponentPtr[edge_array.Length];
      for (int i = 0; i < edge_array.Length; i++)
      {
        if (null == edge_array[i])
          throw new ArgumentException("edges contains a null edge.", nameof(edges));
        cptrs[i] = edge_array[i].NonConstComponentPtr();
      }

      uint id = 0;
      IntPtr ptr_face = UnsafeNativeMethods.ON_SubD_AddFaceFromEdgePtrs(ptr_subd, (uint)cptrs.Length, cptrs, ref id);
      GC.KeepAlive(edges);
      GC.KeepAlive(m_subd);
      if (IntPtr.Zero == ptr_face || 0 == id)
        return null;
      return new SubDFace(m_subd, ptr_face, id);
    }

  }
}

