
#include "bindings.h"

#pragma once

#if defined(ON_PYTHON_COMPILE)
void initPostEffectBindings(pybind11::module& m);
#else
void initPostEffectBindings(void* m);
#endif

class BND_File3dmPostEffect : public BND_ModelComponent
{
public:
  ON_PostEffect* m_post_effect = nullptr;

protected:
  void SetTrackedPointer(ON_PostEffect* pep, const ON_ModelComponentReference* compref);

public:
  BND_File3dmPostEffect();
  BND_File3dmPostEffect(const BND_File3dmPostEffect& other);
  BND_File3dmPostEffect(ON_PostEffect* pep, const ON_ModelComponentReference* compref);

  ON_PostEffect::Types Type(void) const { return m_post_effect->Type(); }
  std::wstring LocalName(void) const { return std::wstring(static_cast<const wchar_t*>(m_post_effect->LocalName())); }
  bool IsVisible(void) const { return m_post_effect->IsVisible(); }
  bool IsActive(void) const { return m_post_effect->IsActive(); }
  std::wstring GetParameter(const wchar_t* n) const { return static_cast<const wchar_t*>(m_post_effect->GetParameter(n).AsString()); }
  bool SetParameter(const wchar_t* n, const std::wstring& v) { return m_post_effect->SetParameter(n, v.c_str()); }
};
