using System;
using Rhino.Runtime.InteropWrappers;
using System.Collections.Generic;
using System.Linq;

#if RHINO_SDK
namespace Rhino.Geometry
{
  /// <summary>
  /// Parameters used by the 'Squish' command flattening algorithm
  /// </summary>
  public class SquishParameters : IDisposable
  {
    IntPtr m_ptr; // This class is never const
    internal IntPtr ConstPointer() { return m_ptr; }
    internal IntPtr NonConstPointer() { return m_ptr; }
    private readonly bool m_dontdelete;

    /// <summary>
    /// Initializes a new instance with default values.
    /// Initial values are same as Default.
    /// </summary>
    /// <since>5.0</since>
    public SquishParameters()
    {
      m_ptr = UnsafeNativeMethods.ON_SquishParameters_New();
      m_dontdelete = false;
    }

    /// <summary>
    /// Initializes a new instance with default values.
    /// Initial values are same as Default.
    /// This is used ONLY internally when getting the wrapper for ON_Squisher::m_sp
    /// </summary>
    /// <since>5.0</since>
    internal SquishParameters(IntPtr ptr)
    {
      m_ptr = ptr;
      m_dontdelete = true;
    }

    /// <summary>
    /// Passively reclaims unmanaged resources when the class user did not explicitly call Dispose().
    /// </summary>
    ~SquishParameters()
    {
      Dispose(false);
    }

    /// <summary>
    /// Gets the default Squishing parameters
    /// </summary>
    /// <since>7.9</since>
    public static SquishParameters Default
    {
      get
      {
        var sp = new SquishParameters();
        return sp;
      }
    }

    /// <summary>
    /// Actively reclaims unmanaged resources that this instance uses.
    /// </summary>
    /// <since>7.9</since>
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
        if (!m_dontdelete)
        {
          UnsafeNativeMethods.ON_SquishParameters_Delete(m_ptr);
        }
      }
      m_ptr = IntPtr.Zero;
    }

    #region Properties
    private bool GetBool(UnsafeNativeMethods.SquishParametersBoolConst which)
    {
      IntPtr ptr = ConstPointer();
      return UnsafeNativeMethods.ON_SquishParameters_GetBool(ptr, which);
    }
    private void SetBool(UnsafeNativeMethods.SquishParametersBoolConst which, bool val)
    {
      IntPtr ptr = NonConstPointer();
      UnsafeNativeMethods.ON_SquishParameters_SetBool(ptr, which, val);
    }

    /// <summary>
    /// the mesh has coincident vertices and PreserveTopology
    /// is true, then the flattening is based on the mesh's
    /// topology and coincident vertices will remain coincident.
    /// Otherwise coincident vertices are free to move apart.
    /// </summary>
    /// <since>7.9</since>
    public bool PreserveTopology
    {
      get 
      { 
        IntPtr ptr = ConstPointer();
        return UnsafeNativeMethods.ON_SquishParameters_GetBool(ptr, UnsafeNativeMethods.SquishParametersBoolConst.PreserveTopology);
      }
      set
      {
        IntPtr ptr = NonConstPointer();
        UnsafeNativeMethods.ON_SquishParameters_SetBool(ptr, UnsafeNativeMethods.SquishParametersBoolConst.PreserveTopology, value);
      }
    }
    /// <summary>
    /// If SaveMapping is true, then ON_SquishMesh()
    /// will save extra information on the squished mesh so 
    /// 3d points and curves near the input mesh can be mapped
    /// to the squished mesh and 2d points and curves on the 
    /// squished mesh can be mapped back to the 3d mesh.
    /// </summary>
    /// <since>7.9</since>
    public bool SaveMapping
    {
      get
      {
        IntPtr ptr = ConstPointer();
        return UnsafeNativeMethods.ON_SquishParameters_GetBool(ptr, UnsafeNativeMethods.SquishParametersBoolConst.SaveMapping);
      }
      set
      {
        IntPtr ptr = NonConstPointer();
        UnsafeNativeMethods.ON_SquishParameters_SetBool(ptr, UnsafeNativeMethods.SquishParametersBoolConst.SaveMapping, value);
      }
    }

    private double GetDouble(UnsafeNativeMethods.SquishParametersDoubleConst which)
    {
      IntPtr ptr = ConstPointer();
      return UnsafeNativeMethods.ON_SquishParameters_GetDouble(ptr, which);
    }
    private void SetDouble(UnsafeNativeMethods.SquishParametersDoubleConst which, double val)
    {
      IntPtr ptr = NonConstPointer();
      UnsafeNativeMethods.ON_SquishParameters_SetDouble(ptr, which, val);
    }

    /// <summary>
    /// Spring constant for stretched boundary edges
    /// </summary>
    /// <since>7.9</since>
    public double BoundaryStretchConstant
    {
      get
      {
        IntPtr ptr = ConstPointer();
        return UnsafeNativeMethods.ON_SquishParameters_GetDouble(ptr, UnsafeNativeMethods.SquishParametersDoubleConst.BoundaryStretchConstant);
      }
      set
      {
        IntPtr ptr = NonConstPointer();
        UnsafeNativeMethods.ON_SquishParameters_SetDouble(ptr, UnsafeNativeMethods.SquishParametersDoubleConst.BoundaryStretchConstant, value);
      }
    }

    /// <summary>
    /// Spring constant for compressed boundary edges times the rest length
    /// </summary>
    /// <since>7.9</since>
    public double BoundaryCompressConstant
    {
      get
      {
        IntPtr ptr = ConstPointer();
        return UnsafeNativeMethods.ON_SquishParameters_GetDouble(ptr, UnsafeNativeMethods.SquishParametersDoubleConst.BoundaryCompressConstant);
      }
      set
      {
        IntPtr ptr = NonConstPointer();
        UnsafeNativeMethods.ON_SquishParameters_SetDouble(ptr, UnsafeNativeMethods.SquishParametersDoubleConst.BoundaryCompressConstant, value);
      }
    }

    /// <summary>
    /// Spring constant for stretched boundary edges times the rest length
    /// </summary>
    /// <since>7.9</since>
    public double InteriorStretchConstant
    {
      get
      {
        IntPtr ptr = ConstPointer();
        return UnsafeNativeMethods.ON_SquishParameters_GetDouble(ptr, UnsafeNativeMethods.SquishParametersDoubleConst.InteriorStretchConstant);
      }
      set
      {
        IntPtr ptr = NonConstPointer();
        UnsafeNativeMethods.ON_SquishParameters_SetDouble(ptr, UnsafeNativeMethods.SquishParametersDoubleConst.InteriorStretchConstant, value);
      }
    }

    /// <summary>
    /// Spring constant for compressed interior edges times the rest length
    /// </summary>
    /// <since>7.9</since>
    public double InteriorCompressConstant
    {
      get
      {
        IntPtr ptr = ConstPointer();
        return UnsafeNativeMethods.ON_SquishParameters_GetDouble(ptr, UnsafeNativeMethods.SquishParametersDoubleConst.InteriorCompressConstant);
      }
      set
      {
        IntPtr ptr = NonConstPointer();
        UnsafeNativeMethods.ON_SquishParameters_SetDouble(ptr, UnsafeNativeMethods.SquishParametersDoubleConst.InteriorCompressConstant, value);
      }
    }

    /*
    If -1.0 <= m_absolute_limit < 0.0, then then an absolute
    compression limit is applied so that
    (2d length)/(3d length) >= fabs(m_absolute_limit).
    In particular, if m_absolute_limit = -1.0, then no compression
    is permitted (2d length) >= (3d length).

    If 0.0 < m_absolute_limit <= 1.0 then then an absolute
    stretching limit is applied so that
    (2d length)/(3d length) <= 1/fabs(m_absolute_limit).

    Examples:
      m_absolute_limit
       1.0: no stretching, (2d length) <= 1.0*(3d length)
       0.5: cap on stretching, 0.5*(2d length) <= (3d length)
      -0.5: cap on compression, (2d length) >= 0.5*(3d length)
      -1.0: no compression, (2d length) >= 1.0*(3d length)
    */
    /// <summary>
    /// If -1.0 &lt;= AnsoluteLimit &lt; 0.0, then then an absolute
    ///    compression limit is applied so that
    ///    (2d length)/(3d length) &gt;= abs(AbsoluteLimit).
    ///    In particular, Absolute = -1.0, then no compression
    ///    is permitted(2d length) &gt;= (3d length).
    /// If 0.0 &lt; m_absolute_limit &lt;= 1.0 then then an absolute
    ///    stretching limit is applied so that
    ///    (2d length)/(3d length) &lt;= 1/abs(AbsoluteLimit).
    /// Examples:
    ///     AbsoluteLimit
    ///     1.0: no stretching, (2d length) &lt;= 1.0*(3d length)
    ///     0.5: cap on stretching, 0.5*(2d length) &lt;= (3d length)
    ///     -0.5: cap on compression, (2d length) >= 0.5*(3d length)
    ///     -1.0: no compression, (2d length) >= 1.0*(3d length)
    /// </summary>
    /// <since>7.9</since>
    public double AbsoluteLimit
    {
      get
      {
        IntPtr ptr = ConstPointer();
        return UnsafeNativeMethods.ON_SquishParameters_GetDouble(ptr, UnsafeNativeMethods.SquishParametersDoubleConst.AbsoluteLimit);
      }
      set
      {
        IntPtr ptr = NonConstPointer();
        UnsafeNativeMethods.ON_SquishParameters_SetDouble(ptr, UnsafeNativeMethods.SquishParametersDoubleConst.AbsoluteLimit, value);
      }
    }

    /// <summary>
    /// The flattening agorithm to use:
    ///   Geometric: (scale independent)the "spring" constant is
    ///     proportional to 1/L^2 and the result is independent of scale.
    ///   PhysicalStress: (scale dependent) the "spring" constant is 
    ///     proportional to 1/L.    
    /// </summary>
    /// <since>7.9</since>
    public SquishFlatteningAlgorithm Algorithm
    {
      get
      {
        IntPtr ptr = ConstPointer(); 
        return (SquishFlatteningAlgorithm)UnsafeNativeMethods.ON_SquishParameters_GetMaterial(ptr); 
      }
      set
      {
        IntPtr ptr = ConstPointer();
        UnsafeNativeMethods.ON_SquishParameters_SetMaterial(ptr, (uint)value);
      }
    }
#endregion

    /// <summary>
    ///     A simplified interface for setting the Stretch and Compression spring constants 
    /// </summary>
    /// <param name="boundaryBias">
    /// boundary_bias: 0.0 to 1.0
    ///   0.0: boundary and interior treated the same
    ///   1.0: strongest bias to preserving boundary lengths at the expense of interior distortion.
    /// </param>
    /// <param name="deformationBias">
    /// deformation_bias: -1.0 to 1.0
    ///   -1.0: strongest bias in favor of compression.
    ///    0.0: no preference between compression and stretching
    ///    1.0: strongest bias in favor of stretching
    /// </param>
    /// <since>7.9</since>
    public void SetSpringConstants(double boundaryBias, double deformationBias)
    {
      IntPtr ptr = ConstPointer();
      UnsafeNativeMethods.ON_SquishParameters_SetSpringConstants(ptr, boundaryBias, deformationBias);
    }
    /// <summary>
    ///  See if the spring constants could have been set with SetSpringConstants().
    /// </summary>
    /// <param name="boundaryBias">
    /// boundary_bias: 0.0 to 1.0
    /// 0.0: boundary and interior treated the same
    /// 1.0: strongest bias to preserving boundary lengths at the expense of interior distortion.
    /// </param>
    /// <param name="deformationBias">
    /// deformation_bias: -1.0 to 1.0
    /// -1.0: strongest bias in favor of compression.
    ///  0.0: no preference between compression and stretching
    ///  1.0: strongest bias in favor of stretching
    /// </param>
    /// <returns>
    /// If the spring constants have values that could be set by
    /// calling SetSpringConstants(), then boundary_bias and
    /// deformation_bias are set to those values and this function
    /// returns true.
    /// 
    /// Otherwise, boundaryBias and deformationBias are set to 0.0
    /// and false is returned.
    /// </returns>
    /// <since>7.9</since>
    public bool GetSpringConstants(out double boundaryBias, out double deformationBias)
    {
      IntPtr ptr = ConstPointer();
      boundaryBias = 0.0; deformationBias = 0.0;
      return UnsafeNativeMethods.ON_SquishParameters_GetSpringConstants(ptr, ref boundaryBias, ref deformationBias);
    }
    /// <summary>
    /// Sets the squish deformation characteristics
    /// </summary>
    /// <param name="deformation"></param>
    /// <param name="bPreserveBoundary"></param>
    /// <param name="boundaryStretchConstant"></param>
    /// <param name="boundaryCompressConstant"></param>
    /// <param name="interiorStretchConstant"></param>
    /// <param name="interiorCompressConstant"></param>
    /// <since>7.9</since>
    public void SetDeformation(SquishDeformation deformation, bool bPreserveBoundary,
      double boundaryStretchConstant, double boundaryCompressConstant,
      double interiorStretchConstant, double interiorCompressConstant)
    {
      IntPtr ptr = NonConstPointer();
      UnsafeNativeMethods.ON_SquishParameters_SetDeformation(ptr, (int)deformation, bPreserveBoundary,
        boundaryStretchConstant, boundaryCompressConstant,
        interiorStretchConstant, interiorCompressConstant);
    }

  }

  /// <summary>
  /// class used to wrap Squish functions
  /// </summary>
  public class Squisher : IDisposable
  {
    IntPtr m_ptr; // This class is never const
    internal IntPtr ConstPointer() { return m_ptr; }
    internal IntPtr NonConstPointer() { return m_ptr; }

    /// <summary>
    /// constructs a squisher with default parameters
    /// </summary>
    /// <since>7.9</since>
    public Squisher()
    {
      m_ptr = UnsafeNativeMethods.ON_Squisher_New();
      // I knopw that throwing an exception is frowned upon in a constructor, but after tal;king with Steve Baer, the decision was:
      //     "this case is really never going to happen."
      if (null == m_ptr) throw new NullReferenceException("Could not instantiate a new Squisher.");
    }

    /// <summary>
    /// Passively reclaims unmanaged resources when the class user did not explicitly call Dispose().
    /// </summary>
    ~Squisher()
    {
      Dispose(false);
    }

    /// <summary>
    /// Actively reclaims unmanaged resources that this instance uses.
    /// </summary>
    /// <since>7.9</since>
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
        UnsafeNativeMethods.ON_SquishParameters_Delete(m_ptr);
      m_ptr = IntPtr.Zero;
    }

    /// <summary>
    /// Returns true if the input geometry was the result of a squish operation
    /// </summary>
    /// <param name="geometry"></param>
    /// <returns></returns>
    /// <since>7.9</since>
    public static bool Is2dPatternSquished(GeometryBase geometry)
    {
      if (null != geometry && UnsafeNativeMethods.ON_Check_2dPatternIsSquished(geometry.ConstPointer()))
      {
        GC.KeepAlive(geometry);
        return true;
      }
      GC.KeepAlive(geometry);
      return false;
    }

    /// <summary>
    /// Squish the given mesh into a flat mesh
    /// </summary>
    /// <param name="sp">The parameters for the squish operation</param>
    /// <param name="mesh3d">The mesh to squish</param>
    /// <param name="marks">Points, Curves, PoiuntClouds, and TextDots to squish. Can be null.</param>
    /// <param name="squished_marks_out">The squished marks. If a mark fails to squish, it will have a
    /// null entry in the list.</param>
    /// <returns>A flattened mesh</returns>
    /// <since>7.9</since>
    public Mesh SquishMesh(SquishParameters sp, Mesh mesh3d, IEnumerable<GeometryBase> marks, List<GeometryBase> squished_marks_out)
    {
      if (null == mesh3d) return null;
      IntPtr ptr_this = ConstPointer();
      IntPtr mesh_ptr = IntPtr.Zero;
      if (null != squished_marks_out) squished_marks_out.Clear();
      if (marks != null && squished_marks_out != null && marks.Count() > 0)
      {
        using (SimpleArrayGeometryPointer marks_in = new SimpleArrayGeometryPointer(marks))
        using (SimpleArrayGeometryPointer marks_out = new SimpleArrayGeometryPointer())
        {
          mesh_ptr = UnsafeNativeMethods.ON_Squisher_SquishMesh2(ptr_this, sp.ConstPointer(), mesh3d.ConstPointer(), marks_in.ConstPointer(), marks_out.NonConstPointer());
          GC.KeepAlive(sp); GC.KeepAlive(mesh3d);
          if (IntPtr.Zero == mesh_ptr)
            return null;
          var marks_out_array = marks_out.ToNonConstArray();
          squished_marks_out.AddRange(marks_out_array);
        }
      }
      else
      {
        mesh_ptr = UnsafeNativeMethods.ON_Squisher_SquishMesh(ptr_this, sp.ConstPointer(), mesh3d.ConstPointer());
        GC.KeepAlive(sp); GC.KeepAlive(mesh3d);
        if (IntPtr.Zero == mesh_ptr)
          return null;
      }
      return GeometryBase.CreateGeometryHelper(mesh_ptr, null) as Mesh;
    }

    /// <summary>
    /// Squish the given mesh into a flat mesh
    /// </summary>
    /// <param name="sp">The parameters for the squish operation</param>
    /// <param name="mesh3d">The mesh to squish</param>
    /// <returns>A flattened mesh</returns>
    /// <since>7.9</since>
    public Mesh SquishMesh(SquishParameters sp, Mesh mesh3d)
    {
      if (null == mesh3d) return null;
      IntPtr ptr_this = ConstPointer();
      IntPtr mesh_ptr = IntPtr.Zero;
      mesh_ptr = UnsafeNativeMethods.ON_Squisher_SquishMesh(ptr_this, sp.ConstPointer(), mesh3d.ConstPointer());
      GC.KeepAlive(sp); GC.KeepAlive(mesh3d);
      if (IntPtr.Zero == mesh_ptr)
        return null;
      return GeometryBase.CreateGeometryHelper(mesh_ptr, null) as Mesh;
    }
    /// <summary>
    /// Squish the surface into a flat brep
    /// </summary>
    /// <param name="sp">The parameters for the squish operation</param>
    /// <param name="surface">The surface to be squished</param>
    /// <param name="marks">Point, PountCloud, TextDot, and Curve objects to squish. Can be null.</param>
    /// <param name="squished_marks_out">A list of the squished marks, with null entires for marks
    /// that fail to squish. Can be null.</param>
    /// <returns>A brep representing the flattened surface</returns>
    /// <since>7.9</since>
    public Brep SquishSurface(SquishParameters sp, Surface surface, IEnumerable<GeometryBase> marks, List<GeometryBase> squished_marks_out)
    {
      IntPtr ptr_this = ConstPointer();
      IntPtr brep_ptr = IntPtr.Zero;
      if (null != squished_marks_out) squished_marks_out.Clear();
      if (null == surface) return null;
      if (marks != null && squished_marks_out != null && marks.Count() > 0)
      {
        using (SimpleArrayGeometryPointer marks_in = new SimpleArrayGeometryPointer(marks))
        using (SimpleArrayGeometryPointer marks_out = new SimpleArrayGeometryPointer())
        {
          brep_ptr = UnsafeNativeMethods.ON_Squisher_SquishSurface2(ptr_this, sp.ConstPointer(), surface.ConstPointer(),
            marks_in.ConstPointer(), marks_out.NonConstPointer());
          GC.KeepAlive(sp); GC.KeepAlive(surface);
          if (IntPtr.Zero == brep_ptr)
            return null;
          var marks_out_array = marks_out.ToNonConstArray();
          squished_marks_out.AddRange(marks_out_array);
        }
      }
      else
      {
        brep_ptr = UnsafeNativeMethods.ON_Squisher_SquishSurface(ptr_this, sp.ConstPointer(), surface.ConstPointer());
        GC.KeepAlive(sp); GC.KeepAlive(surface);
        if (IntPtr.Zero == brep_ptr)
          return null;
      }
      return GeometryBase.CreateGeometryHelper(brep_ptr, null) as Brep;
    }
    /// <summary>
    /// Squish the surface into a flat brep
    /// </summary>
    /// <param name="sp">The parameters for the squish operation</param>
    /// <param name="surface">The surface to be squished</param>
    /// <returns>A brep representing the flattened surface</returns>
    /// <since>7.9</since>
    public Brep SquishSurface(SquishParameters sp, Surface surface)
    {
      IntPtr ptr_this = ConstPointer();
      IntPtr brep_ptr = IntPtr.Zero;
      if (null == surface) return null;
      brep_ptr = UnsafeNativeMethods.ON_Squisher_SquishSurface(ptr_this, sp.ConstPointer(), surface.ConstPointer());
      GC.KeepAlive(sp); GC.KeepAlive(surface);
      if (IntPtr.Zero == brep_ptr)
        return null;
      return GeometryBase.CreateGeometryHelper(brep_ptr, null) as Brep;
    }

    /// <summary>
    /// Maps a point on or near the previously squished 3d surface to the flattened surface
    /// </summary>
    /// <param name="point">The point to squish</param>
    /// <param name="squishedPoint">The squished point</param>
    /// <returns>true if successful</returns>
    /// <since>7.9</since>
    public bool SquishPoint(Point3d point, out Point3d squishedPoint)
    {
      squishedPoint = new Point3d();
      IntPtr ptr_this = ConstPointer();
      var rc = UnsafeNativeMethods.ON_Squisher_SquishPoint(ptr_this, ref point, ref squishedPoint);
      return rc;
    }
    /// <summary>
    /// Maps a curve on or near a previously squished 3d surface to the resulting 2d surface
    /// </summary>
    /// <param name="curve">The curve to squish</param>
    /// <returns>The squished curve</returns>
    /// <since>7.9</since>
    public PolylineCurve SquishCurve(Curve curve)
    {
      if (null == curve) return null;
      IntPtr ptr_this = ConstPointer();
      IntPtr ptr_squished = UnsafeNativeMethods.ON_Squisher_SquishCurve(ptr_this, curve.ConstPointer());
      GC.KeepAlive(curve);
      if (ptr_squished == IntPtr.Zero) return null;
      return GeometryBase.CreateGeometryHelper(ptr_squished, null) as PolylineCurve;
    }

    /// <summary>
    /// Maps a TextDot on or near a previously squished 3d surface to the resulting 2d surface
    /// </summary>
    /// <param name="textDot">The text dot to squish</param>
    /// <returns>The resulting textDot</returns>
    /// <since>7.9</since>
    public TextDot SquishTextDot(TextDot textDot)
    {
      if (null == textDot) return null;
      IntPtr ptr_this = ConstPointer();
      IntPtr ptr_squished = UnsafeNativeMethods.ON_Squisher_SquishTextDot(ptr_this, textDot.ConstPointer());
      GC.KeepAlive(textDot);
      if (ptr_squished == IntPtr.Zero) return null;
      return GeometryBase.CreateGeometryHelper(ptr_squished, null) as TextDot;
    }
    /// <summary>
    /// Maps a PointCloud on or near a previously squished 3d surface to the resulting 2d surface
    /// </summary>
    /// <param name="pointCloud">The text dot to squish</param>
    /// <returns>The resulting PointCloud</returns>
    PointCloud SquishPointCloud(PointCloud pointCloud)
    {
      if (null == pointCloud) return null;
      IntPtr ptr_this = ConstPointer();
      IntPtr ptr_squished = UnsafeNativeMethods.ON_Squisher_SquishPointCloud(ptr_this, pointCloud.ConstPointer());
      GC.KeepAlive(pointCloud);
      if (ptr_squished == IntPtr.Zero) return null;
      return GeometryBase.CreateGeometryHelper(ptr_squished, null) as PointCloud;
    }
    /// <summary>
    /// Get the 2d mesh that results from the squish operation
    /// </summary>
    /// <returns>Returns the squished 2d mesh</returns>
    /// <since>7.9</since>
    public Mesh Get2dMesh()
    {
      IntPtr ptr_this = ConstPointer();
      IntPtr ptr_mesh = UnsafeNativeMethods.ON_Squisher_GetMesh2d(ptr_this);
      if (ptr_mesh == IntPtr.Zero) return null;
      return GeometryBase.CreateGeometryHelper(ptr_mesh, null) as Mesh;
    }
    /// <summary>
    /// Get the 3d mesh that was used for squish operation
    /// </summary>
    /// <returns>Returns the squished 2d mesh</returns>
    /// <since>7.9</since>
    public Mesh Get3dMesh()
    {
      IntPtr ptr_this = ConstPointer();
      IntPtr ptr_mesh = UnsafeNativeMethods.ON_Squisher_GetMesh3d(ptr_this);
      if (ptr_mesh == IntPtr.Zero) return null;
      return GeometryBase.CreateGeometryHelper(ptr_mesh, null) as Mesh;
    }

    /// <summary>
    /// Gets lines at the position of the mesh edges and diagonals that were constrained during the squish, in the 2d mesh.
    /// The line at any index here corresponds to the same line in GetLengthConstrained3dLines.
    /// </summary>
    /// <returns>An array of lines representing the length constraints</returns>
    /// <since>8.0</since>
    public Line[] GetLengthConstrained2dLines()
    {
      IntPtr ptr_this = ConstPointer();
      IntPtr ptr_edges = UnsafeNativeMethods.ON_Squisher_GetLengthConstrained2dLines(ptr_this);
      if (null == ptr_edges) return null;
      int count = UnsafeNativeMethods.ON_LineArray_Count(ptr_edges);
      if (count <= 0) return null;
      Line[] edges = new Line[count];
      UnsafeNativeMethods.ON_LineArray_CopyValues(ptr_edges, edges);
      return edges;
    }

    /// <summary>
    /// Gets lines at the position of the mesh edges and diagonals that were constrained during the squish, in the 2d mesh.
    /// The line at any index here corresponds to the same line in GetLengthConstrained3dLines.
    /// </summary>
    /// <returns>An array of lines representing the length constraints</returns>
    /// <since>7.9</since>
    /// <deprecated>8.0</deprecated>
    /// <remarks>Renamed to GetLengthConstrained2dLines.</remarks>
    public Line[] GetMesh2dEdges() { return GetLengthConstrained2dLines(); }

    /// <summary>
    /// Gets lines at the position of the mesh edges and diagonals that were constrained during the squish, in the 3d mesh.
    /// The line at any index here corresponds to the same line in GetLengthConstrained2dLines.
    /// </summary>
    /// <returns>An array of lines representing the length constraints</returns>
    /// <since>8.0</since>
    public Line[] GetLengthConstrained3dLines()
    {
      IntPtr ptr_this = ConstPointer();
      IntPtr ptr_edges = UnsafeNativeMethods.ON_Squisher_GetLengthConstrained3dLines(ptr_this);
      if (null == ptr_edges) return null;
      int count = UnsafeNativeMethods.ON_LineArray_Count(ptr_edges);
      if (count <= 0) return null;
      Line[] edges = new Line[count];
      UnsafeNativeMethods.ON_LineArray_CopyValues(ptr_edges, edges);
      return edges;
    }

    /// <summary>
    /// Gets lines at the position of the mesh edges and diagonals that were constrained during the squish, in the 3d mesh.
    /// The line at any index here corresponds to the same line in GetLengthConstrained2dLines.
    /// </summary>
    /// <returns>An array of lines representing the length constraints</returns>
    /// <since>7.9</since>
    /// <deprecated>8.0</deprecated>
    /// <remarks>Renamed to GetLengthConstrained3dLines.</remarks>
    public Line[] GetMesh3dEdges() { return GetLengthConstrained3dLines(); }

    /// <summary>
    /// Gets mesh vertex indices for the triangular faces that were constrained during the squish.
    /// Indices can be used in both the 2d and 3d mesh vertices arrays.
    /// </summary>
    /// <returns>An array of mesh faces</returns>
    /// <since>8.0</since>
    public MeshFace[] GetAreaConstrainedTrianglesIndices()
    {
      IntPtr ptr_this = ConstPointer();
      IntPtr ptr_faces = UnsafeNativeMethods.ON_Squisher_GetAreaConstrainedTrianglesIndices(ptr_this);
      if (null == ptr_faces) return null;
      int count = UnsafeNativeMethods.ON_MeshFaceArray_Count(ptr_faces);
      if (count <= 0) return null;
      MeshFace[] faces = new MeshFace[count];
      UnsafeNativeMethods.ON_MeshFaceArray_CopyValues(ptr_faces, faces);
      return faces;
    }

    /// <summary>
    /// Maps 2D geometry from the squished surface or mesh back to the original 3D surface or mesh
    /// </summary>
    /// <param name="squishedGeometry">The squished surface or mesh</param>
    /// <param name="marks">The input 2D geometry</param>
    /// <returns>An enumeratd list of squished marks. Individual marks that fail to squish
    /// are null in this list. Returns null on complete failure.</returns>
    /// <since>7.9</since>
    public static IEnumerable<GeometryBase> SquishBack2dMarks(GeometryBase squishedGeometry,
      IEnumerable<GeometryBase> marks)
    {
      if (null != squishedGeometry && null != marks)
      {
        var marks_out = new List<GeometryBase>(marks.Count());
        using (SimpleArrayGeometryPointer marks_in_array = new SimpleArrayGeometryPointer(marks))
        using (SimpleArrayGeometryPointer marks_out_array = new SimpleArrayGeometryPointer())
        {
          marks_out.Clear();
          var unSquishResult = UnsafeNativeMethods.ON_Squish_Back_2dMarks(squishedGeometry.ConstPointer(),
          marks_in_array.ConstPointer(), marks_out_array.NonConstPointer());
          GC.KeepAlive(squishedGeometry); 
          if (!unSquishResult)
            return null;
          marks_out.AddRange(marks_out_array.ToNonConstArray());
          return marks_out;
        }
      }
      return null;
    }
  }
}
#endif