#include "stdafx.h"

RH_C_FUNCTION ON_BezierCurve* ON_BezierCurve_New()
{
  return new ON_BezierCurve();
}

RH_C_FUNCTION ON_BezierCurve* ON_BezierCurve_New2(int dimension, bool rational, int order, int cvLength, /*ARRAY*/const double* cvs)
{
  ON_BezierCurve* rc = new ON_BezierCurve(dimension, rational, order);
  if (cvLength == rc->m_cv_capacity && cvs)
  {
    memcpy(rc->m_cv, cvs, cvLength * sizeof(double));
  }
  return rc;
}

RH_C_FUNCTION ON_BezierCurve* ON_BezierCurve_New2d(int count, /*ARRAY*/const ON_2dPoint* points)
{
  ON_BezierCurve* rc = nullptr;
  if( count && points )
  {
    ON_2dPointArray pts;
    pts.Append(count, points);
    rc = new ON_BezierCurve(pts);
    pts.KeepArray();
  }
  return rc;
}

RH_C_FUNCTION ON_BezierCurve* ON_BezierCurve_New3d(int count, /*ARRAY*/const ON_3dPoint* points)
{
  ON_BezierCurve* rc = nullptr;
  if( count && points )
  {
    ON_3dPointArray pts;
    pts.Append(count, points);
    rc = new ON_BezierCurve(pts);
    pts.KeepArray();
  }
  return rc;
}

RH_C_FUNCTION ON_BezierCurve* ON_BezierCurve_New4d(int count, /*ARRAY*/const ON_4dPoint* points)
{
  ON_BezierCurve* rc = nullptr;
  if( count && points )
  {
    ON_4dPointArray pts;
    pts.Append(count, points);
    rc = new ON_BezierCurve(pts);
    pts.KeepArray();
  }
  return rc;
}

RH_C_FUNCTION void ON_BezierCurve_Delete(ON_BezierCurve* pBez)
{
  if (pBez)
    delete pBez;
}

RH_C_FUNCTION bool ON_BezierCurve_IsValid(const ON_BezierCurve* pConstBezierCurve)
{
  bool rc = false;
  if( pConstBezierCurve )
  {
    rc = pConstBezierCurve->IsValid();
  }
  return rc;
}

RH_C_FUNCTION void ON_BezierCurve_Dump(const ON_BezierCurve* pConstBezierCurve, CRhCmnStringHolder* pStringHolder)
{
  if( pConstBezierCurve && pStringHolder )
  {
    ON_wString s;
    ON_TextLog textlog(s);
    pConstBezierCurve->Dump(textlog);
    pStringHolder->Set(s);
  }
}

RH_C_FUNCTION int ON_BezierCurve_Dimension(const ON_BezierCurve* pConstBezierCurve)
{
  int rc = 0;
  if( pConstBezierCurve )
    rc = pConstBezierCurve->Dimension();
  return rc;
}

RH_C_FUNCTION int ON_BezierCurve_Order(const ON_BezierCurve* pConstBezierCurve)
{
  if (pConstBezierCurve)
    return pConstBezierCurve->Order();
  return 0;
}

RH_C_FUNCTION int ON_BezierCurve_CvCapacity(const ON_BezierCurve* pConstBezierCurve)
{
  if (pConstBezierCurve)
    return pConstBezierCurve->m_cv_capacity;
  return 0;
}

RH_C_FUNCTION void ON_BezierCurve_SetCvs(const ON_BezierCurve* pConstBezierCurve, int length, /*ARRAY*/double* cvs)
{
  if (pConstBezierCurve && pConstBezierCurve->m_cv && pConstBezierCurve->m_cv_capacity == length && cvs)
  {
    memcpy(cvs, pConstBezierCurve->m_cv, length * sizeof(double));
  }
}

RH_C_FUNCTION ON_BezierCurve* ON_BezierCurve_Loft(int count, /*ARRAY*/const ON_3dPoint* points)
{
  ON_BezierCurve* rc = nullptr;
  if( count && points )
  {
    rc = new ON_BezierCurve();
    ON_3dPointArray _pts(count);
    _pts.Append(count, points);
    if( !rc->Loft(_pts) )
    {
      delete rc;
      rc = nullptr;
    }
  }
  return rc;
}

RH_C_FUNCTION ON_BezierCurve* ON_BezierCurve_Loft2(int count, /*ARRAY*/const ON_2dPoint* points)
{
  ON_BezierCurve* rc = nullptr;
  if( count && points )
  {
    rc = new ON_BezierCurve();
    if(rc->Loft(2, count, 2, &(points->x), 0, nullptr))
    {
      delete rc;
      rc = nullptr;
    }
  }
  return rc;
}

RH_C_FUNCTION void ON_BezierCurve_BoundingBox(const ON_BezierCurve* pConstBezierCurve, bool accurate, ON_BoundingBox* bounding_box)
{
  if( pConstBezierCurve && bounding_box )
  {
    if( accurate )
      pConstBezierCurve->GetTightBoundingBox(*bounding_box);
    else
      *bounding_box = pConstBezierCurve->BoundingBox();
  }
}

RH_C_FUNCTION void ON_BezierCurve_PointAt(const ON_BezierCurve* pConstBezierCurve, double t, ON_3dPoint* point)
{
  if( pConstBezierCurve && point )
    *point = pConstBezierCurve->PointAt(t);
}

RH_C_FUNCTION void ON_BezierCurve_TangentAt(const ON_BezierCurve* pConstBezierCurve, double t, ON_3dVector* tangent)
{
  if( pConstBezierCurve && tangent )
    *tangent = pConstBezierCurve->TangentAt(t);
}

RH_C_FUNCTION void ON_BezierCurve_CurvatureAt(const ON_BezierCurve* pConstBezierCurve, double t, ON_3dVector* tangent)
{
  if( pConstBezierCurve && tangent )
    *tangent = pConstBezierCurve->CurvatureAt(t);
}

RH_C_FUNCTION ON_NurbsCurve* ON_BezierCurve_GetNurbForm(const ON_BezierCurve* pConstBezierCurve)
{
  ON_NurbsCurve* rc = nullptr;
  if( pConstBezierCurve )
  {
    // https://mcneel.myjetbrains.com/youtrack/issue/RH-40480
    // Always use static constructor
    //rc = new ON_NurbsCurve();
    rc = ON_NurbsCurve::New();
    if( !pConstBezierCurve->GetNurbForm(*rc) )
    {
      delete rc;
      rc = nullptr;
    }
  }
  return rc;
}

RH_C_FUNCTION bool ON_BezierCurve_IsRational(const ON_BezierCurve* pConstBezierCurve)
{
  bool rc = false;
  if( pConstBezierCurve )
    rc = pConstBezierCurve->IsRational();
  return rc;
}

RH_C_FUNCTION int ON_BezierCurve_CVCount(const ON_BezierCurve* pConstBezierCurve)
{
  int rc = 0;
  if( pConstBezierCurve )
    rc = pConstBezierCurve->CVCount();
  return rc;
}

RH_C_FUNCTION bool ON_BezierCurve_GetCV3d(const ON_BezierCurve* pConstBezierCurve, int index, ON_3dPoint* point)
{
  bool rc = false;
  if( pConstBezierCurve && point )
    rc = pConstBezierCurve->GetCV(index, *point);
  return rc;
}

RH_C_FUNCTION bool ON_BezierCurve_GetCV4d(const ON_BezierCurve* pConstBezierCurve, int index, ON_4dPoint* point)
{
  bool rc = false;
  if( pConstBezierCurve && point )
    rc = pConstBezierCurve->GetCV(index, *point);
  return rc;
}

RH_C_FUNCTION bool ON_BezierCurve_MakeRational(ON_BezierCurve* pBezierCurve, bool on)
{
  bool rc = false;
  if( pBezierCurve )
  {
    if( on )
      rc = pBezierCurve->MakeRational();
    else
      rc = pBezierCurve->MakeNonRational();
  }
  return rc;
}

RH_C_FUNCTION bool ON_BezierCurve_ChangeInt(ON_BezierCurve* pBezierCurve, bool degree, int newInt)
{
  bool rc = false;
  if( pBezierCurve )
  {
    if( degree )
      rc = pBezierCurve->IncreaseDegree(newInt);
    else
      rc = pBezierCurve->ChangeDimension(newInt);
  }
  return rc;
}

RH_C_FUNCTION bool ON_BezierCurve_Split(const ON_BezierCurve* pConstBezierCurve, double t, ON_BezierCurve* left, ON_BezierCurve* right)
{
  bool rc = false;
  if (pConstBezierCurve && left && right)
  {
    rc = pConstBezierCurve->Split(t, *left, *right);
  }
  return rc;
}

RH_C_FUNCTION ON_SimpleArray<ON_BezierCurve*>* ON_BezierCurve_NewSimpleArray()
{
  return new ON_SimpleArray<ON_BezierCurve*>();
}

RH_C_FUNCTION void ON_BezierCurve_DeleteSimpleArray(ON_SimpleArray<ON_BezierCurve*>* ptrArray)
{
  if (ptrArray)
    delete ptrArray;
}

RH_C_FUNCTION void ON_BezierCurve_AppendToSimpleArray(ON_SimpleArray<ON_BezierCurve*>* ptrArray, ON_BezierCurve* ptrCurve)
{
  if (ptrArray)
    ptrArray->Append(ptrCurve);
}

RH_C_FUNCTION ON_BezierSurface* ON_BezierSurface_New()
{
  return new ON_BezierSurface();
}

RH_C_FUNCTION void ON_BezierSurface_Delete(ON_BezierSurface* pBez)
{
  if (pBez)
    delete pBez;
}

RH_C_FUNCTION bool ON_BezierSurface_IsValid(const ON_BezierSurface* pConstBezierSurface)
{
  bool rc = false;
  if (pConstBezierSurface)
  {
    rc = pConstBezierSurface->IsValid();
  }
  return rc;
}

RH_C_FUNCTION void ON_BezierSurface_Dump(const ON_BezierSurface* pConstBezierSurface, CRhCmnStringHolder* pStringHolder)
{
  if (pConstBezierSurface && pStringHolder)
  {
    ON_wString s;
    ON_TextLog textlog(s);
    pConstBezierSurface->Dump(textlog);
    pStringHolder->Set(s);
  }
}

RH_C_FUNCTION int ON_BezierSurface_Dimension(const ON_BezierSurface* pConstBezierSurface)
{
  int rc = 0;
  if (pConstBezierSurface)
    rc = pConstBezierSurface->Dimension();
  return rc;
}

RH_C_FUNCTION ON_BezierSurface* ON_BezierSurface_Loft(ON_SimpleArray<ON_BezierCurve*>* ptrBezCurveArray)
{
  if (nullptr == ptrBezCurveArray)
    return nullptr;
  ON_BezierSurface* rc = new ON_BezierSurface();
  if (!rc->Loft(ptrBezCurveArray->Count(), ptrBezCurveArray->Array()))
  {
    delete rc;
    rc = nullptr;
  }
  return rc;
}

RH_C_FUNCTION void ON_BezierSurface_BoundingBox(const ON_BezierSurface* pConstBezierSurface, ON_BoundingBox* bounding_box)
{
  if (pConstBezierSurface && bounding_box)
  {
    *bounding_box = pConstBezierSurface->BoundingBox();
  }
}

RH_C_FUNCTION bool ON_BezierSurface_Transform(ON_BezierSurface* pBezierSurface, const ON_Xform* xform)
{
  if (pBezierSurface && xform)
  {
    return pBezierSurface->Transform(*xform);
  }
  return false;
}

RH_C_FUNCTION void ON_BezierSurface_Domain(const ON_BezierSurface* pConstBezierSurface, int direction, ON_Interval* interval)
{
  if (pConstBezierSurface && interval)
  {
    *interval = pConstBezierSurface->Domain(direction);
  }
}

RH_C_FUNCTION ON_BezierSurface* ON_BezierSurface_Reverse(const ON_BezierSurface* pConstBezierSurface, int direction)
{
  if (nullptr == pConstBezierSurface)
    return nullptr;
  ON_BezierSurface* rc = new ON_BezierSurface(*pConstBezierSurface);
  if (!rc->Reverse(direction))
  {
    delete rc;
    rc = nullptr;
  }
  return rc;
}

RH_C_FUNCTION ON_BezierSurface* ON_BezierSurface_Transpose(const ON_BezierSurface* pConstBezierSurface)
{
  if (nullptr == pConstBezierSurface)
    return nullptr;
  ON_BezierSurface* rc = new ON_BezierSurface(*pConstBezierSurface);
  if (!rc->Transpose())
  {
    delete rc;
    rc = nullptr;
  }
  return rc;
}

RH_C_FUNCTION void ON_BezierSurface_PointAt(const ON_BezierSurface* pConstBezierSurface, double u, double v, ON_3dPoint* pt)
{
  if (pConstBezierSurface && pt)
  {
    *pt = pConstBezierSurface->PointAt(u, v);
  }
}

RH_C_FUNCTION ON_NurbsSurface* ON_BezierSurface_GetNurbForm(const ON_BezierSurface* pConstBezierSurface)
{
  ON_NurbsSurface* rc = nullptr;
  if (pConstBezierSurface)
  {
    // Always use static constructor
    rc = ON_NurbsSurface::New();
    if (!pConstBezierSurface->GetNurbForm(*rc))
    {
      delete rc;
      rc = nullptr;
    }
  }
  return rc;
}

RH_C_FUNCTION bool ON_BezierSurface_IsRational(const ON_BezierSurface* pConstBezierSurface)
{
  if (pConstBezierSurface)
    return pConstBezierSurface->IsRational();
  return false;
}

RH_C_FUNCTION int ON_BezierSurface_CVCount(const ON_BezierSurface* pConstBezierSurface, int direction)
{
  if (pConstBezierSurface)
  {
    if (0 == direction)
      return pConstBezierSurface->m_order[0];
    return pConstBezierSurface->m_order[1];
  }
  return 0;
}


RH_C_FUNCTION bool ON_BezierSurface_GetCV3d(const ON_BezierSurface* pConstBezierSurface, int i, int j, ON_3dPoint* point)
{
  bool rc = false;
  if (pConstBezierSurface && point)
    rc = pConstBezierSurface->GetCV(i, j, *point);
  return rc;
}

RH_C_FUNCTION bool ON_BezierSurface_GetCV4d(const ON_BezierSurface* pConstBezierSurface, int i, int j, ON_4dPoint* point)
{
  bool rc = false;
  if (pConstBezierSurface && point)
    rc = pConstBezierSurface->GetCV(i, j, *point);
  return rc;
}

RH_C_FUNCTION bool ON_BezierSurface_MakeRational(ON_BezierSurface* pBezierSurface, bool on)
{
  bool rc = false;
  if (pBezierSurface)
  {
    if (on)
      rc = pBezierSurface->MakeRational();
    else
      rc = pBezierSurface->MakeNonRational();
  }
  return rc;
}

RH_C_FUNCTION bool ON_BezierSurface_Split(const ON_BezierSurface* pConstBezierSurface, int direction, double t, ON_BezierSurface* left, ON_BezierSurface* right)
{
  bool rc = false;
  if (pConstBezierSurface && left && right)
  {
    rc = pConstBezierSurface->Split(direction, t, *left, *right);
  }
  return rc;
}

