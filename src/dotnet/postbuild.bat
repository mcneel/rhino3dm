echo off

IF "GH_Debug" == "%1" GOTO gh_debug_copy
IF "GH_Release" == "%1" GOTO gh_release_copy

REM The existence of a file called 
REM ...\src4\DotNetSDK\rhinocommon\dotnet\postbuild_copy.yes
REM enables automatic copying of RhinoCommon.dll to
REM Rhino executable directories.

IF NOT EXIST ..\postbuild_copy.yes exit 0

@echo RhinoCommon postbuild.bat called with args: "%1"
rem use dir to figure out where this batch file is being called from
rem DIR

IF "Debug" == "%1" GOTO debug_copy
IF "Release" == "%1" GOTO release_copy

GOTO done

REM =============================================================

:debug_copy

echo --- Debug build copies -----------------------------------------

set TARGETDIR=..\..\..\..\rhino4\Debug\
if NOT exist %TARGETDIR%Rhino.exe GOTO after_debug_win32
echo copy RhinoCommon output to %TARGETDIR%
REM The asterisk in the source filename is there so copy prints filename in output window
copy RhinoCommon.*dll %TARGETDIR%
copy RhinoCommon.*pdb %TARGETDIR%

:after_debug_win32

set TARGETDIR=..\..\..\..\rhino4\x64\Debug\
if NOT exist %TARGETDIR%Rhino.exe GOTO after_debug_x64
echo copy RhinoCommon output to %TARGETDIR%
REM The asterisk in the source filename is there so copy prints filename in output window
copy RhinoCommon.*dll %TARGETDIR%
copy RhinoCommon.*pdb %TARGETDIR%

:after_debug_x64

GOTO done

REM =============================================================

:release_copy

echo ---Release build copies -----------------------------------------

set TARGETDIR=..\..\..\..\rhino4\Release\
if NOT exist %TARGETDIR%Rhino.exe GOTO after_release_win32
echo copy RhinoCommon output to %TARGETDIR%
REM The asterisk in the source filename is there so copy prints filename in output window
copy RhinoCommon.*dll %TARGETDIR%
copy RhinoCommon.*pdb %TARGETDIR%

:after_release_win32

set TARGETDIR=..\..\..\..\rhino4\x64\Release\
if NOT exist %TARGETDIR%Rhino.exe GOTO after_release_x64
echo copy RhinoCommon output to %TARGETDIR%
REM The asterisk in the source filename is there so copy prints filename in output window
copy RhinoCommon.*dll %TARGETDIR%
copy RhinoCommon.*pdb %TARGETDIR%

:after_release_x64

GOTO done

REM =============================================================


REM --- GrassHopper stuff below -----------------------------------


:gh_debug_copy
@echo performing grasshopper debug copy...
copy RhinoCommon.dll ..\..\..\Bin\rh_common\RhinoCommon.dll
copy RhinoCommon.xml ..\..\..\Bin\rh_common\RhinoCommon.xml
copy RhinoCommon.pdb ..\..\..\Bin\rh_common\RhinoCommon.pdb
GOTO done

:gh_release_copy
@echo performing grasshopper release copy...
copy RhinoCommon.dll ..\..\..\Bin\rh_common\RhinoCommon.dll
copy RhinoCommon.xml ..\..\..\Bin\rh_common\RhinoCommon.xml
copy RhinoCommon.pdb ..\..\..\Bin\rh_common\RhinoCommon.pdb
GOTO done

:done
exit 0

