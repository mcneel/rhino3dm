using Rhino.Geometry;
#pragma warning disable 1591
using System;
using System.Runtime.Serialization;
using Rhino.Runtime.InteropWrappers;

namespace Rhino.DocObjects
{
  /// <summary>
  /// Attributes (color, material, layer,...) associated with a rhino object
  /// </summary>
  [Serializable]
  public class ObjectAttributes : Runtime.CommonObject
  {
#if RHINO_SDK
    public override bool IsDocumentControlled
    {
      get
      {
        bool rc = base.IsDocumentControlled;
        if (rc)
        {
          var parent = m__parent as RhinoObject;
          if (parent != null)
            rc = parent.m_pRhinoObject == IntPtr.Zero;
        }
        return rc;
      }
    }
#endif
    internal override IntPtr _InternalDuplicate(out bool applymempressure)
    {
      applymempressure = false;
      IntPtr const_ptr_this = ConstPointer();
      return UnsafeNativeMethods.ON_Object_Duplicate(const_ptr_this);
    }

    internal override IntPtr _InternalGetConstPointer()
    {
#if RHINO_SDK
      var parent_object = m__parent as RhinoObject;
      if (null == parent_object)
      {
        var parent_model_object = m__parent as FileIO.File3dmObject;
        if (parent_model_object != null)
          return parent_model_object.GetAttributesConstPointer();
      }
      IntPtr const_ptr_parent = IntPtr.Zero;
      if (null != parent_object)
        const_ptr_parent = parent_object.ConstPointer();
      return UnsafeNativeMethods.CRhinoObject_Attributes(const_ptr_parent);
#else
      var parent_model_object = m__parent as Rhino.FileIO.File3dmObject;
      if (parent_model_object != null)
        return parent_model_object.GetAttributesConstPointer();
      return IntPtr.Zero;
#endif
    }

    internal ObjectAttributes(IntPtr pNonConstAttributes)
    {
      ConstructNonConstObject(pNonConstAttributes);
    }

#if RHINO_SDK
    internal override IntPtr NonConstPointer()
    {
      var rhinoObjectParent = m__parent as RhinoObject;
      if (rhinoObjectParent != null && rhinoObjectParent.m_pRhinoObject != IntPtr.Zero)
      {
        IntPtr ptr_this = UnsafeNativeMethods.CRhinoObjectAttributes_FromRhinoObject(rhinoObjectParent.m_pRhinoObject);
        if (ptr_this != IntPtr.Zero)
          return ptr_this;
      }
      var file3dmParent = m__parent as FileIO.File3dmObject;
      if (file3dmParent!=null)
      {
        return file3dmParent.GetAttributesNonConstPointer();
      }
      return base.NonConstPointer();
    }

    internal ObjectAttributes(RhinoObject parentObject)
    {
      ConstructConstObject(parentObject, -1);
    }
#endif

    internal ObjectAttributes(FileIO.File3dmObject parent)
    {
      ConstructConstObject(parent, -1);
    }

    public ObjectAttributes()
    {
#if RHINO_SDK
      IntPtr ptr = UnsafeNativeMethods.CRhinoObjectAttributes_New(IntPtr.Zero);
#else
      IntPtr ptr = UnsafeNativeMethods.ON_3dmObjectAttributes_New(IntPtr.Zero);
#endif
      ConstructNonConstObject(ptr);
    }

    // serialization constructor
    protected ObjectAttributes(SerializationInfo info, StreamingContext context)
      : base (info, context)
    {
    }

    /// <summary>
    /// Constructs a copy of this <see cref="ObjectAttributes"/> instance.
    /// </summary>
    /// <returns>A new instance on success, or null on failure.</returns>
    public ObjectAttributes Duplicate()
    {
      IntPtr ptr_this = ConstPointer();
#if RHINO_SDK
      IntPtr ptr_new = UnsafeNativeMethods.CRhinoObjectAttributes_New(ptr_this);
#else
      IntPtr ptr_new = UnsafeNativeMethods.ON_3dmObjectAttributes_New(ptr_this);
#endif
      if (IntPtr.Zero == ptr_new)
        return null;
      return new ObjectAttributes(ptr_new);
    }

    int GetInt(UnsafeNativeMethods.ObjectAttrsInteger which)
    {
      IntPtr ptr = ConstPointer();
      return UnsafeNativeMethods.ON_3dmObjectAttributes_GetSetInt(ptr, which, false, 0);
    }
    void SetInt(UnsafeNativeMethods.ObjectAttrsInteger which, int setValue)
    {
      IntPtr ptr = NonConstPointer();
      UnsafeNativeMethods.ON_3dmObjectAttributes_GetSetInt(ptr, which, true, setValue);
    }


    /// <summary>
    /// An object must be in one of three modes: normal, locked or hidden.
    /// If an object is in normal mode, then the object's layer controls visibility
    /// and selectability. If an object is locked, then the object's layer controls
    /// visibility by the object cannot be selected. If the object is hidden, it is
    /// not visible and it cannot be selected.
    /// </summary>
    public ObjectMode Mode
    {
      get
      {
        int rc = GetInt(UnsafeNativeMethods.ObjectAttrsInteger.Mode);
        return (ObjectMode)rc;
      }
      set { SetInt(UnsafeNativeMethods.ObjectAttrsInteger.Mode, (int)value); }
    }

    bool GetBool(UnsafeNativeMethods.ObjectAttrsBool which)
    {
      IntPtr ptr = ConstPointer();
      return UnsafeNativeMethods.ON_3dmObjectAttributes_GetSetBool(ptr, which, false, false);
    }
    void SetBool(UnsafeNativeMethods.ObjectAttrsBool which, bool setValue)
    {
      IntPtr ptr = NonConstPointer();
      UnsafeNativeMethods.ON_3dmObjectAttributes_GetSetBool(ptr, which, true, setValue);
    }

    /// <summary>
    /// Apply a transformation.
    /// </summary>
    /// <param name="xform">The transformation.</param>
    /// <returns>trueif successful, false otherwise.</returns>
    public bool Transform(Transform xform)
    {
      var pointer = NonConstPointer();
      var success = UnsafeNativeMethods.ON_3dmObjectAttributes_Transform(pointer, ref xform);
      return success;
    }

    /// <summary>
    /// Use this query to determine if an object is part of an instance definition.
    /// </summary>
    public bool IsInstanceDefinitionObject
    {
      get { return GetBool(UnsafeNativeMethods.ObjectAttrsBool.IsInstanceDefinitionObject); }
    }

    /// <summary>Gets or sets an object's visiblity.</summary>
    public bool Visible
    {
      get { return GetBool(UnsafeNativeMethods.ObjectAttrsBool.IsVisible); }
      set { SetBool(UnsafeNativeMethods.ObjectAttrsBool.IsVisible, value); }
    }

    /// <summary>
    /// Gets or sets an object's casts shadows property, or whether or not an object casts shadows on other objects and a ground plane.
    /// </summary>
    public bool CastsShadows
    {
      get { return GetBool(UnsafeNativeMethods.ObjectAttrsBool.CastsShadows); }
      set { SetBool(UnsafeNativeMethods.ObjectAttrsBool.CastsShadows, value); }
    }

    /// <summary>
    /// Gets or sets an object's receives shadows property, or whether or not an object receives shadows from other objects.
    /// </summary>
    public bool ReceivesShadows
    {
      get { return GetBool(UnsafeNativeMethods.ObjectAttrsBool.ReceivesShadows); }
      set { SetBool(UnsafeNativeMethods.ObjectAttrsBool.ReceivesShadows, value); }
    }

    /// <summary>
    /// The Linetype used to display an object is specified in one of two ways.
    /// If LinetypeSource is ON::linetype_from_layer, then the object's layer ON_Layer::Linetype() is used.
    /// If LinetypeSource is ON::linetype_from_object, then value of m_linetype is used.
    /// </summary>
    public ObjectLinetypeSource LinetypeSource
    {
      get
      {
        int rc = GetInt(UnsafeNativeMethods.ObjectAttrsInteger.LineTypeSource);
        return (ObjectLinetypeSource)rc;
      }
      set { SetInt(UnsafeNativeMethods.ObjectAttrsInteger.LineTypeSource, (int)value); }
    }

    /// <summary>
    /// The color used to display an object is specified in one of three ways.
    /// If ColorSource is ON::color_from_layer, then the object's layer ON_Layer::Color() is used.
    /// If ColorSource is ON::color_from_object, then value of m_color is used.
    /// If ColorSource is ON::color_from_material, then the diffuse color of the object's
    /// render material is used.  See ON_3dmObjectAttributes::MaterialSource() to
    /// determine where to get the definition of the object's render material.
    /// </summary>
    /// <example>
    /// <code source='examples\vbnet\ex_modifyobjectcolor.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_modifyobjectcolor.cs' lang='cs'/>
    /// <code source='examples\py\ex_modifyobjectcolor.py' lang='py'/>
    /// </example>
    public ObjectColorSource ColorSource
    {
      get
      {
        int rc = GetInt(UnsafeNativeMethods.ObjectAttrsInteger.ColorSource);
        return (ObjectColorSource)rc;
      }
      set { SetInt(UnsafeNativeMethods.ObjectAttrsInteger.ColorSource, (int)value); }
    }

    /// <summary>
    /// The color used to plot an object on paper is specified in one of three ways.
    /// If PlotColorSource is ON::plot_color_from_layer, then the object's layer ON_Layer::PlotColor() is used.
    /// If PlotColorSource is ON::plot_color_from_object, then value of PlotColor() is used.
    /// </summary>
    public ObjectPlotColorSource PlotColorSource
    {
      get
      {
        int rc = GetInt(UnsafeNativeMethods.ObjectAttrsInteger.PlotColorSource);
        return (ObjectPlotColorSource)rc;
      }
      set { SetInt(UnsafeNativeMethods.ObjectAttrsInteger.PlotColorSource, (int)value); }
    }

    public ObjectPlotWeightSource PlotWeightSource
    {
      get
      {
        int rc = GetInt(UnsafeNativeMethods.ObjectAttrsInteger.PlotWeightSource);
        return (ObjectPlotWeightSource)rc;
      }
      set { SetInt(UnsafeNativeMethods.ObjectAttrsInteger.PlotWeightSource, (int)value); }
    }

    /// <summary>
    /// Determines if an object has a display mode override for a given viewport.
    /// </summary>
    /// <param name="viewportId">Id of a Rhino Viewport.</param>
    /// <returns>true if the object has a display mode override for the viewport; otherwise, false.</returns>
    /// <example>
    /// <code source='examples\vbnet\ex_objectdisplaymode.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_objectdisplaymode.cs' lang='cs'/>
    /// <code source='examples\py\ex_objectdisplaymode.py' lang='py'/>
    /// </example>
    public bool HasDisplayModeOverride(Guid viewportId)
    {
      IntPtr const_ptr_this = ConstPointer();
      return UnsafeNativeMethods.ON_3dmObjectAttributes_HasDisplayModeOverride(const_ptr_this, viewportId);
    }

#if RHINO_SDK
    /// <summary>
    /// By default, objects are drawn using the display mode of the viewport that
    /// the object is being drawn in. Setting a specific display mode, instructs
    /// Rhino to always use that display mode, regardless of the viewport's mode.
    /// This version affects the object's display mode for all viewports.
    /// </summary>
    /// <param name="mode">The display mode.</param>
    /// <returns>true if setting was successful.</returns>
    public bool SetDisplayModeOverride(Display.DisplayModeDescription mode)
    {
      return SetDisplayModeOverride(mode, Guid.Empty);
    }
    /// <summary>
    /// By default, objects are drawn using the display mode of the viewport that
    /// the object is being drawn in. Setting a specific display mode, instructs
    /// Rhino to always use that display mode, regardless of the viewport's mode.
    /// This version sets a display mode for a specific viewport.
    /// </summary>
    /// <param name="mode">The display mode.</param>
    /// <param name="rhinoViewportId">The Rhino viewport ID.</param>
    /// <returns>true on success.</returns>
    /// <example>
    /// <code source='examples\vbnet\ex_objectdisplaymode.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_objectdisplaymode.cs' lang='cs'/>
    /// <code source='examples\py\ex_objectdisplaymode.py' lang='py'/>
    /// </example>
    public bool SetDisplayModeOverride(Display.DisplayModeDescription mode, Guid rhinoViewportId)
    {
      IntPtr ptr_this = NonConstPointer();
      Guid mode_id = mode.Id;
      return UnsafeNativeMethods.ON_3dmObjectAttributes_UseDisplayMode(ptr_this, rhinoViewportId, mode_id);
    }
#endif

    /// <summary>
    /// By default, objects are drawn using the display mode of the viewport that
    /// the object is being drawn in. Setting a specific display mode, instructs
    /// Rhino to always use that display mode, regardless of the viewport's mode.
    /// This function resets an object to use the viewport's display mode for all
    /// viewports.
    /// </summary>
    public void RemoveDisplayModeOverride()
    {
      RemoveDisplayModeOverride(Guid.Empty);
    }

    /// <summary>
    /// By default, objects are drawn using the display mode of the viewport that
    /// the object is being drawn in. Setting a specific display mode, instructs
    /// Rhino to always use that display mode, regardless of the viewport's mode.
    /// This function resets an object to use the viewport's display mode.
    /// </summary>
    /// <param name="rhinoViewportId">viewport that display mode overrides should be cleared from.</param>
    /// <example>
    /// <code source='examples\vbnet\ex_objectdisplaymode.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_objectdisplaymode.cs' lang='cs'/>
    /// <code source='examples\py\ex_objectdisplaymode.py' lang='py'/>
    /// </example>
    public void RemoveDisplayModeOverride(Guid rhinoViewportId)
    {
      IntPtr ptr_this = NonConstPointer();
      UnsafeNativeMethods.ON_3dmObjectAttributes_ClearDisplayMode(ptr_this, rhinoViewportId);
    }

    /// <summary> Make this object hidden in a given detail </summary>
    /// <param name="detailId"></param>
    /// <returns></returns>
    public bool AddHideInDetailOverride(Guid detailId)
    {
      IntPtr ptrThis = NonConstPointer();
      return UnsafeNativeMethods.ON_3dmObjectAttributes_HideInDetail(ptrThis, true, detailId);
    }

    /// <summary> Remove hidden in detail flag for a specific detail </summary>
    /// <param name="detailId"></param>
    /// <returns></returns>
    public bool RemoveHideInDetailOverride(Guid detailId)
    {
      IntPtr ptrThis = NonConstPointer();
      return UnsafeNativeMethods.ON_3dmObjectAttributes_HideInDetail(ptrThis, false, detailId);
    }

    /// <summary>Is this object supposed to be hidden in a given detail</summary>
    /// <param name="detailId"></param>
    /// <returns></returns>
    public bool HasHideInDetailOverrideSet(Guid detailId)
    {
      Guid[] ids = GetHideInDetailOverrides();
      foreach(var id in ids)
      {
        if (id == detailId)
          return true;
      }
      return false;
    }

    /// <summary>
    /// Get list of details that this object is supposed to be hidden in
    /// </summary>
    /// <returns></returns>
    public Guid[] GetHideInDetailOverrides()
    {
      using (var guids = new SimpleArrayGuid())
      {
        IntPtr constPtrThis = ConstPointer();
        UnsafeNativeMethods.ON_3dmObjectAttributes_HideInDetailIds(constPtrThis, guids.NonConstPointer());
        return guids.ToArray();
      }
    }
/*
    /// <summary>
    /// If "this" has attributes (color, plot weight, ...) with "by parent" sources,
    /// then the values of those attributes on parentAttributes are copied.
    /// </summary>
    /// <param name="parentAttributes">-</param>
    /// <param name="controlLimits">
    /// The bits in controlLimits determine which attributes may be copied.
    /// 1: visibility
    /// 2: color
    /// 4: render material
    /// 8: plot color
    /// 0x10: plot weight
    /// 0x20: linetype
    /// </param>
    /// <returns>
    /// The bits in the returned integer indicate which attributes were actually modified.
    /// 1: visibility
    /// 2: color
    /// 4: render material
    /// 8: plot color
    /// 0x10: plot weight
    /// 0x20: linetype.
    /// </returns>
    public int ApplyParentalControl(ObjectAttributes parentAttributes, int controlLimits)
    {
      IntPtr ptr = NonConstPointer();
      IntPtr parentPtr = parentAttributes.ConstPointer();
      return (int)UnsafeNativeMethods.ON_3dmObjectAttributes_ApplyParentalControl(ptr, parentPtr, (uint)controlLimits);
    }
    /// <summary>
    /// If "this" has attributes (color, plot weight, ...) with "by parent" sources,
    /// then the values of those attributes on parentAttributes are copied.
    /// </summary>
    /// <param name="parentAttributes">-</param>
    /// <returns>
    /// The bits in the returned integer indicate which attributes were actually modified.
    /// 1: visibility
    /// 2: color
    /// 4: render material
    /// 8: plot color
    /// 0x10: plot weight
    /// 0x20: linetype.
    /// </returns>
    public int ApplyParentalControl(ObjectAttributes parentAttributes)
    {
      IntPtr ptr = NonConstPointer();
      IntPtr parentPtr = parentAttributes.ConstPointer();
      return (int)UnsafeNativeMethods.ON_3dmObjectAttributes_ApplyParentalControl(ptr, parentPtr, 0xFFFFFFFF);
    }
*/
    /// <summary>
    /// Every object has a Guid (globally unique identifier, also known as UUID, or universally
    /// unique identifier). The default value is Guid.Empty.
    /// <para>
    /// When an object is added to a model, the value is checked.  If the value is Guid.Empty, a
    /// new Guid is created. If the value is not null but it is already used by another object
    /// in the model, a new Guid is created. If the value is not Guid.Empty and it is not used by
    /// another object in the model, then that value persists. When an object is updated, by
    /// a move for example, the value of ObjectId persists.
    /// </para>
    /// <para>This value is the same as the one returned by object.Id.</para>
    /// </summary>
    public Guid ObjectId
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        return UnsafeNativeMethods.ON_3dmObjectAttributes_m_uuid(const_ptr_this); 
      }
      set
      {
        IntPtr ptr_this = NonConstPointer();
        UnsafeNativeMethods.ON_3dmObjectAttributes_set_m_uuid(ptr_this, value);
      }
    }


    const int IDX_NAME = 0;
    const int IDX_URL = 1;
    string GetString(int which)
    {
      IntPtr ptr = ConstPointer();
      using (var sh = new StringHolder())
      {
        IntPtr ptr_string = sh.NonConstPointer();
        UnsafeNativeMethods.ON_3dmObjectAttributes_GetSetString(ptr, which, false, null, ptr_string);
        return sh.ToString();
      }
    }
    void SetString(int which, string str)
    {
      IntPtr ptr = NonConstPointer();
      UnsafeNativeMethods.ON_3dmObjectAttributes_GetSetString(ptr, which, true, str, IntPtr.Zero);
    }

    /// <summary>
    /// Gets or sets an object optional text name.
    /// <para>More than one object in a model can have the same name and
    /// some objects may have no name.</para>
    /// </summary>
    public string Name
    {
      get { return GetString(IDX_NAME); }
      set { SetString(IDX_NAME, value); }
    }

    /// <summary>
    /// Objects may have an URL. There are no restrictions on what value this
    /// URL may have. As an example, if the object came from a commercial part
    /// library, the URL might point to the definition of that part.
    /// </summary>
    public string Url
    {
      get{ return GetString(IDX_URL); }
      set { SetString(IDX_URL, value); }
    }

    /// <summary>
    /// Gets or sets an associated layer index.
    /// <para>Layer definitions in an OpenNURBS model are stored in a layer table.
    /// The layer table is conceptually an array of ON_Layer classes.  Every
    /// OpenNURBS object in a model is on some layer.  The object's layer
    /// is specified by zero based indicies into the ON_Layer array.</para>
    /// </summary>
    /// <example>
    /// <code source='examples\vbnet\ex_moveobjectstocurrentlayer.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_moveobjectstocurrentlayer.cs' lang='cs'/>
    /// <code source='examples\py\ex_moveobjectstocurrentlayer.py' lang='py'/>
    /// </example>
    public int LayerIndex
    {
      get { return GetInt(UnsafeNativeMethods.ObjectAttrsInteger.LayerIndex); }
      set { SetInt(UnsafeNativeMethods.ObjectAttrsInteger.LayerIndex, value); }
    }
    /// <summary>
    /// Gets or sets the linetype index.
    /// <para>Linetype definitions in an OpenNURBS model are stored in a linetype table.
    /// The linetype table is conceptually an array of ON_Linetype classes. Every
    /// OpenNURBS object in a model references some linetype.  The object's linetype
    /// is specified by zero based indicies into the ON_Linetype array.</para>
    /// <para>Index 0 is reserved for continuous linetype (no pattern).</para>
    /// </summary>
    public int LinetypeIndex
    {
      get { return GetInt(UnsafeNativeMethods.ObjectAttrsInteger.LinetypeIndex); }
      set { SetInt(UnsafeNativeMethods.ObjectAttrsInteger.LinetypeIndex, value); }
    }
    /// <summary>
    /// Gets or sets the material index.
    /// <para>If you want something simple and fast, set the index of
    /// the rendering material.</para>
    /// </summary>
    /*
     * ...and ignore m_rendering_attributes. If you are developing
     * a high quality plug-in renderer, and a user is assigning one of your fabulous
     * rendering materials to this object, then add rendering material information to
     * the m_rendering_attributes.m_materials[] array.
    */
    public int MaterialIndex
    {
      get { return GetInt(UnsafeNativeMethods.ObjectAttrsInteger.MaterialIndex); }
      set { SetInt(UnsafeNativeMethods.ObjectAttrsInteger.MaterialIndex, value); }
    }

    // [skipping]
    // ON_ObjectRenderingAttributes m_rendering_attributes;

    /// <summary>
    /// Determines if the simple material should come from the object or from it's layer.
    /// High quality rendering plug-ins should use m_rendering_attributes.
    /// </summary>
    public ObjectMaterialSource MaterialSource
    {
      get { return (ObjectMaterialSource)GetInt(UnsafeNativeMethods.ObjectAttrsInteger.MaterialSource); }
      set { SetInt(UnsafeNativeMethods.ObjectAttrsInteger.MaterialSource, (int)value); }
    }

    const int IDX_COLOR = 0;
    const int IDX_PLOT_COLOR = 1;
    System.Drawing.Color GetColor(int which)
    {
      IntPtr ptr = ConstPointer();
      int abgr = UnsafeNativeMethods.ON_3dmObjectAttributes_GetSetColor(ptr, which, false, 0);
      return Runtime.Interop.ColorFromWin32(abgr);
    }
    void SetColor(int which, System.Drawing.Color c)
    {
      IntPtr ptr = NonConstPointer();
      int argb = c.ToArgb();
      UnsafeNativeMethods.ON_3dmObjectAttributes_GetSetColor(ptr, which, true, argb);
    }

#if RHINO_SDK
    /// <summary>
    /// Gets all object decals associated with this object.
    /// </summary>
    public Render.Decals Decals
    {
      get { return (m_decals ?? (m_decals = new Render.Decals(this))); }
    }
    private Render.Decals m_decals;
#endif

    /// <summary>
    /// If you are developing a high quality plug-in renderer, and a user is
    /// assigning a custom render material to this object, then add rendering
    /// material information to the MaterialRefs dictionary.
    /// 
    /// Note to developers:
    ///  As soon as the MaterialRefs dictionary contains items rendering
    ///  material queries slow down.  Do not populate the MaterialRefs
    /// dictionary when setting the MaterialIndex will take care of your needs.
    /// </summary>
    public MaterialRefs MaterialRefs
    {
      get { return m_material_refs ?? (m_material_refs = new MaterialRefs(this)); }
    }
    private MaterialRefs m_material_refs;
    /// <summary>
    /// If ON::color_from_object == ColorSource, then color is the object's display color.
    /// </summary>
    /// <example>
    /// <code source='examples\vbnet\ex_modifyobjectcolor.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_modifyobjectcolor.cs' lang='cs'/>
    /// <code source='examples\py\ex_modifyobjectcolor.py' lang='py'/>
    /// </example>
    public System.Drawing.Color ObjectColor
    {
      get { return GetColor(IDX_COLOR); }
      set { SetColor(IDX_COLOR, value); }
    }
    /// <summary>
    /// If plot_color_from_object == PlotColorSource, then PlotColor is the object's plotting color.
    /// </summary>
    public System.Drawing.Color PlotColor
    {
      get { return GetColor(IDX_PLOT_COLOR); }
      set { SetColor(IDX_PLOT_COLOR, value); }
    }

    /// <summary>
    /// A mapping from any plugin source is associated with these attributes
    /// Need to do this here to respond correctly to ModifyObjectAttributes event
    /// </summary>
    public bool HasMapping
    {
      get { return UnsafeNativeMethods.ON_3dmObjectAttributes_HasMapping(NonConstPointer()); }
    }


#if RHINO_SDK
    public System.Drawing.Color DrawColor(RhinoDoc document)
    {
      return DrawColor(document, Guid.Empty);
    }
    public System.Drawing.Color DrawColor(RhinoDoc document, Guid viewportId)
    {
      IntPtr const_ptr_this = ConstPointer();
      int abgr = UnsafeNativeMethods.CRhinoObjectAttributes_DrawColor(const_ptr_this, document.RuntimeSerialNumber, viewportId);
      return Runtime.Interop.ColorFromWin32(abgr);
    }

    public System.Drawing.Color ComputedPlotColor(RhinoDoc document)
    {
      return ComputedPlotColor(document, Guid.Empty);
    }
    public System.Drawing.Color ComputedPlotColor(RhinoDoc document, Guid viewportId)
    {
      IntPtr const_ptr_this = ConstPointer();
      int abgr = UnsafeNativeMethods.CRhinoObjectAttributes_PlotColor(const_ptr_this, document.RuntimeSerialNumber, viewportId);
      return Runtime.Interop.ColorFromWin32(abgr);
    }

    public double ComputedPlotWeight(RhinoDoc document)
    {
      return ComputedPlotWeight(document, Guid.Empty);
    }
    public double ComputedPlotWeight(RhinoDoc document, Guid viewportId)
    {
      IntPtr const_ptr_this = ConstPointer();
      return UnsafeNativeMethods.CRhinoObjectAttributes_PlotWeight(const_ptr_this, document.RuntimeSerialNumber, viewportId);
    }

#endif

    /// <summary>
    /// Display order used to force objects to be drawn on top or behind each other.
    /// Larger numbers draw on top of smaller numbers.
    /// <para>0  = draw object in standard depth buffered order</para>
    /// <para>&lt;0 = draw object behind "normal" draw order objects</para>
    /// <para>&gt;0 = draw object on top of "normal" draw order objects</para>
    /// </summary>
    /// <example>
    /// <code source='examples\vbnet\ex_displayorder.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_displayorder.cs' lang='cs'/>
    /// <code source='examples\py\ex_displayorder.py' lang='py'/>
    /// </example>
    public int DisplayOrder
    {
      get
      {
        return GetInt(UnsafeNativeMethods.ObjectAttrsInteger.DisplayOrder);
      }
      set
      {
        SetInt(UnsafeNativeMethods.ObjectAttrsInteger.DisplayOrder, value);
      }
    }

    /// <summary>
    /// Plot weight in millimeters.
    /// =0.0 means use the default width
    /// &lt;0.0 means don't plot (visible for screen display, but does not show on plot)
    /// </summary>
    public double PlotWeight
    {
      get
      {
        IntPtr ptr = ConstPointer();
        return UnsafeNativeMethods.ON_3dmObjectAttributes_PlotWeight(ptr, false, 0.0);
      }
      set
      {
        IntPtr ptr = NonConstPointer();
        UnsafeNativeMethods.ON_3dmObjectAttributes_PlotWeight(ptr, true, value);
      }
    }

    /// <summary>
    /// Used to indicate an object has a decoration (like an arrowhead on a curve)
    /// </summary>
    public ObjectDecoration ObjectDecoration
    {
      get { return (ObjectDecoration)GetInt(UnsafeNativeMethods.ObjectAttrsInteger.ObjectDecoration); }
      set { SetInt(UnsafeNativeMethods.ObjectAttrsInteger.ObjectDecoration, (int)value); }
    }

    /// <summary>
    /// When a surface object is displayed in wireframe, this controls
    /// how many isoparametric wires are used.
    /// value    number of isoparametric wires
    /// -1       boundary wires (off)
    /// 0        boundary and knot wires 
    /// 1        boundary and knot wires and, if there are no interior knots, a single interior wire.
    /// N>=2     boundary and knot wires and (N+1) interior wires.
    /// </summary>
    /// <example>
    /// <code source='examples\vbnet\ex_isocurvedensity.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_isocurvedensity.cs' lang='cs'/>
    /// <code source='examples\py\ex_isocurvedensity.py' lang='py'/>
    /// </example>
    public int WireDensity
    {
      get { return GetInt(UnsafeNativeMethods.ObjectAttrsInteger.WireDensity); }
      set { SetInt(UnsafeNativeMethods.ObjectAttrsInteger.WireDensity, value); }
    }


    /// <summary>
    /// If ViewportId is nil, the object is active in all viewports. If ViewportId is not nil, then 
    /// this object is only active in a specific view. This field is primarily used to assign page
    /// space objects to a specific page, but it can also be used to restrict model space to a
    /// specific view.
    /// </summary>
    public Guid ViewportId
    {
      get
      {
        IntPtr ptr = ConstPointer();
        return UnsafeNativeMethods.ON_3dmObjectAttributes_ViewportId(ptr, false, Guid.Empty);
      }
      set
      {
        IntPtr ptr = NonConstPointer();
        UnsafeNativeMethods.ON_3dmObjectAttributes_ViewportId(ptr, true, value);
      }
    }

    /// <summary>
    /// Starting with V4, objects can be in either model space or page space.
    /// If an object is in page space, then ViewportId is not nil and
    /// identifies the page it is on.
    /// </summary>
    public ActiveSpace Space
    {
      get { return (ActiveSpace)GetInt(UnsafeNativeMethods.ObjectAttrsInteger.Space); }
      set { SetInt(UnsafeNativeMethods.ObjectAttrsInteger.Space, (int)value); }
    }

    /// <summary>number of groups object belongs to.</summary>
    public int GroupCount
    {
      get { return GetInt(UnsafeNativeMethods.ObjectAttrsInteger.GroupCount); }
    }


    /// <summary>
    /// Returns an array of GroupCount group indices.  If GroupCount is zero, then GetGroupList() returns null.
    /// </summary>
    /// <returns>An array of group indices. null might be returned in place of an empty array.</returns>
    public int[] GetGroupList()
    {
      int count = GroupCount;
      if( count < 1 )
        return null;
      int[] rc = new int[count];
      IntPtr ptr = ConstPointer();
      UnsafeNativeMethods.ON_3dmObjectAttributes_GroupList(ptr, rc);
      return rc;
    }

    // [skipping]
    // int TopGroup() const; I'm not sure how this is used
    // BOOL IsInGroup()
    // BOOL IsInGroups()

    const int IDX_ADD_TO_GROUP = 0;
    const int IDX_REMOVE_FROM_GROUP = 1;
    //const int idxRemoveFromTopGroup = 2;
    const int IDX_REMOVE_FROM_ALL_GROUPS = 3;

    /// <summary>
    /// Adds object to the group with specified index by appending index to
    /// group list.
    /// <para>If the object is already in group, nothing is changed.</para>
    /// </summary>
    /// <param name="groupIndex">The index that will be added.</param>
    public void AddToGroup(int groupIndex)
    {
      IntPtr ptr = NonConstPointer();
      UnsafeNativeMethods.ON_3dmObjectAttributes_GroupOp(ptr, IDX_ADD_TO_GROUP, groupIndex);
    }
    /// <summary>
    /// removes object from the group with specified index.
    /// <para>If the object is not in the group, nothing is changed.</para>
    /// </summary>
    /// <param name="groupIndex">The index that will be removed.</param>
    public void RemoveFromGroup(int groupIndex)
    {
      IntPtr ptr = NonConstPointer();
      UnsafeNativeMethods.ON_3dmObjectAttributes_GroupOp(ptr, IDX_REMOVE_FROM_GROUP, groupIndex);
    }

    // [skipping]
    // void RemoveFromTopGroup(); don't understand how this is used

    /// <summary>Removes object from all groups.</summary>
    public void RemoveFromAllGroups()
    {
      IntPtr ptr = NonConstPointer();
      UnsafeNativeMethods.ON_3dmObjectAttributes_GroupOp(ptr, IDX_REMOVE_FROM_ALL_GROUPS, -1);
    }

    // [skipping]
    //  bool FindDisplayMaterialRef(
    //  bool FindDisplayMaterialId( 
    //  bool AddDisplayMaterialRef(
    //  bool RemoveDisplayMaterialRef(
    //  void RemoveAllDisplayMaterialRefs();
    //  int DisplayMaterialRefCount() const;
    //  ON_SimpleArray<ON_DisplayMaterialRef> m_dmref;

    #region user strings
    /// <summary>
    /// Attach a user string (key,value combination) to this geometry.
    /// </summary>
    /// <param name="key">id used to retrieve this string.</param>
    /// <param name="value">string associated with key. If null, the key will be removed</param>
    /// <returns>true on success.</returns>
    public bool SetUserString(string key, string value)
    {
      return _SetUserString(key, value);
    }
    /// <summary>
    /// Gets a user string.
    /// </summary>
    /// <param name="key">id used to retrieve the string.</param>
    /// <returns>string associated with the key if successful. null if no key was found.</returns>
    public string GetUserString(string key)
    {
      return _GetUserString(key);
    }

    public int UserStringCount
    {
      get
      {
        return _UserStringCount;
      }
    }

    /// <summary>
    /// Gets an independent copy of the collection of (user text key, user text value) pairs attached to this object.
    /// </summary>
    /// <returns>A collection of key strings and values strings. This </returns>
    public System.Collections.Specialized.NameValueCollection GetUserStrings()
    {
      return _GetUserStrings();
    }

    public bool DeleteUserString(string key)
    {
      return SetUserString(key, null);
    }

    public void DeleteAllUserStrings()
    {
      _DeleteAllUserStrings();
    }
    #endregion
  }
}
