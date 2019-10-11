import sys
if sys.version_info.major==2:
    from _rhino3dm import *
else:
    from ._rhino3dm import *

__version__ = '0.7.3'

Point2d.__str__ = lambda self: "{},{}".format(self.X, self.Y)
Point3d.__str__ = lambda self: "{},{},{}".format(self.X, self.Y, self.Z)
Vector2d.__str__ = lambda self: "{},{}".format(self.X, self.Y)
Vector3d.__str__ = lambda self: "{},{},{}".format(self.X, self.Y, self.Z)
