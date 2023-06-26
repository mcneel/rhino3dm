
#include "bindings.h"

BND_File3dmPostEffect::BND_File3dmPostEffect()
{
}

BND_File3dmPostEffect::BND_File3dmPostEffect(ON_PostEffect* pep)
: _pep(pep)
{
}

BND_File3dmPostEffect::BND_File3dmPostEffect(const BND_File3dmPostEffect& pep) 
{ 
  _pep = new ON_PostEffect(*pep._pep);
  _owned = true;
}

BND_File3dmPostEffect::~BND_File3dmPostEffect()
{
  if (_owned)
  {
    delete _pep;
  }
}

BND_UUID BND_File3dmPostEffect::Id(void) const
{
  return ON_UUID_to_Binding(_pep ? _pep->Id() : ON_nil_uuid);
}

ON_PostEffect::Types BND_File3dmPostEffect::Type(void) const
{
  return _pep ? _pep->Type() : ON_PostEffect::Types::Unset;
}

std::wstring BND_File3dmPostEffect::LocalName(void) const
{
  return std::wstring(_pep ? static_cast<const wchar_t*>(_pep->LocalName()) : L"");
}

bool BND_File3dmPostEffect::Listable(void) const
{
  return _pep ? _pep->Type() != ON_PostEffect::Types::ToneMapping : false;
}

bool BND_File3dmPostEffect::On(void) const
{
  return _pep ? _pep->On() : false;
}

void BND_File3dmPostEffect::SetOn(bool b)
{
  if (_pep)
  {
    _pep->SetOn(b);
  }
}

bool BND_File3dmPostEffect::Shown(void) const
{
  return _pep ? _pep->Shown() : false;
}

void BND_File3dmPostEffect::SetShown(bool b)
{
  if (_pep)
  {
    _pep->SetShown(b);
  }
}

std::wstring BND_File3dmPostEffect::GetParameter(const wchar_t* n) const
{
  return _pep ? static_cast<const wchar_t*>(_pep->GetParameter(n).AsString()) : L"";
}

bool BND_File3dmPostEffect::SetParameter(const wchar_t* n, const std::wstring& v)
{
  return _pep ? _pep->SetParameter(n, v.c_str()) : false;
}

BND_File3dmPostEffectTable::BND_File3dmPostEffectTable()
{
  _peps = new ON_PostEffects();
  _owned = true;
}

BND_File3dmPostEffectTable::BND_File3dmPostEffectTable(const BND_File3dmPostEffectTable& pet)
{
  _peps = new ON_PostEffects(*pet._peps);
  _owned = true;
}

BND_File3dmPostEffectTable::BND_File3dmPostEffectTable(ON_PostEffects* peps)
: _peps(peps)
{
}

BND_File3dmPostEffectTable::~BND_File3dmPostEffectTable()
{
  if (_owned)
  {
    delete _peps;
  }
}

void BND_File3dmPostEffectTable::Add(const BND_File3dmPostEffect& pep)
{
  ON_PostEffectParams params;

  const auto id = Binding_to_ON_UUID(pep.Id());
  const ON_wString name = pep.LocalName().c_str();

  _peps->AddPostEffect(pep.Type(), id, name, params, pep.Listable(), pep.On(), pep.Shown());
}

BND_File3dmPostEffect* BND_File3dmPostEffectTable::FindIndex(int index)
{
  ON_SimpleArray<ON_PostEffect*> a;
  _peps->GetPostEffects(a);

  if ((index < 0) || (index >= a.Count()))
    return nullptr;

  return new BND_File3dmPostEffect(a[index]);
}

BND_File3dmPostEffect* BND_File3dmPostEffectTable::IterIndex(int index)
{
  return FindIndex(index);
}

BND_File3dmPostEffect* BND_File3dmPostEffectTable::FindId(BND_UUID id)
{
  const ON_UUID pep_id = Binding_to_ON_UUID(id);

  auto* pep = _peps->PostEffectFromId(pep_id);
  if (nullptr == pep)
    return nullptr;

  return new BND_File3dmPostEffect(pep);
}

int BND_File3dmPostEffectTable::Count() const
{
  ON_SimpleArray<const ON_PostEffect*> a;
  _peps->GetPostEffects(a);

  return a.Count();
}

//////////////////////////////////////////////////////////////////////////////

#if defined(ON_PYTHON_COMPILE)
namespace py = pybind11;
void initPostEffectBindings(pybind11::module& m)
{
  py::class_<BND_File3dmPostEffect>(m, "PostEffect")
    .def(py::init<>())
    //.def(py::init<const BND_File3dmPostEffect&>(), py::arg("other"))
    .def_property_readonly("Id", &BND_File3dmPostEffect::Id)
    .def_property_readonly("Type", &BND_File3dmPostEffect::Type)
    .def_property_readonly("LocalName", &BND_File3dmPostEffect::LocalName)
    .def_property_readonly("Listable", &BND_File3dmPostEffect::Listable)
    .def_property("On", &BND_File3dmPostEffect::On, &BND_File3dmPostEffect::SetOn)
    .def_property("Shown", &BND_File3dmPostEffect::Shown, &BND_File3dmPostEffect::SetShown)
    .def("GetParameter", &BND_File3dmPostEffect::GetParameter)
    .def("SetParameter", &BND_File3dmPostEffect::SetParameter)
    ;

  py::class_<BND_File3dmPostEffectTable>(m, "PostEffectTable")
    .def(py::init<>())
    .def("__len__", &BND_File3dmPostEffectTable::Count)
    .def("Add", &BND_File3dmPostEffectTable::Add)
    .def("FindIndex", &BND_File3dmPostEffectTable::FindIndex)
    .def("FindId", &BND_File3dmPostEffectTable::FindId)
    ;
}
#endif

#if defined(ON_WASM_COMPILE)
using namespace emscripten;

void initPostEffectBindings(void*)
{
  class_<BND_File3dmPostEffect>("PostEffect")
    .constructor<>()
    //.constructor<const BND_File3dmPostEffect&>()
    .property("id", &BND_File3dmPostEffect::Id)
    .property("type", &BND_File3dmPostEffect::Type)
    .property("localName", &BND_File3dmPostEffect::LocalName)
    .property("listable", &BND_File3dmPostEffect::Listable)
    .property("on", &BND_File3dmPostEffect::On, &BND_File3dmPostEffect::SetOn)
    .property("shown", &BND_File3dmPostEffect::Shown, &BND_File3dmPostEffect::SetShown)
    .function("getParameter", &BND_File3dmPostEffect::GetParameter, allow_raw_pointers())
    .function("setParameter", &BND_File3dmPostEffect::SetParameter, allow_raw_pointers())
    ;
  
  class_<BND_File3dmPostEffectTable>("PostEffectTable")
    .constructor<>()
    .property("count", &BND_File3dmPostEffectTable::Count)
    .function("add", &BND_File3dmPostEffectTable::Add)
    .function("findIndex", &BND_File3dmPostEffectTable::FindIndex)
    .function("findId", &BND_File3dmPostEffectTable::FindId)
    ;

}
#endif
