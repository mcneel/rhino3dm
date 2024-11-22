#pragma warning disable 1591
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using Rhino.Runtime.InteropWrappers;
using Rhino.FileIO;
using Rhino.Geometry;

namespace Rhino.DocObjects
{
  [Serializable]
  public sealed class Linetype : ModelComponent
  {
    #region members
    // Represents both a CRhinoLinetype and an ON_Linetype. When m_ptr is
    // null, the object uses m_doc and m_id to look up the const
    // CRhinoLinetype in the linetype table.
#if RHINO_SDK
    readonly Rhino.RhinoDoc m_doc;
#endif
    readonly Guid m_id=Guid.Empty;
    #endregion

    #region constructors
    /// <since>5.0</since>
    public Linetype() : base()
    {
      // Creates a new non-document control ON_Linetype
      IntPtr pLinetype = UnsafeNativeMethods.ON_Linetype_New(IntPtr.Zero);
      ConstructNonConstObject(pLinetype);
    }

    /// <since>8.0</since>
    public Linetype(Linetype other) : base()
    {
      IntPtr pOther = other.ConstPointer();
      IntPtr pLinetype = UnsafeNativeMethods.ON_Linetype_New(pOther);
      ConstructNonConstObject(pLinetype);
    }

    /// <summary>
    /// Duplicates a linetype, clears the name, id, and locked bits.
    /// </summary>
    /// <returns>The duplicated linetype if successful, null otherwise.</returns>
    /// <since>8.0</since>
    public Linetype DuplicateLinetype()
    {
      IntPtr ptr_const_this = ConstPointer();
      IntPtr ptr_linetype = UnsafeNativeMethods.ON_Linetype_DuplicateLinetype(ptr_const_this);
      if (ptr_linetype != IntPtr.Zero)
        return new Linetype(ptr_linetype);
      return null;
    }

#if RHINO_SDK
    internal Linetype(int index, Rhino.RhinoDoc doc) : base()
    {
      m_id = UnsafeNativeMethods.CRhinoLinetypeTable_GetLinetypeId(doc.RuntimeSerialNumber, index);
      m_doc = doc;
      m__parent = m_doc;
    }
#endif

    internal Linetype(IntPtr pLinetype)
       : base()
    {
      ConstructNonConstObject(pLinetype);
    }

    // serialization constructor
    private Linetype(SerializationInfo info, StreamingContext context)
      : base (info, context)
    {
    }

    internal Linetype(Guid id, Rhino.FileIO.File3dm onxModel)
    {
      m_id = id;
      m__parent = onxModel;
    }
    #endregion

    /// <since>5.0</since>
    public bool CommitChanges()
    {
#if RHINO_SDK
      if (m_id == Guid.Empty || IsDocumentControlled)
        return false;
      IntPtr pThis = NonConstPointer();
      return UnsafeNativeMethods.CRhinoLinetypeTable_CommitChanges(m_doc.RuntimeSerialNumber, pThis, m_id);
#else
      return true;
#endif
    }

    internal override IntPtr _InternalGetConstPointer()
    {
#if RHINO_SDK
      if (m_doc != null)
      {
        var rc = UnsafeNativeMethods.CRhinoLinetypeTable_GetLinetypePointer2(m_doc.RuntimeSerialNumber, m_id);
        if (rc == IntPtr.Zero)
          throw new Runtime.DocumentCollectedException($"Could not find Linetype with ID {m_id}");
        return rc;
      }
#endif
      Rhino.FileIO.File3dm file_parent = m__parent as Rhino.FileIO.File3dm;
      if (file_parent != null)
      {
        IntPtr pModel = file_parent.NonConstPointer();
        return UnsafeNativeMethods.ONX_Model_GetLinetypePointer(pModel, m_id);
      }

      return IntPtr.Zero;
    }

    internal override IntPtr NonConstPointer()
    {
      if (m__parent is Rhino.FileIO.File3dm)
      {
        return _InternalGetConstPointer();
      }
      return base.NonConstPointer();
    }

    internal override IntPtr _InternalDuplicate(out bool applymempressure)
    {
      applymempressure = false;
      IntPtr pConstPointer = ConstPointer();
      return UnsafeNativeMethods.ON_Object_Duplicate(pConstPointer);
    }

    #region properties
    /// <summary>
    /// Returns <see cref="ModelComponentType.LinePattern"/>.
    /// </summary>
    /// <since>6.0</since>
    public override ModelComponentType ComponentType => ModelComponentType.LinePattern;

    /// <summary>
    /// Gets a value indicating whether this linetype has been deleted and is 
    /// currently in the Undo buffer.
    /// </summary>
    /// <since>5.0</since>
    public override bool IsDeleted => base.IsDeleted;

    /// <summary>
    /// Gets a value indicting whether this linetype is a referenced linetype. 
    /// Referenced linetypes are part of referenced documents.
    /// </summary>
    /// <since>5.0</since>
    public override bool IsReference => base.IsReference;

    /// <summary>The name of this linetype.</summary>
    /// <since>5.0</since>
    public override string Name
    {
      get
      {
        using (var sh = new StringHolder())
        {
          IntPtr pLinetype = ConstPointer();
          IntPtr pString = sh.NonConstPointer();
          UnsafeNativeMethods.ON_Linetype_GetLinetypeName(pLinetype, pString);
          return sh.ToString();
        }
      }
      set
      {
        IntPtr pThis = NonConstPointer();
        UnsafeNativeMethods.ON_Linetype_SetLinetypeName(pThis, value);
      }
    }

    /// <summary>The index of this linetype.</summary>
    /// <since>5.0</since>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    //[Obsolete("Use the Index property.")]
    public int LinetypeIndex
    {
      get { return Index; }
      set { Index = value; }
    }

    /// <summary>Total length of one repeat of the pattern.</summary>
    /// <since>5.0</since>
    public double PatternLength
    {
      get
      {
        IntPtr pConstThis = ConstPointer();
        return UnsafeNativeMethods.ON_Linetype_PatternLength(pConstThis);
      }
    }

    /// <summary>Number of segments in the pattern.</summary>
    /// <since>5.0</since>
    public int SegmentCount
    {
      get { return GetInt(UnsafeNativeMethods.LinetypeInteger.SegmentCount); }
    }

    /// <summary>
    /// true if this linetype has been modified by LinetypeTable.ModifyLinetype()
    /// and the modifications can be undone.
    /// </summary>
    /// <since>5.0</since>
    public bool IsModified
    {
      get
      {
#if RHINO_SDK
        if (null == m_doc)
          return false;
        int index = Index;
        return UnsafeNativeMethods.CRhinoLinetype_IsModified(m_doc.RuntimeSerialNumber, index);
#else
        return false;
#endif
      }
    }

#if RHINO_SDK
    /// <summary>
    /// Returns true if the linetype is in use by a Rhino object, 
    /// a layer, an instance definition, or a section style.
    /// </summary>
    /// <since>8.7</since>
    public bool InUse
    {
      get
      {
        if (null == m_doc)
          return false;
        int index = Index;
        return UnsafeNativeMethods.CRhinoLinetype_InUse(m_doc.RuntimeSerialNumber, index);
      }
    }
#endif

    /// <summary>
    /// Defines how the ends of open curves should be drawn
    /// </summary>
    /// <since>8.0</since>
    public Rhino.DocObjects.LineCapStyle LineCapStyle
    {
      get { return (Rhino.DocObjects.LineCapStyle)GetInt(UnsafeNativeMethods.LinetypeInteger.LineCap); }
      set { SetInt(UnsafeNativeMethods.LinetypeInteger.LineCap, (int)value); }
    }

    /// <summary>
    /// Defines how the corners of curves should be drawn
    /// </summary>
    /// <since>8.0</since>
    public Rhino.DocObjects.LineJoinStyle LineJoinStyle
    {
      get { return (Rhino.DocObjects.LineJoinStyle)GetInt(UnsafeNativeMethods.LinetypeInteger.LineJoin); }
      set { SetInt(UnsafeNativeMethods.LinetypeInteger.LineJoin, (int)value); }
    }

    /// <summary> Base width for this linetype </summary>
    /// <since>8.0</since>
    public double Width
    {
      get
      {
        IntPtr ptr = ConstPointer();
        return UnsafeNativeMethods.ON_Linetype_GetWidth(ptr);
      }
      set
      {
        IntPtr ptr = NonConstPointer();
        UnsafeNativeMethods.ON_Linetype_SetWidth(ptr, value);
      }
    }

    /// <summary>
    /// Unit system that widths are defined in. UnitSystem.None is default
    /// and means that the width is defined in pixels.
    /// </summary>
    /// <since>8.0</since>
    public UnitSystem WidthUnits
    {
      get
      {
        IntPtr ptr = ConstPointer();
        return UnsafeNativeMethods.ON_Linetype_GetWidthUnits(ptr);
      }
      set
      {
        IntPtr ptr = NonConstPointer();
        UnsafeNativeMethods.ON_Linetype_SetWidthUnits(ptr, value);
      }
    }

    /// <summary>
    /// Taper points are positions/width combinations along the length of a curve
    /// </summary>
    /// <returns></returns>
    /// <since>8.0</since>
    public Point2d[] GetTaperPoints()
    {
      using (var pointArray = new Rhino.Runtime.InteropWrappers.SimpleArrayPoint2d())
      {
        IntPtr ptr = ConstPointer();
        IntPtr ptArrayPtr = pointArray.NonConstPointer();
        UnsafeNativeMethods.ON_Linetype_GetTaperPoints(ptr, ptArrayPtr);
        return pointArray.ToArray();
      }
    }

    /// <summary>
    /// Set taper to a simple start width / end width
    /// </summary>
    /// <param name="startWidth"></param>
    /// <param name="endWidth"></param>
    /// <since>8.0</since>
    public void SetTaper(double startWidth, double endWidth)
    {
      SetTaper(startWidth, Point2d.Unset, endWidth);
    }

    /// <summary>
    /// Set taper for this linetype width a single internal taper point
    /// </summary>
    /// <param name="startWidth"></param>
    /// <param name="taperPoint"></param>
    /// <param name="endWidth"></param>
    /// <since>8.0</since>
    public void SetTaper(double startWidth, Point2d taperPoint, double endWidth) 
    {
      IntPtr ptr_this = NonConstPointer();
      UnsafeNativeMethods.ON_Linetype_SetTaper(ptr_this, startWidth, taperPoint, endWidth);
    }

    /// <summary>
    /// Remove taper information for stroke
    /// </summary>
    /// <since>8.0</since>
    public void RemoveTaper()
    {
      IntPtr ptr_this = NonConstPointer();
      UnsafeNativeMethods.ON_Linetype_RemoveTaper(ptr_this);
    }

    /// <summary>
    /// Linetype patterns and widths are typically interpreted as distances on
    /// the printed output when printing. In this case AlwaysModelDistances is
    /// false (default). When set to true, the linetype pattern and width will
    /// be interpreted as being in world distances. This is useful for cases
    /// like modeling a road as a single curve.
    /// </summary>
    /// <since>8.6</since>
    public bool AlwaysModelDistances
    {
      get
      {
        IntPtr constPtrThis = ConstPointer();
        return UnsafeNativeMethods.ON_Linetype_AlwaysModelDistances(constPtrThis);
      }
      set
      {
        IntPtr ptrThis = NonConstPointer();
        UnsafeNativeMethods.ON_Linetype_SetAlwaysModelDistances(ptrThis, value);
      }
    }

    const int idxSegmentCount = 1;
    int GetInt(UnsafeNativeMethods.LinetypeInteger which)
    {
      IntPtr pConstLinetype = ConstPointer();
      return UnsafeNativeMethods.ON_Linetype_GetInt(pConstLinetype, which);
    }
    void SetInt(UnsafeNativeMethods.LinetypeInteger which, int val)
    {
      IntPtr ptr = NonConstPointer();
      UnsafeNativeMethods.ON_Linetype_SetInt(ptr, which, val);
    }

    /// <summary>
    /// Returns true if the pattern is locked and cannot be modified.
    /// </summary>
    /// <since>8.0</since>
    public bool IsPatternLocked
    {
      get
      {
#if RHINO_SDK
        if (null == m_doc)
          return false;
        return UnsafeNativeMethods.CRhinoLinetypeTable_PatternIsLocked(m_doc.RuntimeSerialNumber, Index);
#else
        return false;
#endif
      }
    }

#endregion

    #region methods
    /// <summary>
    /// Set linetype to default settings.
    /// </summary>
    /// <since>5.0</since>
    public void Default()
    {
      IntPtr pThis = NonConstPointer();
      UnsafeNativeMethods.ON_Linetype_Default(pThis);
    }

    /// <summary>Adds a segment to the pattern.</summary>
    /// <param name="length">The length of the segment to be added.</param>
    /// <param name="isSolid">
    /// If true, the length is interpreted as a line. If false,
    /// then the length is interpreted as a space.
    /// </param>
    /// <returns>Index of the added segment.</returns>
    /// <since>5.0</since>
    public int AppendSegment(double length, bool isSolid)
    {
      IntPtr pThis = NonConstPointer();
      return UnsafeNativeMethods.ON_Linetype_AppendSegment(pThis, length, isSolid);
    }

    /// <summary>Removes a segment in the linetype.</summary>
    /// <param name="index">Zero based index of the segment to remove.</param>
    /// <returns>true if the segment index was removed.</returns>
    /// <since>5.0</since>
    public bool RemoveSegment(int index)
    {
      IntPtr pThis = NonConstPointer();
      return UnsafeNativeMethods.ON_Linetype_RemoveSegment(pThis, index);
    }

    /// <summary>Set all segments.</summary>
    /// <param name="segments">
    /// An array of segments lengths. 
    /// Lengths &gt;= 0 are interpreted as a line. 
    /// Lengths &lt; 0 are interpreted as a space.
    /// </param>
    /// <returns>true if the segments were replaced</returns>
    /// <since>6.8</since>
    public bool SetSegments(IEnumerable<double> segments)
    {
      IntPtr ptr_this = NonConstPointer();
      if (segments==null)
      {
        double[] empty = new double[0];
        return UnsafeNativeMethods.ON_Linetype_SetSegments(ptr_this, 0, empty);
      }
      var segment_list = new List<double>(segments);
      double[] segment_array = segment_list.ToArray();
      return UnsafeNativeMethods.ON_Linetype_SetSegments(ptr_this, segment_array.Length, segment_array);
    }

    /// <summary>Sets the length and type of the segment at index.</summary>
    /// <param name="index">Zero based index of the segment.</param>
    /// <param name="length">The length of the segment to be added in millimeters.</param>
    /// <param name="isSolid">
    /// If true, the length is interpreted as a line. If false,
    /// then the length is interpreted as a space.
    /// </param>
    /// <returns>true if the operation was successful; otherwise false.</returns>
    /// <since>5.0</since>
    public bool SetSegment(int index, double length, bool isSolid)
    {
      IntPtr pThis = NonConstPointer();
      return UnsafeNativeMethods.ON_Linetype_SetSegment(pThis, index, length, isSolid);
    }

    /// <summary>
    /// Gets the segment information at a index.
    /// </summary>
    /// <param name="index">Zero based index of the segment.</param>
    /// <param name="length">The length of the segment in millimeters.</param>
    /// <param name="isSolid">
    /// If the length is interpreted as a line, true is assigned during the call to this out parameter.
    /// <para>If the length is interpreted as a space, then false is assigned during the call to this out parameter.</para>
    /// </param>
    /// <exception cref="IndexOutOfRangeException">If the index is unacceptable.</exception>
    /// <since>5.0</since>
    public void GetSegment(int index, out double length, out bool isSolid)
    {
      if (index < 0 || index >= SegmentCount)
        throw new IndexOutOfRangeException();
      IntPtr pConstThis = ConstPointer();
      length = 0;
      isSolid = false;
      UnsafeNativeMethods.ON_Linetype_GetSegment(pConstThis, index, ref length, ref isSolid);
    }
    #endregion

#if RHINO_SDK
    /// <summary>
    /// Reads linetypes from either a Rhino .3dm file or an AutoCAD .lin file.
    /// </summary>
    /// <param name="path">The path to the file to read.</param>
    /// <returns>An array of linetypes if successful, otherwise an empty array.</returns>
    /// <since>6.6</since>
    public static Linetype[] ReadFromFile(string path)
    {
      using (var rc = new SimpleArrayLinetypePointer())
      {
        IntPtr ptr_linetypes = rc.NonConstPointer();
        int count = UnsafeNativeMethods.RHC_RhinoReadLinetypesFromFile(path, ptr_linetypes);
        if (count > 0)
        {
          return rc.ToNonConstArray();
        }
        return new Linetype[0];
      }
    }

    /// <summary>
    /// Creates a linetype from a pattern string.
    /// Values greater than zero represent line segments, 
    /// and values less than or equal to zero represents space segments.
    /// </summary>
    /// <param name="patternString">The pattern string.</param>
    /// <param name="millimeters">
    /// Specify true if the pattern is represented in millimeters. 
    /// Specify false if the pattern is represented in inches.
    /// </param>
    /// <returns>A valid linetype if successful, null otherwise.</returns>
    /// <since>7.4</since>
    public static Linetype CreateFromPatternString(string patternString, bool millimeters)
    {
      IntPtr ptr_linetype = UnsafeNativeMethods.RHC_RhLinetypeFromPatternString(patternString, millimeters);
      if (IntPtr.Zero != ptr_linetype)
        return new DocObjects.Linetype(ptr_linetype);
      return null;
    }

    /// <summary>
    /// Returns a string that represents the pattern of the linetype, which can be used in user interface.
    /// Values greater than zero represent line segments, 
    /// and values less than or equal to zero represent space segments.
    /// </summary>
    /// <param name="millimeters">
    /// If true, the string is formatted in millimeters. 
    /// If false, the string is formatted in inches.
    /// </param>
    /// <returns>The pattern string.</returns>
    /// <since>7.4</since>
    public string PatternString(bool millimeters)
    {
      using (var sh = new StringHolder())
      {
        IntPtr ptr_string = sh.NonConstPointer();
        IntPtr ptr_const_this = ConstPointer();
        UnsafeNativeMethods.RHC_RhPatternStringFromLinetype(ptr_const_this, millimeters, ptr_string);
        return sh.ToString();
      }
    }

#endif

    #region user strings
    /// <summary>
    /// Attach a user string (key,value combination) to this geometry.
    /// </summary>
    /// <param name="key">id used to retrieve this string.</param>
    /// <param name="value">string associated with key.</param>
    /// <returns>true on success.</returns>
    /// <since>8.9</since>
    public bool SetUserString(string key, string value) => _SetUserString(key, value);

    /// <summary>
    /// Gets user string from this geometry.
    /// </summary>
    /// <param name="key">id used to retrieve the string.</param>
    /// <returns>string associated with the key if successful. null if no key was found.</returns>
    /// <since>8.9</since>
    public string GetUserString(string key) => _GetUserString(key);

    /// <summary>
    /// Gets the amount of user strings.
    /// </summary>
    /// <since>8.9</since>
    public int UserStringCount => _UserStringCount;

    /// <summary>
    /// Gets a copy of all (user key string, user value string) pairs attached to this geometry.
    /// </summary>
    /// <returns>A new collection.</returns>
    /// <since>8.9</since>
    public System.Collections.Specialized.NameValueCollection GetUserStrings() => _GetUserStrings();

    /// <since>8.9</since>
    public bool DeleteUserString(string key) => SetUserString(key, null);

    /// <since>8.9</since>
    public void DeleteAllUserStrings() => _DeleteAllUserStrings();
    #endregion
  }
}

#if RHINO_SDK
namespace Rhino.DocObjects.Tables
{
  /// <summary>
  /// LinetypeTable event types
  /// <since>8.0</since>
  /// </summary>
  public enum LinetypeTableEventType
  {
    /// <summary>
    /// A linetype was added.
    /// </summary>
    Added = 0,
    /// <summary>
    /// A linetype was deleted.
    /// </summary>
    Deleted = 1,
    /// <summary>
    /// A linetype was undeleted.
    /// </summary>
    Undeleted = 2,
    /// <summary>
    /// A linetype was modified.
    /// </summary>
    Modified = 3,
    /// <summary>
    /// The linetype table was sorted.
    /// </summary>
    Sorted = 4,
    /// <summary>
    /// The current linetype has changed.
    /// </summary>
    Current = 5
  }

  /// <summary>
  /// LinetypeTable event arguments
  /// <since>8.0</since>
  /// </summary>
  public class LinetypeTableEventArgs : EventArgs
  {
    readonly private uint m_doc_sn;
    readonly private LinetypeTableEventType m_event_type;
    readonly private int m_linetype_index;
    readonly private IntPtr m_ptr_old_linetype;

    internal LinetypeTableEventArgs(uint docSerialNumber, int eventType, int index, IntPtr pConstOldLinetype)
    {
      m_doc_sn = docSerialNumber;
      m_event_type = (LinetypeTableEventType)eventType;
      m_linetype_index = index;
      m_ptr_old_linetype = pConstOldLinetype;
    }

    /// <summary>
    /// The document in which the event occurred.
    /// </summary>
    /// <since>8.0</since>
    public RhinoDoc Document
    {
      get { return m_doc ?? (m_doc = RhinoDoc.FromRuntimeSerialNumber(m_doc_sn)); }
    }
    private RhinoDoc m_doc;

    /// <summary>
    /// The event type.
    /// </summary>
    /// <since>8.0</since>
    public LinetypeTableEventType EventType
    {
      get { return m_event_type; }
    }

    /// <summary>
    /// Index of the linetype.
    /// </summary>
    /// <since>8.0</since>
    public int LinetypeIndex
    {
      get { return m_linetype_index; }
    }

    /// <summary>
    /// The new state.
    /// </summary>
    /// <since>8.0</since>
    public Linetype NewState
    {
      get { return m_new_linetype ?? (m_new_linetype = new Linetype(LinetypeIndex, Document)); }
    }
    private Linetype m_new_linetype;

    /// <summary>
    /// The old state.
    /// </summary>
    /// <since>8.0</since>
    public Linetype OldState
    {
      get
      {
        if (m_old_linetype == null && m_ptr_old_linetype != IntPtr.Zero)
        {
          // 14 July 2022 - S. Baer
          // m_ptr_old_linetype is const and is deleted when this EventWatcher event
          // completes. Make a copy when OldState is accessed so we don't end up
          // with a double delete in the LineType finalizer.
          IntPtr pLineType = UnsafeNativeMethods.ON_Linetype_New(m_ptr_old_linetype);
          m_old_linetype = new Linetype(pLineType);
        }
        return m_old_linetype;
      }
    }
    private Linetype m_old_linetype;
  }


  public sealed class LinetypeTable :
    RhinoDocCommonTable<Linetype>, ICollection<Linetype>
  {
    internal LinetypeTable(RhinoDoc doc) : base(doc)
    {
    }

    /// <summary>Document that owns this table.</summary>
    /// <since>5.0</since>
    public new RhinoDoc Document
    {
      get { return base.Document; }
    }

    /// <summary>
    /// Returns number of linetypes in the linetypes table, including deleted linetypes.
    /// </summary>
    /// <since>5.0</since>
    public new int Count
    {
      get
      {
        return UnsafeNativeMethods.CRhinoLinetypeTable_LinetypeCount(m_doc.RuntimeSerialNumber, false);
      }
    }

    /// <summary>
    /// Returns number of linetypes in the linetypes table, excluding deleted linetypes.
    /// </summary>
    /// <since>5.0</since>
    public int ActiveCount
    {
      get
      {
        return UnsafeNativeMethods.CRhinoLinetypeTable_LinetypeCount(m_doc.RuntimeSerialNumber, true);
      }
    }

    /// <summary>
    /// At all times, there is a "current" linetype.  Unless otherwise specified,
    /// new objects are assigned to the current linetype. If the current linetype
    /// source is LinetypeFromLayer the object's layer's linetype is used instead.
    /// </summary>
    /// <since>5.0</since>
    public int CurrentLinetypeIndex
    {
      get
      {
        return UnsafeNativeMethods.CRhinoLinetypeTable_CurrentLinetypeIndex(m_doc.RuntimeSerialNumber);
      }
    }

    /// <summary>
    /// For display in Rhino viewports, the linetypes are scaled by a single scale
    /// factor for all viewports. This is not used for printing, where all linetype
    /// patterns are scaled to print in their defined size 1:1 on the paper.
    /// </summary>
    /// <since>5.0</since>
    public double LinetypeScale
    {
      get
      {
        return UnsafeNativeMethods.CRhinoLinetypeTable_GetLinetypeScale(m_doc.RuntimeSerialNumber);
      }
      set
      {
        UnsafeNativeMethods.CRhinoLinetypeTable_SetLinetypeScale(m_doc.RuntimeSerialNumber, value);
      }
    }

    /// <summary>
    /// Conceptually, the linetype table is an array of linetypes.
    /// The operator[] can be used to get individual linetypes. A linetype is
    /// either active or deleted and this state is reported by Linetype.IsDeleted.
    /// </summary>
    /// <param name="index">zero based array index.</param>
    /// <returns>
    /// Reference to the linetype.  If index is out of range, the current
    /// linetype is returned. Note that this reference may become invalid after
    /// AddLinetype() is called.
    /// </returns>
    public DocObjects.Linetype this[int index]
    {
      get
      {
        var rc = FindIndex(index);
        // https://mcneel.myjetbrains.com/youtrack/issue/RH-77124
        // Commenting out for now
        //if (null == rc)
        //  throw new IndexOutOfRangeException();
        return rc;
      }
    }

    /// <summary>
    /// Source used by an object to determine its current linetype to be used by new objects.
    /// </summary>
    /// <since>5.0</since>
    public Rhino.DocObjects.ObjectLinetypeSource CurrentLinetypeSource
    {
      get
      {
        return (Rhino.DocObjects.ObjectLinetypeSource)UnsafeNativeMethods.CRhinoLinetypeTable_GetCurrentLinetypeSource(m_doc.RuntimeSerialNumber);
      }
      set
      {
        UnsafeNativeMethods.CRhinoLinetypeTable_SetCurrentLinetypeSource(m_doc.RuntimeSerialNumber, (int)value);
      }
    }

    /// <summary>
    /// At all times, there is a "current" linetype. Unless otherwise specified, new objects
    /// are assigned to the current linetype. The current linetype is never deleted.
    /// </summary>
    /// <param name="linetypeIndex">
    /// Value for new current linetype. 0 &lt;= linetypeIndex &lt; LinetypeTable.Count.
    /// </param>
    /// <param name="quiet">
    /// if true, then no warning message box pops up if the current linetype request can't be satisfied.
    /// </param>
    /// <returns>true if current linetype index successfully set.</returns>
    /// <since>5.0</since>
    public bool SetCurrentLinetypeIndex(int linetypeIndex, bool quiet)
    {
      return UnsafeNativeMethods.CRhinoLinetypeTable_SetCurrentLinetypeIndex(m_doc.RuntimeSerialNumber, linetypeIndex, quiet);
    }

    /// <summary>
    /// Returns the effective linetype index to be used to find the 
    /// linetype definition to draw an object. If an object's linetype
    /// source is LinetypeFromObject, the linetype index in the object's
    /// attributes is used. If an object's linetype source is LinetypeFromLayer
    /// the linetype index from the object's layer is used.
    /// </summary>
    /// <param name="rhinoObject">The Rhino object to use in the query.</param>
    /// <returns>The effective linetype index.</returns>
    /// <since>5.0</since>
    public int LinetypeIndexForObject(Rhino.DocObjects.RhinoObject rhinoObject)
    {
      IntPtr pConstRhinoObject = rhinoObject.ConstPointer();
      return UnsafeNativeMethods.CRhinoLinetypeTable_EffectiveLinetypeIndex(m_doc.RuntimeSerialNumber, pConstRhinoObject);
    }

    /// <summary>
    /// Returns reference to the current linetype. Note that this reference may
    /// become invalid after a call to AddLinetype().
    /// </summary>
    /// <remarks>
    /// At all times, there is a "current" linetype. Unless otherwise specified,
    /// new objects are assigned to the current linetype. The current linetype
    /// is never deleted.
    /// </remarks>
    /// <since>5.0</since>
    public DocObjects.Linetype CurrentLinetype
    {
      get
      {
        return new Rhino.DocObjects.Linetype(CurrentLinetypeIndex, m_doc);
      }
    }

    /// <summary>
    /// Fills in the linetype table with any default linetypes not already included.
    /// </summary>
    /// <remarks>
    /// New documents only contain the continuous linetype. Other default linetypes
    /// are added, on demand, when the user needs them. Calling this function ensures
    /// that the linetype table is populated with the default linetypes.
    /// </remarks>
    /// <returns>The number of default linetypes added to the linetype table.</returns>
    /// <since>6.0</since>
    public int LoadDefaultLinetypes()
    {
      return LoadDefaultLinetypes(false);
    }

    /// <summary>
    /// Fills in the linetype table with any default linetypes not already included.
    /// </summary>
    /// <param name="ignoreDeleted">Ignore default linetypes that have been deleted.</param>
    /// <remarks>
    /// New documents only contain the continuous linetype. Other default linetypes
    /// are added, on demand, when the user needs them. Calling this function ensures
    /// that the linetype table is populated with the default linetypes.
    /// </remarks>
    /// <returns>The number of default linetypes added to the linetype table.</returns>
    /// <since>8.0</since>
    public int LoadDefaultLinetypes(bool ignoreDeleted)
    {
      return UnsafeNativeMethods.CRhinoLinetypeTable_InitDefaultLinetypes(m_doc.RuntimeSerialNumber, ignoreDeleted);
    }

    /// <summary>Obsolete. Use the other overload.</summary>
    /// <param name="name">search ignores case.</param>
    /// <param name="ignoreDeletedLinetypes">If true, deleted linetypes are not checked.</param>
    /// <returns>
    /// >=0 index of the linetype with the given name
    /// -1  no linetype has the given name.
    /// </returns>
    /// <since>5.0</since>
    /// <deprecated>6.0</deprecated>
    [Obsolete("ignoreDeletedLinetypes is now ignored. Items are removed permanently now. Remove the second method argument.")]
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public int Find(string name, bool ignoreDeletedLinetypes)
    {
      return Find(name);
    }

    /// <summary>Finds the linetype with a given name.</summary>
    /// <param name="name">The name of the linetype to find. The search ignores case.</param>
    /// <returns>
    /// If the linetype was found, the linetype index, &gt;=0, is returned.
    /// If the linetype was not found, -1 is returned.
    /// Note, the linetype index of -1 denotes the default, or "Continuous" linetype.
    /// </returns>
    /// <since>6.0</since>
    public int Find(string name)
    {
      return UnsafeNativeMethods.CRhinoLinetypeTable_FindLinetype(m_doc.RuntimeSerialNumber, name);
    }

    /// <summary>Finds a linetype with a matching ID.</summary>
    /// <param name="id">The ID of the line type to be found.</param>
    /// <param name="ignoreDeletedLinetypes">If true, deleted linetypes are not checked.</param>
    /// <returns>
    /// If the linetype was found, the linetype index, &gt;=0, is returned.
    /// If the linetype was not found, -1 is returned.
    /// Note, the linetype index of -1 denotes the default, or "Continuous" linetype.
    /// </returns>
    /// <since>5.0</since>
    public int Find(Guid id, bool ignoreDeletedLinetypes)
    {
      return UnsafeNativeMethods.CRhinoLinetypeTable_FindLinetype2(m_doc.RuntimeSerialNumber, id, ignoreDeletedLinetypes);
    }

    /// <summary>Finds the linetype with a given name.</summary>
    /// <param name="name">he name of the linetype to find.</param>
    /// <returns>
    /// The linetype. If the linetype was not found, then the default, or or "Continuous" linetype is returned.
    /// </returns>
    /// <since>6.0</since>
    public Linetype FindName(string name)
    {
      int index = Find(name);
      return FindIndex(index);
    }

    /// <summary>
    /// Retrieves a Linetype object based on Index. This search type of search is discouraged.
    /// We are moving towards using only IDs for all tables.
    /// </summary>
    /// <param name="index">The index to search for.</param>
    /// <returns>A Linetype object, or null if none was found.</returns>
    /// <since>6.0</since>
    public Linetype FindIndex(int index)
    {
      int system_count = UnsafeNativeMethods.CRhinoLinetypeTable_SystemLinetypeCount(m_doc.RuntimeSerialNumber);
      if (index < -system_count || index >= Count)
        return null;
      return new Linetype(index, m_doc);
    }

    /// <summary>
    /// Adds a new linetype with specified definition to the linetype table.
    /// </summary>
    /// <param name="linetype">
    /// Definition of new linetype.  The information in linetype is copied.
    /// If linetype.Name is empty then a unique name of the form "Linetype 01"
    /// will be automatically created.
    /// </param>
    /// <returns>
    /// Index of newline type or -1 on error.
    /// </returns>
    /// <since>5.0</since>
    public int Add(DocObjects.Linetype linetype)
    {
      IntPtr pConstLinetype = linetype.ConstPointer();
      return UnsafeNativeMethods.CRhinoLinetypeTable_AddLinetype(m_doc.RuntimeSerialNumber, pConstLinetype, false);
    }

    /// <since>5.0</since>
    void ICollection<Linetype>.Add(Linetype item)
    {
      if (Add(item) < 0)
        throw new InvalidOperationException("Could not add item to table.");
    }

    /// <summary>
    /// Adds a new linetype with specified definition to the linetype table.
    /// </summary>
    /// <param name="name">A name for the new linetype.</param>
    /// <param name="segmentLengths">Positive values are dashes, negative values are gaps.</param>
    /// <returns>
    /// Index of new linetype or -1 on error.
    /// </returns>
    /// <since>5.0</since>
    public int Add(string name, IEnumerable<double> segmentLengths)
    {
      using (Runtime.InteropWrappers.SimpleArrayDouble segs = new Rhino.Runtime.InteropWrappers.SimpleArrayDouble(segmentLengths))
      {
        IntPtr pSegs = segs.NonConstPointer();
        return UnsafeNativeMethods.CRhinoLinetypeTable_AddLinetype2(m_doc.RuntimeSerialNumber, name, pSegs);
      }
    }

    /// <summary>
    /// Adds a reference linetypes that will not be saved in files.
    /// </summary>
    /// <param name="linetype">Definition of new linetype.  The information in linetype is copied.
    /// If linetype.Name is empty then a unique name of the form "Linetype 01"
    /// will be automatically created.</param>
    /// <returns>
    /// Index of new linetype or -1 on error.
    /// </returns>
    /// <since>5.0</since>
    public int AddReferenceLinetype(DocObjects.Linetype linetype)
    {
      IntPtr pConstLinetype = linetype.ConstPointer();
      return UnsafeNativeMethods.CRhinoLinetypeTable_AddLinetype(m_doc.RuntimeSerialNumber, pConstLinetype, true);
    }

    /// <summary>Modify linetype settings.</summary>
    /// <param name="linetype">New linetype settings. This information is copied.</param>
    /// <param name="index">Zero based index of linetype to set.</param>
    /// <param name="quiet">
    /// if true, information message boxes pop up when illegal changes are attempted.
    /// </param>
    /// <returns>
    /// true if successful. false if linetype_index is out of range or the
    /// settings attempt to lock or hide the current linetype.
    /// </returns>
    /// <since>5.0</since>
    public bool Modify(DocObjects.Linetype linetype, int index, bool quiet)
    {
      IntPtr pConstLinetype = linetype.ConstPointer();
      return UnsafeNativeMethods.CRhinoLinetypeTable_Modify(m_doc.RuntimeSerialNumber, pConstLinetype, index, quiet);
    }

    /// <summary>
    /// If the linetype has been modified and the modification can be undone,
    /// then UndoModify() will restore the linetype to its previous state.
    /// </summary>
    /// <param name="index">Zero based index of linetype for which to undo changes.</param>
    /// <returns>
    /// true if this linetype had been modified and the modifications were undone.
    /// </returns>
    /// <since>5.0</since>
    public bool UndoModify(int index)
    {
      return UnsafeNativeMethods.CRhinoLinetypeTable_Un(m_doc.RuntimeSerialNumber, index, true);
    }

    /// <summary>Deletes linetype.</summary>
    /// <param name="index">zero based index of linetype to delete.</param>
    /// <param name="quiet">
    /// If true, no warning message box appears if a linetype the
    /// linetype cannot be deleted because it is the current linetype
    /// or it contains active geometry.
    /// </param>
    /// <returns>
    /// true if successful. false if linetypeIndex is out of range or the
    /// linetype cannot be deleted because it is the current linetype or
    /// because it linetype is referenced by active geometry.
    /// </returns>
    /// <since>5.0</since>
    public bool Delete(int index, bool quiet)
    {
      return UnsafeNativeMethods.CRhinoLinetypeTable_Delete(m_doc.RuntimeSerialNumber, index, quiet);
    }

    /// <since>6.0</since>
    public override bool Delete(Linetype item)
    {
      if (item == null) return false;
      return Delete(item.Index, true);
    }

    /// <summary>Deletes multiple linetypes.</summary>
    /// <param name="indices">An array, a list or any enumerable instance of linetype indices.</param>
    /// <param name="quiet">If true, no warning message box appears if a linetype the
    /// linetype cannot be deleted because it is the current linetype
    /// or it contains active geometry.</param>
    /// <returns>true if operation succeeded.</returns>
    /// <since>5.0</since>
    public bool Delete(IEnumerable<int> indices, bool quiet)
    {
      List<int> l = new List<int>(indices);
      int[] _indices = l.ToArray();
      return UnsafeNativeMethods.CRhinoLinetypeTable_Delete2(m_doc.RuntimeSerialNumber, _indices.Length, _indices, quiet);
    }

    /// <summary>Restores a linetype that has been deleted.</summary>
    /// <param name="index">A linetype index to be undeleted.</param>
    /// <returns>true if successful.</returns>
    /// <since>5.0</since>
    public bool Undelete(int index)
    {
      return UnsafeNativeMethods.CRhinoLinetypeTable_Un(m_doc.RuntimeSerialNumber, index, false);
    }

    /// <summary>
    /// Obsolete. Use the other overload. Gets unused linetype name used as default when creating new linetypes.
    /// </summary>
    /// <param name="ignoreDeleted">
    /// If this is true then a name used by a deleted linetype is allowed.
    /// </param>
    /// <returns>The unused linetype name.</returns>
    /// <since>5.0</since>
    /// <deprecated>6.0</deprecated>
    [Obsolete("ignoreDeleted is now ignored. Items are removed permanently now. Remove the second method argument.")]
    public string GetUnusedLinetypeName(bool ignoreDeleted)
    {
      return GetUnusedLinetypeName();
    }

    /// <summary>
    /// Gets unused linetype name used as default when creating new linetypes.
    /// </summary>
    /// <returns>The unused linetype name.</returns>
    /// <since>6.0</since>
    public string GetUnusedLinetypeName()
    {
      using (var sh = new StringHolder())
      {
        IntPtr pString = sh.NonConstPointer();
        UnsafeNativeMethods.CRhinoLinetypeTable_GetUnusedLinetypeName(m_doc.RuntimeSerialNumber, pString);
        return sh.ToString();
      }
    }

    /// <summary>
    /// Returns the text name of the continuous linetype.
    /// </summary>
    /// <since>5.0</since>
    public string ContinuousLinetypeName
    {
      get
      {
        IntPtr pName = UnsafeNativeMethods.CRhinoLinetypeTable_GetNameOfDefaultLinetype(m_doc.RuntimeSerialNumber, true, default(bool));
        return Marshal.PtrToStringUni(pName);
      }
    }

    /// <summary>
    /// Returns the text name of the by-layer linetype.
    /// </summary>
    /// <since>5.0</since>
    public string ByLayerLinetypeName
    {
      get
      {
        IntPtr pName = UnsafeNativeMethods.CRhinoLinetypeTable_GetNameOfDefaultLinetype(m_doc.RuntimeSerialNumber, false, true);
        return Marshal.PtrToStringUni(pName);
      }
    }

    /// <summary>
    /// Returns the text name of the by-parent linetype.
    /// </summary>
    /// <since>6.0</since>
    public string ByParentLinetypeName
    {
      get
      {
        IntPtr pName = UnsafeNativeMethods.CRhinoLinetypeTable_GetNameOfDefaultLinetype(m_doc.RuntimeSerialNumber, false, false);
        return Marshal.PtrToStringUni(pName);
      }
    }

    /// <since>6.0</since>
    public override ModelComponentType ComponentType
    {
      get
      {
        return ModelComponentType.LinePattern;
      }
    }

    /// <since>5.0</since>
    public override IEnumerator<Linetype> GetEnumerator()
    {
      // 2-Feb-2017 Dale Fugier https://mcneel.myjetbrains.com/youtrack/issue/RH-37691
      // Steve and I are still debating whether or not to automatically fill
      // in the linetype table with it's defaults. If we change our mind,
      // We can modify the Count property's wrapper...
      int count = this.Count;

      return base.GetEnumerator();
    }
  }
}
#endif
