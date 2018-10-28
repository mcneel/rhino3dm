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
  BND_UUID AddSphere(const class BND_Sphere& sphere, const class BND_3dmAttributes* attributes);
  BND_UUID AddCurve(const class BND_Curve* curve, const class BND_3dmAttributes* attributes);
  BND_UUID AddTextDot(std::wstring text, const ON_3dPoint& location, const class BND_3dmAttributes* attributes);
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
  BND_UUID AddSurface(const class BND_Surface* surface, const class BND_3dmAttributes* attributes);
  BND_UUID AddExtrusion(const class BND_Extrusion* extrusion, const class BND_3dmAttributes* attributes);
  BND_UUID AddMesh(const class BND_Mesh* mesh, const class BND_3dmAttributes* attributes);
  BND_UUID AddBrep(const class BND_Brep* brep, const class BND_3dmAttributes* attributes);
  //Guid AddLeader1(Plane plane, IEnumerable<Point2d> points)
  //Guid AddLeader2(Plane plane, IEnumerable<Point2d> points, DocObjects.ObjectAttributes attributes)
  //Guid AddLeader3(string text, Plane plane, IEnumerable<Point2d> points, DocObjects.ObjectAttributes attributes)
  //Guid AddLeader4(string text, Plane plane, IEnumerable<Point2d> points)
  BND_UUID AddHatch(const class BND_Hatch* hatch, const class BND_3dmAttributes* attributes);

  void Delete(BND_UUID objectId);
  //int Delete(IEnumerable<Guid> objectIds)

  int Count() const;
  BND_FileObject* ModelObjectAt(int index);
  BND_CommonObject* ObjectAt(int index);
  BND_3dmAttributes* AttributesAt(int index);
  BND_BoundingBox GetBoundingBox() const;
};

class BND_File3dmMaterialTable
{
  std::shared_ptr<ONX_Model> m_model;
public:
  BND_File3dmMaterialTable(std::shared_ptr<ONX_Model> m) { m_model = m; }
  int Count() const { return m_model->ActiveComponentCount(ON_ModelComponent::Type::RenderMaterial); }
  void Add(const class BND_Material& material);
  class BND_Material* FindIndex(int index);
  class BND_Material* FindId(BND_UUID id);
};

class BND_File3dmLayerTable
{
  std::shared_ptr<ONX_Model> m_model;
public:
  BND_File3dmLayerTable(std::shared_ptr<ONX_Model> m) { m_model = m; }
  int Count() const { return m_model.get()->ActiveComponentCount(ON_ModelComponent::Type::Layer); }
  void Add(const class BND_Layer& layer);
  class BND_Layer* FindName(std::wstring name, BND_UUID parentId);
  //BND_Layer* FindNameHash(NameHash nameHash)
  class BND_Layer* FindIndex(int index);
  class BND_Layer* FindId(BND_UUID id);
};

class BND_File3dmStringTable
{
  std::shared_ptr<ONX_Model> m_model;
public:
  BND_File3dmStringTable(std::shared_ptr<ONX_Model> m) { m_model = m; }
  int Count() const;
  int DocumentUserTextCount() const;
  std::wstring GetKey(int i) const;
  std::wstring GetValue(int i) const;
  std::wstring GetValueFromKey(std::wstring key) const;
  //public string GetValue(string section, string entry)
  //public string[] GetSectionNames()
  //public string[] GetEntryNames(string section)
  //public string SetString(string section, string entry, string value)
  void SetString(std::wstring key, std::wstring value);
  //public void Delete(string section, string entry)
  void Delete(std::wstring key);
#if defined(ON_PYTHON_COMPILE)
  pybind11::tuple GetKeyValue(int i) const;
#endif
};

class BND_ONXModel
{
  std::shared_ptr<ONX_Model> m_model;
public:
  BND_ONXModel();
  BND_ONXModel(ONX_Model* m);
  static BND_ONXModel* Read(std::wstring path);
  //public static File3dm Read(string path, TableTypeFilter tableTypeFilterFilter, ObjectTypeFilter objectTypeFilter)
  //public static File3dm ReadWithLog(string path, TableTypeFilter tableTypeFilterFilter, ObjectTypeFilter objectTypeFilter, out string errorLog)
  //public static File3dm ReadWithLog(string path, out string errorLog)
  static std::string ReadNotes(std::wstring path);
  static int ReadArchiveVersion(std::wstring path);
  //public static bool ReadRevisionHistory(string path, out string createdBy, out string lastEditedBy, out int revision, out DateTime createdOn, out DateTime lastEditedOn)
  //public static void ReadApplicationData(string path, out string applicationName, out string applicationUrl, out string applicationDetails)

  #if defined(ON_WASM_COMPILE)
  // from https://sean.voisen.org/blog/2018/03/rendering-images-emscripten-wasm/
  static BND_ONXModel* WasmFromByteArray(std::string buffer);
  #endif

  static BND_ONXModel* FromByteArray(int length, const void* buffer);

  bool Write(std::wstring path, int version);
  //public bool Write(string path, File3dmWriteOptions options)
  //public bool WriteWithLog(string path, int version, out string errorLog)
  //public bool WriteWithLog(string path, File3dmWriteOptions options, out string errorLog)

  std::wstring GetStartSectionComments() const;
  void SetStartSectionComments(std::wstring comments);
  //public File3dmNotes Notes | get; set;
  std::wstring GetApplicationName() const;
  void SetApplicationName(std::wstring name);
  std::wstring GetApplicationUrl() const;
  void SetApplicationUrl(std::wstring url);
  std::wstring GetApplicationDetails() const;
  void SetApplicationDetails(std::wstring details);
  std::wstring GetCreatedBy() const;
  std::wstring GetLastEditedBy() const;
  //public DateTime Created | get;
  //public DateTime LastEdited | get;
  int GetRevision() const;
  void SetRevision(int r);
  //public File3dmSettings Settings | get;
  //public ManifestTable Manifest | get;

  BND_ONXModel_ObjectTable Objects() { return BND_ONXModel_ObjectTable(m_model); }
  BND_File3dmMaterialTable Materials() { return BND_File3dmMaterialTable(m_model); }
  //public File3dmLinetypeTable AllLinetypes
  BND_File3dmLayerTable Layers() { return BND_File3dmLayerTable(m_model); }
  //public File3dmGroupTable AllGroups | get;
  //public File3dmDimStyleTable AllDimStyles | get;
  //public File3dmHatchPatternTable AllHatchPatterns | get;
  //public File3dmInstanceDefinitionTable AllInstanceDefinitions | get;
  //public File3dmViewTable AllViews | get; 
  //public IList<ViewInfo> NamedViews | get;
  //public File3dmNamedConstructionPlanes AllNamedConstructionPlanes | get;
  //public File3dmPlugInDataTable PlugInData | get;
  BND_File3dmStringTable Strings() { return BND_File3dmStringTable(m_model); }
  //std::wstring Dump() const;
  //std::wstring DumpSummary() const;
  //public void DumpToTextLog(TextLog log)
public:
  static bool ReadTest(std::wstring filepath);
};
