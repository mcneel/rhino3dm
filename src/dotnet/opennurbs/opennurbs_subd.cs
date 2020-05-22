using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Rhino.Runtime;

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
          m_edges = new Collections.SubDEdgeList(this);
        return m_edges;
      }
    }

    /// <summary>
    /// Test subd to see if the active level is a solid.  
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

#if RHINO_SDK
    /// <summary>
    /// Create a Brep based on this SubD geometry
    /// </summary>
    /// <returns></returns>
    /// <since>7.0</since>
    public Brep ToBrep()
    {
      return Brep.TryConvertBrep(this);
    }

    /// <summary>
    /// Create a new SubD from a mesh
    /// </summary>
    /// <param name="mesh"></param>
    /// <returns></returns>
    /// <since>7.0</since>
    public static SubD CreateFromMesh(Mesh mesh)
    {
      return CreateFromMesh(mesh, null);
    }

    /// <summary>
    /// Create a new SubD from a mesh
    /// </summary>
    /// <param name="mesh"></param>
    /// <param name="options"></param>
    /// <returns></returns>
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
#endif

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

    SubDCreationOptions(int which)
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
        return new SubDCreationOptions(0);
      }
    }

    /// <summary>
    /// Create an interior sub-D crease along coincident input mesh edges
    /// where the vertex normal directions at one end differ by at 
    /// least 30 degrees.
    /// </summary>
    /// <since>7.0</since>
    public static SubDCreationOptions InteriorCreaseAtMeshCrease
    {
      get
      {
        return new SubDCreationOptions(1);
      }
    }

    /// <summary>
    /// Create an interior sub-D crease along all coincident input mesh edges.
    /// </summary>
    /// <since>7.0</since>
    public static SubDCreationOptions InteriorCreaseAtMeshEdge
    {
      get
      {
        return new SubDCreationOptions(2);
      }
    }

    /// <summary>
    /// Look for convex corners at sub-D vertices with 2 edges that have an
    /// included angle &lt;= 90 degrees.
    /// </summary>
    /// <since>7.0</since>
    public static SubDCreationOptions ConvexCornerAtMeshCorner
    {
      get
      {
        return new SubDCreationOptions(3);
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
    /// how interior creases are treated
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
    /// how convex corners are treated
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
    /// When the interior crease option is AtMeshCreases the value of
    /// MinimumCreaseAngleRadians determines which coincident input mesh edges
    /// generate sub-D creases.
    /// If the input mesh has vertex normals, and the angle between vertex
    /// normals is &gt; MinimumCreaseAngleRadians at an end of a coincident
    /// input mesh edge, the corresponding sub-D edge will be a crease.
    /// </summary>
    /// <since>7.0</since>
    public double MinimumCreaseAngleRadians
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        return UnsafeNativeMethods.ON_ToSubDParameters_MinimumCreaseAngleRadians(const_ptr_this);
      }
      set
      {
        IntPtr ptr_this = NonConstPointer();
        UnsafeNativeMethods.ON_ToSubDParameters_SetMinimumCreaseAngleRadians(ptr_this, value);
      }
    }

    /// <summary>
    /// If ConvexCornerTest is at_mesh_corner, then an input mesh boundary
    /// vertex becomes a sub-D corner when the number of edges that end at the
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
    /// If ConvexCornerTest is at_mesh_corner, then an input mesh boundary
    /// vertex becomes a sub-D corner when the number of edges that end at the
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

#if RHINO_SDK
    /// <summary>
    /// If false, input mesh vertex locations will be used to set subd vertex control net locations.
    /// If true, input mesh vertex locations will be used to set subd vertex limit surface locations.
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

