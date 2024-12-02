#pragma warning disable 1591
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Rhino.Runtime.InteropWrappers;

#if RHINO_SDK
namespace Rhino.Display
{
  /// <summary>
  /// Graphics display techologies.
  /// </summary>
  /// <since>7.0</since>
  public enum DisplayTechnology : int
  { 
    None = 0,
    OpenGL = 1,
    Metal = 2,
    DirectX = 3,
    Software = 4,
    Vulkan = 5
  };

  /// <summary>
  /// Defines enumerated constants for display blend modes.
  /// </summary>
  /// <since>5.0</since>
  public enum BlendMode
  {
    /// <summary>
    /// Blends using 0.
    /// </summary>
    Zero = 0,

    /// <summary>
    /// Blends using 1.
    /// </summary>
    One = 1,

    /// <summary>
    /// Blends using source color.
    /// </summary>
    SourceColor = 0x0300,

    /// <summary>
    /// Blends using 1-source color.
    /// </summary>
    OneMinusSourceColor = 0x0301,

    /// <summary>
    /// Blends using the source alpha channel.
    /// </summary>
    SourceAlpha = 0x0302,

    /// <summary>
    /// Blends using 1-the source alpha channel.
    /// </summary>
    OneMinusSourceAlpha = 0x0303,

    /// <summary>
    /// Blends using the destination alpha channel.
    /// </summary>
    DestinationAlpha = 0x0304,

    /// <summary>
    /// Blends using 1-the destination alpha channel.
    /// </summary>
    OneMinusDestinationAlpha = 0x0305,

    /// <summary>
    /// Blends using the destination color.
    /// </summary>
    DestinationColor = 0x0306,

    /// <summary>
    /// Blends using 1-the destination color.
    /// </summary>
    OneMinusDestinationColor = 0x0307,

    /// <summary>
    /// Blends using the source alpha saturation.
    /// </summary>
    SourceAlphaSaturate = 0x0308
  }

  /// <summary>
  /// A bitmap resource that can be used by the display pipeline (currently only
  /// in OpenGL display).  Reuse DisplayBitmaps for drawing if possible; it is
  /// much more expensive to construct new DisplayBitmaps than it is to reuse
  /// existing DisplayBitmaps.
  /// </summary>
  public class DisplayBitmap : IDisposable
  {
    IntPtr m_ptr_display_bmp;
    internal IntPtr NonConstPointer() { return m_ptr_display_bmp; }

    /// <summary>
    /// Constructs a DisplayBitmap from an existing bitmap.
    /// </summary>
    /// <param name="bitmap">The original bitmap.</param>
    /// <since>5.0</since>
    public DisplayBitmap(System.Drawing.Bitmap bitmap)
    {
      IntPtr hbmp = bitmap.GetHbitmap();
      m_ptr_display_bmp = UnsafeNativeMethods.CRhCmnDisplayBitmap_New(null, hbmp);
    }

    /// <summary>
    /// Creates a DisplayBitmap either from a path, or a bitmap.
    /// If the path is null, a random tag name will be used.
    /// If the bitmap is null, the bitmap will be loaded from the path.
    /// If both are null, the object is invalid.
    /// if both are valid objects, the bitmap will be used and it will be added to Rhino's bitmap
    /// cache with the path supplied.  In other words, this is a way to add a bitmap from memory
    /// directly into Rhino's memory cache.
    /// </summary>
    /// <param name="path">If null, use a temporary tag name.  If non-null, use that path in Rhino's bitmap cache.  Note, this version does not support URLs.</param>
    /// <param name="bitmap">If null, load the bitmap from the supplied path.  If non-null, creates the bitmap from the data supplied.</param>
    /// <since>7.16</since>
    public DisplayBitmap(string path, System.Drawing.Bitmap bitmap)
    {
      m_ptr_display_bmp = UnsafeNativeMethods.CRhCmnDisplayBitmap_New(path, bitmap.GetHbitmap());
    }

    private DisplayBitmap(IntPtr pBmp)
    {
      m_ptr_display_bmp = pBmp;
    }

    /// <summary>
    /// Update the image used for this DisplayBitmap
    /// </summary>
    /// <param name="bitmap"></param>
    public void Update(System.Drawing.Bitmap bitmap)
    {
      IntPtr hbmp = bitmap.GetHbitmap();
      if (m_ptr_display_bmp == IntPtr.Zero)
      {
        m_ptr_display_bmp = UnsafeNativeMethods.CRhCmnDisplayBitmap_New(null, hbmp);
        return;
      }
      UnsafeNativeMethods.CRhCmnDisplayBitmap_Update(m_ptr_display_bmp, hbmp);
    }

    /// <summary>
    /// Load a DisplayBitmap from and image file on disk or from URL. If path starts
    /// with http:// or https:// then an attempt is made to load the bitmap from an
    /// online resource
    /// </summary>
    /// <param name="path">A location from which to load the file.</param>
    /// <returns>The new display bitmap, or null on error.</returns>
    /// <since>5.0</since>
    public static DisplayBitmap Load(string path)
    {
      if (path.StartsWith("http://", StringComparison.InvariantCultureIgnoreCase) ||
          path.StartsWith("https://", StringComparison.InvariantCultureIgnoreCase))
      {
        try
        {
          using (var client = new System.Net.WebClient())
          {
            var stream = client.OpenRead(path);
            var bmp = new System.Drawing.Bitmap(stream);
            return new DisplayBitmap(bmp);
          }
        }
        catch(Exception)
        {
          return null;
        }
      }

      IntPtr ptr_bmp = UnsafeNativeMethods.CRhCmnDisplayBitmap_New(path, IntPtr.Zero);
      if (IntPtr.Zero == ptr_bmp)
        return null;
      return new DisplayBitmap(ptr_bmp);
    }

    /// <summary>
    /// Sets blending function used to determine how this bitmap is blended
    /// with the current frame buffer color.  The default setting is SourceAlpha
    /// for source and OneMinusSourceAlpha for destination.  See OpenGL's
    /// glBlendFunc for details.
    /// <para>http://www.opengl.org/sdk/docs/man/xhtml/glBlendFunc.xml</para>
    /// </summary>
    /// <param name="source">The source blend mode.</param>
    /// <param name="destination">The destination blend mode.</param>
    /// <since>5.0</since>
    public void SetBlendFunction(BlendMode source, BlendMode destination)
    {
      UnsafeNativeMethods.CRhCmnDisplayBitmap_SetBlendFunction(m_ptr_display_bmp, (int)source, (int)destination);
    }

    /// <summary>
    /// Gets the source and destination blend modes.
    /// </summary>
    /// <param name="source">The source blend mode is assigned to this out parameter.</param>
    /// <param name="destination">The destination blend mode is assigned to this out parameter.</param>
    /// <since>5.0</since>
    public void GetBlendModes(out BlendMode source, out BlendMode destination)
    {
      int s = 0, d = 0;
      UnsafeNativeMethods.CRhCmnDisplayBitmap_GetBlendFunction(m_ptr_display_bmp, ref s, ref d);
      source = (BlendMode)s;
      destination = (BlendMode)d;
    }

    /// <summary>
    /// Size of the underlying bitmap image
    /// </summary>
    /// <since>7.0</since>
    public System.Drawing.Size Size
    {
      get
      {
        int width = 0, height = 0;
        UnsafeNativeMethods.CRhCmnDisplayBitmap_Size(m_ptr_display_bmp, ref width, ref height);
        return new System.Drawing.Size(width, height);
      }
    }

    /// <summary>
    /// Passively reclaims unmanaged resources when the class user did not explicitly call Dispose().
    /// </summary>
    ~DisplayBitmap() { Dispose(false); }

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
      if (IntPtr.Zero != m_ptr_display_bmp)
      {
        UnsafeNativeMethods.CRhCmnDisplayBitmap_Delete(m_ptr_display_bmp);
        // This is probably where we want to store the HGLRC/GLuint
        // combinations or textures that need to be purged. We can
        // then perform the purging the next time a different bitmap
        // is drawn
      }
      m_ptr_display_bmp = IntPtr.Zero;
    }

  }

  public class DisplayBitmapDrawList
  {
    internal Geometry.Point3d[] m_points;
    internal int[] m_colors_argb;

    /// <since>5.0</since>
    public DisplayBitmapDrawList()
    {
      MaximumCachedSortLists = 10;
      SortAngleTolerance = RhinoMath.ToRadians(5);
    }

    Geometry.BoundingBox m_bbox = Geometry.BoundingBox.Unset;
    /// <since>5.0</since>
    public Geometry.BoundingBox BoundingBox
    {
      get{ return m_bbox; }
    }

    /// <summary>
    /// Maximum number of cached sort order index lists stored on this class.
    /// Default is 10, but depending on the number of points in this list you
    /// may get better performance by setting this value to a certain percentage
    /// of the point count.
    /// </summary>
    /// <since>5.0</since>
    public int MaximumCachedSortLists { get; set; }

    /// <summary>
    /// Angle in radians used to determine if an index list is "parallel enough"
    /// to a viewports camera angle. Default is 0.0873 radians (5 degrees)
    /// </summary>
    /// <since>5.0</since>
    public double SortAngleTolerance { get; set; }

    
    class DirectedOrder
    {
      int[] m_indices;
      public DirectedOrder(int count)
      {
        m_indices = new int[count];
        for (int i = 0; i < count; i++)
          m_indices[i] = i;
      }
      public Geometry.Vector3d Direction { get; set; }
      public int[] Indices
      {
        get { return m_indices; }
        set { m_indices = value; }
      }
    }
    LinkedList<DirectedOrder> m_order = new LinkedList<DirectedOrder>();

    Geometry.Vector3d m_camera_vector;
    int IndexComparison(int a, int b)
    {
      double dist_a = m_points[a].m_x * m_camera_vector.m_x + m_points[a].m_y * m_camera_vector.m_y + m_points[a].m_z * m_camera_vector.m_z;
      double dist_b = m_points[b].m_x * m_camera_vector.m_x + m_points[b].m_y * m_camera_vector.m_y + m_points[b].m_z * m_camera_vector.m_z;
      if (dist_a < dist_b)
        return 1;
      if (dist_a > dist_b)
        return -1;
      return 0;
    }

    /// <since>5.0</since>
    public int[] Sort(Geometry.Vector3d cameraDirection)
    {
      DirectedOrder d = null;

      var node = m_order.First;
      while(node!=null )
      {
        if( node.Value.Direction.IsParallelTo(cameraDirection, SortAngleTolerance) == 1 )
        {
          // move node to head to keep at top of MRU cache
          m_order.Remove(node);
          m_order.AddFirst(node);
          return node.Value.Indices;
        }
        d = node.Value;
        node = node.Next;
      }

      if (d == null || m_order.Count < MaximumCachedSortLists)
        d = new DirectedOrder(m_points.Length);

      m_camera_vector = cameraDirection;
      int[] indices = d.Indices;
      Array.Sort(indices, IndexComparison);
      d.Direction = cameraDirection;
      m_order.AddFirst(d);
      if (m_order.Count > MaximumCachedSortLists)
        m_order.RemoveLast();
      return indices;
    }

    /// <since>5.0</since>
    public void SetPoints(IEnumerable<Geometry.Point3d> points)
    {
      SetPoints(points, System.Drawing.Color.White);
    }

    /// <since>5.0</since>
    public void SetPoints(IEnumerable<Geometry.Point3d> points, System.Drawing.Color blendColor)
    {
      m_order = new LinkedList<DirectedOrder>();
      var _points = new List<Geometry.Point3d>(points);
      m_points = _points.ToArray();
      m_colors_argb = new int[] { blendColor.ToArgb() };
      m_bbox = new Geometry.BoundingBox(m_points);
    }

    /// <since>5.0</since>
    public void SetPoints(IEnumerable<Rhino.Geometry.Point3d> points, IEnumerable<System.Drawing.Color> colors)
    {
      var _points = new List<Geometry.Point3d>(points);
      var _colors = new List<System.Drawing.Color>(colors);
      if (_points.Count != _colors.Count)
        throw new ArgumentException("length of points must be the same as length of colors");

      m_order = new LinkedList<DirectedOrder>();
      m_points = _points.ToArray();
      m_colors_argb = new int[_colors.Count];
      for (int i = 0; i < _colors.Count; i++)
        m_colors_argb[i] = _colors[i].ToArgb();
      m_bbox = new Geometry.BoundingBox(m_points);
    }
  }

  public class DisplayPointAttributes
  {
    internal static bool AreEqual(DisplayPointAttributes a, DisplayPointAttributes b)
    {
      if (a == null && b == null)
        return true;
      if (a == null || b == null)
        return false;

      return a.PointStyle == b.PointStyle &&
        a.StrokeColor == b.StrokeColor &&
        a.FillColor == b.FillColor &&
        a.Diameter == b.Diameter &&
        a.StrokeWidth == b.StrokeWidth &&
        a.SecondarySize == b.SecondarySize &&
        a.RotationRadians == b.RotationRadians;
    }

    /// <since>8.0</since>
    public DisplayPointAttributes()
    {
    }

    /// <since>8.0</since>
    public DisplayPointAttributes(DisplayPointAttributes attributes)
    {
      PointStyle = attributes.PointStyle;
      StrokeColor = attributes.StrokeColor;
      FillColor = attributes.FillColor;
      Diameter = attributes.Diameter;
      StrokeWidth = attributes.StrokeWidth;
      SecondarySize = attributes.SecondarySize;
      RotationRadians = attributes.RotationRadians;
    }

    /// <since>8.0</since>
    public PointStyle? PointStyle { get; set; }
    /// <since>8.0</since>
    public System.Drawing.Color? StrokeColor { get; set; }
    /// <since>8.0</since>
    public System.Drawing.Color? FillColor { get; set; }
    /// <since>8.0</since>
    public float? Diameter { get; set; }
    /// <since>8.0</since>
    public float? StrokeWidth { get; set; }
    /// <since>8.0</since>
    public float? SecondarySize { get; set; }
    /// <since>8.0</since>
    public float? RotationRadians { get; set; } 
  }
  /// <summary>
  /// A 3d point with attributes used by the display pipeline
  /// </summary>
  public class DisplayPoint
  {
    readonly Rhino.Geometry.Point3d _location;
    DisplayPointAttributes _attributes;

    /// <since>8.0</since>
    public DisplayPoint(Rhino.Geometry.Point3d location)
    {
      _location = location;
    }

    /// <since>8.0</since>
    public DisplayPoint WithAttributes(DisplayPointAttributes attributes)
    {
      var rc = new DisplayPoint(_location);
      if (attributes != null)
        rc._attributes = new DisplayPointAttributes(attributes);
      return rc;
    }

    internal DisplayPointAttributes Attributes => _attributes;
    /// <since>8.0</since>
    public Rhino.Geometry.Point3d Location => _location;

    internal RhDisplayPoint ToDisplayPoint(DisplayPointAttributes fallbackAttributes)
    {
      RhDisplayPoint rc = new RhDisplayPoint(_location);

      if (_attributes != null && _attributes.PointStyle.HasValue)
        rc.m_style = UnsafeNativeMethods.RHC_RhinoPointStyleFromPointStyle(_attributes.PointStyle.Value);
      else if (fallbackAttributes!= null && fallbackAttributes.PointStyle.HasValue)
        rc.m_style = UnsafeNativeMethods.RHC_RhinoPointStyleFromPointStyle(fallbackAttributes.PointStyle.Value);

      if (_attributes != null && _attributes.StrokeColor.HasValue)
        rc.m_strokeColor = Rhino.Runtime.Interop.ColorToABGR(_attributes.StrokeColor.Value);
      else if (fallbackAttributes != null && fallbackAttributes.StrokeColor.HasValue)
        rc.m_strokeColor = Rhino.Runtime.Interop.ColorToABGR(fallbackAttributes.StrokeColor.Value);

      if (_attributes != null && _attributes.FillColor.HasValue)
        rc.m_fillColor = Rhino.Runtime.Interop.ColorToABGR(_attributes.FillColor.Value);
      else if (fallbackAttributes != null && fallbackAttributes.FillColor.HasValue)
        rc.m_fillColor = Rhino.Runtime.Interop.ColorToABGR(fallbackAttributes.FillColor.Value);

      if (_attributes != null && _attributes.Diameter.HasValue)
        rc.m_diameterPixels = _attributes.Diameter.Value;
      else if (fallbackAttributes != null && fallbackAttributes.Diameter.HasValue)
        rc.m_diameterPixels = fallbackAttributes.Diameter.Value;

      if (_attributes != null && _attributes.StrokeWidth.HasValue)
        rc.m_strokeWidthPixels = _attributes.StrokeWidth.Value;
      else if (fallbackAttributes != null && fallbackAttributes.StrokeWidth.HasValue)
        rc.m_strokeWidthPixels = fallbackAttributes.StrokeWidth.Value;

      if (_attributes != null && _attributes.SecondarySize.HasValue)
        rc.m_innerDiameterPixels = _attributes.SecondarySize.Value;
      else if (fallbackAttributes != null && fallbackAttributes.SecondarySize.HasValue)
        rc.m_innerDiameterPixels = fallbackAttributes.SecondarySize.Value;

      if (_attributes != null && _attributes.RotationRadians.HasValue)
        rc.m_rotationRadians = _attributes.RotationRadians.Value;
      else if (fallbackAttributes != null && fallbackAttributes.RotationRadians.HasValue)
        rc.m_rotationRadians = fallbackAttributes.RotationRadians.Value;

      if (rc.m_rotationRadians == RhinoMath.UnsetSingle)
        rc.m_rotationRadians = 0;
      return rc;
    }
  }

  public class DisplayPointSet : IDisposable
  {
    DisplayPoint[] _points;

    RhDisplayPoint[] _cachedNativePoints;
    DisplayPointAttributes _cachedFallbackAttributes;
    IntPtr _cacheHandle = IntPtr.Zero;

    /// <since>8.0</since>
    public static DisplayPointSet Create(IEnumerable<DisplayPoint> points)
    {
      if (points == null)
        return null;
      var list = points as List<DisplayPoint>;
      if (list == null)
        list = new List<DisplayPoint>(points);
      if (list.Count < 1)
        return null;
      return new DisplayPointSet(list);
    }

    internal RhDisplayPoint[] RhDisplayPoints(DisplayPointAttributes fallbackAttributes)
    {
      if (_cachedNativePoints != null &&
        DisplayPointAttributes.AreEqual(_cachedFallbackAttributes, fallbackAttributes))
        return _cachedNativePoints;

      DestroyCacheHandle();

      if (_cachedNativePoints==null)
        _cachedNativePoints = new RhDisplayPoint[_points.Length];

      _cachedFallbackAttributes = null;
      if (fallbackAttributes != null)
        _cachedFallbackAttributes = new DisplayPointAttributes(fallbackAttributes);

      for( int i=0; i<_points.Length; i++)
      {
        _cachedNativePoints[i] = _points[i].ToDisplayPoint(fallbackAttributes);
      }
      return _cachedNativePoints;
    }

    private DisplayPointSet(List<DisplayPoint> points)
    {
      GC.SuppressFinalize(this);
      _points = points.ToArray();
    }

    ~DisplayPointSet()
    {
      DestroyCacheHandle();
    }

    /// <since>8.0</since>
    public void Dispose()
    {
      DestroyCacheHandle();
    }

    internal DisplayPoint[] Points()
    {
      return _points;
    }

    internal IntPtr CacheHandle()
    {
      if (IntPtr.Zero == _cacheHandle)
      {
        _cacheHandle = UnsafeNativeMethods.CRhinoCacheHandle_New();
        GC.ReRegisterForFinalize(this);
      }
      return _cacheHandle;
    }

    void DestroyCacheHandle()
    {
      if (_cacheHandle != IntPtr.Zero)
      {
        UnsafeNativeMethods.CRhinoCacheHandle_Delete(_cacheHandle);
        _cacheHandle = IntPtr.Zero;
        GC.SuppressFinalize(this);
      }
    }
  }
}

namespace Rhino.Runtime.InteropWrappers
{
  [StructLayout(LayoutKind.Sequential, Pack = 4, Size = 48)]
  public struct RhDisplayPoint
  {
    /// <since>8.0</since>
    public RhDisplayPoint(Rhino.Geometry.Point3d location)
    {
      m_vertex = new Geometry.Point3f((float)location.X, (float)location.Y, (float)location.Z);
      m__padding1 = 0;
      m_style = 50; // RPS_VARIABLE_DOT
      unchecked
      {
        const uint UNSETCOLOR = 0xffffffff;
        m_strokeColor = (int)UNSETCOLOR;
        m_fillColor = (int)UNSETCOLOR;
      }
      m_diameterPixels = RhinoMath.UnsetSingle;
      m_strokeWidthPixels = RhinoMath.UnsetSingle;
      m_innerDiameterPixels = RhinoMath.UnsetSingle;
      m_rotationRadians = RhinoMath.UnsetSingle;
      m__padding2 = 0;
    }

    public Rhino.Geometry.Point3f m_vertex;
    public float m__padding1; // to match layout used in metal shaders
    public int m_style;
    public int m_strokeColor;
    public int m_fillColor;
    public float m_diameterPixels;
    public float m_strokeWidthPixels;
    public float m_innerDiameterPixels;
    public float m_rotationRadians;
    public float m__padding2; // to match layout used in metal shaders
  }
}

namespace Rhino.Geometry
{
  /// <summary>
  /// Represents a simple particle.
  /// <para>This base class only defines position and display properties (size, color, bitmap id).
  /// You will most likely create a class that derives from this particle class to perform some
  /// sort of physical simulation (movement over time or frames).
  /// </para>
  /// </summary>
  public class Particle
  {
    int m_index = -1;
    Point3d m_location;

    /// <summary>
    /// Initializes a new instance of the <see cref="Particle"/> class.
    /// </summary>
    /// <since>5.0</since>
    public Particle()
    {
      Color = System.Drawing.Color.White;
    }

    /// <summary>
    /// Gets the parent particle system of this particle.
    /// </summary>
    /// <since>5.0</since>
    public ParticleSystem ParentSystem { get; internal set; }

    /// <summary>
    /// Index in ParentSystem for this Particle. Can change when the particle
    /// system is modified.
    /// </summary>
    /// <since>5.0</since>
    public int Index
    {
      get
      {
        if( ParentSystem==null )
          return -1;
        return m_index;
      }
      internal set
      {
        m_index = value;
      }
    }

    /// <summary>3d Location of the Particle.</summary>
    /// <since>5.0</since>
    public Point3d Location
    {
      get { return m_location; }
      set
      {
        if( value != m_location )
        {
          if (ParentSystem != null)
            ParentSystem.UpdateParticleLocation(m_index, value);
          m_location = value;
        }
      }
    }

    /// <since>5.0</since>
    public float Size { get; set; }

    /// <since>5.0</since>
    public System.Drawing.Color Color { get; set; }

    /// <since>5.0</since>
    public int DisplayBitmapIndex { get; set; }

    /// <summary>
    /// Base class implementation does nothing.
    /// </summary>
    /// <since>5.0</since>
    public virtual void Update(){}
  }

  public class ParticleSystem : IEnumerable<Particle>
  {
    readonly List<Particle> m_particles = new List<Particle>();
    int m_empty_slot_count; // = 0 initialized by runtime
    BoundingBox m_bbox = BoundingBox.Unset;

    //cache data used for drawing
    internal Point3d[] m_points = new Point3d[0];
    internal int[] m_colors_argb = new int[0];
    internal float[] m_sizes = new float[0];
    internal int[] m_display_bitmap_ids = new int[0];

    /// <since>5.0</since>
    public bool DrawRequiresDepthSorting { get; set; }
    /// <since>5.0</since>
    public bool DisplaySizesInWorldUnits { get; set; }


    /// <since>5.0</since>
    public BoundingBox BoundingBox
    {
      get
      {
        if (!m_bbox.IsValid)
        {
          foreach (Particle p in this)
            m_bbox.Union(p.Location);
        }
        return m_bbox;
      }
    }

    /// <summary>
    /// Adds a particle to this ParticleSystem. A Particle can only be in one system
    /// at a time.  If the Particle already exists in a different system, this function
    /// will return false. You should remove the particle from the other system first
    /// before adding it.
    /// </summary>
    /// <param name="particle">A particle to be added.</param>
    /// <returns>
    /// true if this particle was added to the system or if is already in the system.
    /// false if the particle already exists in a different system.
    /// </returns>
    /// <since>5.0</since>
    public virtual bool Add(Particle particle)
    {
      ParticleSystem existing_system = particle.ParentSystem;
      if( existing_system==this )
        return true; // already in system

      if (existing_system != null)
        return false;

      particle.Index = -1;
      if (m_empty_slot_count > 0)
      {
        for (int i = 0; i < m_particles.Count; i++)
        {
          if (m_particles[i] == null)
          {
            m_particles[i] = particle;
            particle.Index = i;
            if (m_points.Length == m_particles.Count)
              m_points[i] = particle.Location;
            m_empty_slot_count--;
            break;
          }
        }
      }
      if (particle.Index == -1)
      {
        m_particles.Add(particle);
        particle.Index = m_particles.Count - 1;
      }
      if (m_bbox.IsValid)
        m_bbox.Union(particle.Location);
      return true;
    }

    /// <summary>
    /// Removes a single particle from this system.
    /// </summary>
    /// <param name="particle">The particle to be removed.</param>
    /// <since>5.0</since>
    public virtual void Remove(Particle particle)
    {
      int index = particle.Index;
      if (particle.ParentSystem == this && index >= 0 && index < m_particles.Count)
      {
        var particle_in_list = m_particles[index];
        if (particle == particle_in_list)
        {
          m_particles[index] = null; //don't remove as this will mess up other particle slots
          particle.Index = -1;
          particle.ParentSystem = null;
          m_empty_slot_count++;
          if( m_bbox.IsValid && !m_bbox.Contains(particle.Location, true) )
            m_bbox = BoundingBox.Unset;
        }
      }
    }

    /// <summary>
    /// Remove all Particles from this system.
    /// </summary>
    /// <since>5.0</since>
    public virtual void Clear()
    {
      for (int i = 0; i < m_particles.Count; i++)
      {
        var particle = m_particles[i];
        if (particle != null)
        {
          particle.Index = -1;
          particle.ParentSystem = null;
        }
        m_particles.Clear();
        m_empty_slot_count = 0;
      }
      m_bbox = BoundingBox.Unset;
    }

    /// <summary>
    /// Calls Update on every particle in the system.
    /// </summary>
    /// <since>5.0</since>
    public virtual void Update()
    {
      foreach (var particle in this)
      {
        if (particle == null)
          continue;
        particle.Update();
      }
    }

    internal void UpdateDrawCache()
    {
      if (m_points.Length != m_particles.Count || m_empty_slot_count > 0)
      {
        if (m_empty_slot_count > 0)
        {
          int count = m_particles.Count;
          for (int i = count-1; i>=0; i--)
          {
            if (m_particles[i] == null)
              m_particles.RemoveAt(i);
          }
          m_empty_slot_count = 0;
        }
        m_points = new Point3d[m_particles.Count];
        m_colors_argb = new int[m_particles.Count];
        m_sizes = new float[m_particles.Count];
        m_display_bitmap_ids = new int[m_particles.Count];
        for (int i = 0; i < m_particles.Count; i++)
        {
          Particle p = m_particles[i];
          m_points[i] = p.Location;
          m_colors_argb[i] = p.Color.ToArgb();
          m_sizes[i] = p.Size;
          m_display_bitmap_ids[i] = p.DisplayBitmapIndex;
        }
      }
    }

    internal void UpdateParticleLocation(int index, Point3d newLocation)
    {
      if (m_points.Length == m_particles.Count && index >= 0 && index < m_points.Length)
        m_points[index] = newLocation;
      ClearDepthSortCache();
      if (m_bbox.IsValid)
      {
        var particle = m_particles[index];
        if (particle != null)
        {
          if (m_bbox.Contains(particle.Location, true))
            m_bbox.Union(newLocation);
          else
            m_bbox = BoundingBox.Unset;
        }
      }
    }

    void ClearDepthSortCache()
    {
    }

    #region enumerable support
    /// <since>5.0</since>
    public IEnumerator<Particle> GetEnumerator()
    {
      for( int i=0; i<m_particles.Count; i++ )
      {
        Particle p = m_particles[i];
        if (p != null)
          yield return p;
      }
    }

    /// <since>5.0</since>
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
    {
      return GetEnumerator();
    }
    #endregion
  }
}
#endif
