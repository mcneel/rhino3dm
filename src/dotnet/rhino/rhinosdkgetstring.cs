#pragma warning disable 1591
using System;

#if RHINO_SDK
namespace Rhino.Input.Custom
{
  /// <summary>Used to get strings.</summary>
  public class GetString : GetBaseClass
  {
    /// <summary>
    /// Constructs a new GetString.
    /// </summary>
    /// <example>
    /// <code source='examples\vbnet\ex_addlayer.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_addlayer.cs' lang='cs'/>
    /// <code source='examples\py\ex_addlayer.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    public GetString()
    {
      IntPtr ptr = UnsafeNativeMethods.CRhinoGetString_New();
      Construct(ptr);
    }

    /// <summary>Returns the string that the user typed. By default, space stops the string input.</summary>
    /// <returns>The result type. If the user typed a string, this is <see cref="GetResult.String"/>.</returns>
    /// <example>
    /// <code source='examples\vbnet\ex_addlayer.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_addlayer.cs' lang='cs'/>
    /// <code source='examples\py\ex_addlayer.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    [CLSCompliant(false)]
    public GetResult Get()
    {
      IntPtr ptr = NonConstPointer();
      int rc = UnsafeNativeMethods.CRhinoGetString_Get(ptr, false);
      GC.KeepAlive(this);
      return (GetResult)rc;
    }

    /// <summary>Returns the string that the user typed. By default, space does not stop input.</summary>
    /// <returns>The result type. If the user typed a string, this is <see cref="GetResult.String"/>.</returns>
    /// <since>5.0</since>
    [CLSCompliant(false)]
    public GetResult GetLiteralString()
    {
      IntPtr ptr = NonConstPointer();
      int rc = UnsafeNativeMethods.CRhinoGetString_Get(ptr, true);
      GC.KeepAlive(this);
      return (GetResult)rc;
    }
  }

  /// <summary>
  /// If you want to explicitly get string input, then use GetString class with
  /// options. If you only want to get options, then use this class (GetOption)
  /// </summary>
  public class GetOption : GetBaseClass
  {
    /// <since>5.0</since>
    public GetOption()
    {
      IntPtr ptr = UnsafeNativeMethods.CRhinoGetOption_New();
      Construct(ptr);
    }

    /// <summary>
    /// Call to get an option. A return value of "option" means the user selected
    /// a valid option. Use Option() the determine which option.
    /// </summary>
    /// <returns>If the user chose an option, then <see cref="GetResult.Option"/>; another enumeration value otherwise.</returns>
    /// <since>5.0</since>
    [CLSCompliant(false)]
    public GetResult Get()
    {
      IntPtr ptr = NonConstPointer();
      int rc = UnsafeNativeMethods.CRhinoGetOption_Get(ptr);
      GC.KeepAlive(this);
      return (GetResult)rc;
    }
  }

  /// <summary>Used to get double precision numbers.</summary>
  public class GetNumber : GetBaseClass
  {
    /// <summary>Create a new GetNumber.</summary>
    /// <example>
    /// <code source='examples\vbnet\ex_addbackgroundbitmap.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_addbackgroundbitmap.cs' lang='cs'/>
    /// <code source='examples\py\ex_addbackgroundbitmap.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    public GetNumber()
    {
      IntPtr ptr = UnsafeNativeMethods.CRhinoGetNumber_New();
      Construct(ptr);
    }

    /// <summary>Call to get a number.</summary>
    /// <returns>If the user chose a number, then <see cref="GetResult.Number"/>; another enumeration value otherwise.</returns>
    /// <example>
    /// <code source='examples\vbnet\ex_addbackgroundbitmap.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_addbackgroundbitmap.cs' lang='cs'/>
    /// <code source='examples\py\ex_addbackgroundbitmap.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    [CLSCompliant(false)]
    public GetResult Get()
    {
      IntPtr ptr = NonConstPointer();
      int rc = UnsafeNativeMethods.CRhinoGetNumber_Get(ptr);
      GC.KeepAlive(this);
      return (GetResult)rc;
    }

    /// <summary>
    /// Sets a lower limit on the number that can be returned.
    /// By default there is no lower limit.
    /// </summary>
    /// <param name="lowerLimit">smallest acceptable number.</param>
    /// <param name="strictlyGreaterThan">
    /// If true, then the returned number will be > lower_limit.
    /// </param>
    /// <example>
    /// <code source='examples\vbnet\ex_addbackgroundbitmap.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_addbackgroundbitmap.cs' lang='cs'/>
    /// <code source='examples\py\ex_addbackgroundbitmap.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    public void SetLowerLimit(double lowerLimit, bool strictlyGreaterThan)
    {
      IntPtr ptr = NonConstPointer();
      UnsafeNativeMethods.CRhinoGetNumber_SetLimit(ptr, lowerLimit, strictlyGreaterThan, true);
      GC.KeepAlive(this);
    }

    /// <summary>
    /// Sets an upper limit on the number that can be returned.
    /// By default there is no upper limit.
    /// </summary>
    /// <param name="upperLimit">largest acceptable number.</param>
    /// <param name="strictlyLessThan">If true, then the returned number will be &lt; upper_limit.</param>
    /// <since>5.0</since>
    public void SetUpperLimit( double upperLimit, bool strictlyLessThan )
    {
      IntPtr ptr = NonConstPointer();
      UnsafeNativeMethods.CRhinoGetNumber_SetLimit(ptr, upperLimit, strictlyLessThan, false);
      GC.KeepAlive(this);
    }

  }

  /// <summary>Used to get integer numbers.</summary>
  public class GetInteger : GetBaseClass
  {
    /// <since>5.0</since>
    public GetInteger()
    {
      IntPtr ptr = UnsafeNativeMethods.CRhinoGetInteger_New();
      Construct(ptr);
    }

    /// <summary>
    /// Call to get an integer.
    /// </summary>
    /// <returns>If the user chose a number, then <see cref="GetResult.Number"/>; another enumeration value otherwise.</returns>
    /// <since>5.0</since>
    [CLSCompliant(false)]
    public GetResult Get()
    {
      IntPtr ptr = NonConstPointer();
      int rc = UnsafeNativeMethods.CRhinoGetInteger_Get(ptr);
      GC.KeepAlive(this);
      return (GetResult)rc;
    }

    /// <since>5.0</since>
    public new int Number()
    {
      IntPtr ptr = NonConstPointer();
      int rc = UnsafeNativeMethods.CRhinoGetInteger_Number(ptr);
      GC.KeepAlive(this);
      return rc;
    }

    /// <summary>
    /// Sets a lower limit on the number that can be returned.
    /// By default there is no lower limit.
    /// </summary>
    /// <param name="lowerLimit">smallest acceptable number.</param>
    /// <param name="strictlyGreaterThan">
    /// If true, then the returned number will be > lower_limit.
    /// </param>
    /// <since>5.0</since>
    public void SetLowerLimit( int lowerLimit, bool strictlyGreaterThan )
    {
      IntPtr ptr = NonConstPointer();
      UnsafeNativeMethods.CRhinoGetInteger_SetLimit(ptr, lowerLimit, strictlyGreaterThan, true);
      GC.KeepAlive(this);
    }

    /// <summary>
    /// Sets an upper limit on the number that can be returned.
    /// By default there is no upper limit.
    /// </summary>
    /// <param name="upperLimit">largest acceptable number.</param>
    /// <param name="strictlyLessThan">If true, then the returned number will be &lt; upper_limit.</param>
    /// <since>5.0</since>
    public void SetUpperLimit( int upperLimit, bool strictlyLessThan )
    {
      IntPtr ptr = NonConstPointer();
      UnsafeNativeMethods.CRhinoGetInteger_SetLimit(ptr, upperLimit, strictlyLessThan, false);
      GC.KeepAlive(this);
    }
  }

  // skipping CRhinoGetColor - somewhat unusual
}

namespace Rhino.Geometry
{
  /// <summary>
  /// RebuildCurveIntensity used used to specify rebuild smoothing
  /// and uniformity intensities. 
  /// </summary>
  /// <since>9.0</since>
  public enum RebuildCurveIntensity : byte
  {
    None = 0,
    Low = 1,
    Moderate = 2,
    Medium = 3,
    High = 4,
    Extreme = 5,
  }

  /// <summary>
  /// RebuildCurveTangentMatch used used to specify tangent matching options.
  /// </summary>
  /// <since>9.0</since>
  public enum RebuildCurveTangentMatching : byte
  {
    None = 0,
    AtStart = 1,
    AtEnd = 2,
    AtStartAndEnd = 3,
  }

  /// <since>9.0</since>
  public enum RebuildCurveKinkSplitting : byte
  {
    None = 0,
    AtG1Changes = 1,
    AtLargeG2Changes = 2,
    AtMediumG2Changes = 3,
    AtSmallG2Changes = 4,
  }

  /// <summary>
  /// Options used for curve rebuilding
  /// </summary>
  public class RebuildCurveOptions : IDisposable
  {
    bool m_delete_pointer = true;
    IntPtr m_ptr; // This class is never const
    internal IntPtr ConstPointer() { return m_ptr; }
    internal IntPtr NonConstPointer() { return m_ptr; }

    /// <summary>
    /// </summary>
    /// <since>9.0</since>
    public RebuildCurveOptions()
    {
      m_ptr = UnsafeNativeMethods.CRhinoRebuildCurveOptions_New();
    }

    ~RebuildCurveOptions()
    {
      Dispose();
    }

    internal RebuildCurveOptions(IntPtr ptr)
    {
      m_ptr = ptr;
      m_delete_pointer = false;
    }

    internal void ReleaseIntPtr()
    {
      m_ptr = IntPtr.Zero;
    }


    /// <summary>
    /// Actively reclaims unmanaged resources that this instance uses.
    /// </summary>
    /// <since>9.0</since>
    public void Dispose()
    {
      if (IntPtr.Zero != m_ptr && m_delete_pointer)
      {
        UnsafeNativeMethods.CRhinoRebuildCurveOptions_Delete(m_ptr);
        m_ptr = IntPtr.Zero;
        GC.SuppressFinalize(this);
      }
    }

    private int GetInt(UnsafeNativeMethods.RebuildCurveOptionsInteger which)
    {
      IntPtr constPtr = ConstPointer();
      int rc = UnsafeNativeMethods.CRhinoRebuildCurveOptions_GetInt(constPtr, which);
      GC.KeepAlive(this);
      return rc;
    }

    private void SetInt(UnsafeNativeMethods.RebuildCurveOptionsInteger which, int val)
    {
      IntPtr ptr = NonConstPointer();
      UnsafeNativeMethods.CRhinoRebuildCurveOptions_SetInt(ptr, which, val);
      GC.KeepAlive(this);
    }

    /// <since>9.0</since>
    public int IntensityToInt(Rhino.Geometry.RebuildCurveIntensity intensity)
    {
      int n;
      switch (intensity)
      {
        case Rhino.Geometry.RebuildCurveIntensity.None:
          n = 0;
          break;
        case Rhino.Geometry.RebuildCurveIntensity.Low:
          n = 1;
          break;
        case Rhino.Geometry.RebuildCurveIntensity.Moderate:
          n = 2;
          break;
        case Rhino.Geometry.RebuildCurveIntensity.Medium:
          n = 3;
          break;
        case Rhino.Geometry.RebuildCurveIntensity.High:
          n = 4;
          break;
        case Rhino.Geometry.RebuildCurveIntensity.Extreme:
          n = 5;
          break;
        default:
          n = 0;
          break;
      }
      return n;
    }

    /// <since>9.0</since>
    public Rhino.Geometry.RebuildCurveIntensity IntToIntensity(int intensity_as_int)
    {
      Rhino.Geometry.RebuildCurveIntensity intensity;
      switch (intensity_as_int)
      {
        case 0:
          intensity = Rhino.Geometry.RebuildCurveIntensity.None;
          break;
        case 1:
          intensity = Rhino.Geometry.RebuildCurveIntensity.Low;
          break;
        case 2:
          intensity = Rhino.Geometry.RebuildCurveIntensity.Moderate;
          break;
        case 3:
          intensity = Rhino.Geometry.RebuildCurveIntensity.Medium;
          break;
        case 4:
          intensity = Rhino.Geometry.RebuildCurveIntensity.High;
          break;
        case 5:
          intensity = Rhino.Geometry.RebuildCurveIntensity.Extreme;
          break;
        default:
          intensity = Rhino.Geometry.RebuildCurveIntensity.None;
          break;
      }
      return intensity;
    }

    private int TangentMatchingToInt(Rhino.Geometry.RebuildCurveTangentMatching tangent_matching)
    {
      int n;
      switch (tangent_matching)
      {
        case Rhino.Geometry.RebuildCurveTangentMatching.None:
          n = 0;
          break;
        case Rhino.Geometry.RebuildCurveTangentMatching.AtStart:
          n = 1;
          break;
        case Rhino.Geometry.RebuildCurveTangentMatching.AtEnd:
          n = 2;
          break;
        case Rhino.Geometry.RebuildCurveTangentMatching.AtStartAndEnd:
          n = 3;
          break;
        default:
          n = 0;
          break;
      }
      return n;
    }
    private Rhino.Geometry.RebuildCurveTangentMatching IntToTangentMatch(int tangent_matching_as_int)
    {
      Rhino.Geometry.RebuildCurveTangentMatching tangent_matching;
      switch (tangent_matching_as_int)
      {
        case 0:
          tangent_matching = Rhino.Geometry.RebuildCurveTangentMatching.None;
          break;
        case 1:
          tangent_matching = Rhino.Geometry.RebuildCurveTangentMatching.AtStart;
          break;
        case 2:
          tangent_matching = Rhino.Geometry.RebuildCurveTangentMatching.AtEnd;
          break;
        case 3:
          tangent_matching = Rhino.Geometry.RebuildCurveTangentMatching.AtStartAndEnd;
          break;

        default:
          tangent_matching = Rhino.Geometry.RebuildCurveTangentMatching.None;
          break;
      }
      return tangent_matching;
    }

    /// <since>9.0</since>
    public int KinkSplittingToInt(Rhino.Geometry.RebuildCurveKinkSplitting kink_splitting)
    {
      int n;
      switch (kink_splitting)
      {
        case Rhino.Geometry.RebuildCurveKinkSplitting.None:
          n = 0;
          break;
        case Rhino.Geometry.RebuildCurveKinkSplitting.AtG1Changes:
          n = 1;
          break;
        case Rhino.Geometry.RebuildCurveKinkSplitting.AtLargeG2Changes:
          n = 2;
          break;
        case Rhino.Geometry.RebuildCurveKinkSplitting.AtMediumG2Changes:
          n = 3;
          break;
        case Rhino.Geometry.RebuildCurveKinkSplitting.AtSmallG2Changes:
          n = 4;
          break;
        default:
          n = 0;
          break;
      }
      return n;
    }

    /// <since>9.0</since>
    public Rhino.Geometry.RebuildCurveKinkSplitting IntToKinkSplitting(int kink_splitting_at_as_int)
    {
      Rhino.Geometry.RebuildCurveKinkSplitting kink_splitting;
      switch (kink_splitting_at_as_int)
      {
        case 0:
          kink_splitting = Rhino.Geometry.RebuildCurveKinkSplitting.None;
          break;
        case 1:
          kink_splitting = Rhino.Geometry.RebuildCurveKinkSplitting.AtG1Changes;
          break;
        case 2:
          kink_splitting = Rhino.Geometry.RebuildCurveKinkSplitting.AtLargeG2Changes;
          break;
        case 3:
          kink_splitting = Rhino.Geometry.RebuildCurveKinkSplitting.AtMediumG2Changes;
          break;
        case 4:
          kink_splitting = Rhino.Geometry.RebuildCurveKinkSplitting.AtSmallG2Changes;
          break;

        default:
          kink_splitting = Rhino.Geometry.RebuildCurveKinkSplitting.None;
          break;
      }
      return kink_splitting;
    }


    /// <summary>
    /// The number of input curves.
    /// </summary>
    /// <since>9.0</since>
    public int InputCurveCount
    {
      get
      {
        IntPtr constPtr = ConstPointer();
        int i = GetInt(UnsafeNativeMethods.RebuildCurveOptionsInteger.ConstInputCurveCount);
        GC.KeepAlive(this);
        return i;
      }
    }


    /// <summary>
    /// The range of point counts in the input curves.
    /// InputPointCountRange.I = minimum input curve point count.
    /// InputPointCountRange.J = maximum input curve point count.
    /// </summary>
    /// <since>9.0</since>
    public Rhino.IndexPair InputPointCountRange
    {
      get
      {
        IntPtr constPtr = ConstPointer();
        int i = GetInt(UnsafeNativeMethods.RebuildCurveOptionsInteger.ConstInputPointCountRangeMin);
        int j = GetInt(UnsafeNativeMethods.RebuildCurveOptionsInteger.ConstInputPointCountRangeMax);
        GC.KeepAlive(this);
        return new Rhino.IndexPair(i, j);
      }
    }

    /// <summary>
    /// The range of degrees in the input curves.
    /// InputDegreeRange.I = minimum input curve degree.
    /// InputDegreeRange.J = maximum input curve degree.
    /// </summary>
    /// <since>9.0</since>
    public Rhino.IndexPair InputDegreeRange
    {
      get
      {
        IntPtr constPtr = ConstPointer();
        int i = GetInt(UnsafeNativeMethods.RebuildCurveOptionsInteger.ConstInputDegreeRangeMin);
        int j = GetInt(UnsafeNativeMethods.RebuildCurveOptionsInteger.ConstInputDegreeRangeMax);
        GC.KeepAlive(this);
        return new Rhino.IndexPair(i, j);
      }
    }


    /// <summary>
    /// The range of span counts in the input curves.
    /// InputDegreeRange.I = minimum input curve span count.
    /// InputDegreeRange.J = maximum input curve span count.
    /// </summary>
    /// <since>9.0</since>
    public Rhino.IndexPair InputSpanCountRange
    {
      get
      {
        IntPtr constPtr = ConstPointer();
        int i = GetInt(UnsafeNativeMethods.RebuildCurveOptionsInteger.ConstInputSpanCountRangeMin);
        int j = GetInt(UnsafeNativeMethods.RebuildCurveOptionsInteger.ConstInputSpanCountRangeMax);
        GC.KeepAlive(this);
        return new Rhino.IndexPair(i, j);
      }
    }


    /// <summary>
    /// The mimimum value a user interface may specify as a degree value.
    /// </summary>
    /// <since>9.0</since>
    public int MinimumDegreeLimit
    {
      get { return GetInt(UnsafeNativeMethods.RebuildCurveOptionsInteger.ConstMinimumDegreeLimit); }
    }

    /// <summary>
    /// The maximum value a user interface may specify as a degree value.
    /// </summary>
    /// <since>9.0</since>
    public int MaximumDegreeLimit
    {
      get { return GetInt(UnsafeNativeMethods.RebuildCurveOptionsInteger.ConstMaximumDegreeLimit); }
    }

    /// <summary>
    /// The degree the rebuit curves will have.
    /// Changing the degree can change other options. After changing the degre,
    /// user interface code must update all displayed values.
    /// </summary>
    /// <since>9.0</since>
    public int Degree
    {
      get { return GetInt(UnsafeNativeMethods.RebuildCurveOptionsInteger.Degree); }
      set { SetInt(UnsafeNativeMethods.RebuildCurveOptionsInteger.Degree, value); }
    }

    /// <summary>
    /// The mimimum value a user interface may specify as a point count value.
    /// </summary>
    /// <since>9.0</since>
    public int MinimumPointCountLimit
    {
      get { return GetInt(UnsafeNativeMethods.RebuildCurveOptionsInteger.ConstMinimumPointCountLimit); }
    }

    /// <summary>
    /// The maximum value a user interface may specify as a point count value.
    /// </summary>
    /// <since>9.0</since>
    public int MaximumPointCountLimit
    {
      get { return GetInt(UnsafeNativeMethods.RebuildCurveOptionsInteger.ConstMaximumPointCountLimit); }
    }

    /// <summary>
    /// Always sets the number of control points in the rebuilt curve.
    /// If PointCountRangePermitted is true or the minimum and maximum point counts are equal, that value is returned.
    /// Otherwise 0 is returned and you must use MinimumPointCount and MaximumPointCount to get the point count range.
    /// Put another way, when 0 is returned, a range of point counts has been specified and 
    /// you must use MinimumPointCount and MaximumPointCount to get the point count range.
    /// </summary>
    /// <since>9.0</since>
    public int PointCount
    {
      get { return GetInt(UnsafeNativeMethods.RebuildCurveOptionsInteger.PointCount); }
      set { SetInt(UnsafeNativeMethods.RebuildCurveOptionsInteger.PointCount, value); }
    }

    /// <summary>
    /// If user interface is permitted to set a range of point counts, then PointCountRangePermitted = true
    /// and SetPointCountRange() may be used to set the range. Otherwise point count ranges are not permitted\
    /// and user interface must use PointCount to set a single value.
    /// </summary>
    /// <since>9.0</since>
    public bool PointCountRangePermitted
    {
      get { return 1 == GetInt(UnsafeNativeMethods.RebuildCurveOptionsInteger.ConstPointCountRangesPermitted); }
    }

    /// <summary>
    /// The minimum point count. If the context permits ranges of point counts,
    /// then it is possible that MinimumPointCount &lt; MaximumPointCount. Otherwise
    /// MinimumPointCount = MaximumPointCount = PointCount.
    /// </summary>
    /// <since>9.0</since>
    public int MinimumPointCount
    {
      get { return GetInt(UnsafeNativeMethods.RebuildCurveOptionsInteger.ConstMinimumPointCount); }
    }

    /// <summary>
    /// The maximum point count. If the context permits ranges of point counts,
    /// then it is possible that MinimumPointCount &lt; MaximumPointCount. Otherwise
    /// MinimumPointCount = MaximumPointCount = PointCount.
    /// </summary>
    /// <since>9.0</since>
    public int MaximumPointCount
    {
      get { return GetInt(UnsafeNativeMethods.RebuildCurveOptionsInteger.ConstMaximumPointCount); }
    }

    /// <summary>
    /// If PointCountRangePermitted is true, then the user interface may use SetPointCountRange(min,max,tolerance)
    /// to specify a range of acceptable point counts. Otherwise using PointCount to specify a single point
    /// count value.
    /// </summary>
    /// <param name="minimum_point_count"></param>
    /// <param name="maximum_point_count"></param>
    /// <param name="tolerance">
    /// If tolerance &gt; 0 and there is a rebuilt curve in the point range with deviation &lt;= tolerance,
    /// the the curve with the smallest point count whose deviation &lt;= tolerance will be returned.
    /// Otherwise the curve with the smallest deviation is returned.
    /// </param>
    /// <since>9.0</since>
    public void SetPointCountRange(int minimum_point_count, int maximum_point_count, double tolerance)
    {
      IntPtr ptr = NonConstPointer();
      UnsafeNativeMethods.CRhinoRebuildCurveOptions_SetPointCountRange(ptr, minimum_point_count, maximum_point_count, tolerance);
      GC.KeepAlive(this);
    }

    /// <summary>
    /// When PointCountRangePermitted is true and MinimumPointCount &lt; MaximumPointCount,
    /// PointCountRangeTolerance specifies the desired maximum deviation between the target 
    /// and rebuilt curves. If there is a rebuilt curve in the point count range with
    /// deviation &lt;= PointCountRangeTolerance, then the rebuilt curve with the smallest
    /// point count whose deviation &lt;= tolerance is returned. Otherwise the curve
    /// with the smallest deviation is returned.
    /// Use SetPointCountRange(min,max,tol) to set PointCountRangeTolerance. 
    /// </summary>
    /// <since>9.0</since>
    public double PointCountRangeTolerance
    {
      get
      {
        IntPtr constPtr = ConstPointer();
        double a = UnsafeNativeMethods.CRhinoRebuildCurveOptions_PointCountRangeTolerance(constPtr);
        GC.KeepAlive(this);
        return a;
      }
    }

    /// <summary>
    /// When SubDFriendly is true, the degree is set to 3 and the ends of rebuilt open curves have zero 2nd derivative.
    /// These types of curves are useful for lofting into SubDs.
    /// </summary>
    /// <since>9.0</since>
    public bool SubDFriendly
    {
      get { return (1 == GetInt(UnsafeNativeMethods.RebuildCurveOptionsInteger.SubDFriendly)); }
      set { SetInt(UnsafeNativeMethods.RebuildCurveOptionsInteger.SubDFriendly, value ? 1 : 0); }
    }

    /// <summary>
    /// Returns true if the user interface may offer tangent matching as an option.
    /// </summary>
    /// <since>9.0</since>
    public bool TangentMatchingPermitted
    {
      get { return 1 == GetInt(UnsafeNativeMethods.RebuildCurveOptionsInteger.ConstTangentMatchingPermitted); }
    }

    /// <summary>
    /// When TangentMatchPermitted is true, TangentMatch is the active tangent mathing option.
    /// When TangentMatchPermitted is false, the user interface should not offer tangent matching.
    /// </summary>
    /// <since>9.0</since>
    public Rhino.Geometry.RebuildCurveTangentMatching TangentMatching
    {
      get { return IntToTangentMatch(GetInt(UnsafeNativeMethods.RebuildCurveOptionsInteger.TangentMatching)); }
      set { SetInt(UnsafeNativeMethods.RebuildCurveOptionsInteger.TangentMatching, TangentMatchingToInt(value)); }
    }

    /// <summary>
    /// True if the user interface is permitted to show the smoothing intensity option
    /// </summary>
    /// <since>9.0</since>
    public bool SmoothingPermitted
    {
      get { return (1 == GetInt(UnsafeNativeMethods.RebuildCurveOptionsInteger.ConstSmoothingPermitted)); }
    }

    /// <summary>
    /// When SmoothingPermitted is true, SmoothingIntensity sets and gets the intensity of forcing the
    /// 2nd point of contiguous triples of control points to be halfway between the 1st and 3rd point.
    /// </summary>
    /// <since>9.0</since>
    public Rhino.Geometry.RebuildCurveIntensity SmoothingIntensity
    {
      get { return IntToIntensity(GetInt(UnsafeNativeMethods.RebuildCurveOptionsInteger.SmoothingIntensity)); }
      set { SetInt(UnsafeNativeMethods.RebuildCurveOptionsInteger.SmoothingIntensity, IntensityToInt(value)); }
    }

    /// <summary>
    /// True if the user interface is permitted to show the uniformity intensity option
    /// </summary>
    /// <since>9.0</since>
    public bool UniformityPermitted
    {
      get { return (1 == GetInt(UnsafeNativeMethods.RebuildCurveOptionsInteger.ConstUniformityPermitted)); }
    }

    /// <summary>
    /// When UniformityPermitted is true, UniformityIntensity sets and gets the intensity of forcing the
    /// the distance between control points to be equal.
    /// </summary>
    /// <since>9.0</since>
    public Rhino.Geometry.RebuildCurveIntensity UniformityIntensity
    {
      get { return IntToIntensity(GetInt(UnsafeNativeMethods.RebuildCurveOptionsInteger.UniformityIntensity)); }
      set { SetInt(UnsafeNativeMethods.RebuildCurveOptionsInteger.UniformityIntensity, IntensityToInt(value)); }
    }

    /// <summary>
    /// True if input target 
    /// curves should be deleted when possible.
    /// </summary>
    /// <since>9.0</since>
    public bool DeleteInputCurves
    {
      get { return (1 == GetInt(UnsafeNativeMethods.RebuildCurveOptionsInteger.DeleteInputCurves)); }
      set { SetInt(UnsafeNativeMethods.RebuildCurveOptionsInteger.DeleteInputCurves, value ? 1 : 0); }
    }
    /// <summary>
    /// True if output rebuilt curves should be on the current layer.
    /// False if output rebuild curves should be on the target curve's layer.
    /// </summary>
    /// <since>9.0</since>
    public bool OutputToCurrentLayer
    {
      get { return (1 == GetInt(UnsafeNativeMethods.RebuildCurveOptionsInteger.OutputToCurrentLayer)); }
      set { SetInt(UnsafeNativeMethods.RebuildCurveOptionsInteger.OutputToCurrentLayer, value ? 1 : 0); }
    }
    /// <summary>
    /// True if the control points of the rebuilt curves are displayed
    /// during the dynamic preview.
    /// </summary>
    /// <since>9.0</since>
    public bool ShowControlPoints
    {
      get { return (1 == GetInt(UnsafeNativeMethods.RebuildCurveOptionsInteger.ShowControlPoints)); }
      set { SetInt(UnsafeNativeMethods.RebuildCurveOptionsInteger.ShowControlPoints, value ? 1 : 0); }
    }
    /// <summary>
    /// True if the user interface is permitted to show the kink splitting options
    /// </summary>
    /// <since>9.0</since>
    public bool KinkSplittingPermitted
    {
      get { return (1 == GetInt(UnsafeNativeMethods.RebuildCurveOptionsInteger.ConstKinkSplittingPermitted)); }
      set { SetInt(UnsafeNativeMethods.RebuildCurveOptionsInteger.ConstKinkSplittingPermitted, value ? 1 : 0); }
    }

    /// <since>9.0</since>
    public Rhino.Geometry.RebuildCurveKinkSplitting KinkSplitting
    {
      get { return (IntToKinkSplitting(GetInt(UnsafeNativeMethods.RebuildCurveOptionsInteger.KinkSplitting))); }
      set { SetInt(UnsafeNativeMethods.RebuildCurveOptionsInteger.KinkSplitting, KinkSplittingToInt(value)); }
    }

    /// <summary>
    /// If the KinkSplitting is on, then KinkAngleDegrees is the angle in degrees used to
    /// detect discontinuous changes in target curve tangents and curvature directions.
    /// </summary>
    /// <since>9.0</since>
    public double KinkAngleDegrees
    {
      get {
        IntPtr constPtr = ConstPointer();
        double a = UnsafeNativeMethods.CRhinoRebuildCurveOptions_KinkAngleDegrees(constPtr);
        GC.KeepAlive(this);
        return a; 
      }
      set
      {
        IntPtr ptr = NonConstPointer();
        UnsafeNativeMethods.CRhinoRebuildCurveOptions_SetKinkAngleDegrees(ptr, value);
        GC.KeepAlive(this);
      }
    }

    /// <summary>
    /// When the rebuild options are presented in a dialog, the initial rebuild
    /// calculation is deferred so that the dialog appears immediately instead of after
    /// that calculation finishes. The dialog calls this one time, after it is on the
    /// screen, and the initial calculation and the first dynamic preview happen then.
    /// The dialog must update itself from these options when this returns. The
    /// calculation fills in the current output properties and it can increase the point
    /// count when kink splitting needs more points than the current settings specify.
    /// Calling this when no initial calculation is pending does nothing.
    /// </summary>
    /// <since>9.0</since>
    public void CalculateInitialPreview()
    {
      IntPtr ptr = NonConstPointer();
      UnsafeNativeMethods.CRhinoRebuildCurveOptions_CalculateInitialPreview(ptr);
      GC.KeepAlive(this);
    }

    /// <summary>
    /// The maximum deviation between a target curve and the corresponding rebuilt curve
    /// calculated using the current properties.
    /// </summary>
    /// <since>9.0</since>
    public double CurrentOutputMaximumDeviation
    {
      get
      {
        IntPtr constPtr = ConstPointer();
        double a = UnsafeNativeMethods.CRhinoRebuildCurveOptions_CurrentOutputMaximumDeviation(constPtr);
        GC.KeepAlive(this);
        return a;
      }
    }

    /// <summary>
    /// The range of point counts in the rebuilt curves calculated using the current properties.
    /// When multiple target curves are being rebuilt with variable point counts or kink splittings,
    /// the resulting rebuilt curves can have a range of point counts.
    /// CurrentOutputPointCountRange.I = minimum point count.
    /// CurrentOutputPointCountRange.J = maximum point count.
    /// </summary>
    /// <since>9.0</since>
    public Rhino.IndexPair CurrentOutputPointCountRange
    {
      get
      {
        IntPtr constPtr = ConstPointer();
        int i = GetInt(UnsafeNativeMethods.RebuildCurveOptionsInteger.ConstCurrentOutputPointCountRangeMin);
        int j = GetInt(UnsafeNativeMethods.RebuildCurveOptionsInteger.ConstCurrentOutputPointCountRangeMax);
        GC.KeepAlive(this);
        return new Rhino.IndexPair(i, j);
      }
    }

    /// <summary>
    /// The range of span counts in the rebuilt curves calculated using the current properties.
    /// When multiple target curves are being rebuilt with variable point counts or kink splittings,
    /// the resulting rebuilt curves can have a range of span counts.
    /// CurrentOutputSpanCountRange.I = minimum span count.
    /// CurrentOutputSpanCountRange.J = maximum span count.
    /// NOTE WELL: 
    /// Do not confuse span counts with point counts.
    /// The span count is always &lt;= PointCount - Degree and is the number of spans in the 
    /// rebuilt NURBS curves.
    /// </summary>
    /// <since>9.0</since>
    public Rhino.IndexPair CurrentOutputSpanCountRange
    {
      get
      {
        IntPtr constPtr = ConstPointer();
        int i = GetInt(UnsafeNativeMethods.RebuildCurveOptionsInteger.ConstCurrentOutputSpanCountRangeMin);
        int j = GetInt(UnsafeNativeMethods.RebuildCurveOptionsInteger.ConstCurrentOutputSpanCountRangeMax);
        GC.KeepAlive(this);
        return new Rhino.IndexPair(i, j);
      }
    }

    /// <summary>
    /// Serial number of the Rhino document containing the target curves.
    /// </summary>
    /// <since>9.0</since>
    public int RhinoDocumentSerialNumber
    {
      get { return GetInt(UnsafeNativeMethods.RebuildCurveOptionsInteger.ConstRhinoDocSerialNumber); }
    }
  }
}
#endif
