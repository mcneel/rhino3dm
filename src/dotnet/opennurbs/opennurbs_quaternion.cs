using System;
using System.Runtime.InteropServices;
using Rhino.Runtime;

namespace Rhino.Geometry
{
  /// <summary>
  /// Represents the four coefficient values in a quaternion.
  /// <para>The first value <i>a</i> is the real part,
  /// while the rest multiplies <i>i</i>, <i>j</i> and <i>k</i>, that are imaginary.</para>
  /// <para>quaternion = a + bi + cj + dk</para>
  /// </summary>
  [StructLayout(LayoutKind.Sequential, Pack = 8, Size = 32)]
  [Serializable]
  public struct Quaternion : IEquatable<Quaternion>, IEpsilonComparable<Quaternion>
  {
    #region statics
    /// <summary>
    /// Returns the default quaternion, where all coefficients are 0.
    /// </summary>
    /// <since>5.0</since>
    public static Quaternion Zero
    {
      get { return new Quaternion(); }
    }
    /// <summary>
    /// Returns the (1,0,0,0) quaternion.
    /// </summary>
    /// <since>5.0</since>
    public static Quaternion Identity
    {
      get { return new Quaternion(1.0, 0.0, 0.0, 0.0); }
    }

    /// <summary>
    /// Returns the (0,1,0,0) quaternion.
    /// </summary>
    /// <since>5.0</since>
    public static Quaternion I
    {
      get { return new Quaternion(0.0, 1.0, 0.0, 0.0); }
    }

    /// <summary>
    /// Returns the (0,0,1,0) quaternion.
    /// </summary>
    /// <since>5.0</since>
    public static Quaternion J
    {
      get { return new Quaternion(0.0, 0.0, 1.0, 0.0); }
    }

    /// <summary>
    /// Returns the (0,0,0,1) quaternion.
    /// </summary>
    /// <since>5.0</since>
    public static Quaternion K
    {
      get { return new Quaternion(0.0, 0.0, 0.0, 1.0); }
    }
    #endregion

    // quaternion = a + bi + cj + dk
    double m_a;
    double m_b;
    double m_c;
    double m_d;

    /// <summary>
    /// Initializes a new quaternion with the provided coefficients.
    /// </summary>
    /// <param name="a">A number. This is the real part.</param>
    /// <param name="b">Another number. This is the first coefficient of the imaginary part.</param>
    /// <param name="c">Another number. This is the second coefficient of the imaginary part.</param>
    /// <param name="d">Another number. This is the third coefficient of the imaginary part.</param>
    /// <since>5.0</since>
    public Quaternion(double a, double b, double c, double d)
    {
      m_a = a;
      m_b = b;
      m_c = c;
      m_d = d;
    }

    /// <summary>
    /// Determines whether two quaternions have the same value.
    /// </summary>
    /// <param name="a">A quaternion.</param>
    /// <param name="b">Another quaternion.</param>
    /// <returns>true if the quaternions have exactly equal coefficients; otherwise false.</returns>
    /// <since>5.0</since>
    public static bool operator ==(Quaternion a, Quaternion b)
    {
      return a.Equals(b);
    }

    /// <summary>
    /// Determines whether two quaternions have different values.
    /// </summary>
    /// <param name="a">A quaternion.</param>
    /// <param name="b">Another quaternion.</param>
    /// <returns>true if the quaternions differ in any coefficient; otherwise false.</returns>
    /// <since>5.0</since>
    public static bool operator !=(Quaternion a, Quaternion b)
    {
      return (a.m_a != b.m_a || a.m_b != b.m_b || a.m_c != b.m_c || a.m_d != b.m_d);
    }

    /// <summary>
    /// Determines whether this quaternion has the same value of another quaternion.
    /// </summary>
    /// <param name="other">Another quaternion to compare.</param>
    /// <returns>true if the quaternions have exactly equal coefficients; otherwise false.</returns>
    /// <since>5.0</since>
    [ConstOperation]
    public bool Equals(Quaternion other)
    {
      return (m_a == other.m_a && m_b == other.m_b && m_c == other.m_c && m_d == other.m_d);
    }

    /// <summary>
    /// Determines whether an object is a quaternion and has the same value of this quaternion.
    /// </summary>
    /// <param name="obj">Another object to compare.</param>
    /// <returns>true if obj is a quaternion and has exactly equal coefficients; otherwise false.</returns>
    [ConstOperation]
    public override bool Equals(object obj)
    {
      return (obj is Quaternion && this == (Quaternion)obj);
    }

    /// <summary>
    /// Check that all values in other are within epsilon of the values in this
    /// </summary>
    /// <param name="other"></param>
    /// <param name="epsilon"></param>
    /// <returns></returns>
    /// <since>5.4</since>
    [ConstOperation]
    public bool EpsilonEquals(Quaternion other, double epsilon)
    {
      return RhinoMath.EpsilonEquals(m_a, other.m_a, epsilon) &&
             RhinoMath.EpsilonEquals(m_b, other.m_b, epsilon) &&
             RhinoMath.EpsilonEquals(m_c, other.m_c, epsilon) &&
             RhinoMath.EpsilonEquals(m_d, other.m_d, epsilon);
    }
    /// <summary>
    /// Gets a non-unique but repeatable hashing code for this quaternion.
    /// </summary>
    /// <returns>A signed number.</returns>
    [ConstOperation]
    public override int GetHashCode()
    {
      // MSDN docs recommend XOR'ing the internal values to get a hash code
      return m_a.GetHashCode() ^ m_b.GetHashCode() ^ m_c.GetHashCode() ^ m_d.GetHashCode();
    }

    /// <summary>
    /// Gets or sets the real part of the quaternion.
    /// </summary>
    /// <since>5.0</since>
    public double A
    {
      get { return m_a; }
      set { m_a = value; }
    }

    /// <summary>
    /// Gets or sets the first imaginary coefficient of the quaternion.
    /// </summary>
    /// <since>5.0</since>
    public double B
    {
      get { return m_b; }
      set { m_b = value; }
    }

    /// <summary>
    /// Gets or sets the second imaginary coefficient of the quaternion.
    /// </summary>
    /// <since>5.0</since>
    public double C
    {
      get { return m_c; }
      set { m_c = value; }
    }

    /// <summary>
    /// Gets or sets the third imaginary coefficient of the quaternion.
    /// </summary>
    /// <since>5.0</since>
    public double D
    {
      get { return m_d; }
      set { m_d = value; }
    }

    /// <summary>
    /// Sets all coefficients of the quaternion.
    /// </summary>
    /// <since>5.0</since>
    public void Set(double a, double b, double c, double d)
    {
      m_a = a;
      m_b = b;
      m_c = c;
      m_d = d;
    }

    /// <summary>
    /// Multiplies (scales) all quaternion coefficients by a factor and returns a new quaternion with the result.
    /// </summary>
    /// <param name="q">A quaternion.</param>
    /// <param name="x">A number.</param>
    /// <returns>A new quaternion.</returns>
    /// <since>5.0</since>
    public static Quaternion operator*(Quaternion q, int x)
    {
      return new Quaternion(q.m_a*x,q.m_b*x,q.m_c*x,q.m_d*x);
    }

    /// <summary>
    /// Multiplies (scales) all quaternion coefficients by a factor and returns a new quaternion with the result.
    /// </summary>
    /// <param name="q">A quaternion.</param>
    /// <param name="x">A number.</param>
    /// <returns>A new quaternion.</returns>
    /// <since>5.0</since>
    public static Quaternion operator*(Quaternion q, float x)
    {
      return new Quaternion(q.m_a*x,q.m_b*x,q.m_c*x,q.m_d*x);
    }

    /// <summary>
    /// Multiplies (scales) all quaternion coefficients by a factor and returns a new quaternion with the result.
    /// </summary>
    /// <param name="q">A quaternion.</param>
    /// <param name="x">A number.</param>
    /// <returns>A new quaternion.</returns>
    /// <since>5.0</since>
    public static Quaternion operator*(Quaternion q, double x)
    {
      return new Quaternion(q.m_a*x,q.m_b*x,q.m_c*x,q.m_d*x);
    }

    /// <summary>
    /// Divides all quaternion coefficients by a factor and returns a new quaternion with the result.
    /// </summary>
    /// <param name="q">A quaternion.</param>
    /// <param name="y">A number.</param>
    /// <returns>A new quaternion.</returns>
    /// <since>5.0</since>
    public static Quaternion operator/(Quaternion q, double y)
    {
      double x = (0d!=y) ? 1d/y : 0.0;
      return new Quaternion(q.m_a*x,q.m_b*x,q.m_c*x,q.m_d*x);
    }

    /// <summary>
    /// Adds two quaternions.
    /// <para>This sums each quaternion coefficient with its correspondent and returns
    /// a new result quaternion.</para>
    /// </summary>
    /// <param name="a">A quaternion.</param>
    /// <param name="b">Another quaternion.</param>
    /// <returns>A new quaternion.</returns>
    public static Quaternion operator+(Quaternion a, Quaternion b)
    {
      return new Quaternion(a.m_a+b.m_a, a.m_b+b.m_b, a.m_c+b.m_c, a.m_d+b.m_d);
    }

    /// <summary>
    /// Subtracts a quaternion from another one.
    /// <para>This computes the difference of each quaternion coefficient with its
    /// correspondent and returns a new result quaternion.</para>
    /// </summary>
    /// <param name="a">A quaternion.</param>
    /// <param name="b">Another quaternion.</param>
    /// <returns>A new quaternion.</returns>
    /// <since>5.0</since>
    public static Quaternion operator-(Quaternion a, Quaternion b)
    {
      return new Quaternion(a.m_a-b.m_a, a.m_b-b.m_b, a.m_c-b.m_c, a.m_d-b.m_d);
    }

    /// <summary>
    /// Multiplies a quaternion with another one.
    /// <para>Quaternion multiplication (Hamilton product) is not commutative.</para>
    /// </summary>
    /// <param name="a">The first term.</param>
    /// <param name="b">The second term.</param>
    /// <returns>A new quaternion.</returns>
    /// <since>5.0</since>
    public static Quaternion operator*(Quaternion a, Quaternion b)
    {
      return new Quaternion(a.m_a*b.m_a - a.m_b*b.m_b - a.m_c*b.m_c - a.m_d*b.m_d,
                            a.m_a*b.m_b + a.m_b*b.m_a + a.m_c*b.m_d - a.m_d*b.m_c,
                            a.m_a*b.m_c - a.m_b*b.m_d + a.m_c*b.m_a + a.m_d*b.m_b,
                            a.m_a*b.m_d + a.m_b*b.m_c - a.m_c*b.m_b + a.m_d*b.m_a);
    }

    /// <summary>
    /// Determines if the four coefficients are valid numbers within RhinoCommon.
    /// <para>See <see cref="RhinoMath.IsValidDouble(double)"/>.</para>
    /// </summary>
    /// <since>5.0</since>
    public bool IsValid
    {
      get
      {
        return RhinoMath.IsValidDouble(m_a) &&
          RhinoMath.IsValidDouble(m_b) &&
          RhinoMath.IsValidDouble(m_c) &&
          RhinoMath.IsValidDouble(m_d);
      }
    }

    /// <summary>
    /// Gets a new quaternion that is the conjugate of this quaternion.
    /// <para>This is (a,-b,-c,-d)</para>
    /// </summary>
    /// <since>5.0</since>
    public Quaternion Conjugate
    {
      get
      {
        return new Quaternion(m_a, -m_b, -m_c, -m_d);
      }
    }

    /// <summary>
    /// Modifies this quaternion to become
    /// <para>(a/L2, -b/L2, -c/L2, -d/L2),</para>
    /// <para>where L2 = length squared = (a*a + b*b + c*c + d*d).</para>
    /// <para>This is the multiplicative inverse, i.e.,
    /// (a,b,c,d)*(a/L2, -b/L2, -c/L2, -d/L2) = (1,0,0,0).</para>
    /// </summary>
    /// <returns>
    /// true if successful. false if the quaternion is zero and cannot be inverted.
    /// </returns>
    /// <since>5.0</since>
    public bool Invert()
    {
      double x = m_a * m_a + m_b * m_b + m_c * m_c + m_d * m_d;
      if (x <= double.Epsilon) //double.MinValue is an extremely big negative number. Epsilon is basically 0.
        return false;
      x = 1.0 / x;
      m_a *= x;
      x = -x;
      m_b *= x;
      m_c *= x;
      m_d *= x;
      return true;
    }

    /// <summary>
    /// Computes a new inverted quaternion,
    /// <para>(a/L2, -b/L2, -c/L2, -d/L2),</para>
    /// <para>where L2 = length squared = (a*a + b*b + c*c + d*d).</para>
    /// This is the multiplicative inverse, i.e.,
    /// (a,b,c,d)*(a/L2, -b/L2, -c/L2, -d/L2) = (1,0,0,0).
    /// If this is the zero quaternion, then the zero quaternion is returned.
    /// </summary>
    /// <since>5.0</since>
    public Quaternion Inverse
    {
      get
      {
        double x = m_a * m_a + m_b * m_b + m_c * m_c + m_d * m_d;
        x = (x > double.Epsilon) ? 1.0 / x : 0.0; //double.MinValue is an extremely big negative number. Epsilon is basically 0.
        return new Quaternion(m_a * x, -m_b * x, -m_c * x, -m_d * x);
      }
    }

    /// <summary>
    /// Returns the length or norm of the quaternion.
    /// </summary>
    /// <value>Math.Sqrt(a*a + b*b + c*c + d*d)</value>
    /// <since>5.0</since>
    public double Length
    {
      get
      {
        return UnsafeNativeMethods.ON_Quaternion_Length(ref this);
      }
    }

    /// <summary>
    /// Gets the result of (a^2 + b^2 + c^2 + d^2).
    /// </summary>
    /// <since>5.0</since>
    public double LengthSquared
    {
      get
      {
        return (m_a * m_a + m_b * m_b + m_c * m_c + m_d * m_d);
      }
    }

    /// <summary>
    /// Computes the distance or norm of the difference between this and another quaternion.
    /// </summary>
    /// <param name="q">Another quaternion.</param>
    /// <returns>(this - q).Length.</returns>
    /// <since>5.0</since>
    [ConstOperation]
    public double DistanceTo(Quaternion q)
    {
      Quaternion pq = new Quaternion(q.m_a-m_a, q.m_b-m_b, q.m_c-m_c, q.m_d-m_d);
      return pq.Length;
    }

    /// <summary>
    /// Returns the distance or norm of the difference between two quaternions.
    /// </summary>
    /// <param name="p">A quaternion.</param>
    /// <param name="q">Another quaternion.</param>
    /// <returns>(p - q).Length()</returns>
    /// <since>5.0</since>
    public static double Distance(Quaternion p, Quaternion q)
    {
      Quaternion pq = new Quaternion(q.m_a-p.m_a,q.m_b-p.m_b,q.m_c-p.m_c,q.m_d-p.m_d);
      return pq.Length;
    }

    /// <summary>
    /// Returns 4x4 real valued matrix form of the quaternion
    /// a  b  c  d
    /// -b  a -d  c
    /// -c  d  a -b
    /// -d -c  b  a
    /// which has the same arithmetic properties as the quaternion. 
    /// </summary>
    /// <returns>A transform value.</returns>
    /// <remarks>
    /// Do not confuse this with the rotation defined by the quaternion. This
    /// function will only be interesting to math nerds and is not useful in
    /// rendering or animation applications.
    /// </remarks>
    /// <since>5.0</since>
    [ConstOperation]
    public Transform MatrixForm()
    {
      Transform rc = new Transform();
      rc.m_00 =  m_a; rc.m_01 =  m_b; rc.m_02 =  m_c; rc.m_03 =  m_d;
      rc.m_10 = -m_b; rc.m_11 =  m_a; rc.m_12 = -m_d; rc.m_13 =  m_c;
      rc.m_20 = -m_c; rc.m_21 =  m_d; rc.m_22 =  m_a; rc.m_23 = -m_b;
      rc.m_30 = -m_d; rc.m_31 = -m_c; rc.m_32 =  m_b; rc.m_33 =  m_a;
      return rc;
    }

    /// <summary>
    /// Scales the quaternion's coordinates so that a*a + b*b + c*c + d*d = 1.
    /// </summary>
    /// <returns>
    /// true if successful.  false if the quaternion is zero and cannot be unitized.
    /// </returns>
    /// <since>5.0</since>
    public bool Unitize()
    {
      return UnsafeNativeMethods.ON_Quaternion_Unitize(ref this);
    }

    /// <summary>
    /// Sets the quaternion to cos(angle/2), sin(angle/2)*x, sin(angle/2)*y, sin(angle/2)*z
    /// where (x,y,z) is the unit vector parallel to axis.  This is the unit quaternion
    /// that represents the rotation of angle about axis.
    /// </summary>
    /// <param name="angle">in radians.</param>
    /// <param name="axisOfRotation">The direction of the axis of rotation.</param>
    /// <since>5.0</since>
    public void SetRotation(double angle, Vector3d axisOfRotation)
    {
      double s = axisOfRotation.Length;
      s = (s > 0.0) ? Math.Sin(0.5*angle)/s : 0.0;
      m_a = Math.Cos(0.5*angle);
      m_b = s*axisOfRotation.m_x;
      m_c = s*axisOfRotation.m_y;
      m_d = s*axisOfRotation.m_z;
    }

    /// <summary>
    /// Returns the unit quaternion
    /// cos(angle/2), sin(angle/2)*x, sin(angle/2)*y, sin(angle/2)*z
    /// where (x,y,z) is the unit vector parallel to axis.  This is the
    /// unit quaternion that represents the rotation of angle about axis.
    /// </summary>
    /// <param name="angle">An angle in radians.</param>
    /// <param name="axisOfRotation">The axis of rotation.</param>
    /// <returns>A new quaternion.</returns>
    /// <since>5.0</since>
    public static Quaternion Rotation(double angle, Vector3d axisOfRotation)
    {
      double s = axisOfRotation.Length;
      s = (s > 0.0) ? Math.Sin(0.5*angle)/s : 0.0;
      return new Quaternion(Math.Cos(0.5*angle),s*axisOfRotation.m_x,s*axisOfRotation.m_y,s*axisOfRotation.m_z);
    }

    /// <summary>
    /// Sets the quaternion to the unit quaternion which rotates
    /// plane0.xaxis to plane1.xaxis, plane0.yaxis to plane1.yaxis,
    /// and plane0.zaxis to plane1.zaxis.
    /// </summary>
    /// <param name="plane0">The "from" rotation plane. Origin point is ignored.</param>
    /// <param name="plane1">The "to" rotation plane. Origin point is ignored.</param>
    /// <remarks>The plane origins are ignored</remarks>
    /// <since>5.0</since>
    public void SetRotation(Plane plane0, Plane plane1)
    {
      UnsafeNativeMethods.ON_Quaternion_SetRotation(ref this, ref plane0, ref plane1);
    }

    /// <summary>
    /// Returns the unit quaternion that represents the rotation that maps
    /// plane0.xaxis to plane1.xaxis, plane0.yaxis to plane1.yaxis, and 
    /// plane0.zaxis to plane1.zaxis.
    /// </summary>
    /// <param name="plane0">The first plane.</param>
    /// <param name="plane1">The second plane.</param>
    /// <returns>A quaternion value.</returns>
    /// <remarks>The plane origins are ignored</remarks>
    /// <since>5.0</since>
    public static Quaternion Rotation(Plane plane0, Plane plane1)
    {
      Quaternion q = new Quaternion();
      q.SetRotation(plane0, plane1);
      return q;
    }

    /// <summary>
    /// Returns the rotation defined by the quaternion.
    /// </summary>
    /// <param name="angle">An angle in radians.</param>
    /// <param name="axis">unit axis of rotation of 0 if (b,c,d) is the zero vector.</param>
    /// <returns>True if the operation succeeded; otherwise, false.</returns>
    /// <remarks>
    /// If the quaternion is not unitized, the rotation of its unitized form is returned.
    /// </remarks>
    /// <since>5.0</since>
    [ConstOperation]
    public bool GetRotation(out double angle, out Vector3d axis)
    {
      double s = Length;
      angle = (s > double.MinValue) ? 2.0*Math.Acos(m_a/s) : 0.0;
      axis.m_x = m_b;
      axis.m_y = m_c;
      axis.m_z = m_d;
      return (axis.Unitize() && s > double.MinValue);
    }

    /// <summary>
    /// Returns the frame created by applying the quaternion's rotation
    /// to the canonical world frame (1,0,0),(0,1,0),(0,0,1).
    /// </summary>
    /// <param name="plane">A plane. This out value will be assigned during this call.</param>
    /// <returns>true if the operation succeeded; otherwise, false.</returns>
    /// <since>5.0</since>
    [ConstOperation]
    public bool GetRotation(out Plane plane)
    {
      plane = new Plane();
      return UnsafeNativeMethods.ON_Quaternion_GetRotation(ref this, ref plane);
    }

    /// <summary>
    /// Returns a transformation matrix that performs the rotation defined by the quaternion.
    /// The transformation returned by this method has the property that xform * V = q.Rotate(V).
    /// If the quaternion is not unitized, the rotation of its unitized form is returned.
    /// </summary>
    /// <param name="xform"></param>
    /// <returns>true if successful, false otherise.</returns>
    /// <remarks>
    /// Do not confuse the result of this method the transformation matrix returned by <see cref="MatrixForm"/>.
    /// </remarks>
    /// <since>7.12</since>
    [ConstOperation]
    public bool GetRotation(out Transform xform)
    {
      xform = new Transform();
      var rc = UnsafeNativeMethods.ON_Quaternion_GetRotation2(ref this, ref xform);
      if (!rc)
        xform = Transform.Unset;
      return rc;
    }

    /// <summary>
    /// Rotates a 3d vector. This operation is also called conjugation,
    /// because the result is the same as
    /// (q.Conjugate()*(0,x,y,x)*q/q.LengthSquared).Vector.
    /// </summary>
    /// <param name="v">The vector to be rotated.</param>
    /// <returns>
    /// R*v, where R is the rotation defined by the unit quaternion.
    /// This is mathematically the same as the values
    /// (Inverse(q)*(0,x,y,z)*q).Vector
    /// and
    /// (q.Conjugate()*(0,x,y,x)*q/q.LengthSquared).Vector.
    /// </returns>
    /// <remarks>
    /// If you need to rotate more than a dozen or so vectors,
    /// it will be more efficient to calculate the rotation
    /// matrix once and use it repeatedly.
    /// </remarks>
    /// <since>5.0</since>
    [ConstOperation]
    public Vector3d Rotate(Vector3d v)
    {
      Vector3d vout = new Vector3d();
      UnsafeNativeMethods.ON_Quaternion_Rotate(ref this, v, ref vout);
      return vout;
    }

    /// <summary>
    /// The imaginary part of the quaternion
    /// <para>(B,C,D)</para>
    /// </summary>
    /// <since>5.0</since>
    public Vector3d Vector
    {
      get { return new Vector3d(m_b, m_c, m_d); }
    }

    /// <summary>
    /// The real (scalar) part of the quaternion
    /// <para>This is <see cref="A"/>.</para>
    /// </summary>
    /// <since>5.0</since>
    public double Scalar
    {
      get { return m_a; }
    }

    /// <summary>
    /// true if a, b, c, and d are all zero.
    /// </summary>
    /// <since>5.0</since>
    public bool IsZero
    {
      get { return (0.0 == m_a && 0.0 == m_b && 0.0 == m_c && 0.0 == m_d); }
    }

    /// <summary>
    /// true if b, c, and d are all zero.
    /// </summary>
    /// <since>5.0</since>
    public bool IsScalar
    {
      get { return (0.0 == m_b && 0.0 == m_c && 0.0 == m_d); }
    }

    /// <summary>
    /// true if a = 0 and at least one of b, c, or d is not zero.
    /// </summary>
    /// <since>5.0</since>
    public bool IsVector
    {
      get { return (0.0 == m_a && (0.0 != m_b || 0.0 != m_c || 0.0 != m_d)); }
    }

    /// <summary>
    /// Returns a string representation of this Quaternion.
    /// </summary>
    /// <returns>A textual representation.</returns>
    /// <since>7.12</since>
    [ConstOperation]
    public override string ToString()
    {
      System.Text.StringBuilder sb = new System.Text.StringBuilder();
      IFormatProvider provider = System.Globalization.CultureInfo.InvariantCulture;
      sb.AppendFormat("A={0},", A.ToString(provider));
      sb.AppendFormat(" B={0},", B.ToString(provider));
      sb.AppendFormat(" C={0},", C.ToString(provider));
      sb.AppendFormat(" D={0}", D.ToString(provider));
      return sb.ToString();
    }

    /// <summary>
    /// The quaternion product of p and q.  This is the same value as p*q.
    /// </summary>
    /// <param name="p">The first transform.</param>
    /// <param name="q">The second transform.</param>
    /// <returns>A transform value.</returns>
    /// <since>5.0</since>
    public static Quaternion Product( Quaternion p, Quaternion q )
    {
      return new Quaternion(p.m_a*q.m_a - p.m_b*q.m_b - p.m_c*q.m_c - p.m_d*q.m_d,
                            p.m_a*q.m_b + p.m_b*q.m_a + p.m_c*q.m_d - p.m_d*q.m_c,
                            p.m_a*q.m_c - p.m_b*q.m_d + p.m_c*q.m_a + p.m_d*q.m_b,
                            p.m_a*q.m_d + p.m_b*q.m_c - p.m_c*q.m_b + p.m_d*q.m_a);
    }

    /// <summary>
    /// Computes the vector cross product of p and q = (0,x,y,z),
    /// <para>where (x,y,z) = <see cref="Vector3d.CrossProduct(Vector3d,Vector3d)">CrossProduct</see>(p.<see cref="Vector">Vector</see>,q.<see cref="Vector">Vector</see>).</para>
    /// <para>This <b>is not the same</b> as the quaternion product p*q.</para>
    /// </summary>
    /// <param name="p">A quaternion.</param>
    /// <param name="q">Another quaternion.</param>
    /// <returns>A new quaternion.</returns>
    /// <since>5.0</since>
    public static Quaternion CrossProduct(Quaternion p, Quaternion q)
    {
      return new Quaternion(0.0, p.m_c * q.m_d - p.m_d * q.m_c, p.m_d * q.m_b - p.m_b * q.m_d, p.m_b * q.m_c - p.m_c * q.m_d);
    }
  }
}
