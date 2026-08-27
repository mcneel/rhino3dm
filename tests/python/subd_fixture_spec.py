"""Authored expectations for tests/models/subd_creases.3dm.

Single source of truth, shared by three consumers:

  * tests/models/authoring/make_subd_fixture.py   - builds the .3dm, inside Rhino
  * tests/models/authoring/report_subd_fixture.py - expected vs. actual, inside Rhino
  * tests/python/test_SubD_RH3DM*.py              - asserts the same values through rhino3dm

Plain data, no imports: it has to load inside Rhino (where rhino3dm is absent)
just as well as next to the rhino3dm tests.

The fixture is a SubD box - 6 quads, 12 edges, 8 vertices - whose crease and
sharpness configuration is chosen so that every value below is exactly
predictable. No float fudging, no "assertIsInstance(x, float)" placeholders.

        H---------G            z
       /|        /|            |   y
      E---------F |            |  /
      | |       | |            | /
      | D-------|-C            |/
      |/        |/             +------x
      A---------B

Vertex naming: A-D are the bottom (z=0) ring, E-H the top (z=SIZE) ring, both
counter-clockwise seen from +z, with E directly above A.

Why this particular configuration: it produces every SubD vertex tag and every
edge flavour the bindings expose, from one small closed SubD.

  * the bottom ring AB, BC, CD, DA is creased, so A-D become crease vertices;
  * the single vertical crease AE pushes A to three creases (a Corner) and
    leaves E with one (a Dart) - which is also the only dart crease edge;
  * the other three verticals are sharp (soft creases) with distinct
    sharpness, including one tapered edge whose two ends differ;
  * the top ring stays ordinary smooth.

Confidence markers, quoted per value in the tables below:

  AUTHORED   the value the fixture was built with. A mismatch means the
             authoring script or the .3dm is wrong, not the binding.
  EXACT      arithmetic, straight out of the opennurbs evaluator - no tolerance
             needed beyond float round-off. The source line is cited.
  PREDICTED  read off the ON_SubD documentation rather than computed. These are
             the ones to check in Rhino first; report_subd_fixture.py exists so
             they can be validated or invalidated without guesswork.
"""

FIXTURE_NAME = "subd_creases.3dm"

# The .3dm is written in Rhino 8 format so an 8.x rhino3dm build can read it.
FILE_VERSION = 8

SIZE = 10.0

# ---------------------------------------------------------------------------
# Control net
# ---------------------------------------------------------------------------

# AUTHORED. Control net points, and the vertex names used as keys throughout.
VERTEX_POINTS = {
    "A": (0.0,  0.0,  0.0),
    "B": (SIZE, 0.0,  0.0),
    "C": (SIZE, SIZE, 0.0),
    "D": (0.0,  SIZE, 0.0),
    "E": (0.0,  0.0,  SIZE),
    "F": (SIZE, 0.0,  SIZE),
    "G": (SIZE, SIZE, SIZE),
    "H": (0.0,  SIZE, SIZE),
}

# AUTHORED. Quad faces as ordered vertex loops, wound so the normal points out
# of the box (checked by hand; see report_subd_fixture.py for the machine check).
FACE_LOOPS = {
    "bottom": ("A", "D", "C", "B"),
    "top":    ("E", "F", "G", "H"),
    "front":  ("A", "B", "F", "E"),
    "right":  ("B", "C", "G", "F"),
    "back":   ("C", "D", "H", "G"),
    "left":   ("D", "A", "E", "H"),
}

# ---------------------------------------------------------------------------
# Authored crease / sharpness configuration
# ---------------------------------------------------------------------------

# AUTHORED. Edges are keyed by their unordered endpoint pair, so nothing here
# depends on the component ids or on which endpoint opennurbs calls "end 0".
CREASE_EDGES = ("AB", "BC", "CD", "DA", "AE")

# AUTHORED. Sharpness per *end vertex*, again to stay orientation-independent.
# "DH" is tapered: its two ends carry different sharpness.
SHARP_EDGES = {
    "BF": {"B": 1.0, "F": 1.0},
    "CG": {"C": 3.0, "G": 3.0},
    "DH": {"D": 0.5, "H": 2.5},
}

# AUTHORED. Ordinary smooth edges - no crease, no sharpness.
SMOOTH_EDGES = ("EF", "FG", "GH", "HE")

# ---------------------------------------------------------------------------
# SubD level
# ---------------------------------------------------------------------------

SUBD = {
    "face_count":   6,      # AUTHORED
    "edge_count":   12,     # AUTHORED
    "vertex_count": 8,      # AUTHORED
    "is_solid":     True,   # AUTHORED - the box is closed
    "crease_edge_count": 5,  # AUTHORED - AB BC CD DA AE
    "sharp_edge_count":  3,  # AUTHORED - BF CG DH
    "smooth_not_sharp_edge_count": 4,  # AUTHORED - EF FG GH HE
    # EXACT. One Catmull-Clark step on a cube: F' = sum of face edge counts,
    # V' = V + E + F, E' = 2E + sum of face edge counts.
    "subdivided_once": {"face_count": 24, "edge_count": 48, "vertex_count": 26},
}

# ---------------------------------------------------------------------------
# Per-edge expectations
# ---------------------------------------------------------------------------
#
#   tag             SubDEdgeTag name
#   is_hard_crease  EXACT: crease tag and both ends are crease-or-corner
#                   (ON_SubDEdge::IsHardCrease, opennurbs_subd.cpp:4639)
#   is_dart_crease  EXACT: crease tag and dart_count > 0 (same file, :4653)
#   dart_count      EXACT: number of end vertices tagged Dart (:4661)
#   is_sharp        EXACT: smooth tag, one end > 0 and one end < 4
#                   (ON_SubDEdge::IsSharp, opennurbs_subd.h:21153)
#   sharpness       AUTHORED, per end vertex. EndSharpness returns 0 for any
#                   edge that is not sharp, creases included.
#   subdivision_point
#                   EXACT for creases (midpoint, opennurbs_subd.cpp:16140) and
#                   for these sharp edges: a sharp edge whose average sharpness
#                   is >= 1 subdivides to its control net midpoint
#                   (GetSharpSubdivisionPoint :6346 + :16020). None means the
#                   ordinary Catmull-Clark blend - not hard-coded here.

def _mid(a, b):
    pa, pb = VERTEX_POINTS[a], VERTEX_POINTS[b]
    return ((pa[0] + pb[0]) / 2.0, (pa[1] + pb[1]) / 2.0, (pa[2] + pb[2]) / 2.0)


EDGES = {
    # bottom ring - plain hard creases between crease/corner vertices
    "AB": {"tag": "Crease", "is_smooth": False, "is_crease": True,
           "is_hard_crease": True, "is_dart_crease": False, "dart_count": 0,
           "is_sharp": False, "sharpness": {"A": 0.0, "B": 0.0},
           "subdivision_point": _mid("A", "B")},
    "BC": {"tag": "Crease", "is_smooth": False, "is_crease": True,
           "is_hard_crease": True, "is_dart_crease": False, "dart_count": 0,
           "is_sharp": False, "sharpness": {"B": 0.0, "C": 0.0},
           "subdivision_point": _mid("B", "C")},
    "CD": {"tag": "Crease", "is_smooth": False, "is_crease": True,
           "is_hard_crease": True, "is_dart_crease": False, "dart_count": 0,
           "is_sharp": False, "sharpness": {"C": 0.0, "D": 0.0},
           "subdivision_point": _mid("C", "D")},
    "DA": {"tag": "Crease", "is_smooth": False, "is_crease": True,
           "is_hard_crease": True, "is_dart_crease": False, "dart_count": 0,
           "is_sharp": False, "sharpness": {"D": 0.0, "A": 0.0},
           "subdivision_point": _mid("D", "A")},

    # the lone vertical crease: A is a Corner, E is a Dart, so this is the
    # fixture's only dart crease and the only crease that is not "hard".
    "AE": {"tag": "Crease", "is_smooth": False, "is_crease": True,
           "is_hard_crease": False, "is_dart_crease": True, "dart_count": 1,
           "is_sharp": False, "sharpness": {"A": 0.0, "E": 0.0},
           "subdivision_point": _mid("A", "E")},

    # soft creases - smooth tag, nonzero sharpness
    "BF": {"tag": "Smooth", "is_smooth": True, "is_crease": False,
           "is_hard_crease": False, "is_dart_crease": False, "dart_count": 0,
           "is_sharp": True, "sharpness": {"B": 1.0, "F": 1.0},
           "subdivision_point": _mid("B", "F")},
    "CG": {"tag": "Smooth", "is_smooth": True, "is_crease": False,
           "is_hard_crease": False, "is_dart_crease": False, "dart_count": 0,
           "is_sharp": True, "sharpness": {"C": 3.0, "G": 3.0},
           "subdivision_point": _mid("C", "G")},
    # tapered: 0.5 at the bottom, 2.5 at the top. Average 1.5 >= 1, so this one
    # still subdivides to the midpoint.
    "DH": {"tag": "Smooth", "is_smooth": True, "is_crease": False,
           "is_hard_crease": False, "is_dart_crease": False, "dart_count": 0,
           "is_sharp": True, "sharpness": {"D": 0.5, "H": 2.5},
           "subdivision_point": _mid("D", "H")},

    # top ring - ordinary smooth, so the subdivision point is the Catmull-Clark
    # blend and is deliberately not hard-coded.
    # EF and HE run into the dart vertex E. DartCount counts dart end vertices
    # whatever the edge's own tag is, so it is 1 here even though neither edge
    # is a crease - and IsDartCrease, which does test the tag, stays False.
    "EF": {"tag": "Smooth", "is_smooth": True, "is_crease": False,
           "is_hard_crease": False, "is_dart_crease": False, "dart_count": 1,
           "is_sharp": False, "sharpness": {"E": 0.0, "F": 0.0},
           "subdivision_point": None},
    "FG": {"tag": "Smooth", "is_smooth": True, "is_crease": False,
           "is_hard_crease": False, "is_dart_crease": False, "dart_count": 0,
           "is_sharp": False, "sharpness": {"F": 0.0, "G": 0.0},
           "subdivision_point": None},
    "GH": {"tag": "Smooth", "is_smooth": True, "is_crease": False,
           "is_hard_crease": False, "is_dart_crease": False, "dart_count": 0,
           "is_sharp": False, "sharpness": {"G": 0.0, "H": 0.0},
           "subdivision_point": None},
    "HE": {"tag": "Smooth", "is_smooth": True, "is_crease": False,
           "is_hard_crease": False, "is_dart_crease": False, "dart_count": 1,
           "is_sharp": False, "sharpness": {"H": 0.0, "E": 0.0},
           "subdivision_point": None},
}

# EXACT for every edge of a box: two endpoints, two faces, and the control net
# centre is the midpoint (ON_SubDEdge::ControlNetCenterPoint, :15980).
EDGE_VERTEX_COUNT = 2
EDGE_FACE_COUNT = 2

# ---------------------------------------------------------------------------
# Per-vertex expectations
# ---------------------------------------------------------------------------
#
#   tag               EXACT, from ON_SubDVertex::SuggestedVertexTag
#                     (opennurbs_subd.cpp:8268), which is what
#                     UpdateAllTagsAndSectorCoefficients assigns: on a closed
#                     SubD, 3+ crease edges is a Corner, exactly 2 a Crease,
#                     exactly 1 a Dart, and none a Smooth. The fixture never
#                     sets a vertex tag by hand - they all fall out of the five
#                     creased edges.
#   vertex_sharpness  PREDICTED from ON_SubDVertex::VertexSharpness
#                     (opennurbs_subd.h:19807): a *crease or dart* vertex needs
#                     one attached edge with positive end sharpness here, but a
#                     *smooth* vertex needs two or more. That is why F, G and H
#                     - smooth, each touching exactly one sharp edge - report
#                     0.0 even though IsSharp is True for them. A Corner is
#                     never sharp, so A is 0.0 as well.
#   is_sharp          PREDICTED from ON_SubDVertex::IsSharp (:19793): smooth,
#                     dart or crease, and attached to at least one sharp edge.
#   surface_point     EXACT only for the Corner, which interpolates its control
#                     net point. None elsewhere - the limit point of a smooth or
#                     crease vertex is not hard-coded.

VERTICES = {
    "A": {"tag": "Corner", "is_corner": True,  "is_crease": False, "is_dart": False,
          "is_smooth": False, "crease_edge_count": 3, "vertex_sharpness": 0.0,
          "is_sharp": False, "surface_point": VERTEX_POINTS["A"]},
    "B": {"tag": "Crease", "is_corner": False, "is_crease": True,  "is_dart": False,
          "is_smooth": False, "crease_edge_count": 2, "vertex_sharpness": 1.0,
          "is_sharp": True,  "surface_point": None},
    "C": {"tag": "Crease", "is_corner": False, "is_crease": True,  "is_dart": False,
          "is_smooth": False, "crease_edge_count": 2, "vertex_sharpness": 3.0,
          "is_sharp": True,  "surface_point": None},
    "D": {"tag": "Crease", "is_corner": False, "is_crease": True,  "is_dart": False,
          "is_smooth": False, "crease_edge_count": 2, "vertex_sharpness": 0.5,
          "is_sharp": True,  "surface_point": None},
    "E": {"tag": "Dart",   "is_corner": False, "is_crease": False, "is_dart": True,
          "is_smooth": False, "crease_edge_count": 1, "vertex_sharpness": 0.0,
          "is_sharp": False, "surface_point": None},
    "F": {"tag": "Smooth", "is_corner": False, "is_crease": False, "is_dart": False,
          "is_smooth": True, "crease_edge_count": 0, "vertex_sharpness": 0.0,
          "is_sharp": True,  "surface_point": None},
    "G": {"tag": "Smooth", "is_corner": False, "is_crease": False, "is_dart": False,
          "is_smooth": True, "crease_edge_count": 0, "vertex_sharpness": 0.0,
          "is_sharp": True,  "surface_point": None},
    "H": {"tag": "Smooth", "is_corner": False, "is_crease": False, "is_dart": False,
          "is_smooth": True, "crease_edge_count": 0, "vertex_sharpness": 0.0,
          "is_sharp": True,  "surface_point": None},
}

# EXACT: every corner of a box has three edges and three faces.
VERTEX_EDGE_COUNT = 3
VERTEX_FACE_COUNT = 3

# ---------------------------------------------------------------------------
# Per-face expectations
# ---------------------------------------------------------------------------
#
#   centre              EXACT. ON_SubDFace::ControlNetCenterPoint averages the
#                       control net points (opennurbs_subd_eval.cpp:1192), and
#                       for a quad SubdivisionPoint is the same average
#                       (opennurbs_subd.cpp:14525) - so both equal the centre.
#   normal              PREDICTED direction of ControlNetCenterNormal. The
#                       magnitude is not asserted, only that it is the outward
#                       axis; an inward normal means the fixture was wound the
#                       wrong way.
#   sharp_edge_count    EXACT, by counting this face's sharp edges.
#   max_edge_sharpness  EXACT. ON_SubDFace::MaximumEdgeSharpness is the largest
#                       sharpness on the boundary, creases counting as zero
#                       (opennurbs_subd.h:22556). For the tapered edge DH that
#                       is its larger end, 2.5.

FACES = {
    # loop            centre            normal        sharp edges   max sharpness
    "bottom": {"loop": FACE_LOOPS["bottom"], "centre": (5.0, 5.0, 0.0),
               "normal": (0.0, 0.0, -1.0), "sharp_edge_count": 0,
               "max_edge_sharpness": 0.0, "has_sharp_edges": False,
               "edges": ("AB", "BC", "CD", "DA")},
    "top":    {"loop": FACE_LOOPS["top"], "centre": (5.0, 5.0, SIZE),
               "normal": (0.0, 0.0, 1.0), "sharp_edge_count": 0,
               "max_edge_sharpness": 0.0, "has_sharp_edges": False,
               "edges": ("EF", "FG", "GH", "HE")},
    "front":  {"loop": FACE_LOOPS["front"], "centre": (5.0, 0.0, 5.0),
               "normal": (0.0, -1.0, 0.0), "sharp_edge_count": 1,
               "max_edge_sharpness": 1.0, "has_sharp_edges": True,
               "edges": ("AB", "BF", "EF", "AE")},
    "right":  {"loop": FACE_LOOPS["right"], "centre": (SIZE, 5.0, 5.0),
               "normal": (1.0, 0.0, 0.0), "sharp_edge_count": 2,
               "max_edge_sharpness": 3.0, "has_sharp_edges": True,
               "edges": ("BC", "CG", "FG", "BF")},
    "back":   {"loop": FACE_LOOPS["back"], "centre": (5.0, SIZE, 5.0),
               "normal": (0.0, 1.0, 0.0), "sharp_edge_count": 2,
               "max_edge_sharpness": 3.0, "has_sharp_edges": True,
               "edges": ("CD", "DH", "GH", "CG")},
    "left":   {"loop": FACE_LOOPS["left"], "centre": (0.0, 5.0, 5.0),
               "normal": (-1.0, 0.0, 0.0), "sharp_edge_count": 1,
               "max_edge_sharpness": 2.5, "has_sharp_edges": True,
               "edges": ("DA", "AE", "HE", "DH")},
}

# EXACT: every face is a planar convex quad.
FACE_EDGE_COUNT = 4
FACE_VERTEX_COUNT = 4

# ---------------------------------------------------------------------------
# Helpers shared by all three consumers
# ---------------------------------------------------------------------------

#: Distance under which two control net points are the same vertex. The control
#: net is authored at exact binary-representable coordinates, so this only has
#: to absorb 3dm round-tripping.
POINT_TOL = 1e-9

#: Tolerance for sharpness and for evaluated points.
VALUE_TOL = 1e-9


def edge_key(name_a, name_b):
    """Canonical key for the edge between two named vertices, either order."""
    return "".join(sorted((name_a, name_b)))


def canon(edge_name):
    """Canonical key for an edge written in reading order, e.g. "DA" -> "AD"."""
    return edge_key(edge_name[0], edge_name[1])


#: EDGES above is written in reading order ("DA", "HE"), so index it by the
#: canonical sorted key too.
EDGES_BY_KEY = {canon(k): v for k, v in EDGES.items()}

#: Faces keyed by their unordered vertex set, for lookup from a read SubD.
FACES_BY_KEY = {frozenset(f["loop"]): name for name, f in FACES.items()}


def vertex_name_at(point, tol=POINT_TOL):
    """Name of the control net vertex at ``point``, or None."""
    for name, p in VERTEX_POINTS.items():
        if (abs(p[0] - point[0]) <= tol and abs(p[1] - point[1]) <= tol
                and abs(p[2] - point[2]) <= tol):
            return name
    return None


def sharpness_at(edge_name, vertex_name):
    """Authored sharpness of ``edge_name`` at the end sitting on ``vertex_name``."""
    return EDGES_BY_KEY[canon(edge_name)]["sharpness"][vertex_name]
