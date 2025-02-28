#include "stdafx.h"

RH_C_FUNCTION ON_3dmConstructionPlaneGridDefaults* ON_3dmConstructionPlaneGridDefaults_New()
{
  return new ON_3dmConstructionPlaneGridDefaults();
}

RH_C_FUNCTION void ON_3dmConstructionPlaneGridDefaults_Delete(ON_3dmConstructionPlaneGridDefaults* gridDefaults)
{
  if (gridDefaults)
    delete gridDefaults;
}

RH_C_FUNCTION void ON_3dmConstructionPlaneGridDefaults_Get(const ON_3dmConstructionPlaneGridDefaults* defaults,
  double* gridSpacing, double* snapSpacing,
  int* gridLineCount, int* gridThickFrequency,
  bool* showGrid, bool* showGridAxes, bool* showWorldAxes)
{
  if (nullptr == defaults)
    return;
  if (gridSpacing)
    *gridSpacing = defaults->m_grid_spacing;
  if (snapSpacing)
    *snapSpacing = defaults->m_snap_spacing;
  if (gridLineCount)
    *gridLineCount = defaults->m_grid_line_count;
  if (gridThickFrequency)
    *gridThickFrequency = defaults->m_grid_thick_frequency;
  if (showGrid)
    *showGrid = defaults->m_bShowGrid;
  if (showGridAxes)
    *showGridAxes = defaults->m_bShowGridAxes;
  if (showWorldAxes)
    *showWorldAxes = defaults->m_bShowWorldAxes;
}

RH_C_FUNCTION void ON_3dmConstructionPlaneGridDefaults_Set(ON_3dmConstructionPlaneGridDefaults* defaults,
  double gridSpacing, double snapSpacing,
  int gridLineCount, int gridThickFrequency,
  bool showGrid, bool showGridAxes, bool showWorldAxes)
{
  if (nullptr == defaults)
    return;
  defaults->m_grid_spacing = gridSpacing;
  defaults->m_snap_spacing = snapSpacing;
  defaults->m_grid_line_count = gridLineCount;
  defaults->m_grid_thick_frequency = gridThickFrequency;
  defaults->m_bShowGrid = showGrid;
  defaults->m_bShowGridAxes = showGridAxes;
  defaults->m_bShowWorldAxes = showWorldAxes;
}

RH_C_FUNCTION void ON_3dmConstructionPlane_Copy(const ON_3dmConstructionPlane* pCP, ON_PLANE_STRUCT* plane,
                                                double* grid_spacing, double* snap_spacing,
                                                int* grid_line_count, int* grid_thick_freq,
                                                bool* depthbuffered, CRhCmnStringHolder* pString)
{
  if( pCP )
  {
    if( plane )
      CopyToPlaneStruct(*plane, pCP->m_plane);
    if( grid_spacing )
      *grid_spacing = pCP->m_grid_spacing;
    if( snap_spacing )
      *snap_spacing = pCP->m_snap_spacing;
    if( grid_line_count )
      *grid_line_count = pCP->m_grid_line_count;
    if( grid_thick_freq )
      *grid_thick_freq = pCP->m_grid_thick_frequency;
    if( depthbuffered )
      *depthbuffered = pCP->m_bDepthBuffer;
    if( pString )
      pString->Set(pCP->m_name);
  }
}

RH_C_FUNCTION ON_3dmConstructionPlane* ON_3dmConstructionPlane_New(
  const ON_PLANE_STRUCT* plane,
  double grid_spacing,
  double snap_spacing,
  int grid_line_count,
  int grid_thick_frequency,
  bool depthBuffered,
  const RHMONO_STRING* _name)
{
  ON_3dmConstructionPlane* rc = nullptr;
  if (nullptr != plane)
  {
    rc = new ON_3dmConstructionPlane();
    rc->m_plane = FromPlaneStruct(*plane);
    rc->m_grid_spacing = grid_spacing;
    rc->m_snap_spacing = snap_spacing;
    rc->m_grid_line_count = grid_line_count;
    rc->m_grid_thick_frequency = grid_thick_frequency;
    rc->m_bDepthBuffer = depthBuffered;
    if (nullptr != _name) // 29-Jun-2017 Dale Fugier, name field is optional
    {
      INPUTSTRINGCOERCE(name, _name);
      rc->m_name = name;
    }
  }
  return rc;
}

RH_C_FUNCTION void ON_3dmConstructionPlane_Delete(ON_3dmConstructionPlane* pCPlane)
{
  if( pCPlane )
    delete pCPlane;
}


RH_C_FUNCTION ON_3dmView* ON_3dmView_New(const ON_3dmView* pConstOther3dmView)
{
  if( pConstOther3dmView )
    return new ON_3dmView(*pConstOther3dmView);
  return new ON_3dmView();
}

RH_C_FUNCTION void ON_3dmView_Delete(ON_3dmView* ptr)
{
  if( ptr )
    delete ptr;
}

RH_C_FUNCTION void ON_3dmView_NameGet(const ON_3dmView* pView, CRhCmnStringHolder* pString)
{
  if( pView && pString)
    pString->Set(pView->m_name);
}

RH_C_FUNCTION void ON_3dmView_NameSet(ON_3dmView* pView, const RHMONO_STRING* _name)
{
  INPUTSTRINGCOERCE(name, _name);
  if( pView)
  {
    pView->m_name = name;
  }
}

RH_C_FUNCTION const ON_Viewport* ON_3dmView_ViewportPointer(const ON_3dmView* pView)
{
  const ON_Viewport* rc = nullptr;
  if( pView )
  {
    rc = &(pView->m_vp);
  }
  return rc;
}

RH_C_FUNCTION void ON_3dmView_FocalBlurDistance_Set(ON_3dmView* pView, double blur)
{
	if (pView)
	{
		pView->SetFocalBlurDistance(blur);
	}
}

RH_C_FUNCTION double ON_3dmView_FocalBlurDistance_Get(const ON_3dmView* pView)
{
	if (pView)
	{
		return pView->FocalBlurDistance();
	}
	return 0;
}

RH_C_FUNCTION void ON_3dmView_FocalBlurAperture_Set(ON_3dmView* pView, double blur)
{
	if (pView)
	{
		pView->SetFocalBlurAperture(blur);
	}
}

RH_C_FUNCTION double ON_3dmView_FocalBlurAperture_Get(const ON_3dmView* pView)
{
	if (pView)
	{
		return pView->FocalBlurAperture();
	}
	return 0;
}

RH_C_FUNCTION void ON_3dmView_FocalBlurJitter_Set(ON_3dmView* pView, double blur)
{
	if (pView)
	{
		pView->SetFocalBlurJitter(blur);
	}
}

RH_C_FUNCTION double ON_3dmView_FocalBlurJitter_Get(const ON_3dmView* pView)
{
	if (pView)
	{
		return pView->FocalBlurJitter();
	}
	return 0;
}

RH_C_FUNCTION void ON_3dmView_FocalBlurSampleCount_Set(ON_3dmView* pView, unsigned int sample_count)
{
	if (pView)
	{
		pView->SetFocalBlurSampleCount(sample_count);
	}
}

RH_C_FUNCTION unsigned int ON_3dmView_FocalBlurSampleCount_Get(const ON_3dmView* pView)
{
	if (pView)
	{
		return pView->FocalBlurSampleCount();
	}
	return 0;
}

RH_C_FUNCTION void ON_3dmView_FocalBlurMode_Set(ON_3dmView* pView, unsigned int m)
{
	ON_FocalBlurModes mode = ON_FocalBlurModes::None;
	if (m == 0)
		mode = ON_FocalBlurModes::None;
	if (m == 1)
		mode = ON_FocalBlurModes::Automatic;
	if (m == 2)
		mode = ON_FocalBlurModes::Manual;

	if (pView)
	{
		pView->SetFocalBlurMode(mode);
	}
}

RH_C_FUNCTION unsigned int ON_3dmView_FocalBlurMode_Get(const ON_3dmView* pView)
{
	if (pView)
	{
		return (unsigned int)pView->FocalBlurMode();
	}
	return 0;
}

RH_C_FUNCTION void ON_3dmView_WallpaperGetFilename(const ON_3dmView* pView, CRhCmnStringHolder* pString)
{
  if (pView && pString)
  {
    pString->Set(pView->m_wallpaper_image.m_image_file_reference.FullPath());
  }
}

RH_C_FUNCTION bool ON_3dmView_WallpaperGetHidden(const ON_3dmView* pView)
{
  if (pView)
  {
    return pView->m_wallpaper_image.m_bHidden;
  }

  return false;
}

RH_C_FUNCTION bool ON_3dmView_WallpaperGetGrayScale(const ON_3dmView* pView)
{
  if (pView)
  {
    return pView->m_wallpaper_image.m_bGrayScale;
  }

  return false;
}

RH_C_FUNCTION void ON_3dmView_SetSectionBehavior(ON_3dmView* pView, int behavior)
{
  if (pView)
  {
    pView->SetSectionBehavior(ON::ViewSectionBehaviorFromUnsigned((unsigned char)behavior));
  }
}

RH_C_FUNCTION int ON_3dmView_GetSectionBehavior(const ON_3dmView* pConstView)
{
  if (pConstView)
    return (int)pConstView->SectionBehavior();
  return 0;
}

RH_C_FUNCTION ON_UUID ON_3dmView_NamedViewId(const ON_3dmView* pView)
{
  if (pView)
  {
    return pView->m_named_view_id;
  }

  return ON_nil_uuid;
}

RH_C_FUNCTION void ON_3dmView_GetClippingPlanes(const ON_3dmView* pView, ON_SimpleArray<ON_ClippingPlaneInfo>* array_to_fill)
{
  if (pView && array_to_fill)
  {
    *array_to_fill = pView->m_clipping_planes;
  }
}

RH_C_FUNCTION ON_UUID ON_ClippingPlaneInfo_GetPlaneId(const ON_ClippingPlaneInfo* pClippingPlaneInfo)
{
  if (pClippingPlaneInfo)
  {
    return pClippingPlaneInfo->m_plane_id;
  }
  return ON_nil_uuid;
}

RH_C_FUNCTION bool ON_ClippingPlaneInfo_GetPlane(const ON_ClippingPlaneInfo* pClippingPlaneInfo, ON_PLANE_STRUCT* plane)
{
  if (pClippingPlaneInfo && plane)
  {
    CopyToPlaneStruct(*plane, pClippingPlaneInfo->m_plane_equation);
    return true;
  }
  return false;
}

RH_C_FUNCTION double ON_ClippingPlaneInfo_GetDepth(const ON_ClippingPlaneInfo* pClippingPlaneInfo)
{
  if (pClippingPlaneInfo)
  {
    return pClippingPlaneInfo->Depth();
  }
  return 0.0;
}

RH_C_FUNCTION bool ON_ClippingPlaneInfo_GetDepthEnabled(const ON_ClippingPlaneInfo* pClippingPlaneInfo)
{
  if (pClippingPlaneInfo)
  {
    return pClippingPlaneInfo->DepthEnabled();
  }
  return false;
}




RH_C_FUNCTION ON_EarthAnchorPoint* ON_EarthAnchorPoint_New()
{
  return new ON_EarthAnchorPoint();
}

RH_C_FUNCTION void ON_EarthAnchorPoint_Delete(ON_EarthAnchorPoint* pEarthAnchor)
{
  if( pEarthAnchor )
    delete pEarthAnchor;
}
enum EarthAnchorPointDouble : int {
  EarthBasepointLatitude = 0,
  EarthBasepointLongitude = 1,
  EarthBasepointElevation = 2,
  KMLOrientationHeadingAngleRadians = 3,
  KMLOrientationTiltAngleRadians = 4,
  KMLOrientationRollAngleRadians = 5,
  KMLOrientationHeadingAngleDegrees = 6,
  KMLOrientationTiltAngleDegrees = 7,
  KMLOrientationRollAngleDegrees = 8,
};

RH_C_FUNCTION double ON_EarthAnchorPoint_GetDouble(const ON_EarthAnchorPoint* pConstEarthAnchor, enum EarthAnchorPointDouble which)
{
  double rc = 0;
  if( pConstEarthAnchor )
  {
    if (EarthAnchorPointDouble::EarthBasepointLatitude == which)
      rc = pConstEarthAnchor->Latitude();
    else if (EarthAnchorPointDouble::EarthBasepointLongitude == which)
      rc = pConstEarthAnchor->Longitude();
    else if (EarthAnchorPointDouble::EarthBasepointElevation == which)
      rc = pConstEarthAnchor->ElevationInMeters();
    else if (EarthAnchorPointDouble::KMLOrientationHeadingAngleRadians == which)
      rc = pConstEarthAnchor->KMLOrientationHeadingAngleRadians();
    else if (EarthAnchorPointDouble::KMLOrientationTiltAngleRadians == which)
      rc = pConstEarthAnchor->KMLOrientationTiltAngleRadians();
    else if (EarthAnchorPointDouble::KMLOrientationRollAngleRadians == which)
      rc = pConstEarthAnchor->KMLOrientationRollAngleRadians();
    else if (EarthAnchorPointDouble::KMLOrientationHeadingAngleDegrees == which)
      rc = pConstEarthAnchor->KMLOrientationHeadingAngleDegrees();
    else if (EarthAnchorPointDouble::KMLOrientationTiltAngleDegrees == which)
      rc = pConstEarthAnchor->KMLOrientationTiltAngleDegrees();
    else if (EarthAnchorPointDouble::KMLOrientationRollAngleDegrees == which)
      rc = pConstEarthAnchor->KMLOrientationRollAngleDegrees();
  }
  return rc;
}

RH_C_FUNCTION void ON_EarthAnchorPoint_SetDouble(ON_EarthAnchorPoint* pEarthAnchor, enum EarthAnchorPointDouble which, double val)
{
  if( pEarthAnchor )
  {
    if( EarthAnchorPointDouble::EarthBasepointLatitude==which )
      pEarthAnchor->SetLatitude(val);
    else if( EarthAnchorPointDouble::EarthBasepointLongitude==which )
      pEarthAnchor->SetLongitude(val);
    else if( EarthAnchorPointDouble::EarthBasepointElevation==which )
      pEarthAnchor->SetElevation(ON::LengthUnitSystem::Meters,val);
  }
}

RH_C_FUNCTION int ON_EarthAnchorPoint_GetEarthCoordinateSystem(const ON_EarthAnchorPoint* pConstEarthAnchor)
{
  int rc = 0;
  if (pConstEarthAnchor)
    rc = (int)(static_cast<unsigned int>(pConstEarthAnchor->EarthCoordinateSystem()));
  return rc;
}

RH_C_FUNCTION void ON_EarthAnchorPoint_SetEarthCoordinateSystem(ON_EarthAnchorPoint* pEarthAnchor, int val)
{
  if (pEarthAnchor)
    pEarthAnchor->SetEarthCoordinateSystem(ON::EarthCoordinateSystemFromUnsigned(val));
}

RH_C_FUNCTION int ON_EarthAnchorPoint_GetEarthBasepointElevationZero(const ON_EarthAnchorPoint* pConstEarthAnchor)
{
  int rc = 0;
  if (pConstEarthAnchor)
    rc = (int)(static_cast<unsigned int>(pConstEarthAnchor->EarthCoordinateSystem()));
  return rc;
}

RH_C_FUNCTION void ON_EarthAnchorPoint_SetEarthBasepointElevationZero(ON_EarthAnchorPoint* pEarthAnchor, int val)
{
  if( pEarthAnchor )
    pEarthAnchor->SetEarthCoordinateSystem( ON::EarthCoordinateSystemFromUnsigned(val));
}

RH_C_FUNCTION void ON_EarthAnchorPoint_ModelBasePoint(ON_EarthAnchorPoint* pEarthAnchor, bool set, ON_3dPoint* point)
{
  if( pEarthAnchor && point )
  {
    if( set )
      pEarthAnchor->SetModelPoint(*point);
    else
      *point = pEarthAnchor->ModelPoint();
  }
}

RH_C_FUNCTION void ON_EarthAnchorPoint_ModelDirection(ON_EarthAnchorPoint* pEarthAnchor, bool north, bool set, ON_3dVector* vector)
{
  if( pEarthAnchor && vector )
  {
    if( set )
    {
      if( north )
        pEarthAnchor->SetModelNorth(*vector);
      else
        pEarthAnchor->SetModelEast(*vector);
    }
    else
    {
      *vector = north? pEarthAnchor->ModelNorth() : pEarthAnchor->ModelEast();
    }
  }
}

RH_C_FUNCTION void ON_EarthAnchorPoint_GetString(const ON_EarthAnchorPoint* pConstEarthAnchor, bool name, CRhCmnStringHolder* pString)
{
  if( pConstEarthAnchor && pString )
  {
    if( name )
      pString->Set( pConstEarthAnchor->m_name );
    else
      pString->Set( pConstEarthAnchor->m_description );
  }
}

RH_C_FUNCTION void ON_EarthAnchorPoint_SetString(ON_EarthAnchorPoint* pEarthAnchor, bool name, const RHMONO_STRING* str)
{
  if( pEarthAnchor && str )
  {
    INPUTSTRINGCOERCE(_str, str);
    if( name )
      pEarthAnchor->m_name = _str;
    else
      pEarthAnchor->m_description = _str;
  }
}

RH_C_FUNCTION bool ON_EarthAnchorPoint_EarthLocationIsSet(ON_EarthAnchorPoint* pEarthAnchor)
{
	bool bIsSet = false;
	if (pEarthAnchor)
	{
		bIsSet = pEarthAnchor->EarthLocationIsSet();
	}
	return bIsSet;
}

RH_C_FUNCTION bool ON_EarthAnchorPoint_ModelLocationIsSet(ON_EarthAnchorPoint* pEarthAnchor)
{
  bool bIsSet = false;
  if (pEarthAnchor)
  {
    bIsSet = pEarthAnchor->ModelLocationIsSet();
  }
  return bIsSet;
}

RH_C_FUNCTION void ON_EarthAnchorPoint_GetModelCompass(const ON_EarthAnchorPoint* pConstEarthAnchor, ON_PLANE_STRUCT* plane)
{
  if( pConstEarthAnchor && plane )
  {
    ON_Plane _plane;
    if( pConstEarthAnchor->GetModelCompass(_plane) )
      CopyToPlaneStruct(*plane, _plane);
  }
}

RH_C_FUNCTION void ON_EarthAnchorPoint_GetModelToEarthTransform(const ON_EarthAnchorPoint* pConstEarthAnchor, ON::LengthUnitSystem us, ON_Xform* xform)
{
  if( pConstEarthAnchor && xform )
  {
    pConstEarthAnchor->GetModelToEarthXform(us, *xform);
  }
}

RH_C_FUNCTION void ON_3dmSettings_Delete(ON_3dmSettings* pSettings)
{
  if (pSettings)
    delete pSettings;
}

// note: This object must be deleted with ON_EarthAnchorPoint_Delete
RH_C_FUNCTION ON_EarthAnchorPoint* ON_3dmSettings_GetEarthAnchorPoint(const ON_3dmSettings* pConstSettings)
{
  ON_EarthAnchorPoint* rc = nullptr;
  if (pConstSettings)
    rc = new ON_EarthAnchorPoint(pConstSettings->m_earth_anchor_point);
  return rc;
}

RH_C_FUNCTION void ON_3dmSettings_GetModelUrl(const ON_3dmSettings* pConstSettings, CRhCmnStringHolder* pString)
{
  if( pConstSettings && pString )
    pString->Set(pConstSettings->m_model_URL);
}

RH_C_FUNCTION void ON_3dmSettings_SetModelUrl(ON_3dmSettings* pSettings, const RHMONO_STRING* str)
{
  if( pSettings )
  {
    INPUTSTRINGCOERCE(_str, str);
    pSettings->m_model_URL = _str;
  }
}

RH_C_FUNCTION void ON_3dmSettings_GetModelBasepoint(const ON_3dmSettings* pConstSettings, ON_3dPoint* point)
{
  if( pConstSettings && point )
    *point = pConstSettings->m_model_basepoint;
}

RH_C_FUNCTION void ON_3dmSettings_SetModelBasepoint(ON_3dmSettings* pSettings, ON_3DPOINT_STRUCT point )
{
  if( pSettings )
  {
    ON_3dPoint pt(point.val);
    pSettings->m_model_basepoint = pt;
  }
}

enum UnitsTolerancesSettingsDouble : int {
  ModelAbsTol = 0,
  ModelAngleTol = 1,
  ModelRelTol = 2,
  PageAbsTol = 3,
  PageAngleTol = 4,
  PageRelTol = 5,
};

RH_C_FUNCTION double ON_3dmSettings_GetDouble(const ON_3dmSettings* pConstSettings, enum UnitsTolerancesSettingsDouble which)
{
  double rc = 0;
  if( pConstSettings )
  {
    switch( which )
    {
    case UnitsTolerancesSettingsDouble::ModelAbsTol:
      rc = pConstSettings->m_ModelUnitsAndTolerances.m_absolute_tolerance;
      break;
    case UnitsTolerancesSettingsDouble::ModelAngleTol:
      rc = pConstSettings->m_ModelUnitsAndTolerances.m_angle_tolerance;
      break;
    case UnitsTolerancesSettingsDouble::ModelRelTol:
      rc = pConstSettings->m_ModelUnitsAndTolerances.m_relative_tolerance;
      break;
    case UnitsTolerancesSettingsDouble::PageAbsTol:
      rc = pConstSettings->m_PageUnitsAndTolerances.m_absolute_tolerance;
      break;
    case UnitsTolerancesSettingsDouble::PageAngleTol:
      rc = pConstSettings->m_PageUnitsAndTolerances.m_angle_tolerance;
      break;
    case UnitsTolerancesSettingsDouble::PageRelTol:
      rc = pConstSettings->m_PageUnitsAndTolerances.m_relative_tolerance;
      break;
    }
  }
  return rc;
}

RH_C_FUNCTION void ON_3dmSettings_SetDouble(ON_3dmSettings* pSettings, enum UnitsTolerancesSettingsDouble which, double val)
{
  if( pSettings )
  {
    switch( which )
    {
    case UnitsTolerancesSettingsDouble::ModelAbsTol:
      pSettings->m_ModelUnitsAndTolerances.m_absolute_tolerance = val;
      break;
    case UnitsTolerancesSettingsDouble::ModelAngleTol:
      pSettings->m_ModelUnitsAndTolerances.m_angle_tolerance = val;
      break;
    case UnitsTolerancesSettingsDouble::ModelRelTol:
      pSettings->m_ModelUnitsAndTolerances.m_relative_tolerance = val;
      break;
    case UnitsTolerancesSettingsDouble::PageAbsTol:
      pSettings->m_PageUnitsAndTolerances.m_absolute_tolerance = val;
      break;
    case UnitsTolerancesSettingsDouble::PageAngleTol:
      pSettings->m_PageUnitsAndTolerances.m_angle_tolerance = val;
      break;
    case UnitsTolerancesSettingsDouble::PageRelTol:
      pSettings->m_PageUnitsAndTolerances.m_relative_tolerance = val;
      break;
    }
  }
}

RH_C_FUNCTION int ON_3dmSettings_GetSetUnitSystem(ON_3dmSettings* pSettings, bool model, bool set, int set_val)
{
  int rc = set_val;
  if( pSettings )
  {
    if( set )
    {
      if( model )
        pSettings->m_ModelUnitsAndTolerances.m_unit_system = ON::LengthUnitSystemFromUnsigned(set_val);
      else
        pSettings->m_PageUnitsAndTolerances.m_unit_system = ON::LengthUnitSystemFromUnsigned(set_val);
    }
    else
    {
      if( model )
        rc = (int)static_cast<unsigned int>(pSettings->m_ModelUnitsAndTolerances.m_unit_system.UnitSystem());
      else
        rc = (int)static_cast<unsigned int>(pSettings->m_PageUnitsAndTolerances.m_unit_system.UnitSystem());
    }
  }
  return rc;
}

RH_C_FUNCTION void ON_3dmSettings_SetRenderingSource(ON_3dmRenderSettings* pRenderSettings, int set_val)
{
	if (pRenderSettings)
	{
		pRenderSettings->SetRenderingSource(ON_3dmRenderSettings::RenderingSources(set_val));
	}
}

RH_C_FUNCTION int ON_3dmSettings_GetRenderingSource(const ON_3dmRenderSettings* pRenderSettings)
{
	if (pRenderSettings)
	{
		return (int)pRenderSettings->RenderingSource();
	}
	return 0;
}

RH_C_FUNCTION void ON_3dmSettings_SetNamedView(ON_3dmRenderSettings* pRenderSettings, const ON_wString* pName)
{
	if (pRenderSettings && pName)
	{
		pRenderSettings->SetNamedView(*pName);
	}
}

RH_C_FUNCTION void ON_3dmSettings_GetNamedView(const ON_3dmRenderSettings* pRenderSettings, CRhCmnStringHolder* pSH)
{
	if (pRenderSettings && pSH)
	{
		pSH->Set(pRenderSettings->NamedView());
	}
}

RH_C_FUNCTION void ON_3dmSettings_SetSnapshot(ON_3dmRenderSettings* pRenderSettings, const ON_wString* pName)
{
	if (pRenderSettings && pName)
	{
		pRenderSettings->SetSnapshot(*pName);
	}
}

RH_C_FUNCTION void ON_3dmSettings_GetSnapshot(const ON_3dmRenderSettings* pRenderSettings, CRhCmnStringHolder* pSH)
{
	if (pRenderSettings && pSH)
	{
		pSH->Set(pRenderSettings->Snapshot());
	}
}

RH_C_FUNCTION void ON_3dmSettings_SetSpecificViewport(ON_3dmRenderSettings* pRenderSettings, const ON_wString* pName)
{
	if (pRenderSettings && pName)
	{
		pRenderSettings->SetSpecificViewport(*pName);
	}
}

RH_C_FUNCTION void ON_3dmSettings_GetSpecificViewport(const ON_3dmRenderSettings* pRenderSettings, CRhCmnStringHolder* pSH)
{
	if (pRenderSettings && pSH)
	{
		pSH->Set(pRenderSettings->SpecificViewport());
	}
}

RH_C_FUNCTION ON_3dmRenderSettings* ON_3dmRenderSettings_New(const ON_3dmRenderSettings* other)
{
  if( other )
    return new ON_3dmRenderSettings(*other);
  return new ON_3dmRenderSettings();
}

RH_C_FUNCTION const ON_3dmRenderSettings* ON_3dmRenderSettings_ConstPointer(unsigned int docSerialNumber)
{
#if !defined RHINO3DM_BUILD
  auto* pDoc = CRhinoDoc::FromRuntimeSerialNumber(docSerialNumber);
  if (nullptr != pDoc)
    return &pDoc->Properties().RenderSettings();
#endif

  return nullptr;
}

RH_C_FUNCTION bool ON_3dmRenderSettings_GetRenderEnvironmentOverride(const ON_3dmRenderSettings* rs, int u)
{
  if (nullptr == rs)
    return false;

  const auto usage = ON_3dmRenderSettings::EnvironmentUsage(u);
  return rs->RenderEnvironmentOverride(usage);
}

RH_C_FUNCTION void ON_3dmRenderSettings_SetRenderEnvironmentOverride(ON_3dmRenderSettings* rs, int u, bool on)
{
  if (nullptr != rs)
  {
    const auto usage = ON_3dmRenderSettings::EnvironmentUsage(u);
    rs->SetRenderEnvironmentOverride(usage, on);
  }
}

RH_C_FUNCTION ON_UUID ON_3dmRenderSettings_GetRenderEnvironment(const ON_3dmRenderSettings* rs, int u, int p)
{
  if (nullptr == rs)
    return ON_nil_uuid;

  const auto usage   = ON_3dmRenderSettings::EnvironmentUsage(u);
  const auto purpose = ON_3dmRenderSettings::EnvironmentPurpose(p);

  return rs->RenderEnvironmentId(usage, purpose);
}

RH_C_FUNCTION void ON_3dmRenderSettings_SetRenderEnvironment(ON_3dmRenderSettings* rs, int u, const ON_UUID* id)
{
  if ((nullptr != rs) && (nullptr != id))
  {
    const auto usage = ON_3dmRenderSettings::EnvironmentUsage(u);
    rs->SetRenderEnvironmentId(usage, *id);
  }
}

RH_C_FUNCTION const ON_GroundPlane* ON_3dmRenderSettings_GetGroundPlane(const ON_3dmRenderSettings* rs)
{
  return rs ? &rs->GroundPlane() : nullptr;
}

RH_C_FUNCTION const ON_Dithering* ON_3dmRenderSettings_GetDithering(const ON_3dmRenderSettings* rs)
{
  return rs ? &rs->Dithering() : nullptr;
}

RH_C_FUNCTION const ON_SafeFrame* ON_3dmRenderSettings_GetSafeFrame(const ON_3dmRenderSettings* rs)
{
  return rs ? &rs->SafeFrame() : nullptr;
}

RH_C_FUNCTION const ON_Skylight* ON_3dmRenderSettings_GetSkylight(const ON_3dmRenderSettings* rs)
{
  return rs ? &rs->Skylight() : nullptr;
}

RH_C_FUNCTION const ON_RenderChannels* ON_3dmRenderSettings_GetRenderChannels(const ON_3dmRenderSettings* rs)
{
  return rs ? &rs->RenderChannels() : nullptr;
}

RH_C_FUNCTION const ON_LinearWorkflow* ON_3dmRenderSettings_GetLinearWorkflow(const ON_3dmRenderSettings* rs)
{
  return rs ? &rs->LinearWorkflow() : nullptr;
}

RH_C_FUNCTION const ON_Sun* ON_3dmRenderSettings_GetSun(const ON_3dmRenderSettings* rs)
{
  return rs ? &rs->Sun() : nullptr;
}

RH_C_FUNCTION const ON_PostEffects* ON_3dmRenderSettings_GetPostEffects(const ON_3dmRenderSettings* rs)
{
  return rs ? &rs->PostEffects() : nullptr;
}

///////////////////////////////////////////////////////////////////////////////////////////////////////

RH_C_FUNCTION const ON_3dmRenderSettings* ON_3dmRenderSettings_ConstPointer_ONX_Model(ONX_Model* ptrModel)
{
  if (nullptr == ptrModel)
    return nullptr;

  return &ptrModel->m_settings.m_RenderSettings;
}

RH_C_FUNCTION void ON_3dmRenderSettings_ONX_Model_Commit(const ON_3dmRenderSettings* rs_srce, ONX_Model* ptrModel)
{
  if ((nullptr == rs_srce) || (nullptr == ptrModel))
    return;

  ptrModel->m_settings.m_RenderSettings = *rs_srce;
}

enum RenderSettingColor : int
{
  AmbientLight = 0,
  BackgroundColorTop = 1,
  BackgroundColorBottom = 2,
};

RH_C_FUNCTION int ON_3dmRenderSettings_GetColor(const ON_3dmRenderSettings* pConstRenderSettings, enum RenderSettingColor which)
{
  int rc = 0;
  if( pConstRenderSettings )
  {
    unsigned int abgr=0;
    switch(which)
    {
    case RenderSettingColor::AmbientLight:
      abgr = (unsigned int)(pConstRenderSettings->m_ambient_light);
      break;
    case RenderSettingColor::BackgroundColorTop:
      abgr = (unsigned int)(pConstRenderSettings->m_background_color);
      break;
    case RenderSettingColor::BackgroundColorBottom:
      abgr = (unsigned int)(pConstRenderSettings->m_background_bottom_color);
      break;
    }
    rc = (int)ABGR_to_ARGB(abgr);
  }
  return rc;
}

RH_C_FUNCTION void ON_3dmRenderSettings_SetColor(ON_3dmRenderSettings* pRenderSettings, enum RenderSettingColor which, int argb)
{
  if( pRenderSettings )
  {
    unsigned int abgr = ARGB_to_ABGR(argb);
    switch(which)
    {
    case RenderSettingColor::AmbientLight:
      pRenderSettings->m_ambient_light = abgr;
      break;
    case RenderSettingColor::BackgroundColorTop:
      pRenderSettings->m_background_color = abgr;
      break;
    case RenderSettingColor::BackgroundColorBottom:
      pRenderSettings->m_background_bottom_color = abgr;
      break;
    }
  }
}

#ifdef RDK_RENDER_PRESETS
//RH_C_FUNCTION ON_UUID ON_3dmRenderSettings_GetCurrentRenderPreset(ON_3dmRenderSettings* pSettings)
//{
//  if (nullptr != pSettings)
//    return pSettings->CurrentRenderPreset();
//
//  return ON_nil_uuid;
//}
//
//RH_C_FUNCTION void ON_3dmRenderSettings_SetCurrentRenderPreset(ON_3dmRenderSettings* pSettings, const ON_UUID uuid)
//{
//  if (nullptr != pSettings)
//  {
//    pSettings->SetCurrentRenderPreset(uuid);
//  }
//}
#endif

RH_C_FUNCTION ON::LengthUnitSystem ON_3dmRenderSettings_GetUnitSystem(ON_3dmRenderSettings* pSettings)
{
  ON::LengthUnitSystem us = ON::LengthUnitSystem::None;
  if (nullptr != pSettings)
    us = pSettings->m_image_us;
  return us;
}

RH_C_FUNCTION void ON_3dmRenderSettings_SetUnitSystem(ON_3dmRenderSettings* pSettings, ON::LengthUnitSystem us)
{
  if (nullptr != pSettings)
    pSettings->m_image_us = us;
}

RH_C_FUNCTION double ON_3dmRenderSettings_GetImageDpi(const ON_3dmRenderSettings* pConstRenderSettings)
{
  return (pConstRenderSettings ? pConstRenderSettings->m_image_dpi : 0.0);
}

RH_C_FUNCTION void ON_3dmRenderSettings_SetImageDpi(ON_3dmRenderSettings* pRenderSettings, double value)
{
  if (pRenderSettings) pRenderSettings->m_image_dpi = value;
}

enum RenderSettingBool : int {
  UseHiddenLights = 0,
  DepthCue = 1,
  FlatShade = 2,
  RenderBackFaces = 3,
  RenderPoints = 4,
  RenderCurves = 5,
  RenderIsoparams = 6,
  RenderMeshEdges = 7,
  RenderAnnotation = 8,
  UseViewportSize = 9,
  ScaleBackgroundToFit = 10,
  TransparentBackground = 11,
};

RH_C_FUNCTION bool ON_3dmRenderSettings_GetBool(const ON_3dmRenderSettings* pConstRenderSettings, enum RenderSettingBool which)
{
  bool rc = false;
  if( pConstRenderSettings )
  {
    switch(which)
    {
    case RenderSettingBool::UseHiddenLights:
      rc = pConstRenderSettings->m_bUseHiddenLights?true:false;
      break;
    case RenderSettingBool::DepthCue:
      rc = pConstRenderSettings->m_bDepthCue?true:false;
      break;
    case RenderSettingBool::FlatShade:
      rc = pConstRenderSettings->m_bFlatShade?true:false;
      break;
    case RenderSettingBool::RenderBackFaces:
      rc = pConstRenderSettings->m_bRenderBackfaces?true:false;
      break;
    case RenderSettingBool::RenderPoints:
      rc = pConstRenderSettings->m_bRenderPoints?true:false;
      break;
    case RenderSettingBool::RenderCurves:
      rc = pConstRenderSettings->m_bRenderCurves?true:false;
      break;
    case RenderSettingBool::RenderIsoparams:
      rc= pConstRenderSettings->m_bRenderIsoparams?true:false;
      break;
    case RenderSettingBool::RenderMeshEdges:
      rc = pConstRenderSettings->m_bRenderMeshEdges?true:false;
      break;
    case RenderSettingBool::RenderAnnotation:
      rc = pConstRenderSettings->m_bRenderAnnotation?true:false;
      break;
    case RenderSettingBool::UseViewportSize:
      rc = pConstRenderSettings->m_bCustomImageSize?false:true;
      break;
    case RenderSettingBool::ScaleBackgroundToFit:
      rc = pConstRenderSettings->m_bScaleBackgroundToFit;
      break;
    case RenderSettingBool::TransparentBackground:
      rc = pConstRenderSettings->m_bTransparentBackground;
      break;
    default:
      break;
    }
  }
  return rc;
}

RH_C_FUNCTION void ON_3dmRenderSettings_SetBool(ON_3dmRenderSettings* pRenderSettings, enum RenderSettingBool which, bool b)
{
  if( pRenderSettings )
  {
    switch(which)
    {
    case RenderSettingBool::UseHiddenLights:
      pRenderSettings->m_bUseHiddenLights = b;
      break;
    case RenderSettingBool::DepthCue:
      pRenderSettings->m_bDepthCue = b;
      break;
    case RenderSettingBool::FlatShade:
      pRenderSettings->m_bFlatShade = b;
      break;
    case RenderSettingBool::RenderBackFaces:
      pRenderSettings->m_bRenderBackfaces = b;
      break;
    case RenderSettingBool::RenderPoints:
      pRenderSettings->m_bRenderPoints = b;
      break;
    case RenderSettingBool::RenderCurves:
      pRenderSettings->m_bRenderCurves = b;
      break;
    case RenderSettingBool::RenderIsoparams:
      pRenderSettings->m_bRenderIsoparams = b;
      break;
    case RenderSettingBool::RenderMeshEdges:
      pRenderSettings->m_bRenderMeshEdges = b;
      break;
    case RenderSettingBool::RenderAnnotation:
      pRenderSettings->m_bRenderAnnotation = b;
      break;
    case RenderSettingBool::UseViewportSize:
      pRenderSettings->m_bCustomImageSize = !b;
      break;
    case RenderSettingBool::ScaleBackgroundToFit:
      pRenderSettings->m_bScaleBackgroundToFit = b;
      break;
    case RenderSettingBool::TransparentBackground:
      pRenderSettings->m_bTransparentBackground = b;
      break;
    default:
      break;
    }
  }
}

enum FocalDouble : int {
  BlurDistance = 0,
  BlurAperture = 1,
  BlurJitter = 2,
};



enum RenderSettingInt : int {
  BackgroundStyle = 0,
  AntialiasStyle = 1,
  ShadowmapStyle = 2,
  ShadowmapWidth = 3,
  ShadowmapHeight = 4,
  ImageWidth = 5,
  ImageHeight = 6,
};

RH_C_FUNCTION int ON_3dmRenderSettings_GetInt(const ON_3dmRenderSettings* pConstRenderSettings, enum RenderSettingInt which)
{
  int rc = 0;
  if( pConstRenderSettings )
  {
    switch(which)
    {
    case RenderSettingInt::BackgroundStyle:
      rc = pConstRenderSettings->m_background_style;
      break;
    case RenderSettingInt::AntialiasStyle:
      rc = pConstRenderSettings->m_antialias_style;
      break;
    case RenderSettingInt::ShadowmapStyle:
      rc = pConstRenderSettings->m_shadowmap_style;
      break;
    case RenderSettingInt::ShadowmapWidth:
      rc = pConstRenderSettings->m_shadowmap_width;
      break;
    case RenderSettingInt::ShadowmapHeight:
      rc = pConstRenderSettings->m_shadowmap_height;
      break;
    case RenderSettingInt::ImageWidth:
      rc = pConstRenderSettings->m_image_width;
      break;
    case RenderSettingInt::ImageHeight:
      rc = pConstRenderSettings->m_image_height;
      break;
    default:
      break;
    }
  }
  return rc;
}

RH_C_FUNCTION void ON_3dmRenderSettings_SetInt(ON_3dmRenderSettings* pRenderSettings, enum RenderSettingInt which, int i)
{
  if( pRenderSettings )
  {
    switch(which)
    {
    case RenderSettingInt::BackgroundStyle:
      pRenderSettings->m_background_style = i;
      break;
    case RenderSettingInt::AntialiasStyle:
      pRenderSettings->m_antialias_style = i;
      break;
    case RenderSettingInt::ShadowmapStyle:
      pRenderSettings->m_shadowmap_style = i;
      break;
    case RenderSettingInt::ShadowmapWidth:
      pRenderSettings->m_shadowmap_width = i;
      break;
    case RenderSettingInt::ShadowmapHeight:
      pRenderSettings->m_shadowmap_height = i;
      break;
    case RenderSettingInt::ImageWidth:
      pRenderSettings->m_image_width = i;
      break;
    case RenderSettingInt::ImageHeight:
      pRenderSettings->m_image_height = i;
      break;
    default:
      break;
    }
  }
}



RH_C_FUNCTION ON_3dmAnimationProperties* ON_3dmAnimationProperties_New(const ON_3dmAnimationProperties* pConstOther)
{
  if( pConstOther )
    return new ON_3dmAnimationProperties(*pConstOther);
  return new ON_3dmAnimationProperties();
}

RH_C_FUNCTION const ON_3dmAnimationProperties* ON_3dmAnimationProperties_ConstPointer(unsigned int docSerialNumber)
{
  const ON_3dmAnimationProperties* rc = nullptr;
#if !defined(RHINO3DM_BUILD)
  CRhinoDoc* pDoc = CRhinoDoc::FromRuntimeSerialNumber(docSerialNumber);
  if( pDoc )
    rc = &(pDoc->Properties().AnimationProperties());
#endif
  return rc;
}

RH_C_FUNCTION void ON_3dmAnimationProperties_Delete(ON_3dmAnimationProperties* ptr)
{
  if( ptr )
    delete ptr;
}

enum AnimationPropertiesCaptureTypes : int
{
    Path = 0,
    Turntable = 1,
    Flythrough = 2,
    DaySunStudy = 3,
    SeasonalSunStudy = 4,
    None = 5
};

RH_C_FUNCTION AnimationPropertiesCaptureTypes ON_3dmAnimationProperties_CaptureType(const ON_3dmAnimationProperties* pConst)
{
	if (nullptr == pConst)
		return AnimationPropertiesCaptureTypes::None;

	const ON_3dmAnimationProperties::CaptureTypes type = pConst->CaptureType();
	if (ON_3dmAnimationProperties::CaptureTypes::day_sun_study == type)
	{
		return AnimationPropertiesCaptureTypes::DaySunStudy;
	}
	else
	if (ON_3dmAnimationProperties::CaptureTypes::flythrough == type)
	{
		return AnimationPropertiesCaptureTypes::Flythrough;
	}
	else
	if (ON_3dmAnimationProperties::CaptureTypes::path == type)
	{
		return AnimationPropertiesCaptureTypes::Path;
	}
	else
	if (ON_3dmAnimationProperties::CaptureTypes::seasonal_sun_study == type)
	{
		return AnimationPropertiesCaptureTypes::SeasonalSunStudy;
	}
	else
	if (ON_3dmAnimationProperties::CaptureTypes::turntable == type)
	{
		return AnimationPropertiesCaptureTypes::Turntable;
	}
	else
	{
		return AnimationPropertiesCaptureTypes::None;
	}
}

RH_C_FUNCTION void ON_3dmAnimationProperties_SetCaptureType(ON_3dmAnimationProperties* p, AnimationPropertiesCaptureTypes which)
{
	if (nullptr == p)
		return;

	if (AnimationPropertiesCaptureTypes::DaySunStudy == which)
	{
		p->SetCaptureType(ON_3dmAnimationProperties::CaptureTypes::day_sun_study);
	}
	else
	if (AnimationPropertiesCaptureTypes::Flythrough == which)
	{
		p->SetCaptureType(ON_3dmAnimationProperties::CaptureTypes::flythrough);
	}
	else
	if (AnimationPropertiesCaptureTypes::Path == which)
	{
		p->SetCaptureType(ON_3dmAnimationProperties::CaptureTypes::path);
	}
	else
	if (AnimationPropertiesCaptureTypes::SeasonalSunStudy == which)
	{
		p->SetCaptureType(ON_3dmAnimationProperties::CaptureTypes::seasonal_sun_study);
	}
	else
	if (AnimationPropertiesCaptureTypes::Turntable == which)
	{
		p->SetCaptureType(ON_3dmAnimationProperties::CaptureTypes::turntable);
	}
	else
	{
		p->SetCaptureType(ON_3dmAnimationProperties::CaptureTypes::none);
	}
}

RH_C_FUNCTION void ON_3dmAnimationProperties_FileExtension(const ON_3dmAnimationProperties* pConst, CRhCmnStringHolder* pString)
{
	if (pConst && pString)
		pString->Set(pConst->FileExtension());
}

RH_C_FUNCTION void ON_3dmAnimationProperties_SetFileExtension(ON_3dmAnimationProperties* p, const ON_wString* s)
{

	if (p && s)
	{
		p->SetFileExtension(*s);
	}
}

RH_C_FUNCTION void ON_3dmAnimationProperties_CaptureMethod(const ON_3dmAnimationProperties* pConst, CRhCmnStringHolder* pString)
{
	if (pConst && pString)
		pString->Set(pConst->CaptureMethod());
}

RH_C_FUNCTION void ON_3dmAnimationProperties_SetCaptureMethod(ON_3dmAnimationProperties* p, const ON_wString* s)
{
	if (p && s)
	{
		p->SetCaptureMethod(*s);
	}
}

RH_C_FUNCTION void ON_3dmAnimationProperties_ViewportName(const ON_3dmAnimationProperties* pConst, CRhCmnStringHolder* pString)
{
	if (pConst && pString)
		pString->Set(pConst->ViewportName());
}

RH_C_FUNCTION void ON_3dmAnimationProperties_SetViewportName(ON_3dmAnimationProperties* p, const ON_wString* s)
{
	if (p && s)
	{
		p->SetViewportName(*s);
	}
}

RH_C_FUNCTION void ON_3dmAnimationProperties_HtmlFilename(const ON_3dmAnimationProperties* pConst, CRhCmnStringHolder* pString)
{
	if (pConst && pString)
		pString->Set(pConst->HtmlFilename());
}

RH_C_FUNCTION void ON_3dmAnimationProperties_SetHtmlFilename(ON_3dmAnimationProperties* p, const ON_wString* s)
{
	if (p && s)
	{
		p->SetHtmlFilename(*s);
	}
}

RH_C_FUNCTION ON_UUID ON_3dmAnimationProperties_DisplayMode(const ON_3dmAnimationProperties* pConst)
{
	if (nullptr == pConst)
		return ON_nil_uuid;

	return pConst->DisplayMode();
}

RH_C_FUNCTION void ON_3dmAnimationProperties_SetDisplayMode(ON_3dmAnimationProperties* p,  ON_UUID id)
{
	if (p)
	{
		p->SetDisplayMode(id);
	}
}

RH_C_FUNCTION void ON_3dmAnimationProperties_CameraPoints(const ON_3dmAnimationProperties* pConst, ON_3dPointArray* pPoints)
{
	if ((nullptr == pConst) || (nullptr == pPoints))
		return;

	pPoints->SetCount(0);

	const ON_3dPointArray& a = pConst->CameraPoints();
	if (a.Count() <= 0)
		return;

	*pPoints = a;
}

RH_C_FUNCTION void ON_3dmAnimationProperties_SetCameraPoints(ON_3dmAnimationProperties* p, int count, /*ARRAY*/const ON_3dPoint* points)
{
	if ((nullptr == p) || (nullptr == points))
		return;

	ON_3dPointArray& a = p->CameraPoints();

	if (count <= 0)
	{
		a.Empty();
	}
	else
	{
		
		a.SetCount(count);
		a.Append(count, points);
	}
}

RH_C_FUNCTION void ON_3dmAnimationProperties_TargetPoints(const ON_3dmAnimationProperties* pConst, ON_3dPointArray* pPoints)
{
	if ((nullptr == pConst) || (nullptr == pPoints))
		return;

	pPoints->SetCount(0);

	const ON_3dPointArray& a = pConst->TargetPoints();
	if (a.Count() <= 0)
		return;

	*pPoints = a;
}

RH_C_FUNCTION void ON_3dmAnimationProperties_SetTargetPoints(ON_3dmAnimationProperties* p, int count, /*ARRAY*/const ON_3dPoint* points)
{
	if ((nullptr == p) || (nullptr == points))
		return;

	ON_3dPointArray& a = p->TargetPoints();

	if (count <= 0)
	{
		a.Empty();
	}
	else
	{
		
		a.SetCount(count);
		a.Append(count, points);
	}
}

RH_C_FUNCTION int ON_3dmAnimationProperties_FrameCount(const ON_3dmAnimationProperties* pConst)
{
	if (nullptr == pConst)
		return ON_3dmAnimationProperties::Default.FrameCount();

	return pConst->FrameCount();
}

RH_C_FUNCTION void ON_3dmAnimationProperties_SetFrameCount(ON_3dmAnimationProperties* p,  int i)
{
	if (p)
	{
		p->SetFrameCount(i);
	}
}

RH_C_FUNCTION int ON_3dmAnimationProperties_CurrentFrame(const ON_3dmAnimationProperties* pConst)
{
	if (nullptr == pConst)
		return ON_3dmAnimationProperties::Default.CurrentFrame();

	return pConst->CurrentFrame();
}

RH_C_FUNCTION void ON_3dmAnimationProperties_SetCurrentFrame(ON_3dmAnimationProperties* p,  int i)
{
	if (p)
	{
		p->SetCurrentFrame(i);
	}
}

RH_C_FUNCTION ON_UUID ON_3dmAnimationProperties_CameraPathId(const ON_3dmAnimationProperties* pConst)
{
	if (nullptr == pConst)
		return ON_nil_uuid;

	return pConst->CameraPathId();
}

RH_C_FUNCTION void ON_3dmAnimationProperties_SetCameraPathId(ON_3dmAnimationProperties* p,  ON_UUID id)
{
	if (p)
	{
		p->SetCameraPathId(id);
	}
}

RH_C_FUNCTION ON_UUID ON_3dmAnimationProperties_TargetPathId(const ON_3dmAnimationProperties* pConst)
{
	if (nullptr == pConst)
		return ON_nil_uuid;

	return pConst->TargetPathId();
}

RH_C_FUNCTION void ON_3dmAnimationProperties_SetTargetPathId(ON_3dmAnimationProperties* p,  ON_UUID id)
{
	if (p)
	{
		p->SetTargetPathId(id);
	}
}

RH_C_FUNCTION double ON_3dmAnimationProperties_Latitude(const ON_3dmAnimationProperties* pConst)
{
	if (nullptr == pConst)
		return ON_3dmAnimationProperties::Default.Latitude();

	return pConst->Latitude();
}

RH_C_FUNCTION void ON_3dmAnimationProperties_SetLatitude(ON_3dmAnimationProperties* p,  double d)
{
	if (p)
	{
		p->SetLatitude(d);
	}
}

RH_C_FUNCTION double ON_3dmAnimationProperties_Longitude(const ON_3dmAnimationProperties* pConst)
{
	if (nullptr == pConst)
		return ON_3dmAnimationProperties::Default.Longitude();

	return pConst->Longitude();
}

RH_C_FUNCTION void ON_3dmAnimationProperties_SetLongitude(ON_3dmAnimationProperties* p,  double d)
{
	if (p)
	{
		p->SetLongitude(d);
	}
}

RH_C_FUNCTION double ON_3dmAnimationProperties_NorthAngle(const ON_3dmAnimationProperties* pConst)
{
	if (nullptr == pConst)
		return ON_3dmAnimationProperties::Default.NorthAngle();

	return pConst->NorthAngle();
}

RH_C_FUNCTION void ON_3dmAnimationProperties_SetNorthAngle(ON_3dmAnimationProperties* p,  double d)
{
	if (p)
	{
		p->SetNorthAngle(d);
	}
}

RH_C_FUNCTION int ON_3dmAnimationProperties_StartDay(const ON_3dmAnimationProperties* pConst)
{
	if (nullptr == pConst)
		return ON_3dmAnimationProperties::Default.StartDay();

	return pConst->StartDay();
}

RH_C_FUNCTION void ON_3dmAnimationProperties_SetStartDay(ON_3dmAnimationProperties* p,  int i)
{
	if (p)
	{
		p->SetStartDay(i);
	}
}

RH_C_FUNCTION int ON_3dmAnimationProperties_StartMonth(const ON_3dmAnimationProperties* pConst)
{
	if (nullptr == pConst)
		return ON_3dmAnimationProperties::Default.StartMonth();

	return pConst->StartMonth();
}

RH_C_FUNCTION void ON_3dmAnimationProperties_SetStartMonth(ON_3dmAnimationProperties* p,  int i)
{
	if (p)
	{
		p->SetStartMonth(i);
	}
}

RH_C_FUNCTION int ON_3dmAnimationProperties_StartYear(const ON_3dmAnimationProperties* pConst)
{
	if (nullptr == pConst)
		return ON_3dmAnimationProperties::Default.StartYear();

	return pConst->StartYear();
}

RH_C_FUNCTION void ON_3dmAnimationProperties_SetStartYear(ON_3dmAnimationProperties* p,  int i)
{
	if (p)
	{
		p->SetStartYear(i);
	}
}

RH_C_FUNCTION int ON_3dmAnimationProperties_EndDay(const ON_3dmAnimationProperties* pConst)
{
	if (nullptr == pConst)
		return ON_3dmAnimationProperties::Default.EndDay();

	return pConst->EndDay();
}

RH_C_FUNCTION void ON_3dmAnimationProperties_SetEndDay(ON_3dmAnimationProperties* p,  int i)
{
	if (p)
	{
		p->SetEndDay(i);
	}
}

RH_C_FUNCTION int ON_3dmAnimationProperties_EndMonth(const ON_3dmAnimationProperties* pConst)
{
	if (nullptr == pConst)
		return ON_3dmAnimationProperties::Default.EndMonth();

	return pConst->EndMonth();
}

RH_C_FUNCTION void ON_3dmAnimationProperties_SetEndMonth(ON_3dmAnimationProperties* p,  int i)
{
	if (p)
	{
		p->SetEndMonth(i);
	}
}

RH_C_FUNCTION int ON_3dmAnimationProperties_EndYear(const ON_3dmAnimationProperties* pConst)
{
	if (nullptr == pConst)
		return ON_3dmAnimationProperties::Default.EndYear();

	return pConst->EndYear();
}

RH_C_FUNCTION void ON_3dmAnimationProperties_SetEndYear(ON_3dmAnimationProperties* p,  int i)
{
	if (p)
	{
		p->SetEndYear(i);
	}
}

RH_C_FUNCTION int ON_3dmAnimationProperties_StartHour(const ON_3dmAnimationProperties* pConst)
{
	if (nullptr == pConst)
		return ON_3dmAnimationProperties::Default.StartHour();

	return pConst->StartHour();
}

RH_C_FUNCTION void ON_3dmAnimationProperties_SetStartHour(ON_3dmAnimationProperties* p,  int i)
{
	if (p)
	{
		p->SetStartHour(i);
	}
}

RH_C_FUNCTION int ON_3dmAnimationProperties_StartMinutes(const ON_3dmAnimationProperties* pConst)
{
	if (nullptr == pConst)
		return ON_3dmAnimationProperties::Default.StartMinutes();

	return pConst->StartMinutes();
}

RH_C_FUNCTION void ON_3dmAnimationProperties_SetStartMinutes(ON_3dmAnimationProperties* p,  int i)
{
	if (p)
	{
		p->SetStartMinutes(i);
	}
}

RH_C_FUNCTION int ON_3dmAnimationProperties_StartSeconds(const ON_3dmAnimationProperties* pConst)
{
	if (nullptr == pConst)
		return ON_3dmAnimationProperties::Default.StartSeconds();

	return pConst->StartSeconds();
}

RH_C_FUNCTION void ON_3dmAnimationProperties_SetStartSeconds(ON_3dmAnimationProperties* p,  int i)
{
	if (p)
	{
		p->SetStartSeconds(i);
	}
}

RH_C_FUNCTION int ON_3dmAnimationProperties_EndHour(const ON_3dmAnimationProperties* pConst)
{
	if (nullptr == pConst)
		return ON_3dmAnimationProperties::Default.EndHour();

	return pConst->EndHour();
}

RH_C_FUNCTION void ON_3dmAnimationProperties_SetEndHour(ON_3dmAnimationProperties* p,  int i)
{
	if (p)
	{
		p->SetEndHour(i);
	}
}

RH_C_FUNCTION int ON_3dmAnimationProperties_EndMinutes(const ON_3dmAnimationProperties* pConst)
{
	if (nullptr == pConst)
		return ON_3dmAnimationProperties::Default.EndMinutes();

	return pConst->EndMinutes();
}

RH_C_FUNCTION void ON_3dmAnimationProperties_SetEndMinutes(ON_3dmAnimationProperties* p,  int i)
{
	if (p)
	{
		p->SetEndMinutes(i);
	}
}

RH_C_FUNCTION int ON_3dmAnimationProperties_EndSeconds(const ON_3dmAnimationProperties* pConst)
{
	if (nullptr == pConst)
		return ON_3dmAnimationProperties::Default.EndSeconds();

	return pConst->EndSeconds();
}

RH_C_FUNCTION void ON_3dmAnimationProperties_SetEndSeconds(ON_3dmAnimationProperties* p,  int i)
{
	if (p)
	{
		p->SetEndSeconds(i);
	}
}

RH_C_FUNCTION int ON_3dmAnimationProperties_DaysBetweenFrames(const ON_3dmAnimationProperties* pConst)
{
	if (nullptr == pConst)
		return ON_3dmAnimationProperties::Default.DaysBetweenFrames();

	return pConst->DaysBetweenFrames();
}

RH_C_FUNCTION void ON_3dmAnimationProperties_SetDaysBetweenFrames(ON_3dmAnimationProperties* p,  int i)
{
	if (p)
	{
		p->SetDaysBetweenFrames(i);
	}
}

RH_C_FUNCTION int ON_3dmAnimationProperties_MinutesBetweenFrames(const ON_3dmAnimationProperties* pConst)
{
	if (nullptr == pConst)
		return ON_3dmAnimationProperties::Default.MinutesBetweenFrames();

	return pConst->MinutesBetweenFrames();
}

RH_C_FUNCTION void ON_3dmAnimationProperties_SetMinutesBetweenFrames(ON_3dmAnimationProperties* p,  int i)
{
	if (p)
	{
		p->SetMinutesBetweenFrames(i);
	}
}

RH_C_FUNCTION int ON_3dmAnimationProperties_LightIndex(const ON_3dmAnimationProperties* pConst)
{
	if (nullptr == pConst)
		return ON_3dmAnimationProperties::Default.LightIndex();

	return pConst->LightIndex();
}

RH_C_FUNCTION void ON_3dmAnimationProperties_SetLightIndex(ON_3dmAnimationProperties* p,  int i)
{
	if (p)
	{
		p->SetLightIndex(i);
	}
}

RH_C_FUNCTION void ON_3dmAnimationProperties_HtmlFileName(const ON_3dmAnimationProperties* pConst, CRhCmnStringHolder* pString)
{
	if (pConst && pString)
		pString->Set(pConst->HtmlFilename());
}

RH_C_FUNCTION void ON_3dmAnimationProperties_SetHtmlFileName(ON_3dmAnimationProperties* p, const ON_wString* s)
{
	if (p && s)
	{
		p->SetHtmlFilename(*s);
	}
}

RH_C_FUNCTION void ON_3dmAnimationProperties_FolderName(const ON_3dmAnimationProperties* pConst, CRhCmnStringHolder* pString)
{
	if (pConst && pString)
		pString->Set(pConst->FolderName());
}

RH_C_FUNCTION void ON_3dmAnimationProperties_SetFolderName(ON_3dmAnimationProperties* p, const ON_wString* s)
{
	if (p && s)
	{
		p->SetFolderName(*s);
	}
}

RH_C_FUNCTION void ON_3dmAnimationProperties_Images(const ON_3dmAnimationProperties* p, ON_ClassArray<ON_wString>* pImages)
{
	if ((nullptr == p) || (nullptr == pImages))
		return;

	pImages->SetCount(0);

	const  ON_ClassArray<ON_wString>& a = p->Images();
	if (a.Count() <= 0)
		return;

	*pImages = a;
}

RH_C_FUNCTION void ON_3dmAnimationProperties_SetImages(ON_3dmAnimationProperties* p, const ON_ClassArray<ON_wString>* pImages)
{
	if ((nullptr == p) || (nullptr == pImages))
		return;

	p->Images().SetCount(0);

	if (pImages->Count() <= 0)
		return;

	p->Images() = *pImages;
}

RH_C_FUNCTION void ON_3dmAnimationProperties_Dates(const ON_3dmAnimationProperties* p, ON_ClassArray<ON_wString>* pDates)
{
	if ((nullptr == p) || (nullptr == pDates))
		return;

	pDates->SetCount(0);

	const  ON_ClassArray<ON_wString>& a = p->Dates();
	if (a.Count() <= 0)
		return;

	*pDates = a;
}

RH_C_FUNCTION void ON_3dmAnimationProperties_SetDates(ON_3dmAnimationProperties* p, const ON_ClassArray<ON_wString>* pDates)
{
	if ((nullptr == p) || (nullptr == pDates))
		return;

	p->Dates().SetCount(0);

	if (pDates->Count() <= 0)
		return;

	p->Dates() = *pDates;
}

RH_C_FUNCTION bool ON_3dmAnimationProperties_RenderFull(const ON_3dmAnimationProperties* pConst)
{
	if (nullptr == pConst)
		return ON_3dmAnimationProperties::Default.RenderFull();

	return pConst->RenderFull();
}

RH_C_FUNCTION void ON_3dmAnimationProperties_SetRenderFull(ON_3dmAnimationProperties* p,  bool b)
{
	if (p)
	{
		p->SetRenderFull(b);
	}
}

RH_C_FUNCTION bool ON_3dmAnimationProperties_RenderPreview(const ON_3dmAnimationProperties* pConst)
{
	if (nullptr == pConst)
		return ON_3dmAnimationProperties::Default.RenderPreview();

	return pConst->RenderPreview();
}

RH_C_FUNCTION void ON_3dmAnimationProperties_SetRenderPreview(ON_3dmAnimationProperties* p,  bool b)
{
	if (p)
	{
		p->SetRenderPreview(b);
	}
}
