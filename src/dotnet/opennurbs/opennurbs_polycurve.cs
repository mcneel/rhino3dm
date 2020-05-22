using System;
using Rhino.Display;
using System.Runtime.Serialization;
using Rhino.Runtime;

namespace Rhino.Geometry
{
  /// <summary>
  /// Represents a curve that is the result of joining several (possibly different)
  /// types of curves.
  /// </summary>
  [Serializable]
  public class PolyCurve : Curve
  {
    #region constructors
    internal PolyCurve(IntPtr ptr, object parent, int subobject_index)
      :base(ptr, parent, subobject_index)
    {
    }
    internal PolyCurve(PolyCurve other)
    {
      IntPtr pOther = IntPtr.Zero;
      if (null != other)
        pOther = other.ConstPointer();
      IntPtr ptr = UnsafeNativeMethods.ON_PolyCurve_New(pOther);
      ConstructNonConstObject(ptr);
      GC.KeepAlive(other);
    }

    /// <summary>
    /// Protected constructor for internal use.
    /// </summary>
    /// <param name="info">Serialization data.</param>
    /// <param name="context">Serialization stream.</param>
    protected PolyCurve(SerializationInfo info, StreamingContext context)
      : base (info, context)
    {
    }

    internal override GeometryBase DuplicateShallowHelper()
    {
      return new PolyCurve(IntPtr.Zero, null, -1);
    }
    /// <summary>
    /// Initializes a new, empty polycurve.
    /// </summary>
    /// <since>5.0</since>
    public PolyCurve()
    {
      IntPtr ptr = UnsafeNativeMethods.ON_PolyCurve_New(IntPtr.Zero);
      ConstructNonConstObject(ptr);
    }

    /// <summary>
    /// Duplicates this polycurve.
    /// <para>When not overridden in a derived class, this calls <see cref="DuplicatePolyCurve"/>.</para>
    /// </summary>
    /// <returns>An exact duplicate of this curve.</returns>
    /// <since>5.0</since>
    public override GeometryBase Duplicate()
    {
      return DuplicatePolyCurve();
    }
    /// <summary>
    /// Duplicates this polycurve.
    /// <para>This is the same as <see cref="Duplicate"/>.</para>
    /// </summary>
    /// <returns>An exact duplicate of this curve.</returns>
    /// <since>5.0</since>
    public PolyCurve DuplicatePolyCurve()
    {
      return new PolyCurve(this);
    }

    #endregion

    #region properties
    const int idxIsNested = 0;

    /// <summary>
    /// Gets the number of segments that make up this Polycurve.
    /// </summary>
    /// <since>5.0</since>
    public int SegmentCount
    {
      get
      {
        IntPtr ptr = ConstPointer();
        return UnsafeNativeMethods.ON_PolyCurve_Count(ptr);
      }
    }

    /// <summary>
    /// Gets the segment curve at the given index.
    /// </summary>
    /// <param name="index">Index of segment to retrieve.</param>
    /// <returns>The segment at the given index or null on failure.</returns>
    /// <since>5.0</since>
    public Curve SegmentCurve(int index)
    {
      IntPtr ptr = ConstPointer();
      IntPtr pCurve = UnsafeNativeMethods.ON_PolyCurve_SegmentCurve(ptr, index);
      return GeometryBase.CreateGeometryHelper(pCurve, this, index) as Curve;
    }

/*

    //[skipping]
    //  ON_Curve* FirstSegmentCurve() const; // returns NULL if count = 0
    //  ON_Curve* LastSegmentCurve() const;  // returns NULL if count = 0

    /// <summary>
    /// Constructs an array containing all polycurve segments.
    /// </summary>
    /// <returns>An array of all the segments that make up this PolyCurve.</returns>
    public Curve[] SegmentCurves()
    {
      Runtime.INTERNAL_CurveArray curves = new Runtime.INTERNAL_CurveArray();
      IntPtr ptr = ConstPointer();
      IntPtr pArray = curves.NonConstPointer();
      UnsafeNativeMethods.ON_PolyCurve_SegmentCurves(ptr, pArray);
      Curve[] rc = curves.ToArray(true);
      if (rc != null)
      {
        for (int i = 0; i < rc.Length; i++)
        {
          Curve curve = rc[i];
          if (curve != null)
            curve.SetParentPolyCurve(this);
        }
      }
      return rc;
    }
*/
    /// <summary>
    /// Gets a value indicating whether or not a PolyCurve contains nested PolyCurves.
    /// </summary>
    /// <seealso cref="RemoveNesting"/>
    /// <since>5.0</since>
    public bool IsNested
    {
      get
      {
        IntPtr ptr = ConstPointer();
        return UnsafeNativeMethods.ON_PolyCurve_GetBool(ptr, idxIsNested);
      }
    }

    /// <summary>
    /// This is a quick way to see if the curve has gaps between the sub curve segments. 
    /// </summary>
    /// <since>5.0</since>
    public bool HasGap
    {
      get
      {
        IntPtr ptr = ConstPointer();
        return (UnsafeNativeMethods.ON_PolyCurve_HasGap(ptr) > 0);
      }
    }


    #endregion

    #region methods
    const int idxRemoveNestingEx = 1;

    /// <summary>
    /// Explodes nested polycurve segments and reconstructs this curve from the shattered remains. 
    /// The result will have not have any PolyCurves as segments but it will have identical 
    /// locus and parameterization.
    /// </summary>
    /// <returns>
    /// true if any nested PolyCurve was found and absorbed, false if no PolyCurve segments could be found.
    /// </returns>
    /// <seealso cref="IsNested"/>
    /// <since>5.0</since>
    public bool RemoveNesting()
    {
      // check to see if this curve is nested before forcing it to be non-const
      if (!IsNested)
        return false;
      IntPtr ptr = NonConstPointer();
      return UnsafeNativeMethods.ON_PolyCurve_GetBool(ptr, idxRemoveNestingEx);
    }

    /// <summary>
    /// Explodes this PolyCurve into a list of Curve segments. This will <b>not explode</b> nested polycurves. 
    /// Call <see cref="RemoveNesting"/> first if you need all individual segments.
    /// </summary>
    /// <returns>An array of polycurve segments.</returns>
    /// <since>5.0</since>
    [ConstOperation]
    public Curve[] Explode()
    {
      Runtime.InteropWrappers.SimpleArrayCurvePointer curves = new Runtime.InteropWrappers.SimpleArrayCurvePointer();
      IntPtr pConstThis = ConstPointer();
      IntPtr pCurveArray = curves.NonConstPointer();
      UnsafeNativeMethods.ON_PolyCurve_SegmentCurves(pConstThis, pCurveArray);

      int count = UnsafeNativeMethods.ON_CurveArray_Count(pCurveArray);
      Curve[] rc = new Curve[count];
      for (int i = 0; i < count; i++)
      {
        IntPtr pConstSegmentCurve = UnsafeNativeMethods.ON_CurveArray_Get(pCurveArray, i);
        if (IntPtr.Zero == pConstSegmentCurve)
          continue;
        IntPtr pNewCurve = UnsafeNativeMethods.ON_Curve_DuplicateCurve(pConstSegmentCurve);
        rc[i] = GeometryBase.CreateGeometryHelper(pNewCurve, null) as Curve;
      }
      curves.Dispose();
      return rc;
    }

    //[skipping]
    //  void Reserve( int ); // make sure capacity is at least the specified count
    //  // ON_Curve pointers added with Prepend(), Append(), PrependAndMatch(), AppendANdMatch(),and Insert() are deleted
    //  // by ~ON_PolyCurve(). Use ON_CurveProxy( ON_Curve*) if you want
    //  // the original curve segment to survive ~ON_PolyCurve().
    //  BOOL Prepend( ON_Curve* ); // Prepend curve.
    //  BOOL Append( ON_Curve* );  // Append curve.
    //  BOOL Insert( 
    //           int, // segment_index,
    //           ON_Curve*
    //           );
    //  //PrependAndMatch() and AppendAndMatch() return FALSE if this->IsCLosed() or 
    //  //this->Count() > 0 and curve is closed
    //  BOOL PrependAndMatch(ON_Curve*); //Prepend and match end of curve to start of polycurve
    //  BOOL AppendAndMatch(ON_Curve*);  //Append and match start of curve to end of polycurve

    /// <summary>
    /// Appends and matches the start of the line to the end of polycurve. 
    /// This function will fail if the polycurve is closed.
    /// </summary>
    /// <param name="line">Line segment to append.</param>
    /// <returns>true on success, false on failure.</returns>
    /// <since>5.0</since>
    public bool Append(Line line)
    {
      if (!line.IsValid) { return false; }
      return Append(new LineCurve(line));
    }
    /// <summary>
    /// Appends and matches the start of the arc to the end of polycurve. 
    /// This function will fail if the polycurve is closed or if SegmentCount > 0 and the arc is closed.
    /// </summary>
    /// <param name="arc">Arc segment to append.</param>
    /// <returns>true on success, false on failure.</returns>
    /// <since>5.0</since>
    public bool Append(Arc arc)
    {
      IntPtr ptr = NonConstPointer();
      bool rc = UnsafeNativeMethods.ON_PolyCurve_AppendAndMatch(ptr, ref arc);
      return rc;
    }
    /// <summary>
    /// Appends and matches the start of the curve to the end of polycurve. 
    /// This function will fail if the PolyCurve is closed or if SegmentCount > 0 and the new segment is closed.
    /// </summary>
    /// <param name="curve">Segment to append.</param>
    /// <returns>true on success, false on failure.</returns>
    /// <since>5.0</since>
    public bool Append(Curve curve)
    {
      if (null == curve)
        return false;
      IntPtr ptr = NonConstPointer();
      IntPtr pCurve = curve.ConstPointer();
      bool rc = UnsafeNativeMethods.ON_PolyCurve_AppendAndMatch2(ptr, pCurve);
      GC.KeepAlive(curve);
      return rc;
    }

    /// <summary>
    /// Appends the curve to the polycurve without changing the new segment's geometry. 
    /// This function will fail if the PolyCurve is closed or if SegmentCount > 0 and the new segment is closed.
    /// </summary>
    /// <param name="curve">Segment to append.</param>
    /// <returns>true on success, false on failure.</returns>
    /// <since>6.0</since>
    public bool AppendSegment(Curve curve)
    {
      if(null == curve)
        return false;
      IntPtr ptr = NonConstPointer();
      IntPtr pCurve = curve.ConstPointer();
      bool rc = UnsafeNativeMethods.ON_PolyCurve_Append(ptr, pCurve);
      GC.KeepAlive(curve);
      return rc;
    }

    /// <summary>
    /// Converts a polycurve parameter to a segment curve parameter.
    /// </summary>
    /// <param name="polycurveParameter">Parameter on PolyCurve to convert.</param>
    /// <returns>
    /// Segment curve evaluation parameter or UnsetValue if the 
    /// segment curve parameter could not be computed.
    /// </returns>
    /// <since>5.0</since>
    [ConstOperation]
    public double SegmentCurveParameter(double polycurveParameter)
    {
      IntPtr ptr = ConstPointer();
      return UnsafeNativeMethods.ON_PolyCurve_SegmentCurveParameter(ptr, polycurveParameter);
    }

    /// <summary>
    /// Converts a segment curve parameter to a polycurve parameter.
    /// </summary>
    /// <param name="segmentIndex">Index of segment.</param>
    /// <param name="segmentCurveParameter">Parameter on segment.</param>
    /// <returns>
    /// Polycurve evaluation parameter or UnsetValue if the polycurve curve parameter could not be computed.
    /// </returns>
    /// <since>5.0</since>
    [ConstOperation]
    public double PolyCurveParameter(int segmentIndex, double segmentCurveParameter)
    {
      IntPtr ptr = ConstPointer();
      return UnsafeNativeMethods.ON_PolyCurve_PolyCurveParameter(ptr, segmentIndex, segmentCurveParameter);
    }

    /// <summary>
    /// Returns the polycurve sub-domain assigned to a segment curve.
    /// </summary>
    /// <param name="segmentIndex">Index of segment.</param>
    /// <returns>
    /// The polycurve sub-domain assigned to a segment curve. 
    /// Returns Interval.Unset if segment_index &lt; 0 or segment_index >= Count().
    /// </returns>
    /// <since>5.0</since>
    [ConstOperation]
    public Interval SegmentDomain(int segmentIndex)
    {
      Interval rc = new Interval();
      IntPtr ptr = ConstPointer();
      UnsafeNativeMethods.ON_PolyCurve_SegmentDomain(ptr, segmentIndex, ref rc);
      return rc;
    }

    /// <summary>
    /// Finds the segment used for evaluation at polycurve_parameter.
    /// </summary>
    /// <param name="polycurveParameter">Parameter on polycurve for segment lookup.</param>
    /// <returns>
    /// Index of the segment used for evaluation at polycurve_parameter. 
    /// If polycurve_parameter &lt; Domain.Min(), then 0 is returned. 
    /// If polycurve_parameter > Domain.Max(), then Count()-1 is returned.
    /// </returns>
    /// <since>5.0</since>
    [ConstOperation]
    public int SegmentIndex(double polycurveParameter)
    {
      IntPtr ptr = ConstPointer();
      return UnsafeNativeMethods.ON_PolyCurve_SegmentIndex(ptr, polycurveParameter);
    }

    /// <summary>Finds the segments that overlap the Polycurve sub domain.</summary>
    /// <param name="subdomain">Domain on this PolyCurve.</param>
    /// <param name="segmentIndex0">
    /// Index of first segment that overlaps the sub-domain.
    /// </param>
    /// <param name="segmentIndex1">
    /// Index of last segment that overlaps the sub-domain. Note that segmentIndex0 &lt;= i &lt; segmentIndex1.
    /// </param>
    /// <returns>Number of segments that overlap the sub-domain.</returns>
    /// <since>5.0</since>
    [ConstOperation]
    public int SegmentIndexes(Interval subdomain, out int segmentIndex0, out int segmentIndex1)
    {
      segmentIndex0 = -1;
      segmentIndex1 = -1;
      IntPtr ptr = ConstPointer();
      return UnsafeNativeMethods.ON_PolyCurve_SegmentIndexes(ptr, subdomain, ref segmentIndex0, ref segmentIndex1);
    }
    #endregion

#if RHINO_SDK
    /// <summary>
    /// Removes any nesting of polycurves. If this polycurve has just a single segment, the segment is returned.
    /// If, after nest removal, there are adjacent segments which are polylines, they are combined into a single polyline.
    /// The new curve may have a different domain from this polycurve. If the start and end segments of a closed input are polylines,
    /// the result may have a different seam location since the start and end segments will be combined.
    /// </summary>
    /// <returns>A new curve that is not necessarily a polycurve if successful, null otherwise. </returns>
    /// <seealso cref="RemoveNesting"/>
    /// <since>7.0</since>
    public Curve CleanUp()
    {
      IntPtr ptr_const_this = ConstPointer();
      IntPtr ptr = UnsafeNativeMethods.RHC_RhinoCleanUpPolyCurve(ptr_const_this);
      return GeometryBase.CreateGeometryHelper(ptr, null) as Curve;
    }

    private IntPtr CurveDisplay()
    {
      if (IntPtr.Zero == m_pCurveDisplay)
      {
        IntPtr pThis = ConstPointer();
        m_pCurveDisplay = UnsafeNativeMethods.CurveDisplay_FromPolyCurve(pThis);
      }
      return m_pCurveDisplay;
    }

    internal override void Draw(DisplayPipeline pipeline, System.Drawing.Color color, int thickness)
    {
      IntPtr pDisplayPipeline = pipeline.NonConstPointer();
      int argb = color.ToArgb();
      IntPtr pCurveDisplay = CurveDisplay();
      UnsafeNativeMethods.CurveDisplay_Draw(pCurveDisplay, pDisplayPipeline, argb, thickness);
    }
#endif

    //[skipping]
    //  ON_Curve* operator[](int) const;

    //  BOOL Remove(); // delete last segment and reduce count by 1
    //  BOOL Remove( int ); // delete specified segment and reduce count by 1

    //  // Use the HarvestSegment() function when you want to prevent a
    //  // segment from being destroyed by ~ON_PolyCurve().  HarvestSegment()
    //  // replaces the polycurve segment with a NULL.  Count() and parameter
    //  // information remains unchanged.
    //  ON_Curve* HarvestSegment( int );

    //[skipping]
    //  void SetSegment(int index, ON_Curve* crv);
    //  bool SetParameterization( const double* t );
    //  bool ParameterSearch(double t, int& index, bool bEnableSnap) const;

    //[skipping]
    //public double[] SegmentParameters()
    //{
    //}
  }
}
