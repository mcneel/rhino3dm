// WARNING: This feature is deprecated. Use the "Native References" folder instead.
// Right-click on the "Native References" folder, select "Add Native Reference",
// and then select the static library or framework that you'd like to bind.
//
// Once you've added your static library or framework, right-click the library or
// framework and select "Properties" to change the LinkWith values.

#if __IOS__
using ObjCRuntime;
[assembly: LinkWith("librhino3dmio_native.a", LinkTarget.Simulator64 | LinkTarget.Arm64, ForceLoad = true, IsCxx = true)]
#endif

