#include "bindings.h"

#pragma once

#if defined(ON_PYTHON_COMPILE)
void initExtensionsBindings(pybind11::module& m);
#else
void initExtensionsBindings(void* m);
#endif

class BND_FileObject
{
public:
  BND_GeometryBase* m_geometry = nullptr;
  BND_3dmAttributes* m_attributes = nullptr;

  BND_GeometryBase* GetGeometry() { return m_geometry; };
  BND_3dmAttributes* GetAttributes() { return m_attributes; }
};

class BND_ONXModel_ObjectTable
{
  std::shared_ptr<ONX_Model> m_model;
public:
  BND_ONXModel_ObjectTable(std::shared_ptr<ONX_Model> m) { m_model = m; }
  BND_UUID AddPoint1(double x, double y, double z);
  BND_UUID AddPoint2(const ON_3dPoint& point) { return AddPoint1(point.x, point.y, point.z); }
  //Guid AddPoint3(Point3d point, DocObjects.ObjectAttributes attributes)
  BND_UUID AddPoint4(const ON_3fPoint& point) { return AddPoint1(point.x, point.y, point.z); }
  //Guid AddPoint5(Point3f point, DocObjects.ObjectAttributes attributes)
  //Guid[] AddPoints1(IEnumerable<Point3d> points)
  //Guid[] AddPoints2(IEnumerable<Point3d> points, DocObjects.ObjectAttributes attributes)
  //Guid[] AddPoints3(IEnumerable<Point3f> points)
  //Guid[] AddPoints4(IEnumerable<Point3f> points, DocObjects.ObjectAttributes attributes)
  //Guid AddPointCloud1(PointCloud cloud)
  //Guid AddPointCloud2(PointCloud cloud, DocObjects.ObjectAttributes attributes)
  //Guid AddPointCloud3(IEnumerable<Point3d> points)
  //Guid AddPointCloud4(IEnumerable<Point3d> points, DocObjects.ObjectAttributes attributes)
  //Guid AddClippingPlane1(Plane plane, double uMagnitude, double vMagnitude, Guid clippedViewportId)
  //Guid AddClippingPlane2(Plane plane, double uMagnitude, double vMagnitude, IEnumerable<Guid> clippedViewportIds)
  //Guid AddClippingPlane3(Plane plane, double uMagnitude, double vMagnitude, IEnumerable<Guid> clippedViewportIds, DocObjects.ObjectAttributes attributes)
  //Guid AddLinearDimension1(LinearDimension dimension)
  //Guid AddLinearDimension2(LinearDimension dimension, DocObjects.ObjectAttributes attributes)
  //Guid AddAngularDimension1(AngularDimension dimension)
  //Guid AddAngularDimension2(AngularDimension dimension, ObjectAttributes attributes)
  BND_UUID AddLine1(const ON_3dPoint& from, const ON_3dPoint& to);
  //Guid AddLine2(Point3d from, Point3d to, DocObjects.ObjectAttributes attributes)
  //Guid AddLine3(Line line)
  //Guid AddLine4(Line line, DocObjects.ObjectAttributes attributes)
  //Guid AddPolyline1(IEnumerable<Point3d> points)
  //Guid AddPolyline2(IEnumerable<Point3d> points, DocObjects.ObjectAttributes attributes)
  //Guid AddArc1(Arc arc)
  //Guid AddArc2(Arc arc, DocObjects.ObjectAttributes attributes)
  BND_UUID AddCircle1(const class BND_Circle& circle);
  //Guid AddCircle2(Circle circle, DocObjects.ObjectAttributes attributes)
  //Guid AddEllipse1(Ellipse ellipse)
  //Guid AddEllipse2(Ellipse ellipse, DocObjects.ObjectAttributes attributes)
  BND_UUID AddSphere1(const class BND_Sphere& sphere);
  //Guid AddSphere2(Sphere sphere, DocObjects.ObjectAttributes attributes)
  BND_UUID AddCurve1(const class BND_Curve* curve);
  //Guid AddCurve2(Geometry.Curve curve, DocObjects.ObjectAttributes attributes)
  BND_UUID AddTextDot1(std::wstring text, const ON_3dPoint& location);
  //Guid AddTextDot2(string text, Point3d location, DocObjects.ObjectAttributes attributes)
  //Guid AddTextDot3(Geometry.TextDot dot)
  //Guid AddTextDot4(Geometry.TextDot dot, DocObjects.ObjectAttributes attributes)
  //Guid AddInstanceObject1(InstanceReferenceGeometry instanceReference)
  //Guid AddInstanceObject2(InstanceReferenceGeometry instanceReference, ObjectAttributes attributes)
  //Guid AddInstanceObject3(int instanceDefinitionIndex, Transform instanceXform)
  //Guid AddInstanceObject4(int instanceDefinitionIndex, Transform instanceXform, ObjectAttributes attributes)
  //Guid AddText1(string text, Plane plane, double height, string fontName, bool bold, bool italic)
  //Guid AddText2(string text, Plane plane, double height, string fontName, bool bold, bool italic, TextJustification justification)
  //Guid AddText3(string text, Plane plane, double height, string fontName, bool bold, bool italic, TextJustification justification, DocObjects.ObjectAttributes attributes)
  //Guid AddText4(string text, Plane plane, double height, string fontName, bool bold, bool italic, DocObjects.ObjectAttributes attributes)
  //Guid AddSurface1(Geometry.Surface surface)
  //Guid AddSurface2(Geometry.Surface surface, DocObjects.ObjectAttributes attributes)
  //Guid AddExtrusion1(Geometry.Extrusion extrusion)
  //Guid AddExtrusion2(Geometry.Extrusion extrusion, DocObjects.ObjectAttributes attributes)
  BND_UUID AddMesh1(const class BND_Mesh* mesh);
  //Guid AddMesh2(Geometry.Mesh mesh, DocObjects.ObjectAttributes attributes)
  BND_UUID AddBrep1(const class BND_Brep* brep);
  //Guid AddBrep2(Geometry.Brep brep, DocObjects.ObjectAttributes attributes)
  //Guid AddLeader1(Plane plane, IEnumerable<Point2d> points)
  //Guid AddLeader2(Plane plane, IEnumerable<Point2d> points, DocObjects.ObjectAttributes attributes)
  //Guid AddLeader3(string text, Plane plane, IEnumerable<Point2d> points, DocObjects.ObjectAttributes attributes)
  //Guid AddLeader4(string text, Plane plane, IEnumerable<Point2d> points)
  //Guid AddHatch1(Hatch hatch)
  //Guid AddHatch2(Hatch hatch, DocObjects.ObjectAttributes attributes)

  //bool Delete(Guid objectId)
  //int Delete(IEnumerable<Guid> objectIds)

  int Count() const;
  BND_FileObject* ModelObjectAt(int index);
  BND_CommonObject* ObjectAt(int index);
  BND_3dmAttributes* AttributesAt(int index);
  BND_BoundingBox GetBoundingBox() const;
};

class BND_File3dmLayerTable
{
  std::shared_ptr<ONX_Model> m_model;
public:
  BND_File3dmLayerTable(std::shared_ptr<ONX_Model> m) { m_model = m; }
  int Count() const { return m_model.get()->ActiveComponentCount(ON_ModelComponent::Type::Layer); }
  //void Add(const class BND_Layer& layer);
  //public Layer FindName(string name, Guid parentId)
  //public Layer FindNameHash(NameHash nameHash)
  //public Layer FindIndex(int index)
  //public Layer FindId(Guid id)
};

class BND_ONXModel
{
  std::shared_ptr<ONX_Model> m_model;
public:
  BND_ONXModel();
  BND_ONXModel(ONX_Model* m);
  static BND_ONXModel* Read(const char* path);
  static std::string ReadNotes(const char* path);
  static int ReadArchiveVersion(const char* path);

  #if defined(ON_WASM_COMPILE)
  // from https://sean.voisen.org/blog/2018/03/rendering-images-emscripten-wasm/
  static BND_ONXModel* WasmFromByteArray(std::string buffer);
  #endif

  static BND_ONXModel* FromByteArray(int length, const void* buffer);

  bool Write(const char* path, int version);

  std::wstring GetStartSectionComments() const;
  void SetStartSectionComments(std::wstring comments);

  std::wstring GetApplicationName() const;
  void SetApplicationName(std::wstring name);
  std::wstring GetApplicationUrl() const;
  void SetApplicationUrl(std::wstring url);
  std::wstring GetApplicationDetails() const;
  void SetApplicationDetails(std::wstring details);
  std::wstring GetCreatedBy() const;
  std::wstring GetLastEditedBy() const;
  int GetRevision() const;
  void SetRevision(int r);

  BND_ONXModel_ObjectTable Objects() { return BND_ONXModel_ObjectTable(m_model); }
  BND_File3dmLayerTable Layers() { return BND_File3dmLayerTable(m_model); }
public:
  static bool ReadTest(std::wstring filepath);
};
