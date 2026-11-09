#include "bnd_file3dmembeddedfiles.h"

bool BND_File3dmEmbeddedFiles_AddFile(const BND_File3dmEmbeddedFiles& table, const BND_File3dmEmbeddedFile& embeddedFile)
{
  if (nullptr == table.m_ptr)
    throw std::runtime_error("Rhino.FileIO.File3dm: the model is gone");
  const ON_EmbeddedFile* ef = embeddedFile.RH3DM_Const();
  if (nullptr == ef || 0 == ef->Length())
    return false;
  return !table.m_ptr->AddModelComponent(*ef).IsEmpty();
}
