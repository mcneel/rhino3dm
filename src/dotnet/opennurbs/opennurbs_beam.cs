using System;
using System.Runtime.Serialization;
using Rhino.Runtime;

namespace Rhino.Geometry
{
  /// <summary>
  /// Represents an extrusion, or objects such as beams or linearly extruded elements,
  /// that can be represented by profile curves and two miter planes at the extremes.
  /// </summary>
  [Serializable]
  public class Extrusion : Surface
  {
    #region internals
    internal Extrusion(IntPtr nativePointer, object parent)
      : base(nativePointer, parent)
    { }

    internal override GeometryBase DuplicateShallowHelper()
    {
      return new Extrusion(IntPtr.Zero, null);
    }
    #endregion

    /// <summary>
    /// Protected serialization constructor.
    /// </summary>
    /// <param name="info">The serialization data.</param>
    /// <param name="context">The serialization context stream.</param>
    protected Extrusion(SerializationInfo info, StreamingContext context)
      : base (info, context)
    {
    }
    #region statics

    /// <summary>
    /// Creates an extrusion of a 3d curve (which must be planar) and a height.
    /// </summary>
    /// <param name="planarCurve">
    /// Planar curve used as profile
    /// </param>
    /// <param name="height">
    /// If the height &gt; 0, the bottom of the extrusion will be in plane and
    /// the top will be height units above the plane.
    /// If the height &lt; 0, the top of the extrusion will be in plane and
    /// the bottom will be height units below the plane.
    /// The plane used is the one that is returned from the curve's TryGetPlane function.
    /// </param>
    /// <param name="cap">
    /// If the curve is closed and cap is true, then the resulting extrusion is capped.
    /// </param>
    /// <returns>
    /// If the input is valid, then a new extrusion is returned. Otherwise null is returned
    /// </returns>
    /// <since>5.1</since>
    public static Extrusion Create(Curve planarCurve, double height, bool cap)
    {
      IntPtr ptr_const_curve = planarCurve.ConstPointer();
      IntPtr ptr_new_extrusion = UnsafeNativeMethods.ON_Extrusion_CreateFrom3dCurve(ptr_const_curve, height, cap);
      GC.KeepAlive(planarCurve);
      return IntPtr.Zero == ptr_new_extrusion ? null : new Extrusion(ptr_new_extrusion, null);
    }

    /// <summary>
    /// Gets an extrusion from a box.
    /// </summary>
    /// <param name="box">IsValid must be true.</param>
    /// <param name="cap">If true, the base and the top of the box will be capped. Defaults to true.</param>
    /// <returns>Extrusion on success. null on failure.</returns>
    /// <since>6.0</since>
    public static Extrusion CreateBoxExtrusion(Box box, bool cap=true)
    {
      if (!box.IsValid) return null;

      var pl = new Polyline(5)
      {
        box.PointAt(0,0,0),
        box.PointAt(1,0,0),
        box.PointAt(1,1,0),
        box.PointAt(0,1,0),
        box.PointAt(0,0,0),
      }.ToPolylineCurve();

      double height = box.m_dz.Length;
      return Create(pl, height, cap);
    }

    /// <summary>
    /// Gets an extrusion form of a cylinder.
    /// </summary>
    /// <param name="cylinder">IsFinite must be true.</param>
    /// <param name="capBottom">If true, the end at cylinder.Height1 will be capped.</param>
    /// <param name="capTop">If true, the end at cylinder.Height2 will be capped.</param>
    /// <returns>Extrusion on success. null on failure.</returns>
    /// <since>5.0</since>
    public static Extrusion CreateCylinderExtrusion(Cylinder cylinder, bool capBottom, bool capTop)
    {
      IntPtr ptr_new_extrusion = UnsafeNativeMethods.ON_Extrusion_CreateCylinder(ref cylinder, capBottom, capTop);
      return IntPtr.Zero == ptr_new_extrusion ? null : new Extrusion(ptr_new_extrusion, null);
    }

    /// <summary>
    /// Gets an extrusion form of a pipe.
    /// </summary>
    /// <param name="cylinder">IsFinite must be true.</param>
    /// <param name="otherRadius">
    /// If cylinder.Radius is less than other radius, then the cylinder will be the inside
    /// of the pipe.
    /// </param>
    /// <param name="capBottom">If true, the end at cylinder.Height1 will be capped.</param>
    /// <param name="capTop">If true, the end at cylinder.Height2 will be capped.</param>
    /// <returns>Extrusion on success. null on failure.</returns>
    /// <since>5.0</since>
    public static Extrusion CreatePipeExtrusion(Cylinder cylinder, double otherRadius, bool capTop, bool capBottom)
    {
      IntPtr ptr_new_extrusion = UnsafeNativeMethods.ON_Extrusion_CreatePipe(ref cylinder, otherRadius, capBottom, capTop);
      return IntPtr.Zero == ptr_new_extrusion ? null : new Extrusion(ptr_new_extrusion, null);
    }
    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="Extrusion"/> class.
    /// </summary>
    /// <since>5.0</since>
    public Extrusion()
    {
      IntPtr ptr = UnsafeNativeMethods.ON_Extrusion_New(IntPtr.Zero);
      ConstructNonConstObject(ptr);
    }

    /// <summary>
    /// Constructs a brep form of the extrusion. The outer profile is always the first face of the brep.
    /// If there are inner profiles, additional brep faces are created for each profile. If the
    /// outer profile is closed, then end caps are added as the last two faces of the brep.
    /// </summary>
    /// <param name="splitKinkyFaces">
    /// If true and the profiles have kinks, then the faces corresponding to those profiles are split
    /// so they will be G1.
    /// </param>
    /// <returns>A brep with a similar shape like this extrusion, or null on error.</returns>
    /// <since>5.0</since>
    [ConstOperation]
    public Brep ToBrep(bool splitKinkyFaces)
    {
      IntPtr ptr_const_this = ConstPointer();
      IntPtr ptr_brep = UnsafeNativeMethods.ON_Extrusion_BrepForm(ptr_const_this, splitKinkyFaces);
      return CreateGeometryHelper(ptr_brep, null) as Brep;
    }
    
    /// <summary>
    /// Allows to set the two points at the extremes and the up vector.
    /// </summary>
    /// <param name="a">The start point.</param>
    /// <param name="b">The end point.</param>
    /// <param name="up">The up vector.</param>
    /// <returns>true if the operation succeeded; otherwise false.
    /// Setting up=a-b will make the operation fail.</returns>
    /// <since>5.0</since>
    public bool SetPathAndUp(Point3d a, Point3d b, Vector3d up)
    {
      IntPtr ptr_this = NonConstPointer();
      return UnsafeNativeMethods.ON_Extrusion_SetPathAndUp(ptr_this, a, b, up);
    }

    /// <summary>
    /// Gets the start point of the path.
    /// </summary>
    /// <since>5.0</since>
    public Point3d PathStart
    {
      get
      {
        IntPtr ptr_const_this = ConstPointer();
        var rc = new Point3d();
        UnsafeNativeMethods.ON_Extrusion_GetPoint(ptr_const_this, true, ref rc);
        return rc;
      }
    }

    /// <summary>
    /// Gets the end point of the path.
    /// </summary>
    /// <since>5.0</since>
    public Point3d PathEnd
    {
      get
      {
        IntPtr ptr_const_this = ConstPointer();
        var rc = new Point3d();
        UnsafeNativeMethods.ON_Extrusion_GetPoint(ptr_const_this, false, ref rc);
        return rc;
      }
    }

    /// <summary>
    /// Gets the up vector of the path.
    /// </summary>
    /// <since>5.0</since>
    public Vector3d PathTangent
    {
      get
      {
        IntPtr ptr_const_this = ConstPointer();
        var rc = new Vector3d();
        UnsafeNativeMethods.ON_Extrusion_GetPathTangent(ptr_const_this, ref rc);
        return rc;
      }
    }

    /// <summary>
    /// Gets or sets the normal of the miter plane at the start in profile coordinates.
    /// In profile coordinates, 0,0,1 always maps to the extrusion axis
    /// </summary>
    /// <since>5.0</since>
    public Vector3d MiterPlaneNormalAtStart
    {
      get
      {
        IntPtr ptr_const_this = ConstPointer();
        var rc = new Vector3d();
        UnsafeNativeMethods.ON_Extrusion_GetMiterPlaneNormal(ptr_const_this, 0, ref rc);
        return rc;
      }
      set
      {
        IntPtr ptr_this = NonConstPointer();
        UnsafeNativeMethods.ON_Extrusion_SetMiterPlaneNormal(ptr_this, 0, value);
      }
    }

    /// <summary>
    /// Gets or sets the normal of the miter plane at the end in profile coordinates.
    /// In profile coordinates, 0,0,1 always maps to the extrusion axis
    /// </summary>
    /// <since>5.0</since>
    public Vector3d MiterPlaneNormalAtEnd
    {
      get
      {
        IntPtr ptr_const_this = ConstPointer();
        var rc = new Vector3d();
        UnsafeNativeMethods.ON_Extrusion_GetMiterPlaneNormal(ptr_const_this, 1, ref rc);
        return rc;
      }
      set
      {
        IntPtr ptr_this = NonConstPointer();
        UnsafeNativeMethods.ON_Extrusion_SetMiterPlaneNormal(ptr_this, 1, value);
      }
    }

    /// <summary>
    /// Returns a value indicating whether a miter plane at start is defined.
    /// </summary>
    /// <since>5.0</since>
    public bool IsMiteredAtStart
    {
      get
      {
        IntPtr ptr_const_this = ConstPointer();
        int rc = UnsafeNativeMethods.ON_Extrusion_IsMitered(ptr_const_this);
        return (1 == rc || 3 == rc);
      }
    }

    /// <summary>
    /// Gets a value indicating whether a miter plane at the end is defined.
    /// </summary>
    /// <since>5.0</since>
    public bool IsMiteredAtEnd
    {
      get
      {
        IntPtr ptr_const_this = ConstPointer();
        int rc = UnsafeNativeMethods.ON_Extrusion_IsMitered(ptr_const_this);
        return (2 == rc || 3 == rc);
      }
    }

    /// <summary>
    /// Gets a value indicating whether there is no gap among all surfaces constructing this object.
    /// </summary>
    /// <since>5.0</since>
    public override bool IsSolid
    {
      get
      {
        IntPtr ptr_const_this = ConstPointer();
        return UnsafeNativeMethods.ON_Extrusion_IsSolid(ptr_const_this);
      }
    }

    /// <summary>
    /// Gets a value indicating whether the surface that fills the bottom profile is existing.
    /// </summary>
    /// <since>5.0</since>
    public bool IsCappedAtBottom
    {
      get
      {
        IntPtr ptr_const_this = ConstPointer();
        int rc = UnsafeNativeMethods.ON_Extrusion_IsCapped(ptr_const_this);
        return (1 == rc || 3 == rc);
      }
    }

    /// <summary>
    /// Gets a value indicating whether the surface that fills the top profile is existing.
    /// </summary>
    /// <since>5.0</since>
    public bool IsCappedAtTop
    {
      get
      {
        IntPtr ptr_const_this = ConstPointer();
        int rc = UnsafeNativeMethods.ON_Extrusion_IsCapped(ptr_const_this);
        return (2 == rc || 3 == rc);
      }
    }

    /// <summary>
    /// Gets the amount of capping surfaces.
    /// </summary>
    /// <since>5.0</since>
    public int CapCount
    {
      get
      {
        IntPtr ptr_const_this = ConstPointer();
        return UnsafeNativeMethods.ON_Extrusion_CapCount(ptr_const_this);
      }
    }

    /// <summary>
    /// Gets the transformation that maps the XY profile curve to its 3d location.
    /// </summary>
    /// <param name="s">
    /// 0.0 = starting profile
    /// 1.0 = ending profile.
    /// </param>
    /// <returns>A Transformation. The transform is Invalid on failure.</returns>
    /// <since>5.0</since>
    [ConstOperation]
    public Transform GetProfileTransformation(double s)
    {
      var xform = new Transform();
      IntPtr ptr_const_this = ConstPointer();
      if (!UnsafeNativeMethods.ON_Extrusion_GetProfileTransformation(ptr_const_this, s, ref xform))
        xform = Geometry.Transform.Unset;

      return xform;
    }

    /// <summary>
    /// Gets the 3D plane containing the profile curve at a normalized path parameter.
    /// </summary>
    /// <param name="s">
    /// 0.0 = starting profile
    /// 1.0 = ending profile.
    /// </param>
    /// <returns>A plane. The plane is Invalid on failure.</returns>
    /// <remarks>
    ///  When no mitering is happening, GetPathPlane() and GetProfilePlane() return the same plane.
    /// </remarks>
    /// <since>5.0</since>
    [ConstOperation]
    public Plane GetProfilePlane(double s)
    {
      var plane = new Plane();
      IntPtr ptr_const_this = ConstPointer();
      if( !UnsafeNativeMethods.ON_Extrusion_GetPlane(ptr_const_this, true, s, ref plane) )
        plane = Plane.Unset;
      return plane;
    }

    /// <summary>
    /// Gets the 3D plane perpendicular to the path at a normalized path parameter.
    /// </summary>
    /// <param name="s">
    /// 0.0 = starting profile
    /// 1.0 = ending profile.
    /// </param>
    /// <returns>A plane. The plane is Invalid on failure.</returns>
    /// <remarks>
    ///  When no mitering is happening, GetPathPlane() and GetProfilePlane() return the same plane.
    /// </remarks>
    /// <since>5.0</since>
    [ConstOperation]
    public Plane GetPathPlane(double s)
    {
      var plane = new Plane();
      IntPtr ptr_const_this = ConstPointer();
      if( !UnsafeNativeMethods.ON_Extrusion_GetPlane(ptr_const_this, false, s, ref plane) )
        plane = Plane.Unset;
      return plane;
    }

    /// <summary>
    /// Sets the outer profile of the extrusion.
    /// </summary>
    /// <param name="outerProfile">curve in the XY plane or a 2D curve.</param>
    /// <param name="cap">
    /// If outerProfile is a closed curve, then cap determines if the extrusion
    /// has end caps. If outerProfile is an open curve, cap is ignored.
    /// </param>
    /// <returns>
    /// true if the profile was set. If the outer profile is closed, then the
    /// extrusion may also have inner profiles. If the outer profile is open,
    /// the extrusion may not have inner profiles. If the extrusion already
    /// has a profile, the set will fail.
    /// </returns>
    /// <since>5.0</since>
    public bool SetOuterProfile(Curve outerProfile, bool cap)
    {
      IntPtr ptr_this = NonConstPointer();
      IntPtr ptr_const_curve = outerProfile.ConstPointer();
      bool rc = UnsafeNativeMethods.ON_Extrusion_SetOuterProfile(ptr_this, ptr_const_curve, cap);
      GC.KeepAlive(outerProfile);
      return rc;
    }

    /// <summary>
    /// Adds an inner profile.
    /// </summary>
    /// <param name="innerProfile">Closed curve in the XY plane or a 2d curve.</param>
    /// <returns>true if the profile was set.</returns>
    /// <since>5.0</since>
    public bool AddInnerProfile(Curve innerProfile)
    {
      IntPtr ptr_this = NonConstPointer();
      IntPtr ptr_const_curve = innerProfile.ConstPointer();
      bool rc = UnsafeNativeMethods.ON_Extrusion_AddInnerProfile(ptr_this, ptr_const_curve);
      GC.KeepAlive(innerProfile);
      return rc;
    }

    /// <summary>
    /// Gets the amount of profile curves.
    /// </summary>
    /// <since>5.0</since>
    public int ProfileCount
    {
      get
      {
        IntPtr ptr_const_this = ConstPointer();
        return UnsafeNativeMethods.ON_Extrusion_ProfileCount(ptr_const_this);
      }
    }

    /// <summary>
    /// Gets a transversal isocurve of the extruded profile.
    /// </summary>
    /// <param name="profileIndex">
    /// 0 &lt;= profileIndex &lt; ProfileCount
    /// The outer profile has index 0.
    /// </param>
    /// <param name="s">
    /// 0.0 &lt;= s &lt;= 1.0
    /// A relative parameter controlling which profile is returned.
    /// 0 = bottom profile and 1 = top profile.
    /// </param>
    /// <returns>The profile.</returns>
    /// <since>5.0</since>
    [ConstOperation]
    public Curve Profile3d(int profileIndex, double s)
    {
      IntPtr ptr_const_this = ConstPointer();
      IntPtr ptr_new_curve = UnsafeNativeMethods.ON_Extrusion_Profile3d(ptr_const_this, profileIndex, s);
      return CreateGeometryHelper(ptr_new_curve, null) as Curve;
    }

    /// <summary>
    /// Gets one of the profiles.
    /// </summary>
    /// <param name="ci">The index of this profile.</param>
    /// <returns>The profile.</returns>
    /// <since>5.0</since>
    [ConstOperation]
    public Curve Profile3d(ComponentIndex ci)
    {
      IntPtr ptr_const_this = ConstPointer();
      IntPtr ptr_new_curve = UnsafeNativeMethods.ON_Extrusion_Profile3d2(ptr_const_this, ci);
      return CreateGeometryHelper(ptr_new_curve, null) as Curve;
    }

    /// <summary>
    /// Gets one of the longitudinal curves along the beam or extrusion.
    /// </summary>
    /// <param name="ci">The index of this profile.</param>
    /// <returns>The profile.</returns>
    /// <since>5.0</since>
    [ConstOperation]
    public Curve WallEdge(ComponentIndex ci)
    {
      IntPtr ptr_const_this = ConstPointer();
      IntPtr ptr_new_curve = UnsafeNativeMethods.ON_Extrusion_WallEdge(ptr_const_this, ci);
      return CreateGeometryHelper(ptr_new_curve, null) as Curve;
    }

    /// <summary>
    /// Gets one of the longitudinal surfaces of the extrusion.
    /// </summary>
    /// <param name="ci">The index specifying which precise item to retrieve.</param>
    /// <returns>The surface.</returns>
    /// <since>5.0</since>
    [ConstOperation]
    public Surface WallSurface(ComponentIndex ci)
    {
      IntPtr ptr_const_this = ConstPointer();
      IntPtr ptr_new_surface = UnsafeNativeMethods.ON_Extrusion_WallSurface(ptr_const_this, ci);
      return CreateGeometryHelper(ptr_new_surface, null) as Surface;
    }

    /// <summary>
    /// Gets the line-like curve that is the conceptual axis of the extrusion.
    /// </summary>
    /// <returns>The path as a line curve.</returns>
    /// <since>5.0</since>
    [ConstOperation]
    public LineCurve PathLineCurve()
    {
      IntPtr ptr_const_this = ConstPointer();
      IntPtr ptr_linecurve = UnsafeNativeMethods.ON_Extrusion_PathLineCurve(ptr_const_this);
      return CreateGeometryHelper(ptr_linecurve, null) as LineCurve;
    }

    /// <summary>
    /// Gets the index of the profile curve at a domain related to a parameter. 
    /// </summary>
    /// <param name="profileParameter">Parameter on profile curve.</param>
    /// <returns>
    /// -1 if profileParameter does not correspond to a point on the profile curve.
    /// When the profileParameter corresponds to the end of one profile and the
    /// beginning of the next profile, the index of the next profile is returned.
    /// </returns>
    /// <since>5.0</since>
    [ConstOperation]
    public int ProfileIndex(double profileParameter)
    {
      IntPtr ptr_const_this = ConstPointer();
      return UnsafeNativeMethods.ON_Extrusion_ProfileIndex(ptr_const_this, profileParameter);
    }

    /// <summary>
    /// Obtains a reference to a specified type of mesh for this extrusion.
    /// </summary>
    /// <param name="meshType">The mesh type.</param>
    /// <returns>A mesh.</returns>
    /// <since>5.6</since>
    [ConstOperation]
    public Mesh GetMesh(MeshType meshType)
    {
      IntPtr ptr_const_this = ConstPointer();
      IntPtr ptr_const_mesh = UnsafeNativeMethods.ON_Extrusion_GetMesh(ptr_const_this, (int)meshType);
      if (IntPtr.Zero == ptr_const_mesh)
        return null;
      return CreateGeometryHelper(ptr_const_mesh, new MeshHolder(this, meshType)) as Mesh;
    }

    //skipping
    //  const ON_PolyCurve* PolyProfile() const;
    //  int GetProfileCurves( ON_SimpleArray<const ON_Curve*>& profile_curves ) const;

#if RHINO_SDK
    /// <summary>
    /// Constructs all the Wireframe curves for this Extrusion.
    /// </summary>
    /// <returns>An array of Wireframe curves.</returns>
    /// <since>5.6</since>
    [ConstOperation]
    public Curve[] GetWireframe()
    {
      IntPtr ptr_const_this = ConstPointer();
      using (var output = new Runtime.InteropWrappers.SimpleArrayCurvePointer())
      {
        IntPtr ptr_curve_array = output.NonConstPointer();
        UnsafeNativeMethods.CRhinoExtrusionObject_GetWireFrame(ptr_const_this, ptr_curve_array);
        return output.ToNonConstArray();
      }
    }
#endif


  }
}
