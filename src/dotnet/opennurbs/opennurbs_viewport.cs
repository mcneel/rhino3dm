using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Rhino.Geometry;

namespace Rhino.DocObjects
{
  /// <summary>
  /// Represents a viewing frustum.
  /// </summary>
  [Serializable]
  public sealed class ViewportInfo : Runtime.CommonObject
  {
    readonly object m_parent;

    internal override IntPtr _InternalGetConstPointer()
    {
      var vi = m_parent as ViewInfo;
      if (vi != null)
      {
        return vi.ConstViewportPointer();
      }
      return IntPtr.Zero;
    }

    internal override IntPtr _InternalDuplicate(out bool applymempressure)
    {
      applymempressure = false;
      IntPtr const_ptr_this = ConstPointer();
      return UnsafeNativeMethods.ON_Viewport_New(const_ptr_this);
    }

    /// <summary>
    /// Initializes a new instance.
    /// </summary>
    public ViewportInfo()
    {
      IntPtr ptr = UnsafeNativeMethods.ON_Viewport_New(IntPtr.Zero);
      ConstructNonConstObject(ptr);
    }

    /// <summary>
    ///  Initializes a new instance by copying values from another instance.
    /// </summary>
    /// <param name="other">The other viewport info.</param>
    public ViewportInfo(ViewportInfo other)
    {
      IntPtr const_ptr_other_vp = other.ConstPointer();
      IntPtr ptr = UnsafeNativeMethods.ON_Viewport_New(const_ptr_other_vp);
      ConstructNonConstObject(ptr);
    }

#if RHINO_SDK
    /// <summary>
    /// Copies all of the ViewportInfo data from an existing RhinoViewport.
    /// </summary>
    /// <param name="rhinoViewport">A viewport to copy.</param>
    public ViewportInfo(Display.RhinoViewport rhinoViewport)
    {
      IntPtr const_ptr_other_vp = rhinoViewport.ConstPointer();
      IntPtr ptr = UnsafeNativeMethods.ON_Viewport_New2(const_ptr_other_vp);
      ConstructNonConstObject(ptr);
    }

    /// <summary>
    /// Calculates the camera rotation angle.
    /// </summary>
    /// <param name="direction">The camera direction.</param>
    /// <param name="up">The camera up direction.</param>
    /// <returns>The camera rotation angle in radians.</returns>
    public static double CalculateCameraRotationAngle(Vector3d direction, Vector3d up)
    {
      return UnsafeNativeMethods.RHC_RhUpVectorToAngleRadians(direction, up);
    }

    /// <summary>
    /// Calculates the camera up direction.
    /// </summary>
    /// <param name="location">The camera location.</param>
    /// <param name="direction">The camera direction.</param>
    /// <param name="angle">The camera rotation angle in radians.</param>
    /// <returns>The camera up direction.</returns>
    public static Vector3d CalculateCameraUpDirection(Point3d location, Vector3d direction, double angle)
    {
      Vector3d rc = new Vector3d();
      UnsafeNativeMethods.RHC_RhAngleRadiansToUpVector(location, direction, angle, ref rc);
      return rc;
    }

#endif

    internal ViewportInfo(IntPtr ptrOnViewport)
    {
      IntPtr ptr = UnsafeNativeMethods.ON_Viewport_New(ptrOnViewport);
      ConstructNonConstObject(ptr);
    }

    internal ViewportInfo(ViewInfo parent)
    {
      m_parent = parent;
      ConstructConstObject(parent, -1);
    }

    private ViewportInfo(SerializationInfo info, StreamingContext context)
      : base(info, context)
    {
    }

    const int idxIsValidCamera = 0;
    const int idxIsValidFrustum = 1;
    //const int idxIsValid = 2;
    const int idxIsPerspectiveProjection = 3;
    const int idxIsParallelProjection = 4;
    const int idxIsCameraLocationLocked = 5;
    const int idxIsCameraDirectionLocked = 6;
    const int idxIsCameraUpLocked = 7;
    const int idxIsFrustumLeftRightSymmetric = 8;
    const int idxIsFrustumTopBottomSymmetric = 9;

    bool GetBool(int which)
    {
      IntPtr const_ptr_this = ConstPointer();
      return UnsafeNativeMethods.ON_Viewport_GetBool(const_ptr_this, which);
    }
    
    /// <summary>
    /// Gets a value that indicates whether the camera is valid.
    /// </summary>
    public bool IsValidCamera
    {
      get { return GetBool(idxIsValidCamera); }
    }

    /// <summary>
    /// Gets a value that indicates whether the frustum is valid.
    /// </summary>
    public bool IsValidFrustum
    {
      get { return GetBool(idxIsValidFrustum); }
    }

    /// <summary>
    /// Get or set whether this projection is perspective.
    /// </summary>
    public bool IsPerspectiveProjection
    {
      get { return GetBool(idxIsPerspectiveProjection); }
      set
      {
        IntPtr ptr_this = NonConstPointer();
        UnsafeNativeMethods.ON_Viewport_SetProjection(ptr_this, !value);
      }
    }

    /// <summary>
    /// Get or set whether this projection is parallel.
    /// </summary>
    public bool IsParallelProjection
    {
      get { return GetBool(idxIsParallelProjection); }
      set
      {
        IntPtr ptr_this = NonConstPointer();
        UnsafeNativeMethods.ON_Viewport_SetProjection(ptr_this, value);
      }
    }

    /// <summary>
    /// Gets a value that indicates whether this projection is a two-point perspective.
    /// </summary>
    public bool IsTwoPointPerspectiveProjection
    {
      get { return IsPerspectiveProjection && IsCameraUpLocked && IsFrustumLeftRightSymmetric && !IsFrustumTopBottomSymmetric; }
    }

    /// <summary>
    /// Use this function to change projections of valid viewports
    /// from parallel to perspective.  It will make common additional
    /// adjustments to the frustum and camera location so the resulting
    /// views are similar.  The camera direction and target point are
    /// not be changed.
    /// If the current projection is parallel and symmetricFrustum,
    /// FrustumIsLeftRightSymmetric() and FrustumIsTopBottomSymmetric()
    /// are all equal, then no changes are made and true is returned.
    /// </summary>
    /// <param name="symmetricFrustum">true if you want the resulting frustum to be symmetric.</param>
    /// <returns>true if the operation succeeded; otherwise, false.</returns>
    public bool ChangeToParallelProjection(bool symmetricFrustum)
    {
      IntPtr ptr_this = NonConstPointer();
      return UnsafeNativeMethods.ON_Viewport_ChangeToParallelProjection(ptr_this, symmetricFrustum);
    }

    /// <summary>
    /// Use this function to change projections of valid viewports
    /// from parallel to perspective.  It will make common additional
    /// adjustments to the frustum and camera location so the resulting
    /// views are similar.  The camera direction and target point are
    /// not changed.
    /// If the current projection is perspective and symmetricFrustum,
    /// IsFrustumIsLeftRightSymmetric, and IsFrustumIsTopBottomSymmetric
    /// are all equal, then no changes are made and true is returned.
    /// </summary>
    /// <param name="targetDistance">
    /// If RhinoMath.UnsetValue this parameter is ignored.
    /// Otherwise it must be > 0 and indicates which plane in the current view frustum should be perserved.
    /// </param>
    /// <param name="symmetricFrustum">
    /// true if you want the resulting frustum to be symmetric.
    /// </param>
    /// <param name="lensLength">(pass 50.0 when in doubt)
    /// 35 mm lens length to use when changing from parallel
    /// to perspective projections. If the current projection
    /// is perspective or lens_length is &lt;= 0.0,
    /// then this parameter is ignored.</param>
    /// <returns>true if the operation succeeded; otherwise, false.</returns>
    public bool ChangeToPerspectiveProjection( double targetDistance, bool symmetricFrustum, double lensLength)
    {
      IntPtr ptr_this = NonConstPointer();
      return UnsafeNativeMethods.ON_Viewport_ChangeToPerspectiveProjection(ptr_this, targetDistance, symmetricFrustum, lensLength);
    }

    /// <summary>
    /// Changes projections of valid viewports
    /// to a two point perspective.  It will make common additional
    /// adjustments to the frustum and camera location and direction
    /// so the resulting views are similar.
    /// If the current projection is perspective and
    /// IsFrustumIsLeftRightSymmetric is true and
    /// IsFrustumIsTopBottomSymmetric is false, then no changes are
    /// made and true is returned.
    /// </summary>
    /// <param name="targetDistance">
    /// If RhinoMath.UnsetValue this parameter is ignored.  Otherwise
    /// it must be > 0 and indicates which plane in the current 
    /// view frustum should be perserved.
    /// </param>
    /// <param name="up">
    /// The locked up direction. Pass Vector3d.Zero if you want to use the world
    /// axis direction that is closest to the current up direction.
    /// Pass CameraY() if you want to preserve the current up direction.
    /// </param>
    /// <param name="lensLength">
    /// (pass 50.0 when in doubt)
    /// 35 mm lens length to use when changing from parallel
    /// to perspective projections. If the current projection
    /// is perspective or lens_length is &lt;= 0.0,
    /// then this parameter is ignored.
    /// </param>
    /// <returns>true if the operation succeeded; otherwise, false.</returns>
    public bool ChangeToTwoPointPerspectiveProjection(double targetDistance, Vector3d up, double lensLength)
    {
      IntPtr ptr_this = NonConstPointer();
      return UnsafeNativeMethods.ON_Viewport_ChangeToTwoPointPerspectiveProjection(ptr_this, targetDistance, up, lensLength);
    }

    /// <summary>
    /// Gets the camera location (position) point.
    /// </summary>
    public Point3d CameraLocation
    {
      get
      {
        var loc = new Point3d();
        IntPtr const_ptr_this = ConstPointer();
        UnsafeNativeMethods.ON_Viewport_CameraLocation(const_ptr_this, ref loc);
        return loc;
      }
    }

    /// <summary>
    /// Sets the camera location (position) point.
    /// </summary>
    /// <returns>true if the operation succeeded; otherwise, false.</returns>
    public bool SetCameraLocation(Point3d location)
    {
      IntPtr ptr_this = NonConstPointer();
      return UnsafeNativeMethods.ON_Viewport_SetCameraLocation(ptr_this, location);
    }

    /// <summary>
    /// Gets the direction that the camera faces.
    /// </summary>
    public Vector3d CameraDirection
    {
      get
      {
        var loc = new Vector3d();
        IntPtr const_ptr_this = ConstPointer();
        UnsafeNativeMethods.ON_Viewport_CameraDirection(const_ptr_this, ref loc);
        return loc;
      }
    }

    /// <summary>
    /// Sets the direction that the camera faces.
    /// </summary>
    /// <param name="direction">A new direction.</param>
    /// <returns>true if the direction was set; otherwise false.</returns>
    public bool SetCameraDirection(Vector3d direction)
    {
      IntPtr ptr_this = NonConstPointer();
      return UnsafeNativeMethods.ON_Viewport_SetCameraDirection(ptr_this, direction);
    }

    /// <summary>
    /// Gets the camera up vector.
    /// </summary>
    public Vector3d CameraUp
    {
      get
      {
        var loc = new Vector3d();
        IntPtr const_ptr_this = ConstPointer();
        UnsafeNativeMethods.ON_Viewport_CameraUp(const_ptr_this, ref loc);
        return loc;
      }
    }

    /// <summary>
    /// Sets the camera up vector.
    /// </summary>
    /// <param name="up">A new direction.</param>
    /// <returns>true if the direction was set; otherwise false.</returns>
    public bool SetCameraUp(Vector3d up)
    {
      IntPtr ptr_this = NonConstPointer();
      return UnsafeNativeMethods.ON_Viewport_SetCameraUp(ptr_this, up);
    }

    const int idxCameraLocationLock = 0;
    const int idxCameraDirectionLock = 1;
    const int idxCameraUpLock = 2;
    void SetCameraLock(int which, bool val)
    {
      IntPtr ptr_this = NonConstPointer();
      UnsafeNativeMethods.ON_Viewport_SetLocked(ptr_this, which, val);
    }

    /// <summary>
    /// Gets or sets a value that indicates whether the camera location is unmodifiable.
    /// </summary>
    public bool IsCameraLocationLocked
    {
      get { return GetBool(idxIsCameraLocationLocked); }
      set { SetCameraLock(idxCameraLocationLock, value); }
    }

    /// <summary>
    /// Gets or sets a value that indicates whether the direction that the camera faces is unmodifiable.
    /// </summary>
    public bool IsCameraDirectionLocked
    {
      get { return GetBool(idxIsCameraDirectionLocked); }
      set { SetCameraLock(idxCameraDirectionLock, value); }
    }

    /// <summary>
    /// Gets or sets a value that indicates whether the camera up vector is unmodifiable.
    /// </summary>
    public bool IsCameraUpLocked
    {
      get { return GetBool(idxIsCameraUpLocked); }
      set { SetCameraLock(idxCameraUpLock, value); }
    }

    /// <summary>
    /// Gets or sets a value that indicates whether the camera frustum has a vertical symmetry axis.
    /// </summary>
    public bool IsFrustumLeftRightSymmetric
    {
      get { return GetBool(idxIsFrustumLeftRightSymmetric); }
      set
      {
        IntPtr ptr_this = NonConstPointer();
        UnsafeNativeMethods.ON_Viewport_SetIsFrustumSymmetry(ptr_this, true, value);
      }
    }

    /// <summary>
    /// Gets or sets a value that indicates whether the camera frustum has a horizontal symmetry axis.
    /// </summary>
    public bool IsFrustumTopBottomSymmetric
    {
      get { return GetBool(idxIsFrustumTopBottomSymmetric); }
      set
      {
        IntPtr ptr_this = NonConstPointer();
        UnsafeNativeMethods.ON_Viewport_SetIsFrustumSymmetry(ptr_this, false, value);
      }
    }
    
    /// <summary>
    /// Unlocks the camera vectors and location.
    /// </summary>
    public void UnlockCamera()
    {
      IntPtr ptr_this = NonConstPointer();
      UnsafeNativeMethods.ON_Viewport_Unlock(ptr_this, true);
    }

    /// <summary>
    /// Unlocks frustum horizontal and vertical symmetries.
    /// </summary>
    public void UnlockFrustumSymmetry()
    {
      IntPtr ptr_this = NonConstPointer();
      UnsafeNativeMethods.ON_Viewport_Unlock(ptr_this, false);
    }

    /// <summary>
    /// Gets location and vectors of this camera.
    /// </summary>
    /// <param name="location">An out parameter that will be filled with a point during the call.</param>
    /// <param name="cameraX">An out parameter that will be filled with the X vector during the call.</param>
    /// <param name="cameraY">An out parameter that will be filled with the Y vector during the call.</param>
    /// <param name="cameraZ">An out parameter that will be filled with the Z vector during the call.</param>
    /// <returns>true if current camera orientation is valid; otherwise false.</returns>
    public bool GetCameraFrame(out Point3d location,  out Vector3d cameraX, out Vector3d cameraY, out Vector3d cameraZ)
    {
      location = new Point3d(0, 0, 0);
      cameraX = new Vector3d(0, 0, 0);
      cameraY = new Vector3d(0, 0, 0);
      cameraZ = new Vector3d(0, 0, 0);
      IntPtr const_ptr_this = ConstPointer();
      return UnsafeNativeMethods.ON_Viewport_GetCameraFrame(const_ptr_this, ref location, ref cameraX, ref cameraY, ref cameraZ);
    }

    /// <summary>
    /// Gets the unit "to the right" vector.
    /// </summary>
    public Vector3d CameraX
    {
      get 
      {
        var v = new Vector3d();
        IntPtr const_ptr_this = ConstPointer();
        UnsafeNativeMethods.ON_Viewport_CameraAxis(const_ptr_this, 0, ref v);
        return v;
      }
    }

    /// <summary>
    /// Gets the unit "up" vector.
    /// </summary>
    public Vector3d CameraY
    {
      get
      {
        var v = new Vector3d();
        IntPtr const_ptr_this = ConstPointer();
        UnsafeNativeMethods.ON_Viewport_CameraAxis(const_ptr_this, 1, ref v);
        return v;
      }
    }

    /// <summary>
    /// Gets the unit vector in -CameraDirection.
    /// </summary>
    public Vector3d CameraZ
    {
      get
      {
        var v = new Vector3d();
        IntPtr const_ptr_this = ConstPointer();
        UnsafeNativeMethods.ON_Viewport_CameraAxis(const_ptr_this, 2, ref v);
        return v;
      }
    }

    /// <summary> Default z=up perspective camera direction </summary>
    static public Vector3d DefaultCameraDirection
    {
      get
      {
        var v = new Vector3d();
        UnsafeNativeMethods.ON_Viewport_DefaultCameraDirection(ref v);
        return v;
      }
    }

    /// <summary>
    /// Sets the view frustum. If FrustumSymmetryIsLocked() is true
    /// and left != -right or bottom != -top, then they will be
    /// adjusted so the resulting frustum is symmetric.
    /// </summary>
    /// <param name="left">A new left value.</param>
    /// <param name="right">A new right value.</param>
    /// <param name="bottom">A new bottom value.</param>
    /// <param name="top">A new top value.</param>
    /// <param name="nearDistance">A new near distance value.</param>
    /// <param name="farDistance">A new far distance value.</param>
    /// <returns>true if operation succeeded; otherwise, false.</returns>
    public bool SetFrustum(double left, double right, double bottom, double top, double nearDistance, double farDistance)
    {
      return UnsafeNativeMethods.ON_Viewport_SetFrustum(NonConstPointer(), left, right, bottom, top, nearDistance, farDistance);
    }

    /// <summary>
    /// Gets the view frustum.
    /// </summary>
    /// <param name="left">A left value that will be filled during the call.</param>
    /// <param name="right">A right value that will be filled during the call.</param>
    /// <param name="bottom">A bottom value that will be filled during the call.</param>
    /// <param name="top">A top value that will be filled during the call.</param>
    /// <param name="nearDistance">A near distance value that will be filled during the call.</param>
    /// <param name="farDistance">A far distance value that will be filled during the call.</param>
    /// <returns>true if operation succeeded; otherwise, false.</returns>
    public bool GetFrustum(out double left, out double right, out double bottom, out double top, out double nearDistance, out double farDistance)
    {
      left = 0;
      right = 0;
      bottom = 0;
      top = 0;
      nearDistance = 0;
      farDistance = 0;
      IntPtr const_ptr_this = ConstPointer();
      return UnsafeNativeMethods.ON_Viewport_GetFrustum(const_ptr_this, ref left, ref right, ref bottom, ref top, ref nearDistance, ref farDistance);
    }

    /// <summary>
    /// Setting FrustumAspect changes the larger of the frustum's width/height
    /// so that the resulting value of width/height matches the requested
    /// aspect.  The camera angle is not changed.  If you change the shape
    /// of the view port with a call SetScreenPort(), then you generally 
    /// want to call SetFrustumAspect() with the value returned by 
    /// GetScreenPortAspect().
    /// </summary>
    public double FrustumAspect
    {
      get
      {
        double aspect = 0.0;
        IntPtr const_ptr_this = ConstPointer();
        if (!UnsafeNativeMethods.ON_Viewport_GetFrustrumAspect(const_ptr_this, ref aspect))
          aspect = 0;
        return aspect;
      }
      set
      {
        IntPtr ptr_this = NonConstPointer();
        UnsafeNativeMethods.ON_Viewport_SetFrustumAspect(ptr_this, value);
      }
    }

    /// <summary>
    /// Gets the frustum center point.
    /// </summary>
    public Point3d FrustumCenter
    {
      get
      {
        var cen = new Point3d();
        IntPtr const_ptr_this = ConstPointer();
        UnsafeNativeMethods.ON_Viewport_GetFrustumCenter(const_ptr_this, ref cen);
        return cen;
      }
    }

    const int idxFrustumLeft = 0;
    const int idxFrustumRight = 1;
    const int idxFrustumBottom = 2;
    const int idxFrustumTop = 3;
    const int idxFrustumNear = 4;
    const int idxFrustumFar = 5;
    const int idxFrustumMinimumDiameter = 6;
    const int idxFrustumMaximumDiameter = 7;
    double GetDouble(int which)
    {
      IntPtr const_ptr_this = ConstPointer();
      return UnsafeNativeMethods.ON_Viewport_GetDouble(const_ptr_this, which);
    }

    /// <summary>
    /// Gets the frustum left value. This is -right if the frustum has a vertical symmetry axis.
    /// <para>This number is usually negative.</para>
    /// </summary>
    public double FrustumLeft { get { return GetDouble(idxFrustumLeft); } }

    /// <summary>
    /// Gets the frustum right value. This is -left if the frustum has a vertical symmetry axis.
    /// <para>This number is usually positive.</para>
    /// </summary>
    public double FrustumRight { get { return GetDouble(idxFrustumRight); } }

    /// <summary>
    /// Gets the frustum bottom value. This is -top if the frustum has a horizontal symmetry axis.
    /// <para>This number is usually negative.</para>
    /// </summary>
    public double FrustumBottom { get { return GetDouble(idxFrustumBottom); } }

    /// <summary>
    /// Gets the frustum top value. This is -bottom if the frustum has a horizontal symmetry axis.
    /// <para>This number is usually positive.</para>
    /// </summary>
    public double FrustumTop { get { return GetDouble(idxFrustumTop); } }

    /// <summary>
    /// Gets the frustum near-cutting value.
    /// </summary>
    public double FrustumNear { get { return GetDouble(idxFrustumNear); } }

    /// <summary>
    /// Gets the frustum far-cutting value.
    /// </summary>
    public double FrustumFar { get { return GetDouble(idxFrustumFar); } }

    /// <summary>
    /// Gets the frustum width. This is <see cref="FrustumRight"/> - <see cref="FrustumLeft"/>.
    /// </summary>
    public double FrustumWidth    { get { return FrustumRight - FrustumLeft; } }

    /// <summary>
    /// Gets the frustum height. This is <see cref="FrustumTop"/> - <see cref="FrustumBottom"/>.
    /// </summary>
    public double FrustumHeight   { get { return FrustumTop - FrustumBottom; } }

    /// <summary>
    /// Gets the frustum minimum diameter, or the minimum between <see cref="FrustumWidth"/> and <see cref="FrustumHeight"/>.
    /// </summary>
    public double FrustumMinimumDiameter { get { return GetDouble(idxFrustumMinimumDiameter); } }

    /// <summary>
    /// Gets the frustum maximum diameter, or the maximum between <see cref="FrustumWidth"/> and <see cref="FrustumHeight"/>.
    /// </summary>
    public double FrustumMaximumDiameter { get { return GetDouble(idxFrustumMaximumDiameter); } }

    /// <summary>
    /// Sets the frustum near and far using a bounding box.
    /// </summary>
    /// <param name="boundingBox">A bounding box to use.</param>
    /// <returns>true if operation succeeded; otherwise, false.</returns>
    public bool SetFrustumNearFar(BoundingBox boundingBox)
    {
      IntPtr ptr_this = NonConstPointer();
      return UnsafeNativeMethods.ON_Viewport_SetFrustumNearFarBoundingBox(ptr_this, boundingBox.Min, boundingBox.Max);
    }

    /// <summary>
    /// Sets the frustum near and far using a center point and radius.
    /// </summary>
    /// <param name="center">A center point.</param>
    /// <param name="radius">A radius value.</param>
    /// <returns>true if operation succeeded; otherwise, false.</returns>
    public bool SetFrustumNearFar(Point3d center, double radius)
    {
      IntPtr ptr_this = NonConstPointer();
      return UnsafeNativeMethods.ON_Viewport_SetFrustumNearFarSphere(ptr_this, center, radius);
    }

    /// <summary>
    /// Sets the frustum near and far distances using two values.
    /// </summary>
    /// <param name="nearDistance">The new near distance.</param>
    /// <param name="farDistance">The new far distance.</param>
    /// <returns>true if operation succeeded; otherwise, false.</returns>
    public bool SetFrustumNearFar(double nearDistance, double farDistance)
    {
      IntPtr ptr_this = NonConstPointer();
      return UnsafeNativeMethods.ON_Viewport_SetFrustumNearFar(ptr_this, nearDistance, farDistance);
    }

    /// <summary>
    /// If needed, adjusts the current frustum so it has the 
    /// specified symmetries and adjust the camera location
    /// so the target plane remains visible.
    /// </summary>
    /// <param name="isLeftRightSymmetric">If true, the frustum will be adjusted so left = -right.</param>
    /// <param name="isTopBottomSymmetric">If true, the frustum will be adjusted so top = -bottom.</param>
    /// <param name="targetDistance">
    /// If projection is not perspective or target_distance is RhinoMath.UnsetValue,
    /// then this parameter is ignored. If the projection is perspective and targetDistance
    /// is not RhinoMath.UnsetValue, then it must be > 0.0 and it is used to determine
    /// which plane in the old frustum will appear unchanged in the new frustum.
    /// </param>
    /// <returns>
    /// Returns true if the viewport has now a frustum with the specified symmetries.
    /// </returns>
    public bool ChangeToSymmetricFrustum(bool isLeftRightSymmetric, bool isTopBottomSymmetric, double targetDistance)
    {
      IntPtr ptr_this = NonConstPointer();
      return UnsafeNativeMethods.ON_Viewport_ChangeToSymmetricFrustum(ptr_this, isLeftRightSymmetric, isTopBottomSymmetric, targetDistance);
    }

    /// <summary>
    /// Gets the clipping distance of a point. This function ignores the
    /// current value of the viewport's near and far settings. If
    /// the viewport is a perspective projection, then it intersects
    /// the semi infinite frustum volume with the bounding box and
    /// returns the near and far distances of the intersection.
    /// If the viewport is a parallel projection, it instersects the
    /// infinte view region with the bounding box and returns the
    /// near and far distances of the projection.
    /// </summary>
    /// <param name="point">A point to measure.</param>
    /// <param name="distance">distance of the point (can be &lt; 0)</param>
    /// <returns>true if the bounding box intersects the view frustum and
    /// near_dist/far_dist were set.
    /// false if the bounding box does not intesect the view frustum.</returns>
    public bool GetPointDepth(Point3d point, out double distance)
    {
      IntPtr const_ptr_this = ConstPointer();
      double far_distance = 0;
      distance = 0;
      return UnsafeNativeMethods.ON_Viewport_GetPointDepth(const_ptr_this, point, ref distance, ref far_distance, false);
    }

    /// <summary>
    /// Gets near and far clipping distances of a bounding box.
    /// This function ignores the current value of the viewport's 
    /// near and far settings. If the viewport is a perspective
    /// projection, the it intersects the semi infinite frustum
    /// volume with the bounding box and returns the near and far
    /// distances of the intersection.  If the viewport is a parallel
    /// projection, it instersects the infinte view region with the
    /// bounding box and returns the near and far distances of the
    /// projection.
    /// </summary>
    /// <param name="bbox">The bounding box to sample.</param>
    /// <param name="nearDistance">Near distance of the box. This value can be zero or 
    /// negative when the camera location is inside bbox.</param>
    /// <param name="farDistance">Far distance of the box. This value can be equal to 
    /// near_dist, zero or negative when the camera location is in front of the bounding box.</param>
    /// <returns>true if the bounding box intersects the view frustum and near_dist/far_dist were set. 
    /// false if the bounding box does not intesect the view frustum.</returns>
    public bool GetBoundingBoxDepth(BoundingBox bbox, out double nearDistance, out double farDistance)
    {
      IntPtr const_ptr_this = ConstPointer();
      nearDistance = 0;
      farDistance = 0;
      return UnsafeNativeMethods.ON_Viewport_GetBoundingBoxDepth(const_ptr_this, bbox.Min, bbox.Max, ref nearDistance, ref farDistance, false);
    }

    /// <summary>
    /// Gets near and far clipping distances of a bounding sphere.
    /// </summary>
    /// <param name="sphere">The sphere to sample.</param>
    /// <param name="nearDistance">Near distance of the sphere (can be &lt; 0)</param>
    /// <param name="farDistance">Far distance of the sphere (can be equal to near_dist)</param>
    /// <returns>true if the sphere intersects the view frustum and near_dist/far_dist were set.
    /// false if the sphere does not intesect the view frustum.</returns>
    public bool GetSphereDepth(Sphere sphere, out double nearDistance, out double farDistance)
    {
      IntPtr const_ptr_this = ConstPointer();
      nearDistance = 0;
      farDistance = 0;
      return UnsafeNativeMethods.ON_Viewport_GetSphereDepth(const_ptr_this, sphere.Center, sphere.Radius, ref nearDistance, ref farDistance, false);
    }

    /// <summary>
    /// Sets near and far clipping distance subject to constraints.
    /// </summary>
    /// <param name="nearDistance">(>0) desired near clipping distance.</param>
    /// <param name="farDistance">(>near_dist) desired near clipping distance.</param>
    /// <param name="minNearDistance">
    /// If min_near_dist &lt;= 0.0, it is ignored.
    /// If min_near_dist &gt; 0 and near_dist &lt; min_near_dist, then the frustum's near_dist will be increased to min_near_dist.
    /// </param>
    /// <param name="minNearOverFar">
    /// If min_near_over_far &lt;= 0.0, it is ignored.
    /// If near_dist &lt; far_dist*min_near_over_far, then
    /// near_dist is increased and/or far_dist is decreased
    /// so that near_dist = far_dist*min_near_over_far.
    /// If near_dist &lt; target_dist &lt; far_dist, then near_dist
    /// near_dist is increased and far_dist is decreased so that
    /// projection precision will be good at target_dist.
    /// Otherwise, near_dist is simply set to 
    /// far_dist*min_near_over_far.
    /// </param>
    /// <param name="targetDistance">If target_dist &lt;= 0.0, it is ignored.
    /// If target_dist &gt; 0, it is used as described in the
    /// description of the min_near_over_far parameter.</param>
    /// <returns>true if operation succeeded; otherwise, false.</returns>
    public bool SetFrustumNearFar(double nearDistance, double farDistance, double minNearDistance, double minNearOverFar, double targetDistance)
    {
      IntPtr ptr_this = NonConstPointer();
      return UnsafeNativeMethods.ON_Viewport_SetFrustrumNearFar(ptr_this, nearDistance, farDistance, minNearDistance, minNearOverFar, targetDistance);
    }

    const int idxNearPlane = 0;
    const int idxFarPlane = 1;
    const int idxLeftPlane = 2;
    const int idxRightPlane = 3;
    const int idxBottomPlane = 4;
    const int idxTopPlane = 5;
    Plane GetPlane(int which)
    {
      IntPtr const_ptr_this = ConstPointer();
      var plane = new Plane();
      if (!UnsafeNativeMethods.ON_Viewport_GetPlane(const_ptr_this, which, ref plane))
        plane = Plane.Unset;
      return plane;
    }

    /// <summary>
    /// Gets near clipping plane if camera and frustum
    /// are valid.  The plane's frame is the same as the camera's
    /// frame.  The origin is located at the intersection of the
    /// camera direction ray and the near clipping plane. The plane's
    /// normal points out of the frustum towards the camera
    /// location.
    /// </summary>
    public Plane FrustumNearPlane
    {
      get { return GetPlane(idxNearPlane); }
    }

    /// <summary>
    /// Gets far clipping plane if camera and frustum
    /// are valid.  The plane's frame is the same as the camera's
    /// frame.  The origin is located at the intersection of the
    /// camera direction ray and the far clipping plane. The plane's
    /// normal points into the frustum towards the camera location.
    /// </summary>
    public Plane FrustumFarPlane
    {
      get { return GetPlane(idxFarPlane); }
    }

    /// <summary>
    /// Gets the frustum left plane that separates visibile from off-screen.
    /// </summary>
    public Plane FrustumLeftPlane
    {
      get { return GetPlane(idxLeftPlane); }
    }

    /// <summary>
    /// Gets the frustum right plane that separates visibile from off-screen.
    /// </summary>
    public Plane FrustumRightPlane
    {
      get { return GetPlane(idxRightPlane); }
    }

    /// <summary>
    /// Gets the frustum bottom plane that separates visibile from off-screen.
    /// </summary>
    public Rhino.Geometry.Plane FrustumBottomPlane
    {
      get { return GetPlane(idxBottomPlane); }
    }

    /// <summary>
    /// Gets the frustum top plane that separates visibile from off-screen.
    /// </summary>
    public Plane FrustumTopPlane
    {
      get { return GetPlane(idxTopPlane); }
    }
    
    /// <summary>
    /// Gets the corners of near clipping plane rectangle.
    /// 4 points are returned in the order of bottom left, bottom right,
    /// top left, top right.
    /// </summary>
    /// <returns>
    /// Four corner points on success.
    /// Empty array if viewport is not valid.
    /// </returns>
    public Point3d[] GetNearPlaneCorners()
    {
      var left_bottom = new Point3d();
      var right_bottom = new Point3d();
      var left_top = new Point3d();
      var right_top = new Point3d();
      IntPtr const_ptr_this = ConstPointer();
      if (!UnsafeNativeMethods.ON_Viewport_GetNearFarRect(const_ptr_this, true, ref left_bottom, ref right_bottom, ref left_top, ref right_top))
        return new Point3d[0];
      return new[] { left_bottom, right_bottom, left_top, right_top };
    }

    /// <summary>
    /// Gets the corners of far clipping plane rectangle.
    /// 4 points are returned in the order of bottom left, bottom right,
    /// top left, top right.
    /// </summary>
    /// <returns>
    /// Four corner points on success.
    /// Empty array if viewport is not valid.
    /// </returns>
    public Point3d[] GetFarPlaneCorners()
    {
      var left_bottom = new Point3d();
      var right_bottom = new Point3d();
      var left_top = new Point3d();
      var right_top = new Point3d();
      IntPtr const_ptr_this = ConstPointer();
      if (!UnsafeNativeMethods.ON_Viewport_GetNearFarRect(const_ptr_this, false, ref left_bottom, ref right_bottom, ref left_top, ref right_top))
        return new Point3d[0];
      return new[] { left_bottom, right_bottom, left_top, right_top };
    }

    /// <summary>
    /// Location of viewport in pixels.
    /// These are provided so you can set the port you are using
    /// and get the appropriate transformations to and from
    /// screen space.
    /// // For a Windows window
    /// /      int width = width of window client area in pixels;
    /// /      int height = height of window client area in pixels;
    /// /      port_left = 0;
    /// /      port_right = width;
    /// /      port_top = 0;
    /// /      port_bottom = height;
    /// /      port_near = 0;
    /// /      port_far = 1;
    /// /      SetScreenPort( port_left, port_right, 
    /// /                     port_bottom, port_top, 
    /// /                     port_near, port_far );
    /// </summary>
    /// <param name="left">A left value.</param>
    /// <param name="right">A left value. (port_left != port_right)</param>
    /// <param name="bottom">A bottom value.</param>
    /// <param name="top">A top value. (port_top != port_bottom)</param>
    /// <param name="near">A near value.</param>
    /// <param name="far">A far value.</param>
    /// <returns>true if input is valid.</returns>
    public bool SetScreenPort(int left, int right, int bottom, int top, int near, int far)
    {
      IntPtr ptr_this = NonConstPointer();
      return UnsafeNativeMethods.ON_Viewport_SetScreenPort(ptr_this, left, right, bottom, top, near, far);
    }

    /// <summary>
    /// Gets the location of viewport in pixels.
    /// <para>See value meanings in <see cref="SetScreenPort(int, int, int, int, int, int)">SetScreenPort</see>.</para>
    /// </summary>
    /// <param name="windowRectangle">A new rectangle.</param>
    /// <param name="near">The near value.</param>
    /// <param name="far">The far value.</param>
    /// <returns>true if input is valid.</returns>
    public bool SetScreenPort(System.Drawing.Rectangle windowRectangle, int near, int far)
    {
      return SetScreenPort(windowRectangle.Left, windowRectangle.Right, windowRectangle.Bottom, windowRectangle.Top, near, far);
    }

    /// <summary>
    /// Gets the location of viewport in pixels.
    /// <para>See value meanings in <see cref="SetScreenPort(int, int, int, int, int, int)">SetScreenPort</see>.</para>
    /// </summary>
    /// <param name="windowRectangle">A new rectangle.</param>
    /// <returns>true if input is valid.</returns>
    public bool SetScreenPort(System.Drawing.Rectangle windowRectangle)
    {
      return SetScreenPort(windowRectangle, 0, 0);
    }

    /// <summary>
    /// Get the location of viewport in pixels (non System.Drawing version of GetScreenPort)
    /// </summary>
    /// <param name="left"></param>
    /// <param name="top"></param>
    /// <param name="right"></param>
    /// <param name="bottom"></param>
    public void GetScreenPortLocation(out int left, out int top, out int right, out int bottom)
    {
      var rect = ScreenPort;
      left = rect.Left;
      top = rect.Top;
      right = rect.Right;
      bottom = rect.Bottom;
    }

    /// <summary>
    /// Gets the location of viewport in pixels.
    /// <para>See value meanings in <see cref="SetScreenPort(int, int, int, int, int, int)">SetScreenPort</see>.</para>
    /// </summary>
    /// <param name="near">The near value. This out parameter is assigned during the call.</param>
    /// <param name="far">The far value. This out parameter is assigned during the call.</param>
    /// <returns>The rectangle, or <see cref="System.Drawing.Rectangle.Empty">Empty</see> rectangle on error.</returns>
    public System.Drawing.Rectangle GetScreenPort(out int near, out int far)
    {
      int left = 0;
      int right = 0;
      int bottom = 0;
      int top = 0;
      near = 0;
      far = 0;
      IntPtr const_ptr_this = ConstPointer();
      if (!UnsafeNativeMethods.ON_Viewport_GetScreenPort(const_ptr_this, ref left, ref right, ref bottom, ref top, ref near, ref far))
        return System.Drawing.Rectangle.Empty;
      // .NET/Windows uses Y down so make sure the top and bottom are passed correctly to Rectangle.FromLTRB()
      // OpenNurbs appears to by Y-Down and the RDK appears to by Y-Up so this check should ensure a Rectangle
      // with a positive height.
      return System.Drawing.Rectangle.FromLTRB(left, top > bottom ? bottom : top, right, top > bottom ? top : bottom);
    }

    /// <summary>
    /// Gets the location of viewport in pixels.
    /// See documentation for <see cref="SetScreenPort(int, int, int, int, int, int)">SetScreenPort</see>.
    /// </summary>
    /// <returns>The rectangle, or <see cref="System.Drawing.Rectangle.Empty">Empty</see> rectangle on error.</returns>
    public System.Drawing.Rectangle GetScreenPort()
    {
      int near;
      int far;
      return GetScreenPort(out near, out far);
    }

    /// <summary>
    /// Get or set the screen port. <see cref="SetScreenPort(System.Drawing.Rectangle)"/> and <seealso cref="GetScreenPort()"/>
    /// </summary>
    public System.Drawing.Rectangle ScreenPort
    {
      get
      {
        return GetScreenPort();
      }
      set
      {
        SetScreenPort(value);
      }
    }

    /// <summary>
    /// Gets the sceen aspect ratio.
    /// <para>This is width / height.</para>
    /// </summary>
    public double ScreenPortAspect
    {
      get
      {
        double aspect = 0.0;
        IntPtr const_ptr_this = ConstPointer();
        if (!UnsafeNativeMethods.ON_Viewport_GetScreenPortAspect(const_ptr_this, ref aspect))
          aspect = 0;
        return aspect;
      }
    }

    /// <summary>
    /// Gets the field of view angles.
    /// </summary>
    /// <param name="halfDiagonalAngleRadians">1/2 of diagonal subtended angle. This out parameter is assigned during this call.</param>
    /// <param name="halfVerticalAngleRadians">1/2 of vertical subtended angle. This out parameter is assigned during this call.</param>
    /// <param name="halfHorizontalAngleRadians">1/2 of horizontal subtended angle. This out parameter is assigned during this call.</param>
    /// <returns>true if the operation succeeded; otherwise, false.</returns>
    public bool GetCameraAngles(out double halfDiagonalAngleRadians, out double halfVerticalAngleRadians, out double halfHorizontalAngleRadians)
    {
      IntPtr const_ptr_this = ConstPointer();
      halfDiagonalAngleRadians = 0;
      halfHorizontalAngleRadians = 0;
      halfVerticalAngleRadians = 0;
      return UnsafeNativeMethods.ON_Viewport_GetCameraAngle2(const_ptr_this, ref halfDiagonalAngleRadians, ref halfVerticalAngleRadians, ref halfHorizontalAngleRadians);
    }

    /// <summary>
    /// Gets or sets the 1/2 smallest angle. See <see cref="GetCameraAngles"/> for more information.
    /// </summary>
    public double CameraAngle
    {
      get
      {
        double d = 0.0;
        IntPtr const_ptr_this = ConstPointer();
        if (!UnsafeNativeMethods.ON_Viewport_GetCameraAngle(const_ptr_this, ref d))
          d = 0;
        return d;
      }
      set
      {
        IntPtr ptr_this = NonConstPointer();
        UnsafeNativeMethods.ON_Viewport_SetCameraAngle(ptr_this, value);
      }
    }

    /// <summary>
    /// This property assumes the camera is horizontal and crop the
    /// film rather than the image when the aspect of the frustum
    /// is not 36/24.  (35mm film is 36mm wide and 24mm high.)
    /// Setting preserves camera location,
    /// changes the frustum, but maintains the frustum's aspect.
    /// </summary>
    public double Camera35mmLensLength
    {
      get
      {
        double d = 0.0;
        IntPtr const_ptr_this = ConstPointer();
        if (!UnsafeNativeMethods.ON_Viewport_GetCamera35mmLensLength(const_ptr_this, ref d))
          d = 0;
        return d;
      }
      set
      {
        IntPtr ptr_this = NonConstPointer();
        UnsafeNativeMethods.ON_Viewport_SetCamera35mmLensLength(ptr_this, value);
      }
    }

    /// <summary>
    /// Computes a transform from a coordinate system to another.
    /// </summary>
    /// <param name="sourceSystem">The coordinate system to map from.</param>
    /// <param name="destinationSystem">The coordinate system to map into.</param>
    /// <returns>The 4x4 transformation matrix (acts on the left).</returns>
    public Transform GetXform(CoordinateSystem sourceSystem, CoordinateSystem destinationSystem)
    {
      var matrix = new Transform();
      IntPtr const_ptr_this = ConstPointer();
      if (!UnsafeNativeMethods.ON_Viewport_GetXform(const_ptr_this, (int)sourceSystem, (int)destinationSystem, ref matrix))
        matrix = Transform.Unset;
      return matrix;
    }

    /// <summary>
    /// Gets the world coordinate line in the view frustum
    /// that projects to a point on the screen.
    /// </summary>
    /// <param name="screenX">(screenx,screeny) = screen location.</param>
    /// <param name="screenY">(screenx,screeny) = screen location.</param>
    /// <returns>3d world coordinate line segment starting on the near clipping plane and ending on the far clipping plane.</returns>
    public Line GetFrustumLine( double screenX, double screenY)
    {
      var line = new Line();
      IntPtr const_ptr_this = ConstPointer();
      if (!UnsafeNativeMethods.ON_Viewport_GetFrustumLine(const_ptr_this, screenX, screenY, ref line))
        line = Line.Unset;
      return line;
    }

    /// <summary>
    /// Gets the world coordinate line in the view frustum
    /// that projects to a point on the screen.
    /// </summary>
    /// <param name="screenPoint">screen location</param>
    /// <returns>3d world coordinate line segment starting on the near clipping plane and ending on the far clipping plane.</returns>
    public Line GetFrustumLine(System.Drawing.Point screenPoint)
    {
      return GetFrustumLine(screenPoint.X, screenPoint.Y);
    }

    /// <summary>
    /// Gets the world coordinate line in the view frustum
    /// that projects to a point on the screen.
    /// </summary>
    /// <param name="screenPoint">screen location</param>
    /// <returns>3d world coordinate line segment starting on the near clipping plane and ending on the far clipping plane.</returns>
    public Line GetFrustumLine(System.Drawing.PointF screenPoint)
    {
      return GetFrustumLine(screenPoint.X, screenPoint.Y);
    }

    /// <summary>
    /// Gets the scale factor from point in frustum to screen scale.
    /// </summary>
    /// <param name="pointInFrustum">point in viewing frustum.</param>
    /// <returns>number of pixels per world unit at the 3d point.</returns>
    public double GetWorldToScreenScale(Point3d pointInFrustum)
    {
      double d = 0.0;
      IntPtr const_ptr_this = ConstPointer();
      if (!UnsafeNativeMethods.ON_Viewport_GetWorldToScreenScale(const_ptr_this, pointInFrustum, ref d))
        d = 0;
      return d;
    }

    /// <summary>
    /// Extends this viewport view to include a bounding box.
    /// <para>Use Extents() as a quick way to set a viewport to so that bounding
    /// volume is inside of a viewports frustrum.
    /// The view angle is used to determine the position of the camera.</para>
    /// </summary>
    /// <param name="halfViewAngleRadians">1/2 smallest subtended view angle in radians.</param>
    /// <param name="bbox">A bounding box in 3d world coordinates.</param>
    /// <returns>true if the operation succeeded; otherwise, false.</returns>
    public bool Extents(double halfViewAngleRadians, BoundingBox bbox)
    {
      IntPtr ptr_this = NonConstPointer();
      return UnsafeNativeMethods.ON_Viewport_ExtentsBBox(ptr_this, halfViewAngleRadians, bbox.Min, bbox.Max);
    }

    /// <summary>
    /// Extends this viewport view to include a sphere.
    /// <para>Use Extents() as a quick way to set a viewport to so that bounding
    /// volume is inside of a viewports frustrum.
    /// The view angle is used to determine the position of the camera.</para>
    /// </summary>
    /// <param name="halfViewAngleRadians">1/2 smallest subtended view angle in radians.</param>
    /// <param name="sphere">A sphere in 3d world coordinates.</param>
    /// <returns>true if the operation succeeded; otherwise, false.</returns>
    public bool Extents(double halfViewAngleRadians, Sphere sphere)
    {
      IntPtr ptr_this = NonConstPointer();
      return UnsafeNativeMethods.ON_Viewport_ExtentsSphere(ptr_this, halfViewAngleRadians, sphere.Center, sphere.Radius);
    }

    /// <summary>
    /// Zooms to a screen zone.
    /// <para>View changing from screen input points. Handy for
    /// using a mouse to manipulate a view.
    /// ZoomToScreenRect() may change camera and frustum settings.</para>
    /// </summary>
    /// <param name="left">Screen coord.</param>
    /// <param name="top">Screen coord.</param>
    /// <param name="right">Screen coord.</param>
    /// <param name="bottom">Screen coord.</param>
    /// <returns>true if the operation succeeded; otherwise, false.</returns>
    public bool ZoomToScreenRect(int left, int top, int right, int bottom)
    {
      IntPtr ptr_this = NonConstPointer();
      return UnsafeNativeMethods.ON_Viewport_ZoomToScreenRect(ptr_this, left, top, right, bottom);
    }

    /// <summary>
    /// Zooms to a screen zone.
    /// <para>View changing from screen input points. Handy for
    /// using a mouse to manipulate a view.
    /// ZoomToScreenRect() may change camera and frustum settings.</para>
    /// </summary>
    /// <param name="windowRectangle">The new window rectangle in screen space.</param>
    /// <returns>true if the operation succeeded; otherwise, false.</returns>
    public bool ZoomToScreenRect(System.Drawing.Rectangle windowRectangle)
    {
      return ZoomToScreenRect(windowRectangle.Left, windowRectangle.Top, windowRectangle.Right, windowRectangle.Bottom);
    }

    /// <summary>
    /// DollyCamera() does not update the frustum's clipping planes.
    /// To update the frustum's clipping planes call DollyFrustum(d)
    /// with d = dollyVector o cameraFrameZ.  To convert screen locations
    /// into a dolly vector, use GetDollyCameraVector().
    /// Does not update frustum.  To update frustum use DollyFrustum(d) with d = dollyVector o cameraFrameZ.
    /// </summary>
    /// <param name="dollyVector">dolly vector in world coordinates.</param>
    /// <returns>true if the operation succeeded; otherwise, false.</returns>
    public bool DollyCamera(Vector3d dollyVector)
    {
      IntPtr ptr_this = NonConstPointer();
      return UnsafeNativeMethods.ON_Viewport_DollyCamera(ptr_this, dollyVector);
    }

    /// <summary>
    /// Gets a world coordinate dolly vector that can be passed to DollyCamera().
    /// </summary>
    /// <param name="screenX0">Screen coords of start point.</param>
    /// <param name="screenY0">Screen coords of start point.</param>
    /// <param name="screenX1">Screen coords of end point.</param>
    /// <param name="screenY1">Screen coords of end point.</param>
    /// <param name="projectionPlaneDistance">Distance of projection plane from camera. When in doubt, use 0.5*(frus_near+frus_far).</param>
    /// <returns>The world coordinate dolly vector.</returns>
    public Vector3d GetDollyCameraVector(int screenX0, int screenY0, int screenX1, int screenY1, double projectionPlaneDistance)
    {
      var v = new Vector3d();
      IntPtr const_ptr_this = ConstPointer();
      if (UnsafeNativeMethods.ON_Viewport_GetDollyCameraVector(const_ptr_this, screenX0, screenY0, screenX1, screenY1, projectionPlaneDistance, ref v))
        v = Vector3d.Unset;
      return v;
    }

    /// <summary>
    /// Gets a world coordinate dolly vector that can be passed to DollyCamera().
    /// </summary>
    /// <param name="screen0">Start point.</param>
    /// <param name="screen1">End point.</param>
    /// <param name="projectionPlaneDistance">Distance of projection plane from camera. When in doubt, use 0.5*(frus_near+frus_far).</param>
    /// <returns>The world coordinate dolly vector.</returns>
    public Vector3d GetDollyCameraVector(System.Drawing.Point screen0, System.Drawing.Point screen1, double projectionPlaneDistance)
    {
      return GetDollyCameraVector(screen0.X, screen0.Y, screen1.X, screen1.Y, projectionPlaneDistance);
    }

    /// <summary>
    /// Moves the frustum clipping planes.
    /// </summary>
    /// <param name="dollyDistance">Distance to move in camera direction.</param>
    /// <returns>true if the operation succeeded; otherwise, false.</returns>
    public bool DollyFrustum(double dollyDistance)
    {
      IntPtr ptr_this = NonConstPointer();
      return UnsafeNativeMethods.ON_Viewport_DollyFrustum(ptr_this, dollyDistance);
    }

    /// <summary>
    /// Dolly the camera location and so that the view frustum contains
    /// all of the document objects that can be seen in view.
    /// If the projection is perspective, the camera angle is not changed.
    /// </summary>
    /// <param name="geometry"></param>
    /// <param name="border">
    /// If border > 1.0, then the fustum in enlarged by this factor
    /// to provide a border around the view.  1.1 works well for
    /// parallel projections; 0.0 is suggested for perspective projections.
    /// </param>
    /// <returns>True if successful.</returns>
    public bool DollyExtents(IEnumerable<GeometryBase> geometry, double border)
    {
      var world_2_camera = GetXform(CoordinateSystem.World, CoordinateSystem.Camera);
      BoundingBox cam_bbox = BoundingBox.Unset;
      foreach( var g in geometry )
      {
        var bbox = g.GetBoundingBox(world_2_camera);
        cam_bbox.Union(bbox);
      }
      return DollyExtents(cam_bbox, border);
    }

    /// <summary>
    /// Dolly the camera location and so that the view frustum contains
    /// all of the document objects that can be seen in view.
    /// If the projection is perspective, the camera angle is not changed.
    /// </summary>
    /// <param name="cameraCoordinateBoundingBox"></param>
    /// <param name="border">
    /// If border > 1.0, then the fustum in enlarged by this factor
    /// to provide a border around the view.  1.1 works well for
    /// parallel projections; 0.0 is suggested for perspective projections.
    /// </param>
    /// <returns>True if successful.</returns>
    public bool DollyExtents(BoundingBox cameraCoordinateBoundingBox, double border)
    {
      bool rc = false;
      if (cameraCoordinateBoundingBox.IsValid)
      {
        if (border > 1.0 && RhinoMath.IsValidDouble(border))
        {
          double dx = cameraCoordinateBoundingBox.Max.X - cameraCoordinateBoundingBox.Min.X;
          dx *= 0.5 * (border - 1.0);
          double dy = cameraCoordinateBoundingBox.Max.Y - cameraCoordinateBoundingBox.Min.Y;
          dy *= 0.5 * (border - 1.0);
          var pt = cameraCoordinateBoundingBox.Max;
          cameraCoordinateBoundingBox.Max = new Point3d(pt.X + dx, pt.Y + dy, pt.Z);
          pt = cameraCoordinateBoundingBox.Min;
          cameraCoordinateBoundingBox.Min = new Point3d(pt.X - dx, pt.Y - dy, pt.Z);
        }
        IntPtr ptr_this = NonConstPointer();
        rc = UnsafeNativeMethods.ON_Viewport_DollyExtents(ptr_this, cameraCoordinateBoundingBox.Min, cameraCoordinateBoundingBox.Max);
      }
      return rc;
    }

    /// <summary>
    /// Applies scaling factors to parallel projection clipping coordinates
    /// by setting the m_clip_mod transformation. 
    /// If you want to compress the view projection across the viewing
    /// plane, then set x = 0.5, y = 1.0, and z = 1.0.
    /// </summary>
    public System.Drawing.SizeF ViewScale
    {
      get
      {
        double w = 0.0;
        double h = 0.0;
        IntPtr const_ptr_this = ConstPointer();
        UnsafeNativeMethods.ON_Viewport_GetViewScale(const_ptr_this, ref w, ref h);
        return new System.Drawing.SizeF((float)w, (float)h);
      }
      set
      {
        IntPtr ptr_this = NonConstPointer();
        UnsafeNativeMethods.ON_Viewport_SetViewScale(ptr_this, (double)value.Width, (double)value.Height);
      }
    }
    
    /* Don't wrap until someone asks for this.
    /// <summary>
    /// Gets the m_clip_mod transformation.
    /// </summary>
    public Rhino.Geometry.Transform ClipModTransform
    {
      get
      {
        Rhino.Geometry.Transform matrix = new Rhino.Geometry.Transform();
        UnsafeNativeMethods.ON_Viewport_ClipModXform(ConstPointer(), ref matrix);
        return matrix;
      }
    }

    /// <summary>
    /// Gets the m_clip_mod inverse transformation.
    /// </summary>
    public Rhino.Geometry.Transform ClipModInverseTransform
    {
      get
      {
        Rhino.Geometry.Transform matrix = new Rhino.Geometry.Transform();
        UnsafeNativeMethods.ON_Viewport_ClipModInverseXform(ConstPointer(), ref matrix);
        return matrix;
      }
    }

    public bool IsClipModTransformIdentity
    {
      get
      {
        return 1==UnsafeNativeMethods.ON_Viewport_ClipModXformIsIdentity(ConstPointer());
      }
    }
    */

    /// <summary>
    /// Return a point on the central axis of the view frustum.
    /// This point is a good choice for a general purpose target point.
    /// </summary>
    /// <param name="targetDistance">If targetDistance > 0.0, then the distance from the returned
    /// point to the camera plane will be targetDistance. Note that
    /// if the frustum is not symmetric, the distance from the
    /// returned point to the camera location will be larger than
    /// targetDistance.
    /// If targetDistance == ON_UNSET_VALUE and the frustum
    /// is valid with near > 0.0, then 0.5*(near + far) will be used
    /// as the targetDistance.</param>
    /// <returns>A point on the frustum's central axis.  If the viewport or input
    /// is not valid, then ON_3dPoint::UnsetPoint is returned.</returns>
    public Point3d FrustumCenterPoint( double targetDistance ) 
    {
      Point3d point = Point3d.Unset;
      IntPtr const_ptr_this = ConstPointer();
      UnsafeNativeMethods.ON_Viewport_FrustumCenterPoint(const_ptr_this, targetDistance, ref point);
      return point;
    }

    /// <summary>
    /// The current value of the target point.  This point does not play
    /// a role in the view projection calculations.  It can be used as a 
    /// fixed point when changing the camera so the visible regions of the
    /// before and after frustums both contain the region of interest.
    /// The default constructor sets this point on ON_3dPoint::UnsetPoint.
    /// You must explicitly call one SetTargetPoint() functions to set
    /// the target point.
    /// </summary>
    public Point3d TargetPoint
    {
      get
      {
        var point = new Point3d();
        IntPtr const_ptr_this = ConstPointer();
        UnsafeNativeMethods.ON_Viewport_TargetPoint(const_ptr_this, ref point);
        return point;
      }
      set
      {
        IntPtr ptr_this = NonConstPointer();
        UnsafeNativeMethods.ON_Viewport_SetTargetPoint(ptr_this, value);
      }
    }

    /// <summary>
    /// Gets the distance from the target point to the camera plane.
    /// Note that if the frustum is not symmetric, then this distance
    /// is shorter than the distance from the target to the camera location.
    /// </summary>
    /// <param name="useFrustumCenterFallback">If bUseFrustumCenterFallback is false and the target point is
    /// not valid, then ON_UNSET_VALUE is returned.
    /// If bUseFrustumCenterFallback is true and the frustum is valid
    /// and current target point is not valid or is behind the camera,
    /// then 0.5*(near + far) is returned.</param>
    /// <returns>Shortest signed distance from camera plane to target point.
    /// If the target point is on the visible side of the camera,
    /// a positive value is returned.  ON_UNSET_VALUE is returned
    /// when the input of view is not valid.</returns>
    public double TargetDistance( bool useFrustumCenterFallback )
    {
      IntPtr const_ptr_this = ConstPointer();
      return UnsafeNativeMethods.ON_Viewport_TargetDistance(const_ptr_this, useFrustumCenterFallback);
    }

    /*
    /// <summary>
    /// Gets suggested values for setting the perspective minimum
    /// near distance and minimum near/far ratio.
    /// </summary>
    /// <param name="cameraLocation">-</param>
    /// <param name="depthBufferBitDepth">Typically 32, 34, 16 or 8, but any value can be passed in.</param>
    /// <param name="minNearDist">Suggest value for passing to SetPerspectiveMinNearDist().  </param>
    /// <param name="minNearOverFar">Suggest value for passing to SetPerspectiveMinNearOverFar().    </param>
    public static void GetPerspectiveClippingPlaneConstraints( Rhino.Geometry.Point3d cameraLocation, 
                                                               int depthBufferBitDepth, 
                                                               ref double minNearDist, 
                                                               ref double minNearOverFar)
    {
      UnsafeNativeMethods.ON_Viewport_GetPerspectiveClippingPlaneConstraints(cameraLocation, depthBufferBitDepth, ref minNearDist, ref minNearOverFar);
    }

    /// <summary>
    /// Sets suggested the perspective minimum near distance and
    /// minimum near/far ratio to the suggested values returned
    /// by GetPerspectiveClippingPlaneConstraints().
    /// </summary>
    /// <param name="depthBufferBitDepth">Typically 32, 34, 16 or 8, but any value can be passed in.</param>
    public void SetPerspectiveClippingPlaneConstraints( int depthBufferBitDepth)
    {
      UnsafeNativeMethods.ON_Viewport_SetPerspectiveClippingPlaneConstraints(NonConstPointer(), depthBufferBitDepth);
    }
    */

    /// <summary>
    /// Expert user function to control the minimum
    /// ratio of near/far when perspective projections
    /// are begin used.
    /// </summary>
    public double PerspectiveMinNearOverFar
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        return UnsafeNativeMethods.ON_Viewport_GetPerspectiveMinNearOverFar(const_ptr_this);
      }
      set
      {
        IntPtr ptr_this = NonConstPointer();
        UnsafeNativeMethods.ON_Viewport_SetPerspectiveMinNearOverFar(ptr_this, value);
      }
    }

    /// <summary>
    /// Expert user function to control the minimum
    /// value of near when perspective projections
    /// are being used.
    /// </summary>
    public double PerspectiveMinNearDist
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        return UnsafeNativeMethods.ON_Viewport_GetPerspectiveMinNearDist(const_ptr_this);
      }
      set
      {
        IntPtr ptr_this = NonConstPointer();
        UnsafeNativeMethods.ON_Viewport_SetPerspectiveMinNearDist(ptr_this, value);
      }
    }

    /// <summary>
    /// Sets the viewport's id to the value used to 
    /// uniquely identify this viewport.
    /// There is no approved way to change the viewport 
    /// id once it is set in order to maintain consistency
    /// across multiple viewports and those routines that 
    /// manage them.
    /// </summary>
    public Guid Id
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        return UnsafeNativeMethods.ON_Viewport_GetViewportId(const_ptr_this);
      }
      //set
      //{
      //  IntPtr pThis = NonConstPointer();
      //  UnsafeNativeMethods.ON_Viewport_SetViewportId(pThis, value);
      //}
    }

    /// <summary>
    /// Transforms the view camera location, direction, and up.
    /// </summary>
    /// <param name="xform">Transformation to apply to camera.</param>
    /// <returns>True if a valid camera was transformed, false if
    /// invalid camera, frustum, or transformation.</returns>
    public bool TransformCamera(Transform xform)
    {
      IntPtr ptr_this = NonConstPointer();
      return UnsafeNativeMethods.ON_Viewport_Transform(ptr_this, ref xform);
    }

    /// <summary>
    /// Rotates the view camera.
    /// </summary>
    /// <param name="rotationAngleRadians">The amount to rotate expressed in radians.</param>
    /// <param name="rotationAxis">The axis to rotate around.</param>
    /// <param name="rotationCenter">The point to rotate around.</param>
    /// <returns>True if rotation is successful, false otherwise.</returns>
    public bool RotateCamera(double rotationAngleRadians, Vector3d rotationAxis, Point3d rotationCenter)
    {
      IntPtr ptr_this = NonConstPointer();
      return UnsafeNativeMethods.ON_Viewport_Rotate(ptr_this, rotationAngleRadians, rotationAxis, rotationCenter);
    }

    /*
    public static double DefaultNearDistance        { get { return 0.005; } }
    public static double DefaultFarDistance         { get { return 1000.0; } } 
    public static double DefaultMinNearDistance     { get { return 0.0001; } }
    public static double DefaultMinNearOverFar      { get { return 0.0001; } } 
    */
  }
}



