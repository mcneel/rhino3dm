#include "bindings.h"

#pragma once

#if defined(ON_PYTHON_COMPILE)
void initViewportBindings(pybind11::module& m);
#else
void initViewportBindings(void* m);
#endif

class BND_Viewport : public BND_CommonObject
{
public:
  ON_Viewport* m_viewport = nullptr;
public:
  BND_Viewport();
  BND_Viewport(ON_Viewport* viewport, const ON_ModelComponentReference* compref);

  bool IsValidCameraFrame() const { return m_viewport->IsValidCameraFrame(); }
  bool IsValidCamera() const { return m_viewport->IsValidCamera(); }
  bool IsValidFrustum() const { return m_viewport->IsValidFrustum(); }

  bool IsPerspectiveProjection() const { return m_viewport->IsPerspectiveProjection(); }
  bool IsParallelProjection() const { return m_viewport->IsParallelProjection(); }
  bool IsTwoPointPerspectiveProjection() const { return m_viewport->IsTwoPointPerspectiveProjection(); }
  void SetProjectionToParallel(bool parallel);
  void SetProjectionToPerspective(bool perspective);

  bool ChangeToParallelProjection( bool bSymmetricFrustum );
  bool ChangeToPerspectiveProjection( double target_distance, bool bSymmetricFrustum, double lens_length);
  bool ChangeToTwoPointPerspectiveProjection( double target_distance, ON_3dVector up, double lens_length);

  bool SetCameraLocation( const ON_3dPoint& );
  bool SetCameraDirection( const ON_3dVector& );
  bool SetCameraUp( const ON_3dVector& );

  ON_3dPoint CameraLocation() const;
  ON_3dVector CameraDirection() const;
  ON_3dVector CameraUp() const;

  ON_3dVector CameraX() const; // unit to right vector
  ON_3dVector CameraY() const; // unit up vector
  ON_3dVector CameraZ() const; // unit vector in -CameraDirection

  bool SetFrustum(double left,double right,double bottom,double top,double near_dist,double far_dist);

#if defined(ON_PYTHON_COMPILE)
  BND_DICT GetFrustum() const;
#endif

#if defined(__EMSCRIPTEN__)
  emscripten::val GetFrustum() const;
#endif

  void SetScreenPort(BND_TUPLE rect);
  BND_TUPLE GetScreenPort() const;

  double ScreenPortAspect() const;

  double GetCameraAngle() const;
  void SetCameraAngle(double half_smallest_angle);
  double GetCamera35mmLensLength() const;
  void SetCamera35mmLensLength(double lens_length);

  class BND_Transform* GetXform(ON::coordinate_system srcCS, ON::coordinate_system destCS);

  bool DollyExtents(const class BND_BoundingBox& bbox, double border);
  ON_3dPoint FrustumCenterPoint(double targetDistance) const { return m_viewport->FrustumCenterPoint(targetDistance); }
  //    public Point3d TargetPoint {get;set;}
  double TargetDistance(bool useFrustumCenterFallback) const { return m_viewport->TargetDistance(useFrustumCenterFallback); }
  // public double PerspectiveMinNearOverFar {get;set}
  //     public double PerspectiveMinNearDist {get;set}

  BND_UUID GetId() const;
  //    public bool TransformCamera(Transform xform)
  //    public bool RotateCamera(double rotationAngleRadians, Vector3d rotationAxis, Point3d rotationCenter)

protected:
  void SetTrackedPointer(ON_Viewport* viewport, const ON_ModelComponentReference* compref);
};
