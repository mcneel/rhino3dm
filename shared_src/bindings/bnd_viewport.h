#include "bindings.h"

#pragma once

#if defined(ON_PYTHON_COMPILE)
void initViewportBindings(pybind11::module& m);
#else
void initViewportBindings();
#endif

class BND_Viewport : public BND_Object
{
  std::shared_ptr<ON_Viewport> m_viewport;
public:
  BND_Viewport();
  BND_Viewport(ON_Viewport* viewport);

  bool IsValidCameraFrame() const;
  bool IsValidCamera() const;
  bool IsValidFrustum() const;

  bool IsPerspectiveProjection() const;
  bool IsParallelProjection() const;
  bool IsTwoPointPerspectiveProjection() const;
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

  #if defined(__EMSCRIPTEN__)
  emscripten::val GetFrustum() const;

  void SetScreenPort(emscripten::val rect);
  emscripten::val GetScreenPort() const;
  #endif

  double ScreenPortAspect() const;

  double GetCameraAngle() const;
  void SetCameraAngle(double half_smallest_angle);
  double GetCamera35mmLensLength() const;
  void SetCamera35mmLensLength(double lens_length);

  ON_Xform* GetXform(ON::coordinate_system srcCS, ON::coordinate_system destCS);

  bool DollyExtents(const class BND_BoundingBox& bbox, double border);
};
