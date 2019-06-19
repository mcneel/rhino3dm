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
    public DisplayMaterial()
    {
      m_ptr = UnsafeNativeMethods.CDisplayPipelineMaterial_New(IntPtr.Zero);
    }
    /// <summary>
    /// Duplicate another material.
    /// </summary>
    public DisplayMaterial(DisplayMaterial other)
    {
      IntPtr ptr = other.ConstPointer();
      m_ptr = UnsafeNativeMethods.CDisplayPipelineMaterial_New(ptr);
    }

    public DisplayMaterial(DocObjects.Material material)
    {
      IntPtr pConstMaterial = material.ConstPointer();
      m_ptr = UnsafeNativeMethods.CDisplayPipelineMaterial_New4(pConstMaterial);
    }

    /// <summary>
    /// Constructs a default material with a specific diffuse color.
    /// </summary>
    /// <param name="diffuse">Diffuse color of material. The alpha component of the Diffuse color is ignored.</param>
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
    public Color Diffuse
    {
      get { return GetColor(idxDiffuse); }
      set { SetColor(idxDiffuse, value); }
    }

    /// <summary>
    /// Gets or sets the Diffuse color of the back side of the Material. 
    /// The alpha component of the color will be ignored.
    /// </summary>
    public Color BackDiffuse
    {
      get { return GetColor(idxBackDiffuse); }
      set { SetColor(idxBackDiffuse, value); }
    }

    /// <summary>
    /// Gets or sets the Specular color of the Material. 
    /// The alpha component of the color will be ignored.
    /// </summary>
    public Color Specular
    {
      get { return GetColor(idxSpecular); }
      set { SetColor(idxSpecular, value); }
    }

    /// <summary>
    /// Gets or sets the Specular color of the back side of the Material. 
    /// The alpha component of the color will be ignored.
    /// </summary>
    public Color BackSpecular
    {
      get { return GetColor(idxBackSpecular); }
      set { SetColor(idxBackSpecular, value); }
    }

    /// <summary>
    /// Gets or sets the Ambient color of the Material. 
    /// The alpha component of the color will be ignored.
    /// </summary>
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
    public Color Emission
    {
      get { return GetColor(idxEmission); }
      set { SetColor(idxEmission, value); }
    }
    /// <summary>
    /// Gets or sets the Emissive color of the back side of the Material. 
    /// The alpha component of the color will be ignored.
    /// </summary>
    public Color BackEmission
    {
      get { return GetColor(idxBackEmission); }
      set { SetColor(idxBackEmission, value); }
    }

    /// <summary>
    /// Gets or sets the shine factor of the material (0.0 to 1.0)
    /// </summary>
    public double Shine
    {
      get { return GetDouble(idxShine); }
      set { SetDouble(idxShine, value); }
    }
    /// <summary>
    /// Gets or sets the shine factor of the back side of the material (0.0 to 1.0)
    /// </summary>
    public double BackShine
    {
      get { return GetDouble(idxBackShine); }
      set { SetDouble(idxBackShine, value); }
    }

    /// <summary>
    /// Gets or sets the transparency of the material (0.0 = opaque to 1.0 = transparent)
    /// </summary>
    public double Transparency
    {
      get { return GetDouble(idxTransparency); }
      set { SetDouble(idxTransparency, value); }
    }

    /// <summary>
    /// Gets or sets the transparency of the back side material (0.0 = opaque to 1.0 = transparent)
    /// </summary>
    public double BackTransparency
    {
      get { return GetDouble(idxBackTransparency); }
      set { SetDouble(idxBackTransparency, value); }
    }

    const int idxIsTwoSided = 0;

    public bool IsTwoSided
    {
      get
      {
        IntPtr pConstThis = ConstPointer();
        return UnsafeNativeMethods.CDisplayPipelineMaterial_GetBool(pConstThis, idxIsTwoSided);
      }
      set
      {
        IntPtr pThis = NonConstPointer();
        UnsafeNativeMethods.CDisplayPipelineMaterial_SetBool(pThis, idxIsTwoSided, value);
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
      return Rhino.Runtime.Interop.ColorFromWin32(StripAlpha(abgr));
    }
    private void SetColor(int which, Color c)
    {
      IntPtr ptr = NonConstPointer();
      int argb = StripAlpha(c.ToArgb());
      UnsafeNativeMethods.CDisplayPipelineMaterial_SetColor(ptr, which, argb);
    }

    const int idxShine = 0;
    const int idxTransparency = 1;
    const int idxBackShine = 2;
    const int idxBackTransparency = 3;

    private double GetDouble(int which)
    {
      IntPtr ptr = ConstPointer();
      return UnsafeNativeMethods.CDisplayPipelineMaterial_GetSetDouble(ptr, which, false, 0);
    }
    private void SetDouble(int which, double value)
    {
      IntPtr ptr = NonConstPointer();
      UnsafeNativeMethods.CDisplayPipelineMaterial_GetSetDouble(ptr, which, true, value);
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

    bool AddTexture(string filename, int which, bool front)
    {
      IntPtr pMaterial = NonConstMaterialPointer(front);
      return UnsafeNativeMethods.ON_Material_AddTexture(pMaterial, filename, which);
    }
    bool SetTexture(Rhino.DocObjects.Texture texture, int which, bool front)
    {
      IntPtr pMaterial = NonConstMaterialPointer(front);
      IntPtr pTexture = texture.ConstPointer();
      return UnsafeNativeMethods.ON_Material_SetTexture(pMaterial, pTexture, which);
    }
    Rhino.DocObjects.Texture GetTexture(int which, bool front)
    {
      IntPtr pConstMaterial = ConstMaterialPointer(front);
      int index = UnsafeNativeMethods.ON_Material_GetTexture(pConstMaterial, which);
      if (index >= 0)
        return new Rhino.DocObjects.Texture(index, this, front);
      return null;
    }
    #endregion

    #region Bitmap
    public Rhino.DocObjects.Texture GetBitmapTexture(bool front)
    {
      return GetTexture(Rhino.DocObjects.Material.idxBitmapTexture, front);
    }
    public bool SetBitmapTexture(string filename, bool front)
    {
      return AddTexture(filename, Rhino.DocObjects.Material.idxBitmapTexture, front);
    }
    public bool SetBitmapTexture(Rhino.DocObjects.Texture texture, bool front)
    {
      return SetTexture(texture, Rhino.DocObjects.Material.idxBitmapTexture, front);
    }
    #endregion

    #region Bump
    /// <summary>
    /// Gets the bump texture for this display material.
    /// </summary>
    /// <returns>The texture, or null if no bump texture has been added to this material.</returns>
    public Rhino.DocObjects.Texture GetBumpTexture(bool front)
    {
      return GetTexture(Rhino.DocObjects.Material.idxBumpTexture, front);
    }
    public bool SetBumpTexture(string filename, bool front)
    {
      return AddTexture(filename, Rhino.DocObjects.Material.idxBumpTexture, front);
    }
    public bool SetBumpTexture(Rhino.DocObjects.Texture texture, bool front)
    {
      return SetTexture(texture, Rhino.DocObjects.Material.idxBumpTexture, front);
    }
    #endregion

    #region Environment
    public Rhino.DocObjects.Texture GetEnvironmentTexture(bool front)
    {
      return GetTexture(Rhino.DocObjects.Material.idxEmapTexture, front);
    }
    public bool SetEnvironmentTexture(string filename, bool front)
    {
      return AddTexture(filename, Rhino.DocObjects.Material.idxEmapTexture, front);
    }
    public bool SetEnvironmentTexture(Rhino.DocObjects.Texture texture, bool front)
    {
      return SetTexture(texture, Rhino.DocObjects.Material.idxEmapTexture, front);
    }
    #endregion

    #region Transparency
    public Rhino.DocObjects.Texture GetTransparencyTexture(bool front)
    {
      return GetTexture(Rhino.DocObjects.Material.idxTransparencyTexture, front);
    }
    public bool SetTransparencyTexture(string filename, bool front)
    {
      return AddTexture(filename, Rhino.DocObjects.Material.idxTransparencyTexture, front);
    }
    public bool SetTransparencyTexture(Rhino.DocObjects.Texture texture, bool front)
    {
      return SetTexture(texture, Rhino.DocObjects.Material.idxTransparencyTexture, front);
    }
    #endregion
  }
}
#endif