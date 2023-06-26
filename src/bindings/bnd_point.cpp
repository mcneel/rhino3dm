#include "bindings.h"
#if defined(ON_PYTHON_COMPILE)
#include <pybind11/operators.h>
#endif


BND_Interval::BND_Interval(const ON_Interval& i)
{
  m_t0 = i.m_t[0];
  m_t1 = i.m_t[1];
}

BND_Interval::BND_Interval(double t0, double t1)
{
  m_t0 = t0;
  m_t1 = t1;
}

bool BND_Interval::operator==(const BND_Interval& other) const
{
  return m_t0 == other.m_t0 && m_t1 == other.m_t1;
}

bool BND_Interval::operator!=(const BND_Interval& other) const
{
  return m_t0 != other.m_t0 || m_t1 != other.m_t1;
}

ON_3dPoint BND_Point3d::Transform(const ON_3dPoint& pt, const BND_Transform& transform)
{
  ON_3dPoint rc = transform.m_xform * pt;
  return rc;
}


#if defined(ON_PYTHON_COMPILE)
static ON_3dPoint GetUnsetPoint3d(pybind11::object /*self*/)
{
  return ON_3dPoint::UnsetPoint;
}

static pybind11::dict EncodePoint2d(const ON_2dPoint& pt)
{
  pybind11::dict d;
  d["X"] = pt.x;
  d["Y"] = pt.y;
  return d;
}

static double GetPoint3dX(const ON_3dPoint& pt)
{
  return pt.x;
}
static pybind11::dict EncodePoint3d(const ON_3dPoint& pt)
{
  pybind11::dict d;
  d["X"] = pt.x;
  d["Y"] = pt.y;
  d["Z"] = pt.z;
  return d;
}

static pybind11::dict EncodePoint4d(const ON_4dPoint& pt)
{
  pybind11::dict d;
  d["X"] = pt.x;
  d["Y"] = pt.y;
  d["Z"] = pt.z;
  d["W"] = pt.w;
  return d;
}
static pybind11::dict EncodeVector2d(const ON_2dVector& v)
{
  pybind11::dict d;
  d["X"] = v.x;
  d["Y"] = v.y;
  return d;
}
static pybind11::dict EncodeVector3d(const ON_3dVector& v)
{
  pybind11::dict d;
  d["X"] = v.x;
  d["Y"] = v.y;
  d["Z"] = v.z;
  return d;
}

static int ON_3dVectorIsParallelTo(const ON_3dVector& a, const ON_3dVector& b)
{
  return a.IsParallelTo(b);
}
static int ON_3dVectorIsParallelTo2(const ON_3dVector& a, const ON_3dVector& b, double tol)
{
  return a.IsParallelTo(b, tol);
}

static double ON_3dVectorVectorAngle(ON_3dVector a, ON_3dVector b)
{
  if (!a.Unitize() || !b.Unitize())
    return ON_UNSET_VALUE;
  
  //compute dot product
  double dot = a.x * b.x + a.y * b.y + a.z * b.z;
  // remove any "noise"
  if (dot > 1.0) dot = 1.0;
  if (dot < -1.0) dot = -1.0;
  double radians = acos(dot);
  return radians;
}

static double ON_3dVectorVectorAngle2(ON_3dVector a, ON_3dVector b, const BND_Plane& bndPlane)
{
  ON_Plane plane = bndPlane.ToOnPlane();
  { // Project vectors onto plane.
    ON_3dPoint pA = plane.Origin() + a;
    ON_3dPoint pB = plane.Origin() + b;

    pA = plane.ClosestPointTo(pA);
    pB = plane.ClosestPointTo(pB);

    a = pA - plane.Origin();
    b = pB - plane.Origin();
  }

  // Abort on invalid cases.
  if (!a.Unitize()) { return ON_UNSET_VALUE; }
  if (!b.Unitize()) { return ON_UNSET_VALUE; }

  double dot = a * b;
  { // Limit dot product to valid range.
    if (dot >= 1.0)
    {
      dot = 1.0;
    }
    else if (dot < -1.0)
    {
      dot = -1.0;
    }
  }

  double angle = acos(dot);
  { // Special case (anti)parallel vectors.
    if (fabs(angle) < 1e-64) { return 0.0; }
    if (fabs(angle - ON_PI) < 1e-64) { return ON_PI; }
  }

  ON_3dVector cross = ON_3dVector::CrossProduct(a, b);
  if (plane.zaxis.IsParallelTo(cross) == +1)
    return angle;
  return 2.0 * ON_PI - angle;
}

static double ON_3dVectorVectorAngle3(ON_3dVector v1, ON_3dVector v2, ON_3dVector vNormal)
{
  if ((fabs(v1.x - v2.x) < 1e-64) && (fabs(v1.y - v2.y) < 1e-64) && (fabs(v1.z - v2.z) < 1e-64))
    return 0.0;

  double dNumerator = v1 * v2;
  double dDenominator = v1.Length() * v2.Length();

  ON_3dVector vCross = ON_3dVector::CrossProduct(v1, v2);
  vCross.Unitize();

  if ((fabs(vCross.x - 0.0) < 1e-64) && (fabs(vCross.y - 0.0) < 1e-64) && (fabs(vCross.z - 0.0) < 1e-64))
  {
    if ((fabs(dNumerator - 1.0) < 1e-64))
      return 0.0;
    else
      if ((fabs(dNumerator + 1.0) < 1e-64))
        return ON_PI;
  }

  double dDivision = dNumerator / dDenominator;

  if (dDivision > 1.0)
    dDivision = 1.0;
  else
    if (dDivision < -1.0)
      dDivision = -1.0;

  if ((fabs(dDivision + 1.0) < 1e-64))
    return ON_PI;

  double dAngle = acos(dDivision);

  // Check if vCross is parallel or anti parallel to normal vector.
  // If anti parallel Angle = 360 - Angle

  vNormal.Unitize();

  double dDot = vCross * vNormal;

  if ((fabs(dDot + 1.0) < 1e-64))
    dAngle = (ON_PI * 2.0) - dAngle;

  return dAngle;
}


static pybind11::dict EncodePoint2f(const ON_2fPoint& pt)
{
  pybind11::dict d;
  d["X"] = pt.x;
  d["Y"] = pt.y;
  return d;
}
static pybind11::dict EncodePoint3f(const ON_3fPoint& pt)
{
  pybind11::dict d;
  d["X"] = pt.x;
  d["Y"] = pt.y;
  d["Z"] = pt.z;
  return d;
}
static pybind11::dict EncodeVector3f(const ON_3fVector& v)
{
  pybind11::dict d;
  d["X"] = v.x;
  d["Y"] = v.y;
  d["Z"] = v.z;
  return d;
}

static std::string ReprPoint2d(const ON_2dPoint& p)
{
  std::ostringstream repr;
  repr << "Point2d(" << p.x << ", " << p.y << ")";
  return repr.str();
}

static std::string ReprPoint3d(const ON_3dPoint& p)
{
  std::ostringstream repr;
  repr << "Point3d(" << p.x << ", " << p.y << ", " << p.z << ")";
  return repr.str();
}

static std::string ReprPoint4d(const ON_4dPoint& p)
{
  std::ostringstream repr;
  repr << "Point4d(" << p.x << ", " << p.y << ", " << p.z << ", " << p.w << ")";
  return repr.str();
}

static std::string ReprVector2d(const ON_2dVector& v)
{
  std::ostringstream repr;
  repr << "Vector2d(" << v.x << ", " << v.y << ")";
  return repr.str();
}

static std::string ReprVector3d(const ON_3dVector& v)
{
  std::ostringstream repr;
  repr << "Vector3d(" << v.x << ", " << v.y << ", " << v.z << ")";
  return repr.str();
}

static std::string ReprPoint2f(const ON_2fPoint& p)
{
  std::ostringstream repr;
  repr << "Point2f(" << p.x << ", " << p.y << ")";
  return repr.str();
}

static std::string ReprPoint3f(const ON_3fPoint& p)
{
  std::ostringstream repr;
  repr << "Point3f(" << p.x << ", " << p.y << ", " << p.z << ")";
  return repr.str();
}

static std::string ReprVector3f(const ON_3fVector& v)
{
  std::ostringstream repr;
  repr << "Vector3f(" << v.x << ", " << v.y << ", " << v.z << ")";
  return repr.str();
}

static std::string ReprInterval(const BND_Interval& i)
{
  std::ostringstream repr;
  repr << "Interval(" << i.m_t0 << ", " << i.m_t1 << ")";
  return repr.str();
}


namespace py = pybind11;
void initPointBindings(pybind11::module& m)
{
  py::class_<ON_2dPoint>(m, "Point2d")
    .def(py::init<double, double>(), py::arg("x"), py::arg("y"))
    .def("Encode", &EncodePoint2d)
    .def("__repr__", &ReprPoint2d)
    .def_readwrite("X", &ON_2dPoint::x)
    .def_readwrite("Y", &ON_2dPoint::y)
    .def(py::self + py::self)
    .def(py::self == py::self)
    .def(py::self != py::self)
    .def("DistanceTo", &ON_2dPoint::DistanceTo, py::arg("other"));

  py::class_<ON_3dPoint>(m, "Point3d")
    .def(py::init<double, double, double>(), py::arg("x"), py::arg("y"), py::arg("z"))
    .def_property_readonly_static("Unset", &GetUnsetPoint3d)
    .def("Encode", &EncodePoint3d)
    .def("__repr__", &ReprPoint3d)
    .def_readwrite("X", &ON_3dPoint::x)
    .def_readwrite("Y", &ON_3dPoint::y)
    .def_readwrite("Z", &ON_3dPoint::z)
    .def(py::self + py::self)
    .def(py::self * double())
    .def(py::self + ON_3dVector())
    .def(py::self * ON_Xform())
    .def(py::self == py::self)
    .def(py::self != py::self)
    .def("DistanceTo", &ON_3dPoint::DistanceTo, py::arg("other"))
    .def("Transform", &BND_Point3d::Transform, py::arg("xform"));

  py::class_<ON_4dPoint>(m, "Point4d")
    .def(py::init<double, double, double, double>(), py::arg("x"), py::arg("y"), py::arg("z"), py::arg("w"))
    .def("Encode", &EncodePoint4d)
    .def("__repr__", &ReprPoint4d)
    .def_readwrite("X", &ON_4dPoint::x)
    .def_readwrite("Y", &ON_4dPoint::y)
    .def_readwrite("Z", &ON_4dPoint::z)
    .def_readwrite("W", &ON_4dPoint::w)
    .def(py::self == py::self)
    .def(py::self != py::self);

  py::class_<ON_2dVector>(m, "Vector2d")
    .def(py::init<double, double>(), py::arg("x"), py::arg("y"))
    .def("Encode", &EncodeVector2d)
    .def("__repr__", &ReprVector2d)
    .def_readwrite("X", &ON_2dVector::x)
    .def_readwrite("Y", &ON_2dVector::y)
    .def(py::self == py::self)
    .def(py::self != py::self);

  py::class_<ON_3dVector>(m, "Vector3d")
    .def(py::init<double, double, double>(), py::arg("x"), py::arg("y"), py::arg("z"))
    .def("Encode", &EncodeVector3d)
    .def("__repr__", &ReprVector3d)
    .def("IsParallelTo", &ON_3dVectorIsParallelTo, py::arg("other"))
    .def("IsParallelTo", &ON_3dVectorIsParallelTo2, py::arg("other"), py::arg("angleTolerance"))
    .def_static("VectorAngle", &ON_3dVectorVectorAngle, py::arg("a"), py::arg("b"))
    .def_static("VectorAngle", &ON_3dVectorVectorAngle2, py::arg("a"), py::arg("b"), py::arg("plane"))
    .def_static("VectorAngle", &ON_3dVectorVectorAngle3, py::arg("v1"), py::arg("v2"), py::arg("vNormal"))
    .def_readwrite("X", &ON_3dVector::x)
    .def_readwrite("Y", &ON_3dVector::y)
    .def_readwrite("Z", &ON_3dVector::z)
    .def(py::self == py::self)
    .def(py::self != py::self);

  py::class_<ON_3fVector>(m, "Vector3f")
    .def(py::init<float, float, float>(), py::arg("x"), py::arg("y"), py::arg("z"))
    .def("Encode", &EncodeVector3f)
    .def("__repr__", &ReprVector3f)
    .def_readwrite("X", &ON_3fVector::x)
    .def_readwrite("Y", &ON_3fVector::y)
    .def_readwrite("Z", &ON_3fVector::z)
    .def(py::self == py::self)
    .def(py::self != py::self);

  py::class_<ON_2fPoint>(m, "Point2f")
    .def(py::init<float, float>(), py::arg("x"), py::arg("y"))
    .def("Encode", &EncodePoint2f)
    .def("__repr__", &ReprPoint2f)
    .def_readwrite("X", &ON_2fPoint::x)
    .def_readwrite("Y", &ON_2fPoint::y)
    .def(py::self + py::self)
    .def(py::self == py::self)
    .def(py::self != py::self);

  py::class_<ON_3fPoint>(m, "Point3f")
    .def(py::init<float, float, float>(), py::arg("x"), py::arg("y"), py::arg("z"))
    .def("Encode", &EncodePoint3f)
    .def("__repr__", &ReprPoint3f)
    .def_readwrite("X", &ON_3fPoint::x)
    .def_readwrite("Y", &ON_3fPoint::y)
    .def_readwrite("Z", &ON_3fPoint::z)
    .def(py::self + py::self)
    .def(py::self == py::self)
    .def(py::self != py::self);

  py::class_<BND_Interval>(m, "Interval")
    .def(py::init<double, double>(), py::arg("t0"), py::arg("t1"))
    .def_readwrite("T0", &BND_Interval::m_t0)
    .def_readwrite("T1", &BND_Interval::m_t1)
    .def(py::self == py::self)
    .def(py::self != py::self)
    .def("__repr__", &ReprInterval);

}
#else
using namespace emscripten;

void initPointBindings(void*)
{
  value_array<ON_2dPoint>("Point2dSimple")
    .element(&ON_2dPoint::x)
    .element(&ON_2dPoint::y);

  value_array<ON_3dPoint>("Point3dSimple")
    .element(&ON_3dPoint::x)
    .element(&ON_3dPoint::y)
    .element(&ON_3dPoint::z);

  class_<BND_Point3d>("Point3d")
    .class_function("transform", &BND_Point3d::Transform);

  value_array<ON_4dPoint>("Point4dSimple")
    .element(&ON_4dPoint::x)
    .element(&ON_4dPoint::y)
    .element(&ON_4dPoint::z)
    .element(&ON_4dPoint::w);

  value_array<ON_3fVector>("Vector3fSimple")
    .element(&ON_3fVector::x)
    .element(&ON_3fVector::y)
    .element(&ON_3fVector::z);

  value_array<ON_3dVector>("Vector3dSimple")
    .element(&ON_3dVector::x)
    .element(&ON_3dVector::y)
    .element(&ON_3dVector::z);

  value_array<ON_2dVector>("Vector2dSimple")
    .element(&ON_2dVector::x)
    .element(&ON_2dVector::y);

  value_array<ON_2fPoint>("Point2fSimple")
    .element(&ON_2fPoint::x)
    .element(&ON_2fPoint::y);

  value_array<ON_3fPoint>("Point3fSimple")
    .element(&ON_3fPoint::x)
    .element(&ON_3fPoint::y)
    .element(&ON_3fPoint::z);

  value_array<BND_Interval>("IntervalSimple")
    .element(&BND_Interval::m_t0)
    .element(&BND_Interval::m_t1);
}
#endif
