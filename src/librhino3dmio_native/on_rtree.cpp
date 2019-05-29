#include "stdafx.h"


RH_C_FUNCTION ON_RTree* ON_RTree_New()
{
  return new ON_RTree();
}

RH_C_FUNCTION void ON_RTree_Delete(ON_RTree* pTree)
{
  if( pTree )
    delete pTree;
}

RH_C_FUNCTION bool ON_RTree_CreateMeshFaceTree(ON_RTree* pTree, const ON_Mesh* pConstMesh)
{
  bool rc = false;
  if( pTree && pConstMesh )
    rc = pTree->CreateMeshFaceTree(pConstMesh);
  return rc;
}

RH_C_FUNCTION ON_RTree* ON_RTree_CreatePointCloudTree(const ON_PointCloud* pConstCloud)
{
  if( pConstCloud )
  {
    int count = pConstCloud->m_P.Count();
    ON_RTree* pTree = new ON_RTree(count);
    for( int i=0; i<count; i++ )
    {
      const ON_3dPoint& pt = pConstCloud->m_P[i];
      if (!pTree->Insert(&(pt.x), &(pt.x), i))
      {
        delete pTree;
        return nullptr;
      }
    }
    return pTree;
  }
  return nullptr;
}

RH_C_FUNCTION ON_RTree* ON_RTree_CreatePointArrayTree(/*ARRAY*/const ON_3dPoint* pConstPoints, int count)
{
  ON_RTree* pTree = nullptr;
  if (pConstPoints && count >= 0)
  {
    pTree = new ON_RTree(count);
    for (int i = 0; i<count; i++)
    {
      const ON_3dPoint& pt = pConstPoints[i];
      if (!pTree->Insert(&(pt.x), &(pt.x), i))
      {
        delete pTree;
        pTree = nullptr;
        break;
      }
    }
  }
  return pTree;
}

struct ON_RTreeSearchContext
{
  int m_serial_number;
  int m_mode; //0=none, 1=bbox, 2=sphere, 3=capsule
  ON_RTreeBBox m_bbox;
  ON_RTreeSphere m_sphere;
  // ON_RTreeCapsule m_capsule; // not using yet
};

RH_C_FUNCTION bool ON_RTreeSearchContext_GetBoundingBox(const ON_RTreeSearchContext* pConstContext, ON_3dPoint* p0, ON_3dPoint* p1)
{
  bool rc = false;
  if( pConstContext && 1==pConstContext->m_mode && p0 && p1 )
  {
    p0->Set(pConstContext->m_bbox.m_min[0], pConstContext->m_bbox.m_min[1], pConstContext->m_bbox.m_min[2]);
    p1->Set(pConstContext->m_bbox.m_max[0], pConstContext->m_bbox.m_max[1], pConstContext->m_bbox.m_max[2]);
    rc = true;
  }
  return rc;
}

RH_C_FUNCTION bool ON_RTreeSearchContext_SetBoundingBox(ON_RTreeSearchContext* pContext, ON_3DPOINT_STRUCT min_pt, ON_3DPOINT_STRUCT max_pt)
{
  bool rc = false;
  if( pContext && 1==pContext->m_mode )
  {
    pContext->m_bbox.m_min[0] = min_pt.val[0];
    pContext->m_bbox.m_min[1] = min_pt.val[1];
    pContext->m_bbox.m_min[2] = min_pt.val[2];
    pContext->m_bbox.m_max[0] = max_pt.val[0];
    pContext->m_bbox.m_max[1] = max_pt.val[1];
    pContext->m_bbox.m_max[2] = max_pt.val[2];
    rc = true;
  }
  return rc;
}

RH_C_FUNCTION bool ON_RTreeSearchContext_GetSphere(const ON_RTreeSearchContext* pConstContext, ON_3dPoint* center, double* radius)
{
  bool rc = false;
  if( pConstContext && 2==pConstContext->m_mode && center && radius )
  {
    center->Set(pConstContext->m_sphere.m_point[0], pConstContext->m_sphere.m_point[1], pConstContext->m_sphere.m_point[2]);
    *radius = pConstContext->m_sphere.m_radius;
    rc = true;
  }
  return rc;
}

RH_C_FUNCTION bool ON_RTreeSearchContext_SetSphere(ON_RTreeSearchContext* pContext, ON_3DPOINT_STRUCT center, double radius)
{
  bool rc = false;
  if( pContext && 2==pContext->m_mode )
  {
    pContext->m_sphere.m_point[0] = center.val[0];
    pContext->m_sphere.m_point[1] = center.val[1];
    pContext->m_sphere.m_point[2] = center.val[2];
    pContext->m_sphere.m_radius = radius;
    rc = true;
  }
  return rc;
}

typedef int (CALLBACK* RTREESEARCHPROC)(int serial_number, void* idA, void* idB, ON_RTreeSearchContext* pSearchContext);

// THIS IS NOT THREAD SAFE Dale Lear 2014-09-05
////static RTREESEARCHPROC g_theRTreeSearcher = NULL;

// THIS IS NOT THREAD SAFE Dale Lear 2014-09-05
////static bool RhCmnTreeSearch1(void* context, ON__INT_PTR a_id)
////{
////  bool rc = false;
////  if( g_theRTreeSearcher )
////  {
////    ON_RTreeSearchContext* pContext = (ON_RTreeSearchContext*)(context);
////    int cbrc = g_theRTreeSearcher(pContext->m_serial_number, (void*)a_id, 0, pContext);
////    rc = cbrc?true:false;
////  }
////  return rc;
////}

// THIS IS NOT THREAD SAFE Dale Lear 2014-09-05
////static bool RhCmnTreeSearch2(void* context, ON__INT_PTR a_id, ON__INT_PTR b_id)
////{
////  bool rc = false;
////  if( g_theRTreeSearcher )
////  {
////    // The "void* context" parameter is used to pass
////    // the 4 byte int serial_number parameter in ON_RTree_Search2().
////    ON__INT_PTR icontext = (ON__INT_PTR)context;
////    int serial_number = static_cast<int>(icontext);
////    int cbrc = g_theRTreeSearcher(serial_number, (void*)a_id, (void*)b_id, NULL);
////    
////    rc = cbrc?true:false;
////  }
////  return rc;
////}

struct RhCmnTreeSearch1_thread_safe_context
{
  RTREESEARCHPROC m_searchCallback;
  ON_RTreeSearchContext m_last_searchCallback_parameter;
};

static bool RhCmnTreeSearch1_thread_safe(void* context, ON__INT_PTR a_id)
{
  // THIS IS THREAD SAFE Dale Lear 2014-09-05
  bool rc = false;
  struct RhCmnTreeSearch1_thread_safe_context* thread_safe_context = static_cast<struct RhCmnTreeSearch1_thread_safe_context*>(context);
  if (nullptr != thread_safe_context && nullptr != thread_safe_context->m_searchCallback)
  {
    int cbrc = thread_safe_context->m_searchCallback(
      thread_safe_context->m_last_searchCallback_parameter.m_serial_number, 
      (void*)a_id, 
      nullptr,
      &thread_safe_context->m_last_searchCallback_parameter
      );
    rc = cbrc ? true : false;
  }
  return rc;
}

struct RhCmnTreeSearch2_thread_safe_context
{
  RTREESEARCHPROC m_searchCallback;
  ON__INT_PTR m_first_searchCallback_parameter;
};

static bool RhCmnTreeSearch2_thread_safe(void* context, ON__INT_PTR a_id, ON__INT_PTR b_id)
{
  // THIS IS THREAD SAFE Dale Lear 2014-09-05
  bool rc = false;
  struct RhCmnTreeSearch2_thread_safe_context* thread_safe_context = static_cast<struct RhCmnTreeSearch2_thread_safe_context*>(context);
  if (nullptr != thread_safe_context && nullptr != thread_safe_context->m_searchCallback)
  {
    int serial_number = static_cast<int>(thread_safe_context->m_first_searchCallback_parameter);
    int cbrc = thread_safe_context->m_searchCallback(serial_number, (void*)a_id, (void*)b_id, nullptr);
    rc = cbrc ? true : false;
  }
  return rc;
}

RH_C_FUNCTION bool ON_RTree_Search(const ON_RTree* pConstTree, ON_3DPOINT_STRUCT pt0, ON_3DPOINT_STRUCT pt1, int serial_number, RTREESEARCHPROC searchCB)
{
  bool rc = false;
  if( pConstTree && searchCB )
  {
    RhCmnTreeSearch1_thread_safe_context context;
    context.m_searchCallback = searchCB;
    context.m_last_searchCallback_parameter.m_mode = 1;
    context.m_last_searchCallback_parameter.m_serial_number = serial_number;
    ON_BoundingBox bbox(ON_3dPoint(pt0.val), ON_3dPoint(pt1.val));
    context.m_last_searchCallback_parameter.m_bbox.m_min[0] = bbox.m_min[0];
    context.m_last_searchCallback_parameter.m_bbox.m_min[1] = bbox.m_min[1];
    context.m_last_searchCallback_parameter.m_bbox.m_min[2] = bbox.m_min[2];
    context.m_last_searchCallback_parameter.m_bbox.m_max[0] = bbox.m_max[0];
    context.m_last_searchCallback_parameter.m_bbox.m_max[1] = bbox.m_max[1];
    context.m_last_searchCallback_parameter.m_bbox.m_max[2] = bbox.m_max[2];
    rc = pConstTree->Search(&(context.m_last_searchCallback_parameter.m_bbox), RhCmnTreeSearch1_thread_safe, &context);
  }
  return rc;
}

RH_C_FUNCTION bool ON_RTree_SearchSphere(const ON_RTree* pConstTree, ON_3DPOINT_STRUCT center, double radius, int serial_number, RTREESEARCHPROC searchCB)
{
  bool rc = false;
  if( pConstTree && searchCB )
  {
    RhCmnTreeSearch1_thread_safe_context context;
    context.m_searchCallback = searchCB;
    context.m_last_searchCallback_parameter.m_mode = 2;
    context.m_last_searchCallback_parameter.m_serial_number = serial_number;
    context.m_last_searchCallback_parameter.m_sphere.m_point[0] = center.val[0];
    context.m_last_searchCallback_parameter.m_sphere.m_point[1] = center.val[1];
    context.m_last_searchCallback_parameter.m_sphere.m_point[2] = center.val[2];
    context.m_last_searchCallback_parameter.m_sphere.m_radius = radius;
    rc = pConstTree->Search(&(context.m_last_searchCallback_parameter.m_sphere), RhCmnTreeSearch1_thread_safe, &context);
  }
  return rc;
}

RH_C_FUNCTION bool ON_RTree_Search2(const ON_RTree* pConstTreeA, const ON_RTree* pConstTreeB, double tolerance, int serial_number, RTREESEARCHPROC searchCB)
{
  bool rc = false;
  if (nullptr != pConstTreeA && nullptr != pConstTreeB && nullptr != searchCB)
  {
    struct RhCmnTreeSearch2_thread_safe_context thread_safe_context;
    thread_safe_context.m_searchCallback = searchCB;
    thread_safe_context.m_first_searchCallback_parameter = serial_number;
    rc = ON_RTree::Search(*pConstTreeA, *pConstTreeB, tolerance, RhCmnTreeSearch2_thread_safe, &thread_safe_context);
  }
  return rc;
}

RH_C_FUNCTION bool ON_RTree_InsertRemove(ON_RTree* pTree, bool insert, ON_3DPOINT_STRUCT pt0, ON_3DPOINT_STRUCT pt1, void* elementId)
{
  bool rc = false;
  if( pTree )
  {
    if( insert )
      rc = pTree->Insert(pt0.val, pt1.val, elementId);
    else
      rc = pTree->Remove(pt0.val, pt1.val, elementId);
  }
  return rc;
}

RH_C_FUNCTION void ON_RTree_RemoveAll(ON_RTree* pTree)
{
  if( pTree )
    pTree->RemoveAll();
}

RH_C_FUNCTION unsigned int ON_RTree_SizeOf(const ON_RTree* pConstTree)
{
  unsigned int rc = 0;
  if( pConstTree )
    rc = (unsigned int)pConstTree->SizeOf();
  return rc;
}

RH_C_FUNCTION int ON_RTree_ElementCount(ON_RTree* pTree)
{
  int rc = 0;
  if( pTree )
    rc = pTree->ElementCount();
  return rc;
}