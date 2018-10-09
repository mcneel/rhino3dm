#include "bindings.h"

BND_NurbsCurve::BND_NurbsCurve(ON_NurbsCurve* nurbscurve, const ON_ModelComponentReference* compref)
{
  SetTrackedPointer(nurbscurve, compref);
}

BND_NurbsCurve::BND_NurbsCurve(int degree, int pointCount)
{
  ON_NurbsCurve* nurbscurve = ON_NurbsCurve::New(3, false, degree+1, pointCount);
  SetTrackedPointer(nurbscurve, nullptr);
}

BND_NurbsCurve::BND_NurbsCurve(int dimension, bool rational, int order, int pointCount)
{
  ON_NurbsCurve* nurbscurve = ON_NurbsCurve::New(dimension, rational, order, pointCount);
  SetTrackedPointer(nurbscurve, nullptr);
}

void BND_NurbsCurve::SetTrackedPointer(ON_NurbsCurve* nurbscurve, const ON_ModelComponentReference* compref)
{
  m_nurbscurve = nurbscurve;
  BND_Curve::SetTrackedPointer(nurbscurve, compref);
}

///////////////////////////////////////////////////////////////////////////////////////////

#if defined(ON_PYTHON_COMPILE)
namespace py = pybind11;
void initNurbsCurveBindings(pybind11::module& m)
{
  py::class_<BND_NurbsCurve, BND_Curve>(m, "NurbsCurve")
    .def(py::init<int, int>())
    .def(py::init<int, bool, int, int>());
}
#endif
