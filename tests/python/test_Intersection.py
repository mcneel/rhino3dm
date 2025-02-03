import rhino3dm
import unittest

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

        planeA = rhino3dm.Plane.WorldXY()
        planeB = rhino3dm.Plane.WorldYZ()
        planeC = rhino3dm.Plane.WorldZX()
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
        box = sphere.ToBrep().GetBoundingBox()

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
            






if __name__ == '__main__':
    print("running tests")
    unittest.main()
    print("tests complete")