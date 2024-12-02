using System;
using System.Collections.Generic;
using Rhino.Display;
using Rhino.DocObjects;

#if RHINO_SDK

namespace Rhino.UI
{
  /// <summary>
  /// 
  /// </summary>
  public class ObjectPropertiesPageEventArgs
  {
    /// <summary>
    /// Used by <see cref="ObjectPropertiesPage"/> to notify the page when
    /// updating, modifying or determining if the page should be included
    /// in the navigation bar
    /// </summary>
    /// <param name="page">
    /// Page sending the message
    /// </param>
    /// <since>6.0</since>
    public ObjectPropertiesPageEventArgs(ObjectPropertiesPage page)
    {
      Page = page;
    }
    /// <summary>
    /// The page sending these arguments
    /// </summary>
    /// <since>6.0</since>
    public ObjectPropertiesPage Page { get; }

    /// <summary>
    /// Gets the runtime serial number.
    /// </summary>
    /// <value>The runtime serial number.</value>
    /// <since>6.14</since>
    [CLSCompliant(false)]
    public uint EventRuntimeSerialNumber => UnsafeNativeMethods.PropertiesEditor_Interop_ArgsEventRuntimeSerialNumber();

    /// <summary>
    /// Return a list of Rhino objects to be processed by this object properties page
    /// </summary>
    /// <since>6.0</since>
    public RhinoObject[] Objects => GetObjects(Page?.SupportedTypes ?? ObjectType.AnyObject);

    internal static ObjectType TypeFilter(Type type)
    {
      var filter = ObjectType.AnyObject;

      if (typeof(TextDotObject).IsAssignableFrom(type))
        filter = ObjectType.TextDot;
      else if (typeof(AnnotationObjectBase).IsAssignableFrom(type))
        filter = ObjectType.Annotation;
      else if (typeof(LightObject).IsAssignableFrom(type))
        filter = ObjectType.Light;
      else if (typeof(BrepObject).IsAssignableFrom(type))
        filter = ObjectType.Brep;
      else if (typeof(ClippingPlaneObject).IsAssignableFrom(type))
        filter = ObjectType.ClipPlane;
      else if (typeof(DetailViewObject).IsAssignableFrom(type))
        filter = ObjectType.Detail;
      else if (typeof(ExtrusionObject).IsAssignableFrom(type))
        filter = ObjectType.Extrusion;
      else if (typeof(HatchObject).IsAssignableFrom(type))
        filter = ObjectType.Hatch;
      else if (typeof(InstanceObject).IsAssignableFrom(type))
        filter = ObjectType.InstanceReference;
      else if (typeof(MeshObject).IsAssignableFrom(type))
        filter = ObjectType.Mesh;
      else if (typeof(PointCloudObject).IsAssignableFrom(type))
        filter = ObjectType.PointSet;
      else if (typeof(PointObject).IsAssignableFrom(type))
        filter = ObjectType.Point;
      else if (typeof(SurfaceObject).IsAssignableFrom(type))
        filter = ObjectType.Surface;

      return filter;
    }

    /// <summary>
    /// Return true if any of the selected objects match the given type
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    /// <since>6.0</since>
    public bool IncludesObjectsType<T>() where T : RhinoObject
    {
      return IncludesObjectsType<T>(Page?.AllObjectsMustBeSupported ?? Runtime.HostUtils.RunningOnOSX);
    }

    /// <summary>
    /// Return true if any of the selected objects match the given type
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="allMustMatch">
    /// If true then every selected object must match the object type
    /// otherwise; only a single object has to be of the specified type
    /// </param>
    /// <returns></returns>
    /// <since>6.7</since>
    public bool IncludesObjectsType<T>(bool allMustMatch) where T : RhinoObject
    {
      var filter = allMustMatch ? ObjectType.AnyObject : TypeFilter(typeof(T));
      var page_pointer = RhinoPageHooks.UnmanagedIRhinoPagePointerFromPage(Page);
      var count = 0;
      var array_pointer = UnsafeNativeMethods.IRhinoPropertiesPanelPage_GetObjects(page_pointer, (uint)filter, ref count);
      for (var i = 0; i < count; i++)
      {
        var pointer = UnsafeNativeMethods.IRhinoPropertiesPanelPage_GetObjectsAt(array_pointer, i);
        if (pointer == IntPtr.Zero)
          continue;
        var obj = RhinoObject.CreateRhinoObjectHelper(pointer) as T;
        // If all objects must match and this object does not then return false
        if (allMustMatch && obj == null)
          return false;
        // No requirement for all objects to match and this one does then just return true now
        if (!allMustMatch && obj != null)
          return true;
      }
      return allMustMatch && count > 0;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="objectTypes"></param>
    /// <returns></returns>
    /// <since>6.0</since>
    [CLSCompliant(false)]
    public bool IncludesObjectsType(ObjectType objectTypes) => IncludesObjectsType(objectTypes, Page?.AllObjectsMustBeSupported ?? Runtime.HostUtils.RunningOnOSX);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="objectTypes"></param>
    /// <param name="allMustMatch">
    /// If true then every selected object must match the object type
    /// otherwise; only a single object has to be of the specified type
    /// </param>
    /// <returns></returns>
    /// <since>6.7</since>
    [CLSCompliant (false)]
    public bool IncludesObjectsType(ObjectType objectTypes, bool allMustMatch)
    {
      var pointer = RhinoPageHooks.UnmanagedIRhinoPagePointerFromPage (Page);
      return UnsafeNativeMethods.IRhinoPropertiesPanelPage_AnySelectedObjects (pointer, (uint)objectTypes, allMustMatch);
    }

    /// <summary>
    /// Get selected objects of a given type
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    /// <since>6.0</since>
    public T[] GetObjects<T>() where T : RhinoObject
    {
      var type = typeof(T);
      var filter = TypeFilter(type);

      var page_pointer = RhinoPageHooks.UnmanagedIRhinoPagePointerFromPage(Page);
      var object_count = 0;
      var array_pointer = UnsafeNativeMethods.IRhinoPropertiesPanelPage_GetObjects(page_pointer, (uint)filter, ref object_count);
      var rc = new List<T>();

      for (var i = 0; i < object_count; i++)
      {
        var pointer = UnsafeNativeMethods.IRhinoPropertiesPanelPage_GetObjectsAt(array_pointer, i);
        if (pointer == IntPtr.Zero)
          continue;
        var rhino_object = RhinoObject.CreateRhinoObjectHelper(pointer) as T;
        if (rhino_object != null)
          rc.Add(rhino_object);
      }

      UnsafeNativeMethods.IRhinoPropertiesPanelPage_GetObjectsClear(array_pointer);

      return rc.ToArray();
    }

    /// <summary>
    /// Get selected objects that match a given filter
    /// </summary>
    /// <param name="filter"></param>
    /// <returns></returns>
    /// <since>6.0</since>
    [CLSCompliant(false)]
    public RhinoObject[] GetObjects(ObjectType filter)
    {
      var page_pointer = RhinoPageHooks.UnmanagedIRhinoPagePointerFromPage(Page);
      var object_count = 0;
      var array_pointer = UnsafeNativeMethods.IRhinoPropertiesPanelPage_GetObjects(page_pointer, (uint)filter, ref object_count);
      var rc = new List<RhinoObject>();

      for (var i = 0; i < object_count; i++)
      {
        var pointer = UnsafeNativeMethods.IRhinoPropertiesPanelPage_GetObjectsAt(array_pointer, i);
        if (pointer == IntPtr.Zero)
          continue;
        var rhino_object = RhinoObject.CreateRhinoObjectHelper(pointer);
        if (rhino_object != null)
          rc.Add(rhino_object);
      }

      UnsafeNativeMethods.IRhinoPropertiesPanelPage_GetObjectsClear(array_pointer);

      return rc.ToArray();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    /// <since>6.0</since>
    public int ObjectCount
    {
      get
      {
        var pointer = RhinoPageHooks.UnmanagedIRhinoPagePointerFromPage(Page);
        return UnsafeNativeMethods.IRhinoPropertiesPanelPage_ObjectCount(pointer);
      }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <since>6.0</since>
    [CLSCompliant(false)]
    public uint ObjectTypes
    {
      get
      {
        var pointer = RhinoPageHooks.UnmanagedIRhinoPagePointerFromPage(Page);
        return UnsafeNativeMethods.IRhinoPropertiesPanelPage_ObjectTypes(pointer);
      }
    }

    /// <summary>
    /// Active view
    /// </summary>
    /// <since>6.0</since>
    public RhinoView View
    {
      get
      {
        var pointer = RhinoPageHooks.UnmanagedIRhinoPagePointerFromPage(Page);
        var sn = UnsafeNativeMethods.IRhinoPropertiesPanelPage_ViewRuntimeSerialNumber(pointer);
        return RhinoView.FromRuntimeSerialNumber(sn);
      }
    }

    /// <summary>
    /// Active viewport
    /// </summary>
    /// <since>6.0</since>
    public RhinoViewport Viewport
    {
      get
      {
        return View?.ActiveViewport;

        // This is causing a problem in the detail view object properties
        // page.  If you change the detail view scale this will return the
        // wrong viewport until you open a different model.
        //var pointer = RhinoPageHooks.UnmanagedIRhinoPagePointerFromPage(Page);
        //var id = UnsafeNativeMethods.IRhinoPropertiesPanelPage_ViewportId(pointer);
        //return id == Guid.Empty ? null : RhinoViewport.FromId(id);
      }
    }

    /// <summary>
    /// Document containing the objects and views
    /// </summary>
    /// <since>6.0</since>
    [CLSCompliant(false)]
    public uint DocRuntimeSerialNumber
    {
      get
      {
        var pointer = RhinoPageHooks.UnmanagedIRhinoPagePointerFromPage(Page);
        var sn = UnsafeNativeMethods.IRhinoPropertiesPanelPage_RhinoDocRuntimeSerialNumber(pointer);
        return sn;
      }
    }

    /// <summary>
    /// Document containing the objects and views
    /// </summary>
    /// <since>6.0</since>
    public RhinoDoc Document => RhinoDoc.FromRuntimeSerialNumber(DocRuntimeSerialNumber);
  }

  /// <summary>
  /// Passed to Rhino.PlugIns.PlugIn.ObjectPropertiesPages to allow a plug-in
  /// to add custom ObjectPropertiesPage pages to the Rhino properties panel.
  /// </summary>
  public class ObjectPropertiesPageCollection
  {
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="documentRuntimeSerialNumber">
    /// Document associated with the Rhino properties panel.
    /// </param>
    internal ObjectPropertiesPageCollection(uint documentRuntimeSerialNumber)
    {
      DocumentRuntimeSerialNumber = documentRuntimeSerialNumber;
    }

    /// <summary>
    /// Custom page to add
    /// </summary>
    /// <param name="page"></param>
    /// <since>7.0</since>
    public void Add(ObjectPropertiesPage page)
    {
      if (page == null)
        throw new ArgumentNullException(nameof(page));
      Pages.Add(page);
    }
    /// <summary>
    /// Add plug-in pages to this list
    /// </summary>
    internal List<ObjectPropertiesPage> Pages { get; } = new List<ObjectPropertiesPage>();

    /// <summary>
    /// Document associated with the Rhino properties panel.
    /// </summary>
    /// <since>7.0</since>
    [CLSCompliant(false)]
    public uint DocumentRuntimeSerialNumber { get; }

    /// <summary>
    /// Document associated with the Rhino properties panel.
    /// </summary>
    /// <since>7.0</since>
    public RhinoDoc Document => _doc ?? (_doc = RhinoDoc.FromRuntimeSerialNumber(DocumentRuntimeSerialNumber));
    private RhinoDoc _doc;
  }

  /// <summary>
  /// Base class used to add object property user interface panels
  /// </summary>
  public abstract class ObjectPropertiesPage
  {
    #region Virtual methods, should be made abstract when obsolete properties are removed
    /// <summary>
    /// Icon to display in the object properties tab control.  Will not get called
    /// if PageIconEmbeddedResourceString is overridden and provides a string for a 
    /// successfully loaded icon resource.
    /// </summary>
    /// <param name="sizeInPixels">
    /// The requested icon size in pixels, DPI scaling has been applied.  The
    /// default value is 24 X DPI scale.
    /// </param>
    /// <returns></returns>
    /// <since>6.0</since>
    public virtual System.Drawing.Icon PageIcon(System.Drawing.Size sizeInPixels)
    {
#pragma warning disable CS0618 // Type or member is obsolete
      // This provides backwards compatibility
      return Icon;
#pragma warning restore CS0618 // Type or member is obsolete
    }
    #endregion Virtual methods, should be made abstract when obsolete properties are removed

    #region Abstract properties changed to virtual properties and made obsolete
    /// <summary>
    /// (OBSOLETE - Override PageIcon instead)
    /// Icon to display in the object properties tab control
    /// </summary>
    /// <since>5.0</since>
    [Obsolete("Override PageIcon instead")]
    public virtual System.Drawing.Icon Icon { get; }
    #endregion Abstract properties changed to virtual properties and made obsolete

    #region Abstract properties
    ///<summary>
    /// The control that represents this page. Rhino Windows supports classes
    /// that implement the IWin32Windows interface, are derived from
    /// System.Windows.FrameworkElement or Eto.Forms.Control.  Mac Rhino
    /// supports controls that are derived from NSview or Eto.Forms.Control.
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
    /// English string used to describe this page
    /// </summary>
    /// <since>5.0</since>
    public abstract string EnglishPageTitle { get; }
    #endregion Abstract properties

    /// <summary>
    /// Override to specify which objects this page supports
    /// </summary>
    /// <since>6.0</since>
    [CLSCompliant(false)]
    public virtual ObjectType SupportedTypes => ObjectType.AnyObject;


    /// <summary>
    /// Returns false which only
    /// requires a single item of SupportedTypes to be selected.
    /// 
    /// Override if you wish to change the above behavior.
    /// </summary>
    /// <since>6.7</since>
    [CLSCompliant (false)]
    public virtual bool AllObjectsMustBeSupported => false;

    /// <summary>
    /// The page navigation control adds buttons in the order the pages are 
    /// processed, override this method and return a sort index to move the button
    /// to the beginning of the list.  By default this returns -1 which puts the
    /// button at the end of the list.
    /// </summary>
    /// <since>7.0</since>
    public virtual int Index => -1;

    #region Public virtual methods
    /// <summary>
    /// Called when the parent container is initially created.
    /// </summary>
    /// <param name="hwndParent"></param>
    /// <since>5.0</since>
    public virtual void OnCreateParent(IntPtr hwndParent) { }
    /// <summary>
    /// Called when the parent containers client rectangle size has changed and
    /// the PageControl has been resized.
    /// </summary>
    /// <param name="width"></param>
    /// <param name="height"></param>
    /// <since>5.0</since>
    public virtual void OnSizeParent(int width, int height) { }
    ///<summary>Called when this page is activated/deactivated.</summary>
    ///<param name="active">
    ///If true then this page is on top otherwise it is about to be hidden.
    ///</param>
    ///<returns>
    ///If true then the page is hidden and the requested page is not
    ///activated otherwise will not allow you to change the current page.
    ///Default returns true.  The return value is currently ignored.
    ///</returns>
    /// <since>5.0</since>
    public virtual bool OnActivate(bool active) { return true; }
    /// <summary>
    /// Called when the F1 key or help button is pressed, override to display
    /// plug-in specific help for this page.
    /// </summary>
    /// <since>5.0</since>
    public virtual void OnHelp() { }
    /// <summary>
    /// Called when the selected objects list changes, return true if the
    /// object list contains one or more object the page can modify.
    /// </summary>
    /// <param name="rhObj"></param>
    /// <returns></returns>
    /// <since>5.0</since>
    /// <deprecated>6.0</deprecated>
    [Obsolete("ShouldDisplay(RhinoObject rhObj) is obsolete, override ShouldDisplay(ObjectPropertiesPageEventArgs e) instead")]
    public virtual bool ShouldDisplay(RhinoObject rhObj)
    {
      var e = new ObjectPropertiesPageEventArgs(this);
      var value = e.IncludesObjectsType(SupportedTypes, AllObjectsMustBeSupported);
      return value;
    }
    /// <summary>
    /// Called when the selected objects list changes, return true if the
    /// object list contains one or more object the page can modify.
    /// </summary>
    /// <param name="e"></param>
    /// <returns></returns>
    /// <since>6.0</since>
    public virtual bool ShouldDisplay(ObjectPropertiesPageEventArgs e)
    {
#pragma warning disable 618
      return ShouldDisplay((RhinoObject)null);
#pragma warning restore 618
    }
    /// <summary>
    /// Called on the active page after the selected objects list has changed
    /// to notify the page to initialize its content to reflect the new
    /// object list.
    /// </summary>
    /// <param name="rhObj"></param>
    /// <since>5.0</since>
    /// <deprecated>6.0</deprecated>
    [Obsolete("InitializeControls is obsolete, override UpdatePage instead")]
    public virtual void InitializeControls(RhinoObject rhObj) { }

    /// <summary>
    /// Called on the active page after the selected objects list has changed
    /// to notify the page to initialize its content to reflect the new
    /// object list.
    /// </summary>
    /// <param name="e"></param>
    /// <since>6.0</since>
    public virtual void UpdatePage(ObjectPropertiesPageEventArgs e)
    {
#pragma warning disable 618
      // Call the deprecated method to avoid breaking the SDK
      InitializeControls(null);
#pragma warning restore 618
    }

    /// <summary>
    /// Call this method when the page is ready to modify the selected objects
    /// list.  Rhino will suspend UpdatePageNotfictaion, call the passed action
    /// then restore UpdatePageNotfictaion. 
    /// </summary>
    /// <param name="callbackAction">
    /// Called when it is safe to modify objects.
    /// </param>
    /// <since>6.0</since>
    public void ModifyPage(Action<ObjectPropertiesPageEventArgs> callbackAction)
    {
      RhinoPageHooks.ObjectPropertiesModifyPage(this, callbackAction);
    }

    /// <summary>
    /// If your object properties page supports sub-object selection, you
    /// should override this method and return true.  This is ignored for view
    /// pages.  The default implementation returns false.
    /// </summary>
    /// <since>6.0</since>
    public virtual bool SupportsSubObjects => false;

    #endregion Public virtual methods

    #region Public virtual properties
    /// <summary>
    /// Localized page description string, returns the EnglishPageTitle by
    /// default.
    /// </summary>
    /// <since>5.0</since>
    public virtual string LocalPageTitle => EnglishPageTitle;

    /// <summary>
    /// Override this and return the page you want to replace a specific object
    /// properties page.
    /// </summary>
    /// <since>5.11</since>
    public virtual PropertyPageType PageType => PropertyPageType.Custom;

    /// <summary>
    /// Resource string for a embedded icon resource in the assembly containing
    /// the page instance.  If this returns a valid resource and Rhino can
    /// load the icon the loaded icon will get used directly otherwise;
    /// the PageIcon method will get called.
    /// </summary>
    /// <value>The page icon embedded resource string.</value>
    /// <since>6.4</since>
    public virtual string PageIconEmbeddedResourceString => null;

    /// <summary>
    /// This method is called when scripting the Rhino Properties command and
    /// choosing this page.
    /// </summary>
    /// <param name="doc">Active <see cref="RhinoDoc"/></param>
    /// <param name="objectList">
    /// List of objects selected by the Properties command.
    /// </param>
    /// <returns></returns>
    /// <since>6.0</since>
    /// <deprecated>6.0</deprecated>
    [Obsolete("RunScript(RhinoDoc doc, RhinoObject[] objectList) is obsolete, override RunScript(ObjectPropertiesPageEventArgs e) instead")]
    public virtual Commands.Result RunScript(RhinoDoc doc, RhinoObject[] objectList)
    {
      RhinoApp.WriteLine(Localization.LocalizeString("Scripting not supported for this option", 30));
      Dialogs.ShowMessage(Localization.LocalizeString("Scripting not supported for this option", 31), Localization.LocalizeString("Unsupported Option", 32));
      return Commands.Result.Success;
    }

    /// <summary>
    /// This method is called when scripting the Rhino Properties command and
    /// choosing this page.
    /// </summary>
    /// <param name="e">
    /// Provides access to the selected object list and document.
    /// </param>
    /// <returns></returns>
    /// <since>6.0</since>
    public virtual Commands.Result RunScript(ObjectPropertiesPageEventArgs e)
    {
#pragma warning disable CS0618 // Type or member is obsolete
      // This provides backwards compatibility
      return RunScript(e.Document, e.Objects);
#pragma warning restore CS0618 // Type or member is obsolete
    }

    #endregion Public virtual properties

    #region Public properties
    /// <summary>
    /// Return a list of Rhino objects to be processed by this object properties page
    /// </summary>
    /// <since>5.11</since>
    public RhinoObject[] SelectedObjects => GetSelectedObjects(SupportedTypes);

    internal static ObjectType TypeFilter(Type type)
    {
      return ObjectPropertiesPageEventArgs.TypeFilter(type);
    }

    /// <summary>
    /// Return true if any of the selected objects match the given type
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    /// <since>6.0</since>
    public bool AnySelectedObject<T>() where T : RhinoObject
    {
      return AnySelectedObject<T>(AllObjectsMustBeSupported);
    }

    /// <summary>
    /// Return true if any of the selected objects match the given type
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="allMustMatch">
    /// If true then every selected object must match the object type
    /// otherwise; only a single object has to be of the specified type
    /// </param>
    /// <returns></returns>
    /// <since>6.7</since>
    public bool AnySelectedObject<T>(bool allMustMatch) where T : RhinoObject
    {
      var type = typeof (T);
      var filter = allMustMatch ? ObjectType.AnyObject : TypeFilter(type);

      // 9 July 2024 S. Baer (RH-82891)
      // Try to bail out as quick as possible by using the more efficient
      // RhinoDoc functions. This function could be further optimized to not
      // require any .NET looping through object lists.
      uint docSerialNumber = RhinoPageHooks.RhinoDocRuntimeSerialNumberFromPage(this);
      RhinoDoc doc = RhinoDoc.FromRuntimeSerialNumber(docSerialNumber);
      if (doc != null)
      {
        if (!doc.Objects.SelectedObjectsExist(filter, true))
          return false;

        var objectlist = doc.Objects.GetObjectList(new ObjectEnumeratorSettings() { ObjectTypeFilter = filter, SelectedObjectsFilter = true });
        IDisposable disposable = objectlist as IDisposable;
        bool listHasElements = false;
        foreach (var rhinoObject in objectlist)
        {
          listHasElements = true;
          var obj = rhinoObject as T;
          // If all objects must match and this object does not then return false
          if (allMustMatch && obj == null)
          {
            if (disposable != null)
              disposable.Dispose();
            return false;
          }
          // No requirement for all objects to match and this one does then just return true now
          if (!allMustMatch && obj != null)
          {
            if (disposable != null)
              disposable.Dispose();
            return true;
          }
        }
        if (disposable != null)
          disposable.Dispose();
        return allMustMatch && listHasElements;
      }

      // This code should never be hit anymore since doc should always exist
      var page_pointer = RhinoPageHooks.UnmanagedIRhinoPagePointerFromPage(this);
      var count = 0;
      var array_pointer = UnsafeNativeMethods.IRhinoPropertiesPanelPage_GetObjects(page_pointer, (uint)filter, ref count);
      for (var i = 0; i < count; i++)
      {
        var pointer = UnsafeNativeMethods.IRhinoPropertiesPanelPage_GetObjectsAt(array_pointer, i);
        if (pointer == IntPtr.Zero)
          continue;
        var obj = RhinoObject.CreateRhinoObjectHelper(pointer) as T;
        // If all objects must match and this object does not then return false
        if (allMustMatch && obj == null)
          return false;
        // No requirement for all objects to match and this one does then just return true now
        if (!allMustMatch && obj != null)
          return true;
      }
      return allMustMatch && count > 0;
    }

    /// <summary>
    /// Get selected objects of a given type
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    /// <since>6.0</since>
    public T[] GetSelectedObjects<T>() where T : RhinoObject
    {
      var type = typeof(T);
      var filter = TypeFilter(type);

      var page_pointer = RhinoPageHooks.UnmanagedIRhinoPagePointerFromPage(this);
      var object_count = 0;
      var array_pointer = UnsafeNativeMethods.IRhinoPropertiesPanelPage_GetObjects(page_pointer, (uint)filter, ref object_count);
      var rc = new List<T>();

      for (var i = 0; i < object_count; i++)
      {
        var pointer = UnsafeNativeMethods.IRhinoPropertiesPanelPage_GetObjectsAt(array_pointer, i);
        if (pointer == IntPtr.Zero)
          continue;
        var rhino_object = RhinoObject.CreateRhinoObjectHelper(pointer) as T;
        if (rhino_object != null)
          rc.Add(rhino_object);
      }

      UnsafeNativeMethods.IRhinoPropertiesPanelPage_GetObjectsClear(array_pointer);

      return rc.ToArray();
    }

    /// <summary>
    /// Get selected objects that match a given filter
    /// </summary>
    /// <param name="filter"></param>
    /// <returns></returns>
    /// <since>6.0</since>
    [CLSCompliant(false)]
    public RhinoObject[] GetSelectedObjects(ObjectType filter)
    {
      var page_pointer = RhinoPageHooks.UnmanagedIRhinoPagePointerFromPage(this);
      var object_count = 0;
      var array_pointer = UnsafeNativeMethods.IRhinoPropertiesPanelPage_GetObjects(page_pointer, (uint)filter, ref object_count);
      var rc = new List<RhinoObject>();

      for (var i = 0; i < object_count; i++)
      {
        var pointer = UnsafeNativeMethods.IRhinoPropertiesPanelPage_GetObjectsAt(array_pointer, i);
        if (pointer == IntPtr.Zero)
          continue;
        var rhino_object = RhinoObject.CreateRhinoObjectHelper(pointer);
        if (rhino_object != null)
          rc.Add(rhino_object);
      }

      UnsafeNativeMethods.IRhinoPropertiesPanelPage_GetObjectsClear(array_pointer);

      return rc.ToArray();
    }
    #endregion Public properties
  }
}
#endif
