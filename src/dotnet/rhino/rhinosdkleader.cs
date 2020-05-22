#if RHINO_SDK

namespace Rhino.DocObjects
{
  /// <summary>
  /// Rhino Object that represents leader geometry and attributes
  /// </summary>
  public class LeaderObject : AnnotationObjectBase
  {
    internal LeaderObject(uint serialNumber) : base(serialNumber)
    { }

    /// <summary>
    /// Get the leader geometry for this object.
    /// </summary>
    /// <since>6.0</since>
    public Geometry.Leader LeaderGeometry
    {
      get
      {
        return Geometry as Geometry.Leader;
      }
    }

    internal override CommitGeometryChangesFunc GetCommitFunc()
    {
      return UnsafeNativeMethods.CRhinoV6LeaderObject_InternalCommitChanges;
    }
  }
}
#endif