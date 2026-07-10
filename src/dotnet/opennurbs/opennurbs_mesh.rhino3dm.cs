#if RHINO3DM_BUILD
using System;
using Rhino.Render;

namespace Rhino.Geometry
{
  // rhino3dm-only additions to Mesh. Kept in a separate partial-class file so the
  // sync-managed opennurbs_mesh.cs stays byte-identical to the RhinoCommon source.
  //
  // RhinoCommon exposes SetCachedTextureCoordinatesFromMaterial / GetCachedTextureCoordinates
  // overloads that take a RhinoObject (and therefore a RhinoDoc); those are RHINO_SDK only.
  // rhino3dm has no document, so these variants resolve the object's texture mappings from a
  // File3dm (ONX_Model) instead. They call the same opennurbs methods via the *_ONX_Model
  // native exports. (RH3DM-170)
  //
  // TODO (post-8.32): push these File3dm/ONX_Model overloads UPSTREAM into RhinoCommon's
  // opennurbs_mesh.cs on the rhino repo 8.x branch. Once they live upstream they will sync
  // down like everything else and THIS whole file should be removed. Fine to ship as an
  // extension for 8.32; the upstreaming is the follow-up.
  public partial class Mesh : GeometryBase
  {
    /// <summary>
    /// Sets up a cached texture coordinate set for each texture in the material, using the
    /// texture mappings stored in the given File3dm for the object with the specified id.
    /// After this method is called the correct texture coordinates for each texture can be
    /// fetched using GetCachedTextureCoordinatesFromTexture.
    /// </summary>
    /// <param name="file">File3dm that contains the object's texture mappings.</param>
    /// <param name="objectId">Id of the object in the file that defines the texture mappings.</param>
    /// <param name="material">Material with textures that define mapping channels.</param>
    /// <since>8.17</since>
    public void SetCachedTextureCoordinatesFromMaterial(FileIO.File3dm file, Guid objectId, Rhino.DocObjects.Material material)
    {
      UnsafeNativeMethods.ON_Mesh_SetCachedTextureCoordinatesFromMaterial_ONX_Model(NonConstPointer(), file.ConstPointer(), objectId, material.ConstPointer());
      GC.KeepAlive(file);
      GC.KeepAlive(material);
      GC.KeepAlive(this);
    }

    /// <summary>
    /// Returns the cached texture coordinate set for a texture, using the texture mappings
    /// stored in the given File3dm for the object with the specified id. Make sure to set up
    /// cached texture coordinates first by calling SetCachedTextureCoordinatesFromMaterial.
    /// </summary>
    /// <param name="file">File3dm that contains the object's texture mappings.</param>
    /// <param name="objectId">Id of the object in the file that defines the texture mappings.</param>
    /// <param name="texture">Texture that defines the mapping channel.</param>
    /// <returns>Cached texture coordinates if available and otherwise null.</returns>
    /// <since>8.17</since>
    public CachedTextureCoordinates GetCachedTextureCoordinatesFromTexture(FileIO.File3dm file, Guid objectId, Rhino.DocObjects.Texture texture)
    {
      var tc_pointer = UnsafeNativeMethods.ON_Mesh_GetCachedTextureCoordinates_ONX_Model(NonConstPointer(), file.ConstPointer(), objectId, texture.ConstPointer());
      if (tc_pointer == IntPtr.Zero)
        return null;
      var tc = new CachedTextureCoordinates(tc_pointer);
      GC.KeepAlive(file);
      GC.KeepAlive(texture);
      GC.KeepAlive(this);
      return tc;
    }
  }
}
#endif
