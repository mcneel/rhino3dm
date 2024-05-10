using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("Rhino3dm")]
[assembly: AssemblyDescription("Cross Platform Rhino dotnet SDK")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("Robert McNeel & Associates")]
[assembly: AssemblyProduct("Rhino")]
[assembly: AssemblyCopyright("Copyright © 2023")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]
// Brian Gillespie: 9/22/2010
// See http://msdn.microsoft.com/en-us/library/system.security.securityrulesattribute.aspx
// This is needed to get the AssemblyResolver to work... probably a bunch more:
// [assembly: System.Security.SecurityRules(System.Security.SecurityRuleSet.Level1)]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// Setting DefaultDllImportSearchPaths to AssemblyDirectory | UseDllDirectoryForDependencies | System32
// Ensures RhinoCommon will load its dependencies only from the Rhino\System folder or
// from Windwos\System32 folder but never will try from the Appliction folder, where the .exe is located.
// This is especially significant when Rhino is loaded as DLL
// 
// See also:
// https://docs.microsoft.com/windows/desktop/dlls/dynamic-link-library-search-order
// https://msdn.microsoft.com/library/system.runtime.interopservices.dllimportsearchpath
[assembly: DefaultDllImportSearchPaths(DllImportSearchPath.AssemblyDirectory | DllImportSearchPath.UseDllDirectoryForDependencies | DllImportSearchPath.System32)]


// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("e1abd154-3fbb-4901-b1b7-76202cafc6bf")]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version 
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Revision and Build Numbers 
// by using the '*' as shown below:
// Roll the version number when we don't want older Mono built plug-ins to run
// The autobuild used to tweak this number for every build and we stopped doing this
// with a build number in the mid-12000 range. Starting with build-15000, the Build
// number is manually set by Steve
// 10 Feb 2011 - updated to 15006 because exposed python classes have changed which
//               should break python on OSX
// 31 July 2012 (5.0.15006.1) Rolled revision to 1 in order to test plug-in versioning in Rhino 5
// 06 Aug 2012 (5.1.30000.0) Rolled minor to 1 and build number to 30000 since the autobuild
//                           was actually rolling version numbers
// 10 Aug 2012 (5.1.30000.1) Added document properties and object properties custom page support
//                           Added ObjectTable.Replace which takes a point cloud
//                           Added MeshFaceList.AddFaces
// 21 Aug 2012 (5.1.30000.2) Added RunScript version that takes a display string
//                           Added gumball and keyboard shortcut settings
// 23 Aug 2012 (5.1.30000.3) Added DrawForegroundEventArgs
//                           Added OnSpaceMorph virtual function for custom objects
// 29 Aug 2012 (5.1.30000.4) Adding support for history and history replay
//                           SR0 - Initial release of Rhino 5
// 12 Oct 2012 (5.1.30000.5) Update for SR1 of Rhino 5
// 21 Feb 2013 (5.1.30000.6) Update for SR2 of Rhino 5
// 21 Feb 2013 (5.1.30000.7) Update for SR3 of Rhino 5
// March 2013 Rhino V6 version = 6.0.0.1
// 29 Aug 2017 - switch to automated assembly versioning
// 2019-05-06 - use rhino3dm_version.cs
//#if RHINO3DMIO_BUILD
//[assembly: AssemblyVersion("6.0.0.1")]
//#endif

[assembly: AssemblyVersion("8.7.0.0")]

// 2013-12-19, Brian Gillespie
// AssemblyFileVersion is set in /src4/version.h
// and must change with each build, or the installer will fail to
// replace RhinoCommon.dll.
// [assembly: AssemblyFileVersion("6.0.0.1")]

#if MONO_BUILD && RHINO3DMIO_BUILD
//Mobile platform build has non-compliant classes
[assembly: System.CLSCompliant(false)]
#else
[assembly: System.CLSCompliant(true)]
#endif

// 23 April 2007 S. Baer (RR 25439)
// Plug-Ins that are being loaded from a network drive will throw security exceptions
// if they are not marked with the AllowPartiallyTrustedCallersAttribute. This assembly
// also requires that this attribute be set in order for things to work.

#if !RHINO_SDK && !MONO_BUILD
[assembly: System.Security.SecurityRules(System.Security.SecurityRuleSet.Level1)]
#endif

// 04 August 2016 Max S. RH-34981
// InternalsVisibleTo so that Rhino.UI can access internal classes in RhinoCommon
[assembly: InternalsVisibleTo("Rhino.UI, PublicKey=002400000480000094000000060200000024000052534131000400000100010083c66ae8bfbbea" +
                                                  "010a18559b1502c1b79e1fbb74b62ea03fec9bd46ec6fec5c1917c8a92c44f96a449f87cce288e" +
                                                  "341a3109b0528c9775fe5b46bfc85ecb90e75f265bef0700eb98176671b4ff9c7e74ac683ebe8d" +
                                                  "50cd4a1c4538d6bf94a7c7c48da9fee90327e273fbc0208c76f6782220d290dee6067981d33ea4" +
                                                  "a3b345cf")]

[assembly: InternalsVisibleTo("RDK_EtoUI, PublicKey=002400000480000094000000060200000024000052534131000400000100010083c66ae8bfbbea" +
                                                  "010a18559b1502c1b79e1fbb74b62ea03fec9bd46ec6fec5c1917c8a92c44f96a449f87cce288e" +
                                                  "341a3109b0528c9775fe5b46bfc85ecb90e75f265bef0700eb98176671b4ff9c7e74ac683ebe8d" +
                                                  "50cd4a1c4538d6bf94a7c7c48da9fee90327e273fbc0208c76f6782220d290dee6067981d33ea4" +
                                                  "a3b345cf")]

[assembly: InternalsVisibleTo("RhinoWindows, PublicKey=002400000480000094000000060200000024000052534131000400000100010083c66ae8bfbbea" +
                                                  "010a18559b1502c1b79e1fbb74b62ea03fec9bd46ec6fec5c1917c8a92c44f96a449f87cce288e" +
                                                  "341a3109b0528c9775fe5b46bfc85ecb90e75f265bef0700eb98176671b4ff9c7e74ac683ebe8d" +
                                                  "50cd4a1c4538d6bf94a7c7c48da9fee90327e273fbc0208c76f6782220d290dee6067981d33ea4" +
                                                  "a3b345cf")]

[assembly: InternalsVisibleTo ("RhinoMac, PublicKey=002400000480000094000000060200000024000052534131000400000100010083c66ae8bfbbea" +
                                                  "010a18559b1502c1b79e1fbb74b62ea03fec9bd46ec6fec5c1917c8a92c44f96a449f87cce288e" +
                                                  "341a3109b0528c9775fe5b46bfc85ecb90e75f265bef0700eb98176671b4ff9c7e74ac683ebe8d" +
                                                  "50cd4a1c4538d6bf94a7c7c48da9fee90327e273fbc0208c76f6782220d290dee6067981d33ea4" +
                                                  "a3b345cf")]

[assembly: InternalsVisibleTo("MeshCommands, PublicKey=002400000480000094000000060200000024000052534131000400000100010083c66ae8bfbbea" +
                                                  "010a18559b1502c1b79e1fbb74b62ea03fec9bd46ec6fec5c1917c8a92c44f96a449f87cce288e" +
                                                  "341a3109b0528c9775fe5b46bfc85ecb90e75f265bef0700eb98176671b4ff9c7e74ac683ebe8d" +
                                                  "50cd4a1c4538d6bf94a7c7c48da9fee90327e273fbc0208c76f6782220d290dee6067981d33ea4" +
                                                  "a3b345cf")]

[assembly: InternalsVisibleTo ("Commands, PublicKey=002400000480000094000000060200000024000052534131000400000100010083c66ae8bfbbea" +
                                                  "010a18559b1502c1b79e1fbb74b62ea03fec9bd46ec6fec5c1917c8a92c44f96a449f87cce288e" +
                                                  "341a3109b0528c9775fe5b46bfc85ecb90e75f265bef0700eb98176671b4ff9c7e74ac683ebe8d" +
                                                  "50cd4a1c4538d6bf94a7c7c48da9fee90327e273fbc0208c76f6782220d290dee6067981d33ea4" +
                                                  "a3b345cf")]

[assembly: InternalsVisibleTo("MeshUtilities, PublicKey=002400000480000094000000060200000024000052534131000400000100010083c66ae8bfbbea" +
                                                  "010a18559b1502c1b79e1fbb74b62ea03fec9bd46ec6fec5c1917c8a92c44f96a449f87cce288e" +
                                                  "341a3109b0528c9775fe5b46bfc85ecb90e75f265bef0700eb98176671b4ff9c7e74ac683ebe8d" +
                                                  "50cd4a1c4538d6bf94a7c7c48da9fee90327e273fbc0208c76f6782220d290dee6067981d33ea4" +
                                                  "a3b345cf")]

[assembly: InternalsVisibleTo("ConstraintsUI, PublicKey=002400000480000094000000060200000024000052534131000400000100010083c66ae8bfbbea" +
                                                  "010a18559b1502c1b79e1fbb74b62ea03fec9bd46ec6fec5c1917c8a92c44f96a449f87cce288e" +
                                                  "341a3109b0528c9775fe5b46bfc85ecb90e75f265bef0700eb98176671b4ff9c7e74ac683ebe8d" +
                                                  "50cd4a1c4538d6bf94a7c7c48da9fee90327e273fbc0208c76f6782220d290dee6067981d33ea4" +
                                                  "a3b345cf")]

[assembly: InternalsVisibleTo("Export_DAE, PublicKey=002400000480000094000000060200000024000052534131000400000100010083c66ae8bfbbea" +
                                                  "010a18559b1502c1b79e1fbb74b62ea03fec9bd46ec6fec5c1917c8a92c44f96a449f87cce288e" +
                                                  "341a3109b0528c9775fe5b46bfc85ecb90e75f265bef0700eb98176671b4ff9c7e74ac683ebe8d" +
                                                  "50cd4a1c4538d6bf94a7c7c48da9fee90327e273fbc0208c76f6782220d290dee6067981d33ea4" +
                                                  "a3b345cf")]

[assembly: InternalsVisibleTo ("RhinoiOS, PublicKey=002400000480000094000000060200000024000052534131000400000100010083c66ae8bfbbea" +
                                                  "010a18559b1502c1b79e1fbb74b62ea03fec9bd46ec6fec5c1917c8a92c44f96a449f87cce288e" +
                                                  "341a3109b0528c9775fe5b46bfc85ecb90e75f265bef0700eb98176671b4ff9c7e74ac683ebe8d" +
                                                  "50cd4a1c4538d6bf94a7c7c48da9fee90327e273fbc0208c76f6782220d290dee6067981d33ea4" +
                                                  "a3b345cf")]
