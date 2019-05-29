#pragma warning disable 1591
#if RHINO_SDK
using System;

namespace Rhino.Render
{
  sealed class RdkContentLookup : FreeFloatingBase
  {
    internal RdkContentLookup(IntPtr p) : base(p) { }
    //internal RdkContentLookup(uint ds) : base(ds) { }

    public RenderContent LookUpContent(Guid uuidInstance)
    {
      if (CppPointer != IntPtr.Zero)
      {
        return RenderContent.FromPointer(UnsafeNativeMethods.IRhRdkContentLookup_LookUpContent(CppPointer, uuidInstance));
      }

      return null;
    }

    public override void CopyFrom(FreeFloatingBase src)
    {
      throw new NotImplementedException();
    }

    internal override IntPtr DefaultCppConstructor()
    {
      throw new NotImplementedException();
    }

    internal override void DeleteCpp()
    {
      throw new NotImplementedException();
    }
  }
}
#endif