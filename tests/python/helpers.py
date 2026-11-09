"""Shared helpers for the Python tests.

    from helpers import MODELS, model, curve, box_brep, quad_mesh, temp_dir, stored_path
"""

import os
import shutil
import tempfile

import rhino3dm

MODELS = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', 'models')


def model(name):
    """the path of a file in tests/models"""
    return os.path.join(MODELS, name)


def stored_path(path):
    """a full path as opennurbs stores it on this platform: with Windows separators on Windows"""
    return os.path.normpath(path) if os.name == 'nt' else path


def read_model(name):
    """a File3dm read from tests/models"""
    return rhino3dm.File3dm.Read(model(name))


def curve():
    """a degree-3 control-point curve through four points in the XY plane"""
    pts = [rhino3dm.Point3d(*xyz) for xyz in [(0, 0, 0), (1, 2, 0), (4, 1, 0), (6, -2, 0)]]
    return rhino3dm.Curve.CreateControlPointCurve(pts, 3)


def box_brep():
    """a 1 x 2 x 3 box brep at the origin"""
    return rhino3dm.Brep.CreateFromBox(rhino3dm.BoundingBox(0, 0, 0, 1, 2, 3))


def quad_mesh():
    """a mesh with one quad face on the unit square"""
    m = rhino3dm.Mesh()
    for xyz in [(0, 0, 0), (1, 0, 0), (1, 1, 0), (0, 1, 0)]:
        m.Vertices.Add(*[float(c) for c in xyz])
    m.Faces.AddFace(0, 1, 2, 3)
    return m


def first_geometry(f, cls):
    """the first object geometry of type `cls` in File3dm `f`, or None"""
    for o in f.Objects:
        g = o.Geometry
        if isinstance(g, cls):
            return g
    return None


class TempDir:
    """a temporary directory for a test that writes files; removed afterwards

        with TempDir() as d:
            path = os.path.join(d, 'out.3dm')
    """

    def __enter__(self):
        self.path = tempfile.mkdtemp(prefix='rhino3dm_test_')
        return self.path

    def __exit__(self, *exc):
        shutil.rmtree(self.path, ignore_errors=True)
        return False
