using Microsoft.Win32;
using System;
using System.Collections.Specialized;
using System.Diagnostics;
// using System.Linq;
using System.Net;
using System.Windows.Forms;

namespace Rhino.Runtime
{
  /// <summary>
  /// Provides tools for sending Google Analytics events using the Measurement Protocol. See https://developers.google.com/analytics/devguides/collection/protocol/v1/devguide#event for details.
  /// </summary>
  public class Analytics
  {

    #region Public Members
    /// <summary>
    /// Google Analytics Tracking ID
    /// </summary>
    public string GoogleAnalyticsTrackingID { get; set; }
    /// <summary>
    /// Name of Application. For example, "Ocelot". Do not include version numbers.
    /// Maps to Google Analytics parameter 'an'
    /// </summary>
    public string AppName { get; set; }
    /// <summary>
    /// Platform application is running on. For example "Mac", "Windows". Again, don't include version numbers.
    /// Maps to Google Analytics parameter 'ai'
    /// </summary>
    public string AppPlatform { get; set; }
    /// <summary>
    /// App Installer Id. In Rhino, we use this to differentiate between different builds such as "WIP" and "Commercial".
    /// Maps to Google Analytics parameter 'aiid'
    /// </summary>
    public string AppInstallerId { get; set; }
    /// <summary>
    /// Application version string.
    /// Maps to Google Analytics parameter 'av'
    /// </summary>
    public string AppVersion { get; set; }

    #endregion

    /// <summary>
    /// Construct Analytics class
    /// </summary>
    /// <param name="TrackingID">Google Analytics Tracking ID</param>
    /// <param name="Name">Name of Application. For example, "Ocelot". Do not include version numbers. Maps to Google Analytics parameter 'an'</param>
    public Analytics(string TrackingID, string Name)
    {
      GoogleAnalyticsTrackingID = TrackingID;
      AppName = Name;
    }

    /// <summary>
    /// Construct Analytics class
    /// </summary>
    /// <param name="TrackingID">Google Analytics Tracking ID</param>
    /// <param name="Name">Name of Application. For example, "Ocelot". Do not include version numbers. Maps to Google Analytics parameter 'an'</param>
    /// <param name="Platform">Platform application is running on. For example "Mac", "Windows". Again, don't include version numbers. Maps to Google Analytics parameter 'ai'</param>
    /// <param name="InstallerId">App Installer Id. In Rhino, we use this to differentiate between different builds such as "WIP" and "Commercial". Maps to Google Analytics parameter 'aiid'</param>
    /// <param name="Version"> Application version string. Maps to Google Analytics parameter 'av'</param>
    public Analytics(string TrackingID, string Name, string Platform, string InstallerId, string Version)
    {
      GoogleAnalyticsTrackingID = TrackingID;
      AppName = Name;
      AppPlatform = Platform;
      AppInstallerId = InstallerId;
      AppVersion = Version;
    }

    #region Static Members
    static Guid _user_id = Guid.Empty;
#if DEBUG
    static bool? m_usage_stats_enabled = false; //don't upload stats in a debug build
#else
    static bool? m_usage_stats_enabled = null;
#endif
        #endregion

    /// <summary>
    /// Sends a Google Analytics event using the Measurement Protocol. See https://developers.google.com/analytics/devguides/collection/protocol/v1/devguide#event for details.
    /// This method is asyncrhonous and the return code is completely ignored. Validate the data you send 
    /// </summary>
    /// <param name="Category">Event category. We use the feature or subsystem, such as "installer" or "app" or "document" or "loft". Maps to the Google Analytics parameter "ec".</param>
    /// <param name="Action">Event action. A verb: "open" or "start" or "option" Maps to the Google Analytics parameter "ea".</param>
    /// <param name="Label">Event label. Maps to the Google Analytics parameter "el".</param>
    /// <param name="Value">Event value. Maps to the Google Analytics parameter "ev".</param>
    [CLSCompliant(false)]
    public void Send(string Category, string Action, string Label, uint Value)
    {
      if (!UsageStatisticsEnabled)
        return;
      /*
      possible other fields to send:
      ua=string  // User Agent
      ds=string  // Data Source (string, no max)
      sr=800x600 // Screen Resolution (string, 20 bytes max)
      vp=300x400 // Viewport resolution (string 20 bytes max)
      ul=en-US   // User Language (string, 20 bytes)
      dl=string  // Document Location (URL string, 2048 bytes max)
      an=string  // App Name (100 bytes)
      aid=string // App ID (150 bytes)
      av=string  // App Version (100 bytes)

      */

      var data = new NameValueCollection()
      {
        {"ec", Category},
        {"ea", Action},
        {"el", Label},
        {"ev", Value.ToString() }
      };

      Send(data);
    }

    /// <summary>
    /// Advanced method for sending Google Analytics data.
    /// It is the caller's responsibility to make sure that all parameters passed will result in a valid Google Analytics hit. Failure to do so will result in Google Analytics ignoring your hit, and the caller will get no data.
    /// The Analytics class will populate data from the Application, the GoogleAnalyticsTrackingID, the User ID, and set the hit type "t" to "event". It also sets other information about the system.
    /// </summary>
    /// <param name="data">Name-Value pairs of data to send. Any valid Google Analytics Measurement Protocol parameter is allowed. No input validation is performed.</param>
    public void Send(NameValueCollection data)
    {
      var all_data = new NameValueCollection()
      {
        {"tid", GoogleAnalyticsTrackingID},
        {"v", "1"},
        {"an", AppName },
        {"aid", AppPlatform },
        {"aiid", AppInstallerId },
        {"av", AppVersion },
        {"uid", UserId.ToString()},
        {"cid", UserId.ToString()},
        {"sr", ScreenResolution },
        {"ul", UserLanguage },
        {"t", "event"},
      };

      // Add passed-in data
      foreach (string name in data.AllKeys)
        all_data.Add(name, data[name]);

      // Delete empty values
      foreach (string name in data.AllKeys)
        if (string.IsNullOrWhiteSpace(all_data[name]))
          all_data.Remove(name);

      // in the background, post data to google analytics and elasticsearch, one at a time, and ignore all errors
      System.Threading.Tasks.Task.Run(() =>
      {
        using (WebClient client = new WebClient())
        {
          // client.Headers.Add("user-agent", UserAgent);
          // try { client.UploadValues(new Uri("https://www.google-analytics.com/collect"), all_data); } catch { }
          client.Headers.Add("user-agent", UserAgent); // the first post wipes the headers...
          try { client.UploadValues(new Uri("https://logstash.analytics.rhino3d.com"), all_data); } catch { }
        }
      });
    }

    /// <summary>
    /// Sends a Google Analytics event using the Measurement Protocol. See https://developers.google.com/analytics/devguides/collection/protocol/v1/devguide#event for details.
    /// </summary>
    /// <param name="Category">Event category. We use the feature or subsystem, such as "installer" or "app" or "document" or "loft". Maps to the Google Analytics parameter "ec".</param>
    public void Send(string Category)
    {
      Send(Category, null, null, 1);
    }

    /// <summary>
    /// Sends a Google Analytics event using the Measurement Protocol. See https://developers.google.com/analytics/devguides/collection/protocol/v1/devguide#event for details.
    /// </summary>
    /// <param name="Category">Event category. We use the feature or subsystem, such as "installer" or "app" or "document" or "loft". Maps to the Google Analytics parameter "ec".</param>
    /// <param name="Action">Event action. A verb: "open" or "start" or "option" Maps to the Google Analytics parameter "ea".</param>
    public void Send(string Category, string Action)
    {
      Send(Category, Action, null, 1);
    }

    /// <summary>
    /// Sends a Google Analytics event using the Measurement Protocol. See https://developers.google.com/analytics/devguides/collection/protocol/v1/devguide#event for details.
    /// </summary>
    /// <param name="Category">Event category. We use the feature or subsystem, such as "installer" or "app" or "document" or "loft". Maps to the Google Analytics parameter "ec".</param>
    /// <param name="Action">Event action. A verb: "open" or "start" or "option" Maps to the Google Analytics parameter "ea".</param>
    /// <param name="Label">Event label. Maps to the Google Analytics parameter "el".</param>
    public void Send(string Category, string Action, string Label)
    {
      Send(Category, Action, Label, 1);
    }

    static string _os_version = null;
    static string OperatingSystemVersion
    {
      get
      {
        if (_os_version != null)
          return _os_version;

        if (HostUtils.RunningOnWindows)
        {
          string major, minor;
          // The 'CurrentMajorVersionNumber' string value in the CurrentVersion key is new for Windows 10, 
          // and will most likely (hopefully) be there for some time before MS decides to change this - again...
          if (TryGetRegistryKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion", "CurrentMajorVersionNumber", out major))
          {
            if (TryGetRegistryKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion", "CurrentMinorVersionNumber", out minor))
            {
              _os_version = String.Format("{0}.{1}", major, minor);
              return _os_version;
            }
          }

          // When the 'CurrentMajorVersionNumber' value is not present we fallback to reading the previous key used for this: 'CurrentVersion'
          string version;
          if (TryGetRegistryKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion", "CurrentVersion", out version))
          {
            _os_version = version;
            return _os_version;
          }
        } else if (HostUtils.RunningOnOSX) {          
          _os_version = (string)GetOperatingSystemVersionMethod.Invoke(null, null);
          return _os_version;
        }

        return "";
      }
    }

    static string UserAgent
    {
      get
      {
        // for example, "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_13_6) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/68.0.3440.106 Safari/537.36"
        if (HostUtils.RunningOnWindows)
          return string.Format("RhinoAnalytics/{1} (Windows NT {0}; Win64; x64) Mozilla/5.0", OperatingSystemVersion, RhinoApp.Version.ToString());
        if (HostUtils.RunningOnOSX)
          return string.Format("RhinoAnalytics/{1} (Macintosh; Intel Mac OS X {0}) Mozilla/5.0", OperatingSystemVersion.Replace(".", "_"), RhinoApp.Version.ToString());
        return null;
      }
    }

    private static bool TryGetRegistryKey(string path, string key, out string value)
    {
      value = null;
      try
      {
        var rk = Registry.LocalMachine.OpenSubKey(path);
        if (rk == null) return false;
        object val = rk.GetValue(key);
        if (val != null)
          value = val.ToString();
        return value != null;
      }
      catch
      {
        return false;
      }
    }

    private static System.Reflection.MethodInfo m_getOperatingSystemVersionMethod;
    private static System.Reflection.MethodInfo GetOperatingSystemVersionMethod
    {
      get
      {
        if (m_getOperatingSystemVersionMethod != null)
        {
          return m_getOperatingSystemVersionMethod;
        }
        else
        {
          m_getOperatingSystemVersionMethod = GetStaticMethod("RhinoMac", "RhinoMac.Runtime.MacPlatformService", "GetOperatingSystemVersion");
          return m_getOperatingSystemVersionMethod;
        }
      }
    }

    private static System.Reflection.MethodInfo GetStaticMethod(string assemblyName, string className, string methodName)
    {
      var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
      foreach (var assembly in assemblies)
      {
        if (assembly.FullName.Contains(assemblyName))
        {
          Type t = assembly.GetType(className);
          if (t != null)
          {
            var mi = t.GetMethod(methodName, System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
            if (mi != null)
              return mi;
          }
        }
      }
      return null;
    }

    static string _user_language;
    static string UserLanguage
    {
      get
      {
        if (!string.IsNullOrWhiteSpace(_user_language))
          return _user_language;

        try {
          int lcid = Rhino.ApplicationSettings.AppearanceSettings.LanguageIdentifier;
          var culture = new System.Globalization.CultureInfo(lcid);
          _user_language = culture.Name.ToLower();
          return _user_language;
        }
        catch {
          return null;
        }
      }
    }

    static string _screen_resolution = null;
    static string ScreenResolution
    {
      get
      {
        if (_screen_resolution == null)
          _screen_resolution = String.Format("{0}x{1}", Screen.PrimaryScreen.Bounds.Width.ToString(), Screen.PrimaryScreen.Bounds.Height.ToString());
        return _screen_resolution;
      }
    }


    /// <summary>
    /// Determine if user allows automatic data collection from Rhino
    /// </summary>
    public static bool UsageStatisticsEnabled
    {
      get
      {
        if (HostUtils.RunningOnOSX) {
          m_usage_stats_enabled = PersistentSettings.FromPlugInId(RhinoApp.CurrentRhinoId).AddChild("Options").AddChild("General").GetBool("UsageStatisticsEnabled", true);
          return (bool)m_usage_stats_enabled;
        }

        if (m_usage_stats_enabled == null)
        {
          const bool DefaultUpdateEnabled = true;

          using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\McNeel\McNeelUpdate", false))
          {
            while (true)
            {
              if (key == null)
              {
                m_usage_stats_enabled = DefaultUpdateEnabled; // don't break here; closing the key will throw an exception.
                break;
              }

              string value = (string)key.GetValue("Enabled");
              if (string.IsNullOrEmpty(value))
              {
                m_usage_stats_enabled = DefaultUpdateEnabled;
                break;
              }

              char c = value.ToLowerInvariant()[0];
              if ('y' == c || 't' == c || '1' == c)
                m_usage_stats_enabled = true;
              else
                m_usage_stats_enabled = false;

              break;
            }
          }
        }

        return (bool)m_usage_stats_enabled;
      }
    }

    /// <summary>
    /// Returns a GUID that allows events to be aggregated by user. There is no way to determine who the
    /// end user is based on this GUID, unless the user tells you their ID.  On Windows, this uses the 
    /// registry to store the ID.  On Mac, the Hardware UUID is used as the ID.
    /// </summary>
    public static Guid UserId
    {
      get
      {
        if (_user_id == Guid.Empty)
        {
          if (HostUtils.RunningOnWindows)
          {
            // Try to read and set the value.
            using (var key = Registry.CurrentUser.CreateSubKey(@"Software\McNeel"))
            {
              if (key != null)
              {
                string id = key.GetValue("uid") as string;
                if (!string.IsNullOrWhiteSpace(id))
                  Guid.TryParse(id, out _user_id);


                // Failed to read value; create it
                if (_user_id == Guid.Empty)
                {
                  _user_id = Guid.NewGuid();
                  key.SetValue("uid", _user_id.ToString());
                }
              }
            }
          }

          if (HostUtils.RunningOnOSX)
          {
            var proc = new Process
            {
              StartInfo = new ProcessStartInfo
              {
                FileName = "/usr/sbin/ioreg",
                Arguments = "-rd1 -c IOPlatformExpertDevice",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
              }
            };

            proc.Start();
            proc.WaitForExit();

            string procOutput = proc.StandardOutput.ReadToEnd();
            string keyEntry = "IOPlatformUUID";
            if (procOutput.Contains(keyEntry))
            {
              string remainingString = procOutput.Substring(procOutput.IndexOf(keyEntry, StringComparison.InvariantCulture) + (keyEntry.Length + 5));
              if (remainingString.Contains(System.Environment.NewLine))
              {
                try
                {
                  string[] splitStrings = remainingString.Split(new string[] { System.Environment.NewLine }, StringSplitOptions.None);
                  string hardware_id = splitStrings[0].Remove(splitStrings[0].Length - 1);
                  _user_id = new Guid(hardware_id);
                }
                catch
                {
                  return Guid.Empty;
                }
              }
            }
          }
        } 

        return _user_id;
      }
    }

  }
}
