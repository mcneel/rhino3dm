using System;
using System.Runtime.InteropServices;
using Rhino.Runtime;

namespace Rhino.Geometry
{
  /// <summary>
  /// Represents the values of a plane and the two semi-axes radii in an ellipse.
  /// </summary>
  [StructLayout(LayoutKind.Sequential, Pack = 8, Size = 144)]
  [Serializable]
  public struct Ellipse : IEpsilonComparable<Ellipse>, ICloneable
  {
    #region members
    internal Plane m_plane;
    internal double m_radius1;
    internal double m_radius2;
    #endregion

    #region constructors
    /// <summary>
    /// Initializes a new ellipse from base plane and both principal radii.
    /// </summary>
    /// <param name="plane">Base plane of ellipse.</param>
    /// <param name="radius1">Ellipse radius along base plane X direction.</param>
    /// <param name="radius2">Ellipse radius along base plane Y direction.</param>
    /// <since>5.0</since>
    public Ellipse(Plane plane, double radius1, double radius2)
    {
      m_plane = plane;
      m_radius1 = radius1;
      m_radius2 = radius2;
    }

    /// <summary>
    /// Initializes a new ellipse from a center point and the two semi-axes intersections.
    /// </summary>
    /// <param name="center">A center for the ellipse. The average of the foci.</param>
    /// <param name="second">The intersection of the ellipse X axis with the ellipse itself.</param>
    /// <param name="third">A point that determines the radius along the Y semi-axis.
    /// <para>If the point is at right angle with the (center - second point) vector,
    /// it will be the intersection of the ellipse Y axis with the ellipse itself.</para>
    /// </param>
    /// <since>5.0</since>
    public Ellipse(Point3d center, Point3d second, Point3d third)
    {
      m_plane = new Plane(center, second, third);
      m_radius1 = center.DistanceTo(second);
      m_radius2 = center.DistanceTo(third);
    }
    #endregion

    #region properties
    /// <summary>
    /// Gets or sets the base plane of the ellipse.
    /// </summary>
    /// <since>5.0</since>
    public Plane Plane
    {
      get { return m_plane; }
      set { m_plane = value; }
    }

    /// <summary>
    /// Gets or sets the radius of the ellipse along the base plane X semi-axis.
    /// </summary>
    /// <since>5.0</since>
    public double Radius1
    {
      get { return m_radius1; }
      set { m_radius1 = value; }
    }

    /// <summary>
    /// Gets or sets the radius of the ellipse along the base plane Y semi-axis.
    /// </summary>
    /// <since>5.0</since>
    public double Radius2
    {
      get { return m_radius2; }
      set { m_radius2 = value; }
    }

    /// <summary>
    /// Gets or sets the center of the ellipse.
    /// </summary>
    /// <since>7.16</since>
    public Point3d Center
    {
      get { return m_plane.Origin; }
      set { m_plane.Origin = value; }
    }

    /// <summary>
    /// Gets the distance from the center to a focus.
    /// </summary>
    /// <since>7.16</since>
    public double FocalDistance
    {
      get
      {
        double[] radius = new double[] { m_radius1, m_radius2 };
        int i = (Math.Abs(radius[0]) >= Math.Abs(radius[1])) ? 0 : 1;
        double a = Math.Abs(radius[i]);
        double b = a > 0.0 ? Math.Abs(radius[1 - i]) / a : 0.0;
        return a * Math.Sqrt(1.0 - b * b);
      }
    }

    /// <summary>
    /// Returns an indication of the validity of this ellipse.
    /// </summary>
    /// <since>6.0</since>
    public bool IsValid
    {
      get
      {
        return Plane.IsValid 
          && m_radius1 > RhinoMath.ZeroTolerance 
          && m_radius2 > RhinoMath.ZeroTolerance;
      }
    }
    #endregion

    #region methods

    /// <summary>
    /// Gets the foci. The foci are two points whose sum of distances from any point on the ellipse is always the same.
    /// </summary>
    /// <param name="F1">The first focus.</param>
    /// <param name="F2">The second focus.</param>
    /// <since>7.16</since>
    public void GetFoci(out Point3d F1, out Point3d F2)
    {
      double f = FocalDistance;
      Vector3d majorAxis = (m_radius1 >= m_radius2) ? m_plane.XAxis : m_plane.YAxis;
      F1 = m_plane.Origin + f * majorAxis;
      F2 = m_plane.Origin - f * majorAxis;
    }

    /// <summary>
    /// Constructs a nurbs curve representation of this ellipse. 
    /// <para>This is equivalent to calling NurbsCurve.CreateFromEllipse().</para>
    /// </summary>
    /// <returns>A nurbs curve representation of this ellipse or null if no such representation could be made.</returns>
    /// <since>5.0</since>
    [ConstOperation]
    public NurbsCurve ToNurbsCurve()
    {
      return NurbsCurve.CreateFromEllipse(this);
    }

    /// <summary>
    /// Check that all values in other are within epsilon of the values in this
    /// </summary>
    /// <param name="other"></param>
    /// <param name="epsilon"></param>
    /// <returns></returns>
    /// <since>5.4</since>
    [ConstOperation]
    public bool EpsilonEquals(Ellipse other, double epsilon)
    {
      return m_plane.EpsilonEquals(other.m_plane, epsilon) &&
             RhinoMath.EpsilonEquals(m_radius1, other.m_radius1, epsilon) &&
             RhinoMath.EpsilonEquals(m_radius2, other.m_radius2, epsilon);
    }

    object ICloneable.Clone()
    {
      return this;
    }
    #endregion
  }
}
