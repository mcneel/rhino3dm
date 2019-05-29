using System;
using System.Collections.Generic;
using System.Globalization;
using System.Drawing;
using System.IO;
using System.Linq;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.PlugIns;
using Rhino.UI;
using Point = Rhino.Geometry.Point;
using Rhino.Runtime;

#pragma warning disable 1591

namespace Rhino.FileIO
{
  /// <summary>
  /// Support for obj file format
  /// </summary>
  public static class FileObj
  {
    /// <summary>Write an obj file based on the contents of a RhinoDoc</summary>
    /// <param name="filename"></param>
    /// <param name="doc"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    public static WriteFileResult Write(String filename, RhinoDoc doc, FileObjWriteOptions options)
    {
      FileObjWriter writer = new FileObjWriter();
      return writer.WriteFile(filename, doc, options);
    }

    /// <param name="filename"></param>
    /// <param name="doc"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    public static bool Read(String filename, RhinoDoc doc, FileObjReadOptions options)
    {
      using (FileObjReader reader = new FileObjReader())
      {
        return reader.ReadFile(filename, doc, options);
      }
    }
  }

  internal partial class FileObjReader : IDisposable
  {
    enum ElementType
    {
      //EUnknown = 0,
      //Point,
      //Line,
      //Face,
      Curve,
      Curve2D,
      Surface
    };

    enum CsType
    {
      CsUnknown = 0,
      Bmatrix,
      Bezier,
      Bspline,
      Cardinal,
      Taylor
    };

    string TypeToString(CsType type)
    {
      string str;
      switch(type)
      {
        case CsType.Bezier:
        { 
          str = "bezier";
          break;
        }
        case CsType.Bmatrix:
        {
          str = "bmatrix";
          break;
        }
        case CsType.Bspline:
        {
          str = "bspline";
          break;
        }
        case CsType.Cardinal:
        {
          str = "cardinal";
          break;
        }
        case CsType.Taylor:
        {
          str = "taylor";
          break;
        }
        case CsType.CsUnknown:
        {
          str = "unknown";
          break;
        }
        default:
        {
          str = "unknown";
          break;
        }
      }

      return str;
    }

    class VTnIdx
    {
      internal VTnIdx()
      {
        VArrayIdx = -1;
        Vidx = -1;
        Tidx = -1;
        Nidx = -1;
      }

      internal int VArrayIdx //index into the m_V array of the ON_Mesh
      {
        get;
        set;
      }

      internal int Vidx //index of the v from then OBJ file
      {
        get;
        set;
      }

      internal int Tidx //index of the vt from then OBJ file
      {
        get;
        set;
      }

      internal int Nidx //index of the vn from then OBJ file
      {
        get;
        set;
      }
    };


    class ReadObjGroupObject : IDisposable
    {
      internal ReadObjGroupObject()
      {
        GName = null;
        OName = null;
        MName = null;

        //Reserve 256 faces worth up front
        FaceListList = new List<List<VTnIdx>> { Capacity = 256 };
        //Reserve 256x4 vertexes up front
        ObjvArray = new List<VTnIdx> { Capacity = 1024 };
        CurveArray = new List<NurbsCurve>();

        PlineArray = new List<PolylineCurve>();
        BrepArray = new List<Brep>();
        PointArray = new List<Point3d>();
        PointCloudArray = new List<PointCloud>();
        SurfCvIdxArray =  new List<int>();

        m_mesh = null;
      }
      public void Dispose()
      {
        foreach (var pline in PlineArray)
          pline?.Dispose();
        PlineArray.Clear();

        foreach (var curve in CurveArray)
          curve?.Dispose();
        CurveArray.Clear();

        foreach (var brep in BrepArray)
          brep?.Dispose();
        BrepArray.Clear();

        m_mesh?.Dispose();
      }

      internal bool IsValid(int idx, int vCt, int tCt, int nCt)
      {
        int ct = Mesh.Vertices.Count;
        VTnIdx vtn = ObjvArray[idx];


        if (0 > vtn.VArrayIdx || ct <= vtn.VArrayIdx)
          return false;

        if (0 > vtn.Vidx || vCt <= vtn.Vidx)
          return false;

        if (0 < tCt && -1 != vtn.Tidx)
        {
          if (0 > vtn.Tidx || tCt <= vtn.Tidx)
            return false;
        }

        if (0 < nCt && -1 != vtn.Nidx)
        {
          if (0 > vtn.Nidx || nCt <= vtn.Nidx)
            return false;
        }

        return true;
      }

      internal String GName;
      internal String OName;
      internal String MName;

      internal readonly List<String> GroupNames = new List<string>();
  
      //Do not delete these.  They are added directly to the doc.
      Mesh m_mesh;
      public Mesh Mesh => m_mesh ?? (m_mesh = new Mesh());

      internal readonly List<VTnIdx> ObjvArray;
      internal VTnIdx AddVtnIdx()
      {
        VTnIdx tmp = new VTnIdx();
        ObjvArray.Add(tmp);
        return ObjvArray.Last();
      }

      internal int VtnCount => ObjvArray.Count;

      internal readonly List<List<VTnIdx>> FaceListList;
      internal List<VTnIdx> AddFaceList(int startCount = 0)
      {
        List<VTnIdx> tmp = new List<VTnIdx>(startCount);
        FaceListList.Add(tmp);
        return FaceListList.Last();
      }

      internal readonly List<NurbsCurve> CurveArray;
      public void AddCurve(NurbsCurve crv)
      {
        CurveArray.Add(crv);
      }

      internal readonly List<PolylineCurve> PlineArray;
      internal readonly List<Brep> BrepArray;
      internal readonly List<Point3d> PointArray;
      internal readonly List<PointCloud> PointCloudArray;
      internal readonly List<int> SurfCvIdxArray;
      internal double[] SurfUKnotArray;
      internal double[] SurfVKnotArray;
    };

    private RhinoDoc m_rhino_doc;
    private string m_filename;

    readonly int m_max_bogus_face_ct;
    int m_bogus_face_ct;
    bool m_display_bogus_limit_prompt;

    private readonly bool m_b_import_al_type_objs;

    readonly List<String> m_group_names = new List<string>();

    String m_group_name;
    String m_object_name;
    private String m_material_name;

    bool m_b_file_has_vertex_colors;
    readonly List<Color> m_v_color_array = new List<Color>();

    readonly List<Point3d> m_v_array = new List<Point3d>();
    readonly List<double> m_v_weight_array = new List<double>(); //only used for rational points. Set to 0 otherwise.

    readonly List<Point2d> m_vp_array = new List<Point2d>();
    readonly List<double> m_vp_weight_array = new List<double>(); //only used for rational points. Set to 0 otherwise.

    readonly List<Point2f> m_vt_array = new List<Point2f>();
    readonly List<Vector3f> m_vn_array = new List<Vector3f>();

    readonly List<int> m_f_array = new List<int>();


    ElementType m_element_type;
    CsType m_cs_type; //curve/surface type
    bool m_rat;      // rational
    int m_deg_u;      // degree in u
    int m_deg_v;      // degree in v

    bool m_b_have_knots_in_u_dir;
    bool m_b_have_knots_in_v_dir;

    private double m_tol;


    private readonly List<NurbsCurve> m_curv2_array = new List<NurbsCurve>();

    ReadObjGroupObject m_current_ogo;
    readonly List<ReadObjGroupObject> m_obj_array = new List<ReadObjGroupObject>();

    public FileObjReader()
    {
      m_max_bogus_face_ct = 25;
      m_bogus_face_ct = 0;
      m_display_bogus_limit_prompt = false;

      m_b_import_al_type_objs = false;

      m_group_name = null;
      m_object_name = null;
      m_material_name = null;

      BogusIndexCount = 0;
    }

    //private ThreadTerminator m_terminator;

    ObjTextFile m_text_file = new ObjTextFile("");
    class ObjTextFile : IDisposable
    {
      internal ObjTextFile(String filename)
      {
        m_file = null;
        if (false == String.IsNullOrEmpty(filename))
          m_file = new StreamReader(filename);
        FileEnd = false;
      }

      public void Dispose()
      {
        m_file.Close();
      }

      readonly char[] m_separators = new char[]
      {
        (char) 10, //new line
        (char) 13, //carriage return
        (char) 0,  //null
        (char) 9,  //tab
        (char) 32, //space
        (char) 92  //back slash

        // Don't use '/' as a separator or it will split the face vertex combos, better to remove the line wrap indicators
        // than to leave them in and try to split them out
        //(char) 47  //forward slash  
      };

      private readonly StreamReader m_file;
      internal bool IsValid => null != m_file;

      internal bool FileEnd;
      private String[] m_str_array;

      internal int StringCount
      {
        get
        {
          if (null != m_str_array)
            return m_str_array.Length;
          else
            return 0;
        }
      }

      internal string GetString(int i)
      {
        string str = "";
        if (0 <= i && m_str_array.Length > i)
          str = $"{m_str_array[i]}";
        return str;
      }

      private string Buffer { get; set; }

      internal bool GetTexturePath(FileObjReader reader, string omit, out string path)
      {
        if (false == ExtractFileName(omit, Buffer, out path))
        {
          int i, ct = StringCount;
          for (i = 1; ct > i; i++)
          {
            if (1 < i)
            {
              path += " ";
              path += GetString(i);
              continue;
            }

            path += GetString(i);
          }
        }

        try
        {
          FileInfo fi = new FileInfo(path);
          if (false == fi.Exists)
          {
            //Try harder
            //Figure out path of mtl file.  If it's just a file name
            //assume it's a relative path and add the obj's path

            if (false == reader.FixFilePath(ref path))
              return false; //The file must not exist
          }
        }
        catch (Exception)
        {
          string error_msg = $"\"{path}\" is not a valid obj material file.\n";
          HostUtils.DebugString("Error " + error_msg);
          return false;
        }
      
        return true;
      }


      private static bool ExtractFileName(string maptype, string buffer, out string result)
      {
        result = "";

        int idx = buffer.IndexOf(maptype, StringComparison.OrdinalIgnoreCase);
        if (-1 == idx)
          return false;

        // the +1 is to get by the whitespace, if this turns out to not work because of 
        // multiple spaces, for instance, I'll have to rework this.  Fortunately, it does not
        // come up often, not sure offhand what we do on export to obj if a texture filename 
        // contains spaces.
        result = buffer.Substring(idx + maptype.Length + 1);
        result = result.Trim();

        // Feb 26, 2018 - Tim
        // Fix for RH-44378 
        // Remove quotes from the mtllib string.
        char[] trimchars = { '"' };
        result = result.Trim(trimchars);
        return true;
      }

      internal bool GetNextObj()
      {
        if (null == m_file)
          return false;

        Buffer = m_file.ReadLine();
        if (null == Buffer)
        {
          FileEnd = true;
          return false;
        }

        Buffer = Buffer.Trim();
        while (false == String.IsNullOrEmpty(Buffer) && '\\' == Buffer[Buffer.Length - 1])
        {
          Buffer = Buffer.Substring(0, Buffer.Length - 1); // shave off the trailing '\'
          Buffer += " "; // add trailing space, fix for RH-51448
          Buffer += m_file.ReadLine();
          Buffer = Buffer.Trim();
        }

        m_str_array = Buffer.Split(m_separators, StringSplitOptions.RemoveEmptyEntries);
        return m_str_array.Length > 0;
      }

      internal bool CombineStrings(out string output)
      {
        output = "";
        int i, ct = m_str_array.Length;
        if (2 > ct)
          return false;

        output = m_str_array[1];
        for (i = 2; ct > i; i++)
          output = $"{output} {m_str_array[i]}";

        return true;
      }
      
    }

    static bool StringsMatch(string a, string b)
    {
      return 0 == String.Compare(a, b, StringComparison.OrdinalIgnoreCase);
    }

    static bool ConvertStringToInt(string input, out int output)
    {
      if (StringsMatch(input, "nan"))
      {
        output = -1;
        return true;
      }

      if (false == int.TryParse(input, NumberStyles.Integer, CultureInfo.InvariantCulture, out output))
      {
        output = -1; //int.TryParse will set output to zero on failure
        return false;
      }
      return true;
    }

    static bool ConvertStringToDouble(string input, out double output)
    {
      if (StringsMatch(input, "nan"))
      {
        output = 0.0;
        return true;
      }

      return double.TryParse(input, NumberStyles.Float, CultureInfo.InvariantCulture, out output);
    }

    public int BogusIndexCount { get; private set; }

    private bool ConvertIndex(ref int idx, int itemct)
    {
      if (0 > idx && itemct >= Math.Abs(idx))
      {
        idx = itemct + idx;
        return true;
      }

      if (1 <= idx && itemct >= idx)
      {
        idx = idx - 1;
        return true;
      }

      //11_10_2008 TimH. Changed the way this works a little so it displays
      //error message but still reads in the file despite having a bogus index.
      idx = -1;
      BogusIndexCount++;
      return false;
    }

    private bool ReadFile(FileObjReadOptions options)
    {
      m_text_file = new ObjTextFile(m_filename);
      if (false == m_text_file.IsValid)
        return false;

      bool rc = true;
      while (false == m_text_file.FileEnd && rc)
      {
        if (false == m_text_file.GetNextObj())
          continue;

        int ct = m_text_file.StringCount;
        if (0 == ct)
          continue;

        string str = m_text_file.GetString(0);
        switch (str[0])
        {
          case 'b':
          case 'B':
          {
            if (StringsMatch(str, "bevel"))
            {
              //TODO bevel interpolation
            }
            else if (StringsMatch(str, "bmat"))
            {
              //TODO basis matrix
            }
            break;
          }

          case 'c':
          case 'C':
          {
            if (StringsMatch(str, "cstype"))
            {
              rc = SetCsType();
            }
            else if (StringsMatch(str, "curv2"))
            {
              rc = AddCurve(true); //true for 2d curves
            }
            else if (StringsMatch(str, "curv"))
            {
              rc = AddCurve();
            }
            else if (StringsMatch(str, "con"))
            {
              //TODO Connectivity bewteen free-form surfaces
            }
            else if (StringsMatch(str, "c_interp"))
            {
              //TODO color interpolation
            }
            else if (StringsMatch(str, "ctech"))
            {
              //TODO curve approximation technique
            }
            break;
          }

          case 'd':
          case 'D':
          {
            if (StringsMatch(str, "deg"))
              rc = SetDegree();
            else if (StringsMatch(str, "d_interp"))
            {
              //TODO dissolve interpolation
            }
            break;
          }

          case 'e':
          case 'E':
          {
            if (StringsMatch(str, "end"))
            {
              //Nothing to do for end.  Just continue on.
            }
            break;
          }

          case 'f':
          case 'F':
          {
            if (StringsMatch(str, "f"))
            {
              rc = AddFace(options);
              //May 06, 2010 Tim. Fix for RR 66268
              //Hack to work around bogus faces and give the user at least a portion of the model
              //This use to completely bail.
              if (false == rc)
              {
                if (m_max_bogus_face_ct > m_bogus_face_ct)
                {
                  RhinoApp.WriteLine(Localization.LocalizeString("Encountered bogus mesh face.\n", 1));
                  m_bogus_face_ct++;
                }
                else
                {
                  if (false == m_display_bogus_limit_prompt)
                  {
                    RhinoApp.WriteLine(Localization.LocalizeString("Encountered {0} bogus mesh faces. No longer displaying errors to command line.\n", 2), m_bogus_face_ct);
                    m_display_bogus_limit_prompt = true;
                  }
                }

                rc = true;
              }
            }
            break;
          }

          case 'g':
          case 'G':
          {
            if (StringsMatch(str, "g") || StringsMatch(str, "group"))
            {
              m_group_names.Clear();
              int i;

              // We need the m_group_name to contain all of the "g" contents otherwise it screws up the sorter
              // Also, when group order is reversed we actually need to reverse the order of the strings in the 
              // array so Rhino layer nesting happens properly, it doesn't really matter for Rhino groups
              //m_group_name = false == options.ReverseGroupOrder ? m_text_file.GetString(1) : m_text_file.GetString(ct - 1);
              m_group_name = null;
              
              if (false == options.ReverseGroupOrder)
              {
                for (i = 1; ct > i; i++) //start at 1 so we miss the "g"
                {
                  m_group_name = null == m_group_name ? m_text_file.GetString(i) : $"{m_group_name} {m_text_file.GetString(i)}";
                  m_group_names.Add(m_text_file.GetString(i));
                }
              }
              else
              {
                for (i = ct-1; 0 < i; i--) //not <= 0, so we miss the "g"
                {
                  m_group_name = null == m_group_name ? m_text_file.GetString(i) : $"{m_group_name} {m_text_file.GetString(i)}";
                  m_group_names.Add(m_text_file.GetString(i));
                }
              }

              rc = AddorSetGroup(options);
            }
            break;
          }

          case 'h':
          case 'H':
          {
            if (StringsMatch(str, "hole"))
              rc = AddLoop();
            break;
          }

          case 'l':
          case 'L':
          {
            if (StringsMatch(str, "l"))
            {
              rc = AddPolyline();
            }
            else if (StringsMatch(str, "lod"))
            {
              //TODO level of detail
            }
            break;
          }

          case 'm':
          case 'M':
          {
            if (StringsMatch(str, "mtllib"))
              AddMaterialsToDoc(options); //don't bail if we can't load materials
            else if (StringsMatch(str, "mg"))
            {
              //TODO merging group
            }
            break;
          }

          case 'o':
          case 'O':
          {
            if (StringsMatch(str, "o"))
            {
              if (m_text_file.CombineStrings(out m_object_name))
                rc = AddorSetGroup(options);
            }
            break;
          }

          case 'p':
          case 'P':
          {
            if (StringsMatch(str, "parm"))
            {
              if (ElementType.Curve == m_element_type || ElementType.Curve2D == m_element_type)
                rc = AddCurveKnots();
              else if (ElementType.Surface == m_element_type)
                rc = AddSurfaceKnots();
            }
            else if (StringsMatch(str, "p"))
            {
              rc = AddPoints();
            }
            break;
          }

          case 's':
          case 'S':
          {
            if (StringsMatch(str, "surf"))
            {
              rc = AddSurface();
            }
            else if (StringsMatch(str, "step"))
            {
              //TODO step size
            }
            else if (StringsMatch(str, "scrv"))
            {
              //TODO special curve
            }
            else if (StringsMatch(str, "sp"))
            {
              //TODO special point
            }
            else if (StringsMatch(str, "s"))
            {
              //TODO smoothing group
            }
            else if (StringsMatch(str, "shadow_obj"))
            {
              //TODO shadow casting
            }
            else if (StringsMatch(str, "stech"))
            {
              //TODO surface approximation technique
            }
            break;
          }

          case 't':
          case 'T':
          {
            if (StringsMatch(str, "trim"))
            {
              rc = AddLoop();
            }
            else if (StringsMatch(str, "trace_obj"))
            {
              //TODO ray tracing
            }
            break;
          }

          case 'u':
          case 'U':
          {
            if (StringsMatch(str, "usemtl") && false == options.IgnoreTextures)
            {
              if (m_text_file.CombineStrings(out m_material_name))
                rc = AddorSetGroup(options);
            }
            break;
          }

          case 'v':
          case 'V':
          {
            if (1 == str.Length)
              rc = AddVertex();
            else
            {
              switch (str[1])
              {
                case 'p':
                case 'P':
                {
                  //parameter space vertex
                  rc = AddParameterSpaceVertex();
                  break;
                }
                case 't':
                case 'T':
                {
                  //texture coordinate
                  rc = AddVertexTexture();
                  break;
                }
                case 'n':
                case 'N':
                {
                  //vertex normal
                  rc = AddVertexNormal();
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

          default:
          {
            break;
          }
        }//end of switch
      }

      return rc;
    }

    int GetObjectCount()
    {
      return m_obj_array.Count;
    }

    void AddGroupsToDoc(ReadObjGroupObject ogo, ObjectAttributes attribs, FileObjReadOptions options)
    {
      int i, ct = ogo.GroupNames.Count;
      attribs.RemoveFromAllGroups();
      attribs.LayerIndex = m_rhino_doc.Layers.CurrentLayerIndex;
  
      if (FileObjReadOptions.UseObjGsAs.IgnoreObjGroups == options.UseObjGroupsAs)
      {
        if (options.UseObjObjects && false == String.IsNullOrEmpty(ogo.OName))
          attribs.Name = ogo.OName;
      }
      else if (FileObjReadOptions.UseObjGsAs.ObjGroupsAsGroups == options.UseObjGroupsAs)
      {
        for (i=0; ct>i; i++)
        {
          var group_idx = m_rhino_doc.Groups.Find(ogo.GroupNames[i]);
          if (RhinoMath.UnsetIntIndex == group_idx)
            group_idx = m_rhino_doc.Groups.Add(ogo.GroupNames[i]);

          attribs.AddToGroup(group_idx);
        }

        if (options.UseObjObjects && false == String.IsNullOrEmpty(ogo.OName))
          attribs.Name = ogo.OName;
      }
      else if (FileObjReadOptions.UseObjGsAs.ObjGroupsAsLayers == options.UseObjGroupsAs)
      {
        Guid parent_id = Guid.Empty;
        for (i=0; ct>i; i++)
        {
          var layer_idx = m_rhino_doc.Layers.Find(parent_id, ogo.GroupNames[i], -1);
          if (-1 == layer_idx)
          {
            Layer layer = new Layer {Name = ogo.GroupNames[i], ParentLayerId = parent_id};
            layer_idx = options.ReadOptions.ImportReferenceMode ? m_rhino_doc.Layers.AddReferenceLayer(layer) : m_rhino_doc.Layers.Add(layer);
          }

          if (-1 != layer_idx)
            parent_id = m_rhino_doc.Layers[layer_idx].Id;

          attribs.LayerIndex = layer_idx;
        }

        if (options.UseObjObjects && false == String.IsNullOrEmpty(ogo.OName))
          attribs.Name = ogo.OName;
      }
      else if (FileObjReadOptions.UseObjGsAs.ObjGroupsAsObjects == options.UseObjGroupsAs)
      {
        if (1 <= ct)
        {
            attribs.Name = ogo.GroupNames[0];
          for (i = 1; ct > i; i++)
            attribs.Name = $"{attribs.Name} {ogo.GroupNames[i]}";
        }
        else if (false == string.IsNullOrEmpty(ogo.GName))
          attribs.Name = ogo.GName;
      }
    }


    bool AddObjectsToDoc(FileObjReadOptions options)
    {
      bool rc = false;

      int i, ct = m_obj_array.Count;

#if _DEBUG
      const bool b_add2_d_curves = false;
      if (b_add2_d_curves)
      {
        cct = m_curv2_array.Count();
        for (i=0; i < cct; i++)
          m_rhino_doc.Objects.AddCurve(m_curv2_array[i]);
      }
#endif

      //Don't bail early if we find crap at this point.
      //It's already been read from the file and created where possible so it shouldn't have a huge impact
      //And the function will still return rc (which will be false if there was crap)
      for (i=0; i<ct; i++)
      {
        //if (m_terminator.))
        //{
        //  m_bCancelled = true;
        //  break;
        //}

        ReadObjGroupObject ogo = m_obj_array[i];

        ObjectAttributes attribs = new ObjectAttributes();
        AddGroupsToDoc(ogo, attribs, options);
        int matidx;
        if (false == string.IsNullOrEmpty(ogo.MName) && m_material_indices.TryGetValue(ogo.MName, out matidx))
        {
          attribs.MaterialIndex = matidx;
          attribs.MaterialSource = ObjectMaterialSource.MaterialFromObject;
          if (options.DisplayColorFromObjMaterial)
          {
            attribs.ColorSource = ObjectColorSource.ColorFromObject;
            attribs.ObjectColor = m_rhino_doc.Materials[matidx].DiffuseColor;
          }
        }

        if (null != ogo.Mesh)
        {
          if (MakeMesh(ogo))
          {
            rc = FixMesh(ogo.Mesh, options);
            if (options.MapYtoZ)
              ogo.Mesh.Transform(options.GetTransform());
            m_rhino_doc.Objects.AddMesh(ogo.Mesh, 
                                        attribs, 
                                        null, 
                                        options.ReadOptions.ImportReferenceMode,
                                        false);  // Do not require the mesh to be valid.
          }
        }

        var cct = ogo.CurveArray.Count;
        int j;
        if (0 < cct)
        {
          rc = true;
          for (j = 0; j < cct; j++)
          {
            if (options.MapYtoZ)
              ogo.CurveArray[j].Transform(options.GetTransform());

            m_rhino_doc.Objects.AddCurve(ogo.CurveArray[j], attribs, null, options.ReadOptions.ImportReferenceMode);
          }
        }

        var lct = ogo.PlineArray.Count;
        if (0 < lct)
        {
          rc = true;
          for (j = 0; j < lct; j++)
          {
            if (options.MapYtoZ)
              ogo.PlineArray[j].Transform(options.GetTransform());

            m_rhino_doc.Objects.AddCurve(ogo.PlineArray[j], attribs, null, options.ReadOptions.ImportReferenceMode);
          }
        }

        var pct = ogo.PointArray.Count;
        if (0 < pct)
        {
          rc = true;
          for (j = 0; j < pct; j++)
          {
            if (options.MapYtoZ)
              ogo.PointArray[j].Transform(options.GetTransform());

            m_rhino_doc.Objects.AddPoint(ogo.PointArray[j], attribs, null, options.ReadOptions.ImportReferenceMode);
          }
        }

        var pcct = ogo.PointCloudArray.Count;
        if (0 < pcct)
        {
          rc = true;
          for (j = 0; j < pcct; j++)
          {
            if (options.MapYtoZ)
              ogo.PointCloudArray[j].Transform(options.GetTransform());

            m_rhino_doc.Objects.AddPointCloud(ogo.PointCloudArray[j], attribs, null, options.ReadOptions.ImportReferenceMode);
          }
        }

        var sct = ogo.BrepArray.Count;
        if (0 < sct)
        {
          rc = true;
          for (j = 0; j < sct; j++)
          {
            if (0 == ogo.BrepArray[j].Faces[0].Loops.Count)
              ogo.BrepArray[j].Loops.AddOuterLoop(0);

            ogo.BrepArray[j].Repair(m_rhino_doc.ModelAbsoluteTolerance);

            if (options.MapYtoZ)
              ogo.BrepArray[j].Transform(options.GetTransform());


            m_rhino_doc.Objects.AddBrep(ogo.BrepArray[j], attribs, null, options.ReadOptions.ImportReferenceMode);
          }
        }
      }

      return rc;
    }

    public bool ReadFile(String filename, RhinoDoc doc, FileObjReadOptions options)
    {
      m_rhino_doc = doc;
      m_tol = m_rhino_doc.ModelAbsoluteTolerance;

      m_filename = filename;

      // add/set a group that has no g, o or usemtl to for the catchall 
      AddorSetGroup(options);

      bool rc = ReadFile(options);
      if (false == rc)
        RhinoApp.WriteLine(Localization.LocalizeString("Errors occurred reading this file, may be incomplete.\n", 3));

      if (0 < GetObjectCount())
        rc = AddObjectsToDoc(options);

      m_text_file?.Dispose();
      return rc;
    }

    public void Dispose()
    {
      foreach(var crv in m_curv2_array)
        crv?.Dispose();
      m_curv2_array.Clear();

      foreach(var obj in m_obj_array)
        obj?.Dispose();
      m_obj_array.Clear();
    }
  }

  internal partial class FileObjWriter
  {
    private class ObjTrim
    {
      internal ObjTrim()
      {
        Outer = 1;
        CurveCount = 0;
        Curveindex = new int[2];
        Domain = null;
      }

      internal int Outer; /* outer/inner flag */
      internal int CurveCount; /* number of curves in the trim loop */
      internal readonly int[] Curveindex; /* absolute indices of 1st and last curves */
      internal Interval[] Domain; /* domains of the curves in the trim */
    };

    readonly CultureInfo m_culture_info_en_us = CultureInfo.InvariantCulture;

		internal FileObjWriter()
    {
      m_oindx = 0;
      m_mindx = 1;
      m_vindx = 0;
      m_vtindx = 0;
      m_vnindx = 0;
      m_vpindx = 0;
      m_curv2_count = 0;
      m_trimcount = 0;
      m_wrote_default_material = false;
      m_sorted_mat_name_array = new List<string>();

      m_rhino_doc = null;
      m_matfile = null;
      m_file = null;
    }

    /* pointer returned by _wfopen( L"...", L"wb" ) */
    private StreamWriter m_file;

    private RhinoDoc m_rhino_doc;
    private string m_filename;

    /* private */
    private string m_s_buffer;

    private int m_vindx, m_vpindx, m_vtindx, m_vnindx;

    // auto-object index
    private int m_oindx;

    private String m_lastlayername;

    private String m_lastgroupname;

    // auto-material index
    private int m_mindx;

    /* number of 2d curves in the file */
    private int m_curv2_count;
    /* number of trimming loops in the file */
    private int m_trimcount;

    private String[] m_mat_name_array;
    private readonly List<String> m_sorted_mat_name_array;
    private bool m_wrote_default_material;

    /* array of trims */
    private ObjTrim[] m_trims;

    private void WriteObjString(FileObjWriteOptions options, String str)
    {
      if (65 > str.Length || false == options.WrapLongLines)
      {
        m_s_buffer = str;
        WriteObjFlush(options);
        return;
      }

      int start_idx = 0;
      int space_idx = 0, tmp = str.IndexOf(" ", start_idx, StringComparison.Ordinal);
      space_idx = (-1 == tmp ? space_idx + 1 : tmp);
      while (str.Length != space_idx)
      {
        if (60 < space_idx - start_idx)
        {
          m_s_buffer = $"{str.Substring(start_idx, space_idx - start_idx)} \\";
          WriteObjFlush(options);
          start_idx = space_idx + 1;
        }

        space_idx++;
        tmp = str.IndexOf(" ", space_idx, StringComparison.Ordinal);
        space_idx = (-1 == tmp ? str.Length : tmp);
      }


      m_s_buffer = str.Substring(start_idx, str.Length - start_idx);
      WriteObjFlush(options);
    }

    private void WriteObjFlush(FileObjWriteOptions options)
    {
      // objFile.SBuffer is a wchar_t string.
      // objFile.File.Write() converts the wchar_t string into
      // a UTF-8 encoded string and writes the UTF-8 encode string
      // to the "file".  (Tim and Dale Lear determined this by looking
      // at files in a binary editor and testing strings with
      // Unicode code points > 127 (we used a greek letter).
      // OBJ files are supposed to be "ASCII" files.  The strings
      // that end up getting passed as objFile have had all
      // elements > 127 converted to underbars so the output file
      // will be "ASCII".
      m_file.Write(m_s_buffer);
      m_s_buffer = "";
      WriteObjeol(options);
    }

    private void WriteObjeol(FileObjWriteOptions options, StreamWriter file = null)
    {
      StreamWriter tmp = m_file;
      if (null != file)
        tmp = file;

      string eol = null;
      switch (options.EolType)
      {
        case FileObjWriteOptions.AsciiEol.Lf: /* LF */
          eol = new string((char) 0x0A, 1);
          break;
        case FileObjWriteOptions.AsciiEol.Cr: /* CR */
          eol = new string((char)0x0D, 1);
          break;
        default: /* CRLF */
          eol = new string((char)0x0D, 1);
          eol += (char) 0x0A;
          break;
      }

      if (null != eol)
        tmp.Write(eol);
    }

    public class RhinoObjectMesh
    {
      /// <param name="mesh"></param>
      /// <param name="attribs"></param>
      public RhinoObjectMesh(Mesh mesh, ObjectAttributes attribs)
      {
        Mesh = mesh;
        Attribs = attribs;
      }

      public Mesh Mesh;

      public ObjectAttributes Attribs;
    };

    /// <summary>
    /// 
    /// </summary>
    public class ObjRhinoObject
    {
      /// <param name="locobject"></param>
      /// <param name="attribs"></param>
      /// <param name="xform"></param>
      /// <param name="bInstance"></param>
      public ObjRhinoObject(RhinoObject locobject, ObjectAttributes attribs, Transform xform, bool bInstance)
      {
        Object = locobject;
        Attribs = attribs;
        Xform = xform;
        BInstance = bInstance;
      }

      public RhinoObject Object;

      public ObjectAttributes Attribs;

      public Transform Xform;

      public bool BInstance;
    };


    private class MeshLayerComparer : IComparer<RhinoObjectMesh>
    {
      public int Compare(RhinoObjectMesh x, RhinoObjectMesh y)
      {
        if (y != null && (x != null && x.Attribs.LayerIndex < y.Attribs.LayerIndex))
          return -1;
        if (y != null && (x != null && x.Attribs.LayerIndex > y.Attribs.LayerIndex))
          return 1;
        return 0;
      }
    };

    private class MeshGroupComparer : IComparer<RhinoObjectMesh>
    {
      public int Compare(RhinoObjectMesh x, RhinoObjectMesh y)
      {
        if (x != null)
        {
          if (y != null)
          {
            int xidx = 0, xct = x.Attribs.GroupCount, yidx = 0, yct = y.Attribs.GroupCount;
            while (xct > xidx && yct > yidx)
            {
              if (x.Attribs.GetGroupList()[xidx] < y.Attribs.GetGroupList()[yidx])
                return 1;
              if (x.Attribs.GetGroupList()[xidx] > y.Attribs.GetGroupList()[yidx])
                return -1;

              xidx++;
              yidx++;
            }
            if (xct > yct)
              return 1;
            if (xct < yct)
              return -1;
          }
        }
        return 0;
      }
    };

    public class ObjLayerComparer : IComparer<ObjRhinoObject>
    {
      public int Compare(ObjRhinoObject x, ObjRhinoObject y)
      {
        if (y != null && (x != null && x.Attribs.LayerIndex < y.Attribs.LayerIndex))
          return -1;
        if (y != null && (x != null && x.Attribs.LayerIndex > y.Attribs.LayerIndex))
          return 1;
        return 0;
      }
    };

    public class ObjGroupComparer : IComparer<ObjRhinoObject>
    {
      public int Compare(ObjRhinoObject x, ObjRhinoObject y)
      {
        if (x != null)
        {
          if (y != null)
          {
            int xidx = 0, xct = x.Attribs.GroupCount, yidx = 0, yct = y.Attribs.GroupCount;
            while (xct > xidx && yct > yidx)
            {
              if (x.Attribs.GetGroupList()[xidx] < y.Attribs.GetGroupList()[yidx])
                return 1;
              if (x.Attribs.GetGroupList()[xidx] > y.Attribs.GetGroupList()[yidx])
                return -1;

              xidx++;
              yidx++;
            }
            if (xct > yct)
              return 1;
            if (xct < yct)
              return -1;
          }
        }
        return 0;
      }
    };

    private int WriteMeshesToFile(Mesh[] meshes, ObjectAttributes[] attribs, FileObjWriteOptions options)
    {
      int i, openct = 0, ict = meshes.Length;
      RhinoObjectMesh[] mesh_list = new RhinoObjectMesh[ict];
      for (i = 0; ict > i; i++)
      {
        if (false == meshes[i].IsClosed)
          openct++;
        mesh_list[i] = new RhinoObjectMesh(meshes[i], attribs[i]);
      }

      if (ict == openct && false == options.ExportOpenMeshes)
      {
        Dialogs.ShowMessage(
          Localization.LocalizeString("No closed meshes were included in the selection. The OBJ file was not written.", 4),
          Localization.LocalizeString("OBJ Export Open Mesh Warning", 5), ShowMessageButton.OKCancel, ShowMessageIcon.Warning);
        return 0;
      }

      // Aug 6, 2015 - Tim  Fix for RH-31104.  Always sort by group when user has chosen 
      // GROUP_AS_GROUP == myOBJfile.m_ExportGnames
      if (FileObjWriteOptions.ObjObjectNames.ObjectAsObject != options.ExportObjectNames && options.SortObjGroups)
      {
        if (FileObjWriteOptions.ObjGroupNames.LayerAsGroup == options.ExportGroupNameLayerNames)
        {
          MeshLayerComparer tmp = new MeshLayerComparer();
          Array.Sort(mesh_list, tmp);
        }
      }
      else if (FileObjWriteOptions.ObjGroupNames.GroupAsGroup == options.ExportGroupNameLayerNames)
      {
        MeshGroupComparer tmp = new MeshGroupComparer();
        Array.Sort(mesh_list, tmp);
      }

      int outct = 0;
      for (i = 0; ict > i; i++)
      {
        Mesh mesh = mesh_list[i].Mesh;
        if (null == mesh)
          continue;

        if (false == mesh.IsClosed && false == options.ExportOpenMeshes)
          continue;

          m_b_use_uvw = false;
          m_uvw = Transform.Identity;

        ObjectAttributes o_attribs = mesh_list[i].Attribs;
        if (null != o_attribs)
          WriteObjGroupAndMat(options, m_matfile, Path.GetDirectoryName(m_filename), m_rhino_doc, o_attribs);

        if (FileObjWriteOptions.VertexWelding.Unwelded == options.MeshType)
          WriteUnweldedObjRhinoMesh(options, mesh);
        else if (FileObjWriteOptions.VertexWelding.Welded == options.MeshType)
          WriteObjRhinoMesh(options, mesh, true);
        else
          WriteObjRhinoMesh(options, mesh, false);
        outct++;
      }

      if (false == options.ExportOpenMeshes)
      {
        // prefixed newline isolates this prompt a little from the rest of the output
        // so it's a little easier to notice. 
        if (1 < openct)
          RhinoApp.WriteLine(Localization.LocalizeString("\n{0} open meshes were not exported.\n", 6), openct);
        else if (1 == openct)
          RhinoApp.WriteLine(Localization.LocalizeString("\n1 open mesh was not exported.\n", 7));
      }
      return outct;
    }

    private int WriteMeshesToFile(List<RhinoObject> rhinoObjects, FileObjWriteOptions options)
    {
      int i, ict = rhinoObjects.Count;
      List<RhinoObject> meshable_rhino_objects = new List<RhinoObject>(ict);
      List<RhinoObject> other_rhino_objects = new List<RhinoObject>(ict);

      for (i = 0; ict > i; i++)
      {
        // divy objects up into two list, those that can be meshed and those that cannot.
        if (rhinoObjects[i].IsMeshable(MeshType.Any))
          meshable_rhino_objects.Add(rhinoObjects[i]);
        else 
          other_rhino_objects.Add(rhinoObjects[i]);
      }

      // Get meshes to export (meshes breps, copys mesh object meshes,
      // deals with instance references that contain meshes and breps,
      // etc.
      Mesh[] meshes;
      ObjectAttributes[] attribs;

      int ui = options.WriteOptions.SuppressDialogBoxes ? 2 : options.UseSimpleDialog ? 0 : 1;
      MeshingParameters mp = options.MeshParameters;
      Result rs = RhinoObject.MeshObjects(meshable_rhino_objects, ref mp, ref ui, options.GetTransform(), out meshes, out attribs);
      if (Result.Cancel == rs)
        return -1;

      if (Result.Success != rs)
        return 0;

      if (!options.WriteOptions.SuppressDialogBoxes)
        options.UseSimpleDialog = 0 == ui;

      options.MeshParameters = mp;

      int rc = WriteMeshesToFile(meshes, attribs, options);
      if (0 == rc)
        return rc;

      rc += WriteObjectsToFile(other_rhino_objects, options);

      return rc;
    }

    private int WriteObjectsToFile(List<RhinoObject> rhinoObjects, FileObjWriteOptions options)
    {
      int j;
      int outct = 0, jct = rhinoObjects.Count;
      var object_array = new List<ObjRhinoObject>(jct);
      Transform xform = options.GetTransform();
      for (j = 0; jct > j; j++)
      {
        RhinoObject obj = rhinoObjects[j];
        if (null == obj)
          continue;

        if (ObjectType.InstanceReference == obj.ObjectType)
        {
          InstanceObject instance_obj = rhinoObjects[j] as InstanceObject;
          if (null == instance_obj)
            continue;

          // for v2 export, instance references must be exploded
          RhinoObject[] pieces;
          ObjectAttributes[] piece_attributes;
          Transform[] piece_transforms;
          instance_obj.Explode(true, out pieces, out piece_attributes, out piece_transforms);

          for (int k = 0; k < pieces.Length; k++)
            object_array.Add(new ObjRhinoObject(pieces[k], piece_attributes[k], piece_transforms[k], true));
        }
        else
          object_array.Add(new ObjRhinoObject(obj, obj.Attributes, Transform.Identity, false));
      }

      // Aug 6, 2015 - Tim  Fix for RH-31104.  Always sort by group when user has chosen 
      // GROUP_AS_GROUP == myOBJfile.m_ExportGnames
      if (FileObjWriteOptions.ObjObjectNames.ObjectAsGroup != options.ExportObjectNames && options.SortObjGroups)
      {
        if (FileObjWriteOptions.ObjGroupNames.LayerAsGroup == options.ExportGroupNameLayerNames)
        {
          ObjLayerComparer tmp = new ObjLayerComparer();
          object_array.Sort(tmp);
        }
      }
      else if (FileObjWriteOptions.ObjGroupNames.GroupAsGroup == options.ExportGroupNameLayerNames)
      {
        ObjGroupComparer tmp = new ObjGroupComparer();
        object_array.Sort(tmp);
      }

      jct = object_array.Count;
      for (j = 0; jct > j; j++)
      {
        if (null == object_array[j].Object)
          continue;

        WriteObjGroupAndMat(options, m_matfile, Path.GetDirectoryName(m_filename), m_rhino_doc, object_array[j].Attribs);

        Curve curve = object_array[j].Object.Geometry as Curve;
        Mesh mesh = object_array[j].Object.Geometry as Mesh;
        Brep brep = object_array[j].Object.Geometry as Brep;
        Extrusion extrusion = object_array[j].Object.Geometry as Extrusion;
        Point pt = object_array[j].Object.Geometry as Point;
        PointCloud ptset = object_array[j].Object.Geometry as PointCloud;
        
        if (null != curve)
        {
          if (object_array[j].BInstance || false == xform.IsIdentity)
          {
            Curve new_curve = curve.DuplicateCurve();
            if (null != new_curve)
            {
              if (object_array[j].BInstance)
                new_curve.Transform(object_array[j].Xform);
              if (false == xform.IsIdentity)
                new_curve.Transform(xform);
              WriteObjNurb(options, new_curve);
            }
          }
          else
            WriteObjNurb(options, curve);
        }
        else if (null != mesh)
        {
          if (object_array[j].BInstance || false == xform.IsIdentity)
          {
            Mesh new_mesh = new Mesh();
            new_mesh.CopyFrom(mesh);
            new_mesh.Transform(object_array[j].Xform);
            if (false == xform.IsIdentity)
              new_mesh.Transform(xform);

            if (FileObjWriteOptions.VertexWelding.Unwelded == options.MeshType)
              WriteUnweldedObjRhinoMesh(options, new_mesh);
            else if (FileObjWriteOptions.VertexWelding.Welded == options.MeshType)
              WriteObjRhinoMesh(options, new_mesh, true);
            else
              WriteObjRhinoMesh(options, new_mesh, false);
          }
          else
          {
            if (FileObjWriteOptions.VertexWelding.Unwelded == options.MeshType)
              WriteUnweldedObjRhinoMesh(options, mesh);
            else if (FileObjWriteOptions.VertexWelding.Welded == options.MeshType)
              WriteObjRhinoMesh(options, mesh, true);
            else
              WriteObjRhinoMesh(options, mesh, false);
          }
        }
        else if (null != brep)
        {
          if (object_array[j].BInstance || false == xform.IsIdentity)
          {
            Brep new_brep = brep.DuplicateBrep();
            if (null != new_brep)
            {
              if (object_array[j].BInstance)
                new_brep.Transform(object_array[j].Xform);
              if (false == xform.IsIdentity)
                new_brep.Transform(xform);
              WriteObjBrep(options, new_brep);
            }
          }
          else
            WriteObjBrep(options, brep);
        }
        else if (null != extrusion)
        {
          Brep ext_brep = extrusion.ToBrep();
          if (null != ext_brep)
          {
            if (object_array[j].BInstance || false == xform.IsIdentity)
            {
              Brep new_brep = ext_brep.DuplicateBrep();
              if (null != new_brep)
              {
                if (object_array[j].BInstance)
                  new_brep.Transform(object_array[j].Xform);
                if (false == xform.IsIdentity)
                  new_brep.Transform(xform);
                WriteObjBrep(options, new_brep);
              }
            }
            else
              WriteObjBrep(options, ext_brep);
          }
        }
        else if (null != pt)
        {
          if (object_array[j].BInstance || false == xform.IsIdentity)
          {
            Point pt2 = new Point(pt.Location);
            if (object_array[j].BInstance)
              pt2.Transform(object_array[j].Xform);
            if (false == xform.IsIdentity)
              pt2.Transform(xform);
            WriteObjPoint(options, pt2);
          }
          else
            WriteObjPoint(options, pt);
        }
        else if (null != ptset)
        {
          if (object_array[j].BInstance || false == xform.IsIdentity)
          {
            PointCloud ptset2 = new PointCloud(ptset);
            if (object_array[j].BInstance)
              ptset2.Transform(object_array[j].Xform);
            if (false == xform.IsIdentity)
              ptset2.Transform(xform);
            WriteObjPointCloud(options, ptset2);
          }
          else
            WriteObjPointCloud(options, ptset);
        }

        outct++;
      }

      return outct;
    }

    private StreamWriter m_matfile;
    private string m_matfilename;

    private void CreateMaterialFile(FileObjWriteOptions options)
    {
      string locmatfilename = "";

      if (Rhino.Runtime.HostUtils.RunningOnOSX)
      {
        m_matfilename = $"{Path.GetDirectoryName(options.ActualFilePathOnMac)}/{Path.GetFileNameWithoutExtension(options.ActualFilePathOnMac)}.mtl";
        locmatfilename = $"{Path.GetFileNameWithoutExtension(options.ActualFilePathOnMac)}.mtl";
      }
      else
      {
        m_matfilename = $"{Path.GetDirectoryName(m_filename)}\\{Path.GetFileNameWithoutExtension(m_filename)}.mtl";
        locmatfilename = $"{Path.GetFileNameWithoutExtension(m_filename)}.mtl";
      }

      m_matfile = new StreamWriter(m_matfilename);

      m_matfile.Write("# Rhino");
      WriteObjeol(options, m_matfile);

      WriteObjString(options, $"mtllib {locmatfilename}");
    }


    /// <summary>
    /// 
    /// </summary>
    /// <param name="filename"></param>
    /// <param name="doc"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    public WriteFileResult WriteFile(String filename, RhinoDoc doc, FileObjWriteOptions options)
    {
      //DateTime start = DateTime.Now;
      m_rhino_doc = doc;
      m_filename = filename;

      //get some Rhino objects to work with
      var settings = new ObjectEnumeratorSettings { SelectedObjectsFilter = options.WriteOptions.WriteSelectedObjectsOnly };
      var rhino_objects = new List<RhinoObject>(doc.Objects.GetObjectList(settings));

      m_file = new StreamWriter(filename, false, System.Text.Encoding.ASCII, 65536);
      if (null == m_file)
        return BailEarly();

      WriteObjString(options, "# Rhino");
      WriteObjFlush(options);

      if (options.ExportMaterialDefinitions)
        CreateMaterialFile(options);

      if (FileObjWriteOptions.GeometryType.Mesh == options.ObjectType)
      {
        int rs = WriteMeshesToFile(rhino_objects, options);
        if (-1 == rs)
          return BailEarly(WriteFileResult.Cancel);
        else if (0 == rs)
          return BailEarly(WriteFileResult.Failure);
      }
      else
      {
        if (0 == WriteObjectsToFile(rhino_objects, options))
          return BailEarly(WriteFileResult.Failure);
      }

      doc.Views.Redraw();

      m_matfile?.Close();
      m_file.Close();

      //DateTime end = DateTime.Now;
      //RhinoApp.WriteLine($"start - {start.Hour}:{start.Minute}:{start.Second}   end - {end.Hour}:{end.Minute}:{end.Second}");

      return WriteFileResult.Success;
    }

    public WriteFileResult BailEarly()
    {
      return BailEarly(WriteFileResult.Failure);
    }

    public WriteFileResult BailEarly(WriteFileResult result)
    {
      if (null != m_file)
      {
        m_file.Close();
        FileInfo fi = new FileInfo(m_filename);
        if (fi.Exists)
          fi.Delete();
      }

      if (null != m_matfile)
      {
        m_matfile.Close();
        FileInfo fi = new FileInfo(m_matfilename);
        if (fi.Exists)
          fi.Delete();
      }

      return result;
    }

  }
}