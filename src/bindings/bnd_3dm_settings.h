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
  std::wstring GetName() const { return std::wstring(m_cplane.m_name.Array()); }
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

