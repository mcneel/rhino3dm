#include "bindings.h"

#pragma once

#if defined(ON_PYTHON_COMPILE)
#if defined(NANOBIND)
namespace py = nanobind;
void initTextureMappingBindings(py::module_& m);
#else
namespace py = pybind11;
void initTextureMappingBindings(py::module& m);
#endif

#else
void initTextureMappingBindings(void* m);
#endif

class BND_TextureMapping : public BND_CommonObject
{
public:
  ON_TextureMapping* m_mapping = nullptr;
public:
  BND_TextureMapping(ON_TextureMapping* mapping, const ON_ModelComponentReference* compref);
  BND_TextureMapping();

  bool RequiresVertexNormals() const { return m_mapping->RequiresVertexNormals(); }
  bool IsPeriodic() const { return m_mapping->IsPeriodic(); }
  static BND_TextureMapping* CreateSurfaceParameterMapping();
  static BND_TextureMapping* CreatePlaneMapping(const class BND_Plane& plane, const class BND_Interval& dx,
    const class BND_Interval& dy, const class BND_Interval& dz);
  static BND_TextureMapping* CreateCylinderMapping(const class BND_Cylinder& cylinder, bool capped);
  static BND_TextureMapping* CreateSphereMapping(const class BND_Sphere& sphere);
  static BND_TextureMapping* CreateBoxMapping(const class BND_Plane& plane,
    const class BND_Interval& dx, const class BND_Interval& dy, const class BND_Interval& dz, bool capped);

  std::tuple<BND_Plane, BND_Interval, BND_Interval, BND_Interval>* TryGetMappingPlane() const;
  class BND_Cylinder* TryGetMappingCylinder() const;
  class BND_Sphere* TryGetMappingSphere() const;
  std::tuple<BND_Plane, BND_Interval, BND_Interval, BND_Interval>* TryGetMappingBox() const;

  bool ReverseTextureCoordinate(int dir) { return m_mapping->ReverseTextureCoordinate(dir); }
  bool SwapTextureCoordinate(int i, int j) { return m_mapping->SwapTextureCoordinate(i, j); }
  bool TileTextureCoordinate(int dir, double count, double offset) { return m_mapping->TileTextureCoordinate(dir, count, offset); }
  std::tuple<int, ON_3dPoint> Evaluate( const ON_3dPoint& P, const ON_3dVector& N ) const;
//  int Evaluate(const ON_3dPoint& P, const ON_3dVector& N, ON_3dPoint* T, const ON_Xform& P_xform, const ON_Xform& N_xform ) const;
//  int EvaluatePlaneMapping( const ON_3dPoint& P, const ON_3dVector& N, ON_3dPoint* T ) const;
//  int EvaluateSphereMapping( const ON_3dPoint& P, const ON_3dVector& N, ON_3dPoint* T ) const;
//  int EvaluateCylinderMapping( const ON_3dPoint& P, const ON_3dVector& N, ON_3dPoint* T ) const;
//  int EvaluateBoxMapping( const ON_3dPoint& P, const ON_3dVector& N, ON_3dPoint* T ) const;
//  bool HasMatchingTextureCoordinates( const ON_Mesh& mesh, const ON_Xform* object_xform = nullptr ) const;
//  bool HasMatchingTextureCoordinates( const class ON_MappingTag& tag, const ON_Xform* object_xform = nullptr ) const;
//  bool GetTextureCoordinates( const ON_Mesh& mesh, ON_SimpleArray<ON_3fPoint>& T, const ON_Xform* mesh_xform = 0, bool bLazy = false, ON_SimpleArray<int>* Tside = 0 ) const;
//  bool GetTextureCoordinates( const ON_Mesh& mesh, ON_SimpleArray<ON_2fPoint>& T, const ON_Xform* mesh_xform = 0, bool bLazy = false, ON_SimpleArray<int>* Tside = 0 ) const;

  ON_TextureMapping::TYPE GetType() const { return m_mapping->m_type; }
//  ON_TextureMapping::PROJECTION m_projection = ON_TextureMapping::PROJECTION::no_projection;
//  ON_TextureMapping::TEXTURE_SPACE m_texture_space = ON_TextureMapping::TEXTURE_SPACE::single;
//  bool m_bCapped = false;
//  ON_Xform m_Pxyz = ON_Xform::IdentityTransformation;
//  ON_Xform m_Nxyz = ON_Xform::IdentityTransformation;
//  ON_Xform m_uvw = ON_Xform::IdentityTransformation;
//  ON__UINT32 MappingCRC() const;
//  const ON_Object* CustomMappingPrimitive(void) const;
//  const ON_Mesh* CustomMappingMeshPrimitive(void) const;
//  const ON_Brep* CustomMappingBrepPrimitive(void) const;
//  const ON_Surface* CustomMappingSurfacePrimitive(void) const;
//  void SetCustomMappingPrimitive(ON_Object*);

  //public override Guid Id
  //public Transform UvwTransform | get; set;
  //public Transform PrimativeTransform | get; set;
  //public Transform NormalTransform | get; set;
  //public override ModelComponentType ComponentType

  bool HasId() const { return m_mapping->IdIsSet(); }
  BND_UUID Id() const { return ON_UUID_to_Binding(m_mapping->Id()); }


protected:
  void SetTrackedPointer(ON_TextureMapping* mapping, const ON_ModelComponentReference* compref);

};