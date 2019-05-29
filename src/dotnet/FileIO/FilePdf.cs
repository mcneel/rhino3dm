using System;

namespace Rhino.FileIO
{
  /// <summary>
  /// Used for events that are fired while constructing/saving a Pdf
  /// </summary>
  public class FilePdfEventArgs : EventArgs
  {
    internal FilePdfEventArgs(FilePdf pdf)
    {
      Pdf = pdf;
    }

    /// <summary>
    /// The document that is about to be written
    /// </summary>
    public FilePdf Pdf { get; private set; }
  }
  /// <summary> Support for PDF file format </summary>
  public abstract class FilePdf
  {
    /// <summary>
    /// Fired immediately before a Pdf is written
    /// </summary>
    public static event EventHandler<FilePdfEventArgs> PreWrite;

    internal delegate int SendToPdfCallback(IntPtr pFilename, IntPtr ptrPrintInfoPages, IntPtr ptrErrorMsgString);
    static SendToPdfCallback g_callback;
    static int SendToPdf(IntPtr pFilename, IntPtr ptrPrintInfoPages, IntPtr ptrErrorMessageString)
    {
      try
      {
        string filename = Runtime.InteropWrappers.StringWrapper.GetStringFromPointer(pFilename);
        int count = UnsafeNativeMethods.CRhinoPrintInfoArray_Count(ptrPrintInfoPages);
        if (string.IsNullOrWhiteSpace(filename) || count < 1)
          return 0;
        FilePdf pdf = Create();
        if (null == pdf)
          return 0;
        for (int i = 0; i < count; i++)
        {
          IntPtr ptr_view_capture = UnsafeNativeMethods.CRhinoPrintInfoArray_Item(ptrPrintInfoPages, i);
          var viewcapure = Runtime.Interop.ViewCaptureFromPointer(ptr_view_capture);
          pdf.AddPage(viewcapure);
          GC.KeepAlive(viewcapure);
        }
        pdf.Write(filename);
        return 1;
      }
      catch(Exception ex)
      {
        UnsafeNativeMethods.ON_wString_Set(ptrErrorMessageString, ex.Message);
        //RhinoApp.WriteLine("Error writing PDF");
        //RhinoApp.WriteLine(ex.Message);
        //Runtime.HostUtils.ExceptionReport(ex);
        return 0;
      }
    }
    internal static void SetHooks()
    {
      g_callback = SendToPdf;
      UnsafeNativeMethods.RHC_SetSendToPdfProc(g_callback);
    }

    /// <summary>
    /// Called by the framework to fire a PreWrite event
    /// </summary>
    protected void FirePreWriteEvent()
    {
      if (PreWrite != null)
        PreWrite(this, new FilePdfEventArgs(this));
    }

    /// <summary> Create a new instance of a FilePdf class</summary>
    /// <returns></returns>
    public static FilePdf Create()
    {
      Guid export_pdf_id = new Guid("DA09708B-2F39-4CAE-B787-D3CEB6D82DDC");
      object obj = RhinoApp.GetPlugInObject(export_pdf_id);
      return obj as FilePdf;
    }

    /// <summary>
    /// Add a new page to this document and draw a viewport into it based on
    /// provided ViewCaptureSettings
    /// </summary>
    /// <param name="settings"></param>
    /// <returns>page number on success</returns>
    public abstract int AddPage(Display.ViewCaptureSettings settings);

    /// <summary>
    /// Add a blank page to this document
    /// </summary>
    /// <param name="widthInDots"></param>
    /// <param name="heightInDots"></param>
    /// <param name="dotsPerInch"></param>
    /// <returns>page number on success</returns>
    public abstract int AddPage(int widthInDots, int heightInDots, int dotsPerInch);

    /// <summary>
    /// Draw text on a page
    /// </summary>
    /// <param name="pageNumber"></param>
    /// <param name="text"></param>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="heightPoints"></param>
    /// <param name="onfont"></param>
    /// <param name="fillColor"></param>
    /// <param name="strokeColor"></param>
    /// <param name="strokeWidth"></param>
    /// <param name="angleDegrees"></param>
    /// <param name="horizontalAlignment"></param>
    /// <param name="verticalAlignment"></param>
    public abstract void DrawText(int pageNumber, string text, double x, double y, float heightPoints,
      Rhino.DocObjects.Font onfont, System.Drawing.Color fillColor, System.Drawing.Color strokeColor, float strokeWidth,
      float angleDegrees, DocObjects.TextHorizontalAlignment horizontalAlignment, DocObjects.TextVerticalAlignment verticalAlignment);
    
    /// <summary>
    /// Draw a polyline path
    /// </summary>
    /// <param name="pageNumber"></param>
    /// <param name="polyline"></param>
    /// <param name="fillColor"></param>
    /// <param name="strokeColor"></param>
    /// <param name="strokeWidth"></param>
    public abstract void DrawPolyline(int pageNumber, System.Drawing.PointF[] polyline, System.Drawing.Color fillColor, System.Drawing.Color strokeColor, float strokeWidth);

    /// <summary>
    /// Draw a line
    /// </summary>
    /// <param name="pageNumber"></param>
    /// <param name="from"></param>
    /// <param name="to"></param>
    /// <param name="strokeColor"></param>
    /// <param name="strokeWidth"></param>
    public void DrawLine(int pageNumber, System.Drawing.PointF from, System.Drawing.PointF to, System.Drawing.Color strokeColor, float strokeWidth)
    {
      DrawPolyline(pageNumber, new[] { from, to }, System.Drawing.Color.Empty, strokeColor, strokeWidth);
    }

    /// <summary>
    /// Draw a bitmap
    /// </summary>
    /// <param name="pageNumber"></param>
    /// <param name="bitmap"></param>
    /// <param name="left"></param>
    /// <param name="top"></param>
    /// <param name="width"></param>
    /// <param name="height"></param>
    /// <param name="rotationInDegrees"></param>
    public abstract void DrawBitmap(int pageNumber, System.Drawing.Bitmap bitmap, float left, float top, float width, float height, float rotationInDegrees);

    /// <summary> Write pdf to a file </summary>
    /// <param name="filename"></param>
    public abstract void Write(string filename);

    /// <summary> Write pdf to a stream </summary>
    /// <param name="stream"></param>
    public abstract void Write(System.IO.Stream stream);

    /// <summary>
    /// Get actual implementation of PdfDocument class
    /// </summary>
    /// <returns></returns>
    public abstract object PdfDocumentImplementation();
  }
}