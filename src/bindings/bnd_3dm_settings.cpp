
#include "bindings.h"

BND_Viewport* BND_ViewInfo::GetViewport() const
{
  return new BND_Viewport(new ON_Viewport(m_view.m_vp), nullptr);
}

void BND_ViewInfo::SetViewport(const BND_Viewport& viewport)
{
  m_view.m_vp = *viewport.m_viewport;
}

BND_RenderSettings::BND_RenderSettings()
{
  SetTrackedPointer(new ON_3dmRenderSettings, nullptr);
  Construct();
}

BND_RenderSettings::BND_RenderSettings(std::shared_ptr<ONX_Model> m)
{
  m_model = m;
  m_render_settings = &m_model->m_settings.m_RenderSettings;

  Construct();
}

BND_RenderSettings::BND_RenderSettings(const BND_RenderSettings& other)
{
  SetTrackedPointer(new ON_3dmRenderSettings(*other.m_render_settings), nullptr);
  Construct();
}

BND_RenderSettings::BND_RenderSettings(ON_3dmRenderSettings* renderSettings, const ON_ModelComponentReference* compref)
{
  SetTrackedPointer(renderSettings, compref);
  Construct();
}

BND_RenderSettings::~BND_RenderSettings()
{
  delete m_ground_plane;
  delete m_safe_frame;
  delete m_dithering;
  delete m_skylight;
  delete m_linear_workflow;
  delete m_render_channels;
  delete m_render_environments;
  delete m_sun;
  delete m_post_effects;

  if (m_model)
    m_render_settings = nullptr;
}

void BND_RenderSettings::Construct()
{
  m_ground_plane        = new BND_File3dmGroundPlane       (&m_render_settings->GroundPlane());
  m_safe_frame          = new BND_File3dmSafeFrame         (&m_render_settings->SafeFrame());
  m_dithering           = new BND_File3dmDithering         (&m_render_settings->Dithering());
  m_skylight            = new BND_File3dmSkylight          (&m_render_settings->Skylight());
  m_linear_workflow     = new BND_File3dmLinearWorkflow    (&m_render_settings->LinearWorkflow());
  m_render_channels     = new BND_File3dmRenderChannels    (&m_render_settings->RenderChannels());
  m_render_environments = new BND_File3dmRenderEnvironments( m_render_settings);
  m_sun                 = new BND_File3dmSun               (&m_render_settings->Sun());
  m_post_effects        = new BND_File3dmPostEffectTable   (&m_render_settings->PostEffects());
}

void BND_RenderSettings::SetTrackedPointer(ON_3dmRenderSettings* rs, const ON_ModelComponentReference* compref)
{
  m_render_settings = rs;

  BND_CommonObject::SetTrackedPointer(rs, compref);
}

BND_Plane BND_EarthAnchorPoint::GetModelCompass() const
{
  ON_Plane compass;
  if (m_anchor_point.GetModelCompass(compass))
    return BND_Plane::FromOnPlane(compass);
  return BND_Plane::Unset();
}

BND_Transform BND_EarthAnchorPoint::GetModelToEarthTransform(ON::LengthUnitSystem modelUnitSystem) const
{
  ON_Xform xform;
  if (m_anchor_point.GetModelToEarthXform(modelUnitSystem, xform))
    return BND_Transform(xform);
  return BND_Transform(ON_Xform::Unset);
}

BND_EarthAnchorPoint BND_File3dmSettings::GetEarthAnchorPoint() const
{
  BND_EarthAnchorPoint rc;
  rc.m_anchor_point = m_model->m_settings.m_earth_anchor_point;
  return rc;
}

void BND_File3dmSettings::SetEarthAnchorPoint(const BND_EarthAnchorPoint& anchorPoint)
{
  m_model->m_settings.m_earth_anchor_point = anchorPoint.m_anchor_point;
}

#if defined(ON_PYTHON_COMPILE)
namespace py = pybind11;
void init3dmSettingsBindings(pybind11::module& m)
{
  py::class_<BND_ConstructionPlane>(m, "ConstructionPlane")
    .def(py::init<>())
    .def_property("Plane", &BND_ConstructionPlane::GetPlane, &BND_ConstructionPlane::SetPlane)
    .def_property("GridSpacing", &BND_ConstructionPlane::GetGridSpacing, &BND_ConstructionPlane::SetGridSpacing)
    .def_property("SnapSpacing", &BND_ConstructionPlane::GetSnapSpacing, &BND_ConstructionPlane::SetSnapSpacing)
    .def_property("GridLineCount", &BND_ConstructionPlane::GetGridLineCount, &BND_ConstructionPlane::SetGridLineCount)
    .def_property("ThickLineFrequency", &BND_ConstructionPlane::GetThickLineFrequency, &BND_ConstructionPlane::SetThickLineFrequency)
    .def_property("DepthBuffered", &BND_ConstructionPlane::IsDepthBuffered, &BND_ConstructionPlane::SetDepthBuffered)
    .def_property("Name", &BND_ConstructionPlane::GetName, &BND_ConstructionPlane::SetName)
    ;

  py::class_<BND_ViewInfo>(m, "ViewInfo")
    .def(py::init<>())
    .def_property("Name", &BND_ViewInfo::GetName, &BND_ViewInfo::SetName)
    .def_property_readonly("WallpaperName", &BND_ViewInfo::GetWallpaperFilename)
    .def_property("ShowWallpaperInGrayScale", &BND_ViewInfo::ShowWallpaperInGrayScale, &BND_ViewInfo::SetShowWallpaperInGrayScale)
    .def_property("WallpaperHidden", &BND_ViewInfo::WallpaperHidden, &BND_ViewInfo::SetWallpaperHidden)
    .def_property("FocalBlurDistance", &BND_ViewInfo::GetFocalBlurDistance, &BND_ViewInfo::SetFocalBlurDistance)
    .def_property("FocalBlurAperture", &BND_ViewInfo::GetFocalBlurAperture, &BND_ViewInfo::SetFocalBlurAperture)
    .def_property("FocalBlurJitter", &BND_ViewInfo::GetFocalBlurJitter, &BND_ViewInfo::SetFocalBlurJitter)
    .def_property("FocalBlurSampleCount", &BND_ViewInfo::GetFocalBlurSampleCount, &BND_ViewInfo::SetFocalBlurSampleCount)
    .def_property("Viewport", &BND_ViewInfo::GetViewport, &BND_ViewInfo::SetViewport)
    ;

  py::class_<BND_RenderSettings, BND_CommonObject>(m, "RenderSettings")
    .def(py::init<>())
    .def(py::init<const BND_RenderSettings&>(), py::arg("other"))
    .def_property("AmbientLight", &BND_RenderSettings::GetAmbientLight, &BND_RenderSettings::SetAmbientLight)
    .def_property("BackgroundColorTop", &BND_RenderSettings::GetBackgroundColorTop, &BND_RenderSettings::SetBackgroundColorTop)
    .def_property("BackgroundColorBottom", &BND_RenderSettings::GetBackgroundColorBottom, &BND_RenderSettings::SetBackgroundColorBottom)
    .def_property("UseHiddenLights", &BND_RenderSettings::GetUseHiddenLights, &BND_RenderSettings::SetUseHiddenLights)
    .def_property("DepthCue", &BND_RenderSettings::GetDepthCue, &BND_RenderSettings::SetDepthCue)
    .def_property("FlatShade", &BND_RenderSettings::GetFlatShade, &BND_RenderSettings::SetFlatShade)
    .def_property("RenderBackFaces", &BND_RenderSettings::GetRenderBackFaces, &BND_RenderSettings::SetRenderBackFaces)
    .def_property("RenderPoints", &BND_RenderSettings::GetRenderPoints, &BND_RenderSettings::SetRenderPoints)
    .def_property("RenderCurves", &BND_RenderSettings::GetRenderCurves, &BND_RenderSettings::SetRenderCurves)
    .def_property("RenderIsoParams", &BND_RenderSettings::GetRenderIsoParams, &BND_RenderSettings::SetRenderIsoParams)
    .def_property("RenderMeshEdges", &BND_RenderSettings::GetRenderMeshEdges, &BND_RenderSettings::SetRenderMeshEdges)
    .def_property("RenderAnnotations", &BND_RenderSettings::GetRenderAnnotations, &BND_RenderSettings::SetRenderAnnotations)
    .def_property("UseViewportSize", &BND_RenderSettings::GetUseViewportSize, &BND_RenderSettings::SetUseViewportSize)
    .def_property("ScaleBackgroundToFit", &BND_RenderSettings::GetScaleBackgroundToFit, &BND_RenderSettings::SetScaleBackgroundToFit)
    .def_property("TransparentBackground", &BND_RenderSettings::GetTransparentBackground, &BND_RenderSettings::SetTransparentBackground)
    .def_property("ImageDpi", &BND_RenderSettings::GetImageDpi, &BND_RenderSettings::SetImageDpi)
    .def_property("ShadowMapLevel", &BND_RenderSettings::GetShadowMapLevel, &BND_RenderSettings::SetShadowMapLevel)
    .def_property("NamedView", &BND_RenderSettings::GetNamedView, &BND_RenderSettings::SetNamedView)
    .def_property("SnapShot", &BND_RenderSettings::GetSnapShot, &BND_RenderSettings::SetSnapShot)
    .def_property("SpecificViewport", &BND_RenderSettings::GetSpecificViewport, &BND_RenderSettings::SetSpecificViewport)
    .def_property_readonly("GroundPlane", &BND_RenderSettings::GetGroundPlane)
    .def_property_readonly("SafeFrame", &BND_RenderSettings::GetSafeFrame)
    .def_property_readonly("Dithering", &BND_RenderSettings::GetDithering)
    .def_property_readonly("Skylight", &BND_RenderSettings::GetSkylight)
    .def_property_readonly("LinearWorkflow", &BND_RenderSettings::GetLinearWorkflow)
    .def_property_readonly("RenderChannels", &BND_RenderSettings::GetRenderChannels)
    .def_property_readonly("Sun", &BND_RenderSettings::GetSun)
    .def_property_readonly("RenderEnvironments", &BND_RenderSettings::GetRenderEnvironments)
    .def_property_readonly("PostEffects", &BND_RenderSettings::GetPostEffects)
    ;

  py::class_<BND_EarthAnchorPoint>(m, "EarthAnchorPoint")
    .def_property("EarthBasepointLatitude", &BND_EarthAnchorPoint::EarthBasepointLatitude, &BND_EarthAnchorPoint::SetEarthBasepointLatitude)
    .def_property("EarthBasepointLongitude", &BND_EarthAnchorPoint::EarthBasepointLongitude, &BND_EarthAnchorPoint::SetEarthBasepointLongitude)
    .def_property("EarthBasepointElevation", &BND_EarthAnchorPoint::EarthBasepointElevation, &BND_EarthAnchorPoint::SetEarthBasepointElevation)
    .def_property("EarthBasepointElevationZero", &BND_EarthAnchorPoint::EarthBasepointElevationZero, &BND_EarthAnchorPoint::SetEarthBasepointElevationZero)
    .def_property("ModelBasePoint", &BND_EarthAnchorPoint::ModelBasePoint, &BND_EarthAnchorPoint::SetModelBasePoint)
    .def_property("ModelNorth", &BND_EarthAnchorPoint::ModelNorth, &BND_EarthAnchorPoint::SetModelNorth)
    .def_property("ModelEast", &BND_EarthAnchorPoint::ModelEast, &BND_EarthAnchorPoint::SetModelEast)
    .def_property("Name", &BND_EarthAnchorPoint::Name, &BND_EarthAnchorPoint::SetName)
    .def_property("Description", &BND_EarthAnchorPoint::Description, &BND_EarthAnchorPoint::SetDescription)
    .def("EarthLocationIsSet", &BND_EarthAnchorPoint::EarthLocationIsSet)
    .def("GetModelCompass", &BND_EarthAnchorPoint::GetModelCompass)
    .def("GetModelToEarthTransform", &BND_EarthAnchorPoint::GetModelToEarthTransform, py::arg("modelUnitSystem"))
    ;

  py::class_<BND_File3dmSettings>(m, "File3dmSettings")
    .def_property("ModelUrl", &BND_File3dmSettings::GetModelUrl, &BND_File3dmSettings::SetModelUrl)
    .def_property("ModelBasePoint", &BND_File3dmSettings::GetModelBasePoint, &BND_File3dmSettings::SetModelBasePoint)
    .def_property("EarthAnchorPoint", &BND_File3dmSettings::GetEarthAnchorPoint, &BND_File3dmSettings::SetEarthAnchorPoint)
    .def_property("ModelAbsoluteTolerance", &BND_File3dmSettings::GetModelAbsoluteTolerance, &BND_File3dmSettings::SetModelAbsoluteTolerance)
    .def_property("ModelAngleToleranceRadians", &BND_File3dmSettings::GetModelAngleToleranceRadians, &BND_File3dmSettings::SetModelAngleToleranceRadians)
    .def_property("ModelAngleToleranceDegrees", &BND_File3dmSettings::GetModelAngleToleranceDegrees, &BND_File3dmSettings::SetModelAngleToleranceDegrees)
    .def_property("ModelRelativeTolerance", &BND_File3dmSettings::GetModelRelativeTolerance, &BND_File3dmSettings::SetModelRelativeTolerance)
    .def_property("PageAbsoluteTolerance", &BND_File3dmSettings::GetPageAbsoluteTolerance, &BND_File3dmSettings::SetPageAbsoluteTolerance)
    .def_property("PageAngleToleranceRadians", &BND_File3dmSettings::GetPageAngleToleranceRadians, &BND_File3dmSettings::SetPageAngleToleranceRadians)
    .def_property("PageAngleToleranceDegrees", &BND_File3dmSettings::GetPageAngleToleranceDegrees, &BND_File3dmSettings::SetPageAngleToleranceDegrees)
    .def_property("PageRelativeTolerance", &BND_File3dmSettings::GetPageRelativeTolerance, &BND_File3dmSettings::SetPageRelativeTolerance)
    .def_property("ModelUnitSystem", &BND_File3dmSettings::GetModelUnitSystem, &BND_File3dmSettings::SetModelUnitSystem)
    .def_property("PageUnitSystem", &BND_File3dmSettings::GetPageUnitSystem, &BND_File3dmSettings::SetPageUnitSystem)
    .def_property_readonly("RenderSettings", &BND_File3dmSettings::GetRenderSettings)
    ;
}
#endif

#if defined(ON_WASM_COMPILE)
using namespace emscripten;

void init3dmSettingsBindings(void*)
{
  class_<BND_ConstructionPlane>("ConstructionPlane")
    .constructor<>()
    .property("plane", &BND_ConstructionPlane::GetPlane, &BND_ConstructionPlane::SetPlane)
    .property("gridSpacing", &BND_ConstructionPlane::GetGridSpacing, &BND_ConstructionPlane::SetGridSpacing)
    .property("snapSpacing", &BND_ConstructionPlane::GetSnapSpacing, &BND_ConstructionPlane::SetSnapSpacing)
    .property("gridLineCount", &BND_ConstructionPlane::GetGridLineCount, &BND_ConstructionPlane::SetGridLineCount)
    .property("thickLineFrequency", &BND_ConstructionPlane::GetThickLineFrequency, &BND_ConstructionPlane::SetThickLineFrequency)
    .property("depthBuffered", &BND_ConstructionPlane::IsDepthBuffered, &BND_ConstructionPlane::SetDepthBuffered)
    .property("name", &BND_ConstructionPlane::GetName, &BND_ConstructionPlane::SetName)
    ;

  class_<BND_ViewInfo>("ViewInfo")
    .property("name", &BND_ViewInfo::GetName, &BND_ViewInfo::SetName)
    .property("wallpaperName", &BND_ViewInfo::GetWallpaperFilename)
    .property("showWallpaperInGrayScale", &BND_ViewInfo::ShowWallpaperInGrayScale, &BND_ViewInfo::SetShowWallpaperInGrayScale)
    .property("wallpaperHidden", &BND_ViewInfo::WallpaperHidden, &BND_ViewInfo::SetWallpaperHidden)
    .property("focalBlurDistance", &BND_ViewInfo::GetFocalBlurDistance, &BND_ViewInfo::SetFocalBlurDistance)
    .property("focalBlurAperture", &BND_ViewInfo::GetFocalBlurAperture, &BND_ViewInfo::SetFocalBlurAperture)
    .property("focalBlurJitter", &BND_ViewInfo::GetFocalBlurJitter, &BND_ViewInfo::SetFocalBlurJitter)
    .property("focalBlurSampleCount", &BND_ViewInfo::GetFocalBlurSampleCount, &BND_ViewInfo::SetFocalBlurSampleCount)
    //.property("viewport", &BND_ViewInfo::GetViewport, &BND_ViewInfo::SetViewport, allow_raw_pointers())
    ;

  class_<BND_RenderSettings, base<BND_CommonObject>>("RenderSettings")
    .constructor<>()
    .property("ambientLight", &BND_RenderSettings::GetAmbientLight, &BND_RenderSettings::SetAmbientLight)
    .property("backgroundColorTop", &BND_RenderSettings::GetBackgroundColorTop, &BND_RenderSettings::SetBackgroundColorTop)
    .property("backgroundColorBottom", &BND_RenderSettings::GetBackgroundColorBottom, &BND_RenderSettings::SetBackgroundColorBottom)
    .property("useHiddenLights", &BND_RenderSettings::GetUseHiddenLights, &BND_RenderSettings::SetUseHiddenLights)
    .property("depthCue", &BND_RenderSettings::GetDepthCue, &BND_RenderSettings::SetDepthCue)
    .property("flatShade", &BND_RenderSettings::GetFlatShade, &BND_RenderSettings::SetFlatShade)
    .property("renderBackFaces", &BND_RenderSettings::GetRenderBackFaces, &BND_RenderSettings::SetRenderBackFaces)
    .property("renderPoints", &BND_RenderSettings::GetRenderPoints, &BND_RenderSettings::SetRenderPoints)
    .property("renderCurves", &BND_RenderSettings::GetRenderCurves, &BND_RenderSettings::SetRenderCurves)
    .property("renderIsoParams", &BND_RenderSettings::GetRenderIsoParams, &BND_RenderSettings::SetRenderIsoParams)
    .property("renderMeshEdges", &BND_RenderSettings::GetRenderMeshEdges, &BND_RenderSettings::SetRenderMeshEdges)
    .property("renderAnnotations", &BND_RenderSettings::GetRenderAnnotations, &BND_RenderSettings::SetRenderAnnotations)
    .property("useViewportSize", &BND_RenderSettings::GetUseViewportSize, &BND_RenderSettings::SetUseViewportSize)
    .property("scaleBackgroundToFit", &BND_RenderSettings::GetScaleBackgroundToFit, &BND_RenderSettings::SetScaleBackgroundToFit)
    .property("transparentBackground", &BND_RenderSettings::GetTransparentBackground, &BND_RenderSettings::SetTransparentBackground)
    .property("imageDpi", &BND_RenderSettings::GetImageDpi, &BND_RenderSettings::SetImageDpi)
    .property("shadowMapLevel", &BND_RenderSettings::GetShadowMapLevel, &BND_RenderSettings::SetShadowMapLevel)
    .property("namedView", &BND_RenderSettings::GetNamedView, &BND_RenderSettings::SetNamedView)
    .property("snapShot", &BND_RenderSettings::GetSnapShot, &BND_RenderSettings::SetSnapShot)
    .property("specificViewport", &BND_RenderSettings::GetSpecificViewport, &BND_RenderSettings::SetSpecificViewport)
    .property("groundPlane", &BND_RenderSettings::GetGroundPlane)
    .property("safeFrame", &BND_RenderSettings::GetSafeFrame)
    .property("dithering", &BND_RenderSettings::GetDithering)
    .property("skylight", &BND_RenderSettings::GetSkylight)
    .property("linearWorkflow", &BND_RenderSettings::GetLinearWorkflow)
    .property("renderChannels", &BND_RenderSettings::GetRenderChannels)
    .property("sun", &BND_RenderSettings::GetSun)
    .property("renderEnvironments", &BND_RenderSettings::GetRenderEnvironments)
    .property("postEffects", &BND_RenderSettings::GetPostEffects)
    ;

  class_<BND_EarthAnchorPoint>("EarthAnchorPoint")
    .property("earthBasepointLatitude", &BND_EarthAnchorPoint::EarthBasepointLatitude, &BND_EarthAnchorPoint::SetEarthBasepointLatitude)
    .property("earthBasepointLongitude", &BND_EarthAnchorPoint::EarthBasepointLongitude, &BND_EarthAnchorPoint::SetEarthBasepointLongitude)
    .property("earthBasepointElevation", &BND_EarthAnchorPoint::EarthBasepointElevation, &BND_EarthAnchorPoint::SetEarthBasepointElevation)
    .property("earthBasepointElevationZero", &BND_EarthAnchorPoint::EarthBasepointElevationZero, &BND_EarthAnchorPoint::SetEarthBasepointElevationZero)
    .property("modelBasePoint", &BND_EarthAnchorPoint::ModelBasePoint, &BND_EarthAnchorPoint::SetModelBasePoint)
    .property("modelNorth", &BND_EarthAnchorPoint::ModelNorth, &BND_EarthAnchorPoint::SetModelNorth)
    .property("modelEast", &BND_EarthAnchorPoint::ModelEast, &BND_EarthAnchorPoint::SetModelEast)
    .property("name", &BND_EarthAnchorPoint::Name, &BND_EarthAnchorPoint::SetName)
    .property("description", &BND_EarthAnchorPoint::Description, &BND_EarthAnchorPoint::SetDescription)
    .function("earthLocationIsSet", &BND_EarthAnchorPoint::EarthLocationIsSet)
    .function("getModelCompass", &BND_EarthAnchorPoint::GetModelCompass)
    .function("getModelToEarthTransform", &BND_EarthAnchorPoint::GetModelToEarthTransform)
    ;

  class_<BND_File3dmSettings>("File3dmSettings")
    .property("modelUrl", &BND_File3dmSettings::GetModelUrl, &BND_File3dmSettings::SetModelUrl)
    .property("modelBasePoint", &BND_File3dmSettings::GetModelBasePoint, &BND_File3dmSettings::SetModelBasePoint)
    .property("earthAnchorPoint", &BND_File3dmSettings::GetEarthAnchorPoint, &BND_File3dmSettings::SetEarthAnchorPoint)
    .property("modelAbsoluteTolerance", &BND_File3dmSettings::GetModelAbsoluteTolerance, &BND_File3dmSettings::SetModelAbsoluteTolerance)
    .property("modelAngleToleranceRadians", &BND_File3dmSettings::GetModelAngleToleranceRadians, &BND_File3dmSettings::SetModelAngleToleranceRadians)
    .property("modelAngleToleranceDegrees", &BND_File3dmSettings::GetModelAngleToleranceDegrees, &BND_File3dmSettings::SetModelAngleToleranceDegrees)
    .property("modelRelativeTolerance", &BND_File3dmSettings::GetModelRelativeTolerance, &BND_File3dmSettings::SetModelRelativeTolerance)
    .property("pageAbsoluteTolerance", &BND_File3dmSettings::GetPageAbsoluteTolerance, &BND_File3dmSettings::SetPageAbsoluteTolerance)
    .property("pageAngleToleranceRadians", &BND_File3dmSettings::GetPageAngleToleranceRadians, &BND_File3dmSettings::SetPageAngleToleranceRadians)
    .property("pageAngleToleranceDegrees", &BND_File3dmSettings::GetPageAngleToleranceDegrees, &BND_File3dmSettings::SetPageAngleToleranceDegrees)
    .property("pageRelativeTolerance", &BND_File3dmSettings::GetPageRelativeTolerance, &BND_File3dmSettings::SetPageRelativeTolerance)
    .property("modelUnitSystem", &BND_File3dmSettings::GetModelUnitSystem, &BND_File3dmSettings::SetModelUnitSystem)
    .property("pageUnitSystem", &BND_File3dmSettings::GetPageUnitSystem, &BND_File3dmSettings::SetPageUnitSystem)
    .function("renderSettings", &BND_File3dmSettings::GetRenderSettings)
    ;
}
#endif
