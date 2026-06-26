import sys
if sys.version_info.major==2:
    from _rhino3dm import *
else:
    from ._rhino3dm import *

__version__ = '8.32.0'
