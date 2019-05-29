using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.UI;

namespace Rhino.FileIO
{
  internal partial class FileObjReader
  {
    static bool HasAlphaMap(ref Bitmap dib, out Bitmap alphaMap, out bool bIsSolid)
    {
      alphaMap = null;
      bIsSolid = true;

      bool alpha_exists;
      if (32 != GetBitMapDepth(dib, out alpha_exists))
        return false;

      Bitmap twenty_four_bit = new Bitmap(dib.Width, dib.Height, PixelFormat.Format24bppRgb);
      alphaMap = new Bitmap(dib.Width, dib.Height, PixelFormat.Format24bppRgb);

      alpha_exists = false;
      for (int i = 0;dib.Width>i; i++)
      {
        for (int j = 0; dib.Height>j; j++)
        {
          Color color = dib.GetPixel(i, j);
          alphaMap.SetPixel(i, j, Color.FromArgb(color.A, color.A, color.A));
          if (255 != color.A)
            alpha_exists = true;

          if ( (255 != color.R) && (255 != color.G) && (255 != color.B) )
            bIsSolid = false;

          twenty_four_bit.SetPixel(i, j, Color.FromArgb(color.R, color.G, color.B));
        }
      }

      if (!alpha_exists)
        alphaMap = null;

      dib = twenty_four_bit;

      return alpha_exists;
    }

    static int GetBitMapDepth(Bitmap bitmap, out bool bHasAlpha)
    {
      bHasAlpha = false;
      int depth = 0;

      switch (bitmap.PixelFormat)
      {
        case PixelFormat.Alpha:
        case PixelFormat.PAlpha:
          bHasAlpha = true;
          break;

        case PixelFormat.Format8bppIndexed:
          depth = 8;
          break;

        case PixelFormat.Format16bppGrayScale:
        case PixelFormat.Format16bppRgb555:
        case PixelFormat.Format16bppRgb565:
          depth = 16;
          break;

        case PixelFormat.Format16bppArgb1555:
          depth = 16;
          bHasAlpha = true;
          break;

        case PixelFormat.Format24bppRgb:
          depth = 24;
          break;

        case PixelFormat.Format32bppRgb:
          depth = 32;
          break;

          case PixelFormat.Format32bppArgb:
          case PixelFormat.Format32bppPArgb:
            depth = 32;
            bHasAlpha = true;
            break;

        case PixelFormat.Format48bppRgb:
          depth = 48;
          break;

        case PixelFormat.Format4bppIndexed:
          depth = 4;
          break;


        case PixelFormat.Format64bppArgb:
        case PixelFormat.Format64bppPArgb:
          depth = 64;
          bHasAlpha = true;
          break;
      }

      return depth;
    }

    static bool Split32BitTextures(Texture texDiff, Texture texAlpha)
    {
      bool rc = false;
      string orig_file_name = texDiff.FileName;
      FileInfo ofi = new FileInfo(orig_file_name);
      if (false == ofi.Exists)
        return false;

      Bitmap tex_diff_bitmap = (Bitmap)Image.FromFile(orig_file_name);
      bool has_alpha;
      int depth = GetBitMapDepth(tex_diff_bitmap, out has_alpha);

      string s_f_name = ofi.FullName.Substring(0, 0 <= ofi.FullName.IndexOf(ofi.Extension, StringComparison.Ordinal) ? ofi.FullName.IndexOf(ofi.Extension, StringComparison.Ordinal) : ofi.FullName.Length);
      texDiff.FileName = $"{s_f_name}_rhDiffuse.png";
      texAlpha.FileName = $"{s_f_name}_rhAlpha.png";
      texDiff.Enabled = false;
      texAlpha.Enabled = false;

      Bitmap alpha_map;
      bool is_solid;
      if ( HasAlphaMap( ref tex_diff_bitmap, out alpha_map, out is_solid ) )
      {
        if ( null != alpha_map )
        {
          alpha_map.Save(texAlpha.FileName);
          texAlpha.Enabled = true;
          texAlpha.TextureType = TextureType.Transparency;
        }

        if (!is_solid)
        {
          tex_diff_bitmap.Save(texDiff.FileName);
          texDiff.Enabled = true;
        }

        rc = (texDiff.Enabled || texAlpha.Enabled);
      }
      else if (!is_solid && (depth != GetBitMapDepth(tex_diff_bitmap, out has_alpha)))
      {
        tex_diff_bitmap.Save(texDiff.FileName);
        FileInfo fi = new FileInfo(texDiff.FileName);
        if (false == fi.Exists)
          texDiff.FileName = orig_file_name;

        texDiff.Enabled = true;
      }
  
      return rc;
    }

    bool FixFilePath(ref string filename)
    {
      FileInfo obj_fi = new FileInfo(m_filename);
      if (false == obj_fi.Exists)
        return false; // should never happen

      FileInfo mtl_fi = new FileInfo(filename);

      //string newpath = $"{obj_fi.DirectoryName}{Path.DirectorySeparatorChar}{mtl_fi.Name}";
      if (null == obj_fi.DirectoryName)
        return false;
      string newpath = Path.Combine (obj_fi.DirectoryName, mtl_fi.Name);

      mtl_fi = new FileInfo(newpath);
      if (false == mtl_fi.Exists)
      {

        newpath = RhinoDoc.ActiveDoc.FindFile(newpath);
        if (String.IsNullOrEmpty(newpath))
          return false;

        mtl_fi = new FileInfo(newpath);
        if (false == mtl_fi.Exists)
          return false;
      }

      filename = newpath;
      return true;
    }

    static bool ConvertStringToColorNumber(string input, out double d)
    {
      d = 0.0;
      int i;

      if (ConvertStringToInt(input, out i) && -1 != i)
      {
        d = i*255;
        return true;
      }

      if (false == ConvertStringToDouble(input, out d))
        return false;

      // TODO Determine what should be done when a file has 
      //numbers ranging from 0.0-1.0 and 0.0-2.55

      if (0.0 <= d && 1.0 >= d)
      {
        d *= 255.0;
        return true;
      }

      if (0.0 <= d && 2.55 >= d)
      {
        d *= 100.0;
        return true;
      }

      return false;
    }

    static bool GetColor(ObjTextFile tf, out Color color)
    {
      var ct = tf.StringCount;
      color = Color.White;

      //Must have at last 3 strings including the "K" or "T" label
      if (3 > ct)
        return false;

      if (StringsMatch(tf.GetString(1), "xyz"))
      {
        //TODO x y z are the values of the ClEXYZ color space.
        //See Manual.
        return true;
      }

      if (StringsMatch(tf.GetString(1), "spectral"))
      {
        //TODO reflectivity specified by a spetral curve, TF.String(2) is the filename.rfl
        //See Manual.
        return true;
      }

      //By this point assume it is an RGB value
      if (4 != ct)
        return false;

      double d;

      if (false == ConvertStringToColorNumber(tf.GetString(1), out d))
        return false;
      int red = (int)d;

      if (false == ConvertStringToColorNumber(tf.GetString(2), out d))
        return false;
      int green = (int)d;

      if (false == ConvertStringToColorNumber(tf.GetString(3), out d))
        return false;
      int blue = (int)d;

      color = Color.FromArgb(red, green, blue);
      return true;
    }

    bool GetBump(ObjTextFile tf, string str, out Texture tex)
    {
      tex = new Texture();
      double bc = 0.5;
      Color bco = Color.White;
      bool b_made_bump_color = false;
      int i;
      for (i = 1; tf.StringCount > i; i++)
      {
        //check for options here
        if (StringsMatch(tf.GetString(i), "-bm"))
        {
          i++;

          //See if this string has a comma in it.  As far as I can tell from the OBJ spec
          //it's not supposed to but the files I got from Holger do.  If it does,
          //split it into 2 strings.
          string bmstr = tf.GetString(i);
          int idx = bmstr.IndexOf(',');
          if (-1 != idx)
          {
            double tmp;
            if (ConvertStringToDouble(bmstr.Substring(0, idx), out tmp))
              bc = tmp;

            int bcoi;
            if (ConvertStringToInt(bmstr.Substring(idx + 1), out bcoi))
            {
              bco = Color.FromArgb(bcoi);
              b_made_bump_color = true;
            }
          }
          else
            ConvertStringToDouble(bmstr, out bc);

          str = bmstr; // if we get a number set str to that string so GetTexturePath will read beyond it

          //if bc is greater than 1 turn it into a percentage
          if (1.0 <= bc)
            bc /= 100.0;
          break;
        }
      }

      string texfile;
      if (tf.GetTexturePath(this, str, out texfile))
      {
        tex.FileName = texfile;
        tex.TextureType = TextureType.Bump;

        tex.SetAlphaBlendValues(bc, 1.0, 1.0, 0.0, 0.0);
        // See ON_Texture::Default() for corresponding values, m_blend_constant_A and m_blend_A[]
        if (b_made_bump_color)
          tex.SetRGBBlendValues(bco, 1.0, 1.0, 0.0, 0.0);
        // See ON_Texture::Default() for corresponding values, m_blend_constant_RGB and m_blend_RGB[]
        return true;
      }
 
      return false;
    }



  // ReSharper disable once FunctionComplexityOverflow
  void AddMaterialsToDoc(FileObjReadOptions options)
    {
      int ct = m_text_file.StringCount;
      if (2 > ct)
        return;

      string filepath;

      ObjTextFile tf = null;
      if (m_text_file.GetTexturePath(this, m_text_file.GetString(0), out filepath))
        tf = new ObjTextFile(filepath);

      if (null == tf || false == tf.IsValid)
      {
        //Still couldn't figure it out.  Time to bail.
        RhinoApp.WriteLine(Localization.LocalizeString("Unable to find or open {0}.  Materials will be ignored.\n", 8), filepath);
        return;
      }

      bool rc = true;
      Material mat = Material.DefaultMaterial;
      while (rc && false == tf.FileEnd)
      {
        if (false == tf.GetNextObj() || 0 == tf.StringCount)
          continue;

        var str = tf.GetString(0);

        Color color;
        double number;
        int i;
        switch (str[0])
        {
          case 'k':
          case 'K':
            {
              if (1 < str.Length)
              {
                switch (str[1])
                {
                  case 'a':
                  case 'A':
                    {
                      //Ambient reflectivity
                      rc = GetColor(tf, out color);
                      if (rc)
                        mat.AmbientColor = color;
                      break;
                    }
                  case 'd':
                  case 'D':
                    {
                      //Diffuse reflectivity
                      rc = GetColor(tf, out color);
                      if (rc)
                        mat.DiffuseColor = color;
                      break;
                    }
                  case 's':
                  case 'S':
                    {
                      //Specular reflectivity
                      rc = GetColor(tf, out color);
                      if (rc)
                        mat.SpecularColor = color;
                      break;
                    }
                  default:
                    {
                      break;
                    }
                } //end of switch
              }
              break;
            }

          case 'r':
          case 'R':
            {
              if (StringsMatch(str, "refl"))
              {
                if (4 <= tf.StringCount)
                {
                  // TODO it's also possible to have offet and scaling options
                  // but they're not required.  At this point I've never even seen a refl
                  // so just hanldle -type sphere, which are required.
                  if (StringsMatch(tf.GetString(1), "-type"))
                  {
                    if (StringsMatch(tf.GetString(2), "sphere"))
                    {
                      //environment map filename
                      string texfile;

                      Texture tex = new Texture();
                      if (tf.GetTexturePath(this, tf.GetString(2), out texfile))
                      {
                        tex.FileName = texfile;
                        mat.SetEnvironmentTexture(tex);
                      }
                    }
                  }
                }
              }

              break;
            }

          case 't':
          case 'T':
            {
              if (1 < str.Length)
              {
                switch (str[1])
                {
                  case 'f':
                  case 'F':
                    {
                      //Transmission filter
                      rc = GetColor(tf, out color);
                      if (rc)
                        mat.TransparentColor = color;
                      break;
                    }
                  default:
                    {
                      break;
                    }
                } //end of switch
              }
              break;
            }

          case 'd':
          case 'D':
            {
              if (1 == str.Length)
              {
                //Dissolve (transparency)
                if (2 > tf.StringCount)
                  continue;
                if (ConvertStringToDouble(tf.GetString(1), out number))
                  mat.Transparency = 1.0 - number;
              }
              else if (StringsMatch(str, "decal"))
              {
                //TODO
                //decal filename


              }
              break;
            }

          case 'n':
          case 'N':
            {
              if (1 < str.Length)
              {
                switch (str[1])
                {
                  case 's':
                  case 'S':
                    {
                      //Specular Exponent
                      if (2 > tf.StringCount)
                        continue;
                      if (ConvertStringToDouble(tf.GetString(1), out number))
                      {
                        if (number > 100.0)
                          mat.Shine = number / 1000 * Material.MaxShine;
                        //the obj/mtl book suggest this value is between 0 and 1000
                        else
                          mat.Shine = number / 100 * Material.MaxShine;
                        //but we're going to assume that any value < 100 means between 0 and 100. 
                      }
                      break;
                    }
                  case 'i':
                  case 'I':
                  case 'l':
                  case 'L': //Can't read the photocopy and I'm not sure if it's and I or an l.
                    {
                      //Optical Density
                      break;
                    }
                  default:
                    {
                      break;
                    }
                } //end of switch
              }

              //NewMaterial
              if (StringsMatch(str, "newmtl"))
              {
                if (2 > tf.StringCount)
                  continue;

                //only add the material once it has been named.
                //It looks like that's supposed to happen first but I can't tell for sure
                //with the documentation we have.
                // 18 Dec 2017 S. Baer (RH-43164)
                // Make sure the material is not already in the dictionary
                if (!String.IsNullOrEmpty(mat.Name) && !m_material_indices.ContainsKey(mat.Name))
                  m_material_indices.Add(mat.Name, m_rhino_doc.Materials.Add(mat)); //This actually adds the last material when the next newmtl is found

                mat = Material.DefaultMaterial;

                string tmpstr = "";
                for (i = 1; tf.StringCount > i; i++)
                  tmpstr = $"{tmpstr} {tf.GetString(i)}";

                mat.Name = tmpstr;
              }

              break;
            }

          case 'b':
          case 'B':
            {
              if (StringsMatch(str, "bump"))
              {
                //bumpmap filename
                Texture tex;
                if (GetBump(tf, str, out tex))
                  mat.SetBumpTexture(tex);
              }
              break;
            }

          case 'm':
          case 'M':
            {
              if (StringsMatch(str, "map_Ka"))
              {
                //ambient filename

              }
              else if (StringsMatch(str, "map_Kd"))
              {
                //diffuse filename
                string texfile;
                if (tf.GetTexturePath(this, str, out texfile))
                {
                  Texture tex = new Texture { FileName = texfile };
                  tex.TextureType = TextureType.Bitmap;
                  tex.TextureCombineMode = TextureCombineMode.Blend;

                  if (options.Split32BitTextures)
                  {
                    Texture tex_alpha = new Texture();

                    if (Split32BitTextures(tex, tex_alpha))
                    {
                      if (tex.Enabled)
                        mat.SetBitmapTexture(tex);

                      if (tex_alpha.Enabled)
                      {
                        //TODO should this be a transparency texture or something else?
                        //See dissolve below.
                        mat.SetTransparencyTexture(tex_alpha);
                      }
                    }
                    else
                      mat.SetBitmapTexture(tex);
                  }
                  else
                    mat.SetBitmapTexture(tex);
                }
              }
              else if (StringsMatch(str, "map_Ks"))
              {
                //specular reflectivity filename

              }
              else if (StringsMatch(str, "map_Ns"))
              {
                //specular exponent filename

              }
              else if (StringsMatch(str, "map_d"))
              {
                //dissolve filename
                string filename;
                if (tf.GetTexturePath(this, str, out filename))
                {
                  Texture tex = new Texture { FileName = filename };
                  tex.TextureType = TextureType.Transparency;
                  mat.SetTransparencyTexture(tex);
                }
              }
              else if (StringsMatch(str, "map_Bump"))
              {
                //bumpmap filename
                Texture tex;
                if (GetBump(tf, str, out tex))
                  mat.SetBumpTexture(tex);
              }
              break;
            }

          //default:
          //    break;
        }//end of switch
      }//end of while


      //last material was probably not added.
      //check and then add it if needed.
      // 18 Dec 2017 S. Baer (RH-43164)
      // Make sure the material is not already in the dictionary
      if (!String.IsNullOrEmpty(mat.Name) && !m_material_indices.ContainsKey(mat.Name))
        m_material_indices.Add(mat.Name, m_rhino_doc.Materials.Add(mat));
    }

    private readonly Dictionary<string, int> m_material_indices = new Dictionary<string, int>();

    private static bool g_import_al_type_objs;
    bool m_b_reset = true;

    private class OgoComparer : IComparer<ReadObjGroupObject>
    {
      public int Compare(ReadObjGroupObject a, ReadObjGroupObject b)
      {

        if (null != a && null != b)
        {
          int rc = String.Compare(a.GName, b.GName, StringComparison.Ordinal);
          if (0 != rc)
            return rc;

          rc = String.Compare(a.OName, b.OName, StringComparison.Ordinal);
          if (0 != rc)
            return rc;

          if (false == g_import_al_type_objs)
          {
            rc = String.Compare(a.MName, b.MName, StringComparison.Ordinal);
            if (0 != rc)
              return rc;
          }

          return rc;
        }

        return 0;
      }
    };


    bool SetOgo(string gname, string oname, string mname)
    {
      g_import_al_type_objs = m_b_import_al_type_objs;
      m_current_ogo = null;

      ReadObjGroupObject ogo = new ReadObjGroupObject {GName = gname, OName = oname, MName = mname};

      OgoComparer sort_ogo = new OgoComparer();
      int idx = m_obj_array.BinarySearch(ogo, sort_ogo);
      if (0 <= idx && m_obj_array.Count > idx)
      {
        m_current_ogo = m_obj_array[idx];
        if (m_b_import_al_type_objs)
        {
          if (null != mname && 0 != mname[0])
            m_current_ogo.MName = mname;
        }

        m_b_reset = true;
        return true;
      }

      if (false == m_b_reset)
      {
        m_b_reset = true;
        return false;
      }


      m_obj_array.Add(ogo);
      m_current_ogo = m_obj_array.Last();
      if (null != m_current_ogo)
      {
        m_current_ogo.GName = gname;
        m_current_ogo.OName = oname;
        m_current_ogo.MName = mname;
      }

      m_obj_array.Sort(sort_ogo);
      m_b_reset = false;
      return SetOgo(gname, oname, mname);
    }

    
    bool AddorSetGroup(FileObjReadOptions options)
    {
      if (options.MorphTargetOnly)
      {
        if (null == m_current_ogo)
        {
          SetOgo(m_group_name, m_object_name, m_material_name);
          if (null  == m_current_ogo)
            return false;
        }

        return true;
      }

      SetOgo(m_group_name, m_object_name, m_material_name);

      if (null == m_current_ogo)
        return false;

      if (!m_current_ogo.GroupNames.Any())
      {
        int i, ct = m_group_names.Count;
        for (i = 0; i < ct; i++)
          m_current_ogo.GroupNames.Add(m_group_names[i]);
      }

      return true;
    }
  }


  internal partial class FileObjWriter
  {
    private Transform m_uvw;
    private bool m_b_use_uvw;

    private bool GetUvwTransform(Texture[] textures, out Transform uvw)
    {
      // This function's purpose is to try setting uvw to the most appropriate
      // transform based on the various textures on a material that have an
      // offset, repeat, rotate tranform associated with them.  Diffuse trumps all others
      // if it is different than any other.

      uvw = Transform.Identity;
      int i, ict = textures.Length;
      for (i = 0; ict > i; i++)
      {
        switch (textures[i].TextureType)
        {
          case TextureType.Bitmap:
          {
            if (textures[i].UvwTransform.IsValid && !textures[i].UvwTransform.IsIdentity)
            {
              //If there is a transform on the bitmap texture use it and bail out early
              uvw = textures[i].UvwTransform;
              i = ict;
            }
            break;
          }

          case TextureType.Transparency:
          case TextureType.Bump:
          case TextureType.None:
          {
            // If there was no bitmp texture transfom use the transform(s) from the other textures,
            // but only if they're the same.
            if (!textures[i].UvwTransform.IsValid || textures[i].UvwTransform.IsIdentity)
              break;

            if (uvw.IsIdentity)
            {
              uvw = textures[i].UvwTransform;
              break;
            }

            if (uvw != textures[i].UvwTransform)
            {
              uvw = Transform.Identity;
              return false;
            }

            break;
          }
        }
      }

      return !uvw.IsIdentity;
    }

    private void WriteObjGroupAndMat(FileObjWriteOptions options, StreamWriter matfile, String filepath, RhinoDoc doc, ObjectAttributes attribs)
    {
      if (null == attribs)
        return;

      String s_group_name = null;
      String s_object_name = attribs.Name;
      String s_layer_name = "";
      Material tmpmat = new Material();
      if (null != doc)
      {
				if (0 <= doc.Materials.CurrentMaterialIndex && doc.Materials.Count > doc.Materials.CurrentMaterialIndex)
					tmpmat = doc.Materials[doc.Materials.CurrentMaterialIndex];

	      Layer tmplayer = new Layer();
	      if (0 <= doc.Layers.CurrentLayerIndex && doc.Layers.Count > doc.Layers.CurrentLayerIndex)
		      tmplayer = doc.Layers[doc.Layers.CurrentLayerIndex];

				if (0 <= attribs.LayerIndex && doc.Layers.Count > attribs.LayerIndex)
					tmplayer = doc.Layers[attribs.LayerIndex];

				s_layer_name = tmplayer.FullPath;

        int mat_index = attribs.MaterialIndex;
	      if (ObjectMaterialSource.MaterialFromLayer == attribs.MaterialSource)
		      mat_index = tmplayer.RenderMaterialIndex;

				if (0 <= mat_index && doc.Materials.Count > mat_index)
					tmpmat = doc.Materials[mat_index];

        if (-1 == mat_index)
        {
          // October 13, 2013 Tim - Fix for RH-24602
          // if the material index is -1 then the object (or layer) is using 
          // the default render material.  In this case, set the diffuse
          // color in the output to the display color of the object.

          Color display = attribs.DrawColor(doc);
	        if (ObjectColorSource.ColorFromLayer == attribs.ColorSource)
							display = tmplayer.Color;

          tmpmat.DiffuseColor = display;
          try
          {
            tmpmat.Name = $"diffuse_{display.ToKnownColor().ToString ()}";
          }
          catch
          {
            // this try/catch is here because ToKnownColor throws a NotImplementedException on the Mac
            tmpmat.Name = $"diffuse_{display.R.ToString ()}_{display.G.ToString ()}_{display.B.ToString ()}_{display.A.ToString ()}";
          }
        }

        // we want all of the group names on the g line so the nesting works properly
        int gidx = 0, gct = attribs.GroupCount;
        while (gct > gidx)
        {
          string tmp = doc.Groups.GroupName(attribs.GetGroupList()[gidx++]);
          MakeUnderbarString(ref tmp);
          s_group_name = null == s_group_name ? tmp : $"{s_group_name} {tmp}";
        }
      }

      WriteObjGroupName(options, s_layer_name, s_object_name, s_group_name);

      if (false == options.ExportMaterialDefinitions || null == matfile)
        return;

      String tmpmatstr;
      bool b_add_to_mt_lfile = GetMatName(tmpmat, options.UnderbarMaterialNames, out tmpmatstr);

      String materialname = $"usemtl {tmpmatstr}";
      WriteObjString(options, materialname);

      if (false == b_add_to_mt_lfile)
				return;

      matfile.Write($"newmtl {tmpmatstr}");
      WriteObjeol(options, matfile);

      tmpmatstr = $"Ka {(tmpmat.AmbientColor.R / 255.0).ToString("F4", m_culture_info_en_us)} {(tmpmat.AmbientColor.G/255.0).ToString("F4", m_culture_info_en_us)} {(tmpmat.AmbientColor.B/255.0).ToString("F4", m_culture_info_en_us)}"; 
      matfile.Write(tmpmatstr);
      WriteObjeol(options, matfile);

      tmpmatstr = $"Kd {(tmpmat.DiffuseColor.R / 255.0).ToString("F4", m_culture_info_en_us)} {(tmpmat.DiffuseColor.G / 255.0).ToString("F4", m_culture_info_en_us)} {(tmpmat.DiffuseColor.B / 255.0).ToString("F4", m_culture_info_en_us)}";
      matfile.Write(tmpmatstr);
      WriteObjeol(options, matfile);

      tmpmatstr = $"Ks {(tmpmat.SpecularColor.R / 255.0).ToString("F4", m_culture_info_en_us)} {(tmpmat.SpecularColor.G / 255.0).ToString("F4", m_culture_info_en_us)} {(tmpmat.SpecularColor.B / 255.0).ToString("F4", m_culture_info_en_us)}";
      matfile.Write(tmpmatstr);
      WriteObjeol(options, matfile);

      tmpmatstr = $"Tf {(tmpmat.EmissionColor.R / 255.0).ToString("F4", m_culture_info_en_us)} {(tmpmat.EmissionColor.G / 255.0).ToString("F4", m_culture_info_en_us)} {(tmpmat.EmissionColor.B / 255.0).ToString("F4", m_culture_info_en_us)}";
      matfile.Write(tmpmatstr);
      WriteObjeol(options, matfile);

      tmpmatstr = $"d {(1.0 - tmpmat.Transparency).ToString("F4", m_culture_info_en_us)}";
      matfile.Write(tmpmatstr);
      WriteObjeol(options, matfile);

      tmpmatstr = $"Ns {tmpmat.Shine.ToString("F4", m_culture_info_en_us)}";
      matfile.Write(tmpmatstr);
      WriteObjeol(options, matfile);

      m_b_use_uvw = GetUvwTransform(tmpmat.GetTextures(), out m_uvw);

      Texture bitmaptexture = tmpmat.GetBitmapTexture();
      if (null != bitmaptexture)
      {
        String name;
        if (bitmaptexture.Enabled && MakeOBJTexture(bitmaptexture, doc, filepath, out name) &&
            0 < name.Length)
        {
          matfile.Write($"map_Kd {name}");
          WriteObjeol(options, matfile);
        }

        // TODO This needs to be figured out when m_transparency_texture_id is hooked up.  See comments in 
        // opennurbs_texture.cs regarding m_transparency_texture_id
        //if (true == ON_UuidIsNotNil(bitmaptexture.m_transparency_texture_id))
        //{
        //  ti = tmpmat.FindTexture(bitmaptexture.m_transparency_texture_id);
        //  if (0 <= ti)
        //  {
        //    const ON_Texture& transtexture = tmpmat.m_textures[ti];
        //    //fixed 29 12 2011
        //    if (true == transtexture.m_bOn && true == MakeOBJTexture(transtexture, doc, filepath, name) && FALSE == name.IsEmpty())
        //    {
        //      rc = fprintf(matfp, "map_D %s", (LPCSTR)name);
        //      rc = WriteOBJEOL( matfp, myOBJfile );
        //      if (!rc)
        //        return rc;
        //    }
        //  }
        //}
      }

      Texture bumptexture = tmpmat.GetBumpTexture();
      if (null != bumptexture)
      {
        String name;
        if (bumptexture.Enabled && MakeOBJTexture(bumptexture, doc, filepath, out name) &&
            0 < name.Length)
        {
          matfile.Write($"map_Bump {name}");
          WriteObjeol(options, matfile);
        }

        // TODO This needs to be figured out when m_transparency_texture_id is hooked up.  See comments in 
        // opennurbs_texture.cs regarding m_transparency_texture_id
        //if (true == ON_UuidIsNotNil(bumptexture.m_transparency_texture_id))
        //{
        //  ti = tmpmat.FindTexture(bumptexture.m_transparency_texture_id);
        //  if (0 <= ti)
        //  {
        //    const ON_Texture& transtexture = tmpmat.m_textures[ti];
        //    //fixed 29 12 2011
        //    if (true == transtexture.m_bOn && true == MakeOBJTexture(transtexture, doc, filepath, name) && FALSE == name.IsEmpty())
        //    {
        //      rc = fprintf(matfp, "map_D %s", (LPCSTR)name);
        //      rc = WriteOBJEOL( matfp, myOBJfile );
        //      if (!rc)
        //        return rc;
        //    }
        //  }
        //}
      }

      Texture transtexture = tmpmat.GetTransparencyTexture();
      if (null != transtexture)
      {
        String name;
        if (transtexture.Enabled && MakeOBJTexture(transtexture, doc, filepath, out name) &&
            0 < name.Length)
        {
          matfile.Write($"map_D {name}");
          WriteObjeol(options, matfile);
        }
      }

      Texture emaptexture = tmpmat.GetEnvironmentTexture();
      if (null != emaptexture)
      {
        String name;
        if (emaptexture.Enabled && MakeOBJTexture(emaptexture, doc, filepath, out name) &&
            0 < name.Length)
        {
          matfile.Write($"refl -type sphere {name}");
          WriteObjeol(options, matfile);
        }
      }
    }

    private bool MakeOBJTexture(Texture texture, RhinoDoc doc, String filepath, out String name)
    {
      name = "";

      if (0 == texture.FileName.Length)
        return false;

      String oldfilename = Path.GetFileName(texture.FileName);
      String newfilename = $"{filepath}\\{oldfilename}";

      String texturepath = Path.GetDirectoryName(texture.FileName);
      if (string.IsNullOrEmpty(texturepath))
      {
        //if we got in here assume the texture path is relative, get the drive and path from
        //the current open 3dm file
        oldfilename = $"{filepath}\\{oldfilename}";
      }
      else
        oldfilename = $"{texturepath}\\{oldfilename}";

// TODO determine if this archaic short path stuff is necessary anymore
// If it is, see comments at the bottom of 
// https://msdn.microsoft.com/en-us/library/windows/desktop/aa364989%28v=vs.85%29.aspx
// to hook it up

#if TODO_FIGURE_THIS_OUT
    #if ON_RUNTIME_WIN
      wchar_t shortpath[500];
  
      if (! CRhinoFileUtilities::FileExists (newfilename))
      {
        //if the file doesn't already exist, make it, or GetShortPathNameW
        //below will fail.
        FILE* ptmp = _wfopen(newfilename, L"wt"); 
        if (0 == ptmp)
          return false;
        fclose(ptmp);
        if (0 == GetShortPathName(newfilename, shortpath, sizeof(shortpath)/sizeof(shortpath[0])-1))
          return false;
        _wunlink(newfilename);
      }
      else
      {
        if (0 == GetShortPathName(newfilename, shortpath, sizeof(shortpath)/sizeof(shortpath[0])-1))
          return false;
      }

      ON_wString::SplitPath(shortpath, &drive, &dir, &fname, &ext);
      if (9 < ON_wString(fname).Length() || 4 < ON_wString(ext).Length()) //account for null on end
        return false;
    #endif
  
    #if ON_RUNTIME_APPLE
      ON_wString::SplitPath(newfilename, &drive, &dir, &fname, &ext);
    #endif

      newfilename = drive + dir + fname + ext;
      name = fname + ext;
#endif

      name = Path.GetFileName(newfilename);

      //Figure out if the texture filename exists in the path of the 3ds filename
      //if it doesn't copy/make it there
      FileInfo newfile = new FileInfo(newfilename);
      FileInfo oldfile = new FileInfo(oldfilename);
      if (false == newfile.Exists)
      {
        if (oldfile.Exists)
        {
          //copy the file if it exists
          newfile = oldfile.CopyTo(newfilename);
          if (false == newfile.Exists)
            return false;
        }
        else
        {
          //look in bitmap table for the file
          String tmp;
          BitmapEntry bitmap = doc.Bitmaps.Find(texture.FileName, false, out tmp);
          if (null == bitmap)
            return false;

          if (false == bitmap.Save(newfilename))
            return false;
        }
      }

      return true;
    }

    private void WriteObjGroupName(FileObjWriteOptions options, String sLayerName, String sObjectName, String sGroupName)
    {
      String str_object_name = sObjectName;
      String str_layer_name = sLayerName;
      String str_group_name = sGroupName;

      if (String.IsNullOrEmpty(str_object_name))
      {
        m_oindx++;
        str_object_name = $"object_{m_oindx}";
      }

      if (FileObjWriteOptions.ObjObjectNames.ObjectAsGroup != options.ExportObjectNames)
      {
        if (FileObjWriteOptions.ObjGroupNames.LayerAsGroup == options.ExportGroupNameLayerNames && false == String.IsNullOrEmpty(sLayerName) &&
            sLayerName != m_lastlayername)
        {
          // The layer name that comes from Rhino is a combination of all the nested layer names separated by
          // "::".  In order for this to work nicely we need to replace white space with "_" and then replace the
          // "::" with spaces.  It's ok, perhaps even preferable, to use the unmodified string for comparison sake 
          // so set last layername to the unmodified version.
          string tmp = str_layer_name;
          MakeUnderbarString(ref tmp, true);
          tmp = tmp.Replace("::", " ");
          WriteObjGroupObjectString(options, "g", tmp);
          m_lastlayername = str_layer_name;
        }
        else if (FileObjWriteOptions.ObjGroupNames.GroupAsGroup == options.ExportGroupNameLayerNames && false == String.IsNullOrEmpty(sGroupName) &&
                 str_group_name != m_lastgroupname)
        {
          WriteObjGroupObjectString(options, "g", str_group_name);
          m_lastgroupname = str_group_name;
        }

        if (FileObjWriteOptions.ObjObjectNames.ObjectAsObject == options.ExportObjectNames)
          WriteObjGroupObjectString(options, "o", str_object_name);
      }
      else
        WriteObjGroupObjectString(options, "g", str_object_name);
    }

    private void MakeUnderbarString(ref String name, bool bIsLayerName = false)
    {
      name = name.Trim();

      // "char" is really a wchar_t.
      char[] s = name.ToCharArray();
      int i, ict = s.Length;
      for (i = 0; ict > i; i++)
      {
        if (s[i] >= 'a' && s[i] <= 'z')
          continue;
        if (s[i] >= 'A' && s[i] <= 'Z')
          continue;
        if (s[i] >= '0' && s[i] <= '9')
          continue;
        if ('_' == s[i])
          continue;

        // We don't want to nuke the :: in layer names because they are the separators 
        if (bIsLayerName && ':' == s[i])
          continue;

        // Any other value must be replaced with an _ so the obj file will be valid.
        s[i] = '_';
      }

      name = new String(s);
    }

    private void WriteObjGroupObjectString(FileObjWriteOptions options, String kind, String name)
    {
      if (options.MergeNestedGroupingNames)
      {
        //MakeUnderbarString(ref name);
        name = name.Replace(" ", "__");
      }

      String tmp = $"{kind} {name}";

      // Eventually, .NET takes the  wchar_t string "tmp" and converts it
      // to UTF-8 before writing the file.  Since all wchar_t elements 
      // have value <= 127 (that's what the s[] stuff above does), the UTF-8 file has
      // no multi-byte encoded Unicode code points.
      WriteObjString(options, tmp);
    }


    private bool GetMatName(Material tmpmat, bool underbarMaterialName, out String name)
    {
	    name = "";

	    if (null == tmpmat)
		    return false;

      if (Rhino.RhinoMath.UnsetIntIndex == tmpmat.MaterialIndex)
      {
        name = tmpmat.Name;
        if (false == string.IsNullOrEmpty(name))
        {
          if (0 <= m_sorted_mat_name_array.BinarySearch(name))
            return false;

          m_sorted_mat_name_array.Add(name);
          m_sorted_mat_name_array.Sort();
          return true;
        }

        name = "Default";
        if (false == m_wrote_default_material)
        {
          m_wrote_default_material = true;
          return true;
        }

        return false;
      }

	    if (0 > tmpmat.MaterialIndex)
		    return false;

			if (null == m_mat_name_array || tmpmat.MaterialIndex >= m_mat_name_array.Length)
        Array.Resize(ref m_mat_name_array, tmpmat.MaterialIndex + 1);

      if (null != m_mat_name_array[tmpmat.MaterialIndex] &&
          0 != m_mat_name_array[tmpmat.MaterialIndex].Length)
      {
        //Already have a name for this material index  
        name = $"{m_mat_name_array[tmpmat.MaterialIndex]}";
        return false;
      }

      if (String.IsNullOrEmpty(tmpmat.Name))
      {
        //No name material that has not already been added.
        //Cook up name and add to MTL file
        name = $"Material_{m_mindx++}";
        m_mat_name_array[tmpmat.MaterialIndex] = name;
        return true;
      }

      //this material has a name, see if we already wrote it to the mtl file
      name = $"{tmpmat.Name}";
      if (underbarMaterialName)
        MakeUnderbarString(ref name);

      m_mat_name_array[tmpmat.MaterialIndex] = name;

      if (0 <= m_sorted_mat_name_array.BinarySearch(name))
        return false;

      m_sorted_mat_name_array.Add(name);
      m_sorted_mat_name_array.Sort();
      return true;
    }
  }
}