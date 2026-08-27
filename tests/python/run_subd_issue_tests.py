"""Run the SubD binding tests and print one verdict per YouTrack issue.

The five issues are in Needs Testing, and what a tester wants out of a run is a
line per issue, not a wall of dots.

    python run_subd_issue_tests.py          one line per issue
    python run_subd_issue_tests.py -v       plus every failure in full

Exit status is 0 only if every issue passed. Issues whose tests were skipped -
which means the fixture has not been authored yet - count as neither.
"""

import io
import os
import sys
import unittest

_HERE = os.path.dirname(os.path.abspath(__file__))
if _HERE not in sys.path:
    sys.path.insert(0, _HERE)

#: module -> (issue, one-line summary). Order is the order they are reported in.
ISSUES = (
    ("test_SubD_FixtureSpec", None,
     "fixture expectations are self-consistent"),
    ("test_SubD_RH3DM178_SubD", "RH3DM-178",
     "Wrap full SubD API"),
    ("test_SubD_RH3DM175_Face", "RH3DM-175",
     "Wrap SubD Face API"),
    ("test_SubD_RH3DM176_Edge", "RH3DM-176",
     "Wrap SubD Edge API"),
    ("test_SubD_RH3DM177_Vertex", "RH3DM-177",
     "Wrap SubD Vertex API"),
    ("test_SubD_RH3DM169_Creases", "RH3DM-169",
     "Adidas needs SubD Crease in rhino3dm"),
)


def run(module_name, verbose):
    suite = unittest.defaultTestLoader.loadTestsFromName(module_name)
    stream = io.StringIO()
    result = unittest.TextTestRunner(stream=stream, verbosity=0).run(suite)
    if verbose and (result.failures or result.errors):
        print(stream.getvalue())
    return result


def verdict(result):
    total = result.testsRun
    bad = len(result.failures) + len(result.errors)
    skipped = len(result.skipped)
    # A module that would not import shows up as a single synthetic error; call
    # that out rather than reporting it as a failed assertion.
    if len(result.errors) == 1 and "No module named 'rhino3dm'" in result.errors[0][1]:
        return "n/a ", "no rhino3dm build"
    if total and skipped == total:
        return "SKIP", "fixture not authored"
    if bad:
        return "FAIL", "%d of %d failed" % (bad, total)
    return "PASS", "%d checks" % (total - skipped)


def main():
    verbose = "-v" in sys.argv or "--verbose" in sys.argv

    print("rhino3dm SubD bindings - per-issue verdict")
    print("=" * 68)

    try:
        import rhino3dm  # noqa: F401
    except ImportError:
        print("  rhino3dm is not importable, so only the expectations can be")
        print("  checked. Build or pip install rhino3dm and re-run.")
        print("")

    statuses = []
    for module_name, issue, summary in ISSUES:
        result = run(module_name, verbose)
        status, detail = verdict(result)
        statuses.append(status)
        print("  %-4s  %-11s %-40s %s"
              % (status, issue or "spec", summary, detail))

    print("=" * 68)
    if "n/a " in statuses:
        print("No rhino3dm to test against - only the expectations were checked.")
        return 1
    if "FAIL" in statuses:
        print("Something failed. If tests/models/authoring/report_subd_fixture.py")
        print("is clean, the failure is in the binding, not the expectation.")
        return 1
    if "SKIP" in statuses:
        print("Skipped: run tests/models/authoring/make_subd_fixture.py inside")
        print("Rhino to author tests/models/subd_creases.3dm, then re-run.")
        return 0
    print("All five issues verified against the authored fixture.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
