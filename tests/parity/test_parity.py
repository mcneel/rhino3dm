"""Python parity runner: the generated bindings must reproduce the .NET
goldens exactly; the old (hand) bindings run as a temporary, informational
column whose differences must be listed in known_old_divergences.json.

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
UNSET = -1.23432101234321e+308             # RhinoMath.UnsetValue


def load_module(module_dir):
    sys.path.insert(0, module_dir)
    import _rhino3dm
    return _rhino3dm


class Surface:
    """One binding surface: how type names map to classes."""

    def __init__(self, r, suffix):
        self.r, self.suffix = r, suffix

    def cls(self, name):
        return getattr(self.r, name + self.suffix)

    def decode(self, v):
        if isinstance(v, str):
            return {'NaN': math.nan, 'Inf': math.inf, '-Inf': -math.inf}[v]
        if isinstance(v, dict):
            (k, x), = v.items()
            if k == 'static':
                tn, mn = x.rsplit('.', 1)
                if tn == 'RhinoMath' and mn == 'UnsetValue':
                    return UNSET
                return getattr(self.cls(tn), mn)
            return self.cls(k)(*[float(self.decode(a)) for a in x])
        return v

    def make_self(self, case):
        s = case.get('self')
        if s is None:
            return None
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
        if 'index' in case:
            return self_obj[case['index']]
        target = self_obj if self_obj is not None else self.cls(case['type'])
        attr = getattr(target, case['member'])
        if 'args' not in case:
            return attr() if callable(attr) else attr
        ret = attr(*[self.decode(x) for x in case['args']])
        if case.get('mutates'):
            return {'self': self_obj, 'returns': ret}
        return ret


def encode(v):
    if isinstance(v, dict):
        return {k: encode(x) for k, x in v.items()}
    if isinstance(v, (bool, int)) or v is None:
        return v
    if isinstance(v, float):
        return 'NaN' if math.isnan(v) else 'Inf' if v == math.inf else '-Inf' if v == -math.inf else v
    name = type(v).__name__
    base = name[:-1] if name.endswith('X') else name
    if base in ('Point3d', 'Vector3d'):
        return {base: [encode(v.X), encode(v.Y), encode(v.Z)]}
    if base == 'Interval':
        return {base: [encode(v.T0), encode(v.T1)]}
    raise TypeError('cannot encode %r' % (v,))


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
    if isinstance(a, dict) and isinstance(b, dict):
        return a.keys() == b.keys() and all(same(a[k], b[k]) for k in a)
    if isinstance(a, list) and isinstance(b, list):
        return len(a) == len(b) and all(same(x, y) for x, y in zip(a, b))
    if isinstance(a, bool) or isinstance(b, bool):
        return a is b
    return a == b


def run(module_dir):
    r = load_module(module_dir)
    gen, old = Surface(r, 'X'), Surface(r, '')
    known = json.load(open(os.path.join(HERE, 'known_old_divergences.json')))
    failures, unknown_old, rows = [], [], []
    for fname in sorted(os.listdir(os.path.join(HERE, 'cases'))):
        cases = json.load(open(os.path.join(HERE, 'cases', fname)))['cases']
        golden = json.load(open(os.path.join(HERE, 'golden', fname)))
        for c in cases:
            want = golden[c['id']]
            g = run_case(gen, c)
            o = run_case(old, c)
            g_ok = same(g, want)
            o_state = 'ok' if same(o, want) else ('missing' if isinstance(o, dict) and 'missing' in o else 'differs')
            if not g_ok:
                failures.append((c['id'], want, g))
            # a MISSING member is coverage (reported as a count); only a
            # member that exists and computes something else needs a reason
            if o_state == 'differs' and c['id'] not in known:
                unknown_old.append((c['id'], o_state, want, o))
            if o_state == 'ok' and c['id'] in known:
                unknown_old.append((c['id'], 'stale known-divergence entry (now matches)', want, o))
            rows.append((c['id'], g_ok, o_state))
    return rows, failures, unknown_old


def main(argv):
    rows, failures, unknown_old = run(argv[1])
    n = len(rows)
    old_ok = sum(1 for _, _, o in rows if o == 'ok')
    old_missing = sum(1 for _, _, o in rows if o == 'missing')
    print('parity vs .NET: generated %d/%d | old bindings %d/%d match, %d differ, %d missing'
          % (n - len(failures), n, old_ok, n, n - old_ok - old_missing, old_missing))
    for cid, want, got in failures:
        print('  GENERATED MISMATCH %s: want %s got %s' % (cid, want, got))
    for cid, state, want, got in unknown_old:
        print('  old binding %s (unlisted) %s: want %s got %s' % (state, cid, want, got))
    return 1 if failures else 0


def test_generated_matches_dotnet():
    rows, failures, _ = run(os.environ['RH3DM_MODULE_DIR'])
    assert not failures, failures


if __name__ == '__main__':
    sys.exit(main(sys.argv))
