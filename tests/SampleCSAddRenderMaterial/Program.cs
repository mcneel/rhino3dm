
using Rhino.Display;
using Rhino.DocObjects;
using Rhino.FileIO;
using Rhino.Geometry;

// Sample: adding render materials to a File3dm.
//
// rhino3dm has two material-ish types, and only one of them can be created:
//
//   Rhino.DocObjects.Material          -- an ON_Material. Fully writable. This is what you add
//                                         to a File3dm. Since Rhino 7 it carries a physically
//                                         based (PBR) parameter set, which is what Rhino's
//                                         modern render materials are built on.
//
//   Rhino.FileIO.File3dmRenderMaterial -- an ON_RenderContent of kind "material", i.e. the
//                                         document render content that Rhino shows in the
//                                         Materials panel. In rhino3dm this is READ-ONLY:
//                                         File3dm.RenderMaterials has no Add, and
//                                         File3dmRenderMaterial has no public constructor. You
//                                         can enumerate and inspect what a file already has,
//                                         but you cannot author new ones.
//
// So, to add a render material, add a Material (ON_Material) and set its PBR parameters. When
// Rhino opens the file it creates the matching render content -- a "Physically Based" material
// if IsPhysicallyBased is true, otherwise a "Custom" material.
//
// Two things people sometimes miss:
//
//   1. Call ToPhysicallyBased() BEFORE touching the PhysicallyBased property. The property
//      returns null until the material has been converted, so the natural first attempt is a
//      NullReferenceException with no hint as to why.
//
//   2. Call SynchronizeLegacyMaterial() AFTER setting the PBR values. PBR values live alongside
//      the legacy diffuse/gloss/transparency fields, they do not replace them. Without the sync,
//      anything reading the legacy fields (shaded viewport display, pre-V7 Rhino, most
//      third-party 3dm readers) sees the default white plastic.
//
// One more: AddMaterial copies the material into the model, so mutating your local Material
// after the call has no effect on the file. Configure it fully, then add it.

var file3dm = new File3dm();

// --- a metal ------------------------------------------------------------------------------
var brass = new Material { Name = "Brass" };
brass.ToPhysicallyBased();                              // (1) convert first
var brassPbr = brass.PhysicallyBased;
brassPbr.BaseColor = Color4f.FromArgb(1f, 0.83f, 0.69f, 0.22f);
brassPbr.Metallic  = 1.0;
brassPbr.Roughness = 0.25;
brassPbr.SynchronizeLegacyMaterial();                   // (2) then sync
int brassIndex = file3dm.AllMaterials.AddMaterial(brass);

// --- glass --------------------------------------------------------------------------------
// Transparency in PBR is Opacity (1 = opaque, 0 = clear) plus OpacityIOR. It is NOT the legacy
// Material.Transparency field -- SynchronizeLegacyMaterial fills that in for you.
var glass = new Material { Name = "Glass" };
glass.ToPhysicallyBased();
var glassPbr = glass.PhysicallyBased;
glassPbr.BaseColor  = new Color4f(1f, 1f, 1f, 1f);
glassPbr.Opacity    = 0.0;
glassPbr.OpacityIOR = 1.52;                             // soda-lime glass
glassPbr.Roughness  = 0.0;
glassPbr.SynchronizeLegacyMaterial();
int glassIndex = file3dm.AllMaterials.AddMaterial(glass);

// --- a textured material ------------------------------------------------------------------
// PBR_BaseColor and the legacy Diffuse slot are the same slot (both == 1). Use the PBR name in
// PBR materials to keep the intent clear.
var textured = new Material { Name = "Textured" };
textured.ToPhysicallyBased();
var texturedPbr = textured.PhysicallyBased;
texturedPbr.BaseColor = new Color4f(1f, 1f, 1f, 1f);    // white, so the texture is not tinted
texturedPbr.Metallic  = 0.0;
texturedPbr.Roughness = 0.4;
texturedPbr.SetTexture(new Texture { FileName = "wood.png", Enabled = true }, TextureType.PBR_BaseColor);
texturedPbr.SynchronizeLegacyMaterial();
int texturedIndex = file3dm.AllMaterials.AddMaterial(textured);

// A texture is only a path, and it has to resolve on whatever machine opens the file.
// Optional: embed the bitmap to make the 3dm self-contained.
//file3dm.EmbeddedFiles.Add(@"C:\path\to\wood.png");

// --- a plain legacy material --------------------------------------------------------------
// Still perfectly valid -- Rhino turns this into a "Custom" render material. Use it when you
// only need a color and a bit of gloss.
var painted = new Material
{
   Name          = "Painted Steel",
   DiffuseColor  = System.Drawing.Color.Firebrick,
   SpecularColor = System.Drawing.Color.White,
   Shine         = 0.4 * Material.MaxShine,
};
int paintedIndex = file3dm.AllMaterials.AddMaterial(painted);

// --- assigning materials ------------------------------------------------------------------
// Set the index AND the source. MaterialIndex on its own does nothing, because MaterialSource
// defaults to MaterialFromLayer and the index is ignored.
int x = 0;
foreach (int materialIndex in new[] { brassIndex, glassIndex, texturedIndex, paintedIndex })
{
   var attributes = new ObjectAttributes
   {
      MaterialIndex  = materialIndex,
      MaterialSource = ObjectMaterialSource.MaterialFromObject,
   };

   file3dm.Objects.AddSphere(new Sphere(new Point3d(x, 0, 0), 10.0), attributes);
   x += 25;
}

// Or assign to a layer instead, so objects with MaterialFromLayer (the default) pick it up.
// Add copies the layer into the model and does not write an index back into your local
// instance, so read the index off the model-owned layer rather than off the local one.
var layer = new Layer { Name = "Brass Parts", RenderMaterialIndex = brassIndex };
file3dm.AllLayers.Add(layer);
Console.WriteLine("Layer {0} index: {1}", layer.Name, file3dm.AllLayers.FindName(layer.Name, Guid.Empty)?.Index);

var tmpPath = Path.GetTempPath();
tmpPath = Path.Combine(tmpPath, "testRenderMaterial.3dm");

file3dm.Write(tmpPath, 8);
Console.WriteLine(tmpPath);

// --- reading materials back ---------------------------------------------------------------
// Note that a file written by rhino3dm has entries in AllMaterials but NOT in RenderMaterials --
// the render content is created by Rhino when the file is opened and saved from Rhino. Files
// that came out of Rhino 8 have both.
using (var file = File3dm.Read(tmpPath))
{
   Console.WriteLine("ON_Materials (writable):");
   foreach (var material in file.AllMaterials)
   {
      Console.WriteLine("  [{0}] {1}  PBR={2}", material.Index, material.Name, material.IsPhysicallyBased);

      var pbr = material.PhysicallyBased;
      if (pbr != null)
      {
         var c = pbr.BaseColor;
         Console.WriteLine("        base=({0:F2},{1:F2},{2:F2}) metallic={3:F2} roughness={4:F2} opacity={5:F2}",
                           c.R, c.G, c.B, pbr.Metallic, pbr.Roughness, pbr.Opacity);

         var tex = pbr.GetTexture(TextureType.PBR_BaseColor);
         if (tex != null)
         {
            Console.WriteLine("        base-color texture={0}", tex.FileName);
         }
      }
   }

   Console.WriteLine("Render materials (read-only):");
   foreach (var renderMaterial in file.RenderMaterials)
   {
      Console.WriteLine("  {0}  type={1}", renderMaterial.Name, renderMaterial.TypeName);

      // Parameters are looked up by the render content's own parameter names, which differ from
      // the ON_Material property names. GetParameter returns null if not found.
      var color = renderMaterial.GetParameter("base-color") ?? renderMaterial.GetParameter("diffuse");
      if (color != null)
      {
         Console.WriteLine("        base-color={0}", color);
      }

      // A flattened ON_Material approximation of the render content -- handy when you just want
      // colors and textures out of an arbitrary render material, including ones from renderers
      // you know nothing about.
      using (var simulated = renderMaterial.ToMaterial())
      {
         Console.WriteLine("        simulated diffuse={0}", simulated.DiffuseColor);
      }

      // Textures and other nested content hang off the material as children.
      foreach (var child in renderMaterial.Children)
      {
         Console.WriteLine("        child slot={0} kind={1}", child.ChildSlotName, child.Kind);
      }
   }

   var o = new File3dmWriteOptions();
   file3dm.Write("D:/Test.3dm", o);
}
