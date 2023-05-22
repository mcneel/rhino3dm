#pragma warning disable 1591

#if RHINO_SDK
namespace Rhino.UI
{
  public abstract class OptionsDialogPage : StackedDialogPage
  {
    protected OptionsDialogPage(string englishPageTitle)
      :base(englishPageTitle)
    {

    }

    /// <since>5.0</since>
    public virtual Commands.Result RunScript(RhinoDoc doc, Commands.RunMode mode)
    {
      RhinoApp.WriteLine(Localization.LocalizeString("Scripting not supported for this option", 33));
      Dialogs.ShowMessage(Localization.LocalizeString("Scripting not supported for this option", 34), Localization.LocalizeString("Unsupported Option", 35));
      return Commands.Result.Success;
    }

    /// <summary>
    /// 17 March 2021 John Morse
    /// For internal use in determining the page type.  RhinoMac uses this
    /// to ensure pages are sized properly when hosting them.
    /// </summary>
    /// <since>8.0</since>
    public enum PageType
    {
      Options,
      DocumentProperties
    }

    /// <summary>
    /// 17 March 2021 John Morse
    /// For internal use in determining the page type.  RhinoMac uses this
    /// to ensure pages are sized properly when hosting them.
    /// </summary>
    /// <since>8.0</since>
    public PageType OptionsPageType { get; internal set; } = PageType.Options;
  }
}
#endif
