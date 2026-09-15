"""Internal consistency of subd_fixture_spec.py.

The RH3DM-169/175/176/177/178 tests are only as good as the table they assert
against, and that table is written out by hand so that a mismatch points at the
binding rather than at a derivation. Written out by hand also means it can be
mistyped, so the relations that must hold between its entries are checked here.

This needs neither a rhino3dm build nor the authored .3dm, so it still runs when
everything else skips:

    python test_SubD_FixtureSpec.py -v

Two kinds of relation are checked. Arithmetic - face centres, midpoints,
outward normals, Catmull-Clark counts - is simply true or the table is wrong.
The rest encodes a documented ON_SubD rule: which vertex tag follows from a
crease count, when a sharp edge counts towards VertexSharpness. If Rhino ever
invalidates one of those rules (run tests/models/authoring/report_subd_fixture.py
to find out), the rule here and the value in the spec have to move together -
that is the point of stating it twice.
"""

import os
import sys
import unittest

_HERE = os.path.dirname(os.path.abspath(__file__))
if _HERE not in sys.path:
    sys.path.insert(0, _HERE)

import subd_fixture_spec as spec  # noqa: E402

CREASE = frozenset(spec.canon(e) for e in spec.CREASE_EDGES)
SHARP = frozenset(spec.canon(e) for e in spec.SHARP_EDGES)
SMOOTH = frozenset(spec.canon(e) for e in spec.SMOOTH_EDGES)

#: ON_SubDEdgeSharpness::MaximumValue.
MAX_SHARPNESS = 4.0


def _cross(u, w):
    return (u[1] * w[2] - u[2] * w[1],
            u[2] * w[0] - u[0] * w[2],
            u[0] * w[1] - u[1] * w[0])


def _midpoint(a, b):
    pa, pb = spec.VERTEX_POINTS[a], spec.VERTEX_POINTS[b]
    return tuple((pa[i] + pb[i]) / 2.0 for i in range(3))


def _touching(vertex_name):
    return [key for key in spec.EDGES_BY_KEY if vertex_name in key]


class TestEdgePartition(unittest.TestCase):

    def test_the_three_edge_groups_partition_the_box(self):
        self.assertEqual(CREASE | SHARP | SMOOTH, set(spec.EDGES_BY_KEY))
        self.assertEqual(len(CREASE) + len(SHARP) + len(SMOOTH),
                         len(spec.EDGES_BY_KEY))

    def test_group_sizes_match_the_subd_totals(self):
        self.assertEqual(len(CREASE), spec.SUBD["crease_edge_count"])
        self.assertEqual(len(SHARP), spec.SUBD["sharp_edge_count"])
        self.assertEqual(len(SMOOTH), spec.SUBD["smooth_not_sharp_edge_count"])

    def test_component_totals(self):
        self.assertEqual(len(spec.VERTEX_POINTS), spec.SUBD["vertex_count"])
        self.assertEqual(len(spec.EDGES_BY_KEY), spec.SUBD["edge_count"])
        self.assertEqual(len(spec.FACES), spec.SUBD["face_count"])
        self.assertEqual(set(spec.VERTICES), set(spec.VERTEX_POINTS))

    def test_one_catmull_clark_step(self):
        faces = spec.SUBD["face_count"]
        edges = spec.SUBD["edge_count"]
        vertices = spec.SUBD["vertex_count"]
        corners = sum(len(f["loop"]) for f in spec.FACES.values())
        self.assertEqual(spec.SUBD["subdivided_once"],
                         {"face_count": corners,
                          "edge_count": 2 * edges + corners,
                          "vertex_count": vertices + edges + faces})


class TestEdgeTable(unittest.TestCase):

    def test_sharpness_is_keyed_by_the_edges_own_endpoints(self):
        for key, edge in spec.EDGES_BY_KEY.items():
            with self.subTest(edge=key):
                self.assertEqual(set(edge["sharpness"]), set(key))

    def test_tags_and_predicates_follow_the_group(self):
        for key, edge in spec.EDGES_BY_KEY.items():
            with self.subTest(edge=key):
                is_crease = key in CREASE
                self.assertEqual(edge["is_crease"], is_crease)
                self.assertEqual(edge["is_smooth"], not is_crease)
                self.assertEqual(edge["tag"], "Crease" if is_crease else "Smooth")
                self.assertEqual(edge["is_sharp"], key in SHARP)

    def test_only_sharp_edges_carry_sharpness(self):
        for key, edge in spec.EDGES_BY_KEY.items():
            with self.subTest(edge=key):
                values = list(edge["sharpness"].values())
                if key in SHARP:
                    self.assertTrue(all(v > 0.0 for v in values))
                else:
                    self.assertTrue(all(v == 0.0 for v in values))

    def test_sharp_edges_satisfy_the_is_sharp_rule(self):
        # ON_SubDEdgeSharpness::IsSharp wants a valid value - 0 to 4 inclusive -
        # with at least one end above zero. Note the ON_SubDEdge::IsSharp header
        # comment also claims one end must be *below* the maximum; the code does
        # not, and test_SubD_SharpnessWrite pins the real behaviour.
        for key in SHARP:
            values = list(spec.EDGES_BY_KEY[key]["sharpness"].values())
            with self.subTest(edge=key):
                self.assertTrue(all(0.0 <= v <= MAX_SHARPNESS for v in values))
                self.assertTrue(any(v > 0.0 for v in values))

    def test_hard_and_dart_creases_follow_the_end_vertex_tags(self):
        for key, edge in spec.EDGES_BY_KEY.items():
            with self.subTest(edge=key):
                tags = [spec.VERTICES[name]["tag"] for name in key]
                darts = sum(1 for t in tags if t == "Dart")
                self.assertEqual(edge["dart_count"], darts)
                self.assertEqual(edge["is_dart_crease"],
                                 key in CREASE and darts > 0)
                self.assertEqual(edge["is_hard_crease"],
                                 key in CREASE
                                 and all(t in ("Crease", "Corner") for t in tags))

    def test_subdivision_points_are_pinned_only_where_they_are_exact(self):
        # A crease subdivides to its control net midpoint, and so does a sharp
        # edge whose average sharpness reaches 1. Nothing else is hard-coded.
        for key, edge in spec.EDGES_BY_KEY.items():
            with self.subTest(edge=key):
                if key in CREASE:
                    self.assertEqual(edge["subdivision_point"],
                                     _midpoint(key[0], key[1]))
                elif key in SHARP:
                    average = sum(edge["sharpness"].values()) / 2.0
                    self.assertGreaterEqual(
                        average, 1.0,
                        "%s averages %g, so it does not collapse to the midpoint "
                        "and must not pin a subdivision point" % (key, average))
                    self.assertEqual(edge["subdivision_point"],
                                     _midpoint(key[0], key[1]))
                else:
                    self.assertIsNone(edge["subdivision_point"])


class TestVertexTable(unittest.TestCase):

    def test_every_vertex_has_three_edges(self):
        for name in spec.VERTICES:
            with self.subTest(vertex=name):
                self.assertEqual(len(_touching(name)), spec.VERTEX_EDGE_COUNT)

    def test_crease_counts_match_the_edge_table(self):
        for name, vertex in spec.VERTICES.items():
            with self.subTest(vertex=name):
                creases = sum(1 for key in _touching(name) if key in CREASE)
                self.assertEqual(vertex["crease_edge_count"], creases)

    def test_tag_follows_the_crease_count(self):
        # What UpdateAllTagsAndSectorCoefficients derives: three or more creases
        # is a Corner, two a Crease, one a Dart, none a Smooth.
        for name, vertex in spec.VERTICES.items():
            creases = vertex["crease_edge_count"]
            expected = ("Corner" if creases >= 3 else
                        "Crease" if creases == 2 else
                        "Dart" if creases == 1 else "Smooth")
            with self.subTest(vertex=name):
                self.assertEqual(vertex["tag"], expected)

    def test_predicates_agree_with_the_tag(self):
        for name, vertex in spec.VERTICES.items():
            with self.subTest(vertex=name):
                for key, tag in (("is_corner", "Corner"), ("is_crease", "Crease"),
                                 ("is_dart", "Dart"), ("is_smooth", "Smooth")):
                    self.assertEqual(vertex[key], vertex["tag"] == tag)

    def test_all_four_tags_are_exercised(self):
        self.assertEqual({v["tag"] for v in spec.VERTICES.values()},
                         {"Corner", "Crease", "Dart", "Smooth"})

    def test_vertex_sharpness_follows_the_opennurbs_rule(self):
        # ON_SubDVertex::VertexSharpness: a corner is never sharp; a smooth
        # vertex needs two sharp ends before it reports one; a crease or dart
        # needs only one. The maximum of the qualifying ends is the answer.
        for name, vertex in spec.VERTICES.items():
            ends = [spec.EDGES_BY_KEY[key]["sharpness"][name]
                    for key in _touching(name) if spec.EDGES_BY_KEY[key]["is_sharp"]]
            nonzero = [s for s in ends if s > 0.0]
            if vertex["tag"] == "Corner":
                expected = 0.0
            elif vertex["tag"] == "Smooth":
                expected = max(nonzero) if len(nonzero) >= 2 else 0.0
            else:
                expected = max(nonzero) if nonzero else 0.0
            with self.subTest(vertex=name):
                self.assertEqual(vertex["vertex_sharpness"], expected)

    def test_is_sharp_follows_the_opennurbs_rule(self):
        # IsSharp only asks whether a sharp edge is attached - it does not apply
        # the two-ends rule, which is why F, G and H are sharp with a sharpness
        # of zero.
        for name, vertex in spec.VERTICES.items():
            attached_sharp = any(spec.EDGES_BY_KEY[key]["is_sharp"]
                                 for key in _touching(name))
            expected = vertex["tag"] != "Corner" and attached_sharp
            with self.subTest(vertex=name):
                self.assertEqual(vertex["is_sharp"], expected)

    def test_only_the_corner_pins_a_surface_point(self):
        for name, vertex in spec.VERTICES.items():
            with self.subTest(vertex=name):
                if vertex["tag"] == "Corner":
                    self.assertEqual(vertex["surface_point"],
                                     spec.VERTEX_POINTS[name])
                else:
                    self.assertIsNone(vertex["surface_point"])


class TestFaceTable(unittest.TestCase):

    def test_declared_boundary_matches_the_loop(self):
        for name, face in spec.FACES.items():
            loop = face["loop"]
            walked = {spec.edge_key(loop[i], loop[(i + 1) % len(loop)])
                      for i in range(len(loop))}
            with self.subTest(face=name):
                self.assertEqual({spec.canon(e) for e in face["edges"]}, walked)

    def test_every_edge_borders_exactly_two_faces(self):
        for key in spec.EDGES_BY_KEY:
            owners = [name for name, face in spec.FACES.items()
                      if key in {spec.canon(e) for e in face["edges"]}]
            with self.subTest(edge=key):
                self.assertEqual(len(owners), spec.EDGE_FACE_COUNT)

    def test_every_vertex_borders_exactly_three_faces(self):
        for name in spec.VERTICES:
            owners = [f for f in spec.FACES.values() if name in f["loop"]]
            with self.subTest(vertex=name):
                self.assertEqual(len(owners), spec.VERTEX_FACE_COUNT)

    def test_centre_is_the_average_of_the_loop(self):
        for name, face in spec.FACES.items():
            points = [spec.VERTEX_POINTS[v] for v in face["loop"]]
            average = tuple(sum(p[i] for p in points) / len(points)
                            for i in range(3))
            with self.subTest(face=name):
                self.assertEqual(tuple(float(c) for c in face["centre"]), average)

    def test_normal_is_the_outward_normal_of_the_winding(self):
        # If a loop is wound the wrong way the box turns inside out, and every
        # face-normal assertion in RH3DM-175 would then be checking the wrong sign.
        for name, face in spec.FACES.items():
            points = [spec.VERTEX_POINTS[v] for v in face["loop"]]
            u = tuple(points[1][i] - points[0][i] for i in range(3))
            w = tuple(points[2][i] - points[1][i] for i in range(3))
            normal = _cross(u, w)
            length = sum(c * c for c in normal) ** 0.5
            unit = tuple(round(c / length, 9) for c in normal)
            with self.subTest(face=name):
                self.assertEqual(unit, tuple(float(c) for c in face["normal"]))
                # ...and outward means pointing away from the box centre.
                centre = face["centre"]
                middle = spec.SIZE / 2.0
                outward = sum(unit[i] * (centre[i] - middle) for i in range(3))
                self.assertGreater(outward, 0.0)

    def test_sharpness_aggregates_match_the_boundary_edges(self):
        for name, face in spec.FACES.items():
            boundary = [spec.canon(e) for e in face["edges"]]
            sharp = [key for key in boundary if key in SHARP]
            values = [s for key in sharp
                      for s in spec.EDGES_BY_KEY[key]["sharpness"].values()]
            with self.subTest(face=name):
                self.assertEqual(face["sharp_edge_count"], len(sharp))
                self.assertEqual(face["has_sharp_edges"], bool(sharp))
                self.assertEqual(face["max_edge_sharpness"],
                                 max(values) if values else 0.0)

    def test_faces_are_quads(self):
        for name, face in spec.FACES.items():
            with self.subTest(face=name):
                self.assertEqual(len(face["loop"]), spec.FACE_VERTEX_COUNT)
                self.assertEqual(len(face["edges"]), spec.FACE_EDGE_COUNT)


if __name__ == "__main__":
    unittest.main(verbosity=2)
