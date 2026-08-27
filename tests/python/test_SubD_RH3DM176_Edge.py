"""RH3DM-176 - Wrap SubD Edge API.

Every member the binding puts on rhino3dm.SubDEdge, checked against the
authored fixture. The crease and sharpness members get a fuller workout in
test_SubD_RH3DM169_Creases.py; here they are covered as part of the edge
surface, alongside topology and geometry.

Run standalone:

    python test_SubD_RH3DM176_Edge.py -v
"""

import os
import sys
import unittest

_HERE = os.path.dirname(os.path.abspath(__file__))
if _HERE not in sys.path:
    sys.path.insert(0, _HERE)

import subd_fixture_spec as spec  # noqa: E402
import subd_fixture as fixture  # noqa: E402


class TestEdgeIdentityAndTopology(fixture.SubDFixtureTestCase):

    def test_every_authored_edge_is_present_exactly_once(self):
        self.assertEqual(set(self.edges), set(spec.EDGES_BY_KEY))
        self.assertEqual(len(self.edges), spec.SUBD["edge_count"])

    def test_ids_are_unique_and_round_trip(self):
        ids = [e.Id for e in self.subd.Edges]
        self.assertEqual(len(set(ids)), spec.SUBD["edge_count"])
        for edge in self.subd.Edges:
            self.assertEqual(self.subd.Edges[edge.Id].Id, edge.Id)
            self.assertEqual(edge.Index, edge.Id)

    def test_vertex_and_face_counts(self):
        for name, edge in self.edges.items():
            with self.subTest(edge=name):
                self.assertEqual(edge.VertexCount, spec.EDGE_VERTEX_COUNT)
                # Every edge of a closed box is interior, so it has two faces.
                self.assertEqual(edge.FaceCount, spec.EDGE_FACE_COUNT)

    def test_endpoints_are_the_authored_pair(self):
        for name, edge in self.edges.items():
            with self.subTest(edge=name):
                got = {fixture.vertex_name(edge.Vertex(0)),
                       fixture.vertex_name(edge.Vertex(1))}
                self.assertEqual(got, set(name))

    def test_vertex_id_agrees_with_vertex(self):
        for name, edge in self.edges.items():
            with self.subTest(edge=name):
                for i in (0, 1):
                    self.assertEqual(edge.VertexId(i), edge.Vertex(i).Id)

    def test_sub_iterators_agree_with_the_indexed_accessors(self):
        for name, edge in self.edges.items():
            with self.subTest(edge=name):
                self.assertEqual(edge.Vertices.Count, edge.VertexCount)
                self.assertEqual(edge.Faces.Count, edge.FaceCount)
                self.assertEqual([v.Id for v in edge.Vertices],
                                 [edge.Vertex(i).Id for i in range(edge.VertexCount)])

    def test_adjacent_faces_report_this_edge(self):
        for name, edge in self.edges.items():
            with self.subTest(edge=name):
                for face in edge.Faces:
                    self.assertIn(edge.Id, [e.Id for e in face.Edges])

    def test_each_face_pair_matches_the_authored_boundaries(self):
        for name, edge in self.edges.items():
            with self.subTest(edge=name):
                got = {fixture.face_name(f) for f in edge.Faces}
                expected = {fname for fname, f in spec.FACES.items()
                            if name in {spec.canon(e) for e in f["edges"]}}
                self.assertEqual(got, expected)


class TestEdgeGeometry(fixture.SubDFixtureTestCase):

    def test_control_net_points_are_the_endpoint_positions(self):
        for name, edge in self.edges.items():
            with self.subTest(edge=name):
                for i in (0, 1):
                    self.assertPointEqual(
                        edge.ControlNetPoint(i),
                        spec.VERTEX_POINTS[fixture.vertex_name(edge.Vertex(i))],
                        "%s.ControlNetPoint(%d)" % (name, i))

    def test_control_net_direction_runs_from_end_0_to_end_1(self):
        # ON_SubDEdge::ControlNetDirection is P1 - P0, unnormalised. Comparing
        # it against the edge's own endpoints keeps this independent of which
        # endpoint opennurbs happened to store first.
        for name, edge in self.edges.items():
            with self.subTest(edge=name):
                p0 = spec.VERTEX_POINTS[fixture.vertex_name(edge.Vertex(0))]
                p1 = spec.VERTEX_POINTS[fixture.vertex_name(edge.Vertex(1))]
                expected = tuple(b - a for a, b in zip(p0, p1))
                direction = edge.ControlNetDirection
                self.assertPointEqual(direction, expected,
                                      "%s.ControlNetDirection" % name)
                length = (direction.X ** 2 + direction.Y ** 2 + direction.Z ** 2) ** 0.5
                self.assertAlmostEqual(length, spec.SIZE, delta=spec.VALUE_TOL)

    def test_control_net_centre_point_is_the_midpoint(self):
        for name, edge in self.edges.items():
            with self.subTest(edge=name):
                p0 = spec.VERTEX_POINTS[fixture.vertex_name(edge.Vertex(0))]
                p1 = spec.VERTEX_POINTS[fixture.vertex_name(edge.Vertex(1))]
                midpoint = tuple((a + b) / 2.0 for a, b in zip(p0, p1))
                self.assertPointEqual(edge.ControlNetCenterPoint, midpoint,
                                      "%s.ControlNetCenterPoint" % name)

    def test_control_net_centre_normal_matches_the_indexed_face(self):
        # ControlNetCenterNormal(i) is the normal of the edge's i-th face, so it
        # has to agree with that face's own normal.
        for name, edge in self.edges.items():
            with self.subTest(edge=name):
                for i in range(edge.FaceCount):
                    face = edge.Faces[i]
                    self.assertUnitDirection(
                        edge.ControlNetCenterNormal(i),
                        spec.FACES[fixture.face_name(face)]["normal"],
                        "%s.ControlNetCenterNormal(%d)" % (name, i))

    def test_subdivision_point(self):
        # Hard-coded only where it is exact: a crease subdivides to the control
        # net midpoint, and so does a sharp edge whose average sharpness is >= 1
        # (all three of ours are). The four ordinary smooth edges get the
        # Catmull-Clark blend, which is asserted to be *inside* the box and off
        # the midpoint rather than pinned to a number.
        for name, edge in self.edges.items():
            expected = spec.EDGES_BY_KEY[name]["subdivision_point"]
            with self.subTest(edge=name):
                point = edge.SubdivisionPoint
                if expected is not None:
                    self.assertPointEqual(point, expected,
                                          "%s.SubdivisionPoint" % name)
                else:
                    for axis in (point.X, point.Y, point.Z):
                        self.assertGreaterEqual(axis, -spec.VALUE_TOL)
                        self.assertLessEqual(axis, spec.SIZE + spec.VALUE_TOL)
                    centre = edge.ControlNetCenterPoint
                    self.assertNotAlmostEqual(
                        (point.X - centre.X) ** 2 + (point.Y - centre.Y) ** 2
                        + (point.Z - centre.Z) ** 2, 0.0, delta=spec.VALUE_TOL,
                        msg="%s is smooth and not sharp, so its subdivision point "
                            "should not be the control net midpoint" % name)


class TestEdgeTagsAndCreases(fixture.SubDFixtureTestCase):

    def test_tag(self):
        for name, edge in self.edges.items():
            with self.subTest(edge=name):
                expected = spec.EDGES_BY_KEY[name]["tag"]
                self.assertEqual(edge.Tag, fixture.EDGE_TAGS[expected],
                                 "%s should be tagged %s" % (name, expected))

    def test_is_smooth_and_is_crease_follow_the_tag(self):
        for name, edge in self.edges.items():
            expected = spec.EDGES_BY_KEY[name]
            with self.subTest(edge=name):
                self.assertEqual(edge.IsSmooth, expected["is_smooth"])
                self.assertEqual(edge.IsCrease, expected["is_crease"])
                self.assertEqual(edge.IsCrease, edge.Tag == fixture.EDGE_TAGS["Crease"])
                self.assertNotEqual(edge.IsSmooth, edge.IsCrease)

    def test_hard_crease_and_dart_crease(self):
        # AE is the discriminating case: it is a crease, but E is a dart, so it
        # is a dart crease and not a hard one. Everything else on the bottom
        # ring runs between crease/corner vertices and is a hard crease.
        for name, edge in self.edges.items():
            expected = spec.EDGES_BY_KEY[name]
            with self.subTest(edge=name):
                self.assertEqual(edge.IsHardCrease, expected["is_hard_crease"])
                self.assertEqual(edge.IsDartCrease, expected["is_dart_crease"])
                self.assertEqual(edge.DartCount, expected["dart_count"])

    def test_dart_count_matches_the_dart_vertices_on_the_edge(self):
        for name, edge in self.edges.items():
            with self.subTest(edge=name):
                darts = sum(1 for i in (0, 1) if edge.Vertex(i).IsDart)
                self.assertEqual(edge.DartCount, darts)

    def test_is_sharp_and_end_sharpness(self):
        for name, edge in self.edges.items():
            expected = spec.EDGES_BY_KEY[name]
            with self.subTest(edge=name):
                self.assertEqual(edge.IsSharp, expected["is_sharp"])
                for i in (0, 1):
                    at = fixture.vertex_name(edge.Vertex(i))
                    self.assertAlmostEqual(
                        edge.EndSharpness(i), expected["sharpness"][at],
                        delta=spec.VALUE_TOL,
                        msg="%s.EndSharpness at %s" % (name, at))


if __name__ == "__main__":
    unittest.main(verbosity=2)
