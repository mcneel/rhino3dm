using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Threading;
using Rhino.Collections;
using System.Runtime.Serialization;
using Rhino.DocObjects;
using Rhino.FileIO;
using Rhino.Geometry;
using Rhino.Runtime.InteropWrappers;
using Rhino.Render;
using Rhino.Runtime;

namespace Rhino.Render
{
  /// <summary>
  /// Used for cached texture coordinates
  /// </summary>
  public class CachedTextureCoordinates : Runtime.CommonObject, IList<Point3d>
  {
    /// <summary>
    /// Internal constructor used to wrap ON_TextureCoordinates* retrieved from
    /// a Rhino mesh.
    /// </summary>
    /// <param name="pTextureCoordinates"></param>
    internal CachedTextureCoordinates(IntPtr pTextureCoordinates)
    {
      ConstructNonConstObject(pTextureCoordinates);
      DoNotDestructOnDispose();
    }
    /// <summary>
    /// Call this method to get the cached texture coordinates from a Rhino
    /// mesh.
    /// </summary>
    /// <param name="mesh">
    /// Mesh to query for cached coordinates.
    /// </param>
    /// <param name="textureMappingId">
    /// The texture mapping ID to look for.
    /// </param>
    /// <returns>
    /// Returns the cached coordinates if found or null if not.
    /// </returns>
    internal static CachedTextureCoordinates GetCachedTextureCoordinates(Mesh mesh, Guid textureMappingId)
    {
      var tc_pointer = UnsafeNativeMethods.ON_Mesh_CachedTextureCoordinates(mesh.ConstPointer(), textureMappingId);
      if (tc_pointer == IntPtr.Zero)
        return null;
      var tc = new CachedTextureCoordinates(tc_pointer);
      GC.KeepAlive(mesh);
      return tc;
    }
    /// <summary>
    /// Use this method to iterate the cached texture coordinate array.
    /// </summary>
    /// <param name="index">
    /// Index for the vertex to fetch.
    /// </param>
    /// <param name="u">
    /// Output parameter which will receive the U value.
    /// </param>
    /// <param name="v">
    /// Output parameter which will receive the V value.
    /// </param>
    /// <param name="w">
    /// Output parameter which will receive the W value, this is only
    /// meaningful if <see cref="Dim"/> is 3.
    /// </param>
    /// <returns>
    /// Returns true if index is valid; otherwise returns false.
    /// </returns>
    public bool TryGetAt(int index, out double u, out double v, out double w)
    {
      u = v = w = -1.0;
      var success = UnsafeNativeMethods.ON_TextureCoordinates_GetTextureCoordinate(ConstPointer(), index, ref u, ref v, ref w);
      return (success > 0);
    }

    /// <summary>
    /// Coordinate dimension: 2 = UV, 3 = UVW
    /// </summary>
    public int Dim
    {
      get { return UnsafeNativeMethods.ON_TextureCoordinates_GetDimension(ConstPointer()); }
    }
    /// <summary>
    /// The texture mapping Id.
    /// </summary>
    public Guid MappingId
    {
      get { return UnsafeNativeMethods.ON_TextureCoordinates_GetMappingId(ConstPointer()); }
    }

    internal override IntPtr _InternalGetConstPointer()
    {
      return IntPtr.Zero;
    }

    internal override IntPtr _InternalDuplicate(out bool applymempressure)
    {
      applymempressure = false;
      var const_pointer = ConstPointer();
      return UnsafeNativeMethods.ON_Object_Duplicate(const_pointer);
    }

    #region IList<Point3d> implementation
    void ICollection<Point3d>.Add(Point3d item)
    {
      throw new NotSupportedException("The cached texture coordinate list is read-only");
    }
    /// <summary>
    /// IList implementation, this list is always read-only so calling this
    /// will cause a NotSupportedException to be thrown.
    /// </summary>
    void ICollection<Point3d>.Clear()
    {
      throw new NotSupportedException("The cached texture coordinate list is read-only");
    }
    /// <summary>
    /// Determines whether this collection contains a specific value.
    /// </summary>
    /// <param name="item"></param>
    /// <returns></returns>
    public bool Contains(Point3d item)
    {
      var index = IndexOf(item);
      return (index >= 0);
    }
    /// <summary>
    /// Copies the elements of the this collection to an System.Array,
    /// starting at a particular System.Array index.
    /// </summary>
    /// <param name="array">
    /// The one-dimensional System.Array that is the destination of the
    /// elements copied from this collection. The System.Array must have
    /// zero-based indexing.
    /// </param>
    /// <param name="arrayIndex">
    /// The zero-based index in array at which copying begins.
    /// </param>
    /// <exception cref="System.ArgumentNullException">
    /// array is null
    /// </exception>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// arrayIndex is less than 0.
    /// </exception>
    /// <exception cref="System.ArgumentException">
    /// The number of elements in this collection is greater than the available
    /// space from arrayIndex to the end of the destination array.
    /// </exception>
    public void CopyTo(Point3d[] array, int arrayIndex)
    {
      if (array == null) throw new ArgumentNullException("array");
      if (arrayIndex < 0) throw new ArgumentOutOfRangeException("arrayIndex");
      if ((arrayIndex + Count) >= array.Length) throw new ArgumentException("Array not big enough for the point list");
      var count = Count;
      for (int i = 0, j = arrayIndex; i < count; i++, j++)
      {
        double u, v, w;
        TryGetAt(i, out u, out v, out w);
        array[j].X = u;
        array[j].Y = v;
        array[j].Z = w;
      }
    }
    /// <summary>
    /// IList implementation, this list is always read-only so calling this
    /// will cause a NotSupportedException to be thrown.
    /// </summary>
    bool ICollection<Point3d>.Remove(Point3d item)
    {
      throw new NotSupportedException("The cached texture coordinate list is read-only");
    }
    /// <summary>
    /// Number of cached coordinates.
    /// </summary>
    public int Count
    {
      get { return UnsafeNativeMethods.ON_TextureCoordinates_GetPointListCount(ConstPointer()); }
    }
    /// <summary>
    /// This collection is always read-only
    /// </summary>
    public bool IsReadOnly { get { return true; } }
    /// <summary>
    /// Returns an enumerator that iterates through this collection.
    /// </summary>
    /// <returns>
    /// A enumerator that can be used to iterate through this collection.
    /// </returns>
    public IEnumerator<Point3d> GetEnumerator()
    {
      var const_pointer = ConstPointer();
      return new CachedTextureCoordinatesEnumerator(const_pointer);
    }
    /// <summary>
    /// Returns an enumerator that iterates through this collection.
    /// </summary>
    /// <returns>
    /// A enumerator that can be used to iterate through this collection.
    /// </returns>
    IEnumerator IEnumerable.GetEnumerator()
    {
      return GetEnumerator();
    }
    /// <summary>
    /// Determines the index of a specific point in this collection.
    /// </summary>
    /// <param name="item">
    /// The point (UV or UVW) to locate in this collection.
    /// </param>
    /// <returns>
    /// The index of item if found in the list; otherwise, -1.
    /// </returns>
    public int IndexOf(Point3d item)
    {
      var count = Count;
      for (var i = 0; i < count; i++)
      {
        double u, v, w;
        var success = TryGetAt(i, out u, out v, out w);
        if (success && u == item.X && v == item.Y && (Dim < 3 || w == item.Z))
          return i;
      }
      return -1;
    }

    void IList<Point3d>.Insert(int index, Point3d item)
    {
      throw new NotSupportedException("The cached texture coordinate list is read-only");
    }

    void IList<Point3d>.RemoveAt(int index)
    {
      throw new NotSupportedException("The cached texture coordinate list is read-only");
    }
    /// <summary>
    /// Gets the element at the specified index. Never call the set method, it
    /// will always throw a NotSupportedException because this list is
    /// read-only.
    /// </summary>
    /// <param name="index">
    /// The zero-based index of the element to get.
    /// </param>
    /// <returns>
    /// The element at the specified index.
    /// </returns>
    /// <exception cref="NotSupportedException">
    /// IList implementation, this list is always read-only so calling the set
    /// method will always cause a NotSupportedException to be thrown.
    /// </exception>
    public Point3d this[int index]
    {
      get
      {
        if (index < 0 || index >= Count) throw new ArgumentOutOfRangeException("index", "index is not a valid index in this list");
        double u, v, w;
        TryGetAt(index, out u, out v, out w);
        return new Point3d(u, v, w);
      }
    }

    Point3d IList<Point3d>.this[int index]
    {
      get
      {
        return this[index];
      }
      set
      {
        throw new NotSupportedException("The cached texture coordinate list is read-only");
      }
    }

    #endregion IList<Point3d> implementation
  }

  /// <summary>
  /// Internal class used to enumerate a list of CachedTextureCoordinates
  /// </summary>
  class CachedTextureCoordinatesEnumerator : IEnumerator<Point3d>
  {
    internal CachedTextureCoordinatesEnumerator(IntPtr constPointer)
    {
      m_const_pointer = constPointer;
      m_count = UnsafeNativeMethods.ON_TextureCoordinates_GetPointListCount(constPointer);
    }

    private readonly IntPtr m_const_pointer;
    private readonly int m_count;
    private int m_position = -1;

    public void Dispose()
    {
    }

    public bool MoveNext()
    {
      m_position++;
      return (m_position < m_count);
    }

    public void Reset()
    {
      m_position = -1;
    }

    public Point3d Current
    {
      get
      {
        if (m_position < 0 || m_position >= m_count)
          throw new InvalidOperationException();
        var u = 0.0;
        var v = 0.0;
        var w = 0.0;
        UnsafeNativeMethods.ON_TextureCoordinates_GetTextureCoordinate(m_const_pointer, m_position, ref u, ref v, ref w);
        return new Point3d(u, v, w);
      }
    }

    object IEnumerator.Current
    {
      get { return Current; }
    }
  }

  /// <summary>
  /// Holds texture mapping information.
  /// </summary>
  public class MappingTag
  {
    /// <summary>
    ///  Gets or sets a map globally unique identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    ///  Gets or sets a texture mapping type: linear, cylinder, etc...
    /// </summary>
    public TextureMappingType MappingType { get; set; }

    /// <summary>
    /// Gets or sets the cyclic redundancy check on the mapping.
    /// See also <see cref="RhinoMath.CRC32(uint,byte[])" />.
    /// </summary>
    [CLSCompliant(false)]
    public uint MappingCRC { get; set; }

    /// <summary>
    /// Gets or sets a 4x4 matrix transform.
    /// </summary>
    public Transform MeshTransform { get; set; }
  }
}

namespace Rhino.Geometry
{
  /// <summary>
  /// Type of Mesh Parameters used by the RhinoDoc for meshing objects
  /// </summary>
  public enum MeshingParameterStyle
  {
    /// <summary>No style</summary>
    None = 0,
    /// <summary></summary>
    Fast = 1,
    /// <summary></summary>
    Quality = 2,
    /// <summary></summary>
    Custom = 9,
    /// <summary></summary>
    PerObject = 10
  }

#if RHINO_SDK
  /// <summary>
  /// The direction of the smoothing used by Curve, Surface, and Mesh Smooth.
  /// </summary>
  public enum SmoothingCoordinateSystem
  {
    /// <summary>
    /// World coordinates
    /// </summary>
    World = 0,
    /// <summary>
    /// Construction plane coordinates
    /// </summary>
    CPlane = 1,
    /// <summary>
    /// Object u, v, and n coordinates
    /// </summary>
    Object = 2
  }
#endif

  /// <summary>
  /// Defines how to pack render/meshes textures.
  /// </summary>
  public enum MeshingParameterTextureRange 
  {
    /// <summary>This value is not set.</summary>
    Unset = 0,

    /// <summary>
    /// Rach face has a normalized texture range [0,1]x[0,1].
    /// The normalized coordinate is calculated using the
    /// entire surface domain.  For meshes of trimmed
    /// surfaces when the active area is a small subset of
    /// the entire surface, there will be large regions of
    /// unused texture space in [0,1]x[0,1].  When the 3d region
    /// being meshed is far from being square-ish, there will be
    /// a substantual amount of distortion mapping [0,1]x[0,1]
    /// texture space to the 3d mesh.
    /// </summary>
    UnpackedUnscaledNormalized = 1,

    /// <summary>
    /// Each face is assigned a texture range that is a 
    /// subrectangle of [0,1]x[0,1].  The subrectangles are 
    /// mutually disjoint and packed into into [0,1]x[0,1]
    /// in a way that minimizes distortion and maximizes the
    /// coverage of [0,1]x[0,1].
    /// When the surface or surfaces being meshed are trimmed,
    /// this option takes into account only the region of the
    /// base surface the mesh covers and uses as much of 
    /// [0,1]x[0,1] as possible. (default)
    /// </summary>
    PackedScaledNormalized = 2,
  }


  /// <summary>
  /// Represents settings used for creating a mesh representation of a brep or surface.
  /// </summary>
  public class MeshingParameters : IDisposable
  {
    IntPtr m_ptr; // This class is never const
    internal IntPtr ConstPointer() { return m_ptr; }
    internal IntPtr NonConstPointer() { return m_ptr; }

    /// <summary>
    /// Initializes a new instance with default values.
    /// <para>Initial values are same as <see cref="Default"/>.</para>
    /// </summary>
    public MeshingParameters()
    {
      m_ptr = UnsafeNativeMethods.ON_MeshParameters_New();
    }

    /// <summary>
    /// Initializes a new instance with simple values, 
    /// similar to that of Rhino's meshing slider interface.
    /// </summary>
    /// <param name="density">
    /// The density and number of mesh polygons, where 0.0 &lt;= density &lt;= 1.0,
    /// where 0 quickly creates coarse meshes, and 1 slowly creates dense meshes.
    /// </param>
    public MeshingParameters(double density)
    {
      m_ptr = UnsafeNativeMethods.ON_MeshParameters_New2(density, RhinoMath.UnsetValue);
    }

    /// <summary>
    /// Initializes a new instance with simple values, 
    /// similar to that of Rhino's meshing slider interface.
    /// </summary>
    /// <param name="density">
    /// The density and number of mesh polygons, where 0.0 &lt;= density &lt;= 1.0,
    /// where 0 quickly creates coarse meshes, and 1 slowly creates dense meshes.
    /// </param>
    /// <param name="minimumEdgeLength">The minimum allowed mesh edge length.</param>
    public MeshingParameters(double density, double minimumEdgeLength)
    {
      m_ptr = UnsafeNativeMethods.ON_MeshParameters_New2(density, minimumEdgeLength);
    }

    internal MeshingParameters(IntPtr pMeshingParameters)
    {
      m_ptr = pMeshingParameters;
    }

    /// <summary>
    /// Passively reclaims unmanaged resources when the class user did not explicitly call Dispose().
    /// </summary>
    ~MeshingParameters()
    {
      Dispose(false);
    }

    /// <summary>
    /// Actively reclaims unmanaged resources that this instance uses.
    /// </summary>
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
        UnsafeNativeMethods.ON_MeshParameters_Delete(m_ptr);
        m_ptr = IntPtr.Zero;
      }
    }

    #region constants
#if RHINO_SDK
    /// <summary>
    /// Gets the MeshingParameters that are currently set for a document.
    /// These are the same settings that are shown in the DocumentProperties
    /// "mesh settings" user interface.
    /// </summary>
    /// <param name="doc">A Rhino document to query.</param>
    /// <returns>Meshing parameters of the document.</returns>
    /// <exception cref="ArgumentNullException">If doc is null.</exception>
    public static MeshingParameters DocumentCurrentSetting(RhinoDoc doc)
    {
      if (doc == null) throw new ArgumentNullException("doc");

      // this was a redundant construction
      return doc.GetMeshingParameters(doc.MeshingParameterStyle);
    }
#endif

    /// <summary>Gets minimal meshing parameters.</summary>
    /// <example>
    /// <code source='examples\vbnet\ex_createmeshfrombrep.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_createmeshfrombrep.cs' lang='cs'/>
    /// <code source='examples\py\ex_createmeshfrombrep.py' lang='py'/>
    /// </example>
    public static MeshingParameters Minimal
    {
      get
      {
        var mp = new MeshingParameters
        {
          JaggedSeams = true,
          RefineGrid = false,
          SimplePlanes = false,
          ComputeCurvature = false,
          GridMinCount = 16,
          GridMaxCount = 0,
          GridAmplification = 1.0,
          GridAngle = 0.0,
          GridAspectRatio = 6.0,
          Tolerance = 0.0,
          MinimumTolerance = 0.0,
          MinimumEdgeLength = 0.0001,
          MaximumEdgeLength = 0.0,
          RefineAngle = 0.0,
          RelativeTolerance = 0.0
        };
        return mp;
      }
    }

    /// <summary>
    /// Gets mesh creation parameters to create the default render mesh.
    /// Only use this if you plan on specifying your own custom meshing
    /// parameters.
    /// </summary>
    /// <example>
    /// <code source='examples\vbnet\ex_createmeshfrombrep.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_createmeshfrombrep.cs' lang='cs'/>
    /// <code source='examples\py\ex_createmeshfrombrep.py' lang='py'/>
    /// </example>
    public static MeshingParameters Default
    {
      get
      {
        var mp = new MeshingParameters();
        return mp;
      }
    }

    /// <summary>
    /// Gets mesh creation parameters for coarse meshing. 
    /// <para>This corresponds with the "Jagged and Faster" default in Rhino.</para>
    /// </summary>
    /// <example>
    /// <code source='examples\vbnet\ex_createmeshfrombrep.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_createmeshfrombrep.cs' lang='cs'/>
    /// <code source='examples\py\ex_createmeshfrombrep.py' lang='py'/>
    /// </example>
    [Obsolete("Use MeshingParameters.FastRenderMesh")]
    public static MeshingParameters Coarse
    {
      get
      {
        var mp = new MeshingParameters
        {
          GridAmplification = 0.0,
          GridAngle = 0.0,
          GridAspectRatio = 0.0,
          RefineAngle = 0.0,
          RelativeTolerance = 0.65,
          GridMinCount = 16,
          MinimumEdgeLength = 0.0001,
          SimplePlanes = true
        };
        return mp;
      }
    }

    /// <summary>
    /// Gets mesh creation parameters to create the a render mesh when 
    /// meshing speed is prefered over mesh quality.
    /// </summary>
    public static MeshingParameters FastRenderMesh
    {
      get
      {
        var ptr = UnsafeNativeMethods.ON_MeshParameters_FastRenderMesh();
        var mp = new MeshingParameters(ptr);
        return mp;
      }
    }

    /// <summary>
    /// Gets mesh creation parameters for smooth meshing. 
    /// <para>This corresponds with the "Smooth and Slower" default in Rhino.</para>
    /// </summary>
    /// <example>
    /// <code source='examples\vbnet\ex_createmeshfrombrep.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_createmeshfrombrep.cs' lang='cs'/>
    /// <code source='examples\py\ex_createmeshfrombrep.py' lang='py'/>
    /// </example>
    [Obsolete("Use MeshingParameters.QualityRenderMesh")]
    public static MeshingParameters Smooth
    {
      get
      {
        var mp = new MeshingParameters
        {
          GridAmplification = 0.0,
          GridAngle = 0.0,
          GridAspectRatio = 0.0,
          RelativeTolerance = 0.8,
          GridMinCount = 16,
          MinimumEdgeLength = 0.0001,
          SimplePlanes = true,
          RefineAngle = (20.0 * Math.PI) / 180.0
        };
        return mp;
      }
    }

    /// <summary>
    /// Gets mesh creation parameters to create the a render mesh when mesh 
    /// quality is prefered over meshing speed.
    /// </summary>
    public static MeshingParameters QualityRenderMesh
    {
      get
      {
        var ptr = UnsafeNativeMethods.ON_MeshParameters_QualityRenderMesh();
        var mp = new MeshingParameters(ptr);
        return mp;
      }
    }

    /// <summary>
    /// Gets mesh creation parameters to create the default analysis mesh.
    /// </summary>
    public static MeshingParameters DefaultAnalysisMesh
    {
      get
      {
        var ptr = UnsafeNativeMethods.ON_MeshParameters_DefaultAnalysisMesh();
        var mp = new MeshingParameters(ptr);
        return mp;
      }
    }

    #endregion

    #region properties

    /// <summary>
    /// Gets or sets how and if textures will be packed.
    /// </summary>
    public MeshingParameterTextureRange TextureRange
    {
      get
      {
        IntPtr ptr = ConstPointer();
        return (MeshingParameterTextureRange)UnsafeNativeMethods.ON_MeshParameters_GetTextureRange(ptr);
      }
      set
      {
        if (!Enum.IsDefined(typeof(MeshingParameterTextureRange), value))
        {
          throw new ArgumentOutOfRangeException(nameof(value));
        }

        IntPtr ptr = NonConstPointer();
        if(!UnsafeNativeMethods.ON_MeshParameters_SetTextureRange(ptr, (uint)value))
        {
          throw new NotSupportedException("Could not set texture range.");
        }
      }
    }

    bool GetBool(UnsafeNativeMethods.MeshParametersBoolConst which)
    {
      IntPtr ptr = ConstPointer();
      return UnsafeNativeMethods.ON_MeshParameters_GetBool(ptr, which);
    }

    void SetBool(UnsafeNativeMethods.MeshParametersBoolConst which, bool val)
    {
      IntPtr ptr = NonConstPointer();
      UnsafeNativeMethods.ON_MeshParameters_SetBool(ptr, which, val);
    }

    /// <summary>
    /// Gets or sets whether or not the mesh is allowed to have jagged seams. 
    /// When this flag is set to true, meshes on either side of a Brep Edge will not match up.
    /// </summary>
    public bool JaggedSeams
    {
      get { return GetBool(UnsafeNativeMethods.MeshParametersBoolConst.JaggedSeams); }
      set { SetBool(UnsafeNativeMethods.MeshParametersBoolConst.JaggedSeams, value); }
    }

    /// <summary>
    /// Gets or sets a value indicating whether or not the sampling grid can be refined 
    /// when certain tolerances are not met.
    /// </summary>
    public bool RefineGrid
    {
      get { return GetBool(UnsafeNativeMethods.MeshParametersBoolConst.RefineGrid); }
      set { SetBool(UnsafeNativeMethods.MeshParametersBoolConst.RefineGrid, value); }
    }

    /// <summary>
    /// Gets or sets a value indicating whether or not planar areas are allowed 
    /// to be meshed in a simplified manner.
    /// </summary>
    public bool SimplePlanes
    {
      get { return GetBool(UnsafeNativeMethods.MeshParametersBoolConst.SimplePlanes); }
      set { SetBool(UnsafeNativeMethods.MeshParametersBoolConst.SimplePlanes, value); }
    }

    /// <summary>
    /// Gets or sets a value indicating whether or not surface curvature 
    /// data will be embedded in the mesh.
    /// </summary>
    public bool ComputeCurvature
    {
      get { return GetBool(UnsafeNativeMethods.MeshParametersBoolConst.ComputeCurvature); }
      set { SetBool(UnsafeNativeMethods.MeshParametersBoolConst.ComputeCurvature, value); }
    }

    /// <summary>
    /// Gets or sets a value indicating whether or not to post process non-closed meshes
    /// that should be closed. If the Brep being meshed is closed, JaggedSeams = false, 
    /// and ClosedObjectPostProcess = true, and the resulting mesh is not closed, then a
    /// post meshing process is applied to find and close gaps in the mesh. Typically the
    /// resulting mesh is not closed because the input Brep has a geometric flaw, like
    /// loops in trimming curve.
    /// </summary>
    public bool ClosedObjectPostProcess
    {
      get { return GetBool(UnsafeNativeMethods.MeshParametersBoolConst.ClosedObjectPostProcess); }
      set { SetBool(UnsafeNativeMethods.MeshParametersBoolConst.ClosedObjectPostProcess, value); }
    }

    int GetGridCount(bool min)
    {
      IntPtr ptr = ConstPointer();
      return UnsafeNativeMethods.ON_MeshParameters_GetGridCount(ptr, min);
    }

    void SetGridCount(bool min, int val)
    {
      IntPtr ptr = NonConstPointer();
      UnsafeNativeMethods.ON_MeshParameters_SetGridCount(ptr, min, val);
    }

    /// <summary>
    /// Gets or sets the minimum number of grid quads in the initial sampling grid.
    /// </summary>
    public int GridMinCount
    {
      get { return GetGridCount(true); }
      set { SetGridCount(true, value); }
    }

    /// <summary>
    /// Gets or sets the maximum number of grid quads in the initial sampling grid.
    /// </summary>
    public int GridMaxCount
    {
      get { return GetGridCount(false); }
      set { SetGridCount(false, value); }
    }

    double GetDouble(UnsafeNativeMethods.MeshParametersDoubleConst which)
    {
      IntPtr ptr = ConstPointer();
      return UnsafeNativeMethods.ON_MeshParameters_GetDouble(ptr, which);
    }

    void SetDouble(UnsafeNativeMethods.MeshParametersDoubleConst which, double val)
    {
      IntPtr ptr = NonConstPointer();
      UnsafeNativeMethods.ON_MeshParameters_SetDouble(ptr, which, val);
    }

    /// <summary>
    /// Gets or sets the maximum allowed angle difference (in radians) 
    /// for a single sampling quad. The angle pertains to the surface normals.
    /// </summary>
    public double GridAngle
    {
      get { return GetDouble(UnsafeNativeMethods.MeshParametersDoubleConst.GridAngle); }
      set { SetDouble(UnsafeNativeMethods.MeshParametersDoubleConst.GridAngle, value); }
    }

    /// <summary>
    /// Gets or sets the maximum allowed aspect ratio of sampling quads.
    /// </summary>
    public double GridAspectRatio
    {
      get { return GetDouble(UnsafeNativeMethods.MeshParametersDoubleConst.GridAspectRatio); }
      set { SetDouble(UnsafeNativeMethods.MeshParametersDoubleConst.GridAspectRatio, value); }
    }

    /// <summary>
    /// Gets or sets the grid amplification factor. 
    /// Values lower than 1.0 will decrease the number of initial quads, 
    /// values higher than 1.0 will increase the number of initial quads.
    /// </summary>
    public double GridAmplification
    {
      get { return GetDouble(UnsafeNativeMethods.MeshParametersDoubleConst.GridAmplification); }
      set { SetDouble(UnsafeNativeMethods.MeshParametersDoubleConst.GridAmplification, value); }
    }

    /// <summary>
    /// Gets or sets the maximum allowed edge deviation. 
    /// This tolerance is measured between the center of the mesh edge and the surface.
    /// </summary>
    public double Tolerance
    {
      get { return GetDouble(UnsafeNativeMethods.MeshParametersDoubleConst.Tolerance); }
      set { SetDouble(UnsafeNativeMethods.MeshParametersDoubleConst.Tolerance, value); }
    }

    /// <summary>
    /// Gets or sets the minimum tolerance.
    /// </summary>
    public double MinimumTolerance
    {
      get { return GetDouble(UnsafeNativeMethods.MeshParametersDoubleConst.MinimumTolerance); }
      set { SetDouble(UnsafeNativeMethods.MeshParametersDoubleConst.MinimumTolerance, value); }
    }

    /// <summary>
    /// Gets or sets the relative tolerance.
    /// </summary>
    public double RelativeTolerance
    {
      get { return GetDouble(UnsafeNativeMethods.MeshParametersDoubleConst.RelativeTolerance); }
      set { SetDouble(UnsafeNativeMethods.MeshParametersDoubleConst.RelativeTolerance, value); }
    }

    /// <summary>
    /// Gets or sets the minimum allowed mesh edge length.
    /// </summary>
    public double MinimumEdgeLength
    {
      get { return GetDouble(UnsafeNativeMethods.MeshParametersDoubleConst.MinimumEdgeLength); }
      set { SetDouble(UnsafeNativeMethods.MeshParametersDoubleConst.MinimumEdgeLength, value); }
    }

    /// <summary>
    /// Gets or sets the maximum allowed mesh edge length.
    /// </summary>
    public double MaximumEdgeLength
    {
      get { return GetDouble(UnsafeNativeMethods.MeshParametersDoubleConst.MaximumEdgeLength); }
      set { SetDouble(UnsafeNativeMethods.MeshParametersDoubleConst.MaximumEdgeLength, value); }
    }

    /// <summary>
    /// Gets or sets the mesh parameter refine angle.
    /// </summary>
    public double RefineAngle
    {
      get { return GetDouble(UnsafeNativeMethods.MeshParametersDoubleConst.RefineAngle); }
      set { SetDouble(UnsafeNativeMethods.MeshParametersDoubleConst.RefineAngle, value); }
    }


    #endregion
  }

  /// <summary>
  /// Represents a portion of a mesh for partitioning
  /// </summary>
  public class MeshPart
  {
    private readonly int m_vi0;
    private readonly int m_vi1;
    private readonly int m_fi0;
    private readonly int m_fi1;
    private readonly int m_vertex_count;
    private readonly int m_triangle_count;

    internal MeshPart(int vertexStart, int vertexEnd, int faceStart, int faceEnd, int vertexCount, int triangleCount)
    {
      m_vi0 = vertexStart;
      m_vi1 = vertexEnd;
      m_fi0 = faceStart;
      m_fi1 = faceEnd;
      m_vertex_count = vertexCount;
      m_triangle_count = triangleCount;
    }

    /// <summary>Start of subinterval of parent mesh vertex array</summary>
    public int StartVertexIndex { get { return m_vi0; } }
    /// <summary>End of subinterval of parent mesh vertex array</summary>
    public int EndVertexIndex { get { return m_vi1; } }
    /// <summary>Start of subinterval of parent mesh face array</summary>
    public int StartFaceIndex { get { return m_fi0; } }
    /// <summary>End of subinterval of parent mesh face array</summary>
    public int EndFaceIndex { get { return m_fi1; } }

    /// <summary>EndVertexIndex - StartVertexIndex</summary>
    public int VertexCount { get { return m_vertex_count; } }
    /// <summary></summary>
    public int TriangleCount { get { return m_triangle_count; } }
  }

  /// <summary>
  /// Thickness measurement used in the mesh thickness solver.
  /// </summary>
  [StructLayout(LayoutKind.Sequential)]
  public struct MeshThicknessMeasurement
  {
    #region fields
    private readonly int _meshIndex;
    private readonly int _vertexIndex;
    private readonly double _thickness;
    private readonly Point3d _point0;
    private readonly Point3d _point1;
    #endregion

    #region constructor
    /// <summary>
    /// Create a new thickness measurement.
    /// </summary>
    /// <param name="meshIndex">Index of mesh within collection of meshes.</param>
    /// <param name="vertexIndex">Index of mesh vertex.</param>
    /// <param name="thickness">Thickness of mesh at vertex.</param>
    /// <param name="point">Vertex location.</param>
    /// <param name="oppositePoint">Opposite location.</param>
    public MeshThicknessMeasurement(int meshIndex, int vertexIndex, double thickness, Point3d point, Point3d oppositePoint)
    {
      _meshIndex = meshIndex;
      _vertexIndex = vertexIndex;
      _thickness = thickness;
      _point0 = point;
      _point1 = oppositePoint;
    }
    #endregion

    #region properties
    /// <summary>
    /// Gets the index of the mesh associated with this thickness measurement.
    /// </summary>
    public int MeshIndex
    {
      get { return _meshIndex; }
    }
    /// <summary>
    /// Gets the index of the vertex associated with this thickness measurement.
    /// </summary>
    public int VertexIndex
    {
      get { return _vertexIndex; }
    }
    /// <summary>
    /// Gets the local thickness of the mesh.
    /// </summary>
    public double Thickness
    {
      get { return _thickness; }
    }
    /// <summary>
    /// Gets the location of the thickness measurement.
    /// </summary>
    public Point3d Point
    {
      get { return _point0; }
    }
    /// <summary>
    /// Gets the point opposite to the measurement point.
    /// </summary>
    public Point3d OppositePoint
    {
      get { return _point1; }
    }
    #endregion
  }

  /// <summary>
  /// Parameters for Reduce method
  /// </summary>
  public class ReduceMeshParameters
  {
    /// <summary>
    /// Constructs a polygon reduction parameter object with default values.
    /// Users of this class should not rely on default values to stay constant
    /// across service releases.
    /// </summary>
    public ReduceMeshParameters()
    {
      DesiredPolygonCount = 0;
      AllowDistortion = false;
      Accuracy = 10;
      NormalizeMeshSize = false;
      Error = string.Empty;
      FaceTags = null;
      LockedComponents = null;
      CancelToken = CancellationToken.None;
      ProgressReporter = null;
    }

    /// <summary>Desired or target number of faces</summary>
    public int DesiredPolygonCount { get; set; }

    /// <summary>If true mesh appearance is not changed even if the target polygon count is not reached</summary>
    public bool AllowDistortion { get; set; }

    /// <summary>Integer from 1 to 10 telling how accurate reduction algorithm
    /// to use. Greater number gives more accurate results</summary>
    public int Accuracy { get; set; }

    /// <summary>If true mesh is fitted to an axis aligned unit cube until reduction is complete</summary>
    public bool NormalizeMeshSize { get; set; }

    /// <summary></summary>
    public string Error { get; internal set; }

    /// <summary></summary>
    public int[] FaceTags { get; set; }

    /// <summary>List of topological mesh vertices and mesh vertices that will not be moved or deleted in reduction process.
    /// Each mesh vertex will lock the corresponding topological mesh vertex. In other words it is not possible to have a
    /// locked and non-locked mesh vertex at the same location.</summary>
    public ComponentIndex[] LockedComponents { get; set; }

    /// <summary></summary>
    public CancellationToken CancelToken { get; set; }

    /// <summary></summary>
    public IProgress<double> ProgressReporter { get; set; }
  }

  /// <summary>
  /// Represents a geometry type that is defined by vertices and faces.
  /// <para>This is often called a face-vertex mesh.</para>
  /// </summary>
  [Serializable]
  public partial class Mesh : GeometryBase
  {
    #region static mesh creation
#if RHINO_SDK
    /// <summary>
    /// Constructs a planar mesh grid.
    /// </summary>
    /// <param name="plane">Plane of mesh.</param>
    /// <param name="xInterval">Interval describing size and extends of mesh along plane x-direction.</param>
    /// <param name="yInterval">Interval describing size and extends of mesh along plane y-direction.</param>
    /// <param name="xCount">Number of faces in x-direction.</param>
    /// <param name="yCount">Number of faces in y-direction.</param>
    /// <exception cref="ArgumentException">Thrown when plane is a null reference.</exception>
    /// <exception cref="ArgumentException">Thrown when xInterval is a null reference.</exception>
    /// <exception cref="ArgumentException">Thrown when yInterval is a null reference.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when xCount is less than or equal to zero.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when yCount is less than or equal to zero.</exception>
    public static Mesh CreateFromPlane(Plane plane, Interval xInterval, Interval yInterval, int xCount, int yCount)
    {
      if (!plane.IsValid) { throw new ArgumentException("plane is invalid"); }
      if (!xInterval.IsValid) { throw new ArgumentException("xInterval is invalid"); }
      if (!yInterval.IsValid) { throw new ArgumentException("yInterval is invalid"); }
      if (xCount <= 0) { throw new ArgumentOutOfRangeException("xCount"); }
      if (yCount <= 0) { throw new ArgumentOutOfRangeException("yCount"); }

      IntPtr ptr = UnsafeNativeMethods.ON_Mesh_CreateMeshPlane(ref plane, xInterval, yInterval, xCount, yCount);

      if (IntPtr.Zero == ptr)
        return null;
      return new Mesh(ptr, null);
    }

    /// <summary>
    /// Constructs new mesh that matches a bounding box.
    /// </summary>
    /// <param name="box">A box to use for creation.</param>
    /// <param name="xCount">Number of faces in x-direction.</param>
    /// <param name="yCount">Number of faces in y-direction.</param>
    /// <param name="zCount">Number of faces in z-direction.</param>
    /// <returns>A new brep, or null on failure.</returns>
    public static Mesh CreateFromBox(BoundingBox box, int xCount, int yCount, int zCount)
    {
      IntPtr ptr = UnsafeNativeMethods.RHC_RhinoMeshBox2(box.Min, box.Max, xCount, yCount, zCount);
      return IntPtr.Zero == ptr ? null : new Mesh(ptr, null);
    }

    /// <summary>
    ///  Constructs new mesh that matches an aligned box.
    /// </summary>
    /// <param name="box">Box to match.</param>
    /// <param name="xCount">Number of faces in x-direction.</param>
    /// <param name="yCount">Number of faces in y-direction.</param>
    /// <param name="zCount">Number of faces in z-direction.</param>
    /// <returns></returns>
    public static Mesh CreateFromBox(Box box, int xCount, int yCount, int zCount)
    {
      return CreateFromBox(box.GetCorners(), xCount, yCount, zCount);
    }

    /// <summary>
    /// Constructs new mesh from 8 corner points.
    /// </summary>
    /// <param name="corners">
    /// 8 points defining the box corners arranged as the vN labels indicate.
    /// <pre>
    /// <para>v7_____________v6</para>
    /// <para>|\             |\</para>
    /// <para>| \            | \</para>
    /// <para>|  \ _____________\</para>
    /// <para>|   v4         |   v5</para>
    /// <para>|   |          |   |</para>
    /// <para>|   |          |   |</para>
    /// <para>v3--|----------v2  |</para>
    /// <para> \  |           \  |</para>
    /// <para>  \ |            \ |</para>
    /// <para>   \|             \|</para>
    /// <para>    v0_____________v1</para>
    /// </pre>
    /// </param>
    /// <param name="xCount">Number of faces in x-direction.</param>
    /// <param name="yCount">Number of faces in y-direction.</param>
    /// <param name="zCount">Number of faces in z-direction.</param>
    /// <returns>A new brep, or null on failure.</returns>
    /// <returns>A new box mesh, on null on error.</returns>
    public static Mesh CreateFromBox(IEnumerable<Point3d> corners, int xCount, int yCount, int zCount)
    {
      var box_corners = new Point3d[8];
      if (corners == null) { return null; }

      int i = 0;
      foreach (Point3d p in corners)
      {
        box_corners[i] = p;
        i++;
        if (8 == i) { break; }
      }

      if (i < 8) { return null; }

      var ptr = UnsafeNativeMethods.RHC_RhinoMeshBox(box_corners, xCount, yCount, zCount);
      return IntPtr.Zero == ptr ? null : new Mesh(ptr, null);
    }

    /// <summary>
    /// Constructs a mesh sphere.
    /// </summary>
    /// <param name="sphere">Base sphere for mesh.</param>
    /// <param name="xCount">Number of faces in the around direction.</param>
    /// <param name="yCount">Number of faces in the top-to-bottom direction.</param>
    /// <exception cref="ArgumentException">Thrown when sphere is invalid.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when xCount is less than or equal to two.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when yCount is less than or equal to two.</exception>
    /// <returns></returns>
    public static Mesh CreateFromSphere(Sphere sphere, int xCount, int yCount)
    {
      if (!sphere.IsValid) { throw new ArgumentException("sphere is invalid"); }
      if (xCount < 2) { throw new ArgumentOutOfRangeException("xCount"); }
      if (yCount < 2) { throw new ArgumentOutOfRangeException("yCount"); }

      IntPtr ptr = UnsafeNativeMethods.ON_Mesh_CreateMeshSphere(ref sphere.m_plane, sphere.m_radius, xCount, yCount);

      if (IntPtr.Zero == ptr)
        return null;
      return new Mesh(ptr, null);
    }

    /// <summary>
    /// Constructs a icospherical mesh. A mesh icosphere differs from a standard
    /// UV mesh sphere in that it's vertices are evenly distributed. A mesh icosphere
    /// starts from an icosahedron (a regular polyhedron with 20 equilateral triangles).
    /// It is then refined by splitting each triangle into 4 smaller triangles.
    /// This splitting can be done several times.
    /// </summary>
    /// <param name="sphere">The input sphere provides the orienting plane and radius.</param>
    /// <param name="subdivisions">
    /// The number of times you want the faces split, where 0  &lt;= subdivisions &lt;= 7. 
    /// Note, the total number of mesh faces produces is: 20 * (4 ^ subdivisions)
    /// </param>
    /// <returns>A welded mesh icosphere if successful, or null on failure.</returns>
    public static Mesh CreateIcoSphere(Sphere sphere, int subdivisions)
    {
      if (!sphere.IsValid) { throw new ArgumentException("sphere is invalid"); }
      IntPtr ptr = UnsafeNativeMethods.RHC_RhinoMeshIcoSphere(ref sphere, subdivisions);
      if (IntPtr.Zero == ptr)
        return null;
      return new Mesh(ptr, null);
    }

    /// <summary>
    /// Constructs a quad mesh sphere. A quad mesh sphere differs from a standard
    /// UV mesh sphere in that it's vertices are evenly distributed. A quad mesh sphere
    /// starts from a cube (a regular polyhedron with 6 square sides).
    /// It is then refined by splitting each quad into 4 smaller quads.
    /// This splitting can be done several times.
    /// </summary>
    /// <param name="sphere">The input sphere provides the orienting plane and radius.</param>
    /// <param name="subdivisions">
    /// The number of times you want the faces split, where 0  &lt;= subdivisions &lt;= 8. 
    /// Note, the total number of mesh faces produces is: 6 * (4 ^ subdivisions)
    /// </param>
    /// <returns>A welded quad mesh sphere if successful, or null on failure.</returns>
    public static Mesh CreateQuadSphere(Sphere sphere, int subdivisions)
    {
      if (!sphere.IsValid) { throw new ArgumentException("sphere is invalid"); }
      IntPtr ptr = UnsafeNativeMethods.RHC_RhinoMeshQuadSphere(ref sphere, subdivisions);
      if (IntPtr.Zero == ptr)
        return null;
      return new Mesh(ptr, null);
    }

    /// <summary>Constructs a mesh cylinder</summary>
    /// <param name="cylinder"></param>
    /// <param name="vertical">Number of faces in the top-to-bottom direction</param>
    /// <param name="around">Number of faces around the cylinder</param>
    /// <exception cref="ArgumentException">Thrown when cylinder is invalid.</exception>
    /// <returns></returns>
    public static Mesh CreateFromCylinder(Cylinder cylinder, int vertical, int around)
    {
      if (!cylinder.IsValid) { throw new ArgumentException("cylinder is invalid"); }
      IntPtr ptr_mesh = UnsafeNativeMethods.RHC_RhinoMeshCylinder(ref cylinder, vertical, around);
      return CreateGeometryHelper(ptr_mesh, null) as Mesh;
    }

    /// <summary>Constructs a solid mesh cone.</summary>
    /// <param name="cone"></param>
    /// <param name="vertical">Number of faces in the top-to-bottom direction.</param>
    /// <param name="around">Number of faces around the cone.</param>
    /// <exception cref="ArgumentException">Thrown when cone is invalid.</exception>
    /// <returns>A valid mesh if successful.</returns>
    public static Mesh CreateFromCone(Cone cone, int vertical, int around)
    {
      return CreateFromCone(cone, vertical, around, true);
    }

    /// <summary>Constructs a mesh cone.</summary>
    /// <param name="cone"></param>
    /// <param name="vertical">Number of faces in the top-to-bottom direction.</param>
    /// <param name="around">Number of faces around the cone.</param>
    /// <param name="solid">If false the mesh will be open with no faces on the circular planar portion.</param>
    /// <exception cref="ArgumentException">Thrown when cone is invalid.</exception>
    /// <returns>A valid mesh if successful.</returns>
    public static Mesh CreateFromCone(Cone cone, int vertical, int around, bool solid)
    {
      if (!cone.IsValid) { throw new ArgumentException("cone is invalid"); }
      IntPtr ptr_mesh = UnsafeNativeMethods.RHC_RhinoMeshCone(ref cone, vertical, around, solid);
      return CreateGeometryHelper(ptr_mesh, null) as Mesh;
    }

    /// <summary>
    /// Do not use this overload. Use version that takes a tolerance parameter instead.
    /// </summary>
    /// <param name="boundary">Do not use.</param>
    /// <param name="parameters">Do not use.</param>
    /// <returns>
    /// Do not use.
    /// </returns>
    [Obsolete("Use version that takes a tolerance parameter instead")]
    public static Mesh CreateFromPlanarBoundary(Curve boundary, MeshingParameters parameters)
    {
      const double bogus_tolerance = 0.001; // see RH-21770

      return CreateFromPlanarBoundary(boundary, parameters, bogus_tolerance);
    }

    /// <summary>
    /// Attempts to construct a mesh from a closed planar curve.RhinoMakePlanarMeshes
    /// </summary>
    /// <param name="boundary">must be a closed planar curve.</param>
    /// <param name="parameters">parameters used for creating the mesh.</param>
    /// <param name="tolerance">Tolerance to use during operation.</param>
    /// <returns>
    /// New mesh on success or null on failure.
    /// </returns>
    public static Mesh CreateFromPlanarBoundary(Curve boundary, MeshingParameters parameters, double tolerance)
    {
      IntPtr ptr_const_curve = boundary.ConstPointer();
      IntPtr ptr_const_mesh_parameters = parameters.ConstPointer();
      IntPtr ptr_mesh = UnsafeNativeMethods.RHC_RhinoMakePlanarMeshes(ptr_const_curve, ptr_const_mesh_parameters, tolerance);
      GC.KeepAlive(boundary);
      GC.KeepAlive(parameters);
      return CreateGeometryHelper(ptr_mesh, null) as Mesh;
    }

    /// <summary>
    /// Attempts to create a Mesh that is a triangulation of a closed polyline.
    /// </summary>
    /// <param name="polyline">must be closed</param>
    /// <returns>
    /// New mesh on success or null on failure.
    /// </returns>
    public static Mesh CreateFromClosedPolyline(Polyline polyline)
    {
      if (!polyline.IsClosed)
        return null;
      var rc = new Mesh();
      var ptr_mesh = rc.NonConstPointer();
      if (UnsafeNativeMethods.TLC_MeshPolyline(polyline.Count, polyline.ToArray(), ptr_mesh))
        return rc;
      return null;
    }

    /// <summary>
    /// Attempts to create a mesh that is a triangulation of a list of points, projected on a plane,
    /// including its holes and fixed edges.
    /// </summary>
    /// <param name="points">A list, an array or any enumerable of points.</param>
    /// <param name="plane">A plane.</param>
    /// <param name="allowNewVertices">If true, the mesh might have more vertices than the list of input points,
    /// if doing so will improve long thin triangles.</param>
    /// <param name="edges">A list of polylines, or other lists of points representing edges.
    /// This can be null. If nested enumerable items are null, they will be discarded.</param>
    /// <returns>A new mesh, or null if not successful.</returns>
    /// <exception cref="ArgumentNullException">If points is null.</exception>
    /// <exception cref="ArgumentException">If plane is not valid.</exception>
    public static Mesh CreateFromTessellation(IEnumerable<Point3d> points,
      IEnumerable<IEnumerable<Point3d>> edges, Plane plane, bool allowNewVertices)
    {
      if (points == null) throw new ArgumentNullException("points");
      if (!plane.IsValid) throw new ArgumentException("The supplied plane is not valid.", "plane");

      int count_pt;
      var array_pt = RhinoListHelpers.GetConstArray(points, out count_pt);

      IntPtr ptr_mesh;
      using (var marshallable_edges = ArrayOfTArrayMarshal.Create(edges, NullItemsResponse.RemoveNulls))
      {
        ptr_mesh = UnsafeNativeMethods.RHC_Mesh_CreateFromTessellation(
          count_pt,
          array_pt,
          ref plane,
          allowNewVertices,
          marshallable_edges.Length,
          marshallable_edges.LengthsOfPinnedObjects,
          marshallable_edges.AddressesOfPinnedObjects
          );
      }

      return ptr_mesh == IntPtr.Zero ? null : new Mesh(ptr_mesh, null);
    }

    /// <summary>
    /// Constructs a mesh from a brep.
    /// </summary>
    /// <param name="brep">Brep to approximate.</param>
    /// <returns>An array of meshes.</returns>
    /// <example>
    /// <code source='examples\vbnet\ex_tightboundingbox.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_tightboundingbox.cs' lang='cs'/>
    /// <code source='examples\py\ex_tightboundingbox.py' lang='py'/>
    /// </example>
    [Obsolete("Use version that takes MeshingParameters as input")]
    public static Mesh[] CreateFromBrep(Brep brep)
    {
      using (var meshes = new SimpleArrayMeshPointer())
      {
        IntPtr ptr_const_brep = brep.ConstPointer();
        IntPtr ptr_mesh_array = meshes.NonConstPointer();
        int count = UnsafeNativeMethods.ON_Brep_CreateMesh(ptr_const_brep, ptr_mesh_array);
        GC.KeepAlive(brep);
        return count < 1 ? null : meshes.ToNonConstArray();
      }
    }

    /// <summary>
    /// Constructs a mesh from a brep.
    /// </summary>
    /// <param name="brep">Brep to approximate.</param>
    /// <param name="meshingParameters">Parameters to use during meshing.</param>
    /// <returns>An array of meshes.</returns>
    /// <example>
    /// <code source='examples\vbnet\ex_createmeshfrombrep.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_createmeshfrombrep.cs' lang='cs'/>
    /// <code source='examples\py\ex_createmeshfrombrep.py' lang='py'/>
    /// </example>
    public static Mesh[] CreateFromBrep(Brep brep, MeshingParameters meshingParameters)
    {
      IntPtr ptr_const_brep = brep.ConstPointer();
      IntPtr const_ptr_meshing_parameters = meshingParameters.ConstPointer();
      using (var meshes = new SimpleArrayMeshPointer())
      {
        IntPtr ptr_mesh_array = meshes.NonConstPointer();
        int count = UnsafeNativeMethods.ON_Brep_CreateMesh3(ptr_const_brep, ptr_mesh_array, const_ptr_meshing_parameters);
        GC.KeepAlive(brep);
        GC.KeepAlive(meshingParameters);
        return count < 1 ? null : meshes.ToNonConstArray();
      }
    }

    /// <summary>
    /// Constructs a mesh from a surface
    /// </summary>
    /// <param name="surface">Surface to approximate</param>
    /// <returns>New mesh representing the surface</returns>
    public static Mesh CreateFromSurface(Surface surface)
    {
      return CreateFromSurface(surface, MeshingParameters.Default);
    }

    /// <summary>
    /// Constructs a mesh from a surface
    /// </summary>
    /// <param name="surface">Surface to approximate</param>
    /// <param name="meshingParameters">settings used to create the mesh</param>
    /// <returns>New mesh representing the surface</returns>
    public static Mesh CreateFromSurface(Surface surface, MeshingParameters meshingParameters)
    {
      IntPtr ptr_const_surface = surface.ConstPointer();
      IntPtr ptr_const_mp = meshingParameters == null ? IntPtr.Zero : meshingParameters.ConstPointer();
      IntPtr ptr_mesh = UnsafeNativeMethods.ON_Surface_CreateMesh(ptr_const_surface, ptr_const_mp);
      if (ptr_mesh == IntPtr.Zero)
        return null;
      GC.KeepAlive(surface);
      GC.KeepAlive(meshingParameters);
      return new Mesh(ptr_mesh, null);
    }

    /// <summary>
    /// Construct a mesh patch from a variety of input geometry.
    /// </summary>
    /// <param name="outerBoundary">(optional: can be null) Outer boundary
    /// polyline, if provided this will become the outer boundary of the
    /// resulting mesh. Any of the input that is completely outside the outer
    /// boundary will be ignored and have no impact on the result. If any of
    /// the input intersects the outer boundary the result will be
    /// unpredictable and is likely to not include the entire outer boundary.
    /// </param>
    /// <param name="angleToleranceRadians">
    /// Maximum angle between unit tangents and adjacent verticies. Used to
    /// divide curve inputs that cannot otherwise be represented as a polyline.
    /// </param>
    /// <param name="innerBoundaryCurves">
    /// (optional: can be null) Polylines to create holes in the output mesh.
    /// If innerBoundaryCurves are the only input then the result may be null
    /// if trimback is set to false (see comments for trimback) because the
    /// resulting mesh could be invalid (all faces created contained vertexes
    /// from the perimeter boundary).
    /// </param>
    /// <param name="pullbackSurface">
    /// (optional: can be null) Initial surface where 3d input will be pulled
    /// to make a 2d representation used by the function that generates the mesh.
    /// Providing a pullbackSurface can be helpful when it is similar in shape
    /// to the pattern of the input, the pulled 2d points will be a better
    /// representation of the 3d points. If all of the input is more or less
    /// coplanar to start with, providing pullbackSurface has no real benefit.
    /// </param>
    /// <param name="innerBothSideCurves">
    /// (optional: can be null) These polylines will create faces on both sides
    /// of the edge. If there are only input points(innerPoints) there is no
    /// way to guarantee a triangulation that will create an edge between two
    /// particular points. Adding a line, or polyline, to innerBothsideCurves
    /// that includes points from innerPoints will help guide the triangulation.
    /// </param>
    /// <param name="innerPoints">
    /// (optional: can be null) Points to be used to generate the mesh. If
    /// outerBoundary is not null, points outside of that boundary after it has
    /// been pulled to pullbackSurface (or the best plane through the input if
    /// pullbackSurface is null) will be ignored.
    /// </param>
    /// <param name="trimback">
    /// Only used when a outerBoundary has not been provided. When that is the
    /// case, the function uses the perimeter of the surface as the outer boundary
    /// instead. If true, any face of the resulting triangulated mesh that
    /// contains a vertex of the perimeter boundary will be removed.
    /// </param>
    /// <param name="divisions">
    /// Only used when a outerBoundary has not been provided. When that is the
    /// case, division becomes the number of divisions each side of the surface's
    /// perimeter will be divided into to create an outer boundary to work with.
    /// </param>
    /// <returns>mesh on success; null on failure</returns>
    public static Mesh CreatePatch(Polyline outerBoundary, double angleToleranceRadians, Surface pullbackSurface,
      IEnumerable<Curve> innerBoundaryCurves, IEnumerable<Curve> innerBothSideCurves, IEnumerable<Point3d> innerPoints,
      bool trimback, int divisions)
    {
      //Deal with outer boundary if it exists
      IntPtr const_ptr_outer_boundary = IntPtr.Zero;
      PolylineCurve outer_boundary_curve = null;
      if (outerBoundary != null)
      {
        if (!outerBoundary.IsClosed)
          throw new ArgumentException("outerBoundary", "The outerBoundary must be closed");

        outer_boundary_curve = new PolylineCurve(outerBoundary);
        const_ptr_outer_boundary = outer_boundary_curve.ConstPointer();
      }

      //Deal with holes if they exist
      var innerCrvs = new List<PolylineCurve>();
      if (innerBoundaryCurves != null)
      {
        foreach (var crv in innerBoundaryCurves)
          if (crv.IsClosed)
          {
            PolylineCurve polyline_crv = crv as PolylineCurve;
            if (polyline_crv != null)
            {
              innerCrvs.Add(polyline_crv);
            }
            else
            {
              innerCrvs.Add(crv.ToPolyline(0, 0, angleToleranceRadians, 0, 0, 0, 0, 0, true));
            }
          }
          else
          {
            throw new ArgumentException("innerBoundaryCurves", "One or more of the curves is null or not closed.");
          }
      }

      //Deal with both side curves if they exist
      var bothSideCrvs = new List<PolylineCurve>();
      if (innerBothSideCurves != null)
      {
        foreach (var crv in innerBothSideCurves)
          if (crv.IsClosed)
          {
            PolylineCurve polyline_crv = crv as PolylineCurve;
            if (polyline_crv != null)
            {
              bothSideCrvs.Add(polyline_crv);
            }
            else
            {
              bothSideCrvs.Add(crv.ToPolyline(0, 0, angleToleranceRadians, 0, 0, 0, 0, 0, true));
            }
          }
          else
          {
            throw new ArgumentException("innerBothSideCurves", "One or more of the curves is null or not closed.");
          }
      }

      IntPtr const_ptr_surface = pullbackSurface == null ? IntPtr.Zero : pullbackSurface.ConstPointer();

      using (var inner_curves = new SimpleArrayCurvePointer(innerCrvs))
      using (var both_side_curves = new SimpleArrayCurvePointer(bothSideCrvs))
      {
        IntPtr const_ptr_inner_curves = inner_curves.ConstPointer();
        IntPtr const_ptr_both_side_curves = both_side_curves.ConstPointer();
        int count_pt;
        var array_pt = RhinoListHelpers.GetConstArray(innerPoints, out count_pt);

        IntPtr ptr = UnsafeNativeMethods.RHC_RhinoMeshPatch(const_ptr_outer_boundary, const_ptr_surface,
          const_ptr_inner_curves, const_ptr_both_side_curves,
          array_pt, count_pt, trimback, divisions);

        GC.KeepAlive(outerBoundary);
        GC.KeepAlive(innerBoundaryCurves);
        GC.KeepAlive(innerBothSideCurves);
        if (outer_boundary_curve != null)
          outer_boundary_curve.Dispose();

        return IntPtr.Zero == ptr ? null : new Mesh(ptr, null);
      }
    }

    /// <summary>
    /// Computes the solid union of a set of meshes.
    /// </summary>
    /// <param name="meshes">Meshes to union.</param>
    /// <returns>An array of Mesh results or null on failure.</returns>
    public static Mesh[] CreateBooleanUnion(IEnumerable<Mesh> meshes)
    {
      if (null == meshes)
        return null;

      using (var input = new SimpleArrayMeshPointer())
      using (var output = new SimpleArrayMeshPointer())
      {
        foreach (var mesh in meshes)
        {
          if (null == mesh)
            continue;
          input.Add(mesh, true);
        }

        IntPtr const_ptr_input = input.ConstPointer();
        IntPtr ptr_output = output.NonConstPointer();

        // Fugier uses the following two tolerances in RhinoScript for all MeshBooleanUnion
        // calculations.
        const double mesh_bool_intersection_tolerance = RhinoMath.SqrtEpsilon * 10.0;
        const double mesh_bool_overlap_tolerance = RhinoMath.SqrtEpsilon * 10.0;

        Mesh[] rc = null;
        if (UnsafeNativeMethods.RHC_RhinoMeshBooleanUnion(const_ptr_input, mesh_bool_intersection_tolerance, mesh_bool_overlap_tolerance, ptr_output))
          rc = output.ToNonConstArray();
        GC.KeepAlive(meshes);

        return rc;
      }
    }

    static Mesh[] MeshBooleanHelper(IEnumerable<Mesh> firstSet, IEnumerable<Mesh> secondSet, UnsafeNativeMethods.MeshBooleanIntDiffConst which)
    {
      if (null == firstSet || null == secondSet)
        return null;

      using (var input1 = new SimpleArrayMeshPointer())
      using (var input2 = new SimpleArrayMeshPointer())
      using (var output = new SimpleArrayMeshPointer())
      {
        foreach (Mesh mesh in firstSet)
        {
          if (null == mesh)
            continue;
          input1.Add(mesh, true);
        }

        foreach (Mesh mesh in secondSet)
        {
          if (null == mesh)
            continue;
          input2.Add(mesh, true);
        }

        IntPtr const_ptr_input1 = input1.ConstPointer();
        IntPtr const_ptr_input2 = input2.ConstPointer();
        IntPtr ptr_output = output.NonConstPointer();

        // Fugier uses the following two tolerances in RhinoScript for all MeshBoolean...
        // calculations.
        const double mesh_bool_intersection_tolerance = RhinoMath.SqrtEpsilon * 10.0;
        const double mesh_bool_overlap_tolerance = RhinoMath.SqrtEpsilon * 10.0;

        Mesh[] rc = null;
        if (UnsafeNativeMethods.RHC_RhinoMeshBooleanIntDiff(const_ptr_input1, const_ptr_input2, mesh_bool_intersection_tolerance, mesh_bool_overlap_tolerance, ptr_output, which))
        {
          rc = output.ToNonConstArray();
        }
        GC.KeepAlive(firstSet);
        GC.KeepAlive(secondSet);

        return rc;
      }
    }

    /// <summary>
    /// Computes the solid difference of two sets of Meshes.
    /// </summary>
    /// <param name="firstSet">First set of Meshes (the set to subtract from).</param>
    /// <param name="secondSet">Second set of Meshes (the set to subtract).</param>
    /// <returns>An array of Mesh results or null on failure.</returns>
    public static Mesh[] CreateBooleanDifference(IEnumerable<Mesh> firstSet, IEnumerable<Mesh> secondSet)
    {
      return MeshBooleanHelper(firstSet, secondSet, UnsafeNativeMethods.MeshBooleanIntDiffConst.Difference);
    }
    /// <summary>
    /// Computes the solid intersection of two sets of meshes.
    /// </summary>
    /// <param name="firstSet">First set of Meshes.</param>
    /// <param name="secondSet">Second set of Meshes.</param>
    /// <returns>An array of Mesh results or null on failure.</returns>
    public static Mesh[] CreateBooleanIntersection(IEnumerable<Mesh> firstSet, IEnumerable<Mesh> secondSet)
    {
      return MeshBooleanHelper(firstSet, secondSet, UnsafeNativeMethods.MeshBooleanIntDiffConst.Intersect);
    }

    /// <summary>
    /// Splits a set of meshes with another set.
    /// </summary>
    /// <param name="meshesToSplit">A list, an array, or any enumerable set of meshes to be split. If this is null, null will be returned.</param>
    /// <param name="meshSplitters">A list, an array, or any enumerable set of meshes that cut. If this is null, null will be returned.</param>
    /// <returns>A new mesh array, or null on error.</returns>
    public static Mesh[] CreateBooleanSplit(IEnumerable<Mesh> meshesToSplit, IEnumerable<Mesh> meshSplitters)
    {
      return MeshBooleanHelper(meshesToSplit, meshSplitters, UnsafeNativeMethods.MeshBooleanIntDiffConst.Split);
    }

    /// <summary>
    /// Constructs a new mesh pipe from a curve.
    /// </summary>
    /// <param name="curve">A curve to pipe.</param>
    /// <param name="radius">The radius of the pipe.</param>
    /// <param name="segments">The number of segments in the pipe.</param>
    /// <param name="accuracy">The accuracy of the pipe.</param>
    /// <param name="capType">The type of cap to be created at the end of the pipe.</param>
    /// <param name="faceted">Specifies whether the pipe is faceted, or not.</param>
    /// <param name="intervals">A series of intervals to pipe. This value can be null.</param>
    /// <returns>A new mesh, or null on failure.</returns>
    public static Mesh CreateFromCurvePipe(Curve curve, double radius, int segments, int accuracy,
      MeshPipeCapStyle capType, bool faceted, IEnumerable<Interval> intervals = null)
    {
      if (curve == null)
        throw new ArgumentNullException("curve");
      if (!Enum.IsDefined(typeof(MeshPipeCapStyle), capType))
        throw new ArgumentOutOfRangeException("capType");
      if (radius <= 0.0)
        throw new ArgumentOutOfRangeException("radius");

      var const_curve_ptr = curve.ConstPointer();
      IntPtr intervals_ptr = IntPtr.Zero;
      SimpleArrayInterval dotnet_intervals = null;

      if (intervals != null)
      {
        dotnet_intervals = new SimpleArrayInterval();

        foreach (var interval in intervals)
        {
          dotnet_intervals.Add(interval);
        }

        if (dotnet_intervals.Count > 0) intervals_ptr = dotnet_intervals.ConstPointer();
      }

      IntPtr ptr_mesh = UnsafeNativeMethods.RHC_Mesh_CreateFromCurvePipe(
        const_curve_ptr, radius, segments, accuracy, capType, faceted, intervals_ptr);

      if (dotnet_intervals != null)
      {
        dotnet_intervals.Dispose();
      }
      GC.KeepAlive(curve);

      return IntPtr.Zero == ptr_mesh ? null : new Mesh(ptr_mesh, null);
    }

#endif
    #endregion

    #region constructors
    /// <summary>Initializes a new empty mesh.</summary>
    /// <example>
    /// <code source='examples\vbnet\ex_addmesh.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_addmesh.cs' lang='cs'/>
    /// <code source='examples\py\ex_addmesh.py' lang='py'/>
    /// </example>
    public Mesh()
    {
      IntPtr ptr = UnsafeNativeMethods.ON_Mesh_New(IntPtr.Zero);
      ConstructNonConstObject(ptr);
      ApplyMemoryPressure();
    }

    /// <summary>
    /// Protected constructor for internal use.
    /// </summary>
    /// <param name="info">Serialization data.</param>
    /// <param name="context">Serialization stream.</param>
    protected Mesh(SerializationInfo info, StreamingContext context)
      : base(info, context)
    {
    }

    internal override IntPtr _InternalGetConstPointer()
    {
      var mh = m__parent as MeshHolder;
      if (mh != null)
        return mh.MeshPointer();

      return base._InternalGetConstPointer();
    }

    /// <summary>
    /// Performs some memory cleanup if necessary
    /// </summary>
    protected override void OnSwitchToNonConst()
    {
      var mh = m__parent as MeshHolder;
      base.OnSwitchToNonConst();

      if (mh != null)
      {
        m__parent = null;
        mh.ReleaseMesh();
      }
    }

    internal int[] m_internal_ngon_edges;
    /// <summary> Clear local cache on non const calls </summary>
    protected override void NonConstOperation()
    {
      if( OneShotNonConstCallback!=null )
      {
        OneShotNonConstCallback(this, EventArgs.Empty);
        OneShotNonConstCallback = null;
      }
      m_internal_ngon_edges = null;
      base.NonConstOperation();
    }

    // Used for mesh display cache. Kept internal since it is very specific
    // and not designed for general use
    internal EventHandler OneShotNonConstCallback { get; set; }

    internal override object _GetConstObjectParent()
    {
      if (!IsDocumentControlled)
        return null;
      return base._GetConstObjectParent();
    }

    internal Mesh(IntPtr nativePointer, object parent)
      : base(nativePointer, parent, -1)
    {
      if (null == parent)
        ApplyMemoryPressure();
    }

    /// <summary>
    /// Copies mesh values into this mesh from another mesh.
    /// </summary>
    /// <param name="other">The other mesh to copy from.</param>
    /// <exception cref="ArgumentNullException">If other is null.</exception>
    public void CopyFrom(Mesh other)
    {
      if (other == null) throw new ArgumentNullException("other");

      IntPtr const_ptr_other = other.ConstPointer();
      IntPtr ptr_this = NonConstPointer();
      UnsafeNativeMethods.ON_Mesh_CopyFrom(const_ptr_other, ptr_this);
      GC.KeepAlive(other);
    }

    /// <summary>
    /// Constructs a copy of this mesh.
    /// This is the same as <see cref="DuplicateMesh"/>.
    /// </summary>
    /// <returns>A mesh.</returns>
    public override GeometryBase Duplicate()
    {
      IntPtr ptr = ConstPointer();
      IntPtr ptr_new_mesh = UnsafeNativeMethods.ON_Mesh_New(ptr);
      return new Mesh(ptr_new_mesh, null);
    }

    /// <summary>Constructs a copy of this mesh.
    /// This is the same as <see cref="Duplicate"/>.
    /// </summary>
    public Mesh DuplicateMesh()
    {
      return Duplicate() as Mesh;
    }

    internal override GeometryBase DuplicateShallowHelper()
    {
      return new Mesh(IntPtr.Zero, null);
    }
    #endregion

    #region properties
    /// <summary>
    /// Gets the number of disjoint (topologically unconnected) pieces in this mesh.
    /// </summary>
    public int DisjointMeshCount
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        return UnsafeNativeMethods.ON_Mesh_GetInt(const_ptr_this, UnsafeNativeMethods.MeshIntConst.DisjointMeshCount);
      }
    }

    /// <summary>
    /// Gets a value indicating whether a mesh is considered to be closed (solid).
    /// A mesh is considered solid when every mesh edge borders two or more faces.
    /// </summary>
    /// <returns>true if the mesh is closed, false if it is not.</returns>
    public bool IsClosed
    {
      get
      {
        IntPtr ptr = ConstPointer();
        return UnsafeNativeMethods.ON_Mesh_GetBool(ptr, UnsafeNativeMethods.MeshBoolConst.IsClosed);
      }
    }

    /// <summary>
    /// Gets a value indicating whether or not the mesh is manifold. 
    /// A manifold mesh does not have any edge that borders more than two faces.
    /// </summary>
    /// <param name="topologicalTest">
    /// If true, the query treats coincident vertices as the same.
    /// </param>
    /// <param name="isOriented">
    /// isOriented will be set to true if the mesh is a manifold 
    /// and adjacent faces have compatible face normals.
    /// </param>
    /// <param name="hasBoundary">
    /// hasBoundary will be set to true if the mesh is a manifold 
    /// and there is at least one "edge" with no more than one adjacent face.
    /// </param>
    /// <returns>true if every mesh "edge" has at most two adjacent faces.</returns>
    public bool IsManifold(bool topologicalTest, out bool isOriented, out bool hasBoundary)
    {
      isOriented = false;
      hasBoundary = false;
      IntPtr ptr = ConstPointer();
      return UnsafeNativeMethods.ON_Mesh_IsManifold(ptr, topologicalTest, ref isOriented, ref hasBoundary);
    }

    /// <summary>
    /// Will return true if SetCachedTextureCoordinates has been called;
    /// otherwise will return false.
    /// </summary>
    public bool HasCachedTextureCoordinates
    {
      get
      {
        var const_pointer = ConstPointer();
        var value = UnsafeNativeMethods.ON_Mesh_HasCachedTextureCoordinates(const_pointer);
        return value;
      }
    }

    #region fake list access
    private Collections.MeshVertexList m_vertices;
    /// <summary>
    /// Gets access to the vertices set of this mesh.
    /// </summary>
    /// <example>
    /// <code source='examples\vbnet\ex_addmesh.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_addmesh.cs' lang='cs'/>
    /// <code source='examples\py\ex_addmesh.py' lang='py'/>
    /// </example>
    public Collections.MeshVertexList Vertices
    {
      get { return m_vertices ?? (m_vertices = new Collections.MeshVertexList(this)); }
    }

    private Collections.MeshTopologyVertexList m_topology_vertices;

    /// <summary>
    /// Gets the <see cref="Rhino.Geometry.Collections.MeshTopologyVertexList"/> object associated with this mesh.
    /// <para>This object stores vertex connectivity and the indices of vertices
    /// that were unified while computing the edge topology.</para>
    /// </summary>
    public Collections.MeshTopologyVertexList TopologyVertices
    {
      get
      {
        return m_topology_vertices ?? (m_topology_vertices = new Collections.MeshTopologyVertexList(this));
      }
    }

    private Collections.MeshTopologyEdgeList m_topology_edges;

    /// <summary>
    /// Gets the <see cref="Rhino.Geometry.Collections.MeshTopologyEdgeList"/> object associated with this mesh.
    /// <para>This object stores edge connectivity.</para>
    /// </summary>
    public Collections.MeshTopologyEdgeList TopologyEdges
    {
      get
      {
        return m_topology_edges ?? (m_topology_edges = new Collections.MeshTopologyEdgeList(this));
      }
    }

    private Collections.MeshVertexNormalList m_normals;
    /// <summary>
    /// Gets access to the vertex normal collection in this mesh.
    /// </summary>
    /// <example>
    /// <code source='examples\vbnet\ex_addmesh.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_addmesh.cs' lang='cs'/>
    /// <code source='examples\py\ex_addmesh.py' lang='py'/>
    /// </example>
    public Collections.MeshVertexNormalList Normals
    {
      get { return m_normals ?? (m_normals = new Collections.MeshVertexNormalList(this)); }
    }

    private Collections.MeshFaceList m_faces;
    /// <summary>
    /// Gets access to the mesh face list.
    /// </summary>
    /// <example>
    /// <code source='examples\vbnet\ex_addmesh.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_addmesh.cs' lang='cs'/>
    /// <code source='examples\py\ex_addmesh.py' lang='py'/>
    /// </example>
    public Collections.MeshFaceList Faces
    {
      get { return m_faces ?? (m_faces = new Collections.MeshFaceList(this)); }
    }

    private Collections.MeshNgonList m_ngons;
    /// <summary>
    /// Gets access to the mesh ngon list.
    /// <para>Ngons represent groups of Faces (triangles + quads).</para>
    /// <para>Faces are used to tessellate an Ngon internally.</para>
    /// <para>When a triangle or quad is referenced in the Ngon list, it is no longer visualized and
    /// conceived as a single entity, but takes part of the Ngon.</para>
    /// <para>If you need to get access to both Ngons and the faces that are not referenced by Ngons,
    /// that is, all polygons that are visible in the mesh, then use the
    /// <see cref="GetNgonAndFacesEnumerable()"/> helper method.</para>
    /// </summary>
    public Collections.MeshNgonList Ngons
    {
      get { return m_ngons ?? (m_ngons = new Collections.MeshNgonList(this)); }
    }

    private Collections.MeshFaceNormalList m_facenormals;
    /// <summary>
    /// Gets access to the face normal collection in this mesh.
    /// </summary>
    public Collections.MeshFaceNormalList FaceNormals
    {
      get { return m_facenormals ?? (m_facenormals = new Collections.MeshFaceNormalList(this)); }
    }

    private Collections.MeshVertexColorList m_vertexcolors;
    /// <summary>
    /// Gets access to the (optional) vertex color collection in this mesh.
    /// </summary>
    public Collections.MeshVertexColorList VertexColors
    {
      get { return m_vertexcolors ?? (m_vertexcolors = new Collections.MeshVertexColorList(this)); }
    }

    private Collections.MeshTextureCoordinateList m_texcoords;
    /// <summary>
    /// Gets access to the vertex texture coordinate collection in this mesh.
    /// </summary>
    public Collections.MeshTextureCoordinateList TextureCoordinates
    {
      get { return m_texcoords ?? (m_texcoords = new Collections.MeshTextureCoordinateList(this)); }
    }

    private Collections.MeshVertexStatusList m_hidden;
    /// <summary>
    /// Gets access to the vertex hidden/visibility collection in this mesh.
    /// This is a runtime property and it is not saved in the 3dm file.
    /// </summary>
    public Collections.MeshVertexStatusList ComponentStates
    {
      get { return m_hidden ?? (m_hidden = new Collections.MeshVertexStatusList(this)); }
    }

    /// <summary>
    /// Removes all texture coordinate information from this mesh.
    /// </summary>
    public void ClearTextureData()
    {
      IntPtr ptr_this = NonConstPointer();
      UnsafeNativeMethods.ON_Mesh_DestroyTextureData(ptr_this);
    }

    /// <summary>
    /// Removes surface parameters, curvature parameters and surface statistics from the mesh.
    /// </summary>
    public void ClearSurfaceData()
    {
      IntPtr ptr_this = NonConstPointer();
      UnsafeNativeMethods.ON_Mesh_DestroySurfaceData(ptr_this);
    }

    /// <summary>
    /// Removes topology data, forcing all topology information to be recomputed.
    /// </summary>
    public void DestroyTopology()
    {
      IntPtr ptr_this = NonConstPointer();
      UnsafeNativeMethods.ON_Mesh_DestroyTopology(ptr_this);
    }

    /// <summary>
    /// Destroys the mesh vertex access tree.
    /// </summary>
    public void DestroyTree()
    {
      IntPtr ptr_this = NonConstPointer();
      UnsafeNativeMethods.ON_Mesh_DestroyTree(ptr_this);
    }

    /// <summary>
    /// Destroys mesh partition.
    /// </summary>
    public void DestroyPartition()
    {
      IntPtr ptr_this = NonConstPointer();
      UnsafeNativeMethods.ON_Mesh_DestroyPartition(ptr_this);
    }
    #endregion
    #endregion properties

    #region methods
    /// <summary>
    /// If the mesh has SurfaceParameters, the surface is evaluated at
    /// these parameters and the mesh geometry is updated.
    /// </summary>
    /// <param name="surface">An input surface.</param>
    /// <returns>true if the operation succceeded; false otherwise.</returns>
    public bool EvaluateMeshGeometry(Surface surface)
    {
      // don't switch to non-const if we don't have to
      IntPtr const_ptr_this = ConstPointer();
      if (!UnsafeNativeMethods.ON_Mesh_HasSurfaceParameters(const_ptr_this))
        return false;
      IntPtr ptr_this = NonConstPointer();
      IntPtr const_ptr_surface = surface.ConstPointer();
      bool rc = UnsafeNativeMethods.ON_Mesh_EvaluateMeshGeometry(ptr_this, const_ptr_surface);
      GC.KeepAlive(surface);
      return rc;
    }

    /// <summary>
    /// Set texture coordinates using given mapping and applying given transform.
    /// 
    /// Set lazy to false to generate texture coordinates right away.
    /// </summary>
    /// <param name="tm">Texture mapping</param>
    /// <param name="xf">Transform to apply to the texture mapping</param>
    /// <param name="lazy">Whether to generate lazily (true) or right away (false)</param>
    public void SetTextureCoordinates(TextureMapping tm, Transform xf, bool lazy)
    {
      UnsafeNativeMethods.ON_Mesh_SetTextureCoordinatesFromMappingAndTransform(NonConstPointer(), tm.ConstPointer(), ref xf, lazy);
    }

    /// <summary>
    /// Set cached texture coordinates using the specified mapping.
    /// </summary>
    /// <param name="tm"></param>
    /// <param name="xf"></param>
    public void SetCachedTextureCoordinates(TextureMapping tm, ref Transform xf)
    {
      UnsafeNativeMethods.ON_Mesh_SetCachedTextureCoordinates(NonConstPointer(), tm.ConstPointer(), ref xf, false);
      GC.KeepAlive(tm);
    }

    /// <summary>
    /// Call this method to get cached texture coordinates for a texture
    /// mapping with the specified Id.
    /// </summary>
    /// <param name="textureMappingId">
    /// Texture mapping Id
    /// </param>
    /// <returns>
    /// Object which allows access to coordinates and other props.
    /// </returns>
    public CachedTextureCoordinates GetCachedTextureCoordinates(Guid textureMappingId)
    {
      return CachedTextureCoordinates.GetCachedTextureCoordinates(this, textureMappingId);
    }

    /// <summary>
    /// Removes any unreferenced objects from arrays, reindexes as needed 
    /// and shrinks arrays to minimum required size.
    /// </summary>
    /// <returns>true on success, false on failure.</returns>
    /// <example>
    /// <code source='examples\vbnet\ex_addmesh.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_addmesh.cs' lang='cs'/>
    /// <code source='examples\py\ex_addmesh.py' lang='py'/>
    /// </example>
    public bool Compact()
    {
      IntPtr ptr_this = NonConstPointer();
      return UnsafeNativeMethods.ON_Mesh_NonConstBoolOp(ptr_this, UnsafeNativeMethods.MeshNonConstBoolConst.Compact);
    }

#if RHINO_SDK
    /// <summary>
    /// Compute volume of the mesh. 
    /// </summary>
    /// <returns>Volume of the mesh.</returns>
    [ConstOperation]
    public double Volume()
    {
      IntPtr ptr_this = ConstPointer();
      return UnsafeNativeMethods.ON_Mesh_Volume(ptr_this);
    }
#endif

    /// <summary>Reverses the direction of the mesh.</summary>
    /// <param name="vertexNormals">If true, vertex normals will be reversed.</param>
    /// <param name="faceNormals">If true, face normals will be reversed.</param>
    /// <param name="faceOrientation">If true, face orientations will be reversed.</param>
    public void Flip(bool vertexNormals, bool faceNormals, bool faceOrientation)
    {
      IntPtr ptr_this = NonConstPointer();
      UnsafeNativeMethods.ON_Mesh_Flip(ptr_this, vertexNormals, faceNormals, faceOrientation);
    }

    /// <summary>
    /// Determines orientation of a "solid" mesh.
    /// </summary>
    /// <returns>
    /// <para>+1 = mesh is solid with outward facing normals.</para>
    /// <para>-1 = mesh is solid with inward facing normals.</para>
    /// <para>0 = mesh is not solid.</para>
    /// </returns>
    [ConstOperation]
    public int SolidOrientation()
    {
      IntPtr const_ptr_this = ConstPointer();
      return UnsafeNativeMethods.ON_Mesh_GetInt(const_ptr_this, UnsafeNativeMethods.MeshIntConst.SolidOrientation);
    }

    /// <summary>
    /// Determines if a point is inside a solid mesh.
    /// </summary>
    /// <param name="point">3d point to test.</param>
    /// <param name="tolerance">
    /// (&gt;=0) 3d distance tolerance used for ray-mesh intersection
    /// and determining strict inclusion.
    /// </param>
    /// <param name="strictlyIn">
    /// If strictlyIn is true, then point must be inside mesh by at least
    /// tolerance in order for this function to return true.
    /// If strictlyIn is false, then this function will return true if
    /// point is inside or the distance from point to a mesh face is &lt;= tolerance.
    /// </param>
    /// <returns>
    /// true if point is inside the solid mesh, false if not.
    /// </returns>
    /// <remarks>
    /// The caller is responsible for making certing the mesh is solid before
    /// calling this function. If the mesh is not solid, the behavior is unpredictable.
    /// </remarks>
    [ConstOperation]
    public bool IsPointInside(Point3d point, double tolerance, bool strictlyIn)
    {
      IntPtr const_ptr_this = ConstPointer();
      return UnsafeNativeMethods.ON_Mesh_IsPointInside(const_ptr_this, point, tolerance, strictlyIn);
    }

    // need to implement
    //int GetMeshEdges( ON_SimpleArray<ON_2dex>& edges ) const;
    //int* GetVertexLocationIds(int first_vid, int* Vid, int* Vindex) const;
    //int GetMeshFaceSideList( const int* Vid, struct ON_MeshFaceSide*& sides) const;
    //int GetMeshEdgeList(ON_SimpleArray<ON_2dex>& edge_list, int edge_type_partition[5] ) const;
    //int GetMeshEdgeList(ON_SimpleArray<ON_2dex>& edge_list, ON_SimpleArray<int>& ci_meshtop_edge_map, int edge_type_partition[5]) const;
    //int GetMeshEdgeList(ON_SimpleArray<ON_2dex>& edge_list, ON_SimpleArray<int>& ci_meshtop_edge_map, ON_SimpleArray<int>& ci_meshtop_vertex_map, int edge_type_partition[5]) const;

#if RHINO_SDK
    /// <summary>
    /// Smooths a mesh by averaging the positions of mesh vertices in a specified region.
    /// </summary>
    /// <param name="smoothFactor">The smoothing factor, which controls how much vertices move towards the average of the neighboring vertices.</param>
    /// <param name="bXSmooth">When true vertices move in X axis direction.</param>
    /// <param name="bYSmooth">When true vertices move in Y axis direction.</param>
    /// <param name="bZSmooth">When true vertices move in Z axis direction.</param>
    /// <param name="bFixBoundaries">When true vertices along naked edges will not be modified.</param>
    /// <param name="coordinateSystem">The coordinates to determine the direction of the smoothing.</param>
    /// <returns>True if successful, false otherwise.</returns>
    public bool Smooth(double smoothFactor, bool bXSmooth, bool bYSmooth, bool bZSmooth, bool bFixBoundaries, SmoothingCoordinateSystem coordinateSystem)
    {
      return Smooth(smoothFactor, bXSmooth, bYSmooth, bZSmooth, bFixBoundaries, coordinateSystem, Plane.WorldXY);
    }

    /// <summary>
    /// Smooths a mesh by averaging the positions of mesh vertices in a specified region.
    /// </summary>
    /// <param name="smoothFactor">The smoothing factor, which controls how much vertices move towards the average of the neighboring vertices.</param>
    /// <param name="bXSmooth">When true vertices move in X axis direction.</param>
    /// <param name="bYSmooth">When true vertices move in Y axis direction.</param>
    /// <param name="bZSmooth">When true vertices move in Z axis direction.</param>
    /// <param name="bFixBoundaries">When true vertices along naked edges will not be modified.</param>
    /// <param name="coordinateSystem">The coordinates to determine the direction of the smoothing.</param>
    /// <param name="plane">If SmoothingCoordinateSystem.CPlane specified, then the construction plane.</param>
    /// <returns>True if successful, false otherwise.</returns>
    public bool Smooth(double smoothFactor, bool bXSmooth, bool bYSmooth, bool bZSmooth, bool bFixBoundaries, SmoothingCoordinateSystem coordinateSystem, Plane plane)
    {
      IntPtr ptr_this = NonConstPointer();
      return UnsafeNativeMethods.RHC_RhinoSmoothMesh(ptr_this, smoothFactor, bXSmooth, bYSmooth, bZSmooth, bFixBoundaries, (int)coordinateSystem, ref plane);
    }

    /// <summary>
    /// Smooths part of a mesh by averaging the positions of mesh vertices in a specified region.
    /// </summary>
    /// <param name="vertexIndices">The mesh vertex indices that specify the part of the mesh to smooth.</param>
    /// <param name="smoothFactor">The smoothing factor, which controls how much vertices move towards the average of the neighboring vertices.</param>
    /// <param name="bXSmooth">When true vertices move in X axis direction.</param>
    /// <param name="bYSmooth">When true vertices move in Y axis direction.</param>
    /// <param name="bZSmooth">When true vertices move in Z axis direction.</param>
    /// <param name="bFixBoundaries">When true vertices along naked edges will not be modified.</param>
    /// <param name="coordinateSystem">The coordinates to determine the direction of the smoothing.</param>
    /// <param name="plane">If SmoothingCoordinateSystem.CPlane specified, then the construction plane.</param>
    /// <returns>True if successful, false otherwise.</returns>
    public bool Smooth(IEnumerable<int> vertexIndices, double smoothFactor, bool bXSmooth, bool bYSmooth, bool bZSmooth, bool bFixBoundaries, SmoothingCoordinateSystem coordinateSystem, Plane plane)
    {
      IntPtr ptr_this = NonConstPointer();
      using (var indices = new SimpleArrayInt(vertexIndices))
      {
        IntPtr ptr_indices = indices.NonConstPointer();
        return UnsafeNativeMethods.RHC_RhinoSmoothMesh2(ptr_this, ptr_indices, smoothFactor, bXSmooth, bYSmooth, bZSmooth, bFixBoundaries, (int) coordinateSystem, ref plane);
      }
    }

    /// <summary>
    /// Makes sure that faces sharing an edge and having a difference of normal greater
    /// than or equal to angleToleranceRadians have unique vertexes along that edge,
    /// adding vertices if necessary.
    /// </summary>
    /// <param name="angleToleranceRadians">Angle at which to make unique vertices.</param>
    /// <param name="modifyNormals">
    /// Determines whether new vertex normals will have the same vertex normal as the original (false)
    /// or vertex normals made from the corrsponding face normals (true)
    /// </param>
    public void Unweld(double angleToleranceRadians, bool modifyNormals)
    {
      IntPtr ptr_this = NonConstPointer();
      UnsafeNativeMethods.RHC_RhinoUnWeldMesh(ptr_this, angleToleranceRadians, modifyNormals);
    }

    /// <summary>
    /// Adds creases to a smooth mesh by creating coincident vertices along selected edges.
    /// </summary>
    /// <param name="edgeIndices">An array of mesh topology edge indices.</param>
    /// <param name="modifyNormals">
    /// If true, the vertex normals on each side of the edge take the same value as the face to which they belong, giving the mesh a hard edge look.
    /// If false, each of the vertex normals on either side of the edge is assigned the same value as the original normal that the pair is replacing, keeping a smooth look.
    /// </param>
    /// <returns>true if successful, false otherwise.</returns>
    public bool UnweldEdge(IEnumerable<int> edgeIndices, bool modifyNormals)
    {
      var edges = new RhinoList<int>(edgeIndices);
      var count = edges.Count;
      if (count <= 0)
        return false;

      IntPtr ptr_this = NonConstPointer();
      return UnsafeNativeMethods.RHC_RhinoUnweldMeshEdge(ptr_this, count, edges.m_items, modifyNormals);
    }

    /// <summary>
    /// Makes sure that faces sharing an edge and having a difference of normal greater
    /// than or equal to angleToleranceRadians share vertexes along that edge, vertex normals
    /// are averaged.
    /// </summary>
    /// <param name="angleToleranceRadians">Angle at which to weld vertices.</param>
    public void Weld(double angleToleranceRadians)
    {
      IntPtr ptr_this = NonConstPointer();
      UnsafeNativeMethods.RHC_RhinoWeldMesh(ptr_this, angleToleranceRadians);
    }

    /// <summary>
    /// Removes mesh normals and reconstructs the face and vertex normals based
    /// on the orientation of the faces.
    /// </summary>
    public void RebuildNormals()
    {
      IntPtr ptr_this = NonConstPointer();
      UnsafeNativeMethods.RHC_RhinoRebuildMeshNormals(ptr_this);
    }

    /// <summary>
    /// Extracts, or removes, non-manifold mesh edges. 
    /// </summary>
    /// <param name="selective">If true, then extract hanging faces only.</param>
    /// <returns>A mesh containing the extracted non-manifold parts if successful, null otherwise.</returns>
    public Mesh ExtractNonManifoldEdges(bool selective)
    {
      IntPtr ptr_this = NonConstPointer();
      IntPtr ptr = UnsafeNativeMethods.RHC_RhinoExtractNonManifoldMeshEdges(ptr_this, selective);
      return ptr != IntPtr.Zero ? new Mesh(ptr, null) : null;
    }

    /// <summary>
    /// Attempts to "heal" naked edges in a mesh based on a given distance.  
    /// First attempts to move vertexes to neighboring vertexes that are within that
    /// distance away. Then it finds edges that have a closest point to the vertex within
    /// the distance and splits the edge. When it finds one it splits the edge and
    /// makes two new edges using that point.
    /// </summary>
    /// <param name="distance">Distance to not exceed when modifying the mesh.</param>
    /// <returns>true if successful, false otherwise.</returns>
    public bool HealNakedEdges(double distance)
    {
      IntPtr ptr_this = NonConstPointer();
      return UnsafeNativeMethods.RHC_RhinoHealMesh(ptr_this, distance);
    }

    /// <summary>
    /// Attempts to determine "holes" in the mesh by chaining naked edges together. 
    /// Then it triangulates the closed polygons adds the faces to the mesh.
    /// </summary>
    /// <returns>true if successful, false otherwise.</returns>
    /// <remarks>This function does not differentiate between inner and outer naked edges.  
    /// If you need that, it would be better to use Mesh.FillHole.
    /// </remarks>
    public bool FillHoles()
    {
      IntPtr ptr_this = NonConstPointer();
      return UnsafeNativeMethods.RHC_RhinoFillMeshHoles(ptr_this);
    }

    /// <summary>
    /// Given a starting "naked" edge index, this function attempts to determine a "hole"
    /// by chaining additional naked edges together until if returns to the start index.
    /// Then it triangulates the closed polygon and either adds the faces to the mesh.
    /// </summary>
    /// <param name="topologyEdgeIndex">Starting naked edge index.</param>
    /// <returns>true if successful, false otherwise.</returns>
    public bool FileHole(int topologyEdgeIndex)
    {
      IntPtr ptr_this = NonConstPointer();
      return UnsafeNativeMethods.RHC_RhinoFillMeshHole(ptr_this, topologyEdgeIndex);
    }

    /// <summary>
    /// Attempts to fix inconsistencies in the directions of mesh faces in a mesh. This function
    /// does not modify mesh vertex normals, it rearranges the mesh face winding and face
    /// normals to make them all consistent. Note, you may want to call Mesh.Normals.ComputeNormals()
    /// to recompute vertex normals after calling this functions.
    /// </summary>
    /// <returns>number of faces that were modified.</returns>
    public int UnifyNormals()
    {
      return UnifyNormals(IsDocumentControlled);
    }

    /// <summary>
    /// Attempts to fix inconsistencies in the directions of mesh faces in a mesh. This function
    /// does not modify mesh vertex normals, it rearranges the mesh face winding and face
    /// normals to make them all consistent. Note, you may want to call Mesh.Normals.ComputeNormals()
    /// to recompute vertex normals after calling this functions.
    /// </summary>
    /// <param name="countOnly">If true, then only the number of faces that would be modified is determined.</param>
    /// <returns>If countOnly=false, the number of faces that were modified. If countOnly=true, the number of faces that would be modified.</returns>
    public int UnifyNormals(bool countOnly)
    {
      IntPtr ptr_this = NonConstPointer();
      int rc = UnsafeNativeMethods.RHC_RhinoUnifyMeshNormals(ptr_this, countOnly);
      return rc;
    }

    /// <summary>
    /// Splits up the mesh into its unconnected pieces.
    /// </summary>
    /// <returns>An array containing all the disjoint pieces that make up this Mesh.</returns>
    [ConstOperation]
    public Mesh[] SplitDisjointPieces()
    {
      IntPtr const_ptr_this = ConstPointer();
      using (var meshes = new SimpleArrayMeshPointer())
      {
        IntPtr ptr_mesh_array = meshes.NonConstPointer();
        UnsafeNativeMethods.RHC_RhinoSplitDisjointMesh(const_ptr_this, ptr_mesh_array);
        return meshes.ToNonConstArray();
      }
    }

    /// <summary>
    /// Split a mesh by an infinite plane.
    /// </summary>
    /// <param name="plane">The splitting plane.</param>
    /// <returns>A new mesh array with the split result. This can be null if no result was found.</returns>
    [ConstOperation]
    public Mesh[] Split(Plane plane)
    {
      IntPtr const_ptr_this = ConstPointer();
      using (var meshes = new SimpleArrayMeshPointer())
      {
        IntPtr ptr_mesh_array = meshes.NonConstPointer();
        UnsafeNativeMethods.RHC_RhinoMeshBooleanSplit(const_ptr_this, ptr_mesh_array, ref plane);
        return meshes.ToNonConstArray();
      }
    }
    /// <summary>
    /// Split a mesh with another mesh.
    /// </summary>
    /// <param name="mesh">Mesh to split with.</param>
    /// <returns>An array of mesh segments representing the split result.</returns>
    [ConstOperation]
    public Mesh[] Split(Mesh mesh)
    {
      using (var input_meshes = new SimpleArrayMeshPointer())
      using (var splitters = new SimpleArrayMeshPointer())
      using (var meshes = new SimpleArrayMeshPointer())
      {
        input_meshes.Add(this, true);
        splitters.Add(mesh, true);
        IntPtr const_ptr_input_meshes = input_meshes.ConstPointer();
        IntPtr const_ptr_splitters = splitters.ConstPointer();
        IntPtr ptr_result_meshes = meshes.NonConstPointer();
        UnsafeNativeMethods.RHC_RhinoMeshBooleanSplit2(const_ptr_input_meshes, const_ptr_splitters, ptr_result_meshes);
        GC.KeepAlive(mesh);
        return meshes.ToNonConstArray();
      }
    }
    /// <summary>
    /// Split a mesh with a collection of meshes.
    /// </summary>
    /// <param name="meshes">Meshes to split with.</param>
    /// <returns>An array of mesh segments representing the split result.</returns>
    [ConstOperation]
    public Mesh[] Split(IEnumerable<Mesh> meshes)
    {
      using (var input_meshes = new SimpleArrayMeshPointer())
      using (var splitters = new SimpleArrayMeshPointer())
      using (var on_meshes = new SimpleArrayMeshPointer())
      {
        input_meshes.Add(this, true);
        foreach (var mesh in meshes)
        {
          if (mesh != null)
            splitters.Add(mesh, true);
        }
        IntPtr const_ptr_input_meshes = input_meshes.ConstPointer();
        IntPtr const_ptr_splitters = splitters.ConstPointer();
        IntPtr ptr_result_meshes = on_meshes.NonConstPointer();
        UnsafeNativeMethods.RHC_RhinoMeshBooleanSplit2(const_ptr_input_meshes, const_ptr_splitters, ptr_result_meshes);
        GC.KeepAlive(meshes);
        return on_meshes.ToNonConstArray();
      }
    }

    /// <summary>
    /// Constructs the outlines of a mesh projected against a plane.
    /// </summary>
    /// <param name="plane">A plane to project against.</param>
    /// <returns>An array of polylines, or null on error.</returns>
    [ConstOperation]
    public Polyline[] GetOutlines(Plane plane)
    {
      IntPtr const_ptr_this = ConstPointer();
      int polylines_created = 0;
      IntPtr ptr_polylines = UnsafeNativeMethods.TL_GetMeshOutline(const_ptr_this, ref plane, ref polylines_created);
      if (polylines_created < 1 || IntPtr.Zero == ptr_polylines)
        return null;

      // convert the C++ polylines created into .NET polylines
      var rc = new Polyline[polylines_created];
      for (int i = 0; i < polylines_created; i++)
      {
        int point_count = UnsafeNativeMethods.ON_SimpleArray_PolylineCurve_GetCount(ptr_polylines, i);
        var pl = new Polyline(point_count);
        if (point_count > 0)
        {
          pl.m_size = point_count;
          UnsafeNativeMethods.ON_SimpleArray_PolylineCurve_GetPoints(ptr_polylines, i, point_count, pl.m_items);
        }
        rc[i] = pl;
      }
      UnsafeNativeMethods.ON_SimpleArray_PolylineCurve_Delete(ptr_polylines, true);
      return rc;
    }

    /// <summary>
    /// Constructs the outlines of a mesh. The projection information in the
    /// viewport is used to determine how the outlines are projected.
    /// </summary>
    /// <param name="viewport">A viewport to determine projection direction.</param>
    /// <returns>An array of polylines, or null on error.</returns>
    [ConstOperation]
    public Polyline[] GetOutlines(Display.RhinoViewport viewport)
    {
      IntPtr const_ptr_this = ConstPointer();
      int polylines_created = 0;
      IntPtr const_ptr_rhino_viewport = viewport.ConstPointer();
      IntPtr ptr_polylines = UnsafeNativeMethods.TL_GetMeshOutline2(const_ptr_this, const_ptr_rhino_viewport, ref polylines_created);
      if (polylines_created < 1 || IntPtr.Zero == ptr_polylines)
        return null;

      // convert the C++ polylines created into .NET polylines
      var rc = new Polyline[polylines_created];
      for (int i = 0; i < polylines_created; i++)
      {
        int point_count = UnsafeNativeMethods.ON_SimpleArray_PolylineCurve_GetCount(ptr_polylines, i);
        var pl = new Polyline(point_count);
        if (point_count > 0)
        {
          pl.m_size = point_count;
          UnsafeNativeMethods.ON_SimpleArray_PolylineCurve_GetPoints(ptr_polylines, i, point_count, pl.m_items);
        }
        rc[i] = pl;
      }
      UnsafeNativeMethods.ON_SimpleArray_PolylineCurve_Delete(ptr_polylines, true);
      GC.KeepAlive(viewport);
      return rc;
    }

    /// <summary>
    /// Constructs the outlines of a mesh.
    /// </summary>
    /// <param name="viewportInfo">The viewport info that provides the outline direction.</param>
    /// <param name="plane">Usually the view's construction plane. If a parallel projection and view plane is parallel to this, then project the results to the plane.</param>
    /// <returns>An array of polylines, or null on error.</returns>
    [ConstOperation]
    public Polyline[] GetOutlines(ViewportInfo viewportInfo, Plane plane)
    {
      IntPtr const_ptr_this = ConstPointer();
      int polylines_created = 0;
      IntPtr const_ptr_viewport = viewportInfo.ConstPointer();
      IntPtr ptr_polylines = UnsafeNativeMethods.TL_GetMeshOutline3(const_ptr_this, const_ptr_viewport, ref plane, ref polylines_created);
      if (polylines_created < 1 || IntPtr.Zero == ptr_polylines)
        return null;

      // convert the C++ polylines created into .NET polylines
      var rc = new Polyline[polylines_created];
      for (int i = 0; i < polylines_created; i++)
      {
        int point_count = UnsafeNativeMethods.ON_SimpleArray_PolylineCurve_GetCount(ptr_polylines, i);
        var pl = new Polyline(point_count);
        if (point_count > 0)
        {
          pl.m_size = point_count;
          UnsafeNativeMethods.ON_SimpleArray_PolylineCurve_GetPoints(ptr_polylines, i, point_count, pl.m_items);
        }
        rc[i] = pl;
      }
      UnsafeNativeMethods.ON_SimpleArray_PolylineCurve_Delete(ptr_polylines, true);
      GC.KeepAlive(viewportInfo);
      return rc;
    }

    /// <summary>
    /// Returns all edges of a mesh that are considered "naked" in the
    /// sense that the edge only has one face.
    /// </summary>
    /// <returns>An array of polylines, or null on error.</returns>
    /// <example>
    /// <code source='examples\vbnet\ex_dupmeshboundary.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_dupmeshboundary.cs' lang='cs'/>
    /// <code source='examples\py\ex_dupmeshboundary.py' lang='py'/>
    /// </example>
    [ConstOperation]
    public Polyline[] GetNakedEdges()
    {
      IntPtr const_ptr_this = ConstPointer();
      int polylines_created = 0;
      IntPtr ptr_polylines = UnsafeNativeMethods.ON_Mesh_GetNakedEdges(const_ptr_this, ref polylines_created);
      if (polylines_created < 1 || IntPtr.Zero == ptr_polylines)
        return null;

      // convert the C++ polylines created into .NET polylines
      var rc = new Polyline[polylines_created];
      for (int i = 0; i < polylines_created; i++)
      {
        int point_count = UnsafeNativeMethods.ON_SimpleArray_PolylineCurve_GetCount(ptr_polylines, i);
        var pl = new Polyline(point_count);
        if (point_count > 0)
        {
          pl.m_size = point_count;
          UnsafeNativeMethods.ON_SimpleArray_PolylineCurve_GetPoints(ptr_polylines, i, point_count, pl.m_items);
        }
        rc[i] = pl;
      }
      UnsafeNativeMethods.ON_SimpleArray_PolylineCurve_Delete(ptr_polylines, true);
      return rc;
    }

    /// <summary>
    /// Explode the mesh into submeshes where a submesh is a collection of faces that are contained
    /// within a closed loop of "unwelded" edges. Unwelded edges are edges where the faces that share
    /// the edge have unique mesh vertexes (not mesh topology vertexes) at both ends of the edge.
    /// </summary>
    /// <returns>
    /// Array of submeshes on success; null on error. If the count in the returned array is 1, then
    /// nothing happened and the ouput is essentially a copy of the input.
    /// </returns>
    [ConstOperation]
    public Mesh[] ExplodeAtUnweldedEdges()
    {
      IntPtr const_ptr_this = ConstPointer();
      using (var meshes = new SimpleArrayMeshPointer())
      {
        IntPtr ptr_mesh_array = meshes.NonConstPointer();
        UnsafeNativeMethods.RHC_RhinoExplodeMesh(const_ptr_this, ptr_mesh_array);
        return meshes.ToNonConstArray();
      }
    }

#endif

    /// <summary>
    /// Appends a copy of another mesh to this one and updates indices of appended mesh parts.
    /// </summary>
    /// <param name="other">Mesh to append to this one.</param>
    /// <example>
    /// <code source='examples\vbnet\ex_createmeshfrombrep.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_createmeshfrombrep.cs' lang='cs'/>
    /// <code source='examples\py\ex_createmeshfrombrep.py' lang='py'/>
    /// </example>
    public void Append(Mesh other)
    {
      if (null == other || other.ConstPointer() == ConstPointer())
        return;
      IntPtr ptr_this = NonConstPointer();
      IntPtr const_ptr_other = other.ConstPointer();
      UnsafeNativeMethods.ON_Mesh_Append(ptr_this, const_ptr_other);
      GC.KeepAlive(other);
    }

    /// <summary>
    /// Append a list of meshes. This function is much more efficient than making
    /// repeated calls to Mesh.Append(Mesh) when lots of meshes are being joined
    /// into a single large mesh.
    /// </summary>
    /// <param name="meshes">Meshes to append to this one.</param>
    public void Append(IEnumerable<Mesh> meshes)
    {
      List<GeometryBase> g = new List<GeometryBase>();
      foreach (Mesh msh in meshes)
      {
        if (msh == null)
          throw new ArgumentNullException("meshes");
        g.Add(msh);
      }
      using (Runtime.InteropWrappers.SimpleArrayGeometryPointer mesh_array = new Runtime.InteropWrappers.SimpleArrayGeometryPointer(g))
      {
        IntPtr ptr_this = NonConstPointer();
        IntPtr ptr_meshes = mesh_array.ConstPointer();
        UnsafeNativeMethods.ON_Mesh_Append2(ptr_this, ptr_meshes);
        GC.KeepAlive(meshes);
      }
    }

#if RHINO_SDK
    /// <summary>
    /// Gets the point on the mesh that is closest to a given test point.
    /// </summary>
    /// <param name="testPoint">Point to seach for.</param>
    /// <returns>The point on the mesh closest to testPoint, or Point3d.Unset on failure.</returns>
    [ConstOperation]
    public Point3d ClosestPoint(Point3d testPoint)
    {
      Point3d point_on_mesh;
      if (ClosestPoint(testPoint, out point_on_mesh, 0.0) < 0)
      {
        return Point3d.Unset;
      }
      return point_on_mesh;
    }

    /// <summary>
    /// Gets the point on the mesh that is closest to a given test point. Similar to the 
    /// ClosestPoint function except this returns a MeshPoint class which includes
    /// extra information beyond just the location of the closest point.
    /// </summary>
    /// <param name="testPoint">The source of the search.</param>
    /// <param name="maximumDistance">
    /// Optional upper bound on the distance from test point to the mesh. 
    /// If you are only interested in finding a point Q on the mesh when 
    /// testPoint.DistanceTo(Q) &lt; maximumDistance, 
    /// then set maximumDistance to that value. 
    /// This parameter is ignored if you pass 0.0 for a maximumDistance.
    /// </param>
    /// <returns>closest point information on success. null on failure.</returns>
    [ConstOperation]
    public MeshPoint ClosestMeshPoint(Point3d testPoint, double maximumDistance)
    {
      IntPtr const_ptr_this = ConstPointer();
      var ds = new MeshPointDataStruct();
      if (UnsafeNativeMethods.ON_Mesh_GetClosestPoint3(const_ptr_this, testPoint, ref ds, maximumDistance))
        return new MeshPoint(this, ds);
      return null;
    }

    /// <summary>
    /// Gets the point on the mesh that is closest to a given test point.
    /// </summary>
    /// <param name="testPoint">Point to seach for.</param>
    /// <param name="pointOnMesh">Point on the mesh closest to testPoint.</param>
    /// <param name="maximumDistance">
    /// Optional upper bound on the distance from test point to the mesh. 
    /// If you are only interested in finding a point Q on the mesh when 
    /// testPoint.DistanceTo(Q) &lt; maximumDistance, 
    /// then set maximumDistance to that value. 
    /// This parameter is ignored if you pass 0.0 for a maximumDistance.
    /// </param>
    /// <returns>
    /// Index of face that the closest point lies on if successful. 
    /// -1 if not successful; the value of pointOnMesh is undefined.
    /// </returns>
    [ConstOperation]
    public int ClosestPoint(Point3d testPoint, out Point3d pointOnMesh, double maximumDistance)
    {
      pointOnMesh = Point3d.Unset;
      IntPtr const_ptr_this = ConstPointer();
      return UnsafeNativeMethods.ON_Mesh_GetClosestPoint(const_ptr_this, testPoint, ref pointOnMesh, maximumDistance);
    }

    /// <summary>
    /// Gets the point on the mesh that is closest to a given test point.
    /// </summary>
    /// <param name="testPoint">Point to seach for.</param>
    /// <param name="pointOnMesh">Point on the mesh closest to testPoint.</param>
    /// <param name="normalAtPoint">The normal vector of the mesh at the closest point.</param>
    /// <param name="maximumDistance">
    /// Optional upper bound on the distance from test point to the mesh. 
    /// If you are only interested in finding a point Q on the mesh when 
    /// testPoint.DistanceTo(Q) &lt; maximumDistance, 
    /// then set maximumDistance to that value. 
    /// This parameter is ignored if you pass 0.0 for a maximumDistance.
    /// </param>
    /// <returns>
    /// Index of face that the closest point lies on if successful. 
    /// -1 if not successful; the value of pointOnMesh is undefined.
    /// </returns>
    [ConstOperation]
    public int ClosestPoint(Point3d testPoint, out Point3d pointOnMesh, out Vector3d normalAtPoint, double maximumDistance)
    {
      pointOnMesh = Point3d.Unset;
      normalAtPoint = Vector3d.Unset;
      IntPtr const_ptr_this = ConstPointer();
      return UnsafeNativeMethods.ON_Mesh_GetClosestPoint2(const_ptr_this, testPoint, ref pointOnMesh, ref normalAtPoint, maximumDistance);
    }

    /// <summary>
    /// Evaluate a mesh at a set of barycentric coordinates.
    /// </summary>
    /// <param name="meshPoint">MeshPoint instance contiaining a valid Face Index and Barycentric coordinates.</param>
    /// <returns>A Point on the mesh or Point3d.Unset if the faceIndex is not valid or if the barycentric coordinates could not be evaluated.</returns>
    [ConstOperation]
    public Point3d PointAt(MeshPoint meshPoint)
    {
      if (meshPoint == null)
        throw new ArgumentNullException("meshPoint");
      return PointAt(meshPoint.FaceIndex, meshPoint.T[0], meshPoint.T[1], meshPoint.T[2], meshPoint.T[3]);
    }
    /// <summary>
    /// Evaluates a mesh at a set of barycentric coordinates. Barycentric coordinates must 
    /// be assigned in accordance with the rules as defined by MeshPoint.T.
    /// </summary>
    /// <param name="faceIndex">Index of triangle or quad to evaluate.</param>
    /// <param name="t0">First barycentric coordinate.</param>
    /// <param name="t1">Second barycentric coordinate.</param>
    /// <param name="t2">Third barycentric coordinate.</param>
    /// <param name="t3">Fourth barycentric coordinate. If the face is a triangle, this coordinate will be ignored.</param>
    /// <returns>A Point on the mesh or Point3d.Unset if the faceIndex is not valid or if the barycentric coordinates could not be evaluated.</returns>
    [ConstOperation]
    public Point3d PointAt(int faceIndex, double t0, double t1, double t2, double t3)
    {
      IntPtr const_ptr_this = ConstPointer();
      Point3d pt = Point3d.Unset;

      if (UnsafeNativeMethods.ON_Mesh_MeshPointAt(const_ptr_this, faceIndex, t0, t1, t2, t3, ref pt))
        return pt;
      return Point3d.Unset;
    }

    /// <summary>
    /// Evaluate a mesh normal at a set of barycentric coordinates.
    /// </summary>
    /// <param name="meshPoint">MeshPoint instance contiaining a valid Face Index and Barycentric coordinates.</param>
    /// <returns>A Normal vector to the mesh or Vector3d.Unset if the faceIndex is not valid or if the barycentric coordinates could not be evaluated.</returns>
    [ConstOperation]
    public Vector3d NormalAt(MeshPoint meshPoint)
    {
      if (meshPoint == null)
        throw new ArgumentNullException("meshPoint");
      return NormalAt(meshPoint.FaceIndex, meshPoint.T[0], meshPoint.T[1], meshPoint.T[2], meshPoint.T[3]);
    }
    /// <summary>
    /// Evaluate a mesh normal at a set of barycentric coordinates. Barycentric coordinates must 
    /// be assigned in accordance with the rules as defined by MeshPoint.T.
    /// </summary>
    /// <param name="faceIndex">Index of triangle or quad to evaluate.</param>
    /// <param name="t0">First barycentric coordinate.</param>
    /// <param name="t1">Second barycentric coordinate.</param>
    /// <param name="t2">Third barycentric coordinate.</param>
    /// <param name="t3">Fourth barycentric coordinate. If the face is a triangle, this coordinate will be ignored.</param>
    /// <returns>A Normal vector to the mesh or Vector3d.Unset if the faceIndex is not valid or if the barycentric coordinates could not be evaluated.</returns>
    [ConstOperation]
    public Vector3d NormalAt(int faceIndex, double t0, double t1, double t2, double t3)
    {
      IntPtr const_ptr_this = ConstPointer();
      Vector3d nr = Vector3d.Unset;

      if (UnsafeNativeMethods.ON_Mesh_MeshNormalAt(const_ptr_this, faceIndex, t0, t1, t2, t3, ref nr))
        return nr;
      return Vector3d.Unset;
    }

    /// <summary>
    /// Evaluate a mesh color at a set of barycentric coordinates.
    /// </summary>
    /// <param name="meshPoint">MeshPoint instance contiaining a valid Face Index and Barycentric coordinates.</param>
    /// <returns>The interpolated vertex color on the mesh or Color.Transparent if the faceIndex is not valid, 
    /// if the barycentric coordinates could not be evaluated, or if there are no colors defined on the mesh.</returns>
    [ConstOperation]
    public Color ColorAt(MeshPoint meshPoint)
    {
      if (meshPoint == null)
        throw new ArgumentNullException("meshPoint");
      return ColorAt(meshPoint.FaceIndex, meshPoint.T[0], meshPoint.T[1], meshPoint.T[2], meshPoint.T[3]);
    }
    /// <summary>
    /// Evaluate a mesh normal at a set of barycentric coordinates. Barycentric coordinates must 
    /// be assigned in accordance with the rules as defined by <see cref="MeshPoint.T">MeshPoint.T</see>.
    /// </summary>
    /// <param name="faceIndex">Index of triangle or quad to evaluate.</param>
    /// <param name="t0">First barycentric coordinate.</param>
    /// <param name="t1">Second barycentric coordinate.</param>
    /// <param name="t2">Third barycentric coordinate.</param>
    /// <param name="t3">Fourth barycentric coordinate. If the face is a triangle, this coordinate will be ignored.</param>
    /// <returns>The interpolated vertex color on the mesh or Color.Transparent if the faceIndex is not valid, 
    /// if the barycentric coordinates could not be evaluated, or if there are no colors defined on the mesh.</returns>
    /// <remarks>Coordinate 0,0,0,0 is not a valid set of barycentric coordinates. The sum of t0 to t3 should be 1.</remarks>
    [ConstOperation]
    public Color ColorAt(int faceIndex, double t0, double t1, double t2, double t3)
    {
      IntPtr const_ptr_this = ConstPointer();
      int argb = UnsafeNativeMethods.ON_Mesh_MeshColorAt(const_ptr_this, faceIndex, t0, t1, t2, t3);

      if (argb < 0)
        return Color.Transparent;
      Color col = Color.FromArgb(argb);
      col = Color.FromArgb(255, col.B, col.G, col.R);
      return col;
    }

    /// <summary>
    /// Pulls a collection of points to a mesh.
    /// </summary>
    /// <param name="points">An array, a list or any enumerable set of points.</param>
    /// <returns>An array of points. This can be empty.</returns>
    [ConstOperation]
    public Point3d[] PullPointsToMesh(IEnumerable<Point3d> points)
    {
      var rc = new List<Point3d>();
      foreach (var point in points)
      {
        Point3d closest = ClosestPoint(point);
        if (closest.IsValid)
          rc.Add(closest);
      }
      return rc.ToArray();
    }

    /// <summary>
    /// Makes a new mesh with vertices offset a distance in the opposite direction of the existing vertex normals.
    /// Same as Mesh.Offset(distance, false)
    /// </summary>
    /// <param name="distance">A distance value to use for offsetting.</param>
    /// <returns>A new mesh on success, or null on failure.</returns>
    [ConstOperation]
    public Mesh Offset(double distance)
    {
      return Offset(distance, false);
    }

    /// <summary>
    /// Makes a new mesh with vertices offset a distance in the opposite direction of the existing vertex normals.
    /// Optionally, based on the value of solidify, adds the input mesh and a ribbon of faces along any naked edges.
    /// If solidify is false it acts exactly as the Offset(distance) function.
    /// </summary>
    /// <param name="distance">A distance value.</param>
    /// <param name="solidify">true if the mesh should be solidified.</param>
    /// <returns>A new mesh on success, or null on failure.</returns>
    [ConstOperation]
    public Mesh Offset(double distance, bool solidify)
    {
      Vector3d direction = Vector3d.Unset;
      IntPtr const_ptr_this = ConstPointer();
      IntPtr ptr_new_mesh = UnsafeNativeMethods.RHC_RhinoMeshOffset(const_ptr_this, distance, solidify, ref direction);
      if (IntPtr.Zero == ptr_new_mesh)
        return null;
      return new Mesh(ptr_new_mesh, null);
    }

    /// <summary>
    /// Makes a new mesh with vertices offset a distance along the direction parameter.
    /// Optionally, based on the value of solidify, adds the input mesh and a ribbon of faces along any naked edges.
    /// If solidify is false it acts exactly as the Offset(distance) function.
    /// </summary>
    /// <param name="distance">A distance value.</param>
    /// <param name="solidify">true if the mesh should be solidified.</param>
    /// <param name="direction">Direction of offset for all vertices.</param>
    /// <returns>A new mesh on success, or null on failure.</returns>
    [ConstOperation]
    public Mesh Offset(double distance, bool solidify, Vector3d direction)
    {
      IntPtr const_ptr_this = ConstPointer();
      IntPtr ptr_new_mesh = UnsafeNativeMethods.RHC_RhinoMeshOffset(const_ptr_this, distance, solidify, ref direction);
      if (IntPtr.Zero == ptr_new_mesh)
        return null;
      return new Mesh(ptr_new_mesh, null);
    }

    /// <summary>
    /// Collapses multiple mesh faces, with greater/less than edge length, based on the principles 
    /// found in Stan Melax's mesh reduction PDF, 
    /// see http://pomax.nihongoresources.com/downloads/PolygonReduction.pdf
    /// </summary>
    /// <param name="bGreaterThan">Determines whether edge with lengths greater than or less than edgeLength are collapsed.</param>
    /// <param name="edgeLength">Length with which to compare to edge lengths.</param>
    /// <returns>Number of edges (faces) that were collapsed.</returns>
    /// <remarks>
    /// This number may differ from the initial number of edges that meet
    /// the input criteria because the lengths of some initial edges may be altered as other edges are collapsed.
    /// </remarks>
    public int CollapseFacesByEdgeLength(bool bGreaterThan, double edgeLength)
    {
      IntPtr ptr_this = NonConstPointer();
      return UnsafeNativeMethods.RHC_RhinoCollapseMeshEdges(ptr_this, bGreaterThan, edgeLength);
    }

    /// <summary>
    /// Collapses multiple mesh faces, with areas less than LessThanArea and greater than GreaterThanArea, 
    /// based on the principles found in Stan Melax's mesh reduction PDF, 
    /// see http://pomax.nihongoresources.com/downloads/PolygonReduction.pdf
    /// </summary>
    /// <param name="lessThanArea">Area in which faces are selected if their area is less than or equal to.</param>
    /// <param name="greaterThanArea">Area in which faces are selected if their area is greater than or equal to.</param>
    /// <returns>Number of faces that were collapsed in the process.</returns>
    /// <remarks>
    /// This number may differ from the initial number of faces that meet
    /// the input criteria because the areas of some initial faces may be altered as other faces are collapsed.
    /// The face area must be both less than LessThanArea AND greater than GreaterThanArea in order to be considered.  
    /// Use large numbers for lessThanArea or zero for greaterThanArea to simulate an OR.
    /// </remarks>
    public int CollapseFacesByArea(double lessThanArea, double greaterThanArea)
    {
      IntPtr ptr_this = NonConstPointer();
      return UnsafeNativeMethods.RHC_RhinoCollapseMeshFacesByArea(ptr_this, lessThanArea, greaterThanArea);
    }

    /// <summary>
    /// Collapses a multiple mesh faces, determined by face aspect ratio, based on criteria found in Stan Melax's polygon reduction,
    /// see http://pomax.nihongoresources.com/downloads/PolygonReduction.pdf
    /// </summary>
    /// <param name="aspectRatio">Faces with an aspect ratio less than aspectRatio are considered as candidates.</param>
    /// <returns>Number of faces that were collapsed in the process.</returns>
    /// <remarks>
    /// This number may differ from the initial number of faces that meet 
    /// the input criteria because the aspect ratios of some initial faces may be altered as other faces are collapsed.
    /// </remarks>
    public int CollapseFacesByByAspectRatio(double aspectRatio)
    {
      IntPtr ptr_this = NonConstPointer();
      return UnsafeNativeMethods.RHC_RhinoCollapseMeshFacesByAspectRatio(ptr_this, aspectRatio);
    }

    /// <summary>
    /// Allows to obtain unsafe pointers to the underlying unmanaged data structures of the mesh.
    /// </summary>
    /// <param name="writable">true if user will need to write onto the structure. false otherwise.</param>
    /// <returns>A lock that needs to be released.</returns>
    /// <remarks>The lock implements the IDisposable interface, and one call of its
    /// <see cref="IDisposable.Dispose()"/> or <see cref="ReleaseUnsafeLock"/> will update the data structure as required.
    /// This can be achieved with a using statement (Using in Vb.Net).</remarks>
    public MeshUnsafeLock GetUnsafeLock(bool writable)
    {
      return new MeshUnsafeLock(this, writable);
    }

    /// <summary>
    /// Updates the Mesh data with the information that was stored via the <see cref="MeshUnsafeLock"/>.
    /// </summary>
    /// <param name="meshData">The data that will be unlocked.</param>

    public void ReleaseUnsafeLock(MeshUnsafeLock meshData)
    {
      meshData.Release();
    }

    /// <summary>
    /// Constructs new mesh from the current one, with shut lining applied to it.
    /// </summary>
    /// <param name="faceted">Specifies whether the shutline is faceted.</param>
    /// <param name="tolerance">The tolerance of the shutline.</param>
    /// <param name="curves">A collection of curve arguments.</param>
    /// <returns>A new mesh with shutlining. Null on failure.</returns>
    /// <exception cref="ArgumentNullException">If curves is null.</exception>
    /// <exception cref="InvalidOperationException">If displacement failed
    /// because of an error. The exception message specifies the error.</exception>
    [ConstOperation]
    public Mesh WithShutLining(bool faceted, double tolerance, IEnumerable<ShutLiningCurveInfo> curves)
    {
      if (curves == null) throw new ArgumentNullException("curves");

      var shut_lining_curves = curves as IList<ShutLiningCurveInfo> ?? curves.ToList();
      IntPtr[] native_args = shut_lining_curves.Select(ShutLiningCurveInfo.CreateNativeArgument).ToArray();

      var const_ptr_mesh = ConstPointer();

      IntPtr new_mesh_ptr;
      unsafe
      {
        fixed (IntPtr* ptr_to_array = native_args)
        {
          new_mesh_ptr = UnsafeNativeMethods.RHC_Mesh_RhinoShutlineMesh(
            const_ptr_mesh,
            faceted,
            tolerance,
            shut_lining_curves.Count,
            new IntPtr(ptr_to_array));
        }
      }

      Array.ForEach(native_args, ShutLiningCurveInfo.DeleteNativeArgument);

      if (new_mesh_ptr == IntPtr.Zero)
        throw new InvalidOperationException("WithShutLining failed. This is likely a tolerance issue.");

      return new Mesh(new_mesh_ptr, null);
    }

    /// <summary>
    /// Constructs new mesh from the current one, with displacement applied to it.
    /// </summary>
    /// <param name="displacement">Information on mesh displacement.</param>
    /// <returns>A new mesh with shutlining.</returns>
    /// <exception cref="ArgumentNullException">If displacer is null.</exception>
    /// <exception cref="InvalidOperationException">If displacement failed
    /// because of an error. The exception message specifies the error.</exception>
    [ConstOperation]
    public Mesh WithDisplacement(MeshDisplacementInfo displacement)
    {
      if (displacement == null)
        throw new ArgumentNullException("displacement");

      IntPtr const_ptr_mesh = ConstPointer();
      IntPtr texture_ptr = displacement.Texture.ConstPointer();
      IntPtr mapping_ptr = displacement.Mapping.ConstPointer();

      Transform local_mapping_xform = displacement.MappingTransform;
      Transform local_instance_xform = displacement.InstanceTransform;

      string error_text;
      IntPtr new_mesh;

      using (var sw = new StringWrapper())
      {
        IntPtr string_ptr = sw.NonConstPointer;

        new_mesh = UnsafeNativeMethods.RHC_Mesh_WithDisplacement(
          const_ptr_mesh,
          texture_ptr,
          displacement.Black,
          displacement.White,
          mapping_ptr,
          ref local_mapping_xform,
          ref local_instance_xform,
          displacement.BlackMove,
          displacement.PostWeldAngle,
          displacement.RefineSensitivity,
          displacement.SweepPitch,
          displacement.WhiteMove,
          displacement.ChannelNumber,
          displacement.FaceLimit,
          displacement.FairingAmount,
          displacement.RefineStepCount,
          displacement.MemoryLimit,
          string_ptr
          );

        error_text = sw.ToString();
      }

      if (new_mesh == IntPtr.Zero)
        throw new InvalidOperationException(error_text);

      var rc = new Mesh(new_mesh, null);

      return rc;
    }

    /// <summary>
    /// Constructs new mesh from the current one, with edge softening applied to it.
    /// </summary>
    /// <param name="softeningRadius">The softening radius.</param>
    /// <param name="chamfer">Specifies whether to chamfer the edges.</param>
    /// <param name="faceted">Specifies whether the edges are faceted.</param>
    /// <param name="force">Specifies whether to soften edges despite too large a radius.</param>
    /// <param name="angleThreshold">Threshold angle (in degrees) which controls whether an edge is softened or not.
    /// The angle refers to the angles between the adjacent faces of an edge.</param>
    /// <returns>A new mesh with soft edges.</returns>
    /// <exception cref="InvalidOperationException">If displacement failed
    /// because of an error. The exception message specifies the error.</exception>
    [ConstOperation]
    public Mesh WithEdgeSoftening(double softeningRadius, bool chamfer, bool faceted, bool force, double angleThreshold)
    {
      if (softeningRadius <= 0.0)
        throw new ArgumentOutOfRangeException("softeningRadius");

      IntPtr const_ptr_this = ConstPointer();

      IntPtr new_mesh = UnsafeNativeMethods.RHC_Mesh_RhinoEdgeSoftenMesh(
        const_ptr_this,
        softeningRadius,
        chamfer,
        faceted,
        force,
        angleThreshold);

      if (new_mesh == IntPtr.Zero)
        throw new InvalidOperationException("WithEdgeSoftening failed.");

      return new Mesh(new_mesh, null);
    }
#endif
    #endregion

    internal bool IndexOpBool(UnsafeNativeMethods.MeshIndexOpBoolConst which, int index)
    {
      IntPtr ptr_this = NonConstPointer();
      return UnsafeNativeMethods.ON_Mesh_IndexOpBool(ptr_this, which, index);
    }
    internal bool TopItemIsHidden(UnsafeNativeMethods.MeshTopologyHiddenConst which, int index)
    {
      IntPtr const_ptr_this = ConstPointer();
      return UnsafeNativeMethods.ON_MeshTopology_TopItemIsHidden(const_ptr_this, which, index);
    }

    ///// <summary>
    ///// Gets a value indicating whether or not the mesh has nurbs surface parameters.
    ///// </summary>
    //public bool HasSurfaceParameters
    //{
    //  get
    //  {
    //    IntPtr ptr = ConstPointer();
    //    return UnsafeNativeMethods.ON_Mesh_GetBool(ptr, idxHasSurfaceParameters);
    //  }
    //}

    ///// <summary>
    ///// Gets a value indicating whether or not the mesh has nurbs surface curvature data.
    ///// </summary>
    //public bool HasPrincipalCurvatures
    //{
    //  get
    //  {
    //    IntPtr ptr = ConstPointer();
    //    return UnsafeNativeMethods.ON_Mesh_GetBool(ptr, idxHasPrincipalCurvatures);
    //  }
    //}


    #region topological methods
    /// <summary>
    /// Returns an array of bool values equal in length to the number of vertices in this
    /// mesh. Each value corresponds to a mesh vertex and is set to true if the vertex is
    /// not completely surrounded by faces.
    /// </summary>
    /// <returns>An array of true/false flags that, at each index, reveals if the corresponding
    /// vertex is completely surrounded by faces.</returns>
    [ConstOperation]
    public bool[] GetNakedEdgePointStatus()
    {
      int count = Vertices.Count;
      if (count < 1)
        return null;

      // IMPORTANT!!! - DO NOT marshal arrays of bools. This can cause problems with
      // the marshaler because it will attempt to convert the items into U1 size
      var status = new int[count];
      IntPtr const_ptr_this = ConstPointer();
      if (UnsafeNativeMethods.ON_Mesh_NakedEdgePoints(const_ptr_this, status, count))
      {
        var rc = new bool[count];
        for (int i = 0; i < count; i++)
        {
          if (status[i] != 0)
            rc[i] = true;
        }
        return rc;
      }
      return null;
    }

    //David: I have disabled these. It seems very, very geeky.
    //public bool TransposeSurfaceParameters()
    //{
    //  IntPtr ptr = NonConstPointer();
    //  return UnsafeNativeMethods.ON_Mesh_NonConstBoolOp(ptr, idxTransposeSurfaceParameters);
    //}

    //public bool ReverseSurfaceParameters(int direction)
    //{
    //  IntPtr ptr = NonConstPointer();
    //  return UnsafeNativeMethods.ON_Mesh_Reverse(ptr, false, direction);
    //}

    ///// <summary>
    ///// finds all coincident vertices and merges them if break angle is small enough
    ///// </summary>
    ///// <param name="tolerance">coordinate tols for considering vertices to be coincident.</param>
    ///// <param name="cosineNormalAngle">
    ///// cosine normal angle tolerance in radians
    ///// if vertices are coincident, then they are combined
    ///// if NormalA o NormalB >= this value
    ///// </param>
    ///// <returns>-</returns>
    //public bool CombineCoincidentVertices(Vector3f tolerance, double cosineNormalAngle)
    //{
    //  IntPtr ptr = NonConstPointer();
    //  return UnsafeNativeMethods.ON_Mesh_CombineCoincidentVertices(ptr, tolerance, cosineNormalAngle);
    //}

    #endregion

    /// <summary>
    /// In ancient times (or modern smartphone times), some rendering engines
    /// were only able to process small batches of triangles and the
    /// CreatePartitions() function was provided to partition the mesh into
    /// subsets of vertices and faces that those rendering engines could handle.
    /// </summary>
    /// <param name="maximumVertexCount"></param>
    /// <param name="maximumTriangleCount"></param>
    /// <returns>true on success</returns>
    public bool CreatePartitions(int maximumVertexCount, int maximumTriangleCount)
    {
      IntPtr ptr_this = NonConstPointer();
      return UnsafeNativeMethods.ON_Mesh_CreatePartition(ptr_this, maximumVertexCount, maximumTriangleCount);
    }

    /// <summary>
    /// Number of partition information chunks stored on this mesh based
    /// on the last call to CreatePartitions
    /// </summary>
    public int PartitionCount
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        return UnsafeNativeMethods.ON_Mesh_PartitionCount(const_ptr_this);
      }
    }

    /// <summary>
    /// Retrieves a partition. See <see cref="CreatePartitions"/> for details.
    /// </summary>
    /// <param name="which">The partition index.</param>
    /// <returns></returns>
    [ConstOperation]
    public MeshPart GetPartition(int which)
    {
      IntPtr const_ptr_this = ConstPointer();
      int vi0 = 0, vi1 = 0, fi0 = 0, fi1 = 0;
      int vert_count = 0;
      int tri_count = 0;
      if (UnsafeNativeMethods.ON_Mesh_GetMeshPart(const_ptr_this, which, ref vi0, ref vi1, ref fi0, ref fi1, ref vert_count, ref tri_count))
        return new MeshPart(vi0, vi1, fi0, fi1, vert_count, tri_count);
      return null;
    }

    #region full mesh ngon+faces enumerator access

    /// <summary>
    /// Retrieves a complete enumerable, i.e., one that provides an iterator over every face that is present,
    /// no matter if defined as a triangle, a quad, or a strictly over-four-sided ngon.
    /// </summary>
    /// <returns>The enumerator capable of enumerating through <see cref="Mesh.Ngons">Mesh.Ngons</see>> Mesh.Ngons and Faces</returns>
    public IEnumerable<MeshNgon> GetNgonAndFacesEnumerable()
    {
      // The type is hidden for now. We can decide to show it, if it makes sense to show
      // new methods from the C++ counterpart, but should decide "soon" as IL distinguishes methods by return type.
      // Also, we might store this in a field (in this case, this should rather be a property without "get").
      return new MeshNgonCompleteEnumerable(this);
    }

    /// <summary>
    /// Retrieves the count of items that <see cref="GetNgonAndFacesEnumerable"/> will provide.
    /// </summary>
    /// <returns>The amount of faces that are not part of an ngon + the amount of ngons.</returns>
    [ConstOperation]
    public int GetNgonAndFacesCount()
    {
      var const_ptr_this = ConstPointer();
      return UnsafeNativeMethods.ON_Mesh_NgonIterator_Count(const_ptr_this);
    }

    private class MeshNgonCompleteEnumerable : IEnumerable<MeshNgon>
    {
      readonly Mesh m_mesh;

      internal MeshNgonCompleteEnumerable(Mesh mesh)
      {
        if (mesh == null)
          throw new ArgumentNullException("mesh");
        m_mesh = mesh;
      }

      public IEnumerator<MeshNgon> GetEnumerator()
      {
        return new MeshNgonCompleteEnumerator(m_mesh);
      }

      IEnumerator IEnumerable.GetEnumerator()
      {
        return GetEnumerator();
      }
    }

    private class MeshNgonCompleteEnumerator : IEnumerator<MeshNgon>
    {
      readonly IntPtr m_unmanaged_iterator_const_ptr;
      MeshNgon m_current;
      MeshNgon m_next;

      internal MeshNgonCompleteEnumerator(Mesh mesh)
      {
        if (mesh == null)
          throw new ArgumentNullException("mesh");
        m_unmanaged_iterator_const_ptr = UnsafeNativeMethods.ON_Mesh_NgonIterator_New(mesh.ConstPointer());
        Reset();
      }

      public MeshNgon Current
      {
        get { return m_current; }
      }

      object IEnumerator.Current
      {
        get { return Current; }
      }

      public bool MoveNext()
      {
        m_current = m_next;
        if (m_current != null)
        {
          var possibly_new_ptr = UnsafeNativeMethods.ON_Mesh_NgonIterator_NextNgon(m_unmanaged_iterator_const_ptr);
          m_next = (possibly_new_ptr != IntPtr.Zero) ? new MeshNgon(possibly_new_ptr) : null;
          return true;
        }
        return false;
      }

      public void Reset()
      {
        m_current = null;

        var possibly_new_ptr = UnsafeNativeMethods.ON_Mesh_NgonIterator_FirstNgon(m_unmanaged_iterator_const_ptr);
        m_next = (possibly_new_ptr != IntPtr.Zero) ? new MeshNgon(possibly_new_ptr) : null;
      }

      public void Dispose()
      {
        UnsafeNativeMethods.ON_Mesh_NgonIterator_Delete(m_unmanaged_iterator_const_ptr);
        GC.SuppressFinalize(this);
      }

      ~MeshNgonCompleteEnumerator()
      {
        UnsafeNativeMethods.ON_Mesh_NgonIterator_Delete(m_unmanaged_iterator_const_ptr);
      }
    }

    #endregion

    //[skipping]
    //  bool SetTextureCoordinates( 
    //  bool EvaluateMeshGeometry( const ON_Surface& ); // evaluate surface at tcoords
    //  int GetVertexEdges( 
    //  int GetMeshEdges( 

    //[skipping]
    // bool SwapEdge( int topei );
    //  void DestroyHiddenVertexArray();
    //  const bool* HiddenVertexArray() const;
    //  void SetVertexHiddenFlag( int meshvi, bool bHidden );
    //  bool VertexIsHidden( int meshvi ) const;
    //  bool FaceIsHidden( int meshvi ) const;
    //  const ON_MeshTopology& Topology() const;
    //  void DestroyTopology();
    //  const ON_MeshPartition* Partition() const;
    //  void DestroyPartition();


    // [skipping]
    //  ON_3fVectorArray m_N;
    //  ON_3fVectorArray m_FN;
    //  ON_MappingTag m_Ttag; // OPTIONAL tag for values in m_T[]
    //  ON_2fPointArray m_T;  // OPTIONAL texture coordinates for each vertex
    //  ON_2dPointArray m_S;
    //  ON_Interval m_srf_domain[2]; // surface evaluation domain.
    //  double m_srf_scale[2];
    //  ON_Interval m_packed_tex_domain[2];
    //  bool m_packed_tex_rotate;
    //  bool HasPackedTextureRegion() const;
    //  ON_SimpleArray<ON_SurfaceCurvature> m_K;  // OPTIONAL surface curvatures
    //  ON_MappingTag m_Ctag; // OPTIONAL tag for values in m_C[]
    //  ON_SimpleArray<bool> m_H; // OPTIONAL vertex visibility.
    //  int m_hidden_count;       // number of vertices that are hidden
    //  const ON_Object* m_parent; // runtime parent geometry (use ...::Cast() to get it)

#if RHINO_SDK
    /// <summary>
    /// Reduce polygon count
    /// </summary>
    /// <param name="desiredPolygonCount">desired or target number of faces</param>
    /// <param name="allowDistortion">
    /// If true mesh appearance is not changed even if the target polygon count is not reached
    /// </param>
    /// <param name="accuracy">Integer from 1 to 10 telling how accurate reduction algorithm
    ///  to use. Greater number gives more accurate results
    /// </param>
    /// <param name="normalizeSize">If true mesh is fitted to an axis aligned unit cube until reduction is complete</param>
    /// <returns>True if mesh is successfully reduced and false if mesh could not be reduced for some reason.</returns>
    public bool Reduce(int desiredPolygonCount, bool allowDistortion, int accuracy, bool normalizeSize)
    {
      return Reduce(desiredPolygonCount, allowDistortion, accuracy, normalizeSize, false);
    }

    /// <summary>
    /// Reduce polygon count
    /// </summary>
    /// <param name="desiredPolygonCount">desired or target number of faces</param>
    /// <param name="allowDistortion">
    /// If true mesh appearance is not changed even if the target polygon count is not reached
    /// </param>
    /// <param name="accuracy">Integer from 1 to 10 telling how accurate reduction algorithm
    ///  to use. Greater number gives more accurate results
    /// </param>
    /// <param name="normalizeSize">If true mesh is fitted to an axis aligned unit cube until reduction is complete</param>
    /// <param name="threaded">
    /// If True then will run computation inside a worker thread and ignore any provided CancellationTokens and ProgressReporters.
    /// If False then will run on main thread.</param>
    /// <returns>True if mesh is successfully reduced and false if mesh could not be reduced for some reason.</returns>
    public bool Reduce(int desiredPolygonCount, bool allowDistortion, int accuracy, bool normalizeSize, bool threaded)
    {
      IntPtr ptr_this = NonConstPointer();
      return UnsafeNativeMethods.RHC_RhinoReduceMesh(ptr_this, desiredPolygonCount, allowDistortion, accuracy, normalizeSize,
        IntPtr.Zero, 0, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, threaded);
    }

    /// <summary>Reduce polygon count</summary>
    /// <param name="desiredPolygonCount">desired or target number of faces</param>
    /// <param name="allowDistortion">
    /// If true mesh appearance is not changed even if the target polygon count is not reached
    /// </param>
    /// <param name="accuracy">Integer from 1 to 10 telling how accurate reduction algorithm
    ///  to use. Greater number gives more accurate results
    /// </param>
    /// <param name="normalizeSize">If true mesh is fitted to an axis aligned unit cube until reduction is complete</param>
    /// <param name="cancelToken"></param>
    /// <param name="progress"></param>
    /// <param name="problemDescription"></param>
    /// <returns>True if mesh is successfully reduced and false if mesh could not be reduced for some reason.</returns>
    public bool Reduce(int desiredPolygonCount, bool allowDistortion, int accuracy, bool normalizeSize,
      CancellationToken cancelToken, IProgress<double> progress, out string problemDescription)
    {
      return Reduce(desiredPolygonCount, allowDistortion, accuracy, normalizeSize, cancelToken, progress, out problemDescription, false);
    }

    /// <summary>Reduce polygon count</summary>
    /// <param name="desiredPolygonCount">desired or target number of faces</param>
    /// <param name="allowDistortion">
    /// If true mesh appearance is not changed even if the target polygon count is not reached
    /// </param>
    /// <param name="accuracy">Integer from 1 to 10 telling how accurate reduction algorithm
    ///  to use. Greater number gives more accurate results
    /// </param>
    /// <param name="normalizeSize">If true mesh is fitted to an axis aligned unit cube until reduction is complete</param>
    /// <param name="cancelToken"></param>
    /// <param name="progress"></param>
    /// <param name="problemDescription"></param>
    /// <param name="threaded">
    /// If True then will run computation inside a worker thread and ignore any provided CancellationTokens and ProgressReporters.
    /// If False then will run on main thread.</param>
    /// <returns>True if mesh is successfully reduced and false if mesh could not be reduced for some reason.</returns>
    public bool Reduce(int desiredPolygonCount, bool allowDistortion, int accuracy, bool normalizeSize,
      CancellationToken cancelToken, IProgress<double> progress, out string problemDescription, bool threaded)
    {
      IntPtr ptr_this = NonConstPointer();

      ThreadTerminator terminator = null;
      if (cancelToken != CancellationToken.None)
      {
        terminator = new ThreadTerminator();
        cancelToken.Register(terminator.RequestCancel);
      }
      IntPtr ptr_terminator = terminator == null ? IntPtr.Zero : terminator.NonConstPointer();
      ProgressReporter reporter = null;
      int progress_report_serial_number = 0;
      if (progress != null)
      {
        reporter = new ProgressReporter(progress);
        progress_report_serial_number = reporter.SerialNumber;
        reporter.Enable();
      }

      bool rc;
      using (var sw = new StringWrapper())
      {
        IntPtr ptr_string = sw.NonConstPointer;
        rc = UnsafeNativeMethods.RHC_RhinoReduceMesh(ptr_this, desiredPolygonCount, allowDistortion, accuracy, normalizeSize,
          ptr_terminator, progress_report_serial_number, ptr_string, IntPtr.Zero, IntPtr.Zero, false);
        problemDescription = sw.ToString();
      }
      if (terminator != null)
        terminator.Dispose();
      if (reporter != null)
        reporter.Disable();
      return rc;
    }

    /// <summary>Reduce polygon count</summary>
    /// <param name="parameters">Parameters</param>
    /// <returns>True if mesh is successfully reduced and false if mesh could not be reduced for some reason.</returns>
    public bool Reduce(ReduceMeshParameters parameters)
    {
      return Reduce(parameters, false);
    }

    /// <summary>Reduce polygon count</summary>
    /// <param name="parameters">Parameters</param>
    /// <param name="threaded">
    /// If True then will run computation inside a worker thread and ignore any provided CancellationTokens and ProgressReporters.
    /// If False then will run on main thread.</param>
    /// <returns>True if mesh is successfully reduced and false if mesh could not be reduced for some reason.</returns>
    public bool Reduce(ReduceMeshParameters parameters, bool threaded)
    {
      IntPtr ptr_this = NonConstPointer();

      ThreadTerminator terminator = null;
      if (parameters.CancelToken != CancellationToken.None)
      {
        terminator = new ThreadTerminator();
        parameters.CancelToken.Register(terminator.RequestCancel);
      }
      IntPtr ptr_terminator = terminator == null ? IntPtr.Zero : terminator.NonConstPointer();
      ProgressReporter reporter = null;
      int progress_report_serial_number = 0;
      if (parameters.ProgressReporter != null)
      {
        reporter = new ProgressReporter(parameters.ProgressReporter);
        progress_report_serial_number = reporter.SerialNumber;
        reporter.Enable();
      }

      SimpleArrayInt faceTags = new SimpleArrayInt(parameters.FaceTags);
      IntPtr ptr_face_tags = faceTags.Count == 0 ? IntPtr.Zero : faceTags.NonConstPointer();

      INTERNAL_ComponentIndexArray lockedComponents = new INTERNAL_ComponentIndexArray();
      foreach (ComponentIndex ci in parameters.LockedComponents)
      {
        lockedComponents.Add(ci);
      }

      bool rc;
      using (var sw = new StringWrapper())
      {
        IntPtr ptr_string = sw.NonConstPointer;
        rc = UnsafeNativeMethods.RHC_RhinoReduceMesh(ptr_this,
                                                     parameters.DesiredPolygonCount,
                                                     parameters.AllowDistortion,
                                                     parameters.Accuracy,
                                                     parameters.NormalizeMeshSize,
                                                     ptr_terminator,
                                                     progress_report_serial_number,
                                                     sw.NonConstPointer,
                                                     ptr_face_tags,
                                                     lockedComponents.NonConstPointer(),
                                                     threaded);

        if (rc)
        {
          parameters.Error = sw.ToString();
          if (ptr_face_tags != IntPtr.Zero)
            parameters.FaceTags = faceTags.ToArray();
        }
      }
      if (terminator != null)
        terminator.Dispose();
      if (reporter != null)
        reporter.Disable();
      return rc;
    }

    /// <summary>
    /// Compute thickness metrics for this mesh.
    /// </summary>
    /// <param name="meshes">Meshes to include in thickness analysis.</param>
    /// <param name="maximumThickness">Maximum thickness to consider. Use as small a thickness as possible to speed up the solver.</param>
    /// <returns>Array of thickness measurements.</returns>
    public static MeshThicknessMeasurement[] ComputeThickness(IEnumerable<Mesh> meshes, double maximumThickness)
    {
      return ComputeThickness(meshes, maximumThickness, System.Threading.CancellationToken.None);
    }
    /// <summary>
    /// Compute thickness metrics for this mesh.
    /// </summary>
    /// <param name="meshes">Meshes to include in thickness analysis.</param>
    /// <param name="maximumThickness">Maximum thickness to consider. Use as small a thickness as possible to speed up the solver.</param>
    /// <param name="cancelToken">Computation cancellation token.</param>
    /// <returns>Array of thickness measurements.</returns>
    public static MeshThicknessMeasurement[] ComputeThickness(IEnumerable<Mesh> meshes, double maximumThickness, System.Threading.CancellationToken cancelToken)
    {
      return ComputeThickness(meshes, maximumThickness, -1, cancelToken);
    }
    /// <summary>
    /// Compute thickness metrics for this mesh.
    /// </summary>
    /// <param name="meshes">Meshes to include in thickness analysis.</param>
    /// <param name="maximumThickness">Maximum thickness to consider. Use as small a thickness as possible to speed up the solver.</param>
    /// <param name="sharpAngle">Sharpness angle in radians.</param>
    /// <param name="cancelToken">Computation cancellation token.</param>
    /// <returns>Array of thickness measurements.</returns>
    public static MeshThicknessMeasurement[] ComputeThickness(IEnumerable<Mesh> meshes, double maximumThickness, double sharpAngle, System.Threading.CancellationToken cancelToken)
    {
      if (meshes == null)
        throw new ArgumentNullException("meshes");

      int vertex_count = 0;
      using (var input_meshes = new SimpleArrayMeshPointer())
      {
        foreach (Mesh mesh in meshes)
        {
          if (mesh == null)
            throw new ArgumentException("The mesh collection may not contain null meshes.");
          input_meshes.Add(mesh, true);
          vertex_count += mesh.Vertices.Count;
        }

        IntPtr const_ptr_input_meshes = input_meshes.ConstPointer();
        IntPtr ptr_terminator = IntPtr.Zero;
        if (cancelToken != System.Threading.CancellationToken.None)
        {
          ThreadTerminator terminator = new ThreadTerminator();
          ptr_terminator = terminator.NonConstPointer();
          cancelToken.Register(terminator.RequestCancel);
        }

        // David R: Because I don't know how many measurements C++ will generate, I need to make
        //          an array big enough to contain them all. This is potentially very wasteful.
        MeshThicknessMeasurement[] measurements = new MeshThicknessMeasurement[vertex_count];
        // David R: Hmm, bit icky, but MethodGen translates /*ARRAY*/const ON_MESHTHICKNESS_STRUCT* into IntPtr.
        GCHandle handle = GCHandle.Alloc(measurements, GCHandleType.Pinned);
        IntPtr ptr_measurements = Marshal.UnsafeAddrOfPinnedArrayElement(measurements, 0);
        int count = UnsafeNativeMethods.ON_Mesh_ThicknessProperties(const_ptr_input_meshes, ptr_terminator, maximumThickness, sharpAngle, vertex_count, ptr_measurements);
        handle.Free();
        if (count == 0)
          return new MeshThicknessMeasurement[0];

        Array.Resize(ref measurements, count);
        GC.KeepAlive(meshes);
        return measurements;
      }
    }

    /// <summary>
    /// Constructs contour curves for a mesh, sectioned along a linear axis.
    /// </summary>
    /// <param name="meshToContour">A mesh to contour.</param>
    /// <param name="contourStart">A start point of the contouring axis.</param>
    /// <param name="contourEnd">An end point of the contouring axis.</param>
    /// <param name="interval">An interval distance.</param>
    /// <returns>An array of curves. This array can be empty.</returns>
    /// <example>
    /// <code source='examples\vbnet\ex_makerhinocontours.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_makerhinocontours.cs' lang='cs'/>
    /// <code source='examples\py\ex_makerhinocontours.py' lang='py'/>
    /// </example>
    public static Curve[] CreateContourCurves(Mesh meshToContour, Point3d contourStart, Point3d contourEnd, double interval)
    {
      IntPtr const_ptr_mesh = meshToContour.ConstPointer();
      using (var outputcurves = new SimpleArrayCurvePointer())
      {
        double tolerance = RhinoDoc.ActiveDoc?.ModelAbsoluteTolerance ?? RhinoDoc.DefaultModelAbsoluteTolerance;
        IntPtr ptr_curves = outputcurves.NonConstPointer();
        int count = UnsafeNativeMethods.RHC_MakeRhinoContours2(const_ptr_mesh, contourStart, contourEnd, interval, ptr_curves, tolerance);
        GC.KeepAlive(meshToContour);
        return 0 == count ? new Curve[0] : outputcurves.ToNonConstArray();
      }
    }

    /// <summary>
    /// Constructs contour curves for a mesh, sectioned at a plane.
    /// </summary>
    /// <param name="meshToContour">A mesh to contour.</param>
    /// <param name="sectionPlane">A cutting plane.</param>
    /// <returns>An array of curves. This array can be empty.</returns>
    public static Curve[] CreateContourCurves(Mesh meshToContour, Plane sectionPlane)
    {
      IntPtr const_ptr_mesh = meshToContour.ConstPointer();
      using (var outputcurves = new SimpleArrayCurvePointer())
      {
        double tolerance = RhinoDoc.ActiveDoc?.ModelAbsoluteTolerance ?? RhinoDoc.DefaultModelAbsoluteTolerance;
        IntPtr ptr_curves = outputcurves.NonConstPointer();
        int count = UnsafeNativeMethods.RHC_MakeRhinoContours3(const_ptr_mesh, ref sectionPlane, ptr_curves, tolerance);
        GC.KeepAlive(meshToContour);
        return 0 == count ? new Curve[0] : outputcurves.ToNonConstArray();
      }
    }

#endif
  }

#if RHINO_SDK
  /// <summary>
  /// Permits access to the underlying mesh raw data structures in an unsafe way.
  /// </summary>
  /// <remarks>This lock object needs to be disposed before using the Mesh in other calculations and this can 
  /// be achieved with the using keyword (Using in VB.Net).</remarks>
  public sealed class MeshUnsafeLock : IDisposable
  {
    Mesh m_mesh;

    internal MeshUnsafeLock(Mesh parent, bool writable)
    {
      m_mesh = parent;
      Writable = writable;
    }

    internal bool Writable { get; set; }

    /// <summary>
    /// Retrieves a pointer to the raw mesh vertex array, which uses coordinates
    /// defined with single precision floating point numbers.
    /// </summary>
    /// <param name="length">The length of the array. This value is returned by reference (out in C#).</param>
    /// <returns>The beginning of the vertex array. Item 0 is the first vertex,
    /// and item length-1 is the last valid one.</returns>
    [CLSCompliant(false)]
    public unsafe Point3f* VertexPoint3fArray(out int length)
    {
      if (m_mesh == null) throw new ObjectDisposedException("The lock was released.");

      IntPtr ptr_mesh = Writable ? m_mesh.NonConstPointer() : m_mesh.ConstPointer();
      IntPtr ptr_vertex_array = UnsafeNativeMethods.ON_Mesh_VertexArray_Pointer(ptr_mesh);

      length = m_mesh.Vertices.Count;

      return (Point3f*)ptr_vertex_array.ToPointer();
    }

    void IDisposable.Dispose()
    {
      Release();
    }

    /// <summary>
    /// Releases the lock and updates the underlying unmanaged data structures.
    /// </summary>
    public void Release()
    {
      if (!Writable || m_mesh == null) return;

      IntPtr ptr_this = m_mesh.NonConstPointer();
      UnsafeNativeMethods.ON_Mesh_UnlockMeshData(ptr_this);

      m_mesh = null;
    }
  }
#endif
}

namespace Rhino.Geometry.Collections
{
  /// <summary>
  /// Provides access to the vertices and vertex-related functionality of a mesh.
  /// </summary>
  public class MeshVertexList : IResizableList<Point3f>, IReadOnlyList<Point3f>, IList
  {
    private readonly Mesh m_mesh;

    #region constructors
    internal MeshVertexList(Mesh ownerMesh)
    {
      m_mesh = ownerMesh;
    }
    #endregion

    #region properties
    /// <summary>
    /// Gets or sets the number of mesh vertices.
    /// </summary>
    public int Count
    {
      get
      {
        IntPtr const_ptr_mesh = m_mesh.ConstPointer();
        return UnsafeNativeMethods.ON_Mesh_GetInt(const_ptr_mesh, UnsafeNativeMethods.MeshIntConst.VertexCount);
      }
      set
      {
        if (value >= 0 && value != Count)
        {
          IntPtr ptr_mesh = m_mesh.NonConstPointer();
          UnsafeNativeMethods.ON_Mesh_SetInt(ptr_mesh, UnsafeNativeMethods.MeshIntConst.VertexCount, value);
          UnsafeNativeMethods.ON_Mesh_RepairHiddenArray(ptr_mesh);
        }
      }
    }

    /// <summary>
    /// Gets or sets the total number of vertices the internal data structure can hold without resizing.
    /// </summary>
    public int Capacity
    {
      get
      {
        IntPtr const_ptr_mesh = m_mesh.ConstPointer();
        return UnsafeNativeMethods.ON_Mesh_GetInt(const_ptr_mesh, UnsafeNativeMethods.MeshIntConst.VertexCapacity);
      }
      set
      {
        if (Capacity != value)
        {
          if (value < Count)
            throw new ArgumentOutOfRangeException("value", "Capacity must be larger than or equal to the list Count");

          IntPtr ptr_mesh = m_mesh.NonConstPointer();
          UnsafeNativeMethods.ON_Mesh_SetInt(ptr_mesh, UnsafeNativeMethods.MeshIntConst.VertexCapacity, value);
        }
      }
    }

    /// <summary>
    /// Gets or sets the vertex at the given index. 
    /// The index must be valid or an IndexOutOfRangeException will be thrown.
    /// </summary>
    /// <param name="index">Index of control vertex to access.</param>
    /// <exception cref="IndexOutOfRangeException">Thrown when the index is invalid.</exception>
    /// <returns>The control vertex at [index].</returns>
    public Point3f this[int index]
    {
      get
      {
        if (index < 0 || index >= Count)
          throw new IndexOutOfRangeException();

        var rc = new Point3d();
        IntPtr const_ptr_mesh = m_mesh.ConstPointer();
        UnsafeNativeMethods.ON_Mesh_Vertex(const_ptr_mesh, index, ref rc);
        return new Point3f((float)rc.X, (float)rc.Y, (float)rc.Z);
      }
      set
      {
        if (index < 0 || index >= Count)
          throw new IndexOutOfRangeException();

        IntPtr ptr_mesh = m_mesh.NonConstPointer();
        UnsafeNativeMethods.ON_Mesh_SetVertex(ptr_mesh, index, value.m_x, value.m_y, value.m_z);
      }
    }

    /// <summary>
    /// Set to true if the vertices should be stored in double precision
    /// </summary>
    public bool UseDoublePrecisionVertices
    {
      get
      {
        IntPtr const_ptr_mesh = m_mesh.ConstPointer();
        return UnsafeNativeMethods.ON_Mesh_GetBool(const_ptr_mesh, UnsafeNativeMethods.MeshBoolConst.HasDoublePrecisionVerts);
      }
      set
      {
        if( value != UseDoublePrecisionVertices )
        {
          IntPtr ptr_mesh = m_mesh.NonConstPointer();
          UnsafeNativeMethods.ON_Mesh_UseDoublePrecisionVertices(ptr_mesh, value);
        }
      }
    }

    #endregion

    #region access
    /// <summary>
    /// Clears the Vertex list on the mesh.
    /// </summary>
    public void Clear()
    {
      IntPtr ptr = m_mesh.NonConstPointer();
      UnsafeNativeMethods.ON_Mesh_ClearList(ptr, UnsafeNativeMethods.MeshClearListConst.ClearVertices);
      UnsafeNativeMethods.ON_Mesh_RepairHiddenArray(ptr);
    }

    /// <summary>
    /// Releases all memory allocated to store faces. The list capacity will be 0 after this call.
    /// <para>Subsequent calls can add new items.</para>
    /// </summary>
    public void Destroy()
    {
      IntPtr ptr_mesh = m_mesh.NonConstPointer();
      UnsafeNativeMethods.ON_Mesh_SetInt(ptr_mesh, UnsafeNativeMethods.MeshIntConst.VertexCapacity, 0);
    }

    /// <summary>
    /// Adds a new vertex to the end of the Vertex list.
    /// </summary>
    /// <param name="x">X component of new vertex coordinate.</param>
    /// <param name="y">Y component of new vertex coordinate.</param>
    /// <param name="z">Z component of new vertex coordinate.</param>
    /// <returns>The index of the newly added vertex.</returns>
    /// <example>
    /// <code source='examples\vbnet\ex_addmesh.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_addmesh.cs' lang='cs'/>
    /// <code source='examples\py\ex_addmesh.py' lang='py'/>
    /// </example>
    public int Add(float x, float y, float z)
    {
      return Add(new Point3d(x, y, z));
    }
    /// <summary>
    /// Adds a new vertex to the end of the Vertex list.
    /// </summary>
    /// <param name="x">X component of new vertex coordinate.</param>
    /// <param name="y">Y component of new vertex coordinate.</param>
    /// <param name="z">Z component of new vertex coordinate.</param>
    /// <returns>The index of the newly added vertex.</returns>
    /// <example>
    /// <code source='examples\vbnet\ex_addmesh.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_addmesh.cs' lang='cs'/>
    /// <code source='examples\py\ex_addmesh.py' lang='py'/>
    /// </example>
    public int Add(double x, double y, double z)
    {
      return Add(new Point3d(x, y, z));
    }
    /// <summary>
    /// Adds a new vertex to the end of the Vertex list.
    /// </summary>
    /// <param name="vertex">Location of new vertex.</param>
    /// <returns>The index of the newly added vertex.</returns>
    public int Add(Point3f vertex)
    {
      return Add(new Point3d(vertex.m_x, vertex.m_y, vertex.m_z));
    }
    /// <summary>
    /// Adds a new vertex to the end of the Vertex list.
    /// </summary>
    /// <param name="vertex">Location of new vertex.</param>
    /// <returns>The index of the newly added vertex.</returns>
    public int Add(Point3d vertex)
    {
      int count = Count;
      SetVertex(count, vertex.X, vertex.Y, vertex.Z);

      IntPtr ptr_mesh = m_mesh.NonConstPointer();
      UnsafeNativeMethods.ON_Mesh_RepairHiddenArray(ptr_mesh);
      return count;
    }

    /// <summary>
    /// Adds a series of new vertices to the end of the vertex list.
    /// <para>This overload accepts double-precision points.</para>
    /// </summary>
    /// <param name="vertices">A list, an array or any enumerable set of <see cref="Point3d"/>.</param>
    public void AddVertices(IEnumerable<Point3d> vertices)
    {
      IntPtr ptr_mesh = m_mesh.NonConstPointer();
      int count = Count;
      int index = count;
      foreach (Point3d vertex in vertices)
      {
        var x = vertex.X;
        var y = vertex.Y;
        var z = vertex.Z;
        UnsafeNativeMethods.ON_Mesh_SetVertex(ptr_mesh, index, x, y, z);
        index++;
      }
      UnsafeNativeMethods.ON_Mesh_RepairHiddenArray(ptr_mesh);
    }

    /// <summary>
    /// Adds a series of new vertices to the end of the vertex list.
    /// <para>This overload accepts single-precision points.</para>
    /// </summary>
    /// <param name="vertices">A list, an array or any enumerable set of <see cref="Point3f"/>.</param>
    public void AddVertices(IEnumerable<Point3f> vertices)
    {
      IntPtr ptr_mesh = m_mesh.NonConstPointer();
      int count = Count;
      int index = count;
      foreach (Point3f vertex in vertices)
      {
        float x = vertex.X;
        float y = vertex.Y;
        float z = vertex.Z;
        UnsafeNativeMethods.ON_Mesh_SetVertex(ptr_mesh, index, x, y, z);
        index++;
      }
      UnsafeNativeMethods.ON_Mesh_RepairHiddenArray(ptr_mesh);
    }

    /// <summary>
    /// Sets or adds a vertex to the Vertex List.
    /// <para>If [index] is less than [Count], the existing vertex at [index] will be modified.</para>
    /// <para>If [index] equals [Count], a new vertex is appended to the end of the vertex list.</para> 
    /// <para>If [index] is larger than [Count], the function will return false.</para>
    /// </summary>
    /// <param name="index">Index of vertex to set.</param>
    /// <param name="x">X component of vertex location.</param>
    /// <param name="y">Y component of vertex location.</param>
    /// <param name="z">Z component of vertex location.</param>
    /// <returns>true on success, false on failure.</returns>
    public bool SetVertex(int index, float x, float y, float z)
    {
      IntPtr ptr_mesh = m_mesh.NonConstPointer();
      bool rc = UnsafeNativeMethods.ON_Mesh_SetVertex(ptr_mesh, index, x, y, z);
      return rc;
    }

    /// <summary>
    /// Sets or adds a vertex to the Vertex List.
    /// <para>If [index] is less than [Count], the existing vertex at [index] will be modified.</para>
    /// <para>If [index] equals [Count], a new vertex is appended to the end of the vertex list.</para> 
    /// <para>If [index] is larger than [Count], the function will return false.</para>
    /// </summary>
    /// <param name="index">Index of vertex to set.</param>
    /// <param name="x">X component of vertex location.</param>
    /// <param name="y">Y component of vertex location.</param>
    /// <param name="z">Z component of vertex location.</param>
    /// <param name="updateNormals">Set to true if you'd like the vertex and face normals impacted by the change updated.</param>
    /// <returns>true on success, false on failure.</returns>
    public bool SetVertex(int index, double x, double y, double z, bool updateNormals)
    {
      IntPtr ptr_mesh = m_mesh.NonConstPointer();
      bool rc = UnsafeNativeMethods.ON_Mesh_SetVertexWithNormal(ptr_mesh, index, x, y, z, updateNormals);
      return rc;
    }

    /// <summary>
    /// Sets or adds a vertex to the Vertex List.
    /// <para>If [index] is less than [Count], the existing vertex at [index] will be modified.</para>
    /// <para>If [index] equals [Count], a new vertex is appended to the end of the vertex list.</para> 
    /// <para>If [index] is larger than [Count], the function will return false.</para>
    /// </summary>
    /// <param name="index">Index of vertex to set.</param>
    /// <param name="x">X component of vertex location.</param>
    /// <param name="y">Y component of vertex location.</param>
    /// <param name="z">Z component of vertex location.</param>
    /// <returns>true on success, false on failure.</returns>
    public bool SetVertex(int index, double x, double y, double z)
    {
      IntPtr ptr_mesh = m_mesh.NonConstPointer();
      bool rc = UnsafeNativeMethods.ON_Mesh_SetVertex(ptr_mesh, index, x, y, z);
      return rc;
    }
    /// <summary>
    /// Sets or adds a vertex to the Vertex List.
    /// <para>If [index] is less than [Count], the existing vertex at [index] will be modified.</para>
    /// <para>If [index] equals [Count], a new vertex is appended to the end of the vertex list.</para> 
    /// <para>If [index] is larger than [Count], the function will return false.</para>
    /// </summary>
    /// <param name="index">Index of vertex to set.</param>
    /// <param name="vertex">Vertex location.</param>
    /// <returns>true on success, false on failure.</returns>
    public bool SetVertex(int index, Point3f vertex)
    {
      return SetVertex(index, vertex.X, vertex.Y, vertex.Z);
    }
    /// <summary>
    /// Sets or adds a vertex to the Vertex List.
    /// <para>If [index] is less than [Count], the existing vertex at [index] will be modified.</para>
    /// <para>If [index] equals [Count], a new vertex is appended to the end of the vertex list.</para> 
    /// <para>If [index] is larger than [Count], the function will return false.</para>
    /// </summary>
    /// <param name="index">Index of vertex to set.</param>
    /// <param name="vertex">Vertex location.</param>
    /// <returns>true on success, false on failure.</returns>
    public bool SetVertex(int index, Point3d vertex)
    {
      return SetVertex(index, vertex.X, vertex.Y, vertex.Z);
    }

    /// <summary>
    /// Gets a value indicating whether or not a vertex is hidden.
    /// </summary>
    /// <param name="vertexIndex">Index of vertex to query.</param>
    /// <returns>true if the vertex is hidden, false if it is not.</returns>
    public bool IsHidden(int vertexIndex)
    {
      IntPtr const_ptr_mesh = m_mesh.ConstPointer();
      return UnsafeNativeMethods.ON_Mesh_GetHiddenValue(const_ptr_mesh, vertexIndex);
    }

    /// <summary>
    /// Hides the vertex at the given index.
    /// </summary>
    /// <param name="vertexIndex">Index of vertex to hide.</param>
    public void Hide(int vertexIndex)
    {
      IntPtr const_ptr_mesh = m_mesh.ConstPointer();
      // If vertex is already hidden, DO NOT copy the mesh but return right away.
      if (UnsafeNativeMethods.ON_Mesh_GetHiddenValue(const_ptr_mesh, vertexIndex))
        return;

      IntPtr ptr_mesh = m_mesh.NonConstPointer();
      UnsafeNativeMethods.ON_Mesh_HiddenVertexOp(ptr_mesh, 0, UnsafeNativeMethods.MeshHiddenVertexOpConst.EnsureHiddenList);
      UnsafeNativeMethods.ON_Mesh_HiddenVertexOp(ptr_mesh, vertexIndex, UnsafeNativeMethods.MeshHiddenVertexOpConst.HideVertex);
    }
    /// <summary>
    /// Shows the vertex at the given index.
    /// </summary>
    /// <param name="vertexIndex">Index of vertex to show.</param>
    public void Show(int vertexIndex)
    {
      IntPtr const_ptr_mesh = m_mesh.ConstPointer();
      // If vertex is already visible, DO NOT copy the mesh but return right away.
      if (!UnsafeNativeMethods.ON_Mesh_GetHiddenValue(const_ptr_mesh, vertexIndex))
        return;

      IntPtr ptr_mesh = m_mesh.NonConstPointer();
      UnsafeNativeMethods.ON_Mesh_HiddenVertexOp(ptr_mesh, 0, UnsafeNativeMethods.MeshHiddenVertexOpConst.EnsureHiddenList);
      UnsafeNativeMethods.ON_Mesh_HiddenVertexOp(ptr_mesh, vertexIndex, UnsafeNativeMethods.MeshHiddenVertexOpConst.ShowVertex);
    }
    /// <summary>
    /// Hides all vertices in the mesh.
    /// </summary>
    public void HideAll()
    {
      IntPtr ptr_mesh = m_mesh.NonConstPointer();
      UnsafeNativeMethods.ON_Mesh_HiddenVertexOp(ptr_mesh, 0, UnsafeNativeMethods.MeshHiddenVertexOpConst.EnsureHiddenList);
      UnsafeNativeMethods.ON_Mesh_HiddenVertexOp(ptr_mesh, 0, UnsafeNativeMethods.MeshHiddenVertexOpConst.HideAll);
    }
    /// <summary>
    /// Shows all vertices in the mesh.
    /// </summary>
    public void ShowAll()
    {
      IntPtr ptr_mesh = m_mesh.NonConstPointer();
      UnsafeNativeMethods.ON_Mesh_HiddenVertexOp(ptr_mesh, 0, UnsafeNativeMethods.MeshHiddenVertexOpConst.ShowAll);
    }
    #endregion

    #region methods
    /// <summary>
    /// Removes all vertices that are currently not used by the Face list.
    /// </summary>
    /// <returns>The number of unused vertices that were removed.</returns>
    public int CullUnused()
    {
      IntPtr ptr_mesh = m_mesh.NonConstPointer();
      return UnsafeNativeMethods.ON_Mesh_CullOp(ptr_mesh, false);
    }

    /// <summary>
    /// Merges identical vertices.
    /// </summary>
    /// <param name="ignoreNormals">
    /// If true, vertex normals will not be taken into consideration when comparing vertices.
    /// </param>
    /// <param name="ignoreAdditional">
    /// If true, texture coordinates, colors, and principal curvatures 
    /// will not be taken into consideration when comparing vertices.
    /// </param>
    /// <returns>
    /// true if the mesh is changed, in which case the mesh will have fewer vertices than before.
    /// </returns>
    public bool CombineIdentical(bool ignoreNormals, bool ignoreAdditional)
    {
      IntPtr ptr = m_mesh.NonConstPointer();
      return UnsafeNativeMethods.ON_Mesh_CombineIdenticalVertices(ptr, ignoreNormals, ignoreAdditional);
    }

#if RHINO_SDK
    /// <summary>
    /// Moves mesh vertices that belong to naked edges to neighboring vertices, within the specified distance.
    /// <para>This forces unaligned mesh vertices to the same location and is helpful to clean meshes for 3D printing.</para>
    /// <para>See the <code>_AlignMeshVertices</code> Rhino command for more information.</para>
    /// </summary>
    /// <param name="distance">Distance that should not be exceed when modifying the mesh.</param>
    /// <param name="whichVertices">If not null, defines which vertices should be considered for adjustment.</param>
    /// <returns>If the operation succeeded, the number of moved vertices, or -1 on error.</returns>
    public int Align(double distance, IEnumerable<bool> whichVertices = null)
    {
      bool[] which_vertices_array;

      if (whichVertices == null)
      {
        which_vertices_array = new bool[Count];
        for (int i = 0; i < which_vertices_array.Length; i++) which_vertices_array[i] = true;
      }
      else
      {
        int vertices_counts;
        which_vertices_array = RhinoListHelpers.GetConstArray(whichVertices, out vertices_counts);

        if (vertices_counts != Count)
          throw new ArgumentException("whichVertices has to have the same length as Mesh.Vertices.Count", "whichVertices");
      }

      var ptr_mesh = m_mesh.NonConstPointer();
      return UnsafeNativeMethods.RHC_MeshVerticesAlign(ptr_mesh, distance, Count, which_vertices_array);
    }

    /// <summary>
    /// Moves mesh vertices that belong to naked edges to neighboring vertices, within the specified distance.
    /// <para>This forces unaligned mesh vertices to the same location and is helpful to clean meshes for 3D printing.</para>
    /// <para>See the <code>_AlignMeshVertices</code> Rhino command for more information.</para>
    /// </summary>
    /// <param name="meshes">The enumerable of meshes that need to have vertices adjusted.</param>
    /// <param name="distance">Distance that should not be exceed when modifying the mesh.</param>
    /// <param name="whichVertices">If not null, defines which vertices should be considered for adjustment.
    /// <para>If this parameter is non-null, then all items within it have to be non-null as well, defining for each mesh, which vertices to adjust.</para></param>
    /// <returns>If the operation succeeded, the number of moved vertices, or -1 on error.</returns>
    public static int Align(IEnumerable<Mesh> meshes, double distance, IEnumerable<IEnumerable<bool>> whichVertices = null)
    {
      if (meshes == null) throw new ArgumentNullException("meshes");

      var meshes_buffered = meshes as Mesh[] ?? new List<Mesh>(meshes).ToArray(); //no extra Linq usings.

      var array_mesh_ptrs = new SimpleArrayMeshPointer();
      foreach (var mesh in meshes_buffered) array_mesh_ptrs.Add(mesh, false);

      if (whichVertices == null)
      {
        var which_vertices_array_array = new bool[array_mesh_ptrs.Count][];
        int mc = 0;
        foreach (var mesh in meshes_buffered)
        {
          var which_vertices_array = new bool[mesh.Vertices.Count];
          for (int i = 0; i < which_vertices_array.Length; i++) which_vertices_array[i] = true;
          which_vertices_array_array[mc++] = which_vertices_array;
        }

        whichVertices = which_vertices_array_array;
      }

      using (var which = ArrayOfTArrayMarshal.Create(whichVertices, NullItemsResponse.Throw))
      {
        var meshes_non_const_array = array_mesh_ptrs.NonConstPointer();

        return UnsafeNativeMethods.RHC_MeshesVerticesAlign(meshes_non_const_array, distance,
          which.Length, which.LengthsOfPinnedObjects, which.AddressesOfPinnedObjects);
      }
    }
#endif

    /// <summary>
    /// Gets a list of all of the faces that share a given vertex.
    /// </summary>
    /// <param name="vertexIndex">The index of a vertex in the mesh.</param>
    /// <returns>An array of indices of faces on success, null on failure.</returns>
    public int[] GetVertexFaces(int vertexIndex)
    {
      IntPtr const_ptr_mesh = m_mesh.ConstPointer();
      var face_ids = new SimpleArrayInt();
      int count = UnsafeNativeMethods.ON_Mesh_GetVertexFaces(const_ptr_mesh, face_ids.m_ptr, vertexIndex);
      int[] ids = null;
      if (count > 0)
        ids = face_ids.ToArray();
      face_ids.Dispose();
      return ids;
    }

    /// <summary>
    /// Gets a list of other vertices which are "topologically" identical
    /// to this vertex.
    /// </summary>
    /// <param name="vertexIndex">A vertex index in the mesh.</param>
    /// <returns>
    /// Array of indices of vertices that are topoligically the same as this vertex. The
    /// array includes vertexIndex. Returns null on failure.
    /// </returns>
    public int[] GetTopologicalIndenticalVertices(int vertexIndex)
    {
      IntPtr const_ptr_mesh = m_mesh.ConstPointer();
      int[] ids = null;
      using (var vertex_ids = new SimpleArrayInt())
      {
        int count = UnsafeNativeMethods.ON_Mesh_GetTopologicalVertices(const_ptr_mesh, vertex_ids.m_ptr, vertexIndex);
        if (count > 0)
          ids = vertex_ids.ToArray();
      }
      return ids;
    }

    /// <summary>
    /// Gets indices of all vertices that form "edges" with a given vertex index.
    /// </summary>
    /// <param name="vertexIndex">The index of a vertex to query.</param>
    /// <returns>An array of vertex indices that are connected with the specified vertex.</returns>
    public int[] GetConnectedVertices(int vertexIndex)
    {
      IntPtr const_ptr_mesh = m_mesh.ConstPointer();
      int[] ids = null;
      using (var vertex_ids = new SimpleArrayInt())
      {
        int count = UnsafeNativeMethods.ON_Mesh_GetConnectedVertices(const_ptr_mesh, vertex_ids.m_ptr, vertexIndex);
        if (count > 0)
          ids = vertex_ids.ToArray();
      }
      return ids;
    }

    /// <summary>
    /// Get double precision location at a given index
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public Point3d Point3dAt(int index)
    {
      IntPtr const_ptr_mesh = m_mesh.ConstPointer();
      var pt = new Point3d();
      UnsafeNativeMethods.ON_Mesh_Vertex(const_ptr_mesh, index, ref pt);
      return pt;
    }

    /// <summary>
    /// Copies all vertices to a new array of <see cref="Point3f"/>.
    /// </summary>
    /// <returns>A new array.</returns>
    public Point3f[] ToPoint3fArray()
    {
      int count = Count;
      var rc = new Point3f[count];
      IntPtr const_ptr_mesh = m_mesh.ConstPointer();
      for (int i = 0; i < count; i++)
      {
        var pt = new Point3d();
        UnsafeNativeMethods.ON_Mesh_Vertex(const_ptr_mesh, i, ref pt);
        rc[i] = new Point3f((float)pt.X, (float)pt.Y, (float)pt.Z);
      }
      return rc;
    }

    /// <summary>
    /// Copies all vertices to a new array of <see cref="Point3d"/>.
    /// </summary>
    /// <returns>A new array.</returns>
    public Point3d[] ToPoint3dArray()
    {
      int count = Count;
      var rc = new Point3d[count];
      IntPtr const_ptr_mesh = m_mesh.ConstPointer();
      for (int i = 0; i < count; i++)
      {
        var pt = new Point3d();
        UnsafeNativeMethods.ON_Mesh_Vertex(const_ptr_mesh, i, ref pt);
        rc[i] = pt;
      }
      return rc;
    }

    /// <summary>
    /// Copies all vertices to a linear array of float in x,y,z order
    /// </summary>
    /// <returns>The float array.</returns>
    public float[] ToFloatArray()
    {
      int count = Count;
      var rc = new float[count * 3];
      IntPtr const_ptr_mesh = m_mesh.ConstPointer();
      var pt = new Point3d();
      int index = 0;
      // There is a much more efficient way to do this with
      // marshalling the whole array at once, but this will
      // do for now
      for (int i = 0; i < count; i++)
      {
        UnsafeNativeMethods.ON_Mesh_Vertex(const_ptr_mesh, i, ref pt);
        rc[index++] = (float)pt.X;
        rc[index++] = (float)pt.Y;
        rc[index++] = (float)pt.Z;
      }
      return rc;
    }

    /// <summary>
    /// Removes the vertex at the given index and all faces that reference that index.
    /// </summary>
    /// <param name="index">Index of vertex to remove.</param>
    /// <param name="shrinkFaces">If true, quads that reference the deleted vertex will be converted to triangles.</param>
    /// <returns>true on success, false on failure.</returns>
    public bool Remove(int index, bool shrinkFaces)
    {
      return Remove(new int[] { index }, shrinkFaces);
    }
    ///// <summary>
    ///// Removes the vertices at the given indices and all faces that reference those vertices.
    ///// </summary>
    ///// <param name="indices">Vertex indices to remove.</param>
    ///// <param name="shrinkFaces">If true, quads that reference the deleted vertex will be converted to triangles.</param>
    ///// <returns>true on success, false on failure.</returns>
    //public bool Remove(IEnumerable<int> indices, bool shrinkFaces)
    //{
    //  if (indices == null)
    //    throw new ArgumentNullException("indices");
    //  var idx = new List<int>(indices); // DavidR: Just switching to a HashSet<int> would speed things up.
    //  if (idx.Count == 0)
    //    return true;

    //  int max = Count;
    //  foreach (int index in idx)
    //  {
    //    if (index < 0)
    //      throw new IndexOutOfRangeException("Vertex index must be larger than or equal to zero");
    //    if (index >= max)
    //      throw new IndexOutOfRangeException("Vertex index must be smaller than the size of the collection");
    //  }

    //  var faces = m_mesh.Faces;
    //  var faceidx = new List<int>();

    //  for (int i = 0; i < faces.Count; i++)
    //  {
    //    var face = faces[i];
    //    int k = -1;
    //    int n = 0;

    //    if (idx.Contains(face.A)) { k = 0; n++; }
    //    if (idx.Contains(face.B)) { k = 1; n++; }
    //    if (n >= 2) { faceidx.Add(i); continue; }
    //    if (idx.Contains(face.C)) { k = 2; n++; }
    //    if (n >= 2) { faceidx.Add(i); continue; }
    //    if (face.IsQuad && idx.Contains(face.D)) { k = 3; n++; }
    //    if (n >= 2) { faceidx.Add(i); continue; }

    //    // Do not change face.
    //    if (n == 0) { continue; }

    //    // Always remove triangles.
    //    if (face.IsTriangle) { faceidx.Add(i); continue; }

    //    // Remove quads when shrinking is not allowed.
    //    if (face.IsQuad && !shrinkFaces) { faceidx.Add(i); continue; }

    //    // Convert quad to triangle.
    //    switch (k)
    //    {
    //      case 0:
    //        face.A = face.B;
    //        face.B = face.C;
    //        face.C = face.D;
    //        break;

    //      case 1:
    //        face.B = face.C;
    //        face.C = face.D;
    //        break;

    //      case 2:
    //        face.C = face.D;
    //        break;

    //      case 3:
    //        face.D = face.C;
    //        break;
    //    }
    //    faces.SetFace(i, face);
    //  }

    //  if (faceidx.Count > 0)
    //  {
    //    faces.DeleteFaces(faceidx);
    //  }

    //  CullUnused();
    //  return true;
    //}
    /// <summary>
    /// Removes the vertices at the given indices and all faces that reference those vertices.
    /// </summary>
    /// <param name="indices">Vertex indices to remove.</param>
    /// <param name="shrinkFaces">If true, quads that reference the deleted vertex will be converted to triangles.</param>
    /// <returns>true on success, false on failure.</returns>
    public bool Remove(IEnumerable<int> indices, bool shrinkFaces)
    {
      if (indices == null)
        throw new ArgumentNullException("indices");

      // Using a bool array will result in O(1) lookup, instead of O(N) for List<int>.
      bool[] map = new bool[Count];
      int indexCount = 0;
      foreach (int index in indices)
      {
        if (index < 0)
          throw new IndexOutOfRangeException(string.Format("Vertex index [{0}] may not be negative.", index));
        if (index >= map.Length)
          throw new IndexOutOfRangeException(string.Format("Vertex index [{0}] must be strictly less than Count [{1}]", index, Count));
        map[index] = true;
        indexCount++;
      }

      if (indexCount == 0)
        return true;

      var faces = m_mesh.Faces;
      var faceidx = new List<int>(m_mesh.Faces.Count);
      for (int i = 0; i < faces.Count; i++)
      {
        var face = faces[i];
        if (shrinkFaces && face.IsQuad)
        {
          int k = -1;
          int n = 0;
          if (map[face.A]) { k = 0; n++; }
          if (map[face.B]) { k = 1; n++; }
          if (map[face.C]) { k = 2; n++; }
          if (map[face.D]) { k = 3; n++; }

          // Face was unaffected.
          if (n == 0)
            continue;

          // Face must be removed completely.
          if (n > 1)
          {
            faceidx.Add(i);
            continue;
          }

          // Convert quad to triangle.
          switch (k)
          {
            case 0:
              face.A = face.B;
              face.B = face.C;
              face.C = face.D;
              break;

            case 1:
              face.B = face.C;
              face.C = face.D;
              break;

            case 2:
              face.C = face.D;
              break;

            case 3:
              face.D = face.C;
              break;
          }
          faces.SetFace(i, face);
        }
        else
        {
          // Since the test is really simple for triangles and non-shrinking quads, special case it.
          if (map[face.A] || map[face.B] || map[face.C] || map[face.D])
            faceidx.Add(i);
        }
      }

      if (faceidx.Count > 0)
        faces.DeleteFaces(faceidx);

      CullUnused();
      return true;
    }
    #endregion

    #region IResizableList<Point3f>, IList and related implementations

    int IList<Point3f>.IndexOf(Point3f item)
    {
      return GenericIListImplementation.IndexOf(this, item);
    }

    void IList<Point3f>.Insert(int index, Point3f item)
    {
      GenericIListImplementation.Insert(this, index, item);
    }

    void IList<Point3f>.RemoveAt(int index)
    {
      GenericIListImplementation.RemoveAt(this, index);
    }

    void ICollection<Point3f>.Add(Point3f item)
    {
      Add(item);
    }

    bool ICollection<Point3f>.Contains(Point3f item)
    {
      return GenericIListImplementation.IndexOf(this, item) != -1;
    }

    void ICollection<Point3f>.CopyTo(Point3f[] array, int arrayIndex)
    {
      GenericIListImplementation.CopyTo(this, array, arrayIndex);
    }

    bool ICollection<Point3f>.IsReadOnly
    {
      get { return false; }
    }

    bool ICollection<Point3f>.Remove(Point3f item)
    {
      return GenericIListImplementation.Remove(this, item);
    }

    int IList.Add(object value)
    {
      var item = GenericIListImplementation.HelperCoercePoint(value);
      return Add(item);
    }

    bool IList.Contains(object value)
    {
      return GenericIListImplementation.Contains(this, value);
    }

    int IList.IndexOf(object value)
    {
      return GenericIListImplementation.IndexOf(this, value);
    }

    void IList.Insert(int index, object value)
    {
      var item = GenericIListImplementation.HelperCoercePoint(value);
      GenericIListImplementation.Insert(this, index, item);
    }

    bool IList.IsFixedSize
    {
      get { return false; }
    }

    bool IList.IsReadOnly
    {
      get { return false; }
    }

    void IList.Remove(object value)
    {
      var item = GenericIListImplementation.HelperCoercePoint(value);
      GenericIListImplementation.Remove(this, item);
    }

    void IList.RemoveAt(int index)
    {
      GenericIListImplementation.RemoveAt(this, index);
    }

    object IList.this[int index]
    {
      get
      {
        return this[index];
      }
      set
      {
        this[index] = GenericIListImplementation.HelperCoercePoint(value);
      }
    }

    void ICollection.CopyTo(Array array, int index)
    {
      GenericIListImplementation.CopyTo(this, array, index);
    }

    bool ICollection.IsSynchronized
    {
      get { return false; }
    }

    object ICollection.SyncRoot
    {
      get
      {
        throw GenericIListImplementation.MakeNoSyncException(this);
      }
    }

    /// <summary>
    /// Gets an enumerator that yields all mesh vertices (points) in this collection.
    /// </summary>
    /// <returns>The enumerator.</returns>
    public IEnumerator<Point3f> GetEnumerator()
    {
      int count = Count;
      for (int i = 0; i < count; i++) yield return this[i];
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
      return GetEnumerator();
    }
    #endregion
  }

  /// <summary>
  /// Provides access to the mesh topology vertices of a mesh. Topology vertices are
  /// sets of vertices in the MeshVertexList that can topologically be considered the
  /// same vertex.
  /// </summary>
  public class MeshTopologyVertexList : IEnumerable<Point3f>, IList<Point3f>, IList, IReadOnlyList<Point3f>
  {
    private readonly Mesh m_mesh;

    #region constructors
    internal MeshTopologyVertexList(Mesh ownerMesh)
    {
      m_mesh = ownerMesh;
    }
    #endregion

    #region properties
    /// <summary>
    /// Gets or sets the number of mesh topology vertices.
    /// </summary>
    public int Count
    {
      get
      {
        IntPtr const_ptr_mesh = m_mesh.ConstPointer();
        return UnsafeNativeMethods.ON_Mesh_GetInt(const_ptr_mesh, UnsafeNativeMethods.MeshIntConst.MeshTopologyVertexCount);
      }
    }

    /// <summary>
    /// Gets or sets the vertex at the given index. Setting a location adjusts all vertices
    /// in the mesh's vertex list that are defined by this topological vertex
    /// The index must be valid or an IndexOutOfRangeException will be thrown.
    /// </summary>
    /// <param name="index">Index of topology vertex to access.</param>
    /// <returns>The topological vertex at [index].</returns>
    public Point3f this[int index]
    {
      get
      {
        if (index < 0 || index >= Count)
          throw new IndexOutOfRangeException();

        var rc = new Point3f();
        IntPtr ptr = m_mesh.ConstPointer();
        UnsafeNativeMethods.ON_Mesh_TopologyVertex(ptr, index, ref rc);
        return rc;
      }
      set
      {
        if (index < 0 || index >= Count)
          throw new IndexOutOfRangeException();

        IntPtr ptr_mesh = m_mesh.NonConstPointer();
        UnsafeNativeMethods.ON_Mesh_SetTopologyVertex(ptr_mesh, index, value);
      }
    }
    #endregion

    #region methods
    /// <summary>
    /// Gets the topology vertex index for an existing mesh vertex in the mesh's
    /// VertexList.
    /// </summary>
    /// <param name="vertexIndex">Index of a vertex in the Mesh.Vertices.</param>
    /// <returns>Index of a topology vertex in the Mesh.TopologyVertices.</returns>
    public int TopologyVertexIndex(int vertexIndex)
    {
      IntPtr const_ptr_mesh = m_mesh.ConstPointer();
      int rc = UnsafeNativeMethods.ON_Mesh_TopologyVertexIndex(const_ptr_mesh, vertexIndex);
      if (-1 == rc)
        throw new IndexOutOfRangeException();
      return rc;
    }

    /// <summary>
    /// Gets all indices of the mesh vertices that a given topology vertex represents.
    /// </summary>
    /// <param name="topologyVertexIndex">Index of a topology vertex in Mesh.TopologyVertices to query.</param>
    /// <returns>
    /// Indices of all vertices that in Mesh.Vertices that a topology vertex represents.
    /// </returns>
    public int[] MeshVertexIndices(int topologyVertexIndex)
    {
      IntPtr const_ptr_mesh = m_mesh.ConstPointer();
      int count = UnsafeNativeMethods.ON_MeshTopologyVertex_Count(const_ptr_mesh, topologyVertexIndex, true);
      if (-1 == count)
        throw new IndexOutOfRangeException();
      if (count < 1)
        return null;
      var rc = new int[count];
      UnsafeNativeMethods.ON_MeshTopologyVertex_GetIndices(const_ptr_mesh, topologyVertexIndex, count, rc);
      return rc;
    }

    /// <summary>
    /// Returns TopologyVertexIndices for a given mesh face index.
    /// </summary>
    /// <param name="faceIndex">The index of a face to query.</param>
    /// <returns>An array of vertex indices.</returns>
    public int[] IndicesFromFace(int faceIndex)
    {
      int[] rc;
      int a = 0, b = 0, c = 0, d = 0;
      IntPtr const_ptr_mesh = m_mesh.ConstPointer();
      if (UnsafeNativeMethods.ON_MeshTopology_GetTopFaceVertices(const_ptr_mesh, faceIndex, ref a, ref b, ref c, ref d))
      {
        rc = c == d ? new int[] { a, b, c } : new int[] { a, b, c, d };
      }
      else
        rc = new int[0];
      return rc;
    }

    /// <summary>
    /// Gets all topological vertices that are connected to a given vertex.
    /// </summary>
    /// <param name="topologyVertexIndex">index of a topology vertex in Mesh.TopologyVertices.</param>
    /// <returns>
    /// Indices of all topological vertices that are connected to this topological vertex.
    /// null if no vertices are connected to this vertex.
    /// </returns>
    public int[] ConnectedTopologyVertices(int topologyVertexIndex)
    {
      IntPtr ptr = m_mesh.ConstPointer();
      int count = UnsafeNativeMethods.ON_MeshTopologyVertex_Count(ptr, topologyVertexIndex, false);
      if (-1 == count)
        throw new IndexOutOfRangeException();
      if (count < 1)
        return null;
      var rc = new int[count];
      UnsafeNativeMethods.ON_MeshTopologyVertex_ConnectedVertices(ptr, topologyVertexIndex, count, rc);
      return rc;
    }

    /// <summary>
    /// Gets all topological vertices that are connected to a given vertex.
    /// </summary>
    /// <param name="topologyVertexIndex">index of a topology vertex in Mesh.TopologyVertices.</param>
    /// <param name="sorted">if true, thr vertices are returned in a radially sorted order.</param>
    /// <returns>
    /// Indices of all topological vertices that are connected to this topological vertex.
    /// null if no vertices are connected to this vertex.
    /// </returns>
    public int[] ConnectedTopologyVertices(int topologyVertexIndex, bool sorted)
    {
      if (sorted)
        SortEdges(topologyVertexIndex);
      return ConnectedTopologyVertices(topologyVertexIndex);
    }

    /// <summary>
    /// Sorts the edge list for the mesh topology vertex list so that
    /// the edges are in radial order when you call ConnectedTopologyVertices.
    /// A nonmanifold edge is treated as a boundary edge with respect
    /// to sorting.  If any boundary or nonmanifold edges end at the
    /// vertex, then the first edge will be a boundary or nonmanifold edge.
    /// </summary>
    /// <returns>true on success.</returns>
    public bool SortEdges()
    {
      IntPtr const_ptr_mesh = m_mesh.ConstPointer();
      return UnsafeNativeMethods.ON_MeshTopologyVertex_SortEdges(const_ptr_mesh, -1);
    }

    /// <summary>
    /// Sorts the edge list for as single mesh topology vertex so that
    /// the edges are in radial order when you call ConnectedTopologyVertices.
    /// A nonmanifold edge is treated as a boundary edge with respect
    /// to sorting.  If any boundary or nonmanifold edges end at the
    /// vertex, then the first edge will be a boundary or nonmanifold edge.
    /// </summary>
    /// <param name="topologyVertexIndex">index of a topology vertex in Mesh.TopologyVertices></param>
    /// <returns>true on success.</returns>
    public bool SortEdges(int topologyVertexIndex)
    {
      IntPtr const_ptr_mesh = m_mesh.ConstPointer();
      return UnsafeNativeMethods.ON_MeshTopologyVertex_SortEdges(const_ptr_mesh, topologyVertexIndex);
    }

    /// <summary>
    /// Returns true if the topological vertex is hidden. The mesh topology
    /// vertex is hidden if and only if all the ON_Mesh vertices it represents is hidden.
    /// </summary>
    /// <param name="topologyVertexIndex">index of a topology vertex in Mesh.TopologyVertices.</param>
    /// <returns>true if mesh topology vertex is hidden.</returns>
    public bool IsHidden(int topologyVertexIndex)
    {
      return m_mesh.TopItemIsHidden(UnsafeNativeMethods.MeshTopologyHiddenConst.TopVertexIsHidden, topologyVertexIndex);
    }

    /// <summary>
    /// Gets all faces that are connected to a given vertex.
    /// </summary>
    /// <param name="topologyVertexIndex">Index of a topology vertex in Mesh.TopologyVertices.</param>
    /// <returns>
    /// Indices of all faces in Mesh.Faces that are connected to this topological vertex.
    /// null if no faces are connected to this vertex.
    /// </returns>
    public int[] ConnectedFaces(int topologyVertexIndex)
    {
      IntPtr ptr = m_mesh.ConstPointer();
      if (topologyVertexIndex < 0 || topologyVertexIndex >= Count)
        throw new IndexOutOfRangeException();
      using (var arr = new SimpleArrayInt())
      {
        UnsafeNativeMethods.ON_MeshTopologyVertex_ConnectedFaces(ptr, topologyVertexIndex, arr.m_ptr);
        return arr.ToArray();
      }
    }

    /// <summary>
    /// Gets the count of edges that are connected to a given vertex.
    /// </summary>
    /// <param name="topologyVertexIndex">Index of a topology vertex in Mesh.TopologyVertices.</param>
    /// <returns>The amount of edges at this vertex. This can be 0.</returns>
    public int ConnectedEdgesCount(int topologyVertexIndex)
    {
      IntPtr ptr = m_mesh.ConstPointer();
      int count = UnsafeNativeMethods.ON_MeshTopologyVertex_Count(ptr, topologyVertexIndex, false);
      if (count == -1) throw new ArgumentOutOfRangeException("topologyVertexIndex");
      return count;
    }

    /// <summary>
    /// Gets a particular edge that is connected to a topological vertex.
    /// <para>Call TopologyVertices.SortVertices before this if you are interested in ordered edges.</para>
    /// </summary>
    /// <param name="topologyVertexIndex">Index of a topology vertex in Mesh.TopologyVertices.</param>
    /// <param name="edgeAtVertexIndex">Index of the edge at the vertex.</param>
    /// <returns>
    /// The index of the connected edge.
    /// </returns>
    public int ConnectedEdge(int topologyVertexIndex, int edgeAtVertexIndex)
    {
      IntPtr ptr = m_mesh.ConstPointer();
      int rc = UnsafeNativeMethods.ON_MeshTopologyVertex_ConnectedEdge(ptr, topologyVertexIndex, edgeAtVertexIndex);
      if (rc == -1)
      {
        if (topologyVertexIndex >= Count)
          throw new ArgumentOutOfRangeException("topologyVertexIndex");
        else
          throw new ArgumentOutOfRangeException("edgeAtVertexIndex");
      }
      return rc;
    }

    /// <summary>
    /// Gets all edges that are connected to a given vertex.
    /// <para>Call TopologyVertices.SortVertices before this if you are interested in ordered edges.</para>
    /// </summary>
    /// <param name="topologyVertexIndex">Index of a topology vertex in Mesh.TopologyVertices.</param>
    /// <returns>
    /// Indices of all edges around vertex that are connected to this topological vertex.
    /// null if no faces are connected to this vertex.
    /// </returns>
    public int[] ConnectedEdges(int topologyVertexIndex)
    {
      IntPtr ptr = m_mesh.ConstPointer();
      int count = UnsafeNativeMethods.ON_MeshTopologyVertex_Count(ptr, topologyVertexIndex, false);
      if (-1 == count)
        throw new IndexOutOfRangeException();
      if (count < 1)
        return null;
      var rc = new int[count];
      UnsafeNativeMethods.ON_MeshTopologyVertex_ConnectedEdges(ptr, topologyVertexIndex, count, rc);
      return rc;
    }

    #endregion

    #region IList<Point3f>, IList and related implementations

    int IList<Point3f>.IndexOf(Point3f item)
    {
      return GenericIListImplementation.IndexOf(this, item);
    }

    void IList<Point3f>.Insert(int index, Point3f item)
    {
      throw GenericIListImplementation.MakeReadOnlyException(this);
    }

    void IList<Point3f>.RemoveAt(int index)
    {
      throw GenericIListImplementation.MakeReadOnlyException(this);
    }

    void ICollection<Point3f>.Add(Point3f item)
    {
      throw GenericIListImplementation.MakeReadOnlyException(this);
    }

    void ICollection<Point3f>.Clear()
    {
      throw GenericIListImplementation.MakeReadOnlyException(this);
    }

    bool ICollection<Point3f>.Contains(Point3f item)
    {
      return GenericIListImplementation.IndexOf(this, item) != -1;
    }

    void ICollection<Point3f>.CopyTo(Point3f[] array, int arrayIndex)
    {
      GenericIListImplementation.CopyTo(this, array, arrayIndex);
    }

    bool ICollection<Point3f>.IsReadOnly
    {
      get { return true; }
    }

    bool ICollection<Point3f>.Remove(Point3f item)
    {
      throw GenericIListImplementation.MakeReadOnlyException(this);
    }

    int IList.Add(object value)
    {
      throw GenericIListImplementation.MakeReadOnlyException(this);
    }

    void IList.Clear()
    {
      throw GenericIListImplementation.MakeReadOnlyException(this);
    }

    bool IList.Contains(object value)
    {
      return GenericIListImplementation.Contains(this, value);
    }

    int IList.IndexOf(object value)
    {
      return GenericIListImplementation.IndexOf(this, value);
    }

    void IList.Insert(int index, object value)
    {
      throw GenericIListImplementation.MakeReadOnlyException(this);
    }

    bool IList.IsFixedSize
    {
      get { return true; }
    }

    bool IList.IsReadOnly
    {
      get { return true; }
    }

    void IList.Remove(object value)
    {
      throw GenericIListImplementation.MakeReadOnlyException(this);
    }

    void IList.RemoveAt(int index)
    {
      throw GenericIListImplementation.MakeReadOnlyException(this);
    }

    object IList.this[int index]
    {
      get
      {
        return this[index];
      }
      set
      {
        throw GenericIListImplementation.MakeReadOnlyException(this);
      }
    }

    void ICollection.CopyTo(Array array, int index)
    {
      GenericIListImplementation.CopyTo(this, array, index);
    }

    bool ICollection.IsSynchronized
    {
      get { return false; }
    }

    object ICollection.SyncRoot
    {
      get { throw GenericIListImplementation.MakeNoSyncException(this); }
    }

    /// <summary>
    /// Gets an enumerator that yields all topology vertices in this collection.
    /// </summary>
    /// <returns>The enumerator.</returns>
    public IEnumerator<Point3f> GetEnumerator()
    {
      var count = Count;
      for (int i = 0; i < count; i++)
        yield return this[i];
    }
    IEnumerator IEnumerable.GetEnumerator()
    {
      return GetEnumerator();
    }
    #endregion
  }

  /// <summary>
  /// Represents an entry point to the list of edges in a mesh topology.
  /// </summary>
  public class MeshTopologyEdgeList
  {
    private readonly Mesh m_mesh;

    #region constructors
    internal MeshTopologyEdgeList(Mesh ownerMesh)
    {
      m_mesh = ownerMesh;
    }
    #endregion

    #region properties
    /// <summary>
    /// Gets the amount of edges in this list.
    /// </summary>
    public int Count
    {
      get
      {
        IntPtr ptr = m_mesh.ConstPointer();
        return UnsafeNativeMethods.ON_Mesh_GetInt(ptr, UnsafeNativeMethods.MeshIntConst.MeshTopologyEdgeCount);
      }
    }
    #endregion

    /// <summary>Gets the two topology vertices for a given topology edge.</summary>
    /// <param name="topologyEdgeIndex">An index of a topology edge.</param>
    /// <returns>The pair of vertex indices the edge connects.</returns>
    public IndexPair GetTopologyVertices(int topologyEdgeIndex)
    {
      int i = -1, j = -1;
      IntPtr const_ptr_mesh = m_mesh.ConstPointer();
      UnsafeNativeMethods.ON_MeshTopologyEdge_TopVi(const_ptr_mesh, topologyEdgeIndex, ref i, ref j);
      return new IndexPair(i, j);
    }

    /// <summary>
    /// Gets indices of faces connected to an edge.
    /// </summary>
    /// <param name="topologyEdgeIndex">An index of a topology edge that is queried.</param>
    /// <returns>An array of face indices the edge borders. This might be empty on error.</returns>
    public int[] GetConnectedFaces(int topologyEdgeIndex)
    {
      IntPtr const_ptr_mesh = m_mesh.ConstPointer();
      int count = UnsafeNativeMethods.ON_MeshTopologyEdge_TopfCount(const_ptr_mesh, topologyEdgeIndex);
      if (count <= 0)
        return new int[0];
      var rc = new int[count];
      UnsafeNativeMethods.ON_MeshTopologyEdge_TopfList(const_ptr_mesh, topologyEdgeIndex, count, rc);
      return rc;
    }

    /// <summary>
    /// Gets indices of faces connected to an edge.
    /// </summary>
    /// <param name="topologyEdgeIndex">An index of a topology edge that is queried.</param>
    /// <param name="faceOrientationMatchesEdgeDirection">An array of Boolean values that explains whether each face direction matches the direction of the specified edge.</param>
    /// <returns>An array of face indices the edge borders. This might be empty on error.</returns>
    public int[] GetConnectedFaces(int topologyEdgeIndex, out bool[] faceOrientationMatchesEdgeDirection)
    {
      IntPtr const_ptr_mesh = m_mesh.ConstPointer();
      int count = UnsafeNativeMethods.ON_MeshTopologyEdge_TopfCount(const_ptr_mesh, topologyEdgeIndex);
      if (count <= 0)
      {
        faceOrientationMatchesEdgeDirection = new bool[0];
        return new int[0];
      }
      var rc = new int[count];
      faceOrientationMatchesEdgeDirection = new bool[count];
      UnsafeNativeMethods.ON_MeshTopologyEdge_TopfList2(const_ptr_mesh, topologyEdgeIndex, count, rc, faceOrientationMatchesEdgeDirection);
      return rc;
    }

    /// <summary>
    /// Gets indices of edges that surround a given face.
    /// </summary>
    /// <param name="faceIndex">A face index.</param>
    /// <returns>A new array of indices to the topological edges that are connected with the specified face.</returns>
    public int[] GetEdgesForFace(int faceIndex)
    {
      IntPtr const_ptr_mesh = m_mesh.ConstPointer();
      int a = 0, b = 0, c = 0, d = 0;
      UnsafeNativeMethods.ON_MeshTopologyFace_Edges(const_ptr_mesh, faceIndex, ref a, ref b, ref c, ref d);

      if (a < 0 || b < 0 || c < 0 || d < 0)
      {
        if (faceIndex < 0 || faceIndex >= m_mesh.Faces.Count)
          throw new IndexOutOfRangeException();
        return new int[0];
      }

      if (c == d)
        return new int[] { a, b, c };
      return new int[] { a, b, c, d };
    }

    /// <summary>
    /// Gets indices of edges that surround a given face.
    /// </summary>
    /// <param name="faceIndex">A face index.</param>
    /// <param name="sameOrientation">
    /// Same length as returned edge index array. For each edge, the sameOrientation value
    /// tells you if the edge orientation matches the face orientation (true), or is
    /// reversed (false) compared to it.
    /// </param>
    /// <returns>A new array of indices to the topological edges that are connected with the specified face.</returns>
    public int[] GetEdgesForFace(int faceIndex, out bool[] sameOrientation)
    {
      sameOrientation = new bool[0];
      IntPtr const_ptr_mesh = m_mesh.ConstPointer();
      int a = 0, b = 0, c = 0, d = 0;
      var orientation = new int[4];
      if (!UnsafeNativeMethods.ON_MeshTopologyFace_Edges2(const_ptr_mesh, faceIndex, ref a, ref b, ref c, ref d, orientation))
      {
        if (faceIndex < 0 || faceIndex >= m_mesh.Faces.Count)
          throw new IndexOutOfRangeException();
        return new int[0];
      }

      if (c == d)
      {
        sameOrientation = new bool[] { orientation[0] == 1, orientation[1] == 1, orientation[2] == 1 };
        return new int[] { a, b, c };
      }
      sameOrientation = new bool[] { orientation[0] == 1, orientation[1] == 1, orientation[2] == 1, orientation[3] == 1 };
      return new int[] { a, b, c, d };
    }

    /// <summary>
    /// Returns index of edge that connects topological vertices. 
    /// returns -1 if no edge is found.
    /// </summary>
    /// <param name="topologyVertex1">The first topology vertex index.</param>
    /// <param name="topologyVertex2">The second topology vertex index.</param>
    /// <returns>The edge index.</returns>
    public int GetEdgeIndex(int topologyVertex1, int topologyVertex2)
    {
      IntPtr const_ptr_mesh = m_mesh.ConstPointer();
      return UnsafeNativeMethods.ON_MeshTopology_TopEdge(const_ptr_mesh, topologyVertex1, topologyVertex2);
    }

    /// <summary>Gets the 3d line along an edge.</summary>
    /// <param name="topologyEdgeIndex">The topology edge index.</param>
    /// <returns>
    /// Line along edge. If input is not valid, an Invalid Line is returned.
    /// </returns>
    public Line EdgeLine(int topologyEdgeIndex)
    {
      var rc = new Line();
      IntPtr const_ptr_mesh = m_mesh.ConstPointer();
      UnsafeNativeMethods.ON_MeshTopology_TopEdgeLine(const_ptr_mesh, topologyEdgeIndex, ref rc);
      return rc;
    }

    /// <summary>
    /// Replaces a mesh edge with a vertex at its center and update adjacent faces as needed.
    /// </summary>
    /// <param name="topologyEdgeIndex">An index of a topology edge in <see cref="Mesh.TopologyEdges"/>.</param>
    /// <returns>true if successful.</returns>
    public bool CollapseEdge(int topologyEdgeIndex)
    {
      return m_mesh.IndexOpBool(UnsafeNativeMethods.MeshIndexOpBoolConst.CollapseEdge, topologyEdgeIndex);
    }

#if RHINO_SDK
    /// <summary>
    /// Divides a mesh edge to create two or more triangles
    /// </summary>
    /// <param name="topologyEdgeIndex">Edge to divide</param>
    /// <param name="t">
    /// Parameter along edge. This is the same as getting an EdgeLine and calling PointAt(t) on that line
    /// </param>
    /// <returns>true if successful</returns>
    public bool SplitEdge(int topologyEdgeIndex, double t)
    {
      var line = EdgeLine(topologyEdgeIndex);
      if (!line.IsValid)
        return false;
      var point = line.PointAt(t);
      return SplitEdge(topologyEdgeIndex, point);
    }

    /// <summary>
    /// Divides a mesh edge to create two or more triangles
    /// </summary>
    /// <param name="topologyEdgeIndex">Edge to divide</param>
    /// <param name="point">
    /// Location to perform the split
    /// </param>
    /// <returns>true if successful</returns>
    public bool SplitEdge(int topologyEdgeIndex, Point3d point)
    {
      IntPtr ptr_mesh = m_mesh.NonConstPointer();
      return UnsafeNativeMethods.ON_Mesh_SplitMeshEdge(ptr_mesh, topologyEdgeIndex, point);
    }

    /// <summary>
    /// Determines if the mesh edge is unwelded, or if the mesh faces that share the edge have unique vertex indices.
    /// </summary>
    /// <param name="topologyEdgeIndex">An index of a topology edge in <see cref="Mesh.TopologyEdges"/>.</param>
    /// <returns>true if the edge is unwelded, false if the edge is welded.</returns>
    public bool IsEdgeUnwelded(int topologyEdgeIndex)
    {
      IntPtr const_ptr_mesh = m_mesh.ConstPointer();
      return UnsafeNativeMethods.TLC_IsMeshEdgeUnwelded(const_ptr_mesh, topologyEdgeIndex);
    }
#endif

    /// <summary>
    /// Determines if a mesh edge index is valid input for <see cref="SwapEdge"/>.
    /// </summary>
    /// <param name="topologyEdgeIndex">An index of a topology edge in <see cref="Mesh.TopologyEdges"/>.</param>
    /// <returns>true if edge can be swapped.</returns>
    public bool IsSwappableEdge(int topologyEdgeIndex)
    {
      return m_mesh.IndexOpBool(UnsafeNativeMethods.MeshIndexOpBoolConst.IsSwappableEdge, topologyEdgeIndex);
    }

    /// <summary>
    /// If the edge is shared by two triangular face, then the edge is swapped.
    /// </summary>
    /// <param name="topologyEdgeIndex">An index of a topology edge in <see cref="Mesh.TopologyEdges"/>.</param>
    /// <returns>true if successful.</returns>
    public bool SwapEdge(int topologyEdgeIndex)
    {
      return m_mesh.IndexOpBool(UnsafeNativeMethods.MeshIndexOpBoolConst.SwapEdge, topologyEdgeIndex);
    }

    /// <summary>
    /// Returns true if the topological edge is hidden. The mesh topology
    /// edge is hidden only if either of its mesh topology vertices is hidden.
    /// </summary>
    /// <param name="topologyEdgeIndex">An index of a topology edge in <see cref="Mesh.TopologyEdges"/>.</param>
    /// <returns>true if mesh topology edge is hidden.</returns>
    public bool IsHidden(int topologyEdgeIndex)
    {
      return m_mesh.TopItemIsHidden(UnsafeNativeMethods.MeshTopologyHiddenConst.TopEdgeIsHidden, topologyEdgeIndex);
    }

#if RHINO_SDK
    /// <summary>
    /// Returns true if the topological edge is an interior ngon edge
    /// </summary>
    /// <param name="topologyEdgeIndex">An index of a topology edge in <see cref="Mesh.TopologyEdges"/>.</param>
    /// <returns>true if mesh topology edge is an interior ngon edge.</returns>
    public bool IsNgonInterior(int topologyEdgeIndex)
    {
      int count = Count;
      if (m_mesh.m_internal_ngon_edges == null || m_mesh.m_internal_ngon_edges.Length != count)
      {
        IntPtr const_ptr_this = m_mesh.ConstPointer();
        m_mesh.m_internal_ngon_edges = new int[Count];
        UnsafeNativeMethods.ON_Mesh_GetEdgeList(const_ptr_this, m_mesh.m_internal_ngon_edges, m_mesh.m_internal_ngon_edges.Length);
      }
      return m_mesh.m_internal_ngon_edges[topologyEdgeIndex] == 1;
    }
#endif
  }

  /// <summary>
  /// Provides access to the Vertex Normals of a Mesh.
  /// </summary>
  public class MeshVertexNormalList : IResizableList<Vector3f>, IList, IReadOnlyList<Vector3f>
  {
    private readonly Mesh m_mesh;

    #region constructors
    internal MeshVertexNormalList(Mesh ownerMesh)
    {
      m_mesh = ownerMesh;
    }
    #endregion

    #region properties
    /// <summary>
    /// Gets or sets the number of mesh vertex normals.
    /// </summary>
    public int Count
    {
      get
      {
        IntPtr const_ptr_mesh = m_mesh.ConstPointer();
        return UnsafeNativeMethods.ON_Mesh_GetInt(const_ptr_mesh, UnsafeNativeMethods.MeshIntConst.NormalCount);
      }
      set
      {
        if (value >= 0 && Count != value)
        {
          IntPtr ptr_mesh = m_mesh.NonConstPointer();
          UnsafeNativeMethods.ON_Mesh_SetInt(ptr_mesh, UnsafeNativeMethods.MeshIntConst.NormalCount, value);
        }
      }
    }

    /// <summary>
    /// Gets or sets the amount of vertex normals that the list can hold without resizing.
    /// </summary>
    public int Capacity
    {
      get
      {
        IntPtr const_ptr_mesh = m_mesh.ConstPointer();
        return UnsafeNativeMethods.ON_Mesh_GetInt(const_ptr_mesh, UnsafeNativeMethods.MeshIntConst.NormalCapacity);
      }
      set
      {
        if (Capacity != value)
        {
          if (value < Count)
            throw new ArgumentOutOfRangeException("value", "Capacity must be larger than or equal to the list Count");

          IntPtr ptr_mesh = m_mesh.NonConstPointer();
          UnsafeNativeMethods.ON_Mesh_SetInt(ptr_mesh, UnsafeNativeMethods.MeshIntConst.NormalCapacity, value);
        }
      }
    }

    /// <summary>
    /// Gets or sets the vertex at the given index. 
    /// The index must be valid or an IndexOutOfRangeException will be thrown.
    /// </summary>
    /// <param name="index">Index of control vertex to access.</param>
    /// <exception cref="IndexOutOfRangeException">Thrown when the index is invalid.</exception>
    /// <returns>The control vertex at [index].</returns>
    public Vector3f this[int index]
    {
      get
      {
        if (index < 0 || index >= Count)
          throw new IndexOutOfRangeException();

        var rc = new Vector3f();
        IntPtr const_ptr_mesh = m_mesh.ConstPointer();
        UnsafeNativeMethods.ON_Mesh_GetNormal(const_ptr_mesh, index, ref rc, false);
        return rc;
      }
      set
      {
        if (index < 0 || index >= Count)
        {
          throw new IndexOutOfRangeException();
        }

        IntPtr ptr_mesh = m_mesh.NonConstPointer();
        UnsafeNativeMethods.ON_Mesh_SetNormal(ptr_mesh, index, value, false);
      }
    }
    #endregion

    #region access
    /// <summary>
    /// Clears the vertex normal collection on the mesh.
    /// </summary>
    public void Clear()
    {
      IntPtr ptr_mesh = m_mesh.NonConstPointer();
      UnsafeNativeMethods.ON_Mesh_ClearList(ptr_mesh, UnsafeNativeMethods.MeshClearListConst.ClearNormals);
    }

    /// <summary>
    /// Releases all memory allocated to store vertex normals. The list capacity will be 0 after this call.
    /// <para>Subsequent calls can add new items.</para>
    /// </summary>
    public void Destroy()
    {
      IntPtr ptr_mesh = m_mesh.NonConstPointer();
      UnsafeNativeMethods.ON_Mesh_SetInt(ptr_mesh, UnsafeNativeMethods.MeshIntConst.NormalCapacity, 0);
    }

    private bool SetNormalsHelper(Vector3f[] normals, bool append)
    {
      if (null == normals || normals.Length < 1)
        return false;
      IntPtr ptr_mesh = m_mesh.NonConstPointer();
      return UnsafeNativeMethods.ON_Mesh_SetNormals(ptr_mesh, normals.Length, normals, append);
    }

    /// <summary>
    /// Adds a new vertex normal at the end of the list.
    /// </summary>
    /// <param name="x">X component of new vertex normal.</param>
    /// <param name="y">Y component of new vertex normal.</param>
    /// <param name="z">Z component of new vertex normal.</param>
    /// <returns>The index of the newly added vertex normal.</returns>
    public int Add(float x, float y, float z)
    {
      return Add(new Vector3f(x, y, z));
    }
    /// <summary>
    /// Adds a new vertex normal at the end of the list.
    /// </summary>
    /// <param name="x">X component of new vertex normal.</param>
    /// <param name="y">Y component of new vertex normal.</param>
    /// <param name="z">Z component of new vertex normal.</param>
    /// <returns>The index of the newly added vertex normal.</returns>
    public int Add(double x, double y, double z)
    {
      return Add(new Vector3f((float)x, (float)y, (float)z));
    }
    /// <summary>
    /// Adds a new vertex normal at the end of the list.
    /// </summary>
    /// <param name="normal">new vertex normal.</param>
    /// <returns>The index of the newly added vertex normal.</returns>
    public int Add(Vector3f normal)
    {
      int n = Count;
      if (!SetNormal(n, normal))
        return -1;
      return n;
    }
    /// <summary>
    /// Adds a new vertex normal at the end of the list.
    /// </summary>
    /// <param name="normal">new vertex normal.</param>
    /// <returns>The index of the newly added vertex normal.</returns>
    public int Add(Vector3d normal)
    {
      return Add(new Vector3f((float)normal.X, (float)normal.Y, (float)normal.Z));
    }
    /// <summary>
    /// Appends a collection of normal vectors.
    /// </summary>
    /// <param name="normals">Normals to append.</param>
    /// <returns>true on success, false on failure.</returns>
    public bool AddRange(Vector3f[] normals)
    {
      return SetNormalsHelper(normals, true);
    }

    /// <summary>
    /// Sets or adds a normal to the list.
    /// <para>If [index] is less than [Count], the existing vertex normal at [index] will be modified.</para>
    /// <para>If [index] equals [Count], a new vertex normal is appended to the end of the list.</para> 
    /// <para>If [index] is larger than [Count], the function will return false.</para>
    /// </summary>
    /// <param name="index">Index of vertex normal to set.</param>
    /// <param name="x">X component of vertex normal.</param>
    /// <param name="y">Y component of vertex normal.</param>
    /// <param name="z">Z component of vertex normal.</param>
    /// <returns>true on success, false on failure.</returns>
    public bool SetNormal(int index, float x, float y, float z)
    {
      return SetNormal(index, new Vector3f(x, y, z));
    }
    /// <summary>
    /// Sets or adds a vertex normal to the list.
    /// <para>If [index] is less than [Count], the existing vertex normal at [index] will be modified.</para>
    /// <para>If [index] equals [Count], a new vertex normal is appended to the end of the list.</para> 
    /// <para>If [index] is larger than [Count], the function will return false.</para>
    /// </summary>
    /// <param name="index">Index of vertex normal to set.</param>
    /// <param name="x">X component of vertex normal.</param>
    /// <param name="y">Y component of vertex normal.</param>
    /// <param name="z">Z component of vertex normal.</param>
    /// <returns>true on success, false on failure.</returns>
    public bool SetNormal(int index, double x, double y, double z)
    {
      return SetNormal(index, new Vector3f((float)x, (float)y, (float)z));
    }
    /// <summary>
    /// Sets or adds a vertex normal to the list.
    /// <para>If [index] is less than [Count], the existing vertex normal at [index] will be modified.</para>
    /// <para>If [index] equals [Count], a new vertex normal is appended to the end of the vertex list.</para> 
    /// <para>If [index] is larger than [Count], the function will return false.</para>
    /// </summary>
    /// <param name="index">Index of vertex normal to set.</param>
    /// <param name="normal">The new normal at the index.</param>
    /// <returns>true on success, false on failure.</returns>
    public bool SetNormal(int index, Vector3f normal)
    {
      IntPtr ptr = m_mesh.NonConstPointer();
      return UnsafeNativeMethods.ON_Mesh_SetNormal(ptr, index, normal, false);
    }
    /// <summary>
    /// Sets or adds a vertex normal to the list.
    /// <para>If [index] is less than [Count], the existing vertex normal at [index] will be modified.</para>
    /// <para>If [index] equals [Count], a new vertex normal is appended to the end of the list.</para> 
    /// <para>If [index] is larger than [Count], the function will return false.</para>
    /// </summary>
    /// <param name="index">Index of vertex normal to set.</param>
    /// <param name="normal">The new normal at the index.</param>
    /// <returns>true on success, false on failure.</returns>
    public bool SetNormal(int index, Vector3d normal)
    {
      return SetNormal(index, new Vector3f((float)normal.m_x, (float)normal.m_y, (float)normal.m_z));
    }
    /// <summary>
    /// Sets all normal vectors in one go. This method destroys the current normal array if it exists.
    /// </summary>
    /// <param name="normals">Normals for the entire mesh.</param>
    /// <returns>true on success, false on failure.</returns>
    public bool SetNormals(Vector3f[] normals)
    {
      return SetNormalsHelper(normals, false);
    }
    #endregion

    #region methods
    /// <summary>
    /// Copies all vertex normals to a linear array of float in x,y,z order
    /// </summary>
    /// <returns>The float array.</returns>
    public float[] ToFloatArray()
    {
      var count = Count;
      var rc = new float[count * 3];
      var const_ptr_mesh = m_mesh.ConstPointer();
      var index = 0;
      var vec = new Vector3f();
      for (var i = 0; i < count; i++)
      {
        UnsafeNativeMethods.ON_Mesh_GetNormal(const_ptr_mesh, i, ref vec, false);
        rc[index++] = vec.X;
        rc[index++] = vec.Y;
        rc[index++] = vec.Z;
      }
      return rc;
    }
    /// <summary>
    /// Computes the vertex normals based on the physical shape of the mesh.
    /// </summary>
    /// <returns>true on success, false on failure.</returns>
    /// <example>
    /// <code source='examples\vbnet\ex_addmesh.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_addmesh.cs' lang='cs'/>
    /// <code source='examples\py\ex_addmesh.py' lang='py'/>
    /// </example>
    public bool ComputeNormals()
    {
      IntPtr ptr = m_mesh.NonConstPointer();
      return UnsafeNativeMethods.ON_Mesh_NonConstBoolOp(ptr, UnsafeNativeMethods.MeshNonConstBoolConst.ComputeVertexNormals);
    }

    /// <summary>
    /// Unitizes all vertex normals.
    /// </summary>
    /// <returns>true on success, false on failure.</returns>
    public bool UnitizeNormals()
    {
      IntPtr ptr = m_mesh.NonConstPointer();
      return UnsafeNativeMethods.ON_Mesh_NonConstBoolOp(ptr, UnsafeNativeMethods.MeshNonConstBoolConst.UnitizeVertexNormals);
    }

    /// <summary>
    /// Reverses direction of all vertex normals
    /// <para>This is the same as Mesh.Flip(true, false, false)</para>
    /// </summary>
    public void Flip()
    {
      m_mesh.Flip(true, false, false);
    }
    #endregion

    #region IResizableList<Point3f>, IList and related implementations
    int IList<Vector3f>.IndexOf(Vector3f item)
    {
      return GenericIListImplementation.IndexOf(this, item);
    }

    void IList<Vector3f>.Insert(int index, Vector3f item)
    {
      GenericIListImplementation.Insert(this, index, item);
    }

    void IList<Vector3f>.RemoveAt(int index)
    {
      GenericIListImplementation.RemoveAt(this, index);
    }

    void ICollection<Vector3f>.Add(Vector3f item)
    {
      Add(item);
    }

    bool ICollection<Vector3f>.Contains(Vector3f item)
    {
      return GenericIListImplementation.IndexOf(this, item) != -1;
    }

    void ICollection<Vector3f>.CopyTo(Vector3f[] array, int arrayIndex)
    {
      GenericIListImplementation.CopyTo(this, array, arrayIndex);
    }

    bool ICollection<Vector3f>.IsReadOnly
    {
      get { return false; }
    }

    bool ICollection<Vector3f>.Remove(Vector3f item)
    {
      return GenericIListImplementation.Remove(this, item);
    }

    int IList.Add(object value)
    {
      var item = GenericIListImplementation.HelperCoerceVector(value);
      return Add(item);
    }

    bool IList.Contains(object value)
    {
      return GenericIListImplementation.Contains(this, value);
    }

    int IList.IndexOf(object value)
    {
      return GenericIListImplementation.IndexOf(this, value);
    }

    void IList.Insert(int index, object value)
    {
      var item = GenericIListImplementation.HelperCoerceVector(value);
      GenericIListImplementation.Insert(this, index, item);
    }

    bool IList.IsFixedSize
    {
      get { return false; }
    }

    bool IList.IsReadOnly
    {
      get { return false; }
    }

    void IList.Remove(object value)
    {
      var item = GenericIListImplementation.HelperCoerceVector(value);
      GenericIListImplementation.Remove(this, item);
    }

    void IList.RemoveAt(int index)
    {
      GenericIListImplementation.RemoveAt(this, index);
    }

    object IList.this[int index]
    {
      get
      {
        return this[index];
      }
      set
      {
        this[index] = GenericIListImplementation.HelperCoerceVector(value);
      }
    }

    void ICollection.CopyTo(Array array, int index)
    {
      GenericIListImplementation.CopyTo(this, array, index);
    }

    bool ICollection.IsSynchronized
    {
      get { return false; }
    }

    object ICollection.SyncRoot
    {
      get
      {
        throw GenericIListImplementation.MakeNoSyncException(this);
      }
    }

    /// <summary>
    /// Gets an enumerator that yields all normals (vectors) in this collection.
    /// </summary>
    /// <returns>The enumerator.</returns>
    public IEnumerator<Vector3f> GetEnumerator()
    {
      var count = Count;
      for (int i = 0; i < count; i++)
        yield return this[i];
    }
    IEnumerator IEnumerable.GetEnumerator()
    {
      return GetEnumerator();
    }
    #endregion
  }

  /// <summary>
  /// Provides access to the faces and Face related functionality of a Mesh.
  /// </summary>
  public class MeshFaceList : IResizableList<MeshFace>, IList, IReadOnlyList<MeshFace>
  {
    private readonly Mesh m_mesh;

    #region constructors
    internal MeshFaceList(Mesh ownerMesh)
    {
      m_mesh = ownerMesh;
    }
    #endregion

    #region properties
    /// <summary>
    /// Gets or sets the number of mesh faces. When getting this can includes invalid faces.
    /// </summary>
    public int Count
    {
      get
      {
        IntPtr const_ptr_mesh = m_mesh.ConstPointer();
        return UnsafeNativeMethods.ON_Mesh_GetInt(const_ptr_mesh, UnsafeNativeMethods.MeshIntConst.FaceCount);
      }
      set
      {
        if (value >= 0 && Count != value)
        {
          IntPtr ptr_mesh = m_mesh.NonConstPointer();
          UnsafeNativeMethods.ON_Mesh_SetInt(ptr_mesh, UnsafeNativeMethods.MeshIntConst.FaceCount, value);
        }
      }
    }

    /// <summary>
    /// Gets the number of faces that are valid quads (4 corners).
    /// </summary>
    public int QuadCount
    {
      get
      {
        IntPtr const_ptr_mesh = m_mesh.ConstPointer();
        return UnsafeNativeMethods.ON_Mesh_GetInt(const_ptr_mesh, UnsafeNativeMethods.MeshIntConst.QuadCount);
      }
    }

    /// <summary>
    /// Gets the number of faces that are valid triangles (3 corners).
    /// </summary>
    public int TriangleCount
    {
      get
      {
        IntPtr const_ptr_mesh = m_mesh.ConstPointer();
        return UnsafeNativeMethods.ON_Mesh_GetInt(const_ptr_mesh, UnsafeNativeMethods.MeshIntConst.TriangleCount);
      }
    }

    /// <summary>
    /// Gets the number of vertices in the mesh of this face list
    /// </summary>
    private int VertexCount
    {
      get
      {
        IntPtr const_ptr_mesh = m_mesh.ConstPointer();
        return UnsafeNativeMethods.ON_Mesh_GetInt(const_ptr_mesh, UnsafeNativeMethods.MeshIntConst.VertexCount);
      }
    }

    // 7 Mar 2010 S. Baer - skipping indexing operator for now. I'm a little concerned that this
    // would cause code that looks like
    // int v0 = mesh.Faces[0].A;
    // int v1 = mesh.Faces[0].B;
    // int v2 = mesh.Faces[0].C;
    // int v3 = mesh.Faces[0].D;
    // The above code would always be 4 times as slow as a single call to get all 4 indices at once
    //public MeshFace this[int index]


    /// <summary>
    /// Gets or sets the total number of mesh triangles and quads the internal data structure can hold without resizing.
    /// </summary>
    public int Capacity
    {
      get
      {
        IntPtr const_ptr_mesh = m_mesh.ConstPointer();
        return UnsafeNativeMethods.ON_Mesh_GetInt(const_ptr_mesh, UnsafeNativeMethods.MeshIntConst.FaceCapacity);
      }
      set
      {
        if (Capacity != value)
        {
          if (value < Count)
            throw new ArgumentOutOfRangeException("value", "Capacity must be larger than or equal to the list Count");

          IntPtr ptr_mesh = m_mesh.NonConstPointer();
          UnsafeNativeMethods.ON_Mesh_SetInt(ptr_mesh, UnsafeNativeMethods.MeshIntConst.FaceCapacity, value);
        }
      }
    }
    #endregion

    #region methods
    #region face access
    /// <summary>
    /// Clears the Face list on the mesh.
    /// </summary>
    public void Clear()
    {
      IntPtr ptr_mesh = m_mesh.NonConstPointer();
      UnsafeNativeMethods.ON_Mesh_ClearList(ptr_mesh, UnsafeNativeMethods.MeshClearListConst.ClearFaces);
    }

    /// <summary>
    /// Releases all memory allocated to store faces. The list capacity will be 0 after this call.
    /// <para>Subsequent calls can add new items.</para>
    /// </summary>
    public void Destroy()
    {
      IntPtr ptr_mesh = m_mesh.NonConstPointer();
      UnsafeNativeMethods.ON_Mesh_SetInt(ptr_mesh, UnsafeNativeMethods.MeshIntConst.FaceCapacity, 0);
    }

    /// <summary>
    /// Appends a new mesh face to the end of the mesh face list.
    /// </summary>
    /// <param name="face">Face to add.</param>
    /// <returns>The index of the newly added face.</returns>
    public int AddFace(MeshFace face)
    {
      return AddFace(face.m_a, face.m_b, face.m_c, face.m_d);
    }
    /// <summary>
    /// Appends a new triangular face to the end of the mesh face list.
    /// </summary>
    /// <param name="vertex1">Index of first face corner.</param>
    /// <param name="vertex2">Index of second face corner.</param>
    /// <param name="vertex3">Index of third face corner.</param>
    /// <returns>The index of the newly added triangle.</returns>
    public int AddFace(int vertex1, int vertex2, int vertex3)
    {
      return AddFace(vertex1, vertex2, vertex3, vertex3);
    }
    /// <summary>
    /// Appends a new quadragular face to the end of the mesh face list.
    /// </summary>
    /// <param name="vertex1">Index of first face corner.</param>
    /// <param name="vertex2">Index of second face corner.</param>
    /// <param name="vertex3">Index of third face corner.</param>
    /// <param name="vertex4">Index of fourth face corner.</param>
    /// <returns>The index of the newly added quad.</returns>
    /// <example>
    /// <code source='examples\vbnet\ex_addmesh.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_addmesh.cs' lang='cs'/>
    /// <code source='examples\py\ex_addmesh.py' lang='py'/>
    /// </example>
    public int AddFace(int vertex1, int vertex2, int vertex3, int vertex4)
    {
      IntPtr ptr = m_mesh.NonConstPointer();
      return UnsafeNativeMethods.ON_Mesh_AddFace(ptr, vertex1, vertex2, vertex3, vertex4);
    }

    /// <summary>
    /// Appends a list of faces to the end of the mesh face list.
    /// </summary>
    /// <param name="faces">Faces to add.</param>
    /// <returns>Indices of the newly created faces</returns>
    public int[] AddFaces(IEnumerable<MeshFace> faces)
    {
      var rc = new List<int>();
      foreach (var face in faces)
      {
        int index = AddFace(face);
        rc.Add(index);
      }
      return rc.ToArray();
    }

    /// <summary>
    /// Inserts a mesh face at a defined index in this list.
    /// </summary>
    /// <param name="index">An index.</param>
    /// <param name="face">A face.</param>
    /// <exception cref="ArgumentOutOfRangeException">If index is negative or &gt;= Count.</exception>
    public void Insert(int index, MeshFace face)
    {
      IntPtr ptr_mesh = m_mesh.NonConstPointer();
      bool rc = UnsafeNativeMethods.ON_Mesh_InsertFace(ptr_mesh, index, face.A, face.B, face.C, face.D);
      if (!rc && (index < 0 || index >= Count))
        throw new ArgumentOutOfRangeException("index");
    }

    /// <summary>
    /// Sets a face at a specific index of the mesh.
    /// </summary>
    /// <param name="index">A position in the list.</param>
    /// <param name="face">A face.</param>
    /// <returns>true if the operation succeeded, otherwise false.</returns>
    public bool SetFace(int index, MeshFace face)
    {
      return SetFace(index, face.m_a, face.m_b, face.m_c, face.m_d);
    }
    /// <summary>
    /// Sets a triangular face at a specific index of the mesh.
    /// </summary>
    /// <param name="index">A position in the list.</param>
    /// <param name="vertex1">The first vertex index.</param>
    /// <param name="vertex2">The second vertex index.</param>
    /// <param name="vertex3">The third vertex index.</param>
    /// <returns>true if the operation succeeded, otherwise false.</returns>
    public bool SetFace(int index, int vertex1, int vertex2, int vertex3)
    {
      return SetFace(index, vertex1, vertex2, vertex3, vertex3);
    }

    /// <summary>
    /// Sets a quadrangular face at a specific index of the mesh.
    /// </summary>
    /// <param name="index">A position in the list.</param>
    /// <param name="vertex1">The first vertex index.</param>
    /// <param name="vertex2">The second vertex index.</param>
    /// <param name="vertex3">The third vertex index.</param>
    /// <param name="vertex4">The fourth vertex index.</param>
    /// <returns>true if the operation succeeded, otherwise false.</returns>
    public bool SetFace(int index, int vertex1, int vertex2, int vertex3, int vertex4)
    {
      IntPtr ptr_mesh = m_mesh.NonConstPointer();
      return UnsafeNativeMethods.ON_Mesh_SetFace(ptr_mesh, index, vertex1, vertex2, vertex3, vertex4);
    }

    /// <summary>
    /// Returns the mesh face at the given index. 
    /// </summary>
    /// <param name="index">Index of face to get. Must be larger than or equal to zero and 
    /// smaller than the Face Count of the mesh.</param>
    /// <returns>The mesh face at the given index on success or MeshFace.Unset if the index is out of range.</returns>
    public MeshFace GetFace(int index)
    {
      var rc = new MeshFace();
      IntPtr const_ptr_mesh = m_mesh.ConstPointer();
      if (UnsafeNativeMethods.ON_Mesh_GetFace(const_ptr_mesh, index, ref rc))
        return rc;

      return MeshFace.Unset;
    }

#if RHINO_SDK
    /// <summary>
    /// Returns the mesh face at the given index. 
    /// </summary>
    /// <param name="index">Index of face to get. Must be larger than or equal to zero and 
    /// smaller than the Face Count of the mesh.</param>
    /// <returns>The mesh face at the given index on success or MeshFace.Unset if the index is out of range.</returns>
    public double GetFaceAspectRatio(int index)
    {
      MeshFace face = m_mesh.Faces.GetFace(index);
      Point3d a = m_mesh.Vertices[face.A];
      Point3d b = m_mesh.Vertices[face.B];
      Point3d c = m_mesh.Vertices[face.C];
      Point3d d = m_mesh.Vertices[face.D];
      return UnsafeNativeMethods.RHC_RhinoCalculateAspectRatio(ref a, ref b, ref c, ref d);
    }
#endif

    /// <summary>
    /// Returns the mesh face at the given index. 
    /// </summary>
    /// <param name="index">Index of face to get. Must be larger than or equal to zero and 
    /// smaller than the Face Count of the mesh.</param>
    public MeshFace this[int index]
    {
      get
      {
        var face = GetFace(index);
        if (face.A == int.MinValue && (index < 0 || index >= Count))
          throw new ArgumentOutOfRangeException("index");
        return face;
      }
      set
      {
        SetFace(index, value);
      }
    }

    /// <summary>
    /// Gets the 3D location of the vertices forming a face.
    /// </summary>
    /// <param name="faceIndex">A face index.</param>
    /// <param name="a">A first point. This out argument is assigned during the call.</param>
    /// <param name="b">A second point. This out argument is assigned during the call.</param>
    /// <param name="c">A third point. This out argument is assigned during the call.</param>
    /// <param name="d">A fourth point. This out argument is assigned during the call.</param>
    /// <returns>true if the operation succeeded, otherwise false.</returns>
    public bool GetFaceVertices(int faceIndex, out Point3f a, out Point3f b, out Point3f c, out Point3f d)
    {
      IntPtr const_ptr_mesh = m_mesh.ConstPointer();
      a = new Point3f();
      b = new Point3f();
      c = new Point3f();
      d = new Point3f();
      bool rc = UnsafeNativeMethods.ON_Mesh_GetFaceVertices(const_ptr_mesh, faceIndex, ref a, ref b, ref c, ref d);
      return rc;
    }

    /// <summary>
    /// Gets the bounding box of a face.
    /// </summary>
    /// <param name="faceIndex">A face index.</param>
    /// <returns>A new bounding box, or <see cref="BoundingBox.Empty"/> on error.</returns>
    public BoundingBox GetFaceBoundingBox(int faceIndex)
    {
      Point3f a, b, c, d;
      if (!GetFaceVertices(faceIndex, out a, out b, out c, out d))
        return BoundingBox.Empty;
      BoundingBox rc = BoundingBox.Empty;
      rc.Union(a);
      rc.Union(b);
      rc.Union(c);
      rc.Union(d);
      return rc;
    }

    /// <summary>
    /// Gets the center point of a face.
    /// <para>For a triangular face, this is the centroid or barycenter.</para>
    /// <para>For a quad, this is the avarage of four coner points.</para>
    /// </summary>
    /// <param name="faceIndex">A face index.</param>
    /// <returns>The center point.</returns>
    public Point3d GetFaceCenter(int faceIndex)
    {
      IntPtr const_ptr_mesh = m_mesh.ConstPointer();
      var rc = new Point3d();
      if (!UnsafeNativeMethods.ON_Mesh_GetFaceCenter(const_ptr_mesh, faceIndex, ref rc))
        throw new IndexOutOfRangeException();
      return rc;
    }

    /// <summary>
    /// Gets all faces that share a topological edge with a given face.
    /// </summary>
    /// <param name="faceIndex">A face index.</param>
    /// <returns>All indices that share an edge.</returns>
    public int[] AdjacentFaces(int faceIndex)
    {
      int[] edges = m_mesh.TopologyEdges.GetEdgesForFace(faceIndex);
      if (null == edges || edges.Length < 1)
        return new int[0];

      var face_ids = new Dictionary<int, int>();
      for (int i = 0; i < edges.Length; i++)
      {
        int edge_index = edges[i];
        int[] faces = m_mesh.TopologyEdges.GetConnectedFaces(edge_index);
        if (faces == null)
          continue;
        for (int j = 0; j < faces.Length; j++)
        {
          int face_id = faces[j];
          if (face_id != faceIndex)
            face_ids[face_id] = face_id;
        }
      }

      var rc = new int[face_ids.Count];
      face_ids.Keys.CopyTo(rc, 0);
      return rc;
    }

    /// <summary>
    /// Copies all of the face indices to a linear array of indices per face.
    /// 
    /// Note that this includes indices from invalid faces too.
    /// </summary>
    /// <returns>The int array. This method never returns null.</returns>
    /// <param name="asTriangles">If set to <c>true</c> as triangles.</param>
    public int[] ToIntArray(bool asTriangles)
    {
      List<int> rep = null;
      return ToIntArray(asTriangles, ref rep);
    }

    /// <summary>
    /// Copies all of the faces to a linear array of indices.
    /// 
    /// Clean-up of vertex indices if replacedIndices is a valid List&lt;int&gt;
    /// /// </summary>
    /// <returns>The int array. This method never returns null.</returns>
    /// <param name="asTriangles">If set to <c>true</c> as triangles.</param>
    /// <param name="replacedIndices">List is populated with vertex indices that were replaced with 0. If replacedIndices is null there will be no cleanup</param>
    public int[] ToIntArray(bool asTriangles, ref List<int> replacedIndices)
    {
      int count = asTriangles ? (QuadCount * 2 + TriangleCount) * 3 : Count * 4;
      int invalidcount = Count - (QuadCount + TriangleCount) ;

      int invalidQuads = (from iq in this where iq.IsQuad && !iq.IsValid() select iq).Count();
      int invalidTris = (from iq in this where iq.IsTriangle && !iq.IsValid() select iq).Count();

      count += asTriangles ? (invalidQuads * 2 + invalidTris) * 3 : invalidcount * 4;

      bool cleanup = replacedIndices != null;
      HashSet<int> replacements = new HashSet<int>();
      var rc = new int[count];
      int current = 0;
      int face_count = Count;
      int vertex_count = VertexCount;
      var face = new MeshFace();
      IntPtr const_ptr_mesh = m_mesh.ConstPointer();
      for (int index = 0; index < face_count; index++)
      {
        UnsafeNativeMethods.ON_Mesh_GetFace(const_ptr_mesh, index, ref face);
        // also don't use indices that go past the vertex count and we want it cleaned up
        var A = face.A < vertex_count ? face.A : cleanup ? 0 : face.A;
        var B = face.B < vertex_count ? face.B : cleanup ? 0 : face.B;
        var C = face.C < vertex_count ? face.C : cleanup ? 0 : face.C;
        var D = face.D < vertex_count ? face.D : cleanup ? 0 : face.D;

        if(cleanup) {
          if (A != face.A) replacements.Add(face.A);
          if (B != face.B) replacements.Add(face.B);
          if (C != face.C) replacements.Add(face.C);
          if (face.IsQuad && D != face.D) replacements.Add(face.D);
        }
        rc[current++] = A;
        rc[current++] = B;
        rc[current++] = C;
        // no triangle here if we're past the upper bound
        if (asTriangles)
        {
          if (C != D)
          {
            rc[current++] = C;
            rc[current++] = D;
            rc[current++] = A;
          }
        }
        else
        {
          rc[current++] = D;
        }
      }

      if (cleanup) replacedIndices.AddRange(replacements);
      return rc;
    }

    #endregion

    /// <summary>
    /// Removes a collection of faces from the mesh without affecting the remaining geometry.
    /// </summary>
    /// <param name="faceIndexes">An array containing all the face indices to be removed.</param>
    /// <returns>The number of faces deleted on success.</returns>
    public int DeleteFaces(IEnumerable<int> faceIndexes)
    {
      return DeleteFaces(faceIndexes, true);
    }

    /// <summary>
    /// Removes a collection of faces from the mesh without affecting the remaining geometry.
    /// </summary>
    /// <param name="faceIndexes">An array containing all the face indices to be removed.</param>
    /// <param name="compact">No longer used.</param>
    /// <returns>The number of faces deleted on success.</returns>
    public int DeleteFaces(IEnumerable<int> faceIndexes, bool compact)
    {
      if (null == faceIndexes)
        return 0;
      var face_indexes = new RhinoList<int>(faceIndexes);

      if (face_indexes.Count < 1)
        return 0;
      var f = face_indexes.m_items;
      IntPtr ptr = m_mesh.NonConstPointer();
      return UnsafeNativeMethods.ON_Mesh_DeleteFace(ptr, face_indexes.Count, f);
    }


    /// <summary>
    /// Removes a face from the mesh.
    /// </summary>
    /// <param name="index">The index of the face that will be removed.</param>
    /// <exception cref="ArgumentOutOfRangeException">If index is &lt; 0 or &gt;= Count.</exception>
    public void RemoveAt(int index)
    {
      RemoveAt(index, true);
    }

    /// <summary>
    /// Removes a face from the mesh.
    /// </summary>
    /// <param name="index">The index of the face that will be removed.</param>
    /// <param name="compact">No longer used.</param>
    /// <exception cref="ArgumentOutOfRangeException">If index is &lt; 0 or &gt;= Count.</exception>
    public void RemoveAt(int index, bool compact)
    {
      IntPtr ptr_mesh = m_mesh.NonConstPointer();
      int[] indices = { index };
      int count = UnsafeNativeMethods.ON_Mesh_DeleteFace(ptr_mesh, 1, indices);
      if (count != 1 && (index < 0 || index > Count))
        throw new ArgumentOutOfRangeException("index");
    }
    
    /// <summary>Splits all quads along the short diagonal.</summary>
    /// <returns>true on success, false on failure.</returns>
    public bool ConvertQuadsToTriangles()
    {
      IntPtr ptr_mesh = m_mesh.NonConstPointer();
      return UnsafeNativeMethods.ON_Mesh_NonConstBoolOp(ptr_mesh, UnsafeNativeMethods.MeshNonConstBoolConst.ConvertQuadsToTriangles);
    }

    /// <summary>
    /// Splits non-planar quads into two triangles based on given params.
    /// </summary>
    /// <param name="planarTolerance">
    /// If planarTolerance >= 0, then a quad is split if its vertices
    /// are not coplanar.  
    /// If both planarTolerance = Rhino.RhinoMath.UnsetValue and 
    /// angleToleranceRadians >= 0.0, then the planarity test is skipped.
    /// </param>
    /// <param name="angleToleranceRadians">
    /// If angleToleranceRadians >= 0.0, then a quad is split if the
    /// angle between opposite corner normals is > angleToleranceRadians.
    /// The corner normal is the normal to the triangle formed by two
    /// adjacent edges and the diagonal connecting their endpoints.
    /// A quad has four corner normals.
    /// If both angleToleranceRadians = Rhino.RhinoMath.UnsetValue and planarTolerance >= 0.0,
    /// then the corner normal angle test is skipped.
    /// </param>
    /// <param name="splitMethod">
    /// 0 default 
    ///   Currently divides along the short diagonal. This may be
    ///   changed as better methods are found or preferences change.
    ///   By passing zero, you let the developers of this code
    ///   decide what's best for you over time.
    /// 1 divide along the short diagonal
    /// 2 divide along the long diagonal
    /// 3 minimize resulting area
    /// 4 maximize resulting area
    /// 5 minimize angle between triangle normals
    /// 6 maximize angle between triangle normals
    /// </param>
    /// <returns>Number of quads that were converted to triangles.</returns>
    /// <remarks>
    /// If both planarTolerance = Rhino.RhinoMath.UnsetValue and angleToleranceRadians = Rhino.RhinoMath.UnsetValue,
    /// then all quads are split.
    /// </remarks>
    public int ConvertNonPlanarQuadsToTriangles(double planarTolerance, double angleToleranceRadians, int splitMethod)
    {
      IntPtr ptr_mesh = m_mesh.NonConstPointer();
      return (int)UnsafeNativeMethods.ON_Mesh_ConvertNonPlanarQuadsToTriangles(ptr_mesh, planarTolerance, angleToleranceRadians, (uint)splitMethod);
    }

    /// <summary>
    /// Joins adjacent triangles into quads if the resulting quad is 'nice'.
    /// </summary>
    /// <param name="angleToleranceRadians">
    /// Used to compare adjacent triangles' face normals. For two triangles 
    /// to be considered, the angle between their face normals has to 
    /// be &lt;= angleToleranceRadians. When in doubt use RhinoMath.PI/90.0 (2 degrees).
    /// </param>
    /// <param name="minimumDiagonalLengthRatio">
    /// ( &lt;= 1.0) For two triangles to be considered the ratio of the 
    /// resulting quad's diagonals 
    /// (length of the shortest diagonal)/(length of longest diagonal). 
    /// has to be >= minimumDiagonalLengthRatio. When in doubt us .875.
    /// </param>
    /// <returns>true on success, false on failure.</returns>
    public bool ConvertTrianglesToQuads(double angleToleranceRadians, double minimumDiagonalLengthRatio)
    {
      IntPtr ptr_mesh = m_mesh.NonConstPointer();
      return UnsafeNativeMethods.ON_Mesh_ConvertTrianglesToQuads(ptr_mesh, angleToleranceRadians, minimumDiagonalLengthRatio);
    }

    /// <summary>
    /// Attempts to removes degenerate faces from the mesh.
    /// <para>Degenerate faces are faces that contains such a combination of indices,
    /// that their final shape collapsed in a line or point.</para>
    /// <para>Before returning, this method also attempts to repair faces by juggling
    /// vertex indices.</para>
    /// </summary>
    /// <returns>The number of degenerate faces that were removed.</returns>
    public int CullDegenerateFaces()
    {
      IntPtr ptr_mesh = m_mesh.NonConstPointer();
      return UnsafeNativeMethods.ON_Mesh_CullOp(ptr_mesh, true);
    }

    /// <summary>
    /// Gets a value indicating whether a face is hidden.
    /// <para>A face is hidden if, and only if, at least one of its vertices is hidden.</para>
    /// </summary>
    /// <param name="faceIndex">A face index.</param>
    /// <returns>true if hidden, false if fully visible.</returns>
    public bool IsHidden(int faceIndex)
    {
      IntPtr const_ptr_mesh = m_mesh.ConstPointer();
      return UnsafeNativeMethods.ON_Mesh_FaceIsHidden(const_ptr_mesh, faceIndex);
    }

    /// <summary>
    /// Returns true if at least one of the face edges are not topologically
    /// connected to any other faces.
    /// </summary>
    /// <param name="faceIndex">A face index.</param>
    /// <returns>true if that face makes the mesh open, otherwise false.</returns>
    public bool HasNakedEdges(int faceIndex)
    {
      IntPtr const_ptr_mesh = m_mesh.ConstPointer();
      return UnsafeNativeMethods.ON_Mesh_FaceHasNakedEdges(const_ptr_mesh, faceIndex);
    }

    /// <summary>
    /// Gets the topology vertex indices of a face.
    /// </summary>
    /// <param name="faceIndex">A face index.</param>
    /// <returns>An array of integers.</returns>
    public int[] GetTopologicalVertices(int faceIndex)
    {
      IntPtr const_ptr_mesh = m_mesh.ConstPointer();
      var v = new int[4];
      if (!UnsafeNativeMethods.ON_Mesh_FaceTopologicalVertices(const_ptr_mesh, faceIndex, v))
        return new int[0];
      return v;
    }

#if RHINO_SDK

    /// <summary>
    /// Gets an array of pairs of mesh faces that clash.
    /// </summary>
    /// <param name="maxPairCount">
    /// If >0, then at most this many pairs will be added to the output array.
    /// If &lt;=0, then all clashing pairs will be added to the output array.
    /// </param>
    /// <returns>Array of pairs of mesh face indices.</returns>
    public IndexPair[] GetClashingFacePairs(int maxPairCount)
    {
      using (var face_indices = new Runtime.InteropWrappers.SimpleArray2dex())
      {
        IntPtr const_ptr_this = m_mesh.ConstPointer();
        IntPtr ptr_face_indices = face_indices.NonConstPointer();
        int rc = UnsafeNativeMethods.ON_Mesh_GetClashingFacePairs(const_ptr_this, maxPairCount, ptr_face_indices);
        if (rc > 0)
          return face_indices.ToArray();
      }
      return new IndexPair[0];
    }

    /// <summary>
    /// Find all connected face indices where adjacent face normals meet
    /// the criteria of angleRadians and greaterThanAngle
    /// </summary>
    /// <param name="faceIndex">face index to start from</param>
    /// <param name="angleRadians">angle to use for comparison of what is connected</param>
    /// <param name="greaterThanAngle">
    /// If true angles greater than or equal to are considered connected.
    /// If false, angles less than or equal to are considerd connected.</param>
    /// <returns>list of connected face indices</returns>
    public int[] GetConnectedFaces(int faceIndex, double angleRadians, bool greaterThanAngle)
    {
      IntPtr ptr_const_mesh = m_mesh.ConstPointer();
      using (var indices = new SimpleArrayInt())
      {
        IntPtr ptr_simplearray_int = indices.NonConstPointer();
        UnsafeNativeMethods.RHC_RhinoMakeConnectedMeshFaceList(ptr_const_mesh, faceIndex, angleRadians, greaterThanAngle, ptr_simplearray_int);
        return indices.ToArray();
      }
    }

    /// <summary>
    /// Uses startFaceIndex and finds all connected face indexes up to unwelded
    /// or naked edges. If treatNonmanifoldLikeUnwelded is true then non-manifold
    /// edges will be considered as unwelded or naked
    /// </summary>
    /// <param name="startFaceIndex">Initial face index</param>
    /// <param name="treatNonmanifoldLikeUnwelded">
    /// True means non-manifold edges will be handled like unwelded edges, 
    /// False means they aren't considered
    /// </param>
    /// <returns>Array of connected face indexes</returns>
    public int[] GetConnectedFacesToEdges(int startFaceIndex, bool treatNonmanifoldLikeUnwelded)
    {
      IntPtr ptr_const_mesh = m_mesh.ConstPointer();
      using (var indices = new SimpleArrayInt())
      {
        IntPtr ptr_simplearray_int = indices.NonConstPointer();
        UnsafeNativeMethods.RHC_RhinoMakeMeshPartFaceList(ptr_const_mesh, startFaceIndex, treatNonmanifoldLikeUnwelded, ptr_simplearray_int);
        return indices.ToArray();
      }
    }

    /// <summary>
    /// Examines and adds face indexes to whollyDegenerateFaces if the face is a triangle with zero area or a quad both triangles have zero area.
    /// Face indexes are added to partiallyDegenerateFaces when a quad has one triangle with zero area.
    /// </summary>
    /// <param name="whollyDegenerateFaces">Array of indexes for faces, both triangles and quads, that have zero area.</param>
    /// <param name="partiallyDegenerateFaces">Array of indexes for quad faces, that have one triangle with zero area.</param>
    /// <returns>Returns true if the mesh has wholly or partially degenerate faces, false otherwise.</returns>
    public bool GetZeroAreaFaces(out int[] whollyDegenerateFaces, out int[] partiallyDegenerateFaces)
    {
      var ptr_const_mesh = m_mesh.ConstPointer();
      using (var wholly = new SimpleArrayInt())
      using (var partially = new SimpleArrayInt())
      {
        var ptr_wholly = wholly.NonConstPointer();
        var ptr_partially = partially.NonConstPointer();
        bool rc = UnsafeNativeMethods.RHC_RhinoGetZeroAreaMeshFaces(ptr_const_mesh, ptr_wholly, ptr_partially);
        whollyDegenerateFaces = (rc) ? wholly.ToArray() : new int[0];
        partiallyDegenerateFaces = (rc) ? partially.ToArray() : new int[0];
        return rc;
      }
    }

    /// <summary>
    /// Deletes or fixes mesh faces that have zero area.
    /// </summary>
    /// <param name="fixedFaceCount">Number of fixed partially degenerate faces.</param>
    /// <returns>Number of removed wholly degenerate faces.</returns>
    public int RemoveZeroAreaFaces(ref int fixedFaceCount)
    {
      IntPtr ptr_mesh = m_mesh.NonConstPointer();
      return UnsafeNativeMethods.RHC_RhinoCullZeroAreaMeshFaces(ptr_mesh, ref fixedFaceCount);
    }

    /// <summary>
    /// Finds all of the duplicate faces.
    /// </summary>
    /// <returns>
    /// The indexes that are duplicates of other indexes if successful. 
    /// If there are no duplicate, then an empty array is returned.
    /// </returns>
    public int[] GetDuplicateFaces()
    {
      IntPtr ptr_const_mesh = m_mesh.ConstPointer();
      using (var duplicates = new SimpleArrayInt())
      {
        IntPtr ptr_duplicates = duplicates.NonConstPointer();
        int rc = UnsafeNativeMethods.RHC_RhinoGetDuplicateMeshFaces(ptr_const_mesh, ptr_duplicates);
        return 0 == rc ? new int[0] : duplicates.ToArray();
      }
    }

    /// <summary>
    /// Extracts, or removes, duplicate faces.
    /// </summary>
    /// <returns>A mesh containing the extracted duplicate faces if successful, null otherwise.</returns>
    public Mesh ExtractDuplicateFaces()
    {
      IntPtr ptr_mesh = m_mesh.NonConstPointer();
      IntPtr ptr = UnsafeNativeMethods.RHC_RhinoExtractDuplicateMeshFaces(ptr_mesh);
      return IntPtr.Zero == ptr ? null : new Mesh(ptr, null);
    }

    /// <summary>
    /// Extracts, or removes, faces.
    /// </summary>
    /// <param name="faceIndices">The face indices to be extracted.</param>
    /// <returns>A mesh containing the extracted faces if successful, null otherwise.</returns>
    public Mesh ExtractFaces(IEnumerable<int> faceIndices)
    {
      IntPtr ptr_mesh = m_mesh.NonConstPointer();
      using (var indices = new SimpleArrayInt(faceIndices))
      {
        IntPtr ptr_indices = indices.NonConstPointer();
        IntPtr ptr = UnsafeNativeMethods.RHC_RhinoExtractMeshFaces(ptr_mesh, ptr_indices);
        return IntPtr.Zero == ptr ? null : new Mesh(ptr, null);
      }
    }

#endif
    #endregion

    #region IResizableList<Point3f>, IList and related implementations

    int IList<MeshFace>.IndexOf(MeshFace item)
    {
      return GenericIListImplementation.IndexOf(this, item);
    }

    void IList<MeshFace>.Insert(int index, MeshFace item)
    {
      GenericIListImplementation.Insert(this, index, item);
    }

    void IList<MeshFace>.RemoveAt(int index)
    {
      GenericIListImplementation.RemoveAt(this, index);
    }

    void ICollection<MeshFace>.Add(MeshFace item)
    {
      AddFace(item);
    }

    bool ICollection<MeshFace>.Contains(MeshFace item)
    {
      return GenericIListImplementation.IndexOf(this, item) != -1;
    }

    void ICollection<MeshFace>.CopyTo(MeshFace[] array, int arrayIndex)
    {
      GenericIListImplementation.CopyTo(this, array, arrayIndex);
    }

    bool ICollection<MeshFace>.IsReadOnly
    {
      get { return false; }
    }

    bool ICollection<MeshFace>.Remove(MeshFace item)
    {
      return GenericIListImplementation.Remove(this, item);
    }

    int IList.Add(object value)
    {
      var item = (MeshFace)(value);
      return AddFace(item);
    }

    const string EXCEPTION_VALUE_NOT_MESHFACE = "Input to the method must be a mesh face.";

    bool IList.Contains(object value)
    {
      if (!(value is MeshFace)) throw new ArgumentException(EXCEPTION_VALUE_NOT_MESHFACE, "value");
      return GenericIListImplementation.Contains(this, (MeshFace)value);
    }

    int IList.IndexOf(object value)
    {
      if (!(value is MeshFace)) throw new ArgumentException(EXCEPTION_VALUE_NOT_MESHFACE, "value");
      return GenericIListImplementation.IndexOf(this, (MeshFace)value);
    }

    void IList.Insert(int index, object value)
    {
      if (!(value is MeshFace)) throw new ArgumentException(EXCEPTION_VALUE_NOT_MESHFACE, "value");
      GenericIListImplementation.Insert(this, index, (MeshFace)value);
    }

    bool IList.IsFixedSize
    {
      get { return false; }
    }

    bool IList.IsReadOnly
    {
      get { return false; }
    }

    void IList.Remove(object value)
    {
      if (!(value is MeshFace)) throw new ArgumentException(EXCEPTION_VALUE_NOT_MESHFACE, "value");
      var item = (MeshFace)value;
      GenericIListImplementation.Remove(this, item);
    }

    void IList.RemoveAt(int index)
    {
      GenericIListImplementation.RemoveAt(this, index);
    }

    object IList.this[int index]
    {
      get
      {
        return this[index];
      }
      set
      {
        if (!(value is MeshFace)) throw new ArgumentException(EXCEPTION_VALUE_NOT_MESHFACE, "value");
        this[index] = (MeshFace)value;
      }
    }

    void ICollection.CopyTo(Array array, int index)
    {
      GenericIListImplementation.CopyTo(this, array, index);
    }

    bool ICollection.IsSynchronized
    {
      get { return false; }
    }

    object ICollection.SyncRoot
    {
      get
      {
        throw GenericIListImplementation.MakeNoSyncException(this);
      }
    }

    /// <summary>
    /// Gets an enumerator that yields all faces in this collection.
    /// </summary>
    /// <returns>The enumerator.</returns>
    public IEnumerator<MeshFace> GetEnumerator()
    {
      int count = Count;
      for (int i = 0; i < count; i++)
        yield return this[i];
    }
    IEnumerator IEnumerable.GetEnumerator()
    {
      return GetEnumerator();
    }
    #endregion
  }


  /// <summary>
  /// Provides access to the ngons and n-gon related functionality of a Mesh.
  /// See also the <see cref="Rhino.Geometry.Mesh.Ngons"/> property for Ngon functionality details.
  /// </summary>
  public class MeshNgonList : IResizableList<MeshNgon>, IList, IReadOnlyList<MeshNgon>
  {
    private readonly Mesh m_mesh;

    #region constructors
    internal MeshNgonList(Mesh ownerMesh)
    {
      m_mesh = ownerMesh;
    }
    #endregion

    #region properties
    /// <summary>
    /// Gets or sets the number of mesh ngons.
    /// </summary>
    public int Count
    {
      get
      {
        IntPtr const_ptr_mesh = m_mesh.ConstPointer();
        return UnsafeNativeMethods.ON_Mesh_NgonCount(const_ptr_mesh);
      }
      set
      {
        IntPtr ptr_mesh = m_mesh.NonConstPointer();
        UnsafeNativeMethods.ON_Mesh_SetNgonCount(ptr_mesh, value);
      }
    }

    /// <summary>
    /// Gets or sets the number of mesh ngons.
    /// </summary>
    [CLSCompliant(false)]
    public uint UnsignedCount
    {
      get
      {
        IntPtr const_ptr_mesh = m_mesh.ConstPointer();
        return UnsafeNativeMethods.ON_Mesh_NgonUnsignedCount(const_ptr_mesh);
      }
      set
      {
        IntPtr ptr_mesh = m_mesh.NonConstPointer();
        UnsafeNativeMethods.ON_Mesh_SetNgonUnsignedCount(ptr_mesh, value);
      }
    }

    #endregion

    #region methods
    #region ngon access
    /// <summary>
    /// Clears the Ngon list on the mesh.
    /// </summary>
    public void Clear()
    {
      IntPtr ptr_mesh = m_mesh.NonConstPointer();
      UnsafeNativeMethods.ON_Mesh_SetNgonCount(ptr_mesh, 0);
    }

    /// <summary>
    /// Appends a new ngon to the end of the mesh ngon list.
    /// </summary>
    /// <param name="ngon">Ngon to add.</param>
    /// <returns>The index of the newly added ngon.</returns>
    public int AddNgon(MeshNgon ngon)
    {
      IntPtr ptr_mesh = m_mesh.NonConstPointer();
      return UnsafeNativeMethods.ON_Mesh_AddNgon(ptr_mesh, (uint)ngon.m_vi.LongLength, ngon.m_vi, (uint)ngon.m_fi.LongLength, ngon.m_fi);
    }

#if RHINO_SDK
    /// <summary>
    /// Appends a new ngon to the end of the mesh ngon list.
    /// <para>This override will also add the necessary triangular faces to the respective MeshFaceList of the same mesh class.</para>
    /// <para>If a triangle or a quad is specified, the necessary operation is executed on the face list and no ngon is added to the mesh.</para>
    /// </summary>
    /// <param name="boundary">An array of indices.</param>
    /// <returns>
    /// <para>1 if an ngon was added, along with the necessary triangles.</para>
    /// <para>0 if a triangle or a quad was provided and the necessary faces were added.</para>
    /// <para>-1 if an error other than the two exceptions occurred when tessellating boundary vertices and no ngon was added.</para>
    /// </returns>
    /// <exception cref="ArgumentNullException">If boundary is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">If boundary is less than 3 items long.</exception>
    internal int AddNgonBoundary(int[] boundary)
    {
      if (boundary == null) throw new ArgumentNullException("boundary");
      if (boundary.Length < 3) throw new ArgumentOutOfRangeException("boundary", "There need to be at least 3 vertices.");

      IntPtr ptr_mesh = m_mesh.NonConstPointer();
      return UnsafeNativeMethods.ON_Mesh_AddNgon_Boundary(ptr_mesh, boundary, boundary.Length);
    }
#endif

    /// <summary>
    /// Appends a list of ngons to the end of the mesh ngon list.
    /// </summary>
    /// <param name="ngons">Ngons to add.</param>
    /// <returns>Indices of the newly created ngons</returns>
    public int[] AddNgons(IEnumerable<MeshNgon> ngons)
    {
      IntPtr mesh_ptr = m_mesh.NonConstPointer();
      var ngon_id_list = new List<int>();
      foreach (var ngon in ngons)
      {
        int index = AddNgon(ngon);
        ngon_id_list.Add(index);
      }
      return ngon_id_list.ToArray();
    }

    /// <summary>
    /// Add an ngon for each group of connected coplanar faces.
    /// </summary>
    /// <param name="planarTolerance">3d distance tolerance for coplanar test.</param>
    /// <returns>Number of ngons added to the mesh.</returns>
    public int AddPlanarNgons(double planarTolerance)
    {
      IntPtr ptr_mesh = m_mesh.NonConstPointer();
      return UnsafeNativeMethods.ON_Mesh_AddPlanarNgons(ptr_mesh, planarTolerance, 0, 0, false);
    }

    /// <summary>
    /// Add an ngon for each group of connected coplanar faces.
    /// </summary>
    /// <param name="planarTolerance">3d distance tolerance for coplanar test.</param>
    /// <param name="minimumNgonVertexCount">Mininimum number of vertices for an ngon.</param>
    /// <param name="minimumNgonFaceCount">Minimum number of faces for an ngon.</param>
    /// <param name="allowHoles">Determines whether the ngon can have inner boundaries.</param>
    /// <returns>Number of ngons added to the mesh.</returns>
    public int AddPlanarNgons(double planarTolerance, int minimumNgonVertexCount, int minimumNgonFaceCount, bool allowHoles)
    {
      IntPtr ptr_mesh = m_mesh.NonConstPointer();
      return UnsafeNativeMethods.ON_Mesh_AddPlanarNgons(ptr_mesh, planarTolerance, minimumNgonVertexCount, minimumNgonFaceCount, allowHoles);
    }

    /// <summary>
    /// Get an ngon's boundary points.
    /// </summary>
    /// <param name="ngon">ngon.</param>
    /// <param name="bAppendStartPoint">If true, the first point in the list is also appended to the end of the list to create a closed polyline.</param>
    /// <returns>A list of ngon boundary points.</returns>
    public Point3d[] NgonBoundaryVertexList(MeshNgon ngon, bool bAppendStartPoint)
    {
      Point3d[] ngon_bdry_point_list = null;
      IntPtr mesh_cptr = m_mesh.ConstPointer();
      using (var ngon_bdry_pts = new SimpleArrayPoint3d())
      {
        IntPtr const_ptr_bdry_pts = ngon_bdry_pts.ConstPointer();
        int count = UnsafeNativeMethods.ON_Mesh_GetNgonBoundaryPoints(mesh_cptr, bAppendStartPoint, const_ptr_bdry_pts, (uint)ngon.m_vi.LongLength, ngon.m_vi, (uint)ngon.m_fi.LongLength, ngon.m_fi);
        if (count > 0)
          ngon_bdry_point_list = ngon_bdry_pts.ToArray();
      }
      return ngon_bdry_point_list;
    }

    /// <summary>
    /// Inserts a mesh ngon at a defined index in this list.
    /// </summary>
    /// <param name="index">An ngon index.</param>
    /// <param name="ngon">An ngon.</param>
    /// <exception cref="ArgumentOutOfRangeException">If index is negative or &gt;= Count.</exception>
    public void Insert(int index, MeshNgon ngon)
    {
      bool rc = false;
      if (index >= 0)
      {
        IntPtr ptr_mesh = m_mesh.NonConstPointer();
        rc = UnsafeNativeMethods.ON_Mesh_InsertNgon(ptr_mesh, index, (uint)ngon.m_vi.LongLength, ngon.m_vi, (uint)ngon.m_fi.LongLength, ngon.m_fi);
      }
      if (!rc)
        throw new ArgumentOutOfRangeException("index");
    }

    /// <summary>
    /// Set an ngon in this list.
    /// </summary>
    /// <param name="index">An ngon index.</param>
    /// <param name="ngon">An ngon.</param>
    /// <exception cref="ArgumentOutOfRangeException">If index is negative or &gt;= Count.</exception>
    public void SetNgon(int index, MeshNgon ngon)
    {
      bool rc = false;
      if (index >= 0)
      {
        IntPtr ptr_mesh = m_mesh.NonConstPointer();
        rc = UnsafeNativeMethods.ON_Mesh_ModifyNgon(ptr_mesh, index, (uint)ngon.m_vi.LongLength, ngon.m_vi, (uint)ngon.m_fi.LongLength, ngon.m_fi);
      }
      if (!rc)
        throw new ArgumentOutOfRangeException("index");
    }

    /// <summary>
    /// Returns the mesh ngon at the given index. 
    /// </summary>
    /// <param name="index">Index of ngon to get. Must be larger than or equal to zero and 
    /// smaller than the Ngon Count of the mesh.</param>
    /// <returns>The mesh ngon at the given index.  This ngon can be MeshNgon.Empty.</returns>
    public MeshNgon GetNgon(int index)
    {
      IntPtr ptr_mesh = m_mesh.NonConstPointer();
      IntPtr ptr_ngon = UnsafeNativeMethods.ON_Mesh_Ngon(ptr_mesh, index);
      return new MeshNgon(ptr_ngon);
    }

    /// <summary>
    /// Returns the index of a mesh ngon the face belongs to.
    /// </summary>
    /// <param name="meshFaceIndex">Index of a mesh face.</param>
    /// <returns>The index of the mesh ngon the face belongs to or -1 if the face does not belong to an ngon.</returns>
    public int NgonIndexFromFaceIndex(int meshFaceIndex)
    {
      IntPtr const_ptr_mesh = m_mesh.ConstPointer();
      return UnsafeNativeMethods.ON_Mesh_NgonIndexFromFaceIndex(const_ptr_mesh, meshFaceIndex);
    }

    /// <summary>
    /// Returns the mesh ngon at the given index. 
    /// </summary>
    /// <param name="index">Index of face to get. Must be larger than or equal to zero and 
    /// smaller than the Face Count of the mesh.</param>
    public MeshNgon this[int index]
    {
      get
      {
        // TODO: Check to see if this really needs to be a NonConstPointer
        IntPtr ptr_mesh = m_mesh.NonConstPointer();
        IntPtr ngon_ptr = UnsafeNativeMethods.ON_Mesh_Ngon(ptr_mesh, index);
        return new MeshNgon(ngon_ptr);
      }
      set
      {
        SetNgon(index, value);
      }
    }

    /// <summary>
    /// Gets the bounding box of an ngon.
    /// </summary>
    /// <param name="index">A ngon index.</param>
    /// <returns>A new bounding box, or <see cref="BoundingBox.Empty"/> on error.</returns>
    public BoundingBox GetNgonBoundingBox(int index)
    {
      IntPtr const_ptr_mesh = m_mesh.ConstPointer();
      BoundingBox bbox = BoundingBox.Empty;
      UnsafeNativeMethods.ON_Mesh_GetNgonBoundingBoxFromNgonIndex(const_ptr_mesh, index, ref bbox);
      return bbox;
    }

    /// <summary>
    /// Gets the bounding box of an ngon.
    /// </summary>
    /// <param name="ngon">An ngon.</param>
    /// <returns>A new bounding box, or <see cref="BoundingBox.Empty"/> on error.</returns>
    public BoundingBox GetNgonBoundingBox(MeshNgon ngon)
    {
      IntPtr const_ptr_mesh = m_mesh.ConstPointer();
      BoundingBox bbox = BoundingBox.Empty;
      UnsafeNativeMethods.ON_Mesh_GetNgonBoundingBoxFromNgon(const_ptr_mesh, ref bbox, (uint)ngon.m_vi.LongLength, ngon.m_vi, (uint)ngon.m_fi.LongLength, ngon.m_fi);
      return bbox;
    }

    /// <summary>
    /// Gets the outer edge count of an ngon.
    /// </summary>
    /// <param name="index">Ngon index.</param>
    /// <returns>Outer edge count or zero on error.</returns>
    public int GetNgonOuterEdgeCount(int index)
    {
      IntPtr const_ptr_mesh = m_mesh.ConstPointer();
      return UnsafeNativeMethods.ON_MeshNgon_OuterBoundaryEdgeCount(const_ptr_mesh, (uint)index);
    }

    /// <summary>
    /// Gets the complete edge count of an ngon.
    /// </summary>
    /// <param name="index">Ngon index.</param>
    /// <returns>Complete edge count or zero on error.</returns>
    public int GetNgonEdgeCount(int index)
    {
      IntPtr const_ptr_mesh = m_mesh.ConstPointer();
      return UnsafeNativeMethods.ON_MeshNgon_BoundaryEdgeCount(const_ptr_mesh, (uint)index);
    }

    /// <summary>
    /// Determines whether a ngon has holes.
    /// </summary>
    /// <param name="index">Ngon index.</param>
    /// <returns>true for holes (or malformed ngon, see remarks), false for no holes.</returns>
    /// <remarks>A slit, for example, will give an edge count that differs from outer edge count despite the
    /// lack of true "holes" ie. interior edges that are not shared by more than one face of the ngon in question.</remarks>
    public bool NgonHasHoles(int index)
    {
      return GetNgonEdgeCount(index) != GetNgonOuterEdgeCount(index);
    }

    /// <summary>
    /// Gets the center point of an ngon.
    /// <para>This the avarage of the corner points.</para>
    /// </summary>
    /// <param name="index">A ngon index.</param>
    /// <returns>The center point.</returns>
    public Point3d GetNgonCenter(int index)
    {
      IntPtr const_ptr_mesh = m_mesh.ConstPointer();
      Point3d center = Point3d.Unset;
      if (!UnsafeNativeMethods.ON_Mesh_GetNgonCenterFromNgonIndex(const_ptr_mesh, index, ref center))
        throw new IndexOutOfRangeException();
      return center;
    }

    /// <summary>
    /// Gets the center point of an ngon.
    /// <para>This the avarage of the corner points.</para>
    /// </summary>
    /// <param name="ngon">An ngon.</param>
    /// <returns>The center point.</returns>
    public Point3d GetNgonCenter(MeshNgon ngon)
    {
      IntPtr const_ptr_mesh = m_mesh.ConstPointer();
      Point3d center = Point3d.Unset;
      if (!UnsafeNativeMethods.ON_Mesh_GetNgonCenterFromNgon(const_ptr_mesh, ref center, (uint)ngon.m_vi.LongLength, ngon.m_vi, (uint)ngon.m_fi.LongLength, ngon.m_fi))
        throw new IndexOutOfRangeException();
      return center;
    }

#endregion

    /// <summary>
    /// Remove one or more ngons from the mesh.
    /// </summary>
    /// <param name="indices">An array of ngon indices.</param>
    /// <returns>The number of deleted ngons.</returns>
    public int RemoveNgons(IEnumerable<int> indices)
    {
      if (null == indices)
        return 0;
      var ngon_index_list = new RhinoList<int>(indices);
      if (ngon_index_list.Count < 1)
        return 0;
      int[] ngon_index_array = ngon_index_list.m_items;
      IntPtr mesh_ptr = m_mesh.NonConstPointer();
      return UnsafeNativeMethods.ON_Mesh_RemoveNgons(mesh_ptr, ngon_index_list.Count, ngon_index_array);
    }

    /// <summary>
    /// Removes an ngon from the mesh.
    /// </summary>
    /// <param name="index">The index of the ngon.</param>
    public void RemoveAt(int index)
    {
      IntPtr ptr_mesh = m_mesh.NonConstPointer();
      UnsafeNativeMethods.ON_Mesh_RemoveNgon(ptr_mesh, index);
    }

    /// <summary>
    /// Get a list of vertices that form the boundary of a face set. This is often use to get n-gon boundaries.
    /// </summary>
    /// <param name="ngonFaceIndexList">List of mesh face indices.</param>
    /// <returns>List of mesh vertex indices that form the boundary of the face set.</returns>
    public int[] GetNgonBoundary(IEnumerable<int> ngonFaceIndexList)
    {
      int[] ngon_vertex_list = null;
      if (null != ngonFaceIndexList)
      {
        var ngon_fi = new RhinoList<int>(ngonFaceIndexList);
        if (ngon_fi.Count > 0)
        {
          IntPtr const_ptr_mesh = m_mesh.ConstPointer();
          using (var ngon_vi = new SimpleArrayInt())
          {
            int[] ngon_fi_array = ngon_fi.m_items;
            int vertex_count = UnsafeNativeMethods.ON_Mesh_GetNgonBoundary(const_ptr_mesh, ngon_fi.Count, ngon_fi_array, ngon_vi.m_ptr);
            if (vertex_count > 0)
              ngon_vertex_list = ngon_vi.ToArray();
          }
        }
      }
      return ngon_vertex_list;
    }

    /// <summary>
    /// Tests an ngon to see if the vertex and face references are valid and pass partial boundary validity checks.
    /// </summary>
    /// <param name="index">The index of the ngon to test.</param>
    /// <returns>0 if the ngon is not valid, otherwise the number of boundary edges.</returns>
    /// <remarks>If the return value is > MeshNgon.BoundaryVertexCount, then the ngon has either inner boundaries or duplicate vertices.</remarks>
    [CLSCompliant(false)]
    public uint IsValid(int index)
    {
      return IsValid(index, null);
    }

    /// <summary>
    /// Tests an ngon to see if the vertex and face references are valid and pass partial boundary validity checks.
    /// </summary>
    /// <param name="index">The index of the ngon to test.</param>
    /// <param name="textLog">A textlog for collecting information about problems.</param>
    /// <returns>0 if the ngon is not valid, otherwise the number of boundary edges.</returns>
    /// <remarks>If the return value is > MeshNgon.BoundaryVertexCount, then the ngon has either inner boundaries or duplicate vertices.</remarks>
    [CLSCompliant(false)]
    public uint IsValid(int index, TextLog textLog)
    {
      IntPtr ptr_textlog = textLog?.NonConstPointer() ?? IntPtr.Zero;
      IntPtr ptr_const_mesh = m_mesh.ConstPointer();
      return UnsafeNativeMethods.ON_MeshNgon_IsValid(ptr_const_mesh, index, ptr_textlog);
    }

#endregion

#region IResizableList<Point3f>, IList and related implementations

    int IList<MeshNgon>.IndexOf(MeshNgon item)
    {
      return GenericIListImplementation.IndexOf(this, item);
    }

    void IList<MeshNgon>.Insert(int index, MeshNgon item)
    {
      GenericIListImplementation.Insert(this, index, item);
    }

    void IList<MeshNgon>.RemoveAt(int index)
    {
      GenericIListImplementation.RemoveAt(this, index);
    }

    void ICollection<MeshNgon>.Add(MeshNgon item)
    {
      AddNgon(item);
    }

    bool ICollection<MeshNgon>.Contains(MeshNgon item)
    {
      return GenericIListImplementation.IndexOf(this, item) != -1;
    }

    void ICollection<MeshNgon>.CopyTo(MeshNgon[] array, int arrayIndex)
    {
      GenericIListImplementation.CopyTo(this, array, arrayIndex);
    }

    bool ICollection<MeshNgon>.IsReadOnly
    {
      get { return false; }
    }

    bool ICollection<MeshNgon>.Remove(MeshNgon item)
    {
      return GenericIListImplementation.Remove(this, item);
    }

    int IList.Add(object value)
    {
      var item = (MeshNgon)(value);
      return AddNgon(item);
    }

    const string EXCEPTION_VALUE_NOT_MESHNGON = "Input to the method must be a mesh ngon.";

    bool IList.Contains(object value)
    {
      if (!(value is MeshNgon)) throw new ArgumentException(EXCEPTION_VALUE_NOT_MESHNGON, "value");
      return GenericIListImplementation.Contains(this, (MeshNgon)value);
    }

    int IList.IndexOf(object value)
    {
      if (!(value is MeshNgon)) throw new ArgumentException(EXCEPTION_VALUE_NOT_MESHNGON, "value");
      return GenericIListImplementation.IndexOf(this, (MeshNgon)value);
    }

    void IList.Insert(int index, object value)
    {
      if (!(value is MeshNgon)) throw new ArgumentException(EXCEPTION_VALUE_NOT_MESHNGON, "value");
      GenericIListImplementation.Insert(this, index, (MeshNgon)value);
    }

    bool IList.IsFixedSize
    {
      get { return false; }
    }

    bool IList.IsReadOnly
    {
      get { return false; }
    }

    void IList.Remove(object value)
    {
      if (!(value is MeshNgon)) throw new ArgumentException(EXCEPTION_VALUE_NOT_MESHNGON, "value");
      var item = (MeshNgon)value;
      GenericIListImplementation.Remove(this, item);
    }

    void IList.RemoveAt(int index)
    {
      GenericIListImplementation.RemoveAt(this, index);
    }

    object IList.this[int index]
    {
      get
      {
        return this[index];
      }
      set
      {
        if (!(value is MeshNgon)) throw new ArgumentException(EXCEPTION_VALUE_NOT_MESHNGON, "value");
        this[index] = (MeshNgon)value;
      }
    }

    void ICollection.CopyTo(Array array, int index)
    {
      GenericIListImplementation.CopyTo(this, array, index);
    }

    bool ICollection.IsSynchronized
    {
      get { return false; }
    }

    object ICollection.SyncRoot
    {
      get
      {
        throw GenericIListImplementation.MakeNoSyncException(this);
      }
    }

    /// <summary>
    /// Gets an enumerator that yields all ngons in this collection.
    /// </summary>
    /// <returns>The enumerator.</returns>
    public IEnumerator<MeshNgon> GetEnumerator()
    {
      int count = Count;
      for (int i = 0; i < count; i++)
        yield return this[i];
    }
    IEnumerator IEnumerable.GetEnumerator()
    {
      return GetEnumerator();
    }
#endregion
  }


  /// <summary>
  /// Provides access to the Face normals of a Mesh.
  /// </summary>
  public class MeshFaceNormalList : IResizableList<Vector3f>, IList, IReadOnlyList<Vector3f>
  {
    private readonly Mesh m_mesh;

#region constructors
    internal MeshFaceNormalList(Mesh ownerMesh)
    {
      m_mesh = ownerMesh;
    }
#endregion

#region properties
    /// <summary>
    /// Gets or sets the number of mesh face normals.
    /// </summary>
    public int Count
    {
      get
      {
        IntPtr const_ptr_mesh = m_mesh.ConstPointer();
        return UnsafeNativeMethods.ON_Mesh_GetInt(const_ptr_mesh, UnsafeNativeMethods.MeshIntConst.FaceNormalCount);
      }
      set
      {
        if (value >= 0 && Count != value)
        {
          IntPtr ptr_mesh = m_mesh.NonConstPointer();
          UnsafeNativeMethods.ON_Mesh_SetInt(ptr_mesh, UnsafeNativeMethods.MeshIntConst.FaceNormalCount, value);
        }
      }
    }

    /// <summary>
    /// Gets or sets the face normal at the given face index. 
    /// The index must be valid or an IndexOutOfRangeException will be thrown.
    /// </summary>
    /// <param name="index">Index of face normal to access.</param>
    /// <exception cref="IndexOutOfRangeException">Thrown when the index is invalid.</exception>
    /// <returns>The face normal at [index].</returns>
    public Vector3f this[int index]
    {
      get
      {
        if (index < 0 || index >= Count)
          throw new IndexOutOfRangeException();

        var rc = new Vector3f();
        IntPtr const_ptr_mesh = m_mesh.ConstPointer();
        UnsafeNativeMethods.ON_Mesh_GetNormal(const_ptr_mesh, index, ref rc, true);
        return rc;
      }
      set
      {
        if (index < 0 || index >= Count)
          throw new IndexOutOfRangeException();

        IntPtr ptr_mesh = m_mesh.NonConstPointer();
        UnsafeNativeMethods.ON_Mesh_SetNormal(ptr_mesh, index, value, true);
      }
    }

    /// <summary>
    /// Gets or sets the total number of face normals the internal data structure can hold without resizing.
    /// </summary>
    public int Capacity
    {
      get
      {
        IntPtr const_ptr_mesh = m_mesh.ConstPointer();
        return UnsafeNativeMethods.ON_Mesh_GetInt(const_ptr_mesh, UnsafeNativeMethods.MeshIntConst.FaceNormalCapacity);
      }
      set
      {
        if (Capacity != value)
        {
          IntPtr ptr_mesh = m_mesh.NonConstPointer();
          UnsafeNativeMethods.ON_Mesh_SetInt(ptr_mesh, UnsafeNativeMethods.MeshIntConst.FaceNormalCapacity, value);
        }
      }
    }
#endregion

#region methods
#region face access
    /// <summary>
    /// Clears the Face Normal list on the mesh.
    /// </summary>
    public void Clear()
    {
      IntPtr ptr_mesh = m_mesh.NonConstPointer();
      UnsafeNativeMethods.ON_Mesh_ClearList(ptr_mesh, UnsafeNativeMethods.MeshClearListConst.ClearFaceNormals);
    }

    /// <summary>
    /// Releases all memory allocated to store face normals. The list capacity will be 0 after this call.
    /// <para>Subsequent calls can add new items.</para>
    /// </summary>
    public void Destroy()
    {
      IntPtr ptr_mesh = m_mesh.NonConstPointer();
      UnsafeNativeMethods.ON_Mesh_SetInt(ptr_mesh, UnsafeNativeMethods.MeshIntConst.FaceNormalCapacity, 0);
    }

    /// <summary>
    /// Appends a face normal to the list of mesh face normals.
    /// </summary>
    /// <param name="x">X component of face normal.</param>
    /// <param name="y">Y component of face normal.</param>
    /// <param name="z">Z component of face normal.</param>
    /// <returns>The index of the newly added face normal.</returns>
    public int AddFaceNormal(float x, float y, float z)
    {
      return AddFaceNormal(new Vector3f(x, y, z));
    }
    /// <summary>
    /// Appends a face normal to the list of mesh face normals.
    /// </summary>
    /// <param name="x">X component of face normal.</param>
    /// <param name="y">Y component of face normal.</param>
    /// <param name="z">Z component of face normal.</param>
    /// <returns>The index of the newly added face normal.</returns>
    public int AddFaceNormal(double x, double y, double z)
    {
      return AddFaceNormal(new Vector3f((float)x, (float)y, (float)z));
    }
    /// <summary>
    /// Appends a face normal to the list of mesh face normals.
    /// </summary>
    /// <param name="normal">New face normal.</param>
    /// <returns>The index of the newly added face normal.</returns>
    public int AddFaceNormal(Vector3d normal)
    {
      return AddFaceNormal(new Vector3f((float)normal.m_x, (float)normal.m_y, (float)normal.m_z));
    }
    /// <summary>
    /// Appends a face normal to the list of mesh face normals.
    /// </summary>
    /// <param name="normal">New face normal.</param>
    /// <returns>The index of the newly added face normal.</returns>
    public int AddFaceNormal(Vector3f normal)
    {
      SetFaceNormal(Count, normal);
      return Count - 1;
    }

    /// <summary>
    /// Sets a face normal vector at an index using three single-precision numbers.
    /// </summary>
    /// <param name="index">An index.</param>
    /// <param name="x">A x component.</param>
    /// <param name="y">A y component.</param>
    /// <param name="z">A z component.</param>
    /// <returns>true on success; false on error.</returns>
    public bool SetFaceNormal(int index, float x, float y, float z)
    {
      return SetFaceNormal(index, new Vector3f(x, y, z));
    }

    /// <summary>
    /// Sets a face normal vector at an index using three double-precision numbers.
    /// </summary>
    /// <param name="index">An index.</param>
    /// <param name="x">A x component.</param>
    /// <param name="y">A y component.</param>
    /// <param name="z">A z component.</param>
    /// <returns>true on success; false on error.</returns>
    public bool SetFaceNormal(int index, double x, double y, double z)
    {
      return SetFaceNormal(index, new Vector3f((float)x, (float)y, (float)z));
    }

    /// <summary>
    /// Sets a face normal vector at an index using a single-precision vector.
    /// </summary>
    /// <param name="index">An index.</param>
    /// <param name="normal">A normal vector.</param>
    /// <returns>true on success; false on error.</returns>
    public bool SetFaceNormal(int index, Vector3d normal)
    {
      return SetFaceNormal(index, new Vector3f((float)normal.m_x, (float)normal.m_y, (float)normal.m_z));
    }

    /// <summary>
    /// Sets a face normal vector at an index using a single-precision vector.
    /// </summary>
    /// <param name="index">An index.</param>
    /// <param name="normal">A normal vector.</param>
    /// <returns>true on success; false on error.</returns>
    public bool SetFaceNormal(int index, Vector3f normal)
    {
      IntPtr ptr_mesh = m_mesh.NonConstPointer();
      return UnsafeNativeMethods.ON_Mesh_SetNormal(ptr_mesh, index, normal, true);
    }
#endregion

    /// <summary>
    /// Unitizes all the existing face normals.
    /// </summary>
    /// <returns>true on success, false on failure.</returns>
    public bool UnitizeFaceNormals()
    {
      IntPtr ptr_mesh = m_mesh.NonConstPointer();
      return UnsafeNativeMethods.ON_Mesh_NonConstBoolOp(ptr_mesh, UnsafeNativeMethods.MeshNonConstBoolConst.UnitizeFaceNormals);
    }

    /// <summary>
    /// Computes all the face normals for this mesh based on the physical shape of the mesh.
    /// </summary>
    /// <returns>true on success, false on failure.</returns>
    public bool ComputeFaceNormals()
    {
      IntPtr ptr_mesh = m_mesh.NonConstPointer();
      return UnsafeNativeMethods.ON_Mesh_NonConstBoolOp(ptr_mesh, UnsafeNativeMethods.MeshNonConstBoolConst.ComputeFaceNormals);
    }
#endregion

#region IResizableList<Point3f>, IList and related implementations
    int IList<Vector3f>.IndexOf(Vector3f item)
    {
      return GenericIListImplementation.IndexOf(this, item);
    }

    void IList<Vector3f>.Insert(int index, Vector3f item)
    {
      GenericIListImplementation.Insert(this, index, item);
    }

    void IList<Vector3f>.RemoveAt(int index)
    {
      GenericIListImplementation.RemoveAt(this, index);
    }

    void ICollection<Vector3f>.Add(Vector3f item)
    {
      AddFaceNormal(item);
    }

    bool ICollection<Vector3f>.Contains(Vector3f item)
    {
      return GenericIListImplementation.IndexOf(this, item) != -1;
    }

    void ICollection<Vector3f>.CopyTo(Vector3f[] array, int arrayIndex)
    {
      GenericIListImplementation.CopyTo(this, array, arrayIndex);
    }

    bool ICollection<Vector3f>.IsReadOnly
    {
      get { return false; }
    }

    bool ICollection<Vector3f>.Remove(Vector3f item)
    {
      return GenericIListImplementation.Remove(this, item);
    }

    int IList.Add(object value)
    {
      var item = GenericIListImplementation.HelperCoerceVector(value);
      return AddFaceNormal(item);
    }

    bool IList.Contains(object value)
    {
      return GenericIListImplementation.Contains(this, value);
    }

    int IList.IndexOf(object value)
    {
      return GenericIListImplementation.IndexOf(this, value);
    }

    void IList.Insert(int index, object value)
    {
      var item = GenericIListImplementation.HelperCoerceVector(value);
      GenericIListImplementation.Insert(this, index, item);
    }

    bool IList.IsFixedSize
    {
      get { return false; }
    }

    bool IList.IsReadOnly
    {
      get { return false; }
    }

    void IList.Remove(object value)
    {
      var item = GenericIListImplementation.HelperCoerceVector(value);
      GenericIListImplementation.Remove(this, item);
    }

    void IList.RemoveAt(int index)
    {
      GenericIListImplementation.RemoveAt(this, index);
    }

    object IList.this[int index]
    {
      get
      {
        return this[index];
      }
      set
      {
        this[index] = GenericIListImplementation.HelperCoerceVector(value);
      }
    }

    void ICollection.CopyTo(Array array, int index)
    {
      GenericIListImplementation.CopyTo(this, array, index);
    }

    bool ICollection.IsSynchronized
    {
      get { return false; }
    }

    object ICollection.SyncRoot
    {
      get
      {
        throw GenericIListImplementation.MakeNoSyncException(this);
      }
    }

    /// <summary>
    /// Gets an enumerator that yields all normals (vectors) in this collection.
    /// </summary>
    /// <returns>The enumerator.</returns>
    public IEnumerator<Vector3f> GetEnumerator()
    {
      var count = Count;
      for (int i = 0; i < count; i++)
        yield return this[i];
    }
    IEnumerator IEnumerable.GetEnumerator()
    {
      return GetEnumerator();
    }
#endregion
  }

  /// <summary>
  /// Provides access to the vertex colors of a mesh object.
  /// </summary>
  public class MeshVertexColorList : IResizableList<Color>, IList, IReadOnlyList<Color>
  {
    private readonly Mesh m_mesh;

#region constructors
    internal MeshVertexColorList(Mesh ownerMesh)
    {
      m_mesh = ownerMesh;
    }
#endregion

#region properties
    /// <summary>
    /// Gets or sets the number of mesh colors.
    /// </summary>
    public int Count
    {
      get
      {
        IntPtr const_ptr_mesh = m_mesh.ConstPointer();
        return UnsafeNativeMethods.ON_Mesh_GetInt(const_ptr_mesh, UnsafeNativeMethods.MeshIntConst.ColorCount);
      }
      set
      {
        if (value >= 0 && Count != value)
        {
          IntPtr ptr_mesh = m_mesh.NonConstPointer();
          UnsafeNativeMethods.ON_Mesh_SetInt(ptr_mesh, UnsafeNativeMethods.MeshIntConst.ColorCount, value);
        }
      }
    }

    /// <summary>
    /// Gets or sets the vertex color at the given index. 
    /// The index must be valid or an IndexOutOfRangeException will be thrown.
    /// </summary>
    /// <param name="index">Index of vertex control to access.</param>
    /// <exception cref="IndexOutOfRangeException">Thrown when the index is invalid.</exception>
    /// <returns>The vertex color at [index].</returns>
    public Color this[int index]
    {
      get
      {
        int argb = 0;
        IntPtr const_ptr_mesh = m_mesh.ConstPointer();
        // get color will return false when the index is out of range
        if (!UnsafeNativeMethods.ON_Mesh_GetColor(const_ptr_mesh, index, ref argb))
          throw new IndexOutOfRangeException();
        return Color.FromArgb(argb);
      }
      set
      {
        if (index < 0 || index >= Count)
          throw new IndexOutOfRangeException();

        IntPtr ptr_mesh = m_mesh.NonConstPointer();
        UnsafeNativeMethods.ON_Mesh_SetColor(ptr_mesh, index, value.ToArgb());
      }
    }

    /// <example>
    /// <code source='examples\vbnet\ex_analysismode.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_analysismode.cs' lang='cs'/>
    /// </example>
    /// <summary>
    /// Gets or sets a mapping information for the mesh associated with these vertex colors.
    /// </summary>
    public MappingTag Tag
    {
      get
      {
        IntPtr const_ptr_mesh = m_mesh.ConstPointer();
        Guid id = Guid.Empty;
        int mapping_type = 0;
        uint crc = 0;
        var xf = new Transform();
        UnsafeNativeMethods.ON_Mesh_GetMappingTag(const_ptr_mesh, 0, ref id, ref mapping_type, ref crc, ref xf);
        var mt = new MappingTag
        {
          Id = id,
          MappingCRC = crc,
          MappingType = (TextureMappingType)mapping_type,
          MeshTransform = xf
        };
        return mt;
      }
      set
      {
        IntPtr ptr_mesh = m_mesh.NonConstPointer();
        Transform xf = value.MeshTransform;
        UnsafeNativeMethods.ON_Mesh_SetMappingTag(ptr_mesh, 0, value.Id, (int)value.MappingType, value.MappingCRC, ref xf);
      }
    }
#endregion

#region access
    /// <summary>
    /// Clears the vertex color list on the mesh.
    /// </summary>
    public void Clear()
    {
      IntPtr ptr_mesh = m_mesh.NonConstPointer();
      UnsafeNativeMethods.ON_Mesh_ClearList(ptr_mesh, UnsafeNativeMethods.MeshClearListConst.ClearColors);
    }

    /// <summary>
    /// Adds a new vertex color to the end of the color list.
    /// </summary>
    /// <param name="red">Red component of color, must be in the 0~255 range.</param>
    /// <param name="green">Green component of color, must be in the 0~255 range.</param>
    /// <param name="blue">Blue component of color, must be in the 0~255 range.</param>
    /// <returns>The index of the newly added color.</returns>
    public int Add(int red, int green, int blue)
    {
      SetColor(Count, red, green, blue);
      return Count - 1;
    }
    /// <summary>
    /// Adds a new vertex color to the end of the color list.
    /// </summary>
    /// <param name="color">Color to append, Alpha channels will be ignored.</param>
    /// <returns>The index of the newly added color.</returns>
    public int Add(Color color)
    {
      SetColor(Count, color);
      return Count - 1;
    }

    /// <summary>
    /// Sets or adds a vertex color to the color List.
    /// <para>If [index] is less than [Count], the existing vertex at [index] will be modified.</para>
    /// <para>If [index] equals [Count], a new vertex is appended to the end of the vertex list.</para> 
    /// <para>If [index] is larger than [Count], the function will return false.</para>
    /// </summary>
    /// <param name="index">Index of vertex color to set. 
    /// If index equals Count, then the color will be appended.</param>
    /// <param name="red">Red component of vertex color. Value must be in the 0~255 range.</param>
    /// <param name="green">Green component of vertex color. Value must be in the 0~255 range.</param>
    /// <param name="blue">Blue component of vertex color. Value must be in the 0~255 range.</param>
    /// <returns>true on success, false on failure.</returns>
    public bool SetColor(int index, int red, int green, int blue)
    {
      return SetColor(index, Color.FromArgb(red, green, blue));
    }
    /// <summary>
    /// Sets or adds a vertex to the Vertex List.
    /// <para>If [index] is less than [Count], the existing vertex at [index] will be modified.</para>
    /// <para>If [index] equals [Count], a new vertex is appended to the end of the vertex list.</para> 
    /// <para>If [index] is larger than [Count], the function will return false.</para>
    /// </summary>
    /// <param name="index">Index of vertex color to set. 
    /// If index equals Count, then the color will be appended.</param>
    /// <param name="color">Color to set, Alpha channels will be ignored.</param>
    /// <returns>true on success, false on failure.</returns>
    public bool SetColor(int index, Color color)
    {
      if (index < 0 || index > Count)
      {
        throw new IndexOutOfRangeException();
      }

      IntPtr ptr_mesh = m_mesh.NonConstPointer();
      return UnsafeNativeMethods.ON_Mesh_SetColor(ptr_mesh, index, color.ToArgb());
    }

    /// <summary>
    /// Sets a color at the three or four vertex indices of a specified face.
    /// </summary>
    /// <param name="face">A face to use to retrieve indices.</param>
    /// <param name="color">A color.</param>
    /// <returns>true on success; false on error.</returns>
    public bool SetColor(MeshFace face, Color color)
    {
      return SetColor(face.A, color) &&
        SetColor(face.B, color) &&
        SetColor(face.C, color) &&
        SetColor(face.D, color);
    }

    /// <summary>
    /// Gets or sets the total number of vertex colors the internal data structure can hold without resizing.
    /// </summary>
    public int Capacity
    {
      get
      {
        IntPtr const_ptr_mesh = m_mesh.ConstPointer();
        return UnsafeNativeMethods.ON_Mesh_GetInt(const_ptr_mesh, UnsafeNativeMethods.MeshIntConst.ColorCapacity);
      }
      set
      {
        if (Capacity != value)
        {
          if (value < Count)
            throw new ArgumentOutOfRangeException("value", "Capacity must be larger than or equal to the list Count");

          IntPtr ptr_mesh = m_mesh.NonConstPointer();
          UnsafeNativeMethods.ON_Mesh_SetInt(ptr_mesh, UnsafeNativeMethods.MeshIntConst.ColorCapacity, value);
        }
      }
    }
#endregion

#region methods
    private bool SetColorsHelper(Color[] colors, bool append)
    {
      if (colors == null) { return false; }

      IntPtr ptr_mesh = m_mesh.NonConstPointer();

      int count = colors.Length;
      var argb = new int[count];

      for (int i = 0; i < count; i++)
        argb[i] = colors[i].ToArgb();

      return UnsafeNativeMethods.ON_Mesh_SetVertexColors(ptr_mesh, count, argb, append);
    }

    /// <summary>
    /// Constructs a valid vertex color list consisting of a single color.
    /// </summary>
    /// <param name="baseColor">Color to apply to every vertex.</param>
    /// <returns>true on success, false on failure.</returns>
    public bool CreateMonotoneMesh(Color baseColor)
    {
      int count = m_mesh.Vertices.Count;
      var colors = new Color[count];

      for (int i = 0; i < count; i++)
        colors[i] = baseColor;

      return SetColors(colors);
    }

    /// <summary>
    /// Sets all the vertex colors in one go. For the Mesh to be valid, the number 
    /// of colors must match the number of vertices.
    /// </summary>
    /// <param name="colors">Colors to set.</param>
    /// <returns>true on success, false on failure.</returns>
    /// <example>
    /// <code source='examples\vbnet\ex_analysismode.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_analysismode.cs' lang='cs'/>
    /// </example>
    public bool SetColors(Color[] colors)
    {
      return SetColorsHelper(colors, false);
    }

    /// <summary>
    /// Appends a collection of colors to the vertex color list. 
    /// For the Mesh to be valid, the number of colors must match the number of vertices.
    /// </summary>
    /// <param name="colors">Colors to append.</param>
    /// <returns>true on success, false on failure.</returns>
    public bool AppendColors(Color[] colors)
    {
      return SetColorsHelper(colors, true);
    }

    /// <summary>
    /// Releases all memory allocated to store vertex colors. The list capacity will be 0 after this call.
    /// <para>Subsequent calls can add new items.</para>
    /// </summary>
    public void Destroy()
    {
      IntPtr ptr_mesh = m_mesh.NonConstPointer();
      UnsafeNativeMethods.ON_Mesh_SetInt(ptr_mesh, UnsafeNativeMethods.MeshIntConst.ColorCapacity, 0);
    }
#endregion

#region IResizableList<Point3f>, IList and related implementations
    int IList<Color>.IndexOf(Color item)
    {
      return IndexOfHelper(item);
    }

    private int IndexOfHelper(Color item)
    {
      int count = Count;
      for (int i = 0; i < count; i++)
      {
        if (item.Equals(this[i])) return i;
      }
      return -1;
    }

    void IList<Color>.Insert(int index, Color item)
    {
      GenericIListImplementation.Insert(this, index, item);
    }

    void IList<Color>.RemoveAt(int index)
    {
      GenericIListImplementation.RemoveAt(this, index);
    }

    void ICollection<Color>.Add(Color item)
    {
      Add(item);
    }

    bool ICollection<Color>.Contains(Color item)
    {
      return IndexOfHelper(item) != -1;
    }

    void ICollection<Color>.CopyTo(Color[] array, int arrayIndex)
    {
      GenericIListImplementation.CopyTo(this, array, arrayIndex);
    }

    bool ICollection<Color>.IsReadOnly
    {
      get { return false; }
    }

    bool ICollection<Color>.Remove(Color item)
    {
      return RemoveHelper(item);
    }

    private bool RemoveHelper(Color item)
    {
      var index = IndexOfHelper(item);

      bool found = index != -1;
      if (found) GenericIListImplementation.RemoveAt(this, index);

      return found;
    }

    const string EXCEPTION_VALUE_NOT_COLOR = "Input to the method must be a color.";

    int IList.Add(object value)
    {
      if (!(value is Color)) throw new ArgumentException(EXCEPTION_VALUE_NOT_COLOR, "value");

      return Add((Color)value);
    }

    bool IList.Contains(object value)
    {
      if (!(value is Color)) throw new ArgumentException(EXCEPTION_VALUE_NOT_COLOR, "value");

      return IndexOfHelper((Color)value) != -1;
    }

    int IList.IndexOf(object value)
    {
      if (!(value is Color)) throw new ArgumentException(EXCEPTION_VALUE_NOT_COLOR, "value");

      return IndexOfHelper((Color)value);
    }

    void IList.Insert(int index, object value)
    {
      if (!(value is Color)) throw new ArgumentException(EXCEPTION_VALUE_NOT_COLOR, "value");

      GenericIListImplementation.Insert(this, index, (Color)value);
    }

    bool IList.IsFixedSize
    {
      get { return false; }
    }

    bool IList.IsReadOnly
    {
      get { return false; }
    }

    void IList.Remove(object value)
    {
      if (!(value is Color)) throw new ArgumentException(EXCEPTION_VALUE_NOT_COLOR, "value");

      RemoveHelper((Color)value);
    }

    void IList.RemoveAt(int index)
    {
      GenericIListImplementation.RemoveAt(this, index);
    }

    object IList.this[int index]
    {
      get
      {
        return this[index];
      }
      set
      {
        if (!(value is Color)) throw new ArgumentException(EXCEPTION_VALUE_NOT_COLOR, "value");

        this[index] = (Color)value;
      }
    }

    void ICollection.CopyTo(Array array, int index)
    {
      GenericIListImplementation.CopyTo(this, array, index);
    }

    bool ICollection.IsSynchronized
    {
      get { return false; }
    }

    object ICollection.SyncRoot
    {
      get
      {
        throw GenericIListImplementation.MakeNoSyncException(this);
      }
    }

    /// <summary>
    /// Gets an enumerator that yields all colors in this collection.
    /// </summary>
    /// <returns>The enumerator.</returns>
    public IEnumerator<Color> GetEnumerator()
    {
      var count = Count;
      for (int i = 0; i < count; i++)
        yield return this[i];
    }
    IEnumerator IEnumerable.GetEnumerator()
    {
      return GetEnumerator();
    }
#endregion
  }

  /// <summary>
  /// Provides access to the Vertex Texture coordinates of a Mesh.
  /// </summary>
  public class MeshTextureCoordinateList : IResizableList<Point2f>, IList, IReadOnlyList<Point2f>
  {
    private readonly Mesh m_mesh;

#region constructors
    internal MeshTextureCoordinateList(Mesh ownerMesh)
    {
      m_mesh = ownerMesh;
    }
#endregion

#region properties
    /// <summary>
    /// Gets or sets the number of texture coordinates.
    /// </summary>
    public int Count
    {
      get
      {
        IntPtr const_ptr_mesh = m_mesh.ConstPointer();
        return UnsafeNativeMethods.ON_Mesh_GetInt(const_ptr_mesh, UnsafeNativeMethods.MeshIntConst.TextureCoordinateCount);
      }
      set
      {
        if (value >= 0 && Count != value)
        {
          IntPtr ptr_mesh = m_mesh.NonConstPointer();
          UnsafeNativeMethods.ON_Mesh_SetInt(ptr_mesh, UnsafeNativeMethods.MeshIntConst.TextureCoordinateCount, value);
        }
      }
    }

    /// <summary>
    /// Gets or sets the total number of texture coordinates the internal data structure can hold without resizing.
    /// </summary>
    public int Capacity
    {
      get
      {
        IntPtr const_ptr_mesh = m_mesh.ConstPointer();
        return UnsafeNativeMethods.ON_Mesh_GetInt(const_ptr_mesh, UnsafeNativeMethods.MeshIntConst.TextureCoordinateCapacity);
      }
      set
      {
        if (Capacity != value)
        {
          if (value < Count)
            throw new ArgumentOutOfRangeException("value", "Capacity must be larger than or equal to the list Count");

          IntPtr ptr_mesh = m_mesh.NonConstPointer();
          UnsafeNativeMethods.ON_Mesh_SetInt(ptr_mesh, UnsafeNativeMethods.MeshIntConst.TextureCoordinateCapacity, value);
        }
      }
    }

    /// <summary>
    /// Gets or sets the texture coordinate at the given index. 
    /// The index must be valid or an IndexOutOfRangeException will be thrown.
    /// </summary>
    /// <param name="index">Index of texture coordinates to access.</param>
    /// <exception cref="IndexOutOfRangeException">Thrown when the index is invalid.</exception>
    /// <returns>The texture coordinate at [index].</returns>
    public Point2f this[int index]
    {
      get
      {
        if (index < 0 || index >= Count)
          throw new IndexOutOfRangeException();

        IntPtr const_ptr_mesh = m_mesh.ConstPointer();
        float s = 0;
        float t = 0;
        if (!UnsafeNativeMethods.ON_Mesh_GetTextureCoordinate(const_ptr_mesh, index, ref s, ref t)) { return Point2f.Unset; }
        return new Point2f(s, t);
      }
      set
      {
        if (index < 0 || index >= Count)
          throw new IndexOutOfRangeException();

        IntPtr ptr_mesh = m_mesh.NonConstPointer();
        UnsafeNativeMethods.ON_Mesh_SetTextureCoordinate(ptr_mesh, index, value.m_x, value.m_y);
      }
    }

#endregion

#region access
    /// <summary>
    /// Clears the Texture Coordinate list on the mesh.
    /// </summary>
    public void Clear()
    {
      IntPtr ptr_mesh = m_mesh.NonConstPointer();
      UnsafeNativeMethods.ON_Mesh_ClearList(ptr_mesh, UnsafeNativeMethods.MeshClearListConst.ClearTextureCoordinates);
    }

    /// <summary>
    /// Adds a new texture coordinate to the end of the Texture list.
    /// </summary>
    /// <param name="s">S component of new texture coordinate.</param>
    /// <param name="t">T component of new texture coordinate.</param>
    /// <returns>The index of the newly added texture coordinate.</returns>
    public int Add(float s, float t)
    {
      int n = Count;
      if (!SetTextureCoordinate(n, new Point2f(s, t)))
        return -1;
      return n;
    }
    /// <summary>
    /// Adds a new texture coordinate to the end of the Texture list.
    /// </summary>
    /// <param name="s">S component of new texture coordinate.</param>
    /// <param name="t">T component of new texture coordinate.</param>
    /// <returns>The index of the newly added texture coordinate.</returns>
    public int Add(double s, double t)
    {
      return Add((float)s, (float)t);
    }
    /// <summary>
    /// Adds a new texture coordinate to the end of the Texture list.
    /// </summary>
    /// <param name="tc">Texture coordinate to add.</param>
    /// <returns>The index of the newly added texture coordinate.</returns>
    public int Add(Point2f tc)
    {
      return Add(tc.m_x, tc.m_y);
    }
    /// <summary>
    /// Adds a new texture coordinate to the end of the Texture list.
    /// </summary>
    /// <param name="tc">Texture coordinate to add.</param>
    /// <returns>The index of the newly added texture coordinate.</returns>
    public int Add(Point3d tc)
    {
      return Add((float)tc.m_x, (float)tc.m_y);
    }
    /// <summary>
    /// Appends an array of texture coordinates.
    /// </summary>
    /// <param name="textureCoordinates">Texture coordinates to append.</param>
    /// <returns>true on success, false on failure.</returns>
    public bool AddRange(Point2f[] textureCoordinates)
    {
      return SetTextureCoordinatesHelper(textureCoordinates, true);
    }

    /// <summary>
    /// Sets or adds a texture coordinate to the Texture Coordinate List.
    /// <para>If [index] is less than [Count], the existing coordinate at [index] will be modified.</para>
    /// <para>If [index] equals [Count], a new coordinate is appended to the end of the coordinate list.</para> 
    /// <para>If [index] is larger than [Count], the function will return false.</para>
    /// </summary>
    /// <param name="index">Index of texture coordinate to set.</param>
    /// <param name="s">S component of texture coordinate.</param>
    /// <param name="t">T component of texture coordinate.</param>
    /// <returns>true on success, false on failure.</returns>
    public bool SetTextureCoordinate(int index, float s, float t)
    {
      IntPtr ptr_mesh = m_mesh.NonConstPointer();
      return UnsafeNativeMethods.ON_Mesh_SetTextureCoordinate(ptr_mesh, index, s, t);
    }
    /// <summary>
    /// Sets or adds a texture coordinate to the Texture Coordinate List.
    /// <para>If [index] is less than [Count], the existing coordinate at [index] will be modified.</para>
    /// <para>If [index] equals [Count], a new coordinate is appended to the end of the coordinate list.</para> 
    /// <para>If [index] is larger than [Count], the function will return false.</para>
    /// </summary>
    /// <param name="index">Index of texture coordinate to set.</param>
    /// <param name="s">S component of texture coordinate.</param>
    /// <param name="t">T component of texture coordinate.</param>
    /// <returns>true on success, false on failure.</returns>
    public bool SetTextureCoordinate(int index, double s, double t)
    {
      return SetTextureCoordinate(index, (float)s, (float)t);
    }
    /// <summary>
    /// Sets or adds a texture coordinate to the Texture Coordinate List.
    /// <para>If [index] is less than [Count], the existing coordinate at [index] will be modified.</para>
    /// <para>If [index] equals [Count], a new coordinate is appended to the end of the coordinate list.</para> 
    /// <para>If [index] is larger than [Count], the function will return false.</para>
    /// </summary>
    /// <param name="index">Index of texture coordinate to set.</param>
    /// <param name="tc">Texture coordinate point.</param>
    /// <returns>true on success, false on failure.</returns>
    public bool SetTextureCoordinate(int index, Point2f tc)
    {
      return SetTextureCoordinate(index, tc.m_x, tc.m_y);
    }
    /// <summary>
    /// Sets or adds a texture coordinate to the Texture Coordinate List.
    /// <para>If [index] is less than [Count], the existing coordinate at [index] will be modified.</para>
    /// <para>If [index] equals [Count], a new coordinate is appended to the end of the coordinate list.</para> 
    /// <para>If [index] is larger than [Count], the function will return false.</para>
    /// </summary>
    /// <param name="index">Index of texture coordinate to set.</param>
    /// <param name="tc">Texture coordinate point.</param>
    /// <returns>true on success, false on failure.</returns>
    public bool SetTextureCoordinate(int index, Point3f tc)
    {
      return SetTextureCoordinate(index, tc.m_x, tc.m_y);
    }
    /// <summary>
    /// Sets all texture coordinates in one go.
    /// </summary>
    /// <param name="textureCoordinates">Texture coordinates to assign to the mesh.</param>
    /// <returns>true on success, false on failure.</returns>
    public bool SetTextureCoordinates(Point2f[] textureCoordinates)
    {
      return SetTextureCoordinatesHelper(textureCoordinates, false);
    }

    private bool SetTextureCoordinatesHelper(Point2f[] textureCoordinates, bool append)
    {
      IntPtr ptr_mesh = m_mesh.NonConstPointer();
      if (textureCoordinates == null)
        return false;
      return UnsafeNativeMethods.ON_Mesh_SetTextureCoordinates(ptr_mesh, textureCoordinates.Length, textureCoordinates, append);
    }

    /// <summary>
    /// Set all texture coordinates based on a texture mapping function
    /// </summary>
    /// <param name="mapping">The new mapping type.</param>
    /// <returns>true on success, false on failure.</returns>
    public bool SetTextureCoordinates(TextureMapping mapping)
    {
      IntPtr ptr_mesh = m_mesh.NonConstPointer();
      IntPtr const_ptr_mapping = mapping.ConstPointer();
      return UnsafeNativeMethods.ON_Mesh_SetTextureCoordinates2(ptr_mesh, const_ptr_mapping);
    }

    /// <summary>
    /// Releases all memory allocated to store texture coordinates. The list capacity will be 0 after this call.
    /// <para>Subsequent calls can add new items.</para>
    /// </summary>
    public void Destroy()
    {
      IntPtr ptr_mesh = m_mesh.NonConstPointer();
      UnsafeNativeMethods.ON_Mesh_SetInt(ptr_mesh, UnsafeNativeMethods.MeshIntConst.TextureCoordinateCapacity, 0);
    }
#endregion

#region methods
    /// <summary>
    /// Scales the texture coordinates so the texture domains are [0,1] 
    /// and eliminate any texture rotations.
    /// </summary>
    /// <returns>true on success, false on failure.</returns>
    public bool NormalizeTextureCoordinates()
    {
      IntPtr ptr = m_mesh.NonConstPointer();
      return UnsafeNativeMethods.ON_Mesh_NonConstBoolOp(ptr, UnsafeNativeMethods.MeshNonConstBoolConst.NormalizeTextureCoordinates);
    }
    /// <summary>
    /// Transposes texture coordinates.
    /// <para>The region of the bitmap the texture uses does not change.
    /// All texture coordinates rows (Us) become columns (Vs), and vice versa.</para>
    /// </summary>
    /// <returns>true on success, false on failure.</returns>
    public bool TransposeTextureCoordinates()
    {
      IntPtr ptr = m_mesh.NonConstPointer();
      return UnsafeNativeMethods.ON_Mesh_NonConstBoolOp(ptr, UnsafeNativeMethods.MeshNonConstBoolConst.TransposeTextureCoordinates);
    }
    /// <summary>
    /// Reverses one coordinate direction of the texture coordinates.
    /// <para>The region of the bitmap the texture uses does not change.
    /// Either Us or Vs direction is flipped.</para>
    /// </summary>
    /// <param name="direction">
    /// <para>0 = first texture coordinate is reversed.</para>
    /// <para>1 = second texture coordinate is reversed.</para>
    /// </param>
    /// <returns>true if operation succeeded; otherwise, false.</returns>
    public bool ReverseTextureCoordinates(int direction)
    {
      IntPtr ptr = m_mesh.NonConstPointer();
      return UnsafeNativeMethods.ON_Mesh_Reverse(ptr, true, direction);
    }

    /// <summary>
    /// Copies all vertices to a linear array of float in u,v order
    /// </summary>
    /// <returns>The float array.</returns>
    public float[] ToFloatArray()
    {
      var count = Count;
      var rc = new float[count * 2];
      var const_ptr_mesh = m_mesh.ConstPointer();
      var index = 0;
      // There is a much more efficient way to do this with
      // marshalling the whole array at once, but this will
      // do for now
      for (var i = 0; i < count; i++)
      {
        var s = 0.0f;
        var t = 0.0f;
        UnsafeNativeMethods.ON_Mesh_GetTextureCoordinate(const_ptr_mesh, i, ref s, ref t);
        rc[index++] = s;
        rc[index++] = t;
      }
      return rc;
    }
#endregion

#region IResizableList<Point2f>, IList and related implementations
    int IList<Point2f>.IndexOf(Point2f item)
    {
      return GenericIListImplementation.IndexOf(this, item);
    }

    void IList<Point2f>.Insert(int index, Point2f item)
    {
      GenericIListImplementation.Insert(this, index, item);
    }

    void IList<Point2f>.RemoveAt(int index)
    {
      GenericIListImplementation.RemoveAt(this, index);
    }

    void ICollection<Point2f>.Add(Point2f item)
    {
      Add(item);
    }

    bool ICollection<Point2f>.Contains(Point2f item)
    {
      return GenericIListImplementation.IndexOf(this, item) != -1;
    }

    void ICollection<Point2f>.CopyTo(Point2f[] array, int arrayIndex)
    {
      GenericIListImplementation.CopyTo(this, array, arrayIndex);
    }

    bool ICollection<Point2f>.IsReadOnly
    {
      get { return false; }
    }

    bool ICollection<Point2f>.Remove(Point2f item)
    {
      return GenericIListImplementation.Remove(this, item);
    }

    int IList.Add(object value)
    {
      var item = GenericIListImplementation.HelperCoercePoint2(value);
      return Add(item);
    }

    bool IList.Contains(object value)
    {
      Point2f item;
      if (!GenericIListImplementation.HelperTryCoerce(value, out item)) return false;

      return GenericIListImplementation.Contains(this, item);
    }

    int IList.IndexOf(object value)
    {
      Point2f item;
      if (!GenericIListImplementation.HelperTryCoerce(value, out item)) return -1;

      return GenericIListImplementation.IndexOf(this, item);
    }

    void IList.Insert(int index, object value)
    {
      var item = GenericIListImplementation.HelperCoercePoint2(value);
      GenericIListImplementation.Insert(this, index, item);
    }

    bool IList.IsFixedSize
    {
      get { return false; }
    }

    bool IList.IsReadOnly
    {
      get { return false; }
    }

    void IList.Remove(object value)
    {
      var item = GenericIListImplementation.HelperCoercePoint2(value);
      GenericIListImplementation.Remove(this, item);
    }

    void IList.RemoveAt(int index)
    {
      GenericIListImplementation.RemoveAt(this, index);
    }

    object IList.this[int index]
    {
      get
      {
        return this[index];
      }
      set
      {
        this[index] = GenericIListImplementation.HelperCoercePoint2(value);
      }
    }

    void ICollection.CopyTo(Array array, int index)
    {
      GenericIListImplementation.CopyTo(this, array, index);
    }

    bool ICollection.IsSynchronized
    {
      get { return false; }
    }

    object ICollection.SyncRoot
    {
      get
      {
        throw GenericIListImplementation.MakeNoSyncException(this);
      }
    }

    /// <summary>
    /// Gets an enumerator that yields all texture coordinates in this collection.
    /// </summary>
    /// <returns>The enumerator.</returns>
    public IEnumerator<Point2f> GetEnumerator()
    {
      var count = Count;
      for (int i = 0; i < count; i++)
        yield return this[i];
    }
    IEnumerator IEnumerable.GetEnumerator()
    {
      return GetEnumerator();
    }
#endregion
  }

  /// <summary>
  /// Provides access to status information relative to components of a mesh.
  /// </summary>
  public class MeshVertexStatusList : IResizableList<bool>, IList, IReadOnlyList<bool>
  {
    private readonly Mesh m_mesh;

#region constructors
    internal MeshVertexStatusList(Mesh ownerMesh)
    {
      m_mesh = ownerMesh;
    }
#endregion

#region properties
    /// <summary>
    /// Gets or sets the number of hidden vertices. For this to be a valid part of a mesh, this count should be the same as the one of mesh vertices.
    /// </summary>
    public int Count
    {
      get
      {
        IntPtr const_ptr_mesh = m_mesh.ConstPointer();
        return UnsafeNativeMethods.ON_Mesh_GetInt(const_ptr_mesh, UnsafeNativeMethods.MeshIntConst.HiddenVertexCount);
      }
      set
      {
        if (value >= 0 && Count != value)
        {
          IntPtr ptr_mesh = m_mesh.NonConstPointer();
          UnsafeNativeMethods.ON_Mesh_SetInt(ptr_mesh, UnsafeNativeMethods.MeshIntConst.HiddenVertexCount, value);
        }
      }
    }

    /// <summary>
    /// Gets a value indicating how many vertices have been set to hidden.
    /// </summary>
    public int HiddenCount
    {
      get
      {
        IntPtr const_ptr_mesh = m_mesh.ConstPointer();
        return UnsafeNativeMethods.ON_Mesh_GetInt(const_ptr_mesh, UnsafeNativeMethods.MeshIntConst.HiddenVertexHiddenCount);
      }
    }

    /// <summary>
    /// Gets or sets the total number of hidden vertex information the internal data structure can hold without resizing.
    /// </summary>
    public int Capacity
    {
      get
      {
        IntPtr const_ptr_mesh = m_mesh.ConstPointer();
        return UnsafeNativeMethods.ON_Mesh_GetInt(const_ptr_mesh, UnsafeNativeMethods.MeshIntConst.HiddenVertexCapacity);
      }
      set
      {
        if (Capacity != value)
        {
          if (value < Count)
            throw new ArgumentOutOfRangeException("value", "Capacity must be larger than or equal to the list Count");

          IntPtr ptr_mesh = m_mesh.NonConstPointer();
          UnsafeNativeMethods.ON_Mesh_SetInt(ptr_mesh, UnsafeNativeMethods.MeshIntConst.HiddenVertexCapacity, value);
        }
      }
    }

    /// <summary>
    /// Gets or sets the hidden value at the given index. 
    /// The index must be valid or an IndexOutOfRangeException will be thrown.
    /// </summary>
    /// <param name="index">Index of hidden flag to access.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the index is invalid.</exception>
    /// <returns>The hidden flag at [index].</returns>
    public bool this[int index]
    {
      get
      {
        if (index < 0 || index >= Count)
          throw new ArgumentOutOfRangeException("index");

        IntPtr const_ptr_mesh = m_mesh.ConstPointer();
        bool h = false;
        if (!UnsafeNativeMethods.ON_Mesh_GetHiddenVertexFlag(const_ptr_mesh, index, ref h)) { return false; }
        return h;
      }
      set
      {
        if (index < 0 || index >= Count)
          throw new ArgumentOutOfRangeException("index");

        IntPtr ptr_mesh = m_mesh.NonConstPointer();
        UnsafeNativeMethods.ON_Mesh_SetHiddenVertexFlag(ptr_mesh, index, value);
      }
    }

#endregion

#region access
    /// <summary>
    /// Clears the hidden vertex list on the mesh. This results in a fully visible mesh.
    /// </summary>
    public void Clear()
    {
      IntPtr ptr_mesh = m_mesh.NonConstPointer();
      UnsafeNativeMethods.ON_Mesh_ClearList(ptr_mesh, UnsafeNativeMethods.MeshClearListConst.ClearHiddenVertices);
    }

    /// <summary>
    /// Adds a new flag at the end of the list.
    /// </summary>
    /// <param name="hidden">True if vertex is hidden.</param>
    /// <returns>The index of the newly added hidden vertex.</returns>
    public void Add(bool hidden)
    {
      int n = Count;
      var ptr_mesh = m_mesh.NonConstPointer();
      UnsafeNativeMethods.ON_Mesh_SetHiddenVertexFlag(ptr_mesh, n, hidden);
    }
    /// <summary>
    /// Appends an array, a list or any enumerable of flags to the end of the list.
    /// </summary>
    /// <param name="values">Hidden values to append.</param>
    /// <returns>true on success, false on failure.</returns>
    public void AddRange(IEnumerable<bool> values)
    {
      foreach (var value in values)
      {
        Add(value);
      }
    }

    /// <summary>
    /// Releases all memory allocated to store hidden vertices. The list capacity will be 0 after this call.
    /// <para>Vertices will be immediately considered visible.</para>
    /// <para>Subsequent calls can add new items.</para>
    /// </summary>
    public void Destroy()
    {
      IntPtr ptr_mesh = m_mesh.NonConstPointer();
      UnsafeNativeMethods.ON_Mesh_SetInt(ptr_mesh, UnsafeNativeMethods.MeshIntConst.HiddenVertexCapacity, 0);
    }
#endregion

#region IResizableList<Point2f>, IList and related implementations
    int IList<bool>.IndexOf(bool item)
    {
      return GenericIListImplementation.IndexOf(this, item);
    }

    void IList<bool>.Insert(int index, bool item)
    {
      GenericIListImplementation.Insert(this, index, item);
    }

    void IList<bool>.RemoveAt(int index)
    {
      GenericIListImplementation.RemoveAt(this, index);
    }

    void ICollection<bool>.Add(bool item)
    {
      Add(item);
    }

    /// <summary>
    /// Determines if some vertices are hidden or some are shown.
    /// </summary>
    /// <param name="hidden">The value to be checked. True means some vertex is hidden.</param>
    /// <returns>True if the array contains the specified value.</returns>
    public bool Contains(bool hidden)
    {
      return hidden ? HiddenCount > 0 : HiddenCount == 0;
    }

    /// <summary>
    /// Copies to an array, starting at an index.
    /// </summary>
    /// <param name="array">The array to be copied into.</param>
    /// <param name="arrayIndex">The starting index in the array.</param>
    public void CopyTo(bool[] array, int arrayIndex)
    {
      GenericIListImplementation.CopyTo(this, array, arrayIndex);
    }

    bool ICollection<bool>.IsReadOnly
    {
      get { return false; }
    }

    bool ICollection<bool>.Remove(bool item)
    {
      return GenericIListImplementation.Remove(this, item);
    }

    int IList.Add(object value)
    {
      var value_bool = value is bool;

      if (value_bool)
      {
        Add((bool)value);
        return Count - 1;
      }
      return -1;
    }

    bool IList.Contains(object value)
    {
      if (!(value is bool)) return false;

      return Contains((bool)value);
    }

    int IList.IndexOf(object value)
    {
      if (!(value is bool)) return -1;

      return GenericIListImplementation.IndexOf(this, (bool)value);
    }

    void IList.Insert(int index, object value)
    {
      if (!(value is bool)) throw new ArgumentException("value must be a Boolean", "value");

      GenericIListImplementation.Insert(this, index, (bool)value);
    }

    bool IList.IsFixedSize
    {
      get { return false; }
    }

    bool IList.IsReadOnly
    {
      get { return false; }
    }

    void IList.Remove(object value)
    {
      if (!(value is bool)) throw new ArgumentException("value must be a Boolean", "value");

      GenericIListImplementation.Remove(this, (bool)value);
    }

    void IList.RemoveAt(int index)
    {
      GenericIListImplementation.RemoveAt(this, index);
    }

    object IList.this[int index]
    {
      get
      {
        return this[index];
      }
      set
      {
        if (!(value is bool)) throw new ArgumentException("value must be a Boolean", "value");

        this[index] = (bool)(value);
      }
    }

    void ICollection.CopyTo(Array array, int index)
    {
      GenericIListImplementation.CopyTo(this, array, index);
    }

    bool ICollection.IsSynchronized
    {
      get { return false; }
    }

    object ICollection.SyncRoot
    {
      get
      {
        throw GenericIListImplementation.MakeNoSyncException(this);
      }
    }

    /// <summary>
    /// Gets an enumerator that yields all flags in this collection.
    /// </summary>
    /// <returns>The enumerator.</returns>
    public IEnumerator<bool> GetEnumerator()
    {
      var count = Count;
      for (int i = 0; i < count; i++)
        yield return this[i];
    }
    IEnumerator IEnumerable.GetEnumerator()
    {
      return GetEnumerator();
    }
#endregion
  }
}

//class ON_CLASS ON_MeshVertexRef : public ON_Geometry
//{
//  ON_OBJECT_DECLARE(ON_MeshVertexRef);
//public:
//  ON_MeshVertexRef();
//  ~ON_MeshVertexRef();
//  ON_MeshVertexRef& operator=(const ON_MeshVertexRef&);


//  // parent mesh
//  const ON_Mesh* m_mesh;

//  // m_mesh->m_V[] index
//  // (can be -1 when m_top_vi references a shared vertex location)
//  int m_mesh_vi; 

//  // m_mesh->m_top.m_tope[] index
//  int m_top_vi; 


//  /*
//  Description:
//    Override of the virtual ON_Geometry::ComponentIndex().
//  Returns:
//    A component index for the vertex.  The type of the returned
//    component index can be 
//    ON_ComponentIndex::mesh_vertex, 
//    ON_ComponentIndex::meshtop_vertex, or
//    ON_ComponentIndex::invalid_type.
//  */
//  ON_ComponentIndex ComponentIndex() const;

//  /*
//  Returns:
//    The mesh topology associated with this 
//    mesh vertex reference or NULL if it doesn't
//    exist.
//  */
//  const ON_MeshTopology* MeshTopology() const;

//  /*
//  Returns:
//    The 3d location of the mesh vertex.  Returns
//    ON_UNSET_POINT is this ON_MeshVertexRef is not 
//    valid.
//  */
//  ON_3dPoint Point() const;

//  /*
//  Returns:
//    The mesh topology vertex associated with this 
//    mesh vertex reference.
//  */
//  const ON_MeshTopologyVertex* MeshTopologyVertex() const;

//  // overrides of virtual ON_Object functions
//  BOOL IsValid( ON_TextLog* text_log = NULL ) const;
//  void Dump( ON_TextLog& ) const;
//  unsigned int SizeOf() const;
//  ON::ObjectType ObjectType() const;

//  // overrides of virtual ON_Geometry functions
//  int Dimension() const;
//  BOOL GetBBox(
//         double* boxmin,
//         double* boxmax,
//         int bGrowBox = false
//         ) const;
//  BOOL Transform( 
//         const ON_Xform& xform
//         );
//};

//class ON_CLASS ON_MeshEdgeRef : public ON_Geometry
//{
//  ON_OBJECT_DECLARE(ON_MeshEdgeRef);
//public:
//  ON_MeshEdgeRef();
//  ~ON_MeshEdgeRef();
//  ON_MeshEdgeRef& operator=(const ON_MeshEdgeRef&);

//  // parent mesh
//  const ON_Mesh* m_mesh;

//  // m_mesh->m_top.m_tope[] index
//  int m_top_ei; 

//  /*
//  Description:
//    Override of the virtual ON_Geometry::ComponentIndex().
//  Returns:
//    A mesh component index for the edge.  The type is
//    ON_ComponentIndex::meshtop_edge and the index is the
//    index into the ON_MeshTopology.m_tope[] array.
//  */
//  ON_ComponentIndex ComponentIndex() const;

//  /*
//  Returns:
//    The mesh topology associated with this 
//    mesh edge reference or NULL if it doesn't
//    exist.
//  */

//  const ON_MeshTopology* MeshTopology() const;
//  /*
//  Returns:
//    The 3d location of the mesh edge.  Returns
//    ON_UNSET_POINT,ON_UNSET_POINT, is this ON_MeshEdgeRef
//    is not valid.
//  */
//  ON_Line Line() const;

//  /*
//  Returns:
//    The mesh topology edge associated with this 
//    mesh edge reference.
//  */
//  const ON_MeshTopologyEdge* MeshTopologyEdge() const;

//  // overrides of virtual ON_Object functions
//  BOOL IsValid( ON_TextLog* text_log = NULL ) const;
//  void Dump( ON_TextLog& ) const;
//  unsigned int SizeOf() const;
//  ON::ObjectType ObjectType() const;

//  // overrides of virtual ON_Geometry functions
//  int Dimension() const;
//  BOOL GetBBox(
//         double* boxmin,
//         double* boxmax,
//         int bGrowBox = false
//         ) const;
//  BOOL Transform( 
//         const ON_Xform& xform
//         );
//};

//class ON_CLASS ON_MeshFaceRef : public ON_Geometry
//{
//  ON_OBJECT_DECLARE(ON_MeshFaceRef);
//public:
//  ON_MeshFaceRef();
//  ~ON_MeshFaceRef();
//  ON_MeshFaceRef& operator=(const ON_MeshFaceRef&);

//  // parent mesh
//  const ON_Mesh* m_mesh;

//  // m_mesh->m_F[] and m_mesh->m_top.m_tope[] index.
//  int m_mesh_fi; 

//  /*
//  Description:
//    Override of the virtual ON_Geometry::ComponentIndex().
//  Returns:
//    A mesh component index for the face.  The type is
//    ON_ComponentIndex::mesh_face and the index is the
//    index into the ON_Mesh.m_F[] array.
//  */
//  ON_ComponentIndex ComponentIndex() const;

//  /*
//  Returns:
//    The mesh topology associated with this 
//    mesh face reference or NULL if it doesn't
//    exist.
//  */
//  const ON_MeshTopology* MeshTopology() const;

//  /*
//  Returns:
//    The mesh face associated with this mesh face reference.
//  */
//  const ON_MeshFace* MeshFace() const;

//  /*
//  Returns:
//    The mesh topology face associated with this 
//    mesh face reference.
//  */
//  const ON_MeshTopologyFace* MeshTopologyFace() const;

//  // overrides of virtual ON_Object functions
//  BOOL IsValid( ON_TextLog* text_log = NULL ) const;
//  void Dump( ON_TextLog& ) const;
//  unsigned int SizeOf() const;
//  ON::ObjectType ObjectType() const;

//  // overrides of virtual ON_Geometry functions
//  int Dimension() const;
//  BOOL GetBBox(
//         double* boxmin,
//         double* boxmax,
//         int bGrowBox = false
//         ) const;
//  BOOL Transform( 
//         const ON_Xform& xform
//         );
//};

//*
//Description:
//  Calculate a quick and dirty polygon mesh approximation
//  of a surface.
//Parameters:
//  surface - [in]
//  mesh_density - [in] If <= 10, this number controls
//        the relative polygon count.  If > 10, this number
//        specifies a target number of polygons.
//  mesh - [in] if not NULL, the polygon mesh will be put
//              on this mesh.
//Returns:
//  A polygon mesh approximation of the surface or NULL
//  if the surface could not be meshed.
//*/
//ON_DECL
//ON_Mesh* ON_MeshSurface( 
//            const ON_Surface& surface, 
//            int mesh_density = 0,
//            ON_Mesh* mesh = 0
//            );

//*
//Description:
//  Calculate a quick and dirty polygon mesh approximation
//  of a surface.
//Parameters:
//  surface - [in]
//  u_count - [in] >= 2 Number of "u" parameters in u[] array.
//  u       - [in] u parameters
//  v_count - [in] >= 2 Number of "v" parameters in v[] array.
//  v       - [in] v parameters
//  mesh - [in] if not NULL, the polygon mesh will be put
//              on this mesh.
//Returns:
//  A polygon mesh approximation of the surface or NULL
//  if the surface could not be meshed.
//*/
//ON_DECL
//ON_Mesh* ON_MeshSurface( 
//            const ON_Surface& surface, 
//            int u_count,
//            const double* u,
//            int v_count,
//            const double* v,
//            ON_Mesh* mesh = 0
//            );

//*
//Description:
//  Finds the barycentric coordinates of the point on a 
//  triangle that is closest to P.
//Parameters:
//  A - [in] triangle corner
//  B - [in] triangle corner
//  C - [in] triangle corner
//  P - [in] point to test
//  a - [out] barycentric coordinate
//  b - [out] barycentric coordinate
//  c - [out] barycentric coordinate
//        If ON_ClosestPointToTriangle() returns true, then
//        (*a)*A + (*b)*B + (*c)*C is the point on the 
//        triangle's plane that is closest to P.  It is 
//        always the case that *a + *b + *c = 1, but this
//        function will return negative barycentric 
//        coordinate if the point on the plane is not
//        inside the triangle.
//Returns:
//  True if the triangle is not degenerate.  False if the
//  triangle is degenerate; in this case the returned
//  closest point is the input point that is closest to P.
//*/
//ON_DECL
//bool ON_ClosestPointToTriangle( 
//        ON_3dPoint A, ON_3dPoint B, ON_3dPoint C,
//        ON_3dPoint P,
//        double* a, double* b, double* c
//        );


//*
//Description:
//  Finds the barycentric coordinates of the point on a 
//  triangle that is closest to P.
//Parameters:
//  A - [in] triangle corner
//  B - [in] triangle corner
//  C - [in] triangle corner
//  P - [in] point to test
//  a - [out] barycentric coordinate
//  b - [out] barycentric coordinate
//  c - [out] barycentric coordinate
//        If ON_ClosestPointToTriangle() returns true, then
//        (*a)*A + (*b)*B + (*c)*C is the point on the 
//        triangle's plane that is closest to P.  It is 
//        always the case that *a + *b + *c = 1, but this
//        function will return negative barycentric 
//        coordinate if the point on the plane is not
//        inside the triangle.
//Returns:
//  True if the triangle is not degenerate.  False if the
//  triangle is degenerate; in this case the returned
//  closest point is the input point that is closest to P.
//*/
//ON_DECL
//bool ON_ClosestPointToTriangleFast( 
//          const ON_3dPoint& A, 
//          const ON_3dPoint& B, 
//          const ON_3dPoint& C, 
//          ON_3dPoint P,
//          double* a, double* b, double* c
//          );


//*
//Description:
//  Calculate a mesh representation of the NURBS surface's control polygon.
//Parameters:
//  nurbs_surface - [in]
//  bCleanMesh - [in] If true, then degenerate quads are cleaned
//                    up to be triangles. Surfaces with singular
//                    sides are a common source of degenerate qauds.
//  input_mesh - [in] If NULL, then the returned mesh is created
//       by a class to new ON_Mesh().  If not null, then this 
//       mesh will be used to store the conrol polygon.
//Returns:
//  If successful, a pointer to a mesh.
//*/
//ON_DECL
//ON_Mesh* ON_ControlPolygonMesh( 
//          const ON_NurbsSurface& nurbs_surface, 
//          bool bCleanMesh,
//          ON_Mesh* input_mesh = NULL
//          );

//*
//Description:
//  Finds the intersection between a line segment an a triangle.
//Parameters:
//  A - [in] triangle corner
//  B - [in] triangle corner
//  C - [in] triangle corner
//  P - [in] start of line segment
//  Q - [in] end of line segment
//  abc - [out] 
//     barycentric coordinates of intersection point(s)
//  t - [out] line coordinate of intersection point(s)
//Returns:
//  0 - no intersection
//  1 - one intersection point
//  2 - intersection segment
//*/
//ON_DECL
//int ON_LineTriangleIntersect(
//        const ON_3dPoint& A,
//        const ON_3dPoint& B,
//        const ON_3dPoint& C,
//        const ON_3dPoint& P,
//        const ON_3dPoint& Q,
//        double abc[2][3], 
//        double t[2],
//        double tol
//        );

//*
//Description:
//  Finds the unit normal to the triangle
//Parameters:
//  A - [in] triangle corner
//  B - [in] triangle corner
//  C - [in] triangle corner
//Returns:
//  Unit normal
//*/
//ON_DECL
//ON_3dVector ON_TriangleNormal(
//        const ON_3dPoint& A,
//        const ON_3dPoint& B,
//        const ON_3dPoint& C
//        );

//*
//Description:
//  Triangulate a 2D simple closed polygon.
//Parameters:
//  point_count - [in] number of points in polygon ( >= 3 )
//  point_stride - [in]
//  P - [in] 
//    i-th point = (P[i*point_stride], P[i*point_stride+1])
//  tri_stride - [in]
//  triangle - [out]
//    array of (point_count-2)*tri_stride integers
//Returns:
//  True if successful.  In this case, the polygon is trianglulated into 
//  point_count-2 triangles.  The indexes of the 3 points that are the 
//  corner of the i-th (0<= i < point_count-2) triangle are
//    (triangle[i*tri_stride], triangle[i*tri_stride+1], triangle[i*tri_stride+2]).
//Remarks:
//  Do NOT duplicate the start/end point; i.e., a triangle will have
//  a point count of 3 and P will specify 3 distinct non-collinear points.
//*/
//ON_DECL
//bool ON_Mesh2dPolygon( 
//          int point_count,
//          int point_stride,
//          const double* P,
//          int tri_stride,
//          int* triangle 
//          );

//#endif
namespace Rhino.Geometry
{
  /// <summary>
  /// Represents the values of the four indices of a mesh face quad.
  /// <para>If the third and fourth values are the same, this face represents a
  /// triangle.</para>
  /// </summary>
  [StructLayout(LayoutKind.Sequential, Pack = 4, Size = 16)]
  [DebuggerDisplay("{DebuggerDisplayUtil}")]
  [Serializable]
  public struct MeshFace : IEquatable<MeshFace>, IComparable<MeshFace>, IComparable
  {
#region members
    internal int m_a;
    internal int m_b;
    internal int m_c;
    internal int m_d;
#endregion

#region constructors
    /// <summary>
    /// Constructs a new triangular Mesh face.
    /// </summary>
    /// <param name="a">Index of first corner.</param>
    /// <param name="b">Index of second corner.</param>
    /// <param name="c">Index of third corner.</param>
    public MeshFace(int a, int b, int c)
    {
      m_a = a;
      m_b = b;
      m_c = c;
      m_d = c;
    }
    /// <summary>
    /// Constructs a new quadrangular Mesh face.
    /// </summary>
    /// <param name="a">Index of first corner.</param>
    /// <param name="b">Index of second corner.</param>
    /// <param name="c">Index of third corner.</param>
    /// <param name="d">Index of fourth corner.</param>
    public MeshFace(int a, int b, int c, int d)
    {
      m_a = a;
      m_b = b;
      m_c = c;
      m_d = d;
    }

    /// <summary>
    /// Gets an Unset MeshFace. Unset faces have Int32.MinValue for all corner indices.
    /// </summary>
    public static MeshFace Unset
    {
      get { return new MeshFace(int.MinValue, int.MinValue, int.MinValue); }
    }
#endregion

#region properties
    /// <summary>
    /// Internal property that figures out the debugger display for mesh Faces.
    /// </summary>
    internal string DebuggerDisplayUtil
    {
      get
      {
        return IsTriangle ? string.Format(System.Globalization.CultureInfo.InvariantCulture, "T({0}, {1}, {2})", m_a, m_b, m_c)
          : string.Format(System.Globalization.CultureInfo.InvariantCulture, "Q({0}, {1}, {2}, {3})", m_a, m_b, m_c, m_d);
      }
    }

    /// <summary>
    /// Gets or sets the first corner index of the mesh face.
    /// </summary>
    public int A
    {
      get { return m_a; }
      set { m_a = value; }
    }
    /// <summary>
    /// Gets or sets the second corner index of the mesh face.
    /// </summary>
    public int B
    {
      get { return m_b; }
      set { m_b = value; }
    }
    /// <summary>
    /// Gets or sets the third corner index of the mesh face.
    /// </summary>
    public int C
    {
      get { return m_c; }
      set { m_c = value; }
    }
    /// <summary>
    /// Gets or sets the fourth corner index of the mesh face. 
    /// If D equals C, the mesh face is considered to be a triangle 
    /// rather than a quad.
    /// </summary>
    public int D
    {
      get { return m_d; }
      set { m_d = value; }
    }

    /// <summary>
    /// Gets or sets the vertex index associated with an entry in this face.
    /// </summary>
    /// <param name="index">A number in interval [0-3] that refers to an index of a vertex in this face.</param>
    /// <returns>The vertex index associated with this mesh face.</returns>
    public int this[int index]
    {
      get
      {
        if (index == 0) return m_a;
        if (index == 1) return m_b;
        if (index == 2) return m_c;
        if (index == 3) return m_d;
        throw new IndexOutOfRangeException();
      }
      set
      {
        if (index == 0) m_a = value;
        else if (index == 1) m_b = value;
        else if (index == 2) m_c = value;
        else if (index == 3) m_d = value;
        else
          throw new IndexOutOfRangeException();
      }
    }

    /// <summary>
    /// Gets a value indicating whether or not this mesh face 
    /// is considered to be valid. Note that even valid mesh faces 
    /// could potentially be invalid in the context of a specific Mesh, 
    /// if one or more of the corner indices exceeds the number of 
    /// vertices on the mesh. If you want to perform a complete 
    /// validity check, use IsValid(int) instead.
    /// </summary>
    public bool IsValid()
    {
      if (m_a < 0) { return false; }
      if (m_b < 0) { return false; }
      if (m_c < 0) { return false; }
      if (m_d < 0) { return false; }

      if (m_a == m_b) { return false; }
      if (m_a == m_c) { return false; }
      if (m_a == m_d) { return false; }
      if (m_b == m_c) { return false; }
      if (m_b == m_d) { return false; }

      return true;
    }
    /// <summary>
    /// Gets a value indicating whether or not this mesh face 
    /// is considered to be valid. Unlike the simple IsValid function, 
    /// this function takes upper bound indices into account.
    /// </summary>
    /// <param name="vertexCount">Number of vertices in the mesh that this face is a part of.</param>
    /// <returns>true if the face is considered valid, false if not.</returns>
    public bool IsValid(int vertexCount)
    {
      if (!IsValid()) { return false; }

      if (m_a >= vertexCount) { return false; }
      if (m_b >= vertexCount) { return false; }
      if (m_c >= vertexCount) { return false; }
      if (m_d >= vertexCount) { return false; }

      return true;
    }

    /// <summary>
    /// Gets a value indicating whether or not this mesh face 
    /// is considered to be valid. Unlike the simple IsValid function, 
    /// this function takes actual point locations into account.
    /// </summary>
    /// <param name="points">Array of vertices with which to validate the face.</param>
    /// <returns>true if the face is considered valid, false if not.</returns>
    public bool IsValid(Point3d[] points)
    {
      return IsValidEx(ref points);
    }

    /// <summary>
    /// Attempts to repair this mesh face by taking both face indexes and 
    /// actual vertex locations into account. 
    /// </summary>
    /// <param name="points">Array of vertices with which to consider when repairing the face.</param>
    /// <returns>true if the face was repaired, false if not.</returns>
    /// <remarks>This function assumes the face is invalid and attempts to repair unconditionally.  It is
    /// only worthwhile to call Repair on faces where IsValid returns false.
    /// </remarks>
    public bool Repair(Point3d[] points)
    {
      return RepairEx(ref points);

    }

    /// <summary>
    /// Gets a value indicating whether or not this mesh face 
    /// is considered to be valid. Unlike the simple IsValid function, 
    /// this function takes actual point locations into account.
    /// </summary>
    /// <param name="points">Array of vertices with which to validate the face.</param>
    /// <returns>true if the face is considered valid, false if not.</returns>
    public bool IsValidEx(ref Point3d[] points)
    {
      int ct = points.Length;

      // Determine that vertex indexes are valid
      if (false == A >= 0 && A < ct && B >= 0 && B < ct && C >= 0 && C < ct && D >= 0 && D < ct &&
                   A != B && B != C && C != A && (C == D || (A != D && B != D)))
        return false;

      // Determine that vertex locations are valid
      if (!(points[A] != points[B]))
        return false;
      if (!(points[A] != points[C]))
        return false;
      if (!(points[B] != points[C]))
        return false;
      if (C != D)
      {
        if (!(points[A] != points[D]))
          return false;
        if (!(points[B] != points[D]))
          return false;
        if (!(points[C] != points[D]))
          return false;
      }

      return true;
    }

    private bool SetFaceIndex(ref MeshFace face, int idx, int input)
    {
      switch (idx)
      {
        case 0:
          face.A = input;
          return true;
        case 1:
          face.B = input;
          return true;
        case 2:
          face.C = input;
          return true;
        case 3:
          face.D = input;
          return true;
      }

      return false;
    }

    /// <summary>
    /// Attempts to repair this mesh face by taking both face indexes and 
    /// actual vertex locations into account. 
    /// </summary>
    /// <param name="points">Array of vertices with which to consider when repairing the face.</param>
    /// <returns>true if the face was repaired, false if not.</returns>
    /// <remarks>This function assumes the face is invalid and attempts to repair unconditionally.  It is
    /// only worthwhile to call Repair on faces where IsValid returns false.
    /// 
    /// This function is the analog of UnsafeNativeMethods.ON_MeshFace_Repair done completely here to avoid 
    /// copying the point array.
    /// </remarks>
    public bool RepairEx(ref Point3d[] points)
    {
      int ct = points.Length;

      MeshFace f = this;
      int fvi_count = 0;
      f.A = f.B = f.C = f.D = -1;

      if (A >= 0 && A < ct)
        SetFaceIndex(ref f, fvi_count++, A);

      if (B >= 0 && B < ct && f.A != B)
      {
        if (0 == fvi_count || points[f.A] != points[B])
          SetFaceIndex(ref f, fvi_count++, B);
      }

      if (fvi_count < 1)
        return false;

      if (C >= 0 && C < ct && f.A != C && f.B != C && points[f.A] != points[C])
      {
        if (1 == fvi_count || points[f.B] != points[C])
          SetFaceIndex(ref f, fvi_count++, C);
      }

      if (fvi_count < 2)
        return false;

      if (D >= 0 && D < ct && f.A != D && f.B != D && f.C != D && points[f.A] != points[D] && points[f.B] != points[D])
      {
        if (2 == fvi_count || points[f.C] != points[D])
          SetFaceIndex(ref f, fvi_count++, D);
      }

      if (fvi_count < 3)
        return false;

      if (3 == fvi_count)
        f.D = f.C;


      if (false == f.IsValidEx(ref points))
        return false;

      A = f.A;
      B = f.B;
      C = f.C;
      D = f.D;

      return true;
    }

    /// <summary>
    /// Gets a value indicating whether or not this mesh face is a triangle. 
    /// A mesh face is considered to be a triangle when C equals D, thus it is 
    /// possible for an Invalid mesh face to also be a triangle.
    /// </summary>
    public bool IsTriangle { get { return m_c == m_d; } }
    /// <summary>
    /// Gets a value indicating whether or not this mesh face is a quad. 
    /// A mesh face is considered to be a triangle when C does not equal D, 
    /// thus it is possible for an Invalid mesh face to also be a quad.
    /// </summary>
    public bool IsQuad { get { return m_c != m_d; } }
#endregion

#region methods
    /// <summary>
    /// Sets all the corners for this face as a triangle.
    /// </summary>
    /// <param name="a">Index of first corner.</param>
    /// <param name="b">Index of second corner.</param>
    /// <param name="c">Index of third corner.</param>
    public void Set(int a, int b, int c)
    {
      m_a = a;
      m_b = b;
      m_c = c;
      m_d = c;
    }
    /// <summary>
    /// Sets all the corners for this face as a quad.
    /// </summary>
    /// <param name="a">Index of first corner.</param>
    /// <param name="b">Index of second corner.</param>
    /// <param name="c">Index of third corner.</param>
    /// <param name="d">Index of fourth corner.</param>
    public void Set(int a, int b, int c, int d)
    {
      m_a = a;
      m_b = b;
      m_c = c;
      m_d = d;
    }

    /// <summary>
    /// Reverses the orientation of the face by swapping corners. 
    /// The first corner is always maintained.
    /// </summary>
    public MeshFace Flip()
    {
      if (m_c == m_d)
        return new MeshFace(m_a, m_c, m_b, m_b);
      return new MeshFace(m_a, m_d, m_c, m_b);
    }

    /// <summary>
    /// Returns a value indicating whether the other mesh face
    /// has precisely the same value as the current one.
    /// </summary>
    /// <param name="other">The other mesh face for comparison.</param>
    /// <returns>true if the other face is, also orderly, equal
    /// to the present one; otherwise false.</returns>
    public bool Equals(MeshFace other)
    {
      return (A == other.A && B == other.B && C == other.C && D == other.D);
    }

    /// <summary>
    /// Returns a value indicating whether the other object obj
    /// has precisely the same value as the current one.
    /// </summary>
    /// <param name="obj">Any object the represents the other mesh face for comparison.</param>
    /// <returns>true if obj is a mesh face that, also orderly, equals
    /// to the present one; otherwise false.</returns>
    public override bool Equals(object obj)
    {
      return obj is MeshFace && Equals((MeshFace)obj);
    }

    /// <summary>
    /// Returns a runtime-stable hashcode for the current mesh face.
    /// You are not allowed to rely on persistance of this hashcode in
    /// serialization, but for each version of RhinoCommon, this hashcode
    /// will be the same for each mesh face.
    /// </summary>
    /// <returns>A non-unique integer that represents this mesh face.</returns>
    public override int GetHashCode()
    {
      // Giulio, 2013 Nov 4: this does not do bit-avalanching
      // but it makes at least sure that a rotated face does not
      // compute the same hashcode as the original. This is not a requirement
      // but might be useful at times.
      return (~A) ^ B ^ C ^ D;
      // other option:
      // return A ^ (B<<1) ^ (C<<2) ^ (D<<3);
    }

    /// <summary>
    /// Determines whether two <see cref="MeshFace"/> structures have equal values.
    /// </summary>
    /// <param name="a">The first MeshFace.</param>
    /// <param name="b">The second MeshFace.</param>
    /// <returns>true if the indices of the two points are exactly equal; otherwise false.</returns>
    public static bool operator ==(MeshFace a, MeshFace b)
    {
      return a.Equals(b);
    }

    /// <summary>
    /// Determines whether two <see cref="MeshFace"/> structures have different values.
    /// </summary>
    /// <param name="a">The first MeshFace.</param>
    /// <param name="b">The second MeshFace.</param>
    /// <returns>true if the indices of the two points are in any way different; otherwise false.</returns>
    public static bool operator !=(MeshFace a, MeshFace b)
    {
      return (a.A != b.A || a.B != b.B || a.C != b.C || a.D != b.D);
    }

    /// <summary>
    /// Returns a string representation for this <see cref="MeshFace"/>.
    /// This is to provide a meaningful visualization of this structure
    /// and is subject to change in newer releases.
    /// </summary>
    /// <returns>The string representation.</returns>
    public override string ToString()
    {
      return DebuggerDisplayUtil;
    }

    /// <summary>
    /// Compares this <see cref="MeshFace" /> with another <see cref="MeshFace" />
    /// and returns a value of 1, 0, or -1, referring to dictionary order.
    /// <para>Index evaluation priority is first A, then B, then C, then D.</para>
    /// </summary>
    /// <param name="other">The other <see cref="MeshFace" /> to use in comparison.</param>
    /// <returns>
    /// <para> 0: if this is identical to other</para>
    /// <para>-1: if this &lt; other. Priority is for index of corner A first, then B, then C, then D.</para>
    /// <para>+1: otherwise.</para>
    /// </returns>
    public int CompareTo(MeshFace other)
    {
      if (A < other.A) return -1;
      if (A > other.A) return 1;

      if (B < other.B) return -1;
      if (B > other.B) return 1;

      if (C < other.C) return -1;
      if (C > other.C) return 1;

      if (D < other.D) return -1;
      if (D > other.D) return 1;

      return 0;
    }

    int IComparable.CompareTo(object obj)
    {
      if (!(obj is MeshFace))
        throw new ArgumentException("obj");

      return CompareTo((MeshFace)obj);
    }
#endregion
  }

  /// <summary>
  /// Represents a mesh ngon.
  /// <para>When retrieved from the Ngon property of a mesh,
  /// this contains faces that have edge valence strictly higher than 4.</para>
  /// <para>When retrieving Ngons from <see cref="Mesh.GetNgonAndFacesEnumerable()"/>,
  /// this might contain also triangles and quads.</para>
  /// </summary>
  public class MeshNgon : IEquatable<MeshNgon>, IComparable<MeshNgon>, IComparable
  {
    internal uint[] m_vi;
    internal uint[] m_fi;

    internal MeshNgon(IntPtr ngonPtr)
    {
      uint v_count = 0;
      uint f_count = 0;
      UnsafeNativeMethods.ON_MeshNgon_Counts(ngonPtr, ref v_count, ref f_count);
      m_vi = new uint[v_count];
      m_fi = new uint[f_count];
      if (v_count > 0 || f_count > 0)
      {
        UnsafeNativeMethods.ON_MeshNgon_CopyArrays(ngonPtr, m_vi, m_fi);
      }
    }

    /// <summary>
    /// Constructs an ngon from lists of mesh vertex and face indexes.
    /// </summary>
    /// <param name="meshVertexIndexList">
    /// A list of mesh vertex indexes that define the outer boundary of the ngon. The mesh vertex indexes must be in the correct order.
    /// </param>
    /// <param name="meshFaceIndexList">
    /// A list of mesh face indexes that define the interior of the ngon. The mesh face indexes
    /// may be in any order.
    /// </param>
    /// <returns></returns>
    public static MeshNgon Create(IList<int> meshVertexIndexList, IList<int> meshFaceIndexList)
    {
      var rc = new MeshNgon(IntPtr.Zero);
      rc.Set(meshVertexIndexList, meshFaceIndexList);
      return rc;
    }

    /// <summary>
    /// Gets an empty MeshNgon.
    /// </summary>
    public static MeshNgon Empty
    {
      get { return new MeshNgon(IntPtr.Zero); }
    }

#region properties
    /// <summary>
    /// Internal property that figures out the debugger display for mesh Faces.
    /// </summary>
    internal string DebuggerDisplayUtil
    {
      get
      {
        return string.Format(System.Globalization.CultureInfo.InvariantCulture, "Ngon({0} boundary vertices, {1} faces)", m_vi.LongLength, m_fi.LongLength);
      }
    }

    /// <summary>
    /// Get the number of vertices in this ngon.
    /// <seealso cref="MeshNgon"/> about length information.
    /// </summary>
    public int BoundaryVertexCount
    {
      get { return m_vi.Length; }
    }

    /// <summary>
    /// Get the number of faces in this ngon.
    /// </summary>
    public int FaceCount
    {
      get { return m_fi.Length; }
    }

    /// <summary>
    /// Get a mesh vertex index from the ngon's vertex index list.
    /// </summary>
    /// <param name="index">A mesh vertex number between 0 and MeshNgon.VertexCount-1 that refers to a mesh vertex index in this ngon.</param>
    /// <returns>A mesh vertex index.</returns>
    /// <seealso cref="MeshNgon"/> about length information.
    public int this[int index]
    {
      get
      {
        return (int)m_vi[index];
      }
    }

    /// <summary>
    /// Get the outer boundary mesh vertex list of the ngon.
    /// <para>Vertices are sorted counterclockwise with respect to the direction of the face,
    /// although the degree by which vertex normals will respect this might vary.</para>
    /// </summary>
    /// <returns>A list of mesh vertex indexes.</returns>
    /// <seealso cref="MeshNgon"/> about length information.
    [CLSCompliant(false)]
    public uint[] BoundaryVertexIndexList()
    {
      return m_vi.ToArray();
    }

    /// <summary>
    /// Get the ngon's mesh face index list.
    /// </summary>
    /// <returns>A list of mesh face indexes.</returns>
    [CLSCompliant(false)]
    public uint[] FaceIndexList()
    {
      return m_fi.ToArray();
    }

#endregion

#region methods

    /// <summary>
    /// Set the ngon vertex and face index lists.
    /// </summary>
    /// <param name="meshVertexIndexList">
    /// A list of mesh vertex indexes that define the outer boundary of the ngon. The mesh vertex indexes must be in the correct order.
    /// </param>
    /// <param name="meshFaceIndexList">
    /// A list of mesh face indexes that define the interior of the ngon. The mesh face indexes
    /// may be in any order.
    /// </param>
    public void Set(IList<int> meshVertexIndexList, IList<int> meshFaceIndexList)
    {
      // 24 March 2016 S. Baer
      // I realize this is slow. We can add olverloads that take uint or a bool
      // that says "I promise these are all non-negative ints" in the future
      uint[] vi = new uint[meshVertexIndexList.LongCount()];
      for (int i = 0; i < meshVertexIndexList.LongCount(); i++)
      {
        int v = meshVertexIndexList[i];
        if (v < 0)
          throw new ArgumentOutOfRangeException("meshVertexIndexList");
        vi[i] = (uint)v;
      }
      uint[] fi = new uint[meshFaceIndexList.LongCount()];
      for (int i = 0; i < meshFaceIndexList.LongCount(); i++)
      {
        int f = meshFaceIndexList[i];
        if (f < 0)
          throw new ArgumentOutOfRangeException("meshFaceIndexList");
        fi[i] = (uint)f;
      }
      m_vi = vi;
      m_fi = fi;
    }

    /// <summary>
    /// Determines if this ngon and otherNgon are identical.
    /// </summary>
    /// <param name="otherNgon">The other ngon for comparison.</param>
    /// <returns>true if otherNgon is identical to this ngon; otherwise false.
    /// </returns>
    public bool Equals(MeshNgon otherNgon)
    {
      return CompareTo(otherNgon) == 0;
    }



    /// <summary>
    /// Determines if otherObj is a MeshNgon and is identical to this ngon.
    /// </summary>
    /// <param name="otherObj">Any object the represents the other mesh face for comparison.</param>
    /// <returns>true if otherObj is a MeshNgon and is identical to this ngon; otherwise false.
    /// </returns>
    public override bool Equals(object otherObj)
    {
      MeshNgon other_ngon = otherObj as MeshNgon;
      return other_ngon != null && Equals(other_ngon);
    }

    /// <summary>
    /// Returns a runtime-stable hashcode for the current mesh ngon.
    /// You are not allowed to rely on persistance of this hashcode in
    /// serialization, but for each instance of the application, this hashcode
    /// will be the same for ngons with identical vertex and face lists.
    /// </summary>
    /// <returns>A non-unique integer that represents this mesh ngon.</returns>
    public override int GetHashCode()
    {
      return UnsafeNativeMethods.ON_MeshNgon_HashCode((uint)m_vi.LongLength, m_vi, (uint)m_fi.LongLength, m_fi);
    }

    /// <summary>
    /// Determines whether two <see cref="MeshNgon"/> structures have equal values.
    /// </summary>
    /// <param name="a">The first MeshNgon.</param>
    /// <param name="b">The second MeshNgon.</param>
    /// <returns>true if the vertex and face index lists are identical; otherwise false.</returns>
    public static bool operator ==(MeshNgon a, MeshNgon b)
    {
      if (((object)a) == null) return ((object)b) == null;
      if (((object)b) == null) return false;

      return a.Equals(b);
    }

    /// <summary>
    /// Determines whether two <see cref="MeshNgon"/> structures have different values.
    /// </summary>
    /// <param name="a">The first MeshNgon.</param>
    /// <param name="b">The second MeshNgon.</param>
    /// <returns>true if the vertex or face index lists are different in any way; otherwise false.</returns>
    public static bool operator !=(MeshNgon a, MeshNgon b)
    {
      if (((object)a) == null) return ((object)b) != null;
      if (((object)b) == null) return true;

      return !a.Equals(b);
    }

    /// <summary>
    /// Returns a string representation for this <see cref="MeshNgon"/>.
    /// This is to provide a meaningful visualization of this structure
    /// and is subject to change in newer releases.
    /// </summary>
    /// <returns>The string representation.</returns>
    public override string ToString()
    {
      return DebuggerDisplayUtil;
    }

    /// <summary>
    /// Compares this <see cref="MeshNgon" /> with otherNgon
    /// and returns a value of 1, 0, or -1.
    /// <para>Priority is for vertex count, then face count,
    /// then vertex index list values, then face index list values.
    /// </para>
    /// </summary>
    /// <param name="otherNgon">The other <see cref="MeshNgon" /> to use in comparison.</param>
    /// <returns>
    /// <para> 0: if this is identical to otherNgon</para>
    /// <para>-1: if this &lt; otherNgon. </para>
    /// <para>+1: if this &gt; otherNgon.</para>
    /// </returns>
    public int CompareTo(MeshNgon otherNgon)
    {
      return UnsafeNativeMethods.ON_MeshNgon_Compare((uint)m_vi.LongLength, m_vi, (uint)m_fi.LongLength, m_fi,
        (uint)otherNgon.m_vi.LongLength, otherNgon.m_vi, (uint)otherNgon.m_fi.LongLength, otherNgon.m_fi);
    }

    int IComparable.CompareTo(object obj)
    {
      if (!(obj is MeshNgon))
        throw new ArgumentException("obj");

      return CompareTo((MeshNgon)obj);
    }
#endregion
  }

#if RHINO_SDK
  /// <summary>
  /// Contains shutlining curve information. This is used in <see cref="Mesh.WithShutLining"/>.
  /// </summary>
  public class ShutLiningCurveInfo
  {
    private readonly Curve m_curve;
    private readonly bool m_enabled;
    private readonly double m_radius;
    private readonly int m_profile;
    private readonly bool m_pull;
    private readonly bool m_is_bump;

    private readonly Interval[] m_curve_intervals;

    /// <summary>
    /// Creates a new instance of the ShutLining curve information class.
    /// </summary>
    /// <param name="curve">The profile curve.</param>
    /// <param name="radius">The profile radius.</param>
    /// <param name="profile">The profile type.</param>
    /// <param name="pull">True if the curve should be pulled.</param>
    /// <param name="isBump">True if profile constitutes a bump. See Rhino's Help for more information.</param>
    /// <param name="curveIntervals">The parts of the curve to use as profiles.</param>
    /// <param name="enabled">If true, this curve is active.</param>
    /// <exception cref="ArgumentNullException">If curve is null.</exception>
    public ShutLiningCurveInfo(Curve curve,
      double radius, int profile, bool pull, bool isBump,
      IEnumerable<Interval> curveIntervals = null, bool enabled = true)
    {
      if (curve == null) throw new ArgumentNullException("curve");

      m_curve = curve;
      m_enabled = enabled;
      m_radius = radius;
      m_profile = profile;
      m_pull = pull;
      m_is_bump = isBump;

      if (curveIntervals != null)
      {
        var array = curveIntervals as Interval[] ?? curveIntervals.ToArray();
        if (array.Length > 0) this.m_curve_intervals = array;
      }
    }

    /// <summary>
    /// The profile curve.
    /// </summary>
    public Curve Curve
    {
      get
      {
        return m_curve;
      }
    }

    /// <summary>
    /// If true, this curve is active.
    /// </summary>
    public bool Enabled
    {
      get
      {
        return m_enabled;
      }
    }

    /// <summary>
    /// The profile radius.
    /// </summary>
    public double Radius
    {
      get
      {
        return m_radius;
      }
    }

    /// <summary>
    /// >The profile type.
    /// </summary>
    public int Profile
    {
      get
      {
        return m_profile;
      }
    }

    /// <summary>
    /// True if the curve should be pulled.
    /// </summary>
    public bool Pull
    {
      get
      {
        return m_pull;
      }
    }

    /// <summary>
    /// True if profile constitutes a bump. See Rhino's Help for more information.
    /// </summary>
    public bool IsBump
    {
      get
      {
        return m_is_bump;
      }
    }

    /// <summary>
    /// The parts of the curve to use as profiles.
    /// </summary>
    public ReadOnlyCollection<Interval> CurveIntervals
    {
      get
      {
        return new ReadOnlyCollection<Interval>(m_curve_intervals);
      }
    }

    internal static IntPtr CreateNativeArgument(ShutLiningCurveInfo info)
    {
      var crv_const_ptr = info.m_curve.ConstPointer();

      unsafe
      {
        fixed (void* intervals = info.m_curve_intervals)
        {
          return UnsafeNativeMethods.RHC_ShutLiningCurve_New(
            crv_const_ptr,
            info.m_enabled,
            info.m_radius,
            info.m_profile,
            info.m_pull,
            info.m_is_bump,
            new IntPtr(intervals),
            info.m_curve_intervals == null ? 0 : info.m_curve_intervals.Length);
        }
      }
    }

    internal static void DeleteNativeArgument(IntPtr ptr)
    {
      UnsafeNativeMethods.RHC_ShutLiningCurve_Delete(ptr);
    }
  }

  /// <summary>
  /// Contains mesh displacement information.
  /// </summary>
  public class MeshDisplacementInfo
  {
    /// <summary>
    /// Constructs a displacement information instance with default values.
    /// Users of this class should not rely on default values to stay constant
    /// across service releases.
    /// </summary>
    /// <exception cref="ArgumentNullException">If texture or mapping is null.</exception>
    public MeshDisplacementInfo(RenderTexture texture,
      TextureMapping mapping)
    {
      if (texture == null) throw new ArgumentNullException("texture");
      Texture = texture;

      if (mapping == null) throw new ArgumentNullException("mapping");
      Mapping = mapping;

      Black = 0.0;
      White = 1.0;
      MappingTransform = Transform.Identity;
      InstanceTransform = Transform.Identity;

      BlackMove = 0.0;
      PostWeldAngle = 40.0;
      RefineSensitivity = 0.5;
      SweepPitch = 1000.0;
      WhiteMove = 1.0;
      ChannelNumber = 1;
      FaceLimit = 10000;
      FairingAmount = 4;
      RefineStepCount = 1;
      MemoryLimit = 64;
    }

    /// <summary>
    /// The texture used as displacement.
    /// </summary>
    public RenderTexture Texture { get; private set; }

    /// <summary>
    /// Value considered lowest point in the displacement.
    /// </summary>
    public double Black { get; set; }

    /// <summary>
    /// Value considered highest point of the displacement texture.
    /// </summary>
    public double White { get; set; }

    /// <summary>
    /// The texture mapping of the mesh.
    /// </summary>
    public TextureMapping Mapping { get; private set; }

    /// <summary>
    /// Texture mapping transform.
    /// </summary>
    public Transform MappingTransform { get; set; }

    /// <summary>
    /// Instance transformation of the mesh.
    /// </summary>
    public Transform InstanceTransform { get; set; }

    // --- ARGS

    /// <summary>
    /// The amount of displacement for the black color in the texture.
    /// </summary>
    public double BlackMove { get; set; }

    /// <summary>
    /// Specifies the maximum angle between face normals of adjacent faces
    /// that will get welded together.
    /// </summary>
    public double PostWeldAngle { get; set; }

    /// <summary>
    /// <para>Specifies how sensitive the divider for contrasts is on the
    /// displacement texture.</para>
    /// <para>Specify 1 to split all mesh edges on each refine step.</para>
    /// <para>Specify 0.99 to make even slight contrasts on the displacement
    /// texture cause edges to be split.</para>
    /// <para>Specifying 0.01 only splits edges where heavy contrast
    /// exists.</para>
    /// </summary>
    public double RefineSensitivity { get; set; }

    /// <summary>
    /// Specifies how densely the object is initially subdivided.
    /// The lower the value, the higher the resolution of the displaced mesh.
    /// </summary>
    public double SweepPitch { get; set; }

    /// <summary>
    /// The amount of displacement for the white color in the texture.
    /// </summary>
    public double WhiteMove { get; set; }

    /// <summary>
    /// Mapping channel number for the displacement mapping.
    /// </summary>
    public int ChannelNumber { get; set; }

    /// <summary>
    /// Runs a mesh reduction as a post process o simplify the result of
    /// displacement to meet the specified number of faces.
    /// </summary>
    public int FaceLimit { get; set; }

    /// <summary>
    /// Straightens rough feature edges.
    /// The value specifies the number of passes.
    /// </summary>
    public int FairingAmount { get; set; }

    /// <summary>
    /// Specifies the number of refinement passes.
    /// </summary>
    public int RefineStepCount { get; set; }

    /// <summary>
    /// Specifies how much memory can be allocated for use by the
    /// displacement mesh. Value in megabytes.
    /// </summary>
    public int MemoryLimit { get; set; }
  }
#endif
}