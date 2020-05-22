using System;

namespace Rhino.Runtime
{
  /// <summary>
  /// Contains static methods to marshal objects between RhinoCommon and legacy Rhino_DotNet or C++.
  /// </summary>
  public static class Interop
  {
    //eventually this could go away if we just do all of the abgr->argb conversions in C++
    internal static System.Drawing.Color ColorFromWin32(int abgr)
    {
#if MONO_BUILD || DOTNETCORE
      return System.Drawing.Color.FromArgb(0xFF, (abgr & 0xFF), ((abgr >> 8) & 0xFF), ((abgr >> 16) & 0xFF));
#else
      return System.Drawing.ColorTranslator.FromWin32(abgr);
#endif
    }

#if RHINO_SDK
    /// <summary>
    /// Create managed Font from native ON_Font*
    /// </summary>
    /// <param name="ptrManagedFont"></param>
    /// <returns></returns>
    /// <since>6.0</since>
    public static DocObjects.Font FontFromPointer(IntPtr ptrManagedFont)
    {
      if (IntPtr.Zero == ptrManagedFont)
        return null;
      return new DocObjects.Font(ptrManagedFont);
    }

    /// <summary>
    /// Create a ViewCaptureSettings class from a native const CRhinoPrintInfo*
    /// The pointer values are copied
    /// </summary>
    /// <param name="ptrViewCapture"></param>
    /// <returns></returns>
    /// <since>6.0</since>
    public static Display.ViewCaptureSettings ViewCaptureFromPointer(IntPtr ptrViewCapture)
    {
      if (IntPtr.Zero == ptrViewCapture)
        return null;
      return new Display.ViewCaptureSettings(ptrViewCapture);
    }

    /// <summary>
    /// Gets the C++ CRhinoDoc* for a given RhinoCommon RhinoDoc class.
    /// </summary>
    /// <param name="doc">A document.</param>
    /// <returns>A pointer value.</returns>
    /// <since>5.0</since>
    public static IntPtr NativeRhinoDocPointer(RhinoDoc doc)
    {
      if (doc == null)
        return IntPtr.Zero;
      return UnsafeNativeMethods.CRhinoDoc_GetFromId(doc.RuntimeSerialNumber);
    }

    /// <summary>
    /// Get native NSFont* from a Rhino Font. Only works on Mac
    /// </summary>
    /// <returns>NSFont* on success. IntPtr.Zero on failure</returns>
    /// <param name="font"></param>
    /// <since>6.9</since>
    public static IntPtr NSFontFromFont(Rhino.DocObjects.Font font)
    {
#if MONO_BUILD
      if (Rhino.Runtime.HostUtils.RunningOnOSX)
      {
        IntPtr constPtrFont = font.ConstPointer();
        IntPtr nsfont = UnsafeNativeMethods.ON_Font_GetAppleFont(constPtrFont);
        return nsfont;
      }
#endif
      return IntPtr.Zero;
    }

    /// <summary>
    /// Get native NSFont* from a Rhino Font. Only works on Mac
    /// </summary>
    /// <returns>NSFont* on success. IntPtr.Zero on failure</returns>
    /// <param name="font"></param>
    /// <param name="pointSize">Point size</param>
    /// <since>6.9</since>
    public static IntPtr NSFontFromFont(Rhino.DocObjects.Font font, double pointSize)
    {
#if MONO_BUILD
      if (Rhino.Runtime.HostUtils.RunningOnOSX)
      {
        IntPtr constPtrFont = font.ConstPointer();
        IntPtr nsfont = UnsafeNativeMethods.ON_Font_GetAppleFont2(constPtrFont, pointSize);
        return nsfont;
      }
#endif
      return IntPtr.Zero;
    }

#endif

    /// <summary>
    /// Returns the underlying const ON_Geometry* for a RhinoCommon class. You should only
    /// be interested in using this function if you are writing C++ code.
    /// </summary>
    /// <param name="geometry">A geometry object. This can be null and in such a case <see cref="IntPtr.Zero"/> is returned.</param>
    /// <returns>A pointer to the const geometry.</returns>
    /// <since>5.0</since>
    public static IntPtr NativeGeometryConstPointer(Geometry.GeometryBase geometry)
    {
      IntPtr rc = IntPtr.Zero;
      if (geometry != null)
        rc = geometry.ConstPointer();
      return rc;
    }

    /// <summary>
    /// Returns the underlying non-const ON_Geometry* for a RhinoCommon class. You should
    /// only be interested in using this function if you are writing C++ code.
    /// </summary>
    /// <param name="geometry">A geometry object. This can be null and in such a case <see cref="IntPtr.Zero"/> is returned.</param>
    /// <returns>A pointer to the non-const geometry.</returns>
    /// <since>5.0</since>
    public static IntPtr NativeGeometryNonConstPointer(Geometry.GeometryBase geometry)
    {
      IntPtr rc = IntPtr.Zero;
      if (geometry != null)
        rc = geometry.NonConstPointer();
      return rc;
    }

    /// <summary>You must call Disable on the reporter output if it's not null when you are done with it. It will NOT clean itself.
    /// You should call Dispose on the terminator if it's not null, because that will keep it alive for the time of the computation.</summary>
    internal static void MarshalProgressAndCancelToken(System.Threading.CancellationToken cancel, IProgress<double> progress,
      out IntPtr ptrTerminator, out int progressInt, out ProgressReporter reporter, out ThreadTerminator terminator)
    {
      reporter = null;
      progressInt = 0;
      if (progress != null)
      {
        reporter = new ProgressReporter(progress);
        progressInt = reporter.SerialNumber;
        reporter.Enable();
      }

      terminator = null;
      if (cancel != System.Threading.CancellationToken.None)
      {
        terminator = new ThreadTerminator();
        cancel.Register(terminator.RequestCancel);
      }
      ptrTerminator = terminator == null ? IntPtr.Zero : terminator.NonConstPointer();
    }

#if RHINO_SDK
    /// <summary>
    /// Get a CRhinoPrintInfo* for a given ViewCaptureSettings class
    /// </summary>
    /// <param name="settings"></param>
    /// <returns></returns>
    /// <since>6.0</since>
    public static IntPtr NativeNonConstPointer(Display.ViewCaptureSettings settings)
    {
      return settings.NonConstPointer();
    }

    /// <summary>
    /// Get ON_Viewport* from a ViewportInfo instance
    /// </summary>
    /// <param name="viewport"></param>
    /// <returns></returns>
    /// <since>5.1</since>
    public static IntPtr NativeNonConstPointer(DocObjects.ViewportInfo viewport)
    {
      return viewport.NonConstPointer();
    }

    /// <summary>
    /// Get CRhinoViewport* from a RhinoViewport instance
    /// </summary>
    /// <param name="viewport"></param>
    /// <returns></returns>
    /// <since>6.0</since>
    public static IntPtr NativeNonConstPointer(Display.RhinoViewport viewport)
    {
      return viewport.NonConstPointer();
    }

    /// <summary>
    /// Get CRhinoDisplayPipeline* for a DisplayPipeline instance
    /// </summary>
    /// <param name="pipeline"></param>
    /// <returns></returns>
    /// <since>6.0</since>
    public static IntPtr NativeNonConstPointer(Display.DisplayPipeline pipeline)
    {
      return pipeline.NonConstPointer();
    }


    /// <summary>
    /// Get CRhinoGetPoint* from a GetPoint instance
    /// </summary>
    /// <param name="getPoint"></param>
    /// <returns></returns>
    /// <since>6.0</since>
    public static IntPtr NativeNonConstPointer(Input.Custom.GetPoint getPoint)
    {
      return getPoint.NonConstPointer();
    }

    /// <summary>
    /// Returns the underlying const CRhinoObject* for a RhinoCommon class. You should only
    /// be interested in using this function if you are writing C++ code.
    /// </summary>
    /// <param name="rhinoObject">A Rhino object.</param>
    /// <returns>A pointer to the Rhino const object.</returns>
    /// <since>5.0</since>
    public static IntPtr RhinoObjectConstPointer(DocObjects.RhinoObject rhinoObject)
    {
      IntPtr rc = IntPtr.Zero;
      if (rhinoObject != null)
        rc = rhinoObject.ConstPointer();
      return rc;
    }

    /// <summary>
    /// Constructs a RhinoCommon Rhino object from an unmanaged C++ RhinoObject pointer.
    /// </summary>
    /// <param name="pRhinoObject">The original pointer.</param>
    /// <returns>A new Rhino object, or null if the pointer was invalid or <see cref="IntPtr.Zero"/>.</returns>
    /// <since>5.0</since>
    public static DocObjects.RhinoObject RhinoObjectFromPointer(IntPtr pRhinoObject)
    {
      return DocObjects.RhinoObject.CreateRhinoObjectHelper(pRhinoObject);
    }

    /// <summary>
    /// Returns the underlying const CRhinoFileWriteOptions* for a Rhino.FileIO.FileWriteOptions object. 
    /// You should only be interested in using this function if you are writing C++ code.
    /// </summary>
    /// <param name="options">A FileWriteOptions object.</param>
    /// <returns>A pointer to the Rhino const object.</returns>
    /// <since>6.0</since>
    public static IntPtr FileWriteOptionsConstPointer(FileIO.FileWriteOptions options)
    {
      IntPtr rc = IntPtr.Zero;
      if (options != null)
        rc = options.ConstPointer();
      return rc;
    }

    /// <summary>
    /// Returns the underlying const CRhinoFileReadOptions* for a Rhino.FileIO.FileReadOptions object.
    /// You should only be interested in using this function if you are writing C++ code.
    /// </summary>
    /// <param name="options">A FileReadOptions object.</param>
    /// <returns>A pointer to the Rhino const object.</returns>
    /// <since>6.0</since>
    public static IntPtr FileReadOptionsConstPointer(FileIO.FileReadOptions options)
    {
      IntPtr rc = IntPtr.Zero;
      if (options != null)
        rc = options.ConstPointer();
      return rc;
    }

#endif

    /// <summary>
    /// Constructs a RhinoCommon Geometry class from a given ON_Geomety*. The ON_Geometry*
    /// must be declared on the heap and its lifetime becomes controlled by RhinoCommon.
    /// </summary>
    /// <param name="pGeometry">ON_Geometry*</param>
    /// <returns>The appropriate geometry class in RhinoCommon on success.</returns>
    /// <since>5.0</since>
    public static Geometry.GeometryBase CreateFromNativePointer(IntPtr pGeometry)
    {
      return Geometry.GeometryBase.CreateGeometryHelper(pGeometry, null);
    }

    /// <summary>
    /// Attempts to copy the contents of a RMA.OpenNURBS.OnArc to a Rhino.Geometry.Arc.
    /// </summary>
    /// <param name="source">A source OnArc.</param>
    /// <param name="destination">A destination arc.</param>
    /// <returns>true if the operation succeeded; false otherwise.</returns>
    /// <since>5.0</since>
    public static bool TryCopyFromOnArc(object source, out Geometry.Arc destination)
    {
      destination = new Geometry.Arc();
      bool rc = false;
      IntPtr ptr = GetInternalPointer(source, "RMA.OpenNURBS.OnArc");
      if (IntPtr.Zero != ptr)
      {
        UnsafeNativeMethods.ON_Arc_Copy(ptr, ref destination, true);
        rc = true;
      }
      return rc;
    }

    /// <summary>
    /// Attempts to copy the contents of a Rhino.Geometry.Arc to a RMA.OpenNURBS.OnArc.
    /// </summary>
    /// <param name="source">A source arc.</param>
    /// <param name="destination">A destination OnArc.</param>
    /// <returns>true if the operation succeeded; false otherwise.</returns>
    /// <since>5.0</since>
    public static bool TryCopyToOnArc(Geometry.Arc source, object destination)
    {
      bool rc = false;
      IntPtr ptr = GetInternalPointer(destination, "RMA.OpenNURBS.OnArc");
      if (IntPtr.Zero != ptr)
      {
        UnsafeNativeMethods.ON_Arc_Copy(ptr, ref source, false);
        rc = true;
      }
      return rc;
    }

    static IntPtr GetInternalPointer(object rhinoDotNetObject, string name)
    {
      IntPtr rc = IntPtr.Zero;
      if (null != rhinoDotNetObject)
      {
        Type t = rhinoDotNetObject.GetType();
        if (t.FullName == name)
        {
          System.Reflection.PropertyInfo pi = t.GetProperty("InternalPointer");
          rc = (IntPtr)pi.GetValue(rhinoDotNetObject, null);
        }
      }
      return rc;
    }

    static Geometry.GeometryBase CopyHelper(object source, string name)
    {
      IntPtr ptr_existing_geometry = GetInternalPointer(source, name);
      IntPtr ptr_new_geometry = UnsafeNativeMethods.ON_Object_Duplicate(ptr_existing_geometry);
      return Geometry.GeometryBase.CreateGeometryHelper(ptr_new_geometry, null);
    }

    /// <summary>
    /// Copies a Rhino_DotNet brep to a RhinoCommon brep class.
    /// </summary>
    /// <param name="source">
    /// RMA.OpenNURBS.IOnBrep or RMA.OpenNURBS.OnBrep.
    /// </param>
    /// <returns>
    /// RhinoCommon object on success. This will be an independent copy.
    /// </returns>
    /// <since>5.0</since>
    public static Geometry.Brep FromOnBrep(object source)
    {
      Geometry.GeometryBase g = CopyHelper(source, "RMA.OpenNURBS.OnBrep");
      var rc = g as Geometry.Brep;
      return rc;
    }

    /// <summary>
    /// Copies a Rhino_DotNet surface to a RhinoCommon Surface class.
    /// </summary>
    /// <param name="source">
    /// Any of the following in the RMA.OpenNURBS namespace are acceptable.
    /// IOnSurface, OnSurface, IOnPlaneSurface, OnPlaneSurface, IOnClippingPlaneSurface,
    /// OnClippingPlaneSurface, IOnNurbsSurface, OnNurbsSurfac, IOnRevSurface, OnRevSurface,
    /// IOnSumSurface, OnSumSurface.
    /// </param>
    /// <returns>
    /// RhinoCommon object on success. This will be an independent copy.
    /// </returns>
    /// <since>5.0</since>
    public static Geometry.Surface FromOnSurface(object source)
    {
      Geometry.GeometryBase g = CopyHelper(source, "RMA.OpenNURBS.OnSurface");
      if (null == g)
        g = CopyHelper(source, "RMA.OpenNURBS.OnPlaneSurface");
      if (null == g)
        g = CopyHelper(source, "RMA.OpenNURBS.OnClippingPlaneSurface");
      if (null == g)
        g = CopyHelper(source, "RMA.OpenNURBS.OnNurbsSurface");
      if (null == g)
        g = CopyHelper(source, "RMA.OpenNURBS.OnRevSurface");
      if (null == g)
        g = CopyHelper(source, "RMA.OpenNURBS.OnSumSurface");
      var rc = g as Geometry.Surface;
      return rc;
    }

    /// <summary>
    /// Copies a Rhino_DotNet mesh to a RhinoCommon mesh class.
    /// </summary>
    /// <param name="source">
    /// RMA.OpenNURBS.IOnMesh or RMA.OpenNURBS.OnMesh.
    /// </param>
    /// <returns>
    /// RhinoCommon object on success. This will be an independent copy.
    /// </returns>
    /// <since>5.0</since>
    public static Geometry.Mesh FromOnMesh(object source)
    {
      Geometry.GeometryBase g = CopyHelper(source, "RMA.OpenNURBS.OnMesh");
      var rc = g as Geometry.Mesh;
      return rc;
    }

    /// <summary>
    /// Copies a Rhino_DotNet curve to a RhinoCommon curve class.
    /// </summary>
    /// <param name="source">
    /// RMA.OpenNURBS.IOnCurve or RMA.OpenNURBS.OnCurve.
    /// </param>
    /// <returns>
    /// RhinoCommon object on success. This will be an independent copy.
    /// </returns>
    /// <since>5.0</since>
    public static Geometry.Curve FromOnCurve(object source)
    {
      Geometry.GeometryBase g = CopyHelper(source, "RMA.OpenNURBS.OnCurve");
      if (null == g)
        g = CopyHelper(source, "RMA.OpenNURBS.OnArcCurve");
      if (null == g)
        g = CopyHelper(source, "RMA.OpenNURBS.OnLineCurve");
      if (null == g)
        g = CopyHelper(source, "RMA.OpenNURBS.OnNurbsCurve");
      if (null == g)
        g = CopyHelper(source, "RMA.OpenNURBS.OnPolylineCurve");
      if (null == g)
        g = CopyHelper(source, "RMA.OpenNURBS.OnPolyCurve");
      var rc = g as Geometry.Curve;
      return rc;
    }

    static Type GetRhinoDotNetType(string name)
    {
      System.Reflection.Assembly rhino_dot_net = HostUtils.GetRhinoDotNetAssembly();
      if (null == rhino_dot_net)
        return null;
      return rhino_dot_net.GetType(name);
    }

    /// <summary>
    /// Constructs a Rhino_DotNet OnBrep that is a copy of a given brep.
    /// </summary>
    /// <param name="source">A source brep.</param>
    /// <returns>
    /// Rhino_DotNet object on success. This will be an independent copy.
    /// </returns>
    /// <since>5.0</since>
    public static object ToOnBrep(Geometry.Brep source)
    {
      object rc = null;
      IntPtr const_ptr_source = source.ConstPointer();
      Type on_brep_type = GetRhinoDotNetType("RMA.OpenNURBS.OnBrep");
      if (IntPtr.Zero != const_ptr_source && null != on_brep_type)
      {
        System.Reflection.MethodInfo mi = on_brep_type.GetMethod("WrapNativePointer", new[] { typeof(IntPtr), typeof(bool), typeof(bool) });
        IntPtr ptr_new_brep = UnsafeNativeMethods.ON_Brep_New(const_ptr_source);
        rc = mi.Invoke(null, new object[] { ptr_new_brep, false, true });
      }
      return rc;
    }

    /// <summary>
    /// Constructs a Rhino_DotNet OnSurface that is a copy of a given curve.
    /// </summary>
    /// <param name="source">A source brep.</param>
    /// <returns>
    /// Rhino_DotNet object on success. This will be an independent copy.
    /// </returns>
    /// <since>5.0</since>
    public static object ToOnSurface(Geometry.Surface source)
    {
      object rc = null;
      IntPtr const_ptr_source = source.ConstPointer();
      Type on_type = GetRhinoDotNetType("RMA.OpenNURBS.OnSurface");
      if (IntPtr.Zero != const_ptr_source && null != on_type)
      {
        System.Reflection.MethodInfo mi = on_type.GetMethod("WrapNativePointer", new[] { typeof(IntPtr), typeof(bool), typeof(bool) });
        IntPtr ptr_new_surface = UnsafeNativeMethods.ON_Surface_DuplicateSurface(const_ptr_source);
        rc = mi.Invoke(null, new object[] { ptr_new_surface, false, true });
      }
      return rc;
    }

    /// <summary>
    /// Constructs a Rhino_DotNet OnMesh that is a copy of a given mesh.
    /// </summary>
    /// <param name="source">A source brep.</param>
    /// <returns>
    /// Rhino_DotNet object on success. This will be an independent copy.
    /// </returns>
    /// <since>5.0</since>
    public static object ToOnMesh(Geometry.Mesh source)
    {
      object rc = null;
      IntPtr const_ptr_source = source.ConstPointer();
      Type on_type = GetRhinoDotNetType("RMA.OpenNURBS.OnMesh");
      if (IntPtr.Zero != const_ptr_source && null != on_type)
      {
        System.Reflection.MethodInfo mi = on_type.GetMethod("WrapNativePointer", new[] { typeof(IntPtr), typeof(bool), typeof(bool) });
        IntPtr ptr_new_mesh = UnsafeNativeMethods.ON_Mesh_New(const_ptr_source);
        rc = mi.Invoke(null, new object[] { ptr_new_mesh, false, true });
      }
      return rc;
    }

    /// <summary>
    /// Constructs a Rhino_DotNet OnCurve that is a copy of a given curve.
    /// </summary>
    /// <param name="source">A RhinoCommon source curve.</param>
    /// <returns>
    /// Rhino_DotNet object on success. This will be an independent copy.
    /// </returns>
    /// <since>5.0</since>
    public static object ToOnCurve(Geometry.Curve source)
    {
      object rc = null;
      IntPtr const_ptr_source = source.ConstPointer();
      Type on_type = GetRhinoDotNetType("RMA.OpenNURBS.OnCurve");
      if (IntPtr.Zero != const_ptr_source && null != on_type)
      {
        System.Reflection.MethodInfo mi = on_type.GetMethod("WrapNativePointer", new[] { typeof(IntPtr), typeof(bool), typeof(bool) });
        IntPtr ptr_new_curve = UnsafeNativeMethods.ON_Curve_DuplicateCurve(const_ptr_source);
        rc = mi.Invoke(null, new object[] { ptr_new_curve, false, true });
      }
      return rc;
    }

    /// <summary>
    /// Constructs a Rhino_DotNet OnXform from a given RhinoCommon Transform.
    /// </summary>
    /// <param name="source">A RhinoCommon source transform.</param>
    /// <returns>
    /// Rhino_DotNet object on success. This will be an independent copy.
    /// </returns>
    /// <since>5.0</since>
    public static object ToOnXform(Geometry.Transform source)
    {
      object rc = null;
      Type on_type = GetRhinoDotNetType("RMA.OpenNURBS.OnXform");
      if (null != on_type)
      {
        var vals = new double[16];
        for( int row=0; row<4; row++ )
        {
          for (int column = 0; column < 4; column++)
          {
            vals[4 * row + column] = source[row, column];
          }
        }
        rc = Activator.CreateInstance(on_type, vals);
      }
      return rc;
    }

#if RHINO_SDK
    /// <summary>
    /// Convert a Rhino.Display.Viewport to an RMA.Rhino.IRhinoViewport.
    /// </summary>
    /// <param name="source">A RhinoCommon viewport.</param>
    /// <returns>
    /// Rhino_DotNet IRhinoViewport object on success. This will be an independent copy.
    /// </returns>
    /// <since>5.0</since>
    public static object ToIRhinoViewport(Display.RhinoViewport source)
    {
      object rc = null;
      IntPtr const_ptr_source = source.ConstPointer();
      Type rh_type = GetRhinoDotNetType("RMA.Rhino.MRhinoViewport");
      if (IntPtr.Zero != const_ptr_source && null != rh_type)
      {
        System.Reflection.MethodInfo mi = rh_type.GetMethod("WrapNativePointer", new[] { typeof(IntPtr), typeof(bool), typeof(bool) });
        const bool is_const = true;
        const bool do_delete = false;
        rc = mi.Invoke(null, new object[] { const_ptr_source, is_const, do_delete });
      }
      return rc;
    }
#endif
    /*
        public static Rhino.Geometry.Curve TryCopyFromOnCurve(object source)
        {
          if (source != null)
          {
            try
            {
              Type base_type = Type.GetType("RMA.OpenNURBS.OnCurve");
              System.Type t = source.GetType();
              if (t.IsAssignableFrom(base_type))
              {
                System.Reflection.PropertyInfo pi = t.GetProperty("InternalPointer");
                IntPtr ptr = (IntPtr)pi.GetValue(source, null);
                Rhino.Geometry.Curve crv = Rhino.Geometry.Curve.CreateCurveHelper(ptr, null);
                crv.NonConstPointer();
                return crv;
              }
            }
            catch (Exception)
            {
            }
          }
          return null;
        }

        /// <summary>
        /// Do not hold on to the returned class outside the scope of your current function.
        /// </summary>
        /// <param name="source">-</param>
        /// <returns>-</returns>
        public static Rhino.Display.DisplayPipeline ConvertFromMRhinoDisplayPipeline(object source)
        {
          if (source != null)
          {
            try
            {
              Type base_type = Type.GetType("RMA.Rhino.MRhinoDisplayPipeline");
              System.Type t = source.GetType();
              if (t.IsAssignableFrom(base_type))
              {
                System.Reflection.PropertyInfo pi = t.GetProperty("InternalPointer");
                IntPtr ptr = (IntPtr)pi.GetValue(source, null);
                return new Rhino.Display.DisplayPipeline(ptr);
              }
            }
            catch (Exception)
            {
            }
          }
          return null;
        }
        */
#if RHINO_SDK
    /// <summary>
    /// Gets a C++ plug-in pointer for a given RhinoCommon plug-in.
    /// <para>This is a Rhino SDK function.</para>
    /// </summary>
    /// <param name="plugin">A plug-in.</param>
    /// <returns>A pointer.</returns>
    /// <since>5.0</since>
    public static IntPtr PlugInPointer(PlugIns.PlugIn plugin)
    {
      return null == plugin ? IntPtr.Zero : plugin.NonConstPointer();
    }
#endif
  }
}

namespace Rhino.Runtime.InteropWrappers
{
#if RHINO_SDK
  class RhinoDib : IDisposable
  {
    IntPtr m_ptr; //CRhinoDib*

    public static RhinoDib FromBitmap(System.Drawing.Bitmap bitmap)
    {
      IntPtr hbmp = bitmap.GetHbitmap();
      IntPtr ptr_dib = UnsafeNativeMethods.CRhinoDib_FromHBitmap(hbmp);
      return ptr_dib == IntPtr.Zero ? null : new RhinoDib(ptr_dib, true);
    }

    /// <summary>
    /// Convert unmanaged CRhinoDib pointer to a System.Drawing bitmap.
    /// </summary>
    /// <returns>
    /// If the pointer to the CRhinoDib is null then null is returned otherwise;
    /// if the CRhinoDib is valid then the DIB is converted to a System.Drawing.Bitmap
    /// otherwise null is returned. 
    /// </returns>
    /// <param name="rhinoDibPointer">
    /// Unmanaged CRhinoDib pointer
    /// </param>
    /// <param name="deletePointer">
    /// If set to <c>true</c> delete the unmanaged pointer otherwise;
    /// don't delete the pointer.
    /// </param>
    public static System.Drawing.Bitmap ToBitmap(IntPtr rhinoDibPointer, bool deletePointer)
    {
      using(var dib = Attach(rhinoDibPointer, deletePointer))
      {
        var bitmap = dib == null ? null : dib.ToBitmap();
        return bitmap;
      }
    }

    /// <summary>
    /// Create a new RhinoDib and attach it to the specified CRhinoDib pointer.
    /// </summary>
    /// <param name="rhinoDibPointer">
    /// Unmanaged CRhinoDib pointer to attach to.
    /// </param>
    /// <param name="deletePointer">
    /// If set to <c>true</c> delete the unmanaged pointer when the returned RhinoDib
    /// is disposed of otherwise; don't delete the unmanaged pointer.
    /// </param>
    public static RhinoDib Attach(IntPtr rhinoDibPointer, bool deletePointer)
    {
      if (rhinoDibPointer == IntPtr.Zero)
        return null;
      var value = new RhinoDib(rhinoDibPointer, deletePointer);
      return value;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Rhino.Runtime.InteropWrappers.RhinoDib"/>
    /// class and attach it to a unmanaged CRhinoDib pointer.
    /// </summary>
    /// <param name="rhinoDibPointer">
    /// Unmanaged CRhinoDib pointer to attach to.
    /// </param>
    /// <param name="autoDelete">
    /// If set to <c>true</c> delete the unmanaged pointer when this object
    /// is disposed of otherwise; don't delete the unmanaged pointer.
    /// </param>
    private RhinoDib(IntPtr rhinoDibPointer, bool autoDelete)
    {
      m_ptr = rhinoDibPointer;
      m_auto_delete = autoDelete;
    }

    /// <summary>
    /// IF true then Dispose(bool) will delete m_pointer otherwise it will
    /// just set it to IntPtr.Zero.
    /// </summary>
    private readonly bool m_auto_delete;

    /// <summary>
    /// Initializes a new empty unmanaged DIB.
    /// </summary>
    public RhinoDib()
    {
      m_ptr = UnsafeNativeMethods.CRhinoDib_New();
      m_auto_delete = true;
    }

    /// <summary>
    /// Gets the const pointer (const CRhinoDib*).
    /// </summary>
    /// <returns>The const pointer.</returns>
    public IntPtr ConstPointer => m_ptr;

    /// <summary>
    /// Gets the non-const pointer (CRhinoDib*).
    /// </summary>
    /// <returns>The non-const pointer.</returns>
    public IntPtr NonConstPointer => m_ptr;

    /// <summary>
    /// Passively reclaims unmanaged resources when the class user did not explicitly call Dispose().
    /// </summary>
    ~RhinoDib()
    {
      Dispose(false);
    }

    /// <summary>
    /// Actively reclaims unmanaged resources that this instance uses.
    /// </summary>
    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    void Dispose(bool disposing)
    {
      if (IntPtr.Zero != m_ptr)
      {
        if (m_auto_delete)
          UnsafeNativeMethods.CRhinoDib_Delete(m_ptr);
        m_ptr = IntPtr.Zero;
      }
    }

    /// <summary>
    /// Create System.Drawing.Bitmap from DIB
    /// </summary>
    /// <returns>The bitmap.</returns>
    public System.Drawing.Bitmap ToBitmap()
    {
      IntPtr const_ptr_this = ConstPointer;
      IntPtr native_bitmap = UnsafeNativeMethods.CRhinoDib_Bitmap (const_ptr_this);

      if (native_bitmap == IntPtr.Zero)
        return null;

      // If NOT running in Windows or the color depth is NOT 32bit
      if (!HostUtils.RunningOnWindows || UnsafeNativeMethods.CRhinoDib_ColorDepth(const_ptr_this) != 32)
        return System.Drawing.Image.FromHbitmap(native_bitmap);

      // System.Drawing.Image.FromHbitmap doesn't work with DIBs that have
      // transparency so make a System.Drawing.Bitmap and manually copy the
      // source bits.
      int width = 0, height = 0;
      UnsafeNativeMethods.CRhinoDib_Size(const_ptr_this, ref width, ref height);
      var rc = new System.Drawing.Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
      // Get the raw bits from the new System.Drawing.Bitmap
      var rect = System.Drawing.Rectangle.FromLTRB(0, 0, width, height);
      var data = rc.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadWrite, rc.PixelFormat);
      unsafe
      {
        // void* Bit array
        var bits = data.Scan0.ToPointer();
        var pointer = new IntPtr(bits);
        // Call unmanaged code to copy the bits
        UnsafeNativeMethods.CRhinoDib_CopyToBitSystemBitmap(const_ptr_this, pointer, data.Stride, 4);
      }
      rc.UnlockBits(data);
      return rc;
    }
  }
#endif

  /// <summary>
  /// Maintains a pointer created with the C++ new keyword
  /// and takes care to delete it even if the developers forgets or cannot call
  /// Dispose(). This class must be inherited and a meaningful constructor
  /// should set UnsafePointer. Only the first call of Dispose() triggers
  /// ReleaseUnsafePointer().
  /// </summary>
  internal abstract class IntPtrSafeHandle : IDisposable
  {
    bool m_deleted;
    public virtual IntPtr UnsafePointer { get; protected set; }

    public void Dispose()
    {
      GC.SuppressFinalize(this);
      Dispose(true);
    }

    private void Dispose(bool disposing)
    {
      if (!m_deleted) ReleaseUnsafePointer();
      m_deleted = true;
    }

    protected abstract void ReleaseUnsafePointer();

    ~IntPtrSafeHandle()
    {
      Dispose(false);
    }

    public static implicit operator IntPtr(IntPtrSafeHandle handle)
    {
      return handle.UnsafePointer;
    }

    /// <summary>
    /// Creates on object responsible of creating and disposing an unsafe pointer.
    /// </summary>
    /// <param name="creator">The creation method.</param>
    /// <param name="disposer">The disposing method.</param>
    /// <returns></returns>
    public static IntPtrSafeHandle CreateFromMethods(Func<IntPtr> creator, Action<IntPtr> disposer)
    {
      if (creator == null) throw new ArgumentNullException(nameof(creator));
      if (disposer == null) throw new ArgumentNullException(nameof(disposer));

      var rc = new IntPtrSafeHandleImplementer(disposer);
      rc.UnsafePointer = creator();
      return rc;
    }

    private class IntPtrSafeHandleImplementer : IntPtrSafeHandle
    {
      Action<IntPtr> m_disposer;

      public IntPtrSafeHandleImplementer(Action<IntPtr> disposer)
      {
        m_disposer = disposer;
      }

      protected override void ReleaseUnsafePointer()
      {
        m_disposer(UnsafePointer);
      }
    }
  }
}
