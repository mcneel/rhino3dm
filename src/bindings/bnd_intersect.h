#include "bindings.h"

#pragma once

#if defined(ON_PYTHON_COMPILE)
#if defined(NANOBIND)
namespace py = nanobind;
void initIntersectBindings(py::module_& m);
#else
namespace py = pybind11;
void initIntersectBindings(py::module& m);
#endif

#else
void initIntersectBindings(void* m);
#endif

class BND_Intersection
{
public:
  static BND_TUPLE LineLine(const ON_Line& lineA, const ON_Line& lineB);
  static BND_TUPLE LineLine2(const ON_Line& lineA, const ON_Line& lineB, double tolerance, bool finiteSegments);
  static BND_TUPLE LinePlane(const ON_Line& line, const class BND_Plane& plane);
  static BND_TUPLE PlanePlane(const class BND_Plane& planeA, const class BND_Plane& planeB);
  static BND_TUPLE PlanePlanePlane(const class BND_Plane& planeA, const class BND_Plane& planeB, const class BND_Plane& planeC);
  //static BND_TUPLE PlaneCircle(const class BND_Plane& plane, const class BND_Circle& circle);
  static BND_TUPLE PlaneSphere(const class BND_Plane& plane, const class BND_Sphere& sphere);
  static BND_TUPLE LineCircle(const ON_Line& line, const class BND_Circle& circle);
  static BND_TUPLE LineSphere(const ON_Line& line, const class BND_Sphere& sphere);
  static BND_TUPLE LineCylinder(const ON_Line& line, const class BND_Cylinder& cylinder);
  static BND_TUPLE SphereSphere(const class BND_Sphere& sphereA, const class BND_Sphere& sphereB);
  static BND_TUPLE LineBox(const ON_Line& line, const class BND_BoundingBox& box, double tolerance);
  //public static bool LineBox(Line line, Box box, double tolerance, out Interval lineParameters)

};
