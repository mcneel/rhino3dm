#if RHINO_SDK

#pragma warning disable 1591
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Xml;
using System.Drawing;
using System.Globalization;
using System.Text;
using System.Runtime.Serialization;
using System.Security.Permissions;
using System.Security.AccessControl;

using Rhino.Geometry;
using Timer = System.Threading.Timer;

namespace Rhino.Runtime
{
  interface ISettingsWriteErrorService
  {
    /// <summary>
    /// If there is a entry for this settings file then increment the settings
    /// file successfully saved counter otherwise; do nothing.
    /// </summary>
    /// <param name="fileName">
    /// Used to generate registry key unique to this file name
    /// </param>
    /// <since>6.3</since>
    void LogSuccess(string fileName);

    /// <summary>
    /// If there is a entry for this settings file increment the failure counter
    /// otherwise create a new entry and set the failure count to one.
    /// </summary>
    /// <param name="fileName">
    /// Used to generate registry key unique to this file name
    /// </param>
    /// <param name="exception">
    /// The exception causing this event
    /// </param>
    /// <since>6.3</since>
    void LogFailure(string fileName, Exception exception);

    /// <summary>
    /// Gets the number of failed write attempts as well as successful writes
    /// since the first reported failure.
    /// </summary>
    /// <param name="fileName">
    /// Used to generate registry key unique to this file name
    /// </param>
    /// <param name="successCount">
    /// Successful write events since first failure
    /// </param>
    /// <param name="failureCount">
    /// Number
    /// </param>
    /// <returns></returns>
    /// <since>6.3</since>
    void GeErrorInfo(string fileName, out int successCount, out int failureCount);
  }

  sealed class DefaultSettingsWriteErrorService : ISettingsWriteErrorService
  {
    public void LogSuccess(string fileName) { }

    public void LogFailure(string fileName, Exception exception) { }

    public void GeErrorInfo(string fileName, out int successCount, out int failureCount)
    {
      successCount = failureCount = 0;
    }
  }

  /// <summary>
  /// Settings serialization service
  /// </summary>
  interface ISettingsService
  {
    /// <since>6.2</since>
    void TestHarness();
    /// <summary>
    /// If true then WriteToPlist and ReadFromPlist are called to serialize settings
    /// otherwise; WriteSettings and ReadSettings are called using XML in a memory
    /// stream.
    /// </summary>
    /// <value><c>true</c> if supports plist; otherwise, <c>false</c>.</value>
    bool SupportsPlist { get; }
    /// <summary>
    /// Called when a setting value is deleted from a PersistentSettings dictionary.
    /// </summary>
    /// <param name="persistentSettings">Persistent settings.</param>
    /// <param name="key">Key.</param>
    void DeleteItem(PersistentSettings persistentSettings, string key);
    /// <summary>
    /// Called when a child key is deleted from a PersistentSettings dictionary.
    /// </summary>
    /// <param name="persistentSettings">Persistent settings.</param>
    /// <param name="key">Key.</param>
    void DeleteChild(PersistentSettings persistentSettings, string key);
    /// <summary>
    /// Return true if the settings system supports reading from the AllUsers location
    /// prior to the current user section.  If this is true any settings read from
    /// AllUsers will be marked as read-only at runtime
    /// </summary>
    /// <since>6.2</since>
    bool SupportsAllUsers { get; }
    /// <summary>
    /// Return true if the settings system should use file watchers when reading settings
    /// to determine when settings have changed so changed events can be raised.
    /// </summary>
    /// <since>6.2</since>
    bool UseFileWatchers { get; }
    /// <summary>
    /// Return true if settings changed events should be raised immediately after writing
    /// the settings file.
    /// </summary>
    /// <since>6.2</since>
    bool RaiseChangedEventAfterWriting { get; }
    /// <summary>
    /// Writes the settings to OS specific location.
    /// </summary>
    /// <param name="isShuttingDown">
    /// Will be true if Rhino is in the process of shutting down when writing the file
    /// </param>
    /// <param name="stream">
    /// Stream containing the XML document
    /// </param>
    /// <param name="fileName">
    /// Requested file name and location.
    /// </param>
    /// <param name="writing">
    /// Called twice, first time with a false argument meaning about to write the
    /// final file, second time with a true meaning it is done writing.
    /// </param>
    /// <returns>
    /// Return <c>true</c>, if settings was written, <c>false</c> otherwise.
    /// </returns>
    bool WriteSettings(bool isShuttingDown, Stream stream, string fileName, Action<bool, bool> writing);
    /// <summary>
    /// Writes the settings.
    /// </summary>
    /// <returns><c>true</c>, if settings was written, <c>false</c> otherwise.</returns>
    /// <param name="isShuttingDown">If set to <c>true</c> is shutting down.</param>
    /// <param name="settings">Settings.</param>
    /// <param name="commandSettings">Command settings.</param>
    /// <param name="writing">Writing.</param>
    bool WriteToPlist(bool isShuttingDown, PersistentSettings settings, Dictionary<string, PersistentSettings> commandSettings, Action<bool, bool> writing);
    /// <summary>
    /// Check to see if a setting value exist in a key and if it does create a new
    /// SettingValue and initialize its ValueString using the plist value.
    /// </summary>
    /// <param name="persistentSettings">Persistent settings containing the key.</param>
    /// <param name="key">Setting value key name.</param>
    /// <param name="newSettingValue">
    /// Invoked to create new SettingValue which will get initialized with the current
    /// PLIST value.
    /// </param>
    void TryGetPlistValue(PersistentSettings persistentSettings, string key, Func<string, SettingValue> newSettingValue);
    /// <summary>
    /// Enumerate Rhino PLIST file for value keys associated with the specified PersistentSettings
    /// dictionary.
    /// </summary>
    /// <param name="persistentSettings">Persistent settings dictionary.</param>
    /// <param name="settingValueDictionary">Value dictionary associated with the PersistentSettings.</param>
    void EnumeratePlistKeys(PersistentSettings persistentSettings, Dictionary<string, SettingValue> settingValueDictionary);
    /// <summary>
    /// Enumerates the Rhino PLIST finding child keys associated with the specified PersistentSettings
    /// dictionary.
    /// </summary>
    /// <param name="persistentSettings">Persistent settings.</param>
    /// <param name="settingValueDictionary">Setting value dictionary.</param>
    void EnumeratePlistChildKeys(PersistentSettings persistentSettings, Dictionary<string, PersistentSettings> settingValueDictionary);
    /// <summary>
    /// Read settings XML contents into a Stream which will get passed to a XML
    /// document for parsing.
    /// </summary>
    /// <param name="fileName">File name.</param>
    /// <returns>
    /// Returns null if the file does not exist or could not be read, a valid
    /// Stream otherwise.
    /// </returns>
    /// <since>6.2</since>
    Stream ReadSettings(string fileName);
    /// <summary>
    /// Read settings directly from some system provided archive.  This is used
    /// on Mac to read settings from the com.mcneel.rhinoceros.ver.plist
    /// </summary>
    /// <returns><c>true</c>, if read settings was directed, <c>false</c> otherwise.</returns>
    /// <param name="plugInId">Plug in identifier.</param>
    /// <param name="settings">Settings.</param>
    /// <param name="commandSettings">Command settings.</param>
    bool ReadFromPlist(Guid plugInId, ref PersistentSettings settings, Func<string, PersistentSettings>commandSettings);
    /// <summary>
    /// Deletes the settings file.
    /// </summary>
    /// <param name="fileName">
    /// Full path to settings to delete.
    /// </param>
    /// <since>6.2</since>
    void DeleteSettingsFile(string fileName);

    /// <summary>
    /// Gets value string.
    /// </summary>
    /// <returns>The value string</returns>
    /// <param name="settingValue">Setting value to query</param>
    string GetPlistValue(SettingValue settingValue);

    /// <summary>
    /// Sets value string.
    /// </summary>
    /// <param name="settingValue">Setting value to query</param>
    /// <param name="value">Value string</param>
    void SetValue(SettingValue settingValue, string value);

    /// <summary>
    /// Called when a new setting value is added to the runtime
    /// dictionary, Mac Rhino should check the PLIST for the
    /// key and initialize it with the current key value
    /// </summary>
    /// <param name="settingValue">Setting value.</param>
    void OnAddSettingValue(SettingValue settingValue);
  }

  /// <summary>
  /// This service has been tested on both Mac and Windows and can be used to write
  /// settings XML files on either platform.
  /// </summary>
  sealed class FileSettingsService : ISettingsService
  {
    #region Construction and access to service providers
    /// <summary>
    /// This is private so there can only be a single instance of this class.  Call
    /// FileSettingsService.Service to get the single instance.
    /// </summary>
    private FileSettingsService(){}

    /// <summary>
    /// The one and only instance of ISettingsWriteErrorService, lazily created the first
    /// time WriteErrorService called
    /// </summary>
    private static ISettingsWriteErrorService g_write_error_service_implementation;
    /// <summary>
    /// The one and only instance of ISettingsWriteErrorService, lazily created the first
    /// time this is called
    /// </summary>
    public static ISettingsWriteErrorService WriteErrorService => g_write_error_service_implementation
                                                                  ?? (g_write_error_service_implementation = HostUtils.GetPlatformService<ISettingsWriteErrorService>() ?? new DefaultSettingsWriteErrorService());

    /// <summary>
    /// The one and only instance of FileSettingsService, lazily created the first
    /// time it is called
    /// </summary>
    /// <value>The service.</value>
    public static FileSettingsService Service => g_file_service ?? (g_file_service = new FileSettingsService());
    /// <summary>
    /// The one and only instance of FileSettingsService, lazily created the first
    /// time Service is called
    /// </summary>
    private static FileSettingsService g_file_service;

#endregion Construction and access to service providers

#region ISettingsService implementation

    public void TestHarness(){}

#region ISettingsService properties
    /// <summary>
    /// If true then WriteToPlist and ReadFromPlist are called to serialize settings
    /// otherwise; WriteSettings and ReadSettings are called using XML in a memory
    /// stream.
    /// </summary>
    /// <value><c>true</c> if supports plist; otherwise, <c>false</c>.</value>
    public bool SupportsPlist => false;

    /// <summary>
    /// Called when a setting value is deleted from a PersistentSettings dictionary.
    /// </summary>
    /// <param name="persistentSettings">Persistent settings.</param>
    /// <param name="key">Key.</param>
    public void DeleteItem(PersistentSettings persistentSettings, string key){}

    /// <summary>
    /// Called when a child key is deleted from a PersistentSettings dictionary.
    /// </summary>
    /// <param name="persistentSettings">Persistent settings.</param>
    /// <param name="key">Key.</param>
    public void DeleteChild(PersistentSettings persistentSettings, string key){}

    /// <summary>
    /// Return true if the settings system supports reading from the AllUsers location
    /// prior to the current user section.  If this is true any settings read from
    /// AllUsers will be marked as read-only at runtime
    /// </summary>
    public bool SupportsAllUsers => !HostUtils.RunningOnOSX;
    /// <summary>
    /// Return true if the settings system should use file watchers when reading settings
    /// to determine when settings have changed so changed events can be raised.
    /// </summary>
    public bool UseFileWatchers => !HostUtils.RunningOnOSX;
    /// <summary>
    /// Return true if settings changed events should be raised immediately after writing
    /// the settings file.
    /// </summary>
    public bool RaiseChangedEventAfterWriting => HostUtils.RunningOnOSX;

    #endregion ISettingsService properties

    /// <summary>
    /// Writes the settings.
    /// </summary>
    /// <returns><c>true</c>, if settings was written, <c>false</c> otherwise.</returns>
    /// <param name="isShuttingDown">If set to <c>true</c> is shutting down.</param>
    /// <param name="settings">Settings.</param>
    /// <param name="commandSettings">Command settings.</param>
    /// <param name="writing">Writing.</param>
    public bool WriteToPlist(bool isShuttingDown, PersistentSettings settings, Dictionary<string, PersistentSettings> commandSettings, Action<bool, bool> writing)
    {
      return false;
    }
    /// <summary>
    /// Check to see if a setting value exist in a key and if it does create a new
    /// SettingValue and initialize its ValueString using the plist value.
    /// </summary>
    /// <param name="persistentSettings">Persistent settings containing the key.</param>
    /// <param name="key">Setting value key name.</param>
    /// <param name="newSettingValue">
    /// Invoked to create new SettingValue which will get initialized with the current
    /// PLIST value.
    /// </param>
    public void TryGetPlistValue(PersistentSettings persistentSettings, string key, Func<string, SettingValue> newSettingValue){}
    /// <summary>
    /// Enumerate Rhino PLIST file for value keys associated with the specified PersistentSettings
    /// dictionary.
    /// </summary>
    /// <param name="persistentSettings">Persistent settings dictionary.</param>
    /// <param name="settingValueDictionary">Value dictionary associated with the PersistentSettings.</param>
    public void EnumeratePlistKeys(PersistentSettings persistentSettings, Dictionary<string, SettingValue> settingValueDictionary){}
    /// <summary>
    /// Enumerates the Rhino PLIST finding child keys associated with the specified PersistentSettings
    /// dictionary.
    /// </summary>
    /// <param name="persistentSettings">Persistent settings.</param>
    /// <param name="settingValueDictionary">Setting value dictionary.</param>
    public void EnumeratePlistChildKeys(PersistentSettings persistentSettings, Dictionary<string, PersistentSettings> settingValueDictionary){}

    /// <summary>
    /// Read settings directly from some system provided archive.  This is used
    /// on Mac to read settings from the com.mcneel.rhinoceros.ver.plist
    /// </summary>
    /// <returns><c>true</c>, if read settings was directed, <c>false</c> otherwise.</returns>
    /// <param name="plugInId">Plug in identifier.</param>
    /// <param name="settings">Settings.</param>
    /// <param name="commandSettings">Command settings.</param>
    public bool ReadFromPlist(Guid plugInId, ref PersistentSettings settings, Func<string, PersistentSettings> commandSettings) { return false; }

    /// <summary>
    /// Writes the settings to OS specific location.
    /// </summary>
    /// <param name="isShuttingDown">
    /// Will be true if Rhino is in the process of shutting down when writing the file
    /// </param>
    /// <param name="stream">
    /// Stream containing the XML document
    /// </param>
    /// <param name="fileName">
    /// Requested file name and location.
    /// </param>
    /// <param name="writing">
    /// Called twice, first time with a false argument meaning about to write the
    /// final file, second time with a true meaning it is done writing.
    /// </param>
    /// <returns>
    /// Return <c>true</c>, if settings was written, <c>false</c> otherwise.
    /// </returns>
    public bool WriteSettings(bool isShuttingDown, Stream stream, string fileName, Action<bool, bool> writing)
    {
      // Write settings to a temporary file in the output folder
      var temp_file_name = WriteTempFile(isShuttingDown, Path.GetDirectoryName(fileName), fileName, stream);
      // If the temporary file was successfully created then tempFileName will be the full path to
      // the file name otherwise it will be null
      if (string.IsNullOrEmpty (temp_file_name) || !File.Exists (temp_file_name))
        return false; // Error creating temp file so bail

      // Begin write notification
      writing?.Invoke(isShuttingDown, false);

      // Attempt to copy the temp file to the settingsFileName, try several times just in case
      // another instance is currently writing the file
      var result = false;
      const int max_trys = 5; // No more than five times
      for (var i = 0; i < max_trys; i++)
      {
        try
        {
          File.Copy(temp_file_name, fileName, true);
          result = true; // File successfully copied
          i = max_trys; // Will cause the for loop to end
        }
        catch
        {
          // Error copying the file so pause then try again
          Thread.Sleep(50);
        }
      }

      // Delete the temporary file
      try
      {
        File.Delete(temp_file_name);
      }
      catch (UnauthorizedAccessException)
      {
        try
        {
          // Have received a few Raygun exceptions of this exception, found a 
          // couple of post that say to make sure the file attributes are set
          // to normal before trying to delete so I thought I would give it a
          // try.
          File.SetAttributes(temp_file_name, FileAttributes.Normal);
          File.Delete(temp_file_name);
        }
        catch (Exception exception)
        {
          HostUtils.ExceptionReport(exception);
        }
      }
      catch (Exception ex)
      {
        HostUtils.ExceptionReport (ex);
      }

      // End writing notification
      writing?.Invoke(isShuttingDown, true);

      return result;
    }
    /// <summary>
    /// Read settings XML contents into a Stream which will get passed to a XML
    /// document for parsing.
    /// </summary>
    /// <param name="fileName">File name.</param>
    /// <returns>
    /// Returns null if the file does not exist or could not be read, a valid
    /// Stream otherwise.
    /// </returns>
    public Stream ReadSettings(string fileName)
    {
      if (File.Exists(fileName) == false)
        return null;
      FileStream stream = null;
      try
      {
        bool keep_trying = false;
        int lock_try_count = 0;
        // Lame attempt to handle file locking. For performance reasons, only
        // try once more after failure to acquire lock.
        do
        {
          try
          {
            keep_trying = false;
            stream = new FileStream(fileName, FileMode.Open, FileAccess.Read);
          }
          catch (IOException ioe)
          {
            if (!(ioe is FileNotFoundException) &&
                !(ioe is DirectoryNotFoundException) &&
                !(ioe is PathTooLongException))
            {
              // File is locked. Try once more then give up.  
              if (lock_try_count < 1)
              {
                keep_trying = true;
                lock_try_count++;
                Thread.Sleep (50);
              }
            }
          }
        } while (keep_trying);
      }
      catch(Exception exception)
      {
        stream?.Dispose();
        stream = null;
        HostUtils.ExceptionReport(exception);
      }
      return stream;
    }
    /// <summary>
    /// Deletes the settings file.
    /// </summary>
    /// <param name="fileName">
    /// Full path to settings to delete.
    /// </param>
    public void DeleteSettingsFile(string fileName)
    {
      DeleteFile (fileName);
    }

    /// <summary>
    /// Gets value string.
    /// </summary>
    /// <returns>The value string</returns>
    /// <param name="settingValue">Setting value to query</param>
    public string GetPlistValue(SettingValue settingValue) => settingValue?.ValueString;

    /// <summary>
    /// Sets value string.
    /// </summary>
    /// <param name="settingValue">Setting value to query</param>
    /// <param name="value">Value string</param>
    public void SetValue(SettingValue settingValue, string value) => settingValue.ValueString = value;

    /// <summary>
    /// Called when a new setting value is added to the runtime
    /// dictionary, Mac Rhino should check the PLIST for the
    /// key and initialize it with the current key value
    /// </summary>
    /// <param name="settingValue">Setting value.</param>
    public void OnAddSettingValue(SettingValue settingValue) { }

#endregion ISettingsService implementation

    /// <summary>
    /// Constructs a temporary file and write plug-in and plug-in command settings to the temp file.
    /// Only writes PersistentSettings that contain one or more item with a value that differs
    /// from the default value.
    /// </summary>
    public string WriteTempFile(bool isShuttingDown, string outputFolder, string fileName, Stream stream)
    {
      if (stream == null)
        return null;
      try
      {
        if (!Directory.Exists(outputFolder))
          Directory.CreateDirectory(outputFolder);
        var temp_file = Path.Combine(outputFolder, "settings-" + Guid.NewGuid().ToString() + ".tmp.xml");
        // Write the memory stream to the temp file
        using (var file_stream = new FileStream(temp_file, FileMode.Create, FileAccess.Write))
        {
          stream.Seek(0, SeekOrigin.Begin);
          stream.CopyTo(file_stream);
          stream.Flush();
          stream.Close();
        }
        WriteErrorService.LogSuccess(fileName);
        return temp_file;
      }
      catch (UnauthorizedAccessException uaex)
      {
        // Special case handling for UnauthorizedAccessException, check access
        // control settings for the output folder
        var access_string = GetAccessRightsString(outputFolder);
        if (string.IsNullOrEmpty(access_string))
        {
          // Had trouble getting the access string so just report the original exception
          // Only report to RayGun when shutting down
          if (isShuttingDown)
            HostUtils.ExceptionReport(uaex);
          WriteErrorService.LogFailure(fileName, uaex);
        }
        else
        {
          System.Diagnostics.Debug.WriteLine(uaex.Message);
          System.Diagnostics.Debug.WriteLine(access_string);
          try
          {
            // Throw a new exception containing the original message and the output
            // directory access permissions
            WriteErrorService.GeErrorInfo(fileName, out int successCount, out int failureCount);
            // Get the last two directory names plus the settings file name so we can tell if the
            // settings file is associated with a plug-in or core Rhino
            var sub1 = Path.GetFileName(Path.GetDirectoryName(fileName));
            var sub2 = Path.GetFileName(Path.GetDirectoryName(Path.GetDirectoryName(fileName)));
            var report_file_name = Path.Combine(sub1 ?? "", Path.Combine(sub2 ?? "", Path.GetFileName(fileName) ?? ""));
            // Round down to the nearest 5%
            var failure_rate = Math.Round(Math.Floor(((float)failureCount / successCount) / .05f) * .05f, 2, MidpointRounding.AwayFromZero);
            var addational_info = $"Settings file: {report_file_name}{Environment.NewLine}Failure rate: {failure_rate}";
            throw new SettingsTempFileUnauthorizedAccessException(uaex.Message + Environment.NewLine + addational_info + Environment.NewLine + access_string, uaex);
          }
          catch (Exception e)
          {
            // Only report to RayGun when shutting down
            if (isShuttingDown)
              HostUtils.ExceptionReport(e);
            WriteErrorService.LogFailure(fileName, e);
          }
        }
        return null;
      }
      catch (Exception ex)
      {
        // Only report to RayGun when shutting down
        if (isShuttingDown)
          HostUtils.ExceptionReport(ex);
        WriteErrorService.LogFailure(fileName, ex);
        return null;
      }
    }

    class SettingsTempFileUnauthorizedAccessException : UnauthorizedAccessException
    {
      public SettingsTempFileUnauthorizedAccessException(string message, Exception innerException)
      : base(message, innerException)
      {
      }
    }

    private string GetAccessRightsString(string directoryName)
    {
      var builder = new StringBuilder();
      try
      {
        var di = new DirectoryInfo(directoryName);
        var rules = di.GetAccessControl(AccessControlSections.Access)?.GetAccessRules(true, true, typeof(System.Security.Principal.NTAccount));
        if (rules == null)
          return "Directory.GetAccessControl returned null";
        builder.AppendLine($"Access rights for {directoryName}");
        foreach (var rule in rules)
          if (rule is FileSystemAccessRule access)
            builder.AppendLine($"{access.IdentityReference.Value}::{access.AccessControlType}({access.FileSystemRights})");
        var result = builder.ToString();
        return string.IsNullOrWhiteSpace(result)
          ? "No FileSystemAccessRules found"
          : result;
      }
      catch (Exception exception)
      {
        builder.AppendLine($"GetAccessRightsString exception caught: {exception.Message}");
        var tab = string.Empty;
        for (var e = exception; e != null; e = e.InnerException)
          builder.AppendLine($"{tab += "\t"}{e.Message}");
        return builder.ToString();
      }
    }

    #region Delete helpers

    /// <summary>
    /// Helper method to delete a directory if it is empty.
    /// </summary>
    /// <param name="directory">Full path to directory to delete.</param>
    /// <returns>Returns true if the directory was empty and successfully deleted otherwise returns false.</returns>
    public bool DeleteDirectoryIfEmpty(string directory)
    {
      bool rc = false;
      try
      {
        if (Directory.Exists(directory))
        {
          string [] files = Directory.GetFiles(directory);
          string [] folders = Directory.GetDirectories(directory);
          if ((null == files || files.Length < 1) && (null == folders || folders.Length < 1))
          {
            Directory.Delete(directory);
            rc = true;
          }
        }
      }
      catch (Exception ex)
      {
        HostUtils.ExceptionReport(ex);
      }
      return rc;
    }

    public void DeleteFileAndDirectory(string settingsFileName)
    {
      // Delete the settings file
      File.Delete(settingsFileName);
      // Move up the settings path and delete the folder and its parent as long as they don't contain
      // files or sub folders
      var folder = Path.GetDirectoryName(settingsFileName);
      for (var i = 0; i < 2 && DeleteDirectoryIfEmpty(folder); i++)
        folder = Path.GetDirectoryName (folder);
    }

    public void DeleteFile(string fileName)
    {
      // If there are no settings, the settings dictionary is empty or the settings dictionary only contains default values
      // and the settings file exists
      if (!File.Exists(fileName))
        return;
      try
      {
        DeleteFileAndDirectory(fileName);
      }
      catch (UnauthorizedAccessException ae)
      {
        if (File.Exists(fileName))
        {
          File.SetAttributes(fileName, FileAttributes.Normal);
          DeleteFileAndDirectory(fileName);
        } 
        else
          HostUtils.ExceptionReport(ae);
      }
      catch (Exception ex)
      {
        HostUtils.ExceptionReport(ex);
      }
    }

#endregion Delete helpers
  }
}

namespace Rhino
{
  /// <summary>
  /// Used to convert string to string lists and string dictionaries and back
  /// to strings again.
  /// </summary>
  public static class PersistentSettingsConverter
  {
    /// <summary>
    /// Determines if the string value is formatted as a string list.
    /// </summary>
    /// <param name="s">String to check</param>
    /// <returns>
    /// Returns true if it is a XML string list otherwise return false.
    /// </returns>
    /// <since>6.0</since>
    public static bool IsStringList(string s)
    {
      return (!string.IsNullOrEmpty(s) && s.Contains("<list"));
    }

    /// <summary>
    /// Determines if the string value is formatted as a key value pair
    /// string list.
    /// </summary>
    /// <param name="s">String to check</param>
    /// <returns>
    /// Returns true if it is a XML key value pair list otherwise return false.
    /// </returns>
    /// <since>6.0</since>
    public static bool IsStringDictionary(string s)
    {
      return (!string.IsNullOrEmpty(s) && s.Contains("<dictionary"));
    }

    /// <summary>
    /// Attempts to convert a string to a string value list.
    /// </summary>
    /// <param name="s">String to parse</param>
    /// <param name="value">
    /// Result will get copied here, if the string is null or empty then this
    /// will be an empty list, if there was an error parsing then this will be
    /// null otherwise it will be the string parsed as a list.
    /// </param>
    /// <returns>
    /// Returns true if the string is not empty and properly formatted as a
    /// string list otherwise returns false.
    /// </returns>
    /// <since>6.0</since>
    public static bool TryParseStringList(string s, out string[] value)
    {
      if (!IsStringList(s))
      {
        value = (string.IsNullOrWhiteSpace(s) ? new string[0] : new[] {s});
        return false;
      }
      try
      {
        var doc = new XmlDocument();
        doc.LoadXml(s);
        var nodes = doc.FirstChild.SelectNodes("./value");
        value = new string[nodes == null ? 0 : nodes.Count];
        for (var i = 0; nodes != null && i < nodes.Count; i++)
          value[i] = nodes[i].InnerText;
        return true;
      }
      catch
      {
        value = null;
        return false;
      }
    }

    /// <summary>
    /// Converts a string array to a properly formatted string list XML string.
    /// </summary>
    /// <param name="values">
    /// List of strings to turn into a string list XML string.
    /// </param>
    /// <returns>
    /// Returns a properly formatted XML string that represents the list of
    /// strings.
    /// </returns>
    /// <since>6.0</since>
    public static string ToString(string[] values)
    {
      using (var string_writer = new StringWriter())
      using (var writer = XmlWriter.Create(string_writer, NewXmlWriterSettings()))
      {
        writer.WriteStartElement("list");
        if (values != null)
        {
          foreach (var s in values.Where(s => !string.IsNullOrWhiteSpace(s)))
            writer.WriteElementString("value", s);
        }
        writer.WriteEndElement();
        writer.Close();
        var result = string_writer.ToString();
        return result;
      }
    }

    /// <summary>
    /// Attempts to convert a string to a key value string pair array.
    /// </summary>
    /// <param name="s">String to parse</param>
    /// <param name="value">
    /// Result will get copied here, if the string is null or empty then this
    /// will be an empty array, if there was an error parsing then this will be
    /// null otherwise it will be the string parsed as a key value string pair
    /// array.
    /// </param>
    /// <returns>
    /// Returns true if the string is not empty and properly formatted as a
    /// key value string pair list otherwise returns false.
    /// </returns>
    public static bool TryParseStringDictionary(string s, out KeyValuePair<string, string>[] value)
    {
      if (!IsStringDictionary(s))
      {
        value = string.IsNullOrWhiteSpace(s)
          ? new KeyValuePair<string, string>[0]
          : new[] {new KeyValuePair<string, string>(UI.Localization.LocalizeString("Unnamed", 36), s)};
        return false;
      }

      try
      {
        var doc = new XmlDocument();
        doc.LoadXml(s);
        var nodes = doc.FirstChild.SelectNodes("./value");
        var values = new List<KeyValuePair<string, string>>();
        if (null != nodes)
          values.AddRange(from XmlNode node in nodes
            let attr = null == node.Attributes ? null : node.Attributes["key"]
            where attr != null
            select new KeyValuePair<string, string>(attr.Value, node.InnerText));
        value = values.ToArray();
        return true;
      }
      catch
      {
        value = null;
        return false;
      }
    }

    /// <summary>
    /// Converts a key value string pair array to a properly formatted string
    /// dictionary XML string.
    /// </summary>
    /// <param name="value">
    /// List of string pairs to turn into a dictionary XML string.
    /// </param>
    /// <returns>
    /// Returns a properly formatted XML string that represents the string
    /// dictionary.
    /// </returns>
    public static string ToString(KeyValuePair<string, string>[] value)
    {
      using (var string_writer = new StringWriter())
      {
        using (var writer = XmlWriter.Create(string_writer, NewXmlWriterSettings()))
        {
          writer.WriteStartElement("dictionary");
          if (value != null)
            foreach (var item in value)
            {
              writer.WriteStartElement("value");
              writer.WriteAttributeString("key", item.Key);
              writer.WriteString(item.Value);
              writer.WriteEndElement();
            }
          writer.WriteEndElement();
          writer.Close();
          return string_writer.ToString();
        }
      }
    }

    /// <summary> XmlWriterSettings object used by the ToString(...) methods. </summary>
    /// <returns></returns>
    private static XmlWriterSettings NewXmlWriterSettings()
    {
      // 5 Dec 2017 S. Baer
      // Made Indent false (default). The string comparison tests for things like
      // MRU file list were always failing when the Indent was turned on. This
      // caused the settings file to be written every time when selecting a file
      // from the MRU list
      var xml_settings = new XmlWriterSettings
      {
        Encoding = Encoding.UTF8,
        //Indent = true,
        //IndentChars = "  ",
        OmitXmlDeclaration = true,
        NewLineOnAttributes = false,
        CloseOutput = false
      };
      return xml_settings;
    }

    /// <summary>
    /// Converts an enumerated value string (integer as string) to
    /// a enumerated value name.
    /// </summary>
    /// <param name="type">The enumerated type</param>
    /// <param name="intValueAsString">enumerated integer value as string</param>
    /// <param name="value">Output value, will be null on error</param>
    /// <returns>
    /// Returns true if the successfully converted or false if not.
    /// </returns>
    /// <since>6.0</since>
    public static bool TryParseEnum(Type type, string intValueAsString, out string value)
    {
      value = null;
      if (type == null || !type.IsEnum) return false;
      if (string.IsNullOrEmpty(intValueAsString)) return false;
      // Check to see if the Value is an integer
      int i;
      if (!int.TryParse(intValueAsString, SettingValue.ParseNumerStyle, SettingValue.ParseNumberFormat, out i))
        return false;
      // Check to see if the integer value maps to a enum value
      if (!Enum.IsDefined(type, i)) return false;
      // Get the name of the enum from the integer value
      value = Enum.GetName(type, i);
      return !string.IsNullOrEmpty(value);
    }

    /// <summary>
    /// Converts an enumerated value name to its integer
    /// equivalent.
    /// </summary>
    /// <param name="type">The enumerated type</param>
    /// <param name="enumValueName">Enumerated value name as string</param>
    /// <param name="value">Output value, will get set to -1 on error</param>
    /// <returns>
    /// Returns true if the successfully converted or false if not.
    /// </returns>
    /// <since>6.0</since>
    public static bool TryParseEnum(Type type, string enumValueName, out int value)
    {
      value = -1;
      if (type == null || !type.IsEnum) return false;
      if (string.IsNullOrEmpty(enumValueName)) return false;
      if (!Enum.IsDefined(type, enumValueName)) return false;
      var obj = Enum.Parse(type, enumValueName);
      value = (int) obj;
      return true;
    }

    /// <summary>
    /// Converts a double value to a string.
    /// </summary>
    /// <param name="value">double value</param>
    /// <returns>
    /// Returns the double value as a settings file formatted string.
    /// </returns>
    /// <since>6.10</since>
    public static string ToString(double value) => SettingValue.DoubleToString(value);

    /// <summary>
    /// Converts the string representation of a number to its double-precision
    /// floating-point number equivalent. A return value indicates whether the
    /// conversion succeeded or failed.
    /// system culture.
    /// </summary>
    /// <param name="s">A string containing a number to convert.</param>
    /// <param name="value">
    /// When this method returns, contains the double-precision floating-point
    /// number equivalent of the s parameter, if the conversion succeeded, or
    /// zero if the conversion failed. The conversion fails if the s parameter
    /// is null or Empty, is not a number in a valid format, or represents a 
    /// number less than MinValue or greater than MaxValue. This parameter is
    /// passed uninitialized; any value originally supplied in result will be
    /// overwritten.
    /// </param>
    /// <returns>
    /// Returns true if s was converted successfully; otherwise, false..
    /// </returns>
    /// <since>6.10</since>
    public static bool TryParseDouble(string s, out double value) => SettingValue.TryGetDouble(s, out value);
  }

  /// <summary> PersistentSettings contains a dictionary of these items. </summary>
  class SettingValue : ISerializable
  {
    /// <summary>
    /// ISerializable constructor.
    /// </summary>
    /// <param name="info">Serialization data.</param>
    /// <param name="context">Serialization stream.</param>
    protected SettingValue(SerializationInfo info, StreamingContext context)
    {
      m_value = info.GetString("value");
      m_default_value = info.GetString("default_value");
      Name = info.GetString ("name");
    }

    public SettingValue(SettingValue other)
    {
      if (null == other) return;
      m_parent = other.m_parent;
      m_value = other.m_value;
      m_default_value = other.m_default_value;
      Name = other.Name;
      ReadOnly = other.ReadOnly;
      Hidden = other.Hidden;
      RuntimeType = other.RuntimeType;
    }

    /// <summary> Constructor. </summary>
    /// <param name="parent">PersistentSettings collection this is being added to.</param>
    /// <param name="name">Current value name.</param>
    /// <param name="runtimeType">Value data type.</param>
    /// <param name="value">Current value string.</param>
    /// <param name="defaultValue">Default value string.</param>
    public SettingValue(PersistentSettings parent, string name, Type runtimeType, string value, string defaultValue)
    {
      m_parent = parent;
      if (!string.IsNullOrEmpty(value))
        m_value = value;
      if (!string.IsNullOrEmpty(defaultValue))
        m_default_value = defaultValue;
      RuntimeType = runtimeType;      
      Name = name;
    }

    public override string ToString() => m_value;

    /// <summary> ISerializable required method. </summary>
    /// <param name="info">Serialization data.</param>
    /// <param name="context">Serialization stream.</param>
    [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.SerializationFormatter)]
    void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
    {
      info.AddValue("value", m_value);
      info.AddValue("default_value", m_default_value);
      info.AddValue ("name", Name);
    }

    /// <summary>
    /// Copies values from another SettingsValue object. If the destination contains more than one item,
    /// assumes it is a string list and appends values from the source object that are not currently in
    /// the array.
    /// </summary>
    /// <param name="source">The source settings.</param>
    public void CopyFrom(SettingValue source)
    {
      if (null != source)
      {
        m_value = source.m_value;
        m_default_value = source.m_default_value;
        Hidden = source.Hidden;
        ReadOnly = source.ReadOnly;
        RuntimeType = source.RuntimeType;
      }
    }

    /// <summary>
    /// Determines if two SettingsValue have the same data. Does not compare default values.
    /// </summary>
    /// <param name="other">The other value.</param>
    /// <returns>true if this and other setting have the same value, without comparing the default. Otherwise, false.</returns>
    public bool ValuesAreEqual(SettingValue other)
    {
      return ValuesAreEqual(other, false);
    }

    /// <summary>
    /// Determines if two SettingsValues have the same data and optionally compares default data.
    /// </summary>
    /// <param name="other">The other value.</param>
    /// <param name="compareDefaults">true if the default value should be compared.</param>
    /// <returns>true if this and other setting have the same value, optionally comparing the default. Otherwise, false.</returns>
    public bool ValuesAreEqual(SettingValue other, bool compareDefaults)
    {
      if (null == other)
        return false;
      if (0 != string.Compare(m_value, other.m_value, StringComparison.Ordinal))
        return false;
      if (compareDefaults && 0 != string.Compare(m_default_value, other.m_default_value, StringComparison.Ordinal))
        return false;
      return true;
    }

    /// <summary>
    /// Sets either the current or default value string.
    /// </summary>
    /// <param name="isDefault">If true then the current value string is set otherwise the default value string is.</param>
    /// <param name="s">New value string.</param>
    /// <param name="setChangedFlag">
    /// If true then ChangedSinceSave will get set if the s is not exactly the,
    /// same as m_value.  If false then ChangedSinceSave will not get modified.
    /// Setting ChangedSinceSave to true causes the parent to get notified that
    /// a setting has changed and queue a write settings file request.
    /// 
    /// SetValue() may get called by a Get... method when using a default value
    /// which should not trigger a changed event.
    /// </param>
    public void SetValue(bool isDefault, string s, bool setChangedFlag)
    {
      if (isDefault)
        m_default_value = s;
      else if (!ReadOnly) // Do NOT allow editing of read-only variables
      {
        if (setChangedFlag && 0 != string.Compare(m_value, s, StringComparison.Ordinal))
          ChangedSinceSave = true;
        PersistentSettings.Service.SetValue(this, s);
      }
    }

    /// <summary>
    /// Gets the current or default value as requested.
    /// </summary>
    /// <param name="isDefault">If true then the default value string is returned otherwise the current value string is returned.</param>
    /// <returns>If isDefault is true then the default value string is returned otherwise the current value string is returned.</returns>
    public string GetValue(bool isDefault)
    {
      return isDefault ? m_default_value : PersistentSettings.Service.GetPlistValue(this);
    }

    /// <summary>
    /// Compare current and default values and return true if they are identical, compare is case sensitive.
    /// </summary>
    public bool ValueSameAsDefault
    {
      get { return (0 == string.Compare(m_value, m_default_value, StringComparison.Ordinal)); }
    }

    /// <summary>
    /// Compare current and default values and return true if they differ, compare is case sensitive.
    /// </summary>
    public bool ValueDifferentThanDefault
    {
      get { return (!ValueSameAsDefault); }
    }

    public static bool TryGetGuid(string str, out Guid value) => Guid.TryParse(str, out value);
    public bool TryGetGuid(bool isDefault, out Guid value)
    {
      RuntimeType = typeof(Guid);
      return TryGetGuid(GetValue(isDefault), out value);
    }

    public static bool TryGetBool(string str, out bool value)
    {
      if (bool.TryParse(str, out value))
        return true;
      if (!int.TryParse(str, out int i))
        return false;
      value = i != 0;
      return true;
    }

    public bool TryGetBool(bool isDefault, out bool value)
    {
      RuntimeType = typeof(bool);
      return TryGetBool(GetValue(isDefault), out value);
    }

    public static bool TryGetByte(string str, out byte value) => byte.TryParse(str, ParseNumerStyle, ParseNumberFormat, out value);
    public bool TryGetByte(bool isDefault, out byte value)
    {
      RuntimeType = typeof(byte);
      return TryGetByte(GetValue(isDefault), out value);
    }

    public static bool TryGetInteger(string str, out int value) => int.TryParse(str, ParseNumerStyle, ParseNumberFormat, out value);
    public bool TryGetInteger(bool isDefault, out int value)
    {
      RuntimeType = typeof(int);
      return TryGetInteger(GetValue(isDefault), out value);
    }

    public static bool TryGetUnsignedInteger(string str, out uint value) => uint.TryParse(str, ParseNumerStyle, ParseNumberFormat, out value);
    public bool TryGetUnsignedInteger(bool isDefault, out uint value)
    {
      RuntimeType = typeof(uint);
      return TryGetUnsignedInteger(GetValue(isDefault), out value);
    }

    public static bool TryGetDouble(string str, out double value) => double.TryParse(str, ParseNumerStyle, ParseNumberFormat, out value);
    public bool TryGetDouble(bool isDefault, out double value)
    {
      RuntimeType = typeof(double);
      return TryGetDouble(GetValue(isDefault), out value);
    }

    public static NumberStyles ParseNumerStyle => NumberStyles.Any;
    //15 Feb 2018 S. Baer (RH-43828)
    // Always use CultureInfo.InvariantCulture instead of attempting to construct a 1033 CultureInfo.
    // A 1033 CultureInfo on some people's computers appear to be able to have their OS tweaked in a
    // way that commas are still used as the separator instead of periods.
    public static CultureInfo ParseCulture => CultureInfo.InvariantCulture;
    public static NumberFormatInfo ParseNumberFormat => ParseCulture.NumberFormat;


    public static bool TryGetChar(string str, out char value) => char.TryParse(str, out value);
    public bool TryGetChar(bool isDefault, out char value)
    {
      RuntimeType = typeof(char);
      return TryGetChar(GetValue(isDefault), out value);
    }

    public bool TryGetString(bool isDefault, out string value)
    {
      if (RuntimeType == null)
        RuntimeType = typeof(string);
      value = GetValue(isDefault);
      return true;
    }

    /// <summary>
    /// I was going to use Path.PathSeparator, ';' which works when specifying a path but is a valid file name character so
    /// it does not work in a file name list, the '|' character is in both the Path.GetInvalidFileNameChars() and 
    /// Path.GetInvalidPathChars() list of characters so I went ahead and used it for now.
    /// </summary>
    public static readonly char StringListSeparator = '|';

    /// <summary>
    /// Gets the <see cref="StringListSeparator"/> value in an array.
    /// </summary>
    public static char[] StringListSeparatorAsArray
    {
      get { return new char[] {StringListSeparator}; }
    }

    public static readonly string StringListRootKey = "%root%";

    public static bool TryGetStringDictionary(string str, out KeyValuePair<string, string>[] value) => PersistentSettingsConverter.TryParseStringDictionary(str, out value);
    public bool TryGetStringDictionary(bool isDefault, out KeyValuePair<string, string>[] value) => TryGetStringDictionary(GetValue(isDefault), out value);

    internal static bool TryGetStringList(SettingValue settingValue, string str, string rootString, out string[] value)
    {
      return (PersistentSettingsConverter.IsStringList(str)
              ? PersistentSettingsConverter.TryParseStringList(str, out value)
              : TryParseLegacyStringList(settingValue, str, rootString, out value));
    }

    public bool TryGetStringList(bool isDefault, string rootString, out string[] value) => TryGetStringList(this, GetValue(isDefault), rootString, out value);

    private static bool TryParseLegacyStringList(SettingValue settingValue, string s, string rootString, out string[] value)
    {
      value = null;
      if (!string.IsNullOrEmpty(s))
      {
        var list_seporator = new string(StringListSeparatorAsArray);
        s = s.Replace(StringListSeparator + StringListRootKey + StringListSeparator,
          string.IsNullOrEmpty(rootString) ? list_seporator : list_seporator + rootString + list_seporator);
        s = s.Replace(StringListSeparator + StringListRootKey,
          string.IsNullOrEmpty(rootString) ? string.Empty : list_seporator + rootString);
        s = s.Replace(StringListRootKey + StringListSeparator,
          string.IsNullOrEmpty(rootString) ? string.Empty : rootString + list_seporator);
        s = s.Replace(StringListRootKey, string.IsNullOrEmpty(rootString) ? string.Empty : rootString);
        value = s.Split(StringListSeparatorAsArray);
        if (settingValue != null)
          settingValue.RuntimeType = value.GetType();
      }
      return true;
    }

    public static bool TryGetDate(string str, out DateTime value) => DateTime.TryParse(str, SettingValue.ParseCulture, DateTimeStyles.None, out value);
    public bool TryGetDate(bool isDefault, out DateTime value)
    {
      var success = TryGetDate(GetValue(isDefault), out value);
      RuntimeType = value.GetType();
      return success;
    }

    // https://mcneel.myjetbrains.com/youtrack/issue/RH-45736
    // 30 March 2017 John Morse
    // Added TryGetNumberList method to the settings value class and modified
    // all methods which try to retrieve and parse list of numbers like points,
    // colors or vectors to use the new method.  The method will make sure there
    // is the appropriate number of tokens and that the string tokens are not
    // empty or null.
    internal static bool TryGetNumberList(string str, int requiredListLenght, out string[] stringList)
    {
      // Split the string into number parts
      stringList = str?.Split(',');
      // If the resulting array is not the correct size
      if (stringList?.Length != requiredListLenght)
        return false;
      // The array is the correct size make sure that each item
      // is non null
      for (var i = 0; i < requiredListLenght; i++)
        if (string.IsNullOrEmpty(stringList[i]))
          return false;
      // Has the required number of items of not empty strings
      return true;
    }

    public bool TryGetColor(bool isDefault, out Color value)
    {
      var success = TryGetColor(GetValue(isDefault), out value);
      RuntimeType = value.GetType();
      return success;
    }

    public static bool TryGetColor(string str, out Color value)
    {
      value = Color.Empty;
      if (!TryGetNumberList(str, 4, out string[] argb))
        return false;

      Int32 alpha, red, green, blue;
      if (Int32.TryParse(argb[0], SettingValue.ParseNumerStyle, SettingValue.ParseNumberFormat, out alpha)
          && Int32.TryParse(argb[1], SettingValue.ParseNumerStyle, SettingValue.ParseNumberFormat, out red)
          && Int32.TryParse(argb[2], SettingValue.ParseNumerStyle, SettingValue.ParseNumberFormat, out green)
          && Int32.TryParse(argb[3], SettingValue.ParseNumerStyle, SettingValue.ParseNumberFormat, out blue))
      {
        try
        {
          value = Color.FromArgb(alpha, red, green, blue);
        }
        catch (ArgumentException)
        {
          return false;
        }
        return true;
      }

      return false;
    }

    public bool TryGetPoint3d(bool isDefault, out Point3d value)
    {
      var success = TryGetPoint3d(GetValue(isDefault), out value);
      RuntimeType = value.GetType();
      return success;
    }

    public bool TryGetPoint3d(string str, out Point3d value)
    {
      value = Point3d.Unset;
      RuntimeType = value.GetType();

      if (!TryGetNumberList(str, 3, out string[] point))
        return false;

      double x, y, z;
      if (double.TryParse(point[0], SettingValue.ParseNumerStyle, SettingValue.ParseNumberFormat, out x)
          && double.TryParse(point[1], SettingValue.ParseNumerStyle, SettingValue.ParseNumberFormat, out y)
          && double.TryParse(point[2], SettingValue.ParseNumerStyle, SettingValue.ParseNumberFormat, out z))
      {
        value = new Point3d(x, y, z);
        return true;
      }

      return false;
    }

    public bool TryGetSize(bool isDefault, out Size value)
    {
      var success = TryGetSize(GetValue(isDefault), out value);
      RuntimeType = value.GetType();
      return success;
    }

    public static bool TryGetSize(string str, out Size value)
    {
      value = Size.Empty;

      if (!TryGetNumberList(str, 2, out string[] size))
        return false;

      int width, height;
      if (int.TryParse(size[0], SettingValue.ParseNumerStyle, SettingValue.ParseNumberFormat, out width)
          && int.TryParse(size[1], SettingValue.ParseNumerStyle, SettingValue.ParseNumberFormat, out height))
      {
        value = new Size(width, height);
        return true;
      }

      return false;
    }

    public bool TryGetPoint(bool isDefault, out System.Drawing.Point value)
    {
      var success = TryGetPoint(GetValue(isDefault), out value);
      RuntimeType = value.GetType();
      return success;
    }

    public static bool TryGetPoint(string str, out System.Drawing.Point value)
    {
      Size sz;
      bool rc = TryGetSize(str, out sz);
      value = new System.Drawing.Point(sz.Width, sz.Height);
      return rc;
    }

    public bool TryGetRectangle(bool isDefault, out Rectangle value)
    {
      var success = TryGetRectangle(GetValue(isDefault), out value);
      RuntimeType = value.GetType();
      return success;
    }

    public static bool TryGetRectangle(string str, out Rectangle value)
    {
      value = Rectangle.Empty;

      if (!TryGetNumberList(str, 4, out string[] rect))
        return false;

      int x, y, width, height;

      if (int.TryParse(rect[0], SettingValue.ParseNumerStyle, SettingValue.ParseNumberFormat, out x)
          && int.TryParse(rect[1], SettingValue.ParseNumerStyle, SettingValue.ParseNumberFormat, out y)
          && int.TryParse(rect[2], SettingValue.ParseNumerStyle, SettingValue.ParseNumberFormat, out width)
          && int.TryParse(rect[3], SettingValue.ParseNumerStyle, SettingValue.ParseNumberFormat, out height))
      {
        value = new Rectangle(x, y, width, height);
        return true;
      }

      return false;
    }

    public static bool TryGetRectangleTLRB(string str, out Rectangle value)
    {
      value = Rectangle.Empty;

      if (!TryGetNumberList(str, 4, out string[] rect))
        return false;

      int l, t, b, r;

      if (int.TryParse(rect[0], SettingValue.ParseNumerStyle, SettingValue.ParseNumberFormat, out l)
          && int.TryParse(rect[1], SettingValue.ParseNumerStyle, SettingValue.ParseNumberFormat, out t)
          && int.TryParse(rect[2], SettingValue.ParseNumerStyle, SettingValue.ParseNumberFormat, out b)
          && int.TryParse(rect[3], SettingValue.ParseNumerStyle, SettingValue.ParseNumberFormat, out r))
      {
        value = Rectangle.FromLTRB(l, t, b, r);
        return true;
      }

      return false;
    }

    public static string GuidToString(Guid value) => value.ToString();

    public void SetGuid(bool isDefault, Guid value, EventHandler<PersistentSettingsEventArgs<Guid>> validator,
      bool setChangedFlag)
    {
      if (validator != null)
      {
        Guid old_value;
        TryGetGuid(isDefault, out old_value);
        var a = new PersistentSettingsEventArgs<Guid>(old_value, value);
        validator(this, a);
        if (a.Cancel)
          return;
        value = a.NewValue;
      }
      RuntimeType = value.GetType();
      SetValue(isDefault, GuidToString(value), setChangedFlag);
    }

    public static string BoolToString(bool value) => value.ToString(SettingValue.ParseNumberFormat);

    public void SetBool(bool isDefault, bool value, EventHandler<PersistentSettingsEventArgs<bool>> validator,
      bool setChangedFlag)
    {
      if (validator != null)
      {
        bool old_value;
        TryGetBool(isDefault, out old_value);
        var a = new PersistentSettingsEventArgs<bool>(old_value, value);
        validator(this, a);
        if (a.Cancel)
          return;
        value = a.NewValue;
      }
      RuntimeType = value.GetType();
      SetValue(isDefault, BoolToString(value), setChangedFlag);
    }

    public void SetByte(bool isDefault, byte value, EventHandler<PersistentSettingsEventArgs<byte>> validator,
      bool setChangedFlag)
    {
      if (validator != null)
      {
        byte old_value;
        TryGetByte(isDefault, out old_value);
        var a = new PersistentSettingsEventArgs<byte>(old_value, value);
        validator(this, a);
        if (a.Cancel)
          return;
        value = a.NewValue;
      }
      RuntimeType = value.GetType();
      SetValue(isDefault, value.ToString(SettingValue.ParseNumberFormat), setChangedFlag);
    }

    public static string IntegerToString(int value) => value.ToString(SettingValue.ParseNumberFormat);

    public void SetInteger(bool isDefault, int value, EventHandler<PersistentSettingsEventArgs<int>> validator,
      bool setChangedFlag)
    {
      if (validator != null)
      {
        int old_value;
        TryGetInteger(isDefault, out old_value);
        var a = new PersistentSettingsEventArgs<int>(old_value, value);
        validator(this, a);
        if (a.Cancel)
          return;
        value = a.NewValue;
      }
      RuntimeType = value.GetType();
      SetValue(isDefault, IntegerToString(value), setChangedFlag);
    }

    public bool TryGetEnum<T>(bool isDefault, out T value) where T : struct
    {
      RuntimeType = typeof(T);
      return Enum.TryParse<T>(GetValue(isDefault), false, out value);
    }

    public void SetEnum<T>(bool isDefault, T value, EventHandler<PersistentSettingsEventArgs<T>> validator,
      bool setChangedFlag) where T : struct, IConvertible
    {
      if (!typeof(T).IsEnum) throw new ArgumentException("type must be an Enumeration");
      if (validator != null)
      {
        T old_value;
        TryGetEnum<T>(isDefault, out old_value);
        var a = new PersistentSettingsEventArgs<T>(old_value, value);
        validator(this, a);
        if (a.Cancel)
          return;
        value = a.NewValue;
      }
      RuntimeType = value.GetType();
      var int_value = Convert.ToInt32(value);
      SetValue(isDefault, int_value.ToString(SettingValue.ParseNumberFormat), setChangedFlag);
    }

    public void SetUnsignedInteger(bool isDefault, uint value, EventHandler<PersistentSettingsEventArgs<uint>> validator,
      bool setChangedFlag)
    {
      if (validator != null)
      {
        uint old_value;
        TryGetUnsignedInteger(isDefault, out old_value);
        var a = new PersistentSettingsEventArgs<uint>(old_value, value);
        validator(this, a);
        if (a.Cancel)
          return;
        value = a.NewValue;
      }
      RuntimeType = value.GetType();
      SetValue(isDefault, value.ToString(SettingValue.ParseNumberFormat), setChangedFlag);
    }

    public static string DoubleToString(double value) => value.ToString(SettingValue.ParseNumberFormat);

    public void SetDouble(bool isDefault, double value, EventHandler<PersistentSettingsEventArgs<double>> validator,
      bool setChangedFlag)
    {
      if (validator != null)
      {
        double old_value;
        TryGetDouble(isDefault, out old_value);
        var a = new PersistentSettingsEventArgs<double>(old_value, value);
        validator(this, a);
        if (a.Cancel)
          return;
        value = a.NewValue;
      }
      RuntimeType = value.GetType();
      SetValue(isDefault, DoubleToString(value), setChangedFlag);
    }

    public void SetChar(bool isDefault, char value, EventHandler<PersistentSettingsEventArgs<char>> validator,
      bool setChangedFlag)
    {
      if (validator != null)
      {
        char old_value;
        TryGetChar(isDefault, out old_value);
        var a = new PersistentSettingsEventArgs<char>(old_value, value);
        validator(this, a);
        if (a.Cancel)
          return;
        value = a.NewValue;
      }
      RuntimeType = value.GetType();
      SetValue(isDefault, value.ToString(SettingValue.ParseNumberFormat), setChangedFlag);
    }

    public void SetString(bool isDefault, string value, EventHandler<PersistentSettingsEventArgs<string>> validator,
      bool setChangedFlag)
    {
      if (validator != null)
      {
        string old_value;
        TryGetString(isDefault, out old_value);
        var a = new PersistentSettingsEventArgs<string>(old_value, value);
        validator(this, a);
        if (a.Cancel)
          return;
        value = a.NewValue;
      }
      if (RuntimeType == null)
        RuntimeType = value == null ? null : value.GetType();
      SetValue(isDefault, value, setChangedFlag);
    }

    public void SetStringDictionary(bool isDefault, KeyValuePair<string, string>[] value,
      EventHandler<PersistentSettingsEventArgs<KeyValuePair<string, string>[]>> validator, bool setChangedFlag)
    {
      if (validator != null)
      {
        KeyValuePair<string, string>[] old_value;
        TryGetStringDictionary(isDefault, out old_value);
        var a = new PersistentSettingsEventArgs<KeyValuePair<string, string>[]>(old_value, value);
        validator(this, a);
        if (a.Cancel)
          return;
        value = a.NewValue;
      }
      var new_value = PersistentSettingsConverter.ToString(value);
      if (null != value)
        RuntimeType = value.GetType();
      SetValue(isDefault, new_value, setChangedFlag);
    }

    public void SetStringList(bool isDefault, string[] value,
      EventHandler<PersistentSettingsEventArgs<string[]>> validator, bool setChangedFlag)
    {
      if (validator != null)
      {
        string[] old_value;
        TryGetStringList(isDefault, string.Empty, out old_value);
        var a = new PersistentSettingsEventArgs<string[]>(old_value, value);
        validator(this, a);
        if (a.Cancel)
          return;
        value = a.NewValue;
      }
      var new_value = PersistentSettingsConverter.ToString(value);
      if (null != value)
        RuntimeType = value.GetType();
      SetValue(isDefault, new_value, setChangedFlag);
    }

    public void SetDate(bool isDefault, DateTime value, EventHandler<PersistentSettingsEventArgs<DateTime>> validator,
      bool setChangedFlag)
    {
      if (validator != null)
      {
        DateTime old_value;
        TryGetDate(isDefault, out old_value);
        var a = new PersistentSettingsEventArgs<DateTime>(old_value, value);
        validator(this, a);
        if (a.Cancel)
          return;
        value = a.NewValue;
      }
      RuntimeType = value.GetType();
      SetValue(isDefault, value.ToString("F", SettingValue.ParseCulture), setChangedFlag);
    }

    public static string ColorToString(Color value) => string.Format(
      SettingValue.ParseCulture,
      "{0},{1},{2},{3}",
      value.A.ToString(SettingValue.ParseNumberFormat),
      value.R.ToString(SettingValue.ParseNumberFormat),
      value.G.ToString(SettingValue.ParseNumberFormat),
      value.B.ToString(SettingValue.ParseNumberFormat));

    public void SetColor(bool isDefault, Color value, EventHandler<PersistentSettingsEventArgs<Color>> validator,
      bool setChangedFlag)
    {
      if (validator != null)
      {
        Color old_value;
        TryGetColor(isDefault, out old_value);
        var a = new PersistentSettingsEventArgs<Color>(old_value, value);
        validator(this, a);
        if (a.Cancel)
          return;
        value = a.NewValue;
      }
      RuntimeType = value.GetType();
      if (value.IsEmpty)
        SetValue(isDefault, string.Empty, setChangedFlag);
      else
        SetValue(isDefault, ColorToString(value), setChangedFlag);
    }

    public static string Point3dToString(Point3d value) => string.Format(
      SettingValue.ParseCulture,
      "{0},{1},{2}",
      value.m_x.ToString(SettingValue.ParseNumberFormat),
      value.m_y.ToString(SettingValue.ParseNumberFormat),
      value.m_z.ToString(SettingValue.ParseNumberFormat));

    public void SetPoint3d(bool isDefault, Point3d value, EventHandler<PersistentSettingsEventArgs<Point3d>> validator,
      bool setChangedFlag)
    {
      if (validator != null)
      {
        Point3d old_value;
        TryGetPoint3d(isDefault, out old_value);
        var a = new PersistentSettingsEventArgs<Point3d>(old_value, value);
        validator(this, a);
        if (a.Cancel)
          return;
        value = a.NewValue;
      }
      RuntimeType = value.GetType();
      SetValue(isDefault, Point3dToString(value), setChangedFlag);
    }

    public static string RectangleToString(Rectangle value) => string.Format(
      SettingValue.ParseCulture,
      "{0},{1},{2},{3}",
      value.Left.ToString(SettingValue.ParseNumberFormat),
      value.Top.ToString(SettingValue.ParseNumberFormat),
      value.Width.ToString(SettingValue.ParseNumberFormat),
      value.Height.ToString(SettingValue.ParseNumberFormat));

    public static string RectangleToTLRBString(Rectangle value) => string.Format(
      SettingValue.ParseCulture,
      "{0},{1},{2},{3}",
      value.Left.ToString(SettingValue.ParseNumberFormat),
      value.Top.ToString(SettingValue.ParseNumberFormat),
      value.Right.ToString(SettingValue.ParseNumberFormat),
      value.Bottom.ToString(SettingValue.ParseNumberFormat));

    public void SetRectangle(bool isDefault, Rectangle value,
      EventHandler<PersistentSettingsEventArgs<Rectangle>> validator, bool setChangedFlag)
    {
      if (validator != null)
      {
        Rectangle old_value;
        TryGetRectangle(isDefault, out old_value);
        var a = new PersistentSettingsEventArgs<Rectangle>(old_value, value);
        validator(this, a);
        if (a.Cancel)
          return;
        value = a.NewValue;
      }
      RuntimeType = value.GetType();
      SetValue(isDefault, RectangleToString(value), setChangedFlag);
    }

    public static string SizeToString(Size value) => string.Format(
      SettingValue.ParseCulture,
      "{0},{1}",
      value.Width.ToString(SettingValue.ParseNumberFormat),
      value.Height.ToString(SettingValue.ParseNumberFormat));

    public void SetSize(bool isDefault, Size value, EventHandler<PersistentSettingsEventArgs<Size>> validator,
      bool setChangedFlag)
    {
      if (validator != null)
      {
        Size old_value;
        TryGetSize(isDefault, out old_value);
        var a = new PersistentSettingsEventArgs<Size>(old_value, value);
        validator(this, a);
        if (a.Cancel)
          return;
        value = a.NewValue;
      }
      RuntimeType = value.GetType();
      SetValue(isDefault, SizeToString(value), setChangedFlag);
    }

    public static string PointToString(System.Drawing.Point value) => string.Format(
      SettingValue.ParseCulture,
      "{0},{1}",
      value.X.ToString(SettingValue.ParseNumberFormat),
      value.Y.ToString(SettingValue.ParseNumberFormat));
    
    public void SetPoint(bool isDefault, System.Drawing.Point value,
      EventHandler<PersistentSettingsEventArgs<System.Drawing.Point>> validator, bool setChangedFlag)
    {
      if (validator != null)
      {
        System.Drawing.Point old_value;
        TryGetPoint(isDefault, out old_value);
        var a = new PersistentSettingsEventArgs<System.Drawing.Point>(old_value, value);
        validator(this, a);
        if (a.Cancel)
          return;
        value = a.NewValue;
      }
      RuntimeType = value.GetType();
      SetValue(isDefault, PointToString(value), setChangedFlag);
    }

    private readonly PersistentSettings m_parent;
    public PersistentSettings Parent => m_parent;
    private string m_value;
    public string ValueString
    {
      get => m_value;
      set => m_value = value;
    }
    private string m_default_value;
    public Type RuntimeType { get; internal set; }
    public bool ReadOnly { get; set; }
    public bool Hidden { get; set; }
    public string Name { get; }

    public bool ChangedSinceSave
    {
      get { return m_changed_since_save; }
      set
      {
        m_changed_since_save = value;
        if (value)
        {
          // Add changed event handler
          m_parent?.Parent?.OnSettingChangedSinceSave();
        }
      }
    }

    private bool m_changed_since_save;
  }

  /// <summary>
  /// Event argument passed to the <see cref="Rhino.PlugIns.PlugIn.SettingsSaved"/> event.
  /// </summary>
  public class PersistentSettingsSavedEventArgs : EventArgs
  {
    internal PersistentSettingsSavedEventArgs(bool savedByThisRhino, PlugInSettings settings)
    {
      SavedByThisRhino = savedByThisRhino;
      m_plug_in_settings = settings;
    }

    /// <summary>
    /// Will be true if this instance of Rhino is writing the settings file
    /// or false if a different instance of Rhino has modified the settings
    /// file.
    /// </summary>
    /// <since>6.0</since>
    public bool SavedByThisRhino { get; private set; }

    /// <summary>
    /// The old PlugIn settings
    /// </summary>
    /// <seealso cref="Rhino.PlugIns.PlugIn.Settings"/>
    /// <seealso cref="Rhino.PlugIns.PlugIn.SettingsSaved"/>
    /// <since>6.0</since>
    public PersistentSettings PlugInSettings
    {
      get
      {
        var settings = m_plug_in_settings.PluginSettings;
        return settings;
      }
    }

    /// <summary>
    /// The new command settings
    /// </summary>
    /// <param name="englishCommandName">
    /// English command to find settings for
    /// </param>
    /// <returns></returns>
    /// <since>6.0</since>
    public PersistentSettings CommandSettings(string englishCommandName)
    {
      var settings = m_plug_in_settings.CommandSettings(englishCommandName);
      return settings;
    }

    private readonly PlugInSettings m_plug_in_settings;
  }

  /// <summary>
  /// Represents event data that is passed as state in persistent settings events.
  /// </summary>
  public abstract class PersistentSettingsEventArgs : EventArgs
  {
    protected PersistentSettingsEventArgs()
    {
      Cancel = false;
    }

    /// <since>5.0</since>
    public bool Cancel { get; set; }
  }

  /// <summary>
  /// Represents the persistent settings modification event arguments.
  /// </summary>
  /// <typeparam name="T">The type of the current and new setting that is being modified.</typeparam>
  public class PersistentSettingsEventArgs<T> : PersistentSettingsEventArgs
  {
    public PersistentSettingsEventArgs(T currentValue, T newValue)
    {
      CurrentValue = currentValue;
      NewValue = newValue;
    }

    public T CurrentValue { get; set; }
    public T NewValue { get; private set; }
  }


  /// <summary>
  /// A dictionary of SettingValue items.
  /// </summary>
  [Serializable]
  public class PersistentSettings : ISerializable
  {
    private static Runtime.ISettingsService g_service_implementation;
    /// <since>6.1</since>
    internal static Runtime.ISettingsService Service
    {
      get
      {
        if (g_service_implementation != null)
          return g_service_implementation;
        g_service_implementation = Runtime.HostUtils.GetPlatformService<Runtime.ISettingsService>();
        return (g_service_implementation ?? (g_service_implementation = Runtime.FileSettingsService.Service));
      }
    }

    readonly Dictionary<string, SettingValue> m_settings;
    readonly Dictionary<string, Delegate> m_settings_validators;
    readonly Dictionary<string, PersistentSettings> m_children = new Dictionary<string, PersistentSettings>();
    readonly object m_lock_settings = new object();

    internal Dictionary<string, SettingValue> Settings => m_settings;
    internal Dictionary<string, PersistentSettings> Children => m_children;

    /// <summary>
    /// Check to see if the settings dictionary contains the specified key
    /// </summary>
    /// <returns><c>true</c>, if key was contained, <c>false</c> otherwise.</returns>
    /// <param name="key">Key.</param>
    internal bool ContainsKey(string key) => m_settings.ContainsKey(key);

    /// <summary>
    /// This will get set when a settings item is deleted, can't rely on
    /// SettingsValue.ChangedSinceSave flag since the setting is not in the
    /// dictionary.
    /// </summary>
    internal bool ItemDeletedSinceSave { get; set; }

    /// <summary>
    /// Used to flag a settings instance as being reset, tells Rhino to just delete
    /// the settings file when closing so everything will revert to default values.
    /// </summary>
    internal bool ResettingDontWrite { get; set; }

    /// <summary>
    /// If false then values will appear in the EditOptions window
    /// </summary>
    /// <since>6.0</since>
    public bool HiddenFromUserInterface { get; set; }

    /// <summary>
    /// Call this method to get a nested settings <see cref="PersistentSettings"/>
    /// instance, will throw a <see cref="KeyNotFoundException"/> exception if
    /// the key does not exist.
    /// </summary>
    /// <param name="key">Key name</param>
    /// <exception cref="KeyNotFoundException">Thrown if the key does not exist.</exception>
    /// <returns>
    /// Returns persistent settings for the specified key or throws an
    /// exception if the key is invalid.
    /// </returns>
    /// <since>6.0</since>
    public PersistentSettings GetChild(string key)
    {
      PersistentSettings rc;
      if (TryGetChild(key, out rc))
        return rc;
      throw new KeyNotFoundException(key);
    }

    /// <summary>
    /// Call this method to get a nested settings <see cref="PersistentSettings"/>
    /// instance, will return true if the key exists and value was set
    /// otherwise; will return false and value will be set to null.
    /// </summary>
    /// <param name="key">[in] Key name</param>
    /// <param name="value">
    /// [out] Will be set the child settings if the key is valid otherwise
    /// it will be null.
    /// </param>
    /// <returns>
    /// Returns true if the key exists and value was set otherwise; returns
    /// false.
    /// </returns>
    /// <since>6.0</since>
    public bool TryGetChild(string key, out PersistentSettings value)
    {
      var success = m_children.TryGetValue(key, out value);
      return success;
    }

    /// <summary>
    /// Gets a collection containing the keys in the settings dictionary.
    /// </summary>
    /// <since>6.0</since>
    public ICollection<string> Keys
    {
      get
      {
        if (SyncKeys)
          PersistentSettings.Service.EnumeratePlistKeys(this, m_settings);
        return m_settings.Keys;
      }
    }
    internal bool SyncKeys { get; set; } = Runtime.HostUtils.RunningOnOSX;

    /// <summary>
    /// Gets a collection containing the keys in the settings dictionary.
    /// </summary>
    /// <since>6.0</since>
    public ICollection<string> ChildKeys
    {
      get
      {
        if (SyncKeys)
          PersistentSettings.Service.EnumeratePlistChildKeys(this, m_children);
        return m_children.Keys;
      }
    }
    internal bool SyncChildKeys { get; set; } = Runtime.HostUtils.RunningOnOSX;

    /// <summary>
    /// Get the type of the last value passed to Set... or Get... for the
    /// specified setting.
    /// </summary>
    /// <param name="key">
    /// Key name for which to search.
    /// </param>
    /// <param name="type">
    /// Type of the last value passed to Set... or Get... for the specified
    /// setting.
    /// </param>
    /// <returns></returns>
    /// <since>6.0</since>
    public bool TryGetSettingType(string key, out Type type)
    {
      SettingValue value;
      var success = m_settings.TryGetValue(key, out value);
      type = success ? value.RuntimeType : null;
      return success;
    }

    /// <summary>
    /// Gets the type of the last value passed to Set... or Get... for the
    /// specified setting.
    /// </summary>
    /// <param name="key">
    /// Key name for which to search.
    /// </param>
    /// <returns>
    /// Type of the last value passed to Set... or Get... for the specified
    /// setting.
    /// </returns>
    /// <exception cref="KeyNotFoundException">
    /// A KeyNotFoundException exception is thrown if the key is not found in
    /// the settings dictionary.
    /// </exception>
    /// <since>6.0</since>
    public Type GetSettingType(string key)
    {
      Type result;
      if (!TryGetSettingType(key, out result))
        throw new KeyNotFoundException(key);
      return result;
    }

    /// <summary>
    /// Values read from all users settings files will be marked as read-only
    /// which will cause any future calls to Set... to fail.
    /// </summary>
    /// <param name="key">
    /// Key name for which to search.
    /// </param>
    /// <param name="value">
    /// Value will be true if the setting is read-only otherwise false.
    /// setting.
    /// </param>
    /// <returns></returns>
    /// <since>6.0</since>
    public bool TryGetSettingIsReadOnly(string key, out bool value)
    {
      SettingValue setting_value;
      var success = m_settings.TryGetValue(key, out setting_value);
      value = (success && setting_value.ReadOnly);
      return success;
    }

    /// <summary>
    /// Values read from all users settings files will be marked as read-only
    /// which will cause any future calls to Set... to fail.
    /// </summary>
    /// <param name="key">
    /// Key name for which to search.
    /// </param>
    /// <returns>
    /// Returns true if the setting is read-only otherwise false.
    /// </returns>
    /// <exception cref="KeyNotFoundException">
    /// A KeyNotFoundException exception is thrown if the key is not found in
    /// the settings dictionary.
    /// </exception>
    /// <since>6.0</since>
    public bool GetSettingIsReadOnly(string key)
    {
      bool result;
      if (!TryGetSettingIsReadOnly(key, out result))
        throw new KeyNotFoundException(key);
      return result;
    }

    /// <summary>
    /// Values read from all users settings files will be marked as read-only
    /// which will cause any future calls to Set... to fail.
    /// </summary>
    /// <param name="key">
    /// Key name for which to search.
    /// </param>
    /// <param name="value">
    /// Value will be true if the setting is read-only otherwise false.
    /// setting.
    /// </param>
    /// <returns></returns>
    /// <since>6.0</since>
    public bool TryGetSettingIsHiddenFromUserInterface(string key, out bool value)
    {
      return TryGetSettingIsHiddenFromUserInterface(key, out value, null);
    }

    /// <summary>
    /// Values read from all users settings files will be marked as read-only
    /// which will cause any future calls to Set... to fail.
    /// </summary>
    /// <param name="key">
    /// Key name for which to search.
    /// </param>
    /// <param name="value">
    /// Value will be true if the setting is read-only otherwise false.
    /// setting.
    /// </param>
    /// <param name="legacyKeyList">
    /// </param>
    /// <returns></returns>
    /// <since>6.0</since>
    public bool TryGetSettingIsHiddenFromUserInterface(string key, out bool value, IEnumerable<string> legacyKeyList)
    {
      var sv = TryGetSettingsValue(null, key, legacyKeyList);
      value = (sv != null && sv.Hidden);
      return (sv != null);
    }

    /// <summary>
    /// Values read from all users settings files will be marked as read-only
    /// which will cause any future calls to Set... to fail.
    /// </summary>
    /// <param name="key">
    /// Key name for which to search.
    /// </param>
    /// <returns>
    /// Returns true if the setting is read-only otherwise false.
    /// </returns>
    /// <exception cref="KeyNotFoundException">
    /// A KeyNotFoundException exception is thrown if the key is not found in
    /// the settings dictionary.
    /// </exception>
    /// <since>6.0</since>
    public bool GetSettingIsHiddenFromUserInterface(string key)
    {
      return GetSettingIsHiddenFromUserInterface(key, null);
    }

    /// <summary>
    /// Values read from all users settings files will be marked as read-only
    /// which will cause any future calls to Set... to fail.
    /// </summary>
    /// <param name="key">
    /// Key name for which to search.
    /// </param>
    /// <param name="legacyKeyList">
    /// </param>
    /// <returns>
    /// Returns true if the setting is read-only otherwise false.
    /// </returns>
    /// <exception cref="KeyNotFoundException">
    /// A KeyNotFoundException exception is thrown if the key is not found in
    /// the settings dictionary.
    /// </exception>
    /// <since>6.0</since>
    public bool GetSettingIsHiddenFromUserInterface(string key, IEnumerable<string> legacyKeyList)
    {
      bool result;
      if (!TryGetSettingIsHiddenFromUserInterface(key, out result, legacyKeyList))
        throw new KeyNotFoundException(key);
      return result;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="key"></param>
    /// <since>6.0</since>
    public void HideSettingFromUserInterface(string key)
    {
      SettingValue setting_value;
      if (!m_settings.TryGetValue(key, out setting_value))
        throw new KeyNotFoundException(key);
      setting_value.Hidden = true;
    }

    /// <summary>
    /// Call this method to add a new child key, if the key is exists then the
    /// existing key is returned otherwise a new empty <see cref="PersistentSettings"/>
    /// child key is added and the new settings are returned.
    /// </summary>
    /// <param name="key">Key to add to the child dictionary.</param>
    /// <returns>
    /// If the key is exists then the existing key is returned otherwise a new
    /// empty <see cref="PersistentSettings"/> child key is added and the new
    /// settings are returned.
    /// </returns>
    /// <since>6.0</since>
    public PersistentSettings AddChild(string key)
    {
      PersistentSettings result;
      m_children.TryGetValue(key, out result);
      if (null != result) return result;
      result = new PersistentSettings(Parent, null, IsWindowPositionSettings, IsCommandsSettings, key){ Owner = this };
      m_children.Add(key, result);
      return result;
    }

    /// <summary>
    /// Call this method to delete a child settings key.
    /// </summary>
    /// <param name="key"></param>
    /// <since>6.0</since>
    public void DeleteChild(string key)
    {
      if (m_children.ContainsKey(key))
      {
        m_children.Remove(key);
        if (Service.SupportsPlist)
          Service.DeleteChild(this, key);
      }
    }

    protected PersistentSettings(SerializationInfo info, StreamingContext context)
    {
    }

    [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.SerializationFormatter)]
    void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
    {
      m_settings.GetObjectData(info, context);
    }

    private readonly PersistentSettings AllUserSettings;

    /// <since>5.0</since>
    public static PersistentSettings FromPlugInId(Guid pluginId)
    {
#if RHINO_SDK
      if (pluginId == Guid.Empty)
        throw new System.ComponentModel.InvalidEnumArgumentException($"pluginId Can not be Guid.Empty");
      PersistentSettingsManager manager = PersistentSettingsManager.Create(pluginId);
      return manager.PluginSettings;
#else
      return null;
#endif
    }

    static PersistentSettings _rhinoAppSettings;
    /// <since>6.14</since>
    public static PersistentSettings RhinoAppSettings => _rhinoAppSettings ?? (_rhinoAppSettings = FromPlugInId(RhinoApp.CurrentRhinoId));

    internal PersistentSettings(PlugInSettings parent, PersistentSettings allUserSettings, bool windowPositionSettings, bool isCommandSettings, string key)
    {
      Parent = parent;
      IsWindowPositionSettings = windowPositionSettings;
      IsCommandsSettings = isCommandSettings;
      Key = key;
      AllUserSettings = allUserSettings;
      m_settings = new Dictionary<string, SettingValue>();
      m_settings_validators = new Dictionary<string, Delegate>();
    }

    internal PlugInSettings Parent { get; private set; }
    internal PersistentSettings Owner { get; private set; }
    internal bool IsWindowPositionSettings { get; private set; }
    internal string Key { get; }
    internal bool IsCommandsSettings { get; }

    internal void MergeChangedSettingsFile(PersistentSettings source)
    {
      lock (m_lock_settings)
      {
        if (source == null)
        {
          // Set all values to default value
          foreach (var setting in m_settings)
            setting.Value.SetValue(false, setting.Value.GetValue(true), false);
          // Set all child settings to default values
          foreach (var settings in m_children)
            settings.Value.MergeChangedSettingsFile(null);
        }
        else
        {
          // Make values match source
          foreach (var setting in m_settings)
          {
            SettingValue value;
            if (source.m_settings.TryGetValue(setting.Key, out value) && value != null)
            {
              // Value found in source so make the value and default match
              setting.Value.SetValue(true, value.GetValue(true), false);
              setting.Value.SetValue(false, value.GetValue(false), false);
            }
            else
            {
              // Value not found in source so set to default value
              setting.Value.SetValue(false, setting.Value.GetValue(true), false);
            }
          }
          // Now look for values that are in the source dictionary but not in
          // this settings dictionary
          foreach (var setting in source.m_settings)
          {
            // synched in loop above
            if (m_settings.ContainsKey(setting.Key))
              continue;
            // In source dictionary but not in the current dictionary so add it
            m_settings.Add(setting.Key, new SettingValue(this, setting.Key, setting.Value.RuntimeType, setting.Value.GetValue(false), setting.Value.GetValue(true)));
          }
          // Process child settings
          foreach (var settings in m_children)
          {
            PersistentSettings other;
            source.m_children.TryGetValue(settings.Key, out other);
            settings.Value.MergeChangedSettingsFile(other);
          }
          // Check source children for settings that are not in the current
          // settings child dictionary
          foreach (var settings in source.m_children)
          {
            // In the current child dictionary so nothing to do
            if (m_children.ContainsKey(settings.Key)) continue;
            // Create a new child settings
            var child = new PersistentSettings(Parent, null, IsWindowPositionSettings, settings.Value.IsCommandsSettings, settings.Key) { Owner = this };
            // Merge source settings into new child
            child.MergeChangedSettingsFile(settings.Value);
            // Add child to this child dictionary
            m_children.Add(settings.Key, child);
          }
        }
      }
    }

    internal void CopyFrom(PersistentSettings source)
    {
      if (null != source)
      {
        lock (m_lock_settings)
        {
          foreach (var item in source.m_settings)
          {
            if (m_settings.ContainsKey(item.Key))
              m_settings[item.Key].CopyFrom(item.Value);
            else
              m_settings.Add(item.Key, new SettingValue(this, item.Key, item.Value.RuntimeType, item.Value.GetValue(false), item.Value.GetValue(true)));
            // Tag the item as ReadOnly so the global setting can  not be changed.
            m_settings[item.Key].ReadOnly = true;
          }
          foreach (var item in source.m_children)
          {
            if (m_children.ContainsKey(item.Key))
              m_children[item.Key].CopyFrom(item.Value);
            else
            {
              var value = new PersistentSettings(Parent, null, IsWindowPositionSettings, item.Value.IsCommandsSettings, item.Key) { Owner = this };
              value.CopyFrom(item.Value);
              m_children.Add(item.Key, value);
            }
          }
        }
      }
    }

    /// <summary>
    /// Sets a validator for a given key.
    /// <para>Note to implementers: <typeparamref name="T">parameter T</typeparamref> should be one of the
    /// supported types for the PersistentSettings class and should match the type associated with the key.</para>
    /// <para>This method allows to use anonymous methods and lambda expressions.</para>
    /// </summary>
    /// <param name="key">The key to which to bind the validator.</param>
    /// <param name="validator">A validator instance of your own class.</param>
    /// <typeparam name="T"></typeparam>
    public void RegisterSettingsValidator<T>(string key, EventHandler<PersistentSettingsEventArgs<T>> validator)
    {
      m_settings_validators[key] = validator;
    }

    /// <summary>
    /// Provides a way to find a ready-to-use validator for the
    /// PersistentSetting class for the given the key, or obtaining null.
    /// </summary>
    /// <typeparam name="T">The type that the validator acts upon.</typeparam>
    /// <param name="key">The name of the setting key.</param>
    /// <exception cref="InvalidCastException">If type parameter T is not
    /// the right specialization for <see cref="PersistentSettingsEventArgs{T}"/>.</exception>
    /// <returns>A valid validator, or null if no validator was found.</returns>
    /// <since>5.0</since>
    public EventHandler<PersistentSettingsEventArgs<T>> GetValidator<T>(string key)
    {
      Delegate validator;
      m_settings_validators.TryGetValue(key, out validator);

      if (object.ReferenceEquals(validator, null)) return null;

      var typedValidator = validator as EventHandler<PersistentSettingsEventArgs<T>>;
      if (typedValidator == null)
        throw new InvalidCastException(
          string.Format("The requested validator for key \"{0}\" was of type: {1} and "
                        + "therefore it would have been specialized for type: {2}, but was expected to be "
                        + "specialized for type: {3} based on passed content."
            , key, validator.GetType().ToString(),
            validator.GetType().GenericTypeArguments[0].GenericTypeArguments[0].ToString(),
            typeof(T).ToString())
        );

      return typedValidator;
    }

    /// <since>6.0</since>
    public bool ContainsChangedValues()
    {
      // http://mcneel.myjetbrains.com/youtrack/issue/RH-30428
      // 6 June 2015 John Morse
      // Added check check for ItemDeletedSinceSave which allows calls to
      // Delete to trigger changed events
      if (null != m_settings && (ItemDeletedSinceSave || m_settings.Any(v => v.Value.ChangedSinceSave)))
        return true;
      return (null != m_children && m_children.Any(child => child.Value.ContainsChangedValues()));
    }

    /// <since>6.0</since>
    public void ClearChangedFlag()
    {
      lock (m_lock_settings)
      {
        foreach (var key_value_pair in m_settings)
          key_value_pair.Value.ChangedSinceSave = false;
        foreach (var child in m_children)
          child.Value.ClearChangedFlag();
        ItemDeletedSinceSave = false;
      }
    }

    /// <since>5.0</since>
    public bool ContainsModifiedValues(PersistentSettings allUserSettings)
    {
      lock (m_lock_settings)
      {
        if (null != m_settings && m_settings.Count > 0)
        {
          foreach (var v in m_settings)
          {
            if (v.Value.ValueDifferentThanDefault)
              return true;
            if (null != allUserSettings && allUserSettings.m_settings.ContainsKey(v.Key) &&
                0 !=
                string.Compare(v.Value.GetValue(false), allUserSettings.m_settings[v.Key].GetValue(false),
                  StringComparison.Ordinal))
              return true;
          }
        }
        if (null != m_children && m_children.Count > 0)
        {
          foreach (var child in m_children)
            if (child.Value.ContainsModifiedValues(null))
              return true;
        }
        return false;
      }
    }

    internal string this[string key]
    {
      get { return m_settings[key].GetValue(false); }
      set
      {
        if (m_settings.ContainsKey(key))
          m_settings[key].SetValue(false, value, true);
        else
          m_settings.Add(key, new SettingValue(this, key, typeof(string), value, ""));
      }
    }

    SettingValue GetValue(string key, Type runtimeType)
    {
      SettingValue rc;
      if (m_settings.TryGetValue(key, out rc))
        return rc;
      rc = new SettingValue(this, key, runtimeType, "", "");
      m_settings.Add(key, rc);
      // Does nothing on Windows, on Mac will check the Rhino PLIST
      // for the setting and set the current value to the PLIST value
      // if present.
      PersistentSettings.Service.OnAddSettingValue(rc);
      return rc;
    }

    private SettingValue RemapLegacaySettingsValue(string key, IEnumerable<string> legacyKeyList)
    {
      // If there is no legacy list then there is nothing to do
      if (legacyKeyList == null) return null;
      // Process the legacy list looking for an existing item with using a
      // legacy key
      foreach (var k in legacyKeyList)
      {
        SettingValue value;
        if (!m_settings.TryGetValue(k, out value))
          continue; // legacy key not found
        // Legacy key found!!!
        // Remove the legacy dictionary entry which was read from a old
        // settings file
        m_settings.Remove(k);
        // Replace it with the current value
        m_settings[key] = value;
        return value;
      }
      return null;
    }

    private SettingValue TryGetSettingsValue(Type runtimeType, string key, IEnumerable<string> legacyKeyList)
    {
      SettingValue value;
      m_settings.TryGetValue(key, out value);
      // If the value is null then check to see if there a legacy value was
      // previously read and remap it to this the specified key
      if (value == null)
        value = RemapLegacaySettingsValue(key, legacyKeyList);
      if (value == null && runtimeType != null)
      {
        // Does nothing on Windows, on Mac will check the Rhino PLIST
        // for the setting and set the current value to the PLIST value
        // if present found
        Service.TryGetPlistValue(this, key, (k) =>
        {
          // This gets passed to the PLIST parsing function and the
          // value.ValueString will get initialized using the parsed
          // PLSIT value 
          value = new SettingValue(this, key, runtimeType, "", "");
          m_settings[key] = value;
          return value;
        });
      }
      return value;
    }

    /// <since>6.0</since>
    public bool TryGetGuid(string key, out Guid value)
    {
      return TryGetGuid(key, out value, null);
    }

    /// <since>6.0</since>
    public bool TryGetGuid(string key, out Guid value, IEnumerable<string> legacyKeyList)
    {
      var sv = TryGetSettingsValue(typeof(Guid), key, legacyKeyList);
      if (sv != null)
        return sv.TryGetGuid(false, out value);
      value = Guid.Empty;
      return false;
    }

    /// <since>5.0</since>
    public bool TryGetBool(string key, out bool value)
    {
      return TryGetBool(key, out value, null);
    }

    /// <since>6.0</since>
    public bool TryGetBool(string key, out bool value, IEnumerable<string> legacyKeyList)
    {
      var sv = TryGetSettingsValue(typeof(bool), key, legacyKeyList);
      if (sv != null)
        return sv.TryGetBool(false, out value);
      value = false;
      return false;
    }

    /// <since>5.0</since>
    public bool GetBool(string key)
    {
      bool rc;
      if (m_settings[key].TryGetBool(false, out rc))
        return rc;
      throw new NotSupportedException("Key '" + key + "' value type is not a bool.");
    }

    /// <since>5.0</since>
    public bool GetBool(string key, bool defaultValue)
    {
      return GetBool(key, defaultValue, null);
    }

    private void SetValueToDefaultIfEmpy(string key, Action<SettingValue> setAction)
    {
      // Get... functions call SetDefault to set the default value, a side
      // effect is the setting gets added to the m_settings dictionary.
      // Mac Rhino will also look for the setting in the Rhino PLIST and
      // if found initialize the SettingValue.ValueString with the PLIST
      // value.  If this happens then there is not need to set the current
      // value to the default value, doing so will actually cause the 
      // saved value to get deleted from the PLIST because
      // SettingValue.SetValue deletes PLIST entries when setting to 
      // the default value.
      var setting = m_settings[key];
      // If the new SettingValue.ValueString is NOT empty then it was
      // initialized using the PLIST so DO NOT set the value to the 
      // default value.  If the new SettingValue.ValueString IS EMPTY
      // then set the current value to the provided default value.
      if (string.IsNullOrEmpty(setting.ValueString))
        setAction(setting);
    }

    /// <since>6.0</since>
    public bool GetBool(string key, bool defaultValue, IEnumerable<string> legacyKeyList)
    {
      bool rc;
      if (TryGetBool(key, out rc, legacyKeyList))
      {
        m_settings[key].SetBool(true, defaultValue, GetValidator<bool>(key), false);
        return rc;
      }
      SetDefault(key, defaultValue);
      SetValueToDefaultIfEmpy(key, setting => setting.SetBool(false, defaultValue, GetValidator<bool>(key), false));
      return GetBool(key);
    }

    /// <since>5.0</since>
    public bool TryGetByte(string key, out byte value)
    {
      return TryGetByte(key, out value, null);
    }

    /// <since>6.0</since>
    public bool TryGetByte(string key, out byte value, IEnumerable<string> legacyKeyList)
    {
      var sv = TryGetSettingsValue(typeof(byte), key, legacyKeyList);
      if (sv != null)
        return sv.TryGetByte(false, out value);
      value = 0;
      return false;
    }

    /// <since>5.0</since>
    public byte GetByte(string key)
    {
      byte rc;
      if (m_settings[key].TryGetByte(false, out rc))
        return rc;
      throw new NotSupportedException("Key '" + key + "' value type is not a byte.");
    }

    /// <since>5.0</since>
    public byte GetByte(string key, byte defaultValue)
    {
      return GetByte(key, defaultValue, null);
    }

    /// <since>6.0</since>
    public byte GetByte(string key, byte defaultValue, IEnumerable<string> legacyKeyList)
    {
      byte rc;
      if (TryGetByte(key, out rc, legacyKeyList))
      {
        m_settings[key].SetByte(true, defaultValue, GetValidator<byte>(key), false);
        return rc;
      }
      SetDefault(key, defaultValue);
      SetValueToDefaultIfEmpy(key, setting => setting.SetByte(false, defaultValue, GetValidator<byte>(key), false));
      return GetByte(key);
    }

    /// <since>5.0</since>
    public bool TryGetInteger(string key, out int value)
    {
      return TryGetInteger(key, out value, null);
    }

    /// <since>6.0</since>
    public bool TryGetInteger(string key, out int value, IEnumerable<string> legacyKeyList)
    {
      var sv = TryGetSettingsValue(typeof(int), key, legacyKeyList);
      if (sv != null)
        return sv.TryGetInteger(false, out value);
      value = int.MaxValue;
      return false;
    }

    /// <since>5.0</since>
    public int GetInteger(string key)
    {
      int rc;
      if (m_settings[key].TryGetInteger(false, out rc))
        return rc;
      throw new NotSupportedException("Key '" + key + "' value type is not an integer.");
    }

    /// <since>5.0</since>
    public int GetInteger(string key, int defaultValue)
    {
      return GetInteger(key, defaultValue, null);
    }

    /// <since>6.0</since>
    public int GetInteger(string key, int defaultValue, int bound, bool boundIsLower)
    {
      var rc = GetInteger(key, defaultValue);
      if (boundIsLower && rc < bound)
        rc = bound;
      if (!boundIsLower && rc > bound)
        rc = bound;
      return rc;
    }

    /// <since>6.0</since>
    public int GetInteger(string key, int defaultValue, int lowerBound, int upperBound)
    {
      var rc = GetInteger(key, defaultValue);
      if (rc < lowerBound)
        rc = lowerBound;
      if (rc > upperBound)
        rc = upperBound;
      return rc;
    }


    /// <since>6.0</since>
    public int GetInteger(string key, int defaultValue, IEnumerable<string> legacyKeyList)
    {
      int rc;
      if (TryGetInteger(key, out rc, legacyKeyList))
      {
        m_settings[key].SetInteger(true, defaultValue, GetValidator<int>(key), false);
        return rc;
      }
      SetDefault(key, defaultValue);
      SetValueToDefaultIfEmpy(key, setting => setting.SetInteger(false, defaultValue, GetValidator<int>(key), false));
      return GetInteger(key);
    }

    /// <since>5.0</since>
    [CLSCompliant(false)]
    public bool TryGetUnsignedInteger(string key, out uint value)
    {
      return TryGetUnsignedInteger(key, out value, null);
    }

    /// <since>6.0</since>
    [CLSCompliant(false)]
    public bool TryGetUnsignedInteger(string key, out uint value, IEnumerable<string> legacyKeyList)
    {
      var sv = TryGetSettingsValue(typeof(uint), key, legacyKeyList);
      if (sv != null)
        return sv.TryGetUnsignedInteger(false, out value);
      value = uint.MaxValue;
      return false;
    }

    /// <since>5.0</since>
    [CLSCompliant(false)]
    public uint GetUnsignedInteger(string key)
    {
      uint rc;
      if (m_settings[key].TryGetUnsignedInteger(false, out rc))
        return rc;
      throw new NotSupportedException("Key '" + key + "' value type is not an unsigned integer.");
    }

    /// <since>5.0</since>
    [CLSCompliant(false)]
    public uint GetUnsignedInteger(string key, uint defaultValue)
    {
      return GetUnsignedInteger(key, defaultValue, null);
    }

    /// <since>6.0</since>
    [CLSCompliant(false)]
    public uint GetUnsignedInteger(string key, uint defaultValue, IEnumerable<string> legacyKeyList)
    {
      uint rc;
      if (TryGetUnsignedInteger(key, out rc, legacyKeyList))
      {
        m_settings[key].SetUnsignedInteger(true, defaultValue, GetValidator<uint>(key), false);
        return rc;
      }
      SetDefault(key, defaultValue);
      SetValueToDefaultIfEmpy(key, setting => setting.SetUnsignedInteger(false, defaultValue, GetValidator<uint>(key), false));
      return GetUnsignedInteger(key);
    }

    /// <since>5.0</since>
    public bool TryGetDouble(string key, out double value)
    {
      return TryGetDouble(key, out value, null);
    }

    /// <since>6.0</since>
    public bool TryGetDouble(string key, out double value, IEnumerable<string> legacyKeyList)
    {
      var sv = TryGetSettingsValue(typeof(double), key, legacyKeyList);
      if (sv != null)
        return sv.TryGetDouble(false, out value);
      value = double.MaxValue;
      return false;
    }

    /// <since>5.0</since>
    public double GetDouble(string key)
    {
      double rc;
      if (m_settings[key].TryGetDouble(false, out rc))
        return rc;
      throw new NotSupportedException("Key '" + key + "' value type is not a double.");
    }

    /// <since>5.0</since>
    public double GetDouble(string key, double defaultValue)
    {
      return GetDouble(key, defaultValue, null);
    }

    /// <since>6.0</since>
    public double GetDouble(string key, double defaultValue, IEnumerable<string> legacyKeyList)
    {
      double rc;
      if (TryGetDouble(key, out rc, legacyKeyList))
      {
        m_settings[key].SetDouble(true, defaultValue, GetValidator<double>(key), false);
        return rc;
      }
      SetDefault(key, defaultValue);
      SetValueToDefaultIfEmpy(key, setting => setting.SetDouble(false, defaultValue, GetValidator<double>(key), false));
      return GetDouble(key);
    }

    /// <since>5.0</since>
    public bool TryGetChar(string key, out char value)
    {
      return TryGetChar(key, out value, null);
    }

    /// <since>6.0</since>
    public bool TryGetChar(string key, out char value, IEnumerable<string> legacyKeyList)
    {
      var sv = TryGetSettingsValue(typeof(char), key, legacyKeyList);
      if (sv != null)
        return sv.TryGetChar(false, out value);
      value = char.MaxValue;
      return false;
    }

    /// <since>5.0</since>
    public char GetChar(string key)
    {
      char rc;
      if (m_settings[key].TryGetChar(false, out rc))
        return rc;
      throw new NotSupportedException("Key '" + key + "' value type is not a char.");
    }

    /// <since>5.0</since>
    public char GetChar(string key, char defaultValue)
    {
      return GetChar(key, defaultValue, null);
    }

    /// <since>6.0</since>
    public char GetChar(string key, char defaultValue, IEnumerable<string> legacyKeyList)
    {
      char rc;
      if (TryGetChar(key, out rc))
      {
        m_settings[key].SetChar(true, defaultValue, GetValidator<char>(key), false);
        return rc;
      }
      SetDefault(key, defaultValue);
      SetValueToDefaultIfEmpy(key, setting => setting.SetChar(false, defaultValue, GetValidator<char>(key), false));
      return GetChar(key);
    }

    /// <since>5.0</since>
    public bool TryGetString(string key, out string value)
    {
      return TryGetString(key, out value, null);
    }

    /// <since>6.0</since>
    public bool TryGetString(string key, out string value, IEnumerable<string> legacyKeyList)
    {
      var sv = TryGetSettingsValue(typeof(string), key, legacyKeyList);
      if (sv != null)
        return sv.TryGetString(false, out value);
      value = "";
      return false;
    }

    /// <since>5.0</since>
    public string GetString(string key)
    {
      string rc;
      if (m_settings[key].TryGetString(false, out rc))
        return rc;
      throw new NotSupportedException("Key '" + key + "' value type is not a string.");
    }

    /// <since>5.0</since>
    public string GetString(string key, string defaultValue)
    {
      return GetString(key, defaultValue, null);
    }

    /// <since>6.0</since>
    public string GetString(string key, string defaultValue, IEnumerable<string> legacyKeyList)
    {
      string rc;
      if (TryGetString(key, out rc))
      {
        m_settings[key].SetString(true, defaultValue, GetValidator<string>(key), false);
        return rc;
      }
      SetDefault(key, defaultValue);
      SetValueToDefaultIfEmpy(key, setting => setting.SetString(false, defaultValue, GetValidator<string>(key), false));
      return GetString(key);
    }

    public bool TryGetStringDictionary(string key, out KeyValuePair<string, string>[] value)
    {
      return TryGetStringDictionary(key, out value, null);
    }

    public bool TryGetStringDictionary(string key, out KeyValuePair<string, string>[] value,
      IEnumerable<string> legacyKeyList)
    {
      var sv = TryGetSettingsValue(typeof(KeyValuePair<string, string>), key, legacyKeyList);
      if (sv != null)
        return sv.TryGetStringDictionary(false, out value);
      value = null;
      return false;
    }

    /// <since>6.0</since>
    public KeyValuePair<string, string>[] GetStringDictionary(string key)
    {
      KeyValuePair<string, string>[] rc;
      if (m_settings[key].TryGetStringDictionary(false, out rc))
        return rc;
      throw new NotSupportedException("Key '" + key + "' value type is not a string Dictionary.");
    }

    public KeyValuePair<string, string>[] GetStringDictionary(string key, KeyValuePair<string, string>[] defaultValue)
    {
      return GetStringDictionary(key, defaultValue, null);
    }

    public KeyValuePair<string, string>[] GetStringDictionary(string key, KeyValuePair<string, string>[] defaultValue,
      IEnumerable<string> legacyKeyList)
    {
      KeyValuePair<string, string>[] rc;
      if (TryGetStringDictionary(key, out rc, legacyKeyList))
      {
        m_settings[key].SetStringDictionary(true, defaultValue, GetValidator<KeyValuePair<string, string>[]>(key), false);
        return rc;
      }
      SetDefault(key, defaultValue);
      SetValueToDefaultIfEmpy(key, setting => setting.SetStringDictionary(false, defaultValue, GetValidator<KeyValuePair<string, string>[]>(key), false));
      return GetStringDictionary(key);
    }

    /// <since>5.0</since>
    public bool TryGetStringList(string key, out string[] value)
    {
      return TryGetStringList(key, out value, null);
    }

    /// <since>6.0</since>
    public bool TryGetStringList(string key, out string[] value, IEnumerable<string> legacyKeyList)
    {

      value = null;
      var sv = TryGetSettingsValue(typeof(string[]), key, legacyKeyList);
      if (sv == null)
        return false;
      string root_string = string.Empty;
      if (null != AllUserSettings && AllUserSettings.m_settings.ContainsKey(key))
        root_string = AllUserSettings.m_settings[key].GetValue(false);
      return sv.TryGetStringList(false, root_string, out value);
    }

    /// <since>5.0</since>
    public string[] GetStringList(string key)
    {
      var sv = TryGetSettingsValue(typeof(string[]), key, null);
      if (sv == null)
        throw new KeyNotFoundException(key);
      string[] rc;
      if (TryGetStringList(key, out rc))
        return rc;
      throw new NotSupportedException("Key '" + key + "' value type is not a string list.");
    }

    /// <since>5.0</since>
    public string[] GetStringList(string key, string[] defaultValue)
    {
      return GetStringList(key, defaultValue, null);
    }

    /// <since>6.0</since>
    public string[] GetStringList(string key, string[] defaultValue, IEnumerable<string> legacyKeyList)
    {
      string[] rc;
      if (TryGetStringList(key, out rc, legacyKeyList))
      {
        m_settings[key].SetStringList(true, defaultValue, GetValidator<string[]>(key), false);
        return rc;
      }
      SetDefault(key, defaultValue);
      SetValueToDefaultIfEmpy(key, setting => setting.SetStringList(false, defaultValue, GetValidator<string[]>(key), false));
      return GetStringList(key);
    }

    /// <since>5.0</since>
    public bool TryGetDate(string key, out DateTime value)
    {
      return TryGetDate(key, out value, null);
    }

    /// <since>6.0</since>
    public bool TryGetDate(string key, out DateTime value, IEnumerable<string> legacyKeyList)
    {
      var sv = TryGetSettingsValue(typeof(DateTime), key, legacyKeyList);
      if (sv != null)
        return sv.TryGetDate(false, out value);
      value = DateTime.MinValue;
      return false;
    }

    /// <since>5.0</since>
    public DateTime GetDate(string key)
    {
      DateTime rc;
      if (m_settings[key].TryGetDate(false, out rc))
        return rc;
      throw new NotSupportedException("Key '" + key + "' value type is not a DateTime.");
    }

    /// <since>5.0</since>
    public DateTime GetDate(string key, DateTime defaultValue)
    {
      return GetDate(key, defaultValue, null);
    }

    /// <since>6.0</since>
    public DateTime GetDate(string key, DateTime defaultValue, IEnumerable<string> legacyKeyList)
    {
      DateTime rc;
      if (TryGetDate(key, out rc, legacyKeyList))
      {
        m_settings[key].SetDate(true, defaultValue, GetValidator<DateTime>(key), false);
        return rc;
      }
      SetDefault(key, defaultValue);
      SetValueToDefaultIfEmpy(key, setting => setting.SetDate(false, defaultValue, GetValidator<DateTime>(key), false));
      return GetDate(key);
    }

    /// <since>5.0</since>
    public bool TryGetColor(string key, out Color value)
    {
      return TryGetColor(key, out value, null);
    }

    /// <since>6.0</since>
    public bool TryGetColor(string key, out Color value, IEnumerable<string> legacyKeyList)
    {
      var sv = TryGetSettingsValue(typeof(Color), key, legacyKeyList);
      if (sv != null)
        return sv.TryGetColor(false, out value);
      value = Color.Empty;
      return false;
    }

    /// <since>5.0</since>
    public Color GetColor(string key)
    {
      Color rc;
      if (m_settings[key].TryGetColor(false, out rc))
        return rc;
      throw new NotSupportedException("Key '" + key + "' value type is not a Color.");
    }

    /// <since>5.0</since>
    public Color GetColor(string key, Color defaultValue)
    {
      return GetColor(key, defaultValue, null);
    }

    /// <since>6.0</since>
    public Color GetColor(string key, Color defaultValue, IEnumerable<string> legacyKeyList)
    {
      Color rc;
      if (TryGetColor(key, out rc, legacyKeyList))
      {
        m_settings[key].SetColor(true, defaultValue, GetValidator<Color>(key), false);
        return rc;
      }
      SetDefault(key, defaultValue);
      SetValueToDefaultIfEmpy(key, setting => setting.SetColor(false, defaultValue, GetValidator<Color>(key), false));
      // Won't be in PLIST on Mac if current value is equal to the default value so
      // calling GetColor will throw an exception when the color is not found. This
      // should just return the default color in that case.
      if (TryGetColor(key, out rc))
        return rc;
      return defaultValue;
    }

    /// <since>6.0</since>
    public Guid GetGuid(string key)
    {
      Guid rc;
      if (m_settings[key].TryGetGuid(false, out rc))
        return rc;
      throw new NotSupportedException("Key '" + key + "' value type is not a Guid.");
    }

    /// <since>6.0</since>
    public Guid GetGuid(string key, Guid defaultValue)
    {
      return GetGuid(key, defaultValue, null);
    }

    /// <since>6.0</since>
    public Guid GetGuid(string key, Guid defaultValue, IEnumerable<string> legacyKeyList)
    {
      Guid rc;
      if (TryGetGuid(key, out rc, legacyKeyList))
      {
        m_settings[key].SetGuid(true, defaultValue, GetValidator<Guid>(key), false);
        return rc;
      }
      SetDefault(key, defaultValue);
      SetValueToDefaultIfEmpy(key, setting => setting.SetGuid(false, defaultValue, GetValidator<Guid>(key), false));
      return GetGuid(key);
    }

    /// <since>5.0</since>
    public bool TryGetPoint(string key, out System.Drawing.Point value)
    {
      return TryGetPoint(key, out value, null);
    }

    /// <since>6.0</since>
    public bool TryGetPoint(string key, out System.Drawing.Point value, IEnumerable<string> legacyKeyList)
    {
      var sv = TryGetSettingsValue(typeof(System.Drawing.Point), key, legacyKeyList);
      if (sv != null)
        return sv.TryGetPoint(false, out value);
      value = System.Drawing.Point.Empty;
      return false;
    }

    /// <since>5.0</since>
    public System.Drawing.Point GetPoint(string key)
    {
      System.Drawing.Point rc;
      if (m_settings[key].TryGetPoint(false, out rc))
        return rc;
      throw new NotSupportedException("Key '" + key + "' value type is not a Point.");
    }

    /// <since>5.0</since>
    public System.Drawing.Point GetPoint(string key, System.Drawing.Point defaultValue)
    {
      return GetPoint(key, defaultValue, null);
    }

    /// <since>6.0</since>
    public System.Drawing.Point GetPoint(string key, System.Drawing.Point defaultValue,
      IEnumerable<string> legacyKeyList)
    {
      System.Drawing.Point rc;
      if (TryGetPoint(key, out rc, legacyKeyList))
      {
        m_settings[key].SetPoint(true, defaultValue, GetValidator<System.Drawing.Point>(key), false);
        return rc;
      }
      SetDefault(key, defaultValue);
      SetValueToDefaultIfEmpy(key, setting => setting.SetPoint(false, defaultValue, GetValidator<System.Drawing.Point>(key), false));
      return GetPoint(key);
    }

    /// <since>5.0</since>
    public bool TryGetPoint3d(string key, out Point3d value)
    {
      return TryGetPoint3d(key, out value, null);
    }

    /// <since>6.0</since>
    public bool TryGetPoint3d(string key, out Point3d value, IEnumerable<string> legacyKeyList)
    {
      var sv = TryGetSettingsValue(typeof(Point3d), key, legacyKeyList);
      if (sv != null)
        return sv.TryGetPoint3d(false, out value);
      value = Point3d.Unset;
      return false;
    }

    /// <since>5.0</since>
    public Point3d GetPoint3d(string key)
    {
      Point3d rc;
      if (m_settings[key].TryGetPoint3d(false, out rc))
        return rc;
      throw new NotSupportedException("Key '" + key + "' value type is not a Point3d.");
    }

    /// <since>5.0</since>
    public Point3d GetPoint3d(string key, Point3d defaultValue)
    {
      return GetPoint3d(key, defaultValue, null);
    }

    /// <since>6.0</since>
    public Point3d GetPoint3d(string key, Point3d defaultValue, IEnumerable<string> legacyKeyList)
    {
      Point3d rc;
      if (TryGetPoint3d(key, out rc, legacyKeyList))
      {
        m_settings[key].SetPoint3d(true, defaultValue, GetValidator<Point3d>(key), false);
        return rc;
      }
      SetDefault(key, defaultValue);
      SetValueToDefaultIfEmpy(key, setting => setting.SetPoint3d(false, defaultValue, GetValidator<Point3d>(key), false));
      return GetPoint3d(key);
    }

    /// <since>5.0</since>
    public bool TryGetSize(string key, out Size value)
    {
      return TryGetSize(key, out value, null);
    }

    /// <since>6.0</since>
    public bool TryGetSize(string key, out Size value, IEnumerable<string> legacyKeyList)
    {
      var sv = TryGetSettingsValue(typeof(Size), key, legacyKeyList);
      if (sv != null)
        return sv.TryGetSize(false, out value);
      value = Size.Empty;
      return false;
    }

    /// <since>5.0</since>
    public Size GetSize(string key)
    {
      Size rc;
      if (m_settings[key].TryGetSize(false, out rc))
        return rc;
      throw new NotSupportedException("Key '" + key + "' value type is not a Size.");
    }

    /// <since>5.0</since>
    public Size GetSize(string key, Size defaultValue)
    {
      return GetSize(key, defaultValue, null);
    }

    /// <since>6.0</since>
    public Size GetSize(string key, Size defaultValue, IEnumerable<string> legacyKeyList)
    {
      Size rc;
      if (TryGetSize(key, out rc, legacyKeyList))
      {
        m_settings[key].SetSize(true, defaultValue, GetValidator<Size>(key), false);
        return rc;
      }
      SetDefault(key, defaultValue);
      SetValueToDefaultIfEmpy(key, setting => setting.SetSize(false, defaultValue, GetValidator<Size>(key), false));
      return GetSize(key);
    }

    /// <since>5.0</since>
    public bool TryGetRectangle(string key, out Rectangle value)
    {
      return TryGetRectangle(key, out value, null);
    }

    /// <since>6.0</since>
    public bool TryGetRectangle(string key, out Rectangle value, IEnumerable<string> legacyKeyList)
    {
      var sv = TryGetSettingsValue(typeof(Rectangle), key, legacyKeyList);
      if (sv != null)
        return sv.TryGetRectangle(false, out value);
      value = Rectangle.Empty;
      return false;
    }

    /// <since>5.0</since>
    public Rectangle GetRectangle(string key)
    {
      Rectangle rc;
      if (m_settings[key].TryGetRectangle(false, out rc))
        return rc;
      throw new NotSupportedException("Key '" + key + "' value type is not a Rectangle.");
    }

    /// <since>5.0</since>
    public Rectangle GetRectangle(string key, Rectangle defaultValue)
    {
      return GetRectangle(key, defaultValue, null);
    }

    /// <since>6.0</since>
    public Rectangle GetRectangle(string key, Rectangle defaultValue, IEnumerable<string> legacyKeyList)
    {
      Rectangle rc;
      if (TryGetRectangle(key, out rc, legacyKeyList))
      {
        m_settings[key].SetRectangle(true, defaultValue, GetValidator<Rectangle>(key), false);
        return rc;
      }
      SetDefault(key, defaultValue);
      SetValueToDefaultIfEmpy(key, setting => setting.SetRectangle(false, defaultValue, GetValidator<Rectangle>(key), false));
      return GetRectangle(key);
    }

    /// <since>5.0</since>
    public bool TryGetDefault(string key, out bool value)
    {
      SettingValue sv;
      if (m_settings.TryGetValue(key, out sv) && sv != null)
        return sv.TryGetBool(true, out value);
      value = false;
      return false;
    }

    /// <since>5.0</since>
    public bool TryGetDefault(string key, out byte value)
    {
      SettingValue sv;
      if (m_settings.TryGetValue(key, out sv) && sv != null)
        return sv.TryGetByte(true, out value);
      value = 0;
      return false;
    }

    /// <since>5.0</since>
    public bool TryGetDefault(string key, out int value)
    {
      SettingValue sv;
      if (m_settings.TryGetValue(key, out sv) && sv != null)
        return sv.TryGetInteger(true, out value);
      value = 0;
      return false;
    }

    /// <since>5.0</since>
    public bool TryGetDefault(string key, out double value)
    {
      SettingValue sv;
      if (m_settings.TryGetValue(key, out sv) && sv != null)
        return sv.TryGetDouble(true, out value);
      value = 0;
      return false;
    }

    /// <since>5.0</since>
    public bool TryGetDefault(string key, out char value)
    {
      SettingValue sv;
      if (m_settings.TryGetValue(key, out sv) && sv != null)
        return sv.TryGetChar(true, out value);
      value = '\0';
      return false;
    }

    /// <since>5.0</since>
    public bool TryGetDefault(string key, out string value)
    {
      SettingValue sv;
      if (m_settings.TryGetValue(key, out sv) && sv != null)
        return sv.TryGetString(true, out value);
      value = null;
      return false;
    }

    /// <since>5.0</since>
    public bool TryGetDefault(string key, out string[] value)
    {
      if (m_settings.ContainsKey(key))
      {
        string root_string = string.Empty;
        if (null != AllUserSettings && AllUserSettings.m_settings.ContainsKey(key))
          root_string = AllUserSettings.m_settings[key].GetValue(true);
        return m_settings[key].TryGetStringList(true, root_string, out value);
      }
      value = null;
      return false;
    }

    /// <since>5.0</since>
    public bool TryGetDefault(string key, out DateTime value)
    {
      SettingValue sv;
      if (m_settings.TryGetValue(key, out sv) && sv != null)
        return sv.TryGetDate(true, out value);
      value = DateTime.MinValue;
      return false;
    }

    /// <since>5.0</since>
    public bool TryGetDefault(string key, out Color value)
    {
      SettingValue sv;
      if (m_settings.TryGetValue(key, out sv) && sv != null)
        return sv.TryGetColor(true, out value);
      value = Color.Empty;
      return false;
    }

    /// <since>5.0</since>
    public bool TryGetDefault(string key, out Point3d value)
    {
      SettingValue sv;
      if (m_settings.TryGetValue(key, out sv) && sv != null)
        return sv.TryGetPoint3d(true, out value);
      value = Point3d.Unset;
      return false;
    }

    /// <since>5.0</since>
    public bool TryGetDefault(string key, out Size value)
    {
      SettingValue sv;
      if (m_settings.TryGetValue(key, out sv) && sv != null)
        return sv.TryGetSize(true, out value);
      value = Size.Empty;
      return false;
    }

    /// <since>5.0</since>
    public bool TryGetDefault(string key, out Rectangle value)
    {
      SettingValue sv;
      if (m_settings.TryGetValue(key, out sv) && sv != null)
        return sv.TryGetRectangle(true, out value);
      value = Rectangle.Empty;
      return false;
    }

    /// <summary>
    /// Get a stored enumerated value, or return default value if not found
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="defaultValue"></param>
    /// <returns></returns>
    /// <since>5.4</since>
    [CLSCompliant(false)]
    public T GetEnumValue<T>(T defaultValue)
      where T : struct, IConvertible
    {
      Type enum_type = typeof(T);
      return GetEnumValue(enum_type.Name, defaultValue);
    }

    /// <summary>
    /// Gets a stored enumerated value using a custom key, or return default value if not found. 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="key"> </param>
    /// <param name="defaultValue"> </param>
    /// <returns></returns>
    /// <since>5.4</since>
    [CLSCompliant(false)]
    public T GetEnumValue<T>(String key, T defaultValue)
      where T : struct, IConvertible
    {
      T rc;
      if (TryGetEnumValue<T>(key, out rc))
      {
        m_settings[key].SetEnum<T>(true, defaultValue, GetValidator<T>(key), false);
        return rc;
      }
      GetValue(key, defaultValue.GetType()).SetEnum(true, defaultValue, GetValidator<T>(key), true);
      m_settings[key].SetEnum(false, defaultValue, GetValidator<T>(key), false);
      return GetEnumValue<T>(key);
    }

    /// <summary>
    /// Get a stored enumerated value using a custom key.
    /// </summary>
    /// <param name="key"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns> 
    /// <exception cref="KeyNotFoundException"></exception>
    /// <since>5.4</since>
    [CLSCompliant(false)]
    public T GetEnumValue<T>(String key)
      where T : struct, IConvertible
    {
      if (null == key) throw new ArgumentNullException("key");
      if (!typeof(T).IsEnum) throw new ArgumentException("!typeof(T).IsEnum");

      var enum_type = typeof(T);

      T value;
      if (TryGetEnumValue<T>(key, out value))
      {
        return value;
      }
      var error_message = String.Format("Value for key={0} for enumerated type {1} not found.", key, enum_type.Name);
      throw new KeyNotFoundException(error_message);
    }

    /// <summary>
    /// Attempt to get the stored value for an enumerated setting using a custom key. Note: the enumerated value ALWAYS gets assigned!
    /// Be sure to check for success of this method to prevent erroneous use of the value.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="key"> </param>
    /// <param name="enumValue"></param>
    /// <returns>true if successful</returns>
    /// <since>5.4</since>
    [CLSCompliant(false)]
    public bool TryGetEnumValue<T>(String key, out T enumValue)
      where T : struct, IConvertible
    {
      if (string.IsNullOrEmpty(key)) throw new ArgumentNullException("key");
      var enum_type = typeof(T);
      if (!enum_type.IsEnum) throw new ArgumentException("!typeof(T).IsEnum");

      enumValue = default(T);

      var value = TryGetSettingsValue(enum_type, key, null);
      return value != null && value.TryGetEnum<T>(false, out enumValue);
    }

    /// <since>6.0</since>
    public void SetGuid(string key, Guid value)
    {
      GetValue(key, typeof(Guid)).SetGuid(false, value, GetValidator<Guid>(key), true);
    }

    /// <since>5.0</since>
    public void SetBool(string key, bool value)
    {
      GetValue(key, typeof(bool)).SetBool(false, value, GetValidator<bool>(key), true);
    }

    /// <since>5.0</since>
    public void SetByte(string key, byte value)
    {
      GetValue(key, typeof(byte)).SetByte(false, value, GetValidator<byte>(key), true);
    }

    /// <since>5.0</since>
    public void SetInteger(string key, int value)
    {
      GetValue(key, typeof(int)).SetInteger(false, value, GetValidator<int>(key), true);
    }

    /// <since>5.0</since>
    [CLSCompliant(false)]
    public void SetUnsignedInteger(string key, uint value)
    {
      GetValue(key, typeof(uint)).SetUnsignedInteger(false, value, GetValidator<uint>(key), true);
    }

    /// <since>5.0</since>
    public void SetDouble(string key, double value)
    {
      GetValue(key, typeof(double)).SetDouble(false, value, GetValidator<double>(key), true);
    }

    /// <since>5.0</since>
    public void SetChar(string key, char value)
    {
      GetValue(key, typeof(char)).SetChar(false, value, GetValidator<char>(key), true);
    }

    /// <since>5.0</since>
    public void SetString(string key, string value)
    {
      GetValue(key, typeof(string)).SetString(false, value, GetValidator<string>(key), true);
    }

    /// <summary>
    /// Adding this string to a string list when calling SetStringList will cause the ProgramData setting to
    /// get inserted at that location in the list.
    /// </summary>
    /// <since>5.0</since>
    public static string StringListRootKey
    {
      get { return SettingValue.StringListRootKey; }
    }

    /// <summary>
    /// Including a item with the value of StringListRootKey will cause the ProgramData value to get inserted at
    /// that location in the list when calling GetStringList.
    /// </summary>
    /// <param name="key">The string key.</param>
    /// <param name="value">An array of values to set.</param>
    /// <since>5.0</since>
    public void SetStringList(string key, string[] value)
    {
      GetValue(key, typeof(string[])).SetStringList(false, value, GetValidator<string[]>(key), true);
    }

    public void SetStringDictionary(string key, KeyValuePair<string, string>[] value)
    {
      GetValue(key, typeof(KeyValuePair<string, string>[])).SetStringDictionary(false, value, GetValidator<KeyValuePair<string, string>[]>(key), true);
    }

    /// <since>5.0</since>
    public void DeleteItem(string key)
    {
      if (m_settings.ContainsKey(key))
      {
        // Queue a settings file write as necessary
        m_settings[key].ChangedSinceSave = true;
        // http://mcneel.myjetbrains.com/youtrack/issue/RH-30428
        // 6 June 2015 John Morse
        // Added check check for ItemDeletedSinceSave which allows calls to
        // Delete to trigger changed events
        ItemDeletedSinceSave = true;
        m_settings.Remove(key);
        if (Service.SupportsPlist)
          Service.DeleteItem(this, key);
      }
    }

    /// <since>5.0</since>
    public void SetDate(string key, DateTime value)
    {
      GetValue(key, typeof(DateTime)).SetDate(false, value, GetValidator<DateTime>(key), true);
    }

    /// <since>5.0</since>
    public void SetColor(string key, Color value)
    {
      GetValue(key, typeof(Color)).SetColor(false, value, GetValidator<Color>(key), true);
    }

    /// <since>5.0</since>
    public void SetPoint3d(string key, Point3d value)
    {
      GetValue(key, typeof(Point3d)).SetPoint3d(false, value, GetValidator<Point3d>(key), true);
    }

    /// <since>5.0</since>
    public void SetRectangle(string key, Rectangle value)
    {
      GetValue(key, typeof(Rectangle)).SetRectangle(false, value, GetValidator<Rectangle>(key), true);
    }

    /// <since>5.0</since>
    public void SetSize(string key, Size value)
    {
      GetValue(key, typeof(Size)).SetSize(false, value, GetValidator<Size>(key), true);
    }

    /// <since>5.0</since>
    public void SetPoint(string key, System.Drawing.Point value)
    {
      GetValue(key, typeof(System.Drawing.Point)).SetPoint(false, value, GetValidator<System.Drawing.Point>(key), true);
    }

    /// <since>5.0</since>
    public void SetDefault(string key, bool value)
    {
      GetValue(key, typeof(bool)).SetBool(true, value, GetValidator<bool>(key), true);
    }

    /// <since>5.0</since>
    public void SetDefault(string key, byte value)
    {
      GetValue(key, typeof(byte)).SetByte(true, value, GetValidator<byte>(key), true);
    }

    /// <since>5.0</since>
    public void SetDefault(string key, int value)
    {
      GetValue(key, typeof(int)).SetInteger(true, value, GetValidator<int>(key), true);
    }

    /// <since>5.0</since>
    public void SetDefault(string key, double value)
    {
      GetValue(key, typeof(double)).SetDouble(true, value, GetValidator<double>(key), true);
    }

    /// <since>5.0</since>
    public void SetDefault(string key, char value)
    {
      GetValue(key, typeof(char)).SetChar(true, value, GetValidator<char>(key), true);
    }

    /// <since>5.0</since>
    public void SetDefault(string key, string value)
    {
      GetValue(key, typeof(string)).SetString(true, value, GetValidator<string>(key), true);
    }

    public void SetDefault(string key, KeyValuePair<string, string>[] value)
    {
      GetValue(key, typeof(KeyValuePair<string, string>[])).SetStringDictionary(true, value, GetValidator<KeyValuePair<string, string>[]>(key), true);
    }

    /// <since>5.0</since>
    public void SetDefault(string key, string[] value)
    {
      GetValue(key, typeof(string[])).SetStringList(true, value, GetValidator<string[]>(key), true);
    }

    /// <since>5.0</since>
    public void SetDefault(string key, DateTime value)
    {
      GetValue(key, typeof(DateTime)).SetDate(true, value, GetValidator<DateTime>(key), true);
    }

    /// <since>5.0</since>
    public void SetDefault(string key, Color value)
    {
      GetValue(key, typeof(Color)).SetColor(true, value, GetValidator<Color>(key), true);
    }

    /// <since>5.0</since>
    public void SetDefault(string key, Rectangle value)
    {
      GetValue(key, typeof(Rectangle)).SetRectangle(true, value, GetValidator<Rectangle>(key), true);
    }

    /// <since>5.0</since>
    public void SetDefault(string key, Size value)
    {
      GetValue(key, typeof(Size)).SetSize(true, value, GetValidator<Size>(key), true);
    }

    /// <since>5.0</since>
    public void SetDefault(string key, System.Drawing.Point value)
    {
      GetValue(key, typeof(System.Drawing.Point)).SetPoint(true, value, GetValidator<System.Drawing.Point>(key), true);
    }

    /// <since>5.0</since>
    public void SetDefault(string key, Point3d value)
    {
      GetValue(key, typeof(Point3d)).SetPoint3d(true, value, GetValidator<Point3d>(key), true);
    }

    /// <since>6.0</since>
    public void SetDefault(string key, Guid value)
    {
      GetValue(key, typeof(Guid)).SetGuid(true, value, GetValidator<Guid>(key), true);
    }

    /// <summary>
    /// Set an enumerated value in the settings.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="enumValue"></param>
    /// <since>5.4</since>
    [CLSCompliant(false)]
    public void SetEnumValue<T>(T enumValue)
      where T : struct, IConvertible
    {
      Type enum_type = typeof(T);
      SetEnumValue(enum_type.Name, enumValue);
    }

    /// <summary>
    /// Set an enumerated value in the settings using a custom key
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="key"> </param>
    /// <param name="value"></param>
    /// <since>5.4</since>
    [CLSCompliant(false)]
    public void SetEnumValue<T>(String key, T value)
      where T : struct, IConvertible
    {
      if (null == key) throw new ArgumentNullException("key");

      if (!typeof(T).IsEnum)
        throw new ArgumentException("!typeof(T).IsEnum");
      GetValue(key, typeof(T)).SetEnum<T>(false, value, GetValidator<T>(key), true);
    }

    /// <summary>
    /// If the settings dictionary contains one or more values, which are not equal to the default value, then Write the contents
    /// of this settings dictionary to the specified XmlWriter contained within elementName.
    /// </summary>
    /// <param name="xmlWriter">XmlWriter object to write to.</param>
    /// <param name="elementName">Element which will contain key value pairs.</param>
    /// <param name="attributeName">Optional element attribute.</param>
    /// <param name="attributeValue">Optional element attribute value.</param>
    /// <param name="allUserSettings">All users settings to compare with.</param>
    internal void WriteXmlElement(XmlWriter xmlWriter, string elementName, string attributeName, string attributeValue,
      PersistentSettings allUserSettings)
    {
      if (null != m_settings && ContainsModifiedValues(allUserSettings))
      {
        xmlWriter.WriteStartElement(elementName);
        if (!string.IsNullOrEmpty(attributeName) && !string.IsNullOrEmpty(attributeValue))
          xmlWriter.WriteAttributeString(attributeName, attributeValue);

        if (HiddenFromUserInterface)
          xmlWriter.WriteAttributeString("hidden", "True");

        lock (m_lock_settings)
        {
          foreach (var item in m_settings)
          {
            string all_user_value = null;
            if (null != allUserSettings && allUserSettings.m_settings.ContainsKey(item.Key))
              all_user_value = allUserSettings.m_settings[item.Key].GetValue(false);
            var value = item.Value.GetValue(false);
            var value_different_than_all_user = (null != all_user_value &&
                                                 0 != string.Compare(value, all_user_value, StringComparison.Ordinal));
            if (!value_different_than_all_user && !item.Value.ValueDifferentThanDefault) continue;
            // Write current value
            xmlWriter.WriteStartElement("entry");
            xmlWriter.WriteAttributeString("key", item.Key);
            // Not sure about this yet, writing the child settings hidden flag is okay but not
            // sure it is a good idea on an item by item basis yet.
            //if (item.Value.Hidden)
            //  xmlWriter.WriteAttributeString("hidden", "True");
            // The following is used when you want to write the default and all user values as item attributes
            // to the settings output file, useful when trying to determine why a value was written
            //const bool bWriteDefaultValue = false;
            //if (bWriteDefaullValue)
            //{
            //  string defaultValue = item.Value.GetValue(true);
            //  xmlWriter.WriteAttributeString("DefaultValue", null == defaultValue ? "" : defaultValue);
            //  if (null != allUserValue)
            //    xmlWriter.WriteAttributeString("AllUsersValue", allUserValue);
            //}

            if (!string.IsNullOrEmpty(value))
            {
              var write_string = true;
              // Special case for string arrays
              if (value.Contains("<list"))
              {
                string[] strings;
                if (item.Value.TryGetStringList(false, null, out strings))
                {
                  write_string = false;
                  xmlWriter.WriteStartElement("list");
                  if (strings != null)
                    foreach (var s in strings)
                      xmlWriter.WriteElementString("value", s);
                  xmlWriter.WriteEndElement();
                }
              }
              else if (value.Contains("<dictionary"))
              {
                KeyValuePair<string, string>[] items;
                if (item.Value.TryGetStringDictionary(false, out items))
                {
                  write_string = false;
                  xmlWriter.WriteStartElement("dictionary");
                  if (items != null)
                    foreach (var entry in items)
                    {
                      xmlWriter.WriteStartElement("value");
                      xmlWriter.WriteAttributeString("key", entry.Key);
                      xmlWriter.WriteString(entry.Value);
                      xmlWriter.WriteEndElement();
                    }
                  xmlWriter.WriteEndElement();
                }
              }
              if (write_string)
                xmlWriter.WriteString(value);
            }
            xmlWriter.WriteEndElement();
          }
          foreach (var item in m_children)
            item.Value.WriteXmlElement(xmlWriter, "child", "key", item.Key, null);
          xmlWriter.WriteEndElement();
        }
      }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="node"></param>
    /// <returns></returns>
    static string XmlDecode(XmlNode node)
    {
      if (node == null) return null;
      if (node.ChildNodes.Count == 1)
      {
        var child_node = node.ChildNodes[0];
        // Get the decoded string value from the node
        if (child_node.Value != null)
          return child_node.Value;
      }
      // Return the inner XML for things like lists or dictionaries which
      // contain child nodes.  In the case of things like string lists and
      // dictionaries the node.ChildNodes[0].Value will be null so you must
      // return in the node.InnerXml
      return node.InnerXml;
    }

    /// <summary>
    /// Parse XmlNode for settings "entry" elements, add entry elements to the
    /// dictionary first and if then check the defaults list and make sure the
    /// entry is in the list before setting the default value.
    /// </summary>
    internal void ParseXmlNodes(XmlNode nodeRoot)
    {
      if (null != m_settings && null != nodeRoot)
      {
        XmlNodeList nodeList = nodeRoot.SelectNodes("./entry");
        if (nodeList != null)
        {
          foreach (XmlNode entry in nodeList)
          {
            XmlNode attr = null == entry.Attributes ? null : entry.Attributes["key"];
            if (attr != null && !string.IsNullOrEmpty(attr.Value))
            {
              // Set string to InnerXml and not InnerText to handle string lists which
              // are saved as XML now 
              var decoded_string = XmlDecode(entry);
              SetString(attr.Value, decoded_string);
              XmlNode hidden = null == entry.Attributes ? null : entry.Attributes["hidden"];
              if (hidden != null && "True".Equals(hidden.Value))
                HideSettingFromUserInterface(attr.Value);
            }
          }
        }
        if (null != m_children)
        {
          var node_list = nodeRoot.SelectNodes("./child");
          if (node_list != null)
          {
            foreach (XmlNode node in node_list)
            {
              XmlNode attr = null == node.Attributes ? null : node.Attributes["key"];
              if (attr == null || string.IsNullOrEmpty(attr.Value)) continue;
              var settings = AddChild(attr.Value);
              attr = null == node.Attributes ? null : node.Attributes["hidden"];
              if (attr != null && "True".Equals(attr.Value))
                settings.HiddenFromUserInterface = true;
              settings.ParseXmlNodes(node);
            }
          }
        }
      }
    }
  }

  class PlugInSettings
  {
    private readonly System.Reflection.Assembly m_assembly; // plug-in or skin assembly
    private readonly Guid m_plugin_id;
    private readonly bool m_create_watcher;
    private readonly PlugInSettings m_all_user_settings;
    private PersistentSettings m_plugin_settings; // = null; initialized by runtime
    private PersistentSettings m_windows_position_settings; // = null; initialized by runtime
    private Dictionary<string, PersistentSettings> m_command_settings_dict; // = null; initialized by runtime

    public Guid PlugInId => m_plugin_id; // used by RhinoMac ISettingsService

    private PersistentSettings AllUserPlugInSettings => m_all_user_settings?.m_plugin_settings;

    private PersistentSettings AllUserCommandSettings(string commandName)
    {
      if (null != m_all_user_settings && !string.IsNullOrEmpty(commandName) &&
          null != m_all_user_settings.m_command_settings_dict &&
          m_all_user_settings.m_command_settings_dict.ContainsKey(commandName))
        return m_all_user_settings.m_command_settings_dict[commandName];
      return null;
    }

    /// <summary>
    /// Main settings element id attribute value, used to query valid settings section in settings XML file.
    /// </summary>
    private const string CURRENT_XML_FORMAT_VERSION = "2.0";

    private static List<PlugInSettings> g_changed_settings;
    private static Timer g_changed_timer;

    private static void InvokeSettingsSaved (Guid plugInId, bool isWriting)
    {
      // Get the plug-in
      var plug_in = PlugIns.PlugIn.Find(plugInId);
      try
      {
        // 11 December 2019 John Morse
        // https://mcneel.myjetbrains.com/youtrack/issue/RH-55242
        // Check to see if the plugInId is in the dirty (need to be saved list) which indicates
        // it is about to be written again so don't need to read it right now
        var dirty = g_changed_settings?.FirstOrDefault(item => item.PlugInId == plugInId) != null;
        // Invoke the settings changed event
        if (null == plug_in)
        {
          // C++ Plug-in
          PersistentSettingsHooks.InvokeSetingsSaved(plugInId, isWriting, dirty);
          if (UnsafeNativeMethods.CRhinoApp_IsRhinoUUID(plugInId) != 0)
            RhinoApp.OnSettingsSaved(isWriting, dirty);
        }
        else
        {
          // .NET plug-in
          plug_in.InvokeSetingsSaved(plug_in, isWriting, dirty);
        }
      }
      catch (Exception e)
      {
        Runtime.HostUtils.ExceptionReport (e);
      }
    }

    internal void OnSettingChangedSinceSave()
    {
      if (!m_enable_on_setting_changed_since_save)
        return;
      // If loading or unloading Rhino then ignore the changed events.  The shutdown code should
      // write modified settings files without using a timer or idle event.
      if (UnsafeNativeMethods.CRhinoApp_IsInitializing() || UnsafeNativeMethods.CRhinoApp_IsExiting())
        return;
      lock (g_settings_changed_lock)
      {
        if (g_changed_settings == null)
          g_changed_settings = new List<PlugInSettings> {this};
        else if (!g_changed_settings.Contains(this))
          g_changed_settings.Add(this);
        // If running on Mac OS-X then there will only be a single instance of 
        // Rhino running and file watchers won't be working so manually raise the
        // settings changed event
        if (Runtime.HostUtils.RunningOnOSX)
        {
          // 9 February 2018 John Morse
          // https://mcneel.myjetbrains.com/youtrack/issue/RH-44066
          // Need to figure this out, starting the save timer causes strange behavior on Mac, documents
          // don't get deallocated for a long, long time causing core Rhino to report memory leaks
          RhinoApp.Idle += WriteSettingsOnIdle;
        }
        else
        {
          StartChangedTimer();
        }
      }
    }

    private static void StartChangedTimer()
    {
      // Start or reset the changed timer when a value changes, if nothing
      // changes for a complete timer tick then write the settings file on
      // the next idle event.
      if (g_changed_timer == null)
        g_changed_timer = new Timer(state => WriteSettingsOnTimerTick(), null, Timeout.Infinite, Timeout.Infinite);
      else
        g_changed_timer.Change(Timeout.Infinite, Timeout.Infinite);
      const int inc = 500;
      g_changed_timer.Change(inc, inc);
    }

    private static void KillChangedTimer()
    {
      g_changed_timer?.Change(Timeout.Infinite, Timeout.Infinite);
      g_changed_timer?.Dispose();
      g_changed_timer = null;
    }

    private static void WriteSettingsOnTimerTick()
    {
      if (!Commands.Command.InCommand())
      {
        // Changes have stopped so kill the change timer
        KillChangedTimer();
        // No settings values have changed for a complete timer tick so
        // write the settings file on the next idle event.
        RhinoApp.Idle += WriteSettingsOnIdle;
      }
    }

    private static void WriteSettingsOnIdle(object sender, EventArgs e)
    {
      if (!Commands.Command.InCommand())
        FlushSettingsSavedQueue();
    }

    internal PlugInSettings Duplicate(bool createEventWatcher)
    {
      var plug_in_settings = new PlugInSettings(m_assembly, m_plugin_id, m_all_user_settings, createEventWatcher)
      {
        m_enable_on_setting_changed_since_save = false
      };
      if (plug_in_settings.m_plugin_settings == null)
        plug_in_settings.m_plugin_settings = new PersistentSettings(plug_in_settings, null, false, false, null);
      if (plug_in_settings.m_windows_position_settings == null)
        plug_in_settings.m_windows_position_settings = new PersistentSettings(plug_in_settings, null, true, false, null);
      if (plug_in_settings.m_command_settings_dict == null)
        plug_in_settings.m_command_settings_dict = new Dictionary<string, PersistentSettings>();
      plug_in_settings.MergeChangedSettingsFile(this);
      plug_in_settings.m_enable_on_setting_changed_since_save = true;
      return plug_in_settings;
    }

    internal void MergeChangedSettingsFile(PlugInSettings other)
    {
      if (other == null) return;
      if (m_plugin_id != other.m_plugin_id) return;
      var enable_changed = m_enable_on_setting_changed_since_save;
      m_enable_on_setting_changed_since_save = false;
      //this.m_command_settings_dict
      m_plugin_settings.MergeChangedSettingsFile(other.m_plugin_settings);
      // Merge command settings from other commands dictionary
      foreach (var item in m_command_settings_dict)
      {
        // Get settings for command in other dictionary
        PersistentSettings command_settings;
        other.m_command_settings_dict.TryGetValue(item.Key, out command_settings);
        // Merge settings from other commands dictionary
        item.Value.MergeChangedSettingsFile(command_settings);
      }
      // Check for commands in other commands dictionary that are not currently
      // in this commands dictionary
      foreach (var item in other.m_command_settings_dict)
      {
        // Command already in the dictionary so there is nothing to do
        if (m_command_settings_dict.ContainsKey(item.Key)) continue;
        // Make a new settings class
        var settings = new PersistentSettings(this, null, false, true, item.Key);
        // Merge settings from other
        settings.MergeChangedSettingsFile(item.Value);
        // Add to commands dictionary
        m_command_settings_dict.Add(item.Key, settings);
      }
      ClearChangedSinceSavedFlag();
      m_enable_on_setting_changed_since_save = enable_changed;
    }

    /// <summary>
    /// Should get call from PersistentSettingsHooks.Save when core Rhino has
    /// called SaveProfile on a application settings class and wants to update
    /// the applications settings XML file.
    /// </summary>
    /// <param name="shuttingDown"></param>
    /// <returns></returns>
    internal bool SaveSettingsUnmanagedHook(bool shuttingDown)
    {
      lock (g_settings_changed_lock)
      {
        // 08 June 2020 John Morse
        // https://mcneel.myjetbrains.com/youtrack/issue/RH-58733
        // Check to see if the file being saved is in the changed list, if
        // it is then remove it to avoid multiple writes for the same change
        // event.
        for (var i = (g_settings_changed_list?.Count ?? 0) - 1; i >= 0; i--)
          if (g_settings_changed_list[i].PlugInId == PlugInId)
            g_settings_changed_list.RemoveAt(i);
        // If the list is empty then kill the changed timer and the changed
        // hook to avoid getting changed notifications for an empty list
        if (shuttingDown || (g_settings_changed_list?.Count ?? 0) < 1)
        {
          RhinoApp.Idle -= WriteSettingsOnIdle;
          KillChangedTimer();
          g_changed_settings?.Clear();
          g_changed_settings = null;
        }
        return WriteSettings(shuttingDown);
      }
    }

    public static void FlushSettingsSavedQueue()
    {
      try
      {
        lock (g_settings_changed_lock)
        {
          RhinoApp.Idle -= WriteSettingsOnIdle;
          KillChangedTimer();
          if (g_changed_settings == null)
            return;
          foreach (var item in g_changed_settings.Where(item => item.ContainsChangedSinceSavedValues()))
          {
            item.WriteSettings(false);
            // If running on Mac OS-X then there will only be a single instance of 
            // Rhino running and file watchers won't be working so manually raise the
            // settings changed event
            if (PersistentSettings.Service.RaiseChangedEventAfterWriting)
              InvokeSettingsSaved(item.m_plugin_id, true);
          }
          g_changed_settings.Clear();
          g_changed_settings = null;
        }
      }
      catch
      {

        lock (g_settings_changed_lock)
        {
          KillChangedTimer();
          g_changed_settings?.Clear();
          g_changed_settings = null;
        }
      }
    }

    /// <summary>
    /// Computes folder to read or write settings files.
    /// </summary>
    private string SettingsFileFolder(bool localSettings)
    {
      return PlugIns.PlugIn.SettingsDirectoryHelper(localSettings, m_assembly, m_plugin_id);
    }

    /// <summary>
    /// Get the current Rhino scheme name as a valid file path name without any
    /// spaces
    /// </summary>
    /// <returns>
    /// Returns the current Rhino scheme name as a valid file path name without
    /// any spaces
    /// </returns>
    internal static string GetRhinoSchemeRegistryPath()
    {
      // Parse the scheme name
      string scheme;
      using (var sw = new Runtime.InteropWrappers.StringWrapper())
      {
        var ptr_wstring = sw.NonConstPointer;
        UnsafeNativeMethods.CRhinoApp_GetRhinoSchemeRegistryPath(false, ptr_wstring);
        scheme = sw.ToString().Trim();
        const char replace_char = '_';
        if (string.IsNullOrWhiteSpace(scheme))
          scheme = string.Empty;
        else
          scheme = Path.GetInvalidFileNameChars().Aggregate(scheme, (current, c) => current.Replace(c, replace_char));
        scheme = scheme.Replace(' ', replace_char).Replace(':', replace_char);
      }
      return scheme;
    }

    /// <summary>
    /// Computes full path to settings file to read or write.
    /// </summary>
    private string SettingsFileName(bool localSettings, bool windowPositions)
    {
      // Parse the scheme name
      string scheme = GetRhinoSchemeRegistryPath();
      var file_name = windowPositions ? "window_positions" : "settings";
      if (!string.IsNullOrEmpty(scheme))
        file_name = file_name + "-" + scheme;
      return Path.Combine(SettingsFileFolder(localSettings), file_name + ".xml");
    }

    /// <summary>PersistentSettingsManager constructor.</summary>
    /// <param name="pluginAssembly">Requires a valid Skin, DLL or PlugIn object to attach to.</param>
    /// <param name="pluginId">Requires a PlugIn Id to attach to.</param>
    /// <param name="allUserSettings">All user setting to compare for changes.</param>
    /// <param name="createWatcher">
    /// If true then a file watcher is created when the settings are initially
    /// read and will raise PlugIn.SettingsSaved events when the plug-in is 
    /// unloading or when PlugIn.SaveSettings() is called.
    /// </param>
    internal PlugInSettings(System.Reflection.Assembly pluginAssembly, Guid pluginId, PlugInSettings allUserSettings,
      bool createWatcher)
    {
      m_assembly = pluginAssembly;
      m_plugin_id = pluginId;
      m_create_watcher = createWatcher;
      m_all_user_settings = allUserSettings;
    }

    /// <summary>PersistentSettingsManager constructor</summary>
    /// <param name="pluginId"></param>
    /// <param name="allUserSettings"></param>
    /// <param name="createWatcher">
    /// If true then a file watcher is created when the settings are initially
    /// read and will raise PlugIn.SettingsSaved events when the plug-in is 
    /// unloading or when PlugIn.SaveSettings() is called.
    /// </param>
    internal PlugInSettings(Guid pluginId, PlugInSettings allUserSettings, bool createWatcher)
    {
      m_plugin_id = pluginId;
      m_create_watcher = createWatcher;
      m_all_user_settings = allUserSettings;
    }

    /// <summary>
    /// Gets the Plug-in settings associated with this plug-in, if this is the first time called then
    /// the plug-in settings member variable will get initialized and if a settings file exists it
    /// will get loaded.
    /// </summary>
    public PersistentSettings PluginSettings
    {
      get
      {
        if (m_plugin_settings == null)
          ReadSettings(false);
        return m_plugin_settings;
      }
    }

    /// <summary>
    /// Gets the Plug-in settings associated with this plug-in, if this is the first time called then
    /// the plug-in settings member variable will get initialized and if a settings file exists it
    /// will get loaded.
    /// </summary>
    public PersistentSettings WindowPositionSettings
    {
      get
      {
        if (m_plugin_settings == null)
          ReadSettings(false);
        return m_windows_position_settings;
      }
    }

    /// <summary>
    /// Gets the PersistentSettings associated with the specified command.  If the settings file
    /// has not been previously loaded and exists then it will get read.  If the command name is
    /// not in the command settings dictionary then a new entry will get created and its settings
    /// will be returned.
    /// </summary>
    /// <param name="name">Command name key to search for and/or add.</param>
    /// <returns>Returns PersistentSettings object associated with command name on success or null on error.</returns>
    public PersistentSettings CommandSettings(string name)
    {
      if (m_command_settings_dict == null)
      {
        ReadSettings(false);
        if (m_command_settings_dict == null)
          return null;
      }
      if (m_command_settings_dict.ContainsKey(name))
        return m_command_settings_dict[name];
      // There were no settings available for the command, so create one
      // for writing
      var settings = new PersistentSettings(this, AllUserCommandSettings (name), false, true, name);
      m_command_settings_dict[name] = settings;
      return m_command_settings_dict[name];
    }

    public bool ReadSettings(bool updateDefaults)
    {
      var result = false;
      // https://app.raygun.io/dashboard/6bsfxg/errors/841187247
      // 19 August 2015 John Morse
      // Don't allow this function to throw an exception
      try
      {
        // If reading the local settings
        if (null != m_all_user_settings)
        {
          if (updateDefaults)
          {
            // Clear the plug-in and commands settings dictionary values so that
            // properties will revert to default values.  Properties with default
            // values do not get written to the settings file so you must clear
            // the dictionaries so old, non default values, get replace with default
            // values as appropriate.
            m_plugin_settings = null;
            m_command_settings_dict = null;
          }
          // TODO: 28 Jan 2014 S. Baer
          // I'm working on some other code right now, but just glancing through
          // the next few lines don't quite make sense to me. If result is false
          // from the first call, does it make sense to set it to true in the
          // next line?
          // 19 August 2015 John Morse
          // Steve, yes it makes sense, the first attempt is looking for all
          // user settings files and will return false if they do not exist,
          // you still want to go ahead and read local user settings files.
          m_enable_on_setting_changed_since_save = false;
          // First read All User settings (WINDOWS ONLY, same folder on Mac)
          result = PersistentSettings.Service.SupportsAllUsers && m_all_user_settings.ReadSettingsHelper(false, false);
          // Now read the local settings
          if (ReadSettingsHelper(true, false))
            result = true;
          // Try to read the WindowPositions settings file
          ReadSettingsHelper(true, true);
          m_enable_on_setting_changed_since_save = true;
        }
      }
      catch (Exception e)
      {
        Runtime.HostUtils.ExceptionReport(e);
      }
      return result;
    }

    private bool m_enable_on_setting_changed_since_save = true;
    internal bool EnableOnSettingChangedSinceSave => m_enable_on_setting_changed_since_save;

    class SettingsXmlException : XmlException
    {
      public SettingsXmlException(string filename, Exception e)
        : base(FormatXmlExceptionMessage(filename, e), e.InnerException)
      {
      }
    }

    private static string FormatXmlExceptionMessage(string filename, Exception exception)
    {
      var message = $"{exception.Message}{Environment.NewLine}FileName: {filename}{Environment.NewLine}Content:{Environment.NewLine}";
      try
      {
        // Read the first 10 lines of the settings file
        using (var stream = new StreamReader(filename))
        {
          for (var i = 0; stream.Peek() > 0 && i < 10; i++)
            message += stream.ReadLine();
        }
      }
      catch (Exception e)
      {
        message += $"Exception extracting first 10 lines:{Environment.NewLine}{e.Message}";
      }
      return message;
    }

    /// <summary>
    /// Reads existing settings for a plug-in and its associated commands.
    /// Clears the dirty flag for the settings. 
    /// </summary>
    /// <returns>
    /// true if settings are successfully read. false if there was no existing
    /// settings file to read, or if a read lock could not be acquired.
    /// </returns>
    public bool ReadSettingsHelper(bool localSettings, bool windowPositions)
    {
      if (!windowPositions && m_plugin_settings == null)
      {
        m_plugin_settings = new PersistentSettings(this, AllUserPlugInSettings, false, false, null);
        // If AllUserSettings is not null then we are reading local settings, when
        // reading local settings first get values previously read from the All Users
        // location and add them to the local dictionary so the settings will propagate
        // to the current user.
        if (localSettings && null != m_all_user_settings)
          m_plugin_settings.CopyFrom(AllUserPlugInSettings);
      }

      if (!windowPositions && m_command_settings_dict == null)
      {
        m_command_settings_dict = new Dictionary<string, PersistentSettings>();
        // If AllUserSettings is not null then we are reading local settings, when
        // reading local settings first get values previously read from the All Users
        // location and add them to the local dictionary so the settings will propagate
        // to the current user.
        if (null != m_all_user_settings && null != m_all_user_settings.m_command_settings_dict)
        {
          foreach (var item in m_all_user_settings.m_command_settings_dict)
          {
            // Make a new settings dictionary to associate with this command
            var settings = new PersistentSettings(this, item.Value, false, true, item.Key);
            // Copy settings from global command dictionary to local dictionary
            settings.CopyFrom(item.Value);
            // Add the settings to the local dictionary
            m_command_settings_dict.Add(item.Key, settings);
          }
        }
      }

      if (windowPositions && m_windows_position_settings == null)
      {
        m_windows_position_settings = new PersistentSettings(this, null, true, false, null);
      }

      var settings_file_name = SettingsFileName(localSettings, windowPositions);

      // Need to listen for directory and/or file creation events to know
      // when the settings change the first time.
      if (!windowPositions && m_create_watcher)
        AttachFileWatcher(settings_file_name);

      Stream stream = null;
      try
      {
        if (PersistentSettings.Service.SupportsPlist)
        {
          PersistentSettings.Service.ReadFromPlist(
            m_plugin_id,
            ref m_plugin_settings,
            (commandName) =>
            {
              // Called by reader to get the PersistentSettings associated with a command name
              var entries = new PersistentSettings(this, AllUserCommandSettings (commandName), false, true, commandName);
              if (null != m_all_user_settings && m_command_settings_dict.ContainsKey(commandName))
                m_command_settings_dict[commandName].CopyFrom(entries);
              else
                m_command_settings_dict[commandName] = entries;
              return m_command_settings_dict[commandName];
            });
        }
        else // Use service to read XML into a stream then parse the XML
        {
          // Get stream from service provider, the stream should contain the XML to parse
          stream = PersistentSettings.Service.ReadSettings(settings_file_name);
          if (stream == null)
            return false; // File not found or error reading the file

          var doc = new XmlDocument();
          var ns = new XmlNamespaceManager(doc.NameTable);
          ns.AddNamespace ("xml", "http://www.w3.org/XML/1998/namespace");

          // https://app.raygun.io/dashboard/6bsfxg/errors/841187247
          // 19 August 2015 John Morse
          // Catch parsing exception
          var loaded = true;
          try
          {
            doc.Load (stream);
          }
          catch (Exception)
          {
            loaded = false;
            // Runtime.HostUtils.ExceptionReport(new SettingsXmlException (settings_file_name, ex));
          }

          stream.Dispose();

          if (!loaded)
            return false;

          // Check the version information stored in the "id" attribute
          var root_node = doc.SelectSingleNode("/settings[@xml:id]", ns) ??
                          doc.SelectSingleNode("/settings[@id]", ns);

          if (root_node == null || root_node.Attributes == null)
            return false; // Root settings node not found or has no attributes
                          // Get the "id" attribute
          var id_attr = root_node.Attributes["id"];
          if (id_attr == null)
            return false; // "id" attribute not found

          //15 Feb 2018 S. Baer (RH-43828)
          // Always use CultureInfo.InvariantCulture instead of attempting to construct a 1033 CultureInfo.
          // A 1033 CultureInfo on some people's computers appear to be able to have their OS tweaked in a
          // way that commas are still used as the separator instead of periods.

          // Convert the current version into a double for comparison
          var current_version = double.Parse(CURRENT_XML_FORMAT_VERSION, SettingValue.ParseCulture);
          // Try to convert the "id" attribute string into a double then make
          // sure it is equal to the current major version.
          double version;
          if (!double.TryParse(id_attr.Value, NumberStyles.Float, SettingValue.ParseCulture, out version))
            return false;

          if ((int)version != (int)current_version)
            return false;

          // Parse main <plug-in> entry, if it exists, for plug-in settings
          var plugin_settings = windowPositions ? m_windows_position_settings : m_plugin_settings;
          plugin_settings.ParseXmlNodes(root_node.SelectSingleNode ("./settings"));

          // Look for <command> nodes which will have a command name property that identifies the plug-in command settings
          var command_nodes = windowPositions ? null : root_node.SelectNodes("./command");
          if (command_nodes != null)
          {
            foreach (XmlNode command_node in command_nodes)
            {
              var attr_collection = command_node.Attributes;
              if (attr_collection == null)
                continue;
              var attr = attr_collection.GetNamedItem("name");
              if (null == attr || string.IsNullOrEmpty(attr.Value)) continue;
              var entries = new PersistentSettings(this, AllUserCommandSettings (attr.Value), false, true, attr.Value);
              entries.ParseXmlNodes(command_node);
              if (null != m_all_user_settings && m_command_settings_dict.ContainsKey(attr.Value))
                m_command_settings_dict[attr.Value].CopyFrom(entries);
              else
                m_command_settings_dict[attr.Value] = entries;
            }
          }
        }
      }
      catch (Exception ex)
      {
        Runtime.HostUtils.ExceptionReport(ex);
        return false;
      }
      finally
      {
        // Reading a settings file will populate the settings dictionaries by calling
        // SetValue() which will cause the ChangedSinceSave property to get set for each item
        // read.  The ChangedSinceSave flag is used to determine when a settings file needs to
        // be flushed so clear the changed flag when done reading a file.
        ClearChangedSinceSavedFlag();
        if (stream != null)
        {
          stream.Close();
          stream.Dispose();
        }
      }

      return true;
    }

#region File watcher specific
    internal void DetachWatchers()
    {
      if (m_watcher_timer != null)
      {
        m_watcher_timer.Dispose();
        m_watcher_timer = null;
      }

      // copy watchers out to an array before disposing so there aren't any
      // potential bugs where other threads are attempting to modify the
      // watchers list during this process
      var values = m_watchers.Values.ToArray();
      m_watchers.Clear();
      foreach (var value in values)
      {
        var watcher = value.Value;
        watcher.EnableRaisingEvents = false;
        watcher.Dispose();
      }
    }

    protected void AttachFileWatcher(string fileName)
    {
      if (PersistentSettings.Service.UseFileWatchers == false)
        return; // No need for file watchers on Mac
      if (string.IsNullOrEmpty(fileName)) return;
      // If already watching then bail
      if (m_watchers.ContainsKey(fileName)) return;
      var folder = Path.GetDirectoryName(fileName);
      if (null == folder) return;
      if (!Directory.Exists(folder))
      {
        // Attach directory file watcher to the parent directory
        // so we can tell when the settings directory gets created
        var parent = Path.GetDirectoryName(folder);
        if (parent == null || m_watchers.ContainsKey(parent) || !Directory.Exists(parent)) return;
        var watcher = new FileSystemWatcher()
        {
          Path = parent,
          Filter = Path.GetFileName(folder),
          NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.CreationTime | NotifyFilters.FileName | NotifyFilters.LastAccess | NotifyFilters.Size | NotifyFilters.DirectoryName
        };
        //watcher.Deleted += WatcherSettingsFolderCreated;
        watcher.Changed += WatcherSettingsFolderCreated;
        //watcher.Renamed += WatcherSettingsFolderCreated;
        watcher.Created += WatcherSettingsFolderCreated;
        watcher.EnableRaisingEvents = true;
        m_watchers.Add(parent, new KeyValuePair<string, FileSystemWatcher>(fileName, watcher));
        return;
      }
      try
      {
        // The file is already being watched, nothing to do.
        if (m_watchers.ContainsKey(fileName)) return;
        // Attach file watcher to the settings file, the settings folder MUST
        // exist or an exception will get thrown.
        var watcher = new FileSystemWatcher
        {
          Path = Path.GetDirectoryName(fileName),
          Filter = Path.GetFileName(fileName),
          NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.CreationTime | NotifyFilters.FileName | NotifyFilters.LastAccess | NotifyFilters.Size
        };
        watcher.Created += WatcherOnChanged;
        watcher.Deleted += WatcherOnChanged;
        watcher.Changed += WatcherOnChanged;
        watcher.EnableRaisingEvents = true;
        m_watchers.Add(fileName, new KeyValuePair<string, FileSystemWatcher>(fileName, watcher));
      }
      catch (Exception exception)
      {
        Runtime.HostUtils.ExceptionReport(exception);
      }
    }

    private void WatcherSettingsFolderCreated(object sender, FileSystemEventArgs e)
    {
      var parent = Path.GetDirectoryName(e.FullPath);
      if (null == parent) return;
      KeyValuePair<string, FileSystemWatcher> found;
      if (!m_watchers.TryGetValue(parent, out found)) return;
      AttachFileWatcher(found.Key);
    }

    /// <summary>
    /// SettingsSaved event to raise, used by WatcherTimerCallback to
    /// add events to the queue processed by SettingsChangedOnIdle.
    /// </summary>
    class SettingsSavedEvent
    {
      public SettingsSavedEvent(Guid plugInId, bool writing)
      {
        PlugInId = plugInId;
        Writing = writing;
      }
      public Guid PlugInId { get; private set; }
      public bool Writing { get; private set; }
    }
    /// <summary>
    /// List of SettingsSaved events to raise
    /// </summary>
    private static List<SettingsSavedEvent> g_settings_changed_list;
    private static readonly object g_settings_changed_lock = new object();
    /// <summary>
    /// Rhino.Idle event callback. Only raise the settings changed event
    /// during an OnIdle event when a command is not running.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private static void SettingsChangedOnIdle(object sender, EventArgs e)
    {
      // Don't update settings while a command is running.
      if (Commands.Command.InCommand())
        return;
      FlushSettingsChangedEventQueue();
    }

    internal static void FlushSettingsChangedEventQueue()
    {
      RhinoApp.Idle -= SettingsChangedOnIdle;
      try
      {
        // Lock the settings changed list while raising changed events
        lock (g_settings_changed_lock)
        {
          // Process the changed event list
          if (g_settings_changed_list != null)
          {
            foreach (var saved_event in g_settings_changed_list)
              InvokeSettingsSaved(saved_event.PlugInId, saved_event.Writing);
          }
          g_settings_changed_list = null;
        }
      }
      catch
      {
        lock (g_settings_changed_lock)
          g_settings_changed_list = null;
      }
    }

    private void WatcherTimerCallback(object state)
    {
      // Ready to raise the SettingsSaved event so kill the timer
      // 30 July 2019 John Morse - RH-51762
      // Turn timer off
      m_watcher_timer?.Change(Timeout.Infinite, Timeout.Infinite);
      // Dispose of timer
      m_watcher_timer?.Dispose();
      m_watcher_timer = null;
      //
      // Raise the settings change event during Rhino idle processing
      // when a command is not running.
      //
      lock (g_settings_changed_lock)
      {
        if (g_settings_changed_list == null)
        {
          g_settings_changed_list = new List<SettingsSavedEvent>();
          RhinoApp.Idle += SettingsChangedOnIdle;
        }
        // Only add changed item to the list if it is not currently in the list
        var found = g_settings_changed_list.FirstOrDefault(item => item.PlugInId == m_plugin_id && item.Writing == m_writing) != null;
        if (!found)
          g_settings_changed_list.Add(new SettingsSavedEvent(m_plugin_id, m_writing));
      }
      //
      // Only do the following if the OnIdle processing is turned off above
      //
      //PlugInSettings.InvokeSettingsSaved(m_plugin_id, m_writing);
      m_writing = false;
    }

    private void WatcherOnChanged(object sender, FileSystemEventArgs e)
    {
      // Uses a timer in an attempt to only raise the SettingsSaved event once
      const int time = 500;
      // If no timer exists then create one
      if (null == m_watcher_timer)
        m_watcher_timer = new Timer(WatcherTimerCallback, null, time, Timeout.Infinite);
      else
      {
        // Timer existed which means there was another file event so reset the
        // timer and start waiting again
        m_watcher_timer.Change(Timeout.Infinite, Timeout.Infinite);
        m_watcher_timer.Change(time, Timeout.Infinite);
      }
    }

    private Timer m_watcher_timer;
    private Dictionary<string, KeyValuePair<string, FileSystemWatcher>> m_watchers = new Dictionary<string, KeyValuePair<string, FileSystemWatcher>>();
#endregion File watcher specific


    /// <summary>
    /// Check the plug-in and command settings dictionaries for values that have changed.
    /// </summary>
    /// <returns>Returns true if either the plug-ins or commands dictionaries contains a value that has changed.</returns>
    public bool ContainsChangedSinceSavedValues()
    {
      if (null != m_plugin_settings && m_plugin_settings.ContainsChangedValues())
        return true; // Plug-in settings contains a modified value
      return (null != m_command_settings_dict && m_command_settings_dict.Any(item => item.Value.ContainsChangedValues()));
    }

    public void ClearChangedSinceSavedFlag()
    {
      if (null != m_plugin_settings)
        m_plugin_settings.ClearChangedFlag();
      if (null != m_command_settings_dict)
        foreach (var persistent_settingse in m_command_settings_dict)
          persistent_settingse.Value.ClearChangedFlag();
    }

    /// <summary>
    /// Check the plug-in and command settings dictionaries for values other than default value.
    /// </summary>
    /// <returns>Returns true if either the plug-ins or commands dictionaries contains a modified item otherwise false.</returns>
    public bool ContainsModifiedValues()
    {
      if (null != m_plugin_settings && m_plugin_settings.ContainsModifiedValues(AllUserPlugInSettings))
        return true; // Plug-in settings contains a modified value
      if (null != m_command_settings_dict)
        foreach (var item in m_command_settings_dict)
          if (item.Value.ContainsModifiedValues(AllUserCommandSettings(item.Key)))
            return true; // This command's settings have been modified
      return false;
    }

    internal bool WriteSettings(bool shuttingDown)
    {
      // The following, commented out code, would attempt to write to the All Users (global) file first, if that succeeded then the
      // current user file would get hosed and the All Users file would be the only remaining file. Steve, Brian and John decided
      // that settings would get read from the All Users area and those values would get replaced by the ones in Current User and
      // when closing values that were different than the default value or that did not match the ones in All Users would get written
      // to the Current User section and that we would provide a tool for migrating settings to the All User area.
      //bool result = this.WriteSettingsHelper(false);
      //if (result)
      //  this.DeleteSettingsFile(true); // All User file written successfully so delete the current user file if it exists
      //else
      //  result = this.WriteSettingsHelper(true);
      //return result;

      // Write windows positions file
      if (m_windows_position_settings != null)
      {
        var keys = m_windows_position_settings.Keys;
        var empty = (keys == null || keys.Count < 1);
        if (empty)
        {
          keys = m_windows_position_settings.ChildKeys;
          empty = (keys == null || keys.Count < 1);
        }
        if (!empty)
          WriteSettingsHelper(shuttingDown, true, true);
      }
      // Simply write the settings to current user, if the code above is run then comment this out, see comment at top of this
      // function for more information.
      return WriteSettingsHelper(shuttingDown, true, false);
    }

    private void DeleteSettingsFile(bool localSettings, bool windowPostions)
    {
      PersistentSettings.Service?.DeleteSettingsFile(SettingsFileName (localSettings, windowPostions));
    }
    /// <summary>
    /// Flushes the current settings to the user's roaming directory. 
    /// If an xml file for the guid already exists, it attempts to update
    /// the file in order to maintain comments, etc. If the xml is corrupt
    /// or the file does not exist, it writes out a new file.
    /// </summary>
    /// <param name="shuttingDown"></param>
    /// <param name="localSettings"></param>
    /// <param name="windowPositions"></param>
    /// <returns>
    /// true if settings where flushed to disk, otherwise false. 
    /// </returns>
    internal bool WriteSettingsHelper(bool shuttingDown, bool localSettings, bool windowPositions)
    {
      if (windowPositions && m_windows_position_settings == null)
      {
        DeleteSettingsFile(true, true);
        return true;
      }
      if (windowPositions)
      {
        var resetting_dont_write = (m_windows_position_settings?.ResettingDontWrite ?? false)
          || (m_plugin_settings?.ResettingDontWrite ?? false)
          || (/*UnsafeNativeMethods.CRhinoApp_IsRhinoUUID(PlugInId) != 0 &&*/  UnsafeNativeMethods.CRhCommandsCallbacks_ResetWindowPositions())
          || UnsafeNativeMethods.CRhCommandsCallbacks_ResetSettings();
        if (resetting_dont_write)
        {
          DeleteSettingsFile(localSettings, true);
          return true;
        }
      }
      else
      {
        var resetting_dont_write = (m_plugin_settings != null && m_plugin_settings.ResettingDontWrite) || UnsafeNativeMethods.CRhCommandsCallbacks_ResetSettings();
        ClearChangedSinceSavedFlag();
        if (resetting_dont_write || !ContainsModifiedValues())
        {
          DeleteSettingsFile(localSettings, false);
          return true;
        }
      }
      var success =  PersistentSettings.Service.SupportsPlist
        // If the service supports direct writing (Mac PLIST)
        ? !windowPositions && PersistentSettings.Service.WriteToPlist(shuttingDown, m_plugin_settings, m_command_settings_dict, WriteSettingsCallback)
        // Otherwise write XML to a stream and send the stream to the settings service for serialzation
        : PersistentSettings.Service.WriteSettings(shuttingDown, WriteToStream(windowPositions), SettingsFileName(localSettings, windowPositions), WriteSettingsCallback);
      return success;
    }
    private void WriteSettingsCallback(bool isShuttingDown, bool done)
    {
      if (done) // Finished writing file
      {
        // If NOT using file watchers clear the m_writing flag
        if (PersistentSettings.Service.UseFileWatchers == false)
          m_writing = false;
      }
      else // Starting to write the file
      {
        // Tells file watchers we are writing this settings file
        m_writing = true;

        // to prevent settings changed notifications from getting sent to this
        // instance when Rhino is shutting down.
        if (isShuttingDown)
          DetachWatchers();
      }
    }
    private bool m_writing;
    /// <summary>
    /// Constructs a temporary file and write plug-in and plug-in command settings to the temp file.
    /// Only writes PersistentSettings that contain one or more item with a value that differs
    /// from the default value.
    /// </summary>
    private Stream WriteToStream(bool windowPositions)
    {
      try
      {
        // Only write contents if the file contains modified values otherwise
        // just write an empty file, this is needed to so the file watcher
        // can tell when settings change and notify Rhino or the plug-in of 
        // the change.
        PersistentSettings settings = null;
        if (windowPositions) // Window positions - if it contains modified values
          settings = (m_windows_position_settings?.ContainsModifiedValues (null) ?? false) ? m_windows_position_settings : null;
        else // plug-in settings - if the plug-in settings contains modified values
          settings = ContainsModifiedValues () ? m_plugin_settings : null;
        // Write settings to a memory stream then flush to temp_file
        // Pass null command settings dictionary for window positions, only the
        // plug-in settings should contain a commands section
        return ToStream(settings, windowPositions ? null : m_command_settings_dict);
      }
      catch (Exception ex)
      {
        Runtime.HostUtils.ExceptionReport(ex);
        return null;
      }
    }
    /// <summary>
    /// Write the specified settings to a MemoryStream
    /// </summary>
    /// <param name="settings">
    /// The settings to write, if null the XML document's settings section will be empty
    /// otherwise; it will contain any settings that are not equal to their default value.
    /// </param>
    /// <param name="commandSettings">
    /// Should be null when writing window positions XML file.  If writing plug-in settings
    /// then pass the commands dictionary.
    /// </param>
    /// <returns>
    /// Returns a stream containing the settings XML document on success otherwise; null.
    /// </returns>
    Stream ToStream(PersistentSettings settings, Dictionary<string, PersistentSettings>commandSettings)
    {
      MemoryStream stream = null;
      try
      {
        var xml_writer = XmlWriter.Create(
          stream = new MemoryStream(),
          new XmlWriterSettings
          {
            Encoding = Encoding.UTF8,
            Indent = true,
            IndentChars = "  ",
            OmitXmlDeclaration = false,
            NewLineOnAttributes = false,
            CloseOutput = false
          });
        
        xml_writer.WriteStartDocument();
        {
          xml_writer.WriteStartElement("settings");
          {
            xml_writer.WriteAttributeString("id", CURRENT_XML_FORMAT_VERSION);

            // Write settings section for plug-in or window position settings,
            // settings will be null if all values are default so the "settings"
            // section will be empty
            settings?.WriteXmlElement(xml_writer, "settings", "", "", AllUserPlugInSettings);

            // Write command section if provided (will be null for window position settings)
            if (commandSettings != null)
              foreach (var item in commandSettings)
                item.Value.WriteXmlElement(xml_writer, "command", "name", item.Key, AllUserCommandSettings (item.Key));
          }
          xml_writer.WriteEndElement();
        }
        xml_writer.WriteEndDocument();

        xml_writer.Flush();
        xml_writer.Close();

        return stream;
      }
      catch (Exception ex) 
      {
        Runtime.HostUtils.ExceptionReport(ex);
        stream?.Dispose();
        return null;
      }
    }
  }

  class PersistentSettingsManager
  {
    static readonly List<PersistentSettingsManager> g_all_managers = new List<PersistentSettingsManager>();
    System.Reflection.Assembly m_assembly;
    /// <summary>
    /// If this settings PersistentSettingsManager is created by plug-in provided DLL then save the plug-in ID
    /// so that when the plug-in requests its setting it will get the same PersistentSettingsManager
    /// </summary>
    internal readonly Guid m_plugin_id;
    private readonly PlugInSettings m_settings_local;

    /// <summary>
    /// PersistentSettingsManager constructor.
    /// </summary>
    /// <param name="assembly">Requires a valid Assembly object to attach to.</param>
    /// <param name="pluginId">Optional plug-in Id which identifies the plug-in associated with this settings class.</param>
    PersistentSettingsManager(System.Reflection.Assembly assembly, Guid pluginId)
    {
      m_assembly = assembly;
      m_plugin_id = pluginId;
      m_settings_local = new PlugInSettings(m_assembly, m_plugin_id, new PlugInSettings(m_assembly, m_plugin_id, null, true), true);
    }
    /// <summary>
    /// PersistentSettingsManager constructor.
    /// </summary>
    /// <param name="pluginId">Requires a valid pluginId to attach to</param>
    PersistentSettingsManager(Guid pluginId)
    {
      m_plugin_id = pluginId;
      m_settings_local = new PlugInSettings(pluginId, new PlugInSettings(pluginId, null, true), true);
    }
    /// <summary>
    /// PersistentSettingsManager constructor.
    /// </summary>
    /// <param name="skin"></param>
    PersistentSettingsManager(Rhino.Runtime.Skin skin)
    {
      m_plugin_id = Guid.Empty;
      System.Reflection.Assembly assembly = skin.GetType().Assembly;
      m_settings_local = new PlugInSettings(assembly, m_plugin_id, new PlugInSettings(assembly, m_plugin_id, null, true), true);
    }

    public static PersistentSettingsManager Create(Guid pluginId)
    {
      if (pluginId == Guid.Empty)
        throw new System.ComponentModel.InvalidEnumArgumentException($"pluginId Can not be Guid.Empty");
      for (int i = 0; i < g_all_managers.Count; i++)
        if (g_all_managers[i].m_plugin_id == pluginId)
          return g_all_managers[i];
      var ps = new PersistentSettingsManager(pluginId);
      g_all_managers.Add(ps);
      return ps;
    }

    public static PersistentSettingsManager Create(PlugIns.PlugIn plugin)
    {
      Guid plugin_id = plugin.Id;
      System.Reflection.Assembly assembly = plugin.GetType().Assembly;
      for (int i = 0; i < g_all_managers.Count; i++)
      {
        if (g_all_managers[i].m_assembly == assembly || g_all_managers[i].m_plugin_id == plugin_id)
        {
          g_all_managers[i].m_assembly = assembly;
          return g_all_managers[i];
        }
      }
      var ps = new PersistentSettingsManager(assembly, plugin_id);
      g_all_managers.Add(ps);
      return ps;
    }

    public static PersistentSettingsManager Create(Runtime.Skin skin)
    {
      System.Reflection.Assembly assembly = skin.GetType().Assembly;
      for (int i = 0; i < g_all_managers.Count; i++)
        if (g_all_managers[i].m_assembly == assembly)
          return g_all_managers[i];
      var ps = new PersistentSettingsManager(assembly, Guid.Empty);
      g_all_managers.Add(ps);
      return ps;
    }

    internal PlugInSettings InternalPlugInSettings { get { return m_settings_local; } }
    /// <summary>
    /// Gets the Plug-in settings associated with this plug-in, if this is the first time called then
    /// the plug-in settings member variable will get initialized and if a settings file exists it
    /// will get loaded.
    /// </summary>
    public PersistentSettings PluginSettings { get { return m_settings_local.PluginSettings; } }
    /// <summary>
    /// Gets the window position settings associated with this plug-in, if this is the first time called then
    /// the window position settings member variable will get initialized and if a settings file exists it
    /// will get loaded.
    /// </summary>
    public PersistentSettings WindowPositionSettings { get { return m_settings_local.WindowPositionSettings; } }
    /// <summary>
    /// Gets the PersistentSettings associated with the specified command.  If the settings file
    /// has not been previously loaded and exists then it will get read.  If the command name is
    /// not in the command settings dictionary then a new entry will get created and its settings
    /// will be returned.
    /// </summary>
    /// <param name="name">Command name key to search for and/or add.</param>
    /// <returns>Returns PersistentSettings object associated with command name on success or null on error.</returns>
    public PersistentSettings CommandSettings(string name)
    {
      return m_settings_local.CommandSettings(name);
    }
    /// <summary>
    /// Check the plug-in and command settings dictionaries for values that have changed.
    /// </summary>
    /// <returns>Returns true if either the plug-ins or commands dictionaries contains a changed item otherwise false.</returns>
    public bool ContainsChangedValues()
    {
      return m_settings_local.ContainsChangedSinceSavedValues();
    }
    /// <summary>
    /// Clear changed flag for all values
    /// </summary>
    public void ClearChangedFlag()
    {
      m_settings_local.ClearChangedSinceSavedFlag();
    }
    /// <summary>
    /// Check the plug-in and command settings dictionaries for values other than default value.
    /// </summary>
    /// <returns>Returns true if either the plug-ins or commands dictionaries contains a modified item otherwise false.</returns>
    public bool ContainsModifiedValues()
    {
      return m_settings_local.ContainsModifiedValues();
    }
    /// <summary>
    /// If they exist and contain modified values write global settings first then local settings.
    /// </summary>
    /// <param name="shuttingDown">
    /// Will be true if this is getting called when Rhino is closing otherwise it will be false.
    /// </param>
    /// <returns>Returns true if local settings were successfully written.</returns>
    public bool WriteSettings(bool shuttingDown)
    {
      return m_settings_local.WriteSettings(shuttingDown);
    }

    public bool ReadSettings(bool updateDefaults)
    {
      return m_settings_local.ReadSettings(updateDefaults);
    }
  }
}

#endif
