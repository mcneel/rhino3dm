using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Rhino.Runtime;

namespace Rhino.Geometry
{
  /// <summary>
  /// Represents a plane surface.
  /// </summary>
  [Serializable]
  public class PlaneSurface : Surface
  {
    internal PlaneSurface(IntPtr ptr, object parent) 
      : base(ptr, parent)
    { }

    /// <summary>
    /// Constructs a new plane surface.
    /// </summary>
    /// <since>8.0</since>
    public PlaneSurface()
    {
      IntPtr ptr = UnsafeNativeMethods.ON_PlaneSurface_New3();
      ConstructNonConstObject(ptr);
    }

    /// <summary>
    /// Constructs a new plane surface.
    /// </summary>
    /// <param name="plane">The plane.</param>
    /// <since>8.1</since>
    public PlaneSurface(Plane plane)
    {
      IntPtr ptr = UnsafeNativeMethods.ON_PlaneSurface_New2(ref plane);
      ConstructNonConstObject(ptr);
    }

    /// <summary>
    /// Constructs a plane surface with x and y extents.
    /// </summary>
    /// <param name="plane">The plane.</param>
    /// <param name="xExtents">The increasing x interval of the plane that defines the rectangle.
    /// The corresponding evaluation interval domain is set so that it matches the
    /// extents interval.</param>
    /// <param name="yExtents">The increasing y interval of the plane that defines the rectangle.
    /// The corresponding evaluation interval domain is set so that it matches the
    /// extents interval.</param>
    /// <example>
    /// <code source='examples\vbnet\ex_planesurface.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_planesurface.cs' lang='cs'/>
    /// <code source='examples\py\ex_planesurface.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    public PlaneSurface(Plane plane, Interval xExtents, Interval yExtents)
    {
      IntPtr ptr = UnsafeNativeMethods.ON_PlaneSurface_New(ref plane, xExtents, yExtents);
      ConstructNonConstObject(ptr);
    }

    /// <summary>
    /// Protected constructor for internal use.
    /// </summary>
    /// <param name="info">Serialization data.</param>
    /// <param name="context">Serialization stream.</param>
    protected PlaneSurface(SerializationInfo info, StreamingContext context)
      : base (info, context)
    {
    }

    internal override GeometryBase DuplicateShallowHelper()
    {
      return new PlaneSurface(IntPtr.Zero, null);
    }

    /// <summary>
    /// Gets or sets the plane surface's plane.
    /// </summary>
    /// <since>8.0</since>
    public Plane Plane
    {
      get
      {
        IntPtr ptr_const_this = ConstPointer();
        Plane plane = new Plane();
        UnsafeNativeMethods.ON_PlaneSurface_GetPlane(ptr_const_this, ref plane);
        return plane;
      }
      set
      {
        IntPtr ptr_this = NonConstPointer();
        UnsafeNativeMethods.ON_PlaneSurface_SetPlane(ptr_this, ref value);
      }
    }

    /// <summary>
    /// Gets the extents of the plane surface.
    /// </summary>
    /// <param name="direction">
    /// The direction, where 0 gets plane surface's x coordinate extents
    /// and 1 gets plane surface's y coordinate extents.
    /// </param>
    /// <returns>An increasing interval.</returns>
    /// <since>8.0</since>
    public Interval GetExtents(int direction)
    {
      IntPtr ptr_const_this = ConstPointer();
      Interval extents = new Interval();
      UnsafeNativeMethods.ON_PlaneSurface_GetExtents(ptr_const_this, direction, ref extents);
      return extents;
    }

    /// <summary>
    /// Sets the extents of the plane surface.
    /// </summary>
    /// <param name="direction">
    /// The direction, where 0 sets plane surface's x coordinate extents
    /// and 1 sets plane surface's y coordinate extents.
    /// </param>
    /// <param name="extents">An increasing interval.</param>
    /// <param name="syncDomain">
    /// If true, the corresponding evaluation interval domain is set so that it matches the extents interval.
    /// If false, the corresponding evaluation interval domain is not changed.
    /// </param>
    /// <since>8.0</since>
    public void SetExtents(int direction, Interval extents, bool syncDomain)
    {
      IntPtr ptr_this = NonConstPointer();
      UnsafeNativeMethods.ON_PlaneSurface_SetExtents(ptr_this, direction, extents, syncDomain);
    }


    /// <summary>
    /// Computes a polygon mesh of the surface made of one quad.
    /// </summary>
    /// <returns>A polygon mesh of the surface.</returns>
    /// <since>8.0</since>
    public Mesh ToMesh()
    {
      IntPtr ptr_const_this = ConstPointer();
      IntPtr ptr_mesh = UnsafeNativeMethods.ON_PlaneSurface_CreateMesh(ptr_const_this);
      if (IntPtr.Zero != ptr_mesh)
        return new Mesh(ptr_mesh, null);
      return null;
    }

    /// <summary>
    /// Create a plane that contains the intersection of a bounding box.
    /// </summary>
    /// <param name="lineInPlane">A line that will lie on the plane.</param>
    /// <param name="vectorInPlane">A vector the direction of which will be in plane.</param>
    /// <param name="box">A box to cut through.</param>
    /// <returns>A new plane surface on success, or null on error.</returns>
    /// <since>5.0</since>
    public static PlaneSurface CreateThroughBox(Line lineInPlane, Vector3d vectorInPlane, BoundingBox box)
    {
      IntPtr ptr = UnsafeNativeMethods.ON_PlaneSurface_CreatePlaneThroughBox(ref lineInPlane, vectorInPlane, ref box);
      if (IntPtr.Zero == ptr)
        return null;
      return new PlaneSurface(ptr, null);
    }

    /// <summary>
    /// Create a plane that contains the intersection of a bounding box.
    /// </summary>
    /// <param name="plane">An original plane value.</param>
    /// <param name="box">A box to use for extension boundary.</param>
    /// <returns>A new plane surface on success, or null on error.</returns>
    /// <example>
    /// <code source='examples\vbnet\ex_splitbrepwithplane.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_splitbrepwithplane.cs' lang='cs'/>
    /// <code source='examples\py\ex_splitbrepwithplane.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    public static PlaneSurface CreateThroughBox(Plane plane, BoundingBox box)
    {
      IntPtr ptr = UnsafeNativeMethods.ON_PlaneSurface_CreatePlaneThroughBox2(ref plane, ref box);
      if (IntPtr.Zero == ptr)
        return null;
      return new PlaneSurface(ptr, null);
    }
  }

  /// <summary>
  /// Represents a planar surface that is used as clipping plane in viewports.
  /// A clipping plane object maintains a list of viewports that it clips against.
  /// </summary>
  [Serializable]
  public class ClippingPlaneSurface : PlaneSurface
  {
    /// <summary>
    /// Constructs an empty clipping plane surface
    /// </summary>
    public ClippingPlaneSurface()
    {
      IntPtr ptr = UnsafeNativeMethods.ON_ClippingPlaneSurface_New();
      ConstructNonConstObject(ptr);
    }
    /// <summary>
    /// Constructs a clipping plane surface from a Plane
    /// </summary>
    /// <param name="plane"></param>
    public ClippingPlaneSurface(Plane plane)
    {
      IntPtr ptr = UnsafeNativeMethods.ON_ClippingPlaneSurface_New_FromPlane(ref plane);
      ConstructNonConstObject(ptr);
    }
    /// <summary>
    /// Constructs a ClippingPlaneSurface from a PlaneSurface
    /// </summary>
    /// <param name="planeSurface"></param>
    public ClippingPlaneSurface(PlaneSurface planeSurface)
    {
      IntPtr ptr = UnsafeNativeMethods.ON_ClippingPlaneSurface_New_FromPLaneSurface(planeSurface.ConstPointer());
      ConstructNonConstObject(ptr);
    }
    internal ClippingPlaneSurface(IntPtr ptr, object parent)
      : base(ptr, parent)
    { }

    /// <summary>
    /// Protected constructor for internal use.
    /// </summary>
    /// <param name="info">Serialization data.</param>
    /// <param name="context">Serialization stream.</param>
    protected ClippingPlaneSurface(SerializationInfo info, StreamingContext context)
      : base (info, context)
    {
    }

    internal override GeometryBase DuplicateShallowHelper()
    {
      return new ClippingPlaneSurface(IntPtr.Zero, null);
    }

    /// <summary>
    /// Distance that the clipping has an effect
    /// </summary>
    /// <since>8.0</since>
    public double PlaneDepth
    {
      get
      {
        IntPtr pConstThis = ConstPointer();
        return UnsafeNativeMethods.ON_ClippingPlaneSurface_GetDepth(pConstThis);
      }
      set
      {
        IntPtr pThis = NonConstPointer();
        UnsafeNativeMethods.ON_ClippingPlaneSurface_SetDepth(pThis, value);
      }
    }

    /// <summary>
    /// Determines if the PlaneDepth value should be used
    /// </summary>
    /// <since>8.0</since>
    public bool PlaneDepthEnabled
    {
      get
      {
        IntPtr pConstThis = ConstPointer();
        return UnsafeNativeMethods.ON_ClippingPlaneSurface_GetDepthEnabled(pConstThis);
      }
      set
      {
        IntPtr pThis = NonConstPointer();
        UnsafeNativeMethods.ON_ClippingPlaneSurface_SetDepthEnabled(pThis, value);
      }
    }

    /// <summary>
    /// Returns the ids of RhinoViewport objects that are clipped by this clipping plane.
    /// </summary>
    /// <returns>The ids of RhinoViewport objects.</returns>
    /// <since>5.0</since>
    [ConstOperation]
    public Guid[] ViewportIds()
    {
      IntPtr ptr_const_this = ConstPointer();
      int count = UnsafeNativeMethods.ON_ClippingPlaneSurface_ViewportIdCount(ptr_const_this);
      Guid[] rc = new Guid[count];
      for (int i = 0; i < count; i++)
        rc[i] = UnsafeNativeMethods.ON_ClippingPlaneSurface_ViewportId(ptr_const_this, i);
      return rc;
    }

    /// <summary>
    /// Adds a viewport id to the list of viewports that this clipping plane clips.
    /// </summary>
    /// <param name="viewportId">The id of the RhinoViewport to add.</param>
    /// <returns>true if the viewport was added, false if the viewport is already in the list.</returns>
    /// <since>6.1</since>
    public bool AddClipViewportId(Guid viewportId)
    {
      IntPtr ptr_this = NonConstPointer();
      return UnsafeNativeMethods.ON_ClippingPlaneSurface_AddClipViewport(ptr_this, viewportId);
    }

    /// <summary>
    /// Removes a viewport id that this being clipped by this clipping plane.
    /// </summary>
    /// <param name="viewportId">The id of the RhinoViewport to remove.</param>
    /// <returns>true if the viewport was removed, false if the viewport was not in the list.</returns>
    /// <since>6.1</since>
    public bool RemoveClipViewportId(Guid viewportId)
    {
      IntPtr ptr_this = NonConstPointer();
      return UnsafeNativeMethods.ON_ClippingPlaneSurface_RemoveClipViewport(ptr_this, viewportId);
    }

    /// <summary>
    /// Should the object and layer participation lists be used when determining clipping
    /// </summary>
    /// <since>8.0</since>
    public bool ParticipationListsEnabled
    {
      get
      {
        IntPtr ptr_const_this = ConstPointer();
        return UnsafeNativeMethods.ON_ClippingPlaneSurce_ParticipationEnabled(ptr_const_this);
      }
      set
      {
        IntPtr ptr_this = NonConstPointer();
        UnsafeNativeMethods.ON_ClippingPlaneSurface_SetParticipationEnabled(ptr_this, value);
      }
    }

    /// <summary>
    /// Set a list of specific object ids and layers that this clipping plane surface clips.
    /// </summary>
    /// <param name="objectIds"></param>
    /// <param name="layerIndices"></param>
    /// <param name="isExclusionList">Is the list a set of ids to not clip or a set to clip</param>
    /// <since>8.0</since>
    public void SetClipParticipation(IEnumerable<Guid> objectIds, IEnumerable<int> layerIndices, bool isExclusionList)
    {
      IntPtr ptr_this = NonConstPointer();
      using(var idlist = new Rhino.Runtime.InteropWrappers.SimpleArrayGuid(objectIds))
      using(var layerlist = new Rhino.Runtime.InteropWrappers.SimpleArrayInt(layerIndices))
      {
        IntPtr idListPtr = idlist.ConstPointer();
        IntPtr layerListPtr = layerlist.ConstPointer();
        UnsafeNativeMethods.ON_ClippingPlaneSurface_SetClipList(ptr_this, idListPtr, layerListPtr, isExclusionList);
      }
    }

    /// <summary>
    /// </summary>
    /// <param name="objectIds"></param>
    /// <param name="layerIndices"></param>
    /// <param name="isExclusionList"></param>
    public void GetClipParticipation(out IEnumerable<Guid> objectIds, out IEnumerable<int> layerIndices, out bool isExclusionList)
    {
      objectIds = null;
      layerIndices = null;
      isExclusionList = true;
      using (var idlist = new Rhino.Runtime.InteropWrappers.SimpleArrayGuid(objectIds))
      using (var layerlist = new Rhino.Runtime.InteropWrappers.SimpleArrayInt(layerIndices))
      {
        IntPtr constPtrThis = ConstPointer();
        IntPtr idListPtr = idlist.NonConstPointer();
        IntPtr layerListPtr = layerlist.NonConstPointer();
        UnsafeNativeMethods.ON_ClippingPlaneSurface_GetClipList(constPtrThis, idListPtr, layerListPtr, ref isExclusionList);
        objectIds = idlist.ToArray();
        layerIndices = layerlist.ToArray();
      }
    }

    /// <summary>
    /// Remove list of object ids that this clipping plane surface clips. This causes the clipping
    /// plane surface to clip all objects
    /// </summary>
    /// <since>8.0</since>
    public void ClearClipParticipationLists()
    {
      IntPtr ptr_this = NonConstPointer();
      UnsafeNativeMethods.ON_ClippingPlaneSurface_ClearParticipationLists(ptr_this);
    }
  }
}
