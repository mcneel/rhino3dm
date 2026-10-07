using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Rhino
{
  /// <summary>
  /// This class probably does not have to be publicly exposed
  /// </summary>
  class ProgressReporter
  {
    static int g_next_serial_number = 0;
    readonly IProgress<double> m_progress;

    // [RH-89148] The registry is shared by every thread. A reporter is created on the calling
    // thread, but a computation calls OnProgressReportCallback from whatever worker thread it
    // reports from, so a per-thread registry loses every report a worker makes.
    static readonly ConcurrentDictionary<int, ProgressReporter> g_all_reporters =
      new ConcurrentDictionary<int, ProgressReporter>();

    public ProgressReporter(IProgress<double> progress)
    {
      m_progress = progress;
      SerialNumber = Interlocked.Increment(ref g_next_serial_number);

      g_all_reporters[SerialNumber] = this;
    }

    public int SerialNumber { get; private set; }


    internal delegate void ProgressReportCallback(int serialNumber, double fractionComplete);

    // Native code keeps the address of this delegate's thunk. Creating a new delegate on every
    // Enable() call would let an earlier one be collected while native still points at it.
    static readonly ProgressReportCallback g_progress_report_callback = OnProgressReportCallback;

    static void OnProgressReportCallback(int serialNumber, double fractionComplete)
    {
      try
      {
        if (g_all_reporters.TryGetValue(serialNumber, out ProgressReporter reporter))
          reporter.m_progress.Report(fractionComplete);
      }
      catch(Exception ex)
      {
        Runtime.HostUtils.ExceptionReport(ex);
      }
    }

    public void Enable()
    {
      UnsafeNativeMethods.ON_ProgressReporter_SetReportCallback(g_progress_report_callback);
    }

    /// <summary>Idempotent. Safe to call from any thread.</summary>
    public void Disable()
    {
      g_all_reporters.TryRemove(SerialNumber, out _);
    }
  }
}
