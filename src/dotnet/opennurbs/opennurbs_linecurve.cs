using System;
using Rhino.Display;
using System.Runtime.Serialization;

namespace Rhino.Geometry
{
  /// <summary>
  /// Represents a linear curve.
  /// </summary>
  [Serializable]
  public class LineCurve : Curve
  {
    /// <summary>
    /// Initializes a new instance of the <see cref="LineCurve"/> class.
    /// </summary>
    /// <since>5.0</since>
    public LineCurve()
    {
      IntPtr ptr = UnsafeNativeMethods.ON_LineCurve_New(IntPtr.Zero);
      ConstructNonConstObject(ptr);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LineCurve"/> class, by
    /// copying values from another linear curve.
    /// </summary>
    /// <since>5.0</since>
    public LineCurve(LineCurve other)
    {
      IntPtr pOther = IntPtr.Zero;
      if( null != other )
        pOther = other.ConstPointer();
      IntPtr ptr = UnsafeNativeMethods.ON_LineCurve_New(pOther);
      ConstructNonConstObject(ptr);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LineCurve"/> class, by
    /// setting start and end point from two <see cref="Point2d">2D points</see>.</summary>
    /// <param name="from">A start point.</param>
    /// <param name="to">An end point.</param>
    /// <since>5.0</since>
    public LineCurve(Point2d from, Point2d to)
    {
      IntPtr ptr = UnsafeNativeMethods.ON_LineCurve_New2(from,to);
      ConstructNonConstObject(ptr);
    }
    /// <example>
    /// <code source='examples\vbnet\ex_addtruncatedcone.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_addtruncatedcone.cs' lang='cs'/>
    /// <code source='examples\py\ex_addtruncatedcone.py' lang='py'/>
    /// </example>
    /// <summary>
    /// Initializes a new instance of the <see cref="LineCurve"/> class, by
    /// setting start and end point from two <see cref="Point3d">3D points</see>.</summary>
    /// <param name="from">A start point.</param>
    /// <param name="to">An end point.</param>
    /// <since>5.0</since>
    public LineCurve(Point3d from, Point3d to)
    {
      IntPtr ptr = UnsafeNativeMethods.ON_LineCurve_New3(from, to);
      ConstructNonConstObject(ptr);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LineCurve"/> class, by
    /// retrieving its value from a <see cref="Line">line</see>.
    /// </summary>
    /// <param name="line">A line to use as model.</param>
    /// <since>5.0</since>
    public LineCurve(Line line)
    {
      IntPtr ptr = UnsafeNativeMethods.ON_LineCurve_New3(line.From, line.To);
      ConstructNonConstObject(ptr);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LineCurve"/> class, by
    /// retrieving its value from a <see cref="Line">line</see> and setting the domain.
    /// </summary>
    /// <param name="line">A line to use as model.</param>
    /// <param name="t0">The new domain start.</param>
    /// <param name="t1">The new domain end.</param>
    /// <since>5.0</since>
    public LineCurve(Line line, double t0, double t1)
    {
      IntPtr ptr = UnsafeNativeMethods.ON_LineCurve_New4(line.From, line.To, t0, t1);
      ConstructNonConstObject(ptr);
    }
    internal LineCurve(IntPtr ptr, object parent, int subobject_index)
      : base(ptr, parent, subobject_index)
    {
    }

    /// <summary>
    /// Protected constructor used in serialization.
    /// </summary>
    /// <param name="info">Serialization data.</param>
    /// <param name="context">Serialization stream.</param>
    protected LineCurve(SerializationInfo info, StreamingContext context)
      : base (info, context)
    {
    }

    internal override GeometryBase DuplicateShallowHelper()
    {
      return new LineCurve(IntPtr.Zero, null, -1);
    }

#if RHINO_SDK
    internal override void Draw(DisplayPipeline pipeline, System.Drawing.Color color, int thickness)
    {
      IntPtr ptr = ConstPointer();
      IntPtr pDisplayPipeline = pipeline.NonConstPointer();
      int argb = color.ToArgb();
      UnsafeNativeMethods.ON_LineCurve_Draw(ptr, pDisplayPipeline, argb, thickness);
    }
#endif

    /// <summary>
    /// Gets or sets the Line value inside this curve.
    /// </summary>
    /// <since>5.0</since>
    public Line Line
    {
      get
      {
        IntPtr ptr = ConstPointer();
        Line line = new Line();
        UnsafeNativeMethods.ON_LineCurve_GetSetLine(ptr, false, ref line);
        return line;
      }
      set
      {
        IntPtr ptr = NonConstPointer();
        UnsafeNativeMethods.ON_LineCurve_GetSetLine(ptr, true, ref value);
      }
    }
  }
}
