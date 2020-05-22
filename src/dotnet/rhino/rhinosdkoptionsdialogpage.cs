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
  }
}
#endif