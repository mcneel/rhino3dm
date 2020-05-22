#pragma warning disable 1591
#if RHINO_SDK
namespace Rhino.ApplicationSettings
{
  /// <summary>
  /// Provides static (Shared in Vb.Net) properties to modify Rhino History settings.
  /// </summary>
  public static class HistorySettings
  {
    private const int idxRecordingEnabled = 0;
    private const int idxUpdateEnabled = 1;
    private const int idxObjectLockingEnabled = 2;
    private const int idxBrokenRecordWarningEnabled = 3;

    /// <summary>
    /// When history recording is enabled, new objects keep a record of how they
    /// were constructed so that they can be updated if an input object changes.
    /// </summary>
    /// <since>5.0</since>
    public static bool RecordingEnabled
    {
      get
      {
        return UnsafeNativeMethods.CRhinoHistoryManager_GetBool(idxRecordingEnabled);
      }
      set
      {
        UnsafeNativeMethods.CRhinoHistoryManager_SetBool(idxRecordingEnabled, value);
      }
    }

    /// <summary>
    /// When history update is enabled, dependent objects are automatically updated
    /// when an antecedent is modified.
    /// </summary>
    /// <since>5.0</since>
    public static bool UpdateEnabled
    {
      get
      {
        return UnsafeNativeMethods.CRhinoHistoryManager_GetBool(idxUpdateEnabled);
      }
      set
      {
        UnsafeNativeMethods.CRhinoHistoryManager_SetBool(idxUpdateEnabled, value);
      }
    }

    /// <summary>
    /// When history object locking is enabled, objects with history on them act as if
    /// they were locked and the only way to modify these objects is to edit their inputs.
    /// </summary>
    /// <since>5.0</since>
    public static bool ObjectLockingEnabled
    {
      get
      {
        return UnsafeNativeMethods.CRhinoHistoryManager_GetBool(idxObjectLockingEnabled);
      }
      set
      {
        UnsafeNativeMethods.CRhinoHistoryManager_SetBool(idxObjectLockingEnabled, value);
      }
    }

    /// <summary>
    /// Displays a warning dialog when an action is taken that breaks the link between the output and input objects. 
    /// </summary>
    /// <since>6.10</since>
    public static bool BrokenRecordWarningEnabled
    {
      get
      {
        return UnsafeNativeMethods.CRhinoHistoryManager_GetBool(idxBrokenRecordWarningEnabled);
      }
      set
      {
        UnsafeNativeMethods.CRhinoHistoryManager_SetBool(idxBrokenRecordWarningEnabled, value);
      }
    }
  }
}
// skip
//public class HistoryManager { }
//public class RecordHistoryCommandOptionHelper { }
#endif
