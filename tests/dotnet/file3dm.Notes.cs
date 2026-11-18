namespace rhino3dm_test;

using System.Drawing;
using System.IO;
using Rhino.FileIO;

public class File3dmNotesTests
{
    // A new model has empty, hidden, plain-text notes.
    [Test]
    public void NotesDefaultsAreEmpty()
    {
        var notes = new File3dm().Notes;
        Assert.That(notes.Notes, Is.Null.Or.Empty);
        Assert.That(notes.IsVisible, Is.False);
        Assert.That(notes.IsHtml, Is.False);
    }

    // The text, flags and window rectangle set on the notes are in the written file (RH3DM-208).
    [Test]
    public void NotesSurviveWriteAndRead()
    {
        var w = new File3dm();
        w.Notes.Notes = "Revision B\nlicence: CC-BY, café";
        w.Notes.IsVisible = true;
        w.Notes.IsHtml = true;
        w.Notes.WindowRectangle = new Rectangle(10, 20, 300, 200);
        using var d = new Helpers.TempDir();
        var path = Path.Combine(d.Path, "notes.3dm");
        Assert.That(w.Write(path, 8), Is.True);
        Assert.That(File3dm.ReadNotes(path), Is.EqualTo("Revision B\nlicence: CC-BY, café"));
        var f = File3dm.Read(path);
        Assert.That(f.Notes.Notes, Is.EqualTo("Revision B\nlicence: CC-BY, café"));
        Assert.That(f.Notes.IsVisible, Is.True);
        Assert.That(f.Notes.IsHtml, Is.True);
        Assert.That(f.Notes.WindowRectangle, Is.EqualTo(new Rectangle(10, 20, 300, 200)));
    }

    // Setting one value of the notes leaves the others as they were.
    [Test]
    public void NotesSetterKeepsTheOtherValues()
    {
        var notes = new File3dm().Notes;
        notes.Notes = "text";
        notes.IsVisible = true;
        notes.WindowRectangle = new Rectangle(1, 2, 3, 4);
        notes.IsHtml = true;
        Assert.That(notes.Notes, Is.EqualTo("text"));
        Assert.That(notes.IsVisible, Is.True);
        Assert.That(notes.WindowRectangle, Is.EqualTo(new Rectangle(1, 2, 3, 4)));
    }
}
