"""Record a perf run: one markdown page per run in history/ (with the numbers beside it as JSON), and the
index history/README.md rebuilt from all of them.

usage: python record.py <results dir> [--env E] [--commit SHA] [--date YYYY-MM-DD] [--notes TEXT] ...
       python record.py --reindex        (rebuild history/README.md, e.g. after copying in a CI record)

<results dir> is what compare.py reads (<lib>_{ref,cur}[_rN].json, each task keeping its lowest median),
plus an optional run.json that round.sh perf and the CI workflow write: env, date, commit, branch, scale,
rounds, repeats, builds, machine. Command-line options override run.json. Libraries are whatever the
directory holds, so a one-off like javascript-wasm64_ref.json gets its own table.

Factor = reference / current: "3.0x faster" means this tree takes a third of the time. Tasks under
--floor-ms on either side are marked, since timer and GC noise dominate them."""
import argparse, glob, json, math, os, re, sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
from tasks import TASKS  # noqa: E402

ORDER = ['python', 'dotnet', 'javascript', 'javascript-wasm64']
TITLES = {'python': 'Python', 'dotnet': '.NET', 'javascript': 'JavaScript (wasm32)',
          'javascript-wasm64': 'JavaScript (wasm64 / memory64)'}
SAME = 1.05  # within 5% either way: "same"


def load(d, lib, side):
    best, n, meta = {}, 0, {}
    for f in sorted(glob.glob(os.path.join(d, '%s_%s*.json' % (lib, side)))):
        if not re.match(r'^%s_%s(_r\d+)?\.json$' % (re.escape(lib), side), os.path.basename(f)):
            continue
        data = json.load(open(f))
        n += 1
        meta = meta or {k: v for k, v in data.items() if k != 'results'}
        for task, v in data['results'].items():
            if 'median_ms' in v:
                best[task] = min(best.get(task, float('inf')), v['median_ms'])
    return best, n, meta


def libs_in(d):
    found = {re.match(r'^(.+)_(ref|cur)(_r\d+)?\.json$', os.path.basename(f)).group(1)
             for f in glob.glob(os.path.join(d, '*_*.json')) if re.match(r'^.+_(ref|cur)(_r\d+)?\.json$', os.path.basename(f))}
    return sorted(found, key=lambda l: (ORDER.index(l) if l in ORDER else len(ORDER), l))


def release(v):
    """9.2.0b0 -> 9.2.0-beta.0, 8.35.0 -> 8.35.0"""
    m = re.match(r'^(\d+\.\d+\.\d+)(a|b|rc)(\d+)$', v or '')
    return '%s-%s.%s' % (m.group(1), {'a': 'alpha', 'b': 'beta', 'rc': 'rc'}[m.group(2)], m.group(3)) if m else v


def ms(x):
    if x is None:
        return '-'
    return '{:,.0f}'.format(x) if x >= 100 else '%.1f' % x if x >= 1 else '%.2f' % x


def factor(x, y):
    if not x or not y:
        return '-'
    f = x / y
    if 1 / SAME < f < SAME:
        return 'same (%.2fx)' % f
    return '%.2fx faster' % f if f > 1 else '**%.2fx slower**' % (1 / f)


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument('results', nargs='?')
    ap.add_argument('--reindex', action='store_true')
    for k in ('env', 'date', 'commit', 'branch', 'scale', 'rounds', 'repeats', 'builds', 'machine',
              'ref_label', 'cur_label', 'notes', 'tag'):
        ap.add_argument('--' + k.replace('_', '-'))
    ap.add_argument('--floor-ms', type=float, default=10.0)
    ap.add_argument('--out', default=os.path.join(HERE, 'history'))
    a = ap.parse_args()
    if a.reindex:
        return index(a.out)
    if not a.results:
        ap.error('a results directory is required')

    run_path = os.path.join(a.results, 'run.json')
    run = json.load(open(run_path)) if os.path.exists(run_path) else {}
    for k, v in vars(a).items():
        if v is not None and k not in ('results', 'out', 'floor_ms'):
            run[k] = v
    for k in ('env', 'date', 'commit'):
        if not run.get(k):
            sys.exit('record.py: no %s (pass --%s or write run.json)' % (k, k))

    libs, tables, versions = libs_in(a.results), {}, {}
    for lib in libs:
        ref, nref, mref = load(a.results, lib, 'ref')
        cur, ncur, mcur = load(a.results, lib, 'cur')
        versions[lib] = {'ref': mref.get('version'), 'cur': mcur.get('version'),
                         'runtime': mcur.get('runtime') or mref.get('runtime'), 'rounds': max(nref, ncur)}
        tables[lib] = [{'task': t, 'ref_ms': ref.get(t), 'cur_ms': cur.get(t)}
                       for t in dict.fromkeys(list(ref) + list(cur))]
    if not tables:
        sys.exit('record.py: no <lib>_{ref,cur}*.json in ' + a.results)
    pyv = versions.get('python', {})
    ref_label = run.get('ref_label') or release(pyv.get('ref')) or 'reference'
    cur_label = run.get('cur_label') or release(pyv.get('cur')) or 'current'
    run.update({'ref_label': ref_label, 'cur_label': cur_label, 'versions': versions, 'floor_ms': a.floor_ms})

    sha = run['commit'][:8]
    name = '%s_%s_%s%s' % (run['date'][:10], sha, run['env'], '_' + run['tag'] if run.get('tag') else '')
    L = []
    out = L.append
    out('# Perf run %s: %s vs %s\n' % (run['date'][:10], cur_label, ref_label))
    out('| | |\n|---|---|')
    out('| Date | %s |' % run['date'])
    out('| Commit | `%s`%s |' % (run['commit'], ' (%s)' % run['branch'] if run.get('branch') else ''))
    out('| Environment | `%s`%s |' % (run['env'], ': ' + run['machine'] if run.get('machine') else ''))
    if run.get('builds'):
        out('| Builds | %s |' % run['builds'])
    out('| Data | model scale `%s` (tests/perf/gen_model.py, written by %s) + tests/models |' % (run.get('scale', '?'), ref_label))
    out('| Method | one warm-up, median of %s repeats (2 when the warm-up took over 1 s); %s round(s), reference and current alternating; each task keeps its lowest median |'
        % (run.get('repeats', '?'), run.get('rounds', '?')))
    out('| Factor | %s time / %s time; "same" is within 5%%; † = under %g ms on a side (noisy) |' % (ref_label, cur_label, a.floor_ms))
    if run.get('notes'):
        out('| Notes | %s |' % run['notes'])
    for lib in libs:
        v, rows = versions[lib], tables[lib]
        both = [r for r in rows if r['ref_ms'] and r['cur_ms']]
        geo = math.exp(sum(math.log(r['ref_ms'] / r['cur_ms']) for r in both) / len(both)) if both else None
        faster = sum(r['ref_ms'] / r['cur_ms'] >= SAME for r in both)
        slower = sum(r['ref_ms'] / r['cur_ms'] <= 1 / SAME for r in both)
        out('\n## %s\n' % TITLES.get(lib, lib))
        out('%s is `%s`, %s is `%s`; %s. %d tasks: %d faster, %d slower, %d the same; geometric mean %s.\n'
            % (ref_label, v['ref'], cur_label, v['cur'], v['runtime'] or '?', len(both), faster, slower,
               len(both) - faster - slower, factor(geo, 1) if geo else '-'))
        out('| Task | Parameters | %s (ms) | %s (ms) | Factor |' % (ref_label, cur_label))
        out('|---|---|--:|--:|---|')
        for r in rows:
            x, y = r['ref_ms'], r['cur_ms']
            noisy = ' †' if x and y and min(x, y) < a.floor_ms else ''
            out('| `%s` | %s | %s | %s | %s%s |' % (r['task'], TASKS.get(r['task'], ''), ms(x), ms(y), factor(x, y), noisy))
    os.makedirs(a.out, exist_ok=True)
    open(os.path.join(a.out, name + '.md'), 'w').write('\n'.join(L) + '\n')
    json.dump({'run': run, 'results': tables}, open(os.path.join(a.out, name + '.json'), 'w'), indent=1)
    index(a.out)
    print('recorded ' + os.path.join(a.out, name + '.md'))


def index(d):
    """history/README.md: one row per run, newest first, with each library's geometric-mean factor"""
    runs = []
    for f in glob.glob(os.path.join(d, '*.json')):
        data = json.load(open(f))
        run, geo = data['run'], {}
        for lib, rows in data['results'].items():
            both = [r for r in rows if r['ref_ms'] and r['cur_ms']]
            if both:
                geo[lib] = math.exp(sum(math.log(r['ref_ms'] / r['cur_ms']) for r in both) / len(both))
        runs.append((run['date'], os.path.basename(f)[:-5], run, geo))
    runs.sort(key=lambda r: (r[0], r[1]), reverse=True)
    libs = sorted({l for r in runs for l in r[3]}, key=lambda l: (ORDER.index(l) if l in ORDER else len(ORDER), l))
    L = ['# Perf run history\n',
         'One page per recorded run of tests/perf (`python tests/perf/record.py`, or `round.sh perf record`).',
         'Columns per library: the geometric mean over its tasks of reference time / current time (above 1:',
         'the current build is faster). Runs differ in environment, builds and model scale, so compare',
         'rows of the same environment.\n',
         '| Date | Commit | Env | Versions | Scale | %s | Notes |' % ' | '.join(TITLES.get(l, l) for l in libs),
         '|---|---|---|---|---|%s---|' % ('--:|' * len(libs))]
    for date, name, run, geo in runs:
        L.append('| [%s](%s.md) | `%s` | %s | %s vs %s | %s | %s | %s |' % (
            date[:16], name, run['commit'][:8], run['env'], run['cur_label'], run['ref_label'], run.get('scale', '?'),
            ' | '.join('%.2f' % geo[l] if l in geo else '' for l in libs), run.get('notes', '').split('. ')[0]))
    os.makedirs(d, exist_ok=True)
    open(os.path.join(d, 'README.md'), 'w').write('\n'.join(L) + '\n')


if __name__ == '__main__':
    main()
