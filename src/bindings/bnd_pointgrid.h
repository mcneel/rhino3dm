#include "bindings.h"

#pragma once

#if defined(ON_PYTHON_COMPILE)
void initPointGridBindings(rh3dmpymodule& m);
#else
void initPointGridBindings(void* m);
#endif

class BND_PointGrid : public BND_GeometryBase
{
  ON_PointGrid* m_pointgrid = nullptr;
protected:
  void SetTrackedPointer(ON_PointGrid* pointgrid, const ON_ModelComponentReference* compref);

public:
  BND_PointGrid();
  BND_PointGrid(ON_PointGrid* pointgrid, const ON_ModelComponentReference* compref);
};
