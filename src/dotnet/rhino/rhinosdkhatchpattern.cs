#pragma warning disable 1591
using System;
using System.Runtime.Serialization;
using System.Collections.Generic;
using System.Drawing;
using System.Linq.Expressions;
using Rhino.Runtime.InteropWrappers;
using Rhino.FileIO;

namespace Rhino.DocObjects
{
  public enum HatchPatternFillType
  {
    Solid = 0,
    Lines = 1,
    Gradient = 2
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
      : base (info, context)
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
      if (parent_file!=null)
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
#if RHINO_SDK
    const int idxIsDeleted = 0;
    const int idxIsReference = 1;

    /// <summary>
    /// Deleted hatch patterns are kept in the runtime hatch pattern table so that undo
    /// will work with hatch patterns.  Call IsDeleted to determine to determine if
    /// a hatch pattern is deleted.
    /// </summary>
    public bool IsDeleted
    {
      get
      {
        if (!IsDocumentControlled)
          return false;
        IntPtr pConstThis = ConstPointer();
        return UnsafeNativeMethods.CRhinoHatchPattern_GetBool(pConstThis, idxIsDeleted);
      }
    }

    /// <summary>
    /// Rhino allows multiple files to be viewed simultaneously. Hatch patterns in the
    /// document are "normal" or "reference". Reference hatch patterns are not saved.
    /// </summary>
    public bool IsReference
    {
      get
      {
        if (!IsDocumentControlled)
          return false;
        IntPtr pConstThis = ConstPointer();
        return UnsafeNativeMethods.CRhinoHatchPattern_GetBool(pConstThis, idxIsReference);
      }
    }

    /// <summary>
    /// Creates preview line segments of the hatch pattern.
    /// </summary>
    /// <param name="width">The width of the preview.</param>
    /// <param name="height">The height of the preview.</param>
    /// <param name="angle">The rotation angle of the pattern display in radians.</param>
    /// <returns>The preview line segments if successful, an empty array on failure.</returns>
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
    /// Returns <see cref="ModelComponentType.HatchPattern"/>.
    /// </summary>
    public override ModelComponentType ComponentType
    {
      get
      {
        return ModelComponentType.HatchPattern;
      }
    }

    /// <summary>
    /// Index in the hatch pattern table for this pattern. -1 if not in the table.
    /// </summary>
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

    public static class Defaults
    {
      public static HatchPattern Solid
      {
        get
        {
          IntPtr const_ptr = UnsafeNativeMethods.ON_HatchPattern_Static(UnsafeNativeMethods.HatchPatternType.Solid);
          return new HatchPattern(const_ptr);
        }
      }

      public static HatchPattern Hatch1
      {
        get
        {
          IntPtr const_ptr = UnsafeNativeMethods.ON_HatchPattern_Static(UnsafeNativeMethods.HatchPatternType.Hatch1);
          return new HatchPattern(const_ptr);
        }
      }

      public static HatchPattern Hatch2
      {
        get
        {
          IntPtr const_ptr = UnsafeNativeMethods.ON_HatchPattern_Static(UnsafeNativeMethods.HatchPatternType.Hatch2);
          return new HatchPattern(const_ptr);
        }
      }

      public static HatchPattern Hatch3
      {
        get
        {
          IntPtr const_ptr = UnsafeNativeMethods.ON_HatchPattern_Static(UnsafeNativeMethods.HatchPatternType.Hatch3);
          return new HatchPattern(const_ptr);
        }
      }

      public static HatchPattern Dash
      {
        get
        {
          IntPtr const_ptr = UnsafeNativeMethods.ON_HatchPattern_Static(UnsafeNativeMethods.HatchPatternType.HatchDash);
          return new HatchPattern(const_ptr);
        }
      }

      public static HatchPattern Grid
      {
        get
        {
          IntPtr const_ptr = UnsafeNativeMethods.ON_HatchPattern_Static(UnsafeNativeMethods.HatchPatternType.Grid);
          return new HatchPattern(const_ptr);
        }
      }

      public static HatchPattern Grid60
      {
        get
        {
          IntPtr const_ptr = UnsafeNativeMethods.ON_HatchPattern_Static(UnsafeNativeMethods.HatchPatternType.Grid60);
          return new HatchPattern(const_ptr);
        }
      }

      public static HatchPattern Plus
      {
        get
        {
          IntPtr const_ptr = UnsafeNativeMethods.ON_HatchPattern_Static(UnsafeNativeMethods.HatchPatternType.Plus);
          return new HatchPattern(const_ptr);
        }
      }

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
    [Obsolete("ignoreDeleted is now ignored. Items are removed permanenently now. Use FindName.")]
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
    public HatchPattern FindName(string name)
    {
      return __FindNameInternal(name);
    }

    /// <summary>
    /// Finds a HatchPattern given its name hash.
    /// </summary>
    /// <param name="nameHash">The name hash of the HatchPattern to be searched.</param>
    /// <returns>An Linetype, or null on error.</returns>
    public HatchPattern FindNameHash(NameHash nameHash)
    {
      return __FindNameHashInternal(nameHash);
    }

    /// <summary>
    /// Retrieves a HatchPattern object based on Index. This seach type of search is discouraged.
    /// We are moving towards using only IDs for all tables.
    /// </summary>
    /// <param name="index">The index to search for.</param>
    /// <returns>A HatchPattern object, or null if none was found.</returns>
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
    /// some other problem occured.
    /// </returns>
    public int Add(Rhino.DocObjects.HatchPattern pattern)
    {
      if (null == pattern)
        return -1;
      IntPtr pPattern = pattern.ConstPointer();
      return UnsafeNativeMethods.CRhinoHatchPatternTable_AddPattern(m_doc.RuntimeSerialNumber, pPattern, false);
    }

    void ICollection<HatchPattern>.Add(HatchPattern item)
    {
      if (Add(item) < 0)
        throw new NotSupportedException("Could not add HatchPattern.");
    }

    #region enumerator

    public override bool Delete(HatchPattern item)
    {
      return Delete(item, true);
    }

    public bool Delete(HatchPattern item, bool quiet)
    {
      if (null == item) return false;
      IntPtr pPattern = item.ConstPointer();
      return UnsafeNativeMethods.CRhinoHatchPatternTable_DeleteHatchPattern(m_doc.RuntimeSerialNumber, pPattern, quiet);
    }
    #endregion
  }
}
#endif