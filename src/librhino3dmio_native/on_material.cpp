#include "stdafx.h"

RH_C_FUNCTION ON_Material* ON_Material_New(const ON_Material* pConstOther)
{
  ON_Material* rc = new ON_Material();
  if( pConstOther )
    *rc = *pConstOther;
  return rc;
}

RH_C_FUNCTION void ON_Material_CopyFrom(ON_Material* pThis, const ON_Material* pConstOther)
{
  if( pThis && pConstOther )
  {
    *pThis = *pConstOther;
  }
}

RH_C_FUNCTION void ON_Material_Default(ON_Material* pMaterial)
{
  if( pMaterial )
    *pMaterial = ON_Material::Unset;
}

RH_C_FUNCTION int ON_Material_Index(const ON_Material* pConstMaterial)
{
  if( pConstMaterial )
    return pConstMaterial->Index();
  return -1;
}

RH_C_FUNCTION int ON_Material_FindBitmapTexture(const ON_Material* pConstMaterial, const RHMONO_STRING* filename)
{
  int rc = -1;
  INPUTSTRINGCOERCE(_filename, filename);
  if( pConstMaterial )
  {
    rc = pConstMaterial->FindTexture(_filename, ON_Texture::TYPE::bitmap_texture);
  }
  return rc;
}

RH_C_FUNCTION void ON_Material_SetBitmapTexture(ON_Material* pMaterial, int index, const RHMONO_STRING* filename)
{
  INPUTSTRINGCOERCE(_filename, filename);
  if( pMaterial && index>=0 && index<pMaterial->m_textures.Count())
  {
    pMaterial->m_textures[index].m_image_file_reference.SetFullPath( _filename, false );
  }
}

RH_C_FUNCTION int ON_Material_AddBitmapTexture(ON_Material* pMaterial, const RHMONO_STRING* filename)
{
  int rc = -1;
  INPUTSTRINGCOERCE(_filename, filename);
  if( pMaterial && _filename)
  {
    rc = pMaterial->AddTexture(_filename, ON_Texture::TYPE::bitmap_texture);
  }
  return rc;
}

RH_C_FUNCTION int ON_Material_AddBumpTexture(ON_Material* pMaterial, const RHMONO_STRING* filename)
{
  int rc = -1;
  INPUTSTRINGCOERCE(_filename, filename);
  if( pMaterial && _filename)
  {
    rc = pMaterial->AddTexture(_filename, ON_Texture::TYPE::bump_texture);
  }
  return rc;
}

RH_C_FUNCTION int ON_Material_AddEnvironmentTexture(ON_Material* pMaterial, const RHMONO_STRING* filename)
{
  int rc = -1;
  INPUTSTRINGCOERCE(_filename, filename);
  if( pMaterial && _filename)
  {
    rc = pMaterial->AddTexture(_filename, ON_Texture::TYPE::emap_texture);
  }
  return rc;
}

RH_C_FUNCTION int ON_Material_AddTransparencyTexture(ON_Material* pMaterial, const RHMONO_STRING* filename)
{
  int rc = -1;
  INPUTSTRINGCOERCE(_filename, filename);
  if( pMaterial && _filename)
  {
    rc = pMaterial->AddTexture(_filename, ON_Texture::TYPE::transparency_texture);
  }
  return rc;
}

RH_C_FUNCTION bool ON_Material_ModifyTexture(ON_Material* pMaterial, ON_UUID texture_id, const ON_Texture* pConstTexture)
{
  bool rc = false;
  if( pMaterial && pConstTexture )
  {
    int index = pMaterial->FindTexture(texture_id);
    if( index>=0 )
    {
      pMaterial->m_textures[index] = *pConstTexture;
      rc = true;
    }
  }
  return rc;
}

RH_C_FUNCTION double ON_Material_GetDouble(const ON_Material* pConstMaterial, int which)
{
  const int idxShine = 0;
  const int idxTransparency = 1;
  const int idxIOR = 2;
  const int idxReflectivity = 3;
  const int idxFresnelIOR = 4;
  const int idxRefractionGlossiness = 5;
  const int idxReflectionGlossiness = 6;
 
  double rc = 0;
  if( pConstMaterial )
  {
    switch(which)
    {
    case idxShine:
      rc = pConstMaterial->m_shine;
      break;
    case idxTransparency:
      rc = pConstMaterial->m_transparency;
      break;
    case idxIOR:
      rc = pConstMaterial->m_index_of_refraction;
      break;
    case idxReflectivity:
      rc = pConstMaterial->m_reflectivity;
      break;
    case idxFresnelIOR:
      rc = pConstMaterial->m_fresnel_index_of_refraction;
      break;
    case idxRefractionGlossiness:
      rc = pConstMaterial->m_refraction_glossiness;
      break;
    case idxReflectionGlossiness:
      rc = pConstMaterial->m_reflection_glossiness;
      break;
    default:
      break;
    }
  }
  return rc;
}

RH_C_FUNCTION void ON_Material_SetDouble(ON_Material* pMaterial, int which, double val)
{
  const int idxShine = 0;
  const int idxTransparency = 1;
  const int idxIOR = 2;
  const int idxReflectivity = 3;
  const int idxFresnelIOR = 4;
  const int idxRefractionGlossiness = 5;
  const int idxReflectionGlossiness = 6;

  if( pMaterial )
  {
    switch(which)
    {
    case idxShine:
      pMaterial->SetShine(val);
      break;
    case idxTransparency:
      pMaterial->SetTransparency(val);
      break;
    case idxIOR:
      pMaterial->m_index_of_refraction = val;
      break;
    case idxReflectivity:
      pMaterial->m_reflectivity = val;
      break;
    case idxFresnelIOR:
      pMaterial->m_fresnel_index_of_refraction = val;
      break;
    case idxRefractionGlossiness:
      pMaterial->m_refraction_glossiness = val;
      break;
    case idxReflectionGlossiness:
      pMaterial->m_reflection_glossiness = val;
      break;
    default:
      break;
    }
  }
}

RH_C_FUNCTION bool ON_Material_AddTexture(ON_Material* pMaterial, const RHMONO_STRING* filename, int which)
{
  bool rc = false;
  if (pMaterial && filename)
  {
    ON_Texture::TYPE tex_type = (ON_Texture::TYPE)which;

    int index = pMaterial->FindTexture(nullptr, tex_type);

    if (index >= 0)
    {
      pMaterial->DeleteTexture(nullptr, tex_type);
    }

    INPUTSTRINGCOERCE(_filename, filename);

    rc = pMaterial->AddTexture(_filename, tex_type) >= 0;
  }
  return rc;
}

RH_C_FUNCTION bool ON_Material_SetTexture(ON_Material* pMaterial, const ON_Texture* pConstTexture, int which)
{
  bool rc = false;
  if (pMaterial && pConstTexture)
  {
    ON_Texture::TYPE tex_type = (ON_Texture::TYPE)which;

    int index = pMaterial->FindTexture(nullptr, tex_type);

    if (index >= 0)
    {
      pMaterial->DeleteTexture(nullptr, tex_type);
    }

    ON_Texture texture(*pConstTexture);
    texture.m_type = tex_type;

    rc = pMaterial->AddTexture(texture) >= 0;
  }
  return rc;
}

RH_C_FUNCTION int ON_Material_GetTexture(const ON_Material* pConstMaterial, int which)
{
  int rc = -1;
  if( pConstMaterial )
  {
    ON_Texture::TYPE tex_type = (ON_Texture::TYPE)which;

    rc = pConstMaterial->FindTexture(nullptr, tex_type);
  }
  return rc;
}

RH_C_FUNCTION int ON_Material_GetTextureCount(const ON_Material* pConstMaterial)
{
  if( pConstMaterial )
    return pConstMaterial->m_textures.Count();
  return 0;
}

RH_C_FUNCTION int ON_Material_PreviewColor(const ON_Material* pConstMaterial)
{
	return (int)pConstMaterial->PreviewColor();
}

RH_C_FUNCTION int ON_Material_GetColor( const ON_Material* pConstMaterial, int which )
{
  const int idxDiffuse = 0;
  const int idxAmbient = 1;
  const int idxEmission = 2;
  const int idxSpecular = 3;
  const int idxReflection = 4;
  const int idxTransparent = 5;
  int rc = 0;
  if( pConstMaterial )
  {
    unsigned int abgr = 0;
    switch(which)
    {
    case idxDiffuse:
      abgr = (unsigned int)(pConstMaterial->m_diffuse);
      break;
    case idxAmbient:
      abgr = (unsigned int)(pConstMaterial->m_ambient);
      break;
    case idxEmission:
      abgr = (unsigned int)(pConstMaterial->m_emission);
      break;
    case idxSpecular:
      abgr = (unsigned int)(pConstMaterial->m_specular);
      break;
    case idxReflection:
      abgr = (unsigned int)(pConstMaterial->m_reflection);
      break;
    case idxTransparent:
      abgr = (unsigned int)(pConstMaterial->m_transparent);
      break;
    default:
      break;
    }
    rc = (int)abgr;
  }
  return rc;
}

RH_C_FUNCTION void ON_Material_SetColor( ON_Material* pMaterial, int which, int argb )
{
  const int idxDiffuse = 0;
  const int idxAmbient = 1;
  const int idxEmission = 2;
  const int idxSpecular = 3;
  const int idxReflection = 4;
  const int idxTransparent = 5;
  int abgr = ARGB_to_ABGR(argb);
  if( pMaterial )
  {
    switch(which)
    {
    case idxDiffuse:
      pMaterial->m_diffuse = abgr;
      break;
    case idxAmbient:
      pMaterial->m_ambient = abgr;
      break;
    case idxEmission:
      pMaterial->m_emission = abgr;
      break;
    case idxSpecular:
      pMaterial->m_specular = abgr;
      break;
    case idxReflection:
      pMaterial->m_reflection = abgr;
      break;
    case idxTransparent:
      pMaterial->m_transparent = abgr;
      break;
    default:
      break;
    }
  }
}

RH_C_FUNCTION void ON_Material_GetName(const ON_Material* pConstMaterial, CRhCmnStringHolder* pString)
{
  if( pConstMaterial && pString )
  {
    pString->Set(pConstMaterial->Name());
  }
}

RH_C_FUNCTION void ON_Material_SetName(ON_Material* pMaterial, const RHMONO_STRING* name)
{
  if( pMaterial )
  {
    INPUTSTRINGCOERCE(_name, name);
    pMaterial->SetName( _name );
  }
}
/////////////////////////////////////////////////////////////

RH_C_FUNCTION ON_Texture* ON_Texture_New()
{
  return new ON_Texture();
}

RH_C_FUNCTION const ON_Texture* ON_Material_GetTexturePointer(const ON_Material* pConstMaterial, int index)
{
  const ON_Texture* rc = nullptr;
  if( pConstMaterial && index>=0 )
  {
    rc = pConstMaterial->m_textures.At(index);
  }
  return rc;
}

RH_C_FUNCTION int ON_Material_NextBitmapTexture(const ON_Material* pConstMaterial, int index)
{
  if( pConstMaterial )
    return pConstMaterial->FindTexture(nullptr, ON_Texture::TYPE::bitmap_texture, index);
  return -1;
}

RH_C_FUNCTION int ON_Material_NextBumpTexture(const ON_Material* pConstMaterial, int index)
{
  if( pConstMaterial )
    return pConstMaterial->FindTexture(nullptr, ON_Texture::TYPE::bump_texture, index);
  return -1;
}

RH_C_FUNCTION int ON_Material_NextEnvironmentTexture(const ON_Material* pConstMaterial, int index)
{
  if( pConstMaterial )
    return pConstMaterial->FindTexture(nullptr, ON_Texture::TYPE::emap_texture, index);
  return -1;
}

RH_C_FUNCTION int ON_Material_NextTransparencyTexture(const ON_Material* pConstMaterial, int index)
{
  if( pConstMaterial )
    return pConstMaterial->FindTexture(nullptr, ON_Texture::TYPE::transparency_texture, index);
  return -1;
}

RH_C_FUNCTION void ON_Texture_GetFileName(const ON_Texture* pConstTexture, CRhCmnStringHolder* pString)
{
  if( pConstTexture && pString )
  {
    pString->Set(pConstTexture->m_image_file_reference.FullPath());
  }
}

RH_C_FUNCTION void ON_Texture_SetFileName(ON_Texture* pTexture, const RHMONO_STRING* filename)
{
  if( pTexture )
  {
    INPUTSTRINGCOERCE(_filename, filename);
    pTexture->m_image_file_reference.SetFullPath( _filename, false );
  }
}

RH_C_FUNCTION const ON_FileReference* ON_Texture_GetFileReference(const ON_Texture* pConstTexture)
{
  const ON_FileReference* rc = nullptr;
  if (pConstTexture)
  {
    rc = new ON_FileReference(pConstTexture->m_image_file_reference);
  }
  return rc;
}

RH_C_FUNCTION bool ON_Texture_SetFileReference(ON_Texture* pTexture, const ON_FileReference* pConstFileReference)
{
  bool rc = false;
  if (pTexture)
  {
    if (pConstFileReference)
      pTexture->m_image_file_reference = *pConstFileReference;
    else
      pTexture->m_image_file_reference = ON_FileReference::Unset;
    rc = true;
  }
  return rc;
}

RH_C_FUNCTION ON_UUID ON_Texture_GetId(const ON_Texture* pConstTexture)
{
  if( pConstTexture )
    return pConstTexture->m_texture_id;
  return ON_nil_uuid;
}

RH_C_FUNCTION bool ON_Texture_GetEnabled(const ON_Texture* pConstTexture)
{
  if( pConstTexture )
    return pConstTexture->m_bOn;
  return false;
}

RH_C_FUNCTION void ON_Texture_SetEnabled(ON_Texture* pTexture, bool enabled)
{
  if( pTexture )
  {
    pTexture->m_bOn = enabled;
  }
}

RH_C_FUNCTION int ON_Texture_TextureType(const ON_Texture* pConstTexture)
{
  if( pConstTexture )
    return (int)pConstTexture->m_type;
  return (int)ON_Texture::TYPE::no_texture_type;
}

RH_C_FUNCTION void ON_Texture_SetTextureType(ON_Texture* pTexture, int texture_type)
{
  if( pTexture )
    pTexture->m_type = ON_Texture::TypeFromUnsigned(texture_type);
}


RH_C_FUNCTION int ON_Texture_Mode(const ON_Texture* pConstTexture)
{
  if( pConstTexture )
    return (int)pConstTexture->m_mode;
  return (int)ON_Texture::MODE::no_texture_mode;
}

RH_C_FUNCTION void ON_Texture_SetMode(ON_Texture* pTexture, int value)
{
  if( pTexture )
    pTexture->m_mode = ON_Texture::ModeFromUnsigned(value);
}

const int IDX_WRAPMODE_U = 0;
const int IDX_WRAPMODE_V = 1;
const int IDX_WRAPMODE_W = 2;

RH_C_FUNCTION int ON_Texture_wrapuvw(const ON_Texture* pConstTexture, int uvw)
{
  if ( nullptr == pConstTexture )
    pConstTexture = &ON_Texture::Default;

  if (uvw == IDX_WRAPMODE_U)
    return static_cast<unsigned int>(pConstTexture->m_wrapu);
  if (uvw == IDX_WRAPMODE_V)
    return static_cast<unsigned int>(pConstTexture->m_wrapv);
  if (uvw == IDX_WRAPMODE_W)
    return static_cast<unsigned int>(pConstTexture->m_wrapw);

  return static_cast<unsigned int>(ON_Texture::WRAP::repeat_wrap);
}

RH_C_FUNCTION void ON_Texture_Set_wrapuvw(ON_Texture* pTexture, int uvw, int value)
{
  if( pTexture )
  {
    if (uvw == IDX_WRAPMODE_U)
      pTexture->m_wrapu = ON_Texture::WrapFromUnsigned(value);
    else if (uvw == IDX_WRAPMODE_V)
      pTexture->m_wrapv = ON_Texture::WrapFromUnsigned(value);
    else if (uvw == IDX_WRAPMODE_W)
      pTexture->m_wrapw = ON_Texture::WrapFromUnsigned(value);
  }
}


RH_C_FUNCTION void ON_Texture_uvw(const ON_Texture* pConstTexture, ON_Xform* instanceXform)
{
  if (pConstTexture && instanceXform)
    *instanceXform = pConstTexture->m_uvw;
}

RH_C_FUNCTION void ON_Texture_Setuvw(ON_Texture* pTexture, ON_Xform* instanceXform)
{
  if (pTexture && instanceXform)
    pTexture->m_uvw = *instanceXform;
}

RH_C_FUNCTION void ON_Texture_GetAlphaBlendValues(const ON_Texture* pConstTexture, double* c, double* a0, double* a1, double* a2, double* a3)
{
  if( pConstTexture && c && a0 && a1 && a2 && a3 )
  {
    *c = pConstTexture->m_blend_constant_A;
    *a0 = pConstTexture->m_blend_A0;
    *a1 = pConstTexture->m_blend_A1;
    *a2 = pConstTexture->m_blend_A2;
    *a3 = pConstTexture->m_blend_A3;
  }
}

RH_C_FUNCTION void ON_Texture_SetAlphaBlendValues(ON_Texture* pTexture, double c, double a0, double a1, double a2, double a3)
{
  if( pTexture )
  {
    pTexture->m_blend_constant_A = c;
    pTexture->m_blend_A0 = a0;
    pTexture->m_blend_A1 = a1;
    pTexture->m_blend_A2 = a2;
    pTexture->m_blend_A3 = a3;
  }
}

RH_C_FUNCTION void ON_Texture_SetRGBBlendValues(ON_Texture* pTexture, unsigned int color, double a0, double a1, double a2, double a3)
{
  if (pTexture)
  {

    pTexture->m_blend_constant_RGB = ON_Color(color);
    pTexture->m_blend_RGB0 = a0;
    pTexture->m_blend_RGB1 = a1;
    pTexture->m_blend_RGB2 = a2;
    pTexture->m_blend_RGB3 = a3;
  }
}

RH_C_FUNCTION int ON_Texture_GetMappingChannelId(const ON_Texture* pConstTexture)
{
  if (pConstTexture) return pConstTexture->m_mapping_channel_id;
  return 0;
}

RH_C_FUNCTION ON_UUID ON_Material_PlugInId(const ON_Material* pConstMaterial)
{
  if( pConstMaterial )
    return pConstMaterial->MaterialPlugInId();
  return ON_nil_uuid;
}

RH_C_FUNCTION void ON_Material_SetPlugInId(ON_Material* pMaterial, ON_UUID id)
{
  if( pMaterial )
    pMaterial->SetMaterialPlugInId(id);
}

enum MaterialBool : int
{
  FresnelReflections,
  AlphaTransparency,
  DisableLighting
};

RH_C_FUNCTION bool ON_Material_GetBool(const ON_Material* pConstMaterial, enum MaterialBool which)
{
 
  bool rc = false;
  if( pConstMaterial )
  {
    switch(which)
    {
      case FresnelReflections:
      rc = pConstMaterial->FresnelReflections();
      break;
      case AlphaTransparency:
      rc = pConstMaterial->UseDiffuseTextureAlphaForObjectTransparencyTexture();
      break;
      case DisableLighting:
        rc = pConstMaterial->DisableLighting();
        break;
    }
  }
  return rc;
}

RH_C_FUNCTION void ON_Material_SetBool(ON_Material* pMaterial, enum MaterialBool which, bool value)
{
  if (pMaterial)
  {
    switch (which) {
    case FresnelReflections:
      pMaterial->SetFresnelReflections(value);
      break;
    case AlphaTransparency:
      pMaterial->SetUseDiffuseTextureAlphaForObjectTransparencyTexture(value);
      break;
    case DisableLighting:
      pMaterial->SetDisableLighting(value);
      break;
    }
  }

}
