using Rhino.DocObjects;
using Rhino.Runtime;
using Rhino.Runtime.InteropWrappers;
#pragma warning disable 1591
using System;

#if RHINO_SDK
namespace Rhino.Display
{
  /// <summary>
  /// A window that contains a single layout 'page'
  /// </summary>
  public class RhinoPageView : RhinoView
  {
    internal RhinoPageView(uint serialNumber)
      : base(serialNumber)
    {
    }

    /// <summary>Copies a page view.</summary>
    /// <param name="duplicatePageGeometry">Set true if you want the page view geometry copied, along with the view.</param>
    /// <returns>The new page view if successful, null otherwise.</returns>
    /// <since>6.3</since>
    public RhinoPageView Duplicate(bool duplicatePageGeometry)
    {
      IntPtr ptrPageView = UnsafeNativeMethods.CRhinoPageView_Duplicate(RuntimeSerialNumber, duplicatePageGeometry);
      return RhinoView.FromIntPtr(ptrPageView) as RhinoPageView;
    }


    /// <summary>
    /// Gets the active viewport.
    /// The ActiveViewport is the same as the MainViewport for standard RhinoViews. In
    /// a RhinoPageView, the active viewport may be the RhinoViewport of a child detail object.
    /// Most of the time, you will use ActiveViewport unless you explicitly need to work with
    /// the main viewport.
    /// </summary>
    /// <since>5.0</since>
    public override RhinoViewport ActiveViewport
    {
      get
      {
        bool main_viewport = false;
        IntPtr viewport_ptr = UnsafeNativeMethods.CRhinoView_ActiveViewport(RuntimeSerialNumber, ref main_viewport);
        if (main_viewport)
          return MainViewport;
        return new RhinoViewport(this, viewport_ptr);
      }
    }

    /// <summary>
    /// Deactivates the active details and sets the page view as active.
    /// </summary>
    /// <example>
    /// <code source='examples\vbnet\ex_addlayout.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_addlayout.cs' lang='cs'/>
    /// <code source='examples\py\ex_addlayout.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    public void SetPageAsActive()
    {
      if (!PageIsActive)
      {
        var details = GetDetailViews();
        if (details != null)
        {
          foreach (DetailViewObject detail in details)
            detail.IsActive = false;
        }
      }
    }

    /// <summary>
    /// Returns the id of the active detail. 
    /// If no detail is active, or if the page is active, then Guid.Empty is returned.
    /// </summary>
    /// <since>7.0</since>
    public Guid ActiveDetailId
    {
      get
      {
        DetailViewObject detail = ActiveDetail;
        return detail != null 
          ? detail.Id 
          : Guid.Empty;
      }
    }

    /// <summary>
    /// Returns the active detail object. 
    /// If no detail is active, or if the page is active, then null is returned.
    /// </summary>
    /// <since>7.0</since>
    public DetailViewObject ActiveDetail
    {
      get
      {
        if (!PageIsActive)
        {
          var details = GetDetailViews();
          if (details != null)
          {
            foreach (DetailViewObject detail in details)
            {
              if (detail != null && detail.IsActive)
                return detail;
            }
          }
        }
        return null;
      }
    }

    /// <summary>
    /// Sets the active detail. 
    /// </summary>
    /// <param name="detailId">The id of the detail view object to set active.</param>
    /// <returns>true if successful, false otherwise.</returns>
    /// <example>
    /// <code source='examples\vbnet\ex_addlayout.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_addlayout.cs' lang='cs'/>
    /// <code source='examples\py\ex_addlayout.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    public bool SetActiveDetail(Guid detailId)
    {
      bool rc = false;
      var details = GetDetailViews();
      if (details != null)
      {
        foreach (DetailViewObject detail in details)
        {
          if (detail.Id == detailId)
          {
            detail.IsActive = true;
            rc = true;
            break;
          }
        }
      }
      return rc;
    }

    /// <summary>
    /// Sets the active detail. 
    /// </summary>
    /// <param name="detailName">The name, or title, of the detail to set active.</param>
    /// <param name="compareCase">Unused.</param>
    /// <returns>true if successful, false otherwise.</returns>
    /// <since>5.0</since>
    public bool SetActiveDetail(string detailName, bool compareCase)
    {
      bool rc = false;
      var details = GetDetailViews();
      if (details != null)
      {
        foreach (DetailViewObject detail in details)
        {
          var vp = detail.Viewport;
          if (string.Equals(vp.Name, detailName, StringComparison.OrdinalIgnoreCase))
          {
            detail.IsActive = true;
            rc = true;
            break;
          }
        }
      }
      return rc;
    }

    /// <summary>
    /// Returns true if the page is active, rather than any detail view. 
    /// This occurs when the MainViewport.Id == ActiveViewportID.
    /// </summary>
    /// <example>
    /// <code source='examples\vbnet\ex_activeviewport.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_activeviewport.cs' lang='cs'/>
    /// <code source='examples\py\ex_activeviewport.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    public bool PageIsActive
    {
      get
      {
        return MainViewport.Id == ActiveViewportID;
      }
    }

    /// <summary>
    /// Creates a detail view object that is displayed on this page and adds it to the document.
    /// </summary>
    /// <param name="title">The detail view title.</param>
    /// <param name="corner0">Corners of the detail view in world coordinates.</param>
    /// <param name="corner1">Corners of the detail view in world coordinates.</param>
    /// <param name="initialProjection">The defined initial projection type.</param>
    /// <returns>Newly created detail view on success, null on error.</returns>
    /// <example>
    /// <code source='examples\vbnet\ex_addlayout.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_addlayout.cs' lang='cs'/>
    /// <code source='examples\py\ex_addlayout.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    public DetailViewObject AddDetailView(string title, Geometry.Point2d corner0, Geometry.Point2d corner1, DefinedViewportProjection initialProjection)
    {
      IntPtr ptr_detail = UnsafeNativeMethods.CRhinoPageView_AddDetailView(RuntimeSerialNumber, corner0, corner1, title, (int)initialProjection);
      DetailViewObject rc = null;
      if (ptr_detail != IntPtr.Zero)
      {
        uint sn = UnsafeNativeMethods.CRhinoObject_RuntimeSN(ptr_detail);
        rc = new DetailViewObject(sn);
      }
      return rc;
    }

    /// <summary>
    /// Gets a list of the detail view objects associated with this layout.
    /// </summary>
    /// <returns>An array of detail view objects if successful, an empty array if the layout has no details.</returns>
    /// <since>5.0</since>
    public DetailViewObject[] GetDetailViews()
    {
      IntPtr ptr_list = UnsafeNativeMethods.CRhinoDetailViewArray_New();
      int count = UnsafeNativeMethods.CRhinoPageView_GetDetailViewObjects(RuntimeSerialNumber, ptr_list);
      if (count < 1)
      {
        UnsafeNativeMethods.CRhinoDetailViewArray_Delete(ptr_list);
        return new DetailViewObject[0];
      }

      var rc = new DetailViewObject[count];
      for (int i = 0; i < count; i++)
      {
        IntPtr ptr_detail = UnsafeNativeMethods.CRhinoDetailViewArray_Item(ptr_list, i);
        if (ptr_detail != IntPtr.Zero)
        {
          uint sn = UnsafeNativeMethods.CRhinoObject_RuntimeSN(ptr_detail);
          rc[i] = new DetailViewObject(sn);
        }
      }
      UnsafeNativeMethods.CRhinoDetailViewArray_Delete(ptr_list);
      return rc;
    }

    /// <summary>
    /// Gets or sets the runtime page number and updates the page number for all
    /// of the other pages. The first page has a value of 0.
    /// </summary>
    /// <since>5.0</since>
    public int PageNumber
    {
      get
      {
        return UnsafeNativeMethods.CRhinoPageView_GetPageNumber(RuntimeSerialNumber);
      }
      set
      {
        UnsafeNativeMethods.CRhinoPageView_SetPageNumber(RuntimeSerialNumber, value);
      }
    }

    /// <summary>
    /// Creates a preview image of the page.
    /// </summary>
    /// <param name="size">The size of the preview image.</param>
    /// <param name="grayScale">Set true to produce a grayscale image, false to produce a color image.</param>
    /// <returns>A bitmap if successful, null otherwise.</returns>
    /// <since>7.0</since>
    public System.Drawing.Bitmap GetPreviewImage(System.Drawing.Size size, bool grayScale)
    {
      using (var dib = new RhinoDib())
      {
        var ptr_dib = dib.NonConstPointer;
        if (UnsafeNativeMethods.CRhinoPageView_GetPreviewImage(RuntimeSerialNumber, size.Width, size.Height, grayScale, ptr_dib))
        {
          var bitmap = dib.ToBitmap();
          return bitmap;
        }
        return null;
      }
    }

    /// <summary>
    /// Width of the page in the document's PageUnitSystem
    /// </summary>
    /// <example>
    /// <code source='examples\vbnet\ex_rhinopageviewwidthheight.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_rhinopageviewwidthheight.cs' lang='cs'/>
    /// <code source='examples\py\ex_rhinopageviewwidthheight.py' lang='py'/>
    /// </example>
    /// <since>5.10</since>
    public double PageWidth
    {
      get
      {
        return UnsafeNativeMethods.CRhinoPageView_GetSize(RuntimeSerialNumber, true);
      }
      set
      {
        UnsafeNativeMethods.CRhinoPageView_SetSize(RuntimeSerialNumber, true, value);
      }
    }

    /// <summary>
    /// Height of the page in the document's PageUnitSystem
    /// </summary>
    /// <example>
    /// <code source='examples\vbnet\ex_rhinopageviewwidthheight.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_rhinopageviewwidthheight.cs' lang='cs'/>
    /// <code source='examples\py\ex_rhinopageviewwidthheight.py' lang='py'/>
    /// </example>
    /// <since>5.10</since>
    public double PageHeight
    {
      get
      {
        return UnsafeNativeMethods.CRhinoPageView_GetSize(RuntimeSerialNumber, false);
      }
      set
      {
        UnsafeNativeMethods.CRhinoPageView_SetSize(RuntimeSerialNumber, false, value);
      }
    }

    /// <summary>Same as the MainViewport.Name.</summary>
    /// <example>
    /// <code source='examples\vbnet\ex_activeviewport.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_activeviewport.cs' lang='cs'/>
    /// <code source='examples\py\ex_activeviewport.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    public string PageName
    {
      get
      {
        RhinoViewport vp = MainViewport;
        if (vp != null)
          return vp.Name;
        return String.Empty;
      }
      set
      {
        RhinoViewport vp = MainViewport;
        if (vp != null)
          vp.Name = value;
      }
    }

    /// <summary>
    /// Returns the name of the layout's destination printer.
    /// </summary>
    /// <since>6.15</since>
    public string PrinterName
    {
      get
      {
        using (var sh = new StringHolder())
        {
          IntPtr ptr_string = sh.NonConstPointer();
          if (UnsafeNativeMethods.CRhinoPageView_PrinterName(RuntimeSerialNumber, ptr_string))
            return sh.ToString();
          return null;
        }
      }
    }

    /// <summary>
    /// Returns the name of the layout's media, or paper (e.g. Letter, Legal, A1, etc.),
    /// used to determine the page width and page height.
    /// </summary>
    /// <since>6.15</since>
    public string PaperName
    {
      get
      {
        using (var sh = new StringHolder())
        {
          IntPtr ptr_string = sh.NonConstPointer();
          if (UnsafeNativeMethods.CRhinoPageView_PaperName(RuntimeSerialNumber, ptr_string))
            return sh.ToString();
          return null;
        }
      }
    }


    internal delegate void PageViewCallback(IntPtr pView, Guid newDetailId, Guid oldDetailId);
    private static PageViewCallback g_on_page_space_changed;
    private static EventHandler<PageViewSpaceChangeEventArgs> g_detail_space_change;
    private static void OnActiveDetailChange(IntPtr pPageView, Guid newDetailId, Guid oldDetailId)
    {
      g_detail_space_change?.SafeInvoke(null, new PageViewSpaceChangeEventArgs(pPageView, newDetailId, oldDetailId));
    }
    /// <since>5.0</since>
    public static event EventHandler<PageViewSpaceChangeEventArgs> PageViewSpaceChange
    {
      add
      {
        if( Runtime.HostUtils.ContainsDelegate(g_detail_space_change, value) )
          return;
        if (g_detail_space_change == null)
        {
          g_on_page_space_changed = OnActiveDetailChange;
          UnsafeNativeMethods.CRhinoEventWatcher_SetDetailEventCallback(g_on_page_space_changed);
        }
        // ReSharper disable once DelegateSubtraction
        g_detail_space_change -= value;
        g_detail_space_change += value;
      }
      remove
      {
        // ReSharper disable once DelegateSubtraction
        g_detail_space_change -= value;
        if (g_detail_space_change == null)
        {
          UnsafeNativeMethods.CRhinoEventWatcher_SetDetailEventCallback(null);
          g_on_page_space_changed = null;
        }
      }
    }

    internal delegate void PageViewPropertiesCallback(uint documentSerialNumber, uint pageviewSerialNumber);
    private static PageViewPropertiesCallback g_pageview_properties_callback;
    private static EventHandler<PageViewPropertiesChangeEventArgs> g_pageview_properties_change;
    private static void OnPageViewPropertiesChange(uint documentSerialNumber, uint pageViewSerialNumber)
    {
      g_pageview_properties_change?.SafeInvoke(null, new PageViewPropertiesChangeEventArgs(documentSerialNumber, pageViewSerialNumber));
    }
    public static event EventHandler<PageViewPropertiesChangeEventArgs> PageViewPropertiesChange
    {
      add
      {
        if (Runtime.HostUtils.ContainsDelegate(g_pageview_properties_change, value))
          return;
        if (g_pageview_properties_change == null)
        {
          g_pageview_properties_callback = OnPageViewPropertiesChange;
          UnsafeNativeMethods.CRhinoEventWatcher_SetPageViewPropertiesCallback(g_pageview_properties_callback);
        }
        // ReSharper disable once DelegateSubtraction
        g_pageview_properties_change -= value;
        g_pageview_properties_change += value;
      }
      remove
      {
        // ReSharper disable once DelegateSubtraction
        g_pageview_properties_change -= value;
        if (g_pageview_properties_change == null)
        {
          UnsafeNativeMethods.CRhinoEventWatcher_SetDetailEventCallback(null);
          g_pageview_properties_callback = null;
        }
      }
    }

  }
}
#endif
