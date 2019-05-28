#include "stdafx.h"

RH_C_FUNCTION bool ON_DetailView_GetBool(const ON_DetailView* pConstDetail, int which)
{
  const int idxIsParallelProjection = 0;
  const int idxIsPerspectiveProjection = 1;
  const int idxIsProjectionLocked = 2;

  bool rc = false;
  if( pConstDetail )
  {
    if( idxIsParallelProjection == which )
      rc = pConstDetail->m_view.m_vp.Projection()==ON::parallel_view;
    else if( idxIsPerspectiveProjection == which )
      rc = pConstDetail->m_view.m_vp.Projection()==ON::perspective_view;
    else if( idxIsProjectionLocked == which )
      rc = pConstDetail->m_view.m_bLockedProjection;
  }
  return rc;
}

RH_C_FUNCTION void ON_DetailView_SetBool(ON_DetailView* pDetail, int which, bool val)
{
  const int idxIsParallelProjection = 0;
  const int idxIsPerspectiveProjection = 1;
  const int idxIsProjectionLocked = 2;
  if( pDetail )
  {
    if( idxIsParallelProjection == which )
    {
      if( val )
        pDetail->m_view.m_vp.SetProjection(ON::parallel_view);
      else
        pDetail->m_view.m_vp.SetProjection(ON::perspective_view);
    }
    else if( idxIsPerspectiveProjection == which )
    {
      if( val )
        pDetail->m_view.m_vp.SetProjection(ON::perspective_view);
      else
        pDetail->m_view.m_vp.SetProjection(ON::parallel_view);
    }
    else if( idxIsProjectionLocked == which )
      pDetail->m_view.m_bLockedProjection = val;
  }
}

RH_C_FUNCTION double ON_DetailView_GetPageToModelRatio(const ON_DetailView* pConstDetail)
{
  double rc = 0;
  if( pConstDetail )
  {
    rc = pConstDetail->m_page_per_model_ratio;
  }
  return rc;
}

RH_C_FUNCTION bool ON_DetailView_SetScale(ON_DetailView* pDetail, double model_length, int modelUnitSystem, double paper_length, int pageUnitSystem)
{
  bool rc = false;
  ON::LengthUnitSystem model_units = ON::LengthUnitSystemFromUnsigned(modelUnitSystem);
  ON::LengthUnitSystem paper_units = ON::LengthUnitSystemFromUnsigned(pageUnitSystem);
  if( pDetail &&
      pDetail->m_view.m_vp.Projection()==ON::parallel_view &&
      model_units != ON::LengthUnitSystem::None &&
      paper_units != ON::LengthUnitSystem::None )
  {
    double model_length_mm = ::fabs( model_length * ON::UnitScale(model_units, ON::LengthUnitSystem::Millimeters ) );
    double paper_length_mm = ::fabs( paper_length * ON::UnitScale(paper_units, ON::LengthUnitSystem::Millimeters ) );
    if( model_length_mm <= ON_ZERO_TOLERANCE || paper_length_mm <= ON_ZERO_TOLERANCE )
      return false;

    pDetail->m_page_per_model_ratio = paper_length_mm / model_length_mm;
    
    // This is necessary so that dimension history updates get the right
    // world to page xform when detail scale change triggers history replay
    rc = pDetail->UpdateFrustum(model_units, paper_units);
  }
  return rc;
}
