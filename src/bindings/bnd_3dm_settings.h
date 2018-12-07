#include "bindings.h"

#pragma once

#if defined(ON_PYTHON_COMPILE)
void init3dmSettingsBindings(pybind11::module& m);
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

class BND_File3dmSettings
{
  std::shared_ptr<ONX_Model> m_model;
public:
  BND_File3dmSettings(std::shared_ptr<ONX_Model> m) { m_model = m; }
  std::wstring GetModelUrl() const { return std::wstring(m_model->m_settings.m_model_URL); }
  void SetModelUrl(const std::wstring& s) { m_model->m_settings.m_model_URL = s.c_str(); }
  ON_3dPoint GetModelBasePoint() const { return m_model->m_settings.m_model_basepoint; }
  void SetModelBasePoint(const ON_3dPoint& pt) { m_model->m_settings.m_model_basepoint = pt; }

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

};