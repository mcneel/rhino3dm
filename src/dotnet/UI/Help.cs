
namespace Rhino.UI
{
  /// <summary>
  /// Implement this class to add help to a modeless UI panel.
  /// </summary>
  public interface IHelp
  {
    /// <summary>
    /// Help topic URL which gets passed to RhinoHelp.Show
    /// </summary>
    string HelpUrl { get; }
  }

  /// <summary>
  /// Provides access to the built in Rhino help system
  /// </summary>
  public static class RhinoHelp
  {
    /// <summary>
    /// Call this method to display standard Rhino help
    /// </summary>
    /// <param name="helpLink">
    /// Rhino help links are formatted like this:
    /// http://docs.mcneel.com/rhino/6/help/en-us/index.htm#commands/line.htm
    /// This parameter would be equal to "#commands/line.htm" in the link
    /// above.  Rhino will calculate the string up to and including the
    /// index.html and append this value to the end.
    /// </param>
    /// <returns></returns>
    public static bool Show(string helpLink)
    {
      return (ShowHelpCalled = UnsafeNativeMethods.CRhinoApp_DoHelp(helpLink));
    }

    /// <summary>
    /// Total hack to work around the fact that options and object properties
    /// pages OnHelp virtual methods return void instead of bool.  This will
    /// get set to the result of the last Show call above.
    /// </summary>
    /// <value><c>true</c> if show help called; otherwise, <c>false</c>.</value>
    internal static bool ShowHelpCalled { get; set; }
  }
}
