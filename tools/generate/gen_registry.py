"""The generator's single source of truth: the class registry, every
C#-type <-> C++ / C-layer table, and the C#-type helpers. Planner and
emitters `from gen_registry import *`; tables filled at run time
(register_value_types, extend_enum_table) are mutated in place."""

import glob
import json
import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

# ---------------------------------------------------------------- registry
CLASSES = [
    # order matters: a class whose methods return another generated class must
    # come AFTER it (inline bodies need the complete type), and abstract bases
    # come before every class deriving them.
    # mode 'pointer': heap-owned ON_X* self; 'value': ON_X held by value
    # (RhinoCommon structs like Transform, whose members pass `ref this`);
    # 'abstract': a generated BASE class -- no storage, no constructor, its
    # members reach self through a pure-virtual getter each derived class
    # overrides with its typed pointer. 'base' on a derived entry names the
    # abstract class it extends, mirroring the RHINOCOMMON hierarchy (the
    # spec), not opennurbs's (ON_Viewport is ON_Geometry there, but
    # ViewportInfo is not a GeometryBase in .NET).
    # 'copy' is the expression constructing this class's storage from the
    # hand-written BND source object.
    # the root (G62): RhinoCommon's CommonObject over every class whose native
    # object is an ON_Object. An abstract base may itself extend an abstract base:
    # it answers the base's getters with its own. CachedTextureCoordinates is a
    # CommonObject in .NET but wraps ON_TextureCoordinates (no ON_Object), so it
    # does not extend this (pilot-findings #31)
    {'type': 'Rhino.Runtime.CommonObject', 'cs': 'CommonObject',
     'on': 'ON_Object',
     'mode': 'abstract', 'getter': 'RH3DM_ObjPtr',
     'gen': 'BND_CommonObjectX', 'reg': 'CommonObjectX'},
    {'type': 'Rhino.Geometry.GeometryBase', 'cs': 'GeometryBase',
     'on': 'ON_Geometry',
     'mode': 'abstract', 'getter': 'RH3DM_GeomPtr',
     'self_extra': ['ON_Object'],
     'gen': 'BND_GeometryBaseX', 'reg': 'GeometryBaseX', 'base': 'CommonObjectX'},
    {'type': 'Rhino.DocObjects.ModelComponent', 'cs': 'ModelComponent',
     'on': 'ON_ModelComponent',
     'mode': 'abstract', 'getter': 'RH3DM_McPtr',
     'self_extra': ['ON_Object'],
     'gen': 'BND_ModelComponentX', 'reg': 'ModelComponentX', 'base': 'CommonObjectX'},
    {'type': 'Rhino.Geometry.Curve', 'cs': 'Curve', 'on': 'ON_Curve',
     'mode': 'pointer', 'hand': 'BND_Curve', 'hand_header': 'bnd_curve.h',
     'copy': 'source.m_curve ? source.m_curve->DuplicateCurve() : nullptr',
     'gen': 'BND_CurveX', 'reg': 'CurveX', 'base': 'GeometryBaseX'},
    {'type': 'Rhino.Geometry.Surface', 'cs': 'Surface', 'on': 'ON_Surface',
     'mode': 'pointer', 'hand': 'BND_Surface', 'hand_header': 'bnd_surface.h',
     'copy': 'source.m_surface ? source.m_surface->DuplicateSurface() : nullptr',
     'gen': 'BND_SurfaceX', 'reg': 'SurfaceX', 'base': 'GeometryBaseX'},
    # m_brep is private on BND_Brep; copy via the public GeometryPointer()
    {'type': 'Rhino.Geometry.Brep', 'cs': 'Brep', 'on': 'ON_Brep',
     'mode': 'pointer', 'hand': 'BND_Brep', 'hand_header': 'bnd_brep.h',
     'copy': ('source.GeometryPointer()'
              ' ? new ON_Brep(*ON_Brep::Cast(source.GeometryPointer())) : nullptr'),
     'gen': 'BND_BrepX', 'reg': 'BrepX', 'base': 'GeometryBaseX'},
    {'type': 'Rhino.Geometry.Mesh', 'cs': 'Mesh', 'on': 'ON_Mesh',
     'mode': 'pointer', 'hand': 'BND_Mesh', 'hand_header': 'bnd_mesh.h',
     'copy': ('source.GeometryPointer()'
              ' ? new ON_Mesh(*ON_Mesh::Cast(source.GeometryPointer())) : nullptr'),
     'gen': 'BND_MeshX', 'reg': 'MeshX', 'base': 'GeometryBaseX'},
    # the manifest's richest class (46 candidates): camera/frustum math is
    # all doubles, bools, points and vectors -- exactly the marshalled set
    {'type': 'Rhino.DocObjects.ViewportInfo', 'cs': 'ViewportInfo', 'user_strings': True,
     'on': 'ON_Viewport',
     'mode': 'pointer', 'hand': 'BND_Viewport', 'hand_header': 'bnd_viewport.h',
     'copy': 'source.m_viewport ? new ON_Viewport(*source.m_viewport) : nullptr',
     'gen': 'BND_ViewportX', 'reg': 'ViewportX', 'bridge': False, 'base': 'CommonObjectX'},
    # hand binding file is bnd_beam.h / C layer on_beam.cpp (opennurbs's
    # historical name for ON_Extrusion)
    {'type': 'Rhino.Geometry.Extrusion', 'cs': 'Extrusion', 'on': 'ON_Extrusion',
     'mode': 'pointer', 'hand': 'BND_Extrusion', 'hand_header': 'bnd_beam.h',
     'copy': ('source.GeometryPointer()'
              ' ? new ON_Extrusion(*ON_Extrusion::Cast(source.GeometryPointer()))'
              ' : nullptr'),
     'gen': 'BND_ExtrusionX', 'reg': 'ExtrusionX', 'base': 'SurfaceX'},
    {'type': 'Rhino.Geometry.PointCloud', 'cs': 'PointCloud', 'on': 'ON_PointCloud',
     'mode': 'pointer', 'hand': 'BND_PointCloud', 'hand_header': 'bnd_pointcloud.h',
     'copy': ('source.GeometryPointer()'
              ' ? new ON_PointCloud(*ON_PointCloud::Cast(source.GeometryPointer()))'
              ' : nullptr'),
     'gen': 'BND_PointCloudX', 'reg': 'PointCloudX', 'base': 'GeometryBaseX',
     # bulk tier: Python buffer protocol over the contiguous ON_3dPoint storage
     # (3 doubles each) -> numpy.asarray(cloud) is an (N, 3) zero-copy view
     'buffer': 'm_ptr->m_P'},
    # G99 (Luis 2026-10-16, option A): RhinoCommon's Font holds a MANAGED font -- an entry of
    # opennurbs' process-wide list (ON_Font::GetManagedFont): shared, immutable, never freed. FontX
    # does the same (`managed`: a borrowed pointer, never deleted); the bridge passes the old
    # BND_Font's managed font through
    {'type': 'Rhino.DocObjects.Font', 'cs': 'Font', 'on': 'ON_Font',
     'mode': 'pointer', 'hand': 'BND_Font', 'hand_header': 'bnd_font.h', 'managed': True,
     'copy': 'const_cast<ON_Font*>(source.m_managed_font)',
     'gen': 'BND_FontX', 'reg': 'FontX'},
    # hand class holds ON_BezierCurve BY VALUE (private; public ONBezier()
    # accessor added for this) -- second value-mode class
    {'type': 'Rhino.Geometry.BezierCurve', 'cs': 'BezierCurve',
     'on': 'ON_BezierCurve',
     'mode': 'value', 'hand': 'BND_BezierCurve', 'hand_header': 'bnd_bezier.h',
     'copy': 'source.ONBezier()',
     'gen': 'BND_BezierCurveX', 'reg': 'BezierCurveX'},
    # m_polycurve is private; the BND_Curve base's m_curve is public and
    # aliases the same object (the Brep approach)
    {'type': 'Rhino.Geometry.PolyCurve', 'cs': 'PolyCurve', 'on': 'ON_PolyCurve',
     'mode': 'pointer', 'hand': 'BND_PolyCurve', 'hand_header': 'bnd_polycurve.h',
     'copy': ('source.m_curve'
              ' ? new ON_PolyCurve(*ON_PolyCurve::Cast(source.m_curve))'
              ' : nullptr'),
     'gen': 'BND_PolyCurveX', 'reg': 'PolyCurveX', 'base': 'CurveX'},
    # concrete subtypes: the hand classes keep their specific pointer
    # private, so copy through the public base pointer (Brep/PolyCurve idiom)
    {'type': 'Rhino.Geometry.LineCurve', 'cs': 'LineCurve', 'on': 'ON_LineCurve',
     'mode': 'pointer', 'hand': 'BND_LineCurve', 'hand_header': 'bnd_linecurve.h',
     'copy': ('source.m_curve'
              ' ? new ON_LineCurve(*ON_LineCurve::Cast(source.m_curve)) : nullptr'),
     'gen': 'BND_LineCurveX', 'reg': 'LineCurveX', 'base': 'CurveX'},
    {'type': 'Rhino.Geometry.ArcCurve', 'cs': 'ArcCurve', 'on': 'ON_ArcCurve',
     'mode': 'pointer', 'hand': 'BND_ArcCurve', 'hand_header': 'bnd_arccurve.h',
     'copy': ('source.m_curve'
              ' ? new ON_ArcCurve(*ON_ArcCurve::Cast(source.m_curve)) : nullptr'),
     'gen': 'BND_ArcCurveX', 'reg': 'ArcCurveX', 'base': 'CurveX'},
    {'type': 'Rhino.Geometry.NurbsCurve', 'cs': 'NurbsCurve', 'on': 'ON_NurbsCurve',
     'mode': 'pointer', 'hand': 'BND_NurbsCurve', 'hand_header': 'bnd_nurbscurve.h',
     'copy': ('source.m_curve'
              ' ? new ON_NurbsCurve(*ON_NurbsCurve::Cast(source.m_curve)) : nullptr'),
     'gen': 'BND_NurbsCurveX', 'reg': 'NurbsCurveX', 'base': 'CurveX'},
    {'type': 'Rhino.Geometry.PolylineCurve', 'cs': 'PolylineCurve', 'on': 'ON_PolylineCurve',
     'mode': 'pointer', 'hand': 'BND_PolylineCurve', 'hand_header': 'bnd_polylinecurve.h',
     'copy': ('source.m_curve'
              ' ? new ON_PolylineCurve(*ON_PolylineCurve::Cast(source.m_curve)) : nullptr'),
     'gen': 'BND_PolylineCurveX', 'reg': 'PolylineCurveX', 'base': 'CurveX'},
    {'type': 'Rhino.Geometry.NurbsSurface', 'cs': 'NurbsSurface', 'on': 'ON_NurbsSurface',
     'mode': 'pointer', 'hand': 'BND_NurbsSurface', 'hand_header': 'bnd_nurbssurface.h',
     'copy': ('source.m_surface'
              ' ? new ON_NurbsSurface(*ON_NurbsSurface::Cast(source.m_surface)) : nullptr'),
     'gen': 'BND_NurbsSurfaceX', 'reg': 'NurbsSurfaceX', 'base': 'SurfaceX'},
    {'type': 'Rhino.Geometry.PlaneSurface', 'cs': 'PlaneSurface', 'on': 'ON_PlaneSurface',
     'mode': 'pointer', 'hand': 'BND_PlaneSurface', 'hand_header': 'bnd_planesurface.h',
     'copy': ('source.m_surface'
              ' ? new ON_PlaneSurface(*ON_PlaneSurface::Cast(source.m_surface)) : nullptr'),
     'gen': 'BND_PlaneSurfaceX', 'reg': 'PlaneSurfaceX', 'base': 'SurfaceX'},
    {'type': 'Rhino.Geometry.RevSurface', 'cs': 'RevSurface', 'on': 'ON_RevSurface',
     'mode': 'pointer', 'hand': 'BND_RevSurface', 'hand_header': 'bnd_revsurface.h',
     'copy': 'source.m_revsurface ? new ON_RevSurface(*source.m_revsurface) : nullptr',
     'gen': 'BND_RevSurfaceX', 'reg': 'RevSurfaceX', 'base': 'SurfaceX'},
    {'type': 'Rhino.DocObjects.Linetype', 'cs': 'Linetype', 'on': 'ON_Linetype',
     'mode': 'pointer', 'hand': 'BND_Linetype', 'hand_header': 'bnd_linetype.h',
     'copy': ('source.m_linetype'
              ' ? new ON_Linetype(*source.m_linetype) : nullptr'),
     'gen': 'BND_LinetypeX', 'reg': 'LinetypeX', 'bridge': False, 'base': 'ModelComponentX'},
    {'type': 'Rhino.DocObjects.DimensionStyle', 'cs': 'DimensionStyle',
     'on': 'ON_DimStyle',
     'mode': 'pointer', 'hand': 'BND_DimensionStyle',
     'hand_header': 'bnd_dimensionstyle.h',
     'copy': ('source.m_dimstyle'
              ' ? new ON_DimStyle(*source.m_dimstyle) : nullptr'),
     'gen': 'BND_DimensionStyleX', 'reg': 'DimensionStyleX', 'bridge': False, 'base': 'ModelComponentX'},
    {'type': 'Rhino.DocObjects.Group', 'cs': 'Group', 'on': 'ON_Group',
     'mode': 'pointer', 'hand': 'BND_Group', 'hand_header': 'bnd_group.h',
     'copy': 'source.m_group ? new ON_Group(*source.m_group) : nullptr',
     'gen': 'BND_GroupX', 'reg': 'GroupX', 'base': 'ModelComponentX'},
    # G91: what ManifestTable.Instantiate makes for ModelComponentType.HatchPattern (and the
    # File3dmHatchPatternTable's items); no old binding
    {'type': 'Rhino.DocObjects.HatchPattern', 'cs': 'HatchPattern', 'on': 'ON_HatchPattern',
     'mode': 'pointer', 'hand': None, 'bridge': False, 'base': 'ModelComponentX',
     'gen': 'BND_HatchPatternX', 'reg': 'HatchPatternX'},
    {'type': 'Rhino.Geometry.InstanceDefinitionGeometry', 'cs': 'InstanceDefinitionGeometry',
     'on': 'ON_InstanceDefinition',
     'mode': 'pointer', 'hand': 'BND_InstanceDefinitionGeometry', 'hand_header': 'bnd_instance.h',
     'copy': 'source.m_idef ? new ON_InstanceDefinition(*source.m_idef) : nullptr',
     'gen': 'BND_InstanceDefinitionGeometryX', 'reg': 'InstanceDefinitionGeometryX', 'base': 'ModelComponentX'},
    # -- replacement phase 2: classes not begun before (docs/vnext/old-coverage.md)
    {'type': 'Rhino.DocObjects.Layer', 'cs': 'Layer', 'on': 'ON_Layer',
     'mode': 'pointer', 'hand': 'BND_Layer', 'hand_header': 'bnd_layer.h',
     'copy': 'source.m_layer ? new ON_Layer(*source.m_layer) : nullptr',
     'gen': 'BND_LayerX', 'reg': 'LayerX', 'base': 'ModelComponentX'},
    {'type': 'Rhino.DocObjects.Material', 'cs': 'Material', 'on': 'ON_Material',
     'mode': 'pointer', 'hand': 'BND_Material', 'hand_header': 'bnd_material.h',
     'copy': 'source.m_material ? new ON_Material(*source.m_material) : nullptr',
     'gen': 'BND_MaterialX', 'reg': 'MaterialX', 'base': 'ModelComponentX'},
    {'type': 'Rhino.Geometry.Light', 'cs': 'Light', 'on': 'ON_Light',
     'mode': 'pointer', 'hand': 'BND_Light', 'hand_header': 'bnd_light.h',
     'copy': ('ON_Light::Cast(source.GeometryPointer())'
              ' ? new ON_Light(*ON_Light::Cast(source.GeometryPointer())) : nullptr'),
     'gen': 'BND_LightX', 'reg': 'LightX', 'base': 'GeometryBaseX'},
    {'type': 'Rhino.DocObjects.ObjectAttributes', 'cs': 'ObjectAttributes',
     'on': 'ON_3dmObjectAttributes',
     'mode': 'pointer', 'hand': 'BND_3dmObjectAttributes', 'hand_header': 'bnd_3dm_attributes.h',
     'copy': 'source.m_attributes ? new ON_3dmObjectAttributes(*source.m_attributes) : nullptr',
     'gen': 'BND_ObjectAttributesX', 'reg': 'ObjectAttributesX', 'base': 'CommonObjectX'},
    {'type': 'Rhino.Geometry.MeshingParameters', 'cs': 'MeshingParameters',
     'on': 'ON_MeshParameters',
     'mode': 'value', 'hand': 'BND_MeshingParameters', 'hand_header': 'bnd_mesh.h',
     'copy': 'source.ONMeshParameters()',
     'gen': 'BND_MeshingParametersX', 'reg': 'MeshingParametersX'},
    {'type': 'Rhino.DocObjects.Texture', 'cs': 'Texture', 'on': 'ON_Texture',
     'mode': 'pointer', 'hand': 'BND_Texture', 'hand_header': 'bnd_texture.h',
     'copy': 'source.m_texture ? new ON_Texture(*source.m_texture) : nullptr',
     'gen': 'BND_TextureX', 'reg': 'TextureX', 'base': 'CommonObjectX'},
    {'type': 'Rhino.Render.TextureMapping', 'cs': 'TextureMapping', 'user_strings': True, 'on': 'ON_TextureMapping',
     'mode': 'pointer', 'hand': 'BND_TextureMapping', 'hand_header': 'bnd_texture_mapping.h',
     'copy': 'source.m_mapping ? new ON_TextureMapping(*source.m_mapping) : nullptr',
     'gen': 'BND_TextureMappingX', 'reg': 'TextureMappingX', 'base': 'ModelComponentX'},
    {'type': 'Rhino.DocObjects.EarthAnchorPoint', 'cs': 'EarthAnchorPoint',
     'on': 'ON_EarthAnchorPoint',
     'mode': 'value', 'hand': 'BND_EarthAnchorPoint', 'hand_header': 'bnd_3dm_settings.h',
     'copy': 'source.m_anchor_point',
     'gen': 'BND_EarthAnchorPointX', 'reg': 'EarthAnchorPointX'},
    {'type': 'Rhino.DocObjects.ViewInfo', 'cs': 'ViewInfo', 'on': 'ON_3dmView',
     'mode': 'value', 'hand': 'BND_ViewInfo', 'hand_header': 'bnd_3dm_settings.h',
     'copy': 'source.m_view',
     # G104: RhinoCommon's `internal ViewInfo(File3dm parent, Guid id, IntPtr ptr, bool namedViewTable)` -- a
     # LIVE view into the file's (named) view table. ConstPointer/NonConstPointer resolve it with
     # ONX_Model_ViewPointer(file, id, ptr, named); the generated view resolves on every access (RhinoCommon
     # returns its cached ptr while non-zero, which dangles once the table reallocates)
     'fileview': {'resolver': 'ONX_Model_ViewPointer', 'ctor': ['File3dm', 'Guid', 'IntPtr', 'bool']},
     'gen': 'BND_ViewInfoX', 'reg': 'ViewInfoX'},
    # -- more geometry: bridge copies go through Duplicate() (works for any ON_Geometry)
    {'type': 'Rhino.Geometry.Point', 'cs': 'Point', 'on': 'ON_Point',
     'mode': 'pointer', 'hand': 'BND_Point', 'hand_header': 'bnd_pointgeometry.h',
     'copy': ('ON_Point::Cast(source.GeometryPointer() ? source.GeometryPointer()->Duplicate() : nullptr)'),
     'gen': 'BND_PointX', 'reg': 'PointX', 'base': 'GeometryBaseX'},
    {'type': 'Rhino.Geometry.TextDot', 'cs': 'TextDot', 'on': 'ON_TextDot',
     'mode': 'pointer', 'hand': 'BND_TextDot', 'hand_header': 'bnd_annotationbase.h',
     'copy': ('ON_TextDot::Cast(source.GeometryPointer() ? source.GeometryPointer()->Duplicate() : nullptr)'),
     'gen': 'BND_TextDotX', 'reg': 'TextDotX', 'base': 'GeometryBaseX'},
    {'type': 'Rhino.Geometry.Hatch', 'cs': 'Hatch', 'on': 'ON_Hatch',
     'mode': 'pointer', 'hand': 'BND_Hatch', 'hand_header': 'bnd_hatch.h',
     'copy': ('ON_Hatch::Cast(source.GeometryPointer() ? source.GeometryPointer()->Duplicate() : nullptr)'),
     'gen': 'BND_HatchX', 'reg': 'HatchX', 'base': 'GeometryBaseX'},
    {'type': 'Rhino.Geometry.SubD', 'cs': 'SubD', 'on': 'ON_SubD',
     'mode': 'pointer', 'hand': 'BND_SubD', 'hand_header': 'bnd_subd.h',
     'copy': ('ON_SubD::Cast(source.GeometryPointer() ? source.GeometryPointer()->Duplicate() : nullptr)'),
     'gen': 'BND_SubDX', 'reg': 'SubDX', 'base': 'GeometryBaseX'},
    # -- batch 2 (G46): native-backed classes
    # an IDisposable HANDLE (specgen Handle): owns its pointer, frees it with its own Delete
    {'type': 'Rhino.FileIO.FileReference', 'cs': 'FileReference', 'on': 'ON_FileReference',
     'mode': 'pointer', 'hand': None, 'bridge': False, 'noctor': True,
     'gen': 'BND_FileReferenceX', 'reg': 'FileReferenceX'},
    # G107: Draco compression (Luis 2026-10-19: sync c/draco.cpp, U24) -- the handle RhinoCommon's
    # DracoCompression owns (Draco_Compress / Draco_Delete); its pointee is draco.cpp's own class,
    # opaque here (compiled renamed, src/CMakeLists.txt)
    {'type': 'Rhino.FileIO.DracoCompression', 'cs': 'DracoCompression', 'on': 'BND_Draco',
     'mode': 'pointer', 'hand': None, 'bridge': False, 'noctor': True,
     'gen': 'BND_DracoCompressionX', 'reg': 'DracoCompressionX'},
    # G96: an INTERNAL handle (never registered): AnnotationBase.GetTextRuns's elements, which
    # PlainText / TextHasRtfFormatting read through LINQ -- a pooled ON_TextRun the object
    # gives back with ON_TextRun_ReturnManagedTextRun (specgen InternalHandles)
    {'type': 'Rhino.Geometry.TextRun', 'cs': 'TextRun', 'on': 'ON_TextRun',
     'mode': 'pointer', 'hand': None, 'bridge': False, 'noctor': True, 'internal': True,
     'gen': 'BND_TextRunX', 'reg': 'TextRunX'},
    # a decal (G60): a std::shared_ptr<ON_Decal> handle (specgen SharedHandle)
    {'type': 'Rhino.Render.Decal', 'cs': 'Decal', 'on': 'ON_Decal',
     'mode': 'pointer', 'hand': 'BND_File3dmDecal', 'bridge': False, 'noctor': True,
     'gen': 'BND_DecalX', 'reg': 'DecalX'},
    # an object's decals (G60): a child of its attributes keeping RhinoCommon's
    # constructor state (read-only flag, document serial number; specgen CtorStore)
    {'type': 'Rhino.Render.Decals', 'cs': 'Decals', 'on': 'ON_3dmObjectAttributes',
     'mode': 'child', 'parent': 'ObjectAttributesX', 'hand': 'BND_File3dmDecalTable', 'bridge': False,
     'gen': 'BND_DecalsX', 'reg': 'DecalsX'},
    # mesh modifiers (G46): children of an object's attributes (old names: Displacement, ...)
    {'type': 'Rhino.FileIO.File3dmMeshModifiers', 'cs': 'File3dmMeshModifiers', 'on': 'ON_3dmObjectAttributes',
     'mode': 'child', 'parent': 'ObjectAttributesX', 'hand': 'BND_File3dmMeshModifiers', 'bridge': False,
     'gen': 'BND_File3dmMeshModifiersX', 'reg': 'File3dmMeshModifiersX'},
    {'type': 'Rhino.FileIO.File3dmDisplacement', 'cs': 'File3dmDisplacement', 'on': 'ON_3dmObjectAttributes',
     'mode': 'child', 'parent': 'File3dmMeshModifiersX', 'hand': 'BND_File3dmDisplacement', 'bridge': False,
     'gen': 'BND_File3dmDisplacementX', 'reg': 'File3dmDisplacementX'},
    {'type': 'Rhino.FileIO.File3dmEdgeSoftening', 'cs': 'File3dmEdgeSoftening', 'on': 'ON_3dmObjectAttributes',
     'mode': 'child', 'parent': 'File3dmMeshModifiersX', 'hand': 'BND_File3dmEdgeSoftening', 'bridge': False,
     'gen': 'BND_File3dmEdgeSofteningX', 'reg': 'File3dmEdgeSofteningX'},
    {'type': 'Rhino.FileIO.File3dmThickening', 'cs': 'File3dmThickening', 'on': 'ON_3dmObjectAttributes',
     'mode': 'child', 'parent': 'File3dmMeshModifiersX', 'hand': 'BND_File3dmThickening', 'bridge': False,
     'gen': 'BND_File3dmThickeningX', 'reg': 'File3dmThickeningX'},
    {'type': 'Rhino.FileIO.File3dmCurvePiping', 'cs': 'File3dmCurvePiping', 'on': 'ON_3dmObjectAttributes',
     'mode': 'child', 'parent': 'File3dmMeshModifiersX', 'hand': 'BND_File3dmCurvePiping', 'bridge': False,
     'gen': 'BND_File3dmCurvePipingX', 'reg': 'File3dmCurvePipingX'},
    {'type': 'Rhino.FileIO.File3dmShutLining', 'cs': 'File3dmShutLining', 'on': 'ON_3dmObjectAttributes',
     'mode': 'child', 'parent': 'File3dmMeshModifiersX', 'hand': 'BND_File3dmShutLining', 'bridge': False,
     'gen': 'BND_File3dmShutLiningX', 'reg': 'File3dmShutLiningX'},
    # a BORROWED pointer into a mesh (specgen Borrowed): never deleted; the mesh is kept alive
    {'type': 'Rhino.Render.CachedTextureCoordinates', 'cs': 'CachedTextureCoordinates', 'on': 'ON_TextureCoordinates',
     'mode': 'pointer', 'hand': None, 'bridge': False, 'noctor': True,
     'gen': 'BND_CachedTextureCoordinatesX', 'reg': 'CachedTextureCoordinatesX'},
    {'type': 'Rhino.DocObjects.Environment', 'cs': 'Environment', 'on': 'ON_Environment',
     'mode': 'pointer', 'hand': 'BND_Environment', 'hand_header': 'bnd_texture.h',
     'copy': 'source.m_env ? new ON_Environment(*source.m_env) : nullptr',
     'gen': 'BND_EnvironmentX', 'reg': 'EnvironmentX', 'base': 'CommonObjectX'},
    # -- batch 1 (G43): COMPONENTS -- objects RhinoCommon makes as (owner, key) and
    # resolves through the owner on every access (specgen Component). The proxy
    # bases exist for RhinoCommon's inheritance (protected constructors: no init).
    {'type': 'Rhino.Geometry.SurfaceProxy', 'cs': 'SurfaceProxy', 'on': 'ON_SurfaceProxy',
     'mode': 'pointer', 'hand': None, 'bridge': False, 'noctor': True,
     'gen': 'BND_SurfaceProxyX', 'reg': 'SurfaceProxyX', 'base': 'SurfaceX'},
    {'type': 'Rhino.Geometry.CurveProxy', 'cs': 'CurveProxy', 'on': 'ON_CurveProxy',
     'mode': 'pointer', 'hand': None, 'bridge': False, 'noctor': True,
     'gen': 'BND_CurveProxyX', 'reg': 'CurveProxyX', 'base': 'CurveX'},
    {'type': 'Rhino.Geometry.BrepFace', 'cs': 'BrepFace', 'on': 'ON_BrepFace',
     'mode': 'pointer', 'hand': None, 'bridge': False,
     'gen': 'BND_BrepFaceX', 'reg': 'BrepFaceX', 'base': 'SurfaceProxyX'},
    {'type': 'Rhino.Geometry.BrepEdge', 'cs': 'BrepEdge', 'on': 'ON_BrepEdge',
     'mode': 'pointer', 'hand': None, 'bridge': False,
     'gen': 'BND_BrepEdgeX', 'reg': 'BrepEdgeX', 'base': 'CurveProxyX'},
    {'type': 'Rhino.Geometry.BrepTrim', 'cs': 'BrepTrim', 'on': 'ON_BrepTrim',
     'mode': 'pointer', 'hand': None, 'bridge': False,
     'gen': 'BND_BrepTrimX', 'reg': 'BrepTrimX', 'base': 'CurveProxyX'},
    {'type': 'Rhino.Geometry.BrepVertex', 'cs': 'BrepVertex', 'on': 'ON_BrepVertex',
     'mode': 'pointer', 'hand': None, 'bridge': False,
     'gen': 'BND_BrepVertexX', 'reg': 'BrepVertexX', 'base': 'PointX'},
    {'type': 'Rhino.Geometry.BrepLoop', 'cs': 'BrepLoop', 'on': 'ON_BrepLoop',
     'mode': 'pointer', 'hand': None, 'bridge': False,
     'gen': 'BND_BrepLoopX', 'reg': 'BrepLoopX', 'base': 'GeometryBaseX'},
    # the SubD components' abstract base (G46): `Id` is the components' key
    {'type': 'Rhino.Geometry.SubDComponent', 'cs': 'SubDComponent', 'on': 'ON_SubDComponentBase',
     'mode': 'abstract', 'getter': 'RH3DM_SubDCompPtr',
     'gen': 'BND_SubDComponentX', 'reg': 'SubDComponentX'},
    {'type': 'Rhino.Geometry.SubDFace', 'cs': 'SubDFace', 'on': 'ON_SubDFace',
     'mode': 'pointer', 'hand': None, 'bridge': False,
     'gen': 'BND_SubDFaceX', 'reg': 'SubDFaceX', 'base': 'SubDComponentX'},
    {'type': 'Rhino.Geometry.SubDEdge', 'cs': 'SubDEdge', 'on': 'ON_SubDEdge',
     'mode': 'pointer', 'hand': None, 'bridge': False,
     'gen': 'BND_SubDEdgeX', 'reg': 'SubDEdgeX', 'base': 'SubDComponentX'},
    {'type': 'Rhino.Geometry.SubDVertex', 'cs': 'SubDVertex', 'on': 'ON_SubDVertex',
     'mode': 'pointer', 'hand': None, 'bridge': False,
     'gen': 'BND_SubDVertexX', 'reg': 'SubDVertexX', 'base': 'SubDComponentX'},
    {'type': 'Rhino.Geometry.AnnotationBase', 'cs': 'AnnotationBase', 'on': 'ON_Annotation',
     'mode': 'pointer', 'hand': 'BND_AnnotationBase', 'hand_header': 'bnd_annotationbase.h',
     'copy': ('ON_Annotation::Cast(source.GeometryPointer() ? source.GeometryPointer()->Duplicate() : nullptr)'),
     'gen': 'BND_AnnotationBaseX', 'reg': 'AnnotationBaseX', 'base': 'GeometryBaseX',
     'delete_as_object': True,
     # G88: RhinoCommon's managed `m_parent_dimstyle`, modeled: the object (or, for a
     # File3dm view, a light copy -- a new view of the same file and id) the
     # ParentDimensionStyle setter stores; ConstParentDimStylePointer() is its pointer
     # (null when unset); the DimensionStyleId setter retargets its id
     'modeled': {'m_parent_dimstyle': {'cs': 'DimensionStyle', 'ptr': 'ConstParentDimStylePointer',
                                       'prop': 'ParentDimensionStyle', 'id_prop': 'DimensionStyleId'}}},
    # the dimension classes and instance references (2026-10-20, triage batch B): portable in
    # RhinoCommon; the old bindings register them as DimLinear / DimAngular / DimRadial /
    # DimOrdinate / InstanceReference (paired through 'hand')
    {'type': 'Rhino.Geometry.Dimension', 'cs': 'Dimension', 'on': 'ON_Dimension',
     'mode': 'pointer', 'hand': 'BND_Dimension', 'hand_header': 'bnd_annotationbase.h',
     'copy': ('ON_Dimension::Cast(source.GeometryPointer() ? source.GeometryPointer()->Duplicate() : nullptr)'),
     'gen': 'BND_DimensionX', 'reg': 'DimensionX', 'base': 'AnnotationBaseX'},
    {'type': 'Rhino.Geometry.LinearDimension', 'cs': 'LinearDimension', 'on': 'ON_DimLinear',
     'mode': 'pointer', 'hand': 'BND_DimLinear', 'hand_header': 'bnd_annotationbase.h',
     'copy': ('ON_DimLinear::Cast(source.GeometryPointer() ? source.GeometryPointer()->Duplicate() : nullptr)'),
     'gen': 'BND_LinearDimensionX', 'reg': 'LinearDimensionX', 'base': 'DimensionX'},
    {'type': 'Rhino.Geometry.AngularDimension', 'cs': 'AngularDimension', 'on': 'ON_DimAngular',
     'mode': 'pointer', 'hand': 'BND_DimAngular', 'hand_header': 'bnd_annotationbase.h',
     'copy': ('ON_DimAngular::Cast(source.GeometryPointer() ? source.GeometryPointer()->Duplicate() : nullptr)'),
     'gen': 'BND_AngularDimensionX', 'reg': 'AngularDimensionX', 'base': 'DimensionX'},
    {'type': 'Rhino.Geometry.RadialDimension', 'cs': 'RadialDimension', 'on': 'ON_DimRadial',
     'mode': 'pointer', 'hand': 'BND_DimRadial', 'hand_header': 'bnd_annotationbase.h',
     'copy': ('ON_DimRadial::Cast(source.GeometryPointer() ? source.GeometryPointer()->Duplicate() : nullptr)'),
     'gen': 'BND_RadialDimensionX', 'reg': 'RadialDimensionX', 'base': 'DimensionX'},
    {'type': 'Rhino.Geometry.OrdinateDimension', 'cs': 'OrdinateDimension', 'on': 'ON_DimOrdinate',
     'mode': 'pointer', 'hand': 'BND_DimOrdinate', 'hand_header': 'bnd_annotationbase.h',
     'copy': ('ON_DimOrdinate::Cast(source.GeometryPointer() ? source.GeometryPointer()->Duplicate() : nullptr)'),
     'gen': 'BND_OrdinateDimensionX', 'reg': 'OrdinateDimensionX', 'base': 'DimensionX'},
    # G116 (2026-10-28, batch 2 J1): the two remaining portable annotation classes; the old
    # bindings register them as Leader / Text
    {'type': 'Rhino.Geometry.Leader', 'cs': 'Leader', 'on': 'ON_Leader',
     'mode': 'pointer', 'hand': 'BND_Leader', 'hand_header': 'bnd_annotationbase.h',
     'copy': ('ON_Leader::Cast(source.GeometryPointer() ? source.GeometryPointer()->Duplicate() : nullptr)'),
     'gen': 'BND_LeaderX', 'reg': 'LeaderX', 'base': 'AnnotationBaseX'},
    {'type': 'Rhino.Geometry.TextEntity', 'cs': 'TextEntity', 'on': 'ON_Text',
     'mode': 'pointer', 'hand': 'BND_Text', 'hand_header': 'bnd_annotationbase.h',
     'copy': ('ON_Text::Cast(source.GeometryPointer() ? source.GeometryPointer()->Duplicate() : nullptr)'),
     'gen': 'BND_TextEntityX', 'reg': 'TextEntityX', 'base': 'AnnotationBaseX'},
    {'type': 'Rhino.Geometry.InstanceReferenceGeometry', 'cs': 'InstanceReferenceGeometry', 'on': 'ON_InstanceRef',
     'mode': 'pointer', 'hand': 'BND_InstanceReferenceGeometry', 'hand_header': 'bnd_instance.h',
     'copy': ('ON_InstanceRef::Cast(source.GeometryPointer() ? source.GeometryPointer()->Duplicate() : nullptr)'),
     'gen': 'BND_InstanceReferenceGeometryX', 'reg': 'InstanceReferenceGeometryX', 'base': 'GeometryBaseX'},
    # -- static classes: no instances, static members only
    {'type': 'Rhino.Geometry.Intersect.Intersection', 'cs': 'Intersection', 'on': None,
     'mode': 'static', 'hand': None, 'bridge': False,
     'gen': 'BND_IntersectionX', 'reg': 'IntersectionX'},
    # G122: a managed C# class (BlockId, ArrowType: no native object) with one static over the C
    # layer -- bound for its statics only, as the old binding did (Arrowhead.GetPoints); its
    # instance members need data-class Guid / nested-enum fields (deviations D73)
    {'type': 'Rhino.Geometry.Arrowhead', 'cs': 'Arrowhead', 'on': None,
     'mode': 'static', 'hand': None, 'bridge': False,
     'gen': 'BND_ArrowheadX', 'reg': 'ArrowheadX'},
    # the file's render settings: a VIEW of the file (no id) whose setters copy
    # on write and commit back (RenderSettings.Commit, see G41)
    {'type': 'Rhino.Render.RenderSettings', 'cs': 'RenderSettings', 'user_strings': True, 'on': 'ON_3dmRenderSettings',
     'mode': 'pointer', 'hand': None, 'bridge': False,
     'gen': 'BND_RenderSettingsX', 'reg': 'RenderSettingsX', 'base': 'CommonObjectX'},
    # the RDK settings inside the render settings (DocumentOrFreeFloatingBase):
    # write-through views of the file (ON_X_FromONX_Model), or free-floating
    # objects with their own New/Delete; every property is the Variant idiom (G42)
    {'type': 'Rhino.Render.GroundPlane', 'cs': 'GroundPlane', 'on': 'ON_GroundPlane',
     'mode': 'pointer', 'hand': None, 'bridge': False, 'gen': 'BND_GroundPlaneX', 'reg': 'GroundPlaneX'},
    {'type': 'Rhino.Render.SafeFrame', 'cs': 'SafeFrame', 'on': 'ON_SafeFrame',
     'mode': 'pointer', 'hand': None, 'bridge': False, 'gen': 'BND_SafeFrameX', 'reg': 'SafeFrameX'},
    {'type': 'Rhino.Render.Sun', 'cs': 'Sun', 'on': 'ON_Sun',
     'mode': 'pointer', 'hand': None, 'bridge': False, 'gen': 'BND_SunX', 'reg': 'SunX'},
    # Luis 2026-10-16: RenderSettings.PostEffects -- the portable PostEffectCollection (free-floating,
    # ON_PostEffects) and its PostEffectData items (the old PostEffect's portable counterpart)
    {'type': 'Rhino.Render.PostEffects.PostEffectCollection', 'cs': 'PostEffectCollection', 'on': 'ON_PostEffects',
     'mode': 'pointer', 'hand': None, 'bridge': False, 'gen': 'BND_PostEffectCollectionX', 'reg': 'PostEffectCollectionX'},
    # a component of (collection, id) (G98: specgen DetectGuidComponent) -- the old PostEffect's
    # portable counterpart
    {'type': 'Rhino.Render.PostEffects.PostEffectData', 'cs': 'PostEffectData', 'on': 'ON_PostEffect',
     'mode': 'pointer', 'hand': None, 'old': 'BND_File3dmPostEffect', 'bridge': False, 'noctor': True,
     'gen': 'BND_PostEffectDataX', 'reg': 'PostEffectDataX'},
    {'type': 'Rhino.Render.Skylight', 'cs': 'Skylight', 'on': 'ON_Skylight',
     'mode': 'pointer', 'hand': None, 'bridge': False, 'gen': 'BND_SkylightX', 'reg': 'SkylightX'},
    {'type': 'Rhino.Render.LinearWorkflow', 'cs': 'LinearWorkflow', 'on': 'ON_LinearWorkflow',
     'mode': 'pointer', 'hand': None, 'bridge': False, 'gen': 'BND_LinearWorkflowX', 'reg': 'LinearWorkflowX'},
    {'type': 'Rhino.Render.Dithering', 'cs': 'Dithering', 'on': 'ON_Dithering',
     'mode': 'pointer', 'hand': None, 'bridge': False, 'gen': 'BND_DitheringX', 'reg': 'DitheringX'},
    {'type': 'Rhino.Render.RenderChannels', 'cs': 'RenderChannels', 'on': 'ON_RenderChannels',
     'mode': 'pointer', 'hand': None, 'bridge': False, 'gen': 'BND_RenderChannelsX', 'reg': 'RenderChannelsX'},
    # -- File3dm: an owning ONX_Model; its tables are views onto it
    {'type': 'Rhino.FileIO.File3dm', 'cs': 'File3dm', 'on': 'ONX_Model',
     'mode': 'pointer', 'hand': None, 'bridge': False,
     'gen': 'BND_File3dmX', 'reg': 'File3dmX'},
    # the items of File3dm.Objects: a proxy (model, id) whose Geometry is a
    # copy-on-write VIEW and whose Attributes write through (ParentViews)
    {'type': 'Rhino.FileIO.File3dmObject', 'cs': 'File3dmObject', 'on': 'ON_ModelGeometryComponent',
     'mode': 'pointer', 'hand': None, 'bridge': False, 'base': 'ModelComponentX',
     'gen': 'BND_File3dmObjectX', 'reg': 'File3dmObjectX'},
    {'type': 'Rhino.FileIO.File3dmObjectTable', 'cs': 'File3dmObjectTable', 'on': 'ONX_Model',
     'mode': 'child', 'parent': 'File3dmX', 'hand': None, 'bridge': False,
     'gen': 'BND_File3dmObjectTableX', 'reg': 'File3dmObjectTableX'},
    {'type': 'Rhino.FileIO.File3dmSettings', 'cs': 'File3dmSettings', 'on': 'ON_3dmSettings',
     'mode': 'child', 'parent': 'File3dmX', 'hand': None, 'bridge': False,
     'derive': '&{p}->m_settings',
     'gen': 'BND_File3dmSettingsX', 'reg': 'File3dmSettingsX'},
    {'type': 'Rhino.FileIO.File3dmStringTable', 'cs': 'File3dmStringTable', 'on': 'ONX_Model',
     'mode': 'child', 'parent': 'File3dmX', 'hand': None, 'bridge': False,
     'gen': 'BND_File3dmStringTableX', 'reg': 'File3dmStringTableX'},
    {'type': 'Rhino.FileIO.File3dmNotes', 'cs': 'File3dmNotes', 'on': 'ONX_Model',
     'mode': 'child', 'parent': 'File3dmX', 'hand': None, 'bridge': False,
     'gen': 'BND_File3dmNotesX', 'reg': 'File3dmNotesX'},
    {'type': 'Rhino.FileIO.File3dmLayerTable', 'cs': 'File3dmLayerTable', 'on': 'ONX_Model',
     'mode': 'child', 'parent': 'File3dmX', 'hand': None, 'bridge': False,
     'gen': 'BND_File3dmLayerTableX', 'reg': 'File3dmLayerTableX'},
    {'type': 'Rhino.FileIO.File3dmMaterialTable', 'cs': 'File3dmMaterialTable', 'on': 'ONX_Model',
     'mode': 'child', 'parent': 'File3dmX', 'hand': None, 'bridge': False,
     'gen': 'BND_File3dmMaterialTableX', 'reg': 'File3dmMaterialTableX'},
    {'type': 'Rhino.FileIO.File3dmLinetypeTable', 'cs': 'File3dmLinetypeTable', 'on': 'ONX_Model',
     'mode': 'child', 'parent': 'File3dmX', 'hand': None, 'bridge': False,
     'gen': 'BND_File3dmLinetypeTableX', 'reg': 'File3dmLinetypeTableX'},
    {'type': 'Rhino.FileIO.File3dmGroupTable', 'cs': 'File3dmGroupTable', 'on': 'ONX_Model',
     'mode': 'child', 'parent': 'File3dmX', 'hand': None, 'bridge': False,
     'gen': 'BND_File3dmGroupTableX', 'reg': 'File3dmGroupTableX'},
    # G91 (Luis 2026-10-15): File3dm.Manifest -- the public abstract ManifestTable, its bodies
    # from the internal File3dmManifestTable (`impl`) that File3dm constructs
    {'type': 'Rhino.FileIO.ManifestTable', 'cs': 'ManifestTable', 'on': 'ONX_Model', 'impl': 'File3dmManifestTable',
     'mode': 'child', 'parent': 'File3dmX', 'hand': None, 'bridge': False,
     'gen': 'BND_ManifestTableX', 'reg': 'ManifestTableX'},
    {'type': 'Rhino.FileIO.File3dmDimStyleTable', 'cs': 'File3dmDimStyleTable', 'on': 'ONX_Model',
     'mode': 'child', 'parent': 'File3dmX', 'hand': None, 'bridge': False,
     'gen': 'BND_File3dmDimStyleTableX', 'reg': 'File3dmDimStyleTableX'},
    {'type': 'Rhino.FileIO.File3dmInstanceDefinitionTable', 'cs': 'File3dmInstanceDefinitionTable', 'on': 'ONX_Model',
     'mode': 'child', 'parent': 'File3dmX', 'hand': None, 'bridge': False,
     'gen': 'BND_File3dmInstanceDefinitionTableX', 'reg': 'File3dmInstanceDefinitionTableX'},
    {'type': 'Rhino.FileIO.File3dmViewTable', 'cs': 'File3dmViewTable', 'on': 'ONX_Model',
     'mode': 'child', 'parent': 'File3dmX', 'hand': None, 'bridge': False,
     'gen': 'BND_File3dmViewTableX', 'reg': 'File3dmViewTableX'},
    {'type': 'Rhino.FileIO.File3dmNamedConstructionPlanes', 'cs': 'File3dmNamedConstructionPlanes', 'on': 'ONX_Model',
     'mode': 'child', 'parent': 'File3dmX', 'hand': None, 'bridge': False,
     'gen': 'BND_File3dmNamedConstructionPlanesX', 'reg': 'File3dmNamedConstructionPlanesX'},
    {'type': 'Rhino.FileIO.File3dmHatchPatternTable', 'cs': 'File3dmHatchPatternTable', 'on': 'ONX_Model',
     'mode': 'child', 'parent': 'File3dmX', 'hand': None, 'bridge': False,
     'gen': 'BND_File3dmHatchPatternTableX', 'reg': 'File3dmHatchPatternTableX'},
    {'type': 'Rhino.FileIO.File3dmPlugInDataTable', 'cs': 'File3dmPlugInDataTable', 'on': 'ONX_Model',
     'mode': 'child', 'parent': 'File3dmX', 'hand': None, 'bridge': False,
     'gen': 'BND_File3dmPlugInDataTableX', 'reg': 'File3dmPlugInDataTableX'},
    # G104 (Luis: generate it): the table's item -- RhinoCommon's `internal File3dmPlugInData(File3dm parent,
    # Guid id)` stores the file and a plug-in id (registry state from CtorStore); PlugInId reads the id
    {'type': 'Rhino.FileIO.File3dmPlugInData', 'cs': 'File3dmPlugInData', 'on': 'ONX_Model',
     'mode': 'child', 'parent': 'File3dmX', 'hand': None, 'bridge': False,
     'gen': 'BND_File3dmPlugInDataX', 'reg': 'File3dmPlugInDataX'},
    # embedded files (G71): a File3dm proxy component and the file's collection of them
    # (RhinoCommon's File3dmEmbeddedFiles: Add plus a manifest enumerator, no Count)
    # 'old': the hand class it replaces, for old-coverage only (no bridge): the old
    # bindings register these as "EmbeddedFile" / "File3dmEmbeddedFileTable"
    {'type': 'Rhino.FileIO.File3dmEmbeddedFile', 'cs': 'File3dmEmbeddedFile', 'on': 'ON_EmbeddedFile',
     'mode': 'pointer', 'hand': None, 'old': 'BND_File3dmEmbeddedFile', 'bridge': False, 'base': 'ModelComponentX',
     'gen': 'BND_File3dmEmbeddedFileX', 'reg': 'File3dmEmbeddedFileX'},
    {'type': 'Rhino.FileIO.File3dmEmbeddedFiles', 'cs': 'File3dmEmbeddedFiles', 'on': 'ONX_Model',
     'mode': 'child', 'parent': 'File3dmX', 'hand': None, 'old': 'BND_File3dmEmbeddedFileTable', 'bridge': False,
     'gen': 'BND_File3dmEmbeddedFilesX', 'reg': 'File3dmEmbeddedFilesX'},
    # render content (G87): File3dm proxies of (file, id) resolved by
    # ONX_Model_FindRenderContentFromId -- read-only, as in RhinoCommon (every member
    # goes through ConstPointer); the three collections walk the manifest by kind
    {'type': 'Rhino.FileIO.File3dmRenderContent', 'cs': 'File3dmRenderContent', 'on': 'ON_RenderContent',
     'mode': 'pointer', 'hand': None, 'old': 'BND_File3dmRenderContent', 'bridge': False, 'base': 'ModelComponentX',
     'proxy_nonconst': 'none', 'gen': 'BND_File3dmRenderContentX', 'reg': 'File3dmRenderContentX'},
    {'type': 'Rhino.FileIO.File3dmRenderMaterial', 'cs': 'File3dmRenderMaterial', 'on': 'ON_RenderMaterial',
     'mode': 'pointer', 'hand': None, 'old': 'BND_File3dmRenderMaterial', 'bridge': False, 'base': 'File3dmRenderContentX',
     'gen': 'BND_File3dmRenderMaterialX', 'reg': 'File3dmRenderMaterialX'},
    {'type': 'Rhino.FileIO.File3dmRenderEnvironment', 'cs': 'File3dmRenderEnvironment', 'on': 'ON_RenderEnvironment',
     'mode': 'pointer', 'hand': None, 'old': 'BND_File3dmRenderEnvironment', 'bridge': False, 'base': 'File3dmRenderContentX',
     'gen': 'BND_File3dmRenderEnvironmentX', 'reg': 'File3dmRenderEnvironmentX'},
    {'type': 'Rhino.FileIO.File3dmRenderTexture', 'cs': 'File3dmRenderTexture', 'on': 'ON_RenderTexture',
     'mode': 'pointer', 'hand': None, 'old': 'BND_File3dmRenderTexture', 'bridge': False, 'base': 'File3dmRenderContentX',
     'gen': 'BND_File3dmRenderTextureX', 'reg': 'File3dmRenderTextureX'},
    {'type': 'Rhino.FileIO.File3dmRenderMaterials', 'cs': 'File3dmRenderMaterials', 'on': 'ONX_Model',
     'mode': 'child', 'parent': 'File3dmX', 'hand': None, 'bridge': False,
     'gen': 'BND_File3dmRenderMaterialsX', 'reg': 'File3dmRenderMaterialsX'},
    {'type': 'Rhino.FileIO.File3dmRenderEnvironments', 'cs': 'File3dmRenderEnvironments', 'on': 'ONX_Model',
     'mode': 'child', 'parent': 'File3dmX', 'hand': None, 'bridge': False,
     'gen': 'BND_File3dmRenderEnvironmentsX', 'reg': 'File3dmRenderEnvironmentsX'},
    {'type': 'Rhino.FileIO.File3dmRenderTextures', 'cs': 'File3dmRenderTextures', 'on': 'ONX_Model',
     'mode': 'child', 'parent': 'File3dmX', 'hand': None, 'bridge': False,
     'gen': 'BND_File3dmRenderTexturesX', 'reg': 'File3dmRenderTexturesX'},
    # -- collections: views onto a parent object (mode 'child')
    {'type': 'Rhino.Geometry.Collections.MeshVertexList', 'cs': 'MeshVertexList', 'on': 'ON_Mesh',
     'mode': 'child', 'parent': 'MeshX', 'hand': None, 'bridge': False,
     'gen': 'BND_MeshVertexListX', 'reg': 'MeshVertexListX'},
    {'type': 'Rhino.Geometry.Collections.MeshFaceList', 'cs': 'MeshFaceList', 'on': 'ON_Mesh',
     'mode': 'child', 'parent': 'MeshX', 'hand': None, 'bridge': False,
     'gen': 'BND_MeshFaceListX', 'reg': 'MeshFaceListX'},
    # 'old': the old bindings register this list as "MeshNormalList" (same members, m_N)
    {'type': 'Rhino.Geometry.Collections.MeshVertexNormalList', 'cs': 'MeshVertexNormalList', 'on': 'ON_Mesh',
     'mode': 'child', 'parent': 'MeshX', 'hand': None, 'old': 'BND_MeshNormalList', 'bridge': False,
     'gen': 'BND_MeshVertexNormalListX', 'reg': 'MeshVertexNormalListX'},
    {'type': 'Rhino.Geometry.Collections.MeshVertexColorList', 'cs': 'MeshVertexColorList', 'on': 'ON_Mesh',
     'mode': 'child', 'parent': 'MeshX', 'hand': None, 'bridge': False,
     'gen': 'BND_MeshVertexColorListX', 'reg': 'MeshVertexColorListX'},
    {'type': 'Rhino.Geometry.Collections.MeshTextureCoordinateList', 'cs': 'MeshTextureCoordinateList', 'on': 'ON_Mesh',
     'mode': 'child', 'parent': 'MeshX', 'hand': None, 'bridge': False,
     'gen': 'BND_MeshTextureCoordinateListX', 'reg': 'MeshTextureCoordinateListX'},
    {'type': 'Rhino.Geometry.Collections.MeshTopologyEdgeList', 'cs': 'MeshTopologyEdgeList', 'on': 'ON_Mesh',
     'mode': 'child', 'parent': 'MeshX', 'hand': None, 'bridge': False,
     'gen': 'BND_MeshTopologyEdgeListX', 'reg': 'MeshTopologyEdgeListX'},
    # G47: reached through `new BrepLoopList(this)` -- the (Brep) constructor, which nulls
    # m_brepface: members are planned from the bodies specialized for it
    {'type': 'Rhino.Geometry.Collections.BrepLoopList', 'cs': 'BrepLoopList', 'on': 'ON_Brep',
     'mode': 'child', 'parent': 'BrepX', 'hand': None, 'bridge': False,
     'gen': 'BND_BrepLoopListX', 'reg': 'BrepLoopListX'},
    {'type': 'Rhino.Geometry.Collections.BrepTrimList', 'cs': 'BrepTrimList', 'on': 'ON_Brep',
     'mode': 'child', 'parent': 'BrepX', 'hand': None, 'bridge': False,
     'gen': 'BND_BrepTrimListX', 'reg': 'BrepTrimListX'},
    {'type': 'Rhino.Geometry.Collections.BrepFaceList', 'cs': 'BrepFaceList', 'on': 'ON_Brep',
     'mode': 'child', 'parent': 'BrepX', 'hand': None, 'bridge': False,
     'gen': 'BND_BrepFaceListX', 'reg': 'BrepFaceListX'},
    {'type': 'Rhino.Geometry.Collections.BrepEdgeList', 'cs': 'BrepEdgeList', 'on': 'ON_Brep',
     'mode': 'child', 'parent': 'BrepX', 'hand': None, 'bridge': False,
     'gen': 'BND_BrepEdgeListX', 'reg': 'BrepEdgeListX'},
    {'type': 'Rhino.Geometry.Collections.BrepVertexList', 'cs': 'BrepVertexList', 'on': 'ON_Brep',
     'mode': 'child', 'parent': 'BrepX', 'hand': None, 'bridge': False,
     'gen': 'BND_BrepVertexListX', 'reg': 'BrepVertexListX'},
    {'type': 'Rhino.Geometry.Collections.BrepSurfaceList', 'cs': 'BrepSurfaceList', 'on': 'ON_Brep',
     'mode': 'child', 'parent': 'BrepX', 'hand': None, 'bridge': False,
     'gen': 'BND_BrepSurfaceListX', 'reg': 'BrepSurfaceListX'},
    {'type': 'Rhino.Geometry.Collections.NurbsCurvePointList', 'cs': 'NurbsCurvePointList', 'on': 'ON_NurbsCurve',
     'mode': 'child', 'parent': 'NurbsCurveX', 'hand': None, 'bridge': False,
     'gen': 'BND_NurbsCurvePointListX', 'reg': 'NurbsCurvePointListX'},
    {'type': 'Rhino.Geometry.Collections.NurbsCurveKnotList', 'cs': 'NurbsCurveKnotList', 'on': 'ON_NurbsCurve',
     'mode': 'child', 'parent': 'NurbsCurveX', 'hand': None, 'bridge': False,
     'gen': 'BND_NurbsCurveKnotListX', 'reg': 'NurbsCurveKnotListX'},
    {'type': 'Rhino.Geometry.Collections.NurbsSurfacePointList', 'cs': 'NurbsSurfacePointList', 'on': 'ON_NurbsSurface',
     'mode': 'child', 'parent': 'NurbsSurfaceX', 'hand': None, 'bridge': False,
     'gen': 'BND_NurbsSurfacePointListX', 'reg': 'NurbsSurfacePointListX'},
    # -- batch 1 (G43): lists of components, and children with a key
    # (`new NurbsSurfaceKnotList(this, 0)`, `new PointCloudItem(this, index)`)
    {'type': 'Rhino.Geometry.Collections.SubDFaceList', 'cs': 'SubDFaceList', 'on': 'ON_SubD',
     'mode': 'child', 'parent': 'SubDX', 'hand': None, 'bridge': False,
     'gen': 'BND_SubDFaceListX', 'reg': 'SubDFaceListX'},
    {'type': 'Rhino.Geometry.Collections.SubDEdgeList', 'cs': 'SubDEdgeList', 'on': 'ON_SubD',
     'mode': 'child', 'parent': 'SubDX', 'hand': None, 'bridge': False,
     'gen': 'BND_SubDEdgeListX', 'reg': 'SubDEdgeListX'},
    {'type': 'Rhino.Geometry.Collections.SubDVertexList', 'cs': 'SubDVertexList', 'on': 'ON_SubD',
     'mode': 'child', 'parent': 'SubDX', 'hand': None, 'bridge': False,
     'gen': 'BND_SubDVertexListX', 'reg': 'SubDVertexListX'},
    {'type': 'Rhino.Geometry.Collections.NurbsSurfaceKnotList', 'cs': 'NurbsSurfaceKnotList', 'on': 'ON_NurbsSurface',
     'mode': 'child', 'parent': 'NurbsSurfaceX', 'hand': None, 'bridge': False,
     'gen': 'BND_NurbsSurfaceKnotListX', 'reg': 'NurbsSurfaceKnotListX'},
    {'type': 'Rhino.Geometry.PointCloudItem', 'cs': 'PointCloudItem', 'on': 'ON_PointCloud',
     'mode': 'child', 'parent': 'PointCloudX', 'hand': None, 'bridge': False,
     'gen': 'BND_PointCloudItemX', 'reg': 'PointCloudItemX'},
    # `Material.PhysicallyBased => IsPhysicallyBased ? new PhysicallyBasedMaterial(this) : null`
    {'type': 'Rhino.DocObjects.PhysicallyBasedMaterial', 'cs': 'PhysicallyBasedMaterial', 'on': 'ON_Material',
     'mode': 'child', 'parent': 'MaterialX', 'hand': None, 'bridge': False,
     'gen': 'BND_PhysicallyBasedMaterialX', 'reg': 'PhysicallyBasedMaterialX'},
]
BY_CS = {c['cs']: c for c in CLASSES}
# an internal implementation type is its public class (G91: File3dmManifestTable -> ManifestTableX)
for _c in CLASSES:
    if _c.get('impl'):
        BY_CS.setdefault(_c['impl'], _c)
BY_TYPE = {c['type']: c for c in CLASSES}
BY_REG = {c['reg']: c for c in CLASSES}

CVALUE = {'double', 'int', 'bool', 'unsigned int'}
OUTLOCAL = {
    'double*':          ('double {n} = 0;', '{n}'),
    'int*':             ('int {n} = 0;', '{n}'),
    'bool*':            ('bool {n} = false;', '{n}'),
    # value types reach the binding as the GENERATED value types (translated
    # RhinoCommon bodies), converted from the C layer's opennurbs locals
    'ON_UUID*':         ('ON_UUID {n} = ON_nil_uuid;', 'ON_UUID_to_Binding({n})'),
    'unsigned int*':    ('unsigned int {n} = 0;', '{n}'),     # a component id (`uint id = 0; ... ref id`)
}
CS_ARG = {
    'double':   ('double', '{n}'),
    'int':      ('int', '{n}'),
    'bool':     ('bool', '{n}'),
    'uint':     ('unsigned int', '{n}'),
    'string':   ('std::wstring', 'RH3DM_S({n}).data()'),
    # Guid: Python uuid.UUID / JS string, the core binding converters
    'Guid':     ('BND_UUID', 'Binding_to_ON_UUID({n})'),
    # System.Drawing.Color: the old bindings' BND_Color (Python tuple, JS
    # {r,g,b,a}); the C layer takes an int whose encoding the C# call site
    # chooses (value.ToArgb()) -- so no default conversion: only a recorded one
    'Color':    ('BND_Color', None),
}
def seq_elem(cstype):
    """`IEnumerable<Point3d>` -> 'Point3d': a sequence parameter's element (G82),
    bound as a std::vector of the element's binding type; None otherwise"""
    m = re.match(r'^(?:[\w.]*\.)?IEnumerable<(\w+)>$', (cstype or '').replace(' ', ''))   # any namespace spelling
    if not m:
        return None
    e = m.group(1)
    return e if e in VT_CPP or e in ('double', 'int') else None


def seq_cpp(cstype):
    """the binding type of a sequence parameter's element (G82)"""
    e = seq_elem(cstype)
    return None if e is None else VT_CPP.get(e, e)


CS_ARG_CTYPES = {
    'double': {'double'}, 'int': {'int'}, 'bool': {'bool'}, 'uint': {'unsigned int'},
    'string': {'const RHMONO_STRING*'},
    'Guid': {'ON_UUID'},
    'Color': {'int', 'unsigned int'},
}
# Translated value types (api/valuetypes.json) mirror RhinoCommon's layout,
# which is the C layer's struct layout (sizes static_assert'ed by the
# RH3DM_Ptr/RH3DM_Val adapters): they cross the C boundary by their bits,
# no per-type conversion; register_value_types() fills these.
VT_CPP = {}          # C# name -> generated struct (Plane -> BND_PlaneX)
VT_FIELDS = {}       # C# name -> its field names in layout order (m_x, m_y, m_z) (G72)
# a scratch out local the C# body declares itself (`Plane plane; ...
# IsPlanar(ptr, true, ref plane, tol)`) is typed by its C pointer type
C_VT = {'ON_3dPoint*': 'Point3d', 'ON_3DPOINT_STRUCT*': 'Point3d',
        'ON_3dVector*': 'Vector3d', 'ON_3DVECTOR_STRUCT*': 'Vector3d',
        'ON_Interval*': 'Interval', 'ON_INTERVAL_STRUCT*': 'Interval',
        'ON_PLANE_STRUCT*': 'Plane', 'ON_Xform*': 'Transform', 'ON_Line*': 'Line', 'ON_BoundingBox*': 'BoundingBox',
        'ON_Arc*': 'Arc', 'ON_Circle*': 'Circle', 'ON_Sphere*': 'Sphere',
        'ON_2DPOINT_STRUCT*': 'Point2d', 'ON_2dVector*': 'Vector2d', 'ON_2DVECTOR_STRUCT*': 'Vector2d',
        # sizes static_assert'ed where they cross (RH3DM_Ptr)
        'ON_Cone*': 'Cone', 'ON_Cylinder*': 'Cylinder', 'ON_Torus*': 'Torus', 'ON_Ellipse*': 'Ellipse',
        # rhcommon_c.h's mirror of ON_SubDEdgeSharpness (two floats; G109)
        'ON_SUBD_EDGE_SHARPNESS_STRUCT*': 'SubDEdgeSharpness'}
VT_JS_KEY = {}       # generated struct -> JS shape-test property
VT_JS_FIELDS = {}    # generated struct -> every JS field of its value object (G100: Arc vs Circle)


def field_backed(vt, cs):
    """{field: property} for properties that ARE a field: the translated
    getter is `return <field>;` (and a setter, if any, assigns it). Decided
    from the bodies, not the names -- Transform's M00 is field m_00."""
    out = {}
    fields = {f['Name'] for t in vt['types'] if t['Cs'] == cs for f in t['Fields']}
    for m in vt['members']:
        if m['Type'] != cs or m['Kind'] != 'property' or m.get('Static') or not m.get('Ok', True):
            continue
        g = re.search(r'::%s\(\)(?: const)?\n\{\n  return (\w+);\n\}' % re.escape(m['Name']), m.get('Def') or '')
        if g and g.group(1) in fields and g.group(1) not in out:
            out[g.group(1)] = m['Name']
    return out


CLASS_BODIES = {}     # (simple type, Kind, Name, (C# param types)) -> class-mode translation (G63)
DATA_CPP = {}         # C# data class -> generated class (ConstructionPlane -> BND_ConstructionPlaneX) (G64)
DATA_INFO = {}        # C# data class -> {'fields': {...}, 'props': {name: has_setter}, 'methods': {...}}


def register_value_types(vt):
    CLASS_BODIES.clear(); DATA_CPP.clear(); DATA_INFO.clear()
    for t in vt['types']:
        if t.get('IsData'):
            DATA_CPP[t['Cs']] = t['Cpp']
            DATA_INFO[t['Cs']] = {'fields': {f['Name'] for f in t['Fields']}, 'props': {}, 'methods': set()}
    for m in vt['members']:
        if m['Type'] in DATA_INFO and m.get('Ok'):
            if m['Kind'] == 'property' and not m.get('Static'):
                DATA_INFO[m['Type']]['props'][m['Name']] = bool(m.get('Setter'))
            elif m['Kind'] == 'method':
                DATA_INFO[m['Type']]['methods'].add(m['Name'])
    for r in vt.get('classmembers') or []:
        CLASS_BODIES[(r['Type'], r['Kind'], r['Name'], tuple(r.get('ParamsCs') or ()))] = r
    for m in vt['members']:
        if m.get('Ok') and m.get('Static') and m['Kind'] in ('property', 'static-field'):
            VT_STATICS.add((m['Type'], m['Name']))
        if m.get('Ok') and m['Kind'] == 'field-const':
            VT_CONSTS.add((m['Type'], m['Name']))
        if m.get('Ok') and m['Kind'] == 'ctor' and m.get('Params') and all(
                q['Type'] in ('double', 'float', 'int') and not q.get('Mode') for q in m['Params']):
            VT_NUM_CTORS.add((m['Type'], len(m['Params'])))
        if m.get('Ok') and m['Kind'] == 'property' and not m.get('Static'):
            VT_PROPS.add((m['Type'], m['Name']))
            VT_PROP_RET[(m['Type'], m['Name'])] = m.get('Ret')
            if m.get('Setter'):
                VT_PROP_SET.add((m['Type'], m['Name']))
    VT_DEFINED_ENUMS.clear()
    for e in vt.get('enums', []):
        pub_t = CS_ENUMS.get(e['Cs'])
        if pub_t is not None and pub_t['gen'] and pub_t['cpp'] == e['Cpp']:
            # a GENERATED public enum the translated bodies define verbatim (G107: DracoColorFormat,
            # a data class's field): one type -- the values header defines it, the generator registers it
            VT_ENUMS[e['Cpp']] = pub_t['cpp']
            VT_DEFINED_ENUMS.add(e['Cs'])
            continue
        if pub_t is None or pub_t['gen']:
            raise SystemExit('translated enum %s: no registered public type (add it to CS_ENUMS)' % e['Cs'])
        VT_ENUMS[e['Cpp']] = pub_t['cpp']
    for t in vt['types']:
        if t.get('IsStaticClass') or not t.get('LayoutSize') or t.get('IsData'):
            continue                       # a data class never crosses the C boundary by layout (G64)
        cs, cpp = t['Cs'], t['Cpp']
        VT_CPP[cs] = cpp
        VT_FIELDS[cs] = [f['Name'] for f in t.get('Fields') or []]
        OUTLOCAL.setdefault('VT:' + cs, ('%s {n};' % cpp, '{n}'))
        CS_RET_CPP.setdefault(cs, cpp)
        CS_RET_EXPR.setdefault(cs, '{n}')
        CS_RET_CPP['VT:' + cs] = cpp          # a value-type local, whatever cs's
        CS_RET_EXPR['VT:' + cs] = '{n}'       # explicit opennurbs conversion
        first = (t.get('Fields') or [{}])[0].get('Name', '')
        key = field_backed(vt, cs).get(first)
        if key:
            VT_JS_KEY.setdefault(cpp, key)
        fb = field_backed(vt, cs)
        jf = [fb[f['Name']] for f in t.get('Fields') or [] if fb.get(f['Name'])]
        if jf:
            VT_JS_FIELDS.setdefault(cpp, jf)


CS_RET_DIRECT = {'bool': 'bool', 'int': 'int', 'double': 'double', 'uint': 'unsigned int'}
CS_RET_OUT = {'Guid': 'ON_UUID*'}
CS_RET_EXPR = {'double': '{n}', 'int': '{n}', 'bool': '{n}',
               'Guid': 'ON_UUID_to_Binding({n})'}
CS_RET_CPP = {'double': 'double', 'int': 'int', 'bool': 'bool',
              'Guid': 'BND_UUID'}
# interop-holder wrappers (C# Rhino.Runtime.InteropWrappers) -> C-side type,
# generated local, and return treatment
HOLDERS = {
    'StringHolder':      {'ctype': 'CRhCmnStringHolder*',
                          'local': 'CRhCmnStringHolder {n};',
                          'cs': 'string', 'conv': 'RH3DM_W({n})'},
    'StringWrapper':     {'ctype': 'ON_wString*',
                          'local': 'ON_wString {n};',
                          'cs': 'string', 'conv': 'std::wstring(static_cast<const wchar_t*>({n}))'},
    'SimpleArrayDouble': {'ctype': 'ON_SimpleArray<double>*',
                          'local': 'ON_SimpleArray<double> {n};',
                          'cs': 'double[]', 'elem': '{n}[i]'},
    'SimpleArrayInt':    {'ctype': 'ON_SimpleArray<int>*',
                          'local': 'ON_SimpleArray<int> {n};',
                          'cs': 'int[]', 'elem': '{n}[i]'},
    'SimpleArrayPoint3d':{'ctype': 'ON_3dPointArray*',
                          'local': 'ON_3dPointArray {n};',
                          'cs': 'Point3d[]', 'elem': 'RH3DM_P({n}[i])'},
    'SimpleArrayGuid':   {'ctype': 'ON_SimpleArray<ON_UUID>*',
                          'local': 'ON_SimpleArray<ON_UUID> {n};',
                          'cs': 'Guid[]', 'elem': 'ON_UUID_to_Binding({n}[i])'},
    # holders of object POINTERS: only the wrap-tuple loop reads them (G72); the
    # plain holder shape refuses them (an element is a pointer, not a value)
    'SimpleArrayCurvePointer': {'ctype': 'ON_SimpleArray<ON_Curve*>*',
                               'local': 'ON_SimpleArray<ON_Curve*> {n};',
                               'cs': 'Curve[]', 'ptr': 'ON_Curve'},
}

# C functions that corrupt memory for ordinary arguments: every member calling
# one is refused until the C layer is fixed upstream (the generator never
# ships a member it knows overruns the heap). Keyed by C function -> finding.
UNSAFE_NATIVES = {
    # none open. ON_PointCloud_InsertPoints (pilot-findings #34: the tail copied to the
    # old end instead of index+count) was fixed upstream as U11 (RH-99686, 2026-10-20)
}

NET_DEFAULT_MESSAGES = {
    'ArgumentOutOfRangeException': 'Specified argument was out of the range of valid values.',
    'ArgumentNullException': 'Value cannot be null.',
    'IndexOutOfRangeException': 'Index was outside the bounds of the array.',
    'ArgumentException': 'Value does not fall within the expected range.',
}


def net_message(exc, msg):
    """the text .NET reports for `throw new <exc>(<msg>)` (G101), C-string escaped. The manifest
    keeps a literal or `nameof(p)` argument as Message: a ONE-word message of ArgumentOutOfRange /
    ArgumentNull is the parameter name their one-argument constructors take (a (paramName, message)
    pair records the message sentence); no message is the parameterless constructor's default text"""
    exc = (exc or '').split('.')[-1]
    dflt = NET_DEFAULT_MESSAGES.get(exc, exc)
    if not msg:
        text = dflt
    elif exc in ('ArgumentOutOfRangeException', 'ArgumentNullException') and re.match(r'^[A-Za-z_]\w*$', msg):
        text = "%s (Parameter '%s')" % (dflt, msg)
    else:
        text = msg
    return text.replace('\\', '\\\\').replace('"', "'")


EXMAP = {
    'IndexOutOfRangeException': 'std::out_of_range',
    'ArgumentOutOfRangeException': 'std::out_of_range',
    'ArgumentNullException': 'std::invalid_argument',
    'ArgumentException': 'std::invalid_argument',
    'InvalidOperationException': 'std::runtime_error',
    'NotSupportedException': 'std::runtime_error',
    # -> Python FileNotFoundError (translator registered in gen_init.cpp)
    'FileNotFoundException': 'RH3DM_FileNotFound',
    'ApplicationException': 'std::runtime_error',
    # a File3dm proxy whose component is no longer in the model (CommonObject.ConstPointer)
    'DocumentCollectedException': 'std::runtime_error',
    # System.Exception itself (Decal.Mapping's `default: throw new Exception(...)`; G60)
    'Exception': 'std::runtime_error',
}
EXMAP_BASE = dict(EXMAP)
# RhinoCommon's own exception types (specgen Exception; G60): `throw new
# DecalReadOnlyException()` is its base type with the message its parameterless
# constructor fixes. index_types extends EXMAP with them; EXMSG holds the messages
EXMSG = {}
# C# enum parameter/return types. The C side always takes/returns int (the C#
# call site casts, `(int)continuityType`). 'cpp' is the binding-side spelling:
# an enum the HAND bindings already register (reused -- registering the same
# python name twice would throw), or an RH3DM_-prefixed enum class this
# generator defines and registers from the manifest's Values.
CS_ENUMS = {
    'CoordinateSystem': {'cpp': 'ON::coordinate_system', 'gen': False},
    'TransformSimilarityType': {'cpp': 'TransformSimilarityType', 'gen': False},
    'TransformRigidType': {'cpp': 'TransformRigidType', 'gen': False},
    'UnitSystem':       {'cpp': 'ON::LengthUnitSystem', 'gen': False},
    'Continuity':       {'cpp': 'RH3DM_Continuity', 'gen': True},
    'IsoStatus':        {'cpp': 'RH3DM_IsoStatus', 'gen': True},
}


RENUMBERED_ENUMS = {}   # RhinoCommon enum -> (the hand C++ enum not reused, the differing members) (G78)
_CPP_ENUMS = None


def cpp_enum_values(cpp):
    """the members and values of a C++ enum declared in opennurbs or the hand
    bindings' headers (`ON_Dithering::Methods` -> {SimpleNoise: 0, ...}), or None"""
    global _CPP_ENUMS
    if _CPP_ENUMS is None:
        _CPP_ENUMS = {}
        heads = glob.glob(os.path.join(ROOT, 'src', 'lib', 'opennurbs', '*.h')) + glob.glob(os.path.join(ROOT, 'src', 'bindings', '*.h'))
        for h in heads:
            t = open(h, errors='replace').read()
            for mm in re.finditer(r'\benum\s+(?:class\s+|struct\s+)?(\w+)\s*(?::\s*[\w ]+)?\{([^{}]*)\}', t):
                body = re.sub(r'//[^\n]*|/\*.*?\*/', '', mm.group(2), flags=re.S)
                vals, cur, ok = {}, -1, True
                for part in [q.strip() for q in body.split(',') if q.strip()]:
                    nm = part.split('=')[0].strip()
                    if '=' in part:
                        v = part.split('=', 1)[1].strip()
                        try:
                            cur = int(v, 0)
                        except ValueError:
                            if v in vals:
                                cur = vals[v]
                            else:
                                ok = False
                                break
                    else:
                        cur += 1
                    vals[nm] = cur
                if ok:
                    _CPP_ENUMS.setdefault(mm.group(1), []).append(vals)
    found = _CPP_ENUMS.get(cpp.split('::')[-1]) or []
    return found[0] if len(found) == 1 else None


def extend_enum_table(manifest):
    """every namespace-level portable RhinoCommon enum gets a CS_ENUMS entry:
    the old bindings' C++ type when they register that exact Python name
    (re-registering it would throw), otherwise a generated RH3DM_<Name>
    defined from the manifest's values. Nested enums (DimensionStyle.Field)
    are left out: their short names repeat across types."""
    hand, hand_js = {}, {}
    for f in glob.glob(os.path.join(ROOT, 'src', 'bindings', '*.cpp')):
        text = open(f, errors='replace').read()
        for cpp, name in re.findall(r'py::enum_<([^>]+)>\(m, "(\w+)"', text):
            hand.setdefault(name, cpp.strip())
        # nested under a class: `py::class_<...> pyDimStyle(m, "DimensionStyle");
        # py::enum_<ON_DimStyle::field>(pyDimStyle, "Field")` is DimensionStyle.Field
        cls_vars = dict((v, n) for v, n in re.findall(r'py::class_<[^;]*?>\s*(\w+)\s*\(m,\s*"(\w+)"', text))
        for cpp, var, name in re.findall(r'py::enum_<([^>]+)>\((\w+), "(\w+)"', text):
            if var in cls_vars:
                hand.setdefault(cls_vars[var] + '.' + name, cpp.strip())
        for cpp, name in re.findall(r'\benum_<([^>]+)>\("(\w+)"\)', text):
            hand_js.setdefault(cpp.strip(), name)
    # a hand enum declared only inside a .cpp (bnd_intersect.cpp's
    # LineSphereIntersection) cannot be named by generated code -- and its
    # Python name is taken, so no RH3DM_ copy either: members using it are
    # refused (G49; before, a tuple silently dropped it)
    in_headers = set()
    for f in glob.glob(os.path.join(ROOT, 'src', 'bindings', '*.h')):
        in_headers |= set(re.findall(r'\benum\s+(?:class\s+)?(\w+)', open(f, errors='replace').read()))
    def visible(cpp):
        return '::' in cpp or cpp.startswith('ON') or cpp in in_headers
    HIDDEN_ENUMS.clear()
    HIDDEN_ENUMS.update(n for n, c in hand.items() if not visible(c))
    # a hand enum is reused only when it numbers RhinoCommon's members as RhinoCommon
    # does (G78: ON_Dithering::Methods has SimpleNoise = 0, RhinoCommon 2) -- compared
    # on the members both name; otherwise the generated enum, with RhinoCommon's values
    rc_values = {}
    tnames = {y['Type'] for y in manifest['members'] if y['Kind'] == 'type'}
    for x in manifest['members']:
        if x['Kind'] == 'enum' and x['Variant'] == 'portable' and x.get('EnumInts') and x.get('Values'):
            names = [v.split('=')[0].strip() for v in x['Values']]
            k = (x['Type'].replace('+', '.').split('.')[-1] + '.' + x['Name']) if x['Type'] in tnames else x['Name']
            rc_values[k] = rc_values[k.replace('.', '')] = dict(zip(names, x['EnumInts']))   # hand keys: either spelling
    for k in list(hand):
        cv = cpp_enum_values(hand[k])
        rv = rc_values.get(k)
        if cv and rv and any(n in cv and cv[n] != v for n, v in rv.items()):
            RENUMBERED_ENUMS[k] = (hand.pop(k), {n: (v, cv.get(n)) for n, v in rv.items() if cv.get(n) != v})
    type_names = {x['Type'] for x in manifest['members'] if x['Kind'] == 'type'}
    seen, nested = {}, {}
    for x in manifest['members']:
        if x['Kind'] != 'enum' or x['Variant'] != 'portable':
            continue
        if x['Type'] in type_names:
            # nested (DimensionStyle.ArrowType): keyed by its owner's simple name
            nested.setdefault(x['Type'].replace('+', '.').split('.')[-1] + '.' + x['Name'], set()).add(x['Type'])
            continue
        seen.setdefault(x['Name'], set()).add(x['Type'])
    for name, owners in seen.items():
        if name in CS_ENUMS or len(owners) != 1 or name in HIDDEN_ENUMS:
            continue
        CS_ENUMS[name] = ({'cpp': hand[name], 'gen': False} if name in hand
                          else {'cpp': 'RH3DM_' + name, 'gen': True})
    # nested: Python registers it IN its owner's generated class (DimensionStyleX.ArrowType,
    # as RhinoCommon nests it); JS has no nesting -- owner + name (the old bindings'
    # DimensionStyleField). A hand registration under the same qualified name is reused.
    for key, owners in nested.items():
        if key in CS_ENUMS or len(owners) != 1:
            continue
        owner = key.split('.')[0]
        joined = key.replace('.', '')
        if key in HIDDEN_ENUMS or joined in HIDDEN_ENUMS:
            continue
        if key in hand:
            CS_ENUMS[key] = {'cpp': hand[key], 'gen': False, 'owner': owner,
                             'js': hand_js.get(hand[key], joined)}
        elif joined in hand:
            # registered by the old bindings at module level under the joined name
            # (`EnvironmentBackgroundProjections`): reuse it, never register it twice
            CS_ENUMS[key] = {'cpp': hand[joined], 'gen': False, 'owner': owner,
                             'js': hand_js.get(hand[joined], joined)}
        else:
            CS_ENUMS[key] = {'cpp': 'RH3DM_' + key.replace('.', '_'), 'gen': True, 'owner': owner,
                             'js': key.replace('.', '')}
            if key in RENUMBERED_ENUMS or joined in RENUMBERED_ENUMS:
                # the old bindings register this JS name with their (differently numbered)
                # enum: the generated one takes its owner's generated name (G78)
                CS_ENUMS[key]['js'] = owner + 'X' + key.split('.')[-1]
    # a renumbered enum at module level would take the hand enum's Python name: refused
    for k in RENUMBERED_ENUMS:
        if '.' not in k and not any(c.get('owner') for kk, c in CS_ENUMS.items() if kk.replace('.', '') == k):
            CS_ENUMS.pop(k, None)
            HIDDEN_ENUMS.add(k)


# what the JS post-script (src/generated/gen_post.js, linked with --post-js) installs on the
# embind classes after the module initializes (G101): `keep` -- (class, member) whose result is a
# view of `this` (Python's keep_alive<0,1> / reference_internal): the result holds its parent;
# `iters` -- iterator classes whose next().value holds the iterator; `static_props` -- (class,
# name) of RhinoCommon static properties, registered as zero-argument class functions, made
# getters; `enumerable` -- classes whose RhinoCommon type is IEnumerable: for...of
# G112: `keep_args` -- (class, member, argument index, static): the result holds that argument (Python
# keep_alive<0, N>); `keep_set` -- (class, property): the object holds the value its setter took
# (Python keep_alive<1, 2>, a borrowing modeled-field setter)
# 'user_strings' (D62, Luis 2026-10-20): a class registering RhinoCommon's internal CommonObject
# user-string members under their public names, where rhino3dm has them and RhinoCommon does not
JS_POST = {'keep': [], 'iters': [], 'static_props': [], 'enumerable': [], 'not_iterable': [], 'grid': [],
           'keep_args': [], 'keep_set': []}

# IEnumerable classes whose RhinoCommon enumerator is a private class over two counts and a
# (u, v) getter, row-major in u (G102): {C# simple name: (count u, count v, getter)}. Each entry
# cites the enumerator it reproduces; its members must be generated, else nothing is emitted.
GRID_ENUMS = {
    # NurbsSurfacePointList.NurbsSrfEnum: position p -> GetControlPoint(p / CountV, p % CountV)
    'NurbsSurfacePointList': ('CountU', 'CountV', 'GetControlPoint'),
}


# JS value objects (G101; Luis 2026-10-17 "leaking fields"): a struct field no RhinoCommon property
# reads directly (field_backed) was a JS field under its C++ name (CylinderX.m_basecircle). Here the
# RhinoCommon properties that DECOMPOSE it -- together exactly its content, each with a getter and
# a setter -- become the value object's fields instead (getter/setter fields; set in this order).
# A property without a setter (readonly struct) names `('ctor', other field)`: set through the
# struct's constructor (type, other) as RhinoCommon builds it. The generator checks every name.
VALUE_FIELD_PROPS = {
    'Cylinder': {'m_basecircle': ['BasePlane', 'Radius']},          # the circle = its plane + radius
    'ControlPoint': {'m_vertex': ['X', 'Y', 'Z', 'Weight']},        # the homogeneous components, raw
    'ComponentIndex': {'m_type': [('ComponentIndexType', 'm_index')]},
    # G109 (Luis 2026-10-20): no property reads either end (only this[i] / EndSharpness(i)), so a
    # plain string names the RAW field after the RhinoCommon constructor parameter at its position,
    # SubDEdgeSharpness(double sharpness0, double sharpness1) -- never through that constructor:
    # FromInterval couples the ends (Crease needs both at 5), so a field-by-field rebuild through it
    # would turn {5, 5} into NaN. The generator checks the constructor and position.
    'SubDEdgeSharpness': {'m_sharpness0': 'sharpness0', 'm_sharpness1': 'sharpness1'},
}


def cs_enumerable(cs):
    """whether RhinoCommon type `cs` (simple name) or a base implements IEnumerable (JS for...of; G101)"""
    seen, todo = set(), [t for t in SIMPLE_TYPES.get(cs, [])]
    while todo:
        t = todo.pop()
        if t in seen:
            continue
        seen.add(t)
        for b in (TYPE_RECS.get(t) or {}).get('Bases') or []:
            if re.match(r'^(System\.Collections\.Generic\.)?(IEnumerable|IList|IReadOnlyList|ICollection)\b', b):
                return True
            todo += SIMPLE_TYPES.get(b.split('<')[0].split('.')[-1], [])
    return False


def value_self(cls):
    """a value-mode class's object (G104: a `fileview` class resolves its file view first)"""
    return 'RH3DM_Val()' if cls.get('fileview') else 'const_cast<%s*>(&m_val)' % cls['on']


def camel(n):
    return n[0].lower() + n[1:]


CS_NAMESPACES = {'Rhino', 'Geometry', 'DocObjects', 'FileIO', 'Render', 'Collections', 'Display', 'Runtime',
                 'System', 'Drawing'}


def cs_unqualify(t):
    """Rhino.Geometry.Plane / Geometry.Plane -> Plane (namespace prefixes
    only: a nested name like DimensionStyle.ArrowFit is kept)"""
    if t and '.' in t:
        parts = t.split('.')
        while len(parts) > 1 and parts[0] in CS_NAMESPACES:
            parts = parts[1:]
        return '.'.join(parts)
    return t


def cs_signature(member):
    """(return type, [(mode, type, name)]) from the manifest's STRUCTURED
    record (specgen writes Roslyn's parse: Returns + Params) -- no signature
    text parsing, so generics and default values cannot confuse it.
    Unsupported parameter modifiers (params, this) yield (None, None).
    A bare nested enum name resolves in the member's own type, as C# does
    (`ArrowType` in DimensionStyle is DimensionStyle.ArrowType)."""
    if 'Params' not in member and 'Returns' not in member:
        return None, None
    owner = member.get('Type', '').replace('+', '.').split('.')[-1]
    def q(t):
        return owner + '.' + t if t and '.' not in t and (owner + '.' + t) in CS_ENUMS else t
    ret = q(cs_unqualify(member.get('Returns') or member.get('Type', '').split('.')[-1]))
    params = []
    for p in member.get('Params') or []:
        mode = p.get('Mode') or ''
        if mode == 'in':
            mode = ''                      # by-value for our purposes
        if mode not in ('', 'out', 'ref'):
            return None, None
        params.append((mode, q(cs_unqualify(p['Type'])), p['Name']))
    return ret, params


def local_value(init):
    """a local's initializer as (C++ expression, C# value type or None):
    literals as written, value-type statics (Plane.WorldXY) as the generated
    static getter; anything else is unknown (None)"""
    if init is None:
        return None
    t = init.strip()
    if re.match(r'^-?(\d+\.?\d*([eE][-+]?\d+)?|\.\d+)[dDfF]?$', t):
        return t.rstrip('dDfF'), None
    if t in ('true', 'false'):
        return t, None
    if t == 'IntPtr.Zero':
        return 'nullptr', None
    # `Guid id = default(Guid);` and the other zero values C# spells default(T)
    zero = {'default(Guid)': 'ON_nil_uuid', 'Guid.Empty': 'ON_nil_uuid', 'default(int)': '0',
            'default(double)': '0.0', 'default(bool)': 'false', 'default(uint)': '0'}
    if t in zero:
        return zero[t], None
    mm = re.match(r'^default\((\w+)\)$', t)
    if mm and mm.group(1) in VT_CPP:
        # `Point3d rc = default(Point3d);`: the all-zero struct (value-initialized)
        return '%s{}' % VT_CPP[mm.group(1)], mm.group(1)
    # `new Point3d(0, 0, 0)` -- the translated constructor, numeric literals only;
    # integer literals written as doubles so no other one-argument constructor
    # (Transform(Transform)) can compete in overload resolution (G49)
    mm = re.match(r'^new\s+([\w.]+)\s*\((.*)\)$', t)
    if mm and cs_unqualify(mm.group(1)) in VT_CPP:
        vt = cs_unqualify(mm.group(1))
        vals = [v.strip() for v in mm.group(2).split(',')]
        num = r'^-?(\d+\.?\d*([eE][-+]?\d+)?|\.\d+)[dDfF]?$'
        if (vt, len(vals)) in VT_NUM_CTORS and all(re.match(num, v) for v in vals):
            vals = [v.rstrip('dDfF') for v in vals]
            vals = [v if re.search(r'[.eE]', v) else v + '.0' for v in vals]
            return '%s(%s)' % (VT_CPP[vt], ', '.join(vals)), vt
        return None
    mm = re.match(r'^RhinoMath\.(\w+)$', t)
    if mm and ('RhinoMath', mm.group(1)) in VT_CONSTS:
        return 'RH3DM_RhinoMath::' + mm.group(1), None
    mm = re.match(r'^(\w+)\.(\w+)$', cs_unqualify(t))     # Geometry.Transform.Unset too
    if mm and mm.group(1) in VT_CPP and (mm.group(1), mm.group(2)) in VT_STATICS:
        return '%s::%s()' % (VT_CPP[mm.group(1)], mm.group(2)), mm.group(1)
    return None






PY_OPS = {('+', 2): 'add', ('-', 2): 'sub', ('*', 2): 'mul', ('/', 2): 'truediv',
          ('%', 2): 'mod', ('==', 2): 'eq', ('!=', 2): 'ne', ('<', 2): 'lt',
          ('>', 2): 'gt', ('<=', 2): 'le', ('>=', 2): 'ge',
          ('-', 1): 'neg', ('+', 1): 'pos'}
CS_OP_NAMES = {('+', 2): 'op_Addition', ('-', 2): 'op_Subtraction',
               ('*', 2): 'op_Multiply', ('/', 2): 'op_Division', ('%', 2): 'op_Modulus',
               ('==', 2): 'op_Equality', ('!=', 2): 'op_Inequality',
               ('<', 2): 'op_LessThan', ('>', 2): 'op_GreaterThan',
               ('<=', 2): 'op_LessThanOrEqual', ('>=', 2): 'op_GreaterThanOrEqual',
               ('-', 1): 'op_UnaryNegation', ('+', 1): 'op_UnaryPlus'}



# G106 (Luis: option A for every value type): the JS name of a value-type member -- camelCase like the
# generated classes; an operator by RhinoCommon's method name for it (Point3d.Add, Vector3d.Negate,
# Transform.Multiply); None: not registered in JS (.NET plumbing, operators with no method name)
JS_VT_OPS = {'op_Addition': 'add', 'op_Subtraction': 'subtract', 'op_Multiply': 'multiply',
             'op_Division': 'divide', 'op_UnaryNegation': 'negate', 'op_Equality': 'equals'}
# CompareTo stays (compareTo): RhinoCommon's public ordering, the only one JS has without the operators
JS_VT_DROP = {'GetHashCode', 'Clone', 'op_Inequality', 'op_UnaryPlus', 'op_Modulus',
              'op_LessThan', 'op_GreaterThan', 'op_LessThanOrEqual', 'op_GreaterThanOrEqual'}
OBSOLETE = set()     # (C# simple type, member name, parameter count) marked [Obsolete] (manifest; G106)


# a JS function (the value type's namespace is a constructor) already owns these: `Vector3dX.length` would
# collide with Function.length -- RhinoCommon's spelling is kept (Vector3dX.Length(v); G106)
JS_FN_OWN = {'length', 'name', 'prototype', 'caller', 'arguments', 'apply', 'call', 'bind', 'toString'}


def js_vt_name(name):
    if name in JS_VT_DROP:
        return None
    jn = JS_VT_OPS.get(name) or camel(name)
    return name if jn in JS_FN_OWN else jn


VT_ENUMS = {}        # RH3DM_<Name> -> the C++ type the bindings register under <Name>
VT_DEFINED_ENUMS = set()   # generated public enums the translated values header defines (G107)
VT_STATICS = set()   # (C# type, name) of translated static getters (Plane.WorldXY)
VT_CONSTS = set()    # (C# type, name) of translated constants (RhinoMath.UnsetValue)
VT_PROPS = set()     # (C# type, name) of translated instance properties (Interval.Min)
VT_PROP_RET = {}     # (C# type, name) -> C++ return type of that property's getter
VT_PROP_SET = set()  # (C# type, name) of translated instance properties with a setter (G67)
VT_NUM_CTORS = set() # (C# type, arity) of translated all-numeric constructors (Point3d(x, y, z))
HIDDEN_ENUMS = set() # Python names of hand enums declared only in a .cpp (see extend_enum_table)



BRIDGE_STATICS = {}     # (class C# name, static name) -> generated static plans
BRIDGE_CANDIDATES = {}  # value type C# name -> manifest members delegating to a class static
BRIDGE_INCLUDES = {}    # value type C# name -> generated class headers its bindings need


# ---------------------------------------------------------------- RhinoCommon types
# the manifest's `type` records, indexed once (index_types): inheritance with
# generic arguments, and properties whose getter is a constant
TYPE_RECS = {}       # full type -> type record
SIMPLE_TYPES = {}    # simple name -> [full types]
CONST_PROPS = {}     # (full type, property) -> (C# constant text, value or None)
PLANNED = {}         # registry name -> its plans, filled as classes are planned (in CLASSES order)
ENUM_VALUES = {}     # C# enum name -> its declared values (ints, distinct), when all are literal
ENUM_NAMED = {}      # C# enum name -> {member: value}, same condition
WRAPPER = {}         # the Variant value wrapper's natives (specgen Wrapper on the internal type)
GUARD_ENUMS = set()  # C# enums a guard condition names (must be defined even if no signature uses them)
C_BY_NAME = {}       # the C surface by function name (bnd_gen fills it)
PROPS_BY_TYPE = {}   # full type -> its portable property records (nearest-declaration lookups)
NO_OWNED_ABSTRACT = set()  # abstract bases whose guards ask RH3DM_NoOwnedPtr() (derived classes answer)


# .NET's own enums a translated body names (G97: Sun.GetDateTime(DateTimeKind)): manifest-shaped
# records, as the runtime declares them -- specgen's translator declares the same (Translate.cs)
SYSTEM_ENUMS = [
    {'Kind': 'enum', 'Type': 'System', 'Name': 'DateTimeKind', 'Variant': 'portable',
     'Values': ['Unspecified = 0', 'Utc = 1', 'Local = 2'], 'EnumInts': [0, 1, 2]},
]


def with_system_enums(manifest):
    """the manifest with SYSTEM_ENUMS appended (once)"""
    have = {(x['Type'], x['Name']) for x in manifest['members'] if x['Kind'] == 'enum'}
    manifest['members'] += [e for e in SYSTEM_ENUMS if (e['Type'], e['Name']) not in have]
    return manifest


def index_types(manifest):
    TYPE_RECS.clear(); SIMPLE_TYPES.clear(); CONST_PROPS.clear(); PLANNED.clear(); ENUM_VALUES.clear()
    OBSOLETE.clear()
    OBSOLETE.update((x['Type'].replace('+', '.').split('.')[-1], x['Name'], len(x.get('Params') or []))
                    for x in manifest['members'] if 'Obsolete' in x)
    PROPS_BY_TYPE.clear(); NO_OWNED_ABSTRACT.clear()
    for x in manifest['members']:
        if x['Kind'] == 'property' and x['Variant'] in ('portable', 'rhino3dm-only'):
            PROPS_BY_TYPE.setdefault(x['Type'], []).append(x)
    owners = {}
    for x in manifest['members']:
        if x['Kind'] == 'enum' and x['Variant'] in ('portable', 'rhino3dm-only') and x.get('Values'):
            owners.setdefault(x['Name'], []).append(x)
            # also by its declaring type (RenderChannels.Modes): a nested enum's
            # short name can repeat (DataSources.Modes) where this key does not
            owners.setdefault(x['Type'].replace('+', '.').split('.')[-1] + '.' + x['Name'], []).append(x)
    for name, recs in owners.items():
        if len(recs) != 1:
            continue                          # the short name repeats: ambiguous
        vals, nxt, named = [], 0, {}
        for v in recs[0]['Values']:
            n, _, e = v.partition('=')
            n, e = n.strip(), e.strip()
            if e:
                if e in named:
                    val = named[e]            # RenderMaterial = Material
                else:
                    try:
                        val = int(e, 0)
                    except ValueError:
                        vals = None
                        break
            else:
                val = nxt
            named[n] = val
            vals.append(val)
            nxt = val + 1
        # members valued by the C layer's constants (`Text = UnsafeNativeMethods.TextRunTypeConsts.Text`;
        # G96): the extractor's resolved values, one per member, in order
        ints = recs[0].get('EnumInts')
        if vals is None and ints and len(ints) == len(recs[0]['Values']):
            named = {v.partition('=')[0].strip(): iv for v, iv in zip(recs[0]['Values'], ints)}
            vals = list(ints)
        if vals:
            ENUM_VALUES[name] = sorted(set(vals))
            ENUM_NAMED[name] = dict(named)
    for x in manifest['members']:
        if x['Kind'] == 'type':
            # a partial class yields one record per declaration (its bases differ):
            # merge -- union the bases, keep the first non-null of each fact
            prev = TYPE_RECS.get(x['Type'])
            if prev is None:
                TYPE_RECS[x['Type']] = dict(x)
                SIMPLE_TYPES.setdefault(x['Type'].replace('+', '.').split('.')[-1], []).append(x['Type'])
            else:
                for k, v in x.items():
                    if k == 'Bases' and v:
                        pb = list(prev.get('Bases') or []); pa = list(prev.get('BaseArgs') or [None] * len(pb))
                        for i, b in enumerate(v):
                            if b not in pb:
                                pb.append(b); pa.append((x.get('BaseArgs') or [None] * len(v))[i])
                        prev['Bases'], prev['BaseArgs'] = pb, (pa if any(pa) else None)
                    elif prev.get(k) is None and v is not None and k != 'BaseArgs':
                        prev[k] = v
        elif x['Kind'] == 'property' and x.get('Const') and x['Variant'] in ('portable', 'rhino3dm-only'):
            CONST_PROPS[(x['Type'], x['Name'])] = (x['Const'], x.get('ConstValue'))
    # VIEWS: a registry class gets a second storage state -- (ONX_Model*, id),
    # resolved on every access -- when RhinoCommon constructs it that way:
    #  - the File3dm proxy idiom (specgen FileProxy: `new Layer(id, file)`), or
    #  - a const object whose pointer a File3dm PROXY parent owns (specgen
    #    ParentViews: GeometryBase/ObjectAttributes with a File3dmObject parent),
    #    where the parent's helper resolves (file pointer, id) -- the same pair.
    # Writes either RESOLVE again (edits reach the file: Layer, ObjectAttributes)
    # or COPY ON WRITE through the class's `_InternalDuplicate` (File3dmObject's
    # geometry). Anything else (a holder parent, no duplicate) gets no view.
    # The dict is kept under 'proxy': {Resolver, NonConst: resolve|cow, Dup}.
    proxies = {c['cs']: (TYPE_RECS.get(c['type']) or {}).get('FileProxy') for c in CLASSES}
    def chain_fact(t, key):
        rec = TYPE_RECS.get(t) or {}
        if rec.get(key):
            return rec[key]
        for bt, _ in base_chain(t):
            if (TYPE_RECS.get(bt) or {}).get(key):
                return TYPE_RECS[bt][key]
        return None
    WRAPPER.clear()
    for rec in TYPE_RECS.values():
        if rec.get('Wrapper'):
            WRAPPER.update(rec['Wrapper'])
    EXMAP.clear(); EXMAP.update(EXMAP_BASE); EXMSG.clear()
    excs = {t.replace('+', '.').split('.')[-1]: r['Exception'] for t, r in TYPE_RECS.items() if r.get('Exception')}
    grew = True
    while grew:
        grew = False
        for n, e in excs.items():
            if n not in EXMAP and e['Base'] in EXMAP:
                EXMAP[n] = EXMAP[e['Base']]
                grew = True
    for n, e in excs.items():
        if n in EXMAP and e.get('Message'):
            EXMSG[n] = e['Message']
    def fix_messages(v):
        # every `throw new X()` (a guard, a fail-throw, an enum map's default):
        # no message of its own -> the one X's constructor fixes
        if isinstance(v, dict):
            if v.get('Exception') in EXMSG and not v.get('Message'):
                v['Message'] = EXMSG[v['Exception']]
            for w in v.values():
                fix_messages(w)
        elif isinstance(v, list):
            for w in v:
                fix_messages(w)
    for x in manifest['members']:
        if x['Kind'] not in ('type', 'enum'):
            fix_messages(x)
    for c in CLASSES:
        c.pop('proxy', None); c.pop('ff', None)
        base = BY_REG.get(c.get('base', ''))
        if c['mode'] != 'pointer' or (base is not None and base['mode'] != 'abstract'):
            continue                       # a concrete-base derived class shares its base's storage
        dup = chain_fact(c['type'], 'Duplicate')
        ff = (TYPE_RECS.get(c['type']) or {}).get('FreeFloating')
        if ff and ff.get('New') and ff.get('Delete'):
            # DocumentOrFreeFloatingBase: owns through its own New/Delete; backed by
            # a file it is a write-through view (CppPointer -> CppFromFile3dm)
            c['ff'] = ff
            if ff.get('FromFile'):
                c['proxy'] = {'Resolver': ff['FromFile'], 'NonConst': 'resolve', 'Dup': None, 'Origin': 'file', 'NoId': True}
            continue
        fp = (TYPE_RECS.get(c['type']) or {}).get('FileProxy')
        if fp:
            if fp['NonConst'] == 'resolve':
                c['proxy'] = {'Resolver': fp['Resolver'], 'NonConst': 'resolve', 'Dup': None, 'Origin': 'file'}
            elif fp['NonConst'] == 'base' and dup:
                c['proxy'] = {'Resolver': fp['Resolver'], 'NonConst': 'cow', 'Dup': dup, 'Origin': 'file'}
            elif fp['NonConst'] == 'base' and c.get('proxy_nonconst') == 'none':
                # nothing to copy on write with (no Duplicate): writes refused (G87)
                c['proxy'] = {'Resolver': fp['Resolver'], 'NonConst': 'none', 'Dup': None, 'Origin': 'file'}
            if c.get('proxy') and fp.get('ChildField'):
                # a child proxy (G89): `(Guid id, <type> parent)` stores the parent where the file
                # goes; the generated class holds a light copy of it (Parent / TopLevel)
                c['proxy']['Child'] = {'Field': fp['ChildField'], 'Type': fp['ChildType']}
            if c.get('proxy') and fp.get('IdField') is None:
                # a view of the file itself (RenderSettings): no id, and a write
                # back (`Commit()`) when the type's commit targets the same file field
                c['proxy']['NoId'] = True
                cm = (TYPE_RECS.get(c['type']) or {}).get('Commit')
                if cm and cm.get('FileField') == fp.get('FileField'):
                    c['proxy']['Commit'] = cm['Native']
            continue
        # every ParentViews entry along the chain (Curve has its own CurveHolder
        # entry; the File3dmObject one is GeometryBase's)
        all_pvs = [pv for t in [c['type']] + [bt for bt, _ in base_chain(c['type'])]
                   for pv in (TYPE_RECS.get(t) or {}).get('ParentViews') or []]
        # `new Texture(index, material)` (G57): a view of (owner object, index) the
        # type's own _InternalGetConstPointer resolves with one native call; no
        # NonConstPointer branch, so writes copy (NonConstOperation)
        own_pvs = [pv for pv in (TYPE_RECS.get(c['type']) or {}).get('ParentViews') or [] if pv.get('IndexNative')]
        if own_pvs and dup:
            pv = own_pvs[0]
            owner = BY_CS.get(pv['ParentType'])
            fn = C_BY_NAME.get(pv['IndexNative'])
            if (len(own_pvs) == 1 and owner and owner['mode'] == 'pointer' and fn and fn.get('variant') != 'rhino-only'
                    and len(fn['params']) == 2 and fn['params'][1]['type'] == 'int'):
                c['proxy'] = {'Resolver': pv['IndexNative'], 'NonConst': 'cow', 'Dup': dup, 'Origin': 'parent',
                              'Parent': pv['ParentType'], 'Owner': owner['reg'], 'Field': pv['IndexField']}
                continue
        # `new ViewportInfo(viewInfo)` (G89): a view of the owner's sub-object -- the type's
        # const resolver is one native on the owner's own const pointer (ViewInfo's
        # ConstViewportPointer), no NonConstPointer branch, so writes copy
        own_sub = [pv for pv in (TYPE_RECS.get(c['type']) or {}).get('ParentViews') or []
                   if not pv.get('IndexNative') and pv.get('ConstNative') and not pv.get('NonConst')]
        if own_sub and dup and len(own_sub) == 1:
            pv = own_sub[0]
            owner = BY_CS.get(pv['ParentType'])
            fn = C_BY_NAME.get(pv['ConstNative']['Name'])
            if (owner and owner['mode'] in ('pointer', 'value') and not owner.get('proxy') and fn and fn.get('variant') != 'rhino-only'
                    and len(fn['params']) == 1 and [a.get('Value') for a in pv['ConstNative']['Args']] == ['ConstPointer']
                    and fn['params'][0]['type'].replace('const ', '').rstrip('*').strip() == owner['on']):
                c['proxy'] = {'Resolver': pv['ConstNative']['Name'], 'NonConst': 'cow', 'Dup': dup, 'Origin': 'parent',
                              'Parent': pv['ParentType'], 'Owner': owner['reg'], 'Field': None, 'NoIndex': True}
                continue
        for pv in all_pvs:
            parent = proxies.get(pv['ParentType'])
            if not parent or not pv.get('ConstNative'):
                continue
            def is_file_id(call):      # the parent's (file pointer, id) exactly
                a = call['Args']
                return (len(a) == 2 and a[0]['Kind'] == 'fieldptr' and a[0]['Text'] == parent['FileField']
                        and a[1]['Kind'] == 'expr' and a[1]['Text'] == parent['IdField'])
            if not is_file_id(pv['ConstNative']):
                continue
            nc = pv.get('NonConstNative')
            if nc is not None and is_file_id(nc) and nc['Name'] == pv['ConstNative']['Name']:
                c['proxy'] = {'Resolver': pv['ConstNative']['Name'], 'NonConst': 'resolve', 'Dup': None,
                              'Origin': 'object', 'Parent': pv['ParentType']}
            elif nc is None and dup:
                c['proxy'] = {'Resolver': pv['ConstNative']['Name'], 'NonConst': 'cow', 'Dup': dup,
                              'Origin': 'object', 'Parent': pv['ParentType']}
            break
    index_components()
    index_ctor_keys()
    index_scopes()
    index_child_state()


def index_child_state():
    """a CHILD class whose RhinoCommon type's one constructor only stores its
    parameters (specgen CtorStore; G60: `Decals(ObjectAttributes parent, bool
    read_only, uint rhino_doc_sn)`): the first parameter is the parent (the
    view's pointer), every other one a field the view keeps --
    c['state'] = [(C++ type, field)], set by the property that makes the view
    (planner.child_view: constants only). A parameter of another type, or one
    not stored: no state (the view is refused where it needs it)."""
    for c in CLASSES:
        c.pop('state', None)
        rec = TYPE_RECS.get(c['type']) or {}
        cs = rec.get('CtorStore')
        if c['mode'] != 'child' or c.get('key') or c.get('derive') or not cs or len(cs) < 2:
            continue
        parent = BY_REG.get(c.get('parent', ''))
        if (parent is None or cs_unqualify(cs[0]['Type'] or '') != parent['cs']
                or cs[0]['Field'] not in (rec.get('ParentFields') or [])):
            continue
        if any(q['Field'] is None or q['Type'] not in ('bool', 'int', 'uint', 'Guid') for q in cs[1:]):
            continue
        # a Guid field is kept as opennurbs' ON_UUID (G104: File3dmPlugInData.m_id)
        c['state'] = [('ON_UUID' if q['Type'] == 'Guid' else CS_ARG[q['Type']][0], q['Field']) for q in cs[1:]]


def index_ctor_keys():
    """a child class whose RhinoCommon type has constructors that null fields
    (BrepLoopList(Brep): m_brepface = null) and that its parent reaches through ONE
    of them (`Brep.Loops => new BrepLoopList(this)`): c['ctor_key'] = that
    constructor's key ("(Brep)"). Its members are planned from the bodies specialized
    for it (specgen Specialized). Two accessors reaching it differently: no key."""
    for c in CLASSES:
        c.pop('ctor_key', None)
        rec = TYPE_RECS.get(c['type']) or {}
        if c['mode'] != 'child' or not rec.get('NullingCtors'):
            continue
        parent = BY_REG.get(c.get('parent', ''))
        if parent is None:
            continue
        keys = set()
        for x in PROPS_BY_TYPE.get(parent['type'], []):
            if cs_unqualify(x.get('Child') or '') == c['cs'] and x.get('ChildArgs') == ['this']:
                keys.add('(%s)' % parent['cs'])
        nulling = {n['Key'] for n in rec['NullingCtors']}
        if len(keys) == 1 and keys <= nulling:
            c['ctor_key'] = keys.pop()


def index_scopes():
    """a child class reached through one nulling constructor (index_ctor_keys) whose
    RhinoCommon type also has a SCOPING constructor taking a component of the same
    parent (BrepLoopList(BrepFace): m_brepface = face; specgen ScopingCtors; G77):
    c['scope'] = {key, field, comp}. The generated view keeps `m_scope` (the
    component's key, -1 for the whole parent); members whose bodies read the field
    are planned for both and dispatch on it."""
    for c in CLASSES:
        c.pop('scope', None)
        rec = TYPE_RECS.get(c['type']) or {}
        if c['mode'] != 'child' or not c.get('ctor_key') or not rec.get('ScopingCtors') or c.get('state') or c.get('key'):
            continue
        parent = BY_REG.get(c.get('parent', ''))
        found = []
        for sc in rec['ScopingCtors']:
            comp = BY_CS.get(sc['Param'])
            if comp and comp.get('comp') and comp['comp']['Owner'] is parent and comp['comp'].get('Const') in C_BY_NAME:
                found.append({'key': sc['Key'], 'field': sc['Field'], 'comp': comp})
        if len(found) == 1:
            c['scope'] = found[0]


def storage_host(c):
    """the class holding a registry class's storage: itself, or the concrete
    base it extends (planner.storage_owner, for the registry pass)"""
    while True:
        b = BY_REG.get(c.get('base', ''))
        if b is None or b['mode'] == 'abstract':
            return c
        c = b


def index_components():
    """COMPONENTS (G43): RhinoCommon's `new BrepFace(index, brep)` -- an object
    made of (owner, key) and resolved through the owner on every access
    (specgen Component). A POINTER class whose record carries its own pointer
    resolver is a component (c['comp']); a CHILD class whose record is a
    pointer-less component is a child WITH A KEY (c['key']: PointCloudItem,
    NurbsSurfaceKnotList -- the owner's pointer plus an index or direction).
    A component deriving a concrete class (BrepFace : Surface) keeps that
    class's storage, which then hosts the component state (c['comp_host']).
    Anything else -- an unregistered owner, a resolver missing from the C
    surface, a host that is not view-capable -- grants nothing."""
    for c in CLASSES:
        c.pop('comp', None); c.pop('comp_host', None); c.pop('key', None); c.pop('handle', None); c.pop('borrowed', None)
        c.pop('shared', None)
        sh = (TYPE_RECS.get(c['type']) or {}).get('SharedHandle')
        if (sh and c['mode'] == 'pointer' and all(n in C_BY_NAME and C_BY_NAME[n].get('variant') != 'rhino-only'
                                                for n in (sh['Raw'], sh['Delete']))
                and C_BY_NAME[sh['Raw']]['returns'] == '%s*' % c['on']):
            # a shared_ptr HANDLE (G60: Decal): owns a std::shared_ptr<T>*; its object is Raw(sp)
            c['shared'] = {'Raw': sh['Raw'], 'Delete': sh['Delete'], 'Copy': sh.get('Copy'),
                           'sp': C_BY_NAME[sh['Raw']]['params'][0]['type']}
            c.pop('proxy', None)
            continue
        if ((TYPE_RECS.get(c['type']) or {}).get('Borrowed') or c.get('managed')) and c['mode'] == 'pointer':
            c['borrowed'] = True             # G48: a pointer into another object, never deleted
            c.pop('proxy', None)
        hd = (TYPE_RECS.get(c['type']) or {}).get('Handle')
        if hd and c['mode'] == 'pointer' and hd.get('Delete') in C_BY_NAME \
                and C_BY_NAME[hd['Delete']].get('variant') != 'rhino-only':
            # an IDisposable HANDLE (G46): owns its pointer, frees it with its Delete
            c['handle'] = {'Delete': hd['Delete'], 'Field': hd['Field']}
            c.pop('proxy', None)
    for c in CLASSES:
        rec = (TYPE_RECS.get(c['type']) or {}).get('Component')
        if not rec or rec.get('KeyType') not in CS_ARG:
            continue
        owner = BY_CS.get(rec['Owner'])
        if owner is None:
            continue
        if c['mode'] == 'child':
            if rec.get('Const') is None and c.get('parent') == owner['reg'] and 'ptr' not in rec['Ctor']:
                c['key'] = {'Type': CS_ARG[rec['KeyType']][0], 'Names': rec['KeyNames'],
                            'OwnerNames': rec['OwnerNames'], 'Ctor': rec['Ctor']}
            continue
        if c['mode'] != 'pointer' or not rec.get('Const') or owner['mode'] != 'pointer':
            continue
        if rec['Const'] not in C_BY_NAME or (rec.get('NonConst') and rec['NonConst'] not in C_BY_NAME):
            continue
        host = storage_host(c)
        if host is not c and (not host.get('proxy') or rec['KeyType'] != 'int'):
            continue
        # a Guid key (G98: PostEffectData) is stored as the native's ON_UUID
        c['comp'] = {'Owner': owner, 'Const': rec['Const'], 'NonConst': rec.get('NonConst'),
                     'Key': 'ON_UUID' if rec['KeyType'] == 'Guid' else CS_ARG[rec['KeyType']][0], 'KeyNames': rec['KeyNames'],
                     'OwnerNames': rec['OwnerNames'], 'Ctor': rec['Ctor']}
        if host is c:
            c.pop('proxy', None)          # a component is never a File3dm view
        else:
            host['comp_host'] = True
    # views INSIDE another object (G58: PolyCurve.SegmentCurve, BrepFace.GetMesh)
    # use the same (fn, owner, key) state: every geometry view class hosts it
    for c in CLASSES:
        if c['mode'] == 'pointer' and storage_host(c) is c and (c.get('proxy') or {}).get('Origin') == 'object':
            c['comp_host'] = True


def resolve_type(name, near):
    """a base named `File3dmCommonComponentTable` (or `DocObjects.Linetype`)
    from type `near`: the unique full type of that simple name, preferring
    near's namespace"""
    simple = cs_unqualify(name).split('.')[-1]
    cands = SIMPLE_TYPES.get(simple, [])
    if len(cands) == 1:
        return cands[0]
    ns = near.rsplit('.', 1)[0]
    same = [c for c in cands if c.rsplit('.', 1)[0] == ns]
    return same[0] if len(same) == 1 else None


def base_chain(full_type):
    """[(base full type, {type param: argument})], nearest first, through
    every base CLASS (interfaces have no type record with members we use)"""
    out, todo, seen = [], [(full_type, {})], set()
    while todo:
        t, binding = todo.pop(0)
        rec = TYPE_RECS.get(t)
        if rec is None or t in seen:
            continue
        seen.add(t)
        for i, b in enumerate(rec.get('Bases') or []):
            bt = resolve_type(b, t)
            if bt is None or bt in seen:
                continue
            args = (rec.get('BaseArgs') or [None] * len(rec['Bases']))[i] or []
            params = TYPE_RECS.get(bt, {}).get('TypeParams') or []
            # a base's own arguments may name OUR type parameters (T -> T)
            b2 = {p: cs_unqualify(binding.get(a, a)) for p, a in zip(params, args)}
            out.append((bt, b2))
            todo.append((bt, b2))
    return out


def class_const(full_type, name):
    """the constant a property of this type (or its nearest declaring base)
    returns: `ComponentType` on File3dmLayerTable -> ('ModelComponentType.Layer', '5')"""
    if (full_type, name) in CONST_PROPS:
        return CONST_PROPS[(full_type, name)]
    for bt, _ in base_chain(full_type):
        if (bt, name) in CONST_PROPS:
            return CONST_PROPS[(bt, name)]
    return None


def view_copyable(c):
    """a class whose File3dm views can be copied as views (G89: RH3DM_ViewCopy): it holds
    the view state itself and has no parent link to carry"""
    pr = c.get('proxy') or {}
    return (c['mode'] == 'pointer' and pr.get('Origin') in ('file', 'object') and not pr.get('Child')
            and not c.get('comp') and BY_REG.get(c.get('base', ''), {}).get('mode') in (None, 'abstract'))


# Hand-written bindings the generated surface calls into (G90): rhino3dm-only API kept
# by hand (api/exceptions.toml [[hand_kept]]) that has no RhinoCommon body to
# generate from. gen_init calls each `init` after every generated class exists (its
# classes may derive from generated ones), and aliases `for...of` on its tables.
HAND_BINDINGS = [
    {'init': 'initBitmapXBindings', 'header': 'bnd_bitmapx.h', 'iter_tables': ['File3dmBitmapTableX'],
     'classes': ['BitmapX', 'File3dmBitmapTableX', 'File3dmBitmapTableXIter']},
]
# members a generated class registers from hand-written free functions taking it
# (`const BND_<reg>&`): Python `keep_alive<0, 1>` (the result refers to the object)
HAND_MEMBERS = {
    'File3dmX': [{'name': 'Bitmaps', 'kind': 'property', 'fn': 'RH3DM_Hand_File3dmX_Bitmaps', 'header': 'bnd_bitmapx.h'}],
}
# a hand path BEHIND a generated member (G119): where RhinoCommon's body gives up with
# `throw new NotSupportedException(..)`, the generated body calls the hand function instead.
# File3dmObjectTable.Add(GeometryBase, ObjectAttributes): RhinoCommon adds only linear/angular
# dimensions among annotations (a Leader, TextEntity, radial or ordinate dimension -- and
# anything else its switch lacks -- throws); rhino3dm 8.x's Add took any geometry. Luis
# 2026-10-28: keep that hand path; the RhinoCommon fix is upstream U30 (D71).
HAND_FALLBACKS = {
    'File3dmObjectTableX': [{'member': 'Add', 'params': ['GeometryBase', 'ObjectAttributes'],
                             'throws': 'NotSupportedException', 'header': 'bnd_objecttablex.h',
                             'call': 'RH3DM_Hand_File3dmObjectTable_AddAny(m_ptr, item, attributes)'}],
}
# a hand class standing in for a RhinoCommon class nothing generates (G91): RhinoCommon's
# BitmapEntry has no working member outside Rhino (pilot #37), so where a body constructs
# one (`new BitmapEntry(index, file)`: ManifestTable.Instantiate) the kept BitmapX is made
# from the same arguments. A deviation, recorded as D44.
HAND_STANDINS = {
    # the index RhinoCommon passes is the manifest's, always unset for an image (images carry
    # no model index: ON_ModelComponent::IndexRequired(Image) is false) -- so the stand-in is
    # made from Instantiate's `id` instead: (file, index) -> (file, id)
    'BitmapEntry': {'gen': 'BND_BitmapX', 'args': ['int'], 'header': 'bnd_bitmapx.h', 'rebind': ('index', 'id')},
}
