using System;
using Rhino.FileIO;
using Rhino.Geometry;
using Rhino.Render;
using Rhino.Runtime.InteropWrappers;
using Rhino.Display;
using System.Collections.Generic;
using Rhino.UI.Gumball;

namespace Rhino.DocObjects
{
#if RHINO_SDK
  /// <summary>
  /// Represents an object in the document.
  /// <para>RhinoObjects should only ever be creatable by the RhinoDoc.</para>
  /// </summary>
  public class RhinoObject : ModelComponent // We don't want to allow for any private copies of RhinoObjects. We need to ensure correct hierarchy though
  {
  #region internals
    // All RhinoObject pointer validation is performed against the runtime serial number
    internal uint m_rhinoobject_serial_number;
    internal GeometryBase m_original_geometry;
    internal GeometryBase m_edited_geometry;
    internal ObjectAttributes m_original_attributes;
    internal ObjectAttributes m_edited_attributes;
    internal Material m_edited_material;
    internal int m_edited_material_index = -1;

    // well.. that is not exactly true. As we start adding support for custom Rhino objects
    // we need a way to create RhinoObject from a plug-in
    internal IntPtr m_pRhinoObject = IntPtr.Zero; // this is ONLY set when the object is a custom rhinocommon object

    internal delegate uint CommitGeometryChangesFunc(uint sn, IntPtr pConstGeometry);

    // same thing
    internal IntPtr NonConstPointer_I_KnowWhatImDoing()
    {
      return ConstPointer();
    }

    /// <summary>
    /// !!!DO NOT CALL THIS FUNCTION UNLESS YOU ARE WORKING WITH CUSTOM RHINO OBJECTS!!!
    /// </summary>
    /// <returns>A pointer.</returns>
    internal override IntPtr NonConstPointer()
    {
      if (IntPtr.Zero == m_pRhinoObject)
        throw new Runtime.DocumentCollectedException();
      return m_pRhinoObject;
    }

    internal override IntPtr _InternalGetConstPointer()
    {
      if (m_pRhinoObject != IntPtr.Zero)
        return m_pRhinoObject;

      var rc = UnsafeNativeMethods.RHC_LookupObjectBySerialNumber(m_rhinoobject_serial_number);
      if (IntPtr.Zero == rc)
      {
        // TODO: Steve; what is this for? I ran across it when I hit a stack overflow
        //var doc_sn = 0u;
        //rc = UnsafeNativeMethods.Rdk_FindContentInstance(ref doc_sn, Id);
        if (IntPtr.Zero == rc)
          throw new Runtime.DocumentCollectedException();
      }

      return rc;
    }

    // this protected constructor should only be used by "custom" subclasses
    internal RhinoObject()
    {
      RegisterCustomObjectType(GetType());
      m_rhinoobject_serial_number = 0;

      g_the_duplicate_callback = OnRhinoObjectDuplicate;
      g_the_doc_notify_callback = OnRhinoObjectDocNotify;
      g_the_active_in_viewport_callback = OnRhinoObjectActiveInViewport;
      g_the_selection_callback = OnRhinoObjectSelection;
      g_the_pick_callback = OnRhinoObjectPick;
      g_the_picked_callback = OnRhinoObjectPicked;
      g_the_transform_callback = OnRhinoObjectTransform;
      g_the_space_morph_callback = OnRhinoObjectSpaceMorph;
      g_the_delete_callback = OnRhinoObjectDeleted;

      UnsafeNativeMethods.CRhinoObject_SetCallbacks(g_the_duplicate_callback,
                                                    g_the_doc_notify_callback, g_the_active_in_viewport_callback,
                                                    g_the_selection_callback, g_the_transform_callback,
                                                    g_the_space_morph_callback, g_the_delete_callback);
      UnsafeNativeMethods.CRhinoObject_SetPickCallbacks(g_the_pick_callback, g_the_picked_callback);
    }

    internal RhinoObject(uint sn)
    {
      m_rhinoobject_serial_number = sn;
    }

    System.Runtime.InteropServices.GCHandle _drawCallback;
    void RegisterPerObjectCallbacks(bool add, IntPtr ptrThis)
    {
      if (_drawCallback.IsAllocated)
        _drawCallback.Free();

      if (IntPtr.Zero == ptrThis)
        return;

      if (add)
      {
        RhinoObjectDrawCallback callback = new RhinoObjectDrawCallback(OnRhinoObjectDraw);
        _drawCallback = System.Runtime.InteropServices.GCHandle.Alloc(callback);
        IntPtr drawfunc = System.Runtime.InteropServices.Marshal.GetFunctionPointerForDelegate(callback);
        UnsafeNativeMethods.CRhinoObject_SetPerObjectCallbacks(ptrThis, drawfunc);
      }
      else
      {
        UnsafeNativeMethods.CRhinoObject_SetPerObjectCallbacks(ptrThis, IntPtr.Zero);
      }
    }

    internal delegate void RhinoObjectDrawCallback(IntPtr pConstRhinoObject, IntPtr pDisplayPipeline);
    internal delegate void RhinoObjectDuplicateCallback(uint docSerialNumber, uint sourceObjectSerialNumber, uint newObjectSerialNumber, IntPtr newObjectPointer);
    internal delegate void RhinoObjectDocNotifyCallback(uint docSerialNumber, uint serialNumber, int add, Guid managedTypeId);
    internal delegate int RhinoObjectActiveInViewportCallback(uint docSerialNumber, uint serialNumber, IntPtr pRhinoViewport);
    internal delegate void RhinoObjectSelectionCallback(uint docSerialNumber, uint serialNumber);
    internal delegate void RhinoObjectTransformCallback(uint docSerialNumber, uint serialNumber, IntPtr pConstTransform);
    internal delegate void RhinoObjectSpaceMorphCallback(uint docSerialNumber, uint serialNumber, IntPtr pConstSpaceMorph);
    internal delegate void RhinoObjectPickCallback(uint docSerialNumber, uint serialNumber, IntPtr pConstRhinoObject, IntPtr pRhinoObjRefArray);
    internal delegate void RhinoObjectPickedCallback(uint docSerialNumber, uint serialNumber, IntPtr pConstRhinoObject, IntPtr pRhinoObjRefArray, int count);
    internal delegate void RhinoObjectDeletedCallback(uint serialNumber);

    static RhinoObjectDuplicateCallback g_the_duplicate_callback;
    static RhinoObjectDocNotifyCallback g_the_doc_notify_callback;
    static RhinoObjectActiveInViewportCallback g_the_active_in_viewport_callback;
    static RhinoObjectSelectionCallback g_the_selection_callback;
    static RhinoObjectTransformCallback g_the_transform_callback;
    static RhinoObjectSpaceMorphCallback g_the_space_morph_callback;
    static RhinoObjectPickCallback g_the_pick_callback;
    static RhinoObjectPickedCallback g_the_picked_callback;
    static RhinoObjectDeletedCallback g_the_delete_callback;

    void OnRhinoObjectDraw(IntPtr pConstRhinoObject, IntPtr pDisplayPipeline)
    {
      // 8 Aug 2014 S. Baer (RH-28405)
      // Don't allow plug-in code to bring down Rhino.
      try
      {
        OnDraw(new Display.DrawEventArgs(pDisplayPipeline, 0));
      }
      catch (Exception ex)
      {
        Runtime.HostUtils.ExceptionReport(ex);
      }
    }

    internal static bool SubclassCreateNativePointer { get; set; }
    static RhinoObject()
    {
      SubclassCreateNativePointer = true;
    }

    static void OnRhinoObjectDuplicate(uint docSerialNumber, uint sourceObjectSerialNumber, uint newObjectSerialNumber, IntPtr newObjectPointer)
    {
      var doc = RhinoDoc.FromRuntimeSerialNumber(docSerialNumber);
      if (doc != null)
      {
        var rhobj = doc.Objects.FindCustomObject(sourceObjectSerialNumber);
        if (rhobj != null)
        {
          var t = rhobj.GetType();
          SubclassCreateNativePointer = false;
          var newobj = Activator.CreateInstance(t) as RhinoObject;
          SubclassCreateNativePointer = true;
          if (newobj != null)
          {
            newobj.m_rhinoobject_serial_number = newObjectSerialNumber;
            newobj.m_pRhinoObject = newObjectPointer;
            doc.Objects.AddCustomObjectForTracking(newObjectSerialNumber, newobj, newObjectPointer);
            try
            {
              // 7 March 2013 S. Baer (RH-16792)
              // Don't allow plug-in code to bring down Rhino.
              newobj.OnDuplicate(rhobj);
            }
            catch (Exception ex)
            {
              Runtime.HostUtils.ExceptionReport(ex);
            }
          }
        }
      }
    }

    static System.Collections.Generic.List<RhinoObject> g_custom_objects;
    static System.Collections.Generic.List<Type> g_custom_object_types;
    static void RegisterCustomObjectType(Type t)
    {
      if (t == null)
        throw new ArgumentNullException();
      if (g_custom_object_types == null)
        g_custom_object_types = new System.Collections.Generic.List<Type>();
      foreach (var registered in g_custom_object_types)
      {
        if (registered.GUID == t.GUID)
          return;
      }
      g_custom_object_types.Add(t);
    }

    static void OnRhinoObjectDocNotify(uint docSerialNumber, uint serialNumber, int add, Guid managedTypeId)
    {
      try
      {
        var doc = RhinoDoc.FromRuntimeSerialNumber(docSerialNumber);
        if (doc != null)
        {
          var rhobj = doc.Objects.FindCustomObject(serialNumber);
          if (rhobj == null && add == 1 && managedTypeId!=Guid.Empty && g_custom_object_types!=null)
          {
            // create the managed parallel object and add it to the document list of custom objects
            foreach (var registered in g_custom_object_types)
            {
              if (registered.GUID == managedTypeId)
              {
                SubclassCreateNativePointer = false;
                rhobj = Activator.CreateInstance(registered) as RhinoObject;
                SubclassCreateNativePointer = true;
                if (rhobj != null)
                {
                  rhobj.m_rhinoobject_serial_number = serialNumber;
                  var ptr_object = UnsafeNativeMethods.CRhinoDoc_LookupObjectByRuntimeSerialNumber(docSerialNumber, serialNumber);
                  rhobj.m_pRhinoObject = ptr_object;
                  doc.Objects.AddCustomObjectForTracking(serialNumber, rhobj, ptr_object);
                }
                break;
              }
            }
          }

          if (rhobj != null)
          {
            // This is a perfect time to set up or tear down the virtual function
            // callbacks on a custom rhino object
            var ptrObject = UnsafeNativeMethods.CRhinoDoc_LookupObjectByRuntimeSerialNumber(docSerialNumber, serialNumber);
            rhobj.RegisterPerObjectCallbacks(add == 1, ptrObject);
            if (add == 1)
            {
              if (g_custom_objects == null)
                g_custom_objects = new System.Collections.Generic.List<RhinoObject>();
              g_custom_objects.Add(rhobj);
              rhobj.OnAddToDocument(doc);
            }
            else
              rhobj.OnDeleteFromDocument(doc);

            // 8 May 2014 S. Baer (RH-21330)
            // When a custom object is added to or deleted from the document
            // clear out the "edited" geometry and attributes since we don't
            // want them hanging around and causing problems in the future
            rhobj.m_edited_attributes = null;
            rhobj.m_edited_geometry = null;
            rhobj.m_edited_material = null;
            rhobj.m_edited_material_index = -1;

            // 9 Jan 2018 S. Baer
            // We should not hold on to m_pRhinoObject if this class is associated with a RhinoDoc
            // in any way.
            rhobj.m_pRhinoObject = IntPtr.Zero;
          }
        }
      }
      catch (Exception ex)
      {
        // 8 Feb 2013 S. Baer (RH-15766)
        // Add exception handling since some custom objects were throwing
        // exceptions and that is very dangerous during unmanaged callbacks
        // like this
        Runtime.HostUtils.ExceptionReport(ex);
      }
    }

    static int OnRhinoObjectActiveInViewport(uint docSerialNumber, uint serialNumber, IntPtr pRhinoViewport)
    {
      var rc = -1;
      var doc = RhinoDoc.FromRuntimeSerialNumber(docSerialNumber);
      if (doc != null)
      {
        var rhobj = doc.Objects.FindCustomObject(serialNumber);
        if (rhobj != null)
          rc = rhobj.IsActiveInViewport(new Display.RhinoViewport(null, pRhinoViewport)) ? 1 : 0;
      }
      return rc;
    }

    static void OnRhinoObjectSelection(uint docSerialNumber, uint serialNumber)
    {
      // 8 Aug 2014 S. Baer (RH-28405)
      // Don't allow plug-in code to bring down Rhino.
      try
      {
        var doc = RhinoDoc.FromRuntimeSerialNumber(docSerialNumber);
        if (doc != null)
        {
          var rhobj = doc.Objects.FindCustomObject(serialNumber);
          if (rhobj != null)
            rhobj.OnSelectionChanged();
        }
      }
      catch (Exception ex)
      {
        Runtime.HostUtils.ExceptionReport(ex);
      }
    }

    static void OnRhinoObjectSpaceMorph(uint docSerialNumber, uint serialNumber, IntPtr pConstSpaceMorph)
    {
      var doc = RhinoDoc.FromRuntimeSerialNumber(docSerialNumber);
      if (doc != null)
      {
        var rhobj = doc.Objects.FindCustomObject(serialNumber);
        if (rhobj != null)
        {
          var sm = new NativeSpaceMorphWrapper(pConstSpaceMorph);
          rhobj.OnSpaceMorph(sm);
          sm.m_pSpaceMorph = IntPtr.Zero; // null the pointer out in case someone accidentally holds onto the spacemorph
        }
      }
    }

    static void OnRhinoObjectTransform(uint docSerialNumber, uint serialNumber, IntPtr pConstTransform)
    {
      var doc = RhinoDoc.FromRuntimeSerialNumber(docSerialNumber);
      if (doc != null)
      {
        var rhobj = doc.Objects.FindCustomObject(serialNumber);
        if (rhobj != null)
        {
          var xf = (Transform)System.Runtime.InteropServices.Marshal.PtrToStructure(pConstTransform, typeof(Transform));
          rhobj.OnTransform(xf);
        }
      }
    }

    static void OnRhinoObjectDeleted(uint serialNumber)
    {
      if (g_custom_objects != null)
      {
        for (var i = 0; i < g_custom_objects.Count; i++)
        {
          var rhobj = g_custom_objects[i];
          if (rhobj != null && rhobj.m_rhinoobject_serial_number == serialNumber)
          {
            rhobj.m_pRhinoObject = IntPtr.Zero;
            rhobj.m_rhinoobject_serial_number = 0;
            rhobj.RegisterPerObjectCallbacks(false, IntPtr.Zero);
            GC.SuppressFinalize(rhobj);
            g_custom_objects.RemoveAt(i);
            return;
          }
        }
      }
    }

    static System.Collections.Generic.IEnumerable<ObjRef> ObjRefCollectionFromIntPtr(IntPtr pRhinoObjRefArray, int count)
    {
      for (var i = count - 1; i >= 0; i--)
      {
        var ptr_rhinoobjref = UnsafeNativeMethods.CRhinoObjRefArray_GetLastItem(pRhinoObjRefArray, i);
        if( IntPtr.Zero!=ptr_rhinoobjref )
        {
          yield return new ObjRef(ptr_rhinoobjref);
        }
      }
    }

    static void OnRhinoObjectPick(uint docSerialNumber, uint serialNumber, IntPtr pConstRhinoPickContext, IntPtr pRhinoObjRefArray)
    {
      var doc = RhinoDoc.FromRuntimeSerialNumber(docSerialNumber);
      if (doc != null)
      {
        var rhobj = doc.Objects.FindCustomObject(serialNumber);
        if (rhobj != null)
        {
          System.Collections.Generic.IEnumerable<ObjRef> objs = rhobj.OnPick(new Input.Custom.PickContext(pConstRhinoPickContext));
          if (objs != null)
          {
            foreach (var objref in objs)
            {
              var p_const_obj_ref = objref.ConstPointer();
              UnsafeNativeMethods.CRhinoObjRefArray_Append(pRhinoObjRefArray, p_const_obj_ref);
            }
          }
        }
      }
    }

    static void OnRhinoObjectPicked(uint docSerialNumber, uint serialNumber, IntPtr pConstRhinoPickContext, IntPtr pRhinoObjRefArray, int count)
    {
      var doc = RhinoDoc.FromRuntimeSerialNumber(docSerialNumber);
      if (doc != null)
      {
        var rhobj = doc.Objects.FindCustomObject(serialNumber);
        var list = ObjRefCollectionFromIntPtr(pRhinoObjRefArray, count);
        if (rhobj != null)
        {
          rhobj.OnPicked(new Input.Custom.PickContext(pConstRhinoPickContext), list);
        }
      }
    }

    internal static RhinoObject CreateRhinoObjectHelper(IntPtr pRhinoObject)
    {
      if (IntPtr.Zero == pRhinoObject)
        return null;

      var sn = UnsafeNativeMethods.CRhinoObject_RuntimeSN(pRhinoObject);
      if (sn < 1)
        return null;

      var doc_id = UnsafeNativeMethods.CRhinoObject_Document(pRhinoObject);
      var doc = RhinoDoc.FromRuntimeSerialNumber(doc_id);
      if (doc != null)
      {
        var custom = doc.Objects.FindCustomObject(sn);
        if (custom != null)
          return custom;
      }

      var type = UnsafeNativeMethods.CRhinoRhinoObject_GetRhinoObjectType(pRhinoObject);
      if (type < 0)
        return null;
      RhinoObject rc;
      switch (type)
      {
        case UnsafeNativeMethods.RhinoObjectTypeConsts.CRhinoPointObject: //1
          rc = new PointObject(sn);
          break;
        case UnsafeNativeMethods.RhinoObjectTypeConsts.CRhinoCurveObject: //2
          rc = new CurveObject(sn);
          break;
        case UnsafeNativeMethods.RhinoObjectTypeConsts.CRhinoMeshObject: //3
          rc = new MeshObject(sn);
          break;
        case UnsafeNativeMethods.RhinoObjectTypeConsts.CRhinoBrepObject: //4
          rc = new BrepObject(sn);
          break;
        case UnsafeNativeMethods.RhinoObjectTypeConsts.CRhinoPointCloudObject: //5
          rc = new PointCloudObject(sn);
          break;
        case UnsafeNativeMethods.RhinoObjectTypeConsts.CRhinoSurfaceObject: //6
          rc = new SurfaceObject(sn);
          break;
        case UnsafeNativeMethods.RhinoObjectTypeConsts.CRhinoInstanceObject: //7
          rc = new InstanceObject(sn);
          break;
        case UnsafeNativeMethods.RhinoObjectTypeConsts.CRhinoHatchObject: //8
          rc = new HatchObject(sn);
          break;
        case UnsafeNativeMethods.RhinoObjectTypeConsts.CRhinoDetailViewObject: //9
          rc = new DetailViewObject(sn);
          break;
        case UnsafeNativeMethods.RhinoObjectTypeConsts.CRhinoClippingPlaneObject: //10
          rc = new ClippingPlaneObject(sn);
          break;
        case UnsafeNativeMethods.RhinoObjectTypeConsts.CRhinoTextDot: //11
          rc = new TextDotObject(sn);
          break;
        case UnsafeNativeMethods.RhinoObjectTypeConsts.CRhinoGripObject: //12
          rc = new GripObject(sn);
          break;
        case UnsafeNativeMethods.RhinoObjectTypeConsts.CRhinoExtrusionObject: //13
          rc = new ExtrusionObject(sn);
          break;
        case UnsafeNativeMethods.RhinoObjectTypeConsts.CRhinoLight: //14
          rc = new LightObject(sn);
          break;
        case UnsafeNativeMethods.RhinoObjectTypeConsts.CRhinoMorphControl: //15
          rc = new MorphControlObject(sn);
          break;
        case UnsafeNativeMethods.RhinoObjectTypeConsts.CRhinoText: // 16
          rc = new TextObject(sn);
          break;
        case UnsafeNativeMethods.RhinoObjectTypeConsts.CRhinoLeader: //17
          rc = new LeaderObject(sn);
          break;
        case UnsafeNativeMethods.RhinoObjectTypeConsts.CRhinoDimLinear: //18
          rc = new LinearDimensionObject(sn);
          break;
        case UnsafeNativeMethods.RhinoObjectTypeConsts.CRhinoDimAngular: //19
          rc = new AngularDimensionObject(sn);
          break;
        case UnsafeNativeMethods.RhinoObjectTypeConsts.CRhinoDimRadial: //20
          rc = new RadialDimensionObject(sn);
          break;
        case UnsafeNativeMethods.RhinoObjectTypeConsts.CRhinoDimOrdinate: //21
          rc = new OrdinateDimensionObject(sn);
          break;
        case UnsafeNativeMethods.RhinoObjectTypeConsts.CRhinoCentermark: //22
          rc = new CentermarkObject(sn);
          break;
        case UnsafeNativeMethods.RhinoObjectTypeConsts.CRhinoAnnotation: //23
          rc = new AnnotationObjectBase(sn);
          break;
        case UnsafeNativeMethods.RhinoObjectTypeConsts.CRhinoDimension: //24
          rc = new DimensionObject(sn);
          break;
        case UnsafeNativeMethods.RhinoObjectTypeConsts.CRhinoSubDObject: // 25
          rc = new SubDObject(sn);
          break;
        case UnsafeNativeMethods.RhinoObjectTypeConsts.CRhinoProxyObject: // 26
          rc = new ProxyObject(sn);
          break;
        case UnsafeNativeMethods.RhinoObjectTypeConsts.CRhinoNamedViewCameraIcon: // 27
          rc = new NamedViewWidgetObject(sn);
          break;
        default:
          rc = new RhinoObject(sn);
          break;
      }

      if( doc==null )
      {
        rc.m_pRhinoObject = pRhinoObject;
      }
      return rc;
    }
    #endregion

  #region statics
    /// <summary>
    /// Gets the runtime serial number that will be assigned to
    /// the next Rhino Object that is created.
    /// </summary>
    /// <since>5.0</since>
    [CLSCompliant(false)]
    public static uint NextRuntimeSerialNumber
    {
      get
      {
        return UnsafeNativeMethods.CRhinoObject_NextRuntimeObjectSerialNumber();
      }
    }

    /// <summary> Get a Rhino object for a unique runtime serial number </summary>
    /// <param name="serialNumber"></param>
    /// <returns></returns>
    /// <since>6.0</since>
    [CLSCompliant(false)]
    public static RhinoObject FromRuntimeSerialNumber(uint serialNumber)
    {
      IntPtr ptr_object = UnsafeNativeMethods.RHC_LookupObjectBySerialNumber(serialNumber);
      return CreateRhinoObjectHelper(ptr_object);
    }

    /// <summary>Meshes Rhino objects.</summary>
    /// <param name="rhinoObjects">The Rhino objects to mesh.</param>
    /// <param name="parameters">The parameters used to create the meshes.</param>
    /// <param name="meshes">The created meshes are appended to this array.</param>
    /// <param name="attributes">The object attributes that coincide with each created mesh are appended to this array.</param>
    /// <returns>The results of the calculation.</returns>
    /// <since>5.9</since>
    public static Commands.Result MeshObjects(System.Collections.Generic.IEnumerable<RhinoObject> rhinoObjects, MeshingParameters parameters, out Mesh[] meshes, out ObjectAttributes[] attributes)
    {
      using (var rhinoobject_array = new Runtime.InternalRhinoObjectArray(rhinoObjects))
      using (var mesh_array = new SimpleArrayMeshPointer())
      {
        var ptr_rhinoobject_array = rhinoobject_array.NonConstPointer();
        var ui_style = -1; //no user interface
        var ptr_mesh_parameters = parameters.NonConstPointer();
        var ptr_mesharray = mesh_array.NonConstPointer();
        var ptr_attributesarray = UnsafeNativeMethods.ON_SimpleArray_3dmObjectAttributes_New();
        var rc = UnsafeNativeMethods.RHC_RhinoMeshObjects(ptr_rhinoobject_array, ref ui_style, ptr_mesh_parameters, ptr_mesharray, ptr_attributesarray);
        meshes = mesh_array.ToNonConstArray();
        var attrib_count = UnsafeNativeMethods.ON_SimpleArray_3dmObjectAttributes_Count(ptr_attributesarray);
        attributes = new ObjectAttributes[attrib_count];
        for (var i = 0; i < attrib_count; i++)
        {
          var ptr_attribute = UnsafeNativeMethods.ON_SimpleArray_3dmObjectAttributes_Get(ptr_attributesarray, i);
          attributes[i] = new ObjectAttributes(ptr_attribute);
        }
        UnsafeNativeMethods.ON_SimpleArray_3dmObjectAttributes_Delete(ptr_attributesarray);
        return (Commands.Result)rc;
      }
    }

    /// <summary>Meshes Rhino objects.</summary>
    /// <param name="rhinoObjects">The Rhino objects to mesh.</param>
    /// <param name="parameters">The parameters used to create the meshes. The meshing parameters may change.</param>
    /// <param name="simpleDialog">true to display the simple mesh parameters dialog, false to display the detailed mesh parameters dialog.</param>
    /// <param name="meshes">The created meshes are appended to this array.</param>
    /// <param name="attributes">The object attributes that coincide with each created mesh are appended to this array.</param>
    /// <returns>The results of the calculation.</returns>
    /// <since>5.9</since>
    public static Commands.Result MeshObjects(System.Collections.Generic.IEnumerable<RhinoObject> rhinoObjects, ref MeshingParameters parameters, ref bool simpleDialog, out Mesh[] meshes, out ObjectAttributes[] attributes)
    {
      using (var rhinoobject_array = new Runtime.InternalRhinoObjectArray(rhinoObjects))
      using (var mesh_array = new SimpleArrayMeshPointer())
      {
        var ptr_rhinoobject_array = rhinoobject_array.NonConstPointer();
        var ui_style = simpleDialog ? 0: 1;
        var ptr_mesh_parameters = parameters.NonConstPointer();
        var ptr_mesharray = mesh_array.NonConstPointer();
        var ptr_attributesarray = UnsafeNativeMethods.ON_SimpleArray_3dmObjectAttributes_New();
        var rc = UnsafeNativeMethods.RHC_RhinoMeshObjects(ptr_rhinoobject_array, ref ui_style, ptr_mesh_parameters, ptr_mesharray, ptr_attributesarray);
        simpleDialog = ui_style == 0;
        meshes = mesh_array.ToNonConstArray();
        var attrib_count = UnsafeNativeMethods.ON_SimpleArray_3dmObjectAttributes_Count(ptr_attributesarray);
        attributes = new ObjectAttributes[attrib_count];
        for (var i = 0; i < attrib_count; i++)
        {
          var ptr_attribute = UnsafeNativeMethods.ON_SimpleArray_3dmObjectAttributes_Get(ptr_attributesarray, i);
          attributes[i] = new ObjectAttributes(ptr_attribute);
        }
        UnsafeNativeMethods.ON_SimpleArray_3dmObjectAttributes_Delete(ptr_attributesarray);
        return (Commands.Result)rc;
      }
    }

    /// <summary>Meshes Rhino objects.</summary>
    /// <param name="rhinoObjects">The Rhino objects to mesh.</param>
    /// <param name="parameters">The parameters used to create the meshes. The meshing parameters may change.</param>
    /// <param name="uiStyle">The user interface style, where: -1 = no interface, 0 = simple dialog, 1 = details dialog, 2 = script or batch mode</param>
    /// <param name="xform">Transform to be used for export with origin, or Z to Y up, etc.</param>
    /// <param name="meshes">The created meshes are appended to this array.</param>
    /// <param name="attributes">The object attributes that coincide with each created mesh are appended to this array.</param>
    /// <returns>The results of the calculation.</returns>
    /// <since>6.0</since>
    public static Commands.Result MeshObjects(System.Collections.Generic.IEnumerable<RhinoObject> rhinoObjects, ref MeshingParameters parameters, ref int uiStyle, Transform xform, out Mesh[] meshes, out ObjectAttributes[] attributes)
    {
      using (var rhinoobject_array = new Runtime.InternalRhinoObjectArray(rhinoObjects))
      using (var mesh_array = new SimpleArrayMeshPointer())
      {
        var ptr_rhinoobject_array = rhinoobject_array.NonConstPointer();
        var ptr_mesh_parameters = parameters.NonConstPointer();
        var ptr_mesharray = mesh_array.NonConstPointer();
        var ptr_attributesarray = UnsafeNativeMethods.ON_SimpleArray_3dmObjectAttributes_New();
        var rc = UnsafeNativeMethods.RHC_RhinoMeshObjectsWithTransform(ptr_rhinoobject_array, ref uiStyle, ref xform, ptr_mesh_parameters, ptr_mesharray, ptr_attributesarray);
        meshes = mesh_array.ToNonConstArray();
        var attrib_count = UnsafeNativeMethods.ON_SimpleArray_3dmObjectAttributes_Count(ptr_attributesarray);
        attributes = new ObjectAttributes[attrib_count];
        for (var i = 0; i < attrib_count; i++)
        {
          var ptr_attribute = UnsafeNativeMethods.ON_SimpleArray_3dmObjectAttributes_Get(ptr_attributesarray, i);
          attributes[i] = new ObjectAttributes(ptr_attribute);
        }
        UnsafeNativeMethods.ON_SimpleArray_3dmObjectAttributes_Delete(ptr_attributesarray);
        return (Commands.Result)rc;
      }
    }

    /// <summary>
    /// Gets the render meshes of some objects.
    /// </summary>
    /// <param name="rhinoObjects">An array, a list, or any enumerable set of Rhino objects.</param>
    /// <param name="okToCreate">true if the method is allowed to instantiate new meshes if they do not exist.</param>
    /// <param name="returnAllObjects">true if all objects should be returned.</param>
    /// <returns>An array of object references.</returns>
    /// <since>5.0</since>
    public static ObjRef[] GetRenderMeshes(System.Collections.Generic.IEnumerable<RhinoObject> rhinoObjects, bool okToCreate, bool returnAllObjects)
    {
      var rhinoobject_array = new Runtime.InternalRhinoObjectArray(rhinoObjects);
      var p_rh_object_array = rhinoobject_array.NonConstPointer();
      var p_obj_ref_array = UnsafeNativeMethods.RHC_RhinoGetRenderMeshes(p_rh_object_array, okToCreate, returnAllObjects, true, false);
      rhinoobject_array.Dispose();

      if (IntPtr.Zero == p_obj_ref_array)
        return new ObjRef[0];

      var count = UnsafeNativeMethods.RhinoObjRefArray_Count(p_obj_ref_array);
      if (count < 1)
        return new ObjRef[0];

      var rc = new ObjRef[count];
      for (var i = 0; i < count; i++)
      {
        var p_const_obj_ref = UnsafeNativeMethods.RhinoObjRefArray_GetItem(p_obj_ref_array, i);
        rc[i] = new ObjRef(p_const_obj_ref);
      }
      UnsafeNativeMethods.RhinoObjRefArray_Delete(p_obj_ref_array);
      return rc;
    }


    /// <summary>
    /// Gets the render meshes of some objects.
    /// </summary>
    /// <param name="rhinoObjects">An array, a list, or any enumerable set of Rhino objects.</param>
    /// <param name="okToCreate">true if the method is allowed to instantiate new meshes if they do not exist.</param>
    /// <param name="returnAllObjects">true if all objects should be returned.</param>
    /// <param name="skipHiddenObjects">true if if hidden objects should be ignored.</param>
    /// <param name="updateMeshTCs">true if the TCs should be updated with a texture mapping.</param>
    /// <returns>An array of object references.</returns>
    /// <since>7.3</since>
    public static ObjRef[] GetRenderMeshesWithUpdatedTCs(
      System.Collections.Generic.IEnumerable<RhinoObject> rhinoObjects, 
      bool okToCreate, 
      bool returnAllObjects,
      bool skipHiddenObjects,
      bool updateMeshTCs
      )
    {
      var rhinoobject_array = new Runtime.InternalRhinoObjectArray(rhinoObjects);
      var p_rh_object_array = rhinoobject_array.NonConstPointer();
      var p_obj_ref_array = UnsafeNativeMethods.RHC_RhinoGetRenderMeshes(p_rh_object_array, okToCreate, returnAllObjects, skipHiddenObjects, updateMeshTCs);
      rhinoobject_array.Dispose();

      if (IntPtr.Zero == p_obj_ref_array)
        return new ObjRef[0];

      var count = UnsafeNativeMethods.RhinoObjRefArray_Count(p_obj_ref_array);
      if (count < 1)
        return new ObjRef[0];

      var rc = new ObjRef[count];
      for (var i = 0; i < count; i++)
      {
        var p_const_obj_ref = UnsafeNativeMethods.RhinoObjRefArray_GetItem(p_obj_ref_array, i);
        rc[i] = new ObjRef(p_const_obj_ref);
      }
      UnsafeNativeMethods.RhinoObjRefArray_Delete(p_obj_ref_array);
      return rc;
    }

    /// <summary>
    /// Return list of fill surfaces if any for object and clipping plane.
    /// </summary>
    /// <param name="rhinoObject">Object to clip</param>
    /// <param name="clippingPlaneObject">Clipping plane to use</param>
    /// <returns></returns>
    /// <since>6.7</since>
    public static Brep[] GetFillSurfaces(RhinoObject rhinoObject, ClippingPlaneObject clippingPlaneObject)
    {
      using (var fillBreps = new SimpleArrayBrepPointer())
      {
        using (var cps = new SimpleArrayClippingPlaneObjectPointer())
        {
          cps.Add(clippingPlaneObject, true);
          if (UnsafeNativeMethods.RHC_RhinoObjectGetFillSurfaces(rhinoObject.ConstPointer(), cps.NonConstPointer(), fillBreps.NonConstPointer(), false))
          {
            return fillBreps.ToNonConstArray();
          }
        }
      }
      return null;
    }

    /// <summary>
    /// Return list of fill surfaces if any for object and clipping plane. Fills are trimmed by
    /// clipping planes that did not generate them.
    /// </summary>
    /// <param name="rhinoObject">Object to clip</param>
    /// <param name="clippingPlaneObjects">Enumeration of clipping plane objects</param>
    /// <returns>Array of Brep containing fully trimmed fills if there were any generated.</returns>
    /// <since>6.7</since>
    public static Brep[] GetFillSurfaces(RhinoObject rhinoObject, IEnumerable<ClippingPlaneObject> clippingPlaneObjects)
    {
      return GetFillSurfaces(rhinoObject, clippingPlaneObjects, false);
    }

    /// <summary>
    /// Return list of fill surfaces if any for object and clipping plane.
    /// </summary>
    /// <param name="rhinoObject">Object to clip</param>
    /// <param name="clippingPlaneObjects">Enumeration of clipping plane objects</param>
		/// <param name="unclippedFills">Use true to get fills that are not trimmed by all clipping planes</param>
    /// <returns>Array of Brep containing fills if there were any generated, trimmed if unclippedFills was false</returns>
    /// <since>6.7</since>
    public static Brep[] GetFillSurfaces(RhinoObject rhinoObject, IEnumerable<ClippingPlaneObject> clippingPlaneObjects, bool unclippedFills)
    {
      using (var fillBreps = new SimpleArrayBrepPointer())
      {
        using (var cps = new SimpleArrayClippingPlaneObjectPointer())
        {
          foreach (var clippingPlaneObject in clippingPlaneObjects)
          {
            cps.Add(clippingPlaneObject, true);
          }
          if (UnsafeNativeMethods.RHC_RhinoObjectGetFillSurfaces(rhinoObject.ConstPointer(), cps.NonConstPointer(), fillBreps.NonConstPointer(), unclippedFills))
          {
            return fillBreps.ToNonConstArray();
          }
        }
      }
      return null;
    }

    /// <summary>
    /// Gets a world XY-plane aligned tight bounding box from a collection of Rhino objects.
    /// </summary>
    /// <param name="rhinoObjects">A collection of Rhino objects.</param>
    /// <param name="boundingBox">A tight bounding box.</param>
    /// <returns></returns>
    /// <since>7.0</since>
    public static bool GetTightBoundingBox(IEnumerable<RhinoObject> rhinoObjects, out BoundingBox boundingBox)
    {
      boundingBox = BoundingBox.Unset;
      using (var obj_array = new Runtime.InternalRhinoObjectArray(rhinoObjects))
      {
        var ptr_obj_array = obj_array.NonConstPointer();
        return UnsafeNativeMethods.RHC_RhinoGetTightBoundingBox(ptr_obj_array, ref boundingBox);
      }
    }

    /// <summary>
    /// Gets a plane aligned tight bounding box from a collection of Rhino objects.
    /// </summary>
    /// <param name="rhinoObjects">A collection of Rhino objects.</param>
    /// <param name="plane">A valid alignment plane.</param>
    /// <param name="boundingBox">A tight bounding box.</param>
    /// <returns></returns>
    /// <since>7.0</since>
    public static bool GetTightBoundingBox(IEnumerable<RhinoObject> rhinoObjects, Plane plane, out BoundingBox boundingBox)
    {
      boundingBox = BoundingBox.Unset;
      using (var obj_array = new Runtime.InternalRhinoObjectArray(rhinoObjects))
      {
        var ptr_obj_array = obj_array.NonConstPointer();
        return UnsafeNativeMethods.RHC_RhinoGetTightBoundingBox2(ptr_obj_array, ref plane, ref boundingBox);
      }
    }

    #endregion

    #region properties
    /// <summary>
    /// Gets the Rhino-based object type.
    /// </summary>
    /// <since>5.0</since>
    [CLSCompliant(false)]
    public ObjectType ObjectType
    {
      get
      {
        var ptr = ConstPointer();
        return (ObjectType)UnsafeNativeMethods.CRhinoObject_ObjectType(ptr);
      }
    }


    /// <summary>
    /// Gets the document that owns this object.
    /// </summary>
    /// <since>5.0</since>
    public RhinoDoc Document
    {
      get
      {
        var const_ptr = ConstPointer();
        var sn = UnsafeNativeMethods.CRhinoObject_Document(const_ptr);
        return RhinoDoc.FromRuntimeSerialNumber(sn);
      }
    }


    /// <summary>
    /// Gets the underlying geometry for this object.
    /// <para>All rhino objects are composed of geometry and attributes.</para>
    /// </summary>
    /// <since>5.0</since>
    public virtual GeometryBase Geometry
    {
      get
      {
        if (null != m_edited_geometry)
          return m_edited_geometry;

        if (null == m_original_geometry)
        {
          var ci = new ComponentIndex();
          // use the "const" geometry that is associated with this RhinoObject
          var const_ptr_this = ConstPointer();
          var const_ptr_geometry = UnsafeNativeMethods.CRhinoObject_Geometry(const_ptr_this, ci);
          if (IntPtr.Zero == const_ptr_geometry)
            return null;

          m_original_geometry = GeometryBase.CreateGeometryHelper(const_ptr_geometry, this);
        }
        return m_original_geometry;
      }
      // We can also implement set if it makes sense.
      // One thing we would have to do is make sure that the geometry is of the correct type
      // It might be better to place this type of functionality on the subclass specific
      // geometry getters (CurveObject.CurveGeometry)
    }


    /// <summary>
    /// Gets or sets the object attributes.
    /// </summary>
    /// <since>5.0</since>
    public virtual ObjectAttributes Attributes
    {
      get
      {
        if (null != m_edited_attributes)
          return m_edited_attributes;

        return m_original_attributes ?? (m_original_attributes = new ObjectAttributes(this));
      }
      set
      {
        // make sure this object is still valid - ConstPointer will throw a DocumentCollectedException
        // if the object is no longer in existance
        ConstPointer();
        if (m_edited_attributes == value || Document == null)
          return;
        // make sure the edited attributes still have the same object id
        // so things like CommitChanges will continue to work
        var object_id = Id;
        m_edited_attributes = value.Duplicate();
        m_edited_attributes.ObjectId = object_id;
      }
    }

    /// <summary>
    /// Gets the objects runtime serial number.
    /// </summary>
    /// <since>5.0</since>
    [CLSCompliant(false)]
    public uint RuntimeSerialNumber
    {
      get
      {
        if (m_rhinoobject_serial_number != 0)
          return m_rhinoobject_serial_number;
        var const_ptr_this = ConstPointer();
        return UnsafeNativeMethods.CRhinoObject_RuntimeSN(const_ptr_this);
      }
    }

    bool GetBool(UnsafeNativeMethods.RhinoObjectGetBool which)
    {
      var ptr = ConstPointer();
      return UnsafeNativeMethods.CRhinoObject_GetBool(ptr, which);
    }

    /// <summary>
    /// Returns true if object is a closed solid, otherwise false.
    /// </summary>
    /// <since>7.6</since>
    public bool IsSolid
    {
      get 
      { 
        return GetBool(UnsafeNativeMethods.RhinoObjectGetBool.IsSolid); 
      }
    }

    /// <summary>
    /// Some objects cannot be deleted, like grips on lights and annotation objects. 
    /// </summary>
    /// <since>5.0</since>
    public bool IsDeletable
    {
      get { return GetBool(UnsafeNativeMethods.RhinoObjectGetBool.IsDeletable); }
      protected set
      {
        // only custom subclasses can set this flag
        var p_const_this = ConstPointer();
        UnsafeNativeMethods.CRhinoCustomObject_SetIsDeletable(p_const_this, value);
      }
    }

    /// <summary>
    /// true if the object is deleted. Deleted objects are kept by the document
    /// for undo purposes. Call RhinoDoc.UndeleteObject to undelete an object.
    /// </summary>
    /// <since>5.0</since>
    public bool IsDeleted
    {
      get { return GetBool(UnsafeNativeMethods.RhinoObjectGetBool.IsDeleted); }
    }

    /// <summary>
    /// true if the object is used as part of an instance definition.   
    /// </summary>
    /// <since>5.0</since>
    public bool IsInstanceDefinitionGeometry
    {
      get { return GetBool(UnsafeNativeMethods.RhinoObjectGetBool.IsInstanceDefinitionGeometry); }
    }

    /// <summary>
    /// An object must be in one of three modes: normal, locked or hidden.
    /// If an object is in normal mode, then the object's layer controls visibility
    /// and selectability. If an object is locked, then the object's layer controls
    /// visibility by the object cannot be selected. If the object is hidden, it is
    /// not visible and it cannot be selected.
    /// </summary>
    /// <since>5.0</since>
    public bool IsNormal
    {
      get { return GetBool(UnsafeNativeMethods.RhinoObjectGetBool.IsNormal); }
    }

    /// <summary>
    /// An object must be in one of three modes: normal, locked or hidden.
    /// If an object is in normal mode, then the object's layer controls visibility
    /// and selectability. If an object is locked, then the object's layer controls
    /// visibility by the object cannot be selected. If the object is hidden, it is
    /// not visible and it cannot be selected.
    /// </summary>
    /// <since>5.0</since>
    public bool IsLocked
    {
      get { return GetBool(UnsafeNativeMethods.RhinoObjectGetBool.IsLocked); }
    }

    /// <summary>
    /// An object must be in one of three modes: normal, locked or hidden.
    /// If an object is in normal mode, then the object's layer controls visibility
    /// and selectability. If an object is locked, then the object's layer controls
    /// visibility by the object cannot be selected. If the object is hidden, it is
    /// not visible and it cannot be selected.
    /// </summary>
    /// <since>5.0</since>
    public bool IsHidden
    {
      get { return GetBool(UnsafeNativeMethods.RhinoObjectGetBool.IsHidden); }
    }

    /// <summary>
    /// Gets a value indicating if an object is a reference object. An object from a work session
    /// reference model is a reference object and cannot be modified. An object is
    /// a reference object if, and only if, it is on a reference layer.
    /// </summary>
    /// <since>5.0</since>
    public bool IsReference
    {
      get { return GetBool(UnsafeNativeMethods.RhinoObjectGetBool.IsReference); }
    }

    /// <summary>
    /// Obsolete - use ReferenceModelSerialNumber
    /// </summary>
    /// <since>6.3</since>
    [CLSCompliant(false)]
    public uint WorksessionReferenceSerialNumber
    {
      get { return ReferenceModelSerialNumber; }
    }

    /// <summary>
    /// When a component is in a model for reference, this value identifies the reference model.
    /// </summary>
    /// <remarks>
    /// Reference components are not saved in .3dm archives. 
    /// Typically this value is set and locked by the code that adds a component to a model.
    /// This value is not saved in .3dm archives.
    /// </remarks>
    /// <since>6.12</since>
    [CLSCompliant(false)]
    public override uint ReferenceModelSerialNumber
    {
      get { return (uint)GetInt(UnsafeNativeMethods.RhinoObjectGetInt.WorksessionRefSerialNumber); }
    }

    /// <summary>
    /// When a component is in a model as part of the information required for a linked instance definition,
    /// this value identifies the linked instance definition reference model.
    /// </summary>
    /// <remarks>
    /// Reference components are not saved in .3dm archives. 
    /// Typically this value is set and locked by the code that adds a component to a model.
    /// This value is not saved in .3dm archives.
    /// </remarks>
    /// <since>6.12</since>
    [CLSCompliant(false)]
    public override uint InstanceDefinitionModelSerialNumber
    {
      get { return (uint)GetInt(UnsafeNativeMethods.RhinoObjectGetInt.LinkedInstanceDefinitionSerialNumber); }
    }

    /// <summary>Gets the object visibility.</summary>
    /// <since>5.0</since>
    public bool Visible
    {
      get
      {
        // 4 April 2016 - John Morse
        // http://mcneel.myjetbrains.com/youtrack/issue/RH-33541
        // Need to make sure the object and its layer are visible to determine
        // if the object is visible on the screen, do NOT just check the objects
        // attribute visibility state, it can be true when the objects layer is
        // off and the object is NOT visible.
        return GetBool(UnsafeNativeMethods.RhinoObjectGetBool.IsVisible);
      }
    }

    //internal bool InternalIsSolid()
    //{
    //  return GetBool(idxIsSolid);
    //}

    #endregion

    /// <summary>
    /// Constructs a deep (full) copy of the geometry.
    /// </summary>
    /// <returns>A copy of the internal geometry.</returns>
    /// <example>
    /// <code source='examples\vbnet\ex_duplicateobject.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_duplicateobject.cs' lang='cs'/>
    /// <code source='examples\py\ex_duplicateobject.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    public GeometryBase DuplicateGeometry()
    {
      if (null != m_edited_geometry)
        return m_edited_geometry.Duplicate();

      var g = Geometry;
      if (null != g)
        return g.Duplicate();

      return null;
    }

    /// <summary>
    /// Moves changes made to this RhinoObject into the RhinoDoc.
    /// </summary>
    /// <returns>
    /// true if changes were made.
    /// </returns>
    /// <example>
    /// <code source='examples\vbnet\ex_addlayout.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_addlayout.cs' lang='cs'/>
    /// <code source='examples\py\ex_addlayout.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    public bool CommitChanges()
    {
      var rc = false;

      // object not in document, i.e. ChangeQueue.Mesh.Object
      if (Document == null) return rc;

      if (null != m_edited_geometry)
      {
        var func = GetCommitFunc();
        if (null == func)
          return false;
        var p_const_geometry = m_edited_geometry.ConstPointer();
        var serial_number = func(m_rhinoobject_serial_number, p_const_geometry);
        if (serial_number > 0)
        {
          rc = true;
          m_rhinoobject_serial_number = serial_number;
          m_edited_geometry = null;
        }
        else
          return false;
      }

      if (null != m_edited_material)
      {
        if (m_edited_material_index == -1)
        {
          m_edited_material_index = Document.Materials.Add(m_edited_material);
          if (null != m_edited_attributes)
          {
            m_edited_attributes.MaterialIndex = m_edited_material_index;
          }
          rc = (-1 != m_edited_material_index);
        }
        else
        {
          rc = Document.Materials.Modify(m_edited_material, m_edited_material_index, false);
        }
        if (rc)
        {
          m_edited_material = null;
          m_edited_material_index = -1;
        }
      }

      if (null != m_edited_attributes)
      {
        rc = Document.Objects.ModifyAttributes(this, m_edited_attributes, false);
        if (rc)
          m_edited_attributes = null;
      }

      return rc;
    }

    internal virtual CommitGeometryChangesFunc GetCommitFunc()
    {
      return null;
    }

    /// <summary>
    /// Computes an estimate of the number of bytes that this object is using in memory.
    /// Note that this is a runtime memory estimate and does not directly compare to the
    /// amount of space take up by the object when saved to a file.
    /// </summary>
    /// <returns>The estimated number of bytes this object occupies in memory.</returns>
    /// <since>5.0</since>
    [CLSCompliant(false)]
    public uint MemoryEstimate()
    {
      var p_const_this = ConstPointer();
      return UnsafeNativeMethods.ON_Object_SizeOf(p_const_this);
    }

    int GetInt(UnsafeNativeMethods.RhinoObjectGetInt which)
    {
      var ptr = ConstPointer();
      return UnsafeNativeMethods.CRhinoObject_GetInt(ptr, which);
    }


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
    /// <para>This value is the same as the one returned by this.Attributes.ObjectId.</para>
    /// </summary>
    /// <since>5.0</since>
    public override Guid Id
    {
      get
      {
        return Attributes.ObjectId;
      }
      set
      {
        Attributes.ObjectId = value;
      }
    }

    /// <summary>
    /// Rhino objects have optional text names.  More than one object in
    /// a model can have the same name and some objects may have no name.
    /// </summary>
    /// <since>5.0</since>
    public override string Name
    {
      get
      {
        return Attributes.Name;
      }
      set
      {
        Attributes.Name = value;
      }
    }

    /// <summary>Number of groups object belongs to.</summary>
    /// <since>5.0</since>
    public int GroupCount
    {
      get
      {
        return Attributes.GroupCount;
      }
    }

    /// <summary>
    /// Allocates an array of group indices of length GroupCount.
    /// If <see cref="GroupCount"/> is 0, then this method returns null.
    /// </summary>
    /// <returns>An array of group indices, or null if <see cref="GroupCount"/> is 0.</returns>
    /// <since>5.0</since>
    public int[] GetGroupList()
    {
      var count = GroupCount;
      if (count < 1)
        return null;
      var rc = new int[count];
      var ptr = ConstPointer();
      
      // [Giulio - 2018-01-05] Do not attempt to marshal the first item of the array by ref. This creates an unsafe situation.
      // https://stackoverflow.com/questions/4147423/why-does-incorrectly-using-ref-myarray0-to-pass-in-an-array-work-but-only-i/4147467
      UnsafeNativeMethods.CRhinoObject_GetGroupList(ptr, rc);
      return rc;
    }

    /// <summary>Check selection state.</summary>
    /// <param name="checkSubObjects">
    /// (false is good default)
    /// If true and the entire object is not selected, and some subset of the object
    /// is selected, like some edges of a surface, then 3 is returned.
    /// If false and the entire object is not selected, then zero is returned.
    /// </param>
    /// <returns>
    /// 0 = object is not selected.
    /// 1 = object is selected.
    /// 2 = entire object is selected persistently.
    /// 3 = one or more proper sub-objects are selected.
    /// </returns>
    /// <since>5.0</since>
    public int IsSelected(bool checkSubObjects)
    {
      var ptr = ConstPointer();
      return UnsafeNativeMethods.CRhinoObject_IsSelected(ptr, checkSubObjects);
    }

    /// <summary>Check sub-object selection state.</summary>
    /// <param name="componentIndex">Index of sub-object to check.</param>
    /// <returns>true if the sub-object is selected.</returns>
    /// <remarks>A sub-object cannot be persistently selected.</remarks>
    /// <since>5.0</since>
    public bool IsSubObjectSelected(ComponentIndex componentIndex)
    {
      var ptr = ConstPointer();
      return UnsafeNativeMethods.CRhinoObject_IsSubObjectSelected(ptr, componentIndex);
    }

    /// <summary>Get a list of all selected sub-objects.</summary>
    /// <returns>An array of sub-object indices, or null if there are none.</returns>
    /// <since>5.0</since>
    public ComponentIndex[] GetSelectedSubObjects()
    {
      var ptr = ConstPointer();
      var arr = new INTERNAL_ComponentIndexArray();
      var p_array = arr.NonConstPointer();
      var count = UnsafeNativeMethods.CRhinoObject_GetSelectedSubObjects(ptr, p_array, true);
      ComponentIndex[] rc = null;
      if (count > 0)
      {
        rc = arr.ToArray();
      }
      arr.Dispose();
      return rc;
    }

    /// <summary>Reports if an object can be selected.</summary>
    /// <param name="ignoreSelectionState">
    /// If true, then selected objects are selectable.
    /// If false, then selected objects are not selectable.
    /// </param>
    /// <param name="ignoreGripsState">
    /// If true, then objects with grips on can be selected.
    /// If false, then the value returned by the object's IsSelectableWithGripsOn() function decides if the object can be selected.
    /// </param>
    /// <param name="ignoreLayerLocking">
    /// If true, then objects on locked layers are selectable.
    /// If false, then objects on locked layers are not selectable.
    /// </param>
    /// <param name="ignoreLayerVisibility">
    /// If true, then objects on hidden layers are selectable.
    /// If false, then objects on hidden layers are not selectable.
    /// </param>
    /// <returns>true if object is capable of being selected.</returns>
    /// <remarks>
    /// Objects that are locked, hidden, or on locked or hidden layers
    /// cannot be selected. If IsSelectableWithGripsOn() returns false,
    /// then an that object is not selectable if it has grips turned on.
    /// </remarks>
    /// <since>5.0</since>
    public bool IsSelectable(bool ignoreSelectionState, bool ignoreGripsState, bool ignoreLayerLocking, bool ignoreLayerVisibility)
    {
      var ptr = ConstPointer();
      return UnsafeNativeMethods.CRhinoObject_IsSelectable(ptr, ignoreSelectionState, ignoreGripsState, ignoreLayerLocking, ignoreLayerVisibility);
    }
    /// <summary>Reports if an object can be selected.</summary>
    /// <returns>true if object is capable of being selected.</returns>
    /// <remarks>
    /// Objects that are locked, hidden, or on locked or hidden layers
    /// cannot be selected. If IsSelectableWithGripsOn() returns false,
    /// then an that object is not selectable if it has grips turned on.
    /// </remarks>
    /// <since>5.0</since>
    public bool IsSelectable()
    {
      return IsSelectable(false, false, false, false);
    }

    /// <summary>Reports if a sub-object can be selected.</summary>
    /// <param name="componentIndex">index of sub-object to check.</param>
    /// <param name="ignoreSelectionState">
    /// If true, then selected objects are selectable.
    /// If false, then selected objects are not selectable.
    /// </param>
    /// <returns>true if the specified sub-object can be selected.</returns>
    /// <remarks>
    /// Objects that are locked, hidden, or on locked or hidden layers
    /// cannot be selected. If IsSelectableWithGripsOn() returns false,
    /// then that object is not selectable if it has grips turned on.
    /// </remarks>
    /// <since>5.0</since>
    public bool IsSubObjectSelectable(ComponentIndex componentIndex, bool ignoreSelectionState)
    {
      var ptr = ConstPointer();
      return UnsafeNativeMethods.CRhinoObject_IsSubObjectSelectable(ptr, componentIndex, ignoreSelectionState);
    }

    /// <summary>Selects an object.</summary>
    /// <param name="on">The new selection state; true activates selection.</param>
    /// <param name="syncHighlight">
    /// If true, then the object is highlighted if it is selected 
    /// and unhighlighted if is not selected.
    /// <para>Highlighting can be and stay out of sync, as its specification is independent.</para>
    /// </param>
    /// <param name="persistentSelect">
    /// Objects that are persistently selected stay selected when a command terminates.
    /// </param>
    /// <param name="ignoreGripsState">
    /// If true, then objects with grips on can be selected.
    /// If false, then the value returned by the object's IsSelectableWithGripsOn() function
    /// decides if the object can be selected when it has grips turned on.
    /// </param>
    /// <param name="ignoreLayerLocking">
    /// If true, then objects on locked layers can be selected.
    /// If false, then objects on locked layers cannot be selected.
    /// </param>
    /// <param name="ignoreLayerVisibility">
    /// If true, then objects on hidden layers can be selectable.
    /// If false, then objects on hidden layers cannot be selected.
    /// </param>
    /// <returns>
    /// <para>0: object is not selected.</para>
    /// <para>1: object is selected.</para>
    /// <para>2: object is selected persistently.</para>
    /// </returns>
    /// <remarks>
    /// Objects that are locked, hidden, or on locked or hidden layers
    /// cannot be selected. If IsSelectableWithGripsOn() returns false,
    /// then an that object is not selectable if it has grips turned on.
    /// </remarks>
    /// <since>5.0</since>
    public int Select(bool on, bool syncHighlight, bool persistentSelect, bool ignoreGripsState, bool ignoreLayerLocking, bool ignoreLayerVisibility)
    {
      var ptr = ConstPointer();
      return UnsafeNativeMethods.CRhinoObject_Select(ptr, on, syncHighlight, persistentSelect, ignoreGripsState, ignoreLayerLocking, ignoreLayerVisibility);
    }

    /// <summary>Selects an object.</summary>
    /// <param name="on">The new selection state; true activates selection.</param>
    /// <returns>
    /// <para>0: object is not selected.</para>
    /// <para>1: object is selected.</para>
    /// <para>2: object is selected persistently.</para>
    /// </returns>
    /// <remarks>
    /// Objects that are locked, hidden, or on locked or hidden layers
    /// cannot be selected. If IsSelectableWithGripsOn() returns false,
    /// then an that object is not selectable if it has grips turned on.
    /// </remarks>
    /// <example>
    /// <code source='examples\vbnet\ex_orientonsrf.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_orientonsrf.cs' lang='cs'/>
    /// <code source='examples\py\ex_orientonsrf.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    public int Select(bool on)
    {
      return Select(on, true);
    }

    /// <summary>Selects an object.</summary>
    /// <param name="on">The new selection state; true activates selection.</param>
    /// <param name="syncHighlight">
    /// If true, then the object is highlighted if it is selected
    /// and not highlighted if is not selected.
    /// <para>Highlighting can be and stay out of sync, as its specification is independent.</para>
    /// </param>
    /// <returns>
    /// <para>0: object is not selected.</para>
    /// <para>1: object is selected.</para>
    /// <para>2: object is selected persistently.</para>
    /// </returns>
    /// <remarks>
    /// Objects that are locked, hidden, or on locked or hidden layers
    /// cannot be selected. If IsSelectableWithGripsOn() returns false,
    /// then an that object is not selectable if it has grips turned on.
    /// </remarks>
    /// <since>5.0</since>
    public int Select(bool on, bool syncHighlight)
    {
      return Select(on, syncHighlight, true, false, false, false);
    }

    /// <summary>Reports if an object can be selected.</summary>
    /// <param name="componentIndex">Index of sub-object to check.</param>
    /// <param name="select">The new selection state; true activates selection.</param>
    /// <param name="syncHighlight">
    /// (default=true)
    /// If true, then the object is highlighted if it is selected 
    /// and unhighlighted if is not selected.
    /// </param>
    /// <returns>
    /// 0: object is not selected
    /// 1: object is selected
    /// 2: object is selected persistently.
    /// </returns>
    /// <remarks>
    /// Objects that are locked, hidden, or on locked or hidden layers
    /// cannot be selected. If IsSelectableWithGripsOn() returns false,
    /// then an that object is not selectable if it has grips turned on.
    /// </remarks>
    /// <since>5.0</since>
    public int SelectSubObject(ComponentIndex componentIndex, bool select, bool syncHighlight)
    {
      return SelectSubObject(componentIndex, select, syncHighlight, false);
    }

    /// <summary>Reports if an object can be selected.</summary>
    /// <param name="componentIndex">Index of sub-object to check.</param>
    /// <param name="select">The new selection state; true activates selection.</param>
    /// <param name="syncHighlight">
    /// (default=true)
    /// If true, then the object is highlighted if it is selected 
    /// and unhighlighted if is not selected.
    /// </param>
    /// <param name="persistentSelect">When true, selection persists even after the current command
    /// terminates.</param>
    /// <returns>
    /// <para>0: object is not selected</para>
    /// <para>1: object is selected</para>
    /// <para>2: object is selected persistently.</para>
    /// </returns>
    /// <remarks>
    /// Objects that are locked, hidden, or on locked or hidden layers
    /// cannot be selected. If IsSelectableWithGripsOn() returns false,
    /// then an that object is not selectable if it has grips turned on.
    /// </remarks>
    /// <since>6.0</since>
    public int SelectSubObject(ComponentIndex componentIndex, bool select, bool syncHighlight, bool persistentSelect)
    {
      var ptr = ConstPointer();
      return UnsafeNativeMethods.CRhinoObject_SelectSubObject(ptr, componentIndex, select, syncHighlight, persistentSelect);
    }

    /// <summary>
    /// Removes selection from all sub-objects.
    /// </summary>
    /// <returns>The number of unselected sub-objects.</returns>
    /// <since>5.0</since>
    public int UnselectAllSubObjects()
    {
      // 20 Jan 2010 - S. Baer
      // The Rhino SDK function CRhinoObject::UnselectAllSubObjects is not const, but we shouldn't have to
      // be copying objects around in order to unselect subobjects (especially when SelectSubObject is
      // considered a const operation.)
      //
      // I dug through the Rhino core source code and passing a const_cast pointer appears to be okay in
      // this situation
      return GetInt(UnsafeNativeMethods.RhinoObjectGetInt.UnselectAllSubObjects);
    }

    /// <summary>Check highlight state.</summary>
    /// <param name="checkSubObjects">
    /// If true and the entire object is not highlighted, and some subset of the object
    /// is highlighted, like some edges of a surface, then 3 is returned.
    /// If false and the entire object is not highlighted, then zero is returned.
    /// </param>
    /// <returns>
    /// <para>0: object is not highlighted.</para>
    /// <para>1: entire object is highlighted.</para>
    /// <para>3: one or more proper sub-objects are highlighted.</para>
    /// </returns>
    /// <since>5.0</since>
    public int IsHighlighted(bool checkSubObjects)
    {
      var ptr = ConstPointer();
      return UnsafeNativeMethods.CRhinoObject_IsHighlighted(ptr, checkSubObjects);
    }

    /// <summary>
    /// Modifies the highlighting of the object.
    /// </summary>
    /// <param name="enable">true if highlighting should be enabled.</param>
    /// <returns>true if the object is now highlighted.</returns>
    /// <since>5.0</since>
    public bool Highlight(bool enable)
    {
      var ptr = ConstPointer();
      return UnsafeNativeMethods.CRhinoObject_Highlight(ptr, enable);
    }

    /// <summary>
    /// Determines if a sub-object is highlighted.
    /// </summary>
    /// <param name="componentIndex">A sub-object component index.</param>
    /// <returns>true if the sub-object is highlighted.</returns>
    /// <since>5.0</since>
    public bool IsSubObjectHighlighted(ComponentIndex componentIndex)
    {
      var ptr = ConstPointer();
      return UnsafeNativeMethods.CRhinoObject_IsSubObjectHighlighted(ptr, componentIndex);
    }

    /// <summary>
    /// Gets a list of all highlighted sub-objects.
    /// </summary>
    /// <returns>An array of all highlighted sub-objects; or null is there are none.</returns>
    /// <since>5.0</since>
    public ComponentIndex[] GetHighlightedSubObjects()
    {
      var ptr = ConstPointer();
      var arr = new INTERNAL_ComponentIndexArray();
      var p_array = arr.NonConstPointer();
      var count = UnsafeNativeMethods.CRhinoObject_GetSelectedSubObjects(ptr, p_array, false);
      ComponentIndex[] rc = null;
      if (count > 0)
      {
        rc = arr.ToArray();
      }
      arr.Dispose();
      return rc;
    }

    /// <summary>
    /// Highlights a sub-object.
    /// </summary>
    /// <param name="componentIndex">A sub-object component index.</param>
    /// <param name="highlight">true if the sub-object should be highlighted.</param>
    /// <returns>true if the sub-object is now highlighted.</returns>
    /// <since>5.0</since>
    public bool HighlightSubObject(ComponentIndex componentIndex, bool highlight)
    {
      var ptr = ConstPointer();
      return UnsafeNativeMethods.CRhinoObject_HighlightSubObject(ptr, componentIndex, highlight);
    }

    /// <summary>
    /// Removes highlighting from all sub-objects.
    /// </summary>
    /// <returns>The number of changed sub-objects.</returns>
    /// <since>5.0</since>
    public int UnhighlightAllSubObjects()
    {
      // 20 Jan 2010 - S. Baer
      // See my comments in UnselectAllSubObjects. The same goes for UnhighlightAllSubObjects
      return GetInt(UnsafeNativeMethods.RhinoObjectGetInt.UnhighightAllSubObjects);
    }

    /// <summary>Gets or sets the activation state of object default editing grips.</summary>
    /// <since>5.0</since>
    public bool GripsOn
    {
      get
      {
        var ptr = ConstPointer();
        var rc = UnsafeNativeMethods.CRhinoObject_GripsOn(ptr);
        return rc != 0;
      }
      set
      {
        // 20 Jan 2010 - S. Baer
        // Made enabling grips a const operation. The PointsOn command performs
        // a const_cast on CRhinoObject when turning grips on
        var ptr = ConstPointer();
        UnsafeNativeMethods.CRhinoObject_EnableGrips(ptr, value);
      }
    }

    /// <summary>
    /// true if grips are turned on and at least one is selected.
    /// </summary>
    /// <since>5.0</since>
    public bool GripsSelected
    {
      get
      {
        return GetBool(UnsafeNativeMethods.RhinoObjectGetBool.GripsSelected);
      }
    }

    /// <summary>Turns on/off the object's editing grips.</summary>
    /// <param name="customGrips">The custom object grips.</param>
    /// <returns>
    /// true if the call succeeded.  If you attempt to add custom grips to an
    /// object that does not support custom grips, then false is returned.
    /// </returns>
    /// <since>5.0</since>
    public bool EnableCustomGrips(Custom.CustomObjectGrips customGrips)
    {
      var p_const_this = ConstPointer();
      var p_grips = customGrips==null?IntPtr.Zero:customGrips.NonConstPointer();
      var rc = UnsafeNativeMethods.CRhinoObject_EnableCustomGrips(p_const_this, p_grips);
      if (rc && customGrips != null)
      {
        customGrips.OnAttachedToRhinoObject(this);
      }
      return rc;
    }

    /// <summary>
    /// Returns grips for this object If grips are enabled. If grips are not
    /// enabled, returns null.
    /// </summary>
    /// <returns>An array of grip objects; or null if there are no grips.</returns>
    /// <since>5.0</since>
    public GripObject[] GetGrips()
    {
      var p_this = ConstPointer();
      var p_grip_list = UnsafeNativeMethods.ON_GripList_New();
      var count = UnsafeNativeMethods.CRhinoObject_GetGrips(p_this, p_grip_list);
      GripObject[] rc = null;
      if (count > 0)
      {
        var grips = new System.Collections.Generic.List<GripObject>();
        for (var i = 0; i < count; i++)
        {
          var p_grip = UnsafeNativeMethods.ON_GripList_Get(p_grip_list, i);
          var sn = UnsafeNativeMethods.CRhinoObject_RuntimeSN(p_grip);
          if (IntPtr.Zero != p_grip && sn > 0)
          {
            var g = new GripObject(sn);
            grips.Add(g);
          }
        }
        if (grips.Count > 0)
          rc = grips.ToArray();
      }
      UnsafeNativeMethods.ON_GripList_Delete(p_grip_list);
      return rc;
    }

    /// <summary>
    /// Used to turn analysis modes on and off.
    /// </summary>
    /// <param name="mode">A visual analysis mode.</param>
    /// <param name="enable">true if the mode should be activated; false otherwise.</param>
    /// <returns>true if this object supports the analysis mode.</returns>
    /// <since>5.0</since>
    public bool EnableVisualAnalysisMode(Display.VisualAnalysisMode mode, bool enable)
    {
      var p_const_this = ConstPointer();
      var id = mode.Id;
      return UnsafeNativeMethods.CRhinoObject_EnableVisualAnalysisMode(p_const_this, id, enable);
    }

    /// <summary>
    /// Reports if any visual analysis mode is currently active for an object.
    /// </summary>
    /// <returns>true if an analysis mode is active; otherwise false.</returns>
    /// <since>5.0</since>
    public bool InVisualAnalysisMode()
    {
      return InVisualAnalysisMode(null);
    }

    /// <summary>
    /// Reports if a visual analysis mode is currently active for an object.
    /// </summary>
    /// <param name="mode">
    /// The mode to check for.
    /// <para>Use null if you want to see if any mode is active.</para>
    /// </param>
    /// <returns>true if the specified analysis mode is active; otherwise false.</returns>
    /// <since>5.0</since>
    public bool InVisualAnalysisMode(Display.VisualAnalysisMode mode)
    {
      var p_const_this = ConstPointer();
      var id = Guid.Empty;
      if (mode != null)
        id = mode.Id;
      return UnsafeNativeMethods.CRhinoObject_InVisualAnalysisMode(p_const_this, id);
    }

    /// <summary>
    /// Gets a list of currently enabled analysis modes for this object.
    /// </summary>
    /// <returns>An array of visual analysis modes. The array can be empty, but not null.</returns>
    /// <since>5.0</since>
    public Display.VisualAnalysisMode[] GetActiveVisualAnalysisModes()
    {
      var p_const_this = ConstPointer();
      var count = UnsafeNativeMethods.CRhinoObject_AnalysisModeList_Count(p_const_this);
      var rc = new Display.VisualAnalysisMode[count];
      for (var i = 0; i < count; i++)
      {
        var id = UnsafeNativeMethods.CRhinoObject_AnalysisModeListId(p_const_this, i);
        rc[i] = Display.VisualAnalysisMode.Find(id);
      }
      return rc;
    }

    /// <summary>
    /// Gets a localized short descriptive name of the object.
    /// </summary>
    /// <param name="plural">true if the descriptive name should in plural.</param>
    /// <returns>A string with the short localized descriptive name.</returns>
    /// <since>5.0</since>
    public virtual string ShortDescription(bool plural)
    {
      using (var sh = new StringHolder())
      {
        var p_const_this = ConstPointer();
        var p_string = sh.NonConstPointer();
        UnsafeNativeMethods.CRhinoObject_ShortDescription(p_const_this, p_string, plural);
        return sh.ToString();
      }
    }

    /// <summary>
    /// Get a brief description of a object, including it's attributes and geometry.
    /// </summary>
    /// <param name="textLog">A text log for collecting the description.</param>
    /// <since>6.0</since>
    public void Description(TextLog textLog)
    {
      if (null == textLog)
        throw new ArgumentNullException(nameof(textLog));
      IntPtr ptr_textlog = textLog.NonConstPointer();
      IntPtr ptr_const_this = ConstPointer();
      UnsafeNativeMethods.RHC_RhinoDescribeObject(ptr_const_this, ptr_textlog);
    }

    /// <summary>
    /// Returns true if the object is a picture frame.
    /// A picture frame object is an object that displays a texture map in all views.
    /// </summary>
    /// <since>7.9</since>
    public bool IsPictureFrame
    {
      get
      {
        var ptr_const_this = ConstPointer();
        return UnsafeNativeMethods.CRhinoObject_IsPictureFrame(ptr_const_this);
      }
    }

    /// <summary>
    /// Returns true if the object is capable of having a mesh of the specified type
    /// </summary>
    /// <param name="meshType"></param>
    /// <returns></returns>
    /// <since>5.0</since>
    public virtual bool IsMeshable(MeshType meshType)
    {
      var p_const_this = ConstPointer();
      return UnsafeNativeMethods.CRhinoObject_IsMeshable(p_const_this, (int)meshType);
    }

    /// <summary>
    /// Sets the per-object meshing parameters for this object. 
    /// When set, this object will use these meshing parameters when generating a render mesh, 
    /// instead of those provided by the document.
    /// </summary>
    /// <param name="mp">
    /// The per-object meshing parameters. 
    /// Note: if null, then the per-object meshing parameters will be removed, and this object will
    /// revert to using the meshing parameters provided by the document.
    /// </param>
    /// <returns>True if successful, false otherwise.</returns>
    /// <since>7.0</since>
    public bool SetRenderMeshParameters(MeshingParameters mp)
    {
      IntPtr ptr_const_this = ConstPointer();
      IntPtr ptr_const_mesh_parameters = (null != mp) ? mp.ConstPointer() : IntPtr.Zero;
      return UnsafeNativeMethods.RHC_RhinoObjectSetMeshParameters(ptr_const_this, ptr_const_mesh_parameters);
    }

    /// <summary>
    /// Returns the meshing parameters that this object uses for generating render meshes.
    /// If this object does not have per-object meshing parameters, then the document's meshing parameters are returned.
    /// </summary>
    /// <returns>The render meshing parameters.</returns>
    /// <since>5.0</since>
    public MeshingParameters GetRenderMeshParameters()
    {
      return GetRenderMeshParameters(true);
    }

    /// <summary>
    /// Returns the meshing parameters that this object uses for generating render meshes.
    /// </summary>
    /// <param name="returnDocumentParametersIfUnset">
    /// If true, then return the per-object meshing parameters for this object.
    /// If this object does not have per-object meshing parameters, then the document's meshing parameters are returned.
    /// If false, then return the per-object meshing parameters for this object.
    /// If this object does not have per-object meshing parameters, then null is returned.
    /// </param>
    /// <returns>The render meshing parameters if successful, null otherwise.</returns>
    /// <since>7.0</since>
    public MeshingParameters GetRenderMeshParameters(bool returnDocumentParametersIfUnset)
    {
      if (returnDocumentParametersIfUnset)
      {
        var rc = new MeshingParameters();
        var p_meshing_parameters = rc.NonConstPointer();
        var p_const_this = ConstPointer();
        UnsafeNativeMethods.CRhinoObject_GetRenderMeshParameters(p_const_this, p_meshing_parameters);
        return rc;
      }
      IntPtr ptr_const_this = ConstPointer();
      IntPtr ptr_mesh_parameters = UnsafeNativeMethods.RHC_RhinoObjectGetMeshParameters(ptr_const_this);
      return (IntPtr.Zero != ptr_mesh_parameters) ? new MeshingParameters(ptr_mesh_parameters) : null;
    }

    /// <summary>
    /// RhinoObjects can have several different types of meshes and 
    /// different numbers of meshes.  A b-rep can have a render and 
    /// an analysis mesh on each face.  A mesh object has a single 
    /// render mesh and no analysis mesh. Curve, point, and annotation
    /// objects have no meshes.
    /// </summary>
    /// <param name="meshType">type of mesh to count</param>
    /// <param name="parameters">
    /// if not null and if the object can change its mesh (like a brep),
    /// then only meshes that were created with these mesh parameters are counted.
    /// </param>
    /// <returns>number of meshes</returns>
    /// <since>5.0</since>
    public virtual int MeshCount(MeshType meshType, MeshingParameters parameters)
    {
      var p_const_this = ConstPointer();
      var p_mesh_parameters = IntPtr.Zero;
      if (parameters != null)
        p_mesh_parameters = parameters.ConstPointer();
      return UnsafeNativeMethods.CRhinoObject_MeshCount(p_const_this, (int)meshType, p_mesh_parameters);
    }

    /// <summary>
    /// Create meshes used to render and analyze surface and polysurface objects.
    /// </summary>
    /// <param name="meshType">type of meshes to create</param>
    /// <param name="parameters">
    /// in parameters that control the quality of the meshes that are created
    /// </param>
    /// <param name="ignoreCustomParameters">
    /// Default should be false. Should the object ignore any custom meshing
    /// parameters on the object's attributes
    /// </param>
    /// <returns>number of meshes created</returns>
    /// <since>5.0</since>
    public virtual int CreateMeshes(MeshType meshType, MeshingParameters parameters, bool ignoreCustomParameters)
    {
      var p_this = NonConstPointer_I_KnowWhatImDoing();
      var p_const_mesh_parameters = parameters.ConstPointer();
      return UnsafeNativeMethods.CRhinoObject_CreateMeshes(p_this, (int)meshType, p_const_mesh_parameters, ignoreCustomParameters);
    }

    /// <summary>
    /// Get existing meshes used to render and analyze surface and polysurface objects.
    /// </summary>
    /// <param name="meshType"></param>
    /// <returns>An array of meshes.</returns>
    /// <since>5.0</since>
    public virtual Mesh[] GetMeshes(MeshType meshType)
    {
      using (var mesh_array = new SimpleArrayMeshPointer())
      {
        var p_mesh_array = mesh_array.NonConstPointer();
        var p_const_this = ConstPointer();
        UnsafeNativeMethods.CRhinoObject_GetMeshes(p_const_this, p_mesh_array, (int)meshType);
        return mesh_array.ToConstArray(this);
      }
    }

    /// <summary>
    /// If a Rhino object has been manipulated by Rhino's gumball, and the gumball is not in its default position,
    /// then the object's repositioned gumball frame is returned.
    /// </summary>
    /// <param name="frame">The gumball frame.</param>
    /// <returns>true if the object has a gumball frame, otherwise false.</returns>
    /// <since>7.0</since>
    public bool TryGetGumballFrame(out GumballFrame frame)
    {
      frame = new GumballFrame();
      var ptr_const_this = ConstPointer();
      var plane = new Plane();
      var distance = new Vector3d();
      var mode = 0;
      var rc = UnsafeNativeMethods.RHC_RhTryGetRhinoObjectGumballFrame(ptr_const_this, ref plane, ref distance, ref mode);
      if (rc)
        frame = new GumballFrame(plane, distance, (GumballScaleMode)mode);
      return rc;
    }

    /// <summary>
    /// Determines if custom render meshes will be built for a particular object.
    /// </summary>
    /// <param name="viewport">The viewport being rendered.</param>
    /// <param name="preview">
    /// Type of mesh to build. If attributes is non-null then a smaller mesh may be
    /// generated in less time, false is meant when actually rendering.
    /// </param>
    /// <returns>
    /// Returns true if custom render mesh(es) will get built for this object.
    /// </returns>
    /// <since>5.7</since>
    /// <deprecated>6.0</deprecated>
    [Obsolete]
    public bool SupportsRenderPrimitiveList(ViewportInfo viewport, bool preview)
    {
      // Andy, we are just passing Guid.Empty for the plug-in Id for now until there is an actual
      // need for it, if it ever comes up we can add an overloaded version that includes the Guid.
      // Per Andy:
      // The plug-in Id is optionally used by the custom mesh provider to determine if the plug-in
      // is allowed access to the custom meshes.  Currently none of our custom mesh providers
      // pay attention to the Id.
      var da = new Rhino.Display.DisplayPipelineAttributes(IntPtr.Zero);

      return UnsafeNativeMethods.Rdk_CRMManager_WillBuildCustomMesh(viewport.ConstPointer(), ConstPointer(), Document.RuntimeSerialNumber, Guid.Empty, preview ? da.ConstPointer() : IntPtr.Zero);
    }

    /// <summary>
    /// Determines if custom render meshes will be built for a particular object.
    /// </summary>
    /// <param name="viewport">The viewport being rendered.</param>
    /// <param name="attrs">
    /// Type of mesh to build. If attributes is non-null then a smaller mesh may be
    /// generated in less time, false is meant when actually rendering.
    /// </param>
    /// <returns>
    /// Returns true if custom render mesh(es) will get built for this object.
    /// </returns>
    /// <since>6.0</since>
    public bool SupportsRenderPrimitiveList(ViewportInfo viewport, Rhino.Display.DisplayPipelineAttributes attrs)
    {
      // Andy, we are just passing Guid.Empty for the plug-in Id for now until there is an actual
      // need for it, if it ever comes up we can add an overloaded version that includes the Guid.
      // Per Andy:
      // The plug-in Id is optionally used by the custom mesh provider to determine if the plug-in
      // is allowed access to the custom meshes.  Currently none of our custom mesh providers
      // pay attention to the Id.
      return UnsafeNativeMethods.Rdk_CRMManager_WillBuildCustomMesh(viewport.ConstPointer(), ConstPointer(), Document.RuntimeSerialNumber, Guid.Empty, attrs == null ? IntPtr.Zero : attrs.ConstPointer());
    }

    /// <summary>
    /// Build custom render mesh(es) for this object.
    /// </summary>
    /// <param name="viewport">The viewport being rendered.</param>
    /// <param name="preview">
    /// Type of mesh to build, if preview is true then a smaller mesh may be
    /// generated in less time, false is meant when actually rendering.
    /// </param>
    /// <returns>
    /// Returns a RenderPrimitiveList if successful otherwise returns null.
    /// </returns>
    /// <since>5.7</since>
    /// <deprecated>6.0</deprecated>
    [Obsolete]
    public RenderPrimitiveList GetRenderPrimitiveList(ViewportInfo viewport, bool preview)
    {
      // Andy, we are just passing Guid.Empty for the plug-in Id for now until there is an actual
      // need for it, if it ever comes up we can add an overloaded version that includes the Guid.
      // Per Andy:
      // The plug-in Id is optionally used by the custom mesh provider to determine if the plug-in
      // is allowed access to the custom meshes.  Currently none of our custom mesh providers
      // pay attention to the Id.
      var primitives = new RenderPrimitiveList(this);
      var da = new Rhino.Display.DisplayPipelineAttributes(IntPtr.Zero);

      var success = UnsafeNativeMethods.Rdk_CRMManager_BuildCustomMeshes(viewport.ConstPointer(), Document.RuntimeSerialNumber, primitives.NonConstPointer(), Guid.Empty, preview ? da.ConstPointer() : IntPtr.Zero);
      if (success)
        return primitives;
      primitives.Dispose();
      return null;
    }


    /// <summary>
    /// Build custom render mesh(es) for this object.
    /// </summary>
    /// <param name="viewport">The viewport being rendered.</param>
    /// <param name="attrs">
    /// Attributes for the view mode you are supplying meshes for.  Will be null if this is a modal rendering.
    /// </param>
    /// <returns>
    /// Returns a RenderPrimitiveList if successful otherwise returns null.
    /// </returns>
    /// <since>6.0</since>
    public RenderPrimitiveList GetRenderPrimitiveList(ViewportInfo viewport, Rhino.Display.DisplayPipelineAttributes attrs)
    {
      // Andy, we are just passing Guid.Empty for the plug-in Id for now until there is an actual
      // need for it, if it ever comes up we can add an overloaded version that includes the Guid.
      // Per Andy:
      // The plug-in Id is optionally used by the custom mesh provider to determine if the plug-in
      // is allowed access to the custom meshes.  Currently none of our custom mesh providers
      // pay attention to the Id.
      var primitives = new RenderPrimitiveList(this);
      var success = UnsafeNativeMethods.Rdk_CRMManager_BuildCustomMeshes(viewport.ConstPointer(), Document.RuntimeSerialNumber, primitives.NonConstPointer(), Guid.Empty, attrs == null ? IntPtr.Zero : attrs.ConstPointer());
      if (success)
        return primitives;
      primitives.Dispose();
      return null;
    }

    /// <summary>
    /// Get the bounding box for the custom render meshes associated with this
    /// object.
    /// </summary>
    /// <param name="viewport">The viewport being rendered.</param>
    /// <param name="preview">
    /// Type of mesh to build, if preview is true then a smaller mesh may be
    /// generated in less time, false is meant when actually rendering.
    /// </param>
    /// <param name="boundingBox">
    /// This will be set to BoundingBox.Unset on failure otherwise it will be
    /// the bounding box for the custom render meshes associated with this
    /// object.
    /// </param>
    /// <returns>
    /// Returns true if the bounding box was successfully calculated otherwise
    /// returns false on error.
    /// </returns>
    /// <since>5.7</since>
    /// <deprecated>6.0</deprecated>
    [Obsolete]
    public bool TryGetRenderPrimitiveBoundingBox(ViewportInfo viewport, bool preview, out BoundingBox boundingBox)
    {
      boundingBox = BoundingBox.Unset;

      var min = new Point3d();
      var max = new Point3d();

      // Andy, we are just passing Guid.Empty for the plug-in Id for now until there is an actual
      // need for it, if it ever comes up we can add an overloaded version that includes the Guid.
      // Per Andy:
      // The plug-in Id is optionally used by the custom mesh provider to determine if the plug-in
      // is allowed access to the custom meshes.  Currently none of our custom mesh providers
      // pay attention to the Id.
      var da = new Rhino.Display.DisplayPipelineAttributes(IntPtr.Zero);

      if (UnsafeNativeMethods.Rdk_CRMManager_BoundingBox(viewport.ConstPointer(), ConstPointer(), Document.RuntimeSerialNumber, Guid.Empty, preview ? da.ConstPointer() : IntPtr.Zero, ref min, ref max))
      {
        boundingBox = new BoundingBox(min, max);
        return boundingBox.IsValid;
      }

      return false;
    }


    /// <summary>
    /// Get the bounding box for the custom render meshes associated with this
    /// object.
    /// </summary>
    /// <param name="viewport">The viewport being rendered.</param>
    /// <param name="attrs">
    /// Attributes for the view mode you are supplying meshes for.  Will be null if this is a modal rendering.
    /// </param>
    /// <param name="boundingBox">
    /// This will be set to BoundingBox.Unset on failure otherwise it will be
    /// the bounding box for the custom render meshes associated with this
    /// object.
    /// </param>
    /// <returns>
    /// Returns true if the bounding box was successfully calculated otherwise
    /// returns false on error.
    /// </returns>
    /// <since>6.0</since>
    public bool TryGetRenderPrimitiveBoundingBox(ViewportInfo viewport, Rhino.Display.DisplayPipelineAttributes attrs, out BoundingBox boundingBox)
    {
      boundingBox = BoundingBox.Unset;

      var min = new Point3d();
      var max = new Point3d();

      // Andy, we are just passing Guid.Empty for the plug-in Id for now until there is an actual
      // need for it, if it ever comes up we can add an overloaded version that includes the Guid.
      // Per Andy:
      // The plug-in Id is optionally used by the custom mesh provider to determine if the plug-in
      // is allowed access to the custom meshes.  Currently none of our custom mesh providers
      // pay attention to the Id.
      if (UnsafeNativeMethods.Rdk_CRMManager_BoundingBox(viewport.ConstPointer(), ConstPointer(), Document.RuntimeSerialNumber, Guid.Empty, attrs == null ? IntPtr.Zero : attrs.ConstPointer(), ref min, ref max))
      {
        boundingBox = new BoundingBox(min, max);
        return boundingBox.IsValid;
      }

      return false;
    }

    /// <summary>
    /// Explodes the object into sub-objects. It is up to the caller to add the returned objects to the document.
    /// </summary>
    /// <returns>An array of Rhino objects, or null if this object cannot be exploded.</returns>
    /// <since>5.0</since>
    public RhinoObject[] GetSubObjects()
    {
      using (var arr = new Runtime.InternalRhinoObjectArray())
      {
        var ptr = ConstPointer();
        var p_array = arr.NonConstPointer();
        UnsafeNativeMethods.CRhinoObject_GetSubObjects(ptr, p_array);
        RhinoObject[] rc = arr.ToNonConstArray();
        return rc;
      }
    }

    /// <summary>
    /// True if the object has a dynamic transformation
    /// </summary>
    /// <since>5.0</since>
    public bool HasDynamicTransform
    {
      get { return GetBool(UnsafeNativeMethods.RhinoObjectGetBool.HasDynamicTransform); }
    }

    /// <summary>
    /// While an object is being dynamically transformed (dragged, rotated, ...),
    /// the current transformation can be retrieved and used for creating
    /// dynamic display.
    /// </summary>
    /// <param name="transform"></param>
    /// <returns>
    /// True if the object is being edited and its transformation
    /// is available.  False if the object is not being edited,
    /// in which case the identity transform is returned.
    /// </returns>
    /// <since>5.0</since>
    public bool GetDynamicTransform(out Transform transform)
    {
      transform = Transform.Identity;
      var p_const_this = ConstPointer();
      return UnsafeNativeMethods.CRhinoObject_GetDynamicTransform(p_const_this, ref transform);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="channel"></param>
    /// <returns></returns>
    /// <since>5.7</since>
    public TextureMapping GetTextureMapping(int channel)
    {
      Transform transform;
      return GetTextureMapping(channel, out transform);
    }
    /// <summary>
    /// Get objects texture mapping
    /// </summary>
    /// <param name="channel"></param>
    /// <param name="objectTransform"></param>
    /// <returns></returns>
    /// <since>5.7</since>
    public TextureMapping GetTextureMapping(int channel, out Transform objectTransform)
    {
      var pointer = ConstPointer();
      objectTransform = Transform.Identity;
      var mapping = UnsafeNativeMethods.ON_TextureMapping_GetMappingFromObject(pointer, channel, ref objectTransform);
      return (IntPtr.Zero == mapping ? null : new TextureMapping(mapping));
    }

     /// <summary>
    /// 
    /// </summary>
    /// <param name="channel"></param>
    /// <param name="tm"></param>
    /// <returns></returns>
    /// <since>6.0</since>
    public int SetTextureMapping(int channel, TextureMapping tm)
    {
      return UnsafeNativeMethods.ON_TextureMapping_SetObjectMapping(ConstPointer(), channel, tm.ConstPointer());
     }
    
    
    /// <summary>
    /// Sets texture mapping and mapping object transform for a channel
    /// </summary>
    /// <param name="channel"></param>
    /// <param name="tm"></param>
    /// <param name="objectTransform">Mapping channel object transform</param>
    /// <returns></returns>
    /// <since>6.26</since>
    public int SetTextureMapping(int channel, TextureMapping tm, Transform objectTransform)
    {
      return UnsafeNativeMethods.ON_TextureMapping_SetObjectMappingAndTransform(ConstPointer(), channel, tm.ConstPointer(), ref objectTransform);
    }

    /// <summary>
    /// Returns true if this object has a texture mapping form any source (pluginId)
    /// </summary>
    /// <returns></returns>
   /// <since>6.0</since>
   public bool HasTextureMapping()
    {
      return UnsafeNativeMethods.ON_TextureMapping_ObjectHasMapping(ConstPointer());
    }



    /// <summary>
    /// Get a list of the texture mapping channel Id's associated with object. 
    /// </summary>
    /// <returns>
    /// Returns an array of channel Id's or an empty list if there are not mappings.
    /// </returns>
    /// <since>5.7</since>
    public int[] GetTextureChannels()
    {
      var pointer = ConstPointer();
      var check = new int[] {};
      var count = UnsafeNativeMethods.ON_TextureMapping_GetObjectTextureChannels(pointer, 0, check);
      if (count < 1)
        return new int[] { };
      var result = new int[count];
      UnsafeNativeMethods.ON_TextureMapping_GetObjectTextureChannels(pointer, count, result);
      return result;
    }

    #if NO_LONGER_USED
    /// <summary>
    /// Gets the instance ID of the render material associated with this object.
    /// Andy says this was probably around before the RenderMaterial property
    /// but it is only used internally now so I made it private.
    /// </summary>
    private Guid RenderMaterialInstanceId
    {
      get
      {
        var p_const_this = ConstPointer();
        return UnsafeNativeMethods.Rdk_RenderContent_ObjectInstanceId(p_const_this);
      }
      // DO NOT DO THIS!  See the comment below which came from NonConstPointer()
      // This was moved to RhinoDoc.Objects.ModifyRenderMaterialInstanceId
      // !!!DO NOT CALL THIS FUNCTION UNLESS YOU ARE WORKING WITH CUSTOM RHINO OBJECTS!!!
      //set
      //{
      //  IntPtr pThis = NonConstPointer();
      //  UnsafeNativeMethods.Rdk_RenderContent_SetObjectMaterialInstanceid(pThis, value);
      //}
    }
    #endif

    /// <summary>
    /// Gets the render material associated with this object or null if there
    /// is none.  This does not pay attention to the material source and will
    /// not check parent objects or layers for a RenderMaterial.
    /// </summary>
    /// <since>5.7</since>
    public RenderMaterial RenderMaterial
    {
      get
      {
        Guid id = Guid.Empty;
        if (m_edited_material != null)
        {
          id = m_edited_material.RenderMaterialInstanceId;
        }
        else
        {
          int index = Attributes.MaterialIndex;

          var material = Document?.Materials.FindIndex(index) ?? null;
          if (material != null)
          {
            id = material.RenderMaterialInstanceId;
          }
        }

        return RenderContent.FromId(Document, id) as RenderMaterial;
      }
      set
      {
        if (Document == null) return;
        if (value == null)
        {
          throw new ArgumentNullException();
        }

        if (value.Document == null)
        {
          throw new ArgumentException("The material is not attached to a document.");
        }

        m_edited_material_index = Attributes.MaterialIndex;
        m_edited_material = value.SimulatedMaterial(RenderTexture.TextureGeneration.Allow);

        m_edited_material.RenderMaterialInstanceId = value.Id;

        //This would be correct to put this here - but the accessor function above does it for now on the C++ side.
        //This also ensures correct setting up of the material.
        //m_edited_material.RenderPlugInId = Guid.Universal;

        Attributes.MaterialSource = ObjectMaterialSource.MaterialFromObject;
      }
    }
    /// <summary>
    /// Gets the RenderMaterial that this object uses based on it's attributes
    /// and the document that the object is associated with. If there is no 
    /// RenderMaterial associated with this object then null is returned.  If
    /// null is returned you should call GetMaterial to get the material used
    /// to render this object.
    /// </summary>
    /// <param name="frontMaterial">
    /// If true, gets the material used to render the object's front side
    /// otherwise; gets the material used to render the back side of the
    /// object.
    /// </param>
    /// <returns>
    /// If there is a RenderMaterial associated with this objects' associated
    /// Material then it is returned otherwise; null is returned.
    /// </returns>
    /// <since>5.10</since>
    public RenderMaterial GetRenderMaterial(bool frontMaterial)
    {
      var const_pointer = ConstPointer();
      var id = UnsafeNativeMethods.Rdk_RenderContent_ObjectMaterialInstanceId(const_pointer, frontMaterial);
      return RenderContent.FromId(Document, id) as RenderMaterial;
    }


    /// <summary>
    /// Gets the RenderMaterial associated with this object if there is one. If
    /// there is no RenderMaterial associated with this object then null is
    /// returned.  If null is returned you should call GetMaterial to get the
    /// material used to render this object.
    /// </summary>
    /// <param name="componentIndex">
    /// Returns the RenderMaterial associated with the specified sub object or
    /// the objects top level material if it is set to
    /// <seealso cref="Rhino.Geometry.ComponentIndex.Unset"/>
    /// </param>
    /// <param name="plugInId">
    /// The plug-in specific material to look for.
    /// </param>
    /// <param name="attributes">
    /// Optional object attributes used to determine the material source, if
    /// null the objects attributes are used.
    /// </param>
    /// <returns>
    /// Returns the <see cref="RenderMaterial"/> associated with the sub object
    /// identified by componentIndex if the component index is set to
    /// <seealso cref="ComponentIndex.Unset"/> then the top level
    /// RenderMaterail is returned.  If this method returns null it means there
    /// is no RenderMaterial associated with the object or  sub object so you
    /// should may GetMaterial get the objects generic material.
    /// </returns>
    /// <since>6.0</since>
    public RenderMaterial GetRenderMaterial(ComponentIndex componentIndex, Guid plugInId, ObjectAttributes attributes)
    {
      var pointer = ConstPointer();
      var att_pointer = attributes == null ? IntPtr.Zero : attributes.ConstPointer();
      var id = UnsafeNativeMethods.Rdk_RenderContent_ObjectRdkMaterial(pointer, componentIndex, plugInId, att_pointer);
      return RenderContent.FromId(Document, id) as RenderMaterial;
    }

    /// <summary>
    /// Gets the RenderMaterial associated with this object if there is one. If
    /// there is no RenderMaterial associated with this object then null is
    /// returned.  If null is returned you should call GetMaterial to get the
    /// material used to render this object.
    /// </summary>
    /// <param name="componentIndex">
    /// Returns the RenderMaterial associated with the specified sub object or
    /// the objects top level material if it is set to
    /// <seealso cref="Rhino.Geometry.ComponentIndex.Unset"/>
    /// </param>
    /// <param name="plugInId">
    /// The plug-in specific material to look for.
    /// </param>
    /// <returns>
    /// Returns the <see cref="RenderMaterial"/> associated with the sub object
    /// identified by componentIndex if the component index is set to
    /// <seealso cref="ComponentIndex.Unset"/> then the top level
    /// RenderMaterail is returned.  If this method returns null it means there
    /// is no RenderMaterial associated with the object or sub object so you
    /// should may GetMaterial get the objects generic material.
    /// </returns>
    /// <since>6.0</since>
    public RenderMaterial GetRenderMaterial(ComponentIndex componentIndex, Guid plugInId)
    {
      var value = GetRenderMaterial(componentIndex, plugInId, null);
      return value;
    }

    /// <summary>
    /// Gets the RenderMaterial associated with this object if there is one. If
    /// there is no RenderMaterial associated with this object then null is
    /// returned.  If null is returned you should call GetMaterial to get the
    /// material used to render this object.
    /// </summary>
    /// <param name="componentIndex">
    /// Returns the RenderMaterial associated with the specified sub object or
    /// the objects top level material if it is set to ComponentIndex.Unset
    /// </param>
    /// <returns>
    /// Returns the <see cref="RenderMaterial"/> associated with the sub object
    /// identified by componentIndex if the component index is set to
    /// <seealso cref="ComponentIndex.Unset"/> then the top level
    /// RenderMaterail is returned.  If this method returns null it means there
    /// is no RenderMaterial associated with the object or  sub object so you
    /// should may GetMaterial get the objects generic material.
    /// </returns>
    /// <since>6.0</since>
    public RenderMaterial GetRenderMaterial(ComponentIndex componentIndex)
    {
      var value = GetRenderMaterial(componentIndex, Utilities.DefaultRenderPlugInId, null);
      return value;
    }

    /// <summary>
    /// Will be true if the object contains sub object meshes with materials
    /// that are different than the top level object.
    /// </summary>
    /// <since>6.0</since>
    public bool HasSubobjectMaterials
    {
      get
      {
        var pointer = ConstPointer();
        var value = UnsafeNativeMethods.CRhinoObject_HasSubobjectMaterials(pointer);
        return value;
      }
    }
    /// <summary>
    /// 
    /// </summary>
    /// <since>6.0</since>
    public ComponentIndex[] SubobjectMaterialComponents
    {
      get
      {
        var pointer = ConstPointer();
        var array = new INTERNAL_ComponentIndexArray();
        var array_pointer = array.NonConstPointer();
        var result = UnsafeNativeMethods.CRhinoObject_GetSubobjectMaterialComponents(pointer, array_pointer);
        if (result < 1)
        {
          array.Dispose();
          return new ComponentIndex[0];
        }
        var value = array.ToArray();
        return value;
      }
    }

    /// <summary>
    /// Returns <see cref="ModelComponentType.ModelGeometry"/>.
    /// </summary>
    /// <since>6.0</since>
    public override ModelComponentType ComponentType
    {
      get
      {
        return ModelComponentType.ModelGeometry;
      }
    }

    /// <summary>
    /// Gets material that this object uses based on it's attributes and the document
    /// that the object is associated with.  In the rare case that a document is not
    /// associated with this object, null will be returned.
    /// </summary>
    /// <param name="frontMaterial">
    /// If true, gets the material used to render the object's front side
    /// </param>
    /// <returns></returns>
    /// <since>5.0</since>
    public Material GetMaterial(bool frontMaterial)
    {
      var const_pointer = ConstPointer();
      var doc = Document;
      if (doc == null)
        return null;
      var index = UnsafeNativeMethods.CRhinoObject_GetMaterial(const_pointer, frontMaterial);
      if( index<-2 ) // -1 and -2 are valid since the material may be the "default" or "locked default" material
        return null;

      if( -1==index )
        return Material.DefaultMaterial;
      return doc.Materials[index];
    }

    /// <summary>
    /// Get the Material associated with the sub object
    /// identified by componentIndex if the component index is
    /// set to ComponentIndex.Unset then the top level material
    /// is returned.
    /// </summary>
    /// <param name="componentIndex">
    /// Returns the material associated with the specified sub object or the
    /// objects top level material if it is set to ComponentIndex.Unset
    /// </param>
    /// <param name="plugInId">
    /// The plug-in specific material to look for.
    /// </param>
    /// <param name="attributes">
    /// Optional object attributes used to determine the material source, if
    /// null the objects attributes are used.
    /// </param>
    /// <returns>
    /// Returns the Material associated with the sub object
    /// identified by componentIndex if the component index is set to
    /// ComponentIndex.Unset then the top level material is returned.
    /// </returns>
    /// <since>6.0</since>
    public Material GetMaterial(ComponentIndex componentIndex, Guid plugInId, ObjectAttributes attributes)
    {
      var doc = Document;
      if (doc == null)
        return Material.DefaultMaterial;
      var pointer = ConstPointer();
      var att_pointer = attributes == null ? IntPtr.Zero : attributes.ConstPointer();
      var index = UnsafeNativeMethods.CRhinoObject_ObjectMaterial(pointer, componentIndex, plugInId, att_pointer);
      return (index < 0 ? Material.DefaultMaterial : doc.Materials[index]);
    }

    /// <summary>
    /// Get the Material associated with the sub object
    /// identified by componentIndex if the component index is
    /// set to ComponentIndex.Unset
    /// then the top level material is returned.
    /// </summary>
    /// <param name="componentIndex">
    /// Returns the material associated with the specified sub object or the
    /// objects top level material if it is set to ComponentIndex.Unset
    /// </param>
    /// <param name="plugInId">
    /// The plug-in specific material to look for.
    /// </param>
    /// <returns>
    /// Returns the Material associated with the sub object
    /// identified by componentIndex if the component index is set to
    /// ComponentIndex.Unset
    /// then the top level material is returned.
    /// </returns>
    /// <since>6.0</since>
    public Material GetMaterial(ComponentIndex componentIndex, Guid plugInId)
    {
      var value = GetMaterial(componentIndex, plugInId, null);
      return value;
    }
  
    /// <summary>
    /// Get the Material associated with the sub object
    /// identified by componentIndex if the component index is
    /// set to ComponentIndex.Unset
    /// then the top level material is returned.
    /// </summary>
    /// <param name="componentIndex">
    /// Returns the material associated with the specified sub object or the
    /// objects top level material if it is set to ComponentIndex.Unset
    /// </param>
    /// <returns>
    /// Returns the Material associated with the sub object
    /// identified by componentIndex if the component index is
    /// set to ComponentIndex.Unset then the top level material
    /// is returned.
    /// </returns>
    /// <since>6.0</since>
    public Material GetMaterial(ComponentIndex componentIndex)
    {
      var value = GetMaterial(componentIndex, Guid.Empty, null);
      return value;
    }

    /// <summary>
    /// Query the object for the value of a given named custom render mesh parameter.
    /// </summary>
    /// <param name="providerId">Id of the custom render mesh provider</param>
    /// <param name="parameterName">Name of the parameter</param>
    /// <returns>IConvertible. Note that you can't directly cast from object, instead you have to use the Convert mechanism.</returns>
    /// <since>6.0</since>
    [CLSCompliant(false)]
    public virtual IConvertible GetCustomRenderMeshParameter(Guid providerId, String parameterName)
    {
      var value = new Variant();

      bool bRet = UnsafeNativeMethods.Rdk_GetCRMParameter(ConstPointer(), providerId, parameterName, value.NonConstPointer());

      return bRet ? value : null;
    }

    /// <summary>
    /// Set the named custom render mesh parameter value for this object.
    /// </summary>
    /// <param name="providerId">Id of the custom render mesh provider</param>
    /// <param name="parameterName"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    /// <since>6.0</since>
    public virtual void SetCustomRenderMeshParameter(Guid providerId, String parameterName, object value)
    {
      var v = new Variant(value);

      UnsafeNativeMethods.Rdk_SetCRMParameter(ConstPointer(), providerId, parameterName, v.ConstPointer());
    }


    /// <summary>
    /// Called when Rhino wants to draw this object
    /// </summary>
    /// <param name="e"></param>
    protected virtual void OnDraw(Display.DrawEventArgs e)
    {
      var p_const_this = ConstPointer();
      UnsafeNativeMethods.CRhinoObject_Draw(p_const_this, e.m_ptr_display_pipeline);
    }

    /// <summary>
    /// Called when this a new instance of this object is created and copied from
    /// an existing object
    /// </summary>
    /// <param name="source"></param>
    protected virtual void OnDuplicate(RhinoObject source) { }

    /// <summary>
    /// This call informs an object it is about to be deleted.
    /// Some objects, like clipping planes, need to do a little extra cleanup
    /// before they are deleted.
    /// </summary>
    /// <param name="doc"></param>
    protected virtual void OnDeleteFromDocument(RhinoDoc doc) { }

    /// <summary>
    /// This call informs an object it is about to be added to the list of
    /// active objects in the document.
    /// </summary>
    /// <param name="doc"></param>
    protected virtual void OnAddToDocument(RhinoDoc doc) { }

    /// <summary>
    /// Determine if this object is active in a particular viewport.
    /// </summary>
    /// <param name="viewport"></param>
    /// <remarks>
    /// The default implementation tests for space and viewport id. This
    /// handles things like testing if a page space object is visible in a
    /// modeling view.
    /// </remarks>
    /// <returns>True if the object is active in viewport</returns>
    /// <since>5.0</since>
    public virtual bool IsActiveInViewport(Display.RhinoViewport viewport)
    {
      var p_const_this = ConstPointer();
      return UnsafeNativeMethods.CRhinoObject_IsActiveInViewport(p_const_this, viewport.ConstPointer());
    }

    /// <summary>
    /// Called to determine if this object or some sub-portion of this object should be
    /// picked given a pick context.
    /// </summary>
    /// <param name="context"></param>
    protected virtual System.Collections.Generic.IEnumerable<ObjRef> OnPick(Input.Custom.PickContext context)
    {
      var p_const_this = ConstPointer();
      var p_const_context = context.ConstPointer();
      var p_obj_ref_array = UnsafeNativeMethods.CRhinoObject_Pick(p_const_this, p_const_context);
      if (IntPtr.Zero == p_obj_ref_array)
        return null;
      return new Runtime.InternalRhinoObjRefArray(p_obj_ref_array);
    }

    /// <summary>
    /// Called when this object has been picked
    /// </summary>
    /// <param name="context"></param>
    /// <param name="pickedItems">
    /// Items that were picked. This parameter is enumerable because there may
    /// have been multiple sub-objects picked
    /// </param>
    protected virtual void OnPicked(Input.Custom.PickContext context, System.Collections.Generic.IEnumerable<ObjRef> pickedItems)
    {
    }

    /// <summary>
    /// Called when the selection state of this object has changed
    /// </summary>
    protected virtual void OnSelectionChanged()
    {
    }

    /// <summary>
    /// Called when a transformation has been applied to the geometry
    /// </summary>
    /// <param name="transform"></param>
    protected virtual void OnTransform(Transform transform)
    {
    }

    /// <summary>
    /// Called when a space morph has been applied to the geometry.
    /// Currently this only works for CustomMeshObject instances
    /// </summary>
    /// <param name="morph"></param>
    protected virtual void OnSpaceMorph(SpaceMorph morph)
    {
    }

    /// <summary>
    /// If this object has a history record, the CopyOnReplace field is set
    ///  When an object is replaced in a document and the old object has a history record with
    /// this field set, the history record is copied and attached to the new object.
    /// That allows a descendant object to continue the history linkage after
    /// it is edited.
    /// </summary>
    /// <param name="bCopy"></param>
    /// <since>7.1</since>
    public void SetCopyHistoryOnReplace(bool bCopy)
    {
      var p_const_this = ConstPointer();
      UnsafeNativeMethods.CRhinoObject_SetCopyHistoryOnReplace(p_const_this, bCopy);
    }

    /// <summary>
    /// Gets the setting of the CopyOnReplace field in this object's history
    /// </summary>
    /// <returns>
    /// true if this object has history and the field is set
    /// false otherwise
    /// </returns>
    /// <since>7.1</since>
    public bool CopyHistoryOnReplace()
    {
      var p_const_this = ConstPointer();
      return UnsafeNativeMethods.CRhinoObject_CopyHistoryOnReplace(p_const_this);
    }

    /// <summary>
    /// Returns whether this object has a history record
    /// </summary>
    /// <returns></returns>
    /// <since>7.1</since>
    public bool HasHistoryRecord()
    {
      var p_const_this = ConstPointer();
      return UnsafeNativeMethods.CRhinoObject_HasHistoryRecord(p_const_this);
    }

    internal bool IsCustom
    {
      get
      {
        IntPtr ptr = ConstPointer();
        return UnsafeNativeMethods.CRhinoObject_IsCustom(ptr);
      }
    }
  }

  /// <summary>
  /// A proxy object (not saved in files)
  /// </summary>
  public class ProxyObject : RhinoObject
  {
    Mesh[] _meshes = new Mesh[0];

    internal ProxyObject(uint serialNumber) : base(serialNumber)
    { }

    /// <inheritdoc/>
    /// <since>7.0</since>
    public override int CreateMeshes(MeshType meshType, MeshingParameters parameters, bool ignoreCustomParameters)
    {
      int rc = base.CreateMeshes(meshType, parameters, ignoreCustomParameters);
      if( 0==rc )
      {
        // 7 Mar 2020 S. Baer (RH-54886)
        // For now I'm just doing the bare minimum required to get the
        // above YT issue fixed. I haven't settled completely on how this
        // should be implemented. Another approach that I may take is to
        // hang the meshes off of the ON_Geometry when appropriate. The
        // downside to that approach is that the Meshes will soon be deleted
        // when the proxy geometry is destroyed.
        Brep brep = Geometry as Brep;
        if( brep!=null )
        {
          _meshes = Mesh.CreateFromBrep(brep, parameters);
          if (_meshes != null)
            rc = _meshes.Length;
        }
      }
      return rc;
    }

    /// <inheritdoc/>
    /// <since>7.0</since>
    public override Mesh[] GetMeshes(MeshType meshType)
    {
      Mesh[] rc = base.GetMeshes(meshType);
      if( rc==null || rc.Length == 0)
      {
        rc = _meshes;
      }
      return rc;
    }
  }
#endif
  /// <summary>
  /// Defines enumerated values for several kinds of selection methods.
  /// </summary>
  /// <since>5.0</since>
  public enum SelectionMethod
  {
    /// <summary>
    /// Selected by non-mouse method (SelAll, etc.)
    /// </summary>
    Other = 0,

    /// <summary>
    /// Selected by a mouse click on the object.
    /// </summary>
    MousePick = 1,

    /// <summary>
    /// Selected by a mouse selection window box. 
    /// Window selection indicates the object is completely contained by the selection rectangle.
    /// </summary>
    WindowBox = 2,

    /// <summary>
    /// Selected by a mouse selection crossing box. 
    /// A crossing selection indicates the object intersects with the selection rectangle.
    /// </summary>
    CrossingBox = 3
  }

  // skipping CRhinoPhantomObject, CRhinoProxyObject

#if RHINO_SDK
  // all ObjRef's are created in .NET
  /// <summary>
  /// Represents a reference to a Rhino object.
  /// </summary>
  public class ObjRef : IDisposable
  {
    private IntPtr m_ptr; // pointer to unmanaged CRhinoObjRef
    internal IntPtr ConstPointer() { return m_ptr; }
    internal IntPtr NonConstPointer() { return m_ptr; }
    internal ObjRef()
    {
      m_ptr = UnsafeNativeMethods.CRhinoObjRef_New();
    }

    /// <summary>
    /// Copy constructor
    /// </summary>
    /// <param name="other"></param>
    /// <since>7.0</since>
    public ObjRef(ObjRef other)
    {
      m_ptr = UnsafeNativeMethods.CRhinoObjRef_Copy(other.m_ptr);
    }
    internal ObjRef(IntPtr pOtherObjRef)
    {
      m_ptr = UnsafeNativeMethods.CRhinoObjRef_Copy(pOtherObjRef);
    }

    internal ObjRef(RhinoDoc doc, IntPtr pOtherObjRef, bool ptrIsCRhinoObjRef)
    {
      m_ptr = ptrIsCRhinoObjRef ? UnsafeNativeMethods.CRhinoObjRef_Copy(pOtherObjRef) : UnsafeNativeMethods.CRhinoObjRef_FromOnObjRef(doc?.RuntimeSerialNumber ?? 0, pOtherObjRef);
    }

    internal ObjRef(RhinoObject parent, IntPtr pGeometry)
    {
      var p_parent = parent.ConstPointer();
      m_ptr = UnsafeNativeMethods.CRhinoObjRef_New3(p_parent, pGeometry);
    }

    /// <summary>
    /// Initializes a new object reference from a globally unique identifier (<see cref="Guid"/>).
    /// </summary>
    /// <param name="id">The ID.</param>
    /// <since>5.0</since>
    [Obsolete("Use version that takes a document.")]
    public ObjRef(Guid id)
    {
      m_ptr = UnsafeNativeMethods.CRhinoObjRef_New1(0, id);
    }

    /// <summary>
    /// Initializes a new object reference from a globally unique identifier (<see cref="Guid"/>).
    /// </summary>
    /// <param name="doc">The Rhino document</param>
    /// <param name="id">The ID.</param>
    /// <since>7.6</since>
    public ObjRef(RhinoDoc doc, Guid id)
    {
      if (null == doc)
        m_ptr = UnsafeNativeMethods.CRhinoObjRef_New1(0, id);
      else
        m_ptr = UnsafeNativeMethods.CRhinoObjRef_New1(doc.RuntimeSerialNumber, id);
    }

    /// <summary>
    /// Initializes a new object reference from a guid and component index. The
    /// component index is used to specify a "piece" of the geometry
    /// </summary>
    /// <param name="id">The object's Id</param>
    /// <param name="ci">a portion of the object</param>
    /// <since>7.0</since>
    [Obsolete("Use version that takes a document.")]
    public ObjRef(Guid id, Geometry.ComponentIndex ci)
    {
      m_ptr = UnsafeNativeMethods.CRhinoObjRef_New5(0, id, ref ci);
    }

    /// <summary>
    /// Initializes a new object reference from a guid and component index. The
    /// component index is used to specify a "piece" of the geometry
    /// </summary>
    /// <param name="doc">The Rhino document</param>
    /// <param name="id">The object's Id</param>
    /// <param name="ci">a portion of the object</param>
    /// <since>7.6</since>
    public ObjRef(RhinoDoc doc, Guid id, Geometry.ComponentIndex ci)
    {
      m_ptr = UnsafeNativeMethods.CRhinoObjRef_New5(doc.RuntimeSerialNumber, id, ref ci);
    }


    /// <summary>
    /// Initializes a new object reference from a Rhino object.
    /// </summary>
    /// <param name="rhinoObject">The Rhino object.</param>
    /// <since>5.0</since>
    public ObjRef(RhinoObject rhinoObject)
    {
      var p_object = rhinoObject.ConstPointer();
      m_ptr = UnsafeNativeMethods.CRhinoObjRef_New2(p_object);
    }

    /// <summary>
    /// Initialized a new object reference from a Rhino object and pick context
    /// </summary>
    /// <param name="rhinoObject"></param>
    /// <param name="pickContext"></param>
    /// <since>5.0</since>
    public ObjRef(RhinoObject rhinoObject, Input.Custom.PickContext pickContext)
    {
      var p_object = rhinoObject.ConstPointer();
      var p_pick_context = pickContext.ConstPointer();
      m_ptr = UnsafeNativeMethods.CRhinoObjRef_New4(p_object, p_pick_context);
    }

    /// <summary>Returns the id of the referenced Rhino object.</summary>
    /// <since>5.0</since>
    public Guid ObjectId
    {
      get { return UnsafeNativeMethods.CRhinoObjRef_ObjectUuid(m_ptr); }
    }

    /// <summary>
    /// If &gt; 0, then this is the value of a Rhino object's serial number field.
    /// The serial number is used instead of the pointer to prevent crashes in
    /// cases when the RhinoObject is deleted but an ObjRef continues to reference
    /// the Rhino object. The value of RuntimeSerialNumber is not saved in archives
    /// because it generally changes if you save and reload an archive.
    /// </summary>
    /// <since>5.0</since>
    [CLSCompliant(false)]
    public uint RuntimeSerialNumber
    {
      get { return UnsafeNativeMethods.CRhinoObjRef_RuntimeSN(m_ptr); }
    }

    /// <summary>
    /// Gets the component index of the referenced (sub) geometry.
    /// Some objects have sub-objects that are valid pieces of geometry. For
    /// example, breps have edges and faces that are valid curves and surfaces.
    /// Each sub-object has a component index that is &gt; 0. The parent
    /// geometry has a component index = -1.
    /// </summary>
    /// <since>5.0</since>
    public ComponentIndex GeometryComponentIndex
    {
      get
      {
        var ci = new ComponentIndex();
        UnsafeNativeMethods.CRhinoObjRef_GeometryComponentIndex(m_ptr, ref ci);
        return ci;
      }
    }

    internal IntPtr GetGeometryConstPointer(GeometryBase geometry)
    {
      if (geometry is Surface)
        return UnsafeNativeMethods.CRhinoObjRef_Surface(m_ptr);
      if (geometry is Curve)
        return UnsafeNativeMethods.CRhinoObjRef_Curve(m_ptr);
      if (geometry is Point)
        return UnsafeNativeMethods.CRhinoObjRef_Point(m_ptr);
      if (geometry is Brep)
        return UnsafeNativeMethods.CRhinoObjRef_Brep(m_ptr);
      if (geometry is SubD)
        return UnsafeNativeMethods.CRhinoObjRef_SubD(m_ptr);

      return UnsafeNativeMethods.CRhinoObjRef_Geometry(m_ptr);
    }

    private GeometryBase ObjRefToGeometryHelper(IntPtr pGeometry)
    {
      if (pGeometry == IntPtr.Zero)
        return null;
      object parent;
      if (UnsafeNativeMethods.CRhinoObjRef_IsTopLevelGeometryPointer(m_ptr, pGeometry))
        parent = Object();
      else
        parent = new ObjRef(this); // copy in case user decides to call Dispose on this ObjRef
      return null == parent ? null : GeometryBase.CreateGeometryHelper(pGeometry, parent);
    }

    /// <summary>
    /// Gets the geometry linked to the object targeted by this reference.
    /// </summary>
    /// <returns>The geometry.</returns>
    /// <since>5.0</since>
    public GeometryBase Geometry()
    {
      var p_geometry = UnsafeNativeMethods.CRhinoObjRef_Geometry(m_ptr);
      return ObjRefToGeometryHelper(p_geometry);
    }

    /// <summary>
    /// Gets the clipping plane surface if this reference targeted one.
    /// </summary>
    /// <returns>A clipping plane surface, or null if this reference targeted something else.</returns>
    /// <since>5.0</since>
    public ClippingPlaneSurface ClippingPlaneSurface()
    {
      var p_clipping_plane_surface = UnsafeNativeMethods.CRhinoObjRef_ClippingPlaneSurface(m_ptr);
      return ObjRefToGeometryHelper(p_clipping_plane_surface) as ClippingPlaneSurface;
    }

    /// <summary>
    /// Gets the curve if this reference targeted one.
    /// </summary>
    /// <returns>A curve, or null if this reference targeted something else.</returns>
    /// <example>
    /// <code source='examples\vbnet\ex_intersectcurves.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_intersectcurves.cs' lang='cs'/>
    /// <code source='examples\py\ex_intersectcurves.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    public Curve Curve()
    {
      var ptr_curve = UnsafeNativeMethods.CRhinoObjRef_Curve(m_ptr);
      return ObjRefToGeometryHelper(ptr_curve) as Curve;
    }

    /// <summary>
    /// Gets the edge if this reference geometry is one.
    /// </summary>
    /// <returns>A boundary representation edge; or null on error.</returns>
    /// <since>5.0</since>
    public BrepEdge Edge()
    {
      var pre_brep_edge = UnsafeNativeMethods.CRhinoObjRef_Edge(m_ptr);
      return ObjRefToGeometryHelper(pre_brep_edge) as BrepEdge;
    }

    /// <summary>
    /// If the referenced geometry is a brep face, a brep with one face, or
    /// a surface, this returns the brep face.
    /// </summary>
    /// <returns>A boundary representation face; or null on error.</returns>
    /// <since>5.0</since>
    public BrepFace Face()
    {
      var ptr_brep_face = UnsafeNativeMethods.CRhinoObjRef_Face(m_ptr);
      return ObjRefToGeometryHelper(ptr_brep_face) as BrepFace;
    }

    /// <summary>
    /// If the referenced geometry is an edge of a surface,
    /// this returns the associated brep trim.
    /// </summary>
    /// <returns>A boundary representation trim; or null on error</returns>
    /// <since>5.8</since>
    public BrepTrim Trim()
    {
      var ptr_brep_trim = UnsafeNativeMethods.CRhinoObjRef_Trim(m_ptr);
      return ObjRefToGeometryHelper(ptr_brep_trim) as BrepTrim;
    }

    /// <summary>
    ///  Gets the brep if this reference geometry is one.
    /// </summary>
    /// <returns>A boundary representation; or null on error.</returns>
    /// <example>
    /// <code source='examples\vbnet\ex_booleandifference.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_booleandifference.cs' lang='cs'/>
    /// <code source='examples\py\ex_booleandifference.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    public Brep Brep()
    {
      var p_brep = UnsafeNativeMethods.CRhinoObjRef_Brep(m_ptr);
      return ObjRefToGeometryHelper(p_brep) as Brep;
    }

    /// <summary>
    /// Gets the surface if the referenced geometry is one.
    /// </summary>
    /// <returns>A surface; or null if the referenced object is not a surface, or on error.</returns>
    /// <example>
    /// <code source='examples\vbnet\ex_orientonsrf.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_orientonsrf.cs' lang='cs'/>
    /// <code source='examples\py\ex_orientonsrf.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    public Surface Surface()
    {
      var p_surface = UnsafeNativeMethods.CRhinoObjRef_Surface(m_ptr);
      return ObjRefToGeometryHelper(p_surface) as Surface;
    }

    /// <summary>
    /// Gets the text dot if the referenced geometry is one.
    /// </summary>
    /// <returns>A text dot; or null if the referenced object is not a text dot, or on error.</returns>
    /// <since>5.0</since>
    public TextDot TextDot()
    {
      var p_text_dot = UnsafeNativeMethods.CRhinoObjRef_TextDot(m_ptr);
      return ObjRefToGeometryHelper(p_text_dot) as TextDot;
    }

    /// <summary>
    /// Gets the mesh if the referenced geometry is one.
    /// </summary>
    /// <returns>A mesh; or null if the referenced object is not a mesh, or on error.</returns>
    /// <since>5.0</since>
    public Mesh Mesh()
    {
      var p_mesh = UnsafeNativeMethods.CRhinoObjRef_Mesh(m_ptr);
      return ObjRefToGeometryHelper(p_mesh) as Mesh;
    }

    /// <summary>
    /// Gets the SubD if the referenced geometry is one.
    /// </summary>
    /// <returns>A SubD; or null if the referenced object is not a SubD, or on error.</returns>
    /// <since>7.0</since>
    public SubD SubD()
    {
      var p_subd = UnsafeNativeMethods.CRhinoObjRef_SubD(m_ptr);
      return ObjRefToGeometryHelper(p_subd) as SubD;
    }

    /// <summary>
    /// Gets the SubDFace if the referenced geometry is one.
    /// </summary>
    /// <returns>A SubDFace; or null if the referenced object is not a SubDFace, or on error.</returns>
    /// <since>7.0</since>
    public SubDFace SubDFace()
    {
      uint id = 0;
      IntPtr ptrSubD = UnsafeNativeMethods.CRhinoObjRef_SubDFace(m_ptr, ref id);
      SubD parentSubD = ObjRefToGeometryHelper(ptrSubD) as SubD;
      if (parentSubD != null)
        return parentSubD.Faces.Find(id);
      return null;
    }

    /// <summary>
    /// Gets the point if the referenced geometry is one.
    /// </summary>
    /// <returns>A point; or null if the referenced object is not a point, or on error.</returns>
    /// <since>5.0</since>
    public Point Point()
    {
      var p_point = UnsafeNativeMethods.CRhinoObjRef_Point(m_ptr);
      return ObjRefToGeometryHelper(p_point) as Point;
    }

    /// <summary>
    /// Gets the point cloud if the referenced geometry is one.
    /// </summary>
    /// <returns>A point cloud; or null if the referenced object is not a point cloud, or on error.</returns>
    /// <since>5.0</since>
    public PointCloud PointCloud()
    {
      var p_point_cloud = UnsafeNativeMethods.CRhinoObjRef_PointCloud(m_ptr);
      return ObjRefToGeometryHelper(p_point_cloud) as PointCloud;
    }

    /// <summary>
    /// Gets the text entity if the referenced geometry is one.
    /// </summary>
    /// <returns>A text entity; or null if the referenced object is not a text entity, or on error.</returns>
    /// <since>5.0</since>
    public TextEntity TextEntity()
    {
      var p_text_entity = UnsafeNativeMethods.CRhinoObjRef_Annotation(m_ptr);
      return ObjRefToGeometryHelper(p_text_entity) as TextEntity;
    }

    /// <summary>
    /// Gets the light if the referenced geometry is one.
    /// </summary>
    /// <returns>A light; or null if the referenced object is not a light, or on error.</returns>
    /// <since>5.0</since>
    public Light Light()
    {
      var p_light = UnsafeNativeMethods.CRhinoObjRef_Light(m_ptr);
      return ObjRefToGeometryHelper(p_light) as Light;
    }

    /// <summary>
    /// Gets the hatch if the referenced geometry is one.
    /// </summary>
    /// <returns>A hatch; or null if the referenced object is not a hatch</returns>
    /// <since>5.0</since>
    public Hatch Hatch()
    {
      return Geometry() as Hatch;
    }

    //private bool IsSubGeometry()
    //{
    //  return UnsafeNativeMethods.CRhinoObjRef_IsSubGeometry(m_ptr);
    //}

    /// <summary>
    /// Passively reclaims unmanaged resources when the class user did not explicitly call Dispose().
    /// </summary>
    ~ObjRef()
    {
      // 9 Jan 2020 S. Baer (RH-50670)
      // Attempt to work around crash in garbage collection thread.
      // Experimenting with placing this on a list of objects to be deleted on
      // the main thread after the next command completes
      if( Rhino.RhinoApp.IsRunningHeadless )
        Dispose(false); // just dispose when headless since we aren't running commands
      else
        Rhino.Runtime.HostUtils.AddObjectsToDeleteOnMainThread(this);
    }

    /// <summary>
    /// Actively reclaims unmanaged resources that this instance uses.
    /// </summary>
    /// <since>5.0</since>
    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    /// <summary>
    /// For derived class implementers.
    /// <para>This method is called with argument true when class user calls Dispose(), while with argument false when
    /// the Garbage Collector invokes the finalizer, or Finalize() method.</para>
    /// <para>You must reclaim all used unmanaged resources in both cases, and can use this chance to call Dispose on disposable fields if the argument is true.</para>
    /// <para>Also, you must call the base virtual method within your overriding method.</para>
    /// </summary>
    /// <param name="disposing">true if the call comes from the Dispose() method; false if it comes from the Garbage Collector finalizer.</param>
    protected virtual void Dispose(bool disposing)
    {
      if (IntPtr.Zero != m_ptr)
      {
        UnsafeNativeMethods.CRhinoObjRef_Delete(m_ptr);
        m_ptr = IntPtr.Zero;
      }
    }

    /// <summary>Returns the referenced Rhino object.</summary>
    /// <example>
    /// <code source='examples\vbnet\ex_orientonsrf.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_orientonsrf.cs' lang='cs'/>
    /// <code source='examples\py\ex_orientonsrf.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    public RhinoObject Object()
    {
      var constPtrRhinoObject = UnsafeNativeMethods.CRhinoObjRef_Object(m_ptr);
      var rc = RhinoObject.CreateRhinoObjectHelper(constPtrRhinoObject);
      if (rc != null && UnsafeNativeMethods.CRhinoObject_IsProxyObject(constPtrRhinoObject))
        rc.m__parent = this;
      return rc;
    }

    /// <summary>
    /// If sub-object selection is enabled and a piece of an instance reference
    /// is selected, this will return the selected piece.
    /// </summary>
    /// <returns></returns>
    /// <since>6.0</since>
    public RhinoObject InstanceDefinitionPart()
    {
      var rc = UnsafeNativeMethods.CRhinoObjRef_InstancePiece(m_ptr);
      return RhinoObject.CreateRhinoObjectHelper(rc);
    }

    /// <summary>
    /// Gets the method used to select this object.
    /// </summary>
    /// <returns>The method used to select this object.</returns>
    /// <since>5.0</since>
    public SelectionMethod SelectionMethod()
    {
      var rc = UnsafeNativeMethods.CRhinoObjRef_SelectionMethod(m_ptr);

      switch (rc)
      {
        case 0:
          return DocObjects.SelectionMethod.Other;
        case 1:
          return DocObjects.SelectionMethod.MousePick;
        case 2:
          return DocObjects.SelectionMethod.WindowBox;
        case 3:
          return DocObjects.SelectionMethod.CrossingBox;
        default:
          return DocObjects.SelectionMethod.Other;
      }
    }

    /// <summary>
    /// If the object was selected by picking a point on it, then
    /// SelectionPoint() returns the point where the selection
    /// occurred, otherwise it returns Point3d.Unset.
    /// </summary>
    /// <returns>
    /// The point where the selection occurred or Point3d.Unset on failure.
    /// </returns>
    /// <example>
    /// <code source='examples\vbnet\ex_constrainedcopy.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_constrainedcopy.cs' lang='cs'/>
    /// <code source='examples\py\ex_constrainedcopy.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    public Point3d SelectionPoint()
    {
      var pt = Point3d.Unset;
      var rc = UnsafeNativeMethods.CRhinoObjRef_SelectionPoint(m_ptr, ref pt);
      if (rc)
        return pt;

      return Point3d.Unset;
    }

    /// <summary>
    /// If the object was interactively selected in a page space detail
    /// view, then SelectionViewDetailSerialNumber() returns the CRhinoObject
    /// serial number of the detail view object.  Use SelectionView()
    /// to get the page view that contains the detail view object.
    /// If SelectionViewDetailSerialNumber() returns 0, then the selection
    /// did not happen in a detail view.
    /// </summary>
    /// <returns></returns>
    /// <since>6.5</since>
    [CLSCompliant(false)]
    public uint SelectionViewDetailSerialNumber()
    {
      uint detail_view_sn = UnsafeNativeMethods.CRhinoObjRef_SelectionViewDetailSerialNumber(m_ptr);
      return detail_view_sn;
    }

    /// <summary>
    /// If the object was interactively selected in a particular viewport, then
    /// SelectionView() returns the view where the object was selected.
    /// </summary>
    /// <returns></returns>
    /// <since>6.5</since>
    public RhinoView SelectionView()
    {
      IntPtr ptr = UnsafeNativeMethods.CRhinoObjRef_SelectionView(m_ptr);
      if (ptr == IntPtr.Zero)
        return null;
      return RhinoView.FromIntPtr(ptr);
    }


    /// <summary>
    /// If the reference geometry is a curve or edge with a selection
    /// point, then this gets the parameter of the selection point.
    /// </summary>
    /// <param name="parameter">The parameter of the selection point.</param>
    /// <returns>If the selection point was on a curve or edge, then the
    /// curve/edge is returned, otherwise null.</returns>
    /// <remarks>
    /// If a curve was selected and CurveParameter is called and the 
    /// SelectionMethod() is not 1 (point pick on object), the curve will
    /// be returned and parameter will be set to the start parameter of
    /// the picked curve. This can be misleading so it may be necessary
    /// to call SelectionMethod() first, before calling CurveParameter
    /// to get the desired information.</remarks>
    /// <example>
    /// <code source='examples\vbnet\ex_addradialdimension.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_addradialdimension.cs' lang='cs'/>
    /// <code source='examples\py\ex_addradialdimension.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    public Curve CurveParameter(out double parameter)
    {
      parameter = 0.0;

      var p_curve = UnsafeNativeMethods.CRhinoObjRef_CurveParameter(m_ptr, ref parameter);
      return ObjRefToGeometryHelper(p_curve) as Curve;
    }

    /// <summary>
    /// If the reference geometry is a surface, brep with one face,
    /// or surface edge with a selection point, then this gets the 
    /// surface parameters of the selection point.
    /// </summary>
    /// <param name="u">The U value is assigned to this out parameter during the call.</param>
    /// <param name="v">The V value is assigned to this out parameter during the call.</param>
    /// <returns>
    /// If the selection point was on a surface, then the surface is returned.
    /// </returns>
    /// <since>5.0</since>
    public Surface SurfaceParameter(out double u, out double v)
    {
      u = 0.0;
      v = 0.0;

      var p_surface = UnsafeNativeMethods.CRhinoObjRef_SurfaceParameter(m_ptr, ref u, ref v);
      return ObjRefToGeometryHelper(p_surface) as Surface;
    }

    /// <summary>
    /// When an object is selected by picking a sub-object, SetSelectionComponent
    /// may be used to identify the sub-object.
    /// </summary>
    /// <param name="componentIndex"></param>
    /// <since>5.0</since>
    public void SetSelectionComponent(ComponentIndex componentIndex)
    {
      var p_this = NonConstPointer();
      UnsafeNativeMethods.CRhinoObjRef_SetSelectionComponent(p_this, componentIndex);
    }
  }
#endif


  // skipping CRhinoObjRefArray
}

#if RHINO_SDK

namespace Rhino.Runtime
{
  // do not make this class available in the public SDK. It is pretty hairy
  class InternalRhinoObjectArray : IDisposable
  {
    IntPtr m_ptr; //ON_SimpleArray<CRhinoObject*>*
    readonly System.Collections.Generic.List<DocObjects.RhinoObject> m_rhino_objects;
    //public IntPtr ConstPointer() { return m_ptr; }
    public IntPtr NonConstPointer() { return m_ptr; }

    public InternalRhinoObjectArray()
    {
      m_ptr = UnsafeNativeMethods.RhinoObjectArray_New(0);
    }
    public InternalRhinoObjectArray(System.Collections.Generic.IEnumerable<DocObjects.RhinoObject> rhinoObjects)
    {
      m_rhino_objects = new System.Collections.Generic.List<DocObjects.RhinoObject>(rhinoObjects);
      var count = m_rhino_objects.Count;
      m_ptr = UnsafeNativeMethods.RhinoObjectArray_New(count);

      for (var i = 0; i < count; i++)
      {
        var p_rhino_object = m_rhino_objects[i].ConstPointer();
        UnsafeNativeMethods.RhinoObjectArray_Add(m_ptr, p_rhino_object);
      }
    }

    public DocObjects.RhinoObject[] ToNonConstArray()
    {
      return ToArrayFromPointer(m_ptr, false);
    }

    public DocObjects.RhinoObject[] ToArray()
    {
      if (null != m_rhino_objects)
        return m_rhino_objects.ToArray();

      return ToArrayFromPointer(m_ptr, true);
    }

    public static DocObjects.RhinoObject[] ToArrayFromPointer(IntPtr pRhinoObjectArray, bool documentControlled)
    {
      if (IntPtr.Zero == pRhinoObjectArray)
        return new DocObjects.RhinoObject[0];

      var count = UnsafeNativeMethods.RhinoObjectArray_Count(pRhinoObjectArray);
      var rc = new DocObjects.RhinoObject[count];
      for (var i = 0; i < count; i++)
      {
        var p_rhino_object = UnsafeNativeMethods.RhinoObjectArray_Get(pRhinoObjectArray, i);
        rc[i] = DocObjects.RhinoObject.CreateRhinoObjectHelper(p_rhino_object);
        if( ! documentControlled )
        {
          if (UnsafeNativeMethods.CRhinoObject_Document(p_rhino_object) == 0)
            rc[i].m_pRhinoObject = p_rhino_object;
        }
      }
      return rc;
    }

    ~InternalRhinoObjectArray()
    {
      Dispose(false);
    }

    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
      if (IntPtr.Zero != m_ptr)
      {
        UnsafeNativeMethods.RhinoObjectArray_Delete(m_ptr);
        m_ptr = IntPtr.Zero;
      }
    }
  }

  class InternalRhinoObjRefArray : System.Collections.Generic.IEnumerable<DocObjects.ObjRef>, IDisposable
  {
    IntPtr m_p_rhino_obj_ref_array; //CRhinoObjRefArray*
    //public IntPtr ConstPointer() { return m_ptr; }
    public IntPtr NonConstPointer() { return m_p_rhino_obj_ref_array; }

    public InternalRhinoObjRefArray()
    {
      m_p_rhino_obj_ref_array = UnsafeNativeMethods.CRhinoObjRefArray_New();
    }

    public InternalRhinoObjRefArray(IntPtr pRhinoObjRefArray)
    {
      m_p_rhino_obj_ref_array = pRhinoObjRefArray;
    }

    ~InternalRhinoObjRefArray()
    {
      Dispose(false);
    }

    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
      if (IntPtr.Zero != m_p_rhino_obj_ref_array)
      {
        UnsafeNativeMethods.CRhinoObjRefArray_Delete(m_p_rhino_obj_ref_array);
        m_p_rhino_obj_ref_array = IntPtr.Zero;
      }
    }

    System.Collections.Generic.IEnumerator<DocObjects.ObjRef> GetEnumeratorHelper()
    {
      var count = UnsafeNativeMethods.CRhinoObjRefArray_Count(m_p_rhino_obj_ref_array);
      for (var i = 0; i < count; i++)
      {
        var p_rhino_obj_ref = UnsafeNativeMethods.CRhinoObjRefArray_GetItem(m_p_rhino_obj_ref_array, i);
        if (IntPtr.Zero != p_rhino_obj_ref)
        {
          yield return new DocObjects.ObjRef(p_rhino_obj_ref);
        }
      }
    }

    public System.Collections.Generic.IEnumerator<DocObjects.ObjRef> GetEnumerator()
    {
      return GetEnumeratorHelper();
    }
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
    {
      return GetEnumeratorHelper();
    }
  }
}

#endif
