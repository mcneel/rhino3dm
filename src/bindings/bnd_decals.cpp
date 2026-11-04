#include "bnd_decals.h"

int BND_Decals_Count(const BND_Decals& decals)
{
  // the same walk as the generated enumerator (BND_DecalsIter): a snapshot, visible ones counted
  BND_DecalsIter it(decals.m_ptr);
  int n = 0;
  for (BND_Decal* d = it.Gen_next(); d != nullptr; d = it.Gen_next())
  {
    n++;
    delete d;
  }
  return n;
}
