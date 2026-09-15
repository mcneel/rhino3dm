"""RH3DM-178 - Wrap full SubD API.

Parent task for RH3DM-169/175/176/177. What is left for this file is the part
none of the children own: the SubD object itself, the nine component-iterator
instantiations, the two tag enums, and the lifetime guarantee that makes any of
it usable from a Blender importer that drops the File3dm once it has read it.

Run standalone:

    python test_SubD_RH3DM178_SubD.py -v

Values come from tests/python/subd_fixture_spec.py. See that file for what is
authored, what is arithmetic, and what is predicted from the ON_SubD docs.
"""

import gc
import os
import sys
import unittest

import rhino3dm

_HERE = os.path.dirname(os.path.abspath(__file__))
if _HERE not in sys.path:
    sys.path.insert(0, _HERE)

import subd_fixture_spec as spec  # noqa: E402
import subd_fixture as fixture  # noqa: E402


class TestEmptySubD(unittest.TestCase):
    """The default-constructed SubD, which needs no fixture."""

    def test_default_construction(self):
        subd = rhino3dm.SubD()
        self.assertEqual(subd.FaceCount, 0)
        self.assertEqual(subd.EdgeCount, 0)
        self.assertEqual(subd.VertexCount, 0)

    def test_empty_iterators_are_empty(self):
        subd = rhino3dm.SubD()
        for iterator in (subd.Faces, subd.Edges, subd.Vertices):
            self.assertEqual(len(iterator), 0)
            self.assertEqual(iterator.Count, 0)
            self.assertEqual(list(iterator), [])

    def test_tag_enums_are_exposed(self):
        # RH3DM-176/177 assert the values on real components; here we only check
        # that the enums themselves round-trip through the binding.
        self.assertEqual(len(fixture.EDGE_TAGS), 4)
        self.assertEqual(len(fixture.VERTEX_TAGS), 5)
        self.assertEqual(len(set(fixture.EDGE_TAGS.values())), 4)
        self.assertEqual(len(set(fixture.VERTEX_TAGS.values())), 5)


class TestSubDObject(fixture.SubDFixtureTestCase):

    def test_component_counts(self):
        self.assertEqual(self.subd.FaceCount, spec.SUBD["face_count"])
        self.assertEqual(self.subd.EdgeCount, spec.SUBD["edge_count"])
        self.assertEqual(self.subd.VertexCount, spec.SUBD["vertex_count"])

    def test_is_solid(self):
        self.assertEqual(self.subd.IsSolid, spec.SUBD["is_solid"])

    def test_sharp_edge_count(self):
        self.assertEqual(self.subd.SharpEdgeCount, spec.SUBD["sharp_edge_count"])

    def test_clear_evaluation_cache_keeps_the_subd_usable(self):
        # F is an ordinary smooth vertex, so its limit point really is computed
        # and really is cached.
        before = self.vertices["F"].SurfacePoint
        self.subd.ClearEvaluationCache()
        after = fixture.vertices_by_name(self.subd)["F"].SurfacePoint
        self.assertPointEqual(after, (before.X, before.Y, before.Z),
                              "SurfacePoint after ClearEvaluationCache")

    def test_update_all_tags_does_not_change_authored_tags(self):
        # The fixture is written already up to date, so re-running the update
        # must leave every tag where the authoring script left it.
        before = {name: v.Tag for name, v in self.vertices.items()}
        changed = self.subd.UpdateAllTagsAndSectorCoefficients()
        self.assertIsInstance(changed, int)
        after = {name: v.Tag for name, v in fixture.vertices_by_name(self.subd).items()}
        self.assertEqual(after, before)

    def test_subdivide(self):
        expected = spec.SUBD["subdivided_once"]
        subd = fixture.read_subd()  # mutating, so work on a fresh read
        self.assertTrue(subd.Subdivide(1), "SubD.Subdivide(1) returned False")
        self.assertEqual(subd.FaceCount, expected["face_count"])
        self.assertEqual(subd.EdgeCount, expected["edge_count"])
        self.assertEqual(subd.VertexCount, expected["vertex_count"])


class TestComponentIterators(fixture.SubDFixtureTestCase):
    """The nine BND_SubDComponentIterator instantiations.

    Three rooted on the SubD, and six rooted on a component. They differ in one
    visible way: ``[]`` on a SubD-rooted iterator looks a component up by Id,
    while on a component-rooted one it indexes by position around the component.
    """

    def subd_rooted(self):
        return (("Faces", self.subd.Faces, spec.SUBD["face_count"]),
                ("Edges", self.subd.Edges, spec.SUBD["edge_count"]),
                ("Vertices", self.subd.Vertices, spec.SUBD["vertex_count"]))

    def component_rooted(self):
        face = self.faces["front"]
        edge = self.edge("BF")
        vertex = self.vertices["A"]
        return (("Face.Edges", face.Edges, spec.FACE_EDGE_COUNT),
                ("Face.Vertices", face.Vertices, spec.FACE_VERTEX_COUNT),
                ("Edge.Faces", edge.Faces, spec.EDGE_FACE_COUNT),
                ("Edge.Vertices", edge.Vertices, spec.EDGE_VERTEX_COUNT),
                ("Vertex.Faces", vertex.Faces, spec.VERTEX_FACE_COUNT),
                ("Vertex.Edges", vertex.Edges, spec.VERTEX_EDGE_COUNT))

    def test_all_nine_iterators_report_their_count(self):
        cases = list(self.subd_rooted()) + list(self.component_rooted())
        self.assertEqual(len(cases), 9)
        for label, iterator, expected in cases:
            with self.subTest(iterator=label):
                self.assertEqual(iterator.Count, expected)
                self.assertEqual(len(iterator), expected)

    def test_all_nine_iterators_yield_every_component_once(self):
        for label, iterator, expected in list(self.subd_rooted()) + list(self.component_rooted()):
            with self.subTest(iterator=label):
                items = list(iterator)
                self.assertEqual(len(items), expected)
                # Distinct ids confirm nothing is skipped and no trailing null
                # wrapper is appended - the two ways the postfix ++ went wrong.
                self.assertEqual(len({c.Id for c in items}), expected)

    def test_iteration_starts_at_the_first_component(self):
        for label, iterator, _ in self.subd_rooted():
            with self.subTest(iterator=label):
                self.assertEqual(list(iterator)[0].Id, iterator.First().Id)

    def test_cursor_walks_first_to_last(self):
        # First/Next/Current/Last are the cursor the JS binding has to use, and
        # they have to agree with __iter__ on the same iterator.
        for label, iterator, expected in self.subd_rooted():
            with self.subTest(iterator=label):
                ids = [iterator.First().Id]
                self.assertEqual(iterator.Current().Id, ids[0])
                for _ in range(expected - 1):
                    ids.append(iterator.Next().Id)
                self.assertEqual(len(set(ids)), expected)
                self.assertEqual(iterator.Current().Id, ids[-1])
                self.assertEqual(iterator.Last().Id, ids[-1])

    def test_current_index_starts_at_zero(self):
        for label, iterator, _ in self.subd_rooted():
            with self.subTest(iterator=label):
                iterator.First()
                self.assertEqual(iterator.CurrentIndex, 0)
                iterator.Next()
                self.assertEqual(iterator.CurrentIndex, 1)

    def test_subd_rooted_lookup_is_by_id(self):
        for label, iterator, _ in self.subd_rooted():
            with self.subTest(iterator=label):
                for component in iterator:
                    self.assertEqual(iterator[component.Id].Id, component.Id)

    def test_component_rooted_lookup_is_by_position(self):
        for label, iterator, expected in self.component_rooted():
            with self.subTest(iterator=label):
                by_position = [iterator[i].Id for i in range(expected)]
                by_iteration = [c.Id for c in iterator]
                self.assertEqual(by_position, by_iteration)

    def test_id_and_index_alias_the_same_value(self):
        # Both are the component Id in the current binding; a Blender importer
        # keying on either has to get the same component back.
        for label, iterator, _ in self.subd_rooted():
            with self.subTest(iterator=label):
                for component in iterator:
                    self.assertEqual(component.Index, component.Id)


class TestComponentIdentity(fixture.SubDFixtureTestCase):

    def test_same_component_two_ways_is_equal(self):
        face = self.faces["front"]
        self.assertEqual(face, self.subd.Faces[face.Id])
        edge = face.Edges.First()
        self.assertEqual(edge, self.subd.Edges[edge.Id])
        vertex = face.Vertices.First()
        self.assertEqual(vertex, self.subd.Vertices[vertex.Id])

    def test_different_components_are_not_equal(self):
        self.assertNotEqual(self.faces["front"], self.faces["back"])
        self.assertNotEqual(self.edge("AB"), self.edge("BC"))
        self.assertNotEqual(self.vertices["A"], self.vertices["B"])

    def test_components_are_hashable_and_deduplicate(self):
        self.assertEqual(len(set(self.subd.Faces)), spec.SUBD["face_count"])
        self.assertEqual(len(set(self.subd.Edges)), spec.SUBD["edge_count"])
        self.assertEqual(len(set(self.subd.Vertices)), spec.SUBD["vertex_count"])

    def test_different_component_types_are_never_equal(self):
        self.assertNotEqual(self.faces["front"], self.edge("AB"))
        self.assertNotEqual(self.edge("AB"), self.vertices["A"])


class TestComponentLifetime(unittest.TestCase):
    """Components must outlive the File3dm they were read from.

    A Blender importer reads the model, pulls the geometry it wants, and lets
    the File3dm go. Components used to hold a raw pointer to a transient SubD,
    so doing that dangled; they now carry a refcounted handle instead.
    """

    def setUp(self):
        if fixture.fixture_path() is None:
            self.skipTest(fixture.MISSING_FIXTURE)

    def test_components_survive_their_model(self):
        model = rhino3dm.File3dm.Read(fixture.fixture_path())
        subd = model.Objects[0].Geometry
        vertices = fixture.vertices_by_name(subd)
        edges = fixture.edges_by_name(subd)
        vertex = vertices["A"]                      # from a temporary iterator
        edge = edges[spec.canon("DH")]
        face = subd.Faces.First().Edges.First().Faces.First()  # chain of temporaries
        expected_endpoints = {fixture.vertex_name(edge.Vertex(0)),
                              fixture.vertex_name(edge.Vertex(1))}

        del model, subd, vertices, edges
        gc.collect()

        self.assertEqual(vertex.Edges.Count, spec.VERTEX_EDGE_COUNT)
        self.assertEqual(vertex.Faces.Count, spec.VERTEX_FACE_COUNT)
        self.assertEqual({fixture.vertex_name(edge.Vertex(0)),
                          fixture.vertex_name(edge.Vertex(1))}, expected_endpoints)
        self.assertEqual(edge.EndSharpness(0) + edge.EndSharpness(1),
                         sum(spec.EDGES_BY_KEY[spec.canon("DH")]["sharpness"].values()))
        self.assertEqual(face.EdgeCount, spec.FACE_EDGE_COUNT)


if __name__ == "__main__":
    unittest.main(verbosity=2)
