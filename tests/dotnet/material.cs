namespace rhino3dm_test;

using Rhino.Display;
using Rhino.DocObjects;

public class MaterialTests
{
    // The Texture overload of SetBumpTexture stores the texture as a Bump texture, not as a Bitmap.
    [Test]
    public void SetBumpTexture_keepsBumpType()
    {
        var mat = new Material();
        var tex = new Texture();
        tex.FileName = "bump.png";
        mat.SetBumpTexture(tex);

        var bump = mat.GetBumpTexture();
        Assert.That(bump, Is.Not.Null);
        Assert.That(bump.TextureType, Is.EqualTo(TextureType.Bump));
        Assert.That(mat.GetBitmapTexture(), Is.Null);
    }

    // SetTexture assigns the texture to the named channel, here a PBR roughness map.
    [Test]
    public void SetTexture_honorsTextureType()
    {
        var mat = new Material();
        var tex = new Texture();
        tex.FileName = "roughness.png";
        tex.TextureType = TextureType.PBR_Roughness;
        mat.SetTexture(tex, tex.TextureType);

        var roughness = mat.GetTexture(TextureType.PBR_Roughness);
        Assert.That(roughness, Is.Not.Null);
        Assert.That(roughness.TextureType, Is.EqualTo(TextureType.PBR_Roughness));
    }

    // Material.PhysicallyBased is null until ToPhysicallyBased(), then a view onto the material.
    [Test]
    public void MaterialPhysicallyBasedIsNoneUntilConverted()
    {
        var m = new Material();
        Assert.That(m.PhysicallyBased, Is.Null);
        m.ToPhysicallyBased();
        var pbr = m.PhysicallyBased;
        pbr.Metallic = 0.25;
        Assert.That(m.PhysicallyBased.Metallic, Is.EqualTo(0.25));
    }

    // Material.MaxShine is 255.
    [Test]
    public void MaterialMaxShineIs255()
    {
        Assert.That(Material.MaxShine, Is.EqualTo(255.0));
    }

    // GetBitmapTexture returns the material's texture; changing it does not change the material until SetBitmapTexture.
    [Test]
    public void MaterialBitmapTextureIsACopyOnWriteView()
    {
        var m = new Material();
        Assert.That(m.GetBitmapTexture(), Is.Null);
        var t = new Texture();
        t.FileName = "a.png";
        Assert.That(m.SetBitmapTexture(t), Is.True);
        var v = m.GetBitmapTexture();
        Assert.That(v.FileName, Is.EqualTo("a.png"));
        v.FileName = "c.png";
        Assert.That(v.FileName, Is.EqualTo("c.png"));
        Assert.That(m.GetBitmapTexture().FileName, Is.EqualTo("a.png"));
        Assert.That(m.SetBitmapTexture(v), Is.True);
        Assert.That(m.GetBitmapTexture().FileName, Is.EqualTo("c.png"));
    }

    // A physically based material's BaseColor reads back the Color4f it was set to.
    [Test]
    public void MaterialPhysicallyBasedBaseColorIsAColor4f()
    {
        var c = new Color4f(0.25f, 0.5f, 0.75f, 1.0f);
        var m = new Material();
        m.ToPhysicallyBased();
        var pbr = m.PhysicallyBased;
        pbr.BaseColor = c;
        Assert.That(pbr.BaseColor == c, Is.True);
    }

    // ToPhysicallyBased makes a material physically based, and its Emission color can be set.
    [Test]
    public void MaterialToPhysicallyBasedEmission()
    {
        var m = new Material();
        m.ToPhysicallyBased();
        Assert.That(m.IsPhysicallyBased, Is.True);
        m.PhysicallyBased.Emission = new Color4f(1.0f, 0.5f, 0.25f, 1.0f);
        var e = m.PhysicallyBased.Emission;
        Assert.That(e.R, Is.EqualTo(1.0).Within(1e-6));
        Assert.That(e.G, Is.EqualTo(0.5).Within(1e-6));
        Assert.That(e.B, Is.EqualTo(0.25).Within(1e-6));
    }

    // SetTexture(texture, type) sets the texture, and GetTexture(type) reads it back.
    [Test]
    public void MaterialSetTextureByType()
    {
        var m = new Material();
        var tex = new Texture();
        tex.FileName = "/tmp/bump.png";
        tex.TextureType = TextureType.Bump;
        Assert.That(m.SetTexture(tex, tex.TextureType), Is.True);
        Assert.That(m.GetTexture(TextureType.Bump).FileName, Is.EqualTo("/tmp/bump.png"));
    }

    // RenderMaterialInstanceId starts nil and can be set; setting it makes the universal render engine the plug-in.
    [Test]
    public void MaterialRenderMaterialInstanceId()
    {
        var m = new Material();
        Assert.That(m.RenderMaterialInstanceId, Is.EqualTo(Guid.Empty));
        var uid = Guid.NewGuid();
        m.RenderMaterialInstanceId = uid;
        Assert.That(m.RenderMaterialInstanceId, Is.EqualTo(uid));
        Assert.That(m.RenderPlugInId, Is.EqualTo(new Guid("99999999-9999-9999-9999-999999999999")));
    }

    // CompareAppearance ignores names, and orders materials with a different diffuse color antisymmetrically.
    [Test]
    public void MaterialCompareAppearance()
    {
        var a = new Material();
        var b = new Material();
        a.Name = "one";
        b.Name = "two";
        Assert.That(Material.CompareAppearance(a, b), Is.EqualTo(0));
        b.DiffuseColor = System.Drawing.Color.FromArgb(255, 255, 0, 0);
        var c = Material.CompareAppearance(a, b);
        Assert.That(c, Is.Not.EqualTo(0));
        Assert.That(Material.CompareAppearance(b, a), Is.EqualTo(-c));
        Assert.That(c, Is.GreaterThan(0));
    }
}
