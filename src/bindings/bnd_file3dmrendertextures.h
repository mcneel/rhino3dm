// Triage F (Luis 2026-10-28; docs/vnext/rhino3dm-only-triage.md F; api/exceptions.toml
// [[hand_kept]]): file interop rhino3dm has and RhinoCommon does not.
//   F3 RdkXml: the RDK user data of an object / a model / a plug-in data entry, as XML
//   F4 File3dmEmbeddedFile.Read(path), File3dm.GetEmbeddedFileAsBase64(path[, strict])
// and File3dmRenderContentTable.Add (Luis 2026-10-28: upstream; hand-kept for 9.2, 2026-10-29),
// on the kind's collection as Find went (triage H1): File3dm.RenderMaterials.Add(material), ...
// Hand-maintained members of File3dmRenderTextures; the generated binding is bnd_file3dmrendertextures_gen.h (tools/generate/bnd_gen.py).
#pragma once

#include "bnd_common.h"
#include "bnd_file3dmrendertextures_gen.h"
#include "bnd_file3dmrendertexture_gen.h"

void BND_File3dmRenderTextures_Add(const BND_File3dmRenderTextures& table, const BND_File3dmRenderTexture& texture);
