using System;

namespace Rhino.FileIO
{
  /// <summary>
  /// Details about Rhino a Rhino plug-in that was loaded in the Rhino
  /// process when a 3dm file is saved
  /// </summary>
  public class File3dmPlugInDetails
  {
    /// <summary>Plug-in Id</summary>
    /// <since>9.0</since>
    public Guid Id { get; set; }

    /// <summary>Public facing name of the plug-in</summary>
    /// <since>9.0</since>
    public string Name { get; set; }

    /// <summary>The plug-in's binary filename</summary>
    /// <since>9.0</since>
    public string Filename {  get; set; }
  }
}
