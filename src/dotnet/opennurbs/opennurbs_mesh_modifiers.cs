using System;
using Rhino.DocObjects;
using Rhino.Runtime.InteropWrappers;

namespace Rhino.FileIO
{
  /// <summary>
  /// Represents the displacement attached to file3dm object attributes.
  /// </summary>
  public class File3dmDisplacement
  {
    private readonly ObjectAttributes m_attr;

    internal File3dmDisplacement(ObjectAttributes attr)
    {
      m_attr = attr;
    }

    /// <summary>
    /// Specifies whether the displacement feature is enabled or not.
    /// </summary>
    public bool On
    {
      get { return UnsafeNativeMethods.ON_3dmObjectAttributes_Displacement_GetOn(m_attr.ConstPointer()); }
      set {        UnsafeNativeMethods.ON_3dmObjectAttributes_Displacement_SetOn(m_attr.NonConstPointer(), value); }
    }

    /// <summary>
    /// Specifies which texture is used for computing the displacement amount.
    /// </summary>
    public Guid TextureId
    {
      get { return UnsafeNativeMethods.ON_3dmObjectAttributes_Displacement_GetTextureId(m_attr.ConstPointer()); }
      set {        UnsafeNativeMethods.ON_3dmObjectAttributes_Displacement_SetTextureId(m_attr.NonConstPointer(), value); }
    }

    /// <summary>
    /// Specifies which texture mapping channel is used for the displacement texture.
    /// </summary>
    public int MappingChannel
    {
      get { return UnsafeNativeMethods.ON_3dmObjectAttributes_Displacement_GetMappingChannel(m_attr.ConstPointer()); }
      set {        UnsafeNativeMethods.ON_3dmObjectAttributes_Displacement_SetMappingChannel(m_attr.NonConstPointer(), value); }
    }

    /// <summary>
    /// Specifies the amount of displacement for the black color in the texture.
    /// </summary>
    public double BlackPoint
    {
      get { return UnsafeNativeMethods.ON_3dmObjectAttributes_Displacement_GetBlackPoint(m_attr.ConstPointer()); }
      set {        UnsafeNativeMethods.ON_3dmObjectAttributes_Displacement_SetBlackPoint(m_attr.NonConstPointer(), value); }
    }

    /// <summary>
    /// Specifies the amount of displacement for the white color in the texture.
    /// </summary>
    public double WhitePoint
    {
      get { return UnsafeNativeMethods.ON_3dmObjectAttributes_Displacement_GetWhitePoint(m_attr.ConstPointer()); }
      set {        UnsafeNativeMethods.ON_3dmObjectAttributes_Displacement_SetWhitePoint(m_attr.NonConstPointer(), value); }
    }

    /// <summary>
    /// Specifies how densely the object is initially subdivided.
    /// The lower the value, the higher the resolution of the displaced mesh.
    /// </summary>
    public int InitialQuality
    {
      get { return UnsafeNativeMethods.ON_3dmObjectAttributes_Displacement_GetInitialQuality(m_attr.ConstPointer()); }
      set {        UnsafeNativeMethods.ON_3dmObjectAttributes_Displacement_SetInitialQuality(m_attr.NonConstPointer(), value); }
    }

    /// <summary>
    /// Specifies whether to perform a mesh reduction as a post process to simplify the result of displacement.
    /// </summary>
    public bool FinalMaxFacesOn
    {
      get { return UnsafeNativeMethods.ON_3dmObjectAttributes_Displacement_GetFinalMaxFacesOn(m_attr.ConstPointer()); }
      set {        UnsafeNativeMethods.ON_3dmObjectAttributes_Displacement_SetFinalMaxFacesOn(m_attr.NonConstPointer(), value); }
    }

    /// <summary>
    /// Specifies how many faces the reduction post process should target.
    /// </summary>
    public int FinalMaxFaces
    {
      get { return UnsafeNativeMethods.ON_3dmObjectAttributes_Displacement_GetFinalMaxFaces(m_attr.ConstPointer()); }
      set {        UnsafeNativeMethods.ON_3dmObjectAttributes_Displacement_SetFinalMaxFaces(m_attr.NonConstPointer(), value); }
    }

    /// <summary>
    /// Specifies whether or not to perform a fairing step. Fairing straightens rough feature edges.
    /// </summary>
    public bool FairingOn
    {
      get { return UnsafeNativeMethods.ON_3dmObjectAttributes_Displacement_GetFairingOn(m_attr.ConstPointer()); }
      set {        UnsafeNativeMethods.ON_3dmObjectAttributes_Displacement_SetFairingOn(m_attr.NonConstPointer(), value); }
    }

    /// <summary>
    /// Specifies the number of steps for the fairing process. Fairing straightens rough feature edges.
    /// </summary>
    public int Fairing
    {
      get { return UnsafeNativeMethods.ON_3dmObjectAttributes_Displacement_GetFairing(m_attr.ConstPointer()); }
      set {        UnsafeNativeMethods.ON_3dmObjectAttributes_Displacement_SetFairing(m_attr.NonConstPointer(), value); }
    }

    /// <summary>
    ///  Specifies the maximum angle between face normals of adjacent faces that will get welded together.
    /// </summary>
    public double PostWeldAngle
    {
      get { return UnsafeNativeMethods.ON_3dmObjectAttributes_Displacement_GetPostWeldAngle(m_attr.ConstPointer()); }
      set {        UnsafeNativeMethods.ON_3dmObjectAttributes_Displacement_SetPostWeldAngle(m_attr.NonConstPointer(), value); }
    }

    /// <summary>
    /// Specifies in megabytes how much memory can be allocated for use by the displacement mesh.
    /// </summary>
    public int MeshMemoryLimit
    {
      get { return UnsafeNativeMethods.ON_3dmObjectAttributes_Displacement_GetMeshMemoryLimit(m_attr.ConstPointer()); }
      set {        UnsafeNativeMethods.ON_3dmObjectAttributes_Displacement_SetMeshMemoryLimit(m_attr.NonConstPointer(), value); }
    }

    /// <summary>
    /// Specifies the number of refinement passes
    /// </summary>
    public int RefineSteps
    {
      get { return UnsafeNativeMethods.ON_3dmObjectAttributes_Displacement_GetRefineSteps(m_attr.ConstPointer()); }
      set {        UnsafeNativeMethods.ON_3dmObjectAttributes_Displacement_SetRefineSteps(m_attr.NonConstPointer(), value); }
    }

    /// <summary>
    /// Specifies how sensitive the divider for contrasts is on the displacement texture.
    /// Specify 1 to split all mesh edges on each refine step.
    /// Specify 0.99 to make even slight contrasts on the displacement texture cause edges to be split.
    /// Specifying 0.01 only splits edges where heavy contrast exists.
    /// </summary>
    public double RefineSensitivity
    {
      get { return UnsafeNativeMethods.ON_3dmObjectAttributes_Displacement_GetRefineSensitivity(m_attr.ConstPointer()); }
      set {        UnsafeNativeMethods.ON_3dmObjectAttributes_Displacement_SetRefineSensitivity(m_attr.NonConstPointer(), value); }
    }

    /// <summary>
    /// Formula to use to calculate sweep resolution from initial quality.
    /// </summary>
    public enum SweepResolutionFormulas
    {
      /// <summary>
      /// Default formula.
      /// </summary>
      Default = 0,
      /// <summary>
      /// Formula used in Rhino 5. Dependent on absolute tolerance.
      /// </summary>
      AbsoluteToleranceDependent = 1
    };

    /// <summary>
    /// Specifies which formula is used to calculate sweep resolution from initial quality.
    /// </summary>
    public SweepResolutionFormulas SweepResolutionFormula
    {
      get { return (SweepResolutionFormulas)UnsafeNativeMethods.ON_3dmObjectAttributes_Displacement_GetSweepResolutionFormula(m_attr.ConstPointer()); }
      set {                                 UnsafeNativeMethods.ON_3dmObjectAttributes_Displacement_SetSweepResolutionFormula(m_attr.NonConstPointer(), (int)value); }
    }

    /// <summary>
    /// Gets all the sub-items on the displacement. Sub-items can exist to override the top-level parameters
    /// for polysurface/SubD faces. Sub-items are identified by the face index that they apply to.
    /// <return>An array of the face indexes of each sub-item on the displacement.</return>
    /// </summary>
    public int[] GetSubItemFaceIndexes()
    {
      using (var list = new SimpleArrayInt())
      {
        UnsafeNativeMethods.ON_3dmObjectAttributes_Displacement_GetSubItems(m_attr.ConstPointer(), list.NonConstPointer());
        return list.ToArray();
      }
    }

    /// <summary>
    /// Adds a new sub-item to the displacement.
    /// <param>face_index is the index of the face on the polysurface/SubD.</param>
    /// <param>on is the override for File3dmDisplacement.On.</param>
    /// <param>texture is the override for File3dmDisplacement.TextureId.</param>
    /// <param>mapping_channel is the override for File3dmDisplacement.MappingChannel.</param>
    /// <param>black_point is the override for File3dmDisplacement.BlackPoint.</param>
    /// <param>white_point is the override for File3dmDisplacement.WhitePoint.</param>
    /// </summary>
    public bool AddSubItem(int face_index, bool on, Guid texture, int mapping_channel, double black_point, double white_point)
    {
      return UnsafeNativeMethods.ON_3dmObjectAttributes_Displacement_AddSubItem(m_attr.ConstPointer(),
                                 face_index, on, texture, mapping_channel, black_point, white_point);
    }

    /// <summary>
    /// Deletes a sub-item by its face index.
    /// <param>face_index is the index of the face on the polysurface/SubD.</param>
    /// </summary>
    public void DeleteSubItem(int face_index)
    {
      UnsafeNativeMethods.ON_3dmObjectAttributes_Displacement_DeleteSubItem(m_attr.ConstPointer(), face_index);
    }

    /// <summary>
    /// Deletes all the sub-items from the displacement.
    /// </summary>
    public void DeleteAllSubItems()
    {
      UnsafeNativeMethods.ON_3dmObjectAttributes_Displacement_DeleteAllSubItems(m_attr.ConstPointer());
    }

    /// <summary>
    /// Gets the override for displacement 'on'.
    /// <param>face_index is the index of the face on the polysurface/SubD.</param>
    /// </summary>
    public bool SubItemOn(int face_index)
    {
      return UnsafeNativeMethods.ON_3dmObjectAttributes_Displacement_GetSubItemOn(m_attr.ConstPointer(), face_index);
    }

    /// <summary>
    /// Sets the override for displacement 'on'.
    /// <param>face_index is the index of the face on the polysurface/SubD.</param>
    /// </summary>
    public void SetSubItemOn(int face_index, bool on)
    {
      UnsafeNativeMethods.ON_3dmObjectAttributes_Displacement_SetSubItemOn(m_attr.NonConstPointer(), face_index, on);
    }

    /// <summary>
    /// Gets the override for displacement 'texture'.
    /// <param>face_index is the index of the face on the polysurface/SubD.</param>
    /// </summary>
    public Guid SubItemTexture(int face_index)
    {
      return UnsafeNativeMethods.ON_3dmObjectAttributes_Displacement_GetSubItemTexture(m_attr.ConstPointer(), face_index);
    }

    /// <summary>
    /// Sets the override for displacement 'texture'.
    /// <param>face_index is the index of the face on the polysurface/SubD.</param>
    /// </summary>
    public void SetSubItemTexture(int face_index, Guid texture_id)
    {
      UnsafeNativeMethods.ON_3dmObjectAttributes_Displacement_SetSubItemTexture(m_attr.NonConstPointer(), face_index, texture_id);
    }

    /// <summary>
    /// Gets the override for displacement 'mapping channel'.
    /// <param>face_index is the index of the face on the polysurface/SubD.</param>
    /// </summary>
    public int SubItemMappingChannel(int face_index)
    {
      return UnsafeNativeMethods.ON_3dmObjectAttributes_Displacement_GetSubItemMappingChannel(m_attr.ConstPointer(), face_index);
    }

    /// <summary>
    /// Sets the override for displacement 'mapping channel'.
    /// <param>face_index is the index of the face on the polysurface/SubD.</param>
    /// </summary>
    public void SetSubItemMappingChannel(int face_index, int chan)
    {
      UnsafeNativeMethods.ON_3dmObjectAttributes_Displacement_SetSubItemMappingChannel(m_attr.NonConstPointer(), face_index, chan);
    }

    /// <summary>
    /// Gets the override for displacement 'black-point'.
    /// <param>face_index is the index of the face on the polysurface/SubD.</param>
    /// </summary>
    public double SubItemBlackPoint(int face_index)
    {
      return UnsafeNativeMethods.ON_3dmObjectAttributes_Displacement_GetSubItemBlackPoint(m_attr.ConstPointer(), face_index);
    }

    /// <summary>
    /// Sets the override for displacement 'black-point'.
    /// <param>face_index is the index of the face on the polysurface/SubD.</param>
    /// </summary>
    public void SetSubItemBlackPoint(int face_index, double black_point)
    {
      UnsafeNativeMethods.ON_3dmObjectAttributes_Displacement_SetSubItemBlackPoint(m_attr.NonConstPointer(), face_index, black_point);
    }

    /// <summary>
    /// Gets the override for displacement 'white-point'.
    /// <param>face_index is the index of the face on the polysurface/SubD.</param>
    /// </summary>
    public double SubItemWhitePoint(int face_index)
    {
      return UnsafeNativeMethods.ON_3dmObjectAttributes_Displacement_GetSubItemWhitePoint(m_attr.ConstPointer(), face_index);
    }

    /// <summary>
    /// Sets the override for displacement 'white-point'.
    /// <param>face_index is the index of the face on the polysurface/SubD.</param>
    /// </summary>
    public void SetSubItemWhitePoint(int face_index, double white_point)
    {
      UnsafeNativeMethods.ON_3dmObjectAttributes_Displacement_SetSubItemWhitePoint(m_attr.NonConstPointer(), face_index, white_point);
    }
  }

  /// <summary>
  /// Represents the edge softening attached to file3dm object attributes.
  /// </summary>
  public class File3dmEdgeSoftening
  {
    private readonly ObjectAttributes m_attr;

    internal File3dmEdgeSoftening(ObjectAttributes attr)
    {
      m_attr = attr;
    }

    /// <summary>
    /// Specifies whether edge softening is enabled or not.
    /// </summary>
    public bool On
    {
      get { return UnsafeNativeMethods.ON_3dmObjectAttributes_EdgeSoftening_GetOn(m_attr.ConstPointer()); }
      set {        UnsafeNativeMethods.ON_3dmObjectAttributes_EdgeSoftening_SetOn(m_attr.NonConstPointer(), value); }
    }

    /// <summary>
    /// The softening radius.
    /// </summary>
    public double Softening
    {
      get { return UnsafeNativeMethods.ON_3dmObjectAttributes_EdgeSoftening_GetSoftening(m_attr.ConstPointer()); }
      set {        UnsafeNativeMethods.ON_3dmObjectAttributes_EdgeSoftening_SetSoftening(m_attr.NonConstPointer(), value); }
    }

    /// <summary>
    /// Specifies whether to chamfer the edges.
    /// </summary>
    public bool Chamfer
    {
      get { return UnsafeNativeMethods.ON_3dmObjectAttributes_EdgeSoftening_GetChamfer(m_attr.ConstPointer()); }
      set {        UnsafeNativeMethods.ON_3dmObjectAttributes_EdgeSoftening_SetChamfer(m_attr.NonConstPointer(), value); }
    }

    /// <summary>
    /// Specifies whether the edges are faceted.
    /// </summary>
    public bool Faceted
    {
      get { return UnsafeNativeMethods.ON_3dmObjectAttributes_EdgeSoftening_GetFaceted(m_attr.ConstPointer()); }
      set {        UnsafeNativeMethods.ON_3dmObjectAttributes_EdgeSoftening_SetFaceted(m_attr.NonConstPointer(), value); }
    }

    /// <summary>
    /// Threshold angle (in degrees) which controls whether an edge is softened or not.
    /// The angle refers to the angles between the adjacent faces of an edge.
    /// </summary>
    public double EdgeAngleThreshold
    {
      get { return UnsafeNativeMethods.ON_3dmObjectAttributes_EdgeSoftening_GetEdgeAngleThreshold(m_attr.ConstPointer()); }
      set {        UnsafeNativeMethods.ON_3dmObjectAttributes_EdgeSoftening_SetEdgeAngleThreshold(m_attr.NonConstPointer(), value); }
    }

    /// <summary>
    /// Specifies whether to soften edges despite too large a radius.
    /// </summary>
    public bool ForceSoftening
    {
      get { return UnsafeNativeMethods.ON_3dmObjectAttributes_EdgeSoftening_GetForceSoftening(m_attr.ConstPointer()); }
      set {        UnsafeNativeMethods.ON_3dmObjectAttributes_EdgeSoftening_SetForceSoftening(m_attr.NonConstPointer(), value); }
    }
  }

  /// <summary>
  /// Represents the thickening attached to file3dm object attributes.
  /// </summary>
  public class File3dmThickening
  {
    private readonly ObjectAttributes m_attr;

    internal File3dmThickening(ObjectAttributes attr)
    {
      m_attr = attr;
    }

    /// <summary>
    /// Specifies whether the feature is enabled or not.
    /// </summary>
    public bool On
    {
      get { return UnsafeNativeMethods.ON_3dmObjectAttributes_Thickening_GetOn(m_attr.ConstPointer()); }
      set {        UnsafeNativeMethods.ON_3dmObjectAttributes_Thickening_SetOn(m_attr.NonConstPointer(), value); }
    }

    /// <summary>
    /// Specifies how thick meshes will be made.
    /// </summary>
    public double Distance
    {
      get { return UnsafeNativeMethods.ON_3dmObjectAttributes_Thickening_GetDistance(m_attr.ConstPointer()); }
      set {        UnsafeNativeMethods.ON_3dmObjectAttributes_Thickening_SetDistance(m_attr.NonConstPointer(), value); }
    }

    /// <summary>
    /// Specifies whether to make open meshes solid by adding walls when thickening.
    /// </summary>
    public bool Solid
    {
      get { return UnsafeNativeMethods.ON_3dmObjectAttributes_Thickening_GetSolid(m_attr.ConstPointer()); }
      set {        UnsafeNativeMethods.ON_3dmObjectAttributes_Thickening_SetSolid(m_attr.NonConstPointer(), value); }
    }

    /// <summary>
    /// Specifies whether to only offset the original surface.
    /// </summary>
    public bool OffsetOnly
    {
      get { return UnsafeNativeMethods.ON_3dmObjectAttributes_Thickening_GetOffsetOnly(m_attr.ConstPointer()); }
      set {        UnsafeNativeMethods.ON_3dmObjectAttributes_Thickening_SetOffsetOnly(m_attr.NonConstPointer(), value); }
    }

    /// <summary>
    /// Specifies whether to thicken to both sides of the surface.
    /// </summary>
    public bool BothSides
    {
      get { return UnsafeNativeMethods.ON_3dmObjectAttributes_Thickening_GetBothSides(m_attr.ConstPointer()); }
      set {        UnsafeNativeMethods.ON_3dmObjectAttributes_Thickening_SetBothSides(m_attr.NonConstPointer(), value); }
    }
  }

  /// <summary>
  /// Represents the curve piping attached to file3dm object attributes.
  /// </summary>
  public class File3dmCurvePiping
  {
    private readonly ObjectAttributes m_attr;

    internal File3dmCurvePiping(ObjectAttributes attr)
    {
      m_attr = attr;
    }

    /// <summary>
    /// Specifies whether curve piping is enabled or not.
    /// </summary>
    public bool On
    {
      get { return UnsafeNativeMethods.ON_3dmObjectAttributes_CurvePiping_GetOn(m_attr.ConstPointer()); }
      set {        UnsafeNativeMethods.ON_3dmObjectAttributes_CurvePiping_SetOn(m_attr.NonConstPointer(), value); }
    }

    /// <summary>
    /// Specifies the radius of the pipe (minimum value 0.0001).
    /// </summary>
    public double Radius
    {
      get { return UnsafeNativeMethods.ON_3dmObjectAttributes_CurvePiping_GetRadius(m_attr.ConstPointer()); }
      set {        UnsafeNativeMethods.ON_3dmObjectAttributes_CurvePiping_SetRadius(m_attr.NonConstPointer(), value); }
    }

    /// <summary>
    /// Specifies the number of segments in the pipe (minimum value 2).
    /// </summary>
    public int Segments
    {
      get { return UnsafeNativeMethods.ON_3dmObjectAttributes_CurvePiping_GetSegments(m_attr.ConstPointer()); }
      set {        UnsafeNativeMethods.ON_3dmObjectAttributes_CurvePiping_SetSegments(m_attr.NonConstPointer(), value); }
    }

    /// <summary>
    /// Specifies whether the pipe is faceted or not.
    /// </summary>
    public bool Faceted
    {
      get { return UnsafeNativeMethods.ON_3dmObjectAttributes_CurvePiping_GetFaceted(m_attr.ConstPointer()); }
      set {        UnsafeNativeMethods.ON_3dmObjectAttributes_CurvePiping_SetFaceted(m_attr.NonConstPointer(), value); }
    }

    /// <summary>
    /// Specifies the accuracy of the pipe in the range 0 to 100.
    /// </summary>
    public int Accuracy
    {
      get { return UnsafeNativeMethods.ON_3dmObjectAttributes_CurvePiping_GetAccuracy(m_attr.ConstPointer()); }
      set {        UnsafeNativeMethods.ON_3dmObjectAttributes_CurvePiping_SetAccuracy(m_attr.NonConstPointer(), value); }
    }

    /// <summary>
    /// Defines how the pipe is capped at the ends.
    /// </summary>
    public enum CapTypes
    {
      /// <summary>
      /// No capping.
      /// </summary>
      None = 0,
      /// <summary>
      /// A flat surface will cap the pipe.
      /// </summary>
      Flat = 1,
      /// <summary>
      /// A simple construction will cap the pipe.
      /// </summary>
      Box  = 2,
      /// <summary>
      /// A meridians-and-parallels hemisphere construction will cap the pipe.
      /// </summary>
      Dome = 3,
    };

    /// <summary>
    /// Specifies the cap type to use.
    /// </summary>
    public CapTypes CapType
    {
      get { return (CapTypes)UnsafeNativeMethods.ON_3dmObjectAttributes_CurvePiping_GetCapType(m_attr.ConstPointer()); }
      set {                  UnsafeNativeMethods.ON_3dmObjectAttributes_CurvePiping_SetCapType(m_attr.NonConstPointer(), (int)value); }
    }
  }

  /// <summary>
  /// Represents the shut-lining attached to file3dm object attributes.
  /// </summary>
  public class File3dmShutLining
  {
    private readonly ObjectAttributes m_attr;

    internal File3dmShutLining(ObjectAttributes attr)
    {
      m_attr = attr;
    }

    /// <summary>
    /// Specifies whether shut-lining is enabled or not.
    /// </summary>
    public bool On
    {
      get { return UnsafeNativeMethods.ON_3dmObjectAttributes_ShutLining_GetOn(m_attr.ConstPointer()); }
      set {        UnsafeNativeMethods.ON_3dmObjectAttributes_ShutLining_SetOn(m_attr.NonConstPointer(), value); }
    }

    /// <summary>
    /// Specifies whether the shut-lining is faceted or not.
    /// </summary>
    public bool Faceted
    {
      get { return UnsafeNativeMethods.ON_3dmObjectAttributes_ShutLining_GetFaceted(m_attr.ConstPointer()); }
      set {        UnsafeNativeMethods.ON_3dmObjectAttributes_ShutLining_SetFaceted(m_attr.NonConstPointer(), value); }
    }

    /// <summary>
    /// Specifies whether the shut-lining automatically updates or not.
    /// </summary>
    public bool AutoUpdate
    {
      get { return UnsafeNativeMethods.ON_3dmObjectAttributes_ShutLining_GetAutoUpdate(m_attr.ConstPointer()); }
      set {        UnsafeNativeMethods.ON_3dmObjectAttributes_ShutLining_SetAutoUpdate(m_attr.NonConstPointer(), value); }
    }

    /// <summary>
    /// Specifies whether updating is forced or not.
    /// </summary>
    public bool ForceUpdate
    {
      get { return UnsafeNativeMethods.ON_3dmObjectAttributes_ShutLining_GetForceUpdate(m_attr.ConstPointer()); }
      set {        UnsafeNativeMethods.ON_3dmObjectAttributes_ShutLining_SetForceUpdate(m_attr.NonConstPointer(), value); }
    }

    /// <summary>
    /// Gets all the curves on the shut-lining. Each curve is identified by a Guid.
    /// If there are no curves present, the array will be empty.
    /// </summary>
    public Guid[] GetCurves()
    {
      using (var list = new SimpleArrayGuid())
      {
        UnsafeNativeMethods.ON_3dmObjectAttributes_ShutLining_GetCurves(m_attr.ConstPointer(), list.NonConstPointer());
        return list.ToArray();
      }
    }

    /// <summary>
    /// Adds a new curve to the shut-lining. The curve will have an id of Guid.Empty.
    /// After adding a curve, you should set the id to that of a curve in the model
    /// that will be used to calculate the shut-lining.
    /// </summary>
    public Guid AddCurve()
    {
      return UnsafeNativeMethods.ON_3dmObjectAttributes_ShutLining_AddCurve(m_attr.NonConstPointer());
    }

    /// <summary>
    /// Deletes all the curves from the shut-lining.
    /// </summary>
    public void DeleteAllCurves()
    {
      UnsafeNativeMethods.ON_3dmObjectAttributes_ShutLining_DeleteAllCurves(m_attr.NonConstPointer());
    }

    /// <summary>
    /// Return whether shut-line is created for the given curve.
    /// </summary>
    public bool CurveEnabled(Guid curve_id)
    {
      return UnsafeNativeMethods.ON_3dmObjectAttributes_ShutLining_GetCurveEnabled(m_attr.ConstPointer(), curve_id);
    }

    /// <summary>
    /// Sets whether shut-line is created for the given curve.
    /// </summary>
    public void SetCurveEnabled(Guid curve_id, bool enabled)
    {
      UnsafeNativeMethods.ON_3dmObjectAttributes_ShutLining_SetCurveEnabled(m_attr.NonConstPointer(), curve_id, enabled);
    }

    /// <summary>
    /// Returns the radius of the pipe used to create the shut-line for the given curve.
    /// </summary>
    public double CurveRadius(Guid curve_id)
    {
      return UnsafeNativeMethods.ON_3dmObjectAttributes_ShutLining_GetCurveRadius(m_attr.ConstPointer(), curve_id);
    }

    /// <summary>
    /// Sets the radius of the pipe used to create the shut-line for the given curve.
    /// </summary>
    public void SetCurveRadius(Guid curve_id, double radius)
    {
      UnsafeNativeMethods.ON_3dmObjectAttributes_ShutLining_SetCurveRadius(m_attr.NonConstPointer(), curve_id, radius);
    }

    /// <summary>
    /// Returns the profile of the shut-line for the given curve.
    /// </summary>
    public int CurveProfile(Guid curve_id)
    {
      return UnsafeNativeMethods.ON_3dmObjectAttributes_ShutLining_GetCurveProfile(m_attr.ConstPointer(), curve_id);
    }

    /// <summary>
    /// Sets the profile of the shut-line for the given curve.
    /// </summary>
    public void SetCurveProfile(Guid curve_id, int profile)
    {
      UnsafeNativeMethods.ON_3dmObjectAttributes_ShutLining_SetCurveProfile(m_attr.NonConstPointer(), curve_id, profile);
    }

    /// <summary>
    /// Returns whether the given curve is pulled to the surface before creating the shut-line.
    /// </summary>
    public bool CurvePull(Guid curve_id)
    {
      return UnsafeNativeMethods.ON_3dmObjectAttributes_ShutLining_GetCurvePull(m_attr.ConstPointer(), curve_id);
    }

    /// <summary>
    /// Sets whether the given curve is pulled to the surface before creating the shut-line.
    /// </summary>
    public void SetCurvePull(Guid curve_id, bool pull)
    {
      UnsafeNativeMethods.ON_3dmObjectAttributes_ShutLining_SetCurvePull(m_attr.NonConstPointer(), curve_id, pull);
    }

    /// <summary>
    /// Returns whether to create a bump instead of a dent for the given curve.
    /// </summary>
    public bool CurveIsBump(Guid curve_id)
    {
      return UnsafeNativeMethods.ON_3dmObjectAttributes_ShutLining_GetCurveIsBump(m_attr.ConstPointer(), curve_id);
    }

    /// <summary>
    /// Sets whether to create a bump instead of a dent for the given curve.
    /// </summary>
    public void SetCurveIsBump(Guid curve_id, bool b)
    {
      UnsafeNativeMethods.ON_3dmObjectAttributes_ShutLining_SetCurveIsBump(m_attr.NonConstPointer(), curve_id, b);
    }
  }

  /// <summary>
  /// Represents the mesh modifiers attached to file3dm object attributes.
  /// </summary>
  public class File3dmMeshModifiers
  {
    private readonly ObjectAttributes m_attr;
    private File3dmDisplacement m_displacement;
    private File3dmEdgeSoftening m_edge_softening;
    private File3dmThickening m_thickening;
    private File3dmCurvePiping m_curve_piping;
    private File3dmShutLining m_shut_lining;

    internal File3dmMeshModifiers(ObjectAttributes attr)
    {
      m_attr = attr;
    }

    /// <summary>
    /// Returns an object that provides access to displacement information.
    /// If no displacement information is present, the method returns null.
    /// </summary>
    public File3dmDisplacement Displacement
    {
      get
      {
        if (!UnsafeNativeMethods.ON_3dmObjectAttributes_HasDisplacement(m_attr.ConstPointer()))
          return null;

        return m_displacement ?? (m_displacement = new File3dmDisplacement(m_attr));
      }
    }

    /// <summary>
    /// Returns an object that provides access to edge softening information.
    /// If no edge softening information is present, the method returns null.
    /// </summary>
    public File3dmEdgeSoftening EdgeSoftening 
    {
      get
      {
        if (!UnsafeNativeMethods.ON_3dmObjectAttributes_HasEdgeSoftening(m_attr.ConstPointer()))
          return null;

        return m_edge_softening ?? (m_edge_softening = new File3dmEdgeSoftening(m_attr));
      }
    }

    /// <summary>
    /// Returns an object that provides access to thickening information.
    /// If no thickening information is present, the method returns null.
    /// </summary>
    public File3dmThickening Thickening
    {
      get
      {
        if (!UnsafeNativeMethods.ON_3dmObjectAttributes_HasThickening(m_attr.ConstPointer()))
          return null;

        return m_thickening ?? (m_thickening = new File3dmThickening(m_attr));
      }
    }

    /// <summary>
    /// Returns an object that provides access to curve piping information.
    /// If no curve piping information is present, the method returns null.
    /// </summary>
    public File3dmCurvePiping CurvePiping
    {
      get
      {
        if (!UnsafeNativeMethods.ON_3dmObjectAttributes_HasCurvePiping(m_attr.ConstPointer()))
          return null;

        return m_curve_piping ?? (m_curve_piping = new File3dmCurvePiping(m_attr));
      }
    }

    /// <summary>
    /// Returns an object that provides access to shut-lining information.
    /// If no shutlining information is present, the method returns null.
    /// </summary>
    public File3dmShutLining ShutLining
    {
      get
      {
        if (!UnsafeNativeMethods.ON_3dmObjectAttributes_HasShutLining(m_attr.ConstPointer()))
          return null;

        return m_shut_lining ?? (m_shut_lining = new File3dmShutLining(m_attr));
      }
    }
  }
}
