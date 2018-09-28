import struct

bitness = 8 * struct.calcsize("P")
if bitness==32:
    from _rhino3dm_win32 import *
else:
    from _rhino3dm_win64 import *



Point3d.__str__ = lambda self: "{},{}".format(self.X, self.Y)
Point3d.__str__ = lambda self: "{},{},{}".format(self.X, self.Y, self.Z)
Vector2d.__str__ = lambda self: "{},{}".format(self.X, self.Y)
Vector3d.__str__ = lambda self: "{},{},{}".format(self.X, self.Y, self.Z)
