"""RH3DM-177 - Wrap SubD Vertex API.

Every member the binding puts on rhino3dm.SubDVertex, checked against the
authored fixture. The fixture is built so that all four meaningful vertex tags
occur - Corner at A, Crease at B/C/D, Dart at E, Smooth at F/G/H - which is
what makes the tag predicates worth asserting at all.

Run standalone:

    python test_SubD_RH3DM177_Vertex.py -v
"""

import os
import sys
import unittest

_HERE = os.path.dirname(os.path.abspath(__file__))
if _HERE not in sys.path:
    sys.path.insert(0, _HERE)

import subd_fixture_spec as spec  # noqa: E402
import subd_fixture as fixture  # noqa: E402


class TestVertexIdentityAndTopology(fixture.SubDFixtureTestCase):

    def test_every_authored_vertex_is_present_exactly_once(self):
        self.assertEqual(set(self.vertices), set(spec.VERTICES))
        self.assertEqual(len(self.vertices), spec.SUBD["vertex_count"])

    def test_ids_are_unique_and_round_trip(self):
        ids = [v.Id for v in self.subd.Vertices]
        self.assertEqual(len(set(ids)), spec.SUBD["vertex_count"])
        for vertex in self.subd.Vertices:
            self.assertEqual(self.subd.Vertices[vertex.Id].Id, vertex.Id)
            self.assertEqual(vertex.Index, vertex.Id)

    def test_edge_and_face_counts(self):
        for name, vertex in self.vertices.items():
            with self.subTest(vertex=name):
                self.assertEqual(vertex.EdgeCount, spec.VERTEX_EDGE_COUNT)
                self.assertEqual(vertex.FaceCount, spec.VERTEX_FACE_COUNT)

    def test_sub_iterators_agree_with_the_indexed_accessor(self):
        for name, vertex in self.vertices.items():
            with self.subTest(vertex=name):
                self.assertEqual(vertex.Edges.Count, vertex.EdgeCount)
                self.assertEqual(vertex.Faces.Count, vertex.FaceCount)
                self.assertEqual([e.Id for e in vertex.Edges],
                                 [vertex.Edge(i).Id for i in range(vertex.EdgeCount)])

    def test_attached_edges_are_the_authored_ones(self):
        for name, vertex in self.vertices.items():
            with self.subTest(vertex=name):
                got = {fixture.edge_name(e) for e in vertex.Edges}
                expected = {key for key in spec.EDGES_BY_KEY if name in key}
                self.assertEqual(got, expected)

    def test_attached_faces_are_the_authored_ones(self):
        for name, vertex in self.vertices.items():
            with self.subTest(vertex=name):
                got = {fixture.face_name(f) for f in vertex.Faces}
                expected = {fname for fname, f in spec.FACES.items()
                            if name in f["loop"]}
                self.assertEqual(got, expected)

    def test_attached_edges_report_this_vertex(self):
        for name, vertex in self.vertices.items():
            with self.subTest(vertex=name):
                for edge in vertex.Edges:
                    self.assertIn(name, {fixture.vertex_name(edge.Vertex(0)),
                                         fixture.vertex_name(edge.Vertex(1))})


class TestVertexLinkedList(fixture.SubDFixtureTestCase):
    """Next/Previous walk the SubD's own vertex list, not the control net."""

    def test_next_from_the_head_visits_every_vertex(self):
        head = self.subd.Vertices.First()
        seen = [head.Id]
        vertex = head
        for _ in range(spec.SUBD["vertex_count"] - 1):
            vertex = vertex.Next()
            seen.append(vertex.Id)
        self.assertEqual(len(set(seen)), spec.SUBD["vertex_count"])
        self.assertEqual(set(seen), {v.Id for v in self.subd.Vertices})

    def test_next_and_previous_are_inverses(self):
        # Stepping off either end of the list would dereference null, so this
        # only ever steps forward and back from the head.
        head = self.subd.Vertices.First()
        self.assertEqual(head.Next().Previous(), head)


class TestVertexGeometry(fixture.SubDFixtureTestCase):

    def test_control_net_point(self):
        for name, vertex in self.vertices.items():
            with self.subTest(vertex=name):
                self.assertPointEqual(vertex.ControlNetPoint,
                                      spec.VERTEX_POINTS[name],
                                      "%s.ControlNetPoint" % name)

    def test_surface_point(self):
        # Hard-coded only for the Corner, which interpolates its control net
        # point exactly. The rest are limit points of a Catmull-Clark surface;
        # they are asserted to be inside the control net box and, except at the
        # corner, pulled off the control net point.
        for name, vertex in self.vertices.items():
            expected = spec.VERTICES[name]["surface_point"]
            with self.subTest(vertex=name):
                point = vertex.SurfacePoint
                if expected is not None:
                    self.assertPointEqual(point, expected,
                                          "%s.SurfacePoint" % name)
                    continue
                for axis in (point.X, point.Y, point.Z):
                    self.assertGreaterEqual(axis, -spec.VALUE_TOL)
                    self.assertLessEqual(axis, spec.SIZE + spec.VALUE_TOL)
                control = vertex.ControlNetPoint
                self.assertNotAlmostEqual(
                    (point.X - control.X) ** 2 + (point.Y - control.Y) ** 2
                    + (point.Z - control.Z) ** 2, 0.0, delta=spec.VALUE_TOL,
                    msg="%s is not a corner, so its limit point should not sit "
                        "on its control net point" % name)


class TestVertexTags(fixture.SubDFixtureTestCase):

    def test_tag(self):
        for name, vertex in self.vertices.items():
            expected = spec.VERTICES[name]["tag"]
            with self.subTest(vertex=name):
                self.assertEqual(vertex.Tag, fixture.VERTEX_TAGS[expected],
                                 "%s should be tagged %s" % (name, expected))

    def test_all_four_meaningful_tags_occur(self):
        # If the fixture ever degenerates to one tag, the predicates below stop
        # discriminating anything.
        tags = {v.Tag for v in self.subd.Vertices}
        self.assertEqual(tags, {fixture.VERTEX_TAGS[t]
                                for t in ("Corner", "Crease", "Dart", "Smooth")})

    def test_tag_predicates(self):
        for name, vertex in self.vertices.items():
            expected = spec.VERTICES[name]
            with self.subTest(vertex=name):
                self.assertEqual(vertex.IsCorner, expected["is_corner"])
                self.assertEqual(vertex.IsCrease, expected["is_crease"])
                self.assertEqual(vertex.IsDart, expected["is_dart"])
                self.assertEqual(vertex.IsSmooth, expected["is_smooth"])

    def test_exactly_one_predicate_matches_each_vertex(self):
        for name, vertex in self.vertices.items():
            with self.subTest(vertex=name):
                matched = [vertex.IsSmooth, vertex.IsCrease,
                           vertex.IsCorner, vertex.IsDart]
                self.assertEqual(sum(1 for m in matched if m), 1)

    def test_tag_follows_the_crease_count_at_the_vertex(self):
        # This is what UpdateAllTagsAndSectorCoefficients derives the tag from:
        # three or more creases is a corner, two is a crease, one is a dart.
        for name, vertex in self.vertices.items():
            expected = spec.VERTICES[name]
            with self.subTest(vertex=name):
                creases = sum(1 for e in vertex.Edges if e.IsCrease)
                self.assertEqual(creases, expected["crease_edge_count"])


class TestVertexSharpness(fixture.SubDFixtureTestCase):
    """The vertex-level view of RH3DM-169's crease data."""

    def test_vertex_sharpness(self):
        # The discriminating cases are F, G and H: each is smooth and touches
        # exactly one sharp edge, and ON_SubDVertex::VertexSharpness requires
        # *two* sharp ends at a smooth vertex, so they report 0.0 - while the
        # crease vertices B, C and D report their single sharp edge's value.
        for name, vertex in self.vertices.items():
            with self.subTest(vertex=name):
                self.assertAlmostEqual(vertex.VertexSharpness,
                                       spec.VERTICES[name]["vertex_sharpness"],
                                       delta=spec.VALUE_TOL)

    def test_is_sharp(self):
        for name, vertex in self.vertices.items():
            expected = spec.VERTICES[name]["is_sharp"]
            with self.subTest(vertex=name):
                self.assertEqual(vertex.IsSharp(True), expected)
                # No edge in the fixture is sharp at one end only, so the
                # end-check and the any-sharp-edge check agree everywhere.
                self.assertEqual(vertex.IsSharp(False), expected)

    def test_a_sharp_vertex_touches_a_sharp_edge(self):
        for name, vertex in self.vertices.items():
            with self.subTest(vertex=name):
                touches_sharp = any(e.IsSharp for e in vertex.Edges)
                if vertex.IsSharp(True):
                    self.assertTrue(touches_sharp)
                if vertex.VertexSharpness > 0.0:
                    self.assertTrue(touches_sharp)


if __name__ == "__main__":
    unittest.main(verbosity=2)
