import rhino3dm
import unittest

class TestAnnotation(unittest.TestCase):
    def test_readAnnotations(self):

        model = rhino3dm.File3dm.Read('../models/textEntities_r8.3dm')
        objects = model.Objects
        plainText = ["Hello World!", "Hello Cruel World!", "WTF"]
        for obj in objects:
            geo = obj.Geometry

            match obj.Geometry.ObjectType:
                case rhino3dm.ObjectType.Annotation:
                    if not any(x in geo.PlainText for x in plainText):
                        self.fail("Annotation plain text not or not correct")
                case rhino3dm.ObjectType.TextDot:
                    if not any(x in geo.Text for x in plainText):
                        self.fail("TextDot text not or not correct")





if __name__ == '__main__':
    print("running tests")
    unittest.main()
    print("tests complete")