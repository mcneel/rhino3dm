#pragma warning disable 1591
#if RHINO_SDK
using System;
using System.Collections.Generic;
using Rhino.Runtime.InteropWrappers;

namespace Rhino.Render
{
  public class RenderPlugInList : List<RenderPlugInInfo>
  {
    public RenderPlugInList()
    {
      IntPtr pList = UnsafeNativeMethods.Rdk_RenderPlugInInfo_New();
      if (pList != IntPtr.Zero)
      {
        bool b = true;

        while (b)
        {
          using (var sh = new StringHolder())
          {
            IntPtr pString = sh.NonConstPointer();
            Guid uuid = new Guid();
            b = 1==UnsafeNativeMethods.Rdk_RenderPlugInInfo(pList, ref uuid, pString);
            
            if (b)
            {
              Add(new RenderPlugInInfo(uuid, sh.ToString()));
            }
          }
        }

        UnsafeNativeMethods.Rdk_RenderPlugInInfo_Delete(pList);
      }
    }

  }

  public class RenderPlugInInfo
  {
    internal RenderPlugInInfo(Guid plugInId, String name)//, Rhino.PlugIns.RenderPlugIn plugIn)
    {
      PlugInId = plugInId;
      Name = name;
      //_plugIn = plugIn;
    }

    //private Rhino.PlugIns.RenderPlugIn _plugIn;

    public string Name { get; set; }

    //internal Rhino.PlugIns.RenderPlugIn PlugIn
    //{
    //  get { return _plugIn; }
    //  set { _plugIn = value; }
    //}

    public Guid PlugInId { get; set; }
  }
}
#endif