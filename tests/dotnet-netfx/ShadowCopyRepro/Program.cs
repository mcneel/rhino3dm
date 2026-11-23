using System;
using System.Diagnostics;
using System.IO;

// RH3DM-201: Rhino3dm on .NET Framework in a host that shadow-copies assemblies.
// Each mode runs in its own process: once the native library is loaded it stays loaded.
static class Program
{
  static int Main(string[] args)
  {
    if (args.Length == 1)
      return RunMode(args[0]);

    int failures = 0;
    foreach (var mode in new[] { "direct", "shadow" })
    {
      var psi = new ProcessStartInfo(Process.GetCurrentProcess().MainModule.FileName, mode)
      {
        UseShellExecute = false,
        WorkingDirectory = Path.GetTempPath(),  // not bin: the current folder is a DLL search path
      };
      using (var p = Process.Start(psi))
      {
        p.WaitForExit();
        failures += p.ExitCode == 0 ? 0 : 1;
      }
    }
    Console.WriteLine(failures == 0 ? "RESULT: PASS" : "RESULT: FAIL");
    return failures;
  }

  static int RunMode(string mode)
  {
    var bin = AppDomain.CurrentDomain.BaseDirectory;
    var setup = new AppDomainSetup { ApplicationBase = bin };
    if (mode == "shadow")
    {
      setup.ShadowCopyFiles = "true";
      setup.CachePath = Path.Combine(Path.GetTempPath(), "rh3dm201-shadow");
    }
    var domain = AppDomain.CreateDomain(mode, null, setup);
    try
    {
      var worker = (Worker)domain.CreateInstanceAndUnwrap(typeof(Worker).Assembly.FullName, typeof(Worker).FullName);
      var report = worker.Run();
      Console.WriteLine($"[{mode}] {report}");
      return report.StartsWith("ok") ? 0 : 1;
    }
    finally
    {
      AppDomain.Unload(domain);
    }
  }
}

public class Worker : MarshalByRefObject
{
  public string Run()
  {
    var asm = typeof(Rhino.Geometry.Circle).Assembly;
    var where = $"Location={asm.Location} CodeBase={asm.CodeBase}";
    try
    {
      // calls into librhino3dm_native
      var start = new Rhino.Geometry.Circle(1.0).ToNurbsCurve().PointAtStart;
      return $"ok start={start} {where}";
    }
    catch (Exception e)
    {
      var inner = e;
      while (inner.InnerException != null)
        inner = inner.InnerException;
      return $"FAIL {inner.GetType().Name}: {inner.Message} {where}";
    }
  }
}
