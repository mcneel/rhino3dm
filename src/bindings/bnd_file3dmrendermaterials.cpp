#include "bnd_file3dmrendermaterials.h"
#include "bnd_file3dmrendermaterial_gen.h"

using namespace bnd_shared;

void BND_File3dmRenderMaterials_Add(const BND_File3dmRenderMaterials& table, const BND_File3dmRenderMaterial& material)
{
  AddRenderContent(table.m_ptr, material);
}
