using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Rhino.Runtime.InteropWrappers;

#if RHINO_SDK
namespace Rhino.Geometry
{
  /// <summary>
  /// New interactive FilletSrf
  /// </summary>
  public class SurfaceFilletBase : IDisposable
  {
    internal IntPtr m_ptr; // This class is never const
    internal IntPtr ConstPointer() { return m_ptr; }
    internal IntPtr NonConstPointer() { return m_ptr; }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="FaceA"></param>
    /// <param name="uvA"></param>
    /// <param name="FaceB"></param>
    /// <param name="uvB"></param>
    /// <param name="radius"></param>
    /// <param name="tolerance"></param>
    protected SurfaceFilletBase(BrepFace FaceA, Point2d uvA, BrepFace FaceB, Point2d uvB,
      double radius, double tolerance)
    {
      m_ptr = UnsafeNativeMethods.CRhinoSurfaceFillet_New(FaceA.ConstPointer(), ref uvA, FaceB.ConstPointer(), ref uvB, radius, tolerance);
      // I know that throwing an exception is frowned upon in a constructor, but after talking with Steve Baer, the decision was:
      //     "this case is really never going to happen."
      if (null == m_ptr) throw new NullReferenceException("Could not instantiate a new SurfaceFillet.");
    }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="FaceA"></param>
    /// <param name="uvA"></param>
    /// <param name="FaceB"></param>
    /// <param name="uvB"></param>
    /// <param name="radius"></param>
    /// <param name="allowExtend"></param>
    /// <param name="tolerance"></param>
    protected SurfaceFilletBase(BrepFace FaceA, Point2d uvA, BrepFace FaceB, Point2d uvB,
      double radius, bool allowExtend, double tolerance)
    {
      m_ptr = UnsafeNativeMethods.CRhinoSurfaceFillet_New2(FaceA.ConstPointer(), ref uvA, FaceB.ConstPointer(), ref uvB, radius, 
        allowExtend, tolerance);
      // I know that throwing an exception is frowned upon in a constructor, but after talking with Steve Baer, the decision was:
      //     "this case is really never going to happen."
      if (null == m_ptr) throw new NullReferenceException("Could not instantiate a new SurfaceFillet.");
    }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="RailFace"></param>
    /// <param name="Rail3d"></param>
    /// <param name="Face"></param>
    /// <param name="uv"></param>
    /// <param name="tolerance"></param>
    /// <exception cref="NullReferenceException"></exception>
    protected SurfaceFilletBase(BrepFace RailFace, Curve Rail3d, BrepFace Face, Point2d uv, double tolerance)
    {
      m_ptr = UnsafeNativeMethods.CRhinoSurfaceFillet_RailNew(RailFace.ConstPointer(), Rail3d.ConstPointer(), Face.ConstPointer(), ref uv, tolerance);
      // I know that throwing an exception is frowned upon in a constructor, but after talking with Steve Baer, the decision was:
      //     "this case is really never going to happen."
      if (null == m_ptr) throw new NullReferenceException("Could not instantiate a new SurfaceToRailFillet.");
    }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="face"></param>
    /// <param name="uv"></param>
    /// <param name="rail3d"></param>
    /// <param name="u"></param>
    /// <param name="tolerance"></param>
    /// <param name="radius"></param>
    /// <exception cref="NullReferenceException"></exception>
    protected SurfaceFilletBase(BrepFace face, Point2d uv, Curve rail3d, double u, double radius, double tolerance)
    { 
      m_ptr = UnsafeNativeMethods.CRhinoSurfaceFillet_CrvNew(face.ConstPointer(), ref uv, rail3d.ConstPointer(), u, radius, tolerance);
      // I know that throwing an exception is frowned upon in a constructor, but after talking with Steve Baer, the decision was:
      //     "this case is really never going to happen."
      if (null == m_ptr) throw new NullReferenceException("Could not instantiate a new SurfaceToRailFillet.");
    }
    /// <summary>
    /// 
    /// </summary>
    ~SurfaceFilletBase()
    {
      Dispose(false);
    }
    /// <summary>
    /// Actively reclaims unmanaged resources that this instance uses.
    /// </summary>
    /// <since>7.9</since>
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
        UnsafeNativeMethods.CRhinoSurfaceFillet_Delete(m_ptr);
      m_ptr = IntPtr.Zero;
    }

    /// <summary>
    /// This will clear out the existing rails and fillet surfaces, 
    /// and redo the offset intersections, etc.
    /// </summary>
    /// <param name="radius">The new radius</param>
    /// <returns>true if successful</returns>
    protected bool ChangeFilletRadius(double radius)
    {
      return UnsafeNativeMethods.CRhinoSurfaceFillet_ChangeRadius(m_ptr, radius);
    }

    /// <summary>
    /// If the two input breps are the same, TrimmedBreps[1] will be empty.
    /// The constructor idoes everything necessary for this to be called.
    /// Calles to the various fillet creation members will not change the result.
    /// </summary>
    /// <param name="bExtend"></param>
    /// <param name="TrimmedBreps0"></param>
    /// <param name="TrimmedBreps1"></param>
    /// <returns></returns>
    /// <since>8.0</since>
    public bool TrimBreps(bool bExtend, List<Brep> TrimmedBreps0, List<Brep> TrimmedBreps1) //Trim input breps with the rails.
    {
      using (SimpleArrayBrepPointer breps0 = new SimpleArrayBrepPointer())
      using (SimpleArrayBrepPointer breps1 = new SimpleArrayBrepPointer())
      {
        bool bResult = UnsafeNativeMethods.CRhinoSurfaceFillet_TrimBreps(m_ptr, bExtend, breps0.NonConstPointer(), breps1.NonConstPointer());
        if (bResult)
        {
          var breps0_array = breps0.ToNonConstArray();
          var breps1_array = breps1.ToNonConstArray();
          TrimmedBreps0.AddRange(breps0_array);
          TrimmedBreps1.AddRange(breps1_array);
          return bResult;
        }
      }
      return false;
    }

    /// <summary>
    /// make fillet surfaces and hack them up to active pieces. Adjusts cross sections 
    /// </summary>
    /// <param name="railDegree">The degree of the rail</param>
    /// <param name="bExtend">If true, when one input surface is longer than the other, the fillet surface is extended to the input surface edges.</param>
    /// <param name="Fillets">the fillet8s that were created</param>
    /// <returns>true if successful</returns>
    /// <since>8.0</since>
    public bool RationalArcs(int railDegree, bool bExtend, List<Brep> Fillets)
    {
      using (SimpleArrayBrepPointer fillets = new SimpleArrayBrepPointer())
      {
        bool bResult = UnsafeNativeMethods.CRhinoSurfaceFillet_RationalArcs(m_ptr, railDegree, bExtend, fillets.NonConstPointer());
        if (bResult)
        {
          var fillets_array = fillets.ToNonConstArray();
          Fillets.AddRange(fillets_array);
          return bResult;
        }
      }
      return false;
    }

    /// <summary>
    /// make fillet surfaces and hack them up to active pieces. Adjusts cross sections 
    /// </summary>
    /// <param name="railDegree">The degree of the rail</param>
    /// <param name="bExtend">If true, when one input surface is longer than the other, the fillet surface is extended to the input surface edges.</param>
    /// <param name="Fillets">the fillet8s that were created</param>
    /// <returns>true if successful</returns>
    /// <since>8.0</since>
    public bool NonRationalQuinticArcs(int railDegree, bool bExtend, List<Brep> Fillets) //Arc approximation
    {
      using (SimpleArrayBrepPointer fillets = new SimpleArrayBrepPointer())
      {
        bool bResult = UnsafeNativeMethods.CRhinoSurfaceFillet_NonRationalQuinticArcs(m_ptr, railDegree, bExtend, fillets.NonConstPointer());
        if (bResult)
        {
          var fillets_array = fillets.ToNonConstArray();
          Fillets.AddRange(fillets_array);
          return bResult;
        }
      }
      return false;
    }

    /// <summary>
    /// make fillet surfaces and hack them up to active pieces. Adjusts cross sections 
    /// </summary>
    /// <param name="railDegree">The degree of the rail</param>
    /// <param name="TanSlider">A number from -1 to 1 indicating how far towards the theoretical rational midpoint to adjust the tangent control points.</param>
    /// <param name="InnerSlider">A number from -1 to 1 indicating how far towards the theoretical rational midpoint to adjust the inner control points.</param>
    /// <param name="bExtend">If true, when one input surface is longer than the other, the fillet surface is extended to the input surface edges.</param>
    /// <param name="Fillets">the fillet8s that were created</param>
    /// <returns>true if successful</returns>
    /// <since>8.0</since>
    public bool NonRationalQuintic(int railDegree, double TanSlider, double InnerSlider, bool bExtend, List<Brep> Fillets)
    {
      using (SimpleArrayBrepPointer fillets = new SimpleArrayBrepPointer())
      {
        bool bResult = UnsafeNativeMethods.CRhinoSurfaceFillet_NonRationalQuintic(m_ptr, railDegree, TanSlider, InnerSlider, bExtend, fillets.NonConstPointer());
        if (bResult)
        {
          var fillets_array = fillets.ToNonConstArray();
          Fillets.AddRange(fillets_array);
          return bResult;
        }
      }
      return false;
    }

    /// <summary>
    /// make fillet surfaces and hack them up to active pieces. Adjusts cross sections 
    /// </summary>
    /// <param name="railDegree">The degree of the rail</param>
    /// <param name="bExtend">If true, when one input surface is longer than the other, the fillet surface is extended to the input surface edges.</param>
    /// <param name="Fillets">the fillet8s that were created</param>
    /// <returns>true if successful</returns>
    /// <since>8.0</since>
    public bool NonRationalQuarticArcs(int railDegree, bool bExtend, List<Brep> Fillets) //Arc approximation
    {
      using (SimpleArrayBrepPointer fillets = new SimpleArrayBrepPointer())
      {
        bool bResult = UnsafeNativeMethods.CRhinoSurfaceFillet_NonRationalQuarticArcs(m_ptr, railDegree, bExtend, fillets.NonConstPointer());
        if (bResult)
        {
          var fillets_array = fillets.ToNonConstArray();
          Fillets.AddRange(fillets_array);
          return bResult;
        }
      }
      return false;
    }

    /// <summary>
    /// make fillet surfaces and hack them up to active pieces. Adjusts cross sections 
    /// </summary>
    /// <param name="railDegree">The degree of the rail</param>
    /// <param name="TanSlider">A number from -1 to 1 indicating how far towards the theoretical rational midpoint to adjust the tangent control points.</param>
    /// <param name="InnerSlider">A number from -1 to 1 indicating how far towards the theoretical rational midpoint to adjust the inner control points.</param>
    /// <param name="bExtend">If true, when one input surface is longer than the other, the fillet surface is extended to the input surface edges.</param>
    /// <param name="Fillets">the fillet8s that were created</param>
    /// <returns>true if successful</returns>
    /// <since>8.0</since>
    public bool NonRationalQuartic(int railDegree, double TanSlider, double InnerSlider, bool bExtend, List<Brep> Fillets)
    {
      using (SimpleArrayBrepPointer fillets = new SimpleArrayBrepPointer())
      {
        bool bResult = UnsafeNativeMethods.CRhinoSurfaceFillet_NonRationalQuartic(m_ptr, railDegree, TanSlider, InnerSlider, bExtend, fillets.NonConstPointer());
        if (bResult)
        {
          var fillets_array = fillets.ToNonConstArray();
          Fillets.AddRange(fillets_array);
          return bResult;
        }
      }
      return false;
    }

    /// <summary>
    /// make fillet surfaces and hack them up to active pieces. Adjusts cross sections 
    /// </summary>
    /// <param name="railDegree">The degree of the rail</param>
    /// <param name="bExtend">If true, when one input surface is longer than the other, the fillet surface is extended to the input surface edges.</param>
    /// <param name="Fillets">the fillet8s that were created</param>
    /// <returns>true if successful</returns>
    /// <since>8.0</since>
    public bool NonRationalCubicArcs(int railDegree, bool bExtend, List<Brep> Fillets) //Arc approximation
    {
      using (SimpleArrayBrepPointer fillets = new SimpleArrayBrepPointer())
      {
        bool bResult = UnsafeNativeMethods.CRhinoSurfaceFillet_NonRationalCubicArcs(m_ptr, railDegree, bExtend, fillets.NonConstPointer());
        if (bResult)
        {
          var fillets_array = fillets.ToNonConstArray();
          Fillets.AddRange(fillets_array);
          return bResult;
        }
      }
      return false;
    }
    /// <summary>
    /// make fillet surfaces and hack them up to active pieces. Adjusts cross sections 
    /// </summary>
    /// <param name="railDegree">The degree of the rail</param>
    /// <param name="TanSlider">A number from -1 to 1 indicating how far towards the theoretical rational midpoint to adjust the tangent control points.</param>
    /// <param name="bExtend">If true, when one input surface is longer than the other, the fillet surface is extended to the input surface edges.</param>
    /// <param name="Fillets">the fillet8s that were created</param>
    /// <returns>true if successful</returns>
    /// <since>8.0</since>
    public bool NonRationalCubic(int railDegree, double TanSlider, bool bExtend, List<Brep> Fillets)
    {
      using (SimpleArrayBrepPointer fillets = new SimpleArrayBrepPointer())
      {
        bool bResult = UnsafeNativeMethods.CRhinoSurfaceFillet_NonRationalCubic(m_ptr, railDegree, TanSlider, bExtend, fillets.NonConstPointer());
        if (bResult)
        {
          var fillets_array = fillets.ToNonConstArray();
          Fillets.AddRange(fillets_array);
          return bResult;
        }
      }
      return false;
    }

    /// <summary>
    /// make fillet surfaces and hack them up to active pieces. Adjusts cross sections 
    /// </summary>
    /// <param name="railDegree">The degree of the rail</param>
    /// <param name="bExtend">If true, when one input surface is longer than the other, the fillet surface is extended to the input surface edges.</param>
    /// <param name="Fillets">the fillet8s that were created</param>
    /// <returns>true if successful</returns>
    /// <since>8.0</since>
    public bool G2ChordalQuintic(int railDegree, bool bExtend, List<Brep> Fillets)
    {
      using (SimpleArrayBrepPointer fillets = new SimpleArrayBrepPointer())
      {
        bool bResult = UnsafeNativeMethods.CRhinoSurfaceFillet_G2ChordalQuintic(m_ptr, railDegree, bExtend, fillets.NonConstPointer());
        if (bResult)
        {
          var fillets_array = fillets.ToNonConstArray();
          Fillets.AddRange(fillets_array);
          return bResult;
        }
      }
      return false;
    }

    /// <summary>
    /// Creates a standard surface fillet using rational arc sections
    /// </summary>
    /// <param name="faceA">the first face to use constructing the fillet</param>
    /// <param name="uvA">The parametric u,v selection point on FaceA close to the edge to fillet</param>
    /// <param name="faceB">the second face to use constructing the fillet</param>
    /// <param name="uvB">The parametric u,v selection point on FaceB close to the edge to fillet</param>
    /// <param name="radius">The radius of the fillet</param>
    /// <param name="tolerance">Tolerance to use in fitting a solution</param>
    /// <param name="trimmedBrepsA">if bTrim = true, returns the remains of FaceA trimmed to the fillet</param>
    /// <param name="trimmedBrepsB">if bTrim = true, the remains of FaceB trimmed to the fillet</param>
    /// <param name="rail_degree">the degree of the rail curve</param>
    /// <param name="bTrim">if True, trim the faces and retuen those results in resultsA and resultsB</param>
    /// <param name="bExtend">if True and if one input surface is longer than the other, the fillet surface is extended to the input surface edges</param>
    /// <param name="Fillets">the resulting fillet surfaces</param>
    /// <returns>True if successful.</returns>
    /// <since>8.0</since>
    public static bool CreateRationalArcsFilletSrf(BrepFace faceA, Point2d uvA, BrepFace faceB, Point2d uvB,
      double radius, double tolerance, List<Brep> trimmedBrepsA, List<Brep> trimmedBrepsB,
      int rail_degree, bool bTrim, bool bExtend, List<Brep> Fillets)
    {

      using (SimpleArrayBrepPointer trimmed_brepsA = new SimpleArrayBrepPointer())
      using (SimpleArrayBrepPointer trimmed_brepsB = new SimpleArrayBrepPointer())
      using (SimpleArrayBrepPointer fillets = new SimpleArrayBrepPointer())
      {
        bool bResult = UnsafeNativeMethods.RhCreateRationalArcsFilletSrf(faceA.ConstPointer(), uvA,
          faceB.ConstPointer(), uvB, radius, tolerance, trimmed_brepsA.NonConstPointer(), trimmed_brepsB.NonConstPointer(),
          rail_degree, bTrim, bExtend, fillets.NonConstPointer());
        if (bResult)
        {
          var brepsA_array = trimmed_brepsA.ToNonConstArray();
          var brepsB_array = trimmed_brepsB.ToNonConstArray();
          trimmedBrepsA.AddRange(brepsA_array);
          trimmedBrepsB.AddRange(brepsB_array);
          var fillets_array = fillets.ToNonConstArray();
          Fillets.AddRange(fillets_array);
          return bResult;
        }
      }
      return false;
    }

    /// <summary>
    /// Creates a surface fillet using Non-rational Quintic arc approximations as sections
    /// </summary>
    /// <param name="faceA">the first face to use constructing the fillet</param>
    /// <param name="uvA">The parametric u,v selection point on FaceA close to the edge to fillet</param>
    /// <param name="faceB">the second face to use constructing the fillet</param>
    /// <param name="uvB">The parametric u,v selection point on FaceB close to the edge to fillet</param>
    /// <param name="radius">The radius of the fillet</param>
    /// <param name="tolerance">Tolerance to use in fitting a solution</param>
    /// <param name="trimmedBrepsA">if bTrim = true, returns the remains of FaceA trimmed to the fillet</param>
    /// <param name="trimmedBrepsB">if bTrim = true, the remains of FaceB trimmed to the fillet</param>
    /// <param name="rail_degree">the degree of the rail curve</param>
    /// <param name="bTrim">if True, trim the faces and retuen those results in resultsA and resultsB</param>
    /// <param name="bExtend">if True and if one input surface is longer than the other, the fillet surface is extended to the input surface edges</param>
    /// <param name="Fillets">the resulting fillet surfaces</param>
    /// <returns>True if successful.</returns>
    /// <since>8.0</since>
    public static bool CreateNonRationalQuinticArcsFilletSrf(BrepFace faceA, Point2d uvA, BrepFace faceB, Point2d uvB,
      double radius, double tolerance, List<Brep> trimmedBrepsA, List<Brep> trimmedBrepsB,
      int rail_degree, bool bTrim, bool bExtend, List<Brep> Fillets)
    {
      using (SimpleArrayBrepPointer trimmed_brepsA = new SimpleArrayBrepPointer())
      using (SimpleArrayBrepPointer trimmed_brepsB = new SimpleArrayBrepPointer())
      using (SimpleArrayBrepPointer fillets = new SimpleArrayBrepPointer())
      {
        bool bResult = UnsafeNativeMethods.RhCreateNonRationalQuinticArcsFilletSrf(faceA.ConstPointer(), uvA,
          faceB.ConstPointer(), uvB, radius, tolerance, trimmed_brepsA.NonConstPointer(), trimmed_brepsB.NonConstPointer(),
          rail_degree, bTrim, bExtend, fillets.NonConstPointer());
        if (bResult)
        {
          var brepsA_array = trimmed_brepsA.ToNonConstArray();
          var brepsB_array = trimmed_brepsB.ToNonConstArray();
          trimmedBrepsA.AddRange(brepsA_array);
          trimmedBrepsB.AddRange(brepsB_array);
          var fillets_array = fillets.ToNonConstArray();
          Fillets.AddRange(fillets_array);
          return bResult;
        }
      }
      return false;
    }

    /// <summary>
    /// Creates a fillet using non-rational Quintic sections with a tangent and inner slider
    /// </summary>
    /// <param name="faceA">the first face to use constructing the fillet</param>
    /// <param name="uvA">The parametric u,v selection point on FaceA close to the edge to fillet</param>
    /// <param name="faceB">the second face to use constructing the fillet</param>
    /// <param name="uvB">The parametric u,v selection point on FaceB close to the edge to fillet</param>
    /// <param name="radius">The radius of the fillet</param>
    /// <param name="tolerance">Tolerance to use in fitting a solution</param>
    /// <param name="trimmedBrepsA">if bTrim = true, returns the remains of FaceA trimmed to the fillet</param>
    /// <param name="trimmedBrepsB">if bTrim = true, the remains of FaceB trimmed to the fillet</param>
    /// <param name="rail_degree">the degree of the rail curve</param>
    /// <param name="TanSlider">A number between -0.95 and 0.95 indicating how far to push the tangent control points toward or away from the theoretical quadratic middle control point</param>
    /// <param name="InnerSlider">A number between -0.95 and 0.95 indicating how far to push the inner control points toward or away from the theoretical quadratic middle control point</param>
    /// <param name="bTrim">if True, trim the faces and retuen those results in resultsA and resultsB</param>
    /// <param name="bExtend">if True and if one input surface is longer than the other, the fillet surface is extended to the input surface edges</param>
    /// <param name="Fillets">the resulting fillet surfaces</param>
    /// <returns>True if successful.</returns>
    /// <since>8.0</since>
    public static bool CreateNonRationalQuinticFilletSrf(BrepFace faceA, Point2d uvA, BrepFace faceB, Point2d uvB,
      double radius, double tolerance, List<Brep> trimmedBrepsA, List<Brep> trimmedBrepsB,
      int rail_degree, double TanSlider, double InnerSlider, bool bTrim, bool bExtend, List<Brep> Fillets)
    {
      using (SimpleArrayBrepPointer trimmed_brepsA = new SimpleArrayBrepPointer())
      using (SimpleArrayBrepPointer trimmed_brepsB = new SimpleArrayBrepPointer())
      using (SimpleArrayBrepPointer fillets = new SimpleArrayBrepPointer())
      {
        bool bResult = UnsafeNativeMethods.RhCreateNonRationalQuinticFilletSrf(faceA.ConstPointer(), uvA,
          faceB.ConstPointer(), uvB, radius, tolerance, trimmed_brepsA.NonConstPointer(), trimmed_brepsB.NonConstPointer(),
          rail_degree, TanSlider, InnerSlider, bTrim, bExtend, fillets.NonConstPointer());
        if (bResult)
        {
          var brepsA_array = trimmed_brepsA.ToNonConstArray();
          var brepsB_array = trimmed_brepsB.ToNonConstArray();
          trimmedBrepsA.AddRange(brepsA_array);
          trimmedBrepsB.AddRange(brepsB_array);
          var fillets_array = fillets.ToNonConstArray();
          Fillets.AddRange(fillets_array);
          return bResult;
        }
      }
      return false;
    }

    /// <summary>
    /// Creates a surface fillet using Non-rational Quartic arc approximations as sections
    /// </summary>
    /// <param name="faceA">the first face to use constructing the fillet</param>
    /// <param name="uvA">The parametric u,v selection point on FaceA close to the edge to fillet</param>
    /// <param name="faceB">the second face to use constructing the fillet</param>
    /// <param name="uvB">The parametric u,v selection point on FaceB close to the edge to fillet</param>
    /// <param name="radius">The radius of the fillet</param>
    /// <param name="tolerance">Tolerance to use in fitting a solution</param>
    /// <param name="trimmedBrepsA">if bTrim = true, returns the remains of FaceA trimmed to the fillet</param>
    /// <param name="trimmedBrepsB">if bTrim = true, the remains of FaceB trimmed to the fillet</param>
    /// <param name="rail_degree">the degree of the rail curve</param>
    /// <param name="bTrim">if True, trim the faces and retuen those results in resultsA and resultsB</param>
    /// <param name="bExtend">if True and if one input surface is longer than the other, the fillet surface is extended to the input surface edges</param>
    /// <param name="Fillets">the resulting fillet surfaces</param>
    /// <returns>True if successful.</returns>
    /// <since>8.0</since>
    public static bool CreateNonRationalQuarticArcsFilletSrf(BrepFace faceA, Point2d uvA, BrepFace faceB, Point2d uvB,
      double radius, double tolerance, List<Brep> trimmedBrepsA, List<Brep> trimmedBrepsB,
      int rail_degree, bool bTrim, bool bExtend, List<Brep> Fillets)
    {
      using (SimpleArrayBrepPointer trimmed_brepsA = new SimpleArrayBrepPointer())
      using (SimpleArrayBrepPointer trimmed_brepsB = new SimpleArrayBrepPointer())
      using (SimpleArrayBrepPointer fillets = new SimpleArrayBrepPointer())
      {
        bool bResult = UnsafeNativeMethods.RhCreateNonRationalQuarticArcsFilletSrf(faceA.ConstPointer(), uvA,
          faceB.ConstPointer(), uvB, radius, tolerance, trimmed_brepsA.NonConstPointer(), trimmed_brepsB.NonConstPointer(),
          rail_degree, bTrim, bExtend, fillets.NonConstPointer());
        if (bResult)
        {
          var brepsA_array = trimmed_brepsA.ToNonConstArray();
          var brepsB_array = trimmed_brepsB.ToNonConstArray();
          trimmedBrepsA.AddRange(brepsA_array);
          trimmedBrepsB.AddRange(brepsB_array);
          var fillets_array = fillets.ToNonConstArray();
          Fillets.AddRange(fillets_array);
          return bResult;
        }
      }
      return false;
    }

    /// <summary>
    /// Creates a fillet using non-rational Quartic sections with a tangent and inner slider
    /// </summary>
    /// <param name="faceA">the first face to use constructing the fillet</param>
    /// <param name="uvA">The parametric u,v selection point on FaceA close to the edge to fillet</param>
    /// <param name="faceB">the second face to use constructing the fillet</param>
    /// <param name="uvB">The parametric u,v selection point on FaceB close to the edge to fillet</param>
    /// <param name="radius">The radius of the fillet</param>
    /// <param name="tolerance">Tolerance to use in fitting a solution</param>
    /// <param name="trimmedBrepsA">if bTrim = true, returns the remains of FaceA trimmed to the fillet</param>
    /// <param name="trimmedBrepsB">if bTrim = true, the remains of FaceB trimmed to the fillet</param>
    /// <param name="rail_degree">the degree of the rail curve</param>
    /// <param name="TanSlider">A number between -0.95 and 0.95 indicating how far to push the tangent control points toward or away from the theoretical quadratic middle control point</param>
    /// <param name="InnerSlider">A number between -0.95 and 0.95 indicating how far to push the inner control point toward or away from the theoretical quadratic middle control point</param>
    /// <param name="bTrim">if True, trim the faces and retuen those results in resultsA and resultsB</param>
    /// <param name="bExtend">if True and if one input surface is longer than the other, the fillet surface is extended to the input surface edges</param>
    /// <param name="Fillets">the resulting fillet surfaces</param>
    /// <returns></returns>
    /// <since>8.0</since>
    public static bool CreateNonRationalQuarticFilletSrf(BrepFace faceA, Point2d uvA, BrepFace faceB, Point2d uvB,
      double radius, double tolerance, List<Brep> trimmedBrepsA, List<Brep> trimmedBrepsB,
      int rail_degree, double TanSlider, double InnerSlider, bool bTrim, bool bExtend, List<Brep> Fillets)
    {
      using (SimpleArrayBrepPointer trimmed_brepsA = new SimpleArrayBrepPointer())
      using (SimpleArrayBrepPointer trimmed_brepsB = new SimpleArrayBrepPointer())
      using (SimpleArrayBrepPointer fillets = new SimpleArrayBrepPointer())
      {
        bool bResult = UnsafeNativeMethods.RhCreateNonRationalQuarticFilletSrf(faceA.ConstPointer(), uvA,
          faceB.ConstPointer(), uvB, radius, tolerance, trimmed_brepsA.NonConstPointer(), trimmed_brepsB.NonConstPointer(),
          rail_degree, TanSlider, InnerSlider, bTrim, bExtend, fillets.NonConstPointer());
        if (bResult)
        {
          var brepsA_array = trimmed_brepsA.ToNonConstArray();
          var brepsB_array = trimmed_brepsB.ToNonConstArray();
          trimmedBrepsA.AddRange(brepsA_array);
          trimmedBrepsB.AddRange(brepsB_array);
          var fillets_array = fillets.ToNonConstArray();
          Fillets.AddRange(fillets_array);
          return bResult;
        }
      }
      return false;
    }

    /// <summary>
    /// Creates a surface fillet using Non-rational Cubic arc approximations as sections
    /// </summary>
    /// <param name="faceA">the first face to use constructing the fillet</param>
    /// <param name="uvA">The parametric u,v selection point on FaceA close to the edge to fillet</param>
    /// <param name="faceB">the second face to use constructing the fillet</param>
    /// <param name="uvB">The parametric u,v selection point on FaceB close to the edge to fillet</param>
    /// <param name="radius">The radius of the fillet</param>
    /// <param name="tolerance">Tolerance to use in fitting a solution</param>
    /// <param name="trimmedBrepsA">if bTrim = true, returns the remains of FaceA trimmed to the fillet</param>
    /// <param name="trimmedBrepsB">if bTrim = true, the remains of FaceB trimmed to the fillet</param>
    /// <param name="rail_degree">the degree of the rail curve</param>
    /// <param name="bTrim">if True, trim the faces and retuen those results in resultsA and resultsB</param>
    /// <param name="bExtend">if True and if one input surface is longer than the other, the fillet surface is extended to the input surface edges</param>
    /// <param name="Fillets">the resulting fillet surfaces</param>
    /// <returns>True if successful.</returns>
    /// <since>8.0</since>
    public static bool CreateNonRationalCubicArcsFilletSrf(BrepFace faceA, Point2d uvA, BrepFace faceB, Point2d uvB,
      double radius, double tolerance, List<Brep> trimmedBrepsA, List<Brep> trimmedBrepsB,
      int rail_degree, bool bTrim, bool bExtend, List<Brep> Fillets)
    {
      using (SimpleArrayBrepPointer trimmed_brepsA = new SimpleArrayBrepPointer())
      using (SimpleArrayBrepPointer trimmed_brepsB = new SimpleArrayBrepPointer())
      using (SimpleArrayBrepPointer fillets = new SimpleArrayBrepPointer())
      {
        bool bResult = UnsafeNativeMethods.RhCreateNonRationalCubicArcsFilletSrf(faceA.ConstPointer(), uvA,
          faceB.ConstPointer(), uvB, radius, tolerance, trimmed_brepsA.NonConstPointer(), trimmed_brepsB.NonConstPointer(),
          rail_degree, bTrim, bExtend, fillets.NonConstPointer());
        if (bResult)
        {
          var brepsA_array = trimmed_brepsA.ToNonConstArray();
          var brepsB_array = trimmed_brepsB.ToNonConstArray();
          trimmedBrepsA.AddRange(brepsA_array);
          trimmedBrepsB.AddRange(brepsB_array);
          var fillets_array = fillets.ToNonConstArray();
          Fillets.AddRange(fillets_array);
          return bResult;
        }
      }
      return false;
    }

    /// <summary>
    /// Creates a fillet using non-rational Cubic sections with a tangent slider
    /// </summary>
    /// <param name="faceA">the first face to use constructing the fillet</param>
    /// <param name="uvA">The parametric u,v selection point on FaceA close to the edge to fillet</param>
    /// <param name="faceB">the second face to use constructing the fillet</param>
    /// <param name="uvB">The parametric u,v selection point on FaceB close to the edge to fillet</param>
    /// <param name="radius">The radius of the fillet</param>
    /// <param name="tolerance">Tolerance to use in fitting a solution</param>
    /// <param name="trimmedBrepsA">if bTrim = true, returns the remains of FaceA trimmed to the fillet</param>
    /// <param name="trimmedBrepsB">if bTrim = true, the remains of FaceB trimmed to the fillet</param>
    /// <param name="rail_degree">the degree of the rail curve</param>
    /// <param name="TanSlider">A number between -0.95 and 0.95 indicating how far to push the tangent control points toward or away from the theoretical quadratic middle control point</param>
    /// <param name="bTrim">if True, trim the faces and retuen those results in resultsA and resultsB</param>
    /// <param name="bExtend">if True and if one input surface is longer than the other, the fillet surface is extended to the input surface edges</param>
    /// <param name="Fillets">the resulting fillet surfaces</param>
    /// <returns></returns>
    /// <since>8.0</since>
    public static bool CreateNonRationalCubicFilletSrf(BrepFace faceA, Point2d uvA, BrepFace faceB, Point2d uvB,
      double radius, double tolerance, List<Brep> trimmedBrepsA, List<Brep> trimmedBrepsB,
      int rail_degree, double TanSlider, bool bTrim, bool bExtend, List<Brep> Fillets)
    {
      using (SimpleArrayBrepPointer trimmed_brepsA = new SimpleArrayBrepPointer())
      using (SimpleArrayBrepPointer trimmed_brepsB = new SimpleArrayBrepPointer())
      using (SimpleArrayBrepPointer fillets = new SimpleArrayBrepPointer())
      {
        bool bResult = UnsafeNativeMethods.RhCreateNonRationalCubicFilletSrf(faceA.ConstPointer(), uvA,
          faceB.ConstPointer(), uvB, radius, tolerance, trimmed_brepsA.NonConstPointer(), trimmed_brepsB.NonConstPointer(),
          rail_degree, TanSlider, bTrim, bExtend, fillets.NonConstPointer());
        if (bResult)
        {
          var brepsA_array = trimmed_brepsA.ToNonConstArray();
          var brepsB_array = trimmed_brepsB.ToNonConstArray();
          trimmedBrepsA.AddRange(brepsA_array);
          trimmedBrepsB.AddRange(brepsB_array);
          var fillets_array = fillets.ToNonConstArray();
          Fillets.AddRange(fillets_array);
          return bResult;
        }
      }
      return false;
    }

    /// <summary>
    /// Creates a surface fillet using G2 chordal Quintic arc approximations as sections
    /// </summary>
    /// <param name="faceA">the first face to use constructing the fillet</param>
    /// <param name="uvA">The parametric u,v selection point on FaceA close to the edge to fillet</param>
    /// <param name="faceB">the second face to use constructing the fillet</param>
    /// <param name="uvB">The parametric u,v selection point on FaceB close to the edge to fillet</param>
    /// <param name="radius">The radius of the fillet</param>
    /// <param name="tolerance">Tolerance to use in fitting a solution</param>
    /// <param name="trimmedBrepsA">if bTrim = true, returns the remains of FaceA trimmed to the fillet</param>
    /// <param name="trimmedBrepsB">if bTrim = true, the remains of FaceB trimmed to the fillet</param>
    /// <param name="rail_degree">the degree of the rail curve</param>
    /// <param name="bTrim">if True, trim the faces and retuen those results in resultsA and resultsB</param>
    /// <param name="bExtend">if True and if one input surface is longer than the other, the fillet surface is extended to the input surface edges</param>
    /// <param name="Fillets">the resulting fillet surfaces</param>
    /// <returns>True if successful.</returns>
    /// <since>8.0</since>
    public static bool CreateG2ChordalQuinticFilletSrf(BrepFace faceA, Point2d uvA, BrepFace faceB, Point2d uvB,
      double radius, double tolerance, List<Brep> trimmedBrepsA, List<Brep> trimmedBrepsB,
      int rail_degree, bool bTrim, bool bExtend, List<Brep> Fillets)
    {
      using (SimpleArrayBrepPointer trimmed_brepsA = new SimpleArrayBrepPointer())
      using (SimpleArrayBrepPointer trimmed_brepsB = new SimpleArrayBrepPointer())
      using (SimpleArrayBrepPointer fillets = new SimpleArrayBrepPointer())
      {
        bool bResult = UnsafeNativeMethods.RhCreateG2ChordalQuinticFilletSrf(faceA.ConstPointer(), uvA,
          faceB.ConstPointer(), uvB, radius, tolerance, trimmed_brepsA.NonConstPointer(), trimmed_brepsB.NonConstPointer(),
          rail_degree, bTrim, bExtend, fillets.NonConstPointer());
        if (bResult)
        {
          var brepsA_array = trimmed_brepsA.ToNonConstArray();
          var brepsB_array = trimmed_brepsB.ToNonConstArray();
          trimmedBrepsA.AddRange(brepsA_array);
          trimmedBrepsB.AddRange(brepsB_array);
          var fillets_array = fillets.ToNonConstArray();
          Fillets.AddRange(fillets_array);
          return bResult;
        }
      }
      return false;
    }
    /// <summary>
    /// Check to see if the fillet is properly initialized. If it is not, then the selected curves, surfaces, and/or radius must
    /// be adjusted in some way before creating the fillet
    /// </summary>
    /// <returns></returns>
    public bool IsInitialized()
    {
      return UnsafeNativeMethods.RhIsSurfaceFilletInitialized(m_ptr);
    }

  }

  /// <summary>
  /// New interactive FilletSrf
  /// </summary>
  internal class SurfaceFillet : SurfaceFilletBase
  {
    /// <summary>
    /// 
    /// </summary>
    /// <param name="FaceA"></param>
    /// <param name="uvA"></param>
    /// <param name="FaceB"></param>
    /// <param name="uvB"></param>
    /// <param name="radius"></param>
    /// <param name="tolerance"></param>
    public SurfaceFillet(BrepFace FaceA, Point2d uvA, BrepFace FaceB, Point2d uvB,
      double radius, double tolerance) : base(FaceA, uvA, FaceB, uvB, radius, tolerance)
    { }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="FaceA"></param>
    /// <param name="uvA"></param>
    /// <param name="FaceB"></param>
    /// <param name="uvB"></param>
    /// <param name="radius"></param>
    /// <param name="allowExtend"></param>
    /// <param name="tolerance"></param>
    public SurfaceFillet(BrepFace FaceA, Point2d uvA, BrepFace FaceB, Point2d uvB,
      double radius, bool allowExtend, double tolerance) : base(FaceA, uvA, FaceB, uvB, radius, allowExtend, tolerance)
    { }

    /// <summary>
    /// This will clear out the existing rails and fillet surfaces, 
    /// and redo the offset intersections, etc.
    /// </summary>
    /// <param name="radius">The new radius</param>
    /// <returns>true if successful</returns>
    public bool ChangeRadius(double radius)
    {
      return base.ChangeFilletRadius(radius);
    }
  }

  /// <summary>
  /// New interactive FilletSrf
  /// </summary>
  internal class SurfaceToRailFillet : SurfaceFilletBase
  {
    /// <summary>
    /// 
    /// </summary>
    /// <param name="RailFace"></param>
    /// <param name="Rail3d"></param>
    /// <param name="Face"></param>
    /// <param name="uv"></param>
    /// <param name="tolerance"></param>
    /// <exception cref="NullReferenceException"></exception>
    public SurfaceToRailFillet(BrepFace RailFace, Curve Rail3d, BrepFace Face, Point2d uv, double tolerance)
      : base(RailFace, Rail3d, Face, uv, tolerance)
    { }
    /// <summary>
    /// This will clear out the existing rails and fillet surfaces, 
    /// and redo the offset intersections, etc.
    /// </summary>
    /// <param name="radius">The new radius</param>
    /// <returns>true if successful</returns>
    public bool ChangeRadius(double radius)
    {
      return base.ChangeFilletRadius(radius);
    }

  }

  internal class SurfaceToCurveFillet : SurfaceFilletBase
  {
    public SurfaceToCurveFillet(BrepFace face, Point2d uv, Curve rail3d, double u, double radius, double tolerance)
      : base(face, uv, rail3d, u, radius, tolerance)
    { }
  }
}

#endif
