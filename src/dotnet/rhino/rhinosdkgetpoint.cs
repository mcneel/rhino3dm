#pragma warning disable 1591
using System;
using System.Reflection;
using Rhino.Geometry;
using Rhino.Display;

#if RHINO_SDK
namespace Rhino.Input.Custom
{
  /// <summary>
  /// Used to interactively get a point.
  /// </summary>
  public class GetPoint : GetBaseClass
  {
    /// <summary>Create a new GetPoint.</summary>
    /// <example>
    /// <code source='examples\vbnet\ex_addline.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_addline.cs' lang='cs'/>
    /// <code source='examples\py\ex_addline.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    public GetPoint()
    {
      IntPtr ptr = UnsafeNativeMethods.CRhinoGetPoint_New();
      Construct(ptr);
    }

    internal GetPoint(bool subclass)
    {
      //subclass is only there to differentiate the constructor for GetTransform
    }

    /// <summary>
    /// Sets a base point used by ortho snap, from snap, planar snap, etc.
    /// </summary>
    /// <param name="basePoint">The new base point.</param>
    /// <param name="showDistanceInStatusBar">
    /// If true, then the distance from base_point to the current point will be in the
    /// status bar distance pane.
    /// </param>
    /// <example>
    /// <code source='examples\vbnet\ex_addline.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_addline.cs' lang='cs'/>
    /// <code source='examples\py\ex_addline.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    public void SetBasePoint(Point3d basePoint, bool showDistanceInStatusBar)
    {
      IntPtr ptr = NonConstPointer();
      UnsafeNativeMethods.CRhinoGetPoint_SetBasePoint(ptr, basePoint, showDistanceInStatusBar);
    }

    /// <since>5.0</since>
    public bool TryGetBasePoint(out Point3d basePoint)
    {
      IntPtr ptr = ConstPointer();
      basePoint = new Point3d();
      bool rc = UnsafeNativeMethods.CRhinoGetPoint_GetBasePoint(ptr, ref basePoint);
      if (!rc)
        basePoint = Point3d.Unset;
      return rc;
    }

    /// <since>6.0</since>
    public bool GetPlanarConstraint(ref RhinoViewport vp, out Plane plane)
    {
      IntPtr ptr = ConstPointer();
      IntPtr ptr_viewport = IntPtr.Zero;
      if(null != vp)
        ptr_viewport = vp.ConstPointer();

      plane = new Plane();
      bool rc = UnsafeNativeMethods.CRhinoGetPoint_GetPlanarConstraint(ptr, ptr_viewport, ref plane);
      if(!rc)
        plane = Plane.Unset;
      return rc;
    }

    /// <summary>
    /// Sets distance constraint from base point.
    /// </summary>
    /// <param name="distance">
    /// pass UnsetValue to clear this constraint. Pass 0.0 to disable the
    /// ability to set this constraint by typing a number during GetPoint.
    /// </param>
    /// <remarks>
    /// If the base point is set and the distance from base point constraint
    /// is > 0, then the picked point is constrained to be this distance
    /// from the base point.
    /// </remarks>
    /// <since>5.0</since>
    public void ConstrainDistanceFromBasePoint(double distance)
    {
      IntPtr ptr = NonConstPointer();
      UnsafeNativeMethods.CRhinoGetPoint_ConstrainDistanceFromBasePoint(ptr, distance);
    }

    /// <summary>
    /// Color used by CRhinoGetPoint::DynamicDraw to draw the current point and
    /// the line from the base point to the current point.
    /// </summary>
    /// <since>5.0</since>
    public System.Drawing.Color DynamicDrawColor
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        int argb = 0;
        int abgr = UnsafeNativeMethods.CRhinoGetPoint_DynamicDrawColor(const_ptr_this, ref argb, false);
        return Runtime.Interop.ColorFromWin32(abgr);
      }
      set
      {
        IntPtr ptr_this = NonConstPointer();
        int argb = value.ToArgb();
        UnsafeNativeMethods.CRhinoGetPoint_DynamicDrawColor(ptr_this, ref argb, true);
      }
    }

    /// <summary>
    /// Sets cursor that will be used when Get() is called and snap is not
    /// happening.
    /// </summary>
    /// <param name="cursor"></param>
    /// <since>6.0</since>
    public void SetCursor(UI.CursorStyle cursor)
    {
      IntPtr ptr_this = NonConstPointer();
      UnsafeNativeMethods.CRhinoGetPoint_SetCursor(ptr_this, cursor);
    }

    /// <summary>
    /// Enables or disables object snap cursors. By default, object snap cursors are enabled.
    /// </summary>
    /// <param name="enable">If true then object snap cursors (plus sign with "near", "end", etc.) 
    /// are used when the point snaps to a object. 
    /// </param>
    /// <since>6.0</since>
    public void EnableObjectSnapCursors(bool enable)
    {
      EnableItem(UnsafeNativeMethods.GetPointEnable.EnableObjectSnapCursors, enable);
    }

    /// <summary>
    /// Use DrawLineFromPoint() if you want a dynamic line drawn from a point to the point being picked.
    /// </summary>
    /// <param name="startPoint">
    /// The line is drawn from startPoint to the point being picked. If the base
    /// point has not been set, then it is set to startPoint.
    /// </param>
    /// <param name="showDistanceInStatusBar">
    /// if true, the distance from the basePoint to the point begin picked is shown in the status bar.
    /// </param>
    /// <remarks>
    /// Calling DrawLineFromPoint automatically enables drawing the line. Use
    /// EnableDrawLineFromPoint() to toggle the line drawing state.
    /// </remarks>
    /// <example>
    /// <code source='examples\vbnet\ex_addline.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_addline.cs' lang='cs'/>
    /// <code source='examples\py\ex_addline.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    public void DrawLineFromPoint(Point3d startPoint, bool showDistanceInStatusBar)
    {
      IntPtr ptr = NonConstPointer();
      UnsafeNativeMethods.CRhinoGetPoint_DrawLineFromPoint(ptr, startPoint, showDistanceInStatusBar);
    }

    void EnableItem(UnsafeNativeMethods.GetPointEnable which, bool enable)
    {
      IntPtr ptr = NonConstPointer();
      UnsafeNativeMethods.CRhinoGetPoint_EnableItem(ptr, which, enable);
    }

    /// <summary>
    /// Controls drawing of dynamic a line from the start point.
    /// </summary>
    /// <param name="enable">
    /// if true, a dynamic line is drawn from the DrawLineFromPoint startPoint to the point being picked.
    /// </param>
    /// <since>5.0</since>
    public void EnableDrawLineFromPoint( bool enable )
    {
      EnableItem(UnsafeNativeMethods.GetPointEnable.EnableDrawLineFromPoint, enable);
    }

    /// <summary>
    /// The default functionality of the GetPoint operation is to perform a redraw on exit.
    /// Calling this function with true turns off automatic redraw at the end of GetPoint.
    /// May be needed in some commands for flicker free feedback.
    /// When set to true, the caller is responsible for cleaning up the screen
    /// after GetPoint.
    /// </summary>
    /// <param name="noRedraw"></param>
    /// <since>6.0</since>
    public void EnableNoRedrawOnExit(bool noRedraw)
    {
      EnableItem(UnsafeNativeMethods.GetPointEnable.NoRedrawOnExit, noRedraw);
    }

    /// <summary>
    /// Controls availability of ortho snap. Default is true.
    /// </summary>
    /// <param name="permit">
    /// if true, then GetPoint pays attention to the Rhino "ortho snap" and "planar snap" settings
    /// reported by ModelAidSettings.Ortho and ModelAidSettings.Planar.
    /// </param>
    /// <since>5.0</since>
    public void PermitOrthoSnap( bool permit )
    {
      EnableItem(UnsafeNativeMethods.GetPointEnable.PermitOrthoSnap, permit);
    }

    /// <summary>
    /// Control the availability of the built-in "From" option. By default, the "From" option is enabled.
    /// </summary>
    /// <param name="permit">
    /// if true, then the "From" option is automatically available in GetPoint.
    /// </param>
    /// <remarks>
    /// The GetPoint "From" option is never visible on the command line and the user must
    /// type the complete option name to activate the "From" option. When the GetPoint "From"
    /// snap is enabled, the user set/change the base point during GetPoint by typing "From" and
    /// picking a point.
    /// A related option is the built-in distance from base point constraint that is can be set
    /// before GetPoint is called by passing a value to GetPoint::ConstrainDistanceFromBasePoint 
    /// or during GetPoint by entering a number.
    /// </remarks>
    /// <since>5.0</since>
    public void PermitFromOption( bool permit )
    {
      EnableItem(UnsafeNativeMethods.GetPointEnable.PermitFromOption, permit);
    }

    /// <summary>
    /// Control the availability of the built-in linear, planar, curve, and surface
    /// constraint options like "Along", "AlongPerp", "AlongTan", "AlongParallel",
    /// "Between", "OnCrv", "OnSrf", ".x", ".y", ".z", ".xy", etc.
    /// </summary>
    /// <param name="permit">
    /// if true, then the built-in constraint options are automatically available in GetPoint.
    /// </param>
    /// <remarks>
    /// By default, these built-in constraint options are available unless an explicit
    /// constraint is added by calling one of the GetPoint::Constrain functions. Calling
    /// GetPoint::ClearConstraints automatically enables the built-in constraint options.
    /// The built-in constraint options are never visible on the command line and the
    /// user must type the complete option name to activate these options.
    /// </remarks>
    /// <since>5.0</since>
    public void PermitConstraintOptions( bool permit )
    {
      EnableItem(UnsafeNativeMethods.GetPointEnable.PermitConstraintOptions, permit);
    }

    /// <summary>
    /// Permits the use of the tab key to define a line constraint.
    /// </summary>
    /// <param name="permit">If true, then the built-in tab key mode is available.</param>
    /// <remarks>By default, use of the tab key is supported.</remarks>
    /// <since>5.0</since>
    public void PermitTabMode( bool permit )
    {
      EnableItem(UnsafeNativeMethods.GetPointEnable.PermitTabMode, permit);
    }

    /// <summary>
    /// Permits the use of the control key to define a line constraint.
    /// </summary>
    /// <param name="permitMode">
    /// 0: no elevator modes are permitted
    /// 1: fixed plane elevator mode (like the Line command)
    /// 2: cplane elevator mode (like object dragging)
    /// </param>
    /// <since>5.0</since>
    public void PermitElevatorMode(int permitMode)
    {
      IntPtr ptr = NonConstPointer();
      UnsafeNativeMethods.CRhinoGetPoint_PermitElevatorMode(ptr, permitMode);
    }

    /// <summary>
    /// By default, object snaps like "end", "near", etc. are controlled by the user.
    /// If you want to disable this ability, then call PermitObjectSnap(false).
    /// </summary>
    /// <param name="permit">true to permit snapping to objects.</param>
    /// <since>5.0</since>
    public void PermitObjectSnap( bool permit )
    {
      EnableItem(UnsafeNativeMethods.GetPointEnable.PermitObjectSnap, permit);
    }

    // [skipping]
    //  void ProhibitObjectSnap( const CRhinoObject* object );

    /// <summary>
    /// Adds a point to the list of osnap points.
    /// </summary>
    /// <param name="point">A point.</param>
    /// <returns>Total number of snap points.</returns>
    /// <remarks>
    /// When point osnap is enabled, GetPoint will snap to points in the Rhino model.
    /// If you want the user to be able to snap to additional points, then use
    /// GetPoint::AddSnapPoints to specify the locations of these additional points.
    /// </remarks>
    /// <since>5.0</since>
    public int AddSnapPoint(Point3d point)
    {
      IntPtr ptr = NonConstPointer();
      Point3d[] pts = new Point3d[] { point };
      return UnsafeNativeMethods.CRhinoGetPoint_AddSnapPoints(ptr, 1, pts, true);
    }
    /// <summary>
    /// Adds points to the list of osnap points.
    /// </summary>
    /// <param name="points">An array of points to snap onto.</param>
    /// <returns>Total number of snap points.</returns>
    /// <remarks>
    /// When point osnap is enabled, GetPoint will snap to points in the Rhino model.
    /// If you want the user to be able to snap to additional points, then use
    /// GetPoint::AddSnapPoints to specify the locations of these additional points.
    /// </remarks>
    /// <since>5.0</since>
    public int AddSnapPoints(Point3d[] points)
    {
      if( null == points || points.Length<1)
        return 0;
      int count = points.Length;
      IntPtr ptr = NonConstPointer();
      return UnsafeNativeMethods.CRhinoGetPoint_AddSnapPoints(ptr, count, points, true);
    }


    /// <summary>
    /// Adds a point to the list of construction points.
    /// </summary>
    /// <param name="point">A point to be added.</param>
    /// <returns>Total number of construction points.</returns>
    /// <remarks>
    /// Construction points are like snap points except that they get snapped to even when
    /// point osnap is off.  Typically, there are only a few construction points while there
    /// can be many snap points. For example, when polylines are drawn the start point is a
    /// construction point and the other points are snap points.
    /// </remarks>
    /// <since>5.0</since>
    public int AddConstructionPoint(Point3d point)
    {
      IntPtr ptr = NonConstPointer();
      Point3d[] pts = new Point3d[] { point };
      return UnsafeNativeMethods.CRhinoGetPoint_AddSnapPoints(ptr, 1, pts, false);
    }
    /// <summary>
    /// Adds points to the list of construction points.
    /// </summary>
    /// <param name="points">An array of points to be added.</param>
    /// <returns>Total number of construction points.</returns>
    /// <remarks>
    /// Construction points are like snap points except that they get snapped to even when
    /// point osnap is off.  Typically, there are only a few construction points while there
    /// can be many snap points. For example, when polylines are drawn the start point is a
    /// construction point and the other points are snap points.
    /// </remarks>
    /// <since>5.0</since>
    public int AddConstructionPoints(Point3d[] points)
    {
      if (null == points || points.Length < 1)
        return 0;
      int count = points.Length;
      IntPtr ptr = NonConstPointer();
      return UnsafeNativeMethods.CRhinoGetPoint_AddSnapPoints(ptr, count, points, false);
    }

    /// <summary>
    /// Remove all snap points.
    /// </summary>
    /// <remarks>
    /// When point osnap is enabled, GetPoint will snap to points in the Rhino model.
    /// If you want the user to be able to snap to additional points, then use GetPoint::AddSnapPoints
    /// to specify the locations of these additional points.
    /// </remarks>
    /// <since>5.0</since>
    public void ClearSnapPoints()
    {
      IntPtr ptr = NonConstPointer();
      UnsafeNativeMethods.CRhinoGetPoint_ClearSnapPoints(ptr, true);
    }

    /// <summary>
    /// Remove all construction points.
    /// </summary>
    /// <remarks>
    /// Construction points are like snap points except that they get snapped to
    /// even when point osnap is off. Typically, there are only a few construction
    /// points while there can be many snap points. For example, when polylines
    /// are drawn the start point is a construction point and the other points are
    /// snap points.
    /// </remarks>
    /// <since>5.0</since>
    public void ClearConstructionPoints()
    {
      IntPtr ptr = NonConstPointer();
      UnsafeNativeMethods.CRhinoGetPoint_ClearSnapPoints(ptr, false);
    }

    /// <summary>
    /// Gets current snap points.
    /// </summary>
    /// <returns>An array of points.</returns>
    /// <since>5.0</since>
    public Point3d[] GetSnapPoints()
    {
      Runtime.InteropWrappers.SimpleArrayPoint3d pts = new Runtime.InteropWrappers.SimpleArrayPoint3d();
      IntPtr conrt_ptr_this = ConstPointer();
      IntPtr ptr_array = pts.NonConstPointer();
      UnsafeNativeMethods.CRhinoGetPoint_GetSnapPoints(conrt_ptr_this, ptr_array, true);
      Point3d[] rc = pts.ToArray();
      pts.Dispose();
      return rc;
    }

    /// <summary>
    /// Gets current construction points.
    /// </summary>
    /// <returns>An array of points.</returns>
    /// <remarks>
    /// Construction points are like snap points except that they get snapped to
    /// even when point osnap is off. Typically, there are only a few construction
    /// points while there can be many snap points. For example, when polylines
    /// are drawn the start point is a construction point and the other points are
    /// snap points.
    /// </remarks>
    /// <since>5.0</since>
    public Point3d[] GetConstructionPoints()
    {
      Runtime.InteropWrappers.SimpleArrayPoint3d pts = new Runtime.InteropWrappers.SimpleArrayPoint3d();
      IntPtr const_ptr_this = ConstPointer();
      IntPtr ptr_array = pts.NonConstPointer();
      UnsafeNativeMethods.CRhinoGetPoint_GetSnapPoints(const_ptr_this, ptr_array, false);
      Point3d[] rc = pts.ToArray();
      pts.Dispose();
      return rc;
    }

    const int idxEnableCurveSnapTangentBar = 0;
    const int idxEnableCurveSnapPerpBar = 1;
    const int idxEnableCurveSnapArrow = 3;

    /// <summary>
    /// Controls display of the curve snap tangent bar icon.
    /// </summary>
    /// <param name="drawTangentBarAtSnapPoint">
    /// true to draw a tangent bar icon whenever GetPoint snaps to a curve.
    /// </param>
    /// <param name="drawEndPoints">
    /// true to draw points at the end of the tangent bar.
    /// </param>
    /// <remarks>
    /// The tangent bar is drawn by GetPoint::DynamicDraw. If you override GetPoint::DynamicDraw,
    /// then you must call the base class function.
    /// </remarks>
    /// <since>5.0</since>
    public void EnableCurveSnapTangentBar(bool drawTangentBarAtSnapPoint, bool drawEndPoints)
    {
      IntPtr ptr = NonConstPointer();
      UnsafeNativeMethods.CRhinoGetPoint_EnableItem2(ptr, idxEnableCurveSnapTangentBar, drawTangentBarAtSnapPoint, drawEndPoints);
    }
    /// <summary>
    /// Controls display of the curve snap perpendicular bar icon.
    /// </summary>
    /// <param name="drawPerpBarAtSnapPoint">
    /// true to draw a tangent bar icon  whenever GetPoint snaps to a curve.
    /// </param>
    /// <param name="drawEndPoints">
    /// true to draw points at the end of the tangent bar.
    /// </param>
    /// <since>5.0</since>
    public void EnableCurveSnapPerpBar(bool drawPerpBarAtSnapPoint, bool drawEndPoints)
    {
      IntPtr ptr = NonConstPointer();
      UnsafeNativeMethods.CRhinoGetPoint_EnableItem2(ptr, idxEnableCurveSnapPerpBar, drawPerpBarAtSnapPoint, drawEndPoints);
    }
    /// <summary>
    /// Controls display of the curve snap arrow icon.
    /// </summary>
    /// <param name="drawDirectionArrowAtSnapPoint">
    /// true to draw arrow icon whenever GetPoint snaps to a curve.
    /// </param>
    /// <param name="reverseArrow">
    /// true if arrow icon direction should be the reverse of the first derivative direction.
    /// </param>
    /// <remarks>
    /// The tangent bar is drawn by GetPoint::DynamicDraw. If you override GetPoint::DynamicDraw,
    /// then you must call the base class function.
    /// </remarks>
    /// <since>5.0</since>
    public void EnableCurveSnapArrow(bool drawDirectionArrowAtSnapPoint, bool reverseArrow)
    {
      IntPtr ptr = NonConstPointer();
      UnsafeNativeMethods.CRhinoGetPoint_EnableItem2(ptr, idxEnableCurveSnapArrow, drawDirectionArrowAtSnapPoint, reverseArrow);
    }

    /// <summary>
    /// If you want GetPoint() to try to snap to curves when the mouse is near a curve
    /// (like the center point in the Circle command when the AroundCurve option is on),
    /// then enable the snap to curves option.
    /// </summary>
    /// <param name="enable">Whether points should be enabled.</param>
    /// <since>5.0</since>
    public void EnableSnapToCurves(bool enable)
    {
      EnableItem(UnsafeNativeMethods.GetPointEnable.EnableSnapToCurves, enable);
    }

    /// <summary>Constrains the picked point to lie on a line.</summary>
    /// <param name="from">The start point of constraint.</param>
    /// <param name="to">The end point of constraint.</param>
    /// <returns>true if constraint could be applied.</returns>
    /// <example>
    /// <code source='examples\vbnet\ex_arraybydistance.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_arraybydistance.cs' lang='cs'/>
    /// <code source='examples\py\ex_arraybydistance.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    public bool Constrain(Point3d from, Point3d to)
    {
      IntPtr ptr = NonConstPointer();
      return UnsafeNativeMethods.CRhinoGetPoint_Constrain1(ptr, from, to);
    }
    /// <summary>Constrains the picked point to lie on a line.</summary>
    /// <param name="line">A line to use as constraint.</param>
    /// <returns>true if constraint could be applied.</returns>
    /// <example>
    /// <code source='examples\vbnet\ex_constrainedcopy.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_constrainedcopy.cs' lang='cs'/>
    /// <code source='examples\py\ex_constrainedcopy.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    public bool Constrain(Line line)
    {
      return Constrain(line.From, line.To);
    }

    /// <summary>Constrains the picked point to lie on an arc.</summary>
    /// <param name="arc">An arc to use as constraint.</param>
    /// <returns>true if constraint could be applied.</returns>
    /// <since>5.0</since>
    public bool Constrain(Arc arc)
    {
      IntPtr ptr = NonConstPointer();
      return UnsafeNativeMethods.CRhinoGetPoint_Constrain2(ptr, ref arc);
    }
    /// <summary>Constrains the picked point to lie on a circle.</summary>
    /// <param name="circle">A circle to use as constraint.</param>
    /// <returns>true if constraint could be applied.</returns>
    /// <since>5.0</since>
    public bool Constrain(Circle circle)
    {
      IntPtr ptr = NonConstPointer();
      return UnsafeNativeMethods.CRhinoGetPoint_Constrain3(ptr, ref circle);
    }
    /// <summary>constrain the picked point to lie on a plane.</summary>
    /// <param name="plane">A plane to use as constraint.</param>
    /// <param name="allowElevator">true if elevator mode should be allowed at user request.</param>
    /// <returns>true if constraint could be applied.</returns>
    /// <since>5.0</since>
    public bool Constrain(Plane plane, bool allowElevator)
    {
      IntPtr ptr = NonConstPointer();
      return UnsafeNativeMethods.CRhinoGetPoint_Constrain4(ptr, ref plane, allowElevator);
    }
    /// <summary>Constrains the picked point to lie on a sphere.</summary>
    /// <param name="sphere">A sphere to use as constraint.</param>
    /// <returns>true if constraint could be applied.</returns>
    /// <since>5.0</since>
    public bool Constrain(Sphere sphere)
    {
      IntPtr ptr = NonConstPointer();
      return UnsafeNativeMethods.CRhinoGetPoint_Constrain5(ptr, ref sphere);
    }
    /// <summary>Constrains the picked point to lie on a cylinder.</summary>
    /// <param name="cylinder">A cylinder to use as constraint.</param>
    /// <returns>true if constraint could be applied.</returns>
    /// <since>5.0</since>
    public bool Constrain(Cylinder cylinder)
    {
      IntPtr ptr = NonConstPointer();
      return UnsafeNativeMethods.CRhinoGetPoint_Constrain6(ptr, ref cylinder);
    }

    /// <summary>Constrains the picked point to lie on a curve.</summary>
    /// <param name="curve">A curve to use as constraint.</param>
    /// <param name="allowPickingPointOffObject">
    /// defines whether the point pick is allowed to happen off object. When false,
    /// a "no no" cursor is shown when the cursor is not on the object. When true,
    /// a normal point picking cursor is used and the marker is visible also when
    /// the cursor is not on the object.
    /// </param>
    /// <returns>true if constraint could be applied.</returns>
    /// <example>
    /// <code source='examples\vbnet\ex_insertknot.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_insertknot.cs' lang='cs'/>
    /// <code source='examples\py\ex_insertknot.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    public bool Constrain(Curve curve, bool allowPickingPointOffObject)
    {
      IntPtr ptr_this = NonConstPointer();
      IntPtr const_ptr_curve = curve.ConstPointer();
      return UnsafeNativeMethods.CRhinoGetPoint_Constrain7(ptr_this, const_ptr_curve, allowPickingPointOffObject);
    }
    /// <summary>Constrains the picked point to lie on a surface.</summary>
    /// <param name="surface">A surface to use as constraint.</param>
    /// <param name="allowPickingPointOffObject">
    /// defines whether the point pick is allowed to happen off object. When false,
    /// a "no no" cursor is shown when the cursor is not on the object. When true,
    /// a normal point picking cursor is used and the marker is visible also when
    /// the cursor is not on the object.
    /// </param>
    /// <returns>true if constraint could be applied.</returns>
    /// <example>
    /// <code source='examples\vbnet\ex_orientonsrf.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_orientonsrf.cs' lang='cs'/>
    /// <code source='examples\py\ex_orientonsrf.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    public bool Constrain(Surface surface, bool allowPickingPointOffObject)
    {
      IntPtr ptr_this = NonConstPointer();
      IntPtr const_ptr_surface = surface.ConstPointer();
      return UnsafeNativeMethods.CRhinoGetPoint_Constrain8(ptr_this, const_ptr_surface, allowPickingPointOffObject);
    }
    /// <summary>Constrains the picked point to lie on a brep.</summary>
    /// <param name="brep">A brep to use as constraint.</param>
    /// <param name="wireDensity">
    /// When wire_density&lt;0, isocurve intersection snapping is turned off, when wire_density>=0, the value
    /// defines the isocurve density used for isocurve intersection snapping.
    /// </param>
    /// <param name="faceIndex">
    /// When face_index &lt;0, constrain to whole brep. When face_index >=0, constrain to individual face.
    /// </param>
    /// <param name="allowPickingPointOffObject">
    /// defines whether the point pick is allowed to happen off object. When false,
    /// a "no no" cursor is shown when the cursor is not on the object. When true,
    /// a normal point picking cursor is used and the marker is visible also when
    /// the cursor is not on the object.
    /// </param>
    /// <returns>true if constraint could be applied.</returns>
    /// <since>5.0</since>
    public bool Constrain(Brep brep, int wireDensity, int faceIndex, bool allowPickingPointOffObject)
    {
      IntPtr ptr_this = NonConstPointer();
      IntPtr const_ptr_brep = brep.ConstPointer();
      return UnsafeNativeMethods.CRhinoGetPoint_Constrain9(ptr_this, const_ptr_brep, wireDensity, faceIndex, allowPickingPointOffObject);
    }

    /// <summary>Constrains the picked point to lie on a mesh.</summary>
    /// <param name="mesh">A mesh to use as constraint.</param>
    /// <param name="allowPickingPointOffObject">
    /// defines whether the point pick is allowed to happen off object. When false,
    /// a "no no" cursor is shown when the cursor is not on the object. When true,
    /// a normal point picking cursor is used and the marker is visible also when
    /// the cursor is not on the object.
    /// </param>
    /// <returns>true if constraint could be applied.</returns>
    /// <since>5.0</since>
    public bool Constrain(Mesh mesh, bool allowPickingPointOffObject)
    {
      IntPtr ptr_this = NonConstPointer();
      IntPtr const_ptr_mesh = mesh.ConstPointer();
      return UnsafeNativeMethods.CRhinoGetPoint_ConstrainToMesh(ptr_this, const_ptr_mesh, allowPickingPointOffObject);
    }


    /// <summary>
    /// If enabled, the picked point is constrained to be on the active construction plane.
    /// If the base point is set, then the point is constrained to be on the plane that contains
    /// the base point and is parallel to the active construction plane. By default this
    /// constraint is enabled.
    /// </summary>
    /// <param name="throughBasePoint">true if the base point should be used as compulsory level reference.</param>
    /// <returns>
    /// If true and the base point is set, then the point is constrained to be on the plane parallel
    /// to the construction plane that passes through the base point, even when planar mode is off.
    /// If throughBasePoint is false, then the base point shift only happens if planar mode is on.
    /// </returns>
    /// <example>
    /// <code source='examples\vbnet\ex_addbackgroundbitmap.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_addbackgroundbitmap.cs' lang='cs'/>
    /// <code source='examples\py\ex_addbackgroundbitmap.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    public bool ConstrainToConstructionPlane(bool throughBasePoint)
    {
      IntPtr ptr = NonConstPointer();
      return UnsafeNativeMethods.CRhinoGetPoint_ConstrainToConstructionPlane(ptr, throughBasePoint);
    }

    /// <summary>
    /// Constrains point to lie on a plane that is parallel to the
    /// viewing plane and passes through the view's target point.
    /// </summary>
    /// <since>5.0</since>
    public void ConstrainToTargetPlane()
    {
      EnableItem(UnsafeNativeMethods.GetPointEnable.ConstrainToTargetPlane, true);
    }

    /// <summary>
    /// If enabled, the picked point is constrained to be on the 
    /// intersection of the plane and the virtual CPlane going through
    /// the plane origin.
    /// If the planes are parallel, the constraint works just like planar constraint.
    /// </summary>
    /// <param name="plane">The plane used for the plane - virtual CPlane intersection.</param>
    /// <returns>true if the operation succeeded; false otherwise.</returns>
    /// <since>5.0</since>
    public bool ConstrainToVirtualCPlaneIntersection(Plane plane)
    {
      IntPtr ptr = NonConstPointer();
      return UnsafeNativeMethods.CRhinoGetPoint_ConstrainToVirtualCPlaneIntersection(ptr, ref plane);
    }

    /// <summary>
    /// Removes any explicit constraints added by calls to GetPoint::Constraint() and enable
    /// the built-in constraint options.
    /// </summary>
    /// <example>
    /// <code source='examples\vbnet\ex_arraybydistance.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_arraybydistance.cs' lang='cs'/>
    /// <code source='examples\py\ex_arraybydistance.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    public void ClearConstraints()
    {
      IntPtr ptr = NonConstPointer();
      UnsafeNativeMethods.CRhinoGetPoint_ClearConstraints(ptr);
    }

    /// <summary>
    /// If you have lengthy computations in OnMouseMove() and/or DymanicDraw()
    /// overrides, then periodically call InterruptMouseMove() to see if you
    /// should interrupt your work because the mouse has moved again.
    /// </summary>
    /// <returns>true if you should interrupt your work; false otherwise.</returns>
    /// <since>5.0</since>
    public bool InterruptMouseMove()
    {
      IntPtr ptr = ConstPointer();
      return UnsafeNativeMethods.CRhinoGetPoint_InterruptMouseMose(ptr);
    }

    internal static GetPoint m_active_gp; // = null; [runtime default]
    internal delegate void MouseCallback( IntPtr pRhinoViewport, uint flags, Point3d point, System.Drawing.Point viewWndPoint, int mousemove);
    internal delegate int DrawCallback(IntPtr pDisplayPipeline, Point3d point);

    private static void CustomMouseCallback(IntPtr pRhinoViewport, uint flags, Point3d point, System.Drawing.Point viewWndPoint, int move)
    {
      if (null == m_active_gp)
        return;
      try
      {
        GetPointMouseEventArgs e = new GetPointMouseEventArgs(m_active_gp, pRhinoViewport, flags, point, viewWndPoint);
        bool call_move = (move != 0);
        if (call_move)
          m_active_gp.OnMouseMove(e);
        else
          m_active_gp.OnMouseDown(e);
      }
      catch (Exception ex)
      {
        // only report exception once per session
        if( !g_mousemove_exception_reported )
          Runtime.HostUtils.ExceptionReport(ex);
        g_mousemove_exception_reported = true;
      }
    }
    static bool g_mousemove_exception_reported = false;

    bool m_baseOnDynamicDrawCalled;
    private static int CustomDrawCallback(IntPtr pDisplayPipeline, Point3d point)
    {
      if (null == m_active_gp)
        return 0;
      m_active_gp.m_baseOnDynamicDrawCalled = false;
      int rc = 0;
      try
      {
        GetPointDrawEventArgs e = new GetPointDrawEventArgs(m_active_gp, pDisplayPipeline, point);
        m_active_gp.OnDynamicDraw(e);
        rc = m_active_gp.m_baseOnDynamicDrawCalled ? 1 : 0;
        m_active_gp.m_baseOnDynamicDrawCalled = false;
      }
      catch (Exception ex)
      {
        Runtime.HostUtils.ExceptionReport(ex);
      }
      return rc;
    }

    private static void GetPointPostDrawObjectsCallback(IntPtr pPipeline, uint conduitSerialNumber, uint _)
    {
      if (null == m_active_gp)
        return;

      try
      {
        var e = new DrawEventArgs(pPipeline, conduitSerialNumber);
        m_active_gp.OnPostDrawObjects(e);
      }
      catch (Exception ex)
      {
        Runtime.HostUtils.ExceptionReport(ex);
      }
    }
    /// <summary>
    /// Called every time the mouse moves. MouseMove is called once per mouse move and is called
    /// BEFORE any calls to OnDynamicDraw. If you are doing anything that takes a long time,
    /// periodically call InterruptMouseMove() to see if you should stop. If the view is such
    /// that the 2d screen point can't be mapped to a 3d point, the 'point' argument will be Unset.
    /// </summary>
    /// <since>5.0</since>
    public event EventHandler<GetPointMouseEventArgs> MouseMove;

    /// <summary>Calls the <see cref="MouseMove"/> event and can/should be called by overriding implementation.</summary>
    /// <param name="e">Current argument for the event.</param>
    protected virtual void OnMouseMove(GetPointMouseEventArgs e)
    {
      if (MouseMove != null)
        MouseMove(this, e);
    }

    /// <summary>
    /// Gets or sets an arbitrary object that can be attached to this <see cref="GetPoint"/> instance.
    /// Useful for passing some/ information that you may need in a DynamicDraw event since you can get at this Tag from
    /// the GetPointDrawEventArgs.
    /// </summary>
    /// <example>
    /// <code source='examples\vbnet\ex_arraybydistance.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_arraybydistance.cs' lang='cs'/>
    /// <code source='examples\py\ex_arraybydistance.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    public object Tag { get; set; }

    /// <summary>
    /// Called during Get2dRectangle, Get2dLine, and GetPoint(..,true) when the mouse down event for
    /// the initial point occurs. This function is not called during ordinary point getting because
    /// the mouse down event terminates an ordinary point get and returns a GetResult.Point result.
    /// </summary>
    /// <since>5.0</since>
    public event EventHandler<GetPointMouseEventArgs> MouseDown;

    /// <summary>Default calls the MouseDown event.</summary>
    /// <param name="e">Current argument for the event.</param>
    protected virtual void OnMouseDown(GetPointMouseEventArgs e)
    {
      if (MouseDown != null)
        MouseDown(this, e);
    }

    /// <summary>
    /// Event to use if you want to dynamically draw things as the mouse/digitizer moves.
    /// Every time the mouse moves, DynamicDraw will be called once per viewport. The
    /// calls to DynamicDraw happen AFTER the call to MouseMove.
    ///
    /// If you are drawing anything that takes a long time, periodically call 
    /// InterruptMouseMove() to see if you should stop.
    /// </summary>
    /// <since>5.0</since>
    public event EventHandler<GetPointDrawEventArgs> DynamicDraw;

    /// <summary>Default calls the DynamicDraw event.</summary>
    /// <param name="e">Current argument for the event.</param>
    /// <example>
    /// <code source='examples\vbnet\ex_getpointdynamicdraw.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_getpointdynamicdraw.cs' lang='cs'/>
    /// <code source='examples\py\ex_getpointdynamicdraw.py' lang='py'/>
    /// </example>
    protected virtual void OnDynamicDraw(GetPointDrawEventArgs e)
    {
      if (DynamicDraw != null)
        DynamicDraw(this, e);
      m_baseOnDynamicDrawCalled = true;
    }

    /// <summary>
    /// In the "RARE" case that you need to draw some depth buffered geometry during
    /// a Get() operation, setting this value to true will force entire frames to be redrawn
    /// while the user moves the mouse. This allows DisplayPipeline events to be triggered
    /// as well as OnPostDrawObjects
    /// NOTE!! Setting this value to true comes with a significant performance penalty because the
    /// scene needs to be fully regenerated every frame where the standard
    /// DynamicDraw event draws temporary decorations (geometry) on top of a static scene.
    /// </summary>
    /// <since>5.0</since>
    public bool FullFrameRedrawDuringGet { get; set; }

    /// <summary>
    /// Same as the DisplayPipeline.PostDrawObjects, but only works during the 
    /// operation of the Get() function.
    /// NOTE: You must set FullFrameRedrawDuringGet to true in order for this
    /// event to be called.
    /// </summary>
    /// <since>5.0</since>
    public event EventHandler<DrawEventArgs> PostDrawObjects;

    /// <summary>
    /// In the "rare" case that you need to draw some depth buffered geometry during
    /// a GetPoint operation, override the OnPostDrawObjects function.
    /// NOTE!! Overriding this function comes with a significant performance penalty because the
    /// scene needs to be fully regenerated every frame where the standard
    /// DynamicDraw event draws temporary decorations (geometry) on top of a static scene.
    /// </summary>
    /// <param name="e">Current argument for the event.</param>
    protected virtual void OnPostDrawObjects(DrawEventArgs e)
    {
      if (PostDrawObjects != null)
        PostDrawObjects(this, e);
    }

    /// <summary>
    /// After setting up options and so on, call this method to get a 3d point.
    /// </summary>
    /// <param name="onMouseUp">
    /// If false, the point is returned when the left mouse button goes down.
    /// If true, the point is returned when the left mouse button goes up.
    /// </param>
    /// <returns><see cref="GetResult.Point"/> if the user chose a point; other enumeration value otherwise.</returns>
    /// <since>5.0</since>
    [CLSCompliant(false)]
    public GetResult Get(bool onMouseUp)
    {
      return Get(onMouseUp, false);
    }

    /// <summary>
    /// After setting up options and so on, call this method to get a 2d or 3d point.
    /// </summary>
    /// <param name="onMouseUp">
    /// If false, the point is returned when the left mouse button goes down.
    /// If true, the point is returned when the left mouse button goes up.
    /// </param>
    /// <param name="get2DPoint">If true then get a 2d point otherwise get a 2d point</param>
    /// <returns><see cref="GetResult.Point"/> if the user chose a 3d point; <see cref="GetResult.Point2d"/> if the user chose a 2d point; other enumeration value otherwise.</returns>
    /// <since>5.12</since>
    [CLSCompliant(false)]
    public GetResult Get(bool onMouseUp, bool get2DPoint)
    {
      var old = m_active_gp;
      m_active_gp = this;

      MouseCallback mouse_cb = CustomMouseCallback;
      DrawCallback draw_cb = CustomDrawCallback;
      GetTransform.CalculateXformCallack calc_xform_cb = GetTransform.CustomCalcXform;
      DisplayPipeline.ConduitCallback post_draw_cb = null;

      if (FullFrameRedrawDuringGet)
      {
        post_draw_cb = GetPointPostDrawObjectsCallback;
      }
      else
      {
        if( IsFunctionOverridden("OnPostDrawObjects") || PostDrawObjects!=null)
          post_draw_cb = GetPointPostDrawObjectsCallback;
      }

      var ptr = NonConstPointer();
      var rc = UnsafeNativeMethods.CRhinoGetPoint_GetPoint(ptr, onMouseUp, get2DPoint, mouse_cb, draw_cb, post_draw_cb, calc_xform_cb);

      m_active_gp = old;

      // https://mcneel.myjetbrains.com/youtrack/issue/RH-67575
      GC.KeepAlive(m_active_gp);
      GC.KeepAlive(this);

      return (GetResult)rc;
    }

    bool IsFunctionOverridden(string functionName)
    {
      try
      {
        Type base_type = typeof(GetPoint);
        // The virtual functions are protected, so we need to call the overload
        // of GetMethod that takes some binding flags
        const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public;

        MethodInfo mi = this.GetType().GetMethod(functionName, flags);
        return (mi.DeclaringType != base_type);
      }
      catch (Exception)
      {
        //mask
      }
      return false;
    }

    internal GetResult GetXformHelper()
    {
      // This function should only ever be called by GetTransform
      if( !(this is GetTransform) )
        throw new ApplicationException("GetXformHelper called from the wrong place");

      GetPoint old = m_active_gp;
      m_active_gp = this;

      MouseCallback mouseCB = CustomMouseCallback;
      DrawCallback drawCB = CustomDrawCallback;
      Rhino.Display.DisplayPipeline.ConduitCallback postDrawCB = null;
      if (FullFrameRedrawDuringGet)
      {
        postDrawCB = GetPointPostDrawObjectsCallback;
      }
      else
      {
        if(IsFunctionOverridden("OnPostDrawObjects"))
          postDrawCB = GetPointPostDrawObjectsCallback;
      }

      IntPtr ptr = NonConstPointer();
      GetTransform.CalculateXformCallack calcXformCB = GetTransform.CustomCalcXform;
      uint rc = UnsafeNativeMethods.CRhinoGetXform_GetXform(ptr, mouseCB, drawCB, postDrawCB, calcXformCB);
      m_active_gp = old;

      return (GetResult)rc;
    }
    /// <summary>
    /// After setting up options and so on, call GetPoint::Get to get a 3d point. The
    /// point is retrieved when the mouse goes down.
    /// </summary>
    /// <example>
    /// <code source='examples\vbnet\ex_addline.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_addline.cs' lang='cs'/>
    /// <code source='examples\py\ex_addline.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    [CLSCompliant(false)]
    public GetResult Get()
    {
      return Get(false);
    }

    /// <summary>
    /// Gets the type of object snap used to obtain the point.
    /// </summary>
    /// <since>6.24</since>
    public Rhino.ApplicationSettings.OsnapModes OsnapEventType
    {
      get
      {
        IntPtr ptr_this = ConstPointer();
        int rc = UnsafeNativeMethods.CRhinoGetPoint_SnapEventMode(ptr_this);
        return (Rhino.ApplicationSettings.OsnapModes)rc;
      }
    }

    /// <summary>
    /// If the user is typing a number, but hasn't pressed Enter, 
    /// true is returned, along with the number the user typed.
    /// </summary>
    /// <param name="number">The number typed by the user.</param>
    /// <returns>true if successful, false otherwise.</returns>
    /// <since>7.23</since>
    public bool NumberPreview(out double number)
    {
      double d = RhinoMath.UnsetValue;
      IntPtr ptr_this = ConstPointer();
      bool rc = UnsafeNativeMethods.CRhinoGetPoint_NumberPreview(ptr_this, ref d);
      number = d;
      return rc;
    }

    /// <summary>
    /// Call this function to see if the point was on an object. If the point was
    /// on an object an ObjRef is returned; otherwise null is returned.
    /// </summary>
    /// <returns>A point object reference.</returns>
    /// <since>5.0</since>
    public DocObjects.ObjRef PointOnObject()
    {
      var rc = new DocObjects.ObjRef();
      IntPtr pThis = ConstPointer();
      IntPtr pObjRef = rc.NonConstPointer();
      if (!UnsafeNativeMethods.CRhinoGetPoint_PointOnObject(pThis, pObjRef))
      {
        rc.Dispose();
        rc = null;
      }
      return rc;
    }

    /// <summary>
    /// Use to determine is point was on a curve.
    /// </summary>
    /// <param name="t">
    /// If the point was on a curve, then the t is the curve
    /// parameter for the point.  The point returned by Point()
    /// is the same as curve.PointAt(t).
    /// </param>
    /// <returns>A curve at a specified parameter value.</returns>
    /// <example>
    /// <code source='examples\vbnet\ex_insertknot.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_insertknot.cs' lang='cs'/>
    /// <code source='examples\py\ex_insertknot.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    public Curve PointOnCurve(out double t)
    {
      t = RhinoMath.UnsetValue;
      DocObjects.ObjRef objref = PointOnObject();
      if (objref == null)
        return null;
      Curve rc = objref.CurveParameter(out t);
      return rc;
    }

    /// <summary>
    /// Use to determine if point was on a surface. If the point was on a surface, 
    /// then the (u,v) are the surface parameters for the point. The point returned
    /// by Point() is the same as surface.PointAt(u,v).
    /// </summary>
    /// <param name="u">If the point was on a surface, then the u parameter.</param>
    /// <param name="v">If the point was on a surface, then the v parameter.</param>
    /// <returns>The surface or null if the point was not on a surface.</returns>
    /// <since>6.0</since>
    public Surface PointOnSurface(out double u, out double v)
    {
      u = RhinoMath.UnsetValue;
      v = RhinoMath.UnsetValue;
      DocObjects.ObjRef objref = PointOnObject();
      if (objref == null)
        return null;
      Surface rc = objref.SurfaceParameter(out u, out v);
      return rc;
    }

    /// <summary>
    /// Use to determine if point was on a Brep face. If the point was on a Brep face, 
    /// then the (u,v) are the face parameters for the point.
    /// </summary>
    /// <param name="u">If the point was on a Brep face, then the u parameter.</param>
    /// <param name="v">If the point was on a Brep face, then the v parameter.</param>
    /// <returns>The Brep face or null if the point was not on a Brep face.</returns>
    /// <since>6.0</since>
    public BrepFace PointOnBrep(out double u, out double v)
    {
      u = RhinoMath.UnsetValue;
      v = RhinoMath.UnsetValue;
      DocObjects.ObjRef objref = PointOnObject();
      if (objref == null)
        return null;
      objref.SurfaceParameter(out u, out v);
      BrepFace rc = objref.Face();
      return rc;
    }
  }

  /// <summary>
  /// Arguments for drawing during point getting.
  /// </summary>
  public class GetPointDrawEventArgs : DrawEventArgs
  {
    //private IntPtr m_pRhinoViewport;
    private readonly Point3d m_point;
    private readonly GetPoint m_source;
    internal GetPointDrawEventArgs(GetPoint source, IntPtr pDisplayPipeline, Point3d point) : base(pDisplayPipeline, 0)
    {
      //m_pRhinoViewport = pRhinoViewport;
      m_point = point;
      m_source = source;
    }

    /// <since>5.0</since>
    public Point3d CurrentPoint
    {
      get { return m_point; }
    }

    /// <summary>
    /// GetPoint class that this draw event originated from.
    /// </summary>
    /// <example>
    /// <code source='examples\vbnet\ex_arraybydistance.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_arraybydistance.cs' lang='cs'/>
    /// <code source='examples\py\ex_arraybydistance.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    public GetPoint Source { get { return m_source; } }
  }

  /// <summary>
  /// Arguments for mouse information during point getting.
  /// </summary>
  public class GetPointMouseEventArgs : EventArgs
  {
    private readonly IntPtr m_pRhinoViewport;
    private RhinoViewport m_viewport;
    private readonly uint m_flags;
    private readonly Point3d m_point;
    private readonly System.Drawing.Point m_windowPoint;
    private readonly GetPoint m_source;

    internal GetPointMouseEventArgs(GetPoint source, IntPtr pRhinoViewport, uint flags, Point3d point, System.Drawing.Point wndPoint)
    {
      m_pRhinoViewport = pRhinoViewport;
      m_flags = flags;
      m_point = point;
      m_windowPoint = wndPoint;
      m_source = source;
    }

    /// <since>5.0</since>
    public GetPoint Source { get { return m_source; } }

    /// <since>5.0</since>
    public RhinoViewport Viewport
    {
      get { return m_viewport ?? (m_viewport = new RhinoViewport(null, m_pRhinoViewport)); }
    }
    /// <since>5.0</since>
    public Point3d Point
    {
      get{ return m_point; }
    }
    /// <since>5.0</since>
    public System.Drawing.Point WindowPoint
    {
      get{ return m_windowPoint; }
    }

    // from winuser.h
    const int MK_LBUTTON = 0x0001;
    const int MK_RBUTTON = 0x0002;
    const int MK_SHIFT = 0x0004;
    const int MK_CONTROL = 0x0008;
    const int MK_MBUTTON = 0x0010;

    /// <since>5.0</since>
    public bool LeftButtonDown
    {
      get
      {
        return (m_flags & MK_LBUTTON) == MK_LBUTTON;
      }
    }
    /// <since>5.0</since>
    public bool RightButtonDown
    {
      get
      {
        return (m_flags & MK_RBUTTON) == MK_RBUTTON;
      }
    }
    /// <since>5.0</since>
    public bool ShiftKeyDown
    {
      get
      {
        return (m_flags & MK_SHIFT) == MK_SHIFT;
      }
    }
    /// <since>5.0</since>
    public bool ControlKeyDown
    {
      get
      {
        return (m_flags & MK_CONTROL) == MK_CONTROL;
      }
    }
    /// <since>5.0</since>
    public bool MiddleButtonDown
    {
      get
      {
        return (m_flags & MK_MBUTTON) == MK_MBUTTON;
      }
    }
  }
}


#endif
