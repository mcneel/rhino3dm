import unittest, os, os.path, rhino3dm

class TestIO(unittest.TestCase):
    def __init__(self, path):
        super(TestIO, self).__init__("read3dm")
        self._path = path

    def read3dm(self):
        rc = rhino3dm.File3dm._TestRead(self._path)
        self.assertTrue(rc, self._path)


suite = unittest.TestSuite()
for (root, dirs, files) in os.walk('example_3dm_files_20181010'):
    for filename in files:
        if filename.lower().endswith('.3dm'):
            path = os.path.join(root, filename)
            suite.addTest(TestIO(path))

unittest.TextTestRunner(verbosity=2).run(suite)
