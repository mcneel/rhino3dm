#pragma warning disable 1591
#if RHINO_SDK
using System;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Input.Custom;

namespace Rhino.Collections
{
  /// <summary>
  /// A collection of Rhino object, grip objects, and the Rhino objects that owns the grips.
  /// Used by the TransformCommand and GetTransform classes.
  /// </summary>
  public class TransformObjectList : IDisposable
  {
    /// <since>5.0</since>
    public TransformObjectList()
    {
      m_ptr = UnsafeNativeMethods.CRhinoXformObjectList_New();
    }

    internal TransformObjectList(Input.Custom.GetTransform parent)
    {
      m_ptr = IntPtr.Zero;
      m_parent = parent;
    }

    #region IDisposable/Pointer handling

    readonly Input.Custom.GetTransform m_parent;
    IntPtr m_ptr;
    internal IntPtr ConstPointer()
    {
      if (m_parent != null)
      {
        IntPtr const_ptr_parent = m_parent.ConstPointer();
        return UnsafeNativeMethods.CRhinoXformObjectList_PtrFromGetXform(const_ptr_parent);
      }
      return m_ptr;
    }
    internal IntPtr NonConstPointer()
    {
      if (m_parent != null)
      {
        IntPtr ptr_parent = m_parent.NonConstPointer();
        return UnsafeNativeMethods.CRhinoXformObjectList_PtrFromGetXform(ptr_parent);
      }
      return m_ptr;
    }

    ~TransformObjectList()
    {
      Dispose(false);
    }

    /// <since>5.0</since>
    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
      if (m_ptr != IntPtr.Zero)
      {
        UnsafeNativeMethods.CRhinoXformObjectList_Delete(m_ptr);
      }
      m_ptr = IntPtr.Zero;
    }
    #endregion

    /// <summary>
    /// Gets the bounding box of all of the objects that this list contains.
    /// </summary>
    /// <param name="regularObjects">true if any object except grips should be included; otherwise false.</param>
    /// <param name="grips">true if grips should be included; otherwise false.</param>
    /// <returns>
    /// Unset BoundingBox if this list is empty.
    /// </returns>
    /// <since>5.0</since>
    public BoundingBox GetBoundingBox(bool regularObjects, bool grips)
    {
      BoundingBox rc = BoundingBox.Unset;
      IntPtr const_ptr_this = ConstPointer();
      UnsafeNativeMethods.CRhinoXformObjectList_BoundingBox(const_ptr_this, regularObjects, grips, ref rc);
      return rc;
    }

    /// <since>5.0</since>
    public bool DisplayFeedbackEnabled
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        return UnsafeNativeMethods.CRhinoXformObjectList_DisplayFeedbackEnabled(const_ptr_this);
      }
      set
      {
        IntPtr ptr_this = NonConstPointer();
        UnsafeNativeMethods.CRhinoXformObjectList_SetDisplayFeedback(ptr_this, value);
      }
    }

    /// <since>5.0</since>
    public bool UpdateDisplayFeedbackTransform(Transform xform)
    {
      IntPtr ptr_this = NonConstPointer();
      return UnsafeNativeMethods.CRhinoXformObjectList_UpdateDisplayFeedbackTransform(ptr_this, ref xform);
    }

    /// <summary> Remove all elements from this list </summary>
    /// <since>5.10</since>
    public void Clear()
    {
      IntPtr ptr_this = NonConstPointer();
      UnsafeNativeMethods.CRhinoXformObjectList_Empty(ptr_this);
    }

    /// <summary> Number of elements in this list </summary>
    /// <since>5.10</since>
    public int Count
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        return UnsafeNativeMethods.CRhinoXformObjectList_ObjectCount(const_ptr_this);
      }
    }

    /// <summary> Number of elements in grip list </summary>
    /// <since>6.0</since>
    public int GripCount
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        return UnsafeNativeMethods.CRhinoXformObjectList_GripCount(const_ptr_this);
      }
    }

    /// <summary> Number of elements in grip owner list </summary>
    /// <since>6.0</since>
    public int GripOwnerCount
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        return UnsafeNativeMethods.CRhinoXformObjectList_GripOwnerCount(const_ptr_this);
      }
    }

    /// <summary> Add a RhinoObject to this list </summary>
    /// <param name="rhinoObject"></param>
    /// <since>5.10</since>
    public void Add(DocObjects.RhinoObject rhinoObject)
    {
      IntPtr const_ptr_rhinoobject = rhinoObject.ConstPointer();
      IntPtr ptr_this = NonConstPointer();
      UnsafeNativeMethods.CRhinoXformObjectList_AddObject2(ptr_this, const_ptr_rhinoobject);
    }

    /// <summary>
    /// Add an ObjRef to this list. Use this to add polyedges so the references are properly counted
    /// </summary>
    /// <param name="objref"></param>
    /// <since>5.10</since>
    public void Add(DocObjects.ObjRef objref)
    {
      IntPtr const_ptr_objref = objref.ConstPointer();
      IntPtr ptr_this = NonConstPointer();
      UnsafeNativeMethods.CRhinoXformObjectList_AddObject(ptr_this, const_ptr_objref);
    }

    /// <summary>
    /// Add objects to list with a GetObject
    /// </summary>
    /// <param name="go">Setup the GetObject, i.e. prompt, geometry filter, allow pre/post select 
    /// before passing it as an argument.</param>
    /// <param name="allowGrips">Specifically allow grips to be selected.  if true, grips must also be included in geometry filter
    /// of the GetObject in order to be selected.</param>
    /// <returns>Number of objects selected.</returns>
    /// <since>6.0</since>
    public int AddObjects(GetObject go, bool allowGrips)
    {
      IntPtr const_ptr_getobj = go.ConstPointer();
      IntPtr ptr_this = NonConstPointer();
      return UnsafeNativeMethods.CRhinoXformObjectList_AddObjects(ptr_this, const_ptr_getobj, allowGrips);
    }

    /// <summary>
    /// Gets access to the internal RhinoObject array of the TransformObjectList object.
    /// </summary>
    /// <returns>An array of Rhino objects, or an empty array if there were no Rhino objects.</returns>
    /// <since>7.0</since>
    public RhinoObject[] ObjectArray()
    {
      int count = Count;
      if (count < 1)
        return new RhinoObject[0];

      RhinoObject[] rc = new RhinoObject[count];
      IntPtr const_ptr_this = ConstPointer();
      for (var i = 0; i < count; i++)
      {
        var ptr_grip_owner = UnsafeNativeMethods.CRhinoXformObjectList_GetObject(const_ptr_this, i);
        rc[i] = RhinoObject.CreateRhinoObjectHelper(ptr_grip_owner);
      }
      return rc;
    }

    /// <summary>
    /// Gets access to the internal GripObject array of the TransformObjectList object.
    /// </summary>
    /// <returns>An array of grip objects, or an empty array if there were no grip objects.</returns>
    /// <since>6.0</since>
    public GripObject[] GripArray()
    {
      int count = GripCount;
      if (count < 1)
        return new GripObject[0];

      GripObject[] rc = new GripObject[count];
      IntPtr const_ptr_this = ConstPointer();
      for (var i = 0; i < count; i++)
      {
        var ptr_grip = UnsafeNativeMethods.CRhinoXformObjectList_GetGrip(const_ptr_this, i);
        rc[i] = RhinoObject.CreateRhinoObjectHelper(ptr_grip) as GripObject;
      }
      return rc;
    }

    /// <summary>
    /// Gets access to the internal GripOwner array of the TransformObjectList object.
    /// </summary>
    /// <returns>A
    /// n array of Rhino objects that are the owners of the grip objects the collection, 
    /// or an empty array if there were no Rhino objects.
    /// </returns>
    /// <since>6.0</since>
    public RhinoObject[] GripOwnerArray()
    {
      int count = GripOwnerCount;
      if (count < 1)
        return new RhinoObject[0];

      RhinoObject[] rc = new RhinoObject[count];
      IntPtr const_ptr_this = ConstPointer();
      for (var i = 0; i < count; i++)
      {
        var ptr_grip_owner = UnsafeNativeMethods.CRhinoXformObjectList_GetGripOwner(const_ptr_this, i);
        rc[i] = RhinoObject.CreateRhinoObjectHelper(ptr_grip_owner);
      }
      return rc;
    }
  }
}

namespace Rhino.Input.Custom
{

  /// <summary>
  /// Used for getting a Transform
  /// </summary>
  public abstract class GetTransform : GetPoint
  {
    protected GetTransform() : base(true)
    {
      IntPtr ptr = UnsafeNativeMethods.CRhinoGetXform_New();
      Construct(ptr);
    }

    internal delegate int CalculateXformCallack(IntPtr pRhinoViewport, Point3d point, ref Transform xform);
    internal static int CustomCalcXform(IntPtr pRhinoViewport, Point3d point, ref Transform xform)
    {
      GetTransform active_gxform = m_active_gp as GetTransform;
      if (null == active_gxform)
        return 0;
      try
      {
        Display.RhinoViewport viewport = new Display.RhinoViewport(null, pRhinoViewport);
        xform = active_gxform.CalculateTransform(viewport, point);
        return 1;
      }
      catch (Exception ex)
      {
        Runtime.HostUtils.ExceptionReport(ex);
      }
      return 0;
    }


    /// <summary>
    /// Adds any objects you want transformed and grips you want transformed.
    /// Make sure no duplicates are in the list and that no grip owners are
    /// passed in as objects.
    /// </summary>
    /// <param name="list">A custom transform object list.</param>
    /// <since>5.0</since>
    public void AddTransformObjects(Collections.TransformObjectList list)
    {
      IntPtr ptr_this = NonConstPointer();
      IntPtr const_ptr_list = list.ConstPointer();
      UnsafeNativeMethods.CRhinoGetXform_AppendObjects( ptr_this, const_ptr_list );
    }
    //void AppendObjects( const CRhinoGetObject& get );
    //void AppendObjects( CRhinoObjectIterator& it );
    //void AppendObject( const CRhinoObject* object );

    /// <summary>
    /// Retrieves the final transformation.
    /// <para>Override this virtual function to provide your own custom transformation method.</para>
    /// </summary>
    /// <param name="viewport">A Rhino viewport that the user is using.</param>
    /// <param name="point">A point that the user is selecting.</param>
    /// <returns>A transformation matrix value.</returns>
    /// <since>5.0</since>
    public abstract Transform CalculateTransform(Display.RhinoViewport viewport, Point3d point);

    // I think this can be handled in the Get() function in the base class
    //virtual CRhinoGet::result GetXform( CRhinoHistory* pHistory = NULL );

    //////////////////////////////////////////////////////////////////
    // Overridden members
    //void SetBasePoint( ON_3dPoint base_point, BOOL bShowDistanceInStatusBar = false );
    //void OnMouseMove( CRhinoViewport& vp, UINT nFlags, const ON_3dPoint& pt, const CPoint* p );
    //void DynamicDraw( HDC, CRhinoViewport& vp, const ON_3dPoint& pt );

    /// <since>5.0</since>
    public bool HaveTransform
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        return UnsafeNativeMethods.CRhinoGetXform_HaveTransform(const_ptr_this);
      }
      set
      {
        IntPtr ptr_this = NonConstPointer();
        UnsafeNativeMethods.CRhinoGetXform_SetHaveTransform(ptr_this, value);
      }
    }
    /// <since>5.0</since>
    public Transform Transform
    {
      get
      {
        Transform rc = Transform.Unset;
        IntPtr const_ptr_this = ConstPointer();
        UnsafeNativeMethods.CRhinoGetXform_Transform(const_ptr_this, ref rc);
        return rc;
      }
      set
      {
        IntPtr ptr_this = NonConstPointer();
        UnsafeNativeMethods.CRhinoGetXform_SetTransform(ptr_this, ref value);
      }
    }
    /*
  bool m_bMouseDrag; // true if transformation is from a mouse drag
  ON_3dPoint m_basepoint;
    */
    Collections.TransformObjectList m_object_list;
    /// <since>5.0</since>
    public Collections.TransformObjectList ObjectList
    {
      get
      {
        return m_object_list ?? (m_object_list = new Collections.TransformObjectList(this));
      }
    }
    /*
  //////////////////////////////////////////////////////////////////
  //
  // Tools to support custom grip moving relative to the frame returned
  // by CRhinoGripObject::GetGripDirections()
  //

  Description:
    This is a utility function that can be called in CalculateTransform()
    if you want to transform grips relative to the frame returned by
    CRhinoGripObject::GetGripDirections().
  void SetGripFrameTransform( double x_scale, double y_scale, double z_scale );

  Description:
    If GetGripFrameTransform() returns true, then grips should be
    transformed by moving them in the translation returned by
    GetGripTranslation().  If GetGripFrameTransform() returns false,
    then grips should be transformed by m_xform.
  bool HasGripFrameTransform() const;
  bool GetGripFrameTransform( double* x_scale, double* y_scale, double* z_scale ) const;
     */




    /// <summary>
    /// Gets the Transformation.
    /// <para>Call this after having set up options and so on.</para>
    /// </summary>
    /// <returns>The result based on user choice.</returns>
    /// <since>5.0</since>
    [CLSCompliant(false)]
    public GetResult GetXform()
    {
      return GetXformHelper();
    }
  } 
}
#endif
