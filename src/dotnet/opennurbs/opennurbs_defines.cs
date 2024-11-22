using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using Rhino.Geometry;

namespace Rhino
{
  /// <summary>
  /// Represents two indices: I and J.
  /// </summary>
  [StructLayout(LayoutKind.Sequential, Pack = 4, Size = 8)]
  [System.Diagnostics.DebuggerDisplay("{m_i}, {m_j}")]
  [Serializable]
  public struct IndexPair : IList<int>, IReadOnlyList<int>
  {
    int m_i, m_j;

    /// <summary>
    /// Initializes a new instance of <see cref="IndexPair"/> with two indices.
    /// </summary>
    /// <param name="i">A first index.</param>
    /// <param name="j">A second index.</param>
    /// <since>5.0</since>
    public IndexPair(int i, int j)
    {
      m_i = i;
      m_j = j;
    }
    
    /// <summary>
    /// Gets or sets the first, I index.
    /// </summary>
    /// <since>5.0</since>
    public int I
    {
      get { return m_i; }
      set { m_i = value; }
    }

    /// <summary>
    /// Gets or sets the second, J index.
    /// </summary>
    /// <since>5.0</since>
    public int J
    {
      get { return m_j; }
      set { m_j = value; }
    }

    /// <summary>
    /// Gets or sets a particular index of this structure.
    /// <remarks>Only 0 and 1 are valid indices.</remarks>
    /// </summary>
    /// <param name="index">Either 0 for I, or 1 for J.</param>
    /// <returns>The value at the index.</returns>
    public int this[int index]
    {
      get
      {
        switch (index)
        {
          case 0:
            return m_i;
          case 1:
            return m_j;
          default:
            throw new ArgumentOutOfRangeException(nameof(index));
        }
      }
      set
      {
        switch (index)
        {
          case 0:
            m_i = value;
            break;
          case 1:
            m_j = value;
            break;
          default:
            throw new ArgumentOutOfRangeException(nameof(index));
        }
      }
    }

    /// <summary>
    /// Determines the index of a specific item in <see cref="IndexPair"/>.
    /// </summary>
    /// 
    /// <returns>
    /// The index, 0 for I or 1 for J of <paramref name="item"/> if found in the list; otherwise, -1.
    /// </returns>
    /// <param name="item">The object to locate in the <see cref="T:System.Collections.Generic.IList`1"/>.</param>
    /// <since>6.0</since>
    public int IndexOf(int item)
    {
      return item == m_i ? 0 : (item == m_j ? 1 : -1);
    }

    /// <summary>
    /// Determines whether the <see cref="IndexPair"/> contains a specific value.
    /// </summary>
    /// 
    /// <returns>
    /// true if <paramref name="item"/> is found in the <see cref="IndexPair"/>; otherwise, false.
    /// </returns>
    /// <param name="item">The number to locate in the <see cref="IndexPair"/>.</param>
    /// <since>6.0</since>
    public bool Contains(int item)
    {
      return IndexOf(item) != -1;
    }

    /// <summary>
    /// Copies the elements of the <see cref="IndexPair"/> to an <see cref="T:System.Array"/>, starting at a particular <see cref="T:System.Array"/> index.
    /// </summary>
    /// <param name="array">The one-dimensional <see cref="T:System.Array"/> that is the destination of the elements copied
    /// from <see cref="IndexPair"/>.
    /// The <see cref="T:System.Array"/> must have zero-based indexing.</param>
    /// <param name="arrayIndex">The zero-based index in <paramref name="array"/> at which copying begins.</param>
    /// <exception cref="T:System.ArgumentNullException"><paramref name="array"/> is null.</exception>
    /// <exception cref="T:System.ArgumentOutOfRangeException"><paramref name="arrayIndex"/> is less than 0 or <paramref name="arrayIndex"/> is greater than or equals the length of array minus 2.</exception>
    /// <exception cref="T:System.ArgumentException">The length of the <paramref name="array"/> is less than 2.</exception>
    /// <since>6.0</since>
    public void CopyTo(int[] array, int arrayIndex)
    {
      if (array == null) throw new ArgumentNullException(nameof(array));
      if (arrayIndex < 0 || arrayIndex >= array.Length - 2) throw new ArgumentOutOfRangeException(nameof(arrayIndex));
      if (array.Length < 2) throw new ArgumentException("Array needs to be at least 2 items long for this IndexPair to fit.", nameof(array));

      array[arrayIndex] = m_i;
      array[arrayIndex + 1] = m_j;
    }

    /// <summary>
    /// Returns the amount of elements in this pair of indices, which is always 2.
    /// </summary>
    /// <since>6.0</since>
    public int Count => 2;

    /// <summary>
    /// Gets an enumerator that goes over <see cref="I"/> and <see cref="J"/>, in this order.
    /// </summary>
    /// <returns>The needed enumerator.</returns>
    /// <since>6.0</since>
    public IEnumerator<int> GetEnumerator()
    {
      yield return m_i;
      yield return m_j;
    }

    #region Explicit interface implementations

    void IList<int>.Insert(int index, int item)
    {
      throw new InvalidOperationException("IndexPair has fixed length");
    }

    void IList<int>.RemoveAt(int index)
    {
      throw new InvalidOperationException("IndexPair has fixed length");
    }

    void ICollection<int>.Add(int item)
    {
      throw new InvalidOperationException("IndexPair has fixed length");
    }

    void ICollection<int>.Clear()
    {
      throw new InvalidOperationException("IndexPair has fixed length");
    }

    bool ICollection<int>.IsReadOnly
    {
      get { return false; }
    }

    bool ICollection<int>.Remove(int item)
    {
      throw new InvalidOperationException("IndexPair has fixed length");
    }

    /// <since>6.0</since>
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
    {
      return GetEnumerator();
    }
    #endregion
  }

  /// <summary>
  /// Provides constants and static methods that are additional to
  /// <see cref="System.Math"/>.
  /// </summary>
  public static class RhinoMath
  {
    // Only used inside of this class. Not exposed since there is already
    // a Math.PI that people can use
    const double PI = 3.141592653589793238462643;

    /// <summary>
    /// Quarter of a rotation. 90 degrees. 1.57...
    /// </summary>
    public const double HalfPI = PI / 2;

    /// <summary>
    /// One eigth of a rotation. 45 degrees. 0.78...
    /// </summary>
    public const double QuarterPI = PI / 4;

    /// <summary>
    /// Full rotation. 360 degrees. 6.28...
    /// </summary>
    public const double TwoPI = Tau;

    /// <summary>
    /// Ratio of circumference divided by radius. Full rotation. 360 degrees. 6.28...
    /// </summary>
    public const double Tau = PI * 2;


    /// <summary>
    /// Gets Rhino's Zero Tolerance constant, which is 2^-32. In cases when an
    /// absolute "zero" tolerance is required to compare model space coordinates, 
    /// ZeroTolerance is used. The value of ZeroTolerance should be no smaller than 
    /// <see cref="RhinoMath.Epsilon"/> and should be several orders of magnitude smaller than <see cref="RhinoMath.SqrtEpsilon"/>.
    /// </summary>
    /// <remarks>
    /// This is equivalent to openNURBS ON_ZERO_TOLERANCE.
    /// </remarks>
    public const double ZeroTolerance = 2.3283064365386962890625e-10;

    /// <summary>
    /// Gets the Rhino standard Unset value. Use this value rather than Double.NaN when 
    /// a bogus floating point value is required.
    /// </summary>
    /// <remarks>
    /// This is equivalent to openNURBS ON_UNSET_VALUE.
    /// </remarks>
    public const double UnsetValue = -1.23432101234321e+308;

    /// <summary>
    /// Gets the value of DBL_EPSILON, which is the smallest positive floating point number x such that 1 + x != 1.
    /// This is different than Double.Epsilon which is the smallest positive Double value that is greater than zero.
    /// </summary>
    /// <remarks>
    /// This is equivalent to openNURBS ON_EPSILON.
    /// </remarks>
    public const double Epsilon = 2.2204460492503131e-16;

    /// <summary>
    /// Represents a default value that is used when comparing square roots.
    /// <para>This value is several orders of magnitude larger than <see cref="RhinoMath.ZeroTolerance"/>.</para>
    /// </summary>
    /// <remarks>
    /// This is equivalent to openNURBS ON_SQRT_EPSILON.
    /// </remarks>
    public const double SqrtEpsilon = 1.490116119385000000e-8;

    /// <summary>
    /// Represents the default angle tolerance, used when no other values are provided.
    /// <para>This is one degree, expressed in radians.</para>
    /// </summary>
    /// <remarks>
    /// This is equivalent to openNURBS ON_DEFAULT_ANGLE_TOLERANCE.
    /// </remarks>
    public const double DefaultAngleTolerance = PI / 180.0;

    /// <summary>
    /// Get Rhino's default distance tolerance in millimeters.
    /// </summary>
    /// <remarks>
    /// This is equivalent to openNURBS ON_DEFAULT_DISTANCE_TOLERANCE_MM.
    /// </remarks>
    public const double DefaultDistanceToleranceMillimeters = 0.01;

    /// <summary>
    /// Gets the single precision floating point number that is considered 'unset' in Rhino.
    /// </summary>
    /// <remarks>
    /// This is equivalent to openNURBS ON_UNSET_FLOAT.
    /// </remarks>
    public const float UnsetSingle = -1.234321e+38f;

    /// <summary>
    /// When signed int values are used in a context where 
    /// 0 and small negative values are valid indices and there needs
    /// to be a value that indicates the index is not set.
    /// </summary>
    /// <remarks>
    /// This is equivalent to openNURBS ON_UNSET_INT_INDEX.
    /// </remarks>
    public const int UnsetIntIndex = int.MinValue + 1;

    /// <summary>
    /// Convert an angle from degrees to radians.
    /// </summary>
    /// <param name="degrees">Degrees to convert (180 degrees equals pi radians).</param>
    /// <since>5.0</since>
    public static double ToRadians(double degrees)
    {
      return degrees * (PI / 180.0);
    }

    /// <summary>
    /// Convert an angle from radians to degrees.
    /// </summary>
    /// <param name="radians">Radians to convert (180 degrees equals pi radians).</param>
    /// <since>5.0</since>
    public static double ToDegrees(double radians)
    {
      return radians * (180.0 / PI);
    }

    /// <summary>
    /// Determines whether a <see cref="double"/> value is valid within the RhinoCommon context.
    /// <para>Rhino does not use Double.NaN by convention, so this test evaluates to true if:</para>
    /// <para>x is not equal to RhinoMath.UnsetValue</para>
    /// <para>System.Double.IsNaN(x) evaluates to false</para>
    /// <para>System.Double.IsInfinity(x) evaluates to false</para>
    /// </summary>
    /// <param name="x"><see cref="double"/> number to test for validity.</param>
    /// <returns>true if the number if valid, false if the number is NaN, Infinity or Unset.</returns>
    /// <since>5.0</since>
    public static bool IsValidDouble(double x)
    {
      return (x != UnsetValue) && (!double.IsInfinity(x)) && (!double.IsNaN(x));
    }

    /// <summary>
    /// Determines whether a <see cref="float"/> value is valid within the RhinoCommon context.
    /// <para>Rhino does not use Single.NaN by convention, so this test evaluates to true if:</para>
    /// <para>x is not equal to RhinoMath.UnsetValue,</para>
    /// <para>System.Single.IsNaN(x) evaluates to false</para>
    /// <para>System.Single.IsInfinity(x) evaluates to false</para>
    /// </summary>
    /// <param name="x"><see cref="float"/> number to test for validity.</param>
    /// <returns>true if the number if valid, false if the number is NaN, Infinity or Unset.</returns>
    /// <since>5.0</since>
    public static bool IsValidSingle(float x)
    {
      return (x != UnsetSingle) && (!float.IsInfinity(x)) && (!float.IsNaN(x));
    }

    /// <summary>
    /// Portrays an <see cref="int"/> index in text.
    /// </summary>
    /// <param name="index"><see cref="int"/> number express as string.</param>
    /// <returns>The text representation of the int index.</returns>
    /// <since>6.0</since>
    public static string IntIndexToString(int index)
    {
      string rc;
      if (index == RhinoMath.UnsetIntIndex)
        rc = "UnsetIntIndex";
      else
        rc = index.ToString();
      return rc;
    }

    /// <summary> 
    /// Computes the scale factor for changing the measurements unit systems.
    /// </summary>
    /// <param name="from">The system to convert from.</param>
    /// <param name="to">The system to convert measurements into.</param>
    /// <returns>A scale multiplier.</returns>
    /// <since>5.0</since>
    public static double UnitScale(UnitSystem from, UnitSystem to)
    {
      return UnsafeNativeMethods.ONC_UnitScale(from, to);
    }

    /// <summary>
    /// Computes the scale factor for changing the measurements unit systems.
    /// </summary>
    /// <param name="from">The system to convert from.</param>
    /// <param name="fromMetersPerUnit">For custom units, specify the meters per unit.</param>
    /// <param name="to">The system to convert measurements into.</param>
    /// <param name="toMetersPerUnit">For custom units, specify the meters per unit.</param>
    /// <returns>A scale multiplier.</returns>
    /// <since>8.0</since>
    public static double UnitScale(UnitSystem from, double fromMetersPerUnit, UnitSystem to, double toMetersPerUnit)
    {
      return UnsafeNativeMethods.ONC_UnitScale2(from, fromMetersPerUnit, to, toMetersPerUnit);
    }

    /// <summary>
    /// Return number of meters per one unit of a given unit system
    /// </summary>
    /// <param name="units"></param>
    /// <returns></returns>
    /// <since>7.1</since>
    public static double MetersPerUnit(UnitSystem units)
    {
      return UnsafeNativeMethods.ONC_MetersPerUnit(units);
    }

    /// <summary>
    /// Restricts a <see cref="int"/> to be specified within an interval of two integers.
    /// </summary>
    /// <param name="value">An integer.</param>
    /// <param name="bound1">A first bound.</param>
    /// <param name="bound2">A second bound. This does not necessarily need to be larger or smaller than bound1.</param>
    /// <returns>The clamped value.</returns>
    /// <since>5.0</since>
    public static int Clamp(int value, int bound1, int bound2)
    {
      int min = bound1;
      int max = bound2;

      if (bound1 > bound2)
      {
        min = bound2;
        max = bound1;
      }
      if (value > max)
        value = max;
      if (value < min)
        value = min;
      return value;
    }

    /// <summary>
    /// Limits a <see cref="double"/> to be specified within an interval of two numbers, by specifying a fixed minimum and maximum.
    /// </summary>
    /// <param name="value">A number.</param>
    /// <param name="bound1">A first bound.</param>
    /// <param name="bound2">A second bound. This does not necessarily need to be larger or smaller than bound1.</param>
    /// <returns>The clamped value.</returns>
    /// <since>5.0</since>
    public static double Clamp(double value, double bound1, double bound2)
    {
      double min = bound1;
      double max = bound2;

      if (bound1 > bound2)
      {
        min = bound2;
        max = bound1;
      }
      if (value > max)
        value = max;
      if (value < min)
        value = min;
      return value;
    }

    /// <summary>
    /// Limits a <see cref="double"/> to be specified within an interval of two numbers by repeating the available interval cyclically.
    /// </summary>
    /// <param name="value">A number.</param>
    /// <param name="bound1">A first bound.</param>
    /// <param name="bound2">A second bound. This does not necessarily need to be larger or smaller than bound1.</param>
    /// <returns></returns>
    /// <since>7.1</since>
    public static double Wrap(double value, double bound1, double bound2)
    {
      if (bound2 < bound1)
      {
        double tmp = bound1;
        bound1 = bound2;
        bound2 = tmp;
      }
      if (bound2 == bound1)
        value = bound1;
      else if (value < bound1)
        value = bound2 - (bound1 - value) % (bound2 - bound1);
      else if (bound2 < value)
        value = bound1 + (value - bound1) % (bound2 - bound1);
      return value;
    }

    /// <summary>
    /// Advances the cyclic redundancy check value remainder given a byte array.
    /// http://en.wikipedia.org/wiki/Cyclic_redundancy_check.
    /// </summary>
    /// <param name="currentRemainder">The remainder from which to start.</param>
    /// <param name="buffer">The value to add to the current remainder.</param>
    /// <returns>The new current remainder.</returns>
    /// <since>5.0</since>
    [CLSCompliant(false)]
    public static uint CRC32(uint currentRemainder, byte[] buffer)
    {
      return UnsafeNativeMethods.ON_CRC32_Compute(currentRemainder, buffer.Length, buffer);
    }

    /// <summary>
    /// Advances the cyclic redundancy check value remainder given a <see cref="double"/>.
    /// http://en.wikipedia.org/wiki/Cyclic_redundancy_check.
    /// </summary>
    /// <param name="currentRemainder">The remainder from which to start.</param>
    /// <param name="value">The value to add to the current remainder.</param>
    /// <returns>The new current remainder.</returns>
    /// <example>
    /// <code source='examples\vbnet\ex_analysismode.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_analysismode.cs' lang='cs'/>
    /// </example>
    /// <since>5.0</since>
    [CLSCompliant(false)]
    public static uint CRC32(uint currentRemainder, double value)
    {
      return CRC32(currentRemainder, BitConverter.GetBytes(value));
    }

    /// <summary>
    /// Advances the cyclic redundancy check value remainder given a <see cref="int"/>.
    /// http://en.wikipedia.org/wiki/Cyclic_redundancy_check.
    /// </summary>
    /// <param name="currentRemainder">The remainder from which to start.</param>
    /// <param name="value">The value to add to the current remainder.</param>
    /// <returns>The new current remainder.</returns>
    /// <since>5.0</since>
    [CLSCompliant(false)]
    public static uint CRC32(uint currentRemainder, int value)
    {
      return CRC32(currentRemainder, BitConverter.GetBytes(value));
    }

#if RHINO_SDK
    /// <summary>
    /// Evaluates command line math expression.  
    /// </summary>
    /// <param name="expression"></param>
    /// <returns>result</returns>
    /// <exception cref="ArgumentNullException"><paramref name="expression"/> is null.</exception>
    /// <exception cref="FormatException"><paramref name="expression"/> cannot be parsed.</exception>
    /// <since>6.0</since>
    public static double ParseNumber(string expression)
    {
      if (expression == null)
        throw new ArgumentNullException();

      double result = RhinoMath.UnsetValue;
      UnsafeNativeMethods.RHC_RhinoParseNumber(expression, ref result);

      if (RhinoMath.EpsilonEquals(result, RhinoMath.UnsetValue, RhinoMath.ZeroTolerance))
        throw new FormatException(string.Format("\"{0}\" expression could not be parsed", expression));
      return result;
    }

    /// <summary>
    /// Evaluates command line math expression.  
    /// </summary>
    /// <param name="expression"></param>
    /// <param name="result"></param>
    /// <returns>true if successful otherwise false</returns>
    /// <since>6.0</since>
    public static bool TryParseNumber(string expression, out double result)
    {
      result = RhinoMath.UnsetValue;

      if (expression == null)
        return false;

      UnsafeNativeMethods.RHC_RhinoParseNumber(expression, ref result);

      return !RhinoMath.EpsilonEquals(result, RhinoMath.UnsetValue, RhinoMath.ZeroTolerance);
    }
#endif

    /// <summary>
    /// Compare two doubles for equality within some "epsilon" range
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="epsilon"></param>
    /// <returns></returns>
    /// <since>5.4</since>
    public static bool EpsilonEquals(double x, double y, double epsilon)
    {
      // IEEE standard says that any comparison between NaN should return false;
      if (double.IsNaN(x) || double.IsNaN(y))
        return false;
      if (double.IsPositiveInfinity(x))
        return double.IsPositiveInfinity(y);
      if (double.IsNegativeInfinity(x))
        return double.IsNegativeInfinity(y);

      // if both are smaller than epsilon, their difference may not be.
      // therefore compare in absolute sense
      if (Math.Abs(x) < epsilon && Math.Abs(y) < epsilon)
      {
        bool result = Math.Abs(x - y) < epsilon;
        return result;
      }

      return (x >= y - epsilon && x <= y + epsilon);
    }

    /// <summary>
    /// Compare to floats for equality within some "epsilon" range
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="epsilon"></param>
    /// <returns></returns>
    /// <since>5.4</since>
    public static bool EpsilonEquals(float x, float y, float epsilon)
    {
      // IEEE standard says that any comparison between NaN should return false;
      if (float.IsNaN(x) || float.IsNaN(y))
        return false;
      if (float.IsPositiveInfinity(x))
        return float.IsPositiveInfinity(y);
      if (float.IsNegativeInfinity(x))
        return float.IsNegativeInfinity(y);

      // if both are smaller than epsilon, their difference may not be.
      // therefore compare in absolute sense
      if (Math.Abs(x) < epsilon && Math.Abs(y) < epsilon)
      {
        bool result = Math.Abs(x - y) < epsilon;
        return result;
      }

      return (x >= y - epsilon && x <= y + epsilon);
    }

    /// <summary>
    /// Expert tool to evaluate surface unit normal.
    /// </summary>
    /// <param name="limitDirection">Determines which direction is used to compute the limit, where:
    /// 0 = default, 1 = from quadrant I, 2 = from quadrant II, etc.</param>
    /// <param name="ds">First partial derivative.</param>
    /// <param name="dt">First partial derivative.</param>
    /// <param name="dss">Second partial derivative.</param>
    /// <param name="dst">Second partial derivative.</param>
    /// <param name="dtt">Second partial derivative.</param>
    /// <param name="n">Unit normal.</param>
    /// <returns>True if successful, false otherwise.</returns>
    /// <since>7.0</since>
    public static bool EvaluateNormal(
      int limitDirection,
      Rhino.Geometry.Vector3d ds,
      Rhino.Geometry.Vector3d dt,
      Rhino.Geometry.Vector3d dss,
      Rhino.Geometry.Vector3d dst,
      Rhino.Geometry.Vector3d dtt,
      out Rhino.Geometry.Vector3d n
      )
    {
      n = Rhino.Geometry.Vector3d.Unset;
      return UnsafeNativeMethods.ONC_EvNormal(limitDirection, ds, dt, dss, dst, dtt, ref n);
    }

    /// <summary>
    /// Expert tool to evaluate partial derivatives of surface unit normal.
    /// </summary>
    /// <param name="ds">First partial derivative.</param>
    /// <param name="dt">First partial derivative.</param>
    /// <param name="dss">Second partial derivative.</param>
    /// <param name="dst">Second partial derivative.</param>
    /// <param name="dtt">Second partial derivative.</param>
    /// <param name="ns">First partial derivative of unit normal. If the Jacobian is degenerate, ns is set to zero.</param>
    /// <param name="nt">First partial derivative of unit normal. If the Jacobian is degenerate, nt is set to zero.</param>
    /// <returns>true if Jacobian is non-degenerate, false if Jacobian is degenerate.</returns>
    /// <since>7.0</since>
    public static bool EvaluateNormalPartials(
      Rhino.Geometry.Vector3d ds,
      Rhino.Geometry.Vector3d dt,
      Rhino.Geometry.Vector3d dss,
      Rhino.Geometry.Vector3d dst,
      Rhino.Geometry.Vector3d dtt,
      out Rhino.Geometry.Vector3d ns,
      out Rhino.Geometry.Vector3d nt
      )
    {
      ns = Rhino.Geometry.Vector3d.Unset;
      nt = Rhino.Geometry.Vector3d.Unset;
      return UnsafeNativeMethods.ONC_EvNormalPartials(ds, dt, dss, dst, dtt, ref ns, ref nt);
    }

    #region Integration
#if false // moving into rhino 9 for now

    /// <summary>
    /// 
    /// </summary>
    /// <param name="context"></param>
    /// <param name="side"></param>
    /// <param name="t"></param>
    /// <returns></returns>
    public delegate double Integrate1Callback(Object context, Rhino.Geometry.CurveEvaluationSide side, double t);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="context"></param>
    /// <param name="side"></param>
    /// <param name="s"></param>
    /// <param name="t"></param>
    /// <returns></returns>
    public delegate double Integrate2Callback(Object context, Rhino.Geometry.CurveEvaluationSide side, double s, double t);


    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate double Integrate1CallbackWrapperDelegate(IntPtr gchContextWrapper, int limitDirection, double t);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate double Integrate2CallbackWrapperDelegate(IntPtr gchContextWrapper, int limitDirection, double s, double t);

    private class Context1Wrapper
    {
      public Integrate1Callback func;
      public Object context;

      public Context1Wrapper(Integrate1Callback _func, Object _context)
      {
        func = _func;
        context = _context;
      }
    }
    private class Context2Wrapper
    {
      public Integrate2Callback func;
      public Object context;

      public Context2Wrapper(Integrate2Callback _func, Object _context)
      {
        func = _func;
        context = _context;
      }
    }
    private static double Integrate1CallbackWrapper(IntPtr ptrContextWrapper, int limitDirection, double t)
    {
      Context1Wrapper contextWrapper = (GCHandle.FromIntPtr(ptrContextWrapper).Target) as Context1Wrapper;
      if (null == contextWrapper) return double.NaN;
      if (null == contextWrapper.context || null == contextWrapper.func) return double.NaN;

      // use the side enum
      Rhino.Geometry.CurveEvaluationSide side;
      if (limitDirection == 0) side = CurveEvaluationSide.Default;
      else if (limitDirection < 0) side = CurveEvaluationSide.Below;
      else side = CurveEvaluationSide.Above;

      return contextWrapper.func(contextWrapper.context, side, t);
    }

    private static double Integrate2CallbackWrapper(IntPtr ptrContextWrapper, int limitDirection, double s, double t)
    {
      Context2Wrapper contextWrapper = (GCHandle.FromIntPtr(ptrContextWrapper).Target) as Context2Wrapper;
      if (null == contextWrapper) return double.NaN;
      if (null == contextWrapper.context || null == contextWrapper.func) return double.NaN;

      Rhino.Geometry.CurveEvaluationSide side;
      if (limitDirection == 0) side = CurveEvaluationSide.Default;
      else if (limitDirection < 0) side = CurveEvaluationSide.Below;
      else side = CurveEvaluationSide.Above;
      return contextWrapper.func(contextWrapper.context, side, s, t);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="func"></param>
    /// <param name="context"></param>
    /// <param name="limits"></param>
    /// <param name="relativeTolerance"></param>
    /// <param name="absoluteTolerance"></param>
    /// <param name="errorBound"></param>
    /// <returns></returns>
    public static double Integrate(Integrate1Callback func, object context, Interval limits, double relativeTolerance, double absoluteTolerance, ref double errorBound)
    {
      if (null != func && null != context)
      {
        Integrate1CallbackWrapperDelegate funcWrapper = new Integrate1CallbackWrapperDelegate(Integrate1CallbackWrapper);
        Context1Wrapper contextWrapper = new Context1Wrapper(func, context);

        var gchCallbackWrapper = GCHandle.Alloc(funcWrapper);
        IntPtr ptrCallbackWrapper = Marshal.GetFunctionPointerForDelegate(funcWrapper);
        var gchContextWrapper = GCHandle.Alloc(contextWrapper);
        IntPtr ptrContextWrapper = GCHandle.ToIntPtr(gchContextWrapper);
        double rc = UnsafeNativeMethods.ON_Integrate_1D(ptrCallbackWrapper, ptrContextWrapper, limits, relativeTolerance, absoluteTolerance, ref errorBound);

        gchContextWrapper.Free();
        gchCallbackWrapper.Free();

        return rc;
      }
      return 0.0;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="func"></param>
    /// <param name="context"></param>
    /// <param name="curve"></param>
    /// <param name="relativeTolerance"></param>
    /// <param name="absoluteTolerance"></param>
    /// <param name="errorBound"></param>
    /// <returns></returns>
    public static double Integrate(Integrate1Callback func, object context, Curve curve, double relativeTolerance, double absoluteTolerance, ref double errorBound)
    {
      if (null != func && null != context && null != curve)
      {
        Integrate1CallbackWrapperDelegate funcWrapper = new Integrate1CallbackWrapperDelegate(Integrate1CallbackWrapper);
        Context1Wrapper contextWrapper = new Context1Wrapper(func, context);

        var gchCallbackWrapper = GCHandle.Alloc(funcWrapper);
        IntPtr ptrCallbackWrapper = Marshal.GetFunctionPointerForDelegate(funcWrapper);
        var gchContextWrapper = GCHandle.Alloc(contextWrapper);
        IntPtr ptrContextWrapper = GCHandle.ToIntPtr(gchContextWrapper);
        double rc = UnsafeNativeMethods.ON_Integrate_1D_Curve(ptrCallbackWrapper, ptrContextWrapper, curve.ConstPointer(), relativeTolerance, absoluteTolerance, ref errorBound);

        gchContextWrapper.Free();
        gchCallbackWrapper.Free();

        return rc;
      }
      return 0.0;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="func"></param>
    /// <param name="context"></param>
    /// <param name="limits1"></param>
    /// <param name="limits2"></param>
    /// <param name="relativeTolerance"></param>
    /// <param name="absoluteTolerance"></param>
    /// <param name="errorBound"></param>
    /// <returns></returns>
    public static double Integrate(Integrate2Callback func, object context, Interval limits1, Interval limits2, double relativeTolerance, double absoluteTolerance, ref double errorBound)
    {
      if (null != func && null != context)
      {
        Integrate2CallbackWrapperDelegate funcWrapper = new Integrate2CallbackWrapperDelegate(Integrate2CallbackWrapper);
        Context2Wrapper contextWrapper = new Context2Wrapper(func, context);

        var gchCallbackWrapper = GCHandle.Alloc(funcWrapper);
        IntPtr ptrCallbackWrapper = Marshal.GetFunctionPointerForDelegate(funcWrapper);
        var gchContextWrapper = GCHandle.Alloc(contextWrapper);
        IntPtr ptrContextWrapper = GCHandle.ToIntPtr(gchContextWrapper);
        double rc = UnsafeNativeMethods.ON_Integrate_2D(ptrCallbackWrapper, ptrContextWrapper, limits1, limits2, relativeTolerance, absoluteTolerance, ref errorBound);

        gchContextWrapper.Free();
        gchCallbackWrapper.Free();

        return rc;
      }
      return 0.0;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="callback"></param>
    /// <param name="context"></param>
    /// <param name="surface"></param>
    /// <param name="relativeTolerance"></param>
    /// <param name="absoluteTolerance"></param>
    /// <param name="errorBound"></param>
    /// <returns></returns>
    public static double Integrate(Integrate2Callback callback, object context, Surface surface, double relativeTolerance, double absoluteTolerance, ref double errorBound)
    {
      if (null != callback && null != context && null != surface)
      {
        Integrate2CallbackWrapperDelegate funcWrapper = new Integrate2CallbackWrapperDelegate(Integrate2CallbackWrapper);
        Context2Wrapper contextWrapper = new Context2Wrapper(callback, context);

        var gchCallbackWrapper = GCHandle.Alloc(funcWrapper);
        IntPtr ptrCallbackWrapper = Marshal.GetFunctionPointerForDelegate(funcWrapper);
        var gchContextWrapper = GCHandle.Alloc(contextWrapper);
        IntPtr ptrContextWrapper = GCHandle.ToIntPtr(gchContextWrapper);
        double rc = UnsafeNativeMethods.ON_Integrate_1D_Curve(ptrCallbackWrapper, ptrContextWrapper, surface.ConstPointer(), relativeTolerance, absoluteTolerance, ref errorBound);

        gchContextWrapper.Free();
        gchCallbackWrapper.Free();

        return rc;
      }
      return 0.0;
    }
    #endif
    #endregion // Integration
  }


  /// <summary>
  /// Provides the anti-alias levels used for render quality
  /// </summary>
  /// <since>6.0</since>
  public enum AntialiasLevel
  {
    /// <summary>
    /// Low quality
    /// </summary>
    None = 0,
    /// <summary>
    /// Draft quality
    /// </summary>
    Draft = 1,
    /// <summary>
    /// Good quality
    /// </summary>
    Good = 2,
    /// <summary>
    /// High quality
    /// </summary>
    High = 3
  }


  namespace Geometry
  {
    /// <summary>
    /// Provides enumerated values for continuity along geometry,
    /// such as continuous first derivative or continuous unit tangent and curvature.
    /// </summary>
    /// <since>5.0</since>
    public enum Continuity
    {
      /// <summary>
      /// There is no continuity.
      /// </summary>
      None = 0,

      /// <summary>
      /// Continuous Function : Test for parametric continuity. In particular, all types of curves
      /// are considered infinitely continuous at the start/end of the evaluation domain.
      /// </summary>
      C0_continuous = 1,

      /// <summary>
      /// Continuous first derivative : Test for parametric continuity. In particular,
      /// all types of curves are considered infinitely continuous at the start/end
      /// of the evaluation domain.
      /// </summary>
      C1_continuous = 2,

      /// <summary>
      /// Continuous first derivative and second derivative : Test for parametric continuity.
      /// In particular, all types of curves are considered infinitely continuous at the
      /// start/end of the evaluation domain.
      /// </summary>
      C2_continuous = 3,

      /// <summary>
      /// Continuous unit tangent : Test for parametric continuity. In particular, all types of
      /// curves are considered infinitely continuous at the start/end of the evaluation domain.
      /// </summary>
      G1_continuous = 4,

      /// <summary>
      /// Continuous unit tangent and curvature : Test for parametric continuity. In particular,
      /// all types of curves are considered infinitely continuous at the start/end of the
      /// evaluation domain.
      /// </summary>
      G2_continuous = 5,


      /// <summary>
      /// Locus continuous function :
      /// Continuity tests using the following enum values are identical to tests using the
      /// preceding enum values on the INTERIOR of a curve's domain. At the END of a curve
      /// a "locus" test is performed in place of a parametric test. In particular, at the
      /// END of a domain, all open curves are locus discontinuous. At the END of a domain,
      /// all closed curves are at least C0_locus_continuous. By convention all Curves
      /// are considered locus continuous at the START of the evaluation domain. This
      /// convention is not strictly correct, but it was adopted to make iterative kink
      /// finding tools easier to use and so that locus discontinuities are reported once
      /// at the end parameter of a curve rather than twice.
      /// </summary>
      C0_locus_continuous = 6,

      /// <summary>
      /// Locus continuous first derivative :
      /// Continuity tests using the following enum values are identical to tests using the
      /// preceding enum values on the INTERIOR of a curve's domain. At the END of a curve
      /// a "locus" test is performed in place of a parametric test. In particular, at the
      /// END of a domain, all open curves are locus discontinuous. At the END of a domain,
      /// all closed curves are at least C0_locus_continuous. By convention all Curves
      /// are considered locus continuous at the START of the evaluation domain. This
      /// convention is not strictly correct, but it was adopted to make iterative kink
      /// finding tools easier to use and so that locus discontinuities are reported once
      /// at the end parameter of a curve rather than twice.
      /// </summary>
      C1_locus_continuous = 7,

      /// <summary>
      /// Locus continuous first and second derivative :
      /// Continuity tests using the following enum values are identical to tests using the
      /// preceding enum values on the INTERIOR of a curve's domain. At the END of a curve
      /// a "locus" test is performed in place of a parametric test. In particular, at the
      /// END of a domain, all open curves are locus discontinuous. At the END of a domain,
      /// all closed curves are at least C0_locus_continuous. By convention all Curves
      /// are considered locus continuous at the START of the evaluation domain. This
      /// convention is not strictly correct, but it was adopted to make iterative kink
      /// finding tools easier to use and so that locus discontinuities are reported once
      /// at the end parameter of a curve rather than twice.
      /// </summary>
      C2_locus_continuous = 8,

      /// <summary>
      /// Locus continuous unit tangent :
      /// Continuity tests using the following enum values are identical to tests using the
      /// preceding enum values on the INTERIOR of a curve's domain. At the END of a curve
      /// a "locus" test is performed in place of a parametric test. In particular, at the
      /// END of a domain, all open curves are locus discontinuous. At the END of a domain,
      /// all closed curves are at least C0_locus_continuous. By convention all Curves
      /// are considered locus continuous at the START of the evaluation domain. This
      /// convention is not strictly correct, but it was adopted to make iterative kink
      /// finding tools easier to use and so that locus discontinuities are reported once
      /// at the end parameter of a curve rather than twice.
      /// </summary>
      G1_locus_continuous = 9,

      /// <summary>
      /// Locus continuous unit tangent and curvature :
      /// Continuity tests using the following enum values are identical to tests using the
      /// preceding enum values on the INTERIOR of a curve's domain. At the END of a curve
      /// a "locus" test is performed in place of a parametric test. In particular, at the
      /// END of a domain, all open curves are locus discontinuous. At the END of a domain,
      /// all closed curves are at least C0_locus_continuous. By convention all Curves
      /// are considered locus continuous at the START of the evaluation domain. This
      /// convention is not strictly correct, but it was adopted to make iterative kink
      /// finding tools easier to use and so that locus discontinuities are reported once
      /// at the end parameter of a curve rather than twice.
      /// </summary>
      G2_locus_continuous = 10,

      /// <summary>
      /// Analytic discontinuity. Cinfinity_continuous is a euphemism for "at a knot".
      /// </summary>
      Cinfinity_continuous = 11,

      /// <summary>
      /// Aesthetic discontinuity
      /// </summary>
      Gsmooth_continuous = 12,
    }

    //public enum ControlPointStyle : int
    //{
    //  None = 0,
    //  NotRational = 1,
    //  HomogeneousRational = 2,
    //  EuclideanRational = 3,
    //  IntrinsicPointStyle = 4,
    //}

    /// <summary>
    /// Defines enumerated values for various mesh types.
    /// </summary>
    /// <since>5.0</since>
    public enum MeshType
    {
      /// <summary>
      /// The default mesh.
      /// </summary>
      Default = 0,

      /// <summary>
      /// The render mesh.
      /// </summary>
      Render = 1,

      /// <summary>
      /// The analysis mesh.
      /// </summary>
      Analysis = 2,

      /// <summary>
      /// The preview mesh.
      /// </summary>
      Preview = 3,

      /// <summary>
      /// Any mesh that is available.
      /// </summary>
      Any = 4
    }
  }

  namespace DocObjects
  {
    /// <summary>Defines the current working space.</summary>
    /// <since>5.0</since>
    public enum ActiveSpace
    {
      /// <summary>There is no working space.</summary>
      None = 0,
      /// <summary>3d modeling or "world" space.</summary>
      ModelSpace = 1,
      /// <summary>page/layout/paper/printing space.</summary>
      PageSpace = 2,
      /// <summary>UV Editor space.</summary>
      UVEditorSpace = 3,
      /// <summary>Block Editor space.</summary>
      BlockEditorSpace = 4
    }

    /// <summary>
    /// Defines enumerated values for coordinate systems to use as references.
    /// </summary>
    /// <since>5.0</since>
    public enum CoordinateSystem
    {
      /// <summary>
      /// The world coordinate system. This has origin (0,0,0),
      /// X unit axis is (1, 0, 0) and Y unit axis is (0, 1, 0).
      /// </summary>
      World = 0,

      /// <summary>
      /// The camera coordinate system.
      /// </summary>
      Camera = 1,

      /// <summary>
      /// The clip coordinate system.
      /// </summary>
      Clip = 2,

      /// <summary>
      /// The screen coordinate system.
      /// </summary>
      Screen = 3
    }

    /// <summary>
    /// Defines enumerated values for the display and behavior of single objects.
    /// </summary>
    /// <since>5.0</since>
    public enum ObjectMode
    {
      ///<summary>Object mode comes from layer.</summary>
      Normal = 0,
      ///<summary>Not visible, object cannot be selected or changed.</summary>
      Hidden = 1,
      ///<summary>Visible, object cannot be selected or changed.</summary>
      Locked = 2,
      ///<summary>
      ///Object is part of an InstanceDefinition. The InstanceDefinition
      ///m_object_uuid[] array will contain this object attribute's id.
      ///</summary>
      InstanceDefinitionObject = 3
      //ObjectModeCount = 4
    }

    /// <summary>
    /// Defines enumerated values for the source of display color of single objects.
    /// </summary>
    /// <since>5.0</since>
    public enum ObjectColorSource
    {
      /// <summary>use color assigned to layer.</summary>
      ColorFromLayer = 0,
      /// <summary>use color assigned to object.</summary>
      ColorFromObject = 1,
      /// <summary>use diffuse render material color.</summary>
      ColorFromMaterial = 2,
      /// <summary>
      /// for objects with parents (like objects in instance references, use parent linetype)
      /// if no parent, treat as color_from_layer.
      /// </summary>
      ColorFromParent = 3
    }

    /// <summary>
    /// Defines enumerated values for the source of plotting/printing color of single objects.
    /// </summary>
    /// <since>5.0</since>
    public enum ObjectPlotColorSource
    {
      /// <summary>use plot color assigned to layer.</summary>
      PlotColorFromLayer = 0,
      /// <summary>use plot color assigned to object.</summary>
      PlotColorFromObject = 1,
      /// <summary>use display color.</summary>
      PlotColorFromDisplay = 2,
      /// <summary>
      /// for objects with parents (like objects in instance references, use parent plot color)
      /// if no parent, treat as plot_color_from_layer.
      /// </summary>
      PlotColorFromParent = 3
    }

    /// <summary>
    /// Defines enumerated values for the source of plotting/printing weight of single objects.
    /// </summary>
    /// <since>5.0</since>
    public enum ObjectPlotWeightSource
    {
      /// <summary>use plot color assigned to layer.</summary>
      PlotWeightFromLayer = 0,
      /// <summary>use plot color assigned to object.</summary>
      PlotWeightFromObject = 1,
      /// <summary>
      /// for objects with parents (like objects in instance references, use parent plot color)
      /// if no parent, treat as plot_color_from_layer.
      /// </summary>
      PlotWeightFromParent = 3
    }

    /// <summary>
    /// Defines enumerated values for the source of linetype of single objects.
    /// </summary>
    /// <since>5.0</since>
    public enum ObjectLinetypeSource
    {
      /// <summary>use line style assigned to layer.</summary>
      LinetypeFromLayer = 0,
      /// <summary>use line style assigned to object.</summary>
      LinetypeFromObject = 1,
      /// <summary>
      /// for objects with parents (like objects in instance references, use parent linetype)
      /// if not parent, treat as linetype_from_layer.
      /// </summary>
      LinetypeFromParent = 3
    }

    /// <summary>
    /// Defines enumerated values for the source of material of single objects.
    /// </summary>
    /// <since>5.0</since>
    public enum ObjectMaterialSource
    {
      /// <summary>use material assigned to layer.</summary>
      MaterialFromLayer = 0,
      /// <summary>use material assigned to object.</summary>
      MaterialFromObject = 1,
      /// <summary>
      /// for objects with parents, like definition geometry in instance
      /// references and faces in polysurfaces, this value indicates the
      /// material definition should come from the parent. If the object
      /// does not have an obvious "parent", then treat it the same as
      /// material_from_layer.
      /// </summary>
      MaterialFromParent = 3
    }

    /// <summary>
    /// Defines enumerated values for display modes, such as wireframe or shaded.
    /// </summary>
    /// <since>5.0</since>
    public enum DisplayMode
    {
      /// <summary>
      /// The default display mode.
      /// </summary>
      Default = 0,

      /// <summary>
      /// The wireframe display mode.
      /// <para>Objects are generally only outlined by their corresponding isocurves and edges.</para>
      /// </summary>
      Wireframe = 1,

      /// <summary>
      /// The shaded display mode.
      /// <para>Objects are generally displayed with their corresponding isocurves and edges,
      /// and are filled with their display colors.</para>
      /// </summary>
      Shaded = 2,

      /// <summary>
      /// The render display mode.
      /// <para>Objects are generally displayed in a similar way to the one that will be resulting
      /// from rendering.</para>
      /// </summary>
      RenderPreview = 3
    }

    /// <summary>
    /// Defines enumerated values for the display of distances in US customary and Imperial units.
    /// </summary>
    /// <since>5.0</since>
    public enum DistanceDisplayMode
    {
      /// <summary>
      /// Shows distance decimals.
      /// </summary>
      Decimal = 0,

      /// <summary>
      /// Show feet.
      /// </summary>
      Feet = 1,

      /// <summary>
      /// Show feet and inches.
      /// </summary>
      FeetAndInches = 2
    }

    /// <summary>
    /// Defines enumerated values for the display of angles.
    /// </summary>
    /// <since>6.0</since>
    public enum AngleDisplayMode
    {
      /// <summary>
      /// Shows angle in decimal degrees
      /// </summary>
      Degrees = 0,

      /// <summary>
      /// Show angle as degree,minute,second.
      /// </summary>
      DMS = 1,

      /// <summary>
      /// Show angle as grads.
      /// </summary>
      Grads = 2
    }

    /// <summary>
    /// Defines enumerated values for the line alignment of text.
    /// </summary>
    /// <since>5.0</since>
    public enum TextDisplayAlignment
    {
      /// <summary>
      /// Normal alignment.
      /// </summary>
      Normal = 0,

      /// <summary>
      /// Horizontal alignment.
      /// </summary>
      Horizontal = 1,

      /// <summary>
      /// Above line alignment.
      /// </summary>
      AboveLine = 2,

      /// <summary>
      /// In line alignment.
      /// </summary>
      InLine = 3
    }

    /// <summary>
    /// Defines binary mask values for each object type that can be found in a document.
    /// </summary>
    /// <since>5.0</since>
    [Flags, CLSCompliant(false)]
    public enum ObjectType : uint
    {
      /// <summary>
      /// Nothing.
      /// </summary>
      None = 0,

      /// <summary>
      /// A point.
      /// </summary>
      Point = 1,

      /// <summary>
      /// A point set or cloud.
      /// </summary>
      PointSet = 2,

      /// <summary>
      /// A curve.
      /// </summary>
      Curve = 4,

      /// <summary>
      /// A surface.
      /// </summary>
      Surface = 8,

      /// <summary>
      /// A brep.
      /// </summary>
      Brep = 0x10,

      /// <summary>
      /// A mesh.
      /// </summary>
      Mesh = 0x20,
      //Layer = 0x40,
      //Material = 0x80,

      /// <summary>
      /// A rendering light.
      /// </summary>
      Light = 0x100,

      /// <summary>
      /// An annotation.
      /// </summary>
      Annotation = 0x200,
      //UserData = 0x400,

      /// <summary>
      /// A block definition.
      /// </summary>
      InstanceDefinition = 0x800,

      /// <summary>
      /// A block reference.
      /// </summary>
      InstanceReference = 0x1000,

      /// <summary>
      /// A text dot.
      /// </summary>
      TextDot = 0x2000,

      /// <summary>Selection filter value - not a real object type.</summary>
      Grip = 0x4000,

      /// <summary>
      /// A detail.
      /// </summary>
      Detail = 0x8000,

      /// <summary>
      /// A hatch.
      /// </summary>
      Hatch = 0x10000,

      /// <summary>
      /// A morph control.
      /// </summary>
      MorphControl = 0x20000,

      /// <summary>
      /// A SubD object.
      /// </summary>
      SubD = 0x40000,

      /// <summary>
      /// A brep loop.
      /// </summary>
      BrepLoop = 0x80000,

      /// <summary>
      /// a brep vertex.
      /// </summary>
      BrepVertex = 0x100000,

      /// <summary>Selection filter value - not a real object type.</summary>
      PolysrfFilter = 0x200000,
      /// <summary>Selection filter value - not a real object type.</summary>
      EdgeFilter = 0x400000,
      /// <summary>Selection filter value - not a real object type.</summary>
      PolyedgeFilter = 0x800000,

      /// <summary>
      /// A mesh vertex.
      /// </summary>
      MeshVertex = 0x01000000,

      /// <summary>
      /// A mesh edge.
      /// </summary>
      MeshEdge = 0x02000000,

      /// <summary>
      /// A mesh face.
      /// </summary>
      MeshFace = 0x04000000,

      /// <summary>
      /// A cage.
      /// </summary>
      Cage = 0x08000000,

      /// <summary>
      /// A phantom object.
      /// </summary>
      Phantom = 0x10000000,

      /// <summary>
      /// A clipping plane.
      /// </summary>
      ClipPlane = 0x20000000,

      /// <summary>
      /// An extrusion.
      /// </summary>
      Extrusion = 0x40000000,

      /// <summary>
      /// All bits set.
      /// </summary>
      AnyObject = 0xFFFFFFFF
    }

    /// <summary>
    /// Defines bit mask values to represent object decorations.
    /// </summary>
    /// <since>5.0</since>
    [Flags]
    public enum ObjectDecoration
    {
      /// <summary>There are no object decorations.</summary>
      None = 0,
      /// <summary>Arrow head at start.</summary>
      StartArrowhead = 0x08,
      /// <summary>Arrow head at end.</summary>
      EndArrowhead = 0x10,
      /// <summary>Arrow head at start and end.</summary>
      BothArrowhead = 0x18
    }
  }

  /// <summary>
  /// Characters used for different 'drafting style' symbols
  /// </summary>
  public static class Symbols
  {
    // Character constants from opennurbs
    static readonly char g_degree_symbol = (char)UnsafeNativeMethods.ON_wString_DegreeSymbol();
    static readonly char g_radius_symbol = (char)UnsafeNativeMethods.ON_wString_RadiusSymbol();
    static readonly char g_diameter_symbol = (char)UnsafeNativeMethods.ON_wString_DiameterSymbol();
    static readonly char g_plusminus_symbol = (char)UnsafeNativeMethods.ON_wString_PlusMinusSymbol();

    /// <summary> Degree symbol used for angles </summary>
    /// <since>6.0</since>
    public static char DegreeSymbol { get { return g_degree_symbol; } }
    /// <summary> Radius symbol </summary>
    /// <since>6.0</since>
    public static char RadiusSymbol { get { return g_radius_symbol; } }
    /// <summary> Diameter symbol </summary>
    /// <since>6.0</since>
    public static char DiameterSymbol { get { return g_diameter_symbol; } }
    /// <summary> Plus-Minus tolerance symbol </summary>
    /// <since>6.0</since>
    public static char PlusMinusSymbol { get { return g_plusminus_symbol; } }
  }
}

namespace Rhino.Geometry
{
  /// <summary>
  /// Defines enumerated values to represent light styles or types, such as directional or spotlight.
  /// </summary>
  /// <since>5.0</since>
  public enum LightStyle
  {
    /// <summary>
    /// No light type. This is the default value of the enumeration type.
    /// </summary>
    None = 0,
    /// <summary>
    /// Light location and direction in camera coordinates.
    /// +x points to right, +y points up, +z points towards camera.
    /// </summary>
    CameraDirectional = 4,
    /// <summary>
    /// Light location and direction in camera coordinates.
    /// +x points to right, +y points up, +z points towards camera.
    /// </summary>
    CameraPoint = 5,
    /// <summary>
    /// Light location and direction in camera coordinates.
    /// +x points to right, +y points up, +z points towards camera.
    /// </summary>
    CameraSpot = 6,
    /// <summary>Light location and direction in world coordinates.</summary>
    WorldDirectional = 7,
    /// <summary>Light location and direction in world coordinates.</summary>
    WorldPoint = 8,
    /// <summary>Light location and direction in world coordinates.</summary>
    WorldSpot = 9,
    /// <summary>Ambient light.</summary>
    Ambient = 10,
    /// <summary>Linear light in world coordinates.</summary>
    WorldLinear = 11,
    /// <summary>Rectangular light in world coordinates.</summary>
    WorldRectangular = 12
  }

  /// <summary>
  /// Defines enumerated values to represent component index types.
  /// </summary>
  /// <since>5.0</since>
  public enum ComponentIndexType
  {
    /// <summary>
    /// Not used. This is the default value of the enumeration type.
    /// </summary>
    InvalidType = 0,

    /// <summary>
    /// Targets a brep vertex index.
    /// </summary>
    BrepVertex = 1,

    /// <summary>
    /// Targets a brep edge index.
    /// </summary>
    BrepEdge = 2,

    /// <summary>
    /// Targets a brep face index.
    /// </summary>
    BrepFace = 3,

    /// <summary>
    /// Targets a brep trim index.
    /// </summary>
    BrepTrim = 4,

    /// <summary>
    /// Targets a brep loop index.
    /// </summary>
    BrepLoop = 5,

    /// <summary>
    /// Targets a mesh vertex index.
    /// </summary>
    MeshVertex = 11,

    /// <summary>
    /// Targets a mesh topology vertex index.
    /// </summary>
    MeshTopologyVertex = 12,

    /// <summary>
    /// Targets a mesh topology edge index.
    /// </summary>
    MeshTopologyEdge = 13,

    /// <summary>
    /// Targets a mesh face index.
    /// </summary>
    MeshFace = 14,

    /// <summary>
    /// Targets a mesh n-gon index.
    /// </summary>
    MeshNgon = 15,

    /// <summary>
    /// Targets an instance definition part index.
    /// </summary>
    InstanceDefinitionPart = 21,

    /// <summary>
    /// Targets a polycurve segment index.
    /// </summary>
    PolycurveSegment = 31,

    /// <summary>
    /// Targets a point cloud point index.
    /// </summary>
    PointCloudPoint = 41,

    /// <summary>
    /// Targets a group member index.
    /// </summary>
    GroupMember = 51,

    /// <summary>
    /// 3d bottom profile curves. Index identifies profile component
    /// </summary>
    ExtrusionBottomProfile = 61,

    /// <summary>
    /// 3d top profile curves. Index identifies profile component
    /// </summary>
    ExtrusionTopProfile = 62,

    /// <summary>
    /// 3d wall edge curve.
    /// Index/2: identifies profile component
    /// Index%2: 0=start, 1=end
    /// </summary>
    ExtrusionWallEdge = 63,

    /// <summary>
    /// Side wall surfaces.
    /// Index identifies profile component
    /// </summary>
    ExtrusionWallSurface = 64,

    /// <summary>
    /// Bottom and top cap surfaces. Index 0=bottom, 1=top
    /// </summary>
    ExtrusionCapSurface = 65,

    /// <summary>
    /// Extrusion path (axis line). Index -1=entire path, 0=start point, 1=endpoint
    /// </summary>
    ExtrusionPath = 66,

    /// <summary>Targets a SubD vertex pointer Id. Ids are not guaranteed to be sequential.</summary>
    SubdVertex = 71, // SubDVertexPtr.Id, (use ON_SubD.ComponentPtrFromComponentIndex())

    /// <summary>Targets a SubD edge pointer Id. Ids are not guaranteed to be sequential.</summary>
    SubdEdge = 72,   // SubDEdgePtr.Id

    /// <summary>Targets a SubD face pointer Id. Ids are not guaranteed to be sequential.</summary>
    SubdFace = 73,   // SubDFacePtr.Id

    /// <summary>
    /// Targets a linear dimension point index.
    /// </summary>
    DimLinearPoint = 100,

    /// <summary>
    /// Targets a radial dimension point index.
    /// </summary>
    DimRadialPoint = 101,

    /// <summary>
    /// Targets an angular dimension point index.
    /// </summary>
    DimAngularPoint = 102,

    /// <summary>
    /// Targets an ordinate dimension point index.
    /// </summary>
    DimOrdinatePoint = 103,

    /// <summary>
    /// Targets a text point index.
    /// </summary>
    DimTextPoint = 104,

    /// <summary>
    /// Targets no specific type.
    /// </summary>
    NoType = 0x0FFFFFFF // switched to 0fffffff from 0xffffffff in order to maintain cls compliance
  }

  /// <summary>
  /// Represents an index of an element contained in another object.
  /// </summary>
  [StructLayout(LayoutKind.Sequential, Pack = 4, Size = 8)]
  [Serializable]
  public struct ComponentIndex : IEquatable<ComponentIndex>, IComparable<ComponentIndex>
  {
    private readonly uint m_type;
    private readonly int m_index;

    /// <summary>
    /// Construct component index with a specific type/index combination
    /// </summary>
    /// <param name="type"></param>
    /// <param name="index"></param>
    /// <since>5.0</since>
    public ComponentIndex(ComponentIndexType type, int index)
    {
      m_type = (uint)type;
      m_index = index;
    }

    /// <summary>
    /// The interpretation of Index depends on the Type value.
    /// Type             m_index interpretation (0 based indices)
    /// no_type            used when context makes it clear what array is being index
    /// brep_vertex        Brep.m_V[] array index
    /// brep_edge          Brep.m_E[] array index
    /// brep_face          Brep.m_F[] array index
    /// brep_trim          Brep.m_T[] array index
    /// brep_loop          Brep.m_L[] array index
    /// mesh_vertex        Mesh.m_V[] array index
    /// meshtop_vertex     MeshTopology.m_topv[] array index
    /// meshtop_edge       MeshTopology.m_tope[] array index
    /// mesh_face          Mesh.m_F[] array index
    /// idef_part          InstanceDefinition.m_object_uuid[] array index
    /// polycurve_segment  PolyCurve::m_segment[] array index
    /// dim_linear_point   LinearDimension2::POINT_INDEX
    /// dim_radial_point   RadialDimension2::POINT_INDEX
    /// dim_angular_point  AngularDimension2::POINT_INDEX
    /// dim_ordinate_point OrdinateDimension2::POINT_INDEX
    /// dim_text_point     TextEntity2 origin point.
    /// </summary>
    /// <since>5.0</since>
    public ComponentIndexType ComponentIndexType
    {
      get
      {
        if (0xFFFFFFFF == m_type)
          return ComponentIndexType.NoType;
        int t = (int)m_type;
        return (ComponentIndexType)t;
      }
    }
    /// <summary>
    /// The interpretation of m_index depends on the m_type value.
    /// m_type             m_index interpretation (0 based indices)
    /// no_type            used when context makes it clear what array is being index
    /// brep_vertex        Brep.m_V[] array index
    /// brep_edge          Brep.m_E[] array index
    /// brep_face          Brep.m_F[] array index
    /// brep_trim          Brep.m_T[] array index
    /// brep_loop          Brep.m_L[] array index
    /// mesh_vertex        Mesh.m_V[] array index
    /// meshtop_vertex     MeshTopology.m_topv[] array index
    /// meshtop_edge       MeshTopology.m_tope[] array index
    /// mesh_face          Mesh.m_F[] array index
    /// idef_part          InstanceDefinition.m_object_uuid[] array index
    /// polycurve_segment  PolyCurve::m_segment[] array index
    /// dim_linear_point   LinearDimension2::POINT_INDEX
    /// dim_radial_point   RadialDimension2::POINT_INDEX
    /// dim_angular_point  AngularDimension2::POINT_INDEX
    /// dim_ordinate_point OrdinateDimension2::POINT_INDEX
    /// dim_text_point     TextEntity2 origin point.
    /// </summary>
    /// <since>5.0</since>
    public int Index
    {
      get { return m_index; }
    }

    /// <summary>
    /// Return true is this component index is the same as the Unset component index
    /// </summary>
    /// <returns></returns>
    /// <since>7.0</since>
    public bool IsUnset()
    {
      var unset = Unset;
      return m_index == unset.m_index && m_type == unset.m_type;
    }

    /// <summary>
    /// Determines whether the specified <see cref="ComponentIndex" /> is equal to the current <see cref="ComponentIndex" />,
    /// comparing by value.
    /// </summary>
    /// <param name="other">The other <see cref="ComponentIndex" /> to compare with.</param>
    /// <returns>true if other has the same ComponentIndexType and Index as this; otherwise, false.</returns>
    /// <since>8.0</since>
    public bool Equals(ComponentIndex other)
    {
      return m_type == other.m_type && m_index == other.m_index;
    }

    /// <summary>
    /// Computes the hash code for this <see cref="ComponentIndex" /> object.
    /// </summary>
    /// <returns>A hash value that might be equal for two different <see cref="ComponentIndex" /> values.</returns>
    /// <since>8.0</since>
    public override int GetHashCode()
    {
      //This was generated by Visual Studios tool when adding the interface
      int hashCode = 345902784;
      hashCode = hashCode * -1521134295 + m_type.GetHashCode();
      hashCode = hashCode * -1521134295 + m_index.GetHashCode();
      return hashCode;
    }

    /// <summary>
    /// Determines whether the specified <see cref="object" /> is equal to the current <see cref="ComponentIndex" />,
    /// comparing by value.
    /// </summary>
    /// <param name="obj">The other object to compare with.</param>
    /// <returns>true if obj is a <see cref="ComponentIndex" /> and ComponentIndexType and Index is the same; false otherwise.</returns>
    /// <since>8.0</since>
    public override bool Equals(object obj)
    {
      return obj is ComponentIndex idx && this == idx;
    }

    /// <summary>
    /// Compares this <see cref="ComponentIndex" /> with another <see cref="ComponentIndex" />.
    /// </summary>
    /// <param name="other">The other <see cref="ComponentIndex" /> to use in comparison</param>
    /// <returns>
    /// <para> 0: if this is identical to other</para>
    /// <para>-1: if this.ComponentIndexType &lt; other.ComponentIndexType</para>
    /// <para>-1: if this.ComponentIndexType == other.ComponentIndexType and this.Index &lt; other.Index</para>
    /// <para>+1: otherwise.</para>
    /// </returns>
    /// <since>8.0</since>
    public int CompareTo(ComponentIndex other)
    {
      //Copied from ON_COMPONENT_INDEX::Compare
      int lhs_i = (int)m_type;
      int rhs_i = (int)other.m_type;
      if (lhs_i < rhs_i)
        return -1;
      if (lhs_i > rhs_i)
        return 1;

      if (m_index < other.m_index)
        return -1;
      if (m_index > other.m_index)
        return 1;
      return 0;
    }

    /// <summary>
    /// Determines whether two ComponentIndexes have equal values.
    /// </summary>
    /// <param name="a">The first <see cref="ComponentIndex" /></param>
    /// <param name="b">The second <see cref="ComponentIndex" /></param>
    /// <returns>True if the ComponentIndexes are equal</returns>
    /// <since>8.0</since>
    public static bool operator==(ComponentIndex a, ComponentIndex b)
    {
      return a.ComponentIndexType == b.ComponentIndexType && a.Index == b.Index;
    }

    /// <summary>
    /// Determines whether two ComponentIndexes have different values.
    /// </summary>
    /// <param name="a">The first <see cref="ComponentIndex" /></param>
    /// <param name="b">The second <see cref="ComponentIndex" /></param>
    /// <returns>True if the ComponentIndexes are unequal</returns>
    /// <since>8.0</since>
    public static bool operator!=(ComponentIndex a, ComponentIndex b)
    {
      return a.ComponentIndexType != b.ComponentIndexType || a.Index != b.Index;
    }

    /// <summary>
    /// Determines whether the first <see cref="ComponentIndex" /> comes before
    /// the second.
    /// </summary>
    /// <param name="a">The first <see cref="ComponentIndex" /></param>
    /// <param name="b">The second <see cref="ComponentIndex" /></param>
    /// <returns>true if the first comes before the second; otherwise, false.</returns>
    /// <since>8.0</since>
    public static bool operator<(ComponentIndex a, ComponentIndex b)
    {
      return a.CompareTo(b) < 0;
    }

    /// <summary>
    /// Determines whether the first <see cref="ComponentIndex" /> comes after
    /// the second.
    /// </summary>
    /// <param name="a">The first <see cref="ComponentIndex" /></param>
    /// <param name="b">The second <see cref="ComponentIndex" /></param>
    /// <returns>true if the first comes after the second; otherwise, false.</returns>
    /// <since>8.0</since>
    public static bool operator>(ComponentIndex a, ComponentIndex b)
    {
      return a.CompareTo(b) > 0;
    }

    /// <summary>
    /// Determines whether the first <see cref="ComponentIndex" /> comes before
    /// the second, or it is equal to it.
    /// </summary>
    /// <param name="a">The first <see cref="ComponentIndex" /></param>
    /// <param name="b">The second <see cref="ComponentIndex" /></param>
    /// <returns>true if the first is equal to the second or comes before it; otherwise, false</returns>
    /// <since>8.0</since>
    public static bool operator<=(ComponentIndex a, ComponentIndex b)
    {
      return a.CompareTo(b) <= 0;
    }

    /// <summary>
    /// Determines whether the first <see cref="ComponentIndex" /> comes after
    /// the second, or it is equal to it.
    /// </summary>
    /// <param name="a">The first <see cref="ComponentIndex" /></param>
    /// <param name="b">The second <see cref="ComponentIndex" /></param>
    /// <returns>true if the first is equal to the second or comes after it; otherwise, false</returns>
    /// <since>8.0</since>
    public static bool operator>=(ComponentIndex a, ComponentIndex b)
    {
      return a.CompareTo(b) >= 0;
    }

    /// <summary>
    /// The unset value of component index.
    /// </summary>
    /// <since>5.0</since>
    public static ComponentIndex Unset
    {
      get { return new ComponentIndex(ComponentIndexType.InvalidType, -1); }
    }
  }
}

namespace Rhino.Display
{
  ///<summary>
  ///Style of color gradient
  ///</summary>
  /// <since>7.0</since>
  public enum GradientType
  {
    ///<summary>No gradient</summary>
    None = 0,
    ///<summary>Linear (or axial) gradient between two points</summary>
    Linear = 1,
    ///<summary>Radial (or spherical) gradient using a center point and a radius</summary>
    Radial = 2,
    ///<summary>Disabled linear gradient. Useful for keeping gradient information around, but not having it displayed</summary>
    LinearDisabled = 3,
    ///<summary>Disabled radial gradient. Useful for keeping gradient information around, but not having it displayed</summary>
    RadialDisabled = 4
  }
}
