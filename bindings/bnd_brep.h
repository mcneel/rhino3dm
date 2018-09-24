#include "bindings.h"

#pragma once

class BND_Brep : public BND_Geometry
{
  std::shared_ptr<ON_Brep> m_brep;
public:
  BND_Brep();
  BND_Brep(ON_Brep* brep);
};
