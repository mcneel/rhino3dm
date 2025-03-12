#include "bindings.h"

#pragma once

#if defined(ON_PYTHON_COMPILE)
void initGroupBindings(rh3dmpymodule& m);
#else
void initGroupBindings(void* m);
#endif

class BND_Group : public BND_CommonObject
{
public:
  ON_Group* m_group = nullptr;
public:
  BND_Group();
  BND_Group(ON_Group* group, const ON_ModelComponentReference* compref);

  std::wstring GetName() const { return std::wstring(m_group->NameAsPointer()); }
  void SetName(const std::wstring& name) { m_group->SetName(name.c_str()); }
  BND_UUID GetId() const { return ON_UUID_to_Binding(m_group->Id()); }
  void SetId(BND_UUID id) { m_group->SetId(Binding_to_ON_UUID(id)); }
  int GetIndex() const { return m_group->Index(); }
  //public File3dmObject[] GroupMembers(int groupIndex)

protected:
  void SetTrackedPointer(ON_Group* group, const ON_ModelComponentReference* compref);
};
