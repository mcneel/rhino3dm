#include "bnd_commonobject.h"

using namespace bnd_shared;

// ---- F1 ----------------------------------------------------------------------

BND_DICT BND_CommonObject_Encode(const BND_CommonObject& obj)
{
  BND_DICT d = RH3DM_DictNew();
  SetVersions(d);
  const ON_Object* o = obj.RH3DM_ObjPtr();
  std::string data;
  if (o)
  {
    unsigned int on_version = ON_BinaryArchive::ArchiveOpenNURBSVersionToWrite(kArchive3dm, ON::Version());
    // 256 was too small on x86 builds to account for extra data written
    ON_Write3dmBufferArchive archive(o->SizeOf() + 512, 0, kArchive3dm, on_version);
    if (archive.WriteObject(o))
      data = Base64(archive);
  }
  RH3DM_DictSet(d, "data", data);
  return d;
}

BND_CommonObject* BND_CommonObject_Decode(BND_DICT jsonObject)
{
  int rhinoversion = 0, opennurbsversion = 0;
  std::string bytes = ArchiveBytes(jsonObject, rhinoversion, opennurbsversion);
  ON_Object* o = nullptr;
  if (!bytes.empty())
  {
    ON_Read3dmBufferArchive archive(bytes.size(), bytes.data(), false, rhinoversion, (unsigned int)opennurbsversion);
    archive.ReadObject(&o);
  }
  return RH3DM_Wrap_CommonObject(o);    // adopts o (a type with no generated class is freed: null)
}

BND_DICT BND_CommonObject_ToJSON(const BND_CommonObject& obj, BND_DICT)
{
  return BND_CommonObject_Encode(obj);
}

// ---- F3 -------------------------------------------------------------------------

std::wstring BND_CommonObject_RdkXml(const BND_CommonObject& obj)
{
  ON_wString xml;
  const ON_Object* o = obj.RH3DM_ObjPtr();
  return (o && ONX_Model::GetRDKObjectInformation(*o, xml)) ? std::wstring(xml.Array()) : std::wstring();
}
