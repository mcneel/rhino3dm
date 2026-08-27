"""Author tests/models/subd_creases.3dm - RUN THIS INSIDE RHINO.

The SubD tests for RH3DM-169/175/176/177/178 need a fixture whose creases and
edge sharpness are known exactly. rhino3dm cannot build one: its SubD bindings
are read-only, and nothing outside Rhino can set edge sharpness. So the fixture
is authored here, from the same tests/python/subd_fixture_spec.py the tests
assert against.

Rhino 8 or later (SubD.SetEdgeSharpness arrived in 8.36).

    1. open the Rhino ScriptEditor, or run _RunPythonScript
    2. run this file
    3. it writes tests/models/subd_creases.3dm and adds the SubD to the
       current document so you can look at it
    4. run report_subd_fixture.py next, to check the values the tests
       hard-code against what Rhino actually produced

What it builds: a 10-unit SubD box whose bottom ring and one vertical edge are
hard creases, whose other three verticals are soft creases (sharpness 1, 3, and
a tapered 0.5-to-2.5), and whose top ring is left ordinary smooth. Between them
those produce all four vertex tags - Corner, Crease, Dart, Smooth - which is
what makes the tag assertions in the tests worth anything.

In the viewport you should see: a creased bottom, one creased vertical edge (on
the -x/-y corner), and three verticals that pinch the surface progressively
harder as the sharpness rises.
"""

import os
import sys

import Rhino
import System
from System.Collections.Generic import List


#: Set this only if the script is being run from somewhere that leaves __file__
#: undefined, e.g. pasted into the editor. Otherwise leave it empty.
REPO_ROOT = ""

#: Add the authored SubD to the current document, for a look in the viewport.
ADD_TO_DOCUMENT = True


# ---------------------------------------------------------------------------
# Finding the repository and the spec
# ---------------------------------------------------------------------------

def repo_root():
    if REPO_ROOT:
        return REPO_ROOT
    try:
        here = os.path.dirname(os.path.abspath(__file__))
    except NameError:
        raise RuntimeError(
            "__file__ is not defined - set REPO_ROOT at the top of this script "
            "to the root of your rhino3dm checkout.")
    # tests/models/authoring -> tests/models -> tests -> repo
    return os.path.dirname(os.path.dirname(os.path.dirname(here)))


def load_spec():
    path = os.path.join(repo_root(), "tests", "python")
    if path not in sys.path:
        sys.path.insert(0, path)
    import subd_fixture_spec
    return subd_fixture_spec


spec = load_spec()


# ---------------------------------------------------------------------------
# Building the control net
# ---------------------------------------------------------------------------

#: The order vertices are added to the mesh. Only used to index the faces.
VERTEX_ORDER = ("A", "B", "C", "D", "E", "F", "G", "H")


def build_mesh():
    """The box mesh the SubD is created from, wound so normals face outwards."""
    mesh = Rhino.Geometry.Mesh()
    for name in VERTEX_ORDER:
        x, y, z = spec.VERTEX_POINTS[name]
        mesh.Vertices.Add(x, y, z)
    index = {name: i for i, name in enumerate(VERTEX_ORDER)}
    for loop in spec.FACE_LOOPS.values():
        a, b, c, d = (index[name] for name in loop)
        mesh.Faces.AddFace(a, b, c, d)
    mesh.Normals.ComputeNormals()
    mesh.Compact()
    if not mesh.IsValid:
        raise RuntimeError("the box mesh is not valid")
    if not mesh.IsClosed:
        raise RuntimeError("the box mesh is not closed - check FACE_LOOPS winding")
    return mesh


def build_subd(mesh):
    """A SubD with no creases and no corners, whatever the conversion defaults are."""
    options = Rhino.Geometry.SubDCreationOptions.Smooth
    subd = Rhino.Geometry.SubD.CreateFromMesh(mesh, options)
    if subd is None:
        raise RuntimeError("SubD.CreateFromMesh returned None")

    # Start from a known state rather than trusting the conversion: every edge
    # smooth, every vertex smooth. Anything the options did to corners or
    # interior creases is undone here.
    #
    # Resetting the *vertices* is not belt and braces. UpdateVertexTags calls
    # ON_SubDVertex::SuggestedVertexTag with the input-tag bias on, and that
    # bias keeps an existing Corner tag on a two-crease vertex instead of
    # demoting it to Crease. Leave a corner behind from the mesh conversion and
    # B, C and D would silently stay Corners.
    edges = List[Rhino.Geometry.SubDEdge]()
    for edge in subd.Edges:
        edges.Add(edge)
    subd.Edges.SetEdgeTags(edges, Rhino.Geometry.SubDEdgeTag.Smooth)

    vertices = List[Rhino.Geometry.SubDVertex]()
    for vertex in subd.Vertices:
        vertices.Add(vertex)
    subd.Vertices.SetVertexTags(vertices, Rhino.Geometry.SubDVertexTag.Smooth)

    subd.UpdateAllTagsAndSectorCoefficients()
    return subd


# ---------------------------------------------------------------------------
# Naming components, so the authored configuration is applied to the right ones
# ---------------------------------------------------------------------------

def vertex_name(vertex):
    p = vertex.ControlNetPoint
    return spec.vertex_name_at((p.X, p.Y, p.Z))


def edge_ends(edge):
    """(name at end 0, name at end 1) for a SubDEdge."""
    return (vertex_name(edge.VertexFrom), vertex_name(edge.VertexTo))


def edges_by_name(subd):
    """Canonical spec key -> SubDEdge, re-read from the SubD.

    Re-read after every mutation: editing a SubD can move its components, so a
    map built before a SetEdgeTags call is not safe to use after one.
    """
    found = {}
    for edge in subd.Edges:
        a, b = edge_ends(edge)
        if a is None or b is None:
            raise RuntimeError("an edge endpoint is off the authored control net")
        found[spec.edge_key(a, b)] = edge
    if len(found) != spec.SUBD["edge_count"]:
        raise RuntimeError("expected %d edges, found %d"
                           % (spec.SUBD["edge_count"], len(found)))
    return found


# ---------------------------------------------------------------------------
# Applying the authored configuration
# ---------------------------------------------------------------------------

def apply_creases(subd):
    """Tag the hard creases. Vertex tags follow from the update afterwards."""
    edges = edges_by_name(subd)
    creases = List[Rhino.Geometry.SubDEdge]()
    for name in spec.CREASE_EDGES:
        creases.Add(edges[spec.canon(name)])
    subd.Edges.SetEdgeTags(creases, Rhino.Geometry.SubDEdgeTag.Crease)
    subd.UpdateAllTagsAndSectorCoefficients()


def apply_sharpness(subd):
    """Give the soft-crease edges their sharpness, oriented to the right end.

    SubDEdgeSharpness(s0, s1) puts s0 at the edge's end 0, and which endpoint
    that is depends on how the SubD was built. The spec keys sharpness by vertex
    name instead, so the values are swapped here if the edge runs the other way.
    """
    edges = edges_by_name(subd)
    for name, by_vertex in spec.SHARP_EDGES.items():
        edge = edges[spec.canon(name)]
        at_end_0, at_end_1 = edge_ends(edge)
        sharpness = Rhino.Geometry.SubDEdgeSharpness(
            by_vertex[at_end_0], by_vertex[at_end_1])

        one = List[Rhino.Geometry.SubDEdge]()
        one.Add(edge)
        changed = subd.SetEdgeSharpness(one, sharpness, False)
        if changed != 1:
            raise RuntimeError(
                "SetEdgeSharpness changed %d edges for %s, expected 1" % (changed, name))
        # Re-read: the call above may have moved the components.
        edges = edges_by_name(subd)
    subd.UpdateAllTagsAndSectorCoefficients()


# ---------------------------------------------------------------------------
# Output
# ---------------------------------------------------------------------------

def fixture_path():
    return os.path.join(repo_root(), "tests", "models", spec.FIXTURE_NAME)


def write_fixture(subd):
    """Write the .3dm at the file version the rhino3dm tests can read."""
    path = fixture_path()
    model = Rhino.FileIO.File3dm()
    model.Settings.ModelUnitSystem = Rhino.UnitSystem.Millimeters
    if model.Objects.AddSubD(subd) == System.Guid.Empty:
        raise RuntimeError("File3dm.Objects.AddSubD failed")
    if not model.Write(path, spec.FILE_VERSION):
        raise RuntimeError("could not write %s" % path)
    return path


def summarise(subd):
    edges = edges_by_name(subd)
    print("")
    print("  edges")
    for name in sorted(edges):
        edge = edges[name]
        at_0, at_1 = edge_ends(edge)
        s0 = edge.EndSharpness(0, False)
        s1 = edge.EndSharpness(1, False)
        print("    %-3s %-7s sharpness %s=%.2f %s=%.2f%s"
              % (name, edge.Tag, at_0, s0, at_1, s1,
                 "  <- sharp" if edge.IsSharp else ""))
    print("")
    print("  vertices")
    for vertex in subd.Vertices:
        name = vertex_name(vertex)
        creases = sum(1 for e in vertex.Edges
                      if e.Tag == Rhino.Geometry.SubDEdgeTag.Crease)
        print("    %-3s %-7s %d crease edge(s)" % (name, vertex.Tag, creases))


def main():
    mesh = build_mesh()
    subd = build_subd(mesh)
    apply_creases(subd)
    apply_sharpness(subd)

    if subd.Faces.Count != spec.SUBD["face_count"]:
        raise RuntimeError("expected %d faces, got %d"
                           % (spec.SUBD["face_count"], subd.Faces.Count))
    if not subd.IsSolid:
        raise RuntimeError("the authored SubD is not solid")

    path = write_fixture(subd)
    print("wrote %s (3dm version %d)" % (path, spec.FILE_VERSION))
    summarise(subd)

    if ADD_TO_DOCUMENT:
        doc = Rhino.RhinoDoc.ActiveDoc
        doc.Objects.AddSubD(subd)
        doc.Views.Redraw()
        print("")
        print("added the SubD to the active document")

    print("")
    print("next: run report_subd_fixture.py to check this against the values the")
    print("      rhino3dm tests hard-code")


if __name__ == "__main__":
    main()
