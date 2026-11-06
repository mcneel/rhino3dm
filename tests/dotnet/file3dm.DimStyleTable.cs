namespace rhino3dm_test;

using System.Linq;
using Rhino.DocObjects;
using Rhino.FileIO;

public class File3dmDimStyleTableTests
{
    // AllDimStyles.Delete(item) removes a dimension style from the table.
    [Test]
    public void DeleteDimStyle()
    {
        var file3dm = new File3dm();
        file3dm.ApplicationName = "dotnet";
        file3dm.ApplicationDetails = "rhino3dm-tests-deleteDimStyle";
        file3dm.ApplicationUrl = "https://rhino3d.com";

        file3dm.AllDimStyles.Add(new DimensionStyle());
        file3dm.AllDimStyles.Add(new DimensionStyle());

        int qtyDimStyles1 = file3dm.AllDimStyles.Count;
        file3dm.AllDimStyles.Delete(file3dm.AllDimStyles.First());
        int qtyDimStyles2 = file3dm.AllDimStyles.Count;

        Assert.That((qtyDimStyles1, qtyDimStyles2), Is.EqualTo((2, 1)));
    }
}
