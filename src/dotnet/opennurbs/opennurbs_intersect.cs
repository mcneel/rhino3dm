using System;
using System.Collections.Generic;
using Rhino.Runtime.InteropWrappers;
using System.Linq;
using Rhino.FileIO;
using System.Runtime.InteropServices;
using System.Numerics;

namespace Rhino.Geometry.Intersect
{
  /// <summary>
  /// Provides static methods for the computation of intersections, projections, sections and similar.
  /// </summary>
  public static class Intersection
  {
    #region analytic
    /// <summary>
    /// Intersects two lines.
    /// </summary>
    /// <param name="lineA">First line for intersection.</param>
    /// <param name="lineB">Second line for intersection.</param>
    /// <param name="a">
    /// Parameter on lineA that is closest to LineB. 
    /// The shortest distance between the lines is the chord from lineA.PointAt(a) to lineB.PointAt(b)
    /// </param>
    /// <param name="b">
    /// Parameter on lineB that is closest to LineA. 
    /// The shortest distance between the lines is the chord from lineA.PointAt(a) to lineB.PointAt(b)
    /// </param>
    /// <param name="tolerance">
    /// If tolerance > 0.0, then an intersection is reported only if the distance between the points is &lt;= tolerance. 
    /// If tolerance &lt;= 0.0, then the closest point between the lines is reported.
    /// </param>
    /// <param name="finiteSegments">
    /// If true, the input lines are treated as finite segments. 
    /// If false, the input lines are treated as infinite lines.
    /// </param>
    /// <returns>
    /// true if a closest point can be calculated and the result passes the tolerance parameter test; otherwise false.
    /// </returns>
    /// <remarks>
    /// If the lines are exactly parallel, meaning the system of equations used to find a and b 
    /// has no numerical solution, then false is returned. If the lines are nearly parallel, which 
    /// is often numerically true even if you think the lines look exactly parallel, then the 
    /// closest points are found and true is returned. So, if you care about weeding out "parallel" 
    /// lines, then you need to do something like the following:
    /// <code lang="cs">
    /// bool rc = Intersect.LineLine(lineA, lineB, out a, out b, tolerance, segments);
    /// if (rc)
    /// {
    ///   double angle_tol = RhinoMath.ToRadians(1.0); // or whatever
    ///   double parallel_tol = Math.Cos(angle_tol);
    ///   if ( Math.Abs(lineA.UnitTangent * lineB.UnitTangent) >= parallel_tol )
    ///   {
    ///     ... do whatever you think is appropriate
    ///   }
    /// }
    /// </code>
    /// <code lang="vb">
    /// Dim rc As Boolean = Intersect.LineLine(lineA, lineB, a, b, tolerance, segments)
    /// If (rc) Then
    ///   Dim angle_tol As Double = RhinoMath.ToRadians(1.0) 'or whatever
    ///   Dim parallel_tolerance As Double = Math.Cos(angle_tol)
    ///   If (Math.Abs(lineA.UnitTangent * lineB.UnitTangent) >= parallel_tolerance) Then
    ///     ... do whatever you think is appropriate
    ///   End If
    /// End If
    /// </code>
    /// </remarks>
    /// <since>5.0</since>
    public static bool LineLine(Line lineA, Line lineB, out double a, out double b, double tolerance, bool finiteSegments)
    {
      bool rc = LineLine(lineA, lineB, out a, out b);
      if (rc)
      {
        if (finiteSegments)
        {
          if (a < 0.0)
            a = 0.0;
          else if (a > 1.0)
            a = 1.0;
          if (b < 0.0)
            b = 0.0;
          else if (b > 1.0)
            b = 1.0;
        }
        if (tolerance > 0.0)
        {
          rc = (lineA.PointAt(a).DistanceTo(lineB.PointAt(b)) <= tolerance);
        }
      }
      return rc;
    }
    /// <summary>
    /// Find the unique closest-points pair between two infinite lines, if it exists.
    /// </summary>
    /// <param name="lineA">First line.</param>
    /// <param name="lineB">Second line.</param>
    /// <param name="a">
    /// Parameter on lineA that is closest to lineB.
    /// </param>
    /// <param name="b">
    /// Parameter on lineB that is closest to lineA. 
    /// The shortest distance between the lines is the chord from lineA.PointAt(a) to lineB.PointAt(b)
    /// </param>
    /// <returns>
    /// true if points are found and false if the lines are numerically parallel. 
    /// Numerically parallel means the 2x2 matrix:
    /// <para>+AoA  -AoB</para>
    /// <para>-AoB  +BoB</para>
    /// is numerically singular, where A = (lineA.To - lineA.From) and B = (lineB.To-lineB.From).
    /// </returns>
    /// <example>
    /// <code source='examples\vbnet\ex_intersectlines.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_intersectlines.cs' lang='cs'/>
    /// <code source='examples\py\ex_intersectlines.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    public static bool LineLine(Line lineA, Line lineB, out double a, out double b)
    {
      a = 0; b = 0;
      return UnsafeNativeMethods.ON_Intersect_LineLine(ref lineA, ref lineB, ref a, ref b);
    }
    /// <summary>
    /// Intersects a line and a plane. This function only returns true if the 
    /// intersection result is a single point (i.e. if the line is coincident with 
    /// the plane then no intersection is assumed).
    /// </summary>
    /// <param name="line">Line for intersection.</param>
    /// <param name="plane">Plane to intersect.</param>
    /// <param name="lineParameter">Parameter on line where intersection occurs. 
    /// If the parameter is not within the {0, 1} Interval then the finite segment 
    /// does not intersect the plane.</param>
    /// <returns>true on success, false on failure.</returns>
    /// <since>5.0</since>
    public static bool LinePlane(Line line, Plane plane, out double lineParameter)
    {
      lineParameter = 0.0;
      return UnsafeNativeMethods.ON_Intersect_LinePlane(ref line, ref plane, ref lineParameter);
    }
    /// <summary>
    /// Intersects two planes and return the intersection line. If the planes are 
    /// parallel or coincident, no intersection is assumed.
    /// </summary>
    /// <param name="planeA">First plane for intersection.</param>
    /// <param name="planeB">Second plane for intersection.</param>
    /// <param name="intersectionLine">If this function returns true, 
    /// the intersectionLine parameter will return the line where the planes intersect.</param>
    /// <returns>true on success, false on failure.</returns>
    /// <since>5.0</since>
    public static bool PlanePlane(Plane planeA, Plane planeB, out Line intersectionLine)
    {
      intersectionLine = new Line();
      return UnsafeNativeMethods.ON_Intersect_PlanePlane(ref planeA, ref planeB, ref intersectionLine);
    }
    /// <summary>
    /// Intersects three planes to find the single point they all share.
    /// </summary>
    /// <param name="planeA">First plane for intersection.</param>
    /// <param name="planeB">Second plane for intersection.</param>
    /// <param name="planeC">Third plane for intersection.</param>
    /// <param name="intersectionPoint">Point where all three planes converge.</param>
    /// <returns>true on success, false on failure. If at least two out of the three planes 
    /// are parallel or coincident, failure is assumed.</returns>
    /// <since>5.0</since>
    public static bool PlanePlanePlane(Plane planeA, Plane planeB, Plane planeC, out Point3d intersectionPoint)
    {
      intersectionPoint = new Point3d();
      return UnsafeNativeMethods.ON_Intersect_PlanePlanePlane(ref planeA, ref planeB, ref planeC, ref intersectionPoint);
    }

    /// <summary>
    /// Intersects a plane with a circle using exact calculations.
    /// </summary>
    /// <param name="plane">Plane to intersect.</param>
    /// <param name="circle">Circe to intersect.</param>
    /// <param name="firstCircleParameter">First intersection parameter on circle if successful or RhinoMath.UnsetValue if not.</param>
    /// <param name="secondCircleParameter">Second intersection parameter on circle if successful or RhinoMath.UnsetValue if not.</param>
    /// <returns>The type of intersection that occurred.</returns>
    /// <since>5.0</since>
    public static PlaneCircleIntersection PlaneCircle(Plane plane, Circle circle, out double firstCircleParameter, out double secondCircleParameter)
    {
      firstCircleParameter = RhinoMath.UnsetValue;
      secondCircleParameter = RhinoMath.UnsetValue;

      if (plane.ZAxis.IsParallelTo(circle.Plane.ZAxis, RhinoMath.ZeroTolerance * Math.PI) != 0)
      {
        if (Math.Abs(plane.DistanceTo(circle.Center)) < RhinoMath.ZeroTolerance)
          return PlaneCircleIntersection.Coincident;
        return PlaneCircleIntersection.Parallel;
      }

      Line L;

      //At this point, the PlanePlane should never fail since I already checked for parallellillity.
      if (!PlanePlane(plane, circle.Plane, out L)) { return PlaneCircleIntersection.Parallel; }

      double Lt = L.ClosestParameter(circle.Center);
      Point3d Lp = L.PointAt(Lt);

      double d = circle.Center.DistanceTo(Lp);

      //If circle radius equals the projection distance, we have a tangent intersection.
      if (Math.Abs(d - circle.Radius) < RhinoMath.ZeroTolerance)
      {
        circle.ClosestParameter(Lp, out firstCircleParameter);
        secondCircleParameter = firstCircleParameter;
        return PlaneCircleIntersection.Tangent;
      }

      //If circle radius too small to get an intersection, then abort.
      if (d > circle.Radius) { return PlaneCircleIntersection.None; }

      double offset = Math.Sqrt((circle.Radius * circle.Radius) - (d * d));
      Vector3d dir = offset * L.UnitTangent;

      if (!circle.ClosestParameter(Lp + dir, out firstCircleParameter)) { return PlaneCircleIntersection.None; }
      if (!circle.ClosestParameter(Lp - dir, out secondCircleParameter)) { return PlaneCircleIntersection.None; }

      return PlaneCircleIntersection.Secant;
    }
    /// <summary>
    /// Intersects a plane with a sphere using exact calculations.
    /// </summary>
    /// <param name="plane">Plane to intersect.</param>
    /// <param name="sphere">Sphere to intersect.</param>
    /// <param name="intersectionCircle">Intersection result.</param>
    /// <returns>If <see cref="PlaneSphereIntersection.None"/> is returned, the intersectionCircle has a radius of zero and the center point 
    /// is the point on the plane closest to the sphere.</returns>
    /// <since>5.0</since>
    public static PlaneSphereIntersection PlaneSphere(Plane plane, Sphere sphere, out Circle intersectionCircle)
    {
      intersectionCircle = new Circle();
      int rc = UnsafeNativeMethods.ON_Intersect_PlaneSphere(ref plane, ref sphere, ref intersectionCircle);

      return (PlaneSphereIntersection)rc;
    }
    /// <summary>
    /// Intersects a line with a circle using exact calculations.
    /// </summary>
    /// <param name="line">Line for intersection.</param>
    /// <param name="circle">Circle for intersection.</param>
    /// <param name="t1">Parameter on line for first intersection.</param>
    /// <param name="point1">Point on circle closest to first intersection.</param>
    /// <param name="t2">Parameter on line for second intersection.</param>
    /// <param name="point2">Point on circle closest to second intersection.</param>
    /// <returns>
    /// If <see cref="LineCircleIntersection.Single"/> is returned, only t1 and point1 will have valid values. 
    /// If <see cref="LineCircleIntersection.Multiple"/> is returned, t2 and point2 will also be filled out.
    /// </returns>
    /// <example>
    /// <code source='examples\vbnet\ex_intersectlinecircle.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_intersectlinecircle.cs' lang='cs'/>
    /// <code source='examples\py\ex_intersectlinecircle.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    public static LineCircleIntersection LineCircle(Line line, Circle circle, out double t1, out Point3d point1, out double t2, out Point3d point2)
    {
      t1 = 0.0;
      t2 = 0.0;
      point1 = new Point3d();
      point2 = new Point3d();

      if (!line.IsValid || !circle.IsValid) { return LineCircleIntersection.None; }

      int rc = UnsafeNativeMethods.ON_Intersect_LineCircle(ref line, ref circle, ref t1, ref point1, ref t2, ref point2);
      return (LineCircleIntersection)rc;
    }
    /// <summary>
    /// Intersects a line with a sphere using exact calculations.
    /// </summary>
    /// <param name="line">Line for intersection.</param>
    /// <param name="sphere">Sphere for intersection.</param>
    /// <param name="intersectionPoint1">First intersection point.</param>
    /// <param name="intersectionPoint2">Second intersection point.</param>
    /// <returns>If <see cref="LineSphereIntersection.None"/> is returned, the first point is the point on the line closest to the sphere and 
    /// the second point is the point on the sphere closest to the line. 
    /// If <see cref="LineSphereIntersection.Single"/> is returned, the first point is the point on the line and the second point is the 
    /// same point on the sphere.</returns>
    /// <since>5.0</since>
    public static LineSphereIntersection LineSphere(Line line, Sphere sphere, out Point3d intersectionPoint1, out Point3d intersectionPoint2)
    {
      intersectionPoint1 = new Point3d();
      intersectionPoint2 = new Point3d();
      int rc = UnsafeNativeMethods.ON_Intersect_LineSphere(ref line, ref sphere, ref intersectionPoint1, ref intersectionPoint2);

      return (LineSphereIntersection)rc;
    }
    /// <summary>
    /// Intersects a line with a cylinder using exact calculations.
    /// </summary>
    /// <param name="line">Line for intersection.</param>
    /// <param name="cylinder">Cylinder for intersection.</param>
    /// <param name="intersectionPoint1">First intersection point.</param>
    /// <param name="intersectionPoint2">Second intersection point.</param>
    /// <returns>If None is returned, the first point is the point on the line closest
    /// to the cylinder and the second point is the point on the cylinder closest to
    /// the line. 
    /// <para>If <see cref="LineCylinderIntersection.Single"/> is returned, the first point
    /// is the point on the line and the second point is the  same point on the
    /// cylinder.</para></returns>
    /// <since>5.0</since>
    public static LineCylinderIntersection LineCylinder(Line line, Cylinder cylinder, out Point3d intersectionPoint1, out Point3d intersectionPoint2)
    {
      intersectionPoint1 = new Point3d();
      intersectionPoint2 = new Point3d();
      int rc = UnsafeNativeMethods.ON_Intersect_LineCylinder(ref line, ref cylinder, ref intersectionPoint1, ref intersectionPoint2);

      return (LineCylinderIntersection)rc;
    }
    /// <summary>
    /// Intersects two spheres using exact calculations.
    /// </summary>
    /// <param name="sphereA">First sphere to intersect.</param>
    /// <param name="sphereB">Second sphere to intersect.</param>
    /// <param name="intersectionCircle">
    /// If intersection is a point, then that point will be the center, radius 0.
    /// </param>
    /// <returns>
    /// The intersection type.
    /// </returns>
    /// <since>5.0</since>
    public static SphereSphereIntersection SphereSphere(Sphere sphereA, Sphere sphereB, out Circle intersectionCircle)
    {
      intersectionCircle = new Circle();
      int rc = UnsafeNativeMethods.ON_Intersect_SphereSphere(ref sphereA, ref sphereB, ref intersectionCircle);

      if (rc <= 0 || rc > 3)
      {
        return SphereSphereIntersection.None;
      }

      return (SphereSphereIntersection)rc;
    }

    /// <summary>
    /// Intersects two arcs using exact calculations.
    /// </summary>
    /// <param name="arcA">First arc to intersect.</param>
    /// <param name="arcB">Second arc to intersect.</param>
    /// <param name="intersectionPoint1">First intersection point.</param>
    /// <param name="intersectionPoint2">Second intersection point.</param>
    /// <returns>The intersection type.</returns>
    /// <since>7.12</since>
    public static ArcArcIntersection ArcArc(Arc arcA, Arc arcB, out Point3d intersectionPoint1, out Point3d intersectionPoint2)
    {
      intersectionPoint1 = new Point3d();
      intersectionPoint2 = new Point3d();
      int rc = UnsafeNativeMethods.ON_Intersect_ArcArc(ref arcA, ref arcB, ref intersectionPoint1, ref intersectionPoint2);
      return (ArcArcIntersection)rc;
    }

    /// <summary>
    /// Intersects two circles using exact calculations.
    /// </summary>
    /// <param name="circleA">First circle to intersect.</param>
    /// <param name="circleB">Second circle to intersect.</param>
    /// <param name="intersectionPoint1">First intersection point.</param>
    /// <param name="intersectionPoint2">Second intersection point.</param>
    /// <returns>The intersection type.</returns>
    /// <since>7.12</since>
    public static CircleCircleIntersection CircleCircle(Circle circleA, Circle circleB, out Point3d intersectionPoint1, out Point3d intersectionPoint2)
    {
      intersectionPoint1 = new Point3d();
      intersectionPoint2 = new Point3d();
      int rc = UnsafeNativeMethods.ON_Intersect_CircleCircle(ref circleA, ref circleB, ref intersectionPoint1, ref intersectionPoint2);
      return (CircleCircleIntersection)rc;
    }

    /// <summary>
    /// Intersects an infinite line and an axis aligned bounding box.
    /// </summary>
    /// <param name="box">BoundingBox to intersect.</param>
    /// <param name="line">Line for intersection.</param>
    /// <param name="tolerance">
    /// If tolerance &gt; 0.0, then the intersection is performed against a box 
    /// that has each side moved out by tolerance.
    /// </param>
    /// <param name="lineParameters">
    /// The chord from line.PointAt(lineParameters.T0) to line.PointAt(lineParameters.T1) is the intersection.
    /// </param>
    /// <returns>true if the line intersects the box, false if no intersection occurs.</returns>
    /// <since>5.0</since>
    public static bool LineBox(Line line, BoundingBox box, double tolerance, out Interval lineParameters)
    {
      lineParameters = new Interval();
      return UnsafeNativeMethods.ON_Intersect_BoundingBoxLine(ref box, ref line, tolerance, ref lineParameters);
    }
    /// <summary>
    /// Intersects an infinite line with a box volume.
    /// </summary>
    /// <param name="box">Box to intersect.</param>
    /// <param name="line">Line for intersection.</param>
    /// <param name="tolerance">
    /// If tolerance &gt; 0.0, then the intersection is performed against a box 
    /// that has each side moved out by tolerance.
    /// </param>
    /// <param name="lineParameters">
    /// The chord from line.PointAt(lineParameters.T0) to line.PointAt(lineParameters.T1) is the intersection.
    /// </param>
    /// <returns>true if the line intersects the box, false if no intersection occurs.</returns>
    /// <since>5.0</since>
    public static bool LineBox(Line line, Box box, double tolerance, out Interval lineParameters)
    {
      //David: test this!
      BoundingBox bbox = new BoundingBox(new Point3d(box.X.Min, box.Y.Min, box.Z.Min),
                                         new Point3d(box.X.Max, box.Y.Max, box.Z.Max));
      Transform xform = Transform.ChangeBasis(Plane.WorldXY, box.Plane);
      line.Transform(xform);

      return LineBox(line, bbox, tolerance, out lineParameters);
    }
    #endregion

    #region sections
#if RHINO_SDK
    /// <summary>
    /// Intersects a curve with an (infinite) plane.
    /// </summary>
    /// <param name="curve">Curve to intersect.</param>
    /// <param name="plane">Plane to intersect with.</param>
    /// <param name="tolerance">Tolerance to use during intersection.</param>
    /// <returns>A list of intersection events or null if no intersections were recorded.</returns>
    /// <since>5.0</since>
    public static CurveIntersections CurvePlane(Curve curve, Plane plane, double tolerance)
    {
      if (!plane.IsValid)
        return null;

      // Use dedicated plane intersector in Rhino5
      IntPtr pConstCurve = curve.ConstPointer();
      IntPtr pIntersectArray = UnsafeNativeMethods.ON_Curve_IntersectPlane(pConstCurve, ref plane, tolerance);
      GC.KeepAlive(curve);
      return CurveIntersections.Create(pIntersectArray);
    }

    /// <summary>
    /// Intersects a mesh with an infinite plane.
    /// <see cref="RhinoMath.ZeroTolerance"/> is used as fixed tolerance.
    /// We suggest to use the document tolerance multiplied by <see cref="MeshIntersectionsTolerancesCoefficient"/> instaed.
    /// </summary>
    /// <param name="mesh">Mesh to intersect.</param>
    /// <param name="plane">Plane to intersect with.</param>
    /// <returns>
    /// An array of polylines describing the intersection loops, 
    /// or null if no intersections could be found.
    /// </returns>
    /// <since>5.0</since>
    public static Polyline[] MeshPlane(Mesh mesh, Plane plane)
    {
      return MeshPlane(mesh, null, plane, RhinoMath.ZeroTolerance);
    }

    /// <summary>
    /// Intersects a mesh with an infinite plane.
    /// <para>Tolerance can be specified.</para>
    /// <para>We suggest to use the document tolerance multiplied by <see cref="MeshIntersectionsTolerancesCoefficient"/>.</para>
    /// </summary>
    /// <param name="mesh">Mesh to intersect.</param>
    /// <param name="cache">Intersection cache for mesh.</param>
    /// <param name="plane">Plane to intersect with.</param>
    /// <param name="tolerance">Intersection tolerance.</param>
    /// <returns>
    /// An array of polylines describing the intersection loops, 
    /// or null if no intersections could be found.
    /// </returns>
    /// <since>8.0</since>
    public static Polyline[] MeshPlane(Mesh mesh, MeshIntersectionCache cache, Plane plane, double tolerance)
    {
      Rhino.Collections.RhinoList<Plane> planes = new Rhino.Collections.RhinoList<Plane>(1, plane);
      return MeshPlane(mesh, cache, planes, tolerance);
    }

    /// <summary>
    /// Intersects a mesh with an infinite plane.
    /// <para>Tolerance can be specified.</para>
    /// <para>We suggest to use the document tolerance multiplied by <see cref="MeshIntersectionsTolerancesCoefficient"/>.</para>
    /// </summary>
    /// <param name="mesh">Mesh to intersect.</param>
    /// <param name="cache">Intersection cache for mesh.</param>
    /// <param name="plane">Plane to intersect with.</param>
    /// <param name="tolerance">Intersection tolerance.</param>
    /// <param name="overlaps">If true, overlaps are computed and will be part of the output.</param>
    /// <returns>
    /// An array of polylines describing the intersection loops, 
    /// or null if no intersections could be found.
    /// </returns>
    /// <since>8.0</since>
    public static Polyline[] MeshPlane(Mesh mesh, MeshIntersectionCache cache, Plane plane, double tolerance, bool overlaps)
    {
      Rhino.Collections.RhinoList<Plane> planes = new Rhino.Collections.RhinoList<Plane>(1, plane);
      return MeshPlane(mesh, cache, planes, tolerance, overlaps);
    }

    /// <summary>
    /// Intersects a mesh with a collection of infinite planes.
    /// <para><see cref="RhinoMath.ZeroTolerance"/> is used as fixed tolerance.</para>
    /// <para>We suggest to use the document tolerance multiplied by <see cref="MeshIntersectionsTolerancesCoefficient"/> instaed.</para>
    /// </summary>
    /// <param name="mesh">Mesh to intersect.</param>
    /// <param name="planes">Planes to intersect with.</param>
    /// <returns>
    /// An array of polylines describing the intersection loops, 
    /// or null if no intersections could be found.
    /// </returns>
    /// <since>5.0</since>
    public static Polyline[] MeshPlane(Mesh mesh, IEnumerable<Plane> planes)
    {
      return MeshPlane(mesh, null, planes, RhinoMath.ZeroTolerance);
    }

    /// <summary>
    /// Intersects a mesh with a collection of infinite planes, providing both overlaps and perforations.
    /// <para>Tolerance can be specified.</para>
    /// <para>We suggest to use the document tolerance multiplied by <see cref="MeshIntersectionsTolerancesCoefficient"/>.</para>
    /// </summary>
    /// <param name="mesh">Mesh to intersect.</param>
    /// <param name="cache">Intersection cache for the mesh.</param>
    /// <param name="planes">Planes to intersect with.</param>
    /// <param name="tolerance">Intersection tolerance.</param>
    /// <returns>
    /// An array of polylines describing the intersection loops, 
    /// or null if no intersections could be found.
    /// </returns>
    /// <since>8.0</since>
    public static Polyline[] MeshPlane(Mesh mesh, MeshIntersectionCache cache, IEnumerable<Plane> planes, double tolerance)
    {
      return MeshPlane(mesh, cache, planes, tolerance, true);
    }

    /// <summary>
    /// Intersects a mesh with a collection of infinite planes.
    /// <para>Tolerance can be specified.</para>
    /// <para>We suggest to use the document tolerance multiplied by <see cref="MeshIntersectionsTolerancesCoefficient"/>.</para>
    /// </summary>
    /// <param name="mesh">Mesh to intersect.</param>
    /// <param name="cache">Intersection cache for the mesh.</param>
    /// <param name="planes">Planes to intersect with.</param>
    /// <param name="tolerance">Intersection tolerance.</param>
    /// <param name="overlaps">If true, overlaps are computed and will be part of the output.</param>
    /// <returns>
    /// An array of polylines describing the intersection loops, 
    /// or null if no intersections could be found.
    /// </returns>
    /// <since>9.0</since>
    public static Polyline[] MeshPlane(Mesh mesh, MeshIntersectionCache cache, IEnumerable<Plane> planes, double tolerance, bool overlaps)
    {
      // https://mcneel.myjetbrains.com/youtrack/issue/RH-67504

      if (null == mesh || null == planes)
        return null;
      Rhino.Collections.RhinoList<Plane> list = planes as Rhino.Collections.RhinoList<Plane> ?? new Rhino.Collections.RhinoList<Plane>(planes);
      if (list.Count < 1)
        return null;

      bool use_local_cache = (cache == null && list.Count > 2);

      using (MeshIntersectionCache local_cache = use_local_cache ? new MeshIntersectionCache() : null)
      {
        IntPtr pMesh = mesh.ConstPointer();

        MeshIntersectionCache cache_in_use = use_local_cache ? local_cache : cache;

        IntPtr pCache = (null != cache_in_use) ? cache_in_use.NonConstPointer() : IntPtr.Zero;

        using (var out_points = new SimpleArrayArrayPoint3d())
        {
          IntPtr pOutPoints = out_points.NonConstPointer();

          int count = UnsafeNativeMethods.ON_Mesh_GetIntersections(pMesh, pCache, list.Count, list.m_items, tolerance, overlaps, pOutPoints);

          GC.KeepAlive(mesh); GC.KeepAlive(cache_in_use);

          if (count > 0)
          {
            List<Polyline> out_polylines = new List<Polyline>(out_points.Count);
            for (int i = 0; i < out_points.Count; i++)
            {
              Polyline pline = out_points.PolylineAt(i);
              if (null != pline)
                out_polylines.Add(pline);
            }
            return out_polylines.ToArray();
          }
        }
      }

      return null;
    }

    /// <summary>
    /// Intersects a mesh with an (infinite) plane. This is the old version in v5
    /// </summary>
    /// <param name="mesh">Mesh to intersect.</param>
    /// <param name="plane">Plane to intersect with.</param>
    /// <returns>An array of polylines describing the intersection loops or null (Nothing in Visual Basic) if no intersections could be found.</returns>
    /// <since>5.0</since>
    internal static Polyline[] MeshPlaneOld(Mesh mesh, Plane plane)
    {
      Rhino.Collections.RhinoList<Plane> planes = new Rhino.Collections.RhinoList<Plane>(1, plane);
      return MeshPlaneOld(mesh, planes);
    }

    /// <summary>
    /// Intersects a mesh with a collection of (infinite) planes. This is the old version in v5
    /// </summary>
    /// <param name="mesh">Mesh to intersect.</param>
    /// <param name="planes">Planes to intersect with.</param>
    /// <returns>An array of polylines describing the intersection loops or null (Nothing in Visual Basic) if no intersections could be found.</returns>
    /// <exception cref="ArgumentNullException">If planes is null.</exception>
    /// <since>5.0</since>
    internal static Polyline[] MeshPlaneOld(Mesh mesh, IEnumerable<Plane> planes)
    {
      if (planes == null) throw new ArgumentNullException("planes");

      Rhino.Collections.RhinoList<Plane> list = planes as Rhino.Collections.RhinoList<Plane> ??
                                                new Rhino.Collections.RhinoList<Plane>(planes);
      if (list.Count < 1)
        return null;

      IntPtr pMesh = mesh.ConstPointer();
      int polylines_created = 0;
      IntPtr pPolys = UnsafeNativeMethods.TL_Intersect_MeshPlanes1(pMesh, list.Count, list.m_items, ref polylines_created);
      GC.KeepAlive(mesh);
      if (polylines_created < 1 || IntPtr.Zero == pPolys)
        return null;

      // convert the C++ polylines created into .NET polylines
      Polyline[] rc = new Polyline[polylines_created];
      for (int i = 0; i < polylines_created; i++)
      {
        int point_count = UnsafeNativeMethods.ON_Intersect_MeshPlanes2(pPolys, i);
        Polyline pl = new Polyline(point_count);
        if (point_count > 0)
        {
          pl.m_size = point_count;
          UnsafeNativeMethods.ON_Intersect_MeshPlanes3(pPolys, i, point_count, pl.m_items);
        }
        rc[i] = pl;
      }
      UnsafeNativeMethods.ON_Intersect_MeshPlanes4(pPolys);

      return rc;
    }

    /// <summary>
    /// Intersects a Brep with an (infinite) plane.
    /// </summary>
    /// <param name="brep">Brep to intersect.</param>
    /// <param name="plane">Plane to intersect with.</param>
    /// <param name="tolerance">Tolerance to use for intersections.</param>
    /// <param name="joinCurves">If true, join the resulting curves where possible.</param>
    /// <param name="intersectionCurves">The intersection curves will be returned here.</param>
    /// <param name="intersectionPoints">The intersection points will be returned here.</param>
    /// <returns>true on success, false on failure.</returns>
    /// <since>8.27</since>
    public static bool BrepPlane(Brep brep, Plane plane, double tolerance, bool joinCurves, out Curve[] intersectionCurves, out Point3d[] intersectionPoints)
    {
      intersectionPoints = null;
      intersectionCurves = null;

      if (brep == null)
        return false;

      if (!plane.IsValid)
        return false;

      intersectionCurves = new Curve[0];
      intersectionPoints = new Point3d[0];

      Runtime.InteropWrappers.SimpleArrayPoint3d outputPoints = new Runtime.InteropWrappers.SimpleArrayPoint3d();
      IntPtr outputPointsPtr = outputPoints.NonConstPointer();

      Runtime.InteropWrappers.SimpleArrayCurvePointer outputCurves = new Runtime.InteropWrappers.SimpleArrayCurvePointer();
      IntPtr outputCurvesPtr = outputCurves.NonConstPointer();

      IntPtr brepPtr = brep.ConstPointer();

      bool rc = UnsafeNativeMethods.ON_Intersect_BrepPlane(brepPtr, plane, tolerance, joinCurves, outputCurvesPtr, outputPointsPtr);

      if (rc)
      {
        intersectionCurves = outputCurves.ToNonConstArray();
        intersectionPoints = outputPoints.ToArray();
      }

      outputPoints.Dispose();
      outputCurves.Dispose();
      Runtime.CommonObject.GcProtect(brep);

      return rc;
    }

    /// <summary>
    /// Intersects a Brep with an (infinite) plane.
    /// </summary>
    /// <param name="brep">Brep to intersect.</param>
    /// <param name="plane">Plane to intersect with.</param>
    /// <param name="tolerance">Tolerance to use for intersections.</param>
    /// <param name="intersectionCurves">The intersection curves will be returned here.</param>
    /// <param name="intersectionPoints">The intersection points will be returned here.</param>
    /// <returns>true on success, false on failure.</returns>
    /// <since>5.0</since>
    public static bool BrepPlane(Brep brep, Plane plane, double tolerance, out Curve[] intersectionCurves, out Point3d[] intersectionPoints)
    {
      return BrepPlane(brep, plane, tolerance, true, out intersectionCurves, out intersectionPoints);
    }

    /// <summary>
    /// Intersects geometry with an (infinite) plane.
    /// </summary>
    /// <param name="geometry">GeometryBase object to intersect.</param>
    /// <param name="plane">Plane to intersect with.</param>
    /// <param name="tolerance">Tolerance to use for intersections.</param>
    /// <param name="joinCurves">If true, join the resulting curves of each object where possible, for every geometry type. Curves of different objects, such as the objects of an instance definition, are never joined.</param>
    /// <param name="doc">The document that has the instance definition when geometry is an InstanceReferenceGeometry. Can be null for other geometry.</param>
    /// <param name="intersectionCurves">The intersection curves will be returned here.</param>
    /// <param name="intersectionPoints">The intersection points will be returned here.</param>
    /// <returns>
    /// true if the intersection was computed, including when the plane misses the geometry.
    /// For an InstanceReferenceGeometry, true if it was computed for any object in the instance definition.
    /// false if the plane is not valid, tolerance is negative or not a number, the geometry type is not
    /// supported or the intersection failed. Curves and points found before a failure are still returned.
    /// </returns>
    /// <remarks>
    /// The curves and points are projected onto the plane. Curves that are not valid after the
    /// projection are not returned.
    /// For an InstanceReferenceGeometry, curves are fitted inside the instance definition with tolerance
    /// divided by the largest scale factor of the transform, so they are within tolerance in world
    /// coordinates, also for non-uniform scale. Points are tested with their distance to the plane in
    /// world coordinates. With non-uniform scale, overlaps of curves with the plane are detected with the
    /// stricter curve tolerance, so a curve closer than tolerance to the plane can be missed as an overlap.
    /// When joinCurves is true, the open curves of each object are joined: they can be reversed, very short curves
    /// can be dropped, and joined curves are PolyCurve.
    /// For point clouds, the points within tolerance of the plane are returned as one polyline in
    /// intersectionCurves, or in intersectionPoints when there is only one. Points closer together
    /// than tolerance are merged.
    /// The objects of an instance definition that are not visible are skipped, as the Contour command does.
    /// </remarks>
    /// <since>9.0</since>
    public static bool GeometryPlane(GeometryBase geometry, Plane plane, double tolerance, bool joinCurves, RhinoDoc doc, out Curve[] intersectionCurves, out Point3d[] intersectionPoints)
    {
      return GeometryPlane(geometry, plane, tolerance, joinCurves, false, false, doc, Transform.Identity, null,
        System.Threading.CancellationToken.None, out intersectionCurves, out intersectionPoints);
    }

    /// <summary>
    /// Intersects geometry with an (infinite) plane.
    /// </summary>
    /// <param name="geometry">GeometryBase object to intersect.</param>
    /// <param name="plane">Plane to intersect with.</param>
    /// <param name="tolerance">Tolerance to use for intersections.</param>
    /// <param name="joinCurves">If true, join the resulting curves of each object where possible, for every geometry type. Curves of different objects, such as the objects of an instance definition, are never joined.</param>
    /// <param name="pointCloudAsPoints">
    /// If true, every point of a PointCloud within tolerance of the plane is returned in intersectionPoints.
    /// If false, they are returned as one polyline in intersectionCurves, as the Contour command does.
    /// </param>
    /// <param name="includeHiddenObjects">
    /// If false, the objects of an instance definition that are hidden or on a layer that is off are skipped,
    /// as the Contour command does. Nested instance references are entered whether they are visible or not,
    /// and their objects are tested one by one.
    /// </param>
    /// <param name="doc">The document that has the instance definition when geometry is, or contains, an InstanceReferenceGeometry. Can be null for other geometry.</param>
    /// <param name="xform">
    /// Maps geometry to the space of the plane and the results, as InstanceReferenceGeometry.Xform does.
    /// Avoids transforming a copy of the geometry. Must not be singular.
    /// </param>
    /// <param name="cache">
    /// Data computed from the geometry alone, kept between calls, or null. Reuse one cache for many planes through the same geometry.
    /// </param>
    /// <param name="cancel">
    /// Checked between the objects of an instance definition, in point clouds and in mesh intersections.
    /// A single Brep intersection cannot be cancelled once it has started.
    /// </param>
    /// <param name="intersectionCurves">The intersection curves will be returned here.</param>
    /// <param name="intersectionPoints">The intersection points will be returned here.</param>
    /// <returns>
    /// true if the intersection was computed, including when the plane misses the geometry.
    /// For an InstanceReferenceGeometry, true if it was computed for any object in the instance definition.
    /// false if the plane is not valid, tolerance is negative or not a number, the geometry type is not
    /// supported, the intersection failed or was cancelled, or xform or the transform of an
    /// InstanceReferenceGeometry is singular. Curves and points found before a failure or cancellation are
    /// still returned.
    /// </returns>
    /// <remarks>
    /// The curves and points are projected onto the plane. Curves that are not valid after the
    /// projection are not returned.
    /// For xform and for an InstanceReferenceGeometry, curves are fitted in the space of the geometry with
    /// tolerance divided by the largest scale factor of the transform, so they are within tolerance in world
    /// coordinates, also for non-uniform scale. Points are tested with their distance to the plane in
    /// world coordinates. With non-uniform scale, overlaps of curves with the plane are detected with the
    /// stricter curve tolerance, so a curve closer than tolerance to the plane can be missed as an overlap.
    /// When joinCurves is true, the open curves of each object are joined: they can be reversed, very short curves
    /// can be dropped, and joined curves are PolyCurve. Objects after a cancellation are not joined.
    /// For point clouds, the points within tolerance of the plane are returned in intersectionPoints when
    /// pointCloudAsPoints is true, all of them. Otherwise they are returned as one polyline in intersectionCurves,
    /// or in intersectionPoints when there is only one, and points closer together than tolerance are merged.
    /// </remarks>
    /// <since>9.0</since>
    public static bool GeometryPlane(GeometryBase geometry, Plane plane, double tolerance, bool joinCurves, bool pointCloudAsPoints,
      bool includeHiddenObjects, RhinoDoc doc, Transform xform, PlaneIntersectionCache cache, System.Threading.CancellationToken cancel,
      out Curve[] intersectionCurves, out Point3d[] intersectionPoints)
    {
      intersectionPoints = null;
      intersectionCurves = null;

      if (geometry == null)
        return false;

      if (!plane.IsValid)
        return false;

      // Not Runtime.Interop.MarshalProgressAndCancelToken: its registration on the token is never
      // removed, and callers such as Grasshopper keep one token for many calls.
      ThreadTerminator terminator = null;
      var registration = default(System.Threading.CancellationTokenRegistration);
      if (cancel.CanBeCanceled)
      {
        terminator = new ThreadTerminator();
        registration = cancel.Register(terminator.RequestCancel);
      }
      try
      {
        using (var outputPoints = new SimpleArrayPoint3d())
        using (var outputCurves = new SimpleArrayCurvePointer())
        {
          uint docSN = doc != null ? doc.RuntimeSerialNumber : 0;
          IntPtr cachePtr = IntPtr.Zero;
          if (cache != null)
          {
            cachePtr = cache.NonConstPointer();
            cache.KeepAlive(geometry);
          }
          IntPtr terminatorPtr = terminator != null ? terminator.NonConstPointer() : IntPtr.Zero;

          bool rc = UnsafeNativeMethods.ON_Intersect_GeometryPlane(geometry.ConstPointer(), plane, tolerance, joinCurves, pointCloudAsPoints, includeHiddenObjects, docSN,
            ref xform, cachePtr, terminatorPtr, outputCurves.NonConstPointer(), outputPoints.NonConstPointer());

          // Copied also when rc is false: outputCurves.Dispose() does not delete the curves.
          intersectionCurves = outputCurves.ToNonConstArray();
          intersectionPoints = outputPoints.ToArray();

          Runtime.CommonObject.GcProtect(geometry);
          GC.KeepAlive(cache);
          return rc;
        }
      }
      finally
      {
        // Dispose waits for a callback that is running, so none runs after the terminator is deleted.
        registration.Dispose();
        if (terminator != null) terminator.Dispose();
      }
    }
    #endif

    /// <summary>
    /// Utility function for creating a PlaneSurface through a Box.
    /// </summary>
    /// <param name="plane">Plane to extend.</param>
    /// <param name="box">Box to extend through.</param>
    /// <param name="fuzzyness">Box will be inflated by this amount.</param>
    /// <returns>A Plane surface through the box or null.</returns>
    internal static PlaneSurface ExtendThroughBox(Plane plane, BoundingBox box, double fuzzyness)
    {
      if (fuzzyness != 0.0) { box.Inflate(fuzzyness); }

      Point3d[] corners = box.GetCorners();
      int side = 0;
      bool valid = false;

      for (int i = 0; i < corners.Length; i++)
      {
        double d = plane.DistanceTo(corners[i]);
        if (d == 0.0) { continue; }

        if (d < 0.0)
        {
          if (side > 0) { valid = true; break; }
          side = -1;
        }
        else
        {
          if (side < 0) { valid = true; break; }
          side = +1;
        }
      }

      if (!valid) { return null; }

      Interval s, t;
      if (!plane.ExtendThroughBox(box, out s, out t)) { return null; }

      if (s.IsSingleton || t.IsSingleton)
        return null;

      return new PlaneSurface(plane, s, t);
    }
    #endregion

    #region geometric
#if RHINO_SDK
    /// <summary>
    /// Finds the places where a curve intersects itself. 
    /// </summary>
    /// <param name="curve">Curve for self-intersections.</param>
    /// <param name="tolerance">Intersection tolerance. If the curve approaches itself to within tolerance, 
    /// an intersection is assumed.</param>
    /// <returns>A collection of intersection events.</returns>
    /// <since>5.0</since>
    public static CurveIntersections CurveSelf(Curve curve, double tolerance)
    {
      IntPtr pCurve = curve.ConstPointer();
      IntPtr pIntersectArray = UnsafeNativeMethods.ON_Intersect_CurveSelf(pCurve, tolerance);
      GC.KeepAlive(curve);
      return CurveIntersections.Create(pIntersectArray);
    }

    /// <summary>
    /// Finds the intersections between two curves. 
    /// </summary>
    /// <param name="curveA">First curve for intersection.</param>
    /// <param name="curveB">Second curve for intersection.</param>
    /// <param name="tolerance">Intersection tolerance. If the curves approach each other to within tolerance, an intersection is assumed.</param>
    /// <param name="overlapTolerance">The tolerance with which the curves are tested.</param>
    /// <returns>A collection of intersection events.</returns>
    /// <example>
    /// <code source='examples\vbnet\ex_intersectcurves.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_intersectcurves.cs' lang='cs'/>
    /// <code source='examples\py\ex_intersectcurves.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    public static CurveIntersections CurveCurve(Curve curveA, Curve curveB, double tolerance, double overlapTolerance)
    {
      IntPtr pCurveA = curveA.ConstPointer();
      IntPtr pCurveB = curveB.ConstPointer();
      IntPtr pIntersectArray = UnsafeNativeMethods.ON_Intersect_CurveCurve(pCurveA, pCurveB, tolerance, overlapTolerance, IntPtr.Zero, IntPtr.Zero);
      Runtime.CommonObject.GcProtect(curveA, curveB);
      return CurveIntersections.Create(pIntersectArray);
    }

    /// <summary>
    /// Finds the intersections between two curves.
    /// </summary>
    /// <param name="curveA">First curve for intersection.</param>
    /// <param name="curveB">Second curve for intersection.</param>
    /// <param name="tolerance">Intersection tolerance. If the curves approach each other to within tolerance, an intersection is assumed.</param>
    /// <param name="overlapTolerance">The tolerance with which the curves are tested.</param>
    /// <param name="invalidIndices">The indices in the resulting CurveIntersections collection that are invalid.</param>
    /// <param name="textLog">A text log that contains tails about the invalid intersection events.</param>
    /// <returns>A collection of intersection events.</returns>
    /// <since>7.0</since>
    public static CurveIntersections CurveCurveValidate(Curve curveA, Curve curveB, double tolerance, double overlapTolerance, out int[] invalidIndices, out TextLog textLog)
    {
      invalidIndices = new int[0];
      textLog = new TextLog();
      using (SimpleArrayInt output_indices = new SimpleArrayInt())
      {
        IntPtr ptr_curve_a = curveA.ConstPointer();
        IntPtr ptr_curve_b = curveB.ConstPointer();
        IntPtr ptr_indices = output_indices.NonConstPointer();
        IntPtr ptr_text_log = textLog.NonConstPointer();
        IntPtr ptr_intersect_array = UnsafeNativeMethods.ON_Intersect_CurveCurve(ptr_curve_a, ptr_curve_b, tolerance, overlapTolerance, ptr_indices, ptr_text_log);
        if (ptr_intersect_array != IntPtr.Zero)
          invalidIndices = output_indices.ToArray();
        Runtime.CommonObject.GcProtect(curveA, curveB);
        return CurveIntersections.Create(ptr_intersect_array);
      }
    }

    /// <summary>
    /// Intersects a curve and an infinite line. 
    /// </summary>
    /// <param name="curve">Curve for intersection.</param>
    /// <param name="line">Infinite line to intersect.</param>
    /// <param name="tolerance">Intersection tolerance. If the curves approach each other to within tolerance, an intersection is assumed.</param>
    /// <param name="overlapTolerance">The tolerance with which the curves are tested.</param>
    /// <returns>A collection of intersection events.</returns>
    /// <since>6.0</since>
    public static CurveIntersections CurveLine(Curve curve, Line line, double tolerance, double overlapTolerance)
    {
      // https://mcneel.myjetbrains.com/youtrack/issue/RH-68486
      IntPtr pCurve = curve.ConstPointer();
      IntPtr pIntersectArray = UnsafeNativeMethods.ON_Intersect_CurveLine(pCurve, line.From, line.To, tolerance, overlapTolerance);
      Runtime.CommonObject.GcProtect(curve, line);
      return CurveIntersections.Create(pIntersectArray);
    }

    /// <summary>
    /// Intersects a curve and a surface.
    /// </summary>
    /// <param name="curve">Curve for intersection.</param>
    /// <param name="surface">Surface for intersection.</param>
    /// <param name="tolerance">Intersection tolerance. If the curve approaches the surface to within tolerance, an intersection is assumed.</param>
    /// <param name="overlapTolerance">The tolerance with which the curves are tested.</param>
    /// <returns>A collection of intersection events.</returns>
    /// <example>
    /// <code source='examples\vbnet\ex_curvesurfaceintersect.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_curvesurfaceintersect.cs' lang='cs'/>
    /// <code source='examples\py\ex_curvesurfaceintersect.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    public static CurveIntersections CurveSurface(Curve curve, Surface surface, double tolerance, double overlapTolerance)
    {
      IntPtr pCurve = curve.ConstPointer();
      IntPtr pSurface = surface.ConstPointer();
      if (overlapTolerance > 0.0 && overlapTolerance < tolerance)
        overlapTolerance = tolerance;

      IntPtr pIntersectArray = UnsafeNativeMethods.ON_Intersect_CurveSurface(pCurve, pSurface, tolerance, overlapTolerance, IntPtr.Zero, IntPtr.Zero);
      Runtime.CommonObject.GcProtect(curve, surface);
      return CurveIntersections.Create(pIntersectArray);
    }

    /// <summary>
    /// Intersects a curve and a surface.
    /// </summary>
    /// <param name="curve">Curve for intersection.</param>
    /// <param name="surface">Surface for intersection.</param>
    /// <param name="tolerance">Intersection tolerance. If the curve approaches the surface to within tolerance, an intersection is assumed.</param>
    /// <param name="overlapTolerance">The tolerance with which the curves are tested.</param>
    /// <param name="invalidIndices">The indices in the resulting CurveIntersections collection that are invalid.</param>
    /// <param name="textLog">A text log that contains tails about the invalid intersection events.</param>
    /// <returns>A collection of intersection events.</returns>
    /// <since>7.0</since>
    public static CurveIntersections CurveSurfaceValidate(Curve curve, Surface surface, double tolerance, double overlapTolerance, out int[] invalidIndices, out TextLog textLog)
    {
      invalidIndices = new int[0];
      textLog = new TextLog();

      if (overlapTolerance > 0.0 && overlapTolerance < tolerance)
        overlapTolerance = tolerance;

      using (SimpleArrayInt output_indices = new SimpleArrayInt())
      {
        IntPtr ptr_curve = curve.ConstPointer();
        IntPtr ptr_surface = surface.ConstPointer();
        IntPtr ptr_indices = output_indices.NonConstPointer();
        IntPtr ptr_text_log = textLog.NonConstPointer();
        IntPtr ptr_intersect_array = UnsafeNativeMethods.ON_Intersect_CurveSurface(ptr_curve, ptr_surface, tolerance, overlapTolerance, ptr_indices, ptr_text_log);
        if (ptr_intersect_array != IntPtr.Zero)
          invalidIndices = output_indices.ToArray();
        Runtime.CommonObject.GcProtect(curve, surface);
        return CurveIntersections.Create(ptr_intersect_array);
      }
    }

    /// <summary>
    /// Intersects a sub-curve and a surface.
    /// </summary>
    /// <param name="curve">Curve for intersection.</param>
    /// <param name="curveDomain">Domain of sub-curve to take into consideration for Intersections.</param>
    /// <param name="surface">Surface for intersection.</param>
    /// <param name="tolerance">Intersection tolerance. If the curve approaches the surface to within tolerance, an intersection is assumed.</param>
    /// <param name="overlapTolerance">The tolerance with which the curves are tested.</param>
    /// <returns>A collection of intersection events.</returns>
    /// <since>5.0</since>
    public static CurveIntersections CurveSurface(Curve curve, Interval curveDomain, Surface surface, double tolerance, double overlapTolerance)
    {
      Interval domain = curve.Domain;
      double t0 = Math.Max(domain.Min, curveDomain.Min);
      double t1 = Math.Min(domain.Max, curveDomain.Max);
      if (t0 >= t1) { return null; }

      IntPtr pCurve = curve.ConstPointer();
      IntPtr pSurface = surface.ConstPointer();
      IntPtr pIntersectArray = UnsafeNativeMethods.ON_Intersect_CurveSurface2(pCurve, pSurface, t0, t1, tolerance, overlapTolerance, IntPtr.Zero, IntPtr.Zero);
      Runtime.CommonObject.GcProtect(curve, surface);
      return CurveIntersections.Create(pIntersectArray);
    }

    /// <summary>
    /// Intersects a sub-curve and a surface.
    /// </summary>
    /// <param name="curve">Curve for intersection.</param>
    /// <param name="curveDomain">Domain of sub-curve to take into consideration for Intersections.</param>
    /// <param name="surface">Surface for intersection.</param>
    /// <param name="tolerance">Intersection tolerance. If the curve approaches the surface to within tolerance, an intersection is assumed.</param>
    /// <param name="overlapTolerance">The tolerance with which the curves are tested.</param>
    /// <param name="invalidIndices">The indices in the resulting CurveIntersections collection that are invalid.</param>
    /// <param name="textLog">A text log that contains tails about the invalid intersection events.</param>
    /// <returns>A collection of intersection events.</returns>
    /// <since>7.0</since>
    public static CurveIntersections CurveSurfaceValidate(Curve curve, Interval curveDomain, Surface surface, double tolerance, double overlapTolerance, out int[] invalidIndices, out TextLog textLog)
    {
      invalidIndices = new int[0];
      textLog = new TextLog();

      Interval domain = curve.Domain;
      double t0 = Math.Max(domain.Min, curveDomain.Min);
      double t1 = Math.Min(domain.Max, curveDomain.Max);
      if (t0 >= t1) { return null; }

      if (overlapTolerance > 0.0 && overlapTolerance < tolerance)
        overlapTolerance = tolerance;

      using (SimpleArrayInt output_indices = new SimpleArrayInt())
      {
        IntPtr ptr_curve = curve.ConstPointer();
        IntPtr ptr_surface = surface.ConstPointer();
        IntPtr ptr_indices = output_indices.NonConstPointer();
        IntPtr ptr_text_log = textLog.NonConstPointer();
        IntPtr ptr_intersect_array = UnsafeNativeMethods.ON_Intersect_CurveSurface2(ptr_curve, ptr_surface, t0, t1, tolerance, overlapTolerance, ptr_indices, ptr_text_log);
        if (ptr_intersect_array != IntPtr.Zero)
          invalidIndices = output_indices.ToArray();
        Runtime.CommonObject.GcProtect(curve, surface);
        return CurveIntersections.Create(ptr_intersect_array);
      }
    }

    /// <summary>
    /// Intersects a curve with a Brep. This function returns the 3D points of intersection
    /// and 3D overlap curves. If an error occurs while processing overlap curves, this function 
    /// will return false, but it will still provide partial results.
    /// </summary>
    /// <param name="curve">Curve for intersection.</param>
    /// <param name="brep">Brep for intersection.</param>
    /// <param name="tolerance">Fitting and near miss tolerance.</param>
    /// <param name="overlapCurves">The overlap curves will be returned here.</param>
    /// <param name="intersectionPoints">The intersection points will be returned here.</param>
    /// <returns>true on success, false on failure.</returns>
    /// <example>
    /// <code source='examples\vbnet\ex_elevation.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_elevation.cs' lang='cs'/>
    /// <code source='examples\py\ex_elevation.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    public static bool CurveBrep(Curve curve, Brep brep, double tolerance, out Curve[] overlapCurves, out Point3d[] intersectionPoints)
    {
      overlapCurves = new Curve[0];
      intersectionPoints = new Point3d[0];

      if (curve == null || brep == null)
        return false;

      Runtime.InteropWrappers.SimpleArrayPoint3d outputPoints = new Runtime.InteropWrappers.SimpleArrayPoint3d();
      IntPtr outputPointsPtr = outputPoints.NonConstPointer();

      Runtime.InteropWrappers.SimpleArrayCurvePointer outputCurves = new Runtime.InteropWrappers.SimpleArrayCurvePointer();
      IntPtr outputCurvesPtr = outputCurves.NonConstPointer();

      IntPtr curvePtr = curve.ConstPointer();
      IntPtr brepPtr = brep.ConstPointer();

      bool rc = UnsafeNativeMethods.ON_Intersect_CurveBrep(curvePtr, brepPtr, tolerance, outputCurvesPtr, outputPointsPtr);
      if (rc)
      {
        overlapCurves = outputCurves.ToNonConstArray();
        intersectionPoints = outputPoints.ToArray();
      }

      outputPoints.Dispose();
      outputCurves.Dispose();
      Runtime.CommonObject.GcProtect(curve, brep);
      return rc;
    }

    /// <summary>
    /// Intersects a curve with a Brep. This function returns the 3D points of intersection, curve parameters at the intersection locations,
    /// and 3D overlap curves. If an error occurs while processing overlap curves, this function 
    /// will return false, but it will still provide partial results.
    /// </summary>
    /// <param name="curve">Curve for intersection.</param>
    /// <param name="brep">Brep for intersection.</param>
    /// <param name="tolerance">Fitting and near miss tolerance.</param>
    /// <param name="overlapCurves">The overlap curves will be returned here.</param>
    /// <param name="intersectionPoints">The intersection points will be returned here.</param>
    /// <param name="curveParameters">The intersection curve parameters will be returned here.</param>
    /// <returns>true on success, false on failure.</returns>
    /// <since>6.0</since>
    public static bool CurveBrep(Curve curve, Brep brep, double tolerance, out Curve[] overlapCurves, out Point3d[] intersectionPoints, out double[] curveParameters)
    {
      overlapCurves = new Curve[0];
      intersectionPoints = new Point3d[0];
      curveParameters = new double[0];

      if (curve == null || brep == null)
        return false;

      SimpleArrayPoint3d outputPoints = new SimpleArrayPoint3d();
      IntPtr outputPointsPtr = outputPoints.NonConstPointer();

      SimpleArrayCurvePointer outputCurves = new SimpleArrayCurvePointer();
      IntPtr outputCurvesPtr = outputCurves.NonConstPointer();

      SimpleArrayDouble outputParameters = new SimpleArrayDouble();
      IntPtr outputParametersPtr = outputParameters.NonConstPointer();

      IntPtr curvePtr = curve.ConstPointer();
      IntPtr brepPtr = brep.ConstPointer();


      bool rc = UnsafeNativeMethods.ON_Intersect_CurveBrep2(curvePtr, brepPtr, tolerance, outputCurvesPtr, outputPointsPtr, outputParametersPtr);
      if (rc)
      {
        overlapCurves = outputCurves.ToNonConstArray();
        intersectionPoints = outputPoints.ToArray();
        curveParameters = outputParameters.ToArray();
      }

      outputPoints.Dispose();
      outputCurves.Dispose();
      outputParameters.Dispose();
      Runtime.CommonObject.GcProtect(curve, brep);

      return rc;
    }

    /// <summary>
    /// Intersect a curve with a Brep. This function returns the intersection parameters on the curve.
    /// </summary>
    /// <param name="curve">Curve.</param>
    /// <param name="brep">Brep.</param>
    /// <param name="tolerance">Absolute tolerance for intersections.</param>
    /// <param name="angleTolerance">Angle tolerance in radians.</param>
    /// <param name="t">Curve parameters at intersections.</param>
    /// <returns>True on success, false on failure.</returns>
    /// <since>6.0</since>
    public static bool CurveBrep(Curve curve, Brep brep, double tolerance, double angleTolerance, out double[] t)
    {
      if (curve == null) throw new ArgumentNullException(nameof(curve));
      if (brep == null) throw new ArgumentNullException(nameof(brep));

      IntPtr curvePtr = curve.ConstPointer();
      IntPtr brepPtr = brep.ConstPointer();
      using (SimpleArrayDouble array = new SimpleArrayDouble())
      {
        IntPtr arrayPtr = array.NonConstPointer();

        bool rc = false;
        rc = UnsafeNativeMethods.ON_Intersect_CurveBrepParameters(curvePtr, brepPtr, tolerance, angleTolerance, arrayPtr);

        Runtime.CommonObject.GcProtect(curve, brep);
        if (rc)
          t = array.ToArray();
        else
          t = new double[0];
        return rc;
      }
    }

    /// <summary>
    /// Intersects a curve with a Brep face.
    /// </summary>
    /// <param name="curve">A curve.</param>
    /// <param name="face">A brep face.</param>
    /// <param name="tolerance">Fitting and near miss tolerance.</param>
    /// <param name="overlapCurves">A overlap curves array argument. This out reference is assigned during the call.</param>
    /// <param name="intersectionPoints">A points array argument. This out reference is assigned during the call.</param>
    /// <returns>true on success, false on failure.</returns>
    /// <since>5.0</since>
    public static bool CurveBrepFace(Curve curve, BrepFace face, double tolerance, out Curve[] overlapCurves, out Point3d[] intersectionPoints)
    {
      overlapCurves = new Curve[0];
      intersectionPoints = new Point3d[0];

      Runtime.InteropWrappers.SimpleArrayPoint3d outputPoints = new Runtime.InteropWrappers.SimpleArrayPoint3d();
      IntPtr outputPointsPtr = outputPoints.NonConstPointer();

      Runtime.InteropWrappers.SimpleArrayCurvePointer outputCurves = new Runtime.InteropWrappers.SimpleArrayCurvePointer();
      IntPtr outputCurvesPtr = outputCurves.NonConstPointer();

      IntPtr curvePtr = curve.ConstPointer();
      IntPtr facePtr = face.ConstPointer();

      bool rc = UnsafeNativeMethods.RHC_RhinoCurveFaceIntersect(curvePtr, facePtr, tolerance, outputCurvesPtr, outputPointsPtr);

      if (rc)
      {
        overlapCurves = outputCurves.ToNonConstArray();
        intersectionPoints = outputPoints.ToArray();
      }

      outputPoints.Dispose();
      outputCurves.Dispose();
      Runtime.CommonObject.GcProtect(curve, face);

      return rc;
    }

    /// <summary>
    /// Intersects two Surfaces.
    /// </summary>
    /// <param name="surfaceA">First Surface for intersection.</param>
    /// <param name="surfaceB">Second Surface for intersection.</param>
    /// <param name="tolerance">Intersection tolerance.</param>
    /// <param name="intersectionCurves">The intersection curves will be returned here.</param>
    /// <param name="intersectionPoints">The intersection points will be returned here.</param>
    /// <returns>true on success, false on failure.</returns>
    /// <since>5.0</since>
    public static bool SurfaceSurface(Surface surfaceA, Surface surfaceB, double tolerance, out Curve[] intersectionCurves, out Point3d[] intersectionPoints)
    {
      intersectionCurves = new Curve[0];
      intersectionPoints = new Point3d[0];

      Runtime.InteropWrappers.SimpleArrayPoint3d outputPoints = new Runtime.InteropWrappers.SimpleArrayPoint3d();
      IntPtr outputPointsPtr = outputPoints.NonConstPointer();

      Runtime.InteropWrappers.SimpleArrayCurvePointer outputCurves = new Runtime.InteropWrappers.SimpleArrayCurvePointer();
      IntPtr outputCurvesPtr = outputCurves.NonConstPointer();

      IntPtr srfPtrA = surfaceA.ConstPointer();
      IntPtr srfPtrB = surfaceB.ConstPointer();

      bool rc = UnsafeNativeMethods.RHC_RhinoIntersectSurfaces(srfPtrA, srfPtrB, tolerance, outputCurvesPtr, outputPointsPtr);

      if (rc)
      {
        intersectionCurves = outputCurves.ToNonConstArray();
        intersectionPoints = outputPoints.ToArray();
      }

      outputPoints.Dispose();
      outputCurves.Dispose();
      Runtime.CommonObject.GcProtect(surfaceA, surfaceB);

      return rc;
    }

    /// <summary>
    /// Intersects a plane and a bounding box.
    /// </summary>
    /// <param name="plane">The plane.</param>
    /// <param name="boundingBox">The bounding box.</param>
    /// <param name="polyline">The output polyline if successful.</param>
    /// <returns>
    /// True if successful, false otherwise.
    /// </returns>
    /// <remarks>
    /// Intersects the four bounding box infinite lines in the direction of the max coordinate with the plane.
    /// Point and single line intersections are ignored.
    /// </remarks>
    /// <since>8.0</since>
    public static bool PlaneBoundingBox(Plane plane, BoundingBox boundingBox, out Polyline polyline)
    {
      polyline = null;
      using (SimpleArrayPoint3d points = new SimpleArrayPoint3d())
      {
        IntPtr ptr_points = points.NonConstPointer();
        bool rc = UnsafeNativeMethods.RHC_RhinoIntersectPlaneWithBoundingBox(ref plane, ref boundingBox, ptr_points);
        if (rc)
          polyline = Polyline.PolyLineFromNativeArray(points);
        return rc;
      }
    }

    /// <summary>
    /// Intersects two Breps.
    /// </summary>
    /// <param name="brepA">First Brep for intersection.</param>
    /// <param name="brepB">Second Brep for intersection.</param>
    /// <param name="tolerance">Intersection tolerance.</param>
    /// <param name="intersectionCurves">The intersection curves will be returned here.</param>
    /// <param name="intersectionPoints">The intersection points will be returned here.</param>
    /// <returns>
    /// true if the operation was success, false otherwise.
    /// Check the output arrays to determine how the input intersects.
    /// </returns>
    /// <since>5.0</since>
    public static bool BrepBrep(Brep brepA, Brep brepB, double tolerance, out Curve[] intersectionCurves, out Point3d[] intersectionPoints)
    {
      return BrepBrep(brepA, brepB, tolerance, true, out intersectionCurves, out intersectionPoints);
    }

    /// <summary>
    /// Intersects two Breps.
    /// </summary>
    /// <param name="brepA">First Brep for intersection.</param>
    /// <param name="brepB">Second Brep for intersection.</param>
    /// <param name="tolerance">Intersection tolerance.</param>
    /// <param name="joinCurves">If true, join the resulting curves where possible.</param>
    /// <param name="intersectionCurves">The intersection curves will be returned here.</param>
    /// <param name="intersectionPoints">The intersection points will be returned here.</param>
    /// <returns>
    /// true if the operation was success, false otherwise.
    /// Check the output arrays to determine how the input intersects.
    /// </returns>
    /// <since>8.12</since>
    public static bool BrepBrep(Brep brepA, Brep brepB, double tolerance, bool joinCurves, out Curve[] intersectionCurves, out Point3d[] intersectionPoints)
    {
      intersectionCurves = new Curve[0];
      intersectionPoints = new Point3d[0];

      if (brepA == null || brepB == null)
        return false;

      Runtime.InteropWrappers.SimpleArrayPoint3d outputPoints = new Runtime.InteropWrappers.SimpleArrayPoint3d();
      IntPtr outputPointsPtr = outputPoints.NonConstPointer();

      Runtime.InteropWrappers.SimpleArrayCurvePointer outputCurves = new Runtime.InteropWrappers.SimpleArrayCurvePointer();
      IntPtr outputCurvesPtr = outputCurves.NonConstPointer();

      IntPtr brepPtrA = brepA.ConstPointer();
      IntPtr brepPtrB = brepB.ConstPointer();

      bool rc = UnsafeNativeMethods.ON_Intersect_BrepBrep(brepPtrA, brepPtrB, tolerance, joinCurves, outputCurvesPtr, outputPointsPtr);

      if (rc)
      {
        intersectionCurves = outputCurves.ToNonConstArray();
        intersectionPoints = outputPoints.ToArray();
      }

      outputPoints.Dispose();
      outputCurves.Dispose();
      Runtime.CommonObject.GcProtect(brepA, brepB);

      return rc;
    }

    /// <summary>
    /// Intersects a Brep and a Surface.
    /// </summary>
    /// <param name="brep">A brep to be intersected.</param>
    /// <param name="surface">A surface to be intersected.</param>
    /// <param name="tolerance">A tolerance value.</param>
    /// <param name="intersectionCurves">The intersection curves array argument. This out reference is assigned during the call.</param>
    /// <param name="intersectionPoints">The intersection points array argument. This out reference is assigned during the call.</param>
    /// <returns>
    /// true if the operation was success, false otherwise.
    /// Check the output arrays to determine how the input intersects.
    /// </returns>
    /// <since>5.0</since>
    public static bool BrepSurface(Brep brep, Surface surface, double tolerance, out Curve[] intersectionCurves, out Point3d[] intersectionPoints)
    {
      return BrepSurface(brep, surface, tolerance, true, out intersectionCurves, out intersectionPoints);
    }

    /// <summary>
    /// Intersects a Brep and a Surface.
    /// </summary>
    /// <param name="brep">A brep to be intersected.</param>
    /// <param name="surface">A surface to be intersected.</param>
    /// <param name="tolerance">A tolerance value.</param>
    /// <param name="joinCurves">If true, join the resulting curves where possible.</param>
    /// <param name="intersectionCurves">The intersection curves array argument. This out reference is assigned during the call.</param>
    /// <param name="intersectionPoints">The intersection points array argument. This out reference is assigned during the call.</param>
    /// <returns>
    /// true if the operation was success, false otherwise.
    /// Check the output arrays to determine how the input intersects.
    /// </returns>
    /// <since>8.12</since>
    public static bool BrepSurface(Brep brep, Surface surface, double tolerance, bool joinCurves, out Curve[] intersectionCurves, out Point3d[] intersectionPoints)
    {
      intersectionCurves = new Curve[0];
      intersectionPoints = new Point3d[0];

      if (brep == null || surface == null)
        return false;

      Runtime.InteropWrappers.SimpleArrayPoint3d outputPoints = new Runtime.InteropWrappers.SimpleArrayPoint3d();
      IntPtr outputPointsPtr = outputPoints.NonConstPointer();

      Runtime.InteropWrappers.SimpleArrayCurvePointer outputCurves = new Runtime.InteropWrappers.SimpleArrayCurvePointer();
      IntPtr outputCurvesPtr = outputCurves.NonConstPointer();

      IntPtr brepPtr = brep.ConstPointer();
      IntPtr surfacePtr = surface.ConstPointer();

      bool rc = UnsafeNativeMethods.ON_Intersect_BrepSurface(brepPtr, surfacePtr, tolerance, joinCurves, outputCurvesPtr, outputPointsPtr);

      if (rc)
      {
        intersectionCurves = outputCurves.ToNonConstArray();
        intersectionPoints = outputPoints.ToArray();
      }

      outputPoints.Dispose();
      outputCurves.Dispose();
      Runtime.CommonObject.GcProtect(brep, surface);

      return rc;
    }

    /// <summary>
    /// Fast intersection of two meshes, returned as loose line segments rather than assembled polylines.
    /// <para>Vertices are read in double precision when the mesh has them. Faces that cross or touch within
    /// tolerance produce segments; coplanar overlaps are ignored. Use <see cref="MeshMesh(IEnumerable{Mesh}, double, out Polyline[], bool, out Polyline[], bool, out Mesh, FileIO.TextLog, System.Threading.CancellationToken, IProgress{double})"/>
    /// when overlaps or joined curves are needed, and <see cref="MeshMeshPredicate(IEnumerable{Mesh}, IEnumerable{Mesh}, double, bool, FileIO.TextLog, System.Threading.CancellationToken)"/>
    /// when only the yes/no answer is needed.</para>
    /// </summary>
    /// <param name="meshA">First mesh for intersection.</param>
    /// <param name="meshB">Second mesh for intersection.</param>
    /// <returns>An array of intersection line segments, empty if no intersections were found.</returns>
    /// <since>5.0</since>
    public static Line[] MeshMeshFast(Mesh meshA, Mesh meshB)
    {
      if (meshA == null) return Array.Empty<Line>();
      if (meshB == null) return Array.Empty<Line>();

      IntPtr ptrA = meshA.ConstPointer();
      IntPtr ptrB = meshB.ConstPointer();
      Line[] intersectionLines = new Line[0];

      using (Runtime.InteropWrappers.SimpleArrayLine arr = new Runtime.InteropWrappers.SimpleArrayLine())
      {
        IntPtr pLines = arr.NonConstPointer();
        int rc = UnsafeNativeMethods.ON_Mesh_IntersectMesh(ptrA, ptrB, pLines);
        if (rc > 0)
          intersectionLines = arr.ToArray();
      }
      Runtime.CommonObject.GcProtect(meshA, meshB);

      return intersectionLines;
    }

    /// <summary>
    /// Instructs Rhino to provide the new mesh-mesh intersector implementation. This affects also MeshSplit.
    /// </summary>
    internal static bool UseNewMeshIntersections
    {
      get
      {
        return UnsafeNativeMethods.RH_MX_UseNew(false, false);
      }
      set
      {
        UnsafeNativeMethods.RH_MX_UseNew(true, value);
      }
    }

    /// <summary>
    /// Instructs Rhino to provide the new mesh-mesh intersector implementation for MeshBoolean* commands.
    /// </summary>
    internal static bool UseNewMeshBooleans
    {
      get
      {
        return GetSet_MX_DebugOptions(6, true, default);
      }
      set
      {
        GetSet_MX_DebugOptions(6, false, value);
      }
    }

    //only internal hint for debugging. Not used in UI
    internal static bool SearchOverlaps
    {
      get
      {
        return GetSet_MX_DebugOptions(15, true, default);
      }
      set
      {
        GetSet_MX_DebugOptions(15, false, value);
      }
    }

    /// <summary>
    /// Modify an internal debug mechanism. Talk to Giulio regarding this.
    /// </summary>
    internal static bool GetSet_MX_DebugOptions(int which, bool get, bool new_value)
    {
      return UnsafeNativeMethods.RH_GetSet_MX_DebugOptions(which, get, new_value);
    }

    /// <summary>
    /// Instructs Rhino to print way way too much information regarding intersections.
    /// </summary>
    internal static bool PrintMoreMeshIntersectionInfo
    {
      get { return GetSet_MX_DebugOptions(2, true, default); }
      set { GetSet_MX_DebugOptions(2, false, value); }
    }

    /// <summary>
    /// Silence any error, do not print to command line, do not report progress, no cancellation possibility, no command interactions
    /// </summary>
    internal static bool MeshIntersectionExecutionSilence
    {
      get { return GetSet_MX_DebugOptions(0, true, default); }
      set { GetSet_MX_DebugOptions(0, false, value); }
    }

    private const string DiminishMeshIntersectionsTolerancesRequest_CODE = "MeshIntersections.RequestedDiminishTolerancesCoefficient";
    private const double DiminishMeshIntersectionsTolerancesRequest_DEFAULT = 0.0001;

    /// <summary>
    /// <para>Offers a requested adjustment coefficient for mesh-mesh intersections tolerances.
    /// The value can be used to multiply the document absolute tolerance.</para>
    /// <para>This is only a UI value; it is up to developer to honor (or not) this request, depending on application needs.</para>
    /// </summary>
    /// <remarks>Generally, document tolerances are around 0.001 for objects sized about 100 units.
    /// However, good mesh triangles for these objects are often a few orders of magnitude smaller than these values.
    /// This coefficient is provided to scale absolute document tolerances to values more suitable for good mesh intersections.
    /// </remarks>
    /// <since>7.0</since>

    // <value><para>Setting the value to 1.0 results in the setting doing nothing.</para>
    // <para>Setting the value to 0.0 results in the default value being reset.</para>
    // <para>Setting negative values results in an exception.</para></value>
    // <exception cref="ArgumentOutOfRangeException">When the value is negative.</exception>
    public static double MeshIntersectionsTolerancesCoefficient
    {
      get
      {
        return PersistentSettings.RhinoAppSettings.GetDouble(DiminishMeshIntersectionsTolerancesRequest_CODE, DiminishMeshIntersectionsTolerancesRequest_DEFAULT);
      }
      private set //[Giulio, 2020 May 2. This should generally not be modified by users, or reproducing errors will become more difficult]
      {
        if (value < 0) throw new ArgumentOutOfRangeException(nameof(value), "Value cannot be negative.");
        if (value == 0) value = DiminishMeshIntersectionsTolerancesRequest_DEFAULT;
        PersistentSettings.RhinoAppSettings.SetDouble(DiminishMeshIntersectionsTolerancesRequest_CODE, value);
      }
    }

    internal static bool MeshMesh_Helper(IEnumerable<Mesh> meshes, IEnumerable<Mesh> meshesBOrNull, double tolerance,
      bool computeSelfIntersections, bool overlaps_with_intersections, out Polyline[] intersections,
      bool overlapsPolylines, out Polyline[] overlapsPolylinesResult, bool overlapsMesh, out Mesh overlapsMeshResult,
      bool meshpairs, out int[] meshpairsResult, FileIO.TextLog textLog, System.Threading.CancellationToken cancel, IProgress<double> progress)
    {
      if (meshes == null) throw new ArgumentNullException(nameof(meshes));
      if (meshesBOrNull != null && (computeSelfIntersections || meshpairs))
        throw new ArgumentException("A second mesh set cannot be combined with self-intersections or mesh pairs.");
      tolerance = Math.Abs(tolerance);

      intersections = null;
      overlapsPolylinesResult = null;
      overlapsMeshResult = null;
      meshpairsResult = null;

      Runtime.Interop.MarshalProgressAndCancelToken(cancel, progress,
        out IntPtr ptr_terminator, out int progress_report_serial_number, out var reporter, out var terminator);

      try
      {
        using (var input = new SimpleArrayMeshPointer())
        using (var inputB = meshesBOrNull == null ? null : new SimpleArrayMeshPointer())
        {
          foreach (var mesh in meshes)
          {
            if (mesh == null) continue;
            input.Add(mesh, true);
          }

          if (inputB != null)
          {
            foreach (var mesh in meshesBOrNull)
            {
              if (mesh == null) continue;
              inputB.Add(mesh, true);
            }
          }

          using (var pairs = meshpairs ? new SimpleArray2dex() : null)
          using (var intersections_native = new SimpleArrayArrayPoint3d())
          using (var overlaps_native = (overlapsPolylines ?
            (overlaps_with_intersections ? intersections_native : new SimpleArrayArrayPoint3d()) : null))
          {
            IntPtr mesh_overlaps_ptr = IntPtr.Zero;
            if (overlapsMesh)
            {
              overlapsMeshResult = new Mesh();
              mesh_overlaps_ptr = overlapsMeshResult.NonConstPointer();
            }
            IntPtr overlaps_native_ptr = IntPtr.Zero;
            if (overlaps_native != null)
            {
              overlaps_native_ptr = overlaps_native.NonConstPointer();
            }
            IntPtr pairs_native_ptr = IntPtr.Zero;
            if (pairs != null)
            {
              pairs_native_ptr = pairs.NonConstPointer();
            }

            bool rc = UnsafeNativeMethods.RH_MX_MeshMeshIntersect(computeSelfIntersections,
              input.ConstPointer(), inputB != null ? inputB.ConstPointer() : IntPtr.Zero,
              tolerance, intersections_native.NonConstPointer(), overlaps_native_ptr,
              mesh_overlaps_ptr, meshpairs, pairs_native_ptr,
              textLog != null ? textLog.NonConstPointer() : IntPtr.Zero, ptr_terminator, progress_report_serial_number);

            GC.KeepAlive(meshes);
            GC.KeepAlive(meshesBOrNull);

            if (!rc)
            {
              if (overlapsMeshResult != null)
              {
                overlapsMeshResult.Dispose();
                overlapsMeshResult = null;
              }
              return false;
            }

            if (!overlaps_with_intersections && overlaps_native != null && overlaps_native.Count > 0)
            {
              Polyline[] output_pls = new Polyline[overlaps_native.Count];
              for (int i = 0; i < output_pls.Length; i++)
              {
                int pca = overlaps_native.PointCountAt(i);
                var npl = new Polyline(pca);

                for (int j = 0; j < pca; j++)
                {
                  npl.Add(overlaps_native[i, j]);
                }

                output_pls[i] = npl;
              }

              overlapsPolylinesResult = output_pls;
            }

            if (intersections_native.Count > 0)
            {
              Polyline[] output_pls = new Polyline[intersections_native.Count];
              for (int i = 0; i < output_pls.Length; i++)
              {
                int pca = intersections_native.PointCountAt(i);
                var npl = new Polyline(pca);

                for (int j = 0; j < pca; j++)
                {
                  npl.Add(intersections_native[i, j]);
                }

                output_pls[i] = npl;
              }

              intersections = output_pls;
            }

            // Flatten the intersecting mesh couples (native ON_2dex of input indices) into the
            // [i0,j0,i1,j1,...] result. Without this the predicate's pairs out-parameter is always null.
            if (pairs != null && pairs.Count > 0)
            {
              var couples = pairs.ToArray();
              int[] flat = new int[couples.Length * 2];
              for (int i = 0; i < couples.Length; i++)
              {
                flat[2 * i] = couples[i].I;
                flat[2 * i + 1] = couples[i].J;
              }
              meshpairsResult = flat;
            }
          }
        }
      }
      finally
      {
        if (reporter != null) reporter.Disable();
        if (terminator != null) terminator.Dispose();
      }

      return true;
    }


    /// <summary>
    /// Intersects meshes. Overlaps and perforations are provided in the output list.
    /// <para>Tolerance can be specified.</para>
    /// <para>We suggest to use the document tolerance multiplied by <see cref="MeshIntersectionsTolerancesCoefficient"/>.</para>
    /// </summary>
    /// <param name="meshes">The mesh input list. This cannot be null. Null entries are tolerated.</param>
    /// <param name="tolerance">A tolerance value. If negative, the positive value will be used.
    /// WARNING! Good tolerance values are in the magnitude of 10^-7, or RhinoMath.SqrtEpsilon*10.</param>
    /// <param name="intersections">Returns the intersections.</param>
    /// <param name="overlapsPolylines">If true, overlaps are computed and returned.</param>
    /// <param name="overlapsPolylinesResult">If requested, overlaps are returned here.</param>
    /// <param name="overlapsMesh">If true, an overlaps mesh is computed and returned.</param>
    /// <param name="overlapsMeshResult">If requested, overlaps are returned here.</param>
    /// <param name="textLog">A text log, or null.</param>
    /// <param name="cancel">A cancellation token to stop the computation at a given point.</param>
    /// <param name="progress">A progress reporter to inform the user about progress, or null. The reported value is indicative.</param>
    /// <returns>True, if the operation succeeded, otherwise false.</returns>
    /// <since>7.0</since>
    public static bool MeshMesh(IEnumerable<Mesh> meshes, double tolerance,
      out Polyline[] intersections, bool overlapsPolylines, out Polyline[] overlapsPolylinesResult, bool overlapsMesh, out Mesh overlapsMeshResult,
      FileIO.TextLog textLog, System.Threading.CancellationToken cancel, IProgress<double> progress)
    {
      return MeshMesh_Helper(meshes, null, tolerance, false, false, out intersections,
        overlapsPolylines, out overlapsPolylinesResult,
        overlapsMesh, out overlapsMeshResult, false, out _,
        textLog, cancel, progress);
    }

    /// <summary>
    /// Intersects two sets of meshes with each other. Overlaps and perforations are provided in the output list.
    /// <para>Only events between a mesh of the first set and a mesh of the second set are computed. Intersections
    /// among meshes of the same set, and self-intersections within a single mesh, are ignored.</para>
    /// <para>Tolerance can be specified.</para>
    /// <para>We suggest to use the document tolerance multiplied by <see cref="MeshIntersectionsTolerancesCoefficient"/>.</para>
    /// </summary>
    /// <param name="setA">The first mesh input list. This cannot be null. Null entries are tolerated.</param>
    /// <param name="setB">The second mesh input list. This cannot be null. Null entries are tolerated.</param>
    /// <param name="tolerance">A tolerance value. If negative, the positive value will be used.
    /// WARNING! Good tolerance values are in the magnitude of 10^-7, or RhinoMath.SqrtEpsilon*10.</param>
    /// <param name="intersections">Returns the intersections.</param>
    /// <param name="overlapsPolylines">If true, overlaps are computed and returned.</param>
    /// <param name="overlapsPolylinesResult">If requested, overlaps are returned here.</param>
    /// <param name="overlapsMesh">If true, an overlaps mesh is computed and returned.</param>
    /// <param name="overlapsMeshResult">If requested, overlaps are returned here.</param>
    /// <param name="textLog">A text log, or null.</param>
    /// <param name="cancel">A cancellation token to stop the computation at a given point.</param>
    /// <param name="progress">A progress reporter to inform the user about progress, or null. The reported value is indicative.</param>
    /// <returns>True, if the operation succeeded, otherwise false.</returns>
    /// <since>9.0</since>
    public static bool MeshMeshTwoSets(IEnumerable<Mesh> setA, IEnumerable<Mesh> setB, double tolerance,
      out Polyline[] intersections, bool overlapsPolylines, out Polyline[] overlapsPolylinesResult, bool overlapsMesh, out Mesh overlapsMeshResult,
      FileIO.TextLog textLog, System.Threading.CancellationToken cancel, IProgress<double> progress)
    {
      if (setA == null) throw new ArgumentNullException(nameof(setA));
      if (setB == null) throw new ArgumentNullException(nameof(setB));

      return MeshMesh_Helper(setA, setB, tolerance, false, false, out intersections,
        overlapsPolylines, out overlapsPolylinesResult,
        overlapsMesh, out overlapsMeshResult, false, out _,
        textLog, cancel, progress);
    }

    /// <summary>
    /// Determines which meshes intersect. No geometry is computed: each pair of meshes is tested only until
    /// its first crossing is found, with the fast intersector behind <see cref="MeshMeshFast"/>. Faces that
    /// cross or touch within tolerance count; coplanar overlap without a crossing does not.
    /// <para>The same as <see cref="MeshMeshPredicate(IEnumerable{Mesh}, IEnumerable{Mesh}, double, bool, out IntersectingMeshPair[], FileIO.TextLog, System.Threading.CancellationToken)"/> with fast = true.</para>
    /// </summary>
    /// <param name="meshes">The mesh input list. This cannot be null. Null entries are tolerated.</param>
    /// <param name="tolerance">A tolerance value. If negative, the positive value will be used.
    /// WARNING! Good tolerance values are in the magnitude of 10^-7, or RhinoMath.SqrtEpsilon*10.</param>
    /// <param name="pairs">An array containing pairs of meshes that intersect.</param>
    /// <param name="textLog">A text log, or null.</param>
    /// <returns>True, if meshes intersect, otherwise false. False is also returned on error, but textLog will start with Error: and contain the error.</returns>
    /// <since>8.0</since>
    public static bool MeshMeshPredicate(IEnumerable<Mesh> meshes, double tolerance, out int[] pairs,
      FileIO.TextLog textLog)
    {
      return MeshMesh_Helper(meshes, null, tolerance, false, false, out _,
        false, out _,
        false, out _,
        true, out pairs,
        textLog, System.Threading.CancellationToken.None, null);
    }

    /// <summary>
    /// Determines which meshes intersect, and reports one intersecting face couple for each pair found.
    /// No geometry is computed: each pair of meshes is tested only until its first hit.
    /// </summary>
    /// <param name="meshesA">The first mesh list. This cannot be null. Null entries are skipped but keep their index.</param>
    /// <param name="meshesB">A second mesh list, or null. When null, every pair within meshesA is tested once;
    /// otherwise every mesh of meshesA is tested against every mesh of meshesB. Null entries are skipped but keep their index.</param>
    /// <param name="tolerance">A tolerance value. If negative, the positive value will be used.
    /// WARNING! Good tolerance values are in the magnitude of 10^-7, or RhinoMath.SqrtEpsilon*10.</param>
    /// <param name="fast">True uses the fast intersector behind <see cref="MeshMeshFast"/>: faces that cross or touch
    /// within tolerance count, coplanar overlap without a crossing does not. False uses the accurate intersector,
    /// which also reports coplanar overlap and costs about as much as <see cref="MeshMeshFast"/>.</param>
    /// <param name="intersectingPairs">The pairs of meshes that intersect, each with the first intersecting faces found.
    /// <see cref="IntersectingMeshPair.MeshIndexA"/> indexes meshesA and <see cref="IntersectingMeshPair.MeshIndexB"/> indexes meshesB,
    /// or both index meshesA with MeshIndexA less than MeshIndexB. Empty when nothing intersects.</param>
    /// <param name="textLog">A text log, or null.</param>
    /// <param name="cancel">A cancellation token to stop the computation at a given point.</param>
    /// <returns>True if any pair intersects, otherwise false. False is also returned on error or cancellation.</returns>
    /// <since>9.0</since>
    public static bool MeshMeshPredicate(IEnumerable<Mesh> meshesA, IEnumerable<Mesh> meshesB, double tolerance, bool fast,
      out IntersectingMeshPair[] intersectingPairs, FileIO.TextLog textLog, System.Threading.CancellationToken cancel)
    {
      return MeshMeshPredicate_Helper(meshesA, meshesB, tolerance, fast, true, out intersectingPairs, textLog, cancel);
    }

    /// <summary>
    /// Determines whether any of the meshes intersect. No geometry is computed, and the search stops at the first
    /// pair of meshes that intersect: this is the quickest answer when the intersecting pairs are not needed.
    /// </summary>
    /// <param name="meshesA">The first mesh list. This cannot be null. Null entries are skipped.</param>
    /// <param name="meshesB">A second mesh list, or null. When null, every pair within meshesA is tested once;
    /// otherwise every mesh of meshesA is tested against every mesh of meshesB. Null entries are skipped.</param>
    /// <param name="tolerance">A tolerance value. If negative, the positive value will be used.
    /// WARNING! Good tolerance values are in the magnitude of 10^-7, or RhinoMath.SqrtEpsilon*10.</param>
    /// <param name="fast">True uses the fast intersector behind <see cref="MeshMeshFast"/>: faces that cross or touch
    /// within tolerance count, coplanar overlap without a crossing does not. False uses the accurate intersector,
    /// which also reports coplanar overlap and costs about as much as <see cref="MeshMeshFast"/>.</param>
    /// <param name="textLog">A text log, or null.</param>
    /// <param name="cancel">A cancellation token to stop the computation at a given point.</param>
    /// <returns>True if any pair intersects, otherwise false. False is also returned on error or cancellation.</returns>
    /// <since>9.0</since>
    public static bool MeshMeshPredicate(IEnumerable<Mesh> meshesA, IEnumerable<Mesh> meshesB, double tolerance, bool fast,
      FileIO.TextLog textLog, System.Threading.CancellationToken cancel)
    {
      return MeshMeshPredicate_Helper(meshesA, meshesB, tolerance, fast, false, out _, textLog, cancel);
    }

    static bool MeshMeshPredicate_Helper(IEnumerable<Mesh> meshesA, IEnumerable<Mesh> meshesB, double tolerance, bool fast,
      bool reportIntersectingPairs, out IntersectingMeshPair[] intersectingPairs, FileIO.TextLog textLog, System.Threading.CancellationToken cancel)
    {
      if (meshesA == null) throw new ArgumentNullException(nameof(meshesA));
      tolerance = Math.Abs(tolerance);
      intersectingPairs = Array.Empty<IntersectingMeshPair>();

      Runtime.Interop.MarshalProgressAndCancelToken(cancel, null,
        out IntPtr ptr_terminator, out int _, out var reporter, out var terminator);
      try
      {
        using (var input = new SimpleArrayMeshPointer())
        using (var inputB = meshesB == null ? null : new SimpleArrayMeshPointer())
        using (var pairs_native = reportIntersectingPairs ? new SimpleArray2dex() : null)
        using (var faces_native = reportIntersectingPairs ? new SimpleArray2dex() : null)
        {
          foreach (var mesh in meshesA)
            input.AddConstKeepingNullSlot(mesh);
          if (inputB != null)
          {
            foreach (var mesh in meshesB)
              inputB.AddConstKeepingNullSlot(mesh);
          }

          bool rc = UnsafeNativeMethods.RH_MX_MeshMeshPredicate(input.ConstPointer(), inputB != null ? inputB.ConstPointer() : IntPtr.Zero,
            tolerance, fast,
            pairs_native != null ? pairs_native.NonConstPointer() : IntPtr.Zero,
            faces_native != null ? faces_native.NonConstPointer() : IntPtr.Zero,
            textLog != null ? textLog.NonConstPointer() : IntPtr.Zero, ptr_terminator);

          GC.KeepAlive(meshesA);
          GC.KeepAlive(meshesB);

          if (reportIntersectingPairs)
            intersectingPairs = IntersectingMeshPairsFromMeshCouplesAndTheirFaceCouples(pairs_native, faces_native);
          return rc;
        }
      }
      finally
      {
        if (reporter != null) reporter.Disable();
        if (terminator != null) terminator.Dispose();
      }
    }

    static IntersectingMeshPair[] IntersectingMeshPairsFromMeshCouplesAndTheirFaceCouples(SimpleArray2dex meshCouples, SimpleArray2dex faceCouples)
    {
      IndexPair[] mesh_couples = meshCouples.ToArray();
      if (mesh_couples.Length == 0) return Array.Empty<IntersectingMeshPair>();
      IndexPair[] face_couples = faceCouples.ToArray();
      var intersecting_pairs = new IntersectingMeshPair[mesh_couples.Length];
      for (int i = 0; i < mesh_couples.Length; i++)
        intersecting_pairs[i] = new IntersectingMeshPair(mesh_couples[i].I, mesh_couples[i].J, face_couples[i].I, face_couples[i].J);
      return intersecting_pairs;
    }

    /// <summary>
    /// Intersects two meshes. Overlaps and near misses are handled. This is an old method kept for compatibility.
    /// <para>Tolerance can be specified.</para>
    /// <para>We suggest to use the document tolerance multiplied by <see cref="MeshIntersectionsTolerancesCoefficient"/>.</para>
    /// </summary>
    /// <param name="meshA">First mesh for intersection.</param>
    /// <param name="meshB">Second mesh for intersection.</param>
    /// <param name="tolerance">A tolerance value. If negative, the positive value will be used.
    /// WARNING! Good tolerance values are in the magnitude of 10^-7, or RhinoMath.SqrtEpsilon*10.</param>
    /// <returns>An array of intersection and overlaps polylines.</returns>
    /// <since>5.0</since>
    public static Polyline[] MeshMeshAccurate(Mesh meshA, Mesh meshB, double tolerance)
    {
      if (UseNewMeshIntersections)
      {
        var arr = new[] { meshA, meshB };
        var rc = MeshMesh_Helper(arr, null, tolerance, false, true,
          out Polyline[] result, true, out Polyline[] _, false, out Mesh _,
          false, out int[] _,
          null, System.Threading.CancellationToken.None, null);
        if (!rc) return null;
        return result;
      }
      else
      {
        IntPtr pMeshA = meshA.ConstPointer();
        IntPtr pMeshB = meshB.ConstPointer();
        int polylines_created = 0;
        IntPtr pPolys = UnsafeNativeMethods.ON_Intersect_MeshMesh1(pMeshA, pMeshB, ref polylines_created, tolerance);
        if (polylines_created < 1 || IntPtr.Zero == pPolys)
          return null;

        // convert the C++ polylines created into .NET polylines. We can reuse the meshplane functions
        Polyline[] rc = new Polyline[polylines_created];
        for (int i = 0; i < polylines_created; i++)
        {
          int point_count = UnsafeNativeMethods.ON_SimpleArray_ON_Polyline_itemI_count(pPolys, i);
          Polyline pl = new Polyline(point_count);
          if (point_count > 0)
          {
            pl.m_size = point_count;
            UnsafeNativeMethods.ON_SimpleArray_ON_Polyline_memcpy_del(pPolys, i, point_count, pl.m_items);
          }
          rc[i] = pl;
        }
        UnsafeNativeMethods.ON_SimpleArray_ON_Polyline_delete(pPolys);

        Runtime.CommonObject.GcProtect(meshA, meshB);
        return rc;
      }
    }

    /// <summary>Finds the first intersection of a ray with a mesh.</summary>
    /// <param name="mesh">A mesh to intersect.</param>
    /// <param name="ray">A ray to be casted.</param>
    /// <returns>
    /// &gt;= 0.0 parameter along ray if successful.
    /// &lt; 0.0 if no intersection found.
    /// </returns>
    /// <since>5.0</since>
    public static double MeshRay(Mesh mesh, Ray3d ray)
    {
      IntPtr pConstMesh = mesh.ConstPointer();
      int count = 0;
      IntPtr zero = IntPtr.Zero;
      double rc = UnsafeNativeMethods.ON_Intersect_MeshRay2(pConstMesh, ref ray, true, 0.0, ref count, ref zero, ref zero);
      GC.KeepAlive(mesh);

      return rc;
    }

    /// <summary>
    /// Finds the first intersection of each one of a set of rays with a mesh.
    /// </summary>
    /// <param name="mesh">A mesh to intersect.</param>
    /// <param name="rays">The rays to cast.</param>
    /// <returns>
    /// One value for each ray, in the order of the rays: the parameter along that ray of its
    /// first intersection with the mesh, or a value smaller than 0.0 where that ray does not
    /// meet the mesh. Null when the mesh or the set of rays is null.
    /// </returns>
    /// <remarks>
    /// The rays are cast on several threads, so this is much faster than one call of
    /// <see cref="MeshRay(Mesh, Ray3d)"/> per ray.
    /// </remarks>
    /// <since>9.0</since>
    public static double[] MeshRays(Mesh mesh, IEnumerable<Ray3d> rays)
    {
      return MeshRays(mesh, rays, true);
    }

    /// <summary>
    /// Finds the first intersection of each one of a set of rays with a mesh.
    /// </summary>
    /// <param name="mesh">A mesh to intersect.</param>
    /// <param name="rays">The rays to cast.</param>
    /// <param name="multithreaded">True to cast the rays on several threads.</param>
    /// <returns>
    /// One value for each ray, in the order of the rays: the parameter along that ray of its
    /// first intersection with the mesh, or a value smaller than 0.0 where that ray does not
    /// meet the mesh. Null when the mesh or the set of rays is null.
    /// </returns>
    /// <since>9.0</since>
    public static double[] MeshRays(Mesh mesh, IEnumerable<Ray3d> rays, bool multithreaded)
    {
      if (null == mesh || null == rays)
        return null;

      Ray3d[] ray_array = rays as Ray3d[] ?? new List<Ray3d>(rays).ToArray();
      if (ray_array.Length < 1)
        return new double[0];

      double[] rc = new double[ray_array.Length];

      IntPtr ptr_const_mesh = mesh.ConstPointer();

      // one ray is a search of a tree, so a chunk holds enough of them to be worth a thread
      const int minimum_chunk = 512;

      if (!multithreaded || ray_array.Length <= minimum_chunk)
      {
        UnsafeNativeMethods.ON_Intersect_MeshRays(ptr_const_mesh, ray_array.Length, ray_array, 0, ray_array.Length, rc);
      }
      else
      {
        int chunk_count = System.Environment.ProcessorCount * 4;
        int chunk_size = (ray_array.Length + chunk_count - 1) / chunk_count;
        if (chunk_size < minimum_chunk)
        {
          chunk_size = minimum_chunk;
          chunk_count = (ray_array.Length + chunk_size - 1) / chunk_size;
        }

        System.Threading.Tasks.Parallel.For(0, chunk_count, chunk =>
        {
          int start = chunk * chunk_size;
          int end = start + chunk_size;
          if (end > ray_array.Length)
            end = ray_array.Length;
          if (start < end)
            UnsafeNativeMethods.ON_Intersect_MeshRays(ptr_const_mesh, ray_array.Length, ray_array, start, end, rc);
        });
      }

      GC.KeepAlive(mesh);
      return rc;
    }

    /*
    /// <summary>Finds all intersections of a ray with a mesh.</summary>
    /// <param name="mesh">A mesh to intersect.</param>
    /// <param name="ray">A ray to be casted.</param>
    /// <param name="tolerance">A tolerance value to discard similar hits.</param>
    /// <param name="tParameters">A list of t values along the ray.</param>
    /// <param name="meshFaceIndices">For each t, returns a mesh face index that was hit.</param>
    public static void MeshRay(Mesh mesh, Ray3d ray, double tolerance, out double[] tParameters, out int[] meshFaceIndices)
    {
      IntPtr pConstMesh = mesh.ConstPointer();

      IntPtr tsOut = new IntPtr(int.MaxValue);
      IntPtr faceIdsOut = new IntPtr(int.MaxValue);
      int count = 0;
      double rc = UnsafeNativeMethods.ON_Intersect_MeshRay2(pConstMesh, ref ray, false, tolerance, ref count, ref tsOut, ref faceIdsOut);

      tParameters = new double[count];
      meshFaceIndices = new int[count];
      UnsafeNativeMethods.ON_Intersect_MeshPolyline_FillDelete(count, IntPtr.Zero, faceIdsOut, tsOut, null, meshFaceIndices, tParameters);

      GC.KeepAlive(mesh);
    }
    */

    /// <summary>Finds the first intersection of a ray with a mesh.</summary>
    /// <param name="mesh">A mesh to intersect.</param>
    /// <param name="ray">A ray to be casted.</param>
    /// <param name="meshFaceIndices">faces on mesh that ray intersects.</param>
    /// <returns>
    /// &gt;= 0.0 parameter along ray if successful.
    /// &lt; 0.0 if no intersection found.
    /// </returns>
    /// <remarks>
    /// The ray may intersect more than one face in cases where the ray hits
    /// the edge between two faces or the vertex corner shared by multiple faces,
    /// or more than one face is coplanar or happens to be intersecting at the
    /// same location as the ray.
    /// </remarks>
    /// <since>5.0</since>
    public static double MeshRay(Mesh mesh, Ray3d ray, out int[] meshFaceIndices)
    {
      meshFaceIndices = null;
      double rc;

      IntPtr pConstMesh = mesh.ConstPointer();
      int count = default;
      IntPtr _ = IntPtr.Zero;
      IntPtr faces = new IntPtr(int.MaxValue);
      rc = UnsafeNativeMethods.ON_Intersect_MeshRay2(pConstMesh, ref ray, true, 0.0, ref count, ref _, ref faces);

      GC.KeepAlive(mesh);
      meshFaceIndices = new int[count];
      UnsafeNativeMethods.ON_Intersect_MeshPolyline_FillDelete(count, _, faces, _, null, meshFaceIndices, null);
      GC.KeepAlive(meshFaceIndices);

      return rc;
    }

    private static Point3d[] MeshPolyline_Helper(Mesh mesh, PolylineCurve curve, out int[] faceIds, bool sorted)
    {
      faceIds = null;
      IntPtr pConstMesh = mesh.ConstPointer();
      IntPtr points = default;
      IntPtr faceIdsPtr = default;
      IntPtr pConstCurve = curve.ConstPointer();
      int count = UnsafeNativeMethods.ON_Intersect_MeshPolyline1(pConstMesh, pConstCurve, sorted, ref points, ref faceIdsPtr);

      GC.KeepAlive(mesh);

      if (0 == count || IntPtr.Zero == points || IntPtr.Zero == faceIdsPtr)
        return new Point3d[0];

      var pointsOut = new Point3d[count];
      faceIds = new int[count];
      UnsafeNativeMethods.ON_Intersect_MeshPolyline_FillDelete(count, points, faceIdsPtr, IntPtr.Zero, pointsOut, faceIds, null);

      Runtime.CommonObject.GcProtect(curve, pointsOut, faceIds);
      return pointsOut;
    }

    /// <summary>
    /// Finds the intersection of a mesh and a polyline. Starting from version 7, points are always sorted along the polyline.
    /// </summary>
    /// <param name="mesh">A mesh to intersect.</param>
    /// <param name="curve">A polyline curves to intersect.</param>
    /// <param name="faceIds">The indices of the intersecting faces. This out reference is assigned during the call.</param>
    /// <returns>An array of points: one for each face that was passed by the faceIds out reference.</returns>
    /// <since>5.0</since>
    public static Point3d[] MeshPolyline(Mesh mesh, PolylineCurve curve, out int[] faceIds)
    {
      return MeshPolyline_Helper(mesh, curve, out faceIds, true);
    }

    /// <summary>
    /// Finds the intersection of a mesh and a polyline. Points are guaranteed to be sorted along the polyline.
    /// </summary>
    /// <param name="mesh">A mesh to intersect.</param>
    /// <param name="curve">A polyline curves to intersect.</param>
    /// <param name="faceIds">The indices of the intersecting faces. This out reference is assigned during the call.</param>
    /// <returns>An array of points: one for each face that was passed by the faceIds out reference.</returns>
    /// <since>5.0</since>
    public static Point3d[] MeshPolylineSorted(Mesh mesh, PolylineCurve curve, out int[] faceIds)
    {
      return MeshPolyline_Helper(mesh, curve, out faceIds, true);
    }

    private static Point3d[] MeshLine_Helper(Mesh mesh, Line line, bool faces, out int[] faceIds, bool sorted)
    {
      sorted = true; //from Rhino 7 SR4, sorted is always on. Cost is frivolous.

      if (mesh == null) throw new ArgumentNullException(nameof(mesh));

      IntPtr pConstMesh = mesh.ConstPointer();
      IntPtr points = default;
      IntPtr faceIdsPtr = default;
      int count = UnsafeNativeMethods.ON_Intersect_MeshLine(pConstMesh, line.From, line.To, sorted, ref points, faces, ref faceIdsPtr);
      if (0 == count || IntPtr.Zero == points)
      {
        faceIds = new int[0];
        return new Point3d[0];
      }

      var pointsOut = new Point3d[count];
      if (faces) faceIds = new int[count]; else faceIds = null;
      UnsafeNativeMethods.ON_Intersect_MeshPolyline_FillDelete(count, points, faceIdsPtr, IntPtr.Zero, pointsOut, faceIds, null);
      
      Runtime.CommonObject.GcProtect(mesh, pointsOut, faceIds);
      return pointsOut;
    }


    /// <summary>
    /// Finds the intersections of a mesh and a line. The points are not necessarily sorted.
    /// </summary>
    /// <param name="mesh">A mesh to intersect</param>
    /// <param name="line">The line to intersect with the mesh</param>
    /// <param name="faceIds">The indices of the intersecting faces. This out reference is assigned during the call. Empty if nothing is found.</param>
    /// <returns>An array of points: one for each face that was passed by the faceIds out reference.
    /// Empty if no items are found.</returns>
    /// <since>5.0</since>
    public static Point3d[] MeshLine(Mesh mesh, Line line, out int[] faceIds)
    {
      return MeshLine_Helper(mesh, line, true, out faceIds, false);
    }

    /// <summary>
    /// Finds the intersections of a mesh and a line.
    /// </summary>
    /// <param name="mesh">A mesh to intersect</param>
    /// <param name="line">The line to intersect with the mesh</param>
    /// <returns>An array of points: one for each face that was passed by the faceIds out reference.
    /// Empty if no items are found.</returns>
    /// <since>7.3</since>
    public static Point3d[] MeshLine(Mesh mesh, Line line)
    {
      return MeshLine_Helper(mesh, line, false, out _, false);
    }


    /// <summary>
    /// Finds the intersections of a mesh and a line. Points are sorted along the line.
    /// </summary>
    /// <param name="mesh">A mesh to intersect</param>
    /// <param name="line">The line to intersect with the mesh</param>
    /// <param name="faceIds">The indices of the intersecting faces. This out reference is assigned during the call. Empty if nothing is found.</param>
    /// <returns>An array of points: one for each face that was passed by the faceIds out reference.
    /// Empty if no items are found.</returns>
    /// <since>5.0</since>
    public static Point3d[] MeshLineSorted(Mesh mesh, Line line, out int[] faceIds)
    {
      return MeshLine_Helper(mesh, line, true, out faceIds, true);
    }

    /// <summary>
    /// Intersects a curve and a mesh, returning certified hits sorted by curve parameter.
    /// </summary>
    /// <remarks>Every overlap is reported as two Point events at the overlap's endpoints plus one
    /// Overlap event for the in-plane span. This overload always includes the Overlap events; use
    /// the <see cref="MeshCurve(Mesh, Curve, double, bool)"/> overload to drop them while still
    /// reporting the endpoint Points.</remarks>
    /// <param name="mesh">Mesh to intersect.</param>
    /// <param name="curve">Curve to intersect.</param>
    /// <param name="tolerance">Post-process merge tolerance in 3D model space. Consecutive
    /// intersection events whose 3D endpoints are within this distance are fused into a single
    /// event. Must be a positive finite value (use the document absolute tolerance as a starting
    /// point). The internal calculation tolerance is a fixed value independent of this
    /// parameter.</param>
    /// <returns>Array of intersection events sorted by curve parameter. Empty on failure or no intersection.</returns>
    /// <since>9.0</since>
    // RH-97386: internal until automated geometry error reporting covers every cgk path.
    internal static MeshCurveIntersection[] MeshCurve(Mesh mesh, Curve curve, double tolerance)
    {
      return MeshCurve(mesh, curve, tolerance, includeOverlaps: true);
    }

    /// <summary>
    /// Same as <see cref="MeshCurve(Mesh, Curve, double)"/> with a flag controlling the Overlap
    /// span events.
    /// </summary>
    /// <remarks>
    /// Every overlap is reported as two Point events at the overlap's endpoints plus one Overlap
    /// event for the in-plane span. The two endpoint Points are emitted in both modes;
    /// <paramref name="includeOverlaps"/> controls only the span event. Pass <c>false</c> to drop
    /// the Overlap span events while still reporting the endpoint crossings as Point events — it
    /// does not reduce the result to transversal crossings only.
    /// </remarks>
    /// <since>9.0</since>
    internal static MeshCurveIntersection[] MeshCurve(Mesh mesh, Curve curve, double tolerance, bool includeOverlaps)
    {
      // tolerance must be a finite positive double: it is used as the 3D post-process merge
      // tolerance and the guard prevents a no-op call. The `!(tolerance > 0)` form also
      // rejects NaN. Caller-side validation here saves a P/Invoke round-trip.
      if (mesh == null || curve == null || !(tolerance > 0)) return new MeshCurveIntersection[0];
      IntPtr ptr_mesh = mesh.ConstPointer();
      IntPtr ptr_curve = curve.ConstPointer();
      IntPtr ptr_array = UnsafeNativeMethods.RHC_Intersect_CurveMesh(ptr_curve, ptr_mesh, tolerance, includeOverlaps);
      Runtime.CommonObject.GcProtect(mesh, curve);
      if (ptr_array == IntPtr.Zero) return new MeshCurveIntersection[0];
      try
      {
        int count = UnsafeNativeMethods.RHC_Intersect_CurveMeshCount(ptr_array);
        var result = new MeshCurveIntersection[count];
        // Fetch the flat face-incidence pool once and slice per-event below.
        int poolCount = UnsafeNativeMethods.RHC_Intersect_CurveMeshFacePoolCount(ptr_array);
        int[] pool = poolCount > 0 ? new int[poolCount] : null;
        if (poolCount > 0)
          UnsafeNativeMethods.RHC_Intersect_CurveMeshFacePoolCopy(ptr_array, pool);

        for (int i = 0; i < count; i++)
        {
          var evt = new MeshCurveIntersection();
          int on_boundary = 0;
          int incidence = 0;
          int face_indices_offset = -1;
          int face_indices_count = 0;
          UnsafeNativeMethods.RHC_Intersect_CurveMeshData(ptr_array, i,
            ref evt.m_type,
            ref evt.m_face_index, ref evt.m_face_triangle_index,
            ref face_indices_offset, ref face_indices_count,
            ref evt.m_t0, ref evt.m_t1,
            ref evt.m_point0, ref evt.m_point1,
            ref evt.m_barycentric0, ref evt.m_barycentric1,
            ref evt.m_t_tolerance, ref evt.m_point_tolerance, ref evt.m_barycentric_tolerance,
            ref on_boundary,
            ref incidence);
          evt.m_on_boundary = on_boundary != 0;
          evt.m_incidence = incidence;
          if (pool != null && face_indices_offset >= 0 && face_indices_count > 0)
          {
            evt.m_face_indices = new int[face_indices_count];
            Array.Copy(pool, face_indices_offset, evt.m_face_indices, 0, face_indices_count);
          }
          else
          {
            // Single-face hit (face interior, or mesh boundary edge): the participating-face
            // list is just the canonical face.
            evt.m_face_indices = new int[] { evt.m_face_index };
          }
          result[i] = evt;
        }
        return result;
      }
      finally
      {
        UnsafeNativeMethods.RHC_Intersect_CurveMeshDelete(ptr_array);
      }
    }

    /// <summary>
    /// Simpler overload of <see cref="MeshCurve(Mesh, Curve, double)"/>: returns the Point-event
    /// hits (isolated crossings and the endpoints of every overlap span) as a flat
    /// <see cref="Point3d"/> array, and the overlap spans as sub-curves of <paramref name="curve"/>
    /// via <paramref name="overlapCurves"/>. Both arrays are ordered by curve parameter.
    /// </summary>
    /// <param name="mesh">Mesh to intersect.</param>
    /// <param name="curve">Curve to intersect.</param>
    /// <param name="tolerance">Numerical tolerance; see the rich overload.</param>
    /// <param name="overlapCurves">Receives one trimmed sub-curve of the input per overlap event
    /// (the portion of <paramref name="curve"/> that lies in a face's plane).</param>
    /// <returns>One point per Point event — isolated crossings and the endpoints of every overlap span — ordered by curve parameter. Empty on failure or no intersection.</returns>
    /// <since>9.0</since>
    internal static Point3d[] MeshCurve(Mesh mesh, Curve curve, double tolerance, out Curve[] overlapCurves)
    {
      var events = MeshCurve(mesh, curve, tolerance);
      var points = new List<Point3d>();
      var overlaps = new List<Curve>();
      for (int i = 0; i < events.Length; i++)
      {
        var e = events[i];
        if (e.IsPoint)
        {
          points.Add(e.PointA);
        }
        else if (e.IsOverlap)
        {
          var sub = curve.Trim(e.CurveParameter);
          if (sub != null)
            overlaps.Add(sub);
          /* If Trim returns null the overlap range was outside the curve's domain or otherwise
           * un-trimmable; the event is dropped from the simpler overload's output. The rich
           * overload still exposes the full event for callers that need it. */
        }
        /* else: NoEvent / unknown — skip. */
      }
      overlapCurves = overlaps.ToArray();
      return points.ToArray();
    }

    /// <summary>
    /// Simpler overload of <see cref="MeshCurve(Mesh, Curve, double)"/> with mesh face indices.
    /// Mirrors <see cref="MeshLine(Mesh, Line, out int[])"/> and adds overlap output as sub-curves.
    /// </summary>
    /// <param name="mesh">Mesh to intersect.</param>
    /// <param name="curve">Curve to intersect.</param>
    /// <param name="tolerance">Numerical tolerance; see the rich overload.</param>
    /// <param name="faceIds">Receives one face index per returned point (parallel to the return array).</param>
    /// <param name="overlapCurves">Receives one trimmed sub-curve of the input per overlap event.</param>
    /// <param name="overlapFaceIds">Receives one face index per overlap sub-curve (parallel to <paramref name="overlapCurves"/>).</param>
    /// <returns>One point per Point event — isolated crossings and the endpoints of every overlap span — ordered by curve parameter. Empty on failure or no intersection.</returns>
    /// <since>9.0</since>
    internal static Point3d[] MeshCurve(Mesh mesh, Curve curve, double tolerance, out int[] faceIds, out Curve[] overlapCurves, out int[] overlapFaceIds)
    {
      var events = MeshCurve(mesh, curve, tolerance);
      var points = new List<Point3d>();
      var pointFaceIds = new List<int>();
      var overlaps = new List<Curve>();
      var overlapFaces = new List<int>();
      for (int i = 0; i < events.Length; i++)
      {
        var e = events[i];
        if (e.IsPoint)
        {
          points.Add(e.PointA);
          pointFaceIds.Add(e.FaceIndex);
        }
        else if (e.IsOverlap)
        {
          var sub = curve.Trim(e.CurveParameter);
          if (sub != null)
          {
            overlaps.Add(sub);
            overlapFaces.Add(e.FaceIndex);
          }
          /* If Trim returns null the event is dropped from the simpler output. The parallel
           * face-id array stays in sync because we add to both lists only on success. */
        }
        /* else: NoEvent / unknown — skip. */
      }
      faceIds = pointFaceIds.ToArray();
      overlapCurves = overlaps.ToArray();
      overlapFaceIds = overlapFaces.ToArray();
      return points.ToArray();
    }

    /// <summary>
    /// Computes point intersections that occur when shooting a ray to a collection of surfaces and Breps.
    /// </summary>
    /// <param name="ray">A ray used in intersection.</param>
    /// <param name="geometry">Only Surface and Brep objects are currently supported. Trims are ignored on Breps.</param>
    /// <param name="maxReflections">The maximum number of reflections. This value should be any value between 1 and 1000, inclusive.</param>
    /// <returns>An array of points: one for each surface or Brep face that was hit, or an empty array on failure.</returns>
    /// <exception cref="ArgumentNullException">geometry is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">maxReflections is strictly outside the [1-1000] range.</exception>
    /// <since>5.0</since>
    public static Point3d[] RayShoot(Ray3d ray, IEnumerable<GeometryBase> geometry, int maxReflections)
    {
      if (null == geometry) throw new ArgumentNullException(nameof(geometry));
      if (maxReflections < 1 || maxReflections > 1000)
        throw new ArgumentOutOfRangeException("maxReflections", "maxReflections must be between 1-1000");

      using (var in_geom = new SimpleArrayGeometryPointer(geometry))
      using (var out_points = new SimpleArrayPoint3d())
      {
        var ptr_const_geom = in_geom.ConstPointer();
        var ptr_out_points = out_points.NonConstPointer();
        int count = UnsafeNativeMethods.ON_RayShooter_ShootRay(ptr_const_geom, ray.Position, ray.Direction, maxReflections, ptr_out_points, IntPtr.Zero, IntPtr.Zero, false);
        if (count > 0) 
          return out_points.ToArray();
      }
      return new Point3d[0];
    }

    /// <summary>
    /// Computes point intersections that occur when shooting a ray to a collection of surfaces and Breps.
    /// </summary>
    /// <param name="geometry">The collection of surfaces and Breps to intersect. Trims are ignored on Breps.</param>
    /// <param name="ray">>A ray used in intersection.</param>
    /// <param name="maxReflections">The maximum number of reflections. This value should be any value between 1 and 1000, inclusive.</param>
    /// <returns>An array of RayShootEvent structs if successful, or an empty array on failure.</returns>
    /// <exception cref="ArgumentNullException">geometry is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">maxReflections is strictly outside the [1-1000] range.</exception>
    /// <since>7.0</since>
    public static RayShootEvent[] RayShoot(IEnumerable<GeometryBase> geometry, Ray3d ray, int maxReflections)
    {
      return RayShoot(geometry, ray, maxReflections, false);
    }

    /// <summary>
    /// Computes point intersections that occur when shooting a ray to a collection of surfaces and Breps.
    /// </summary>
    /// <param name="geometry">The collection of surfaces and Breps to intersect.</param>
    /// <param name="ray">A ray used in intersection.</param>
    /// <param name="maxReflections">The maximum number of reflections. This value should be any value between 1 and 1000, inclusive.</param>
    /// <param name="honorTrims">
    /// When true, hits that land outside a Brep face's trimming loops are skipped and the ray continues.
    /// When false, Breps are treated as their untrimmed surfaces, which is what Rhino 8 and earlier did.
    /// </param>
    /// <returns>An array of RayShootEvent structs if successful, or an empty array on failure.</returns>
    /// <exception cref="ArgumentNullException">geometry is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">maxReflections is strictly outside the [1-1000] range.</exception>
    /// <since>9.0</since>
    public static RayShootEvent[] RayShoot(IEnumerable<GeometryBase> geometry, Ray3d ray, int maxReflections, bool honorTrims)
    {
      if (null == geometry) throw new ArgumentNullException(nameof(geometry));
      if (maxReflections < 1 || maxReflections > 1000)
        throw new ArgumentOutOfRangeException("maxReflections", "maxReflections must be between 1-1000");

      using (var in_geom = new SimpleArrayGeometryPointer(geometry))
      using (var out_geom_idx = new SimpleArrayInt())
      using (var out_face_idx = new SimpleArrayInt())
      using (var out_points = new SimpleArrayPoint3d())
      {
        var ptr_const_geom = in_geom.ConstPointer();
        var ptr_out_geom_idx = out_geom_idx.NonConstPointer();
        var ptr_out_face_idx = out_face_idx.NonConstPointer();
        var ptr_out_points = out_points.NonConstPointer();
        var count = UnsafeNativeMethods.ON_RayShooter_ShootRay(ptr_const_geom, ray.Position, ray.Direction, maxReflections, ptr_out_points, ptr_out_geom_idx, ptr_out_face_idx, honorTrims);
        if (count > 0)
        {
          var points = out_points.ToArray();
          var geom_idx = out_geom_idx.ToArray();
          var face_idx = out_face_idx.ToArray();
          if (count == points.Length && 
            count == geom_idx.Length && 
            count == face_idx.Length
            )
          {
            var rc = new RayShootEvent[count];
            for (var i = 0; i < count; i++)
            {
              rc[i] = new RayShootEvent
              {
                Point = points[i],
                GeometryIndex = geom_idx[i],
                BrepFaceIndex = face_idx[i]
              };
            }
            return rc;
          }
        }
      }
      return new RayShootEvent[0];
    }

#endif

    #endregion

#if RHINO_SDK
    /// <summary>
    /// Projects points onto meshes.
    /// </summary>
    /// <param name="meshes">the meshes to project on to.</param>
    /// <param name="points">the points to project.</param>
    /// <param name="direction">the direction to project.</param>
    /// <param name="tolerance">
    /// Projection tolerances used for culling close points and for line-mesh intersection.
    /// </param>
    /// <returns>
    /// Array of projected points, or null in case of any error or invalid input.
    /// </returns>
    /// <since>5.0</since>
    public static Point3d[] ProjectPointsToMeshes(IEnumerable<Mesh> meshes, IEnumerable<Point3d> points, Vector3d direction, double tolerance)
    {
      Point3d[] rc = null;
      if (meshes != null && points != null)
      {
        using (var mesh_array = new Runtime.InteropWrappers.SimpleArrayMeshPointer())
        {
          foreach (Mesh mesh in meshes)
            mesh_array.Add(mesh, true);

          Rhino.Collections.Point3dList inputpoints = new Rhino.Collections.Point3dList(points);
          if (inputpoints.Count > 0)
          {
            IntPtr const_ptr_mesh_array = mesh_array.ConstPointer();

            using (Runtime.InteropWrappers.SimpleArrayPoint3d output = new Runtime.InteropWrappers.SimpleArrayPoint3d())
            {
              IntPtr ptr_output = output.NonConstPointer();
              if (UnsafeNativeMethods.RHC_RhinoProjectPointsToMeshes(const_ptr_mesh_array, direction, tolerance, inputpoints.Count, inputpoints.m_items, ptr_output, IntPtr.Zero))
                rc = output.ToArray();
            }
          }
        }
      }
      GC.KeepAlive(meshes);

      return rc;
    }

    /// <summary>
    /// Projects points onto meshes.
    /// </summary>
    /// <param name="meshes">the meshes to project on to.</param>
    /// <param name="points">the points to project.</param>
    /// <param name="direction">the direction to project.</param>
    /// <param name="tolerance">
    /// Projection tolerances used for culling close points and for line-mesh intersection.
    /// </param>
    /// <param name="indices">Return points[i] is a projection of points[indices[i]]</param>
    /// <returns>
    /// Array of projected points, or null in case of any error or invalid input.
    /// </returns>
    /// <example>
    /// <code source='examples\vbnet\ex_projectpointstomeshesex.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_projectpointstomeshesex.cs' lang='cs'/>
    /// <code source='examples\py\ex_projectpointstomeshesex.py' lang='py'/>
    /// </example>
    /// <since>5.10</since>
    public static Point3d[] ProjectPointsToMeshesEx(IEnumerable<Mesh> meshes, IEnumerable<Point3d> points, Vector3d direction, double tolerance, out int[] indices)
    {
      Point3d[] rc = null;
      indices = new int[0];
      if (meshes != null && points != null)
      {
        using (var mesh_array = new Runtime.InteropWrappers.SimpleArrayMeshPointer())
        {
          foreach (Mesh mesh in meshes)
            mesh_array.Add(mesh, true);

          Rhino.Collections.Point3dList inputpoints = new Rhino.Collections.Point3dList(points);
          if (inputpoints.Count > 0)
          {
            IntPtr const_ptr_mesh_array = mesh_array.ConstPointer();

            using (Runtime.InteropWrappers.SimpleArrayPoint3d output = new Runtime.InteropWrappers.SimpleArrayPoint3d())
            using (Runtime.InteropWrappers.SimpleArrayInt output_indices = new Runtime.InteropWrappers.SimpleArrayInt())
            {
              IntPtr ptr_output = output.NonConstPointer();
              IntPtr ptr_indices = output_indices.NonConstPointer();
              if (UnsafeNativeMethods.RHC_RhinoProjectPointsToMeshes(const_ptr_mesh_array, direction, tolerance, inputpoints.Count, inputpoints.m_items, ptr_output, ptr_indices))
              {
                rc = output.ToArray();
                indices = output_indices.ToArray();
              }
            }
          }
        }
      }
      GC.KeepAlive(meshes);
      return rc;
    }


    /// <summary>
    /// Projects points onto breps.
    /// </summary>
    /// <param name="breps">The breps projection targets.</param>
    /// <param name="points">The points to project.</param>
    /// <param name="direction">The direction to project.</param>
    /// <param name="tolerance">The tolerance used for intersections.</param>
    /// <returns>
    /// Array of projected points, or null in case of any error or invalid input.
    /// </returns>
    /// <example>
    /// <code source='examples\vbnet\ex_projectpointstobreps.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_projectpointstobreps.cs' lang='cs'/>
    /// <code source='examples\py\ex_projectpointstobreps.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    public static Point3d[] ProjectPointsToBreps(IEnumerable<Brep> breps, IEnumerable<Point3d> points, Vector3d direction, double tolerance)
    {
      Point3d[] rc = null;
      if (breps != null && points != null)
      {
        using (var brep_array = new Runtime.InteropWrappers.SimpleArrayBrepPointer())
        {
          foreach (Brep brep in breps)
            brep_array.Add(brep, true);

          Rhino.Collections.Point3dList inputpoints = new Rhino.Collections.Point3dList(points);
          if (inputpoints.Count > 0)
          {
            IntPtr const_ptr_brep_array = brep_array.ConstPointer();

            using (Runtime.InteropWrappers.SimpleArrayPoint3d output = new Runtime.InteropWrappers.SimpleArrayPoint3d())
            {
              IntPtr ptr_output_points = output.NonConstPointer();
              if (UnsafeNativeMethods.RHC_RhinoProjectPointsToBreps(const_ptr_brep_array, direction, tolerance, inputpoints.Count, inputpoints.m_items, ptr_output_points, IntPtr.Zero))
                rc = output.ToArray();
            }
          }
        }
      }
      GC.KeepAlive(breps);
      return rc;
    }

    /// <summary>
    /// Projects points onto breps.
    /// </summary>
    /// <param name="breps">The breps projection targets.</param>
    /// <param name="points">The points to project.</param>
    /// <param name="direction">The direction to project.</param>
    /// <param name="tolerance">The tolerance used for intersections.</param>
    /// <param name="indices">Return points[i] is a projection of points[indices[i]]</param>
    /// <returns>
    /// Array of projected points, or null in case of any error or invalid input.
    /// </returns>
    /// <since>5.10</since>
    public static Point3d[] ProjectPointsToBrepsEx(IEnumerable<Brep> breps, IEnumerable<Point3d> points, Vector3d direction, double tolerance, out int[] indices)
    {
      Point3d[] rc = null;
      indices = new int[0];
      if (breps != null && points != null)
      {
        using (var brep_array = new Runtime.InteropWrappers.SimpleArrayBrepPointer())
        {
          foreach (Brep brep in breps)
            brep_array.Add(brep, true);

          Rhino.Collections.Point3dList inputpoints = new Rhino.Collections.Point3dList(points);
          if (inputpoints.Count > 0)
          {
            IntPtr const_ptr_brep_array = brep_array.ConstPointer();

            using (Runtime.InteropWrappers.SimpleArrayPoint3d output = new Runtime.InteropWrappers.SimpleArrayPoint3d())
            using (Runtime.InteropWrappers.SimpleArrayInt output_indices = new Runtime.InteropWrappers.SimpleArrayInt())
            {
              IntPtr ptr_output_points = output.NonConstPointer();
              IntPtr ptr_indices = output_indices.NonConstPointer();
              if (UnsafeNativeMethods.RHC_RhinoProjectPointsToBreps(const_ptr_brep_array, direction, tolerance, inputpoints.Count, inputpoints.m_items, ptr_output_points, ptr_indices))
              {
                rc = output.ToArray();
                indices = output_indices.ToArray();
              }
            }
          }
        }
      }
      GC.KeepAlive(breps);
      return rc;
    }

#endif
  }

#if RHINO_SDK
  /// <summary>
  /// Two meshes that intersect, as reported by
  /// <see cref="Intersection.MeshMeshPredicate(IEnumerable{Mesh}, IEnumerable{Mesh}, double, bool, out IntersectingMeshPair[], FileIO.TextLog, System.Threading.CancellationToken)"/>,
  /// with one face of each that intersect.
  /// </summary>
  /// <since>9.0</since>
  public readonly struct IntersectingMeshPair
  {
    /// <summary>
    /// Initializes a new instance of <see cref="IntersectingMeshPair"/>.
    /// </summary>
    /// <param name="meshIndexA">The index of the first mesh.</param>
    /// <param name="meshIndexB">The index of the second mesh.</param>
    /// <param name="faceIndexA">The index of a face of the first mesh.</param>
    /// <param name="faceIndexB">The index of a face of the second mesh.</param>
    /// <since>9.0</since>
    public IntersectingMeshPair(int meshIndexA, int meshIndexB, int faceIndexA, int faceIndexB)
    {
      MeshIndexA = meshIndexA;
      MeshIndexB = meshIndexB;
      FaceIndexA = faceIndexA;
      FaceIndexB = faceIndexB;
    }

    /// <summary>
    /// The index of the first mesh, in meshesA.
    /// </summary>
    /// <since>9.0</since>
    public int MeshIndexA { get; }

    /// <summary>
    /// The index of the second mesh, in meshesB, or in meshesA when meshesB is null.
    /// </summary>
    /// <since>9.0</since>
    public int MeshIndexB { get; }

    /// <summary>
    /// The index of a face of the first mesh that intersects <see cref="FaceIndexB"/>, or -1 if it could not be determined.
    /// </summary>
    /// <since>9.0</since>
    public int FaceIndexA { get; }

    /// <summary>
    /// The index of a face of the second mesh that intersects <see cref="FaceIndexA"/>, or -1 if it could not be determined.
    /// </summary>
    /// <since>9.0</since>
    public int FaceIndexB { get; }

    /// <summary>
    /// Deconstructs this pair, so that it can be written as <c>var (meshIndexA, meshIndexB, _, _) = pair;</c>.
    /// </summary>
    /// <param name="meshIndexA">Receives <see cref="MeshIndexA"/>.</param>
    /// <param name="meshIndexB">Receives <see cref="MeshIndexB"/>.</param>
    /// <param name="faceIndexA">Receives <see cref="FaceIndexA"/>.</param>
    /// <param name="faceIndexB">Receives <see cref="FaceIndexB"/>.</param>
    /// <since>9.0</since>
    public void Deconstruct(out int meshIndexA, out int meshIndexB, out int faceIndexA, out int faceIndexB)
    {
      meshIndexA = MeshIndexA;
      meshIndexB = MeshIndexB;
      faceIndexA = FaceIndexA;
      faceIndexB = FaceIndexB;
    }
  }
#endif

  /// <summary>
  /// Represents all possible cases of a Plane|Circle intersection event.
  /// </summary>
  /// <since>5.0</since>
  public enum PlaneCircleIntersection : int
  {
    /// <summary>
    /// No intersections. Either because radius is too small or because circle plane is parallel but not coincident with the intersection plane.
    /// </summary>
    None = 0,

    /// <summary>
    /// Tangent (one point) intersection.
    /// </summary>
    Tangent = 1,

    /// <summary>
    /// Secant (two point) intersection.
    /// </summary>
    Secant = 2,

    /// <summary>
    /// Circle and plane are planar but not coincident. 
    /// Parallel indicates no intersection took place.
    /// </summary>
    Parallel = 3,

    /// <summary>
    /// Circle and plane are co-planar, they intersect everywhere.
    /// </summary>
    Coincident = 4
  }

  /// <summary>
  /// Represents all possible cases of a Plane|Sphere intersection event.
  /// </summary>
  /// <since>5.0</since>
  public enum PlaneSphereIntersection : int
  {
    /// <summary>
    /// No intersections.
    /// </summary>
    None = 0,

    /// <summary>
    /// Tangent intersection.
    /// </summary>
    Point = 1,

    /// <summary>
    /// Circular intersection.
    /// </summary>
    Circle = 2,
  }

  /// <summary>
  /// Represents all possible cases of a Line|Circle intersection event.
  /// </summary>
  /// <since>5.0</since>
  public enum LineCircleIntersection : int
  {
    /// <summary>
    /// No intersections.
    /// </summary>
    None = 0,

    /// <summary>
    /// One intersection.
    /// </summary>
    Single = 1,

    /// <summary>
    /// Two intersections.
    /// </summary>
    Multiple = 2,
  }

  /// <summary>
  /// Represents all possible cases of a Line|Sphere intersection event.
  /// </summary>
  /// <since>5.0</since>
  public enum LineSphereIntersection : int
  {
    /// <summary>
    /// No intersections.
    /// </summary>
    None = 0,

    /// <summary>
    /// One intersection.
    /// </summary>
    Single = 1,

    /// <summary>
    /// Two intersections.
    /// </summary>
    Multiple = 2,
  }

  /// <summary>
  /// Represents all possible cases of a Line|Cylinder intersection event.
  /// </summary>
  /// <since>5.0</since>
  public enum LineCylinderIntersection : int
  {
    /// <summary>
    /// No intersections.
    /// </summary>
    None = 0,

    /// <summary>
    /// One intersection.
    /// </summary>
    Single = 1,

    /// <summary>
    /// Two intersections.
    /// </summary>
    Multiple = 2,

    /// <summary>
    /// Line lies on cylinder.
    /// </summary>
    Overlap = 3
  }

  /// <summary>
  /// Represents all possible cases of a Sphere|Sphere intersection event.
  /// </summary>
  /// <since>5.0</since>
  public enum SphereSphereIntersection : int
  {
    /// <summary>
    /// Spheres do not intersect.
    /// </summary>
    None = 0,

    /// <summary>
    /// Spheres touch at a single point.
    /// </summary>
    Point = 1,

    /// <summary>
    /// Spheres intersect at a circle.
    /// </summary>
    Circle = 2,

    /// <summary>
    /// Spheres are identical.
    /// </summary>
    Overlap = 3
  }

  /// <summary>
  /// Represents all possible cases of a Arc|Arc intersection event.
  /// </summary>
  /// <since>7.12</since>
  public enum ArcArcIntersection : int
  {
    /// <summary>
    /// Arcs do not intersect.
    /// </summary>
    None = 0,

    /// <summary>
    /// Arcs touch at a one point.
    /// </summary>
    Single = 1,

    /// <summary>
    /// Arcs intersect at two points.
    /// </summary>
    Multiple = 2,

    /// <summary>
    /// Arcs are cocircular and overlap.
    /// </summary>
    Overlap = 3
  }

  /// <summary>
  /// Represents all possible cases of a Circle|Circle intersection event.
  /// </summary>
  /// <since>7.12</since>
  public enum CircleCircleIntersection : int
  {
    /// <summary>
    /// Circles do not intersect.
    /// </summary>
    None = 0,

    /// <summary>
    /// Circles touch at a one point.
    /// </summary>
    Single = 1,

    /// <summary>
    /// Circles intersect at two points.
    /// </summary>
    Multiple = 2,

    /// <summary>
    /// Circles are identical.
    /// </summary>
    Overlap = 3
  }

#if RHINO_SDK
  /// <summary>
  /// Data that <see cref="Intersection.GeometryPlane(GeometryBase, Plane, double, bool, bool, bool, RhinoDoc, Transform, PlaneIntersectionCache, System.Threading.CancellationToken, out Curve[], out Point3d[])"/>
  /// computes from the geometry alone, kept between calls: the mesh intersection data of a mesh, and the Brep
  /// a SubD, Extrusion, Surface or BrepFace converts to. Reuse one cache for many planes through the same geometry,
  /// such as a contour stack.
  /// </summary>
  /// <remarks>
  /// Entries are keyed by the native geometry. The cache keeps each geometry it is used with alive until
  /// <see cref="Clear"/> or <see cref="Dispose()"/>, so a new object cannot take over the entry of a collected one.
  /// Call <see cref="Clear"/> when a geometry used with the cache, or an instance definition it references,
  /// is changed in place; not while the cache is in use.
  /// One cache can be used from several threads at the same time.
  /// </remarks>
  public sealed class PlaneIntersectionCache : IDisposable
  {
    IntPtr m_ptr; // CRhinoPlaneIntersectionCache*

    // Keeps the geometry the native entries are keyed by alive. By reference, not by Equals.
    readonly HashSet<GeometryBase> m_geometry = new HashSet<GeometryBase>(new ReferenceComparer());

    sealed class ReferenceComparer : IEqualityComparer<GeometryBase>
    {
      public bool Equals(GeometryBase a, GeometryBase b) => ReferenceEquals(a, b);
      public int GetHashCode(GeometryBase g) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(g);
    }

    /// <summary>
    /// Creates an empty cache.
    /// </summary>
    /// <since>9.0</since>
    public PlaneIntersectionCache()
    {
      m_ptr = UnsafeNativeMethods.CRhinoPlaneIntersectionCache_New();
    }

    /// <summary>
    /// Releases the native cache.
    /// </summary>
    ~PlaneIntersectionCache()
    {
      Dispose(false);
    }

    /// <summary>
    /// Releases the native cache.
    /// </summary>
    /// <since>9.0</since>
    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    void Dispose(bool disposing)
    {
      if (IntPtr.Zero != m_ptr)
        UnsafeNativeMethods.CRhinoPlaneIntersectionCache_Delete(m_ptr);
      m_ptr = IntPtr.Zero;
      if (disposing)
      {
        lock (m_geometry)
          m_geometry.Clear();
      }
    }

    /// <summary>
    /// Removes all entries, and releases the geometry the cache kept alive.
    /// </summary>
    /// <since>9.0</since>
    public void Clear()
    {
      UnsafeNativeMethods.CRhinoPlaneIntersectionCache_Clear(m_ptr);
      lock (m_geometry)
        m_geometry.Clear();
      GC.KeepAlive(this);
    }

    internal IntPtr NonConstPointer()
    {
      if (IntPtr.Zero == m_ptr)
        throw new ObjectDisposedException(nameof(PlaneIntersectionCache));
      return m_ptr;
    }

    internal void KeepAlive(GeometryBase geometry)
    {
      lock (m_geometry)
        m_geometry.Add(geometry);
    }
  }
#endif
}
