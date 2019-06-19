using System;
using System.Collections.Generic;
using Rhino.DocObjects;

namespace Rhino.Geometry
{
#if RHINO_SDK
  /// <summary>
  /// Enumerates the different types of silhouettes and their origins.
  /// </summary>
  [Flags]
  public enum SilhouetteType
  {
    /// <summary>
    /// None.
    /// </summary>
    None = UnsafeNativeMethods.SilEventType.None,
    /// <summary>
    /// Boundary of a region that projects to a curve.  
    /// The view direction is tangent to the surface over the entire region.
    /// </summary>
    Projecting = UnsafeNativeMethods.SilEventType.Projecting,
    /// <summary>
    /// Tangent silhouette curve where the curve projects to a point (within tolerance).
    /// In this case side_fill[i] is meaningless so it's left unset.
    /// </summary>
    TangentProjects = UnsafeNativeMethods.SilEventType.TanProjects,
    /// <summary>
    /// Tangent silhouette curve. The view direction is tangent to the surface along the curve.
    /// </summary>
    Tangent = UnsafeNativeMethods.SilEventType.Tangent,
    /// <summary>
    /// Crease on geometry that is a silhouette.
    /// </summary>
    Crease = UnsafeNativeMethods.SilEventType.Crease,
    /// <summary>
    /// Boundary of geometry is always a silhouette.
    /// </summary>
    Boundary = UnsafeNativeMethods.SilEventType.Boundary,
    /// <summary>
    /// A non-silhouette crease, meaning both sides are visible.
    /// </summary>
    NonSilhouetteCrease = UnsafeNativeMethods.SilEventType.NonSilCrease,
    /// <summary>
    /// A non-silhouette tangent edge that is not a crease.
    /// </summary>
    NonSilhouetteTangent = UnsafeNativeMethods.SilEventType.NonSilTangent,
    /// <summary>
    /// A non-silhouette surface seam.
    /// </summary>
    NonSilhouetteSeam = UnsafeNativeMethods.SilEventType.NonSilSeam,
    /// <summary>
    /// Intersection with a clipping plane.
    /// </summary>
    SectionCut = UnsafeNativeMethods.SilEventType.SectionCut,
    /// <summary>
    /// Miscellaneous curve feature.
    /// </summary>
    MiscellaneousFeature = UnsafeNativeMethods.SilEventType.MiscFeature,
    /// <summary>
    /// Draft curve is a curve of constant draft angle.
    /// </summary>
    DraftCurve = UnsafeNativeMethods.SilEventType.DraftCurve,
  }

  /// <summary>
  /// Information about silhouette curves that are generated from
  /// geometry (surfaces, brep faces, meshes)
  /// </summary>
  public class Silhouette
  {
    #region constructor
    private Silhouette(IntPtr ptrSilEvent)
    {
      IntPtr ptr_curve = UnsafeNativeMethods.TLC_SilEvent_ExtractCurve(ptrSilEvent);
      Curve = GeometryBase.CreateGeometryHelper(ptr_curve, null) as Curve;
      ComponentIndex ci = ComponentIndex.Unset;
      var et = UnsafeNativeMethods.TLC_SilEvent_Extract(ptrSilEvent, ref ci);
      switch (et)
      {
        case UnsafeNativeMethods.SilEventType.None:
          SilhouetteType = SilhouetteType.None;
          break;
        case UnsafeNativeMethods.SilEventType.Tangent:
          SilhouetteType = SilhouetteType.Tangent;
          break;
        case UnsafeNativeMethods.SilEventType.Projecting:
          SilhouetteType = SilhouetteType.Projecting;
          break;
        case UnsafeNativeMethods.SilEventType.TanProjects:
          SilhouetteType = SilhouetteType.TangentProjects;
          break;
        case UnsafeNativeMethods.SilEventType.Boundary:
          SilhouetteType = SilhouetteType.Boundary;
          break;
        case UnsafeNativeMethods.SilEventType.Crease:
          SilhouetteType = SilhouetteType.Crease;
          break;
        case UnsafeNativeMethods.SilEventType.DraftCurve:
          SilhouetteType = SilhouetteType.DraftCurve;
          break;
      }
      GeometryComponentIndex = ci;
    }
    #endregion

    #region static methods
    /// <summary>
    /// Compute silhouettes of a shape for a perspective projection.
    /// </summary>
    /// <param name="geometry">Geometry whose silhouettes need to be computed. Can be Brep, BrepFace, Mesh, or Extrusion.</param>
    /// <param name="silhouetteType">Types of silhouette to compute.</param>
    /// <param name="perspectiveCameraLocation">Location of perspective camera.</param>
    /// <param name="tolerance">Tolerance to use for determining projecting relationships. 
    /// Surfaces and curves that are closer than tolerance, may be treated as projecting. 
    /// When in doubt use RhinoDoc.ModelAbsoluteTolerance.</param>
    /// <param name="angleToleranceRadians">Angular tolerance to use for determining projecting relationships.
    /// A surface normal N that satisfies N o cameraDirection &lt; Sin(angleToleranceRadians) may be considered projecting. 
    /// When in doubt use RhinoDoc.ModelAngleToleranceRadians.</param>
    /// <returns>Array of silhouette curves.</returns>
    public static Silhouette[] Compute(
      GeometryBase geometry,
      SilhouetteType silhouetteType,
      Point3d perspectiveCameraLocation,
      double tolerance,
      double angleToleranceRadians)
    {
      return Compute(geometry, silhouetteType, perspectiveCameraLocation, tolerance, angleToleranceRadians, null, System.Threading.CancellationToken.None);
    }

    /// <summary>
    /// Compute silhouettes of a shape for a perspective projection.
    /// </summary>
    /// <param name="geometry">Geometry whose silhouettes need to be computed. Can be Brep, BrepFace, Mesh, or Extrusion.</param>
    /// <param name="silhouetteType">Types of silhouette to compute.</param>
    /// <param name="perspectiveCameraLocation">Location of perspective camera.</param>
    /// <param name="tolerance">Tolerance to use for determining projecting relationships. 
    /// Surfaces and curves that are closer than tolerance, may be treated as projecting. 
    /// When in doubt use RhinoDoc.ModelAbsoluteTolerance.</param>
    /// <param name="angleToleranceRadians">Angular tolerance to use for determining projecting relationships.
    /// A surface normal N that satisfies N o cameraDirection &lt; Sin(angleToleranceRadians) may be considered projecting. 
    /// When in doubt use RhinoDoc.ModelAngleToleranceRadians.</param>
    /// <param name="clippingPlanes">Optional collection of clipping planes.</param>
    /// <param name="cancelToken">Computation cancellation token.</param>
    /// <returns>Array of silhouette curves.</returns>
    public static Silhouette[] Compute(
      GeometryBase geometry,
      SilhouetteType silhouetteType,
      Point3d perspectiveCameraLocation,
      double tolerance,
      double angleToleranceRadians,
      IEnumerable<Plane> clippingPlanes,
      System.Threading.CancellationToken cancelToken)
    {
      IntPtr const_ptr_geometry = geometry.ConstPointer();
      Plane[] planes = null;
      int plane_count = 0;
      if (clippingPlanes != null)
      {
        List<Plane> p = new List<Plane>(clippingPlanes);
        plane_count = p.Count;
        planes = p.ToArray();
      }

      ThreadTerminator terminator = null;
      IntPtr ptr_terminator = IntPtr.Zero;
      if (cancelToken != System.Threading.CancellationToken.None)
      {
        terminator = new ThreadTerminator();
        ptr_terminator = terminator.NonConstPointer();
        cancelToken.Register(terminator.RequestCancel);
      }

      UnsafeNativeMethods.SilEventType s = (UnsafeNativeMethods.SilEventType)silhouetteType;
      IntPtr ptr_silhouettes = UnsafeNativeMethods.TLC_Sillhouette2(const_ptr_geometry, s, perspectiveCameraLocation, tolerance, angleToleranceRadians, planes, plane_count, ptr_terminator);
      Silhouette[] rc = FromClassArray(ptr_silhouettes);
      UnsafeNativeMethods.TLC_SilhouetteArrayDelete(ptr_silhouettes);
      if (terminator != null)
        terminator.Dispose();
      GC.KeepAlive(geometry);
      return rc;
    }

    /// <summary>
    /// Compute silhouettes of a shape for a parallel projection.
    /// </summary>
    /// <param name="geometry">Geometry whose silhouettes need to be computed. Can be Brep, BrepFace, Mesh, or Extrusion.</param>
    /// <param name="silhouetteType">Types of silhouette to compute.</param>
    /// <param name="parallelCameraDirection">Direction of parallel camera.</param>
    /// <param name="tolerance">Tolerance to use for determining projecting relationships. 
    /// Surfaces and curves that are closer than tolerance, may be treated as projecting. 
    /// When in doubt use RhinoDoc.ModelAbsoluteTolerance.</param>
    /// <param name="angleToleranceRadians">Angular tolerance to use for determining projecting relationships.
    /// A surface normal N that satisfies N o cameraDirection &lt; Sin(angleToleranceRadians) may be considered projecting. 
    /// When in doubt use RhinoDoc.ModelAngleToleranceRadians.</param>
    /// <returns>Array of silhouette curves.</returns>
    public static Silhouette[] Compute(
      GeometryBase geometry,
      SilhouetteType silhouetteType,
      Vector3d parallelCameraDirection,
      double tolerance,
      double angleToleranceRadians)
    {
      return Compute(geometry, silhouetteType, parallelCameraDirection, tolerance, angleToleranceRadians, null, System.Threading.CancellationToken.None);
    }

    /// <summary>
    /// Compute silhouettes of a shape for a parallel projection.
    /// </summary>
    /// <param name="geometry">Geometry whose silhouettes need to be computed. Can be Brep, BrepFace, Mesh, or Extrusion.</param>
    /// <param name="silhouetteType">Types of silhouette to compute.</param>
    /// <param name="parallelCameraDirection">Direction of parallel camera.</param>
    /// <param name="tolerance">Tolerance to use for determining projecting relationships. 
    /// Surfaces and curves that are closer than tolerance, may be treated as projecting. 
    /// When in doubt use RhinoDoc.ModelAbsoluteTolerance.</param>
    /// <param name="angleToleranceRadians">Angular tolerance to use for determining projecting relationships.
    /// A surface normal N that satisfies N o cameraDirection &lt; Sin(angleToleranceRadians) may be considered projecting. 
    /// When in doubt use RhinoDoc.ModelAngleToleranceRadians.</param>
    /// <param name="clippingPlanes">Optional collection of clipping planes.</param>
    /// <param name="cancelToken">Computation cancellation token.</param>
    /// <returns>Array of silhouette curves.</returns>
    public static Silhouette[] Compute(
      GeometryBase geometry,
      SilhouetteType silhouetteType,
      Vector3d parallelCameraDirection,
      double tolerance,
      double angleToleranceRadians,
      IEnumerable<Plane> clippingPlanes,
      System.Threading.CancellationToken cancelToken)
    {
      IntPtr const_ptr_geometry = geometry.ConstPointer();
      Plane[] planes = null;
      int plane_count = 0;
      if (clippingPlanes != null)
      {
        List<Plane> p = new List<Plane>(clippingPlanes);
        plane_count = p.Count;
        planes = p.ToArray();
      }

      ThreadTerminator terminator = null;
      if (cancelToken != System.Threading.CancellationToken.None)
      {
        terminator = new ThreadTerminator();
        cancelToken.Register(terminator.RequestCancel);
      }
      IntPtr ptr_terminator = terminator == null ? IntPtr.Zero : terminator.NonConstPointer();

      var s = (UnsafeNativeMethods.SilEventType)silhouetteType;
      IntPtr ptr_silhouettes = UnsafeNativeMethods.TLC_Sillhouette(const_ptr_geometry, s, parallelCameraDirection, tolerance, angleToleranceRadians, planes, plane_count, ptr_terminator);
      Silhouette[] rc = FromClassArray(ptr_silhouettes);
      UnsafeNativeMethods.TLC_SilhouetteArrayDelete(ptr_silhouettes);
      if (terminator != null)
        terminator.Dispose();
      GC.KeepAlive(geometry);
      return rc;
    }

    /// <summary>
    /// Compute silhouettes of a shape for a specified projection.
    /// </summary>
    /// <param name="geometry">Geometry whose silhouettes need to be computed. Can be Brep, BrepFace, Mesh, or Extrusion.</param>
    /// <param name="silhouetteType">Types of silhouette to compute.</param>
    /// <param name="viewport">Projection.</param>
    /// <param name="tolerance">Tolerance to use for determining projecting relationships. 
    /// Surfaces and curves that are closer than tolerance, may be treated as projecting. 
    /// When in doubt use RhinoDoc.ModelAbsoluteTolerance.</param>
    /// <param name="angleToleranceRadians">Angular tolerance to use for determining projecting relationships.
    /// A surface normal N that satisfies N o cameraDirection &lt; Sin(angleToleranceRadians) may be considered projecting. 
    /// When in doubt use RhinoDoc.ModelAngleToleranceRadians.</param>
    /// <returns>Array of silhouette curves.</returns>
    public static Silhouette[] Compute(
      GeometryBase geometry,
      SilhouetteType silhouetteType,
      ViewportInfo viewport,
      double tolerance,
      double angleToleranceRadians)
    {
      return Compute(geometry, silhouetteType, viewport, tolerance, angleToleranceRadians, null, System.Threading.CancellationToken.None);
    }

    /// <summary>
    /// Compute silhouettes of a shape for a specified projection.
    /// </summary>
    /// <param name="geometry">Geometry whose silhouettes need to be computed. Can be Brep, BrepFace, Mesh, or Extrusion.</param>
    /// <param name="silhouetteType">Types of silhouette to compute.</param>
    /// <param name="viewport">Projection.</param>
    /// <param name="tolerance">Tolerance to use for determining projecting relationships. 
    /// Surfaces and curves that are closer than tolerance, may be treated as projecting. 
    /// When in doubt use RhinoDoc.ModelAbsoluteTolerance.</param>
    /// <param name="angleToleranceRadians">Angular tolerance to use for determining projecting relationships.
    /// A surface normal N that satisfies N o cameraDirection &lt; Sin(angleToleranceRadians) may be considered projecting. 
    /// When in doubt use RhinoDoc.ModelAngleToleranceRadians.</param>
    /// <param name="clippingPlanes">Optional collection of clipping planes.</param>
    /// <param name="cancelToken">Computation cancellation token.</param>
    /// <returns>Array of silhouette curves.</returns>
    public static Silhouette[] Compute(
      GeometryBase geometry,
      SilhouetteType silhouetteType,
      ViewportInfo viewport,
      double tolerance,
      double angleToleranceRadians,
      IEnumerable<Plane> clippingPlanes,
      System.Threading.CancellationToken cancelToken)
    {
      if (viewport.IsParallelProjection)
        return Compute(geometry, silhouetteType, viewport.CameraDirection, tolerance, angleToleranceRadians, clippingPlanes, cancelToken);
      return Compute(geometry, silhouetteType, viewport.CameraLocation, tolerance, angleToleranceRadians, clippingPlanes, cancelToken);
    }

    /// <summary>
    /// Create a silhouette array from an unmanaged class array pointer.
    /// </summary>
    /// <param name="ptrSilouettes">Class array pointer.</param>
    /// <returns>Array of silhouettes.</returns>
    static Silhouette[] FromClassArray(IntPtr ptrSilouettes)
    {
      int count = UnsafeNativeMethods.TLC_SilhouetteArrayCount(ptrSilouettes);
      Silhouette[] rc = new Silhouette[count];
      for (int i = 0; i < rc.Length; i++)
      {
        IntPtr ptr_sil_event = UnsafeNativeMethods.TLC_SilhouetteArrayGet(ptrSilouettes, i);
        rc[i] = new Silhouette(ptr_sil_event);
      }
      return rc;
    }
    #endregion

    #region properties
    /// <summary>
    /// Gets the type of this silhouette curve.
    /// </summary>
    public SilhouetteType SilhouetteType
    {
      get;
      private set;
    }

    /// <summary>
    /// Gets the component index corresponding with this silhouette curve.
    /// This field is only set when the entire silhouette curve is part of some geometry component.
    /// </summary>
    public ComponentIndex GeometryComponentIndex
    {
      get;
      private set;
    }

    /// <summary> 
    /// 3D curve representing the shape of the silhouette.
    /// </summary>
    public Curve Curve
    {
      get;
      private set;
    }
    #endregion
  }
#endif
}