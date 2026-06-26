# rhino3dm 8.x — outstanding issues triage (for the 8.32 release)

> Generated 2026-06-26. Sources: YouTrack project RH3DM (release target 8.x), RH-86691, and the Blender/Linux discourse thread. Each issue was analyzed against the current code in `rhino3dm`, `rhino` (8.32), and `opennurbs`. Effort: **S** < 1 day · **M** 1–3 days · **L** > 3 days / architectural / needs upstream Rhino release.

## Headline

- **Already fixed, just needs the release:** the Blender/Linux segfault. This alone justifies cutting 8.32.
- **Genuine bug worth fixing in 8.32:** RH-86691 (WASM out-of-bounds crash creating instance definitions with meshes) — has a contained rhino3dm-side mitigation.
- **Cheap parity/quality wins for 8.32:** GetTightBoundingBox, the zlib C4081 warning, `__str__` move, C4267 casts.
- **Business-driven feature:** SubD read API (Adidas/Blender) — moderate, reading-only scope fits 8.32.
- **Cross-cutting theme:** several issues (RH-86691, RH3DM-159, and the viewport-camera fix already done this session) are the *same family* — object lifetime/ownership across the embind boundary + opennurbs private/back-pointer handling. And the **decal/RDK API churn** ties together RH3DM-192, 159, and the C4996 half of 183 — best resolved by one coordinated decal migration in 9.x.

## Prioritized table

| Issue | Title | Status | Effort | Recommendation |
|---|---|---|---|---|
| discourse | Blender/Linux segfault (zlib symbol clash) | **Fixed in code, unreleased** (PR #717) | S (release) | **Ship in 8.32** |
| RH3DM-179 | OpenNURBS zlib C4081 MSVC warning | Partial | S (1 line) | **8.32** |
| RH3DM-188 | Expose `GetTightBoundingBox` (Py/JS) | Open | S | **8.32** |
| RH3DM-180 | Move `__str__` out of `__init__.py` | Open | S | **8.32** (quick win) |
| RH3DM-183a | `/W3` C4267 size_t→int casts | Open | S | **8.32** (quick win) |
| RH3DM-190 | PBR materials in web viewers | Not a rhino3dm bug (viewer-side, v7.14) | S (reply) | **Close / redirect** |
| RH3DM-165 | C# sample for adding RenderMaterial | Doc task | S | **8.32** doc (basic path) |
| RH-86691 | WASM out-of-bounds: instance def + mesh | **Fixed in 8.32 bindings (validated)** | M (done) | Shipped fix; **review upstream in OpenNURBS** |
| RH3DM-170 | Cached texture coordinates | Partial (native done; bindings missing) | M | **8.32** (finish wiring) |
| SubD cluster (178/175/176/177/169) | Wrap SubD read API (faces/edges/verts/crease) | Barely started | M (read-only) | **8.32 partial** (unblocks Adidas) |
| RH3DM-191 | Large models — JS parse perf | Architectural | M (partial) / L (full) | 8.32 partial mitigation; **9.x** full |
| RH3DM-192 | Integrate RDK changes (decals/c_rdk) | Open (old-API exception in place) | S | **9.x** (API break) |
| RH3DM-159 | Decal settings not sticky in JS | Open | M | **9.x** (interim: fix `_owned` double-free) |
| RH3DM-185 | Assign RenderMaterial to File3dmObject | Open (real API gap) | M | **9.x** |
| RH3DM-171 | Linux release build empty tables | Workaround in place | S (keep) / L (root-fix) | Keep + **verify CI**; root-fix 9.x |
| RH3DM-181 | Reduce compilation-unit dependencies | Open | M | **9.x** |
| RH3DM-182 | Debuggable Windows Python build | Open | M | **9.x** |
| RH3DM-183b | C4996 RDK deprecations + `/WX` | Open | M | **9.x** (after RDK migration) |

Already **resolved** (no action): RH3DM-174, 168, 167, 162, 161, 160, 158, 157, 156, 150.

## Detail

### Ship in 8.32

**Blender/Linux segfault (discourse 206986)** — *Fixed, unreleased.* zlib/opennurbs symbols collided with the zlib bundled in Blender's OpenVDB. Fixed by PR #717 (`4f99ecbc`, `77b31574`) adding `-fvisibility=hidden` + hidden visibility presets on `zlib_static`/`opennurbs_static`/`_rhino3dm`. Present on `luis/8.32`; latest released tag is only 8.17.0, so no shipped wheel has it. **Action: cut the 8.32 release.**

**RH-86691 — WASM out-of-bounds (instance def + mesh).** *Fixed in rhino3dm bindings (8.32); validated.*

_Abridged summary:_ Adding instance definitions whose objects carry meshes threw `memory access out of bounds` in JS/WASM. Cause: `File3dmInstanceDefinitionTable::Add` does `attributes[i].as<BND_3dmObjectAttributes>()` (`bnd_extensions.cpp:1343`), a **by-value copy**; with no explicit copy ctor, the default shallow-copies the underlying `ON_3dmObjectAttributes` so the temporary copy and the JS-owned wrapper alias it, and tearing down the temporary leaves the survivor's mesh-modifier back-pointer dangling. Data-size dependent (the freed block must be recycled to fault: ~3k verts usually doesn't, ~20k reliably does) and JS-only (embind `.as<>` copies; pybind `py::cast` returns a reference).

**Fix (this is a fix in the rhino3dm bindings):** explicit deep-copy copy ctor for `BND_3dmObjectAttributes` (`bnd_3dm_attributes.cpp`) — the copy owns an independent `ON_3dmObjectAttributes`, removing the aliasing/use-after-free. Validated: a 20k-vert repro crashes on the pre-fix build and passes post-fix; covered by `tests/javascript/instanceDefinitionMesh.test.js` (+ a Python parity guard).

**Should be reviewed upstream in OpenNURBS:** the rhino3dm deep copy resolves the user-visible crash, but the underlying concern is whether `ON_3dmObjectAttributes` copy (`CopyHelper`'s `*m_private = *src.m_private` over the defaulted `ON_3dmObjectAttributesPrivate::operator=`) correctly re-seats the private/back-pointer state to the new owner. Worth an OpenNURBS review so a by-value attributes copy is safe for all consumers (would also let the binding-side mitigation be dropped later).

**RH3DM-188 — GetTightBoundingBox (Py/JS).** *Open; S.* Not exposed anywhere; only loose `GetBoundingBox` is. `ON_Geometry::GetTightBoundingBox` is **public** (no OPENNURBS_PLUS gate) and virtual, so one method on `BND_GeometryBase` (`bnd_geometry.cpp`, next to the existing `BoundingBox()`) covers all 16 subclasses for both Py and JS. ~10–20 lines.

**RH3DM-179 — zlib C4081 (MSVC).** *Partial; S.* opennurbs 8.32 guards the pragma on `ON_CMAKE_BUILD`. The native `librhino3dm_native/CMakeLists.txt` defines `ON_CMAKE_BUILD` (safe), but `src/CMakeLists.txt` (JS/Py) does not, so an MSVC Python build still warns. **Fix: add `add_definitions(-DON_CMAKE_BUILD)` to `src/CMakeLists.txt`** (and optionally drop the valueless `-DOPENNURBS_ZLIB_LIB_DIR`).

**RH3DM-170 — cached texture coordinates.** *Partial; M.* Native `_ONX_Model` exports + P/Invoke already synced (rhino PR #72661); an `ON_TextureCoordinates` wrapper exists. Missing: the `…FromMaterial`/ONX_Model managed + Py/JS methods (current bindings only expose the simple `CachedTextureCoordinates(mappingId)` overload). Additive, spans three binding surfaces.

**Quick wins:** RH3DM-180 (`__str__` → `bnd_point.cpp`, strip monkey-patch; S), RH3DM-183a (C4267 `(int)` casts in `bnd_polyline/embedded_file/extensions`; S). RH3DM-165 (write a C# sample for the proven basic-`Material` + `MaterialIndex` path; S). RH3DM-190: reply that the flow is correct, it's a three.js/viewer limitation (no glTF exporter in rhino3dm) + suggest upgrading off 7.14 — **close as not-a-bug**.

### Feature with a business driver — 8.32 partial

**SubD read API (RH3DM-178 parent; 175 Face / 176 Edge / 177 Vertex / 169 Adidas crease).** *Barely started; M (reading-only).* `bnd_subd.cpp` wraps only top-level `ON_SubD` (IsSolid, Subdivide, …) + `Mesh.CreateFromSubDControlNet`. No component classes/iterators/tags — so **crease is currently unreadable**, blocking the Adidas Blender importer. Add `BND_SubDVertex/Edge/Face` with read accessors (`ControlNetPoint`/`SurfacePoint`/`VertexTag`; `EdgeTag` for crease; connectivity) + counts + `*FromId`/iteration + the tag enums, in one file for Py+JS. Required opennurbs API is public; the iterator-access fixes (RH-86070/RH-86274, cite RH3DM-178) are already on the ON side — **verify they're in the 8.32 submodule**. Defer write/construction to 9.x.

### Defer to 9.x (architectural / API-break / needs upstream)

- **RH3DM-192 (RDK decals/c_rdk):** porting the new shared_ptr decal API is only ~50 lines (2 portable c_rdk exports + 3 trivial CreateParams shims; the other 5 are RHINO_SDK-gated), but it's a **behavior-changing swap of the entire managed Decal API** on a stable 8.x line — and the upstream header itself flags it as Rhino-9 work. Keep the old-API local exception for 8.32; land on vNext. **Make "don't re-sync the legacy decal block" a permanent sync rule.**
- **RH3DM-159 (decals not sticky JS):** `FindIndex` wraps a raw `ON_Decal*` into the collection's transient cache, which `GetDecalArray()` now **clears+repopulates every call** (RH-86089) → dangling; plus `_owned=true` double-free. Proper fix means adopting the shared_ptr decal API (collides with the 192 exception). Minor priority, no `Add` path exists. **Interim (S):** fix the `_owned=true` double-free (latent crash); full fix in 9.x.
- **RH3DM-185 (assign RenderMaterial to object):** real gap — can create a RenderMaterial but can't build one from a `Material`/PBR (no `FromMaterial`/`CreateBasicMaterial` in the subset) nor link it to a `File3dmObject` (attributes only carry an int `MaterialIndex`). Cross-layer design; fits the vNext canonical-RhinoCommon direction.
- **RH3DM-191 (large-model JS perf):** two rhino3dm-side hotspots — `ToThreejsJSON*` builds plain JS arrays element-by-element across embind (vs the zero-copy `typed_memory_view` already used by `ToByteArray2`), and per-object wrapper churn via `ModelObjectAt`/`CreateWrapper`; parse is single-threaded (so worker-count is irrelevant). 8.32 could land an additive typed-array mesh path, but full benefit needs a paired three.js loader change — fits ROADMAP O5 (zero-copy typed arrays) for 9.x.
- **RH3DM-171 (Linux empty tables):** still papered over — manylinux/sdist build `--debug` then `strip`; root optimizer bug never found. pybind11 3.0.4 didn't touch it. **Keep the workaround for 8.32 but verify the CI test actually asserts non-empty Layers/Linetypes/Groups/InstanceDefinitions.** Real fix belongs with the 9.x nanobind port.
- **RH3DM-181 / 182 (build-unit deps; debuggable Windows Python build):** developer-experience; broad/infra changes that pair with the vNext binding-layer rework.
- **RH3DM-183b (C4996 RDK deprecations + `/WX`):** resolve only once the deprecated `ONX_Model` RDK accessors migrate — don't enable `/WX` before then or it masks the migration.
