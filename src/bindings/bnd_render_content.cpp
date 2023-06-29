
#include "bindings.h"

BND_File3dmRenderContent::BND_File3dmRenderContent(ON_RenderContent* rc)
{
  SetTrackedPointer(rc, nullptr);
}

BND_File3dmRenderContent::BND_File3dmRenderContent(ON_RenderContent* rc, const ON_ModelComponentReference* compref)
{
  SetTrackedPointer(rc, compref);
}

BND_File3dmRenderContent::BND_File3dmRenderContent(const BND_File3dmRenderContent& other)
{
  SetTrackedPointer(new ON_RenderContent(*other._rc), nullptr);
}

BND_File3dmRenderContent::~BND_File3dmRenderContent()
{
  delete _parent;
  delete _first_child;
  delete _next_sibling;
  delete _top_level;
}

void BND_File3dmRenderContent::SetTrackedPointer(ON_RenderContent* rc, const ON_ModelComponentReference* compref)
{
  _rc = rc;

  BND_ModelComponent::SetTrackedPointer(rc, compref);
}

std::wstring BND_File3dmRenderContent::Kind(void) const
{
  return std::wstring(static_cast<const wchar_t*>(_rc->Kind()));
}

std::wstring BND_File3dmRenderContent::TypeName(void) const
{
  return std::wstring(static_cast<const wchar_t*>(_rc->TypeName()));
}

void BND_File3dmRenderContent::SetTypeName(const std::wstring& s)
{
  _rc->SetTypeName(s.c_str());
}

BND_UUID BND_File3dmRenderContent::TypeId(void) const
{
  return ON_UUID_to_Binding(_rc->TypeId());
}

void BND_File3dmRenderContent::SetTypeId(const BND_UUID& u)
{
  _rc->SetTypeId(Binding_to_ON_UUID(u));
}

BND_UUID BND_File3dmRenderContent::RenderEngineId(void) const
{
  return ON_UUID_to_Binding(_rc->RenderEngineId());
}

void BND_File3dmRenderContent::SetRenderEngineId(const BND_UUID& u)
{
  _rc->SetRenderEngineId(Binding_to_ON_UUID(u));
}

BND_UUID BND_File3dmRenderContent::PlugInId(void) const
{
  return ON_UUID_to_Binding(_rc->PlugInId());
}

void BND_File3dmRenderContent::SetPlugInId(const BND_UUID& u)
{
  _rc->SetPlugInId(Binding_to_ON_UUID(u));
}

std::wstring BND_File3dmRenderContent::Notes(void) const
{
  return std::wstring(static_cast<const wchar_t*>(_rc->Notes()));
}

void BND_File3dmRenderContent::SetNotes(const std::wstring& s)
{
  _rc->SetNotes(s.c_str());
}

std::wstring BND_File3dmRenderContent::Tags(void) const
{
  return std::wstring(static_cast<const wchar_t*>(_rc->Tags()));
}

void BND_File3dmRenderContent::SetTags(const std::wstring& s)
{
  _rc->SetTags(s.c_str());
}

BND_UUID BND_File3dmRenderContent::GroupId(void) const
{
  return ON_UUID_to_Binding(_rc->GroupId());
}

void BND_File3dmRenderContent::SetGroupId(const BND_UUID& u)
{
  _rc->SetGroupId(Binding_to_ON_UUID(u));
}

bool BND_File3dmRenderContent::Hidden(void) const
{
  return _rc->Hidden();
}

void BND_File3dmRenderContent::SetHidden(bool b)
{
  _rc->SetHidden(b);
}

bool BND_File3dmRenderContent::Reference(void) const
{
  return _rc->Reference();
}

void BND_File3dmRenderContent::SetReference(bool b)
{
  _rc->SetReference(b);
}

bool BND_File3dmRenderContent::AutoDelete(void) const
{
  return _rc->AutoDelete();
}

void BND_File3dmRenderContent::SetAutoDelete(bool b)
{
  return _rc->SetAutoDelete(b);
}

BND_File3dmRenderContent* BND_File3dmRenderContent::Parent(void)
{
  delete _parent;
  _parent = new BND_File3dmRenderContent(_rc->Parent(), nullptr);
  // I don't know what this is for                      ^^^^^^^
  return _parent;
}

BND_File3dmRenderContent* BND_File3dmRenderContent::FirstChild(void)
{
  delete _first_child;
  _first_child = new BND_File3dmRenderContent(_rc->FirstChild(), nullptr);
  // I don't know what this is for                               ^^^^^^^
  return _first_child;
}

BND_File3dmRenderContent* BND_File3dmRenderContent::NextSibling(void)
{
  delete _next_sibling;
  _next_sibling = new BND_File3dmRenderContent(_rc->NextSibling(), nullptr);
  // I don't know what this is for                                 ^^^^^^^
  return _next_sibling;
}

BND_File3dmRenderContent* BND_File3dmRenderContent::TopLevel(void)
{
  delete _top_level;
  _top_level = new BND_File3dmRenderContent(&_rc->TopLevel(), nullptr);
  // I don't know what this is for                            ^^^^^^^
  return _top_level;
}

bool BND_File3dmRenderContent::IsTopLevel(void) const
{
  return _rc->IsTopLevel();
}

bool BND_File3dmRenderContent::IsChild(void) const
{
  return _rc->IsChild();
}

bool BND_File3dmRenderContent::SetChild(const ON_RenderContent& child, const std::wstring& csn)
{
  return _rc->SetChild(child, csn.c_str());
}

std::wstring BND_File3dmRenderContent::ChildSlotName(void) const
{
  return std::wstring(static_cast<const wchar_t*>(_rc->ChildSlotName()));
}

void BND_File3dmRenderContent::SetChildSlotName(const std::wstring& csn)
{
  _rc->SetChildSlotName(csn.c_str());
}

bool BND_File3dmRenderContent::ChildSlotOn(const std::wstring& csn) const
{
  return _rc->ChildSlotOn(csn.c_str());
}

bool BND_File3dmRenderContent::SetChildSlotOn(bool on, const std::wstring& csn)
{
  return _rc->SetChildSlotOn(on, csn.c_str());
}

bool BND_File3dmRenderContent::DeleteChild(const std::wstring& csn)
{
  return _rc->DeleteChild(csn.c_str());
}

const ON_RenderContent* BND_File3dmRenderContent::FindChild(const std::wstring& csn) const
{
  return _rc->FindChild(csn.c_str());
}

std::wstring BND_File3dmRenderContent::XML(bool recursive) const
{
  return std::wstring(static_cast<const wchar_t*>(_rc->XML(recursive)));
}

bool BND_File3dmRenderContent::SetXML(const std::wstring& xml)
{
  return _rc->SetXML(xml.c_str());
}

std::wstring BND_File3dmRenderContent::GetParameter(const wchar_t* name) const
{
  return static_cast<const wchar_t*>(_rc->GetParameter(name).AsString());
}

bool BND_File3dmRenderContent::SetParameter(const wchar_t* name, const std::wstring& val)
{
  return _rc->SetParameter(name, val.c_str());
}

BND_File3dmRenderMaterial::BND_File3dmRenderMaterial()
  :
  BND_File3dmRenderContent(new ON_RenderMaterial)
{
}

BND_File3dmRenderMaterial::BND_File3dmRenderMaterial(const BND_File3dmRenderMaterial& other)
  :
  BND_File3dmRenderContent(other)
{
}

BND_File3dmRenderMaterial::BND_File3dmRenderMaterial(ON_RenderMaterial* rm, const ON_ModelComponentReference* compref)
  :
  BND_File3dmRenderContent(rm, compref)
{
}

BND_Material BND_File3dmRenderMaterial::ToOnMaterial(void) const
{
  BND_Material m;

  if (nullptr != *m.m_material)
  {
    const auto* p = dynamic_cast<const ON_RenderMaterial*>(_rc);
    if (nullptr != p)
    {
      *m.m_material = p->ToOnMaterial();
    }
  }

  return m;
}

//////////////////////////////////////////////////////////////////////////////

#if defined(ON_PYTHON_COMPILE)
namespace py = pybind11;
void initRenderContentBindings(pybind11::module& m)
{
  py::class_<BND_File3dmRenderContent>(m, "RenderContent")
    .def(py::init<>())
    .def(py::init<const BND_File3dmRenderContent&>(), py::arg("other"))
    .def_property_readonly("Kind", &BND_File3dmRenderContent::Kind)
    .def_property_readonly("Parent", &BND_File3dmRenderContent::Parent)
    .def_property_readonly("IsChild", &BND_File3dmRenderContent::IsChild)
    .def_property_readonly("FirstChild", &BND_File3dmRenderContent::FirstChild)
    .def_property_readonly("NextSibling", &BND_File3dmRenderContent::NextSibling)
    .def_property_readonly("TopLevel", &BND_File3dmRenderContent::TopLevel)
    .def_property_readonly("IsTopLevel", &BND_File3dmRenderContent::IsTopLevel)
    .def_property("TypeName", &BND_File3dmRenderContent::TypeName, &BND_File3dmRenderContent::SetTypeName)
    .def_property("TypeId", &BND_File3dmRenderContent::TypeId, &BND_File3dmRenderContent::SetTypeId)
    .def_property("RenderEngineId", &BND_File3dmRenderContent::RenderEngineId, &BND_File3dmRenderContent::SetRenderEngineId)
    .def_property("PlugInId", &BND_File3dmRenderContent::PlugInId, &BND_File3dmRenderContent::SetPlugInId)
    .def_property("Notes", &BND_File3dmRenderContent::Notes, &BND_File3dmRenderContent::SetNotes)
    .def_property("Tags", &BND_File3dmRenderContent::Tags, &BND_File3dmRenderContent::SetTags)
    .def_property("GroupId", &BND_File3dmRenderContent::GroupId, &BND_File3dmRenderContent::SetGroupId)
    .def_property("Hidden", &BND_File3dmRenderContent::Hidden, &BND_File3dmRenderContent::SetHidden)
    .def_property("Reference", &BND_File3dmRenderContent::Reference, &BND_File3dmRenderContent::SetReference)
    .def_property("AutoDelete", &BND_File3dmRenderContent::AutoDelete, &BND_File3dmRenderContent::SetAutoDelete)
    .def_property("ChildSlotName", &BND_File3dmRenderContent::ChildSlotName, &BND_File3dmRenderContent::SetChildSlotName)
    .def_property("ChildSlotOn", &BND_File3dmRenderContent::ChildSlotOn, &BND_File3dmRenderContent::SetChildSlotOn)
    .def_property("XML", &BND_File3dmRenderContent::XML, &BND_File3dmRenderContent::SetXML)
    .def("SetChild", &BND_File3dmRenderContent::SetChild, py::arg("child"), py::arg("child_slot_name"))
    .def("FindChild", &BND_File3dmRenderContent::FindChild, py::arg("child_slot_name"))
    .def("DeleteChild", &BND_File3dmRenderContent::DeleteChild, py::arg("child_slot_name"))
    .def("GetParameter", &BND_File3dmRenderContent::GetParameter)
    .def("SetParameter", &BND_File3dmRenderContent::SetParameter)
   ;
}
#endif

#if defined(ON_WASM_COMPILE)
using namespace emscripten;

void initRenderContentBindings(void*)
{
  class_<BND_File3dmRenderContent>("RenderContent")
    .constructor<>()
    .property("kind", &BND_File3dmRenderContent::Kind)
    .property("parent", &BND_File3dmRenderContent::Parent)
    .property("isChild", &BND_File3dmRenderContent::IsChild)
    .property("firstChild", &BND_File3dmRenderContent::FirstChild)
    .property("nextSibling", &BND_File3dmRenderContent::NextSibling)
    .property("topLevel", &BND_File3dmRenderContent::TopLevel)
    .property("isTopLevel", &BND_File3dmRenderContent::IsTopLevel)
    .property("typeName", &BND_File3dmRenderContent::TypeName, &BND_File3dmRenderContent::SetTypeName)
    .property("typeId", &BND_File3dmRenderContent::TypeId, &BND_File3dmRenderContent::SetTypeId)
    .property("renderEngineId", &BND_File3dmRenderContent::RenderEngineId, &BND_File3dmRenderContent::SetRenderEngineId)
    .property("plugInId", &BND_File3dmRenderContent::PlugInId, &BND_File3dmRenderContent::SetPlugInId)
    .property("notes", &BND_File3dmRenderContent::Notes, &BND_File3dmRenderContent::SetNotes)
    .property("tags", &BND_File3dmRenderContent::Tags, &BND_File3dmRenderContent::SetTags)
    .property("groupId", &BND_File3dmRenderContent::GroupId, &BND_File3dmRenderContent::SetGroupId)
    .property("hidden", &BND_File3dmRenderContent::Hidden, &BND_File3dmRenderContent::SetHidden)
    .property("reference", &BND_File3dmRenderContent::Reference, &BND_File3dmRenderContent::SetReference)
    .property("autoDelete", &BND_File3dmRenderContent::AutoDelete, &BND_File3dmRenderContent::SetAutoDelete)
    .property("childSlotName", &BND_File3dmRenderContent::ChildSlotName, &BND_File3dmRenderContent::SetChildSlotName)
    .property("childSlotOn", &BND_File3dmRenderContent::ChildSlotOn, &BND_File3dmRenderContent::SetChildSlotOn)
    .property("xml", &BND_File3dmRenderContent::XML, &BND_File3dmRenderContent::SetXML)
//  .function("SetChild", &BND_File3dmRenderContent::SetChild,       // I'm not sure about this. allow_raw_pointers())
//  .function("FindChild", &BND_File3dmRenderContent::FindChild,     // I'm not sure about this. allow_raw_pointers())
//  .function("DeleteChild", &BND_File3dmRenderContent::DeleteChild, // I'm not sure about this. allow_raw_pointers())
    .function("getParameter", &BND_File3dmRenderContent::GetParameter)
    .function("setParameter", &BND_File3dmRenderContent::SetParameter)
  ;
}
#endif
