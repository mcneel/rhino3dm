using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Rhino.Runtime.Notifications
{
  /// <summary>
  /// A Notification instance can be used to inform the user about various events. For
  /// a Notification instance to be displayed in Rhino, it must be added to the
  /// <see cref="NotificationCenter"/>. When added, it will be displayed in the
  /// Notifications panel in Rhino. A Notification contains 1 to 3 buttons that are 
  /// automatically wired to its <see cref="ButtonClicked"/> Action if it is not null. 
  /// The buttons are displayed when the Notification is shown modally by either the user 
  /// clicking on a particular notification in the Notifications panel, or by programatically 
  /// showing it using <see cref="ShowModal"/>.
  /// 
  /// Currently, only process-wide notifications are
  /// supported; document specific notifications are not possible.
  /// 
  /// Notification instances contain metadata that can be added, modified, or removed during
  /// its life. The metadata is important for LINQ queries and other patterns.
  /// For example, a particular action may require that multiple notifications be modified. 
  /// Thus, a LINQ query can be performed on the <see cref="NotificationCenter"/> using metadata
  /// to retrieve related Notification objects and modify them as a batch.
  /// 
  /// Notification objects implement <see cref="IAssemblyRestrictedObject"/>. By default, a 
  /// Notification can be editedby any assembly, but explicitly specifing allowed assemblies 
  /// in the constructor changes this behavior.
  /// 
  /// Notification objects are not thread-safe and should only be manipulated in UI thread.
  /// </summary>
  public class Notification : INotifyPropertyChanged, IAssemblyRestrictedObject
  {
    /// <summary>
    /// Determines the severity of a notification.
    /// </summary>
    public enum Severity
    {
      /// <summary>
      /// Least serious.
      /// </summary>
      Debug,
      /// <summary>
      /// Not serious.
      /// </summary>
      Info,
      /// <summary>
      /// Important.
      /// </summary>
      Warning,
      /// <summary>
      /// Very important.
      /// </summary>
      Serious,
      /// <summary>
      /// Extremely important.
      /// </summary>
      Critical
    }

    private readonly Dictionary<string, object> _propertyBackingDictionary = new Dictionary<string, object>();
    private T _GetPropertyValue<T>([CallerMemberName] string propertyName = null, T defaultValue=default(T))
    {
      if (propertyName == null) throw new ArgumentNullException("propertyName");

      object value;
      if (_propertyBackingDictionary.TryGetValue(propertyName, out value))
      {
        if (value == null)
          return default(T);

        return (T)value;
      }

      return defaultValue;
    }

    private bool _SetPropertyValue<T>(T newValue, bool checkIfIllegalAssembly, bool updateDate=true, [CallerMemberName] string propertyName = null)
    {
      if (propertyName == null) throw new ArgumentNullException("propertyName");

      if (checkIfIllegalAssembly)
        this._ThrowIfIllegalAssembly();

      if (EqualityComparer<T>.Default.Equals(newValue, _GetPropertyValue<T>(propertyName)))
        return false;

      this._propertyBackingDictionary[propertyName] = newValue;

      if (updateDate)
        this.DateUpdated = DateTime.UtcNow;

      this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
      return true;
    }

    private void _ThrowIfIllegalAssembly()
    {
      if (!this.Editable())
        throw new InvalidOperationException($"The Notification object {this.ToString()} cannot be edited. It belongs to an assembly that is not allowed to make changes or the code is not wrapped around Notification.ExcecuteAssemblyProtectedCode()");
    }

    private Action<ButtonType> _buttonClicked;
    private NonNullableDictionary<string, string> _metadata;
    private static Stack<MethodBase> _protectedActions = new Stack<MethodBase>();

    /// <summary>
    /// The date the notification was last modified.
    /// </summary>
    /// <remarks>Only visible elements of the notification will cause this property to change.
    /// Changes in metadata or other non-visible members will have no effect on this property.</remarks>
    public DateTime DateUpdated { get; private set; } = DateTime.UtcNow;

    /// <summary>
    /// The assemblies that can modify this instance.
    /// </summary>
    /// <remarks>If empty, any assembly is allowed to edit the instance. You 
    /// may specify additional assemblies that should have access to an instance at creation time.</remarks>
    public ICollection<Assembly> AllowedAssemblies { get; private set; }

    /// <summary>
    /// A copy of all the metadata for this class.
    /// </summary>
    /// <remarks>Modifying the returned dictionary will have no effect on the metadata for this notification.</remarks>
    public IDictionary<string, string> MetadataCopy
    {
      get { return new NonNullableDictionary<string, string>(this._metadata); }
    }

    /// <summary>
    /// An Action that will be invoked whenever a button for the notification is clicked or the notification is closed.
    /// </summary>
    public Action<ButtonType> ButtonClicked
    {
      get { return this._buttonClicked; }

      set
      {
        this._ThrowIfIllegalAssembly();
        this._buttonClicked = value;
      }
    }

    /// <summary>
    /// Gets or sets metadata for this instance.
    /// </summary>
    /// <param name="key">The key to use to get or set metadata.</param>
    /// <returns>null if the key does not map to any metadata; otherwise returns the string representing the metadata.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the calling assembly cannot modify this instance</exception>
    public string this[string key]
    {
      get
      {
        return this._metadata[key];
      }

      set
      {
        this._ThrowIfIllegalAssembly();
        this._metadata[key] = value;
      }
    }

    /// <summary>
    /// The title of the notification. The title is displayed when the notification is displayed modally in Rhino.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if the calling assembly cannot modify this instance</exception>
    public string Title
    {
      get { return _GetPropertyValue<string>(); }

      set { _SetPropertyValue(value, true); }
    }

    /// <summary>
    /// The description of the notification. The description is displayed in the Notifications panel in Rhino.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if the calling assembly cannot modify this instance</exception>
    public string Description
    {
      get { return _GetPropertyValue<string>(); }

      set { _SetPropertyValue(value, true); }
    }

    /// <summary>
    /// The message of the notification. The message is shown only when the instance is displayed modally. It should contain details about the notification.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if the calling assembly cannot modify this instance</exception>
    public string Message
    {
      get { return _GetPropertyValue<string>(); }

      set { _SetPropertyValue(value, true); }
    }

    /// <summary>
    /// The severity of the notification. Changing the severity of the notification may change the way Rhino chooses to display the Notifications panel. 
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if the calling assembly cannot modify this instance</exception>
    public Severity SeverityLevel
    {
      get { return _GetPropertyValue<Severity>(defaultValue:Severity.Info); }

      set { _SetPropertyValue(value, true); }
    }

    /// <summary>
    /// The localized title of the Cancel button.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if the calling assembly cannot modify this instance</exception>
    /// <remarks>Notifications can 1 to 3 buttons for user input. Each button can have a localized title. With the exception of the Cancel button, 
    /// setting a button title to null (the default) will prevent that button from appearing in the interface. Note that in order for buttons
    /// other than the Cancel button to be displayed, you must set a <see cref="ButtonClicked"/> Action.</remarks>
    public string CancelButtonTitle
    {
      get { return _GetPropertyValue<string>(); }

      set { _SetPropertyValue(value, true); }
    }

    /// <summary>
    /// The localized title of the Confirm button.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if the calling assembly cannot modify this instance</exception>
    /// <remarks>Notifications can 1 to 3 buttons for user input. Each button can have a localized title. With the exception of the Cancel button, 
    /// setting a button title to null (the default) will prevent that button from appearing in the interface. Note that in order for buttons
    /// other than the Cancel button to be displayed, you must set a <see cref="ButtonClicked"/> Action.</remarks>
    public string ConfirmButtonTitle
    {
      get { return _GetPropertyValue<string>(); }

      set { _SetPropertyValue(value, true); }
    }

    /// <summary>
    /// The localized title of the Alternate button.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if the calling assembly cannot modify this instance</exception>
    /// <remarks>Notifications can 1 to 3 buttons for user input. Each button can have a localized title. With the exception of the Cancel button, 
    /// setting a button title to null (the default) will prevent that button from appearing in the interface. Note that in order for buttons
    /// other than the Cancel button to be displayed, you must set a <see cref="ButtonClicked"/> Action.</remarks>
    public string AlternateButtonTitle
    {
      get { return _GetPropertyValue<string>(); }

      set { _SetPropertyValue(value, true); }
    }

    /// <summary>
    /// A field used by Rhino for displaying notifications. Not intended for public use.
    /// </summary>
    public Guid? ShowEventId
    {
      get { return _GetPropertyValue<Guid?>(); }
    }

    /// <summary>
    /// If a <see cref="Notification"/> object is only allowed to be modified by certain
    /// assemblies, then any code that interacts with it must be wrapped around this method,
    /// or a <see cref="InvalidOperationException"/> will be thrown. For performance reasons,
    /// the code wrapped by this method should be kept as simple as possible.
    /// </summary>
    /// <param name="action">The code to run that modifies one or more notification objects</param>
    /// <remarks>This method is not thread-safe and should only be manipulated in UI thread.</remarks>
    public static void ExecuteAssemblyProtectedCode(Action action)
    {
      _protectedActions.Push(action.Method);

      try
      {
        action();
      }
      finally
      {
        _protectedActions.Pop();
      }
    }

    /// <summary>
    /// If a <see cref="Notification"/> object is only allowed to be modified by certain
    /// assemblies, then any code that interacts with it must be wrapped around this method,
    /// or a <see cref="InvalidOperationException"/> will be thrown. For performance reasons,
    /// the code wrapped by this method should be kept as simple as possible.
    /// </summary>
    /// <param name="func">The code to run that modifies one or more notification objects</param>
    /// <remarks>This method is not thread-safe and should only be manipulated in UI thread.</remarks>

    public static TResult ExecuteAssemblyProtectedCode<TResult>(Func<TResult> func)
    {
      _protectedActions.Push(func.Method);

      try
      {
        return func();
      }
      finally
      {
        _protectedActions.Pop();
      }
    }

    /// <summary>
    /// Creates a new instance that can be edited by the given assemblies.
    /// </summary>
    /// <param name="allowedAssemblies">The assemblies that will be allowed to edit the instance.
    /// If null or empty, any assembly will be able to edit this notification.</param>
    /// <remarks>Specifying one or more assemblies limits certain operations with the notification object,
    /// throwing an <see cref="InvalidOperationException"/> if an assembly outside the ones passed tries to interact with it.
    /// Any code that tries to interact with a notification object that has restricted assemblies must be wrapped in a 
    /// <see cref="ExecuteAssemblyProtectedCode(Action)"/>. For performance reasons,
    /// it is imporant that such methods are kept as simple as possible.</remarks>
    public Notification(IEnumerable<Assembly> allowedAssemblies)
    {
      this._metadata = new NonNullableDictionary<string, string>();

      if (allowedAssemblies == null)
        allowedAssemblies = new Assembly[0];

      this.AllowedAssemblies = new ReadOnlyCollection<Assembly>(allowedAssemblies.Distinct().ToList());
    }

    /// <summary>
    /// Creates a new instance that can be edited by any assembly.
    /// </summary>
    public Notification() : this(null) { }

    /// <summary>
    /// Tells Rhino to display the notification modally.
    /// </summary>
    /// <remarks>The notification will only be displayed if/once the instance is added to the <see cref="NotificationCenter"/>.
    /// Rhino keeps a queue of notifications that need to be shown modally, so calling this method does not mean that
    /// this particular notification will be immediately displayed.</remarks>
    public void ShowModal()
    {
      _SetPropertyValue(Guid.NewGuid(), false, false, "ShowEventId"); //Anybody can show a modal.
    }

    /// <summary>
    /// Tells Rhino to hide the notification if it is being currently shown as a modal.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if the calling assembly cannot modify this instance</exception>
    /// <remarks>If the notification is not being currently shown but is being queued by Rhino as 
    /// a result of calling <see cref="ShowModal"/>, then the notification will be dequeued. 
    /// If the notification was never queued, then this method has no effect.
    /// </remarks>
    public void HideModal()
    {
      _SetPropertyValue<Guid?>(null, true, false, "ShowEventId");
    }

    /// <summary>
    /// Removes metadata from this instance.
    /// </summary>
    /// <param name="key">The key of the metadata to remove.</param>
    /// <returns>true if the metada was removed; otherwise false.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the calling assembly cannot modify this instance</exception>
    public bool RemoveMetadata(string key)
    {
      this._ThrowIfIllegalAssembly();
      return this._metadata.Remove(key);
    }

    /// <summary>
    /// Returns a readable string representation of the instance.
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
      return $"Notification '{this.Description}' (Severity: {this.SeverityLevel})";
    }

    /// <summary>
    /// Determines whether an assembly can modify the instance. Any code that modifies an assembly protected
    /// notification must be wrapped in a <see cref="ExecuteAssemblyProtectedCode(Action)"/> method.
    /// </summary>
    /// <remarks>Before modifying a notification you are not sure you created (such as a notification returned
    /// from a LINQ query), you should call this method first to ensure you can indeed edit the object.</remarks>
    public bool Editable()
    {
      if (this.AllowedAssemblies.Count == 0)
        return true;

      //See if any of the allowed assemblies are in the protected actions call stack.
      var result = _protectedActions.FirstOrDefault(x => this.AllowedAssemblies.Contains(x.DeclaringType.Assembly));

      return result != null;
    }

    /// <summary>
    /// Triggered whenever a visible property of the instance changes.
    /// </summary>
    public event PropertyChangedEventHandler PropertyChanged;
  }
}
