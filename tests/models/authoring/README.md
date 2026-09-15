# Authoring the SubD test fixture

Scripts that build `tests/models/subd_creases.3dm`, the fixture behind the SubD
tests for RH3DM-169, RH3DM-175, RH3DM-176, RH3DM-177 and RH3DM-178.

**Both scripts run inside Rhino, not against rhino3dm.** rhino3dm's SubD
bindings are read-only, and edge sharpness cannot be set from outside Rhino at
all (`SubD.SetEdgeSharpness` is Rhino-SDK only), so the fixture has to be
authored here. Rhino 8.36 or later.

| file | run it | what it does |
| --- | --- | --- |
| `make_subd_fixture.py` | in Rhino | writes `tests/models/subd_creases.3dm` and adds the SubD to the current document |
| `report_subd_fixture.py` | in Rhino | reads the .3dm back and prints expected vs. actual for every value the tests hard-code |
| `../../python/subd_fixture_spec.py` | – | the values themselves, shared by both of the above and by the tests |

## The fixture

A 10-unit SubD box. Bottom ring `A`-`B`-`C`-`D`, top ring `E`-`F`-`G`-`H`, with
`E` above `A`.

```
        H---------G            z
       /|        /|            |   y
      E---------F |            |  /
      | |       | |            | /
      | D-------|-C            |/
      |/        |/             +------x
      A---------B
```

| edges | configuration |
| --- | --- |
| `AB` `BC` `CD` `DA` | hard crease |
| `AE` | hard crease - the only vertical one |
| `BF` | soft crease, sharpness 1.0 |
| `CG` | soft crease, sharpness 3.0 |
| `DH` | soft crease, tapered 0.5 at `D` to 2.5 at `H` |
| `EF` `FG` `GH` `HE` | ordinary smooth |

Nothing sets a vertex tag; they all fall out of the crease count, which is what
makes them worth asserting:

| vertex | creases | tag |
| --- | --- | --- |
| `A` | 3 | Corner |
| `B` `C` `D` | 2 | Crease |
| `E` | 1 | Dart |
| `F` `G` `H` | 0 | Smooth |

That covers every vertex tag, both crease flavours (hard and dart), uniform and
tapered sharpness, and the case where sharpness is present but the tag is not -
which is the whole point of RH3DM-169.

## Checking it

Run `report_subd_fixture.py` after authoring. It reads the file back through
RhinoCommon - a different code path from the bindings under test - and prints a
line per value:

```
  ok    A tag                                   Corner
  ok    DH sharpness at H                       2.5
  FAIL  F vertex sharpness                      expected 0, got 1 (derived)
```

Rhino is the authority. On a mismatch, fix `subd_fixture_spec.py` (or
`make_subd_fixture.py`, if the fixture is what came out wrong) and re-run. Never
"fix" a rhino3dm test to match - the tests only read the spec, and a value that
disagrees with Rhino is exactly the bug the issue is asking about.

Rows marked `(derived)` are ones RhinoCommon has no direct accessor for -
`SubDEdge.IsHardCrease`/`IsDartCrease`/`DartCount`, `SubDVertex.VertexSharpness`,
and the `SubDFace` sharpness aggregates are rhino3dm-only. They are recomputed
from the documented rule using values Rhino did give; `VertexSharpness` goes
through opennurbs' own `SubDEdgeSharpness.VertexSharpness` helper, so it is
still opennurbs' answer.

`tests/python/test_SubD_FixtureSpec.py` checks the spec's internal arithmetic -
face centres, midpoints, outward normals, subdivision counts - without needing
Rhino or a rhino3dm build. Run it after editing the spec.

## Then

```bash
cd tests/python && python -m unittest discover -p "test_SubD_*.py" -v
```
