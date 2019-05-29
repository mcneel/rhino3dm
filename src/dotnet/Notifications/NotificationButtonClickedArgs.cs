using System;

namespace Rhino.Runtime.Notifications
{
  /// <summary>
  /// The type of button in a notification.
  /// </summary>
  public enum ButtonType
  {
    /// <summary>
    /// Denotes either the Cancel button as well as, on some platforms, the close button if present.
    /// </summary>
    CancelOrClose,
    /// <summary>
    /// The Confirm buttton.
    /// </summary>
    Confirm,
    /// <summary>
    /// The Alternate button.
    /// </summary>
    Alternate
  }

  /// <summary>
  /// Used when a button is clicked for a notification.
  /// </summary>
  public class NotificationButtonClickedArgs : EventArgs
  {
    /// <summary>
    /// Creates a new instance.
    /// </summary>
    /// <param name="notification">The notification whose button was clicked.</param>
    /// <param name="buttonClicked">The button that was clicked.</param>
    public NotificationButtonClickedArgs(Notification notification, ButtonType buttonClicked)
    {
      this.Notification = notification;
      this.ButtonClicked = buttonClicked;
    }

    /// <summary>
    /// The notification whose button was clicked.
    /// </summary>
    public Notification Notification { get; private set; }
    /// <summary>
    /// The button that was clicked.
    /// </summary>
    public ButtonType ButtonClicked { get; private set; }
  }
}
