using System;
using System.Collections.Generic;

namespace Rhino
{
  /// <summary>
  /// This class probably does not have to be publicly exposed
  /// </summary>
  class ProgressReporter
  {
    static int g_next_serial_number = 1;
    IProgress<double> m_progress;
    readonly static List<ProgressReporter> g_all_reporters = new List<ProgressReporter>();
    public ProgressReporter(IProgress<double> progress)
    {
      m_progress = progress;
      SerialNumber = g_next_serial_number++;
      g_all_reporters.Add(this);
    }

    public int SerialNumber { get; private set; }


    internal delegate void ProgressReportCallback(int serialNumber, double fractionComplete);

    static ProgressReportCallback g_progress_report_callback;

    static void OnProgressReportCallback(int serialNumber, double fractionComplete)
    {
      try
      {
        for (int i = 0; i < g_all_reporters.Count; i++)
        {
          if (g_all_reporters[i].SerialNumber == serialNumber)
          {
            g_all_reporters[i].m_progress.Report(fractionComplete);
            return;
          }
        }
      }
      catch(Exception ex)
      {
        Runtime.HostUtils.ExceptionReport(ex);
      }
    }

    public void Enable()
    {
      g_progress_report_callback = OnProgressReportCallback;
      UnsafeNativeMethods.ON_ProgressReporter_SetReportCallback(g_progress_report_callback);
    }

    public void Disable()
    {
      for( int i=0; i<g_all_reporters.Count; i++)
      {
        if (g_all_reporters[i].SerialNumber == SerialNumber)
        {
          g_all_reporters.RemoveAt(i);
          break;
        }
      }
    }
  }
}