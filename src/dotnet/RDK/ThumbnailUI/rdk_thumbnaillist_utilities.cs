#pragma warning disable 1591

using System;

namespace Rhino.UI.Controls.ThumbnailUI
{
    [Obsolete("ThumbnailViewModelFactory is obsolete", false)]  
    public class ThumbnailViewModelFactory : InternalRdkViewModelFactory
    {
        public ThumbnailViewModelFactory() // Constructor needed to be registered as exported type
        {

        }
        public override IntPtr Get(Guid id)
        {
            //if (typeof(RdkThumbnaillistViewModel).GUID == id)
            //{
            //    var sec = new RdkThumbnaillistViewModel() as IRdkViewModel;
            //    return sec.CppPointer;
            //}

            return IntPtr.Zero;
        }
    }
}
