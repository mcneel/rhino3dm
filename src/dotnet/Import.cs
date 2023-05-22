//
// good marshalling webpage
// http://msdn.microsoft.com/en-us/library/aa288468(VS.71).aspx
//
//

class Import
{
#if RHINO3DM_BUILD
  #if __IOS__
    public const string lib = "__Internal";
    public const string librdk = "__Internal";
  #else
    public const string lib = "librhino3dm_native";
    public const string librdk = "librhino3dm_native";
  #endif
#else // RhinoCommon build...
    // DO NOT add the ".dll, .dynlib, .so, ..." extension.
    // Each platform should be smart enough to figure out how
    // to append an extension to find the dynamic library
    public const string lib = "rhcommon_c";
    public const string librdk = "rhcommonrdk_c";
#endif
  private Import() { }
}
