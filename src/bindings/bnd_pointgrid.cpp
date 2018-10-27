#include "bindings.h"

BND_PointGrid::BND_PointGrid()
{
  SetTrackedPointer(new ON_PointGrid(), nullptr);
}

BND_PointGrid::BND_PointGrid(ON_PointGrid* pointgrid, const ON_ModelComponentReference* compref)
{
  SetTrackedPointer(pointgrid, compref);
}

void BND_PointGrid::SetTrackedPointer(ON_PointGrid* pointgrid, const ON_ModelComponentReference* compref)
{
  m_pointgrid = pointgrid;
  BND_GeometryBase::SetTrackedPointer(pointgrid, compref);
}

//////////////////////////////////////////////////////////////////////////////

#if defined(ON_PYTHON_COMPILE)
namespace py = pybind11;
void initPointGridBindings(pybind11::module& m)
{
  py::class_<BND_PointGrid, BND_GeometryBase>(m, "PointGrid")
    .def(py::init<>())
    ;
}
#endif

#if defined(ON_WASM_COMPILE)
using namespace emscripten;

void initPointGridBindings(void*)
{
  class_<BND_PointGrid, base<BND_GeometryBase>>("PointGrid")
    .constructor<>()
    ;
}
#endif
