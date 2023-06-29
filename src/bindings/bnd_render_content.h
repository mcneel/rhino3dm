
#pragma once

#include "bindings.h"

#if defined(ON_PYTHON_COMPILE)
void initRenderContentBindings(pybind11::module& m);
#else
void initRenderContentBindings(void* m);
#endif

class BND_File3dmRenderContent : public BND_ModelComponent
{
public:
  ON_RenderContent* _rc = nullptr;
  BND_File3dmRenderContent* _parent = nullptr;
  BND_File3dmRenderContent* _first_child = nullptr;
  BND_File3dmRenderContent* _next_sibling = nullptr;
  BND_File3dmRenderContent* _top_level = nullptr;

protected:
  void SetTrackedPointer(ON_RenderContent* rc, const ON_ModelComponentReference* compref);

public:
  BND_File3dmRenderContent() { } // This is to keep the compiler happy but it can't work as render content is abstract.
  BND_File3dmRenderContent(ON_RenderContent* rc);
  BND_File3dmRenderContent(ON_RenderContent* rc, const ON_ModelComponentReference* compref);
  BND_File3dmRenderContent(const BND_File3dmRenderContent& other);
  virtual ~BND_File3dmRenderContent();

  std::wstring Kind(void) const;
  std::wstring TypeName(void) const;
  void SetTypeName(const std::wstring&);
  BND_UUID TypeId(void) const;
  void SetTypeId(const BND_UUID&);
  BND_UUID RenderEngineId(void) const;
  void SetRenderEngineId(const BND_UUID&);
  BND_UUID PlugInId(void) const;
  void SetPlugInId(const BND_UUID&);
  std::wstring Notes(void) const;
  void SetNotes(const std::wstring&);
  std::wstring Tags(void) const;
  void SetTags(const std::wstring&);
  BND_UUID GroupId(void) const;
  void SetGroupId(const BND_UUID&);
  bool Hidden(void) const;
  void SetHidden(bool);
  bool Reference(void) const;
  void SetReference(bool);
  bool AutoDelete(void) const;
  void SetAutoDelete(bool);
  BND_File3dmRenderContent* Parent(void);
  BND_File3dmRenderContent* FirstChild(void);
  BND_File3dmRenderContent* NextSibling(void);
  BND_File3dmRenderContent* TopLevel(void);
  bool IsTopLevel(void) const;
  bool IsChild(void) const;
  bool SetChild(const ON_RenderContent& child, const std::wstring& child_slot_name);
  std::wstring ChildSlotName(void) const;
  void SetChildSlotName(const std::wstring& child_slot_name);
  bool ChildSlotOn(const std::wstring& child_slot_name) const;
  bool SetChildSlotOn(bool on, const std::wstring& child_slot_name);
  bool DeleteChild(const std::wstring& child_slot_name);
  const ON_RenderContent* FindChild(const std::wstring& child_slot_name) const;
  std::wstring XML(bool recursive) const;
  bool SetXML(const std::wstring& xml);
  std::wstring GetParameter(const wchar_t* n) const;
  bool SetParameter(const wchar_t* n, const std::wstring& v);
};

class BND_File3dmRenderMaterial : public BND_File3dmRenderContent 
{
public:
  BND_File3dmRenderMaterial();
  BND_File3dmRenderMaterial(const BND_File3dmRenderMaterial& other);
  BND_File3dmRenderMaterial(ON_RenderMaterial* rm, const ON_ModelComponentReference* compref);

  BND_Material ToOnMaterial(void) const;
};
