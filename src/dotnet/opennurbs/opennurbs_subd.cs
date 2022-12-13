using System;
using System.Collections;
using System.Collections.Generic;
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
#if RHINO_SDK
          if (IsNonConst)
          {
            IntPtr ptr_this = NonConstPointer();
            // 29 April 2020 S. Baer (RH-53342)
            // The following call ensures that edge curves will exist. We may want
            // move this call to another location.
            UnsafeNativeMethods.ON_SubD_UpdateSurfaceMeshCache(ptr_this);
          }
#endif
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
      return UnsafeNativeMethods.ON_SubD_PackFaces(ptr_this);
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
    /// If false (Freeform), frame are propogated based on a refrence direction taken from the rail curve curvature direction.
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
      return UnsafeNativeMethods.RHC_RhinoMergeAllCoplanarFaces(ptrThis, tolerance, angleTolerance);
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
    /// Clear cached information that depends on the location of vertex control points
    /// </summary>
    /// <since>7.0</since>
    public void ClearEvaluationCache()
    {
      var const_ptr_this = ConstPointer();
      UnsafeNativeMethods.ON_SubD_ClearEvaluationCache(const_ptr_this);
    }

    /// <summary>
    /// Returns a SubDComponent, eithere a SubDEdge, SubDFace, or SubDVertex, from a component index.
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
      return UnsafeNativeMethods.ON_SubD_UpdateAllTagsAndSectorCoefficients(ptr_this, false);
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
    /// Apply the Catmull-Clark subdivision algorithm and save the results in
    /// this SubD
    /// </summary>
    /// <param name="count">Number of times to subdivide (must be greater than 0)</param>
    /// <returns>true on success</returns>
    /// <since>7.0</since>
    public bool Subdivide(int count)
    {
      if (count < 1)
        return false;
      IntPtr ptrSubD = NonConstPointer();
      return UnsafeNativeMethods.ON_SubD_GlobalSubdivide(ptrSubD, (uint)count);
    }

#if RHINO_SDK
    /// <summary>
    /// Modifies the SubD so that the SubD vertex limit surface points are
    /// equal to surface_points[]
    /// </summary>
    /// <param name="surfacePoints">
    /// point for limit surface to interpolate. surface_points[i] is the
    /// location for the i-th vertex returned by SubVertexIterator vit(this)
    /// </param>
    /// <returns></returns>
    /// <since>7.1</since>
    public bool InterpolateSurfacePoints(Point3d[] surfacePoints)
    {
      IntPtr ptrThis = NonConstPointer();
      return UnsafeNativeMethods.ON_SubD_InterpolateSurfacePoints(ptrThis, surfacePoints.Length, surfacePoints);
    }
#endif
  }

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
    /// Look for convex corners at sub-D vertices with 2 edges that have an
    /// included angle &lt;= 90 degrees.
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
        return (InteriorCreaseOption)rc;
      }
      set
      {
        IntPtr ptr_this = NonConstPointer();
        UnsafeNativeMethods.ON_ToSubDParameters_SetInteriorCreaseOption(ptr_this, (uint)value);
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
        return (ConvexCornerOption)rc;
      }
      set
      {
        IntPtr ptr_this = NonConstPointer();
        UnsafeNativeMethods.ON_ToSubDParameters_SetConvexCornerOption(ptr_this, (uint)value);
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
        return UnsafeNativeMethods.ON_ToSubDParameters_MaximumConvexCornerEdgeCount(const_ptr_this);
      }
      set
      {
        IntPtr ptr_this = NonConstPointer();
        UnsafeNativeMethods.ON_ToSubDParameters_SetMaximumConvexCornerEdgeCount(ptr_this, value);
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
        return UnsafeNativeMethods.ON_ToSubDParameters_MaximumConvexCornerAngleRadians(const_ptr_this);
      }
      set
      {
        IntPtr ptr_this = NonConstPointer();
        UnsafeNativeMethods.ON_ToSubDParameters_SetMaximumConvexCornerAngleRadians(ptr_this, value);
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
        return (ConcaveCornerOption)rc;
      }
      set
      {
        IntPtr ptr_this = NonConstPointer();
        UnsafeNativeMethods.ON_ToSubDParameters_SetConcaveCornerOption(ptr_this, (uint)value);
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
        return UnsafeNativeMethods.ON_ToSubDParameters_MinimumConcaveCornerAngleRadians(const_ptr_this);
      }
      set
      {
        IntPtr ptr_this = NonConstPointer();
        UnsafeNativeMethods.ON_ToSubDParameters_SetMinimumConcaveCornerAngleRadians(ptr_this, value);
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
        return UnsafeNativeMethods.ON_ToSubDParameters_MinimumConcaveCornerEdgeCount(const_ptr_this);
      }
      set
      {
        IntPtr ptr_this = NonConstPointer();
        UnsafeNativeMethods.ON_ToSubDParameters_SetMinimumConcaveCornerEdgeCount(ptr_this, value);
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
        return UnsafeNativeMethods.ON_ToSubDParameters_InterpolateMeshVertices(const_ptr_this);
      }
      set
      {
        IntPtr ptr_this = NonConstPointer();
        UnsafeNativeMethods.ON_ToSubDParameters_SetInterpolateMeshVertices(ptr_this, value);
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
        return UnsafeNativeMethods.ON_SubDToBrepParameters_PackFaces(const_ptr_this);
      }
      set
      {
        IntPtr ptr_this = NonConstPointer();
        UnsafeNativeMethods.ON_SubDToBrepParameters_SetPackFaces(ptr_this, value);
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
        return (ExtraordinaryVertexProcessOption)UnsafeNativeMethods.ON_SubDToBrepParameters_ExtraordinaryVertexProcess(const_ptr_this);
      }
      set
      {
        IntPtr ptr_this = NonConstPointer();
        UnsafeNativeMethods.ON_SubDToBrepParameters_SetExtraordinaryVertexProcess(ptr_this, (uint)value);
      }
    }
  }

  /// <summary>
  /// A part of SubD geometry. Common base class for vertices, faces, and edges
  /// </summary>
  public abstract class SubDComponent
  {
    IntPtr m_ptr; // either ON_SubDFace*, ON_SubDEdge*, or ON_SubDVertex*
    // NOTE: If we choose to save ON_SubDFacePtr.m_ptr values, we can add
    //       another field here to save that value
    ulong m_subd_serial_number;

    internal SubDComponent(SubD subd, IntPtr ptr, uint id)
    {
      m_ptr = ptr;
      ParentSubD = subd;
      Id = id;
      m_subd_serial_number = subd.RuntimeSerialNumber;
    }

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

    internal IntPtr ConstPointer()
    {
      if( m_subd_serial_number != ParentSubD.RuntimeSerialNumber )
      {
        m_subd_serial_number = ParentSubD.RuntimeSerialNumber;
        m_ptr = UpdatePointer();
      }
      return m_ptr;
    }

    internal IntPtr NonConstPointer()
    {
      // make sure the parent SubD is non-const
      ParentSubD.NonConstPointer();
      if (m_subd_serial_number != ParentSubD.RuntimeSerialNumber)
      {
        m_subd_serial_number = ParentSubD.RuntimeSerialNumber;
        m_ptr = UpdatePointer();
      }
      return m_ptr;
    }

    internal abstract IntPtr UpdatePointer();

    const int idx_cs_selected = 0;
    const int idx_cs_highlighted = 1;
    const int idx_cs_hidden = 2;
    const int idx_cs_locked = 3;
    const int idx_cs_deleted = 4;
    const int idx_cs_damaged = 5;

    internal bool GetComponentStatusBool(int which)
    {
      var const_ptr_this = ConstPointer();
      return UnsafeNativeMethods.ON_SubD_ComponentStatusBool(const_ptr_this, which);
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

    internal override IntPtr UpdatePointer()
    {
      IntPtr const_ptr_subd = ParentSubD.ConstPointer();
      return UnsafeNativeMethods.ON_SubDFace_FromId(const_ptr_subd, Id);
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
        return UnsafeNativeMethods.ON_SubDFace_EdgeCount(const_ptr_this);
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
        return System.Drawing.Color.FromArgb(argb);
      }
      set
      {
        IntPtr ptr_face = NonConstPointer();
        int argb = value.ToArgb();
        UnsafeNativeMethods.ON_SubDFace_SetPerFaceColor(ptr_face, argb);
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
        return res;
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
      return UnsafeNativeMethods.ON_SubDFace_EdgeDirectionMatches(const_ptr_this, (uint)index);
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

    internal override IntPtr UpdatePointer()
    {
      IntPtr const_ptr_subd = ParentSubD.ConstPointer();
      return UnsafeNativeMethods.ON_SubDVertex_FromId(const_ptr_subd, Id);
    }

    #region properties
    /// <summary>
    /// Location of the "control net" point that this SubDVertex represents
    /// </summary>
    /// <since>7.0</since>
    public Point3d ControlNetPoint
    {
      get
      {
        IntPtr const_vertex_ptr = ConstPointer();
        Point3d rc = default(Point3d);
        UnsafeNativeMethods.ON_SubDVertex_ControlNetPoint(const_vertex_ptr, ref rc);
        return rc;
      }
      set
      {
        IntPtr ptr_vertex = NonConstPointer();
        UnsafeNativeMethods.ON_SubDVertex_SetControlNetPoint(ptr_vertex, value);
      }
    }

    /// <summary> Number of edges for this vertex </summary>
    /// <since>7.0</since>
    public int EdgeCount
    {
      get
      {
        var const_ptr_this = ConstPointer();
        return UnsafeNativeMethods.ON_SubDVertex_EdgeCount(const_ptr_this);
      }
    }
    /// <summary> Number of faces for this vertex </summary>
    /// <since>7.0</since>
    public int FaceCount
    {
      get
      {
        var const_ptr_this = ConstPointer();
        return UnsafeNativeMethods.ON_SubDVertex_FaceCount(const_ptr_this);
      }
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
        return UnsafeNativeMethods.ON_SubDVertex_GetVertexTag(const_ptr_vertex);
      }
      set
      {
        var ptr_vertex = NonConstPointer();
        UnsafeNativeMethods.ON_SubDVertex_SetVertexTag(ptr_vertex, value);
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
    /// <since>7.1</since>
    public Point3d SurfacePoint()
    {
      IntPtr const_vertex_ptr = ConstPointer();
      Point3d rc = default(Point3d);
      UnsafeNativeMethods.ON_SubDVertex_SurfacePoint(const_vertex_ptr, ref rc);
      return rc;
    }
  }

  /// <summary> Single edge of a SubD </summary>
  public sealed class SubDEdge : SubDComponent
  {
    internal SubDEdge(SubD subd, IntPtr pointer, uint id) : base(subd, pointer, id)
    {
    }

    internal override IntPtr UpdatePointer()
    {
      IntPtr const_ptr_subd = ParentSubD.ConstPointer();
      return UnsafeNativeMethods.ON_SubDEdge_FromId(const_ptr_subd, Id);
    }

    #region properties
    /// <summary> Number of faces for this edge </summary>
    /// <since>7.0</since>
    public int FaceCount
    {
      get
      {
        var const_ptr_this = ConstPointer();
        return UnsafeNativeMethods.ON_SubDEdge_FaceCount(const_ptr_this);
      }
    }

    /// <summary>
    /// Line representing the control net end points
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
    /// Start vertex for this edge
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
        return null;
      }
    }

    /// <summary>
    /// End vertex for this edge
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
        return null;
      }
    }

    /// <summary>
    /// identifies the type of subdivision edge
    /// </summary>
    /// <since>7.0</since>
    public SubDEdgeTag Tag
    {
      get
      {
        var const_ptr_edge = ConstPointer();
        return UnsafeNativeMethods.ON_SubDEdge_GetEdgeTag(const_ptr_edge);
      }
      set
      {
        var ptr_edge = NonConstPointer();
        UnsafeNativeMethods.ON_SubDEdge_SetEdgeTag(ptr_edge, value);
      }
    }

#endregion

    /// <summary>
    /// Retrieve a SubDFace from this edge
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
    /// <param name="clampEnds">
    /// If true, the end knots are clamped.
    /// Otherwise the end knots are(-2,-1,0,...., k1, k1+1, k1+2).
    /// </param>
    /// <returns></returns>
    /// <since>7.0</since>
    public NurbsCurve ToNurbsCurve(bool clampEnds)
    {
      IntPtr const_ptr_this = ConstPointer();
      IntPtr ptr_nurbscurve = UnsafeNativeMethods.ON_SubDEdge_LimitCurve(const_ptr_this, clampEnds);
      return GeometryBase.CreateGeometryHelper(ptr_nurbscurve, null) as NurbsCurve;
    }
#endif

  }

}

namespace Rhino.Geometry.Collections
{
  /// <summary>
  /// Provides access to the vertices and vertex-related functionality of a SubD
  /// </summary>
  public class SubDVertexList
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
        return UnsafeNativeMethods.ON_SubD_GetInt(const_ptr_subd, UnsafeNativeMethods.SubDIntConst.VertexCount);
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
      IntPtr ptr_vertex = UnsafeNativeMethods.ON_SubDVertex_FromId(const_subd_pointer, id);
      if (ptr_vertex != IntPtr.Zero)
        return new SubDVertex(m_subd, ptr_vertex, id);
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

      return null;
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
        return UnsafeNativeMethods.ON_SubD_GetInt(const_ptr_subd, UnsafeNativeMethods.SubDIntConst.EdgeCount);
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
      IntPtr ptr_edge = UnsafeNativeMethods.ON_SubDEdge_FromId(const_subd_pointer, id);
      if (ptr_edge != IntPtr.Zero)
        return new SubDEdge(m_subd, ptr_edge, id);
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
        return UnsafeNativeMethods.ON_SubD_GetInt(const_ptr_subd, UnsafeNativeMethods.SubDIntConst.FaceCount);
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
      IntPtr ptr_face = UnsafeNativeMethods.ON_SubDFace_FromId(const_subd_pointer, id);
      if (ptr_face != IntPtr.Zero)
        return new SubDFace(m_subd, ptr_face, id);
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
          // subd changed from const to non-const. This enumerator needs
          // to update to reflect this change. The face will have a different
          // pointer value
          const_ptr_face = face.ConstPointer();
        }
        const_ptr_face = UnsafeNativeMethods.ON_SubDFace_GetNext(const_ptr_face, ref id);
      }
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

      return new SubDFace(m_subd, ptr_face, id);
    }

  }
}

