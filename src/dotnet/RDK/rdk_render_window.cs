#pragma warning disable 1591
#if RHINO_SDK
using Rhino.DocObjects;
using System;
using System.Drawing;

namespace Rhino.Render
{
  /**
   * Pixel buffer to hold a pointer to (unmanaged) pixel data.
   * 
   * This class will be in the (near) future improved to help with
   * management of buffers between unmanaged and managed code. For
   * C++ this could be the use of std::shared_ptr&lt;&gt;. See 
   * http://phab.mcneel.com/D12 for discussion on this topic.
   */
  public class PixelBuffer
  {
    public IntPtr Buffer { get; private set; }

    /// <summary>
    /// Create a new PixelBuffer pointing to the
    /// (unmanaged) pixel data buffer behind IntPtr
    /// </summary>
    /// <param name="bufferPointer">IntPtr to the unmanaged pixel data buffer</param>
    public PixelBuffer(IntPtr bufferPointer)
    {
      Buffer = bufferPointer;
    }
  }

  public class RenderWindowClonedEventArgs : EventArgs
  {
    internal RenderWindowClonedEventArgs(Guid oldSessionId, Guid newSessionId)
    {
      OldSessionId = oldSessionId;
      NewSessionId = newSessionId;
    }
    public Guid OldSessionId { get; private set; }
    public Guid NewSessionId { get; private set; }
    public RenderWindow OldRenderWindow { get { return RenderWindow.FromSessionId(OldSessionId); } }
    public RenderWindow NewRenderWindow { get { return RenderWindow.FromSessionId(NewSessionId); } }
  }

  public class RenderWindow : IDisposable
  {
    private readonly Guid m_render_window_id = Guid.Empty;
    internal RenderWindow(Guid renderWindowId)
    {
      m_render_window_id = renderWindowId;
    }

    private readonly IntPtr m_render_window_ptr = IntPtr.Zero;
    internal RenderWindow(IntPtr renderWindowIntPtr)
    {
      m_render_window_ptr = renderWindowIntPtr;
    }

    private bool dodelete;
    public static RenderWindow Create(Size szSize)
    {
      var rwptr = UnsafeNativeMethods.Rdk_RenderWindow_Create();
      if (rwptr != IntPtr.Zero)
      {
        var rw = new RenderWindow(rwptr);
        rw.dodelete = true;
        rw.SetSize(szSize);
        return rw;
      }

      return null;
    }

    public void Dispose()
    {
      if (dodelete)
      {
        if (ConstPointer()!= IntPtr.Zero)
        {
          UnsafeNativeMethods.Rdk_RenderWindow_Free(ConstPointer());
        }
      }
    }

    public Guid SessionId { get { return m_render_window_id; } }

    public static event EventHandler<RenderWindowClonedEventArgs> Cloned
    {
      add
      {
        if (g_cloned_event_callback == null)
        {
          g_cloned_event_callback = OnClonedCallback;
          UnsafeNativeMethods.CRdkCmnEventWatcher_SetOnRenderWindowClonedEventCallback(g_cloned_event_callback, Runtime.HostUtils.m_rdk_ew_report);
        }
        ClonedEvent -= value;
        ClonedEvent += value;
      }
      remove
      {
        ClonedEvent -= value;
        if (ClonedEvent != null)
          return;
        UnsafeNativeMethods.CRdkCmnEventWatcher_SetOnRenderWindowClonedEventCallback(null, Runtime.HostUtils.m_rdk_ew_report);
        g_cloned_event_callback = null;
      }
    }

    internal delegate void ClonedEventCallback(Guid oldSessionId, Guid newSessionId);
    private static ClonedEventCallback g_cloned_event_callback;
    private static event EventHandler<RenderWindowClonedEventArgs> ClonedEvent;
    private static void OnClonedCallback(Guid oldSessionId, Guid newSessionId)
    {
      if (ClonedEvent == null)
        return;
      var e = new RenderWindowClonedEventArgs(oldSessionId, newSessionId);
      ClonedEvent.Invoke(FromSessionId(oldSessionId), e);
    }

    public enum RenderSuccessCode : int
    {
      Completed = 0,
      Failed
    }

    [Flags]
    public enum StandardChannels : int
    {
      None    = 0x0000,
      Red     = 0x0001,
      Green   = 0x0002,
      Blue    = 0x0004,
      Alpha   = 0x0008,
      RGBA    = 0x000E,
      RGB     = 0x000F,

      DistanceFromCamera  = 0x0010,
      NormalX             = 0x0020,
      NormalY             = 0x0040,
      NormalZ             = 0x0080,

      LuminanceRed        = 0x0100,
      LuminanceGreen      = 0x0200,
      LuminanceBlue       = 0x0400,

      BackgroundLuminanceRed      = 0x1000,
      BackgroundLuminanceGreen    = 0x2000,
      BackgroundLuminanceBlue     = 0x4000,

      MaterialIds       = 0x00010000,
      ObjectIds         = 0x00020000,
      Wireframe         = 0x00040000,
    }

    public static Guid ChannelId(StandardChannels ch)
    {
      return UnsafeNativeMethods.Rdk_RenderWindow_StandardChannelId((int)ch);
    }

    public System.Drawing.Size Size()
    {
      int width = 0;int height = 0;
      UnsafeNativeMethods.Rdk_RenderWindow_Size(ConstPointer(), ref width, ref height);
      return new System.Drawing.Size(width, height);
    }

    public void SetSize(System.Drawing.Size size)
    {
      int width = size.Width, height = size.Height;
      UnsafeNativeMethods.Rdk_RenderWindow_SetSize(ConstPointer(), width, height);
    }

    public void SetView(ViewInfo view)
    {
      UnsafeNativeMethods.Rdk_RenderWindow_SetView(ConstPointer(), view.ConstPointer());
    }

    /// <summary>
    /// Accepts a rendering progress value to inform the user of the rendering advances.
    /// </summary>
    /// <param name="text">The progress text.</param>
    /// <param name="progress">A progress value in the domain [0.0f; 1.0f].</param>
    public void SetProgress(string text, float progress)
    {
      UnsafeNativeMethods.Rdk_RenderWindow_SetProgress(ConstPointer(), text, (int)(progress * 100.0f));
    }

    /// <summary>
    /// Must be called when an asynchronous render has finished or ended for any reason.
    /// </summary>
    /// <param name="successCode">Completed for a correct or canceled render, Failed for errors.</param>
    public void EndAsyncRender(RenderSuccessCode successCode)
    {
      UnsafeNativeMethods.Rdk_RenderWindow_EndAsyncRender(ConstPointer(), (int)successCode);
    }

    public Channel OpenChannel(StandardChannels id)
    {
      IntPtr pChannel = UnsafeNativeMethods.Rdk_RenderWindow_OpenChannel(ConstPointer(), (int)id);
      if (pChannel != IntPtr.Zero)
      {
        return new Channel(pChannel);
      }
      return null;
    }

    public Bitmap GetBitmap()
    {
      Bitmap bmp = null;

      IntPtr pDib = UnsafeNativeMethods.Rdk_RenderWindow_LockDib(ConstPointer());

      if (UnsafeNativeMethods.CRhinoDib_ColorDepth(pDib) != 32)
      {
        IntPtr native_bitmap = UnsafeNativeMethods.CRhinoDib_Bitmap(pDib);
        bmp = System.Drawing.Image.FromHbitmap(native_bitmap);
      }
      else
      {
        int width = 0, height = 0;
        UnsafeNativeMethods.CRhinoDib_Size(pDib, ref width, ref height);
        bmp = new System.Drawing.Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

        var rect = System.Drawing.Rectangle.FromLTRB(0, 0, width, height);
        var data = bmp.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadWrite, bmp.PixelFormat);
        unsafe
        {
          var bits = data.Scan0.ToPointer();
          var pointer = new IntPtr(bits);

          UnsafeNativeMethods.CRhinoDib_CopyToBitSystemBitmap(pDib, pointer, data.Stride, 4);
        }
        bmp.UnlockBits(data);
      }

      UnsafeNativeMethods.Rdk_RenderWindow_UnlockDib(ConstPointer());
      return bmp;
    }


    /// <summary>
    /// A wireframe channel will not be added if none of the document properties settings
    /// indicate that one is needed. In other words, Rhino will not generate an empty wireframe channel
    /// just for the fun of it.
    /// </summary>
    /// <param name="doc">The document to display</param>
    /// <param name="viewport">The view to display</param>
    /// <param name="size">The size of the image without clipping (ie - if you have a region, it was the
    /// size of the image before you cut the region out.</param>
    /// <param name="region">The area of the rendering you want to display.  This should match the size
    /// of the render window itself (ie - the one set using SetSize)</param>
    /// <returns>Returns true if the wireframe channel was successfully added.</returns>
    public bool AddWireframeChannel(RhinoDoc doc, DocObjects.ViewportInfo viewport, System.Drawing.Size size, System.Drawing.Rectangle region)
    {
      int[] xy = { size.Width, size.Height };
      int[] lrtb = { region.Left, region.Right, region.Top, region.Bottom };
      return UnsafeNativeMethods.Rdk_RenderWindow_AddWireframeChannel(ConstPointer(), doc.RuntimeSerialNumber, viewport.ConstPointer(), xy, lrtb);
    }

    /// <summary>
    /// Add a channel to the frame buffer in addition to the fixed Red, Green, Blue and Alpha channels.
    /// </summary>
    /// <param name="channel">Channel to add</param>
    /// <returns>If the channel existed then true is returned otherwise; returns true if the channel was added or false if not.</returns>
    public bool AddChannel(StandardChannels channel)
    {
      uint pixelSize = 0;
      Guid uuidChannel = Guid.Empty;
      switch (channel)
      {
        case StandardChannels.DistanceFromCamera:
        case StandardChannels.Alpha:
        case StandardChannels.NormalX:
        case StandardChannels.NormalY:
        case StandardChannels.NormalZ:
        case StandardChannels.LuminanceRed:
        case StandardChannels.LuminanceGreen:
        case StandardChannels.LuminanceBlue:
          pixelSize = sizeof(float);
          uuidChannel = ChannelId(channel);
          break;
      }
      if (pixelSize < 1 || uuidChannel == Guid.Empty)
        return false;
      return UnsafeNativeMethods.Rdk_RenderWindow_AddChannel(ConstPointer(), ref uuidChannel, pixelSize);
    }

    /// <summary>
    /// Call this method to open the RenderWindow.StandardChannels.RGBA channel and set a block of color values
    /// </summary>
    /// <param name="size">Size of the area to set. No validation is done on this value</param>
    /// <param name="colors">Array of Color4f values used to set the RenderWindow.StandardChannels.RGBA </param>
    public void SetRGBAChannelColors(System.Drawing.Size size, Rhino.Display.Color4f[] colors)
    {
      SetRGBAChannelColors(System.Drawing.Rectangle.FromLTRB(0, 0, size.Width, size.Height), colors);
    }

    /// <summary>
    /// Call this method to open the RenderWindow.StandardChannels.RGBA channel and set a block of color values
    /// </summary>
    /// <param name="rectangle">
    /// rectangle.X is the horizontal pixel position of the left edge. No validation is done on this value.
    ///   The caller is responsible for ensuring that it is within the frame buffer.
    /// rectangle.Y is the vertical pixel position of the top edge. No validation is done on this value.
    ///   The caller is responsible for ensuring that it is within the frame buffer.
    /// rectangle.Width is the width of the rectangle in pixels. No validation is done on this value.
    /// rectangle.Height is the height of the rectangle in pixels. No validation is done on this value.
    /// </param>
    /// <param name="colors">Array of Color4f values used to set the RenderWindow.StandardChannels.RGBA </param>
    public void SetRGBAChannelColors(System.Drawing.Rectangle rectangle, Rhino.Display.Color4f[] colors)
    {
      using (Rhino.Render.RenderWindow.Channel channel = OpenChannel(Rhino.Render.RenderWindow.StandardChannels.RGBA))
        channel.SetValues(rectangle, colors);
    }

    /// <summary>
    /// Invalidate the entire view window so that the pixels get painted.
    /// </summary>
    public void Invalidate()
    {
      UnsafeNativeMethods.Rdk_RenderWindow_Invalidate(ConstPointer());
    }

    public void InvalidateArea(System.Drawing.Rectangle rect)
    {
      UnsafeNativeMethods.Rdk_RenderWindow_InvalidateArea(ConstPointer(), rect.Top, rect.Left, rect.Bottom, rect.Right);
    }

    /// <summary>
    /// Save current Dib of RenderWindow as file
    /// Helper function for debugging purposes.
    /// </summary>
    /// <param name="filename"></param>
    public void SaveDibAsBitmap(string filename)
    {
      UnsafeNativeMethods.Rdk_RenderWindow_SaveDibAsBitmap(ConstPointer(), filename);
    }

		/// <summary>
		/// Save current RenderWindow contents as
		/// an image file with the given name. The
		/// filetype will be determine. 
		/// 
		/// Pass in render engine Guid if an engine
		/// implements saving to a format that is not
		/// supported by Rhino. Guid.Empty if there is no need for that.
		/// </summary>
		/// <param name="filename">Filename of image file to be created</param>
		/// <param name="renderEngineGuid">render engine ID</param>
		/// <param name="saveAlpha">True if alpha channel should be saved.</param>
    public void SaveRenderImageAs(string filename, Guid renderEngineGuid, bool saveAlpha)
    {
      UnsafeNativeMethods.Rdk_RenderWindow_SaveRenderImageAs(ConstPointer(), filename, renderEngineGuid, saveAlpha);
    }

		/// <summary>
		/// Like RenderWindow.SaveRenderImageAs(string,Guid,bool), but with
		/// Guid set to Guid.Empty.
		/// </summary>
		/// <param name="filename">Filename of image file to be created</param>
		/// <param name="saveAlpha">True if alpha channel should be saved.</param>
    public void SaveRenderImageAs(string filename, bool saveAlpha)
    {
      UnsafeNativeMethods.Rdk_RenderWindow_SaveRenderImageAs(ConstPointer(), filename, Guid.Empty, saveAlpha);
    }

    public static RenderWindow FromSessionId(Guid sessionId)
    {
      var pointer = UnsafeNativeMethods.IRhRdkRenderWindow_Find(sessionId);
      if (pointer == IntPtr.Zero) return null;
      var value = new RenderWindow(sessionId);
      return value;
    }

    /// <summary>
    /// Get an ImageAdjust instance containing current image adjusting settings
    /// for this RenderWindow
    /// </summary>
    /// <returns>ImageAdjust</returns>
    public ImageAdjust GetAdjust()
    {
      float gamma = 0.0f;
      uint dither = 0;
      UnsafeNativeMethods.Rdk_RenderWindow_GetAdjust(ConstPointer(), ref gamma, ref dither);
      return new ImageAdjust(gamma, (Dithering.Methods)dither);
    }

    /// <summary>
    /// Set new ImageAdjust to use. An ImageAdjust instance can be obtained by first
    /// querying for one using GetAdjust()
    /// </summary>
    /// <param name="imageAdjust">ImageAdjust</param>
    public void SetAdjust(ImageAdjust imageAdjust)
    {
      UnsafeNativeMethods.Rdk_RenderWindow_SetAdjust(ConstPointer(), imageAdjust.Gamma, (uint)imageAdjust.Dither);
    }

    #region internals
    internal IntPtr ConstPointer()
    {
      //IRhRdkRenderWindow* RhRdkFindRenderWindow
      //var pointer = UnsafeNativeMethods.Rdk_SdkRender_GetRenderWindow(m_renderPipe.ConstPointer());
      // The above call attempts to get the render frame associated with this pipeline
      // then get the render frame associated with the pipeline then get the render
      // window from the frame.  The problem is that the underlying unmanaged object
      // attached to this pipeline gets destroyed after the rendering is completed.
      // The render frame and window exist until the user closes the render frame so
      // the above call will fail when trying to access the render window for post
      // processing or tone operator adjustments after a render is completed. The
      // method bellow will get the render window using the render session Id associated
      // with this render instance and work as long as the render frame is available.
      if(m_render_window_ptr!=IntPtr.Zero) return m_render_window_ptr;
      var pointer = UnsafeNativeMethods.IRhRdkRenderWindow_Find(m_render_window_id);
      return pointer;
    }
    #endregion

    public class Channel : IDisposable
    {
      private IntPtr m_pChannel;
      internal Channel(IntPtr pChannel)
      {
        m_pChannel = pChannel;
      }

      /// <summary>
      /// Returns the size of the data in one pixel in the channel. For RDK standard channels, this value is always sizeof(float). 
      /// For the special chanRGBA collective channel,
      /// this value is 4 * sizeof(float).
      /// </summary>
      /// <returns>The size of a pixel.</returns>
      public int PixelSize()
      {
        return UnsafeNativeMethods.Rdk_RenderWindowChannel_PixelSize(ConstPointer());
      }

      /// <summary>
      /// If x or y are out of range, the function will fail and may crash Rhino.
      /// </summary>
      /// <param name="x">The horizontal pixel position. No validation is done on this value. 
      /// The caller is responsible for ensuring that it is within the frame buffer.</param>
      /// <param name="y">the vertical pixel position. No validation is done on this value.
      /// The caller is responsible for ensuring that it is within the frame buffer.</param>
      /// <param name="value">The value to store in the channel at the specified position.</param>
      public void SetValue(int x, int y, float value)
      {
        UnsafeNativeMethods.Rdk_RenderWindowChannel_SetFloatValue(ConstPointer(), x, y, value);
      }

      internal void SetValues(System.Drawing.Rectangle rectangle, Rhino.Display.Color4f[] colors)
      {
        UnsafeNativeMethods.Rdk_RenderWindowChannel_SetValueRect(ConstPointer(), rectangle.Left, rectangle.Top, rectangle.Width, rectangle.Height, colors);
      }

      /// <summary>
      /// Set a pixel buffer
      /// </summary>
      /// <param name="rectangle"></param>
      /// <param name="bufferResolution"></param>
      /// <param name="colorBuffer">PixelBuffer to a color buffer of floats in RGBA format.</param>
      public void SetValues(System.Drawing.Rectangle rectangle, Size bufferResolution, PixelBuffer colorBuffer)
      {
        UnsafeNativeMethods.Rdk_RenderWindowChannel_SetValueRect_FromFloats(ConstPointer(), rectangle.Left, rectangle.Top, rectangle.Width, rectangle.Height, bufferResolution.Width, colorBuffer.Buffer, true);
      }

      /// <summary>
      /// If x or y are out of range, the function will fail and may crash Rhino.
      /// </summary>
      /// <param name="x">The horizontal pixel position. No validation is done on this value. 
      /// The caller is responsible for ensuring that it is within the frame buffer.</param>
      /// <param name="y">The vertical pixel position. No validation is done on this value.
      /// The caller is responsible for ensuring that it is within the frame buffer.</param>
      /// <param name="value">The color to store in the channel at the specified position.</param>
      public void SetValue(int x, int y, Rhino.Display.Color4f value)
      {
        UnsafeNativeMethods.Rdk_RenderWindowChannel_SetColorValue(ConstPointer(), x, y, value);
      }

      /// <summary>
      /// Add value to existing values at point x, y.
      /// If x or y are out of range, the function will fail and may crash Rhino.
      /// </summary>
      /// <param name="x">The horizontal pixel position. No validation is done on this value. 
      /// The caller is responsible for ensuring that it is within the frame buffer.</param>
      /// <param name="y">The vertical pixel position. No validation is done on this value.
      /// The caller is responsible for ensuring that it is within the frame buffer.</param>
      /// <param name="value">The color to store in the channel at the specified position.</param>
      public void AddValue(int x, int y, Rhino.Display.Color4f value)
      {
        UnsafeNativeMethods.Rdk_RenderWindowChannel_AddColorValue(ConstPointer(), x, y, value);
      }

      IntPtr ConstPointer()
      {
        return m_pChannel;
      }

      #region IDisposable Members

      public void Dispose()
      {
        Dispose(true);
        GC.SuppressFinalize(this);
      }

      protected virtual void Dispose(bool disposing)
      {
        if (disposing)
        {
          if (m_pChannel != IntPtr.Zero)
          {
            UnsafeNativeMethods.Rdk_RenderWindowChannel_Close(m_pChannel);
            m_pChannel = IntPtr.Zero;
          }
        }
      }

      #endregion
    }

    /// <summary>
    /// Class to set image adjusting settings to a RenderWindow. This can't be
    /// directly created. Instead, one should use RenderWindow.GetAdjust() to
    /// get an ImageAdjust instance that can be modified, then passed into
    /// RenderWindow.SetAdjust() to update image adjusting settings.1
    /// </summary>
    public class ImageAdjust
    {
      /// <summary>
      /// Set the gamma value. The inverse of this will be used to apply
      /// gamma correction to the RenderWindow RGBA channel when necessary.
      /// </summary>
      public float Gamma { get; set; }
      /// <summary>
      /// Set the Dithering method to use when adjusting the RenderWindow
      /// content.
      /// </summary>
      public Dithering.Methods Dither { get; set; }
      /// <summary>
      /// Internal constructor, used in RenderWindow.GetAdjust()
      /// </summary>
      /// <param name="gamma"></param>
      /// <param name="dither"></param>
      internal ImageAdjust(float gamma, Dithering.Methods dither)
      {
        Gamma = gamma;
        Dither = dither;
      }
    }
  }
}

#endif