# Parity tests: the bindings must compute what RhinoCommon computes

One set of cases, one oracle, one runner per language.

| Piece | What it is |
|---|---|
| `cases/*.json` | Inputs: a type, an object (constructor args or a static like `Unset`), and a member, operator, or index. Chosen to hit behavior and edge cases (unset, NaN, overflow guards, signed lengths, mutators), not to enumerate members. |
| `oracle/` | .NET console app. Evaluates every case with RhinoCommon as rhino3dm's .NET build compiles it, writes `golden/*.json`. |
| `golden/*.json` | Committed expected results. Change only by rerunning the oracle, and review the diff. |
| `test_parity.py` | Python runner. The bindings must match **bitwise**: fixtures through `CommonObject.Decode`, recipes built with the bindings' own classes. |
| `parity_js.js` | JS runner for the generated wasm bindings. Cases expecting an exception are skipped (wasm builds without C++ exceptions). |

```sh
bash tests/parity/oracle/run_linux.sh  # regenerate goldens (see the script: docker, fresh native build)
RH3DM_MODULE_DIR=<dir with _rhino3dm*.so> pytest tests/parity  # python
node tests/parity/parity_js.js <path/to/rhino3dm.js>           # js
```

**Exactness needs no fused multiply-add.** Compilers may fuse `a*b + c` into
one FMA instruction (gcc does at -O2 on arm64), which changes the last bit.
Goldens and every binding build under test compile with `-ffp-contract=off`;
then results are IEEE-exact across compilers, optimization levels and CPUs
(measured: Linux/gcc and macOS/clang goldens are bitwise identical). A
last-bit mismatch in a native-backed case means a build without it.

**Fixtures.** Class cases evaluate members on objects the oracle built once
and serialized (`__fixtures__` in the golden, an opennurbs archive), so a
binding's member is compared on the identical object. `construct: true` cases
instead build the object with each binding's own API, testing construction.

When to regenerate goldens: when `api/manifest.json`'s `BodyHash` changes for
a member a case exercises (upstream changed its logic). The golden diff is
then the review of that upstream change.

Adding a case: add it to a `cases/*.json`, run the oracle, commit both. If a
runner disagrees, the binding is wrong -- or the case found a real
RhinoCommon-vs-binding difference worth a ledger entry in
`docs/vnext/pilot-findings.md`.
