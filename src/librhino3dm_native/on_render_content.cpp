
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

RH_C_FUNCTION bool ON_RenderContent_GetParameter(const ON_RenderContent* rc, const RHMONO_STRING* param, ON_XMLVariant* variant)
{
  if ((nullptr == rc) || (nullptr == param) || (nullptr == variant))
    return false;

  INPUTSTRINGCOERCE(_param, param);

  const auto v = rc->GetParameter(_param);
  if (v.IsNull())
    return false;

  *variant = v;

  return true;
}

RH_C_FUNCTION bool ON_RenderContent_SetParameter(ON_RenderContent* rc, const RHMONO_STRING* param, const ON_XMLVariant* variant)
{
  if ((nullptr == rc) || (nullptr == param) || (nullptr == variant))
    return false;

  INPUTSTRINGCOERCE(_param, param);

  if (!rc->SetParameter(_param, *variant))
    return false;

  return true;
}

RH_C_FUNCTION bool ON_RenderMaterial_To_ON_Material(const ON_RenderMaterial* rm, ON_Material* mat)
{
  if ((nullptr != rm) && (nullptr != mat))
  {
    *mat = rm->ToOnMaterial();
    return true;
  }

  return false;
}

RH_C_FUNCTION bool ON_RenderEnvironment_To_ON_Environment(const ON_RenderEnvironment* re, ON_Environment* env)
{
  if ((nullptr != re) && (nullptr != env))
  {
    *env = re->ToOnEnvironment();
    return true;
  }

  return false;
}

RH_C_FUNCTION bool ON_RenderTexture_To_ON_Texture(const ON_RenderTexture* rt, ON_Texture* tex)
{
  if ((nullptr != rt) && (nullptr != tex))
  {
    *tex = rt->ToOnTexture();
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

RH_C_FUNCTION ON_XMLVariant* ON_XMLVariant_New()
{
  return new ON_XMLVariant;
}

RH_C_FUNCTION void ON_XMLVariant_Copy(const ON_XMLVariant* pVS, ON_XMLVariant* pVD)
{
  if (pVS && pVD)
  {
    *pVD = *pVS;
  }
}

RH_C_FUNCTION int ON_XMLVariant_GetUnits(const ON_XMLVariant* pV)
{
  if (pV)
    return int(pV->Units());

  return 0;
}

RH_C_FUNCTION void ON_XMLVariant_SetUnits(ON_XMLVariant* pV, int value)
{
  if (pV) pV->SetUnits((ON::LengthUnitSystem)value);
}

RH_C_FUNCTION int ON_XMLVariant_IsNull(const ON_XMLVariant* pV)
{
  if (pV)
    return pV->IsNull() ? 1 : 0;

  return 1;
}

RH_C_FUNCTION void ON_XMLVariant_SetNull(ON_XMLVariant* pV)
{
  if (pV) pV->SetNull();
}

RH_C_FUNCTION int ON_XMLVariant_Varies(const ON_XMLVariant* pV)
{
  if (pV)
    return pV->Varies() ? 1 : 0;

  return 0;
}

RH_C_FUNCTION void ON_XMLVariant_SetVaries(ON_XMLVariant* pV)
{
  if (pV) pV->SetVaries();
}

RH_C_FUNCTION int ON_XMLVariant_Type(const ON_XMLVariant* pV)
{
  if (pV)
    return int(pV->Type());

  return 0;
}

RH_C_FUNCTION void ON_XMLVariant_SetIntValue(ON_XMLVariant* pV, int v)
{
  if (pV) pV->SetValue(v);
}

RH_C_FUNCTION void ON_XMLVariant_SetBoolValue(ON_XMLVariant* pV, bool v)
{
  if (pV) pV->SetValue(v);
}

RH_C_FUNCTION void ON_XMLVariant_SetDoubleValue(ON_XMLVariant* pV, double v)
{
  if (pV) pV->SetValue(v);
}

RH_C_FUNCTION void ON_XMLVariant_SetFloatValue(ON_XMLVariant* pV, float v)
{
  if (pV) pV->SetValue(v);
}

RH_C_FUNCTION void ON_XMLVariant_SetStringValue(ON_XMLVariant* pV, RHMONO_STRING* _ps)
{
  if (pV && _ps)
  {
    INPUTSTRINGCOERCE(ps, _ps);
    pV->SetValue(ps);
  }
}

RH_C_FUNCTION void ON_XMLVariant_SetOnColorValue(ON_XMLVariant* pV, int v)
{
  if (pV) pV->SetValue(ON_4fColor(ARGB_to_ABGR(v)));
}

RH_C_FUNCTION void ON_XMLVariant_SetColor4fValue(ON_XMLVariant* pV, ON_4FVECTOR_STRUCT v)
{
  if (pV)
  {
    const ON_4fColor col(v.val[0], v.val[1], v.val[2], v.val[3]);
    pV->SetValue(col);
  }
}

RH_C_FUNCTION void ON_XMLVariant_Set2dVectorValue(ON_XMLVariant* pV, ON_2DVECTOR_STRUCT v)
{
  if (pV) pV->SetValue(ON_2dPoint(v.val));
}

RH_C_FUNCTION void ON_XMLVariant_Set3dVectorValue(ON_XMLVariant* pV, ON_3DVECTOR_STRUCT v)
{
  if (pV) pV->SetValue(ON_3dPoint(v.val));
}

RH_C_FUNCTION void ON_XMLVariant_Set4dPointValue(ON_XMLVariant* pV, ON_4DPOINT_STRUCT v)
{
  if (pV) pV->SetValue(ON_4dPoint(v.val));
}

RH_C_FUNCTION void ON_XMLVariant_SetUuidValue(ON_XMLVariant* pV, ON_UUID v)
{
  if (pV) pV->SetValue(v);
}

RH_C_FUNCTION void ON_XMLVariant_SetXformValue(ON_XMLVariant* pV, ON_XFORM_STRUCT v)
{
  if (pV) pV->SetValue(ON_Xform(v.val));
}

RH_C_FUNCTION void ON_XMLVariant_SetByteArrayValue(ON_XMLVariant* pV, /*ARRAY*/const char* buffer, int sizeOfBuffer)
{
  if (pV) pV->SetValue(buffer, sizeOfBuffer);
}

RH_C_FUNCTION void ON_XMLVariant_SetTimeValue(ON_XMLVariant* pV, time_t v)
{
  if (pV) pV->SetValue(v);
}

RH_C_FUNCTION int ON_XMLVariant_GetIntValue(const ON_XMLVariant* pV)
{
  if (pV)
    return pV->AsInteger();
  return 0;
}

RH_C_FUNCTION int ON_XMLVariant_GetBoolValue(const ON_XMLVariant* pV)
{
  if (pV)
    return pV->AsBool() ? 1 : 0;
  return 0;
}

RH_C_FUNCTION double ON_XMLVariant_GetDoubleValue(const ON_XMLVariant* pV)
{
  if (pV)
    return pV->AsDouble();
  return 0.0;
}

RH_C_FUNCTION float ON_XMLVariant_GetFloatValue(const ON_XMLVariant* pV)
{
  if (pV)
    return pV->AsFloat();
  return 0.0f;
}

RH_C_FUNCTION int ON_XMLVariant_GetStringValue(const ON_XMLVariant* pV, CRhCmnStringHolder* pSH)
{
  if (pV && pSH)
  {
    pSH->Set(pV->AsString());
    return 1;
  }
  return 0;
}

RH_C_FUNCTION int ON_XMLVariant_GetOnColorValue(const ON_XMLVariant* pV)
{
  if (pV)
  {
    const auto col = (ON_Color)pV->AsColor();
    return ABGR_to_ARGB(col);
  }
  return 0;
}

RH_C_FUNCTION int ON_XMLVariant_GetColor4fValue(const ON_XMLVariant* pV, ON_4fPoint* p)
{
  if (pV && p)
  {
    const auto c = pV->AsColor();
    p->x = c.Red();
    p->y = c.Green();
    p->z = c.Blue();
    p->w = c.Alpha();
    return 1;
  }
  return 0;
}

RH_C_FUNCTION int ON_XMLVariant_Get2dVectorValue(const ON_XMLVariant* pV, ON_2dVector* v)
{
  if (pV && v)
  {
    *v = pV->As2dPoint();
    return 1;
  }
  return 0;
}

RH_C_FUNCTION int ON_XMLVariant_Get3dVectorValue(const ON_XMLVariant* pV, ON_3dVector* v)
{
  if (pV && v)
  {
    *v = pV->As3dPoint();
    return 1;
  }
  return 0;
}

RH_C_FUNCTION int ON_XMLVariant_Get4dPointValue(const ON_XMLVariant* pV, ON_4dPoint* v)
{
  if (pV && v)
  {
    *v = pV->As4dPoint();
    return 1;
  }
  return 0;
}

RH_C_FUNCTION ON_UUID ON_XMLVariant_GetUuidValue(const ON_XMLVariant* pV)
{
  if (pV)
    return pV->AsUuid();

  return ON_nil_uuid;
}

RH_C_FUNCTION int ON_XMLVariant_GetXformValue(const ON_XMLVariant* pV, ON_Xform* v)
{
  if (pV && v)
  {
    *v = pV->AsXform();
    return 1;
  }
  return 0;
}

RH_C_FUNCTION time_t ON_XMLVariant_GetTimeValue(const ON_XMLVariant* pV)
{
  if (pV)
    return pV->AsTime();
  return 0;
}

RH_C_FUNCTION void ON_XMLVariant_Delete(ON_XMLVariant* p)
{
  delete p;
}
