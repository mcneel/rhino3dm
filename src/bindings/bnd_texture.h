
#pragma once

#include "bindings.h"

#if defined(ON_PYTHON_COMPILE)
void initEnvironmentBindings(pybind11::module& m);
void initTextureBindings(pybind11::module& m);
#else
void initEnvironmentBindings(void* m);
void initTextureBindings(void* m);
#endif

class BND_Texture : public BND_CommonObject
{
public:
  ON_Texture* m_texture = nullptr;
public:
  BND_Texture();
  BND_Texture(ON_Texture* texture, const ON_ModelComponentReference* compref);

  std::wstring GetFileName() const { return std::wstring(m_texture->m_image_file_reference.FullPathAsPointer()); }
  void SetFileName(std::wstring path) { m_texture->m_image_file_reference.SetFullPath(path.c_str(), true); }
  class BND_FileReference* GetFileReference() const;
  //public Guid Id
  //public bool Enabled
  //public TextureType TextureType
  //public int MappingChannelId
  //public TextureCombineMode TextureCombineMode
  int WrapU() const { return (unsigned int) m_texture->m_wrapu; }
  int WrapV() const { return (unsigned int) m_texture->m_wrapv; }
  int WrapW() const { return (unsigned int) m_texture->m_wrapw; }
  BND_Transform UvwTransform() const { return m_texture->m_uvw; }
  //public void GetAlphaBlendValues(out double constant, out double a0, out double a1, out double a2, out double a3)
  //public void SetAlphaBlendValues(double constant, double a0, double a1, double a2, double a3)
  //public void SetRGBBlendValues(Color color, double a0, double a1, double a2, double a3)

protected:
  void SetTrackedPointer(ON_Texture* texture, const ON_ModelComponentReference* compref);
};

class BND_Environment : public BND_CommonObject
{
public:
  ON_Environment* m_env = nullptr;

public:
  BND_Environment();
  BND_Environment(ON_Environment* env, const ON_ModelComponentReference* compref);

  BND_Color BackgroundColor() const;
  void SetBackgroundColor(BND_Color col);

  BND_Texture* BackgroundImage() const;
  void SetBackgroundImage(const BND_Texture& tex);

  ON_Environment::BackgroundProjections BackgroundProjection() const;
  void SetBackgroundProjection(int p);

protected:
  void SetTrackedPointer(ON_Environment* env, const ON_ModelComponentReference* compref);
};
