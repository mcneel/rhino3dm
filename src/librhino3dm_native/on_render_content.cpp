
#include "stdafx.h"

static const ON_RenderContent* FindRenderContentFromIdRecursive(const ON_RenderContent* rc, const ON_UUID& id)
{
  if (rc->Id() == id)
    return rc;

  auto it = rc->GetChildIterator();
  while (auto* child = it.GetNextChild())
  {
    const auto* found = FindRenderContentFromIdRecursive(child, id);
    if (nullptr != found)
      return found;
  }

  return nullptr;
}

static const ON_RenderContent* FindRenderContentFromId(ONX_Model& model, const ON_UUID& id)
{
  ONX_ModelComponentIterator it(model, ON_ModelComponent::Type::RenderContent);
  auto* component = it.FirstComponent();
  while (nullptr != component)
  {
    const auto* rc = dynamic_cast<const ON_RenderContent*>(component);
    if (nullptr != rc)
    {
      auto* found = FindRenderContentFromIdRecursive(rc, id);
      if (nullptr != found)
        return found;
    }

    component = it.NextComponent();
  }

  return nullptr;
}

RH_C_FUNCTION const ON_RenderContent* ONX_Model_FindRenderContentFromId(ONX_Model* model, ON_UUID id)
{
  if (nullptr == model)
    return nullptr;

  return FindRenderContentFromId(*model, id);
}

RH_C_FUNCTION int ONX_Model_GetFile3dmRenderContentKind(ONX_Model* model, ON_UUID id)
{
  if (nullptr != model)
  {
    const auto* rc = FindRenderContentFromId(*model, id);
    if (nullptr != rc)
    {
      if (nullptr != dynamic_cast<const ON_RenderMaterial*>(rc))
        return 0;

      if (nullptr != dynamic_cast<const ON_RenderEnvironment*>(rc))
        return 1;

      if (nullptr != dynamic_cast<const ON_RenderTexture*>(rc))
        return 2;
    }
  }

  return -1;
}

RH_C_FUNCTION ON_RenderMaterial* ON_RenderMaterial3dm_New()
{
  return new ON_RenderMaterial;
}

RH_C_FUNCTION ON_RenderEnvironment* ON_RenderEnvironment3dm_New()
{
  return new ON_RenderEnvironment;
}

RH_C_FUNCTION ON_RenderTexture* ON_RenderTexture3dm_New()
{
  return new ON_RenderTexture;
}

RH_C_FUNCTION void ON_RenderContent_TypeName(const ON_RenderContent* rc, ON_wString* string)
{
  if ((nullptr != rc) && (nullptr != string))
  {
    *string = rc->TypeName();
  }
}

//RH_C_FUNCTION void ON_RenderContent_SetTypeName(ON_RenderContent* rc, const RHMONO_STRING* string)
//{
//  if ((nullptr != rc) && (nullptr != string))
//  {
//    INPUTSTRINGCOERCE(_string, string);
//    rc->SetTypeName(_string);
//  }
//}

RH_C_FUNCTION void ON_RenderContent_Kind(const ON_RenderContent* rc, ON_wString* kind)
{
  if ((nullptr != rc) && (nullptr != kind))
  {
    *kind = rc->Kind();
  }
}

RH_C_FUNCTION ON_UUID ON_RenderContent_TypeId(const ON_RenderContent* rc)
{
  return (nullptr != rc) ? rc->TypeId() : ON_nil_uuid;
}

RH_C_FUNCTION ON_UUID ON_RenderContent_RenderEngineId(const ON_RenderContent* rc)
{
  return (nullptr != rc) ? rc->RenderEngineId() : ON_nil_uuid;
}

RH_C_FUNCTION ON_UUID ON_RenderContent_PlugInId(const ON_RenderContent* rc)
{
  return (nullptr != rc) ? rc->PlugInId() : ON_nil_uuid;
}

RH_C_FUNCTION ON_UUID ON_RenderContent_GroupId(ON_RenderContent* rc)
{
  return (nullptr != rc) ? rc->GroupId() : ON_nil_uuid;
}

RH_C_FUNCTION void ON_RenderContent_Notes(const ON_RenderContent* rc, ON_wString* string)
{
  if ((nullptr != rc) && (nullptr != string))
  {
    *string = rc->Notes();
  }
}

RH_C_FUNCTION void ON_RenderContent_Tags(const ON_RenderContent* rc, ON_wString* string)
{
  if ((nullptr != rc) && (nullptr != string))
  {
    *string = rc->Tags();
  }
}

RH_C_FUNCTION bool ON_RenderContent_Hidden(const ON_RenderContent* rc)
{
  return (nullptr != rc) ? rc->Hidden() : false;
}

RH_C_FUNCTION bool ON_RenderContent_Reference(const ON_RenderContent* rc)
{
  return (nullptr != rc) ? rc->Reference() : false;
}

RH_C_FUNCTION bool ON_RenderContent_AutoDelete(const ON_RenderContent* rc)
{
  return (nullptr != rc) ? rc->AutoDelete() : false;
}

RH_C_FUNCTION bool ON_RenderContent_Children(const ON_RenderContent* rc, ON_SimpleArray<ON_UUID>* children)
{
  if ((nullptr == rc) || (nullptr == children))
    return false;

  const auto* child = rc->FirstChild();
  while (nullptr != child)
  {
    children->Append(child->Id());

    child = child->NextSibling();
  }

  return true;
}

RH_C_FUNCTION ON_UUID ON_RenderContent_TopLevelId(const ON_RenderContent* rc)
{
  if (nullptr == rc)
    return ON_nil_uuid;

  return rc->TopLevel().Id();
}

RH_C_FUNCTION bool ON_RenderContent_IsTopLevel(const ON_RenderContent* rc)
{
  return (nullptr != rc) ? rc->IsTopLevel() : false;
}

RH_C_FUNCTION bool ON_RenderContent_IsChild(const ON_RenderContent* rc)
{
  return (nullptr != rc) ? rc->IsChild() : false;
}

RH_C_FUNCTION void ON_RenderContent_ChildSlotName(const ON_RenderContent* rc, ON_wString* csn)
{
  if ((nullptr != rc) && (nullptr != csn))
  {
    *csn = rc->ChildSlotName();
  }
}

RH_C_FUNCTION bool ON_RenderContent_ChildSlotOn(const ON_RenderContent* rc, const RHMONO_STRING* csn)
{
  if ((nullptr != rc) && (nullptr != csn))
  {
    INPUTSTRINGCOERCE(_csn, csn);
    return rc->ChildSlotOn(_csn);
  }

  return false;
}

RH_C_FUNCTION bool ON_RenderContent_DeleteChild(ON_RenderContent* rc, const RHMONO_STRING* csn)
{
  if ((nullptr != rc) && (nullptr != csn))
  {
    INPUTSTRINGCOERCE(_csn, csn);
    return rc->DeleteChild(_csn);
  }

  return false;
}

RH_C_FUNCTION ON_UUID ON_RenderContent_FindChild(const ON_RenderContent* rc, const RHMONO_STRING* csn)
{
  if ((nullptr != rc) && (nullptr != csn))
  {
    INPUTSTRINGCOERCE(_csn, csn);
    const auto* child = rc->FindChild(_csn);
    if (nullptr != child)
      return child->Id();
  }

  return ON_nil_uuid;
}

RH_C_FUNCTION void ON_RenderContent_XML(const ON_RenderContent* rc, bool recursive, ON_wString* string)
{
  if ((nullptr != rc) && (nullptr != string))
  {
    *string = rc->XML(recursive);
  }
}

RH_C_FUNCTION bool ON_RenderMaterial_SimulatedMaterial(const ON_RenderMaterial* rm, ON_Material* mat)
{
  if ((nullptr != rm) && (nullptr != mat))
  {
    *mat = rm->SimulatedMaterial();

    return true;
  }

  return false;
}

RH_C_FUNCTION bool ON_RenderEnvironment_SimulatedEnvironment(const ON_RenderEnvironment* re, ON_Environment* env)
{
  if ((nullptr != re) && (nullptr != env))
  {
    *env = re->SimulatedEnvironment();

    return true;
  }

  return false;
}

RH_C_FUNCTION bool ON_RenderTexture_SimulatedTexture(const ON_RenderTexture* rt, ON_Texture* tex)
{
  if ((nullptr != rt) && (nullptr != tex))
  {
    *tex = rt->SimulatedTexture();

    return true;
  }

  return false;
}

RH_C_FUNCTION ON_Environment* ON_Environment_New()
{
  return new ON_Environment;
}

RH_C_FUNCTION int ON_Environment_BackgroundColor(const ON_Environment* env)
{
  if (nullptr == env)
    return 0;

  return env->BackgroundColor();
}

RH_C_FUNCTION bool ON_Environment_BackgroundImage(const ON_Environment* env, ON_Texture* tex)
{
  if ((nullptr == env) || (nullptr == tex))
    return false;

  *tex = env->BackgroundImage();

  return true;
}

RH_C_FUNCTION int ON_Environment_BackgroundProjection(const ON_Environment* env)
{
  if (nullptr == env)
    return -1;

  return int(env->BackgroundProjection());
}

RH_C_FUNCTION ON_UUID ONX_Model_GetBackgroundRenderEnvironment(const ONX_Model* ptrModel)
{
  if (nullptr == ptrModel)
    return ON_nil_uuid;

  return ON_nil_uuid;// ptrModel->m_settings.m_RenderSettings.BackgroundRenderEnvironment();
}

RH_C_FUNCTION void ONX_Model_SetBackgroundRenderEnvironment(ONX_Model* ptrModel, ON_UUID uuid)
{
  if (nullptr != ptrModel)
  {
    //ptrModel->m_settings.m_RenderSettings.SetBackgroundRenderEnvironment(uuid);
  }
}

RH_C_FUNCTION bool ONX_Model_GetSkylightingRenderEnvironmentOverride(const ONX_Model* ptrModel)
{
  if (nullptr == ptrModel)
    return false;

  return false;// ptrModel->m_settings.m_RenderSettings.SkylightingRenderEnvironmentOverride();
}

RH_C_FUNCTION void ONX_Model_SetSkylightingRenderEnvironmentOverride(ONX_Model* ptrModel, bool b)
{
  if (nullptr != ptrModel)
  {
    //ptrModel->m_settings.m_RenderSettings.SetSkylightingRenderEnvironmentOverride(b);
  }
}

RH_C_FUNCTION ON_UUID ONX_Model_GetSkylightingRenderEnvironment(const ONX_Model* ptrModel)
{
  if (nullptr == ptrModel)
    return ON_nil_uuid;

  return ON_nil_uuid;// ptrModel->m_settings.m_RenderSettings.SkylightingRenderEnvironment();
}

RH_C_FUNCTION void ONX_Model_SetSkylightingRenderEnvironment(ONX_Model* ptrModel, ON_UUID uuid)
{
  if (nullptr != ptrModel)
  {
    //ptrModel->m_settings.m_RenderSettings.SetSkylightingRenderEnvironment(uuid);
  }
}

RH_C_FUNCTION bool ONX_Model_GetReflectionRenderEnvironmentOverride(const ONX_Model* ptrModel)
{
  if (nullptr == ptrModel)
    return false;

  return false;// ptrModel->m_settings.m_RenderSettings.ReflectionRenderEnvironmentOverride();
}

RH_C_FUNCTION void ONX_Model_SetReflectionRenderEnvironmentOverride(ONX_Model* ptrModel, bool b)
{
  if (nullptr != ptrModel)
  {
    //ptrModel->m_settings.m_RenderSettings.SetReflectionRenderEnvironmentOverride(b);
  }
}

RH_C_FUNCTION ON_UUID ONX_Model_GetReflectionRenderEnvironment(const ONX_Model* ptrModel)
{
  if (nullptr == ptrModel)
    return ON_nil_uuid;

  return ON_nil_uuid;// ptrModel->m_settings.m_RenderSettings.ReflectionRenderEnvironment();
}

RH_C_FUNCTION void ONX_Model_SetReflectionRenderEnvironment(ONX_Model* ptrModel, ON_UUID uuid)
{
  if (nullptr != ptrModel)
  {
    //ptrModel->m_settings.m_RenderSettings.SetReflectionRenderEnvironment(uuid);
  }
}
