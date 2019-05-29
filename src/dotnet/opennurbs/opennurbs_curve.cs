using System;
using Rhino.Collections;
using System.Collections.Generic;
using Rhino.Runtime.InteropWrappers;
using System.Runtime.Serialization;
using Rhino.Runtime;
// don't wrap ON_MeshCurveParameters. It is only needed for the ON_Curve::MeshCurveFunction

namespace Rhino.Geometry
{
  /// <summary>
  /// Used in curve and surface blending functions
  /// </summary>
  public enum BlendContinuity : int
  {
    /// <summary></summary>
    Position = 0,
    /// <summary></summary>
    Tangency = 1,
    /// <summary></summary>
    Curvature = 2
  }

  /// <summary>
  /// Defines enumerated values for all implemented corner styles in curve offsets.
  /// </summary>
  public enum CurveOffsetCornerStyle : int
  {
    /// <summary>
    /// The dafault value.
    /// </summary>
    None = 0,

    /// <summary>
    /// Offsets and extends curves with a straight line until they intersect.
    /// </summary>
    Sharp = 1,

    /// <summary>
    /// Offsets and fillets curves with an arc of radius equal to the offset distance.
    /// </summary>
    Round = 2,

    /// <summary>
    /// Offsets and connects curves with a smooth (G1 continuity) curve.
    /// </summary>
    Smooth = 3,

    /// <summary>
    /// Offsets and connects curves with a straight line between their endpoints.
    /// </summary>
    Chamfer = 4
  }

  /// <summary>
  /// Defines enumerated values for knot spacing styles in interpolated curves.
  /// </summary>
  public enum CurveKnotStyle : int
  {
    /// <summary>
    /// Parameter spacing between consecutive knots is 1.0.
    /// </summary>
    Uniform = 0,

    /// <summary>
    /// Chord length spacing, requires degree=3 with CV1 and CVn1 specified.
    /// </summary>
    Chord = 1,

    /// <summary>
    /// Square root of chord length, requires degree=3 with CV1 and CVn1 specified.
    /// </summary>
    ChordSquareRoot = 2,

    /// <summary>
    /// Periodic with uniform spacing.
    /// </summary>
    UniformPeriodic = 3,

    /// <summary>
    /// Periodic with chord length spacing.
    /// </summary>
    ChordPeriodic = 4,

    /// <summary>
    /// Periodic with square roor of chord length spacing. 
    /// </summary>
    ChordSquareRootPeriodic = 5
  }

  /// <summary>
  /// Defines enumerated values for closed curve orientations.
  /// </summary>
  public enum CurveOrientation : int
  {
    /// <summary>
    /// Unable to compute the curve's orientation.
    /// </summary>
    Undefined = 0,

    /// <summary>
    /// The curve's orientation is clockwise in the xy plane.
    /// </summary>
    Clockwise = -1,

    /// <summary>
    /// The curve's orientation is counter clockwise in the xy plane.
    /// </summary>
    CounterClockwise = +1
  }

  /// <summary>
  /// Defines enumerated values for closed curve/point spatial relationships.
  /// </summary>
  public enum PointContainment : int
  {
    /// <summary>
    /// Relation is meaningless.
    /// </summary>
    Unset,

    /// <summary>
    /// Point is on the interior of the region implied by the closed curve.
    /// </summary>
    Inside,

    /// <summary>
    /// Point is on the exterior of the region implied by the closed curve.
    /// </summary>
    Outside,

    /// <summary>
    /// Point is coincident with the curve and therefor neither inside not outside.
    /// </summary>
    Coincident
  }

  /// <summary>
  /// Defines enumerated values for closed curve/closed curve relationships.
  /// </summary>
  public enum RegionContainment : int
  {
    /// <summary>
    /// There is no common area between the two regions.
    /// </summary>
    Disjoint = 0,

    /// <summary>
    /// The two curves intersect. There is therefore no full containment relationship either way.
    /// </summary>
    MutualIntersection = 1,

    /// <summary>
    /// Region bounded by curveA (first curve) is inside of curveB (second curve).
    /// </summary>
    AInsideB = 2,

    /// <summary>
    /// Region bounded by curveB (second curve) is inside of curveA (first curve).
    /// </summary>
    BInsideA = 3,
  }

  /// <summary>
  /// Defines enumerated values for styles to use during curve extension, such as "Line", "Arc" or "Smooth".
  /// </summary>
  public enum CurveExtensionStyle : int
  {
    /// <summary>
    /// Curve ends will be propagated linearly according to tangents.
    /// </summary>
    Line = 0,

    /// <summary>
    /// Curve ends will be propagated arc-wise according to curvature.
    /// </summary>
    Arc = 1,

    /// <summary>
    /// Curve ends will be propagated smoothly according to curvature.
    /// </summary>
    Smooth = 2,
  }

  /// <summary>
  /// Enumerates the options to use when simplifying a curve.
  /// </summary>
  [FlagsAttribute]
  public enum CurveSimplifyOptions : int
  {
    /// <summary>
    /// No option is specified.
    /// </summary>
    None = 0,

    /// <summary>
    /// Split NurbsCurves at fully multiple knots. 
    /// Effectively turning single nurbs segments with kinks into multiple segments.
    /// </summary>
    SplitAtFullyMultipleKnots = 1,

    /// <summary>
    /// Replace linear segments with LineCurves.
    /// </summary>
    RebuildLines = 2,

    /// <summary>
    /// Replace partially circular segments with ArcCurves.
    /// </summary>
    RebuildArcs = 4,

    /// <summary>
    /// Replace rational nurbscurves with constant weights 
    /// with an equivalent non-rational NurbsCurve.
    /// </summary>
    RebuildRationals = 8,

    /// <summary>
    /// Adjust Curves at G1-joins.
    /// </summary>
    AdjustG1 = 16,

    /// <summary>
    /// Merge adjacent co-linear lines or co-circular arcs 
    /// or combine consecutive line segments into a polyline.
    /// </summary>
    Merge = 32,

    /// <summary>
    /// Implies all of the simplification functions will be used.
    /// </summary>
    All = SplitAtFullyMultipleKnots | RebuildLines | RebuildArcs | RebuildRationals | AdjustG1 | Merge
  }

  /// <summary>
  /// Defines the extremes of a curve through a flagged enumeration. 
  /// </summary>
  /// <example>
  /// <code source='examples\vbnet\ex_extendcurve.vb' lang='vbnet'/>
  /// <code source='examples\cs\ex_extendcurve.cs' lang='cs'/>
  /// <code source='examples\py\ex_extendcurve.py' lang='py'/>
  /// </example>
  [FlagsAttribute]
  public enum CurveEnd : int
  {
    /// <summary>
    /// Not the start nor the end.
    /// </summary>
    None = 0,

    /// <summary>
    /// The frontal part of the curve.
    /// </summary>
    Start = 1,

    /// <summary>
    /// The tail part of the curve.
    /// </summary>
    End = 2,

    /// <summary>
    /// Both the start and the end of the curve.
    /// </summary>
    Both = 3,
  }

  /// <summary>
  /// Defines enumerated values for the options that defines a curve evaluation side when evaluating kinks.
  /// </summary>
  public enum CurveEvaluationSide : int
  {
    /// <summary>
    /// The default evaluation side.
    /// </summary>
    Default = 0,

    /// <summary>
    /// The below evaluation side.
    /// </summary>
    Below = -1,

    /// <summary>
    /// The above evaluation side.
    /// </summary>
    Above = +1
  }

  /// <summary>
  /// Defines enumerated values for types of conic sections.
  /// </summary>
  public enum ConicSectionType : int
  {
    /// <summary>
    /// The curve shape is unknown.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// The curve has the shape of a circle.
    /// </summary>
    Circle = 1,

    /// <summary>
    /// The curve has the shape of an ellipse.
    /// </summary>
    Ellipse = 2,

    /// <summary>
    /// The curve has the shape of a hyperbola.
    /// </summary>
    Hyperbola = 3,

    /// <summary>
    /// The curve has the shape of a parabola.
    /// </summary>
    Parabola = 4
  }

  /// <summary>
  /// Represents a base class that is common to most RhinoCommon curve types.
  /// <para>A curve represents an entity that can be all visited by providing
  /// a single parameter, usually called t.</para>
  /// </summary>
  [Serializable]
  public class Curve : GeometryBase
  {
    #region statics
#if RHINO_SDK

    /// <summary>
    /// Returns the type of conic section based on the curve's shape.
    /// </summary>
    /// <returns></returns>
    [ConstOperation]
    public ConicSectionType GetConicSectionType()
    {
      Point3d focus1, focus2, center;
      return GetConicSectionType(out focus1, out focus2, out center);
    }

    /// <summary>
    /// Returns the type of conic section based on the curve's shape.
    /// </summary>
    /// <param name="focus1">The first focus point, if applicable.</param>
    /// <param name="focus2">The second focus point, if applicable.</param>
    /// <param name="center">The center point, if applicable.</param>
    /// <returns></returns>
    [ConstOperation]
    public ConicSectionType GetConicSectionType(out Point3d focus1, out Point3d focus2, out Point3d center)
    {
      focus1 = Point3d.Unset;
      focus2 = Point3d.Unset;
      center = Point3d.Unset;
      var const_ptr_curve = ConstPointer();
      var rc = UnsafeNativeMethods.RHC_RhinoIsCurveConicSection(const_ptr_curve, ref focus1, ref focus2, ref center);
      var type = Geometry.ConicSectionType.Unknown;
      switch (rc)
      {
        case 1:
          type = Geometry.ConicSectionType.Circle;
          break;
        case 2:
          type = Geometry.ConicSectionType.Ellipse;
          break;
        case 3:
          type = Geometry.ConicSectionType.Hyperbola;
          break;
        case 4:
          type = Geometry.ConicSectionType.Parabola;
          break;
        default:
          type = Geometry.ConicSectionType.Unknown;
          break;
      }
      return type;
    }

    /// <summary>
    /// Interpolates a sequence of points. Used by InterpCurve Command
    /// This routine works best when degree=3.
    /// </summary>
    /// <param name="degree">The degree of the curve >=1.  Degree must be odd.</param>
    /// <param name="points">
    /// Points to interpolate (Count must be >= 2)
    /// </param>
    /// <returns>interpolated curve on success. null on failure.</returns>
    public static Curve CreateInterpolatedCurve(IEnumerable<Point3d> points, int degree)
    {
      return CreateInterpolatedCurve(points, degree, CurveKnotStyle.Uniform);
    }
    /// <summary>
    /// Interpolates a sequence of points. Used by InterpCurve Command
    /// This routine works best when degree=3.
    /// </summary>
    /// <param name="degree">The degree of the curve >=1.  Degree must be odd.</param>
    /// <param name="points">
    /// Points to interpolate. For periodic curves if the final point is a
    /// duplicate of the initial point it is  ignored. (Count must be >=2)
    /// </param>
    /// <param name="knots">
    /// Knot-style to use  and specifies if the curve should be periodic.
    /// </param>
    /// <returns>interpolated curve on success. null on failure.</returns>
    public static Curve CreateInterpolatedCurve(IEnumerable<Point3d> points, int degree, CurveKnotStyle knots)
    {
      return CreateInterpolatedCurve(points, degree, knots, Vector3d.Unset, Vector3d.Unset);
    }
    /// <summary>
    /// Interpolates a sequence of points. Used by InterpCurve Command
    /// This routine works best when degree=3.
    /// </summary>
    /// <param name="degree">The degree of the curve >=1.  Degree must be odd.</param>
    /// <param name="points">
    /// Points to interpolate. For periodic curves if the final point is a
    /// duplicate of the initial point it is  ignored. (Count must be >=2)
    /// </param>
    /// <param name="knots">
    /// Knot-style to use  and specifies if the curve should be periodic.
    /// </param>
    /// <param name="startTangent">A starting tangent.</param>
    /// <param name="endTangent">An ending tangent.</param>
    /// <returns>interpolated curve on success. null on failure.</returns>
    public static Curve CreateInterpolatedCurve(IEnumerable<Point3d> points, int degree, CurveKnotStyle knots, Vector3d startTangent, Vector3d endTangent)
    {
      if (null == points)
        throw new ArgumentNullException("points");

      int count;
      Point3d[] ptArray = RhinoListHelpers.GetConstArray(points, out count);
      if (count < 2)
        throw new InvalidOperationException("Insufficient points for an interpolated curve");

      if (2 == count && !startTangent.IsValid && !endTangent.IsValid)
        return new LineCurve(ptArray[0], ptArray[1]);

      if (1 == degree && count > 2 && !startTangent.IsValid && !endTangent.IsValid)
        return PolylineCurve.Internal_FromArray(ptArray, count);
      IntPtr ptr = UnsafeNativeMethods.RHC_RhinoInterpCurve(degree, count, ptArray, startTangent, endTangent, (int)knots);
      return GeometryBase.CreateGeometryHelper(ptr, null) as NurbsCurve;
    }

    /// <summary>
    /// Creates a soft edited curve from an exising curve using a smooth field of influence.
    /// </summary>
    /// <param name="curve">The curve to soft edit.</param>
    /// <param name="t">
    /// A parameter on the curve to move from. This location on the curve is moved, and the move
    ///  is smoothly tapered off with increasing distance along the curve from this parameter.
    /// </param>
    /// <param name="delta">The direction and magitude, or maximum distance, of the move.</param>
    /// <param name="length">
    /// The distance along the curve from the editing point over which the strength 
    /// of the editing falls off smoothly.
    /// </param>
    /// <param name="fixEnds"></param>
    /// <returns>The soft edited curve if successful. null on failure.</returns>
    public static Curve CreateSoftEditCurve(Curve curve, double t, Vector3d delta, double length, bool fixEnds)
    {
      if (null == curve)
        throw new ArgumentNullException("curve");

      IntPtr const_ptr_curve = curve.ConstPointer();
      IntPtr ptr = UnsafeNativeMethods.RHC_RhinoSoftEditCrv(const_ptr_curve, t, delta, length, fixEnds);
      GC.KeepAlive(curve);
      return GeometryBase.CreateGeometryHelper(ptr, null) as Curve;
    }

    /// <summary>
    /// Rounds the corners of a kinked curve with arcs of a single, specified radius.
    /// </summary>
    /// <param name="curve">The curve to fillet.</param>
    /// <param name="radius">The fillet radius.</param>
    /// <param name="tolerance">The tolerance. When in doubt, use the document's model space absolute tolerance.</param>
    /// <param name="angleTolerance">The angle tolerance in radians. When in doubt, use the document's model space angle tolerance.</param>
    /// <returns>The filleted curve if successful. null on failure.</returns>
    public static Curve CreateFilletCornersCurve(Curve curve, double radius, double tolerance, double angleTolerance)
    {
      if (null == curve)
        throw new ArgumentNullException("curve");

      IntPtr const_ptr_curve = curve.ConstPointer();
      IntPtr ptr = UnsafeNativeMethods.RHC_RhinoFilletCurveCorners(const_ptr_curve, radius, tolerance, angleTolerance);
      GC.KeepAlive(curve);
      return GeometryBase.CreateGeometryHelper(ptr, null) as Curve;
    }

#endif //RHINO_SDK

    /// <summary>
    /// Constructs a curve from a set of control-point locations.
    /// </summary>
    /// <param name="points">Control points.</param>
    /// <param name="degree">Degree of curve. The number of control points must be at least degree+1.</param>
    public static Curve CreateControlPointCurve(IEnumerable<Point3d> points, int degree)
    {
      int count;
      Point3d[] ptArray = RhinoListHelpers.GetConstArray(points, out count);
      if (null == ptArray || count < 2)
        return null;

      if (2 == count)
        return new LineCurve(ptArray[0], ptArray[1]);

      if (1 == degree && count > 2)
        return PolylineCurve.Internal_FromArray(ptArray, count);

      IntPtr ptr = UnsafeNativeMethods.ON_NurbsCurve_CreateControlPointCurve(count, ptArray, degree);
      return GeometryBase.CreateGeometryHelper(ptr, null) as NurbsCurve;
    }
    /// <summary>
    /// Constructs a control-point of degree=3 (or less).
    /// </summary>
    /// <param name="points">Control points of curve.</param>
    public static Curve CreateControlPointCurve(IEnumerable<Point3d> points)
    {
      return CreateControlPointCurve(points, 3);
    }

#if RHINO_SDK

    /// <summary>
    /// Creates a polycurve consisting of two tangent arc segments that connect two points and two directions.
    /// </summary>
    /// <param name="startPt">Start of the arc blend curve.</param>
    /// <param name="startDir">Start direction of the arc blend curve.</param>
    /// <param name="endPt">End of the arc blend curve.</param>
    /// <param name="endDir">End direction of the arc blend curve.</param>
    /// <param name="controlPointLengthRatio">
    /// The ratio of the control polygon lengths of the two arcs. Note, a value of 1.0 
    /// means the control polygon lengths for both arcs will be the same.
    /// </param>
    /// <returns>The arc blend curve, or null on error.</returns>
    public static Curve CreateArcBlend(Point3d startPt, Vector3d startDir, Point3d endPt, Vector3d endDir, double controlPointLengthRatio)
    {
      IntPtr ptr = UnsafeNativeMethods.RHC_RhinoCreateArcBlend(startPt, startDir, endPt, endDir, controlPointLengthRatio);
      return GeometryBase.CreateGeometryHelper(ptr, null) as PolyCurve;
    }

    /// <summary>
    /// Constructs a mean, or average, curve from two curves.
    /// </summary>
    /// <param name="curveA">A first curve.</param>
    /// <param name="curveB">A second curve.</param>
    /// <param name="angleToleranceRadians">
    /// The angle tolerance, in radians, used to match kinks between curves.
    /// If you are unsure how to set this parameter, then either use the
    /// document's angle tolerance RhinoDoc.AngleToleranceRadians,
    /// or the default value (RhinoMath.UnsetValue)
    /// </param>
    /// <returns>The average curve, or null on error.</returns>
    /// <exception cref="ArgumentNullException">If curveA or curveB are null.</exception>
    public static Curve CreateMeanCurve(Curve curveA, Curve curveB, double angleToleranceRadians)
    {
      if (curveA == null) throw new ArgumentNullException("curveA");
      if (curveB == null) throw new ArgumentNullException("curveB");

      IntPtr pCurveA = curveA.ConstPointer();
      IntPtr pCurveB = curveB.ConstPointer();
      IntPtr pNewCurve = UnsafeNativeMethods.RHC_RhinoMeanCurve(pCurveA, pCurveB, angleToleranceRadians);
      Runtime.CommonObject.GcProtect(curveA, curveB);
      return GeometryBase.CreateGeometryHelper(pNewCurve, null) as Curve;
    }
    /// <summary>
    /// Constructs a mean, or average, curve from two curves.
    /// </summary>
    /// <param name="curveA">A first curve.</param>
    /// <param name="curveB">A second curve.</param>
    /// <returns>The average curve, or null on error.</returns>
    /// <exception cref="ArgumentNullException">If curveA or curveB are null.</exception>
    public static Curve CreateMeanCurve(Curve curveA, Curve curveB)
    {
      return CreateMeanCurve(curveA, curveB, RhinoMath.UnsetValue);
    }

    /// <summary>
    /// Create a Blend curve between two existing curves.
    /// </summary>
    /// <param name="curveA">Curve to blend from (blending will occur at curve end point).</param>
    /// <param name="curveB">Curve to blend to (blending will occur at curve start point).</param>
    /// <param name="continuity">Continuity of blend.</param>
    /// <returns>A curve representing the blend between A and B or null on failure.</returns>
    public static Curve CreateBlendCurve(Curve curveA, Curve curveB, BlendContinuity continuity)
    {
      return CreateBlendCurve(curveA, curveB, continuity, 1, 1);
    }
    /// <summary>
    /// Create a Blend curve between two existing curves.
    /// </summary>
    /// <param name="curveA">Curve to blend from (blending will occur at curve end point).</param>
    /// <param name="curveB">Curve to blend to (blending will occur at curve start point).</param>
    /// <param name="continuity">Continuity of blend.</param>
    /// <param name="bulgeA">Bulge factor at curveA end of blend. Values near 1.0 work best.</param>
    /// <param name="bulgeB">Bulge factor at curveB end of blend. Values near 1.0 work best.</param>
    /// <returns>A curve representing the blend between A and B or null on failure.</returns>
    public static Curve CreateBlendCurve(Curve curveA, Curve curveB, BlendContinuity continuity, double bulgeA, double bulgeB)
    {
      if (curveA == null) throw new ArgumentNullException("curveA");
      if (curveB == null) throw new ArgumentNullException("curveB");

      IntPtr pCurveA = curveA.ConstPointer();
      IntPtr pCurveB = curveB.ConstPointer();

      switch (continuity)
      {
        case BlendContinuity.Position:
          return new LineCurve(curveA.PointAtEnd, curveB.PointAtStart);

        case BlendContinuity.Tangency:
          IntPtr pG1Curve = UnsafeNativeMethods.RHC_RhinoBlendG1Curve(pCurveA, pCurveB, bulgeA, bulgeB);
          return GeometryBase.CreateGeometryHelper(pG1Curve, null) as Curve;

        case BlendContinuity.Curvature:
          IntPtr pG2Curve = UnsafeNativeMethods.RHC_RhinoBlendG2Curve(pCurveA, pCurveB, bulgeA, bulgeB);
          return GeometryBase.CreateGeometryHelper(pG2Curve, null) as Curve;
      }

      Runtime.CommonObject.GcProtect(curveA, curveB);
      return null;
    }

    /// <summary>
    /// Makes a curve blend between 2 curves at the parameters specified
    /// with the directions and continuities specified
    /// </summary>
    /// <param name="curve0">First curve to blend from</param>
    /// <param name="t0">Parameter on first curve for blend endpoint</param>
    /// <param name="reverse0">
    /// If false, the blend will go in the natural direction of the curve.
    /// If true, the blend will go in the opposite direction to the curve
    /// </param>
    /// <param name="continuity0">Continuity for the blend at the start</param>
    /// <param name="curve1">Second curve to blend from</param>
    /// <param name="t1">Parameter on second curve for blend endpoint</param>
    /// <param name="reverse1">
    /// If false, the blend will go in the natural direction of the curve.
    /// If true, the blend will go in the opposite direction to the curve
    /// </param>
    /// <param name="continuity1">Continuity for the blend at the end</param>
    /// <returns>The blend curve on success. null on failure</returns>
    public static Curve CreateBlendCurve(Curve curve0, double t0, bool reverse0, BlendContinuity continuity0,
                                         Curve curve1, double t1, bool reverse1, BlendContinuity continuity1)
    {
      IntPtr pConstCurve0 = curve0.ConstPointer();
      IntPtr pConstCurve1 = curve1.ConstPointer();
      IntPtr pCurve = UnsafeNativeMethods.CRhinoBlend_CurveBlend(pConstCurve0, t0, reverse0, (int)continuity0, pConstCurve1, t1, reverse1, (int)continuity1);
      Runtime.CommonObject.GcProtect(curve0, curve1);
      return GeometryBase.CreateGeometryHelper(pCurve, null) as Curve;
    }

    /// <summary>
    /// Creates curves between two open or closed input curves. Uses the control points of the curves for finding tween curves.
    /// That means the first control point of first curve is matched to first control point of the second curve and so on.
    /// There is no matching of curves direction. Caller must match input curves direction before calling the function.
    /// </summary>
    /// <param name="curve0">The first, or starting, curve.</param>
    /// <param name="curve1">The second, or ending, curve.</param>
    /// <param name="numCurves">Number of tween curves to create.</param>
    /// <returns>An array of joint curves. This array can be empty.</returns>
    [Obsolete("Use version that takes tolerance as input")]
    public static Curve[] CreateTweenCurves(Curve curve0, Curve curve1, int numCurves)
    {
      double tolerance, angle_tol;
      RhinoDoc.ActiveDocTolerances(out tolerance, out angle_tol);
      return CreateTweenCurves(curve0, curve1, numCurves, tolerance);
    }

    /// <summary>
    /// Creates curves between two open or closed input curves. Uses the control points of the curves for finding tween curves.
    /// That means the first control point of first curve is matched to first control point of the second curve and so on.
    /// There is no matching of curves direction. Caller must match input curves direction before calling the function.
    /// </summary>
    /// <param name="curve0">The first, or starting, curve.</param>
    /// <param name="curve1">The second, or ending, curve.</param>
    /// <param name="numCurves">Number of tween curves to create.</param>
    /// <param name="tolerance"></param>
    /// <returns>An array of joint curves. This array can be empty.</returns>
    public static Curve[] CreateTweenCurves(Curve curve0, Curve curve1, int numCurves, double tolerance)
    {
      IntPtr pConstCurve0 = curve0.ConstPointer();
      IntPtr pConstCurve1 = curve1.ConstPointer();
      SimpleArrayCurvePointer output = new SimpleArrayCurvePointer();
      IntPtr outputPtr = output.NonConstPointer();
      bool rc = UnsafeNativeMethods.RHC_RhinoTweenCurves(pConstCurve0, pConstCurve1, numCurves, tolerance, outputPtr);
      Runtime.CommonObject.GcProtect(curve0, curve1);
      return rc ? output.ToNonConstArray() : new Curve[0];
    }

    /// <summary>
    /// Creates curves between two open or closed input curves. Make the structure of input curves compatible if needed.
    /// Refits the input curves to have the same structure. The resulting curves are usually more complex than input unless
    /// input curves are compatible and no refit is needed. There is no matching of curves direction.
    /// Caller must match input curves direction before calling the function.
    /// </summary>
    /// <param name="curve0">The first, or starting, curve.</param>
    /// <param name="curve1">The second, or ending, curve.</param>
    /// <param name="numCurves">Number of tween curves to create.</param>
    /// <returns>An array of joint curves. This array can be empty.</returns>
    [Obsolete("Use version that takes tolerance as input")]
    public static Curve[] CreateTweenCurvesWithMatching(Curve curve0, Curve curve1, int numCurves)
    {
      double tolerance, angle_tol;
      RhinoDoc.ActiveDocTolerances(out tolerance, out angle_tol);
      return CreateTweenCurvesWithMatching(curve0, curve1, numCurves, tolerance);
    }

    /// <summary>
    /// Creates curves between two open or closed input curves. Make the structure of input curves compatible if needed.
    /// Refits the input curves to have the same structure. The resulting curves are usually more complex than input unless
    /// input curves are compatible and no refit is needed. There is no matching of curves direction.
    /// Caller must match input curves direction before calling the function.
    /// </summary>
    /// <param name="curve0">The first, or starting, curve.</param>
    /// <param name="curve1">The second, or ending, curve.</param>
    /// <param name="numCurves">Number of tween curves to create.</param>
    /// <param name="tolerance"></param>
    /// <returns>An array of joint curves. This array can be empty.</returns>
    public static Curve[] CreateTweenCurvesWithMatching(Curve curve0, Curve curve1, int numCurves, double tolerance)
    {
      IntPtr pConstCurve0 = curve0.ConstPointer();
      IntPtr pConstCurve1 = curve1.ConstPointer();
      SimpleArrayCurvePointer output = new SimpleArrayCurvePointer();
      IntPtr outputPtr = output.NonConstPointer();
      bool rc = UnsafeNativeMethods.RHC_RhinoTweenCurvesWithMatching(pConstCurve0, pConstCurve1, numCurves, tolerance, outputPtr);
      Runtime.CommonObject.GcProtect(curve0, curve1);
      return rc ? output.ToNonConstArray() : new Curve[0];
    }

    /// <summary>
    /// Creates curves between two open or closed input curves. Use sample points method to make curves compatible.
    /// This is how the algorithm workd: Divides the two curves into an equal number of points, finds the midpoint between the 
    /// corresponding points on the curves and interpolates the tween curve through those points. There is no matching of curves
    /// direction. Caller must match input curves direction before calling the function.
    /// </summary>
    /// <param name="curve0">The first, or starting, curve.</param>
    /// <param name="curve1">The second, or ending, curve.</param>
    /// <param name="numCurves">Number of tween curves to create.</param>
    /// <param name="numSamples">Number of sample points along input curves.</param>
    /// <returns>>An array of joint curves. This array can be empty.</returns>
    [Obsolete("Use version that takes tolerance as input")]
    public static Curve[] CreateTweenCurvesWithSampling(Curve curve0, Curve curve1, int numCurves, int numSamples)
    {
      double tolerance, angle_tol;
      RhinoDoc.ActiveDocTolerances(out tolerance, out angle_tol);
      return CreateTweenCurvesWithSampling(curve0, curve1, numCurves, numSamples, tolerance);
    }

    /// <summary>
    /// Creates curves between two open or closed input curves. Use sample points method to make curves compatible.
    /// This is how the algorithm workd: Divides the two curves into an equal number of points, finds the midpoint between the 
    /// corresponding points on the curves and interpolates the tween curve through those points. There is no matching of curves
    /// direction. Caller must match input curves direction before calling the function.
    /// </summary>
    /// <param name="curve0">The first, or starting, curve.</param>
    /// <param name="curve1">The second, or ending, curve.</param>
    /// <param name="numCurves">Number of tween curves to create.</param>
    /// <param name="numSamples">Number of sample points along input curves.</param>
    /// <param name="tolerance"></param>
    /// <returns>>An array of joint curves. This array can be empty.</returns>
    public static Curve[] CreateTweenCurvesWithSampling(Curve curve0, Curve curve1, int numCurves, int numSamples, double tolerance)
    {
      IntPtr pConstCurve0 = curve0.ConstPointer();
      IntPtr pConstCurve1 = curve1.ConstPointer();
      SimpleArrayCurvePointer output = new SimpleArrayCurvePointer();
      IntPtr outputPtr = output.NonConstPointer();
      bool rc = UnsafeNativeMethods.RHC_RhinoTweenCurveWithSampling(pConstCurve0, pConstCurve1, numCurves, numSamples, tolerance, outputPtr);
      Runtime.CommonObject.GcProtect(curve0, curve1);
      return rc ? output.ToNonConstArray() : new Curve[0];
    }

    /// <summary>
    /// Joins a collection of curve segments together.
    /// </summary>
    /// <param name="inputCurves">Curve segments to join.</param>
    /// <returns>An array of curves which contains.</returns>
    public static Curve[] JoinCurves(IEnumerable<Curve> inputCurves)
    {
      return JoinCurves(inputCurves, 0.0, false);
    }
    /// <summary>
    /// Joins a collection of curve segments together.
    /// </summary>
    /// <param name="inputCurves">An array, a list or any enumerable set of curve segments to join.</param>
    /// <param name="joinTolerance">Joining tolerance, 
    /// i.e. the distance between segment end-points that is allowed.</param>
    /// <returns>An array of joint curves. This array can be empty.</returns>
    /// <example>
    /// <code source='examples\vbnet\ex_dividebylength.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_dividebylength.cs' lang='cs'/>
    /// <code source='examples\py\ex_dividebylength.py' lang='py'/>
    /// </example>
    /// <exception cref="ArgumentNullException">If inputCurves is null.</exception>
    public static Curve[] JoinCurves(IEnumerable<Curve> inputCurves, double joinTolerance)
    {
      return JoinCurves(inputCurves, joinTolerance, false);
    }
    /// <summary>
    /// Joins a collection of curve segments together.
    /// </summary>
    /// <param name="inputCurves">An array, a list or any enumerable set of curve segments to join.</param>
    /// <param name="joinTolerance">Joining tolerance, 
    /// i.e. the distance between segment end-points that is allowed.</param>
    /// <param name="preserveDirection">
    /// <para>If true, curve endpoints will be compared to curve startpoints.</para>
    /// <para>If false, all start and endpoints will be compared and copies of input curves may be reversed in output.</para>
    /// </param>
    /// <returns>An array of joint curves. This array can be empty.</returns>
    /// <exception cref="ArgumentNullException">If inputCurves is null.</exception>
    public static Curve[] JoinCurves(IEnumerable<Curve> inputCurves, double joinTolerance, bool preserveDirection)
    {
      // 1 March 2010 S. Baer
      // JoinCurves calls the unmanaged RhinoMergeCurves function which appears to be a "better"
      // implementation of ON_JoinCurves. We removed the wrapper for ON_JoinCurves for this reason.
      if (null == inputCurves)
        throw new ArgumentNullException("inputCurves");

      using (SimpleArrayCurvePointer input = new SimpleArrayCurvePointer(inputCurves))
      using (SimpleArrayCurvePointer output = new SimpleArrayCurvePointer())
      {
        IntPtr inputPtr = input.ConstPointer();
        IntPtr outputPtr = output.NonConstPointer();

        bool rc = UnsafeNativeMethods.RHC_RhinoMergeCurves(inputPtr,
          outputPtr, joinTolerance, preserveDirection);

        GC.KeepAlive(inputCurves);
        return rc ? output.ToNonConstArray() : new Curve[0];
      }
    }

    /// <summary>
    /// Makes adjustments to the ends of one or both input curves so that they meet at a point.
    /// </summary>
    /// <param name="curveA">1st curve to adjust.</param>
    /// <param name="adjustStartCurveA">
    /// Which end of the 1st curve to adjust: true is start, false is end.
    /// </param>
    /// <param name="curveB">2nd curve to adjust.</param>
    /// <param name="adjustStartCurveB">
    /// which end of the 2nd curve to adjust true==start, false==end.
    /// </param>
    /// <returns>true on success.</returns>
    public static bool MakeEndsMeet(Curve curveA, bool adjustStartCurveA, Curve curveB, bool adjustStartCurveB)
    {
      IntPtr pCurveA = curveA.NonConstPointer();
      IntPtr pCurveB = curveB.NonConstPointer();
      bool rc = UnsafeNativeMethods.RHC_RhinoMakeCurveEndsMeet(pCurveA, adjustStartCurveA, pCurveB, adjustStartCurveB);
      Runtime.CommonObject.GcProtect(curveA, curveB);
      return rc;
    }

    /// <summary>
    /// Finds points at which to cut a pair of curves so that a fillet of given radius can be inserted.
    /// </summary>
    /// <param name="curve0">First curve to fillet.</param>
    /// <param name="curve1">Second curve to fillet.</param>
    /// <param name="radius">Fillet radius.</param>
    /// <param name="t0Base">Parameter value for base point on curve0.</param>
    /// <param name="t1Base">Parameter value for base point on curve1.</param>
    /// <param name="t0">Parameter value of fillet point on curve 0.</param>
    /// <param name="t1">Parameter value of fillet point on curve 1.</param>
    /// <param name="filletPlane">
    /// The fillet is contained in this plane with the fillet center at the plane origin.
    /// </param>
    /// <returns>true on success, false on failure.</returns>
    /// <remarks>
    /// A fillet point is a pair of curve parameters (t0,t1) such that there is a circle
    /// of radius point3 tangent to curve c0 at t0 and tangent to curve c1 at t1. Of all possible
    /// fillet points this function returns the one which is the closest to the base point
    /// t0Base, t1Base. Distance from the base point is measured by the sum of arc lengths
    /// along the two curves. 
    /// </remarks>
    public static bool GetFilletPoints(Curve curve0, Curve curve1, double radius, double t0Base, double t1Base,
                                       out double t0, out double t1, out Plane filletPlane)
    {
      t0 = 0;
      t1 = 0;
      filletPlane = new Plane();
      if (null == curve0 || null == curve1)
        return false;
      IntPtr pCurve0 = curve0.ConstPointer();
      IntPtr pCurve1 = curve1.ConstPointer();
      bool rc = UnsafeNativeMethods.RHC_GetFilletPoints(pCurve0, pCurve1, radius, t0Base, t1Base, ref t0, ref t1, ref filletPlane);
      Runtime.CommonObject.GcProtect(curve0, curve1);
      return rc;
    }

    /// <summary>
    /// Computes the fillet arc for a curve filleting operation.
    /// </summary>
    /// <param name="curve0">First curve to fillet.</param>
    /// <param name="curve1">Second curve to fillet.</param>
    /// <param name="radius">Fillet radius.</param>
    /// <param name="t0Base">Parameter on curve0 where the fillet ought to start (approximately).</param>
    /// <param name="t1Base">Parameter on curve1 where the fillet ought to end (approximately).</param>
    /// <returns>The fillet arc on success, or Arc.Unset on failure.</returns>
    public static Arc CreateFillet(Curve curve0, Curve curve1, double radius, double t0Base, double t1Base)
    {
      Arc arc = Arc.Unset;

      double t0, t1;
      Plane plane;
      if (GetFilletPoints(curve0, curve1, radius, t0Base, t1Base, out t0, out t1, out plane))
      {
        Vector3d radial0 = curve0.PointAt(t0) - plane.Origin;
        Vector3d radial1 = curve1.PointAt(t1) - plane.Origin;
        radial0.Unitize();
        radial1.Unitize();

        double angle = System.Math.Acos(radial0 * radial1);
        Plane fillet_plane = new Plane(plane.Origin, radial0, radial1);
        arc = new Arc(fillet_plane, plane.Origin, radius, angle);
      }
      return arc;
    }

    /// <summary>
    /// Creates a tangent arc between two curves and trims or extends the curves to the arc.
    /// </summary>
    /// <param name="curve0">The first curve to fillet.</param>
    /// <param name="point0">
    /// A point on the first curve that is near the end where the fillet will
    /// be created.
    /// </param>
    /// <param name="curve1">The second curve to fillet.</param>
    /// <param name="point1">
    /// A point on the second curve that is near the end where the fillet will
    /// be created.
    /// </param>
    /// <param name="radius">The radius of the fillet.</param>
    /// <param name="join">Join the output curves.</param>
    /// <param name="trim">
    /// Trim copies of the input curves to the output fillet curve.
    /// </param>
    /// <param name="arcExtension">
    /// Applies when arcs are filleted but need to be extended to meet the
    /// fillet curve or chamfer line. If true, then the arc is extended
    /// maintaining its validity. If false, then the arc is extended with a
    /// line segment, which is joined to the arc converting it to a polycurve.
    /// </param>
    /// <param name="tolerance">
    /// The tolerance, generally the document's absolute tolerance.
    /// </param>
    /// <param name="angleTolerance"></param>
    /// <returns>
    /// The results of the fillet operation. The number of output curves depends
    /// on the input curves and the values of the parameters that were used
    /// during the fillet operation. In most cases, the output array will contain
    /// either one or three curves, although two curves can be returned if the
    /// radius is zero and join = false.
    /// For example, if both join and trim = true, then the output curve
    /// will be a polycurve containing the fillet curve joined with trimmed copies
    /// of the input curves. If join = false and trim = true, then three curves,
    /// the fillet curve and trimmed copies of the input curves, will be returned.
    /// If both join and trim = false, then just the fillet curve is returned.
    /// </returns>
    /// <example>
    /// <code source='examples\vbnet\ex_filletcurves.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_filletcurves.cs' lang='cs'/>
    /// <code source='examples\py\ex_filletcurves.py' lang='py'/>
    /// </example>
    public static Curve[] CreateFilletCurves(Curve curve0, Point3d point0, Curve curve1, Point3d point1, double radius, bool join, bool trim, bool arcExtension, double tolerance, double angleTolerance)
    {
      IntPtr const_ptr_curve0 = curve0.ConstPointer();
      IntPtr const_ptr_curve1 = curve1.ConstPointer();
      using (var output_array = new SimpleArrayCurvePointer())
      {
        IntPtr ptr_output_array = output_array.NonConstPointer();
        UnsafeNativeMethods.RHC_RhFilletCurve(const_ptr_curve0, point0, const_ptr_curve1, point1, radius, join, trim, arcExtension, tolerance, angleTolerance, ptr_output_array);
        Runtime.CommonObject.GcProtect(curve0, curve1);
        return output_array.ToNonConstArray();
      }
    }

    const int idxBooleanUnion = 0;
    const int idxBooleanIntersection = 1;
    const int idxBooleanDifference = 2;
    /// <summary>
    /// Calculates the boolean union of two or more closed, planar curves. 
    /// Note, curves must be co-planar.
    /// </summary>
    /// <param name="curves">The co-planar curves to union.</param>
    /// <returns>Result curves on success, empty array if no union could be calculated.</returns>
    [Obsolete("Use version that takes tolerance as input")]
    public static Curve[] CreateBooleanUnion(IEnumerable<Curve> curves)
    {
      double tolerance, angle_tol;
      RhinoDoc.ActiveDocTolerances(out tolerance, out angle_tol);
      return CreateBooleanUnion(curves, tolerance);
    }
    /// <summary>
    /// Calculates the boolean union of two or more closed, planar curves. 
    /// Note, curves must be co-planar.
    /// </summary>
    /// <param name="curves">The co-planar curves to union.</param>
    /// <param name="tolerance"></param>
    /// <returns>Result curves on success, empty array if no union could be calculated.</returns>
    public static Curve[] CreateBooleanUnion(IEnumerable<Curve> curves, double tolerance)
    {
      if (null == curves)
        throw new ArgumentNullException("curves");

      using (SimpleArrayCurvePointer input = new SimpleArrayCurvePointer(curves))
      using (SimpleArrayCurvePointer output = new SimpleArrayCurvePointer())
      {
        IntPtr inputPtr = input.ConstPointer();
        IntPtr outputPtr = output.NonConstPointer();

        int rc = UnsafeNativeMethods.ON_Curve_BooleanOperation(inputPtr, outputPtr, idxBooleanUnion, tolerance);
        GC.KeepAlive(curves);
        return rc < 1 ? new Curve[0] : output.ToNonConstArray();
      }
    }

    /// <summary>
    /// Calculates the boolean intersection of two closed, planar curves. 
    /// Note, curves must be co-planar.
    /// </summary>
    /// <param name="curveA">The first closed, planar curve.</param>
    /// <param name="curveB">The second closed, planar curve.</param>
    /// <returns>Result curves on success, empty array if no intersection could be calculated.</returns>
    [Obsolete("Use version that takes tolerance as input")]
    public static Curve[] CreateBooleanIntersection(Curve curveA, Curve curveB)
    {
      double tolerance, angle_tol;
      RhinoDoc.ActiveDocTolerances(out tolerance, out angle_tol);
      return CreateBooleanIntersection(curveA, curveB, tolerance);
    }
    /// <summary>
    /// Calculates the boolean intersection of two closed, planar curves. 
    /// Note, curves must be co-planar.
    /// </summary>
    /// <param name="curveA">The first closed, planar curve.</param>
    /// <param name="curveB">The second closed, planar curve.</param>
    /// <param name="tolerance"></param>
    /// <returns>Result curves on success, empty array if no intersection could be calculated.</returns>
    public static Curve[] CreateBooleanIntersection(Curve curveA, Curve curveB, double tolerance)
    {
      if (null == curveA || null == curveB)
        throw new ArgumentNullException(null == curveA ? "curveA" : "curveB");

      using (SimpleArrayCurvePointer input = new SimpleArrayCurvePointer(new Curve[] { curveA, curveB }))
      using (SimpleArrayCurvePointer output = new SimpleArrayCurvePointer())
      {
        IntPtr inputPtr = input.ConstPointer();
        IntPtr outputPtr = output.NonConstPointer();
        int rc = UnsafeNativeMethods.ON_Curve_BooleanOperation(inputPtr, outputPtr, idxBooleanIntersection, tolerance);
        Runtime.CommonObject.GcProtect(curveA, curveB);
        return rc < 1 ? new Curve[0] : output.ToNonConstArray();
      }
    }

    /// <summary>
    /// Calculates the boolean difference between two closed, planar curves. 
    /// Note, curves must be co-planar.
    /// </summary>
    /// <param name="curveA">The first closed, planar curve.</param>
    /// <param name="curveB">The second closed, planar curve.</param>
    /// <returns>Result curves on success, empty array if no difference could be calculated.</returns>
    [Obsolete("Use version that takes tolerance as input")]
    public static Curve[] CreateBooleanDifference(Curve curveA, Curve curveB)
    {
      double tolerance, angle_tol;
      RhinoDoc.ActiveDocTolerances(out tolerance, out angle_tol);
      return CreateBooleanDifference(curveA, curveB, tolerance);
    }
    /// <summary>
    /// Calculates the boolean difference between two closed, planar curves. 
    /// Note, curves must be co-planar.
    /// </summary>
    /// <param name="curveA">The first closed, planar curve.</param>
    /// <param name="curveB">The second closed, planar curve.</param>
    /// <param name="tolerance"></param>
    /// <returns>Result curves on success, empty array if no difference could be calculated.</returns>
    public static Curve[] CreateBooleanDifference(Curve curveA, Curve curveB, double tolerance)
    {
      if (null == curveA || null == curveB)
        throw new ArgumentNullException(null == curveA ? "curveA" : "curveB");
      return CreateBooleanDifference(curveA, new Curve[] { curveB }, tolerance);
    }
    /// <summary>
    /// Calculates the boolean difference between a closed planar curve, and a list of closed planar curves. 
    /// Note, curves must be co-planar.
    /// </summary>
    /// <param name="curveA">The first closed, planar curve.</param>
    /// <param name="subtractors">curves to subtract from the first closed curve.</param>
    /// <returns>Result curves on success, empty array if no difference could be calculated.</returns>
    [Obsolete("Use version that takes tolerance as input")]
    public static Curve[] CreateBooleanDifference(Curve curveA, IEnumerable<Curve> subtractors)
    {
      double tolerance, angle_tol;
      RhinoDoc.ActiveDocTolerances(out tolerance, out angle_tol);
      return CreateBooleanDifference(curveA, subtractors, tolerance);
    }
    /// <summary>
    /// Calculates the boolean difference between a closed planar curve, and a list of closed planar curves. 
    /// Note, curves must be co-planar.
    /// </summary>
    /// <param name="curveA">The first closed, planar curve.</param>
    /// <param name="subtractors">curves to subtract from the first closed curve.</param>
    /// <param name="tolerance"></param>
    /// <returns>Result curves on success, empty array if no difference could be calculated.</returns>
    public static Curve[] CreateBooleanDifference(Curve curveA, IEnumerable<Curve> subtractors, double tolerance)
    {
      if (null == curveA || null == subtractors)
        throw new ArgumentNullException(null == curveA ? "curveA" : "subtractors");

      List<Curve> curves = new List<Curve> { curveA };
      curves.AddRange(subtractors);

      using (SimpleArrayCurvePointer input = new SimpleArrayCurvePointer(curves))
      using(SimpleArrayCurvePointer output = new SimpleArrayCurvePointer())
      {
        IntPtr inputPtr = input.ConstPointer();
        IntPtr outputPtr = output.NonConstPointer();
        int rc = UnsafeNativeMethods.ON_Curve_BooleanOperation(inputPtr, outputPtr, idxBooleanDifference, tolerance);
        GC.KeepAlive(curveA);
        GC.KeepAlive(subtractors);
        return rc < 1 ? new Curve[0] : output.ToNonConstArray();
      }
    }

    /// <summary>
    /// Creates outline curves created from a text string. The functionality is similar to what you find in Rhino's TextObject command or TextEntity.Explode() in RhinoCommon.
    /// </summary>
    /// <param name="text">The text from which to create outline curves.</param>
    /// <param name="font">The text font.</param>
    /// <param name="textHeight">The text height.</param>
    /// <param name="textStyle">The font style. The font style can be any number of the following: 0 - Normal, 1 - Bold, 2 - Italic</param>
    /// <param name="closeLoops">Set this value to True when dealing with normal fonts and when you expect closed loops. You may want to set this to False when specifying a single-stroke font where you don't want closed loops.</param>
    /// <param name="plane">The plane on which the outline curves will lie.</param>
    /// <param name="smallCapsScale">Displays lower-case letters as small caps. Set the relative text size to a percentage of the normal text.</param>
    /// <param name="tolerance">The tolerance for the operation.</param>
    /// <returns>An array containing one or more curves if successful.</returns>
    public static Curve[] CreateTextOutlines(string text, string font, double textHeight, int textStyle, bool closeLoops, Plane plane, double smallCapsScale, double tolerance)
    {
      using (var output_array = new SimpleArrayCurvePointer())
      {
        IntPtr ptr_output_array = output_array.NonConstPointer();
        UnsafeNativeMethods.RHC_CreateTextOutlines(text, font, textHeight, textStyle, closeLoops, ref plane, tolerance, smallCapsScale, ptr_output_array);
        return output_array.ToNonConstArray();
      }
    }

    /// <summary>
    /// Creates a third curve from two curves that are planar in different construction planes. 
    /// The new curve looks the same as each of the original curves when viewed in each plane.
    /// </summary>
    /// <param name="curveA">The first curve.</param>
    /// <param name="curveB">The second curve.</param>
    /// <param name="vectorA">A vector defining the normal direction of the plane which the first curve is drawn upon.</param>
    /// <param name="vectorB">A vector defining the normal direction of the plane which the seconf curve is drawn upon.</param>
    /// <param name="tolerance">The tolerance for the operation.</param>
    /// <param name="angleTolerance">The angle tolerance for the operation.</param>
    /// <returns>An array containing one or more curves if successful.</returns>
    public static Curve[] CreateCurve2View(Curve curveA, Curve curveB, Vector3d vectorA, Vector3d vectorB, double tolerance, double angleTolerance)
    {
      using (var output_array = new SimpleArrayCurvePointer())
      {
        IntPtr ptr_output_array = output_array.NonConstPointer();
        IntPtr ptr0 = curveA.ConstPointer();
        IntPtr ptr1 = curveB.ConstPointer();
        UnsafeNativeMethods.RHC_RhinoCurve2View(ptr0, ptr1, vectorA, vectorB, ptr_output_array, tolerance, angleTolerance);
        Runtime.CommonObject.GcProtect(curveA, curveB);
        return output_array.ToNonConstArray();
      }
    }

    /// <summary>
    /// Determines whether two curves travel more or less in the same direction.
    /// </summary>
    /// <param name="curveA">First curve to test.</param>
    /// <param name="curveB">Second curve to test.</param>
    /// <returns>true if both curves more or less point in the same direction, 
    /// false if they point in the opposite directions.</returns>
    public static bool DoDirectionsMatch(Curve curveA, Curve curveB)
    {
      IntPtr ptr0 = curveA.ConstPointer();
      IntPtr ptr1 = curveB.ConstPointer();
      bool rc = UnsafeNativeMethods.ON_Curve_DoCurveDirectionsMatch(ptr0, ptr1);
      Runtime.CommonObject.GcProtect(curveA, curveB);
      return rc;
    }

    /// <summary>
    /// Projects a curve to a mesh using a direction and tolerance.
    /// </summary>
    /// <param name="curve">A curve.</param>
    /// <param name="mesh">A mesh.</param>
    /// <param name="direction">A direction vector.</param>
    /// <param name="tolerance">A tolerance value.</param>
    /// <returns>A curve array.</returns>
    public static Curve[] ProjectToMesh(Curve curve, Mesh mesh, Vector3d direction, double tolerance)
    {
      Curve[] curves = new Curve[] { curve };
      Mesh[] meshes = new Mesh[] { mesh };
      return ProjectToMesh(curves, meshes, direction, tolerance);
    }

    /// <summary>
    /// Projects a curve to a set of meshes using a direction and tolerance.
    /// </summary>
    /// <param name="curve">A curve.</param>
    /// <param name="meshes">A list, an array or any enumerable of meshes.</param>
    /// <param name="direction">A direction vector.</param>
    /// <param name="tolerance">A tolerance value.</param>
    /// <returns>A curve array.</returns>
    public static Curve[] ProjectToMesh(Curve curve, IEnumerable<Mesh> meshes, Vector3d direction, double tolerance)
    {
      Curve[] curves = new Curve[] { curve };
      return ProjectToMesh(curves, meshes, direction, tolerance);
    }

    /// <summary>
    /// Projects a curve to a set of meshes using a direction and tolerance.
    /// </summary>
    /// <param name="curves">A list, an array or any enumerable of curves.</param>
    /// <param name="meshes">A list, an array or any enumerable of meshes.</param>
    /// <param name="direction">A direction vector.</param>
    /// <param name="tolerance">A tolerance value.</param>
    /// <returns>A curve array.</returns>
    public static Curve[] ProjectToMesh(IEnumerable<Curve> curves, IEnumerable<Mesh> meshes, Vector3d direction, double tolerance)
    {
      foreach (Curve crv in curves)
      {
        if (crv == null)
          throw new ArgumentNullException("curves");
      }
      List<GeometryBase> g = new List<GeometryBase>();
      foreach (Mesh msh in meshes)
      {
        if (msh == null)
          throw new ArgumentNullException("meshes");
        g.Add(msh);
      }

      using (SimpleArrayCurvePointer crv_array = new SimpleArrayCurvePointer(curves))
      using (Runtime.InteropWrappers.SimpleArrayGeometryPointer mesh_array = new Runtime.InteropWrappers.SimpleArrayGeometryPointer(g))
      using (SimpleArrayCurvePointer curves_out = new SimpleArrayCurvePointer())
      {
        IntPtr pCurvesIn = crv_array.ConstPointer();
        IntPtr pMeshes = mesh_array.ConstPointer();
        IntPtr pCurvesOut = curves_out.NonConstPointer();

        Curve[] rc = new Curve[0];
        if (UnsafeNativeMethods.RHC_RhinoProjectCurveToMesh(pMeshes, pCurvesIn, direction, tolerance, pCurvesOut))
          rc = curves_out.ToNonConstArray();
        GC.KeepAlive(curves);
        GC.KeepAlive(meshes);
        return rc;
      }
    }

    /// <summary>
    /// Projects a Curve onto a Brep along a given direction.
    /// </summary>
    /// <param name="curve">Curve to project.</param>
    /// <param name="brep">Brep to project onto.</param>
    /// <param name="direction">Direction of projection.</param>
    /// <param name="tolerance">Tolerance to use for projection.</param>
    /// <returns>An array of projected curves or empty array if the projection set is empty.</returns>
    public static Curve[] ProjectToBrep(Curve curve, Brep brep, Vector3d direction, double tolerance)
    {
      IntPtr brep_ptr = brep.ConstPointer();
      IntPtr curve_ptr = curve.ConstPointer();

      using (SimpleArrayCurvePointer rc = new SimpleArrayCurvePointer())
      {
        IntPtr rc_ptr = rc.NonConstPointer();
        Curve[] crvs = null;
        crvs = UnsafeNativeMethods.RHC_RhinoProjectCurveToBrep(brep_ptr, curve_ptr, direction, tolerance, rc_ptr) ? rc.ToNonConstArray() : new Curve[0];

        Runtime.CommonObject.GcProtect(curve, brep);
        return crvs;
      }
    }

    /// <summary>
    /// Projects a Curve onto a collection of Breps along a given direction.
    /// </summary>
    /// <param name="curve">Curve to project.</param>
    /// <param name="breps">Breps to project onto.</param>
    /// <param name="direction">Direction of projection.</param>
    /// <param name="tolerance">Tolerance to use for projection.</param>
    /// <returns>An array of projected curves or empty array if the projection set is empty.</returns>
    public static Curve[] ProjectToBrep(Curve curve, IEnumerable<Brep> breps, Vector3d direction, double tolerance)
    {
      int[] brep_ids;
      return ProjectToBrep(curve, breps, direction, tolerance, out brep_ids);
    }
    /// <summary>
    /// Projects a Curve onto a collection of Breps along a given direction.
    /// </summary>
    /// <param name="curve">Curve to project.</param>
    /// <param name="breps">Breps to project onto.</param>
    /// <param name="direction">Direction of projection.</param>
    /// <param name="tolerance">Tolerance to use for projection.</param>
    /// <param name="brepIndices">(out) Integers that identify for each resulting curve which Brep it was projected onto.</param>
    /// <returns>An array of projected curves or null if the projection set is empty.</returns>
    public static Curve[] ProjectToBrep(Curve curve, IEnumerable<Brep> breps, Vector3d direction, double tolerance, out int[] brepIndices)
    {
      int[] curveIndices;
      IEnumerable<Curve> crvs = new Curve[] { curve };
      return ProjectToBrep(crvs, breps, direction, tolerance, out curveIndices, out brepIndices);
    }
    /// <summary>
    /// Projects a collection of Curves onto a collection of Breps along a given direction.
    /// </summary>
    /// <param name="curves">Curves to project.</param>
    /// <param name="breps">Breps to project onto.</param>
    /// <param name="direction">Direction of projection.</param>
    /// <param name="tolerance">Tolerance to use for projection.</param>
    /// <returns>An array of projected curves or empty array if the projection set is empty.</returns>
    public static Curve[] ProjectToBrep(IEnumerable<Curve> curves, IEnumerable<Brep> breps, Vector3d direction, double tolerance)
    {
      int[] c_top;
      int[] b_top;
      return ProjectToBrep(curves, breps, direction, tolerance, out c_top, out b_top);
    }

    /// <summary>
    /// Projects a collection of Curves onto a collection of Breps along a given direction.
    /// </summary>
    /// <param name="curves">Curves to project.</param>
    /// <param name="breps">Breps to project onto.</param>
    /// <param name="direction">Direction of projection.</param>
    /// <param name="tolerance">Tolerance to use for projection.</param>
    /// <param name="curveIndices">Index of which curve in the input list was the source for a curve in the return array.</param>
    /// <param name="brepIndices">Index of which brep was used to generate a curve in the return array.</param>
    /// <returns>An array of projected curves. Array is empty if the projection set is empty.</returns>
    public static Curve[] ProjectToBrep(IEnumerable<Curve> curves, IEnumerable<Brep> breps, Vector3d direction, double tolerance, out int[] curveIndices, out int[] brepIndices)
    {
      curveIndices = null;
      brepIndices = null;

      foreach (Curve crv in curves) { if (crv == null) { throw new ArgumentNullException("curves"); } }
      foreach (Brep brp in breps) { if (brp == null) { throw new ArgumentNullException("breps"); } }

      using (SimpleArrayCurvePointer crv_array = new SimpleArrayCurvePointer(curves))
      using (SimpleArrayBrepPointer brp_array = new SimpleArrayBrepPointer())
      {
        foreach (Brep brp in breps) { brp_array.Add(brp, true); }

        IntPtr ptr_crv_array = crv_array.ConstPointer();
        IntPtr ptr_brp_array = brp_array.ConstPointer();

        SimpleArrayInt brp_top = new SimpleArrayInt();
        SimpleArrayInt crv_top = new SimpleArrayInt();

        SimpleArrayCurvePointer rc = new SimpleArrayCurvePointer();
        IntPtr ptr_rc = rc.NonConstPointer();

        if (UnsafeNativeMethods.RHC_RhinoProjectCurveToBrepEx(ptr_brp_array,
                                                              ptr_crv_array,
                                                              direction,
                                                              tolerance,
                                                              ptr_rc,
                                                              brp_top.m_ptr,
                                                              crv_top.m_ptr))
        {
          brepIndices = brp_top.ToArray();
          curveIndices = crv_top.ToArray();
          return rc.ToNonConstArray();
        }
        GC.KeepAlive(curves);
        GC.KeepAlive(breps);
        return new Curve[0];
      }
    }

    /// <summary>
    /// Constructs a curve by projecting an existing curve to a plane.
    /// </summary>
    /// <param name="curve">A curve.</param>
    /// <param name="plane">A plane.</param>
    /// <returns>The projected curve on success; null on failure.</returns>
    public static Curve ProjectToPlane(Curve curve, Plane plane)
    {
      IntPtr pConstCurve = curve.ConstPointer();
      IntPtr pNewCurve = UnsafeNativeMethods.RHC_RhinoProjectCurveToPlane(pConstCurve, ref plane);
      GC.KeepAlive(curve);
      return GeometryBase.CreateGeometryHelper(pNewCurve, null) as Curve;
    }

    /// <summary>
    /// Pull a curve to a BrepFace using closest point projection.
    /// </summary>
    /// <param name="curve">Curve to pull.</param>
    /// <param name="face">Brepface that pulls.</param>
    /// <param name="tolerance">Tolerance to use for pulling.</param>
    /// <returns>An array of pulled curves, or an empty array on failure.</returns>
    public static Curve[] PullToBrepFace(Curve curve, BrepFace face, double tolerance)
    {
      IntPtr brep_ptr = face.m_brep.ConstPointer();
      IntPtr curve_ptr = curve.ConstPointer();

      using (SimpleArrayCurvePointer rc = new SimpleArrayCurvePointer())
      {
        IntPtr rc_ptr = rc.NonConstPointer();
        if (UnsafeNativeMethods.RHC_RhinoPullCurveToBrep(brep_ptr, face.FaceIndex, curve_ptr, tolerance, rc_ptr))
        {
          return rc.ToNonConstArray();
        }
        Runtime.CommonObject.GcProtect(curve, face);
        return new Curve[0];
      }
    }

    /// <summary>
    /// Computes the distances between two arbitrary curves that overlap.
    /// </summary>
    /// <param name="curveA">A curve.</param>
    /// <param name="curveB">Another curve.</param>
    /// <param name="tolerance">A tolerance value.</param>
    /// <param name="maxDistance">The maximum distance value. This is an out reference argument.</param>
    /// <param name="maxDistanceParameterA">The maximum distance parameter on curve A. This is an out reference argument.</param>
    /// <param name="maxDistanceParameterB">The maximum distance parameter on curve B. This is an out reference argument.</param>
    /// <param name="minDistance">The minimum distance value. This is an out reference argument.</param>
    /// <param name="minDistanceParameterA">The minimum distance parameter on curve A. This is an out reference argument.</param>
    /// <param name="minDistanceParameterB">The minimum distance parameter on curve B. This is an out reference argument.</param>
    /// <returns>true if the operation succeeded; otherwise false.</returns>
    /// <example>
    /// <code source='examples\vbnet\ex_crvdeviation.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_crvdeviation.cs' lang='cs'/>
    /// <code source='examples\py\ex_crvdeviation.py' lang='py'/>
    /// </example>
    public static bool GetDistancesBetweenCurves(Curve curveA, Curve curveB, double tolerance,
      out double maxDistance, out double maxDistanceParameterA, out double maxDistanceParameterB,
      out double minDistance, out double minDistanceParameterA, out double minDistanceParameterB)
    {
      IntPtr pConstCrvA = curveA.ConstPointer();
      IntPtr pConstCrvB = curveB.ConstPointer();
      maxDistance = 0;
      maxDistanceParameterA = 0;
      maxDistanceParameterB = 0;
      minDistance = 0;
      minDistanceParameterA = 0;
      minDistanceParameterB = 0;

      bool rc = UnsafeNativeMethods.RHC_RhinoGetOverlapDistance(pConstCrvA, pConstCrvB, tolerance,
        ref maxDistanceParameterA, ref maxDistanceParameterB, ref maxDistance,
        ref minDistanceParameterA, ref minDistanceParameterB, ref minDistance);
      Runtime.CommonObject.GcProtect(curveA, curveB);
      return rc;
    }

    /// <summary>
    /// Determines whether two coplanar simple closed curves are disjoint or intersect;
    /// otherwise, if the regions have a containment relationship, discovers
    /// which curve encloses the other.
    /// </summary>
    /// <param name="curveA">A first curve.</param>
    /// <param name="curveB">A second curve.</param>
    /// <param name="testPlane">A plane.</param>
    /// <param name="tolerance">A tolerance value.</param>
    /// <returns>
    /// A value indicating the relationship between the first and the second curve.
    /// </returns>
    public static RegionContainment PlanarClosedCurveRelationship(Curve curveA, Curve curveB, Plane testPlane, double tolerance)
    {
      IntPtr pConstCurveA = curveA.ConstPointer();
      IntPtr pConstCurveB = curveB.ConstPointer();
      var rc = (RegionContainment)UnsafeNativeMethods.RHC_RhinoPlanarClosedCurveContainmentTest(pConstCurveA, pConstCurveB, ref testPlane, tolerance);
      Runtime.CommonObject.GcProtect(curveA, curveB);
      return rc;
    }

    /// <summary>
    /// Determines if two coplanar curves collide (intersect).
    /// </summary>
    /// <param name="curveA">A curve.</param>
    /// <param name="curveB">Another curve.</param>
    /// <param name="testPlane">A valid plane containing the curves.</param>
    /// <param name="tolerance">A tolerance value for intersection.</param>
    /// <returns>true if the curves intersect, otherwise false</returns>
    public static bool PlanarCurveCollision(Curve curveA, Curve curveB, Plane testPlane, double tolerance)
    {
      IntPtr pConstCurveA = curveA.ConstPointer();
      IntPtr pConstCurveB = curveB.ConstPointer();
      bool rc = UnsafeNativeMethods.RHC_RhinoPlanarCurveCollisionTest(pConstCurveA, pConstCurveB, ref testPlane, tolerance);
      Runtime.CommonObject.GcProtect(curveA, curveB);
      return rc;
    }

#endif
    #endregion statics

    #region constructors

    /// <summary>
    /// Protected constructor for internal use.
    /// </summary>
    // Giulio, 2016 Oct 2
    // Do not make this public or protected: any user might attempt to use this otherwise.
    internal Curve() { }

    internal Curve(IntPtr ptr, object parent)
      : this(ptr, parent, -1)
    {
    }
    internal Curve(IntPtr ptr, object parent, int subobject_index)
      : base(ptr, parent, subobject_index)
    {
    }

    internal override IntPtr _InternalGetConstPointer()
    {
      Rhino.Geometry.CurveHolder holder2 = m__parent as Rhino.Geometry.CurveHolder;
      if (null != holder2)
      {
        return holder2.ConstCurvePointer();
      }


      if (m_subobject_index >= 0)
      {
        Rhino.Geometry.PolyCurve polycurve_parent = m__parent as Rhino.Geometry.PolyCurve;
        if (polycurve_parent != null)
        {
          IntPtr pConstPolycurve = polycurve_parent.ConstPointer();
          IntPtr pConstThis = UnsafeNativeMethods.ON_PolyCurve_SegmentCurve(pConstPolycurve, m_subobject_index);
          return pConstThis;
        }
      }
      return base._InternalGetConstPointer();
    }

    //private PolyCurve m_parent_polycurve; //runtime will initialize this to null
    //internal void SetParentPolyCurve(PolyCurve curve)
    //{
    //  m_ptr = IntPtr.Zero;
    //  m_parent_polycurve = curve;
    //}

    /// <summary>
    /// Constructs an exact duplicate of this Curve.
    /// </summary>
    /// <seealso cref="DuplicateCurve"/>
    public override GeometryBase Duplicate()
    {
      IntPtr ptr = ConstPointer();
      IntPtr pNewCurve = UnsafeNativeMethods.ON_Curve_DuplicateCurve(ptr);
      return GeometryBase.CreateGeometryHelper(pNewCurve, null) as Curve;
    }

    /// <summary>
    /// Constructs an exact duplicate of this curve.
    /// </summary>
    /// <returns>An exact copy of this curve.</returns>
    /// <example>
    /// <code source='examples\vbnet\ex_curvereverse.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_curvereverse.cs' lang='cs'/>
    /// <code source='examples\py\ex_curvereverse.py' lang='py'/>
    /// </example>
    public Curve DuplicateCurve()
    {
      Curve rc = Duplicate() as Curve;
      return rc;
    }

    internal override GeometryBase DuplicateShallowHelper()
    {
      return new Curve(IntPtr.Zero, null);
    }

#if RHINO_SDK
    /// <summary>
    /// Polylines will be exploded into line segments. ExplodeCurves will
    /// return the curves in topological order.
    /// </summary>
    /// <returns>
    /// An array of all the segments that make up this curve.
    /// </returns>
    public Curve[] DuplicateSegments()
    {
      IntPtr ptr = ConstPointer();
      SimpleArrayCurvePointer output = new SimpleArrayCurvePointer();
      IntPtr outputPtr = output.NonConstPointer();

      int rc = UnsafeNativeMethods.RHC_RhinoDuplicateCurveSegments(ptr, outputPtr);
      return rc < 1 ? new Curve[0] : output.ToNonConstArray();
    }

    /// <summary>
    /// Smooths a curve by averaging the positions of control points in a specified region.
    /// </summary>
    /// <param name="smoothFactor">The smoothing factor, which controls how much control points move towards the average of the neighboring control points.</param>
    /// <param name="bXSmooth">When true control points move in X axis direction.</param>
    /// <param name="bYSmooth">When true control points move in Y axis direction.</param>
    /// <param name="bZSmooth">When true control points move in Z axis direction.</param>
    /// <param name="bFixBoundaries">When true the curve ends don't move.</param>
    /// <param name="coordinateSystem">The coordinates to determine the direction of the smoothing.</param>
    /// <returns>The smoothed curve if successful, null otherwise.</returns>
    public Curve Smooth(double smoothFactor, bool bXSmooth, bool bYSmooth, bool bZSmooth, bool bFixBoundaries, SmoothingCoordinateSystem coordinateSystem)
    {
      return Smooth(smoothFactor, bXSmooth, bYSmooth, bZSmooth, bFixBoundaries, coordinateSystem, Plane.WorldXY);
    }

    /// <summary>
    /// Smooths a curve by averaging the positions of control points in a specified region.
    /// </summary>
    /// <param name="smoothFactor">The smoothing factor, which controls how much control points move towards the average of the neighboring control points.</param>
    /// <param name="bXSmooth">When true control points move in X axis direction.</param>
    /// <param name="bYSmooth">When true control points move in Y axis direction.</param>
    /// <param name="bZSmooth">When true control points move in Z axis direction.</param>
    /// <param name="bFixBoundaries">When true the curve ends don't move.</param>
    /// <param name="coordinateSystem">The coordinates to determine the direction of the smoothing.</param>
    /// <param name="plane">If SmoothingCoordinateSystem.CPlane specified, then the construction plane.</param>
    /// <returns>The smoothed curve if successful, null otherwise.</returns>
    public Curve Smooth(double smoothFactor, bool bXSmooth, bool bYSmooth, bool bZSmooth, bool bFixBoundaries, SmoothingCoordinateSystem coordinateSystem, Plane plane)
    {
      IntPtr const_ptr_this = ConstPointer();
      IntPtr ptr = UnsafeNativeMethods.RHC_RhinoSmoothCurve(const_ptr_this, smoothFactor, bXSmooth, bYSmooth, bZSmooth, bFixBoundaries, (int)coordinateSystem, ref plane);
      return CreateGeometryHelper(ptr, null) as Curve;
    }

    /// <summary>
    /// Search for a location on the curve, near seedParmameter, that is perpendicular to a test point.
    /// </summary>
    /// <param name="testPoint">The test point.</param>
    /// <param name="seedParmameter">A "seed" parameter on the curve.</param>
    /// <param name="curveParameter">The parameter value at the perpendicular point</param>
    /// <returns>True if a solution is found, false otherwise.</returns>
    public bool GetLocalPerpPoint(Point3d testPoint, double seedParmameter, out double curveParameter)
    {
      curveParameter = RhinoMath.UnsetValue;
      IntPtr const_ptr_this = ConstPointer();
      return UnsafeNativeMethods.TLC_GetLocalPerpPoint(const_ptr_this, testPoint, seedParmameter, ref curveParameter, Interval.Unset);
    }

    /// <summary>
    /// Search for a location on the curve, near seedParmameter, that is perpendicular to a test point.
    /// </summary>
    /// <param name="testPoint">The test point.</param>
    /// <param name="seedParmameter">A "seed" parameter on the curve.</param>
    /// <param name="subDomain">The sub-domain of the curve to search.</param>
    /// <param name="curveParameter">The parameter value at the perpendicular point</param>
    /// <returns>True if a solution is found, false otherwise.</returns>
    public bool GetLocalPerpPoint(Point3d testPoint, double seedParmameter, Interval subDomain, out double curveParameter)
    {
      curveParameter = RhinoMath.UnsetValue;
      IntPtr const_ptr_this = ConstPointer();
      return UnsafeNativeMethods.TLC_GetLocalPerpPoint(const_ptr_this, testPoint, seedParmameter, ref curveParameter, subDomain);
    }

    /// <summary>
    /// Search for a location on the curve, near seedParmameter, that is tangent to a test point.
    /// </summary>
    /// <param name="testPoint">The test point.</param>
    /// <param name="seedParmameter">A "seed" parameter on the curve.</param>
    /// <param name="curveParameter">The parameter value at the tangent point</param>
    /// <returns>True if a solution is found, false otherwise.</returns>
    public bool GetLocalTangentPoint(Point3d testPoint, double seedParmameter, out double curveParameter)
    {
      curveParameter = RhinoMath.UnsetValue;
      IntPtr const_ptr_this = ConstPointer();
      return UnsafeNativeMethods.TLC_GetLocalTangentPoint(const_ptr_this, testPoint, seedParmameter, ref curveParameter, Interval.Unset);
    }

    /// <summary>
    /// Search for a location on the curve, near seedParmameter, that is tangent to a test point.
    /// </summary>
    /// <param name="testPoint">The test point.</param>
    /// <param name="seedParmameter">A "seed" parameter on the curve.</param>
    /// <param name="subDomain">The sub-domain of the curve to search.</param>
    /// <param name="curveParameter">The parameter value at the tangent point</param>
    /// <returns>True if a solution is found, false otherwise.</returns>
    public bool GetLocalTangentPoint(Point3d testPoint, double seedParmameter, Interval subDomain, out double curveParameter)
    {
      curveParameter = RhinoMath.UnsetValue;
      IntPtr const_ptr_this = ConstPointer();
      return UnsafeNativeMethods.TLC_GetLocalTangentPoint(const_ptr_this, testPoint, seedParmameter, ref curveParameter, subDomain);
    }

#endif

    internal override IntPtr _InternalDuplicate(out bool applymempressure)
    {
      applymempressure = true;
      IntPtr pConstPointer = ConstPointer();
      return UnsafeNativeMethods.ON_Curve_DuplicateCurve(pConstPointer);
    }

    /// <summary>
    /// For derived class implementers.
    /// <para>This method is called with argument true when class user calls Dispose(), while with argument false when
    /// the Garbage Collector invokes the finalizer, or Finalize() method.</para>
    /// <para>You must reclaim all used unmanaged resources in both cases, and can use this chance to call Dispose on disposable fields if the argument is true.</para>
    /// <para>Also, you must call the base virtual method within your overriding method.</para>
    /// </summary>
    /// <param name="disposing">true if the call comes from the Dispose() method; false if it comes from the Garbage Collector finalizer.</param>
    protected override void Dispose(bool disposing)
    {
#if RHINO_SDK
      if (IntPtr.Zero != m_pCurveDisplay)
      {
        UnsafeNativeMethods.CurveDisplay_Delete(m_pCurveDisplay);
        m_pCurveDisplay = IntPtr.Zero;
      }
#endif
      base.Dispose(disposing);
    }
    #endregion

    /// <summary>
    /// Protected serialization constructor for internal use.
    /// </summary>
    /// <param name="info">Serialization data.</param>
    /// <param name="context">Serialization stream.</param>
    protected Curve(SerializationInfo info, StreamingContext context)
      : base(info, context)
    {
    }

    #region internal methods
    const int idxIgnoreNone = 0;
    const int idxIgnorePlane = 1;
    const int idxIgnorePlaneArcOrEllipse = 2;

    /// <summary>
    /// For derived classes implementers.
    /// <para>Defines the necessary implementation to free the instance from being const.</para>
    /// </summary>
    protected override void NonConstOperation()
    {
#if RHINO_SDK
      if (IntPtr.Zero != m_pCurveDisplay)
      {
        UnsafeNativeMethods.CurveDisplay_Delete(m_pCurveDisplay);
        m_pCurveDisplay = IntPtr.Zero;
      }
#endif
      base.NonConstOperation();
    }

#if RHINO_SDK
    internal IntPtr m_pCurveDisplay = IntPtr.Zero;

    internal virtual void Draw(Display.DisplayPipeline pipeline, System.Drawing.Color color, int thickness)
    {
      IntPtr pDisplayPipeline = pipeline.NonConstPointer();
      IntPtr ptr = ConstPointer();
      int argb = color.ToArgb();
      UnsafeNativeMethods.CRhinoDisplayPipeline_DrawCurve(pDisplayPipeline, ptr, argb, thickness);
    }
#endif
    #endregion

    #region properties
    /// <summary>
    /// Gets or sets the domain of the curve.
    /// </summary>
    public Interval Domain
    {
      get
      {
        Interval rc = new Interval();
        IntPtr ptr = ConstPointer();
        UnsafeNativeMethods.ON_Curve_Domain(ptr, false, ref rc);
        return rc;
      }
      set
      {
        IntPtr ptr = NonConstPointer();
        UnsafeNativeMethods.ON_Curve_Domain(ptr, true, ref value);
      }
    }

    /// <summary>
    /// Gets the dimension of the object.
    /// <para>The dimension is typically three. For parameter space trimming
    /// curves the dimension is two. In rare cases the dimension can
    /// be one or greater than three.</para>
    /// </summary>
    public int Dimension
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        return UnsafeNativeMethods.ON_Geometry_Dimension(const_ptr_this);
      }
    }

    /// <summary>
    /// Changes the dimension of a curve.
    /// </summary>
    /// <param name="desiredDimension">The desired dimension.</param>
    /// <returns>
    /// true if the curve's dimension was already desiredDimension
    /// or if the curve's dimension was successfully changed to desiredDimension;
    /// otherwise false.
    /// </returns>
    public bool ChangeDimension(int desiredDimension)
    {
      // check dimension first so we don't need to switch to a non-const object
      // if possible
      if (Dimension == desiredDimension)
        return true;

      IntPtr pThis = NonConstPointer();
      return UnsafeNativeMethods.ON_Curve_ChangeDimension(pThis, desiredDimension);
    }

    /// <summary>
    /// Gets the number of non-empty smooth (c-infinity) spans in the curve.
    /// </summary>
    public int SpanCount
    {
      get
      {
        IntPtr ptr = ConstPointer();
        return UnsafeNativeMethods.ON_Curve_SpanCount(ptr);
      }
    }

    /// <summary>
    /// Gets the maximum algebraic degree of any span
    /// or a good estimate if curve spans are not algebraic.
    /// </summary>
    public int Degree
    {
      get
      {
        IntPtr ptr = ConstPointer();
        return UnsafeNativeMethods.ON_Curve_Degree(ptr);
      }
    }

    #region platonic shape properties
    /// <summary>
    /// Test a curve to see if it is linear to within RhinoMath.ZeroTolerance units (1e-12).
    /// </summary>
    /// <returns>true if the curve is linear.</returns>
    /// <example>
    /// <code source='examples\vbnet\ex_addradialdimension.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_addradialdimension.cs' lang='cs'/>
    /// <code source='examples\py\ex_addradialdimension.py' lang='py'/>
    /// </example>
    public bool IsLinear()
    {
      return IsLinear(RhinoMath.ZeroTolerance);
    }
    /// <summary>
    /// Test a curve to see if it is linear to within the custom tolerance.
    /// </summary>
    /// <param name="tolerance">Tolerance to use when checking linearity.</param>
    /// <returns>
    /// true if the ends of the curve are farther than tolerance apart
    /// and the maximum distance from any point on the curve to
    /// the line segment connecting the curve ends is &lt;= tolerance.
    /// </returns>
    public bool IsLinear(double tolerance)
    {
      IntPtr ptr = ConstPointer();
      return UnsafeNativeMethods.ON_Curve_IsLinear(ptr, tolerance);
    }

    /// <summary>
    /// Several types of Curve can have the form of a polyline
    /// including a degree 1 NurbsCurve, a PolylineCurve,
    /// and a PolyCurve all of whose segments are some form of
    /// polyline. IsPolyline tests a curve to see if it can be
    /// represented as a polyline.
    /// </summary>
    /// <returns>true if this curve can be represented as a polyline; otherwise, false.</returns>
    /// <example>
    /// <code source='examples\vbnet\ex_addradialdimension.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_addradialdimension.cs' lang='cs'/>
    /// <code source='examples\py\ex_addradialdimension.py' lang='py'/>
    /// </example>
    public bool IsPolyline()
    {
      IntPtr ptr = ConstPointer();
      return (UnsafeNativeMethods.ON_Curve_IsPolyline1(ptr, IntPtr.Zero) != 0);
    }
    /// <summary>
    /// Several types of Curve can have the form of a polyline 
    /// including a degree 1 NurbsCurve, a PolylineCurve, 
    /// and a PolyCurve all of whose segments are some form of 
    /// polyline. IsPolyline tests a curve to see if it can be 
    /// represented as a polyline.
    /// </summary>
    /// <param name="polyline">
    /// If true is returned, then the polyline form is returned here.
    /// </param>
    /// <returns>true if this curve can be represented as a polyline; otherwise, false.</returns>
    public bool TryGetPolyline(out Polyline polyline)
    {
      polyline = null;

      SimpleArrayPoint3d outputPts = new SimpleArrayPoint3d();
      int pointCount = 0;
      IntPtr pCurve = ConstPointer();
      IntPtr outputPointsPointer = outputPts.NonConstPointer();

      UnsafeNativeMethods.ON_Curve_IsPolyline2(pCurve, outputPointsPointer, ref pointCount, IntPtr.Zero);
      if (pointCount > 0)
      {
        polyline = Polyline.PolyLineFromNativeArray(outputPts);
      }

      outputPts.Dispose();
      return (pointCount != 0);
    }
    /// <summary>
    /// Several types of Curve can have the form of a polyline 
    /// including a degree 1 NurbsCurve, a PolylineCurve, 
    /// and a PolyCurve all of whose segments are some form of 
    /// polyline. IsPolyline tests a curve to see if it can be 
    /// represented as a polyline.
    /// </summary>
    /// <param name="polyline">
    /// If true is returned, then the polyline form is returned here.
    /// </param>
    /// <param name="parameters">
    /// if true is returned, then the parameters of the polyline
    /// points are returned here.
    /// </param>
    /// <returns>true if this curve can be represented as a polyline; otherwise, false.</returns>
    public bool TryGetPolyline(out Polyline polyline, out double[] parameters)
    {
      polyline = null;
      parameters = null;
      SimpleArrayPoint3d outputPts = new SimpleArrayPoint3d();
      int pointCount = 0;
      IntPtr pCurve = ConstPointer();
      IntPtr outputPointsPointer = outputPts.NonConstPointer();
      Rhino.Runtime.InteropWrappers.SimpleArrayDouble tparams = new SimpleArrayDouble();
      IntPtr ptparams = tparams.NonConstPointer();
      UnsafeNativeMethods.ON_Curve_IsPolyline2(pCurve, outputPointsPointer, ref pointCount, ptparams);
      if (pointCount > 0)
      {
        polyline = Polyline.PolyLineFromNativeArray(outputPts);
        parameters = tparams.ToArray();
      }

      tparams.Dispose();
      outputPts.Dispose();
      return (pointCount != 0);
    }

    /// <summary>
    /// Test a curve to see if it can be represented by an arc or circle within RhinoMath.ZeroTolerance.
    /// </summary>
    /// <returns>
    /// true if the curve can be represented by an arc or a circle within tolerance.
    /// </returns>
    public bool IsArc()
    {
      return IsArc(RhinoMath.ZeroTolerance);
    }
    /// <summary>
    /// Test a curve to see if it can be represented by an arc or circle within the given tolerance.
    /// </summary>
    /// <param name="tolerance">Tolerance to use when checking.</param>
    /// <returns>
    /// true if the curve can be represented by an arc or a circle within tolerance.
    /// </returns>
    public bool IsArc(double tolerance)
    {
      Arc arc = new Arc();
      Plane p = Plane.WorldXY;
      IntPtr ptr = ConstPointer();
      return UnsafeNativeMethods.ON_Curve_IsArc(ptr, idxIgnorePlaneArcOrEllipse, ref p, ref arc, tolerance);
    }
    /// <summary>
    /// Try to convert this curve into an Arc using RhinoMath.ZeroTolerance.
    /// </summary>
    /// <param name="arc">On success, the Arc will be filled in.</param>
    /// <returns>true if the curve could be converted into an arc.</returns>
    public bool TryGetArc(out Arc arc)
    {
      return TryGetArc(out arc, RhinoMath.ZeroTolerance);
    }
    /// <summary>
    /// Try to convert this curve into an Arc using a custom tolerance.
    /// </summary>
    /// <param name="arc">On success, the Arc will be filled in.</param>
    /// <param name="tolerance">Tolerance to use when checking.</param>
    /// <returns>true if the curve could be converted into an arc.</returns>
    public bool TryGetArc(out Arc arc, double tolerance)
    {
      arc = new Arc();
      Plane p = Plane.WorldXY;
      IntPtr ptr = ConstPointer();
      return UnsafeNativeMethods.ON_Curve_IsArc(ptr, idxIgnorePlane, ref p, ref arc, tolerance);
    }
    /// <summary>
    /// Try to convert this curve into an Arc using RhinoMath.ZeroTolerance.
    /// </summary>
    /// <param name="plane">Plane in which the comparison is performed.</param>
    /// <param name="arc">On success, the Arc will be filled in.</param>
    /// <returns>true if the curve could be converted into an arc within the given plane.</returns>
    public bool TryGetArc(Plane plane, out Arc arc)
    {
      return TryGetArc(plane, out arc, RhinoMath.ZeroTolerance);
    }
    /// <summary>
    /// Try to convert this curve into an Arc using a custom tolerance.
    /// </summary>
    /// <param name="plane">Plane in which the comparison is performed.</param>
    /// <param name="arc">On success, the Arc will be filled in.</param>
    /// <param name="tolerance">Tolerance to use when checking.</param>
    /// <returns>true if the curve could be converted into an arc within the given plane.</returns>
    public bool TryGetArc(Plane plane, out Arc arc, double tolerance)
    {
      arc = new Arc();
      IntPtr ptr = ConstPointer();
      return UnsafeNativeMethods.ON_Curve_IsArc(ptr, idxIgnoreNone, ref plane, ref arc, tolerance);
    }

    /// <summary>
    /// Test a curve to see if it can be represented by a circle within RhinoMath.ZeroTolerance.
    /// </summary>
    /// <returns>
    /// true if the Curve can be represented by a circle within tolerance.
    /// </returns>
    public bool IsCircle()
    {
      return IsCircle(RhinoMath.ZeroTolerance);
    }
    /// <summary>
    /// Test a curve to see if it can be represented by a circle within the given tolerance.
    /// </summary>
    /// <param name="tolerance">Tolerance to use when checking.</param>
    /// <returns>
    /// true if the curve can be represented by a circle to within tolerance.
    /// </returns>
    public bool IsCircle(double tolerance)
    {
      Arc arc;
      return TryGetArc(out arc, tolerance) && arc.IsCircle;
    }
    /// <summary>
    /// Try to convert this curve into a circle using RhinoMath.ZeroTolerance.
    /// </summary>
    /// <param name="circle">On success, the Circle will be filled in.</param>
    /// <returns>true if the curve could be converted into a Circle.</returns>
    /// <example>
    /// <code source='examples\vbnet\ex_customgeometryfilter.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_customgeometryfilter.cs' lang='cs'/>
    /// <code source='examples\py\ex_customgeometryfilter.py' lang='py'/>
    /// </example>
    public bool TryGetCircle(out Circle circle)
    {
      return TryGetCircle(out circle, RhinoMath.ZeroTolerance);
    }
    /// <summary>
    /// Try to convert this curve into a Circle using a custom tolerance.
    /// </summary>
    /// <param name="circle">On success, the Circle will be filled in.</param>
    /// <param name="tolerance">Tolerance to use when checking.</param>
    /// <returns>true if the curve could be converted into a Circle within tolerance.</returns>
    /// <example>
    /// <code source='examples\vbnet\ex_circlecenter.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_circlecenter.cs' lang='cs'/>
    /// <code source='examples\py\ex_circlecenter.py' lang='py'/>
    /// </example>
    public bool TryGetCircle(out Circle circle, double tolerance)
    {
      circle = new Circle();

      Arc arc;
      if (TryGetArc(out arc, tolerance))
      {
        if (arc.IsCircle)
        {
          circle = new Circle(arc);
          return true;
        }
      }
      return false;
    }

    /// <summary>
    /// Test a curve to see if it can be represented by an ellipse within RhinoMath.ZeroTolerance.
    /// </summary>
    /// <returns>
    /// true if the Curve can be represented by an ellipse within tolerance.
    /// </returns>
    public bool IsEllipse()
    {
      return IsEllipse(RhinoMath.ZeroTolerance);
    }
    /// <summary>
    /// Test a curve to see if it can be represented by an ellipse within a given tolerance.
    /// </summary>
    /// <param name="tolerance">Tolerance to use for checking.</param>
    /// <returns>
    /// true if the Curve can be represented by an ellipse within tolerance.
    /// </returns>
    public bool IsEllipse(double tolerance)
    {
      Plane plane = Plane.WorldXY;
      Ellipse ellipse = new Ellipse();
      IntPtr ptr = ConstPointer();
      return UnsafeNativeMethods.ON_Curve_IsEllipse(ptr, idxIgnorePlaneArcOrEllipse, ref plane, ref ellipse, tolerance);
    }
    /// <summary>
    /// Try to convert this curve into an Ellipse within RhinoMath.ZeroTolerance.
    /// </summary>
    /// <param name="ellipse">On success, the Ellipse will be filled in.</param>
    /// <returns>true if the curve could be converted into an Ellipse.</returns>
    public bool TryGetEllipse(out Ellipse ellipse)
    {
      return TryGetEllipse(out ellipse, RhinoMath.ZeroTolerance);
    }
    /// <summary>
    /// Try to convert this curve into an Ellipse using a custom tolerance.
    /// </summary>
    /// <param name="ellipse">On success, the Ellipse will be filled in.</param>
    /// <param name="tolerance">Tolerance to use when checking.</param>
    /// <returns>true if the curve could be converted into an Ellipse.</returns>
    public bool TryGetEllipse(out Ellipse ellipse, double tolerance)
    {
      Plane plane = Plane.WorldXY;
      ellipse = new Ellipse();
      IntPtr ptr = ConstPointer();
      return UnsafeNativeMethods.ON_Curve_IsEllipse(ptr, idxIgnorePlane, ref plane, ref ellipse, tolerance);
    }
    /// <summary>
    /// Try to convert this curve into an Ellipse within RhinoMath.ZeroTolerance.
    /// </summary>
    /// <param name="plane">Plane in which the comparison is performed.</param>
    /// <param name="ellipse">On success, the Ellipse will be filled in.</param>
    /// <returns>true if the curve could be converted into an Ellipse within the given plane.</returns>
    public bool TryGetEllipse(Plane plane, out Ellipse ellipse)
    {
      return TryGetEllipse(plane, out ellipse, RhinoMath.ZeroTolerance);
    }
    /// <summary>
    /// Try to convert this curve into an Ellipse using a custom tolerance.
    /// </summary>
    /// <param name="plane">Plane in which the comparison is performed.</param>
    /// <param name="ellipse">On success, the Ellipse will be filled in.</param>
    /// <param name="tolerance">Tolerance to use when checking.</param>
    /// <returns>true if the curve could be converted into an Ellipse within the given plane.</returns>
    public bool TryGetEllipse(Plane plane, out Ellipse ellipse, double tolerance)
    {
      ellipse = new Ellipse();
      IntPtr ptr = ConstPointer();
      return UnsafeNativeMethods.ON_Curve_IsEllipse(ptr, idxIgnoreNone, ref plane, ref ellipse, tolerance);
    }

    /// <summary>Test a curve for planarity.</summary>
    /// <returns>
    /// true if the curve is planar (flat) to within RhinoMath.ZeroTolerance units (1e-12).
    /// </returns>
    /// <example>
    /// <code source='examples\vbnet\ex_addradialdimension.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_addradialdimension.cs' lang='cs'/>
    /// <code source='examples\py\ex_addradialdimension.py' lang='py'/>
    /// </example>
    public bool IsPlanar()
    {
      return IsPlanar(RhinoMath.ZeroTolerance);
    }
    /// <summary>Test a curve for planarity.</summary>
    /// <param name="tolerance">Tolerance to use when checking.</param>
    /// <returns>
    /// true if there is a plane such that the maximum distance from the curve to the plane is &lt;= tolerance.
    /// </returns>
    public bool IsPlanar(double tolerance)
    {
      Plane plane = Plane.WorldXY;
      IntPtr ptr = ConstPointer();
      return UnsafeNativeMethods.ON_Curve_IsPlanar(ptr, true, ref plane, tolerance);
    }
    /// <summary>Test a curve for planarity and return the plane.</summary>
    /// <param name="plane">On success, the plane parameters are filled in.</param>
    /// <returns>
    /// true if there is a plane such that the maximum distance from the curve to the plane is &lt;= RhinoMath.ZeroTolerance.
    /// </returns>
    /// <example>
    /// <code source='examples\vbnet\ex_constrainedcopy.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_constrainedcopy.cs' lang='cs'/>
    /// <code source='examples\py\ex_constrainedcopy.py' lang='py'/>
    /// </example>
    public bool TryGetPlane(out Plane plane)
    {
      return TryGetPlane(out plane, RhinoMath.ZeroTolerance);
    }
    /// <summary>Test a curve for planarity and return the plane.</summary>
    /// <param name="plane">On success, the plane parameters are filled in.</param>
    /// <param name="tolerance">Tolerance to use when checking.</param>
    /// <returns>
    /// true if there is a plane such that the maximum distance from the curve to the plane is &lt;= tolerance.
    /// </returns>
    public bool TryGetPlane(out Plane plane, double tolerance)
    {
      plane = Plane.WorldXY;
      IntPtr ptr = ConstPointer();
      return UnsafeNativeMethods.ON_Curve_IsPlanar(ptr, false, ref plane, tolerance);
    }
    /// <summary>Test a curve to see if it lies in a specific plane.</summary>
    /// <param name="testPlane">Plane to test for.</param>
    /// <returns>
    /// true if the maximum distance from the curve to the testPlane is &lt;= RhinoMath.ZeroTolerance.
    /// </returns>
    public bool IsInPlane(Plane testPlane)
    {
      return IsInPlane(testPlane, RhinoMath.ZeroTolerance);
    }
    /// <summary>Test a curve to see if it lies in a specific plane.</summary>
    /// <param name="testPlane">Plane to test for.</param>
    /// <param name="tolerance">Tolerance to use when checking.</param>
    /// <returns>
    /// true if the maximum distance from the curve to the testPlane is &lt;= tolerance.
    /// </returns>
    public bool IsInPlane(Plane testPlane, double tolerance)
    {
      IntPtr ptr = ConstPointer();
      return UnsafeNativeMethods.ON_Curve_IsInPlane(ptr, ref testPlane, tolerance);
    }
    #endregion
    #endregion

    #region methods
    /// <summary>
    /// If this curve is closed, then modify it so that the start/end point is at curve parameter t.
    /// </summary>
    /// <param name="t">
    /// Curve parameter of new start/end point. The returned curves domain will start at t.
    /// </param>
    /// <returns>true on success, false on failure.</returns>
    public bool ChangeClosedCurveSeam(double t)
    {
      IntPtr ptr = NonConstPointer();
      bool rc = UnsafeNativeMethods.ON_Curve_ChangeClosedCurveSeam(ptr, t);
      return rc;
    }

    const int idxIsClosed = 0;
    const int idxIsPeriodic = 1;

    /// <summary>
    /// Gets a value indicating whether or not this curve is a closed curve.
    /// </summary>
    public bool IsClosed
    {
      get
      {
        IntPtr ptr = ConstPointer();
        return UnsafeNativeMethods.ON_Curve_GetBool(ptr, idxIsClosed);
      }
    }
    /// <summary>
    /// Gets a value indicating whether or not this curve is considered to be Periodic.
    /// </summary>
    public bool IsPeriodic
    {
      get
      {
        IntPtr ptr = ConstPointer();
        return UnsafeNativeMethods.ON_Curve_GetBool(ptr, idxIsPeriodic);
      }
    }

    /// <summary>
    /// Decide if it makes sense to close off this curve by moving the endpoint 
    /// to the start based on start-end gap size and length of curve as 
    /// approximated by chord defined by 6 points.
    /// </summary>
    /// <param name="tolerance">
    /// Maximum allowable distance between start and end. 
    /// If start - end gap is greater than tolerance, this function will return false.
    /// </param>
    /// <returns>true if start and end points are close enough based on above conditions.</returns>
    [ConstOperation]
    public bool IsClosable(double tolerance)
    {
      return IsClosable(tolerance, 0.0, 10.0);
    }
    /// <summary>
    /// Decide if it makes sense to close off this curve by moving the endpoint
    /// to the start based on start-end gap size and length of curve as
    /// approximated by chord defined by 6 points.
    /// </summary>
    /// <param name="tolerance">
    /// Maximum allowable distance between start and end. 
    /// If start - end gap is greater than tolerance, this function will return false.
    /// </param>
    /// <param name="minimumAbsoluteSize">
    /// If greater than 0.0 and none of the interior sampled points are at
    /// least minimumAbsoluteSize from start, this function will return false.
    /// </param>
    /// <param name="minimumRelativeSize">
    /// If greater than 1.0 and chord length is less than 
    /// minimumRelativeSize*gap, this function will return false.
    /// </param>
    /// <returns>true if start and end points are close enough based on above conditions.</returns>
    [ConstOperation]
    public bool IsClosable(double tolerance, double minimumAbsoluteSize, double minimumRelativeSize)
    {
      IntPtr ptr = ConstPointer();
      bool rc = UnsafeNativeMethods.ON_Curve_IsClosable(ptr, tolerance, minimumAbsoluteSize, minimumRelativeSize);
      return rc;
    }

#if RHINO_SDK
    /// <summary>
    /// If IsClosed, just return true. Otherwise, decide if curve can be closed as 
    /// follows: Linear curves polylinear curves with 2 segments, Nurbs with 3 or less 
    /// control points cannot be made closed. Also, if tolerance > 0 and the gap between 
    /// start and end is larger than tolerance, curve cannot be made closed. 
    /// Adjust the curve's endpoint to match its start point.
    /// </summary>
    /// <param name="tolerance">
    /// If nonzero, and the gap is more than tolerance, curve cannot be made closed.
    /// </param>
    /// <returns>true on success, false on failure.</returns>
    public bool MakeClosed(double tolerance)
    {
      if (IsClosed)
        return true;
      IntPtr ptr = NonConstPointer();
      return UnsafeNativeMethods.RHC_RhinoMakeCurveClosed(ptr, tolerance);
    }
#endif

    /// <summary>
    /// Determines the orientation (counterclockwise or clockwise) of a closed, planar curve in the world xy plane.
    /// Only works with simple (no self intersections) closed, planar curves.
    /// </summary>
    /// <returns>The orientation of this curve with respect to world xy plane.</returns>
    [ConstOperation]
    public CurveOrientation ClosedCurveOrientation()
    {
      // 10-Feb-2016 Dale Fugier, http://mcneel.myjetbrains.com/youtrack/issue/RH-32952
      return ClosedCurveOrientation(Geometry.Transform.Identity);
    }

    /// <summary>
    /// Determines the orientation (counterclockwise or clockwise) of a closed, planar curve in a given plane.
    /// Only works with simple (no self intersections) closed, planar curves.
    /// </summary>
    /// <param name="upDirection">A vector that is considered "up".</param>
    /// <returns>The orientation of this curve with respect to a defined up direction.</returns>
    [ConstOperation]
    public CurveOrientation ClosedCurveOrientation(Vector3d upDirection)
    {
      var plane = new Plane(Point3d.Origin, upDirection);
      return ClosedCurveOrientation(plane);
    }
    /// <summary>
    /// Determines the orientation (counterclockwise or clockwise) of a closed, planar curve in a given plane.
    /// Only works with simple (no self intersections) closed, planar curves.
    /// </summary>
    /// <param name="plane">
    /// The plane in which to solve the orientation.
    /// </param>
    /// <returns>The orientation of this curve in the given plane.</returns>
    [ConstOperation]
    public CurveOrientation ClosedCurveOrientation(Plane plane)
    {
      if (!plane.IsValid) { return CurveOrientation.Undefined; }

      //WARNING! David wrote this code without testing it. Is the order of planes in the ChangeBasis function correct?
      //Transform xform = Geometry.Transform.ChangeBasis(Plane.WorldXY, plane);

      // 10-Feb-2016 Dale Fugier, http://mcneel.myjetbrains.com/youtrack/issue/RH-32952
      var xform = Geometry.Transform.PlaneToPlane(plane, Plane.WorldXY);
      return ClosedCurveOrientation(xform);
    }
    /// <summary>
    /// Determines the orientation (counterclockwise or clockwise) of a closed, planar curve.
    /// Only works with simple (no self intersections) closed, planar curves.
    /// </summary>
    /// <param name="xform">
    /// Transformation to map the curve to the world xy plane.
    /// </param>
    /// <returns>The orientation of this curve in the world xy-plane.</returns>
    [ConstOperation]
    public CurveOrientation ClosedCurveOrientation(Transform xform)
    {
      // 10-Feb-2016 Dale Fugier, http://mcneel.myjetbrains.com/youtrack/issue/RH-32952
      var ptr = ConstPointer();
      var retval = UnsafeNativeMethods.ON_Curve_ClosedCurveOrientation(ptr, ref xform);
      var rc = CurveOrientation.Undefined;
      switch (retval)
      {
        case 1:
          // +1: The curve's orientation is counter clockwise in the xy plane.
          rc = CurveOrientation.CounterClockwise;
          break;
        case -1:
          // -1: The curve's orientation is clockwise in the xy plane.
          rc = CurveOrientation.Clockwise;
          break;
      }
      return rc;
    }

    /// <summary>
    /// Reverses the direction of the curve.
    /// </summary>
    /// <returns>true on success, false on failure.</returns>
    /// <remarks>If reversed, the domain changes from [a,b] to [-b,-a]</remarks>
    /// <example>
    /// <code source='examples\vbnet\ex_curvereverse.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_curvereverse.cs' lang='cs'/>
    /// <code source='examples\py\ex_curvereverse.py' lang='py'/>
    /// </example>
    public bool Reverse()
    {
      IntPtr ptr = NonConstPointer();
      return UnsafeNativeMethods.ON_Curve_Reverse(ptr);
    }

#if RHINO_SDK

    /// <summary>
    /// Find parameter of the point on a curve that is locally closest to 
    /// the testPoint.  The search for a local close point starts at
    /// a seed parameter.
    /// </summary>
    /// <param name="testPoint">A point to test against.</param>
    /// <param name="seed">The seed parameter.</param>
    /// <param name="t">>Parameter of the curve that is closest to testPoint.</param>
    /// <returns>true if the search is successful, false if the search fails.</returns>
    [ConstOperation]
    public bool LcoalClosestPoint(Point3d testPoint, double seed, out double t)
    {
      t = 0.0;
      IntPtr ptr = ConstPointer();
      bool rc = UnsafeNativeMethods.ON_Curve_GetLocalClosestPoint(ptr, testPoint, seed, ref t);
      return rc;
    }

    /// <summary>
    /// Finds parameter of the point on a curve that is closest to testPoint.
    /// If the maximumDistance parameter is > 0, then only points whose distance
    /// to the given point is &lt;= maximumDistance will be returned.  Using a 
    /// positive value of maximumDistance can substantially speed up the search.
    /// </summary>
    /// <param name="testPoint">Point to search from.</param>
    /// <param name="t">Parameter of local closest point.</param>
    /// <returns>true on success, false on failure.</returns>
    [ConstOperation]
    public bool ClosestPoint(Point3d testPoint, out double t)
    {
      return ClosestPoint(testPoint, out t, -1.0);
    }

    /// <summary>
    /// Finds the parameter of the point on a curve that is closest to testPoint.
    /// If the maximumDistance parameter is > 0, then only points whose distance
    /// to the given point is &lt;= maximumDistance will be returned.  Using a 
    /// positive value of maximumDistance can substantially speed up the search.
    /// </summary>
    /// <param name="testPoint">Point to project.</param>
    /// <param name="t">parameter of local closest point returned here.</param>
    /// <param name="maximumDistance">The maximum allowed distance.
    /// <para>Past this distance, the search is given up and false is returned.</para>
    /// <para>Use 0 to turn off this parameter.</para></param>
    /// <returns>true on success, false on failure.</returns>
    [ConstOperation]
    public bool ClosestPoint(Point3d testPoint, out double t, double maximumDistance)
    {
      t = 0.0;
      IntPtr ptr = ConstPointer();
      bool rc = UnsafeNativeMethods.ON_Curve_GetClosestPoint(ptr, testPoint, ref t, maximumDistance);
      return rc;
    }

    /// <summary>
    /// Finds the object (and the closest point in that object) that is closest to
    /// this curve. <para><see cref="Brep">Breps</see>, <see cref="Surface">surfaces</see>,
    /// <see cref="Curve">curves</see> and <see cref="PointCloud">point clouds</see> are examples of
    /// objects that can be passed to this function.</para>
    /// </summary>
    /// <param name="geometry">A list, an array or any enumerable set of geometry to search.</param>
    /// <param name="pointOnCurve">The point on curve. This out parameter is assigned during this call.</param>
    /// <param name="pointOnObject">The point on geometry. This out parameter is assigned during this call.</param>
    /// <param name="whichGeometry">The index of the geometry. This out parameter is assigned during this call.</param>
    /// <param name="maximumDistance">Maximum allowable distance. Past this distance, the research is given up and false is returned.</param>
    /// <returns>true on success; false if no object was found or selected.</returns>
    /// <exception cref="ArgumentNullException">If geometry is null.</exception>
    [ConstOperation]
    public bool ClosestPoints(IEnumerable<GeometryBase> geometry,
      out Point3d pointOnCurve,
      out Point3d pointOnObject,
      out int whichGeometry,
      double maximumDistance)
    {
      if (geometry == null) throw new ArgumentNullException("geometry");

      using (SimpleArrayGeometryPointer geom = new SimpleArrayGeometryPointer(geometry))
      {
        pointOnCurve = Point3d.Unset;
        pointOnObject = Point3d.Unset;
        IntPtr pConstThis = ConstPointer();
        IntPtr pGeometryArray = geom.ConstPointer();
        whichGeometry = 0;
        bool rc = UnsafeNativeMethods.RHC_RhinoGetClosestPoint(pConstThis, pGeometryArray, maximumDistance, ref pointOnCurve, ref pointOnObject, ref whichGeometry);
        GC.KeepAlive(geometry);
        return rc;
      }
    }

    /// <summary>
    /// Finds the object (and the closest point in that object) that is closest to
    /// this curve. <para><see cref="Brep">Breps</see>, <see cref="Surface">surfaces</see>,
    /// <see cref="Curve">curves</see> and <see cref="PointCloud">point clouds</see> are examples of
    /// objects that can be passed to this function.</para>
    /// </summary>
    /// <param name="geometry">A list, an array or any enumerable set of geometry to search.</param>
    /// <param name="pointOnCurve">The point on curve. This out parameter is assigned during this call.</param>
    /// <param name="pointOnObject">The point on geometry. This out parameter is assigned during this call.</param>
    /// <param name="whichGeometry">The index of the geometry. This out parameter is assigned during this call.</param>
    /// <returns>true on success; false if no object was found or selected.</returns>
    /// <exception cref="ArgumentNullException">If geometry is null.</exception>
    [ConstOperation]
    public bool ClosestPoints(IEnumerable<GeometryBase> geometry,
      out Point3d pointOnCurve,
      out Point3d pointOnObject,
      out int whichGeometry)
    {
      return ClosestPoints(geometry, out pointOnCurve, out pointOnObject, out whichGeometry, 0.0);
    }

    /// <summary>
    /// Gets closest points between this and another curves.
    /// </summary>
    /// <param name="otherCurve">The other curve.</param>
    /// <param name="pointOnThisCurve">The point on this curve. This out parameter is assigned during this call.</param>
    /// <param name="pointOnOtherCurve">The point on other curve. This out parameter is assigned during this call.</param>
    /// <returns>true on success; false on error.</returns>
    [ConstOperation]
    public bool ClosestPoints(Curve otherCurve, out Point3d pointOnThisCurve, out Point3d pointOnOtherCurve)
    {
      GeometryBase[] a = new GeometryBase[] { otherCurve };
      int which;
      return ClosestPoints(a, out pointOnThisCurve, out pointOnOtherCurve, out which, 0.0);
    }

    /// <summary>
    /// Computes the relationship between a point and a closed curve region. 
    /// This curve must be closed or the return value will be Unset.
    /// Both curve and point are projected to the World XY plane.
    /// </summary>
    /// <param name="testPoint">Point to test.</param>
    /// <returns>Relationship between point and curve region.</returns>
    [Obsolete("Use version that takes a tolerance")]
    [ConstOperation]
    public PointContainment Contains(Point3d testPoint)
    {
      return Contains(testPoint, Plane.WorldXY, RhinoMath.UnsetValue);
    }
    /// <summary>
    /// Computes the relationship between a point and a closed curve region. 
    /// This curve must be closed or the return value will be Unset.
    /// </summary>
    /// <param name="testPoint">Point to test.</param>
    /// <param name="plane">Plane in in which to compare point and region.</param>
    /// <returns>Relationship between point and curve region.</returns>
    [Obsolete("Use version that takes a tolerance")]
    [ConstOperation]
    public PointContainment Contains(Point3d testPoint, Plane plane)
    {
      return Contains(testPoint, plane, RhinoMath.UnsetValue);
    }
    /// <summary>
    /// Computes the relationship between a point and a closed curve region. 
    /// This curve must be closed or the return value will be Unset.
    /// </summary>
    /// <param name="testPoint">Point to test.</param>
    /// <param name="plane">Plane in in which to compare point and region.</param>
    /// <param name="tolerance">Tolerance to use during comparison.</param>
    /// <returns>Relationship between point and curve region.</returns>
    [ConstOperation]
    public PointContainment Contains(Point3d testPoint, Plane plane, double tolerance)
    {
      if (testPoint.IsValid && plane.IsValid && IsClosed)
      {
        IntPtr ptr = ConstPointer();
        int rc = UnsafeNativeMethods.RHC_PointInClosedRegion(ptr, testPoint, plane, tolerance);

        if (0 == rc)
          return PointContainment.Outside;
        if (1 == rc)
          return PointContainment.Inside;
        if (2 == rc)
          return PointContainment.Coincident;
      }
      return PointContainment.Unset;
    }

    /// <summary>
    /// Returns the parameter values of all local extrema. 
    /// Parameter values are in increasing order so consecutive extrema 
    /// define an interval on which each component of the curve is monotone. 
    /// Note, non-periodic curves always return the end points.
    /// </summary>
    /// <param name="direction">The direction in which to perform the calculation.</param>
    /// <returns>The parameter values of all local extrema.</returns>
    [ConstOperation]
    public double[] ExtremeParameters(Vector3d direction)
    {
      using (var array_double = new SimpleArrayDouble())
      {
        IntPtr ptr_array_double = array_double.NonConstPointer();
        IntPtr ptr = ConstPointer();
        int rc = UnsafeNativeMethods.RHC_RhinoCurveExtremeParameters(ptr, direction, ptr_array_double);
        return rc > 0 ? array_double.ToArray() : new double[0];
      }
    }

    /// <summary>
    /// Removes kinks from a curve. Periodic curves deform smoothly without kinks.
    /// </summary>
    /// <param name="curve">The curve to make periodic. Curve must have degree >= 2.</param>
    /// <returns>The resulting curve if successful, null otherwise.</returns>
    public static Curve CreatePeriodicCurve(Curve curve)
    {
      return CreatePeriodicCurve(curve, true);
    }

    /// <summary>
    /// Removes kinks from a curve. Periodic curves deform smoothly without kinks.
    /// </summary>
    /// <param name="curve">The curve to make periodic. Curve must have degree >= 2.</param>
    /// <param name="smooth">
    /// If true, smooths any kinks in the curve and moves control points to make a smooth curve. 
    /// If false, control point locations are not changed or changed minimally (only one point may move) and only the knot vector is altered.
    /// </param>
    /// <returns>The resulting curve if successful, null otherwise.</returns>
    public static Curve CreatePeriodicCurve(Curve curve, bool smooth)
    {
      if (null == curve)
        throw new ArgumentNullException("curve");
      IntPtr const_ptr_curve = curve.ConstPointer();
      IntPtr ptr = UnsafeNativeMethods.RHC_RhinoMakeCurvePeriodic(const_ptr_curve, smooth);
      GC.KeepAlive(curve);
      return GeometryBase.CreateGeometryHelper(ptr, null) as Curve;
    }

#endif

    #region evaluators
    // [skipping]
    // BOOL EvPoint
    // BOOL Ev1Der
    // BOOL Ev2Der
    // BOOL EvTangent
    // BOOL EvCurvature
    // BOOL Evaluate

    const int idxPointAtT = 0;
    const int idxPointAtStart = 1;
    const int idxPointAtEnd = 2;

    /// <summary>Evaluates point at a curve parameter.</summary>
    /// <param name="t">Evaluation parameter.</param>
    /// <returns>Point (location of curve at the parameter t).</returns>
    /// <remarks>No error handling.</remarks>
    /// <example>
    /// <code source='examples\vbnet\ex_addradialdimension.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_addradialdimension.cs' lang='cs'/>
    /// <code source='examples\py\ex_addradialdimension.py' lang='py'/>
    /// </example>
    [ConstOperation]
    public Point3d PointAt(double t)
    {
      Point3d rc = new Point3d();
      IntPtr ptr = ConstPointer();
      UnsafeNativeMethods.ON_Curve_PointAt(ptr, t, ref rc, idxPointAtT);
      return rc;
    }
    /// <summary>
    /// Evaluates point at the start of the curve.
    /// </summary>
    public Point3d PointAtStart
    {
      get
      {
        Point3d rc = new Point3d();
        IntPtr ptr = ConstPointer();
        UnsafeNativeMethods.ON_Curve_PointAt(ptr, 0, ref rc, idxPointAtStart);
        return rc;
      }
    }
    /// <summary>
    /// Evaluates point at the end of the curve.
    /// </summary>
    public Point3d PointAtEnd
    {
      get
      {
        Point3d rc = new Point3d();
        IntPtr ptr = ConstPointer();
        UnsafeNativeMethods.ON_Curve_PointAt(ptr, 1, ref rc, idxPointAtEnd);
        return rc;
      }
    }

#if RHINO_SDK
    /// <summary>
    /// Gets a point at a certain length along the curve. The length must be 
    /// non-negative and less than or equal to the length of the curve. 
    /// Lengths will not be wrapped when the curve is closed or periodic.
    /// </summary>
    /// <param name="length">Length along the curve between the start point and the returned point.</param>
    /// <returns>Point on the curve at the specified length from the start point or Poin3d.Unset on failure.</returns>
    /// <example>
    /// <code source='examples\vbnet\ex_arclengthpoint.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_arclengthpoint.cs' lang='cs'/>
    /// <code source='examples\py\ex_arclengthpoint.py' lang='py'/>
    /// </example>
    [ConstOperation]
    public Point3d PointAtLength(double length)
    {
      double t;
      return !LengthParameter(length, out t) ? Point3d.Unset : PointAt(t);
    }
    /// <summary>
    /// Gets a point at a certain normalized length along the curve. The length must be 
    /// between or including 0.0 and 1.0, where 0.0 equals the start of the curve and 
    /// 1.0 equals the end of the curve. 
    /// </summary>
    /// <param name="length">Normalized length along the curve between the start point and the returned point.</param>
    /// <returns>Point on the curve at the specified normalized length from the start point or Poin3d.Unset on failure.</returns>
    [ConstOperation]
    public Point3d PointAtNormalizedLength(double length)
    {
      double t;
      return !NormalizedLengthParameter(length, out t) ? Point3d.Unset : PointAt(t);
    }
#endif

    /// <summary>Forces the curve to start at a specified point. 
    /// Not all curve types support this operation.</summary>
    /// <param name="point">New start point of curve.</param>
    /// <returns>true on success, false on failure.</returns>
    /// <remarks>Some start points cannot be moved. Be sure to check return code.</remarks>
    public bool SetStartPoint(Point3d point)
    {
      IntPtr ptr = NonConstPointer();
      return UnsafeNativeMethods.ON_Curve_SetPoint(ptr, point, true);
    }
    /// <summary>Forces the curve to end at a specified point. 
    /// Not all curve types support this operation.</summary>
    /// <param name="point">New end point of curve.</param>
    /// <returns>true on success, false on failure.</returns>
    /// <remarks>Some end points cannot be moved. Be sure to check return code</remarks>
    public bool SetEndPoint(Point3d point)
    {
      IntPtr ptr = NonConstPointer();
      return UnsafeNativeMethods.ON_Curve_SetPoint(ptr, point, false);
    }

    /// <summary>Evaluates the unit tangent vector at a curve parameter.</summary>
    /// <param name="t">Evaluation parameter.</param>
    /// <returns>Unit tangent vector of the curve at the parameter t.</returns>
    /// <remarks>No error handling.</remarks>
    [ConstOperation]
    public Vector3d TangentAt(double t)
    {
      Vector3d rc = new Vector3d();
      IntPtr ptr = ConstPointer();
      UnsafeNativeMethods.ON_Curve_GetVector(ptr, idxTangentAt, t, ref rc);
      return rc;
    }
    /// <summary>Evaluates the unit tangent vector at the start of the curve.</summary>
    /// <returns>Unit tangent vector of the curve at the start point.</returns>
    /// <remarks>No error handling.</remarks>
    public Vector3d TangentAtStart
    {
      get { return TangentAt(Domain.Min); }
    }
    /// <summary>Evaluate unit tangent vector at the end of the curve.</summary>
    /// <returns>Unit tangent vector of the curve at the end point.</returns>
    /// <remarks>No error handling.</remarks>
    public Vector3d TangentAtEnd
    {
      get { return TangentAt(Domain.Max); }
    }

    const int idxDerivativeAt = 0;
    const int idxTangentAt = 1;
    const int idxCurvatureAt = 2;

    /// <summary>Returns a 3d frame at a parameter.</summary>
    /// <param name="t">Evaluation parameter.</param>
    /// <param name="plane">The frame is returned here.</param>
    /// <returns>true on success, false on failure.</returns>
    [ConstOperation]
    public bool FrameAt(double t, out Plane plane)
    {
      plane = Plane.WorldXY;
      IntPtr ptr = ConstPointer();
      return UnsafeNativeMethods.ON_Curve_FrameAt(ptr, t, ref plane, false);
    }

    /// <summary>
    /// Evaluate the derivatives at the specified curve parameter.
    /// </summary>
    /// <param name="t">Curve parameter to evaluate.</param>
    /// <param name="derivativeCount">Number of derivatives to evaluate, must be at least 0.</param>
    /// <returns>An array of vectors that represents all the derivatives starting at zero.</returns>
    [ConstOperation]
    public Vector3d[] DerivativeAt(double t, int derivativeCount)
    {
      return DerivativeAt(t, derivativeCount, CurveEvaluationSide.Default);
    }
    /// <summary>
    /// Evaluate the derivatives at the specified curve parameter.
    /// </summary>
    /// <param name="t">Curve parameter to evaluate.</param>
    /// <param name="derivativeCount">Number of derivatives to evaluate, must be at least 0.</param>
    /// <param name="side">Side of parameter to evaluate. If the parameter is at a kink, 
    /// it makes a big difference whether the evaluation is from below or above.</param>
    /// <returns>An array of vectors that represents all the derivatives starting at zero.</returns>
    [ConstOperation]
    public Vector3d[] DerivativeAt(double t, int derivativeCount, CurveEvaluationSide side)
    {
      if (derivativeCount < 0) { throw new InvalidOperationException("The derivativeCount must be larger than or equal to zero"); }

      Vector3d[] rc = null;
      SimpleArrayPoint3d points = new SimpleArrayPoint3d();
      IntPtr pPoints = points.NonConstPointer();
      if (UnsafeNativeMethods.ON_Curve_Evaluate(ConstPointer(), derivativeCount, (int)side, t, pPoints))
      {
        Point3d[] pts = points.ToArray();
        rc = new Vector3d[pts.Length];
        for (int i = 0; i < pts.Length; i++)
        {
          rc[i] = new Vector3d(pts[i]);
        }
      }
      points.Dispose();
      return rc;
    }
    /// <summary>Evaluate the curvature vector at a curve parameter.</summary>
    /// <param name="t">Evaluation parameter.</param>
    /// <returns>Curvature vector of the curve at the parameter t.</returns>
    /// <remarks>No error handling.</remarks>
    /// <example>
    /// <code source='examples\vbnet\ex_addradialdimension.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_addradialdimension.cs' lang='cs'/>
    /// <code source='examples\py\ex_addradialdimension.py' lang='py'/>
    /// </example>
    [ConstOperation]
    public Vector3d CurvatureAt(double t)
    {
      Vector3d rc = new Vector3d();
      IntPtr ptr = ConstPointer();
      UnsafeNativeMethods.ON_Curve_GetVector(ptr, idxCurvatureAt, t, ref rc);
      return rc;
    }

#if RHINO_SDK
    /// <summary>
    /// Return a 3d frame at a parameter. This is slightly different than FrameAt in
    /// that the frame is computed in a way so there is minimal rotation from one
    /// frame to the next.
    /// </summary>
    /// <param name="t">Evaluation parameter.</param>
    /// <param name="plane">The frame is returned here.</param>
    /// <returns>true on success, false on failure.</returns>
    [ConstOperation]
    public bool PerpendicularFrameAt(double t, out Plane plane)
    {
      plane = Plane.WorldXY;
      IntPtr ptr = ConstPointer();
      return UnsafeNativeMethods.ON_Curve_FrameAt(ptr, t, ref plane, true);
    }

    /// <summary>
    /// Gets a collection of perpendicular frames along the curve. Perpendicular frames 
    /// are also known as 'Zero-twisting frames' and they minimize rotation from one frame to the next.
    /// </summary>
    /// <param name="parameters">A collection of <i>strictly increasing</i> curve parameters to place perpendicular frames on.</param>
    /// <returns>An array of perpendicular frames on success or null on failure.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the curve parameters are not increasing.</exception>
    [ConstOperation]
    public Plane[] GetPerpendicularFrames(IEnumerable<double> parameters)
    {
      RhinoList<double> ts = new RhinoList<double>();
      double t0 = double.MinValue;

      foreach (double t in parameters)
      {
        if (t <= t0)
          throw new InvalidOperationException("Curve parameters must be strictly increasing");

        ts.Add(t);
        t0 = t;
      }
      // looks like we need at least two parameters to have this function make sense
      if (ts.Count < 2)
        return null;

      double[] _parameters = ts.ToArray();
      int count = _parameters.Length;
      Plane[] frames = new Plane[count];

      IntPtr pConstCurve = ConstPointer();
      int rc_count = UnsafeNativeMethods.RHC_RhinoGet1RailFrames(pConstCurve, count, _parameters, frames);
      if (rc_count == count)
        return frames;

      if (rc_count > 0)
      {
        Plane[] rc = new Plane[rc_count];
        Array.Copy(frames, rc, rc_count);
        return rc;
      }
      return null;
    }
#endif

    /// <summary>
    /// Test continuity at a curve parameter value.
    /// </summary>
    /// <param name="continuityType">Type of continuity to test for.</param>
    /// <param name="t">Parameter to test.</param>
    /// <returns>
    /// true if the curve has at least the c type continuity at the parameter t.
    /// </returns>
    [ConstOperation]
    public bool IsContinuous(Continuity continuityType, double t)
    {
      IntPtr ptr = ConstPointer();
      return UnsafeNativeMethods.ON_Curve_IsContinuous(ptr, (int)continuityType, t);
    }
    /// <summary>
    /// Searches for a derivative, tangent, or curvature discontinuity.
    /// </summary>
    /// <param name="continuityType">Type of continuity to search for.</param>
    /// <param name="t0">
    /// Search begins at t0. If there is a discontinuity at t0, it will be ignored. This makes it
    /// possible to repeatedly call GetNextDiscontinuity() and step through the discontinuities.
    /// </param>
    /// <param name="t1">
    /// (t0 != t1)  If there is a discontinuity at t1 it will be ignored unless continuityType is
    /// a locus discontinuity type and t1 is at the start or end of the curve.
    /// </param>
    /// <param name="t">If a discontinuity is found, then t reports the parameter at the discontinuity.</param>
    /// <returns>
    /// Parametric continuity tests c = (C0_continuous, ..., G2_continuous):
    ///  true if a parametric discontinuity was found strictly between t0 and t1. Note well that
    ///  all curves are parametrically continuous at the ends of their domains.
    /// 
    /// Locus continuity tests c = (C0_locus_continuous, ...,G2_locus_continuous):
    ///  true if a locus discontinuity was found strictly between t0 and t1 or at t1 is the at the end
    ///  of a curve. Note well that all open curves (IsClosed()=false) are locus discontinuous at the
    ///  ends of their domains.  All closed curves (IsClosed()=true) are at least C0_locus_continuous at 
    ///  the ends of their domains.
    /// </returns>
    [ConstOperation]
    public bool GetNextDiscontinuity(Continuity continuityType, double t0, double t1, out double t)
    {
      t = RhinoMath.UnsetValue;
      IntPtr ptr = ConstPointer();
      return UnsafeNativeMethods.ON_Curve_GetNextDiscontinuity(ptr, (int)continuityType, t0, t1, ref t);
    }
    #endregion

    #region size related methods
#if RHINO_SDK
    /// <summary>
    /// Gets the length of the curve with a fractional tolerance of 1.0e-8.
    /// </summary>
    /// <returns>The length of the curve on success, or zero on failure.</returns>
    /// <example>
    /// <code source='examples\vbnet\ex_arclengthpoint.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_arclengthpoint.cs' lang='cs'/>
    /// <code source='examples\py\ex_arclengthpoint.py' lang='py'/>
    /// </example>
    [ConstOperation]
    public double GetLength()
    {
      // default tolerance used in OpenNURBS
      return GetLength(1.0e-8);
    }
    /// <summary>Get the length of the curve.</summary>
    /// <param name="fractionalTolerance">
    /// Desired fractional precision. 
    /// fabs(("exact" length from start to t) - arc_length)/arc_length &lt;= fractionalTolerance.
    /// </param>
    /// <returns>The length of the curve on success, or zero on failure.</returns>
    [ConstOperation]
    public double GetLength(double fractionalTolerance)
    {
      double length = 0.0;
      Interval sub_domain = Interval.Unset;
      IntPtr ptr = ConstPointer();
      if (UnsafeNativeMethods.ON_Curve_GetLength(ptr, ref length, fractionalTolerance, sub_domain, true))
        return length;
      return 0;
    }
    /// <summary>Get the length of a sub-section of the curve with a fractional tolerance of 1e-8.</summary>
    /// <param name="subdomain">
    /// The calculation is performed on the specified sub-domain of the curve (must be non-decreasing).
    /// </param>
    /// <returns>The length of the sub-curve on success, or zero on failure.</returns>
    [ConstOperation]
    public double GetLength(Interval subdomain)
    {
      // default tolerance used in OpenNURBS
      return GetLength(1.0e-8, subdomain);
    }
    /// <summary>Get the length of a sub-section of the curve.</summary>
    /// <param name="fractionalTolerance">
    /// Desired fractional precision. 
    /// fabs(("exact" length from start to t) - arc_length)/arc_length &lt;= fractionalTolerance.
    /// </param>
    /// <param name="subdomain">
    /// The calculation is performed on the specified sub-domain of the curve (must be non-decreasing).
    /// </param>
    /// <returns>The length of the sub-curve on success, or zero on failure.</returns>
    [ConstOperation]
    public double GetLength(double fractionalTolerance, Interval subdomain)
    {
      double length = 0.0;
      IntPtr ptr = ConstPointer();
      return UnsafeNativeMethods.ON_Curve_GetLength(ptr, ref length, fractionalTolerance, subdomain, false) ? length : 0;
    }

    /// <summary>Used to quickly find short curves.</summary>
    /// <param name="tolerance">Length threshold value for "shortness".</param>
    /// <returns>true if the length of the curve is &lt;= tolerance.</returns>
    /// <remarks>Faster than calling Length() and testing the result.</remarks>
    /// <example>
    /// <code source='examples\vbnet\ex_dividebylength.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_dividebylength.cs' lang='cs'/>
    /// <code source='examples\py\ex_dividebylength.py' lang='py'/>
    /// </example>
    [ConstOperation]
    public bool IsShort(double tolerance)
    {
      Interval subdomain = Interval.Unset;
      IntPtr ptr = ConstPointer();
      return UnsafeNativeMethods.ON_Curve_IsShort(ptr, tolerance, subdomain, true);
    }
    /// <summary>Used to quickly find short curves.</summary>
    /// <param name="tolerance">Length threshold value for "shortness".</param>
    /// <param name="subdomain">
    /// The test is performed on the interval that is the intersection of subdomain with Domain()
    /// </param>
    /// <returns>true if the length of the curve is &lt;= tolerance.</returns>
    /// <remarks>Faster than calling Length() and testing the result.</remarks>
    [ConstOperation]
    public bool IsShort(double tolerance, Interval subdomain)
    {
      IntPtr ptr = ConstPointer();
      return UnsafeNativeMethods.ON_Curve_IsShort(ptr, tolerance, subdomain, false);
    }

    /// <summary>
    /// Looks for segments that are shorter than tolerance that can be removed. 
    /// Does not change the domain, but it will change the relative parameterization.
    /// </summary>
    /// <param name="tolerance">Tolerance which defines "short" segments.</param>
    /// <returns>
    /// true if removable short segments were found. 
    /// false if no removable short segments were found.
    /// </returns>
    public bool RemoveShortSegments(double tolerance)
    {
      IntPtr ptr = NonConstPointer();
      return UnsafeNativeMethods.ON_Curve_RemoveShortSegments(ptr, tolerance);
    }

    /// <summary>
    /// Gets the parameter along the curve which coincides with a given length along the curve. 
    /// A fractional tolerance of 1e-8 is used in this version of the function.
    /// </summary>
    /// <param name="segmentLength">
    /// Length of segment to measure. Must be less than or equal to the length of the curve.
    /// </param>
    /// <param name="t">
    /// Parameter such that the length of the curve from the curve start point to t equals length.
    /// </param>
    /// <returns>true on success, false on failure.</returns>
    [ConstOperation]
    public bool LengthParameter(double segmentLength, out double t)
    {
      return LengthParameter(segmentLength, out t, 1.0e-8);
    }
    /// <summary>
    /// Gets the parameter along the curve which coincides with a given length along the curve.
    /// </summary>
    /// <param name="segmentLength">
    /// Length of segment to measure. Must be less than or equal to the length of the curve.
    /// </param>
    /// <param name="t">
    /// Parameter such that the length of the curve from the curve start point to t equals s.
    /// </param>
    /// <param name="fractionalTolerance">
    /// Desired fractional precision.
    /// fabs(("exact" length from start to t) - arc_length)/arc_length &lt;= fractionalTolerance.
    /// </param>
    /// <returns>true on success, false on failure.</returns>
    [ConstOperation]
    public bool LengthParameter(double segmentLength, out double t, double fractionalTolerance)
    {
      t = 0.0;

      double length = GetLength(fractionalTolerance);
      if (segmentLength > length) { return false; }
      if (length == 0.0) { return false; }

      segmentLength /= length;

      return NormalizedLengthParameter(segmentLength, out t, fractionalTolerance);
    }
    /// <summary>
    /// Gets the parameter along the curve which coincides with a given length along the curve. 
    /// A fractional tolerance of 1e-8 is used in this version of the function.
    /// </summary>
    /// <param name="segmentLength">
    /// Length of segment to measure. Must be less than or equal to the length of the subdomain.
    /// </param>
    /// <param name="t">
    /// Parameter such that the length of the curve from the start of the subdomain to t is s.
    /// </param>
    /// <param name="subdomain">
    /// The calculation is performed on the specified sub-domain of the curve rather than the whole curve.
    /// </param>
    /// <returns>true on success, false on failure.</returns>
    [ConstOperation]
    public bool LengthParameter(double segmentLength, out double t, Interval subdomain)
    {
      return LengthParameter(segmentLength, out t, 1.0e-8, subdomain);
    }
    /// <summary>
    /// Gets the parameter along the curve which coincides with a given length along the curve.
    /// </summary>
    /// <param name="segmentLength">
    /// Length of segment to measure. Must be less than or equal to the length of the subdomain.
    /// </param>
    /// <param name="t">
    /// Parameter such that the length of the curve from the start of the subdomain to t is s.
    /// </param>
    /// <param name="fractionalTolerance">
    /// Desired fractional precision. 
    /// fabs(("exact" length from start to t) - arc_length)/arc_length &lt;= fractionalTolerance.
    /// </param>
    /// <param name="subdomain">
    /// The calculation is performed on the specified sub-domain of the curve rather than the whole curve.
    /// </param>
    /// <returns>true on success, false on failure.</returns>
    [ConstOperation]
    public bool LengthParameter(double segmentLength, out double t, double fractionalTolerance, Interval subdomain)
    {
      t = 0.0;

      double length = GetLength(fractionalTolerance);
      if (segmentLength > length) { return false; }
      if (length == 0.0) { return false; }

      segmentLength /= length;

      return NormalizedLengthParameter(segmentLength, out t, fractionalTolerance, subdomain);
    }

    /// <summary>
    /// Input the parameter of the point on the curve that is a prescribed arc length from the start of the curve. 
    /// A fractional tolerance of 1e-8 is used in this version of the function.
    /// </summary>
    /// <param name="s">
    /// Normalized arc length parameter. 
    /// E.g., 0 = start of curve, 1/2 = midpoint of curve, 1 = end of curve.
    /// </param>
    /// <param name="t">
    /// Parameter such that the length of the curve from its start to t is arc_length.
    /// </param>
    /// <returns>true on success, false on failure.</returns>
    [ConstOperation]
    public bool NormalizedLengthParameter(double s, out double t)
    {
      return NormalizedLengthParameter(s, out t, 1.0e-8);
    }
    /// <summary>
    /// Input the parameter of the point on the curve that is a prescribed arc length from the start of the curve.
    /// </summary>
    /// <param name="s">
    /// Normalized arc length parameter. 
    /// E.g., 0 = start of curve, 1/2 = midpoint of curve, 1 = end of curve.
    /// </param>
    /// <param name="t">
    /// Parameter such that the length of the curve from its start to t is arc_length.
    /// </param>
    /// <param name="fractionalTolerance">
    /// Desired fractional precision. 
    /// fabs(("exact" length from start to t) - arc_length)/arc_length &lt;= fractionalTolerance.
    /// </param>
    /// <returns>true on success, false on failure.</returns>
    [ConstOperation]
    public bool NormalizedLengthParameter(double s, out double t, double fractionalTolerance)
    {
      t = 0.0;
      Interval subdomain = Interval.Unset;
      IntPtr ptr = ConstPointer();
      bool rc = UnsafeNativeMethods.ON_Curve_GetNormalizedArcLengthPoint(ptr, s, ref t, fractionalTolerance, subdomain, true);
      return rc;
    }
    /// <summary>
    /// Input the parameter of the point on the curve that is a prescribed arc length from the start of the curve. 
    /// A fractional tolerance of 1e-8 is used in this version of the function.
    /// </summary>
    /// <param name="s">
    /// Normalized arc length parameter. 
    /// E.g., 0 = start of curve, 1/2 = midpoint of curve, 1 = end of curve.
    /// </param>
    /// <param name="t">
    /// Parameter such that the length of the curve from its start to t is arc_length.
    /// </param>
    /// <param name="subdomain">
    /// The calculation is performed on the specified sub-domain of the curve.
    /// </param>
    /// <returns>true on success, false on failure.</returns>
    [ConstOperation]
    public bool NormalizedLengthParameter(double s, out double t, Interval subdomain)
    {
      return NormalizedLengthParameter(s, out t, 1.0e-8, subdomain);
    }
    /// <summary>
    /// Input the parameter of the point on the curve that is a prescribed arc length from the start of the curve.
    /// </summary>
    /// <param name="s">
    /// Normalized arc length parameter. 
    /// E.g., 0 = start of curve, 1/2 = midpoint of curve, 1 = end of curve.
    /// </param>
    /// <param name="t">
    /// Parameter such that the length of the curve from its start to t is arc_length.
    /// </param>
    /// <param name="fractionalTolerance">
    /// Desired fractional precision. 
    /// fabs(("exact" length from start to t) - arc_length)/arc_length &lt;= fractionalTolerance.
    /// </param>
    /// <param name="subdomain">
    /// The calculation is performed on the specified sub-domain of the curve.
    /// </param>
    /// <returns>true on success, false on failure.</returns>
    [ConstOperation]
    public bool NormalizedLengthParameter(double s, out double t, double fractionalTolerance, Interval subdomain)
    {
      t = 0.0;
      IntPtr ptr = ConstPointer();
      bool rc = UnsafeNativeMethods.ON_Curve_GetNormalizedArcLengthPoint(ptr, s, ref t, fractionalTolerance, subdomain, false);
      return rc;
    }

    /// <summary>
    /// Input the parameter of the point on the curve that is a prescribed arc length from the start of the curve. 
    /// A fractional tolerance of 1e-8 is used in this version of the function.
    /// </summary>
    /// <param name="s">
    /// Array of normalized arc length parameters. 
    /// E.g., 0 = start of curve, 1/2 = midpoint of curve, 1 = end of curve.
    /// </param>
    /// <param name="absoluteTolerance">
    /// If absoluteTolerance > 0, then the difference between (s[i+1]-s[i])*curve_length 
    /// and the length of the curve segment from t[i] to t[i+1] will be &lt;= absoluteTolerance.
    /// </param>
    /// <returns>
    /// If successful, array of curve parameters such that the length of the curve from its start to t[i] is s[i]*curve_length. 
    /// Null on failure.
    /// </returns>
    [ConstOperation]
    public double[] NormalizedLengthParameters(double[] s, double absoluteTolerance)
    {
      return NormalizedLengthParameters(s, absoluteTolerance, 1.0e-8);
    }
    /// <summary>
    /// Input the parameter of the point on the curve that is a prescribed arc length from the start of the curve.
    /// </summary>
    /// <param name="s">
    /// Array of normalized arc length parameters. 
    /// E.g., 0 = start of curve, 1/2 = midpoint of curve, 1 = end of curve.
    /// </param>
    /// <param name="absoluteTolerance">
    /// If absoluteTolerance > 0, then the difference between (s[i+1]-s[i])*curve_length 
    /// and the length of the curve segment from t[i] to t[i+1] will be &lt;= absoluteTolerance.
    /// </param>
    /// <param name="fractionalTolerance">
    /// Desired fractional precision for each segment. 
    /// fabs("true" length - actual length)/(actual length) &lt;= fractionalTolerance.
    /// </param>
    /// <returns>
    /// If successful, array of curve parameters such that the length of the curve from its start to t[i] is s[i]*curve_length. 
    /// Null on failure.
    /// </returns>
    [ConstOperation]
    public double[] NormalizedLengthParameters(double[] s, double absoluteTolerance, double fractionalTolerance)
    {
      int count = s.Length;
      double[] t = new double[count];
      Interval sub_domain = Interval.Unset;
      IntPtr ptr = ConstPointer();
      if (UnsafeNativeMethods.ON_Curve_GetNormalizedArcLengthPoints(ptr, count, s, t, absoluteTolerance, fractionalTolerance, sub_domain, true))
        return t;
      return null;
    }
    /// <summary>
    /// Input the parameter of the point on the curve that is a prescribed arc length from the start of the curve. 
    /// A fractional tolerance of 1e-8 is used in this version of the function.
    /// </summary>
    /// <param name="s">
    /// Array of normalized arc length parameters. 
    /// E.g., 0 = start of curve, 1/2 = midpoint of curve, 1 = end of curve.
    /// </param>
    /// <param name="absoluteTolerance">
    /// If absoluteTolerance > 0, then the difference between (s[i+1]-s[i])*curve_length 
    /// and the length of the curve segment from t[i] to t[i+1] will be &lt;= absoluteTolerance.
    /// </param>
    /// <param name="subdomain">
    /// The calculation is performed on the specified sub-domain of the curve. 
    /// A 0.0 s value corresponds to subdomain->Min() and a 1.0 s value corresponds to subdomain->Max().
    /// </param>
    /// <returns>
    /// If successful, array of curve parameters such that the length of the curve from its start to t[i] is s[i]*curve_length. 
    /// Null on failure.
    /// </returns>
    [ConstOperation]
    public double[] NormalizedLengthParameters(double[] s, double absoluteTolerance, Interval subdomain)
    {
      return NormalizedLengthParameters(s, absoluteTolerance, 1.0e-8, subdomain);
    }
    /// <summary>
    /// Input the parameter of the point on the curve that is a prescribed arc length from the start of the curve.
    /// </summary>
    /// <param name="s">
    /// Array of normalized arc length parameters. 
    /// E.g., 0 = start of curve, 1/2 = midpoint of curve, 1 = end of curve.
    /// </param>
    /// <param name="absoluteTolerance">
    /// If absoluteTolerance > 0, then the difference between (s[i+1]-s[i])*curve_length 
    /// and the length of the curve segment from t[i] to t[i+1] will be &lt;= absoluteTolerance.
    /// </param>
    /// <param name="fractionalTolerance">
    /// Desired fractional precision for each segment. 
    /// fabs("true" length - actual length)/(actual length) &lt;= fractionalTolerance.
    /// </param>
    /// <param name="subdomain">
    /// The calculation is performed on the specified sub-domain of the curve. 
    /// A 0.0 s value corresponds to subdomain->Min() and a 1.0 s value corresponds to subdomain->Max().
    /// </param>
    /// <returns>
    /// If successful, array of curve parameters such that the length of the curve from its start to t[i] is s[i]*curve_length. 
    /// Null on failure.
    /// </returns>
    [ConstOperation]
    public double[] NormalizedLengthParameters(double[] s, double absoluteTolerance, double fractionalTolerance, Interval subdomain)
    {
      int count = s.Length;
      double[] t = new double[count];
      IntPtr ptr = ConstPointer();
      if (UnsafeNativeMethods.ON_Curve_GetNormalizedArcLengthPoints(ptr, count, s, t, absoluteTolerance, fractionalTolerance, subdomain, false))
        return t;
      return null;
    }

    /// <summary>
    /// Divide the curve into a number of equal-length segments.
    /// </summary>
    /// <param name="segmentCount">Segment count. Note that the number of division points may differ from the segment count.</param>
    /// <param name="includeEnds">If true, then the point at the start of the first division segment is returned.</param>
    /// <returns>
    /// List of curve parameters at the division points on success, null on failure.
    /// </returns>
    [ConstOperation]
    public double[] DivideByCount(int segmentCount, bool includeEnds)
    {
      Point3d[] unused_points;
      return DivideByCount(segmentCount, includeEnds, out unused_points);
    }

    /// <summary>
    /// Divide the curve into a number of equal-length segments.
    /// </summary>
    /// <param name="segmentCount">Segment count. Note that the number of division points may differ from the segment count.</param>
    /// <param name="includeEnds">If true, then the point at the start of the first division segment is returned.</param>
    /// <param name="points">A list of division points. If the function returns successfully, this point-array will be filled in.</param>
    /// <returns>Array containing division curve parameters on success, null on failure.</returns>
    [ConstOperation]
    public double[] DivideByCount(int segmentCount, bool includeEnds, out Point3d[] points)
    {
      points = null;

      if (segmentCount < 1)
        return null;

      IntPtr const_ptr_curve = ConstPointer();

      SimpleArrayPoint3d curve_points = new SimpleArrayPoint3d();
      IntPtr ptr_curve_points = curve_points.NonConstPointer();

      SimpleArrayDouble curve_parameters = new SimpleArrayDouble();
      IntPtr ptr_curve_parameters = curve_parameters.NonConstPointer();

      bool success = UnsafeNativeMethods.RHC_RhinoDivideCurve1(const_ptr_curve, segmentCount, includeEnds, ptr_curve_points, ptr_curve_parameters);

      double[] rc = null;
      if (success)
      {
        points = curve_points.ToArray();
        rc = curve_parameters.ToArray();
      }

      curve_points.Dispose();
      curve_parameters.Dispose();

      return success ? rc : null;
    }

    /// <summary>
    /// Divide the curve into specific length segments.
    /// </summary>
    /// <param name="segmentLength">The length of each and every segment (except potentially the last one).</param>
    /// <param name="includeEnds">If true, then the point at the start of the first division segment is returned.</param>
    /// <returns>Array containing division curve parameters if successful, null on failure.</returns>
    /// <example>
    /// <code source='examples\vbnet\ex_dividebylength.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_dividebylength.cs' lang='cs'/>
    /// <code source='examples\py\ex_dividebylength.py' lang='py'/>
    /// </example>
    [ConstOperation]
    public double[] DivideByLength(double segmentLength, bool includeEnds)
    {
      Point3d[] unused_points;
      return DivideByLength(segmentLength, includeEnds, false, out unused_points);
    }

    /// <summary>
    /// Divide the curve into specific length segments.
    /// </summary>
    /// <param name="segmentLength">The length of each and every segment (except potentially the last one).</param>
    /// <param name="includeEnds">If true, then the point at the start of the first division segment is returned.</param>
    /// <param name="reverse">If true, then the divisions start from the end of the curve.</param>
    /// <returns>Array containing division curve parameters if successful, null on failure.</returns>
    /// <example>
    /// <code source='examples\vbnet\ex_dividebylength.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_dividebylength.cs' lang='cs'/>
    /// <code source='examples\py\ex_dividebylength.py' lang='py'/>
    /// </example>
    [ConstOperation]
    public double[] DivideByLength(double segmentLength, bool includeEnds, bool reverse)
    {
      Point3d[] unused_points;
      return DivideByLength(segmentLength, includeEnds, reverse, out unused_points);
    }

    /// <summary>
    /// Divide the curve into specific length segments.
    /// </summary>
    /// <param name="segmentLength">The length of each and every segment (except potentially the last one).</param>
    /// <param name="includeEnds">If true, then the point at the start of the first division segment is returned.</param>
    /// <param name="points">If function is successful, points at each parameter value are returned in points.</param>
    /// <returns>Array containing division curve parameters if successful, null on failure.</returns>
    /// <example>
    /// <code source='examples\vbnet\ex_dividebylength.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_dividebylength.cs' lang='cs'/>
    /// <code source='examples\py\ex_dividebylength.py' lang='py'/>
    /// </example>
    [ConstOperation]
    public double[] DivideByLength(double segmentLength, bool includeEnds, out Point3d[] points)
    {
      return DivideByLength(segmentLength, includeEnds, false, out points);
    }

    /// <summary>
    /// Divide the curve into specific length segments.
    /// </summary>
    /// <param name="segmentLength">The length of each and every segment (except potentially the last one).</param>
    /// <param name="includeEnds">If true, then the point at the start of the first division segment is returned.</param>
    /// <param name="reverse">If true, then the divisions start from the end of the curve.</param>
    /// <param name="points">If function is successful, points at each parameter value are returned in points.</param>
    /// <returns>Array containing division curve parameters if successful, null on failure.</returns>
    /// <example>
    /// <code source='examples\vbnet\ex_dividebylength.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_dividebylength.cs' lang='cs'/>
    /// <code source='examples\py\ex_dividebylength.py' lang='py'/>
    /// </example>
    [ConstOperation]
    public double[] DivideByLength(double segmentLength, bool includeEnds, bool reverse, out Point3d[] points)
    {
      points = null;

      if (segmentLength < 0.0)
        return null;

      IntPtr const_ptr_curve = ConstPointer();

      SimpleArrayPoint3d curve_points = new SimpleArrayPoint3d();
      IntPtr ptr_curve_points = curve_points.NonConstPointer();

      SimpleArrayDouble curve_parameters = new SimpleArrayDouble();
      IntPtr ptr_curve_parameters = curve_parameters.NonConstPointer();

      bool success = UnsafeNativeMethods.RHC_RhinoDivideCurve2(const_ptr_curve, segmentLength, reverse, includeEnds, ptr_curve_points, ptr_curve_parameters);

      double[] rc = null;
      if (success)
      {
        points = curve_points.ToArray();
        rc = curve_parameters.ToArray();
      }

      curve_points.Dispose();
      curve_parameters.Dispose();

      return success ? rc : null;
    }

    /// <summary>
    /// Calculates 3d points on a curve where the linear distance between the points is equal.
    /// </summary>
    /// <param name="distance">The distance betwen division points.</param>
    /// <returns>An array of equidistant points, or null on error.</returns>
    [ConstOperation]
    public Point3d[] DivideEquidistant(double distance)
    {
      Point3d[] rc = null;
      SimpleArrayPoint3d points = new SimpleArrayPoint3d();
      IntPtr pConstThis = ConstPointer();
      IntPtr pPoints = points.NonConstPointer();
      if (UnsafeNativeMethods.RHC_RhinoDivideCurveEquidistant(pConstThis, distance, pPoints) > 0)
        rc = points.ToArray();
      points.Dispose();
      return rc;
    }

    /// <summary>
    /// Divides this curve at fixed steps along a defined contour line.
    /// </summary>
    /// <param name="contourStart">The start of the contouring line.</param>
    /// <param name="contourEnd">The end of the contouring line.</param>
    /// <param name="interval">A distance to measure on the contouring axis.</param>
    /// <returns>An array of points; or null on error.</returns>
    [ConstOperation]
    public Point3d[] DivideAsContour(Point3d contourStart, Point3d contourEnd, double interval)
    {
      Point3d[] rc = null;
      using (SimpleArrayPoint3d points = new SimpleArrayPoint3d())
      {
        IntPtr pConstThis = ConstPointer();
        IntPtr pPoints = points.NonConstPointer();
        double tolerance = RhinoDoc.ActiveDoc?.ModelAbsoluteTolerance ?? RhinoDoc.DefaultModelAbsoluteTolerance;
        if (UnsafeNativeMethods.RHC_MakeRhinoContours1(pConstThis, contourStart, contourEnd, interval, pPoints, tolerance))
          rc = points.ToArray();
      }
      return rc;
    }
#endif

    //David: Do we really need these two functions? Me thinks they are a bit too geeky.
    /// <summary>
    /// Convert a NURBS curve parameter to a curve parameter.
    /// </summary>
    /// <param name="nurbsParameter">Nurbs form parameter.</param>
    /// <param name="curveParameter">Curve parameter.</param>
    /// <returns>true on success, false on failure.</returns>
    /// <remarks>
    /// If HasNurbForm returns 2, this function converts the curve parameter to the NURBS curve parameter.
    /// </remarks>
    [ConstOperation]
    public bool GetCurveParameterFromNurbsFormParameter(double nurbsParameter, out double curveParameter)
    {
      curveParameter = 0.0;
      IntPtr ptr = ConstPointer();
      return UnsafeNativeMethods.ON_Curve_GetNurbParameter(ptr, nurbsParameter, ref curveParameter, true);
    }
    /// <summary>Convert a curve parameter to a NURBS curve parameter.</summary>
    /// <param name="curveParameter">Curve parameter.</param>
    /// <param name="nurbsParameter">Nurbs form parameter.</param>
    /// <returns>true on success, false on failure.</returns>
    /// <remarks>
    /// If GetNurbForm returns 2, this function converts the curve parameter to the NURBS curve parameter.
    /// </remarks>
    [ConstOperation]
    public bool GetNurbsFormParameterFromCurveParameter(double curveParameter, out double nurbsParameter)
    {
      nurbsParameter = 0.0;
      IntPtr ptr = ConstPointer();
      return UnsafeNativeMethods.ON_Curve_GetNurbParameter(ptr, curveParameter, ref nurbsParameter, false);
    }
    #endregion

    #region shape related methods
    private Curve TrimExtendHelper(double t0, double t1, bool trimming)
    {
      IntPtr ptr = ConstPointer();
      IntPtr rc = UnsafeNativeMethods.ON_Curve_TrimExtend(ptr, t0, t1, trimming);
      return GeometryBase.CreateGeometryHelper(rc, null) as Curve;
    }

    /// <summary>
    /// Removes portions of the curve outside the specified interval.
    /// </summary>
    /// <param name="t0">
    /// Start of the trimming interval. Portions of the curve before curve(t0) are removed.
    /// </param>
    /// <param name="t1">
    /// End of the trimming interval. Portions of the curve after curve(t1) are removed.
    /// </param>
    /// <returns>Trimmed portion of this curve is successfull, null on failure.</returns>
    [ConstOperation]
    public Curve Trim(double t0, double t1)
    {
      return TrimExtendHelper(t0, t1, true);
    }
    /// <summary>
    /// Removes portions of the curve outside the specified interval.
    /// </summary>
    /// <param name="domain">
    /// Trimming interval. Portions of the curve before curve(domain[0])
    /// and after curve(domain[1]) are removed.
    /// </param>
    /// <returns>Trimmed curve if successful, null on failure.</returns>
    [ConstOperation]
    public Curve Trim(Interval domain)
    {
      return Trim(domain.T0, domain.T1);
    }

#if RHINO_SDK
    /// <summary>
    /// Shortens a curve by a given length
    /// </summary>
    /// <param name="side"></param>
    /// <param name="length"></param>
    /// <returns>Trimmed curve if successful, null on failure.</returns>
    [ConstOperation]
    public Curve Trim(CurveEnd side, double length)
    {
      if (length <= 0)
        throw new ArgumentException("length must be > 0", "length");
      double cLength = GetLength();
      if (IsClosed || length >= cLength)
        return null;
      if (side == CurveEnd.Both && length >= 2.0 * cLength)
        return null;

      double t0 = Domain[0];
      double t1 = Domain[1];
      if (side == CurveEnd.Start || side == CurveEnd.Both)
      {
        double s = length / cLength;
        NormalizedLengthParameter(s, out t0);
      }
      if (side == CurveEnd.End || side == CurveEnd.Both)
      {
        double s = (cLength - length) / cLength;
        NormalizedLengthParameter(s, out t1);
      }
      return Trim(t0, t1);
    }
#endif

    /// <summary>
    /// Splits (divides) the curve at the specified parameter. 
    /// The parameter must be in the interior of the curve's domain.
    /// </summary>
    /// <param name="t">
    /// Parameter to split the curve at in the interval returned by Domain().
    /// </param>
    /// <returns>
    /// Two curves on success, null on failure.
    /// </returns>
    [ConstOperation]
    public Curve[] Split(double t)
    {
      IntPtr leftptr = IntPtr.Zero;
      IntPtr rightptr = IntPtr.Zero;
      IntPtr pConstThis = ConstPointer();
      bool rc = UnsafeNativeMethods.ON_Curve_Split(pConstThis, t, ref leftptr, ref rightptr);
      Curve[] output = new Curve[2];
      if (leftptr != IntPtr.Zero)
        output[0] = GeometryBase.CreateGeometryHelper(leftptr, null) as Curve;
      if (rightptr != IntPtr.Zero)
        output[1] = GeometryBase.CreateGeometryHelper(rightptr, null) as Curve;
      return rc ? output : null;
    }

    /// <summary>
    /// Splits (divides) the curve at a series of specified parameters. 
    /// The parameter must be in the interior of the curve domain.
    /// </summary>
    /// <param name="t">
    /// Parameters to split the curve at in the interval returned by Domain().
    /// </param>
    /// <returns>Multiple curves on success, null on failure.</returns>
    [ConstOperation]
    public Curve[] Split(IEnumerable<double> t)
    {
      Interval domain = Domain;
      double t0 = domain.Min;
      double t1 = domain.Max;

      RhinoList<double> parameters = new RhinoList<double>(t);
      parameters.Sort();

      // Remove invalid and duplicate parameters.
      for (int i = parameters.Count - 1; i >= 0; i--)
      {
        if (parameters[i] < t0)
          parameters.RemoveAt(i);
        else if (parameters[i] > t1)
          parameters.RemoveAt(i);
        else if (i > 0)
          if (parameters[i].Equals(parameters[i - 1]))
            parameters.RemoveAt(i);
      }
      if (parameters.Count == 0)
        return new Curve[0];

      // Ensure parameters at the curve tips.
      bool t0Added = false;
      bool t1Added = false;
      if (parameters[0] > t0 + RhinoMath.ZeroTolerance)
      {
        t0Added = true;
        parameters.Insert(0, t0);
      }
      if (parameters[parameters.Count - 1] < t1 - RhinoMath.ZeroTolerance)
      {
        t1Added = true;
        parameters.Add(t1);
      }

      RhinoList<Curve> rc = new RhinoList<Curve>();
      for (int i = 0; i < parameters.Count - 1; i++)
      {
        double start = parameters[i];
        double end = parameters[i + 1];
        if ((end - start) > RhinoMath.ZeroTolerance)
        {
          Curve trimcurve = Trim(start, end);
          if (trimcurve != null)
            rc.Add(trimcurve);
        }
      }

      // If we had to add parameters at both curve extremes, and the curve is closed,
      // then the first and last segments are on either side of the seam and should be 
      // joined.
      if (t0Added && t1Added && IsClosed)
      {
        Curve segment0 = rc[0];
        Curve segment1 = rc[rc.Count - 1];
        PolyCurve join = new PolyCurve();
        join.Append(segment1);
        join.Append(segment0);
        join.RemoveNesting();

        // The joint segment is at the end of the segment list.
        rc.RemoveAt(0);
        rc[rc.Count - 1] = join;
      }

      return rc.Count == 0 ? null : rc.ToArray();
    }

#if RHINO_SDK
    /// <summary>
    /// Splits a curve into pieces using a polysurface.
    /// </summary>
    /// <param name="cutter">A cutting surface or polysurface.</param>
    /// <param name="tolerance">A tolerance for computing intersections.</param>
    /// <returns>An array of curves. This array can be empty.</returns>
    [Obsolete("Use version that takes an angle tolerance")]
    [ConstOperation]
    public Curve[] Split(Brep cutter, double tolerance)
    {
      double tol, angletol;
      RhinoDoc.ActiveDocTolerances(out tol, out angletol);
      return Split(cutter, tolerance, angletol);
    }

    /// <summary>
    /// Splits a curve into pieces using a polysurface.
    /// </summary>
    /// <param name="cutter">A cutting surface or polysurface.</param>
    /// <param name="tolerance">A tolerance for computing intersections.</param>
    /// <param name="angleToleranceRadians"></param>
    /// <returns>An array of curves. This array can be empty.</returns>
    [ConstOperation]
    public Curve[] Split(Brep cutter, double tolerance, double angleToleranceRadians)
    {
      if (cutter == null) throw new ArgumentNullException("cutter");

      IntPtr pConstThis = ConstPointer();
      IntPtr pConstBrep = cutter.ConstPointer();
      using (Rhino.Runtime.InteropWrappers.SimpleArrayCurvePointer pieces = new SimpleArrayCurvePointer())
      {
        IntPtr pPieces = pieces.NonConstPointer();
        UnsafeNativeMethods.RHC_RhinoCurveSplit(pConstThis, pConstBrep, tolerance, angleToleranceRadians, pPieces);
        GC.KeepAlive(cutter);
        return pieces.ToNonConstArray();
      }
    }

    /// <summary>
    /// Splits a curve into pieces using a surface.
    /// </summary>
    /// <param name="cutter">A cutting surface or polysurface.</param>
    /// <param name="tolerance">A tolerance for computing intersections.</param>
    /// <returns>An array of curves. This array can be empty.</returns>
    [Obsolete("Use version that takes an angle tolerance")]
    [ConstOperation]
    public Curve[] Split(Surface cutter, double tolerance)
    {
      double tol, angletol;
      RhinoDoc.ActiveDocTolerances(out tol, out angletol);
      return Split(cutter, tolerance, angletol);
    }

    /// <summary>
    /// Splits a curve into pieces using a surface.
    /// </summary>
    /// <param name="cutter">A cutting surface or polysurface.</param>
    /// <param name="tolerance">A tolerance for computing intersections.</param>
    /// <param name="angleToleranceRadians"></param>
    /// <returns>An array of curves. This array can be empty.</returns>
    [ConstOperation]
    public Curve[] Split(Surface cutter, double tolerance, double angleToleranceRadians)
    {
      IntPtr pConstThis = ConstPointer();
      IntPtr pConstSurface = cutter.ConstPointer();
      using (Rhino.Runtime.InteropWrappers.SimpleArrayCurvePointer pieces = new SimpleArrayCurvePointer())
      {
        IntPtr pPieces = pieces.NonConstPointer();
        UnsafeNativeMethods.RHC_RhinoCurveSplit(pConstThis, pConstSurface, tolerance, angleToleranceRadians, pPieces);
        GC.KeepAlive(cutter);
        return pieces.ToNonConstArray();
      }
    }

    /// <summary>
    /// Where possible, analytically extends curve to include the given domain. 
    /// This will not work on closed curves. The original curve will be identical to the 
    /// restriction of the resulting curve to the original curve domain.
    /// </summary>
    /// <param name="t0">Start of extension domain, if the start is not inside the 
    /// Domain of this curve, an attempt will be made to extend the curve.</param>
    /// <param name="t1">End of extension domain, if the end is not inside the 
    /// Domain of this curve, an attempt will be made to extend the curve.</param>
    /// <returns>Extended curve on success, null on failure.</returns>
    [ConstOperation]
    public Curve Extend(double t0, double t1)
    {
      return TrimExtendHelper(t0, t1, false);
    }
    /// <summary>
    /// Where possible, analytically extends curve to include the given domain. 
    /// This will not work on closed curves. The original curve will be identical to the 
    /// restriction of the resulting curve to the original curve domain.
    /// </summary>
    /// <param name="domain">Extension domain.</param>
    /// <returns>Extended curve on success, null on failure.</returns>
    [ConstOperation]
    public Curve Extend(Interval domain)
    {
      return Extend(domain.T0, domain.T1);
    }

    static UnsafeNativeMethods.ExtendCurveConsts ConvertExtensionStyle(CurveExtensionStyle style)
    {
      if (style == CurveExtensionStyle.Arc)
        return UnsafeNativeMethods.ExtendCurveConsts.ExtendTypeArc;
      if (style == CurveExtensionStyle.Line)
        return UnsafeNativeMethods.ExtendCurveConsts.ExtendTypeLine;

      return UnsafeNativeMethods.ExtendCurveConsts.ExtendTypeSmooth;
    }

    /// <summary>
    /// Extends a curve by a specific length.
    /// </summary>
    /// <param name="side">Curve end to extend.</param>
    /// <param name="length">Length to add to the curve end.</param>
    /// <param name="style">Extension style.</param>
    /// <returns>A curve with extended ends or null on failure.</returns>
    [ConstOperation]
    public Curve Extend(CurveEnd side, double length, CurveExtensionStyle style)
    {
      if (side == CurveEnd.None)
        return DuplicateCurve();

      length = Math.Max(length, 0.0);

      double l0 = length;
      double l1 = length;

      if (side == CurveEnd.End)
        l0 = 0.0;
      if (side == CurveEnd.Start)
        l1 = 0.0;

      IntPtr ptr = ConstPointer();
      IntPtr rc = UnsafeNativeMethods.RHC_RhinoExtendCurve(ptr, l0, l1, ConvertExtensionStyle(style));
      return GeometryBase.CreateGeometryHelper(rc, null) as Curve;
    }

    /// <summary>
    /// Extends a curve until it intersects a collection of objects.
    /// </summary>
    /// <param name="side">The end of the curve to extend.</param>
    /// <param name="style">The style or type of extension to use.</param>
    /// <param name="geometry">A collection of objects. Allowable object types are Curve, Surface, Brep.</param>
    /// <returns>New extended curve result on success, null on failure.</returns>
    /// <example>
    /// <code source='examples\vbnet\ex_extendcurve.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_extendcurve.cs' lang='cs'/>
    /// <code source='examples\py\ex_extendcurve.py' lang='py'/>
    /// </example>
    [ConstOperation]
    public Curve Extend(CurveEnd side, CurveExtensionStyle style, System.Collections.Generic.IEnumerable<GeometryBase> geometry)
    {
      if (CurveEnd.None == side)
        return null;
      int _side = 0;
      if (CurveEnd.End == side)
        _side = 1;
      else if (CurveEnd.Both == side)
        _side = 2;

      IntPtr pConstPtr = ConstPointer();

      using (SimpleArrayGeometryPointer geometryArray = new SimpleArrayGeometryPointer(geometry))
      {
        IntPtr geometryArrayPtr = geometryArray.ConstPointer();

        IntPtr rc = UnsafeNativeMethods.RHC_RhinoExtendCurve1(pConstPtr, ConvertExtensionStyle(style), _side, geometryArrayPtr);
        GC.KeepAlive(geometry);
        return GeometryBase.CreateGeometryHelper(rc, null) as Curve;
      }
    }

    /// <summary>
    /// Extends a curve to a point.
    /// </summary>
    /// <param name="side">The end of the curve to extend.</param>
    /// <param name="style">The style or type of extension to use.</param>
    /// <param name="endPoint">A new end point.</param>
    /// <returns>New extended curve result on success, null on failure.</returns>
    [ConstOperation]
    public Curve Extend(CurveEnd side, CurveExtensionStyle style, Point3d endPoint)
    {
      if (CurveEnd.None == side)
        return null;
      int _side = 0;
      if (CurveEnd.End == side)
        _side = 1;
      else if (CurveEnd.Both == side)
        _side = 2;

      IntPtr pConstPtr = ConstPointer();

      IntPtr rc = UnsafeNativeMethods.RHC_RhinoExtendCurve2(pConstPtr, ConvertExtensionStyle(style), _side, endPoint);
      return GeometryBase.CreateGeometryHelper(rc, null) as Curve;
    }

    /// <summary>
    /// Extends a curve on a surface.
    /// </summary>
    /// <param name="side">The end of the curve to extend.</param>
    /// <param name="surface">Surface that contains the curve.</param>
    /// <returns>New extended curve result on success, null on failure.</returns>
    [ConstOperation]
    public Curve ExtendOnSurface(CurveEnd side, Surface surface)
    {
      if (surface == null) { throw new ArgumentNullException("surface"); }
      Brep brep = surface.ToBrep();
      if (brep == null) { return null; }

      return ExtendOnSurface(side, brep.Faces[0]);
    }
    /// <summary>
    /// Extends a curve on a surface.
    /// </summary>
    /// <param name="side">The end of the curve to extend.</param>
    /// <param name="face">BrepFace that contains the curve.</param>
    /// <returns>New extended curve result on success, null on failure.</returns>
    [ConstOperation]
    public Curve ExtendOnSurface(CurveEnd side, BrepFace face)
    {
      if (face == null) { throw new ArgumentNullException("face"); }

      if (CurveEnd.None == side)
        return null;
      int _side = 0;
      if (CurveEnd.End == side)
        _side = 1;
      else if (CurveEnd.Both == side)
        _side = 2;

      IntPtr pConstCurve = ConstPointer();
      IntPtr pConstFace = face.ConstPointer();

      IntPtr rc = UnsafeNativeMethods.RHC_RhinoExtendCrvOnSrf(pConstCurve, pConstFace, _side);
      GC.KeepAlive(face);
      return GeometryBase.CreateGeometryHelper(rc, null) as Curve;
    }

    /// <summary>
    /// Extends a curve by a line until it intersects a collection of objects.
    /// </summary>
    /// <param name="side">The end of the curve to extend.</param>
    /// <param name="geometry">A collection of objects. Allowable object types are Curve, Surface, Brep.</param>
    /// <returns>New extended curve result on success, null on failure.</returns>
    [ConstOperation]
    public Curve ExtendByLine(CurveEnd side, System.Collections.Generic.IEnumerable<GeometryBase> geometry)
    {
      return Extend(side, CurveExtensionStyle.Line, geometry);
    }
    /// <summary>
    /// Extends a curve by an Arc until it intersects a collection of objects.
    /// </summary>
    /// <param name="side">The end of the curve to extend.</param>
    /// <param name="geometry">A collection of objects. Allowable object types are Curve, Surface, Brep.</param>
    /// <returns>New extended curve result on success, null on failure.</returns>
    [ConstOperation]
    public Curve ExtendByArc(CurveEnd side, System.Collections.Generic.IEnumerable<GeometryBase> geometry)
    {
      return Extend(side, CurveExtensionStyle.Arc, geometry);
    }

    static int SimplifyOptionsToInt(CurveSimplifyOptions options)
    {
      int none = 63;
      if ((options & CurveSimplifyOptions.SplitAtFullyMultipleKnots) == CurveSimplifyOptions.SplitAtFullyMultipleKnots)
      {
        // remove DontSplitFMK flag
        none -= (1 << 0);
      }
      if ((options & CurveSimplifyOptions.RebuildLines) == CurveSimplifyOptions.RebuildLines)
      {
        //remove DontRebuildLines flag
        none -= (1 << 1);
      }
      if ((options & CurveSimplifyOptions.RebuildArcs) == CurveSimplifyOptions.RebuildArcs)
      {
        //remove DontRebuildArcs flag
        none -= (1 << 2);
      }
      if ((options & CurveSimplifyOptions.RebuildRationals) == CurveSimplifyOptions.RebuildRationals)
      {
        // remove DontRebuildRationals flag
        none -= (1 << 3);
      }
      if ((options & CurveSimplifyOptions.AdjustG1) == CurveSimplifyOptions.AdjustG1)
      {
        // remove DontAdjustG1 flag
        none -= (1 << 4);
      }
      if ((options & CurveSimplifyOptions.Merge) == CurveSimplifyOptions.Merge)
      {
        // remove DontMerge flag
        none -= (1 << 5);
      }
      return none;
    }
    /// <summary>
    /// Returns a geometrically equivalent PolyCurve.
    /// <para>The PolyCurve has the following properties</para>
    /// <para>
    ///	1. All the PolyCurve segments are LineCurve, PolylineCurve, ArcCurve, or NurbsCurve.
    /// </para>
    /// <para>
    ///	2. The Nurbs Curves segments do not have fully multiple interior knots.
    /// </para>
    /// <para>
    ///	3. Rational Nurbs curves do not have constant weights.
    /// </para>
    /// <para>
    ///	4. Any segment for which IsLinear() or IsArc() is true is a Line, 
    ///    Polyline segment, or an Arc.
    /// </para>
    /// <para>
    ///	5. Adjacent Colinear or Cocircular segments are combined.
    /// </para>
    /// <para>
    ///	6. Segments that meet with G1-continuity have there ends tuned up so
    ///    that they meet with G1-continuity to within machine precision.
    /// </para>
    /// </summary>
    /// <param name="options">Simplification options.</param>
    /// <param name="distanceTolerance">A distance tolerance for the simplification.</param>
    /// <param name="angleToleranceRadians">An angle tolerance for the simplification.</param>
    /// <returns>New simplified curve on success, null on failure.</returns>
    [ConstOperation]
    public Curve Simplify(CurveSimplifyOptions options, double distanceTolerance, double angleToleranceRadians)
    {
      IntPtr pConstPtr = ConstPointer();
      int _options = SimplifyOptionsToInt(options);
      IntPtr rc = UnsafeNativeMethods.RHC_RhinoSimplifyCurve(pConstPtr, _options, distanceTolerance, angleToleranceRadians);
      return GeometryBase.CreateGeometryHelper(rc, null) as Curve;
    }

    /// <summary>
    /// Same as SimplifyCurve, but simplifies only the last two segments at "side" end.
    /// </summary>
    /// <param name="end">If CurveEnd.Start the function simplifies the last two start 
    /// side segments, otherwise if CurveEnd.End the last two end side segments are simplified.
    /// </param>
    /// <param name="options">Simplification options.</param>
    /// <param name="distanceTolerance">A distance tolerance for the simplification.</param>
    /// <param name="angleToleranceRadians">An angle tolerance for the simplification.</param>
    /// <returns>New simplified curve on success, null on failure.</returns>
    [ConstOperation]
    public Curve SimplifyEnd(CurveEnd end, CurveSimplifyOptions options, double distanceTolerance, double angleToleranceRadians)
    {
      // CurveEnd must be Start or End
      if (end != CurveEnd.Start && end != CurveEnd.End)
        return null;

      int side = 0;//Start
      if (CurveEnd.End == end)
        side = 1; //end
      int _options = SimplifyOptionsToInt(options);
      IntPtr pConstPtr = ConstPointer();
      IntPtr rc = UnsafeNativeMethods.RHC_RhinoSimplifyCurveEnd(pConstPtr, side, _options, distanceTolerance, angleToleranceRadians);
      return GeometryBase.CreateGeometryHelper(rc, null) as Curve;
    }

    /// <summary>
    /// Fairs a curve object. Fair works best on degree 3 (cubic) curves. Attempts to 
    /// remove large curvature variations while limiting the geometry changes to be no 
    /// more than the specified tolerance. 
    /// </summary>
    /// <param name="distanceTolerance">Maximum allowed distance the faired curve is allowed to deviate from the input.</param>
    /// <param name="angleTolerance">(in radians) kinks with angles &lt;= angleTolerance are smoothed out 0.05 is a good default.</param>
    /// <param name="clampStart">The number of (control vertices-1) to preserve at start. 
    /// <para>0 = preserve start point</para>
    /// <para>1 = preserve start point and 1st derivative</para>
    /// <para>2 = preserve start point, 1st and 2nd derivative</para>
    /// </param>
    /// <param name="clampEnd">Same as clampStart.</param>
    /// <param name="iterations">The number of iteratoins to use in adjusting the curve.</param>
    /// <returns>Returns new faired Curve on success, null on failure.</returns>
    [ConstOperation]
    public Curve Fair(double distanceTolerance, double angleTolerance, int clampStart, int clampEnd, int iterations)
    {
      IntPtr ptr = ConstPointer();
      IntPtr rc = UnsafeNativeMethods.RHC_RhinoFairCurve(ptr, distanceTolerance, angleTolerance, clampStart, clampEnd, iterations);
      return GeometryBase.CreateGeometryHelper(rc, null) as Curve;
    }

    /// <summary>
    /// Fits a new curve through an existing curve.
    /// </summary>
    /// <param name="degree">The degree of the returned Curve. Must be bigger than 1.</param>
    /// <param name="fitTolerance">The fitting tolerance. If fitTolerance is RhinoMath.UnsetValue or &lt;=0.0,
    /// the document absolute tolerance is used.</param>
    /// <param name="angleTolerance">The kink smoothing tolerance in radians.
    /// <para>If angleTolerance is 0.0, all kinks are smoothed</para>
    /// <para>If angleTolerance is &gt;0.0, kinks smaller than angleTolerance are smoothed</para>  
    /// <para>If angleTolerance is RhinoMath.UnsetValue or &lt;0.0, the document angle tolerance is used for the kink smoothing</para>
    /// </param>
    /// <returns>Returns a new fitted Curve if successful, null on failure.</returns>
    [ConstOperation]
    public Curve Fit(int degree, double fitTolerance, double angleTolerance)
    {
      IntPtr ptr = ConstPointer();
      IntPtr rc = UnsafeNativeMethods.RHC_RhinoFitCurve(ptr, degree, fitTolerance, angleTolerance);
      return GeometryBase.CreateGeometryHelper(rc, null) as Curve;
    }

    /// <summary>
    /// Rebuild a curve with a specific point count.
    /// </summary>
    /// <param name="pointCount">Number of control points in the rebuild curve.</param>
    /// <param name="degree">Degree of curve. Valid values are between and including 1 and 11.</param>
    /// <param name="preserveTangents">If true, the end tangents of the input curve will be preserved.</param>
    /// <returns>A Nurbs curve on success or null on failure.</returns>
    [ConstOperation]
    public NurbsCurve Rebuild(int pointCount, int degree, bool preserveTangents)
    {
      pointCount = Math.Max(pointCount, 2);
      degree = Math.Max(degree, 1);
      degree = Math.Min(degree, 11);

      IntPtr ptr = ConstPointer();
      IntPtr rc = UnsafeNativeMethods.RHC_RhinoRebuildCurve(ptr, pointCount, degree, preserveTangents);
      return GeometryBase.CreateGeometryHelper(rc, null) as NurbsCurve;
    }
#endif
    #endregion

    //David: we should use an Enum here. This function should also be a Property I think.
    /// <summary>
    /// Does a NURBS curve representation of this curve exist?
    /// </summary>
    /// <returns>
    /// 0   unable to create NURBS representation with desired accuracy.
    /// 1   success - NURBS parameterization matches the curve's to the desired accuracy
    /// 2   success - NURBS point locus matches the curve's and the domain of the NURBS
    ///               curve is correct. However, This curve's parameterization and the
    ///               NURBS curve parameterization may not match. This situation happens
    ///               when getting NURBS representations of curves that have a
    ///               transendental parameterization like circles.
    /// </returns>
    [ConstOperation]
    public int HasNurbsForm()
    {
      IntPtr ptr = ConstPointer();
      return UnsafeNativeMethods.ON_Curve_HasNurbForm(ptr);
    }

    /// <summary>
    /// Constructs a NURBS curve representation of this curve.
    /// </summary>
    /// <returns>NURBS representation of the curve on success, null on failure.</returns>
    [ConstOperation]
    public NurbsCurve ToNurbsCurve()
    {
      const double tolerance = 0.0;
      Interval sub_domain = Interval.Unset;
      IntPtr ptr = ConstPointer();
      IntPtr rc = UnsafeNativeMethods.ON_Curve_NurbsCurve(ptr, tolerance, sub_domain, true);
      return GeometryBase.CreateGeometryHelper(rc, null) as NurbsCurve;
    }
    /// <summary>
    /// Constructs a NURBS curve representation of this curve.
    /// </summary>
    /// <param name="subdomain">The NURBS representation for this portion of the curve is returned.</param>
    /// <returns>NURBS representation of the curve on success, null on failure.</returns>
    [ConstOperation]
    public NurbsCurve ToNurbsCurve(Interval subdomain)
    {
      const double tolerance = 0.0;
      IntPtr ptr = ConstPointer();
      IntPtr rc = UnsafeNativeMethods.ON_Curve_NurbsCurve(ptr, tolerance, subdomain, false);
      return GeometryBase.CreateGeometryHelper(rc, null) as NurbsCurve;
    }

    /// <summary>
    /// Get the domain of the curve span with the given index. 
    /// Use the SpanCount property to test how many spans there are.
    /// </summary>
    /// <param name="spanIndex">Index of span.</param>
    /// <returns>Interval of the span with the given index.</returns>
    [ConstOperation]
    public Interval SpanDomain(int spanIndex)
    {
      if (spanIndex < 0) { throw new IndexOutOfRangeException("spanIndex must be larger than or equal to zero"); }
      if (spanIndex >= SpanCount) { throw new IndexOutOfRangeException("spanIndex must be smaller than spanCount"); }

      Interval domain = Interval.Unset;
      if (UnsafeNativeMethods.ON_Curve_SpanInterval(ConstPointer(), spanIndex, ref domain))
        return domain;

      return Interval.Unset;
    }

#if RHINO_SDK
    /// <summary>
    /// Gets a polyline approximation of a curve.
    /// </summary>
    /// <param name="mainSegmentCount">
    /// If mainSegmentCount &lt;= 0, then both subSegmentCount and mainSegmentCount are ignored. 
    /// If mainSegmentCount &gt; 0, then subSegmentCount must be &gt;= 1. In this 
    /// case the nurb will be broken into mainSegmentCount equally spaced 
    /// chords. If needed, each of these chords can be split into as many 
    /// subSegmentCount sub-parts if the subdivision is necessary for the 
    /// mesh to meet the other meshing constraints. In particular, if 
    /// subSegmentCount = 0, then the curve is broken into mainSegmentCount 
    /// pieces and no further testing is performed.</param>
    /// <param name="subSegmentCount">An amount of subsegments.</param>
    /// <param name="maxAngleRadians">
    /// ( 0 to pi ) Maximum angle (in radians) between unit tangents at 
    /// adjacent vertices.</param>
    /// <param name="maxChordLengthRatio">Maximum permitted value of 
    /// (distance chord midpoint to curve) / (length of chord).</param>
    /// <param name="maxAspectRatio">If maxAspectRatio &lt; 1.0, the parameter is ignored. 
    /// If 1 &lt;= maxAspectRatio &lt; sqrt(2), it is treated as if maxAspectRatio = sqrt(2). 
    /// This parameter controls the maximum permitted value of 
    /// (length of longest chord) / (length of shortest chord).</param>
    /// <param name="tolerance">If tolerance = 0, the parameter is ignored. 
    /// This parameter controls the maximum permitted value of the 
    /// distance from the curve to the polyline.</param>
    /// <param name="minEdgeLength">The minimum permitted edge length.</param>
    /// <param name="maxEdgeLength">If maxEdgeLength = 0, the parameter 
    /// is ignored. This parameter controls the maximum permitted edge length.
    /// </param>
    /// <param name="keepStartPoint">If true the starting point of the curve 
    /// is added to the polyline. If false the starting point of the curve is 
    /// not added to the polyline.</param>
    /// <returns>PolylineCurve on success, null on error.</returns>
    [ConstOperation]
    public PolylineCurve ToPolyline(int mainSegmentCount, int subSegmentCount,
                                    double maxAngleRadians, double maxChordLengthRatio, double maxAspectRatio,
                                    double tolerance, double minEdgeLength, double maxEdgeLength, bool keepStartPoint)
    {
      IntPtr ptr = ConstPointer();
      PolylineCurve poly = new PolylineCurve();
      IntPtr polyOut = poly.NonConstPointer();
      Interval curve_domain = Interval.Unset;

      bool rc = UnsafeNativeMethods.RHC_RhinoConvertCurveToPolyline(ptr,
        mainSegmentCount, subSegmentCount, maxAngleRadians,
        maxChordLengthRatio, maxAspectRatio, tolerance, minEdgeLength, maxEdgeLength, polyOut,
        keepStartPoint, curve_domain, true);

      if (!rc)
      {
        poly.Dispose();
        poly = null;
      }

      return poly;
    }
    /// <summary>
    /// Gets a polyline approximation of a curve.
    /// </summary>
    /// <param name="mainSegmentCount">
    /// If mainSegmentCount &lt;= 0, then both subSegmentCount and mainSegmentCount are ignored. 
    /// If mainSegmentCount &gt; 0, then subSegmentCount must be &gt;= 1. In this 
    /// case the nurb will be broken into mainSegmentCount equally spaced 
    /// chords. If needed, each of these chords can be split into as many 
    /// subSegmentCount sub-parts if the subdivision is necessary for the 
    /// mesh to meet the other meshing constraints. In particular, if 
    /// subSegmentCount = 0, then the curve is broken into mainSegmentCount 
    /// pieces and no further testing is performed.</param>
    /// <param name="subSegmentCount">An amount of subsegments.</param>
    /// <param name="maxAngleRadians">
    /// ( 0 to pi ) Maximum angle (in radians) between unit tangents at 
    /// adjacent vertices.</param>
    /// <param name="maxChordLengthRatio">Maximum permitted value of 
    /// (distance chord midpoint to curve) / (length of chord).</param>
    /// <param name="maxAspectRatio">If maxAspectRatio &lt; 1.0, the parameter is ignored. 
    /// If 1 &lt;= maxAspectRatio &lt; sqrt(2), it is treated as if maxAspectRatio = sqrt(2). 
    /// This parameter controls the maximum permitted value of 
    /// (length of longest chord) / (length of shortest chord).</param>
    /// <param name="tolerance">If tolerance = 0, the parameter is ignored. 
    /// This parameter controls the maximum permitted value of the 
    /// distance from the curve to the polyline.</param>
    /// <param name="minEdgeLength">The minimum permitted edge length.</param>
    /// <param name="maxEdgeLength">If maxEdgeLength = 0, the parameter 
    /// is ignored. This parameter controls the maximum permitted edge length.
    /// </param>
    /// <param name="keepStartPoint">If true the starting point of the curve 
    /// is added to the polyline. If false the starting point of the curve is 
    /// not added to the polyline.</param>
    /// <param name="curveDomain">This subdomain of the NURBS curve is approximated.</param>
    /// <returns>PolylineCurve on success, null on error.</returns>
    [ConstOperation]
    public PolylineCurve ToPolyline(int mainSegmentCount, int subSegmentCount,
                                    double maxAngleRadians, double maxChordLengthRatio, double maxAspectRatio,
                                    double tolerance, double minEdgeLength, double maxEdgeLength, bool keepStartPoint,
                                    Interval curveDomain)
    {
      IntPtr ptr = ConstPointer();
      PolylineCurve poly = new PolylineCurve();
      IntPtr polyOut = poly.NonConstPointer();

      bool rc = UnsafeNativeMethods.RHC_RhinoConvertCurveToPolyline(ptr,
        mainSegmentCount, subSegmentCount, maxAngleRadians,
        maxChordLengthRatio, maxAspectRatio, tolerance, minEdgeLength, maxEdgeLength, polyOut,
        keepStartPoint, curveDomain, false);

      if (!rc)
      {
        poly.Dispose();
        poly = null;
      }

      return poly;
    }

    /// <summary>
    /// Gets a polyline approximation of a curve.
    /// </summary>
    /// <param name="tolerance">The tolerance. This is the maximum deviation from line midpoints to the curve. When in doubt, use the document's model space absolute tolerance.</param>
    /// <param name="angleTolerance">The angle tolerance in radians. This is the maximum deviation of the line directions. When in doubt, use the document's model space angle tolerance.</param>
    /// <param name="minimumLength">The minimum segment length.</param>
    /// <param name="maximumLength">The maximum segment length.</param>
    /// <returns>PolyCurve on success, null on error.</returns>
    [ConstOperation]
    public PolylineCurve ToPolyline(double tolerance, double angleTolerance, double minimumLength, double maximumLength)
    {
      IntPtr ptr_const_this = ConstPointer();
      IntPtr ptr = UnsafeNativeMethods.RHC_RhinoConvertCurveToLines(ptr_const_this, tolerance, angleTolerance, minimumLength, maximumLength);
      return GeometryBase.CreateGeometryHelper(ptr, null) as PolylineCurve;
    }

    /// <summary>
    /// Converts a curve into polycurve consisting of arc segments. Sections of the input curves that are nearly straight are converted to straight-line segments.
    /// </summary>
    /// <param name="tolerance">The tolerance. This is the maximum deviation from arc midpoints to the curve. When in doubt, use the document's model space absolute tolerance.</param>
    /// <param name="angleTolerance">The angle tolerance in radians. This is the maximum deviation of the arc end directions from the curve direction. When in doubt, use the document's model space angle tolerance.</param>
    /// <param name="minimumLength">The minimum segment length.</param>
    /// <param name="maximumLength">The maximum segment length.</param>
    /// <returns>PolyCurve on success, null on error.</returns>
    [ConstOperation]
    public PolyCurve ToArcsAndLines(double tolerance, double angleTolerance, double minimumLength, double maximumLength)
    {
      IntPtr ptr_const_this = ConstPointer();
      IntPtr ptr = UnsafeNativeMethods.RHC_RhinoConvertCurveToArcs(ptr_const_this, tolerance, angleTolerance, minimumLength, maximumLength);
      return GeometryBase.CreateGeometryHelper(ptr, null) as PolyCurve;
    }

    /// <summary>
    /// Makes a polyline approximation of the curve and gets the closest point on the mesh for each point on the curve. 
    /// Then it "connects the points" so that you have a polyline on the mesh.
    /// </summary>
    /// <param name="mesh">Mesh to project onto.</param>
    /// <param name="tolerance">Input tolerance (RhinoDoc.ModelAbsoluteTolerance is a good default)</param>
    /// <returns>A polyline curve on success, null on failure.</returns>
    [ConstOperation]
    public PolylineCurve PullToMesh(Mesh mesh, double tolerance)
    {
      IntPtr pConstCurve = ConstPointer();
      IntPtr pConstMesh = mesh.ConstPointer();
      IntPtr pPolylineCurve = UnsafeNativeMethods.RHC_RhinoPullCurveToMesh(pConstCurve, pConstMesh, tolerance);
      GC.KeepAlive(mesh);
      return GeometryBase.CreateGeometryHelper(pPolylineCurve, null) as PolylineCurve;
    }

    /// <summary>
    /// Offsets this curve. If you have a nice offset, then there will be one entry in 
    /// the array. If the original curve had kinks or the offset curve had self 
    /// intersections, you will get multiple segments in the offset_curves[] array.
    /// </summary>
    /// <param name="plane">Offset solution plane.</param>
    /// <param name="distance">The positive or negative distance to offset.</param>
    /// <param name="tolerance">The offset or fitting tolerance.</param>
    /// <param name="cornerStyle">Corner style for offset kinks.</param>
    /// <returns>Offset curves on success, null on failure.</returns>
    [ConstOperation]
    public Curve[] Offset(Plane plane, double distance, double tolerance, CurveOffsetCornerStyle cornerStyle)
    {
      // Create a bogus dir point
      Point3d direction_point = plane.Origin + plane.XAxis;

      Random rnd = new Random(1);

      // Try a hundred times to find a smooth place on the curve where the tangent 
      // is not (anti)parallel to the offset plane z-axis.
      // This is unbelievably foul, but the most consistent offset result I 
      // can come up with on short notice.
      for (int i = 0; i < 100; i++)
      {
        double t = Domain.ParameterAt(rnd.NextDouble());
        if (IsContinuous(Continuity.G1_continuous, t))
        {
          Point3d p = PointAt(t);
          Vector3d v = TangentAt(t);

          if (v.IsParallelTo(plane.ZAxis, RhinoMath.ToRadians(0.1)) == 0)
          {
            Vector3d perp = Vector3d.CrossProduct(v, plane.ZAxis);
            perp.Unitize();
            direction_point = p + 1e-3 * perp;
            break;
          }
        }
      }

      return Offset(direction_point, plane.Normal, distance, tolerance, cornerStyle);

      //IntPtr ptr = ConstPointer();
      //SimpleArrayCurvePointer offsetCurves = new SimpleArrayCurvePointer();
      //IntPtr pCurveArray = offsetCurves.NonConstPointer();
      //bool rc = UnsafeNativeMethods.RHC_RhinoOffsetCurve(ptr, plane.ZAxis, plane.Origin, distance, pCurveArray, tolerance, 0.015, (int)cornerStyle);
      //Curve[] curves = offsetCurves.ToNonConstArray();
      //offsetCurves.Dispose();
      //if (!rc)
      //  return null;
      //return curves;
    }

    /// <summary>
    /// Offsets this curve. If you have a nice offset, then there will be one entry in 
    /// the array. If the original curve had kinks or the offset curve had self 
    /// intersections, you will get multiple segments in the offset_curves[] array.
    /// </summary>
    /// <param name="directionPoint">A point that indicates the direction of the offset.</param>
    /// <param name="normal">The normal to the offset plane.</param>
    /// <param name="distance">The positive or negative distance to offset.</param>
    /// <param name="tolerance">The offset or fitting tolerance.</param>
    /// <param name="cornerStyle">Corner style for offset kinks.</param>
    /// <returns>Offset curves on success, null on failure.</returns>
    [ConstOperation]
    public Curve[] Offset(Point3d directionPoint, Vector3d normal, double distance, double tolerance, CurveOffsetCornerStyle cornerStyle)
    {
      IntPtr ptr = ConstPointer();
      SimpleArrayCurvePointer offsetCurves = new SimpleArrayCurvePointer();
      IntPtr pCurveArray = offsetCurves.NonConstPointer();
      bool rc = UnsafeNativeMethods.RHC_RhinoOffsetCurve2(ptr, distance, directionPoint, normal, (int)cornerStyle, tolerance, pCurveArray);
      Curve[] curves = offsetCurves.ToNonConstArray();
      offsetCurves.Dispose();
      if (!rc)
        return null;
      return curves;
    }

    /// <summary>
    /// Offset this curve on a brep face surface. This curve must lie on the surface.
    /// </summary>
    /// <param name="face">The brep face on which to offset.</param>
    /// <param name="distance">A distance to offset (+)left, (-)right.</param>
    /// <param name="fittingTolerance">A fitting tolerance.</param>
    /// <returns>Offset curves on success, or null on failure.</returns>
    /// <exception cref="ArgumentNullException">If face is null.</exception>
    [ConstOperation]
    public Curve[] OffsetOnSurface(BrepFace face, double distance, double fittingTolerance)
    {
      if (face == null)
        throw new ArgumentNullException("face");

      int fid = face.FaceIndex;
      IntPtr pConstBrep = face.m_brep.ConstPointer();
      SimpleArrayCurvePointer offsetCurves = new SimpleArrayCurvePointer();
      IntPtr pCurveArray = offsetCurves.NonConstPointer();
      IntPtr pConstCurve = ConstPointer();
      int count = UnsafeNativeMethods.RHC_RhinoOffsetCurveOnSrf(pConstCurve, pConstBrep, fid, distance, fittingTolerance, pCurveArray);
      Curve[] curves = offsetCurves.ToNonConstArray();
      offsetCurves.Dispose();
      if (count < 1)
        return null;
      GC.KeepAlive(face);
      return curves;
    }
    /// <summary>
    /// Offset a curve on a brep face surface. This curve must lie on the surface.
    /// <para>This overload allows to specify a surface point at which the offset will pass.</para>
    /// </summary>
    /// <param name="face">The brep face on which to offset.</param>
    /// <param name="throughPoint">2d point on the brep face to offset through.</param>
    /// <param name="fittingTolerance">A fitting tolerance.</param>
    /// <returns>Offset curves on success, or null on failure.</returns>
    /// <exception cref="ArgumentNullException">If face is null.</exception>
    [ConstOperation]
    public Curve[] OffsetOnSurface(BrepFace face, Point2d throughPoint, double fittingTolerance)
    {
      if (face == null)
        throw new ArgumentNullException("face");

      int fid = face.FaceIndex;
      IntPtr pConstBrep = face.m_brep.ConstPointer();
      SimpleArrayCurvePointer offsetCurves = new SimpleArrayCurvePointer();
      IntPtr pCurveArray = offsetCurves.NonConstPointer();
      IntPtr pConstCurve = ConstPointer();
      int count = UnsafeNativeMethods.RHC_RhinoOffsetCurveOnSrf2(pConstCurve, pConstBrep, fid, throughPoint, fittingTolerance, pCurveArray);
      Curve[] curves = offsetCurves.ToNonConstArray();
      offsetCurves.Dispose();
      if (count < 1)
        return null;
      GC.KeepAlive(face);
      return curves;
    }
    /// <summary>
    /// Offset a curve on a brep face surface. This curve must lie on the surface.
    /// <para>This overload allows to specify different offsets for different curve parameters.</para>
    /// </summary>
    /// <param name="face">The brep face on which to offset.</param>
    /// <param name="curveParameters">Curve parameters corresponding to the offset distances.</param>
    /// <param name="offsetDistances">distances to offset (+)left, (-)right.</param>
    /// <param name="fittingTolerance">A fitting tolerance.</param>
    /// <returns>Offset curves on success, or null on failure.</returns>
    /// <exception cref="ArgumentNullException">If face, curveParameters or offsetDistances are null.</exception>
    [ConstOperation]
    public Curve[] OffsetOnSurface(BrepFace face, double[] curveParameters, double[] offsetDistances, double fittingTolerance)
    {
      if (face == null) throw new ArgumentNullException("face");
      if (curveParameters == null) throw new ArgumentNullException("curveParameters");
      if (offsetDistances == null) throw new ArgumentNullException("offsetDistances");

      int array_count = curveParameters.Length;
      if (offsetDistances.Length != array_count)
        throw new ArgumentException("curveParameters and offsetDistances must be the same length");

      int fid = face.FaceIndex;
      IntPtr pConstBrep = face.m_brep.ConstPointer();
      SimpleArrayCurvePointer offsetCurves = new SimpleArrayCurvePointer();
      IntPtr pCurveArray = offsetCurves.NonConstPointer();
      IntPtr pConstCurve = ConstPointer();
      int count = UnsafeNativeMethods.RHC_RhinoOffsetCurveOnSrf3(pConstCurve, pConstBrep, fid, array_count, curveParameters, offsetDistances, fittingTolerance, pCurveArray);
      Curve[] curves = offsetCurves.ToNonConstArray();
      offsetCurves.Dispose();
      if (count < 1)
        return null;
      GC.KeepAlive(face);
      return curves;
    }
    /// <summary>
    /// Offset a curve on a surface. This curve must lie on the surface.
    /// </summary>
    /// <param name="surface">A surface on which to offset.</param>
    /// <param name="distance">A distance to offset (+)left, (-)right.</param>
    /// <param name="fittingTolerance">A fitting tolerance.</param>
    /// <returns>Offset curves on success, or null on failure.</returns>
    /// <exception cref="ArgumentNullException">If surface is null.</exception>
    [ConstOperation]
    public Curve[] OffsetOnSurface(Surface surface, double distance, double fittingTolerance)
    {
      if (surface == null)
        throw new ArgumentNullException("surface");

      Brep b = Brep.CreateFromSurface(surface);
      return OffsetOnSurface(b.Faces[0], distance, fittingTolerance);
    }
    /// <summary>
    /// Offset a curve on a surface. This curve must lie on the surface.
    /// <para>This overload allows to specify a surface point at which the offset will pass.</para>
    /// </summary>
    /// <param name="surface">A surface on which to offset.</param>
    /// <param name="throughPoint">2d point on the brep face to offset through.</param>
    /// <param name="fittingTolerance">A fitting tolerance.</param>
    /// <returns>Offset curves on success, or null on failure.</returns>
    /// <exception cref="ArgumentNullException">If surface is null.</exception>
    [ConstOperation]
    public Curve[] OffsetOnSurface(Surface surface, Point2d throughPoint, double fittingTolerance)
    {
      if (surface == null)
        throw new ArgumentNullException("surface");

      Brep b = Brep.CreateFromSurface(surface);
      return OffsetOnSurface(b.Faces[0], throughPoint, fittingTolerance);
    }
    /// <summary>
    /// Offset this curve on a surface. This curve must lie on the surface.
    /// <para>This overload allows to specify different offsets for different curve parameters.</para>
    /// </summary>
    /// <param name="surface">A surface on which to offset.</param>
    /// <param name="curveParameters">Curve parameters corresponding to the offset distances.</param>
    /// <param name="offsetDistances">Distances to offset (+)left, (-)right.</param>
    /// <param name="fittingTolerance">A fitting tolerance.</param>
    /// <returns>Offset curves on success, or null on failure.</returns>
    /// <exception cref="ArgumentNullException">If surface, curveParameters or offsetDistances are null.</exception>
    [ConstOperation]
    public Curve[] OffsetOnSurface(Surface surface, double[] curveParameters, double[] offsetDistances, double fittingTolerance)
    {
      if (surface == null)
        throw new ArgumentNullException("surface");

      Brep b = Brep.CreateFromSurface(surface);
      return OffsetOnSurface(b.Faces[0], curveParameters, offsetDistances, fittingTolerance);
    }

    /// <summary>
    /// Pulls this curve to a brep face and returns the result of that operation.
    /// </summary>
    /// <param name="face">A brep face.</param>
    /// <param name="tolerance">A tolerance value.</param>
    /// <returns>An array containing the resulting curves after pulling. This array could be empty.</returns>
    /// <exception cref="ArgumentNullException">If face is null.</exception>
    [ConstOperation]
    public Curve[] PullToBrepFace(BrepFace face, double tolerance)
    {
      if (face == null)
        throw new ArgumentNullException("face");

      IntPtr pConstCurve = ConstPointer();
      IntPtr pConstBrepFace = face.ConstPointer();
      using (Runtime.InteropWrappers.SimpleArrayCurvePointer curves = new SimpleArrayCurvePointer())
      {
        IntPtr pCurves = curves.NonConstPointer();
        UnsafeNativeMethods.RHC_RhinoPullCurveToFace(pConstCurve, pConstBrepFace, pCurves, tolerance);
        GC.KeepAlive(face);
        return curves.ToNonConstArray();
      }
    }

    /// <summary>
    /// Finds a curve by offsetting an existing curve normal to a surface.
    /// The caller is responsible for ensuring that the curve lies on the input surface.
    /// </summary>
    /// <param name="surface">Surface from which normals are calculated.</param>
    /// <param name="height">offset distance (distance from surface to result curve)</param>
    /// <returns>
    /// Offset curve at distance height from the surface.  The offset curve is
    /// interpolated through a small number of points so if the surface is irregular
    /// or complicated, the result will not be a very accurate offset.
    /// </returns>
    [ConstOperation]
    public Curve OffsetNormalToSurface(Surface surface, double height)
    {
      IntPtr pConstThis = ConstPointer();
      IntPtr pConstSurface = surface.ConstPointer();
      IntPtr pOffsetCurve = UnsafeNativeMethods.RHC_RhinoOffsetCurveNormal(pConstThis, pConstSurface, height);
      GC.KeepAlive(surface);
      return GeometryBase.CreateGeometryHelper(pOffsetCurve, null) as Curve;
    }
#endif
    #endregion methods
  }
}


// static bool ON_ForceMatchCurveEnds( OnCurve^% Crv0, int end0, OnCurve^% Crv1, int end1 );
//static bool ON_SortCurves( array<IOnCurve^>^ curve_list,
//                          [System::Runtime::InteropServices::Out]array<int>^% index,
//                          [System::Runtime::InteropServices::Out]array<bool>^% bReverse);
//static OnCurve^ RhinoFairCurve(MArgsRhinoFair^% args);
//static bool RhinoMakeCurveEndsMeet(OnCurve^ pCrv0, int end0, OnCurve^ pCrv1, int end1);
//static bool RhinoRemoveShortSegments(OnCurve^ curve, double tolerance);
//static bool RhinoProjectToPlane(OnNurbsCurve^% curve, OnPlane^% plane);
//static bool RhinoGetLineExtremes(IOnCurve^ curve, OnLine^% line);
//static OnNurbsCurve^ RhinoInterpCurve(int degree, IArrayOn3dPoint^ Pt, IOn3dVector^ start_tan, IOn3dVector^ end_tan, int knot_style, OnNurbsCurve^ nurbs_curve);
//static bool RhinoDoCurveDirectionsMatch(IOnCurve^ c0, IOnCurve^ c1);
//static bool RhinoExtendCurve(OnCurve^% crv, IRhinoExtend::Type type, int side, double length);
//static bool RhinoExtendCurve(OnCurve^% crv, IRhinoExtend::Type type, int side, array<IOnGeometry^>^ geom);
//static bool RhinoExtendCurve(OnCurve^% crv, IRhinoExtend::Type type, int side, IOn3dPoint^ end);
//static bool RhinoSimplifyCurve(OnCurve^% crv, int flags, double dist_tol, double angle_tol);
//static bool RhinoSimplifyCurveEnd(OnCurve^% pC, int side, int flags, double dist_tol, double angle_tol);
//static bool RhinoRepairCurve(OnCurve^ pCurve, double repair_tolerance, int dim);
//static int RhinoPlanarClosedCurveContainmentTest(IOnCurve^ closed_curveA, IOnCurve^ closed_curveB, IOnPlane^ plane, double tolerance);
//static bool RhinoPlanarCurveCollisionTest(IOnCurve^ curveA, IOnCurve^ curveB, IOnPlane^ plane, double tolerance);
//static int RhinoPointInPlanarClosedCurve(IOn3dPoint^ point, IOnCurve^ closed_curve, IOnPlane^ plane, double tolerance);
//static bool RhinoDivideCurve(IOnCurve^ curve, double seg_count, double seg_length, bool bReverse, bool bIncludeEnd, ArrayOn3dPoint^ curve_P, Arraydouble^ curve_t);
//static bool RhinoGetOverlapDistance(IOnCurve^ crv_a, IOnInterval^ dom_a, IOnCurve^ crv_b, IOnInterval^ dom_b, double tol, double lim,
//  [System::Runtime::InteropServices::Out]int% cnt,
//  [System::Runtime::InteropServices::Out]array<double,2>^% int_a,
//  [System::Runtime::InteropServices::Out]array<double,2>^% int_b,
//  [System::Runtime::InteropServices::Out]double% max_a,
//  [System::Runtime::InteropServices::Out]double% max_b,
//  [System::Runtime::InteropServices::Out]double% max_d,
//  [System::Runtime::InteropServices::Out]double% min_a,
//  [System::Runtime::InteropServices::Out]double% min_b,
//  [System::Runtime::InteropServices::Out]double% min_d);
//static int RhinoMakeCubicBeziers( IOnCurve^ Curve, [System::Runtime::InteropServices::Out]array<OnBezierCurve^>^% BezArray, double dist_tol, double kink_tol);
//static int Rhino_dup_cmp_curve( IOnCurve^ crva, IOnCurve^ crvb );
//static OnNurbsCurve^ RhinoFitCurve( IOnCurve^ curve_in, int degree, double dFitTol, double dAngleTol );
//      static ArrayOn3dPoint^ RhinoDivideCurveEquidistant( IOnCurve^ curve, double distance );
