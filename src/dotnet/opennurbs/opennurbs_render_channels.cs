using System;
using Rhino.Runtime.InteropWrappers;

namespace Rhino.FileIO
{
  /// <summary>
  /// Represents the dithering in this file.
  /// </summary>
  public class File3dmRenderChannels
  {
    readonly File3dm _parent;

    internal File3dmRenderChannels(File3dm parent)
    {
      _parent = parent;
    }

    /// <summary>
    /// Specifies the mode.
    /// </summary>
    public enum Modes
    {
      /// <summary>
      /// Specifies automatic mode. This determines which channels are needed automatically.
      /// </summary>
      Automatic,

      /// <summary>
      /// Specifies custom mode. This uses the custom list of channel ids.
      /// </summary>
      Custom,
    };

    /// <summary>
    /// Gets or sets the mode.
    /// </summary>
    public Modes Mode
    {
      get => (Modes)UnsafeNativeMethods.ON_RenderChannels_GetMode(_parent.ConstPointer());
      set =>        UnsafeNativeMethods.ON_RenderChannels_SetMode(_parent.NonConstPointer(), (int)value);
    }

    /// <summary>
    /// Gets or sets the custom list of channel ids.
    /// </summary>
    public Guid[] CustomList
    {
      get
      {
        using (var a = new SimpleArrayGuid())
        {
          UnsafeNativeMethods.ON_RenderChannels_GetCustomList(_parent.ConstPointer(), a.NonConstPointer());
          return a.ToArray();
        }
      }

      set
      {
        using (var a = new SimpleArrayGuid())
        {
          foreach (var id in value)
          {
            a.Append(id);
          }

          UnsafeNativeMethods.ON_RenderChannels_SetCustomList(_parent.NonConstPointer(),a.ConstPointer());
        }
      }
    }
  }
}
