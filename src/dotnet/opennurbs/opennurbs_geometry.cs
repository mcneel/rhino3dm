using System;
using System.Runtime.Serialization;
using Rhino.DocObjects;
using Rhino.Runtime;

namespace Rhino.Geometry
{
  /// <summary>
  /// Provides a common base for most geometric classes. This class is abstract.
  /// </summary>
  [Serializable]
  public abstract class GeometryBase : Runtime.CommonObject
  {
    #region constructors / wrapped pointer manipulation
    GeometryBase m_shallow_parent;

    // make internal so outside DLLs can't directly subclass GeometryBase
    internal GeometryBase() { }

    /// <summary>
    /// Protected constructor for internal use.
    /// </summary>
    /// <param name="info">Serialization data.</param>
    /// <param name="context">Serialization stream.</param>
    protected GeometryBase(SerializationInfo info, StreamingContext context)
      :base(info, context)
    {
    }

    internal override IntPtr _InternalDuplicate(out bool applymempressure)
    {
      applymempressure = true;
      IntPtr const_ptr_this = ConstPointer();
      IntPtr rc = UnsafeNativeMethods.ON_Object_Duplicate(const_ptr_this);
      return rc;
    }

    internal override IntPtr _InternalGetConstPointer()
    {
      if (null != m_shallow_parent)
        return m_shallow_parent.ConstPointer();

#if RHINO_SDK
      ObjRef obj_ref = m__parent as ObjRef;
      if (null != obj_ref)
        return obj_ref.GetGeometryConstPointer(this);

      RhinoObject parent_object = ParentRhinoObject();
      if (parent_object == null)
      {
        FileIO.File3dmObject fileobject = m__parent as FileIO.File3dmObject;
        if (null != fileobject)
          return fileobject.GetGeometryConstPointer();
      }

      uint serial_number = 0;
      IntPtr ptr_parent_rhinoobject = IntPtr.Zero;
      if (null != parent_object)
      {
        serial_number = parent_object.m_rhinoobject_serial_number;
        ptr_parent_rhinoobject = parent_object.m_pRhinoObject;
        if(IntPtr.Zero == ptr_parent_rhinoobject && 
           parent_object.m__parent != null &&
           parent_object.m__parent is ObjRef)
        {
          ObjRef objref = parent_object.m__parent as ObjRef;
          IntPtr constPtrObjRef = objref.ConstPointer();
          ptr_parent_rhinoobject = UnsafeNativeMethods.CRhinoObjRef_Object(constPtrObjRef);
        }
      }
      ComponentIndex ci = new ComponentIndex();
      // There are a few cases (like in ReplaceObject callback) where the parent
      // rhino object temporarily holds onto the CRhinoObject* because the object
      // is not officially in the document yet.
      if (ptr_parent_rhinoobject != IntPtr.Zero)
        return UnsafeNativeMethods.CRhinoObject_Geometry(ptr_parent_rhinoobject, ci);
      return UnsafeNativeMethods.CRhinoObject_Geometry2(serial_number, ci);
#else
      var fileobject = m__parent as Rhino.FileIO.File3dmObject;
      if (null != fileobject)
        return fileobject.GetGeometryConstPointer();
      return IntPtr.Zero;
#endif
    }

    internal override object _GetConstObjectParent()
    {
      if (!IsDocumentControlled)
        return null;
      if (null != m_shallow_parent)
        return m_shallow_parent;
      return base._GetConstObjectParent();
    }

    /// <summary>
    /// Is called when a non-const operation occurs.
    /// </summary>
    protected override void OnSwitchToNonConst()
    {
      m_shallow_parent = null;
      base.OnSwitchToNonConst();
    }

    /// <summary>
    /// If true this object may not be modified. Any properties or functions that attempt
    /// to modify this object when it is set to "IsReadOnly" will throw a NotSupportedException.
    /// </summary>
    public sealed override bool IsDocumentControlled
    {
      get
      {
        if (null != m_shallow_parent)
          return m_shallow_parent.IsDocumentControlled;
        return base.IsDocumentControlled;
      }
    }

    /// <summary>
    /// Constructs a light copy of this object. By "light", it is meant that the same
    /// underlying data is used until something is done to attempt to change it. For example,
    /// you could have a shallow copy of a very heavy mesh object and the same underlying
    /// data will be used when doing things like inspecting the number of faces on the mesh.
    /// If you modify the location of one of the mesh vertices, the shallow copy will create
    /// a full duplicate of the underlying mesh data and the shallow copy will become a
    /// deep copy.
    /// </summary>
    /// <returns>An object of the same type as this object.
    /// <para>This behavior is overridden by implementing classes.</para></returns>
    public GeometryBase DuplicateShallow()
    {
      GeometryBase rc = DuplicateShallowHelper();
      if (null != rc)
        rc.m_shallow_parent = this;
      return rc;
    }
    internal virtual GeometryBase DuplicateShallowHelper()
    {
      return null;
    }

    /// <summary>
    /// Constructs a deep (full) copy of this object.
    /// </summary>
    /// <returns>An object of the same type as this, with the same properties and behavior.</returns>
    public virtual GeometryBase Duplicate()
    {
      IntPtr ptr = ConstPointer();
      IntPtr ptr_new_geometry = UnsafeNativeMethods.ON_Object_Duplicate(ptr);
      return CreateGeometryHelper(ptr_new_geometry, null);
    }


    internal GeometryBase(IntPtr ptr, object parent, int subobjectIndex)
    {
      if (subobjectIndex >= 0 && parent == null)
      {
        throw new ArgumentException();
      }

      if (null == parent)
        ConstructNonConstObject(ptr);
      else
        ConstructConstObject(parent, subobjectIndex);
    }

    internal static GeometryBase CreateGeometryHelper(IntPtr pGeometry, object parent)
    {
      return CreateGeometryHelper(pGeometry, parent, -1);
    }

    internal static GeometryBase CreateGeometryHelper(IntPtr pGeometry, object parent, int subobjectIndex)
    {
      if (IntPtr.Zero == pGeometry)
        return null;

      var type = UnsafeNativeMethods.ON_Geometry_GetGeometryType(pGeometry);
      if (type < 0)
        return null;
      GeometryBase rc = null;

      switch (type)
      {
        case UnsafeNativeMethods.OnGeometryTypeConsts.ON_Curve: //1
          rc = new Curve(pGeometry, parent, subobjectIndex);
          break;
        case UnsafeNativeMethods.OnGeometryTypeConsts.ON_NurbsCurve: //2
          rc = new NurbsCurve(pGeometry, parent, subobjectIndex);
          break;
        case UnsafeNativeMethods.OnGeometryTypeConsts.ON_PolyCurve: // 3
          rc = new PolyCurve(pGeometry, parent, subobjectIndex);
          break;
        case UnsafeNativeMethods.OnGeometryTypeConsts.ON_PolylineCurve: //4
          rc = new PolylineCurve(pGeometry, parent, subobjectIndex);
          break;
        case UnsafeNativeMethods.OnGeometryTypeConsts.ON_ArcCurve: //5
          rc = new ArcCurve(pGeometry, parent, subobjectIndex);
          break;
        case UnsafeNativeMethods.OnGeometryTypeConsts.ON_LineCurve: //6
          rc = new LineCurve(pGeometry, parent, subobjectIndex);
          break;
        case UnsafeNativeMethods.OnGeometryTypeConsts.ON_Mesh: //7
          rc = new Mesh(pGeometry, parent);
          break;
        case UnsafeNativeMethods.OnGeometryTypeConsts.ON_Point: //8
          rc = new Point(pGeometry, parent);
          break;
        case UnsafeNativeMethods.OnGeometryTypeConsts.ON_TextDot: //9
          rc = new TextDot(pGeometry, parent);
          break;
        case UnsafeNativeMethods.OnGeometryTypeConsts.ON_Surface: //10
          rc = new Surface(pGeometry, parent);
          break;
        case UnsafeNativeMethods.OnGeometryTypeConsts.ON_Brep: //11
          rc = new Brep(pGeometry, parent);
          break;
        case UnsafeNativeMethods.OnGeometryTypeConsts.ON_NurbsSurface: //12
          rc = new NurbsSurface(pGeometry, parent);
          break;
        case UnsafeNativeMethods.OnGeometryTypeConsts.ON_RevSurface: //13
          rc = new RevSurface(pGeometry, parent);
          break;
        case UnsafeNativeMethods.OnGeometryTypeConsts.ON_PlaneSurface: //14
          rc = new PlaneSurface(pGeometry, parent);
          break;
        case UnsafeNativeMethods.OnGeometryTypeConsts.ON_ClippingPlaneSurface: //15
          rc = new ClippingPlaneSurface(pGeometry, parent);
          break;
        case UnsafeNativeMethods.OnGeometryTypeConsts.ON_Hatch: // 17
          rc = new Hatch(pGeometry, parent);
          break;
        case UnsafeNativeMethods.OnGeometryTypeConsts.ON_SumSurface: //19
          rc = new SumSurface(pGeometry, parent);
          break;
        case UnsafeNativeMethods.OnGeometryTypeConsts.ON_BrepFace: //20
          {
            int faceindex = -1;
            IntPtr ptr_brep = UnsafeNativeMethods.ON_BrepSubItem_Brep(pGeometry, ref faceindex);
            if (ptr_brep != IntPtr.Zero && faceindex >= 0)
            {
              Brep b = new Brep(ptr_brep, parent);
              rc = b.Faces[faceindex];
            }
          }
          break;
        case UnsafeNativeMethods.OnGeometryTypeConsts.ON_BrepEdge: // 21
          {
            int edgeindex = -1;
            IntPtr ptr_brep = UnsafeNativeMethods.ON_BrepSubItem_Brep(pGeometry, ref edgeindex);
            if (ptr_brep != IntPtr.Zero && edgeindex >= 0)
            {
              Brep b = new Brep(ptr_brep, parent);
              rc = b.Edges[edgeindex];
            }
          }
          break;
        case UnsafeNativeMethods.OnGeometryTypeConsts.ON_InstanceReference: // 23
          rc = new InstanceReferenceGeometry(pGeometry, parent);
          break;
        case UnsafeNativeMethods.OnGeometryTypeConsts.ON_Extrusion: //24
          rc = new Extrusion(pGeometry, parent);
          break;
        case UnsafeNativeMethods.OnGeometryTypeConsts.ON_PointCloud: // 26
          rc = new PointCloud(pGeometry, parent);
          break;
        case UnsafeNativeMethods.OnGeometryTypeConsts.ON_DetailView: // 27
          rc = new DetailView(pGeometry, parent);
          break;
        case UnsafeNativeMethods.OnGeometryTypeConsts.ON_Light: //32
          rc = new Light(pGeometry, parent);
          break;
        case UnsafeNativeMethods.OnGeometryTypeConsts.ON_PointGrid: //33
          rc = new Point3dGrid(pGeometry, parent);
          break;
        case UnsafeNativeMethods.OnGeometryTypeConsts.ON_MorphControl: //34
          rc = new MorphControl(pGeometry, parent);
          break;
        case UnsafeNativeMethods.OnGeometryTypeConsts.ON_BrepLoop: //35
          {
            int loopindex = -1;
            IntPtr ptr_brep = UnsafeNativeMethods.ON_BrepSubItem_Brep(pGeometry, ref loopindex);
            if (ptr_brep != IntPtr.Zero && loopindex >= 0)
            {
              Brep b = new Brep(ptr_brep, parent);
              rc = b.Loops[loopindex];
            }
          }
          break;
        case UnsafeNativeMethods.OnGeometryTypeConsts.ON_BrepTrim: // 36
          {
            int trimindex = -1;
            IntPtr ptr_brep = UnsafeNativeMethods.ON_BrepSubItem_Brep(pGeometry, ref trimindex);
            if (ptr_brep != IntPtr.Zero && trimindex >= 0)
            {
              Brep b = new Brep(ptr_brep, parent);
              rc = b.Trims[trimindex];
            }
          }
          break;

        case UnsafeNativeMethods.OnGeometryTypeConsts.ON_Leader: // 38
          rc = new Leader(pGeometry, parent);
          break;

#if OPENNURBS_SUBD_WIP
        case UnsafeNativeMethods.OnGeometryTypeConsts.ON_SubD: // 39
          rc = new SubD(pGeometry, parent);
          break;
#endif

        case UnsafeNativeMethods.OnGeometryTypeConsts.ON_DimLinear: //40
          rc = new LinearDimension(pGeometry, parent);
          break;
        case UnsafeNativeMethods.OnGeometryTypeConsts.ON_DimAngular: //41
          rc = new AngularDimension(pGeometry, parent);
          break;
        case UnsafeNativeMethods.OnGeometryTypeConsts.ON_DimRadial: //42
          rc = new RadialDimension(pGeometry, parent);
          break;
        case UnsafeNativeMethods.OnGeometryTypeConsts.ON_DimOrdinate: //43
          rc = new OrdinateDimension(pGeometry, parent);
          break;
        case UnsafeNativeMethods.OnGeometryTypeConsts.ON_Centermark: //44
          rc = new Centermark(pGeometry, parent);
          break;
        case UnsafeNativeMethods.OnGeometryTypeConsts.ON_Text: //45
          rc = new TextEntity(pGeometry, parent);
          break;
        default:
          rc = new UnknownGeometry(pGeometry, parent, subobjectIndex);
          break;
      }

      return rc;
    }

    #endregion

    /// <summary>
    /// Useful for switch statements that need to differentiate between
    /// basic object types like points, curves, surfaces, and so on.
    /// </summary>
    [CLSCompliant(false)]
    public ObjectType ObjectType
    {
      get
      {
        IntPtr ptr = ConstPointer();
        uint rc = UnsafeNativeMethods.ON_Object_ObjectType(ptr);
        return (ObjectType)rc;
      }
    }

    #region Transforms
    /// <summary>
    /// Transforms the geometry. If the input Transform has a SimilarityType of
    /// OrientationReversing, you may want to consider flipping the transformed
    /// geometry after calling this function when it makes sense. For example,
    /// you may want to call Flip() on a Brep after transforming it.
    /// </summary>
    /// <param name="xform">
    /// Transformation to apply to geometry.
    /// </param>
    /// <returns>true if geometry successfully transformed.</returns>
    public bool Transform(Transform xform)
    {
      if (xform.IsIdentity)
        return true;

      IntPtr ptr = NonConstPointer();
      return UnsafeNativeMethods.ON_Geometry_Transform(ptr, ref xform);
    }

    /// <summary>Translates the object along the specified vector.</summary>
    /// <param name="translationVector">A moving vector.</param>
    /// <returns>true if geometry successfully translated.</returns>
    public bool Translate(Vector3d translationVector)
    {
      IntPtr ptr = NonConstPointer();
      return UnsafeNativeMethods.ON_Geometry_Translate(ptr, translationVector);
    }

    /// <summary>Translates the object along the specified vector.</summary>
    /// <param name="x">The X component.</param>
    /// <param name="y">The Y component.</param>
    /// <param name="z">The Z component.</param>
    /// <returns>true if geometry successfully translated.</returns>
    public bool Translate(double x, double y, double z)
    {
      Vector3d t = new Vector3d(x, y, z);
      return Translate(t);
    }

    /// <summary>
    /// Scales the object by the specified factor. The scale is centered at the origin.
    /// </summary>
    /// <param name="scaleFactor">The uniform scaling factor.</param>
    /// <returns>true if geometry successfully scaled.</returns>
    public bool Scale(double scaleFactor)
    {
      IntPtr ptr = NonConstPointer();
      return UnsafeNativeMethods.ON_Geometry_Scale(ptr, scaleFactor);
    }

    /// <summary>
    /// Rotates the object about the specified axis. A positive rotation 
    /// angle results in a counter-clockwise rotation about the axis (right hand rule).
    /// </summary>
    /// <param name="angleRadians">Angle of rotation in radians.</param>
    /// <param name="rotationAxis">Direction of the axis of rotation.</param>
    /// <param name="rotationCenter">Point on the axis of rotation.</param>
    /// <returns>true if geometry successfully rotated.</returns>
    public bool Rotate(double angleRadians, Vector3d rotationAxis, Point3d rotationCenter)
    {
      IntPtr ptr = NonConstPointer();
      return UnsafeNativeMethods.ON_Geometry_Rotate(ptr, angleRadians, rotationAxis, rotationCenter);
    }
    #endregion

    /// <summary>
    /// Computes an estimate of the number of bytes that this object is using in memory.
    /// </summary>
    /// <returns>An estimated memory footprint.</returns>
    [CLSCompliant(false)]
    [ConstOperation]
    public uint MemoryEstimate()
    {
      IntPtr const_ptr_this = ConstPointer();
      return UnsafeNativeMethods.ON_Object_SizeOf(const_ptr_this);
    }

    /// <summary>
    /// Boundingbox solver. Gets the world axis aligned boundingbox for the geometry.
    /// </summary>
    /// <param name="accurate">If true, a physically accurate boundingbox will be computed. 
    /// If not, a boundingbox estimate will be computed. For some geometry types there is no 
    /// difference between the estimate and the accurate boundingbox. Estimated boundingboxes 
    /// can be computed much (much) faster than accurate (or "tight") bounding boxes. 
    /// Estimated bounding boxes are always similar to or larger than accurate bounding boxes.</param>
    /// <returns>
    /// The boundingbox of the geometry in world coordinates or BoundingBox.Empty 
    /// if not bounding box could be found.
    /// </returns>
    /// <example>
    /// <code source='examples\vbnet\ex_curveboundingbox.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_curveboundingbox.cs' lang='cs'/>
    /// <code source='examples\py\ex_curveboundingbox.py' lang='py'/>
    /// </example>
    [ConstOperation]
    public BoundingBox GetBoundingBox(bool accurate)
    {
#if RHINO_SDK
      RhinoObject parent_object = ParentRhinoObject();
#endif
      if (accurate)
      {
        BoundingBox bbox = new BoundingBox();
        Transform xf = new Transform();
#if RHINO_SDK
        // 3 July 2018 S. Baer (RH-46926)
        // When the object is non-const there is a good chance the geometry is
        // different than what is in the original parent object' geometry. Skip
        // using the parent's bbox getter when in this situation
        if (null != parent_object && !IsNonConst)
        {
          IntPtr ptr_parent_rhinoobject = parent_object.ConstPointer();
          if (UnsafeNativeMethods.CRhinoObject_GetTightBoundingBox(ptr_parent_rhinoobject, ref bbox, ref xf, false))
            return bbox;
        }
#endif
        AnnotationBase ann = this as AnnotationBase;
        if (ann != null)
          return ann.InternalGetBoundingBox();

        IntPtr ptr = ConstPointer();
        return UnsafeNativeMethods.ON_Geometry_GetTightBoundingBox(ptr, ref bbox, ref xf, false) ? bbox : BoundingBox.Empty;
      }
      else
      {
        BoundingBox rc = new BoundingBox();
#if RHINO_SDK
        if (null != parent_object && !IsNonConst)
        {
          IntPtr ptr_parent_rhinoobject = parent_object.ConstPointer();
          if (UnsafeNativeMethods.CRhinoObject_BoundingBox(ptr_parent_rhinoobject, ref rc))
            return rc;
        }
#endif
        AnnotationBase ann = this as AnnotationBase;
        if (ann != null)
          return ann.InternalGetBoundingBox();

        IntPtr ptr = ConstPointer();
        UnsafeNativeMethods.ON_Geometry_BoundingBox(ptr, ref rc);
        return rc;
      }
    }
    /// <summary>
    /// Aligned Boundingbox solver. Gets the world axis aligned boundingbox for the transformed geometry.
    /// </summary>
    /// <param name="xform">Transformation to apply to object prior to the BoundingBox computation. 
    /// The geometry itself is not modified.</param>
    /// <returns>The accurate boundingbox of the transformed geometry in world coordinates 
    /// or BoundingBox.Empty if not bounding box could be found.</returns>
    [ConstOperation]
    public virtual BoundingBox GetBoundingBox(Transform xform)
    {
      BoundingBox bbox = BoundingBox.Empty;

#if RHINO_SDK
      // In cases like breps and curves, the CRhinoBrepObject and CRhinoCurveObject
      // can compute a better tight bounding box
      RhinoObject parent_object = ParentRhinoObject();
      if (parent_object != null)
      {
        IntPtr ptr_parent = parent_object.ConstPointer();
        if (UnsafeNativeMethods.CRhinoObject_GetTightBoundingBox(ptr_parent, ref bbox, ref xform, true))
          return bbox;
      }
#endif
      IntPtr ptr = ConstPointer();
      return UnsafeNativeMethods.ON_Geometry_GetTightBoundingBox(ptr, ref bbox, ref xform, true) ? bbox : BoundingBox.Empty;
    }
    /// <summary>
    /// Aligned Boundingbox solver. Gets the plane aligned boundingbox.
    /// </summary>
    /// <param name="plane">Orientation plane for BoundingBox.</param>
    /// <returns>A BoundingBox in plane coordinates.</returns>
    /// <example>
    /// <code source='examples\vbnet\ex_curveboundingbox.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_curveboundingbox.cs' lang='cs'/>
    /// <code source='examples\py\ex_curveboundingbox.py' lang='py'/>
    /// </example>
    [ConstOperation]
    public BoundingBox GetBoundingBox(Plane plane)
    {
      if (!plane.IsValid) { return BoundingBox.Unset; }

      Transform xform = Geometry.Transform.ChangeBasis(Plane.WorldXY, plane);
      BoundingBox rc = GetBoundingBox(xform);
      return rc;
    }
    /// <summary>
    /// Aligned Boundingbox solver. Gets the plane aligned boundingbox.
    /// </summary>
    /// <param name="plane">Orientation plane for BoundingBox.</param>
    /// <param name="worldBox">Aligned box in World coordinates.</param>
    /// <returns>A BoundingBox in plane coordinates.</returns>
    [ConstOperation]
    public BoundingBox GetBoundingBox(Plane plane, out Box worldBox)
    {
      worldBox = Box.Unset;

      if (!plane.IsValid) { return BoundingBox.Unset; }

      Transform xform = Geometry.Transform.ChangeBasis(Plane.WorldXY, plane);
      BoundingBox rc = GetBoundingBox(xform);

      worldBox = new Box(plane, rc);
      return rc;
    }

    #region GetBool constants
    const int idxIsDeformable = 0;
    const int idxMakeDeformable = 1;
    internal const int idxIsMorphable = 2;
    const int idxHasBrepForm = 3;
    #endregion

    /// <summary>
    /// true if object can be accurately modified with "squishy" transformations like
    /// projections, shears, and non-uniform scaling.
    /// </summary>
    public bool IsDeformable
    {
      get
      {
        IntPtr ptr = ConstPointer();
        return UnsafeNativeMethods.ON_Geometry_GetBool(ptr, idxIsDeformable);
      }
    }

    /// <summary>
    /// If possible, converts the object into a form that can be accurately modified
    /// with "squishy" transformations like projections, shears, an non-uniform scaling.
    /// </summary>
    /// <returns>
    /// false if object cannot be converted to a deformable object. true if object was
    /// already deformable or was converted into a deformable object.
    /// </returns>
    public bool MakeDeformable()
    {
      IntPtr ptr = NonConstPointer();
      return UnsafeNativeMethods.ON_Geometry_GetBool(ptr, idxMakeDeformable);
    }

    // [skipping] BOOL SwapCoordinates( int i, int j );

    // Not exposed here
    // virtual bool Morph( const ON_SpaceMorph& morph );
    // virtual bool IsMorphable() const;
    // Moved to SpaceMorph class

    /// <summary>
    /// Returns true if the Brep.TryConvertBrep function will be successful for this object
    /// </summary>
    public bool HasBrepForm
    {
      get
      {
        IntPtr ptr = ConstPointer();
        return UnsafeNativeMethods.ON_Geometry_GetBool(ptr, idxHasBrepForm);
      }
    }

    // Not exposed here
    // ON_Brep* BrepForm( ON_Brep* brep = NULL ) const;
    // Implemented in static Brep.TryConvertBrep function

    /// <summary>
    /// If this piece of geometry is a component in something larger, like a BrepEdge
    /// in a Brep, then this function returns the component index.
    /// </summary>
    /// <returns>
    /// This object's component index.  If this object is not a sub-piece of a larger
    /// geometric entity, then the returned index has 
    /// m_type = ComponentIndex.InvalidType
    /// and m_index = -1.
    /// </returns>
    [ConstOperation]
    public ComponentIndex ComponentIndex()
    {
      ComponentIndex ci = new ComponentIndex();
      IntPtr ptr = ConstPointer();
      UnsafeNativeMethods.ON_Geometry_ComponentIndex(ptr, ref ci);
      return ci;
    }

    // [skipping]
    // bool EvaluatePoint( const class ON_ObjRef& objref, ON_3dPoint& P ) const;

    #region user strings
    /// <summary>
    /// Attach a user string (key,value combination) to this geometry.
    /// </summary>
    /// <param name="key">id used to retrieve this string.</param>
    /// <param name="value">string associated with key.</param>
    /// <returns>true on success.</returns>
    public bool SetUserString(string key, string value)
    {
      return _SetUserString(key, value);
    }
    /// <summary>
    /// Gets user string from this geometry.
    /// </summary>
    /// <param name="key">id used to retrieve the string.</param>
    /// <returns>string associated with the key if successful. null if no key was found.</returns>
    public string GetUserString(string key)
    {
      return _GetUserString(key);
    }

    /// <summary>
    /// Gets the amount of user strings.
    /// </summary>
    public int UserStringCount
    {
      get
      {
        return _UserStringCount;
      }
    }

    /// <summary>
    /// Gets a copy of all (user key string, user value string) pairs attached to this geometry.
    /// </summary>
    /// <returns>A new collection.</returns>
    public System.Collections.Specialized.NameValueCollection GetUserStrings()
    {
      return _GetUserStrings();
    }
    #endregion

    /// <summary>
    /// Overridden in order to destroy local display cache information
    /// </summary>
    /// <param name="disposing"></param>
    protected override void Dispose(bool disposing)
    {
      base.Dispose(disposing);
      DestroyCacheHandle();
    }

    /// <summary>
    /// Destroy cache handle
    /// </summary>
    protected override void NonConstOperation()
    {
      DestroyCacheHandle();
      base.NonConstOperation();
    }

    internal IntPtr NonConstPointer(bool preserveDisplayCache)
    {
      if (preserveDisplayCache)
      {
        IntPtr temp = m_ptr_cache_handle;
        m_ptr_cache_handle = IntPtr.Zero;
        IntPtr rc = NonConstPointer();
        m_ptr_cache_handle = temp;
        return rc;
      }
      return NonConstPointer();
    }

    // We may want a version of NonConstPointer that tells the system not
    // to nuke the display cache. This way we don't nuke the cache when
    // performing operations that really have no effect on the cache
    IntPtr m_ptr_cache_handle = IntPtr.Zero;
    internal IntPtr CacheHandle()
    {
#if RHINO_SDK
      if (IntPtr.Zero == m_ptr_cache_handle)
        m_ptr_cache_handle = UnsafeNativeMethods.CRhinoCacheHandle_New();
#endif
      return m_ptr_cache_handle;
    }

    void DestroyCacheHandle()
    {
#if RHINO_SDK
      if (m_ptr_cache_handle != IntPtr.Zero)
      {
        UnsafeNativeMethods.CRhinoCacheHandle_Delete(m_ptr_cache_handle);
        m_ptr_cache_handle = IntPtr.Zero;
      }
#endif
    }

#if RHINO_SDK
    /// <summary>
    /// Determines if two geometries equal one another, in pure geometrical shape.
    /// This version only compares the geometry itself and does not include any user
    /// data comparisons.
    /// This is a comparison by value: for two identical items it will be true, no matter
    /// where in memory they may be stored.
    /// </summary>
    /// <param name="first">The first geometry</param>
    /// <param name="second">The second geometry</param>
    /// <returns>The indication of equality</returns>
    public static bool GeometryEquals(GeometryBase first, GeometryBase second)
    {
      if (first == null && second == null)
        return true;
      if (first == null || second == null)
        return false;

      IntPtr first_ptr = first.ConstPointer();
      IntPtr second_ptr = second.ConstPointer();

      bool rc = UnsafeNativeMethods.RH_RhinoCompareGeometry(first_ptr, second_ptr);
      Runtime.CommonObject.GcProtect(first, second);
      return rc;
    }
 #endif
  }

  // DO NOT make public
  class UnknownGeometry : GeometryBase
  {
    public UnknownGeometry(IntPtr ptr, object parent, int subobjectIndex)
      : base(ptr, parent, subobjectIndex)
    {
    }
  }
}
