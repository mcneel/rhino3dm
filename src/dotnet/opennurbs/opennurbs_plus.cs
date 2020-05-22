using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Rhino.DocObjects;
using Rhino.Runtime.InteropWrappers;

namespace Rhino.Runtime.InteropWrappers
{
  /// <summary>
  /// This is only needed when passing values to the Rhino C++ core, ignore
  /// for .NET plug-ins.
  /// </summary>
  [CLSCompliant(false)]
  [StructLayout(LayoutKind.Sequential, Pack = 8, Size = 88)]
  public struct MeshPointDataStruct
  {
    /// <summary>
    /// This is only needed when passing values to the Rhino C++ core, ignore
    /// for .NET plug-ins.
    /// </summary>
    public double m_et;

    //ON_COMPONENT_INDEX m_ci;
    /// <summary>
    /// This is only needed when passing values to the Rhino C++ core, ignore
    /// for .NET plug-ins.
    /// </summary>
    public uint m_ci_type;
    /// <summary>
    /// This is only needed when passing values to the Rhino C++ core, ignore
    /// for .NET plug-ins.
    /// </summary>
    public int m_ci_index;

    /// <summary>
    /// This is only needed when passing values to the Rhino C++ core, ignore
    /// for .NET plug-ins.
    /// </summary>
    public int m_edge_index;
    /// <summary>
    /// This is only needed when passing values to the Rhino C++ core, ignore
    /// for .NET plug-ins.
    /// </summary>
    public int m_face_index;
    /// <summary>
    /// This is only needed when passing values to the Rhino C++ core, ignore
    /// for .NET plug-ins.
    /// </summary>
    public char m_Triangle;
    /// <summary>
    /// This is only needed when passing values to the Rhino C++ core, ignore
    /// for .NET plug-ins.
    /// </summary>
    public double m_t0;
    /// <summary>
    /// This is only needed when passing values to the Rhino C++ core, ignore
    /// for .NET plug-ins.
    /// </summary>
    public double m_t1;
    /// <summary>
    /// This is only needed when passing values to the Rhino C++ core, ignore
    /// for .NET plug-ins.
    /// </summary>
    public double m_t2;
    /// <summary>
    /// This is only needed when passing values to the Rhino C++ core, ignore
    /// for .NET plug-ins.
    /// </summary>
    public double m_t3;

    //ON_3dPoint m_P;
    /// <summary>
    /// This is only needed when passing values to the Rhino C++ core, ignore
    /// for .NET plug-ins.
    /// </summary>
    public double m_Px;
    /// <summary>
    /// This is only needed when passing values to the Rhino C++ core, ignore
    /// for .NET plug-ins.
    /// </summary>
    public double m_Py;
    /// <summary>
    /// This is only needed when passing values to the Rhino C++ core, ignore
    /// for .NET plug-ins.
    /// </summary>
    public double m_Pz;
  }
}

namespace Rhino.Geometry
{

  /// <summary>
  /// Represents a point that is found on a mesh.
  /// </summary>
  public class MeshPoint
  {
    internal Mesh m_parent;
    internal MeshPointDataStruct m_data;
    internal MeshPoint(Mesh parent, MeshPointDataStruct ds)
    {
      m_parent = parent;
      m_data = ds;
    }

    /// <summary>
    /// The mesh that is related to this point.
    /// </summary>
    /// <since>5.0</since>
    public Mesh Mesh
    {
      get { return m_parent; }
    }

    /// <summary>
    /// Edge parameter when found.
    /// </summary>
    /// <since>5.0</since>
    public double EdgeParameter
    {
      get { return m_data.m_et; }
    }

    /// <summary>
    /// Gets the component index of the intersecting element in the mesh.
    /// </summary>
    /// <since>5.0</since>
    public ComponentIndex ComponentIndex
    {
      get
      {
        return new ComponentIndex((ComponentIndexType)m_data.m_ci_type, m_data.m_ci_index);
      }
    }

    /// <summary>
    /// When set, EdgeIndex is an index of an edge in the mesh's edge list.
    /// </summary>
    /// <since>5.0</since>
    public int EdgeIndex
    {
      get { return m_data.m_edge_index; }
    }

    /// <summary>
    /// FaceIndex is an index of a face in mesh.Faces.
    /// When ComponentIndex refers to a vertex, any face that uses the vertex
    /// may appear as FaceIndex.  When ComponenctIndex refers to an Edge or
    /// EdgeIndex is set, then any face that uses that edge may appear as FaceIndex.
    /// </summary>
    /// <since>5.0</since>
    public int FaceIndex
    {
      get { return m_data.m_face_index; }
    }

    //bool IsValid( ON_TextLog* text_log ) const;

#if RHINO_SDK
    /// <summary>
    /// Gets the mesh face indices of the triangle where the
    /// intersection is on the face takes into consideration
    /// the way the quad was split during the intersection.
    /// </summary>
    /// <since>5.0</since>
    public bool GetTriangle(out int a, out int b, out int c)
    {
      IntPtr pConstMesh = m_parent.ConstPointer();
      a = -1;
      b = -1;
      c = -1;
      return UnsafeNativeMethods.ON_MESHPOINT_GetTriangle(pConstMesh, ref m_data, ref a, ref b, ref b);
    }
#endif

    /// <summary>
    /// Face triangle where the intersection takes place:
    /// <para>0 is unset</para>
    /// <para>A is 0,1,2</para>
    /// <para>B is 0,2,3</para>
    /// <para>C is 0,1,3</para>
    /// <para>D is 1,2,3</para>
    /// </summary>
    /// <since>5.0</since>
    public char Triangle
    {
      get { return m_data.m_Triangle; }
    }


    /// <summary>
    /// Barycentric quad coordinates for the point on the mesh
    /// face mesh.Faces[FaceIndex].  If the face is a triangle
    /// disregard T[3] (it should be set to 0.0). If the face is
    /// a quad and is split between vertexes 0 and 2, then T[3]
    /// will be 0.0 when point is on the triangle defined by vi[0],
    /// vi[1], vi[2], and T[1] will be 0.0 when point is on the
    /// triangle defined by vi[0], vi[2], vi[3]. If the face is a
    /// quad and is split between vertexes 1 and 3, then T[2] will
    /// be 0.0 when point is on the triangle defined by vi[0],
    /// vi[1], vi[3], and m_t[0] will be 0.0 when point is on the
    /// triangle defined by vi[1], vi[2], vi[3].
    /// </summary>
    /// <since>5.0</since>
    public double[] T
    {
      get { return m_t ?? (m_t = new double[] { m_data.m_t0, m_data.m_t1, m_data.m_t2, m_data.m_t3 }); }
    }
    double[] m_t;

    /// <summary>
    /// Gets the location (position) of this point.
    /// </summary>
    /// <since>5.0</since>
    public Point3d Point
    {
      get { return new Point3d(m_data.m_Px, m_data.m_Py, m_data.m_Pz); }
    }
  }
}

namespace Rhino.Geometry.Intersect
{
  //// keep private until we have something that works/makes sense
  //public class RayShooter //: IDisposable
  //{
  //  //IntPtr m_mesh_rtree = IntPtr.Zero;
  //  //Mesh m_target_mesh;
  //  //NOTE! This is NOT a direct wrapper around ON_RayShooter. The class name was used to define
  //  //      a general ray shooter against geometry. Different low level unmanaged classes are used
  //  //      by this class
  //  public RayShooter(Mesh targetMesh)
  //  {
  //    //m_target_mesh = targetMesh;
  //    //IntPtr pConstMesh = m_target_mesh.ConstPointer();
  //    //m_mesh_rtree = UnsafeNativeMethods.ON_RTree_NewFromMesh(pConstMesh);
  //  }

  //  public double Shoot(Ray3d ray)
  //  {
  //    return 0;
  //    //return UnsafeNativeMethods.ON_RTree_ShootRay(m_mesh_rtree, ref ray);
  //  }

  //}

  //public class ON_CurveLeafBox { }
  //public class ON_CurveTreeBezier : ON_BezierCurve { }
  //public class ON_SurfaceLeafBox { }
  //public class ON_SurfaceTreeBezier : ON_BezierSurface { }
  //public class ON_CurveTreeNode { }
  //public class ON_CurveTree { }
  //public class ON_SurfaceTreeNode { }
  //public class ON_SurfaceTree { }
  //public class ON_RayShooter { }
  //public class ON_MMX_POINT { }
  //public class ON_MMX_Polyline { }
  //public class ON_CURVE_POINT { }
  //public class ON_CMX_EVENT { }
  //public class ON_MeshTreeNode { }
  //public class ON_MeshTree { }

  //also add ON_RTree

#if RHINO_SDK

  /// <summary>
  /// Represents an element which is part of a clash or intersection between two meshes.
  /// </summary>
  public struct MeshInterference
  {
    #region Members
    private int m_index_a;
    private int m_index_b;
    private Point3d[] m_hit_points;
    #endregion

    #region Properties

    /// <summary>
    /// The index of the first clashing, or interfering object.
    /// </summary>
    /// <since>7.0</since>
    public int IndexA
    {
      get { return m_index_a; }
      set { m_index_a = value; }
    }

    /// <summary>
    /// The index of the second clashing, or interfering object.
    /// </summary>
    /// <since>7.0</since>
    public int IndexB
    {
      get { return m_index_b; }
      set { m_index_b = value; }
    }

    /// <summary>
    /// Array of hit points where the objects of IndexA and IndexB interfere.
    /// </summary>
    /// <since>7.0</since>
    public Point3d[] HitPoints
    {
      get { return m_hit_points; }
      set { m_hit_points = value; }
    }

    #endregion
  }


  /// <summary>
  /// Represents a particular instance of a clash or intersection between two meshes.
  /// </summary>
  public class MeshClash
  {
    #region Members
    Mesh m_mesh_a;
    Mesh m_mesh_b;
    Point3d m_P = Point3d.Unset;
    double m_radius;
    #endregion

    private MeshClash() { }

    /// <summary>
    /// Gets the first mesh.
    /// </summary>
    /// <since>5.0</since>
    public Mesh MeshA { get { return m_mesh_a; } }

    /// <summary>
    /// Gets the second mesh.
    /// </summary>
    /// <since>5.0</since>
    public Mesh MeshB { get { return m_mesh_b; } }

    /// <summary>
    /// If valid, then the sphere centered at ClashPoint of ClashRadius
    /// distance intersects the clashing meshes.
    /// </summary>
    /// <since>5.0</since>
    public Point3d ClashPoint { get { return m_P; } }

    /// <summary>
    /// Gets the clash, or intersection, radius.
    /// </summary>
    /// <since>5.0</since>
    public double ClashRadius { get { return m_radius; } }

    /// <summary>
    /// Searches for locations where the distance from <i>a mesh in one set</i> of meshes
    /// is less than distance to <i>another mesh in a second set</i> of meshes.
    /// </summary>
    /// <param name="setA">The first set of meshes.</param>
    /// <param name="setB">The second set of meshes.</param>
    /// <param name="distance">The largest distance at which there is a clash.
    /// All values smaller than this cause a clash as well.</param>
    /// <param name="maxEventCount">The maximum number of clash objects.</param>
    /// <returns>An array of clash objects.</returns>
    /// <since>5.0</since>
    public static MeshClash[] Search(IEnumerable<Mesh> setA, IEnumerable<Mesh> setB, double distance, int maxEventCount)
    {
      IList<Mesh> _setA = setA as IList<Mesh> ?? new List<Mesh>(setA);

      IList<Mesh> _setB = setB as IList<Mesh> ?? new List<Mesh>(setB);


      Rhino.Runtime.InteropWrappers.SimpleArrayMeshPointer meshes_a = new Runtime.InteropWrappers.SimpleArrayMeshPointer();
      foreach (Mesh m in setA)
        meshes_a.Add(m, true);
      Rhino.Runtime.InteropWrappers.SimpleArrayMeshPointer meshes_b = new Runtime.InteropWrappers.SimpleArrayMeshPointer();
      foreach (Mesh m in setB)
        meshes_b.Add(m, true);

      IntPtr pClashEventList = UnsafeNativeMethods.ON_SimpleArray_ClashEvent_New();
      IntPtr pMeshesA = meshes_a.ConstPointer();
      IntPtr pMeshesB = meshes_b.ConstPointer();
      int count = UnsafeNativeMethods.ONC_MeshClashSearch(pMeshesA, pMeshesB, distance, maxEventCount, true, pClashEventList);

      MeshClash[] rc = new MeshClash[count];
      Point3d pt = new Point3d();
      int indexA = 0;
      int indexB = 0;
      double radius = distance / 2.0;
      for (int i = 0; i < count; i++)
      {
        MeshClash mc = new MeshClash();
        UnsafeNativeMethods.ON_SimpleArray_ClashEvent_GetEvent(pClashEventList, i, ref indexA, ref indexB, ref pt);
        if (indexA >= 0 && indexB >= 0)
        {
          mc.m_mesh_a = _setA[indexA];
          mc.m_mesh_b = _setB[indexB];
          mc.m_P = pt;
          mc.m_radius = radius;
        }
        rc[i] = mc;
      }

      meshes_a.Dispose();
      meshes_b.Dispose();

      UnsafeNativeMethods.ON_SimpleArray_ClashEvent_Delete(pClashEventList);
      GC.KeepAlive(setA);
      GC.KeepAlive(setB);
      return rc;
    }

    /// <summary>
    /// Searches the locations where the distance from <i>the first mesh</i> to <i>a mesh in the second set</i> of meshes
    /// is less than the provided value.
    /// </summary>
    /// <param name="meshA">The first mesh.</param>
    /// <param name="setB">The second set of meshes.</param>
    /// <param name="distance">The largest distance at which there is a clash.
    /// All values smaller than this cause a clash as well.</param>
    /// <param name="maxEventCount">The maximum number of clash objects.</param>
    /// <returns>An array of clash objects.</returns>
    /// <since>5.0</since>
    public static MeshClash[] Search(Mesh meshA, IEnumerable<Mesh> setB, double distance, int maxEventCount)
    {
      return Search(new Mesh[] { meshA }, setB, distance, maxEventCount);
    }

    /// <summary>
    /// Searches the locations where the distance from <i>the first mesh</i> to <i>the second mesh</i>
    /// is less than the provided value.
    /// </summary>
    /// <param name="meshA">The first mesh.</param>
    /// <param name="meshB">The second mesh.</param>
    /// <param name="distance">The largest distance at which there is a clash.
    /// All values smaller than this cause a clash as well.</param>
    /// <param name="maxEventCount">The maximum number of clash objects.</param>
    /// <returns>An array of clash objects.</returns>
    /// <since>5.0</since>
    public static MeshClash[] Search(Mesh meshA, Mesh meshB, double distance, int maxEventCount)
    {
      return Search(new Mesh[] { meshA }, new Mesh[] { meshB }, distance, maxEventCount);
    }

    /// <summary>
    /// Searches for locations where the distance from a RhinoObject, in one set of objects,
    /// is less than the specified distance to another RhinoObject in a second set of objects.
    /// This function uses the object's mesh to calculate the interferences.
    /// Acceptable object types include: BrepObject, ExtrusionObject, MeshObject, and SubDObject. 
    /// </summary>
    /// <param name="setA">The first set of Rhino objects.</param>
    /// <param name="setB">The second set of Rhino objects.</param>
    /// <param name="distance">The largest distance at which a clash can occur.</param>
    /// <returns>An array of mesh interference object if successful, or an empty array on failure.</returns>
    /// <since>7.0</since>
    public static MeshInterference[] Search(IEnumerable<RhinoObject> setA, IEnumerable<RhinoObject> setB, double distance)
    {
      return Search(setA, setB, distance, MeshType.Render, MeshingParameters.FastRenderMesh);
    }

    /// <summary>
    /// Searches for locations where the distance from a RhinoObject, in one set of objects,
    /// is less than the specified distance to another RhinoObject in a second set of objects.
    /// This function uses the object's mesh to calculate the interferences.
    /// Acceptable object types include: BrepObject, ExtrusionObject, MeshObject, and SubDObject. 
    /// </summary>
    /// <param name="setA">The first set of Rhino objects.</param>
    /// <param name="setB">The second set of Rhino objects.</param>
    /// <param name="distance">The largest distance at which a clash can occur.</param>
    /// <param name="meshType">The type of mesh to be used for the calculation.</param>
    /// <param name="meshingParameters">The meshing parameters used to generate meshes for the calculation.</param>
    /// <returns>An array of mesh interference object if successful, or an empty array on failure.</returns>
    /// <since>7.0</since>
    public static MeshInterference[] Search(IEnumerable<RhinoObject> setA, IEnumerable<RhinoObject> setB, double distance, MeshType meshType, MeshingParameters meshingParameters)
    {
      using (var set_a_array = new Runtime.InternalRhinoObjectArray(setA))
      using (var set_b_array = new Runtime.InternalRhinoObjectArray(setB))
      {
        var ptr_set_a = set_a_array.NonConstPointer();
        var ptr_set_b = set_b_array.NonConstPointer();
        var ptr_mp = meshingParameters.ConstPointer();
        var ptr_clash_events = UnsafeNativeMethods.RhObjectClashEventArray_New(); // new
        var count = UnsafeNativeMethods.RHC_CRhClashDetect_TestClash(ptr_set_a, ptr_set_b, distance, (int)meshType, ptr_mp, ptr_clash_events);

        var rc = new MeshInterference[count];
        for (var i = 0; i < count; i++)
        {
          var index_a = -1;
          var index_b = -1;
          var hit_points = new SimpleArrayPoint3d(); // new
          var ptr_hit_points = hit_points.NonConstPointer();
          var mi = new MeshInterference();
          if (UnsafeNativeMethods.RhObjectClashEventArray_GetAt(ptr_clash_events, i, ref index_a, ref index_b, ptr_hit_points))
          {
            mi.IndexA = index_a;
            mi.IndexB = index_b;
            mi.HitPoints = hit_points.Count > 0 ? hit_points.ToArray() : new Point3d[0];
          }
          else
          {
            mi.IndexA = -1;
            mi.IndexB = -1;
            mi.HitPoints = new Point3d[0];
          }
          hit_points.Dispose(); // delete
          rc[i] = mi;
        }

        UnsafeNativeMethods.RhObjectClashEventArray_Delete(ptr_clash_events); // delete
        GC.KeepAlive(setA);
        GC.KeepAlive(setB);
        return rc;
      }
    }

    /// <summary>
    /// Finds all of the mesh faces on each of two Rhino objects that interfere within a clash distance.
    /// This function uses the object's mesh to calculate the interferences.
    /// Acceptable object types include: BrepObject, ExtrusionObject, MeshObject, and SubDObject. 
    /// </summary>
    /// <param name="objA">The first Rhino object.</param>
    /// <param name="objB">The second Rhino object.</param>
    /// <param name="distance">The largest distance at which a clash can occur.</param>
    /// <returns>The resulting meshes are sub-meshes of the input meshes if successful, or an empty array on error.</returns>
    /// <since>7.0</since>
    public static Mesh[] FindDetail(RhinoObject objA, RhinoObject objB, double distance)
    {
      return FindDetail(objA, objB, distance, MeshType.Render, MeshingParameters.FastRenderMesh);
    }

    /// <summary>
    /// Finds all of the mesh faces on each of two Rhino objects that interfere within a clash distance.
    /// This function uses the object's mesh to calculate the interferences.
    /// Acceptable object types include: BrepObject, ExtrusionObject, MeshObject, and SubDObject. 
    /// </summary>
    /// <param name="objA">The first Rhino object.</param>
    /// <param name="objB">The second Rhino object.</param>
    /// <param name="distance">The largest distance at which a clash can occur.</param>
    /// <param name="meshType">The type of mesh to be used for the calculation.</param>
    /// <param name="meshingParameters">The meshing parameters used to generate meshes for the calculation.</param>
    /// <returns>The resulting meshes are sub-meshes of the input meshes if successful, or an empty array on error.</returns>
    /// <since>7.0</since>
    public static Mesh[] FindDetail(RhinoObject objA, RhinoObject objB, double distance, MeshType meshType, MeshingParameters meshingParameters)
    {
      if (null == objA)
        throw new ArgumentNullException(nameof(objA));
      if (null == objB)
        throw new ArgumentNullException(nameof(objB));

      using (var out_meshes = new SimpleArrayMeshPointer())
      {
        var ptr_obj_a = objA.ConstPointer();
        var ptr_obj_b = objB.ConstPointer();
        var ptr_mp = meshingParameters.ConstPointer();
        var ptr_out_meshes = out_meshes.NonConstPointer();
        var count = UnsafeNativeMethods.RHC_CRhClashDetect_FindClashDetail(ptr_obj_a, ptr_obj_b, distance, (int)meshType, ptr_mp, ptr_out_meshes);
        GC.KeepAlive(objA);
        GC.KeepAlive(objB);
        return count > 0 ? out_meshes.ToNonConstArray() : new Mesh[0];
      }
    }
  }
#endif
}
