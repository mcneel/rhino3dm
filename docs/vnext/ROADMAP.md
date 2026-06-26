# rhino3dm vNext — Objectives & Phased Roadmap

> Status: **draft for review** · Owner: luis@mcneel.com · Date: 2026-06-15
> This is a planning document, not an implementation. Edit freely; nothing here is built yet.

---

## 1. Context

rhino3dm ships three libraries from one repo, all sitting on the public [opennurbs](https://github.com/mcneel/opennurbs) submodule:

- **JS/WASM** — emscripten, from hand-written `src/bindings/bnd_*.cpp` that wrap `ON_*` C++ types directly.
- **Python** — pybind11 → migrating to nanobind, from the *same* `bnd_*.cpp` (preprocessor split `ON_WASM_COMPILE` / `ON_PYTHON_COMPILE`).
- **.NET** — `src/librhino3dm_native/on_*.cpp` (a hand-copied, opennurbs-only subset of the private rhino repo's `rhinocommon_c`) → MethodGen → `AutoNativeMethods.cs` P/Invoke → hand-written RhinoCommon-style C# in `src/dotnet/`.

### The two-layer problem
There are **two independent binding layers** that each restate the API surface, and they drift:
- **Layer A** (`bnd_*.cpp`) wraps opennurbs C++ directly — the right tool for JS/Py because embind/nanobind bind C++ classes natively (free OO, methods, inheritance).
- **Layer B** (`rhinocommon_c` flat C ABI) exists because .NET P/Invoke can only call flat C.

Routing JS/Py *through* the flat C ABI (the literal "everything off rhinocommon_c") would lose OO and add marshalling overhead — working against the JS perf goals. So the duplication to eliminate is **not** the two transports; it's that **the exposed API surface is specified twice and diverges**.

### The constraint that shapes everything
**rhino3dm is public; the rhino repo (home of `rhinocommon_c`) is private.** `librhino3dm_native` is a hand-copied subset precisely because the `on_*.cpp` files depend only on (public) opennurbs and can be redistributed. There is already a working precedent for this exact problem: **opennurbs is itself extracted from the private rhino repo (`rhino/src4/opennurbs`) into the public `mcneel/opennurbs` submodule.**

---

## 2. Confirmed decisions (from scoping)

| Topic | Decision |
|---|---|
| **Canonical API** | **RhinoCommon is the single source of truth for all three languages.** Bindings + docs conform to it. |
| **Conformance rule** | **Structure exact, spelling idiomatic.** Same classes/members/semantics/hierarchy as RhinoCommon; spelling per language: JS `camelCase` + getters/setters + typed arrays, Python `snake_case` + Pythonic, .NET = RhinoCommon verbatim. Mapping is deterministic & machine-checkable. |
| **Surface scope** | **Expand toward fuller RhinoCommon** — mirror the opennurbs-portable subset, grow coverage beyond today's types, and keep an explicit tracked manifest of what's in/out. |
| **Versioning** | **Clean break — new major.** Breaking API changes allowed; ship a migration guide. |
| **Top priority** | **Binding unification + sync automation.** |
| **rhinocommon_c sync** | Recommend extracting the opennurbs-portable C layer into a **public repo/submodule** (the opennurbs precedent); interim bridge = CI copy/filter script that PRs into rhino3dm. |
| **JS API** | ESM-first + modern packaging; zero-copy typed arrays; idiomatic OO (getters/setters); full WASM64. |
| **Python** | nanobind. Support CPython 3.9–3.14, all 3 desktop OS, **free-threaded/no-GIL**, Linux aarch64 + manylinux/musllinux. **Drop 3.8 and older.** |
| **Mobile** | Keep iOS/Android, **modernize off end-of-life Xamarin to .NET 8+/MAUI** native TFMs. |
| **Docs** | **One source → all languages**, generated from the RhinoCommon manifest. |

---

## 3. Target architecture: RhinoCommon-as-spec, conformance enforced by tooling

```
            ┌─────────────────────────────────────────────┐
            │  RhinoCommon C# API (private rhino repo)      │  ← canonical spec
            │  src4/DotNetSDK/rhinocommon                   │
            └───────────────┬─────────────────────────────-┘
                            │  Roslyn extractor (opennurbs-portable subset)
                            ▼
            ┌─────────────────────────────────────────────┐
            │  Canonical API Manifest (JSON, in-repo)       │  ← single artifact
            │  classes · members · signatures · in/out flag │     drives gen + checks + docs
            │  + idiom name-mapping rules (camel/snake)      │
            └───┬───────────────┬───────────────┬──────────┘
                │               │               │
       drift-check &    drift-check &    drift-check &
        generation       generation       generation
                │               │               │
                ▼               ▼               ▼
        ┌──────────────┐ ┌──────────────┐ ┌──────────────────┐
        │ JS  (embind) │ │ Py (nanobind)│ │ .NET (rhcommon_c │
        │ bnd_*.cpp    │ │ bnd_*.cpp    │ │  + C# wrappers)  │
        └──────────────┘ └──────────────┘ └──────────────────┘
                │               │               │
                ▼               ▼               ▼
          unified docs ◄────────┴───────────────┘  (one source → all)
```

- **Transports stay fit-for-purpose:** `bnd_*` (C++→embind/nanobind) for JS/Py; `rhinocommon_c` flat C + C# for .NET.
- **The manifest is what unifies them.** Every transport is *measured and (progressively) generated* against it instead of being hand-maintained in isolation.
- **`.NET` keeps RhinoCommon fidelity for free** because it already rides the same C layer RhinoCommon uses.

---

## 4. Objectives (numbered)

**O1 — Single source of truth.** Establish RhinoCommon as the canonical spec; extract a machine-readable manifest of its opennurbs-portable subset, with deterministic per-language name-mapping rules.

**O2 — Quantify & eliminate drift.** Build a checker that diffs each library's actual surface against the manifest, turning "many deviations" into a tracked, CI-gated list that trends to zero.

**O3 — Kill the manual rhinocommon_c sync.** Move from hand-copying to either a public extracted repo/submodule or an automated CI copy/filter+PR — single source, redistributable, dated.

**O4 — Generate, don't hand-maintain.** Progressively generate binding stubs (and eventually full bindings) from the manifest so adding/conforming a type is a manifest edit, not three hand-edits.

**O5 — Modern JS.** WASM64-first, zero-copy typed-array views into WASM memory, idiomatic ESM API with first-class TypeScript, modern packaging.

**O6 — Modern Python.** Finish nanobind migration; modern build (scikit-build-core + cibuildwheel); stable-ABI and free-threaded wheels; broad platform/version matrix; fold Python into the unified build.

**O7 — Modern .NET.** net8+/net9 TFMs, `net*-ios`/`net*-android` (MAUI) off dead Xamarin, source-generated P/Invoke (`[LibraryImport]`) for AOT/trim safety.

**O8 — Unified docs.** One generator off the manifest emits per-language docs to a single modern site; deviation becomes structurally impossible.

**O9 — Unified, automated build & release.** One orchestration (CMake presets + GitHub Actions matrix) covering all three libs incl. Python; remove the orphaned Python build path.

**O10 — Migration story.** Document breaking changes vs the current major; provide upgrade guides per language.

---

## 5. Tooling modernization (concrete recommendations)

### JS / emscripten
- **Upgrade 3.1.30 (Feb 2023) → 4.0.x.** Pin the exact version in `Current_Development_Tools.md` and in CI.
- **WASM64 as the primary build**, with a wasm32 fallback artifact — replace the current `NODE`-flag hack (`-sMEMORY64=1` only under `NODE`, plus the `ON_32BIT_RUNTIME`/`ON_64BIT_RUNTIME` split). Add `-sWASM_BIGINT`.
- **Emit TypeScript from embind** (`--emit-tsd` / embind tsgen) so `.d.ts` is generated, not hand-kept — feeds O8.
- **Zero-copy:** expose mesh/point-cloud/curve buffers as typed-array views over WASM heap (embind `val` + `memory_view`), not marshalled JS arrays.
- Packaging: `-sEXPORT_ES6 -sMODULARIZE`, `ENVIRONMENT=web,worker,node`, dual ESM/CJS, top-level-await init.

### Python / nanobind
- **scikit-build-core** as the build backend (replaces the bespoke root `setup.py` CMake driving).
- **cibuildwheel** for the wheel matrix: Win/macOS(arm64+x86_64)/Linux(x86_64+aarch64), manylinux + musllinux — this is also how the **orphaned Python build folds into CI**.
- **nanobind stable ABI (abi3)** → far fewer wheels across the 3.9–3.14 range; plus **free-threaded (no-GIL) wheels** for 3.13+.
- Remove pybind11 once nanobind reaches parity; drop 3.8-and-older.

### .NET
- Multi-target **net8.0/net9.0** + `net8.0-ios` / `net8.0-android` (MAUI) to retire **end-of-life Xamarin**.
- Switch generated P/Invoke from `[DllImport]` to **source-generated `[LibraryImport]`** for NativeAOT/trim compatibility; have MethodGen emit it.
- Keep `.NET` riding `rhinocommon_c` (preserves RhinoCommon fidelity).

### Cross-cutting
- **Manifest extractor via Roslyn** over the RhinoCommon C# source (docgen already parses RhinoCommon in `src/docgen/RhinoCommonClass.cs` — reuse/extend that).
- Consider **libclang** to parse `bnd_*`/`rhinocommon_c` robustly for the drift-checker instead of regex.
- **CMakePresets.json** (the rhino repo already uses presets) to standardize configure/build across platforms.
- Refresh `Current_Development_Tools.md` (last touched Feb 2023) and make CI the enforcer of pinned versions.
- Docs site: single generator → e.g. mkdocs-material or Docusaurus consuming one JSON; TypeDoc can consume the generated `.d.ts` for JS.

---

## 6. Phased roadmap

> Sequencing favors the top priority (unification + sync) and front-loads the manifest, since gen/checks/docs all depend on it. Phases 4a/4b/4c can parallelize once the manifest + checker exist.

**Phase 0 — Foundations**
- Cut the new-major branch. Refresh toolchain pins (emscripten 4.x, .NET 8/9, NDK, drop Xamarin/py≤3.8). Stand up CMakePresets + a CI skeleton.

**Phase 1 — Canonical manifest (O1)**
- Roslyn extractor → JSON manifest of the opennurbs-portable RhinoCommon subset. Define & encode the idiom name-mapping rules. Establish the in/out coverage manifest.

**Phase 2 — Drift checker (O2)**
- Tool diffs `bnd_*` and `.NET` surfaces against the manifest; emits a ranked deviation report; wire as a (initially non-blocking) CI gate. *This is the first artifact that makes "many deviations" concrete.*

**Phase 3 — rhinocommon_c sync (O3)**
- Decide public-extraction vs CI copy/filter; implement the chosen path so `librhino3dm_native` stops being a manual copy. (Needs Rhino-team coordination if extracting a public repo.)

**Phase 4a — JS modernization (O5)** · **4b — Python (O6)** · **4c — .NET (O7)**
- Per-language toolchain upgrades and idiom conformance, each driven down against the Phase-2 report.

**Phase 5 — Generation & coverage expansion (O4 + scope)**
- Generate stubs from the manifest; expand surface toward fuller RhinoCommon; flip the drift gate to blocking.

**Phase 6 — Unified docs (O8)**
- One generator off the manifest → single site, all languages.

**Phase 7 — Unified build/release + migration (O9, O10)**
- Single GH Actions matrix incl. Python; remove orphaned Python build; publish migration guides.

---

## 7. Open questions / dependencies / risks

- **Rhino-team coordination** is the critical-path dependency for O3 (public extraction of the portable C layer) and for keeping the manifest extractor pointed at a stable RhinoCommon source location.
- **Generation depth (O4):** stubs-only vs full generation is a real fork — recommend deciding *after* Phase 2 shows how mechanical the deviations actually are.
- **opennurbs emscripten-awareness:** CMake notes the WASM build still needs opennurbs adjustments (the temporary 32/64-bit `add_definitions` hack) — WASM64-first will force resolving this upstream in opennurbs.
- **Coverage expansion vs. effort:** "expand toward fuller RhinoCommon" is open-ended; the manifest's in/out flags should gate scope per release.
- **Free-threaded wheels** depend on all native deps (draco, zlib, opennurbs) being thread-safe under no-GIL — needs validation.

---

## 8. Immediate next actions (proposed)

1. Review/edit this doc and lock Phases 0–2.
2. Greenlight a **Phase 1 spike**: the Roslyn manifest extractor (highest-leverage; everything depends on it).
3. Open the Rhino-team conversation on O3 (public extraction of the opennurbs-portable C layer).

## 9. Carried over from the 8.32 sync (deferred to 9.x)

Items intentionally left at the old API during the 8.32 rhinocommon sync, to revisit in 9.x:

- **Decals → shared_ptr API.** Upstream moved decals from the deprecated raw-pointer API
  (`ON_3dmObjectAttributes::GetDecalArray()` / `AddDecal()`) to a shared_ptr API
  (`GetDecalArray(std::vector<std::shared_ptr<ON_Decal>>&)` / `AddDecalEx()`), with the new
  C exports living in `c_rdk/rdk_decals.cpp` (which carries `CRhino*` dependencies). For 8.32
  rhino3dm kept the old API: `on_decals.cpp` at the prior version, the 4 decal functions
  (`DecalCount`/`DecalAt`/`AddDecal`/`AddDecalWithCreateParams`) hand-restored into the synced
  `on_3dm_attributes.cpp`, and `rdk_decals.cs` kept at the prior version. Builds against opennurbs
  8.32 with deprecation warnings only. **9.x:** port the decal C exports to the shared_ptr API and
  adopt rhino's current `rdk_decals.cs`.
- **`BinaryFormatter` in `opennurbs_mesh.cs`.** Triggers `SYSLIB0011` (obsolete-as-error) on
  net7.0+ with modern SDKs; net8/net9 remove `BinaryFormatter` entirely. **9.x:** replace the
  `ISerializable`/`BinaryFormatter` mesh serialization.
- **`ViewportInfo` parent-pointer / camera persistence.** rhino3dm keeps a local
  `ViewportInfo.NonConstPointer()` override so `ViewInfo.Viewport` edits persist (upstream reverted
  it for RH-83697, which only affected full-Rhino UI). Our override is leak-free (non-owning proxy
  into `&ON_3dmView.m_vp`). **9.x:** adopt upstream's reworked `ViewInfo`/`ViewportInfo` ownership
  model if/when it lands, and drop the local override.
