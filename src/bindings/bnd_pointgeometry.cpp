#include "bindings.h"

BND_Point::BND_Point()
{
  SetTrackedPointer(new ON_Point(), nullptr);
}

BND_Point::BND_Point(ON_Point* point, const ON_ModelComponentReference* compref)
{
  SetTrackedPointer(point, compref);
}

void BND_Point::SetTrackedPointer(ON_Point* point, const ON_ModelComponentReference* compref)
{
  m_point = point;
  BND_GeometryBase::SetTrackedPointer(point, compref);
}

//////////////////////////////////////////////////////////////////////////////

#if defined(ON_PYTHON_COMPILE)
namespace py = pybind11;
void initPointGeometryBindings(pybind11::module& m)
{
  py::class_<BND_Point, BND_GeometryBase>(m, "Point")
    .def(py::init<>())
    ;
}
#endif

#if defined(ON_WASM_COMPILE)
using namespace emscripten;

void initPointGeometryBindings(void*)
{
  class_<BND_Point, base<BND_GeometryBase>>("Point")
    .constructor<>()
    ;
}
#endif
