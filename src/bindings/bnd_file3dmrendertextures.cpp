#include "bnd_file3dmrendertextures.h"
#include "bnd_file3dmrendertexture_gen.h"

using namespace bnd_shared;

void BND_File3dmRenderTextures_Add(const BND_File3dmRenderTextures& table, const BND_File3dmRenderTexture& texture)
{
  AddRenderContent(table.m_ptr, texture);
}
