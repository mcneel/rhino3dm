#pragma warning disable 1591

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rhino.UI.Controls.ThumbnailUI
{
  [Obsolete("Thumbnail is obsolete", false)]
  public class Thumbnail : IRhRdkThumbnail, IDisposable
  {

    private IntPtr m_cpp;

    /// <summary>
    /// Thumbnail c++ pointer
    /// </summary>
    public IntPtr CppPointer
    {
      get { return m_cpp; }
    }

    /// <summary>
    /// Constructor for Thumbnail
    /// </summary>
    public Thumbnail(IntPtr pRdkThumbnail)
    {
      m_cpp = pRdkThumbnail;
    }

    /// <summary>
    /// Destructor for Thumbnail
    /// </summary>
    ~Thumbnail()
    {
      Dispose(false);
    }

    /// <summary>
    /// Dispose for Thumbnail
    /// </summary>
    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    void Dispose(bool bDisposing)
    {
      if (m_cpp != IntPtr.Zero)
      {
        m_cpp = IntPtr.Zero;
      }
    }

    public void Dib(ref Bitmap dibOut)
    {
      IntPtr pDib = UnsafeNativeMethods.CRdkThumbnail_Dib(m_cpp);

      dibOut = Rhino.Runtime.InteropWrappers.RhinoDib.ToBitmap(pDib, true);
    }

    public System.Drawing.Bitmap GetDib()
    {
      IntPtr pDib = UnsafeNativeMethods.CRdkThumbnail_Dib(m_cpp);

      var bitmap = Rhino.Runtime.InteropWrappers.RhinoDib.ToBitmap(pDib, true);
      return bitmap;
    }

    public void GetDisplayRect(ref RectangleF rectOut)
    {
      throw new NotImplementedException();
    }

    public Guid Id()
    {
      throw new NotImplementedException();
    }

    public bool IsHot()
    {
      throw new NotImplementedException();
    }

    public bool IsSelected()
    {
      throw new NotImplementedException();
    }

    public string Label()
    {
      throw new NotImplementedException();
    }
  }
}
