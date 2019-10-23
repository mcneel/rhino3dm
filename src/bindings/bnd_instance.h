#include "bindings.h"

#pragma once

#if defined(ON_PYTHON_COMPILE)
void initInstanceBindings(pybind11::module& m);
#else
void initInstanceBindings(void* m);
#endif

class BND_InstanceDefinitionGeometry : public BND_CommonObject
{
public:
  ON_InstanceDefinition* m_idef = nullptr;
  std::vector<BND_UUID> m_guids;
protected:
  void SetTrackedPointer(ON_InstanceDefinition* idef, const ON_ModelComponentReference* compref);

public:
  BND_InstanceDefinitionGeometry(ON_InstanceDefinition* idef, const ON_ModelComponentReference* compref);
  BND_InstanceDefinitionGeometry();
  std::wstring Description() const { return std::wstring(m_idef->Description()); }
  std::wstring Name() const { return std::wstring(m_idef->Name()); }
  BND_UUID Id() const { return ON_UUID_to_Binding(m_idef->Id()); }
  //public Guid[] GetObjectIds()
  std::vector<BND_UUID>& GetObjectIds() { return m_guids; };
};

class BND_InstanceReferenceGeometry : public BND_GeometryBase
{
  ON_InstanceRef* m_iref = nullptr;
protected:
  void SetTrackedPointer(ON_InstanceRef* iref, const ON_ModelComponentReference* compref);

public:
  BND_InstanceReferenceGeometry(ON_InstanceRef* iref, const ON_ModelComponentReference* compref);
  BND_InstanceReferenceGeometry(BND_UUID instanceDefinitionId, const class BND_Transform& transform);
  BND_UUID ParentIdefId() const;
  BND_Transform Xform() const;
};