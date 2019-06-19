
#if RHINO_SDK
namespace Rhino.DocObjects
{
  /// <summary>
  /// Represents the object of a <see cref="Rhino.Geometry.ClippingPlaneSurface">clipping plane</see>,
  /// stored in the Rhino document and with attributes.
  /// </summary>
  public class ClippingPlaneObject : RhinoObject
  {
    internal ClippingPlaneObject(uint serialNumber)
      : base(serialNumber)
    { }

    internal override CommitGeometryChangesFunc GetCommitFunc()
    {
      return UnsafeNativeMethods.CRhinoClippingPlaneObject_InternalCommitChanges;
    }

    /// <summary>
    /// Gets the clipping plane surface.
    /// </summary>
    public Rhino.Geometry.ClippingPlaneSurface ClippingPlaneGeometry
    {
      get
      {
        Rhino.Geometry.ClippingPlaneSurface rc = Geometry as Rhino.Geometry.ClippingPlaneSurface;
        return rc;
      }
    }

    /// <summary>
    /// Adds a viewport to the list of viewports that this clipping plane clips.
    /// </summary>
    /// <param name="viewport">The viewport to add.</param>
    /// <param name="commit">Commit the change. When in doubt, set this parameter to true.</param>
    /// <returns>true if the viewport was added, false if the viewport is already in the list.</returns>
    public bool AddClipViewport(Rhino.Display.RhinoViewport viewport, bool commit)
    {
      if (null == viewport)
        throw new System.ArgumentNullException(nameof(viewport));

      bool rc = false;
      Rhino.Geometry.ClippingPlaneSurface geometry = ClippingPlaneGeometry;
      if (null != geometry)
      {
        rc = geometry.AddClipViewportId(viewport.Id);
        if (rc && commit)
          CommitChanges();
      }
      return rc;
    }

    /// <summary>
    /// Removes a viewport from the list of viewports that this clipping plane clips.
    /// </summary>
    /// <param name="viewport">The viewport to remove.</param>
    /// <param name="commit">Commit the change. When in doubt, set this parameter to true.</param>
    /// <returns>true if the viewport was removed, false if the viewport was not in the list.</returns>
    public bool RemoveClipViewport(Rhino.Display.RhinoViewport viewport, bool commit)
    {
      if (null == viewport)
        throw new System.ArgumentNullException(nameof(viewport));

      bool rc = false;
      Rhino.Geometry.ClippingPlaneSurface geometry = ClippingPlaneGeometry;
      if (null != geometry)
      {
        rc = geometry.RemoveClipViewportId(viewport.Id);
        if (rc && commit)
          CommitChanges();
      }
      return rc;
    }

  }
}
#endif