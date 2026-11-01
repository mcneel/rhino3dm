"""Python parity runner: the bindings must reproduce the .NET goldens exactly.

    python3 tests/parity/test_parity.py <module_dir>     # report + exit code
    pytest tests/parity/test_parity.py                   # RH3DM_MODULE_DIR=...

Exactness: values compare bitwise (doubles via ==, NaN == NaN). The generated
value types are translated from the same C# bodies, so anything else is a bug
-- or FMA contraction, which the value-type sources must be compiled without.
"""

import json
import math
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
VALUE_TYPES = {'Point2d', 'Point3d', 'Vector3d', 'Point3f', 'Vector3f', 'Point2f', 'Interval', 'Line', 'BoundingBox',
               'Plane', 'Circle', 'Arc', 'Sphere', 'Transform'}
UNSET = -1.23432101234321e+308             # RhinoMath.UnsetValue


def load_module(module_dir):
    sys.path.insert(0, module_dir)
    import _rhino3dm
    return _rhino3dm


class Surface:
    """One binding surface: how type names map to classes."""

    def __init__(self, r):
        self.r = r
        self.recipes = {}
        self.fixtures = {}
        self.standalone = True   # own recipes, its own fixture decoding (M3)

    def cls(self, name):
        return getattr(self.r, name)

    def decode(self, v):
        if isinstance(v, str):
            return {'NaN': math.nan, 'Inf': math.inf, '-Inf': -math.inf}.get(v, v)
        if isinstance(v, dict) and ('new' in v or 'call' in v):
            return self.recipe(v)
        if isinstance(v, dict):
            (k, x), = v.items()
            if k == 'enum':
                tn, vn = x.split('.')
                return getattr(getattr(self.r, tn), vn)
            if k == 'Color':
                return tuple(int(c) for c in x)          # the bindings' (r, g, b, a)
            if k == 'Point3dList' and getattr(self, 'own', False):
                return [self.cls('Point3d')(*[float(c) for c in p]) for p in x]   # own (G108): generated points
            if k == 'Point3dList':
                pl = self.r.Point3dList(0)
                for p in x:
                    pl.Add(*[float(c) for c in p])
                return pl
            if k == 'static':
                tn, mn = x.rsplit('.', 1)
                if tn == 'RhinoMath' and mn == 'UnsetValue':
                    return UNSET
                return getattr(self.cls(tn), mn)
            args = [self.decode(a) for a in x]
            if all(isinstance(a, (int, float)) for a in args):
                args = [float(a) for a in args]
            return self.cls(k)(*args)
        return v

    def recipe(self, v):
        """build an object with THIS surface's API (the old bindings' for
        construction; the generated surface wraps the result, see make_self)"""
        if isinstance(v, str):
            fx = self.fixtures.get(v)
            if fx is not None and not getattr(self, 'construct', False):
                return self.from_fixture(fx)
            v = self.recipes[v]
        args = [self.decode(a) for a in v.get('args', [])]
        if 'new' in v:
            return self.cls(v['new'])(*args)
        if 'on' in v:
            return getattr(self.decode(v['on']), v['call'])(*args)
        tn, mn = v['call'].rsplit('.', 1)
        owner = self.cls(tn) if tn in VALUE_TYPES or getattr(self, 'own', False) else getattr(self.r, tn)
        return getattr(owner, mn)(*args)

    def from_fixture(self, fx):
        """the object exactly as .NET built it (archive round-trip)"""
        if 'Transform' in fx:
            x = self.cls('Transform')(1.0)      # old Transform / generated Transform
            for i, m in enumerate(fx['Transform']):
                setattr(x, 'M%d%d' % (i // 4, i % 4), float(m))
            return x
        return self.cls('CommonObject').Decode(fx)      # old CommonObject / generated CommonObject

    def make_self(self, case):
        # G108: each surface builds the recipe with its own classes; the generated surface always does
        # (M3: no bridge from the old API, fixtures through CommonObject.Decode)
        self.own = bool(case.get('own')) or self.standalone
        self.construct = bool(case.get('construct'))     # this case's, never the previous one's
        s = case.get('self')
        if s is None:
            return None
        if case['type'] in VALUE_TYPES and isinstance(s, dict) and 'new' in s:
            return self.recipe(s)      # value types: each binding's OWN constructors
        if isinstance(s, str) or (isinstance(s, dict) and ('new' in s or 'call' in s)):
            # a recipe: the fixture's object (archive round-trip), or (construct) built with this API
            return self.recipe(s)
        if isinstance(s, dict):
            return getattr(self.cls(case['type']), s['static'])
        return self.cls(case['type'])(*[float(self.decode(a)) for a in s])

    def evaluate(self, case):
        self_obj = self.make_self(case)
        if 'op' in case:
            a = [self.decode(x) for x in case['args']]
            op = case['op']
            if len(a) == 1:
                return {'-': lambda: -a[0]}[op]()
            return {'+': lambda: a[0] + a[1], '-': lambda: a[0] - a[1],
                    '*': lambda: a[0] * a[1], '/': lambda: a[0] / a[1],
                    '==': lambda: a[0] == a[1], '!=': lambda: a[0] != a[1],
                    '<': lambda: a[0] < a[1], '>': lambda: a[0] > a[1],
                    '<=': lambda: a[0] <= a[1], '>=': lambda: a[0] >= a[1]}[op]()
        if 'via' in case and self_obj is not None:
            self_obj = getattr(self_obj, case['via'])      # a collection view
        if 'index' in case:
            return self_obj[case['index']]
        target = self_obj if self_obj is not None else self.cls(case['type'])
        if 'set' in case:
            setattr(target, case['member'], self.decode(case['set']))
            t = getattr(target, case['then'])
            return {'then': t() if callable(t) else t}
        attr = getattr(target, case['member'])
        if 'args' not in case or (not case['args'] and not callable(attr)):
            return attr() if callable(attr) else attr
        ret = attr(*[self.decode(x) for x in case['args']])
        if case.get('mutates'):
            return {'self': self_obj, 'returns': ret}
        if 'then' in case:
            t = getattr(self_obj, case['then'])
            return {'returns': ret, 'then': t() if callable(t) else t}
        return ret


def encode(v):
    if isinstance(v, dict):
        return {k: encode(x) for k, x in v.items()}
    if isinstance(v, (tuple, list)):
        return [encode(x) for x in v]
    if hasattr(type(v), '__members__') and hasattr(v, 'name'):     # pybind11 enum
        return {'enum': v.name}
    if isinstance(v, (bool, int, str)) or v is None:
        return v
    if isinstance(v, float):
        return 'NaN' if math.isnan(v) else 'Inf' if v == math.inf else '-Inf' if v == -math.inf else v
    name = type(v).__name__
    base = name
    if base in ('Point3d', 'Vector3d', 'Point3f', 'Vector3f'):
        return {base: [encode(v.X), encode(v.Y), encode(v.Z)]}
    if base in ('Point2d', 'Point2f'):
        return {base: [encode(v.X), encode(v.Y)]}
    if base == 'Line':
        return {base: [encode(c) for p in (v.From, v.To) for c in (p.X, p.Y, p.Z)]}
    if base == 'BoundingBox':
        return {base: [encode(c) for p in (v.Min, v.Max) for c in (p.X, p.Y, p.Z)]}
    if base == 'Interval':
        return {base: [encode(v.T0), encode(v.T1)]}
    if base == 'Plane':
        o, x, y = v.Origin, v.XAxis, v.YAxis
        return {base: [encode(c) for c in (o.X, o.Y, o.Z, x.X, x.Y, x.Z, y.X, y.Y, y.Z)]}
    if base == 'Arc':                    # plane, radius, angle domain (G113)
        pl, d = v.Plane, v.AngleDomain
        o, x, y = pl.Origin, pl.XAxis, pl.YAxis
        return {base: [encode(c) for c in (o.X, o.Y, o.Z, x.X, x.Y, x.Z, y.X, y.Y, y.Z, v.Radius, d.T0, d.T1)]}
    if base == 'Transform':
        return {base: [encode(getattr(v, 'M%d%d' % (i // 4, i % 4))) for i in range(16)]}
    return {'class': base}      # a geometry/class instance: compare its (most-derived) type


def run_case(surface, case):
    try:
        return encode(surface.evaluate(case))
    except IndexError:
        return {'throws': 'out_of_range'}
    except ValueError:
        return {'throws': 'invalid_argument'}
    except (AttributeError, TypeError) as e:
        return {'missing': str(e).split('\n')[0][:80]}


def same(a, b):
    """bitwise for doubles; 5 == 5.0 across int/float (JSON loses the type)"""
    # python Color is an (r, g, b, a) tuple; the golden says {"Color": [...]}
    if isinstance(a, dict) and list(a) == ['Color'] and isinstance(b, list) and len(b) == 4:
        return same(a['Color'], b)
    if isinstance(b, dict) and list(b) == ['Color'] and isinstance(a, list) and len(a) == 4:
        return same(a, b['Color'])
    if isinstance(a, dict) and isinstance(b, dict):
        return a.keys() == b.keys() and all(same(a[k], b[k]) for k in a)
    if isinstance(a, list) and isinstance(b, list):
        return len(a) == len(b) and all(same(x, y) for x, y in zip(a, b))
    if isinstance(a, bool) or isinstance(b, bool):
        return a is b
    return a == b


pending = []


def run(module_dir):
    r = load_module(module_dir)
    gen = Surface(r)
    failures, rows, gaps = [], [], []
    global pending
    pending = []
    for fname in sorted(os.listdir(os.path.join(HERE, 'cases'))):
        doc = json.load(open(os.path.join(HERE, 'cases', fname)))
        cases = doc['cases']
        gen.recipes = doc.get('recipes', {})
        golden = json.load(open(os.path.join(HERE, 'golden', fname)))
        gen.fixtures = golden.get('__fixtures__', {})
        for c in cases:
            if c['id'] not in golden:
                pending.append(c['id'])       # a case the oracle has not run yet
                continue
            want = golden[c['id']]
            g = run_case(gen, c)
            g_ok = same(g, want)
            if not g_ok and isinstance(g, dict) and 'missing' in g:
                gaps.append((c['id'], g['missing']))
                g_ok = True
            if not g_ok:
                failures.append((c['id'], want, g))
            rows.append((c['id'], g_ok))
    return rows, failures, gaps


def main(argv):
    rows, failures, gaps = run(argv[1])
    if pending:
        print('  pending (no golden yet; run the oracle): %s' % ', '.join(pending))
    n = len(rows)
    print('parity vs .NET: %d/%d' % (n - len(failures), n))
    for cid, why in gaps:
        print('  generated gap (member/overload not generated) %s: %s' % (cid, why[:70]))
    for cid, want, got in failures:
        print('  GENERATED MISMATCH %s: want %s got %s' % (cid, want, got))
    return 1 if failures else 0


def test_generated_matches_dotnet():
    rows, failures, _ = run(os.environ['RH3DM_MODULE_DIR'])
    assert not failures, failures


if __name__ == '__main__':
    sys.exit(main(sys.argv))
