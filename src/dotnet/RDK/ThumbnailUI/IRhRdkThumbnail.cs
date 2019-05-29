#pragma warning disable 1591

using System;
using System.Drawing;

namespace Rhino.UI.Controls.ThumbnailUI
{
  [Obsolete("IRhRdkThumbnail is obsolete", false)]
  // C# version of IRhRdkThumbnail
  public interface IRhRdkThumbnail
  {
    // return the thumbnail's unique id.
    //virtual UUID Id(void) const = 0;

    // return the thumbnail's label.
    //virtual ON_wString Label(void) const = 0;

    // return \e true if the thumbnail is selected. 
    //virtual bool IsSelected(void) const = 0;

    // return \e true if the thumbnail is 'hot' (i.e., the mouse is over it). 
    //virtual bool IsHot(void) const = 0;

    // Get the thumbnail's imagery.
    //virtual void Dib(CRhinoDib& dibOut) const = 0;

    // Get the thumbnail's display rectangle in client coords of the parent.
    //virtual void GetDisplayRect(ON_4iRect& rectOut) const = 0;

    Guid Id();

    string Label();

    bool IsSelected();

    bool IsHot();

    // CRhinoDib = System.Drawing.Bitmap
    void Dib(ref System.Drawing.Bitmap dibOut);

    void GetDisplayRect(ref RectangleF rectOut);

    System.Drawing.Bitmap GetDib();
  }
}
