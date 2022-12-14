using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Rhino.Runtime.InteropWrappers;

#if RHINO_SDK
namespace Rhino.Geometry
{
  /// <summary>
  /// New interactive attempt at FilletSrf
  /// </summary>
  public static class SurfaceFillet
  {
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
  }
}

#endif
