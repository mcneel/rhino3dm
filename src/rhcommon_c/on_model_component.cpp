#include "stdafx.h"

// This is so we get access to enum values in RhinoCommon.
RH_C_SHARED_ENUM_PARSE_FILE("../../../opennurbs/opennurbs_model_component.h")

RH_C_FUNCTION unsigned int ON_ModelComponent_DataCRC(const ON_ModelComponent* constModelComponentPtr, unsigned int currentRemainder)
{
  unsigned int rc = 0;
  if (constModelComponentPtr)
  {
    rc = constModelComponentPtr->DataCRC(currentRemainder);
  }
  return rc;
}

RH_C_FUNCTION bool ON_ModelComponent_IsSystemComponent(const ON_ModelComponent* constModelComponentPtr)
{
  bool rc = false;
  if (constModelComponentPtr)
  {
    rc = constModelComponentPtr->IsSystemComponent();
  }
  return rc;
}

RH_C_FUNCTION unsigned int ON_ModelComponent_GetComponentStatus(const ON_ModelComponent* constModelComponentPtr)
{
  unsigned int rc = 0;
  if (constModelComponentPtr)
  {
    ON_ComponentStatus status = constModelComponentPtr->ModelComponentStatus();
    rc = ON_ComponentStatus_RevealField(status);
  }
  return rc;
}

RH_C_FUNCTION bool ON_ModelComponent_SetComponentStatus(ON_ModelComponent* modelComponentPtr, const unsigned int componentStatus)
{
  bool rc = false;
  if (modelComponentPtr)
  {
    ON_ComponentStatus status = ON_ComponentStatus_FromField(componentStatus);
    rc = modelComponentPtr->SetModelComponentStatus(status);
  }
  return rc;
}

RH_C_FUNCTION bool ON_ModelComponent_GetComponentStatusIsLocked(const ON_ModelComponent* constModelComponentPtr)
{
  bool rc = false;
  if (constModelComponentPtr)
  {
    rc = constModelComponentPtr->ModelComponentStatusIsLocked();
  }
  return rc;
}


// - ID

RH_C_FUNCTION ON_UUID ON_ModelComponent_GetId(const ON_ModelComponent* constModelComponentPtr)
{
  ON_UUID rc = ::ON_nil_uuid;
  if (constModelComponentPtr)
  {
    rc = constModelComponentPtr->Id();
  }
  return rc;
}

RH_C_FUNCTION bool ON_ModelComponent_SetId(ON_ModelComponent* modelComponentPtr, const ON_UUID id)
{
  bool rc = false;
  if (modelComponentPtr)
  {
    rc = modelComponentPtr->SetId(id);
  }
  return rc;
}

RH_C_FUNCTION bool ON_ModelComponent_IdIsLocked(const ON_ModelComponent* constModelComponentPtr)
{
  bool rc = false;
  if (constModelComponentPtr)
  {
    rc = constModelComponentPtr->IdIsLocked();
  }
  return rc;
}

RH_C_FUNCTION bool ON_ModelComponent_HasId(const ON_ModelComponent* constModelComponentPtr)
{
  bool rc = false;
  if (constModelComponentPtr)
  {
    rc = constModelComponentPtr->IdIsSet();
  }
  return rc;
}

RH_C_FUNCTION bool ON_ModelComponent_ClearId(ON_ModelComponent* modelComponentPtr)
{
  bool rc = false;
  if (modelComponentPtr)
  {
    rc = modelComponentPtr->ClearId();
  }
  return rc;
}

RH_C_FUNCTION void ON_ModelComponent_LockId(ON_ModelComponent* modelComponentPtr)
{
  if (modelComponentPtr)
    modelComponentPtr->LockId();
}

// - Index

RH_C_FUNCTION int ON_ModelComponent_GetIndex(const ON_ModelComponent* constModelComponentPtr)
{
  int rc = ON_UNSET_INT_INDEX;
  if (constModelComponentPtr)
  {
    rc = constModelComponentPtr->Index();
  }
  return rc;
}

RH_C_FUNCTION bool ON_ModelComponent_SetIndex(ON_ModelComponent* modelComponentPtr, int index)
{
  bool rc = false;
  if (modelComponentPtr)
  {
    rc = modelComponentPtr->SetIndex(index);
  }
  return rc;
}

RH_C_FUNCTION bool ON_ModelComponent_ClearIndex(ON_ModelComponent* modelComponentPtr)
{
  bool rc = false;
  if (modelComponentPtr)
  {
    rc = modelComponentPtr->ClearIndex();
  }
  return rc;
}

RH_C_FUNCTION bool ON_ModelComponent_HasIndex(const ON_ModelComponent* constModelComponentPtr)
{
  bool rc = false;
  if (constModelComponentPtr)
  {
    rc = constModelComponentPtr->IndexIsSet();
  }
  return rc;
}

RH_C_FUNCTION bool ON_ModelComponent_IndexIsLocked(const ON_ModelComponent* constModelComponentPtr)
{
  bool rc = false;
  if (constModelComponentPtr)
  {
    rc = constModelComponentPtr->IndexIsLocked();
  }
  return rc;
}

RH_C_FUNCTION void ON_ModelComponent_LockIndex(ON_ModelComponent* modelComponentPtr)
{
  if (modelComponentPtr)
  {
    modelComponentPtr->LockIndex();
  }
}

// - NAME

RH_C_FUNCTION ON_UUID ON_ModelComponent_GetName(const ON_ModelComponent* constModelComponentPtr, CRhCmnStringHolder* pStringHolder)
{
  ON_UUID rc = ::ON_nil_uuid;
  if (constModelComponentPtr && pStringHolder)
  {
    const ON_wString name = constModelComponentPtr->Name();
    pStringHolder->Set(name);
  }
  return rc;
}

RH_C_FUNCTION bool ON_ModelComponent_SetName(ON_ModelComponent* modelComponentPtr, const RHMONO_STRING* str)
{
  bool rc = false;
  if (modelComponentPtr && str)
  {
    INPUTSTRINGCOERCE(_str, str);
    rc = modelComponentPtr->SetName(_str);
  }
  return rc;
}

RH_C_FUNCTION bool ON_ModelComponent_IsValidComponentName(const RHMONO_STRING* str)
{
  bool rc = false;
  if (str)
  {
    INPUTSTRINGCOERCE(_str, str);
    rc = ON_ModelComponent::IsValidComponentName(_str);
  }
  return rc;
}

RH_C_FUNCTION bool ON_ModelComponent_ClearName(ON_ModelComponent* modelComponentPtr)
{
  bool rc = false;
  if (modelComponentPtr)
  {
    rc = modelComponentPtr->ClearName();
  }
  return rc;
}

RH_C_FUNCTION bool ON_ModelComponent_HasName(const ON_ModelComponent* constModelComponentPtr)
{
  bool rc = false;
  if (constModelComponentPtr)
  {
    rc = constModelComponentPtr->NameIsSet();
  }
  return rc;
}

RH_C_FUNCTION bool ON_ModelComponent_NameIsLocked(const ON_ModelComponent* constModelComponentPtr)
{
  bool rc = false;
  if (constModelComponentPtr)
  {
    rc = constModelComponentPtr->NameIsLocked();
  }
  return rc;
}

RH_C_FUNCTION void ON_ModelComponent_LockName(ON_ModelComponent* modelComponentPtr)
{
  if (modelComponentPtr)
    modelComponentPtr->LockName();
}

RH_C_FUNCTION bool ON_ModelComponent_RequiresUniqueName(ON_ModelComponent::Type modelComponentType)
{
  return ON_ModelComponent::UniqueNameRequired(modelComponentType);
}

RH_C_FUNCTION bool ON_ModelComponent_UniqueNameIgnoresCase(ON_ModelComponent::Type modelComponentType)
{
  return ON_ModelComponent::UniqueNameIgnoresCase(modelComponentType);
}

RH_C_FUNCTION bool ON_ModelComponent_UniqueNameIncludesParent(ON_ModelComponent::Type modelComponentType)
{
  return ON_ModelComponent::UniqueNameIncludesParent(modelComponentType);
}

RH_C_FUNCTION bool ON_ModelComponent_GetDeletedName(const ON_ModelComponent* constModelComponentPtr, CRhCmnStringHolder* pStringHolder)
{
  bool rc = false;
  if (nullptr != constModelComponentPtr && nullptr != pStringHolder)
  {
    const ON_wString str = constModelComponentPtr->DeletedName();
    pStringHolder->Set(str);
    rc = true;;
  }
  return rc;
}

RH_C_FUNCTION unsigned int ON_ModelComponent_ModelSerialNumber(const ON_ModelComponent* constModelComponentPtr)
{
  unsigned int rc = 0;
  if (nullptr != constModelComponentPtr)
    rc = constModelComponentPtr->ModelSerialNumber();
  return rc;
}

RH_C_FUNCTION unsigned int ON_ModelComponent_ReferenceModelSerialNumber(const ON_ModelComponent* constModelComponentPtr)
{
  unsigned int rc = 0;
  if (nullptr != constModelComponentPtr)
    rc = constModelComponentPtr->ReferenceModelSerialNumber();
  return rc;
}

RH_C_FUNCTION unsigned int ON_ModelComponent_InstanceDefinitionModelSerialNumber(const ON_ModelComponent* constModelComponentPtr)
{
  unsigned int rc = 0;
  if (nullptr != constModelComponentPtr)
    rc = constModelComponentPtr->InstanceDefinitionModelSerialNumber();
  return rc;
}

