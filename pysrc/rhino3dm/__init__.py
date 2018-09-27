from _rhino3dm import *


Point3d.__str__ = lambda self: "{},{}".format(self.X, self.Y)
Point3d.__str__ = lambda self: "{},{},{}".format(self.X, self.Y, self.Z)
Vector2d.__str__ = lambda self: "{},{}".format(self.X, self.Y)
Vector3d.__str__ = lambda self: "{},{},{}".format(self.X, self.Y, self.Z)
