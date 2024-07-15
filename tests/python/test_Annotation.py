import rhino3dm
import unittest

class TestAnnotation(unittest.TestCase):
    def test_readAnnotations(self):

        model = rhino3dm.File3dm.Read('../models/textEntities_r8.3dm')
        objects = model.Objects
        plainText = ["Hello World!", "Hello Cruel World!"]
        for obj in objects:
            geo = obj.Geometry
            #print(geo.ObjectType)
            #print(geo.PlainText)
            #print(geo.RichText)
            #print(geo.PlainTextWithFields)
            if not any(x in geo.PlainText for x in plainText):
                self.fail("Annotation plain text not or not correct")

        self.assertTrue(True)




if __name__ == '__main__':
    print("running tests")
    unittest.main()
    print("tests complete")