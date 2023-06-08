
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

BND_File3dmPostEffectTable::BND_File3dmPostEffectTable(ON_PostEffects* peps) : _peps(peps) { }

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
}
#endif

#if defined(ON_WASM_COMPILE)
using namespace emscripten;

void initPostEffectBindings(void*)
{
  class_<BND_File3dmPostEffect>("PostEffect")
    .constructor<>()
    //.constructor<const BND_File3dmPostEffect&>()
    .property("localName", &BND_File3dmPostEffect::LocalName)
    // John C - Not Implemented
    //.property("isVisible", &BND_File3dmPostEffect::IsVisible)
    //.property("isActive", &BND_File3dmPostEffect::IsActive)
    // these cause some errors on compile time
    //.function("getParameter", &BND_File3dmPostEffect::GetParameter)
    //.function("setParameter", &BND_File3dmPostEffect::SetParameter)
    ;
}
#endif
