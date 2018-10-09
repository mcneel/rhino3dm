#include "bindings.h"

BND_BoundingBox::BND_BoundingBox(const ON_3dPoint& min, const ON_3dPoint& max)
: ON_BoundingBox(min, max)
{

}

BND_BoundingBox::BND_BoundingBox(double minX, double minY, double minZ, double maxX, double maxY, double maxZ)
: ON_BoundingBox(ON_3dPoint(minX, minY, minZ), ON_3dPoint(maxX, maxY, maxZ))
{

}

BND_BoundingBox::BND_BoundingBox(const ON_BoundingBox& bbox)
{
  m_min = bbox.m_min;
  m_max = bbox.m_max;
}

bool BND_BoundingBox::Transform(const ON_Xform& xform)
{
  return ON_BoundingBox::Transform(xform);
}

RH_C_FUNCTION ON_Brep* ON_Brep_FromBox(const ON_3dPoint& boxmin, const ON_3dPoint& boxmax)
{
  const ON_3dPoint* _boxmin = (const ON_3dPoint*)&boxmin;
  const ON_3dPoint* _boxmax = (const ON_3dPoint*)&boxmax;

  ON_3dPoint corners[8];
  corners[0] = *_boxmin;
  corners[1].x = _boxmax->x;
  corners[1].y = _boxmin->y;
  corners[1].z = _boxmin->z;

  corners[2].x = _boxmax->x;
  corners[2].y = _boxmax->y;
  corners[2].z = _boxmin->z;

  corners[3].x = _boxmin->x;
  corners[3].y = _boxmax->y;
  corners[3].z = _boxmin->z;

  corners[4].x = _boxmin->x;
  corners[4].y = _boxmin->y;
  corners[4].z = _boxmax->z;

  corners[5].x = _boxmax->x;
  corners[5].y = _boxmin->y;
  corners[5].z = _boxmax->z;

  corners[6].x = _boxmax->x;
  corners[6].y = _boxmax->y;
  corners[6].z = _boxmax->z;

  corners[7].x = _boxmin->x;
  corners[7].y = _boxmax->y;
  corners[7].z = _boxmax->z;
  ON_Brep* rc = ::ON_BrepBox(corners);
  return rc;
}

#if defined(ON_PYTHON_COMPILE)
namespace py = pybind11;
void initBoundingBoxBindings(pybind11::module& m)
{
  py::class_<BND_BoundingBox>(m, "BoundingBox")
    .def(py::init<ON_3dPoint, ON_3dPoint>())
    .def(py::init<double, double, double, double, double, double>())
    .def_property_readonly("Min", &BND_BoundingBox::Min)
    .def_property_readonly("Max", &BND_BoundingBox::Max)
    .def("Contains", &ON_BoundingBox::IsPointIn)
    .def("Transform", &BND_BoundingBox::Transform)
    .def("ToBrep", [](const BND_BoundingBox& bbox) {
        return ON_Brep_FromBox(bbox.m_min, bbox.m_max);
      })
    ;
}
#else
using namespace emscripten;

void initBoundingBoxBindings()
{
  class_<BND_BoundingBox>("BoundingBox")
    .constructor<ON_3dPoint, ON_3dPoint>()
    .constructor<double, double, double, double, double, double>()
    .property("min", &BND_BoundingBox::Min)
    .property("max", &BND_BoundingBox::Max)
    .function("transform", &BND_BoundingBox::Transform);
}
#endif
