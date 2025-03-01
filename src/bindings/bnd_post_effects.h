#include "bindings.h"

#pragma once

#if defined(ON_PYTHON_COMPILE)
void initPostEffectBindings(rh3dmpymodule& m);
#else
void initPostEffectBindings(void* m);
#endif

class BND_File3dmPostEffect
{
private:
  class ON_PostEffect* _pep = nullptr;
  bool _owned = false;

public:
  BND_File3dmPostEffect();
  BND_File3dmPostEffect(ON_PostEffect* pep);
  BND_File3dmPostEffect(const BND_File3dmPostEffect& pep);
  ~BND_File3dmPostEffect();

  BND_UUID Id(void) const;
  ON_PostEffect::Types Type(void) const;
  std::wstring LocalName(void) const;
  bool Listable(void) const;
  bool On(void) const;
  void SetOn(bool b);
  bool Shown(void) const;
  void SetShown(bool b);
  std::wstring GetParameter(const wchar_t* n) const;
  bool SetParameter(const wchar_t* n, const std::wstring& v);
};

class BND_File3dmPostEffectTable
{
private:
  ON_PostEffects* _peps = nullptr;
  bool _owned = false;

public:
  BND_File3dmPostEffectTable();
  BND_File3dmPostEffectTable(ON_PostEffects* peps);
  BND_File3dmPostEffectTable(const BND_File3dmPostEffectTable& pet);
  ~BND_File3dmPostEffectTable();

  int Count() const;
  void Add(const BND_File3dmPostEffect& pep);
  BND_File3dmPostEffect* FindIndex(int index);
  BND_File3dmPostEffect* IterIndex(int index); // helper function for iterator
  BND_File3dmPostEffect* FindId(BND_UUID id);
};
