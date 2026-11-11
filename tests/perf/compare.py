"""Gate this tree's performance on its ratio to the reference release (8.35), task by task.

usage: python compare.py <results dir> --env <name> [--tolerance 1.5] [--floor-ms 10] [--update]
                         [--report report.md]

<results dir> holds <lib>_ref[_rN].json and <lib>_cur[_rN].json (lib: python, dotnet, javascript),
the output of bench.py / bench.mjs / dotnet/Program.cs. With several rounds, each task keeps its
lowest median. ratio = current / reference: below 1 the tree is faster.

Both versions run on the same machine in the same run, so the ratio cancels most of the machine's
speed; the baseline still belongs to one environment (baseline/<env>.json), since an x64 runner and
an arm64 container do not slow down the same tasks equally.

A task FAILS when its ratio exceeds the baseline ratio x tolerance. Tasks where either side's median
is below --floor-ms are reported but not gated (timer and GC noise: two runs of an unchanged tree
moved 5-50 ms JS tasks by up to 1.5x), and so are tasks only one side has.
--update writes the current ratios as the new baseline (do it on purpose, after a change is accepted).
Exit status: 1 on a regression or a task error, else 0."""
import argparse, datetime, glob, json, os, re, sys

HERE = os.path.dirname(os.path.abspath(__file__))
LIBS = ('python', 'dotnet', 'javascript')


def load(d, lib, side):
    """{task: lowest median ms}, [errors], meta of the first file"""
    best, errors, meta = {}, [], {}
    for f in sorted(glob.glob(os.path.join(d, '%s_%s*.json' % (lib, side)))):
        if not re.match(r'^%s_%s(_r\d+)?\.json$' % (lib, side), os.path.basename(f)):
            continue
        data = json.load(open(f))
        meta = meta or {k: v for k, v in data.items() if k != 'results'}
        for task, v in data['results'].items():
            if 'error' in v:
                errors.append('%s %s: %s' % (side, task, v['error']))
            elif 'median_ms' in v:
                best[task] = min(best.get(task, float('inf')), v['median_ms'])
    return best, errors, meta


def fmt(ms):
    if ms is None:
        return '-'
    return '%s' % ('{:,.0f}'.format(ms) if ms >= 100 else '%.1f' % ms if ms >= 1 else '%.2f' % ms)


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument('results')
    ap.add_argument('--env', required=True, help='baseline name, e.g. linux-arm64-docker or ci-ubuntu-x64')
    ap.add_argument('--tolerance', type=float, default=1.5)
    ap.add_argument('--floor-ms', type=float, default=10.0)
    ap.add_argument('--update', action='store_true')
    ap.add_argument('--report')
    a = ap.parse_args()

    base_path = os.path.join(HERE, 'baseline', a.env + '.json')
    base = json.load(open(base_path)) if os.path.exists(base_path) else None
    lines, failed, errors_all, ratios, metas = [], [], [], {}, {}
    out = lines.append
    out('# rhino3dm performance: this tree vs the reference release\n')
    out('Environment `%s`; tolerance %.2fx on the baseline ratio; tasks under %.1f ms not gated.%s\n'
        % (a.env, a.tolerance, a.floor_ms, '' if base else ' **No baseline yet: report only.**'))
    for lib in LIBS:
        ref, e1, mref = load(a.results, lib, 'ref')
        cur, e2, mcur = load(a.results, lib, 'cur')
        if not ref and not cur:
            continue
        metas[lib] = {'ref': mref, 'cur': mcur}
        errors_all += ['%s: %s' % (lib, e) for e in e1 + e2]
        b = (base or {}).get('ratios', {}).get(lib, {})
        out('\n## %s\n' % lib)
        out('Reference %s, current %s, %s\n' % (mref.get('version', '?'), mcur.get('version', '?'), mcur.get('runtime', '')))
        out('| Task | 8.35 (ms) | current (ms) | ratio | baseline | status |')
        out('|---|--:|--:|--:|--:|---|')
        ratios[lib] = {}
        for task in dict.fromkeys(list(ref) + list(cur)):
            x, y = ref.get(task), cur.get(task)
            r = y / x if x and y else None
            if r is not None:
                ratios[lib][task] = round(r, 4)
            br = b.get(task)
            if r is None:
                status = 'one side only'
            elif x < a.floor_ms or y < a.floor_ms:
                status = 'below floor'
            elif br is None:
                status = 'new (no baseline)' if base else ''
            elif r > br * a.tolerance:
                status = '**REGRESSION** (%.2fx the baseline)' % (r / br)
                failed.append('%s %s: ratio %.2f, baseline %.2f' % (lib, task, r, br))
            elif r < br / a.tolerance:
                status = 'improved (%.2fx the baseline): consider --update' % (r / br)
            else:
                status = 'ok'
            out('| %s | %s | %s | %s | %s | %s |' % (task, fmt(x), fmt(y), '-' if r is None else '%.2fx' % r,
                                                     '-' if br is None else '%.2fx' % br, status))
    if errors_all:
        out('\n## Task errors\n')
        out('\n'.join('- ' + e for e in errors_all))
    out('\n**%s**' % ('FAILED: %d regression(s), %d error(s)' % (len(failed), len(errors_all))
                      if failed or errors_all else 'PASSED' if base else 'REPORT ONLY (no baseline)'))
    text = '\n'.join(lines) + '\n'
    print(text)
    if a.report:
        open(a.report, 'w').write(text)
    if a.update:
        os.makedirs(os.path.dirname(base_path), exist_ok=True)
        json.dump({'env': a.env, 'created': datetime.date.today().isoformat(),
                   'versions': {lib: {s: m[s].get('version') for s in m} for lib, m in metas.items()},
                   'ratios': ratios}, open(base_path, 'w'), indent=1, sort_keys=True)
        print('baseline written: ' + base_path)
    return 1 if (failed or errors_all) and base and not a.update else (1 if errors_all else 0)


if __name__ == '__main__':
    sys.exit(main())
