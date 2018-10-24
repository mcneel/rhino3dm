# rhino3dm.py
CPython package based on OpenNURBS with a RhinoCommon style

Project Hompage at: https://github.com/mcneel/rhino3dm

### Supported platforms
* Python27 - Windows (32 and 64 bit)
* Python37 - Windows (32 and 64 bit)
* Python27 - OSX (installed through homebrew)
* Python37 - OSX (installed through homebrew)
* Linux and other python versions are supported through source distributions\


## Test

* start `python`
```
>>> from rhino3dm import *
>>> import requests
>>> req = requests.get("https://files.mcneel.com/rhino3dm/models/RhinoLogo.3dm"")
>>> model = File3dm.FromByteArray(req.content)
>>> for i in range(len(model.Objects)):
>>>     geometry = model.Objects[i].Geometry
>>>     bbox = geometry.GetBoundingBox()
>>>     print("{}, {}".format(bbox.Min, bbox.Max))
```
