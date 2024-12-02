#include "stdafx.h"

RH_C_SHARED_ENUM_PARSE_FILE("on_material.cpp")

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

#if !defined(RHINO3DM_BUILD)
RH_C_FUNCTION ON_UUID ON_Material_RdkMaterialID(const CRhinoMaterial* pMaterial)
{
  if (pMaterial == nullptr)
    return ON_nil_uuid;

  return pMaterial->RdkMaterialInstanceId();
}


RH_C_FUNCTION ON_UUID ON_Material_MaterialChannelIdFromIndex(const CRhinoMaterial* pMaterial, int material_channel_index)
{
  if (pMaterial == nullptr)
    return ON_nil_uuid;

  // This code is copied from ON_Material::MaterialChannelIdFromIndex @ 7.x
  // 7.x should call the above function directly
  for (;;)
  {
    if (material_channel_index <= 0)
      break;
    const int count = pMaterial->m_material_channel.Count();
    if (count <= 0)
      break;
    const ON_UuidIndex* a = pMaterial->m_material_channel.Array();
    for (const ON_UuidIndex* a1 = a + count; a < a1; ++a)
    {
      if (material_channel_index == a->m_i)
        return a->m_id;
    }
    break;
  }

  return ON_nil_uuid;
}

RH_C_FUNCTION int ON_Material_MaterialChannelIndexFromId(CRhinoMaterial* pMaterial, ON_UUID material_channel_id, bool bAddIdIfNotPresent)
{
  if (pMaterial == nullptr)
    return -1;

  // This code is copied from ON_Material::MaterialChannelIndexFromId @ 7.x
  // 7.x should call the above function directly
  for (;;)
  {
    if (ON_nil_uuid == material_channel_id)
      break;
    int unused_index = 0;

    const int count = pMaterial->m_material_channel.Count();
    if (count > 0)
    {
      const ON_UuidIndex* a = pMaterial->m_material_channel.Array();
      for (const ON_UuidIndex* a1 = a + count; a < a1; ++a)
      {
        if (material_channel_id == a->m_id)
          return a->m_i;
        if (a->m_i > unused_index)
          unused_index = a->m_i;
      }
    }

    if (false == bAddIdIfNotPresent)
      break;
    if (count >= 65536)
      break; // some rogue actor filled the m_material_channel[] array.

    ++unused_index;
    if (unused_index <= 0 || unused_index > 65536)
    {
      // int overflow or too big for a material channel index
      for (unused_index = 1; unused_index <= count + 1; ++unused_index)
      {
        if (ON_nil_uuid == ON_Material_MaterialChannelIdFromIndex(pMaterial, unused_index))
          break;
      }
    }
    ON_UuidIndex ui;
    ui.m_id = material_channel_id;
    ui.m_i = unused_index;
    pMaterial->m_material_channel.Append(ui);
    return ui.m_i;
  }

  return 0;
}

RH_C_FUNCTION void ON_Material_ClearMaterialChannels(CRhinoMaterial* pMaterial)
{
  if (pMaterial != nullptr)
  {
    pMaterial->m_material_channel.Empty();
  }
}
#endif

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

RH_C_FUNCTION int ON_Texture_MinFilter(const ON_Texture* pConstTexture)
{
  if (pConstTexture)
    return (int)pConstTexture->m_minfilter;
  return (int)ON_Texture::FILTER::nearest_filter;
}

RH_C_FUNCTION int ON_Texture_MagFilter(const ON_Texture* pConstTexture)
{
  if (pConstTexture)
    return (int)pConstTexture->m_magfilter;
  return (int)ON_Texture::FILTER::nearest_filter;
}

RH_C_FUNCTION void ON_Texture_SetMinFilter(ON_Texture* pTexture, int filter)
{
  if (pTexture)
    pTexture->m_minfilter = ON_Texture::FilterFromUnsigned(filter);
}

RH_C_FUNCTION void ON_Texture_SetMagFilter(ON_Texture* pTexture, int filter)
{
  if (pTexture)
    pTexture->m_magfilter = ON_Texture::FilterFromUnsigned(filter);
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


RH_C_FUNCTION void ON_Texture_Repeat(const ON_Texture* pConstTexture, ON_2dVector* repeat)
{
  if (pConstTexture && repeat)
    *repeat = pConstTexture->Repeat();
}

RH_C_FUNCTION void ON_Texture_SetRepeat(ON_Texture* pTexture, ON_2dVector* repeat)
{
  if (pTexture && repeat)
    pTexture->SetRepeat(*repeat);
}


RH_C_FUNCTION void ON_Texture_Offset(const ON_Texture* pConstTexture, ON_2dVector* offset)
{
  if (pConstTexture && offset)
    *offset = pConstTexture->Offset();
}

RH_C_FUNCTION void ON_Texture_SetOffset(ON_Texture* pTexture, ON_2dVector* offset)
{
  if (pTexture && offset)
    pTexture->SetOffset(*offset);
}

RH_C_FUNCTION double ON_Texture_Rotation(const ON_Texture* pConstTexture)
{
  return pConstTexture ? pConstTexture->Rotation() : 0.0;
}

RH_C_FUNCTION void ON_Texture_SetRotation(ON_Texture* pTexture, double rotation)
{
  if (pTexture)
    pTexture->SetRotation(rotation);
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

RH_C_FUNCTION bool ON_Texture_IsWcsProjected(const ON_Texture* pConstTexture)
{
  if (pConstTexture)
    return pConstTexture->IsWcsProjected();
  return false;
}

RH_C_FUNCTION bool ON_Texture_TreatAsLinear(const ON_Texture* pConstTexture)
{
  if (pConstTexture)
    return pConstTexture->m_bTreatAsLinear;
  return false;
}

RH_C_FUNCTION bool ON_Texture_SetTreatAsLinear(ON_Texture* pTexture, bool value)
{
  if (pTexture)
    pTexture->m_bTreatAsLinear = value;
  return false;
}



RH_C_FUNCTION bool ON_Texture_IsWcsBoxProjected(const ON_Texture* pConstTexture)
{
  if (pConstTexture)
    return pConstTexture->IsWcsBoxProjected();
  return false;
}

#pragma region RH_C_SHARED_ENUM [TextureProjectionModes] [Rhino.DocObjects.TextureProjectionModes] [int]
/// <summary>
/// Enum describing how texture is projected onto geometry
/// </summary>
enum class TextureProjectionModes : int
{
  /// <summary>Not valid projection type</summary>
  Undefined = 0,
  /// <summary>Uses a texture mapping to generate texture coordinates</summary>
  MappingChannel = 1,
  /// <summary>Screen based</summary>
  ScreenBased = 2,
  /// <summary>World coordinate system projection</summary>
  Wcs = 3,
  /// <summary>Box type world coordinate system projection</summary>
  WcsBox = 4,
  /// <summary>Box type environment mapping</summary>
  EnvironmentMapBox = 5,
  /// <summary>Light probe type environment mapping</summary>
  EnvironmentMapLightProbe = 6,
  /// <summary>Spherical environment mapping</summary>
  EnvironmentMapSpherical = 7,
  /// <summary>Cube type environment mapping</summary>
  EnvironmentMapCube = 8,
  /// <summary>Vertical cross cube type environment mapping</summary>
  EnvironmentMapVCrossCube = 9,
  /// <summary>Horizontal cross type environment mapping</summary>
  EnvironmentMapHCrossCube = 10,
  /// <summary>Hemispherical environment mapping</summary>
  EnvironmentMapHemispherical = 11,
  /// <summary>Emap type environment mapping</summary>
  EnvironmentMapEmap = 12,
  /// <summary>Surface parameterization</summary>
  SurfaceParameterization = 13,
};
#pragma endregion

RH_C_FUNCTION TextureProjectionModes ON_Texture_GetProjectionMode(const ON_Texture* pConstTexture)
{
  if (pConstTexture)
  {
    if (ON_Texture::IsBuiltInMappingChannel(pConstTexture->m_mapping_channel_id))
    {
      TextureProjectionModes res = TextureProjectionModes::Undefined;
      const ON_Texture::MAPPING_CHANNEL mc = ON_Texture::BuiltInMappingChannelFromUnsigned(pConstTexture->m_mapping_channel_id);
      switch (mc)
      {
      case ON_Texture::MAPPING_CHANNEL::tc_channel:
        res = TextureProjectionModes::MappingChannel;
        break;
      case ON_Texture::MAPPING_CHANNEL::default_channel:
        res = TextureProjectionModes::MappingChannel;
        break;
      case ON_Texture::MAPPING_CHANNEL::screen_based_channel:
        res = TextureProjectionModes::ScreenBased;
        break;
      case ON_Texture::MAPPING_CHANNEL::wcs_channel:
        res = TextureProjectionModes::Wcs;
        break;
      case ON_Texture::MAPPING_CHANNEL::wcs_box_channel:
        res = TextureProjectionModes::WcsBox;
        break;
      case ON_Texture::MAPPING_CHANNEL::environment_map_box_channel:
        res = TextureProjectionModes::EnvironmentMapBox;
        break;
      case ON_Texture::MAPPING_CHANNEL::environment_map_light_probe_channel:
        res = TextureProjectionModes::EnvironmentMapLightProbe;
        break;
      case ON_Texture::MAPPING_CHANNEL::environment_map_spherical_channel:
        res = TextureProjectionModes::EnvironmentMapSpherical;
        break;
      case ON_Texture::MAPPING_CHANNEL::environment_map_cube_map_channel:
        res = TextureProjectionModes::EnvironmentMapCube;
        break;
      case ON_Texture::MAPPING_CHANNEL::environment_map_vcross_cube_map_channel:
        res = TextureProjectionModes::EnvironmentMapVCrossCube;
        break;
      case ON_Texture::MAPPING_CHANNEL::environment_map_hcross_cube_map_channel:
        res = TextureProjectionModes::EnvironmentMapHCrossCube;
        break;
      case ON_Texture::MAPPING_CHANNEL::environment_map_hemispherical_channel:
        res = TextureProjectionModes::EnvironmentMapHemispherical;
        break;
      case ON_Texture::MAPPING_CHANNEL::environment_map_emap_channel:
        res = TextureProjectionModes::EnvironmentMapEmap;
        break;
      case ON_Texture::MAPPING_CHANNEL::srfp_channel:
        res = TextureProjectionModes::SurfaceParameterization;
        break;
      default:
        res = TextureProjectionModes::Undefined;
        break;
      }
      return res;
    }
    else
    {
      return TextureProjectionModes::MappingChannel;
    }
  }
  return TextureProjectionModes::Undefined;
}

RH_C_FUNCTION void ON_Texture_SetProjectionMode(ON_Texture* pTexture, TextureProjectionModes projectionMode)
{
  if (pTexture)
  {
    if (projectionMode == TextureProjectionModes::MappingChannel)
    {
      if (pTexture->IsBuiltInMappingChannel(pTexture->m_mapping_channel_id))
      {
        pTexture->SetMappingChannel(1);
      }
      else
      {
        // Do not change the mapping channel if one already is set
      }
    }
    else
    {
      switch (projectionMode)
      {
      case TextureProjectionModes::Undefined:
        break;
      case TextureProjectionModes::MappingChannel:
        pTexture->SetBuiltInMappingChannel(ON_Texture::MAPPING_CHANNEL::default_channel);
        break;
          case TextureProjectionModes::ScreenBased:
            pTexture->SetBuiltInMappingChannel(ON_Texture::MAPPING_CHANNEL::screen_based_channel);
            break;
          case TextureProjectionModes::Wcs:
            pTexture->SetBuiltInMappingChannel(ON_Texture::MAPPING_CHANNEL::wcs_channel);
            break;
          case TextureProjectionModes::WcsBox:
            pTexture->SetBuiltInMappingChannel(ON_Texture::MAPPING_CHANNEL::wcs_box_channel);
            break;
          case TextureProjectionModes::EnvironmentMapBox:
            pTexture->SetBuiltInMappingChannel(ON_Texture::MAPPING_CHANNEL::environment_map_box_channel);
            break;
          case TextureProjectionModes::EnvironmentMapLightProbe:
            pTexture->SetBuiltInMappingChannel(ON_Texture::MAPPING_CHANNEL::environment_map_light_probe_channel);
            break;
          case TextureProjectionModes::EnvironmentMapSpherical:
            pTexture->SetBuiltInMappingChannel(ON_Texture::MAPPING_CHANNEL::environment_map_spherical_channel);
            break;
          case TextureProjectionModes::EnvironmentMapCube:
            pTexture->SetBuiltInMappingChannel(ON_Texture::MAPPING_CHANNEL::environment_map_cube_map_channel);
            break;
          case TextureProjectionModes::EnvironmentMapVCrossCube:
            pTexture->SetBuiltInMappingChannel(ON_Texture::MAPPING_CHANNEL::environment_map_vcross_cube_map_channel);
            break;
          case TextureProjectionModes::EnvironmentMapHCrossCube:
            pTexture->SetBuiltInMappingChannel(ON_Texture::MAPPING_CHANNEL::environment_map_hcross_cube_map_channel);
            break;
          case TextureProjectionModes::EnvironmentMapHemispherical:
            pTexture->SetBuiltInMappingChannel(ON_Texture::MAPPING_CHANNEL::environment_map_hemispherical_channel);
            break;
          case TextureProjectionModes::EnvironmentMapEmap:
            pTexture->SetBuiltInMappingChannel(ON_Texture::MAPPING_CHANNEL::environment_map_emap_channel);
            break;
          case TextureProjectionModes::SurfaceParameterization:
            pTexture->SetBuiltInMappingChannel(ON_Texture::MAPPING_CHANNEL::srfp_channel);
            break;
          default:
            break;
      }
    }
  }
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



RH_C_FUNCTION bool ON_Material_PBR_Supported(const ON_Material* p)
{
  return p ? p->IsPhysicallyBased() : false;
}

RH_C_FUNCTION void ON_Material_PBR_SynchronizeLegacyMaterial(ON_Material* p)
{
  if (p)
  {
    p->PhysicallyBased()->SynchronizeLegacyMaterial();
  }
}

RH_C_FUNCTION void ON_Material_PBR_BaseColor(const ON_Material* p, ON_4fPoint* pColor)
{
  if (p && pColor && p->IsPhysicallyBased())
  {
    auto c = p->PhysicallyBased()->BaseColor();
    pColor->x = c.Red();
    pColor->y = c.Green();
    pColor->z = c.Blue();
    pColor->w = c.Alpha();
  }
}

RH_C_FUNCTION void ON_Material_PBR_SetBaseColor(ON_Material* p, ON_4FVECTOR_STRUCT in_color)
{
  if (p && p->IsPhysicallyBased())
  {
    ON_4fColor color;
    color.SetRed(in_color.val[0]);
    color.SetGreen(in_color.val[1]);
    color.SetBlue(in_color.val[2]);
    color.SetAlpha(in_color.val[3]);
    p->PhysicallyBased()->SetBaseColor(color);
  }
}

RH_C_FUNCTION double ON_Material_PBR_Subsurface(const ON_Material* p) { return p && p->IsPhysicallyBased() ? p->PhysicallyBased()->Subsurface() : 0.0; }
RH_C_FUNCTION void ON_Material_PBR_SetSubsurface(ON_Material* p, double d) { if (p && p->IsPhysicallyBased()) { p->PhysicallyBased()->SetSubsurface(d); } }

RH_C_FUNCTION double ON_Material_PBR_SubsurfaceScatteringRadius(const ON_Material* p) { return p && p->IsPhysicallyBased() ? p->PhysicallyBased()->SubsurfaceScatteringRadius() : 0.0; }
RH_C_FUNCTION void ON_Material_PBR_SetSubsurfaceScatteringRadius(ON_Material* p, double d) { if (p && p->IsPhysicallyBased()) { p->PhysicallyBased()->SetSubsurfaceScatteringRadius(d); } }

RH_C_FUNCTION void ON_Material_PBR_SubsurfaceScatteringColor(const ON_Material* p, ON_4fPoint* pColor)
{
  if (p && pColor)
  {
    auto c = p->PhysicallyBased()->SubsurfaceScatteringColor();
    pColor->x = c.Red();
    pColor->y = c.Green();
    pColor->z = c.Blue();
    pColor->w = c.Alpha();
  }
}

RH_C_FUNCTION void ON_Material_PBR_SetSubsurfaceScatteringColor(ON_Material* p, ON_4FVECTOR_STRUCT in_color)
{
  if (p)
  {
    ON_4fColor color;
    color.SetRed(in_color.val[0]);
    color.SetGreen(in_color.val[1]);
    color.SetBlue(in_color.val[2]);
    color.SetAlpha(in_color.val[3]);
    p->PhysicallyBased()->SetSubsurfaceScatteringColor(color);
  }
}

RH_C_FUNCTION int ON_Material_PBR_BRDF(const ON_Material* p) { return p && p->IsPhysicallyBased() ? (int)p->PhysicallyBased()->BRDF() : (int)ON_PhysicallyBasedMaterial::BRDFs::GGX; }
RH_C_FUNCTION void ON_Material_PBR_SetBRDF(ON_Material* p, int i) { if (p && p->IsPhysicallyBased()) { p->PhysicallyBased()->SetBRDF((ON_PhysicallyBasedMaterial::BRDFs)i); } }

RH_C_FUNCTION double ON_Material_PBR_Metallic(const ON_Material* p) { return p && p->IsPhysicallyBased() ? p->PhysicallyBased()->Metallic() : 0.0; }
RH_C_FUNCTION void ON_Material_PBR_SetMetallic(ON_Material* p, double d) { if (p && p->IsPhysicallyBased()) { p->PhysicallyBased()->SetMetallic(d); } }

RH_C_FUNCTION double ON_Material_PBR_Specular(const ON_Material* p) { return p && p->IsPhysicallyBased() ? p->PhysicallyBased()->Specular() : 0.0; }
RH_C_FUNCTION void ON_Material_PBR_SetSpecular(ON_Material* p, double d) { if (p && p->IsPhysicallyBased()) { p->PhysicallyBased()->SetSpecular(d); } }

RH_C_FUNCTION double ON_Material_PBR_ReflectiveIOR(const ON_Material* p) { return p && p->IsPhysicallyBased() ? p->PhysicallyBased()->ReflectiveIOR() : 0.0; }
RH_C_FUNCTION void ON_Material_PBR_SetReflectiveIOR(ON_Material* p, double d) { if (p && p->IsPhysicallyBased()) { p->PhysicallyBased()->SetReflectiveIOR(d); } }

RH_C_FUNCTION double ON_Material_PBR_SpecularTint(const ON_Material* p) { return p && p->IsPhysicallyBased() ? p->PhysicallyBased()->SpecularTint() : 0.0; }
RH_C_FUNCTION void ON_Material_PBR_SetSpecularTint(ON_Material* p, double d) { if (p && p->IsPhysicallyBased()) { p->PhysicallyBased()->SetSpecularTint(d); } }

RH_C_FUNCTION double ON_Material_PBR_Roughness(const ON_Material* p) { return p && p->IsPhysicallyBased() ? p->PhysicallyBased()->Roughness() : 0.0; }
RH_C_FUNCTION void ON_Material_PBR_SetRoughness(ON_Material* p, double d) { if (p && p->IsPhysicallyBased()) { p->PhysicallyBased()->SetRoughness(d); } }

RH_C_FUNCTION double ON_Material_PBR_Anisotropic(const ON_Material* p) { return p && p->IsPhysicallyBased() ? p->PhysicallyBased()->Anisotropic() : 0.0; }
RH_C_FUNCTION void ON_Material_PBR_SetAnisotropic(ON_Material* p, double d) { if (p) { p->PhysicallyBased()->SetAnisotropic(d); } }

RH_C_FUNCTION double ON_Material_PBR_AnisotropicRotation(const ON_Material* p) { return p && p->IsPhysicallyBased() ? p->PhysicallyBased()->AnisotropicRotation() : 0.0; }
RH_C_FUNCTION void ON_Material_PBR_SetAnisotropicRotation(ON_Material* p, double d) { if (p && p->IsPhysicallyBased()) { p->PhysicallyBased()->SetAnisotropicRotation(d); } }

RH_C_FUNCTION double ON_Material_PBR_Sheen(const ON_Material* p) { return p && p->IsPhysicallyBased() ? p->PhysicallyBased()->Sheen() : 0.0; }
RH_C_FUNCTION void ON_Material_PBR_SetSheen(ON_Material* p, double d) { if (p && p->IsPhysicallyBased()) { p->PhysicallyBased()->SetSheen(d); } }

RH_C_FUNCTION double ON_Material_PBR_SheenTint(const ON_Material* p) { return p && p->IsPhysicallyBased() ? p->PhysicallyBased()->SheenTint() : 0.0; }
RH_C_FUNCTION void ON_Material_PBR_SetSheenTint(ON_Material* p, double d) { if (p && p->IsPhysicallyBased()) { p->PhysicallyBased()->SetSheenTint(d); } }

RH_C_FUNCTION double ON_Material_PBR_Clearcoat(const ON_Material* p) { return p && p->IsPhysicallyBased() ? p->PhysicallyBased()->Clearcoat() : 0.0; }
RH_C_FUNCTION void ON_Material_PBR_SetClearcoat(ON_Material* p, double d) { if (p && p->IsPhysicallyBased()) { p->PhysicallyBased()->SetClearcoat(d); } }

RH_C_FUNCTION double ON_Material_PBR_ClearcoatRoughness(const ON_Material* p) { return p && p->IsPhysicallyBased() ? p->PhysicallyBased()->ClearcoatRoughness() : 0.0; }
RH_C_FUNCTION void ON_Material_PBR_SetClearcoatRoughness(ON_Material* p, double d) { if (p && p->IsPhysicallyBased()) { p->PhysicallyBased()->SetClearcoatRoughness(d); } }

RH_C_FUNCTION double ON_Material_PBR_OpacityIOR(const ON_Material* p) { return p && p->IsPhysicallyBased() ? p->PhysicallyBased()->OpacityIOR() : 0.0; }
RH_C_FUNCTION void ON_Material_PBR_SetOpacityIOR(ON_Material* p, double d) { if (p && p->IsPhysicallyBased()) { p->PhysicallyBased()->SetOpacityIOR(d); } }

RH_C_FUNCTION double ON_Material_PBR_Opacity(const ON_Material* p) { return p && p->IsPhysicallyBased() ? p->PhysicallyBased()->Opacity() : 0.0; }
RH_C_FUNCTION void ON_Material_PBR_SetOpacity(ON_Material* p, double d) { if (p && p->IsPhysicallyBased()) { p->PhysicallyBased()->SetOpacity(d); } }

RH_C_FUNCTION double ON_Material_PBR_OpacityRoughness(const ON_Material* p) { return p && p->IsPhysicallyBased() ? p->PhysicallyBased()->OpacityRoughness() : 0.0; }
RH_C_FUNCTION void ON_Material_PBR_SetOpacityRoughness(ON_Material* p, double d) { if (p && p->IsPhysicallyBased()) { p->PhysicallyBased()->SetOpacityRoughness(d); } }

RH_C_FUNCTION double ON_Material_PBR_Alpha(const ON_Material* p) { return p && p->IsPhysicallyBased() ? p->PhysicallyBased()->Alpha() : 1.0; }
RH_C_FUNCTION void ON_Material_PBR_SetAlpha(ON_Material* p, double d) { if (p && p->IsPhysicallyBased()) { p->PhysicallyBased()->SetAlpha(d); } }

RH_C_FUNCTION bool ON_Material_PBR_BaseColorTextureAlphaForObjectAlphaTransparencyTexture(ON_Material* p) { return p && p->IsPhysicallyBased() ? p->PhysicallyBased()->UseBaseColorTextureAlphaForObjectAlphaTransparencyTexture() : true; }
RH_C_FUNCTION void ON_Material_PBR_SetBaseColorTextureAlphaForObjectAlphaTransparencyTexture(ON_Material* p, bool b) { if (p && p->IsPhysicallyBased()) { p->PhysicallyBased()->SetUseBaseColorTextureAlphaForObjectAlphaTransparencyTexture(b); } }

RH_C_FUNCTION void ON_Material_PBR_Emission(const ON_Material* p, ON_4fPoint* pColor)
{
  if (p && pColor && p->IsPhysicallyBased())
  {
    auto c = p->PhysicallyBased()->Emission();
    pColor->x = c.Red();
    pColor->y = c.Green();
    pColor->z = c.Blue();
    pColor->w = c.Alpha();
  }
}

RH_C_FUNCTION void ON_Material_PBR_SetEmission(ON_Material* p, ON_4FVECTOR_STRUCT in_color)
{
  if (p && p->IsPhysicallyBased())
  {
    ON_4fColor color;
    color.SetRed(in_color.val[0]);
    color.SetGreen(in_color.val[1]);
    color.SetBlue(in_color.val[2]);
    color.SetAlpha(in_color.val[3]);
    p->PhysicallyBased()->SetEmission(color);
  }
}

RH_C_FUNCTION bool ON_Material_IsPhysicallyBased(const ON_Material* p)
{
  if (p)
  {
    return p->IsPhysicallyBased();
  }
  return false;
}

RH_C_FUNCTION void ON_Material_ConvertToPBR(ON_Material* p)
{
  if (p)
  {
    p->ToPhysicallyBased();
  }
}
