#pragma warning disable 1591
using System;

namespace Rhino.FileIO
{
  ///<summary>
  ///Typecode format 4 bytes long
  ///<code>
  ///x xxxxxxxxxxxxxxx,x xxx xxxx xxxx x x xx
  ///| |               | |               | |  |
  ///|        |        |                 |
  ///|        |        |         |       +---  &quot;stuff&quot; bit
  ///|        |        |         |
  ///|        |        |         +-- specific codes
  ///|        |        |
  ///|        |        +-- RESERVED - DO NOT USE (should be 0) (will be used to control CRC on/off)
  ///|        |
  ///|        +-- category:_000 0000 0000 0001  Legacy geometry    TCODE_LEGACY_GEOMETRY
  ///|                     _000 0000 0000 0010  openNURBS object   TCODE_OPENNURBS_OBJECT
  ///|                     _000 0000 0000 0100  -- RESERVED - DO NOT USE (should be 0 in any typecode) -- 
  ///|                     _000 0000 0000 1000  -- RESERVED - DO NOT USE (should be 0 in any typecode) --                     
  ///|                     _000 0000 0001 0000  Geometry           TCODE_GEOMETRY
  ///|                     _000 0000 0010 0000  Annotation
  ///|                     _000 0000 0100 0000  Display Attributes TCODE_DISPLAY
  ///|                     _000 0000 1000 0000  Rendering          TCODE_RENDER     
  ///|                     _000 0001 0000 0000                         
  ///|                     _000 0010 0000 0000  Interface          TCODE_INTERFACE 
  ///|                     _000 0100 0000 0000  -- RESERVED - DO NOT USE (should be 0 in any typecode) --
  ///|                     _000 1000 0000 0000  Tolerances         TCODE_TOLERANCE
  ///|                     _001 0000 0000 0000  Tables             TCODE_TABLE    
  ///|                     _010 0000 0000 0000  Table record       TCODE_TABLEREC
  ///|                     _100 0000 0000 0000  User information   TCODE_USER
  ///| 
  ///+-- format: 0 - data size in header  - data block follows    TCODE_SHORT
  ///            1 - data in header - no data block follows
  ///</code>
  ///</summary>
  public static class File3dmTypeCodes
  {
    ///<summary>
    /// (0x00000001)
    /// The TCODE_COMMENTBLOCK is the first chunk in the file, starts 32 bytes into
    /// the file, and contains text information terminated with a ^m_z.  This ^m_z and
    /// contents of this chunk were expanded in February 2000.  Files written with
    /// code released earlier than this will not have the ^m_z.
    ///</summary>
    [CLSCompliant(false)]
    public const uint TCODE_COMMENTBLOCK = 0x00000001;
    ///<summary>
    /// (0x00007FFF)
    /// The TCODE_ENDOFFILE is the last chunk in the file and the first 4 bytes
    /// of information in this chunk is an integer that contains the file length.
    /// This chunk was added in February 2000 and files written with code released
    /// earlier than this will not have this termination block.
    ///</summary>
    [CLSCompliant(false)]
    public const uint TCODE_ENDOFFILE = 0x00007FFF;
    /// <summary>
    /// (0x00007FFE)
    /// this typecode is returned when a rogue eof marker is found
    /// Some v1 3dm file writers put these markers in a "goo".
    /// Simply skip these chunks and continue.
    /// </summary>
    [CLSCompliant(false)]
    public const uint TCODE_ENDOFFILE_GOO = 0x00007FFE;

    [CLSCompliant(false)]
    public const uint TCODE_LEGACY_GEOMETRY = 0x00010000;
    /// <summary>0x00020000.</summary>
    [CLSCompliant(false)]
    public const uint TCODE_OPENNURBS_OBJECT = 0x00020000;
    /// <summary>0x0010000.</summary>
    [CLSCompliant(false)]
    public const uint TCODE_GEOMETRY = 0x00100000;
    /// <summary>0x0020000.</summary>
    [CLSCompliant(false)]
    public const uint TCODE_ANNOTATION = 0x00200000;
    /// <summary>0x0040000.</summary>
    [CLSCompliant(false)]
    public const uint TCODE_DISPLAY = 0x00400000;
    /// <summary>0x0080000.</summary>
    [CLSCompliant(false)]
    public const uint TCODE_RENDER = 0x00800000;
    /// <summary>0x02000000.</summary>
    [CLSCompliant(false)]
    public const uint TCODE_INTERFACE = 0x02000000;
    /// <summary>0x08000000.</summary>
    [CLSCompliant(false)]
    public const uint TCODE_TOLERANCE = 0x08000000;
    /// <summary>0x10000000.</summary>
    [CLSCompliant(false)]
    public const uint TCODE_TABLE = 0x10000000;
    /// <summary>0x20000000.</summary>
    [CLSCompliant(false)]
    public const uint TCODE_TABLEREC = 0x20000000;
    /// <summary>0x40000000.</summary>
    [CLSCompliant(false)]
    public const uint TCODE_USER = 0x40000000;
    /// <summary>0x80000000.</summary>
    [CLSCompliant(false)]
    public const uint TCODE_SHORT = 0x80000000;
    /// <summary>0x8000.</summary>
    [CLSCompliant(false)]
    public const uint TCODE_CRC = 0x8000;
    /// <summary>(TCODE_USER | TCODE_CRC | 0x0000)</summary>
    [CLSCompliant(false)]
    public const uint TCODE_ANONYMOUS_CHUNK = (TCODE_USER | TCODE_CRC | 0x0000);

  
    /// <summary>rendering materials.</summary>
    [CLSCompliant(false)]
    public const uint TCODE_MATERIAL_TABLE = (TCODE_TABLE | 0x0010);
    /// <summary>layers.</summary>
    [CLSCompliant(false)]
    public const uint TCODE_LAYER_TABLE = (TCODE_TABLE | 0x0011);
    /// <summary>rendering lights.</summary>
    [CLSCompliant(false)]
    public const uint TCODE_LIGHT_TABLE = (TCODE_TABLE | 0x0012);
    /// <summary>geometry and annotation.</summary>
    [CLSCompliant(false)]
    public const uint TCODE_OBJECT_TABLE = (TCODE_TABLE | 0x0013);
    /// <summary>
    /// Model Properties: revision history, notes, preview image.
    /// </summary>
    [CLSCompliant(false)]
    public const uint TCODE_PROPERTIES_TABLE = (TCODE_TABLE | 0x0014);

    [CLSCompliant(false)]
    public const uint TCODE_SETTINGS_TABLE = (TCODE_TABLE | 0x0015); // file properties including,
                                                      // units, tolerancess, 
                                                      // annotation defaults, 
                                                      // render mesh defaults, 
                                                      // current layer, 
                                                      // current material,
                                                      // current color,
                                                      // named construction planes,
                                                      // named viewports,
                                                      // current viewports,

    [CLSCompliant(false)]
    public const uint TCODE_BITMAP_TABLE = (TCODE_TABLE | 0x0016); // embedded bitmaps
    [CLSCompliant(false)]
    public const uint TCODE_USER_TABLE = (TCODE_TABLE | 0x0017); // user table

    [CLSCompliant(false)]
    public const uint TCODE_GROUP_TABLE = (TCODE_TABLE | 0x0018); // group table

    [CLSCompliant(false)]
    public const uint TCODE_FONT_TABLE = (TCODE_TABLE | 0x0019); // annotation font table
    [CLSCompliant(false)]
    public const uint TCODE_DIMSTYLE_TABLE = (TCODE_TABLE | 0x0020); // annotation dimension style table

    [CLSCompliant(false)]
    public const uint TCODE_INSTANCE_DEFINITION_TABLE = (TCODE_TABLE | 0x0021); // instance definition table

    [CLSCompliant(false)]
    public const uint TCODE_HATCHPATTERN_TABLE = (TCODE_TABLE | 0x0022); // hatch pattern table

    [CLSCompliant(false)]
    public const uint TCODE_LINETYPE_TABLE = (TCODE_TABLE | 0x0023); // linetype table

    [CLSCompliant(false)]
    public const uint TCODE_OBSOLETE_LAYERSET_TABLE = (TCODE_TABLE | 0x0024); // obsolete layer set table

    [CLSCompliant(false)]
    public const uint TCODE_TEXTURE_MAPPING_TABLE = (TCODE_TABLE | 0x0025); // texture mappings

    [CLSCompliant(false)]
    public const uint TCODE_HISTORYRECORD_TABLE = (TCODE_TABLE | 0x0026); // history records

    [CLSCompliant(false)]
    public const uint TCODE_ENDOFTABLE = 0xFFFFFFFF;

//records in properties table
    [CLSCompliant(false)]
    public const uint TCODE_PROPERTIES_REVISIONHISTORY = (TCODE_TABLEREC | TCODE_CRC | 0x0021);
    [CLSCompliant(false)]
    public const uint TCODE_PROPERTIES_NOTES = (TCODE_TABLEREC | TCODE_CRC | 0x0022);
    [CLSCompliant(false)]
    public const uint TCODE_PROPERTIES_PREVIEWIMAGE = (TCODE_TABLEREC | TCODE_CRC | 0x0023);
    [CLSCompliant(false)]
    public const uint TCODE_PROPERTIES_APPLICATION = (TCODE_TABLEREC | TCODE_CRC | 0x0024);
    [CLSCompliant(false)]
    public const uint TCODE_PROPERTIES_COMPRESSED_PREVIEWIMAGE = (TCODE_TABLEREC | TCODE_CRC | 0x0025);
    [CLSCompliant(false)]
    public const uint TCODE_PROPERTIES_OPENNURBS_VERSION = (TCODE_TABLEREC | TCODE_SHORT | 0x0026);

// records in settings table
    [CLSCompliant(false)]
    public const uint TCODE_SETTINGS_PLUGINLIST = (TCODE_TABLEREC | TCODE_CRC | 0x0135);
    [CLSCompliant(false)]
    public const uint TCODE_SETTINGS_UNITSANDTOLS = (TCODE_TABLEREC | TCODE_CRC | 0x0031);
    [CLSCompliant(false)]
    public const uint TCODE_SETTINGS_RENDERMESH = (TCODE_TABLEREC | TCODE_CRC | 0x0032);
    [CLSCompliant(false)]
    public const uint TCODE_SETTINGS_ANALYSISMESH = (TCODE_TABLEREC | TCODE_CRC | 0x0033);
    [CLSCompliant(false)]
    public const uint TCODE_SETTINGS_ANNOTATION = (TCODE_TABLEREC | TCODE_CRC | 0x0034);
    [CLSCompliant(false)]
    public const uint TCODE_SETTINGS_NAMED_CPLANE_LIST = (TCODE_TABLEREC | TCODE_CRC | 0x0035);
    [CLSCompliant(false)]
    public const uint TCODE_SETTINGS_NAMED_VIEW_LIST = (TCODE_TABLEREC | TCODE_CRC | 0x0036);
    [CLSCompliant(false)]
    public const uint TCODE_SETTINGS_VIEW_LIST = (TCODE_TABLEREC | TCODE_CRC | 0x0037);
    [CLSCompliant(false)]
    public const uint TCODE_SETTINGS_CURRENT_LAYER_INDEX = (TCODE_TABLEREC | TCODE_SHORT | 0x0038);
    [CLSCompliant(false)]
    public const uint TCODE_SETTINGS_CURRENT_MATERIAL_INDEX = (TCODE_TABLEREC | TCODE_CRC | 0x0039);
    [CLSCompliant(false)]
    public const uint TCODE_SETTINGS_CURRENT_COLOR = (TCODE_TABLEREC | TCODE_CRC | 0x003A);
    [CLSCompliant(false)]
    public const uint TCODE_SETTINGS__NEVER__USE__THIS = (TCODE_TABLEREC | TCODE_CRC | 0x003E);
    [CLSCompliant(false)]
    public const uint TCODE_SETTINGS_CURRENT_WIRE_DENSITY = (TCODE_TABLEREC | TCODE_SHORT | 0x003C);
    [CLSCompliant(false)]
    public const uint TCODE_SETTINGS_RENDER = (TCODE_TABLEREC | TCODE_CRC | 0x003D);
    [CLSCompliant(false)]
    public const uint TCODE_SETTINGS_GRID_DEFAULTS = (TCODE_TABLEREC | TCODE_CRC | 0x003F);
    [CLSCompliant(false)]
    public const uint TCODE_SETTINGS_MODEL_URL = (TCODE_TABLEREC | TCODE_CRC | 0x0131);
    [CLSCompliant(false)]
    public const uint TCODE_SETTINGS_CURRENT_FONT_INDEX = (TCODE_TABLEREC | TCODE_SHORT | 0x0132);
    [CLSCompliant(false)]
    public const uint TCODE_SETTINGS_CURRENT_DIMSTYLE_INDEX = (TCODE_TABLEREC | TCODE_SHORT | 0x0133);
// added 29 October 2002 as a chunk to hold new and future ON_3dmSettings information
    [CLSCompliant(false)]
    public const uint TCODE_SETTINGS_ATTRIBUTES = (TCODE_TABLEREC | TCODE_CRC | 0x0134);


// views are subrecords in the settings table
    [CLSCompliant(false)]
    public const uint TCODE_VIEW_RECORD = (TCODE_TABLEREC | TCODE_CRC | 0x003B);
// subrecords if view record
    [CLSCompliant(false)]
    public const uint TCODE_VIEW_CPLANE = (TCODE_TABLEREC | TCODE_CRC | 0x013B);
    [CLSCompliant(false)]
    public const uint TCODE_VIEW_VIEWPORT = (TCODE_TABLEREC | TCODE_CRC | 0x023B);
    [CLSCompliant(false)]
    public const uint TCODE_VIEW_SHOWCONGRID = (TCODE_TABLEREC | TCODE_SHORT | 0x033B);
    [CLSCompliant(false)]
    public const uint TCODE_VIEW_SHOWCONAXES = (TCODE_TABLEREC | TCODE_SHORT | 0x043B);
    [CLSCompliant(false)]
    public const uint TCODE_VIEW_SHOWWORLDAXES = (TCODE_TABLEREC | TCODE_SHORT | 0x053B);
    [CLSCompliant(false)]
    public const uint TCODE_VIEW_TRACEIMAGE = (TCODE_TABLEREC | TCODE_CRC | 0x063B);
    [CLSCompliant(false)]
    public const uint TCODE_VIEW_WALLPAPER = (TCODE_TABLEREC | TCODE_CRC | 0x073B);
    [CLSCompliant(false)]
    public const uint TCODE_VIEW_WALLPAPER_V3 = (TCODE_TABLEREC | TCODE_CRC | 0x074B);
    [CLSCompliant(false)]
    public const uint TCODE_VIEW_TARGET = (TCODE_TABLEREC | TCODE_CRC | 0x083B);
    [CLSCompliant(false)]
    public const uint TCODE_VIEW_DISPLAYMODE = (TCODE_TABLEREC | TCODE_SHORT | 0x093B);
    [CLSCompliant(false)]
    public const uint TCODE_VIEW_NAME = (TCODE_TABLEREC | TCODE_CRC | 0x0A3B);
    [CLSCompliant(false)]
    public const uint TCODE_VIEW_POSITION = (TCODE_TABLEREC | TCODE_CRC | 0x0B3B);

// added 29 October 2002 as a chunk to hold new and future ON_3dmView information
    [CLSCompliant(false)]
    public const uint TCODE_VIEW_ATTRIBUTES = (TCODE_TABLEREC | TCODE_CRC | 0x0C3B);

// added 27 June 2008 as a chunk to hold userdata on ON_Viewports saved in named view list
    [CLSCompliant(false)]
    public const uint TCODE_VIEW_VIEWPORT_USERDATA = (TCODE_TABLEREC | TCODE_CRC | 0x0D3B);

// records in bitmap table
    [CLSCompliant(false)]
    public const uint TCODE_BITMAP_RECORD = (TCODE_TABLEREC | TCODE_CRC | 0x0090); // material table record derived from ON_Bitmap

// records in material table
    [CLSCompliant(false)]
    public const uint TCODE_MATERIAL_RECORD = (TCODE_TABLEREC | TCODE_CRC | 0x0040); // material table record derived from ON_Material

// records in layer table
    [CLSCompliant(false)]
    public const uint TCODE_LAYER_RECORD = (TCODE_TABLEREC | TCODE_CRC | 0x0050); // layer table record derived from ON_Layer

// records in light table
    [CLSCompliant(false)]
    public const uint TCODE_LIGHT_RECORD = (TCODE_TABLEREC | TCODE_CRC | 0x0060);    // light table record derived from ON_Light
    [CLSCompliant(false)]
    public const uint TCODE_LIGHT_RECORD_ATTRIBUTES = (TCODE_INTERFACE | TCODE_CRC | 0x0061); // ON_3dmObjectAttributes chunk
    [CLSCompliant(false)]
    public const uint TCODE_LIGHT_RECORD_ATTRIBUTES_USERDATA = (TCODE_INTERFACE | 0x0062);      // ON_3dmObjectAttributes userdata chunk

    [CLSCompliant(false)]
    public const uint TCODE_LIGHT_RECORD_END = (TCODE_INTERFACE | TCODE_SHORT | 0x006F);

// records in user table
    [CLSCompliant(false)]
    public const uint TCODE_USER_TABLE_UUID = (TCODE_TABLEREC | TCODE_CRC | 0x0080); //table id
    [CLSCompliant(false)]
    public const uint TCODE_USER_RECORD = (TCODE_TABLEREC | 0x0081); // records in user table are in TCODE_USER_RECORD chunks

// records in group table
    [CLSCompliant(false)]
    public const uint TCODE_GROUP_RECORD = (TCODE_TABLEREC | TCODE_CRC | 0x0073);

// records in font table
    [CLSCompliant(false)]
    public const uint TCODE_FONT_RECORD = (TCODE_TABLEREC | TCODE_CRC | 0x0074);

// records in dimension style table
    [CLSCompliant(false)]
    public const uint TCODE_DIMSTYLE_RECORD = (TCODE_TABLEREC | TCODE_CRC | 0x0075);

// records in instance definition table
    [CLSCompliant(false)]
    public const uint TCODE_INSTANCE_DEFINITION_RECORD = (TCODE_TABLEREC | TCODE_CRC | 0x0076);

// records in hatch pattern table
    [CLSCompliant(false)]
    public const uint TCODE_HATCHPATTERN_RECORD = (TCODE_TABLEREC | TCODE_CRC | 0x0077);

// records in linetye pattern table
    [CLSCompliant(false)]
    public const uint TCODE_LINETYPE_RECORD = (TCODE_TABLEREC | TCODE_CRC | 0x0078);

// OBSOLETE records in layer set table
    [CLSCompliant(false)]
    public const uint TCODE_OBSOLETE_LAYERSET_RECORD = (TCODE_TABLEREC | TCODE_CRC | 0x0079);

// records in linetye pattern table
    [CLSCompliant(false)]
    public const uint TCODE_TEXTURE_MAPPING_RECORD = (TCODE_TABLEREC | TCODE_CRC | 0x007A);

// records in history record pattern table
    [CLSCompliant(false)]
    public const uint TCODE_HISTORYRECORD_RECORD = (TCODE_TABLEREC | TCODE_CRC | 0x007B);

// records in object table
    [CLSCompliant(false)]
    public const uint TCODE_OBJECT_RECORD = (TCODE_TABLEREC | TCODE_CRC | 0x0070);
    [CLSCompliant(false)]
    public const uint TCODE_OBJECT_RECORD_TYPE = (TCODE_INTERFACE | TCODE_SHORT | 0x0071);   // ON::ObjectType value
    [CLSCompliant(false)]
    public const uint TCODE_OBJECT_RECORD_ATTRIBUTES = (TCODE_INTERFACE | TCODE_CRC | 0x0072);   // ON_3dmObjectAttributes chunk
    [CLSCompliant(false)]
    public const uint TCODE_OBJECT_RECORD_ATTRIBUTES_USERDATA = (TCODE_INTERFACE | 0x0073);        // ON_3dmObjectAttributes userdata chunk 
    [CLSCompliant(false)]
    public const uint TCODE_OBJECT_RECORD_HISTORY = (TCODE_INTERFACE | TCODE_CRC | 0x0074);   // construction history 
    [CLSCompliant(false)]
    public const uint TCODE_OBJECT_RECORD_HISTORY_HEADER = (TCODE_INTERFACE | TCODE_CRC | 0x0075); // construction history header
    [CLSCompliant(false)]
    public const uint TCODE_OBJECT_RECORD_HISTORY_DATA = (TCODE_INTERFACE | TCODE_CRC | 0x0076); // construction history data
    [CLSCompliant(false)]
    public const uint TCODE_OBJECT_RECORD_END = (TCODE_INTERFACE | TCODE_SHORT | 0x007F);


/////////////////////////////////////////////////////////////////////////////////////
//
// TCODE_OBJECT_RECORD
//   4 byte length of entire object record
//
//   TCODE_OBJECT_RECORD_TYPE required - used to quickly filter and skip unwanted objects
//     4 byte ON::ObjectType
//
//   TCODE_OPENNURBS_CLASS
//     4 byte length
//     TCODE_OPENNURBS_CLASS_UUID
//       4 byte length = 20
//       value of ON_ClassId::m_uuid for this class
//       4 byte CRC
//     TCODE_OPENNURBS_CLASS_DATA
//       4 byte length 
//       class specific data for geometry or annotation object
//       4 byte CRC
//     TCODE_OPENNURBS_CLASS_USERDATA (1 chunk per piece of user data)
//       4 byte length
//       2 byte chunk version 2.1
//       TCODE_OPENNURBS_CLASS_USERDATA_HEADER
//         4 byte length
//         16 byte value of ON_ClassId::m_uuid for this child class of ON_UserData
//         16 byte value of ON_UserData::m_userdata_uuid
//         4 byte value of ON_UserData::m_userdata_copycount
//         128 byte value of ON_UserData::m_userdata_xform
//         16 byte value of  ON_UserData::m_application_uuid (in ver 2.1 chunks)
//       TCODE_ANONYMOUS_CHUNK
//         4 byte length
//         specific user data
//     TCODE_OPENNURBS_CLASS_END
//
//   TCODE_OBJECT_RECORD_ATTRIBUTES (optional)
//     4 byte length
//     ON_3dmObjectAttributes information
//     4 byte crc
//
//   TCODE_OBJECT_RECORD_ATTRIBUTES_USERDATA (optional)
//     4 byte length
//     TCODE_OPENNURBS_CLASS_USERDATA (1 chunk per piece of user data)
//       4 byte length
//       2 byte chunk version 2.1
//       TCODE_OPENNURBS_CLASS_USERDATA_HEADER
//         4 byte length
//         16 byte value of ON_ClassId::m_uuid for this child class of ON_UserData
//         16 byte value of ON_UserData::m_userdata_uuid
//         4 byte value of ON_UserData::m_userdata_copycount
//         128 byte value of ON_UserData::m_userdata_xform
//         16 byte value of  ON_UserData::m_application_uuid (in ver 2.1 chunks)
//       TCODE_ANONYMOUS_CHUNK
//         4 byte length
//         specific user data
//
//   TCODE_OBJECT_RECORD_HISTORY (optional) construction history
//     4 byte length
//     2 byte chunk version
//     TCODE_OBJECT_RECORD_HISTORY_HEADER
//       4 byte length
//       2 byte chunk version 
//       ...
//       4 byte crc
//     TCODE_OBJECT_RECORD_HISTORY_DATA
//       4 byte length
//       2 byte chunk version 
//       ...
//       4 byte crc
//
//   TCODE_OBJECT_RECORD_END required - marks end of object record
//
/////////////////////////////////////////////////////////////////////////////////////


    [CLSCompliant(false)]
    public const uint TCODE_OPENNURBS_CLASS = (TCODE_OPENNURBS_OBJECT | 0x7FFA);
    [CLSCompliant(false)]
    public const uint TCODE_OPENNURBS_CLASS_UUID = (TCODE_OPENNURBS_OBJECT | TCODE_CRC | 0x7FFB);
    [CLSCompliant(false)]
    public const uint TCODE_OPENNURBS_CLASS_DATA = (TCODE_OPENNURBS_OBJECT | TCODE_CRC | 0x7FFC);
    [CLSCompliant(false)]
    public const uint TCODE_OPENNURBS_CLASS_USERDATA = (TCODE_OPENNURBS_OBJECT | 0x7FFD);
    [CLSCompliant(false)]
    public const uint TCODE_OPENNURBS_CLASS_USERDATA_HEADER = (TCODE_OPENNURBS_OBJECT | TCODE_CRC | 0x7FF9);
    [CLSCompliant(false)]
    public const uint TCODE_OPENNURBS_CLASS_END = (TCODE_OPENNURBS_OBJECT | TCODE_SHORT | 0x7FFF);


/////////////////////////////////////////////////////////////////////////////////////
//
// TCODE_OPENNURBS_CLASS
// length of entire openNURBS class object chunk
//
//   TCODE_OPENNURBS_CLASS_UUID
//   length of uuid (16 byte UUID + 4 byte CRC)
//   16 byte UUID ( a.k.a. GUID ) openNURBS class ID - determines specific openNURBS class
//   4 bytes (32 bit CRC of the UUID)
//
//   TCODE_OPENNURBS_CLASS_DATA
//   length of object data
//   ... data that defines object
//       use ON_classname::Read() to read this data and ON_classname::Write()
//       to write this data
//   4 bytes (32 bit CRC of the object data)
//
//   TCODE_OPENNURBS_CLASS_USERDATA ( 0 or more user data chunks)
//
//   TCODE_OPENNURBS_CLASS_END
//   4 bytes = 0
//
/////////////////////////////////////////////////////////////////////////////////////



/////////////////////////////////////////////////////////////////////////////////////
//
//
//  The TCODEs below were used in the version 1 file format and are needed so that
//  the these files can be read and (optionally) written by the current OpenNURBS
//  toolkit.
//
//
/////////////////////////////////////////////////////////////////////////////////////



    [CLSCompliant(false)]
    public const uint TCODE_ANNOTATION_SETTINGS = (TCODE_ANNOTATION | 0x0001);

    [CLSCompliant(false)]
    public const uint TCODE_TEXT_BLOCK = (TCODE_ANNOTATION | 0x0004);
    [CLSCompliant(false)]
    public const uint TCODE_ANNOTATION_LEADER = (TCODE_ANNOTATION | 0x0005);
    [CLSCompliant(false)]
    public const uint TCODE_LINEAR_DIMENSION = (TCODE_ANNOTATION | 0x0006);
    [CLSCompliant(false)]
    public const uint TCODE_ANGULAR_DIMENSION = (TCODE_ANNOTATION | 0x0007);
    [CLSCompliant(false)]
    public const uint TCODE_RADIAL_DIMENSION = (TCODE_ANNOTATION | 0x0008);

// old RhinoIO toolkit (pre February 2000) defines
    [CLSCompliant(false)]
    public const uint TCODE_RHINOIO_OBJECT_NURBS_CURVE = (TCODE_OPENNURBS_OBJECT | 0x0008); // old CRhinoNurbsCurve  
    [CLSCompliant(false)]
    public const uint TCODE_RHINOIO_OBJECT_NURBS_SURFACE = (TCODE_OPENNURBS_OBJECT | 0x0009); // old CRhinoNurbsSurface
    [CLSCompliant(false)]
    public const uint TCODE_RHINOIO_OBJECT_BREP = (TCODE_OPENNURBS_OBJECT | 0x000B); // old CRhinoBrep        
    [CLSCompliant(false)]
    public const uint TCODE_RHINOIO_OBJECT_DATA = (TCODE_OPENNURBS_OBJECT | 0xFFFE); // obsolete - don't confuse with TCODE_OPENNURBS_OBJECT_DATA
    [CLSCompliant(false)]
    public const uint TCODE_RHINOIO_OBJECT_END = (TCODE_OPENNURBS_OBJECT | 0xFFFF); // obsolete - don't confuse with TCODE_OPENNURBS_OBJECT_END


// legacy objects from RhinoCommon 1.x 
    [CLSCompliant(false)]
    public const uint TCODE_LEGACY_ASM = (TCODE_LEGACY_GEOMETRY | 0x0001);
    [CLSCompliant(false)]
    public const uint TCODE_LEGACY_PRT = (TCODE_LEGACY_GEOMETRY | 0x0002);
    [CLSCompliant(false)]
    public const uint TCODE_LEGACY_SHL = (TCODE_LEGACY_GEOMETRY | 0x0003);
    [CLSCompliant(false)]
    public const uint TCODE_LEGACY_FAC = (TCODE_LEGACY_GEOMETRY | 0x0004);
    [CLSCompliant(false)]
    public const uint TCODE_LEGACY_BND = (TCODE_LEGACY_GEOMETRY | 0x0005);
    [CLSCompliant(false)]
    public const uint TCODE_LEGACY_TRM = (TCODE_LEGACY_GEOMETRY | 0x0006);
    [CLSCompliant(false)]
    public const uint TCODE_LEGACY_SRF = (TCODE_LEGACY_GEOMETRY | 0x0007);
    [CLSCompliant(false)]
    public const uint TCODE_LEGACY_CRV = (TCODE_LEGACY_GEOMETRY | 0x0008);
    [CLSCompliant(false)]
    public const uint TCODE_LEGACY_SPL = (TCODE_LEGACY_GEOMETRY | 0x0009);
    [CLSCompliant(false)]
    public const uint TCODE_LEGACY_PNT = (TCODE_LEGACY_GEOMETRY | 0x000A);

    [CLSCompliant(false)]
    public const uint TCODE_STUFF = 0x0100;

    [CLSCompliant(false)]
    public const uint TCODE_LEGACY_ASMSTUFF = (TCODE_LEGACY_GEOMETRY | TCODE_STUFF | TCODE_LEGACY_ASM);
    [CLSCompliant(false)]
    public const uint TCODE_LEGACY_PRTSTUFF = (TCODE_LEGACY_GEOMETRY | TCODE_STUFF | TCODE_LEGACY_PRT);
    [CLSCompliant(false)]
    public const uint TCODE_LEGACY_SHLSTUFF = (TCODE_LEGACY_GEOMETRY | TCODE_STUFF | TCODE_LEGACY_SHL);
    [CLSCompliant(false)]
    public const uint TCODE_LEGACY_FACSTUFF = (TCODE_LEGACY_GEOMETRY | TCODE_STUFF | TCODE_LEGACY_FAC);
    [CLSCompliant(false)]
    public const uint TCODE_LEGACY_BNDSTUFF = (TCODE_LEGACY_GEOMETRY | TCODE_STUFF | TCODE_LEGACY_BND);
    [CLSCompliant(false)]
    public const uint TCODE_LEGACY_TRMSTUFF = (TCODE_LEGACY_GEOMETRY | TCODE_STUFF | TCODE_LEGACY_TRM);
    [CLSCompliant(false)]
    public const uint TCODE_LEGACY_SRFSTUFF = (TCODE_LEGACY_GEOMETRY | TCODE_STUFF | TCODE_LEGACY_SRF);
    [CLSCompliant(false)]
    public const uint TCODE_LEGACY_CRVSTUFF = (TCODE_LEGACY_GEOMETRY | TCODE_STUFF | TCODE_LEGACY_CRV);
    [CLSCompliant(false)]
    public const uint TCODE_LEGACY_SPLSTUFF = (TCODE_LEGACY_GEOMETRY | TCODE_STUFF | TCODE_LEGACY_SPL);
    [CLSCompliant(false)]
    public const uint TCODE_LEGACY_PNTSTUFF = (TCODE_LEGACY_GEOMETRY | TCODE_STUFF | TCODE_LEGACY_PNT);

// legacy objects from RhinoCommon 1.x
    [CLSCompliant(false)]
    public const uint TCODE_RH_POINT = (TCODE_GEOMETRY | 0x0001);

    [CLSCompliant(false)]
    public const uint TCODE_RH_SPOTLIGHT = (TCODE_RENDER | 0x0001);

    [CLSCompliant(false)]
    public const uint TCODE_OLD_RH_TRIMESH = (TCODE_GEOMETRY | 0x0011);
    [CLSCompliant(false)]
    public const uint TCODE_OLD_MESH_VERTEX_NORMALS = (TCODE_GEOMETRY | 0x0012);
    [CLSCompliant(false)]
    public const uint TCODE_OLD_MESH_UV = (TCODE_GEOMETRY | 0x0013);
    [CLSCompliant(false)]
    public const uint TCODE_OLD_FULLMESH = (TCODE_GEOMETRY | 0x0014);


    [CLSCompliant(false)]
    public const uint TCODE_MESH_OBJECT = (TCODE_GEOMETRY | 0x0015);
    [CLSCompliant(false)]
    public const uint TCODE_COMPRESSED_MESH_GEOMETRY = (TCODE_GEOMETRY | 0x0017);
    [CLSCompliant(false)]
    public const uint TCODE_ANALYSIS_MESH = (TCODE_GEOMETRY | 0x0018);

    [CLSCompliant(false)]
    public const uint TCODE_NAME = (TCODE_INTERFACE | 0x0001);
    [CLSCompliant(false)]
    public const uint TCODE_VIEW = (TCODE_INTERFACE | 0x0002);
    [CLSCompliant(false)]
    public const uint TCODE_CPLANE = (TCODE_INTERFACE | 0x0003);

    [CLSCompliant(false)]
    public const uint TCODE_NAMED_CPLANE = (TCODE_INTERFACE | 0x0004);
    [CLSCompliant(false)]
    public const uint TCODE_NAMED_VIEW = (TCODE_INTERFACE | 0x0005);
    [CLSCompliant(false)]
    public const uint TCODE_VIEWPORT = (TCODE_INTERFACE | 0x0006);

    [CLSCompliant(false)]
    public const uint TCODE_SHOWGRID = (TCODE_SHORT | TCODE_INTERFACE | 0x0007);
    [CLSCompliant(false)]
    public const uint TCODE_SHOWGRIDAXES = (TCODE_SHORT | TCODE_INTERFACE | 0x0008);
    [CLSCompliant(false)]
    public const uint TCODE_SHOWWORLDAXES = (TCODE_SHORT | TCODE_INTERFACE | 0x0009);

    [CLSCompliant(false)]
    public const uint TCODE_VIEWPORT_POSITION = (TCODE_INTERFACE | 0x000A);
    [CLSCompliant(false)]
    public const uint TCODE_VIEWPORT_TRACEINFO = (TCODE_INTERFACE | 0x000B);
    [CLSCompliant(false)]
    public const uint TCODE_SNAPSIZE = (TCODE_INTERFACE | 0x000C);
    [CLSCompliant(false)]
    public const uint TCODE_NEAR_CLIP_PLANE = (TCODE_INTERFACE | 0x000D);
    [CLSCompliant(false)]
    public const uint TCODE_HIDE_TRACE = (TCODE_INTERFACE | 0x000E);

    [CLSCompliant(false)]
    public const uint TCODE_NOTES = (TCODE_INTERFACE | 0x000F);
    [CLSCompliant(false)]
    public const uint TCODE_UNIT_AND_TOLERANCES = (TCODE_INTERFACE | 0x0010);

    [CLSCompliant(false)]
    public const uint TCODE_MAXIMIZED_VIEWPORT = (TCODE_SHORT | TCODE_INTERFACE | 0x0011);
    [CLSCompliant(false)]
    public const uint TCODE_VIEWPORT_WALLPAPER = (TCODE_INTERFACE | 0x0012);


    [CLSCompliant(false)]
    public const uint TCODE_SUMMARY = (TCODE_INTERFACE | 0x0013);
    [CLSCompliant(false)]
    public const uint TCODE_BITMAPPREVIEW = (TCODE_INTERFACE | 0x0014);
    [CLSCompliant(false)]
    public const uint TCODE_VIEWPORT_DISPLAY_MODE = (TCODE_SHORT | TCODE_INTERFACE | 0x0015);


    [CLSCompliant(false)]
    public const uint TCODE_LAYERTABLE = (TCODE_SHORT | TCODE_TABLE | 0x0001); // obsolete - do not use
    [CLSCompliant(false)]
    public const uint TCODE_LAYERREF = (TCODE_SHORT | TCODE_TABLEREC | 0x0001);

    [CLSCompliant(false)]
    public const uint TCODE_XDATA = (TCODE_USER | 0x0001);

    [CLSCompliant(false)]
    public const uint TCODE_RGB = (TCODE_SHORT | TCODE_DISPLAY | 0x0001);
    [CLSCompliant(false)]
    public const uint TCODE_TEXTUREMAP = (TCODE_DISPLAY | 0x0002);
    [CLSCompliant(false)]
    public const uint TCODE_BUMPMAP = (TCODE_DISPLAY | 0x0003);
    [CLSCompliant(false)]
    public const uint TCODE_TRANSPARENCY = (TCODE_SHORT | TCODE_DISPLAY | 0x0004);
    [CLSCompliant(false)]
    public const uint TCODE_DISP_AM_RESOLUTION = (TCODE_SHORT | TCODE_DISPLAY | 0x0005);
    [CLSCompliant(false)]
    public const uint TCODE_RGBDISPLAY = (TCODE_SHORT | TCODE_DISPLAY | 0x0006);  // will be used for color by object
    [CLSCompliant(false)]
    public const uint TCODE_RENDER_MATERIAL_ID = (TCODE_DISPLAY | 0x0007);                  // id for render material

    [CLSCompliant(false)]
    public const uint TCODE_LAYER = (TCODE_DISPLAY | 0x0010);

// obsolete layer typecodes from earlier betas - not used anymore
    [CLSCompliant(false)]
    public const uint TCODE_LAYER_OBSELETE_1 = (TCODE_SHORT | TCODE_DISPLAY | 0x0013);
    [CLSCompliant(false)]
    public const uint TCODE_LAYER_OBSELETE_2 = (TCODE_SHORT | TCODE_DISPLAY | 0x0014);
    [CLSCompliant(false)]
    public const uint TCODE_LAYER_OBSELETE_3 = (TCODE_SHORT | TCODE_DISPLAY | 0x0015);

// these were only ever used by AccuModel and never by RhinoCommon
    [CLSCompliant(false)]
    public const uint TCODE_LAYERON = (TCODE_SHORT | TCODE_DISPLAY | 0x0016);
    [CLSCompliant(false)]
    public const uint TCODE_LAYERTHAWED = (TCODE_SHORT | TCODE_DISPLAY | 0x0017);
    [CLSCompliant(false)]
    public const uint TCODE_LAYERLOCKED = (TCODE_SHORT | TCODE_DISPLAY | 0x0018);


    [CLSCompliant(false)]
    public const uint TCODE_LAYERVISIBLE = (TCODE_SHORT | TCODE_DISPLAY | 0x0012);
    [CLSCompliant(false)]
    public const uint TCODE_LAYERPICKABLE = (TCODE_SHORT | TCODE_DISPLAY | 0x0030);
    [CLSCompliant(false)]
    public const uint TCODE_LAYERSNAPABLE = (TCODE_SHORT | TCODE_DISPLAY | 0x0031);
    [CLSCompliant(false)]
    public const uint TCODE_LAYERRENDERABLE = (TCODE_SHORT | TCODE_DISPLAY | 0x0032);


// use LAYERSTATE ( 0 = LAYER_ON, 1 = LAYER_OFF, 2 = LAYER_LOCKED ) instead of above individual toggles 
    [CLSCompliant(false)]
    public const uint TCODE_LAYERSTATE = (TCODE_SHORT | TCODE_DISPLAY | 0x0033);
    [CLSCompliant(false)]
    public const uint TCODE_LAYERINDEX = (TCODE_SHORT | TCODE_DISPLAY | 0x0034);
    [CLSCompliant(false)]
    public const uint TCODE_LAYERMATERIALINDEX = (TCODE_SHORT | TCODE_DISPLAY | 0x0035);

    [CLSCompliant(false)]
    public const uint TCODE_RENDERMESHPARAMS = (TCODE_DISPLAY | 0x0020); // block of parameters for render meshes



    [CLSCompliant(false)]
    public const uint TCODE_DISP_CPLINES = (TCODE_SHORT | TCODE_DISPLAY | 0x0022);
    [CLSCompliant(false)]
    public const uint TCODE_DISP_MAXLENGTH = (TCODE_DISPLAY | 0x0023);

    [CLSCompliant(false)]
    public const uint TCODE_CURRENTLAYER = (TCODE_SHORT | TCODE_DISPLAY | 0x0025);

    [CLSCompliant(false)]
    public const uint TCODE_LAYERNAME = (TCODE_DISPLAY | 0x0011);

    [CLSCompliant(false)]
    public const uint TCODE_LEGACY_TOL_FIT = (TCODE_TOLERANCE | 0x0001);
    [CLSCompliant(false)]
    public const uint TCODE_LEGACY_TOL_ANGLE = (TCODE_TOLERANCE | 0x0002);

    [CLSCompliant(false)]
    public const uint TCODE_DICTIONARY = (TCODE_USER | TCODE_CRC | 0x0010);
    [CLSCompliant(false)]
    public const uint TCODE_DICTIONARY_ID = (TCODE_USER | TCODE_CRC | 0x0011);
    [CLSCompliant(false)]
    public const uint TCODE_DICTIONARY_ENTRY = (TCODE_USER | TCODE_CRC | 0x0012);
    [CLSCompliant(false)]
    public const uint TCODE_DICTIONARY_END = (TCODE_USER | TCODE_SHORT | 0x0013);
  }
}
