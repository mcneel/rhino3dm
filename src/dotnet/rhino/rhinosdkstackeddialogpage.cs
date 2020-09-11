using System;

#if RHINO_SDK
using System.Collections.Generic;
using System.Reflection;

namespace Rhino.UI
{
  /// <summary>
  /// For internal use, the IStackedDialogPageService service is implemented in
  /// RhinoWindows or RhinoMac as appropriate and handles the communication
  /// with core Rhino
  /// </summary>
  [CLSCompliant (false)]
  public interface IStackedDialogPageService
  {
    /// <summary>
    /// Convert image to platform specific unmanaged pointer
    /// </summary>
    /// <param name="image"></param>
    /// <param name="canBeNull"></param>
    /// <returns></returns>
    /// <since>6.0</since>
    IntPtr GetImageHandle(System.Drawing.Image image, bool canBeNull);
    /// <summary>
    /// Convert image to platform specific unmanaged pointer
    /// </summary>
    /// <param name="icon"></param>
    /// <param name="canBeNull"></param>
    /// <returns></returns>
    /// <since>6.1</since>
    IntPtr GetImageHandle(System.Drawing.Icon icon, bool canBeNull);
    /// <summary>
    /// Get the unmanaged pointer associated with the pages content control
    /// </summary>
    /// <param name="nativeWindowObject"></param>
    /// <param name="isRhinoPanel"></param>
    /// <param name="applyPanelStyles"></param>
    /// <param name="host"></param>
    /// <returns></returns>
    /// <since>6.0</since>
    IntPtr GetNativePageWindow(object nativeWindowObject, bool isRhinoPanel, bool applyPanelStyles, out object host);
    /// <summary>
    /// Get the unmanaged pointer associated with the pages content control
    /// </summary>
    /// <param name="pageObject"></param>
    /// <param name="nativeWindowObject"></param>
    /// <param name="isRhinoPanel"></param>
    /// <param name="applyPanelStyles"></param>
    /// <param name="host"></param>
    /// <returns></returns>
    /// <since>6.1</since>
    IntPtr GetNativePageWindow(object pageObject, bool isRhinoPanel, bool applyPanelStyles, out object nativeWindowObject, out object host);
    /// <summary>
    /// Redraw the specified control.
    /// </summary>
    /// <param name="pageControl">
    /// Control to redraw
    /// </param>
    /// <since>6.0</since>
    void RedrawPageControl(object pageControl);
    /// <summary>
    /// Get the minimum size associated with a control object
    /// </summary>
    /// <returns>
    /// Returns true if get control minimum size found, false otherwise.
    /// </returns>
    /// <param name="controlObject">
    /// The control object to check for minimum size.
    /// </param>
    /// <param name="size">
    /// The minimum size of the control if provided.
    /// </param>
    /// <since>6.5</since>
    bool TryGetControlMinimumSize (object controlObject, out System.Drawing.SizeF size);
  }

  class NotImplementedStackedDialogPageService : IStackedDialogPageService
  {
    public IntPtr GetImageHandle(System.Drawing.Image image, bool canBeNull)
		{
			throw new NotImplementedException ();
		}
    public IntPtr GetImageHandle (System.Drawing.Icon icon, bool canBeNull)
    {
      throw new NotImplementedException ();
    }
    public IntPtr GetNativePageWindow(object nativeWindowObject, bool isRhinoPanel, bool applyPanelStyles, out object host)
    {
      throw new NotImplementedException();
    }
    public IntPtr GetNativePageWindow(object pageObject, bool isRhinoPanel, bool applyPanelStyles, out object nativeWindowObject, out object host)
    {
      throw new NotImplementedException();
    }
    public void RedrawPageControl(object pageControl)
    {
      throw new NotImplementedException();
    }
    public bool TryGetControlMinimumSize (object controlObject, out System.Drawing.SizeF size)
    {
      throw new NotImplementedException();
    }
  }

  /// <summary>
  /// Base class to inherit from for the addition of stacked dialog pages.
  /// </summary>
  public abstract class StackedDialogPage
  {
    private static IStackedDialogPageService g_service_implementation;
    internal static IStackedDialogPageService Service
    {
      get
      {
        if (g_service_implementation != null)
          return g_service_implementation;
        g_service_implementation = Runtime.HostUtils.GetPlatformService<IStackedDialogPageService>();
        return (g_service_implementation ?? (g_service_implementation = new NotImplementedStackedDialogPageService()));
      }
    }
    /// <summary>
    /// Protected constructor
    /// </summary>
    /// <param name="englishPageTitle"></param>
    protected StackedDialogPage(string englishPageTitle)
    {
      EnglishPageTitle = englishPageTitle;
    }

    private List<StackedDialogPage> m_children;
    /// <summary>
    /// List of child (sub) pages of this page
    /// </summary>
    /// <since>5.0</since>
    public List<StackedDialogPage> Children => m_children ?? (m_children = new List<StackedDialogPage>());

    /// <summary>
    /// Will be true if this page contains sub pages.
    /// </summary>
    /// <since>5.0</since>
    public bool HasChildren => (m_children!=null && m_children.Count>0);

    /// <summary>
    /// Currently only supported on Windows.  Call this method to add a child
    /// page to a page after the parent dialog has been created.
    /// </summary>
    /// <param name="pageToAdd"></param>
    /// <since>6.0</since>
    public void AddChildPage(StackedDialogPage pageToAdd)
    {
      if (!Runtime.HostUtils.RunningOnWindows)
        return;
      var unmanaged_pointer = RhinoPageHooks.UnmanagedIRhinoPagePointerFromPage(this);
      if (unmanaged_pointer == IntPtr.Zero)
      {
        // The page array has not been processed yet so just append the page
        // to the end of the children list
        Children.Add(pageToAdd);
        return;
      }
      //
      // Unmanaged page has been created so create and add the new child page
      //
      // Use new IRhinoOptionsPageHost unmanaged interface to add the child page
      // Only allow adding child pages to OptionsDialogPages
      var pointer = pageToAdd is OptionsDialogPage
        ? RhinoPageHooks.NewIRhinoOptionsPagePointer(pageToAdd, RhinoPageHooks.RhinoDocRuntimeSerialNumberFromPage(this))
        : IntPtr.Zero;
      // Not a OptionsDialogPage or was unable to allocate a unmanaged pointer
      if (pointer == IntPtr.Zero)
        return;
      // Add the child page to this objects IRhinoOptionsPageHost
      if (!RhinoPageHooks.AddChildPage(unmanaged_pointer, pointer))
      {
        UnsafeNativeMethods.IRhinoOptionsPage_Delete(pointer);
        return;
      }
      // Put the new child page in the child array
      Children.Add(pageToAdd);
    }

    /// <summary>
    /// Check to see if the page has been marked as modified or not.  Marking
    /// the page as modified will cause the Apply button to get enabled if this
    /// is currently the visible page and the page includes the Apply button.
    /// </summary>
    /// <since>6.0</since>
    public bool Modified
    {
      get => RhinoPageHooks.GetSetIsPageModified(this, false, false);
      set => RhinoPageHooks.GetSetIsPageModified(this, true, value);
    }

    /// <summary>
    /// When running on Windows return the window handle for the parent of this
    /// page otherwise; return IntPtr.Zero.
    /// </summary>
    /// <since>6.0</since>
    public IntPtr Handle => RhinoPageHooks.PageCollectionWindowHandle(this);

    /// <summary>
    /// Remove this page from the dialog box
    /// </summary>
    /// <since>6.0</since>
    public void RemovePage()
    {
      RhinoPageHooks.RemovePage(this);
    }

    /// <summary>
    /// Make this page the active, visible page
    /// </summary>
    /// <since>6.0</since>
    public void MakeActivePage()
    {
      RhinoPageHooks.MakeActivePage(this);
    }
    ///<summary>
    /// Return the control that represents this page. Rhino Windows supports
    /// classes that implement the IWin32Windows interface or are derived from
    /// some form of System.Windows.FrameworkElement or Eto.Forms.Control.  Mac
    /// Rhino supports controls that are derived from NSview or
    /// Eto.Forms.Control.
    /// </summary>
    /// <since>5.0</since>
    public virtual object PageControl
    {
      get
      {
        try
        {
          return GetType().GetProperty("PageControl", typeof(System.Windows.Forms.Control))?.GetValue(this);
        }
        catch (Exception exception)
        {
          for (var e = exception; e != null; e = e.InnerException)
          {
            RhinoApp.WriteLine(e.Message);
            RhinoApp.WriteLine(e.StackTrace);
          }
          return null;
        }
      }
    }
    /// <summary>
    /// Called when the parent window has been created on Windows platforms
    /// only.
    /// </summary>
    /// <param name="hwndParent"></param>
    /// <since>5.0</since>
    public virtual void OnCreateParent(IntPtr hwndParent) { }
    /// <summary>
    /// Called when the parent window has been resized
    /// </summary>
    /// <param name="width"></param>
    /// <param name="height"></param>
    /// <since>5.0</since>
    public virtual void OnSizeParent(int width, int height) { }
    /// <summary>
    /// Change the title passed to the constructor and, this will cause
    /// LocalPageTitle to get called also.
    /// </summary>
    /// <param name="newPageTile"></param>
    /// <since>6.0</since>
    public void SetEnglishPageTitle(string newPageTile)
    {
      if (string.IsNullOrWhiteSpace(newPageTile))
        throw new ArgumentNullException(nameof(newPageTile));
      EnglishPageTitle = newPageTile;
      RhinoPageHooks.RefreshPageTitle(this);
    }
    /// <summary>
    /// English string used when scripting this page and a user prefixes a
    /// command option with "_"
    /// </summary>
    /// <since>5.0</since>
    public string EnglishPageTitle { get; private set; }

    /// <summary>
    /// Localized page title that will appear on a tab or other page navigation
    /// control.  This is also uses as a command line option when scripting
    /// this page.
    /// </summary>
    /// <since>5.0</since>
    public virtual string LocalPageTitle => EnglishPageTitle;

    /// <summary>
    /// Optionally override to provide a image to display in 
    /// the Mac Rhino UI
    /// </summary>
    /// <value>The page image.</value>
    /// <since>6.0</since>
    public virtual System.Drawing.Image PageImage => null;

    /// <summary>Called when stacked dialog OK button is pressed.</summary>
    /// <returns>
    /// If return value is true then the dialog will be closed. A return of false means
    /// there was an error and dialog remains open so page can be properly updated.
    /// </returns>
    /// <since>5.0</since>
    public virtual bool OnApply() { return true; }

    ///<summary>Called when stacked dialog Cancel button is pressed.</summary>
    /// <since>5.0</since>
    public virtual void OnCancel(){}

    ///<summary>Called when this page is activated/deactivated.</summary>
    ///<param name="active">If true then this page is on top otherwise it is about to be hidden.</param>
    ///<returns>
    ///If true then the page is hidden and the requested page is not
    ///activated otherwise will not allow you to change the current page.
    ///Default returns true
    ///</returns>
    /// <since>5.0</since>
    public virtual bool OnActivate( bool active) { return true; }

    ///<summary>Called when this page is activated.</summary>
    ///<returns>
    ///true  : if the page wants the "Defaults" button to appear.
    ///false : if the page does not want the "Defaults" button to appear.
    ///
    ///Default returns false
    ///Note: returning false implies that OnDefaults() method will never get called.
    ///</returns>
    /// <since>5.0</since>
    public virtual bool ShowDefaultsButton => false;

    ///<summary>Called when this page is activated</summary>
    ///<returns>
    ///true  : if the page wants the "Apply" button to appear.
    ///false : if the page does not want the "Apply" button to appear.
    ///
    ///Default returns false
    ///Note: If true is returned OnApply will get called when the button is
    ///      clicked.
    ///</returns>
    /// <since>6.0</since>
    public virtual bool ShowApplyButton => false;

    ///<summary>Called when stacked dialog Defaults button is pressed (see ShowDefaultsButton).</summary>
    /// <since>5.0</since>
    public virtual void OnDefaults() { }

    /// <summary>
    /// Called when the parent dialog requests help for this page.
    /// </summary>
    /// <since>5.0</since>
    public virtual void OnHelp() { }

    /// <summary>
    /// RhinoWindows will use reflection to call this method when the page gets
    /// added to a stacked dialog tree control.  When this happens set the
    /// cached item color and bold state as necessary.  This may get hooked up
    /// in Mac Rhino at some point.
    /// </summary>
    internal void AttachedToTreeControl()
    {
      m_attached_to_tree_control = true;
      // If the color has been set
      if (m_tree_item_color != System.Drawing.Color.Empty)
        NavigationTextColor = m_tree_item_color;
      // If the bold flag has been set
      if (m_tree_item_bold)
        NavigationTextIsBold = m_tree_item_bold;
    }
    private bool m_attached_to_tree_control;

    /// <summary>
    /// Currently only used by Windows Rhino.  If this is set to true then the
    /// tree control item text will be bold.
    /// </summary>
    /// <since>6.0</since>
    public bool NavigationTextIsBold
    {
      get
      {
        if (!m_attached_to_tree_control)
          return m_tree_item_bold;
        m_tree_item_bold = false;
        RhinoPageHooks.SetGetTreeItemBold(this, ref m_tree_item_bold, false);
        return m_tree_item_bold;
      }
      set
      {
        m_tree_item_bold = value;
        if (!m_attached_to_tree_control)
          return;
        RhinoPageHooks.SetGetTreeItemBold(this, ref value, true);
      }
    }
    private bool m_tree_item_bold;

    /// <summary>
    /// Currently only used by Windows Rhino.  If this is set to true then the
    /// tree control item text be drawn using this color.  Set the color to
    /// System.Drawing.Color.Empty to use the default color.
    /// </summary>
    /// <since>6.0</since>
    public System.Drawing.Color NavigationTextColor
    {
      get
      {
        if (!m_attached_to_tree_control)
          return m_tree_item_color;
        m_tree_item_color = System.Drawing.Color.Empty;
        RhinoPageHooks.SetGetTreeItemColor(this, ref m_tree_item_color, false);
        return m_tree_item_color;
      }
      set
      {
        m_tree_item_color = value;
        if (!m_attached_to_tree_control)
          return;
        RhinoPageHooks.SetGetTreeItemColor(this, ref value, true);
      }
    }
    private System.Drawing.Color m_tree_item_color = System.Drawing.Color.Empty;
  }
}
#endif
