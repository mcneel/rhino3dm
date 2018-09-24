#include "bindings.h"

BND_Brep::BND_Brep()
{
  m_brep.reset(new ON_Brep());
  SetSharedGeometryPointer(m_brep);
}

BND_Brep::BND_Brep(ON_Brep* brep)
{
  m_brep.reset(brep);
  SetSharedGeometryPointer(m_brep);
}
