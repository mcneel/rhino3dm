"""RH3DM-175 - Wrap SubD Face API.

Every member the binding puts on rhino3dm.SubDFace, checked against the
authored fixture rather than against its own type. "It returned a float" does
not tell a Blender importer that the face it is about to write is the right one.

Run standalone:

    python test_SubD_RH3DM175_Face.py -v
"""

import os
import sys
import unittest

_HERE = os.path.dirname(os.path.abspath(__file__))
if _HERE not in sys.path:
    sys.path.insert(0, _HERE)

import subd_fixture_spec as spec  # noqa: E402
import subd_fixture as fixture  # noqa: E402


class TestFaceIdentityAndTopology(fixture.SubDFixtureTestCase):

    def test_every_authored_face_is_present_exactly_once(self):
        self.assertEqual(set(self.faces), set(spec.FACES))
        self.assertEqual(len(self.faces), spec.SUBD["face_count"])

    def test_ids_are_unique_and_round_trip(self):
        ids = [f.Id for f in self.subd.Faces]
        self.assertEqual(len(set(ids)), spec.SUBD["face_count"])
        for face in self.subd.Faces:
            self.assertEqual(self.subd.Faces[face.Id].Id, face.Id)
            self.assertEqual(face.Index, face.Id)

    def test_edge_and_vertex_counts(self):
        for name, face in self.faces.items():
            with self.subTest(face=name):
                self.assertEqual(face.EdgeCount, spec.FACE_EDGE_COUNT)
                self.assertEqual(face.VertexCount, spec.FACE_VERTEX_COUNT)
                self.assertTrue(face.HasEdges)

    def test_vertices_are_the_authored_loop(self):
        for name, face in self.faces.items():
            with self.subTest(face=name):
                got = [fixture.vertex_name(face.Vertex(i))
                       for i in range(face.VertexCount)]
                self.assertEqual(set(got), set(spec.FACES[name]["loop"]))

    def test_edges_are_the_authored_boundary(self):
        for name, face in self.faces.items():
            with self.subTest(face=name):
                got = {fixture.edge_name(face.Edge(i)) for i in range(face.EdgeCount)}
                expected = {spec.canon(e) for e in spec.FACES[name]["edges"]}
                self.assertEqual(got, expected)

    def test_sub_iterators_agree_with_the_indexed_accessors(self):
        for name, face in self.faces.items():
            with self.subTest(face=name):
                self.assertEqual(face.Edges.Count, face.EdgeCount)
                self.assertEqual(face.Vertices.Count, face.VertexCount)
                self.assertEqual([e.Id for e in face.Edges],
                                 [face.Edge(i).Id for i in range(face.EdgeCount)])
                self.assertEqual([v.Id for v in face.Vertices],
                                 [face.Vertex(i).Id for i in range(face.VertexCount)])

    def test_every_face_edge_reports_this_face(self):
        for name, face in self.faces.items():
            with self.subTest(face=name):
                for edge in face.Edges:
                    self.assertIn(face.Id, [f.Id for f in edge.Faces])


class TestFaceGeometry(fixture.SubDFixtureTestCase):

    def test_control_net_points_are_the_authored_corners(self):
        for name, face in self.faces.items():
            with self.subTest(face=name):
                for i in range(face.VertexCount):
                    vertex = face.Vertex(i)
                    self.assertPointEqual(
                        face.ControlNetPoint(i),
                        spec.VERTEX_POINTS[fixture.vertex_name(vertex)],
                        "%s.ControlNetPoint(%d)" % (name, i))

    def test_control_net_centre_point(self):
        for name, face in self.faces.items():
            with self.subTest(face=name):
                self.assertPointEqual(face.ControlNetCenterPoint,
                                      spec.FACES[name]["centre"],
                                      "%s.ControlNetCenterPoint" % name)

    def test_subdivision_point_of_a_quad_is_its_centre(self):
        # ON_SubDFace::EvaluateCatmullClarkSubdivisionPoint averages the four
        # control net points for a quad, which is the control net centre.
        for name, face in self.faces.items():
            with self.subTest(face=name):
                self.assertPointEqual(face.SubdivisionPoint,
                                      spec.FACES[name]["centre"],
                                      "%s.SubdivisionPoint" % name)

    def test_control_net_centre_normal_points_out_of_the_box(self):
        for name, face in self.faces.items():
            with self.subTest(face=name):
                self.assertUnitDirection(face.ControlNetCenterNormal,
                                         spec.FACES[name]["normal"],
                                         "%s.ControlNetCenterNormal" % name)

    def test_control_net_centre_frame(self):
        for name, face in self.faces.items():
            with self.subTest(face=name):
                frame = face.ControlNetCenterFrame
                self.assertPointEqual(frame.Origin, spec.FACES[name]["centre"],
                                      "%s.ControlNetCenterFrame.Origin" % name)
                self.assertUnitDirection(frame.ZAxis, spec.FACES[name]["normal"],
                                         "%s.ControlNetCenterFrame.ZAxis" % name)

    def test_faces_are_convex_and_planar(self):
        for name, face in self.faces.items():
            with self.subTest(face=name):
                self.assertTrue(face.IsConvex, "%s should be convex" % name)
                self.assertFalse(face.IsNotConvex)
                self.assertTrue(face.IsPlanar(spec.VALUE_TOL),
                                "%s should be planar" % name)
                self.assertFalse(face.IsNotPlanar(spec.VALUE_TOL))


class TestFaceSharpness(fixture.SubDFixtureTestCase):
    """The face-level view of RH3DM-169's crease data."""

    def test_sharp_edge_count(self):
        for name, face in self.faces.items():
            with self.subTest(face=name):
                self.assertEqual(face.SharpEdgeCount,
                                 spec.FACES[name]["sharp_edge_count"])

    def test_has_sharp_edges(self):
        for name, face in self.faces.items():
            with self.subTest(face=name):
                self.assertEqual(face.HasSharpEdges,
                                 spec.FACES[name]["has_sharp_edges"])

    def test_maximum_edge_sharpness(self):
        # Creases count as zero here, so the all-crease bottom face reports 0.0
        # while the left face reports 2.5 - the larger end of the tapered edge.
        for name, face in self.faces.items():
            with self.subTest(face=name):
                self.assertAlmostEqual(face.MaximumEdgeSharpness,
                                       spec.FACES[name]["max_edge_sharpness"],
                                       delta=spec.VALUE_TOL)

    def test_face_sharpness_agrees_with_its_own_edges(self):
        for name, face in self.faces.items():
            with self.subTest(face=name):
                sharp = [e for e in face.Edges if e.IsSharp]
                self.assertEqual(len(sharp), face.SharpEdgeCount)
                ends = [e.EndSharpness(i) for e in sharp for i in (0, 1)]
                self.assertAlmostEqual(face.MaximumEdgeSharpness,
                                       max(ends) if ends else 0.0,
                                       delta=spec.VALUE_TOL)


class TestFaceAttributes(fixture.SubDFixtureTestCase):
    """Members the fixture leaves at their defaults.

    These are asserted at their default values rather than skipped: a binding
    that returned garbage for an unset texture point would still pass a type
    check, and reading one must not crash.
    """

    def test_material_channel_index_is_unset(self):
        for name, face in self.faces.items():
            with self.subTest(face=name):
                self.assertEqual(face.MaterialChannelIndex, 0)

    def test_per_face_color_resolves(self):
        for name, face in self.faces.items():
            with self.subTest(face=name):
                self.assertIsNotNone(face.PerFaceColor)

    def test_texture_points_are_not_set(self):
        for name, face in self.faces.items():
            with self.subTest(face=name):
                self.assertFalse(face.TexturePointsAreSet)
                self.assertEqual(face.TexturePointsCapacity, 0)

    def test_texture_point_accessors_are_safe_when_unset(self):
        # Reading a texture point on a face that has none must return a point,
        # not fault - a Blender importer asks before it knows.
        for name, face in self.faces.items():
            with self.subTest(face=name):
                point = face.TexturePoint(0)
                self.assertTrue(hasattr(point, "X"))
                centre = face.TextureCenterPoint
                self.assertTrue(hasattr(centre, "X"))


if __name__ == "__main__":
    unittest.main(verbosity=2)
