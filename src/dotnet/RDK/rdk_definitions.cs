#pragma warning disable 1591

namespace Rhino.UI.Controls.Definitions
{
  enum RhRdkUiModalities
  {
    Modeless, // Modeless UIs are non-blocking and usually operate directly on the modeless UI document.
    Modal,    // Modal UIs are blocking. Some operate on temporary ('sandbox') data and others ('semi-modal') operate directly on the modeless UI document.
  };

  /** This enum is for MenuTask UserControl*/
  enum RhRdkUserControl  // Indicates which control the user is interacting with.
  {
    Unknown,                    // Unknown control.
    MainThumb,                  // The main content editor thumbnail list.
    MainTree,                   // The main content editor tree control.
    EditorPreview,              // The content editor preview thumbnail list.
    SubNodeCtrl,                // A sub-node control.
    ColorButton,                // A color button.
    CreateNewButton,            // A 'Create New' button - AKA [+] button.
    ContentCtrl,                // An old-style content control.
    NewContentCtrl,             // A new-style content control.
    NewContentCtrlDropDown,     // A new-style content control's drop-down thumbnail list.
    BreadcrumbCtrl,             // A breadcrumb control.
    FloatingPreview,            // A floating preview.
    Spanner,                    // Spanner menu.
    SpannerModal,               // Spanner menu in modal editor.
    ContentTypeSection,         // Content type section.
    ContentTypeBrowserNew,      // Content type browser 'new' page.
    ContentTypeBrowserExisting, // Content type browser 'existing' page.
    ContentInstanceBrowser,     // Content instance browser.
    ToolTipPreview,             // Tool-tip preview.
  };

  enum RhRdkMaterialSource
  {
    None,
    Layer,
    Parent,
    Object,
  };

  /** This enum is for MenuTask Separator*/
  enum Separator
  {
    kNone,   // No separators will surround this task on the menu.
    kBefore, // A separator will appear before this task on the menu.
    kAfter,  // A separator will appear after this task on the menu.
    kBoth,   // A separator will appear before and after this task on the menu.
  };

  /** This enum is for MenuTask SubMenu() */
  enum SubMenus
  {
    None,       // Task will appear in the top-level menu.
    CreateNew,  // Task will appear in a 'Create New' sub-menu.
    Thumbnails, // Task will appear in a 'Thumbnails' sub-menu.
  };

  /** This enum is for MenuTask Execute*/
  enum UserExecuteResult
  {
    kSuccess,
    kFailure,
    kCancel
  };

}
