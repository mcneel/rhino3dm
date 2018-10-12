#include "bindings.h"

BND_Viewport::BND_Viewport()
{
  SetTrackedPointer(new ON_Viewport(), nullptr);
}

BND_Viewport::BND_Viewport(ON_Viewport* viewport, const ON_ModelComponentReference* compref)
{
  SetTrackedPointer(viewport, compref);
}

void BND_Viewport::SetTrackedPointer(ON_Viewport* viewport, const ON_ModelComponentReference* compref)
{
  m_viewport = viewport;
  BND_Object::SetTrackedPointer(viewport, compref);
}

bool BND_Viewport::IsValidCameraFrame() const
{
  return m_viewport->IsValidCameraFrame();
}
bool BND_Viewport::IsValidCamera() const
{
  return m_viewport->IsValidCamera();
}
bool BND_Viewport::IsValidFrustum() const
{
  return m_viewport->IsValidFrustum();
}

bool BND_Viewport::IsPerspectiveProjection() const
{
  return m_viewport->IsPerspectiveProjection();
}
bool BND_Viewport::IsParallelProjection() const
{
  return m_viewport->IsParallelProjection();
}
bool BND_Viewport::IsTwoPointPerspectiveProjection() const
{
  return m_viewport->IsTwoPointPerspectiveProjection();
}

void BND_Viewport::SetProjectionToParallel(bool parallel)
{
  if( parallel )
    m_viewport->SetProjection(ON::parallel_view);
  else
    m_viewport->SetProjection(ON::perspective_view);
}

void BND_Viewport::SetProjectionToPerspective(bool perspective)
{
  SetProjectionToParallel(!perspective);
}

bool BND_Viewport::ChangeToParallelProjection( bool bSymmetricFrustum )
{
  return m_viewport->ChangeToParallelProjection(bSymmetricFrustum);
}
bool BND_Viewport::ChangeToPerspectiveProjection( double target_distance, bool bSymmetricFrustum, double lens_length)
{
  return m_viewport->ChangeToPerspectiveProjection(target_distance, bSymmetricFrustum, lens_length);
}
bool BND_Viewport::ChangeToTwoPointPerspectiveProjection( double target_distance, ON_3dVector up, double lens_length)
{
  return m_viewport->ChangeToTwoPointPerspectiveProjection(target_distance, up, lens_length);
}

bool BND_Viewport::SetCameraLocation( const ON_3dPoint& p)
{
  return m_viewport->SetCameraLocation(p);
}
bool BND_Viewport::SetCameraDirection( const ON_3dVector& v)
{
  return m_viewport->SetCameraDirection(v);
}
bool BND_Viewport::SetCameraUp( const ON_3dVector& v)
{
  return m_viewport->SetCameraUp(v);
}

ON_3dPoint BND_Viewport::CameraLocation() const
{
  return m_viewport->CameraLocation();
}
ON_3dVector BND_Viewport::CameraDirection() const
{
  return m_viewport->CameraDirection();
}
ON_3dVector BND_Viewport::CameraUp() const
{
  return m_viewport->CameraUp();
}

ON_3dVector BND_Viewport::CameraX() const
{
  return m_viewport->CameraX();
}
ON_3dVector BND_Viewport::CameraY() const
{
  return m_viewport->CameraY();
}
ON_3dVector BND_Viewport::CameraZ() const
{
  return m_viewport->CameraZ();
}

bool BND_Viewport::SetFrustum(double left, double right, double bottom, double top, double near_dist, double far_dist)
{
  return m_viewport->SetFrustum(left, right, bottom, top, near_dist, far_dist);
}

#if defined(__EMSCRIPTEN__)
emscripten::val BND_Viewport::GetFrustum() const
{
  double left, right, bottom, top, near, far;
  bool success = m_viewport->GetFrustum(&left, &right, &bottom, &top, &near, &far);
  if( success )
  {
    emscripten::val v(emscripten::val::object());
    v.set("left", emscripten::val(left));
    v.set("right", emscripten::val(right));
    v.set("bottom", emscripten::val(bottom));
    v.set("top", emscripten::val(top));
    v.set("near", emscripten::val(near));
    v.set("far", emscripten::val(far));
    return v;
  }
  return emscripten::val::null();
}

void BND_Viewport::SetScreenPort(emscripten::val rect)
{
  int x = rect["x"].as<int>();
  int y = rect["y"].as<int>();
  int width = rect["width"].as<int>();
  int height = rect["height"].as<int>();
  m_viewport->SetScreenPort(x, x+width, y+height, y);
}

emscripten::val BND_Viewport::GetScreenPort() const
{
  int left, right, bottom, top, near, far;
  bool success = m_viewport->GetScreenPort(&left, &right, &bottom, &top, &near, &far);
  if( success )
  {
    emscripten::val v(emscripten::val::object());
    v.set("x", emscripten::val(left));
    v.set("y", emscripten::val(top));
    v.set("width", emscripten::val(fabs(right-left)));
    v.set("height", emscripten::val(fabs(bottom-top)));
    return v;
  }
  return emscripten::val::null();
}
#endif

double BND_Viewport::ScreenPortAspect() const
{
  double aspect=1;
  m_viewport->GetScreenPortAspect(aspect);
  return aspect;
}

double BND_Viewport::GetCameraAngle() const
{
  double d=1;
  m_viewport->GetCameraAngle(&d);
  return d;
}
void BND_Viewport::SetCameraAngle(double half_smallest_angle)
{
  m_viewport->SetCameraAngle(half_smallest_angle);
}
double BND_Viewport::GetCamera35mmLensLength() const
{
  double d=1;
  m_viewport->GetCamera35mmLensLength(&d);
  return d;
}
void BND_Viewport::SetCamera35mmLensLength(double lens_length)
{
  m_viewport->SetCamera35mmLensLength(lens_length);
}

ON_Xform* BND_Viewport::GetXform(ON::coordinate_system srcCS, ON::coordinate_system destCS)
{
  ON_Xform* xf = new ON_Xform();
  if( !m_viewport->GetXform(srcCS, destCS, *xf) )
  {
    delete xf;
    return nullptr;
  }
  return xf;
}

bool BND_Viewport::DollyExtents(const BND_BoundingBox& bbox, double border)
{
  bool rc = false;
  ON_BoundingBox cameraCoordinateBoundingBox = bbox;
  if (cameraCoordinateBoundingBox.IsValid())
  {
    if (border > 1.0 && ON_IsValid(border))
    {
      double dx = cameraCoordinateBoundingBox.m_max.x - cameraCoordinateBoundingBox.m_min.x;
      dx *= 0.5 * (border - 1.0);
      double dy = cameraCoordinateBoundingBox.m_max.y - cameraCoordinateBoundingBox.m_min.y;
      dy *= 0.5 * (border - 1.0);
      ON_3dPoint pt = cameraCoordinateBoundingBox.m_max;
      cameraCoordinateBoundingBox.m_max.Set(pt.x + dx, pt.y + dy, pt.z);
      pt = cameraCoordinateBoundingBox.m_min;
      cameraCoordinateBoundingBox.m_min.Set(pt.x - dx, pt.y - dy, pt.z);
    }


    {
      ON_Viewport* pViewport = m_viewport;
      ON_BoundingBox camcoord_bbox = cameraCoordinateBoundingBox;
      if ( !camcoord_bbox.IsValid() || !pViewport->IsValid() )
      {
        return false;
      }

      double aspect = 0.0;
      if ( !m_viewport->GetFrustumAspect(aspect) )
      {
        return false;
      }
      if ( !ON_IsValid(aspect) || 0.0 == aspect )
      {
        return false;
      }

      // 22 May 2006 Dale Lear
      //     I added the scale call to handle non-uniform viewport scaling
      ON_3dVector scale(1.0,1.0,0.0);
      pViewport->GetViewScale(&scale.x,&scale.y);

      const double xmin = camcoord_bbox.m_min.x;
      const double xmax = camcoord_bbox.m_max.x;
      const double ymin = camcoord_bbox.m_min.y;
      const double ymax = camcoord_bbox.m_max.y;
      double dx = 0.5*(xmax - xmin)*scale.x;
      double dy = 0.5*(ymax - ymin)*scale.y;
      if ( dx <= ON_SQRT_EPSILON && dy <= ON_SQRT_EPSILON)
      {
        dx = dy = 0.5;
      }

      if( dx < dy*aspect )
        dx = dy*aspect;
      else
        dy = dx/aspect;

      // pad depths a bit so clippling plane are not coplanar with displayed geometry
      // zmax is on frustum near and zmin is on frustum far
      double zmin = camcoord_bbox.m_min.z;
      double zmax = camcoord_bbox.m_max.z;

      // Please discuss any changes to dz caclulations with Dale Lear
      // before you check in the code.
      double dz = (zmax - zmin)*0.00390625; // 0.00390625 = 1/256
      if ( ON::perspective_view == pViewport->Projection() )
      {
        // 16 May 2006 Dale Lear
        //    Do not increase zmax too much or you make zooming to small
        //    objects in perspective views impossible.  To test any
        //    changes, make a line from (0,0,0) to (0.001,0.001,0.001).
        //    Make a perspective view with a 50mm lens angle.  If you
        //    can't ZEA on the line, then you've adjusted dz too much.
        if ( dz <= 1.0e-6 )
          dz = 1.0e-6;
      }
      else if ( dz <= 0.125 )
      {
        // In parallel projection it is ok to be generous.
        dz = 0.125;
      }
      zmax += dz;

      // It is ok to adjust zmin by more generous amount because it
      // does not effect the ability to zoom in on small objects a
      // perspective view.
      if ( dz <= 0.125 )
        dz = 0.125;
      zmin -= dz;
      dz = zmax - zmin;

      double frus_near = 0.0;
      if( ON::parallel_view == pViewport->Projection() )
      {
        // parallel projection
        //double cota = 50.0/12.0; // 50 mm lens angle
        //frus_near = ((dx > dy) ? dx : dy)*cota;
        frus_near = 0.125*dz;
      }
      else if( ON::perspective_view == pViewport->Projection() )
      {
        // perspective projection
        double ax, ay;
        if ( pViewport->GetCameraAngle(nullptr,&ay,&ax) )
        {
          double zx = (ON_IsValid(ax) && ax > 0.0) ? dx/tan(ax) : 0.0;
          double zy = (ON_IsValid(ay) && ay > 0.0) ? dy/tan(ay) : 0.0;
          frus_near = (zx > zy) ? zx : zy;
        }
      }

      if ( !ON_IsValid(frus_near) || frus_near <= ON_SQRT_EPSILON )
      {
        frus_near = 1.0;
      }

      ON_3dPoint camloc = pViewport->CameraLocation();
      if ( camloc.IsValid() )
      {
        ON_3dVector dolly = 0.5*(xmax + xmin)*pViewport->CameraX()
                          + 0.5*(ymax + ymin)*pViewport->CameraY()
                          + (frus_near + zmax)*pViewport->CameraZ();
        camloc += dolly;
        if ( pViewport->SetCameraLocation(camloc) )
        {
          double frus_far = frus_near + dz;
          rc = pViewport->SetFrustum( -dx, dx, -dy, dy, frus_near, frus_far);
        }
      }
    }

  }
  return rc;

}

#if defined(ON_PYTHON_COMPILE)
namespace py = pybind11;
void initViewportBindings(pybind11::module& m)
{
  py::class_<BND_Viewport, BND_Object>(m, "ViewportInfo")
    .def(py::init<>())
    .def_property_readonly("IsValidCameraFrame", &BND_Viewport::IsValidCameraFrame)
    .def_property_readonly("isValidCamer", &BND_Viewport::IsValidCamera)
    .def_property_readonly("IsValidFrustum", &BND_Viewport::IsValidFrustum)
    .def_property("IsParallelProjection", &BND_Viewport::IsParallelProjection, &BND_Viewport::SetProjectionToParallel)
    .def_property("IsPerspectiveProjection", &BND_Viewport::IsPerspectiveProjection, &BND_Viewport::SetProjectionToPerspective)
    .def_property_readonly("IsTwoPointPerspectiveProjection", &BND_Viewport::IsTwoPointPerspectiveProjection)
    .def("ChangeToParallelProjection", &BND_Viewport::ChangeToParallelProjection)
    .def("ChangeToPerspectiveProjection", &BND_Viewport::ChangeToPerspectiveProjection)
    .def("ChangeToTwoPointPerspectiveProjection", &BND_Viewport::ChangeToTwoPointPerspectiveProjection)
    .def_property_readonly("CameraLocation", &BND_Viewport::CameraLocation)
    .def_property_readonly("CameraDirection", &BND_Viewport::CameraDirection)
    .def_property_readonly("CameraUp", &BND_Viewport::CameraUp)
    .def("SetCameraLocation", &BND_Viewport::SetCameraLocation)
    .def("SetCameraDirection", &BND_Viewport::SetCameraDirection)
    .def("SetCameraUp", &BND_Viewport::SetCameraUp)
    .def_property_readonly("CameraX", &BND_Viewport::CameraX)
    .def_property_readonly("CameraY", &BND_Viewport::CameraY)
    .def_property_readonly("CameraZ", &BND_Viewport::CameraZ)
    .def("SetFrustum", &BND_Viewport::SetFrustum)
    .def_property_readonly("ScreenPortAspect", &BND_Viewport::ScreenPortAspect)
    .def_property("CameraAngle", &BND_Viewport::GetCameraAngle, &BND_Viewport::SetCameraAngle)
    .def_property("Camera35mmLensLength", &BND_Viewport::GetCamera35mmLensLength, &BND_Viewport::SetCamera35mmLensLength)
    .def("GetXform", &BND_Viewport::GetXform)
    .def("DollyExtents", &BND_Viewport::DollyExtents);
}

#else

using namespace emscripten;

void initViewportBindings(void*)
{
  class_<BND_Viewport, base<BND_Object>>("ViewportInfo")
    .constructor<>()
    .property("isValidCameraFrame", &BND_Viewport::IsValidCameraFrame)
    .property("isValidCamer", &BND_Viewport::IsValidCamera)
    .property("isValidFrustum", &BND_Viewport::IsValidFrustum)
    .property("isParallelProjection", &BND_Viewport::IsParallelProjection, &BND_Viewport::SetProjectionToParallel)
    .property("isPerspectiveProjection", &BND_Viewport::IsPerspectiveProjection, &BND_Viewport::SetProjectionToPerspective)
    .property("isTwoPointPerspectiveProjection", &BND_Viewport::IsTwoPointPerspectiveProjection)
    .function("changeToParallelProjection", &BND_Viewport::ChangeToParallelProjection)
    .function("changeToPerspectiveProjection", &BND_Viewport::ChangeToPerspectiveProjection)
    .function("changeToTwoPointPerspectiveProjection", &BND_Viewport::ChangeToTwoPointPerspectiveProjection)
    .property("cameraLocation", &BND_Viewport::CameraLocation)
    .property("cameraDirection", &BND_Viewport::CameraDirection)
    .property("cameraUp", &BND_Viewport::CameraUp)
    .function("setCameraLocation", &BND_Viewport::SetCameraLocation)
    .function("setCameraDirection", &BND_Viewport::SetCameraDirection)
    .function("setCameraUp", &BND_Viewport::SetCameraUp)
    .property("cameraX", &BND_Viewport::CameraX)
    .property("cameraY", &BND_Viewport::CameraY)
    .property("cameraZ", &BND_Viewport::CameraZ)
    .function("setFrustum", &BND_Viewport::SetFrustum)
    .function("getFrustum", &BND_Viewport::GetFrustum)
    .property("screenPort", &BND_Viewport::GetScreenPort, &BND_Viewport::SetScreenPort)
    .property("screenPortAspect", &BND_Viewport::ScreenPortAspect)
    .property("cameraAngle", &BND_Viewport::GetCameraAngle, &BND_Viewport::SetCameraAngle)
    .property("camera35mmLensLength", &BND_Viewport::GetCamera35mmLensLength, &BND_Viewport::SetCamera35mmLensLength)
    .function("getXform", &BND_Viewport::GetXform, allow_raw_pointers())
    .function("dollyExtents", &BND_Viewport::DollyExtents);
}
#endif
