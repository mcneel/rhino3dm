namespace rhino3dm_test;

using System.Linq;
using Rhino.FileIO;

public class File3dmPlugInDataTableTests
{
    // PlugInData iterates over Count File3dmPlugInData records, each with its plug-in id.
    [Test]
    public void PlugInDataTableIterates()
    {
        var g = Helpers.ReadModel("file3dm_stuff.3dm");
        var pd = g.PlugInData.ToList();
        Assert.That(pd.Count, Is.EqualTo(g.PlugInData.Count));
        Assert.That(pd.All(p => p is File3dmPlugInData && p.PlugInId is Guid), Is.True);
    }
}
