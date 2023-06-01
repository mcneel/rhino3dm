
#pragma once

#include "bindings.h"

#if defined(ON_PYTHON_COMPILE)
void initPostEffectBindings(pybind11::module& m);
#else
void initPostEffectBindings(void* m);
#endif

class BND_File3dmPostEffect
{
private:
  class ON_PostEffect* _pep = nullptr;
  //bool _owned = false;

public:
  BND_File3dmPostEffect() = default;
  BND_File3dmPostEffect(ON_PostEffect* pep);
  //BND_File3dmPostEffect(ON_PostEffect* pep) : _pep(pep) { } // TODO implement in .cpp

  /*
  BND_File3dmPostEffect(const BND_File3dmPostEffect& pep) { _pep = new ON_PostEffect(*pep._pep); _owned = true; }
  BND_File3dmPostEffect(ON_PostEffect* pep) : _pep(pep) { }
  ~BND_File3dmPostEffect() { if (_owned) delete _pep; }
  */

  BND_UUID Id(void) const { return ON_UUID_to_Binding(_pep ? _pep->Id() : ON_nil_uuid); }
  ON_PostEffect::Types Type(void) const { return _pep ? _pep->Type() : ON_PostEffect::Types::Unset; }
  std::wstring LocalName(void) const { return std::wstring(_pep ? static_cast<const wchar_t*>(_pep->LocalName()) : L""); }
  bool Listable(void) const { return _pep ? _pep->Type() != ON_PostEffect::Types::ToneMapping : false; }
  bool On(void) const { return _pep ? _pep->On() : false; }
  void SetOn(bool b) { if (_pep) _pep->SetOn(b); }
  bool Shown(void) const { return _pep ? _pep->Shown() : false; }
  void SetShown(bool b) { if (_pep) _pep->SetShown(b); }
  std::wstring GetParameter(const wchar_t* n) const { return _pep ? static_cast<const wchar_t*>(_pep->GetParameter(n).AsString()) : L""; }
  bool SetParameter(const wchar_t* n, const std::wstring& v) { return _pep ? _pep->SetParameter(n, v.c_str()) : false; }
};

class BND_File3dmPostEffectTable
{
private:
  ON_PostEffects* _peps = nullptr;
  bool _owned = false;

public:
  BND_File3dmPostEffectTable() = default;
  BND_File3dmPostEffectTable(ON_PostEffects* peps);

  /*
  BND_File3dmPostEffectTable() { _peps = new ON_PostEffects; _owned = true; }
  BND_File3dmPostEffectTable(const BND_File3dmPostEffectTable& pet) { _peps = new ON_PostEffects(*pet._peps); _owned = true; }
  BND_File3dmPostEffectTable(ON_PostEffects* peps) : _peps(peps) { }
  ~BND_File3dmPostEffectTable() { if (_owned) delete _peps; }
  */

  int Count() const;
  void Add(const BND_File3dmPostEffect& pep);
  BND_File3dmPostEffect* FindIndex(int index);
  BND_File3dmPostEffect* IterIndex(int index); // helper function for iterator
  BND_File3dmPostEffect* FindId(BND_UUID id);
};
