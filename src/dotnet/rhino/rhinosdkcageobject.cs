//public class WireframeObject : RhinoObject { }
//public class CageObject : WireframeObject { }


#if RHINO_SDK
namespace Rhino.DocObjects
{
  /// <summary>
  /// Represents a <see cref="Rhino.Geometry.MorphControl">MorphControl</see> in a document.
  /// </summary>
  public class MorphControlObject : RhinoObject
  {
    internal MorphControlObject(uint serialNumber)
      : base(serialNumber) { }

    //internal override CommitGeometryChangesFunc GetCommitFunc()
    //{
    //  return UnsafeNativeMethods.CRhinoMorphControlObject_InternalCommitChanges;
    //}
  }
}
#endif