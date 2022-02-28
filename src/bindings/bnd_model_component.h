#include "bindings.h"

#pragma once

class BND_ModelComponent : public BND_CommonObject
{
  ON_ModelComponent* m_model_component = nullptr;
protected:
  BND_ModelComponent();
  void SetTrackedPointer(ON_ModelComponent* modelComponent, const ON_ModelComponentReference* compref);

public:
  unsigned int DataCRC(unsigned int currentRemainer) const { return m_model_component->DataCRC(currentRemainer); }
  bool IsSystemComponent() const { return m_model_component->IsSystemComponent(); }
  BND_UUID GetId() const;
  void SetId(BND_UUID id);
  void ClearId() { m_model_component->ClearId(); }
//    public bool HasId{get;}
//    public bool IdIsLocked{get}
//    public void LockId()
//    public virtual int Index{get;set;}
//    public void ClearIndex()
//    public bool HasIndex{get}
//    public bool IndexIsLocked{get}
//    public void LockIndex()
//    public virtual ComponentStatus ComponentStatus{get;set}
//    public bool IsComponentStatusLocked{get}
//    public static bool IsValidComponentName(string name)
//     public virtual string Name{get;set}
//    public void ClearName()
//    public bool HasName{get}
//     public bool NameIsLocked{get}
//    public void LockName()
//    public string DeletedName{get}
//    public virtual uint ModelSerialNumber{get}
//    public virtual uint ReferenceModelSerialNumber{get}
//    public virtual uint InstanceDefinitionModelSerialNumber{get}
//    public abstract ModelComponentType ComponentType { get; }
//    public static bool ModelComponentTypeRequiresUniqueName(ModelComponentType type)
//    public static bool ModelComponentTypeIgnoresCase(ModelComponentType type)
//    public static bool ModelComponentTypeIncludesParent(ModelComponentType type)
};


#if defined(ON_PYTHON_COMPILE)
void initModelComponentBindings(pybind11::module& m);
#else
void initModelComponentBindings(void* m);
#endif

