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
