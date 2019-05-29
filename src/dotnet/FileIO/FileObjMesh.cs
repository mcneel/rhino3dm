using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Rhino.Geometry;
using Rhino.Geometry.Collections;
using Rhino.UI;

#pragma warning disable 1591

namespace Rhino.FileIO
{
  partial class FileObjReader
  {
    private class VtnComparer : IComparer<VTnIdx>
    {
      public int Compare(VTnIdx a, VTnIdx b)
      {
        if (null != a && null != b)
        {
          if (a.Vidx > b.Vidx)
            return 1;
          if (a.Vidx < b.Vidx)
            return -1;
          if (a.Nidx > b.Nidx)
            return 1;
          if (a.Nidx < b.Nidx)
            return -1;
          if (a.Tidx > b.Tidx)
            return 1;
          if (a.Tidx < b.Tidx)
            return -1;
          return 0;
        }
        return 0;
      }
    };

    static bool FixMesh(Mesh mesh, FileObjReadOptions options)
    {
      if (null == mesh)
        return false;

      //clean up crappy meshes
      if (0 < mesh.TextureCoordinates.Count && mesh.TextureCoordinates.Count != mesh.Vertices.Count)
        mesh.TextureCoordinates.Clear();

      // TODO figure out how to get at the surface parameters, if we care.  These get
      // set to the TCs on import I think.  Although I don't remember encountering them as I ported
      // the code that makes the TC's
      //if (0 < pMesh->m_S.Count() && pMesh->m_S.Count() != pMesh->m_V.Count())
      //  pMesh->m_S.SetCount(0);

      if (0 < mesh.Normals.Count && mesh.Normals.Count != mesh.Vertices.Count)
        mesh.Normals.Clear();

      mesh.Compact();

      if (0 == mesh.Faces.Count || 0 == mesh.Vertices.Count)
        return false;

      int ict = mesh.Normals.Count;
      for (int i=0; i < ict; i++)
      {
        if (!mesh.Normals[i].IsZero)
        {
          if (!mesh.Normals[i].IsUnitVector)
            mesh.Normals[i].Unitize();
          continue;
        }

        int[] vi =  mesh.TopologyVertices.MeshVertexIndices(mesh.TopologyVertices.TopologyVertexIndex(i));
        int j, jct = vi.Length;
        Vector3f norm = new Vector3f(0,0,0);
        for (j=0; jct > j; j++)
        {
          if (0 > vi[j] || ict <= vi[j])
            continue;

          norm += mesh.Normals[vi[j]];
        }

        norm.Unitize();
        if (norm.IsZero)
        {
          var x = mesh.Normals[i];
          x.Z = (float)1.0;
        }
        else
          mesh.Normals[i] = norm;
      }

      //Make vertex normals from face normals if they sucked (counts didn't match above) or didn't exist
      if (0 == mesh.Normals.Count|| options.MorphTargetOnly)
        mesh.Normals.ComputeNormals();

      //Flip closed meshes if they are inside out
      if (mesh.IsClosed)
      {
        double volume = mesh.Volume();
        if (0 > volume)
          mesh.Flip(true,  // vertex normals 
                    true,  // face normals
                    true); // face orientations
      }
       
      return true;
    }

    bool CheckAndRepairFace(ref MeshFace face, ref Point3d[] vertexArray)
    {
      if (null == face || null == vertexArray)
        return false;

      if (false == face.IsValidEx(ref vertexArray))
        return face.RepairEx(ref vertexArray);

      return true;
    }

    // ReSharper disable once FunctionComplexityOverflow
    bool MakeMesh(ReadObjGroupObject ogo)
    {
      if (null == ogo.Mesh || 0 == ogo.VtnCount)
        return false;

      // First sort and cull unique V/N/T combos.
      List<VTnIdx> tmp_array = new List<VTnIdx>(ogo.ObjvArray);

      VtnComparer vtn_comparer = new VtnComparer();
      tmp_array.Sort(vtn_comparer);
      ogo.ObjvArray.Clear();
      int i, ict = tmp_array.Count;
      VTnIdx curr_vtn = tmp_array[0];
      ogo.ObjvArray.Add(curr_vtn);
      for (i=1; ict>i; i++)
      {
        if (0 != vtn_comparer.Compare(curr_vtn, tmp_array[i]))
          ogo.ObjvArray.Add(tmp_array[i]);

        curr_vtn = tmp_array[i];
      }

      // Now append those combos to the mesh
      ict = ogo.ObjvArray.Count;
      int vct = m_v_array.Count;
      int tct = m_vt_array.Count;
      int nct = m_vn_array.Count;
      int cct = m_v_color_array.Count;
      for (i=0; ict>i; i++)
      {
        VTnIdx vtn = ogo.ObjvArray[i];

        if (0 <= vtn.Vidx && vct > vtn.Vidx) //has to be a valid vertex index
        {
          vtn.VArrayIdx = ogo.Mesh.Vertices.Count;
          ogo.Mesh.Vertices.Add(m_v_array[vtn.Vidx]);
        }
        else
        {
          //ON_Error(__FILE__, __LINE__, "Bogus vertex index.\n");
          //return false;
          continue;
        }

        if (0 < cct && m_b_file_has_vertex_colors)
        {
          if (0 <= vtn.Vidx && cct > vtn.Vidx)
            ogo.Mesh.VertexColors.Add(m_v_color_array[vtn.Vidx]);
          else
          {
            //ON_Error(__FILE__, __LINE__, "Bogus color index.\n");
            //return false;
          }
        }

        if (0 < nct)
        {
          if (0 <= vtn.Nidx && nct > vtn.Nidx)
            ogo.Mesh.Normals.Add(m_vn_array[vtn.Nidx]);
          else
          {
            //ON_Error(__FILE__, __LINE__, "Bogus normal index.\n");
            //return false;
            continue;
          }
        }

        if (0 < tct)
        {
          if (0 <= vtn.Tidx && tct > vtn.Tidx)
            ogo.Mesh.TextureCoordinates.Add(m_vt_array[vtn.Tidx]);
          else
          {
            //ON_Error(__FILE__, __LINE__, "Bogus texture index.\n");
            //return false;
            continue;
          }
        }
      }

      Point3d[] vertex_array = ogo.Mesh.Vertices.ToPoint3dArray();
      ict = ogo.FaceListList.Count;
      for (i=0; ict>i; i++)
      {
        m_f_array.Clear();

        List<VTnIdx> facearray = ogo.FaceListList[i];
        int j, jct = facearray.Count;
        for (j=0; jct>j; j++)
        {
          int idx = ogo.ObjvArray.BinarySearch(facearray[j], vtn_comparer);
          if (-1 != idx)
          {
            if (ogo.IsValid(idx, m_v_array.Count, m_vt_array.Count, m_vn_array.Count))
              m_f_array.Add(ogo.ObjvArray[idx].VArrayIdx);
          }
        }

        MeshFace face = new MeshFace(); 
        jct = m_f_array.Count;
        if (3 > jct)
          continue;
        
        if (3 == jct)
        {
          face.A = m_f_array[0];
          face.B = m_f_array[1];
          face.C = face.D = m_f_array[2];
          if (CheckAndRepairFace(ref face, ref vertex_array))
            ogo.Mesh.Faces.AddFace(face);
        }
        else if (4 == jct)
        {
          face.A = m_f_array[0];
          face.B = m_f_array[1];
          face.C = m_f_array[2];
          face.D = m_f_array[3];
          if (CheckAndRepairFace(ref face, ref vertex_array))
            ogo.Mesh.Faces.AddFace(face);
        }
        else
        {
          List<int> f_array = new List<int>(jct);

          Polyline pline = new Polyline();
          for (j=0; jct>j; j++)
            pline.Add(ogo.Mesh.Vertices[m_f_array[j]].X, ogo.Mesh.Vertices[m_f_array[j]].Y, ogo.Mesh.Vertices[m_f_array[j]].Z);

          // if the last index does not match the first index,
          // ie. closed, insert it at the beginning and insert
          // the last point at the beginning of the polyline
          if (m_f_array[0] != m_f_array.Last())
            pline.Add(ogo.Mesh.Vertices[m_f_array[0]].X, ogo.Mesh.Vertices[m_f_array[0]].Y, ogo.Mesh.Vertices[m_f_array[0]].Z);

      #if DEBUG
          //const bool b_add_polyline = false;
          //if (b_add_polyline)
          //  m_rhino_doc.Objects.AddPolyline(pline);
      #endif

          MeshFace[] faces = pline.TriangulateClosedPolyline();
          if (faces != null)
          {
            foreach (MeshFace mesh_face in faces)
            {
              ogo.Mesh.Faces.AddFace(m_f_array[mesh_face.A],m_f_array[mesh_face.B],m_f_array[mesh_face.C]);
              f_array.Add(ogo.Mesh.Faces.Count - 1);
            }

            MeshNgon ngon = MeshNgon.Create(m_f_array, f_array);
            ogo.Mesh.Ngons.AddNgon(ngon);
          }
        }
      }

      return true;
    }

    bool AddParameterSpaceVertex()
    {
      double d, weight = 1.0;
      Point2d vp = new Point2d();

      int ct = m_text_file.StringCount;

      if (3 > ct)
        return false;

      if (false == ConvertStringToDouble(m_text_file.GetString(1), out d))
        return false;
      vp.X = d;

      if (false == ConvertStringToDouble(m_text_file.GetString(2), out d))
        return false;
      vp.Y = d;


      if (4 == ct)
      {
        if (false == ConvertStringToDouble(m_text_file.GetString(3), out weight))
          return false;
      }

      m_vp_array.Add(vp);
      m_vp_weight_array.Add(weight);
      return true;
    }

    bool AddVertexTexture()
    {
      Point2f vt = new Point2f();

      if (3 > m_text_file.StringCount)
        return false;

      double d;
      if (false == ConvertStringToDouble(m_text_file.GetString(1), out d))
        return false;
      vt.X = (float)d;

      if (false == ConvertStringToDouble(m_text_file.GetString(2), out d))
        return false;
      vt.Y = (float)d;

      m_vt_array.Add(vt);
      return true;
    }

    bool AddVertexNormal()
    {
      Vector3f vn = new Vector3f();

      if (4 > m_text_file.StringCount)
        return false;

      double d;
      if (false == ConvertStringToDouble(m_text_file.GetString(1), out d))
        return false;
      vn.X = (float)d;
      if (false == ConvertStringToDouble(m_text_file.GetString(2), out d))
        return false;
      vn.Y = (float)d;
      if (false == ConvertStringToDouble(m_text_file.GetString(3), out d))
        return false;
      vn.Z = (float)d;

      m_vn_array.Add(vn);
      return true;
    }

    bool AddVertex()
    {
      double d, weight = 1.0;
      Point3d v = new Point3d();

      int ct = m_text_file.StringCount;
      if (4 > ct)
        return false;

      if (false == ConvertStringToDouble(m_text_file.GetString(1), out d))
        return false;
      v.X = d;

      if (false == ConvertStringToDouble(m_text_file.GetString(2), out d))
        return false;
      v.Y = d;

      if (false == ConvertStringToDouble(m_text_file.GetString(3), out d))
        return false;
      v.Z = d;

      if (5 == ct)
      {
        if (false == ConvertStringToDouble(m_text_file.GetString(4), out weight))
          return false;
      }

      if (7 == ct)
      {
        double red, green, blue;
        if (false == ConvertStringToDouble(m_text_file.GetString(4), out red))
          return false;
        if (false == ConvertStringToDouble(m_text_file.GetString(5), out green))
          return false;
        if (false == ConvertStringToDouble(m_text_file.GetString(6), out blue))
          return false;

        m_b_file_has_vertex_colors = true;

        m_v_color_array.Add((1.0 >= red && 1.0 >= green && 1.0 >= blue) ? Color.FromArgb((int) (red*255), (int) (green*255), (int) (blue*255)) : Color.FromArgb((int) red, (int) green, (int) blue));
      }

      m_v_array.Add(v);
      m_v_weight_array.Add(weight);
      return true;
    }

    bool AddPoints()
    {
      int i, vidx, ct = m_text_file.StringCount;

      if (2 > ct)
        return false;

      if (2 == ct)
      {
        if (false == ConvertStringToInt(m_text_file.GetString(1), out vidx))
          return false;
        if (false == ConvertIndex(ref vidx, m_v_array.Count))
          return false;
        m_current_ogo.PointArray.Add(m_v_array[vidx]);
    
        return true;
      }

      PointCloud ptcloud = new PointCloud();
      for (i=1; i<ct; i++)
      {
        if (false == ConvertStringToInt(m_text_file.GetString(i), out vidx))
          return false;
        if (false == ConvertIndex(ref vidx, m_v_array.Count))
          return false;
        ptcloud.Add(m_v_array[vidx]);
      }
      m_current_ogo.PointCloudArray.Add(ptcloud);

      return true;
    }

    int ParseFaceString(string str, out int vIdx, out int tIdx, out int nIdx, out int success)
    {
      //OBJ uses 1 based indexes so you can never get a 0 
      //even if it is a relative (negative) index
      vIdx = tIdx = nIdx = -1;

      string[] strarray = new string[3];

      int i, slash_ct = 0, ct = str.Length;
      success = 0;
      for (i=0; i < ct; i++)
      {
        if ('/' == str[i])
          slash_ct++;
        else
          strarray[slash_ct] += str[i];
      }

      if (false == String.IsNullOrEmpty(strarray[0]))
      {
        if (ConvertStringToInt(strarray[0], out vIdx))
        {
          if (false == ConvertIndex(ref vIdx, m_v_array.Count))
            success = 1;
        }
      }
      else
      {
        // You have to have at least a vertex, that's the rule
        success = 1;
      }

      if (0 == success && false == String.IsNullOrEmpty(strarray[1]))
      {
        if (ConvertStringToInt(strarray[1], out tIdx))
        {
          if (false == ConvertIndex(ref tIdx, m_vt_array.Count))
            success = 2;
        }
      }

      if (0 == success && false == String.IsNullOrEmpty(strarray[2]))
      {
        if (ConvertStringToInt(strarray[2], out nIdx))
        {
          if (false == ConvertIndex(ref nIdx, m_vn_array.Count))
            success = 3;
        }
      }

      return slash_ct + 1;
    }


    bool AddFace(FileObjReadOptions options)
    {
      if (m_current_ogo?.Mesh == null)
        return false;

      int i, ct = m_text_file.StringCount;

      if (4 > ct)
        return false; //must have at least a triangle

      List<VTnIdx> facearray = m_current_ogo.AddFaceList(ct);

      int v_idx, t_idx, n_idx, success;
      int init_ct = ParseFaceString(m_text_file.GetString(1), out v_idx, out t_idx, out n_idx, out success);
      if (0 >= init_ct)
        return false;

      for (i=1; i<ct; i++)
      {
        VTnIdx objv = m_current_ogo.AddVtnIdx();

        //all corners of a face must have the same number of components
        if (init_ct != ParseFaceString(m_text_file.GetString(i), out v_idx, out t_idx, out n_idx, out success))
          return false;

        if (0 != success)
        {
          if (m_max_bogus_face_ct > m_bogus_face_ct)
          {
            switch(success)
            {
              case 1:
                RhinoApp.WriteLine(
                  Localization.LocalizeString(
                    "Encountered mesh face with bogus vertex index (count is {0}). Search for \"{1}\" in your file.\n", 9),
                    m_v_array.Count, m_text_file.GetString(i));
               break;
              case 2:
                RhinoApp.WriteLine(
                  Localization.LocalizeString(
                    "Encountered mesh face with bogus texture index (count is {0}). Search for \"{1}\" in your file.\n", 10),
                    m_vt_array.Count, m_text_file.GetString(i));
               break;
              case 3:
                RhinoApp.WriteLine(
                  Localization.LocalizeString(
                    "Encountered mesh face with bogus normal index (count is {0}). Search for \"{1}\" in your file.\n", 11),
                    m_vn_array.Count, m_text_file.GetString(i));
               break;
            }

            m_bogus_face_ct++;
          }
          else
          {
            if (false == m_display_bogus_limit_prompt)
            {
              RhinoApp.WriteLine(
                Localization.LocalizeString(
                  "Encountered {0} bogus mesh faces. No longer displaying errors to command line.\n", 12),
                  m_bogus_face_ct);
              m_display_bogus_limit_prompt = true;
            }
          }
        }

        if (options.MorphTargetOnly)
        {
          objv.Vidx = v_idx;
          facearray.Add(objv);
        }
        else
        {
          objv.Vidx = v_idx;

          if (0 <= t_idx)
            objv.Tidx = t_idx;

          if (0 <= n_idx)
            objv.Nidx = n_idx;

          facearray.Add(objv);
        }
      }

      return true;
    }
  }

  internal partial class FileObjWriter
  {
		private void WriteObjVertex(FileObjWriteOptions options, Point3d point, Color color)
    {
      //This is to get rid of negative zeros in the ascii file
      Point3d tmp = new Point3d(point);
      if (Math.Abs(tmp.X) < RhinoMath.ZeroTolerance)
        tmp.X = 0.0;
      if (Math.Abs(tmp.Y) < RhinoMath.ZeroTolerance)
        tmp.Y = 0.0;
      if (Math.Abs(tmp.Z) < RhinoMath.ZeroTolerance)
        tmp.Z = 0.0;

      var num_format = $"G{options.SignificantDigits}";
      WriteObjString(options, $"v {tmp.X.ToString(num_format, m_culture_info_en_us)} {tmp.Y.ToString(num_format, m_culture_info_en_us)} {tmp.Z.ToString(num_format, m_culture_info_en_us)} {color.R.ToString(m_culture_info_en_us)} {color.G.ToString(m_culture_info_en_us)} {color.B.ToString(m_culture_info_en_us)}");
    }

		private void WriteObjVertex(FileObjWriteOptions options, Point3d point)
		{
			//This is to get rid of negative zeros in the ascii file
			Point3d tmp = new Point3d(point);
			if (Math.Abs(tmp.X) < RhinoMath.ZeroTolerance)
				tmp.X = 0.0;
			if (Math.Abs(tmp.Y) < RhinoMath.ZeroTolerance)
				tmp.Y = 0.0;
			if (Math.Abs(tmp.Z) < RhinoMath.ZeroTolerance)
				tmp.Z = 0.0;

      var num_format = $"G{options.SignificantDigits}";
      WriteObjString(options, $"v {tmp.X.ToString(num_format, m_culture_info_en_us)} {tmp.Y.ToString(num_format, m_culture_info_en_us)} {tmp.Z.ToString(num_format, m_culture_info_en_us)}");
		}

		private void WriteObjVertexNormal(FileObjWriteOptions options, Vector3d norm)
    {
      //This is to get rid of negative zeros in the ascii file
      Vector3d tmp = new Vector3d(norm);
      if (Math.Abs(tmp.X) < RhinoMath.ZeroTolerance)
        tmp.X = 0.0;
      if (Math.Abs(tmp.Y) < RhinoMath.ZeroTolerance)
        tmp.Y = 0.0;
      if (Math.Abs(tmp.Z) < RhinoMath.ZeroTolerance)
        tmp.Z = 0.0;

      var num_format = $"G{options.SignificantDigits}";
      WriteObjString(options, $"vn {tmp.X.ToString(num_format, m_culture_info_en_us)} {tmp.Y.ToString(num_format, m_culture_info_en_us)} {tmp.Z.ToString(num_format, m_culture_info_en_us)}");
    }

    private void WriteObjVertexTexture(FileObjWriteOptions options, Point2f tc)
    {
      //This is to get rid of negative zeros in the ascii file
      var tmp = new Geometry.Point(new Point3d(tc.X, tc.Y, 0.0));
      if (Math.Abs(tmp.Location.X) < RhinoMath.ZeroTolerance)
        tmp.Location = new Point3d(0.0, tmp.Location.Y, 0.0);
      if (Math.Abs(tmp.Location.Y) < RhinoMath.ZeroTolerance)
        tmp.Location = new Point3d(tmp.Location.X, 0.0, 0.0);

      if (m_b_use_uvw)
        tmp.Transform(m_uvw);

      var num_format = $"G{options.SignificantDigits}";
      WriteObjString(options, $"vt {tmp.Location.X.ToString(num_format, m_culture_info_en_us)} {tmp.Location.Y.ToString(num_format, m_culture_info_en_us)}");
    }


    private void WriteUnweldedObjRhinoMesh(FileObjWriteOptions options, Mesh mesh)
    {
      MeshFaceList faces = mesh.Faces;
      int numfaces = faces.Count;
      Point3d[] pts = mesh.Vertices.ToPoint3dArray();
      int ptCt = pts.Length;
      MeshVertexNormalList normals = null;
      if (options.ExportNormals)
      {
        if (null != mesh.Normals && ptCt == mesh.Normals.Count)
          normals = mesh.Normals;
      }
      MeshTextureCoordinateList uv = null;
      if (options.ExportTcs)
      {
        if (null != mesh.TextureCoordinates && ptCt == mesh.TextureCoordinates.Count)
          uv = mesh.TextureCoordinates;
      }

      int basevindx = m_vindx; // base vertex index;
      int basevtindx = m_vtindx; // base texture index
      int basevnindx = m_vnindx; // base normal index

      int i, j, jct;
      int[,] face_v_array = new int[numfaces, 4];
	    Color[] colors = mesh.VertexColors.ToArray();
	    bool b_has_v_cs = colors.Length == ptCt;
      for (i = 0; numfaces > i; i++)
      {
        MeshFace face = faces[i];
        jct = face.IsTriangle ? 3 : 4;
        for (j = 0; jct > j; j++)
        {
					if (b_has_v_cs && options.ExportVcs)
						WriteObjVertex(options, pts[face[j]], colors[face[j]]);
					else
						WriteObjVertex(options, pts[face[j]]);

					face_v_array[i, j] = ++basevindx;
        }
      }


      int[,] face_t_array = new int[numfaces, 4];
      if (null != uv)
      {
        for (i = 0; numfaces > i; i++)
        {
          MeshFace face = faces[i];
          jct = face.IsTriangle ? 3 : 4;
          for (j = 0; jct > j; j++)
          {
            WriteObjVertexTexture(options, uv[face[j]]);
            face_t_array[i, j] = ++basevtindx;
          }
        }
      }

      int[,] face_n_array = new int[numfaces, 4];
      if (null != normals)
      {
        for (i = 0; numfaces > i; i++)
        {
          MeshFace face = faces[i];
          jct = face.IsTriangle ? 3 : 4;
          for (j = 0; jct > j; j++)
          {
            WriteObjVertexNormal(options, normals[face[j]]);
            face_n_array[i, j] = ++basevnindx;
          }
        }
      }

      /* 3 or 4 sided facet */
      for (i = 0; i < numfaces; i++)
      {
        if (null != normals && null != uv)
        {
          WriteObjString(options,
            faces[i].IsTriangle
              ? $"f {face_v_array[i, 0]}/{face_t_array[i, 0]}/{face_n_array[i, 0]} {face_v_array[i, 1]}/{face_t_array[i, 1]}/{face_n_array[i, 1]} {face_v_array[i, 2]}/{face_t_array[i, 2]}/{face_n_array[i, 2]}"
              : $"f {face_v_array[i, 0]}/{face_t_array[i, 0]}/{face_n_array[i, 0]} {face_v_array[i, 1]}/{face_t_array[i, 1]}/{face_n_array[i, 1]} {face_v_array[i, 2]}/{face_t_array[i, 2]}/{face_n_array[i, 2]} {face_v_array[i, 3]}/{face_t_array[i, 3]}/{face_n_array[i, 3]}");
        }
        else if (null != normals)
        {
          WriteObjString(options,
            faces[i].IsTriangle
              ? $"f {face_v_array[i, 0]}//{face_n_array[i, 0]} {face_v_array[i, 1]}//{face_n_array[i, 1]} {face_v_array[i, 2]}//{face_n_array[i, 2]}"
              : $"f {face_v_array[i, 0]}//{face_n_array[i, 0]} {face_v_array[i, 1]}//{face_n_array[i, 1]} {face_v_array[i, 2]}//{face_n_array[i, 2]} {face_v_array[i, 3]}//{face_n_array[i, 3]}");
        }
        else if (null != uv)
        {
          WriteObjString(options,
            faces[i].IsTriangle
              ? $"f {face_v_array[i, 0]}/{face_t_array[i, 0]} {face_v_array[i, 1]}/{face_t_array[i, 1]} {face_v_array[i, 2]}/{face_t_array[i, 2]}"
              : $"f {face_v_array[i, 0]}/{face_t_array[i, 0]} {face_v_array[i, 1]}/{face_t_array[i, 1]} {face_v_array[i, 2]}/{face_t_array[i, 2]} {face_v_array[i, 3]}/{face_t_array[i, 3]}");
        }
        else
        {
          WriteObjString(options,
            faces[i].IsTriangle
              ? $"f {face_v_array[i, 0]} {face_v_array[i, 1]} {face_v_array[i, 2]}"
              : $"f {face_v_array[i, 0]} {face_v_array[i, 1]} {face_v_array[i, 2]} {face_v_array[i, 3]}");
        }
      }


      m_vindx = basevindx;
      m_vtindx = basevtindx;
      m_vnindx = basevnindx;
    }

    // ReSharper disable once FunctionComplexityOverflow
    private void WriteObjRhinoMesh(FileObjWriteOptions options, Mesh mesh, bool bWeldMesh)
    {
      int numpoints = mesh.Vertices.Count;
      MeshFaceList faces = mesh.Faces;
      int numfaces = faces.Count;
      bool b_export_normals = options.ExportNormals && null != mesh.Normals && numpoints == mesh.Normals.Count;

      bool b_export_tcs = options.ExportTcs && null != mesh.TextureCoordinates && numpoints == mesh.TextureCoordinates.Count;

      int pi, vi;
      int fi;

      var basevind = m_vindx;
      var basevtindx = m_vtindx;
      var basevnindx = m_vnindx;
      /* vertex 3d point */
      var vct = bWeldMesh ? mesh.TopologyVertices.Count : mesh.Vertices.Count;
      Point3d[] pts = mesh.Vertices.ToPoint3dArray();
			Color[] colors = mesh.VertexColors.ToArray();
			bool b_has_v_cs = colors.Length == pts.Length;
			for (pi = 0; vct > pi; pi++)
      {
        var idx = pi;
        if (bWeldMesh)
        {
          idx = mesh.TopologyVertices.MeshVertexIndices(pi)[0];
          if (0 > idx || numpoints <= idx)
            continue;
        }

				if (b_has_v_cs && options.ExportVcs)
					WriteObjVertex(options, pts[idx], colors[idx]);
				else
					WriteObjVertex(options, pts[idx]);
        basevind++;
      }


      /* vertex 2d texture */
      if (b_export_tcs)
      {
        for (vi = 0; numpoints > vi; vi++)
        {
          WriteObjVertexTexture(options, mesh.TextureCoordinates[vi]);
          basevtindx++;
        }
      }

      /* vertex 3d unit normal */
      if (b_export_normals)
      {
        int ni;
        for (ni = 0; numpoints > ni; ni++)
        {
          WriteObjVertexNormal(options, mesh.Normals[ni]);
          basevnindx++;
        }
      }

      bool b_n_gons_created = false;
      bool[] used_face_array = new bool[numfaces];
      if (options.CreateNgons)
      {
        // TODO - After Dale hooks up the ability to merge planar ngons that share an edge
        // TODO - and simplication of shared colinear boundaries call the appropriate function here.
        // TODO - see objFile.IncludeUnWeldedEdges and objFile.CullUnnecessaryVertexes

        double planar_tolerance = .01;
        if (null != m_rhino_doc)
          planar_tolerance = m_rhino_doc.ModelAbsoluteTolerance;

        if (planar_tolerance < 1.0e-4)
          planar_tolerance = 1.0e-4;
        else if (planar_tolerance > 0.1)
          planar_tolerance = 0.1;

        mesh.Ngons.AddPlanarNgons(planar_tolerance,         //planarTolerance
                                  3,                        //minimumNgonVertexCount
                                  options.MinNgonFaceCount, //minimumNgonFaceCount
                                  false);                   //allowHoles
        
        int mesh_ngon_count = mesh.Ngons.Count;
        if (0 < mesh_ngon_count)
        {
          //write out ngons
          for (fi = 0; mesh_ngon_count > fi; fi++)
          {
            MeshNgon ngon = mesh.Ngons[fi];
            if (null == ngon)
              continue;

            String fstr = "f";

            int act = ngon.m_vi.Length;
            for (vi = 0; act > vi; vi++)
            {
              String str;

              // Do not use loc_vidx for TCs or normals
              int abs_vidx = (int)ngon.m_vi[vi];
              int loc_vidx = bWeldMesh ? mesh.TopologyVertices.TopologyVertexIndex(abs_vidx) : abs_vidx; 
              if (b_export_normals && b_export_tcs)
                str = $"{loc_vidx + m_vindx + 1}/{abs_vidx + m_vtindx + 1}/{abs_vidx + m_vnindx + 1}";
              else if (b_export_normals)
                str = $"{loc_vidx + m_vindx + 1}//{abs_vidx + m_vnindx + 1}";
              else if (b_export_tcs)
                str = $"{loc_vidx + m_vindx + 1}/{abs_vidx + m_vtindx + 1}";
              else
                str = $"{loc_vidx + m_vindx + 1}";

              fstr = $"{fstr} {str}";
            }

            WriteObjString(options, fstr);

            int bct = ngon.m_fi.Length;
            for (vi = 0; bct > vi; vi++)
            {
              if (ngon.m_fi[vi] >= numfaces)
                continue; //bogus face index in ngon

              used_face_array[ngon.m_fi[vi]] = true;
            }
          }

          b_n_gons_created = true;
        }
      }

      int[] fp = new int[4], fv = new int[4], fn = new int[4];
      /* 3 or 4 sided facet */
      for (fi = 0; numfaces > fi; fi++)
      {
        if (b_n_gons_created && used_face_array[fi])
          continue;

        // fp = points, fn = normal, fv = texture
        /* this is for backward indexing
        fv[0] = fn[0] = fp[0] = faces[fi].a - numpoints;
        fv[1] = fn[1] = fp[1] = faces[fi].b - numpoints;
        fv[2] = fn[2] = fp[2] = faces[fi].c - numpoints;
        fv[3] = fn[3] = fp[3] = faces[fi].d - numpoints;
        *****/
        // use forward indexing from a base (works w/ crossroads)

        fp[0] = (bWeldMesh ? mesh.TopologyVertices.TopologyVertexIndex(faces[fi][0]) : faces[fi][0]) + m_vindx + 1;
        fp[1] = (bWeldMesh ? mesh.TopologyVertices.TopologyVertexIndex(faces[fi][1]) : faces[fi][1]) + m_vindx + 1;
        fp[2] = (bWeldMesh ? mesh.TopologyVertices.TopologyVertexIndex(faces[fi][2]) : faces[fi][2]) + m_vindx + 1;
        fp[3] = (bWeldMesh ? mesh.TopologyVertices.TopologyVertexIndex(faces[fi][3]) : faces[fi][3]) + m_vindx + 1;

        fn[0] = faces[fi][0] + m_vnindx + 1;
        fn[1] = faces[fi][1] + m_vnindx + 1;
        fn[2] = faces[fi][2] + m_vnindx + 1;
        fn[3] = faces[fi][3] + m_vnindx + 1;

        fv[0] = faces[fi][0] + m_vtindx + 1;
        fv[1] = faces[fi][1] + m_vtindx + 1;
        fv[2] = faces[fi][2] + m_vtindx + 1;
        fv[3] = faces[fi][3] + m_vtindx + 1;

        if (b_export_normals && b_export_tcs)
        {
          WriteObjString(options,
            faces[fi].IsTriangle
              ? $"f {fp[0]}/{fv[0]}/{fn[0]} {fp[1]}/{fv[1]}/{fn[1]} {fp[2]}/{fv[2]}/{fn[2]}"
              : $"f {fp[0]}/{fv[0]}/{fn[0]} {fp[1]}/{fv[1]}/{fn[1]} {fp[2]}/{fv[2]}/{fn[2]} {fp[3]}/{fv[3]}/{fn[3]}");
        }
        else if (b_export_normals)
        {
          WriteObjString(options,
            faces[fi].IsTriangle
              ? $"f {fp[0]}//{fn[0]} {fp[1]}//{fn[1]} {fp[2]}//{fn[2]}"
              : $"f {fp[0]}//{fn[0]} {fp[1]}//{fn[1]} {fp[2]}//{fn[2]} {fp[3]}//{fn[3]}");
        }
        else if (b_export_tcs)
        {
          WriteObjString(options,
            faces[fi].IsTriangle
              ? $"f {fp[0]}/{fv[0]} {fp[1]}/{fv[1]} {fp[2]}/{fv[2]}"
              : $"f {fp[0]}/{fv[0]} {fp[1]}/{fv[1]} {fp[2]}/{fv[2]} {fp[3]}/{fv[3]}");
        }
        else //(normals==NULL && uv==NULL)
        {
          WriteObjString(options, faces[fi].IsTriangle
              ? $"f {fp[0]} {fp[1]} {fp[2]}"
            : $"f {fp[0]} {fp[1]} {fp[2]} {fp[3]}");
        }
      }
      m_vindx = basevind; // base vertex index;
      m_vtindx = basevtindx; // base texture index
      m_vnindx = basevnindx; // base normal index
    }
  }
}