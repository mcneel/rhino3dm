#include "stdafx.h"
#include "curvedisplay.h"

#if !defined(RHINO3DM_BUILD)
class CRhCmnCurveDisplay
{
public:
  CRhCmnCurveDisplay( const ON_NurbsCurve& nc );
  void Draw( CRhinoDisplayPipeline* pipeline, unsigned int color, int thickness );

private:
  ON_NurbsCurve m_curve;
  CRhinoCacheHandle m_cache;
//  ON_ClassArray<ON_BezierCurve> m_beziers;
};

CRhCmnCurveDisplay::CRhCmnCurveDisplay(const ON_NurbsCurve& nurbs_curve)
{
  m_curve = nurbs_curve;
  /*
  int spancount = nurbs_curve.SpanCount();
  m_beziers.Reserve(spancount);


  ON_4dPoint p;
  const int order = nurbs_curve.Order();
  int i1 = nurbs_curve.CVCount()-1;

  if ( order >= 2 ) 
  {
    for ( int j0 = 0, i = order-2; i < i1; i++, j0++ )
    {
      double t0 = nurbs_curve.Knot(i);
      double t1 = nurbs_curve.Knot(i+1);
      if ( !ON_IsValid(t0) || !ON_IsValid(t1) )
        continue;
      if ( t0 >= t1 )
        continue;
      ON_BezierCurve& bez = m_beziers.AppendNew();
      bez.Create(3,true,order);
      int j;
      for ( j = 0; j < order; j++ ) 
      {
        nurbs_curve.GetCV(j0+j,p);
        if ( !p.IsValid() )
          break;
        bez.SetCV(j,p);
      }
      if ( j < order )
      {
        // bogus bbox is a bogus segment - skip it
        bez.Destroy();
        m_beziers.Remove();
        continue;
      }
      ON_ConvertNurbSpanToBezier(4,order,
                                 bez.m_cv_stride, bez.m_cv,
                                 nurbs_curve.m_knot + j0, t0, t1 );
      ON_BoundingBox bbox;
      bez.GetBBox( bbox.m_min, bbox.m_max );
      if ( !bbox.IsValid() )
      {
        // bogus bbox is a bogus segment - skip it
        bez.Destroy();
        m_beziers.Remove();
        continue;
      }
    }
  }
  */
}

void CRhCmnCurveDisplay::Draw( CRhinoDisplayPipeline* pipeline, unsigned int color, int thickness )
{
  if (nullptr == pipeline)
    return;

  unsigned int alpha = ((color & 0xff000000) >> 24);
  color &= 0x00ffffff;
  ON_Color c = color;
  c.SetAlpha(alpha);
  pipeline->DrawCurve(m_curve, c, (float)thickness, &m_cache);

/*
  // check for bbox visibility first?
  // restore color,thickness if we attempt to change it?

  int count = m_beziers.Count();
  const ON_BezierCurve* pBez = m_beziers.Array();
  CRhCurveAttributes ca;
  engine.GetCurveAttributes(ca);
  // Curve attributes doesn't play along with the ON_Color concept of alpha=0 mean opaque
  unsigned int alpha = ((color & 0xff000000)>>24);
  color &= 0x00ffffff;
  ca.m_Color = color;
  ca.m_Color.SetAlpha(alpha);
  ca.m_nThickness = thickness;
  for( int i=0; i<count; i++ )
  {
    int order = pBez->m_order;
    if( order >=2 )
      engine.DrawBezier( pBez->m_dim, pBez->m_is_rat, order, pBez->m_cv_stride, pBez->m_cv, ca );
    else if (1 == order)
    {
      float diameter = ca.m_nThickness * engine.DpiScale();
      ON_3dPoint pt(pBez->m_cv);
      engine.DrawPoints(1, &pt, RPS_VARIABLE_DOT, ca.m_Color, ca.m_Color, diameter, 0, 0, 0, true, false, nullptr);
    }
    pBez++;
  }
  */
}

RH_C_FUNCTION CRhCmnCurveDisplay* CurveDisplay_FromArcCurve(const ON_ArcCurve* pCurve)
{
  CRhCmnCurveDisplay* rc = nullptr;
  if( pCurve )
  {
    ON_NurbsCurve nc;
    if( pCurve->GetNurbForm(nc) != 0 )
    {
      rc = new CRhCmnCurveDisplay(nc);
    }
  }
  return rc;
}

RH_C_FUNCTION CRhCmnCurveDisplay* CurveDisplay_FromNurbsCurve(const ON_NurbsCurve* pCurve)
{
  CRhCmnCurveDisplay* rc = nullptr;
  if( pCurve )
    rc = new CRhCmnCurveDisplay( *pCurve);

  return rc;
}

RH_C_FUNCTION CRhCmnCurveDisplay* CurveDisplay_FromPolyCurve(const ON_PolyCurve* pCurve)
{
  CRhCmnCurveDisplay* rc = nullptr;
  if( pCurve )
  {
    ON_NurbsCurve nc;
    if( pCurve->GetNurbForm(nc) != 0 )
    {
      rc = new CRhCmnCurveDisplay(nc);
    }
  }
  return rc;
}


RH_C_FUNCTION void CurveDisplay_Delete(CRhCmnCurveDisplay* pCurveDisplay)
{
  if( pCurveDisplay )
    delete pCurveDisplay;
}

RH_C_FUNCTION void ON_Curve_Draw(const ON_Curve* pConstCurve, CRhinoDisplayPipeline* pPipeline,
  int argb, float thickness, int patternCount, /*ARRAY*/const float* pattern, bool patternBySegment,
  float patternOffset, bool patternLengthsInWorld, ON::LineCapStyle capStyle, ON::LineJoinStyle joinStyle,
  float taperPosition, float taperThickness, CRhinoCacheHandle* cacheHandle)
{
  CRhinoDisplayEngine* engine = pPipeline ? pPipeline->Engine() : nullptr;
  if (pConstCurve && engine)
  {
    ON_Color color = ARGB_to_ABGR(argb);
    CRhinoDisplayPen pen(color, thickness);
    pen.SetCapStyle(capStyle);
    pen.SetJoinStyle(joinStyle);
    if (pattern && patternCount>1)
    {
      for (int i = 0; i < patternCount; i++)
        pen.SetPattern(i, pattern[i]);
    }
    pen.SetPatternBySegment(patternBySegment);
    pen.SetPatternOffset(patternOffset);
    pen.SetPatternLengthsInWorldUnits(patternLengthsInWorld);
    if (taperPosition >= 0)
    {
      pen.SetTaperPosition(taperPosition);
      pen.SetTaperThickness(taperThickness);
    }
    engine->DrawCurve(*pConstCurve, pen, cacheHandle);
  }
}

RH_C_FUNCTION void CurveDisplay_Draw(CRhCmnCurveDisplay* pCurveDisplay, CRhinoDisplayPipeline* pPipeline, int argb, int thickness)
{
  if( pCurveDisplay && pPipeline )
  {
    unsigned int abgr = ARGB_to_ABGR(argb);
    pCurveDisplay->Draw(pPipeline, abgr, thickness);
  }
}

RH_C_FUNCTION void ON_PolylineCurve_Draw(const ON_PolylineCurve* pCrv, CRhinoDisplayPipeline* pDisplayPipeline, int argb, int thickness)
{
  if (pCrv && pDisplayPipeline)
  {
    int abgr = ARGB_to_ABGR(argb);
    pDisplayPipeline->DrawPolyline(pCrv->m_pline, abgr, thickness);
  }
}


RH_C_FUNCTION void ON_LineCurve_Draw(const ON_LineCurve* pCrv, CRhinoDisplayPipeline* pDisplayPipeline, int argb, int thickness)
{
  if (pCrv && pDisplayPipeline)
  {
    int abgr = ARGB_to_ABGR(argb);
    pDisplayPipeline->DrawLine(pCrv->m_line.from, pCrv->m_line.to, abgr, thickness);
  }
}

#endif

