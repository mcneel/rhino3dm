
namespace Rhino.Runtime.Notifications
{
  /// <summary>
  /// The NotificationCenter holds all <see cref="Notification"/> objects that are displayed in the Notifications panel by Rhino.
  /// 
  /// The NotificationCenter is not thread-safe and should only be used in the UI thread.
  /// </summary>
  public static class NotificationCenter
  {
    /// <summary>
    /// A set containing all the <see cref="Notification"/> instances. Any Notification added will be displayed in the
    /// Notifications panel. Any Notification removed will be removed from the Notifications panel, and, if shown modally
    /// or queued to be shown modally, will be closed or dequeued from the modal queue.
    /// </summary>
    public static readonly TrulyObservableOrderedSet<Notification> Notifications = new TrulyObservableOrderedSet<Notification>();
  }
}
