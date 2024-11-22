using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security.Permissions;
using Rhino.Runtime;

namespace Rhino.Geometry
{
  /// <summary>
  /// Represents an interval in one-dimensional space,
  /// that is defined as two extrema or bounds.
  /// </summary>
  [StructLayout(LayoutKind.Sequential, Pack = 8, Size = 16)]
  [DebuggerDisplay("({m_t0}, {m_t1})")]
  [Serializable]
  public struct Interval : ISerializable, IEquatable<Interval>, IComparable<Interval>, IComparable, IEpsilonComparable<Interval>, IValidable
  {
    #region Members
    private double m_t0;
    private double m_t1;
    #endregion

    #region Constructors
    /// <summary>
    /// Initializes a new instance of the Rhino.Geometry.Interval class.
    /// </summary>
    /// <param name="t0">The first value.</param>
    /// <param name="t1">The second value.</param>
    /// <since>5.0</since>
    public Interval(double t0, double t1)
    {
      m_t0 = t0;
      m_t1 = t1;
    }

    /// <summary>
    /// Initializes a new instance copying the other instance values.
    /// </summary>
    /// <param name="other">The Rhino.Geometry.Interval to use as a base.</param>
    /// <since>5.0</since>
    public Interval(Interval other)
    {
      m_t0 = other.m_t0;
      m_t1 = other.m_t1;
    }

    private Interval(SerializationInfo info, StreamingContext context)
    {
      m_t0 = info.GetDouble("T0");
      m_t1 = info.GetDouble("T1");
    }

    [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.SerializationFormatter)]
    void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
    {
      info.AddValue("T0", m_t0);
      info.AddValue("T1", m_t1);
    }

    #endregion

    #region Operators
    /// <summary>
    /// Determines whether the two Intervals have equal values.
    /// </summary>
    /// <param name="a">The first interval.</param>
    /// <param name="b">The second interval.</param>
    /// <returns>true if the components of the two intervals are exactly equal; otherwise false.</returns>
    /// <since>5.0</since>
    public static bool operator ==(Interval a, Interval b)
    {
      return a.CompareTo(b) == 0;
    }

    /// <summary>
    /// Determines whether the two Intervals have different values.
    /// </summary>
    /// <param name="a">The first interval.</param>
    /// <param name="b">The second interval.</param>
    /// <returns>true if the two intervals are different in any value; false if they are equal.</returns>
    /// <since>5.0</since>
    public static bool operator !=(Interval a, Interval b)
    {
      return a.CompareTo(b) != 0;
    }

    /// <summary>
    /// Shifts a <see cref="Interval" /> by a specific amount (addition).
    /// </summary>
    /// <param name="interval">The interval to be used as a base.</param>
    /// <param name="number">The shifting value.</param>
    /// <returns>A new interval where T0 and T1 are summed with number.</returns>
    public static Interval operator +(Interval interval, double number)
    {
      return new Interval(interval.m_t0 + number, interval.m_t1 + number);
    }

    /// <summary>
    /// Shifts an interval by a specific amount (addition).
    /// </summary>
    /// <param name="number">The shifting value.</param>
    /// <param name="interval">The interval to be used as a base.</param>
    /// <returns>A new interval where T0 and T1 are summed with number.</returns>
    public static Interval operator +(double number, Interval interval)
    {
      return new Interval(interval.m_t0 + number, interval.m_t1 + number);
    }

    /// <summary>
    /// Shifts an interval by a specific amount (subtraction).
    /// </summary>
    /// <param name="interval">The base interval (minuend).</param>
    /// <param name="number">The shifting value to be subtracted (subtrahend).</param>
    /// <returns>A new interval with [T0-number, T1-number].</returns>
    /// <since>5.0</since>
    public static Interval operator -(Interval interval, double number)
    {
      return new Interval(interval.m_t0 - number, interval.m_t1 - number);
    }

    /// <summary>
    /// Shifts an interval by a specific amount (subtraction).
    /// </summary>
    /// <param name="number">The shifting value to subtract from (minuend).</param>
    /// <param name="interval">The interval to be subtracted from (subtrahend).</param>
    /// <returns>A new interval with [number-T0, number-T1].</returns>
    /// <since>5.0</since>
    public static Interval operator -(double number, Interval interval)
    {
      return new Interval(number - interval.m_t0, number - interval.m_t1);
    }

    /// <summary>
    /// Determines whether the first specified <see cref="Interval"/> comes before
    /// (has inferior sorting value than) the second Interval.
    /// <para>The lower bound has first evaluation priority.</para>
    /// </summary>
    /// <param name="a">First interval.</param>
    /// <param name="b">Second interval.</param>
    /// <returns>true if a[0] is smaller than b[0], or a[0] == b[0] and a[1] is smaller than b[1]; otherwise, false.</returns>
    /// <since>5.0</since>
    public static bool operator <(Interval a, Interval b)
    {
      return a.CompareTo(b) < 0;
    }

    /// <summary>
    /// Determines whether the first specified <see cref="Interval"/> comes before
    /// (has inferior sorting value than) the second Interval, or is equal to it.
    /// <para>The lower bound has first evaluation priority.</para>
    /// </summary>
    /// <param name="a">First interval.</param>
    /// <param name="b">Second interval.</param>
    /// <returns>true if a[0] is smaller than b[0], or a[0] == b[0] and a[1] is smaller than or equal to b[1]; otherwise, false.</returns>
    /// <since>5.0</since>
    public static bool operator <=(Interval a, Interval b)
    {
      return a.CompareTo(b) <= 0;
    }

    /// <summary>
    /// Determines whether the first specified <see cref="Interval"/> comes after
    /// (has superior sorting value than) the second Interval.
    /// <para>The lower bound has first evaluation priority.</para>
    /// </summary>
    /// <param name="a">First interval.</param>
    /// <param name="b">Second interval.</param>
    /// <returns>true if a[0] is larger than b[0], or a[0] == b[0] and a[1] is larger than b[1]; otherwise, false.</returns>
    /// <since>5.0</since>
    public static bool operator >(Interval a, Interval b)
    {
      return a.CompareTo(b) > 0;
    }

    /// <summary>
    /// Determines whether the first specified <see cref="Interval"/> comes after
    /// (has superior sorting value than) the second Interval, or is equal to it.
    /// <para>The lower bound has first evaluation priority.</para>
    /// </summary>
    /// <param name="a">First interval.</param>
    /// <param name="b">Second interval.</param>
    /// <returns>true if a[0] is larger than b[0], or a[0] == b[0] and a[1] is larger than or equal to b[1]; otherwise, false.</returns>
    /// <since>5.0</since>
    public static bool operator >=(Interval a, Interval b)
    {
      return a.CompareTo(b) >= 0;
    }
    #endregion

    #region Constants
    // David thinks: This is not really "empty" is it? Empty would be {0,0}.
    /////<summary>Sets interval to (RhinoMath.UnsetValue, RhinoMath.UnsetValue)</summary>
    //public static Interval Empty
    //{
    //  get { return new Interval(RhinoMath.UnsetValue, RhinoMath.UnsetValue); }
    //}

    /// <summary>
    /// Gets an Interval whose limits are RhinoMath.UnsetValue.
    /// </summary>
    /// <since>5.0</since>
    public static Interval Unset
    {
      get
      {
        return new Interval(RhinoMath.UnsetValue, RhinoMath.UnsetValue);
      }
    }
    #endregion

    #region Properties
    /// <summary>
    /// Gets or sets the lower bound of the Interval.
    /// </summary>
    /// <since>5.0</since>
    public double T0 { get { return m_t0; } set { m_t0 = value; } }

    /// <summary>
    /// Gets or sets the upper bound of the Interval.
    /// </summary>
    /// <since>5.0</since>
    public double T1 { get { return m_t1; } set { m_t1 = value; } }

    /// <summary>
    /// Gets or sets the indexed bound of this Interval.
    /// </summary>
    /// <param name="index">Bound index (0 = lower; 1 = upper).</param>
    public double this[int index]
    {
      get
      {
        if (0 == index) { return m_t0; }
        if (1 == index) { return m_t1; }

        // IronPython works with indexing is we thrown an IndexOutOfRangeException
        throw new IndexOutOfRangeException();
      }
      set
      {
        if (0 == index) { m_t0 = value; }
        else if (1 == index) { m_t1 = value; }
        else { throw new IndexOutOfRangeException(); }
      }
    }

    /// <summary>
    /// Gets the smaller of T0 and T1.
    /// </summary>
    /// <since>5.0</since>
    public double Min
    {
      get { return ((RhinoMath.IsValidDouble(m_t0) && RhinoMath.IsValidDouble(m_t1)) ? (m_t0 <= m_t1 ? m_t0 : m_t1) : RhinoMath.UnsetValue); }
    }

    /// <summary>
    /// Gets the larger of T0 and T1.
    /// </summary>
    /// <since>5.0</since>
    public double Max
    {
      get { return ((RhinoMath.IsValidDouble(m_t0) && RhinoMath.IsValidDouble(m_t1)) ? (m_t0 <= m_t1 ? m_t1 : m_t0) : RhinoMath.UnsetValue); }
    }

    /// <summary>
    /// Gets the average of T0 and T1.
    /// </summary>
    /// <example>
    /// <code source='examples\vbnet\ex_extendcurve.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_extendcurve.cs' lang='cs'/>
    /// <code source='examples\py\ex_extendcurve.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    public double Mid
    {
      get { return ((RhinoMath.IsValidDouble(m_t0) && RhinoMath.IsValidDouble(m_t1)) ? ((m_t0 == m_t1) ? m_t0 : (0.5 * (m_t0 + m_t1))) : RhinoMath.UnsetValue); }
    }

    /// <summary>
    /// Gets the signed length of the numeric range. 
    /// If the interval is decreasing, a negative length will be returned.
    /// </summary>
    /// <since>5.0</since>
    public double Length
    {
      get { return ((RhinoMath.IsValidDouble(m_t0) && RhinoMath.IsValidDouble(m_t1)) ? m_t1 - m_t0 : 0.0); }
    }

    /// <summary>
    /// Gets a value indicating whether or not this Interval is valid. 
    /// Valid intervals must contain valid numbers.
    /// </summary>
    /// <since>5.0</since>
    public bool IsValid
    {
      get { return RhinoMath.IsValidDouble(m_t0) && RhinoMath.IsValidDouble(m_t1); }
    }

    // If we decide that Interval.Empty should indeed be replaced with Interval.Unset, this function becomes pointless
    /////<summary>Returns true if T[0] == T[1] == ON.UnsetValue.</summary>
    //public bool IsEmpty
    //{
    //  get { return (RhinoMath.UnsetValue == m_t0 && RhinoMath.UnsetValue == m_t1); }
    //}

    /// <summary>
    /// Returns true if T0 == T1 != ON.UnsetValue.
    /// </summary>
    /// <since>5.0</since>
    public bool IsSingleton
    {
      get { return (RhinoMath.IsValidDouble(m_t0) && m_t0 == m_t1); }
    }

    /// <summary>
    /// Returns true if T0 &lt; T1.
    /// </summary>
    /// <since>5.0</since>
    public bool IsIncreasing
    {
      get { return (RhinoMath.IsValidDouble(m_t0) && m_t0 < m_t1); }
    }

    /// <summary> 
    /// Returns true if T[0] &gt; T[1].
    /// </summary>
    /// <since>5.0</since>
    public bool IsDecreasing
    {
      get { return (RhinoMath.IsValidDouble(m_t1) && m_t1 < m_t0); }
    }
    #endregion

    #region Methods
    /// <summary>
    /// Computes the hash code for this <see cref="Interval" /> object.
    /// </summary>
    /// <returns>A hash value that might be equal for two different <see cref="Interval" /> values.</returns>
    [ConstOperation]
    public override int GetHashCode()
    {
      // MSDN docs recommend XOR'ing the internal values to get a hash code
      return m_t0.GetHashCode() ^ m_t1.GetHashCode();
    }

    /// <summary>
    /// Determines whether the specified <see cref="object" /> is equal to the current <see cref="Interval" />,
    /// comparing by value.
    /// </summary>
    /// <param name="obj">The other object to compare with.</param>
    /// <returns>true if obj is an <see cref="Interval" /> and has the same bounds; false otherwise.</returns>
    [ConstOperation]
    public override bool Equals(object obj)
    {
      return (obj is Interval && this == (Interval)obj);
    }

    /// <summary>
    /// Determines whether the specified <see cref="Interval" /> is equal to the current <see cref="Interval" />,
    /// comparing by value.
    /// </summary>
    /// <param name="other">The other interval to compare with.</param>
    /// <returns>true if obj is an <see cref="Interval" /> and has the same bounds; false otherwise.</returns>
    /// <since>5.0</since>
    [ConstOperation]
    public bool Equals(Interval other)
    {
      return this == other;
    }

    /// <summary>
    /// Compares this <see cref="Interval" /> with another interval.
    /// <para>The lower bound has first evaluation priority.</para>
    /// </summary>
    /// <param name="other">The other <see cref="Interval" /> to compare with.</param>
    ///<returns>
    ///<para> 0: if this is identical to other</para>
    ///<para>-1: if this[0] &lt; other[0]</para>
    ///<para>+1: if this[0] &gt; other[0]</para>
    ///<para>-1: if this[0] == other[0] and this[1] &lt; other[1]</para>
    ///<para>+1: if this[0] == other[0] and this[1] &gt; other[1]</para>.</returns>
    /// <since>5.0</since>
    [ConstOperation]
    public int CompareTo(Interval other)
    {
      if (m_t0 < other.m_t0)
        return -1;
      if (m_t0 > other.m_t0)
        return 1;
      if (m_t1 < other.m_t1)
        return -1;
      if (m_t1 > other.m_t1)
        return 1;
      return 0;
    }

    [ConstOperation]
    int IComparable.CompareTo(object obj)
    {
      if (obj is Interval)
        return CompareTo((Interval)obj);

      throw new ArgumentException("Input must be of type Interval", "obj");
    }

    /// <summary>
    /// Returns a string representation of this <see cref="Interval" />.
    /// </summary>
    /// <returns>A string with T0,T1.</returns>
    [ConstOperation]
    public override string ToString()
    {
      var culture = System.Globalization.CultureInfo.InvariantCulture;
      return String.Format("{0},{1}", m_t0.ToString(culture), m_t1.ToString(culture));
    }

    /// <summary>
    /// Grows the <see cref="Interval" /> to include the given number.
    /// </summary>
    /// <param name="value">Number to include in this interval.</param>
    /// <since>5.0</since>
    public void Grow(double value)
    {
      if (!RhinoMath.IsValidDouble(value)) { return; }

      if (IsDecreasing) { Swap(); }
      if (m_t0 > value) { m_t0 = value; }
      if (m_t1 < value) { m_t1 = value; }
    }

    /// <summary>
    /// Ensures this <see cref="Interval" /> is either singleton or increasing.
    /// </summary>
    /// <since>5.0</since>
    public void MakeIncreasing()
    {
      if (IsDecreasing) { Swap(); }
    }

    /// <summary>
    /// Changes interval to [-T1, -T0].
    /// </summary>
    /// <since>5.0</since>
    public void Reverse()
    {
      if (IsValid)
      {
        double temp = m_t0;
        m_t0 = -m_t1;
        m_t1 = -temp;
      }
    }

    /// <summary>
    /// Exchanges T0 and T1.
    /// </summary>
    /// <since>5.0</since>
    public void Swap()
    {
      double temp = m_t0;
      m_t0 = m_t1;
      m_t1 = temp;
    }

    #region Evaluation
    ///<summary>Converts normalized parameter to interval value, or pair of values.</summary>
    ///<returns>Interval parameter min*(1.0-normalizedParameter) + max*normalizedParameter.</returns>
    ///<seealso>NormalizedParameterAt</seealso>
    /// <since>5.0</since>
    [ConstOperation]
    public double ParameterAt(double normalizedParameter)
    {
      return (RhinoMath.IsValidDouble(normalizedParameter) ? ((1.0 - normalizedParameter) * m_t0 + normalizedParameter * m_t1) : RhinoMath.UnsetValue);
    }

    ///<summary>Converts normalized parameter to interval value, or pair of values.</summary>
    ///<returns>Interval parameter min*(1.0-normalizedParameter) + max*normalized_paramete.</returns>
    ///<seealso>NormalizedParameterAt</seealso>
    /// <since>5.0</since>
    [ConstOperation]
    public Interval ParameterIntervalAt(Interval normalizedInterval)
    {
      double t0 = ParameterAt(normalizedInterval.m_t0);
      double t1 = ParameterAt(normalizedInterval.m_t1);
      return new Interval(t0, t1);
    }

    ///<summary>Converts interval value, or pair of values, to normalized parameter.</summary>
    ///<returns>Normalized parameter x so that min*(1.0-x) + max*x = intervalParameter.</returns>
    ///<seealso>ParameterAt</seealso>
    /// <since>5.0</since>
    [ConstOperation]
    public double NormalizedParameterAt(double intervalParameter)
    {
      double x;
      if (RhinoMath.IsValidDouble(intervalParameter))
      {
        if (m_t0 != m_t1)
        {
          x = (intervalParameter == m_t1) ? 1.0 : (intervalParameter - m_t0) / (m_t1 - m_t0);
        }
        else
          x = m_t0;
      }
      else
      {
        x = RhinoMath.UnsetValue;
      }
      return x;
    }

    ///<summary>Converts interval value, or pair of values, to normalized parameter.</summary>
    ///<returns>Normalized parameter x so that min*(1.0-x) + max*x = intervalParameter.</returns>
    ///<seealso>ParameterAt</seealso>
    /// <since>5.0</since>
    [ConstOperation]
    public Interval NormalizedIntervalAt(Interval intervalParameter)
    {
      double t0 = NormalizedParameterAt(intervalParameter.m_t0);
      double t1 = NormalizedParameterAt(intervalParameter.m_t1);
      return new Interval(t0, t1);
    }

    /// <summary>
    /// Tests a parameter for Interval inclusion.
    /// </summary>
    /// <param name="t">Parameter to test.</param>
    /// <returns>true if t is contained within or is coincident with the limits of this Interval.</returns>
    /// <since>5.0</since>
    [ConstOperation]
    public bool IncludesParameter(double t)
    {
      return IncludesParameter(t, false);
    }
    /// <summary>
    /// Tests a parameter for Interval inclusion.
    /// </summary>
    /// <param name="t">Parameter to test.</param>
    /// <param name="strict">If true, the parameter must be fully on the inside of the Interval.</param>
    /// <returns>true if t is contained within the limits of this Interval.</returns>
    /// <since>5.0</since>
    [ConstOperation]
    public bool IncludesParameter(double t, bool strict)
    {
      if (!RhinoMath.IsValidDouble(t)) { return false; }
      if (strict)
      {
        if ((m_t0 <= m_t1) && (m_t0 < t) && (t < m_t1)) { return true; }
        if ((m_t1 <= m_t0) && (m_t1 < t) && (t < m_t0)) { return true; }
      }
      else
      {
        if ((m_t0 <= m_t1) && (m_t0 <= t) && (t <= m_t1)) { return true; }
        if ((m_t1 <= m_t0) && (m_t1 <= t) && (t <= m_t0)) { return true; }
      }

      return false;
    }

    /// <summary>
    /// Tests another interval for Interval inclusion.
    /// </summary>
    /// <param name="interval">Interval to test.</param>
    /// <returns>true if the other interval is contained within or is coincident with the limits of this Interval; otherwise false.</returns>
    /// <since>5.0</since>
    [ConstOperation]
    public bool IncludesInterval(Interval interval)
    {
      return IncludesInterval(interval, false);
    }
    /// <summary>
    /// Tests another interval for Interval inclusion.
    /// </summary>
    /// <param name="interval">Interval to test.</param>
    /// <param name="strict">If true, the other interval must be fully on the inside of the Interval.</param>
    /// <returns>true if the other interval is contained within the limits of this Interval; otherwise false.</returns>
    /// <since>5.0</since>
    [ConstOperation]
    public bool IncludesInterval(Interval interval, bool strict)
    {
      return (IncludesParameter(interval.m_t0, strict) && IncludesParameter(interval.m_t1, strict));
    }

    #endregion
    #endregion

    #region Static methods

    /// <summary>
    /// Returns a new Interval that is the Intersection of the two input Intervals.
    /// </summary>
    /// <param name="a">The first input interval.</param>
    /// <param name="b">The second input interval.</param>
    /// <returns>If the intersection is not empty, then 
    /// intersection = [max(a.Min(),b.Min()), min(a.Max(),b.Max())]
    /// The interval [ON.UnsetValue,ON.UnsetValue] is considered to be
    /// the empty set interval.  The result of any intersection involving an
    /// empty set interval or disjoint intervals is the empty set interval.</returns>
    /// <since>5.0</since>
    public static Interval FromIntersection(Interval a, Interval b)
    {
      Interval rc = new Interval();
      UnsafeNativeMethods.ON_Interval_Intersection(ref rc, a, b);
      return rc;
    }


    /// <summary>
    /// Returns a new Interval which contains both inputs.
    /// </summary>
    /// <param name="a">The first input interval.</param>
    /// <param name="b">The second input interval.</param>
    /// <returns>The union of an empty set and an increasing interval is the increasing interval.
    /// <para>The union of two empty sets is empty.</para>
    /// <para>The union of an empty set an a non-empty interval is the non-empty interval.</para>
    /// <para>The union of two non-empty intervals is [min(a.Min(),b.Min()), max(a.Max(),b.Max())]</para>
    /// </returns>
    /// <since>5.0</since>
    public static Interval FromUnion(Interval a, Interval b)
    {
      Interval rc = new Interval();
      UnsafeNativeMethods.ON_Interval_Union(ref rc, a, b);
      return rc;
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
    public bool EpsilonEquals(Interval other, double epsilon)
    {
      return RhinoMath.EpsilonEquals(m_t0, other.m_t0, epsilon) &&
             RhinoMath.EpsilonEquals(m_t1, other.m_t1, epsilon);
    }
  }

  /// <summary>
  /// Represents the two coordinates of a point in two-dimensional space,
  /// using <see cref="double"/>-precision floating point numbers.
  /// </summary>
  [StructLayout(LayoutKind.Sequential, Pack = 8, Size = 16)]
  [DebuggerDisplay("({m_x}, {m_y})")]
  [Serializable]
  public struct Point2d : ISerializable, IEquatable<Point2d>, IComparable<Point2d>, IComparable, IEpsilonComparable<Point2d>, IValidable, IFormattable
  {
    private double m_x;
    private double m_y;

    /// <summary>
    /// Gets or sets the X (first) coordinate of the point.
    /// </summary>
    /// <since>5.0</since>
    public double X { get { return m_x; } set { m_x = value; } }

    /// <summary>
    /// Gets or sets the Y (second) coordinate of the point.
    /// </summary>
    /// <since>5.0</since>
    public double Y { get { return m_y; } set { m_y = value; } }

    #region constructors

    /// <summary>
    /// Initializes a new instance of <see cref="Point2d"/> from coordinates.
    /// </summary>
    /// <param name="x">The X (first) coordinate.</param>
    /// <param name="y">The Y (second) coordinate.</param>
    /// <since>5.0</since>
    public Point2d(double x, double y)
    {
      m_x = x;
      m_y = y;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="Point2d"/> by converting a vector.
    /// </summary>
    /// <param name="vector">The vector that will be copied.</param>
    /// <since>5.0</since>
    public Point2d(Vector2d vector)
    {
      m_x = vector.X;
      m_y = vector.Y;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="Point2d"/> by copying another <see cref="Point2d"/>.
    /// </summary>
    /// <param name="point">The point that will be copied.</param>
    /// <since>5.0</since>
    public Point2d(Point2d point)
    {
      m_x = point.m_x;
      m_y = point.m_y;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="Point3d"/> by copying the first two coordinates of a <see cref="Point3d"/>.
    /// </summary>
    /// <param name="point">The point that will be used: the Z (third) coordinate is discarded.</param>
    /// <since>5.0</since>
    public Point2d(Point3d point)
    {
      m_x = point.m_x;
      m_y = point.m_y;
    }

    private Point2d(SerializationInfo info, StreamingContext context)
    {
      m_x = info.GetDouble("X");
      m_y = info.GetDouble("Y");
    }

    [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.SerializationFormatter)]
    void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
    {
      info.AddValue("X", m_x);
      info.AddValue("Y", m_y);
    }
    #endregion

    #region operators
    //static Point2d^ operator *=(Point2d^ point, double t);
    //static Point2d^ operator /=(Point2d^ point, double t);
    //static Point2d^ operator +=(Point2d^ point, Point2d^ other);
    //static Point2d^ operator +=(Point2d^ point, Vector2d^ vector);
    //static Point2d^ operator -=(Point2d^ point, Point2d^ other);
    //static Point2d^ operator -=(Point2d^ point, Vector2d^ vector);

    /// <summary>
    /// Multiplies a <see cref="Point2d"/> by a number.
    /// </summary>
    /// <param name="point">A point.</param>
    /// <param name="t">A number.</param>
    /// <returns>A new point that is coordinate-wise multiplied by t.</returns>
    /// <since>5.0</since>
    public static Point2d operator *(Point2d point, double t)
    {
      return new Point2d(point.X * t, point.Y * t);
    }

    /// <summary>
    /// Multiplies a <see cref="Point2d"/> by a number.
    /// <para>(Provided for languages that do not support operator overloading. You can use the * operator otherwise)</para>
    /// </summary>
    /// <param name="point">A point.</param>
    /// <param name="t">A number.</param>
    /// <returns>A new point that is coordinate-wise multiplied by t.</returns>
    /// <since>5.0</since>
    public static Point2d Multiply(Point2d point, double t)
    {
      return new Point2d(point.X * t, point.Y * t);
    }

    /// <summary>
    /// Multiplies a <see cref="Point2d"/> by a number.
    /// </summary>
    /// <param name="t">A number.</param>
    /// <param name="point">A point.</param>
    /// <returns>A new point that is coordinate-wise multiplied by t.</returns>
    /// <since>5.0</since>
    public static Point2d operator *(double t, Point2d point)
    {
      return new Point2d(point.X * t, point.Y * t);
    }

    /// <summary>
    /// Multiplies a <see cref="Point2d"/> by a number.
    /// <para>(Provided for languages that do not support operator overloading. You can use the * operator otherwise)</para>
    /// </summary>
    /// <param name="t">A number.</param>
    /// <param name="point">A point.</param>
    /// <returns>A new point that is coordinate-wise multiplied by t.</returns>
    /// <since>5.0</since>
    public static Point2d Multiply(double t, Point2d point)
    {
      return new Point2d(point.X * t, point.Y * t);
    }

    /// <summary>
    /// Divides a <see cref="Point2d"/> by a number.
    /// </summary>
    /// <param name="t">A number.</param>
    /// <param name="point">A point.</param>
    /// <returns>A new point that is coordinate-wise divided by t.</returns>
    /// <since>5.0</since>
    public static Point2d operator /(Point2d point, double t)
    {
      return new Point2d(point.X / t, point.Y / t);
    }

    /// <summary>
    /// Divides a <see cref="Point2d"/> by a number.
    /// <para>(Provided for languages that do not support operator overloading. You can use the / operator otherwise)</para>
    /// </summary>
    /// <param name="point">A point.</param>
    /// <param name="t">A number.</param>
    /// <returns>A new point that is coordinate-wise divided by t.</returns>
    /// <since>5.0</since>
    public static Point2d Divide(Point2d point, double t)
    {
      return new Point2d(point.X / t, point.Y / t);
    }

    /// <summary>
    /// Adds a point with a vector.
    /// </summary>
    /// <param name="point">A point.</param>
    /// <param name="vector">A vector.</param>
    /// <returns>A new point that is coordinate-wise summed with the vector.</returns>
    public static Point2d operator +(Point2d point, Vector2d vector)
    {
      return new Point2d(point.X + vector.X, point.Y + vector.Y);
    }

    /// <summary>
    /// Adds a point with a vector.
    /// <para>(Provided for languages that do not support operator overloading. You can use the + operator otherwise)</para>
    /// </summary>
    /// <param name="point">A point.</param>
    /// <param name="vector">A vector.</param>
    /// <returns>A new point that is coordinate-wise summed with the vector.</returns>
    /// <since>5.0</since>
    public static Point2d Add(Point2d point, Vector2d vector)
    {
      return new Point2d(point.X + vector.X, point.Y + vector.Y);
    }

    /// <summary>
    /// Adds a vector with a point.
    /// </summary>
    /// <param name="vector">A vector.</param>
    /// <param name="point">A point.</param>
    /// <returns>A new point that is coordinate-wise summed with the vector.</returns>
    public static Point2d operator +(Vector2d vector, Point2d point)
    {
      return new Point2d(point.X + vector.X, point.Y + vector.Y);
    }

    /// <summary>
    /// Adds a vector with a point.
    /// <para>(Provided for languages that do not support operator overloading. You can use the + operator otherwise)</para>
    /// </summary>
    /// <param name="vector">A vector.</param>
    /// <param name="point">A point.</param>
    /// <returns>A new point that is coordinate-wise summed with the vector.</returns>
    /// <since>5.0</since>
    public static Point2d Add(Vector2d vector, Point2d point)
    {
      return new Point2d(point.X + vector.X, point.Y + vector.Y);
    }

    /// <summary>
    /// Adds a point with a point.
    /// </summary>
    /// <param name="point1">A point.</param>
    /// <param name="point2">A point.</param>
    /// <returns>A new point that is coordinate-wise summed with the other point.</returns>
    public static Point2d operator +(Point2d point1, Point2d point2)
    {
      return new Point2d(point1.X + point2.X, point1.Y + point2.Y);
    }

    /// <summary>
    /// Adds a point with a point.
    /// <para>(Provided for languages that do not support operator overloading. You can use the + operator otherwise)</para>
    /// </summary>
    /// <param name="point1">A point.</param>
    /// <param name="point2">A point.</param>
    /// <returns>A new point that is coordinate-wise summed with the other point.</returns>
    /// <since>5.0</since>
    public static Point2d Add(Point2d point1, Point2d point2)
    {
      return new Point2d(point1.X + point2.X, point1.Y + point2.Y);
    }

    /// <summary>
    /// Subtracts a vector from a point.
    /// </summary>
    /// <param name="point">A point.</param>
    /// <param name="vector">A vector.</param>
    /// <returns>A new point that is coordinate-wise subtracted by vector.</returns>
    /// <since>5.0</since>
    public static Point2d operator -(Point2d point, Vector2d vector)
    {
      return new Point2d(point.X - vector.X, point.Y - vector.Y);
    }

    /// <summary>
    /// Subtracts a vector from a point.
    /// <para>(Provided for languages that do not support operator overloading. You can use the - operator otherwise)</para>
    /// </summary>
    /// <param name="point">A point.</param>
    /// <param name="vector">A vector.</param>
    /// <returns>A new point that is coordinate-wise subtracted by vector.</returns>
    /// <since>5.0</since>
    public static Point2d Subtract(Point2d point, Vector2d vector)
    {
      return new Point2d(point.X - vector.X, point.Y - vector.Y);
    }

    /// <summary>
    /// Subtracts point2 from point1.
    /// </summary>
    /// <param name="point1">A point (minuend).</param>
    /// <param name="point2">A point (subtrahend).</param>
    /// <returns>A new vector that is point1 coordinate-wise subtracted by point2.</returns>
    /// <since>5.0</since>
    public static Vector2d operator -(Point2d point1, Point2d point2)
    {
      return new Vector2d(point1.X - point2.X, point1.Y - point2.Y);
    }

    /// <summary>
    /// Subtracts the second point from the first point.
    /// <para>(Provided for languages that do not support operator overloading. You can use the - operator otherwise)</para>
    /// </summary>
    /// <param name="point1">A point (minuend).</param>
    /// <param name="point2">A point (subtrahend).</param>
    /// <returns>A new vector that is point1 coordinate-wise subtracted by point2.</returns>
    /// <since>5.0</since>
    public static Vector2d Subtract(Point2d point1, Point2d point2)
    {
      return new Vector2d(point1.X - point2.X, point1.Y - point2.Y);
    }

    /// <summary>
    /// Determines whether two <see cref="Point2d"/> have equal values.
    /// </summary>
    /// <param name="a">The first point.</param>
    /// <param name="b">The second point.</param>
    /// <returns>true if the coordinates of the two points are exactly equal; otherwise false.</returns>
    /// <since>5.0</since>
    public static bool operator ==(Point2d a, Point2d b)
    {
      return a.m_x == b.m_x && a.m_y == b.m_y;
    }

    /// <summary>
    /// Determines whether two <see cref="Point2d"/> have different values.
    /// </summary>
    /// <param name="a">The first point.</param>
    /// <param name="b">The second point.</param>
    /// <returns>true if the two points differ in any coordinate; false otherwise.</returns>
    /// <since>5.0</since>
    public static bool operator !=(Point2d a, Point2d b)
    {
      return a.m_x != b.m_x || a.m_y != b.m_y;
    }

    /// <summary>
    /// Determines whether the first specified point comes before (has inferior sorting value than) the second point.
    /// <para>Coordinates evaluation priority is first X, then Y.</para>
    /// </summary>
    /// <param name="a">First point.</param>
    /// <param name="b">Second point.</param>
    /// <returns>true if a.X is smaller than b.X, or a.X == b.X and a.Y is smaller than b.Y; otherwise, false.</returns>
    /// <since>5.0</since>
    public static bool operator <(Point2d a, Point2d b)
    {
      return (a.X < b.X) || (a.X == b.X && a.Y < b.Y);
    }

    /// <summary>
    /// Determines whether the first specified point comes before
    /// (has inferior sorting value than) the second point, or it is equal to it.
    /// <para>Coordinates evaluation priority is first X, then Y.</para>
    /// </summary>
    /// <param name="a">First point.</param>
    /// <param name="b">Second point.</param>
    /// <returns>true if a.X is smaller than b.X, or a.X == b.X and a.Y &lt;= b.Y; otherwise, false.</returns>
    /// <since>5.0</since>
    public static bool operator <=(Point2d a, Point2d b)
    {
      return (a.X < b.X) || (a.X == b.X && a.Y <= b.Y);
    }

    /// <summary>
    /// Determines whether the first specified <see cref="Point2d"/> comes after
    /// (has superior sorting value than) the second point.
    /// <para>Coordinates evaluation priority is first X, then Y.</para>
    /// </summary>
    /// <param name="a">First point.</param>
    /// <param name="b">Second point.</param>
    /// <returns>true if a.X is larger than b.X, or a.X == b.X and a.Y is larger than b.Y; otherwise, false.</returns>
    /// <since>5.0</since>
    public static bool operator >(Point2d a, Point2d b)
    {
      return (a.X > b.X) || (a.X == b.X && a.Y > b.Y);
    }

    /// <summary>
    /// Determines whether the first specified <see cref="Point2d"/> comes after
    /// (has superior sorting value than) the second point, or it is equal to it.
    /// <para>Coordinates evaluation priority is first X, then Y.</para>
    /// </summary>
    /// <param name="a">First point.</param>
    /// <param name="b">Second point.</param>
    /// <returns>true if a.X is larger than b.X, or a.X == b.X and a.Y &gt;= b.Y; otherwise, false.</returns>
    /// <since>5.0</since>
    public static bool operator >=(Point2d a, Point2d b)
    {
      return (a.X > b.X) || (a.X == b.X && a.Y >= b.Y);
    }

    #endregion

    /// <summary>
    /// Determines whether the specified System.Object is a Point2d and has the same values as the present point.
    /// </summary>
    /// <param name="obj">The specified object.</param>
    /// <returns>true if obj is a Point2d and has the same coordinates as this; otherwise false.</returns>
    [ConstOperation]
    public override bool Equals(object obj)
    {
      return (obj is Point2d && this == (Point2d)obj);
    }

    /// <summary>
    /// Determines whether the specified Point2d has the same values as the present point.
    /// </summary>
    /// <param name="point">The specified point.</param>
    /// <returns>true if point has the same coordinates as this; otherwise false.</returns>
    /// <since>5.0</since>
    [ConstOperation]
    public bool Equals(Point2d point)
    {
      return this == point;
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
    /// Compares this <see cref="Point2d" /> with another <see cref="Point2d" />.
    /// <para>Coordinates evaluation priority is first X, then Y.</para>
    /// </summary>
    /// <param name="other">The other <see cref="Point2d" /> to use in comparison.</param>
    /// <returns>
    /// <para> 0: if this is identical to other</para>
    /// <para>-1: if this.X &lt; other.X</para>
    /// <para>-1: if this.X == other.X and this.Y &lt; other.Y</para>
    /// <para>+1: otherwise.</para>
    /// </returns>
    /// <since>5.0</since>
    [ConstOperation]
    public int CompareTo(Point2d other)
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
      if (obj is Point2d)
        return CompareTo((Point2d)obj);

      throw new ArgumentException("Input must be of type Point2d", "obj");
    }

    /// <summary>
    /// Check that all values in other are within epsilon of the values in this
    /// </summary>
    /// <param name="other"></param>
    /// <param name="epsilon"></param>
    /// <returns></returns>
    /// <since>5.4</since>
    [ConstOperation]
    public bool EpsilonEquals(Point2d other, double epsilon)
    {
      return RhinoMath.EpsilonEquals(m_x, other.m_x, epsilon) &&
             RhinoMath.EpsilonEquals(m_y, other.m_y, epsilon);
    }

    /// <summary>
    /// Constructs the string representation for the current point.
    /// </summary>
    /// <returns>The point representation in the form X,Y.</returns>
    [ConstOperation]
    public override string ToString()
    {
      var culture = System.Globalization.CultureInfo.InvariantCulture;
      return String.Format("{0},{1}", X.ToString(culture), Y.ToString(culture));
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
    public double this[int index]
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

    ///<summary>
    ///If any coordinate of a point is UnsetValue, then the point is not valid.
    ///</summary>
    /// <since>5.0</since>
    public bool IsValid
    {
      get { return RhinoMath.IsValidDouble(X) && RhinoMath.IsValidDouble(Y); }
    }

    /// <summary>
    /// Gets the smallest (both positive and negative) valid coordinate, or RhinoMath.UnsetValue if no coordinate is valid.
    /// </summary>
    /// <since>5.0</since>
    public double MinimumCoordinate
    {
      get
      {
        double c;
        if (RhinoMath.IsValidDouble(X))
        {
          c = System.Math.Abs(X);
          if (RhinoMath.IsValidDouble(Y) && System.Math.Abs(Y) < c)
            c = System.Math.Abs(Y);
        }
        else if (RhinoMath.IsValidDouble(Y))
        {
          c = System.Math.Abs(Y);
        }
        else
          c = RhinoMath.UnsetValue;
        return c;
      }
    }

    /// <summary>
    /// Gets the largest valid coordinate, or RhinoMath.UnsetValue if no coordinate is valid.
    /// </summary>
    /// <since>5.0</since>
    public double MaximumCoordinate
    {
      get
      {
        double c;
        if (RhinoMath.IsValidDouble(X))
        {
          c = System.Math.Abs(X);
          if (RhinoMath.IsValidDouble(Y) && System.Math.Abs(Y) > c)
            c = System.Math.Abs(Y);
        }
        else if (RhinoMath.IsValidDouble(Y))
        {
          c = System.Math.Abs(Y);
        }
        else
          c = RhinoMath.UnsetValue;
        return c;
      }
    }

    /// <summary>
    /// Gets a point at 0,0.
    /// </summary>
    /// <since>5.0</since>
    public static Point2d Origin
    {
      get { return new Point2d(0, 0); }
    }

    /// <summary>
    /// Gets a point at RhinoMath.UnsetValue,RhinoMath.UnsetValue.
    /// </summary>
    /// <since>5.0</since>
    public static Point2d Unset
    {
      get { return new Point2d(RhinoMath.UnsetValue, RhinoMath.UnsetValue); }
    }

    /// <summary>
    /// Computes the distance between two points.
    /// </summary>
    /// <param name="other">Another point.</param>
    /// <returns>The length of the line between the two points, or 0 if either point is invalid.</returns>
    /// <example>
    /// <code source='examples\vbnet\ex_leader.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_leader.cs' lang='cs'/>
    /// <code source='examples\py\ex_leader.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    [ConstOperation]
    public double DistanceTo(Point2d other)
    {
      double d;
      if (IsValid && other.IsValid)
      {
        Vector2d v = other - this;
        d = v.Length;
      }
      else
      {
        d = 0.0;
      }
      return d;
    }

    /// <summary>
    /// Computes the square of the distance between two 2d points.
    /// <para>This method is usually largely faster than DistanceTo().</para>
    /// </summary>
    /// <param name="other">Other point for squared distance measurement.</param>
    /// <returns>The squared length of the line between this and the other point; or 0 if any of the points is not valid.</returns>
    /// <since>8.4</since>
    [ConstOperation]
    public double DistanceToSquared(Point2d other) {
      double d;
      if (IsValid && other.IsValid) {
        d = (this - other).SquareLength;
      } else {
        d = 0.0;
      }
      return d;
    }

    /// <summary>
    /// Transforms the present point in place. The transformation matrix acts on the left of the point. i.e.,
    /// <para>result = transformation*point</para>
    /// </summary>
    /// <param name="xform">Transformation to apply.</param>
    /// <since>5.1</since>
    public void Transform(Transform xform)
    {
      double ww = xform.m_30 * m_x + xform.m_31 * m_y + xform.m_33;
      if (ww != 0.0) { ww = 1.0 / ww; }

      double tx = ww * (xform.m_00 * m_x + xform.m_01 * m_y + xform.m_03);
      double ty = ww * (xform.m_10 * m_x + xform.m_11 * m_y + xform.m_13);
      m_x = tx;
      m_y = ty;
    }
  }

  /// <summary>
  /// Represents the three coordinates of a point in three-dimensional space,
  /// using <see cref="double"/>-precision floating point values.
  /// </summary>
  [StructLayout(LayoutKind.Sequential, Pack = 8, Size = 24)]
  [DebuggerDisplay("({m_x}, {m_y}, {m_z})")]
  [Serializable]
  public struct Point3d :
    ISerializable, IEquatable<Point3d>, IComparable<Point3d>, IComparable,
    IEpsilonComparable<Point3d>, ICloneable, IValidable, IFormattable
  {
    #region members
    internal double m_x;
    internal double m_y;
    internal double m_z;
    #endregion

    #region constructors
    /// <summary>
    /// Initializes a new point by defining the X, Y and Z coordinates.
    /// </summary>
    /// <param name="x">The value of the X (first) coordinate.</param>
    /// <param name="y">The value of the Y (second) coordinate.</param>
    /// <param name="z">The value of the Z (third) coordinate.</param>
    /// <example>
    /// <code source='examples\vbnet\ex_addcircle.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_addcircle.cs' lang='cs'/>
    /// <code source='examples\py\ex_addcircle.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    public Point3d(double x, double y, double z)
    {
      m_x = x;
      m_y = y;
      m_z = z;
    }

    /// <summary>
    /// Initializes a new point by copying coordinates from the components of a vector.
    /// </summary>
    /// <param name="vector">A vector.</param>
    /// <since>5.0</since>
    public Point3d(Vector3d vector)
    {
      m_x = vector.m_x;
      m_y = vector.m_y;
      m_z = vector.m_z;
    }

    /// <summary>
    /// Initializes a new point by copying coordinates from a single-precision point.
    /// </summary>
    /// <param name="point">A point.</param>
    /// <since>5.0</since>
    public Point3d(Point3f point)
    {
      m_x = point.X;
      m_y = point.Y;
      m_z = point.Z;
    }

    /// <summary>
    /// Initializes a new point by copying coordinates from another point.
    /// </summary>
    /// <param name="point">A point.</param>
    /// <since>5.0</since>
    public Point3d(Point3d point)
    {
      m_x = point.X;
      m_y = point.Y;
      m_z = point.Z;
    }

    /// <summary>
    /// Initializes a new point by copying coordinates from a four-dimensional point.
    /// The first three coordinates are divided by the last one.
    /// If the W (fourth) dimension of the input point is zero, then it will be just discarded.
    /// </summary>
    /// <param name="point">A point.</param>
    /// <since>5.0</since>
    public Point3d(Point4d point)
    {
      double w = (point.m_w != 1.0 && point.m_w != 0.0) ? 1.0 / point.m_w : 1.0;
      m_x = point.m_x * w;
      m_y = point.m_y * w;
      m_z = point.m_z * w;
    }

    /// <summary>
    /// Gets the value of a point at location 0,0,0.
    /// </summary>
    /// <since>5.0</since>
    public static Point3d Origin
    {
      get { return new Point3d(0, 0, 0); }
    }

    /// <summary>
    /// Gets the value of a point at location RhinoMath.UnsetValue,RhinoMath.UnsetValue,RhinoMath.UnsetValue.
    /// </summary>
    /// <since>5.0</since>
    public static Point3d Unset
    {
      get { return new Point3d(RhinoMath.UnsetValue, RhinoMath.UnsetValue, RhinoMath.UnsetValue); }
    }

    private Point3d(SerializationInfo info, StreamingContext context)
    {
      m_x = info.GetDouble("X");
      m_y = info.GetDouble("Y");
      m_z = info.GetDouble("Z");
    }

    [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.SerializationFormatter)]
    void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
    {
      info.AddValue("X", m_x);
      info.AddValue("Y", m_y);
      info.AddValue("Z", m_z);
    }

    #endregion

    #region operators

    /// <summary>
    /// Multiplies a <see cref="Point3d"/> by a number.
    /// </summary>
    /// <param name="point">A point.</param>
    /// <param name="t">A number.</param>
    /// <returns>A new point that is coordinate-wise multiplied by t.</returns>
    /// <since>5.0</since>
    public static Point3d operator *(Point3d point, double t)
    {
      return new Point3d(point.m_x * t, point.m_y * t, point.m_z * t);
    }

    /// <summary>
    /// Multiplies a <see cref="Point3d"/> by a number.
    /// <para>(Provided for languages that do not support operator overloading. You can use the * operator otherwise)</para>
    /// </summary>
    /// <param name="point">A point.</param>
    /// <param name="t">A number.</param>
    /// <returns>A new point that is coordinate-wise multiplied by t.</returns>
    /// <since>5.0</since>
    public static Point3d Multiply(Point3d point, double t)
    {
      return new Point3d(point.m_x * t, point.m_y * t, point.m_z * t);
    }

    /// <summary>
    /// Multiplies a <see cref="Point3d"/> by a number.
    /// </summary>
    /// <param name="point">A point.</param>
    /// <param name="t">A number.</param>
    /// <returns>A new point that is coordinate-wise multiplied by t.</returns>
    /// <since>5.0</since>
    public static Point3d operator *(double t, Point3d point)
    {
      return new Point3d(point.m_x * t, point.m_y * t, point.m_z * t);
    }

    /// <summary>
    /// Multiplies a <see cref="Point3d"/> by a number.
    /// <para>(Provided for languages that do not support operator overloading. You can use the * operator otherwise)</para>
    /// </summary>
    /// <param name="point">A point.</param>
    /// <param name="t">A number.</param>
    /// <returns>A new point that is coordinate-wise multiplied by t.</returns>
    /// <since>5.0</since>
    public static Point3d Multiply(double t, Point3d point)
    {
      return new Point3d(point.m_x * t, point.m_y * t, point.m_z * t);
    }

    /// <summary>
    /// Divides a <see cref="Point3d"/> by a number.
    /// </summary>
    /// <param name="point">A point.</param>
    /// <param name="t">A number.</param>
    /// <returns>A new point that is coordinate-wise divided by t.</returns>
    /// <since>5.0</since>
    public static Point3d operator /(Point3d point, double t)
    {
      return new Point3d(point.m_x / t, point.m_y / t, point.m_z / t);
    }

    /// <summary>
    /// Divides a <see cref="Point3d"/> by a number.
    /// <para>(Provided for languages that do not support operator overloading. You can use the / operator otherwise)</para>
    /// </summary>
    /// <param name="point">A point.</param>
    /// <param name="t">A number.</param>
    /// <returns>A new point that is coordinate-wise divided by t.</returns>
    /// <since>5.0</since>
    public static Point3d Divide(Point3d point, double t)
    {
      return new Point3d(point.m_x / t, point.m_y / t, point.m_z / t);
    }

    /// <summary>
    /// Sums two <see cref="Point3d"/> instances.
    /// </summary>
    /// <param name="point1">A point.</param>
    /// <param name="point2">A point.</param>
    /// <returns>A new point that results from the addition of point1 and point2.</returns>
    public static Point3d operator +(Point3d point1, Point3d point2)
    {
      return new Point3d(point1.m_x + point2.m_x, point1.m_y + point2.m_y, point1.m_z + point2.m_z);
    }

    /// <summary>
    /// Sums two <see cref="Point3d"/> instances.
    /// <para>(Provided for languages that do not support operator overloading. You can use the + operator otherwise)</para>
    /// </summary>
    /// <param name="point1">A point.</param>
    /// <param name="point2">A point.</param>
    /// <returns>A new point that results from the addition of point1 and point2.</returns>
    /// <since>5.0</since>
    public static Point3d Add(Point3d point1, Point3d point2)
    {
      return new Point3d(point1.m_x + point2.m_x, point1.m_y + point2.m_y, point1.m_z + point2.m_z);
    }

    /// <summary>
    /// Sums up a point and a vector, and returns a new point.
    /// </summary>
    /// <param name="point">A point.</param>
    /// <param name="vector">A vector.</param>
    /// <returns>A new point that results from the addition of point and vector.</returns>
    public static Point3d operator +(Point3d point, Vector3d vector)
    {
      return new Point3d(point.m_x + vector.m_x, point.m_y + vector.m_y, point.m_z + vector.m_z);
    }

    /// <summary>
    /// Sums up a point and a vector, and returns a new point.
    /// <para>(Provided for languages that do not support operator overloading. You can use the + operator otherwise)</para>
    /// </summary>
    /// <param name="point">A point.</param>
    /// <param name="vector">A vector.</param>
    /// <returns>A new point that results from the addition of point and vector.</returns>
    /// <since>5.0</since>
    public static Point3d Add(Point3d point, Vector3d vector)
    {
      return new Point3d(point.m_x + vector.m_x, point.m_y + vector.m_y, point.m_z + vector.m_z);
    }

    /// <summary>
    /// Sums up a point and a vector, and returns a new point.
    /// </summary>
    /// <param name="point">A point.</param>
    /// <param name="vector">A vector.</param>
    /// <returns>A new point that results from the addition of point and vector.</returns>
    public static Point3d operator +(Point3d point, Vector3f vector)
    {
      return new Point3d(point.m_x + vector.m_x, point.m_y + vector.m_y, point.m_z + vector.m_z);
    }

    /// <summary>
    /// Sums up a point and a vector, and returns a new point.
    /// <para>(Provided for languages that do not support operator overloading. You can use the + operator otherwise)</para>
    /// </summary>
    /// <param name="point">A point.</param>
    /// <param name="vector">A vector.</param>
    /// <returns>A new point that results from the addition of point and vector.</returns>
    /// <since>5.0</since>
    public static Point3d Add(Point3d point, Vector3f vector)
    {
      return new Point3d(point.m_x + vector.m_x, point.m_y + vector.m_y, point.m_z + vector.m_z);
    }

    /// <summary>
    /// Sums up a point and a vector, and returns a new point.
    /// </summary>
    /// <param name="vector">A vector.</param>
    /// <param name="point">A point.</param>
    /// <returns>A new point that results from the addition of point and vector.</returns>
    public static Point3d operator +(Vector3d vector, Point3d point)
    {
      return new Point3d(point.m_x + vector.m_x, point.m_y + vector.m_y, point.m_z + vector.m_z);
    }

    /// <summary>
    /// Sums up a point and a vector, and returns a new point.
    /// <para>(Provided for languages that do not support operator overloading. You can use the + operator otherwise)</para>
    /// </summary>
    /// <param name="vector">A vector.</param>
    /// <param name="point">A point.</param>
    /// <returns>A new point that results from the addition of point and vector.</returns>
    /// <since>5.0</since>
    public static Point3d Add(Vector3d vector, Point3d point)
    {
      return new Point3d(point.m_x + vector.m_x, point.m_y + vector.m_y, point.m_z + vector.m_z);
    }

    /// <summary>
    /// Subtracts a vector from a point.
    /// </summary>
    /// <param name="point">A point.</param>
    /// <param name="vector">A vector.</param>
    /// <returns>A new point that is the difference of point minus vector.</returns>
    /// <since>5.0</since>
    public static Point3d operator -(Point3d point, Vector3d vector)
    {
      return new Point3d(point.m_x - vector.m_x, point.m_y - vector.m_y, point.m_z - vector.m_z);
    }

    /// <summary>
    /// Subtracts a vector from a point.
    /// <para>(Provided for languages that do not support operator overloading. You can use the - operator otherwise)</para>
    /// </summary>
    /// <param name="vector">A vector.</param>
    /// <param name="point">A point.</param>
    /// <returns>A new point that is the difference of point minus vector.</returns>
    /// <since>5.0</since>
    public static Point3d Subtract(Point3d point, Vector3d vector)
    {
      return new Point3d(point.m_x - vector.m_x, point.m_y - vector.m_y, point.m_z - vector.m_z);
    }

    /// <summary>
    /// Subtracts a point from another point.
    /// </summary>
    /// <param name="point1">A point.</param>
    /// <param name="point2">Another point.</param>
    /// <returns>A new vector that is the difference of point minus vector.</returns>
    /// <since>5.0</since>
    public static Vector3d operator -(Point3d point1, Point3d point2)
    {
      return new Vector3d(point1.m_x - point2.m_x, point1.m_y - point2.m_y, point1.m_z - point2.m_z);
    }

    /// <summary>
    /// Subtracts a point from another point.
    /// <para>(Provided for languages that do not support operator overloading. You can use the - operator otherwise)</para>
    /// </summary>
    /// <param name="point1">A point.</param>
    /// <param name="point2">Another point.</param>
    /// <returns>A new vector that is the difference of point minus vector.</returns>
    /// <since>5.0</since>
    public static Vector3d Subtract(Point3d point1, Point3d point2)
    {
      return new Vector3d(point1.m_x - point2.m_x, point1.m_y - point2.m_y, point1.m_z - point2.m_z);
    }

    /// <summary>
    /// Computes the additive inverse of all coordinates in the point, and returns the new point.
    /// </summary>
    /// <param name="point">A point.</param>
    /// <returns>A point value that, when summed with the point input, yields the <see cref="Origin"/>.</returns>
    /// <since>5.0</since>
    public static Point3d operator -(Point3d point)
    {
      return new Point3d(-point.m_x, -point.m_y, -point.m_z);
    }

    /// <summary>
    /// Determines whether two Point3d have equal values.
    /// </summary>
    /// <param name="a">The first point.</param>
    /// <param name="b">The second point.</param>
    /// <returns>true if the coordinates of the two points are exactly equal; otherwise false.</returns>
    /// <since>5.0</since>
    public static bool operator ==(Point3d a, Point3d b)
    {
      return (a.m_x == b.m_x && a.m_y == b.m_y && a.m_z == b.m_z);
    }

    /// <summary>
    /// Determines whether two Point3d have different values.
    /// </summary>
    /// <param name="a">The first point.</param>
    /// <param name="b">The second point.</param>
    /// <returns>true if the two points differ in any coordinate; false otherwise.</returns>
    /// <since>5.0</since>
    public static bool operator !=(Point3d a, Point3d b)
    {
      return (a.m_x != b.m_x || a.m_y != b.m_y || a.m_z != b.m_z);
    }

    /// <summary>
    /// Converts a point in a control point, without needing casting.
    /// </summary>
    /// <param name="pt">The point.</param>
    /// <returns>The control point.</returns>
    public static implicit operator ControlPoint(Point3d pt)
    {
      return new ControlPoint(pt);
    }

    /// <summary>
    /// Converts a point in a vector, needing casting.
    /// </summary>
    /// <param name="point">A point.</param>
    /// <returns>The resulting vector.</returns>
    public static explicit operator Vector3d(Point3d point)
    //David: made this operator explicit on jan-22 2011, it was causing problems with the VB compiler.
    {
      return new Vector3d(point);
    }

    /// <summary>
    /// Converts a vector in a point, needing casting.
    /// </summary>
    /// <param name="vector">A vector.</param>
    /// <returns>The resulting point.</returns>
    public static explicit operator Point3d(Vector3d vector)
    //David: made this operator explicit on jan-22 2011, it was causing problems with the VB compiler.
    {
      return new Point3d(vector);
    }

    /// <summary>
    /// Converts a single-precision point in a double-precision point, without needing casting.
    /// </summary>
    /// <param name="point">A point.</param>
    /// <returns>The resulting point.</returns>
    public static implicit operator Point3d(Point3f point)
    {
      return new Point3d(point);
    }

    /// <summary>
    /// Converts a single-precision point in a double-precision point.
    /// </summary>
    /// <param name="point">A point.</param>
    /// <returns>The resulting point.</returns>
    /// <since>6.0</since>
    public static Point3d FromPoint3f(Point3f point)
    {
      return new Point3d(point);
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
    public static bool operator <(Point3d a, Point3d b)
    {
      if (a.X < b.X)
        return true;
      if (a.X == b.X)
      {
        if (a.Y < b.Y)
          return true;
        if (a.Y == b.Y && a.Z < b.Z)
          return true;
      }
      return false;
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
    public static bool operator <=(Point3d a, Point3d b)
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
    public static bool operator >(Point3d a, Point3d b)
    {
      if (a.X > b.X)
        return true;
      if (a.X == b.X)
      {
        if (a.Y > b.Y)
          return true;
        if (a.Y == b.Y && a.Z > b.Z)
          return true;
      }
      return false;
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
    public static bool operator >=(Point3d a, Point3d b)
    {
      return a.CompareTo(b) >= 0;
    }

    #endregion

    #region properties
    /// <summary>
    /// Gets or sets the X (first) coordinate of this point.
    /// </summary>
    /// <since>5.0</since>
    public double X { get { return m_x; } set { m_x = value; } }

    /// <summary>
    /// Gets or sets the Y (second) coordinate of this point.
    /// </summary>
    /// <since>5.0</since>
    public double Y { get { return m_y; } set { m_y = value; } }

    /// <summary>
    /// Gets or sets the Z (third) coordinate of this point.
    /// </summary>
    /// <since>5.0</since>
    public double Z { get { return m_z; } set { m_z = value; } }

    /// <summary>
    /// Gets or sets an indexed coordinate of this point.
    /// </summary>
    /// <param name="index">
    /// The coordinate index. Valid values are:
    /// <para>0 = X coordinate</para>
    /// <para>1 = Y coordinate</para>
    /// <para>2 = Z coordinate</para>
    /// .</param>
    public double this[int index]
    {
      get
      {
        if (0 == index)
          return m_x;
        if (1 == index)
          return m_y;
        if (2 == index)
          return m_z;
        // IronPython works with indexing is we thrown an IndexOutOfRangeException
        throw new IndexOutOfRangeException();
      }
      set
      {
        if (0 == index)
          m_x = value;
        else if (1 == index)
          m_y = value;
        else if (2 == index)
          m_z = value;
        else
          throw new IndexOutOfRangeException();
      }
    }

    /// <summary>
    /// Each coordinate of the point must pass the <see cref="RhinoMath.IsValidDouble"/> test.
    /// </summary>
    /// <since>5.0</since>
    public bool IsValid
    {
      get { return RhinoMath.IsValidDouble(m_x) && RhinoMath.IsValidDouble(m_y) && RhinoMath.IsValidDouble(m_z); }
    }

    /// <summary>
    /// Gets the smallest (both positive and negative) coordinate value in this point.
    /// </summary>
    /// <since>5.0</since>
    public double MinimumCoordinate
    {
      get
      {
        double c;
        if (RhinoMath.IsValidDouble(m_x))
        {
          c = System.Math.Abs(m_x);
          if (RhinoMath.IsValidDouble(m_y) && System.Math.Abs(m_y) < c)
            c = System.Math.Abs(m_y);
          if (RhinoMath.IsValidDouble(m_z) && System.Math.Abs(m_z) < c)
            c = System.Math.Abs(m_z);
        }
        else if (RhinoMath.IsValidDouble(m_y))
        {
          c = System.Math.Abs(m_y);
          if (RhinoMath.IsValidDouble(m_z) && System.Math.Abs(m_z) < c)
            c = System.Math.Abs(m_z);
        }
        else if (RhinoMath.IsValidDouble(m_z))
        {
          c = System.Math.Abs(m_z);
        }
        else
          c = RhinoMath.UnsetValue;
        return c;
      }
    }

    /// <summary>
    /// Gets the largest (both positive and negative) valid coordinate in this point,
    /// or RhinoMath.UnsetValue if no coordinate is valid, as an absolute value.
    /// </summary>
    /// <since>5.0</since>
    public double MaximumCoordinate
    {
      get
      {
        double c;
        if (RhinoMath.IsValidDouble(m_x))
        {
          c = System.Math.Abs(m_x);
          if (RhinoMath.IsValidDouble(m_y) && System.Math.Abs(m_y) > c)
            c = System.Math.Abs(m_y);
          if (RhinoMath.IsValidDouble(m_z) && System.Math.Abs(m_z) > c)
            c = System.Math.Abs(m_z);
        }
        else if (RhinoMath.IsValidDouble(m_y))
        {
          c = System.Math.Abs(m_y);
          if (RhinoMath.IsValidDouble(m_z) && System.Math.Abs(m_z) > c)
            c = System.Math.Abs(m_z);
        }
        else if (RhinoMath.IsValidDouble(m_z))
        {
          c = System.Math.Abs(m_z);
        }
        else
          c = RhinoMath.UnsetValue;
        return c;
      }
    }


    #endregion

    #region methods
    /// <summary>
    /// Determines whether the specified <see cref="object"/> is a <see cref="Point3d"/> and has the same values as the present point.
    /// </summary>
    /// <param name="obj">The specified object.</param>
    /// <returns>true if obj is a Point3d and has the same coordinates as this; otherwise false.</returns>
    [ConstOperation]
    public override bool Equals(object obj)
    {
      return (obj is Point3d && this == (Point3d)obj);
    }

    /// <summary>
    /// Check that all values in other are within epsilon of the values in this
    /// </summary>
    /// <param name="other"></param>
    /// <param name="epsilon"></param>
    /// <returns></returns>
    /// <since>5.4</since>
    [ConstOperation]
    public bool EpsilonEquals(Point3d other, double epsilon)
    {
      return RhinoMath.EpsilonEquals(m_x, other.m_x, epsilon) &&
             RhinoMath.EpsilonEquals(m_y, other.m_y, epsilon) &&
             RhinoMath.EpsilonEquals(m_z, other.m_z, epsilon);
    }

    /// <summary>
    /// Compares this <see cref="Point3d" /> with another <see cref="Point3d" />.
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
    public int CompareTo(Point3d other)
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
      if (obj is Point3d)
        return CompareTo((Point3d)obj);

      throw new ArgumentException("Input must be of type Point3d", "obj");
    }

    /// <summary>
    /// Determines whether the specified <see cref="Point3d"/> has the same values as the present point.
    /// </summary>
    /// <param name="point">The specified point.</param>
    /// <returns>true if point has the same coordinates as this; otherwise false.</returns>
    /// <since>5.0</since>
    [ConstOperation]
    public bool Equals(Point3d point)
    {
      return this == point;
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
    /// Interpolate between two points.
    /// </summary>
    /// <param name="pA">First point.</param>
    /// <param name="pB">Second point.</param>
    /// <param name="t">Interpolation parameter. 
    /// If t=0 then this point is set to pA. 
    /// If t=1 then this point is set to pB. 
    /// Values of t in between 0.0 and 1.0 result in points between pA and pB.</param>
    /// <since>5.0</since>
    public void Interpolate(Point3d pA, Point3d pB, double t)
    {
      m_x = pA.m_x + t * (pB.m_x - pA.m_x);
      m_y = pA.m_y + t * (pB.m_y - pA.m_y);
      m_z = pA.m_z + t * (pB.m_z - pA.m_z);
    }

    /// <summary>
    /// Constructs the string representation for the current point.
    /// </summary>
    /// <returns>The point representation in the form X,Y,Z.</returns>
    [ConstOperation]
    public override string ToString()
    {
      var culture = System.Globalization.CultureInfo.InvariantCulture;
      return string.Format("{0},{1},{2}", m_x.ToString(culture), m_y.ToString(culture), m_z.ToString(culture));
    }

    /// <inheritdoc />
    /// <since>7.0</since>
    [ConstOperation]
    public string ToString(string format, IFormatProvider formatProvider)
    {
      return FormatCoordinates(format, formatProvider, m_x, m_y, m_z);
    }

    /// <summary>
    /// Utility method for formatting coordinate groups.
    /// </summary>
    internal static string FormatCoordinates(string format, IFormatProvider provider, params double[] coordinates)
    {
      var fragments = new string[coordinates.Length];
      for (int i = 0; i < coordinates.Length; i++)
        fragments[i] = coordinates[i].ToString(format, provider);

      // There's a problem here, if the coordinates are formatted using
      // commas as decimal separators, then we cannot use commas to
      // separate the (x,y,z) coordinates from each other.
      // IFormatProvider *may* be a type of CultureInfo which could
      // tell us whether the decimal separator is indeed the comma.
      bool useCommaSeparators = true;
      if (provider is CultureInfo culture)
      {
        if (IsCommaLikeText(culture.NumberFormat.NumberDecimalSeparator))
          useCommaSeparators = false;
      }
      else
      {
        // Since the format provider is not a cultureinfo implementation,
        // format a number to see if it contains any commas:
        var test = (-12345.67890).ToString(format, provider);
        if (IsCommaLikeText(test))
          useCommaSeparators = false;
      }

      // Either separate the coordinates using commas, or semi-colons.
      if (useCommaSeparators)
        return string.Join(",", fragments);
      else
        return string.Join(";", fragments);
    }
    /// <summary>
    /// Test whether a string contains any char which looks like a comma.
    /// </summary>
    private static bool IsCommaLikeText(string text)
    {
      foreach (var c in text)
        if (IsCommaLikeChar(c))
          return true;
      return false;
    }
    /// <summary>
    /// Test whether a char looks like a comma.
    /// </summary>
    private static bool IsCommaLikeChar(char character)
    {
      // Regular comma.
      if (character == 0x002C) return true;

      // This happens a lot, may as well speed up the checks.
      if (character < 0x0600) return false;

      // Arabic decimal separator.
      if (character == 0x066B) return true;

      // Arabic thousand separator.
      if (character == 0x066C) return true;

      // Raised comma.
      if (character == 0x2E34) return true;

      // Small comma.
      if (character == 0xFE50) return true;

      // Full width comma.
      if (character == 0xFF0C) return true;

      return false;
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
    public double DistanceTo(Point3d other)
    {
      double d;
      if (IsValid && other.IsValid)
      {
        double dx = other.m_x - m_x;
        double dy = other.m_y - m_y;
        double dz = other.m_z - m_z;
        d = Vector3d.GetLengthHelper(dx, dy, dz);
      }
      else
      {
        d = 0.0;
      }
      return d;
    }

    /// <summary>
    /// Computes the square of the distance between two points.
    /// <para>This method is usually largely faster than DistanceTo().</para>
    /// </summary>
    /// <param name="other">Other point for squared distance measurement.</param>
    /// <returns>The squared length of the line between this and the other point; or 0 if any of the points is not valid.</returns>
    /// <since>6.0</since>
    [ConstOperation]
    public double DistanceToSquared(Point3d other)
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
    /// <example>
    /// <code source='examples\vbnet\ex_pointatcursor.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_pointatcursor.cs' lang='cs'/>
    /// </example>
    /// <since>5.0</since>
    public void Transform(Transform xform)
    {
      //David: this method doesn't test for validity. Should it?
      double ww = xform.m_30 * m_x + xform.m_31 * m_y + xform.m_32 * m_z + xform.m_33;
      if (ww != 0.0) { ww = 1.0 / ww; }

      double tx = ww * (xform.m_00 * m_x + xform.m_01 * m_y + xform.m_02 * m_z + xform.m_03);
      double ty = ww * (xform.m_10 * m_x + xform.m_11 * m_y + xform.m_12 * m_z + xform.m_13);
      double tz = ww * (xform.m_20 * m_x + xform.m_21 * m_y + xform.m_22 * m_z + xform.m_23);
      m_x = tx;
      m_y = ty;
      m_z = tz;
    }

    /// <summary>
    /// Removes duplicates in the supplied set of points.
    /// </summary>
    /// <param name="points">A list, an array or any enumerable of <see cref="Point3d"/>.</param>
    /// <param name="tolerance">The minimum distance between points.
    /// <para>Points that fall within this tolerance will be discarded.</para>
    /// .</param>
    /// <returns>An array of points without duplicates; or null on error.</returns>
    /// <since>5.0</since>
    public static Point3d[] CullDuplicates(System.Collections.Generic.IEnumerable<Point3d> points, double tolerance)
    {
      if (null == points)
        return null;

      // This code duplicates the static function CullDuplicatePoints in tl_brep_intersect.cpp
      Rhino.Collections.Point3dList point_list = new Rhino.Collections.Point3dList(points);
      int count = point_list.Count;
      if (0 == count)
        return null;

      bool[] dup_list = new bool[count];
      Rhino.Collections.Point3dList non_dups = new Rhino.Collections.Point3dList(count);

      for (int i = 0; i < count; i++)
      {
        // Check if the entry has been flagged as a duplicate
        if (dup_list[i] == false)
        {
          non_dups.Add(point_list[i]);
          // Only compare with entries that haven't been checked
          for (int j = i + 1; j < count; j++)
          {
            if (point_list[i].DistanceTo(point_list[j]) <= tolerance)
              dup_list[j] = true;
          }
        }
      }

      return non_dups.ToArray();
    }

    object ICloneable.Clone()
    {
      return this;
    }

    #endregion

    #region Rhino SDK functions
#if RHINO_SDK
    /// <summary>
    /// Determines whether a set of points is coplanar within a given tolerance.
    /// </summary>
    /// <param name="points">A list, an array or any enumerable of <see cref="Point3d"/>.</param>
    /// <param name="tolerance">A tolerance value. A default might be RhinoMath.ZeroTolerance.</param>
    /// <returns>true if points are on the same plane; false otherwise.</returns>
    /// <since>5.0</since>
    public static bool ArePointsCoplanar(System.Collections.Generic.IEnumerable<Point3d> points, double tolerance)
    {
      int count;
      Point3d[] arrPoints = Rhino.Collections.RhinoListHelpers.GetConstArray(points, out count);
      if (count < 1 || null == arrPoints)
        throw new ArgumentException("points must contain at least 1 point");
      return UnsafeNativeMethods.RHC_RhinoArePointsCoplanar(count, arrPoints, tolerance);
    }

    /// <summary>
    /// Converts the string representation of a point to the equivalent Point3d structure.
    /// </summary>
    /// <param name="input">The point to convert.</param>
    /// <param name="result">The structure that will contain the parsed value.</param>
    /// <returns>true if successful, false otherwise.</returns>
    /// <since>6.12</since>
    public static bool TryParse(string input, out Point3d result)
    {
      result = Point3d.Unset;
      return UnsafeNativeMethods.RHC_RhinoParsePoint(input, ref result);
    }

    /// <summary>
    /// Orders a set of points so they will be connected in a "reasonable polyline" order.
    /// <para>Also, removes points from the list if their common distance exceeds a specified threshold.</para>
    /// </summary>
    /// <param name="points">A list, an array or any enumerable of <see cref="Point3d"/>.</param>
    /// <param name="minimumDistance">Minimum allowed distance among a pair of points. If points are closer than this, only one of them will be kept.</param>
    /// <returns>The new array of sorted and culled points.</returns>
    /// <since>5.0</since>
    public static Point3d[] SortAndCullPointList(System.Collections.Generic.IEnumerable<Point3d> points, double minimumDistance)
    {
      int count;
      Point3d[] arrPoints = Rhino.Collections.RhinoListHelpers.GetConstArray(points, out count);
      if (count < 1 || null == arrPoints)
        return null;
      bool rc = UnsafeNativeMethods.TLC_SortPointList(arrPoints, ref count, minimumDistance);
      if (false == rc)
        return null;
      if (count < arrPoints.Length)
      {
        Point3d[] destPoints = new Point3d[count];
        System.Array.Copy(arrPoints, destPoints, count);
        arrPoints = destPoints;
      }
      return arrPoints;
    }
#endif
    #endregion
  }

  /// <summary>
  /// Represents the four coordinates of a point in four-dimensional space.
  /// <para>The W (fourth) dimension is often considered the weight of the point as seen in 3D space.</para>
  /// </summary>
  [StructLayout(LayoutKind.Sequential, Pack = 8, Size = 32)]
  [DebuggerDisplay("({m_x}, {m_y}, {m_z}, [{m_w}])")]
  [Serializable]
  public struct Point4d : ISerializable, IEquatable<Point4d>, IEpsilonComparable<Point4d>, IValidable, IFormattable
  {
    internal double m_x;
    internal double m_y;
    internal double m_z;
    internal double m_w;

    /// <summary>
    /// Initializes a new instance of the <see cref="Point4d"/> class based on coordinates.
    /// </summary>
    /// <param name="x">The X (first) dimension.</param>
    /// <param name="y">The Y (second) dimension.</param>
    /// <param name="z">The Z (third) dimension.</param>
    /// <param name="w">The W (fourth) dimension, or weight.</param>
    /// <since>5.0</since>
    public Point4d(double x, double y, double z, double w)
    {
      m_x = x;
      m_y = y;
      m_z = z;
      m_w = w;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Point4d"/> class from the coordinates of a point.
    /// </summary>
    /// <param name="point">.</param>
    /// <since>5.0</since>
    public Point4d(Point3d point)
    {
      m_x = point.m_x;
      m_y = point.m_y;
      m_z = point.m_z;
      m_w = 1.0;
    }

    /// <summary>
    /// Initializes a new point by copying coordinates from another point.
    /// </summary>
    /// <param name="point">A point.</param>
    /// <since>6.0</since>
    public Point4d(Point4d point)
    {
      m_x = point.X;
      m_y = point.Y;
      m_z = point.Z;
      m_w = point.W;
    }

    private Point4d(SerializationInfo info, StreamingContext context)
    {
      m_x = info.GetDouble("X");
      m_y = info.GetDouble("Y");
      m_z = info.GetDouble("Z");
      m_w = info.GetDouble("W");
    }

    [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.SerializationFormatter)]
    void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
    {
      info.AddValue("X", m_x);
      info.AddValue("Y", m_y);
      info.AddValue("Z", m_z);
      info.AddValue("W", m_w);
    }

    /// <summary>
    /// Gets or sets the X (first) coordinate of this point.
    /// </summary>
    /// <since>5.0</since>
    public double X { get { return m_x; } set { m_x = value; } }

    /// <summary>
    /// Gets or sets the Y (second) coordinate of this point.
    /// </summary>
    /// <since>5.0</since>
    public double Y { get { return m_y; } set { m_y = value; } }

    /// <summary>
    /// Gets or sets the Z (third) coordinate of this point.
    /// </summary>
    /// <since>5.0</since>
    public double Z { get { return m_z; } set { m_z = value; } }

    /// <summary>
    /// Gets or sets the W (fourth) coordinate -or weight- of this point.
    /// </summary>
    /// <since>5.0</since>
    public double W { get { return m_w; } set { m_w = value; } }


    #region operators
    /// <summary>
    /// Sums two <see cref="Point4d"/> together.
    /// </summary>
    /// <param name="point1">First point.</param>
    /// <param name="point2">Second point.</param>
    /// <returns>A new point that results from the weighted addition of point1 and point2.</returns>
    public static Point4d operator +(Point4d point1, Point4d point2)
    {
      Point4d rc = point1; //copy of the value
      if (point2.m_w == point1.m_w)
      {
        rc.m_x += point2.m_x;
        rc.m_y += point2.m_y;
        rc.m_z += point2.m_z;
      }
      else if (point2.m_w == 0)
      {
        rc.m_x += point2.m_x;
        rc.m_y += point2.m_y;
        rc.m_z += point2.m_z;
      }
      else if (point1.m_w == 0)
      {
        rc.m_x += point2.m_x;
        rc.m_y += point2.m_y;
        rc.m_z += point2.m_z;
        rc.m_w = point2.m_w;
      }
      else
      {
        double sw1 = (point1.m_w > 0.0) ? Math.Sqrt(point1.m_w) : -Math.Sqrt(-point1.m_w);
        double sw2 = (point2.m_w > 0.0) ? Math.Sqrt(point2.m_w) : -Math.Sqrt(-point2.m_w);
        double s1 = sw2 / sw1;
        double s2 = sw1 / sw2;
        rc.m_x = point1.m_x * s1 + point2.m_x * s2;
        rc.m_y = point1.m_y * s1 + point2.m_y * s2;
        rc.m_z = point1.m_z * s1 + point2.m_z * s2;
        rc.m_w = sw1 * sw2;
      }
      return rc;
    }

    /// <summary>
    /// Sums two <see cref="Point4d"/> together.
    /// <para>(Provided for languages that do not support operator overloading. You can use the + operator otherwise)</para>
    /// </summary>
    /// <param name="point1">First point.</param>
    /// <param name="point2">Second point.</param>
    /// <returns>A new point that results from the weighted addition of point1 and point2.</returns>
    /// <since>5.0</since>
    public static Point4d Add(Point4d point1, Point4d point2)
    {
      return point1 + point2;
    }

    /// <summary>
    /// Subtracts the second point from the first point.
    /// </summary>
    /// <param name="point1">First point.</param>
    /// <param name="point2">Second point.</param>
    /// <returns>A new point that results from the weighted subtraction of point2 from point1.</returns>
    /// <since>5.0</since>
    public static Point4d operator -(Point4d point1, Point4d point2)
    {
      Point4d rc = point1; //copy of the value
      if (point2.m_w == point1.m_w)
      {
        rc.m_x -= point2.m_x;
        rc.m_y -= point2.m_y;
        rc.m_z -= point2.m_z;
      }
      else if (point2.m_w == 0.0)
      {
        rc.m_x -= point2.m_x;
        rc.m_y -= point2.m_y;
        rc.m_z -= point2.m_z;
      }
      else if (point1.m_w == 0.0)
      {
        rc.m_x -= point2.m_x;
        rc.m_y -= point2.m_y;
        rc.m_z -= point2.m_z;
        rc.m_w = point2.m_w;
      }
      else
      {
        double sw1 = (point1.m_w > 0.0) ? Math.Sqrt(point1.m_w) : -Math.Sqrt(-point1.m_w);
        double sw2 = (point2.m_w > 0.0) ? Math.Sqrt(point2.m_w) : -Math.Sqrt(-point2.m_w);
        double s1 = sw2 / sw1;
        double s2 = sw1 / sw2;
        rc.m_x = point1.m_x * s1 - point2.m_x * s2;
        rc.m_y = point1.m_y * s1 - point2.m_y * s2;
        rc.m_z = point1.m_z * s1 - point2.m_z * s2;
        rc.m_w = sw1 * sw2;
      }
      return rc;
    }

    /// <summary>
    /// Subtracts the second point from the first point.
    /// <para>(Provided for languages that do not support operator overloading. You can use the - operator otherwise)</para>
    /// </summary>
    /// <param name="point1">First point.</param>
    /// <param name="point2">Second point.</param>
    /// <returns>A new point that results from the weighted subtraction of point2 from point1.</returns>
    /// <since>5.0</since>
    public static Point4d Subtract(Point4d point1, Point4d point2)
    {
      return point1 - point2;
    }

    /// <summary>
    /// Multiplies a point by a number.
    /// </summary>
    /// <param name="point">A point.</param>
    /// <param name="d">A number.</param>
    /// <returns>A new point that results from the coordinate-wise multiplication of point with d.</returns>
    /// <since>5.0</since>
    public static Point4d operator *(Point4d point, double d)
    {
      return new Point4d(point.m_x * d, point.m_y * d, point.m_z * d, point.m_w * d);
    }

    /// <summary>
    /// Multiplies a point by a number.
    /// <para>(Provided for languages that do not support operator overloading. You can use the * operator otherwise)</para>
    /// </summary>
    /// <param name="point">A point.</param>
    /// <param name="d">A number.</param>
    /// <returns>A new point that results from the coordinate-wise multiplication of point with d.</returns>
    /// <since>5.0</since>
    public static Point4d Multiply(Point4d point, double d)
    {
      return point * d;
    }

    /// <summary>
    /// Multiplies two <see cref="Point4d"/> together, returning the dot (internal) product of the two.
    /// This is not the cross product.
    /// </summary>
    /// <param name="point1">The first point.</param>
    /// <param name="point2">The second point.</param>
    /// <returns>A value that results from the coordinate-wise multiplication of point1 and point2.</returns>
    /// <since>5.0</since>
    public static double operator *(Point4d point1, Point4d point2)
    {
      return (point1.m_x * point2.m_x) +
        (point1.m_y * point2.m_y) +
        (point1.m_z * point2.m_z) +
        (point1.m_w * point2.m_w);
    }

    /// <summary>
    /// Determines whether two Point4d have equal values.
    /// </summary>
    /// <param name="a">The first point.</param>
    /// <param name="b">The second point.</param>
    /// <returns>true if the coordinates of the two points are equal; otherwise false.</returns>
    /// <since>5.0</since>
    public static bool operator ==(Point4d a, Point4d b)
    {
      return UnsafeNativeMethods.ON_4dPoint_Equality(a, b);
    }

    /// <summary>
    /// Determines whether two Point4d have different values.
    /// </summary>
    /// <param name="a">The first point.</param>
    /// <param name="b">The second point.</param>
    /// <returns>true if the two points differ in any coordinate; false otherwise.</returns>
    /// <since>5.0</since>
    public static bool operator !=(Point4d a, Point4d b)
    {
      return !(a == b);
    }
    #endregion

    /// <summary>
    /// Determines whether the specified System.Object is Point4d and has same coordinates as the present point.
    /// </summary>
    /// <param name="obj">The specified object.</param>
    /// <returns>true if obj is Point4d and has the same coordinates as this; otherwise false.</returns>
    [ConstOperation]
    public override bool Equals(object obj)
    {
      return (obj is Point4d && this == (Point4d)obj);
    }

    /// <summary>
    /// Determines whether the specified point has same value as the present point.
    /// </summary>
    /// <param name="point">The specified point.</param>
    /// <returns>true if point has the same value as this; otherwise false.</returns>
    /// <since>5.0</since>
    [ConstOperation]
    public bool Equals(Point4d point)
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
    public bool EpsilonEquals(Point4d other, double epsilon)
    {
      return RhinoMath.EpsilonEquals(m_x, other.m_x, epsilon) &&
             RhinoMath.EpsilonEquals(m_y, other.m_y, epsilon) &&
             RhinoMath.EpsilonEquals(m_z, other.m_z, epsilon) &&
             RhinoMath.EpsilonEquals(m_w, other.m_w, epsilon);
    }

    /// <summary>
    /// Computes the hash code for the present point.
    /// </summary>
    /// <returns>A non-unique hash code, which uses all coordinates of this object.</returns>
    [ConstOperation]
    public override int GetHashCode()
    {
      // operator == uses normalized values to compare. This should
      // also be done for GetHashCode so we get similar results
      Point4d x = this;
      UnsafeNativeMethods.ON_4dPoint_Normalize(ref x);
      // MSDN docs recommend XOR'ing the internal values to get a hash code
      return x.m_x.GetHashCode() ^ x.m_y.GetHashCode() ^ x.m_z.GetHashCode() ^ x.m_w.GetHashCode();
    }

    /// <summary>
    /// Gets the value of a point with all coordinates set as RhinoMath.UnsetValue.
    /// </summary>
    /// <since>5.0</since>
    public static Point4d Unset
    {
      get { return new Point4d(RhinoMath.UnsetValue, RhinoMath.UnsetValue, RhinoMath.UnsetValue, RhinoMath.UnsetValue); }
    }

    /// <summary>
    /// Transforms the present point in place. The transformation matrix acts on the left of the point. i.e.,
    /// <para>result = transformation*point</para>
    /// </summary>
    /// <param name="xform">Transformation to apply.</param>
    /// <since>6.0</since>
    public void Transform(Transform xform)
    {
      double xx = xform.m_00 * m_x + xform.m_01 * m_y + xform.m_02 * m_z + xform.m_03 * m_w;
      double yy = xform.m_10 * m_x + xform.m_11 * m_y + xform.m_12 * m_z + xform.m_13 * m_w;
      double zz = xform.m_20 * m_x + xform.m_21 * m_y + xform.m_22 * m_z + xform.m_23 * m_w;
      double ww = xform.m_30 * m_x + xform.m_31 * m_y + xform.m_32 * m_z + xform.m_33 * m_w;
      m_x = xx;
      m_y = yy;
      m_z = zz;
      m_w = ww;
    }

    /// <summary>
    /// Returns an indication regarding the validity of this point.
    /// </summary>
    /// <since>6.0</since>
    public bool IsValid
    {
      get
      {
        return RhinoMath.IsValidDouble(m_x) && RhinoMath.IsValidDouble(m_y) && RhinoMath.IsValidDouble(m_y) && RhinoMath.IsValidDouble(m_w);
      }
    }

    /// <inheritdoc />
    [ConstOperation]
    public override string ToString()
    {
      return String.Format(CultureInfo.InvariantCulture, "{0},{1},{2},{3}", m_x, m_y, m_z, m_w);
    }
    /// <inheritdoc />
    /// <since>7.0</since>
    [ConstOperation]
    public string ToString(string format, IFormatProvider formatProvider)
    {
      return Point3d.FormatCoordinates(format, formatProvider, m_x, m_y, m_z, m_w);
    }
  }

  /// <summary>
  /// Represents the two components of a vector in two-dimensional space,
  /// using <see cref="double"/>-precision floating point numbers.
  /// </summary>
  [StructLayout(LayoutKind.Sequential, Pack = 8, Size = 16)]
  [DebuggerDisplay("({m_x}, {m_y})")]
  [Serializable]
  public struct Vector2d : ISerializable, IEquatable<Vector2d>, IComparable<Vector2d>, IComparable, IEpsilonComparable<Vector2d>, IValidable, IFormattable
  {
    private double m_x;
    private double m_y;

    /// <summary>
    /// Initializes a new instance of the vector based on two, X and Y, components.
    /// </summary>
    /// <param name="x">The X (first) component.</param>
    /// <param name="y">The Y (second) component.</param>
    /// <since>5.0</since>
    public Vector2d(double x, double y)
    {
      m_x = x;
      m_y = y;
    }

    private Vector2d(SerializationInfo info, StreamingContext context)
    {
      m_x = info.GetDouble("X");
      m_y = info.GetDouble("Y");
    }

    [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.SerializationFormatter)]
    void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
    {
      info.AddValue("X", m_x);
      info.AddValue("Y", m_y);
    }

    /// <summary>
    /// Gets or sets the X (first) component of this vector.
    /// </summary>
    /// <since>5.0</since>
    public double X { get { return m_x; } set { m_x = value; } }

    /// <summary>
    /// Gets or sets the Y (second) component of this vector.
    /// </summary>
    /// <since>5.0</since>
    public double Y { get { return m_y; } set { m_y = value; } }

    /// <summary>
    /// Computes the length (or magnitude, or size) of this vector.
    /// This is an application of Pythagoras' theorem.
    /// </summary>
    /// <since>5.0</since>
    public double Length
    {
      get { return UnsafeNativeMethods.ON_2dVector_Length(this); }
    }

    internal static double GetLengthHelper(double dx, double dy)
    {
      if (!RhinoMath.IsValidDouble(dx) ||
          !RhinoMath.IsValidDouble(dy))
        return 0.0;

      double len;
      double fx = Math.Abs(dx);
      double fy = Math.Abs(dy);
      if (fy >= fx)
      {
        len = fx; fx = fy; fy = len;
      }

      // 15 September 2003 Dale Lear
      //     For small denormalized doubles (positive but smaller
      //     than DBL_MIN), some compilers/FPUs set 1.0/fx to +INF.
      //     Without the ON_DBL_MIN test we end up with
      //     microscopic vectors that have infinite length!
      //
      //     Since this code starts with floats, none of this
      //     should be necessary, but it doesn't hurt anything.
      const double ON_DBL_MIN = 2.2250738585072014e-308;
      if (fx > ON_DBL_MIN)
      {
        len = 1.0 / fx;
        fy *= len;
        len = fx * Math.Sqrt(1.0 + fy * fy);
      }
      else if (fx > 0.0 && RhinoMath.IsValidDouble(fx))
        len = fx;
      else
        len = 0.0;
      return len;
    }

    #region operators
    /// <summary>
    /// Multiplies a vector by a number, having the effect of scaling it.
    /// </summary>
    /// <param name="vector">A vector.</param>
    /// <param name="t">A number.</param>
    /// <returns>A new vector that is the original vector coordinate-wise multiplied by t.</returns>
    /// <since>6.0</since>
    public static Vector2d operator *(Vector2d vector, double t)
    {
      return new Vector2d(vector.m_x * t, vector.m_y * t);
    }
    /// <summary>
    /// Multiplies a vector by a number, having the effect of scaling it.
    /// </summary>
    /// <param name="vector">A vector.</param>
    /// <param name="t">A number.</param>
    /// <returns>A new vector that is the original vector coordinate-wise multiplied by t.</returns>
    /// <since>6.0</since>
    public static Vector2d operator *(double t, Vector2d vector)
    {
      return new Vector2d(vector.m_x * t, vector.m_y * t);
    }

    /// <summary>
    /// Multiplies a vector by a number, having the effect of scaling it.
    /// <para>(Provided for languages that do not support operator overloading. You can use the * operator otherwise)</para>
    /// </summary>
    /// <param name="vector">A vector.</param>
    /// <param name="t">A number.</param>
    /// <returns>A new vector that is the original vector coordinate-wise multiplied by t.</returns>
    /// <since>6.0</since>
    public static Vector2d Multiply(Vector2d vector, double t)
    {
      return new Vector2d(vector.m_x * t, vector.m_y * t);
    }
    /// <summary>
    /// Multiplies a vector by a number, having the effect of scaling it.
    /// <para>(Provided for languages that do not support operator overloading. You can use the * operator otherwise)</para>
    /// </summary>
    /// <param name="vector">A vector.</param>
    /// <param name="t">A number.</param>
    /// <returns>A new vector that is the original vector coordinate-wise multiplied by t.</returns>
    /// <since>6.0</since>
    public static Vector2d Multiply(double t, Vector2d vector)
    {
      return new Vector2d(vector.m_x * t, vector.m_y * t);
    }

    /// <summary>
    /// Divides a <see cref="Vector2d"/> by a number, having the effect of shrinking it.
    /// </summary>
    /// <param name="vector">A vector.</param>
    /// <param name="t">A number.</param>
    /// <returns>A new vector that is component-wise divided by t.</returns>
    /// <since>6.0</since>
    public static Vector2d operator /(Vector2d vector, double t)
    {
      return new Vector2d(vector.m_x / t, vector.m_y / t);
    }

    /// <summary>
    /// Divides a <see cref="Vector2d"/> by a number, having the effect of shrinking it.
    /// <para>(Provided for languages that do not support operator overloading. You can use the / operator otherwise)</para>
    /// </summary>
    /// <param name="vector">A vector.</param>
    /// <param name="t">A number.</param>
    /// <returns>A new vector that is component-wise divided by t.</returns>
    /// <since>6.0</since>
    public static Vector2d Divide(Vector2d vector, double t)
    {
      return new Vector2d(vector.m_x / t, vector.m_y / t);
    }

    /// <summary>
    /// Sums up two vectors.
    /// </summary>
    /// <param name="vector1">A vector.</param>
    /// <param name="vector2">A second vector.</param>
    /// <returns>A new vector that results from the component-wise addition of the two vectors.</returns>
    public static Vector2d operator +(Vector2d vector1, Vector2d vector2)
    {
      return new Vector2d(vector1.m_x + vector2.m_x, vector1.m_y + vector2.m_y);
    }

    /// <summary>
    /// Sums up two vectors.
    /// <para>(Provided for languages that do not support operator overloading. You can use the + operator otherwise)</para>
    /// </summary>
    /// <param name="vector1">A vector.</param>
    /// <param name="vector2">A second vector.</param>
    /// <returns>A new vector that results from the component-wise addition of the two vectors.</returns>
    /// <since>6.0</since>
    public static Vector2d Add(Vector2d vector1, Vector2d vector2)
    {
      return new Vector2d(vector1.m_x + vector2.m_x, vector1.m_y + vector2.m_y);
    }

    /// <summary>
    /// Subtracts the second vector from the first one.
    /// </summary>
    /// <param name="vector1">A vector.</param>
    /// <param name="vector2">A second vector.</param>
    /// <returns>A new vector that results from the component-wise difference of vector1 - vector2.</returns>
    /// <since>6.0</since>
    public static Vector2d operator -(Vector2d vector1, Vector2d vector2)
    {
      return new Vector2d(vector1.m_x - vector2.m_x, vector1.m_y - vector2.m_y);
    }

    /// <summary>
    /// Subtracts the second vector from the first one.
    /// <para>(Provided for languages that do not support operator overloading. You can use the - operator otherwise)</para>
    /// </summary>
    /// <param name="vector1">A vector.</param>
    /// <param name="vector2">A second vector.</param>
    /// <returns>A new vector that results from the component-wise difference of vector1 - vector2.</returns>
    /// <since>6.0</since>
    public static Vector2d Subtract(Vector2d vector1, Vector2d vector2)
    {
      return new Vector2d(vector1.m_x - vector2.m_x, vector1.m_y - vector2.m_y);
    }

    /// <summary>
    /// Multiplies two vectors together, returning the dot product (or inner product).
    /// </summary>
    /// <param name="vector1">A vector.</param>
    /// <param name="vector2">A second vector.</param>
    /// <returns>
    /// A value that results from the evaluation of v1.X*v2.X + v1.Y*v2.Y.
    /// <para>This value equals v1.Length * v2.Length * cos(alpha), where alpha is the angle between vectors.</para>
    /// </returns>
    /// <since>6.0</since>
    public static double operator *(Vector2d vector1, Vector2d vector2)
    {
      return (vector1.m_x * vector2.m_x + vector1.m_y * vector2.m_y);
    }

    /// <summary>
    /// Multiplies two vectors together, returning the dot product (or inner product).
    /// <para>(Provided for languages that do not support operator overloading. You can use the * operator otherwise)</para>
    /// </summary>
    /// <param name="vector1">A vector.</param>
    /// <param name="vector2">A second vector.</param>
    /// <returns>
    /// A value that results from the evaluation of v1.X*v2.X + v1.Y*v2.Y.
    /// <para>This value equals v1.Length * v2.Length * cos(alpha), where alpha is the angle between vectors.</para>
    /// </returns>
    /// <since>6.0</since>
    public static double Multiply(Vector2d vector1, Vector2d vector2)
    {
      return (vector1.m_x * vector2.m_x + vector1.m_y * vector2.m_y);
    }

    /// <summary>
    /// Computes the opposite vector.
    /// </summary>
    /// <param name="vector">A vector to negate.</param>
    /// <returns>A new vector where all components were multiplied by -1.</returns>
    /// <since>6.0</since>
    public static Vector2d operator -(Vector2d vector)
    {
      return new Vector2d(-vector.m_x, -vector.m_y);
    }

    /// <summary>
    /// Computes the reversed vector.
    /// <para>(Provided for languages that do not support operator overloading. You can use the - unary operator otherwise)</para>
    /// </summary>
    /// <param name="vector">A vector to negate.</param>
    /// <returns>A new vector where all components were multiplied by -1.</returns>
    /// <since>6.0</since>
    public static Vector2d Negate(Vector2d vector)
    {
      return new Vector2d(-vector.m_x, -vector.m_y);
    }

    /// <summary>
    /// Determines whether two vectors have equal values.
    /// </summary>
    /// <param name="a">The first vector.</param>
    /// <param name="b">The second vector.</param>
    /// <returns>true if components of the two vectors are pairwise equal; otherwise false.</returns>
    /// <since>5.0</since>
    public static bool operator ==(Vector2d a, Vector2d b)
    {
      return a.m_x == b.m_x && a.m_y == b.m_y;
    }

    /// <summary>
    /// Determines whether two vectors have different values.
    /// </summary>
    /// <param name="a">The first vector.</param>
    /// <param name="b">The second vector.</param>
    /// <returns>true if any component of the two vectors is pairwise different; otherwise false.</returns>
    /// <since>5.0</since>
    public static bool operator !=(Vector2d a, Vector2d b)
    {
      return a.m_x != b.m_x || a.m_y != b.m_y;
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
    public static bool operator <(Vector2d a, Vector2d b)
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
    public static bool operator <=(Vector2d a, Vector2d b)
    {
      return (a.X < b.X) || (a.X == b.X && a.Y <= b.Y);
    }

    /// <summary>
    /// Determines whether the first specified vector comes after
    /// (has superior sorting value than) the second vector.
    /// <para>Components have decreasing evaluation priority: first X, then Y.</para>
    /// </summary>
    /// <param name="a">First vector.</param>
    /// <param name="b">Second vector.</param>
    /// <returns>true if a.X is larger than b.X, or a.X == b.X and a.Y is larger than b.Y; otherwise, false.</returns>
    /// <since>5.0</since>
    public static bool operator >(Vector2d a, Vector2d b)
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
    public static bool operator >=(Vector2d a, Vector2d b)
    {
      return (a.X > b.X) || (a.X == b.X && a.Y >= b.Y);
    }
    #endregion

    /// <summary>
    /// Determines whether the specified System.Object is a Vector2d and has the same value as the present vector.
    /// </summary>
    /// <param name="obj">The specified object.</param>
    /// <returns>true if obj is Vector2d and has the same components as this; otherwise false.</returns>
    [ConstOperation]
    public override bool Equals(object obj)
    {
      return (obj is Vector2d && this == (Vector2d)obj);
    }

    /// <summary>
    /// Determines whether the specified vector has the same value as the present vector.
    /// </summary>
    /// <param name="vector">The specified vector.</param>
    /// <returns>true if vector has the same components as this; otherwise false.</returns>
    /// <since>5.0</since>
    [ConstOperation]
    public bool Equals(Vector2d vector)
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
    public bool EpsilonEquals(Vector2d other, double epsilon)
    {
      return RhinoMath.EpsilonEquals(m_x, other.m_x, epsilon) &&
             RhinoMath.EpsilonEquals(m_y, other.m_y, epsilon);
    }

    /// <summary>
    /// Compares this <see cref="Vector2d" /> with another <see cref="Vector2d" />.
    /// <para>Components evaluation priority is first X, then Y.</para>
    /// </summary>
    /// <param name="other">The other <see cref="Vector2d" /> to use in comparison.</param>
    /// <returns>
    /// <para> 0: if this is identical to other</para>
    /// <para>-1: if this.X &lt; other.X</para>
    /// <para>-1: if this.X == other.X and this.Y &lt; other.Y</para>
    /// <para>+1: otherwise.</para>
    /// </returns>
    /// <since>5.0</since>
    [ConstOperation]
    public int CompareTo(Vector2d other)
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
      if (obj is Vector2d)
        return CompareTo((Vector2d)obj);

      throw new ArgumentException("Input must be of type Vector2d", "obj");
    }

    /// <summary>
    /// Provides a hashing value for the present vector.
    /// </summary>
    /// <returns>A non-unique number based on vector components.</returns>
    [ConstOperation]
    public override int GetHashCode()
    {
      // MSDN docs recommend XOR'ing the internal values to get a hash code
      return m_x.GetHashCode() ^ m_y.GetHashCode();
    }

    /// <summary>
    /// Constructs a string representation of the current vector.
    /// </summary>
    /// <returns>A string in the form X,Y.</returns>
    [ConstOperation]
    public override string ToString()
    {
      return String.Format(System.Globalization.CultureInfo.InvariantCulture, "{0},{1}", X, Y);
    }
    /// <inheritdoc />
    /// <since>7.0</since>
    [ConstOperation]
    public string ToString(string format, IFormatProvider formatProvider)
    {
      return Point3d.FormatCoordinates(format, formatProvider, m_x, m_y);
    }

    /// <summary>
    /// Gets the value of the vector with components 0,0.
    /// </summary>
    /// <since>5.0</since>
    public static Vector2d Zero
    {
      get { return new Vector2d(); }
    }

    /// <summary>
    /// Gets the value of the vector with components set as RhinoMath.UnsetValue,RhinoMath.UnsetValue.
    /// </summary>
    /// <since>5.0</since>
    public static Vector2d Unset
    {
      get { return new Vector2d(RhinoMath.UnsetValue, RhinoMath.UnsetValue); }
    }

    /// <summary>
    /// Gets a value indicating whether this vector is valid. 
    /// A valid vector must be formed of valid component values for x, y and z.
    /// </summary>
    /// <since>5.7</since>
    public bool IsValid
    {
      get
      {
        return RhinoMath.IsValidDouble(m_x) &&
               RhinoMath.IsValidDouble(m_y);
      }
    }

    /// <summary>
    /// Returns the square of the length of the vector.
    /// </summary>
    /// <since>6.0</since>
    public double SquareLength
    {
      get
      {
        return m_x * m_x + m_y * m_y;
      }
    }

    /// <summary>
    /// Determines whether a vector is very short.
    /// </summary>
    /// <param name="tolerance">
    /// A nonzero value used as the coordinate zero tolerance.
    /// .</param>
    /// <returns>(Math.Abs(X) &lt;= tiny_tol) AND (Math.Abs(Y) &lt;= tiny_tol) AND (Math.Abs(Z) &lt;= tiny_tol).</returns>
    /// <example>
    /// <code source='examples\vbnet\ex_addline.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_addline.cs' lang='cs'/>
    /// <code source='examples\py\ex_addline.py' lang='py'/>
    /// </example>
    /// <since>6.0</since>
    [ConstOperation]
    public bool IsTiny(double tolerance)
    {
      return UnsafeNativeMethods.ON_2dVector_IsTiny(this, tolerance);
    }

    /// <summary>
    /// Uses RhinoMath.ZeroTolerance for IsTiny calculation.
    /// </summary>
    /// <returns>true if vector is very small, otherwise false.</returns>
    /// <since>6.0</since>
    [ConstOperation]
    public bool IsTiny()
    {
      return IsTiny(RhinoMath.ZeroTolerance);
    }

    /// <summary>
    /// Unitizes the vector in place. A unit vector has length 1 unit. 
    /// <para>An invalid or zero length vector cannot be unitized.</para>
    /// </summary>
    /// <returns>true on success or false on failure.</returns>
    /// <since>5.7</since>
    public bool Unitize()
    {
      bool rc = IsValid && UnsafeNativeMethods.ON_2dVector_Unitize(ref this);
      return rc;
    }

    /// <summary>
    /// Rotates this vector.
    /// </summary>
    /// <param name="angleRadians">Angle of rotation (in radians).</param>
    /// <returns>true on success, false on failure.</returns>
    /// <since>6.0</since>
    public bool Rotate(double angleRadians)
    {
      if (RhinoMath.IsValidDouble(angleRadians))
      {
        UnsafeNativeMethods.ON_2dVector_Rotate(ref this, angleRadians);
        return true;
      }
      return false;
    }
  }

  /// <summary>
  /// Represents the three components of a vector in three-dimensional space,
  /// using <see cref="double"/>-precision floating point numbers.
  /// </summary>
  [StructLayout(LayoutKind.Sequential, Pack = 8, Size = 24)]
  [DebuggerDisplay("({m_x}, {m_y}, {m_z})")]
  [Serializable]
  public struct Vector3d
    : ISerializable, IEquatable<Vector3d>, IComparable<Vector3d>,
    IComparable, IEpsilonComparable<Vector3d>, ICloneable, IValidable, IFormattable
  {
    #region fields
    internal double m_x;
    internal double m_y;
    internal double m_z;
    #endregion

    #region constructors

    /// <summary>
    /// Initializes a new instance of a vector, using its three components.
    /// </summary>
    /// <param name="x">The X (first) component.</param>
    /// <param name="y">The Y (second) component.</param>
    /// <param name="z">The Z (third) component.</param>
    /// <since>5.0</since>
    public Vector3d(double x, double y, double z)
    {
      m_x = x;
      m_y = y;
      m_z = z;
    }

    /// <summary>
    /// Initializes a new instance of a vector, copying the three components from the three coordinates of a point.
    /// </summary>
    /// <param name="point">The point to copy from.</param>
    /// <since>5.0</since>
    public Vector3d(Point3d point)
    {
      m_x = point.m_x;
      m_y = point.m_y;
      m_z = point.m_z;
    }

    /// <summary>
    /// Initializes a new instance of a vector, copying the three components from a single-precision vector.
    /// </summary>
    /// <param name="vector">A single-precision vector.</param>
    /// <since>5.0</since>
    public Vector3d(Vector3f vector)
    {
      m_x = vector.m_x;
      m_y = vector.m_y;
      m_z = vector.m_z;
    }

    /// <summary>
    /// Initializes a new instance of a vector, copying the three components from a vector.
    /// </summary>
    /// <param name="vector">A double-precision vector.</param>
    /// <since>5.0</since>
    public Vector3d(Vector3d vector)
    {
      m_x = vector.m_x;
      m_y = vector.m_y;
      m_z = vector.m_z;
    }

    /// <summary>
    /// Gets the value of the vector with components 0,0,0.
    /// </summary>
    /// <since>5.0</since>
    public static Vector3d Zero
    {
      get { return new Vector3d(); }
    }

    /// <summary>
    /// Gets the value of the vector with components 1,0,0.
    /// </summary>
    /// <since>5.0</since>
    public static Vector3d XAxis
    {
      get { return new Vector3d(1.0, 0.0, 0.0); }
    }

    /// <summary>
    /// Gets the value of the vector with components 0,1,0.
    /// </summary>
    /// <since>5.0</since>
    public static Vector3d YAxis
    {
      get { return new Vector3d(0.0, 1.0, 0.0); }
    }

    /// <summary>
    /// Gets the value of the vector with components 0,0,1.
    /// </summary>
    /// <since>5.0</since>
    public static Vector3d ZAxis
    {
      get { return new Vector3d(0.0, 0.0, 1.0); }
    }

    /// <summary>
    /// Gets the value of the vector with each component set to RhinoMath.UnsetValue.
    /// </summary>
    /// <since>5.0</since>
    public static Vector3d Unset
    {
      get { return new Vector3d(RhinoMath.UnsetValue, RhinoMath.UnsetValue, RhinoMath.UnsetValue); }
    }

    private Vector3d(SerializationInfo info, StreamingContext context)
    {
      m_x = info.GetDouble("X");
      m_y = info.GetDouble("Y");
      m_z = info.GetDouble("Z");
    }

    [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.SerializationFormatter)]
    void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
    {
      info.AddValue("X", m_x);
      info.AddValue("Y", m_y);
      info.AddValue("Z", m_z);
    }

    #endregion

    #region operators

    /// <summary>
    /// Multiplies a vector by a number, having the effect of scaling it.
    /// </summary>
    /// <param name="vector">A vector.</param>
    /// <param name="t">A number.</param>
    /// <returns>A new vector that is the original vector coordinate-wise multiplied by t.</returns>
    /// <since>5.0</since>
    public static Vector3d operator *(Vector3d vector, double t)
    {
      return new Vector3d(vector.m_x * t, vector.m_y * t, vector.m_z * t);
    }

    /// <summary>
    /// Multiplies a vector by a number, having the effect of scaling it.
    /// <para>(Provided for languages that do not support operator overloading. You can use the * operator otherwise)</para>
    /// </summary>
    /// <param name="vector">A vector.</param>
    /// <param name="t">A number.</param>
    /// <returns>A new vector that is the original vector coordinate-wise multiplied by t.</returns>
    /// <since>5.0</since>
    public static Vector3d Multiply(Vector3d vector, double t)
    {
      return new Vector3d(vector.m_x * t, vector.m_y * t, vector.m_z * t);
    }

    /// <summary>
    /// Multiplies a vector by a number, having the effect of scaling it.
    /// </summary>
    /// <param name="t">A number.</param>
    /// <param name="vector">A vector.</param>
    /// <returns>A new vector that is the original vector coordinate-wise multiplied by t.</returns>
    /// <since>5.0</since>
    public static Vector3d operator *(double t, Vector3d vector)
    {
      return new Vector3d(vector.m_x * t, vector.m_y * t, vector.m_z * t);
    }

    /// <summary>
    /// Multiplies a vector by a number, having the effect of scaling it.
    /// <para>(Provided for languages that do not support operator overloading. You can use the * operator otherwise)</para>
    /// </summary>
    /// <param name="t">A number.</param>
    /// <param name="vector">A vector.</param>
    /// <returns>A new vector that is the original vector coordinate-wise multiplied by t.</returns>
    /// <since>5.0</since>
    public static Vector3d Multiply(double t, Vector3d vector)
    {
      return new Vector3d(vector.m_x * t, vector.m_y * t, vector.m_z * t);
    }

    /// <summary>
    /// Divides a <see cref="Vector3d"/> by a number, having the effect of shrinking it.
    /// </summary>
    /// <param name="vector">A vector.</param>
    /// <param name="t">A number.</param>
    /// <returns>A new vector that is component-wise divided by t.</returns>
    /// <since>5.0</since>
    public static Vector3d operator /(Vector3d vector, double t)
    {
      return new Vector3d(vector.m_x / t, vector.m_y / t, vector.m_z / t);
    }

    /// <summary>
    /// Divides a <see cref="Vector3d"/> by a number, having the effect of shrinking it.
    /// <para>(Provided for languages that do not support operator overloading. You can use the / operator otherwise)</para>
    /// </summary>
    /// <param name="vector">A vector.</param>
    /// <param name="t">A number.</param>
    /// <returns>A new vector that is component-wise divided by t.</returns>
    /// <since>5.0</since>
    public static Vector3d Divide(Vector3d vector, double t)
    {
      return new Vector3d(vector.m_x / t, vector.m_y / t, vector.m_z / t);
    }

    /// <summary>
    /// Sums up two vectors.
    /// </summary>
    /// <param name="vector1">A vector.</param>
    /// <param name="vector2">A second vector.</param>
    /// <returns>A new vector that results from the component-wise addition of the two vectors.</returns>
    public static Vector3d operator +(Vector3d vector1, Vector3d vector2)
    {
      return new Vector3d(vector1.m_x + vector2.m_x, vector1.m_y + vector2.m_y, vector1.m_z + vector2.m_z);
    }

    /// <summary>
    /// Sums up two vectors.
    /// <para>(Provided for languages that do not support operator overloading. You can use the + operator otherwise)</para>
    /// </summary>
    /// <param name="vector1">A vector.</param>
    /// <param name="vector2">A second vector.</param>
    /// <returns>A new vector that results from the component-wise addition of the two vectors.</returns>
    /// <since>5.0</since>
    public static Vector3d Add(Vector3d vector1, Vector3d vector2)
    {
      return new Vector3d(vector1.m_x + vector2.m_x, vector1.m_y + vector2.m_y, vector1.m_z + vector2.m_z);
    }

    /// <summary>
    /// Subtracts the second vector from the first one.
    /// </summary>
    /// <param name="vector1">A vector.</param>
    /// <param name="vector2">A second vector.</param>
    /// <returns>A new vector that results from the component-wise difference of vector1 - vector2.</returns>
    /// <since>5.0</since>
    public static Vector3d operator -(Vector3d vector1, Vector3d vector2)
    {
      return new Vector3d(vector1.m_x - vector2.m_x, vector1.m_y - vector2.m_y, vector1.m_z - vector2.m_z);
    }

    /// <summary>
    /// Subtracts the second vector from the first one.
    /// <para>(Provided for languages that do not support operator overloading. You can use the - operator otherwise)</para>
    /// </summary>
    /// <param name="vector1">A vector.</param>
    /// <param name="vector2">A second vector.</param>
    /// <returns>A new vector that results from the component-wise difference of vector1 - vector2.</returns>
    /// <since>5.0</since>
    public static Vector3d Subtract(Vector3d vector1, Vector3d vector2)
    {
      return new Vector3d(vector1.m_x - vector2.m_x, vector1.m_y - vector2.m_y, vector1.m_z - vector2.m_z);
    }

    /// <summary>
    /// Multiplies two vectors together, returning the dot product (or inner product).
    /// This differs from the cross product.
    /// </summary>
    /// <param name="vector1">A vector.</param>
    /// <param name="vector2">A second vector.</param>
    /// <returns>
    /// A value that results from the evaluation of v1.X*v2.X + v1.Y*v2.Y + v1.Z*v2.Z.
    /// <para>This value equals v1.Length * v2.Length * cos(alpha), where alpha is the angle between vectors.</para>
    /// </returns>
    /// <since>5.0</since>
    public static double operator *(Vector3d vector1, Vector3d vector2)
    {
      return (vector1.m_x * vector2.m_x + vector1.m_y * vector2.m_y + vector1.m_z * vector2.m_z);
    }

    /// <summary>
    /// Multiplies two vectors together, returning the dot product (or inner product).
    /// This differs from the cross product.
    /// <para>(Provided for languages that do not support operator overloading. You can use the * operator otherwise)</para>
    /// </summary>
    /// <param name="vector1">A vector.</param>
    /// <param name="vector2">A second vector.</param>
    /// <returns>
    /// A value that results from the evaluation of v1.X*v2.X + v1.Y*v2.Y + v1.Z*v2.Z.
    /// <para>This value equals v1.Length * v2.Length * cos(alpha), where alpha is the angle between vectors.</para>
    /// </returns>
    /// <since>5.0</since>
    public static double Multiply(Vector3d vector1, Vector3d vector2)
    {
      return (vector1.m_x * vector2.m_x + vector1.m_y * vector2.m_y + vector1.m_z * vector2.m_z);
    }

    /// <summary>
    /// Computes the opposite vector.
    /// </summary>
    /// <param name="vector">A vector to negate.</param>
    /// <returns>A new vector where all components were multiplied by -1.</returns>
    /// <since>5.0</since>
    public static Vector3d operator -(Vector3d vector)
    {
      return new Vector3d(-vector.m_x, -vector.m_y, -vector.m_z);
    }

    /// <summary>
    /// Computes the reversed vector.
    /// <para>(Provided for languages that do not support operator overloading. You can use the - unary operator otherwise)</para>
    /// </summary>
    /// <remarks>Similar to <see cref="Reverse">Reverse()</see>, but static for CLR compliance.</remarks>
    /// <param name="vector">A vector to negate.</param>
    /// <returns>A new vector where all components were multiplied by -1.</returns>
    /// <since>5.0</since>
    public static Vector3d Negate(Vector3d vector)
    {
      return new Vector3d(-vector.m_x, -vector.m_y, -vector.m_z);
    }

    /// <summary>
    /// Determines whether two vectors have the same value.
    /// </summary>
    /// <param name="a">A vector.</param>
    /// <param name="b">Another vector.</param>
    /// <returns>true if all coordinates are pairwise equal; false otherwise.</returns>
    /// <since>5.0</since>
    public static bool operator ==(Vector3d a, Vector3d b)
    {
      return a.m_x == b.m_x && a.m_y == b.m_y && a.m_z == b.m_z;
    }

    /// <summary>
    /// Determines whether two vectors have different values.
    /// </summary>
    /// <param name="a">A vector.</param>
    /// <param name="b">Another vector.</param>
    /// <returns>true if any coordinate pair is different; false otherwise.</returns>
    /// <since>5.0</since>
    public static bool operator !=(Vector3d a, Vector3d b)
    {
      return a.m_x != b.m_x || a.m_y != b.m_y || a.m_z != b.m_z;
    }

    /// <summary>
    /// Computes the cross product (or vector product, or exterior product) of two vectors.
    /// <para>This operation is not commutative.</para>
    /// </summary>
    /// <param name="a">First vector.</param>
    /// <param name="b">Second vector.</param>
    /// <returns>A new vector that is perpendicular to both a and b,
    /// <para>has Length == a.Length * b.Length * sin(theta) where theta is the angle between a and b.</para>
    /// <para>The resulting vector is oriented according to the right hand rule.</para>
    /// </returns>
    /// <since>5.0</since>
    public static Vector3d CrossProduct(Vector3d a, Vector3d b)
    {
      return new Vector3d(a.m_y * b.m_z - b.m_y * a.m_z, a.m_z * b.m_x - b.m_z * a.m_x, a.m_x * b.m_y - b.m_x * a.m_y);
    }

    /// <summary>
    /// Compute the angle between two vectors.
    /// <para>This operation is commutative.</para>
    /// </summary>
    /// <param name="a">First vector for angle.</param>
    /// <param name="b">Second vector for angle.</param>
    /// <returns>If the input is valid, the angle (in radians) between a and b; RhinoMath.UnsetValue otherwise.</returns>
    /// <since>5.0</since>
    public static double VectorAngle(Vector3d a, Vector3d b)
    {
      if (!a.Unitize() || !b.Unitize())
        return RhinoMath.UnsetValue;

      //compute dot product
      double dot = a.m_x * b.m_x + a.m_y * b.m_y + a.m_z * b.m_z;
      // remove any "noise"
      if (dot > 1.0) dot = 1.0;
      if (dot < -1.0) dot = -1.0;
      double radians = Math.Acos(dot);
      return radians;
    }

    /// <summary>
    /// Computes the angle on a plane between two vectors.
    /// </summary>
    /// <param name="a">First vector.</param>
    /// <param name="b">Second vector.</param>
    /// <param name="plane">Two-dimensional plane on which to perform the angle measurement.</param>
    /// <returns>On success, the angle (in radians) between a and b as projected onto the plane; RhinoMath.UnsetValue on failure.</returns>
    /// <since>5.0</since>
    public static double VectorAngle(Vector3d a, Vector3d b, Plane plane)
    {
      { // Project vectors onto plane.
        Point3d pA = plane.Origin + a;
        Point3d pB = plane.Origin + b;

        pA = plane.ClosestPoint(pA);
        pB = plane.ClosestPoint(pB);

        a = pA - plane.Origin;
        b = pB - plane.Origin;
      }

      // Abort on invalid cases.
      if (!a.Unitize()) { return RhinoMath.UnsetValue; }
      if (!b.Unitize()) { return RhinoMath.UnsetValue; }

      double dot = a * b;
      { // Limit dot product to valid range.
        if (dot >= 1.0)
        { dot = 1.0; }
        else if (dot < -1.0)
        { dot = -1.0; }
      }

      double angle = Math.Acos(dot);
      { // Special case (anti)parallel vectors.
        if (Math.Abs(angle) < 1e-64) { return 0.0; }
        if (Math.Abs(angle - Math.PI) < 1e-64) { return Math.PI; }
      }

      Vector3d cross = Vector3d.CrossProduct(a, b);
      if (plane.ZAxis.IsParallelTo(cross) == +1)
        return angle;
      return 2.0 * Math.PI - angle;
    }

    /// <summary>
    /// Computes the angle of v1, v2 with a normal vector.
    /// </summary>
    /// <param name="v1">First vector.</param>
    /// <param name="v2">Second vector.</param>
    /// <param name="vNormal">Normal vector.</param>
    /// <returns>On success, the angle (in radians) between a and b with respect of normal vector; RhinoMath.UnsetValue on failure.</returns>
    /// <since>6.0</since>
    public static double VectorAngle(Vector3d v1, Vector3d v2, Vector3d vNormal)
    {
      if ((Math.Abs(v1.X - v2.X) < 1e-64) && (Math.Abs(v1.Y - v2.Y) < 1e-64) && (Math.Abs(v1.Z - v2.Z) < 1e-64))
        return 0.0;

      double dNumerator = v1 * v2;
      double dDenominator = v1.Length * v2.Length;

      Vector3d vCross = Vector3d.CrossProduct(v1, v2);
      vCross.Unitize();

      if ((Math.Abs(vCross.X - 0.0) < 1e-64) && (Math.Abs(vCross.Y - 0.0) < 1e-64) && (Math.Abs(vCross.Z - 0.0) < 1e-64))
      {
        if ((Math.Abs(dNumerator - 1.0) < 1e-64))
          return 0.0;
        else
        if ((Math.Abs(dNumerator + 1.0) < 1e-64))
          return Math.PI;
      }

      double dDivision = dNumerator / dDenominator;

      if (dDivision > 1.0)
        dDivision = 1.0;
      else
      if (dDivision < -1.0)
        dDivision = -1.0;

      if ((Math.Abs(dDivision + 1.0) < 1e-64))
        return Math.PI;

      double dAngle = Math.Acos(dDivision);

      // Check if vCross is parallel or anti parallel to normal vector.
      // If anti parallel Angle = 360 - Angle

      vNormal.Unitize();

      double dDot = vCross * vNormal;

      if ((Math.Abs(dDot + 1.0) < 1e-64))
        dAngle = (Math.PI * 2.0) - dAngle;

      return dAngle;
    }

    /// <summary>
    /// Converts a single-precision (float) vector in a double-precision vector, without needing casting.
    /// </summary>
    /// <param name="vector">A single-precision vector.</param>
    /// <returns>The same vector, expressed using double-precision values.</returns>
    public static implicit operator Vector3d(Vector3f vector)
    {
      return new Vector3d(vector);
    }

    /// <summary>
    /// Determines whether the first specified vector comes before (has inferior sorting value than) the second vector.
    /// <para>Components evaluation priority is first X, then Y, then Z.</para>
    /// </summary>
    /// <param name="a">The first vector.</param>
    /// <param name="b">The second vector.</param>
    /// <returns>true if a.X is smaller than b.X,
    /// or a.X == b.X and a.Y is smaller than b.Y,
    /// or a.X == b.X and a.Y == b.Y and a.Z is smaller than b.Z;
    /// otherwise, false.</returns>
    /// <since>5.0</since>
    public static bool operator <(Vector3d a, Vector3d b)
    {
      if (a.X < b.X)
        return true;
      if (a.X == b.X)
      {
        if (a.Y < b.Y)
          return true;
        if (a.Y == b.Y && a.Z < b.Z)
          return true;
      }
      return false;
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
    public static bool operator <=(Vector3d a, Vector3d b)
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
    public static bool operator >(Vector3d a, Vector3d b)
    {
      if (a.X > b.X)
        return true;
      if (a.X == b.X)
      {
        if (a.Y > b.Y)
          return true;
        if (a.Y == b.Y && a.Z > b.Z)
          return true;
      }
      return false;
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
    public static bool operator >=(Vector3d a, Vector3d b)
    {
      return a.CompareTo(b) >= 0;
    }

    #endregion

    #region static methods
    /// <summary>
    /// Test whether three vectors describe an orthogonal axis system.
    /// All vectors must be mutually perpendicular this to be the case.
    /// </summary>
    /// <param name="x">X axis vector.</param>
    /// <param name="y">Y axis vector.</param>
    /// <param name="z">Z axis vector.</param>
    /// <returns>True if all vectors are non-zero and mutually perpendicular.</returns>
    /// <since>6.7</since>
    public static bool AreOrthogonal(Vector3d x, Vector3d y, Vector3d z)
    {
      return UnsafeNativeMethods.ON_Plane_IsOrthogonalFrame(x, y, z);
    }
    /// <summary>
    /// Test whether three vectors describe an orthogonal, unit axis system.
    /// All vectors must be mutually perpendicular and have unit length for this to be the case.
    /// </summary>
    /// <param name="x">X axis vector.</param>
    /// <param name="y">Y axis vector.</param>
    /// <param name="z">Z axis vector.</param>
    /// <returns>True if all vectors are non-zero and mutually perpendicular.</returns>
    /// <since>6.7</since>
    public static bool AreOrthonormal(Vector3d x, Vector3d y, Vector3d z)
    {
      return UnsafeNativeMethods.ON_Plane_IsOrthonormalFrame(x, y, z);
    }
    /// <summary>
    /// Test whether three vectors describe a right-handed, orthogonal, unit axis system.
    /// The vectors must be orthonormal and follow the right-hand ordering; index-finger=x,
    /// middle-finger=y, thumb=z.
    /// </summary>
    /// <param name="x">X axis vector.</param>
    /// <param name="y">Y axis vector.</param>
    /// <param name="z">Z axis vector.</param>
    /// <returns>True if all vectors are non-zero and mutually perpendicular.</returns>
    /// <since>6.7</since>
    public static bool AreRighthanded(Vector3d x, Vector3d y, Vector3d z)
    {
      return UnsafeNativeMethods.ON_Plane_IsRightHandFrame(x, y, z);
    }
    #endregion

    #region properties
    /// <summary>
    /// Gets or sets the X (first) component of the vector.
    /// </summary>
    /// <since>5.0</since>
    public double X { get { return m_x; } set { m_x = value; } }
    /// <summary>
    /// Gets or sets the Y (second) component of the vector.
    /// </summary>
    /// <since>5.0</since>
    public double Y { get { return m_y; } set { m_y = value; } }
    /// <summary>
    /// Gets or sets the Z (third) component of the vector.
    /// </summary>
    /// <since>5.0</since>
    public double Z { get { return m_z; } set { m_z = value; } }

    /// <summary>
    /// Gets or sets a vector component at the given index.
    /// </summary>
    /// <param name="index">Index of vector component. Valid values are: 
    /// <para>0 = X-component</para>
    /// <para>1 = Y-component</para>
    /// <para>2 = Z-component</para>
    /// .</param>
    public double this[int index]
    {
      get
      {
        if (0 == index)
          return m_x;
        if (1 == index)
          return m_y;
        if (2 == index)
          return m_z;
        // IronPython works with indexing when we thrown an IndexOutOfRangeException
        throw new IndexOutOfRangeException();
      }
      set
      {
        if (0 == index)
          m_x = value;
        else if (1 == index)
          m_y = value;
        else if (2 == index)
          m_z = value;
        else
          throw new IndexOutOfRangeException();
      }
    }

    /// <summary>
    /// Gets a value indicating whether this vector is valid. 
    /// A valid vector must be formed of valid component values for x, y and z.
    /// </summary>
    /// <since>5.0</since>
    public bool IsValid
    {
      get
      {
        return RhinoMath.IsValidDouble(m_x) &&
               RhinoMath.IsValidDouble(m_y) &&
               RhinoMath.IsValidDouble(m_z);
      }
    }

    /// <summary>
    /// Gets the smallest (both positive and negative) component value in this vector.
    /// </summary>
    /// <since>5.0</since>
    public double MinimumCoordinate
    {
      get
      {
        Point3d p = new Point3d(this);
        return p.MinimumCoordinate;
      }
    }

    /// <summary>
    /// Gets the largest (both positive and negative) component value in this vector,  as an absolute value.
    /// </summary>
    /// <since>5.0</since>
    public double MaximumCoordinate
    {
      get
      {
        Point3d p = new Point3d(this);
        return p.MaximumCoordinate;
      }
    }

    /// <summary>
    /// Computes the length (or magnitude, or size) of this vector.
    /// This is an application of Pythagoras' theorem.
    /// If this vector is invalid, its length is considered 0.
    /// </summary>
    /// <since>5.0</since>
    public double Length
    {
      get { return GetLengthHelper(m_x, m_y, m_z); }
    }

    /// <summary>
    /// Computes the squared length (or magnitude, or size) of this vector.
    /// This is an application of Pythagoras' theorem.
    /// While the Length property checks for input validity,
    /// this property does not. You should check validity in advance,
    /// if this vector can be invalid.
    /// </summary>
    /// <since>5.0</since>
    public double SquareLength
    {
      get { return (m_x * m_x) + (m_y * m_y) + (m_z * m_z); }
    }
    /// <summary>
    /// Gets a value indicating whether or not this is a unit vector. 
    /// A unit vector has length 1.
    /// </summary>
    /// <since>5.0</since>
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
    /// Determines whether a vector is very short.
    /// </summary>
    /// <param name="tolerance">
    /// A nonzero value used as the coordinate zero tolerance.
    /// .</param>
    /// <returns>(Math.Abs(X) &lt;= tiny_tol) AND (Math.Abs(Y) &lt;= tiny_tol) AND (Math.Abs(Z) &lt;= tiny_tol).</returns>
    /// <example>
    /// <code source='examples\vbnet\ex_addline.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_addline.cs' lang='cs'/>
    /// <code source='examples\py\ex_addline.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    public bool IsTiny(double tolerance)
    {
      return UnsafeNativeMethods.ON_3dVector_IsTiny(this, tolerance);
    }

    /// <summary>
    /// Uses RhinoMath.ZeroTolerance for IsTiny calculation.
    /// </summary>
    /// <returns>true if vector is very small, otherwise false.</returns>
    /// <since>5.0</since>
    public bool IsTiny()
    {
      return IsTiny(RhinoMath.ZeroTolerance);
    }


    /// <summary>
    /// Gets a value indicating whether the X, Y, and Z values are all equal to 0.0.
    /// </summary>
    /// <since>5.0</since>
    public bool IsZero
    {
      get
      {
        return (m_x == 0.0 && m_y == 0.0 && m_z == 0.0);
      }
    }
    #endregion

    #region methods
    /// <summary>
    /// Determines whether the specified System.Object is a Vector3d and has the same values as the present vector.
    /// </summary>
    /// <param name="obj">The specified object.</param>
    /// <returns>true if obj is a Vector3d and has the same coordinates as this; otherwise false.</returns>
    [ConstOperation]
    public override bool Equals(object obj)
    {
      return (obj is Vector3d && this == (Vector3d)obj);
    }

    /// <summary>
    /// Determines whether the specified vector has the same value as the present vector.
    /// </summary>
    /// <param name="vector">The specified vector.</param>
    /// <returns>true if vector has the same coordinates as this; otherwise false.</returns>
    /// <since>5.0</since>
    [ConstOperation]
    public bool Equals(Vector3d vector)
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
    public bool EpsilonEquals(Vector3d other, double epsilon)
    {
      return RhinoMath.EpsilonEquals(m_x, other.m_x, epsilon) &&
             RhinoMath.EpsilonEquals(m_y, other.m_y, epsilon) &&
             RhinoMath.EpsilonEquals(m_z, other.m_z, epsilon);
    }

    /// <summary>
    /// Compares this <see cref="Vector3d" /> with another <see cref="Vector3d" />.
    /// <para>Component evaluation priority is first X, then Y, then Z.</para>
    /// </summary>
    /// <param name="other">The other <see cref="Vector3d" /> to use in comparison.</param>
    /// <returns>
    /// <para> 0: if this is identical to other</para>
    /// <para>-1: if this.X &lt; other.X</para>
    /// <para>-1: if this.X == other.X and this.Y &lt; other.Y</para>
    /// <para>-1: if this.X == other.X and this.Y == other.Y and this.Z &lt; other.Z</para>
    /// <para>+1: otherwise.</para>
    /// </returns>
    /// <since>5.0</since>
    [ConstOperation]
    public int CompareTo(Vector3d other)
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
      if (obj is Vector3d)
        return CompareTo((Vector3d)obj);

      throw new ArgumentException("Input must be of type Vector3d", "obj");
    }

    /// <summary>
    /// Computes the hash code for the current vector.
    /// </summary>
    /// <returns>A non-unique number that represents the components of this vector.</returns>
    [ConstOperation]
    public override int GetHashCode()
    {
      // MSDN docs recommend XOR'ing the internal values to get a hash code
      return m_x.GetHashCode() ^ m_y.GetHashCode() ^ m_z.GetHashCode();
    }

    /// <summary>
    /// Returns the string representation of the current vector, in the form X,Y,Z.
    /// </summary>
    /// <returns>A string with the current location of the point.</returns>
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
      bool rc = IsValid && UnsafeNativeMethods.ON_3dVector_Unitize(ref this);
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

      m_x = xx;
      m_y = yy;
      m_z = zz;
    }

    /// <summary>
    /// Rotates this vector around a given axis.
    /// </summary>
    /// <param name="angleRadians">Angle of rotation (in radians).</param>
    /// <param name="rotationAxis">Axis of rotation.</param>
    /// <returns>true on success, false on failure.</returns>
    /// <since>5.0</since>
    public bool Rotate(double angleRadians, Vector3d rotationAxis)
    {
      if (RhinoMath.IsValidDouble(angleRadians) && rotationAxis.IsValid)
      {
        UnsafeNativeMethods.ON_3dVector_Rotate(ref this, angleRadians, rotationAxis);
        return true;
      }
      return false;
    }

    ///<summary>
    /// Reverses this vector in place (reverses the direction).
    /// <para>If this vector is Invalid, no changes will occur and false will be returned.</para>
    ///</summary>
    ///<remarks>Similar to <see cref="Negate">Negate</see>, that is only provided for CLR language compliance.</remarks>
    ///<returns>true on success or false if the vector is invalid.</returns>
    /// <since>5.0</since>
    public bool Reverse()
    {
      if (!IsValid)
        return false;
      m_x = -m_x;
      m_y = -m_y;
      m_z = -m_z;
      return true;
    }

    /// <summary>
    /// Determines whether this vector is parallel to another vector, within one degree (within Pi / 180). 
    /// </summary>
    /// <param name="other">Vector to use for comparison.</param>
    /// <returns>
    /// Parallel indicator:
    /// <para>+1 = both vectors are parallel</para>
    /// <para> 0 = vectors are not parallel, or at least one of the vectors is zero</para>
    /// <para>-1 = vectors are anti-parallel.</para>
    /// </returns>
    /// <example>
    /// <code source='examples\vbnet\ex_intersectlines.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_intersectlines.cs' lang='cs'/>
    /// <code source='examples\py\ex_intersectlines.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    [ConstOperation]
    public int IsParallelTo(Vector3d other)
    {
      return IsParallelTo(other, RhinoMath.DefaultAngleTolerance);
    }

    /// <summary>
    /// Determines whether this vector is parallel to another vector, within a provided tolerance. 
    /// </summary>
    /// <param name="other">Vector to use for comparison.</param>
    /// <param name="angleTolerance">Angle tolerance (in radians).</param>
    /// <returns>
    /// Parallel indicator:
    /// <para>+1 = both vectors are parallel.</para>
    /// <para>0 = vectors are not parallel or at least one of the vectors is zero.</para>
    /// <para>-1 = vectors are anti-parallel.</para>
    /// </returns>
    /// <since>5.0</since>
    [ConstOperation]
    public int IsParallelTo(Vector3d other, double angleTolerance)
    {
      int rc = UnsafeNativeMethods.ON_3dVector_IsParallelTo(this, other, angleTolerance);
      return rc;
    }

    ///<summary>
    /// Test to see whether this vector is perpendicular to within one degree of another one. 
    ///</summary>
    /// <param name="other">Vector to compare to.</param>
    ///<returns>true if both vectors are perpendicular, false if otherwise.</returns>
    /// <since>5.0</since>
    [ConstOperation]
    public bool IsPerpendicularTo(Vector3d other)
    {
      return IsPerpendicularTo(other, RhinoMath.DefaultAngleTolerance);
    }

    ///<summary>
    /// Determines whether this vector is perpendicular to another vector, within a provided angle tolerance. 
    ///</summary>
    /// <param name="other">Vector to use for comparison.</param>
    /// <param name="angleTolerance">Angle tolerance (in radians).</param>
    ///<returns>true if vectors form Pi-radians (90-degree) angles with each other; otherwise false.</returns>
    /// <since>5.0</since>
    [ConstOperation]
    public bool IsPerpendicularTo(Vector3d other, double angleTolerance)
    {
      bool rc = false;
      double ll = Length * other.Length;
      if (ll > 0.0)
      {
        if (Math.Abs((m_x * other.m_x + m_y * other.m_y + m_z * other.m_z) / ll) <= Math.Sin(angleTolerance))
          rc = true;
      }
      return rc;
    }

    ///<summary>
    /// Sets this vector to be perpendicular to another vector. 
    /// Result is not unitized.
    ///</summary>
    /// <param name="other">Vector to use as guide.</param>
    ///<returns>true on success, false if input vector is zero or invalid.</returns>
    /// <since>5.0</since>
    public bool PerpendicularTo(Vector3d other)
    {
      return UnsafeNativeMethods.ON_3dVector_PerpendicularTo(ref this, other);
    }

    /// <summary>
    /// Set this vector to be perpendicular to a plane defined by 3 points.
    /// </summary>
    /// <param name="point0">The first point.</param>
    /// <param name="point1">The second point.</param>
    /// <param name="point2">The third point.</param>
    /// <returns></returns>
    /// <since>8.0</since>
    public bool PerpendicularTo(Point3d point0, Point3d point1, Point3d point2)
    {
      return UnsafeNativeMethods.ON_3dVector_PerpendicularTo2(ref this, point0, point1, point2);
    }

    internal static double GetLengthHelper(double dx, double dy, double dz)
    {
      if (!RhinoMath.IsValidDouble(dx) ||
          !RhinoMath.IsValidDouble(dy) ||
          !RhinoMath.IsValidDouble(dz))
        return 0.0;

      double len;
      double fx = Math.Abs(dx);
      double fy = Math.Abs(dy);
      double fz = Math.Abs(dz);
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
      //     Since this code starts with floats, none of this
      //     should be necessary, but it doesn't hurt anything.
      const double ON_DBL_MIN = 2.2250738585072014e-308;
      if (fx > ON_DBL_MIN)
      {
        len = 1.0 / fx;
        fy *= len;
        fz *= len;
        len = fx * Math.Sqrt(1.0 + fy * fy + fz * fz);
      }
      else if (fx > 0.0 && RhinoMath.IsValidDouble(fx))
        len = fx;
      else
        len = 0.0;
      return len;
    }

    object ICloneable.Clone()
    {
      return this;
    }

    #endregion
  }

  /// <summary>
  /// Represents an immutable ray in three dimensions, using position and direction.
  /// </summary>
  [StructLayout(LayoutKind.Sequential, Pack = 8, Size = 48)]
  [DebuggerDisplay("Pt({m_P.X},{m_P.Y},{m_P.Z}) Dir({m_V.X},{m_V.Y},{m_V.Z})")]
  [Serializable]
  public struct Ray3d : ISerializable, IEquatable<Ray3d>, IEpsilonComparable<Ray3d>
  {
    readonly Point3d m_P;
    readonly Vector3d m_V;

    /// <summary>
    /// Initializes a new Ray3d instance.
    /// </summary>
    /// <param name="position">The position.</param>
    /// <param name="direction">The direction.</param>
    /// <since>5.0</since>
    public Ray3d(Point3d position, Vector3d direction)
    {
      m_P = position;
      m_V = direction;
    }

    private Ray3d(SerializationInfo info, StreamingContext context)
    {
      m_P = (Point3d)info.GetValue("Position", typeof(Point3d));
      m_V = (Vector3d)info.GetValue("Direction", typeof(Vector3d));
    }

    [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.SerializationFormatter)]
    void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
    {
      info.AddValue("Position", m_P);
      info.AddValue("Direction", m_V);
    }

    /// <summary>
    /// Gets the starting position of this ray.
    /// </summary>
    /// <since>5.0</since>
    public Point3d Position
    {
      get { return m_P; }
    }

    /// <summary>
    /// Gets the direction vector of this ray.
    /// </summary>
    /// <since>5.0</since>
    public Vector3d Direction
    {
      get { return m_V; }
    }

    /// <summary>
    /// Evaluates a point along the ray.
    /// </summary>
    /// <param name="t">The t parameter.</param>
    /// <returns>A point at (Direction*t + Position).</returns>
    /// <since>5.0</since>
    [ConstOperation]
    public Point3d PointAt(double t)
    {
      if (!m_P.IsValid || !m_V.IsValid)
        return Point3d.Unset;

      Vector3d v = m_V * t;
      Point3d rc = m_P + v;
      return rc;
    }

    #region operators

    /// <summary>
    /// Determines whether two <see cref="Ray3d"/> have equal values.
    /// </summary>
    /// <param name="a">The first <see cref="Ray3d"/>.</param>
    /// <param name="b">The second <see cref="Ray3d"/>.</param>
    /// <returns>true if position and direction of the two rays are equal; otherwise false.</returns>
    /// <since>5.0</since>
    public static bool operator ==(Ray3d a, Ray3d b)
    {
      return (a.m_P == b.m_P && a.m_V == b.m_V);
    }

    /// <summary>
    /// Determines whether two <see cref="Ray3d"/> have different values.
    /// </summary>
    /// <param name="a">The first <see cref="Ray3d"/>.</param>
    /// <param name="b">The second <see cref="Ray3d"/>.</param>
    /// <returns>true if position or direction (or both) in the two rays are different; otherwise false.</returns>
    /// <since>5.0</since>
    public static bool operator !=(Ray3d a, Ray3d b)
    {
      return (a.m_P != b.m_P || a.m_V != b.m_V);
    }
    #endregion

    /// <summary>
    /// Determines whether the specified System.Object is a Ray3d and has the same values as the present ray.
    /// </summary>
    /// <param name="obj">The specified object.</param>
    /// <returns>true if obj is a Ray3d and has the same position and direction as this; otherwise false.</returns>
    [ConstOperation]
    public override bool Equals(object obj)
    {
      return (obj is Ray3d && this == (Ray3d)obj);
    }

    /// <summary>
    /// Determines whether the specified Ray3d has the same value as the present ray.
    /// </summary>
    /// <param name="ray">The specified ray.</param>
    /// <returns>true if ray has the same position and direction as this; otherwise false.</returns>
    /// <since>5.0</since>
    [ConstOperation]
    public bool Equals(Ray3d ray)
    {
      return this == ray;
    }

    /// <summary>
    /// Check that all values in other are within epsilon of the values in this
    /// </summary>
    /// <param name="other"></param>
    /// <param name="epsilon"></param>
    /// <returns></returns>
    /// <since>5.4</since>
    [ConstOperation]
    public bool EpsilonEquals(Ray3d other, double epsilon)
    {
      return m_P.EpsilonEquals(other.m_P, epsilon) &&
             m_V.EpsilonEquals(other.m_V, epsilon);
    }

    /// <summary>
    /// Computes a hashing number that represents the current ray.
    /// </summary>
    /// <returns>A signed integer that represents both position and direction, but is not unique.</returns>
    [ConstOperation]
    public override int GetHashCode()
    {
      // MSDN docs recommend XOR'ing the internal values to get a hash code
      return m_P.GetHashCode() ^ m_V.GetHashCode();
    }
  }

  internal interface IValidable
  {
    bool IsValid
    {
      get;
    }
  }

  // 27 Jan 2010 - S. Baer
  // Removed PlaneEquation from library. Don't add until we actually need it
  //[StructLayout(LayoutKind.Sequential, Pack = 8, Size = 32)]
  //public struct PlaneEquation
  //{
  //  Vector3d m_N;
  //  double m_D;
  //}

  //[StructLayout(LayoutKind.Sequential, Pack = 8, Size = 16)]
  //public struct SurfaceCurvature
  //{
  //  double m_K1;
  //  double m_K2;
  //}
}
