
#include "bindings.h"

#pragma once

#if defined(ON_PYTHON_COMPILE)
#if defined(NANOBIND)
namespace py = nanobind;
void init3dmSettingsBindings(py::module_& m);
#else
namespace py = pybind11;
void init3dmSettingsBindings(py::module& m);
#endif

#else
void init3dmSettingsBindings(void* m);
#endif

class BND_ConstructionPlane
{
  ON_3dmConstructionPlane m_cplane;
public:
  BND_ConstructionPlane() = default;
  BND_Plane GetPlane() const { return BND_Plane::FromOnPlane(m_cplane.m_plane); }
  void SetPlane(const BND_Plane& plane) { m_cplane.m_plane = plane.ToOnPlane(); }
  double GetGridSpacing() const { return m_cplane.m_grid_spacing; }
  void SetGridSpacing(double s) { m_cplane.m_grid_spacing = s; }
  double GetSnapSpacing() const { return m_cplane.m_snap_spacing; }
  void SetSnapSpacing(double s) { m_cplane.m_snap_spacing = s; }
  int GetGridLineCount() const { return m_cplane.m_grid_line_count; }
  void SetGridLineCount(int c) { m_cplane.m_grid_line_count = c; }
  int GetThickLineFrequency() const { return m_cplane.m_grid_thick_frequency; }
  void SetThickLineFrequency(int i) { m_cplane.m_grid_thick_frequency = i; }
  bool IsDepthBuffered() const { return m_cplane.m_bDepthBuffer; }
  void SetDepthBuffered(bool b) { m_cplane.m_bDepthBuffer = b; }
  std::wstring GetName() const { return std::wstring(m_cplane.m_name); }
  void SetName(std::wstring s) { m_cplane.m_name = s.c_str(); }
};

class BND_ViewInfo
{
public:
  ON_3dmView m_view;
public:
  BND_ViewInfo() = default;
  std::wstring GetName() const { return std::wstring(m_view.m_name.Array()); }
  void SetName(std::wstring s) { m_view.m_name = s.c_str(); }
  std::wstring GetWallpaperFilename() const { return std::wstring(m_view.m_wallpaper_image.m_image_file_reference.FullPathAsPointer()); }
  bool ShowWallpaperInGrayScale() const { return m_view.m_wallpaper_image.m_bGrayScale; }
  void SetShowWallpaperInGrayScale(bool b) { m_view.m_wallpaper_image.m_bGrayScale = b; }
  bool WallpaperHidden() const { return m_view.m_wallpaper_image.m_bHidden; }
  void SetWallpaperHidden(bool b) { m_view.m_wallpaper_image.m_bHidden = b; }
  double GetFocalBlurDistance() const { return m_view.FocalBlurDistance(); }
  void SetFocalBlurDistance(double d) { m_view.SetFocalBlurDistance(d); }
  double GetFocalBlurAperture() const { return m_view.FocalBlurAperture(); }
  void SetFocalBlurAperture(double d) { m_view.SetFocalBlurAperture(d); }
  double GetFocalBlurJitter() const { return m_view.FocalBlurJitter(); }
  void SetFocalBlurJitter(double d) { m_view.SetFocalBlurJitter(d); }
  unsigned int GetFocalBlurSampleCount() const { return m_view.FocalBlurSampleCount(); }
  void SetFocalBlurSampleCount(unsigned int i) { m_view.SetFocalBlurSampleCount(i); }
  //public ViewInfoFocalBlurModes FocalBlurMode
  class BND_Viewport* GetViewport() const;
  void SetViewport(const class BND_Viewport& viewport);
};

class BND_RenderSettings : public BND_CommonObject
{
private:
  std::shared_ptr<ONX_Model> m_model;
  ON_3dmRenderSettings* m_render_settings = nullptr;

  BND_File3dmGroundPlane* m_ground_plane = nullptr;
  BND_File3dmSafeFrame* m_safe_frame = nullptr;
  BND_File3dmDithering* m_dithering = nullptr;
  BND_File3dmSkylight* m_skylight = nullptr;
  BND_File3dmLinearWorkflow* m_linear_workflow = nullptr;
  BND_File3dmRenderChannels* m_render_channels = nullptr;
  BND_File3dmRenderEnvironments* m_render_environments = nullptr;
  BND_File3dmSun* m_sun = nullptr;
  BND_File3dmPostEffectTable* m_post_effects = nullptr;

  void Construct();

protected:
  void SetTrackedPointer(ON_3dmRenderSettings* renderSettings, const ON_ModelComponentReference* compref);

public:
  BND_RenderSettings(std::shared_ptr<ONX_Model> m);
  BND_RenderSettings();
  BND_RenderSettings(const BND_RenderSettings& other);
  BND_RenderSettings(ON_3dmRenderSettings* renderSettings, const ON_ModelComponentReference* compref);
  ~BND_RenderSettings();

  BND_Color GetAmbientLight() const { return ON_Color_to_Binding(m_render_settings->m_ambient_light); }
  void SetAmbientLight(const BND_Color& color) { m_render_settings->m_ambient_light = Binding_to_ON_Color(color); }
  BND_Color GetBackgroundColorTop() const { return ON_Color_to_Binding(m_render_settings->m_background_color); }
  void SetBackgroundColorTop(const BND_Color& color) { m_render_settings->m_background_color = Binding_to_ON_Color(color); }
  BND_Color GetBackgroundColorBottom() const { return ON_Color_to_Binding(m_render_settings->m_background_bottom_color); }
  void SetBackgroundColorBottom(const BND_Color& color) { m_render_settings->m_background_bottom_color = Binding_to_ON_Color(color); }
  bool GetUseHiddenLights() const { return m_render_settings->m_bUseHiddenLights; }
  void SetUseHiddenLights(bool b) { m_render_settings->m_bUseHiddenLights = b; }
  bool GetDepthCue() const { return m_render_settings->m_bDepthCue; }
  void SetDepthCue(bool b) { m_render_settings->m_bDepthCue = b; }
  bool GetFlatShade() const { return m_render_settings->m_bFlatShade; }
  void SetFlatShade(bool b) { m_render_settings->m_bFlatShade = b; }
  bool GetRenderBackFaces() const { return m_render_settings->m_bRenderBackfaces; }
  void SetRenderBackFaces(bool b) { m_render_settings->m_bRenderBackfaces = b; }
  bool GetRenderPoints() const { return m_render_settings->m_bRenderPoints; }
  void SetRenderPoints(bool b) { m_render_settings->m_bRenderPoints = b; }
  bool GetRenderCurves() const { return m_render_settings->m_bRenderCurves; }
  void SetRenderCurves(bool b) { m_render_settings->m_bRenderCurves = b; }
  bool GetRenderIsoParams() const { return m_render_settings->m_bRenderIsoparams; }
  void SetRenderIsoParams(bool b) { m_render_settings->m_bRenderIsoparams = b; }
  bool GetRenderMeshEdges() const { return m_render_settings->m_bRenderMeshEdges; }
  void SetRenderMeshEdges(bool b) { m_render_settings->m_bRenderMeshEdges = b; }
  bool GetRenderAnnotations() const { return m_render_settings->m_bRenderAnnotation; }
  void SetRenderAnnotations(bool b) { m_render_settings->m_bRenderAnnotation = b; }
  //AntialiasLevel
  bool GetUseViewportSize() const { return !m_render_settings->m_bCustomImageSize; }
  void SetUseViewportSize(bool b) { m_render_settings->m_bCustomImageSize = !b; }
  bool GetScaleBackgroundToFit() const { return m_render_settings->ScaleBackgroundToFit(); }
  void SetScaleBackgroundToFit(bool b) { m_render_settings->SetScaleBackgroundToFit(b); }
  bool GetTransparentBackground() const { return m_render_settings->m_bTransparentBackground; }
  void SetTransparentBackground(bool b) { m_render_settings->m_bTransparentBackground = b; }
  // ImageUnitSystem
  double GetImageDpi() const { return m_render_settings->m_image_dpi; }
  void SetImageDpi(double d) { m_render_settings->m_image_dpi = d; }
  // ImageSize
  int GetShadowMapLevel() const { return m_render_settings->m_shadowmap_style; }
  void SetShadowMapLevel(int i) { m_render_settings->m_shadowmap_style = i; }
  // BackgroundStyles
  std::wstring GetNamedView() const { return std::wstring(m_render_settings->NamedView().Array()); }
  void SetNamedView(const std::wstring& s) { m_render_settings->SetNamedView(s.c_str()); }
  std::wstring GetSnapShot() const { return std::wstring(m_render_settings->Snapshot().Array()); }
  void SetSnapShot(const std::wstring& s) { m_render_settings->SetSnapshot(s.c_str()); }
  std::wstring GetSpecificViewport() const { return std::wstring(m_render_settings->SpecificViewport().Array()); }
  void SetSpecificViewport(const std::wstring& s) { m_render_settings->SetSpecificViewport(s.c_str()); }

  //RenderSource
  BND_File3dmGroundPlane& GetGroundPlane() const { return *m_ground_plane; }
  BND_File3dmSafeFrame& GetSafeFrame() const { return *m_safe_frame; }
  BND_File3dmDithering& GetDithering() const { return *m_dithering; }
  BND_File3dmSkylight& GetSkylight() const { return *m_skylight; }
  BND_File3dmLinearWorkflow& GetLinearWorkflow() const { return *m_linear_workflow; }
  BND_File3dmRenderChannels& GetRenderChannels() const { return *m_render_channels; }
  BND_File3dmRenderEnvironments& GetRenderEnvironments() const { return *m_render_environments; }
  BND_File3dmSun& GetSun() const { return *m_sun; }
  BND_File3dmPostEffectTable& GetPostEffects() const { return *m_post_effects; }

  //void SetGroundPlane(BND_File3dmGroundPlane& gp) { m_render_settings->GroundPlane = gp; }

};

class BND_EarthAnchorPoint
{
public:
  ON_EarthAnchorPoint m_anchor_point;
public:
  BND_EarthAnchorPoint() = default;

  double EarthBasepointLatitude() const { return m_anchor_point.Latitude(); }
  void SetEarthBasepointLatitude(double d) { m_anchor_point.SetLatitude(d); }
  double EarthBasepointLongitude() const { return m_anchor_point.Longitude(); }
  void SetEarthBasepointLongitude(double d) { m_anchor_point.SetLongitude(d); }
  double EarthBasepointElevation() const { return m_anchor_point.ElevationInMeters(); }
  void SetEarthBasepointElevation(double d) { m_anchor_point.SetElevation(ON::LengthUnitSystem::Meters, d); }
  ON::EarthCoordinateSystem EarthBasepointElevationZero() const { return m_anchor_point.EarthCoordinateSystem(); }
  void SetEarthBasepointElevationZero(ON::EarthCoordinateSystem cs) { m_anchor_point.SetEarthCoordinateSystem(cs); }
  ON_3dPoint ModelBasePoint() const { return m_anchor_point.ModelPoint(); }
  void SetModelBasePoint(const ON_3dPoint& pt) { m_anchor_point.SetModelPoint(pt); }
  ON_3dVector ModelNorth() const { return m_anchor_point.ModelNorth(); }
  void SetModelNorth(const ON_3dVector& v) { m_anchor_point.SetModelNorth(v); }
  ON_3dVector ModelEast() const { return m_anchor_point.ModelEast(); }
  void SetModelEast(const ON_3dVector& v) { m_anchor_point.SetModelEast(v); }
  std::wstring Name() const { return std::wstring(m_anchor_point.m_name); }
  void SetName(const std::wstring& name) { m_anchor_point.m_name = name.c_str(); }
  std::wstring Description() const { return std::wstring(m_anchor_point.m_description); }
  void SetDescription(const std::wstring& desc) { m_anchor_point.m_description = desc.c_str(); }
  bool EarthLocationIsSet() const { return m_anchor_point.EarthLocationIsSet(); }
  BND_Plane GetModelCompass() const;
  BND_Transform GetModelToEarthTransform(ON::LengthUnitSystem modelUnitSystem) const;
  //BND_TUPLE GetEarthAnchorPlane() const;
};

class BND_File3dmSettings
{
  std::shared_ptr<ONX_Model> m_model;
public:
  BND_File3dmSettings(std::shared_ptr<ONX_Model> m) { m_model = m; }
  std::wstring GetModelUrl() const { return std::wstring(m_model->m_settings.m_model_URL); }
  void SetModelUrl(const std::wstring& s) { m_model->m_settings.m_model_URL = s.c_str(); }
  ON_3dPoint GetModelBasePoint() const { return m_model->m_settings.m_model_basepoint; }
  void SetModelBasePoint(const ON_3dPoint& pt) { m_model->m_settings.m_model_basepoint = pt; }

  BND_EarthAnchorPoint GetEarthAnchorPoint() const;
  void SetEarthAnchorPoint(const BND_EarthAnchorPoint& anchorPoint);

  double GetModelAbsoluteTolerance() const { return m_model->m_settings.m_ModelUnitsAndTolerances.m_absolute_tolerance; }
  void SetModelAbsoluteTolerance(double t) { m_model->m_settings.m_ModelUnitsAndTolerances.m_absolute_tolerance = t; }
  double GetModelAngleToleranceRadians() const { return m_model->m_settings.m_ModelUnitsAndTolerances.m_angle_tolerance; }
  void SetModelAngleToleranceRadians(double t) { m_model->m_settings.m_ModelUnitsAndTolerances.m_angle_tolerance = t; }
  double GetModelAngleToleranceDegrees() const { return ON_DegreesFromRadians(m_model->m_settings.m_ModelUnitsAndTolerances.m_angle_tolerance); }
  void SetModelAngleToleranceDegrees(double t) { m_model->m_settings.m_ModelUnitsAndTolerances.m_angle_tolerance = ON_RadiansFromDegrees(t); }
  double GetModelRelativeTolerance() const { return m_model->m_settings.m_ModelUnitsAndTolerances.m_relative_tolerance; }
  void SetModelRelativeTolerance(double t) { m_model->m_settings.m_ModelUnitsAndTolerances.m_relative_tolerance = t; }

  double GetPageAbsoluteTolerance() const { return m_model->m_settings.m_PageUnitsAndTolerances.m_absolute_tolerance; }
  void SetPageAbsoluteTolerance(double t) { m_model->m_settings.m_PageUnitsAndTolerances.m_absolute_tolerance = t; }
  double GetPageAngleToleranceRadians() const { return m_model->m_settings.m_PageUnitsAndTolerances.m_angle_tolerance; }
  void SetPageAngleToleranceRadians(double t) { m_model->m_settings.m_PageUnitsAndTolerances.m_angle_tolerance = t; }
  double GetPageAngleToleranceDegrees() const { return ON_DegreesFromRadians(m_model->m_settings.m_PageUnitsAndTolerances.m_angle_tolerance); }
  void SetPageAngleToleranceDegrees(double t) { m_model->m_settings.m_PageUnitsAndTolerances.m_angle_tolerance = ON_RadiansFromDegrees(t); }
  double GetPageRelativeTolerance() const { return m_model->m_settings.m_PageUnitsAndTolerances.m_relative_tolerance; }
  void SetPageRelativeTolerance(double t) { m_model->m_settings.m_PageUnitsAndTolerances.m_relative_tolerance = t; }

  ON::LengthUnitSystem GetModelUnitSystem() const { return m_model->m_settings.m_ModelUnitsAndTolerances.m_unit_system.UnitSystem(); }
  void SetModelUnitSystem(ON::LengthUnitSystem us) { m_model->m_settings.m_ModelUnitsAndTolerances.m_unit_system.SetUnitSystem(us); }

  ON::LengthUnitSystem GetPageUnitSystem() const { return m_model->m_settings.m_PageUnitsAndTolerances.m_unit_system.UnitSystem(); }
  void SetPageUnitSystem(ON::LengthUnitSystem us) { m_model->m_settings.m_PageUnitsAndTolerances.m_unit_system.SetUnitSystem(us); }

  BND_RenderSettings GetRenderSettings() { return BND_RenderSettings(m_model); }
};
