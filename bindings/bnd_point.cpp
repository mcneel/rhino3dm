#include "bindings.h"
#if defined(ON_PYTHON_COMPILE)
#include <pybind11/operators.h>
#endif


BND_Interval::BND_Interval(const ON_Interval& i)
{
  m_t0 = i.m_t[0];
  m_t1 = i.m_t[1];
}

ON_3dPoint BND_Point3d::Transform(const ON_3dPoint& pt, const ON_Xform& transform)
{
  ON_3dPoint rc = transform * pt;
  return rc;
}


#if defined(ON_PYTHON_COMPILE)
namespace py = pybind11;
void initPointBindings(pybind11::module& m)
{
  py::class_<ON_2dPoint>(m, "Point2d")
    .def(py::init<double, double>())
    .def_readwrite("X", &ON_2dPoint::x)
    .def_readwrite("Y", &ON_2dPoint::y)
    .def(py::self + py::self)
    .def("DistanceTo", &ON_2dPoint::DistanceTo);

  py::class_<ON_3dPoint>(m, "Point3d")
    .def(py::init<double, double, double>())
    .def_readwrite("X", &ON_3dPoint::x)
    .def_readwrite("Y", &ON_3dPoint::y)
    .def_readwrite("Z", &ON_3dPoint::z)
    .def(py::self + py::self)
    .def(py::self * double())
    .def(py::self + ON_3dVector())
    .def(py::self * ON_Xform())
    .def("DistanceTo", &ON_3dPoint::DistanceTo)
    .def("Transform", &ON_3dPoint::Transform);

  py::class_<ON_4dPoint>(m, "Point4d")
    .def(py::init<double, double, double, double>())
    .def_readwrite("X", &ON_4dPoint::x)
    .def_readwrite("Y", &ON_4dPoint::y)
    .def_readwrite("Z", &ON_4dPoint::z)
    .def_readwrite("W", &ON_4dPoint::w);

  py::class_<ON_2dVector>(m, "Vector2d")
    .def(py::init<double, double>())
    .def_readwrite("X", &ON_2dVector::x)
    .def_readwrite("Y", &ON_2dVector::y);

  py::class_<ON_3dVector>(m, "Vector3d")
    .def(py::init<double, double, double>())
    .def_readwrite("X", &ON_3dVector::x)
    .def_readwrite("Y", &ON_3dVector::y)
    .def_readwrite("Z", &ON_3dVector::z);

  py::class_<ON_3fPoint>(m, "Point3f")
    .def(py::init<float, float, float>())
    .def_readwrite("X", &ON_3fPoint::x)
    .def_readwrite("Y", &ON_3fPoint::y)
    .def_readwrite("Z", &ON_3fPoint::z)
    .def(py::self + py::self);

  py::class_<BND_Interval>(m, "Interval")
    .def_readwrite("T0", &BND_Interval::m_t0)
    .def_readwrite("T1", &BND_Interval::m_t1);

}
#endif
