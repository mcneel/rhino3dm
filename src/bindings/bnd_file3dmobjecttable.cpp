#include "bnd_file3dmobjecttable.h"
#include "bnd_geometrybase_gen.h"
#include "bnd_objectattributes_gen.h"

BND_UUID BND_File3dmObjectTable_AddAny(ONX_Model* model, const BND_GeometryBase& item, const BND_ObjectAttributes* attributes)
{
  const ON_Geometry* geometry = item.RH3DM_GeomPtr();
  if (nullptr == model || nullptr == geometry)
    return ON_UUID_to_Binding(ON_nil_uuid);
  const ON_3dmObjectAttributes* attrs = attributes ? (const ON_3dmObjectAttributes*)(attributes->RH3DM_Const()) : nullptr;
  // what rhino3dm 8.x's Add (and RhinoCommon's C layer, Internal_ONX_Model_AddModelGeometry) does
  ON_ModelComponentReference ref = model->AddModelGeometryComponent(geometry, attrs);
  return ON_UUID_to_Binding(ON_ModelGeometryComponent::FromModelComponentRef(ref, &ON_ModelGeometryComponent::Unset)->Id());
}
