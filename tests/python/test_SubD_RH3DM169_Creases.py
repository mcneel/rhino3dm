"""RH3DM-169 - Adidas needs SubD Crease in rhino3dm.

The ask is a Blender importer being able to read a Rhino SubD's crease data out
of a .3dm. Two kinds of crease matter, and the issue turns on the second:

  * a *hard* crease is the SubDEdgeTag.Crease tag - the edge is fully creased;
  * a *soft* crease (a weighted edge) is a smooth-tagged edge carrying nonzero
    sharpness, 0 to 4, optionally different at each end.

The first pass at this issue bound the tag only, which covers hard creases and
nothing else. So the sharpness accessors - SubDEdge.IsSharp/EndSharpness,
SubDFace.SharpEdgeCount/MaximumEdgeSharpness, SubDVertex.VertexSharpness/
IsSharp - are what this file is really about, and the last test walks the whole
SubD the way an importer would.

Run standalone:

    python test_SubD_RH3DM169_Creases.py -v
"""

import os
import sys
import unittest

_HERE = os.path.dirname(os.path.abspath(__file__))
if _HERE not in sys.path:
    sys.path.insert(0, _HERE)

import subd_fixture_spec as spec  # noqa: E402
import subd_fixture as fixture  # noqa: E402

#: Blender stores edge crease as 0..1; opennurbs stores sharpness as 0..4.
BLENDER_CREASE_SCALE = 4.0


class TestHardCreases(fixture.SubDFixtureTestCase):

    def test_the_authored_creases_are_the_only_creases(self):
        creased = {name for name, e in self.edges.items() if e.IsCrease}
        self.assertEqual(creased, {spec.canon(e) for e in spec.CREASE_EDGES})
        self.assertEqual(len(creased), spec.SUBD["crease_edge_count"])

    def test_crease_edges_carry_the_crease_tag(self):
        for name in (spec.canon(e) for e in spec.CREASE_EDGES):
            with self.subTest(edge=name):
                edge = self.edges[name]
                self.assertEqual(edge.Tag, fixture.EDGE_TAGS["Crease"])
                self.assertTrue(edge.IsCrease)
                self.assertFalse(edge.IsSmooth)

    def test_a_crease_is_never_also_sharp(self):
        # Sharpness has no meaning on a crease: EndSharpness returns 0 there,
        # which is what stops an importer double-counting a hard crease as a
        # weighted edge.
        for name in (spec.canon(e) for e in spec.CREASE_EDGES):
            with self.subTest(edge=name):
                edge = self.edges[name]
                self.assertFalse(edge.IsSharp)
                self.assertEqual(edge.EndSharpness(0), 0.0)
                self.assertEqual(edge.EndSharpness(1), 0.0)

    def test_hard_crease_versus_dart_crease(self):
        # AE is the one crease that runs into a dart (E), so it is a dart crease
        # rather than a hard one. Every other crease joins crease/corner
        # vertices and is hard.
        self.assertTrue(self.edge("AB").IsHardCrease)
        self.assertFalse(self.edge("AB").IsDartCrease)
        self.assertEqual(self.edge("AB").DartCount, 0)

        self.assertFalse(self.edge("AE").IsHardCrease)
        self.assertTrue(self.edge("AE").IsDartCrease)
        self.assertEqual(self.edge("AE").DartCount, 1)

    def test_crease_vertices_follow_from_the_crease_edges(self):
        # The importer reads vertex creasing from the tags, and the tags are
        # derived: three creases at A make it a Corner, two make B/C/D Creases,
        # the single one at E makes it a Dart.
        for name, expected in (("A", "Corner"), ("B", "Crease"), ("C", "Crease"),
                               ("D", "Crease"), ("E", "Dart"),
                               ("F", "Smooth"), ("G", "Smooth"), ("H", "Smooth")):
            with self.subTest(vertex=name):
                self.assertEqual(self.vertices[name].Tag,
                                 fixture.VERTEX_TAGS[expected])

    def test_crease_edges_subdivide_to_their_midpoint(self):
        # Proof the crease actually reached the evaluator rather than just
        # being stored: a creased edge's subdivision point is the control net
        # midpoint, where a smooth one's is pulled towards its neighbours.
        for name in (spec.canon(e) for e in spec.CREASE_EDGES):
            with self.subTest(edge=name):
                edge = self.edges[name]
                self.assertPointEqual(edge.SubdivisionPoint,
                                      spec.EDGES_BY_KEY[name]["subdivision_point"],
                                      "%s.SubdivisionPoint" % name)


class TestSoftCreases(fixture.SubDFixtureTestCase):
    """Sharpness - the part the first pass at RH3DM-169 did not cover."""

    def test_the_authored_sharp_edges_are_the_only_sharp_edges(self):
        sharp = {name for name, e in self.edges.items() if e.IsSharp}
        self.assertEqual(sharp, {spec.canon(e) for e in spec.SHARP_EDGES})
        self.assertEqual(len(sharp), spec.SUBD["sharp_edge_count"])
        # SubD.SharpEdgeCount answers the same question without a walk, which is
        # how an importer decides whether a SubD carries soft creases at all.
        self.assertEqual(self.subd.SharpEdgeCount, spec.SUBD["sharp_edge_count"])

    def test_a_sharp_edge_is_smooth_tagged(self):
        # A soft crease is not a crease tag. An importer keying only on the tag
        # sees these as ordinary smooth edges - which is the bug behind this
        # issue.
        for name in (spec.canon(e) for e in spec.SHARP_EDGES):
            with self.subTest(edge=name):
                edge = self.edges[name]
                self.assertEqual(edge.Tag, fixture.EDGE_TAGS["Smooth"])
                self.assertTrue(edge.IsSmooth)
                self.assertFalse(edge.IsCrease)
                self.assertTrue(edge.IsSharp)

    def test_uniform_sharpness_values(self):
        for edge_name, vertex_name, expected in (("BF", "B", 1.0), ("BF", "F", 1.0),
                                                 ("CG", "C", 3.0), ("CG", "G", 3.0)):
            with self.subTest(edge=edge_name, at=vertex_name):
                self.assertAlmostEqual(
                    self._end_sharpness(edge_name, vertex_name), expected,
                    delta=spec.VALUE_TOL)

    def test_tapered_sharpness_differs_at_each_end(self):
        # DH is the case a per-edge single value cannot express: 0.5 at the
        # bottom, 2.5 at the top.
        self.assertAlmostEqual(self._end_sharpness("DH", "D"), 0.5,
                               delta=spec.VALUE_TOL)
        self.assertAlmostEqual(self._end_sharpness("DH", "H"), 2.5,
                               delta=spec.VALUE_TOL)
        self.assertNotAlmostEqual(self._end_sharpness("DH", "D"),
                                  self._end_sharpness("DH", "H"),
                                  delta=spec.VALUE_TOL)

    def test_smooth_edges_report_no_sharpness(self):
        for name in (spec.canon(e) for e in spec.SMOOTH_EDGES):
            with self.subTest(edge=name):
                edge = self.edges[name]
                self.assertFalse(edge.IsSharp)
                self.assertEqual(edge.EndSharpness(0), 0.0)
                self.assertEqual(edge.EndSharpness(1), 0.0)

    def test_sharpness_is_within_the_opennurbs_range(self):
        for name, edge in self.edges.items():
            with self.subTest(edge=name):
                for i in (0, 1):
                    self.assertGreaterEqual(edge.EndSharpness(i), 0.0)
                    self.assertLessEqual(edge.EndSharpness(i), BLENDER_CREASE_SCALE)

    def test_sharp_edges_reach_the_evaluator(self):
        # Each of the three sharp edges averages >= 1, and opennurbs collapses
        # such an edge's subdivision point onto the control net midpoint. If the
        # sharpness were stored but ignored, these would land on the ordinary
        # Catmull-Clark blend instead.
        for name in (spec.canon(e) for e in spec.SHARP_EDGES):
            with self.subTest(edge=name):
                edge = self.edges[name]
                self.assertPointEqual(edge.SubdivisionPoint,
                                      spec.EDGES_BY_KEY[name]["subdivision_point"],
                                      "%s.SubdivisionPoint" % name)

    def _end_sharpness(self, edge_name, vertex_name):
        """EndSharpness of an edge at the end sitting on a named vertex."""
        edge = self.edge(edge_name)
        for i in (0, 1):
            if fixture.vertex_name(edge.Vertex(i)) == vertex_name:
                return edge.EndSharpness(i)
        raise AssertionError("%s has no end at %s" % (edge_name, vertex_name))


class TestSharpnessOnFacesAndVertices(fixture.SubDFixtureTestCase):
    """The aggregate accessors, which are how an importer finds sharp edges."""

    def test_face_sharp_edge_count_and_maximum(self):
        for name, face in self.faces.items():
            expected = spec.FACES[name]
            with self.subTest(face=name):
                self.assertEqual(face.HasSharpEdges, expected["has_sharp_edges"])
                self.assertEqual(face.SharpEdgeCount, expected["sharp_edge_count"])
                self.assertAlmostEqual(face.MaximumEdgeSharpness,
                                       expected["max_edge_sharpness"],
                                       delta=spec.VALUE_TOL)

    def test_an_all_crease_face_reports_no_sharpness(self):
        # The bottom face is bounded by four hard creases. Creases count as
        # zero sharpness, so a face full of them still reports 0.0.
        bottom = self.faces["bottom"]
        self.assertTrue(all(e.IsCrease for e in bottom.Edges))
        self.assertFalse(bottom.HasSharpEdges)
        self.assertEqual(bottom.SharpEdgeCount, 0)
        self.assertEqual(bottom.MaximumEdgeSharpness, 0.0)

    def test_the_tapered_edge_contributes_its_larger_end(self):
        # DH runs 0.5..2.5 and bounds the back and left faces. Both take 2.5
        # from it - the back face is only higher because CG's 3.0 also bounds it.
        self.assertAlmostEqual(self.faces["left"].MaximumEdgeSharpness, 2.5,
                               delta=spec.VALUE_TOL)
        self.assertAlmostEqual(self.faces["back"].MaximumEdgeSharpness, 3.0,
                               delta=spec.VALUE_TOL)

    def test_vertex_sharpness(self):
        for name, vertex in self.vertices.items():
            with self.subTest(vertex=name):
                self.assertAlmostEqual(vertex.VertexSharpness,
                                       spec.VERTICES[name]["vertex_sharpness"],
                                       delta=spec.VALUE_TOL)

    def test_a_smooth_vertex_needs_two_sharp_ends(self):
        # The rule that most often surprises: at a crease or dart vertex one
        # sharp end is enough to give a nonzero VertexSharpness, but a smooth
        # vertex needs two. B and F both sit on the same sharp edge BF, and only
        # B - the crease vertex - reports a sharpness.
        self.assertTrue(self.edge("BF").IsSharp)
        self.assertTrue(self.vertices["B"].IsCrease)
        self.assertTrue(self.vertices["F"].IsSmooth)
        self.assertAlmostEqual(self.vertices["B"].VertexSharpness, 1.0,
                               delta=spec.VALUE_TOL)
        self.assertEqual(self.vertices["F"].VertexSharpness, 0.0)
        # Both are still reported as sharp vertices - IsSharp only asks whether
        # a sharp edge is attached.
        self.assertTrue(self.vertices["B"].IsSharp(True))
        self.assertTrue(self.vertices["F"].IsSharp(True))

    def test_a_corner_is_never_sharp(self):
        corner = self.vertices["A"]
        self.assertTrue(corner.IsCorner)
        self.assertFalse(corner.IsSharp(True))
        self.assertEqual(corner.VertexSharpness, 0.0)


class TestImporterWalk(fixture.SubDFixtureTestCase):
    """End to end: read the SubD the way a Blender importer would.

    Nothing here is new API - it is the members above, used together, to check
    that a caller who only has rhino3dm can rebuild the full crease picture.
    """

    def _crease_table(self):
        """[(endpoint ids, blender crease 0..1, is_hard)] for every creased edge."""
        table = []
        for edge in self.subd.Edges:
            endpoints = (edge.VertexId(0), edge.VertexId(1))
            if edge.IsCrease:
                table.append((endpoints, 1.0, True))
            elif edge.IsSharp:
                weight = max(edge.EndSharpness(0), edge.EndSharpness(1))
                table.append((endpoints, weight / BLENDER_CREASE_SCALE, False))
        return table

    def test_the_walk_finds_every_creased_edge_and_nothing_else(self):
        table = self._crease_table()
        self.assertEqual(len(table), spec.SUBD["crease_edge_count"]
                         + spec.SUBD["sharp_edge_count"])
        self.assertEqual(sum(1 for _, _, hard in table if hard),
                         spec.SUBD["crease_edge_count"])
        self.assertEqual(sum(1 for _, _, hard in table if not hard),
                         spec.SUBD["sharp_edge_count"])

    def test_the_walk_reads_the_authored_weights(self):
        by_endpoints = {}
        for endpoints, weight, hard in self._crease_table():
            names = frozenset(fixture.vertex_name(self.subd.Vertices[i])
                              for i in endpoints)
            by_endpoints["".join(sorted(names))] = (weight, hard)

        self.assertEqual(by_endpoints[spec.canon("BF")], (1.0 / 4.0, False))
        self.assertEqual(by_endpoints[spec.canon("CG")], (3.0 / 4.0, False))
        self.assertEqual(by_endpoints[spec.canon("DH")], (2.5 / 4.0, False))
        for name in spec.CREASE_EDGES:
            self.assertEqual(by_endpoints[spec.canon(name)], (1.0, True))

    def test_untouched_edges_do_not_appear(self):
        listed = {frozenset(endpoints) for endpoints, _, _ in self._crease_table()}
        for name in spec.SMOOTH_EDGES:
            with self.subTest(edge=name):
                edge = self.edge(name)
                self.assertNotIn(frozenset((edge.VertexId(0), edge.VertexId(1))),
                                 listed)


if __name__ == "__main__":
    unittest.main(verbosity=2)
