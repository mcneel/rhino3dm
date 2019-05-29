using System;
using System.Collections.Generic;
using Rhino.Runtime;

namespace Rhino.Geometry.Intersect
{
  /// <summary>
  /// Provides all the information for a single Curve Intersection event.
  /// </summary>
  public class IntersectionEvent
  {
    #region members
    internal int m_type; // 1 == ccx_point
                         // 2 == ccx_overlap
                         // 3 == csx_point
                         // 4 == csx_overlap

    // using same internal names as OpenNURBS so we can keep things straight
    internal Point3d m_A0; //Point on A (or first point on overlap)
    internal Point3d m_A1; //Point on A (or last point on overlap)
    internal Point3d m_B0; //Point on B (or first point on overlap)
    internal Point3d m_B1; //Point on B (or last point on overlap)
    internal double m_a0; //Parameter on A (or first parameter of overlap)
    internal double m_a1; //Parameter on A (or last parameter of overlap)
    internal double m_b0; //Parameter on B (or first parameter of overlap) (or first U parameter on surface)
    internal double m_b1; //Parameter on B (or last parameter of overlap) (or last U parameter on surface)
    internal double m_b2; //First V parameter on surface
    internal double m_b3; //Last V parameter on surface
    #endregion

    #region properties
    /// <summary>
    /// All curve intersection events are either a single point or an overlap.
    /// </summary>
    public bool IsPoint
    {
      get { return (1 == m_type || 3 == m_type); }
    }

    /// <summary>
    /// All curve intersection events are either a single point or an overlap.
    /// </summary>
    /// <example>
    /// <code source='examples\vbnet\ex_curvesurfaceintersect.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_curvesurfaceintersect.cs' lang='cs'/>
    /// <code source='examples\py\ex_curvesurfaceintersect.py' lang='py'/>
    /// </example>
    public bool IsOverlap
    {
      get { return !IsPoint; }
    }

    /// <summary>
    /// Gets the point on Curve A where the intersection occured. 
    /// If the intersection type is overlap, then this will return the 
    /// start of the overlap region.
    /// </summary>
    public Point3d PointA
    {
      get { return m_A0; }
    }
    /// <summary>
    /// Gets the end point of the overlap on Curve A. 
    /// If the intersection type is not overlap, this value is meaningless.
    /// </summary>
    public Point3d PointA2
    {
      get { return m_A1; }
    }

    /// <summary>
    /// Gets the point on Curve B (or Surface B) where the intersection occured. 
    /// If the intersection type is overlap, then this will return the 
    /// start of the overlap region.
    /// </summary>
    public Point3d PointB
    {
      get { return m_B0; }
    }
    /// <summary>
    /// Gets the end point of the overlap on Curve B (or Surface B). 
    /// If the intersection type is not overlap, this value is meaningless.
    /// </summary>
    public Point3d PointB2
    {
      get { return m_B1; }
    }

    /// <summary>
    /// Gets the parameter on Curve A where the intersection occured. 
    /// If the intersection type is overlap, then this will return the 
    /// start of the overlap region.
    /// </summary>
    public double ParameterA
    {
      get { return m_a0; }
    }
    /// <summary>
    /// Gets the parameter on Curve B where the intersection occured. 
    /// If the intersection type is overlap, then this will return the 
    /// start of the overlap region.
    /// </summary>
    public double ParameterB
    {
      get { return m_b0; }
    }

    /// <summary>
    /// Gets the interval on curve A where the overlap occurs. 
    /// If the intersection type is not overlap, this value is meaningless.
    /// </summary>
    public Interval OverlapA
    {
      get { return new Interval(m_a0, m_a1); }
    }
    /// <summary>
    /// Gets the interval on curve B where the overlap occurs. 
    /// If the intersection type is not overlap, this value is meaningless.
    /// </summary>
    public Interval OverlapB
    {
      get { return new Interval(m_b0, m_b1); }
    }

    /// <summary>
    /// If this instance records a Curve|Surface intersection event, 
    /// <i>and</i> the intersection type is <b>point</b>, then use this function 
    /// to get the U and V parameters on the surface where the intersection occurs.
    /// </summary>
    /// <param name="u">Parameter on surface u direction where the intersection occurs.</param>
    /// <param name="v">Parameter on surface v direction where the intersection occurs.</param>
    [ConstOperation]
    public void SurfacePointParameter(out double u, out double v)
    {
      // ?? should we throw exceptions for calling this with ccx intersection types
      if (m_type < 3)
      {
        u = v = 0.0;
      }
      else
      {
        u = m_b0;
        v = m_b1;
      }
    }
    /// <summary>
    /// If this instance records a Curve|Surface intersection event, 
    /// <i>and</i> the intersection type if <b>overlap</b>, then use this function 
    /// to get the U and V domains on the surface where the overlap occurs.
    /// </summary>
    /// <param name="uDomain">Domain along surface U direction for overlap event.</param>
    /// <param name="vDomain">Domain along surface V direction for overlap event.</param>
    [ConstOperation]
    public void SurfaceOverlapParameter(out Interval uDomain, out Interval vDomain)
    {
      if( m_type < 3 )
      {
        uDomain = new Interval(0,0);
        vDomain = new Interval(0,0);
      }
      else
      {
        uDomain = new Interval(m_b0, m_b1);
        vDomain = new Interval(m_b2, m_b3);
      }
    }
    #endregion
  }

#if RHINO_SDK
  /// <summary>
  /// Maintains an ordered list of Curve Intersection results.
  /// </summary>
  public class CurveIntersections : IDisposable, IList<IntersectionEvent>
  {
    #region members
    IntPtr m_ptr; //ON_SimpleArray<ON_X_EVENT>
    IntersectionEvent[] m_events; // = null; initialized by runtime
    int m_count;
    #endregion

    #region constructor
    internal static CurveIntersections Create(IntPtr pIntersectionArray)
    {
      if (IntPtr.Zero == pIntersectionArray)
        return null;
      int count = UnsafeNativeMethods.ON_Intersect_IntersectArrayCount(pIntersectionArray);
      return new CurveIntersections(pIntersectionArray, count);
    }

    /// <summary>
    /// Constructor.
    /// </summary>
    CurveIntersections(IntPtr ptr, int count)
    {
      m_ptr = ptr;
      m_count = count;
      m_events = new IntersectionEvent[count];
    }

    /// <summary>
    /// Destructor.
    /// </summary>
    ~CurveIntersections()
    {
      Dispose(false);
    }

    /// <summary>
    /// Actively reclaims unmanaged resources that this instance uses.
    /// </summary>
    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    /// <summary>
    /// For derived class implementers.
    /// <para>This method is called with argument true when class user calls Dispose(), while with argument false when
    /// the Garbage Collector invokes the finalizer, or Finalize() method.</para>
    /// <para>You must reclaim all used unmanaged resources in both cases, and can use this chance to call Dispose on disposable fields if the argument is true.</para>
    /// <para>Also, you must call the base virtual method within your overriding method.</para>
    /// </summary>
    /// <param name="disposing">true if the call comes from the Dispose() method; false if it comes from the Garbage Collector finalizer.</param>
    protected virtual void Dispose(bool disposing)
    {
      if (IntPtr.Zero != m_ptr)
      {
        UnsafeNativeMethods.ON_Intersect_IntersectArrayDelete(m_ptr);
        m_ptr = IntPtr.Zero;
        m_count = 0;
        m_events = null;
      }
    }
    #endregion

    #region properties
    /// <summary>
    /// Gets the number of recorded intersection events.
    /// </summary>
    public int Count
    {
      get { return m_count; }
    }

    /// <summary>
    /// Gets the intersection event data at the given index.
    /// </summary>
    /// <param name="index">Index of intersection event to retrieve.</param>
    public IntersectionEvent this[int index]
    {
      get
      {
        if (index < 0) { throw new IndexOutOfRangeException("index must be equal to or larger than zero"); }
        if (index >= m_count) { throw new IndexOutOfRangeException("index must be less than the size of the collection"); }

        if (m_events[index] == null)
        {
          IntersectionEvent x;
          if (!CacheCurveData(index, out x)) { return null; } // This really should never happen.
          m_events[index] = x;
        }

        return m_events[index];
      }
    }
    internal bool CacheCurveData(int index, out IntersectionEvent x)
    {
      x = new IntersectionEvent();
      IntPtr ptr = ConstPointer();
      return UnsafeNativeMethods.ON_Intersect_CurveIntersectData(ptr, index,
        ref x.m_type, ref x.m_A0, ref x.m_A1, ref x.m_B0, ref x.m_B1,
        ref x.m_a0, ref x.m_a1, ref x.m_b0, ref x.m_b1, ref x.m_b2, ref x.m_b3);
    }
    #endregion

    #region methods
    private IntPtr ConstPointer() { return m_ptr; }

    /// <summary>
    /// Copies all intersection results into another array, departing at an index in the target array.
    /// </summary>
    /// <param name="array">The target array. This value cannot be null.</param>
    /// <param name="arrayIndex">Zero-based index in which to start the copy.</param>
    /// <exception cref="System.ArgumentNullException">If array is null.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">If arrayIndex is less than 0.</exception>
    /// <exception cref="System.ArgumentException">If array is multi-dimensional; or if arrayIndex is
    /// larger than or equal to the length of this collection; or this collection contains more 
    /// IntersectionEvents than the ones that can be stored in array after and including arrayIndex.
    /// </exception>
    public void CopyTo(IntersectionEvent[] array, int arrayIndex)
    {
      if (array == null)
        throw new ArgumentNullException("array");

      if (arrayIndex < 0)
        throw new ArgumentOutOfRangeException("arrayIndex", "The array index must be larger than 0.");

      if (array.Rank != 1)
        throw new ArgumentException("Array rank must be 1, but this array is multi-dimensional.", "array");

      if (arrayIndex >= array.Length ||
          Count + arrayIndex > array.Length)
      {
        throw new ArgumentException("arrayIndex");
      }

      for (int i = 0; i < Count; i++)
      {
        array[arrayIndex++] = this[i];
      }
    }

    /// <summary>
    /// Returns an enumerator that is capable of yielding all IntersectionEvents in the collection.
    /// </summary>
    /// <returns>The constructed enumerator.</returns>
    public IEnumerator<IntersectionEvent> GetEnumerator()
    {
      for (int i = 0; i < Count; i++)
      {
        yield return this[i];
      }
    }
    #endregion

    #region explicit implementations of IList<IntersectionEvent> and its base types
      //that did not seem necessary in normal usage

    /// <summary>
    /// Gets the intersection event data at the given index; setting always throws an exception.
    /// </summary>
    /// <param name="index">Index of intersection event to retrieve.</param>
    /// <returns>The intersection event.</returns>
    /// <exception cref="System.NotSupportedException">NotSupportedException is always thrown when
    /// setting this indexer.</exception>
    IntersectionEvent IList<IntersectionEvent>.this[int index]
    {
      get
      {
        return this[index];
      }
      set
      {
        throw new NotSupportedException("This collection is read-only.");
      }
    }

    /// <summary>
    /// Determines the index of an IntersectionEvent.
    /// </summary>
    /// <param name="item">The IntersectionEvent to be found.</param>
    /// <returns>The index in case the IntersectionEvent was found; -1 otherwise.</returns>
    int IList<IntersectionEvent>.IndexOf(IntersectionEvent item)
    {
      for (int i = 0; i < Count; i++)
      {
        if (item == this[i])
          return i;
      }
      return -1;
    }

    /// <summary>
    /// Inserting is not supported and this method always throws NotSupportedException.
    /// </summary>
    /// <param name="index">Inserting is not supported and this value is ignored.</param>
    /// <param name="item">Inserting is not supported and this value is ignored.</param>
    /// <exception cref="System.NotSupportedException">NotSupportedException is always thrown.</exception>
    void IList<IntersectionEvent>.Insert(int index, IntersectionEvent item)
    {
      throw new NotSupportedException("This list is read-only.");
    }

    /// <summary>
    /// Removal is not supported and this method always throws NotSupportedException.
    /// </summary>
    /// <param name="index">Removal is not supported and this value is ignored.</param>
    /// <exception cref="System.NotSupportedException">NotSupportedException is always thrown.</exception>
    void IList<IntersectionEvent>.RemoveAt(int index)
    {
      throw new NotSupportedException("This list is read-only.");
    }

    /// <summary>
    /// Addition is not supported and this method always throws NotSupportedException.
    /// </summary>
    /// <param name="item">Addition is not supported and this value is ignored.</param>
    /// <exception cref="System.NotSupportedException">NotSupportedException is always thrown.</exception>
    void ICollection<IntersectionEvent>.Add(IntersectionEvent item)
    {
      throw new NotSupportedException("This list is read-only.");
    }

    /// <summary>
    /// Clearing is not supported and this method always throws NotSupportedException.
    /// </summary>
    /// <exception cref="System.NotSupportedException">NotSupportedException is always thrown.</exception>
    void ICollection<IntersectionEvent>.Clear()
    {
      throw new NotSupportedException("This list is read-only.");
    }

    /// <summary>
    /// Allows to establish whether this collection contains and IntersectionEvent.
    /// <para>This method is O(n), where n is the Count of elements in this collection.</para>
    /// </summary>
    /// <param name="item">Object to be found.</param>
    /// <returns>true if element is contained; otherwise false.</returns>
    bool ICollection<IntersectionEvent>.Contains(IntersectionEvent item)
    {
      for (int i = 0; i < Count; i++)
      {
        if (item == this[i])
          return true;
      }
      return false;
    }

    /// <summary>
    /// This collection is readonly, so this property returns always true.
    /// </summary>
    bool ICollection<IntersectionEvent>.IsReadOnly
    {
      get
      {
        return true;
      }
    }

    /// <summary>
    /// Removal is not supported and this method always throws NotSupportedException.
    /// </summary>
    /// <exception cref="System.NotSupportedException">NotSupportedException is always thrown.</exception>
    bool ICollection<IntersectionEvent>.Remove(IntersectionEvent item)
    {
      throw new NotSupportedException("This collection is read-only.");
    }

    /// <summary>
    /// Returns a non-generic enumerator that is capable of yielding all IntersectionEvents in the collection.
    /// This returns the same enumerator as the generic counterpart.
    /// </summary>
    /// <returns>The constructed enumerator.</returns>
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
    {
      return GetEnumerator();
    }
    #endregion
  }
#endif
}