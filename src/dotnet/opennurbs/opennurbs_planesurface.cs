using System;
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
  }
}
