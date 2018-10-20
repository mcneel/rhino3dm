# Sample to get bounding box of remove 3dm file
from rhino3dm import *
import requests # pip install requests
req = requests.get("https://files.mcneel.com/TEST/Rhino Logo.3dm")
model = File3dm.FromByteArray(req.content)
for i in range(len(model.Objects)):
    geometry = model.Objects[i].Geometry
    bbox = geometry.GetBoundingBox()
    print("{}, {}".format(bbox.Min, bbox.Max))