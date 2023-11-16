using System;
using System.Drawing;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Rhino.Collections;
using Rhino.Runtime;
using Rhino.Runtime.InteropWrappers;
using System.Threading;
using System.Security.Policy;

namespace Rhino.Geometry
{
  /// <summary>
  /// Permits access to the underlying PointCloud raw data structures in an unsafe way.
  /// </summary>
  /// <remarks>This lock object needs to be disposed before using the PointCloud in other calculations and this can 
  /// be achieved with the using keyword (Using in VB.Net).</remarks>
  public sealed class PointCloudUnsafeLock : IDisposable
  {
    PointCloud m_cloud;

    internal PointCloudUnsafeLock(PointCloud parent, bool writable)
    {
      m_cloud = parent;
      Writable = writable;
    }

    internal bool Writable { get; set; }

    /// <summary>
    /// Retrieves a pointer to the raw point array, which uses coordinates
    /// defined with double precision floating point numbers, or null if none is available.
    /// </summary>
    /// <param name="length">The length of the array. This value is returned by reference (out in C#).
    /// 0 is returned when there is no single precision array.</param>
    /// <returns>The beginning of the point array. Item 0 is the first vertex,
    /// and item length-1 is the last valid one. If no array is available, null is returned.</returns>
    /// <since>7.9</since>
    [CLSCompliant(false)]
    public unsafe Point3d* PointArray(out int length)
    {
      if (m_cloud == null) throw new ObjectDisposedException("The lock was released.");

      IntPtr ptr_cloud = Writable ? m_cloud.NonConstPointer() : m_cloud.ConstPointer();
      int l = 0;
      IntPtr ptr_array = UnsafeNativeMethods.ON_PointCloud_PointArray_Pointer(ptr_cloud, ref l);

      if (ptr_array == IntPtr.Zero)
      {
        length = 0;
        return null;
      }
      Point3d* result = (Point3d*)ptr_array.ToPointer();
      length = (null != result) ? l : 0;
      return result;
    }

    /// <summary>
    /// Retrieves a pointer to the raw array of point normals, or null if none is available.
    /// </summary>
    /// <param name="length">The length of the array. This value is returned by reference (out in C#).
    /// 0 is returned when there is no single precision array.</param>
    /// <returns>The beginning of the vector array. Item 0 is the first vertex,
    /// and item length-1 is the last valid one. If no array is available, null is returned.</returns>
    /// <since>7.9</since>
    [CLSCompliant(false)]
    public unsafe Vector3d* NormalArray(out int length)
    {
      if (m_cloud == null) throw new ObjectDisposedException("The lock was released.");

      IntPtr ptr_cloud = Writable ? m_cloud.NonConstPointer() : m_cloud.ConstPointer();
      int l = 0;
      IntPtr ptr_array = UnsafeNativeMethods.ON_PointCloud_NormalArray_Pointer(ptr_cloud, ref l);

      if (ptr_array == IntPtr.Zero)
      {
        length = 0;
        return null;
      }
      Vector3d* result = (Vector3d*)ptr_array.ToPointer();
      length = (null != result) ? l : 0;
      return result;
    }

    /// <summary>
    /// Retrieves a pointer to the raw array of colors, which are represented
    /// as 32-bit integers in ARGB format, or null if none is available.
    /// </summary>
    /// <param name="length">The length of the array. This value is returned by reference (out in C#).
    /// 0 is returned when there is no single precision array.</param>
    /// <returns>The beginning of the color array. Item 0 is the first vertex,
    /// and item length-1 is the last valid one. If no array is available, null is returned.</returns>
    /// <since>7.9</since>
    [CLSCompliant(false)]
    public unsafe int* ColorArray(out int length)
    {
      if (m_cloud == null) throw new ObjectDisposedException("The lock was released.");

      IntPtr ptr_cloud = Writable ? m_cloud.NonConstPointer() : m_cloud.ConstPointer();
      int l = 0;
      IntPtr ptr_array = UnsafeNativeMethods.ON_PointCloud_ColorArray_Pointer(ptr_cloud, ref l);

      if (ptr_array == IntPtr.Zero)
      {
        length = 0;
        return null;
      }
      int* result = (int*)ptr_array.ToPointer();
      length = (null != result) ? l : 0;
      return result;
    }

    /// <summary>
    /// Retrieves a pointer to the raw array of point values, or null if none is available.
    /// </summary>
    /// <param name="length">The length of the array. This value is returned by reference (out in C#).
    /// 0 is returned when there is no single precision array.</param>
    /// <returns>The beginning of the value array. Item 0 is the first vertex,
    /// and item length-1 is the last valid one. If no array is available, null is returned.</returns>
    /// <since>7.9</since>
    [CLSCompliant(false)]
    public unsafe double* ValueArray(out int length)
    {
      if (m_cloud == null) throw new ObjectDisposedException("The lock was released.");

      IntPtr ptr_cloud = Writable ? m_cloud.NonConstPointer() : m_cloud.ConstPointer();
      int l = 0;
      IntPtr ptr_array = UnsafeNativeMethods.ON_PointCloud_ValueArray_Pointer(ptr_cloud, ref l);

      if (ptr_array == IntPtr.Zero)
      {
        length = 0;
        return null;
      }
      double* result = (double*)ptr_array.ToPointer();
      length = (null != result) ? l : 0;
      return result;
    }

    void IDisposable.Dispose()
    {
      Release();
    }

    /// <summary>
    /// Releases the lock and updates the underlying unmanaged data structures.
    /// </summary>
    /// <since>6.0</since>
    public void Release()
    {
      if (!Writable || m_cloud == null) return;

      IntPtr ptr_this = m_cloud.NonConstPointer();
      UnsafeNativeMethods.ON_PointCloud_UnlockPointCloudData(ptr_this);

      m_cloud = null;
    }
  }

  /// <summary>
  /// Represents a single item in a point cloud. A PointCloud item 
  /// always has a location, but it has an optional normal vector and color.
  /// </summary>
  public class PointCloudItem
  {
    #region fields

    readonly PointCloud m_parent;
    readonly int m_index;
    #endregion

    #region constructors
    internal PointCloudItem(PointCloud parent, int index)
    {
      m_parent = parent;
      m_index = index;
    }
    #endregion

    #region properties

    /// <summary>
    /// Gets or sets the location of this point cloud item.
    /// </summary>
    /// <since>5.0</since>
    public Point3d Location
    {
      get
      {
        Point3d rc = new Point3d();
        IntPtr ptr = m_parent.ConstPointer();
        UnsafeNativeMethods.ON_PointCloud_GetPoint(ptr, m_index, ref rc);
        return rc;
      }
      set
      {
        IntPtr ptr = m_parent.NonConstPointer();
        UnsafeNativeMethods.ON_PointCloud_SetPoint(ptr, m_index, value);
      }
    }

    /// <summary>
    /// Gets or sets the X component of this point cloud item location.
    /// </summary>
    /// <since>5.0</since>
    public double X
    {
      get
      {
        return Location.X;
      }
      set
      {
        Point3d pt = Location;
        pt.X = value;
        Location = pt;
      }
    }

    /// <summary>
    /// Gets or sets the Y component of this point cloud item location.
    /// </summary>
    /// <since>5.0</since>
    public double Y
    {
      get
      {
        return Location.Y;
      }
      set
      {
        Point3d pt = Location;
        pt.Y = value;
        Location = pt;
      }
    }

    /// <summary>
    /// Gets or sets the Z component of this point cloud item location.
    /// </summary>
    /// <since>5.0</since>
    public double Z
    {
      get
      {
        return Location.Z;
      }
      set
      {
        Point3d pt = Location;
        pt.Z = value;
        Location = pt;
      }
    }

    /// <summary>
    /// Gets or sets the normal vector for this point cloud item.
    /// If this point cloud item does not have a normal vector, 
    /// Vector3d.Unset is returned.
    /// </summary>
    /// <since>5.0</since>
    public Vector3d Normal
    {
      get
      {
        Vector3d rc = Vector3d.Unset;
        IntPtr ptr = m_parent.ConstPointer();
        UnsafeNativeMethods.ON_PointCloud_GetNormal(ptr, m_index, ref rc);
        return rc;
      }
      set
      {
        IntPtr ptr = m_parent.NonConstPointer();
        UnsafeNativeMethods.ON_PointCloud_SetNormal(ptr, m_index, value);
      }
    }

    /// <summary>
    /// Gets or sets the color of this point cloud item.
    /// If this point cloud item does not have a color, System.Drawing.Color.Black is returned.
    /// </summary>
    /// <since>5.0</since>
    public Color Color
    {
      get
      {
        int argb = 0;
        IntPtr ptr = m_parent.ConstPointer();
        UnsafeNativeMethods.ON_PointCloud_GetColor(ptr, m_index, ref argb);
        return Color.FromArgb(argb);
      }
      set
      {
        IntPtr ptr = m_parent.NonConstPointer();
        UnsafeNativeMethods.ON_PointCloud_SetColor(ptr, m_index, value.ToArgb());
      }
    }

    /// <summary>
    /// Gets or sets the hidden flag of this point cloud item.
    /// If this point cloud item does not have a hidden flag, false is returned.
    /// </summary>
    /// <since>5.0</since>
    public bool Hidden
    {
      get
      {
        bool rc = false;
        IntPtr ptr = m_parent.ConstPointer();
        UnsafeNativeMethods.ON_PointCloud_GetHiddenFlag(ptr, m_index, ref rc);
        return rc;
      }
      set
      {
        IntPtr ptr = m_parent.NonConstPointer();
        UnsafeNativeMethods.ON_PointCloud_SetHiddenFlag(ptr, m_index, value);
      }
    }

    /// <summary>
    /// Gets or set the extra value of this point cloud item.
    /// The extra value can be used to store a user-defined value, such as intensity.
    /// If this point cloud item does not have an extra value, RhinoMath.UnsetValue is returned.
    /// </summary>
    /// <since>7.5</since>
    public double PointValue
    {
      get
      {
        double rc = RhinoMath.UnsetValue;
        IntPtr ptr = m_parent.ConstPointer();
        UnsafeNativeMethods.ON_PointCloud_GetExtra(ptr, m_index, ref rc);
        return rc;
      }
      set
      {
        IntPtr ptr = m_parent.NonConstPointer();
        UnsafeNativeMethods.ON_PointCloud_SetExtra(ptr, m_index, value);
      }
    }

    /// <summary>
    /// Gets the index of this point cloud item.
    /// </summary>
    /// <since>5.0</since>
    public int Index
    {
      get { return m_index; }
    }

    #endregion
  }

  /// <summary>
  /// Represents a collection of coordinates with optional normal vectors and colors.
  /// </summary>
  [Serializable]
  public class PointCloud : GeometryBase, IEnumerable<PointCloudItem>
  {
    #region constructors
    internal PointCloud(IntPtr nativePointer, object parent)
      : base(nativePointer, parent, -1)
    {
      if (null == parent)
        ApplyMemoryPressure();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PointCloud"/> class
    /// that is empty.
    /// </summary>
    /// <since>5.0</since>
    public PointCloud()
    {
      IntPtr ptr = UnsafeNativeMethods.ON_PointCloud_New();
      ConstructNonConstObject(ptr);
      ApplyMemoryPressure();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PointCloud"/> class,
    /// copying (Merge) the content of another point cloud.
    /// </summary>
    /// <since>5.0</since>
    public PointCloud(PointCloud other)
    {
      IntPtr ptr = UnsafeNativeMethods.ON_PointCloud_New();
      ConstructNonConstObject(ptr);

      if (other != null)
        Merge(other);

      ApplyMemoryPressure();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PointCloud"/> class,
    /// copying the content from a set of points.
    /// </summary>
    /// <param name="points">A list or an array of Point3d, or any object that implements <see cref="IEnumerable{Point3d}"/>.</param>
    /// <since>5.0</since>
    public PointCloud(IEnumerable<Point3d> points)
    {
      int count;
      Point3d[] point_array = Rhino.Collections.RhinoListHelpers.GetConstArray(points, out count);
      IntPtr ptr;
      if (null == point_array || count < 1)
      {
        ptr = UnsafeNativeMethods.ON_PointCloud_New();
      }
      else
      {
        ptr = UnsafeNativeMethods.ON_PointCloud_New1(count, point_array);
      }
      ConstructNonConstObject(ptr);
      ApplyMemoryPressure();
    }

    internal override GeometryBase DuplicateShallowHelper()
    {
      return new PointCloud(IntPtr.Zero, null);
    }

    // serialization constructor
    /// <summary>
    /// Binds with the Rhino default serializer to support object persistence.
    /// </summary>
    /// <param name="info">Some storage.</param>
    /// <param name="context">The source and destination of the stream.</param>
    protected PointCloud(SerializationInfo info, StreamingContext context)
      : base (info, context)
    {
    }
    #endregion

    const int idx_PointCount = 0;
    const int idx_NormalCount = 1;
    const int idx_ColorCount = 2;
    //const int idx_HiddenCount = 3;
    const int idx_HiddenPointCount = 4;
    const int idx_ExtraCount = 5;

    #region properties
    /// <summary>
    /// Gets the number of points in this point cloud.
    /// </summary>
    /// <since>5.0</since>
    public int Count
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        return UnsafeNativeMethods.ON_PointCloud_GetInt(const_ptr_this, idx_PointCount);
      }
    }
    /// <summary>
    /// Gets the item at the given index.
    /// </summary>
    /// <param name="index">Index of item to retrieve.</param>
    /// <returns>The item at the given index.</returns>
    public PointCloudItem this[int index]
    {
      get
      {
        if (index < 0) { throw new IndexOutOfRangeException("index must be larger than or equal to zero"); }
        if (index >= Count) { throw new IndexOutOfRangeException("index must be smaller than the Number of points in the PointCloud"); }
        return new PointCloudItem(this, index);
      }
    }

    /// <summary>
    /// Gets the number of points that have their Hidden flag set.
    /// </summary>
    /// <since>5.0</since>
    public int HiddenPointCount
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        return UnsafeNativeMethods.ON_PointCloud_GetInt(const_ptr_this, idx_HiddenPointCount);
      }
    }

    const int idx_Colors = 0;
    const int idx_Normals = 1;
    const int idx_Hidden = 2;
    const int idx_Extra = 3;

    /// <summary>
    /// Gets a value indicating whether or not the points in this 
    /// point cloud have colors assigned to them.
    /// </summary>
    /// <since>5.0</since>
    public bool ContainsColors
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        return UnsafeNativeMethods.ON_PointCloud_GetBool(const_ptr_this, idx_Colors);
      }
    }

    /// <summary>
    /// Gets a value indicating whether or not the points in this 
    /// point cloud have normals assigned to them.
    /// </summary>
    /// <since>5.0</since>
    public bool ContainsNormals
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        return UnsafeNativeMethods.ON_PointCloud_GetBool(const_ptr_this, idx_Normals);
      }
    }

    /// <summary>
    /// Gets a value indicating whether or not the points in this 
    /// point cloud have hidden flags assigned to them.
    /// </summary>
    /// <since>5.0</since>
    public bool ContainsHiddenFlags
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        return UnsafeNativeMethods.ON_PointCloud_GetBool(const_ptr_this, idx_Hidden);
      }
    }

    /// <summary>
    /// Gets a value indicating whether or not the points in this point cloud have extra values assigned to them.
    /// Extra values can be used to store a user-defined values, such as intensity.
    /// </summary>
    /// <since>7.5</since>
    public bool ContainsPointValues
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        return UnsafeNativeMethods.ON_PointCloud_GetBool(const_ptr_this, idx_Extra);
      }
    }

    #endregion

    #region methods

    /// <summary>
    /// Destroys the color information in this point cloud.
    /// </summary>
    /// <since>5.0</since>
    public void ClearColors()
    {
      if (!ContainsColors)
        return;

      IntPtr ptr_this = NonConstPointer();
      UnsafeNativeMethods.ON_PointCloud_DestroyArray(ptr_this, idx_Colors);
    }

    /// <summary>
    /// Destroys the normal vector information in this point cloud.
    /// </summary>
    /// <since>5.0</since>
    public void ClearNormals()
    {
      if (!ContainsNormals)
        return;

      IntPtr ptr_this = NonConstPointer();
      UnsafeNativeMethods.ON_PointCloud_DestroyArray(ptr_this, idx_Normals);
    }

    /// <summary>
    /// Destroys the hidden flag information in this point cloud.
    /// </summary>
    /// <since>5.0</since>
    public void ClearHiddenFlags()
    {
      if (!ContainsHiddenFlags)
        return;

      IntPtr ptr_this = NonConstPointer();
      UnsafeNativeMethods.ON_PointCloud_DestroyArray(ptr_this, idx_Hidden);
    }

    /// <summary>
    /// Destroys the extra value information in this point cloud.
    /// Extra values can be used to store a user-defined values, such as intensity.
    /// </summary>
    /// <since>7.5</since>
    public void ClearPointValues()
    {
      if (!ContainsPointValues)
        return;

      IntPtr ptr_this = NonConstPointer(); 
      UnsafeNativeMethods.ON_PointCloud_DestroyArray(ptr_this, idx_Extra);
    }

    /// <summary>
    /// Appends a new PointCloudItem to the end of this point cloud.
    /// </summary>
    /// <returns>The newly appended item.</returns>
    /// <since>5.0</since>
    public PointCloudItem AppendNew()
    {
      Add(Point3d.Origin);
      return this[Count - 1];
    }

    /// <summary>
    /// Inserts a new <see cref="PointCloudItem"/> at a specific position of the point cloud.
    /// </summary>
    /// <param name="index">Index of new item.</param>
    /// <returns>The newly inserted item.</returns>
    /// <since>5.0</since>
    public PointCloudItem InsertNew(int index)
    {
      Insert(index, Point3d.Origin);
      return this[index];
    }

    /// <summary>
    /// Merges, or appends, a specified point cloud into this one.
    /// </summary>
    /// <param name="other">PointCloud to merge with this one.</param>
    /// <since>5.0</since>
    public void Merge(PointCloud other)
    {
      IntPtr const_ptr_other = other.ConstPointer();
      IntPtr ptr_this = NonConstPointer();
      UnsafeNativeMethods.ON_PointCloud_MergeCloud(ptr_this, const_ptr_other);
    }

    /// <summary>
    /// Append a new point to the end of the list.
    /// </summary>
    /// <param name="point">Point to append.</param>
    /// <since>5.0</since>
    public void Add(Point3d point)
    {
      IntPtr ptr_this = NonConstPointer();
      UnsafeNativeMethods.ON_PointCloud_AppendPoint1(ptr_this, point);
    }

    /// <summary>
    /// Append a new point to the end of the list.
    /// </summary>
    /// <param name="point">Point to append.</param>
    /// <param name="normal">Normal vector of new point.</param>
    /// <since>5.0</since>
    public void Add(Point3d point, Vector3d normal)
    {
      IntPtr ptr_this = NonConstPointer();
      UnsafeNativeMethods.ON_PointCloud_AppendPoint3(ptr_this, point, normal);
    }

    /// <summary>
    /// Append a new point to the end of the list.
    /// </summary>
    /// <param name="point">Point to append.</param>
    /// <param name="color">Color of new point.</param>
    /// <since>5.0</since>
    public void Add(Point3d point, Color color)
    {
      IntPtr ptr_this = NonConstPointer();
      UnsafeNativeMethods.ON_PointCloud_AppendPoint2(ptr_this, point, color.ToArgb());
    }

    /// <summary>
    /// Append a new point to the end of the list.
    /// </summary>
    /// <param name="point">Point to append.</param>
    /// <param name="normal">Normal vector of new point.</param>
    /// <param name="color">Color of new point.</param>
    /// <since>5.0</since>
    public void Add(Point3d point, Vector3d normal, Color color)
    {
      IntPtr ptr_this = NonConstPointer();
      UnsafeNativeMethods.ON_PointCloud_AppendPoint4(ptr_this, point, color.ToArgb(), normal);
    }

    /// <summary>
    /// Append a new point to the end of the list.
    /// </summary>
    /// <param name="point">Point to append.</param>
    /// <param name="normal">Normal vector of new point.</param>
    /// <param name="color">Color of new point.</param>
    /// <param name="value">
    /// Extra value of new point.
    /// An extra value can be used to store a user-defined value, such as intensity.
    /// </param>
    /// <since>7.5</since>
    public void Add(Point3d point, Vector3d normal, Color color, double value)
    {
      IntPtr ptr_this = NonConstPointer();
      UnsafeNativeMethods.ON_PointCloud_AppendPoint5(ptr_this, point, color.ToArgb(), normal, value);
    }

    /// <summary>
    /// Appends a collection of points to this point cloud.
    /// </summary>
    /// <param name="points">Points to append.</param>
    /// <since>5.0</since>
    public void AddRange(IEnumerable<Point3d> points)
    {
      int count;
      Point3d[] point_array = Rhino.Collections.RhinoListHelpers.GetConstArray(points, out count);
      if (point_array == null)
        return;

      IntPtr ptr_this = NonConstPointer();
      UnsafeNativeMethods.ON_PointCloud_AppendPoints(ptr_this, count, point_array, null);
    }

    /// <summary>
    /// Appends a collection of points and normal vectors to this point cloud.
    /// </summary>
    /// <exception cref="ArgumentException">thrown if points and normals have differing numbers of elements</exception>
    /// <param name="points">Points to append.</param>
    /// <param name="normals">Normal Vectors to append.</param>
    /// <since>6.0</since>
    public void AddRange(IEnumerable<Point3d> points, IEnumerable<Vector3d> normals)
    {
      int point_count;
      Point3d[] point_array = Rhino.Collections.RhinoListHelpers.GetConstArray(points, out point_count);
      if (point_array == null || point_count == 0)
        return;

      int normal_count;
      Vector3d[] normal_array = Rhino.Collections.RhinoListHelpers.GetConstArray(normals, out normal_count);
      if (normal_array == null || normal_count == 0)
        return;

      if (point_count != normal_count)
        throw new ArgumentException("Must supply equal number of points and vectors");

      IntPtr ptr_this = NonConstPointer();
      UnsafeNativeMethods.ON_PointCloud_AppendPoints(ptr_this, point_count, point_array, normal_array);
    }

    /// <summary>
    /// Appends a collection of points and normal vectors to this point cloud.
    /// </summary>
    /// <param name="points">Points to append.</param>
    /// <param name="colors">Colors to append.</param>
    /// <since>6.0</since>
    public void AddRange(IEnumerable<Point3d> points, IEnumerable<Color> colors)
    {
      int count;
      Point3d[] ptArray = Rhino.Collections.RhinoListHelpers.GetConstArray(points, out count);
      if (ptArray == null) { return; }

      int countColor;

      var argbList = new List<int>();
#if RHINO3DM_BUILD || DOTNETCORE
      foreach (var color in colors) {
        var abgr = Color.FromArgb (color.A, color.B, color.G, color.R);
        argbList.Add (abgr.ToArgb ());
      }
#else 
      foreach (var color in colors)
        argbList.Add(ColorTranslator.ToWin32(color));
#endif
      int[] argbArray = Rhino.Collections.RhinoListHelpers.GetConstArray(argbList.ToArray(), out countColor);
      if (argbArray == null) { return; }

      if (count != countColor) { throw new ArgumentException("Must supply equal number of points and colors"); }

      if (count == 0 || countColor == 0) { return; }


      IntPtr ptr = NonConstPointer();
      UnsafeNativeMethods.ON_PointCloud_AppendPoints2(ptr, count, ptArray, argbArray);
    }

    /// <summary>
    /// Appends a collection of points, normal vectors, and colors to this point cloud.
    /// </summary>
    /// <param name="points">Points to append.</param>
    /// <param name="normals">Normal Vectors to append.</param>
    /// <param name="colors">Colors to append.</param>
    /// <since>6.0</since>
    public void AddRange(IEnumerable<Point3d> points, IEnumerable<Vector3d> normals, IEnumerable<Color> colors)
    {
      int point_count;
      Point3d[] point_array = Rhino.Collections.RhinoListHelpers.GetConstArray(points, out point_count);
      if (point_array == null || point_count <= 0)
        return;

      int normal_count;
      Vector3d[] normal_array = Rhino.Collections.RhinoListHelpers.GetConstArray(normals, out normal_count);
      if (normal_array == null || normal_count <= 0)
        return;

      var argb_list = new List<int>(point_count);
      foreach (var color in colors)
        argb_list.Add(color.ToArgb());
      int color_count = argb_list.Count;
      int[] argb_array = argb_list.ToArray();

      if (point_count != normal_count || point_count != color_count)
        throw new ArgumentException("Must supply equal number of points, vectors, and colors");

      IntPtr ptr_this = NonConstPointer();
      UnsafeNativeMethods.ON_PointCloud_AppendPoints4(ptr_this, point_count, point_array, normal_array, argb_array);
    }

    /// <summary>
    /// Appends a collection of points, normal vectors, colors, and extra values to this point cloud.
    /// </summary>
    /// <param name="points">Points to append.</param>
    /// <param name="normals">Normal Vectors to append.</param>
    /// <param name="colors">Colors to append.</param>
    /// <param name="values">
    /// Exta point values to append.
    /// Extra values can be used to store a user-defined values, such as intensity.
    /// </param>
    /// <since>7.5</since>
    public void AddRange(IEnumerable<Point3d> points, IEnumerable<Vector3d> normals, IEnumerable<Color> colors, IEnumerable<double> values)
    {
      int point_count;
      Point3d[] point_array = Rhino.Collections.RhinoListHelpers.GetConstArray(points, out point_count);
      if (point_array == null || point_count <= 0)
        return;

      int normal_count;
      Vector3d[] normal_array = Rhino.Collections.RhinoListHelpers.GetConstArray(normals, out normal_count);
      if (normal_array == null || normal_count <= 0)
        return;

      int value_count;
      double[] value_array = Rhino.Collections.RhinoListHelpers.GetConstArray(values, out value_count);
      if (value_array == null || value_count <= 0)
        return;

      var argb_list = new List<int>(point_count);
      foreach (var color in colors)
        argb_list.Add(color.ToArgb());
      int color_count = argb_list.Count;
      int[] argb_array = argb_list.ToArray();

      if (point_count != normal_count || point_count != color_count || point_count != value_count)
        throw new ArgumentException("Must supply equal number of points, vectors, colors, and values");

      IntPtr ptr_this = NonConstPointer();
      UnsafeNativeMethods.ON_PointCloud_AppendPoints5(ptr_this, point_count, point_array, normal_array, argb_array, value_array);
    }

    /// <summary>Inserts a new point into the point list.</summary>
    /// <param name="index">Insertion index.</param>
    /// <param name="point">Point to append.</param>
    /// <since>5.0</since>
    public void Insert(int index, Point3d point)
    {
      if (index < 0) { throw new IndexOutOfRangeException("index must be equal to or larger than zero"); }
      if (index > Count) { throw new IndexOutOfRangeException("index must be equal to or smaller than Count"); }

      IntPtr ptr_this = NonConstPointer();
      UnsafeNativeMethods.ON_PointCloud_InsertPoint1(ptr_this, index, point);
    }

    /// <summary>Inserts a new point into the point list.</summary>
    /// <param name="index">Insertion index.</param>
    /// <param name="point">Point to append.</param>
    /// <param name="normal">Normal vector of new point.</param>
    /// <since>5.0</since>
    public void Insert(int index, Point3d point, Vector3d normal)
    {
      if (index < 0) { throw new IndexOutOfRangeException("index must be equal to or larger than zero"); }
      if (index > Count) { throw new IndexOutOfRangeException("index must be equal to or smaller than Count"); }

      IntPtr ptr_this = NonConstPointer();
      UnsafeNativeMethods.ON_PointCloud_InsertPoint3(ptr_this, index, point, normal);
    }

    /// <summary>
    /// Inserts a new point into the point list.
    /// </summary>
    /// <param name="index">Insertion index.</param>
    /// <param name="point">Point to append.</param>
    /// <param name="color">Color of new point.</param>
    /// <since>5.0</since>
    public void Insert(int index, Point3d point, Color color)
    {
      if (index < 0) { throw new IndexOutOfRangeException("index must be equal to or larger than zero"); }
      if (index > Count) { throw new IndexOutOfRangeException("index must be equal to or smaller than Count"); }

      IntPtr ptr = NonConstPointer();
      UnsafeNativeMethods.ON_PointCloud_InsertPoint2(ptr, index, point, color.ToArgb());
    }

    /// <summary>
    /// Inserts a new point into the point list.
    /// </summary>
    /// <param name="index">Insertion index.</param>
    /// <param name="point">Point to append.</param>
    /// <param name="normal">Normal vector of new point.</param>
    /// <param name="color">Color of new point.</param>
    /// <since>5.0</since>
    public void Insert(int index, Point3d point, Vector3d normal, Color color)
    {
      if (index < 0) { throw new IndexOutOfRangeException("index must be equal to or larger than zero"); }
      if (index > Count) { throw new IndexOutOfRangeException("index must be equal to or smaller than Count"); }

      IntPtr ptr = NonConstPointer();
      UnsafeNativeMethods.ON_PointCloud_InsertPoint4(ptr, index, point, color.ToArgb(), normal);
    }

    /// <summary>
    /// Inserts a new point into the point list.
    /// </summary>
    /// <param name="index">Insertion index.</param>
    /// <param name="point">Point to append.</param>
    /// <param name="normal">Normal vector of new point.</param>
    /// <param name="color">Color of new point.</param>
    /// <param name="value">
    /// An extra value of new point.
    /// An extra values can be used to store a user-defined value, such as intensity.
    /// </param>
    /// <since>7.5</since>
    public void Insert(int index, Point3d point, Vector3d normal, Color color, double value)
    {
      if (index < 0) { throw new IndexOutOfRangeException("index must be equal to or larger than zero"); }
      if (index > Count) { throw new IndexOutOfRangeException("index must be equal to or smaller than Count"); }

      IntPtr ptr = NonConstPointer();
      UnsafeNativeMethods.ON_PointCloud_InsertPoint5(ptr, index, point, color.ToArgb(), normal, value);
    }

    /// <summary>
    /// Append a collection of points to this point cloud.
    /// </summary>
    /// <param name="index">Index at which to insert the new collection.</param>
    /// <param name="points">Points to append.</param>
    /// <since>5.0</since>
    public void InsertRange(int index, IEnumerable<Point3d> points)
    {
      if (index < 0) { throw new IndexOutOfRangeException("Index must be larger than or equal to zero"); }
      if (index > Count) { throw new IndexOutOfRangeException("Index must be smaller than or equal to Count"); }

      int count;
      Point3d[] point_array = Rhino.Collections.RhinoListHelpers.GetConstArray(points, out count);
      if (point_array == null)
        return;

      IntPtr ptr = NonConstPointer();
      UnsafeNativeMethods.ON_PointCloud_InsertPoints(ptr, index, count, point_array);
    }

    /// <summary>
    /// Remove the point at the given index.
    /// </summary>
    /// <param name="index">Index of point to remove.</param>
    /// <since>5.0</since>
    public void RemoveAt(int index)
    {
      if (index < 0) { throw new IndexOutOfRangeException("index must be equal to or larger than zero"); }
      if (index >= Count) { throw new IndexOutOfRangeException("index must be smaller than Count"); }

      IntPtr ptr_this = NonConstPointer();
      UnsafeNativeMethods.ON_PointCloud_RemovePoint(ptr_this, index);
    }

    /// <summary>
    /// Removes points at given indices.
    /// </summary>
    /// <param name="indices">An array of indices of the points to remove.</param>
    /// <returns>The number of points removed from the point cloud.</returns>
    /// <since>7.5</since>
    public int RemoveRange(IEnumerable<int> indices)
    {
      IntPtr ptr_this = NonConstPointer();
      using (var indexes = new SimpleArrayInt(indices))
      {
        IntPtr ptr_const_indexes = indexes.ConstPointer();
        return UnsafeNativeMethods.ON_PointCloud_RemoveRange(ptr_this, ptr_const_indexes);
      }
    }

#if RHINO_SDK
    /// <summary>
    /// Returns a ShrinkWrap mesh from this point cloud object.
    /// </summary>
    /// <param name="parameters"></param>
    /// <returns></returns>
    /// <since>8.0</since>
    public Mesh ShrinkWrap(ShrinkWrapParameters parameters)
    {
      var plugin_id = new Guid("768DD816-C492-48B4-8C1D-28665571F281");
      var obj = Rhino.RhinoApp.GetPlugInObject(plugin_id);
      if (!(obj is IShrinkWrapService sw))
        return null;

      var mesh = sw.ShrinkWrap(this, parameters);

      return mesh;
    }

    /// <summary>
    /// Creates planar curves by intersecting a plane with a point cloud.
    /// </summary>
    /// <param name="plane">The plane to intersect with</param>
    /// <param name="absoluteTolerance">The document's model absolute tolerance</param>
    /// <returns>The intersection curves if successful, an empty array if unsuccessful.</returns>
    /// <since>8.0</since>
    public Curve[] CreateSectionCurve(Plane plane, double absoluteTolerance)
    {
      return CreateSectionCurve(plane, absoluteTolerance, 0.1, 0.05, true, true, true, 0.01);
    }

    /// <summary>
    /// Creates a planar curve by intersecting a plane with a point cloud.
    /// </summary>
    /// <param name="plane">The plane to intersect with</param>
    /// <param name="absoluteTolerance">The document's model absolute tolerance</param>
    /// <param name="maxDistance">
    /// Maximum distance to plane. The thickness of the "slab" around the plane from which sample points are taken.
    /// Those sample points are projected to the section plane and a polyline is found that connects them.
    /// This distance depends on the size of the point cloud and the spacing of the points.
    /// </param>
    /// <param name="minDistance">
    /// Minimum distance between points. A threshold for the minimum spacing between adjacent sample points.
    /// If there are points closer than that, some are not used.
    /// </param>
    /// <param name="openCurves">True for open, false for closed.</param>
    /// <param name="createSpline">Creates a smooth curve. You can create both a curve and a polyline.</param>
    /// <param name="createPolyline">Creates a polyline. You can create both a curve and a polyline.</param>
    /// <param name="fitTolerance">The tolerance used to fit the curve through the polyline.</param>
    /// <returns>The intersection curves if successful, an empty array if unsuccessful.</returns>
    /// <since>8.0</since>
    public Curve[] CreateSectionCurve(Plane plane, double absoluteTolerance, double maxDistance, double minDistance, bool openCurves, bool createSpline, bool createPolyline, double fitTolerance)
    {
      if (!createSpline && !createPolyline)
        return Array.Empty<Curve>();

      using (var out_curves = new SimpleArrayCurvePointer())
      {
        IntPtr ptr_this = ConstPointer();
        IntPtr ptr_out_curves = out_curves.NonConstPointer();
        int curve_count = UnsafeNativeMethods.RHC_RhinoCreatePointCloudSection(ptr_this, ref plane, absoluteTolerance, maxDistance, minDistance, openCurves, createSpline, createPolyline, fitTolerance, ptr_out_curves);
        if (curve_count > 0)
          return out_curves.ToNonConstArray();
        return Array.Empty<Curve>();
      }
    }

    /// <summary>
    /// Creates a planar curves by intersecting a plane with a point cloud.
    /// </summary>
    /// <param name="contourStart">Start point for vector that is normal to contour plane.</param>
    /// <param name="contourEnd">End point for vector that is normal to contour plane.</param>
    /// <param name="interval">he interval or distance between contours.</param>
    /// <param name="absoluteTolerance">The document's model absolute tolerance</param>
    /// <returns>The intersection curves if successful, an empty array if unsuccessful.</returns>
    /// <since>8.0</since>
    public Curve[] CreateContourCurves(Point3d contourStart, Point3d contourEnd, double interval, double absoluteTolerance)
    {
      return CreateContourCurves(contourStart, contourEnd, interval, absoluteTolerance, 0.1, 0.05, true, true, true, 0.01);
    }

    /// <summary>
    /// Creates a planar curves by intersecting a plane with a point cloud.
    /// </summary>
    /// <param name="startPoint">Start point for vector that is normal to contour plane.</param>
    /// <param name="endPoint">End point for vector that is normal to contour plane.</param>
    /// <param name="interval">he interval or distance between contours.</param>
    /// <param name="absoluteTolerance">The document's model absolute tolerance</param>
    /// <param name="maxDistance">
    /// Maximum distance to plane. The thickness of the "slab" around the plane from which sample points are taken.
    /// Those sample points are projected to the section plane and a polyline is found that connects them.
    /// This distance depends on the size of the point cloud and the spacing of the points.
    /// </param>
    /// <param name="minDistance">
    /// Minimum distance between points. A threshold for the minimum spacing between adjacent sample points.
    /// If there are points closer than that, some are not used.
    /// </param>
    /// <param name="openCurves">True for open, false for closed.</param>
    /// <param name="createSpline">Creates a smooth curve. You can create both a curve and a polyline.</param>
    /// <param name="createPolyline">Creates a polyline. You can create both a curve and a polyline.</param>
    /// <param name="fitTolerance">The tolerance used to fit the curve through the polyline.</param>
    /// <returns>The intersection curves if successful, an empty array if unsuccessful.</returns>
    /// <since>8.0</since>
    public Curve[] CreateContourCurves(Point3d startPoint, Point3d endPoint, double interval, double absoluteTolerance, double maxDistance, double minDistance, bool openCurves, bool createSpline, bool createPolyline, double fitTolerance)
    {
      if (!createSpline && !createPolyline)
        return Array.Empty<Curve>();

      using (var out_curves = new SimpleArrayCurvePointer())
      {
        IntPtr ptr_this = ConstPointer();
        IntPtr ptr_out_curves = out_curves.NonConstPointer();
        int curve_count = UnsafeNativeMethods.RHC_RhinoCreatePointCloudContours(ptr_this, startPoint, endPoint, interval, absoluteTolerance, maxDistance, minDistance, openCurves, createSpline, createPolyline, fitTolerance, ptr_out_curves);
        if (curve_count > 0)
          return out_curves.ToNonConstArray();
        return Array.Empty<Curve>();
      }
    }

#endif

    /// <summary>
    /// Copy all the point coordinates in this point cloud to an array.
    /// </summary>
    /// <returns>An array containing all the points in this point cloud.</returns>
    /// <since>5.0</since>
    [ConstOperation]
    public Point3d[] GetPoints()
    {
      IntPtr const_ptr_this = ConstPointer();
      int count = UnsafeNativeMethods.ON_PointCloud_GetInt(const_ptr_this, idx_PointCount);
      if (count < 1)
        return new Point3d[0];

      Point3d[] rc = new Point3d[count];
      UnsafeNativeMethods.ON_PointCloud_GetPoints(const_ptr_this, count, rc);
      return rc;
    }

    /// <summary>
    /// Returns the location of the point at a specific index.
    /// </summary>
    /// <param name="index">The index.</param>
    /// <returns></returns>
    /// <since>6.0</since>
    [ConstOperation]
    public Point3d PointAt(int index)
    {
      Point3d rc = new Point3d();
      IntPtr ptr = ConstPointer();
      if (!UnsafeNativeMethods.ON_PointCloud_GetPoint(ptr, index, ref rc))
      {
        if (index < 0) { throw new IndexOutOfRangeException("index must be larger than or equal to zero"); }
        if (index >= Count) { throw new IndexOutOfRangeException("index must be smaller than the Number of points in the PointCloud"); }
      }
      return rc;
    }

    /// <summary>
    /// Copy all the normal vectors in this point cloud to an array.
    /// </summary>
    /// <returns>An array containing all the normals in this point cloud.</returns>
    /// <since>5.0</since>
    [ConstOperation]
    public Vector3d[] GetNormals()
    {
      IntPtr const_ptr_this = ConstPointer();
      int count = UnsafeNativeMethods.ON_PointCloud_GetInt(const_ptr_this, idx_NormalCount);
      if (count < 1)
        return new Vector3d[0];

      Vector3d[] rc = new Vector3d[count];
      UnsafeNativeMethods.ON_PointCloud_GetNormals(const_ptr_this, count, rc);
      return rc;
    }

    /// <summary>
    /// Copy all the point colors in this point cloud to an array.
    /// </summary>
    /// <returns>An array containing all the colors in this point cloud.</returns>
    /// <since>5.0</since>
    [ConstOperation]
    public Color[] GetColors()
    {
      IntPtr const_ptr_this = ConstPointer();
      int count = UnsafeNativeMethods.ON_PointCloud_GetInt(const_ptr_this, idx_ColorCount);
      if (count < 1)
        return new Color[0];

      int[] rc = new int[count];
      UnsafeNativeMethods.ON_PointCloud_GetColors(const_ptr_this, count, rc);

      Color[] res = new Color[count];
      for (int i = 0; i < count; i++)
        res[i] = Color.FromArgb(rc[i]);
      return res;
    }

    /// <summary>
    /// Copy all the extra point values in this point cloud to an array.
    /// Extra values can be used to store a user-defined value, such as intensity.
    /// </summary>
    /// <returns>An array containing all the extra point values in this point cloud.</returns>
    /// <since>7.5</since>
    [ConstOperation]
    public double[] GetPointValues()
    {
      IntPtr const_ptr_this = ConstPointer();
      int count = UnsafeNativeMethods.ON_PointCloud_GetInt(const_ptr_this, idx_ExtraCount);
      if (count < 1)
        return new double[0];

      double[] rc = new double[count];
      UnsafeNativeMethods.ON_PointCloud_GetExtras(const_ptr_this, count, rc);
      return rc;
    }

    /// <summary>
    /// Returns index of the closest point in the point cloud to a given test point.
    /// </summary>
    /// <param name="testPoint">.</param>
    /// <returns>Index of point in the point cloud on success. -1 on failure.</returns>
    /// <since>5.0</since>
    [ConstOperation]
    public int ClosestPoint(Point3d testPoint)
    {
      IntPtr const_ptr_this = ConstPointer();
      return UnsafeNativeMethods.ON_PointCloud_GetClosestPoint(const_ptr_this, testPoint);
    }

    /// <summary>
    /// Returns a random subsample of a point cloud.
    /// </summary>
    /// <param name="numberOfPoints">The number of points the new point cloud should contain.</param>
    /// <returns>A subsample of this point cloud if success, null otherwise.</returns>
    /// <since>7.5</since>
    [ConstOperation]
    [CLSCompliant(false)]
    public PointCloud GetRandomSubsample(uint numberOfPoints)
    {
      IntPtr ptr_const_this = ConstPointer();
      IntPtr ptr = UnsafeNativeMethods.ON_PointCloud_RandomSubsample(ptr_const_this, numberOfPoints, IntPtr.Zero, 0);
      if (IntPtr.Zero == ptr)
        return null;
      return new PointCloud(ptr, null);
    }

    /// <summary>
    /// Returns a random subsample of a point cloud.
    /// </summary>
    /// <param name="numberOfPoints">The number of points the new point cloud should contain.</param>
    /// <param name="cancelToken">The cancellation token.</param>
    /// <param name="progress">The provider for progress updates.</param>
    /// <returns>A subsample of this point cloud if success, null otherwise.</returns>
    /// <since>7.5</since>
    [ConstOperation]
    [CLSCompliant(false)]
    public PointCloud GetRandomSubsample(uint numberOfPoints, CancellationToken cancelToken, IProgress<double> progress)
    {
      IntPtr ptr_const_this = ConstPointer();

      Rhino.Runtime.Interop.MarshalProgressAndCancelToken(cancelToken, progress,
        out IntPtr ptr_terminator, out int progress_report_serial_number, out var reporter, out var terminator);

      IntPtr ptr = UnsafeNativeMethods.ON_PointCloud_RandomSubsample(ptr_const_this, numberOfPoints, ptr_terminator, progress_report_serial_number);

      if (terminator != null) terminator.Dispose();
      if (reporter != null) reporter.Disable();

      if (IntPtr.Zero == ptr)
        return null;
      return new PointCloud(ptr, null);
    }

    /// <summary>
    /// Allows the developer to obtain unsafe pointers to the underlying unmanaged data structures of the PointCloud.
    /// </summary>
    /// <param name="writable">true if user will need to write onto the structure. false otherwise.</param>
    /// <returns>A lock that needs to be released.</returns>
    /// <remarks>The lock implements the IDisposable interface, and one call of its
    /// <see cref="IDisposable.Dispose()"/> or <see cref="ReleaseUnsafeLock"/> will update the data structure as required.
    /// This can be achieved with a using statement (Using in Vb.Net).</remarks>
    /// <since>7.9</since>
    public PointCloudUnsafeLock GetUnsafeLock(bool writable)
    {
      return new PointCloudUnsafeLock(this, writable);
    }

    /// <summary>
    /// Updates the PointCloud data with the information that was stored via the <see cref="PointCloudUnsafeLock"/>.
    /// </summary>
    /// <param name="pointCloudData">The data that will be unlocked.</param>
    /// <since>7.9</since>
    public void ReleaseUnsafeLock(PointCloudUnsafeLock pointCloudData)
    {
      pointCloudData.Release();
    }

#endregion

#region IEnumerable<PointCloudItem> Members

    private class PointCloudPoints : IReadOnlyList<Point3d>, IList<Point3d>
    {
      PointCloud m_ptc;

      public PointCloudPoints(PointCloud ptc)
      {
        m_ptc = ptc;
      }

      public Point3d this[int index] => m_ptc.PointAt(index);

      Point3d IList<Point3d>.this[int index] { get => m_ptc.PointAt(index); set => throw new NotSupportedException("This view is read-only."); }

      public int Count => m_ptc.Count;

      public bool IsReadOnly => true;

      void ICollection<Point3d>.Add(Point3d item)
      {
        throw new NotSupportedException("This view is read-only.");
      }

      void ICollection<Point3d>.Clear()
      {
        throw new NotSupportedException("This view is read-only.");
      }

      public bool Contains(Point3d item)
      {
        return GenericIListImplementation.Contains(this, item);
      }

      public void CopyTo(Point3d[] array, int arrayIndex)
      {
        GenericIListImplementation.CopyTo(this, array, arrayIndex);
      }

      public IEnumerator<Point3d> GetEnumerator()
      {
        for (int i = 0; i < m_ptc.Count; i++) yield return this[i];
      }

      public int IndexOf(Point3d item)
      {
        return GenericIListImplementation.IndexOf(this, item);
      }

      void IList<Point3d>.Insert(int index, Point3d item)
      {
        throw new NotSupportedException("This view is read-only.");
      }

      bool ICollection<Point3d>.Remove(Point3d item)
      {
        throw new NotSupportedException("This view is read-only.");
      }

      void IList<Point3d>.RemoveAt(int index)
      {
        throw new NotSupportedException("This view is read-only.");
      }

      IEnumerator IEnumerable.GetEnumerator()
      {
        return GetEnumerator();
      }
    }

    /// <summary>
    /// Returns an enumerator and list indexer over point cloud locations. 
    /// </summary>
    /// <returns>The read-only list. This is a reference to the present point cloud.</returns>
    /// <since>6.0</since>
    public IReadOnlyList<Point3d> AsReadOnlyListOfPoints()
    {
      return new PointCloudPoints(this);
    }

    /// <summary>
    /// Gets an enumerator that allows to modify each point cloud point.
    /// </summary>
    /// <returns>A instance of <see cref="IEnumerator{PointCloudItem}"/>.</returns>
    /// <since>5.0</since>
    public IEnumerator<PointCloudItem> GetEnumerator()
    {
      return new PointCloudItemEnumerator(this);
    }
    /// <since>5.0</since>
    IEnumerator IEnumerable.GetEnumerator()
    {
      return GetEnumerator();
    }
    private class PointCloudItemEnumerator : IEnumerator<PointCloudItem>
    {
#region members
      private readonly PointCloud m_owner;
      int position = -1;
#endregion

#region constructor
      public PointCloudItemEnumerator(PointCloud cloud_points)
      {
        m_owner = cloud_points;
      }
#endregion

#region enumeration logic
      public bool MoveNext()
      {
        position++;
        return (position < m_owner.Count);
      }
      public void Reset()
      {
        position = -1;
      }

      public PointCloudItem Current
      {
        get
        {
          try
          {
            return m_owner[position];
          }
          catch (IndexOutOfRangeException)
          {
            throw new InvalidOperationException();
          }
        }
      }
      object IEnumerator.Current
      {
        get
        {
          try
          {
            return m_owner[position];
          }
          catch (IndexOutOfRangeException)
          {
            throw new InvalidOperationException();
          }
        }
      }
#endregion

#region IDisposable logic
      private bool m_disposed; // = false; <- initialized by runtime
      public void Dispose()
      {
        if (m_disposed) { return; }
        m_disposed = true;
        GC.SuppressFinalize(this);
      }
#endregion
    }
#endregion
  }
}

