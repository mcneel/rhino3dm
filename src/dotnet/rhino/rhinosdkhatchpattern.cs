#pragma warning disable 1591
using System;
using System.Runtime.Serialization;
using System.Collections.Generic;
using System.Drawing;
using System.Linq.Expressions;
using Rhino.Runtime.InteropWrappers;
using Rhino.FileIO;
using Rhino.Collections;

namespace Rhino.DocObjects
{
  /// <since>5.0</since>
  public enum HatchPatternFillType
  {
    Solid = 0,
    Lines = 1,
    Gradient = 2
  }

  /// <summary>
  /// Hatch lines are used by hatch pattern to specify
  /// the dashes and offset patterns of the lines.
  /// </summary>
  /// <remarks>
  /// Each line has the following information:
  /// Angle is the direction of the line counter-clockwise from the x-axis.
  /// The first line origin is at the base point.
  /// Each line repetition is offset from the previous line.
  /// Offset.X is parallel to the line and Offset.Y is perpendicular to the line.
  /// The base and offset values are rotated by the line's angle to 
  /// produce a location in the hatch pattern's coordinate system.
  /// There can be gaps and dashes specified for drawing the line.
  /// If there are no dashes, the line is solid.
  /// Negative length dashes are gaps.
  /// Positive length dashes are drawn as line segments.
  /// </remarks>
  /// <since>8.0</since>
  public class HatchLine : IDisposable
  {
    /// <summary>
    /// Construts a new hatch line.
    /// </summary>
    /// <since>8.0</since>
    public HatchLine()
    {
      m_ptr = UnsafeNativeMethods.ON_HatchLine_New(IntPtr.Zero);
    }

    /// <summary>
    /// Construts a new hatch line.
    /// </summary>
    /// <param name="hatchLine">The hatch line to copy.</param>
    /// <since>8.0</since>
    public HatchLine(HatchLine hatchLine)
    {
      if (null == hatchLine)
        m_ptr = UnsafeNativeMethods.ON_HatchLine_New(IntPtr.Zero);
      else
      {
        IntPtr pConstLine = hatchLine.ConstPointer();
        m_ptr = UnsafeNativeMethods.ON_HatchLine_New(pConstLine);
      }
    }

    #region Properties

    /// <summary>
    /// Gets and sets the angle, in radians, of the hatch line.
    /// The angle is measured counter-clockwise from the x-axis.
    /// </summary>
    /// <since>8.0</since>
    public double Angle
    {
      get
      {
        double angle = 0.0;
        IntPtr pConstThis = ConstPointer();
        UnsafeNativeMethods.ON_HatchLine_GetSetAngle(pConstThis, ref angle, false);
        return angle;
      }
      set
      {
        double angle = value;
        IntPtr pThis = NonConstPointer();
        UnsafeNativeMethods.ON_HatchLine_GetSetAngle(pThis, ref angle, true);
      }
    }

    /// <summary>
    /// Get and sets this line's 2d base point.
    /// </summary>
    /// <since>8.0</since>
    public Rhino.Geometry.Point2d BasePoint
    {
      get
      {
        Rhino.Geometry.Point2d basePoint = new Rhino.Geometry.Point2d();
        IntPtr pConstThis = ConstPointer();
        UnsafeNativeMethods.ON_HatchLine_GetSetBasePoint(pConstThis, ref basePoint, false);
        return basePoint;
      }
      set
      {
        Rhino.Geometry.Point2d basePoint = value;
        IntPtr pThis = NonConstPointer();
        UnsafeNativeMethods.ON_HatchLine_GetSetBasePoint(pThis, ref basePoint, true);
      }
    }

    /// <summary>
    /// Get and sets this line's 2d offset for line repetitions.
    /// Offset.X is shift parallel to line.
    /// Offset.Y is spacing perpendicular to line.
    /// </summary>
    /// <since>8.0</since>
    public Rhino.Geometry.Vector2d Offset
    {
      get
      {
        Rhino.Geometry.Vector2d offset = new Rhino.Geometry.Vector2d();
        IntPtr pConstThis = ConstPointer();
        UnsafeNativeMethods.ON_HatchLine_GetSetOffset(pConstThis, ref offset, false);
        return offset;
      }
      set
      {
        Rhino.Geometry.Vector2d offset = value;
        IntPtr pThis = NonConstPointer();
        UnsafeNativeMethods.ON_HatchLine_GetSetOffset(pThis, ref offset, true);
      }
    }

    /// <summary>
    /// Gets the number of dashes + gaps in this line.
    /// </summary>
    /// <since>8.0</since>
    public int DashCount
    {
      get
      {
        IntPtr pConstThis = ConstPointer();
        return UnsafeNativeMethods.ON_HatchLine_DashCount(pConstThis);
      }
    }

    /// <summary>
    /// Gets all of the dashes.
    /// </summary>
    /// <since>8.0</since>
    public IEnumerable<double> GetDashes
    {
      get
      {
        int count = DashCount;
        for (int i = 0; i < count; i++)
        {
          yield return DashAt(i);
        }
      }
    }

    /// <summary>
    /// Get the total length of a pattern repeat.
    /// </summary>
    /// <since>8.0</since>
    public double PatternLength
    {
      get
      {
        IntPtr pConstThis = ConstPointer();
        return UnsafeNativeMethods.ON_HatchLine_GetPatternLength(pConstThis);
      }
    }

    #endregion // Properties

    #region Methods

    /// <summary>
    /// Get the dash length at the specified index.
    /// </summary>
    /// <param name="dashIndex">Index of the dash to get.</param>
    /// <returns>The length of the dash. or gap if negative.</returns>
    /// <since>8.0</since>
    public double DashAt(int dashIndex)
    {
      IntPtr pConstThis = ConstPointer();
      return UnsafeNativeMethods.ON_HatchLine_Dash(pConstThis, dashIndex);
    }

    /// <summary>
    /// Add a dash to the pattern.
    /// </summary>
    /// <param name="dash">Length to append, &lt; 0 for a gap.</param>
    /// <since>8.0</since>
    public void AppendDash(double dash)
    {
      IntPtr pThis = NonConstPointer();
      UnsafeNativeMethods.ON_HatchLine_AppendDash(pThis, dash);
    }

    /// <summary>
    /// Sets a new dash array.
    /// </summary>
    /// <param name="dashes">The dash enumeration.</param>
    /// <since>8.0</since>
    public void SetDashes(IEnumerable<double> dashes)
    {
      List<double> list = new List<double>(dashes);
      int count = list.Count;
      if (count > 0)
      {
        IntPtr pThis = NonConstPointer();
        UnsafeNativeMethods.ON_HatchLine_SetDashes(pThis, count, list.ToArray());
      }
    }

    #endregion // Methods

    #region Housekeeping

    /// <summary>
    /// ON_HatchLine*
    /// </summary>
    private IntPtr m_ptr;

    /// <summary>
    /// Gets the constant (immutable) pointer of this object.
    /// </summary>
    internal IntPtr ConstPointer() { return m_ptr; }

    /// <summary>
    /// Gets the non-constant pointer (for modification) of this object.
    /// </summary>
    internal IntPtr NonConstPointer() { return m_ptr; }

    internal HatchLine(IntPtr ptr)
    {
      m_ptr = ptr;
    }

    /// <summary>
    /// Passively releases the unmanaged object.
    /// </summary>
    ~HatchLine()
    {
      Dispose(false);
    }

    /// <summary>
    /// Actively releases the unmanaged object.
    /// </summary>
    /// <since>8.0</since>
    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases the unmanaged object.
    /// </summary>
    protected virtual void Dispose(bool disposing)
    {
      if (IntPtr.Zero != m_ptr)
      {
        UnsafeNativeMethods.ON_HatchLine_Delete(m_ptr);
        m_ptr = IntPtr.Zero;
      }
    }

    #endregion // Housekeeping
  }


  [Serializable]
  public sealed class HatchPattern : ModelComponent
  {
    #region members
    // Represents both a CRhinoHatchPattern and an ON_HatchPattern. When m_ptr is
    // null, the object uses m_doc and m_id to look up the const
    // CRhinoHatchPattern in the hatch pattern table.
    readonly Guid m_id = Guid.Empty;
    // points to one of the static standard hatch patterns
    // get set to IntPtr.Zero when converted into a non-const pointer
    private IntPtr m_constptr_static;
#if RHINO_SDK
    readonly RhinoDoc m_doc;
#endif
    #endregion

    #region constructors
    /// <since>5.0</since>
    public HatchPattern() : base()
    {
      // Creates a new non-document control ON_HatchPattern
      IntPtr pHP = UnsafeNativeMethods.ON_HatchPattern_New();
      ConstructNonConstObject(pHP);
    }
#if RHINO_SDK
    internal HatchPattern(int index, RhinoDoc doc) : base()
    {
      m_id = UnsafeNativeMethods.CRhinoHatchPatternTable_GetHatchPatternId(doc.RuntimeSerialNumber, index);
      m_doc = doc;
      m__parent = m_doc;
    }
#endif
    internal HatchPattern(IntPtr pHatchPattern)
    {
      if (UnsafeNativeMethods.ON_HatchPattern_IsStandardPattern(pHatchPattern))
        m_constptr_static = pHatchPattern;
      else
        ConstructNonConstObject(pHatchPattern);
    }

    internal HatchPattern(Guid id, Rhino.FileIO.File3dm parent)
    {
      m_id = id;
      m__parent = parent;
    }

    // serialization constructor
    private HatchPattern(SerializationInfo info, StreamingContext context)
      : base(info, context)
    {
    }
    #endregion

#if RHINO_SDK
    /// <summary>
    /// Reads hatch pattern definitions from a file.
    /// </summary>
    /// <param name="filename">
    /// Name of an existing file. If filename is null or empty, default hatch pattern filename is used.
    /// </param>
    /// <param name="quiet">
    /// Ignored.
    /// </param>
    /// <returns>An array of hatch patterns. This can be null, but not empty.</returns>
    /// <since>5.0</since>
    public static HatchPattern[] ReadFromFile(string filename, bool quiet)
    {
      if (string.IsNullOrEmpty(filename))
        filename = null;
      int count = UnsafeNativeMethods.RHC_RhinoReadHatchPatterns(filename, quiet);
      if (count < 1)
        return null;
      HatchPattern[] rc = new HatchPattern[count];
      for (int i = 0; i < count; i++)
      {
        IntPtr pHatchPattern = UnsafeNativeMethods.RHC_RhinoReadHatchPatterns2(i);
        if (pHatchPattern != IntPtr.Zero)
          rc[i] = new HatchPattern(pHatchPattern);
      }
      return rc;
    }
#endif

    internal override IntPtr _InternalGetConstPointer()
    {
#if RHINO_SDK
      if (m_doc != null)
      {
        IntPtr rc = UnsafeNativeMethods.CRhinoHatchPatternTable_GetHatchPatternPointer(m_doc.RuntimeSerialNumber, m_id);
        if (rc == IntPtr.Zero)
          throw new Runtime.DocumentCollectedException($"Could not find HatchPattern with ID {m_id}");
        return rc;
      }
#endif
      Rhino.FileIO.File3dm parent_file = m__parent as Rhino.FileIO.File3dm;
      if (parent_file != null)
      {
        IntPtr pModel = parent_file.NonConstPointer();
        return UnsafeNativeMethods.ONX_Model_GetModelComponentPointer(pModel, m_id);
      }
      return m_constptr_static;
    }

    internal override IntPtr NonConstPointer()
    {
      if (m__parent is Rhino.FileIO.File3dm)
        return _InternalGetConstPointer();

      return base.NonConstPointer();
    }

    protected override void OnSwitchToNonConst()
    {
      base.OnSwitchToNonConst();
      m_constptr_static = IntPtr.Zero;
    }

    internal override IntPtr _InternalDuplicate(out bool applymempressure)
    {
      applymempressure = false;
      IntPtr pConstPointer = ConstPointer();
      return UnsafeNativeMethods.ON_Object_Duplicate(pConstPointer);
    }

    #region properties
    /// <summary>
    /// Returns <see cref="ModelComponentType.HatchPattern"/>.
    /// </summary>
    /// <since>6.0</since>
    public override ModelComponentType ComponentType => ModelComponentType.HatchPattern;

#if RHINO_SDK

    /// <summary>
    /// Deleted hatch patterns are kept in the runtime hatch pattern table so that undo
    /// will work with hatch patterns.  Call IsDeleted to determine to determine if
    /// a hatch pattern is deleted.
    /// </summary>
    /// <since>5.0</since>
    public override bool IsDeleted => base.IsDeleted;

    /// <summary>
    /// Rhino allows multiple files to be viewed simultaneously. Hatch patterns in the
    /// document are "normal" or "reference". Reference hatch patterns are not saved.
    /// </summary>
    /// <since>5.0</since>
    public override bool IsReference => base.IsReference;

    /// <summary>
    /// Creates preview line segments of the hatch pattern.
    /// </summary>
    /// <param name="width">The width of the preview.</param>
    /// <param name="height">The height of the preview.</param>
    /// <param name="angle">The rotation angle of the pattern display in radians.</param>
    /// <returns>The preview line segments if successful, an empty array on failure.</returns>
    /// <since>6.8</since>
    public Rhino.Geometry.Line[] CreatePreviewGeometry(int width, int height, double angle)
    {
      IntPtr const_ptr_this = ConstPointer();
      using (var line_array = new Runtime.InteropWrappers.SimpleArrayLine())
      {
        IntPtr ptr_lines = line_array.NonConstPointer();
        int rc = UnsafeNativeMethods.RHC_RhCreateHatchPatternPreviewGeometry(const_ptr_this, width, height, angle, ptr_lines);
        if (rc == 0)
          return new Rhino.Geometry.Line[0];
        return line_array.ToArray();
      }
    }

#endif

    /// <summary>
    /// Index in the hatch pattern table for this pattern. -1 if not in the table.
    /// </summary>
    /// <since>5.0</since>
    public override int Index
    {
      get
      {
        if (!IsDocumentControlled) // this might not be necessary any longer.
          return -1;
        return base.Index;
      }
      set
      {
        base.Index = value;
      }
    }

    /// <summary>
    /// Gets and sets a short description of the pattern.
    /// </summary>
    /// <since>5.0</since>
    public string Description
    {
      get
      {
        IntPtr pConstThis = ConstPointer();
        if (IntPtr.Zero == pConstThis)
          return String.Empty;
        using (var sh = new StringHolder())
        {
          IntPtr pString = sh.NonConstPointer();
          UnsafeNativeMethods.ON_HatchPattern_GetDescription(pConstThis, pString);
          return sh.ToString();
        }
      }
      set
      {
        IntPtr pThis = NonConstPointer();
        UnsafeNativeMethods.ON_HatchPattern_SetDescription(pThis, value);
      }
    }

    /// <summary>
    /// Gets the pattern's fill type.
    /// </summary>
    /// <since>5.0</since>
    public HatchPatternFillType FillType
    {
      get
      {
        IntPtr pConstThis = ConstPointer();
        return (HatchPatternFillType)UnsafeNativeMethods.ON_HatchPattern_GetFillType(pConstThis);
      }
      set
      {
        IntPtr pThis = NonConstPointer();
        UnsafeNativeMethods.ON_HatchPattern_SetFillType(pThis, (int)value);
      }
    }

    /////////////////////////////////////////////////////////////////
    // Interface functions for line hatches

    /// <summary>
    /// Get the number of HatchLines in the pattern.
    /// </summary>
    /// <since>8.0</since>
    public int HatchLineCount
    {
      get
      {
        IntPtr pConstThis = ConstPointer();
        return UnsafeNativeMethods.ON_HatchPattern_HatchLineCount(pConstThis);
      }
    }

    /// <summary>
    /// Add a HatchLine to the pattern.
    /// </summary>
    /// <param name="hatchLine">The hatch line to add.</param>
    /// <returns>The index of newly added hatch line, or -1 on failure.</returns>
    /// <since>8.0</since>
    public int AddHatchLine(HatchLine hatchLine)
    {
      IntPtr pThis = NonConstPointer();
      IntPtr pConstHatchLine = hatchLine.ConstPointer();
      return UnsafeNativeMethods.ON_HatchPattern_AddHatchLine(pThis, pConstHatchLine);
    }

    /// <summary>
    /// Gets a HatchLine at an index.
    /// </summary>
    /// <param name="hatchLineIndex">The index of the hatch line.</param>
    /// <returns>The hatch line, or null on failure.</returns>
    /// <since>8.0</since>
    public HatchLine HatchLineAt(int hatchLineIndex)
    {
      IntPtr pConstThis = ConstPointer();
      IntPtr pHatchLine = UnsafeNativeMethods.ON_HatchPattern_HatchLine(pConstThis, hatchLineIndex);
      if (IntPtr.Zero != pHatchLine)
        return new HatchLine(pHatchLine);
      return null;
    }

    /// <summary>
    /// Remove a hatch line from the pattern.
    /// </summary>
    /// <param name="hatchLineIndex">The index of the hatch line to remove.</param>
    /// <returns>True if successful, false if index is out of range.</returns>
    /// <since>8.0</since>
    public bool RemoveHatchLine(int hatchLineIndex)
    {
      IntPtr pThis = NonConstPointer();
      return UnsafeNativeMethods.ON_HatchPattern_RemoveHatchLine(pThis, hatchLineIndex);
    }

    /// <summary>
    /// Remove all of the hatch line from the pattern.
    /// </summary>
    /// <since>8.0</since>
    public void RemoveAllHatchLines()
    {
      IntPtr pThis = NonConstPointer();
      UnsafeNativeMethods.ON_HatchPattern_RemoveAllHatchLines(pThis);
    }

    /// <summary>
    /// Gets all the hatch lines.
    /// </summary>
    /// <since>8.0</since>
    public IEnumerable<HatchLine> HatchLines
    {
      get
      {
        int count = HatchLineCount;
        for (int i = 0; i < count; i++)
        {
          yield return HatchLineAt(i);
        }
      }
    }

    /// <summary>
    /// Set all of the hatch lines at once. Existing hatch lines are deleted.
    /// </summary>
    /// <param name="hatchLines">An enumeration of hatch lines.</param>
    /// <returns>The number of hatch lines added.</returns>
    /// <since>8.0</since>
    public int SetHatchLines(IEnumerable<HatchLine> hatchLines)
    {
      using (SimpleArrayHatchLinePointer hatchLinePointers = new SimpleArrayHatchLinePointer(hatchLines))
      {
        IntPtr pHatchLines = hatchLinePointers.ConstPointer();
        IntPtr pThis = NonConstPointer();
        return UnsafeNativeMethods.ON_HatchPattern_SetHatchLines(pThis, pHatchLines);
      }
    }

    //
    /////////////////////////////////////////////////////////////////

    public static class Defaults
    {
      /// <since>6.0</since>
      public static HatchPattern Solid
      {
        get
        {
          IntPtr const_ptr = UnsafeNativeMethods.ON_HatchPattern_Static(UnsafeNativeMethods.HatchPatternType.Solid);
          return new HatchPattern(const_ptr);
        }
      }

      /// <since>6.0</since>
      public static HatchPattern Hatch1
      {
        get
        {
          IntPtr const_ptr = UnsafeNativeMethods.ON_HatchPattern_Static(UnsafeNativeMethods.HatchPatternType.Hatch1);
          return new HatchPattern(const_ptr);
        }
      }

      /// <since>6.0</since>
      public static HatchPattern Hatch2
      {
        get
        {
          IntPtr const_ptr = UnsafeNativeMethods.ON_HatchPattern_Static(UnsafeNativeMethods.HatchPatternType.Hatch2);
          return new HatchPattern(const_ptr);
        }
      }

      /// <since>6.0</since>
      public static HatchPattern Hatch3
      {
        get
        {
          IntPtr const_ptr = UnsafeNativeMethods.ON_HatchPattern_Static(UnsafeNativeMethods.HatchPatternType.Hatch3);
          return new HatchPattern(const_ptr);
        }
      }

      /// <since>6.0</since>
      public static HatchPattern Dash
      {
        get
        {
          IntPtr const_ptr = UnsafeNativeMethods.ON_HatchPattern_Static(UnsafeNativeMethods.HatchPatternType.HatchDash);
          return new HatchPattern(const_ptr);
        }
      }

      /// <since>6.0</since>
      public static HatchPattern Grid
      {
        get
        {
          IntPtr const_ptr = UnsafeNativeMethods.ON_HatchPattern_Static(UnsafeNativeMethods.HatchPatternType.Grid);
          return new HatchPattern(const_ptr);
        }
      }

      /// <since>6.0</since>
      public static HatchPattern Grid60
      {
        get
        {
          IntPtr const_ptr = UnsafeNativeMethods.ON_HatchPattern_Static(UnsafeNativeMethods.HatchPatternType.Grid60);
          return new HatchPattern(const_ptr);
        }
      }

      /// <since>6.0</since>
      public static HatchPattern Plus
      {
        get
        {
          IntPtr const_ptr = UnsafeNativeMethods.ON_HatchPattern_Static(UnsafeNativeMethods.HatchPatternType.Plus);
          return new HatchPattern(const_ptr);
        }
      }

      /// <since>6.0</since>
      public static HatchPattern Squares
      {
        get
        {
          IntPtr const_ptr = UnsafeNativeMethods.ON_HatchPattern_Static(UnsafeNativeMethods.HatchPatternType.Squares);
          return new HatchPattern(const_ptr);
        }
      }
    }

    #endregion
  }
}

#if RHINO_SDK
namespace Rhino.DocObjects.Tables
{
  /// <summary>
  /// All of the hatch pattern definitions contained in a rhino document.
  /// </summary>
  public sealed class HatchPatternTable :
    RhinoDocCommonTable<HatchPattern>, ICollection<HatchPattern>
  {
    internal HatchPatternTable(RhinoDoc doc) : base(doc)
    {
      UnsafeNativeMethods.CRhinoHatchPatternTable_Init(m_doc.RuntimeSerialNumber);
    }

    /// <summary>
    /// Conceptually, the hatch pattern table is an array of hatch patterns.
    /// The operator[] can be used to get individual hatch patterns. A hatch pattern is
    /// either active or deleted and this state is reported by HatchPattern.IsDeleted.
    /// </summary>
    /// <param name="index">zero based array index.</param>
    /// <returns>
    /// If index is out of range, the current hatch pattern is returned.
    /// </returns>
    public DocObjects.HatchPattern this[int index]
    {
      get
      {
        if (index < 0 || index >= Count)
          index = CurrentHatchPatternIndex;
        return new Rhino.DocObjects.HatchPattern(index, m_doc);
      }
    }

    /// <summary>
    /// At all times, there is a "current" hatch pattern.  Unless otherwise
    /// specified, new objects are assigned to the current hatch pattern.
    /// The current hatch pattern is never locked, hidden, or deleted.
    /// </summary>
    /// <example>
    /// <code source='examples\vbnet\ex_hatchcurve.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_hatchcurve.cs' lang='cs'/>
    /// <code source='examples\py\ex_hatchcurve.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    public int CurrentHatchPatternIndex
    {
      get
      {
        return UnsafeNativeMethods.CRhinoHatchPatternTable_GetCurrentIndex(m_doc.RuntimeSerialNumber);
      }
      set
      {
        UnsafeNativeMethods.CRhinoHatchPatternTable_SetCurrentIndex(m_doc.RuntimeSerialNumber, value);
      }
    }

    /// <since>6.0</since>
    public override ModelComponentType ComponentType
    {
      get
      {
        return ModelComponentType.HatchPattern;
      }
    }

    /// <summary>
    /// Finds the hatch pattern with a given name. Search ignores case.
    /// </summary>
    /// <param name="name">The name of the hatch patter to be found.</param>
    /// <param name="ignoreDeleted">true means don't search deleted hatch patterns.</param>
    /// <returns>Index of the hatch pattern with the given name. -1 if no hatch pattern found.</returns>
    /// <example>
    /// <code source='examples\vbnet\ex_hatchcurve.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_hatchcurve.cs' lang='cs'/>
    /// <code source='examples\py\ex_hatchcurve.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    /// <deprecated>6.0</deprecated>
    [Obsolete("ignoreDeleted is now ignored. Items are removed permanently now. Use FindName.")]
    public int Find(string name, bool ignoreDeleted)
    {
      var hatch = FindName(name);
      return hatch == null ? -1 : hatch.Index;
    }

    /// <summary>
    /// Finds the hatch pattern with a given name. Search ignores case.
    /// </summary>
    /// <param name="name">The name of the hatch patter to be found.</param>
    /// <returns>Hatch pattern with the given name. Null if no hatch pattern found.</returns>
    /// <example>
    /// <code source='examples\vbnet\ex_hatchcurve.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_hatchcurve.cs' lang='cs'/>
    /// <code source='examples\py\ex_hatchcurve.py' lang='py'/>
    /// </example>
    /// <since>6.0</since>
    public HatchPattern FindName(string name)
    {
      return __FindNameInternal(name);
    }

    /// <summary>
    /// Finds a HatchPattern given its name hash.
    /// </summary>
    /// <param name="nameHash">The name hash of the HatchPattern to be searched.</param>
    /// <returns>An Linetype, or null on error.</returns>
    /// <since>6.0</since>
    public HatchPattern FindNameHash(NameHash nameHash)
    {
      return __FindNameHashInternal(nameHash);
    }

    /// <summary>
    /// Retrieves a HatchPattern object based on Index. This search type of search is discouraged.
    /// We are moving towards using only IDs for all tables.
    /// </summary>
    /// <param name="index">The index to search for.</param>
    /// <returns>A HatchPattern object, or null if none was found.</returns>
    /// <since>6.0</since>
    public HatchPattern FindIndex(int index)
    {
      return __FindIndexInternal(index);
    }

    /// <summary>
    /// Adds a new hatch pattern with specified definition to the table.
    /// </summary>
    /// <param name="pattern">
    /// definition of new hatch pattern. The information in pattern is copied.
    /// If patern.Name is empty the a unique name of the form "HatchPattern 01"
    /// will be automatically created.
    /// </param>
    /// <returns>
    /// >=0 index of new hatch pattern
    /// -1  not added because a hatch pattern with that name already exists or
    /// some other problem occurred.
    /// </returns>
    /// <since>5.0</since>
    public int Add(Rhino.DocObjects.HatchPattern pattern)
    {
      if (null == pattern)
        return -1;
      IntPtr pPattern = pattern.ConstPointer();
      return UnsafeNativeMethods.CRhinoHatchPatternTable_AddPattern(m_doc.RuntimeSerialNumber, pPattern, false);
    }

    /// <since>5.0</since>
    void ICollection<HatchPattern>.Add(HatchPattern item)
    {
      if (Add(item) < 0)
        throw new NotSupportedException("Could not add HatchPattern.");
    }

    #region enumerator

    /// <summary>
    /// Modify hatch pattern settings.
    /// </summary>
    /// <param name="hatchPattern">Definition of new hatch pattern. The information in the hatch pattern is copied.</param>
    /// <param name="hatchPatternIndex">Zero based index of the hatch pattern to modify.</param>
    /// <param name="quiet">If true, information message boxes pop up when illegal changes are attempted.</param>
    /// <returns>True if successful, or false if hatchPatternIndex is out of range.</returns>
    /// <since>8.0</since>
    public bool Modify(HatchPattern hatchPattern, int hatchPatternIndex, bool quiet)
    {
      if (null == hatchPattern) return false;
      IntPtr ptr_const_pattern = hatchPattern.ConstPointer();
      return UnsafeNativeMethods.CRhinoHatchPatternTable_Modify(m_doc.RuntimeSerialNumber, ptr_const_pattern, hatchPatternIndex, quiet);
    }

    /// <summary>
    /// Deletes a hatch pattern from the table.
    /// </summary>
    /// <param name="item">The hatch pattern to delete.</param>
    /// <returns>true if successful, false otherwise.</returns>
    /// <since>6.0</since>
    public override bool Delete(HatchPattern item)
    {
      return Delete(item, true);
    }

    /// <summary>
    /// Deletes a hatch pattern from the table.
    /// </summary>
    /// <param name="item">The hatch pattern to delete.</param>
    /// <param name="quiet">If true, no warning message box appears if hatch pattern cannot be deleted.</param>
    /// <returns>true if successful, false otherwise.</returns>
    /// <since>6.0</since>
    public bool Delete(HatchPattern item, bool quiet)
    {
      if (null == item) return false;
      IntPtr pPattern = item.ConstPointer();
      return UnsafeNativeMethods.CRhinoHatchPatternTable_DeleteHatchPattern(m_doc.RuntimeSerialNumber, pPattern, quiet);
    }

    /// <summary>
    /// Deletes a hatch pattern from the table.
    /// </summary>
    /// <param name="hatchPatternIndex">The index of the hatch pattern to delete.</param>
    /// <returns>true if successful, false otherwise.</returns>
    /// <since>7.0</since>
    public bool Delete(int hatchPatternIndex)
    {
      return Delete(hatchPatternIndex, true);
    }

    /// <summary>
    /// Deletes a hatch pattern from the table.
    /// </summary>
    /// <param name="hatchPatternIndex">The index of the hatch pattern to delete.</param>
    /// <param name="quiet">If true, no warning message box appears if hatch pattern cannot be deleted.</param>
    /// <returns>true if successful, false otherwise.</returns>
    /// <since>7.0</since>
    public bool Delete(int hatchPatternIndex, bool quiet)
    {
      return UnsafeNativeMethods.CRhinoHatchPatternTable_DeleteHatchPattern2(m_doc.RuntimeSerialNumber, hatchPatternIndex, quiet);
    }

    /// <summary>
    /// Renames a hatch pattern in the table.
    /// </summary>
    /// <param name="item">The hatch pattern to rename</param>
    /// <param name="hatchPatternName">The new hatch pattern name.</param>
    /// <returns>true if successful, false otherwise.</returns>
    /// <since>7.0</since>
    public bool Rename(HatchPattern item, string hatchPatternName)
    {
      if (null == item) return false;
      if (string.IsNullOrEmpty(hatchPatternName)) return false;
      IntPtr pPattern = item.ConstPointer();
      return UnsafeNativeMethods.CRhinoHatchPatternTable_RenameHatchPattern2(m_doc.RuntimeSerialNumber, pPattern, hatchPatternName);
    }

    /// <summary>
    /// Renames a hatch pattern in the table.
    /// </summary>
    /// <param name="hatchPatternIndex">The index of the hatch pattern to rename.</param>
    /// <param name="hatchPatternName">The new hatch pattern name.</param>
    /// <returns>true if successful, false otherwise.</returns>
    /// <since>7.0</since>
    public bool Rename(int hatchPatternIndex, string hatchPatternName)
    {
      if (string.IsNullOrEmpty(hatchPatternName)) return false;
      return UnsafeNativeMethods.CRhinoHatchPatternTable_RenameHatchPattern(m_doc.RuntimeSerialNumber, hatchPatternIndex, hatchPatternName);
    }

    #endregion
  }
}
#endif
