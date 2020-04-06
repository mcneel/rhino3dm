#include "stdafx.h"

///////////////////////////////////////////////////////////////////////////////////////
RH_C_FUNCTION ON_SimpleArray<ON_Line>* ON_LineArray_New()
{
  return new ON_SimpleArray<ON_Line>();
}

RH_C_FUNCTION void ON_LineArray_Delete( ON_SimpleArray<ON_Line>* pArray )
{
  if( pArray )
    delete pArray;
}

RH_C_FUNCTION int ON_LineArray_Count( const ON_SimpleArray<ON_Line>* pArray )
{
  int rc = 0;
  if( pArray )
    rc = pArray->Count();
  return rc;
}

RH_C_FUNCTION void ON_LineArray_CopyValues( const ON_SimpleArray<ON_Line>* pArray, /*ARRAY*/ON_Line* lines )
{
  if( pArray && lines )
  {
    int count = pArray->Count();
    if( count > 0 )
    {
      const ON_Line* source = pArray->Array();
      ::memcpy(lines, source, count * sizeof(ON_Line));
    }
  }
}

///////////////////////////////////////////////////////////////////////////////////////
RH_C_FUNCTION ON_SimpleArray<ON_Object*>* ON_ObjectArray_New()
{
  return new ON_SimpleArray<ON_Object*>();
}

RH_C_FUNCTION void ON_ObjectArray_Delete(ON_SimpleArray<ON_Object*>* pArray)
{
  if (pArray)
    delete pArray;
}

RH_C_FUNCTION void ON_ObjectArray_Append(ON_SimpleArray<ON_Object*>* pArray, ON_Object* pObject)
{
  if (pArray && pObject)
    pArray->Append(pObject);
}

RH_C_FUNCTION int ON_ObjectArray_Count(const ON_SimpleArray<const ON_Object*>* pArray)
{
  if (pArray)
    return pArray->Count();
  return 0;
}

RH_C_FUNCTION ON_Object* ON_ObjectArray_Item(ON_SimpleArray<ON_Object*>* pArray, int index)
{
  if (pArray && index >= 0 && index < pArray->Count())
    return (*pArray)[index];
  return nullptr;
}


///////////////////////////////////////////////////////////////////////////////////////
RH_C_FUNCTION ON_SimpleArray<ON_Plane>* ON_PlaneArray_New()
{
  return new ON_SimpleArray<ON_Plane>();
}

RH_C_FUNCTION void ON_PlaneArray_Delete(ON_SimpleArray<ON_Plane>* pArray)
{
  if (pArray)
    delete pArray;
}

RH_C_FUNCTION int ON_PlaneArray_Count(const ON_SimpleArray<ON_Plane>* pArray)
{
  int rc = 0;
  if (pArray)
    rc = pArray->Count();
  return rc;
}

RH_C_FUNCTION void ON_PlaneArray_CopyValues(const ON_SimpleArray<ON_Plane>* pArray, /*ARRAY*/ ON_PLANE_STRUCT* planes)
{
  if (pArray && planes)
  {
    int count = pArray->Count();
    if (count > 0)
    {
      for (int i = 0; i < count; i++)
        ::CopyToPlaneStruct(planes[i], *pArray->At(i));
    }
  }
}

///////////////////////////////////////////////////////////////////////////////////////

RH_C_FUNCTION ON_SimpleArray<ON_COMPONENT_INDEX>* ON_ComponentIndexArray_New()
{
  return new ON_SimpleArray<ON_COMPONENT_INDEX>();
}

RH_C_FUNCTION void ON_ComponentIndexArray_Delete( ON_SimpleArray<ON_COMPONENT_INDEX>* pArray )
{
  if( pArray )
    delete pArray;
}

RH_C_FUNCTION int ON_ComponentIndexArray_Count( const ON_SimpleArray<ON_COMPONENT_INDEX>* pArray )
{
  int rc = 0;
  if( pArray )
    rc = pArray->Count();
  return rc;
}

RH_C_FUNCTION void ON_ComponentIndexArray_CopyValues( const ON_SimpleArray<ON_COMPONENT_INDEX>* pArray, /*ARRAY*/ON_COMPONENT_INDEX* ci )
{
  if( pArray && ci )
  {
    int count = pArray->Count();
    if( count > 0 )
    {
      const ON_COMPONENT_INDEX* source = pArray->Array();
      ::memcpy(ci, source, count * sizeof(ON_COMPONENT_INDEX));
    }
  }
}


RH_C_FUNCTION void ON_ComponentIndexArray_Add(ON_SimpleArray<ON_COMPONENT_INDEX>* pArray, ON_COMPONENT_INDEX* ci)
{
  if (pArray && ci)
    pArray->Append(*ci);
}

///////////////////////////////////////////////////////////////////////////////////////

RH_C_FUNCTION ON_2dPointArray* ON_2dPointArray_New(int capacity)
{
  if( capacity < 1 )
    return new ON_2dPointArray();
  return new ON_2dPointArray(capacity);
}

RH_C_FUNCTION void ON_2dPointArray_Delete( ON_2dPointArray* pArray )
{
  if( pArray )
    delete pArray;
}


RH_C_FUNCTION int ON_2dPointArray_Count( const ON_2dPointArray* pArray )
{
  int rc = 0;
  if( pArray )
    rc = pArray->Count();
  return rc;
}

RH_C_FUNCTION void ON_2dPointArray_CopyValues( const ON_2dPointArray* pArray, /*ARRAY*/ON_2dPoint* pts )
{
  if( pArray && pts )
  {
    int count = pArray->Count();
    if( count > 0 )
    {
      const ON_2dPoint* source = pArray->Array();
      ::memcpy(pts, source, count * sizeof(ON_2dPoint));
    }
  }
}



///////////////////////////////////////////////////////////////////////////////////////

RH_C_FUNCTION ON_3dPointArray* ON_3dPointArray_New(int capacity)
{
  if( capacity < 1 )
    return new ON_3dPointArray();
  return new ON_3dPointArray(capacity);
}

RH_C_FUNCTION void ON_3dPointArray_Delete( ON_3dPointArray* pArray )
{
  if( pArray )
    delete pArray;
}


RH_C_FUNCTION int ON_3dPointArray_Count( const ON_3dPointArray* pArray )
{
  int rc = 0;
  if( pArray )
    rc = pArray->Count();
  return rc;
}

RH_C_FUNCTION void ON_3dPointArray_CopyValues( const ON_3dPointArray* pArray, /*ARRAY*/ON_3dPoint* pts )
{
  if( pArray && pts )
  {
    int count = pArray->Count();
    if( count > 0 )
    {
      const ON_3dPoint* source = pArray->Array();
      ::memcpy(pts, source, count * sizeof(ON_3dPoint));
    }
  }
}

///////////////////////////////////////////////////////////////////////////////////////

RH_C_FUNCTION ON_SimpleArray<ON_Polyline*>* ON_3dPointArrayArray_New(int capacity)
{
  if (capacity < 1)
    return new ON_SimpleArray<ON_Polyline*>();
  return new ON_SimpleArray<ON_Polyline*>(capacity);
}

RH_C_FUNCTION void ON_3dPointArrayArray_DeleteListAndContent(ON_SimpleArray<ON_Polyline*>* pArray)
{
  if (pArray)
  {
    for (int i = 0; i < pArray->Count(); i++)
    {
      ON_Polyline* ptr = (*pArray)[i];
      delete ptr;
    }
  }
  delete pArray;
}

RH_C_FUNCTION int ON_3dPointArrayArray_Count(const ON_SimpleArray<ON_Polyline*>* pArray)
{
  int rc = 0;
  if (pArray) rc = pArray->Count();
  return rc;
}

RH_C_FUNCTION int ON_3dPointArrayArray_PointCountAt(const ON_SimpleArray<ON_Polyline*>* pArray, int index)
{
  int rc = -1;
  if (pArray)
  {
    if (index >= 0 && index < pArray->Count())
    {
      if ((*pArray)[index] == nullptr) rc = 0;
      else rc = (*pArray)[index]->Count();
    }
  }
  return rc;
}

RH_C_FUNCTION bool ON_3dPointArrayArray_Indexer(const ON_SimpleArray<ON_Polyline*>* pArray, int index, int pointIndex, ON_3dPoint* ret_pt)
{
  bool rc = false;
  if (pArray && ret_pt)
  {
    if (index >= 0 && index < pArray->Count() && pointIndex >= 0)
    {
      if ((*pArray)[index] != nullptr)
      {
        ON_Polyline* pl = (*pArray)[index];
        if (pointIndex < pl->Count())
        {
          *ret_pt = (*pl)[pointIndex];
          rc = true;
        }
      }
    }
  }
  return rc;
}



/////////////////////////////////////////////////////////////////////////////////

RH_C_FUNCTION ON_SimpleArray<int>* ON_IntArray_New(/*ARRAY*/const int* vals, int count)
{
  if( 0==vals || count<1 )
    return new ON_SimpleArray<int>();
  ON_SimpleArray<int>* rc = new ON_SimpleArray<int>(count);
  rc->Append(count, vals);
  return rc;
}


RH_C_FUNCTION void ON_IntArray_CopyValues(const ON_SimpleArray<int>* ptr, /*ARRAY*/int* vals)
{
  if( ptr && vals )
  {
    int count = ptr->Count();
    if( count > 0 )
    {
      const int* source = ptr->Array();
      ::memcpy(vals, source, count * sizeof(int));
    }
  }
}

RH_C_FUNCTION int ON_IntArray_Count(const ON_SimpleArray<int>* ptr)
{
  int rc = 0;
  if( ptr )
    rc = ptr->Count();
  return rc;
}

RH_C_FUNCTION void ON_IntArray_Delete(ON_SimpleArray<int>* p)
{
  if( p )
    delete p;
}

/////////////////////////////////////////////////////////////////////////////////

RH_C_FUNCTION ON_SimpleArray<unsigned int>* ON_UintArray_New(int initial_capacity)
{
  return new ON_SimpleArray<unsigned int>(initial_capacity);
}

RH_C_FUNCTION void ON_UintArray_CopyValues(const ON_SimpleArray<unsigned int>* ptr, /*ARRAY*/unsigned int* vals)
{
  if( ptr && vals )
  {
    unsigned int count = ptr->UnsignedCount();
    if( count > 0 )
    {
      const unsigned int* source = ptr->Array();
      ::memcpy(vals, source, count * sizeof(vals[0]));
    }
  }
}

RH_C_FUNCTION void ON_UintArray_Append(ON_SimpleArray<unsigned int>* ptr, unsigned int val)
{
  if (ptr)
  {
    ptr->Append(val);
  }
}

RH_C_FUNCTION int ON_UintArray_Count(const ON_SimpleArray<unsigned int>* ptr)
{
  return ( ptr ? ptr->Count() : 0);
}

RH_C_FUNCTION unsigned int ON_UintArray_UnsignedCount(const ON_SimpleArray<unsigned int>* ptr)
{
  return ( ptr ? ptr->UnsignedCount() : 0);
}

RH_C_FUNCTION void ON_UintArray_Delete(ON_SimpleArray<unsigned int>* p)
{
  if( p )
    delete p;
}

/////////////////////////////////////////////////////////////////////////////////

RH_C_FUNCTION ON_SimpleArray<ON_UUID>* ON_UUIDArray_New()
{
  return new ON_SimpleArray<ON_UUID>();
}

RH_C_FUNCTION void ON_UUIDArray_CopyValues(const ON_SimpleArray<ON_UUID>* ptr, /*ARRAY*/ON_UUID* vals)
{
  if( ptr && vals )
  {
    int count = ptr->Count();
    if( count > 0 )
    {
      const ON_UUID* source = ptr->Array();
      ::memcpy(vals, source, count * sizeof(ON_UUID));
    }
  }
}

RH_C_FUNCTION void ON_UUIDArray_Append(ON_SimpleArray<ON_UUID>* ptr, const ON_UUID uuid)
{
  if( ptr )
    ptr->Append(uuid);
}

RH_C_FUNCTION int ON_UUIDArray_Count(const ON_SimpleArray<ON_UUID>* ptr)
{
	int rc = 0;
	if (ptr)
		rc = ptr->Count();
	return rc;
}

RH_C_FUNCTION ON_UUID ON_UUIDArray_Get(ON_SimpleArray<ON_UUID>* ptr, int index)
{
  if( ptr && index>=0 && index<ptr->Count() )
  {
    return (*ptr)[index];
  }
  return ON_nil_uuid;
}

RH_C_FUNCTION void ON_UUIDArray_Delete(ON_SimpleArray<ON_UUID>* p)
{
  if( p )
    delete p;
}

/////////////////////////////////////////////////////////////////////////////////

RH_C_FUNCTION ON_SimpleArray<ON_UUID*>* ON_UUIDPtrArray_New()
{
  return new ON_SimpleArray<ON_UUID*>();
}

RH_C_FUNCTION int ON_UUIDPtrArray_Count(const ON_SimpleArray<ON_UUID*>* ptr)
{
  int rc = 0;
  if( ptr )
    rc = ptr->Count();
  return rc;
}

RH_C_FUNCTION ON_UUID ON_UUIDPtrArray_Get(ON_SimpleArray<ON_UUID*>* ptr, int index)
{
  if( ptr && index>=0 && index<ptr->Count() )
  {
    return *((*ptr)[index]);
  }
  return ON_nil_uuid;
}

RH_C_FUNCTION void ON_UUIDPtrArray_Delete(ON_SimpleArray<ON_UUID*>* p)
{
  if( p )
    delete p;
}

////////////////////////////////////
RH_C_FUNCTION ON_SimpleArray<double>* ON_DoubleArray_New()
{
  return new ON_SimpleArray<double>();
}

RH_C_FUNCTION int ON_DoubleArray_Count(const ON_SimpleArray<double>* ptr)
{
  int rc = 0;
  if( ptr )
    rc = ptr->Count();
  return rc;
}

RH_C_FUNCTION void ON_DoubleArray_Append(ON_SimpleArray<double>* pArray, int count, /*ARRAY*/const double* vals)
{
  if( pArray && count>0 && vals )
    pArray->Append(count, vals);
}

RH_C_FUNCTION void ON_DoubleArray_CopyValues(const ON_SimpleArray<double>* ptr, /*ARRAY*/double* vals)
{
  if( ptr && vals )
  {
    int count = ptr->Count();
    if( count > 0 )
    {
      const double* source = ptr->Array();
      ::memcpy(vals, source, count * sizeof(double));
    }
  }
}

RH_C_FUNCTION void ON_DoubleArray_Delete(ON_SimpleArray<double>* p)
{
  if( p )
    delete p;
}

/////////////////////////////////////////////////////////////////////////////////

RH_C_FUNCTION ON_SimpleArray<ON_2dex>* ON_2dexArray_New(/*ARRAY*/const ON_2dex* vals, int count)
{
  if (0 == vals || count<1)
    return new ON_SimpleArray<ON_2dex>();
  ON_SimpleArray<ON_2dex>* rc = new ON_SimpleArray<ON_2dex>(count);
  rc->Append(count, vals);
  return rc;
}


RH_C_FUNCTION void ON_2dexArray_CopyValues(const ON_SimpleArray<ON_2dex>* ptr, /*ARRAY*/ON_2dex* vals)
{
  if (ptr && vals)
  {
    int count = ptr->Count();
    if (count > 0)
    {
      const ON_2dex* source = ptr->Array();
      ::memcpy(vals, source, count * sizeof(ON_2dex));
    }
  }
}

RH_C_FUNCTION int ON_2dexArray_Count(const ON_SimpleArray<ON_2dex>* ptr)
{
  int rc = 0;
  if (ptr)
    rc = ptr->Count();
  return rc;
}

RH_C_FUNCTION void ON_2dexArray_Delete(ON_SimpleArray<ON_2dex>* p)
{
  if (p)
    delete p;
}

//////////////////


RH_C_FUNCTION ON_SimpleArray<ON_Brep*>* ON_BrepArray_New()
{
  return new ON_SimpleArray<ON_Brep*>();
}

RH_C_FUNCTION void ON_BrepArray_Delete(ON_SimpleArray<ON_Brep*>* pBrepArray)
{
  if( pBrepArray )
    delete pBrepArray;
}

RH_C_FUNCTION int ON_BrepArray_Count(const ON_SimpleArray<ON_Brep*>* pBrepArray)
{
  int rc = 0;
  if( pBrepArray )
  {
    rc = pBrepArray->Count();
  }
  return rc;
}

RH_C_FUNCTION void ON_BrepArray_Append(ON_SimpleArray<ON_Brep*>* pBrepArray, ON_Brep* pBrep)
{
  if( pBrepArray && pBrep )
  {
    pBrepArray->Append(pBrep);
  }
}

RH_C_FUNCTION ON_Brep* ON_BrepArray_Get(ON_SimpleArray<ON_Brep*>* pBrepArray, int index)
{
  ON_Brep* rc = nullptr;
  if( pBrepArray && index>=0 && index<pBrepArray->Count() )
  {
    rc = (*pBrepArray)[index];
  }
  return rc;
}

RH_C_FUNCTION ON_SimpleArray<ON_Extrusion*>* ON_ExtrusionArray_New()
{
  return new ON_SimpleArray<ON_Extrusion*>();
}

RH_C_FUNCTION void ON_ExtrusionArray_Delete(ON_SimpleArray<ON_Extrusion*>* pExtrusionArray)
{
  if (pExtrusionArray)
    delete pExtrusionArray;
}

RH_C_FUNCTION int ON_ExtrusionArray_Count(const ON_SimpleArray<ON_Extrusion*>* pExtrusionArray)
{
  int rc = 0;
  if (pExtrusionArray)
  {
    rc = pExtrusionArray->Count();
  }
  return rc;
}

RH_C_FUNCTION void ON_ExtrusionArray_Append(ON_SimpleArray<ON_Extrusion*>* pExtrusionArray, ON_Extrusion* pExtrusion)
{
  if (pExtrusionArray && pExtrusion)
  {
    pExtrusionArray->Append(pExtrusion);
  }
}

RH_C_FUNCTION ON_Extrusion* ON_ExtrusionArray_Get(ON_SimpleArray<ON_Extrusion*>* pExtrusionArray, int index)
{
  ON_Extrusion* rc = nullptr;
  if (pExtrusionArray && index >= 0 && index<pExtrusionArray->Count())
  {
    rc = (*pExtrusionArray)[index];
  }
  return rc;
}


//////////////////


RH_C_FUNCTION ON_SimpleArray<ON_Mesh*>* ON_MeshArray_New()
{
  return new ON_SimpleArray<ON_Mesh*>();
}

RH_C_FUNCTION void ON_MeshArray_Delete(ON_SimpleArray<ON_Mesh*>* pMeshArray)
{
  if( pMeshArray )
    delete pMeshArray;
}

RH_C_FUNCTION int ON_MeshArray_Count(const ON_SimpleArray<ON_Mesh*>* pMeshArray)
{
  int rc = 0;
  if( pMeshArray )
  {
    rc = pMeshArray->Count();
  }
  return rc;
}

RH_C_FUNCTION void ON_MeshArray_Append(ON_SimpleArray<ON_Mesh*>* pMeshArray, ON_Mesh* pMesh)
{
  if( pMeshArray && pMesh )
  {
    pMeshArray->Append(pMesh);
  }
}

RH_C_FUNCTION ON_Mesh* ON_MeshArray_Get(ON_SimpleArray<ON_Mesh*>* pMeshArray, int index)
{
  ON_Mesh* rc = nullptr;
  if( pMeshArray && index>=0 && index<pMeshArray->Count() )
  {
    rc = (*pMeshArray)[index];
  }
  return rc;
}

RH_C_FUNCTION ON_ClassArray<ON_wString>* ON_StringArray_New()
{
  return new ON_ClassArray<ON_wString>();
}

RH_C_FUNCTION void ON_StringArray_Append(ON_ClassArray<ON_wString>* pStrings, const RHMONO_STRING* str)
{
  if( pStrings && str )
  {
    INPUTSTRINGCOERCE(_str, str);
    pStrings->Append(_str);
  }
}

RH_C_FUNCTION void ON_StringArray_Delete(ON_ClassArray<ON_wString>* pStrings)
{
  if(pStrings)
    delete pStrings;
}

RH_C_FUNCTION int ON_StringArray_Count(const ON_ClassArray<ON_wString>* pStrings)
{
  if (pStrings)
    return pStrings->Count();
  return 0;
}

RH_C_FUNCTION void ON_StringArray_Get(const ON_ClassArray<ON_wString>* pStrings, int index, CRhCmnStringHolder* pStringHolder)
{
  if( pStrings && index>=0 && index<pStrings->Count() && pStringHolder )
  {
    pStringHolder->Set((*pStrings)[index]);
  }
}

//////////////////////////////////////////////////////

RH_C_FUNCTION ON_SimpleArray<ON_Surface*>* ON_SurfaceArray_New()
{
  return new ON_SimpleArray<ON_Surface*>();
}

RH_C_FUNCTION void ON_SurfaceArray_Delete(ON_SimpleArray<ON_Surface*>* pSurfaceArray)
{
  if( pSurfaceArray )
    delete pSurfaceArray;
}

RH_C_FUNCTION int ON_SurfaceArray_Count(const ON_SimpleArray<ON_Surface*>* pSurfaceArray)
{
  int rc = 0;
  if( pSurfaceArray )
  {
    rc = pSurfaceArray->Count();
  }
  return rc;
}

RH_C_FUNCTION ON_Surface* ON_SurfaceArray_Get(ON_SimpleArray<ON_Surface*>* pSurfaceArray, int index)
{
  ON_Surface* rc = nullptr;
  if( pSurfaceArray && index>=0 && index<pSurfaceArray->Count() )
  {
    rc = (*pSurfaceArray)[index];
  }
  return rc;
}

///////////////////////////////////////////////////////////////////////////////////////

RH_C_FUNCTION ON_SimpleArray<ON_Interval>* ON_IntervalArray_New()
{
  return new ON_SimpleArray<ON_Interval>();
}

RH_C_FUNCTION void ON_IntervalArray_Delete(ON_SimpleArray<ON_Interval>* pIntervalArray)
{
  if( pIntervalArray )
    delete pIntervalArray;
}

RH_C_FUNCTION int ON_IntervalArray_Count(const ON_SimpleArray<ON_Interval>* pConstIntervalArray)
{
  if( pConstIntervalArray )
    return pConstIntervalArray->Count();
  return 0;
}

RH_C_FUNCTION void ON_IntervalArray_Add(ON_SimpleArray<ON_Interval>* pIntervalArray, const ON_INTERVAL_STRUCT interval)
{
  if (pIntervalArray)
  {
    const ON_Interval* _interval = (const ON_Interval*)&interval;
    pIntervalArray->Append(*_interval);
  }
}

RH_C_FUNCTION void ON_IntervalArray_CopyValues(const ON_SimpleArray<ON_Interval>* pSrcIntervalArray, /*ARRAY*/ON_Interval* dest)
{
  if( pSrcIntervalArray && dest )
  {
    const ON_Interval* pSrc = pSrcIntervalArray->Array();
    int count = pSrcIntervalArray->Count();
    ::memcpy(dest, pSrc, count*sizeof(ON_Interval));
  }
}

///////////////////////////////////////////////////////////////////////////////////////

RH_C_FUNCTION ON_SimpleArray<ON_BezierCurve*>* ON_SimpleArray_BezierCurveNew()
{
  return new ON_SimpleArray<ON_BezierCurve*>();
}

RH_C_FUNCTION void ON_SimpleArray_BezierCurveDelete(ON_SimpleArray<ON_BezierCurve*>* pBezArray)
{
  if( pBezArray )
    delete pBezArray;
}

RH_C_FUNCTION ON_BezierCurve* ON_SimpleArray_BezierCurvePtr(ON_SimpleArray<ON_BezierCurve*>* pBezArray, int index)
{
  ON_BezierCurve* rc = nullptr;
  if( pBezArray && index>=0 && index<pBezArray->Count() )
    rc = (*pBezArray)[index];
  return rc;
}

RH_C_FUNCTION void ON_BezierCurve_Delete(ON_BezierCurve* pBez)
{
  if( pBez )
    delete pBez;
}

// The following is available in opennurbs but only used the Rhino SDK
// version of RhinoCommon.
RH_C_FUNCTION ON_SimpleArray<const ON_3dmObjectAttributes*>* ON_SimpleArray_3dmObjectAttributes_New()
{
  return new ON_SimpleArray<const ON_3dmObjectAttributes*>();
}

RH_C_FUNCTION void ON_SimpleArray_3dmObjectAttributes_Delete( ON_SimpleArray<const ON_3dmObjectAttributes*>* pArray )
{
  if( pArray )
    delete pArray;
}

RH_C_FUNCTION void ON_SimpleArray_3dmObjectAttributes_Add( ON_SimpleArray<const ON_3dmObjectAttributes*>* pArray, const ON_3dmObjectAttributes* pAttributes )
{
  if( pArray && pAttributes )
    pArray->Append(pAttributes);
}

RH_C_FUNCTION int ON_SimpleArray_3dmObjectAttributes_Count( ON_SimpleArray<const ON_3dmObjectAttributes*>* pArray )
{
  if( pArray )
    return pArray->Count();
  return 0;
}

RH_C_FUNCTION const ON_3dmObjectAttributes* ON_SimpleArray_3dmObjectAttributes_Get( ON_SimpleArray<const ON_3dmObjectAttributes*>* pArray, int index )
{
  if( pArray && index>=0 && index<pArray->Count() )
    return (*pArray)[index];
  return nullptr;
}

/////////////////////////////////////////////////////////////////////////////
// ON_SimpleArray<ON_Curve*> 

RH_C_FUNCTION ON_SimpleArray<ON_Curve*>* ON_CurveArray_New(int initial_capacity)
{
  return new ON_SimpleArray<ON_Curve*>(initial_capacity);
}

RH_C_FUNCTION void ON_CurveArray_Append(ON_SimpleArray<ON_Curve*>* arrayPtr, ON_Curve* curvePtr)
{
  if( arrayPtr && curvePtr )
  {
    arrayPtr->Append( curvePtr );
  }
}

RH_C_FUNCTION int ON_CurveArray_Count(const ON_SimpleArray<ON_Curve*>* arrayPtr)
{
  int rc = 0;
  if( arrayPtr )
    rc = arrayPtr->Count();
  return rc;
}

RH_C_FUNCTION ON_Curve* ON_CurveArray_Get(ON_SimpleArray<ON_Curve*>* arrayPtr, int index)
{
  ON_Curve* rc = nullptr;
  
  if( arrayPtr && index>=0 )
  {
    if( index<arrayPtr->Count() )
      rc = (*arrayPtr)[index];
  }
  return rc;
}

RH_C_FUNCTION void ON_CurveArray_Delete(ON_SimpleArray<ON_Curve*>* arrayPtr)
{
  if( arrayPtr )
    delete arrayPtr;
}

////////////////////////////////////////////////////////////

RH_C_FUNCTION ON_ClassArray<ON_ObjRef>* ON_ClassArrayON_ObjRef_New()
{
  return new ON_ClassArray<ON_ObjRef>();
}

RH_C_FUNCTION int ON_ClassArrayON_ObjRef_Count(const ON_ClassArray<ON_ObjRef>* pConstObjRefArray)
{
  if( pConstObjRefArray )
    return pConstObjRefArray->Count();
  return 0;
}

RH_C_FUNCTION void ON_ClassArrayON_ObjRef_Append(ON_ClassArray<ON_ObjRef>* pObjRefArray, const ON_ObjRef* pConstObjRef)
{
  if( pObjRefArray && pConstObjRef )
    pObjRefArray->Append(*pConstObjRef);
}

RH_C_FUNCTION void ON_ClassArrayON_ObjRef_Delete(ON_ClassArray<ON_ObjRef>* pObjRefArray)
{
  if( pObjRefArray )
    delete pObjRefArray;
}

RH_C_FUNCTION const ON_ObjRef* ON_ClassArrayON_ObjRef_Get(const ON_ClassArray<ON_ObjRef>* pConstObjRefArray, int index)
{
  if( pConstObjRefArray )
    return pConstObjRefArray->At(index);
  return nullptr;
}

/////////////////////////////////////////////////////////////////////////////
// ON_SimpleArray<ON_DimStyle*> 

RH_C_FUNCTION ON_SimpleArray<ON_DimStyle*>* ON_DimStyleArray_New()
{
  return new ON_SimpleArray<ON_DimStyle*>();
}

RH_C_FUNCTION void ON_DimStyleArray_Delete(ON_SimpleArray<ON_DimStyle*>* pointerDimStyleArray, bool deleteItems)
{
  if (pointerDimStyleArray)
  {
    if (deleteItems)
    {
      for (int i = 0; i<pointerDimStyleArray->Count(); i++)
      {
        ON_DimStyle* p = (*pointerDimStyleArray)[i];
        if (p)
          delete p;
      }
    }
    delete pointerDimStyleArray;
  }
}

RH_C_FUNCTION int ON_DimStyleArray_Count(const ON_SimpleArray<ON_DimStyle*>* constDimStyleArray)
{
  if (constDimStyleArray)
    return constDimStyleArray->Count();
  return 0;
}

RH_C_FUNCTION ON_DimStyle* ON_DimStyleArray_Get(ON_SimpleArray<ON_DimStyle*>* pointerToArray, int index)
{
  if( pointerToArray && index>=0 &&index<pointerToArray->Count())
    return (*pointerToArray)[index];
  return nullptr;
}

///////////////////////////////////////////////////////////////////////////////////////

RH_C_FUNCTION ON_SimpleArray<const ON_BinaryArchive*>* ON_BinaryArchiveArray_New()
{
	return new ON_SimpleArray<const ON_BinaryArchive*>();
}

RH_C_FUNCTION void ON_BinaryArchiveArray_Delete(ON_SimpleArray<const ON_BinaryArchive*>* pBufferArray)
{
	if (pBufferArray)
		delete pBufferArray;
}

RH_C_FUNCTION int ON_BinaryArchiveArray_Count(const ON_SimpleArray<const ON_BinaryArchive*>* pConstBufferArray)
{
	if (pConstBufferArray)
		return pConstBufferArray->Count();
	return 0;
}

RH_C_FUNCTION void ON_BinaryArchiveArray_Add(ON_SimpleArray<const ON_BinaryArchive*>* pBufferArray, const ON_BinaryArchive* pBuffer)
{
	if (pBufferArray)
	{
		pBufferArray->Append(pBuffer);
	}
}

RH_C_FUNCTION const ON_BinaryArchive* ON_BufferArray_Get(const ON_SimpleArray<const ON_BinaryArchive*>* pConstBufferArray, int index)
{
	if (pConstBufferArray && index >= 0 && index<pConstBufferArray->Count())
		return (*pConstBufferArray)[index];
	return nullptr;
}

///////////////////////////////////////////////////////////////////////////////////////

RH_C_FUNCTION ON_SimpleArray<ON_Linetype*>* ON_LinetypeArray_New()
{
  return new ON_SimpleArray<ON_Linetype*>();
}

RH_C_FUNCTION void ON_LinetypeArray_Delete(ON_SimpleArray<ON_Linetype*>* pArray)
{
  if (pArray)
    delete pArray;
}

RH_C_FUNCTION int ON_LinetypeArray_Count(const ON_SimpleArray<ON_Linetype*>* pArray)
{
  int rc = 0;
  if (pArray)
    rc = pArray->Count();
  return rc;
}

RH_C_FUNCTION ON_Linetype* ON_Linetype_Get(ON_SimpleArray<ON_Linetype*>* pArray, int index)
{
  ON_Linetype* rc = nullptr;
  if (pArray && index >= 0 && index < pArray->Count())
    rc = (*pArray)[index];
  return rc;
}
