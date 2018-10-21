#include "bindings.h"

#pragma once

#if defined(ON_PYTHON_COMPILE)
void initExtrusionBindings(pybind11::module& m);
#else
void initExtrusionBindings(void* m);
#endif

class BND_Extrusion : public BND_Surface
{
  ON_Extrusion* m_extrusion = nullptr;
protected:
  void SetTrackedPointer(ON_Extrusion* extrusion, const ON_ModelComponentReference* compref);

public:
  //public static Extrusion Create(Curve planarCurve, double height, bool cap)
  //public static Extrusion CreateBoxExtrusion(Box box, bool cap = true)
  //public static Extrusion CreateCylinderExtrusion(Cylinder cylinder, bool capBottom, bool capTop)
  //public static Extrusion CreatePipeExtrusion(Cylinder cylinder, double otherRadius, bool capTop, bool capBottom)
  BND_Extrusion();
  //public Brep ToBrep(bool splitKinkyFaces)
  //public bool SetPathAndUp(Point3d a, Point3d b, Vector3d up)
  //public Point3d PathStart | get;
  //public Point3d PathEnd | get;
  //public Vector3d PathTangent | get;
  //public Vector3d MiterPlaneNormalAtStart | get; set;
  //public Vector3d MiterPlaneNormalAtEnd | get; set;
  //public bool IsMiteredAtStart | get;
  //public bool IsMiteredAtEnd | get;
  //public override bool IsSolid | get;
  //public bool IsCappedAtBottom | get;
  //public bool IsCappedAtTop | get;
  //public int CapCount | get;
  //public Transform GetProfileTransformation(double s)
  //public Plane GetProfilePlane(double s)
  //public Plane GetPathPlane(double s)
  //public bool SetOuterProfile(Curve outerProfile, bool cap)
  //public bool AddInnerProfile(Curve innerProfile)
  //public int ProfileCount | get;
  //public Curve Profile3d(int profileIndex, double s)
  //public Curve Profile3d(ComponentIndex ci)
  //public Curve WallEdge(ComponentIndex ci)
  //public Surface WallSurface(ComponentIndex ci)
  //public LineCurve PathLineCurve()
  //public int ProfileIndex(double profileParameter)
  //public Mesh GetMesh(MeshType meshType)

  BND_Extrusion(ON_Extrusion* extrusion, const ON_ModelComponentReference* compref);

};
