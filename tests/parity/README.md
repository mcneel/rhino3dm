# Parity tests: the bindings must compute what RhinoCommon computes

One set of cases, one oracle, one runner per language.

| Piece | What it is |
|---|---|
| `cases/*.json` | Inputs: a type, an object (constructor args or a static like `Unset`), and a member, operator, or index. Chosen to hit behavior and edge cases (unset, NaN, overflow guards, signed lengths, mutators), not to enumerate members. |
| `oracle/` | .NET console app. Evaluates every case with RhinoCommon as rhino3dm's .NET build compiles it, writes `golden/*.json`. |
| `golden/*.json` | Committed expected results. Change only by rerunning the oracle, and review the diff. |
| `test_parity.py` | Python runner. Generated bindings must match **bitwise**. The old bindings run as a temporary informational column. |
| `parity_js.js` | JS runner for the generated wasm bindings. Cases expecting an exception are skipped (wasm builds without C++ exceptions). |
| `known_old_divergences.json` | Temporary: each case where the old bindings compute something different from .NET, with the reason. Missing members are counted, not listed. Delete with the old bindings. |

```sh
dotnet run --project tests/parity/oracle                      # regenerate goldens
RH3DM_MODULE_DIR=<dir with _rhino3dm*.so> pytest tests/parity  # python
node tests/parity/parity_js.js <path/to/rhino3dm.js>           # js
```

When to regenerate goldens: when `api/manifest.json`'s `BodyHash` changes for
a member a case exercises (upstream changed its logic). The golden diff is
then the review of that upstream change.

Adding a case: add it to a `cases/*.json`, run the oracle, commit both. If a
runner disagrees, the binding is wrong -- or the case found a real
RhinoCommon-vs-binding difference worth a ledger entry in
`docs/vnext/pilot-findings.md`.
