REM batch file used to compile all flavors for Windows.
REM Typically you would not use this file to compile and
REM instead just call python build_rhino3dm.py

set BASEPATH=%PATH%
set PATH=C:\Python27;%BASEPATH%
python build_rhino3dm.py
set PATH=C:\Python27_64bit;%BASEPATH%
python build_rhino3dm.py
set PATH="C:\Program Files\Python37";"C:\Users\Steve\AppData\Roaming\Python\Python37\Scripts";%BASEPATH%
python build_rhino3dm.py
set PATH="C:\Program Files (x86)\Python37-32";"c:\users\steve\appdata\roaming\python\python37\site-packages";%BASEPATH%
python build_rhino3dm.py

