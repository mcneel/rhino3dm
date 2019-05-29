using System;
using System.Runtime.InteropServices;
using Rhino.Runtime;

namespace Rhino.Geometry
{
  /// <summary>
  /// Represents the plane and radius values of a sphere.
  /// </summary>
  [StructLayout(LayoutKind.Sequential, Pack = 8, Size = 136)]
  [Serializable]
  public struct Sphere : IEpsilonComparable<Sphere>
  {
    #region members
    internal Plane m_plane;
    internal double m_radius;
    #endregion

    #region constructors
    /// <example>
    /// <code source='examples\vbnet\ex_addsphere.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_addsphere.cs' lang='cs'/>
    /// <code source='examples\py\ex_addsphere.py' lang='py'/>
    /// </example>
    /// <summary>
    /// Initializes a new sphere given center point and radius.
    /// </summary>
    /// <param name="center">A center point.</param>
    /// <param name="radius">A radius value.</param>
    public Sphere(Point3d center, double radius)
    {
      m_plane = Plane.WorldXY;
      m_plane.Origin = center;
      m_radius = radius;
    }

    /// <summary>
    /// Initializes a new sphere given the plane of the equator circle and radius.
    /// </summary>
    /// <param name="equatorialPlane">A plane that will be intesecting
    /// the sphere at the same distance from both poles (parameterization
    /// singularieties).</param>
    /// <param name="radius">A radius value.</param>
    public Sphere(Plane equatorialPlane, double radius)
    {
      m_plane = equatorialPlane;
      m_radius = radius;
    }

    /// <summary>
    /// Gets a sphere with invalid members.
    /// </summary>
    public static Sphere Unset
    {
      get { return new Sphere(Point3d.Unset, RhinoMath.UnsetValue); }
    }

#if RHINO_SDK
    /// <summary>
    /// Attempts to fit a sphere to a collection of points.
    /// </summary>
    /// <param name="points">Points to fit. The collection must contain at least two points.</param>
    /// <returns>The Sphere that best approximates the points or Sphere.Unset on failure.</returns>
    public static Sphere FitSphereToPoints(System.Collections.Generic.IEnumerable<Point3d> points)
    {
      if (points == null) { throw new ArgumentNullException("points"); }
      Rhino.Collections.Point3dList pts = new Rhino.Collections.Point3dList(points);

      if (pts.Count < 2) { return Sphere.Unset; }

      Plane plane;
      if (Plane.FitPlaneToPoints(points, out plane) == PlaneFitResult.Failure)
      { return Sphere.Unset; }

      Point3d meanP = new Point3d(0, 0, 0);
      for (int i = 0; i < pts.Count; i++)
      { meanP += pts[i]; }
      meanP /= pts.Count;

      Point3d center = meanP;
      double radius = -1;

      for (int k = 0; k < 2048; k++)
      {
        double meanL = 0.0;
        Vector3d meanD = new Vector3d(0, 0, 0);
        Point3d current = center;

        for (int i = 0; i < pts.Count; i++)
        {
          Vector3d diff = pts[i] - center;
          double length = diff.Length;

          if (length > RhinoMath.SqrtEpsilon)
          {
            meanL += length;
            meanD -= (diff / length);
          }
        }

        meanL /= pts.Count;
        meanD /= pts.Count;

        center = meanP + (meanD * meanL);
        radius = meanL;

        if (center.DistanceTo(current) < RhinoMath.SqrtEpsilon) { break; }
      }

      plane.Origin = center;
      return new Sphere(plane, radius);
    }
#endif
    #endregion

    #region properties
    /// <summary>
    /// Gets a value that indicates whether the sphere is valid.
    /// </summary>
    public bool IsValid
    {
      get
      {
        return RhinoMath.IsValidDouble(m_radius) && m_radius > 0.0 && m_plane.IsValid;
      }
    }

    /// <summary>
    /// Gets the world aligned boundingbox for this Sphere. 
    /// If the Sphere is Invalid, an empty box is returned.
    /// </summary>
    public BoundingBox BoundingBox
    {
      get
      {
        if (!m_plane.IsValid) { return BoundingBox.Empty; }
        if (!RhinoMath.IsValidDouble(m_radius)) { return BoundingBox.Empty; }

        double r = Math.Abs(m_radius);
        return new BoundingBox(m_plane.Origin - new Vector3d(r, r, r),
                               m_plane.Origin + new Vector3d(r, r, r));
      }
    }

    /// <summary>
    /// Gets or sets the diameter for this sphere.
    /// </summary>
    public double Diameter
    {
      get { return 2.0 * m_radius; }
      set { m_radius = value * 0.5; }
    }

    /// <summary>
    /// Gets or sets the Radius for this sphere.
    /// </summary>
    public double Radius
    {
      get { return m_radius; }
      set { m_radius = value; }
    }

    /// <summary>
    /// Gets or sets the Equatorial plane for this sphere.
    /// </summary>
    [Obsolete("This property has been replaced by Equ*A*torialPlane."),
     System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public Plane EquitorialPlane
    {
      get { return EquatorialPlane; }
      set { EquatorialPlane = value; }
    }

    /// <summary>
    /// Gets or sets the Equatorial plane for this sphere.
    /// </summary>
    public Plane EquatorialPlane
    {
      get
      {
        return m_plane;
      }
      set
      {
        m_plane = value;
      }
    }

    /// <summary>
    /// Gets or sets the center point of the sphere.
    /// </summary>
    public Point3d Center
    {
      get
      {
        return m_plane.Origin;
      }
      set
      {
        m_plane.Origin = value;
      }
    }

    /// <summary>
    /// Gets the point at the North Pole of the sphere.
    /// <para>This is the parameterization singularity that can be obtained,
    /// at V value +Math.Pi/2.</para>
    /// </summary>
    public Point3d NorthPole
    {
      get
      {
        return PointAt(0.0, 0.5 * Math.PI);
      }
    }

    /// <summary>
    /// Gets the point at the South Pole of the sphere.
    /// <para>This is the parameterization singularity that can be obtained,
    /// at V value -Math.Pi/2.</para>
    /// </summary>
    public Point3d SouthPole
    {
      get
      {
        return PointAt(0.0, -0.5 * Math.PI);
      }
    }
    #endregion

    #region methods
    #region evaluators
    /// <summary>
    /// Computes the parallel at a specific latitude angle.
    /// <para>The angle is specified in radians.</para>
    /// </summary>
    /// <param name="radians">An angle in radians for the parallel.</param>
    /// <returns>A circle.</returns>
    [ConstOperation]
    public Circle LatitudeRadians(double radians)
    {
      Point3d p1 = PointAt(0.0, radians);
      Point3d p2 = PointAt(0.5 * Math.PI, radians);
      Point3d p3 = PointAt(Math.PI, radians);
      return new Circle(p1, p2, p3);
    }

    /// <summary>
    /// Computes the parallel at a specific latitude angle.
    /// <para>The angle is specified in degrees.</para>
    /// </summary>
    /// <param name="degrees">An angle in degrees for the meridian.</param>
    /// <returns>A circle.</returns>
    [ConstOperation]
    public Circle LatitudeDegrees(double degrees)
    {
      return LatitudeRadians(RhinoMath.ToRadians(degrees));
    }

    /// <summary>
    /// Computes the meridian at a specific longitude angle.
    /// <para>The angle is specified in radians.</para>
    /// </summary>
    /// <param name="radians">An angle in radians.</param>
    /// <returns>A circle.</returns>
    [ConstOperation]
    public Circle LongitudeRadians(double radians)
    {
      Point3d p0 = PointAt(radians, 0.0);
      Point3d p2 = PointAt(radians + Math.PI, 0.0);
      return new Circle(p0, NorthPole, p2);
    }

    /// <summary>
    /// Computes the meridian at a specific longitude angle.
    /// <para>The angle is specified in degrees.</para>
    /// </summary>
    /// <param name="degrees">An angle in degrees.</param>
    /// <returns>A circle.</returns>
    [ConstOperation]
    public Circle LongitudeDegrees(double degrees)
    {
      return LongitudeRadians(RhinoMath.ToRadians(degrees));
    }

    /// <summary>Evaluates the sphere at specific longitude and latitude angles.</summary>
    /// <param name="longitudeRadians">A number within the interval [0, 2pi].</param>
    /// <param name="latitudeRadians">A number within the interval [-pi/2,pi/2].</param>
    /// <returns>A point value.</returns>
    [ConstOperation]
    public Point3d PointAt(double longitudeRadians, double latitudeRadians)
    {
      return m_radius * NormalAt(longitudeRadians, latitudeRadians) + m_plane.Origin;
    }

    /// <summary>
    /// Computes the normal at a specific angular location on the sphere.
    /// </summary>
    /// <param name="longitudeRadians">A number within the interval [0, 2pi].</param>
    /// <param name="latitudeRadians">A number within the interval [-pi/2, pi/2].</param>
    /// <returns>A vector.</returns>
    [ConstOperation]
    public Vector3d NormalAt(double longitudeRadians, double latitudeRadians)
    {
      return Math.Cos(latitudeRadians) * (Math.Cos(longitudeRadians) * m_plane.XAxis +
             Math.Sin(longitudeRadians) * m_plane.YAxis) +
             Math.Sin(latitudeRadians) * m_plane.ZAxis;
    }

    /// <summary>
    /// Returns point on sphere that is closest to given point.
    /// </summary>
    /// <param name="testPoint">Point to project onto Sphere.</param>
    /// <returns>Point on sphere surface closest to testPoint.</returns>
    [ConstOperation]
    public Point3d ClosestPoint(Point3d testPoint)
    {
      Vector3d v = testPoint - m_plane.Origin;
      v.Unitize();
      return m_plane.Origin + m_radius * v;
    }

    /// <summary>
    /// Finds the angle parameters on this sphere that are closest to a test point.
    /// </summary>
    /// <param name="testPoint">Point to project onto the sphere.</param>
    /// <param name="longitudeRadians">The longitudinal angle (in radians; 0.0 to 2pi) where the sphere approaches testPoint best.</param>
    /// <param name="latitudeRadians">The latitudinal angle (in radians; -0.5pi to +0.5pi) where the sphere approaches testPoint best.</param>
    /// <returns>true on success, false on failure. This function will fail if the point it coincident with the sphere center.</returns>
    [ConstOperation]
    public bool ClosestParameter(Point3d testPoint, out double longitudeRadians, out double latitudeRadians)
    {
      longitudeRadians = 0.0;
      latitudeRadians = 0.0;

      if (!testPoint.IsValid) { return false; }
      if (!IsValid) { return false; }

      //Special case origin coincidence.
      if (testPoint == m_plane.Origin) { return false; }

      double u, v;
      if (!m_plane.ClosestParameter(testPoint, out u, out v)) { return false; }
      double dist = m_plane.DistanceTo(testPoint);

      //Special case north and south-pole points.
      if ((Math.Abs(u) < 1e-64) && (Math.Abs(v) < 1e-64))
      {
        if (dist >= 0)
        { latitudeRadians = 0.5 * Math.PI; }
        else
        { latitudeRadians = -0.5 * Math.PI; }
        return true;
      }

      //Assign longitude.
      longitudeRadians = Math.Atan2(v, u);
      if (longitudeRadians < 0.0)
      { longitudeRadians = 2.0 * Math.PI + longitudeRadians; }

      //Assign latitude.
      if (dist > 1e-64)
      { latitudeRadians = 0.5 * Math.PI - Vector3d.VectorAngle(m_plane.ZAxis, testPoint - m_plane.Origin); }
      else if (dist < -1e-64)
      { latitudeRadians = -0.5 * Math.PI + Vector3d.VectorAngle(-m_plane.ZAxis, testPoint - m_plane.Origin); }
      return true;
    }
    #endregion

    /// <summary>
    /// Rotates this sphere about the center point.
    /// </summary>
    /// <param name="sinAngle">sin(angle)</param>
    /// <param name="cosAngle">cos(angle)</param>
    /// <param name="axisOfRotation">The direction of the axis of rotation.</param>
    /// <returns>true on success; false on failure.</returns>
    public bool Rotate(double sinAngle, double cosAngle, Vector3d axisOfRotation)
    {
      return Rotate(sinAngle, cosAngle, axisOfRotation, m_plane.Origin);
    }

    /// <summary>
    /// Rotates the sphere about the center point.
    /// </summary>
    /// <param name="angleRadians">Angle of rotation (in radians)</param>
    /// <param name="axisOfRotation">Rotation axis.</param>
    /// <returns>true on success; false on failure.</returns>
    public bool Rotate(double angleRadians, Vector3d axisOfRotation)
    {
      double sinAngle = Math.Sin(angleRadians);
      double cosAngle = Math.Cos(angleRadians);
      return Rotate(sinAngle, cosAngle, axisOfRotation, m_plane.Origin);
    }

    /// <summary>
    /// Rotates this sphere about a point and an axis.
    /// </summary>
    /// <param name="sinAngle">sin(angle)</param>
    /// <param name="cosAngle">cod(angle)</param>
    /// <param name="axisOfRotation">Axis of rotation.</param>
    /// <param name="centerOfRotation">Center of rotation.</param>
    /// <returns>true on success; false on failure.</returns>
    public bool Rotate(double sinAngle, double cosAngle, Vector3d axisOfRotation, Point3d centerOfRotation)
    {
      return m_plane.Rotate(sinAngle, cosAngle, axisOfRotation, centerOfRotation);
    }

    /// <summary>
    /// Rotates this sphere about a point and an axis.
    /// </summary>
    /// <param name="angleRadians">Rotation angle (in Radians)</param>
    /// <param name="axisOfRotation">Axis of rotation.</param>
    /// <param name="centerOfRotation">Center of rotation.</param>
    /// <returns>true on success; false on failure.</returns>
    public bool Rotate(double angleRadians, Vector3d axisOfRotation, Point3d centerOfRotation)
    {
      double sinAngle = Math.Sin(angleRadians);
      double cosAngle = Math.Cos(angleRadians);
      return Rotate(sinAngle, cosAngle, axisOfRotation, centerOfRotation);
    }

    /// <summary>
    /// Moves this sphere along a motion vector.
    /// </summary>
    /// <param name="delta">Motion vector.</param>
    /// <returns>true on success; false on failure.</returns>
    public bool Translate(Vector3d delta)
    {
      return m_plane.Translate(delta);
    }

    /// <summary>
    /// Transforms this sphere. Note that non-similarity preserving transformations 
    /// cannot be applied to a sphere as that would result in an ellipsoid.
    /// </summary>
    /// <param name="xform">Transformation matrix to apply.</param>
    /// <returns>true on success, false on failure.</returns>
    public bool Transform(Transform xform)
    {
      Circle xc = new Circle(m_plane, m_radius);
      bool rc = xc.Transform(xform);
      if (rc)
      {
        m_plane = xc.m_plane;
        m_radius = xc.m_radius;
      }
      return rc;
    }

    /// <summary>
    /// Converts this sphere is it Brep representation
    /// </summary>
    /// <returns></returns>
    [ConstOperation]
    public Brep ToBrep()
    {
      return Brep.CreateFromSphere(this);
    }

    /// <summary>
    /// Converts this sphere to its NurbsSurface representation. 
    /// This is synonymous with calling NurbsSurface.CreateFromSphere().
    /// </summary>
    /// <returns>A nurbs surface representation of this sphere or null.</returns>
    [ConstOperation]
    public NurbsSurface ToNurbsSurface()
    {
      return NurbsSurface.CreateFromSphere(this);
    }

    /// <summary>
    /// Converts this Sphere to a RevSurface representation. 
    /// This is synonymous with calling RevSurface.CreateFromSphere().
    /// </summary>
    /// <returns>A surface of revolution representation of this sphere or null.</returns>
    [ConstOperation]
    public RevSurface ToRevSurface()
    {
      return RevSurface.CreateFromSphere(this);
    }
    #endregion

    /// <summary>
    /// Check that all values in other are within epsilon of the values in this
    /// </summary>
    /// <param name="other"></param>
    /// <param name="epsilon"></param>
    /// <returns></returns>
    [ConstOperation]
    public bool EpsilonEquals(Sphere other, double epsilon)
    {
      return m_plane.EpsilonEquals(other.m_plane, epsilon) &&
             RhinoMath.EpsilonEquals(m_radius, other.m_radius, epsilon);
    }
  }
}