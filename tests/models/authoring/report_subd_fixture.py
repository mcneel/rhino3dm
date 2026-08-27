"""Check the fixture against the values the tests hard-code - RUN INSIDE RHINO.

tests/python/subd_fixture_spec.py holds every value the RH3DM-169/175/176/177/178
tests assert. Some of those values are arithmetic and some are read off the
ON_SubD documentation, and the second kind deserves to be checked against Rhino
before anyone trusts a red test.

This reads tests/models/subd_creases.3dm back through RhinoCommon - a completely
separate code path from the rhino3dm bindings under test - and prints expected
against actual, line by line.

    1. run make_subd_fixture.py first, to author the .3dm
    2. run this
    3. read the summary at the bottom

How to read a mismatch:

    the spec is wrong        Rhino is the authority. Fix the value in
                             subd_fixture_spec.py and note it as validated.
    the fixture is wrong     Rhino did not apply what the authoring script
                             asked for. Fix make_subd_fixture.py and re-author.

Either way the fix belongs here, not in the rhino3dm tests: they only read the
spec. What this cannot check is whether the *bindings* forward these values
correctly - that is exactly what the rhino3dm tests are for, and they are only
meaningful once this report is clean.

Coverage note: RhinoCommon does not expose SubDEdge.IsHardCrease/IsDartCrease/
DartCount, SubDVertex.VertexSharpness, or the SubDFace sharpness aggregates.
Those rows are marked "derived": recomputed here from the documented rule using
values Rhino did give us. VertexSharpness is derived through ON's own
SubDEdgeSharpness.VertexSharpness helper, so it is still ON's answer, not ours.
"""

import os
import sys

import Rhino


#: Set only if __file__ is undefined where you are running this.
REPO_ROOT = ""


def repo_root():
    if REPO_ROOT:
        return REPO_ROOT
    try:
        here = os.path.dirname(os.path.abspath(__file__))
    except NameError:
        raise RuntimeError(
            "__file__ is not defined - set REPO_ROOT at the top of this script.")
    return os.path.dirname(os.path.dirname(os.path.dirname(here)))


def load_spec():
    path = os.path.join(repo_root(), "tests", "python")
    if path not in sys.path:
        sys.path.insert(0, path)
    import subd_fixture_spec
    return subd_fixture_spec


spec = load_spec()

EDGE_TAGS = {
    "Unset": Rhino.Geometry.SubDEdgeTag.Unset,
    "Smooth": Rhino.Geometry.SubDEdgeTag.Smooth,
    "Crease": Rhino.Geometry.SubDEdgeTag.Crease,
    "SmoothX": Rhino.Geometry.SubDEdgeTag.SmoothX,
}

VERTEX_TAGS = {
    "Unset": Rhino.Geometry.SubDVertexTag.Unset,
    "Smooth": Rhino.Geometry.SubDVertexTag.Smooth,
    "Crease": Rhino.Geometry.SubDVertexTag.Crease,
    "Corner": Rhino.Geometry.SubDVertexTag.Corner,
    "Dart": Rhino.Geometry.SubDVertexTag.Dart,
}


# ---------------------------------------------------------------------------
# Reporting
# ---------------------------------------------------------------------------

class Report(object):

    def __init__(self):
        self.checked = 0
        self.failed = []

    def section(self, title):
        print("")
        print(title)
        print("  " + "-" * (len(title) + 2))

    def check(self, label, actual, expected, tol=None, derived=False):
        self.checked += 1
        if tol is None:
            ok = actual == expected
        else:
            ok = abs(float(actual) - float(expected)) <= tol
        mark = "ok  " if ok else "FAIL"
        note = " (derived)" if derived else ""
        if ok:
            print("  %s  %-38s %s%s" % (mark, label, _fmt(actual), note))
        else:
            print("  %s  %-38s expected %s, got %s%s"
                  % (mark, label, _fmt(expected), _fmt(actual), note))
            self.failed.append(label)

    def check_point(self, label, actual, expected, tol=None, derived=False):
        tol = spec.VALUE_TOL if tol is None else tol
        got = (actual.X, actual.Y, actual.Z)
        ok = all(abs(a - e) <= tol for a, e in zip(got, expected))
        self.checked += 1
        if ok:
            print("  ok    %-38s %s%s" % (label, _fmt(got),
                                          " (derived)" if derived else ""))
        else:
            print("  FAIL  %-38s expected %s, got %s"
                  % (label, _fmt(expected), _fmt(got)))
            self.failed.append(label)

    def note(self, label, value):
        """Print something the spec does not pin down, for reference."""
        print("  --    %-38s %s" % (label, _fmt(value)))

    def summary(self):
        print("")
        print("=" * 72)
        if not self.failed:
            print("%d checks, all match. The values in subd_fixture_spec.py are good;"
                  % self.checked)
            print("a red rhino3dm test now means the binding, not the expectation.")
        else:
            print("%d checks, %d mismatch:" % (self.checked, len(self.failed)))
            for label in self.failed:
                print("    %s" % label)
            print("")
            print("Rhino is the authority. Fix subd_fixture_spec.py (or")
            print("make_subd_fixture.py, if the fixture itself is wrong) and re-run.")
        print("=" * 72)


def _fmt(value):
    if isinstance(value, tuple):
        if isinstance(value[0], float):
            return "(" + ", ".join("%.6g" % v for v in value) + ")"
        else:
            return "(" + ", ".join("%s" % v for v in value) + ")"
    if isinstance(value, float):
        return "%.6g" % value
    return str(value)


# ---------------------------------------------------------------------------
# Reading the fixture back
# ---------------------------------------------------------------------------

def read_fixture():
    path = os.path.join(repo_root(), "tests", "models", spec.FIXTURE_NAME)
    if not os.path.exists(path):
        raise RuntimeError("%s does not exist - run make_subd_fixture.py first" % path)
    model = Rhino.FileIO.File3dm.Read(path)
    for obj in model.Objects:
        geometry = obj.Geometry
        if isinstance(geometry, Rhino.Geometry.SubD):
            return path, geometry
    raise RuntimeError("%s contains no SubD" % path)


def vertex_name(vertex):
    p = vertex.ControlNetPoint
    return spec.vertex_name_at((p.X, p.Y, p.Z))


def edge_ends(edge):
    return (vertex_name(edge.VertexFrom), vertex_name(edge.VertexTo))


def edge_name(edge):
    a, b = edge_ends(edge)
    return spec.edge_key(a, b)


def face_name(face):
    names = frozenset(vertex_name(face.VertexAt(i)) for i in range(face.VertexCount))
    return spec.FACES_BY_KEY.get(names)


def end_sharpness_at(edge, name):
    """Sharpness of an edge at the end sitting on the named vertex."""
    at_0, at_1 = edge_ends(edge)
    if name == at_0:
        return edge.EndSharpness(0, False)
    if name == at_1:
        return edge.EndSharpness(1, False)
    raise RuntimeError("%s has no end at %s" % (edge_name(edge), name))


# ---------------------------------------------------------------------------
# The checks
# ---------------------------------------------------------------------------

def check_subd(report, subd):
    report.section("SubD")
    report.check("face count", subd.Faces.Count, spec.SUBD["face_count"])
    report.check("edge count", subd.Edges.Count, spec.SUBD["edge_count"])
    report.check("vertex count", subd.Vertices.Count, spec.SUBD["vertex_count"])
    report.check("is solid", subd.IsSolid, spec.SUBD["is_solid"])

    creases = sum(1 for e in subd.Edges if e.Tag == EDGE_TAGS["Crease"])
    report.check("crease edge count", creases, spec.SUBD["crease_edge_count"])
    report.check("sharp edge count", int(subd.SharpEdgeCount()),
                 spec.SUBD["sharp_edge_count"])
    smooth = sum(1 for e in subd.Edges
                 if e.Tag != EDGE_TAGS["Crease"] and not e.IsSharp)
    report.check("smooth, not sharp, edge count", smooth,
                 spec.SUBD["smooth_not_sharp_edge_count"])


def check_edges(report, subd):
    report.section("Edges")
    edges = {edge_name(e): e for e in subd.Edges}
    missing = set(spec.EDGES_BY_KEY) - set(edges)
    if missing:
        report.check("edge set", sorted(edges), sorted(spec.EDGES_BY_KEY))
        return

    for name in sorted(spec.EDGES_BY_KEY):
        expected = spec.EDGES_BY_KEY[name]
        edge = edges[name]
        report.check("%s tag" % name, edge.Tag, EDGE_TAGS[expected["tag"]])
        report.check("%s is sharp" % name, edge.IsSharp, expected["is_sharp"])
        for at, value in sorted(expected["sharpness"].items()):
            report.check("%s sharpness at %s" % (name, at),
                         end_sharpness_at(edge, at), value, tol=spec.VALUE_TOL)

        # Derived: RhinoCommon has no IsHardCrease/IsDartCrease/DartCount, so
        # these follow the rule in opennurbs_subd.cpp:4639-4670 using the tags
        # Rhino did report.
        is_crease = edge.Tag == EDGE_TAGS["Crease"]
        ends = [edge.VertexFrom, edge.VertexTo]
        darts = sum(1 for v in ends if v.Tag == VERTEX_TAGS["Dart"])
        crease_or_corner = all(v.Tag in (VERTEX_TAGS["Crease"], VERTEX_TAGS["Corner"])
                               for v in ends)
        report.check("%s is hard crease" % name, is_crease and crease_or_corner,
                     expected["is_hard_crease"], derived=True)
        report.check("%s is dart crease" % name, is_crease and darts > 0,
                     expected["is_dart_crease"], derived=True)
        report.check("%s dart count" % name, darts, expected["dart_count"],
                     derived=True)

        if expected["subdivision_point"] is None:
            # Not hard-coded; printed so it can be if you ever want it pinned.
            report.note("%s subdivision point" % name, "(smooth, not asserted)")


def check_vertices(report, subd):
    report.section("Vertices")
    vertices = {vertex_name(v): v for v in subd.Vertices}
    for name in sorted(spec.VERTICES):
        expected = spec.VERTICES[name]
        vertex = vertices[name]
        report.check_point("%s control net point" % name, vertex.ControlNetPoint,
                           spec.VERTEX_POINTS[name])
        report.check("%s tag" % name, vertex.Tag, VERTEX_TAGS[expected["tag"]])
        report.check("%s edge count" % name, vertex.EdgeCount, spec.VERTEX_EDGE_COUNT)
        report.check("%s face count" % name, vertex.FaceCount, spec.VERTEX_FACE_COUNT)

        creases = sum(1 for e in vertex.Edges if e.Tag == EDGE_TAGS["Crease"])
        report.check("%s crease edge count" % name, creases,
                     expected["crease_edge_count"])

        # Derived, but through ON's own helper: this is the rule that decides
        # whether a smooth vertex touching one sharp edge counts as sharp.
        sharp_ends = [end_sharpness_at(e, name) for e in vertex.Edges if e.IsSharp]
        nonzero = [s for s in sharp_ends if s > 0.0]
        sharpness = Rhino.Geometry.SubDEdgeSharpness.VertexSharpness(
            vertex.Tag, 0.0, len(nonzero), max(nonzero) if nonzero else 0.0)
        report.check("%s vertex sharpness" % name, sharpness,
                     expected["vertex_sharpness"], tol=spec.VALUE_TOL, derived=True)

        touches_sharp = any(e.IsSharp for e in vertex.Edges)
        can_be_sharp = vertex.Tag in (VERTEX_TAGS["Smooth"], VERTEX_TAGS["Crease"],
                                      VERTEX_TAGS["Dart"])
        report.check("%s is sharp" % name, touches_sharp and can_be_sharp,
                     expected["is_sharp"], derived=True)

        if expected["surface_point"] is not None:
            report.check_point("%s surface point" % name, vertex.SurfacePoint(),
                               expected["surface_point"])
        else:
            p = vertex.SurfacePoint()
            report.note("%s surface point" % name, (p.X, p.Y, p.Z))


def check_faces(report, subd):
    report.section("Faces")
    faces = {face_name(f): f for f in subd.Faces}
    if None in faces:
        report.check("face set", "a face is off the authored control net", "")
        return

    for name in sorted(spec.FACES):
        expected = spec.FACES[name]
        face = faces[name]
        report.check("%s edge count" % name, face.EdgeCount, spec.FACE_EDGE_COUNT)
        report.check("%s vertex count" % name, face.VertexCount,
                     spec.FACE_VERTEX_COUNT)
        report.check_point("%s control net centre" % name,
                           face.ControlNetCenterPoint, expected["centre"])

        # Normalised by hand rather than with Vector3d.Unitize, to keep clear of
        # how the host python binds a mutating method on a value type.
        normal = face.ControlNetCenterNormal
        length = (normal.X ** 2 + normal.Y ** 2 + normal.Z ** 2) ** 0.5
        report.check("%s control net centre normal" % name,
                     tuple(round(c / length, 9) for c in
                           (normal.X, normal.Y, normal.Z)),
                     tuple(float(c) for c in expected["normal"]))

        boundary = [face.EdgeAt(i) for i in range(face.EdgeCount)]
        report.check("%s boundary" % name,
                     tuple(sorted(edge_name(e) for e in boundary)),
                     tuple(sorted(spec.canon(e) for e in expected["edges"])))

        # Derived: SubDFace.SharpEdgeCount/MaximumEdgeSharpness are not on the
        # RhinoCommon face, so they are recomputed from the boundary edges.
        sharp = [e for e in boundary if e.IsSharp]
        report.check("%s has sharp edges" % name, len(sharp) > 0,
                     expected["has_sharp_edges"], derived=True)
        report.check("%s sharp edge count" % name, len(sharp),
                     expected["sharp_edge_count"], derived=True)
        ends = [e.EndSharpness(i, False) for e in sharp for i in (0, 1)]
        report.check("%s maximum edge sharpness" % name,
                     max(ends) if ends else 0.0, expected["max_edge_sharpness"],
                     tol=spec.VALUE_TOL, derived=True)


def main():
    path, subd = read_fixture()
    print("%s" % path)
    print("checking the values hard-coded in tests/python/subd_fixture_spec.py")

    report = Report()
    check_subd(report, subd)
    check_edges(report, subd)
    check_vertices(report, subd)
    check_faces(report, subd)
    report.summary()


if __name__ == "__main__":
    main()
