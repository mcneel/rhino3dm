using System;
using System.Runtime.InteropServices;
using Rhino.Runtime;

namespace Rhino.Geometry
{
  // look at System.Windows.Media.Media3d.Matrix3D structure to help in laying this structure out
  /// <summary>
  /// Represents the values in a 4x4 transform matrix.
  /// <para>This is parallel to C++ ON_Xform.</para>
  /// </summary>
  [StructLayout(LayoutKind.Sequential, Pack = 8, Size = 128)]
  [Serializable]
  public struct Transform : IComparable<Transform>, IEquatable<Transform>, ICloneable
  {
    #region members
    internal double m_00, m_01, m_02, m_03;
    internal double m_10, m_11, m_12, m_13;
    internal double m_20, m_21, m_22, m_23;
    internal double m_30, m_31, m_32, m_33;
    #endregion

    #region constructors
    /// <summary>
    /// Initializes a new transform matrix with a specified value along the diagonal.
    /// </summary>
    /// <param name="diagonalValue">Value to assign to all diagonal cells except M33 which is set to 1.0.</param>
    /// <since>5.0</since>
    public Transform(double diagonalValue)
      : this()
    {
      m_00 = diagonalValue;
      m_11 = diagonalValue;
      m_22 = diagonalValue;
      m_33 = 1.0;
    }

    /// <summary>
    /// Initializes a new transform matrix with a specified value.
    /// </summary>
    /// <param name="value">Value to assign to all cells.</param>
    /// <since>6.0</since>
    public Transform(Transform value)
    {
      m_00 = value.m_00;
      m_01 = value.m_01;
      m_02 = value.m_02;
      m_03 = value.m_03;
      m_10 = value.m_10;
      m_11 = value.m_11;
      m_12 = value.m_12;
      m_13 = value.m_13;
      m_20 = value.m_20;
      m_21 = value.m_21;
      m_22 = value.m_22;
      m_23 = value.m_23;
      m_30 = value.m_30;
      m_31 = value.m_31;
      m_32 = value.m_32;
      m_33 = value.m_33;
    }

    /// <summary>
    /// Gets a new identity transform matrix. An identity matrix defines no transformation.
    /// </summary>
    /// <since>5.0</since>
    public static Transform Identity
    {
      get
      {
        Transform xf = new Transform();
        xf.m_00 = 1.0;
        xf.m_11 = 1.0;
        xf.m_22 = 1.0;
        xf.m_33 = 1.0;
        return xf;
      }
    }

    /// <summary>
    /// ZeroTransformation diagonal = (0,0,0,1)
    /// </summary>
    /// <since>6.1</since>
    public static Transform ZeroTransformation
    {
      get
      {
        Transform xf = new Transform();
        xf.m_33 = 1.0;
        return xf;
      }
    }

    /// <summary>
    /// Gets an XForm filled with RhinoMath.UnsetValue.
    /// </summary>
    /// <since>5.0</since>
    public static Transform Unset
    {
      get
      {
        Transform xf = new Transform();
        xf.m_00 = RhinoMath.UnsetValue;
        xf.m_01 = RhinoMath.UnsetValue;
        xf.m_02 = RhinoMath.UnsetValue;
        xf.m_03 = RhinoMath.UnsetValue;
        xf.m_10 = RhinoMath.UnsetValue;
        xf.m_11 = RhinoMath.UnsetValue;
        xf.m_12 = RhinoMath.UnsetValue;
        xf.m_13 = RhinoMath.UnsetValue;
        xf.m_20 = RhinoMath.UnsetValue;
        xf.m_21 = RhinoMath.UnsetValue;
        xf.m_22 = RhinoMath.UnsetValue;
        xf.m_23 = RhinoMath.UnsetValue;
        xf.m_30 = RhinoMath.UnsetValue;
        xf.m_31 = RhinoMath.UnsetValue;
        xf.m_32 = RhinoMath.UnsetValue;
        xf.m_33 = RhinoMath.UnsetValue;
        return xf;
      }
    }
    #endregion

    #region static constructors

    /// <summary>
    /// Create rotation transformation From Tait-Byran angles (also loosely known as Euler angles).
    /// </summary>
    /// <param name="yaw">Angle, in radians, to rotate about the Z axis.</param>
    /// <param name="pitch">Angle, in radians, to rotate about the Y axis.</param>
    /// <param name="roll">Angle, in radians, to rotate about the X axis.</param>
    /// <returns>A transform matrix from Tait-Byran angles.</returns>
    /// <remarks>
    /// RotationZYX(yaw, pitch, roll) = R_z(yaw) * R_y(pitch) * R_x(roll)
    /// where R_*(angle) is rotation of angle radians about the corresponding world coordinate axis.
    /// </remarks>
    /// <since>6.11</since>
    public static Transform RotationZYX(double yaw, double pitch, double roll)
    {
      Transform xf = Identity;
      UnsafeNativeMethods.ON_Xform_RotationZYX(ref xf, yaw, pitch, roll);
      return xf;
    }

    /// <summary>
    /// Create rotation transformation From Euler angles.
    /// </summary>
    /// <param name="alpha">Angle, in radians, to rotate about the Z axis.</param>
    /// <param name="beta">Angle, in radians, to rotate about the Y axis.</param>
    /// <param name="gamma">Angle, in radians, to rotate about the X axis.</param>
    /// <returns>A transform matrix from Euler angles.</returns>
    /// <remarks>
    /// RotationZYZ(alpha, beta, gamma) = R_z(alpha) * R_y(beta) * R_z(gamma)
    /// where R_*(angle) is rotation of angle radians about the corresponding *-world coordinate axis.
    /// Note, alpha and gamma are in the range (-pi, pi] while beta in the range [0, pi]
    /// </remarks>
    /// <since>6.11</since>
    public static Transform RotationZYZ(double alpha, double beta, double gamma)
    {
      Transform xf = Identity;
      UnsafeNativeMethods.ON_Xform_RotationZYZ(ref xf, alpha, beta, gamma);
      return xf;
    }

    /// <summary>
    /// Constructs a new translation (move) transformation. 
    /// </summary>
    /// <param name="motion">Translation (motion) vector.</param>
    /// <returns>A transform matrix which moves geometry along the motion vector.</returns>
    /// <example>
    /// <code source='examples\vbnet\ex_constrainedcopy.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_constrainedcopy.cs' lang='cs'/>
    /// <code source='examples\py\ex_constrainedcopy.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    public static Transform Translation(Vector3d motion)
    {
      return Translation(motion.m_x, motion.m_y, motion.m_z);
    }

    /// <summary>
    /// Constructs a new translation (move) transformation. 
    /// Right column is (dx, dy, dz, 1.0).
    /// </summary>
    /// <param name="dx">Distance to translate (move) geometry along the world X axis.</param>
    /// <param name="dy">Distance to translate (move) geometry along the world Y axis.</param>
    /// <param name="dz">Distance to translate (move) geometry along the world Z axis.</param>
    /// <returns>A transform matrix which moves geometry with the specified distances.</returns>
    /// <example>
    /// <code source='examples\vbnet\ex_transformbrep.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_transformbrep.cs' lang='cs'/>
    /// <code source='examples\py\ex_transformbrep.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    public static Transform Translation(double dx, double dy, double dz)
    {
      Transform xf = Identity;
      xf.m_03 = dx;
      xf.m_13 = dy;
      xf.m_23 = dz;
      xf.m_33 = 1.0;
      return xf;
    }

    /// <summary>
    /// Constructs a new transformation with diagonal (d0,d1,d2,1.0).
    /// </summary>
    /// <param name="diagonal">The diagonal values.</param>
    /// <returns>A transformation with diagonal (d0,d1,d2,1.0).</returns>
    /// <since>6.12</since>
    public static Transform Diagonal(Vector3d diagonal)
    {
      return Diagonal(diagonal.m_x, diagonal.m_y, diagonal.m_z);
    }

    /// <summary>
    /// Constructs a new transformation with diagonal (d0,d1,d2,1.0).
    /// </summary>
    /// <param name="d0">Transform.M00 value.</param>
    /// <param name="d1">Transform.M11 value.</param>
    /// <param name="d2">Transform.M22 value.</param>
    /// <returns>A transformation with diagonal (d0,d1,d2,1.0).</returns>
    /// <since>6.12</since>
    public static Transform Diagonal(double d0, double d1, double d2)
    {
      Transform xf = Identity;
      xf.m_00 = d0;
      xf.m_11 = d1;
      xf.m_22 = d2;
      return xf;
    }

    /// <summary>
    /// Constructs a new uniform scaling transformation with a specified scaling anchor point.
    /// </summary>
    /// <param name="anchor">Defines the anchor point of the scaling operation.</param>
    /// <param name="scaleFactor">Scaling factor in all directions.</param>
    /// <returns>A transform matrix which scales geometry uniformly around the anchor point.</returns>
    /// <since>5.0</since>
    public static Transform Scale(Point3d anchor, double scaleFactor)
    {
      return Scale(new Plane(anchor, new Vector3d(1, 0, 0), new Vector3d(0, 1, 0)), scaleFactor, scaleFactor, scaleFactor);
    }

    /// <summary>
    /// Constructs a new non-uniform scaling transformation with a specified scaling anchor point.
    /// </summary>
    /// <param name="plane">Defines the center and orientation of the scaling operation.</param>
    /// <param name="xScaleFactor">Scaling factor along the anchor plane X-Axis direction.</param>
    /// <param name="yScaleFactor">Scaling factor along the anchor plane Y-Axis direction.</param>
    /// <param name="zScaleFactor">Scaling factor along the anchor plane Z-Axis direction.</param>
    /// <returns>A transformation matrix which scales geometry non-uniformly.</returns>
    /// <since>5.0</since>
    public static Transform Scale(Plane plane, double xScaleFactor, double yScaleFactor, double zScaleFactor)
    {
      Transform xf = Identity;
      UnsafeNativeMethods.ON_Xform_Scale(ref xf, ref plane, xScaleFactor, yScaleFactor, zScaleFactor);
      return xf;
    }

    /// <summary>
    /// Constructs a new rotation transformation with specified angle, rotation center and rotation axis.
    /// </summary>
    /// <param name="sinAngle">Sin of the rotation angle.</param>
    /// <param name="cosAngle">Cos of the rotation angle.</param>
    /// <param name="rotationAxis">Axis direction of rotation.</param>
    /// <param name="rotationCenter">Center point of rotation.</param>
    /// <returns>A transformation matrix which rotates geometry around an anchor point.</returns>
    /// <since>5.0</since>
    public static Transform Rotation(double sinAngle, double cosAngle, Vector3d rotationAxis, Point3d rotationCenter)
    {
      Transform xf = Identity;
      UnsafeNativeMethods.ON_Xform_Rotation(ref xf, sinAngle, cosAngle, rotationAxis, rotationCenter);
      return xf;
    }

    /// <summary>
    /// Constructs a new rotation transformation with specified angle and rotation center.
    /// </summary>
    /// <param name="angleRadians">Angle (in Radians) of the rotation.</param>
    /// <param name="rotationCenter">Center point of rotation. Rotation axis is vertical.</param>
    /// <returns>A transformation matrix which rotates geometry around an anchor point.</returns>
    /// <since>5.0</since>
    public static Transform Rotation(double angleRadians, Point3d rotationCenter)
    {
      return Rotation(angleRadians, new Vector3d(0, 0, 1), rotationCenter);
    }

    /// <summary>
    /// Constructs a new rotation transformation with specified angle, rotation center and rotation axis.
    /// </summary>
    /// <param name="angleRadians">Angle (in Radians) of the rotation.</param>
    /// <param name="rotationAxis">Axis direction of rotation operation.</param>
    /// <param name="rotationCenter">Center point of rotation. Rotation axis is vertical.</param>
    /// <returns>A transformation matrix which rotates geometry around an anchor point.</returns>
    /// <since>5.0</since>
    public static Transform Rotation(double angleRadians, Vector3d rotationAxis, Point3d rotationCenter)
    {
      return Rotation(Math.Sin(angleRadians), Math.Cos(angleRadians), rotationAxis, rotationCenter);
    }

    /// <summary>
    /// Constructs a new rotation transformation with start and end directions and rotation center.
    /// </summary>
    /// <param name="startDirection">A start direction.</param>
    /// <param name="endDirection">An end direction.</param>
    /// <param name="rotationCenter">A rotation center.</param>
    /// <returns>A transformation matrix which rotates geometry around an anchor point.</returns>
    /// <since>5.0</since>
    public static Transform Rotation(Vector3d startDirection, Vector3d endDirection, Point3d rotationCenter)
    {
      if (Math.Abs(startDirection.Length - 1.0) > RhinoMath.SqrtEpsilon)
        startDirection.Unitize();
      if (Math.Abs(endDirection.Length - 1.0) > RhinoMath.SqrtEpsilon)
        endDirection.Unitize();
      double cos_angle = startDirection * endDirection;
      Vector3d axis = Vector3d.CrossProduct(startDirection, endDirection);
      double sin_angle = axis.Length;
      if (0.0 == sin_angle || !axis.Unitize())
      {
        axis.PerpendicularTo(startDirection);
        axis.Unitize();
        sin_angle = 0.0;
        cos_angle = (cos_angle < 0.0) ? -1.0 : 1.0;
      }
      return Rotation(sin_angle, cos_angle, axis, rotationCenter);
    }

    /// <summary>
    /// Constructs a transformation that maps X0 to X1, Y0 to Y1, Z0 to Z1.
    /// </summary>
    /// <param name="x0">First "from" vector.</param>
    /// <param name="y0">Second "from" vector.</param>
    /// <param name="z0">Third "from" vector.</param>
    /// <param name="x1">First "to" vector.</param>
    /// <param name="y1">Second "to" vector.</param>
    /// <param name="z1">Third "to" vector.</param>
    /// <returns>A rotation transformation value.</returns>
    /// <since>5.0</since>
    public static Transform Rotation(Vector3d x0, Vector3d y0, Vector3d z0,
      Vector3d x1, Vector3d y1, Vector3d z1)
    {
      // F0 changes x0,y0,z0 to world X,Y,Z
      Transform F0 = new Transform();
      F0[0, 0] = x0.X; F0[0, 1] = x0.Y; F0[0, 2] = x0.Z;
      F0[1, 0] = y0.X; F0[1, 1] = y0.Y; F0[1, 2] = y0.Z;
      F0[2, 0] = z0.X; F0[2, 1] = z0.Y; F0[2, 2] = z0.Z;
      F0[3, 3] = 1.0;

      // F1 changes world X,Y,Z to x1,y1,z1
      Transform F1 = new Transform();
      F1[0, 0] = x1.X; F1[0, 1] = y1.X; F1[0, 2] = z1.X;
      F1[1, 0] = x1.Y; F1[1, 1] = y1.Y; F1[1, 2] = z1.Y;
      F1[2, 0] = x1.Z; F1[2, 1] = y1.Z; F1[2, 2] = z1.Z;
      F1[3, 3] = 1.0;

      return F1 * F0;
    }

    /// <summary>
    /// Create mirror transformation matrix
    /// The mirror transform maps a point Q to 
    /// Q - (2*(Q-P)oN)*N, where
    /// P = pointOnMirrorPlane and N = normalToMirrorPlane.
    /// </summary>
    /// <param name="pointOnMirrorPlane">Point on the mirror plane.</param>
    /// <param name="normalToMirrorPlane">Normal vector to the mirror plane.</param>
    /// <returns>A transformation matrix which mirrors geometry in a specified plane.</returns>
    /// <since>5.0</since>
    public static Transform Mirror(Point3d pointOnMirrorPlane, Vector3d normalToMirrorPlane)
    {
      Transform xf = new Transform();
      UnsafeNativeMethods.ON_Xform_Mirror(ref xf, pointOnMirrorPlane, normalToMirrorPlane);
      return xf;
    }

    /// <summary>
    /// Constructs a new Mirror transformation.
    /// </summary>
    /// <param name="mirrorPlane">Plane that defines the mirror orientation and position.</param>
    /// <returns>A transformation matrix which mirrors geometry in a specified plane.</returns>
    /// <since>5.0</since>
    public static Transform Mirror(Plane mirrorPlane)
    {
      return Mirror(mirrorPlane.Origin, mirrorPlane.ZAxis);
    }

    /// <summary>
    /// Computes a change of basis transformation. A basis change is essentially a remapping 
    /// of geometry from one coordinate system to another.
    /// </summary>
    /// <param name="plane0">Coordinate system in which the geometry is currently described.</param>
    /// <param name="plane1">Target coordinate system in which we want the geometry to be described.</param>
    /// <returns>
    /// A transformation matrix which orients geometry from one coordinate system to another on success.
    /// Transform.Unset on failure.
    /// </returns>
    /// <since>5.0</since>
    public static Transform ChangeBasis(Plane plane0, Plane plane1)
    {
      Transform rc = Transform.Identity;
      bool success = UnsafeNativeMethods.ON_Xform_PlaneToPlane(ref rc, ref plane0, ref plane1, false);
      return success ? rc : Transform.Unset;
    }

    /// <summary>
    /// Create a rotation transformation that orients plane0 to plane1. If you want to orient objects from
    /// one plane to another, use this form of transformation.
    /// </summary>
    /// <param name="plane0">The plane to orient from.</param>
    /// <param name="plane1">the plane to orient to.</param>
    /// <returns>The translation transformation if successful, Transform.Unset on failure.</returns>
    /// <since>5.0</since>
    public static Transform PlaneToPlane(Plane plane0, Plane plane1)
    {
      Transform rc = Transform.Identity;
      UnsafeNativeMethods.ON_Xform_PlaneToPlane(ref rc, ref plane0, ref plane1, true);
      return rc;
    }

    /// <summary>
    /// Computes a change of basis transformation. A basis change is essentially a remapping 
    /// of geometry from one coordinate system to another.
    /// </summary>
    /// <param name="initialBasisX">can be any 3d basis.</param>
    /// <param name="initialBasisY">can be any 3d basis.</param>
    /// <param name="initialBasisZ">can be any 3d basis.</param>
    /// <param name="finalBasisX">can be any 3d basis.</param>
    /// <param name="finalBasisY">can be any 3d basis.</param>
    /// <param name="finalBasisZ">can be any 3d basis.</param>
    /// <returns>
    /// A transformation matrix which orients geometry from one coordinate system to another on success.
    /// Transform.Unset on failure.
    /// </returns>
    /// <since>5.0</since>
    public static Transform ChangeBasis(Vector3d initialBasisX, Vector3d initialBasisY, Vector3d initialBasisZ,
      Vector3d finalBasisX, Vector3d finalBasisY, Vector3d finalBasisZ)
    {
      Transform rc = Transform.Identity;
      bool success = UnsafeNativeMethods.ON_Xform_ChangeBasis2(ref rc,
        initialBasisX, initialBasisY, initialBasisZ, finalBasisX, finalBasisY, finalBasisZ);
      return success ? rc : Transform.Unset;
    }

    /// <summary>
    /// Constructs a projection transformation.
    /// </summary>
    /// <param name="plane">Plane onto which everything will be perpendicularly projected.</param>
    /// <returns>A transformation matrix which projects geometry onto a specified plane.</returns>
    /// <since>5.0</since>
    public static Transform PlanarProjection(Plane plane)
    {
      Transform rc = Transform.Identity;
      UnsafeNativeMethods.ON_Xform_PlanarProjection(ref rc, ref plane);
      return rc;
    }
    /// <summary>
    /// Construct a projection onto a plane along a specific direction.
    /// </summary>
    /// <param name="plane">Plane to project onto.</param>
    /// <param name="direction">Projection direction, must not be parallel to the plane.</param>
    /// <returns>Projection transformation or identity transformation if projection could not be calculated.</returns>
    /// <since>6.0</since>
    public static Transform ProjectAlong(Plane plane, Vector3d direction)
    {
      if (!plane.IsValid || !direction.IsValid || direction.IsZero)
        return Identity;

      if (plane.ZAxis.IsPerpendicularTo(direction, RhinoMath.ToRadians(0.01)))
        return Identity;

      if (plane.ZAxis.IsParallelTo(direction, RhinoMath.ToRadians(0.01)) != 0)
        return PlanarProjection(plane);

      Plane plane0 = plane;
      Plane plane1 = new Plane(plane.Origin, direction);

      Line axis;
      if (!Intersect.Intersection.PlanePlane(plane0, plane1, out axis))
        return PlanarProjection(plane);

      Plane plane2 = plane0;
      plane2.XAxis = axis.UnitTangent;
      plane2.YAxis = Vector3d.CrossProduct(plane2.XAxis, plane2.ZAxis);
      plane2.ZAxis = Vector3d.CrossProduct(plane2.YAxis, plane2.XAxis);

      double angle0 = Vector3d.VectorAngle(plane0.ZAxis, direction);
      double angle1 = Math.Sin(0.5 * Math.PI - angle0);
      if (Math.Abs(angle1) < 1e-64)
        return Identity;

      Transform projection = PlanarProjection(plane1);
      Transform rotation = Rotation(plane1.ZAxis, plane0.ZAxis, plane0.Origin);
      Transform scaling = Scale(plane2, 1.0, 1.0 / angle1, 1.0);

      return scaling * rotation * projection;
    }

    /// <summary>
    /// Constructs a Shear transformation.
    /// </summary>
    /// <param name="plane">Base plane for shear.</param>
    /// <param name="x">Shearing vector along plane x-axis.</param>
    /// <param name="y">Shearing vector along plane y-axis.</param>
    /// <param name="z">Shearing vector along plane z-axis.</param>
    /// <returns>A transformation matrix which shear geometry.</returns>
    /// <since>5.0</since>
    public static Transform Shear(Plane plane, Vector3d x, Vector3d y, Vector3d z)
    {
      Transform rc = Transform.Identity;
      UnsafeNativeMethods.ON_Xform_Shear(ref rc, ref plane, x, y, z);
      return rc;
    }

    // TODO: taper.
    #endregion

    #region operators
    /// <summary>
    /// Determines if two transformations are equal in value.
    /// </summary>
    /// <param name="a">A transform.</param>
    /// <param name="b">Another transform.</param>
    /// <returns>true if transforms are equal; otherwise false.</returns>
    /// <since>5.0</since>
    public static bool operator ==(Transform a, Transform b)
    {
      return a.m_00 == b.m_00 && a.m_01 == b.m_01 && a.m_02 == b.m_02 && a.m_03 == b.m_03 &&
        a.m_10 == b.m_10 && a.m_11 == b.m_11 && a.m_12 == b.m_12 && a.m_13 == b.m_13 &&
        a.m_20 == b.m_20 && a.m_21 == b.m_21 && a.m_22 == b.m_22 && a.m_23 == b.m_23 &&
        a.m_30 == b.m_30 && a.m_31 == b.m_31 && a.m_32 == b.m_32 && a.m_33 == b.m_33;
    }

    /// <summary>
    /// Determines if two transformations are different in value.
    /// </summary>
    /// <param name="a">A transform.</param>
    /// <param name="b">Another transform.</param>
    /// <returns>true if transforms are different; otherwise false.</returns>
    /// <since>5.0</since>
    public static bool operator !=(Transform a, Transform b)
    {
      return a.m_00 != b.m_00 || a.m_01 != b.m_01 || a.m_02 != b.m_02 || a.m_03 != b.m_03 ||
        a.m_10 != b.m_10 || a.m_11 != b.m_11 || a.m_12 != b.m_12 || a.m_13 != b.m_13 ||
        a.m_20 != b.m_20 || a.m_21 != b.m_21 || a.m_22 != b.m_22 || a.m_23 != b.m_23 ||
        a.m_30 != b.m_30 || a.m_31 != b.m_31 || a.m_32 != b.m_32 || a.m_33 != b.m_33;
    }

    /// <summary>
    /// Multiplies (combines) two transformations.
    /// </summary>
    /// <param name="a">First transformation.</param>
    /// <param name="b">Second transformation.</param>
    /// <returns>A transformation matrix that combines the effect of both input transformations. 
    /// The resulting Transform gives the same result as though you'd first apply A then B.</returns>
    /// <since>5.0</since>
    public static Transform operator *(Transform a, Transform b)
    {
      Transform xf = new Transform();
      xf.m_00 = a.m_00 * b.m_00 + a.m_01 * b.m_10 + a.m_02 * b.m_20 + a.m_03 * b.m_30;
      xf.m_01 = a.m_00 * b.m_01 + a.m_01 * b.m_11 + a.m_02 * b.m_21 + a.m_03 * b.m_31;
      xf.m_02 = a.m_00 * b.m_02 + a.m_01 * b.m_12 + a.m_02 * b.m_22 + a.m_03 * b.m_32;
      xf.m_03 = a.m_00 * b.m_03 + a.m_01 * b.m_13 + a.m_02 * b.m_23 + a.m_03 * b.m_33;

      xf.m_10 = a.m_10 * b.m_00 + a.m_11 * b.m_10 + a.m_12 * b.m_20 + a.m_13 * b.m_30;
      xf.m_11 = a.m_10 * b.m_01 + a.m_11 * b.m_11 + a.m_12 * b.m_21 + a.m_13 * b.m_31;
      xf.m_12 = a.m_10 * b.m_02 + a.m_11 * b.m_12 + a.m_12 * b.m_22 + a.m_13 * b.m_32;
      xf.m_13 = a.m_10 * b.m_03 + a.m_11 * b.m_13 + a.m_12 * b.m_23 + a.m_13 * b.m_33;

      xf.m_20 = a.m_20 * b.m_00 + a.m_21 * b.m_10 + a.m_22 * b.m_20 + a.m_23 * b.m_30;
      xf.m_21 = a.m_20 * b.m_01 + a.m_21 * b.m_11 + a.m_22 * b.m_21 + a.m_23 * b.m_31;
      xf.m_22 = a.m_20 * b.m_02 + a.m_21 * b.m_12 + a.m_22 * b.m_22 + a.m_23 * b.m_32;
      xf.m_23 = a.m_20 * b.m_03 + a.m_21 * b.m_13 + a.m_22 * b.m_23 + a.m_23 * b.m_33;

      xf.m_30 = a.m_30 * b.m_00 + a.m_31 * b.m_10 + a.m_32 * b.m_20 + a.m_33 * b.m_30;
      xf.m_31 = a.m_30 * b.m_01 + a.m_31 * b.m_11 + a.m_32 * b.m_21 + a.m_33 * b.m_31;
      xf.m_32 = a.m_30 * b.m_02 + a.m_31 * b.m_12 + a.m_32 * b.m_22 + a.m_33 * b.m_32;
      xf.m_33 = a.m_30 * b.m_03 + a.m_31 * b.m_13 + a.m_32 * b.m_23 + a.m_33 * b.m_33;
      return xf;
    }

    /// <summary>
    /// Multiplies a transformation by a point and gets a new point.
    /// </summary>
    /// <param name="m">A transformation.</param>
    /// <param name="p">A point.</param>
    /// <returns>The transformed point.</returns>
    /// <since>5.0</since>
    public static Point3d operator *(Transform m, Point3d p)
    {
      double x = p.m_x; // optimizer should put x,y,z in registers
      double y = p.m_y;
      double z = p.m_z;
      Point3d rc = new Point3d();
      rc.m_x = m.m_00 * x + m.m_01 * y + m.m_02 * z + m.m_03;
      rc.m_y = m.m_10 * x + m.m_11 * y + m.m_12 * z + m.m_13;
      rc.m_z = m.m_20 * x + m.m_21 * y + m.m_22 * z + m.m_23;
      double w = m.m_30 * x + m.m_31 * y + m.m_32 * z + m.m_33;
      if (w != 0.0)
      {
        w = 1.0 / w;
        rc.m_x *= w;
        rc.m_y *= w;
        rc.m_z *= w;
      }
      return rc;
    }

    /// <summary>
    /// Multiplies a transformation by a vector and gets a new vector.
    /// </summary>
    /// <param name="m">A transformation.</param>
    /// <param name="v">A vector.</param>
    /// <returns>The transformed vector.</returns>
    /// <since>5.0</since>
    public static Vector3d operator *(Transform m, Vector3d v)
    {
      double x = v.m_x; // optimizer should put x,y,z in registers
      double y = v.m_y;
      double z = v.m_z;
      Vector3d rc = new Vector3d();
      rc.m_x = m.m_00 * x + m.m_01 * y + m.m_02 * z;
      rc.m_y = m.m_10 * x + m.m_11 * y + m.m_12 * z;
      rc.m_z = m.m_20 * x + m.m_21 * y + m.m_22 * z;
      return rc;
    }

    /// <summary>
    /// Multiplies (combines) two transformations.
    /// <para>This is the same as the * operator between two transformations.</para>
    /// </summary>
    /// <param name="a">First transformation.</param>
    /// <param name="b">Second transformation.</param>
    /// <returns>A transformation matrix that combines the effect of both input transformations. 
    /// The resulting Transform gives the same result as though you'd first apply B then A.</returns>
    /// <since>5.0</since>
    public static Transform Multiply(Transform a, Transform b)
    {
      return a * b;
    }
    #endregion

    #region properties
    #region accessor properties
    /// <summary>Gets or sets this[0,0].</summary>
    /// <since>5.0</since>
    public double M00 { get { return m_00; } set { m_00 = value; } }
    /// <summary>Gets or sets this[0,1].</summary>
    /// <since>5.0</since>
    public double M01 { get { return m_01; } set { m_01 = value; } }
    /// <summary>Gets or sets this[0,2].</summary>
    /// <since>5.0</since>
    public double M02 { get { return m_02; } set { m_02 = value; } }
    /// <summary>Gets or sets this[0,3].</summary>
    /// <since>5.0</since>
    public double M03 { get { return m_03; } set { m_03 = value; } }

    /// <summary>Gets or sets this[1,0].</summary>
    /// <since>5.0</since>
    public double M10 { get { return m_10; } set { m_10 = value; } }
    /// <summary>Gets or sets this[1,1].</summary>
    /// <since>5.0</since>
    public double M11 { get { return m_11; } set { m_11 = value; } }
    /// <summary>Gets or sets this[1,2].</summary>
    /// <since>5.0</since>
    public double M12 { get { return m_12; } set { m_12 = value; } }
    /// <summary>Gets or sets this[1,3].</summary>
    /// <since>5.0</since>
    public double M13 { get { return m_13; } set { m_13 = value; } }

    /// <summary>Gets or sets this[2,0].</summary>
    /// <since>5.0</since>
    public double M20 { get { return m_20; } set { m_20 = value; } }
    /// <summary>Gets or sets this[2,1].</summary>
    /// <since>5.0</since>
    public double M21 { get { return m_21; } set { m_21 = value; } }
    /// <summary>Gets or sets this[2,2].</summary>
    /// <since>5.0</since>
    public double M22 { get { return m_22; } set { m_22 = value; } }
    /// <summary>Gets or sets this[2,3].</summary>
    /// <since>5.0</since>
    public double M23 { get { return m_23; } set { m_23 = value; } }

    /// <summary>Gets or sets this[3,0].</summary>
    /// <since>5.0</since>
    public double M30 { get { return m_30; } set { m_30 = value; } }
    /// <summary>Gets or sets this[3,1].</summary>
    /// <since>5.0</since>
    public double M31 { get { return m_31; } set { m_31 = value; } }
    /// <summary>Gets or sets this[3,2].</summary>
    /// <since>5.0</since>
    public double M32 { get { return m_32; } set { m_32 = value; } }
    /// <summary>Gets or sets this[3,3].</summary>
    /// <since>5.0</since>
    public double M33 { get { return m_33; } set { m_33 = value; } }

    /// <summary>
    /// Gets or sets the matrix value at the given row and column indices.
    /// </summary>
    /// <param name="row">Index of row to access, must be 0, 1, 2 or 3.</param>
    /// <param name="column">Index of column to access, must be 0, 1, 2 or 3.</param>
    /// <returns>The value at [row, column]</returns>
    /// <value>The new value at [row, column]</value>
    public double this[int row, int column]
    {
      get
      {
        if (row < 0) { throw new IndexOutOfRangeException("Negative row indices are not allowed when accessing a Transform matrix"); }
        if (row > 3) { throw new IndexOutOfRangeException("Row indices higher than 3 are not allowed when accessing a Transform matrix"); }
        if (column < 0) { throw new IndexOutOfRangeException("Negative column indices are not allowed when accessing a Transform matrix"); }
        if (column > 3) { throw new IndexOutOfRangeException("Column indices higher than 3 are not allowed when accessing a Transform matrix"); }

        if (row == 0)
        {
          if (column == 0) { return m_00; }
          if (column == 1) { return m_01; }
          if (column == 2) { return m_02; }
          if (column == 3) { return m_03; }
        }
        else if (row == 1)
        {
          if (column == 0) { return m_10; }
          if (column == 1) { return m_11; }
          if (column == 2) { return m_12; }
          if (column == 3) { return m_13; }
        }
        else if (row == 2)
        {
          if (column == 0) { return m_20; }
          if (column == 1) { return m_21; }
          if (column == 2) { return m_22; }
          if (column == 3) { return m_23; }
        }
        else if (row == 3)
        {
          if (column == 0) { return m_30; }
          if (column == 1) { return m_31; }
          if (column == 2) { return m_32; }
          if (column == 3) { return m_33; }
        }

        throw new IndexOutOfRangeException("One of the cross beams has gone out askew on the treadle.");
      }
      set
      {
        if (row < 0) { throw new IndexOutOfRangeException("Negative row indices are not allowed when accessing a Transform matrix"); }
        if (row > 3) { throw new IndexOutOfRangeException("Row indices higher than 3 are not allowed when accessing a Transform matrix"); }
        if (column < 0) { throw new IndexOutOfRangeException("Negative column indices are not allowed when accessing a Transform matrix"); }
        if (column > 3) { throw new IndexOutOfRangeException("Column indices higher than 3 are not allowed when accessing a Transform matrix"); }

        if (row == 0)
        {
          if (column == 0)
          { m_00 = value; }
          else if (column == 1)
          { m_01 = value; }
          else if (column == 2)
          { m_02 = value; }
          else if (column == 3)
          { m_03 = value; }
        }
        else if (row == 1)
        {
          if (column == 0)
          { m_10 = value; }
          else if (column == 1)
          { m_11 = value; }
          else if (column == 2)
          { m_12 = value; }
          else if (column == 3)
          { m_13 = value; }
        }
        else if (row == 2)
        {
          if (column == 0)
          { m_20 = value; }
          else if (column == 1)
          { m_21 = value; }
          else if (column == 2)
          { m_22 = value; }
          else if (column == 3)
          { m_23 = value; }
        }
        else if (row == 3)
        {
          if (column == 0)
          { m_30 = value; }
          else if (column == 1)
          { m_31 = value; }
          else if (column == 2)
          { m_32 = value; }
          else if (column == 3)
          { m_33 = value; }
        }
      }
    }
    #endregion

    /// <summary>Return true if this Transform is the identity transform</summary>
    /// <since>6.0</since>
    public bool IsIdentity
    {
      get { return this == Identity; }
    }

    /// <summary>
    /// Gets a value indicating whether or not this Transform is a valid matrix. 
    /// A valid transform matrix is not allowed to have any invalid numbers.
    /// </summary>
    /// <since>5.0</since>
    public bool IsValid
    {
      get
      {
        bool rc = RhinoMath.IsValidDouble(m_00) && RhinoMath.IsValidDouble(m_01) && RhinoMath.IsValidDouble(m_02) && RhinoMath.IsValidDouble(m_03) &&
                  RhinoMath.IsValidDouble(m_10) && RhinoMath.IsValidDouble(m_11) && RhinoMath.IsValidDouble(m_12) && RhinoMath.IsValidDouble(m_13) &&
                  RhinoMath.IsValidDouble(m_20) && RhinoMath.IsValidDouble(m_21) && RhinoMath.IsValidDouble(m_22) && RhinoMath.IsValidDouble(m_23) &&
                  RhinoMath.IsValidDouble(m_30) && RhinoMath.IsValidDouble(m_31) && RhinoMath.IsValidDouble(m_22) && RhinoMath.IsValidDouble(m_33);
        return rc;
      }
    }

    /// <summary>
    /// True if matrix is Zero4x4, ZeroTransformation, or some other type of
    /// zero. The value xform[3][3] can be anything.
    /// </summary>
    /// <since>6.1</since>
    public bool IsZero
    {
      get
      {
        for(int i=0; i<4; i++)
        {
          for(int j=0; j<4; j++)
          {
            if (3 == i && 3 == j)
              break;
            if (this[i, j] != 0.0)
              return false; // nonzero or nan
          }
        }
        return !double.IsNaN(m_33) && !double.IsInfinity(m_33);
      }
    }

    /// <summary>
    /// True if all values are 0
    /// </summary>
    /// <since>6.1</since>
    public bool IsZero4x4
    {
      get
      {
        return 0.0 == m_33 && IsZero;
      }
    }

    /// <summary>
    /// True if all values are 0, except for M33 which is 1.
    /// </summary>
    /// <seealso cref="Transform.IsZeroTransformationWithTolerance(double)"/>
    /// <since>6.1</since>
    public bool IsZeroTransformation
    {
      get
      {
        return 1.0 == m_33 && IsZero;
      }
    }

    /// <summary>
    /// True if all values are 0 within tolerance, except for M33 which is exactly 1.
    /// </summary>
    /// <param name="zeroTolerance">The zero tolerance.</param>
    /// <returns>Returns true if all values are 0 within tolerance, except for M33 which is exactly 1.</returns>
    /// <seealso cref="Transform.IsZeroTransformationWithTolerance(double)"/>
    /// <since>6.12</since>
    /// <deprecated>7.1</deprecated>
    [Obsolete("This method remains with a typo in its name for backwards compatibility. Use IsZeroTransformationWithTolerance instead.")]
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    [ConstOperation]
    public bool IsZeroTransformaton(double zeroTolerance)
    {
      return UnsafeNativeMethods.ON_Xform_IsZeroTransformation(ref this, zeroTolerance);
    }

    /// <summary>
    /// True if all values are 0 within tolerance, except for M33 which is exactly 1.
    /// </summary>
    /// <param name="zeroTolerance">The tolerance for 0 elements.</param>
    /// <returns>
    /// True if the transformation matrix is ON_Xform::ZeroTransformation, with xform[3][3] equal to 1:
    ///     0 0 0 0
    ///     0 0 0 0
    ///     0 0 0 0
    ///     0 0 0 1
    /// 
    /// An element x of the matrix is "zero" if fabs(x) ≤ zeroTolerance.
    /// IsZeroTransformation is the same as IsZeroTransformationWithTolerance(0.0)
    /// </returns>
    /// <since>7.1</since>
    [ConstOperation]
    public bool IsZeroTransformationWithTolerance(double zeroTolerance)
    {
      return UnsafeNativeMethods.ON_Xform_IsZeroTransformation(ref this, zeroTolerance);
    }

    /// <summary>
    /// Gets a value indicating whether or not the Transform maintains similarity. 
    /// The easiest way to think of Similarity is that any circle, when transformed, 
    /// remains a circle. Whereas a non-similarity Transform deforms circles into ellipses.
    /// </summary>
    /// <since>5.0</since>
    public TransformSimilarityType SimilarityType
    {
      get
      {
        int rc = UnsafeNativeMethods.ON_Xform_IsSimilarity(ref this);
        return (TransformSimilarityType)rc;
      }
    }

    /// <summary>
    /// Gets a value indicating whether or not the Transform maintains similarity. 
    /// A similarity transformation can be broken into a sequence of a dilation, translation, rotation, and a reflection.
    /// </summary>
    /// <param name="tolerance">The evaluation tolerance.</param>
    /// <returns>The similarity type.</returns>
    /// <since>6.12</since>
    [ConstOperation]
    public TransformSimilarityType IsSimilarity(double tolerance)
    {
      int rc = UnsafeNativeMethods.ON_Xform_IsSimilarity2(ref this, tolerance);
      return (TransformSimilarityType)rc;
    }

    /// <summary>
    /// Decomposes a similarity transformation. The transformation must be affine.
    /// A similarity transformation can be broken into a sequence of a dilation, translation, rotation, and a reflection.
    /// </summary>
    /// <param name="translation">Translation vector.</param>
    /// <param name="dilation">Dilation, where dilation lt; 0 if this is an orientation reversing similarity.</param>
    /// <param name="rotation">A proper rotation transformation, where R*Transpose(R)=I and Determinant(R)=1.</param>
    /// <param name="tolerance">The evaluation tolerance.</param>
    /// <returns>The similarity type.</returns>
    /// <remarks>
    /// If X.DecomposeSimilarity(T, d, R, tol) !=0 then X ~ Translation(T)*Diagonal(d)*R
		/// note when d gt;0 the transformation is orientation preserving.
    /// If dilation lt; 0 then Diagonal(dilation) is actually a reflection combined with a true dilation, or
    /// Diagonal(dilation) = Diagonal(-1) * Diagonal(|diagonal|).
    /// </remarks>
    /// <since>6.12</since>
    [ConstOperation]
    public TransformSimilarityType DecomposeSimilarity(out Vector3d translation, out double dilation, out Transform rotation, double tolerance)
    {
      translation = Vector3d.Unset;
      dilation = RhinoMath.UnsetValue;
      rotation = Unset;
      int rc = UnsafeNativeMethods.ON_Xform_DecomposeSimilarity(ref this, ref translation, ref dilation, ref rotation, tolerance);
      return (TransformSimilarityType)rc;
    }

    /// <summary>
    /// Gets a value indicating whether or not the Transform is rigid. 
    /// A rigid transformation can be broken into  a proper rotation and a translation,
    /// while an isometry transformation could also include a reflection.
    /// </summary>
    /// <since>6.12</since>
    public TransformRigidType RigidType
    {
      get
      {
        int rc = UnsafeNativeMethods.ON_Xform_IsRigid(ref this, RhinoMath.ZeroTolerance);
        return (TransformRigidType)rc;
      }
    }

    /// <summary>
    /// Gets a value indicating whether or not the Transform is rigid. 
    /// A rigid transformation can be broken into  a proper rotation and a translation,
    /// while an isometry transformation could also include a reflection.
    /// </summary>
    /// <param name="tolerance">The evaluation tolerance.</param>
    /// <returns>The rigid type.</returns>
    /// <since>6.12</since>
    [ConstOperation]
    public TransformRigidType IsRigid(double tolerance)
    {
      int rc = UnsafeNativeMethods.ON_Xform_IsRigid(ref this, tolerance);
      return (TransformRigidType)rc;
    }

    /// <summary>
    /// Decomposes a rigid transformation. The transformation must be affine.
    /// </summary>
    /// A rigid transformation can be broken into  a proper rotation and a translation,
    /// while an isometry transformation could also include a reflection.
    /// <param name="translation">Translation vector.</param>
    /// <param name="rotation">Proper rotation transformation, where R*Transpose(R)=I and det(R)=1.</param>
    /// <param name="tolerance">The evaluation tolerance.</param>
    /// <returns>The rigid type.</returns>
    /// <remarks>
    /// If X.DecomposeRigid(T, R) is 1, then X ~ Translation(T)*R.
    /// If X.DecomposeRigid(T, R) is -1, then X ~ Transform(-1) * Translation(T)*R.
    /// DecomposeRigid will find the closest rotation to the linear part of this transformation.
    /// </remarks>
    /// <since>6.12</since>
    [ConstOperation]
    public TransformRigidType DecomposeRigid(out Vector3d translation, out Transform rotation, double tolerance)
    {
      translation = Vector3d.Unset;
      rotation = Unset;
      int rc = UnsafeNativeMethods.ON_Xform_DecomposeRigid(ref this, ref translation, ref rotation, tolerance);
      return (TransformRigidType)rc;
    }

    /// <summary>
    /// Tests for an affine transformation.
    /// A transformation is affine if it is valid and its last row is [0, 0, 0, 1].
    /// An affine transformation can be broken into a linear transformation and a translation.
    /// </summary>
    /// <since>6.12</since>
    public bool IsAffine
    {
      get
      {
        return UnsafeNativeMethods.ON_Xform_IsAffine(ref this);
      }
    }

    /// <summary>
    /// Tests for a linear transformation.
    /// A transformation is affine if it is valid and its last row is [0, 0, 0, 1].
    /// If in addition its last column is ( 0, 0, 0, 1)^T then it is linear.
    /// An affine transformation can be broken into a linear transformation and a translation.
    /// </summary>
    /// <since>6.12</since>
    public bool IsLinear
    {
      get
      {
        return UnsafeNativeMethods.ON_Xform_IsLinear(ref this);
      }
    }

    /// <summary>
    /// Decomposes an affine transformation.
    /// A transformation is affine if it is valid and its last row is [0, 0, 0, 1].
    /// An affine transformation can be broken into a linear transformation and a translation.
    /// Note, a perspective transformation is not affine.
    /// </summary>
    /// <param name="translation">Translation vector.</param>
    /// <param name="linear">Linear transformation.</param>
    /// <returns>True if successful decomposition.</returns>
    /// <remarks>
    /// If X.DecomposeAffine(T, L) is true then X == Translation(T)*L.
    /// DecomposeAffine(T,L) succeeds for all affine transformations and is a simple copying of values.
    /// </remarks>
    /// <since>6.12</since>
    [ConstOperation]
    public bool DecomposeAffine(out Vector3d translation, out Transform linear)
    {
      translation = Vector3d.Unset;
      linear = Unset;
      return UnsafeNativeMethods.ON_Xform_DecomposeAffine(ref this, ref translation, ref linear);
    }

    /// <summary>
    /// Decomposes an affine transformation.
    /// A transformation is affine if it is valid and its last row is [0, 0, 0, 1].
    /// An affine transformation can be broken into a linear transformation and a translation.
    /// Note, a perspective transformation is not affine.
    /// </summary>
    /// <param name="linear">Linear transformation.</param>
    /// <param name="translation">Translation vector.</param>
    /// <returns>True if successful decomposition.</returns>
    /// <remarks>
    /// If X.DecomposeAffine(L, T) is true then X == L* Translation(T).
    /// DecomposeAffine(L, T) may fail for affine transformations if L is not invertible,
    /// and is more computationally expensive then X.DecomposeAffine(T, L).
    /// </remarks>
    /// <since>6.12</since>
    [ConstOperation]
    public bool DecomposeAffine(out Transform linear, out Vector3d translation)
    {
      linear = Unset;
      translation = Vector3d.Unset;
      return UnsafeNativeMethods.ON_Xform_DecomposeAffine2(ref this, ref linear, ref translation);
    }

    /// <summary>
		/// An affine transformation can be decomposed into a Symmetric, Rotation and Translation.
    /// Then the Symmetric component may be further decomposed as non-uniform scale in an orthonormal
    /// coordinate system.
    /// </summary>
    /// <param name="translation">Translation vector.</param>
    /// <param name="rotation">Proper rotation transformation.</param>
    /// <param name="orthogonal">Orthogonal basis.</param>
    /// <param name="diagonal">Diagonal elements of a Diagonal transformation.</param>
    /// <returns>True if successful decomposition.</returns>
    /// <since>6.12</since>
    [ConstOperation]
    public bool DecomposeAffine(out Vector3d translation, out Transform rotation, out Transform orthogonal, out Vector3d diagonal)
    {
      translation = Vector3d.Unset;
      rotation = Unset;
      orthogonal = Unset;
      diagonal = Vector3d.Unset;
      return UnsafeNativeMethods.ON_Xform_DecomposeAffine3(ref this, ref translation, ref rotation, ref orthogonal, ref diagonal);
    }

    /// <summary>
    /// Returns true if this is a proper rotation. 
    /// </summary>
    /// <since>6.12</since>
    public bool IsRotation
    {
      get
      {
        return UnsafeNativeMethods.ON_Xform_IsRotation(ref this);
      }
    }

    /// <summary>
    /// Replaces the last row with (0 0 0 1), discarding any perspective part of this transform
    /// </summary>
    /// <since>6.12</since>
    public void Affineize()
    {
      UnsafeNativeMethods.ON_Xform_Affineize(ref this);
    }

    /// <summary>
    /// Affinitize() and replaces the last column with (0 0 0 1)^T, discarding any translation part of this transform.
    /// </summary>
    /// <since>6.12</since>
    public void Linearize()
    {
      UnsafeNativeMethods.ON_Xform_Linearize(ref this);
    }

    /// <summary>
    /// Force the linear part of this transformation to be a rotation (or a rotation with reflection).
    /// Use DecomposeRigid(T,R) to find the nearest rotation.
    /// </summary>
    /// <param name="tolerance">The evaluation tolerance</param>
    /// <returns>true if successful, false otherwise.</returns>
    /// <since>6.12</since>
    public bool Orthogonalize(double tolerance)
    {
      return UnsafeNativeMethods.ON_Xform_Orthogonalize(ref this, tolerance);
    }

    /// <summary>
		/// A Symmetric linear transformation can be decomposed A = Q * Diag * Q ^ T, where Diag is a diagonal
    /// transformation. Diag[i][i] is an eigenvalue of A and the i-th column of Q is a corresponding
    /// unit length eigenvector. Note, this transformation must be Linear and Symmetric.
    /// </summary>
    /// <param name="matrix">An orthonormal matrix of eigenvectors (Q).</param>
    /// <param name="diagonal">A vector of eigenvalues.</param>
    /// <returns>true if successful, false otherwise.</returns>
    /// <remarks>
    /// If success, this== Q*Diagonal(diagonal) * QT, where QT == Q.Transpose().
    /// If L.IsLinear and LT==L.Transpose then LT*L is symmetric and is a common source of symmetric transformations.
    /// </remarks>
    /// <since>6.12</since>
    [ConstOperation]
    public bool DecomposeSymmetric(out Transform matrix, out Vector3d diagonal)
    {
      matrix = Unset;
      diagonal = Vector3d.Unset;
      return UnsafeNativeMethods.ON_Xform_DecomposeSymmetric(ref this, ref matrix, ref diagonal);
    }

    /// <summary>
    /// The determinant of this 4x4 matrix.
    /// </summary>
    /// <since>5.0</since>
    public double Determinant
    {
      get
      {
        return UnsafeNativeMethods.ON_Xform_Determinant(ref this);
      }
    }
    #endregion

    #region methods

    /// <summary>
    /// Find the Tait-Byran angles (also loosely called Euler angles) for a rotation transformation.
    /// </summary>
    /// <param name="yaw">Angle of rotation, in radians, about the Z axis.</param>
    /// <param name="pitch">Angle of rotation, in radians, about the Y axis.</param>
    /// <param name="roll">Angle of rotation, in radians, about the X axis.</param>
    /// <returns>If true, then RotationZYX(yaw, pitch, roll) = R_z(yaw) * R_y(pitch) * R_x(roll) 
    /// where R_*(angle) is rotation of angle radians about the corresponding world coordinate axis.
    /// If false, then this is not a rotation.
    /// </returns>
    /// <since>6.11</since>
    [ConstOperation]
    public bool GetYawPitchRoll(out double yaw, out double pitch, out double roll)
    {
      yaw = pitch = roll = RhinoMath.UnsetValue;
      return UnsafeNativeMethods.ON_Xform_GetYawPitchRoll(ref this, ref yaw, ref pitch, ref roll);
    }

    /// <summary>
    /// Find the Euler angles for a rotation transformation.
    /// </summary>
    /// <param name="alpha">Angle of rotation, in radians, about the Z axis.</param>
    /// <param name="beta">Angle of rotation, in radians, about the Y axis.</param>
    /// <param name="gamma">Angle of rotation, in radians, about the Z axis.</param>
    /// <returns>
    /// If true, then RotationZYZ(alpha, beta, gamma) = R_z(alpha) * R_y(beta) * R_z(gamma)
    /// where R_*(angle) is rotation of angle radians about the corresponding *-world coordinate axis.
    /// If false, then this is not a rotation.
    /// </returns>
    /// <remarks>
    /// Note, alpha and gamma are in the range (-pi, pi] while beta in the range [0, pi]
    /// </remarks>
    /// <since>6.11</since>
    [ConstOperation]
    public bool GetEulerZYZ(out double alpha, out double beta, out double gamma)
    {
      alpha = beta = gamma = RhinoMath.UnsetValue;
      return UnsafeNativeMethods.ON_Xform_GetEulerZYZ(ref this, ref alpha, ref beta, ref gamma);
    }

    /// <summary>
    /// Computes a new bounding box that is the smallest axis aligned
    /// bounding box that contains the transformed result of its 8 original corner
    /// points.
    /// </summary>
    /// <returns>A new bounding box.</returns>
    /// <since>5.0</since>
    [ConstOperation]
    public BoundingBox TransformBoundingBox(BoundingBox bbox)
    {
      BoundingBox rc = bbox;
      rc.Transform(this);
      return rc;
    }

    /// <summary>
    /// Given a list, an array or any enumerable set of points, computes a new array of transformed points.
    /// </summary>
    /// <param name="points">A list, an array or any enumerable set of points to be left untouched and copied.</param>
    /// <returns>A new array.</returns>
    /// <since>5.0</since>
    [ConstOperation]
    public Point3d[] TransformList(System.Collections.Generic.IEnumerable<Point3d> points)
    {
      System.Collections.Generic.List<Point3d> rc = new System.Collections.Generic.List<Point3d>(points);
      for (int i = 0; i < rc.Count; i++)
      {
        Point3d pt = rc[i];
        pt.Transform(this);
        rc[i] = pt;
      }
      return rc.ToArray();
    }

    /// <summary>
    /// Determines if another object is a transform and its value equals this transform value.
    /// </summary>
    /// <param name="obj">Another object.</param>
    /// <returns>true if obj is a transform and has the same value as this transform; otherwise, false.</returns>
    [ConstOperation]
    public override bool Equals(object obj)
    {
      return obj is Transform && Equals((Transform)obj);
    }

    /// <summary>
    /// Determines if another transform equals this transform value.
    /// </summary>
    /// <param name="other">Another transform.</param>
    /// <returns>true if other has the same value as this transform; otherwise, false.</returns>
    /// <since>5.0</since>
    [ConstOperation]
    public bool Equals(Transform other)
    {
      return this == other;
    }

    /// <summary>
    /// Gets a non-unique hashing code for this transform.
    /// </summary>
    /// <returns>A number that can be used to hash this transform in a dictionary.</returns>
    [ConstOperation]
    public override int GetHashCode()
    {
      // MSDN docs recommend XOR'ing the internal values to get a hash code
      return m_00.GetHashCode() ^ m_01.GetHashCode() ^ m_02.GetHashCode() ^ m_03.GetHashCode() ^
             m_10.GetHashCode() ^ m_11.GetHashCode() ^ m_12.GetHashCode() ^ m_13.GetHashCode() ^
             m_20.GetHashCode() ^ m_21.GetHashCode() ^ m_22.GetHashCode() ^ m_23.GetHashCode() ^
             m_30.GetHashCode() ^ m_31.GetHashCode() ^ m_32.GetHashCode() ^ m_33.GetHashCode();
    }

    /// <summary>
    /// Returns a string representation of this transform.
    /// </summary>
    /// <returns>A textual representation.</returns>
    [ConstOperation]
    public override string ToString()
    {
      System.Text.StringBuilder sb = new System.Text.StringBuilder();
      IFormatProvider provider = System.Globalization.CultureInfo.InvariantCulture;
      sb.AppendFormat("R0=({0},{1},{2},{3}),", m_00.ToString(provider), m_01.ToString(provider), m_02.ToString(provider), m_03.ToString(provider));
      sb.AppendFormat(" R1=({0},{1},{2},{3}),", m_10.ToString(provider), m_11.ToString(provider), m_12.ToString(provider), m_13.ToString(provider));
      sb.AppendFormat(" R2=({0},{1},{2},{3}),", m_20.ToString(provider), m_21.ToString(provider), m_22.ToString(provider), m_23.ToString(provider));
      sb.AppendFormat(" R3=({0},{1},{2},{3})", m_30.ToString(provider), m_31.ToString(provider), m_32.ToString(provider), m_33.ToString(provider));
      return sb.ToString();
    }
    /// <summary>
    /// Attempts to get the inverse transform of this transform.
    /// </summary>
    /// <param name="inverseTransform">The inverse transform. This out reference will be assigned during this call.</param>
    /// <returns>
    /// true on success. 
    /// If false is returned and this Transform is Invalid, inserveTransform will be set to this Transform. 
    /// If false is returned and this Transform is Valid, inverseTransform will be set to a pseudo inverse.
    /// </returns>
    /// <since>5.0</since>
    [ConstOperation]
    public bool TryGetInverse(out Transform inverseTransform)
    {
      inverseTransform = this;
      bool rc = false;
      if (IsValid)
        rc = UnsafeNativeMethods.ON_Xform_Invert(ref inverseTransform);
      return rc;
    }

    /// <summary>
    /// Flip row/column values
    /// </summary>
    /// <returns></returns>
    /// <since>5.9</since>
    [ConstOperation]
    public Transform Transpose()
    {
      Transform rc = new Transform();
      for (int r = 0; r < 4; r++)
      {
        for (int c = 0; c < 4; c++)
        {
          rc[r, c] = this[c, r];
        }
      }
      return rc;
    }

    /// <summary>
    /// Return the matrix as a linear array of 16 float values
    /// </summary>
    /// <param name="rowDominant"></param>
    /// <returns></returns>
    /// <since>5.9</since>
    [ConstOperation]
    public float[] ToFloatArray(bool rowDominant)
    {
      var rc = new float[16];

      if (rowDominant)
      {
        rc[0] = (float)m_00; rc[1] = (float)m_01; rc[2] = (float)m_02; rc[3] = (float)m_03;
        rc[4] = (float)m_10; rc[5] = (float)m_11; rc[6] = (float)m_12; rc[7] = (float)m_13;
        rc[8] = (float)m_20; rc[9] = (float)m_21; rc[10] = (float)m_22; rc[11] = (float)m_23;
        rc[12] = (float)m_30; rc[13] = (float)m_31; rc[14] = (float)m_32; rc[15] = (float)m_33;
      }
      else
      {
        rc[0] = (float)m_00; rc[1] = (float)m_10; rc[2] = (float)m_20; rc[3] = (float)m_30;
        rc[4] = (float)m_01; rc[5] = (float)m_11; rc[6] = (float)m_21; rc[7] = (float)m_31;
        rc[8] = (float)m_02; rc[9] = (float)m_12; rc[10] = (float)m_22; rc[11] = (float)m_32;
        rc[12] = (float)m_03; rc[13] = (float)m_13; rc[14] = (float)m_23; rc[15] = (float)m_33;
      }

      return rc;
    }

    /// <since>6.0</since>
    object ICloneable.Clone()
    {
      return Clone();
    }

    /// <summary>
    /// Returns a deep copy of the transform. For languages that treat structures as value types, this can 
    /// be accomplished by a simple assignment.
    /// </summary>
    /// <returns>A deep copy of this data structure.</returns>
    /// <since>6.0</since>
    [ConstOperation]
    public Transform Clone()
    {
      return new Transform(this);
    }

    #endregion

    /// <summary>
    /// Compares this transform with another transform.
    /// <para>M33 has highest value, then M32, etc..</para>
    /// </summary>
    /// <param name="other">Another transform.</param>
    /// <returns>-1 if this &lt; other; 0 if both are equal; 1 otherwise.</returns>
    /// <since>5.0</since>
    [ConstOperation]
    public int CompareTo(Transform other)
    {
      for (int i = 3; i >= 0; i--)
      {
        for (int j = 3; j >= 0; j--)
        {
          if (this[i, j] < other[i, j]) return -1;
          if (this[i, j] < other[i, j]) return 1;
        }
      }
      return 0;
    }
  }

  /// <summary>
  /// Lists all possible outcomes for transform similarity.
  /// </summary>
  /// <since>5.0</since>
  public enum TransformSimilarityType : int
  {
    /// <summary>
    /// Similarity is preserved, but orientation is flipped.
    /// </summary>
    OrientationReversing = -1,

    /// <summary>
    /// Similarity is not preserved. Geometry needs to be deformable for this Transform to operate correctly.
    /// </summary>
    NotSimilarity = 0,

    /// <summary>
    /// Similarity and orientation are preserved.
    /// </summary>
    OrientationPreserving = 1
  }

  /// <summary>
  /// Lists all possible outcomes for rigid transformation.
  /// </summary>
  /// <since>6.12</since>
  public enum TransformRigidType : int
  {
    /// <summary>
    /// Transformation is an orientation reversing isometry.
    /// </summary>
    RigidReversing = -1,

    /// <summary>
    /// Transformation is not an orthogonal transformation.
    /// </summary>
    NotRigid = 0,

    /// <summary>
    /// Transformation is an rigid transformation.
    /// </summary>
    Rigid = 1
  }


  //public class ON_ClippingRegion { }
  //public class ON_Localizer { }

#if RHINO_SDK
  class NativeSpaceMorphWrapper : SpaceMorph
  {
    internal IntPtr m_pSpaceMorph;
    public NativeSpaceMorphWrapper(IntPtr pSpaceMorph)
    {
      m_pSpaceMorph = pSpaceMorph;
      double tolerance = 0;
      bool quickpreview = false;
      bool preservestructure = true;
      if (UnsafeNativeMethods.ON_SpaceMorph_GetValues(pSpaceMorph, ref tolerance, ref quickpreview, ref preservestructure))
      {
        Tolerance = tolerance;
        QuickPreview = quickpreview;
        PreserveStructure = preservestructure;
      }
    }

    public override Point3d MorphPoint(Point3d point)
    {
      UnsafeNativeMethods.ON_SpaceMorph_MorphPoint(m_pSpaceMorph, ref point);
      return point;
    }
  }
#endif

  /// <summary>
  /// Represents a spacial, Euclidean morph.
  /// </summary>
  public abstract class SpaceMorph
  {
    private double m_tolerance;
    private bool m_bQuickPreview;
    private bool m_bPreserveStructure;

    #region from ON_Geometry - moved here to keep clutter out of geometry class
#if RHINO_SDK
    /// <summary>Apply the space morph to geometry.</summary>
    /// <param name="geometry">Geometry to morph.</param>
    /// <returns>true on success, false on failure.</returns>
    /// <since>5.0</since>
    public bool Morph(GeometryBase geometry)
    {
      return PerformGeometryMorph(geometry);
    }

    /// <summary>
    /// Apply the space morph to a plane.
    /// </summary>
    /// <param name="plane">Plane to morph.</param>
    /// <returns>true on success, false on failure.</returns>
    /// <since>6.0</since>
    public bool Morph(ref Plane plane)
    {
      return PerformPlaneMorph(ref plane);
    }

#endif

    /// <summary>
    /// true if the geometry can be morphed by calling SpaceMorph.Morph(geometry)
    /// </summary>
    /// <since>5.0</since>
    public static bool IsMorphable(GeometryBase geometry)
    {
      if (null == geometry)
        return false;
      IntPtr pGeometry = geometry.ConstPointer();
      bool rc = UnsafeNativeMethods.ON_Geometry_GetBool(pGeometry, GeometryBase.idxIsMorphable);
      GC.KeepAlive(geometry);
      return rc;
    }
    #endregion


    /// <summary>Morphs an Euclidean point. <para>This method is abstract.</para></summary>
    /// <param name="point">A point that will be morphed by this function.</param>
    /// <returns>Resulting morphed point.</returns>
    /// <since>5.0</since>
    public abstract Point3d MorphPoint(Point3d point);

    /// <summary>
    /// The desired accuracy of the morph. This value is primarily used for deforming
    /// surfaces and breps. The default is 0.0 and any value &lt;= 0.0 is ignored by
    /// morphing functions. The Tolerance value does not affect the way meshes and points
    /// are morphed.
    /// </summary>
    /// <since>5.0</since>
    public double Tolerance
    {
      get { return m_tolerance; }
      set { m_tolerance = value; }
    }

    /// <summary>
    /// true if the morph should be done as quickly as possible because the result
    /// is being used for some type of dynamic preview. If QuickPreview is true,
    /// the tolerance may be ignored.
    /// The QuickPreview value does not affect the way meshes and points are morphed.
    /// The default is false.
    /// </summary>
    /// <since>5.0</since>
    public bool QuickPreview
    {
      get { return m_bQuickPreview; }
      set { m_bQuickPreview = value; }
    }

    /// <summary>
    /// true if the morph should be done in a way that preserves the structure of the geometry.
    /// In particular, for NURBS objects, true means that only the control points are moved.
    /// The PreserveStructure value does not affect the way meshes and points are morphed.
    /// The default is false.
    /// </summary>
    /// <since>5.0</since>
    public bool PreserveStructure
    {
      get { return m_bPreserveStructure; }
      set { m_bPreserveStructure = value; }
    }


#if RHINO_SDK
    internal delegate void MorphPointCallback(Point3d point, ref Point3d out_point);
    void OnMorphPoint(Point3d point, ref Point3d out_point)
    {
      out_point = MorphPoint(point);
    }

    internal bool PerformGeometryMorph(Geometry.GeometryBase geometry)
    {
      // dont' copy a const geometry if we don't have to
      if (null == geometry || !IsMorphable(geometry))
        return false;

      IntPtr pGeometry = geometry.NonConstPointer();
      NativeSpaceMorphWrapper native_wrapper = this as NativeSpaceMorphWrapper;
      if (native_wrapper != null)
      {
        return UnsafeNativeMethods.ON_SpaceMorph_MorphGeometry2(pGeometry, native_wrapper.m_pSpaceMorph);
      }

      MorphPointCallback cb = new MorphPointCallback(OnMorphPoint);
      GCHandle pinnedCallback = GCHandle.Alloc(cb);
      IntPtr intptr_callback = Marshal.GetFunctionPointerForDelegate(cb);
      bool rc = UnsafeNativeMethods.ON_SpaceMorph_MorphGeometry(pGeometry, m_tolerance, m_bQuickPreview, m_bPreserveStructure, intptr_callback);
      pinnedCallback.Free();
      return rc;
    }

    internal bool PerformPlaneMorph(ref Plane plane)
    {
      NativeSpaceMorphWrapper native_wrapper = this as NativeSpaceMorphWrapper;
      if (native_wrapper != null)
      {
        return UnsafeNativeMethods.ON_SpaceMorph_MorphPlane2(ref plane, native_wrapper.m_pSpaceMorph);
      }

      MorphPointCallback cb = new MorphPointCallback(OnMorphPoint);
      GCHandle pinnedCallback = GCHandle.Alloc(cb);
      IntPtr intptr_callback = Marshal.GetFunctionPointerForDelegate(cb);
      bool rc = UnsafeNativeMethods.ON_SpaceMorph_MorphPlane(ref plane, m_tolerance, m_bQuickPreview, m_bPreserveStructure, intptr_callback);
      pinnedCallback.Free();
      return rc;
    }

#endif
  }
}

#if RHINO_SDK
namespace Rhino.Geometry.Morphs
{
  /// <summary>Deforms objects by rotating them around an axis.</summary>
  public class TwistSpaceMorph : Rhino.Geometry.SpaceMorph, IDisposable
  {
    internal IntPtr m_space_morph;
    IntPtr ConstPointer() { return m_space_morph; }
    IntPtr NonConstPointer() { return m_space_morph; }

    /// <summary>
    /// Constructs a twist space morph.
    /// </summary>
    /// <since>5.1</since>
    public TwistSpaceMorph()
    {
      m_space_morph = UnsafeNativeMethods.CRhinoTwistSpaceMorph_New();
      double tolerance = 0;
      bool quick_preview = false;
      bool preserve_structure = true;
      if (UnsafeNativeMethods.ON_SpaceMorph_GetValues(m_space_morph, ref tolerance, ref quick_preview, ref preserve_structure))
      {
        Tolerance = tolerance;
        QuickPreview = quick_preview;
        PreserveStructure = preserve_structure;
      }
    }

    /// <summary>Axis to rotate about.</summary>
    /// <since>5.1</since>
    public Line TwistAxis
    {
      get
      {
        IntPtr pConstThis = ConstPointer();
        Line rc = new Line();
        UnsafeNativeMethods.CRhinoTwistSpaceMorph_GetLine(pConstThis, ref rc);
        return rc;
      }
      set
      {
        IntPtr pThis = NonConstPointer();
        UnsafeNativeMethods.CRhinoTwistSpaceMorph_SetLine(pThis, ref value);
      }
    }

    /// <summary>
    /// Twist angle in radians.
    /// </summary>
    /// <since>5.1</since>
    public double TwistAngleRadians
    {
      get
      {
        IntPtr pConstThis = ConstPointer();
        return UnsafeNativeMethods.CRhinoTwistSpaceMorph_GetTwistAngle(pConstThis);
      }
      set
      {
        IntPtr pThis = NonConstPointer();
        UnsafeNativeMethods.CRhinoTwistSpaceMorph_SetTwistAngle(pThis, value);
      }
    }

    /// <summary>
    /// If true, the deformation is constant throughout the object, even if the axis is shorter than the object. 
    /// If false, the deformation takes place only the length of the axis.
    /// </summary>
    /// <since>5.1</since>
    public bool InfiniteTwist
    {
      get
      {
        IntPtr pConstThis = ConstPointer();
        return UnsafeNativeMethods.CRhinoTwistSpaceMorph_GetInfiniteTwist(pConstThis);
      }
      set
      {
        IntPtr pThis = NonConstPointer();
        UnsafeNativeMethods.CRhinoTwistSpaceMorph_SetInfiniteTwist(pThis, value);
      }
    }

    /// <summary>Morphs an Euclidean point. <para>This method is abstract.</para></summary>
    /// <param name="point">A point that will be morphed by this function.</param>
    /// <returns>Resulting morphed point.</returns>
    /// <since>5.1</since>
    public override Point3d MorphPoint(Point3d point)
    {
      UnsafeNativeMethods.ON_SpaceMorph_MorphPoint(m_space_morph, ref point);
      return point;
    }

    /// <summary>
    /// Passively reclaims unmanaged resources when the class user did not explicitly call Dispose().
    /// </summary>
    ~TwistSpaceMorph()
    {
      Dispose(false);
    }

    /// <summary>
    /// Actively reclaims unmanaged resources that this instance uses.
    /// </summary>
    /// <since>5.1</since>
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
      if (IntPtr.Zero != m_space_morph)
      {
        UnsafeNativeMethods.ON_SpaceMorph_Delete(m_space_morph);
        m_space_morph = IntPtr.Zero;
      }
    }
  }


  /// <summary>
  /// Deforms objects by bending along a spine arc.
  /// </summary>
  public class BendSpaceMorph : Rhino.Geometry.SpaceMorph, IDisposable
  {
    internal IntPtr m_space_morph;
    IntPtr ConstPointer() { return m_space_morph; }
    IntPtr NonConstPointer() { return m_space_morph; }

    /// <summary>
    /// Constructs a bend space morph.
    /// </summary>
    /// <param name="start">Start of spine that represents the original orientation of the object.</param>
    /// <param name="end">End of spine.</param>
    /// <param name="point">Point to bend through.</param>
    /// <param name="straight">If false, then point determines the region to bend. If true, only the spine region is bent.</param>
    /// <param name="symmetric">If false, then only one end of the object bends. If true, then the object will bend symmetrically around the center if you start the spine in the middle of the object.</param>
    /// <since>5.9</since>
    public BendSpaceMorph(Point3d start, Point3d end, Point3d point, bool straight, bool symmetric)
    {
      double tolerance = 0;
      bool quick_preview = false;
      bool preserve_structure = false;
      m_space_morph = UnsafeNativeMethods.RHC_BendSpaceMorph(start, end, point, Rhino.RhinoMath.UnsetValue, straight, symmetric);
      if (m_space_morph != IntPtr.Zero)
      {
        if (UnsafeNativeMethods.ON_SpaceMorph_GetValues(m_space_morph, ref tolerance, ref quick_preview, ref preserve_structure))
        {
          Tolerance = tolerance;
          QuickPreview = quick_preview;
          PreserveStructure = preserve_structure;
        }
      }
    }

    /// <summary>
    /// Constructs a bend space morph.
    /// </summary>
    /// <param name="start">Start of spine that represents the original orientation of the object.</param>
    /// <param name="end">End of spine.</param>
    /// <param name="point">Used for bend direction.</param>
    /// <param name="angle">Bend angle in radians.</param>
    /// <param name="straight">If false, then point determines the region to bend. If true, only the spine region is bent.</param>
    /// <param name="symmetric">If false, then only one end of the object bends. If true, then the object will bend symmetrically around the center if you start the spine in the middle of the object.</param>
    /// <since>5.9</since>
    public BendSpaceMorph(Point3d start, Point3d end, Point3d point, double angle, bool straight, bool symmetric)
    {
      double tolerance = 0;
      bool quick_preview = false;
      bool preserve_structure = false;
      m_space_morph = UnsafeNativeMethods.RHC_BendSpaceMorph(start, end, point, angle, straight, symmetric);
      if (m_space_morph != IntPtr.Zero)
      {
        if (UnsafeNativeMethods.ON_SpaceMorph_GetValues(m_space_morph, ref tolerance, ref quick_preview, ref preserve_structure))
        {
          Tolerance = tolerance;
          QuickPreview = quick_preview;
          PreserveStructure = preserve_structure;
        }
      }
    }

    /// <summary>
    /// Returns true if the space morph definition is valid, false otherwise.
    /// </summary>
    /// <since>5.9</since>
    public bool IsValid
    {
      get { return (m_space_morph != IntPtr.Zero); }
    }

    /// <summary>Morphs an Euclidean point.</summary>
    /// <param name="point">A point that will be morphed by this object.</param>
    /// <returns>Resulting morphed point.</returns>
    /// <since>5.9</since>
    public override Point3d MorphPoint(Point3d point)
    {
      UnsafeNativeMethods.ON_SpaceMorph_MorphPoint(m_space_morph, ref point);
      return point;
    }

    /// <summary>
    /// Passively reclaims unmanaged resources when the class user did not explicitly call Dispose().
    /// </summary>
    ~BendSpaceMorph()
    {
      Dispose(false);
    }

    /// <summary>
    /// Actively reclaims unmanaged resources that this instance uses.
    /// </summary>
    /// <since>5.9</since>
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
      if (IntPtr.Zero != m_space_morph)
      {
        UnsafeNativeMethods.ON_SpaceMorph_Delete(m_space_morph);
        m_space_morph = IntPtr.Zero;
      }
    }
  }


  /// <summary>
  /// Deforms objects toward or away from a specified axis.
  /// </summary>
  public class TaperSpaceMorph : Rhino.Geometry.SpaceMorph, IDisposable
  {
    internal IntPtr m_space_morph;
    IntPtr ConstPointer() { return m_space_morph; }
    IntPtr NonConstPointer() { return m_space_morph; }

    /// <summary>
    /// Constructs a taper space morph.
    /// </summary>
    /// <param name="start">Start of the taper axis.</param>
    /// <param name="end">End of the taper axis.</param>
    /// <param name="startRadius">Radius at start point.</param>
    /// <param name="endRadius">Radius at end point.</param>
    /// <param name="bFlat">If true, then a one-directional, one-dimensional taper is created.</param>
    /// <param name="infiniteTaper">If false, the deformation takes place only the length of the axis. If true, the deformation happens throughout the object, even if the axis is shorter.</param>
    /// <since>5.9</since>
    public TaperSpaceMorph(Point3d start, Point3d end, double startRadius, double endRadius, bool bFlat, bool infiniteTaper)
    {
      double tolerance = 0;
      bool quick_preview = false;
      bool preserve_structure = false;
      m_space_morph = UnsafeNativeMethods.RHC_TaperSpaceMorph(start, end, startRadius, endRadius, bFlat, infiniteTaper);
      if (m_space_morph != IntPtr.Zero)
      {
        if (UnsafeNativeMethods.ON_SpaceMorph_GetValues(m_space_morph, ref tolerance, ref quick_preview, ref preserve_structure))
        {
          Tolerance = tolerance;
          QuickPreview = quick_preview;
          PreserveStructure = preserve_structure;
        }
      }
    }

    /// <summary>
    /// Returns true if the space morph definition is valid, false otherwise.
    /// </summary>
    /// <since>5.9</since>
    public bool IsValid
    {
      get { return (m_space_morph != IntPtr.Zero); }
    }

    /// <summary>Morphs an Euclidean point.</summary>
    /// <param name="point">A point that will be morphed by this object.</param>
    /// <returns>Resulting morphed point.</returns>
    /// <since>5.9</since>
    public override Point3d MorphPoint(Point3d point)
    {
      UnsafeNativeMethods.ON_SpaceMorph_MorphPoint(m_space_morph, ref point);
      return point;
    }

    /// <summary>
    /// Passively reclaims unmanaged resources when the class user did not explicitly call Dispose().
    /// </summary>
    ~TaperSpaceMorph()
    {
      Dispose(false);
    }

    /// <summary>
    /// Actively reclaims unmanaged resources that this instance uses.
    /// </summary>
    /// <since>5.9</since>
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
      if (IntPtr.Zero != m_space_morph)
      {
        UnsafeNativeMethods.ON_SpaceMorph_Delete(m_space_morph);
        m_space_morph = IntPtr.Zero;
      }
    }
  }


  /// <summary>
  /// Deforms objects in a spiral as if they were caught in a whirlpool.
  /// </summary>
  public class MaelstromSpaceMorph : Rhino.Geometry.SpaceMorph, IDisposable
  {
    internal IntPtr m_space_morph;
    IntPtr ConstPointer() { return m_space_morph; }
    IntPtr NonConstPointer() { return m_space_morph; }

    /// <summary>
    /// Constructs a maelstrom space morph.
    /// </summary>
    /// <param name="plane">Plane on which the base circle will lie. Origin of the plane will be the center point of the circle.</param>
    /// <param name="radius0">First radius.</param>
    /// <param name="radius1">Second radius.</param>
    /// <param name="angle">Coil angle in radians.</param>
    /// <remarks>
    /// <para>
    /// If radius0 = radius1 &gt; 0, then the morph is a rotation where the angle of rotation is proportional to the radius.
    /// 
    /// If radius0 &lt; radius1, then everything inside of the circle of radius radius0 if fixed, the rotation angle increases
    /// smoothly from 0 at radius0 to m_a at radius1, and everything outside of the circle of radius radius1 is rotated by angle.
    /// 
    /// If radius0 &gt; radius1, then everything outside of the circle of radius radius0 if fixed, the rotation angle increases
    /// smoothly from 0 at radius0 to m_a at radius1, and everything inside of the circle of radius radius1 is rotated by angle.
    /// </para>
    /// </remarks>
    /// <since>5.9</since>
    public MaelstromSpaceMorph(Plane plane, double radius0, double radius1, double angle)
    {
      double tolerance = 0;
      bool quick_preview = false;
      bool preserve_structure = false;
      m_space_morph = UnsafeNativeMethods.RHC_MaelstromSpaceMorph(plane, radius0, radius1, angle);
      if (m_space_morph != IntPtr.Zero)
      {
        if (UnsafeNativeMethods.ON_SpaceMorph_GetValues(m_space_morph, ref tolerance, ref quick_preview, ref preserve_structure))
        {
          Tolerance = tolerance;
          QuickPreview = quick_preview;
          PreserveStructure = preserve_structure;
        }
      }
    }

    /// <summary>
    /// Returns true if the space morph definition is valid, false otherwise.
    /// </summary>
    /// <since>5.9</since>
    public bool IsValid
    {
      get
      {
        return (m_space_morph != IntPtr.Zero);
      }
    }

    /// <summary>Morphs an Euclidean point.</summary>
    /// <param name="point">A point that will be morphed by this object.</param>
    /// <returns>Resulting morphed point.</returns>
    /// <since>5.9</since>
    public override Point3d MorphPoint(Point3d point)
    {
      UnsafeNativeMethods.ON_SpaceMorph_MorphPoint(m_space_morph, ref point);
      return point;
    }

    /// <summary>
    /// Passively reclaims unmanaged resources when the class user did not explicitly call Dispose().
    /// </summary>
    ~MaelstromSpaceMorph()
    {
      Dispose(false);
    }

    /// <summary>
    /// Actively reclaims unmanaged resources that this instance uses.
    /// </summary>
    /// <since>5.9</since>
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
      if (IntPtr.Zero != m_space_morph)
      {
        UnsafeNativeMethods.ON_SpaceMorph_Delete(m_space_morph);
        m_space_morph = IntPtr.Zero;
      }
    }
  }


  /// <summary>
  /// Deforms objects toward or away from a specified axis.
  /// </summary>
  public class StretchSpaceMorph : Rhino.Geometry.SpaceMorph, IDisposable
  {
    internal IntPtr m_space_morph;
    IntPtr ConstPointer() { return m_space_morph; }
    IntPtr NonConstPointer() { return m_space_morph; }

    /// <summary>
    /// Constructs a stretch space morph.
    /// </summary>
    /// <param name="start">Start of stretch axis.</param>
    /// <param name="end">End of stretch axis.></param>
    /// <param name="point">End of new stretch axis.</param>
    /// <since>5.9</since>
    public StretchSpaceMorph(Point3d start, Point3d end, Point3d point)
    {
      double tolerance = 0;
      bool quick_preview = false;
      bool preserve_structure = false;
      m_space_morph = UnsafeNativeMethods.RHC_StretchSpaceMorph(start, end, point, Rhino.RhinoMath.UnsetValue);
      if (m_space_morph != IntPtr.Zero)
      {
        if (UnsafeNativeMethods.ON_SpaceMorph_GetValues(m_space_morph, ref tolerance, ref quick_preview, ref preserve_structure))
        {
          Tolerance = tolerance;
          QuickPreview = quick_preview;
          PreserveStructure = preserve_structure;
        }
      }
    }

    /// <summary>
    /// Constructs a stretch space morph.
    /// </summary>
    /// <param name="start">Start of stretch axis.</param>
    /// <param name="end">End of stretch axis.></param>
    /// <param name="length">Length of new stretch axis.</param>
    /// <since>5.9</since>
    public StretchSpaceMorph(Point3d start, Point3d end, double length)
    {
      double tolerance = 0;
      bool quick_preview = false;
      bool preserve_structure = false;
      m_space_morph = UnsafeNativeMethods.RHC_StretchSpaceMorph(start, end, Rhino.Geometry.Point3d.Unset, length);
      if (m_space_morph != IntPtr.Zero)
      {
        if (UnsafeNativeMethods.ON_SpaceMorph_GetValues(m_space_morph, ref tolerance, ref quick_preview, ref preserve_structure))
        {
          Tolerance = tolerance;
          QuickPreview = quick_preview;
          PreserveStructure = preserve_structure;
        }
      }
    }

    /// <summary>
    /// Returns true if the space morph definition is valid, false otherwise.
    /// </summary>
    /// <since>5.9</since>
    public bool IsValid
    {
      get
      {
        return (m_space_morph != IntPtr.Zero);
      }
    }

    /// <summary>Morphs an Euclidean point.</summary>
    /// <param name="point">A point that will be morphed by this object.</param>
    /// <returns>Resulting morphed point.</returns>
    /// <since>5.9</since>
    public override Point3d MorphPoint(Point3d point)
    {
      UnsafeNativeMethods.ON_SpaceMorph_MorphPoint(m_space_morph, ref point);
      return point;
    }

    /// <summary>
    /// Passively reclaims unmanaged resources when the class user did not explicitly call Dispose().
    /// </summary>
    ~StretchSpaceMorph()
    {
      Dispose(false);
    }

    /// <summary>
    /// Actively reclaims unmanaged resources that this instance uses.
    /// </summary>
    /// <since>5.9</since>
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
      if (IntPtr.Zero != m_space_morph)
      {
        UnsafeNativeMethods.ON_SpaceMorph_Delete(m_space_morph);
        m_space_morph = IntPtr.Zero;
      }
    }
  }


  /// <summary>
  /// Deforms an object from a source surface to a target surface.
  /// </summary>
  public class SporphSpaceMorph : Rhino.Geometry.SpaceMorph, IDisposable
  {
    internal IntPtr m_space_morph;
    IntPtr ConstPointer() { return m_space_morph; }
    IntPtr NonConstPointer() { return m_space_morph; }

    /// <summary>
    /// Constructs a Sporph space morph.
    /// </summary>
    /// <param name="surface0">Base surface.</param>
    /// <param name="surface1">Target surface.</param>
    /// <since>5.9</since>
    public SporphSpaceMorph(Surface surface0, Surface surface1)
    {
      double tolerance = 0;
      bool quick_preview = false;
      bool preserve_structure = false;

      IntPtr const_surface0 = surface0.ConstPointer();
      IntPtr const_surface1 = surface1.ConstPointer();

      m_space_morph = UnsafeNativeMethods.RHC_SporphSpaceMorph(const_surface0, const_surface1, Rhino.Geometry.Point2d.Unset, Rhino.Geometry.Point2d.Unset);
      if (m_space_morph != IntPtr.Zero)
      {
        if (UnsafeNativeMethods.ON_SpaceMorph_GetValues(m_space_morph, ref tolerance, ref quick_preview, ref preserve_structure))
        {
          Tolerance = tolerance;
          QuickPreview = quick_preview;
          PreserveStructure = preserve_structure;
        }
      }
      Runtime.CommonObject.GcProtect(surface0, surface1);
    }

    /// <summary>
    /// Constructs a Sporph space morph.
    /// </summary>
    /// <param name="surface0">Base surface.</param>
    /// <param name="surface1">Target surface.</param>
    /// <param name="surface0Param">U,V parameter on surface0 used for orienting.</param>
    /// <param name="surface1Param">U,V parameter on surface1 used for orienting.</param>
    /// <since>5.9</since>
    public SporphSpaceMorph(Surface surface0, Surface surface1, Point2d surface0Param, Point2d surface1Param)
    {
      double tolerance = 0;
      bool quick_preview = false;
      bool preserve_structure = false;

      IntPtr const_surface0 = surface0.ConstPointer();
      IntPtr const_surface1 = surface1.ConstPointer();

      m_space_morph = UnsafeNativeMethods.RHC_SporphSpaceMorph(const_surface0, const_surface1, surface0Param, surface1Param);
      if (m_space_morph != IntPtr.Zero)
      {
        if (UnsafeNativeMethods.ON_SpaceMorph_GetValues(m_space_morph, ref tolerance, ref quick_preview, ref preserve_structure))
        {
          Tolerance = tolerance;
          QuickPreview = quick_preview;
          PreserveStructure = preserve_structure;
        }
      }
      Runtime.CommonObject.GcProtect(surface0, surface1);
    }

    /// <summary>
    /// Returns true if the space morph definition is valid, false otherwise.
    /// </summary>
    /// <since>5.9</since>
    public bool IsValid
    {
      get
      {
        return (m_space_morph != IntPtr.Zero);
      }
    }

    /// <summary>Morphs an Euclidean point.</summary>
    /// <param name="point">A point that will be morphed by this object.</param>
    /// <returns>Resulting morphed point.</returns>
    /// <since>5.9</since>
    public override Point3d MorphPoint(Point3d point)
    {
      UnsafeNativeMethods.ON_SpaceMorph_MorphPoint(m_space_morph, ref point);
      return point;
    }

    /// <summary>
    /// Passively reclaims unmanaged resources when the class user did not explicitly call Dispose().
    /// </summary>
    ~SporphSpaceMorph()
    {
      Dispose(false);
    }

    /// <summary>
    /// Actively reclaims unmanaged resources that this instance uses.
    /// </summary>
    /// <since>5.9</since>
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
      if (IntPtr.Zero != m_space_morph)
      {
        UnsafeNativeMethods.ON_SpaceMorph_Delete(m_space_morph);
        m_space_morph = IntPtr.Zero;
      }
    }
  }


  /// <summary>
  /// Re-aligns objects from a base curve to a target curve.
  /// </summary>
  public class FlowSpaceMorph : Rhino.Geometry.SpaceMorph, IDisposable
  {
    internal IntPtr m_space_morph;
    IntPtr ConstPointer() { return m_space_morph; }
    IntPtr NonConstPointer() { return m_space_morph; }

    /// <summary>
    /// Constructs a flow space morph.
    /// </summary>
    /// <param name="curve0">Base curve.</param>
    /// <param name="curve1">Target curve.</param>
    /// <param name="preventStretching"></param>
    /// <since>5.9</since>
    public FlowSpaceMorph(Curve curve0, Curve curve1, bool preventStretching)
    {
      double tolerance = 0;
      bool quick_preview = false;
      bool preserve_structure = false;

      IntPtr const_curve0 = curve0.ConstPointer();
      IntPtr const_curve1 = curve1.ConstPointer();

      m_space_morph = UnsafeNativeMethods.RHC_FlowSpaceMorph(const_curve0, const_curve1, false, false, preventStretching);
      if (m_space_morph != IntPtr.Zero)
      {
        if (UnsafeNativeMethods.ON_SpaceMorph_GetValues(m_space_morph, ref tolerance, ref quick_preview, ref preserve_structure))
        {
          Tolerance = tolerance;
          QuickPreview = quick_preview;
          PreserveStructure = preserve_structure;
        }
      }
      Runtime.CommonObject.GcProtect(curve0, curve1);
    }

    /// <summary>
    /// Constructs a flow space morph.
    /// </summary>
    /// <param name="curve0">Base curve.</param>
    /// <param name="curve1">Target curve.</param>
    /// <param name="reverseCurve0">If true, then direction of curve0 is reversed.</param>
    /// <param name="reverseCurve1">If true, then direction of curve1 is reversed.</param>
    /// <param name="preventStretching">If true, the length of the objects along the curve directions are not changed. If false, objects are stretched or compressed in the curve direction so that the relationship to the target curve is the same as it is to the base curve.</param>
    /// <since>5.9</since>
    public FlowSpaceMorph(Curve curve0, Curve curve1, bool reverseCurve0, bool reverseCurve1, bool preventStretching)
    {
      double tolerance = 0;
      bool quick_preview = false;
      bool preserve_structure = false;

      IntPtr const_curve0 = curve0.ConstPointer();
      IntPtr const_curve1 = curve1.ConstPointer();

      m_space_morph = UnsafeNativeMethods.RHC_FlowSpaceMorph(const_curve0, const_curve1, reverseCurve0, reverseCurve1, preventStretching);
      if (m_space_morph != IntPtr.Zero)
      {
        if (UnsafeNativeMethods.ON_SpaceMorph_GetValues(m_space_morph, ref tolerance, ref quick_preview, ref preserve_structure))
        {
          Tolerance = tolerance;
          QuickPreview = quick_preview;
          PreserveStructure = preserve_structure;
        }
      }
      Runtime.CommonObject.GcProtect(curve0, curve1);
    }

    /// <summary>
    /// Returns true if the space morph definition is valid, false otherwise.
    /// </summary>
    /// <since>5.9</since>
    public bool IsValid
    {
      get
      {
        return (m_space_morph != IntPtr.Zero);
      }
    }

    /// <summary>Morphs an Euclidean point.</summary>
    /// <param name="point">A point that will be morphed by this object.</param>
    /// <returns>Resulting morphed point.</returns>
    /// <since>5.9</since>
    public override Point3d MorphPoint(Point3d point)
    {
      UnsafeNativeMethods.ON_SpaceMorph_MorphPoint(m_space_morph, ref point);
      return point;
    }

    /// <summary>
    /// Passively reclaims unmanaged resources when the class user did not explicitly call Dispose().
    /// </summary>
    ~FlowSpaceMorph()
    {
      Dispose(false);
    }

    /// <summary>
    /// Actively reclaims unmanaged resources that this instance uses.
    /// </summary>
    /// <since>5.9</since>
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
      if (IntPtr.Zero != m_space_morph)
      {
        UnsafeNativeMethods.ON_SpaceMorph_Delete(m_space_morph);
        m_space_morph = IntPtr.Zero;
      }
    }
  }


  /// <summary>
  /// Rotates, scales, and wraps objects on a surface.
  /// </summary>
  public class SplopSpaceMorph : Rhino.Geometry.SpaceMorph, IDisposable
  {
    internal IntPtr m_space_morph;
    IntPtr ConstPointer() { return m_space_morph; }
    IntPtr NonConstPointer() { return m_space_morph; }

    /// <summary>
    /// Constructs a flow space morph.
    /// </summary>
    /// <param name="plane">Source plane of deformation.</param>
    /// <param name="surface">Surface to wrap objects onto.</param>
    /// <param name="surfaceParam">U,V parameter on surface used for orienting.</param>
    /// <since>5.9</since>
    public SplopSpaceMorph(Plane plane, Surface surface, Point2d surfaceParam)
    {
      double tolerance = 0;
      bool quick_preview = false;
      bool preserve_structure = false;

      IntPtr const_surface = surface.ConstPointer();

      m_space_morph = UnsafeNativeMethods.RHC_SplopSpaceMorph(plane, const_surface, surfaceParam, Rhino.RhinoMath.UnsetValue, Rhino.RhinoMath.UnsetValue);
      if (m_space_morph != IntPtr.Zero)
      {
        if (UnsafeNativeMethods.ON_SpaceMorph_GetValues(m_space_morph, ref tolerance, ref quick_preview, ref preserve_structure))
        {
          Tolerance = tolerance;
          QuickPreview = quick_preview;
          PreserveStructure = preserve_structure;
        }
      }
      GC.KeepAlive(surface);
    }

    /// <summary>
    /// Constructs a flow space morph.
    /// </summary>
    /// <param name="plane">Source plane of deformation.</param>
    /// <param name="surface">Surface to wrap objects onto.</param>
    /// <param name="surfaceParam">U,V parameter on surface used for orienting.</param>
    /// <param name="scale">Scale factor.</param>
    /// <since>5.9</since>
    public SplopSpaceMorph(Plane plane, Surface surface, Point2d surfaceParam, double scale)
    {
      double tolerance = 0;
      bool quick_preview = false;
      bool preserve_structure = false;

      IntPtr const_surface = surface.ConstPointer();

      m_space_morph = UnsafeNativeMethods.RHC_SplopSpaceMorph(plane, const_surface, surfaceParam, scale, Rhino.RhinoMath.UnsetValue);
      if (m_space_morph != IntPtr.Zero)
      {
        if (UnsafeNativeMethods.ON_SpaceMorph_GetValues(m_space_morph, ref tolerance, ref quick_preview, ref preserve_structure))
        {
          Tolerance = tolerance;
          QuickPreview = quick_preview;
          PreserveStructure = preserve_structure;
        }
      }
      GC.KeepAlive(surface);
    }

    /// <summary>
    /// Constructs a flow space morph.
    /// </summary>
    /// <param name="plane">Source plane of deformation.</param>
    /// <param name="surface">Surface to wrap objects onto.</param>
    /// <param name="surfaceParam">U,V parameter on surface used for orienting.</param>
    /// <param name="scale">Scale factor. To ignore, use Rhino.RhinoMath.UnsetValue.</param>
    /// <param name="angle">Rotation angle in radians. To ignore, use Rhino.RhinoMath.UnsetValue.</param>
    /// <since>5.9</since>
    public SplopSpaceMorph(Plane plane, Surface surface, Point2d surfaceParam, double scale, double angle)
    {
      double tolerance = 0;
      bool quick_preview = false;
      bool preserve_structure = false;

      IntPtr const_surface = surface.ConstPointer();

      m_space_morph = UnsafeNativeMethods.RHC_SplopSpaceMorph(plane, const_surface, surfaceParam, scale, angle);
      if (m_space_morph != IntPtr.Zero)
      {
        if (UnsafeNativeMethods.ON_SpaceMorph_GetValues(m_space_morph, ref tolerance, ref quick_preview, ref preserve_structure))
        {
          Tolerance = tolerance;
          QuickPreview = quick_preview;
          PreserveStructure = preserve_structure;
        }
      }
      GC.KeepAlive(surface);
    }

    /// <summary>
    /// Returns true if the space morph definition is valid, false otherwise.
    /// </summary>
    /// <since>5.9</since>
    public bool IsValid
    {
      get
      {
        return (m_space_morph != IntPtr.Zero);
      }
    }

    /// <summary>Morphs an Euclidean point.</summary>
    /// <param name="point">A point that will be morphed by this object.</param>
    /// <returns>Resulting morphed point.</returns>
    /// <since>5.9</since>
    public override Point3d MorphPoint(Point3d point)
    {
      UnsafeNativeMethods.ON_SpaceMorph_MorphPoint(m_space_morph, ref point);
      return point;
    }

    /// <summary>
    /// Passively reclaims unmanaged resources when the class user did not explicitly call Dispose().
    /// </summary>
    ~SplopSpaceMorph()
    {
      Dispose(false);
    }

    /// <summary>
    /// Actively reclaims unmanaged resources that this instance uses.
    /// </summary>
    /// <since>5.9</since>
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
      if (IntPtr.Zero != m_space_morph)
      {
        UnsafeNativeMethods.ON_SpaceMorph_Delete(m_space_morph);
        m_space_morph = IntPtr.Zero;
      }
    }
  }

}
#endif
