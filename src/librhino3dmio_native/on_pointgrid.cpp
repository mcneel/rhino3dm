#include "stdafx.h"

RH_C_FUNCTION ON_PointGrid* ON_PointGrid_New(int rows, int columns)
{
  return new ON_PointGrid(rows, columns);
}
