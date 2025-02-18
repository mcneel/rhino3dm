using System;
using System.Runtime.InteropServices;
using Rhino.Runtime;

namespace Rhino.Geometry
{
  /// <summary>
  /// Represents the values of a plane, a radius and two heights -on top and beneath-
  /// that define a right circular cylinder.
  /// </summary>
  [StructLayout(LayoutKind.Sequential, Pack = 8, Size = 152)]
  [Serializable]
  public struct Cylinder : IEpsilonComparable<Cylinder>
  {
    #region members
    Circle m_basecircle;
    double m_height1;
    double m_height2;
    #endregion

    #region constants
    /// <summary>
    /// Gets an invalid Cylinder.
    /// </summary>
    /// <since>5.0</since>
    public static Cylinder Unset
    {
      get
      {
        return new Cylinder(Circle.Unset, RhinoMath.UnsetValue);
      }
    }
    #endregion

    #region constructors
    // If m_height1 == m_height2, the cylinder is infinite,
    // Otherwise, m_height1 < m_height2 and the center of
    // the "bottom" cap is 
    //   m_basecircle.plane.origin + m_height1*m_basecircle.plane.zaxis,
    // and the center of the top cap is 
    //   m_basecircle.plane.origin + m_height2*m_basecircle.plane.zaxis.

    /// <summary>
    /// Constructs a new cylinder with infinite height.
    /// </summary>
    /// <param name="baseCircle">Base circle for infinite cylinder.</param>
    /// <since>5.0</since>
    public Cylinder(Circle baseCircle)
    {
      m_basecircle = baseCircle;
      m_height1 = 0.0;
      m_height2 = 0.0;
    }

    /// <summary>
    /// Constructs a new cylinder with a finite height.
    /// </summary>
    /// <param name="baseCircle">Base circle for cylinder.</param>
    /// <param name="height">Height of cylinder (zero for infinite cylinder).</param>
    /// <example>
    /// <code source='examples\vbnet\ex_addcylinder.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_addcylinder.cs' lang='cs'/>
    /// <code source='examples\py\ex_addcylinder.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    public Cylinder(Circle baseCircle, double height)
    {
      m_basecircle = baseCircle;
      if (height > 0.0)
      {
        m_height1 = 0.0;
        m_height2 = height;
      }
      else
      {
        m_height1 = height;
        m_height2 = 0.0;
      }
    }
    #endregion

    #region properties

    /// <summary>
    /// Gets a boolean value indicating whether this cylinder is valid.
    /// <para>A valid cylinder is represented by a valid circle and two valid heights.</para>
    /// </summary>
    /// <since>5.0</since>
    public bool IsValid
    {
      get
      {
        if (!m_basecircle.IsValid) { return false; }
        if (!RhinoMath.IsValidDouble(m_height1)) { return false; }
        if (!RhinoMath.IsValidDouble(m_height2)) { return false; }

        return true;
      }
    }

    /// <summary>
    /// true if the cylinder is finite (Height0 != Height1)
    /// false if the cylinder is infinite.
    /// </summary>
    /// <since>5.0</since>
    public bool IsFinite
    {
      get
      {
        return m_height1!=m_height2;
      }
    }

    /// <summary>
    /// Gets the center point of the defining circle.
    /// </summary>
    /// <since>5.0</since>
    public Point3d Center
    {
      get
      {
        return m_basecircle.Plane.Origin;
      }
    }

    /// <summary>
    /// Gets the axis direction of the cylinder.
    /// </summary>
    /// <since>5.0</since>
    public Vector3d Axis
    {
      get
      {
        return m_basecircle.Plane.ZAxis;
      }
    }

    /// <summary>
    /// Gets the height of the cylinder. 
    /// Infinite cylinders have a height of zero, not Double.PositiveInfinity.
    /// </summary>
    /// <since>5.0</since>
    public double TotalHeight
    {
      get
      {
        return m_height2 - m_height1;
      }
    }

    /// <summary>
    /// Gets or sets the start height of the cylinder.
    /// The center of bottom cap is: BasePlane.Origin + Height1 * BasePlane.ZAxis.
    /// </summary>
    /// <since>5.0</since>
    public double Height1
    {
      get { return m_height1; }
      set { m_height1 = value; }
    }

    /// <summary>
    /// Gets or sets the end height of the cylinder. 
    /// If the end height equals the start height, the cylinder is 
    /// presumed to be infinite.
    /// The center of top cap is: BasePlane.Origin + Height2 * BasePlane.ZAxis.
    /// </summary>
    /// <since>5.0</since>
    public double Height2
    {
      get { return m_height2; }
      set { m_height2 = value; }
    }

    /// <summary>
    /// Gets or sets the radius of the cylinder.
    /// </summary>
    /// <since>6.0</since>
    public double Radius
    {
        get { return m_basecircle.Radius; }
        set { m_basecircle.Radius = value; }
    }

    /// <summary>
    /// Gets or sets the base plane of the cylinder.
    /// </summary>
    /// <since>6.0</since>
    public Plane BasePlane
    {
      get { return m_basecircle.Plane; }
      set { m_basecircle.Plane = value; }
    }
    #endregion

    #region methods

    /// <summary>
    /// Compute the circle at the given elevation parameter.
    /// </summary>
    /// <param name="linearParameter">Height parameter for circle section.</param>
    /// <since>5.0</since>
    [ConstOperation]
    public Circle CircleAt(double linearParameter)
    {
      Circle c = m_basecircle;
      if (linearParameter != 0)
        c.Translate(linearParameter * c.Plane.ZAxis);
      return c;
    }

    /// <summary>
    /// Compute the line at the given angle parameter. This line will be degenerate if the cylinder is infinite.
    /// </summary>
    /// <param name="angularParameter">Angle parameter for line section.</param>
    /// <since>5.0</since>
    [ConstOperation]
    public Line LineAt(double angularParameter)
    {
      Point3d p = m_basecircle.PointAt(angularParameter);
      Vector3d z = m_basecircle.Plane.ZAxis;
      Point3d from = p + m_height1 * z;
      Point3d to = p + m_height2 * z;
      Line line = new Line(from, to);
      return line;
    }

    /// <summary>
    /// Transforms this cylinder. Note that non-similarity preserving transformations 
    /// cannot be applied to a cylinder.
    /// </summary>
    /// <param name="xform">Transformation matrix to apply.</param>
    /// <returns>true on success, false on failure.</returns>
    /// <since>8.15</since>
    public bool Transform(Transform xform)
    {
      Circle c = m_basecircle;
      bool rc = c.Transform(xform);
      if (rc)
      {
        Point3d p = m_basecircle.Plane.Origin;
        Vector3d z = m_basecircle.Plane.ZAxis;
        Point3d from = p + m_height1 * z;
        Point3d to = p + m_height2 * z;
        
        from.Transform(xform);
        to.Transform(xform);

        Vector3d v = to - from;
        rc = v.IsTiny() || c.Normal.IsParallelTo(v, 1e-6) != 0;
        if (rc)
        {
          m_basecircle = c;
          m_height1 = m_basecircle.Plane.DistanceTo(from);
          m_height2 = m_basecircle.Plane.DistanceTo(to);
        }
      }
      return rc;
    }

    /// <summary>
    /// Constructs a Brep representation of this Cylinder. 
    /// This is synonymous with calling NurbsSurface.CreateFromCylinder().
    /// </summary>
    /// <param name="capBottom">If true, the bottom of the cylinder will be capped.</param>
    /// <param name="capTop">If true, the top of the cylinder will be capped.</param>
    /// <returns>A Brep representation of the cylinder or null.</returns>
    /// <example>
    /// <code source='examples\vbnet\ex_addcylinder.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_addcylinder.cs' lang='cs'/>
    /// <code source='examples\py\ex_addcylinder.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    [ConstOperation]
    public Brep ToBrep(bool capBottom, bool capTop)
    {
      return Brep.CreateFromCylinder(this, capBottom, capTop);
    }

    /// <summary>
    /// Constructs a Nurbs surface representation of this cylinder. 
    /// This is synonymous with calling NurbsSurface.CreateFromCylinder().
    /// </summary>
    /// <returns>A Nurbs surface representation of the cylinder or null.</returns>
    /// <since>5.0</since>
    [ConstOperation]
    public NurbsSurface ToNurbsSurface()
    {
      return NurbsSurface.CreateFromCylinder(this);
    }

    /// <summary>
    /// Constructs a RevSurface representation of this Cylinder. 
    /// This is synonymous with calling RevSurface.CreateFromCylinder().
    /// </summary>
    /// <returns>A RevSurface representation of the cylinder or null.</returns>
    /// <since>5.0</since>
    [ConstOperation]
    public RevSurface ToRevSurface()
    {
      return RevSurface.CreateFromCylinder(this);
    }
    #endregion

    /// <summary>
    /// Check that all values in other are within epsilon of the values in this
    /// </summary>
    /// <param name="other"></param>
    /// <param name="epsilon"></param>
    /// <returns></returns>
    /// <since>5.4</since>
    [ConstOperation]
    public bool EpsilonEquals(Cylinder other, double epsilon)
    {
      return m_basecircle.EpsilonEquals(other.m_basecircle, epsilon) &&
             RhinoMath.EpsilonEquals(m_height1, other.m_height1, epsilon) &&
             RhinoMath.EpsilonEquals(m_height2, other.m_height2, epsilon);
    }

//  // evaluate parameters and return point
//  ON_3dPoint PointAt(
//    double, // angular parameter [0,2pi]
//    double  // linear parameter (height from base circle's plane)
//    ) const;
//  ON_3dPoint NormalAt(
//    double, // angular parameter [0,2pi]
//    double  // linear parameter (height from base circle's plane)
//    ) const;

//  // returns parameters of point on cylinder that is closest to given point
//  bool ClosestPointTo( 
//         ON_3dPoint, 
//         double*, // angular parameter [0,2pi]
//         double*  // linear parameter (height from base circle's plane)
//         ) const;
//  // returns point on cylinder that is closest to given point
//  ON_3dPoint ClosestPointTo( 
//         ON_3dPoint 
//         ) const;

//  // For intersections see ON_Intersect();

//  // rotate cylinder about its origin
//  bool Rotate(
//        double,               // sin(angle)
//        double,               // cos(angle)
//        const ON_3dVector&  // axis of rotation
//        );
//  bool Rotate(
//        double,               // angle in radians
//        const ON_3dVector&  // axis of rotation
//        );

//  // rotate cylinder about a point and axis
//  bool Rotate(
//        double,               // sin(angle)
//        double,               // cos(angle)
//        const ON_3dVector&, // axis of rotation
//        const ON_3dPoint&   // center of rotation
//        );
//  bool Rotate(
//        double,              // angle in radians
//        const ON_3dVector&, // axis of rotation
//        const ON_3dPoint&   // center of rotation
//        );

//  bool Translate(
//        const ON_3dVector&
//        );

//  // parameterization of NURBS surface does not match cylinder's transcendental paramaterization
//  int GetNurbForm( ON_NurbsSurface& ) const; // returns 0=failure, 2=success

//  /*
//  Description:
//    Creates a surface of revolution definition of the cylinder.
//  Parameters:
//    srf - [in] if not NULL, then this srf is used.
//  Result:
//    A surface of revolution or NULL if the cylinder is not 
//    valid or is infinite.
//  */
//  ON_RevSurface* RevSurfaceForm( ON_RevSurface* srf = NULL ) const;

  }
}
