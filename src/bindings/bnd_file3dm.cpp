#include "bnd_file3dm.h"
#include "base64.h"
#include "bnd_file3dmwriteoptions_gen.h"

using namespace bnd_shared;

std::string BND_File3dm_Encode(const BND_File3dm& file)
{
  return Base64(file.Gen_ToByteArray_2());
}

std::string BND_File3dm_EncodeOptions(const BND_File3dm& file, const BND_File3dmWriteOptions* options)
{
  return Base64(file.Gen_ToByteArray(options));
}

BND_File3dm* BND_File3dm_Decode(std::string buffer)
{
  std::string decoded = base64_decode(buffer);
  RH3DM_Bytes bytes;
  bytes.v.assign(decoded.begin(), decoded.end());
  return BND_File3dm::Gen_FromByteArray(bytes);
}

std::wstring BND_File3dm_RdkXml(const BND_File3dm& file)
{
  ON_wString xml;
  const ONX_Model* model = file.m_ptr;
  for (int i = 0; model && i < model->m_userdata_table.Count(); i++)
  {
    const ONX_Model_UserData* ud = model->m_userdata_table[i];
    if (ud && ONX_Model::GetRDKDocumentInformation(*ud, xml))
      return std::wstring(xml.Array());
  }
  return std::wstring();
}

// the old binding's helpers (src/bindings/bnd_extensions.cpp), reduced to finding one file:
// opennurbs' ONX_Model::GetRDKEmbeddedFile compares whole paths only
static bool SeekPastCompressedBuffer(ON_BinaryArchive& archive)
{
  if (!archive.ReadMode())
    return false;
  size_t sizeof__outbuffer;
  if (!archive.ReadCompressedBufferSize(&sizeof__outbuffer))
    return false;
  if (0 == sizeof__outbuffer)
    return true;
  unsigned int buffer_crc0 = 0;
  char method = 0;
  if (!archive.ReadInt(&buffer_crc0) || !archive.ReadChar(&method))   // 32 bit crc of the uncompressed buffer
    return false;
  if (method == 0)   // uncompressed
    return archive.SeekForward(sizeof__outbuffer);
  if (method == 1)   // compressed
  {
    ON__UINT32 tcode = 0;
    ON__INT64 big_value = 0;
    return archive.BeginRead3dmBigChunk(&tcode, &big_value) && archive.EndRead3dmChunk();
  }
  return false;
}

static bool FindEmbeddedFile(const ONX_Model_UserData& docud, const wchar_t* path, bool strict, ON_SimpleArray<unsigned char>& bytes)
{
  if (!ONX_Model::IsRDKDocumentInformation(docud))
    return false;
  ON_Read3dmBufferArchive a(docud.m_goo.m_value, docud.m_goo.m_goo, false, docud.m_usertable_3dm_version, docud.m_usertable_opennurbs_version);
  int version = 0;
  if (!a.ReadInt(&version) || 4 != version)
    return false;
  // the document data: read and dropped
  int slen = 0;
  if (!a.ReadInt(&slen) || slen <= 0 || slen + 4 > (int)docud.m_goo.m_value)
    return false;
  ON_String s;
  s.SetLength(slen);
  if (!a.ReadChar(slen, s.Array()))
    return false;
  unsigned int count = 0;
  if (!a.ReadInt(&count))
    return false;
  ON_wString searchFilename;
  ON_FileSystemPath::SplitPath(path, nullptr, nullptr, &searchFilename);
  for (unsigned int i = 0; i < count; i++)
  {
    ON_wString sPath;
    if (!a.ReadString(sPath))
      return false;
    bool match = sPath.EqualOrdinal(path, !strict);
    if (!match && !strict)
    {
      ON_wString filename;
      ON_FileSystemPath::SplitPath(sPath, nullptr, nullptr, &filename);
      match = filename.EqualOrdinal(searchFilename, true);
    }
    if (match)
    {
      size_t size;
      bool bFailedCRC = false;
      if (!a.ReadCompressedBufferSize(&size))
        return false;
      bytes.Reserve(size);
      if (!a.ReadCompressedBuffer(size, bytes.Array(), &bFailedCRC))
        return false;
      bytes.SetCount((int)size);
      return true;
    }
    SeekPastCompressedBuffer(a);
  }
  return false;
}

std::string BND_File3dm_GetEmbeddedFileAsBase64Strict(const BND_File3dm& file, std::wstring path, bool strict)
{
  const ONX_Model* model = file.m_ptr;
  ON_SimpleArray<unsigned char> bytes;
  for (int i = 0; model && i < model->m_userdata_table.Count(); i++)
  {
    const ONX_Model_UserData* ud = model->m_userdata_table[i];
    if (ud && FindEmbeddedFile(*ud, path.c_str(), strict, bytes))
      break;
  }
  return bytes.Count() > 0 ? base64_encode(bytes.Array(), bytes.UnsignedCount()) : std::string();
}

std::string BND_File3dm_GetEmbeddedFileAsBase64(const BND_File3dm& file, std::wstring path)
{
  return BND_File3dm_GetEmbeddedFileAsBase64Strict(file, path, false);
}
