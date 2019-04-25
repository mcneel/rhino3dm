#include "bindings.h"

BND_Viewport* BND_ViewInfo::GetViewport() const
{
  return new BND_Viewport(new ON_Viewport(m_view.m_vp), nullptr);
}

void BND_ViewInfo::SetViewport(const BND_Viewport& viewport)
{
  m_view.m_vp = *viewport.m_viewport;
}

BND_RenderSettings::BND_RenderSettings(std::shared_ptr<ONX_Model> m)
{
  m_model = m;
  m_render_settings = &(m_model->m_settings.m_RenderSettings);
}


void BND_RenderSettings::SetTrackedPointer(ON_3dmRenderSettings* renderSettings, const ON_ModelComponentReference* compref)
{
  m_render_settings = renderSettings;
  BND_CommonObject::SetTrackedPointer(renderSettings, compref);
}

BND_RenderSettings::BND_RenderSettings()
{
  SetTrackedPointer(new ON_3dmRenderSettings(), nullptr);
}

BND_RenderSettings::BND_RenderSettings(const BND_RenderSettings& other)
{
  SetTrackedPointer(new ON_3dmRenderSettings(*other.m_render_settings), nullptr);
}


BND_RenderSettings::BND_RenderSettings(ON_3dmRenderSettings* renderSettings, const ON_ModelComponentReference* compref)
{
  SetTrackedPointer(renderSettings, compref);
}

BND_RenderSettings::~BND_RenderSettings()
{
  if (m_model)
    m_render_settings = nullptr;
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
    ;

  py::class_<BND_File3dmSettings>(m, "File3dmSettings")
    .def_property("ModelUrl", &BND_File3dmSettings::GetModelUrl, &BND_File3dmSettings::SetModelUrl)
    .def_property("ModelBasePoint", &BND_File3dmSettings::GetModelBasePoint, &BND_File3dmSettings::SetModelBasePoint)
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
    ;

  class_<BND_File3dmSettings>("File3dmSettings")
    .property("modelUrl", &BND_File3dmSettings::GetModelUrl, &BND_File3dmSettings::SetModelUrl)
    .property("modelBasePoint", &BND_File3dmSettings::GetModelBasePoint, &BND_File3dmSettings::SetModelBasePoint)
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
