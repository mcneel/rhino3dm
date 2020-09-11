#pragma warning disable 1591
using System;
using Rhino.Geometry;
using Rhino.DocObjects;

#if RHINO_SDK
namespace Rhino.Input.Custom
{
  public delegate bool GetObjectGeometryFilter(RhinoObject rhObject, GeometryBase geometry, ComponentIndex componentIndex);

  /// <summary>
  /// The GetObject class is the tool commands use to interactively select objects.
  /// </summary>
  /// <example>
  /// GetObject go = new GetObject();
  /// go.GetObjects(1,0);
  /// if( go.CommandResult() != Command.Result.Success )
  ///    ... use canceled or some other type of input was provided
  /// int object_count = go.ObjectCount();
  /// for( int i=0; i&lt;object_count; i++ )
  /// {
  ///   ObjectReference objref = go.Object(i);
  ///   ON_Geometry geo = objref.Geometry();
  ///   ...
  /// }
  /// </example>
  public class GetObject : GetBaseClass
  {
    /// <summary>
    /// Get the currently running GetObject for a given document
    /// </summary>
    /// <param name="doc"></param>
    /// <returns></returns>
    /// <since>6.3</since>
    public static GetObject ActiveGetObject(RhinoDoc doc)
    {
      IntPtr ptrGetObject = UnsafeNativeMethods.CRhinoGetObject_ActiveGetObject(doc.RuntimeSerialNumber);
      if (ptrGetObject == IntPtr.Zero)
        return null;
      if (g_active_go != null && g_active_go.ConstPointer() == ptrGetObject)
        return g_active_go;
      return new GetObject(ptrGetObject, false, false);
    }

    /// <example>
    /// <code source='examples\vbnet\ex_addobjectstogroup.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_addobjectstogroup.cs' lang='cs'/>
    /// <code source='examples\py\ex_addobjectstogroup.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    public GetObject()
    {
      IntPtr ptr = UnsafeNativeMethods.CRhinoGetObject_New();
      Construct(ptr);
    }

    internal GetObject(IntPtr pConstGetObject)
    {
      Construct(pConstGetObject, true, false);
    }

    internal GetObject(IntPtr pGetObject, bool isConst, bool deleteOnDispose)
    {
      Construct(pGetObject, isConst, deleteOnDispose);
    }

    /// <summary>
    /// The geometry type filter controls which types of geometry
    /// (points, curves, surfaces, meshes, etc.) can be selected.
    /// The default geometry type filter permits selection of all
    /// types of geometry.
    /// NOTE: the filter can be a bitwise combination of multiple ObjectTypes.
    /// </summary>
    /// <example>
    /// <code source='examples\vbnet\ex_orientonsrf.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_orientonsrf.cs' lang='cs'/>
    /// <code source='examples\py\ex_orientonsrf.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    [CLSCompliant(false)]
    public ObjectType GeometryFilter
    {
      get
      {
        IntPtr ptr = NonConstPointer();
        uint rc = UnsafeNativeMethods.CRhinoGetObject_GetSetGeometryFilter(ptr, false, 0);
        return (ObjectType)rc;
      }
      set
      {
        IntPtr ptr = NonConstPointer();
        UnsafeNativeMethods.CRhinoGetObject_GetSetGeometryFilter(ptr, true, (uint)value);
      }
    }

    /// <summary>
    /// The geometry attribute filter provides a secondary filter that
    /// can be used to restrict which objects can be selected. Control
    /// of the type of geometry (points, curves, surfaces, meshes, etc.)
    /// is provided by GetObject.SetGeometryFilter. The geometry attribute
    /// filter is used to require the selected geometry to have certain
    /// attributes (open, closed, etc.). The default attribute filter
    /// permits selection of all types of geometry.
    /// </summary>
    /// <example>
    /// <code source='examples\vbnet\ex_circlecenter.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_circlecenter.cs' lang='cs'/>
    /// <code source='examples\py\ex_circlecenter.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    [CLSCompliant(false)]
    public GeometryAttributeFilter GeometryAttributeFilter
    {
      get
      {
        IntPtr ptr = NonConstPointer();
        uint filter = UnsafeNativeMethods.CRhinoGetObject_GetSetGeometryAttrFilter(ptr, false, 0);
        return (GeometryAttributeFilter)filter;
      }
      set
      {
        IntPtr ptr = NonConstPointer();
        UnsafeNativeMethods.CRhinoGetObject_GetSetGeometryAttrFilter(ptr, true, (uint)value);
      }
    }

    /// <summary>
    /// Checks geometry to see if it can be selected.
    /// Override to provide fancy filtering.
    /// </summary>
    /// <param name="rhObject">parent object being considered.</param>
    /// <param name="geometry">geometry being considered.</param>
    /// <param name="componentIndex">
    /// if >= 0, geometry is a proper sub-part of object->Geometry() with componentIndex.
    /// </param>
    /// <returns>
    /// The default returns true unless you've set a custom geometry filter. If a custom
    /// filter has been set, that delegate is called.
    /// </returns>
    /// <remarks>The delegate should not throw exceptions. If an exception is thrown, a message box will show and
    /// the filter will be disabled.</remarks>
    /// <since>5.0</since>
    public virtual bool CustomGeometryFilter(RhinoObject rhObject, GeometryBase geometry, ComponentIndex componentIndex)
    {
      if (m_filter != null)
      {
        try
        {
          return m_filter(rhObject, geometry, componentIndex);
        }
        catch (Exception ex)
        {
          System.Windows.Forms.MessageBox.Show(
          "The CustomGeometryFilter() function threw an uncaught exception. It will be disabled." +
          "Details: \n\n" +
          $"Type: {ex.GetType().FullName} \n" +
          $"Message: {ex.Message} \n" +
          $"StackTrace: \n{ex.StackTrace}"
          ,
          "CustomGeometryFilter exception boundary");

          //we need to stop the exception from repeating.
          m_filter = null;
        }
      }
      return true;
    }

    GetObjectGeometryFilter m_filter;

    /// <summary>
    /// Set filter callback function that will be called by the CustomGeometryFilter
    /// </summary>
    /// <param name="filter"></param>
    /// <example>
    /// <code source='examples\vbnet\ex_customgeometryfilter.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_customgeometryfilter.cs' lang='cs'/>
    /// <code source='examples\py\ex_customgeometryfilter.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    public void SetCustomGeometryFilter(GetObjectGeometryFilter filter)
    {
      m_filter = filter;
    }

    /// <summary>
    /// Checks geometry to see if it passes the basic GeometryAttributeFilter.
    /// </summary>
    /// <param name="rhObject">parent object being considered.</param>
    /// <param name="geometry">geometry being considered.</param>
    /// <param name="componentIndex">if >= 0, geometry is a proper sub-part of object->Geometry() with componentIndex.</param>
    /// <returns>
    /// true if the geometry passes the filter returned by GeometryAttributeFilter().
    /// </returns>
    /// <since>5.0</since>
    public bool PassesGeometryAttributeFilter(RhinoObject rhObject, GeometryBase geometry, ComponentIndex componentIndex)
    {
      IntPtr const_ptr_rhino_object = IntPtr.Zero;
      if (rhObject != null)
        const_ptr_rhino_object = rhObject.ConstPointer();
      IntPtr const_ptr_geometry = IntPtr.Zero;
      if (geometry != null)
        const_ptr_geometry = geometry.ConstPointer();
      IntPtr ptr = NonConstPointer();
      return UnsafeNativeMethods.CRhinoGetObject_PassesGeometryAttributeFilter(ptr, const_ptr_rhino_object, const_ptr_geometry, componentIndex);
    }

    /// <summary>
    /// Control the pre-selection behavior GetObjects.
    /// </summary>
    /// <param name="enable">if true, pre-selection is enabled.</param>
    /// <param name="ignoreUnacceptablePreselectedObjects">
    /// If true and some acceptable objects are pre-selected, then any unacceptable
    /// pre-selected objects are ignored. If false and any unacceptable are pre-selected,
    /// then the user is forced to post-select.
    /// </param>
    /// <remarks>
    /// By default, if valid input is pre-selected when GetObjects() is called, then that input
    /// is returned and the user is not given the opportunity to post-select. If you want
    /// to force the user to post-select, then call EnablePreSelect(false).
    /// </remarks>
    /// <since>5.0</since>
    public void EnablePreSelect(bool enable, bool ignoreUnacceptablePreselectedObjects)
    {
      IntPtr ptr = NonConstPointer();
      UnsafeNativeMethods.CRhinoGetObject_EnablePreSelect(ptr, enable, ignoreUnacceptablePreselectedObjects);
    }
    /// <since>5.0</since>
    public void DisablePreSelect()
    {
      EnablePreSelect(false, true);
    }

    bool GetBool(UnsafeNativeMethods.RhinoGetObjectBoolConsts which)
    {
      IntPtr ptr = NonConstPointer();
      return UnsafeNativeMethods.CRhinoGetObject_GetSetBool(ptr, which, false, false);
    }
    void SetBool(UnsafeNativeMethods.RhinoGetObjectBoolConsts which, bool setValue)
    {
      IntPtr ptr = NonConstPointer();
      UnsafeNativeMethods.CRhinoGetObject_GetSetBool(ptr, which, true, setValue);
    }

    /// <summary>
    /// Control the availability of post selection in GetObjects.
    /// </summary>
    /// <remarks>
    /// By default, if no valid input is pre-selected when GetObjects is called, then
    /// the user is given the chance to post select. If you want to force the user to pre-select,
    /// then call EnablePostSelect(false).
    /// </remarks>
    /// <since>5.0</since>
    public void EnablePostSelect(bool enable)
    {
      SetBool(UnsafeNativeMethods.RhinoGetObjectBoolConsts.EnablePostSelect, enable);
    }

    /// <summary>
    /// true if pre-selected input will be deselected before
    /// post-selection begins when no pre-selected input is valid.
    /// </summary>
    /// <example>
    /// <code source='examples\vbnet\ex_orientonsrf.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_orientonsrf.cs' lang='cs'/>
    /// <code source='examples\py\ex_orientonsrf.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    public bool DeselectAllBeforePostSelect
    {
      get { return GetBool(UnsafeNativeMethods.RhinoGetObjectBoolConsts.DeselectAllBeforePostSelect); }
      set { SetBool(UnsafeNativeMethods.RhinoGetObjectBoolConsts.DeselectAllBeforePostSelect, value); }
    }

    /// <summary>
    /// In one-by-one post selection, the user is forced
    /// to select objects by post picking them one at a time.
    /// </summary>
    /// <example>
    /// <code source='examples\vbnet\ex_orientonsrf.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_orientonsrf.cs' lang='cs'/>
    /// <code source='examples\py\ex_orientonsrf.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    public bool OneByOnePostSelect
    {
      get { return GetBool(UnsafeNativeMethods.RhinoGetObjectBoolConsts.OneByOnePostSelect); }
      set { SetBool(UnsafeNativeMethods.RhinoGetObjectBoolConsts.OneByOnePostSelect, value); }
    }

    /// <summary>
    /// By default, GetObject.Input will permit a user to select
    /// sub-objects (like a curve in a b-rep or a curve in a group).
    /// If you only want the user to select "top" level objects,
    /// then call EnableSubObjectSelect = false.
    /// </summary>
    /// <example>
    /// <code source='examples\vbnet\ex_orientonsrf.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_orientonsrf.cs' lang='cs'/>
    /// <code source='examples\py\ex_orientonsrf.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    public bool SubObjectSelect
    {
      get { return GetBool(UnsafeNativeMethods.RhinoGetObjectBoolConsts.SubObjectSelect); }
      set { SetBool(UnsafeNativeMethods.RhinoGetObjectBoolConsts.SubObjectSelect, value); }
    }

    /// <summary>
    /// By default, if a call to Input is permitted to select different parts
    /// of the same object, like a polysurface and an edge of that polysurface,
    /// then the top-most object is automatically selected. If you want the
    /// choose-one-object mechanism to include pop up in these cases, then call
    /// EnableChooseOneQuestion = true before calling GetObjects().
    /// </summary>
    /// <since>5.0</since>
    public bool ChooseOneQuestion
    {
      get { return GetBool(UnsafeNativeMethods.RhinoGetObjectBoolConsts.ChooseOneQuestion); }
      set { SetBool(UnsafeNativeMethods.RhinoGetObjectBoolConsts.ChooseOneQuestion, value); }
    }

    /// <summary>
    /// By default, if a call to Input is permitted to select different parts of
    /// the same object, like a polysurface, a surface and an edge, then the
    /// top-most object is preferred. (polysurface beats face beats edge). If
    /// you want the bottom most object to be preferred, then call 
    /// EnableBottomObjectPreference = true before calling GetObjects().
    /// </summary>
    /// <since>5.0</since>
    public bool BottomObjectPreference
    {
      get { return GetBool(UnsafeNativeMethods.RhinoGetObjectBoolConsts.BottomObjectPreference); }
      set { SetBool(UnsafeNativeMethods.RhinoGetObjectBoolConsts.BottomObjectPreference, value); }
    }

    /// <summary>
    /// By default, groups are ignored in GetObject. If you want your call to
    /// GetObjects() to select every object in a group that has any objects
    /// selected, then enable group selection.
    /// </summary>
    /// <example>
    /// <code source='examples\vbnet\ex_orientonsrf.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_orientonsrf.cs' lang='cs'/>
    /// <code source='examples\py\ex_orientonsrf.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    public bool GroupSelect
    {
      get { return GetBool(UnsafeNativeMethods.RhinoGetObjectBoolConsts.GroupSelect); }
      set { SetBool(UnsafeNativeMethods.RhinoGetObjectBoolConsts.GroupSelect, value); }
    }

    /// <summary>
    /// If a subd (or a subd component) cannot be selected, but a brep (or brep
    /// component) can be selected, then automatically create and use a proxy brep.
    /// </summary>
    /// <since>7.0</since>
    public bool ProxyBrepFromSubD
    {
      get { return GetBool(UnsafeNativeMethods.RhinoGetObjectBoolConsts.ProxyBrepFromSubD); }
      set { SetBool(UnsafeNativeMethods.RhinoGetObjectBoolConsts.ProxyBrepFromSubD, value); }
    }

    /// <summary>
    /// By default, objects in inactive details are not permitted to be picked.
    /// In a few rare cases this is used (ex. picking circles during DimRadius)
    /// </summary>
    /// <since>5.8</since>
    public bool InactiveDetailPickEnabled
    {
      get { return GetBool(UnsafeNativeMethods.RhinoGetObjectBoolConsts.InactiveDetailPick); }
      set { SetBool(UnsafeNativeMethods.RhinoGetObjectBoolConsts.InactiveDetailPick, value); }
    }

    /// <summary>
    /// By default, any object selected during a command becomes part of the
    /// "previous selection set" and can be reselected by the SelPrev command.
    /// If you need to select objects but do not want them to be selected by
    /// a subsequent call to SelPrev, then call EnableSelPrev = false.
    /// </summary>
    /// <since>5.0</since>
    public void EnableSelPrevious(bool enable)
    {
      SetBool(UnsafeNativeMethods.RhinoGetObjectBoolConsts.SelPrev, enable);
    }

    /// <summary>
    /// By default, any object post-pick selected by GetObjects() is highlighted.
    /// If you want to post-pick objects and not have them automatically highlight,
    /// then call EnableHighlight = false.
    /// </summary>
    /// <since>5.0</since>
    public void EnableHighlight(bool enable)
    {
      SetBool(UnsafeNativeMethods.RhinoGetObjectBoolConsts.Highlight, enable);
    }

    /// <summary>
    /// By default, reference objects can be selected. If you do not want to be
    /// able to select reference objects, then call EnableReferenceObjectSelect=false.
    /// </summary>
    /// <example>
    /// <code source='examples\vbnet\ex_createblock.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_createblock.cs' lang='cs'/>
    /// <code source='examples\py\ex_createblock.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    public bool ReferenceObjectSelect
    {
      get { return GetBool(UnsafeNativeMethods.RhinoGetObjectBoolConsts.ReferenceObjectSelect); }
      set { SetBool(UnsafeNativeMethods.RhinoGetObjectBoolConsts.ReferenceObjectSelect, value); }
    }

    /// <summary>
    /// By default, post selection will select objects with grips on. If you do
    /// not want to be able to post select objects with grips on, then call
    /// EnableIgnoreGrips = false. The ability to preselect an object with grips
    /// on is determined by the value returned by the virtual
    /// RhinoObject.IsSelectableWithGripsOn.
    /// </summary>
    /// <since>5.0</since>
    public void EnableIgnoreGrips(bool enable)
    {
      SetBool(UnsafeNativeMethods.RhinoGetObjectBoolConsts.IgnoreGrips, enable);
    }

    /// <summary>
    /// By default, when GetObject.GetObjects is called with minimumNumber > 0
    /// and maximumNumber = 0, the command prompt automatically includes "Press Enter
    /// when done" after the user has selected at least minimumNumber of objects. If
    /// you want to prohibit the addition of the "Press Enter when done", then call
    /// EnablePressEnterWhenDonePrompt = false;
    /// </summary>
    /// <since>5.0</since>
    public void EnablePressEnterWhenDonePrompt(bool enable)
    {
      SetBool(UnsafeNativeMethods.RhinoGetObjectBoolConsts.PressEnterWhenDonePrompt, enable);
    }

    /// <summary>
    /// The default prompt when EnablePressEnterWhenDonePrompt is enabled is "Press Enter
    /// when done". Use this function to specify a different string to be appended.
    /// </summary>
    /// <param name="prompt">The text that will be displayed just after the prompt,
    /// after the selection has been made.</param>
    /// <since>5.0</since>
    public void SetPressEnterWhenDonePrompt(string prompt)
    {
      IntPtr ptr = NonConstPointer();
      UnsafeNativeMethods.CRhinoGetObject_SetPressEnterWhenDonePrompt(ptr, prompt);
    }

    /// <summary>
    /// Allow selecting objects that are already selected. By default, GetObjects() disallows
    /// selection of objects that are already selected to avoid putting the same object
    /// in the selection set more than once. Calling EnableAlreadySelectedObjectSelect = true
    /// overrides that restriction and allows selected objects to be selected and
    /// returned by GetObjects. This is useful because, coupled with the return immediately
    /// mode of GetObjects(1, -1), it is possible to select a selected object to deselect
    /// when the selected objects are being managed outside GetObjects() as in the case of
    /// CRhinoPolyEdge::GetEdge().
    /// </summary>
    /// <since>5.0</since>
    public bool AlreadySelectedObjectSelect
    {
      get { return GetBool(UnsafeNativeMethods.RhinoGetObjectBoolConsts.AlreadySelectedObjectSelect); }
      set { SetBool(UnsafeNativeMethods.RhinoGetObjectBoolConsts.AlreadySelectedObjectSelect, value); }
    }

    internal delegate bool GeometryFilterCallback(IntPtr rhObject, IntPtr geometry, ComponentIndex componentIndex);
    internal static GetObject g_active_go; // = null; [runtime default]

    private static bool CustomGeometryFilter(IntPtr rhObject, IntPtr ptrGeometry, ComponentIndex componentIndex)
    {
      bool rc = true;
      if (g_active_go != null)
      {
        try
        {
          RhinoObject rh_object = RhinoObject.CreateRhinoObjectHelper(rhObject);
          using (var or = new ObjRef(rh_object, ptrGeometry))
          {
            GeometryBase geom = or.Geometry();
            rc = g_active_go.CustomGeometryFilter(rh_object, geom, componentIndex);
          }
        }
        catch (Exception ex)
        {
          Runtime.HostUtils.ExceptionReport(ex);
        }
      }
      return rc;
    }

    /// <summary>
    /// Call to select a single object.
    /// </summary>
    /// <returns>
    /// GetResult.Object if an object was selected.
    /// GetResult.Cancel if the user pressed ESCAPE to cancel the selection.
    /// See GetResults for other possible values that may be returned when options, numbers,
    /// etc., are acceptable responses.
    /// </returns>
    /// <since>5.0</since>
    [CLSCompliant(false)]
    public GetResult Get()
    {
      return GetMultiple(1, 1);
    }

    /// <summary>Call to select objects.</summary>
    /// <param name="minimumNumber">minimum number of objects to select.</param>
    /// <param name="maximumNumber">
    /// maximum number of objects to select.
    /// If 0, then the user must press enter to finish object selection.
    /// If -1, then object selection stops as soon as there are at least minimumNumber of object selected.
    /// If >0, then the picking stops when there are maximumNumber objects.  If a window pick, crossing
    /// pick, or Sel* command attempts to add more than maximumNumber, then the attempt is ignored.
    /// </param>
    /// <returns>
    /// GetResult.Object if one or more objects were selected.
    /// GetResult.Cancel if the user pressed ESCAPE to cancel the selection.
    /// See GetResults for other possible values that may be returned when options, numbers,
    /// etc., are acceptable responses.
    /// </returns>
    /// <example>
    /// <code source='examples\vbnet\ex_addobjectstogroup.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_addobjectstogroup.cs' lang='cs'/>
    /// <code source='examples\py\ex_addobjectstogroup.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    [CLSCompliant(false)]
    public GetResult GetMultiple(int minimumNumber, int maximumNumber)
    {
      GetObject old = g_active_go;
      g_active_go = this;
      GeometryFilterCallback cb = null;
      Type t = GetType();
      // Hook up CustomGeometryFilter virtual function if this is a subclass. This way we
      // don't have to pin anything and this class will get collected on the next appropriate GC
      if (m_filter != null || t.IsSubclassOf(typeof(GetObject)))
        cb = CustomGeometryFilter;

      IntPtr ptr = NonConstPointer();
      uint rc = UnsafeNativeMethods.CRhinoGetObject_GetObjects(ptr, minimumNumber, maximumNumber, cb);

      g_active_go = old;
      return (GetResult)rc;
    }

    /// <summary>
    /// Gets the number of objects that were selected.
    /// </summary>
    /// <since>5.0</since>
    public int ObjectCount
    {
      get
      {
        IntPtr ptr = NonConstPointer();
        return UnsafeNativeMethods.CRhinoGetObject_ObjectCount(ptr);
      }
    }

    /// <example>
    /// <code source='examples\vbnet\ex_orientonsrf.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_orientonsrf.cs' lang='cs'/>
    /// <code source='examples\py\ex_orientonsrf.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    public ObjRef Object(int index)
    {
      ObjRef rc = new ObjRef();
      IntPtr ptr = NonConstPointer();
      UnsafeNativeMethods.CRhinoGetObject_Object(ptr, index, rc.NonConstPointer());
      return rc;
    }

    /// <since>5.0</since>
    public ObjRef[] Objects()
    {
      int count = ObjectCount;
      Collections.RhinoList<ObjRef> objrefs = new Collections.RhinoList<ObjRef>(count);
      for (int i = 0; i < count; i++)
      {
        ObjRef objref = Object(i);
        objrefs.Add(objref);
      }
      return objrefs.ToArray();
    }

    /// <since>5.0</since>
    public bool ObjectsWerePreselected
    {
      get { return GetBool(UnsafeNativeMethods.RhinoGetObjectBoolConsts.ObjectsWerePreselected); }
    }

    /// <summary>
    /// Each instance of GetObject has a unique runtime serial number that
    /// is used to identify object selection events associated with that instance.
    /// </summary>
    /// <since>5.0</since>
    [CLSCompliant(false)]
    public uint SerialNumber
    {
      get
      {
        IntPtr ptr = NonConstPointer();
        return UnsafeNativeMethods.CRhinoGetObject_SerialNumber(ptr);
      }
    }

    /// <summary>
    /// By default the picked object list is cleared when GetObject.GetObjects() is called.
    /// If you are reusing a GetObject class and do not want the existing object list
    /// cleared when you call Input, then call EnableClearObjectsOnEntry(false) before
    /// calling GetObjects().
    /// </summary>
    /// <param name="enable">The state to set.</param>
    /// <since>5.0</since>
    public void EnableClearObjectsOnEntry(bool enable)
    {
      SetBool(UnsafeNativeMethods.RhinoGetObjectBoolConsts.ClearObjectsOnEntry, enable);
    }

    /// <summary>
    /// By default any objects in the object list are unselected when GetObject.GetObjects()
    /// exits with any return code besides Object. If you want to leave the objects
    /// selected when non-object input is returned, then call EnableUnselectObjectsOnExit(false)
    /// before calling GetObjects().
    /// </summary>
    /// <param name="enable">The state to set.</param>
    /// <since>5.0</since>
    public void EnableUnselectObjectsOnExit(bool enable)
    {
      SetBool(UnsafeNativeMethods.RhinoGetObjectBoolConsts.UnselectObjectsOnExit, enable);
    }

    /// <summary>
    /// </summary>
    /// <param name="objref"></param>
    /// <since>6.3</since>
    public void AppendToPickList(ObjRef objref)
    {
      IntPtr ptrThis = NonConstPointer();
      IntPtr constPtrObjRef = objref.ConstPointer();
      UnsafeNativeMethods.CRhinoGetObject_AppendToPickList(ptrThis, constPtrObjRef);
    }

    /// <summary>
    /// Clear possible special object drawing
    /// </summary>
    /// <since>6.12</since>
    public void ClearObjects()
    {
      IntPtr ptrThis = NonConstPointer();
      UnsafeNativeMethods.CRhinoGetObject_ClearObjects(ptrThis);
    }


  }

  /// <summary>
  /// If an object passes the geometry TYPE filter, then the geometry ATTRIBUTE
  /// filter is applied.
  /// </summary>
  /// <since>5.0</since>
  [Flags, CLSCompliant(false)]
  public enum GeometryAttributeFilter : uint
  {
    /// <summary>
    /// 3d wire curve
    /// If you want to accept only wire or edge curves, then
    /// specify wire_curve or edge_curve, otherwise both wire
    /// and edge curves will pass the attribute filter.
    /// </summary>
    WireCurve = 1<<0,
    /// <summary>
    /// 3d curve of a surface edge
    /// If you want to accept only wire or edge curves, then
    /// specify wire_curve or edge_curve, otherwise both wire
    /// and edge curves will pass the attribute filter.
    /// </summary>
    EdgeCurve = 1 << 1,
    /// <summary>
    /// Closed Curves and Edges are acceptable
    /// If you want to accept only closed or open curves, then
    /// specify either closed_curve or open_curve.  Otherwise both
    /// closed and open curves will pass the attribute filter.
    /// </summary>
    ClosedCurve = 1<<2,
    /// <summary>
    /// Open Curves and Edges are acceptable
    /// If you want to accept only closed or open curves, then
    /// specify either closed_curve or open_curve.  Otherwise both
    /// closed and open curves will pass the attribute filter.
    /// </summary>
    OpenCurve = 1 << 3,
    /// <summary>
    /// seam edges are acceptable
    /// attributes of acceptable trimming edge objects (associated with an ON_BrepTrim).
    ///
    /// If none of these attributes are explicitly specified, then
    /// any kind of trimming edge will pass the attribute filter.
    /// </summary>
    SeamEdge = 1<<4,
    /// <summary>
    /// edges with 2 different surfaces pass
    /// attributes of acceptable trimming edge objects (associated with an ON_BrepTrim).
    ///
    /// If none of these attributes are explicitly specified, then
    /// any kind of trimming edge will pass the attribute filter.
    /// </summary>
    ManifoldEdge = 1 << 5,
    /// <summary>
    /// edges with 3 or more surfaces pass
    /// attributes of acceptable trimming edge objects (associated with an ON_BrepTrim).
    ///
    /// If none of these attributes are explicitly specified, then
    /// any kind of trimming edge will pass the attribute filter.
    /// </summary>
    NonmanifoldEdge = 1 << 6,
    /// <summary>
    /// any mated edge passes
    /// attributes of acceptable trimming edge objects (associated with an ON_BrepTrim).
    ///
    /// If none of these attributes are explicitly specified, then
    /// any kind of trimming edge will pass the attribute filter.
    /// </summary>
    MatedEdge = (1 << 4) | (1 << 5) | (1 << 6),
    /// <summary>
    /// boundary edges on surface sides pass
    /// attributes of acceptable trimming edge objects (associated with an ON_BrepTrim).
    ///
    /// If none of these attributes are explicitly specified, then
    /// any kind of trimming edge will pass the attribute filter.
    /// </summary>
    SurfaceBoundaryEdge = 1 << 7,
    /// <summary>
    /// boundary edges that trim a surface pass
    /// attributes of acceptable trimming edge objects (associated with an ON_BrepTrim).
    ///
    /// If none of these attributes are explicitly specified, then
    /// any kind of trimming edge will pass the attribute filter.
    /// </summary>
    TrimmingBoundaryEdge = 1 << 8,
    /// <summary>
    /// ant boundary edge passes
    /// attributes of acceptable trimming edge objects (associated with an ON_BrepTrim).
    ///
    /// If none of these attributes are explicitly specified, then
    /// any kind of trimming edge will pass the attribute filter.
    /// </summary>
    BoundaryEdge = (1 << 7) | (1 << 8),
    ///<summary>
    /// If you want to accept only closed or open surfaces, then
    /// specify either closed_surface or open_surface.  Otherwise both
    /// closed and open surfaces will pass the attribute filter.
    ///</summary>
    ClosedSurface = 1 << 9,
    ///<summary>
    /// If you want to accept only closed or open surfaces, then
    /// specify either closed_surface or open_surface.  Otherwise both
    /// closed and open surfaces will pass the attribute filter.
    ///</summary>
    OpenSurface = 1 << 10,
    ///<summary>
    /// If you want to accept only trimmed or untrimmed surfaces, then
    /// specify either trimmed_surface or untrimmed_surface.  Otherwise
    /// both trimmed and untrimmed surfaces will pass the attribute filter.
    ///</summary>
    TrimmedSurface = 1 << 11,
    ///<summary>
    /// If you want to accept only trimmed or untrimmed surfaces, then
    /// specify either trimmed_surface or untrimmed_surface.  Otherwise
    /// both trimmed and untrimmed surfaces will pass the attribute filter.
    ///</summary>
    UntrimmedSurface = 1 << 12,
    ///<summary>
    /// If you want to accept only sub-surfaces of (multi-surface)
    /// polysurface, then specify sub_surface.  If you do not want to
    /// accept sub-surfaces, then specify top_surface.  Otherwise
    /// sub-surfaces and top surfaces will pass the attribute filter.
    ///</summary>
    SubSurface = 1 << 13,
    ///<summary>
    /// If you want to accept only sub-surfaces of (multi-surface)
    /// polysurface, then specify sub_surface.  If you do not want to
    /// accept sub-surfaces, then specify top_surface.  Otherwise
    /// sub-surfaces and top surfaces will pass the attribute filter.
    ///</summary>
    TopSurface = 1 << 14,
    ///<summary>
    /// If you want to accept only manifold or non-manifold polysurfaces,
    /// then specify manifold_polysrf or nonmanifold_polysrf. Otherwise
    /// both manifold and non-manifold polysurfaces will pass the attribute
    /// filter.
    ///</summary>
    ManifoldPolysrf = 1 << 15,
    ///<summary>
    /// If you want to accept only manifold or non-manifold polysurfaces,
    /// then specify manifold_polysrf or nonmanifold_polysrf. Otherwise
    /// both manifold and non-manifold polysurfaces will pass the attribute
    /// filter.
    ///</summary>
    NonmanifoldPolysrf = 1 << 16,
    ///<summary>
    /// If you want to accept only closed or open polysurfaces, then
    /// specify either closed_polysrf or open_polysrf.  Otherwise both
    /// closed and open polysurfaces will pass the attribute filter.
    ///</summary>
    ClosedPolysrf = 1 << 17,
    ///<summary>
    /// If you want to accept only closed or open polysurfaces, then
    /// specify either closed_polysrf or open_polysrf.  Otherwise both
    /// closed and open polysurfaces will pass the attribute filter.
    ///</summary>
    OpenPolysrf = 1 << 18,
    ///<summary>
    /// If you want to accept only closed or open meshes, then
    /// specify either closed_mesh or open_mesh.  Otherwise both
    /// closed and open meshes will pass the attribute filter.
    ///</summary>
    ClosedMesh = 1 << 19,
    ///<summary>
    /// If you want to accept only closed or open meshes, then
    /// specify either closed_mesh or open_mesh.  Otherwise both
    /// closed and open meshes will pass the attribute filter.
    ///</summary>
    OpenMesh = 1 << 20,
    ///<summary>all trimming edges are boundary edges.</summary>
    BoundaryInnerLoop = 1 << 21,
    ///<summary>all trimming edges are mated.</summary>
    MatedInnerLoop = 1 << 22,
    ///<summary>any inner loop is acceptable.</summary>
    InnerLoop = (1 << 21) | (1 << 22),
    ///<summary>all trimming edges are boundary edges.</summary>
    BoundaryOuterLoop = 1 << 23,
    ///<summary>all trimming edges are mated.</summary>
    MatedOuterLoop = 1 << 24,
    ///<summary>any outer loop is acceptable.</summary>
    OuterLoop = (1 << 23) | (1 << 24),
    ///<summary>slit, curve-on-surface, point-on-surface, etc.</summary>
    SpecialLoop = (1 << 25),
    AcceptAllAttributes = 0xffffffff
  }

  // skipping CRhinoMeshRef, CRhinoGetMeshes
}
#endif
