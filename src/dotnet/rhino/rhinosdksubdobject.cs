#if RHINO_SDK
namespace Rhino.DocObjects
{
  /// <summary>
  /// Rhino object for SubD
  /// </summary>
  public class SubDObject : RhinoObject 
  {
    internal SubDObject(uint serialNumber)
      : base(serialNumber) { }

    internal override CommitGeometryChangesFunc GetCommitFunc()
    {
      return UnsafeNativeMethods.CRhinoSubDObject_InternalCommitChanges;
    }

  }
}
#endif
