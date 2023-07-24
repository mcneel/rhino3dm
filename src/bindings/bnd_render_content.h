
#include "bindings.h"

#pragma once

#if defined(ON_PYTHON_COMPILE)
void initRenderContentBindings(pybind11::module& m);
#else
void initRenderContentBindings(void* m);
#endif

class BND_File3dmRenderContent : public BND_ModelComponent
{
public:
  ON_RenderContent* m_rc = nullptr;

protected:
  void SetTrackedPointer(ON_RenderContent* rc, const ON_ModelComponentReference* compref);

public:
  BND_File3dmRenderContent();
  BND_File3dmRenderContent(ON_RenderContent* rc);
  BND_File3dmRenderContent(ON_RenderContent* rc, const ON_ModelComponentReference* compref);
  BND_File3dmRenderContent(const BND_File3dmRenderContent& other);
  virtual ~BND_File3dmRenderContent();

  std::wstring Kind() const;
  std::wstring TypeName() const;
  void SetTypeName(const std::wstring&);
  BND_UUID Id() const;
  std::wstring Name() const;
  void SetName(const std::wstring&);
  BND_UUID TypeId() const;
  void SetTypeId(const BND_UUID&);
  BND_UUID RenderEngineId() const;
  void SetRenderEngineId(const BND_UUID&);
  BND_UUID PlugInId() const;
  void SetPlugInId(const BND_UUID&);
  std::wstring Notes() const;
  void SetNotes(const std::wstring&);
  std::wstring Tags() const;
  void SetTags(const std::wstring&);
  BND_UUID GroupId() const;
  void SetGroupId(const BND_UUID&);
  bool Hidden() const;
  void SetHidden(bool);
  bool Reference() const;
  void SetReference(bool);
  bool AutoDelete() const;
  void SetAutoDelete(bool);
  BND_File3dmRenderContent* Parent() const;
  BND_File3dmRenderContent* FirstChild() const;
  BND_File3dmRenderContent* NextSibling() const;
  BND_File3dmRenderContent* TopLevel() const;
  BND_File3dmRenderContent* FindChild(const std::wstring& child_slot_name) const;
  bool IsTopLevel() const;
  bool IsChild() const;
  bool SetChild(const ON_RenderContent& child, const std::wstring& child_slot_name);
  std::wstring ChildSlotName() const;
  void SetChildSlotName(const std::wstring& child_slot_name);
  bool ChildSlotOn(const std::wstring& child_slot_name) const;
  bool SetChildSlotOn(bool on, const std::wstring& child_slot_name);
  double ChildSlotAmount(const wchar_t* child_slot_name) const;
  bool SetChildSlotAmount(double amount, const wchar_t* child_slot_name);
  bool DeleteChild(const std::wstring& child_slot_name);
  std::wstring XML(bool recursive) const;
  bool SetXML(const std::wstring& xml);
  std::wstring GetParameter(const std::wstring& n) const;
  bool SetParameter(const std::wstring& n, const std::wstring& v);
};

class BND_File3dmRenderMaterial : public BND_File3dmRenderContent 
{
public:
  BND_File3dmRenderMaterial();
  BND_File3dmRenderMaterial(const BND_File3dmRenderMaterial& other);
  BND_File3dmRenderMaterial(ON_RenderContent* rm, const ON_ModelComponentReference* compref);

  BND_Material* ToMaterial() const;
};

class BND_File3dmRenderEnvironment : public BND_File3dmRenderContent 
{
public:
  BND_File3dmRenderEnvironment();
  BND_File3dmRenderEnvironment(const BND_File3dmRenderEnvironment& other);
  BND_File3dmRenderEnvironment(ON_RenderContent* re, const ON_ModelComponentReference* compref);

  BND_Environment* ToEnvironment() const;
};

class BND_File3dmRenderTexture : public BND_File3dmRenderContent 
{
public:
  BND_File3dmRenderTexture();
  BND_File3dmRenderTexture(const BND_File3dmRenderTexture& other);
  BND_File3dmRenderTexture(ON_RenderContent* rt, const ON_ModelComponentReference* compref);

  BND_Texture* ToTexture() const;

  std::wstring Filename() const;
  void SetFilename(const std::wstring&);
};

class BND_File3dmRenderContentTable
{
private:
  std::shared_ptr<ONX_Model> m_model;

public:
  BND_File3dmRenderContentTable(std::shared_ptr<ONX_Model> m) { m_model = m; }

  int Count() const { return m_model.get()->ActiveComponentCount(ON_ModelComponent::Type::RenderContent); }
  void Add(const BND_File3dmRenderContent& rc);
  BND_File3dmRenderContent* FindIndex(int index);
  BND_File3dmRenderContent* IterIndex(int index); // helper function for iterator
  BND_File3dmRenderContent* FindId(BND_UUID id);
};
