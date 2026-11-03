import sys
if sys.version_info.major==2:
    from _rhino3dm import *
else:
    from ._rhino3dm import *

__version__ = '9.2.0b0'
