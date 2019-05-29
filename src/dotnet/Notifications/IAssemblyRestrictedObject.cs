using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rhino.Runtime.Notifications
{
  /// <summary>
  /// A class that implements this interface signals its clients that its instances can
  /// only be modified by certain assemblies. This is useful in cases where only
  /// certain assemblies should be able to modify an object. The actual members of an 
  /// instance that are restricted are left to the discretion of the instance's class,
  /// and should be documented.
  /// </summary>
  public interface IAssemblyRestrictedObject
  {
    /// <summary>
    /// Determines whether an assembly can modify the instance.
    /// </summary>
    /// <returns>true if the instance can be edited by the assembly, otherwise returns false.</returns>
    bool Editable();
  }
}
