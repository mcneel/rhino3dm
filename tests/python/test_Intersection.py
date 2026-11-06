import rhino3dm
import unittest
import os
import sys
import helpers


def _intersection_line(a, b):
    return rhino3dm.Line(rhino3dm.Point3d(*a), rhino3dm.Point3d(*b))

#objective:


class TestIntersection(unittest.TestCase):
    def test_intersectLineLineResults(self):

        lineA = rhino3dm.LineCurve(rhino3dm.Point3d(0, 0, 0), rhino3dm.Point3d(10, 10, 0)).Line
        lineB = rhino3dm.LineCurve(rhino3dm.Point3d(10, 0, 0), rhino3dm.Point3d(0, 10, 0)).Line

        with self.subTest(msg="LineLine"):
            resultLineLine = rhino3dm.Intersection.LineLine(lineA, lineB)

            self.assertTrue( len(resultLineLine) == 3 )
            self.assertTrue( type(resultLineLine) == tuple )
            self.assertTrue( type(resultLineLine[0]) == bool )
            self.assertTrue( type(resultLineLine[1]) == float )
            self.assertTrue( type(resultLineLine[2]) == float )
            self.assertTrue( resultLineLine[0] == True )
            self.assertTrue( resultLineLine[1] == 0.5 )
            self.assertTrue( resultLineLine[2] == 0.5 )

#TODO: Why does ON_IntersectLineLine always fail? 
        with self.subTest(msg="LineLine2"):   
            resultLineLine2 = rhino3dm.Intersection.LineLine(lineA, lineA, 0.0, False)

            self.assertTrue( len(resultLineLine2) == 3 )
            self.assertTrue( type(resultLineLine2) == tuple )
            self.assertTrue( type(resultLineLine2[0]) == bool )
            self.assertTrue( type(resultLineLine2[1]) == float )
            self.assertTrue( type(resultLineLine2[2]) == float )
            self.assertFalse( resultLineLine2[0] == True )
            self.assertFalse( resultLineLine2[1] == 0.5 )
            self.assertFalse( resultLineLine2[2] == 0.5 )

    def test_intersectPlaneResults(self):

        planeA = rhino3dm.Plane.WorldXY
        planeB = rhino3dm.Plane.WorldYZ
        planeC = rhino3dm.Plane.WorldZX
        line = rhino3dm.LineCurve(rhino3dm.Point3d(0, 0, -10), rhino3dm.Point3d(10, 10, 10)).Line
        sphere = rhino3dm.Sphere(rhino3dm.Point3d(0, 0, 0), 5)

        with self.subTest(msg="LinePlane"):
            resultLinePlane = rhino3dm.Intersection.LinePlane(line, planeA)

            self.assertTrue( len(resultLinePlane) == 2 )
            self.assertTrue( type(resultLinePlane) == tuple )
            self.assertTrue( type(resultLinePlane[0]) == bool )
            self.assertTrue( type(resultLinePlane[1]) == float )
            self.assertTrue( resultLinePlane[0] == True )
            self.assertTrue( resultLinePlane[1] == 0.5)


        with self.subTest(msg="PlanePlane"):
            resultPlanePlane = rhino3dm.Intersection.PlanePlane(planeA, planeB)

            self.assertTrue( len(resultPlanePlane) == 2 )
            self.assertTrue( type(resultPlanePlane) == tuple )
            self.assertTrue( type(resultPlanePlane[0]) == bool )
            self.assertTrue( type(resultPlanePlane[1]) == rhino3dm.Line )
            self.assertTrue( resultPlanePlane[0] == True )
            self.assertTrue( resultPlanePlane[1].IsValid )

        with self.subTest(msg="SpherePlane"):
            resultPlaneSphere = rhino3dm.Intersection.PlaneSphere(planeA, sphere)

            self.assertTrue( len(resultPlaneSphere) == 2 )
            self.assertTrue( type(resultPlaneSphere) == tuple )
            self.assertTrue( type(resultPlaneSphere[0]) == rhino3dm.PlaneSphereIntersection ) 
            self.assertTrue( type(resultPlaneSphere[1]) == rhino3dm.Circle )
            self.assertTrue( resultPlaneSphere[0] == rhino3dm.PlaneSphereIntersection.Circle ) #PlaneSphereIntersection enum. Circle = 2
            self.assertTrue( resultPlaneSphere[1].IsValid )

        with self.subTest(msg="PlanePlanePlane"):
            resultPlanePlanePlane = rhino3dm.Intersection.PlanePlanePlane(planeA, planeB, planeC)

            self.assertTrue( len(resultPlanePlanePlane) == 2 )
            self.assertTrue( type(resultPlanePlanePlane) == tuple )
            self.assertTrue( type(resultPlanePlanePlane[0]) == bool )
            self.assertTrue( type(resultPlanePlanePlane[1]) == rhino3dm.Point3d )
            self.assertTrue( resultPlanePlanePlane[0] == True )
            self.assertTrue( resultPlanePlanePlane[1] == rhino3dm.Point3d(0, 0, 0) )

    def test_intersectLineResults(self):

        line = rhino3dm.LineCurve(rhino3dm.Point3d(0, 0, 0), rhino3dm.Point3d(10, 10, 0)).Line
        circle = rhino3dm.Circle(rhino3dm.Point3d(0, 0, 0), 5)
        sphere = rhino3dm.Sphere(rhino3dm.Point3d(0, 0, 0), 5)
        cylinder = rhino3dm.Cylinder(circle, 5)
        box = sphere.ToBrep().GetBoundingBox(False)

        with self.subTest(msg="LineCircle"):
            resultLineCircle = rhino3dm.Intersection.LineCircle(line, circle)

            self.assertTrue( len(resultLineCircle) == 5 )
            self.assertTrue( type(resultLineCircle) == tuple )
            self.assertTrue( type(resultLineCircle[0]) == rhino3dm.LineCircleIntersection )
            self.assertTrue( type(resultLineCircle[1]) == float )
            self.assertTrue( type(resultLineCircle[2]) == rhino3dm.Point3d )
            self.assertTrue( type(resultLineCircle[3]) == float )
            self.assertTrue( type(resultLineCircle[2]) == rhino3dm.Point3d )

            self.assertTrue( resultLineCircle[0] == rhino3dm.LineCircleIntersection.Multiple )

        with self.subTest(msg="LineSphere"):
            resultLineSphere = rhino3dm.Intersection.LineSphere(line, sphere)

            self.assertTrue( len(resultLineSphere) == 3 )
            self.assertTrue( type(resultLineSphere) == tuple )
            self.assertTrue( type(resultLineSphere[0]) == rhino3dm.LineSphereIntersection )
            self.assertTrue( type(resultLineSphere[1]) == rhino3dm.Point3d )
            self.assertTrue( type(resultLineSphere[1]) == rhino3dm.Point3d )

            self.assertTrue( resultLineSphere[0] == rhino3dm.LineSphereIntersection.Multiple )

        with self.subTest(msg="LineCylinder"):
            resultLineCylinder = rhino3dm.Intersection.LineCylinder(line, cylinder)

            self.assertTrue( len(resultLineCylinder) == 3 )
            self.assertTrue( type(resultLineCylinder) == tuple )
            self.assertTrue( type(resultLineCylinder[0]) == rhino3dm.LineCylinderIntersection )
            self.assertTrue( type(resultLineCylinder[1]) == rhino3dm.Point3d )
            self.assertTrue( type(resultLineCylinder[1]) == rhino3dm.Point3d )

            self.assertTrue( resultLineCylinder[0] == rhino3dm.LineCylinderIntersection.Multiple )

        with self.subTest(msg="LineBox"):
            resultLineBox = rhino3dm.Intersection.LineBox(line, box, 0.01)

            self.assertTrue( len(resultLineBox) == 2 )
            self.assertTrue( type(resultLineBox) == tuple )
            self.assertTrue( type(resultLineBox[0]) == bool )
            self.assertTrue( type(resultLineBox[1]) == rhino3dm.Interval)

            self.assertTrue( resultLineBox[0] == True)

    def test_intersectSphereSphere(self):
        
        sphereA = rhino3dm.Sphere(rhino3dm.Point3d(0, 0, 0), 5)
        sphereB = rhino3dm.Sphere(rhino3dm.Point3d(2.5, 0, 0), 5)

        with self.subTest(msg="SphereSphere"):
            resultSphereSphere = rhino3dm.Intersection.SphereSphere(sphereA, sphereB)

            self.assertTrue( len(resultSphereSphere) == 2 )
            self.assertTrue( type(resultSphereSphere) == tuple )
            self.assertTrue( type(resultSphereSphere[0]) == rhino3dm.SphereSphereIntersection )
            self.assertTrue( type(resultSphereSphere[1]) == rhino3dm.Circle )

            self.assertTrue( resultSphereSphere[0] == rhino3dm.SphereSphereIntersection.Circle )
            self.assertTrue( resultSphereSphere[1].IsValid )

    def test_intersectionCircleCircleReturnsTheKindFirst(self):
        """Intersection.CircleCircle returns (kind, point1, point2); two overlapping circles meet at Multiple (D13)."""
        a = rhino3dm.Circle(rhino3dm.Point3d(0, 0, 0), 1.0)
        b = rhino3dm.Circle(rhino3dm.Point3d(1, 0, 0), 1.0)
        res = rhino3dm.Intersection.CircleCircle(a, b)
        self.assertEqual(len(res), 3)
        self.assertIsInstance(res[0], rhino3dm.CircleCircleIntersection)
        self.assertEqual(int(res[0]), 2)    # Multiple

    def test_intersectionLineCircleReturnsTheIntersectionTuple(self):
        """LineCircle returns (type, t1, point1, t2, point2): Multiple for a secant, None with zeros for an invalid line."""
        circle = rhino3dm.Circle(rhino3dm.Point3d(0, 0, 0), 1.0)
        rc = rhino3dm.Intersection.LineCircle(rhino3dm.Line(rhino3dm.Point3d(-2, 0, 0), rhino3dm.Point3d(2, 0, 0)), circle)
        self.assertEqual(len(rc), 5)
        self.assertEqual(int(rc[0]), 2)                      # Multiple
        bad = rhino3dm.Intersection.LineCircle(rhino3dm.Line(rhino3dm.Point3d(0, 0, 0), rhino3dm.Point3d(0, 0, 0)), circle)
        self.assertEqual(int(bad[0]), 0)                     # None
        self.assertEqual(bad[1], 0.0)
        self.assertEqual(bad[3], 0.0)

    def test_intersectionSphereSphereReturnsTheTypeAndCircle(self):
        """SphereSphere returns (Circle, circle) for overlapping spheres and None for spheres apart."""
        a = rhino3dm.Sphere(rhino3dm.Point3d(0, 0, 0), 1.0)
        b = rhino3dm.Sphere(rhino3dm.Point3d(1, 0, 0), 1.0)
        ss = rhino3dm.Intersection.SphereSphere(a, b)
        self.assertEqual(len(ss), 2)
        self.assertEqual(int(ss[0]), 2)                      # Circle
        self.assertIsInstance(ss[1], rhino3dm.Circle)
        far = rhino3dm.Intersection.SphereSphere(a, rhino3dm.Sphere(rhino3dm.Point3d(10, 0, 0), 1.0))
        self.assertEqual(int(far[0]), 0)                     # None

    def test_intersectionLineLineWithToleranceAndFiniteSegments(self):
        """LineLine(a, b, tolerance, finiteSegments) returns (ok, a, b), clamping to the segments when finite."""
        # each case's (ok, a, b) for (tolerance, finite) = (0, False), (0.001, True), (5, True)
        cases = [((0, 0, 0), (4, 0, 0), (2, -1, 0), (2, 3, 0), [(True, 0.5, 0.25)] * 3),          # crossing inside both
                 ((0, 0, 0), (1, 0, 0), (3, -1, 0), (3, 1, 0),
                  [(True, 3.0, 0.5), (False, 1.0, 0.5), (True, 1.0, 0.5)]),                        # past the end of the first
                 ((0, 0, 0), (1, 0, 0), (0, 0, 1), (1, 0, 1), [(False, 0.0, 0.0)] * 3)]           # parallel
        for a0, a1, b0, b1, expected in cases:
            for (tol, finite), want in zip([(0.0, False), (0.001, True), (5.0, True)], expected):
                got = rhino3dm.Intersection.LineLine(_intersection_line(a0, a1), _intersection_line(b0, b1), tol, finite)
                self.assertEqual(tuple(got), want)

    def test_intersectionPlaneCircleSecant(self):
        """PlaneCircle of a vertical circle and the XY plane is a secant at the circle's two crossings."""
        circle = rhino3dm.Circle(rhino3dm.Plane.WorldZX, 2.0)
        kind, t0, t1 = rhino3dm.Intersection.PlaneCircle(rhino3dm.Plane.WorldXY, circle)
        self.assertEqual(kind, rhino3dm.PlaneCircleIntersection.Secant)
        self.assertEqual(sorted(round(circle.PointAt(t).X, 9) for t in (t0, t1)), [-2.0, 2.0])

    def test_intersectionPlaneCircleCoincident(self):
        """PlaneCircle of a circle in the plane is Coincident, with unset parameters."""
        kind, t0, t1 = rhino3dm.Intersection.PlaneCircle(rhino3dm.Plane.WorldXY, rhino3dm.Circle(rhino3dm.Plane.WorldXY, 2.0))
        self.assertEqual(kind, rhino3dm.PlaneCircleIntersection.Coincident)
        self.assertEqual(t0, t1)
        self.assertLess(t0, -1e300)                        # RhinoMath.UnsetValue

if __name__ == '__main__':
    print("running tests")
    unittest.main()
    print("tests complete")
