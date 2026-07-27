# rhino3dm 8.x — outstanding issues triage (for the 8.32 release)

> Generated 2026-06-26. Sources: YouTrack project RH3DM (release target 8.x), RH-86691, and the Blender/Linux discourse thread. Each issue was analyzed against the current code in `rhino3dm`, `rhino` (8.32), and `opennurbs`. Effort: **S** < 1 day · **M** 1–3 days · **L** > 3 days / architectural / needs upstream Rhino release.

## Headline

- **Already fixed, just needs the release:** the Blender/Linux segfault. This alone justifies cutting 8.32.
- **Genuine bug worth fixing in 8.32:** RH-86691 (WASM out-of-bounds crash creating instance definitions with meshes) — has a contained rhino3dm-side mitigation.
- **Cheap parity/quality wins for 8.32:** GetTightBoundingBox, the zlib C4081 warning, `__str__` move, C4267 casts.
- **Business-driven feature:** SubD read API (Adidas/Blender) — moderate, reading-only scope fits 8.32.
- **Cross-cutting theme:** several issues (RH-86691, RH3DM-159, and the viewport-camera fix already done this session) are the *same family* — object lifetime/ownership across the embind boundary + opennurbs private/back-pointer handling. And the **decal/RDK API churn** ties together RH3DM-192, 159, and the C4996 half of 183 — best resolved by one coordinated decal migration in 9.x.

## 8.32 execution plan (Phase 1 rhino3dm → Phase 2 three.js loader)

The loader work is sequenced **after** the rhino3dm fixes it consumes: fix/verify rhino3dm first, then update the three.js `3DMLoader` to render what rhino3dm now produces.

### Phase 1 — rhino3dm changes for 8.32

*Already done this session (validated; needs the release cut):*
- **RH3DM-192** — RDK decal shared_ptr API across all three layers; `rdk_decals.cs` verbatim sync; sync exception eliminated. (In-memory authoring round-trip deferred → 9.x.)
- **RH3DM-159** — *partial:* lifetime safety (`shared_ptr<ON_Decal>`) + JS↔Python parity shipped. "Not sticky" symptom is opennurbs-level (RH-86089) → 9.x, linked to RH3DM-194.

*Remaining, land in 8.32:*
- **RH3DM-183a** — C4267 `size_t→int` casts (`bnd_polyline`/`bnd_embedded_file`/`bnd_extensions`). S.
- **RH3DM-165** — C# sample for the basic `Material` + `MaterialIndex` path. S (doc).
- **RH3DM-170** — finish wiring `…FromMaterial` / `GetCachedTextureCoordinates` Py/JS methods (native already synced). M. **DONE in 8.32**: added `Mesh.SetCachedTextureCoordinatesFromMaterial(File3dm, Guid, Material)` + `GetCachedTextureCoordinatesFromTexture(File3dm, Guid, Texture)` (ONX_Model/headless counterparts to RhinoCommon's RHINO_SDK-only `RhinoObject` overloads) in Py/JS (`bnd_mesh`) and .NET (separate partial file `opennurbs_mesh.rhino3dm.cs`), with tests in all three langs (positive path validated against `tests/models/meshWithTexture.3dm`). **Follow-up (post-8.32):** push the `File3dm`/`ONX_Model` .NET overloads upstream into RhinoCommon `opennurbs_mesh.cs` on the rhino **8.x** branch; once upstream they sync down and the rhino3dm `opennurbs_mesh.rhino3dm.cs` extension must be removed.
- **SubD read cluster (178 parent; 175 Face / 176 Edge / 177 Vertex / 169 Adidas crease)** — `BND_SubDVertex/Edge/Face` read accessors + tag enums, read-only. Verify RH-86070/RH-86274 iterator fixes are in the 8.32 submodule. M. **DONE in 8.32**: Py/JS `bnd_subd` now wraps `SubDVertexTag`/`SubDEdgeTag`, `SubDVertex`/`SubDEdge`/`SubDFace` (control-net points, tags incl. crease, connectivity via VertexFrom/To, VertexAt/EdgeAt/FaceAt, Next/Previous) and `SubD.Vertices/Edges/Faces` list wrappers + counts. .NET already had the API via RhinoCommon sync — validated it resolves at runtime headless (no PLUS-gating). Tests in all three langs, positive read path (26v/48e/24f, 11 crease edges) validated against `tests/models/subdBox.3dm`. Write/construction still deferred to 9.x.

### Phase 2 — three.js `3DMLoader` (after Phase 1)

- **RH3DM-190 — DONE (loader side)**, purely loader-side; **no rhino3dm change** (close/redirect the rhino3dm issue as not-a-bug; reporter is on v7.14). Fixed in `three.js/examples/jsm/loaders/3DMLoader.js` (branch `mcneel-dev`): (1) opacity inversion — the PBR branch now sets `mat.opacity = pbr.opacity` + `mat.transparent = pbr.opacity < 1`. NB: PBR `Opacity` is **1 = opaque** (glTF/Disney convention, same as three.js — *not* flipped), whereas the legacy `Material.transparency` **is** flipped (0 = opaque) and is handled separately by `opacity: 1 - transparency`. The bug: legacy `transparency` stays 0 even when PBR opacity < 1 (verified they don't sync), so the loader rendered every transparent PBR material as opaque; (2) `emissive`/`specularColor` were fed raw 0–255 → now ÷255 like `diffuseColor`; (3) removed the `pbr.opacity===0 && transparency===1` special-case that papered over #1; (4) removed `mat.transmission = 1 - pbr.opacity` — Rhino PBR Opacity is coverage/alpha, not glass transmission, so opacity maps to `mat.opacity` only and `mat.transmission` stays at its default (0). Glass would come from the separate transmission texture channel, not the opacity scalar.
- **Decal display (candidate follow-up to 192/159):** now that rhino3dm reads decals reliably, extend the loader to render them. Not a current YouTrack issue — file/scope if pursued (branch `luis/8.x/decals`).

## Prioritized table

| Issue | Title | Status | Effort | Recommendation |
|---|---|---|---|---|
| discourse | Blender/Linux segfault (zlib symbol clash) | **Fixed in code, unreleased** (PR #717) | S (release) | **Ship in 8.32** |
| RH3DM-179 | OpenNURBS zlib C4081 MSVC warning | **DONE in 8.32** (`ON_CMAKE_BUILD` in `src/CMakeLists.txt`) | S (1 line) | ✅ |
| RH3DM-188 | Expose `GetTightBoundingBox` (Py/JS) | **DONE in 8.32** (`bnd_geometry.cpp` + tests in py/js/dotnet) | S | ✅ |
| RH3DM-180 | Move `__str__` out of `__init__.py` | **DONE in 8.32** (moved to `bnd_point.cpp`; `__init__.py` clean) | S | ✅ |
| RH3DM-183a | `/W3` C4267 size_t→int casts | Open | S | **8.32** (quick win) |
| RH3DM-190 | PBR materials in web viewers | **Loader fix DONE (three.js `mcneel-dev`)**; rhino3dm side = not-a-bug (close/redirect) | S | ✅ loader |
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

- **RH3DM-192 (RDK decals/c_rdk): DONE in 8.32.** Adopted the opennurbs shared_ptr decal API across all three layers. `rdk_decals.cs` now syncs **verbatim** from RhinoCommon (RHINO_SDK-gated methods compile out); the C exports it needs are provided **portably** in `librhino3dm_native/on_decals.cpp` (`SharedPtr_ON_Decal_*`, `ON_Decal_IsVisible`, `Rdk_DecalCreateParams_*`, `Rdk_Decal_AddDecal` via `AddDecalEx()`, `Rdk_Decal_NewDecalSharedPtrFromObjectAttributes` via `GetDecalArray`+CRC). Legacy hand-restored block removed from `on_3dm_attributes.cpp`; MethodGen regenerated (clean decal-scoped diff); dotnet build 0 errors. **The decal sync exception is eliminated.** (Wholesale `c_rdk` sync remains impossible — it binds the closed, app-coupled RDK plugin runtime; only opennurbs `ON_*` RDK data types are bindable.) Caveat: in-memory decal *authoring* doesn't round-trip (opennurbs commits to user data only at archive write + `ON_DecalCollection::operator=` drops pending XML) — reading decals from a Rhino-authored .3dm works.
- **RH3DM-159 (decal settings not sticky): PARTIAL in 8.32 — symptom NOT fixed.** The actual report is "settings not sticky in JS, fine in Python/C#." The user-visible stickiness is **opennurbs-level and now affects both bindings**: `ON_DecalCollection::GetDecalArray()` clears+repopulates from committed RDK user data every call (RH-86089), so an in-memory edit (`attr.Decals[0].Transparency = x`) is discarded on the next read; no headless commit path (commit is archive-write-time only). **Same root cause as RH3DM-194** (edits vs adds). What 8.32 *did* ship: (1) `shared_ptr<ON_Decal>` wrappers → no dangling/double-free; (2) decal-table copy ctor shares parent attributes → **JS↔Python parity** (the original JS-specific discrepancy is gone, both behave identically now); (3) `rdk_decals.cs` verbatim sync. Lifetime safety + parity are covered by Py/JS tests (incl. a populated-array guard reading `tests/models/sphereDecals.3dm`); the stickiness symptom needs the Rhino-9 opennurbs decal-collection rework — **keep open, linked to RH3DM-194.**
- **RH3DM-185 (assign RenderMaterial to object):** real gap — can create a RenderMaterial but can't build one from a `Material`/PBR (no `FromMaterial`/`CreateBasicMaterial` in the subset) nor link it to a `File3dmObject` (attributes only carry an int `MaterialIndex`). Cross-layer design; fits the vNext canonical-RhinoCommon direction.
- **RH3DM-191 (large-model JS perf):** two rhino3dm-side hotspots — `ToThreejsJSON*` builds plain JS arrays element-by-element across embind (vs the zero-copy `typed_memory_view` already used by `ToByteArray2`), and per-object wrapper churn via `ModelObjectAt`/`CreateWrapper`; parse is single-threaded (so worker-count is irrelevant). 8.32 could land an additive typed-array mesh path, but full benefit needs a paired three.js loader change — fits ROADMAP O5 (zero-copy typed arrays) for 9.x.
- **RH3DM-171 (Linux empty tables):** still papered over — manylinux/sdist build `--debug` then `strip`; root optimizer bug never found. pybind11 3.0.4 didn't touch it. **Keep the workaround for 8.32 but verify the CI test actually asserts non-empty Layers/Linetypes/Groups/InstanceDefinitions.** Real fix belongs with the 9.x nanobind port.
- **RH3DM-181 / 182 (build-unit deps; debuggable Windows Python build):** developer-experience; broad/infra changes that pair with the vNext binding-layer rework.
- **RH3DM-183b (C4996 RDK deprecations + `/WX`):** resolve only once the deprecated `ONX_Model` RDK accessors migrate — don't enable `/WX` before then or it masks the migration.
