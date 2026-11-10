namespace rhino3dm_test;

using System.Linq;
using Rhino.DocObjects;
using Rhino.Geometry;

public class ArrowheadTests
{
    // Arrowhead.GetPoints gives a solid triangle's outline and no points for a user block.
    [Test]
    public void ArrowheadGetPoints()
    {
        var pts = Arrowhead.GetPoints(DimensionStyle.ArrowType.SolidTriangle);
        Assert.That(pts.Select(p => (p.X, p.Y)).ToArray(), Is.EqualTo(new[] { (0.0, 0.0), (-1.0, 0.25), (-1.0, -0.25) }));
        Assert.That(Arrowhead.GetPoints(DimensionStyle.ArrowType.UserBlock), Is.Empty);
    }
}
