# RhinoCommon

## Compiling the API docs

Requires Visual Studio 2017 and SHFB ([2017.12.30.0](http://ewsoftware.github.io/SHFB/html/8479cf1a-4f4f-4f0b-89f7-85a04cd78d16.htm) or later).

The Windows and macOS RhinoCommon.dll/xml files must be in `Release/` and `Release-mac/` respectively.

Compile, e.g...

`msbuild src4\DotNetSDK\rhinocommon\dotnet\helpdocs.shfbproj /p:MacVersion=5.4;BuildDate=2018-07-29T11:28:00;OfficialBuild=1;BUILD_TYPE=COMMERCIAL`
