using Rhino.Geometry;

#pragma warning disable 1591

namespace Rhino.FileIO
{
  public class FileObjWriteOptions
  {
    /// <remarks>
    /// The path passed to FileExportPlugIn::WriteFile on the Mac
    /// is a temporary path.  The calling function takes care of moving
    /// the temp (.obj) file to it's appropriate location.  We can't use
    /// that to write that path to construct the .mat file because the calling 
    /// function doesn't know anything about it.  ActualFilePathOnMac is the path
    /// of the obj file's ultimate destination.  It's only meaningful on the 
    /// Mac platform.
    /// </remarks>
    public string ActualFilePathOnMac
    {
      get;
      set;
    }

    /// <summary>
    /// End of Line
    /// </summary>
    public enum AsciiEol
    {
      /// <summary>
      /// MicroSoft
      /// </summary>
      Crlf = 0,
      /// <summary>
      /// UNIX
      /// </summary>
      Lf = 1,
      /// <summary>
      /// Apple
      /// </summary>
      Cr = 2
    };

    /// <remarks>
    /// Alias/Wavefront OBJ files are ASCII files.  
    /// Generally they should have a LF (UNIX)
    /// as the end-of-line marker.
    /// </remarks>
    public AsciiEol EolType
    {
      get;
      set;
    }


    /// <summary>
    /// Type of curve used for trimmed surfaces
    /// </summary>
    public enum CurveType
    {
      /// <summary>
      /// Polyline approximation, see comments for AngleTolRadians
      /// </summary>
      Polyline = 0,
      Nurbs = 1
    };

    /// <summary>
    /// If trim is TL_OBJ_FILE_TRIM_POLYLINE,
    /// angle_tol (in radians) controls the
    /// level of linearization on parameter
    /// space trimming curves.
    /// </summary>
    public readonly double AngleTolRadians;

    /// <summary>
    /// trimming curve option
    /// </summary>
    public CurveType TrimCurveType
    {
      get;
      set;
    }

    /// <summary>
    /// (trimmed) NURBS surfaces may be exported                              
    /// as either NURBS or meshes                              
    /// </summary>
    public enum GeometryType
    {
      Nurbs = 0,
      Mesh = 1
    };

    public GeometryType ObjectType
    {
      get;
      set;
    }

    public enum ObjObjectNames
    {
      /// <summary>
      /// Object names are not exported
      /// </summary>
      NoObjects = 0,
      /// <summary>
      /// Rhino Object names are exported as OBJ groups
      /// </summary>
      ObjectAsGroup = 1,
      /// <summary>
      /// Rhino Object names are exported as OBJ objects
      /// </summary>
      ObjectAsObject = 2
    };

    /// <summary>
    /// Setting to determine what object names in Rhino 
    /// will become in the OBJ output file
    /// </summary>
    public ObjObjectNames ExportObjectNames
    {
      get;
      set;
    }

    public enum ObjGroupNames
    {
      /// <summary>
      /// Neither layer or group names are exported as OBJ groups
      /// </summary>
      NoGroups = 0,
      /// <summary>
      /// Rhino layer names are exported as OBJ groups
      /// </summary>
      LayerAsGroup = 1,
      /// <summary>
      /// Rhino group names are exported as OBJ groups
      /// </summary>
      GroupAsGroup = 2
    };

    /// <summary>
    /// Setting to determine whether object, group or layer names
    /// will become "g"s in the OBJ output file
    /// </summary>
    public ObjGroupNames ExportGroupNameLayerNames
    {
      get;
      set;
    }


    //public enum CrvEnd
    //{
    //  CrvEndLeft = 0,
    //  CrvEndRight = 1
    //};


    /// <summary>
    /// Setting to write an .mtl file and "usemtl"s in the obj file
    /// </summary>
    public bool ExportMaterialDefinitions
    {
      get;
      set;
    }

    /// <summary>
    /// Setting to transform Rhino's Z axis to OBJ's Y axis
    /// </summary>
    public bool MapZtoY
    {
      get;
      set;
    }

    /// <summary>
    /// Number of significant digits to write out for floating point numbers
    /// </summary>
    public int SignificantDigits
    {
      get;
      set;
    }

    /// <summary>
    /// Setting to enable/disable line wrapping with "\"s
    /// </summary>
    public bool WrapLongLines
    {
      get;
      set;
    }

    /// <summary>
    /// Setting to merge nested layer or group names into a single OBJ group name
    /// </summary>
    /// <remarks>
    /// Setting this to true will take a layer setup like this in Rhino
    /// 
    /// Grandparent
    ///   Parent
    ///     Child
    ///
    /// and make a group name of Grandparent__Parent__Child in obj.
    /// 
    /// If false it will be 3 separate group names in obj and will look like this
    /// in the file.
    /// 
    /// g Grandparent Parent Child
    /// </remarks>
    public bool MergeNestedGroupingNames
    {
      get;
      set;
    }


    /// <summary>
    /// Setting to enable/disable sorting of OBJ groups
    /// </summary>
    /// <remarks>
    /// This is mainly for the sake of making an obj file more human readable,
    /// however, it is important that it happens when a user has opted to write
    /// Rhino groups as OBJ "g"s since it is not necessary for every object in Rhino
    /// to be part of a group.
    /// </remarks>
    public bool SortObjGroups
    {
      get;
      set;
    }

    /// <summary>
    /// Determines how/if vertexes of the mesh in Rhino will be modified in the output
    /// </summary>
    /// <remarks>
    /// The actual values of any vertex, normal or texture coordinate are not modified, this setting
    /// determines whether they are duplicated or merged.
    /// </remarks>
    public enum VertexWelding
    {
      /// <summary>
      /// Mesh is exported in existing state
      /// </summary>
      Normal = 0,
      /// <summary>
      /// Mesh topology vertex indexing is used for the v in the OBJ output file
      /// normals and texture coordinates, if they exist, come from the mesh
      /// </summary>
      Welded = 1,
      /// <summary>
      /// Each face gets it's own vertex, and normal and tc if they exist, in the output
      /// </summary>
      Unwelded = 2
    };

    public VertexWelding MeshType
    {
      get;
      set;
    }

    /// <summary>
    /// Setting to enable/diable the creation of ngons for the output
    /// </summary>
    public bool CreateNgons
    {
      get;
      set;
    }

    /// <summary>
    /// Setting to determine whether unwelded edges are ignored in the 
    /// creation of an ngon.
    /// </summary>
    /// <remarks>
    /// Imagine a plane that has been split and then rejoined.  This 
    /// setting determines if you will get one or twon ngons in the output.
    /// 
    /// TODO This is not hooked up yet, Dale hasn't made this available yet. 
    /// See RH-30673.
    /// </remarks>
    public bool IncludeUnweldedEdgesInNgons
    {
      get;
      set;
    }

    /// <summary>
    /// Setting to determine whether interior colinear vertexes are part of the 
    /// ngon.
    /// </summary>
    /// <remarks>
    /// Imagine a plane that has been split and then rejoined.  If IncludeUnweldedEdgesInNgons
    /// is setr to true then you would get an ngon with 6 vertexes, the corners and one on each edge 
    /// that was split.  Setting this to true would remove those 2 vertexes from the interior of the edge.
    /// 
    /// TODO This is not hooked up yet, Dale hasn't made this available yet. 
    /// See RH-30673.
    /// </remarks>
    public bool CullUnnecessaryVertexesInNgons
    {
      get;
      set;
    }

    /// <summary>
    /// Minimum number of faces to consider creation of ngon
    /// </summary>
    public int MinNgonFaceCount
    {
      get;
      set;
    }

    /// <summary>
    /// Enable/disable export of texture coordinates, if they exist.
    /// </summary>
    public bool ExportTcs
    {
      get;
      set;
    }

    /// <summary>
    /// Enable/disable export of vertex normals, if they exist.
    /// </summary>
    public bool ExportNormals
    {
      get;
      set;
    }

		/// <summary>
		/// Enable/disable export of vertex colors, if they exist.
		/// </summary>
		public bool ExportVcs
		{
			get;
			set;
		}

		/// <summary>
		/// Enable/Disable bailing when an open mesh is encountered.
		/// </summary>
		public bool ExportOpenMeshes
    {
      get;
      set;
    }

    /// <summary>
    /// Enable/disable replacing white space with underbars in material names.
    /// </summary>
    public bool UnderbarMaterialNames
    {
      get;
      set;
    }

    /// <summary>
    /// Determines whether to use the simple or detailed meshing dialog.
    /// </summary>
    public bool UseSimpleDialog
    {
      get;
      set;
    }

    /// <summary>
    /// Mesh parameters to use when meshing geometry that is not already a mesh.
    /// </summary>
    public MeshingParameters MeshParameters
    {
      get; 
      set;
    }

    /// <summary>
    /// Determines whether to use relative indexing.
    /// 
    /// TRUE = use relative (negative) indexing
    /// FALSE = use absolute (positive) indexing  
    /// </summary>
    /// <remarks>
    /// This only has an impact on non-mesh output.  There is also 
    /// no interface in the OBJ export plugin so code executed when set to
    /// true has not been thoroughly tested.  It has been there forever and migrated
    /// with each successive revision of OBJ export code.
    /// </remarks>
    public bool UseRelativeIndexing
    {
      get;
      set;
    }

    public FileWriteOptions WriteOptions;

    /// <param name="writeOptions"></param>
    public FileObjWriteOptions(FileWriteOptions writeOptions)
    {
      WriteOptions = writeOptions;
      UseRelativeIndexing = false;
      ObjectType = GeometryType.Mesh;
      TrimCurveType = CurveType.Nurbs;
      EolType = AsciiEol.Crlf;
      MapZtoY = false;
      MeshType = VertexWelding.Normal;
      ExportMaterialDefinitions = true;
      ExportTcs = true;
      ExportOpenMeshes = true;
      ExportNormals = true;
	    ExportVcs = false;
      ExportObjectNames = ObjObjectNames.NoObjects;
      ExportGroupNameLayerNames = ObjGroupNames.NoGroups;
      SortObjGroups = true;
      MergeNestedGroupingNames = false;
      SignificantDigits = 17;
      WrapLongLines = false;
      MeshParameters = MeshingParameters.Default;

      UseSimpleDialog = true;

      AngleTolRadians = RhinoMath.ToRadians(20);

      CreateNgons = true;
      MinNgonFaceCount = 2;
      IncludeUnweldedEdgesInNgons = true;
      CullUnnecessaryVertexesInNgons = true;

      UnderbarMaterialNames = false;
    }

    private void MakeYUpTransform(ref Transform xform)
    {
      xform[0, 0] = 1.0;
      xform[0, 1] = 0.0;
      xform[0, 2] = 0.0;
      xform[0, 3] = 0.0;
      xform[1, 0] = 0.0;
      xform[1, 1] = 0.0;
      xform[1, 2] = 1.0;
      xform[1, 3] = 0.0;
      xform[2, 0] = 0.0;
      xform[2, 1] = -1.0;
      xform[2, 2] = 0.0;
      xform[2, 3] = 0.0;
      xform[3, 0] = 0.0;
      xform[3, 1] = 0.0;
      xform[3, 2] = 0.0;
      xform[3, 3] = 1.0;
    }

    /// <summary>
    /// Calculates the transform combination of ZToY and
    /// any the translation that might occur in a SavewithOrigin.
    /// </summary>
    public Transform GetTransform()
    {
      Transform xform = Transform.Identity;
      if (MapZtoY)
        MakeYUpTransform(ref xform);
      xform = xform * WriteOptions.Xform;
      return xform;
    }
  }

  /// <summary>
  /// Options used when reading an obj file.
  /// </summary>
  public class FileObjReadOptions
  {
    /// <summary>
    /// Determines how "g"s in the obj file will be interpreted
    /// on import
    /// </summary>
    public enum UseObjGsAs
    {
      /// <summary>
      /// OBJ "g"s in the file are ignored
      /// </summary>
      IgnoreObjGroups = 0,
      /// <summary>
      /// OBJ "g"s in the file will become Rhino layers
      /// </summary>
      ObjGroupsAsLayers = 1,
      /// <summary>
      /// OBJ "g"s in the file will become Rhino groups
      /// </summary>
      ObjGroupsAsGroups = 2,
      /// <summary>
      /// OBJ "g"s in the file will become Rhino objects
      /// </summary>
      ObjGroupsAsObjects = 3
    };

    public UseObjGsAs UseObjGroupsAs
    {
      get;
      set;
    }

    /// <summary>
    /// Determines whether or not "o"s in the obj file
    /// will be interpreted as objects in the Rhino model
    /// </summary>
    /// <remarks> 
    /// If UseObjGsAs is OBJGroupsAsObjects this value is ignored. 
    /// </remarks>
    public bool UseObjObjects
    {
      get;
      set;
    }

    /// <summary>
    /// Setting to transform OBJ's Y axis to Rhino's Z axis
    /// </summary>
    public bool MapYtoZ
    {
      get;
      set;
    }

    /// <summary>
    /// TODO 
    /// </summary>
    public bool MorphTargetOnly
    {
      get;
      set;
    }

    /// <summary>
    /// Determines how groups/layers are nested when reading an obj file.
    /// Left to Right (default = false) or Right to Left (true)
    /// </summary>
    public bool ReverseGroupOrder
    {
      get;
      set;
    }

    /// <summary>
    /// Determines whether textures are read from the .mtl file, if it exists.
    /// </summary>
    public bool IgnoreTextures
    {
      get;
      set;
    }


    /// <summary>
    /// Determines whether textures are read from the .mtl file, if it exists.
    /// </summary>
    public bool DisplayColorFromObjMaterial
    {
      get;
      set;
    }

    public bool Split32BitTextures
    {
      get;
      set;
    }

    /// <summary>
    /// Rhino's FileReadOptions passed into ReadFile
    /// </summary>
    public FileReadOptions ReadOptions;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="readOptions"></param>
    public FileObjReadOptions(FileReadOptions readOptions)
    {
      ReadOptions = readOptions;
      UseObjGroupsAs = UseObjGsAs.ObjGroupsAsObjects;
      UseObjObjects = false;
      MapYtoZ = false;
      MorphTargetOnly = false;
      ReverseGroupOrder = false;
      IgnoreTextures = false;
      DisplayColorFromObjMaterial = true;
      Split32BitTextures = false;
    }

    private void MakeZUpTransform(ref Transform xform)
    {
      xform[0, 0] = 1.0;
      xform[0, 1] = 0.0;
      xform[0, 2] = 0.0;
      xform[0, 3] = 0.0;
      xform[1, 0] = 0.0;
      xform[1, 1] = 0.0;
      xform[1, 2] = -1.0;
      xform[1, 3] = 0.0;
      xform[2, 0] = 0.0;
      xform[2, 1] = 1.0;
      xform[2, 2] = 0.0;
      xform[2, 3] = 0.0;
      xform[3, 0] = 0.0;
      xform[3, 1] = 0.0;
      xform[3, 2] = 0.0;
      xform[3, 3] = 1.0;
    }

    /// <summary>
    /// Calculates the YToZ transform.
    /// </summary>
    /// <returns></returns>
    public Transform GetTransform()
    {
      Transform xform = Transform.Identity;
      if (MapYtoZ)
        MakeZUpTransform(ref xform);
//      xform = xform * ReadOptions.Xform;
      return xform;
    }
  }
}