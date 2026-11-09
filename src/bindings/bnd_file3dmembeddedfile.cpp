#include "bnd_file3dmembeddedfile.h"

// ---- F4 -------------------------------------------------------------------------

BND_File3dmEmbeddedFile* BND_File3dmEmbeddedFile_Read(std::wstring fileName)
{
  ON_EmbeddedFile* ef = new ON_EmbeddedFile;
  if (!ef->LoadFromFile(fileName.c_str()))
  {
    delete ef;
    return nullptr;
  }
  return new BND_File3dmEmbeddedFile(ef);
}

BND_File3dmEmbeddedFile* BND_File3dmEmbeddedFile_FromByteArray(std::string bytes)
{
  ON_Buffer b;
  b.Write(bytes.length(), bytes.data());
  b.SeekFromStart(0);
  ON_EmbeddedFile* ef = new ON_EmbeddedFile;
  ef->LoadFromBuffer(b);          // as the old one: the result is returned whether or not it loaded
  return new BND_File3dmEmbeddedFile(ef);
}

BND_File3dmEmbeddedFile* BND_File3dmEmbeddedFile_FromByteArrayNamed(std::string bytes, std::wstring filename)
{
  BND_File3dmEmbeddedFile* rc = BND_File3dmEmbeddedFile_FromByteArray(bytes);
  rc->m_ptr->SetFilename(filename.c_str());
  return rc;
}

RH3DM_Bytes BND_File3dmEmbeddedFile_ToByteArray(const BND_File3dmEmbeddedFile& file)
{
  RH3DM_Bytes rc;
  const ON_EmbeddedFile* ef = file.RH3DM_Const();
  ON_Buffer b;
  if (nullptr == ef || !ef->SaveToBuffer(b))
    return rc;
  rc.v.resize((size_t)b.Size());
  b.SeekFromStart(0);
  if (!rc.v.empty() && b.Read(rc.v.size(), rc.v.data()) != rc.v.size())
    rc.v.clear();
  return rc;
}
