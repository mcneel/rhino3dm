#pragma warning disable 1591

// OPENNURBS_SUBD_WIP is defined in builds with ON_SubD support.
// This define will be used to pull the sub-D project from Rhino 6 SR0 if
// it is not ready to be released.  The sub-D project will remain in all Rhino WIPs
// until it is officially released.
//
#if OPENNURBS_SUBD_WIP

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Globalization;
using Rhino.Runtime;
using System.Text;

// ReSharper disable once CheckNamespace
namespace Rhino.Geometry
{
  /// <summary>
  /// Represents a surface geometry type that is defined by a control net,
  /// crease information and a refinement level.
  /// <para>This is often called a subdivision surface.</para>
  /// <para>Warning: this object is exposed for early testing. It might change without notice.</para>
  /// </summary>
  [Serializable]
  partial class SubD : GeometryBase
  {
    IntPtr m_ptr_subd_ref = IntPtr.Zero; // ON_SubDRef*
    uint m_serial_number;
    private static int g_current_serial_number;

    internal IntPtr ON_SubDRef_Pointer()
    {
      return m_ptr_subd_ref;
    }

    /// <summary>Initializes a new empty SubD surface.</summary>
    public SubD()
    {
      m_serial_number = (uint)Interlocked.Increment(ref g_current_serial_number);

      m_ptr_subd_ref = UnsafeNativeMethods.ON_SubDRef_New();
      IntPtr ptr = UnsafeNativeMethods.ON_SubDRef_NewSubD(m_ptr_subd_ref);
      ConstructNonConstObject(ptr);
      ApplyMemoryPressure();
    }

    internal SubD(IntPtr nativePointer, object parent)
      : base(nativePointer, parent, -1)
    {
      if (null == parent)
      {
        m_serial_number = (uint)Interlocked.Increment(ref g_current_serial_number);
        m_ptr_subd_ref = UnsafeNativeMethods.ON_SubDRef_CreateAndAttach(nativePointer);
        ApplyMemoryPressure();
      }
      // 20 Aug 2015 - S. Baer
      // I'm not sure if we want to create an ON_SubDRef when the geometry has a RhinoObject
      // parent. This will cause the SubD to linger around in memory after the object has been
      // deleted from the document.
      //else if (parent is DocObjects.SubDObject)
      //{
      //  var rhsubd = parent as DocObjects.SubDObject;
      //  IntPtr ptr_subdobject = rhsubd.ConstPointer();
      //  m_ptr_subd_ref = UnsafeNativeMethods.CRhinoSubDObject_SubDRefCopy(ptr_subdobject);
      //}
    }

    internal uint RuntimeSerialNumber
    {
      get { return m_serial_number; }
    }

    protected override void Dispose(bool disposing)
    {
      base.Dispose(disposing);

      if (m_ptr_subd_ref == IntPtr.Zero)
        return;

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
          UnsafeNativeMethods.ON_SubDRef_Delete(m_ptr_subd_ref);
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
        UnsafeNativeMethods.ON_SubDRef_Delete(m_ptr_subd_ref);
      }
      m_ptr_subd_ref = IntPtr.Zero;
    }

    protected override void OnSwitchToNonConst()
    {
      base.OnSwitchToNonConst();
      m_serial_number = (uint)Interlocked.Increment(ref g_current_serial_number);
    }

    public static SubD CreateFromMesh(Mesh mesh, InteriorCreaseOption interiorCreaseOption)
    {
      if (mesh == null) throw new ArgumentNullException("mesh");

      var mesh_const_pointer = mesh.ConstPointer();
      IntPtr subd_ptr = UnsafeNativeMethods.ON_SubD_CreateFromMesh(mesh_const_pointer, (uint)interiorCreaseOption);
      GC.KeepAlive(mesh);

      return CreateGeometryHelper(subd_ptr, null) as SubD;
    }

    public void Subdivide(int meshDensity, SubDType subDType, int count)
    {
      Subdivide(meshDensity, subDType, -1, count);
    }

    public void Subdivide(int meshDensity, SubDType subDType, int levelIndex, int count)
    {
      if (meshDensity < 0) throw new ArgumentOutOfRangeException("meshDensity", "Must be >= 0.");
      if (subDType != SubDType.QuadCatmullClark)
        throw new ArgumentOutOfRangeException("subDType", "Must be CCQuad at present");
      if (levelIndex < 0) levelIndex = ActiveLevel;
      if (count <= 0) throw new ArgumentOutOfRangeException("count");

      var ptr_this = this.NonConstPointer();
      UnsafeNativeMethods.ON_SubD_Subdivide(ptr_this, subDType, (uint)levelIndex, (uint)count);
    }

    public Mesh ToLimitSurfaceMesh(int meshDensity)
    {
      if (meshDensity < 0) throw new ArgumentOutOfRangeException("meshDensity", "Must be >= 0.");
      return ToLimitSurfaceMesh(meshDensity, SubDType.QuadCatmullClark);
    }

    internal Mesh ToLimitSurfaceMesh(SubDDisplayParameters limitMeshParameters)
    {
      return ToLimitSurfaceMesh(limitMeshParameters.DisplayDensity, SubDType.QuadCatmullClark);
    }

    public Mesh ToLimitSurfaceMesh(int meshDensity, SubDType subDType)
    {
      if (meshDensity < 0) throw new ArgumentOutOfRangeException("meshDensity", "Must be >= 0.");
      if (subDType != SubDType.QuadCatmullClark)
        throw new ArgumentOutOfRangeException("subDType", "Must be CCQuad at present");

      var ptr_this = NonConstPointer();
      UnsafeNativeMethods.ON_SubD_SetSubdType(ptr_this, (uint)subDType);

      var ptr_mesh = UnsafeNativeMethods.ON_SubD_ToLimitSurfaceMesh(ptr_this, (uint)meshDensity);
      if (IntPtr.Zero == ptr_mesh) throw new InvalidOperationException("The subd operation failed to create a mesh.");

      return new Mesh(ptr_mesh, null);
    }

    public int UpdateAllTagsAndSectorCoefficients()
    {
      var ptr_this = this.NonConstPointer();
      var value = UnsafeNativeMethods.ON_SubD_UpdateAllTagsAndSectorCoefficients(ptr_this);
      return (int)value;
    }

    public void ClearEvaluationCache()
    {
      var ptr_this = this.NonConstPointer();
      UnsafeNativeMethods.ON_SubD_ClearEvaluationCache(ptr_this);
    }

    public int ActiveLevel
    {
      get
      {
        var const_ptr_this = ConstPointer();
        return (int)UnsafeNativeMethods.ON_SubD_GetActiveLevelIndex(const_ptr_this);
      }
    }

    public int Level
    {
      get
      {
        var const_ptr_this = ConstPointer();
        return (int)UnsafeNativeMethods.ON_SubD_Get_LevelCount(const_ptr_this);
      }
    }

    public SubDType ActiveLevelSubDType { get; set; }

    public bool SetSubDType(SubDType subDType)
    {
      var ptr_this = NonConstPointer();
      return UnsafeNativeMethods.ON_SubD_SetSubdType(ptr_this, (uint)subDType) != 0;
    }

    public static SubDType SubDTypeFromInt(int subdTypeAsInt)
    {
      if (!Enum.IsDefined(typeof(SubDType), (byte)subdTypeAsInt))
        return SubDType.Unset;
      return (SubDType)subdTypeAsInt;
    }

    internal void GetLimitSurfaceInPatches(SubDDisplayParameters limitSurfaceDisplayParameters, PatchGetter patchGetter)
    {
      throw new NotImplementedException("The C++ SDK was not finalized regarding this subtask.");
    }

    private Collections.SubDVertexAccess m_vertices;
    /// <summary>
    /// Gets access to vertices.
    /// </summary>
    internal Collections.SubDVertexAccess Vertices
    {
      get { return m_vertices ?? (m_vertices = new Collections.SubDVertexAccess(this)); }
    }

    private Collections.SubDEdgeAccess m_edges;
    /// <summary>
    /// Gets access to edges.
    /// </summary>
    internal Collections.SubDEdgeAccess Edges
    {
      get { return m_edges ?? (m_edges = new Collections.SubDEdgeAccess(this)); }
    }

    private Collections.SubDFaceAccess m_faces;
    /// <summary>
    /// Gets access to faces.
    /// </summary>
    internal Collections.SubDFaceAccess Faces
    {
      get { return m_faces ?? (m_faces = new Collections.SubDFaceAccess(this)); }
    }

    internal static bool IsSubDVertexTagDefined(SubDVertexTag tag)
    {
      return tag > SubDVertexTag.Unset && tag <= SubDVertexTag.Dart;
    }

    internal static bool IsSubDEdgeTagDefined(SubDEdgeTag tag)
    {
      return tag > SubDEdgeTag.Unset && tag <= SubDEdgeTag.X;
    }

    internal static bool IsSubDTypeDefined(SubDType type)
    {
      return type >= SubDType.TriLoopWarren && type <= SubDType.Custom;
    }

    internal bool RemoveComponentsAndConnections(ComponentIndex[] componentIndices)
    {
      var ptr = NonConstPointer();

      bool rc = UnsafeNativeMethods.ON_SubD_RemoveComponents(
        ptr, componentIndices, (uint)componentIndices.Length);

      return rc;
    }

    internal bool RemoveComponent(ComponentIndex componentIndex)
    {
      var ptr = NonConstPointer();

      bool rc = UnsafeNativeMethods.ON_SubD_RemoveComponent(
          ptr, componentIndex);

      return rc;
    }
  }

  class PatchGetter
  {
    private SubD m_subd;
    private int m_nurbs_display_density;
    private RhinoDoc m_doc;

    public PatchGetter(SubD subd, int nurbsDisplayDensity, RhinoDoc doc)
    {
      m_subd = subd;
      m_nurbs_display_density = nurbsDisplayDensity;
      m_doc = doc;
    }

    public int SCount { get; private set; }

    public int XCount { get; private set; }
  }

  class SubDDisplayParameters
  {
    public int DisplayDensity { get; set; }

    public static SubDDisplayParameters CreateFromDisplayDensity(int nurbsDisplayDensity)
    {
      var new_item = new SubDDisplayParameters { DisplayDensity = nurbsDisplayDensity };
      return new_item;
    }
  }

  interface ISubDComponent
  {
    /// <summary>Points to the next level's vertex when this component
    /// has been subdivided using an algorithm like Catmull-Clark or Loop-Warren.</summary>
    SubDVertexPtr Below { get; set; }

    int Id { get; }

    bool GetSavedSubdivisionPoint(SubD.SubDType type, out Point3d pt);
    bool SetSavedSubdivisionPoint(SubD.SubDType type, Point3d pt);

    ComponentStatus Status { get; set; }

    SubD SubD { get; }


  }

  internal static class SubDComponentImplementation
  {
    public static SubDVertexPtr GetBelow(IntPtr ptr, SubD parent)
    {
      return new SubDVertexPtr(
        UnsafeNativeMethods.ON_SubD_ComponentBase_GetBelowVertex(ptr),
          parent
          );
    }

    public static void SetBelow(IntPtr ptr, SubD parent, IntPtr belowVertexPtr)
    {
      UnsafeNativeMethods.ON_SubD_ComponentBase_SetBelowVertex(ptr, belowVertexPtr);
    }

    public static int GetId(IntPtr ptr)
    {
      return (int)UnsafeNativeMethods.ON_SubD_ComponentBase_GetID(ptr);
    }

    public static bool GetSavedSubdivisionPoint(IntPtr ptr, SubD.SubDType type, out Point3d pt)
    {
      if (!SubD.IsSubDTypeDefined(type))
        throw new ArgumentOutOfRangeException("type");

      pt = new Point3d();
      var rc = UnsafeNativeMethods.ON_SubD_ComponentBase_GetSavedSubdivisionPoint(ptr, type, ref pt);
      return rc;
    }

    public static bool SetSavedSubdivisionPoint(IntPtr ptr, SubD.SubDType type, Point3d pt)
    {
      if (!SubD.IsSubDTypeDefined(type))
        throw new ArgumentOutOfRangeException("type");

      var rc = UnsafeNativeMethods.ON_SubD_ComponentBase_SetSavedSubdivisionPoint(ptr, type, pt);
      return rc;
    }

    internal static byte GetStatus(IntPtr ptr)
    {
      return (byte)UnsafeNativeMethods.ON_SubD_ComponentBase_GetStatus(ptr);
    }

    internal static void SetStatus(IntPtr ptr, byte value)
    {
      UnsafeNativeMethods.ON_SubD_ComponentBase_SetStatus(ptr, value);
    }
  }

  /// <summary>
  /// Efficiently manages a reference to a vertex in a SubD.
  /// <para>Contains connection information to faces and edges.</para>
  /// </summary>
  struct SubDVertexPtr : IEquatable<SubDVertexPtr>, ISubDComponent
  {
    private IntPtr m_vertex_ptr;

    private readonly SubD m_parent;
    private uint m_parent_serial_number;
    private readonly ComponentIndex m_component_index;

    internal SubDVertexPtr(IntPtr fromPtr, SubD parent)
    {
      m_vertex_ptr = fromPtr;

      m_parent = parent;
      m_parent_serial_number = parent.RuntimeSerialNumber;
      m_component_index = new ComponentIndex();

      if (m_parent_serial_number == 0)
      {
        // store component index so we can get at the different pointer in the
        // case when the SubD get copied out of the document
        UnsafeNativeMethods.ON_SubDVertex_ComponentIndex(m_vertex_ptr, ref m_component_index);
      }
    }

    public static SubDVertexPtr FromSubDAndId(SubD subD, int id)
    {
      var const_subd_pointer = subD.ConstPointer();
      var vrtx_ptr = UnsafeNativeMethods.ON_SubDVertex_FromId(const_subd_pointer, (uint)id);
      GC.KeepAlive(subD);
      return vrtx_ptr == IntPtr.Zero ? new SubDVertexPtr() : new SubDVertexPtr(vrtx_ptr, subD);
    }

    // ReSharper disable once UnassignedReadonlyField
    public static readonly SubDVertexPtr Default;

    #region maintenance and exceptions

    internal bool IsDefault
    {
      get
      {
        if (m_vertex_ptr == IntPtr.Zero) return true;

        PointerUpdate(); //this can still lead to blanking out the vertex pointer.
        return m_vertex_ptr == IntPtr.Zero;
      }
    }

    internal IntPtr ConstPointer()
    {
      ZeroPtrCheck();

      if (PointerUpdate())
        ZeroPtrCheck();

      return m_vertex_ptr;
    }

    internal IntPtr ConstPointerOrNull()
    {
      if (m_vertex_ptr != IntPtr.Zero)
        PointerUpdate();

      return m_vertex_ptr;
    }

    internal IntPtr NonConstPointer()
    {
      ZeroPtrCheck();

      // This will cause the parent to become non-const if it is in the document.
      // Once the parent is "non-const" the following function is very fast.
      m_parent.NonConstPointer();

      if (PointerUpdate()) ZeroPtrCheck();

      return m_vertex_ptr;
    }

    bool PointerUpdate()
    {
      bool rc = false;

      if (m_parent_serial_number != m_parent.RuntimeSerialNumber)
      {
        // reattach m_vertex_ptr in the new SubD based on the component index.
        IntPtr const_ptr_subd = m_parent.ConstPointer();
        m_vertex_ptr = UnsafeNativeMethods.ON_SubDVertex_FromComponentIndex(const_ptr_subd, m_component_index);
        m_parent_serial_number = m_parent.RuntimeSerialNumber;
        rc = true;
      }

      return rc;
    }

    private void ZeroPtrCheck()
    {
      if (this.m_vertex_ptr == IntPtr.Zero)
      {
        throw new InvalidOperationException("Reference not set in instance of default vertex.");
      }
    }

    #endregion

    /// <summary>
    /// Links to previous vertex on this level.
    /// </summary>
    public SubDVertexPtr Previous
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        IntPtr const_ptr_prev = UnsafeNativeMethods.ON_SubDVertex_GetPrev(const_ptr_this);
        return new SubDVertexPtr(const_ptr_prev, m_parent);
      }
    }

    /// <summary>
    /// Links to next vertex on this level.
    /// </summary>
    public SubDVertexPtr Next
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        IntPtr const_ptr_next = UnsafeNativeMethods.ON_SubDVertex_GetNext(const_ptr_this);
        return new SubDVertexPtr(const_ptr_next, m_parent);
      }
    }

    public Point3d Location
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();

        Point3d location = default(Point3d);
        UnsafeNativeMethods.ON_SubDVertex_GetLocation(const_ptr_this, ref location);

        return location;
      }
      set
      {
        IntPtr ptr_this = NonConstPointer();
        UnsafeNativeMethods.ON_SubDVertex_SetLocation(ptr_this, value);
      }
    }

    public int EdgeCount
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();

        return UnsafeNativeMethods.ON_SubDVertex_EdgeCount(const_ptr_this);
      }
    }

    public int EdgesTaggedCount(SubD.SubDEdgeTag edgeTag)
    {
      IntPtr const_ptr_this = ConstPointer();

      if (!SubD.IsSubDEdgeTagDefined(edgeTag))
        throw new ArgumentOutOfRangeException("edgeTag");

      return UnsafeNativeMethods.ON_SubDVertex_EdgeCount2(const_ptr_this, edgeTag);
    }

    public SubDEdgePtr EdgeAt(int index)
    {
      bool _;
      return EdgeAt(index, out _);
    }

    public bool EdgeFlipAt(int index)
    {
      bool rc;
      EdgeAt(index, out rc);
      return rc;
    }

    public SubDEdgePtr EdgeAt(int index, out bool flipped)
    {
      IntPtr const_ptr_this = ConstPointer();

      if (index < 0) throw new ArgumentOutOfRangeException("index", "Index cannot be negative.");

      flipped = false;
      var rc = UnsafeNativeMethods.ON_SubDVertex_EdgeAt2(const_ptr_this, (uint)index, ref flipped);
      if (rc == IntPtr.Zero)
      {
        if (index >= EdgeCount) throw new
          ArgumentOutOfRangeException("index", "Index is higher or equal to EdgeCount");

        throw new NotSupportedException("Edge retrieval failed. This is a RhinoCommon library error.");
      }

      return new SubDEdgePtr(rc, m_parent);
    }

    public IEnumerable<SubDEdgePtr> EdgesOrdered
    {
      get
      {
        var sector_iterator = this.GetSectorIterator();
        if (sector_iterator.IsValid)
        {
          var first = sector_iterator.CurrentEdgeCcwPrev;
          yield return first;
          var old_next = sector_iterator.CurrentEdgeCcwNext;
          yield return old_next;

          while (sector_iterator.IncrementFace() && sector_iterator.FirstFace != sector_iterator.CurrentFace)
          {
            var prev = sector_iterator.CurrentEdgeCcwPrev;
            if (prev != old_next)
            {
              yield return prev;
            }

            var next = sector_iterator.CurrentEdgeCcwNext;
            if (next == first)
            {
              yield break; // yield break;
            }

            yield return next;
            old_next = next;
          }
        }
        sector_iterator.Dispose();
      }
    }

    public IEnumerable<SubDEdgeDirPair> Edges
    {
      get
      {
        int ec = this.EdgeCount;
        for (int i = 0; i < ec; i++)
        {
          bool flipped;
          var value = EdgeAt(i, out flipped);
          yield return new SubDEdgeDirPair(value, flipped);
        }
      }
    }

    public void AddEdge(SubDEdgePtr edge, bool flip)
    {
      IntPtr ptr_subd = m_parent.NonConstPointer();
      IntPtr ptr_vrtex = NonConstPointer();
      IntPtr ptr_face = edge.ConstPointer();

      if (!UnsafeNativeMethods.ON_SubDVertex_AddEdge(ptr_subd, ptr_vrtex, ptr_face, flip))
      {
        throw new InvalidOperationException("Addition of vertex to face connection failed.");
      }
    }

    public void RemoveEdge(SubDEdgePtr edge)
    {
      m_parent.NonConstPointer();
      IntPtr ptr_vrtex = NonConstPointer();
      IntPtr ptr_face = edge.ConstPointer();

      if (!UnsafeNativeMethods.ON_SubDVertex_RemoveEdge(ptr_vrtex, ptr_face))
      {
        throw new KeyNotFoundException("edge was not found in vertex.");
      }
    }

    public int FaceCount
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();

        return UnsafeNativeMethods.ON_SubDVertex_FaceCount(const_ptr_this);
      }
    }

    public SubDFacePtr FaceAt(int index)
    {
      IntPtr const_ptr_this = ConstPointer();

      if (index < 0) throw new ArgumentOutOfRangeException("index", "Index cannot be negative.");

      var rc = UnsafeNativeMethods.ON_SubDVertex_FaceAt(const_ptr_this, (uint)index);
      if (rc == IntPtr.Zero)
      {
        if (index >= FaceCount) throw new
          ArgumentOutOfRangeException("index", "Index is higher or equal to FaceCount");

        throw new NotSupportedException("Face retrieval failed. This is a RhinoCommon library error.");
      }

      return new SubDFacePtr(rc, m_parent);
    }

    public IEnumerable<SubDFacePtr> Faces
    {
      get
      {
        int ec = FaceCount;
        for (int i = 0; i < ec; i++)
          yield return FaceAt(i);
      }
    }

    public IEnumerable<SubDFacePtr> FacesOrdered
    {
      get
      {
        var sector_iterator = this.GetSectorIterator();
        if (sector_iterator.IsValid)
        {
          yield return sector_iterator.CurrentFace;

          while (sector_iterator.IncrementFace() && sector_iterator.FirstFace != sector_iterator.CurrentFace)
          {
            yield return sector_iterator.CurrentFace;
          }
        }
        sector_iterator.Dispose();
      }
    }

    public void AddFace(SubDFacePtr face)
    {
      IntPtr ptr_subd = m_parent.NonConstPointer();
      IntPtr ptr_vrtex = NonConstPointer();
      IntPtr ptr_face = face.ConstPointer();

      if (!UnsafeNativeMethods.ON_SubDVertex_AddFace(ptr_subd, ptr_vrtex, ptr_face))
      {
        throw new InvalidOperationException("Addition of vertex to face connection failed.");
      }
    }

    public int Id
    {
      get
      {
        var const_ptr = ConstPointer();
        return SubDComponentImplementation.GetId(const_ptr);
      }
    }

    public bool GetSavedSubdivisionPoint(SubD.SubDType type, out Point3d pt)
    {
      var const_ptr = ConstPointer();
      return SubDComponentImplementation.GetSavedSubdivisionPoint(const_ptr, type, out pt);
    }

    public bool SetSavedSubdivisionPoint(SubD.SubDType type, Point3d pt)
    {
      var ptr = NonConstPointer();
      return SubDComponentImplementation.SetSavedSubdivisionPoint(ptr, type, pt);
    }

    public SubDVertexPtr Below
    {
      get
      {
        var const_ptr = ConstPointer();
        return SubDComponentImplementation.GetBelow(const_ptr, m_parent);
      }
      set
      {
        var ptr = NonConstPointer();
        var new_ptr = value.ConstPointerOrNull();
        SubDComponentImplementation.SetBelow(ptr, m_parent, new_ptr);
      }
    }

    public ComponentStatus Status
    {
      get
      {
        var const_ptr = ConstPointer();

        var cs = SubDComponentImplementation.GetStatus(const_ptr);
        return new ComponentStatus(cs);
      }
      set
      {
        var ptr = NonConstPointer();

        SubDComponentImplementation.SetStatus(ptr, value.PrivateBytes());
      }
    }

    public SubD SubD
    {
      get
      {
        return m_parent;
      }
    }

    #region Equality and object overrides

    public override bool Equals(object obj)
    {
      return obj is SubDVertexPtr && Equals((SubDVertexPtr)obj);
    }

    public bool Equals(SubDVertexPtr other)
    {
      return this == other;
    }

    public static bool operator ==(SubDVertexPtr a, SubDVertexPtr b)
    {
      if (a.m_vertex_ptr != IntPtr.Zero) a.PointerUpdate();
      if (b.m_vertex_ptr != IntPtr.Zero) b.PointerUpdate();

      // Logically, the same vertex cannot belog to two subDs,
      // so we skip that check.
      return a.m_vertex_ptr == b.m_vertex_ptr;
    }

    public static bool operator !=(SubDVertexPtr a, SubDVertexPtr b)
    {
      if (a.m_vertex_ptr != IntPtr.Zero) a.PointerUpdate();
      if (b.m_vertex_ptr != IntPtr.Zero) b.PointerUpdate();

      // Logically, the same vertex cannot belog to two subDs,
      // so we skip that check.
      return a.m_vertex_ptr != b.m_vertex_ptr;
    }

    public override int GetHashCode()
    {
      return m_vertex_ptr.GetHashCode();
    }

    public override string ToString()
    {
      if (this == Default) return "Default vertex";

      return string.Format(
        "{0} {1}", Id.ToString(CultureInfo.InvariantCulture), Location.ToString());
    }

    #endregion

    public void ClearEdges()
    {
      var ptr = this.NonConstPointer();
      UnsafeNativeMethods.ON_SubDVertex_ClearEdges(ptr);
    }

    public void ClearFaces()
    {
      var ptr = this.NonConstPointer();
      UnsafeNativeMethods.ON_SubDVertex_ClearFaces(ptr);
    }

    public bool IsTwoManifold
    {
      get
      {
        foreach (var edge in Edges)
        {
          if (edge.Edge.FaceCount != 2) return false;
        }
        return true;
      }
    }

    public SubDVertexSectorIterator GetSectorIterator()
    {
      return GetSectorIterator(SubDFacePtr.Default, true);
    }

    public SubDVertexSectorIterator GetSectorIterator(SubDFacePtr face, bool counterClockWise)
    {
      return new SubDVertexSectorIterator(m_parent, this, face, counterClockWise);
    }

    public SubD.SubDVertexTag Tag
    {
      get
      {
        var ptr_this = this.ConstPointer();
        var new_value = UnsafeNativeMethods.ON_SubDVertex_GetTag(ptr_this);
        return new_value;
      }
      set
      {
        var ptr_this = this.NonConstPointer();
        UnsafeNativeMethods.ON_SubDVertex_SetTag(ptr_this, value);
      }
    }
  }

  sealed class SubDVertexSectorIterator : ICloneable
  {
    private readonly SubD m_parent;
    private readonly IntPtr m_native_sector_ptr;

    internal SubDVertexSectorIterator(SubD parent, SubDVertexPtr vertex, bool ccw) :
      this(parent, vertex, SubDFacePtr.Default, ccw)
    {
    }

    internal SubDVertexSectorIterator(SubD parent, SubDVertexPtr vertex, SubDFacePtr face, bool ccw)
    {
      if (parent == null) throw new ArgumentNullException("parent");
      m_parent = parent;

      var v_cp = vertex.ConstPointer();
      var f_cp = face.ConstPointerOrNull();

      m_native_sector_ptr = UnsafeNativeMethods.ON_SubDVertex_SectorIterator_New(v_cp, f_cp, ccw);

      if (m_native_sector_ptr == IntPtr.Zero)
        throw new ArgumentException("SubDVertexSectorIterator cannot be instantiated on this vertex, with this face.");
    }

    internal SubDVertexSectorIterator(SubD parent, IntPtr nativeSectorPtr)
    {
      if (parent == null) throw new ArgumentNullException("parent");
      m_parent = parent;
      m_native_sector_ptr = nativeSectorPtr;
    }

    public SubDVertexPtr CenterVertex
    {
      get
      {
        var center = UnsafeNativeMethods.ON_SubDVertex_SectorIterator_CenterVertex(m_native_sector_ptr);
        var subd_ptr = m_parent.ConstPointer();
        return new SubDVertexPtr();
      }
    }

    public SubDEdgePtr CurrentEdgeCcwNext
    {
      get
      {
        var edge = UnsafeNativeMethods.ON_SubDVertex_SectorIterator_CurrentEdge(m_native_sector_ptr, true);
        return new SubDEdgePtr(edge, m_parent);
      }
    }

    public bool CurrentEdgeCcwNextFlipped
    {
      get
      {
        var flipped = UnsafeNativeMethods.ON_SubDVertex_SectorIterator_CurrentEdgeFlipped(m_native_sector_ptr, true);
        return flipped;
      }
    }

    public SubDEdgePtr CurrentEdgeCcwPrev
    {
      get
      {
        var edge = UnsafeNativeMethods.ON_SubDVertex_SectorIterator_CurrentEdge(m_native_sector_ptr, false);
        return new SubDEdgePtr(edge, m_parent);
      }
    }

    public bool CurrentEdgeCcwPrevFlipped
    {
      get
      {
        var flipped = UnsafeNativeMethods.ON_SubDVertex_SectorIterator_CurrentEdgeFlipped(m_native_sector_ptr, false);
        return flipped;
      }
    }

    public SubDFacePtr CurrentFace
    {
      get
      {
        var face_ptr = UnsafeNativeMethods.ON_SubDVertex_SectorIterator_CurrentFace(m_native_sector_ptr);
        return new SubDFacePtr(face_ptr, m_parent);
      }
    }

    public bool CurrentFaceFlipped
    {
      get
      {
        var flipped = UnsafeNativeMethods.ON_SubDVertex_SectorIterator_CurrentFaceFlipped(m_native_sector_ptr);
        return flipped;
      }
    }

    public int CurrentFaceCenterVertexIndex
    {
      get
      {
        var index = UnsafeNativeMethods.ON_SubDVertex_SectorIterator_CurrentFaceCenterVertexIndex(m_native_sector_ptr);
        return (int)index;
      }
    }

    public int CurrentRingIndex
    {
      get
      {
        var index = UnsafeNativeMethods.ON_SubDVertex_SectorIterator_CurrentRingIndex(m_native_sector_ptr);
        return index;
      }
    }

    public SubDFacePtr FirstFace
    {
      get
      {
        var face_ptr = UnsafeNativeMethods.ON_SubDVertex_SectorIterator_InitialFace(m_native_sector_ptr);
        return new SubDFacePtr(face_ptr, m_parent);
      }
    }

    public int FirstFaceCenterVertexIndex
    {
      get
      {
        var index = UnsafeNativeMethods.ON_SubDVertex_SectorIterator_InitialFaceCenterVertexIndex(m_native_sector_ptr);
        return (int)index;
      }
    }

    public bool IsValid
    {
      get
      {
        var valid = UnsafeNativeMethods.ON_SubDVertex_SectorIterator_IsValid(m_native_sector_ptr);
        return valid;
      }
    }

    public bool Reset(SubDVertexPtr vertex, SubDFacePtr face, bool ccw)
    {
      return UnsafeNativeMethods.ON_SubDVertex_SectorIterator_Initialize(
        m_native_sector_ptr, vertex.ConstPointer(), face.ConstPointer(), ccw);
    }

    public bool ResetToCurrentFace(SubDVertexPtr vertex)
    {
      return UnsafeNativeMethods.ON_SubDVertex_SectorIterator_InitializeToCurrentFace(
        m_native_sector_ptr);
    }

    public bool ResetToFirstFace()
    {
      var reset = UnsafeNativeMethods.ON_SubDVertex_SectorIterator_FirstFace(m_native_sector_ptr);
      return reset;
    }

    public bool IncrementFace(bool ccwForward, bool stopAtCrease)
    {
      var incremented = UnsafeNativeMethods.ON_SubDVertex_SectorIterator_IncrementFace(
        m_native_sector_ptr, ccwForward ? 1 : 0, stopAtCrease);
      return incremented;
    }

    public bool IncrementFace()
    {
      return this.IncrementFace(true, false);
    }

    public bool IncrementFaceToCrease(bool ccwForward)
    {
      var incremented = UnsafeNativeMethods.ON_SubDVertex_SectorIterator_IncrementToCrease(
        m_native_sector_ptr, ccwForward ? 1 : 0);
      return incremented;
    }

    public bool AdvanceToNextFace(bool stopAtCrease)
    {
      var next_face = UnsafeNativeMethods.ON_SubDVertex_SectorIterator_NextFace(m_native_sector_ptr, stopAtCrease);
      return next_face != IntPtr.Zero;
    }

    public bool RecedeToPreviousFace(bool stopAtCrease)
    {
      var next_face = UnsafeNativeMethods.ON_SubDVertex_SectorIterator_PrevFace(m_native_sector_ptr, stopAtCrease);
      return next_face != IntPtr.Zero;
    }

    public void Dispose()
    {
      this.Dispose(true);
      GC.SuppressFinalize(this);
    }

    ~SubDVertexSectorIterator()
    {
      Dispose(false);
    }

    private void Dispose(bool disposing)
    {
      UnsafeNativeMethods.ON_SubDVertex_SectorIterator_Delete(m_native_sector_ptr);
    }

    object ICloneable.Clone()
    {
      return Duplicate();
    }

    public SubDVertexSectorIterator Duplicate()
    {
      var ptr = UnsafeNativeMethods.ON_SubDVertex_SectorIterator_Duplicate(this.m_native_sector_ptr);
      var si = new SubDVertexSectorIterator(m_parent, ptr);
      return si;
    }
  }

  /// <summary>
  /// Efficiently manages a reference to an edge in a SubD.
  /// <para>Contains connection information to faces and vertices.</para>
  /// </summary>
  struct SubDEdgePtr : IEquatable<SubDEdgePtr>, ISubDComponent
  {
    public static readonly double UnsetSectorWeight = 8883.0;

    private IntPtr m_edge_ptr;

    private readonly SubD m_parent;
    private uint m_parent_serial_number;
    private readonly ComponentIndex m_component_index;

    // ReSharper disable once UnassignedReadonlyField
    public readonly static SubDEdgePtr Default;

    internal SubDEdgePtr(IntPtr fromEdgePtr, SubD parent)
    {
      m_edge_ptr = fromEdgePtr;

      m_parent = parent;
      m_parent_serial_number = parent.RuntimeSerialNumber;
      m_component_index = new ComponentIndex();

      if (m_parent_serial_number == 0)
      {
        UnsafeNativeMethods.ON_SubDEdge_ComponentIndex(m_edge_ptr, ref m_component_index);
      }
    }

    internal static SubDEdgePtr FromSubDAndId(SubD subD, int id)
    {
        var const_subd_pointer = subD.ConstPointer();
        var edge_ptr = UnsafeNativeMethods.ON_SubDEdge_FromId(const_subd_pointer, (uint)id);
        GC.KeepAlive(subD);
        return edge_ptr == IntPtr.Zero ? new SubDEdgePtr() : new SubDEdgePtr(edge_ptr, subD);
    }

    #region maintenance and exceptions
    //See SubDVertexPtr for specifications

    internal bool IsDefault
    {
      get
      {
        PointerUpdate();
        return m_edge_ptr == IntPtr.Zero;
      }
    }

    internal IntPtr ConstPointer()
    {
      ZeroPtrCheck();

      if (PointerUpdate())
        ZeroPtrCheck();

      return m_edge_ptr;
    }

    internal IntPtr ConstPointerOrNull()
    {
      if (m_edge_ptr != IntPtr.Zero)
        PointerUpdate();

      return m_edge_ptr;
    }

    internal IntPtr NonConstPointer()
    {
      ZeroPtrCheck();
      m_parent.NonConstPointer();

      if (PointerUpdate())
        ZeroPtrCheck();

      return m_edge_ptr;
    }

    bool PointerUpdate()
    {
      bool rc = false;

      if (m_parent_serial_number != m_parent.RuntimeSerialNumber)
      {
        IntPtr const_ptr_subd = m_parent.ConstPointer();
        m_edge_ptr = UnsafeNativeMethods.ON_SubDEdge_FromComponentIndex(const_ptr_subd, m_component_index);
        m_parent_serial_number = m_parent.RuntimeSerialNumber;
        rc = true;
      }

      return rc;
    }

    private void ZeroPtrCheck()
    {
      if (this.m_edge_ptr == IntPtr.Zero)
      {
        throw new InvalidOperationException("Reference not set in instance of default edge.");
      }
    }

    #endregion

    public SubDVertexPtr VertexFrom
    {
      get
      {
        var const_pointer = this.ConstPointer();
        var from = UnsafeNativeMethods.ON_SubDEdge_GetFrom(const_pointer);
        return new SubDVertexPtr(from, m_parent);
      }
      set
      {
        var ptr = NonConstPointer();
        var v0_const_ptr = value.ConstPointerOrNull();
        UnsafeNativeMethods.ON_SubDEdge_SetFrom(ptr, v0_const_ptr);
      }
    }

    public SubDVertexPtr VertexTo
    {
      get
      {
        var const_pointer = this.ConstPointer();
        var from = UnsafeNativeMethods.ON_SubDEdge_GetTo(const_pointer);
        return new SubDVertexPtr(from, m_parent);
      }
      set
      {
        var ptr = NonConstPointer();
        var v1_const_ptr = value.ConstPointerOrNull();
        UnsafeNativeMethods.ON_SubDEdge_SetTo(ptr, v1_const_ptr);
      }
    }

    public SubDVertexPtr OtherEnd(SubDVertexPtr fromOrTo)
    {
      if (fromOrTo == VertexFrom) return VertexTo;
      if (fromOrTo == VertexTo) return VertexFrom;
      throw new ArgumentException("Parameter is neither From not To vertex.", "fromOrTo");
    }

    internal SubDVertexPtr GetVertex(bool start)
    {
      return start ? this.VertexFrom : this.VertexTo;
    }

    internal void SetVertex(bool start, SubDVertexPtr vertex)
    {
      if (start)
      {
        this.VertexFrom = vertex;
      }
      else
      {
        this.VertexTo = vertex;
      }
    }

    public Line Line
    {
      get
      {
        return new Line(VertexFrom.Location, VertexTo.Location);
      }
      set
      {
        var from = VertexFrom;
        from.Location = value.From;
        var to = VertexTo;
        to.Location = value.To;
      }
    }

    public SubD.SubDEdgeTag Tag
    {
      get
      {
        var const_pointer = this.ConstPointer();
        return UnsafeNativeMethods.ON_SubDEdge_GetEdgeTag(const_pointer);
      }
      set
      {
        var ptr = NonConstPointer();
        if (!SubD.IsSubDEdgeTagDefined(value))
          throw new ArgumentOutOfRangeException("value");
        UnsafeNativeMethods.ON_SubDEdge_SetEdgeTag(ptr, value);
      }
    }

    public SubDFacePtr FaceAt(int index)
    {
      IntPtr const_ptr_this = ConstPointer();

      if (index < 0) throw new ArgumentOutOfRangeException("index", "Index cannot be negative.");

      var rc = UnsafeNativeMethods.ON_SubDEdge_FaceAt(const_ptr_this, (uint)index);
      if (rc == IntPtr.Zero)
      {
        if (index >= FaceCount) throw new
          ArgumentOutOfRangeException("index", "Index is higher or equal to FaceCount");

        throw new NotSupportedException("Face retrieval failed. This is a RhinoCommon library error.");
      }

      return new SubDFacePtr(rc, m_parent);
    }

    public int FaceCount
    {
      get
      {
        var const_ptr = this.ConstPointer();

        return UnsafeNativeMethods.ON_SubDEdge_FaceCount(const_ptr);
      }
    }

    internal void AddFace(SubDFacePtr face, bool dir)
    {
      var subd_ptr = this.m_parent.NonConstPointer();
      var edge_ptr = this.NonConstPointer();
      var face_ptr = this.ConstPointer();

      if (!UnsafeNativeMethods.ON_SubDEdge_AddFace(subd_ptr, edge_ptr, face_ptr, dir))
      {
        throw new InvalidOperationException("Addition of edge to face connection failed.");
      }
    }

    public SubDVertexPtr Below
    {
      get
      {
        var const_ptr = ConstPointer();
        return SubDComponentImplementation.GetBelow(const_ptr, m_parent);
      }
      set
      {
        var ptr = NonConstPointer();
        var new_ptr = value.ConstPointerOrNull();
        SubDComponentImplementation.SetBelow(ptr, m_parent, new_ptr);
      }
    }

    /// <summary>
    /// Links to previous edge on this level.
    /// </summary>
    public SubDEdgePtr Previous
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        IntPtr const_ptr_next = UnsafeNativeMethods.ON_SubDEdge_GetPrev(const_ptr_this);
        return new SubDEdgePtr(const_ptr_next, m_parent);
      }
    }

    /// <summary>
    /// Links to next edge on this level.
    /// </summary>
    public SubDEdgePtr Next
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        IntPtr const_ptr_next = UnsafeNativeMethods.ON_SubDEdge_GetNext(const_ptr_this);
        return new SubDEdgePtr(const_ptr_next, m_parent);
      }
    }

    public int Id
    {
      get
      {
        return SubDComponentImplementation.GetId(m_edge_ptr);
      }
    }

    public bool GetSavedSubdivisionPoint(SubD.SubDType type, out Point3d pt)
    {
      var const_ptr = ConstPointer();
      return SubDComponentImplementation.GetSavedSubdivisionPoint(const_ptr, type, out pt);
    }

    public bool SetSavedSubdivisionPoint(SubD.SubDType type, Point3d pt)
    {
      var ptr = NonConstPointer();
      return SubDComponentImplementation.SetSavedSubdivisionPoint(ptr, type, pt);
    }

    public ComponentStatus Status
    {
      get
      {
        var const_ptr = ConstPointer();

        var cs = SubDComponentImplementation.GetStatus(const_ptr);
        return new ComponentStatus(cs);
      }
      set
      {
        var ptr = NonConstPointer();

        SubDComponentImplementation.SetStatus(ptr, value.PrivateBytes());
      }
    }

    public SubD SubD
    {
      get
      {
        return m_parent;
      }
    }

    public double SectorCoefficientFrom
    {
      get
      {
        var const_pointer = this.ConstPointer();
        return UnsafeNativeMethods.ON_SubDEdge_GetSectorCoefficient(const_pointer, 0);
      }
      set
      {
        var ptr = NonConstPointer();
        UnsafeNativeMethods.ON_SubDEdge_SetSectorCoefficient(ptr, 0, value);
      }
    }

    public double SectorCoefficientTo
    {
      get
      {
        var const_pointer = this.ConstPointer();
        return UnsafeNativeMethods.ON_SubDEdge_GetSectorCoefficient(const_pointer, 1);
      }
      set
      {
        var ptr = NonConstPointer();
        UnsafeNativeMethods.ON_SubDEdge_SetSectorCoefficient(ptr, 1, value);
      }
    }

    public void ClearFaces()
    {
      var ptr = this.NonConstPointer();
      UnsafeNativeMethods.ON_SubDEdge_ClearFaces(ptr);
    }

    #region Equality and object overrides

    public override bool Equals(object obj)
    {
      return obj is SubDEdgePtr && Equals((SubDEdgePtr)obj);
    }

    public bool Equals(SubDEdgePtr other)
    {
      return this == other;
    }

    public static bool operator ==(SubDEdgePtr a, SubDEdgePtr b)
    {
      if (a.m_edge_ptr != IntPtr.Zero) a.PointerUpdate();
      if (b.m_edge_ptr != IntPtr.Zero) b.PointerUpdate();

      // Logically, the same vertex cannot belog to two subDs,
      // so we skip that check.
      return a.m_edge_ptr == b.m_edge_ptr;
    }

    public static bool operator !=(SubDEdgePtr a, SubDEdgePtr b)
    {
      if (a.m_edge_ptr != IntPtr.Zero) a.PointerUpdate();
      if (b.m_edge_ptr != IntPtr.Zero) b.PointerUpdate();

      // Logically, the same vertex cannot belog to two subDs,
      // so we skip that check.
      return a.m_edge_ptr != b.m_edge_ptr;
    }

    public override int GetHashCode()
    {
      return m_edge_ptr.GetHashCode();
    }

    public override string ToString()
    {
      if (this == Default) return "Default edge";

      return string.Format("{0}: {1}->{2}", Id, VertexFrom.Id, VertexTo.Id);
    }

    #endregion
  }

  /// <summary>
  /// Efficiently manages a reference to a polygonal face in a SubD, with any number of sides.
  /// <para>Contains connection information to vertices and edges.</para>
  /// </summary>
  struct SubDFacePtr : IEquatable<SubDFacePtr>, ISubDComponent, IComparable<SubDFacePtr>
  {
    private IntPtr m_face_ptr;

    private readonly SubD m_parent;
    private uint m_parent_serial_number;
    private readonly ComponentIndex m_component_index;

    // ReSharper disable once UnassignedReadonlyField
    public readonly static SubDFacePtr Default;

    internal SubDFacePtr(IntPtr fromFacePtr, SubD parent)
    {
      m_face_ptr = fromFacePtr;

      m_parent = parent;
      m_parent_serial_number = parent.RuntimeSerialNumber;
      m_component_index = new ComponentIndex();

      if (m_parent_serial_number == 0)
      {
        UnsafeNativeMethods.ON_SubDFace_ComponentIndex(m_face_ptr, ref m_component_index);
      }
    }

    internal static SubDFacePtr FromSubDAndId(SubD subD, int id)
    {
        var const_subd_pointer = subD.ConstPointer();
        var face_ptr = UnsafeNativeMethods.ON_SubDFace_FromId(const_subd_pointer, (uint)id);
        GC.KeepAlive(subD);
        return face_ptr == IntPtr.Zero ? new SubDFacePtr() : new SubDFacePtr(face_ptr, subD);
    }

    #region maintenance and exceptions
    //See SubDVertexPtr for specifications

    public bool IsDefault
    {
      get
      {
        PointerUpdate();
        return IsDefaultNoUpdate;
      }
    }

    private bool IsDefaultNoUpdate
    {
      get
      {
        return this.m_face_ptr == IntPtr.Zero;
      }
    }

    internal IntPtr ConstPointer()
    {
      ZeroPtrCheck();

      if (PointerUpdate())
        ZeroPtrCheck();

      return m_face_ptr;
    }

    internal IntPtr ConstPointerOrNull()
    {
      if (m_face_ptr != IntPtr.Zero)
        PointerUpdate();

      return m_face_ptr;
    }

    internal IntPtr NonConstPointer()
    {
      ZeroPtrCheck();
      m_parent.NonConstPointer();

      if (PointerUpdate())
        ZeroPtrCheck();

      return m_face_ptr;
    }

    bool PointerUpdate()
    {
      bool rc = false;

      if (m_parent_serial_number != m_parent.RuntimeSerialNumber)
      {
        IntPtr const_ptr_subd = m_parent.ConstPointer();
        m_face_ptr = UnsafeNativeMethods.ON_SubDFace_FromComponentIndex(const_ptr_subd, m_component_index);
        m_parent_serial_number = m_parent.RuntimeSerialNumber;
        rc = true;
      }

      return rc;
    }

    private void ZeroPtrCheck()
    {
      if (this.m_face_ptr == IntPtr.Zero)
      {
        throw new InvalidOperationException("Reference not set in instance of default face.");
      }
    }

    #endregion

    /// <summary>
    /// Links to previous face on this level.
    /// </summary>
    public SubDFacePtr Previous
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        IntPtr const_ptr_next = UnsafeNativeMethods.ON_SubDFace_GetPrev(const_ptr_this);
        return new SubDFacePtr(const_ptr_next, m_parent);
      }
    }

    /// <summary>
    /// Links to next face on this level.
    /// </summary>
    public SubDFacePtr Next
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        IntPtr const_ptr_next = UnsafeNativeMethods.ON_SubDFace_GetNext(const_ptr_this);
        return new SubDFacePtr(const_ptr_next, m_parent);
      }
    }

    public SubDVertexPtr Below
    {
      get
      {
        var const_ptr = ConstPointer();
        return SubDComponentImplementation.GetBelow(const_ptr, m_parent);
      }
      set
      {
        var ptr = NonConstPointer();
        var new_ptr = value.ConstPointerOrNull();
        SubDComponentImplementation.SetBelow(ptr, m_parent, new_ptr);
      }
    }

    public int Id
    {
      get
      {
        ZeroPtrCheck();
        return SubDComponentImplementation.GetId(m_face_ptr);
      }
    }

    public bool GetSavedSubdivisionPoint(SubD.SubDType type, out Point3d pt)
    {
      var const_ptr = ConstPointer();
      return SubDComponentImplementation.GetSavedSubdivisionPoint(const_ptr, type, out pt);
    }

    public bool SetSavedSubdivisionPoint(SubD.SubDType type, Point3d pt)
    {
      var ptr = NonConstPointer();
      return SubDComponentImplementation.SetSavedSubdivisionPoint(ptr, type, pt);
    }

    public ComponentStatus Status
    {
      get
      {
        var const_ptr = ConstPointer();

        var cs = SubDComponentImplementation.GetStatus(const_ptr);
        return new ComponentStatus(cs);
      }
      set
      {
        var ptr = NonConstPointer();

        SubDComponentImplementation.SetStatus(ptr, value.PrivateBytes());
      }
    }

    public SubD SubD
    {
      get
      {
        return m_parent;
      }
    }

    public int EdgeVertexCount
    {
      get
      {
        var const_ptr = this.ConstPointer();
        return UnsafeNativeMethods.ON_SubDFace_EdgeVertexCount(const_ptr);
      }
    }

    public SubDVertexPtr VertexAt(int index)
    {
      var const_ptr = this.ConstPointer();
      var vrtx_ptr = UnsafeNativeMethods.ON_SubDFace_VertexAt(const_ptr, (uint)index);

      if (vrtx_ptr == IntPtr.Zero)
      {
        if (index < 0 || index > EdgeVertexCount)
          throw new ArgumentOutOfRangeException("index");
        throw new InvalidOperationException("SubDEdgePtr.VertexAt call failed unexpectedly.");
      }

      return new SubDVertexPtr(vrtx_ptr, m_parent);
    }

    public SubDEdgePtr EdgeAt(int index, out bool dir)
    {
      var const_ptr = this.ConstPointer();
      dir = default(bool);
      var edge_ptr = UnsafeNativeMethods.ON_SubDFace_EdgeAt(const_ptr, (uint)index, ref dir);

      if (edge_ptr == IntPtr.Zero)
      {
        if (index < 0 || index > EdgeVertexCount)
          throw new ArgumentOutOfRangeException("index");
        throw new InvalidOperationException("SubDEdgePtr.EdgeAt call failed unexpectedly.");
      }

      return new SubDEdgePtr(edge_ptr, m_parent);
    }

    public SubDEdgePtr EdgeAt(int index)
    {
      bool _;
      return EdgeAt(index, out _);
    }

    internal bool EdgeDirectionAt(int index)
    {
      bool dir;
      EdgeAt(index, out dir);

      return dir;
    }

    public IEnumerable<KeyValuePair<SubDEdgePtr, bool>> Edges
    {
      get
      {
        for (int i = 0; i < EdgeVertexCount; i++)
        {
          bool dir;
          var edge = this.EdgeAt(i, out dir);
          yield return new KeyValuePair<SubDEdgePtr, bool>(edge, dir);
        }
      }
    }

    public IEnumerable<SubDVertexPtr> Vertices
    {
      get
      {
        for (int i = 0; i < EdgeVertexCount; i++)
        {
          var vertex = this.VertexAt(i);
          yield return vertex;
        }
      }
    }

    public bool AddEdgeFaceConnection(SubDEdgePtr edge, bool flipped, int index)
    {
      var subd_ptr = m_parent.NonConstPointer();
      var edge_ptr = edge.NonConstPointer();
      var face_ptr = this.NonConstPointer();
      if (index < 0) throw new ArgumentOutOfRangeException("index");

      var rc = UnsafeNativeMethods.ON_SubD_AddEdgeFaceConnection(subd_ptr, edge_ptr, flipped, face_ptr, (uint)index);
      return rc;
    }

    public int IndexOf(SubDVertexPtr vertex)
    {
      var const_ptr_face = this.ConstPointer();
      var const_ptr_vertex = vertex.ConstPointer();
      return UnsafeNativeMethods.ON_SubDFace_VertexIndex(const_ptr_face, const_ptr_vertex);
    }

    public int IndexOf(SubDEdgePtr edge)
    {
      var const_ptr_face = this.ConstPointer();
      var const_ptr_edge = edge.ConstPointer();
      return UnsafeNativeMethods.ON_SubDFace_EdgeIndex(const_ptr_face, const_ptr_edge);
    }

    public void RemoveEdge(SubDEdgePtr edge)
    {
      var face_ptr = this.NonConstPointer();
      var edge_ptr = edge.ConstPointer();
      var rc = UnsafeNativeMethods.ON_SubDFace_RemoveEdge(face_ptr, edge_ptr);
      if (!rc) throw new ArgumentException("edge is not present, and or cannot be removed.", "edge");
    }

    public void RemoveEdge(int edgeInFaceIndex)
    {
      var face_ptr = this.NonConstPointer();
      if (edgeInFaceIndex < 0) throw new ArgumentOutOfRangeException("edgeInFaceIndex");

      var rc = UnsafeNativeMethods.ON_SubDFace_RemoveEdge2(face_ptr, (uint)edgeInFaceIndex);
      if (rc == IntPtr.Zero) throw new ArgumentException("edge is not present, and or cannot be removed.", "edgeInFaceIndex");
    }

    #region Equality and object overrides

    public override bool Equals(object obj)
    {
      return obj is SubDEdgePtr && Equals((SubDEdgePtr)obj);
    }

    public bool Equals(SubDFacePtr other)
    {
      return this == other;
    }

    public static bool operator ==(SubDFacePtr a, SubDFacePtr b)
    {
      if (a.m_face_ptr != IntPtr.Zero) a.PointerUpdate();
      if (b.m_face_ptr != IntPtr.Zero) b.PointerUpdate();

      // Logically, the same vertex cannot belog to two subDs,
      // so we skip that check.
      return a.m_face_ptr == b.m_face_ptr;
    }

    public static bool operator !=(SubDFacePtr a, SubDFacePtr b)
    {
      if (a.m_face_ptr != IntPtr.Zero) a.PointerUpdate();
      if (b.m_face_ptr != IntPtr.Zero) b.PointerUpdate();

      // Logically, the same vertex cannot belog to two subDs,
      // so we skip that check.
      return a.m_face_ptr != b.m_face_ptr;
    }

    public override int GetHashCode()
    {
      return m_face_ptr.GetHashCode();
    }

    public int CompareTo(SubDFacePtr obj)
    {
      if (IsDefaultNoUpdate)
      {
        if (obj.IsDefaultNoUpdate)
          return 0;
        return 1;
      }
      if (obj.IsDefaultNoUpdate) return -1;

      return Id.CompareTo(obj.Id);
    }

    public override string ToString()
    {
      if (this == Default) return "Default face";

      var indices = new StringBuilder("E(");

      foreach (var edge in Edges)
      {
        indices.Append(edge.Key.Id).Append("-");
      }

      indices.Append(") V(");

      foreach (var vertex in Vertices)
      {
        indices.Append(vertex.Id).Append("-");
      }

      indices.Append(")");

      return string.Format("{0}: {1}", Id, indices.ToString());
    }
    #endregion
  }

  struct SubDEdgeDirPair
  {
    readonly SubDEdgePtr m_edge;
    readonly bool m_flipped;

    public SubDEdgeDirPair(SubDEdgePtr value, bool flipped)
    {
      this.m_edge = value;
      this.m_flipped = flipped;
    }

    public SubDEdgePtr Edge { get { return m_edge; } }
    public bool Flipped { get { return m_flipped; } }

    public override string ToString()
    {
      return string.Format("{0} {1}",
        this.Edge.ToString(), this.Flipped.ToString());
    }
  }

  struct SubDVertexDirPair
  {
    readonly SubDVertexPtr m_vertex;
    readonly bool m_flipped;

    public SubDVertexDirPair(SubDVertexPtr value, bool flipped)
    {
      this.m_vertex = value;
      this.m_flipped = flipped;
    }

    public SubDVertexPtr Vertex { get { return m_vertex; } }
    public bool Flipped { get { return m_flipped; } }

    public override string ToString()
    {
      return string.Format("{0} {1}",
        this.Vertex.ToString(), this.Flipped.ToString());
    }
  }
}

namespace Rhino.Geometry.Collections
{
  /// <summary>
  /// Provides access to the vertices and vertex-related functionality of a SubD.
  /// </summary>
  class SubDVertexAccess : IEnumerable<SubDVertexPtr>
  {
    private readonly SubD m_sub_d;

    #region constructors
    internal SubDVertexAccess(SubD ownerSubD)
    {
      m_sub_d = ownerSubD;
    }
    #endregion

    #region properties
    /// <summary>
    /// Gets or sets the number of SubD vertices.
    /// </summary>
    public int Count
    {
      get
      {
        IntPtr const_ptr_sub_d = m_sub_d.ConstPointer();
        return UnsafeNativeMethods.ON_SubD_GetInt(const_ptr_sub_d, UnsafeNativeMethods.SubDIntConst.VertexCount);
      }
    }
    #endregion

    #region access

    /// <summary>
    /// Adds a new vertex to the end of the Vertex list.
    /// </summary>
    /// <param name="tag">The type of vertex tag, such as smooth or corner.</param>
    /// <param name="x">X component of new vertex coordinate.</param>
    /// <param name="y">Y component of new vertex coordinate.</param>
    /// <param name="z">Z component of new vertex coordinate.</param>
    /// <exception cref="ArgumentOutOfRangeException">If tag is unset or non-defined.</exception>
    public SubDVertexPtr Add(SubD.SubDVertexTag tag, double x, double y, double z)
    {
      if (!SubD.IsSubDVertexTagDefined(tag))
        throw new ArgumentOutOfRangeException("tag");

      IntPtr ptr_mesh = m_sub_d.NonConstPointer();
      IntPtr ptr = UnsafeNativeMethods.ON_SubD_AddVertex(ptr_mesh, tag, x, y, z);

      //this should never happen...
      if (ptr == IntPtr.Zero) throw new InvalidOperationException("Impossible to add vertices to this SubD");

      return new SubDVertexPtr(ptr, m_sub_d);
    }

    /// <summary>
    /// Adds a new vertex to the end of the Vertex list.
    /// </summary>
    /// <param name="tag">The type of vertex tag, such as smooth or corner.</param>
    /// <param name="vertex">Location of new vertex.</param>
    /// <returns>The index of the newly added vertex.</returns>
    /// <exception cref="ArgumentOutOfRangeException">If tag is unset or non-defined.</exception>
    public SubDVertexPtr Add(SubD.SubDVertexTag tag, Point3d vertex)
    {
      return Add(tag, vertex.X, vertex.Y, vertex.Z);
    }

    /// <summary>
    /// Adds a series of new vertices to the end of the vertex list.
    /// <para>This overload accepts double-precision points.</para>
    /// </summary>
    /// <param name="tag">The type of vertex tag, such as smooth or corner.</param>
    /// <param name="vertices">A list, an array or any enumerable set of <see cref="Point3d"/>.</param>
    /// <exception cref="ArgumentOutOfRangeException">If tag is unset or non-defined.</exception>
    public void AddVertices(SubD.SubDVertexTag tag, IEnumerable<Point3d> vertices)
    {
      if (!SubD.IsSubDVertexTagDefined(tag))
        throw new ArgumentOutOfRangeException("tag");

      IntPtr ptr_subd = m_sub_d.NonConstPointer();

      foreach (Point3d vertex in vertices)
      {
        bool rc = UnsafeNativeMethods.ON_SubD_AddVertex(ptr_subd, tag, vertex.X, vertex.Y, vertex.Z) != IntPtr.Zero;

        //this should never happen...
        if (!rc) throw new InvalidOperationException("Impossible to add vertices to this SubD");
      }
    }

    /// <summary>
    /// Adds a series of new vertices to the end of the vertex list.
    /// <para>This overload accepts single-precision points.</para>
    /// </summary>
    /// <param name="tag">The type of vertex tag, such as smooth or corner.</param>
    /// <param name="vertices">A list, an array or any enumerable set of <see cref="Point3d"/>.</param>
    public void AddVertices(SubD.SubDVertexTag tag, IEnumerable<Point3f> vertices)
    {
      AddVertices(
        tag,
        vertices.Select(
          Point3d.FromPoint3f
        )
        );
    }

    /// <summary>
    /// Removes some vertices from the subD.
    /// </summary>
    /// <param name="vertexIndices">The array of vertices.
    /// If null, no vertices are removed and the method succeeds.</param>
    /// <returns>True if the method succeeds; false otherwise.</returns>
    public bool RemoveVertexAndConnections(int[] vertexIndices)
    {
      if (vertexIndices == null) return true;

      var vertex_indices_components = new ComponentIndex[vertexIndices.Length];
      for (int i = 0; i < vertex_indices_components.Length; i++)
      {
        var vi = vertexIndices[i];
        if (vi < 0 || vi >= Count)
        {
          string error = string.Format("Input contains invalid index {0} at location {1}.", vi, i);
          throw new ArgumentException(error, "vertexIndices");
        }

        vertex_indices_components[i] = new ComponentIndex(ComponentIndexType.SubdVertex, vertexIndices[i]);
      }

      return m_sub_d.RemoveComponentsAndConnections(vertex_indices_components);
    }

    /// <summary>
    /// Removes a vertex from the subD.
    /// All connected parts are removed, too.
    /// </summary>
    /// <param name="vertexIndex">A vertex.
    /// If null, no vertices are removed and the method succeeds.</param>
    /// <returns>True if the method succeeds; false otherwise.</returns>
    public bool RemoveVertexAndConnections(int vertexIndex)
    {

      return m_sub_d.RemoveComponentsAndConnections(
        new ComponentIndex[] { new ComponentIndex(ComponentIndexType.SubdVertex, vertexIndex), }
        );
    }

    public SubDVertexPtr First
    {
      get
      {
        var const_subd_ptr = m_sub_d.ConstPointer();
        var first = UnsafeNativeMethods.ON_SubD_FirstVertex(const_subd_ptr);
        return new SubDVertexPtr(first, first == IntPtr.Zero ? null : m_sub_d);
      }
    }

    public SubDVertexPtr Find(int vertexIndex)
    {
      var rc = SubDVertexPtr.FromSubDAndId(m_sub_d, vertexIndex);
      if (rc.IsDefault) throw new ArgumentException(
         "vertexIndex does not correspond to a vertex.", "vertexIndex");
      return rc;
    }

    public bool TryFind(int vertexIndex, out SubDVertexPtr vertex)
    {
      vertex = SubDVertexPtr.FromSubDAndId(m_sub_d, vertexIndex);
      return !vertex.IsDefault;
    }

    public IEnumerator<SubDVertexPtr> GetEnumerator()
    {
      var current = First;

      while (current != SubDVertexPtr.Default)
      {
        yield return current;

        current = current.Next;
      }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
      return this.GetEnumerator();
    }

    /// <summary>
    /// This iterator needs to be reset before any use is sensible.
    /// </summary>
    public SubDVertexSectorIterator GetUnboundSectorIterator()
    {
      var ptr = UnsafeNativeMethods.ON_SubDVertex_SectorIterator_EmptyNew();
      return new SubDVertexSectorIterator(m_sub_d, ptr);
    }

    #endregion
  }

  /// <summary>
  /// Provides access to the edge and edge-related functionality of a SubD.
  /// </summary>
  class SubDEdgeAccess : IEnumerable<SubDEdgePtr>
  {
    private readonly SubD m_sub_d;

    #region constructors
    internal SubDEdgeAccess(SubD ownerSubD)
    {
      m_sub_d = ownerSubD;
    }
    #endregion

    #region properties
    /// <summary>
    /// Gets or sets the number of SubD edges.
    /// </summary>
    public int Count
    {
      get
      {
        IntPtr const_ptr_sub_d = m_sub_d.ConstPointer();
        return UnsafeNativeMethods.ON_SubD_GetInt(const_ptr_sub_d, UnsafeNativeMethods.SubDIntConst.EdgeCount);
      }
    }
    #endregion

    #region access

    /// <summary>
    /// Adds a new edge to the end of the SubDEdgePtr list.
    /// </summary>
    /// <param name="tag">The type of edge tag, such as smooth or corner.</param>
    /// <param name="v0">First vertex.</param>
    /// <param name="v1">Second vertex.</param>
    /// <exception cref="ArgumentOutOfRangeException">If tag is unset or non-defined.</exception>
    public SubDEdgePtr Add(SubD.SubDEdgeTag tag, SubDVertexPtr v0, SubDVertexPtr v1)
    {
      if (!SubD.IsSubDEdgeTagDefined(tag))
        throw new ArgumentOutOfRangeException("tag");

      IntPtr ptr_subd = m_sub_d.NonConstPointer();

      IntPtr v0_ptr = v0.ConstPointer();
      IntPtr v1_ptr = v1.ConstPointer();

      IntPtr ptr = UnsafeNativeMethods.ON_SubD_AddEdge(ptr_subd, tag, v0_ptr, v1_ptr);

      //this should never happen...
      if (ptr == IntPtr.Zero) throw new InvalidOperationException("Impossible to add edges to this SubD");

      return new SubDEdgePtr(ptr, m_sub_d);
    }

    public SubDEdgePtr First
    {
      get
      {
        var const_subd_ptr = m_sub_d.ConstPointer();
        var first = UnsafeNativeMethods.ON_SubD_FirstEdge(const_subd_ptr);
        return new SubDEdgePtr(first, first == IntPtr.Zero ? null : m_sub_d);
      }
    }

    public IEnumerator<SubDEdgePtr> GetEnumerator()
    {
      var current = First;

      while (current != SubDEdgePtr.Default)
      {
        yield return current;

        current = current.Next;
      }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
      return this.GetEnumerator();
    }

    #endregion
  }

  /// <summary>
  /// Provides access to the face and face-related functionality of a SubD.
  /// </summary>
  class SubDFaceAccess : IEnumerable<SubDFacePtr>
  {
    private readonly SubD m_sub_d;

    #region constructors
    internal SubDFaceAccess(SubD ownerSubD)
    {
      m_sub_d = ownerSubD;
    }
    #endregion

    #region properties
    /// <summary>
    /// Gets or sets the number of SubD faces.
    /// </summary>
    public int Count
    {
      get
      {
        IntPtr const_ptr_sub_d = m_sub_d.ConstPointer();
        return UnsafeNativeMethods.ON_SubD_GetInt(const_ptr_sub_d, UnsafeNativeMethods.SubDIntConst.FaceCount);
      }
    }
    #endregion

    #region access

    /// <summary>
    /// Adds a new edge to the end of the SubDEdgePtr list.
    /// </summary>
    /// <param name="edges">An array of edges</param>
    /// <param name="directions">The direction each of these edges has, ralated to the face.
    /// True means that the edge is reversed compared to the counter-clockwise order of the face.
    /// <para>This argument can be null. In this case, no edge is considered reversed.</para></param>
    /// <exception cref="ArgumentOutOfRangeException">If tag is unset or non-defined.</exception>
    public SubDFacePtr Add(SubDEdgePtr[] edges, bool[] directions)
    {
      if (edges == null) throw new ArgumentNullException("edges");
      if (directions == null) directions = new bool[edges.Length];
      if (edges.Length != directions.Length) throw new ArgumentOutOfRangeException("directions", "Length of edges and directions must coicide.");
      if (edges.Length < 3) throw new ArgumentOutOfRangeException("edges", "There must be at least 3 edges in a SubD face.");

      IntPtr ptr_subd = m_sub_d.NonConstPointer();

      IntPtr result;
      unsafe
      {
        IntPtr* block = stackalloc IntPtr[edges.Length];
        for (int i = 0; i < edges.Length; i++)
          block[i] = edges[i].ConstPointer();
        var ptr_ptr = new IntPtr(block);
        result = UnsafeNativeMethods.ON_SubD_AddFace(
          ptr_subd, (uint)edges.Length, ptr_ptr, directions);
      }

      //this should never happen...
      if (result == IntPtr.Zero) throw new InvalidOperationException(
        "Impossible to add this face to this SubD.");

      return new SubDFacePtr(result, m_sub_d);
    }

    public SubDFacePtr First
    {
      get
      {
        var const_subd_ptr = m_sub_d.ConstPointer();
        var first = UnsafeNativeMethods.ON_SubD_FirstFace(const_subd_ptr);
        return new SubDFacePtr(first, first == IntPtr.Zero ? null : m_sub_d);
      }
    }

    public IEnumerator<SubDFacePtr> GetEnumerator()
    {
      var current = First;

      while (current != SubDFacePtr.Default)
      {
        yield return current;

        current = current.Next;
      }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
      return this.GetEnumerator();
    }

    #endregion
  }
}

// #if OPENNURBS_SUBD_WIP
#endif
