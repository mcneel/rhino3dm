#include "bindings.h"

#pragma once

#if defined(ON_PYTHON_COMPILE)
void initPointCloudBindings(pybind11::module& m);
#else
void initPointCloudBindings(void* m);
#endif

class BND_PointCloud : public BND_GeometryBase
{
  ON_PointCloud* m_pointcloud = nullptr;
protected:
  void SetTrackedPointer(ON_PointCloud* pointcloud, const ON_ModelComponentReference* compref);

public:
  BND_PointCloud();
  BND_PointCloud(ON_PointCloud* pointcloud, const ON_ModelComponentReference* compref);
};
