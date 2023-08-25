#include "bindings.h"

BND_TUPLE BND_Intersection::LineLine(const ON_Line& lineA, const ON_Line& lineB)
{
  double a = 0;
  double b = 0;
  bool success = ON_Intersect(lineA, lineB, &a, &b);
  BND_TUPLE rc = CreateTuple(3);
  SetTuple(rc, 0, success);
  SetTuple(rc, 1, a);
  SetTuple(rc, 2, b);
  return rc;
}

BND_TUPLE BND_Intersection::LineLine2(const ON_Line& lineA, const ON_Line& lineB, double tolerance, bool finiteSegments)
{
  double a = 0;
  double b = 0;
  bool success = ON_Intersect(lineA, lineB, &a, &b);
  if (success)
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
      success = (lineA.PointAt(a).DistanceTo(lineB.PointAt(b)) <= tolerance);
    }
  }

  BND_TUPLE rc = CreateTuple(3);
  SetTuple(rc, 0, success);
  SetTuple(rc, 1, a);
  SetTuple(rc, 2, b);
  return rc;
}


BND_TUPLE BND_Intersection::LinePlane(const ON_Line& line, const BND_Plane& plane)
{
  double a = 0;
  bool success = ON_Intersect(line, plane.ToOnPlane(), &a);
  BND_TUPLE rc = CreateTuple(2);
  SetTuple(rc, 0, success);
  SetTuple(rc, 1, a);
  return rc;
}

BND_TUPLE BND_Intersection::PlanePlane(const BND_Plane& planeA, const BND_Plane& planeB)
{
  ON_Line line;
  bool success = ON_Intersect(planeA.ToOnPlane(), planeB.ToOnPlane(), line);
  BND_TUPLE rc = CreateTuple(2);
  SetTuple(rc, 0, success);
  SetTuple(rc, 1, line);
  return rc;
}

BND_TUPLE BND_Intersection::PlanePlanePlane(const BND_Plane& planeA, const BND_Plane& planeB, const BND_Plane& planeC)
{
  ON_3dPoint point;
  bool success = ON_Intersect(planeA.ToOnPlane(), planeB.ToOnPlane(), planeC.ToOnPlane(), point);

  BND_TUPLE rc = CreateTuple(2);
  SetTuple(rc, 0, success);
  SetTuple(rc, 1, point);
  return rc;
}

enum class PlaneCircleIntersection : int
{
  None = 0,
  Tangent = 1,
  Secant = 2,
  Parallel = 3,
  Coincident = 4
};

enum class PlaneSphereIntersection : int
{
  None = 0,
  Point = 1,
  Circle = 2,
};

enum class LineCircleIntersection : int
{
  None = 0,
  Single = 1,
  Multiple = 2,
};

enum class LineSphereIntersection : int
{
  None = 0,
  Single = 1,
  Multiple = 2,
};

enum class LineCylinderIntersection : int
{
  None = 0,
  Single = 1,
  Multiple = 2,
  Overlap = 3
};

enum class SphereSphereIntersection : int
{
  None = 0,
  Point = 1,
  Circle = 2,
  Overlap = 3
};

BND_TUPLE BND_Intersection::PlaneSphere(const BND_Plane& plane, const BND_Sphere& sphere)
{
  ON_Circle circle;
  int success = ON_Intersect(plane.ToOnPlane(), sphere.m_sphere, circle);
  BND_TUPLE rc = CreateTuple(2);
  SetTuple(rc, 0, (PlaneSphereIntersection)success);
  SetTuple(rc, 1, BND_Circle(circle));
  return rc;
}

BND_TUPLE BND_Intersection::LineCircle(const ON_Line& line, const BND_Circle& circle)
{
  double t1 = 0.0;
  double t2 = 0.0;
  ON_3dPoint point1(0, 0, 0);
  ON_3dPoint point2(0, 0, 0);

  if (!line.IsValid() || !circle.IsValid())
  {
    BND_TUPLE rc = CreateTuple(5);
    SetTuple(rc, 0, LineCircleIntersection::None);
    SetTuple(rc, 1, 0.0);
    SetTuple(rc, 2, ON_3dPoint(0, 0, 0));
    SetTuple(rc, 3, 0.0);
    SetTuple(rc, 4, ON_3dPoint(0, 0, 0));
    return rc;
  }

  int success = ::ON_Intersect(line, circle.m_circle, &t1, point1, &t2, point2);
  BND_TUPLE rc = CreateTuple(5);
  SetTuple(rc, 0, (LineCircleIntersection)success);
  SetTuple(rc, 1, t1);
  SetTuple(rc, 2, point1);
  SetTuple(rc, 3, t2);
  SetTuple(rc, 4, point2);
  return rc;
}

BND_TUPLE BND_Intersection::LineSphere(const ON_Line& line, const BND_Sphere& sphere)
{
  ON_3dPoint point1(0, 0, 0);
  ON_3dPoint point2(0, 0, 0);
  int success = ::ON_Intersect(line, sphere.m_sphere, point1, point2);
  BND_TUPLE rc = CreateTuple(3);
  SetTuple(rc, 0, (LineSphereIntersection)success);
  SetTuple(rc, 1, point1);
  SetTuple(rc, 2, point2);
  return rc;
}

BND_TUPLE BND_Intersection::LineCylinder(const ON_Line& line, const BND_Cylinder& cylinder)
{
  ON_3dPoint point1(0, 0, 0);
  ON_3dPoint point2(0, 0, 0);
  int success = ::ON_Intersect(line, cylinder.m_cylinder, point1, point2);
  BND_TUPLE rc = CreateTuple(3);
  SetTuple(rc, 0, (LineCylinderIntersection)success);
  SetTuple(rc, 1, point1);
  SetTuple(rc, 2, point2);
  return rc;
}

BND_TUPLE BND_Intersection::SphereSphere(const BND_Sphere& sphereA, const BND_Sphere& sphereB)
{
  ON_Circle circle;
  int success = ::ON_Intersect(sphereA.m_sphere, sphereB.m_sphere, circle);
  BND_TUPLE rc = CreateTuple(2);
  if (success <= 0 || success > 3)
    SetTuple(rc, 0, SphereSphereIntersection::None);
  else
    SetTuple(rc, 0, (SphereSphereIntersection)success);
  SetTuple(rc, 1, BND_Circle(circle));
  return rc;
}

BND_TUPLE BND_Intersection::LineBox(const ON_Line& line, const BND_BoundingBox& box, double tolerance)
{
  ON_Interval i = ON_Interval::EmptyInterval;
  bool success = ::ON_Intersect(box.m_bbox, line, tolerance, &i);
  BND_TUPLE rc = CreateTuple(2);
  SetTuple(rc, 0, success);
  SetTuple(rc, 1, BND_Interval(i));
  return rc;
}


//////////////////////////////////////////////////////////////////////////////

#if defined(ON_PYTHON_COMPILE)
namespace py = pybind11;
void initIntersectBindings(pybind11::module& m)
{
  //py::enum_<PlaneCircleIntersection>(m, "PlaneCircleIntersection")
  //  .value("None", PlaneCircleIntersection::None)
  //  .value("Tangent", PlaneCircleIntersection::Tangent)
  //  .value("Secant", PlaneCircleIntersection::Secant)
  //  .value("Parallel", PlaneCircleIntersection::Parallel)
  //  .value("Coincident", PlaneCircleIntersection::Coincident)
  //  ;

  py::enum_<PlaneSphereIntersection>(m, "PlaneSphereIntersection")
    .value("None", PlaneSphereIntersection::None)
    .value("Point", PlaneSphereIntersection::Point)
    .value("Circle", PlaneSphereIntersection::Circle)
    ;

  py::enum_<LineCircleIntersection>(m, "LineCircleIntersection")
    .value("None", LineCircleIntersection::None)
    .value("Single", LineCircleIntersection::Single)
    .value("Multiple", LineCircleIntersection::Multiple)
    ;

  py::enum_<LineSphereIntersection>(m, "LineSphereIntersection")
    .value("None", LineSphereIntersection::None)
    .value("Single", LineSphereIntersection::Single)
    .value("Multiple", LineSphereIntersection::Multiple)
    ;

  py::enum_<LineCylinderIntersection>(m, "LineCylinderIntersection")
    .value("None", LineCylinderIntersection::None)
    .value("Single", LineCylinderIntersection::Single)
    .value("Multiple", LineCylinderIntersection::Multiple)
    .value("Overlap", LineCylinderIntersection::Overlap)
    ;

  py::enum_<SphereSphereIntersection>(m, "SphereSphereIntersection")
    .value("None", SphereSphereIntersection::None)
    .value("Point", SphereSphereIntersection::Point)
    .value("Circle", SphereSphereIntersection::Circle)
    .value("Overlap", SphereSphereIntersection::Overlap)
    ;

  py::class_<BND_Intersection>(m, "Intersection")
    .def_static("LineLine", &BND_Intersection::LineLine, py::arg("lineA"), py::arg("lineB"))
    .def_static("LineLine", &BND_Intersection::LineLine2, py::arg("lineA"), py::arg("lineB"), py::arg("tolerance"), py::arg("finiteSegments"))
    .def_static("LinePlane", &BND_Intersection::LinePlane, py::arg("line"), py::arg("plane"))
    .def_static("PlanePlane", &BND_Intersection::PlanePlane, py::arg("planeA"), py::arg("planeB"))
    .def_static("PlanePlanePlane", &BND_Intersection::PlanePlanePlane, py::arg("planeA"), py::arg("planeB"), py::arg("planeC"))
    .def_static("PlaneSphere", &BND_Intersection::PlaneSphere, py::arg("plane"), py::arg("sphere"))
    .def_static("LineCircle", &BND_Intersection::LineCircle, py::arg("line"), py::arg("circle"))
    .def_static("LineSphere", &BND_Intersection::LineSphere, py::arg("line"), py::arg("sphere"))
    .def_static("LineCylinder", &BND_Intersection::LineCylinder, py::arg("line"), py::arg("cylinder"))
    .def_static("SphereSphere", &BND_Intersection::SphereSphere, py::arg("sphereA"), py::arg("sphereB"))
    .def_static("LineBox", &BND_Intersection::LineBox, py::arg("line"), py::arg("box"), py::arg("tolerance"))
    ;

}
#endif

#if defined(ON_WASM_COMPILE)
using namespace emscripten;

void initIntersectBindings(void*)
{
  //enum_<PlaneCircleIntersection>("PlaneCircleIntersection")
  //  .value("None", PlaneCircleIntersection::None)
  //  .value("Tangent", PlaneCircleIntersection::Tangent)
  //  .value("Secant", PlaneCircleIntersection::Secant)
  //  .value("Parallel", PlaneCircleIntersection::Parallel)
  //  .value("Coincident", PlaneCircleIntersection::Coincident)
  //  ;

  enum_<PlaneSphereIntersection>("PlaneSphereIntersection")
    .value("None", PlaneSphereIntersection::None)
    .value("Point", PlaneSphereIntersection::Point)
    .value("Circle", PlaneSphereIntersection::Circle)
    ;

  enum_<LineCircleIntersection>("LineCircleIntersection")
    .value("None", LineCircleIntersection::None)
    .value("Single", LineCircleIntersection::Single)
    .value("Multiple", LineCircleIntersection::Multiple)
    ;

  enum_<LineSphereIntersection>("LineSphereIntersection")
    .value("None", LineSphereIntersection::None)
    .value("Single", LineSphereIntersection::Single)
    .value("Multiple", LineSphereIntersection::Multiple)
    ;

  enum_<LineCylinderIntersection>("LineCylinderIntersection")
    .value("None", LineCylinderIntersection::None)
    .value("Single", LineCylinderIntersection::Single)
    .value("Multiple", LineCylinderIntersection::Multiple)
    .value("Overlap", LineCylinderIntersection::Overlap)
    ;

  enum_<SphereSphereIntersection>("SphereSphereIntersection")
    .value("None", SphereSphereIntersection::None)
    .value("Point", SphereSphereIntersection::Point)
    .value("Circle", SphereSphereIntersection::Circle)
    .value("Overlap", SphereSphereIntersection::Overlap)
    ;

  class_<BND_Intersection>("Intersection")
    .class_function("lineLine", &BND_Intersection::LineLine)
    .class_function("lineLineTolerance", &BND_Intersection::LineLine2)
    .class_function("linePlane", &BND_Intersection::LinePlane)
    .class_function("planePlane", &BND_Intersection::PlanePlane)
    .class_function("planePlanePlane", &BND_Intersection::PlanePlanePlane)
    .class_function("planeSphere", &BND_Intersection::PlaneSphere)
    .class_function("lineCircle", &BND_Intersection::LineCircle)
    .class_function("lineSphere", &BND_Intersection::LineSphere)
    .class_function("lineCylinder", &BND_Intersection::LineCylinder)
    .class_function("sphereSphere", &BND_Intersection::SphereSphere)
    .class_function("lineBox", &BND_Intersection::LineBox)
    ;
}
#endif
