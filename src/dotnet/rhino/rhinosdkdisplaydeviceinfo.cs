using System.Collections.Generic;
using Rhino.Runtime.InteropWrappers;
#if RHINO_SDK

namespace Rhino
{

  /// <summary>
  /// Represents a GPU device providing name, vendor and memory all as strings.
  ///
  /// Currently fully implemented only on Windows.
  /// </summary>
  public class GpuDeviceInfo
  {
    internal GpuDeviceInfo(string name, string vendor, ulong memory, string memoryAsString, string driverDateAsString){
      Name = name;
      Vendor = vendor;
      Memory = memory;
      MemoryAsString = memoryAsString;
      DriverDateAsString = driverDateAsString;
    }

    /// <summary>
    /// Name of the device
    /// </summary>
    public string Name { get; private set; }
    /// <summary>
    /// Vendor of the device
    /// </summary>
    public string Vendor { get; private set; }
    /// <summary>
    /// Memory of the device in bytes
    /// </summary>
#pragma warning disable CS3003 // Type is not CLS-compliant
    public ulong Memory { get; private set; }
#pragma warning restore CS3003 // Type is not CLS-compliant
    /// <summary>
    /// Memory of the device as a human-readable string
    /// </summary>
    public string MemoryAsString { get; private set; }
    /// <summary>
    /// The driver date as string formatted year-month-day
    /// </summary>
    public string DriverDateAsString { get; private set; }
  }
  /// <summary>
  /// Get information about display devices found on this machine (GPUs).
  /// </summary>
  public class DisplayDeviceInfo
  {
    /// <summary>
    /// Get a list with the names of all GPUs on this machine.
    /// </summary>
    /// <returns>List of strings</returns>
    static public List<string> GpuNames() {
      List<string> names = new List<string>();
      using (var list = new ClassArrayString())
      {
        var lst_ptr = list.NonConstPointer();
        UnsafeNativeMethods.RhCmn_DisplayDeviceInfo_GetGPUOnlyDeviceNames(lst_ptr);
        names.AddRange(list.ToArray());
      }

      return names;
    }

    /// <summary>
    /// Get a list of GpuDeviceInfo for GPUs found on this machine.
    /// </summary>
    /// <returns>List of GpuDeviceInfo</returns>
    static public List<GpuDeviceInfo> GpuDeviceInfos() {
      List<string> gpuNames = GpuNames();
      List<GpuDeviceInfo> gpuDeviceInfos = new List<GpuDeviceInfo>(gpuNames.Count);

      for(int i = 0; i < gpuNames.Count; i++) {
        ulong memory = UnsafeNativeMethods.RhCmn_DisplayDeviceInfo_GetGpuMemoryAsInt(i);
        using (var vendorName = new StringWrapper("")) {
          using (var memoryAsString = new StringWrapper("")) {
            using (var driverDateAsString = new StringWrapper("")) {
              UnsafeNativeMethods.RhCmn_DisplayDeviceInfo_GetGpuVendorAsString(i, vendorName.NonConstPointer);
              UnsafeNativeMethods.RhCmn_DisplayDeviceInfo_GetGpuMemoryAsString(i, memoryAsString.NonConstPointer);
              UnsafeNativeMethods.RhCmn_DisplayDeviceInfo_GetGpuDriverDateAsString(i, driverDateAsString.NonConstPointer);
              gpuDeviceInfos.Add(
                new GpuDeviceInfo(
                  gpuNames[i],
                  vendorName.ToString(),
                  memory,
                  memoryAsString.ToString(),
                  driverDateAsString.ToString()
                )
              );
            }
          }
        }

      }

      return gpuDeviceInfos;
    }
  }
}
#endif
