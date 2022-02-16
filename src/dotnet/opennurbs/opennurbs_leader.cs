using System;
using System.Runtime.Serialization;
using Rhino.DocObjects;
using Rhino.Runtime.InteropWrappers;


namespace Rhino.Geometry
{

  /// <summary> Arrowhead used by annotation </summary>
  public class Arrowhead
  {
    /// <summary> Constructor </summary>
    /// <since>6.0</since>
    public Arrowhead() : this(DimensionStyle.ArrowType.SolidTriangle, Guid.Empty)
    {
    }

    /// <summary> Constructor </summary>  
    /// <param name="arrowType"> type of this arrowhead </param>
    /// <param name="blockId"> Guid of the block used for user defined display </param>
    /// <since>6.0</since>
    public Arrowhead(DimensionStyle.ArrowType arrowType, Guid blockId)
    {
      BlockId = blockId;
      ArrowType = arrowType;
    }

    /// <summary> Id of block used for user-defined arrowhead </summary>
    /// <since>6.0</since>
    public Guid BlockId { get; }

    /// <summary> Type of arrowhead used by annotation </summary>
    /// <since>6.0</since>
    public DimensionStyle.ArrowType ArrowType { get; }
  }


  /// <summary> Leader geometry class </summary>
  [Serializable]
  public class Leader : AnnotationBase
  {
    /// <summary>
    /// Protected serialization constructor for internal use.
    /// </summary>
    /// <param name="info">Serialization data.</param>
    /// <param name="context">Serialization stream.</param>
    protected Leader(SerializationInfo info, StreamingContext context)
      : base(info, context)
    {
    }

    /// <summary>
    /// internal constructor
    /// </summary>
    /// <param name="nativePointer"></param>
    /// <param name="parent"></param>
    internal Leader(IntPtr nativePointer, object parent)
      : base(nativePointer, parent)
    {
    }

    /// <summary> Constructor </summary>
    /// <since>6.0</since>
    public Leader()
    {
      var ptr = UnsafeNativeMethods.ON_V6_Leader_New();
      ConstructNonConstObject(ptr);
    }

    /// <summary>
    ///  Creates a Leader geometry object
    /// </summary>
    /// <param name="text"></param>
    /// <param name="plane"></param>
    /// <param name="dimstyle"></param>
    /// <param name="points"></param>
    /// <returns></returns>
    /// <since>6.0</since>
    public static Leader Create(string text, Plane plane, DimensionStyle dimstyle, Point3d[] points)
    {
      return CreateWithRichText(AnnotationBase.PlainTextToRtf(text), plane, dimstyle, points);
    }

    /// <summary>
    ///  Creates a Leader geometry object
    /// </summary>
    /// <param name="richText"></param>
    /// <param name="plane"></param>
    /// <param name="dimstyle"></param>
    /// <param name="points"></param>
    /// <returns></returns>
    /// <since>6.0</since>
    public static Leader CreateWithRichText(string richText, Plane plane, DimensionStyle dimstyle, Point3d[] points)
    {
      IntPtr const_ptr_dimstyle = dimstyle.ConstPointer();
      IntPtr ptr_leader = UnsafeNativeMethods.ON_V6_Leader_Create(richText, ref plane, points.Length, points, const_ptr_dimstyle);
      if (IntPtr.Zero == ptr_leader)
        return null;
      var rc = new Leader(ptr_leader, null);
      rc.ParentDimensionStyle = dimstyle;
      return rc;
    }

    private NurbsCurve m_curve;
    /// <summary> Gets the curve used by this leader </summary>
    /// <since>6.0</since>
    public NurbsCurve Curve
    {
      get
      {
        if (m_curve == null)
        {
          IntPtr const_ptr_this = ConstPointer();
          IntPtr const_ptr_crv = UnsafeNativeMethods.ON_V6_Leader_Curve(const_ptr_this, IntPtr.Zero); //Null Dimstyle
          m_curve = CreateGeometryHelper(const_ptr_crv, new CurveHolder(this)) as NurbsCurve;
        }
        return m_curve;
      }
    }
   
    /// <summary>
    /// Get or set the 2d points defining the curve used by this leader
    /// </summary>
    /// <since>6.0</since>
    public Point2d[] Points2D
    {
      get
      {
        using(var pointsarray = new SimpleArrayPoint2d())
        {
          var points = pointsarray.NonConstPointer();
          IntPtr const_ptr_this = ConstPointer();
          UnsafeNativeMethods.ON_V6_Leader_Get2dPoints(const_ptr_this, points);
          return pointsarray.ToArray();
        }
      }
      set
      {
        var ptr_this = NonConstPointer();
        UnsafeNativeMethods.ON_V6_Leader_Set2dPoints(ptr_this, value.Length, value);
      }
    }

    /// <summary>
    /// Get or set the 3d points defining the curve used by this leader
    /// </summary>
    /// <since>6.0</since>
    public Point3d[] Points3D
    {
      get
      {
        var p2d = Points2D;
        var points3d = new Point3d[p2d.Length];
        for(int i = 0; i < p2d.Length; i++)
        {
          points3d[i] = Plane.PointAt(p2d[i].X, p2d[i].Y);
        }
        return points3d;
      }
      set
      {
        IntPtr ptr_this = NonConstPointer();
        UnsafeNativeMethods.ON_V6_Leader_Set3dPoints(ptr_this, value.Length, value);
      }
    }

    #region properties originating from dim style that can be overridden

    /// <summary>
    /// Gets or sets the horizontal alignment of the leader's text
    /// </summary>
    /// <since>6.0</since>
    public TextHorizontalAlignment LeaderTextHorizontalAlignment
    {
      get
      {
        IntPtr thisptr = ConstPointer();
        IntPtr styleptr = ConstParentDimStylePointer();
        return UnsafeNativeMethods.ON_V6_Annotation_LeaderTextHorizontalAlignment(thisptr, styleptr);
      }
      set
      {
        IntPtr thisptr = NonConstPointer();
        IntPtr styleptr = ConstParentDimStylePointer();
        UnsafeNativeMethods.ON_V6_Annotation_SetLeaderTextHorizontalAlignment(thisptr, styleptr, value);
      }
    }

    /// <summary>
    /// Gets or sets the vertical alignment of the leader's text
    /// </summary>
    /// <since>6.0</since>
    public TextVerticalAlignment LeaderTextVerticalAlignment
    {
      get
      {
        IntPtr thisptr = ConstPointer();
        IntPtr styleptr = ConstParentDimStylePointer();
        return UnsafeNativeMethods.ON_V6_Annotation_LeaderTextVerticalAlignment(thisptr, styleptr);
      }
      set
      {
        IntPtr thisptr = NonConstPointer();
        IntPtr styleptr = ConstParentDimStylePointer();
        UnsafeNativeMethods.ON_V6_Annotation_SetLeaderTextVerticalAlignment(thisptr, styleptr, value);
      }
    }

    /// <summary>
    /// The arrowhead type for the leader
    /// </summary>
    /// <since>6.0</since>
    public DimensionStyle.ArrowType LeaderArrowType
    {
      get
      {
        IntPtr thisptr = ConstPointer();
        IntPtr styleptr = ConstParentDimStylePointer();
        return UnsafeNativeMethods.ON_V6_Annotation_LeaderArrowType(thisptr, styleptr);
      }
      set
      {
        IntPtr thisptr = NonConstPointer();
        IntPtr styleptr = ConstParentDimStylePointer();
        UnsafeNativeMethods.ON_V6_Annotation_SetLeaderArrowType(thisptr, styleptr, value);
      }
    }

    /// <summary>
    /// Id of the block used as the arrow for the leader when the arrow type is 'User arrow'
    /// </summary>
    /// <since>6.0</since>
    public Guid LeaderArrowBlockId
    {
      get
      {
        IntPtr thisptr = ConstPointer();
        IntPtr styleptr = ConstParentDimStylePointer();
        return UnsafeNativeMethods.ON_V6_Annotation_LeaderArrowBlockId(thisptr, styleptr);
      }
      set
      {
        IntPtr thisptr = NonConstPointer();
        IntPtr styleptr = ConstParentDimStylePointer();
        UnsafeNativeMethods.ON_V6_Annotation_SetLeaderArrowBlockId(thisptr, styleptr, value);
      }
    }

    /// <summary>
    /// The size of the leader arrow
    /// </summary>
    /// <since>6.0</since>
    public double LeaderArrowSize
    {
      get
      {
        IntPtr thisptr = ConstPointer();
        IntPtr styleptr = ConstParentDimStylePointer();
        return UnsafeNativeMethods.ON_V6_Annotation_LeaderArrowSize(thisptr, styleptr);
      }
      set
      {
        IntPtr thisptr = NonConstPointer();
        IntPtr styleptr = ConstParentDimStylePointer();
        UnsafeNativeMethods.ON_V6_Annotation_SetLeaderArrowSize(thisptr, styleptr, value);
      }
    }

    /// <summary>
    /// The style of the leader curve: polyline or spline
    /// </summary>
    /// <since>6.0</since>
    public DimensionStyle.LeaderCurveStyle LeaderCurveStyle
    {
      get
      {
        IntPtr thisptr = ConstPointer();
        IntPtr styleptr = ConstParentDimStylePointer();
        return UnsafeNativeMethods.ON_V6_Annotation_LeaderCurveType(thisptr, styleptr);
      }
      set
      {
        IntPtr thisptr = NonConstPointer();
        IntPtr styleptr = ConstParentDimStylePointer();
        UnsafeNativeMethods.ON_V6_Annotation_SetLeaderCurveType(thisptr, styleptr, value);
      }
    }

    /// <summary>
    /// Angle for text of leader text
    /// </summary>
    /// <since>6.0</since>
    public DimensionStyle.LeaderContentAngleStyle LeaderContentAngleStyle
    {
      get
      {
        IntPtr thisptr = ConstPointer();
        IntPtr styleptr = ConstParentDimStylePointer();
        return UnsafeNativeMethods.ON_V6_Annotation_LeaderContentAngleStyle(thisptr, styleptr);
      }
      set
      {
        IntPtr thisptr = NonConstPointer();
        IntPtr styleptr = ConstParentDimStylePointer();
        UnsafeNativeMethods.ON_V6_Annotation_SetLeaderContentAngleStyle(thisptr, styleptr, value);
      }
    }

    /// <summary>
    /// Returns true if the leader has a landing line
    /// </summary>
    /// <since>6.0</since>
    public bool LeaderHasLanding
    {
      get
      {
        IntPtr thisptr = ConstPointer();
        IntPtr styleptr = ConstParentDimStylePointer();
        return UnsafeNativeMethods.ON_V6_Annotation_LeaderHasLanding(thisptr, styleptr);
      }
      set
      {
        IntPtr thisptr = NonConstPointer();
        IntPtr styleptr = ConstParentDimStylePointer();
        UnsafeNativeMethods.ON_V6_Annotation_SetLeaderHasLanding(thisptr, styleptr, value);
      }
    }
    
    /// <summary>
    /// Gets or sets the length of the landing line
    /// </summary>
    /// <since>6.0</since>
    public double LeaderLandingLength
    {
      get
      {
        IntPtr thisptr = ConstPointer();
        IntPtr styleptr = ConstParentDimStylePointer();
        return UnsafeNativeMethods.ON_V6_Annotation_LeaderLandingLength(thisptr, styleptr);
      }
      set
      {
        IntPtr thisptr = NonConstPointer();
        IntPtr styleptr = ConstParentDimStylePointer();
        UnsafeNativeMethods.ON_V6_Annotation_SetLeaderLandingLength(thisptr, styleptr, value);
      }
    }
    #endregion properties originating from dim style that can be overridden
  }
}

