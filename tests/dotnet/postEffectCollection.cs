namespace rhino3dm_test;

using System;
using System.Linq;
using Rhino.Render.PostEffects;

public class PostEffectCollectionTests
{
    // (Id, LocalName, On, Shown, Type) of the post effects in tests/models/posteffects.3dm
    static readonly (string Id, string Name, bool On, bool Shown, int Type)[] PostEffects =
    {
        ("57baba9e-fc02-40d2-9682-5f61b9049eaa", "Despeckle", true, true, 0), ("ce2f1c40-7f25-4c2d-b1d3-271e118d843b", "Bloom", true, false, 0),
        ("7243f003-c4f1-415c-8228-35e5efaabd60", "Glow", true, false, 0), ("b4d7b2f3-fa0d-47fb-8063-6869e96dbf35", "Fog", true, false, 0),
        ("ab3b31ab-7d40-4a59-aa5c-5df833a81e8f", "Depth of field", true, false, 0), ("f8e993bf-3407-4121-b71a-6876c2541cc7", "Multiplier", true, false, 0),
        ("1ec87a84-f61f-43dc-b331-92d8b81ed468", "Denoise", true, true, 0), ("acb8d258-c1d6-499d-aa23-02dcdea2b0a2", "Clamp", false, false, 1),
        ("64e53275-0583-4ec4-9bde-b9413df25f08", "Black/White point", false, false, 1), ("a51d4149-3685-463e-b32f-d9ac986b7c1b", "Logarithmic", false, false, 1),
        ("ec302d96-402e-4244-bf25-5d0859ebf37c", "Filmic", false, false, 1), ("4723b167-50a6-42ce-bba6-ce4f8013495b", "False color", false, false, 1),
        ("c65eb6ba-ee66-4c48-9457-95f0936eca08", "Hue/Saturation/Luminance", true, false, 2), ("0034b235-c351-4674-be3f-afec691187b3", "Brightness/Contrast", true, false, 2),
        ("4398d28e-ed29-4974-9304-d56d0e61cc15", "Noise", true, false, 2), ("38986982-3eab-4988-b8cb-32dd4ad0baa3", "Gaussian blur", true, false, 2),
    };

    static PostEffectCollection Collection(string name) => Helpers.ReadModel(name).Settings.RenderSettings.PostEffects;

    // Iterating RenderSettings.PostEffects gives each post effect's id, name, On, Shown and Type.
    [Test]
    public void PostEffectCollectionItemsOfAFile()
    {
        var coll = Collection("posteffects.3dm");
        var got = coll.Select(p => (p.Id.ToString(), p.LocalName, p.On, p.Shown, (int)p.Type)).ToArray();
        Assert.That(got, Is.EqualTo(PostEffects));
    }

    // A file with no post effects has an empty PostEffects collection.
    [Test]
    public void PostEffectCollectionOfFilesWithoutPostEffectsIsEmpty()
    {
        foreach (var name in new[] { "file3dm_stuff.3dm", "textEntities_r8.3dm", "sphereDecals.3dm" })
            Assert.That(Collection(name).ToList(), Is.Empty, name);
    }

    // PostEffectDataFromId finds each post effect by its id.
    [Test]
    public void PostEffectCollectionPostEffectDataFromId()
    {
        var coll = Collection("posteffects.3dm");
        var items = coll.ToList();
        for (int i = 0; i < PostEffects.Length; i++)
        {
            var (oid, oname, _, _, _) = PostEffects[i];
            Assert.That((items[i].Id.ToString(), items[i].LocalName), Is.EqualTo((oid, oname)));
            var d = coll.PostEffectDataFromId(new Guid(oid));
            Assert.That(d.Id.ToString(), Is.EqualTo(oid));
            Assert.That(d.LocalName, Is.EqualTo(oname));
        }
    }

    // The PostEffectData of an id not in the collection throws when used.
    [Test]
    public void PostEffectDataFromAnUnknownIdRaisesOnUse()
    {
        foreach (var name in new[] { "posteffects.3dm", "file3dm_stuff.3dm" })
        {
            var ghost = Collection(name).PostEffectDataFromId(Guid.NewGuid());
            Assert.That(() => ghost.LocalName, Throws.Exception, name);
        }
    }

    // Setting On on a PostEffectData changes the post effect in the file's collection.
    [Test]
    public void PostEffectDataWritesReachTheFile()
    {
        var coll = Collection("posteffects.3dm");
        var oid = PostEffects[0].Id;
        var d = coll.PostEffectDataFromId(new Guid(oid));
        var on = d.On;
        d.On = !on;
        Assert.That(coll.First(p => p.Id.ToString() == oid).On, Is.EqualTo(!on));
    }

    // A new PostEffectCollection is empty.
    [Test]
    public void PostEffectCollectionNewIsEmpty()
    {
        Assert.That(new PostEffectCollection().ToList(), Is.Empty);
    }
}
