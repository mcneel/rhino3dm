#include "stdafx.h"

#if !defined(RHINO3DM_BUILD) // only available in rhino.exe

RH_C_FUNCTION ON_SimpleArray<ON_ClashEvent>* ON_SimpleArray_ClashEvent_New()
{
  return new ON_SimpleArray<ON_ClashEvent>();
}

RH_C_FUNCTION void ON_SimpleArray_ClashEvent_Delete(ON_SimpleArray<ON_ClashEvent>* pArray)
{
  if( pArray )
    delete pArray;
}

RH_C_FUNCTION void ON_SimpleArray_ClashEvent_GetEvent(ON_SimpleArray<ON_ClashEvent>* pArray, int index, int* index0, int* index1, ON_3dPoint* point)
{
  if( pArray && index>=0 && index<pArray->Count() && index0 && index1 && point )
  {
    const ON_ClashEvent& clash = (*pArray)[index];
    *index0 = (int)(clash.m_i[0]);
    *index1 = (int)(clash.m_i[1]);
    *point = clash.m_P;
  }
}

RH_C_FUNCTION int ONC_MeshClashSearch(const ON_SimpleArray<const ON_Mesh*>* pMeshesA, const ON_SimpleArray<const ON_Mesh*>* pMeshesB, double distance, int maxEvents, bool multithread, ON_SimpleArray<ON_ClashEvent>* pClashArray)
{
  int rc = 0;
  if( pMeshesA && pMeshesB && pClashArray )
  {
    int countA = pMeshesA->Count();
    const ON_Mesh* const* a = pMeshesA->Array();
    int countB = pMeshesB->Count();
    const ON_Mesh* const* b = pMeshesB->Array();
    rc = ::ON_MeshClashSearch(countA, a, countB, b, distance, multithread, maxEvents, *pClashArray);
  }
  return rc;
}

RH_C_FUNCTION int ON_Mesh_GetClashingFacePairs(const ON_Mesh* pConstMesh, int maxPairCount, ON_SimpleArray<ON_2dex>* pClashingPairs)
{
  int rc = 0;
  if (nullptr != pConstMesh && nullptr != pClashingPairs)
    rc = pConstMesh->GetClashingFacePairs(maxPairCount, *pClashingPairs);
  return rc;
}


RH_C_FUNCTION void RHC_ON_FPU_ClearExceptionStatus()
{
  ON_FPU_ClearExceptionStatus();
}

RH_C_FUNCTION void RHC_ON_FPU_Init()
{
  ON_FPU_Init();
}

#endif
