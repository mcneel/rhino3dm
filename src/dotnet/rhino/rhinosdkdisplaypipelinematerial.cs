#pragma warning disable 1591
using System;
using System.Drawing;

#if RHINO_SDK
namespace Rhino.Display
{
  public class DisplayMaterial : IDisposable
  {
    #region fields
    private IntPtr m_ptr;
    internal IntPtr ConstPointer() { return m_ptr; }
    internal IntPtr NonConstPointer()
    {
      if(OneShotNonConstCallback != null )
      {
        OneShotNonConstCallback(this, EventArgs.Empty);
        OneShotNonConstCallback = null; // this is a one shot event for cache flushing
      }
      return m_ptr;
    }

    // Used for mesh display cache. Kept internal since it is very specific
    // and not designed for general use
    internal EventHandler OneShotNonConstCallback { get; set; }
    #endregion

    #region constructors
    /// <summary>
    /// Constructs a default material.
    /// </summary>
    /// <since>5.0</since>
    public DisplayMaterial()
    {
      m_ptr = UnsafeNativeMethods.CDisplayPipelineMaterial_New(IntPtr.Zero);
    }
    /// <summary>
    /// Duplicate another material.
    /// </summary>
    /// <since>5.0</since>
    public DisplayMaterial(DisplayMaterial other)
    {
      IntPtr ptr = other.ConstPointer();
      m_ptr = UnsafeNativeMethods.CDisplayPipelineMaterial_New(ptr);
      GC.KeepAlive(other);
    }

    /// <since>5.0</since>
    public DisplayMaterial(DocObjects.Material material)
    {
      IntPtr pConstMaterial = material.ConstPointer();
      m_ptr = UnsafeNativeMethods.CDisplayPipelineMaterial_New4(pConstMaterial);
      GC.KeepAlive(material);
    }

    /// <summary>
    /// Constructs a default material with a specific diffuse color.
    /// </summary>
    /// <param name="diffuse">Diffuse color of material. The alpha component of the Diffuse color is ignored.</param>
    /// <since>5.0</since>
    public DisplayMaterial(Color diffuse)
    {
      int argb = StripAlpha(diffuse.ToArgb());
      m_ptr = UnsafeNativeMethods.CDisplayPipelineMaterial_New1(argb);
    }
    /// <summary>
    /// Constructs a default material with a specific diffuse color and transparency.
    /// </summary>
    /// <param name="diffuse">Diffuse color of material. The alpha component of the Diffuse color is ignored.</param>
    /// <param name="transparency">Transparency factor (0.0 = opaque, 1.0 = transparent)</param>
    /// <since>5.0</since>
    public DisplayMaterial(Color diffuse, double transparency)
    {
      int argb = StripAlpha(diffuse.ToArgb());
      m_ptr = UnsafeNativeMethods.CDisplayPipelineMaterial_New2(argb, transparency);
    }
    /// <summary>
    /// Constructs a material with custom properties.
    /// </summary>
    /// <param name="diffuse">Diffuse color of material. The alpha component of the Diffuse color is ignored.</param>
    /// <param name="specular">Specular color of material. The alpha component of the Specular color is ignored.</param>
    /// <param name="ambient">Ambient color of material. The alpha component of the Ambient color is ignored.</param>
    /// <param name="emission">Emission color of material. The alpha component of the Emission color is ignored.</param>
    /// <param name="shine">Shine (highlight size) of material.</param>
    /// <param name="transparency">Transparency of material (0.0 = opaque, 1.0 = transparent)</param>
    /// <since>5.0</since>
    public DisplayMaterial(Color diffuse, Color specular, Color ambient, Color emission, double shine, double transparency)
    {
      int argbDiffuse = StripAlpha(diffuse.ToArgb());
      int argbSpec = StripAlpha(specular.ToArgb());
      int argbAmbient = StripAlpha(ambient.ToArgb());
      int argbEmission = StripAlpha(emission.ToArgb());

      m_ptr = UnsafeNativeMethods.CDisplayPipelineMaterial_New3(argbDiffuse, argbSpec, argbAmbient, argbEmission, shine, transparency);
    }

    ~DisplayMaterial()
    {
      Dispose(false);
    }

    /// <since>5.0</since>
    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }
    protected virtual void Dispose(bool disposing)
    {
      if (IntPtr.Zero != m_ptr)
      {
        UnsafeNativeMethods.CDisplayPipelineMaterial_Delete(m_ptr);
        m_ptr = IntPtr.Zero;
      }
    }
    #endregion

    #region properties
    /// <summary>
    /// Gets or sets the Diffuse color of the Material. 
    /// The alpha component of the color will be ignored.
    /// </summary>
    /// <since>5.0</since>
    public Color Diffuse
    {
      get { return GetColor(idxDiffuse); }
      set { SetColor(idxDiffuse, value); }
    }

    /// <summary>
    /// Gets or sets the Diffuse color of the back side of the Material. 
    /// The alpha component of the color will be ignored.
    /// </summary>
    /// <since>5.0</since>
    public Color BackDiffuse
    {
      get { return GetColor(idxBackDiffuse); }
      set { SetColor(idxBackDiffuse, value); }
    }

    /// <summary>
    /// Gets or sets the Specular color of the Material. 
    /// The alpha component of the color will be ignored.
    /// </summary>
    /// <since>5.0</since>
    public Color Specular
    {
      get { return GetColor(idxSpecular); }
      set { SetColor(idxSpecular, value); }
    }

    /// <summary>
    /// Gets or sets the Specular color of the back side of the Material. 
    /// The alpha component of the color will be ignored.
    /// </summary>
    /// <since>5.0</since>
    public Color BackSpecular
    {
      get { return GetColor(idxBackSpecular); }
      set { SetColor(idxBackSpecular, value); }
    }

    /// <summary>
    /// Gets or sets the Ambient color of the Material. 
    /// The alpha component of the color will be ignored.
    /// </summary>
    /// <since>5.0</since>
    [Obsolete("This property is obsolete: ambient is no longer supported"),
     System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public Color Ambient
    {
      get { return GetColor(idxAmbient); }
      set { SetColor(idxAmbient, value); }
    }
    /// <summary>
    /// Gets or sets the Ambient color of the back side of the Material. 
    /// The alpha component of the color will be ignored.
    /// </summary>
    /// <since>5.0</since>
    [Obsolete("This property is obsolete: ambient is no longer supported"),
     System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public Color BackAmbient
    {
      get { return GetColor(idxBackAmbient); }
      set { SetColor(idxBackAmbient, value); }
    }

    /// <summary>
    /// Gets or sets the Emissive color of the Material. 
    /// The alpha component of the color will be ignored.
    /// </summary>
    /// <since>5.0</since>
    public Color Emission
    {
      get { return GetColor(idxEmission); }
      set { SetColor(idxEmission, value); }
    }
    /// <summary>
    /// Gets or sets the Emissive color of the back side of the Material. 
    /// The alpha component of the color will be ignored.
    /// </summary>
    /// <since>5.0</since>
    public Color BackEmission
    {
      get { return GetColor(idxBackEmission); }
      set { SetColor(idxBackEmission, value); }
    }

    /// <summary>
    /// Gets or sets the shine factor of the material (0.0 to 1.0)
    /// </summary>
    /// <since>5.0</since>
    public double Shine
    {
      get { return GetDouble(idxShine); }
      set { SetDouble(idxShine, value); }
    }
    /// <summary>
    /// Gets or sets the shine factor of the back side of the material (0.0 to 1.0)
    /// </summary>
    /// <since>5.0</since>
    public double BackShine
    {
      get { return GetDouble(idxBackShine); }
      set { SetDouble(idxBackShine, value); }
    }

    /// <summary>
    /// Gets or sets the transparency of the material (0.0 = opaque to 1.0 = transparent)
    /// </summary>
    /// <since>5.0</since>
    public double Transparency
    {
      get { return GetDouble(idxTransparency); }
      set { SetDouble(idxTransparency, value); }
    }

    /// <summary>
    /// Gets or sets the transparency of the back side material (0.0 = opaque to 1.0 = transparent)
    /// </summary>
    /// <since>5.0</since>
    public double BackTransparency
    {
      get { return GetDouble(idxBackTransparency); }
      set { SetDouble(idxBackTransparency, value); }
    }

    const int idxIsTwoSided = 0;

    /// <since>5.0</since>
    public bool IsTwoSided
    {
      get
      {
        IntPtr pConstThis = ConstPointer();
        bool rc = UnsafeNativeMethods.CDisplayPipelineMaterial_GetBool(pConstThis, idxIsTwoSided);
        GC.KeepAlive(this);
        return rc;
      }
      set
      {
        IntPtr pThis = NonConstPointer();
        UnsafeNativeMethods.CDisplayPipelineMaterial_SetBool(pThis, idxIsTwoSided, value);
        GC.KeepAlive(this);
      }
    }
    #endregion

    #region methods
    const int idxDiffuse = 0;
    const int idxSpecular = 1;
    const int idxAmbient = 2;
    const int idxEmission = 3;
    const int idxBackDiffuse = 4;
    const int idxBackSpecular = 5;
    const int idxBackAmbient = 6;
    const int idxBackEmission = 7;

    private static readonly int m_alpha_only = Color.FromArgb(255, 0, 0, 0).ToArgb();
    private static int StripAlpha(int argb)
    {
      return argb | m_alpha_only;
    }

    private Color GetColor(int which)
    {
      IntPtr ptr = ConstPointer();
      int abgr = UnsafeNativeMethods.CDisplayPipelineMaterial_GetColor(ptr, which);
      GC.KeepAlive(this);
      return Rhino.Runtime.Interop.ColorFromWin32(StripAlpha(abgr));
    }
    private void SetColor(int which, Color c)
    {
      IntPtr ptr = NonConstPointer();
      int argb = StripAlpha(c.ToArgb());
      UnsafeNativeMethods.CDisplayPipelineMaterial_SetColor(ptr, which, argb);
      GC.KeepAlive(this);
    }

    const int idxShine = 0;
    const int idxTransparency = 1;
    const int idxBackShine = 2;
    const int idxBackTransparency = 3;

    private double GetDouble(int which)
    {
      IntPtr ptr = ConstPointer();
      double rc = UnsafeNativeMethods.CDisplayPipelineMaterial_GetSetDouble(ptr, which, false, 0);
      GC.KeepAlive(this);
      return rc;
    }
    private void SetDouble(int which, double value)
    {
      IntPtr ptr = NonConstPointer();
      UnsafeNativeMethods.CDisplayPipelineMaterial_GetSetDouble(ptr, which, true, value);
      GC.KeepAlive(this);
    }

    IntPtr NonConstMaterialPointer(bool front)
    {
      IntPtr pThis = NonConstPointer();
      return UnsafeNativeMethods.CDisplayPipelineMaterial_MaterialPointer(pThis, front);
    }
    IntPtr ConstMaterialPointer(bool front)
    {
      IntPtr pConstThis = ConstPointer();
      return UnsafeNativeMethods.CDisplayPipelineMaterial_MaterialPointer(pConstThis, front);
    }

    bool AddTexture(string filename, Rhino.DocObjects.TextureType which, bool front)
    {
      IntPtr pMaterial = NonConstMaterialPointer(front);
      bool rc = UnsafeNativeMethods.ON_Material_AddTexture(pMaterial, filename, (int)which);
      GC.KeepAlive(this);
      return rc;
    }
    bool SetTexture(Rhino.DocObjects.Texture texture, Rhino.DocObjects.TextureType which, bool front)
    {
      IntPtr pMaterial = NonConstMaterialPointer(front);
      IntPtr pTexture = texture.ConstPointer();
      bool rc = UnsafeNativeMethods.ON_Material_SetTexture(pMaterial, pTexture, (int)which);
      GC.KeepAlive(this);
      return rc;
    }
    Rhino.DocObjects.Texture GetTexture(Rhino.DocObjects.TextureType which, bool front)
    {
      IntPtr pConstMaterial = ConstMaterialPointer(front);
      int index = UnsafeNativeMethods.ON_Material_GetTexture(pConstMaterial, (int)which);
      if (index >= 0)
        return new Rhino.DocObjects.Texture(index, this, front);
      GC.KeepAlive(this);
      return null;
    }
    #endregion

    #region Bitmap
    /// <since>5.0</since>
    public Rhino.DocObjects.Texture GetBitmapTexture(bool front)
    {
      return GetTexture(Rhino.DocObjects.TextureType.Bitmap, front);
    }
    /// <since>5.0</since>
    public bool SetBitmapTexture(string filename, bool front)
    {
      return AddTexture(filename, Rhino.DocObjects.TextureType.Bitmap, front);
    }
    /// <since>5.0</since>
    public bool SetBitmapTexture(Rhino.DocObjects.Texture texture, bool front)
    {
      return SetTexture(texture, Rhino.DocObjects.TextureType.Bitmap, front);
    }
    /// <summary>
    /// Uses the pixels of an in-memory bitmap as this material's bitmap texture, without
    /// writing an image file.
    /// <para>
    /// The pixels are copied into <see cref="TextureCache"/> under <paramref name="name"/>.
    /// Calling this again with the same name replaces them in place, and the change shows up
    /// on the next redraw.
    /// </para>
    /// </summary>
    /// <param name="name">
    /// The name to cache the pixels under. See <see cref="TextureCache.Set"/>.
    /// </param>
    /// <param name="bitmap">The pixels to use.</param>
    /// <param name="front">true for the front material, false for the back material.</param>
    /// <returns>true on success.</returns>
    /// <since>9.0</since>
    /// <seealso cref="TextureCache"/>
    public bool SetBitmapTexture(string name, System.Drawing.Bitmap bitmap, bool front)
    {
      if (!TextureCache.Set(name, bitmap))
        return false;
      return AddTexture(name, Rhino.DocObjects.TextureType.Bitmap, front);
    }
    #endregion

    #region Bump
    /// <summary>
    /// Gets the bump texture for this display material.
    /// </summary>
    /// <returns>The texture, or null if no bump texture has been added to this material.</returns>
    /// <since>5.0</since>
    public Rhino.DocObjects.Texture GetBumpTexture(bool front)
    {
      return GetTexture(Rhino.DocObjects.TextureType.Bump, front);
    }
    /// <since>5.0</since>
    public bool SetBumpTexture(string filename, bool front)
    {
      return AddTexture(filename, Rhino.DocObjects.TextureType.Bump, front);
    }
    /// <since>5.0</since>
    public bool SetBumpTexture(Rhino.DocObjects.Texture texture, bool front)
    {
      return SetTexture(texture, Rhino.DocObjects.TextureType.Bump, front);
    }
    #endregion

    #region Environment
    /// <since>5.0</since>
    public Rhino.DocObjects.Texture GetEnvironmentTexture(bool front)
    {
      return GetTexture(Rhino.DocObjects.TextureType.Emap, front);
    }
    /// <since>5.0</since>
    public bool SetEnvironmentTexture(string filename, bool front)
    {
      return AddTexture(filename, Rhino.DocObjects.TextureType.Emap, front);
    }
    /// <since>5.0</since>
    public bool SetEnvironmentTexture(Rhino.DocObjects.Texture texture, bool front)
    {
      return SetTexture(texture, Rhino.DocObjects.TextureType.Emap, front);
    }
    #endregion

    #region Transparency
    /// <since>5.0</since>
    public Rhino.DocObjects.Texture GetTransparencyTexture(bool front)
    {
      return GetTexture(Rhino.DocObjects.TextureType.Transparency, front);
    }
    /// <since>5.0</since>
    public bool SetTransparencyTexture(string filename, bool front)
    {
      return AddTexture(filename, Rhino.DocObjects.TextureType.Transparency, front);
    }
    /// <since>5.0</since>
    public bool SetTransparencyTexture(Rhino.DocObjects.Texture texture, bool front)
    {
      return SetTexture(texture, Rhino.DocObjects.TextureType.Transparency, front);
    }
    #endregion
  }

  /// <summary>
  /// One side of the material the display pipeline shades an object with. A texture-style
  /// <see cref="Rhino.Display.VisualAnalysisMode"/> sets its texture up here from
  /// SetUpDisplayAttributes.
  /// </summary>
  /// <remarks>
  /// Valid for as long as the <see cref="DisplayPipelineAttributes"/> it came from, the same as
  /// <see cref="DisplayPipelineAttributes.MeshSpecificAttributes"/>. The attributes passed to SetUpDisplayAttributes belong
  /// to the display pipeline and are disposed when that call returns; using this afterwards does
  /// nothing rather than reaching freed memory.
  /// </remarks>
  public sealed class DisplayAttributeMaterial
  {
    readonly DisplayPipelineAttributes m_parent;
    readonly UnsafeNativeMethods.DisplayAttributesMaterialIdx m_which;

    internal DisplayAttributeMaterial(DisplayPipelineAttributes parent, UnsafeNativeMethods.DisplayAttributesMaterialIdx which)
    {
      m_parent = parent;
      m_which = which;
    }

    // Resolved on every call rather than cached: the pipeline replaces the attributes'
    // material as it moves from object to object.
    internal IntPtr MaterialPointer()
    {
      IntPtr ptr_attributes = m_parent.NonConstPointer();
      IntPtr rc = UnsafeNativeMethods.GetDisplayAttributeMaterialNonConst(ptr_attributes, m_which);
      GC.KeepAlive(m_parent);
      return rc;
    }

    /// <summary>
    /// Shine, 0 to <see cref="Rhino.DocObjects.Material.MaxShine"/>.
    /// </summary>
    /// <since>9.0</since>
    public double Shine
    {
      get => m_parent.GetMaterialDouble(m_which, UnsafeNativeMethods.DisplayAttributesMaterialDouble.Shine);
      set => m_parent.SetMaterialDouble(m_which, UnsafeNativeMethods.DisplayAttributesMaterialDouble.Shine, value);
    }

    /// <summary>
    /// Transparency, 0.0 opaque to 1.0 transparent.
    /// </summary>
    /// <since>9.0</since>
    public double Transparency
    {
      get => m_parent.GetMaterialDouble(m_which, UnsafeNativeMethods.DisplayAttributesMaterialDouble.Transparency);
      set => m_parent.SetMaterialDouble(m_which, UnsafeNativeMethods.DisplayAttributesMaterialDouble.Transparency, value);
    }

    /// <summary>
    /// Index of refraction.
    /// </summary>
    /// <since>9.0</since>
    public double IndexOfRefraction
    {
      get => m_parent.GetMaterialDouble(m_which, UnsafeNativeMethods.DisplayAttributesMaterialDouble.IndexOfRefraction);
      set => m_parent.SetMaterialDouble(m_which, UnsafeNativeMethods.DisplayAttributesMaterialDouble.IndexOfRefraction, value);
    }

    /// <summary>
    /// Reflectivity, 0.0 to 1.0.
    /// </summary>
    /// <since>9.0</since>
    public double Reflectivity
    {
      get => m_parent.GetMaterialDouble(m_which, UnsafeNativeMethods.DisplayAttributesMaterialDouble.Reflectivity);
      set => m_parent.SetMaterialDouble(m_which, UnsafeNativeMethods.DisplayAttributesMaterialDouble.Reflectivity, value);
    }

    /// <summary>
    /// Shades with no smoothing, so the individual render mesh faces are visible.
    /// </summary>
    /// <since>9.0</since>
    public bool FlatShaded
    {
      get => m_parent.GetMaterialBool(m_which, UnsafeNativeMethods.DisplayAttributesMaterialBool.FlatShaded);
      set => m_parent.SetMaterialBool(m_which, UnsafeNativeMethods.DisplayAttributesMaterialBool.FlatShaded, value);
    }

    /// <summary>
    /// Objects acquire color from render materials.
    /// </summary>
    /// <since>9.0</since>
    public bool OverrideObjectColor
    {
      get => m_parent.GetMaterialBool(m_which, UnsafeNativeMethods.DisplayAttributesMaterialBool.MatOverrideObjectColor);
      set => m_parent.SetMaterialBool(m_which, UnsafeNativeMethods.DisplayAttributesMaterialBool.MatOverrideObjectColor, value);
    }

    /// <summary>
    /// Objects acquire transparency from render materials.
    /// </summary>
    /// <since>9.0</since>
    public bool OverrideObjectTransparency
    {
      get => m_parent.GetMaterialBool(m_which, UnsafeNativeMethods.DisplayAttributesMaterialBool.MatOverrideObjectTransparency);
      set => m_parent.SetMaterialBool(m_which, UnsafeNativeMethods.DisplayAttributesMaterialBool.MatOverrideObjectTransparency, value);
    }

    /// <summary>
    /// Objects acquire reflectivity from render materials.
    /// </summary>
    /// <since>9.0</since>
    public bool OverrideObjectReflectivity
    {
      get => m_parent.GetMaterialBool(m_which, UnsafeNativeMethods.DisplayAttributesMaterialBool.MatOverrideObjectReflectivity);
      set => m_parent.SetMaterialBool(m_which, UnsafeNativeMethods.DisplayAttributesMaterialBool.MatOverrideObjectReflectivity, value);
    }

    /// <summary>
    /// Diffuse color.
    /// </summary>
    /// <since>9.0</since>
    public Color Diffuse
    {
      get
      {
        IntPtr ptr_attributes = m_parent.NonConstPointer();
        Color rc = Color.FromArgb(UnsafeNativeMethods.CDisplayAttributeMaterial_GetColor(ptr_attributes, m_which, UnsafeNativeMethods.DisplayAttrsMaterialColor.Diffuse));
        GC.KeepAlive(m_parent);
        return rc;
      }
      set
      {
        IntPtr ptr_attributes = m_parent.NonConstPointer();
        UnsafeNativeMethods.CDisplayAttributeMaterial_SetColor(ptr_attributes, m_which, UnsafeNativeMethods.DisplayAttrsMaterialColor.Diffuse, value.ToArgb());
        GC.KeepAlive(m_parent);
      }
    }

    /// <summary>
    /// Emission color.
    /// </summary>
    /// <since>9.0</since>
    public Color Emission
    {
      get
      {
        IntPtr ptr_attributes = m_parent.NonConstPointer();
        Color rc = Color.FromArgb(UnsafeNativeMethods.CDisplayAttributeMaterial_GetColor(ptr_attributes, m_which, UnsafeNativeMethods.DisplayAttrsMaterialColor.Emission));
        GC.KeepAlive(m_parent);
        return rc;
      }
      set
      {
        IntPtr ptr_attributes = m_parent.NonConstPointer();
        UnsafeNativeMethods.CDisplayAttributeMaterial_SetColor(ptr_attributes, m_which, UnsafeNativeMethods.DisplayAttrsMaterialColor.Emission, value.ToArgb());
        GC.KeepAlive(m_parent);
      }
    }

    /// <summary>
    /// When true the material is drawn at full diffuse color, ignoring the scene lights.
    /// </summary>
    /// <since>9.0</since>
    public bool DisableLighting
    {
      get => UnsafeNativeMethods.ON_Material_GetBool(MaterialPointer(), UnsafeNativeMethods.MaterialBool.DisableLighting);
      set => UnsafeNativeMethods.ON_Material_SetBool(MaterialPointer(), UnsafeNativeMethods.MaterialBool.DisableLighting, value);
    }

    /// <summary>
    /// Luminosity.
    /// </summary>
    /// <since>9.0</since>
    public int Luminosity
    {
      get => m_parent.GetMaterialInt(m_which, UnsafeNativeMethods.DisplayAttributesMaterialInt.Luminosity);
      set => m_parent.SetMaterialInt(m_which, UnsafeNativeMethods.DisplayAttributesMaterialInt.Luminosity, value);
    }

    /// <summary>
    /// Shine intensity.
    /// </summary>
    /// <since>9.0</since>
    public int ShineIntensity
    {
      get => m_parent.GetMaterialInt(m_which, UnsafeNativeMethods.DisplayAttributesMaterialInt.ShineIntensity);
      set => m_parent.SetMaterialInt(m_which, UnsafeNativeMethods.DisplayAttributesMaterialInt.ShineIntensity, value);
    }

    /// <summary>
    /// Assigns a texture, replacing any texture this material already has of the same type.
    /// </summary>
    /// <param name="texture">The texture to assign.</param>
    /// <param name="which">The texture type.</param>
    /// <returns>true on success.</returns>
    /// <since>9.0</since>
    public bool SetTexture(Rhino.DocObjects.Texture texture, Rhino.DocObjects.TextureType which)
    {
      if (texture == null)
        throw new ArgumentNullException(nameof(texture));
      IntPtr ptr_material = MaterialPointer();

      // RH-86871: the physically based shaders do not show an environment map, so an emap on a PBR
      // material draws as if it were not there. The built-in EMap and Zebra modes drop the PBR data
      // first (CRhEMapVAM::SetAnalysisModeDisplayAttributes); this does it for the caller, since the
      // display material is per-draw scratch state and nothing downstream wants the PBR half of it.
      if (Rhino.DocObjects.TextureType.Emap == which &&
          UnsafeNativeMethods.ON_Material_IsPhysicallyBased(ptr_material))
      {
        UnsafeNativeMethods.ON_Material_PBR_ToLegacy(ptr_material);
      }

      IntPtr ptr_const_texture = texture.ConstPointer();
      bool rc = UnsafeNativeMethods.ON_Material_SetTexture(ptr_material, ptr_const_texture, (int)which);
      GC.KeepAlive(texture);
      return rc;
    }

    /// <summary>
    /// Gets the texture of the given type, or null when this material has none.
    /// </summary>
    /// <param name="which">
    /// The texture type. <see cref="Rhino.DocObjects.TextureType.None"/> matches nothing.
    /// </param>
    /// <since>9.0</since>
    public Rhino.DocObjects.Texture GetTexture(Rhino.DocObjects.TextureType which)
    {
      // ON_Material::FindTexture skips the type comparison for no_texture_type and so returns
      // the first texture of any type, which is not what this asks for.
      if (Rhino.DocObjects.TextureType.None == which)
        return null;
      int index = UnsafeNativeMethods.ON_Material_GetTexture(MaterialPointer(), (int)which);
      if (index >= 0)
        return new Rhino.DocObjects.Texture(index, this);
      return null;
    }

    /// <summary>
    /// Gets every texture this material uses.
    /// </summary>
    /// <since>9.0</since>
    public Rhino.DocObjects.Texture[] GetTextures()
    {
      int count = UnsafeNativeMethods.ON_Material_GetTextureCount(MaterialPointer());
      var rc = new Rhino.DocObjects.Texture[count];
      for (int i = 0; i < count; i++)
        rc[i] = new Rhino.DocObjects.Texture(i, this);
      return rc;
    }

    /// <summary>
    /// Removes every texture of the given type.
    /// </summary>
    /// <param name="which">
    /// The texture type. <see cref="Rhino.DocObjects.TextureType.None"/> removes nothing.
    /// </param>
    /// <returns>true when a texture was removed.</returns>
    /// <since>9.0</since>
    public bool RemoveTexture(Rhino.DocObjects.TextureType which)
    {
      // ON_Material::DeleteTexture reads a null filename together with no_texture_type as
      // "delete them all", which is not what this asks for.
      if (Rhino.DocObjects.TextureType.None == which)
        return false;
      return UnsafeNativeMethods.ON_Material_DeleteTexture(MaterialPointer(), null, (int)which);
    }
  }

  /// <summary>
  /// Settings for drawing shaded meshes with the Grasshopper 2 ("G2") shader, used by
  /// <see cref="DisplayPipeline.DrawMeshShaded(Geometry.Mesh, GrasshopperDisplayMaterial)"/>
  /// and <see cref="DisplayPipeline.DrawMeshesShaded"/>.
  /// </summary>
  /// <remarks>
  /// The G2 shader produces a gradient-shaded look from a compact set of inputs rather
  /// than from the full lighting model of a <see cref="DisplayMaterial"/>: a diffuse
  /// colour, plus optional striping, stippling and dark-area desaturation. Unlike
  /// <see cref="DisplayMaterial"/> this is a plain settings object with no unmanaged
  /// resources, so it does not need to be disposed.
  /// </remarks>
  /// <since>9.0</since>
  public class GrasshopperDisplayMaterial
  {
    /// <summary>
    /// Constructs a default material with an opaque white diffuse colour.
    /// </summary>
    /// <since>9.0</since>
    public GrasshopperDisplayMaterial()
    {
    }

    /// <summary>
    /// Constructs a material with a specific diffuse colour.
    /// </summary>
    /// <param name="diffuse">The diffuse colour. Unlike
    /// <see cref="DisplayMaterial"/>, the alpha channel is <b>not</b> ignored — it is
    /// the opacity. See <see cref="Diffuse"/>.</param>
    /// <since>9.0</since>
    public GrasshopperDisplayMaterial(Color diffuse)
    {
      Diffuse = diffuse;
    }

    /// <summary>
    /// Gets or sets the diffuse colour — the colour reached in the brightest areas of
    /// the shading. The alpha channel is <b>opacity</b>, following the usual
    /// <see cref="System.Drawing.Color"/> convention: A=255 is fully opaque and A=0
    /// fully transparent. Note this differs from <see cref="DisplayMaterial.Diffuse"/>,
    /// which ignores alpha. Default: opaque white.
    /// </summary>
    /// <since>9.0</since>
    public Color Diffuse { get; set; } = Color.White;

    /// <summary>
    /// Gets or sets whether the G2 striping overlay is applied. Default: false.
    /// </summary>
    /// <since>9.0</since>
    public bool Striping { get; set; }

    /// <summary>
    /// Gets or sets the width of one stripe band in <b>screen-space pixels</b> (used
    /// only when <see cref="Striping"/> is true). Stripes run along the screen diagonal
    /// and the light/dark pattern repeats every <c>2 × StripeWidth</c> pixels; values
    /// below 1 are treated as 1. This is a pixel-space overlay — it does not scale with
    /// zoom or model units. Default: 0.
    /// </summary>
    /// <since>9.0</since>
    public double StripeWidth { get; set; }

    /// <summary>
    /// Gets or sets the stripe contrast (used only when <see cref="Striping"/> is
    /// true). The effective range is 0..0.9; higher values are clamped, and 0 means no
    /// visible striping. Default: 1.0.
    /// </summary>
    /// <since>9.0</since>
    public double StripeContrast { get; set; } = 1.0;

    /// <summary>
    /// Gets or sets whether an 8x8 Bayer stipple (dither) mask is applied.
    /// Default: false.
    /// </summary>
    /// <since>9.0</since>
    public bool Stippling { get; set; }

    /// <summary>
    /// Gets or sets how much of the stipple pattern is turned off, as a fraction from
    /// 0 to 1 (used only when <see cref="Stippling"/> is true). Default: 0.
    /// </summary>
    /// <remarks>
    /// The pattern is an 8x8 Bayer mask — 64 cells — and this is the fraction of those
    /// cells that stop drawing: 0 turns none off (nothing is stippled away), 0.5 turns
    /// off about half of them (32 of 64), and 1 is clamped to 63 of 64, so a trace of
    /// the mesh always remains rather than disappearing entirely. Values outside 0 to 1
    /// are clamped.
    /// </remarks>
    /// <since>9.0</since>
    public double StipplingAmount { get; set; }

    /// <summary>
    /// Gets or sets how far the shaded colour is blended towards greyscale, as a
    /// fraction from 0 to 1: 0 leaves the colour untouched, 1 is fully greyscale, and
    /// values in between blend linearly. Default: 0.
    /// </summary>
    /// <remarks>
    /// The blend is applied uniformly to the final colour of the whole surface,
    /// including flat (unshaded) vertex colours — see <see cref="ShadeFalseColor"/>. It
    /// holds luminosity constant, so the colour drains towards grey without the surface
    /// appearing to get lighter or darker.
    /// </remarks>
    /// <since>9.0</since>
    public double Desaturation { get; set; }

    /// <summary>
    /// Gets or sets whether a mesh's vertex colours are shaded. Has no effect on a mesh
    /// that has no vertex colours. Default: false.
    /// </summary>
    /// <remarks>
    /// When a mesh carries vertex colours, those colours — its "false colours" — are
    /// always what gets drawn, in place of <see cref="Diffuse"/>. This property decides
    /// only how they are drawn: true lights and shades them like any other surface
    /// colour, while false draws them flat, with no lighting or shading applied at all.
    /// Striping, stippling and <see cref="Desaturation"/> still apply either way.
    /// </remarks>
    /// <since>9.0</since>
    public bool ShadeFalseColor { get; set; }
  }
}
#endif
