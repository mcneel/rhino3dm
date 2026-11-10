#include "bnd_file3dmgrouptable.h"

bool BND_File3dmGroupTable_DeleteId(const BND_File3dmGroupTable& table, BND_UUID id)
{
  ON_ModelComponentReference ref = table.m_ptr->RemoveModelComponent(ON_ModelComponent::Type::Group, RH3DM_ToUuid(id));
  return !ref.IsEmpty();
}
