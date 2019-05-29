using System;
using System.Collections.Generic;
using System.Linq;
using Rhino.Geometry;
using Rhino.UI;

#pragma warning disable 1591

namespace Rhino.FileIO
{
  internal partial class FileObjReader
  {
    bool SetCsType()
    {
      m_rat = false;
      m_cs_type = CsType.CsUnknown;

      int i, ct = m_text_file.StringCount;
      if (3 < ct || 2 > ct)
        return false;

      for (i = 1; i < ct; i++)
      {
        if (StringsMatch(m_text_file.GetString(i), "rat"))
          m_rat = true;
        else if (StringsMatch(m_text_file.GetString(i), "bmatrix"))
          m_cs_type = CsType.Bmatrix;
        else if (StringsMatch(m_text_file.GetString(i), "bezier"))
          m_cs_type = CsType.Bezier;
        else if (StringsMatch(m_text_file.GetString(i), "bspline"))
          m_cs_type = CsType.Bspline;
        else if (StringsMatch(m_text_file.GetString(i), "cardinal"))
          m_cs_type = CsType.Cardinal;
        else if (StringsMatch(m_text_file.GetString(i), "taylor"))
          m_cs_type = CsType.Taylor;
        else 
          return false;    
      }

      return true;
    }

    bool SetDegree()
    {
      int ct = m_text_file.StringCount;
      if (3 < ct || 2 > ct)
        return false;

      if (false == ConvertStringToInt(m_text_file.GetString(1), out m_deg_u))
        return false;

      if (3 == ct)
      {
        if (false == ConvertStringToInt(m_text_file.GetString(2), out m_deg_v))
          return false;
      }
  
      return true;
    }

    bool AddPolyline()
    {
      int i, ct = m_text_file.StringCount;

      if (3 > ct)
        return false;

      List<Point3d> pts = new List<Point3d>(ct);
      for (i=1;ct > i; i++)
      {
        int vidx;
        if (false == ConvertStringToInt(m_text_file.GetString(i), out vidx))
          return false;
        if (false == ConvertIndex(ref vidx, m_v_array.Count))
          return false;
        pts.Add(m_v_array[vidx]);
      }

      PolylineCurve pline = new PolylineCurve(pts);

      m_current_ogo.PlineArray.Add(pline);
      return true;
    }

    bool AddCurveKnots()
    {
      if (ElementType.Curve != m_element_type && ElementType.Curve2D != m_element_type)
        return false;

      int i, j, ct = m_text_file.StringCount;
      if (3>ct)
        return false;

      string str = m_text_file.GetString(1);

      var crv = ElementType.Curve == m_element_type ? m_current_ogo.CurveArray.Last() : m_curv2_array.Last();

      if (null == crv)
        return false;

      if (false == StringsMatch(str, "u"))
        return false;

      if (CsType.Bspline == m_cs_type)
      {
        int knotcount = ct - 2;
        int knot_capacity = crv.Order + crv.Points.Count - 2;
        if (knotcount-2 == knot_capacity)
        {
          i=3;
          knotcount = knotcount - 2;
        }
        else if (knotcount == knot_capacity)
          i=2;
        else
        {
          RhinoApp.WriteLine(Localization.LocalizeString("Knot count does not correspond with control point count in the curve.\n", 13));
          return false;
        }

        for (j=0; j < knotcount; i++)
        {
          double d;
          if (false == ConvertStringToDouble(m_text_file.GetString(i), out d))
            return false;
          crv.Knots[j++] = d;
        }
      }
      else if (CsType.Bezier == m_cs_type)
      {
        List<double> knot_array = new List<double>(ct);
        //TODO need to figure how to tell if there are superfluous knots in the file
        //
        for (i = 2; i < ct; i++)
        {
          double d;
          if (false == ConvertStringToDouble(m_text_file.GetString(i), out d))
            return false;
          knot_array.Add(d);
        }

        int degree = crv.Degree;
        int knotcount = knot_array.Count*crv.Degree;
        int kc=0;
        for (i=0 ;i < knot_array.Count && kc < knotcount;i++)
        {
          for (j=0; j<degree; j++)
            crv.Knots[kc++] = knot_array[i];
        }
      }
      else if (CsType.Bmatrix == m_cs_type)
      {
        //TODO
      }
      else if (CsType.Cardinal == m_cs_type)
      {
        //TODO
      }
      else if (CsType.Taylor == m_cs_type)
      {
        //TODO
      }
      else
        return false;

      return true;
    }


    bool AddCurve(bool curve2D = false)
    {
      int i, j, ct = m_text_file.StringCount, vct;
      if (3 > ct)
        return false;

      if (CsType.Bspline != m_cs_type && CsType.Bezier != m_cs_type)
      {
        string type = TypeToString(m_cs_type);
        string str = string.Format(Localization.LocalizeString("This file contains a {0} curve which is not yet supported.\n", 14), type);
        str += Localization.LocalizeString("Please email the file you are attempting to import to tim@mcneel.com\n", 15);
        str += string.Format(Localization.LocalizeString("and {0} curve import will be supported in the next release.\n", 16), type);
        Dialogs.ShowMessage(str, Localization.LocalizeString("OBJ Import Error", 17), ShowMessageButton.OKCancel, ShowMessageIcon.Warning);
        return false;
      }

      int dim;
      int cv_ct;

      if (curve2D)
      {
        m_element_type = ElementType.Curve2D;
        vct = m_vp_array.Count;
        dim = 2;
        cv_ct = ct - 1;
        i=1;
      }
      else
      {
        m_element_type = ElementType.Curve;
        vct = m_v_array.Count;
        dim = 3;
        cv_ct = ct - 3;
        i=3;
      }

      NurbsCurve nc = new NurbsCurve(dim, m_rat, m_deg_u+1, cv_ct);
      if (curve2D)
        m_curv2_array.Add(nc);
      else
      {
        m_current_ogo.AddCurve(nc);
      }


      Point3d pt3_d = new Point3d();
      for (j=0; i < ct; i++, j++)
      {
        int inum;
        if (false == ConvertStringToInt(m_text_file.GetString(i), out inum))
          return false;

        if (false == ConvertIndex(ref inum, vct))
          return false;

        if (curve2D)
        {
          pt3_d.X = m_vp_array[inum].X;
          pt3_d.Y = m_vp_array[inum].Y;
          pt3_d.Z = 0.0;
        }
        else
        {
          pt3_d.X = m_v_array[inum].X;
          pt3_d.Y = m_v_array[inum].Y;
          pt3_d.Z = m_v_array[inum].Z;
        }

        if (m_rat)
          nc.Points.SetPoint(j, pt3_d, curve2D ? m_vp_weight_array[inum] : m_v_weight_array[inum]);
        else
          nc.Points.SetPoint(j, pt3_d);
      }

      return true;
    }

    bool AddLoop()
    {
      if (ElementType.Surface != m_element_type)
        return false;

      Brep brep = m_current_ogo.BrepArray.Last();
      if (null == brep || 0 == brep.Faces.Count)
        return false;

      BrepFace face = brep.Faces[0];
      if (null == face)
        return false;

      NurbsSurface surf = brep.Surfaces[face.SurfaceIndex]as NurbsSurface;
      if (null == surf)
        return false;

      string str = m_text_file.GetString(0);
      BrepLoopType loop_type = BrepLoopType.Unknown;

      if (StringsMatch(str, "trim"))
        loop_type = BrepLoopType.Outer;
      else if (StringsMatch(str, "hole"))
        loop_type = BrepLoopType.Inner;

      int i, ct = m_text_file.StringCount, crvct = m_curv2_array.Count;
      if (0 != (ct - 1)%3)
        return false;


      BrepLoop loop = brep.Loops.Add(loop_type, face);
      for (i = 1; ct > i; i += 3)
      {
        double t0;
        if (false == ConvertStringToDouble(m_text_file.GetString(i), out t0))
          return false;

        double t1;
        if (false == ConvertStringToDouble(m_text_file.GetString(i + 1), out t1))
          return false;

        int inum;
        if (false == ConvertStringToInt(m_text_file.GetString(i + 2), out inum))
          return false;

        if (false == ConvertIndex(ref inum, crvct))
          return false;

        NurbsCurve c2_d_new = new NurbsCurve(m_curv2_array[inum]);
        Interval dom = new Interval(t0, t1);
        var edge_curve = surf.Pushup(c2_d_new, m_tol, dom);
        var c2_di = brep.AddTrimCurve(c2_d_new);

        if (null != edge_curve)
        {
          var c3_di = brep.AddEdgeCurve(edge_curve);
          BrepVertex startvert = brep.Vertices.Add(edge_curve.PointAtStart, m_tol);
          if (edge_curve.IsClosed)
          {
            BrepEdge edge = brep.Edges.Add(brep.Vertices[startvert.VertexIndex], brep.Vertices[startvert.VertexIndex], c3_di, dom, m_tol);
            BrepTrim trim = brep.Trims.Add(edge, false, loop, c2_di);
            trim.Domain = dom;
            if (trim.Domain.IsIncreasing == edge.Domain.IsDecreasing)
              trim.Reverse();
          }
          else
          {
            BrepVertex endvert = brep.Vertices.Add(edge_curve.PointAtEnd, m_tol);
            BrepEdge edge = brep.Edges.Add(brep.Vertices[startvert.VertexIndex], brep.Vertices[endvert.VertexIndex], c3_di, dom, m_tol);
            BrepTrim trim = brep.Trims.Add(edge, false, loop, c2_di);
            trim.Domain = dom;
            if (trim.StartVertex.VertexIndex == edge.StartVertex.VertexIndex)
              trim.Reverse();
          }
        }
      }

      return true;
    }

    private bool bDisplayUnknownSurfaceWarning = true;
    bool AddSurface()
    {
      int ct = m_text_file.StringCount;
      if (5 > ct)
        return false;

      if (CsType.Bspline == m_cs_type)
        return AddBSplineSurface();

      if (CsType.Bezier == m_cs_type)
        return AddBSplineSurface();

      if (bDisplayUnknownSurfaceWarning)
      {
        string type = TypeToString(m_cs_type);
        string str =
          string.Format(
            Localization.LocalizeString("This file contains a {0} surface which is not yet supported.\n", 18), type);
        str += Localization.LocalizeString("Please email the file you are attempting to import to tim@mcneel.com\n",
          19);
        str += string.Format(
          Localization.LocalizeString("and {0} surface import will be supported in the next release.\n", 20), type);
        Dialogs.ShowMessage(str, Localization.LocalizeString("OBJ Import Error", 21), ShowMessageButton.OKCancel,
          ShowMessageIcon.Warning);
        bDisplayUnknownSurfaceWarning = false;
      }

      // May 29, 2015 - Tim
      // Fix for RH-30541.  These files have "surf"s in them but no cstype.  
      // returning true here will essentially ignore them and allow the entire file
      // to be read.
      return true;
    }

    bool AddBSplineSurface()
    {
      m_current_ogo.SurfCvIdxArray.Clear();
      int i, ct = m_text_file.StringCount, vct = m_v_array.Count;

      m_b_have_knots_in_u_dir = false;
      m_b_have_knots_in_v_dir = false;

      m_element_type = ElementType.Surface;
      Brep brep = new Brep();
      m_current_ogo.BrepArray.Add(brep);

      for (i = 5; i < ct; i++)
      {
        int inum;
        if (false == ConvertStringToInt(m_text_file.GetString(i), out inum))
          return false;
    
        if (false == ConvertIndex(ref inum, vct))
          return false;

        //With OBJ all of the CVs appear together and you
        //can't distinguish what's u and what's v.  So we'll 
        //store the indexes on an array and add the CVs after 
        //we know the knots in each direction
        m_current_ogo.SurfCvIdxArray.Add(inum);  
      }

      //In RhinoCommon it's no longer possible to get direct access to the know and CV arrays of the surface
      //You have to know the CV Count in each direction in order to create the surface so the memory is allocated right.
      //I think the line below would allocate the right amount of CV memory since it's a single dimension array but the 
      //CV counts cannot be changed after the fact.  Also, at the point of writing this comment, prior to any debugging occurring
      //I haven't determined how the KnotU and KnotV arrays work exactly, there's an insert knot but I don't think that's meant for
      //the initial setting since there's a [].
      //NurbsSurface surf = NurbsSurface.Create(3, m_Rat, m_DegU+1, m_DegV+1, 1, m_CurrentOGO.m_SurfCVIdxArray.Count);
      //return -1 != brep.AddSurface(surf);

      return true;
    }

    bool AddSurfaceKnots()
    {
      if (ElementType.Surface != m_element_type)
        return false;

      int i, j, ct = m_text_file.StringCount;
      if (2 > ct)
        return false;
      
      Brep brep = m_current_ogo.BrepArray.Last();
      if (null == brep)
        return false;

      int idx;
      string str = m_text_file.GetString(1);
      if (StringsMatch(str, "U"))
        idx = 0;
      else if (StringsMatch(str, "V"))
        idx = 1;
      else
        return false;

      if (CsType.Bspline == m_cs_type)
      {
        if (0 == idx)
          m_current_ogo.SurfUKnotArray = new double[ct - 2 - 2]; //-2 for "parm" and "u" and -2 for superflous knots
        else
          m_current_ogo.SurfVKnotArray = new double[ct - 2 - 2]; //-2 for "parm" and "v" and -2 for superflous knots

        for (j=0, i=3; i < ct-1; i++)
        {
          double d;
          if (false == ConvertStringToDouble(m_text_file.GetString(i), out d))
            return false;
          if (0 == idx)
            m_current_ogo.SurfUKnotArray[j++] = d;
          else
            m_current_ogo.SurfVKnotArray[j++] = d;
        }

        if (0 == idx)
          m_b_have_knots_in_u_dir = true;
        if (1 == idx)
          m_b_have_knots_in_v_dir = true;

        if (m_b_have_knots_in_u_dir && m_b_have_knots_in_v_dir)
        {
          //Now that we have knots in both directions we can determine the cv
          // count in both directions
          int cv_u_ct = m_current_ogo.SurfUKnotArray.Length - ((m_deg_u+1) - 2);
          int cv_v_ct = m_current_ogo.SurfVKnotArray.Length - ((m_deg_v+1) - 2);

          //Create the surface here now since we're not able to do that in AddBSplineSurface in RhinoCommon
          NurbsSurface surf = NurbsSurface.Create(3, m_rat, m_deg_u+1, m_deg_v+1, cv_u_ct, cv_v_ct);
          if (null == surf || m_current_ogo.SurfUKnotArray.Length != surf.KnotsU.Count)
            return false;
          for (i = 0; surf.KnotsU.Count > i; i++)
            surf.KnotsU[i] = m_current_ogo.SurfUKnotArray[i];

          if (m_current_ogo.SurfVKnotArray.Length != surf.KnotsV.Count)
            return false;
          for (i = 0; surf.KnotsV.Count > i; i++)
            surf.KnotsV[i] = m_current_ogo.SurfVKnotArray[i];

          int vi;
          int cv = 0;
          int cvidxct = m_current_ogo.SurfCvIdxArray.Count;
          // read vertices (in Fortranic order)
          for ( vi = 0; cv_v_ct > vi ; vi++ )
          {
            int ui;
            for ( ui = 0; cv_u_ct > ui; ui++ ) 
            {
              if (cvidxct > cv)
              {
                if (m_rat)
                  surf.Points.SetControlPoint(ui, vi, new ControlPoint(m_v_array[m_current_ogo.SurfCvIdxArray[cv]], m_v_weight_array[m_current_ogo.SurfCvIdxArray[cv]]));
                else
                  surf.Points.SetControlPoint(ui, vi, new ControlPoint(m_v_array[m_current_ogo.SurfCvIdxArray[cv]]));
              }
              cv++;
            }
          }

          // Note: there are Add functions for the Faces list, one takes a surface and cooks up an outer boundary for 
          // you.  The other takes a surface index and expects that you will fill in the loops, trims, etc.
          // We want the latter in this instance.  Initially I had the former and the loop I added, expecting to be an outer,
          // ended up as a hole.
          brep.Faces.Add(brep.AddSurface(surf));
        }
      }
      else if (CsType.Bezier == m_cs_type)
      {
        int degree;
        if (0 == idx)
        {
          degree = m_deg_u;
          m_current_ogo.SurfUKnotArray = new double[degree];
        }
        else
        {
          degree = m_deg_v;
          m_current_ogo.SurfVKnotArray = new double[degree];
        }

        if (4 > ct)
          return false;

        double dnum0, dnum1;
        if (false == ConvertStringToDouble(m_text_file.GetString(2), out dnum0))
          return false;

        if (false == ConvertStringToDouble(m_text_file.GetString(3), out dnum1))
          return false;

        for (j=0, i=0; degree > i; i++)
        {
          if (0 == idx)
            m_current_ogo.SurfUKnotArray[j++] = dnum0;
          else
            m_current_ogo.SurfVKnotArray[j++] = dnum0;
        }

        for (i=degree; degree*2 > i; i++)
        {
          if (0 == idx)
            m_current_ogo.SurfUKnotArray[j++] = dnum1;
          else
            m_current_ogo.SurfVKnotArray[j++] = dnum1;
        }

        if (0 == idx)
          m_b_have_knots_in_u_dir = true;
        if (1 == idx)
          m_b_have_knots_in_v_dir = true;

        if (m_b_have_knots_in_u_dir && m_b_have_knots_in_v_dir)
        {
          //Now that we have knots in both directions we can determine the cv
          // count in both directions
          int cv_u_ct = m_current_ogo.SurfUKnotArray.Length - m_deg_u - 1;
          int cv_v_ct = m_current_ogo.SurfVKnotArray.Length - m_deg_v - 1;

          //Create the surface here now since we're not able to do that in AddBSplineSurface in RhinoCommon
          NurbsSurface surf = NurbsSurface.Create(3, m_rat, m_deg_u+1, m_deg_v+1, cv_u_ct, cv_v_ct);
          int vi;
          int cv = 0;
          int cvidxct = m_current_ogo.SurfCvIdxArray.Count;
          // read vertices (in Fortranic order)
          ControlPoint pt = new ControlPoint();
          for ( vi = 0; cv_v_ct > vi ; vi++ )
          {
            int ui;
            for ( ui = 0; cv_u_ct > ui; ui++ ) 
            {
              if (cvidxct > cv)
              {
                pt.Location = new Point3d(m_v_array[m_current_ogo.SurfCvIdxArray[cv]].X, m_v_array[m_current_ogo.SurfCvIdxArray[cv]].Y, m_v_array[m_current_ogo.SurfCvIdxArray[cv]].Z);
                pt.Weight = m_v_weight_array[m_current_ogo.SurfCvIdxArray[cv]];
                surf.Points.SetControlPoint(ui, vi, pt);
              }
              cv++;
            }
          }

          // Note: there are Add functions for the Faces list, one takes a surface and cooks up an outer boundary for 
          // you.  The other takes a surface index and expects that you will fill in the loops, trims, etc.
          // We want the latter in this instance.  Initially I had the former and the loop I added, expecting to be an outer,
          // ended up as a hole.
          brep.Faces.Add(brep.AddSurface(surf));
        }
      }
      else if (CsType.Bmatrix == m_cs_type)
      {
        //TODO
      }
      else if (CsType.Cardinal == m_cs_type)
      {
        //TODO
      }
      else if (CsType.Taylor == m_cs_type)
      {
        //TODO
      }
      else
        return false;

      return true;
    }
  }


  internal partial class FileObjWriter
  {
    double ChordFromAngle(double angleInRadians)
    {
      double chr;

      angleInRadians *= 0.5;
      if (angleInRadians <= 0.0 || angleInRadians >= Math.PI)
        chr = 0.0;
      else
        chr = 0.5 * (1.0 - Math.Cos(angleInRadians)) / Math.Sin(angleInRadians);
      return chr;
    }

    void WriteObjPoint(FileObjWriteOptions options, Point point)
    {
      int basevind = m_vindx;		     // base vertex index;

      basevind++;

      WriteObjVertex(options, point.Location);

      WriteObjString(options, $"p {m_vindx + 1}");
      m_vindx = basevind;		     // base vertex index;
    }

    void WriteObjPointCloud(FileObjWriteOptions options, PointCloud pointset)
    {
      int basevind = m_vindx;		     // base vertex index;

      int i, ct = pointset.Count;
      var num_format = $"G{options.SignificantDigits}";
      for (i = 0; ct > i; i++)
      {
        WriteObjString(options, $"v {pointset[i].X.ToString(num_format, m_culture_info_en_us)} {pointset[i].Y.ToString(num_format, m_culture_info_en_us)} {pointset[i].Z.ToString(num_format, m_culture_info_en_us)}");
        basevind++;
      }


      String str = $"p {m_vindx + 1}";
      for (i=1; ct>i; i++)
        str = $"{str} {i + m_vindx + 1}";

      WriteObjString(options, str);
      m_vindx = basevind;		     // base vertex index;
    }

    void WriteObjVertex(FileObjWriteOptions options,
                        int dim,     // 2 = vp x y ...
                                    // 3 = v x y z ...
                        int isRat,  // 0 = not rational
                                    // 1 = homogeneous rational - write ( x/w, y/w, z/w, w )
                        ControlPoint cp)
    {
      // OBJ files store rational points in euclidean form (like AG and IGES)

      if(!(dim == 2 || dim == 3) || !(isRat == 0 || isRat == 1))
        return;

      var p = new double[4];
      p[0] = cp.Location[0];
      p[1] = cp.Location[1];
      if (3 == dim)
      {
        p[2] = cp.Location[2];
        p[3] = cp.Weight;
      }
      else
      {
        p[2] = cp.Weight;
        p[3] = 0;
      }

      for (int i=0; (dim+isRat) > i; i++)
      {
        //This is to get rid of negative 0s in the ascii file
        if (Math.Abs(p[i]) < RhinoMath.ZeroTolerance)
          p[i] = 0.0;
      }

      var num_format = $"G{options.SignificantDigits}";
      if ( dim == 3 ) 
      {
        // 3d vertex
        if ( 1 == isRat ) 
          WriteObjString(options, $"v {p[0].ToString(num_format, m_culture_info_en_us)} {p[1].ToString(num_format, m_culture_info_en_us)} {p[2].ToString(m_culture_info_en_us)} {p[3].ToString(num_format, m_culture_info_en_us)}");
        else 
          WriteObjString(options, $"v {p[0].ToString(num_format, m_culture_info_en_us)} {p[1].ToString(num_format, m_culture_info_en_us)} {p[2].ToString(num_format, m_culture_info_en_us)}");
        m_vindx++;
      }
      else 
      {
        // 2d parameter space vertex
        if ( 1 == isRat ) 
          WriteObjString(options, $"vp {p[0].ToString(num_format, m_culture_info_en_us)} {p[1].ToString(num_format, m_culture_info_en_us)} {p[2].ToString(num_format, m_culture_info_en_us)}");
        else 
          WriteObjString(options, $"vp {p[0].ToString(num_format, m_culture_info_en_us)} {p[1].ToString(num_format, m_culture_info_en_us)}");
        m_vpindx++;
      }
    }

    void WriteObjKnotVector(FileObjWriteOptions options, 
                            int dir, 
                            int order,
                            int cvCount, 
                            double k0,
                            double k1,
                            double[] knots)
    {
      var str = 0 == dir ? "parm u" : "parm v";

      int i, knot_count = order + cvCount - 2;

      var num_format = $"G{options.SignificantDigits}";
      str = $"{str} {k0.ToString(num_format, m_culture_info_en_us)}";
      for (i = 0; knot_count > i; i++)
        str = $"{str} {knots[i].ToString(num_format, m_culture_info_en_us)}";

      str = $"{str} {k1.ToString(num_format, m_culture_info_en_us)}";

      WriteObjString(options, str);
    }

    void WriteObjPolyline(FileObjWriteOptions options, PolylineCurve polyline)
    {
      int i, startv, unique_point_count;

      var dim = polyline.Dimension;
      var point_count = polyline.PointCount;

      if (polyline.IsClosed)
        unique_point_count = point_count-1;
      else
        unique_point_count = point_count;

      // get starting vertex index
      if (options.UseRelativeIndexing) 
      {
        startv = -unique_point_count;
      }
      else 
      {
        if( dim == 2 )
          startv = m_vpindx + 1;
        else
          startv = m_vindx + 1;
      }
  
      // write vertices
      for (i = 0; unique_point_count> i; i++)
        WriteObjVertex(options, dim, 0, new ControlPoint(polyline.Point(i), 1.0));

      WriteObjString(options, "cstype bezier");
      WriteObjString(options, "deg 1");

      var str = dim == 2 ? "curv2" : $"curv 0 {point_count - 1}";

      // write vertex indices
      for ( i = 0; unique_point_count > i; i++ )
        str = $"{str} {i + startv}";

      for ( i = 0; point_count - unique_point_count > i; i++ )
        str = $"{str} {i + startv}";

      WriteObjString(options, str);

      /* write knot vector */
      str = "parm u";

      var num_format = $"G{options.SignificantDigits}";
      for (i = 0; i < point_count; i++)
        str = $"{str} {polyline.Parameter(i).ToString(num_format, m_culture_info_en_us)}";

      WriteObjString(options, str);

      WriteObjString(options, "end");

      if( dim == 2 )
        m_curv2_count++;
    }

    void WriteObjNurb(FileObjWriteOptions options, Curve crv )
    {
      if (null == crv)
        return;

      int i, unique_point_count, startv;

      NurbsCurve nc = crv.ToNurbsCurve();
      if (false == nc.IsValid)
        return;

      if ( 2 > nc.Dimension || 3 < nc.Dimension )
        return;
    
      bool is_rat = nc.IsRational;

      var point_count = nc.Points.Count;

      if (nc.IsClosed)
      {
        if (nc.IsPeriodic)
          unique_point_count = point_count - nc.Order + 1;
        else
          unique_point_count = point_count - 1;
      }
      else
        unique_point_count = point_count;


      // get starting vertex index
      if (options.UseRelativeIndexing) 
      {
        startv = -unique_point_count;
      }
      else 
      {
        if( nc.Dimension == 2 )
          startv = m_vpindx+1;
        else
          startv = m_vindx+1;
      }
  

      if (nc.Dimension == 2) 
	    {
        // trim curve case
        if (FileObjWriteOptions.CurveType.Polyline == options.TrimCurveType && 0.0 < options.AngleTolRadians)
        {
          PolylineCurve pl = nc.ToPolyline(0, 0,                // max & min segment count
                                           options.AngleTolRadians, // max angle
                                           ChordFromAngle(20),  // chord length
                                           0.0,                 // aspect
                                           0,                   // tolerance
                                           0.0,                 // min length
                                           0,                   // max length
                                           true);               // keep start point
          if (null != pl)
          {
            //TODO do we need a polyline clean?  It gets rid of adjacent duplicate points.
				    //PL.m_pline.Clean();
				    WriteObjPolyline(options, pl);
			    }
			    else
            return;
        }
      }


      // write vertices
      for (i = 0; unique_point_count > i; i++)
        WriteObjVertex( options, nc.Dimension, (is_rat?1:0), nc.Points[i] );

      WriteObjString(options, $"cstype{((is_rat) ? " rat " : " ")}bspline");
      WriteObjString(options, $"deg {nc.Order - 1}");

      var num_format = $"G{options.SignificantDigits}";
      String str;
      if (nc.Dimension == 2)
        str = "curv2";
      else
        str = $"curv {nc.Domain.Min.ToString(num_format, m_culture_info_en_us)} {nc.Domain.Max.ToString(num_format, m_culture_info_en_us)}";

      // write vertex indices
      for ( i = 0; unique_point_count > i; i++ )
        str = $"{str} {i + startv}";
      for (i = 0; point_count - unique_point_count > i; i++)
        str = $"{str} {i + startv}";

      WriteObjString(options, str);

      WriteObjKnotVector(options, 0, nc.Order, nc.Points.Count, 
                         nc.Knots.SuperfluousKnot(true),
                         nc.Knots.SuperfluousKnot(false), nc.Knots.ToArray());

      WriteObjString(options, "end");

      if( 2 == nc.Dimension )
        m_curv2_count++;
    }

    void WriteObjBrep( FileObjWriteOptions options, Brep brep)
    {
      int i;
      Brep v2_brep = brep.DuplicateBrep();
      v2_brep.MakeValidForV2();
      var facecount = brep.Faces.Count;

      for (i=0; facecount > i; i++)
      {
        BrepFace brepface = v2_brep.Faces[i];
        WriteObjBoundaryList(options, brepface);

        Surface surface = v2_brep.Surfaces[brepface.SurfaceIndex];
        if (null == surface)
          continue;

        WriteObjSurface(options, surface );

        WriteObjTrimList(options);

        WriteObjString(options, "end");
        m_trimcount=0;
      }
    }

    /* Write a list of trims for a face 
       This happens after the face is written to list the trim curves */
    void WriteObjTrimList(FileObjWriteOptions options)
    {
      int i;

      if ( m_trimcount < 1 )
        return;

      var curvecount = 0;

      if (options.UseRelativeIndexing)
      {
        for (i = 0; i < m_trimcount; i++)
          curvecount += m_trims[i].CurveCount;
      }

      var num_format = $"G{options.SignificantDigits}";
      for (i = 0; m_trimcount > i; i++) // for each trim loop
      {
        var str = (1 == m_trims[i].Outer) ? "hole" : "trim";
        for (int j = 0; m_trims[i].CurveCount > j; j++) // for each curve in the loop
        {
          str = $"{str} {m_trims[i].Domain[j][0].ToString(num_format, m_culture_info_en_us)} {m_trims[i].Domain[j][1].ToString(num_format, m_culture_info_en_us)} {(!options.UseRelativeIndexing ? m_trims[i].Curveindex[0] + j + 1 : curvecount)}";
          curvecount--;
        }
        WriteObjString(options, str);
      }
    }

    void WriteObjSurface(FileObjWriteOptions options, Surface surface)
    {
      int i;
      int vi, ki, startv;

      NurbsSurface nurbsurface = surface.ToNurbsSurface();
      if (null == nurbsurface)
        return;

      var is_rat = nurbsurface.IsRational?1:0;

      // get starting vertex index
      var point_count = nurbsurface.Points.CountU*nurbsurface.Points.CountV;
      if (options.UseRelativeIndexing) 
        startv = -point_count;
      else 
        startv = m_vindx+1;
  
      // write vertices (in Fortranic order)
      for ( vi = 0; nurbsurface.Points.CountV > vi ; vi++ )
      {
        for ( int ui = 0; nurbsurface.Points.CountU > ui; ui++ ) 
          WriteObjVertex(options, 3, is_rat, nurbsurface.Points.GetControlPoint(ui, vi) );
      }

      WriteObjString(options, $"cstype{((1 == is_rat) ? " rat " : " ")}bspline");
      WriteObjString(options, $"deg {nurbsurface.Degree(0)} {nurbsurface.Degree(1)}");

      nurbsurface.Domain(0);
      var num_format = $"G{options.SignificantDigits}";
      var str = $"surf {nurbsurface.Domain(0)[0].ToString(num_format, m_culture_info_en_us)} {nurbsurface.Domain(0)[1].ToString(num_format, m_culture_info_en_us)} {nurbsurface.Domain(1)[0].ToString(num_format, m_culture_info_en_us)} {nurbsurface.Domain(1)[1].ToString(num_format, m_culture_info_en_us)}";

      // write vertex indices
      for ( i = 0; i < point_count; i++ ) 
        str = $"{str} {i + startv}";

      WriteObjString(options, str);

      for ( ki = 0; 2 > ki; ki++ ) 
      {
        if (1 == ki)
        {
          WriteObjKnotVector(options,
                             1,
                             nurbsurface.OrderV, 
                             nurbsurface.Points.CountV, 
                             nurbsurface.KnotsV.SuperfluousKnot(true),
                             nurbsurface.KnotsV.SuperfluousKnot(false),
                             nurbsurface.KnotsV.ToArray());
        }
        else
        {
          WriteObjKnotVector(options,
                             0,
                             nurbsurface.OrderU,
                             nurbsurface.Points.CountU,
                             nurbsurface.KnotsU.SuperfluousKnot(true),
                             nurbsurface.KnotsU.SuperfluousKnot(false),
                             nurbsurface.KnotsU.ToArray());
        }
      }
    }

    void WriteObjBoundaryList(FileObjWriteOptions options, BrepFace brepface)
    {
      int i;

      var ntrims = brepface.Loops.Count;

      m_trims = new ObjTrim[ntrims];
      for (i = 0; ntrims > i; i++) // each boundary loop
      {
        m_trims[i] = new ObjTrim();
        if (null == m_trims[i])
          return;

        BrepLoop breploop = brepface.Loops[i];
        if (null == breploop)
          continue;

        m_trims[i].Outer = BrepLoopType.Outer == breploop.LoopType ? 0 : 1;
        WriteObjString(options, $"# - {(1 == m_trims[i].Outer ? "hole" : "trim")} loop");

        if (FileObjWriteOptions.CurveType.Polyline == options.TrimCurveType)
          WriteObjBoundaryAsPolyline(options, brepface, i);
        else
          WriteObjSpline(options, brepface, i);
        m_trimcount++;
      }
    }


    void WriteObjSpline(FileObjWriteOptions options, BrepFace brepface, int loopindex)
    {
      int i;

      BrepLoop breploop = brepface.Loops[loopindex];

      /* count the tedges in the whole boundary */
      var tecount = breploop.Trims.Count;

      m_trims[m_trimcount].Domain = new Interval[tecount];
      m_trims[m_trimcount].CurveCount = tecount; // # of curves in this loop
      m_trims[m_trimcount].Curveindex[0] = m_curv2_count;


      for (i = 0; tecount> i; i++)
      {
        /* this writes each tedge as a degree 1 polyline curv2 */
        BrepTrim breptrim = breploop.Trims[i];

        WriteObjNurb(options, breptrim); // increments curv2count

        /* store the domain of each spline (spline == tedge) in the boundary */
        m_trims[m_trimcount].Domain[i][0] = breptrim.Domain[0];
        m_trims[m_trimcount].Domain[i][1] = breptrim.Domain[1];
  
      }

      m_trims[m_trimcount].Curveindex[1] = m_curv2_count;
    }


    /* Write one boundary loop as a bunch of polylines (one for each tedge) */
    void WriteObjBoundaryAsPolyline(FileObjWriteOptions options, BrepFace brepface, int loopindex)
    {
      int i;
      BrepLoop breploop = brepface.Loops[loopindex];

      var tecount = breploop.Trims.Count;

      m_trims[m_trimcount].Domain = new Interval[tecount];
      m_trims[m_trimcount].CurveCount = tecount; // # of curves in this loop
      m_trims[m_trimcount].Curveindex[0] = m_curv2_count;

      for (i = 0; i < tecount; i++)
      {
        /* this writes each tedge as a degree 1 polyline curv2 */
        BrepTrim breptrim = breploop.Trims[i];
        if (null == breptrim)
          continue;

        
        PolylineCurve pl = breptrim.ToPolyline(0, 0,                // max & min segment count
                                               options.AngleTolRadians, // max angle
                                               ChordFromAngle(20),  // chord length
                                               0.0,                 // aspect
                                               0,                   // tolerance
                                               0.0,                 // min length
                                               0,                   // max length
                                               true);               // keep start point
        if (null == pl)
          return;

        WriteObjPolyline(options, pl); // increments curv2count

        m_trims[m_trimcount].Domain[i][0] = pl.Domain[0];
        m_trims[m_trimcount].Domain[i][1] = pl.Domain[1];

      }

      m_trims[m_trimcount].Curveindex[1] = m_curv2_count;
    }
  }
}