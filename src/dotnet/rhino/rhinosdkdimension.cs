#if RHINO_SDK

namespace Rhino.DocObjects
{
  /// <summary>
  /// Rhino Object that represents a linear dimension geometry and attributes
  /// </summary>
  public class LinearDimensionObject : DimensionObject
  {
    internal LinearDimensionObject(uint serialNumber) : base(serialNumber)
    { }

    /// <summary>
    /// Get the dimension geometry for this object.
    /// </summary>
    /// <since>6.0</since>
    public Geometry.LinearDimension LinearDimensionGeometry => Geometry as Geometry.LinearDimension;

    internal override CommitGeometryChangesFunc GetCommitFunc()
    {
      return UnsafeNativeMethods.CRhinoV6LinearDimensionObject_InternalCommitChanges;
    }
  }

  /// <summary>
  /// Rhino Object that represents an angular dimension geometry and attributes
  /// </summary>
  public class AngularDimensionObject : DimensionObject
  {
    internal AngularDimensionObject(uint serialNumber) : base(serialNumber)
    { }

    /// <summary>
    /// Get the dimension geometry for this object.
    /// </summary>
    /// <since>6.0</since>
    public Geometry.AngularDimension AngularDimensionGeometry => Geometry as Geometry.AngularDimension;

    internal override CommitGeometryChangesFunc GetCommitFunc()
    {
      return UnsafeNativeMethods.CRhinoV6AngularDimensionObject_InternalCommitChanges;
    }
  }

  /// <summary>
  /// Rhino Object that represents a radial dimension geometry and attributes
  /// </summary>
  public class RadialDimensionObject : DimensionObject
  {
    internal RadialDimensionObject(uint serialNumber) : base(serialNumber)
    { }

    /// <summary>
    /// Get the dimension geometry for this object.
    /// </summary>
    /// <since>6.0</since>
    public Geometry.RadialDimension RadialDimensionGeometry => Geometry as Geometry.RadialDimension;

    internal override CommitGeometryChangesFunc GetCommitFunc()
    {
      return UnsafeNativeMethods.CRhinoV6RadialDimensionObject_InternalCommitChanges;
    }
  }

  /// <summary>
  /// Rhino Object that represents an ordinate dimension geometry and attributes
  /// </summary>
  public class OrdinateDimensionObject : DimensionObject
  {
    internal OrdinateDimensionObject(uint serialNumber) : base(serialNumber)
    { }

    /// <summary>
    /// Get the dimension geometry for this object.
    /// </summary>
    /// <since>6.0</since>
    public Geometry.OrdinateDimension OrdinateDimensionGeometry => Geometry as Geometry.OrdinateDimension;

    internal override CommitGeometryChangesFunc GetCommitFunc()
    {
      return UnsafeNativeMethods.CRhinoV6OrdinateDimensionObject_InternalCommitChanges;
    }
  }

  /// <summary>
  /// Rhino Object that represents a centermark geometry and attributes
  /// </summary>
  public class CentermarkObject : DimensionObject
  {
    internal CentermarkObject(uint serialNumber) : base(serialNumber)
    { }

    /// <summary>
    /// Get the dimension geometry for this object.
    /// </summary>
    /// <since>6.0</since>
    public Geometry.Centermark CentermarkGeometry => Geometry as Geometry.Centermark;

    internal override CommitGeometryChangesFunc GetCommitFunc()
    {
      return UnsafeNativeMethods.CRhinoV6CentermarkObject_InternalCommitChanges;
    }
  }
}
#endif