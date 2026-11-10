namespace rhino3dm_test;

using System;
using System.Linq;
using Rhino.DocObjects;
using Rhino.FileIO;
using Rhino.Geometry;

public class File3dmGroupTableTests
{
    // a File3dm with groups g0, g1 and points in no group, in g0 and in both
    static (File3dm f, int g0, int g1, Guid none, Guid inG0, Guid both) GroupTableFile()
    {
        var f = new File3dm();
        int g0 = f.AllGroups.AddGroup();
        int g1 = f.AllGroups.AddGroup();
        Guid Add(params int[] groups)
        {
            var a = new ObjectAttributes();
            foreach (var g in groups)
                a.AddToGroup(g);
            return f.Objects.AddPoint(new Point3d(groups.Length, 0, 0), a);
        }
        var none = Add();
        var inG0 = Add(g0);
        var both = Add(g0, g1);
        return (f, g0, g1, none, inG0, both);
    }

    // GroupMembers returns the File3dmObjects in a group.
    [Test]
    public void GroupTableGroupMembers()
    {
        var file3dm = Helpers.ReadModel("groups.3dm");
        var members = file3dm.AllGroups.GroupMembers(0);
        Assert.That(members, Is.Not.Empty);
        Assert.That(members[0], Is.InstanceOf<File3dmObject>());
    }

    // GroupMembers(index) lists the objects in that group, skipping objects in no group, and is empty for an unknown group.
    [Test]
    public void GroupTableGroupMembersListsTheObjectsInAGroup()
    {
        var (f, g0, g1, none, inG0, both) = GroupTableFile();
        var m0 = f.AllGroups.GroupMembers(g0);
        Assert.That(m0.Select(o => o.Id), Is.EquivalentTo(new[] { inG0, both }));
        Assert.That(f.AllGroups.GroupMembers(g1).Select(o => o.Id), Is.EqualTo(new[] { both }));
        Assert.That(f.AllGroups.GroupMembers(99), Is.Empty);
    }

    // GroupMembers(index) returns the group's objects.
    [Test]
    public void GroupTableGroupMembersAreTheGroupsObjects()
    {
        var f = Helpers.ReadModel("groups.3dm");
        Assert.That(f.AllGroups.GroupMembers(0).Select(o => o.Attributes.ObjectId.ToString()).OrderBy(s => s, StringComparer.Ordinal),
            Is.EqualTo(new[] { "439da937-a505-4be1-8c9b-36e40c4ee057", "674d5429-34d2-4029-9835-d86be8eb9fa5" }));
    }

    // AllGroups.Delete(group) returns true for a group in the table; Delete(index) works too.
    [Test]
    public void GroupTableDeleteById()
    {
        var f = new File3dm();
        for (int i = 0; i < 3; i++)
            f.AllGroups.AddGroup();
        Assert.That(f.AllGroups.Delete(f.AllGroups.FindIndex(1)), Is.True);
        f.AllGroups.Delete(0);
        Assert.That(f.AllGroups.Count, Is.EqualTo(1));
    }
}
