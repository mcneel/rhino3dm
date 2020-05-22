using Rhino.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using IndexDistance = System.Collections.Generic.KeyValuePair<int, double>; //this could be a ValueTuple once we switch target runtime.

namespace Rhino.Geometry
{
  /// <summary>
  /// Represents event data that is passed when an item that meets certain 
  /// criteria is found and the passed RTree event is raised.
  /// </summary>
  public class RTreeEventArgs : EventArgs
  {
    IntPtr m_element_a;
    IntPtr m_element_b;
    readonly IntPtr m_pContext;

    internal RTreeEventArgs(IntPtr a, IntPtr b, IntPtr pContext)
    {
      m_element_a = a;
      m_element_b = b;
      m_pContext = pContext;
    }

    /// <summary>
    /// Gets the identifier of the found item.
    /// </summary>
    /// <exception cref="System.OverflowException">If, on 64-bit platforms, the value of this instance is too large or too small to be represented as a 32-bit signed integer.</exception>
    /// <since>5.0</since>
    public int Id { get { return m_element_a.ToInt32(); } }

    /// <summary>
    /// Gets the identifier pointer of the found item.
    /// </summary>
    /// <since>5.0</since>
    public IntPtr IdPtr { get { return m_element_a; } }

    /// <summary>
    /// Gets or sets a value that determines if the search should be conducted farther.
    /// </summary>
    /// <since>5.0</since>
    public bool Cancel { get; set; }

    /// <summary>
    /// If search is using two r-trees, IdB is element b in the search.
    /// </summary>
    /// <since>5.0</since>
    public int IdB { get { return m_element_b.ToInt32(); } }

    /// <summary>
    /// If search is using two r-trees, IdB is the element b pointer in the search.
    /// </summary>
    /// <since>5.0</since>
    public IntPtr IdBPtr { get { return m_element_b; } }

    /// <summary>
    /// Gets or sets an arbitrary object that can be attached to this event args.
    /// This object will "stick" through a single search and can represent user-defined state.
    /// </summary>
    /// <since>5.0</since>
    public object Tag { get; set; }

    /// <summary>
    /// Sphere bounds used during a search. You can modify the sphere in a search callback to
    /// help reduce the bounds to search.
    /// </summary>
    /// <since>5.0</since>
    public Sphere SearchSphere
    {
      get
      {
        Point3d center = new Point3d();
        double radius = 0;
        if( !UnsafeNativeMethods.ON_RTreeSearchContext_GetSphere(m_pContext, ref center, ref radius) )
          return Sphere.Unset;
        return new Sphere(center, radius);
      }
      set
      {
        UnsafeNativeMethods.ON_RTreeSearchContext_SetSphere(m_pContext, value.Center, value.Radius);
      }
    }

    /// <summary>
    /// Bounding box bounds used during a search. You may modify the box in a search callback
    /// to help reduce the bounds to search.
    /// </summary>
    /// <since>5.0</since>
    public BoundingBox SearchBoundingBox
    {
      get
      {
        Point3d min_pt = new Point3d();
        Point3d max_pt = new Point3d();
        if (!UnsafeNativeMethods.ON_RTreeSearchContext_GetBoundingBox(m_pContext, ref min_pt, ref max_pt))
          return BoundingBox.Unset;
        return new BoundingBox(min_pt, max_pt);
      }
      set
      {
        UnsafeNativeMethods.ON_RTreeSearchContext_SetBoundingBox(m_pContext, value.Min, value.Max);
      }
    }
  }

  /// <summary>
  /// Represents a spatial search structure based on implementations of the
  /// R-tree algorithm by Toni Gutman.
  /// </summary>
  /// <remarks>
  /// The opennurbs rtree code is a modified version of the free and unrestricted
  /// R-tree implementation obtained from http://www.superliminal.com/sources/sources.htm .
  /// </remarks>
  public class RTree : IDisposable
  {
    IntPtr m_ptr; //ON_rTree* - this class is never const
    long m_memory_pressure;
    int m_count = -1;

    /// <summary>Initializes a new, empty instance of the tree.</summary>
    /// <example>
    /// <code source='examples\vbnet\ex_closestpoint.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_closestpoint.cs' lang='cs'/>
    /// <code source='examples\py\ex_closestpoint.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    public RTree()
    {
      m_ptr = UnsafeNativeMethods.ON_RTree_New();
    }

    private RTree(IntPtr ptr)
    {
      m_ptr = ptr;
    }

    /// <summary>
    /// Constructs a new tree with an element for each face in the mesh.
    /// The element id is set to the index of the face.
    /// </summary>
    /// <param name="mesh">A mesh.</param>
    /// <returns>A new tree, or null on error.</returns>
    /// <since>5.0</since>
    public static RTree CreateMeshFaceTree(Mesh mesh)
    {
      if (mesh == null) throw new ArgumentNullException(nameof(mesh));

      RTree rc = new RTree();
      IntPtr pRtree = rc.NonConstPointer();
      IntPtr pConstMesh = mesh.ConstPointer();
      if (!UnsafeNativeMethods.ON_RTree_CreateMeshFaceTree(pRtree, pConstMesh))
      {
        rc.Dispose();
        return null;
      }
      uint size = UnsafeNativeMethods.ON_RTree_SizeOf(pRtree);
      rc.m_memory_pressure = size;
      GC.AddMemoryPressure(rc.m_memory_pressure);
      rc.m_count = mesh.Faces.Count;
      GC.KeepAlive(mesh);
      return rc;
    }

    /// <summary>
    /// Constructs a new tree with an element for each point cloud point.
    /// </summary>
    /// <param name="cloud">A point cloud.</param>
    /// <returns>A new tree, or null on error.</returns>
    /// <since>5.0</since>
    public static RTree CreatePointCloudTree(PointCloud cloud)
    {
      if (cloud == null) throw new ArgumentNullException(nameof(cloud));

      IntPtr const_ptr_cloud = cloud.ConstPointer();
      IntPtr ptr_rtree = UnsafeNativeMethods.ON_RTree_CreatePointCloudTree(const_ptr_cloud);
      if (IntPtr.Zero == ptr_rtree)
        return null;

      RTree rc = new RTree(ptr_rtree);
      uint size = UnsafeNativeMethods.ON_RTree_SizeOf(ptr_rtree);
      rc.m_memory_pressure = size;
      GC.AddMemoryPressure(rc.m_memory_pressure);
      rc.m_count = cloud.Count;
      GC.KeepAlive(cloud);
      return rc;
    }

    /// <summary>
    /// Constructs a new tree with an element for each point cloud point.
    /// </summary>
    /// <param name="points">Points.</param>
    /// <returns>A new tree, or null on error.</returns>
    /// <since>6.0</since>
    public static RTree CreateFromPointArray(IEnumerable<Point3d> points)
    {
      return CreateFromPointArray(points, out _, out _);
    }

    internal static RTree CreateFromPointArray(IEnumerable<Point3d> points, out Point3d[] potentiallyOversizePtsArray, out int count)
    {
      if (points == null) throw new ArgumentNullException(nameof(points));

      potentiallyOversizePtsArray = RhinoListHelpers.GetConstArray(points, out count);

      IntPtr ptr_rtree = UnsafeNativeMethods.ON_RTree_CreatePointArrayTree(potentiallyOversizePtsArray, count);

      if (ptr_rtree == IntPtr.Zero)
        throw new InvalidOperationException("Could not create a tree with these inputs.");

      var tree = new RTree(ptr_rtree)
      {
        m_count = count //avoid thread-unsafe tree traversal to fill Count.
      };

      return tree;
    }

    /// <summary>Inserts an element into the tree.</summary>
    /// <param name="point">A point.</param>
    /// <param name="elementId">A number.</param>
    /// <returns>true if element was successfully inserted.</returns>
    /// <example>
    /// <code source='examples\vbnet\ex_closestpoint.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_closestpoint.cs' lang='cs'/>
    /// <code source='examples\py\ex_closestpoint.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    public bool Insert(Point3d point, int elementId)
    {
      return Insert(new BoundingBox(point, point), elementId);
    }

    /// <summary>Inserts an element into the tree.</summary>
    /// <param name="point">A point.</param>
    /// <param name="elementId">A pointer.</param>
    /// <returns>true if element was successfully inserted.</returns>
    /// <since>5.0</since>
    public bool Insert(Point3d point, IntPtr elementId)
    {
      return Insert(new BoundingBox(point, point), elementId);
    }

    /// <summary>Inserts an element into the tree.</summary>
    /// <param name="box">A bounding box.</param>
    /// <param name="elementId">A number.</param>
    /// <returns>true if element was successfully inserted.</returns>
    /// <since>5.0</since>
    public bool Insert(BoundingBox box, int elementId)
    {
      return Insert(box, new IntPtr(elementId));
    }

    /// <summary>Inserts an element into the tree.</summary>
    /// <param name="box">A bounding box.</param>
    /// <param name="elementId">A pointer.</param>
    /// <returns>true if element was successfully inserted.</returns>
    /// <since>5.0</since>
    public bool Insert(BoundingBox box, IntPtr elementId)
    {
      m_count = -1; 
      IntPtr pThis = NonConstPointer();
      return UnsafeNativeMethods.ON_RTree_InsertRemove(pThis, true, box.Min, box.Max, elementId);
    }

    /// <summary>Inserts an element into the tree.</summary>
    /// <param name="point">A point.</param>
    /// <param name="elementId">A number.</param>
    /// <returns>true if element was successfully inserted.</returns>
    /// <since>5.0</since>
    public bool Insert(Point2d point, int elementId)
    {
      return Insert(new Point3d(point.X, point.Y, 0), elementId);
    }

    /// <summary>Inserts an element into the tree.</summary>
    /// <param name="point">A point.</param>
    /// <param name="elementId">A pointer.</param>
    /// <returns>true if element was successfully inserted.</returns>
    /// <since>5.0</since>
    public bool Insert(Point2d point, IntPtr elementId)
    {
      return Insert(new Point3d(point.X, point.Y, 0), elementId);
    }

    /// <summary>Removes an element from the tree.</summary>
    /// <param name="point">A point.</param>
    /// <param name="elementId">A number.</param>
    /// <returns>true if element was successfully removed.</returns>
    /// <since>5.0</since>
    public bool Remove(Point3d point, int elementId)
    {
      return Remove(new BoundingBox(point, point), elementId);
    }

    /// <summary>Removes an element from the tree.</summary>
    /// <param name="point">A point.</param>
    /// <param name="elementId">A pointer.</param>
    /// <returns>true if element was successfully removed.</returns>
    /// <since>5.0</since>
    public bool Remove(Point3d point, IntPtr elementId)
    {
      return Remove(new BoundingBox(point, point), elementId);
    }

    /// <summary>Removes an element from the tree.</summary>
    /// <param name="box">A bounding box.</param>
    /// <param name="elementId">A number.</param>
    /// <returns>true if element was successfully removed.</returns>
    /// <since>5.0</since>
    public bool Remove(BoundingBox box, int elementId)
    {
      return Remove(box, new IntPtr(elementId));
    }

    /// <summary>Removes an element from the tree.</summary>
    /// <param name="box">A bounding box.</param>
    /// <param name="elementId">A pointer.</param>
    /// <returns>true if element was successfully removed.</returns>
    /// <since>5.0</since>
    public bool Remove(BoundingBox box, IntPtr elementId)
    {
      m_count = -1; 
      IntPtr pThis = NonConstPointer();
      return UnsafeNativeMethods.ON_RTree_InsertRemove(pThis, false, box.Min, box.Max, elementId);
    }

    /// <summary>Removes an element from the tree.</summary>
    /// <param name="point">A point.</param>
    /// <param name="elementId">A number.</param>
    /// <returns>true if element was successfully removed.</returns>
    /// <since>5.0</since>
    public bool Remove(Point2d point, int elementId)
    {
      return Remove(new Point3d(point.X, point.Y, 0), elementId);
    }

    /// <summary>
    /// Removes all elements.
    /// </summary>
    /// <since>5.0</since>
    public void Clear()
    {
      m_count = -1; 
      IntPtr pThis = NonConstPointer();
      UnsafeNativeMethods.ON_RTree_RemoveAll(pThis);
    }

    /// <summary>
    /// Gets the number of items in this tree.
    /// </summary>
    /// <since>5.0</since>
    public int Count
    {
      get
      {
        if (m_count < 0)
        {
          IntPtr pThis = NonConstPointer();
          m_count = UnsafeNativeMethods.ON_RTree_ElementCount(pThis);
        }
        return m_count;
      }
    }
    
    static int m_next_serial_number = 1;
    class Callbackholder
    {
      public RTree Sender { get; set; }
      public int SerialNumber { get; set; }
      public EventHandler<RTreeEventArgs> Callback { get; set; }
      public object Tag { get; set; }
    }

    [ThreadStatic]
    static List<Callbackholder> m_callbacks;

    internal delegate int SearchCallback(int serial_number, IntPtr idA, IntPtr idB, IntPtr pContext);
    private static int CustomSearchCallback(int serial_number, IntPtr idA, IntPtr idB, IntPtr pContext)
    {
      Callbackholder cbh = null;
      for (int i = 0; i < m_callbacks.Count; i++)
      {
        Callbackholder holder = m_callbacks[i];
        if (holder.SerialNumber == serial_number)
        {
          cbh = holder;
          break;
        }
      }
      int rc = 1;
      if (cbh != null)
      {
        RTreeEventArgs e = new RTreeEventArgs(idA, idB, pContext);
        e.Tag = cbh.Tag;
        cbh.Callback(cbh.Sender, e);
        if (e.Cancel)
          rc = 0;
        cbh.Tag = e.Tag;
      }
      return rc;
    }

    /// <summary>
    /// Searches for items in a bounding box.
    /// <para>The bounding box can be singular and contain exactly one single point.</para>
    /// </summary>
    /// <param name="box">A bounding box.</param>
    /// <param name="callback">An event handler to be raised when items are found.</param>
    /// <returns>
    /// true if entire tree was searched. It is possible no results were found.
    /// </returns>
    /// <since>5.0</since>
    public bool Search(BoundingBox box, EventHandler<RTreeEventArgs> callback)
    {
      return Search(box, callback, null);
    }

    /// <summary>
    /// Searches for items in a bounding box.
    /// <para>The bounding box can be singular and contain exactly one single point.</para>
    /// </summary>
    /// <param name="box">A bounding box.</param>
    /// <param name="callback">An event handler to be raised when items are found.</param>
    /// <param name="tag">State to be passed inside the <see cref="RTreeEventArgs"/> Tag property.</param>
    /// <returns>
    /// true if entire tree was searched. It is possible no results were found.
    /// </returns>
    /// <since>5.0</since>
    public bool Search(BoundingBox box, EventHandler<RTreeEventArgs> callback, object tag)
    {
      IntPtr pConstTree = ConstPointer();
      if (m_callbacks == null)
        m_callbacks = new List<Callbackholder>();
      Callbackholder cbh = new Callbackholder();
      cbh.SerialNumber = m_next_serial_number++;
      cbh.Callback = callback;
      cbh.Sender = this;
      cbh.Tag = tag;
      m_callbacks.Add(cbh);
      SearchCallback searcher = CustomSearchCallback;
      bool rc = UnsafeNativeMethods.ON_RTree_Search(pConstTree, box.Min, box.Max, cbh.SerialNumber, searcher);
      for (int i = 0; i < m_callbacks.Count; i++)
      {
        if (m_callbacks[i].SerialNumber == cbh.SerialNumber)
        {
          m_callbacks.RemoveAt(i);
          break;
        }
      }
      return rc;
    }

    /// <summary>
    /// Searches for items in a sphere.
    /// </summary>
    /// <param name="sphere">bounds used for searching.</param>
    /// <param name="callback">An event handler to be raised when items are found.</param>
    /// <returns>
    /// true if entire tree was searched. It is possible no results were found.
    /// </returns>
    /// <since>5.0</since>
    public bool Search(Sphere sphere, EventHandler<RTreeEventArgs> callback)
    {
      return Search(sphere, callback, null);
    }

    /// <summary>
    /// Searches for items in a sphere.
    /// </summary>
    /// <param name="sphere">bounds used for searching.</param>
    /// <param name="callback">An event handler to be raised when items are found.</param>
    /// <param name="tag">State to be passed inside the <see cref="RTreeEventArgs"/> Tag property.</param>
    /// <returns>
    /// true if entire tree was searched. It is possible no results were found.
    /// </returns>
    /// <example>
    /// <code source='examples\vbnet\ex_closestpoint.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_closestpoint.cs' lang='cs'/>
    /// <code source='examples\py\ex_closestpoint.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    public bool Search(Sphere sphere, EventHandler<RTreeEventArgs> callback, object tag)
    {
      IntPtr pConstTree = ConstPointer();
      if (m_callbacks == null)
        m_callbacks = new List<Callbackholder>();
      Callbackholder cbh = new Callbackholder();
      cbh.SerialNumber = m_next_serial_number++;
      cbh.Callback = callback;
      cbh.Sender = this;
      cbh.Tag = tag;
      m_callbacks.Add(cbh);
      SearchCallback searcher = CustomSearchCallback;
      bool rc = UnsafeNativeMethods.ON_RTree_SearchSphere(pConstTree, sphere.Center, sphere.Radius, cbh.SerialNumber, searcher);
      for (int i = 0; i < m_callbacks.Count; i++)
      {
        if (m_callbacks[i].SerialNumber == cbh.SerialNumber)
        {
          m_callbacks.RemoveAt(i);
          break;
        }
      }
      return rc;
    }

    /// <summary>
    /// Searches two R-trees for all pairs elements whose bounding boxes overlap.
    /// </summary>
    /// <param name="treeA">A first tree.</param>
    /// <param name="treeB">A second tree.</param>
    /// <param name="tolerance">
    /// If the distance between a pair of bounding boxes is less than tolerance,
    /// then callback is called.
    /// </param>
    /// <param name="callback">A callback event handler.</param>
    /// <returns>
    /// true if entire tree was searched.  It is possible no results were found.
    /// </returns>
    /// <since>5.0</since>
    public static bool SearchOverlaps(RTree treeA, RTree treeB, double tolerance, EventHandler<RTreeEventArgs> callback)
    {
      IntPtr pConstTreeA = treeA.ConstPointer();
      IntPtr pConstTreeB = treeB.ConstPointer();
      if (m_callbacks == null)
        m_callbacks = new List<Callbackholder>();
      Callbackholder cbh = new Callbackholder();
      cbh.SerialNumber = m_next_serial_number++;
      cbh.Callback = callback;
      cbh.Sender = null;
      m_callbacks.Add(cbh);
      SearchCallback searcher = CustomSearchCallback;
      bool rc = UnsafeNativeMethods.ON_RTree_Search2(pConstTreeA, pConstTreeB, tolerance, cbh.SerialNumber, searcher);
      for (int i = 0; i < m_callbacks.Count; i++)
      {
        if (m_callbacks[i].SerialNumber == cbh.SerialNumber)
        {
          m_callbacks.RemoveAt(i);
          break;
        }
      }
      return rc;
    }

    #region pointer / disposable handlers
    IntPtr ConstPointer() { return m_ptr; }
    IntPtr NonConstPointer() { return m_ptr; }

    /// <summary>
    /// Passively reclaims unmanaged resources when the class user did not explicitly call Dispose().
    /// </summary>
    ~RTree()
    {
      Dispose(false);
    }

    /// <summary>
    /// Actively reclaims unmanaged resources that this instance uses.
    /// </summary>
    /// <since>5.0</since>
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
      if (IntPtr.Zero != m_ptr)
      {
        UnsafeNativeMethods.ON_RTree_Delete(m_ptr);
        m_ptr = IntPtr.Zero;
      }
      if (m_memory_pressure > 0)
      {
        GC.RemoveMemoryPressure(m_memory_pressure);
        m_memory_pressure = 0;
      }
    }
    #endregion

    #region Simpler usage methods

    /// <summary>
    /// Finds the point in a list of 3D points that is closest to a test point.
    /// </summary>
    /// <param name="pointcloud">A point cloud to be searched.</param>
    /// <param name="needlePts">Points to search for.</param>
    /// <param name="limitDistance">The maximum allowed distance.</param>
    /// <returns>An enumerable of arrays of indices; each array contains the indices for each of the needlePts.</returns>
    /// <since>6.0</since>
    public static IEnumerable<int[]> PointCloudClosestPoints(PointCloud pointcloud, IEnumerable<Point3d> needlePts, double limitDistance)
    {
      var tree = RTree.CreatePointCloudTree(pointcloud);
      var points = pointcloud.AsReadOnlyListOfPoints();
      return new LazyClosestPointsDistanceEnumerator(tree, points, needlePts, limitDistance);
    }

    /// <summary>
    /// Finds the point in a list of 3D points that is closest to a test point.
    /// </summary>
    /// <param name="hayPoints">A series of points.</param>
    /// <param name="needlePts">Points to search for.</param>
    /// <param name="limitDistance">The maximum allowed distance.</param>
    /// <returns>An enumerable of arrays of indices; each array contains the indices for each of the needlePts.</returns>
    /// <since>6.0</since>
    public static IEnumerable<int[]> Point3dClosestPoints(IEnumerable<Point3d> hayPoints, IEnumerable<Point3d> needlePts, double limitDistance)
    {
      var tree = CreateFromPointArray(hayPoints, out Point3d[] oversized_pts_array, out _);

      return new LazyClosestPointsDistanceEnumerator(tree, oversized_pts_array, needlePts, limitDistance);
    }

    static void CalledWhenFoundDistance(object sender, RTreeEventArgs e)
    {
      var data = e.Tag as List<int>;
      if (data == null) return;

      data.Add(e.Id);
    }

    class LazyClosestPointsDistanceEnumerator : IEnumerable<int[]>, IDisposable
    {
      RTree m_tree;
      IReadOnlyList<Point3d> m_oversized_tree_pts_array;
      IEnumerable<Point3d> m_needlePts;
      double m_limitDistance;

      public LazyClosestPointsDistanceEnumerator(RTree tree, IReadOnlyList<Point3d> oversized_tree_pts_array, IEnumerable<Point3d> needlePts, double limitDistance)
      {
        m_tree = tree;
        m_oversized_tree_pts_array = oversized_tree_pts_array;
        m_needlePts = needlePts;
        m_limitDistance = limitDistance;
      }

      //note: treepoints can be of length (tree.Count) or more
      public IEnumerator<int[]> GetEnumerator()
      {
        foreach (var needle in m_needlePts)
        {
          var data = new List<int>();
          var sphere = new Sphere(needle, m_limitDistance);

          if (m_tree.Search(sphere, CalledWhenFoundDistance, data))
          {
            var distances = new double[data.Count];
            for (int i = 0; i < data.Count; i++)
            {
              distances[i] = needle.DistanceToSquared(m_oversized_tree_pts_array[data[i]]);
            }

            var array = data.ToArray();
            Array.Sort(distances, array);

            yield return array;
          }
          else throw new InvalidOperationException("An error occurred when iterating tree.");
        }
      }

      IEnumerator IEnumerable.GetEnumerator()
      {
        return GetEnumerator();
      }

      public void Dispose()
      {
        if (m_tree != null)
          m_tree.Dispose();
      }
    }

    //-- Types assisting k-neighbor search

    /// <summary>
    /// Finds a certain amount of points in a list of 3D points that are the k-closest to a test point.
    /// </summary>
    /// <param name="pointcloud">A point cloud to be searched.</param>
    /// <param name="needlePts">Points to search for.</param>
    /// <param name="amount">The required amount of closest neighbors to find.</param>
    /// <returns>An enumerable of arrays of indices; each array contains the indices for each of the needlePts.</returns>
    /// <seealso cref="RhinoList.PointCloudKNeighbors(PointCloud, IEnumerable{Point3d}, int)"/>
    /// <since>6.0</since>
    public static IEnumerable<int[]> PointCloudKNeighbors(PointCloud pointcloud, IEnumerable<Point3d> needlePts, int amount)
    {
      var tree = RTree.CreatePointCloudTree(pointcloud);
      var points = pointcloud.AsReadOnlyListOfPoints();
      return new LazyClosestPointsAmountEnumerator(tree, points, needlePts, amount);
    }

    /// <summary>
    /// Finds a certain amount of points in a list of 3D points that are the k-closest to a test point.
    /// </summary>
    /// <param name="hayPoints">A series of points.</param>
    /// <param name="needlePts">Points to search for.</param>
    /// <param name="amount">The required amount of closest neighbors to find.</param>
    /// <returns>An enumerable of arrays of indices; each array contains the indices for each of the needlePts.</returns>
    /// <seealso cref="RhinoList.Point3dKNeighbors(IEnumerable{Point3d}, IEnumerable{Point3d}, int)"/>
    /// <since>6.0</since>
    public static IEnumerable<int[]> Point3dKNeighbors(IEnumerable<Point3d> hayPoints, IEnumerable<Point3d> needlePts, int amount)
    {
      var tree = CreateFromPointArray(hayPoints, out Point3d[] oversized_pts_array, out _);

      return new LazyClosestPointsAmountEnumerator(tree, oversized_pts_array, needlePts, amount);
    }

    static void CalledWhenFoundAmount(object sender, RTreeEventArgs e)
    {
      var data = e.Tag as AmountSearchData;
      if (data == null) return;
      
      Point3d found = data.Points[e.Id];
      double sq_distance = data.Needle.DistanceToSquared(found);
      if (data.Count < data.AmountTarget || data.Max.Value > sq_distance)
      {
        if (data.Count == data.AmountTarget)
        {
          data.Remove(data.Max);
          data.Add(new IndexDistance(e.Id, sq_distance));
          // shrink the sphere to help improve the test
          e.SearchSphere = new Sphere(data.Needle, Math.Sqrt(data.Max.Value) + RhinoMath.SqrtEpsilon);
        }
        else
          data.Add(new IndexDistance(e.Id, sq_distance));
      }
    }

    class AmountSearchData : SortedSet<IndexDistance> //derive so only one object for each point
    {
      public AmountSearchData(IComparer<IndexDistance> comparer) : base(comparer) { }

      public RTree Tree;
      public IReadOnlyList<Point3d> Points;
      public Point3d Needle;
      public int AmountTarget;
    }

    private class DistanceThenIndexComparer : Comparer<IndexDistance>
    {
      private DistanceThenIndexComparer() { }
      static DistanceThenIndexComparer() { g_instance = new DistanceThenIndexComparer();  }

      static DistanceThenIndexComparer g_instance;

      public static DistanceThenIndexComparer Instance 
      {
        get { return g_instance; }
      }

      public override int Compare(IndexDistance x, IndexDistance y)
      {
        int first = x.Value.CompareTo(y.Value);
        return first != 0 ? first : (x.Key - y.Key);
      }
    }

    class LazyClosestPointsAmountEnumerator : IEnumerable<int[]>, IDisposable
    {
      RTree m_tree;
      IReadOnlyList<Point3d> m_oversized_tree_pts_array;
      IEnumerable<Point3d> m_needlePts;
      int m_amount;

      public LazyClosestPointsAmountEnumerator(RTree tree, IReadOnlyList<Point3d> oversized_tree_pts_array, IEnumerable<Point3d> needlePts, int amount)
      {
        if (tree.Count < amount) throw new ArgumentException("Requested more items than the quantity present in tree.");

        m_tree = tree;
        m_oversized_tree_pts_array = oversized_tree_pts_array;
        m_needlePts = needlePts;
        m_amount = amount;
      }
      public IEnumerator<int[]> GetEnumerator()
      {
        if (m_amount == 0) yield break;

        var heap = new AmountSearchData(DistanceThenIndexComparer.Instance) { Tree = m_tree, Points = m_oversized_tree_pts_array, AmountTarget = m_amount, };

        int count = -1;
        foreach (var needle in m_needlePts)
        {
          count++;

          heap.Clear();
          heap.Needle = needle;

          double min_max_dist = double.MinValue;
          for (int i = 0; i < m_amount; i++)
          {
            var test = needle.DistanceToSquared(m_oversized_tree_pts_array[i]);
            if (test > min_max_dist) min_max_dist = test; //pick larger
          }

          var sphere = new Sphere(needle, Math.Sqrt(min_max_dist) + RhinoMath.SqrtEpsilon);

          if (m_tree.Search(sphere, CalledWhenFoundAmount, heap))
          {
            yield return heap.Select(item => item.Key).ToArray();
          }
          else throw new InvalidOperationException("An error occurred when iterating tree.");
        }
      }
      IEnumerator IEnumerable.GetEnumerator()
      {
        return GetEnumerator();
      }

      public void Dispose()
      {
        if (m_tree != null)
          m_tree.Dispose();
      }
    }

    #endregion
  }
}
