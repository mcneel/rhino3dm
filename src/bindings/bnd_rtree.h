#include "bindings.h"

#pragma once

#if defined(ON_PYTHON_COMPILE)
void initRTreeBindings(rh3dmpymodule& m);
#else
void initRTreeBindings(void* m);
#endif

class BND_RTree
{
  ON_RTree m_rtree;
public:
  BND_RTree() = default;
  //    public static RTree CreateMeshFaceTree(Mesh mesh)
  //    public static RTree CreatePointCloudTree(PointCloud cloud)
  //    public static RTree CreateFromPointArray(IEnumerable<Point3d> points)
  //    public bool Insert(Point3d point, int elementId)
  //    public bool Insert(Point3d point, IntPtr elementId)
  //    public bool Insert(BoundingBox box, int elementId)
  //    public bool Insert(BoundingBox box, IntPtr elementId)
  //    public bool Insert(Point2d point, int elementId)
  //    public bool Insert(Point2d point, IntPtr elementId)
  //    public bool Remove(Point3d point, int elementId)
  //    public bool Remove(Point3d point, IntPtr elementId)
  //    public bool Remove(BoundingBox box, int elementId)
  //    public bool Remove(BoundingBox box, IntPtr elementId)
  //    public bool Remove(Point2d point, int elementId)
  //    public void Clear()
  //    public int Count[get;]
  //    public bool Search(BoundingBox box, EventHandler<RTreeEventArgs> callback)
  //    public bool Search(BoundingBox box, EventHandler<RTreeEventArgs> callback, object tag)
  //    public bool Search(Sphere sphere, EventHandler<RTreeEventArgs> callback)
  //    public bool Search(Sphere sphere, EventHandler<RTreeEventArgs> callback, object tag)
  //    public static bool SearchOverlaps(RTree treeA, RTree treeB, double tolerance, EventHandler<RTreeEventArgs> callback)
  //    public static IEnumerable<int[]> PointCloudClosestPoints(PointCloud pointcloud, IEnumerable<Point3d> needlePts, double limitDistance)
  //    public static IEnumerable<int[]> Point3dClosestPoints(IEnumerable<Point3d> hayPoints, IEnumerable<Point3d> needlePts, double limitDistance)
  //    public static IEnumerable<int[]> PointCloudKNeighbors(PointCloud pointcloud, IEnumerable<Point3d> needlePts, int amount)
  //    public static IEnumerable<int[]> Point3dKNeighbors(IEnumerable<Point3d> hayPoints, IEnumerable<Point3d> needlePts, int amount)
};
