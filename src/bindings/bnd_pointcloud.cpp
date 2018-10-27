#include "bindings.h"

BND_PointCloud::BND_PointCloud()
{
  SetTrackedPointer(new ON_PointCloud(), nullptr);
}

BND_PointCloud::BND_PointCloud(ON_PointCloud* pointcloud, const ON_ModelComponentReference* compref)
{
  SetTrackedPointer(pointcloud, compref);
}

void BND_PointCloud::SetTrackedPointer(ON_PointCloud* pointcloud, const ON_ModelComponentReference* compref)
{
  m_pointcloud = pointcloud;
  BND_GeometryBase::SetTrackedPointer(pointcloud, compref);
}

//////////////////////////////////////////////////////////////////////////////

#if defined(ON_PYTHON_COMPILE)
namespace py = pybind11;
void initPointCloudBindings(pybind11::module& m)
{
  py::class_<BND_PointCloud, BND_GeometryBase>(m, "PointCloud")
    .def(py::init<>())
    ;
}
#endif

#if defined(ON_WASM_COMPILE)
using namespace emscripten;

void initPointCloudBindings(void*)
{
  class_<BND_PointCloud, base<BND_GeometryBase>>("PointCloud")
    .constructor<>()
    ;
}
#endif
