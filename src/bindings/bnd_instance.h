#include "bindings.h"

#pragma once

enum class InstanceDefinitionUpdateType : int
{
  Static = 0,
  Embedded = 1,
  LinkedAndEmbedded = 2,
  Linked = 3
};

#if defined(ON_PYTHON_COMPILE)
void initInstanceBindings(pybind11::module& m);
#else
void initInstanceBindings(void* m);
#endif

class BND_InstanceDefinitionGeometry : public BND_CommonObject
{
public:
  ON_InstanceDefinition* m_idef = nullptr;
protected:
  void SetTrackedPointer(ON_InstanceDefinition* idef, const ON_ModelComponentReference* compref);

public:
  BND_InstanceDefinitionGeometry(ON_InstanceDefinition* idef, const ON_ModelComponentReference* compref);
  BND_InstanceDefinitionGeometry();
  std::wstring Description() const { return std::wstring(m_idef->Description()); }
  std::wstring Name() const { return std::wstring(m_idef->Name()); }
  BND_UUID Id() const { return ON_UUID_to_Binding(m_idef->Id()); }
  std::wstring SourceArchive() const { return std::wstring(m_idef->LinkedFilePath()); }
  InstanceDefinitionUpdateType UpdateType() const;
  BND_TUPLE GetObjectIds() const;
  bool IsInstanceGeometryId(BND_UUID id) const { return m_idef->IsInstanceGeometryId(Binding_to_ON_UUID(id));}
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