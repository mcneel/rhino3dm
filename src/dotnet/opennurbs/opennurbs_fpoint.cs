using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Rhino.Runtime;

namespace Rhino.Geometry
{
  /// <summary>
  /// Represents the two coordinates of a point in two-dimensional space,
  /// using <see cref="Single"/>-precision floating point numbers.
  /// </summary>
  [StructLayout(LayoutKind.Sequential, Pack = 4, Size = 8)]
  [DebuggerDisplay("({m_x}, {m_y})")]
  [Serializable]
  public struct Point2f : IEquatable<Point2f>, IComparable<Point2f>, IComparable, IEpsilonFComparable<Point2f>, IValidable, IFormattable
  {
    #region members
    internal float m_x;
    internal float m_y;
    #endregion

    #region constructors
    /// <summary>
    /// Initializes a new two-dimensional point from two components.
    /// </summary>
    /// <param name="x">X component of vector.</param>
    /// <param name="y">Y component of vector.</param>
    /// <since>5.0</since>
    public Point2f(float x, float y)
    {
      m_x = x;
      m_y = y;
    }

    /// <summary>
    /// Initializes a new two-dimensional point from two double-precision floating point numbers as coordinates.
    /// <para>Coordinates will be internally converted to floating point numbers. This might cause precision loss.</para>
    /// </summary>
    /// <param name="x">X component of vector.</param>
    /// <param name="y">Y component of vector.</param>
    /// <since>5.0</since>
    public Point2f(double x, double y)
    {
      m_x = (float)x;
      m_y = (float)y;
    }

    /// <summary>
    /// Gets the standard unset point.
    /// </summary>
    /// <since>5.0</since>
    public static Point2f Unset
    {
      get
      {
        return new Point2f(RhinoMath.UnsetSingle, RhinoMath.UnsetSingle);
      }
    }
    #endregion

    #region properties
    /// <summary>
    /// Gets a value indicating whether this point is considered valid.
    /// </summary>
    /// <since>5.0</since>
    public bool IsValid
    {
      get
      {
        return RhinoMath.IsValidSingle(m_x) &&
               RhinoMath.IsValidSingle(m_y);
      }
    }

    /// <summary>
    /// Gets or sets the X (first) component of the vector.
    /// </summary>
    /// <since>5.0</since>
    public float X { get { return m_x; } set { m_x = value; } }

    /// <summary>
    /// Gets or sets the Y (second) component of the vector.
    /// </summary>
    /// <since>5.0</since>
    public float Y { get { return m_y; } set { m_y = value; } }
    #endregion

    /// <summary>
    /// Determines whether the specified System.Object is a <see cref="Point2f"/> and has the same values as the present point.
    /// </summary>
    /// <param name="obj">The specified object.</param>
    /// <returns>true if obj is Point2f and has the same coordinates as this; otherwise false.</returns>
    [ConstOperation]
    public override bool Equals(object obj)
    {
      return (obj is Point2f && this == (Point2f)obj);
    }

    /// <summary>
    /// Determines whether the specified <see cref="Point2f"/> has the same values as the present point.
    /// </summary>
    /// <param name="point">The specified point.</param>
    /// <returns>true if point has the same coordinates as this; otherwise false.</returns>
    /// <since>5.0</since>
    [ConstOperation]
    public bool Equals(Point2f point)
    {
      return this == point;
    }

    /// <summary>
    /// Check that all values in other are within epsilon of the values in this
    /// </summary>
    /// <param name="other"></param>
    /// <param name="epsilon"></param>
    /// <returns></returns>
    /// <since>5.4</since>
    [ConstOperation]
    public bool EpsilonEquals(Point2f other, float epsilon)
    {
      return RhinoMath.EpsilonEquals(m_x, other.m_x, epsilon) &&
             RhinoMath.EpsilonEquals(m_y, other.m_y, epsilon);
    }

    /// <summary>
    /// Compares this <see cref="Point2f" /> with another <see cref="Point2f" />.
    /// <para>Coordinates evaluation priority is first X, then Y.</para>
    /// </summary>
    /// <param name="other">The other <see cref="Point2f" /> to use in comparison.</param>
    /// <returns>
    /// <para> 0: if this is identical to other</para>
    /// <para>-1: if this.X &lt; other.X</para>
    /// <para>-1: if this.X == other.X and this.Y &lt; other.Y</para>
    /// <para>+1: otherwise.</para>
    /// </returns>
    /// <since>5.0</since>
    [ConstOperation]
    public int CompareTo(Point2f other)
    {
      // dictionary order
      if (m_x < other.m_x)
        return -1;
      if (m_x > other.m_x)
        return 1;

      if (m_y < other.m_y)
        return -1;
      if (m_y > other.m_y)
        return 1;

      return 0;
    }

    [ConstOperation]
    int IComparable.CompareTo(object obj)
    {
      if (obj is Point2f)
        return CompareTo((Point2f)obj);

      throw new ArgumentException("Input must be of type Point2f", "obj");
    }

    /// <summary>
    /// Computes a hash number that represents the current point.
    /// </summary>
    /// <returns>A hash code that is not unique for each point.</returns>
    [ConstOperation]
    public override int GetHashCode()
    {
      // MSDN docs recommend XOR'ing the internal values to get a hash code
      return m_x.GetHashCode() ^ m_y.GetHashCode();
    }

    /// <summary>
    /// Constructs the string representation for the current point.
    /// </summary>
    /// <returns>The point representation in the form X,Y.</returns>
    [ConstOperation]
    public override string ToString()
    {
      var culture = System.Globalization.CultureInfo.InvariantCulture;
      return String.Format("{0},{1}", m_x.ToString(culture), m_y.ToString(culture));
    }
    /// <inheritdoc />
    /// <since>7.0</since>
    [ConstOperation]
    public string ToString(string format, IFormatProvider formatProvider)
    {
      return Point3d.FormatCoordinates(format, formatProvider, m_x, m_y);
    }

    /// <summary>
    /// Accesses the coordinates of this point.
    /// </summary>
    /// <param name="index">Either 0 or 1.</param>
    /// <returns>If index is 0, the X (first) coordinate. If index is 1, the Y (second) coordinate.</returns>
    public float this[int index]
    {
      get
      {
        if (0 == index)
          return m_x;
        if (1 == index)
          return m_y;
        // IronPython works with indexing is we thrown an IndexOutOfRangeException
        throw new IndexOutOfRangeException();
      }
      set
      {
        if (0 == index)
          X = value;
        else if (1 == index)
          Y = value;
        else
          throw new IndexOutOfRangeException();
      }
    }


    /// <summary>
    /// Determines whether two <see cref="Point2f"/> have equal values.
    /// </summary>
    /// <param name="a">The first point.</param>
    /// <param name="b">The second point.</param>
    /// <returns>true if the coordinates of the two points are exactly equal; otherwise false.</returns>
    /// <since>5.0</since>
    public static bool operator ==(Point2f a, Point2f b)
    {
      return (a.m_x == b.m_x && a.m_y == b.m_y);
    }

    /// <summary>
    /// Determines whether two <see cref="Point2f"/> have different values.
    /// </summary>
    /// <param name="a">The first point.</param>
    /// <param name="b">The second point.</param>
    /// <returns>true if the two points differ in any coordinate; false otherwise.</returns>
    /// <since>5.0</since>
    public static bool operator !=(Point2f a, Point2f b)
    {
      return (a.m_x != b.m_x || a.m_y != b.m_y);
    }

    /// <summary>
    /// Determines whether the first specified <see cref="Point2f"/> comes before
    /// (has inferior sorting value than) the second point.
    /// <para>Coordinates evaluation priority is first X, then Y.</para>
    /// </summary>
    /// <param name="a">First point.</param>
    /// <param name="b">Second point.</param>
    /// <returns>true if a.X is smaller than b.X, or a.X == b.X and a.Y is smaller than b.Y; otherwise, false.</returns>
    /// <since>5.0</since>
    public static bool operator <(Point2f a, Point2f b)
    {
      return (a.X < b.X) || (a.X == b.X && a.Y < b.Y);
    }

    /// <summary>
    /// Determines whether the first specified <see cref="Point2f"/> comes before
    /// (has inferior sorting value than) the second point, or it is equal to it.
    /// <para>Coordinates evaluation priority is first X, then Y.</para>
    /// </summary>
    /// <param name="a">First point.</param>
    /// <param name="b">Second point.</param>
    /// <returns>true if a.X is smaller than b.X, or a.X == b.X and a.Y &lt;= b.Y; otherwise, false.</returns>
    /// <since>5.0</since>
    public static bool operator <=(Point2f a, Point2f b)
    {
      return (a.X < b.X) || (a.X == b.X && a.Y <= b.Y);
    }

    /// <summary>
    /// Determines whether the first specified <see cref="Point2f"/> comes after
    /// (has superior sorting value than) the second point.
    /// <para>Coordinates evaluation priority is first X, then Y.</para>
    /// </summary>
    /// <param name="a">First point.</param>
    /// <param name="b">Second point.</param>
    /// <returns>true if a.X is larger than b.X, or a.X == b.X and a.Y is larger than b.Y; otherwise, false.</returns>
    /// <since>5.0</since>
    public static bool operator >(Point2f a, Point2f b)
    {
      return (a.X > b.X) || (a.X == b.X && a.Y > b.Y);
    }

    /// <summary>
    /// Determines whether the first specified <see cref="Point2f"/> comes after
    /// (has superior sorting value than) the second point, or it is equal to it.
    /// <para>Coordinates evaluation priority is first X, then Y.</para>
    /// </summary>
    /// <param name="a">First point.</param>
    /// <param name="b">Second point.</param>
    /// <returns>true if a.X is larger than b.X, or a.X == b.X and a.Y &gt;= b.Y; otherwise, false.</returns>
    /// <since>5.0</since>
    public static bool operator >=(Point2f a, Point2f b)
    {
      return (a.X > b.X) || (a.X == b.X && a.Y >= b.Y);
    }

    /// <summary>
    /// Converts a double-precision point in a single-precision point.
    /// Needs explicit casting to help retain precision.
    /// </summary>
    /// <param name="point">A point.</param>
    /// <returns>The resulting point.</returns>
    public static explicit operator Point2f(Point2d point)
    {
      return new Point2f((float)point.X, (float)point.Y);
    }

    /// <summary>
    /// Sums two <see cref="Point2f"/>s.
    /// </summary>
    /// <param name="a">First point.</param>
    /// <param name="b">Second point.</param>
    /// <returns>An added Vector2f.</returns>
    public static Vector2f operator +(Point2f a, Point2f b)
    {
      return new Vector2f(a.m_x + b.m_x, a.m_y + b.m_y);
    }

    /// <summary>
    /// Subtracts two <see cref="Point2f"/>s.
    /// </summary>
    /// <param name="a">First point.</param>
    /// <param name="b">Second point.</param>
    /// <returns>A subtracted Vector2f.</returns>
    /// <since>6.0</since>
    public static Vector2f operator -(Point2f a, Point2f b)
    {
      return new Vector2f(a.m_x - b.m_x, a.m_y - b.m_y);
    }

    /// <summary>
    /// Multiplies a <see cref="Point2f"/> by a scalar.
    /// </summary>
    /// <param name="a">First point.</param>
    /// <param name="b">Scalar.</param>
    /// <returns>A multiplied Point2f.</returns>
    /// <since>6.0</since>
    public static Point2f operator *(Point2f a, float b)
    {
      return new Point2f(a.m_x * b, a.m_y * b);
    }

    /// <summary>
    /// Divides a <see cref="Point2f"/> by a scalar.
    /// </summary>
    /// <param name="a">First point.</param>
    /// <param name="b">Scalar.</param>
    /// <returns>A divided Point2f.</returns>
    /// <since>6.0</since>
    public static Point2f operator /(Point2f a, float b)
    {
      return new Point2f(a.m_x / b, a.m_y / b);
    }

    /// <summary>
    /// Computes the distance between two points.
    /// </summary>
    /// <param name="other">Other point for distance measurement.</param>
    /// <returns>The length of the line between this and the other point; or 0 if any of the points is not valid.</returns>
    [ConstOperation]
    public double DistanceTo(Point2f other)
    {
      double d = 0.0;
      if (IsValid && other.IsValid)
      {
        double dx = other.m_x - m_x;
        double dy = other.m_y - m_y;
        d = Vector2d.GetLengthHelper(dx, dy);
      }
      return d;
    }

    /// <summary>
    /// Computes the squared distance between two points.
    /// </summary>
    /// <param name="other">Other point for distance measurement.</param>
    /// <returns>The squared length of the line between this and the other point; or 0 if any of the points is not valid.</returns>
    /// <since>7.14</since>
    [ConstOperation]
    public double DistanceToSquared(Point2f other)
    {
      double d;
      if (IsValid && other.IsValid)
      {
        d = (this - other).SquareLength;
      }
      else
      {
        d = 0.0;
      }
      return d;
    }
  }

  /// <summary>
  /// Represents the three coordinates of a point in three-dimensional space,
  /// using <see cref="Single"/>-precision floating point numbers.
  /// </summary>
  // holding off on making this IComparable until I understand all
  // of the rules that FxCop states about IComparable classes
  [StructLayout(LayoutKind.Sequential, Pack = 4, Size = 12)]
  [DebuggerDisplay("({m_x}, {m_y}, {m_z})")]
  [Serializable]
  public struct Point3f : IEquatable<Point3f>, IComparable<Point3f>, IComparable, IEpsilonFComparable<Point3f>, IValidable, IFormattable
  {
    internal float m_x;
    internal float m_y;
    internal float m_z;

    /// <summary>
    /// Initializes a new two-dimensional vector from two components.
    /// </summary>
    /// <param name="x">X component of vector.</param>
    /// <param name="y">Y component of vector.</param>
    /// <param name="z">Z component of vector.</param>
    /// <since>5.0</since>
    public Point3f(float x, float y, float z)
    {
      m_x = x;
      m_y = y;
      m_z = z;
    }

    /// <summary>
    /// Gets the value of a point at location 0,0,0.
    /// </summary>
    /// <since>5.0</since>
    public static Point3f Origin
    {
      get { return new Point3f(0f, 0f, 0f); }
    }

    /// <summary>
    /// Gets the value of a point at location RhinoMath.UnsetValue,RhinoMath.UnsetValue,RhinoMath.UnsetValue.
    /// </summary>
    /// <since>5.0</since>
    public static Point3f Unset
    {
      get { return new Point3f(RhinoMath.UnsetSingle, RhinoMath.UnsetSingle, RhinoMath.UnsetSingle); }
    }

    /// <summary>
    /// Gets or sets the X (first) component of the vector.
    /// </summary>
    /// <since>5.0</since>
    public float X { get { return m_x; } set { m_x = value; } }

    /// <summary>
    /// Gets or sets the Y (second) component of the vector.
    /// </summary>
    /// <since>5.0</since>
    public float Y { get { return m_y; } set { m_y = value; } }

    /// <summary>
    /// Gets or sets the Z (third) component of the vector.
    /// </summary>
    /// <since>5.0</since>
    public float Z { get { return m_z; } set { m_z = value; } }

    /// <summary>
    /// Determines whether the specified System.Object is a Point3f and has the same values as the present point.
    /// </summary>
    /// <param name="obj">The specified object.</param>
    /// <returns>true if obj is Point3f and has the same coordinates as this; otherwise false.</returns>
    [ConstOperation]
    public override bool Equals(object obj)
    {
      return (obj is Point3f && this == (Point3f)obj);
    }

    /// <summary>
    /// Determines whether the specified Point3f has the same values as the present point.
    /// </summary>
    /// <param name="point">The specified point.</param>
    /// <returns>true if point has the same coordinates as this; otherwise false.</returns>
    /// <since>5.0</since>
    [ConstOperation]
    public bool Equals(Point3f point)
    {
      return this == point;
    }

    /// <summary>
    /// Check that all values in other are within epsilon of the values in this
    /// </summary>
    /// <param name="other"></param>
    /// <param name="epsilon"></param>
    /// <returns></returns>
    /// <since>5.4</since>
    [ConstOperation]
    public bool EpsilonEquals(Point3f other, float epsilon)
    {
      return RhinoMath.EpsilonEquals(m_x, other.m_x, epsilon) &&
             RhinoMath.EpsilonEquals(m_y, other.m_y, epsilon) &&
             RhinoMath.EpsilonEquals(m_z, other.m_z, epsilon);
    }

    /// <summary>
    /// Compares this <see cref="Point3f" /> with another <see cref="Point3f" />.
    /// <para>Component evaluation priority is first X, then Y, then Z.</para>
    /// </summary>
    /// <param name="other">The other <see cref="Point3d" /> to use in comparison.</param>
    /// <returns>
    /// <para> 0: if this is identical to other</para>
    /// <para>-1: if this.X &lt; other.X</para>
    /// <para>-1: if this.X == other.X and this.Y &lt; other.Y</para>
    /// <para>-1: if this.X == other.X and this.Y == other.Y and this.Z &lt; other.Z</para>
    /// <para>+1: otherwise.</para>
    /// </returns>
    /// <since>5.0</since>
    [ConstOperation]
    public int CompareTo(Point3f other)
    {
      if (m_x < other.m_x)
        return -1;
      if (m_x > other.m_x)
        return 1;

      if (m_y < other.m_y)
        return -1;
      if (m_y > other.m_y)
        return 1;

      if (m_z < other.m_z)
        return -1;
      if (m_z > other.m_z)
        return 1;

      return 0;
    }

    [ConstOperation]
    int IComparable.CompareTo(object obj)
    {
      if (obj is Point3f)
        return CompareTo((Point3f)obj);

      throw new ArgumentException("Input must be of type Point3f", "obj");
    }

    /// <summary>
    /// Computes a hash code for the present point.
    /// </summary>
    /// <returns>A non-unique integer that represents this point.</returns>
    [ConstOperation]
    public override int GetHashCode()
    {
      // MSDN docs recommend XOR'ing the internal values to get a hash code
      return m_x.GetHashCode() ^ m_y.GetHashCode() ^ m_z.GetHashCode();
    }

    /// <summary>
    /// Constructs the string representation for the current point.
    /// </summary>
    /// <returns>The point representation in the form X,Y,Z.</returns>
    [ConstOperation]
    public override string ToString()
    {
      return String.Format(System.Globalization.CultureInfo.InvariantCulture, "{0},{1},{2}", m_x, m_y, m_z);
    }
    /// <inheritdoc />
    /// <since>7.0</since>
    [ConstOperation]
    public string ToString(string format, IFormatProvider formatProvider)
    {
      return Point3d.FormatCoordinates(format, formatProvider, m_x, m_y, m_z);
    }

    /// <summary>
    /// Each coordinate of the point must pass the <see cref="RhinoMath.IsValidSingle"/> test.
    /// </summary>
    /// <since>5.0</since>
    public bool IsValid
    {
      get { return RhinoMath.IsValidSingle(m_x) && RhinoMath.IsValidSingle(m_y) && RhinoMath.IsValidSingle(m_z); }
    }

    /// <summary>
    /// Computes the distance between two points.
    /// </summary>
    /// <param name="other">Other point for distance measurement.</param>
    /// <returns>The length of the line between this and the other point; or 0 if any of the points is not valid.</returns>
    /// <example>
    /// <code source='examples\vbnet\ex_intersectcurves.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_intersectcurves.cs' lang='cs'/>
    /// <code source='examples\py\ex_intersectcurves.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    [ConstOperation]
    public double DistanceTo(Point3f other)
    {
      double d = 0.0;
      if (IsValid && other.IsValid)
      {
        double dx = other.m_x - m_x;
        double dy = other.m_y - m_y;
        double dz = other.m_z - m_z;
        d = Vector3d.GetLengthHelper(dx, dy, dz);
      }
      return d;
    }

    /// <summary>
    /// Computes the squared distance between two points.
    /// </summary>
    /// <param name="other">Other point for distance measurement.</param>
    /// <returns>The squared length of the line between this and the other point; or 0 if any of the points is not valid.</returns>
    /// <since>7.14</since>
    [ConstOperation]
    public double DistanceToSquared(Point3f other)
    {
      double d;
      if (IsValid && other.IsValid)
      {
        d = (this - other).SquareLength;
      }
      else
      {
        d = 0.0;
      }
      return d;
    }

    /// <summary>
    /// Transforms the present point in place. The transformation matrix acts on the left of the point. i.e.,
    /// <para>result = transformation*point</para>
    /// </summary>
    /// <param name="xform">Transformation to apply.</param>
    /// <since>5.0</since>
    public void Transform(Transform xform)
    {
      //David: this method doesn't test for validity. Should it?
      double ww = xform.m_30 * m_x + xform.m_31 * m_y + xform.m_32 * m_z + xform.m_33;
      if (ww != 0.0) { ww = 1.0 / ww; }

      double tx = ww * (xform.m_00 * m_x + xform.m_01 * m_y + xform.m_02 * m_z + xform.m_03);
      double ty = ww * (xform.m_10 * m_x + xform.m_11 * m_y + xform.m_12 * m_z + xform.m_13);
      double tz = ww * (xform.m_20 * m_x + xform.m_21 * m_y + xform.m_22 * m_z + xform.m_23);
      m_x = (float)tx;
      m_y = (float)ty;
      m_z = (float)tz;
    }

    /// <summary>
    /// Determines whether two points have equal values.
    /// </summary>
    /// <param name="a">The first point.</param>
    /// <param name="b">The second point.</param>
    /// <returns>true if the coordinates of the two points are exactly equal; otherwise false.</returns>
    /// <since>5.0</since>
    public static bool operator ==(Point3f a, Point3f b)
    {
      return (a.m_x == b.m_x && a.m_y == b.m_y && a.m_z == b.m_z);
    }

    /// <summary>
    /// Determines whether two points have different values.
    /// </summary>
    /// <param name="a">The first point.</param>
    /// <param name="b">The second point.</param>
    /// <returns>true if the two points differ in any coordinate; false otherwise.</returns>
    /// <since>5.0</since>
    public static bool operator !=(Point3f a, Point3f b)
    {
      return (a.m_x != b.m_x || a.m_y != b.m_y || a.m_z != b.m_z);
    }

    /// <summary>
    /// Determines whether the first specified point comes before (has inferior sorting value than) the second point.
    /// <para>Coordinates evaluation priority is first X, then Y, then Z.</para>
    /// </summary>
    /// <param name="a">The first point.</param>
    /// <param name="b">The second point.</param>
    /// <returns>true if a.X is smaller than b.X,
    /// or a.X == b.X and a.Y is smaller than b.Y,
    /// or a.X == b.X and a.Y == b.Y and a.Z is smaller than b.Z;
    /// otherwise, false.</returns>
    /// <since>5.0</since>
    public static bool operator <(Point3f a, Point3f b)
    {
      return a.CompareTo(b) < 0;
    }

    /// <summary>
    /// Determines whether the first specified point comes before
    /// (has inferior sorting value than) the second point, or it is equal to it.
    /// <para>Coordinates evaluation priority is first X, then Y, then Z.</para>
    /// </summary>
    /// <param name="a">The first point.</param>
    /// <param name="b">The second point.</param>
    /// <returns>true if a.X is smaller than b.X,
    /// or a.X == b.X and a.Y is smaller than b.Y,
    /// or a.X == b.X and a.Y == b.Y and a.Z &lt;= b.Z;
    /// otherwise, false.</returns>
    /// <since>5.0</since>
    public static bool operator <=(Point3f a, Point3f b)
    {
      return a.CompareTo(b) <= 0;
    }

    /// <summary>
    /// Determines whether the first specified point comes after (has superior sorting value than) the second point.
    /// <para>Coordinates evaluation priority is first X, then Y, then Z.</para>
    /// </summary>
    /// <param name="a">The first point.</param>
    /// <param name="b">The second point.</param>
    /// <returns>true if a.X is larger than b.X,
    /// or a.X == b.X and a.Y is larger than b.Y,
    /// or a.X == b.X and a.Y == b.Y and a.Z is larger than b.Z;
    /// otherwise, false.</returns>
    /// <since>5.0</since>
    public static bool operator >(Point3f a, Point3f b)
    {
      return a.CompareTo(b) > 0;
    }

    /// <summary>
    /// Determines whether the first specified point comes after
    /// (has superior sorting value than) the second point, or it is equal to it.
    /// <para>Coordinates evaluation priority is first X, then Y, then Z.</para>
    /// </summary>
    /// <param name="a">The first point.</param>
    /// <param name="b">The second point.</param>
    /// <returns>true if a.X is larger than b.X,
    /// or a.X == b.X and a.Y is larger than b.Y,
    /// or a.X == b.X and a.Y == b.Y and a.Z &gt;= b.Z;
    /// otherwise, false.</returns>
    /// <since>5.0</since>
    public static bool operator >=(Point3f a, Point3f b)
    {
      return a.CompareTo(b) >= 0;
    }

    /// <summary>
    /// Subtracts a point from another point.
    /// </summary>
    /// <param name="point1">A point.</param>
    /// <param name="point2">Another point.</param>
    /// <returns>A new vector that is the difference of point minus vector.</returns>
    /// <since>5.0</since>
    public static Vector3f operator -(Point3f point1, Point3f point2)
    {
      return new Vector3f(point1.m_x - point2.m_x, point1.m_y - point2.m_y, point1.m_z - point2.m_z);
    }

    /// <summary>
    /// Adds a point to another point.
    /// </summary>
    /// <param name="point1">A point.</param>
    /// <param name="point2">Another point.</param>
    /// <returns>A new vector that is the sum of point1 plus point2.</returns>
    public static Point3f operator +(Point3f point1, Point3f point2)
    {
      return new Point3f(point1.m_x + point2.m_x, point1.m_y + point2.m_y, point1.m_z + point2.m_z);
    }

    /// <summary>
    /// Subtracts a point from another point.
    /// <para>(Provided for languages that do not support operator overloading. You can use the - operator otherwise)</para>
    /// </summary>
    /// <param name="point1">A point.</param>
    /// <param name="point2">Another point.</param>
    /// <returns>A new vector that is the difference of point minus vector.</returns>
    /// <since>5.0</since>
    public static Vector3f Subtract(Point3f point1, Point3f point2)
    {
      return new Vector3f(point1.m_x - point2.m_x, point1.m_y - point2.m_y, point1.m_z - point2.m_z);
    }

    /// <summary>
    /// Converts a double-precision point in a single-precision point.
    /// Needs explicit casting to help retain precision.
    /// </summary>
    /// <param name="point">A point.</param>
    /// <returns>The resulting point.</returns>
    public static explicit operator Point3f(Point3d point)
    {
      return new Point3f((float)point.X, (float)point.Y, (float)point.Z);
    }

    /// <summary>
    /// Converts a single-precision point in a single-precision vector.
    /// Needs explicit casting to help retain precision.
    /// </summary>
    /// <param name="point">A point.</param>
    /// <returns>The resulting point.</returns>
    public static explicit operator Vector3f(Point3f point)
    {
      return new Vector3f(point.X, point.Y, point.Z);
    }

    /// <summary>
    /// Multiplies a point by a factor.
    /// </summary>
    /// <param name="point">A point.</param>
    /// <param name="value">A value.</param>
    /// <returns>A new vector that is the multiplication of point by value.</returns>
    /// <since>6.0</since>
    public static Point3f operator *(Point3f point, float value)
    {
      return new Point3f(point.m_x * value, point.m_y * value, point.m_z * value);
    }

    /// <summary>
    /// Multiplies a point by a factor.
    /// </summary>
    /// <param name="point">A point.</param>
    /// <param name="value">A value.</param>
    /// <returns>A new vector that is the multiplication of point by value.</returns>
    /// <since>6.0</since>
    public static Point3f operator *(float value, Point3f point)
    {
      return new Point3f(point.m_x * value, point.m_y * value, point.m_z * value);
    }
  }

  //skipping ON_4fPoint. I don't think I've ever seen this used in Rhino.

  /// <summary>
  /// Represents the two components of a vector in two-dimensional space,
  /// using <see cref="Single"/>-precision floating point numbers.
  /// </summary>
  [StructLayout(LayoutKind.Sequential, Pack = 4, Size = 8)]
  [DebuggerDisplay("({m_x}, {m_y})")]
  [Serializable]
  public struct Vector2f : IEquatable<Vector2f>, IComparable<Vector2f>, IComparable, IEpsilonFComparable<Vector2f>, IValidable, IFormattable
  {
    internal float m_x;
    internal float m_y;

    /// <summary>
    /// Creates an instance.
    /// </summary>
    /// <param name="x">X component.</param>
    /// <param name="y">Y component.</param>
    /// <since>6.0</since>
    public Vector2f(float x, float y) : this()
    {
      m_x = x;
      m_y = y;
    }

    /// <summary>
    /// Gets or sets the X (first) component of this vector.
    /// </summary>
    /// <since>5.0</since>
    public float X { get { return m_x; } set { m_x = value; } }

    /// <summary>
    /// Gets or sets the Y (second) component of this vector.
    /// </summary>
    /// <since>5.0</since>
    public float Y { get { return m_y; } set { m_y = value; } }

    /// <summary>
    /// Returns the square of the length of this vector.
    /// This method does not check for the validity of its inputs.
    /// </summary>
    /// <since>6.0</since>
    public float SquareLength
    {
      get
      {
        return m_x * m_x + m_y * m_y;
      }
    }

    /// <summary>
    /// Determines whether the specified System.Object is a Vector2f and has the same values as the present vector.
    /// </summary>
    /// <param name="obj">The specified object.</param>
    /// <returns>true if obj is Vector2f and has the same coordinates as this; otherwise false.</returns>
    [ConstOperation]
    public override bool Equals(object obj)
    {
      return (obj is Vector2f && this == (Vector2f)obj);
    }

    /// <summary>
    /// Determines whether the specified vector has the same values as the present vector.
    /// </summary>
    /// <param name="vector">The specified vector.</param>
    /// <returns>true if obj is Vector2f and has the same coordinates as this; otherwise false.</returns>
    /// <since>5.0</since>
    [ConstOperation]
    public bool Equals(Vector2f vector)
    {
      return this == vector;
    }

    /// <summary>
    /// Check that all values in other are within epsilon of the values in this
    /// </summary>
    /// <param name="other"></param>
    /// <param name="epsilon"></param>
    /// <returns></returns>
    /// <since>5.4</since>
    [ConstOperation]
    public bool EpsilonEquals(Vector2f other, float epsilon)
    {
      return RhinoMath.EpsilonEquals(m_x, other.m_x, epsilon) &&
             RhinoMath.EpsilonEquals(m_y, other.m_y, epsilon);
    }
    /// <summary>
    /// Compares this <see cref="Vector2f" /> with another <see cref="Vector2f" />.
    /// <para>Components evaluation priority is first X, then Y.</para>
    /// </summary>
    /// <param name="other">The other <see cref="Vector2f" /> to use in comparison.</param>
    /// <returns>
    /// <para> 0: if this is identical to other</para>
    /// <para>-1: if this.X &lt; other.X</para>
    /// <para>-1: if this.X == other.X and this.Y &lt; other.Y</para>
    /// <para>+1: otherwise.</para>
    /// </returns>
    /// <since>5.0</since>
    [ConstOperation]
    public int CompareTo(Vector2f other)
    {
      if (m_x < other.m_x)
        return -1;
      if (m_x > other.m_x)
        return 1;

      if (m_y < other.m_y)
        return -1;
      if (m_y > other.m_y)
        return 1;

      return 0;
    }

    [ConstOperation]
    int IComparable.CompareTo(object obj)
    {
      if (obj is Vector2f)
        return CompareTo((Vector2f)obj);

      throw new ArgumentException("Input must be of type Vector2f", "obj");
    }

    /// <summary>
    /// Returns an indication regarding the validity of this vector.
    /// </summary>
    /// <since>6.0</since>
    public bool IsValid
    {
      get
      {
        return RhinoMath.IsValidSingle(m_x) && RhinoMath.IsValidSingle(m_y);
      }
    }

    /// <summary>
    /// Computes a hash number that represents the current vector.
    /// </summary>
    /// <returns>A hash code that is not unique for each vector.</returns>
    [ConstOperation]
    public override int GetHashCode()
    {
      // MSDN docs recommend XOR'ing the internal values to get a hash code
      return m_x.GetHashCode() ^ m_y.GetHashCode();
    }

    /// <summary>
    /// Constructs the string representation of the current vector.
    /// </summary>
    /// <returns>The vector representation in the form X,Y,Z.</returns>
    [ConstOperation]
    public override string ToString()
    {
      var culture = System.Globalization.CultureInfo.InvariantCulture;
      return String.Format("{0},{1}", m_x.ToString(culture), m_y.ToString(culture));
    }
    /// <inheritdoc />
    /// <since>7.0</since>
    [ConstOperation]
    public string ToString(string format, IFormatProvider formatProvider)
    {
      return Point3d.FormatCoordinates(format, formatProvider, m_x, m_y);
    }

    /// <summary>
    /// Determines whether two vectors have equal values.
    /// </summary>
    /// <param name="a">The first vector.</param>
    /// <param name="b">The second vector.</param>
    /// <returns>true if the components of the two vectors are exactly equal; otherwise false.</returns>
    /// <since>5.0</since>
    public static bool operator ==(Vector2f a, Vector2f b)
    {
      return (a.m_x == b.m_x && a.m_y == b.m_y);
    }

    /// <summary>
    /// Determines whether two vectors have different values.
    /// </summary>
    /// <param name="a">The first vector.</param>
    /// <param name="b">The second vector.</param>
    /// <returns>true if the two vectors differ in any component; false otherwise.</returns>
    /// <since>5.0</since>
    public static bool operator !=(Vector2f a, Vector2f b)
    {
      return (a.m_x != b.m_x || a.m_y != b.m_y);
    }

    /// <summary>
    /// Determines whether the first specified vector comes before
    /// (has inferior sorting value than) the second vector.
    /// <para>Components have decreasing evaluation priority: first X, then Y.</para>
    /// </summary>
    /// <param name="a">First vector.</param>
    /// <param name="b">Second vector.</param>
    /// <returns>true if a.X is smaller than b.X, or a.X == b.X and a.Y is smaller than b.Y; otherwise, false.</returns>
    /// <since>5.0</since>
    public static bool operator <(Vector2f a, Vector2f b)
    {
      return (a.X < b.X) || (a.X == b.X && a.Y < b.Y);
    }

    /// <summary>
    /// Determines whether the first specified vector comes before
    /// (has inferior sorting value than) the second vector, or it is equal to it.
    /// <para>Components have decreasing evaluation priority: first X, then Y.</para>
    /// </summary>
    /// <param name="a">First vector.</param>
    /// <param name="b">Second vector.</param>
    /// <returns>true if a.X is smaller than b.X, or a.X == b.X and a.Y &lt;= b.Y; otherwise, false.</returns>
    /// <since>5.0</since>
    public static bool operator <=(Vector2f a, Vector2f b)
    {
      return (a.X < b.X) || (a.X == b.X && a.Y <= b.Y);
    }

    /// <summary>
    /// Determines whether the first specified vector comes after (has superior sorting value than) the second vector.
    /// <para>Components have decreasing evaluation priority: first X, then Y.</para>
    /// </summary>
    /// <param name="a">First vector.</param>
    /// <param name="b">Second vector.</param>
    /// <returns>true if a.X is larger than b.X, or a.X == b.X and a.Y is larger than b.Y; otherwise, false.</returns>
    /// <since>5.0</since>
    public static bool operator >(Vector2f a, Vector2f b)
    {
      return (a.X > b.X) || (a.X == b.X && a.Y > b.Y);
    }

    /// <summary>
    /// Determines whether the first specified vector comes after
    /// (has superior sorting value than) the second vector, or it is equal to it.
    /// <para>Components have decreasing evaluation priority: first X, then Y.</para>
    /// </summary>
    /// <param name="a">First vector.</param>
    /// <param name="b">Second vector.</param>
    /// <returns>true if a.X is larger than b.X, or a.X == b.X and a.Y &gt;= b.Y; otherwise, false.</returns>
    /// <since>5.0</since>
    public static bool operator >=(Vector2f a, Vector2f b)
    {
      return (a.X > b.X) || (a.X == b.X && a.Y >= b.Y);
    }

    /// <summary>
    /// Multiplies two <see cref="Vector2f"/> together, returning the dot (internal) product of the two.
    /// </summary>
    /// <param name="point1">The first point.</param>
    /// <param name="point2">The second point.</param>
    /// <returns>A value that results from the coordinate-wise multiplication of point1 and point2.</returns>
    /// <since>6.0</since>
    public static double operator *(Vector2f point1, Vector2f point2)
    {
      return (point1.m_x * point2.m_x) +
        (point1.m_y * point2.m_y);
    }

    /// <summary>
    /// Multiplies two <see cref="Vector2f"/> together, returning the dot (internal) product of the two.
    /// <para>(Provided for languages that do not support operator overloading. You can use the * operator otherwise)</para>
    /// </summary>
    /// <param name="point1">The first point.</param>
    /// <param name="point2">The second point.</param>
    /// <returns>A value that results from the coordinate-wise multiplication of point1 and point2.</returns>
    /// <since>6.0</since>
    public static double Multiply(Vector2f point1, Vector2f point2)
    {
      return point1 * point2;
    }

    /// <summary>
    /// Computes the difference between two vectors.
    /// </summary>
    /// <param name="a">First vector.</param>
    /// <param name="b">Second vector.</param>
    /// <returns>The difference.</returns>
    /// <since>6.0</since>
    public static Vector2f operator -(Vector2f a, Vector2f b)
    {
      return new Vector2f(a.m_x - b.m_x, a.m_y - b.m_y);
    }

    /// <summary>
    /// Computes the sum between two vectors.
    /// </summary>
    /// <param name="a">First vector.</param>
    /// <param name="b">Second vector.</param>
    /// <returns>The difference.</returns>
    public static Vector2f operator +(Vector2f a, Vector2f b)
    {
      return new Vector2f(a.m_x + b.m_x, a.m_y + b.m_y);
    }
  }

  /// <summary>
  /// Represents the three components of a vector in three-dimensional space,
  /// using <see cref="Single"/>-precision floating point numbers.
  /// </summary>
  [StructLayout(LayoutKind.Sequential, Pack = 4, Size = 12)]
  [DebuggerDisplay("({m_x}, {m_y}, {m_z})")]
  [Serializable]
  public struct Vector3f : IEquatable<Vector3f>, IComparable<Vector3f>, IComparable, IEpsilonFComparable<Vector3f>, IValidable, IFormattable
  {
    #region members
    internal float m_x;
    internal float m_y;
    internal float m_z;
    #endregion

    #region constructors
    /// <summary>
    /// Constructs a new vector from 3 single precision numbers.
    /// </summary>
    /// <param name="x">X component of vector.</param>
    /// <param name="y">Y component of vector.</param>
    /// <param name="z">Z component of vector.</param>
    /// <since>5.0</since>
    public Vector3f(float x, float y, float z)
    {
      m_x = x;
      m_y = y;
      m_z = z;
    }
    #endregion

    #region static properties
    /// <summary>
    /// Gets the value of the vector with components 0,0,0.
    /// </summary>
    /// <since>5.0</since>
    public static Vector3f Zero
    {
      get { return new Vector3f(); }
    }

    /// <summary>
    /// Gets the value of the vector with components 1,0,0.
    /// </summary>
    /// <since>5.0</since>
    public static Vector3f XAxis
    {
      get { return new Vector3f(1f, 0f, 0f); }
    }

    /// <summary>
    /// Gets the value of the vector with components 0,1,0.
    /// </summary>
    /// <since>5.0</since>
    public static Vector3f YAxis
    {
      get { return new Vector3f(0f, 1f, 0f); }
    }

    /// <summary>
    /// Gets the value of the vector with components 0,0,1.
    /// </summary>
    /// <since>5.0</since>
    public static Vector3f ZAxis
    {
      get { return new Vector3f(0f, 0f, 1f); }
    }

    /// <summary>
    /// Gets the value of the vector with each component set to RhinoMath.UnsetValue.
    /// </summary>
    /// <since>5.0</since>
    public static Vector3f Unset
    {
      get { return new Vector3f(RhinoMath.UnsetSingle, RhinoMath.UnsetSingle, RhinoMath.UnsetSingle); }
    }
    #endregion static properties

    #region properties
    /// <summary>
    /// Gets or sets the X (first) component of this vector.
    /// </summary>
    /// <since>5.0</since>
    public float X { get { return m_x; } set { m_x = value; } }

    /// <summary>
    /// Gets or sets the Y (second) component of this vector.
    /// </summary>
    /// <since>5.0</since>
    public float Y { get { return m_y; } set { m_y = value; } }

    /// <summary>
    /// Gets or sets the Z (third) component of this vector.
    /// </summary>
    /// <since>5.0</since>
    public float Z { get { return m_z; } set { m_z = value; } }

    /// <summary>
    /// Gets a value indicating whether the X, Y, and Z values are all equal to 0.0.
    /// </summary>
    /// <since>6.0</since>
    public bool IsZero
    {
      get
      {
        return (m_x == 0 && m_y == 0 && m_z == 0);
      }
    }

    /// <summary>
    /// Gets a value indicating whether or not this is a unit vector. 
    /// A unit vector has length 1.
    /// </summary>
    /// <since>6.0</since>
    public bool IsUnitVector
    {
      get
      {
        // checks for invalid values and returns 0.0 if there are any
        double length = GetLengthHelper(m_x, m_y, m_z);
        return Math.Abs(length - 1.0) <= RhinoMath.SqrtEpsilon;
      }
    }

    /// <summary>
    /// Returns an indication regarding the validity of this vector.
    /// </summary>
    /// <since>6.0</since>
    public bool IsValid
    {
      get
      {
        return RhinoMath.IsValidSingle(m_x) && RhinoMath.IsValidSingle(m_y) && RhinoMath.IsValidSingle(m_z);
      }
    }

    #endregion

    /// <summary>
    /// Determines whether the specified System.Object is a Vector3f and has the same values as the present vector.
    /// </summary>
    /// <param name="obj">The specified object.</param>
    /// <returns>true if obj is Vector3f and has the same components as this; otherwise false.</returns>
    [ConstOperation]
    public override bool Equals(object obj)
    {
      return (obj is Vector3f && this == (Vector3f)obj);
    }

    /// <summary>
    /// Determines whether the specified vector has the same values as the present vector.
    /// </summary>
    /// <param name="vector">The specified vector.</param>
    /// <returns>true if vector has the same components as this; otherwise false.</returns>
    /// <since>5.0</since>
    [ConstOperation]
    public bool Equals(Vector3f vector)
    {
      return this == vector;
    }

    /// <summary>
    /// Check that all values in other are within epsilon of the values in this
    /// </summary>
    /// <param name="other"></param>
    /// <param name="epsilon"></param>
    /// <returns></returns>
    /// <since>5.4</since>
    [ConstOperation]
    public bool EpsilonEquals(Vector3f other, float epsilon)
    {
      return RhinoMath.EpsilonEquals(m_x, other.m_x, epsilon) &&
             RhinoMath.EpsilonEquals(m_y, other.m_y, epsilon) &&
             RhinoMath.EpsilonEquals(m_z, other.m_z, epsilon);
    }

    /// <summary>
    /// Compares this <see cref="Vector3f" /> with another <see cref="Vector3f" />.
    /// <para>Component evaluation priority is first X, then Y, then Z.</para>
    /// </summary>
    /// <param name="other">The other <see cref="Vector3f" /> to use in comparison.</param>
    /// <returns>
    /// <para> 0: if this is identical to other</para>
    /// <para>-1: if this.X &lt; other.X</para>
    /// <para>-1: if this.X == other.X and this.Y &lt; other.Y</para>
    /// <para>-1: if this.X == other.X and this.Y == other.Y and this.Z &lt; other.Z</para>
    /// <para>+1: otherwise.</para>
    /// </returns>
    /// <since>5.0</since>
    [ConstOperation]
    public int CompareTo(Vector3f other)
    {
      if (m_x < other.m_x)
        return -1;
      if (m_x > other.m_x)
        return 1;

      if (m_y < other.m_y)
        return -1;
      if (m_y > other.m_y)
        return 1;

      if (m_z < other.m_z)
        return -1;
      if (m_z > other.m_z)
        return 1;

      return 0;
    }

    [ConstOperation]
    int IComparable.CompareTo(object obj)
    {
      if (obj is Vector3f)
        return CompareTo((Vector3f)obj);

      throw new ArgumentException("Input must be of type Vector3f", "obj");
    }

    /// <summary>
    /// Computes a hash number that represents the current vector.
    /// </summary>
    /// <returns>A hash code that is not unique for each vector.</returns>
    [ConstOperation]
    public override int GetHashCode()
    {
      // MSDN docs recommend XOR'ing the internal values to get a hash code
      return m_x.GetHashCode() ^ m_y.GetHashCode() ^ m_z.GetHashCode();
    }

    /// <summary>
    /// Constructs the string representation of the current vector.
    /// </summary>
    /// <returns>The vector representation in the form X,Y,Z.</returns>
    [ConstOperation]
    public override string ToString()
    {
      var culture = System.Globalization.CultureInfo.InvariantCulture;
      return String.Format("{0},{1},{2}",
        m_x.ToString(culture), m_y.ToString(culture), m_z.ToString(culture));
    }
    /// <inheritdoc />
    /// <since>7.0</since>
    [ConstOperation]
    public string ToString(string format, IFormatProvider formatProvider)
    {
      return Point3d.FormatCoordinates(format, formatProvider, m_x, m_y, m_z);
    }

    /// <summary>
    /// Unitizes the vector in place. A unit vector has length 1 unit. 
    /// <para>An invalid or zero length vector cannot be unitized.</para>
    /// </summary>
    /// <returns>true on success or false on failure.</returns>
    /// <since>5.0</since>
    public bool Unitize()
    {
      bool rc = UnsafeNativeMethods.ON_3fVector_Unitize(ref this);
      return rc;
    }

    /// <summary>
    /// Transforms the vector in place.
    /// <para>The transformation matrix acts on the left of the vector; i.e.,</para>
    /// <para>result = transformation*vector</para>
    /// </summary>
    /// <param name="transformation">Transformation matrix to apply.</param>
    /// <since>5.0</since>
    public void Transform(Transform transformation)
    {
      double xx = transformation.m_00 * m_x + transformation.m_01 * m_y + transformation.m_02 * m_z;
      double yy = transformation.m_10 * m_x + transformation.m_11 * m_y + transformation.m_12 * m_z;
      double zz = transformation.m_20 * m_x + transformation.m_21 * m_y + transformation.m_22 * m_z;

      m_x = (float)xx;
      m_y = (float)yy;
      m_z = (float)zz;
    }

    /// <summary>
    /// Rotates this vector around a given axis.
    /// </summary>
    /// <param name="angleRadians">Angle of rotation (in radians).</param>
    /// <param name="rotationAxis">Axis of rotation.</param>
    /// <returns>true on success, false on failure.</returns>
    /// <since>5.0</since>
    public bool Rotate(double angleRadians, Vector3f rotationAxis)
    {
      if (RhinoMath.UnsetValue == angleRadians) { return false; }

      UnsafeNativeMethods.ON_3fVector_Rotate(ref this, angleRadians, rotationAxis);
      return true;
    }

    ///<summary>
    /// Reverses this vector in place (reverses the direction).
    /// <para>If this vector contains RhinoMath.UnsetValue, the 
    /// reverse will also be invalid and false will be returned.</para>
    ///</summary>
    ///<remarks>Similar to <see cref="Negate">Negate</see>, that is only provided for CLR language compliance.</remarks>
    ///<returns>true on success or false if the vector is invalid.</returns>
    /// <since>5.0</since>
    public bool Reverse()
    {
      bool rc = true;

      if (RhinoMath.UnsetSingle != m_x) { m_x = -m_x; } else { rc = false; }
      if (RhinoMath.UnsetSingle != m_y) { m_y = -m_y; } else { rc = false; }
      if (RhinoMath.UnsetSingle != m_z) { m_z = -m_z; } else { rc = false; }

      return rc;
    }

    ///<summary>
    /// Sets this vector to be perpendicular to another vector. 
    /// Result is not unitized.
    ///</summary>
    /// <param name="other">Vector to use as guide.</param>
    ///<returns>true on success, false if input vector is zero or invalid.</returns>
    /// <since>5.0</since>
    [ConstOperation]
    public bool PerpendicularTo(Vector3f other)
    {
      return UnsafeNativeMethods.ON_3fVector_PerpendicularTo(ref this, other);
    }

    /// <summary>
    /// Determines whether two vectors have equal values.
    /// </summary>
    /// <param name="a">The first vector.</param>
    /// <param name="b">The second vector.</param>
    /// <returns>true if the components of the two vectors are exactly equal; otherwise false.</returns>
    /// <since>5.0</since>
    public static bool operator ==(Vector3f a, Vector3f b)
    {
      return (a.m_x == b.m_x && a.m_y == b.m_y && a.m_z == b.m_z);
    }

    /// <summary>
    /// Determines whether two vectors have different values.
    /// </summary>
    /// <param name="a">The first vector.</param>
    /// <param name="b">The second vector.</param>
    /// <returns>true if the two vectors differ in any component; false otherwise.</returns>
    /// <since>5.0</since>
    public static bool operator !=(Vector3f a, Vector3f b)
    {
      return (a.m_x != b.m_x || a.m_y != b.m_y || a.m_z != b.m_z);
    }

    /// <summary>
    /// Determines whether the first specified vector comes before
    /// (has inferior sorting value than) the second vector.
    /// <para>Components evaluation priority is first X, then Y, then Z.</para>
    /// </summary>
    /// <param name="a">The first vector.</param>
    /// <param name="b">The second vector.</param>
    /// <returns>true if a.X is smaller than b.X,
    /// or a.X == b.X and a.Y is smaller than b.Y,
    /// or a.X == b.X and a.Y == b.Y and a.Z is smaller than b.Z;
    /// otherwise, false.</returns>
    /// <since>5.0</since>
    public static bool operator <(Vector3f a, Vector3f b)
    {
      return a.CompareTo(b) < 0;
    }

    /// <summary>
    /// Determines whether the first specified vector comes before
    /// (has inferior sorting value than) the second vector, or it is equal to it.
    /// <para>Components evaluation priority is first X, then Y, then Z.</para>
    /// </summary>
    /// <param name="a">The first vector.</param>
    /// <param name="b">The second vector.</param>
    /// <returns>true if a.X is smaller than b.X,
    /// or a.X == b.X and a.Y is smaller than b.Y,
    /// or a.X == b.X and a.Y == b.Y and a.Z &lt;= b.Z;
    /// otherwise, false.</returns>
    /// <since>5.0</since>
    public static bool operator <=(Vector3f a, Vector3f b)
    {
      return a.CompareTo(b) <= 0;
    }

    /// <summary>
    /// Determines whether the first specified vector comes after (has superior sorting value than)
    /// the second vector.
    /// <para>Components evaluation priority is first X, then Y, then Z.</para>
    /// </summary>
    /// <param name="a">The first vector.</param>
    /// <param name="b">The second vector.</param>
    /// <returns>true if a.X is larger than b.X,
    /// or a.X == b.X and a.Y is larger than b.Y,
    /// or a.X == b.X and a.Y == b.Y and a.Z is larger than b.Z;
    /// otherwise, false.</returns>
    /// <since>5.0</since>
    public static bool operator >(Vector3f a, Vector3f b)
    {
      return a.CompareTo(b) > 0;
    }

    /// <summary>
    /// Determines whether the first specified vector comes after (has superior sorting value than)
    /// the second vector, or it is equal to it.
    /// <para>Components evaluation priority is first X, then Y, then Z.</para>
    /// </summary>
    /// <param name="a">The first vector.</param>
    /// <param name="b">The second vector.</param>
    /// <returns>true if a.X is larger than b.X,
    /// or a.X == b.X and a.Y is larger than b.Y,
    /// or a.X == b.X and a.Y == b.Y and a.Z &gt;= b.Z;
    /// otherwise, false.</returns>
    /// <since>5.0</since>
    public static bool operator >=(Vector3f a, Vector3f b)
    {
      return a.CompareTo(b) >= 0;
    }

    /// <summary>
    /// Sums up two vectors.
    /// </summary>
    /// <param name="vector1">A vector.</param>
    /// <param name="vector2">A second vector.</param>
    /// <returns>A new vector that results from the component-wise addition of the two vectors.</returns>
    public static Vector3f operator +(Vector3f vector1, Vector3f vector2)
    {
      return new Vector3f(vector1.m_x + vector2.m_x, vector1.m_y + vector2.m_y, vector1.m_z + vector2.m_z);
    }

    /// <summary>
    /// Sums up two vectors.
    /// <para>(Provided for languages that do not support operator overloading. You can use the + operator otherwise)</para>
    /// </summary>
    /// <param name="vector1">A vector.</param>
    /// <param name="vector2">A second vector.</param>
    /// <returns>A new vector that results from the component-wise addition of the two vectors.</returns>
    /// <since>6.0</since>
    public static Vector3f Add(Vector3f vector1, Vector3f vector2)
    {
      return new Vector3f(vector1.m_x + vector2.m_x, vector1.m_y + vector2.m_y, vector1.m_z + vector2.m_z);
    }

    /// <summary>
    /// Sums up a point and a vector, and returns a new point.
    /// </summary>
    /// <param name="point">A point.</param>
    /// <param name="vector">A vector.</param>
    /// <returns>A new point that results from the addition of point and vector.</returns>
    public static Point3f operator +(Point3f point, Vector3f vector)
    {
      return new Point3f(point.m_x + vector.m_x, point.m_y + vector.m_y, point.m_z + vector.m_z);
    }

    /// <summary>
    /// Computes the opposite vector.
    /// </summary>
    /// <param name="vector">A vector to negate.</param>
    /// <returns>A new vector where all components were multiplied by -1.</returns>
    /// <since>6.0</since>
    public static Vector3f operator -(Vector3f vector)
    {
      return new Vector3f(-vector.m_x, -vector.m_y, -vector.m_z);
    }

    /// <summary>
    /// Subtracts the second vector from the first one.
    /// </summary>
    /// <param name="vector1">A vector.</param>
    /// <param name="vector2">A second vector.</param>
    /// <returns>A new vector that results from the component-wise difference of vector1 - vector2.</returns>
    /// <since>6.0</since>
    public static Vector3f operator -(Vector3f vector1, Vector3f vector2)
    {
      return new Vector3f(vector1.m_x - vector2.m_x, vector1.m_y - vector2.m_y, vector1.m_z - vector2.m_z);
    }

    /// <summary>
    /// Subtracts the second vector from the first one.
    /// <para>(Provided for languages that do not support operator overloading. You can use the - operator otherwise)</para>
    /// </summary>
    /// <param name="vector1">A vector.</param>
    /// <param name="vector2">A second vector.</param>
    /// <returns>A new vector that results from the component-wise difference of vector1 - vector2.</returns>
    /// <since>6.0</since>
    public static Vector3f Subtract(Vector3f vector1, Vector3f vector2)
    {
      return new Vector3f(vector1.m_x - vector2.m_x, vector1.m_y - vector2.m_y, vector1.m_z - vector2.m_z);
    }

    /// <summary>
    /// Computes the reversed vector.
    /// <para>(Provided for languages that do not support operator overloading. You can use the - unary operator otherwise)</para>
    /// </summary>
    /// <remarks>This is similar to <see cref="Reverse">Reverse()</see>, but is static for CLR compliance, and with default name.</remarks>
    /// <param name="vector">A vector to negate.</param>
    /// <returns>A new vector where all components were multiplied by -1.</returns>
    /// <since>6.0</since>
    public static Vector3f Negate(Vector3f vector)
    {
      return new Vector3f(-vector.m_x, -vector.m_y, -vector.m_z);
    }

    /// <summary>
    /// Sums up a point and a vector, and returns a new point.
    /// <para>(Provided for languages that do not support operator overloading. You can use the + operator otherwise)</para>
    /// </summary>
    /// <param name="point">A point.</param>
    /// <param name="vector">A vector.</param>
    /// <returns>A new point that results from the addition of point and vector.</returns>
    /// <since>5.0</since>
    public static Point3f Add(Point3f point, Vector3f vector)
    {
      return new Point3f(point.m_x + vector.m_x, point.m_y + vector.m_y, point.m_z + vector.m_z);
    }

    /// <summary>
    /// Multiplies a vector by a number, having the effect of scaling it.
    /// </summary>
    /// <param name="vector">A vector.</param>
    /// <param name="t">A number.</param>
    /// <returns>A new vector that is the original vector coordinate-wise multiplied by t.</returns>
    /// <since>5.0</since>
    public static Vector3f operator *(Vector3f vector, float t)
    {
      return new Vector3f(vector.m_x * t, vector.m_y * t, vector.m_z * t);
    }

    /// <summary>
    /// Multiplies two <see cref="Vector3f"/> together, returning the dot (internal) product of the two.
    /// This is not the cross product.
    /// </summary>
    /// <param name="point1">The first point.</param>
    /// <param name="point2">The second point.</param>
    /// <returns>A value that results from the coordinate-wise multiplication of point1 and point2.</returns>
    /// <since>5.0</since>
    public static double operator *(Vector3f point1, Vector3f point2)
    {
      return (point1.m_x * point2.m_x) +
        (point1.m_y * point2.m_y) +
        (point1.m_z * point2.m_z);
    }

    /// <summary>
    /// Multiplies a vector by a number, having the effect of scaling it.
    /// <para>(Provided for languages that do not support operator overloading. You can use the * operator otherwise)</para>
    /// </summary>
    /// <param name="vector">A vector.</param>
    /// <param name="t">A number.</param>
    /// <returns>A new vector that is the original vector coordinate-wise multiplied by t.</returns>
    /// <since>5.0</since>
    public static Vector3f Multiply(Vector3f vector, float t)
    {
      return new Vector3f(vector.m_x * t, vector.m_y * t, vector.m_z * t);
    }

    /// <summary>
    /// Multiplies a vector by a number, having the effect of scaling it.
    /// </summary>
    /// <param name="t">A number.</param>
    /// <param name="vector">A vector.</param>
    /// <returns>A new vector that is the original vector coordinate-wise multiplied by t.</returns>
    /// <since>5.0</since>
    public static Vector3f operator *(float t, Vector3f vector)
    {
      return new Vector3f(vector.m_x * t, vector.m_y * t, vector.m_z * t);
    }

    /// <summary>
    /// Multiplies a vector by a number, having the effect of scaling it.
    /// <para>(Provided for languages that do not support operator overloading. You can use the * operator otherwise)</para>
    /// </summary>
    /// <param name="t">A number.</param>
    /// <param name="vector">A vector.</param>
    /// <returns>A new vector that is the original vector coordinate-wise multiplied by t.</returns>
    /// <since>5.0</since>
    public static Vector3f Multiply(float t, Vector3f vector)
    {
      return new Vector3f(vector.m_x * t, vector.m_y * t, vector.m_z * t);
    }

    /// <summary>
    /// Multiplies two <see cref="Vector3f"/> together, returning the dot (internal) product of the two.
    /// This is not the cross product.
    /// <para>(Provided for languages that do not support operator overloading. You can use the * operator otherwise)</para>
    /// </summary>
    /// <param name="point1">The first point.</param>
    /// <param name="point2">The second point.</param>
    /// <returns>A value that results from the coordinate-wise multiplication of point1 and point2.</returns>
    /// <since>6.0</since>
    public static double Multiply(Vector3f point1, Vector3f point2)
    {
      return point1 * point2;
    }

    /// <summary>
    /// Divides a <see cref="Vector3f"/> by a number, having the effect of shrinking it, t times.
    /// </summary>
    /// <param name="vector">A vector.</param>
    /// <param name="t">A number.</param>
    /// <returns>A new vector that is component-wise divided by t.</returns>
    /// <since>6.0</since>
    public static Vector3f operator /(Vector3f vector, double t)
    {
      return new Vector3f((float)(vector.m_x / t), (float)(vector.m_y / t), (float)(vector.m_z / t));
    }

    /// <summary>
    /// Divides a <see cref="Vector3f"/> by a number, having the effect of shrinking it, t times.
    /// </summary>
    /// <param name="vector">A vector.</param>
    /// <param name="t">A number.</param>
    /// <returns>A new vector that is component-wise divided by t.</returns>
    /// <since>6.0</since>
    public static Vector3f operator /(Vector3f vector, float t)
    {
      return new Vector3f(vector.m_x / t, vector.m_y / t, vector.m_z / t);
    }

    /// <summary>
    /// Divides a <see cref="Vector3f"/> by a number, having the effect of shrinking it, t times.
    /// <para>(Provided for languages that do not support operator overloading. You can use the / operator otherwise)</para>
    /// </summary>
    /// <param name="vector">A vector.</param>
    /// <param name="t">A number.</param>
    /// <returns>A new vector that is component-wise divided by t.</returns>
    /// <since>6.0</since>
    public static Vector3f Divide(Vector3f vector, double t)
    {
      return vector / t;
    }

    /// <summary>
    /// Divides a <see cref="Vector3f"/> by a number, having the effect of shrinking it, t times.
    /// <para>(Provided for languages that do not support operator overloading. You can use the / operator otherwise)</para>
    /// </summary>
    /// <param name="vector">A vector.</param>
    /// <param name="t">A number.</param>
    /// <returns>A new vector that is component-wise divided by t.</returns>
    /// <since>6.0</since>
    public static Vector3f Divide(Vector3f vector, float t)
    {
      return vector / t;
    }

    /// <summary>
    /// Computes the cross product (or vector product, or exterior product) of two vectors.
    /// <para>This operation is not commutative.</para>
    /// </summary>
    /// <param name="a">First vector.</param>
    /// <param name="b">Second vector.</param>
    /// <returns>A new vector that is perpendicular to both a and b,
    /// <para>has Length == a.Length * b.Length and</para>
    /// <para>with a result that is oriented following the right hand rule.</para>
    /// </returns>
    /// <since>5.0</since>
    public static Vector3f CrossProduct(Vector3f a, Vector3f b)
    {
      return new Vector3f(a.m_y * b.m_z - b.m_y * a.m_z, a.m_z * b.m_x - b.m_z * a.m_x, a.m_x * b.m_y - b.m_x * a.m_y);
    }

    /// <summary>
    /// Returns the square length of the vector.
    /// This property does not check for the validity of the inputs.
    /// </summary>
    /// <since>6.0</since>
    public float SquareLength { get
      {
        return m_x * m_x + m_y * m_y + m_z * m_z;
      }
    }

    /// <summary>
    /// Computes the length (or magnitude, or size) of this vector.
    /// This is an application of Pythagoras' theorem.
    /// If this vector is invalid, its length is considered 0.
    /// </summary>
    /// <since>5.0</since>
    public float Length
    {
      get { return GetLengthHelper(m_x, m_y, m_z); }
    }

    internal static float GetLengthHelper(float dx, float dy, float dz)
    {
      if (!RhinoMath.IsValidSingle(dx) ||
          !RhinoMath.IsValidSingle(dy) ||
          !RhinoMath.IsValidSingle(dz))
        return 0f;

      float len;
      float fx = Math.Abs(dx);
      float fy = Math.Abs(dy);
      float fz = Math.Abs(dz);
      if (fy >= fx && fy >= fz)
      {
        len = fx; fx = fy; fy = len;
      }
      else if (fz >= fx && fz >= fy)
      {
        len = fx; fx = fz; fz = len;
      }

      // 15 September 2003 Dale Lear
      //     For small denormalized doubles (positive but smaller
      //     than DBL_MIN), some compilers/FPUs set 1.0/fx to +INF.
      //     Without the ON_DBL_MIN test we end up with
      //     microscopic vectors that have infinite length!
      //
      //     Since this code starts with doubles, none of this
      //     should be necessary, but it doesn't hurt anything.
      const float ON_SINGLE_MIN = (float)2.2250738585072014e-308;
      if (fx > ON_SINGLE_MIN)
      {
        len = 1f / fx;
        fy *= len;
        fz *= len;
        len = fx * (float)Math.Sqrt(1.0 + fy * fy + fz * fz);
      }
      else if (fx > 0.0 && RhinoMath.IsValidSingle(fx))
        len = fx;
      else
        len = 0f;
      return len;
    }

    /// <summary>
    /// Converts a double-precision vector in a single-precision vector.
    /// Needs explicit casting to help retain precision.
    /// </summary>
    /// <param name="vector">A vector.</param>
    /// <returns>The resulting vector.</returns>
    public static explicit operator Vector3f(Vector3d vector)
    {
      return new Vector3f((float)vector.X, (float)vector.Y, (float)vector.Z);
    }
  }
}
