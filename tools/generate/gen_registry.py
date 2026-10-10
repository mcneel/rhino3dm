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
    {'type': 'Rhino.Geometry.GeometryBase', 'cs': 'GeometryBase',
     'on': 'ON_Geometry',
     'mode': 'abstract', 'getter': 'RH3DM_GeomPtr',
     'self_extra': ['ON_Object'],
     'gen': 'BND_GeometryBaseX', 'reg': 'GeometryBaseX'},
    {'type': 'Rhino.DocObjects.ModelComponent', 'cs': 'ModelComponent',
     'on': 'ON_ModelComponent',
     'mode': 'abstract', 'getter': 'RH3DM_McPtr',
     'self_extra': ['ON_Object'],
     'gen': 'BND_ModelComponentX', 'reg': 'ModelComponentX'},
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
    {'type': 'Rhino.DocObjects.ViewportInfo', 'cs': 'ViewportInfo',
     'on': 'ON_Viewport',
     'mode': 'pointer', 'hand': 'BND_Viewport', 'hand_header': 'bnd_viewport.h',
     'copy': 'source.m_viewport ? new ON_Viewport(*source.m_viewport) : nullptr',
     'gen': 'BND_ViewportX', 'reg': 'ViewportX', 'bridge': False},
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
    {'type': 'Rhino.DocObjects.Font', 'cs': 'Font', 'on': 'ON_Font',
     'mode': 'pointer', 'hand': 'BND_Font', 'hand_header': 'bnd_font.h',
     'copy': ('source.m_managed_font'
              ' ? new ON_Font(*source.m_managed_font) : nullptr'),
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
     'gen': 'BND_ObjectAttributesX', 'reg': 'ObjectAttributesX'},
    {'type': 'Rhino.Geometry.MeshingParameters', 'cs': 'MeshingParameters',
     'on': 'ON_MeshParameters',
     'mode': 'value', 'hand': 'BND_MeshingParameters', 'hand_header': 'bnd_mesh.h',
     'copy': 'source.ONMeshParameters()',
     'gen': 'BND_MeshingParametersX', 'reg': 'MeshingParametersX'},
    {'type': 'Rhino.DocObjects.Texture', 'cs': 'Texture', 'on': 'ON_Texture',
     'mode': 'pointer', 'hand': 'BND_Texture', 'hand_header': 'bnd_texture.h',
     'copy': 'source.m_texture ? new ON_Texture(*source.m_texture) : nullptr',
     'gen': 'BND_TextureX', 'reg': 'TextureX'},
    {'type': 'Rhino.Render.TextureMapping', 'cs': 'TextureMapping', 'on': 'ON_TextureMapping',
     'mode': 'pointer', 'hand': 'BND_TextureMapping', 'hand_header': 'bnd_texture_mapping.h',
     'copy': 'source.m_mapping ? new ON_TextureMapping(*source.m_mapping) : nullptr',
     'gen': 'BND_TextureMappingX', 'reg': 'TextureMappingX'},
    {'type': 'Rhino.DocObjects.EarthAnchorPoint', 'cs': 'EarthAnchorPoint',
     'on': 'ON_EarthAnchorPoint',
     'mode': 'value', 'hand': 'BND_EarthAnchorPoint', 'hand_header': 'bnd_3dm_settings.h',
     'copy': 'source.m_anchor_point',
     'gen': 'BND_EarthAnchorPointX', 'reg': 'EarthAnchorPointX'},
    {'type': 'Rhino.DocObjects.ViewInfo', 'cs': 'ViewInfo', 'on': 'ON_3dmView',
     'mode': 'value', 'hand': 'BND_ViewInfo', 'hand_header': 'bnd_3dm_settings.h',
     'copy': 'source.m_view',
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
    {'type': 'Rhino.Geometry.AnnotationBase', 'cs': 'AnnotationBase', 'on': 'ON_Annotation',
     'mode': 'pointer', 'hand': 'BND_AnnotationBase', 'hand_header': 'bnd_annotationbase.h',
     'copy': ('ON_Annotation::Cast(source.GeometryPointer() ? source.GeometryPointer()->Duplicate() : nullptr)'),
     'gen': 'BND_AnnotationBaseX', 'reg': 'AnnotationBaseX', 'base': 'GeometryBaseX',
     'delete_as_object': True},
    # -- static classes: no instances, static members only
    {'type': 'Rhino.Geometry.Intersect.Intersection', 'cs': 'Intersection', 'on': None,
     'mode': 'static', 'hand': None, 'bridge': False,
     'gen': 'BND_IntersectionX', 'reg': 'IntersectionX'},
    # -- File3dm: an owning ONX_Model; its tables are views onto it
    {'type': 'Rhino.FileIO.File3dm', 'cs': 'File3dm', 'on': 'ONX_Model',
     'mode': 'pointer', 'hand': None, 'bridge': False,
     'gen': 'BND_File3dmX', 'reg': 'File3dmX'},
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
    # -- collections: views onto a parent object (mode 'child')
    {'type': 'Rhino.Geometry.Collections.MeshVertexList', 'cs': 'MeshVertexList', 'on': 'ON_Mesh',
     'mode': 'child', 'parent': 'MeshX', 'hand': None, 'bridge': False,
     'gen': 'BND_MeshVertexListX', 'reg': 'MeshVertexListX'},
    {'type': 'Rhino.Geometry.Collections.MeshFaceList', 'cs': 'MeshFaceList', 'on': 'ON_Mesh',
     'mode': 'child', 'parent': 'MeshX', 'hand': None, 'bridge': False,
     'gen': 'BND_MeshFaceListX', 'reg': 'MeshFaceListX'},
    {'type': 'Rhino.Geometry.Collections.MeshVertexNormalList', 'cs': 'MeshVertexNormalList', 'on': 'ON_Mesh',
     'mode': 'child', 'parent': 'MeshX', 'hand': None, 'bridge': False,
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
]
BY_CS = {c['cs']: c for c in CLASSES}
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
# a scratch out local the C# body declares itself (`Plane plane; ...
# IsPlanar(ptr, true, ref plane, tol)`) is typed by its C pointer type
C_VT = {'ON_3dPoint*': 'Point3d', 'ON_3DPOINT_STRUCT*': 'Point3d',
        'ON_3dVector*': 'Vector3d', 'ON_3DVECTOR_STRUCT*': 'Vector3d',
        'ON_Interval*': 'Interval', 'ON_INTERVAL_STRUCT*': 'Interval',
        'ON_PLANE_STRUCT*': 'Plane', 'ON_Xform*': 'Transform', 'ON_Line*': 'Line', 'ON_BoundingBox*': 'BoundingBox',
        'ON_Arc*': 'Arc', 'ON_Circle*': 'Circle', 'ON_Sphere*': 'Sphere',
        'ON_2DPOINT_STRUCT*': 'Point2d', 'ON_2dVector*': 'Vector2d', 'ON_2DVECTOR_STRUCT*': 'Vector2d'}
VT_JS_KEY = {}       # generated struct -> JS shape-test property


def field_backed(vt, cs):
    """{field: property} for properties that ARE a field: the translated
    getter is `return <field>;` (and a setter, if any, assigns it). Decided
    from the bodies, not the names -- Transform's M00 is field m_00."""
    out = {}
    fields = {f['Name'] for t in vt['types'] if t['Cs'] == cs for f in t['Fields']}
    for m in vt['members']:
        if m['Type'] != cs or m['Kind'] != 'property' or m.get('Static') or not m.get('Ok', True):
            continue
        g = re.search(r'::%s\(\)\n\{\n  return (\w+);\n\}' % re.escape(m['Name']), m.get('Def') or '')
        if g and g.group(1) in fields and g.group(1) not in out:
            out[g.group(1)] = m['Name']
    return out


def register_value_types(vt):
    for m in vt['members']:
        if m.get('Ok') and m.get('Static') and m['Kind'] in ('property', 'static-field'):
            VT_STATICS.add((m['Type'], m['Name']))
        if m.get('Ok') and m['Kind'] == 'field-const':
            VT_CONSTS.add((m['Type'], m['Name']))
        if m.get('Ok') and m['Kind'] == 'property' and not m.get('Static'):
            VT_PROPS.add((m['Type'], m['Name']))
            VT_PROP_RET[(m['Type'], m['Name'])] = m.get('Ret')
    for e in vt.get('enums', []):
        pub_t = CS_ENUMS.get(e['Cs'])
        if pub_t is None or pub_t['gen']:
            raise SystemExit('translated enum %s: no registered public type (add it to CS_ENUMS)' % e['Cs'])
        VT_ENUMS[e['Cpp']] = pub_t['cpp']
    for t in vt['types']:
        if t.get('IsStaticClass') or not t.get('LayoutSize'):
            continue
        cs, cpp = t['Cs'], t['Cpp']
        VT_CPP[cs] = cpp
        OUTLOCAL.setdefault('VT:' + cs, ('%s {n};' % cpp, '{n}'))
        CS_RET_CPP.setdefault(cs, cpp)
        CS_RET_EXPR.setdefault(cs, '{n}')
        CS_RET_CPP['VT:' + cs] = cpp          # a value-type local, whatever cs's
        CS_RET_EXPR['VT:' + cs] = '{n}'       # explicit opennurbs conversion
        first = (t.get('Fields') or [{}])[0].get('Name', '')
        key = field_backed(vt, cs).get(first)
        if key:
            VT_JS_KEY.setdefault(cpp, key)


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
}

EXMAP = {
    'IndexOutOfRangeException': 'std::out_of_range',
    'ArgumentOutOfRangeException': 'std::out_of_range',
    'ArgumentNullException': 'std::invalid_argument',
    'ArgumentException': 'std::invalid_argument',
    'InvalidOperationException': 'std::runtime_error',
    'NotSupportedException': 'std::runtime_error',
    # -> Python FileNotFoundError (translator registered in gen_init.cpp)
    'FileNotFoundException': 'RH3DM_FileNotFound',
}
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


def extend_enum_table(manifest):
    """every namespace-level portable RhinoCommon enum gets a CS_ENUMS entry:
    the old bindings' C++ type when they register that exact Python name
    (re-registering it would throw), otherwise a generated RH3DM_<Name>
    defined from the manifest's values. Nested enums (DimensionStyle.Field)
    are left out: their short names repeat across types."""
    hand = {}
    for f in glob.glob(os.path.join(ROOT, 'src', 'bindings', '*.cpp')):
        for cpp, name in re.findall(r'py::enum_<([^>]+)>\(m, "(\w+)"', open(f, errors='replace').read()):
            hand.setdefault(name, cpp.strip())
    type_names = {x['Type'] for x in manifest['members'] if x['Kind'] == 'type'}
    seen = {}
    for x in manifest['members']:
        if x['Kind'] != 'enum' or x['Variant'] != 'portable' or x['Type'] in type_names:
            continue
        seen.setdefault(x['Name'], set()).add(x['Type'])
    for name, owners in seen.items():
        if name in CS_ENUMS or len(owners) != 1:
            continue
        CS_ENUMS[name] = ({'cpp': hand[name], 'gen': False} if name in hand
                          else {'cpp': 'RH3DM_' + name, 'gen': True})


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
    Unsupported parameter modifiers (params, this) yield (None, None)."""
    if 'Params' not in member and 'Returns' not in member:
        return None, None
    ret = cs_unqualify(member.get('Returns') or member.get('Type', '').split('.')[-1])
    params = []
    for p in member.get('Params') or []:
        mode = p.get('Mode') or ''
        if mode == 'in':
            mode = ''                      # by-value for our purposes
        if mode not in ('', 'out', 'ref'):
            return None, None
        params.append((mode, cs_unqualify(p['Type']), p['Name']))
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
    mm = re.match(r'^RhinoMath\.(\w+)$', t)
    if mm and ('RhinoMath', mm.group(1)) in VT_CONSTS:
        return 'RH3DM_RhinoMath::' + mm.group(1), None
    mm = re.match(r'^(\w+)\.(\w+)$', t)
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



VT_ENUMS = {}        # RH3DM_<Name> -> the C++ type the bindings register under <Name>
VT_STATICS = set()   # (C# type, name) of translated static getters (Plane.WorldXY)
VT_CONSTS = set()    # (C# type, name) of translated constants (RhinoMath.UnsetValue)
VT_PROPS = set()     # (C# type, name) of translated instance properties (Interval.Min)
VT_PROP_RET = {}     # (C# type, name) -> C++ return type of that property's getter



BRIDGE_STATICS = {}     # (class C# name, static name) -> generated static plans
BRIDGE_CANDIDATES = {}  # value type C# name -> manifest members delegating to a class static
BRIDGE_INCLUDES = {}    # value type C# name -> generated class headers its bindings need
