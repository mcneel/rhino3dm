#pragma warning disable 1591
using System;
using System.Runtime.InteropServices;

#if RHINO_SDK
using Rhino.PlugIns;
#endif

namespace Rhino.Runtime
{
  /// <summary>
  /// This class should only ever be called from the MonoManager.rhp. Luckily
  /// when embedding mono, you can call private classes with no problem so we
  /// don't need to expose this to the SDK.
  /// </summary>
  class MonoHost
  {
#if RHINO_SDK
    static string g_alternate_bin_directory = String.Empty;
    public static void SetAlternateBinDirectory(string path)
    {
      g_alternate_bin_directory = path;
    }
    public static string AlternateBinDirectory => g_alternate_bin_directory;

#endif

    static void InitializeExceptionHandling()
    {
      // Calling SetUnhandledExceptionMode can throw an exception if any windows have already been
      // created. I don't know how this could happen unless someone else starts writing a mono embedding
      // system in Rhino, but just to be careful

      /*
      // System.Windows.Forms does not work well for Mono yet. Skip the winforms exception handler for now
      try
      {
        System.Windows.Forms.Application.SetUnhandledExceptionMode(System.Windows.Forms.UnhandledExceptionMode.CatchException);
      }
      catch(Exception)
      {
      }
      System.Windows.Forms.Application.ThreadException += new System.Threading.ThreadExceptionEventHandler(Application_ThreadException);
      */

      AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
    }

    static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
      // this should ONLY ever be called if we are actually using Mono
      Exception ex = e.ExceptionObject as Exception;
      if (ex == null) return;
      string msg = ex.ToString() + "\n\nStackTrace:\n" + ex.StackTrace;
      if (sender != null)
      {
        msg += "\n\nSENDER = ";
        msg += sender.ToString();
      }
#if RHINO_SDK
      Rhino.UI.Dialogs.ShowMessage(msg, "Unhandled CurrentDomain Exception in .NET plug-in");
#else
      Console.Error.Write(msg);
#endif
    }

    static void Application_ThreadException(object sender, System.Threading.ThreadExceptionEventArgs e)
    {
      // this should ONLY ever be called if we are actually using Mono
      Exception ex = e.Exception;
      string msg = ex.ToString() + "\n\nStackTrace:\n" + ex.StackTrace;
      if (sender != null)
      {
        msg += "\n\nSENDER = ";
        msg += sender.ToString();
      }
#if RHINO_SDK
      Rhino.UI.Dialogs.ShowMessage(msg, "Unhandled Thread Exception in .NET plug-in");
#else
      Console.Error.Write(msg);
#endif
    }
  }
}

#if RHINO_SDK
partial class UnsafeNativeMethods
{
  // These functions must never be called unless RhinoCommon is being run from Mono
  // [DllImport("__Internal", CallingConvention=CallingConvention.Cdecl)]
  // internal static extern void RhMono_SetPlugInLoadString(int which, [MarshalAs(UnmanagedType.LPWStr)]string str);
}

#endif
