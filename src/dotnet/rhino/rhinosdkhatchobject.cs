#pragma warning disable 1591
using System;
using Rhino.Geometry;

#if RHINO_SDK
namespace Rhino.DocObjects
{
  public class HatchObject : RhinoObject
  {
    internal HatchObject(uint serialNumber)
      : base(serialNumber) { }

    internal override CommitGeometryChangesFunc GetCommitFunc()
    {
      return UnsafeNativeMethods.CRhinoHatch_InternalCommitChanges;
    }

    /// <example>
    /// <code source='examples\vbnet\ex_replacehatchpattern.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_replacehatchpattern.cs' lang='cs'/>
    /// <code source='examples\py\ex_replacehatchpattern.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    public Hatch HatchGeometry
    {
      get { return Geometry as Hatch; }
    }

  }
}
#endif
