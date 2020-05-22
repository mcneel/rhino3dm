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

    /// <summary>
    /// Replaces a hatch object's underlying hatch geometry. This only works for non-document hatch objects.
    /// </summary>
    /// <param name="hatch">The replacement hatch geometry.</param>
    /// <returns>true if successful, false otherwise.</returns>
    /// <since>7.0</since>
    public bool SetHatchGeometry(Hatch hatch)
    {
      if (null == hatch || !hatch.IsValid) return false;
      if (null != Document) return false; // only works with non-document objects
      IntPtr ptr_this = NonConstPointer();
      IntPtr ptr_const_hatch = hatch.ConstPointer();
      return UnsafeNativeMethods.CRhinoHatch_SetHatch(ptr_this, ptr_const_hatch);
    }
  }
}
#endif
