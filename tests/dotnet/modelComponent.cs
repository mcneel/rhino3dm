namespace rhino3dm_test;

using Rhino.DocObjects;

public class ModelComponentTests
{
    // IsValidComponentName accepts an ordinary name and rejects an empty one.
    [Test]
    public void ModelComponentIsValidComponentNameChecksTheName()
    {
        Assert.That(ModelComponent.IsValidComponentName("Layer 01"), Is.True);
        Assert.That(ModelComponent.IsValidComponentName(""), Is.False);
    }

    // NamePathSeparator is '::', on ModelComponent and on a derived class such as Layer.
    [Test]
    public void ModelComponentNamePathSeparator()
    {
        Assert.That(ModelComponent.NamePathSeparator, Is.EqualTo("::"));
        Assert.That(Layer.NamePathSeparator, Is.EqualTo("::"));
    }
}
