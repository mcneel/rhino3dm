// Triage F (Luis 2026-10-28; docs/vnext/rhino3dm-only-triage.md F; api/exceptions.toml
// [[hand_kept]]): file interop rhino3dm has and RhinoCommon does not.
//   F3 RdkXml: the RDK user data of an object / a model / a plug-in data entry, as XML
//   F4 File3dmEmbeddedFile.Read(path), File3dm.GetEmbeddedFileAsBase64(path[, strict])
// and File3dmRenderContentTable.Add (Luis 2026-10-28: upstream; hand-kept for 9.2, 2026-10-29),
// on the kind's collection as Find went (triage H1): File3dm.RenderMaterials.Add(material), ...
// Hand-maintained members of File3dmRenderMaterials; the generated binding is bnd_file3dmrendermaterials_gen.h (tools/generate/bnd_gen.py).
#pragma once

#include "bnd_common.h"
#include "bnd_file3dmrendermaterials_gen.h"
#include "bnd_file3dmrendermaterial_gen.h"

// a copy of the render content added to the model (ONX_Model::AddModelComponent, as the old
// File3dmRenderContentTable.Add: an id or name already in the model is made unique)
void BND_File3dmRenderMaterials_Add(const BND_File3dmRenderMaterials& table, const BND_File3dmRenderMaterial& material);
