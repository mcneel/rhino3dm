using Rhino.Runtime.InteropWrappers;
using System;
using System.Collections.Generic;

#if RHINO_SDK
namespace Rhino.Geometry
{
    /// <summary>
    /// Extrudes a mesh and provides preview
    /// </summary>
    public class MeshExtruder : IDisposable
    {
        IntPtr m_ptr; // IRhinoMeshExtruder
        INTERNAL_ComponentIndexArray ComponentIndices { get; set; }

        /// <summary>
        /// Construct object to extrude given mesh faces, edges and ngons.
        /// </summary>
        /// <param name="inputMesh">Mesh to use as starting point. Will not be modified.</param>
        /// <param name="componentIndices">Mesh faces, edges and ngons to extrude.</param>
        public MeshExtruder(Mesh inputMesh, IEnumerable<ComponentIndex> componentIndices)
        {
            ComponentIndices = new INTERNAL_ComponentIndexArray();
            foreach (var ci in componentIndices)
            {
                ComponentIndices.Add(ci);
            }

            m_ptr = UnsafeNativeMethods.RHC_RhinoMeshExtruder_New(inputMesh.ConstPointer(), ComponentIndices.NonConstPointer());
        }
        /// <summary>
        /// Destructor
        /// </summary>
        ~MeshExtruder()
        {
            Dispose(false);
        }
        /// <summary>
        /// Dispose of this object and any unmanaged memory associated with it.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        private void Dispose(bool bDisposing)
        {
            if (m_ptr != IntPtr.Zero)
            {
                UnsafeNativeMethods.RHC_RhinoMeshExtruder_Delete(m_ptr);
                m_ptr = IntPtr.Zero;
            }
        }

        /// <summary>
        /// Gets Line objects to preview extruded mesh.
        /// </summary>
        public Line[] PreviewLines
        {
            get
            {
                Line[] ret = new Line[0];
                using (SimpleArrayLine pl = new SimpleArrayLine())
                {
                    UnsafeNativeMethods.RHC_RhinoMeshExtruder_PreviewLines(m_ptr, pl.NonConstPointer());
                    ret = pl.ToArray();
                }
                return ret;
            }
        }

        /// <summary>
        /// Creates new extruded mesh. Returns true if any edges or faces were extruded.
        /// </summary>
        /// <param name="extrudedMeshOut">Extruded mesh</param>
        public bool ExtrudedMesh(out Mesh extrudedMeshOut)
        {
            extrudedMeshOut = new Mesh();
            return UnsafeNativeMethods.RHC_RhinoMeshExtruder_ExtrudedMesh(m_ptr, extrudedMeshOut.NonConstPointer());
        }

        /// <summary>
        /// Creates new extruded mesh. Returns true if any edges or faces were extruded.
        /// </summary>
        /// <param name="extrudedMeshOut">Extruded mesh</param>
        /// <param name="componentIndicesOut">Component indices of extruded faces and vertices</param>
        public bool ExtrudedMesh(out Mesh extrudedMeshOut, out List<ComponentIndex> componentIndicesOut)
        {
          extrudedMeshOut = new Mesh();
          var internalComponentIndicesOut = new INTERNAL_ComponentIndexArray();
          bool ret = UnsafeNativeMethods.RHC_RhinoMeshExtruder_ExtrudedMeshComponentsOut(m_ptr, extrudedMeshOut.NonConstPointer(), internalComponentIndicesOut.NonConstPointer());
          componentIndicesOut = new List<ComponentIndex>();
          if (ret)
          {
            for (int i = 0; i < internalComponentIndicesOut.Count; i++)
            {
              var ci = internalComponentIndicesOut.ToArray()[i];
              componentIndicesOut.Add(new ComponentIndex(ci.ComponentIndexType, ci.Index));
            }
          }
          return ret;
        }

        /// <summary>
        /// Return list of faces that were added to connect transformed edges/faces to non-transformed edges/faces.
        /// </summary>
        /// <returns>List of wall faces</returns>
        public List<int> GetWallFaces()
        {
          List<int> res = new List<int>();
          using (var wallFaces = new SimpleArrayInt())
          {
            IntPtr ptr_walFaces = wallFaces.NonConstPointer();
            UnsafeNativeMethods.RHC_RhinoMeshExtruder_GetWallFaces(m_ptr, ptr_walFaces);
            res.AddRange(wallFaces.ToArray());
          }
          return res;
        }

        /// <summary>
        /// Transform of extrusion
        /// </summary>
        public Transform Transform
        {
            get
            {
                var transform = new Transform();
                UnsafeNativeMethods.RHC_RhinoMeshExtruder_GetTransform(m_ptr, ref transform);
                return transform;
            }
            set
            {
                UnsafeNativeMethods.RHC_RhinoMeshExtruder_SetTransform(m_ptr, ref value);
            }
        }

        /// <summary>
        /// Whether or not to perform extrude in UVN basis.
        /// </summary>
        public bool UVN
        {
            get
            {
                return UnsafeNativeMethods.RHC_RhinoMeshExtruder_GetUVN(m_ptr);
            }
            set
            {
                UnsafeNativeMethods.RHC_RhinoMeshExtruder_SetUVN(m_ptr, value);
            }
        }

        /// <summary>
        /// Edge based UVN defines UVN directions according to boundary edge directions
        /// </summary>
        public bool EdgeBasedUVN
        {
            get
            {
                return UnsafeNativeMethods.RHC_RhinoMeshExtruder_GetEdgeBasedUVN(m_ptr);
            }
            set
            {
                UnsafeNativeMethods.RHC_RhinoMeshExtruder_SetEdgeBasedUVN(m_ptr, value);
            }
        }

        /// <summary>
        /// Whether or not to keep original faces.
        /// </summary>
        public bool KeepOriginalFaces
        {
            get
            {
                return UnsafeNativeMethods.RHC_RhinoMeshExtruder_GetKeepOriginalFaces(m_ptr);
            }
            set
            {
                UnsafeNativeMethods.RHC_RhinoMeshExtruder_SetKeepOriginalFaces(m_ptr, value);
            }
        }

        /// <summary>
        /// Mode for creating texture coordinates for extruded areas
        /// </summary>
        public MeshExtruderParameterMode TextureCoordinateMode
        {
            get
            {
                return UnsafeNativeMethods.RHC_RhinoMeshExtruder_GetTextureCoordinateMode(m_ptr);
            }
            set
            {
                UnsafeNativeMethods.RHC_RhinoMeshExtruder_SetTextureCoordinateMode(m_ptr, value);
            }
        }

        /// <summary>
        /// Mode for creating surface parameters for extruded areas
        /// </summary>
        public MeshExtruderParameterMode SurfaceParameterMode
        {
            get
            {
                return UnsafeNativeMethods.RHC_RhinoMeshExtruder_GetSurfaceParameterMode(m_ptr);
            }
            set
            {
                UnsafeNativeMethods.RHC_RhinoMeshExtruder_SetSurfaceParameterMode(m_ptr, value);
            }
        }

        /// <summary>
        /// Face direction mode determines how faces are oriented
        /// </summary>
        public MeshExtruderFaceDirectionMode FaceDirectionMode
        {
            get
            {
                return UnsafeNativeMethods.RHC_RhinoMeshExtruder_GetFaceDirectionMode(m_ptr);
            }
            set
            {
                UnsafeNativeMethods.RHC_RhinoMeshExtruder_SetFaceDirectionMode(m_ptr, value);
            }
        }
    }
}
#endif