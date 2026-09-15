"""rhino3dm-side plumbing for the SubD fixture used by the RH3DM-1xx tests.

Loads tests/models/subd_creases.3dm and indexes its components by the vertex
names in subd_fixture_spec, so the tests can say "the edge between D and H"
instead of guessing a component id. Component ids are an implementation detail
of however the SubD was built; the control net is not.
"""

import os
import sys
import unittest

import rhino3dm

# The spec sits next to this file; make it importable however the tests were
# started (from tests/python, from the repo root, via discover, ...).
_HERE = os.path.dirname(os.path.abspath(__file__))
if _HERE not in sys.path:
    sys.path.insert(0, _HERE)

import subd_fixture_spec as spec  # noqa: E402


# ---------------------------------------------------------------------------
# Loading
# ---------------------------------------------------------------------------

def fixture_path():
    """Absolute path to the fixture .3dm, or None if it has not been authored.

    Searched relative to this file and to the working directory, so the tests
    run the same from tests/python, from tests, and from the repository root.
    """
    here = os.path.dirname(os.path.abspath(__file__))
    candidates = (
        os.path.join(here, "..", "models", spec.FIXTURE_NAME),
        os.path.join("models", spec.FIXTURE_NAME),
        os.path.join("tests", "models", spec.FIXTURE_NAME),
        os.path.join("..", "models", spec.FIXTURE_NAME),
    )
    for path in candidates:
        if os.path.exists(path):
            return os.path.abspath(path)
    return None


MISSING_FIXTURE = (
    "%s has not been authored yet - run tests/models/authoring/"
    "make_subd_fixture.py inside Rhino to create it." % spec.FIXTURE_NAME
)


def read_subd():
    """The fixture's SubD, or None if the fixture is absent."""
    path = fixture_path()
    if path is None:
        return None
    model = rhino3dm.File3dm.Read(path)
    for obj in model.Objects:
        geometry = obj.Geometry
        if isinstance(geometry, rhino3dm.SubD):
            return geometry
    return None


# ---------------------------------------------------------------------------
# Naming components by their control net position
# ---------------------------------------------------------------------------

def _xyz(p):
    return (p.X, p.Y, p.Z)


def vertex_name(vertex):
    """Spec name ("A".."H") of a SubDVertex, from its control net point."""
    return spec.vertex_name_at(_xyz(vertex.ControlNetPoint))


def edge_name(edge):
    """Canonical spec key of a SubDEdge, e.g. "AD", or None if it is not ours."""
    ends = (vertex_name(edge.Vertex(0)), vertex_name(edge.Vertex(1)))
    if None in ends:
        return None
    return spec.edge_key(*ends)


def face_key(face):
    """Unordered set of spec vertex names bounding a SubDFace."""
    return frozenset(vertex_name(face.Vertex(i))
                     for i in range(face.VertexCount))



def face_name(face):
    """Spec name ("bottom", "front", ...) of a SubDFace."""
    return spec.FACES_BY_KEY.get(face_key(face))


def vertices_by_name(subd):
    return {vertex_name(v): v for v in subd.Vertices}


def edges_by_name(subd):
    return {edge_name(e): e for e in subd.Edges}


def faces_by_name(subd):
    return {face_name(f): f for f in subd.Faces}


# ---------------------------------------------------------------------------
# Tag enums
# ---------------------------------------------------------------------------

EDGE_TAGS = {
    "Unset": rhino3dm.SubDEdgeTag.Unset,
    "Smooth": rhino3dm.SubDEdgeTag.Smooth,
    "Crease": rhino3dm.SubDEdgeTag.Crease,
    "SmoothX": rhino3dm.SubDEdgeTag.SmoothX,
}

VERTEX_TAGS = {
    "Unset": rhino3dm.SubDVertexTag.Unset,
    "Smooth": rhino3dm.SubDVertexTag.Smooth,
    "Crease": rhino3dm.SubDVertexTag.Crease,
    "Corner": rhino3dm.SubDVertexTag.Corner,
    "Dart": rhino3dm.SubDVertexTag.Dart,
}


# ---------------------------------------------------------------------------
# Base test case
# ---------------------------------------------------------------------------

class SubDFixtureTestCase(unittest.TestCase):
    """Loads the fixture once per test and adds point/vector assertions."""

    def setUp(self):
        if fixture_path() is None:
            self.skipTest(MISSING_FIXTURE)
        self.subd = read_subd()
        self.assertIsNotNone(self.subd, "fixture contains no SubD object")

        # A None key means a component sits somewhere the spec does not
        # describe, which would silently weaken every lookup below. Check the
        # vertices first: the edge and face names are built out of them.
        self.vertices = vertices_by_name(self.subd)
        self.assertNotIn(None, self.vertices,
                         "a vertex is off the authored control net - is this "
                         "fixture the one make_subd_fixture.py writes?")
        self.assertEqual(set(self.vertices), set(spec.VERTEX_POINTS))

        self.edges = edges_by_name(self.subd)
        self.faces = faces_by_name(self.subd)
        self.assertNotIn(None, self.edges, "an edge is not in the authored control net")
        self.assertNotIn(None, self.faces, "a face does not match an authored loop")

    def edge(self, name):
        return self.edges[spec.canon(name)]

    def assertPointEqual(self, actual, expected, msg=None, tol=spec.VALUE_TOL):
        got = _xyz(actual)
        for axis, a, e in zip("XYZ", got, expected):
            self.assertAlmostEqual(
                a, e, delta=tol,
                msg="%s: %s expected %s, got %s" % (msg or "point", axis, expected, got))

    def assertUnitDirection(self, actual, expected, msg=None, tol=1e-9):
        """Assert a vector points along ``expected``, ignoring its length."""
        got = _xyz(actual)
        length = sum(c * c for c in got) ** 0.5
        self.assertGreater(length, tol, "%s: zero-length vector" % (msg or "vector"))
        unit = tuple(c / length for c in got)
        for axis, a, e in zip("XYZ", unit, expected):
            self.assertAlmostEqual(
                a, e, delta=1e-9,
                msg="%s: %s expected %s, got %s" % (msg or "vector", axis, expected, unit))
