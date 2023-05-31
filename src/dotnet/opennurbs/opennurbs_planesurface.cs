using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Rhino.Runtime;

namespace Rhino.Geometry
{
  /// <summary>
  /// Represents a plane surface, with plane and two intervals.
  /// </summary>
  [Serializable]
  public class PlaneSurface : Surface
  {
    internal PlaneSurface(IntPtr ptr, object parent) 
      : base(ptr, parent)
    { }

    /// <summary>
    /// Initializes a plane surface with x and y intervals.
    /// </summary>
    /// <param name="plane">The plane.</param>
    /// <param name="xExtents">The x interval of the plane that defines the rectangle.
    /// The corresponding evaluation interval domain is set so that it matches the
    /// extents interval.</param>
    /// <param name="yExtents">The y interval of the plane that defines the rectangle.
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

#if RHINO_SDK
    /// <summary>
    /// Makes a plane that includes a line and a vector and goes through a bounding box.
    /// </summary>
    /// <param name="lineInPlane">A line that will lie on the plane.</param>
    /// <param name="vectorInPlane">A vector the direction of which will be in plane.</param>
    /// <param name="box">A box to cut through.</param>
    /// <returns>A new plane surface on success, or null on error.</returns>
    /// <since>5.0</since>
    public static PlaneSurface CreateThroughBox(Line lineInPlane, Vector3d vectorInPlane, BoundingBox box)
    {
      IntPtr ptr = UnsafeNativeMethods.RHC_RhinoPlaneThroughBox(ref lineInPlane, vectorInPlane, ref box);
      if (IntPtr.Zero == ptr)
        return null;
      return new PlaneSurface(ptr, null);
    }

    /// <summary>
    /// Extends a plane into a plane surface so that the latter goes through a bounding box.
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
      IntPtr ptr = UnsafeNativeMethods.RHC_RhinoPlaneThroughBox2(ref plane, ref box);
      if (IntPtr.Zero == ptr)
        return null;
      return new PlaneSurface(ptr, null);
    }
#endif
  }

  /// <summary>
  /// Represents a planar surface that is used as clipping plane in viewports.
  /// A clipping plane object maintains a list of viewports that it clips against.
  /// </summary>
  [Serializable]
  public class ClippingPlaneSurface : PlaneSurface
  {
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
    /// Gets or sets the clipping plane.
    /// </summary>
    /// <since>5.0</since>
    public Plane Plane
    {
      get
      {
        IntPtr pConstThis = ConstPointer();
        Plane p = new Plane();
        UnsafeNativeMethods.ON_ClippingPlaneSurface_GetPlane(pConstThis, ref p);
        return p;
      }
      set
      {
        IntPtr pThis = NonConstPointer();
        UnsafeNativeMethods.ON_ClippingPlaneSurface_SetPlane(pThis, ref value);
      }
    }

    /// <summary>
    /// Distance that the clipping has an effect
    /// </summary>
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
    public void ClearClipParticipationLists()
    {
      IntPtr ptr_this = NonConstPointer();
      UnsafeNativeMethods.ON_ClippingPlaneSurface_ClearParticipationLists(ptr_this);
    }
  }
}
