#include "bnd_file3dmrenderenvironments.h"
#include "bnd_file3dmrenderenvironment_gen.h"

using namespace bnd_shared;

void BND_File3dmRenderEnvironments_Add(const BND_File3dmRenderEnvironments& table, const BND_File3dmRenderEnvironment& environment)
{
  AddRenderContent(table.m_ptr, environment);
}
