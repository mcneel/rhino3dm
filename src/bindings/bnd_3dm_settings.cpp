#include "bindings.h"

BND_Viewport* BND_ViewInfo::GetViewport() const
{
  return new BND_Viewport(new ON_Viewport(m_view.m_vp), nullptr);
}

void BND_ViewInfo::SetViewport(const BND_Viewport& viewport)
{
  m_view.m_vp = *viewport.m_viewport;
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
    .def_property("FocalBlurJitter", &BND_ViewInfo::GetFocalBlurJitter, &BND_ViewInfo::GetFocalBlurJitter)
    .def_property("FocalBlurSampleCount", &BND_ViewInfo::GetFocalBlurSampleCount, &BND_ViewInfo::SetFocalBlurSampleCount)
    .def_property("Viewport", &BND_ViewInfo::GetViewport, &BND_ViewInfo::SetViewport)
    ;
}
#endif

#if defined(ON_WASM_COMPILE)
using namespace emscripten;

void init3dmSettingsBindings(void*)
{
}
#endif
