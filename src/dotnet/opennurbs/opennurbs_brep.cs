using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using Rhino.Runtime.InteropWrappers;
using Rhino.Runtime;

namespace Rhino.Geometry
{
  /// <summary>Defines styles used for creating Brep pipes.</summary>
  public enum PipeCapMode
  {
    /// <summary>No cap.</summary>
    None = 0,
    /// <summary>Caps with planar surface.</summary>
    Flat = 1,
    /// <summary>Caps with hemispherical surface.</summary>
    Round = 2
  }

  /// <summary>
  /// Specifies enumerated constants for all supported loft types.
  /// </summary>
  public enum LoftType
  {
    /// <summary>
    /// Uses chord-length parameterization in the loft direction.
    /// </summary>
    Normal = 0,
    /// <summary>
    /// The surface is allowed to move away from the original curves to make a smoother surface.
    /// The surface control points are created at the same locations as the control points
    /// of the loft input curves.
    /// </summary>
    Loose = 1,
    /// <summary>
    /// The surface sticks closely to the original curves. Uses square root of chord-length
    /// parameterization in the loft direction.
    /// </summary>
    Tight = 2,
    /// <summary>
    /// The sections between the curves are straight. This is also known as a ruled surface.
    /// </summary>
    Straight = 3,
    /// <summary>
    /// Obsolete, do not use.
    /// </summary>
    [Obsolete]
    Developable = 4,
    /// <summary>
    /// Constructs a uniform loft. The object knot vectors will be uniform.
    /// </summary>
    Uniform = 5
  }

  /// <summary>
  /// Corner types used for creating a tapered extrusion
  /// </summary>
  public enum ExtrudeCornerType
  {
    /// <summary>No Corner</summary>
    None = 0,
    /// <summary></summary>
    Sharp = 1,
    /// <summary></summary>
    Round = 2,
    /// <summary></summary>
    Smooth = 3,
    /// <summary></summary>
    Chamfer = 4
  }

  /// <summary>
  /// Blend types used for creating filleted Brep edges
  /// </summary>
  public enum BlendType
  {
    /// <summary>
    /// Creates a ruled surface between brep edges with varying chamfer distances.
    /// </summary>
    Chamfer = 0,
    /// <summary>
    /// Creates a tangent surface between brep edges with varying radius values.
    /// </summary>
    Fillet = 1,
    /// <summary>
    /// Creates a curvature-continuous blend surface between brep edges with varying radius values.
    /// </summary>
    Blend = 2
  }

  /// <summary>
  /// Rail types used for creating filleted Brep edges
  /// </summary>
  public enum RailType
  {
    /// <summary>
    /// The distance from the edge curves determines the intersection.
    /// </summary>
    DistanceFromEdge = 0,
    /// <summary>
    /// The radius of a rolling ball determines the intersection.
    /// </summary>
    RollingBall = 1,
    /// <summary>
    /// The distance between the edge rails determines the intersection.
    /// </summary>
    DistanceBetweenRails = 2
  }

  /// <summary>
  /// Rebuild types for creating swept surfaces
  /// </summary>
  public enum SweepRebuild
  {
    /// <summary>
    /// Do not simplify cross section curves.
    /// </summary>
    None = 0,
    /// <summary>
    /// Rebuild cross section curves through points.
    /// </summary>
    Rebuild = 1,
    /// <summary>
    /// Refit cross section curves to tolerance.
    /// </summary>
    Refit = 2
  }

  /// <summary>
  /// Boundary Representation. A surface or polysurface along with trim curve information.
  /// </summary>
  [Serializable]
  public class Brep : GeometryBase
  {

    #region statics
    /// <summary>
    /// Attempts to convert a generic Geometry object into a Brep.
    /// </summary>
    /// <param name="geometry">Geometry to convert, not all types of GeometryBase can be represented by BReps.</param>
    /// <returns>Brep if a brep form could be created or null if this is not possible. If geometry was of type Brep to 
    /// begin with, the same object is returned, i.e. it is not duplicated.</returns>
    public static Brep TryConvertBrep(GeometryBase geometry)
    {
      if (null == geometry)
        return null;

      var brep = geometry as Brep;
      // special case for Breps in an attempt to not be creating copies willy-nilly
      if (brep != null && brep.IsDocumentControlled)
      {
        return brep;
      }

      IntPtr ptr = geometry.ConstPointer();
      IntPtr ptr_newbrep = UnsafeNativeMethods.ON_Geometry_BrepForm(ptr);
      GC.KeepAlive(geometry);
      return IntPtr.Zero == ptr ? null : new Brep(ptr_newbrep, null);
    }

#if RHINO_SDK

    /// <summary>
    /// Change the seam of a closed trimmed surface.
    /// </summary>
    /// <param name="face">A Brep face with a closed underlying surface.</param>
    /// <param name="direction">The parameter direction (0 = U, 1 = V). The face's underlying surface must be closed in this direction.</param>
    /// <param name="parameter">The parameter at which to place the seam.</param>
    /// <param name="tolerance">Tolerance used to cut up surface.</param>
    /// <returns>A new Brep that has the same geoemtry as the face with a relocated seam if successful, or null on failure.</returns>
    public static Brep ChangeSeam(BrepFace face, int direction, double parameter, double tolerance)
    {
      IntPtr ptr_const_face = face.ConstPointer();
      IntPtr ptr = UnsafeNativeMethods.RHC_RhinoChangeSeam(ptr_const_face, direction, parameter, tolerance);
      GC.KeepAlive(face);
      return IntPtr.Zero == ptr ? null : new Brep(ptr, null);
    }

    /// <summary>
    /// Copy all trims from a Brep face onto a surface.
    /// </summary>
    /// <param name="trimSource">Brep face which defines the trimming curves.</param>
    /// <param name="surfaceSource">The surface to trim.</param>
    /// <param name="tolerance">Tolerance to use for rebuilding 3D trim curves.</param>
    /// <returns>A brep with the shape of surfaceSource and the trims of trimSource or null on failure.</returns>
    public static Brep CopyTrimCurves(BrepFace trimSource, Surface surfaceSource, double tolerance)
    {
      IntPtr ptr_const_face = trimSource.ConstPointer();
      IntPtr ptr_const_surface = surfaceSource.ConstPointer();

      IntPtr ptr = UnsafeNativeMethods.ON_Brep_CopyTrims(ptr_const_face, ptr_const_surface, tolerance);
      Runtime.CommonObject.GcProtect(trimSource, surfaceSource);
      return IntPtr.Zero == ptr ? null : new Brep(ptr, null);
    }

    /// <summary>
    /// Creates a brep representation of the sphere with two similar trimmed NURBS surfaces, and no singularities.
    /// </summary>
    /// <param name="center">The center of the sphere.</param>
    /// <param name="radius">The radius of the sphere.</param>
    /// <param name="tolerance">
    /// Used in computing 2d trimming curves. If &gt;= 0.0, then the max of 
    /// ON_0.0001 * radius and RhinoMath.ZeroTolerance will be used.
    /// </param>
    /// <returns>A new brep, or null on error.</returns>
    public static Brep CreateBaseballSphere(Point3d center, double radius, double tolerance)
    {
      IntPtr ptr = UnsafeNativeMethods.RHC_RhinoBrepBaseballSphere(center, radius, tolerance);
      return IntPtr.Zero == ptr ? null : new Brep(ptr, null);
    }

    /// <summary>
    /// Creates a single developable surface between two curves.
    /// </summary>
    /// <param name="crv0">The first rail curve.</param>
    /// <param name="crv1">The second rail curve.</param>
    /// <param name="reverse0">Reverse the first rail curve.</param>
    /// <param name="reverse1">Reverse the second rail curve</param>
    /// <param name="density">The number of rulings across the surface.</param>
    /// <returns>The output Breps if successful, otherwise an empty array.</returns>
    public static Brep[] CreateDevelopableLoft(Curve crv0, Curve crv1, bool reverse0, bool reverse1, int density)
    {
      if (null == crv0) throw new ArgumentNullException(nameof(crv0));
      if (null == crv1) throw new ArgumentNullException(nameof(crv1));

      var rc = new Brep[0];

      var rail0 = crv0.ToNurbsCurve();
      var rail1 = crv1.ToNurbsCurve();
      if (null == rail0 || null == rail1)
        return rc;

      if (reverse0) rail0.Reverse();
      if (reverse1) rail1.Reverse();

      if (density < 2)
      {
        // A made-up guess at how to get a reasonable density
        var guess = rail0.Points.Count > rail1.Points.Count
          ? rail0.Points.Count / rail0.Order + 3
          : rail1.Points.Count / rail1.Order + 3;
        density = guess;
      }

      var params0 = rail0.DivideByCount(density, true);
      var params1 = rail1.DivideByCount(density, true);
      if (params0.Length != params1.Length)
        return rc;

      var rulings = new List<Point2d>();
      for (var i = 0; i < params0.Length; i++)
        rulings.Add(new Point2d(params0[i], params1[i]));

      IEnumerable<Point2d> temp = rulings;
      if (!DevelopableSrf.UntwistRulings(rail0, rail1, ref temp))
        return rc;

      rulings = temp.ToList();

      var shapes = new List<Curve>();
      for (var i = 0; i < rulings.Count; i++)
      {
        var pt0 = rail0.PointAt(rulings[i].X);
        var pt1 = rail1.PointAt(rulings[i].Y);
        shapes.Add(new LineCurve(pt0, pt1));
      }

      return Brep.CreateFromSweep(rail0, rail1, shapes, rail0.IsClosed && rail1.IsClosed, 0.01);
    }

    /// <summary>
    /// Creates a single developable surface between two curves.
    /// </summary>
    /// <param name="rail0">The first rail curve.</param>
    /// <param name="rail1">The second rail curve.</param>
    /// <param name="fixedRulings">
    /// Rulings define lines across the surface that define the straight sections on the developable surface,
    /// where rulings[i].X = parameter on first rail curve, and rulings[i].Y = parameter on second rail curve.
    /// Note, rulings will be automatically adjusted to minimum twist.
    /// </param>
    /// <returns>The output Breps if successful, otherwise an empty array.</returns>
    public static Brep[] CreateDevelopableLoft(NurbsCurve rail0, NurbsCurve rail1, IEnumerable<Point2d> fixedRulings)
    {
      if (null == rail0) throw new ArgumentNullException(nameof(rail0));
      if (null == rail1) throw new ArgumentNullException(nameof(rail1));
      if (null == fixedRulings) throw new ArgumentNullException(nameof(fixedRulings));

      var rc = new Brep[0];

      var rulings = fixedRulings.ToList();
      if (rulings.Count < 3)
        return rc;

      IEnumerable<Point2d> temp = rulings;
      if (!DevelopableSrf.UntwistRulings(rail0, rail1, ref temp))
        return rc;

      rulings = temp.ToList();

      var shapes = new List<Curve>();
      for (var i = 0; i < rulings.Count; i++)
      {
        var pt0 = rail0.PointAt(rulings[i].X);
        var pt1 = rail1.PointAt(rulings[i].Y);
        shapes.Add(new LineCurve(pt0, pt1));
      }

      return Brep.CreateFromSweep(rail0, rail1, shapes, rail0.IsClosed && rail1.IsClosed, 0.01);
    }

#endif

    /// <summary>
    /// Create a brep representation of a mesh
    /// </summary>
    /// <param name="mesh"></param>
    /// <param name="trimmedTriangles">
    /// if true, triangles in the mesh will be represented by trimmed planes in
    /// the brep. If false, triangles in the mesh will be represented by
    /// untrimmed singular bilinear NURBS surfaces in the brep.
    /// </param>
    /// <returns></returns>
    public static Brep CreateFromMesh(Mesh mesh, bool trimmedTriangles)
    {
      IntPtr ptr_const_mesh = mesh.ConstPointer();
      IntPtr ptr_newbrep = UnsafeNativeMethods.ONC_BrepFromMesh(ptr_const_mesh, trimmedTriangles);
      GC.KeepAlive(mesh);
      return CreateGeometryHelper(ptr_newbrep, null) as Brep;
    }

    /// <summary>
    /// Constructs new brep that matches a bounding box.
    /// </summary>
    /// <param name="box">A box to use for creation.</param>
    /// <returns>A new brep; or null on failure.</returns>
    /// <example>
    /// <code source='examples\vbnet\ex_curvebrepbox.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_curvebrepbox.cs' lang='cs'/>
    /// <code source='examples\py\ex_curvebrepbox.py' lang='py'/>
    /// </example>
    public static Brep CreateFromBox(BoundingBox box)
    {
      IntPtr ptr = UnsafeNativeMethods.ON_Brep_FromBox(box.Min, box.Max);
      return IntPtr.Zero == ptr ? null : new Brep(ptr, null);
    }

    /// <summary>
    /// Constructs new brep that matches an aligned box.
    /// </summary>
    /// <param name="box">Box to match.</param>
    /// <returns>A Brep with 6 faces that is similar to the Box.</returns>
    public static Brep CreateFromBox(Box box)
    {
      return CreateFromBox(box.GetCorners());
    }
    /// <summary>
    /// Constructs new brep from 8 corner points.
    /// </summary>
    /// <param name="corners">
    /// 8 points defining the box corners arranged as the vN labels indicate.
    /// <pre>
    /// <para>v7_______e6____v6</para>
    /// <para>|\             |\</para>
    /// <para>| e7           | e5</para>
    /// <para>|  \ ______e4_____\</para>
    /// <para>e11 v4         |   v5</para>
    /// <para>|   |        e10   |</para>
    /// <para>|   |          |   |</para>
    /// <para>v3--|---e2----v2   e9</para>
    /// <para> \  e8          \  |</para>
    /// <para> e3 |            e1|</para>
    /// <para>   \|             \|</para>
    /// <para>    v0_____e0______v1</para>
    /// </pre>
    /// </param>
    /// <returns>A new brep, or null on error.</returns>
    public static Brep CreateFromBox(IEnumerable<Point3d> corners)
    {
      var box_corners = new Point3d[8];
      if (corners == null) { return null; }

      int i = 0;
      foreach (Point3d p in corners)
      {
        box_corners[i] = p;
        i++;
        if (8 == i) { break; }
      }

      if (i < 8) { return null; }

      IntPtr ptr = UnsafeNativeMethods.ON_Brep_FromBox2(box_corners);
      return IntPtr.Zero == ptr ? null : new Brep(ptr, null);
    }

    /// <summary>
    /// Constructs a Brep definition of a cylinder.
    /// </summary>
    /// <param name="cylinder">cylinder.IsFinite() must be true.</param>
    /// <param name="capBottom">if true end at cylinder.m_height[0] should be capped.</param>
    /// <param name="capTop">if true end at cylinder.m_height[1] should be capped.</param>
    /// <returns>
    /// A Brep representation of the cylinder with a single face for the cylinder,
    /// an edge along the cylinder seam, and vertices at the bottom and top ends of this
    /// seam edge. The optional bottom/top caps are single faces with one circular edge
    /// starting and ending at the bottom/top vertex.
    /// </returns>
    public static Brep CreateFromCylinder(Cylinder cylinder, bool capBottom, bool capTop)
    {
      IntPtr ptr = UnsafeNativeMethods.ON_Brep_FromCylinder(ref cylinder, capBottom, capTop);
      return IntPtr.Zero == ptr ? null : new Brep(ptr, null);
    }

    /// <summary>
    /// Constructs a Brep definition of a sphere.
    /// </summary>
    /// <param name="sphere">The input sphere provides the orienting plane and radius.</param>
    /// <returns>A Brep if successful, null on error.</returns>
    public static Brep CreateFromSphere(Sphere sphere)
    {
      IntPtr ptr = UnsafeNativeMethods.ON_Brep_FromSphere(ref sphere);
      return IntPtr.Zero == ptr ? null : new Brep(ptr, null);
    }

    /// <summary>
    /// Constructs a Brep definition of a quad sphere.
    /// </summary>
    /// <param name="sphere">The input sphere provides the orienting plane and radius.</param>
    /// <returns>A Brep if successful, null on error.</returns>
    public static Brep CreateQuadSphere(Sphere sphere)
    {
      IntPtr ptr = UnsafeNativeMethods.ON_Brep_CreateQuadSphere(ref sphere);
      return IntPtr.Zero == ptr ? null : new Brep(ptr, null);
    }

    /// <summary>
    /// Constructs a Brep representation of the cone with a single
    /// face for the cone, an edge along the cone seam, 
    /// and vertices at the base and apex ends of this seam edge.
    /// The optional cap is a single face with one circular edge 
    /// starting and ending at the base vertex.
    /// </summary>
    /// <param name="cone">A cone value.</param>
    /// <param name="capBottom">if true the base of the cone should be capped.</param>
    /// <returns>A Brep if successful, null on error.</returns>
    public static Brep CreateFromCone(Cone cone, bool capBottom)
    {
      IntPtr ptr = UnsafeNativeMethods.ONC_ON_BrepCone(ref cone, capBottom);
      return IntPtr.Zero == ptr ? null : new Brep(ptr, null);
    }

    /// <summary>
    /// Constructs a brep form of a surface of revolution.
    /// </summary>
    /// <param name="surface">The surface of revolution.</param>
    /// <param name="capStart">
    /// if true, the start of the revolute is not on the axis of revolution,
    /// and the surface of revolution is closed, then a circular cap will be
    /// added to close of the hole at the start of the revolute.
    /// </param>
    /// <param name="capEnd">
    /// if true, the end of the revolute is not on the axis of revolution,
    /// and the surface of revolution is closed, then a circular cap will be
    /// added to close of the hole at the end of the revolute.
    /// </param>
    /// <returns>A Brep if successful, null on error.</returns>
    /// <example>
    /// <code source='examples\vbnet\ex_addtruncatedcone.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_addtruncatedcone.cs' lang='cs'/>
    /// <code source='examples\py\ex_addtruncatedcone.py' lang='py'/>
    /// </example>
    public static Brep CreateFromRevSurface(RevSurface surface, bool capStart, bool capEnd)
    {
      IntPtr ptr_const_surface = surface.ConstPointer();
      IntPtr ptr = UnsafeNativeMethods.ONC_ON_BrepRevSurface(ptr_const_surface, capStart, capEnd);
      GC.KeepAlive(surface);
      return IntPtr.Zero == ptr ? null : new Brep(ptr, null);
    }

    /// <summary>
    /// Constructs a Brep from a surface. The resulting Brep has an outer boundary made
    /// from four trims. The trims are ordered so that they run along the south, east,
    /// north, and then west side of the surface's parameter space.
    /// </summary>
    /// <param name="surface">A surface to convert.</param>
    /// <returns>Resulting brep or null on failure.</returns>
    public static Brep CreateFromSurface(Surface surface)
    {
      if (null == surface)
        return null;
      IntPtr ptr_const_surface = surface.ConstPointer();
      IntPtr ptr_newbrep = UnsafeNativeMethods.ON_Brep_FromSurface(ptr_const_surface);
      GC.KeepAlive(surface);
      return IntPtr.Zero == ptr_newbrep ? null : new Brep(ptr_newbrep, null);
    }

    /// <summary>
    /// Create a Brep trimmed plane.
    /// </summary>
    /// <param name="plane">Plane that will be trimmed.</param>
    /// <param name="curve">A simple (no self intersections) closed curve that defines the outer boundary of the trimmed plane.</param>
    /// <returns>Resulting brep or null on failure.</returns>
    public static Brep CreateTrimmedPlane(Plane plane, Curve curve)
    {
      return CreateTrimmedPlane(plane, new[] { curve });
    }

    /// <summary>
    /// Create a Brep trimmed plane.
    /// </summary>
    /// <param name="plane">Plane that will be trimmed.</param>
    /// <param name="curves">A list of curves that form a simple (no self intersections) closed curve that defines the outer boundary of the trimmed plane.</param>
    /// <returns>Resulting brep or null on failure.</returns>
    public static Brep CreateTrimmedPlane(Plane plane, IEnumerable<Curve> curves)
    {
      using (var crvs = new SimpleArrayCurvePointer(curves))
      {
        IntPtr ptr_const_curves = crvs.ConstPointer();
        IntPtr ptr_newbrep = UnsafeNativeMethods.ON_Brep_TrimmedPlane(ref plane, ptr_const_curves);
        return IntPtr.Zero == ptr_newbrep ? null : new Brep(ptr_newbrep, null);
      }
    }

#if RHINO_SDK

    /// <summary>
    /// Constructs a set of planar breps as outlines by the loops.
    /// </summary>
    /// <param name="inputLoops">Curve loops that delineate the planar boundaries.</param>
    /// <returns>An array of Planar Breps.</returns>
    [Obsolete("Use version that takes tolerance as input")]
    public static Brep[] CreatePlanarBreps(IEnumerable<Curve> inputLoops)
    {
      double tolerance, angle_tol;
      RhinoDoc.ActiveDocTolerances(out tolerance, out angle_tol);
      return CreatePlanarBreps(inputLoops, tolerance);
    }

    /// <summary>
    /// Constructs a set of planar breps as outlines by the loops.
    /// </summary>
    /// <param name="inputLoops">Curve loops that delineate the planar boundaries.</param>
    /// <param name="tolerance"></param>
    /// <returns>An array of Planar Breps.</returns>
    public static Brep[] CreatePlanarBreps(IEnumerable<Curve> inputLoops, double tolerance)
    {
      if (null == inputLoops)
        return null;
      var crvs = new Rhino.Collections.CurveList(inputLoops);
      return CreatePlanarBreps(crvs, tolerance);
    }

    /// <summary>
    /// Constructs a set of planar breps as outlines by the loops.
    /// </summary>
    /// <param name="inputLoop">A curve that should form the boundaries of the surfaces or polysurfaces.</param>
    /// <returns>An array of Planar Breps.</returns>
    [Obsolete("Use version that takes tolerance as input")]
    public static Brep[] CreatePlanarBreps(Curve inputLoop)
    {
      double tolerance, angle_tol;
      RhinoDoc.ActiveDocTolerances(out tolerance, out angle_tol);
      return CreatePlanarBreps(inputLoop, tolerance);
    }

    /// <summary>
    /// Constructs a set of planar breps as outlines by the loops.
    /// </summary>
    /// <param name="inputLoop">A curve that should form the boundaries of the surfaces or polysurfaces.</param>
    /// <param name="tolerance"></param>
    /// <returns>An array of Planar Breps.</returns>
    public static Brep[] CreatePlanarBreps(Curve inputLoop, double tolerance)
    {
      if (null == inputLoop)
        return null;
      var crvs = new Rhino.Collections.CurveList { inputLoop };
      return CreatePlanarBreps(crvs, tolerance);
    }

    /// <summary>
    /// Constructs a Brep using the trimming information of a brep face and a surface. 
    /// Surface must be roughly the same shape and in the same location as the trimming brep face.
    /// </summary>
    /// <param name="trimSource">BrepFace which contains trimmingSource brep.</param>
    /// <param name="surfaceSource">Surface that trims of BrepFace will be applied to.</param>
    /// <returns>A brep with the shape of surfaceSource and the trims of trimSource or null on failure.</returns>
    [Obsolete("Use version that takes tolerance as input")]
    public static Brep CreateTrimmedSurface(BrepFace trimSource, Surface surfaceSource)
    {
      double tolerance, angle_tol;
      RhinoDoc.ActiveDocTolerances(out tolerance, out angle_tol);
      return CreateTrimmedSurface(trimSource, surfaceSource, tolerance);
    }

    /// <summary>
    /// Constructs a Brep using the trimming information of a brep face and a surface. 
    /// Surface must be roughly the same shape and in the same location as the trimming brep face.
    /// </summary>
    /// <param name="trimSource">BrepFace which contains trimmingSource brep.</param>
    /// <param name="surfaceSource">Surface that trims of BrepFace will be applied to.</param>
    /// <param name="tolerance"></param>
    /// <returns>A brep with the shape of surfaceSource and the trims of trimSource or null on failure.</returns>
    public static Brep CreateTrimmedSurface(BrepFace trimSource, Surface surfaceSource, double tolerance)
    {
      IntPtr ptr_const_brepface = trimSource.ConstPointer();
      IntPtr ptr_const_surface = surfaceSource.ConstPointer();

      IntPtr ptr = UnsafeNativeMethods.RHC_RhinoRetrimSurface(ptr_const_brepface, ptr_const_surface, tolerance);
      Runtime.CommonObject.GcProtect(trimSource, surfaceSource);
      return IntPtr.Zero == ptr ? null : new Brep(ptr, null);
    }

    /// <summary>
    /// Makes a brep with one face.
    /// </summary>
    /// <param name="corner1">A first corner.</param>
    /// <param name="corner2">A second corner.</param>
    /// <param name="corner3">A third corner.</param>
    /// <param name="tolerance">
    /// Minimum edge length without collapsing to a singularity.
    /// </param>
    /// <returns>A boundary representation, or null on error.</returns>
    public static Brep CreateFromCornerPoints(Point3d corner1, Point3d corner2, Point3d corner3, double tolerance)
    {
      Point3d[] points = { corner1, corner2, corner3 };
      IntPtr ptr_newbrep = UnsafeNativeMethods.RHC_RhinoCreate1FaceBrepFromPoints(3, points, tolerance);
      return IntPtr.Zero == ptr_newbrep ? null : new Brep(ptr_newbrep, null);
    }

    /// <summary>
    /// make a Brep with one face.
    /// </summary>
    /// <param name="corner1">A first corner.</param>
    /// <param name="corner2">A second corner.</param>
    /// <param name="corner3">A third corner.</param>
    /// <param name="corner4">A fourth corner.</param>
    /// <param name="tolerance">
    /// Minimum edge length allowed before collapsing the side into a singularity.
    /// </param>
    /// <returns>A boundary representation, or null on error.</returns>
    public static Brep CreateFromCornerPoints(Point3d corner1, Point3d corner2, Point3d corner3, Point3d corner4, double tolerance)
    {
      Point3d[] points = { corner1, corner2, corner3, corner4 };
      IntPtr ptr_newbrep = UnsafeNativeMethods.RHC_RhinoCreate1FaceBrepFromPoints(4, points, tolerance);
      return IntPtr.Zero == ptr_newbrep ? null : new Brep(ptr_newbrep, null);
    }

    /// <summary>
    /// Constructs a coons patch from 2, 3, or 4 curves.
    /// </summary>
    /// <param name="curves">A list, an array or any enumerable set of curves.</param>
    /// <returns>resulting brep or null on failure.</returns>
    /// <example>
    /// <code source='examples\vbnet\ex_edgesrf.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_edgesrf.cs' lang='cs'/>
    /// <code source='examples\py\ex_edgesrf.py' lang='py'/>
    /// </example>
    public static Brep CreateEdgeSurface(IEnumerable<Curve> curves)
    {
      var nurbs_curves = new NurbsCurve[4];
      var ptr_nurbscurves = new IntPtr[4];
      for (int i = 0; i < 4; i++)
        ptr_nurbscurves[i] = IntPtr.Zero;
      int index = 0;
      foreach (Curve crv in curves)
      {
        if (null == crv)
          continue;
        NurbsCurve nc = crv as NurbsCurve ?? crv.ToNurbsCurve();
        if (nc == null)
          continue;
        nurbs_curves[index] = nc; // this forces GC to not collect while we are using the pointers
        ptr_nurbscurves[index] = nc.ConstPointer();
        index++;
        if (index > 3)
          break;
      }
      int count = index;
      if (count < 2 || count > 4)
        return null;
      IntPtr ptr_brep = UnsafeNativeMethods.RHC_RhinoCreateEdgeSrf(ptr_nurbscurves[0], ptr_nurbscurves[1], ptr_nurbscurves[2], ptr_nurbscurves[3]);
      GC.KeepAlive(nurbs_curves);
      return IntPtr.Zero == ptr_brep ? null : new Brep(ptr_brep, null);
    }

    /// <summary>
    /// Constructs a set of planar Breps as outlines by the loops.
    /// </summary>
    /// <param name="inputLoops">Curve loops that delineate the planar boundaries.</param>
    /// <returns>An array of Planar Breps or null on error.</returns>
    [Obsolete("Use version that takes tolerance as input")]
    public static Brep[] CreatePlanarBreps(Rhino.Collections.CurveList inputLoops)
    {
      double tolerance, angle_tolerance;
      RhinoDoc.ActiveDocTolerances(out tolerance, out angle_tolerance);
      return CreatePlanarBreps(inputLoops, tolerance);
    }

    /// <summary>
    /// Constructs a set of planar Breps as outlines by the loops.
    /// </summary>
    /// <param name="inputLoops">Curve loops that delineate the planar boundaries.</param>
    /// <param name="tolerance"></param>
    /// <returns>An array of Planar Breps.</returns>
    public static Brep[] CreatePlanarBreps(Rhino.Collections.CurveList inputLoops, double tolerance)
    {
      if (null == inputLoops)
        return null;
      using (var crvs = new SimpleArrayCurvePointer(inputLoops.m_items))
      using (var breps = new SimpleArrayBrepPointer())
      {
        IntPtr ptr_const_inputloops = crvs.ConstPointer();
        IntPtr ptr_breps = breps.NonConstPointer();
        int brep_count = UnsafeNativeMethods.RHC_RhinoMakePlanarBreps(ptr_const_inputloops, ptr_breps, tolerance);
        GC.KeepAlive(inputLoops);
        return brep_count < 1 ? null : breps.ToNonConstArray();
      }
    }
#endif


#if RHINO_SDK
    /// <summary>
    /// Offsets a face including trim information to create a new brep.
    /// </summary>
    /// <param name="face">the face to offset.</param>
    /// <param name="offsetDistance">An offset distance.</param>
    /// <param name="offsetTolerance">
    ///  Use 0.0 to make a loose offset. Otherwise, the document's absolute tolerance is usually sufficient.
    /// </param>
    /// <param name="bothSides">When true, offset to both sides of the input face.</param>
    /// <param name="createSolid">When true, make a solid object.</param>
    /// <returns>
    /// A new brep if successful. The brep can be disjoint if bothSides is true and createSolid is false,
    /// or if createSolid is true and connecting the offsets with side surfaces fails.
    /// null if unsuccessful.
    /// </returns>
    public static Brep CreateFromOffsetFace(BrepFace face, double offsetDistance, double offsetTolerance, bool bothSides, bool createSolid)
    {
      IntPtr ptr_const_face = face.ConstPointer();
      IntPtr ptr_newbrep = UnsafeNativeMethods.RHC_RhinoOffsetSurface(ptr_const_face, offsetDistance, offsetTolerance, bothSides, createSolid);
      GC.KeepAlive(face);
      return IntPtr.Zero == ptr_newbrep ? null : new Brep(ptr_newbrep, null);
    }

    /// <summary>
    /// Constructs closed polysurfaces from surfaces and polysurfaces that bound a region in space.
    /// </summary>
    /// <param name="breps">
    /// The intersecting surfaces and polysurfaces to automatically trim and join into closed polysurfaces.
    /// </param>
    /// <param name="tolerance">
    /// The trim and join tolerance. If set to RhinoMath.UnsetValue, Rhino's global absolute tolerance is used.
    /// </param>
    /// <returns>The resulting polysurfaces on success or null on failure.</returns>
    public static Brep[] CreateSolid(IEnumerable<Brep> breps, double tolerance)
    {
      using (var inbreps = new SimpleArrayBrepPointer())
      {
        foreach (Brep b in breps)
          inbreps.Add(b, true);
        using (var outbreps = new SimpleArrayBrepPointer())
        {
          IntPtr const_ptr_inbreps = inbreps.ConstPointer();
          IntPtr ptr_outbreps = outbreps.NonConstPointer();
          UnsafeNativeMethods.RHC_RhinoCreateSolid(const_ptr_inbreps, ptr_outbreps, tolerance);
          GC.KeepAlive(breps);
          return outbreps.ToNonConstArray();
        }
      }
    }

    /// <summary>
    /// Merges two surfaces into one surface at untrimmed edges.
    /// </summary>
    /// <param name="surface0">The first surface to merge.</param>
    /// <param name="surface1">The second surface to merge.</param>
    /// <param name="tolerance">Surface edges must be within this tolerance for the two surfaces to merge.</param>
    /// <param name="angleToleranceRadians">Edge must be within this angle tolerance in order for contiguous edges to be combined into a single edge.</param>
    /// <returns>The merged surfaces as a Brep if successful, null if not successful.</returns>
    public static Brep MergeSurfaces(Surface surface0, Surface surface1, double tolerance, double angleToleranceRadians)
    {
      if (null == surface0 || null == surface1)
        return null;

      var brep0 = Brep.CreateFromSurface(surface0);
      var brep1 = Brep.CreateFromSurface(surface1);
      if (null == brep0 || null == brep1)
        return null;

      return MergeSurfaces(brep0, brep1, tolerance, angleToleranceRadians, Point2d.Unset, Point2d.Unset, 1.0, true);
    }

    /// <summary>
    /// Merges two surfaces into one surface at untrimmed edges. Both surfaces must be untrimmed and share an edge.
    /// </summary>
    /// <param name="brep0">The first single-face Brep to merge.</param>
    /// <param name="brep1">The second single-face Brep to merge.</param>
    /// <param name="tolerance">Surface edges must be within this tolerance for the two surfaces to merge.</param>
    /// <param name="angleToleranceRadians">Edge must be within this angle tolerance in order for contiguous edges to be combined into a single edge.</param>
    /// <returns>The merged Brep if successful, null if not successful.</returns>
    public static Brep MergeSurfaces(Brep brep0, Brep brep1, double tolerance, double angleToleranceRadians)
    {
      if (null == brep0 || null == brep1)
        return null;

      return MergeSurfaces(brep0, brep1, tolerance, angleToleranceRadians, Point2d.Unset, Point2d.Unset, 1.0, true);
    }

    /// <summary>
    /// Merges two surfaces into one surface at untrimmed edges. Both surfaces must be untrimmed and share an edge.
    /// </summary>
    /// <param name="brep0">The first single-face Brep to merge.</param>
    /// <param name="brep1">The second single-face Brep to merge.</param>
    /// <param name="tolerance">Surface edges must be within this tolerance for the two surfaces to merge.</param>
    /// <param name="angleToleranceRadians">Edge must be within this angle tolerance in order for contiguous edges to be combined into a single edge.</param>
    /// <param name="point0">2D pick point on the first single-face Brep. The value can be unset.</param>
    /// <param name="point1">2D pick point on the second single-face Brep. The value can be unset.</param>
    /// <param name="roundness">Defines the roundness of the merge. Acceptable values are between 0.0 (sharp) and 1.0 (smooth).</param>
    /// <param name="smooth">The surface will be smooth. This makes the surface behave better for control point editing, but may alter the shape of both surfaces.</param>
    /// <returns>The merged Brep if successful, null if not successful.</returns>
    public static Brep MergeSurfaces(Brep brep0, Brep brep1, double tolerance, double angleToleranceRadians, Point2d point0, Point2d point1, double roundness, bool smooth)
    {
      if (null == brep0 || null == brep1)
        return null;

      IntPtr const_ptr_brep0 = brep0.ConstPointer();
      IntPtr const_ptr_brep1 = brep1.ConstPointer();
      IntPtr ptr_brep = UnsafeNativeMethods.RHC_RhinoMergeSrf(const_ptr_brep0, const_ptr_brep1, tolerance, angleToleranceRadians, point0, point1, roundness, smooth);
      Runtime.CommonObject.GcProtect(brep0, brep1);
      return IntPtr.Zero == ptr_brep ? null : new Brep(ptr_brep, null);
    }

    /// <summary>
    /// Constructs a brep patch.
    /// <para>This is the simple version of fit that uses a specified starting surface.</para>
    /// </summary>
    /// <param name="geometry">
    /// Combination of Curves, BrepTrims, Points, PointClouds or Meshes.
    /// Curves and trims are sampled to get points. Trims are sampled for
    /// points and normals.
    /// </param>
    /// <param name="startingSurface">A starting surface (can be null).</param>
    /// <param name="tolerance">
    /// Tolerance used by input analysis functions for loop finding, trimming, etc.
    /// </param>
    /// <returns>
    /// Brep fit through input on success, or null on error.
    /// </returns>
    public static Brep CreatePatch(IEnumerable<GeometryBase> geometry, Surface startingSurface, double tolerance)
    {
      using (var geometry_array = new SimpleArrayGeometryPointer(geometry))
      {
        IntPtr ptr_geometry = geometry_array.NonConstPointer();
        IntPtr const_ptr_surface = startingSurface == null ? IntPtr.Zero : startingSurface.ConstPointer();
        IntPtr ptr_brep = UnsafeNativeMethods.CRhinoFitPatch_Fit1(ptr_geometry, const_ptr_surface, tolerance);
        GC.KeepAlive(geometry);
        GC.KeepAlive(startingSurface);
        return IntPtr.Zero == ptr_brep ? null : new Brep(ptr_brep, null);
      }
    }

    /// <summary>
    /// Constructs a brep patch.
    /// <para>This is the simple version of fit that uses a plane with u x v spans.
    /// It makes a plane by fitting to the points from the input geometry to use as the starting surface.
    /// The surface has the specified u and v span count.</para>
    /// </summary>
    /// <param name="geometry">
    /// A combination of <see cref="Curve">curves</see>, brep trims,
    /// <see cref="Point">points</see>, <see cref="PointCloud">point clouds</see> or <see cref="Mesh">meshes</see>.
    /// Curves and trims are sampled to get points. Trims are sampled for
    /// points and normals.
    /// </param>
    /// <param name="uSpans">The number of spans in the U direction.</param>
    /// <param name="vSpans">The number of spans in the V direction.</param>
    /// <param name="tolerance">
    /// Tolerance used by input analysis functions for loop finding, trimming, etc.
    /// </param>
    /// <returns>
    /// A brep fit through input on success, or null on error.
    /// </returns>
    public static Brep CreatePatch(IEnumerable<GeometryBase> geometry, int uSpans, int vSpans, double tolerance)
    {
      using (var geometry_array = new SimpleArrayGeometryPointer(geometry))
      {
        IntPtr ptr_geometry = geometry_array.NonConstPointer();
        IntPtr ptr_brep = UnsafeNativeMethods.CRhinoFitPatch_Fit2(ptr_geometry, uSpans, vSpans, tolerance);
        return IntPtr.Zero == ptr_brep ? null : new Brep(ptr_brep, null);
      }
    }

    /// <summary>
    /// Constructs a brep patch using all controls
    /// </summary>
    /// <param name="geometry">
    /// A combination of <see cref="Curve">curves</see>, brep trims,
    /// <see cref="Point">points</see>, <see cref="PointCloud">point clouds</see> or <see cref="Mesh">meshes</see>.
    /// Curves and trims are sampled to get points. Trims are sampled for
    /// points and normals.
    /// </param>
    /// <param name="startingSurface">A starting surface (can be null).</param>
    /// <param name="uSpans">
    /// Number of surface spans used when a plane is fit through points to start in the U direction.
    /// </param>
    /// <param name="vSpans">
    /// Number of surface spans used when a plane is fit through points to start in the U direction.
    /// </param>
    /// <param name="trim">
    /// If true, try to find an outer loop from among the input curves and trim the result to that loop
    /// </param>
    /// <param name="tangency">
    /// If true, try to find brep trims in the outer loop of curves and try to
    /// fit to the normal direction of the trim's surface at those locations.
    /// </param>
    /// <param name="pointSpacing">
    /// Basic distance between points sampled from input curves.
    /// </param>
    /// <param name="flexibility">
    /// Determines the behavior of the surface in areas where its not otherwise
    /// controlled by the input.  Lower numbers make the surface behave more
    /// like a stiff material; higher, less like a stiff material.  That is,
    /// each span is made to more closely match the spans adjacent to it if there
    /// is no input geometry mapping to that area of the surface when the
    /// flexibility value is low.  The scale is logrithmic. Numbers around 0.001
    /// or 0.1 make the patch pretty stiff and numbers around 10 or 100 make the
    /// surface flexible.
    /// </param>
    /// <param name="surfacePull">
    /// Tends to keep the result surface where it was before the fit in areas where
    /// there is on influence from the input geometry
    /// </param>
    /// <param name="fixEdges">
    /// Array of four elements. Flags to keep the edges of a starting (untrimmed)
    /// surface in place while fitting the interior of the surface.  Order of
    /// flags is left, bottom, right, top
    ///</param>
    /// <param name="tolerance">
    /// Tolerance used by input analysis functions for loop finding, trimming, etc.
    /// </param>
    /// <returns>
    /// A brep fit through input on success, or null on error.
    /// </returns>
    public static Brep CreatePatch(IEnumerable<GeometryBase> geometry, Surface startingSurface, int uSpans, int vSpans, bool trim,
      bool tangency, double pointSpacing, double flexibility, double surfacePull, bool[] fixEdges, double tolerance)
    {
      using (var geometry_array = new SimpleArrayGeometryPointer(geometry))
      {
        IntPtr ptr_geometry = geometry_array.NonConstPointer();
        IntPtr const_ptr_surface = startingSurface == null ? IntPtr.Zero : startingSurface.ConstPointer();
        var fix_edges = new int[4];
        for (int i = 0; i < 4; i++)
          fix_edges[i] = fixEdges[i] ? 1 : 0;
        IntPtr ptr_brep = UnsafeNativeMethods.CRhinoFitPatch_Fit3(ptr_geometry, const_ptr_surface, uSpans, vSpans, trim, tangency, pointSpacing, flexibility, surfacePull, fix_edges, tolerance);
        GC.KeepAlive(geometry);
        GC.KeepAlive(startingSurface);
        return IntPtr.Zero == ptr_brep ? null : new Brep(ptr_brep, null);
      }
    }

    /// <summary>
    /// Creates a single walled pipe
    /// </summary>
    /// <param name="rail">the path curve for the pipe</param>
    /// <param name="radius">radius of the pipe</param>
    /// <param name="localBlending">
    /// If True, Local (pipe radius stays constant at the ends and changes more rapidly in the middle) is applied.
    /// If False, Global (radius is linearly blended from one end to the other, creating pipes that taper from one radius to the other) is applied
    /// </param>
    /// <param name="cap">end cap mode</param>
    /// <param name="fitRail">
    /// If the curve is a polycurve of lines and arcs, the curve is fit and a single surface is created;
    /// otherwise the result is a polysurface with joined surfaces created from the polycurve segments.
    /// </param>
    /// <param name="absoluteTolerance">
    /// The sweeping and fitting tolerance. If you are unsure what to use, then use the document's absolute tolerance
    /// </param>
    /// <param name="angleToleranceRadians">
    /// The angle tolerance. If you are unsure what to use, then either use the document's angle tolerance in radians
    /// </param>
    /// <returns>Array of created pipes on success</returns>
    public static Brep[] CreatePipe(Curve rail, double radius, bool localBlending, PipeCapMode cap, bool fitRail, double absoluteTolerance, double angleToleranceRadians)
    {
      return CreatePipe(rail, new[] { 0.0 }, new[] { radius }, localBlending, cap, fitRail, absoluteTolerance, angleToleranceRadians);
    }

    /// <summary>
    /// Creates a single walled pipe
    /// </summary>
    /// <param name="rail">the path curve for the pipe</param>
    /// <param name="railRadiiParameters">
    /// one or more normalized curve parameters where changes in radius occur.
    /// Important: curve parameters must be normalized - ranging between 0.0 and 1.0.
    /// </param>
    /// <param name="radii">An array of radii - one at each normalized curve parameter in railRadiiParameters.</param>
    /// <param name="localBlending">
    /// If True, Local (pipe radius stays constant at the ends and changes more rapidly in the middle) is applied.
    /// If False, Global (radius is linearly blended from one end to the other, creating pipes that taper from one radius to the other) is applied
    /// </param>
    /// <param name="cap">end cap mode</param>
    /// <param name="fitRail">
    /// If the curve is a polycurve of lines and arcs, the curve is fit and a single surface is created;
    /// otherwise the result is a polysurface with joined surfaces created from the polycurve segments.
    /// </param>
    /// <param name="absoluteTolerance">
    /// The sweeping and fitting tolerance. If you are unsure what to use, then use the document's absolute tolerance
    /// </param>
    /// <param name="angleToleranceRadians">
    /// The angle tolerance. If you are unsure what to use, then either use the document's angle tolerance in radians
    /// </param>
    /// <returns>Array of created pipes on success</returns>
    public static Brep[] CreatePipe(Curve rail, IEnumerable<double> railRadiiParameters, IEnumerable<double> radii, bool localBlending, PipeCapMode cap, bool fitRail, double absoluteTolerance, double angleToleranceRadians)
    {
      var list_radii_params = new List<double>(railRadiiParameters);
      var list_radii = new List<double>(radii);
      if (list_radii_params.Count < 1 || list_radii_params.Count != list_radii.Count)
        throw new ArgumentException("railRadiiParameters and radii must have at least one element and must have equal lengths");
      IntPtr const_ptr_rail = rail.ConstPointer();
      double[] array_radii_params = list_radii_params.ToArray();
      double[] array_radii = list_radii.ToArray();
      using (var breps = new SimpleArrayBrepPointer())
      {
        IntPtr ptr_breps = breps.NonConstPointer();
        UnsafeNativeMethods.RHC_RhinoPipeBreps(const_ptr_rail, array_radii_params.Length, array_radii_params, array_radii, localBlending, (int)cap, fitRail, absoluteTolerance, angleToleranceRadians, ptr_breps);
        GC.KeepAlive(rail);
        return breps.ToNonConstArray();
      }
    }

    /// <summary>
    /// Sweep1 function that fits a surface through a profile curve that define the surface cross-sections
    /// and one curve that defines a surface edge.
    /// /// </summary>
    /// <param name="rail">Rail to sweep shapes along</param>
    /// <param name="shape">Shape curve</param>
    /// <param name="closed">Only matters if shape is closed</param>
    /// <param name="tolerance">Tolerance for fitting surface and rails</param>
    /// <returns>Array of Brep sweep results</returns>
    /// <remarks>
    /// If you are not producing the sweep results that you are after, then
    /// use the SweepOneRail class with options to generate the swept geometry.
    /// </remarks>
    public static Brep[] CreateFromSweep(Curve rail, Curve shape, bool closed, double tolerance)
    {
      return CreateFromSweep(rail, new[] { shape }, closed, tolerance);
    }

    /// <summary>
    /// Sweep1 function that fits a surface through profile curves that define the surface cross-sections
    /// and one curve that defines a surface edge.
    /// </summary>
    /// <param name="rail">Rail to sweep shapes along</param>
    /// <param name="shapes">Shape curves</param>
    /// <param name="closed">Only matters if shapes are closed</param>
    /// <param name="tolerance">Tolerance for fitting surface and rails</param>
    /// <returns>Array of Brep sweep results</returns>
    /// <remarks>
    /// If you are not producing the sweep results that you are after, then
    /// use the SweepOneRail class with options to generate the swept geometry.
    /// </remarks>
    public static Brep[] CreateFromSweep(Curve rail, IEnumerable<Curve> shapes, bool closed, double tolerance)
    {
      IntPtr const_ptr_rail = rail.ConstPointer();
      using (var shapearray = new SimpleArrayCurvePointer(shapes))
      using (var rc = new SimpleArrayBrepPointer())
      {
        IntPtr const_ptr_shapes = shapearray.ConstPointer();
        IntPtr ptr_breps = rc.NonConstPointer();
        UnsafeNativeMethods.RHC_Rhino1RailSweep(const_ptr_rail, const_ptr_shapes, closed, tolerance, ptr_breps);
        GC.KeepAlive(rail);
        GC.KeepAlive(shapes);
        return rc.ToNonConstArray();
      }
    }

    /// <summary>
    /// Sweep1 function that fits a surface through a profile curve that define the surface cross-sections
    /// and one curve that defines a surface edge. The Segmented version breaks the rail at curvature kinks
    /// and sweeps each piece separately, then put the results together into a Brep.
    /// </summary>
    /// <param name="rail">Rail to sweep shapes along</param>
    /// <param name="shape">Shape curve</param>
    /// <param name="closed">Only matters if shape is closed</param>
    /// <param name="tolerance">Tolerance for fitting surface and rails</param>
    /// <returns>Array of Brep sweep results</returns>
    /// <remarks>
    /// If you are not producing the sweep results that you are after, then
    /// use the SweepOneRail class with options to generate the swept geometry.
    /// </remarks>
    public static Brep[] CreateFromSweepSegmented(Curve rail, Curve shape, bool closed, double tolerance)
    {
      return CreateFromSweepSegmented(rail, new[] { shape }, closed, tolerance);
    }

    /// <summary>
    /// Sweep1 function that fits a surface through a series of profile curves that define the surface cross-sections
    /// and one curve that defines a surface edge. The Segmented version breaks the rail at curvature kinks
    /// and sweeps each piece separately, then put the results together into a Brep.
    /// </summary>
    /// <param name="rail">Rail to sweep shapes along</param>
    /// <param name="shapes">Shape curves</param>
    /// <param name="closed">Only matters if shapes are closed</param>
    /// <param name="tolerance">Tolerance for fitting surface and rails</param>
    /// <returns>Array of Brep sweep results</returns>
    /// <remarks>
    /// If you are not producing the sweep results that you are after, then
    /// use the SweepOneRail class with options to generate the swept geometry.
    /// </remarks>
    public static Brep[] CreateFromSweepSegmented(Curve rail, IEnumerable<Curve> shapes, bool closed, double tolerance)
    {
      IntPtr const_ptr_rail = rail.ConstPointer();
      using (var shapearray = new SimpleArrayCurvePointer(shapes))
      using (var rc = new SimpleArrayBrepPointer())
      {
        IntPtr const_ptr_shapes = shapearray.ConstPointer();
        IntPtr ptr_breps = rc.NonConstPointer();
        UnsafeNativeMethods.RHC_Rhino1RailSweepSegmented(const_ptr_rail, const_ptr_shapes, closed, tolerance, ptr_breps);
        GC.KeepAlive(rail);
        GC.KeepAlive(shapes);
        return rc.ToNonConstArray();
      }
    }

    /// <summary>
    /// General 2 rail sweep. If you are not producing the sweep results that you are after, then
    /// use the SweepTwoRail class with options to generate the swept geometry.
    /// </summary>
    /// <param name="rail1">Rail to sweep shapes along</param>
    /// <param name="rail2">Rail to sweep shapes along</param>
    /// <param name="shape">Shape curve</param>
    /// <param name="closed">Only matters if shape is closed</param>
    /// <param name="tolerance">Tolerance for fitting surface and rails</param>
    /// <returns>Array of Brep sweep results</returns>
    public static Brep[] CreateFromSweep(Curve rail1, Curve rail2, Curve shape, bool closed, double tolerance)
    {
      return CreateFromSweep(rail1, rail2, new[] { shape }, closed, tolerance);
    }

    /// <summary>
    /// General 2 rail sweep. If you are not producing the sweep results that you are after, then
    /// use the SweepTwoRail class with options to generate the swept geometry.
    /// </summary>
    /// <param name="rail1">Rail to sweep shapes along</param>
    /// <param name="rail2">Rail to sweep shapes along</param>
    /// <param name="shapes">Shape curves</param>
    /// <param name="closed">Only matters if shapes are closed</param>
    /// <param name="tolerance">Tolerance for fitting surface and rails</param>
    /// <returns>Array of Brep sweep results</returns>
    public static Brep[] CreateFromSweep(Curve rail1, Curve rail2, IEnumerable<Curve> shapes, bool closed, double tolerance)
    {
      IntPtr const_ptr_rail1 = rail1.ConstPointer();
      IntPtr const_ptr_rail2 = rail2.ConstPointer();
      using (var shapearray = new SimpleArrayCurvePointer(shapes))
      using (var rc = new SimpleArrayBrepPointer())
      {
        IntPtr const_ptr_shapes = shapearray.ConstPointer();
        IntPtr ptr_breps = rc.NonConstPointer();
        UnsafeNativeMethods.RHC_Rhino2RailSweep(const_ptr_rail1, const_ptr_rail2, const_ptr_shapes, closed, tolerance, ptr_breps);
        Runtime.CommonObject.GcProtect(rail1, rail2);
        GC.KeepAlive(shapes);
        return rc.ToNonConstArray();
      }
    }

    /// <summary>
    /// Makes a 2 rail sweep.  Like CreateFromSweep but the result is split where parameterization along a rail changes abruptly
    /// </summary>
    /// <param name="rail1">Rail to sweep shapes along</param>
    /// <param name="rail2">Rail to sweep shapes along</param>
    /// <param name="shapes">Shape curves</param>
    /// <param name="rail_params">Shape parameters</param>
    /// <param name="closed">Only matters if shapes are closed</param>
    /// <param name="tolerance">Tolerance for fitting surface and rails</param>
    /// <returns>Array of Brep sweep results</returns>
    public static Brep[] CreateFromSweepInParts(Curve rail1, Curve rail2, IEnumerable<Curve> shapes, IEnumerable<Point2d> rail_params, bool closed, double tolerance)
    {
      IntPtr const_ptr_rail1 = rail1.ConstPointer();
      IntPtr const_ptr_rail2 = rail2.ConstPointer();
      using (var shapearray = new SimpleArrayCurvePointer(shapes))
      using (var rc = new SimpleArrayBrepPointer())
      {
        IntPtr const_ptr_shapes = shapearray.ConstPointer();
        IntPtr ptr_breps = rc.NonConstPointer();
        UnsafeNativeMethods.RHC_Rhino2RailSweepInParts(const_ptr_rail1, const_ptr_rail2, const_ptr_shapes, closed, tolerance, ptr_breps);
        Runtime.CommonObject.GcProtect(rail1, rail2);
        GC.KeepAlive(shapes);
        return rc.ToNonConstArray();
      }
    }

    /// <summary>
    /// Extrude a curve to a taper making a brep (potentially more than 1)
    /// </summary>
    /// <param name="curveToExtrude">the curve to extrude</param>
    /// <param name="distance">the distance to extrude</param>
    /// <param name="direction">the direction of the extrusion</param>
    /// <param name="basePoint">the basepoint of the extrusion</param>
    /// <param name="draftAngleRadians">angle of the extrusion</param>
    /// <param name="cornerType"></param>
    /// <param name="tolerance">tolerance to use for the extrusion</param>
    /// <param name="angleToleranceRadians">angle tolerance to use for the extrusion</param>
    /// <returns>array of breps on success</returns>
    public static Brep[] CreateFromTaperedExtrude(Curve curveToExtrude, double distance, Vector3d direction, Point3d basePoint, double draftAngleRadians, ExtrudeCornerType cornerType, double tolerance, double angleToleranceRadians)
    {
      IntPtr const_ptr_curve = curveToExtrude.ConstPointer();
      using (var rc = new SimpleArrayBrepPointer())
      {
        IntPtr ptr_breps = rc.NonConstPointer();
        UnsafeNativeMethods.RHC_RhinoCreateTaperedExtrude(const_ptr_curve, distance, direction, basePoint, draftAngleRadians, (int)cornerType, tolerance, angleToleranceRadians, ptr_breps);
        GC.KeepAlive(curveToExtrude);
        return rc.ToNonConstArray();
      }
    }

    /// <summary>
    /// Extrude a curve to a taper making a brep (potentially more than 1)
    /// </summary>
    /// <param name="curveToExtrude">the curve to extrude</param>
    /// <param name="distance">the distance to extrude</param>
    /// <param name="direction">the direction of the extrusion</param>
    /// <param name="basePoint">the basepoint of the extrusion</param>
    /// <param name="draftAngleRadians">angle of the extrusion</param>
    /// <param name="cornerType"></param>
    /// <returns>array of breps on success</returns>
    /// <remarks>tolerances used are based on the active doc tolerance</remarks>
    [Obsolete("Use version that takes tolerance as input")]
    public static Brep[] CreateFromTaperedExtrude(Curve curveToExtrude, double distance, Vector3d direction, Point3d basePoint, double draftAngleRadians, ExtrudeCornerType cornerType)
    {
      // Kept around in order to not break the SDK. Older RhinoCreateTaperedExtrude
      // function used ActiveDoc to get tolerances.
      double tolerance, angle_tol;
      RhinoDoc.ActiveDocTolerances(out tolerance, out angle_tol);
      return CreateFromTaperedExtrude(curveToExtrude, distance, direction, basePoint, draftAngleRadians, cornerType, tolerance, angle_tol);
    }


    /// <summary>
    /// Creates one or more Breps by extruding a curve a distance along an axis with draft angle.
    /// </summary>
    /// <param name="curve">The curve to extrude.</param>
    /// <param name="direction">The extrusion direction.</param>
    /// <param name="distance">The extrusion distance.</param>
    /// <param name="draftAngle">The extrusion draft angle in radians.</param>
    /// <param name="plane">
    /// The end of the extrusion will be parallel to this plane, and "distance" from the plane's origin.
    /// The plane's origin is generally be a point on the curve. For planar curves, a natural choice for the
    /// plane's normal direction will be the normal direction of the curve's plane. In any case, 
    /// plane.Normal = direction may make sense.
    /// </param>
    /// <param name="tolerance">The intersecting and trimming tolerance.</param>
    /// <returns>An array of Breps if successful.</returns>
    public static Brep[] CreateFromTaperedExtrudeWithRef(Curve curve, Vector3d direction, double distance, double draftAngle, Plane plane, double tolerance)
    {
      if (curve == null) throw new ArgumentNullException(nameof(curve));
      IntPtr const_ptr_curve = curve.ConstPointer();
      using (var rc = new SimpleArrayBrepPointer())
      {
        IntPtr ptr_breps = rc.NonConstPointer();
        UnsafeNativeMethods.RHC_RhinoTaperedExtrudeCurveWithRef(const_ptr_curve, direction, distance, draftAngle, plane.Origin, plane.Normal, tolerance, ptr_breps);
        GC.KeepAlive(curve);
        return rc.ToNonConstArray();
      }
    }


    /// <summary>
    /// Makes a surface blend between two surface edges.
    /// </summary>
    /// <param name="face0">First face to blend from.</param>
    /// <param name="edge0">First edge to blend from.</param>
    /// <param name="domain0">The domain of edge0 to use.</param>
    /// <param name="rev0">If false, edge0 will be used in its natural direction. If true, edge0 will be used in the reversed direction.</param>
    /// <param name="continuity0">Continuity for the blend at the start.</param>
    /// <param name="face1">Second face to blend from.</param>
    /// <param name="edge1">Second edge to blend from.</param>
    /// <param name="domain1">The domain of edge1 to use.</param>
    /// <param name="rev1">If false, edge1 will be used in its natural direction. If true, edge1 will be used in the reversed direction.</param>
    /// <param name="continuity1">Continuity for the blend at the end.</param>
    /// <returns>Array of Breps if successful.</returns>
    public static Brep[] CreateBlendSurface(BrepFace face0, BrepEdge edge0, Interval domain0, bool rev0, BlendContinuity continuity0,
                                            BrepFace face1, BrepEdge edge1, Interval domain1, bool rev1, BlendContinuity continuity1)
    {
      if (face0 == null) throw new ArgumentNullException(nameof(face0));
      if (edge0 == null) throw new ArgumentNullException(nameof(edge0));
      if (face1 == null) throw new ArgumentNullException(nameof(face1));
      if (edge1 == null) throw new ArgumentNullException(nameof(edge1));

      var ptr_const_face0 = face0.ConstPointer();
      var ptr_const_edge0 = edge0.ConstPointer();
      var ptr_const_face1 = face1.ConstPointer();
      var ptr_const_edge1 = edge1.ConstPointer();

      using (var breps = new SimpleArrayBrepPointer())
      {
        var ptr_breps = breps.NonConstPointer();
        UnsafeNativeMethods.RHC_RhinoSdkBlend_SurfaceBlend(
          ptr_const_face0, ptr_const_edge0, domain0, rev0, (int)continuity0,
          ptr_const_face1, ptr_const_edge1, domain1, rev1, (int)continuity1,
          ptr_breps);

        Runtime.CommonObject.GcProtect(face0, edge0, face1, edge1);
        return breps.ToNonConstArray();
      }
    }

    /// <summary>
    /// Makes a curve blend between points on two surface edges. The blend will be tangent to the surfaces and perpendicular to the edges.
    /// </summary>
    /// <param name="face0">First face to blend from.</param>
    /// <param name="edge0">First edge to blend from.</param>
    /// <param name="t0">Location on first edge for first end of blend curve.</param>
    /// <param name="rev0">If false, edge0 will be used in its natural direction. If true, edge0 will be used in the reversed direction.</param>
    /// <param name="continuity0">Continuity for the blend at the start.</param>
    /// <param name="face1">Second face to blend from.</param>
    /// <param name="edge1">Second edge to blend from.</param>
    /// <param name="t1">Location on second edge for second end of blend curve.</param>
    /// <param name="rev1">If false, edge1 will be used in its natural direction. If true, edge1 will be used in the reversed direction.</param>
    /// <param name="continuity1">>Continuity for the blend at the end.</param>
    /// <returns>The blend curve on success. null on failure</returns>
    public static Curve CreateBlendShape(BrepFace face0, BrepEdge edge0, double t0, bool rev0, BlendContinuity continuity0,
                                         BrepFace face1, BrepEdge edge1, double t1, bool rev1, BlendContinuity continuity1)
    {
      if (face0 == null) throw new ArgumentNullException(nameof(face0));
      if (edge0 == null) throw new ArgumentNullException(nameof(edge0));
      if (face1 == null) throw new ArgumentNullException(nameof(face1));
      if (edge1 == null) throw new ArgumentNullException(nameof(edge1));

      var ptr_const_face0 = face0.ConstPointer();
      var ptr_const_edge0 = edge0.ConstPointer();
      var ptr_const_face1 = face1.ConstPointer();
      var ptr_const_edge1 = edge1.ConstPointer();

      IntPtr ptr_curve = UnsafeNativeMethods.RHC_RhinoSdkBlend_SurfaceBlendShape(
        ptr_const_face0, ptr_const_edge0, t0, rev0, (int)continuity0,
        ptr_const_face1, ptr_const_edge1, t1, rev1, (int)continuity1
        );
      Runtime.CommonObject.GcProtect(face0, edge0, face1, edge1);

      return CreateGeometryHelper(ptr_curve, null) as Curve;
    }

    /// <summary>
    /// Creates a constant-radius round surface between two surfaces.
    /// </summary>
    /// <param name="face0">First face to fillet from.</param>
    /// <param name="uv0">A parameter face0 at the side you want to keep after filleting.</param>
    /// <param name="face1">Second face to fillet from.</param>
    /// <param name="uv1">A parameter face1 at the side you want to keep after filleting.</param>
    /// <param name="radius">The fillet radius.</param>
    /// <param name="extend">If true, then when one input surface is longer than the other, the fillet surface is extended to the input surface edges.</param>
    /// <param name="tolerance">The tolerance. In in doubt, the the document's model absolute tolerance.</param>
    /// <returns>Array of Breps if successful.</returns>
    public static Brep[] CreateFilletSurface(BrepFace face0, Point2d uv0, BrepFace face1, Point2d uv1, double radius, bool extend, double tolerance)
    {
      if (face0 == null) throw new ArgumentNullException(nameof(face0));
      if (face1 == null) throw new ArgumentNullException(nameof(face1));

      var ptr_const_face0 = face0.ConstPointer();
      var ptr_const_face1 = face1.ConstPointer();

      using (var fillets = new SimpleArrayBrepPointer())
      {
        var ptr_fillets = fillets.NonConstPointer();
        var rc = UnsafeNativeMethods.RHC_RhinoFilletSurface(ptr_const_face0, uv0, ptr_const_face1, uv1, radius, extend, tolerance, ptr_fillets);
        Runtime.CommonObject.GcProtect(face0, face1);
        return rc ? fillets.ToNonConstArray() : new Brep[0];
      }
    }

    /// <summary>
    ///  Creates a constant-radius round surface between two surfaces.
    /// </summary>
    /// <param name="face0">First face to fillet from.</param>
    /// <param name="uv0">A parameter face0 at the side you want to keep after filleting.</param>
    /// <param name="face1">Second face to fillet from.</param>
    /// <param name="uv1">A parameter face1 at the side you want to keep after filleting.</param>
    /// <param name="radius">The fillet radius.</param>
    /// <param name="trim">If true, the input faces will be trimmed, if false, the input faces will be split.</param>
    /// <param name="extend">If true, then when one input surface is longer than the other, the fillet surface is extended to the input surface edges.</param>
    /// <param name="tolerance">The tolerance. In in doubt, the the document's model absolute tolerance.</param>
    /// <param name="outBreps0">The trim or split results of the Brep owned by face0.</param>
    /// <param name="outBreps1">The trim or split results of the Brep owned by face1.</param>
    /// <returns>Array of Breps if successful.</returns>
    public static Brep[] CreateFilletSurface(BrepFace face0, Point2d uv0, BrepFace face1, Point2d uv1, double radius, bool trim, bool extend, double tolerance, out Brep[] outBreps0, out Brep[] outBreps1)
    {
      if (face0 == null) throw new ArgumentNullException(nameof(face0));
      if (face1 == null) throw new ArgumentNullException(nameof(face1));

      outBreps0 = null;
      outBreps1 = null;

      var ptr_const_face0 = face0.ConstPointer();
      var ptr_const_face1 = face1.ConstPointer();

      using (var fillets = new SimpleArrayBrepPointer())
      using (var out_breps0 = new SimpleArrayBrepPointer())
      using (var out_breps1 = new SimpleArrayBrepPointer())
      {
        var ptr_fillets = fillets.NonConstPointer();
        var ptr_out_breps0 = out_breps0.NonConstPointer();
        var ptr_out_breps1 = out_breps1.NonConstPointer();

        var rc = UnsafeNativeMethods.RHC_RhinoFilletSurface2(ptr_const_face0, uv0, ptr_const_face1, uv1, radius, trim, extend, tolerance, ptr_fillets, ptr_out_breps0, ptr_out_breps1);
        Runtime.CommonObject.GcProtect(face0, face1);
        if (rc)
        {
          outBreps0 = out_breps0.ToNonConstArray();
          outBreps1 = out_breps1.ToNonConstArray();
          return fillets.ToNonConstArray();
        }

        return new Brep[0];
      }
    }

    /// <summary>
    /// Creates a ruled surface as a bevel between two input surface edges.
    /// </summary>
    /// <param name="face0">First face to chamfer from.</param>
    /// <param name="uv0">A parameter face0 at the side you want to keep after chamfering.</param>
    /// <param name="radius0">The distance from the intersection of face0 to the edge of the chamfer.</param>
    /// <param name="face1">Second face to chamfer from.</param>
    /// <param name="uv1">A parameter face1 at the side you want to keep after chamfering.</param>
    /// <param name="radius1">The distance from the intersection of face1 to the edge of the chamfer.</param>
    /// <param name="extend">If true, then when one input surface is longer than the other, the chamfer surface is extended to the input surface edges.</param>
    /// <param name="tolerance">The tolerance. In in doubt, the the document's model absolute tolerance.</param>
    /// <returns>Array of Breps if successful.</returns>
    public static Brep[] CreateChamferSurface(BrepFace face0, Point2d uv0, double radius0, BrepFace face1, Point2d uv1, double radius1, bool extend, double tolerance)
    {
      if (face0 == null) throw new ArgumentNullException(nameof(face0));
      if (face1 == null) throw new ArgumentNullException(nameof(face1));

      var ptr_const_face0 = face0.ConstPointer();
      var ptr_const_face1 = face1.ConstPointer();
      Runtime.CommonObject.GcProtect(face0, face1);

      using (var fillets = new SimpleArrayBrepPointer())
      {
        var ptr_fillets = fillets.NonConstPointer();
        var rc = UnsafeNativeMethods.RHC_RhinoChamferSurface(ptr_const_face0, uv0, radius0, ptr_const_face1, uv1, radius1, extend, tolerance, ptr_fillets);
        return rc ? fillets.ToNonConstArray() : new Brep[0];
      }
    }

    /// <summary>
    /// Creates a ruled surface as a bevel between two input surface edges.
    /// </summary>
    /// <param name="face0">First face to chamfer from.</param>
    /// <param name="uv0">A parameter face0 at the side you want to keep after chamfering.</param>
    /// <param name="radius0">The distance from the intersection of face0 to the edge of the chamfer.</param>
    /// <param name="face1">Second face to chamfer from.</param>
    /// <param name="uv1">A parameter face1 at the side you want to keep after chamfering.</param>
    /// <param name="radius1">The distance from the intersection of face1 to the edge of the chamfer.</param>
    /// <param name="trim">If true, the input faces will be trimmed, if false, the input faces will be split.</param>
    /// <param name="extend">If true, then when one input surface is longer than the other, the chamfer surface is extended to the input surface edges.</param>
    /// <param name="tolerance">The tolerance. In in doubt, the the document's model absolute tolerance.</param>
    /// <param name="outBreps0">The trim or split results of the Brep owned by face0.</param>
    /// <param name="outBreps1">The trim or split results of the Brep owned by face1.</param>
    /// <returns>Array of Breps if successful.</returns>
    public static Brep[] CreateChamferSurface(BrepFace face0, Point2d uv0, double radius0, BrepFace face1, Point2d uv1, double radius1, bool trim, bool extend, double tolerance, out Brep[] outBreps0, out Brep[] outBreps1)
    {
      if (face0 == null) throw new ArgumentNullException(nameof(face0));
      if (face1 == null) throw new ArgumentNullException(nameof(face1));

      outBreps0 = null;
      outBreps1 = null;

      var ptr_const_face0 = face0.ConstPointer();
      var ptr_const_face1 = face1.ConstPointer();

      using (var fillets = new SimpleArrayBrepPointer())
      using (var out_breps0 = new SimpleArrayBrepPointer())
      using (var out_breps1 = new SimpleArrayBrepPointer())
      {
        var ptr_fillets = fillets.NonConstPointer();
        var ptr_out_breps0 = out_breps0.NonConstPointer();
        var ptr_out_breps1 = out_breps1.NonConstPointer();

        var rc = UnsafeNativeMethods.RHC_RhinoChamferSurface2(ptr_const_face0, uv0, radius0, ptr_const_face1, uv1, radius1, trim, extend, tolerance, ptr_fillets, ptr_out_breps0, ptr_out_breps1);
        Runtime.CommonObject.GcProtect(face0, face1);
        if (rc)
        {
          outBreps0 = out_breps0.ToNonConstArray();
          outBreps1 = out_breps1.ToNonConstArray();
          return fillets.ToNonConstArray();
        }

        return new Brep[0];
      }
    }

    /// <summary>
    /// Fillets, chamfers, or blends the edges of a brep.
    /// </summary>
    /// <param name="brep">The brep to fillet, chamfer, or blend edges.</param>
    /// <param name="edgeIndices">An array of one or more edge indices where the fillet, chamfer, or blend will occur.</param>
    /// <param name="startRadii">An array of starting fillet, chamfer, or blend radaii, one for each edge index.</param>
    /// <param name="endRadii">An array of ending fillet, chamfer, or blend radaii, one for each edge index.</param>
    /// <param name="blendType">The blend type.</param>
    /// <param name="railType">The rail type.</param>
    /// <param name="tolerance">The tolerance to be used to perform calculations.</param>
    /// <returns>Array of Breps if successful.</returns>
    public static Brep[] CreateFilletEdges(Brep brep, IEnumerable<int> edgeIndices, IEnumerable<double> startRadii, IEnumerable<double> endRadii, BlendType blendType, RailType railType, double tolerance)
    {
      if (brep == null) throw new ArgumentNullException(nameof(brep));

      var ptr_const_brep = brep.ConstPointer();

      using (var edges = new SimpleArrayInt(edgeIndices))
      using (var radii0 = new SimpleArrayDouble(startRadii))
      using (var radii1 = new SimpleArrayDouble(endRadii))
      using (var out_breps = new SimpleArrayBrepPointer())
      {
        var ptr_const_edges = edges.ConstPointer();
        var ptr_const_radii0 = radii0.ConstPointer();
        var ptr_const_radii1 = radii1.ConstPointer();

        var ptr_out_breps = out_breps.NonConstPointer();

        var rc = UnsafeNativeMethods.RHC_RhinoFilletEdges(ptr_const_brep, ptr_const_edges, ptr_const_radii0, ptr_const_radii1, (int)blendType, (int)railType, tolerance, ptr_out_breps);
        if (rc)
          return out_breps.ToNonConstArray();

        GC.KeepAlive(brep);
        return new Brep[0];
      }
    }

    /// <summary>
    /// Offsets a Brep.
    /// </summary>
    /// <param name="brep">The Brep to offset.</param>
    /// <param name="distance">
    /// The distance to offset. This is a signed distance value with respect to
    /// face normals and flipped faces.
    /// </param>
    /// <param name="solid">
    /// If true, then the function makes a closed solid from the input and offset
    /// surfaces by lofting a ruled surface between all of the matching edges.
    /// </param>
    /// <param name="extend">
    /// If true, then the function maintains the sharp corners when the original
    /// surfaces have sharps corner. If False, then the function creates fillets
    /// at sharp corners in the original surfaces.
    /// </param>
    /// <param name="tolerance">The offset tolerance.</param>
    /// <param name="outBlends">The results of the calculation.</param>
    /// <param name="outWalls">The results of the calculation.</param>
    /// <returns>
    /// Array of Breps if successful. If the function succeeds in offsetting, a
    /// single Brep will be returned. Otherwise, the array will contain the 
    /// offset surfaces, outBlends will contain the set of blends used to fill
    /// in gaps (if extend is false), and outWalls will contain the set of wall
    /// surfaces that was supposed to join the offset to the original (if solid
    /// is true).
    /// </returns>
    public static Brep[] CreateOffsetBrep(Brep brep, double distance, bool solid, bool extend, double tolerance, out Brep[] outBlends, out Brep[] outWalls)
    {
      if (brep == null) throw new ArgumentNullException(nameof(brep));

      outBlends = null;
      outWalls = null;

      var ptr_const_brep = brep.ConstPointer();

      using (var out_breps = new SimpleArrayBrepPointer())
      using (var out_blends = new SimpleArrayBrepPointer())
      using (var out_walls = new SimpleArrayBrepPointer())
      {
        var ptr_out_breps = out_breps.NonConstPointer();
        var ptr_out_blends = out_blends.NonConstPointer();
        var ptr_out_walls = out_walls.NonConstPointer();

        var rc = UnsafeNativeMethods.RHC_RhinoOffsetBrep(ptr_const_brep, distance, solid, extend, tolerance, ptr_out_breps, ptr_out_blends, ptr_out_walls);
        if (rc)
        {
          outBlends = out_blends.ToNonConstArray();
          outWalls = out_walls.ToNonConstArray();
          return out_breps.ToNonConstArray();
        }

        GC.KeepAlive(brep);
        return new Brep[0];
      }
    }

#endif

#if RHINO_SDK

    /// <summary>
    /// Joins two naked edges, or edges that are coincident or close together, from two Breps.
    /// </summary>
    /// <param name="brep0">The first Brep.</param>
    /// <param name="edgeIndex0">The edge index on the first Brep.</param>
    /// <param name="brep1">The second Brep.</param>
    /// <param name="edgeIndex1">The edge index on the second Brep.</param>
    /// <param name="joinTolerance">The join tolerance.</param>
    /// <returns>The resulting Brep if successful, null on failure.</returns>
    public static Brep CreateFromJoinedEdges(Brep brep0, int edgeIndex0, Brep brep1, int edgeIndex1, double joinTolerance)
    {
      if (brep0 == null) { throw new ArgumentNullException(nameof(brep0)); }
      if (brep1 == null) { throw new ArgumentNullException(nameof(brep1)); }
      IntPtr ptr_const_brep0 = brep0.ConstPointer();
      IntPtr ptr_const_brep1 = brep1.ConstPointer();
      IntPtr ptr_newbrep = UnsafeNativeMethods.RHC_RhinoJoinEdges2(ptr_const_brep0, edgeIndex0, ptr_const_brep1, edgeIndex1, joinTolerance);
      Runtime.CommonObject.GcProtect(brep0, brep1);
      return ptr_newbrep != IntPtr.Zero ? new Brep(ptr_newbrep, null) : null;
    }

    /// <summary>
    /// Constructs one or more Breps by lofting through a set of curves.
    /// </summary>
    /// <param name="curves">
    /// The curves to loft through. This function will not perform any curve sorting. You must pass in
    /// curves in the order you want them lofted. This function will not adjust the directions of open
    /// curves. Use Curve.DoDirectionsMatch and Curve.Reverse to adjust the directions of open curves.
    /// This function will not adjust the seams of closed curves. Use Curve.ChangeClosedCurveSeam to
    /// adjust the seam of closed curves.
    /// </param>
    /// <param name="start">
    /// Optional starting point of loft. Use Point3d.Unset if you do not want to include a start point.
    /// </param>
    /// <param name="end">
    /// Optional ending point of loft. Use Point3d.Unset if you do not want to include an end point.
    /// </param>
    /// <param name="loftType">type of loft to perform.</param>
    /// <param name="closed">true if the last curve in this loft should be connected back to the first one.</param>
    /// <returns>
    /// Constructs a closed surface, continuing the surface past the last curve around to the
    /// first curve. Available when you have selected three shape curves.
    /// </returns>
    /// <example>
    /// <code source='examples\vbnet\ex_loft.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_loft.cs' lang='cs'/>
    /// <code source='examples\py\ex_loft.py' lang='py'/>
    /// </example>
    public static Brep[] CreateFromLoft(IEnumerable<Curve> curves, Point3d start, Point3d end, LoftType loftType, bool closed)
    {
      return LoftHelper(curves, start, end, loftType, 0, 0, 0.0, closed);
    }
    /// <summary>
    /// Constructs one or more Breps by lofting through a set of curves. Input for the loft is simplified by
    /// rebuilding to a specified number of control points.
    /// </summary>
    /// <param name="curves">
    /// The curves to loft through. This function will not perform any curve sorting. You must pass in
    /// curves in the order you want them lofted. This function will not adjust the directions of open
    /// curves. Use Curve.DoDirectionsMatch and Curve.Reverse to adjust the directions of open curves.
    /// This function will not adjust the seams of closed curves. Use Curve.ChangeClosedCurveSeam to
    /// adjust the seam of closed curves.
    /// </param>
    /// <param name="start">
    /// Optional starting point of loft. Use Point3d.Unset if you do not want to include a start point.
    /// </param>
    /// <param name="end">
    /// Optional ending point of lost. Use Point3d.Unset if you do not want to include an end point.
    /// </param>
    /// <param name="loftType">type of loft to perform.</param>
    /// <param name="closed">true if the last curve in this loft should be connected back to the first one.</param>
    /// <param name="rebuildPointCount">A number of points to use while rebuilding the curves. 0 leaves turns this parameter off.</param>
    /// <returns>
    /// Constructs a closed surface, continuing the surface past the last curve around to the
    /// first curve. Available when you have selected three shape curves.
    /// </returns>
    public static Brep[] CreateFromLoftRebuild(IEnumerable<Curve> curves, Point3d start, Point3d end, LoftType loftType, bool closed, int rebuildPointCount)
    {
      return LoftHelper(curves, start, end, loftType, 1, rebuildPointCount, 0.0, closed);
    }
    /// <summary>
    /// Constructs one or more Breps by lofting through a set of curves. Input for the loft is simplified by
    /// refitting to a specified tolerance.
    /// </summary>
    /// <param name="curves">
    /// The curves to loft through. This function will not perform any curve sorting. You must pass in
    /// curves in the order you want them lofted. This function will not adjust the directions of open
    /// curves. Use Curve.DoDirectionsMatch and Curve.Reverse to adjust the directions of open curves.
    /// This function will not adjust the seams of closed curves. Use Curve.ChangeClosedCurveSeam to
    /// adjust the seam of closed curves.
    /// </param>
    /// <param name="start">
    /// Optional starting point of loft. Use Point3d.Unset if you do not want to include a start point.
    /// </param>
    /// <param name="end">
    /// Optional ending point of lost. Use Point3d.Unset if you do not want to include an end point.
    /// </param>
    /// <param name="loftType">type of loft to perform.</param>
    /// <param name="closed">true if the last curve in this loft should be connected back to the first one.</param>
    /// <param name="refitTolerance">A distance to use in refitting, or 0 if you want to turn this parameter off.</param>
    /// <returns>
    /// Constructs a closed surface, continuing the surface past the last curve around to the
    /// first curve. Available when you have selected three shape curves.
    /// </returns>
    public static Brep[] CreateFromLoftRefit(IEnumerable<Curve> curves, Point3d start, Point3d end, LoftType loftType, bool closed, double refitTolerance)
    {
      return LoftHelper(curves, start, end, loftType, 2, 0, refitTolerance, closed);
    }
    static Brep[] LoftHelper(IEnumerable<Curve> curves, Point3d start, Point3d end,
      LoftType loftType, int simplifyMethod, int rebuildCount, double refitTol, bool closed)
    {
      using (var curve_array = new SimpleArrayCurvePointer(curves))
      using (var brep_array = new SimpleArrayBrepPointer())
      {
        IntPtr const_ptr_curves = curve_array.ConstPointer();
        IntPtr ptr_breps = brep_array.NonConstPointer();
        UnsafeNativeMethods.RHC_RhinoSdkLoft(const_ptr_curves, start, end, (int)loftType, simplifyMethod, rebuildCount, refitTol, closed, ptr_breps);
        GC.KeepAlive(curves);
        return brep_array.ToNonConstArray();
      }
    }

    /// <summary>
    /// Compute the Boolean Union of a set of Breps.
    /// </summary>
    /// <param name="breps">Breps to union.</param>
    /// <param name="tolerance">Tolerance to use for union operation.</param>
    /// <returns>An array of Brep results or null on failure.</returns>
    public static Brep[] CreateBooleanUnion(IEnumerable<Brep> breps, double tolerance)
    {
      return CreateBooleanUnion(breps, tolerance, true);
    }

    /// <summary>
    /// Compute the Boolean Union of a set of Breps.
    /// </summary>
    /// <param name="breps">Breps to union.</param>
    /// <param name="tolerance">Tolerance to use for union operation.</param>
    /// <param name="manifoldOnly">If true, non-manifold input breps are ignored.</param>
    /// <returns>An array of Brep results or null on failure.</returns>
    public static Brep[] CreateBooleanUnion(IEnumerable<Brep> breps, double tolerance, bool manifoldOnly)
    {
      // 30-Mar-2016 Dale Fugier http://mcneel.myjetbrains.com/youtrack/issue/RH-31606
      // Add manifoldOnly parameter
      using (var input = new SimpleArrayBrepPointer())
      using (var output = new SimpleArrayBrepPointer())
      {
        foreach (var brep in breps)
          input.Add(brep, true);

        IntPtr const_ptr_input = input.ConstPointer();
        IntPtr ptr_output = output.NonConstPointer();

        int join_count = UnsafeNativeMethods.RHC_RhinoBooleanUnion(const_ptr_input, ptr_output, tolerance, manifoldOnly);
        GC.KeepAlive(breps);
        return join_count > 0 ? output.ToNonConstArray() : null;
      }
    }

    static Brep[] BooleanIntDiffHelper(IEnumerable<Brep> firstSet, IEnumerable<Brep> secondSet, double tolerance, bool intersection, bool manifoldOnly)
    {
      // 30-Mar-2016 Dale Fugier http://mcneel.myjetbrains.com/youtrack/issue/RH-31606
      // Add manifoldOnly parameter

      if (null == firstSet || null == secondSet)
        return new Brep[0];

      using (var input_set1 = new SimpleArrayBrepPointer())
      using (var input_set2 = new SimpleArrayBrepPointer())
      using (var output = new SimpleArrayBrepPointer())
      {
        foreach (var brep in firstSet)
          input_set1.Add(brep, true);

        foreach (var brep in secondSet)
          input_set2.Add(brep, true);

        IntPtr const_ptr_inputset1 = input_set1.ConstPointer();
        IntPtr const_ptr_inputset2 = input_set2.ConstPointer();
        IntPtr ptr_output = output.NonConstPointer();

        if (UnsafeNativeMethods.RHC_RhinoBooleanIntDiff(const_ptr_inputset1, const_ptr_inputset2, ptr_output, tolerance, intersection, manifoldOnly))
          return output.ToNonConstArray();
        GC.KeepAlive(firstSet);
        GC.KeepAlive(secondSet);
        return null;
      }
    }

    /// <summary>
    /// Compute the Solid Intersection of two sets of Breps.
    /// </summary>
    /// <param name="firstSet">First set of Breps.</param>
    /// <param name="secondSet">Second set of Breps.</param>
    /// <param name="tolerance">Tolerance to use for intersection operation.</param>
    /// <returns>An array of Brep results or null on failure.</returns>
    /// <remarks>The solid orientation of the breps make a difference when calling this function</remarks>
    public static Brep[] CreateBooleanIntersection(IEnumerable<Brep> firstSet, IEnumerable<Brep> secondSet, double tolerance)
    {
      return BooleanIntDiffHelper(firstSet, secondSet, tolerance, true, true);
    }

    /// <summary>
    /// Compute the Solid Intersection of two sets of Breps.
    /// </summary>
    /// <param name="firstSet">First set of Breps.</param>
    /// <param name="secondSet">Second set of Breps.</param>
    /// <param name="tolerance">Tolerance to use for intersection operation.</param>
    /// <param name="manifoldOnly">If true, non-manifold input breps are ignored.</param>
    /// <returns>An array of Brep results or null on failure.</returns>
    /// <remarks>The solid orientation of the breps make a difference when calling this function</remarks>
    public static Brep[] CreateBooleanIntersection(IEnumerable<Brep> firstSet, IEnumerable<Brep> secondSet, double tolerance, bool manifoldOnly)
    {
      return BooleanIntDiffHelper(firstSet, secondSet, tolerance, true, manifoldOnly);
    }

    /// <summary>
    /// Compute the Solid Intersection of two Breps.
    /// </summary>
    /// <param name="firstBrep">First Brep for boolean intersection.</param>
    /// <param name="secondBrep">Second Brep for boolean intersection.</param>
    /// <param name="tolerance">Tolerance to use for intersection operation.</param>
    /// <returns>An array of Brep results or null on failure.</returns>
    /// <remarks>The solid orientation of the breps make a difference when calling this function</remarks>
    public static Brep[] CreateBooleanIntersection(Brep firstBrep, Brep secondBrep, double tolerance)
    {
      if (firstBrep == null) { throw new ArgumentNullException(nameof(firstBrep)); }
      if (secondBrep == null) { throw new ArgumentNullException(nameof(secondBrep)); }

      return BooleanIntDiffHelper(new[] { firstBrep }, new[] { secondBrep }, tolerance, true, true);
    }

    /// <summary>
    /// Compute the Solid Intersection of two Breps.
    /// </summary>
    /// <param name="firstBrep">First Brep for boolean intersection.</param>
    /// <param name="secondBrep">Second Brep for boolean intersection.</param>
    /// <param name="tolerance">Tolerance to use for intersection operation.</param>
    /// <param name="manifoldOnly">If true, non-manifold input breps are ignored.</param>
    /// <returns>An array of Brep results or null on failure.</returns>
    /// <remarks>The solid orientation of the breps make a difference when calling this function</remarks>
    public static Brep[] CreateBooleanIntersection(Brep firstBrep, Brep secondBrep, double tolerance, bool manifoldOnly)
    {
      if (firstBrep == null) { throw new ArgumentNullException(nameof(firstBrep)); }
      if (secondBrep == null) { throw new ArgumentNullException(nameof(secondBrep)); }

      return BooleanIntDiffHelper(new[] { firstBrep }, new[] { secondBrep }, tolerance, true, manifoldOnly);
    }

    /// <summary>
    /// Compute the Solid Difference of two sets of Breps.
    /// </summary>
    /// <param name="firstSet">First set of Breps (the set to subtract from).</param>
    /// <param name="secondSet">Second set of Breps (the set to subtract).</param>
    /// <param name="tolerance">Tolerance to use for difference operation.</param>
    /// <returns>An array of Brep results or null on failure.</returns>
    /// <remarks>The solid orientation of the breps make a difference when calling this function</remarks>
    /// <example>
    /// <code source='examples\vbnet\ex_booleandifference.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_booleandifference.cs' lang='cs'/>
    /// <code source='examples\py\ex_booleandifference.py' lang='py'/>
    /// </example>
    public static Brep[] CreateBooleanDifference(IEnumerable<Brep> firstSet, IEnumerable<Brep> secondSet, double tolerance)
    {
      return BooleanIntDiffHelper(firstSet, secondSet, tolerance, false, true);
    }

    /// <summary>
    /// Compute the Solid Difference of two sets of Breps.
    /// </summary>
    /// <param name="firstSet">First set of Breps (the set to subtract from).</param>
    /// <param name="secondSet">Second set of Breps (the set to subtract).</param>
    /// <param name="tolerance">Tolerance to use for difference operation.</param>
    /// <param name="manifoldOnly">If true, non-manifold input breps are ignored.</param>
    /// <returns>An array of Brep results or null on failure.</returns>
    /// <remarks>The solid orientation of the breps make a difference when calling this function</remarks>
    /// <example>
    /// <code source='examples\vbnet\ex_booleandifference.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_booleandifference.cs' lang='cs'/>
    /// <code source='examples\py\ex_booleandifference.py' lang='py'/>
    /// </example>
    public static Brep[] CreateBooleanDifference(IEnumerable<Brep> firstSet, IEnumerable<Brep> secondSet, double tolerance, bool manifoldOnly)
    {
      return BooleanIntDiffHelper(firstSet, secondSet, tolerance, false, manifoldOnly);
    }

    /// <summary>
    /// Compute the Solid Difference of two Breps.
    /// </summary>
    /// <param name="firstBrep">First Brep for boolean difference.</param>
    /// <param name="secondBrep">Second Brep for boolean difference.</param>
    /// <param name="tolerance">Tolerance to use for difference operation.</param>
    /// <returns>An array of Brep results or null on failure.</returns>
    /// <remarks>The solid orientation of the breps make a difference when calling this function</remarks>
    public static Brep[] CreateBooleanDifference(Brep firstBrep, Brep secondBrep, double tolerance)
    {
      if (firstBrep == null) { throw new ArgumentNullException(nameof(firstBrep)); }
      if (secondBrep == null) { throw new ArgumentNullException(nameof(secondBrep)); }

      return BooleanIntDiffHelper(new[] { firstBrep }, new[] { secondBrep }, tolerance, false, true);
    }

    /// <summary>
    /// Compute the Solid Difference of two Breps.
    /// </summary>
    /// <param name="firstBrep">First Brep for boolean difference.</param>
    /// <param name="secondBrep">Second Brep for boolean difference.</param>
    /// <param name="tolerance">Tolerance to use for difference operation.</param>
    /// <param name="manifoldOnly">If true, non-manifold input breps are ignored.</param>
    /// <returns>An array of Brep results or null on failure.</returns>
    /// <remarks>The solid orientation of the breps make a difference when calling this function</remarks>
    public static Brep[] CreateBooleanDifference(Brep firstBrep, Brep secondBrep, double tolerance, bool manifoldOnly)
    {
      if (firstBrep == null) { throw new ArgumentNullException(nameof(firstBrep)); }
      if (secondBrep == null) { throw new ArgumentNullException(nameof(secondBrep)); }

      return BooleanIntDiffHelper(new[] { firstBrep }, new[] { secondBrep }, tolerance, false, manifoldOnly);
    }

    /// <summary>
    /// Creates a hollowed out shell from a solid Brep. Function only operates on simple, solid, manifold Breps.
    /// </summary>
    /// <param name="brep">The solid Brep to shell.</param>
    /// <param name="facesToRemove">The indices of the Brep faces to remove. These surfaces are removed and the remainder is offset inward, using the outer parts of the removed surfaces to join the inner and outer parts.</param>
    /// <param name="distance">The distance, or thickness, for the shell. This is a signed distance value with respect to face normals and flipped faces.</param>
    /// <param name="tolerance">The offset tolerane. When in doubt, use the document's absolute tolerance.</param>
    /// <returns>An array of Brep results or null on failure.</returns>
    public static Brep[] CreateShell(Brep brep, IEnumerable<int> facesToRemove, double distance, double tolerance)
    {
      if (null == brep) { throw new ArgumentNullException(nameof(brep)); }
      if (null == facesToRemove) { throw new ArgumentNullException(nameof(facesToRemove)); }

      using (var output = new SimpleArrayBrepPointer())
      {
        IntPtr const_ptr_brep = brep.ConstPointer();
        var list_faces_to_remove = new List<int>(facesToRemove);
        int[] array_faces_to_remove = list_faces_to_remove.ToArray();
        IntPtr ptr_output = output.NonConstPointer();

        if (UnsafeNativeMethods.RHC_RhinoShellBrep(const_ptr_brep, array_faces_to_remove.Length, array_faces_to_remove, distance, tolerance, ptr_output) > 0)
          return output.ToNonConstArray();
        GC.KeepAlive(brep);
      }
      return null;
    }

    /// <summary>
    /// Joins the breps in the input array at any overlapping edges to form
    /// as few as possible resulting breps. There may be more than one brep in the result array.
    /// </summary>
    /// <param name="brepsToJoin">A list, an array or any enumerable set of breps to join.</param>
    /// <param name="tolerance">3d distance tolerance for detecting overlapping edges.</param>
    /// <returns>new joined breps on success, null on failure.</returns>
    public static Brep[] JoinBreps(IEnumerable<Brep> brepsToJoin, double tolerance)
    {
      if (null == brepsToJoin)
        return null;

      using (var input = new SimpleArrayBrepPointer())
      using (var output = new SimpleArrayBrepPointer())
      {
        foreach (Brep brep in brepsToJoin)
          input.Add(brep, true);

        IntPtr ptr_input = input.NonConstPointer();
        IntPtr ptr_output = output.NonConstPointer();

        Brep[] rc = null;
        if (UnsafeNativeMethods.RHC_RhinoJoinBreps(ptr_input, ptr_output, tolerance) > 0)
        {
          rc = output.ToNonConstArray();

          // 9 June 2010 - David
          // Flip Breps that are inside out.
          for (int i = 0; i < rc.Length; i++)
          {
            // 23-Jun-2016 Dale Fugier, http://mcneel.myjetbrains.com/youtrack/issue/RH-34697
            //if (!rc[i].IsSolid || rc[i].SolidOrientation != BrepSolidOrientation.Inward)
            //  continue;
            //rc[i].Flip();
            // from rhino3DocObjManager.cpp, line 1239
            if (BrepSolidOrientation.Inward == rc[i].SolidOrientation)
              rc[i].Flip();
          }
        }
        GC.KeepAlive(brepsToJoin);
        return rc;
      }
    }

    /// <summary>
    /// Combines two or more breps into one. A merge is like a boolean union that keeps the inside pieces. This
    /// function creates non-manifold Breps which in general are unusual in Rhino. You may want to consider using
    /// JoinBreps or CreateBooleanUnion functions instead.
    /// </summary>
    /// <param name="brepsToMerge">must contain more than one Brep.</param>
    /// <param name="tolerance">the tolerance to use when merging.</param>
    /// <returns>Single merged Brep on success. Null on error.</returns>
    /// <seealso cref="JoinBreps"/>
    public static Brep MergeBreps(IEnumerable<Brep> brepsToMerge, double tolerance)
    {
      if (null == brepsToMerge)
        return null;

      using (var input = new SimpleArrayBrepPointer())
      {
        foreach (Brep brep in brepsToMerge)
          input.Add(brep, true);

        IntPtr ptr_input = input.NonConstPointer();
        IntPtr ptr_newbrep = UnsafeNativeMethods.RHC_RhinoMergeBreps(ptr_input, tolerance);
        GC.KeepAlive(brepsToMerge);
        return ptr_newbrep != IntPtr.Zero ? new Brep(ptr_newbrep, null) : null;
      }
    }

    /// <summary>
    /// Constructs the contour curves for a brep at a specified interval.
    /// </summary>
    /// <param name="brepToContour">A brep or polysurface.</param>
    /// <param name="contourStart">A point to start.</param>
    /// <param name="contourEnd">A point to use as the end.</param>
    /// <param name="interval">The interaxial offset in world units.</param>
    /// <returns>An array with intersected curves. This array can be empty.</returns>
    /// <example>
    /// <code source='examples\vbnet\ex_makerhinocontours.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_makerhinocontours.cs' lang='cs'/>
    /// <code source='examples\py\ex_makerhinocontours.py' lang='py'/>
    /// </example>
    public static Curve[] CreateContourCurves(Brep brepToContour, Point3d contourStart, Point3d contourEnd, double interval)
    {
      IntPtr ptr_const_brep = brepToContour.ConstPointer();
      using (var outputcurves = new SimpleArrayCurvePointer())
      {
        double tolerance = RhinoDoc.ActiveDoc?.ModelAbsoluteTolerance ?? RhinoDoc.DefaultModelAbsoluteTolerance;
        IntPtr ptr_curves = outputcurves.NonConstPointer();
        int count = UnsafeNativeMethods.RHC_MakeRhinoContours2(ptr_const_brep, contourStart, contourEnd, interval, ptr_curves, tolerance);
        GC.KeepAlive(brepToContour);
        return 0 == count ? new Curve[0] : outputcurves.ToNonConstArray();
      }
    }

    /// <summary>
    /// Constructs the contour curves for a brep, using a slicing plane.
    /// </summary>
    /// <param name="brepToContour">A brep or polysurface.</param>
    /// <param name="sectionPlane">A plane.</param>
    /// <returns>An array with intersected curves. This array can be empty.</returns>
    public static Curve[] CreateContourCurves(Brep brepToContour, Plane sectionPlane)
    {
      IntPtr ptr_const_brep = brepToContour.ConstPointer();
      using (var outputcurves = new SimpleArrayCurvePointer())
      {
        double tolerance = RhinoDoc.ActiveDoc?.ModelAbsoluteTolerance ?? RhinoDoc.DefaultModelAbsoluteTolerance;
        IntPtr ptr_curves = outputcurves.NonConstPointer();
        int count = UnsafeNativeMethods.RHC_MakeRhinoContours3(ptr_const_brep, ref sectionPlane, ptr_curves, tolerance);
        GC.KeepAlive(brepToContour);
        return 0 == count ? new Curve[0] : outputcurves.ToNonConstArray();
      }
    }

    /// <summary>
    /// Create an array of analysis meshes for the brep using the specified settings.
    /// Meshes aren't set on the brep.
    /// </summary>
    /// <param name="brep"></param>
    /// <param name="state"> CurvatureAnalysisSettingsState </param>
    /// <returns>true if meshes were created</returns>
    public static Mesh[] CreateCurvatureAnalysisMesh(Brep brep, Rhino.ApplicationSettings.CurvatureAnalysisSettingsState state)
    {
      IntPtr ptr_brep = brep.NonConstPointer();
      using (var outmeshes = new SimpleArrayMeshPointer())
      {
        IntPtr ptr_meshes = outmeshes.NonConstPointer();
        int count = UnsafeNativeMethods.RHC_BrepCalcCurvatureAnalysisMesh(
          ptr_brep,
          state.GaussRange,
          state.MeanRange,
          state.MinRadiusRange,
          state.MaxRadiusRange,
          (int)state.Style,
          ptr_meshes);
        GC.KeepAlive(brep);
        return 0 == count ? new Mesh[0] : outmeshes.ToNonConstArray();
      }
    }
#endif
    #endregion

    #region constructors
    /// <summary>Initializes a new empty brep</summary>
    public Brep()
    {
      IntPtr ptr = UnsafeNativeMethods.ON_Brep_New(IntPtr.Zero);
      ConstructNonConstObject(ptr);
      ApplyMemoryPressure();
    }

    internal Brep(IntPtr ptr, object parent)
      : base(ptr, parent, -1)
    {
      if (null == parent)
      {
        ApplyMemoryPressure();
        if( IntPtr.Zero != ptr )
        {
          if (!_setTolerancesBoxesAndFlagsOnConstruct.HasValue)
          {
            _setTolerancesBoxesAndFlagsOnConstruct = true;
#if RHINO_SDK
            var settings = PersistentSettings.RhinoAppSettings;
            if (settings != null)
            {
              _setTolerancesBoxesAndFlagsOnConstruct = settings.GetBool("ComputeBrepCacheOnConstruct", true);
            }
#endif
          }
          if( _setTolerancesBoxesAndFlagsOnConstruct.Value )
          {
            UnsafeNativeMethods.ON_Brep_SetTolerancesBoxesAndFlags(ptr, true, true, true, true, true, true, true, true);
          }
        }
      }
    }
    static bool? _setTolerancesBoxesAndFlagsOnConstruct = null;

    /// <summary>
    /// Protected constructor used in serialization.
    /// </summary>
    protected Brep(SerializationInfo info, StreamingContext context)
      : base(info, context)
    {
      IntPtr ptr = NonConstPointer();
      if (IntPtr.Zero != ptr)
      {
        if (!_setTolerancesBoxesAndFlagsOnConstruct.HasValue)
        {
          _setTolerancesBoxesAndFlagsOnConstruct = true;
#if RHINO_SDK
          var settings = PersistentSettings.RhinoAppSettings;
          if (settings != null)
          {
            _setTolerancesBoxesAndFlagsOnConstruct = settings.GetBool("ComputeBrepCacheOnConstruct", true);
          }
#endif
        }
        if (_setTolerancesBoxesAndFlagsOnConstruct.Value)
        {
          UnsafeNativeMethods.ON_Brep_SetTolerancesBoxesAndFlags(ptr, true, true, true, true, true, true, true, true);
        }
      }
    }
    #endregion

    #region properties

    Collections.BrepVertexList m_vertexlist;
    /// <summary>
    /// Gets the brep vertices list accessor.
    /// </summary>
    public Collections.BrepVertexList Vertices
    {
      get { return m_vertexlist ?? (m_vertexlist = new Collections.BrepVertexList(this)); }
    }

    Collections.BrepSurfaceList m_surfacelist;
    /// <summary> Parametric surfaces used by faces </summary>
    public Collections.BrepSurfaceList Surfaces
    {
      get { return m_surfacelist ?? (m_surfacelist = new Collections.BrepSurfaceList(this)); }
    }

    Collections.BrepEdgeList m_edgelist;
    /// <summary>
    /// Gets the brep edges list accessor.
    /// </summary>
    public Collections.BrepEdgeList Edges
    {
      get { return m_edgelist ?? (m_edgelist = new Collections.BrepEdgeList(this)); }
    }

    Collections.BrepTrimList m_trimlist;
    /// <summary>
    /// Gets the brep trims list accessor.
    /// </summary>
    public Collections.BrepTrimList Trims
    {
      get { return m_trimlist ?? (m_trimlist = new Collections.BrepTrimList(this)); }
    }

    Collections.BrepLoopList m_looplist;
    /// <summary>
    /// Gets the brep loop list accessor.
    /// </summary>
    public Collections.BrepLoopList Loops
    {
      get { return m_looplist ?? (m_looplist = new Collections.BrepLoopList(this)); }
    }

    Collections.BrepFaceList m_facelist;
    /// <summary>
    /// Gets the brep faces list accessor.
    /// </summary>
    public Collections.BrepFaceList Faces
    {
      get { return m_facelist ?? (m_facelist = new Collections.BrepFaceList(this)); }
    }

    Collections.BrepCurveList m_c2;
    /// <summary>
    /// Parameter space trimming curves (used by trims)
    /// </summary>
    public Collections.BrepCurveList Curves2D
    {
      get { return m_c2 ?? (m_c2 = new Collections.BrepCurveList(this, true)); }
    }

    Collections.BrepCurveList m_c3;
    /// <summary>
    /// Pointers to 3d curves (used by edges)
    /// </summary>
    public Collections.BrepCurveList Curves3D
    {
      get { return m_c3 ?? (m_c3 = new Collections.BrepCurveList(this, false)); }
    }

    /// <summary>
    /// Determines whether this brep is a solid, or a closed oriented manifold.
    /// </summary>
    /// <example>
    /// <code source='examples\vbnet\ex_isbrepbox.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_isbrepbox.cs' lang='cs'/>
    /// <code source='examples\py\ex_isbrepbox.py' lang='py'/>
    /// </example>
    public bool IsSolid
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        // 23-Jun-2016 Dale Fugier, http://mcneel.myjetbrains.com/youtrack/issue/RH-34697
        //int rc = UnsafeNativeMethods.ON_Brep_GetInt(ptr, idxSolidOrientation);
        //return rc != 0;
        bool rc = UnsafeNativeMethods.ON_Brep_IsSolid(const_ptr_this);
        return rc;
      }
    }

    /// <summary>
    /// Gets the solid orientation state of this Brep.
    /// </summary>
    public BrepSolidOrientation SolidOrientation
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        int rc = UnsafeNativeMethods.ON_Brep_GetInt(const_ptr_this, UnsafeNativeMethods.BrepInt.SolidOrientation);
        return (BrepSolidOrientation)rc;
      }
    }

    /// <summary>
    /// Gets a value indicating whether or not the Brep is manifold. 
    /// Non-Manifold breps have at least one edge that is shared among three or more faces.
    /// </summary>
    public bool IsManifold
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        int rc = UnsafeNativeMethods.ON_Brep_GetInt(const_ptr_this, UnsafeNativeMethods.BrepInt.IsManifold);
        return rc != 0;
      }
    }

    /// <summary>
    /// Returns true if the Brep has a single face and that face is geometrically the same
    /// as the underlying surface.  I.e., the face has trivial trimming.
    /// <para>In this case, the surface is the first face surface. The flag
    /// Brep.Faces[0].OrientationIsReversed records the correspondence between the surface's
    /// natural parametric orientation and the orientation of the Brep.</para>
    /// <para>trivial trimming here means that there is only one loop curve in the brep
    /// and that loop curve is the same as the underlying surface boundary.</para>
    /// </summary>
    public bool IsSurface
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        return UnsafeNativeMethods.ON_Brep_FaceIsSurface(const_ptr_this, -1);
      }
    }

    /*
    BrepRegionTopology m_brep_region_topology;
    /// <summary>Rarely used region topology information.</summary>
    public BrepRegionTopology RegionTopology
    {
      get
      {
        return m_brep_region_topology ?? (m_brep_region_topology = new BrepRegionTopology(this));
      }
    }
    */

#if RHINO_SDK
    /// <summary>
    /// Gets an array containing all regions in this brep.
    /// </summary>
    /// <returns>An array of regions in this brep. This array can be empty, but not null.</returns>
    [ConstOperation]
    public BrepRegion[] GetRegions()
    {
      IntPtr const_ptr_this = ConstPointer();
      int count = UnsafeNativeMethods.ON_Brep_RegionTopologyCount(const_ptr_this, true);
      BrepRegion[] rc = new BrepRegion[count];
      for (int i = 0; i < count; i++)
        rc[i] = new BrepRegion(this, i);
      return rc;
    }
#endif

    #endregion

    #region methods
    /// <summary>
    /// Copies this brep.
    /// </summary>
    /// <returns>A brep.</returns>
    [ConstOperation]
    public override GeometryBase Duplicate()
    {
      IntPtr const_ptr_this = ConstPointer();
      IntPtr ptr_brep = UnsafeNativeMethods.ON_Brep_New(const_ptr_this);
      return new Brep(ptr_brep, null);
    }

    /// <summary>
    /// Same as <see cref="Duplicate()"/>, but already performs a cast to a brep.
    /// <para>This cast always succeeds.</para>
    /// </summary>
    /// <returns>A brep.</returns>
    [ConstOperation]
    public Brep DuplicateBrep()
    {
      return Duplicate() as Brep;
    }

    /// <summary>
    /// Copy a subset of this Brep into another Brep.
    /// </summary>
    /// <param name="faceIndices">
    /// array of face indices in this brep to copy.
    /// (If any values in faceIndices are out of range or if faceIndices contains
    /// duplicates, this function will return null.)
    /// </param>
    /// <returns>A brep, or null on error.</returns>
    [ConstOperation]
    public Brep DuplicateSubBrep(IEnumerable<int> faceIndices)
    {
      var indices = new List<int>(faceIndices);
      int[] indices_array = indices.ToArray();
      IntPtr ptr_const_this = ConstPointer();
      IntPtr ptr_brep = UnsafeNativeMethods.ON_Brep_SubBrep(ptr_const_this, indices_array.Length, indices_array);
      return CreateGeometryHelper(ptr_brep, null) as Brep;
    }

    /// <summary>
    /// Duplicate all the edges of this Brep.
    /// </summary>
    /// <returns>An array of edge curves.</returns>
    [ConstOperation]
    public Curve[] DuplicateEdgeCurves()
    {
      return DuplicateEdgeCurves(false);
    }

    /// <summary>
    /// Duplicate edges of this Brep.
    /// </summary>
    /// <param name="nakedOnly">
    /// If true, then only the "naked" edges are duplicated.
    /// If false, then all edges are duplicated.
    /// </param>
    /// <returns>Array of edge curves on success.</returns>
    /// <example>
    /// <code source='examples\vbnet\ex_dupborder.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_dupborder.cs' lang='cs'/>
    /// <code source='examples\py\ex_dupborder.py' lang='py'/>
    /// </example>
    [ConstOperation]
    public Curve[] DuplicateEdgeCurves(bool nakedOnly)
    {
      IntPtr const_ptr_this = ConstPointer();
      var output = new SimpleArrayCurvePointer();
      IntPtr ptr_output = output.NonConstPointer();

      UnsafeNativeMethods.ON_Brep_DuplicateEdgeCurves(const_ptr_this, ptr_output, nakedOnly, true, true);
      return output.ToNonConstArray();
    }

    /// <summary>
    /// Duplicate naked edges of this Brep
    /// </summary>
    /// <param name="outer"></param>
    /// <param name="inner"></param>
    /// <returns></returns>
    [ConstOperation]
    public Curve[] DuplicateNakedEdgeCurves(bool outer, bool inner)
    {
      IntPtr const_ptr_this = ConstPointer();
      using (var output = new SimpleArrayCurvePointer())
      {
        IntPtr output_curves_ptr = output.NonConstPointer();
        UnsafeNativeMethods.ON_Brep_DuplicateEdgeCurves(const_ptr_this, output_curves_ptr, true, outer, inner);
        return output.ToNonConstArray();
      }
    }
#if RHINO_SDK
    /// <summary>
    /// Constructs all the Wireframe curves for this Brep.
    /// </summary>
    /// <param name="density">Wireframe density. Valid values range between -1 and 99.</param>
    /// <returns>An array of Wireframe curves or null on failure.</returns>
    [ConstOperation]
    public Curve[] GetWireframe(int density)
    {
      IntPtr const_ptr_this = ConstPointer();
      var output = new SimpleArrayCurvePointer();
      IntPtr ptr_output = output.NonConstPointer();

      UnsafeNativeMethods.ON_Brep_GetWireframe(const_ptr_this, density, ptr_output);
      return output.ToNonConstArray();
    }
#endif

    /// <summary>
    /// Duplicate all the corner vertices of this Brep.
    /// </summary>
    /// <returns>An array or corner vertices.</returns>
    [ConstOperation]
    public Point3d[] DuplicateVertices()
    {
      IntPtr const_ptr_this = ConstPointer();

      using (var output_points = new SimpleArrayPoint3d())
      {
        IntPtr output_points_ptr = output_points.NonConstPointer();

        UnsafeNativeMethods.ON_Brep_DuplicateVertices(const_ptr_this, output_points_ptr);
        return output_points.ToArray();
      }
    }


    /// <summary>
    /// Reverses entire brep orientation of all faces.
    /// </summary>
    public void Flip()
    {
      IntPtr ptr_this = NonConstPointer();
      UnsafeNativeMethods.ON_Brep_Flip(ptr_this);
    }

    /// <summary>See if this and other are same brep geometry.</summary>
    /// <param name="other">other brep.</param>
    /// <param name="tolerance">tolerance to use when comparing control points.</param>
    /// <returns>true if breps are the same.</returns>
    [ConstOperation]
    public bool IsDuplicate(Brep other, double tolerance)
    {
      IntPtr const_ptr_this = ConstPointer();
      IntPtr const_ptr_other = other.ConstPointer();
      bool rc = UnsafeNativeMethods.ON_Brep_IsDuplicate(const_ptr_this, const_ptr_other, tolerance);
      GC.KeepAlive(other);
      return rc;
    }

    /// <summary>
    /// Tests the brep to see if its topology information is valid.
    /// </summary>
    /// <param name="log">
    /// If the brep topology is not valid, then a brief english description of
    /// the problem is appended to the log.  The information appended to log is
    /// suitable for low-level debugging purposes by programmers and is not
    /// intended to be useful as a high level user interface tool.
    /// </param>
    /// <returns>true if the topology is valid; false otherwise.</returns>
    [ConstOperation]
    public bool IsValidTopology(out string log)
    {
      using (var sh = new StringHolder())
      {
        IntPtr const_ptr_this = ConstPointer();
        IntPtr ptr_string = sh.NonConstPointer();
        bool rc = UnsafeNativeMethods.ON_Brep_IsValidTest(const_ptr_this, UnsafeNativeMethods.BrepValidTest.IsValidTopology, ptr_string);
        log = sh.ToString();
        return rc;
      }
    }

    /// <summary>
    /// Expert user function that tests the brep to see if its geometry information is valid.
    /// The value of brep.IsValidTopology() must be true before brep.IsValidGeometry() can be
    /// safely called.
    /// </summary>
    /// <param name="log">
    /// If the brep geometry is not valid, then a brief description of the problem
    /// in English is assigned to this out parameter. The information is suitable for
    /// low-level debugging purposes by programmers and is not intended to be
    /// useful as a high level user interface tool. Otherwise, <see cref="string.Empty"/>.
    /// </param>
    /// <returns>A value that indicates whether the geometry is valid.</returns>
    [ConstOperation]
    public bool IsValidGeometry(out string log)
    {
      using (var sh = new StringHolder())
      {
        IntPtr const_ptr_this = ConstPointer();
        IntPtr ptr_string = sh.NonConstPointer();
        bool rc = UnsafeNativeMethods.ON_Brep_IsValidTest(const_ptr_this, UnsafeNativeMethods.BrepValidTest.IsValidGeometry, ptr_string);
        log = sh.ToString();
        return rc;
      }
    }

    /// <summary>
    /// Expert user function that tests the brep to see if its tolerances and
    /// flags are valid.  The values of brep.IsValidTopology() and
    /// brep.IsValidGeometry() must be true before brep.IsValidTolerancesAndFlags()
    /// can be safely called.
    /// </summary>
    /// <param name="log">
    /// If the brep tolerance or flags are not valid, then a brief description 
    /// of the problem in English is assigned to this out parameter. The information is
    /// suitable for low-level debugging purposes by programmers and is not
    /// intended to be useful as a high level user interface tool. Otherwise, <see cref="string.Empty"/>.
    /// </param>
    /// <returns>A value that indicates </returns>
    [ConstOperation]
    public bool IsValidTolerancesAndFlags(out string log)
    {
      using (var sh = new StringHolder())
      {
        IntPtr const_ptr_this = ConstPointer();
        IntPtr ptr_string = sh.NonConstPointer();
        bool rc = UnsafeNativeMethods.ON_Brep_IsValidTest(const_ptr_this, UnsafeNativeMethods.BrepValidTest.IsValidTolerancesAndFlags, ptr_string);
        log = sh.ToString();
        return rc;
      }
    }

#if RHINO_SDK
    /// <summary>
    /// Finds a point on the brep that is closest to testPoint.
    /// </summary>
    /// <param name="testPoint">Base point to project to brep.</param>
    /// <returns>The point on the Brep closest to testPoint or Point3d.Unset if the operation failed.</returns>
    [ConstOperation]
    public Point3d ClosestPoint(Point3d testPoint)
    {
      Point3d pt_cp;
      Vector3d vc_cp;
      ComponentIndex ci;
      double s, t;

      if (!ClosestPoint(testPoint, out pt_cp, out ci, out s, out t, double.MaxValue, out vc_cp))
      {
        return Point3d.Unset;
      }
      return pt_cp;
    }

    /// <summary>
    /// Finds a point on a brep that is closest to testPoint.
    /// </summary>
    /// <param name="testPoint">base point to project to surface.</param>
    /// <param name="closestPoint">location of the closest brep point.</param>
    /// <param name="ci">Component index of the brep component that contains
    /// the closest point. Possible types are brep_face, brep_edge or brep_vertex.</param>
    /// <param name="s">If the ci type is brep_edge, then s is the parameter
    /// of the closest edge point.</param>
    /// <param name="t">If the ci type is brep_face, then (s,t) is the parameter
    /// of the closest edge point.</param>
    /// <param name="maximumDistance">
    /// If maximumDistance &gt; 0, then only points whose distance
    /// is &lt;= maximumDistance will be returned. Using a positive
    /// value of maximumDistance can substantially speed up the search.</param>
    /// <param name="normal">The normal to the face if ci is a brep_face
    /// and the tangent to the edge if ci is brep_edge.
    /// </param>
    /// <returns>true if the operation succeeded; otherwise, false.</returns>
    [ConstOperation]
    public bool ClosestPoint(Point3d testPoint,
      out Point3d closestPoint, out ComponentIndex ci,
      out double s, out double t, double maximumDistance, out Vector3d normal)
    {
      ci = Geometry.ComponentIndex.Unset;
      s = RhinoMath.UnsetValue;
      t = RhinoMath.UnsetValue;
      normal = Vector3d.Unset;
      closestPoint = Point3d.Unset;

      IntPtr const_ptr_this = ConstPointer();
      bool rc = UnsafeNativeMethods.ON_Brep_GetClosestPoint(const_ptr_this, testPoint,
        ref closestPoint, ref ci, ref s, ref t, maximumDistance, ref normal);

      return rc;
    }

    /// <summary>
    /// Find the indices of all brep faces, edges and vertices that are within tolerance of a test-point.
    /// </summary>
    /// <param name="point">Test point.</param>
    /// <param name="tolerance">Coincidence tolerance.</param>
    /// <param name="faces">Array of BrepFace indices.</param>
    /// <param name="edges">Array of BrepEdge indices.</param>
    /// <param name="vertices">Array of BrepVertex indices.</param>
    [ConstOperation]
    public void FindCoincidentBrepComponents(
      Point3d point,
      double tolerance,
      out int[] faces,
      out int[] edges,
      out int[] vertices)
    {
      tolerance = Math.Max(tolerance, 1e-64);

      if (!point.IsValid)
      {
        faces = new int[0];
        edges = new int[0];
        vertices = new int[0];
        return;
      }

      // No possible solutions if point is too far away from the brep.
      BoundingBox box = GetBoundingBox(false);
      if (box.ClosestPoint(point).DistanceTo(point) > tolerance)
      {
        faces = new int[0];
        edges = new int[0];
        vertices = new int[0];
        return;
      }

      // Arrays of lengths that match the vertices, edges and faces.
      // FALSE = do not include, TRUE = include
      bool[] vertexMap = new bool[Vertices.Count];
      bool[] edgeMap = new bool[Edges.Count];
      bool[] faceMap = new bool[Faces.Count];

      // First iterate over all vertices.
      foreach (BrepVertex vertex in Vertices)
      {
        double distance = vertex.Location.DistanceTo(point);
        if (distance <= tolerance)
        {
          // This vertex is within tolerance of the point, 
          // which means that all adjacent edges and faces are too.
          vertexMap[vertex.VertexIndex] = true;
          int[] edgeList = vertex.EdgeIndices();
          foreach (int edgeIndex in edgeList)
            if (!edgeMap[edgeIndex])
            {
              edgeMap[edgeIndex] = true;
              int[] faceList = Edges[edgeIndex].AdjacentFaces();
              for (int i = 0; i < faceList.Length; i++)
                faceMap[faceList[i]] = true;
            }
        }
      }

      // Then iterate over all edges.
      foreach (BrepEdge edge in Edges)
        if (!edgeMap[edge.EdgeIndex])
          if (edge.ClosestPoint(point, out double t, tolerance))
          {
            double distance = edge.PointAt(t).DistanceTo(point);
            if (distance <= tolerance)
            {
              // This edge is within tolerance of the point, 
              // which means that all adjacent faces are too.
              edgeMap[edge.EdgeIndex] = true;
              int[] faceList = edge.AdjacentFaces();
              for (int i = 0; i < faceList.Length; i++)
                faceMap[faceList[i]] = true;
            }
          }

      // Finally iterate over all faces.
      foreach (BrepFace face in Faces)
        if (!faceMap[face.FaceIndex])
          if (face.ClosestPoint(point, out double u, out double v)) // Remember, this uses ON_Surface closest point logic.
            if (face.IsPointOnFace(u, v) != PointFaceRelation.Exterior)
            {
              double distance = face.PointAt(u, v).DistanceTo(point);
              if (distance <= tolerance)
                faceMap[face.FaceIndex] = true;
            }

      faces = ToIndexArray(faceMap);
      edges = ToIndexArray(edgeMap);
      vertices = ToIndexArray(vertexMap);
    }
    /// <summary>
    /// Set all indexed values in a bool array to true.
    /// Utility function for above.
    /// </summary>
    /// <param name="ints">Array of indices.</param>
    /// <param name="bools">Bool array.</param>
    private static void AssignIndices(int[] ints, bool[] bools)
    {
      for (int i = 0; i < ints.Length; i++)
        bools[ints[i]] = true;
    }
    private static int[] ToIndexArray(bool[] map)
    {
      int count = 0;
      for (int i = 0; i < map.Length; i++)
        if (map[i]) count++;

      int[] array = new int[count];
      int k = 0;

      if (count > 0)
        for (int i = 0; i < map.Length; i++)
          if (map[i])
            array[k++] = i;

      return array;
    }

    /// <summary>
    /// Determines if point is inside a Brep.  This question only makes sense when
    /// the brep is a closed and manifold.  This function does not not check for
    /// closed or manifold, so result is not valid in those cases.  Intersects
    /// a line through point with brep, finds the intersection point Q closest
    /// to point, and looks at face normal at Q.  If the point Q is on an edge
    /// or the intersection is not transverse at Q, then another line is used.
    /// </summary>
    /// <param name="point">3d point to test.</param>
    /// <param name="tolerance">
    /// 3d distance tolerance used for intersection and determining strict inclusion.
    /// A good default is RhinoMath.SqrtEpsilon.
    /// </param>
    /// <param name="strictlyIn">
    /// if true, point is in if inside brep by at least tolerance.
    /// if false, point is in if truly in or within tolerance of boundary.
    /// </param>
    /// <returns>
    /// true if point is in, false if not.
    /// </returns>
    [ConstOperation]
    public bool IsPointInside(Point3d point, double tolerance, bool strictlyIn)
    {
      IntPtr const_ptr_this = ConstPointer();
      return UnsafeNativeMethods.RHC_RhinoIsPointInBrep(const_ptr_this, point, tolerance, strictlyIn);
    }

    /// <summary>
    /// Returns a new Brep that is equivalent to this Brep with all planar holes capped.
    /// </summary>
    /// <param name="tolerance">Tolerance to use for capping.</param>
    /// <returns>New brep on success. null on error.</returns>
    [ConstOperation]
    public Brep CapPlanarHoles(double tolerance)
    {
      IntPtr const_ptr_this = ConstPointer();
      IntPtr ptr_brep = UnsafeNativeMethods.RHC_CapPlanarHoles(const_ptr_this, tolerance);
      return IntPtr.Zero == ptr_brep ? null : new Brep(ptr_brep, null);
    }

    /// <summary>
    /// If any edges of this brep overlap edges of otherBrep, merge a copy of otherBrep into this
    /// brep joining all edges that overlap within tolerance.
    /// </summary>
    /// <param name="otherBrep">Brep to be added to this brep.</param>
    /// <param name="tolerance">3d distance tolerance for detecting overlapping edges.</param>
    /// <param name="compact">if true, set brep flags and tolerances, remove unused faces and edges.</param>
    /// <returns>true if any edges were joined.</returns>
    /// <remarks>
    /// if no edges overlap, this brep is unchanged.
    /// otherBrep is copied if it is merged with this, and otherBrep is always unchanged
    /// Use this to join a list of breps in a series.
    /// When joining multiple breps in series, compact should be set to false.
    /// Call compact on the last Join.
    /// </remarks>
    public bool Join(Brep otherBrep, double tolerance, bool compact)
    {
      IntPtr ptr_this = NonConstPointer();
      IntPtr const_ptr_other = otherBrep.ConstPointer();
      bool rc = UnsafeNativeMethods.RHC_RhinoJoinBreps2(ptr_this, const_ptr_other, tolerance, compact);
      GC.KeepAlive(otherBrep);
      return rc;
    }

    /// <summary>
    /// Joins naked edge pairs within the same brep that overlap within tolerance.
    /// </summary>
    /// <param name="tolerance">The tolerance value.</param>
    /// <returns>number of joins made.</returns>
    public int JoinNakedEdges(double tolerance)
    {
      IntPtr ptr_this = NonConstPointer();
      return UnsafeNativeMethods.RHC_RhinoJoinBrepNakedEdges(ptr_this, tolerance);
    }

    /// <summary>
    /// Merges adjacent coplanar faces into single faces.
    /// </summary>
    /// <param name="tolerance">
    /// Tolerance for determining when edges are adjacent.
    /// When in doubt, use the document's ModelAbsoluteTolerance property.
    /// </param>
    /// <returns>true if faces were merged, false if no faces were merged.</returns>
    public bool MergeCoplanarFaces(double tolerance)
    {
      return MergeCoplanarFaces(tolerance, RhinoMath.UnsetValue);
    }

    /// <summary>
    /// Merges adjacent coplanar faces into single faces.
    /// </summary>
    /// <param name="tolerance">
    /// Tolerance for determining when edges are adjacent.
    /// When in doubt, use the document's ModelAbsoluteTolerance property.
    /// </param>
    /// <param name="angleTolerance">
    /// Angle tolerance, in radians, for determining when faces are parallel.
    /// When in doubt, use the document's ModelAngleToleranceRadians property.
    /// </param>
    /// <returns>true if faces were merged, false if no faces were merged.</returns>
    public bool MergeCoplanarFaces(double tolerance, double angleTolerance)
    {
      IntPtr ptr_this = NonConstPointer();
      return UnsafeNativeMethods.RHC_RhinoMergeCoplanarFaces(ptr_this, tolerance, angleTolerance);
    }

    /// <summary>
    /// Splits a Brep into pieces using a Brep as a cutter.
    /// </summary>
    /// <param name="cutter">The Brep to use as a cutter.</param>
    /// <param name="intersectionTolerance">The tolerance with which to compute intersections.</param>
    /// <returns>A new array of Breps. This array can be empty.</returns>
    [ConstOperation]
    public Brep[] Split(Brep cutter, double intersectionTolerance)
    {
      bool raised;
      return Split(cutter, intersectionTolerance, out raised);
    }

    /// <summary>
    /// Splits a Brep into pieces using a Brep as a cutter.
    /// </summary>
    /// <param name="cutter">The Brep to use as a cutter.</param>
    /// <param name="intersectionTolerance">The tolerance with which to compute intersections.</param>
    /// <param name="toleranceWasRaised">
    /// Set to true if the split failed at intersectionTolerance but succeeded
    /// when the tolerance was increased to twice intersectionTolerance.
    /// </param>
    /// <returns>A new array of Breps. This array can be empty.</returns>
    [ConstOperation]
    public Brep[] Split(Brep cutter, double intersectionTolerance, out bool toleranceWasRaised)
    {
      toleranceWasRaised = false;
      IntPtr const_ptr_this = ConstPointer();
      IntPtr const_ptr_cutter = cutter.ConstPointer();
      using (var breps = new SimpleArrayBrepPointer())
      {
        IntPtr const_ptr_breps = breps.NonConstPointer();
        UnsafeNativeMethods.RHC_RhinoBrepSplit(const_ptr_this, const_ptr_cutter, const_ptr_breps, intersectionTolerance, ref toleranceWasRaised);
        GC.KeepAlive(cutter);
        return breps.ToNonConstArray();
      }
    }

    /// <summary>
    /// Splits a Brep into pieces using Breps as cutters.
    /// </summary>
    /// <param name="cutters">One or more Breps to use as cutters.</param>
    /// <param name="intersectionTolerance">The tolerance with which to compute intersections.</param>
    /// <returns>A new array of Breps. This array can be empty.</returns>
    [ConstOperation]
    public Brep[] Split(IEnumerable<Brep> cutters, double intersectionTolerance)
    {
      IntPtr const_ptr_this = ConstPointer();
      using (var inbreps = new SimpleArrayBrepPointer())
      {
        foreach (Brep b in cutters)
          inbreps.Add(b, true);
        using (var outbreps = new SimpleArrayBrepPointer())
        {
          IntPtr const_ptr_inbreps = inbreps.ConstPointer();
          IntPtr ptr_outbreps = outbreps.NonConstPointer();
          UnsafeNativeMethods.RHC_RhinoBrepSplit2(const_ptr_this, const_ptr_inbreps, ptr_outbreps, intersectionTolerance);
          GC.KeepAlive(cutters);
          return outbreps.ToNonConstArray();
        }
      }
    }

    /// <summary>
    /// Splits a Brep into pieces using curves, at least partially on the Brep, as cutters.
    /// </summary>
    /// <param name="cutters">The splitting curves. Only the portion of the curve on the Brep surface will be used for cutting.</param>
    /// <param name="intersectionTolerance">The tolerance with which to compute intersections.</param>
    /// <returns>A new array of Breps. This array can be empty.</returns>
    [ConstOperation]
    public Brep[] Split(IEnumerable<Curve> cutters, double intersectionTolerance)
    {
      IntPtr const_ptr_this = ConstPointer();
      using (var incurves = new SimpleArrayCurvePointer(cutters))
      using (var outbreps = new SimpleArrayBrepPointer())
      {
        IntPtr const_ptr_incurves = incurves.ConstPointer();
        IntPtr ptr_outbreps = outbreps.NonConstPointer();
        UnsafeNativeMethods.RHC_RhinoBrepSplit3(const_ptr_this, const_ptr_incurves, ptr_outbreps, intersectionTolerance);
        GC.KeepAlive(cutters);
        return outbreps.ToNonConstArray();
      }
    }

    /// <summary>
    /// Splits a Brep into pieces using a combination of curves, to be extruded, and Breps as cutters.
    /// </summary>
    /// <param name="cutters">The curves, surfaces, faces and Breps to be used as cutters. Any other geometry is ignored.</param>
    /// <param name="normal">A construction plane normal, used in deciding how to extrude a curve into a cutter.</param>
    /// <param name="planView">Set true if the assume view is a plan, or parallel projection, view.</param>
    /// <param name="intersectionTolerance">The tolerance with which to compute intersections.</param>
    /// <returns>A new array of Breps. This array can be empty.</returns>
    /// <remarks>
    /// A Curve in cutters is extruded to produce a surface to use as a cutter. The extrusion direction is choosen, as in the Rhino Split command,
    /// based on the properties of the active view. In particular the construction plane Normal and whether the active view is a plan view, 
    /// a parallel projection with construction plane normal as the view direction. If planView is false and the curve is planar then the curve
    /// is extruded perpendicular to the curve, otherwise the curve is extruded in the normal direction.
    /// </remarks>
    [ConstOperation]
    public Brep[] Split(IEnumerable<GeometryBase> cutters, Vector3d normal, bool planView, double intersectionTolerance)
    {
      IntPtr const_ptr_this = ConstPointer();
      using (var ingeometry = new SimpleArrayGeometryPointer(cutters))
      using (var outbreps = new SimpleArrayBrepPointer())
      {
        IntPtr const_ptr_ingeometry = ingeometry.ConstPointer();
        IntPtr ptr_outbreps = outbreps.NonConstPointer();
        UnsafeNativeMethods.RHC_RhinoBrepSplit4(const_ptr_this, const_ptr_ingeometry, normal, planView, intersectionTolerance, ptr_outbreps);
        GC.KeepAlive(cutters);
        return outbreps.ToNonConstArray();
      }
    }

    /// <summary>
    /// Trims a brep with an oriented cutter. The parts of the brep that lie inside
    /// (opposite the normal) of the cutter are retained while the parts to the
    /// outside (in the direction of the normal) are discarded.  If the Cutter is
    /// closed, then a connected component of the Brep that does not intersect the
    /// cutter is kept if and only if it is contained in the inside of cutter.
    /// That is the region bounded by cutter opposite from the normal of cutter,
    /// If cutter is not closed all these components are kept.
    /// </summary>
    /// <param name="cutter">A cutting brep.</param>
    /// <param name="intersectionTolerance">A tolerance value with which to compute intersections.</param>
    /// <returns>This Brep is not modified, the trim results are returned in an array.</returns>
    [ConstOperation]
    public Brep[] Trim(Brep cutter, double intersectionTolerance)
    {
      IntPtr const_ptr_this = ConstPointer();
      IntPtr const_ptr_cutter = cutter.ConstPointer();
      using (var rc = new SimpleArrayBrepPointer())
      {
        IntPtr ptr_breps = rc.NonConstPointer();
        if (UnsafeNativeMethods.RHC_RhinoBrepTrim1(const_ptr_this, const_ptr_cutter, intersectionTolerance, ptr_breps) > 0)
          return rc.ToNonConstArray();
        GC.KeepAlive(cutter);
      }
      return new Brep[0];
    }

    /// <summary>
    /// Trims a Brep with an oriented cutter.  The parts of Brep that lie inside
    /// (opposite the normal) of the cutter are retained while the parts to the
    /// outside ( in the direction of the normal ) are discarded. A connected
    /// component of Brep that does not intersect the cutter is kept if and only
    /// if it is contained in the inside of Cutter.  That is the region bounded by
    /// cutter opposite from the normal of cutter, or in the case of a Plane cutter
    /// the halfspace opposite from the plane normal.
    /// </summary>
    /// <param name="cutter">A cutting plane.</param>
    /// <param name="intersectionTolerance">A tolerance value with which to compute intersections.</param>
    /// <returns>This Brep is not modified, the trim results are returned in an array.</returns>
    [ConstOperation]
    public Brep[] Trim(Plane cutter, double intersectionTolerance)
    {
      IntPtr const_ptr_this = ConstPointer();
      using (var rc = new SimpleArrayBrepPointer())
      {
        IntPtr ptr_breps = rc.NonConstPointer();
        if (UnsafeNativeMethods.RHC_RhinoBrepTrim2(const_ptr_this, ref cutter, intersectionTolerance, ptr_breps) > 0)
          return rc.ToNonConstArray();
      }
      return new Brep[0];
    }

    /// <summary>
    /// Unjoins, or separates, edges within the Brep. Note, seams in closed surfaces will not separate.
    /// </summary>
    /// <param name="edgesToUnjoin">The indices of the Brep edges to unjoin.</param>
    /// <returns>This Brep is not modified, the trim results are returned in an array.</returns>
    [ConstOperation]
    public Brep[] UnjoinEdges(IEnumerable<int> edgesToUnjoin)
    {
      IntPtr const_ptr_this = ConstPointer();
      if (null == edgesToUnjoin) { throw new ArgumentNullException(nameof(edgesToUnjoin)); }
      using (var output = new SimpleArrayBrepPointer())
      {
        var list_edges_to_unjoin = new List<int>(edgesToUnjoin);
        int[] array_edges_to_unjoin = list_edges_to_unjoin.ToArray();
        IntPtr ptr_output = output.NonConstPointer();
        if (UnsafeNativeMethods.RHC_RhinoUnjoinEdges(const_ptr_this, array_edges_to_unjoin.Length, array_edges_to_unjoin, ptr_output))
          return output.ToNonConstArray();
      }
      return new Brep[0];
    }

    /// <summary>
    /// Joins two naked edges, or edges that are coincident or close together.
    /// </summary>
    /// <param name="edgeIndex0">The first edge index.</param>
    /// <param name="edgeIndex1">The second edge index.</param>
    /// <param name="joinTolerance">The join tolerance.</param>
    /// <param name="compact">
    /// If joining more than one edge pair and want the edge indices of subsequent pairs to remain valid, 
    /// set to false. But then call Brep.Compact() on the final result.
    /// </param>
    /// <returns>true if successful, false otherwise.</returns>
    public bool JoinEdges(int edgeIndex0, int edgeIndex1, double joinTolerance, bool compact)
    {
      IntPtr ptr_this = NonConstPointer();
      return UnsafeNativeMethods.RHC_RhinoJoinEdges(ptr_this, edgeIndex0, edgeIndex1, joinTolerance, compact);
    }

    /// <summary>
    /// Transform an array of Brep components, bend neighbors to match, and leave the rest fixed.
    /// </summary>
    /// <param name="components">The Brep components to transform.</param>
    /// <param name="xform">The transformation to apply.</param>
    /// <param name="tolerance">The desired fitting tolerance to use when bending faces that share edges with both fixed and transformed components.</param>
    /// <param name="timeLimit">
    /// If the deformation is extreme, it can take a long time to calculate the result.
    /// If time_limit > 0, then the value specifies the maximum amount of time in seconds you want to spend before giving up.
    /// </param>
    /// <param name="useMultipleThreads">True if multiple threads can be used.</param>
    /// <returns>true if successful, false otherwise.</returns>
    public bool TransformComponent(IEnumerable<ComponentIndex> components, Transform xform, double tolerance, double timeLimit, bool useMultipleThreads)
    {
      IntPtr ptr_this = NonConstPointer();
      ComponentIndex[] arr_components = components.ToArray();
      bool rc = UnsafeNativeMethods.RHC_RhinoTransformBrepComponents(
        ptr_this,
        arr_components.Length, arr_components,
        ref xform, tolerance, timeLimit, useMultipleThreads
        );
      return rc;
    }

    /// <summary>
    /// Compute the Area of the Brep. If you want proper Area data with moments 
    /// and error information, use the AreaMassProperties class.
    /// </summary>
    /// <returns>The area of the Brep.</returns>
    [ConstOperation]
    public double GetArea()
    {
      return GetArea(1.0e-6, 1.0e-6);
    }
    /// <summary>
    /// Compute the Area of the Brep. If you want proper Area data with moments 
    /// and error information, use the AreaMassProperties class.
    /// </summary>
    /// <param name="relativeTolerance">Relative tolerance to use for area calculation.</param>
    /// <param name="absoluteTolerance">Absolute tolerance to use for area calculation.</param>
    /// <returns>The area of the Brep.</returns>
    [ConstOperation]
    public double GetArea(double relativeTolerance, double absoluteTolerance)
    {
      IntPtr const_ptr_this = ConstPointer();
      double area = UnsafeNativeMethods.ON_Brep_Area(const_ptr_this, relativeTolerance, absoluteTolerance);
      return area;
    }

    /// <summary>
    /// Compute the Volume of the Brep. If you want proper Volume data with moments 
    /// and error information, use the VolumeMassProperties class.
    /// </summary>
    /// <returns>The volume of the Brep.</returns>
    [ConstOperation]
    public double GetVolume()
    {
      return GetVolume(1.0e-6, 1.0e-6);
    }
    /// <summary>
    /// Compute the Volume of the Brep. If you want proper Volume data with moments 
    /// and error information, use the VolumeMassProperties class.
    /// </summary>
    /// <param name="relativeTolerance">Relative tolerance to use for area calculation.</param>
    /// <param name="absoluteTolerance">Absolute tolerance to use for area calculation.</param>
    /// <returns>The volume of the Brep.</returns>
    [ConstOperation]
    public double GetVolume(double relativeTolerance, double absoluteTolerance)
    {
      IntPtr const_ptr_this = ConstPointer();
      return UnsafeNativeMethods.ON_Brep_Volume(const_ptr_this, relativeTolerance, absoluteTolerance);
    }
#endif
    /// <summary>
    /// Add a 2d curve used by the brep trims
    /// </summary>
    /// <param name="curve"></param>
    /// <returns>
    /// Index used to reference this geometry in the trimming curve list
    /// </returns>
    public int AddTrimCurve(Curve curve)
    {
      // Use const curve, the C function will duplicate the curve
      IntPtr const_ptr_curve = curve.ConstPointer();
      IntPtr ptr_this = NonConstPointer();
      int rc = UnsafeNativeMethods.ON_Brep_AddTrimCurve(ptr_this, const_ptr_curve);
      GC.KeepAlive(curve);
      return rc;
    }

    /// <summary>
    /// Add a 3d curve used by the brep edges
    /// </summary>
    /// <param name="curve"></param>
    /// <returns>
    /// Index used to reference this geometry in the edge curve list
    /// </returns>
    public int AddEdgeCurve(Curve curve)
    {
      // Use const curve, the C function will duplicate the curve
      IntPtr const_ptr_curve = curve.ConstPointer();
      IntPtr ptr_this = NonConstPointer();
      int rc = UnsafeNativeMethods.ON_Brep_AddEdgeCurve(ptr_this, const_ptr_curve);
      GC.KeepAlive(curve);
      return rc;
    }

    /// <summary>
    /// Adds a 3D surface used by BrepFace.
    /// </summary>
    /// <param name="surface">A copy of the surface is added to this brep.</param>
    /// <returns>
    /// Index that should be used to reference the geometry.
    /// <para>-1 is returned if the input is not acceptable.</para>
    /// </returns>
    public int AddSurface(Surface surface)
    {
      IntPtr const_ptr_surface = surface.ConstPointer();
      IntPtr ptr_this = NonConstPointer();
      int rc = UnsafeNativeMethods.ON_Brep_AddSurface(ptr_this, const_ptr_surface);
      GC.KeepAlive(surface);
      return rc;
    }

    /// <summary>
    /// Appends a copy of another brep to this and updates indices of appended
    /// brep parts.  Duplicates are not removed
    /// </summary>
    /// <param name="other"></param>
    public void Append(Brep other)
    {
      IntPtr const_ptr_other = other.ConstPointer();
      IntPtr ptr_this = NonConstPointer();
      UnsafeNativeMethods.ON_Brep_Append(ptr_this, const_ptr_other);
      GC.KeepAlive(other);
    }

    /// <summary>
    /// This function can be used to compute vertex information for a
    /// b-rep when everything but the Vertices array is properly filled in.
    /// It is intended to be used when creating a Brep from a 
    /// definition that does not include explicit vertex information.
    /// </summary>
    public void SetVertices()
    {
      IntPtr ptr_this = NonConstPointer();
      UnsafeNativeMethods.ON_Brep_SetVertices(ptr_this);
    }

    /// <summary>
    /// This function can be used to set the BrepTrim::m_iso
    /// flag. It is intended to be used when creating a Brep from
    /// a definition that does not include compatible parameter space
    /// type information.
    /// </summary>
    public void SetTrimIsoFlags()
    {
      IntPtr ptr_this = NonConstPointer();
      UnsafeNativeMethods.ON_Brep_SetTrimIsoFlags(ptr_this);
    }

    /// <summary>
    /// Set tolerances and flags in a Brep.
    /// </summary>
    public void SetTolerancesBoxesAndFlags()
    {
      SetTolerancesBoxesAndFlags(false, true, true, true, true, true, true, true);
    }

    /// <summary>
    /// Set tolerances and flags in a Brep.
    /// </summary>
    /// <param name="bLazy">If true, only flags and tolerances that are not set will be calculated.</param>
    /// <param name="bSetVertexTolerances">Set true to compute BrepVertex tolerances.</param>
    /// <param name="bSetEdgeTolerances">Set true to compute BrepEdge tolerances.</param>
    /// <param name="bSetTrimTolerances">Set true to compute BrepTrim tolerances.</param>
    /// <param name="bSetTrimIsoFlags">Set true to compute BrepTrim.IsoStatus values.</param>
    /// <param name="bSetTrimTypeFlags">Set true to compute BrepTrim.TrimType values.</param>
    /// <param name="bSetLoopTypeFlags">Set true to compute BrepLoop.BrepLoopType values.</param>
    /// <param name="bSetTrimBoxes">Set true to compute BrepTrim bounding boxes.</param>
    public void SetTolerancesBoxesAndFlags(
      bool bLazy,
      bool bSetVertexTolerances,
      bool bSetEdgeTolerances,
      bool bSetTrimTolerances,
      bool bSetTrimIsoFlags,
      bool bSetTrimTypeFlags,
      bool bSetLoopTypeFlags,
      bool bSetTrimBoxes
      )
    {
      IntPtr ptr_this = NonConstPointer();
      UnsafeNativeMethods.ON_Brep_SetTolerancesBoxesAndFlags(ptr_this,
        bLazy,
        bSetVertexTolerances,
        bSetEdgeTolerances,
        bSetTrimTolerances,
        bSetTrimIsoFlags,
        bSetTrimTypeFlags,
        bSetLoopTypeFlags,
        bSetTrimBoxes
        );
    }

#if RHINO_SDK
    /// <summary>
    /// No support is available for this function.
    /// <para>Expert user function used by MakeValidForV2 to convert trim
    /// curves from one surface to its NURBS form. After calling this function,
    /// you need to change the surface of the face to a NurbsSurface.</para>
    /// </summary>
    /// <param name="face">
    /// Face whose underlying surface has a parameterization that is different
    /// from its NURBS form.
    /// </param>
    /// <param name="nurbsSurface">NURBS form of the face's underlying surface.</param>
    /// <remarks>
    /// Don't call this function unless you know exactly what you are doing.
    /// </remarks>
    public void RebuildTrimsForV2(BrepFace face, NurbsSurface nurbsSurface)
    {
      IntPtr ptr_this = NonConstPointer();
      IntPtr ptr_face = face.NonConstPointer();
      IntPtr const_ptr_surface = nurbsSurface.ConstPointer();
      UnsafeNativeMethods.ON_Brep_RebuildTrimsForV2(ptr_this, ptr_face, const_ptr_surface);
      Runtime.CommonObject.GcProtect(face, nurbsSurface);
    }

    /// <summary>
    /// No support is available for this function.
    /// <para>Expert user function that converts all geometry in brep to nurbs form.</para>
    /// </summary>
    /// <remarks>
    /// Don't call this function unless you know exactly what you are doing.
    /// </remarks>
    public bool MakeValidForV2()
    {
      IntPtr ptr_this = NonConstPointer();
      return UnsafeNativeMethods.ON_Brep_MakeValidForV2(ptr_this);
    }


#endif

    /// <summary>
    /// Deletes any unreferenced objects from arrays, reindexes as needed, and
    /// shrinks arrays to minimum required size. Uses CUllUnused* members to
    /// delete any unreferenced objects from arrays.
    /// </summary>
    public void Compact()
    {
      IntPtr ptr_this = NonConstPointer();
      UnsafeNativeMethods.ON_Brep_Compact(ptr_this);
    }

#if RHINO_SDK
    /// <summary>
    /// Fills in missing or fixes incorrect component information from a Brep. 
    /// Useful when reading Brep information from other file formats that do not 
    /// provide as complete of a Brep definition as requried by Rhino.
    /// </summary>
    /// <param name="tolerance">The repair tolerance. When in doubt, use the document's model absolute tolerance.</param>
    /// <returns>True on success.</returns>
    public bool Repair(double tolerance)
    {
      IntPtr ptr_brep = NonConstPointer();
      return UnsafeNativeMethods.ON_Brep_Repair(ptr_brep, tolerance);
    }

    /// <summary>
    /// Remove all inner loops, or holes, in a Brep.
    /// </summary>
    /// <param name="tolerance">The tolerance. When in doubt, use the document's model absolute tolerance.</param>
    /// <returns>The Brep without holes if successful, null otherwise.</returns>
    [ConstOperation]
    public Brep RemoveHoles(double tolerance)
    {
      IntPtr ptr_const_this = ConstPointer();
      IntPtr ptr_brep = UnsafeNativeMethods.RHC_RhinoBrepRemoveAllInnerLoops(ptr_const_this, tolerance);
      return CreateGeometryHelper(ptr_brep, null) as Brep;
    }

    /// <summary>
    /// Removes inner loops, or holes, in a Brep.
    /// </summary>
    /// <param name="loops">A list of BrepLoop component indexes, where BrepLoop.LoopType == Rhino.Geometry.BrepLoopType.Inner.</param>
    /// <param name="tolerance">The tolerance. When in doubt, use the document's model absolute tolerance.</param>
    /// <returns>The Brep without holes if successful, null otherwise.</returns>
    [ConstOperation]
    public Brep RemoveHoles(IEnumerable<ComponentIndex> loops, double tolerance)
    {
      using (var loop_ci = new INTERNAL_ComponentIndexArray())
      {
        foreach (var ci in loops)
        {
          // Validate component indices
          if (ci.ComponentIndexType == ComponentIndexType.BrepLoop)
          {
            if (ci.Index >= 0 && ci.Index < Loops.Count)
            {
              BrepLoop loop = Loops[ci.Index];
              if (null != loop && loop.LoopType == Rhino.Geometry.BrepLoopType.Inner)
                loop_ci.Add(ci);
            }
          }
        }

        // Bail of no inner loops
        if (0 == loop_ci.Count)
          return null;

        IntPtr ptr_const_this = ConstPointer();
        IntPtr ptr_brep = UnsafeNativeMethods.RHC_RhinoBrepFaceRemoveInnerLoops2(ptr_const_this, loop_ci.Count, loop_ci.ToArray(), tolerance);
        return CreateGeometryHelper(ptr_brep, null) as Brep;
      }
    }

#endif

    bool CullUnusedHelper(UnsafeNativeMethods.BrepCullUnused which)
    {
      IntPtr ptr_this = NonConstPointer();
      return UnsafeNativeMethods.ON_Brep_CullUnused(ptr_this, which);
    }

    /// <summary>Culls faces with m_face_index == -1.</summary>
    /// <returns>true if operation succeeded; false otherwise.</returns>
    public bool CullUnusedFaces()
    {
      return CullUnusedHelper(UnsafeNativeMethods.BrepCullUnused.CullUnusedFaces);
    }

    /// <summary>Culls loops with m_loop_index == -1.</summary>
    /// <returns>true if operation succeeded; false otherwise.</returns>
    public bool CullUnusedLoops()
    {
      return CullUnusedHelper(UnsafeNativeMethods.BrepCullUnused.CullUnusedLoops);
    }

    /// <summary>Culls trims with m_trim_index == -1.</summary>
    /// <returns>true if operation succeeded; false otherwise.</returns>
    public bool CullUnusedTrims()
    {
      return CullUnusedHelper(UnsafeNativeMethods.BrepCullUnused.CullUnusedTrims);
    }

    /// <summary>Culls edges with m_edge_index == -1.</summary>
    /// <returns>true if operation succeeded; false otherwise.</returns>
    public bool CullUnusedEdges()
    {
      return CullUnusedHelper(UnsafeNativeMethods.BrepCullUnused.CullUnusedEdges);
    }

    /// <summary>Culls vertices with m_vertex_index == -1.</summary>
    /// <returns>true if operation succeeded; false otherwise.</returns>
    public bool CullUnusedVertices()
    {
      return CullUnusedHelper(UnsafeNativeMethods.BrepCullUnused.CullUnusedVertices);
    }

    /// <summary>Culls 2d curves not referenced by a trim.</summary>
    /// <returns>true if operation succeeded; false otherwise.</returns>
    public bool CullUnused3dCurves()
    {
      return CullUnusedHelper(UnsafeNativeMethods.BrepCullUnused.CullUnused3dCurves);
    }

    /// <summary>Culls 3d curves not referenced by an edge.</summary>
    /// <returns>true if operation succeeded; false otherwise.</returns>
    public bool CullUnused2dCurves()
    {
      return CullUnusedHelper(UnsafeNativeMethods.BrepCullUnused.CullUnused2dCurves);
    }

    /// <summary>Culls surfaces not referenced by a face.</summary>
    /// <returns>true if operation succeeded; false otherwise.</returns>
    public bool CullUnusedSurfaces()
    {
      return CullUnusedHelper(UnsafeNativeMethods.BrepCullUnused.CullUnusedSurfaces);
    }

    /// <summary>
    /// Standardizes all trims, edges, and faces in the brep.
    /// After standardizing, there may be unused curves and surfaces in the
    /// brep.  Call Brep.Compact to remove these unused curves and surfaces.
    /// </summary>
    public void Standardize()
    {
      IntPtr ptr_this = NonConstPointer();
      UnsafeNativeMethods.ON_Brep_Standardize(ptr_this);
    }
    #endregion

    #region internal helpers

    internal override GeometryBase DuplicateShallowHelper()
    {
      var rc = new Brep(IntPtr.Zero, null);
      return rc;
    }

    #endregion
  }

  /// <summary>
  /// Enumerates the possible point/BrepFace spatial relationships.
  /// </summary>
  public enum PointFaceRelation
  {
    /// <summary>
    /// Point is on the exterior (the trimmed part) of the face.
    /// </summary>
    Exterior = 0,

    /// <summary>
    /// Point is on the interior (the existing part) of the face.
    /// </summary>
    Interior = 1,

    /// <summary>
    /// Point is in limbo.
    /// </summary>
    Boundary = 2
  }

  /// <summary>
  /// Enumerates all possible Solid Orientations for a Brep.
  /// </summary>
  public enum BrepSolidOrientation
  {
    /// <summary>
    /// Brep is not a Solid.
    /// </summary>
    None = 0,

    /// <summary>
    /// Brep is a Solid with inward facing normals.
    /// </summary>
    Inward = -1,

    /// <summary>
    /// Brep is a Solid with outward facing normals.
    /// </summary>
    Outward = 1,

    /// <summary>
    /// Breps is a Solid but no orientation could be computed.
    /// </summary>
    Unknown = 2
  }

  /// <summary>
  /// Enumerates all possible Topological Edge adjacency types.
  /// </summary>
  public enum EdgeAdjacency
  {
    /// <summary>
    /// Edge is not used by any faces and is therefore superfluous.
    /// </summary>
    None = 0,

    /// <summary>
    /// Edge is used by a single face.
    /// </summary>
    Naked = 1,

    /// <summary>
    /// Edge is used by two adjacent faces.
    /// </summary>
    Interior = 2,

    /// <summary>
    /// Edge is used by three or more adjacent faces.
    /// </summary>
    NonManifold = 3
  }

  /// <summary>
  /// Enumerates the possible types of edge concavity types.
  /// </summary>
  public enum Concavity
  {
    /// <summary>
    /// Concavity is not relevant, for example naked and non-manifold edges
    /// cannot have a concavity measure.
    /// </summary>
    None = -1,
    /// <summary>
    /// Faces on either side of the edge are tangent within tolerance.
    /// </summary>
    Tangent = 0,
    /// <summary>
    /// Faces on either side of the edge are convex.
    /// </summary>
    Convex = 1,
    /// <summary>
    /// Faces on either side of the edge are concave.
    /// </summary>
    Concave = 2
  }

  /// <summary>
  /// Brep vertex information
  /// </summary>
  public class BrepVertex : Point
  {
    internal BrepVertex(int index, Brep owner) : base(IntPtr.Zero, null)
    {
      VertexIndex = index;
      Brep = owner;
    }

    #region properties
    /// <summary>
    /// Gets the Brep that owns this vertex.
    /// </summary>
    public Brep Brep { get; }

    /// <summary>
    /// Gets the index of this vertex in the Brep.Vertices collection.
    /// </summary>
    public int VertexIndex { get; }

    #endregion

    #region methods
    /// <summary>
    /// Gets the indices of all edges associated with this vertex.
    /// </summary>
    /// <returns>Empty array on failure.</returns>
    [ConstOperation]
    public int[] EdgeIndices()
    {
      IntPtr ptr_const_brep = Brep.ConstPointer();
      using (var ei = new SimpleArrayInt())
      {
        int rc = UnsafeNativeMethods.ON_BrepVertex_EdgeIndices(ptr_const_brep, VertexIndex, ei.m_ptr);
        return rc == 0 ? new int[0] : ei.ToArray();
      }
    }
    #endregion

    internal override IntPtr _InternalGetConstPointer()
    {
      IntPtr ptr_const_brep = Brep.ConstPointer();
      return UnsafeNativeMethods.ON_BrepVertex_GetPointer(ptr_const_brep, VertexIndex);
    }

    internal override IntPtr NonConstPointer()
    {
      IntPtr ptr_const_brep = Brep.NonConstPointer();
      return UnsafeNativeMethods.ON_BrepVertex_GetPointer(ptr_const_brep, VertexIndex);
    }
  }

  /// <summary>
  /// Represents a single edge curve in a Brep object.
  /// </summary>
  public class BrepEdge : CurveProxy
  {
    internal BrepEdge(int index, Brep owner)
    {
      EdgeIndex = index;
      Brep = owner;
    }

    #region properties

    //    ON_U m_edge_user;
    //    ON_BrepTrim* Trim( int eti ) const;
    //    ON_BrepVertex* Vertex(int evi) const;
    //    int EdgeCurveIndexOf() const;
    //    const ON_Curve* EdgeCurveOf() const;
    //    bool ChangeEdgeCurve( int c3i );
    //    void UnsetPlineEdgeParameters();
    //    int m_c3i;
    //    int m_vi[2];
    //    ON_SimpleArray<int> m_ti;

    /// <summary>
    /// Gets or sets the accuracy of the edge curve (>=0.0 or RhinoMath.UnsetValue)
    /// A value of UnsetValue indicates that the tolerance should be computed.
    ///
    /// The maximum distance from the edge's 3d curve to any surface of a face
    /// that has this edge as a portion of its boundary must be &lt;= this tolerance.
    /// </summary>
    public double Tolerance
    {
      get
      {
        IntPtr ptr_const_this = ConstPointer();
        return UnsafeNativeMethods.ON_BrepEdge_GetTolerance(ptr_const_this);
      }
      set
      {
        IntPtr ptr_this = NonConstPointer();
        UnsafeNativeMethods.ON_BrepEdge_SetTolerance(ptr_this, value);
      }
    }

    /// <summary>
    /// Gets the number of trim-curves that use this edge.
    /// </summary>
    public int TrimCount
    {
      get
      {
        IntPtr ptr_const_brep = Brep.ConstPointer();
        return UnsafeNativeMethods.ON_Brep_EdgeTrimCount(ptr_const_brep, EdgeIndex);
      }
    }

    /// <summary>
    /// Gets the topological valency of this edge. The topological valency 
    /// is defined by how many adjacent faces share this edge.
    /// </summary>
    public EdgeAdjacency Valence
    {
      get
      {
        switch (TrimCount)
        {
          case 0:
            return EdgeAdjacency.None;

          case 1:
            return EdgeAdjacency.Naked;

          case 2:
            return EdgeAdjacency.Interior;

          default:
            return EdgeAdjacency.NonManifold;
        }
      }
    }

    /// <summary>
    /// Gets the Brep that owns this edge.
    /// </summary>
    public Brep Brep { get; }

    /// <summary>
    /// Gets the index of this edge in the Brep.Edges collection.
    /// </summary>
    public int EdgeIndex { get; }

#if RHINO_SDK
    /// <summary>
    /// Determine the concavity of this edge at a specific parameter.
    /// </summary>
    /// <param name="t">Edge curve parameter.</param>
    /// <param name="tolerance">Angle tolerance for tangent faces.</param>
    /// <returns>Concavity measure at parameter.</returns>
    [ConstOperation]
    public Concavity ConcavityAt(double t, double tolerance)
    {
      tolerance = Math.Cos(tolerance); // TODO: Ask Chuck about this.

      IntPtr ptr_const_edge = ConstPointer();
      int measure = UnsafeNativeMethods.ON_Brep_EdgeConcavity(ptr_const_edge, t, tolerance);
      switch (measure)
      {
        case 0: return Concavity.Tangent;
        case 1: return Concavity.Convex;
        case 2: return Concavity.Concave;
        default: return Concavity.None;
      }
    }
#endif
    #endregion

    #region methods

#if RHINO_SDK
    /// <summary>
    /// For a manifold, non-boundary edge, decides whether or not the two surfaces
    /// on either side meet smoothly.
    /// </summary>
    /// <param name="angleToleranceRadians">
    /// used to decide if surface normals on either side are parallel.
    /// </param>
    /// <returns>
    /// true if edge is manifold, has exactly 2 trims, and surface normals on either
    /// side agree to within angle_tolerance.
    /// </returns>
    [ConstOperation]
    public bool IsSmoothManifoldEdge([Optional, DefaultParameterValue(RhinoMath.DefaultAngleTolerance)]double angleToleranceRadians)
    {
      //NOTE!! don't ever use default parameters in RhinoCommon. They just lead to headache
      IntPtr ptr_const_this = ConstPointer();
      return UnsafeNativeMethods.ON_BrepEdge_IsSmoothManifoldEdge(ptr_const_this, angleToleranceRadians);
    }
#endif

    /// <summary>
    /// Gets the indices of all the BrepFaces that use this edge.
    /// </summary>
    [ConstOperation]
    public int[] AdjacentFaces()
    {
      IntPtr ptr_const_brep = Brep.ConstPointer();
      using (var fi = new SimpleArrayInt())
      {
        int rc = UnsafeNativeMethods.ON_Brep_EdgeFaceIndices(ptr_const_brep, EdgeIndex, fi.m_ptr);
        return rc == 0 ? new int[0] : fi.ToArray();
      }
    }


    /// <summary>
    /// Gets the indices of all trims associated with this edge.
    /// </summary>
    /// <returns>Empty array on failure.</returns>
    [ConstOperation]
    public int[] TrimIndices()
    {
      IntPtr ptr_const_brep = Brep.ConstPointer();
      using (var ti = new SimpleArrayInt())
      {
        int rc = UnsafeNativeMethods.ON_BrepEdge_TrimIndices(ptr_const_brep, EdgeIndex, ti.m_ptr);
        return rc == 0 ? new int[0] : ti.ToArray();
      }
    }

#if RHINO_SDK
    /// <summary>
    /// Get corresponding edge parameter for given trim at given trim parameter.
    /// </summary>
    /// <param name="trimIndex"></param>
    /// <param name="trimParameter"></param>
    /// <param name="edgeParameter"></param>
    /// <returns>true on success</returns>
    [ConstOperation]
    public bool GetEdgeParameter(int trimIndex, double trimParameter, out double edgeParameter)
    {
      edgeParameter = RhinoMath.UnsetValue;
      IntPtr ptr_const_brep = Brep.ConstPointer();
      return UnsafeNativeMethods.ON_Brep_GetEdgeParameter(ptr_const_brep, trimIndex, trimParameter, ref edgeParameter);
    }
#endif

    /// <summary>
    /// Get the brep.Curves3D[] index of the 3d curve geometry used by this edge or -1.
    /// </summary>
    public int EdgeCurveIndex
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        return UnsafeNativeMethods.ON_BrepEdge_EdgeCurveIndex(const_ptr_this);
      }
    }

    /// <summary>
    /// Get the brep.Curves3D[] 3d curve geometry used by this edge or null.
    /// </summary>
    public Curve EdgeCurve
    {
      get
      {
        int index = EdgeCurveIndex;
        if (index < 0)
          return null;
        return Brep.Curves3D[index];
      }
    }

    /// <summary>
    /// Set 3d curve geometry used by a b-rep edge.
    /// </summary>
    /// <param name="curve3dIndex">index of 3d curve in m_C3[] array</param>
    /// <returns>true if successful</returns>
    public bool SetEdgeCurve(int curve3dIndex)
    {
      IntPtr ptr_brep = Brep.NonConstPointer();
      return UnsafeNativeMethods.ON_Brep_SetEdgeCurve(ptr_brep, EdgeIndex, curve3dIndex, Interval.Unset);
    }

    /// <summary>
    /// Set 3d curve geometry used by a b-rep edge.
    /// </summary>
    /// <param name="curve3dIndex">index of 3d curve in m_C3[] array</param>
    /// <param name="subDomain"></param>
    /// <returns>true if successful</returns>
    public bool SetEdgeCurve(int curve3dIndex, Interval subDomain)
    {
      IntPtr ptr_brep = Brep.NonConstPointer();
      return UnsafeNativeMethods.ON_Brep_SetEdgeCurve(ptr_brep, EdgeIndex, curve3dIndex, subDomain);
    }

    /// <summary>
    /// BrepVertex at start of edge
    /// </summary>
    public BrepVertex StartVertex
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        int index = UnsafeNativeMethods.ON_BrepEdge_BrepVertex(const_ptr_this, 0);
        if (index < 0)
          return null;
        return new BrepVertex(index, Brep);
      }
    }

    /// <summary>
    /// BrepVertex at end of edge
    /// </summary>
    public BrepVertex EndVertex
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        int index = UnsafeNativeMethods.ON_BrepEdge_BrepVertex(const_ptr_this, 1);
        if (index < 0)
          return null;
        return new BrepVertex(index, Brep);
      }
    }

    internal override IntPtr _InternalGetConstPointer()
    {
      if (null != Brep)
      {
        IntPtr ptr_const_brep = Brep.ConstPointer();
        return UnsafeNativeMethods.ON_Brep_BrepEdgePointer(ptr_const_brep, EdgeIndex);
      }
      return IntPtr.Zero;
    }
    #endregion
  }

  /// <summary>
  /// Each brep trim has a defined type.
  /// </summary>
  public enum BrepTrimType
  {
    /// <summary>Unknown type</summary>
    Unknown = 0,
    /// <summary>
    /// Trim is connected to an edge, is part of an outer, inner or
    /// slit loop, and is the only trim connected to the edge.
    /// </summary>
    Boundary = 1,
    /// <summary>
    /// Trim is connected to an edge, is part of an outer, inner or slit loop,
    /// no other trim from the same loop is connected to the edge, and at least
    /// one trim from a different loop is connected to the edge.
    /// </summary>
    Mated = 2,
    /// <summary>
    /// trim is connected to an edge, is part of an outer, inner or slit loop,
    /// and one other trim from the same loop is connected to the edge.
    /// (There can be other mated trims that are also connected to the edge.
    /// For example, the non-mainfold edge that results when a surface edge lies
    /// in the middle of another surface.)  Non-mainfold "cuts" have seam trims too.
    /// </summary>
    Seam = 3,
    /// <summary>
    /// Trim is part of an outer loop, the trim's 2d curve runs along the singular
    /// side of a surface, and the trim is NOT connected to an edge. (There is
    /// no 3d edge because the surface side is singular.)
    /// </summary>
    Singular = 4,
    /// <summary>
    /// Trim is connected to an edge, is the only trim in a crfonsrf loop, and
    /// is the only trim connected to the edge.
    /// </summary>
    CurveOnSurface = 5,
    /// <summary>
    /// Trim is a point on a surface, trim.m_pbox is records surface parameters,
    /// and is the only trim in a ptonsrf loop.  This trim is not connected to
    /// an edge and has no 2d curve.
    /// </summary>
    PointOnSurface = 6,
    /// <summary></summary>
    Slit = 7
  }


  /// <summary>
  /// Brep trim information is stored in BrepTrim classes. Brep.Trims is an
  /// array of all the trims in the brep. A BrepTrim is derived from CurveProxy
  /// so the trim can supply easy to use evaluation tools via the Curve virtual
  /// member functions.
  /// Note well that the domains and orientations of the curve m_C2[trim.m_c2i]
  /// and the trim as a curve may not agree.
  /// </summary>
  public class BrepTrim : CurveProxy
  {
    #region fields
    int m_index;

    internal BrepTrim(int index, Brep owner)
    {
      m_index = index;
      Brep = owner;
    }
    #endregion

    /// <summary>
    /// Gets the Brep that owns this trim.
    /// </summary>
    public Brep Brep { get; }

    int GetItemIndex(UnsafeNativeMethods.BrepTrimType which)
    {
      IntPtr ptr_const_brep = Brep.ConstPointer();
      return UnsafeNativeMethods.ON_BrepTrim_ItemIndex(ptr_const_brep, m_index, which);
    }

    /// <summary>
    /// Loop that this trim belongs to
    /// </summary>
    public BrepLoop Loop
    {
      get
      {
        int index = GetItemIndex(UnsafeNativeMethods.BrepTrimType.LoopIndex);
        if (index < 0)
          return null;
        return Brep.Loops[index];
      }
    }

    /// <summary>
    /// Brep face this trim belongs to
    /// </summary>
    public BrepFace Face
    {
      get
      {
        int index = GetItemIndex(UnsafeNativeMethods.BrepTrimType.FaceIndex);
        if (index < 0)
          return null;
        return Brep.Faces[index];
      }
    }

    /// <summary>
    /// Brep edge this trim belongs to. This will be null for singular trims
    /// </summary>
    public BrepEdge Edge
    {
      get
      {
        int index = GetItemIndex(UnsafeNativeMethods.BrepTrimType.EdgeIndex);
        if (index < 0)
          return null;
        return Brep.Edges[index];
      }
    }

    /// <summary>
    /// Gets the Brep.Curves2D 2d curve index of the 2d curve geometry used by this trim, or -1.
    /// </summary>
    public int TrimCurveIndex
    {
      get { return GetItemIndex(UnsafeNativeMethods.BrepTrimType.Curve2dIndex); }
    }

    /// <summary>
    /// Gets the Brep.Curves2D 2d curve geometry used by this trim, or null.
    /// </summary>
    public Curve TrimCurve
    {
      get
      {
        int index = GetItemIndex(UnsafeNativeMethods.BrepTrimType.Curve2dIndex);
        if (index < 0)
          return null;
        return Brep.Curves2D[index];
      }
    }

    /// <summary>
    /// Gets the index of this trim in the Brep.Trims collection.
    /// </summary>
    public int TrimIndex
    {
      get { return m_index; }
    }

    /// <summary>Type of trim</summary>
    public BrepTrimType TrimType
    {
      get
      {
        IntPtr ptr_const_brep = Brep.ConstPointer();
        return (BrepTrimType)UnsafeNativeMethods.ON_BrepTrim_Type(ptr_const_brep, m_index);
      }
      set
      {
        IntPtr ptr_brep = Brep.NonConstPointer();
        UnsafeNativeMethods.ON_BrepTrim_SetType(ptr_brep, m_index, (int)value);
      }
    }

    /// <summary></summary>
    public IsoStatus IsoStatus
    {
      get
      {
        IntPtr ptr_const_brep = Brep.ConstPointer();
        return (IsoStatus)UnsafeNativeMethods.ON_BrepTrim_Iso(ptr_const_brep, m_index);
      }
      set
      {
        IntPtr ptr_brep = Brep.NonConstPointer();
        UnsafeNativeMethods.ON_BrepTrim_SetIso(ptr_brep, m_index, (int)value);
      }
    }

    /// <summary>
    /// Get orientation of trim with respect to it's corresponding edge.
    /// </summary>
    /// <returns>true if the 2d trim and 3d edge have opposite orientations</returns>
    [ConstOperation]
    public bool IsReversed()
    {
      IntPtr const_ptr_this = ConstPointer();
      return UnsafeNativeMethods.ON_BrepTrim_GetRevFlag(const_ptr_this);
    }

#if RHINO_SDK
    /// <summary>
    /// Get corresponding trim parameter at given edge parameter.
    /// </summary>
    /// <param name="edgeParameter"></param>
    /// <param name="trimParameter"></param>
    /// <returns>true on success</returns>
    [ConstOperation]
    public bool GetTrimParameter(double edgeParameter, out double trimParameter)
    {
      trimParameter = RhinoMath.UnsetValue;
      IntPtr ptr_const_brep = Brep.ConstPointer();
      return UnsafeNativeMethods.ON_Brep_GetTrimParameter(ptr_const_brep, TrimIndex, edgeParameter, ref trimParameter);
    }
#endif

    /// <summary>
    /// The values in tolerance[] record the accuracy of the parameter space
    /// trimming curves.
    /// </summary>
    /// <remarks>
    /// <para>tolerance[0] = accuracy of parameter space curve in first ("u") parameter</para>
    /// <para>tolerance[1] = accuracy of parameter space curve in second ("v") parameter</para>
    /// <para>
    /// A value of RhinoMath.UnsetValue indicates that the tolerance should be computed.
    /// If the value &gt;= 0.0, then the tolerance is set.
    /// If the value is RhinoMath.UnsetValue, then the tolerance needs to be computed.
    /// </para>
    /// <para>
    /// If the trim is not singular, then the trim must have an edge. If P is a
    /// 3d point on the edge's curve and surface(u,v) = Q is the point on the
    /// surface that is closest to P, then there must be a parameter t in the
    /// interval [m_t[0], m_t[1]] such that
    ///  |u - curve2d(t)[0]| &lt;= tolerance[0]
    /// and
    ///  |v - curve2d(t)[1]| &lt;= tolerance[1]
    ///
    /// If P is the 3d point for the vertex brep.m_V[m_vi[k]] and (uk,vk) is the
    /// corresponding end of the trim's parameter space curve, then there must be
    /// a surface parameter (u,v) such that:
    /// </para>
    /// <list type="bullet">
    /// <item><description>
    /// the distance from the 3d point surface(u,v) to P is &lt;= brep.m_V[m_vi[k]].m_tolerance,
    /// </description></item>
    /// <item><description>|u-uk| &lt;= tolerance[0].</description></item>
    /// <item><description>|v-vk| &lt;= tolerance[1].</description></item>
    /// </list>
    /// </remarks>
    /// <param name="toleranceU"></param>
    /// <param name="toleranceV"></param>
    [ConstOperation]
    public void GetTolerances(out double toleranceU, out double toleranceV)
    {
      IntPtr ptr_const_brep = Brep.ConstPointer();
      toleranceU = UnsafeNativeMethods.ON_BrepTrim_Tolerance(ptr_const_brep, m_index, 0);
      toleranceV = UnsafeNativeMethods.ON_BrepTrim_Tolerance(ptr_const_brep, m_index, 1);
    }


    /// <summary>
    /// The values in tolerance[] record the accuracy of the parameter space
    /// trimming curves.
    /// </summary>
    /// <remarks>
    /// <para>tolerance[0] = accuracy of parameter space curve in first ("u") parameter</para>
    /// <para>tolerance[1] = accuracy of parameter space curve in second ("v") parameter</para>
    /// <para>
    /// A value of RhinoMath.UnsetValue indicates that the tolerance should be computed.
    /// If the value &gt;= 0.0, then the tolerance is set.
    /// If the value is RhinoMath.UnsetValue, then the tolerance needs to be computed.
    /// </para>
    /// <para>
    /// If the trim is not singular, then the trim must have an edge. If P is a
    /// 3d point on the edge's curve and surface(u,v) = Q is the point on the
    /// surface that is closest to P, then there must be a parameter t in the
    /// interval [m_t[0], m_t[1]] such that
    ///  |u - curve2d(t)[0]| &lt;= tolerance[0]
    /// and
    ///  |v - curve2d(t)[1]| &lt;= tolerance[1]
    ///
    /// If P is the 3d point for the vertex brep.m_V[m_vi[k]] and (uk,vk) is the
    /// corresponding end of the trim's parameter space curve, then there must be
    /// a surface parameter (u,v) such that:
    /// </para>
    /// <list type="bullet">
    /// <item><description>
    /// the distance from the 3d point surface(u,v) to P is &lt;= brep.m_V[m_vi[k]].m_tolerance,
    /// </description></item>
    /// <item><description>|u-uk| &lt;= tolerance[0].</description></item>
    /// <item><description>|v-vk| &lt;= tolerance[1].</description></item>
    /// </list>
    /// </remarks>
    /// <param name="toleranceU"></param>
    /// <param name="toleranceV"></param>
    public void SetTolerances(double toleranceU, double toleranceV)
    {
      IntPtr ptr_brep = Brep.NonConstPointer();
      UnsafeNativeMethods.ON_BrepTrim_SetTolerance(ptr_brep, m_index, 0, toleranceU);
      UnsafeNativeMethods.ON_BrepTrim_SetTolerance(ptr_brep, m_index, 1, toleranceV);
    }

    /// <summary>
    /// Set 2d curve geometry used by a b-rep trim.
    /// </summary>
    /// <param name="curve2dIndex">index of 2d curve in m_C2[] array</param>
    /// <returns>true if successful</returns>
    public bool SetTrimCurve(int curve2dIndex)
    {
      IntPtr ptr_brep = Brep.NonConstPointer();
      return UnsafeNativeMethods.ON_Brep_SetTrimCurve(ptr_brep, m_index, curve2dIndex, Interval.Unset);
    }

    /// <summary>
    /// BrepVertex at start of trim
    /// </summary>
    public BrepVertex StartVertex
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        int index = UnsafeNativeMethods.ON_BrepTrim_BrepVertex(const_ptr_this, 0);
        if (index < 0)
          return null;
        return new BrepVertex(index, Brep);
      }
    }

    /// <summary>
    /// BrepVertex at end of trim
    /// </summary>
    public BrepVertex EndVertex
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        int index = UnsafeNativeMethods.ON_BrepTrim_BrepVertex(const_ptr_this, 1);
        if (index < 0)
          return null;
        return new BrepVertex(index, Brep);
      }
    }

    /// <summary>
    /// Set 2d curve geometry used by a b-rep trim.
    /// </summary>
    /// <param name="curve2dIndex">index of 2d curve in m_C2[] array</param>
    /// <param name="subDomain"></param>
    /// <returns>true if successful</returns>
    public bool SetTrimCurve(int curve2dIndex, Interval subDomain)
    {
      IntPtr ptr_brep = Brep.NonConstPointer();
      return UnsafeNativeMethods.ON_Brep_SetTrimCurve(ptr_brep, m_index, curve2dIndex, subDomain);
    }

    internal override IntPtr _InternalGetConstPointer()
    {
      if (null != Brep)
      {
        IntPtr ptr_const_brep = Brep.ConstPointer();
        return UnsafeNativeMethods.ON_Brep_BrepTrimPointer(ptr_const_brep, m_index);
      }
      return IntPtr.Zero;
    }
  }

  /// <summary>
  /// Each brep loop has a defined type, e.g. outer, inner or point on surface.
  /// </summary>
  public enum BrepLoopType
  {
    /// <summary>
    /// Unknown loop type.
    /// </summary>
    Unknown = 0,
    /// <summary>
    /// 2d loop curves form a simple closed curve with a counterclockwise orientation.
    /// </summary>
    Outer = 1,
    /// <summary>
    /// 2d loop curves form a simple closed curve with a clockwise orientation.
    /// </summary>
    Inner = 2,
    /// <summary>
    /// Always closed - used internally during splitting operations.
    /// </summary>
    Slit = 3,
    /// <summary>
    /// "loop" is a curveonsrf made from a single (open or closed) trim that
    /// has type TrimType.CurveOnSurface.
    /// </summary>
    CurveOnSurface = 4,
    /// <summary>
    /// "loop" is a PointOnSurface made from a single trim that has
    /// type TrimType.PointOnSurface.
    /// </summary>
    PointOnSurface = 5
  }

  /// <summary>
  /// Represent a single loop in a Brep object. A loop is composed
  /// of a list of trim curves.
  /// </summary>
  public class BrepLoop : GeometryBase
  {
    internal BrepLoop(int index, Brep owner)
    {
      ConstructConstObject(owner, index);
      LoopIndex = index;
      Brep = owner;
    }

    internal override IntPtr _InternalGetConstPointer()
    {
      IntPtr ptr_const_brep = Brep.ConstPointer();
      return UnsafeNativeMethods.ON_BrepLoop_GetPointer(ptr_const_brep, LoopIndex);
    }

    #region properties
    /// <summary>
    /// Gets the Brep that owns this loop.
    /// </summary>
    public Brep Brep { get; }

    /// <summary>
    /// Gets the index of this loop in the Brep.Loops collection.
    /// </summary>
    public int LoopIndex { get; }

    /// <summary>
    /// BrepFace this loop belongs to.
    /// </summary>
    public BrepFace Face
    {
      get
      {
        IntPtr ptr_const_brep = Brep.ConstPointer();
        int face_index = UnsafeNativeMethods.ON_BrepLoop_FaceIndex(ptr_const_brep, LoopIndex);
        if (face_index < 0)
          return null;
        return Brep.Faces[face_index];
      }
    }

    /// <summary>
    /// type of loop.
    /// </summary>
    public BrepLoopType LoopType
    {
      get
      {
        IntPtr ptr_const_brep = Brep.ConstPointer();
        return (BrepLoopType)UnsafeNativeMethods.ON_BrepLoop_Type(ptr_const_brep, LoopIndex);
      }
    }

    Collections.BrepTrimList m_trims;
    /// <summary>
    /// List of trims for this loop
    /// </summary>
    public Collections.BrepTrimList Trims
    {
      get { return m_trims ?? (m_trims = new Collections.BrepTrimList(this)); }
    }

    #endregion

    #region methods

    /// <summary>
    /// Create a 3D curve that approximates the loop geometry.
    /// </summary>
    /// <returns>A 3D curve that approximates the loop or null on failure.</returns>
    [ConstOperation]
    public Curve To3dCurve()
    {
      IntPtr ptr_const_brep = Brep.ConstPointer();
      IntPtr ptr_loop_curve = UnsafeNativeMethods.ON_BrepLoop_GetCurve3d(ptr_const_brep, LoopIndex);
      return CreateGeometryHelper(ptr_loop_curve, null) as Curve;
    }

    /// <summary>
    /// Create a 2d curve that traces the entire loop
    /// </summary>
    /// <returns></returns>
    [ConstOperation]
    public Curve To2dCurve()
    {
      IntPtr ptr_const_brep = Brep.ConstPointer();
      IntPtr ptr_loop_curve = UnsafeNativeMethods.ON_BrepLoop_GetCurve2d(ptr_const_brep, LoopIndex);
      return CreateGeometryHelper(ptr_loop_curve, null) as Curve;
    }
    #endregion
  }

  /// <summary>
  /// Provides strongly-typed access to brep faces.
  /// <para>A Brep face is composed of one surface and trimming curves.</para>
  /// </summary>
  public class BrepFace : SurfaceProxy
  {
    #region fields
    internal Brep m_brep;
    #endregion

    #region constructors
    internal BrepFace(int index, Brep owner)
    {
      FaceIndex = index;
      m_brep = owner;
    }
    #endregion

    #region properties

    /// <summary>
    /// Gets the Brep that owns this face.
    /// </summary>
    public Brep Brep => m_brep;

    /// <summary>
    /// true if face orientation is opposite of natural surface orientation.
    /// </summary>
    /// <example>
    /// <code source='examples\vbnet\ex_evnormal.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_evnormal.cs' lang='cs'/>
    /// <code source='examples\py\ex_evnormal.py' lang='py'/>
    /// </example>
    public bool OrientationIsReversed
    {
      get
      {
        IntPtr ptr_const_this = ConstPointer();
        return UnsafeNativeMethods.ON_BrepFace_IsReversed(ptr_const_this);
      }
      set
      {
        IntPtr ptr_this = NonConstPointer();
        UnsafeNativeMethods.ON_BrepFace_SetIsReversed(ptr_this, value);
      }
    }

    /// <summary>
    /// Gets a value indicating whether the face is synonymous with the underlying surface. 
    /// If a Face has no trimming curves then it is considered a Surface.
    /// </summary>
    public bool IsSurface
    {
      get
      {
        IntPtr ptr_const_brep = m_brep.ConstPointer();
        return UnsafeNativeMethods.ON_Brep_FaceIsSurface(ptr_const_brep, FaceIndex);
      }
    }

    /// <summary>Index of face in Brep.Faces array.</summary>
    public int FaceIndex { get; }

    /// <summary>
    /// Surface index of the 3d surface geometry used by this face or -1
    /// </summary>
    public int SurfaceIndex
    {
      get
      {
        IntPtr ptr_const_this = ConstPointer();
        return UnsafeNativeMethods.ON_BrepFace_SurfaceIndex(ptr_const_this);
      }
    }

    Collections.BrepLoopList m_loop_list;
    /// <summary>
    /// Loops in this face.
    /// </summary>
    public Collections.BrepLoopList Loops
    {
      get
      {
        return m_loop_list ?? (m_loop_list = new Collections.BrepLoopList(this));
      }
    }

    /// <summary>
    /// Every face has a single outer loop.
    /// </summary>
    public BrepLoop OuterLoop
    {
      get
      {
        IntPtr ptr_const_this = ConstPointer();
        int index = UnsafeNativeMethods.ON_BrepFace_OuterLoopIndex(ptr_const_this);
        if (index < 0)
          return null;
        return Loops[index];
      }
    }
    #endregion

    #region methods
    internal override IntPtr _InternalGetConstPointer()
    {
      if (null != m_brep)
      {
        IntPtr ptr_const_brep = m_brep.ConstPointer();
        return UnsafeNativeMethods.ON_Brep_BrepFacePointer(ptr_const_brep, FaceIndex);
      }
      return IntPtr.Zero;
    }
    internal override IntPtr NonConstPointer()
    {
      if (null != m_brep)
      {
        IntPtr ptr_brep = m_brep.NonConstPointer();
        return UnsafeNativeMethods.ON_Brep_BrepFacePointer(ptr_brep, FaceIndex);
      }
      return base.NonConstPointer();
    }

#if RHINO_SDK
    /// <summary>
    /// Pulls one or more points to a brep face.
    /// </summary>
    /// <param name="points">Points to pull.</param>
    /// <param name="tolerance">Tolerance for pulling operation. Only points that are closer than tolerance will be pulled to the face.</param>
    /// <returns>An array of pulled points.</returns>
    [ConstOperation]
    public Point3d[] PullPointsToFace(IEnumerable<Point3d> points, double tolerance)
    {
      int count;
      Point3d[] inpoints = Rhino.Collections.RhinoListHelpers.GetConstArray(points, out count);
      if (inpoints == null || inpoints.Length < 1)
        return null;
      IntPtr const_ptr_brep = m_brep.ConstPointer();
      using (var outpoints = new SimpleArrayPoint3d())
      {
        IntPtr ptr_outpoints = outpoints.NonConstPointer();
        int points_pulled = UnsafeNativeMethods.RHC_RhinoPullPointsToFace(const_ptr_brep, FaceIndex, count, inpoints, ptr_outpoints, tolerance);
        return points_pulled < 1 ? new Point3d[0] : outpoints.ToArray();
      }
    }

    /// <summary>
    ///  Returns the surface draft angle and point at a parameter.
    /// </summary>
    /// <param name="testPoint">The u,v parameter on the face to evaluate.</param>
    /// <param name="testAngle">The angle in radians to test.</param>
    /// <param name="pullDirection">The pull direction.</param>
    /// <param name="edge">Restricts the point placement to an edge.</param>
    /// <param name="draftPoint">The draft angle point.</param>
    /// <param name="draftAngle">The draft angle in radians.</param>
    /// <returns>True if successful, false otherwise.</returns>
    [ConstOperation]
    public bool DraftAnglePoint(Point2d testPoint, double testAngle, Vector3d pullDirection, bool edge, out Point3d draftPoint, out double draftAngle)
    {
      draftPoint = Point3d.Unset;
      draftAngle = RhinoMath.UnsetValue;
      IntPtr ptr_const_this = ConstPointer();
      var rc = UnsafeNativeMethods.RHC_RhinoDraftAnglePoint(ptr_const_this, testPoint, testAngle, pullDirection, edge, ref draftPoint, ref draftAngle);
      return rc;
    }

    /// <summary>
    /// Remove all inner loops, or holes, from a Brep face.
    /// </summary>
    /// <param name="tolerance"></param>
    /// <returns></returns>
    [ConstOperation]
    public Brep RemoveHoles(double tolerance)
    {
      IntPtr ptr_const_this = ConstPointer();
      ComponentIndex ci = ComponentIndex();
      IntPtr ptr_brep = UnsafeNativeMethods.RHC_RhinoBrepFaceRemoveInnerLoops(ptr_const_this, ref ci, tolerance);
      return CreateGeometryHelper(ptr_brep, null) as Brep;
    }

#endif

    /// <summary>
    /// Extrude a face in a Brep.
    /// </summary>
    /// <param name="pathCurve">The path to extrude along.</param>
    /// <param name="cap">If true, the extrusion is capped with a translation of the face being extruded</param>
    /// <returns>A Brep on success or null on failure.</returns>
    [ConstOperation]
    public Brep CreateExtrusion(Curve pathCurve, bool cap)
    {
      IntPtr ptr_const_brep = m_brep.ConstPointer();
      IntPtr const_ptr_curve = pathCurve.ConstPointer();
      IntPtr ptr_brep = UnsafeNativeMethods.ON_BrepFace_BrepExtrudeFace(ptr_const_brep, FaceIndex, const_ptr_curve, cap);
      if (IntPtr.Zero == ptr_brep)
        return null;
      GC.KeepAlive(pathCurve);
      // CreateGeometryHelper will create the "actual" surface type (Nurbs, Sum, Rev,...)
      return CreateGeometryHelper(ptr_brep, null) as Brep;
    }

    /// <summary>
    /// Defines bitwise mask flags indicating what side of a surface to not shrink.
    /// </summary>
    [Flags]
    public enum ShrinkDisableSide
    {
      /// <summary>
      /// Shrink on all four sides.
      /// </summary>
      ShrinkAllSides = 0x0000,
      /// <summary>
      /// Do not shrink on the west side of domain.
      /// </summary>
      DoNotShrinkWestSide = 0x0001,
      /// <summary>
      /// Do not shrink on the south side of domain.
      /// </summary>
      DoNotShrinkSouthSide = 0x0002,
      /// <summary>
      /// Do not shrink on the east side of domain.
      /// </summary>
      DoNotShrinkEastSide = 0x0004,
      /// <summary>
      /// Do not shrink on the north side of domain.
      /// </summary>
      DoNotShrinkNorthSide = 0x0008
    }

    /// <summary>
    /// Shrinks the underlying untrimmed surface of this Brep face close to trimming boundaries.
    /// Shrinking a surface is like extending smoothly, only backwards. knot of full multiplicity
    /// are added where you want the surface to be cut off. Then the remaining control points are
    /// thrown away.  
    /// </summary>
    /// <param name="disableSide">The side(s) of the surface to not shrink.</param>
    /// <returns>true on success, false on failure.</returns>
    public bool ShrinkFace(ShrinkDisableSide disableSide)
    {
      // https://mcneel.myjetbrains.com/youtrack/issue/RH-39877
      // This function should be named ShrinkSurface. But there is already a BrepFaceList::ShrinkFaces() method. 
      // So I am just perpetuating the incorrect term for the sake of consistency.
      IntPtr ptr_brep = m_brep.NonConstPointer();
      return UnsafeNativeMethods.ON_Brep_ShrinkFace(ptr_brep, FaceIndex, (int)disableSide);
    }

    /// <summary>
    /// Sets the surface domain of this face.
    /// </summary>
    /// <param name="direction">Direction of face to set (0 = U, 1 = V).</param>
    /// <param name="domain">Domain to apply.</param>
    /// <returns>true on success, false on failure.</returns>
    public override bool SetDomain(int direction, Interval domain)
    {
      bool rc = false;
      IntPtr ptr_brep = m_brep.NonConstPointer();
      IntPtr ptr_brep_face = UnsafeNativeMethods.ON_Brep_BrepFacePointer(ptr_brep, FaceIndex);
      if (IntPtr.Zero != ptr_brep_face)
      {
        rc = UnsafeNativeMethods.ON_Surface_SetDomain(ptr_brep_face, direction, domain);
      }
      return rc;
    }

    /// <summary>
    /// Duplicate a face from the brep to create new single face brep.
    /// </summary>
    /// <param name="duplicateMeshes">If true, shading meshes will be copied as well.</param>
    /// <returns>A new single-face brep synonymous with the current Face.</returns>
    [ConstOperation]
    public Brep DuplicateFace(bool duplicateMeshes)
    {
      IntPtr ptr_const_brep = m_brep.ConstPointer();
      IntPtr ptr_new_brep = UnsafeNativeMethods.ON_Brep_DuplicateFace(ptr_const_brep, FaceIndex, duplicateMeshes);
      return IntPtr.Zero == ptr_new_brep ? null : new Brep(ptr_new_brep, null);
    }
    /// <summary>
    /// Gets a copy to the untrimmed surface that this face is based on.
    /// </summary>
    /// <returns>A copy of this face's underlying surface.</returns>
    [ConstOperation]
    public Surface DuplicateSurface()
    {
      IntPtr ptr_const_brep = m_brep.ConstPointer();
      IntPtr ptr_surface = UnsafeNativeMethods.ON_Brep_DuplicateFaceSurface(ptr_const_brep, FaceIndex);
      return CreateGeometryHelper(ptr_surface, null) as Surface;
    }

    /// <summary>
    /// Gets the untrimmed surface that is the base of this face.
    /// </summary>
    /// <returns>A surface, or null on error.</returns>
    [ConstOperation]
    public Surface UnderlyingSurface()
    {
      IntPtr ptr_const_brep = m_brep.ConstPointer();
      IntPtr ptr_surface = UnsafeNativeMethods.ON_BrepFace_SurfaceOf(ptr_const_brep, FaceIndex);
      return CreateGeometryHelper(ptr_surface, new SurfaceOfHolder(this)) as Surface;
    }

#if RHINO_SDK
    /// <summary>
    /// Split this face using 3D trimming curves.
    /// </summary>
    /// <param name="curves">Curves to split with.</param>
    /// <param name="tolerance">Tolerance for splitting, when in doubt use the Document Absolute Tolerance.</param>
    /// <returns>A brep consisting of all the split fragments, or null on failure.</returns>
    /// <example>
    /// <code source='examples\vbnet\ex_tightboundingbox.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_tightboundingbox.cs' lang='cs'/>
    /// <code source='examples\py\ex_tightboundingbox.py' lang='py'/>
    /// </example>
    [ConstOperation]
    public Brep Split(IEnumerable<Curve> curves, double tolerance)
    {
      if (null == curves) { return DuplicateFace(false); }

      using (var curve_array = new SimpleArrayCurvePointer(curves))
      {
        IntPtr ptr_curve_array = curve_array.ConstPointer();
        IntPtr ptr_const_brep = m_brep.ConstPointer();
        IntPtr rc = UnsafeNativeMethods.ON_Brep_SplitFace(ptr_const_brep, FaceIndex, ptr_curve_array, tolerance);
        GC.KeepAlive(curves);
        return IntPtr.Zero == rc ? null : new Brep(rc, null);
      }
    }

    /// <summary>
    /// Tests if a parameter space point is on the interior of a trimmed face.
    /// </summary>
    /// <param name="u">Parameter space point u value.</param>
    /// <param name="v">Parameter space point v value.</param>
    /// <returns>A value describing the spatial relationship between the point and the face.</returns>
    [ConstOperation]
    public PointFaceRelation IsPointOnFace(double u, double v)
    {
      IntPtr ptr_const_brep = m_brep.ConstPointer();
      int rc = UnsafeNativeMethods.TL_Brep_PointIsOnFace(ptr_const_brep, FaceIndex, u, v);
      if (1 == rc)
        return PointFaceRelation.Interior;
      return 2 == rc ? PointFaceRelation.Boundary : PointFaceRelation.Exterior;
    }

    /// <summary>
    /// Gets intervals where the iso curve exists on a BrepFace (trimmed surface)
    /// </summary>
    /// <param name="direction">Direction of isocurve.
    /// <para>0 = Isocurve connects all points with a constant U value.</para>
    /// <para>1 = Isocurve connects all points with a constant V value.</para>
    /// </param>
    /// <param name="constantParameter">Surface parameter that remains identical along the isocurves.</param>
    /// <returns>
    /// If direction = 0, the parameter space iso interval connects the 2d points
    /// (intervals[i][0],iso_constant) and (intervals[i][1],iso_constant).
    /// If direction = 1, the parameter space iso interval connects the 2d points
    /// (iso_constant,intervals[i][0]) and (iso_constant,intervals[i][1]).
    /// </returns>
    [ConstOperation]
    public Interval[] TrimAwareIsoIntervals(int direction, double constantParameter)
    {
      using (var rc = new SimpleArrayInterval())
      {
        IntPtr ptr_const_brep = m_brep.ConstPointer();
        IntPtr ptr_intervals = rc.NonConstPointer();
        UnsafeNativeMethods.RHC_RhinoGetBrepFaceIsoIntervals(ptr_const_brep, FaceIndex, direction, constantParameter, ptr_intervals);
        return rc.ToArray();
      }
    }

    /// <summary>
    /// Similar to IsoCurve function, except this function pays attention to trims on faces 
    /// and may return multiple curves.
    /// </summary>
    /// <param name="direction">Direction of isocurve.
    /// <para>0 = Isocurve connects all points with a constant U value.</para>
    /// <para>1 = Isocurve connects all points with a constant V value.</para>
    /// </param>
    /// <param name="constantParameter">Surface parameter that remains identical along the isocurves.</param>
    /// <returns>Isoparametric curves connecting all points with the constantParameter value.</returns>
    /// <remarks>
    /// In this function "direction" indicates which direction the resulting curve runs.
    /// 0: horizontal, 1: vertical
    /// In the other Surface functions that take a "direction" argument,
    /// "direction" indicates if "constantParameter" is a "u" or "v" parameter.
    /// </remarks>
    [ConstOperation]
    public Curve[] TrimAwareIsoCurve(int direction, double constantParameter)
    {
      IntPtr ptr_const_brep = m_brep.ConstPointer();
      using (var curves = new SimpleArrayCurvePointer())
      {
        IntPtr ptr_curves = curves.NonConstPointer();
        int count = UnsafeNativeMethods.RHC_RhinoGetBrepFaceIsoCurves(ptr_const_brep, FaceIndex, direction, constantParameter, ptr_curves);
        Curve[] rc = new Curve[0];
        if (count > 0)
          rc = curves.ToNonConstArray();
        return rc;
      }
    }
#endif

    /// <summary>
    /// Obtains a reference to a specified type of mesh for this brep face.
    /// </summary>
    /// <param name="meshType">The mesh type.</param>
    /// <returns>A mesh.</returns>
    [ConstOperation]
    public Mesh GetMesh(MeshType meshType)
    {
      IntPtr ptr_const_brep = m_brep.ConstPointer();
      IntPtr const_ptr_mesh = UnsafeNativeMethods.ON_BrepFace_Mesh(ptr_const_brep, FaceIndex, (int)meshType);
      return CreateGeometryHelper(const_ptr_mesh, new MeshHolder(this, meshType)) as Mesh;
    }

    /// <summary>
    /// Sets a reference to a specified type of mesh for this brep face.
    /// </summary>
    /// <param name="meshType">The mesh type.</param>
    /// <param name="mesh">The new mesh.</param>
    /// <returns>true if the operation succeeded; otherwise false.</returns>
    public bool SetMesh(MeshType meshType, Mesh mesh)
    {
      IntPtr ptr_this = NonConstPointer();

      //make sure the mesh isn't parented to any other object
      mesh.EnsurePrivateCopy();
      IntPtr ptr_mesh = mesh.NonConstPointer();
      bool rc = UnsafeNativeMethods.ON_BrepFace_SetMesh(ptr_this, ptr_mesh, (int)meshType);
      MeshHolder mh = new MeshHolder(this, meshType);
      mesh.SetParent(mh);
      return rc;
    }

    /// <summary>
    /// Gets the indices of all the BrepEdges that delineate this Face.
    /// </summary>
    [ConstOperation]
    public int[] AdjacentEdges()
    {
      IntPtr ptr_const_brep = m_brep.ConstPointer();
      using (var ei = new SimpleArrayInt())
      {
        int rc = UnsafeNativeMethods.ON_Brep_FaceEdgeIndices(ptr_const_brep, FaceIndex, ei.m_ptr);
        return rc == 0 ? new int[0] : ei.ToArray();
      }
    }
    /// <summary>
    /// Gets the indices of all the BrepFaces that surround (are adjacent to) this face.
    /// </summary>
    [ConstOperation]
    public int[] AdjacentFaces()
    {
      IntPtr ptr_const_brep = m_brep.ConstPointer();
      using (var fi = new SimpleArrayInt())
      {
        int rc = UnsafeNativeMethods.ON_Brep_FaceFaceIndices(ptr_const_brep, FaceIndex, fi.m_ptr);
        return rc == 0 ? new int[0] : fi.ToArray();
      }
    }

#if RHINO_SDK
    /// <summary>
    /// Expert user tool that replaces the 3d surface geometry use by the face.
    /// </summary>
    /// <param name="surfaceIndex">brep surface index of new surface.</param>
    /// <returns>true if successful.</returns>
    /// <remarks>
    /// If the face had a surface and new surface has a different shape, then
    /// you probably want to call something like RebuildEdges() to move
    /// the 3d edge curves so they will lie on the new surface. This doesn't
    /// delete the old surface; call Brep.CullUnusedSurfaces() or Brep.Compact()
    /// to remove unused surfaces.
    /// </remarks>
    public bool ChangeSurface(int surfaceIndex)
    {
      IntPtr ptr_brep = m_brep.NonConstPointer();
      return UnsafeNativeMethods.ON_BrepFace_ChangeSurface(ptr_brep, FaceIndex, surfaceIndex);
    }

    /// <summary>
    /// Rebuild the edges used by a face so they lie on the surface.
    /// </summary>
    /// <param name="tolerance">tolerance for fitting 3d edge curves.</param>
    /// <param name="rebuildSharedEdges">
    /// if false and and edge is used by this face and a neighbor, then the edge
    /// will be skipped.
    /// </param>
    /// <param name="rebuildVertices">
    /// if true, vertex locations are updated to lie on the surface.
    /// </param>
    /// <returns>true on success.</returns>
    public bool RebuildEdges(double tolerance, bool rebuildSharedEdges, bool rebuildVertices)
    {
      IntPtr ptr_brep = m_brep.NonConstPointer();
      return UnsafeNativeMethods.ON_Brep_RebuildEdges(ptr_brep, FaceIndex, tolerance, rebuildSharedEdges, rebuildVertices);
    }

#endif
    #endregion
  }


#if RHINO_SDK
  /// <summary>
  /// Represents a brep topological region that has sides.
  /// </summary>
  public class BrepRegion : Runtime.CommonObject
  {
    internal BrepRegion(Brep brep, int index)
    {
      Brep = brep;
      Index = index;
    }

    /// <summary>Gets a reference to the Brep this region belongs to.</summary>
    public Brep Brep { get; }

    /// <summary>Gets the index of region in the RegionTopology array.</summary>
    public int Index { get; }

    /// <summary>
    /// Gets a value indicating whether this region is finite.
    /// </summary>
    public bool IsFinite
    {
      get
      {
        IntPtr const_ptr_brep = Brep.ConstPointer();
        return UnsafeNativeMethods.ON_BrepRegion_IsFinite(const_ptr_brep, Index);
      }
    }

    /// <summary>Gets the region bounding box.</summary>
    public BoundingBox BoundingBox
    {
      get
      {
        IntPtr ptr_const_brep = Brep.ConstPointer();
        var rc = new BoundingBox();
        UnsafeNativeMethods.ON_BrepRegion_BoundingBox(ptr_const_brep, Index, ref rc);
        return rc;
      }
    }

    /// <summary>
    /// Gets the boundary of a region as a brep object. If the region is finite,
    /// the boundary will be a closed  manifold brep. The boundary may have more than one
    /// connected component.
    /// </summary>
    /// <returns>A brep or null on error.</returns>
    [ConstOperation]
    public Brep BoundaryBrep()
    {
      IntPtr ptr_const_brep = Brep.ConstPointer();
      IntPtr ptr_brep = UnsafeNativeMethods.ON_BrepRegion_RegionBoundaryBrep(ptr_const_brep, Index);
      return GeometryBase.CreateGeometryHelper(ptr_brep, null) as Brep;
    }

    /// <summary>
    /// Gets an array of <see cref="BrepRegionFaceSide"/> entities delimiting this region.
    /// </summary>
    /// <returns>An array of region face sides. This array might be empty on failure.</returns>
    [ConstOperation]
    public BrepRegionFaceSide[] GetFaceSides()
    {
      IntPtr ptr_const_brep = Brep.ConstPointer();
      int count = UnsafeNativeMethods.ON_BrepRegion_FaceSideCount(ptr_const_brep, Index);
      var rc = new BrepRegionFaceSide[count];
      for (int i = 0; i < count; i++)
        rc[i] = new BrepRegionFaceSide(this, i);
      return rc;
    }

    internal override IntPtr NonConstPointer()
    {
      NonConstOperation();
      IntPtr ptr_brep = Brep.NonConstPointer();
      return UnsafeNativeMethods.ON_Brep_BrepRegion(ptr_brep, Index);
    }

    /// <summary> </summary>
    protected override void NonConstOperation()
    {
      // force the parent brep to be non-const
      Brep.NonConstPointer();
    }

    internal override IntPtr _InternalGetConstPointer()
    {
      IntPtr const_ptr_brep = Brep.ConstPointer();
      return UnsafeNativeMethods.ON_Brep_BrepRegion(const_ptr_brep, Index);
    }

    internal override IntPtr _InternalDuplicate(out bool applymempressure)
    {
      throw new NotImplementedException("This should never be called since we are directly overriding NonConstOperation.");
    }
  }

  /// <summary>
  /// Represents a side of a <see cref="BrepRegion"/> entity.
  /// </summary>
  public class BrepRegionFaceSide : Runtime.CommonObject
  {
    readonly BrepRegion m_parent;
    readonly int m_index;

    internal BrepRegionFaceSide(BrepRegion parent, int index)
    {
      m_parent = parent;
      m_index = index;
    }

    /// <summary>
    /// The brep this side belongs to.
    /// </summary>
    public Brep Brep => m_parent.Brep;

    /// <summary>
    /// The region this side belongs to.
    /// </summary>
    public BrepRegion Region => m_parent;

    /// <summary>
    /// Gets true if BrepFace's surface normal points into region; false otherwise.
    /// </summary>
    public bool SurfaceNormalPointsIntoRegion
    {
      get
      {
        IntPtr ptr_const_brep = m_parent.Brep.ConstPointer();
        int region_index = m_parent.Index;
        return UnsafeNativeMethods.ON_BrepFaceSide_SurfaceNormalDirection(ptr_const_brep, region_index, m_index) == 1;
      }
    }

    /// <summary>Gets the face this side belongs to.</summary>
    public BrepFace Face
    {
      get
      {
        IntPtr ptr_const_brep = m_parent.Brep.ConstPointer();
        int region_index = m_parent.Index;
        int face_index = UnsafeNativeMethods.ON_BrepFaceSide_Face(ptr_const_brep, region_index, m_index);
        if (face_index < 0)
          return null;
        return new BrepFace(face_index, Brep);
      }
    }

    internal override IntPtr NonConstPointer()
    {
      NonConstOperation();
      IntPtr ptr_region = m_parent.NonConstPointer();
      return UnsafeNativeMethods.ON_BrepRegion_FaceSide(ptr_region, m_index);
    }

    /// <summary> </summary>
    protected override void NonConstOperation()
    {
      // force the parent region to be non-const
      m_parent.NonConstPointer();
    }

    internal override IntPtr _InternalGetConstPointer()
    {
      IntPtr const_ptr_region = m_parent.ConstPointer();
      return UnsafeNativeMethods.ON_BrepRegion_FaceSide(const_ptr_region, m_index);
    }

    internal override IntPtr _InternalDuplicate(out bool applymempressure)
    {
      throw new NotImplementedException("This should never be called since we are directly overriding NonConstOperation.");
    }
  }
#endif

  class MeshHolder
  {
    readonly BrepFace m_face;
    readonly Extrusion m_extrusion;
    readonly MeshType m_meshtype;
#if RHINO_SDK
    Render.RenderPrimitive m_primitive;
    Render.RenderPrimitiveList m_parent_list;
    readonly int m_index;
#endif

    public MeshHolder(BrepFace face, MeshType meshType)
    {
      m_face = face;
      m_meshtype = meshType;
    }
    public MeshHolder(Extrusion extrusion, MeshType meshType)
    {
      m_extrusion = extrusion;
      m_meshtype = meshType;
    }
#if RHINO_SDK
    public MeshHolder(Render.RenderPrimitiveList parentList, int index)
    {
      m_parent_list = parentList;
      m_index = index;
      m_parent_list?.IncrementDependencyCount();
    }

    public MeshHolder(Render.RenderPrimitive primitive)
    {
      m_primitive = primitive;
    }
#endif

    public void ReleaseMesh()
    {
#if RHINO_SDK
      if (m_parent_list != null)
      {
        m_parent_list.DecrementDependencyCount();
        m_parent_list = null;
      }
      m_primitive = null;
#endif
    }

    public IntPtr MeshPointer()
    {
      if (m_face != null)
      {
        IntPtr ptr_const_brep = m_face.m_brep.ConstPointer();
        return UnsafeNativeMethods.ON_BrepFace_Mesh(ptr_const_brep, m_face.FaceIndex, (int)m_meshtype);
      }
      if (m_extrusion != null)
      {
        IntPtr ptr_const_extrusion = m_extrusion.ConstPointer();
        return UnsafeNativeMethods.ON_Extrusion_GetMesh(ptr_const_extrusion, (int)m_meshtype);
      }
#if RHINO_SDK
      if (m_parent_list != null)
      {
        IntPtr const_ptr_parent = m_parent_list.ConstPointer();
        return UnsafeNativeMethods.Rdk_CustomMeshes_Mesh(const_ptr_parent, m_index);
      }
      if (m_primitive != null)
      {
        IntPtr const_ptr_primitive = m_primitive.ConstPointer();
        return UnsafeNativeMethods.Rdk_RenderMesh_Mesh(const_ptr_primitive);
      }
#endif
      return IntPtr.Zero;
    }
  }

  class SurfaceOfHolder
  {
    readonly BrepFace m_face;
    public SurfaceOfHolder(BrepFace face)
    {
      m_face = face;
    }
    public IntPtr SurfacePointer()
    {
      IntPtr ptr_brep = m_face.m_brep.ConstPointer();
      return UnsafeNativeMethods.ON_BrepFace_SurfaceOf(ptr_brep, m_face.FaceIndex);
    }
  }

  class SurfaceHolder
  {
    readonly Brep m_brep;
    readonly int m_index;
    public SurfaceHolder(Brep brep, int index)
    {
      m_brep = brep;
      m_index = index;
    }

    public IntPtr ConstSurfacePointer()
    {
      IntPtr ptr_const_brep = m_brep.ConstPointer();
      return UnsafeNativeMethods.ON_Brep_BrepSurfacePointer(ptr_const_brep, m_index);
    }
  }

  class CurveHolder
  {
    readonly Leader m_leader;
    readonly Brep m_brep;
    readonly RevSurface m_revsurface;
    readonly int m_index;
    readonly bool m_c2;
    public CurveHolder(Brep brep, int index, bool c2)
    {
      m_brep = brep;
      m_index = index;
      m_c2 = c2;
    }
    public CurveHolder(Leader leader)
    {
      m_leader = leader;
    }
    public CurveHolder(RevSurface revSurface)
    {
      m_revsurface = revSurface;
    }

    public IntPtr ConstCurvePointer()
    {
      if (m_brep != null)
      {
        IntPtr ptr_const_brep = m_brep.ConstPointer();
        return UnsafeNativeMethods.ON_Brep_BrepCurvePointer(ptr_const_brep, m_index, m_c2);
      }
      if (m_leader != null)
      {
        IntPtr const_ptr_leader = m_leader.ConstPointer();
        return UnsafeNativeMethods.ON_V6_Leader_Curve(const_ptr_leader, IntPtr.Zero); //Null Dimstyle
      }
      if( m_revsurface != null)
      {
        IntPtr const_ptr_revsrf = m_revsurface.ConstPointer();
        return UnsafeNativeMethods.ON_RevSurface_Curve(const_ptr_revsrf);
      }
      return IntPtr.Zero;
    }
    public IntPtr NonConstCurvePointer()
    {
      if (m_brep != null)
      {
        IntPtr ptr_brep = m_brep.NonConstPointer();
        return UnsafeNativeMethods.ON_Brep_BrepCurvePointer(ptr_brep, m_index, m_c2);
      }
      if (m_leader != null)
      {
        IntPtr ptr_leader = m_leader.NonConstPointer();
        return UnsafeNativeMethods.ON_V6_Leader_Curve(ptr_leader, IntPtr.Zero); //Null Dimstyle
      }
      if (m_revsurface != null)
      {
        IntPtr ptr_revsrf = m_revsurface.NonConstPointer();
        return UnsafeNativeMethods.ON_RevSurface_Curve(ptr_revsrf);
      }
      return IntPtr.Zero;
    }
  }
}

namespace Rhino.Geometry.Collections
{
  /// <summary>
  /// Provides access to all the Vertices in a Brep object
  /// </summary>
  public class BrepVertexList : IEnumerable<BrepVertex>, Rhino.Collections.IRhinoTable<BrepVertex>
  {
    readonly Brep m_brep;
    internal BrepVertexList(Brep ownerBrep)
    {
      m_brep = ownerBrep;
    }

    #region properties
    /// <summary>
    /// Gets the number of brep vertices.
    /// </summary>
    public int Count
    {
      get
      {
        IntPtr ptr_const_brep = m_brep.ConstPointer();
        return UnsafeNativeMethods.ON_Brep_GetInt(ptr_const_brep, UnsafeNativeMethods.BrepInt.VertexCount);
      }
    }

    /// <summary>
    /// Gets the BrepVertex at the given index. 
    /// The index must be valid or an IndexOutOfRangeException will be thrown.
    /// </summary>
    /// <param name="index">Index of BrepVertex to access.</param>
    /// <exception cref="IndexOutOfRangeException">Thrown when the index is invalid.</exception>
    /// <returns>The BrepVertex at [index].</returns>
    public BrepVertex this[int index]
    {
      get
      {
        int count = Count;
        if (index < 0 || index >= count)
          throw new IndexOutOfRangeException();

        return new BrepVertex(index, m_brep);
      }
    }
    #endregion

    /// <summary>
    /// Create and add a new vertex to this list
    /// </summary>
    /// <returns></returns>
    public BrepVertex Add()
    {
      IntPtr ptr_brep = m_brep.NonConstPointer();
      int index = UnsafeNativeMethods.ON_Brep_NewVertex(ptr_brep);
      if (index < 0)
        return null;
      return new BrepVertex(index, m_brep);
    }

    /// <summary>
    /// Create and add a new vertex to this list
    /// </summary>
    /// <param name="point"></param>
    /// <param name="vertexTolerance">Use RhinoMath.UnsetTolerance if you are unsure</param>
    /// <returns></returns>
    public BrepVertex Add(Point3d point, double vertexTolerance)
    {
      IntPtr ptr_brep = m_brep.NonConstPointer();
      int index = UnsafeNativeMethods.ON_Brep_NewVertex2(ptr_brep, point, vertexTolerance);
      if (index < 0)
        return null;
      return new BrepVertex(index, m_brep);
    }

    /// <summary>Adds a new point on face to the brep</summary>
    /// <param name="face">face that vertex lies on</param>
    /// <param name="s">surface parameters</param>
    /// <param name="t">surface parameters</param>
    /// <returns>new vertex that represents the point on face</returns>
    /// <remarks>
    /// If a vertex is a point on a face, then brep.Edges[m_ei] will
    /// be an edge with no 3d curve.  This edge will have a single
    /// trim with type ON_BrepTrim::ptonsrf.  There will be a loop
    /// containing this single trim.
    /// </remarks>
    public BrepVertex AddPointOnFace(BrepFace face, double s, double t)
    {
      IntPtr ptr_brep = m_brep.NonConstPointer();
      int index = UnsafeNativeMethods.ON_Brep_NewPointOnFace(ptr_brep, face.FaceIndex, s, t);
      if (index < 0)
        return null;
      return new BrepVertex(index, m_brep);
    }

    #region IEnumerable Implementation

    /// <summary>
    /// Gets the same enumerator as <see cref="GetEnumerator"/>.
    /// </summary>
    /// <returns>The enumerator.</returns>
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
    {
      return GetEnumerator();
    }

    /// <summary>
    /// Gets an enumerator that visits all surfaces.
    /// </summary>
    /// <returns>The enumerator.</returns>
    public IEnumerator<BrepVertex> GetEnumerator()
    {
      return new Rhino.Collections.TableEnumerator<BrepVertexList, BrepVertex>(this);
    }
    #endregion
  }

  /// <summary>
  /// Provides access to all the Faces in a Brep object.
  /// </summary>
  public class BrepFaceList : IEnumerable<BrepFace>, Rhino.Collections.IRhinoTable<BrepFace>
  {
    readonly Brep m_brep;

    #region constructors
    internal BrepFaceList(Brep ownerBrep)
    {
      m_brep = ownerBrep;
    }
    #endregion

    #region properties
    /// <summary>
    /// Gets the number of brep faces.
    /// </summary>
    public int Count
    {
      get
      {
        IntPtr ptr_const_brep = m_brep.ConstPointer();
        return UnsafeNativeMethods.ON_Brep_GetInt(ptr_const_brep, UnsafeNativeMethods.BrepInt.FaceCount);
      }
    }

    /// <summary>
    /// Gets the BrepFace at the given index. 
    /// The index must be valid or an IndexOutOfRangeException will be thrown.
    /// </summary>
    /// <param name="index">Index of BrepFace to access.</param>
    /// <exception cref="IndexOutOfRangeException">Thrown when the index is invalid.</exception>
    /// <returns>The BrepFace at [index].</returns>
    public BrepFace this[int index]
    {
      get
      {
        int count = Count;
        if (index < 0 || index >= count)
        {
          throw new IndexOutOfRangeException();
        }
        if (m_faces == null)
          m_faces = new List<BrepFace>(count);
        int existing_list_count = m_faces.Count;
        for (int i = existing_list_count; i < count; i++)
        {
          m_faces.Add(new BrepFace(i, m_brep));
        }

        return m_faces[index];
      }
    }
    List<BrepFace> m_faces; // = null; initialized to null by runtime
    #endregion

    #region methods
    /// <summary>
    /// Shrinks all the underlying surfaces in this Brep. 
    /// Sometimes the surfaces extend far beyond the trimming boundaries of the Brep Face. 
    /// This function will remove those portions of the surfaces that are not used.
    /// </summary>
    /// <returns>true on success, false on failure.</returns>
    public bool ShrinkFaces()
    {
      IntPtr ptr_brep = m_brep.NonConstPointer();
      return UnsafeNativeMethods.ON_Brep_ShrinkFaces(ptr_brep);
    }

#if RHINO_SDK
    /// <summary>
    /// Splits any faces with creases into G1 pieces.
    /// </summary>
    /// <returns>true on success, false on failure.</returns>
    /// <remarks>If you need to detect whether splitting occured, 
    /// compare the before and after values of Faces.Count </remarks>
    public bool SplitKinkyFaces()
    {
      return SplitKinkyFaces(1e-2, false);
    }
    /// <summary>
    /// Splits any faces with creases into G1 pieces.
    /// </summary>
    /// <param name="kinkTolerance">Tolerance (in radians) to use for crease detection.</param>
    /// <returns>true on success, false on failure.</returns>
    /// <remarks>If you need to detect whether splitting occured, 
    /// compare the before and after values of Faces.Count </remarks>
    public bool SplitKinkyFaces(double kinkTolerance)
    {
      return SplitKinkyFaces(kinkTolerance, false);
    }
    /// <summary>
    /// Splits any faces with creases into G1 pieces.
    /// </summary>
    /// <param name="kinkTolerance">Tolerance (in radians) to use for crease detection.</param>
    /// <param name="compact">If true, the Brep will be compacted if possible.</param>
    /// <returns>true on success, false on failure.</returns>
    /// <remarks>If you need to detect whether splitting occured, 
    /// compare the before and after values of Faces.Count </remarks>
    public bool SplitKinkyFaces(double kinkTolerance, bool compact)
    {
      IntPtr ptr_brep = m_brep.NonConstPointer();
      return UnsafeNativeMethods.ON_Brep_SplitKinkyFaces(ptr_brep, kinkTolerance, compact);
    }

    /// <summary>
    /// Splits a single face into G1 pieces.
    /// </summary>
    /// <param name="faceIndex">The index of the face to split.</param>
    /// <param name="kinkTolerance">Tolerance (in radians) to use for crease detection.</param>
    /// <returns>true on success, false on failure.</returns>
    /// <remarks>
    /// This function leaves deleted stuff in the brep.  Call Brep.Compact() to
    /// remove deleted stuff.
    /// </remarks>
    public bool SplitKinkyFace(int faceIndex, double kinkTolerance)
    {
      IntPtr ptr_brep = m_brep.NonConstPointer();
      return UnsafeNativeMethods.ON_Brep_SplitKinkyFace(ptr_brep, faceIndex, kinkTolerance);
    }

    /// <summary>
    /// Splits closed surfaces so they are not closed.
    /// </summary>
    /// <param name="minimumDegree">
    /// If the degree of the surface &lt; min_degree, the surface is not split.
    /// In some cases, minimumDegree = 2 is useful to preserve piecewise linear
    /// surfaces.
    /// </param>
    /// <returns>true if successful.</returns>
    public bool SplitClosedFaces(int minimumDegree)
    {
      IntPtr ptr_brep = m_brep.NonConstPointer();
      return UnsafeNativeMethods.ON_Brep_SplitClosedFaces(ptr_brep, minimumDegree);
    }

    /// <summary>
    /// Splits surfaces with two singularities, like spheres, so the results
    /// have at most one singularity.
    /// </summary>
    /// <returns>true if successful.</returns>
    public bool SplitBipolarFaces()
    {
      IntPtr ptr_brep = m_brep.NonConstPointer();
      return UnsafeNativeMethods.ON_Brep_SplitBipolarFaces(ptr_brep);
    }

    /// <summary>
    /// Splits all of the faces of a Brep at tangent locations.
    /// </summary>
    /// <returns>True if successful, false otherwise.</returns>
    public bool SplitFacesAtTangents()
    {
      IntPtr ptr_brep = m_brep.NonConstPointer();
      var rc = UnsafeNativeMethods.RHC_RhinoSplitFacesAtTangents(ptr_brep);
      return rc;
    }

    /// <summary>
    /// Splits the face of a Brep at tangent locations.
    /// </summary>
    /// <param name="faceIndex">The index of the face to split.</param>
    /// <returns>True if successful, false otherwise.</returns>
    public bool SplitFaceAtTangents(int faceIndex)
    {
      IntPtr ptr_brep = m_brep.NonConstPointer();
      var rc = UnsafeNativeMethods.RHC_RhinoSplitFaceAtTangents(ptr_brep, faceIndex);
      return rc;
    }
#endif

    /// <summary>
    /// Flips the orientation of faces.
    /// </summary>
    /// <param name="onlyReversedFaces">
    /// If true, clears all BrepFace.OrientationIsReversed flags by calling BrepFace.Transpose()
    /// on each face with a true OrientationIsReversed setting.
    /// If false, all of the faces are flipped regardless of their orientation.
    /// </param>
    public void Flip(bool onlyReversedFaces)
    {
      IntPtr ptr_brep = m_brep.NonConstPointer();
      if (onlyReversedFaces)
        UnsafeNativeMethods.ON_Brep_FlipReversedSurfaces(ptr_brep);
      else
        UnsafeNativeMethods.ON_Brep_Flip(ptr_brep);
    }

    /// <summary>
    /// Deletes a face at a specified index.
    /// </summary>
    /// <param name="faceIndex">The index of the mesh face.</param>
    public void RemoveAt(int faceIndex)
    {
      IntPtr ptr_brep = m_brep.NonConstPointer();
      UnsafeNativeMethods.ON_Brep_DeleteFace(ptr_brep, faceIndex);
      m_faces = null;
    }

    /// <summary>
    /// Extracts a face from a Brep.
    /// </summary>
    /// <param name="faceIndex">A face index</param>
    /// <returns>A brep. This can be null.</returns>
    public Brep ExtractFace(int faceIndex)
    {
      IntPtr ptr_brep = m_brep.NonConstPointer();
      IntPtr ptr_new_brep = UnsafeNativeMethods.ON_Brep_ExtractFace(ptr_brep, faceIndex);
      return GeometryBase.CreateGeometryHelper(ptr_new_brep, null) as Brep;
    }

    /// <summary>
    /// Standardizes the relationship between a BrepFace and the 3d surface it
    /// uses.  When done, the face will be the only face that references its 3d
    /// surface, and the orientations of the face and 3d surface will be the same. 
    /// </summary>
    /// <param name="faceIndex">The index of the face.</param>
    /// <returns>true if successful.</returns>
    public bool StandardizeFaceSurface(int faceIndex)
    {
      IntPtr ptr_brep = m_brep.NonConstPointer();
      return UnsafeNativeMethods.ON_Brep_StandardizeFaceSurface(ptr_brep, faceIndex);
    }

    /// <summary>Standardize all faces in the brep.</summary>
    public void StandardizeFaceSurfaces()
    {
      IntPtr ptr_brep = m_brep.NonConstPointer();
      UnsafeNativeMethods.ON_Brep_StandardizeFaceSurfaces(ptr_brep);
    }

    /// <summary>
    /// Create and add a new face to this list. An incomplete face is added.
    /// The caller must create and fill in the loops used by the face.
    /// </summary>
    /// <param name="surfaceIndex">index of surface in brep's Surfaces list</param>
    /// <returns></returns>
    public BrepFace Add(int surfaceIndex)
    {
      IntPtr ptr_brep = m_brep.NonConstPointer();
      int index = UnsafeNativeMethods.ON_Brep_NewFace(ptr_brep, surfaceIndex);
      if (index < 0)
        return null;
      return this[index];
    }

    /// <summary>
    /// Add a new face to a brep.  This creates a complete face with
    /// new vertices at the surface corners, new edges along the surface
    /// boundary, etc.  The loop of the returned face has four trims that
    /// correspond to the south, east, north, and west side of the 
    /// surface in that order.  If you use this version of Add to
    /// add an exiting brep, then you are responsible for using a tool
    /// like JoinEdges() to hook the new face to its neighbors.
    /// </summary>
    /// <param name="surface">surface is copied</param>
    /// <returns></returns>
    public BrepFace Add(Surface surface)
    {
      IntPtr ptr_brep = m_brep.NonConstPointer();
      IntPtr const_ptr_surface = surface.ConstPointer();
      int index = UnsafeNativeMethods.ON_Brep_NewFace2(ptr_brep, const_ptr_surface);
      GC.KeepAlive(surface);
      return index < 0 ? null : this[index];
    }

    /// <summary>
    /// Add a new face to the brep whose surface geometry is a 
    /// ruled surface between two edges.
    /// </summary>
    /// <param name="edgeA">
    /// The south side of the face's surface will run along edgeA.
    /// </param>
    /// <param name="revEdgeA">
    /// true if the new face's outer boundary orientation along
    /// edgeA is opposite the orientation of edgeA.
    /// </param>
    /// <param name="edgeB">
    /// The north side of the face's surface will run along edgeA
    /// </param>
    /// <param name="revEdgeB">
    /// true if the new face's outer boundary orientation along
    /// edgeB is opposite the orientation of edgeB
    /// </param>
    /// <returns></returns>
    public BrepFace AddRuledFace(BrepEdge edgeA, bool revEdgeA, BrepEdge edgeB, bool revEdgeB)
    {
      IntPtr ptr_brep = m_brep.NonConstPointer();
      int index = UnsafeNativeMethods.ON_Brep_NewRuledFace(ptr_brep, edgeA.EdgeIndex, revEdgeA, edgeB.EdgeIndex, revEdgeB);
      if (index < 0)
        return null;
      return this[index];
    }

    /// <summary>
    /// Add a new face to the brep whose surface geometry is a 
    /// ruled cone with the edge as the base and the vertex as
    /// the apex point.
    /// </summary>
    /// <param name="vertex">
    /// The apex of the cone will be at this vertex.
    /// The north side of the surface's parameter
    /// space will be a singular point at the vertex.
    /// </param>
    /// <param name="edge">
    /// The south side of the face's surface will run along this edge.
    /// </param>
    /// <param name="revEdge">
    /// true if the new face's outer boundary orientation along
    /// the edge is opposite the orientation of edge.
    /// </param>
    /// <returns></returns>
    public BrepFace AddConeFace(BrepVertex vertex, BrepEdge edge, bool revEdge)
    {
      IntPtr ptr_brep = m_brep.NonConstPointer();
      int index = UnsafeNativeMethods.ON_Brep_NewConeFace(ptr_brep, vertex.VertexIndex, edge.EdgeIndex, revEdge);
      if (index < 0)
        return null;
      return this[index];
    }

    /// <summary>
    /// Remove slit trims and slit boundaries from each face.
    /// </summary>
    /// <returns>true if any slits were removed</returns>
    public bool RemoveSlits()
    {
      IntPtr ptr_brep = m_brep.NonConstPointer();
      return UnsafeNativeMethods.ON_Brep_RemoveSlits(ptr_brep);
    }

    /*
    /// <summary>
    /// If faceIndex0 != faceIndex1 and Faces[faceIndex0] and Faces[faceIndex1]
    /// have the same surface, and they are joined along a set of edges that do
    /// not have any other faces, then this will combine the two faces into one.
    /// </summary>
    /// <param name="faceIndex0">-</param>
    /// <param name="faceIndex1">-</param>
    /// <returns>index of merged face if faces were successfully merged. -1 if not merged.</returns>
    /// <remarks>Caller should call Compact when done</remarks>
    public int MergeFaces(int faceIndex0, int faceIndex1)
    {
      IntPtr ptr_brep = m_brep.NonConstPointer();
      return UnsafeNativeMethods.ON_Brep_MergeFaces(ptr_brep, faceIndex0, faceIndex1);
    }

    /// <summary>Merge all possible faces that have same underlying surface.</summary>
    /// <returns>true if any faces were successfully merged.</returns>
    /// <remarks>Caller should call Compact() when done</remarks>
    public bool MergeFaces()
    {
      IntPtr ptr_brep = m_brep.NonConstPointer();
      return UnsafeNativeMethods.ON_Brep_MergeFaces2(ptr_brep);
    }
    */
    #endregion

    #region IEnumerable Implementation

    /// <summary>
    /// Gets the same enumerator as <see cref="GetEnumerator"/>.
    /// </summary>
    /// <returns>The enumerator.</returns>
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
    {
      return GetEnumerator();
    }

    /// <summary>
    /// Gets an enumerators that yields <see cref="BrepFace"/> objects.
    /// </summary>
    /// <returns>The enumerator.</returns>
    public IEnumerator<BrepFace> GetEnumerator()
    {
      return new Rhino.Collections.TableEnumerator<BrepFaceList, BrepFace>(this);
    }
    #endregion
  }

  /// <summary>
  /// Provides access to all the underlying surfaces in a Brep object.
  /// </summary>
  public class BrepSurfaceList : IEnumerable<Surface>, Rhino.Collections.IRhinoTable<Surface>
  {
    readonly Brep m_brep;

    #region constructors
    internal BrepSurfaceList(Brep ownerBrep)
    {
      m_brep = ownerBrep;
    }
    #endregion

    #region properties
    /// <summary>
    /// Gets the number of surfaces in a brep.
    /// </summary>
    public int Count
    {
      get
      {
        IntPtr ptr_const_brep = m_brep.ConstPointer();
        return UnsafeNativeMethods.ON_Brep_GetInt(ptr_const_brep, UnsafeNativeMethods.BrepInt.SurfaceCount);
      }
    }

    /// <summary>
    /// Gets the Surface at the given index. 
    /// The index must be valid or an IndexOutOfRangeException will be thrown.
    /// </summary>
    /// <param name="index">Index of Surface to access.</param>
    /// <exception cref="IndexOutOfRangeException">Thrown when the index is invalid.</exception>
    /// <returns>The Surface at [index].</returns>
    public Surface this[int index]
    {
      get
      {
        int count = Count;
        if (index < 0 || index >= count)
        {
          throw new IndexOutOfRangeException();
        }
        if (m_surfaces == null)
          m_surfaces = new List<Surface>(count);

        int existing_list_count = m_surfaces.Count;
        if (existing_list_count < count)
        {
          IntPtr ptr_const_brep = m_brep.ConstPointer();
          for (int i = existing_list_count; i < count; i++)
          {
            IntPtr ptr_surface = UnsafeNativeMethods.ON_Brep_BrepSurfacePointer(ptr_const_brep, i);
            var srf = GeometryBase.CreateGeometryHelper(ptr_surface, new SurfaceHolder(m_brep, i)) as Surface;
            m_surfaces.Add(srf);
          }
        }
        return m_surfaces[index];
      }
    }
    List<Surface> m_surfaces; // = null; initialized to null by runtime
    #endregion

    #region IEnumerable Implementation

    /// <summary>
    /// Gets the same enumerator as <see cref="GetEnumerator"/>.
    /// </summary>
    /// <returns>The enumerator.</returns>
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
    {
      return GetEnumerator();
    }

    /// <summary>
    /// Gets an enumerator that visits all surfaces.
    /// </summary>
    /// <returns>The enumerator.</returns>
    public IEnumerator<Surface> GetEnumerator()
    {
      return new Rhino.Collections.TableEnumerator<BrepSurfaceList, Surface>(this);
    }
    #endregion
  }

  /// <summary>
  /// Provides access to all the underlying curves in a Brep object.
  /// </summary>
  public class BrepCurveList : IEnumerable<Curve>, Rhino.Collections.IRhinoTable<Curve>
  {
    readonly Brep m_brep;
    readonly bool m_c2list; // if false, then m_c3

    #region constructors
    internal BrepCurveList(Brep ownerBrep, bool c2)
    {
      m_brep = ownerBrep;
      m_c2list = c2;
    }
    #endregion

    #region properties
    /// <summary>Gets the number of curves in this list.</summary>
    public int Count
    {
      get
      {
        IntPtr ptr_const_brep = m_brep.ConstPointer();
        var which = m_c2list ? UnsafeNativeMethods.BrepInt.C2Count : UnsafeNativeMethods.BrepInt.C3Count;
        return UnsafeNativeMethods.ON_Brep_GetInt(ptr_const_brep, which);
      }
    }

    /// <summary>
    /// Gets the Curve at the given index. 
    /// The index must be valid or an IndexOutOfRangeException will be thrown.
    /// </summary>
    /// <param name="index">Index of Curve to access.</param>
    /// <exception cref="IndexOutOfRangeException">Thrown when the index is invalid.</exception>
    /// <returns>The Curve at [index].</returns>
    public Curve this[int index]
    {
      get
      {
        int count = Count;
        if (index < 0 || index >= count)
        {
          throw new IndexOutOfRangeException();
        }
        if (m_curves == null)
          m_curves = new List<Curve>(count);

        int existing_list_count = m_curves.Count;
        if (existing_list_count < count)
        {
          IntPtr ptr_const_brep = m_brep.ConstPointer();
          for (int i = existing_list_count; i < count; i++)
          {
            IntPtr ptr_curve = UnsafeNativeMethods.ON_Brep_BrepCurvePointer(ptr_const_brep, i, m_c2list);
            Curve crv = GeometryBase.CreateGeometryHelper(ptr_curve, new CurveHolder(m_brep, i, m_c2list)) as Curve;
            m_curves.Add(crv);
          }
        }
        return m_curves[index];
      }
    }
    List<Curve> m_curves; // = null; initialized to null by runtime
    #endregion

    #region methods
    /// <summary>Adds a curve</summary>
    /// <param name="curve">A copy of the curve is added to this brep</param>
    /// <returns>
    /// Index that should be used to reference the geometry.
    /// <para>-1 is returned if the input is not acceptable.</para>
    /// </returns>
    public int Add(Curve curve)
    {
      IntPtr const_ptr_curve = curve.ConstPointer();
      IntPtr ptr_brep = m_brep.NonConstPointer();
      int rc = UnsafeNativeMethods.ON_Brep_AddCurve(ptr_brep, const_ptr_curve, m_c2list);
      GC.KeepAlive(curve);
      return rc;
    }
    #endregion

    #region IEnumerable Implementation

    /// <summary>
    /// Gets the same enumerator as <see cref="GetEnumerator"/>.
    /// </summary>
    /// <returns>The enumerator.</returns>
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
    {
      return GetEnumerator();
    }

    /// <summary>Get an enumerator that visits all curves.</summary>
    /// <returns>The enumerator.</returns>
    public IEnumerator<Curve> GetEnumerator()
    {
      return new Rhino.Collections.TableEnumerator<BrepCurveList, Curve>(this);
    }
    #endregion
  }

  /// <summary>
  /// Provides access to all the Edges in a Brep object.
  /// </summary>
  public class BrepEdgeList : IEnumerable<BrepEdge>, Rhino.Collections.IRhinoTable<BrepEdge>
  {
    readonly Brep m_brep;
    List<BrepEdge> m_edges; // = null; initialized to null by runtime

    #region constructors
    internal BrepEdgeList(Brep ownerBrep)
    {
      m_brep = ownerBrep;
    }
    #endregion

    #region properties
    /// <summary>
    /// Gets the number of brep edges.
    /// </summary>
    public int Count
    {
      get
      {
        IntPtr ptr_const_brep = m_brep.ConstPointer();
        return UnsafeNativeMethods.ON_Brep_GetInt(ptr_const_brep, UnsafeNativeMethods.BrepInt.EdgeCount);
      }
    }

    /// <summary>
    /// Gets the BrepEdge at the given index. 
    /// The index must be valid or an IndexOutOfRangeException will be thrown.
    /// </summary>
    /// <param name="index">Index of BrepEdge to access.</param>
    /// <exception cref="IndexOutOfRangeException">Thrown when the index is invalid.</exception>
    /// <returns>The BrepEdge at [index].</returns>
    public BrepEdge this[int index]
    {
      get
      {
        int count = Count;
        if (index < 0 || index >= count)
        {
          throw new IndexOutOfRangeException();
        }
        if (m_edges == null)
          m_edges = new List<BrepEdge>(count);
        int existing_list_count = m_edges.Count;
        for (int i = existing_list_count; i < count; i++)
        {
          m_edges.Add(new BrepEdge(i, m_brep));
        }

        return m_edges[index];
      }
    }
    #endregion

    #region methods
#if RHINO_SDK
    /// <summary>Splits the edge into G1 pieces.</summary>
    /// <param name="edgeIndex">Index of edge to test and split.</param>
    /// <param name="kinkToleranceRadians">The split tolerance in radians.</param>
    /// <returns>true if successful.</returns>
    /// <remarks>
    /// This function leaves deleted stuff in the brep.  Call Brep.Compact() to
    /// remove deleted stuff.
    /// </remarks>
    public bool SplitKinkyEdge(int edgeIndex, double kinkToleranceRadians)
    {
      IntPtr ptr_brep = m_brep.NonConstPointer();
      return UnsafeNativeMethods.ON_Brep_SplitKinkyEdge(ptr_brep, edgeIndex, kinkToleranceRadians);
    }

    /// <summary>
    /// Splits an edge at the specified parameters.
    /// </summary>
    /// <param name="edgeIndex">The index of the edge to be addressed.</param>
    /// <param name="edgeParameters">The parameter along that edge.</param>
    /// <returns>
    /// Number of splits applied to the edge.
    /// </returns>
    /// <remarks>
    /// This function leaves deleted stuff in the brep.  Call Brep.Compact() to
    /// remove deleted stuff.
    /// </remarks>
    public int SplitEdgeAtParameters(int edgeIndex, IEnumerable<double> edgeParameters)
    {
      List<double> t = new List<double>(edgeParameters);
      double[] t_array = t.ToArray();
      IntPtr ptr_brep = m_brep.NonConstPointer();
      return UnsafeNativeMethods.ON_Brep_SplitEdgeAtParameters(ptr_brep, edgeIndex, t_array.Length, t_array);
    }
#endif

    /// <summary>
    /// Create and add a new edge to this list
    /// </summary>
    /// <param name="curve3dIndex"></param>
    /// <returns></returns>
    public BrepEdge Add(int curve3dIndex)
    {
      IntPtr ptr_brep = m_brep.NonConstPointer();
      int index = UnsafeNativeMethods.ON_Brep_NewEdge(ptr_brep, curve3dIndex);
      if (index < 0)
        return null;
      return this[index]; // this way the BrepEdge is properly in the list
    }

    /// <summary>
    /// Create and add a new edge to this list
    /// </summary>
    /// <param name="startVertex"></param>
    /// <param name="endVertex"></param>
    /// <param name="curve3dIndex"></param>
    /// <param name="subDomain"></param>
    /// <param name="edgeTolerance"></param>
    /// <returns></returns>
    public BrepEdge Add(BrepVertex startVertex, BrepVertex endVertex, int curve3dIndex, Interval subDomain, double edgeTolerance)
    {
      return Add(startVertex.VertexIndex, endVertex.VertexIndex, curve3dIndex, subDomain, edgeTolerance);
    }

    /// <summary>
    /// Create and add a new edge to this list
    /// </summary>
    /// <param name="startVertex"></param>
    /// <param name="endVertex"></param>
    /// <param name="curve3dIndex"></param>
    /// <param name="edgeTolerance"></param>
    /// <returns></returns>
    public BrepEdge Add(BrepVertex startVertex, BrepVertex endVertex, int curve3dIndex, double edgeTolerance)
    {
      return Add(startVertex, endVertex, curve3dIndex, Interval.Unset, edgeTolerance);
    }

    /// <summary>
    /// Create and add a new edge to this list
    /// </summary>
    /// <param name="startVertexIndex"></param>
    /// <param name="endVertexIndex"></param>
    /// <param name="curve3dIndex"></param>
    /// <param name="subDomain"></param>
    /// <param name="edgeTolerance"></param>
    /// <returns></returns>
    public BrepEdge Add(int startVertexIndex, int endVertexIndex, int curve3dIndex, Interval subDomain, double edgeTolerance)
    {
      IntPtr ptr_brep = m_brep.NonConstPointer();
      int index = UnsafeNativeMethods.ON_Brep_NewEdge2(ptr_brep, startVertexIndex, endVertexIndex, curve3dIndex, subDomain, edgeTolerance);
      if (index < 0)
        return null;
      return this[index]; // this way the BrepEdge is properly in the list
    }

    /// <summary>
    /// Create and add a new edge to this list
    /// </summary>
    /// <param name="startVertexIndex"></param>
    /// <param name="endVertexIndex"></param>
    /// <param name="curve3dIndex"></param>
    /// <param name="edgeTolerance"></param>
    /// <returns></returns>
    public BrepEdge Add(int startVertexIndex, int endVertexIndex, int curve3dIndex, double edgeTolerance)
    {
      return Add(startVertexIndex, endVertexIndex, curve3dIndex, Interval.Unset, edgeTolerance);
    }
    #endregion

    #region IEnumerable Implementation

    /// <summary>
    /// Gets the same enumerator as <see cref="GetEnumerator"/>.
    /// </summary>
    /// <returns>The enumerator.</returns>
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
    {
      return GetEnumerator();
    }

    /// <summary>
    /// Gets an enumerator that visits all edges.
    /// </summary>
    /// <returns>The enumerator.</returns>
    public IEnumerator<BrepEdge> GetEnumerator()
    {
      return new Rhino.Collections.TableEnumerator<BrepEdgeList, BrepEdge>(this);
    }
    #endregion
  }

  /// <summary>
  /// Provides access to all the Trims in a Brep object
  /// </summary>
  public class BrepTrimList : IEnumerable<BrepTrim>, Rhino.Collections.IRhinoTable<BrepTrim>
  {
    readonly Brep m_brep;
    readonly BrepLoop m_breploop;
    #region constructors
    internal BrepTrimList(Brep ownerBrep)
    {
      m_brep = ownerBrep;
      m_breploop = null;
    }
    internal BrepTrimList(BrepLoop ownerLoop)
    {
      m_brep = ownerLoop.Brep;
      m_breploop = ownerLoop;
    }
    #endregion

    #region properties
    /// <summary>
    /// Gets the number of brep trims.
    /// </summary>
    public int Count
    {
      get
      {
        if (m_breploop != null)
        {
          IntPtr const_ptr_loop = m_breploop.ConstPointer();
          return UnsafeNativeMethods.ON_BrepLoop_TrimCount(const_ptr_loop);
        }
        IntPtr ptr_const_brep = m_brep.ConstPointer();
        return UnsafeNativeMethods.ON_Brep_GetInt(ptr_const_brep, UnsafeNativeMethods.BrepInt.TrimCount);
      }
    }

    /// <summary>
    /// Gets the BrepTrim at the given index. 
    /// The index must be valid or an IndexOutOfRangeException will be thrown.
    /// </summary>
    /// <param name="index">Index of BrepTrim to access.</param>
    /// <exception cref="IndexOutOfRangeException">Thrown when the index is invalid.</exception>
    /// <returns>The BrepTrim at [index].</returns>
    public BrepTrim this[int index]
    {
      get
      {
        int count = Count;
        if (index < 0 || index >= count)
        {
          throw new IndexOutOfRangeException();
        }
        if (m_trims == null)
          m_trims = new List<BrepTrim>(count);
        int existing_list_count = m_trims.Count;

        IntPtr const_ptr_loop = IntPtr.Zero;
        if (m_breploop != null)
          const_ptr_loop = m_breploop.ConstPointer();

        for (int i = existing_list_count; i < count; i++)
        {
          int trim_index = i;
          if (const_ptr_loop != IntPtr.Zero)
            trim_index = UnsafeNativeMethods.ON_BrepLoop_TrimIndex(const_ptr_loop, i);
          m_trims.Add(new BrepTrim(trim_index, m_brep));
        }

        return m_trims[index];
      }
    }
    List<BrepTrim> m_trims; // = null; initialized to null by runtime
    #endregion

    #region methods
    /// <summary>
    /// Add a new trim that will be part of an inner, outer, or slit loop
    /// to the brep.
    /// </summary>
    /// <param name="curve2dIndex">index of 2d trimming curve</param>
    /// <returns>New Trim</returns>
    /// <remarks>
    /// You should set the trim's tolerance, type, iso, li, and m_ei values.
    /// In general, you should try to use the
    /// Add( edge, bRev3d, loop, c2i ) version of NewTrim.
    /// If you want to add a singular trim, use AddSingularTrim.
    /// If you want to add a crvonsrf trim, use AddCurveOnFace.
    /// If you want to add a ptonsrf trim, use AddPointOnFace.
    /// </remarks>
    public BrepTrim Add(int curve2dIndex)
    {
      IntPtr ptr_brep = m_brep.NonConstPointer();
      int index = UnsafeNativeMethods.ON_Brep_NewTrim(ptr_brep, curve2dIndex);
      return index < 0 ? null : this[index]; // this way the BrepTrim is properly in the list
    }

    /// <summary>
    /// Add a new trim that will be part of an inner, outer, or slit loop
    /// to the brep
    /// </summary>
    /// <param name="rev3d">
    /// true if the edge and trim have opposite directions
    /// </param>
    /// <param name="loop">trim is appended to this loop</param>
    /// <param name="curve2dIndex">index of 2d trimming curve</param>
    /// <returns>new trim</returns>
    public BrepTrim Add(bool rev3d, BrepLoop loop, int curve2dIndex)
    {
      IntPtr ptr_brep = m_brep.NonConstPointer();
      int index = UnsafeNativeMethods.ON_Brep_NewTrim2(ptr_brep, rev3d, loop.LoopIndex, curve2dIndex);
      return index < 0 ? null : this[index]; // this way the BrepTrim is properly in the list 
    }

    /// <summary>
    /// Add a new trim that will be part of an inner, outer, or slit loop
    /// to the brep
    /// </summary>
    /// <param name="rev3d">
    /// true if the edge and trim have opposite directions
    /// </param>
    /// <param name="edge">3d edge associated with this trim</param>
    /// <param name="curve2dIndex">index of 2d trimming curve</param>
    /// <returns>new trim</returns>
    public BrepTrim Add(bool rev3d, BrepEdge edge, int curve2dIndex)
    {
      IntPtr ptr_brep = m_brep.NonConstPointer();
      int index = UnsafeNativeMethods.ON_Brep_NewTrim3(ptr_brep, rev3d, edge.EdgeIndex, curve2dIndex);
      return index < 0 ? null : this[index]; // this way the BrepTrim is properly in the list 
    }

    /// <summary>
    /// Add a new trim that will be part of an inner, outer, or slit loop
    /// to the brep.
    /// </summary>
    /// <param name="edge">3d edge associated with this trim</param>
    /// <param name="rev3d">
    /// true if the edge and trim have opposite directions
    /// </param>
    /// <param name="loop">trim is appended to this loop</param>
    /// <param name="curve2dIndex">index of 2d trimming curve</param>
    /// <returns>new trim</returns>
    public BrepTrim Add(BrepEdge edge, bool rev3d, BrepLoop loop, int curve2dIndex)
    {
      IntPtr ptr_brep = m_brep.NonConstPointer();
      int index = UnsafeNativeMethods.ON_Brep_NewTrim4(ptr_brep, edge.EdgeIndex, rev3d, loop.LoopIndex, curve2dIndex);
      return index < 0 ? null : this[index]; // this way the BrepTrim is properly in the list 
    }

    /// <summary> Add a new singular trim to the brep. </summary>
    /// <param name="vertex">vertex along collapsed surface edge</param>
    /// <param name="loop">trim is appended to this loop</param>
    /// <param name="iso"></param>
    /// <param name="curve2dIndex">index of 2d trimming curve</param>
    /// <returns>new trim</returns>
    public BrepTrim AddSingularTrim(BrepVertex vertex, BrepLoop loop, IsoStatus iso, int curve2dIndex)
    {
      IntPtr ptr_brep = m_brep.NonConstPointer();
      int index = UnsafeNativeMethods.ON_Brep_NewSingularTrim(ptr_brep, vertex.VertexIndex, loop.LoopIndex, (int)iso, curve2dIndex);
      return index < 0 ? null : this[index]; // this way the BrepTrim is properly in the list 
    }

    /// <summary>Add a new curve on face to the brep</summary>
    /// <param name="face">face that curve lies on</param>
    /// <param name="edge">3d edge associated with this curve on surface</param>
    /// <param name="rev3d">
    /// true if the 3d edge and the 2d parameter space curve have opposite directions.
    /// </param>
    /// <param name="curve2dIndex">index of 2d curve in face's parameter space</param>
    /// <returns>new trim that represents the curve on surface</returns>
    /// <remarks>
    /// You should set the trim's tolerance and iso values.
    /// </remarks>
    public BrepTrim AddCurveOnFace(BrepFace face, BrepEdge edge, bool rev3d, int curve2dIndex)
    {
      IntPtr ptr_brep = m_brep.NonConstPointer();
      int index = UnsafeNativeMethods.ON_Brep_NewCurveOnFace(ptr_brep, face.FaceIndex, edge.EdgeIndex, rev3d, curve2dIndex);
      return index < 0 ? null : this[index]; // this way the BrepTrim is properly in the list 
    }
    #endregion

    #region IEnumerable Implementation

    /// <summary>
    /// Gets the same enumerator as <see cref="GetEnumerator"/>.
    /// </summary>
    /// <returns>The enumerator.</returns>
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
    {
      return GetEnumerator();
    }

    /// <summary>
    /// Gets an enumerator that visits all edges.
    /// </summary>
    /// <returns>The enumerator.</returns>
    public IEnumerator<BrepTrim> GetEnumerator()
    {
      return new Rhino.Collections.TableEnumerator<BrepTrimList, BrepTrim>(this);
    }
    #endregion
  }

  /// <summary>
  /// Provides access to all the Loops in a Brep object.
  /// </summary>
  public class BrepLoopList : IEnumerable<BrepLoop>, Rhino.Collections.IRhinoTable<BrepLoop>
  {
    readonly Brep m_brep;
    readonly BrepFace m_brepface;

    #region constructors
    internal BrepLoopList(Brep ownerBrep)
    {
      m_brep = ownerBrep;
      m_brepface = null;
    }
    internal BrepLoopList(BrepFace ownerFace)
    {
      m_brep = ownerFace.m_brep;
      m_brepface = ownerFace;
    }
    #endregion

    #region properties
    /// <summary>
    /// Gets the number of brep loops.
    /// </summary>
    public int Count
    {
      get
      {
        if (m_brepface != null)
        {
          IntPtr const_ptr_face = m_brepface.ConstPointer();
          return UnsafeNativeMethods.ON_BrepFace_LoopCount(const_ptr_face);
        }
        IntPtr ptr_const_brep = m_brep.ConstPointer();
        return UnsafeNativeMethods.ON_Brep_GetInt(ptr_const_brep, UnsafeNativeMethods.BrepInt.LoopCount);
      }
    }

    /// <summary>
    /// Gets the BrepLoop at the given index. 
    /// The index must be valid or an IndexOutOfRangeException will be thrown.
    /// </summary>
    /// <param name="index">Index of BrepLoop to access.</param>
    /// <exception cref="IndexOutOfRangeException">Thrown when the index is invalid.</exception>
    /// <returns>The BrepLoop at [index].</returns>
    public BrepLoop this[int index]
    {
      get
      {
        int count = Count;
        if (index < 0 || index >= count)
        {
          throw new IndexOutOfRangeException();
        }
        if (m_loops == null)
          m_loops = new List<BrepLoop>(count);
        int existing_list_count = m_loops.Count;

        IntPtr const_ptr_face = IntPtr.Zero;
        if (m_brepface != null)
          const_ptr_face = m_brepface.ConstPointer();

        for (int i = existing_list_count; i < count; i++)
        {
          int loop_index = i;
          if (const_ptr_face != IntPtr.Zero)
            loop_index = UnsafeNativeMethods.ON_BrepFace_LoopIndex(const_ptr_face, i);
          m_loops.Add(new BrepLoop(loop_index, m_brep));
        }

        return m_loops[index];
      }
    }
    List<BrepLoop> m_loops; // = null; initialized to null by runtime
    #endregion

    #region methods
    /// <summary>
    /// Create a new empty boundary loop. The new loop will not be part of a face and
    /// will not include any trim curves.
    /// </summary>
    /// <param name="loopType"></param>
    /// <returns></returns>
    public BrepLoop Add(BrepLoopType loopType)
    {
      IntPtr ptr_brep = m_brep.NonConstPointer();
      int index = UnsafeNativeMethods.ON_Brep_NewLoop(ptr_brep, (int)loopType, -1);
      return index < 0 ? null : this[index];
    }

    /// <summary>
    /// Create a new boundary loop on a face.  After you get this
    /// BrepLoop, you still need to create the vertices, edges, 
    /// and trims that define the loop.
    /// </summary>
    /// <param name="loopType"></param>
    /// <param name="face"></param>
    /// <returns>New loop that needs to be filled in</returns>
    public BrepLoop Add(BrepLoopType loopType, BrepFace face)
    {
      IntPtr ptr_brep = m_brep.NonConstPointer();
      int faceindex = face.FaceIndex;
      int index = UnsafeNativeMethods.ON_Brep_NewLoop(ptr_brep, (int)loopType, faceindex);
      return index < 0 ? null : this[index];
    }

    /// <summary>
    /// Create a new outer boundary loop that runs along the sides
    /// of the face's surface.  All the necessary trims, edges,
    /// and vertices are created and added to the brep.
    /// </summary>
    /// <param name="faceIndex">
    /// index of face that needs an outer boundary
    /// that runs along the sides of its surface.
    /// </param>
    /// <returns>New outer boundary loop that is complete.</returns>
    public BrepLoop AddOuterLoop(int faceIndex)
    {
      IntPtr ptr_brep = m_brep.NonConstPointer();
      int index = UnsafeNativeMethods.ON_Brep_NewOuterLoop(ptr_brep, faceIndex);
      return index < 0 ? null : this[index];
    }

    /// <summary>
    /// Add a planar trimming loop to a planar face
    /// </summary>
    /// <param name="faceIndex">
    /// index of planar face.  The underlying suface must be a PlaneSurface
    /// </param>
    /// <param name="loopType">
    /// type of loop to add.  If loopType is Unknown, then the loop direction
    /// is tested and the the new loops type will be set to Outer or Inner.
    /// If the loopType is Outer, then the direction of the new loop is tested
    /// and flipped if it is clockwise. If the loopType is Inner, then the
    /// direction of the new loop is tested and flipped if it is counter-clockwise.
    /// </param>
    /// <param name="boundaryCurves">
    /// list of 3d curves that form a simple (no self intersections) closed
    /// curve.  These curves define the 3d edge geometry and should be near
    /// the planar surface.
    /// </param>
    /// <returns>new loop if successful</returns>
    public BrepLoop AddPlanarFaceLoop(int faceIndex, BrepLoopType loopType, IEnumerable<Curve> boundaryCurves)
    {
      IntPtr ptr_brep = m_brep.NonConstPointer();
      using (var crvs = new SimpleArrayCurvePointer(boundaryCurves))
      {
        IntPtr const_ptr_curves = crvs.ConstPointer();
        int index = UnsafeNativeMethods.ON_Brep_NewPlanarFaceLoop(ptr_brep, faceIndex, (int)loopType, const_ptr_curves);
        GC.KeepAlive(boundaryCurves);
        return index < 0 ? null : this[index];
      }
    }
    #endregion

    #region IEnumerable Implementation

    /// <summary>
    /// Gets the same enumerator as <see cref="GetEnumerator"/>.
    /// </summary>
    /// <returns>The enumerator.</returns>
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
    {
      return GetEnumerator();
    }

    /// <summary>
    /// Gets an enumerator that visits all edges.
    /// </summary>
    /// <returns>The enumerator.</returns>
    public IEnumerator<BrepLoop> GetEnumerator()
    {
      return new Rhino.Collections.TableEnumerator<BrepLoopList, BrepLoop>(this);
    }
    #endregion
  }
}
