using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Rhino.Runtime.InteropWrappers;
using System.Net.NetworkInformation;
using Rhino.DocObjects;
using System.Diagnostics;
using System.IO;
#if NET
using System.Runtime.Loader;
#endif

#if RHINO_SDK
using Rhino.PlugIns;
using System.Management;
using System.Reflection.Metadata;
using System.Reflection;
using Rhino.FileIO;
using System.Text.RegularExpressions;
#endif


/// <summary>
/// Attribute used by the iOS AOT (Ahead of Time) compiler to wire up callbacks correctly.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
class MonoPInvokeCallbackAttribute : Attribute {
    public MonoPInvokeCallbackAttribute (Type t) {}
}

namespace Rhino.Runtime
{
  /// <summary>
  /// Exception thrown when calling functions in RhinoCommon and the
  /// application is executing without a license
  /// </summary>
  public class NotLicensedException : Exception
  {
    /// <summary> Default constructor </summary>
    /// <since>7.0</since>
    public NotLicensedException() { }
    /// <summary>
    /// Create a new instance with a custom message
    /// </summary>
    /// <param name="message"></param>
    /// <since>7.0</since>
    public NotLicensedException(string message) : base(message) { }
    /// <summary>
    /// Create a new instance with a custom message and an inner exception
    /// </summary>
    /// <param name="message"></param>
    /// <param name="inner"></param>
    /// <since>7.0</since>
    public NotLicensedException(string message, Exception inner) : base(message, inner) { }

#if RHINO_SDK
    internal static void ThrowNotLicensedException(object sender, NamedParametersEventArgs args)
    {
      throw new NotLicensedException();
    }
#endif
  }

#if RHINO_SDK
  /// <summary>
  /// Passed to LicenseStateChanged event on RhinoApp
  /// </summary>
  public class LicenseStateChangedEventArgs : EventArgs
  {
    /// <summary>
    /// true if RhinoCommon calls will never raise Rhino.Runtime.NotLicensedException.
    /// false otherwise
    /// </summary>
    /// <since>7.0</since>
    public bool CallingRhinoCommonAllowed { get; private set; } = false;

    /// <summary>
    /// LicenseStateChangedEventArgs constructor
    /// </summary>
    /// <param name="callingRhinoCommonAllowed">True when calling RhinoCommon will never raise Rhino.Runtime.NotLicesnedException; false otherwise.</param>
    /// <since>7.0</since>
    public LicenseStateChangedEventArgs(bool callingRhinoCommonAllowed)
    {
      CallingRhinoCommonAllowed = callingRhinoCommonAllowed;
    }
  }
#endif

  /// <summary>
  /// Marks a method as const. This attribute is purely informative to help the
  /// developer know that a method on a class does not alter the class itself.
  /// </summary>
  [AttributeUsage(AttributeTargets.Method)]
  class ConstOperationAttribute : Attribute
  {
    /// <summary>Basic constructor to mark a method as const</summary>
    public ConstOperationAttribute()
    {
    }
  }

#if RHINO_SDK
  /// <summary>
  /// Dictionary style class used for named callbacks from C++ -> .NET
  /// </summary>
  public class NamedParametersEventArgs : EventArgs, IDisposable
  {
    bool m_deleteOnDispose = false;
    internal IntPtr m_pNamedParams;
    internal NamedParametersEventArgs(IntPtr ptr)
    {
      m_pNamedParams = ptr;
      GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Construct a new named parameter even args. You should dispose this class when you are done with it
    /// </summary>
    /// <since>7.0</since>
    public NamedParametersEventArgs()
    {
      m_pNamedParams = UnsafeNativeMethods.CRhParameterDictionary_New();
      m_deleteOnDispose = true;
    }

    /// <summary>
    /// Finalizer in case Dispose wasn't called
    /// </summary>
    ~NamedParametersEventArgs()
    {
      Dispose(true);
    }

    /// <summary>
    /// Dispose native resources
    /// </summary>
    /// <since>7.0</since>
    public void Dispose()
    {
      Dispose(false);
    }

    void Dispose(bool fromFinalizer)
    {
      if (!fromFinalizer)
        GC.SuppressFinalize(this);

      IntPtr ptr = m_pNamedParams;
      m_pNamedParams = IntPtr.Zero;
      if( m_deleteOnDispose && ptr != IntPtr.Zero )
      {
        UnsafeNativeMethods.CRhParameterDictionary_Delete(ptr);
      }
    }

    /// <summary>
    /// Try to get a string value for a given key name
    /// </summary>
    /// <param name="name"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    /// <since>6.15</since>
    public bool TryGetString(string name, out string value)
    {
      using (var str = new StringWrapper())
      {
        IntPtr pString = str.NonConstPointer;
        bool rc = UnsafeNativeMethods.CRhParameterDictionary_GetString(m_pNamedParams, name, pString);
        value = str.ToString();
        return rc;
      }
    }

    /// <summary> Set a string value for a given key name </summary>
    /// <param name="name"></param>
    /// <param name="value"></param>
    /// <since>7.0</since>
    public void Set(string name, string value)
    {
      UnsafeNativeMethods.CRhParameterDictionary_SetString(m_pNamedParams, name, value);
    }

    /// <summary>
    /// Try to get a string array for a given key name
    /// </summary>
    /// <param name="name"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    /// <since>7.0</since>
    public bool TryGetStrings(string name, out string[] value)
    {
      using (var list = new ClassArrayString())
      {
        var pointer = list.NonConstPointer();
        bool rc = UnsafeNativeMethods.CRhParameterDictionary_GetStringList(m_pNamedParams, name, pointer);
        value = list.ToArray();
        return rc;
      }
    }

    /// <summary> Set a list of strings as a value for a given key name </summary>
    /// <param name="name"></param>
    /// <param name="strings"></param>
    /// <since>7.0</since>
    public void Set(string name, IEnumerable<string> strings)
    {
      using (var list = new ClassArrayString())
      {
        foreach (var s in strings)
          list.Add(s);
        var pointer = list.NonConstPointer();
        UnsafeNativeMethods.CRhParameterDictionary_SetStringList(m_pNamedParams, name, pointer);
      }
    }

    /// <summary>
    /// Try to get an array of ObjRef instances for a given key name
    /// </summary>
    /// <param name="name"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    /// <since>8.0</since>
    public bool TryGetObjRefs(string name, out ObjRef[] value)
    {
      using (var obj_refs = new ClassArrayObjRef())
      {
        var pointer = obj_refs.ConstPointer();
        var success = UnsafeNativeMethods.CRhParameterDictionary_GetObjRefList(m_pNamedParams, name, pointer);
        value = obj_refs.ToNonConstArray() ?? new ObjRef[0];
        return success;
      }
    }

    /// <summary> Set a list of ObjRef instances as a value for a given key name </summary>
    /// <param name="name"></param>
    /// <param name="values"></param>
    /// <since>8.0</since>
    public void Set(string name, IEnumerable<ObjRef> values)
    {
      using (var obj_refs = new ClassArrayObjRef())
      {
        if (values != null)
          foreach (var item in values)
            obj_refs.Add(item);
        var pointer = obj_refs.ConstPointer();
        UnsafeNativeMethods.CRhParameterDictionary_SetObjRefList(m_pNamedParams, name, pointer);
      }
    }


    /// <summary>
    /// Try to get a uint array value for a given key name
    /// </summary>
    /// <param name="name"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    /// <since>8.0</since>
    [CLSCompliant(false)]
    public bool TryGetUints(string name, out uint[] value)
    {
      var pointer = UnsafeNativeMethods.ON_UIntArray_New();
      var count = 0;
      var success = UnsafeNativeMethods.CRhParameterDictionary_GetUintList(m_pNamedParams, name, pointer, ref count);
      value = new uint[Math.Max(0, count)];
      if (count > 0)
        UnsafeNativeMethods.ON_UIntArray_CopyValues(pointer, value);
      UnsafeNativeMethods.ON_UIntArray_Delete(pointer);
      return success;
    }

    /// <summary> Set a list of uint as a value for a given key name </summary>
    /// <param name="name"></param>
    /// <param name="values"></param>
    /// <since>8.0</since>
    [CLSCompliant(false)]
    public void Set(string name, IEnumerable<uint> values)
    {
      var array = values?.ToArray() ?? new uint[0];
      UnsafeNativeMethods.CRhParameterDictionary_SetUIntList(m_pNamedParams, name, array, array.Length);
    }

    /// <summary>
    /// Try to get a UUID array value for a given key name
    /// </summary>
    /// <param name="name"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    /// <since>7.0</since>
    public bool TryGetGuids(string name, out Guid[] value)
    {
      using (var list = new SimpleArrayGuid())
      {
        var pointer = list.NonConstPointer();
        bool rc = UnsafeNativeMethods.CRhParameterDictionary_GetGuidList(m_pNamedParams, name, pointer);
        value = list.ToArray();
        return rc;
      }
    }

    /// <summary> Set a list of UUIDs as a value for a given key name </summary>
    /// <param name="name"></param>
    /// <param name="guidList"></param>
    /// <since>7.0</since>
    public void Set(string name, IEnumerable<Guid> guidList)
    {
      using (var list = new SimpleArrayGuid(guidList))
      {
        var pointer = list.NonConstPointer();
        UnsafeNativeMethods.CRhParameterDictionary_SetGuidList(m_pNamedParams, name, pointer);
      }
    }

    /// <summary>
    /// Try to get a bool value for a given key name
    /// </summary>
    /// <param name="name"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    /// <since>6.15</since>
    public bool TryGetBool(string name, out bool value)
    {
      value = false;
      return UnsafeNativeMethods.CRhParameterDictionary_GetBool(m_pNamedParams, name, ref value);
    }

    /// <summary>
    /// Set a bool value for a given key name
    /// </summary>
    /// <param name="name"></param>
    /// <param name="value"></param>
    /// <since>7.0</since>
    public void Set(string name, bool value)
    {
      UnsafeNativeMethods.CRhParameterDictionary_SetBool(m_pNamedParams, name, value);
    }

    /// <summary>
    /// Try to get an int value for a given key name
    /// </summary>
    /// <param name="name"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    /// <since>6.15</since>
    public bool TryGetInt(string name, out int value)
    {
      value = 0;
      return UnsafeNativeMethods.CRhParameterDictionary_GetInt(m_pNamedParams, name, ref value);
    }

    /// <summary>
    /// Set an int value for a given key name
    /// </summary>
    /// <param name="name"></param>
    /// <param name="value"></param>
    /// <since>7.0</since>
    public void Set(string name, int value)
    {
      UnsafeNativeMethods.CRhParameterDictionary_SetInt(m_pNamedParams, name, value);
    }

    /// <summary>
    /// Try to get an unsigned int for a given key name
    /// </summary>
    /// <param name="name"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    /// <since>7.0</since>
    [CLSCompliant(false)]
    public bool TryGetUnsignedInt(string name, out uint value)
    {
      value = 0;
      return UnsafeNativeMethods.CRhParameterDictionary_GetUnsignedInt(m_pNamedParams, name, ref value);
    }

    /// <summary>
    /// Set an unsigned int for a given key name
    /// </summary>
    /// <param name="name"></param>
    /// <param name="value"></param>
    /// <since>7.0</since>
    [CLSCompliant(false)]
    public void Set(string name, uint value)
    {
      UnsafeNativeMethods.CRhParameterDictionary_SetUnsignedInt(m_pNamedParams, name, value);
    }

    /// <summary>
    /// Try to get a double value for a given key name
    /// </summary>
    /// <param name="name"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    /// <since>6.15</since>
    public bool TryGetDouble(string name, out double value)
    {
      value = 0;
      return UnsafeNativeMethods.CRhParameterDictionary_GetDouble(m_pNamedParams, name, ref value);
    }

    /// <summary>
    /// Set a double value for a given key name
    /// </summary>
    /// <param name="name"></param>
    /// <param name="value"></param>
    /// <since>7.0</since>
    public void Set(string name, double value)
    {
      UnsafeNativeMethods.CRhParameterDictionary_SetDouble(m_pNamedParams, name, value);
    }

    /// <summary>
    /// Try to get a Point value for a given key name
    /// </summary>
    /// <param name="name"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    /// <since>8.0</since>
    public bool TryGetPoint2i(string name, out Point value)
    {
      value = Point.Empty;
      var x = value.X;
      var y = value.Y;
      if (UnsafeNativeMethods.CRhParameterDictionary_GetPoint2i(m_pNamedParams, name, ref x, ref y))
      {
        value.X = x;
        value.Y = y;
        return true;
      }
      return false;
    }

    /// <summary>
    /// Set a Point value for a given key name
    /// </summary>
    /// <param name="name"></param>
    /// <param name="value"></param>
    /// <since>8.0</since>
    public void Set(string name, Point value)
    {
      UnsafeNativeMethods.CRhParameterDictionary_SetPoint2i(m_pNamedParams, name, value.X, value.Y);
    }

    /// <summary>
    /// Try to get a Point3d value for a given key name
    /// </summary>
    /// <param name="name"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    /// <since>7.0</since>
    public bool TryGetPoint(string name, out Geometry.Point3d value)
    {
      value = Rhino.Geometry.Point3d.Unset;
      return UnsafeNativeMethods.CRhParameterDictionary_GetPoint3d(m_pNamedParams, name, ref value);
    }

    /// <summary>
    /// Set a Point3d value for a given key name
    /// </summary>
    /// <param name="name"></param>
    /// <param name="value"></param>
    /// <since>7.0</since>
    public void Set(string name, Geometry.Point3d value)
    {
      UnsafeNativeMethods.CRhParameterDictionary_SetPoint3d(m_pNamedParams, name, value);
    }

    /// <summary>
    /// Try to get a Vector3d value for a given key name
    /// </summary>
    /// <param name="name"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    /// <since>7.0</since>
    public bool TryGetVector(string name, out Geometry.Vector3d value)
    {
      value = Rhino.Geometry.Vector3d.Unset;
      return UnsafeNativeMethods.CRhParameterDictionary_GetVector3d(m_pNamedParams, name, ref value);
    }

    /// <summary>
    /// Set a Vector3d value for a given key name
    /// </summary>
    /// <param name="name"></param>
    /// <param name="value"></param>
    /// <since>7.0</since>
    public void Set(string name, Geometry.Vector3d value)
    {
      UnsafeNativeMethods.CRhParameterDictionary_SetVector3d(m_pNamedParams, name, value);
    }

    /// <summary>
    /// Try to get a Color value for a given key name
    /// </summary>
    /// <param name="name"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    /// <since>7.0</since>
    public bool TryGetColor(string name, out Color value)
    {
      value = Color.Empty;
      var v = value.ToArgb();
      if (!UnsafeNativeMethods.CRhParameterDictionary_GetColor(m_pNamedParams, name, ref v))
        return false;
      value = Color.FromArgb(v);
      return true;
    }

    /// <summary>
    /// Set a Color value for a given key name
    /// </summary>
    /// <param name="name"></param>
    /// <param name="value"></param>
    /// <since>7.0</since>
    public void Set(string name, Color value)
    {
      UnsafeNativeMethods.CRhParameterDictionary_SetColor(m_pNamedParams, name, value.ToArgb());
    }

    /// <summary>
    /// Try to get a Color value for a given key name
    /// </summary>
    /// <param name="name"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    /// <since>7.0</since>
    public bool TryGetGuid(string name, out Guid value)
    {
      value = Guid.Empty;
      return UnsafeNativeMethods.CRhParameterDictionary_GetUuid(m_pNamedParams, name, ref value);
    }

    /// <summary>
    /// Set a Color value for a given key name
    /// </summary>
    /// <param name="name"></param>
    /// <param name="value"></param>
    /// <since>7.0</since>
    public void Set(string name, Guid value)
    {
      UnsafeNativeMethods.CRhParameterDictionary_SetUuid(m_pNamedParams, name, value);
    }

    /// <summary>
    /// Try to get a viewport for a given key name
    /// </summary>
    /// <param name="name"></param>
    /// <param name="viewport"></param>
    /// <returns></returns>
    /// <since>7.0</since>
    public bool TryGetViewport(string name, out ViewportInfo viewport)
    {
      viewport = null;
      IntPtr ptr_viewport = UnsafeNativeMethods.CRhParameterDictionary_GetViewport(m_pNamedParams, name);
      if( ptr_viewport != IntPtr.Zero)
      {
        viewport = new ViewportInfo(ptr_viewport, true);
        return true;
      }
      return false;
    }

    /// <summary>
    /// Set geometry for a given key name
    /// </summary>
    /// <param name="name"></param>
    /// <param name="value"></param>
    /// <since>7.0</since>
    public void Set(string name, Geometry.GeometryBase value)
    {
      Set(name, new Geometry.GeometryBase[] { value });
    }

    /// <summary>
    /// Set a list of geometry for a given key name
    /// </summary>
    /// <param name="name"></param>
    /// <param name="values"></param>
    /// <since>7.0</since>
    public void Set(string name, IEnumerable<Geometry.GeometryBase> values)
    {
      IntPtr pObjectArray = UnsafeNativeMethods.ON_ObjectArray_New();
      foreach(var value in values)
      {
        IntPtr ptrObject = value.ConstPointer();
        UnsafeNativeMethods.ON_ObjectArray_Append(pObjectArray, ptrObject);
      }
      UnsafeNativeMethods.CRhParameterDictionary_SetObjects(m_pNamedParams, name, pObjectArray);
      UnsafeNativeMethods.ON_ObjectArray_Delete(pObjectArray);
    }


    /// <summary>
    /// Set a HWND on Windows or NSView* on Mac
    /// </summary>
    /// <param name="name"></param>
    /// <param name="value"></param>
    /// <since>7.0</since>
    public void SetWindowHandle(string name, IntPtr value)
    {
      UnsafeNativeMethods.CRhParameterDictionary_SetWindowHandle(m_pNamedParams, name, value);
    }

    /// <summary>
    /// Gets a HWND on Windows or NSVIew* on Mac
    /// </summary>
    /// <param name="name"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    /// <since>7.0</since>
    public bool TryGetWindowImageHandle(string name, out IntPtr value)
    {
      value = UnsafeNativeMethods.CRhParameterDictionary_GetImageHandle(m_pNamedParams, name);
      return value != IntPtr.Zero;
    }

    /// <summary>
    /// Set a HWND on Windows or NSView* on Mac
    /// </summary>
    /// <param name="name"></param>
    /// <param name="value"></param>
    /// <since>7.0</since>
    public void SetWindowImageHandle(string name, IntPtr value)
    {
      UnsafeNativeMethods.CRhParameterDictionary_SetImageHandle(m_pNamedParams, name, value);
    }

    /// <summary>
    /// Gets a HWND on Windows or NSVIew* on Mac
    /// </summary>
    /// <param name="name"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    /// <since>7.0</since>
    public bool TryGetWindowHandle(string name, out IntPtr value)
    {
      value = UnsafeNativeMethods.CRhParameterDictionary_GetWindowHandle(m_pNamedParams, name);
      return value != IntPtr.Zero;
    }

    /// <summary>
    /// Gets a HWND on Windows or NSVIew* on Mac
    /// </summary>
    /// <param name="name"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    /// <since>7.0</since>
    public bool TryGetUnmangedPointer(string name, out IntPtr value)
    {
      value = UnsafeNativeMethods.CRhParameterDictionary_GetUnmangedPointer(m_pNamedParams, name);
      return value != IntPtr.Zero;
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="name"></param>
    /// <param name="values"></param>
    /// <returns></returns>
    /// <since>7.0</since>
    public bool TryGetGeometry(string name, out Geometry.GeometryBase[] values)
    {
      if( !_retrievedObjects.ContainsKey(name) )
      {
        IntPtr pObjectArray = UnsafeNativeMethods.ON_ObjectArray_New();
        bool rc = UnsafeNativeMethods.CRhParameterDictionary_GetObjects(m_pNamedParams, name, pObjectArray);
        if( rc )
        {
          int count = UnsafeNativeMethods.ON_ObjectArray_Count(pObjectArray);
          var geometry = new Geometry.GeometryBase[count];
          for( int i=0; i<count; i++ )
          {
            IntPtr ptrObject = UnsafeNativeMethods.ON_ObjectArray_Item(pObjectArray, i);
            geometry[i] = Geometry.GeometryBase.CreateGeometryHelper(ptrObject, null);
          }
          _retrievedObjects[name] = geometry;
        }
        UnsafeNativeMethods.ON_ObjectArray_Delete(pObjectArray);
      }
      return _retrievedObjects.TryGetValue(name, out values);
    }

    Dictionary<string, Geometry.GeometryBase[]> _retrievedObjects = new Dictionary<string, Geometry.GeometryBase[]>();

    /// <summary>
    /// Get array of RhinoObject for the specified key
    /// </summary>
    /// <param name="key"></param>
    /// <param name="values"></param>
    /// <returns></returns>
    /// <since>7.0</since>
    public bool TryGetRhinoObjects(string key, out DocObjects.RhinoObject[] values)
    {
      if (!_retrievedRhinoObjects.ContainsKey(key))
      {
        using (var rhobjs = new InternalRhinoObjectArray())
        {
          IntPtr ptr_object_array = rhobjs.NonConstPointer();
          if (UnsafeNativeMethods.CRhParameterDictionary_GetRhinoObjects(m_pNamedParams, key, ptr_object_array))
          {
            var a = rhobjs.ToArray();
            if (a != null)
              _retrievedRhinoObjects[key] = a;
          }
        }
      }
      return _retrievedRhinoObjects.TryGetValue(key, out values);
    }
    Dictionary<string, DocObjects.RhinoObject[]> _retrievedRhinoObjects = new Dictionary<string, DocObjects.RhinoObject[]>();

    /// <summary>
    /// Keep internal for now. This is used by the hatch command.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    internal bool TryGetHatchObjects(string name, out List<DocObjects.HatchObject> value)
    {
      var rc = false;
      value = new List<DocObjects.HatchObject>();
      using (var rhobjs = new Runtime.InternalRhinoObjectArray())
      {
        IntPtr ptr_object_array = rhobjs.NonConstPointer();
        rc = UnsafeNativeMethods.CRhParameterDictionary_GetHatchObjects(m_pNamedParams, name, ptr_object_array);
        if (rc)
        {
          foreach (var rhobj in rhobjs.ToArray())
          {
            if (rhobj is DocObjects.HatchObject hatchobj)
              value.Add(hatchobj);
          }
          rc = value.Count > 0;
        }
      }
      return rc;
    }

    /// <summary>
    /// Keep internal for now. This is used by the hatch command.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="value"></param>
    internal void SetHatchObjects(string name, IEnumerable<DocObjects.HatchObject> value)
    {
      using (var rhobjs = new Runtime.InternalRhinoObjectArray(value))
      {
        IntPtr ptr_object_array = rhobjs.NonConstPointer();
        UnsafeNativeMethods.CRhParameterDictionary_SetHatchObjects(m_pNamedParams, name, ptr_object_array);
      }
    }

    /// <summary>
    /// Get a line for the specified key
    /// </summary>
    /// <param name="name"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    /// <since>8.0</since>
    public bool TryGetLine(string name, out Rhino.Geometry.Line value)
    {
      value = Rhino.Geometry.Line.Unset;
      return UnsafeNativeMethods.CRhParameterDictionary_GetLine(m_pNamedParams, name, ref value);
    }

    /// <summary>
    /// Set a line for the specified key
    /// </summary>
    /// <param name="name"></param>
    /// <param name="value"></param>
    /// <since>8.0</since>
    public void Set(string name, Rhino.Geometry.Line value)
    {
      UnsafeNativeMethods.CRhParameterDictionary_SetLine(m_pNamedParams, name, ref value);
    }

    /// <summary>
    /// Get a arc for the specified key
    /// </summary>
    /// <param name="name"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    /// <since>8.0</since>
    public bool TryGetArc(string name, out Rhino.Geometry.Arc value)
    {
      value = Rhino.Geometry.Arc.Unset;
      return UnsafeNativeMethods.CRhParameterDictionary_GetArc(m_pNamedParams, name, ref value);
    }

    /// <summary>
    /// Set an arc for the specified key
    /// </summary>
    /// <param name="name"></param>
    /// <param name="value"></param>
    /// <since>8.0</since>
    public void Set(string name, Rhino.Geometry.Arc value)
    {
      UnsafeNativeMethods.CRhParameterDictionary_SetArc(m_pNamedParams, name, ref value);
    }

    /// <summary>
    /// Get a plane for the specified key
    /// </summary>
    /// <param name="name"></param>
    /// <param name="plane"></param>
    /// <returns></returns>
    /// <since>8.0</since>
    public bool TryGetPlane(string name, out Rhino.Geometry.Plane plane)
    {
      plane = Rhino.Geometry.Plane.Unset;
      return UnsafeNativeMethods.CRhParameterDictionary_GetPlane(m_pNamedParams, name, ref plane);
    }

    /// <summary>
    /// Set a plane for the specified key
    /// </summary>
    /// <param name="name"></param>
    /// <param name="plane"></param>
    /// <since>8.0</since>
    public void Set(string name, Rhino.Geometry.Plane plane)
    {
      UnsafeNativeMethods.CRhParameterDictionary_SetPlane(m_pNamedParams, name, ref plane);
    }

    /// <summary>
    /// Gets a point array for the specified key
    /// </summary>
    /// <param name="name"></param>
    /// <param name="pts"></param>
    /// <returns></returns>
    /// <since>8.0</since>
    public bool TryGetPoints(string name, out Rhino.Geometry.Point3d[] pts)
    {
      SimpleArrayPoint3d simplePoint3dArray = new SimpleArrayPoint3d();
      bool rc = UnsafeNativeMethods.CRhParameterDictionary_GetPointsList(m_pNamedParams, name, simplePoint3dArray.NonConstPointer());
      pts = simplePoint3dArray.ToArray();
      return rc;
    }

    /// <summary>
    /// Set a point array for the specified key
    /// </summary>
    /// <param name="name"></param>
    /// <param name="pts"></param>
    /// <since>8.0</since>
    public void Set(string name, Rhino.Geometry.Point3d[] pts)
    {
      SimpleArrayPoint3d simplePoint3dArray = new SimpleArrayPoint3d(pts);
      UnsafeNativeMethods.CRhParameterDictionary_SetPointsList(m_pNamedParams, name, simplePoint3dArray.NonConstPointer());
    }

    /// <summary>
    /// Get an MeshingParameters for the specified key
    /// </summary>
    /// <param name="name"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    /// <since>8.0</since>
    public bool TryGetMeshParameters(string name, out Geometry.MeshingParameters value)
    {
      value = new Geometry.MeshingParameters();
      var pointer = value.NonConstPointer();
      return UnsafeNativeMethods.CRhParameterDictionary_GetMeshParams(m_pNamedParams, name, pointer);
    }

    /// <summary>
    /// Set an MeshingParameters for the specified key
    /// </summary>
    /// <param name="name"></param>
    /// <param name="value"></param>
    /// <since>8.0</since>
    public void Set(string name, Geometry.MeshingParameters value)
    {
      UnsafeNativeMethods.CRhParameterDictionary_SetMeshParams(m_pNamedParams, name, value.ConstPointer());
    }
  }

  /// <summary>
  /// Represents a customized environment that changes the appearance of Rhino.
  /// <para>Skin DLLs must contain a single class that derives from the Skin class.</para>
  /// </summary>
  public abstract class Skin
  {
    internal delegate void ShowSplashCallback(int mode, [MarshalAs(UnmanagedType.LPWStr)] string description);
    private static ShowSplashCallback m_ShowSplash;
    private static Skin m_theSingleSkin;

    /// <summary>
    /// Any time Rhino is running there is at most one skin being used (and
    /// possibly no skin).  If a RhinoCommon based Skin class is being used, use
    /// ActiveSkin to get at the instance of this Skin class. May return null
    /// if no Skin is being used or if the skin is not a RhinoCommon based skin.
    /// </summary>
    /// <since>5.0</since>
    public static Skin ActiveSkin
    {
      get { return m_theSingleSkin; }
    }

    internal static string SkinDllPath
    {
      get
      {
        using (var sh = new Rhino.Runtime.InteropWrappers.StringHolder())
        {
          var pointer = sh.NonConstPointer();
          var path = string.Empty;
          if (UnsafeNativeMethods.CRhSkin_SkinDllPath(pointer))
            path = sh.ToString();
          return string.IsNullOrWhiteSpace(path) ? null : path;
        }
      }
    }

    internal static void DeletePointer()
    {
      if( m_theSingleSkin!=null )
      {
        UnsafeNativeMethods.CRhinoSkin_Delete(m_theSingleSkin.m_pSkin);
        m_theSingleSkin.m_pSkin = IntPtr.Zero;
      }
    }

    internal void OnShowSplash(int mode, string description)
    {
      const int HIDESPLASH = 0;
      const int SHOWSPLASH = 1;
      const int SHOWHELP = 2;
      const int MAINFRAMECREATED = 1000;
      const int LICENSECHECKED = 2000;
      const int BUILTIN_COMMANDS_REGISTERED = 3000;
      const int BEGIN_LOAD_PLUGIN = 4000;
      const int END_LOAD_PLUGIN = 5000;
      const int END_LOAD_AT_START_PLUGINS = 6000;
      const int BEGIN_LOAD_PLUGINS_BASE = 100000;
      try
      {
        if (m_theSingleSkin != null)
        {
          switch (mode)
          {
            case HIDESPLASH:
              m_theSingleSkin.HideSplash();
              break;
            case SHOWSPLASH:
              m_theSingleSkin.ShowSplash();
              break;
            case SHOWHELP:
              m_theSingleSkin.ShowHelp();
              break;
            case MAINFRAMECREATED:
              m_theSingleSkin.OnMainFrameWindowCreated();
              break;
            case LICENSECHECKED:
              m_theSingleSkin.OnLicenseCheckCompleted();
              break;
            case BUILTIN_COMMANDS_REGISTERED:
              m_theSingleSkin.OnBuiltInCommandsRegistered();
              break;
            case BEGIN_LOAD_PLUGIN:
              m_theSingleSkin.OnBeginLoadPlugIn(description);
              break;
            case END_LOAD_PLUGIN:
              m_theSingleSkin.OnEndLoadPlugIn();
              break;
            case END_LOAD_AT_START_PLUGINS:
              m_theSingleSkin.OnEndLoadAtStartPlugIns();
              break;
          }
          if (mode >= BEGIN_LOAD_PLUGINS_BASE)
          {
            int count = (mode - BEGIN_LOAD_PLUGINS_BASE);
            m_theSingleSkin.OnBeginLoadAtStartPlugIns(count);
          }
        }
      }
      catch (Exception ex)
      {
        Runtime.HostUtils.DebugString("Exception caught during Show/Hide Splash");
        Rhino.Runtime.HostUtils.ExceptionReport(ex);
      }
    }

    IntPtr m_pSkin;

    /// <summary>
    /// Initializes a new instance of the <see cref="Skin"/> class.
    /// </summary>
    protected Skin()
    {
      if (m_theSingleSkin != null) return;
      // set callback if it hasn't already been set
      if (null == m_ShowSplash)
      {
        m_ShowSplash = OnShowSplash;
      }

      System.Drawing.Bitmap icon = MainRhinoIcon;
      string name = ApplicationName;

      IntPtr hicon = IntPtr.Zero;
      if (icon != null)
        hicon = icon.GetHicon();

      m_pSkin = UnsafeNativeMethods.CRhinoSkin_New(m_ShowSplash, name, hicon);
      m_theSingleSkin = this;
    }
    /// <summary>Is called when the splash screen should be shown.</summary>
    protected virtual void ShowSplash() { }

    /// <summary>
    /// Called when the "help" splash screen should be shown. Default
    /// implementation just calls ShowSplash()
    /// </summary>
    protected virtual void ShowHelp() { ShowSplash(); }

    /// <summary>Is called when the splash screen should be hidden.</summary>
    protected virtual void HideSplash() { }

    /// <summary>Is called when the main frame window is created.</summary>
    protected virtual void OnMainFrameWindowCreated() { }

    /// <summary>Is called when the license check is completed.</summary>
    protected virtual void OnLicenseCheckCompleted() { }

    /// <summary>Is called when built-in commands are registered.</summary>
    protected virtual void OnBuiltInCommandsRegistered() { }

    /// <summary>Is called when the first plug-in that loads at start-up is going to be loaded.</summary>
    /// <param name="expectedCount">The complete amount of plug-ins.</param>
    protected virtual void OnBeginLoadAtStartPlugIns(int expectedCount) { }

    /// <summary>Is called when a specific plug-in is going to be loaded.</summary>
    /// <param name="description">The plug-in description.</param>
    protected virtual void OnBeginLoadPlugIn(string description) { }

    /// <summary>Is called after each plug-in has been loaded.</summary>
    protected virtual void OnEndLoadPlugIn() { }

    /// <summary>Is called after all of the load at start plug-ins have been loaded.</summary>
    protected virtual void OnEndLoadAtStartPlugIns() { }

    /// <summary>If you want to provide a custom icon for your skin.</summary>
    protected virtual System.Drawing.Bitmap MainRhinoIcon
    {
      get { return null; }
    }

    /// <summary>If you want to provide a custom name for your skin.</summary>
    protected virtual string ApplicationName
    {
      get { return string.Empty; }
    }

    PersistentSettingsManager m_SettingsManager;

    /// <summary>
    /// Gets access to the skin persistent settings.
    /// </summary>
    /// <since>5.0</since>
    public PersistentSettings Settings
    {
      get
      {
        if (m_SettingsManager == null)
          m_SettingsManager = PersistentSettingsManager.Create(this);
        return m_SettingsManager.PluginSettings;
      }
    }

    static bool m_settings_written;
    internal static void WriteSettings(bool shuttingDown)
    {
      if (!m_settings_written)
      {
        if (m_theSingleSkin != null && m_theSingleSkin.m_SettingsManager != null)
        {
          if (m_theSingleSkin.m_SettingsManager.m_plugin_id == Guid.Empty)
            m_theSingleSkin.m_SettingsManager.WriteSettings(shuttingDown);
        }
      }
      m_settings_written = true;
    }
  }

  /// <summary>
  /// Represents scripting compiled code.
  /// </summary>
  public abstract class PythonCompiledCode
  {
    /// <summary>
    /// Executes the script in a specific scope.
    /// </summary>
    /// <param name="scope">The scope where the script should be executed.</param>
    /// <since>5.0</since>
    public abstract void Execute(PythonScript scope);
  }

  /// <summary>
  /// Represents a Python script.
  /// </summary>
  public abstract class PythonScript
  {
    /// <summary>
    /// Constructs a new Python script context.
    /// </summary>
    /// <returns>A new Python script, or null if none could be created. Rhino 4 always returns null.</returns>
    /// <since>5.0</since>
    public static PythonScript Create()
    {
      Guid ip_id = new Guid("814d908a-e25c-493d-97e9-ee3861957f49");
      object obj = Rhino.RhinoApp.GetPlugInObject(ip_id);
      if (null == obj)
        return null;
      PythonScript pyscript = obj as PythonScript;
      return pyscript;
    }

    static List<System.Reflection.Assembly> _runtimeAssemblies;

    /// <summary>
    /// Get list of assemblies used by python for library browser and
    /// inclusion into the runtime
    /// </summary>
    /// <returns></returns>
    /// <since>7.0</since>
    public static System.Reflection.Assembly[] RuntimeAssemblies()
    {
      if (null == _runtimeAssemblies)
        return new System.Reflection.Assembly[0];
      return _runtimeAssemblies.ToArray();
    }

    /// <summary>
    /// Add assembly to list of assemblies used by python
    /// </summary>
    /// <param name="assembly"></param>
    /// <since>7.0</since>
    public static void AddRuntimeAssembly(System.Reflection.Assembly assembly)
    {
      if (null == _runtimeAssemblies)
        _runtimeAssemblies = new List<System.Reflection.Assembly>();
      _runtimeAssemblies.Add(assembly);
    }

    /// <summary>
    /// Get/Set additional search paths used by the python interpreter
    /// </summary>
    /// <since>7.1</since>
    public static string[] SearchPaths
    {
      get
      {
        var script = Create();
        if (script != null)
          return script.GetSearchPaths();
        return new string[0];
      }
      set
      {
        var script = Create();
        if (script != null)
          script.SetSearchPaths(value);
      }
    }

    /// <summary>Protected helper function for static SearchPaths</summary>
    /// <returns></returns>
    protected abstract string[] GetSearchPaths();
    /// <summary>Protected helper function for static SearchPaths</summary>
    /// <param name="paths"></param>
    protected abstract void SetSearchPaths(string[] paths);

    /// <summary>
    /// Initializes a new instance of the <see cref="PythonScript"/> class.
    /// </summary>
    protected PythonScript()
    {
      ScriptContextDoc = null;
      Output = RhinoApp.Write;
    }

    /// <summary>
    /// Compiles a class in a quick-to-execute proxy.
    /// </summary>
    /// <param name="script">A string text.</param>
    /// <returns>A Python compiled code instance.</returns>
    /// <since>5.0</since>
    public abstract PythonCompiledCode Compile(string script);

    /// <summary>
    /// Determines if the main scripting context has a variable with a name.
    /// </summary>
    /// <param name="name">The variable name.</param>
    /// <returns>true if the variable is present.</returns>
    /// <since>5.0</since>
    public abstract bool ContainsVariable(string name);

    /// <summary>
    /// Retrieves all variable names in the script.
    /// </summary>
    /// <returns>An enumerable set with all names of the variables.</returns>
    /// <since>5.0</since>
    public abstract System.Collections.Generic.IEnumerable<string> GetVariableNames();

    /// <summary>
    /// Gets the object associated with a variable name in the main scripting context.
    /// </summary>
    /// <param name="name">A variable name.</param>
    /// <returns>The variable object.</returns>
    /// <since>5.0</since>
    public abstract object GetVariable(string name);

    /// <summary>
    /// Sets a variable with a name and an object. Object can be null (Nothing in Visual Basic).
    /// </summary>
    /// <param name="name">A valid variable name in Python.</param>
    /// <param name="value">A valid value for that variable name.</param>
    /// <since>5.0</since>
    public abstract void SetVariable(string name, object value);

    /// <summary>
    /// Sets a variable for runtime introspection.
    /// </summary>
    /// <param name="name">A variable name.</param>
    /// <param name="value">A variable value.</param>
    /// <since>5.0</since>
    public virtual void SetIntellisenseVariable(string name, object value) { }

    /// <summary>
    /// Removes a defined variable from the main scripting context.
    /// </summary>
    /// <param name="name">The variable name.</param>
    /// <since>5.0</since>
    public abstract void RemoveVariable(string name);

    /// <summary>
    /// Evaluates statements and an expression in the main scripting context.
    /// </summary>
    /// <param name="statements">One or several statements.</param>
    /// <param name="expression">An expression.</param>
    /// <returns>The expression result.</returns>
    /// <since>5.0</since>
    public abstract object EvaluateExpression(string statements, string expression);

    /// <summary>
    /// Executes a Python file. The file is executed in a new, __main__ scope.
    /// </summary>
    /// <param name="path">The path to the file.</param>
    /// <returns>true if the file executed. This method can throw scripting-runtime based exceptions.</returns>
    /// <since>5.0</since>
    public abstract bool ExecuteFile(string path);

    /// <summary>
    /// Executes a Python file in the calling script scope. All old variables are kept.
    /// </summary>
    /// <param name="path">The path to the file.</param>
    /// <returns>true if the file executed. This method can throw scripting-runtime based exceptions.</returns>
    /// <since>7.0</since>
    public abstract bool ExecuteFileInScope(string path);

    /// <summary>
    /// Executes a Python string.
    /// </summary>
    /// <param name="script">A Python text.</param>
    /// <returns>true if the file executed. This method can throw scripting-runtime based exceptions.</returns>
    /// <since>5.0</since>
    public abstract bool ExecuteScript(string script);

    /// <summary>
    /// Retrieves a meaningful representation of the call stack.
    /// </summary>
    /// <param name="ex">An exception that was thrown by some of the methods in this class.</param>
    /// <returns>A string that represents the Python exception.</returns>
    /// <since>5.0</since>
    public abstract string GetStackTraceFromException(Exception ex);

    /// <summary>
    /// Gets or sets the Python script "print()" target.
    /// <para>By default string output goes to the Rhino.RhinoApp.Write function.
    /// Set Output if you want to redirect the output from python to a different function
    /// while this script executes.</para>
    /// </summary>
    /// <since>5.0</since>
    public Action<string> Output { get; set; }

    /// <summary>
    /// object set to variable held in scriptcontext.doc.
    /// </summary>
    /// <since>5.0</since>
    public object ScriptContextDoc { get; set; }

    /// <summary>
    /// Command associated with this script. Used for localiation
    /// </summary>
    /// <since>6.0</since>
    public Commands.Command ScriptContextCommand { get; set; }

    /// <summary>
    /// Gets or sets a context unique identified.
    /// </summary>
    /// <since>5.0</since>
    public int ContextId
    {
      get { return m_context_id; }
      set { m_context_id = value; }
    }
    int m_context_id = 1;

    /// <summary>
    /// Creates a control where the user is able to type Python code.
    /// </summary>
    /// <param name="script">A starting script.</param>
    /// <param name="helpcallback">A method that is called when help is shown for a function, a class or a method.</param>
    /// <returns>A Windows Forms control.</returns>
    /// <since>5.0</since>
    public abstract object CreateTextEditorControl(string script, Action<string> helpcallback);

    /// <summary>
    /// Setups the script context. Use a RhinoDoc instance unless unsure.
    /// </summary>
    /// <param name="doc">Document.</param>
    /// <since>6.0</since>
    public virtual void SetupScriptContext(object doc)
    {
    }
  }

  /// <summary>
  /// Defines risky actions that need to be reported in crash exceptions
  /// </summary>
  public class RiskyAction : IDisposable
  {
    IntPtr m_ptr_risky_action;
    /// <summary> Always create this in a using block </summary>
    /// <param name="description"></param>
    /// <param name="file"></param>
    /// <param name="member"></param>
    /// <param name="line"></param>
    /// <since>6.0</since>
    public RiskyAction(string description, [CallerFilePath] string file="", [CallerMemberName] string member="", [CallerLineNumber] int line=0)
    {
      m_ptr_risky_action = UnsafeNativeMethods.CRhRiskyActionSpy_New(description, member, file, line);
    }

    /// <summary>
    /// IDisposable implementation
    /// </summary>
    /// <since>6.0</since>
    public void Dispose()
    {
      UnsafeNativeMethods.CRhRiskyActionSpy_Delete(m_ptr_risky_action);
      m_ptr_risky_action = IntPtr.Zero;
    }
  }
#endif

  /// <summary>
  /// Get platform specific services that are used internally for
  /// general cross platform funtions in RhinoCommon. This includes
  /// services like localization and GUI components that have concrete
  /// implementations in the RhinoWindows or RhinoMac assemblies
  /// </summary>
  public interface IPlatformServiceLocator
  {
    /// <summary>Used to get service of a specific type</summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    /// <since>6.0</since>
    T GetService<T>() where T : class;
  }

  class DoNothingLocator : IPlatformServiceLocator
  {
    public T GetService<T>() where T : class
    {
      return null;
    }
  }

  /// <summary>
  /// Interface for querying information about the operating system Rhino is running on.
  /// </summary>
  interface IOperatingSystemInformation
  {
    /// <summary>
    /// Returns Operating System Installation Type: "Client" | "Server" | "Unknown"
    /// </summary>
    string InstallationType { get; }
    /// <summary>
    /// Returns Operating System Edition: "Professional" | "ServerDatacenter" | ... | "Unknown"
    /// </summary>
    string Edition { get; }
    /// <summary>
    /// Returns Operating System Product Name "Windows 10 Pro" | "Windows Server 2008 R2 Datacenter" | ... | "Unknown"
    /// </summary>
    string ProductName { get; }
    /// <summary>
    /// Returns Operating System Version "6.1" | "6.3" | ... | "Unknown"
    /// </summary>
    string Version { get; }
    /// <summary>
    /// Returns Operating System Build Number "11763" | "7601" | ... | "Unknown"
    /// </summary>
    string BuildNumber { get; }
    /// <summary>
    /// Checks if Operating System is running in a Windows Container
    /// </summary>
    bool IsRunningInWindowsContainer { get; }

  }

  class DoNothingOperatingSystemInformationService : IOperatingSystemInformation
  {
    public string InstallationType => throw new NotImplementedException();

    public string Edition => throw new NotImplementedException();

    public string ProductName => throw new NotImplementedException();

    public string Version => throw new NotImplementedException();

    public string BuildNumber => throw new NotImplementedException();

    public bool IsRunningInWindowsContainer => throw new NotImplementedException();
  }

  /// <summary>
  /// Contains static methods to deal with the runtime environment.
  /// </summary>
  public static class HostUtils
  {
#if RHINO_SDK
    /// <summary>
    /// Calls Assembly.LoadFrom in .NET 4.8. May call a different routine under .NET 7
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    /// <since>8.0</since>
    public static System.Reflection.Assembly LoadAssemblyFrom(string path)
    {
#if NETFRAMEWORK
      return System.Reflection.Assembly.LoadFrom(path);
#else
      string fullPath = Path.GetFullPath(path);

      if (!s_loadFromHandlerSet)
      {
        lock (s_loadFromAssemblyList)
        {
          if (!s_loadFromHandlerSet)
          {
            AppDomain.CurrentDomain.AssemblyResolve += LoadFromResolveHandler;
            s_loadFromHandlerSet = true;
          }
        }
      }

      // Add the path to the LoadFrom path list which we will consult
      // before handling the resolves in our handler.
      lock (s_loadFromAssemblyList)
      {
        if (!s_loadFromAssemblyList.Contains(fullPath))
        {
          s_loadFromAssemblyList.Add(fullPath);
        }
      }

      // LoadFromAssemblyPath can generate an error so let's check for previous errors here.
      UnsafeNativeMethods.RHC_GetLastWindowsError();

      // Curtis:
      // Load in the same context as Rhino vs. always the Default context.
      // this should be the default context when running Rhino normally,
      // however this will be a separate context when running Rhino.Inside on Mac.
      Assembly assembly = RhinoLoadContext.LoadFromAssemblyPath(fullPath);

      // Discard error produced by LoadFromAssemblyPath.
      // Error: "The system cannot find the file specified."
      // The error doesn't seem to be causing any problems so let's discard it.
      // NOTE: Investigation into why this error happens in RH-78218.
      UnsafeNativeMethods.RHC_MaskLastWindowsError(2); // ERROR_FILE_NOT_FOUND
      UnsafeNativeMethods.RHC_GetLastWindowsError();

      return assembly;
#endif
    }

    /// <summary>
    /// Gets the Rhino system managed assembly directory.
    /// </summary>
    /// <since>8.0</since>
    public static string RhinoAssemblyDirectory
    {
      get
      {
        var location = Path.GetDirectoryName(typeof(HostUtils).Assembly.Location);

        if (RunningInNetCore && RunningOnWindows)
        {
          // On Windows we're in netcore, get the parent directory of this assembly.
          location = Path.GetDirectoryName(location);
        }

        return location;
      }
    }

#if NET
    // Modified from https://github.com/dotnet/runtime/blob/main/src/libraries/System.Private.CoreLib/src/System/Reflection/Assembly.cs

    private static readonly List<string> s_loadFromAssemblyList = new List<string>();
    private static bool s_loadFromHandlerSet;
    private static AssemblyLoadContext s_RhinoLoadContext;
    private static AssemblyLoadContext RhinoLoadContext => s_RhinoLoadContext ?? (s_RhinoLoadContext = AssemblyLoadContext.GetLoadContext(typeof(HostUtils).Assembly));

    private static Assembly LoadFromResolveHandler(object sender, ResolveEventArgs args)
    {
      Assembly requestingAssembly = args.RequestingAssembly;
      if (requestingAssembly == null)
        return null;

      // Requesting assembly for LoadFrom is always loaded in defaultContext - proceed only if that
      // is the case.
      if (RhinoLoadContext != AssemblyLoadContext.GetLoadContext(requestingAssembly))
        return null;

      // Get the path where requesting assembly lives and check if it is in the list
      // of assemblies for which LoadFrom was invoked.
      string requestorPath = requestingAssembly.Location;
      if (string.IsNullOrEmpty(requestorPath))
        return null;

      requestorPath = Path.GetFullPath(requestorPath);

      lock (s_loadFromAssemblyList)
      {
        // If the requestor assembly was not loaded using LoadFrom, exit.
        if (!s_loadFromAssemblyList.Contains(requestorPath))
        {
          return null;
        }
      }

      // Requestor assembly was loaded using loadFrom, so look for its dependencies
      // in the same folder as it.
      // Form the name of the assembly using the path of the assembly that requested its load.
      AssemblyName requestedAssemblyName = new AssemblyName(args.Name!);

      // Skip resources
      if (requestedAssemblyName.Name.EndsWith(".resources", StringComparison.OrdinalIgnoreCase))
        return null;

      string requestedAssemblyPath = Path.Combine(Path.GetDirectoryName(requestorPath)!, requestedAssemblyName.Name + ".dll");
      if (!File.Exists(requestedAssemblyPath))
        return null;

      // Load the dependency via LoadFrom so that it goes through the same path of being in the LoadFrom list.
      return LoadAssemblyFrom(requestedAssemblyPath);
    }
#endif

    static System.Collections.Concurrent.ConcurrentBag<IDisposable> g_objectsToDisposeOnMainThread = new System.Collections.Concurrent.ConcurrentBag<IDisposable>();
    internal static void DeleteObjectsOnMainThread(object sender, Rhino.Commands.CommandEventArgs e)
    {
      IDisposable item;
      while (g_objectsToDisposeOnMainThread.TryTake(out item))
      {
        if (item != null)
          item.Dispose();
      }
    }
    internal static void AddObjectsToDeleteOnMainThread(IDisposable item)
    {
      g_objectsToDisposeOnMainThread.Add(item);
    }

    /// <summary>
    /// Indicates whether Rhino is running inside another application.
    /// returns false if Rhino.exe is the top-level application.
    /// returns true if some other application is the top-level application.
    /// </summary>
    /// <since>7.0</since>
    public static bool RunningAsRhinoInside
    {
      get
      {
        string processName;
        Version processVersion;
        GetCurrentProcessInfo(out processName, out processVersion);
        if (processName == "Rhino")
        {
          return false;
        }
        else
        {
          return true;
        }
      }
    }

    /// <summary>
    /// Returns information about the current process. If Rhino is the top level process,
    /// processName is "Rhino". Otherwise, processName is the name, without extension, of the main
    /// module that is executing. For example, "compute.backend" or "Revit".
    ///
    /// processVersion is the System.Version of the running process. It is the FileVersion
    /// of the executable.
    /// </summary>
    /// <since>6.15</since>
    public static void GetCurrentProcessInfo(out string processName, out Version processVersion)
    {
      if (RunningOnOSX || RunningOniOS)
      {
#if RHINO_SDK
        processVersion = RhinoApp.Version;
#else
        processVersion = new Version(RhinoBuildConstants.VERSION_STRING);
#endif
        processName = "Rhino";
      }
      else
      {
        var fvi = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileVersionInfo;
        processVersion = new Version(fvi.FileMajorPart, fvi.FileMinorPart, fvi.FileBuildPart, fvi.FilePrivatePart);

        var moduleName = System.Diagnostics.Process.GetCurrentProcess().MainModule.ModuleName;
        if (moduleName == "Rhinoceros" || moduleName == "rhino")  // Mac Rhino returns Rhinoceros //RH-57096
          moduleName = "Rhino";
        processName = System.IO.Path.GetFileNameWithoutExtension(moduleName);
      }
    }
#endif

    /// <summary>
    /// Returns Operating System Edition: "Professional" | "ServerDatacenter" | ... | "Unknown"
    /// </summary>
    /// <since>6.15</since>
    public static string OperatingSystemEdition
    {
      get
      {
        var psl = GetPlatformService<IOperatingSystemInformation>();
        return psl.Edition;
      }
    }

    /// <summary>
    /// Returns Operating System Installation Type: "Client" | "Server" | "Unknown"
    /// </summary>
    /// <since>6.15</since>
    public static string OperatingSystemInstallationType
    {
      get
      {
        var psl = GetPlatformService<IOperatingSystemInformation>();
        return psl.InstallationType;
      }
    }
    /// <summary>
    /// Returns Operating System Edition: "Professional" | "ServerDatacenter" | ... | "Unknown"
    /// </summary>
    /// <since>6.15</since>
    public static string OperatingSystemProductName
    {
      get
      {
        var psl = GetPlatformService<IOperatingSystemInformation>();
        return psl.ProductName;
      }
    }
    /// <summary>
    /// Returns Operating System Version "6.1" | "6.3" | ... | "Unknown"
    /// </summary>
    /// <since>6.15</since>
    public static string OperatingSystemVersion
    {
      get
      {
        //RH-56630
        if (RunningOnWindows)
        {
          return System.Environment.OSVersion.Version.Major.ToString();
        }
        else
        {
          var psl = GetPlatformService<IOperatingSystemInformation>();
          return psl.Version;
        }
      }
    }
    /// <summary>
    /// Returns Operating System Build Number "11763" | "7601" | ... | "Unknown"
    /// </summary>
    /// <since>6.15</since>
    public static string OperatingSystemBuildNumber
    {
      get
      {
        var psl = GetPlatformService<IOperatingSystemInformation>();
        return psl.BuildNumber;
      }
    }

#if RHINO_SDK
    /// <summary>
    /// Get list of printers available on this system
    /// </summary>
    /// <returns></returns>
    /// <since>8.0</since>
    public static string[] GetPrinterNames()
    {
      using (var names = new InteropWrappers.ClassArrayString())
      {
        IntPtr ptrNames = names.NonConstPointer();
        UnsafeNativeMethods.RHC_GetPrinterNames(ptrNames);
        return names.ToArray();
      }
    }

    /// <summary>
    /// Get list of form names available for a given printer
    /// </summary>
    /// <param name="printerName">name or printer to query</param>
    /// <returns></returns>
    /// <since>8.0</since>
    public static string[] GetPrinterFormNames(string printerName)
    {
      using (var names = new InteropWrappers.ClassArrayString())
      {
        IntPtr ptrNames = names.NonConstPointer();
        UnsafeNativeMethods.RHC_GetPrinterFormNames(printerName, ptrNames);
        return names.ToArray();
      }
    }

    /// <summary>
    /// Get the size of a sheet for a given form name / printer combination
    /// </summary>
    /// <param name="printerName"></param>
    /// <param name="formName"></param>
    /// <param name="widthMillimeters"></param>
    /// <param name="heightMillimeters"></param>
    /// <returns>true on success</returns>
    /// <since>8.0</since>
    public static bool GetPrinterFormSize(string printerName, string formName, out double widthMillimeters, out double heightMillimeters)
    {
      widthMillimeters = 0.0;
      heightMillimeters = 0.0;
      UnsafeNativeMethods.RHC_GetPrinterFormSize(printerName, formName, ref widthMillimeters, ref heightMillimeters);
      return widthMillimeters > 0.0;
    }

    /// <summary>
    /// Get limit margins for a given form (page size) and a given printer.
    /// This is the physical limit area that a printer can print on a given page
    /// </summary>
    /// <param name="printerName"></param>
    /// <param name="formName"></param>
    /// <param name="portrait"></param>
    /// <param name="leftMillimeters"></param>
    /// <param name="topMillimeters"></param>
    /// <param name="rightMillimeters"></param>
    /// <param name="bottomMillimeters"></param>
    /// <returns>true on success</returns>
    /// <since>8.5</since>
    public static bool GetPrinterFormMargins(string printerName, string formName, bool portrait,
      out double leftMillimeters, out double topMillimeters, out double rightMillimeters, out double bottomMillimeters)
    {
      leftMillimeters = 0.0;
      topMillimeters = 0.0;
      rightMillimeters = 0.0;
      bottomMillimeters = 0.0;
      bool rc = UnsafeNativeMethods.RHC_GetPrinterFormMargins(printerName, formName, portrait, ref leftMillimeters,
        ref topMillimeters, ref rightMillimeters, ref bottomMillimeters);
      return rc;
    }

    /// <summary>
    /// Get the output resolution for a given printer.
    /// </summary>
    /// <param name="printerName"></param>
    /// <param name="horizontal">get the horizontal or vertical resolution</param>
    /// <returns>
    /// Dot per inch resolution for a given printer on success. 0 if an error occurred
    /// </returns>
    /// <since>8.2</since>
    public static double GetPrinterDPI(string printerName, bool horizontal)
    {
      return UnsafeNativeMethods.RHC_GetPrinterDPI(printerName, horizontal);
    }

#endif

    static Dictionary<string, IPlatformServiceLocator> g_platform_locator = new Dictionary<string, IPlatformServiceLocator>();

    /// <summary>For internal use only. Loads an assembly for dependency injection via IPlatformServiceLocator.</summary>
    /// <param name="assemblyPath">The relative path of the assembly, relative to the position of RhinoCommon.dll</param>
    /// <param name="typeFullName">The full name of the type that is IPlatformServiceLocator. This is optional.</param>
    /// <typeparam name="T">The type of the service to be instantiated.</typeparam>
    /// <returns>An instance, or null.</returns>
    /// <since>6.0</since>
    public static T GetPlatformService<T>(string assemblyPath = null, string typeFullName = null) where T : class
    {
      if (string.IsNullOrEmpty(assemblyPath))
      {
#if ON_RUNTIME_APPLE_IOS
        assemblyPath = "RhinoiOS.dll";
#else
        assemblyPath = RunningOnWindows ? "RhinoWindows.dll" : "RhinoMac.dll";
#endif
      }

      if (!g_platform_locator.ContainsKey(assemblyPath))
      {
        g_platform_locator[assemblyPath] = new DoNothingLocator();
#if RHINO_SDK
        var service_type = typeof(IPlatformServiceLocator);
        string path;
        if (RunningOnWindows && RunningInNetCore)
        {
          path = System.IO.Path.Combine(RhinoAssemblyDirectory, "netcore", assemblyPath);
          if (!File.Exists(path))
            path = System.IO.Path.Combine(RhinoAssemblyDirectory, assemblyPath);
        }
        else
        {
          path = System.IO.Path.Combine(RhinoAssemblyDirectory, assemblyPath);
        }
        var platform_assembly = LoadAssemblyFrom(path);

        if (typeFullName == null)
        {
          Type[] types = platform_assembly.GetExportedTypes();
          foreach (var t in types)
          {
            if (!t.IsAbstract && service_type.IsAssignableFrom(t))
            {
              g_platform_locator[assemblyPath] = Activator.CreateInstance(t) as IPlatformServiceLocator;
              break;
            }
          }
        }
        else
        {
          object instantiated = platform_assembly.CreateInstance(typeFullName);
          g_platform_locator[assemblyPath] = instantiated as IPlatformServiceLocator;
        }
#endif
      }
      return g_platform_locator[assemblyPath].GetService<T>();
    }

#if RHINO_SDK
    /// <summary>
    /// Inspects a dll to see if it is compiled as native code or as a .NET assembly
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    /// <since>6.0</since>
    public static bool IsManagedDll(string path)
    {
      return UnsafeNativeMethods.RHC_IsManagedDll(path);
    }

    /// <summary>
    /// Clear FPU exception and busy flags (Intel assembly fnclex)
    /// </summary>
    /// <since>6.0</since>
    public static void ClearFpuExceptionStatus()
    {
      UnsafeNativeMethods.RHC_ON_FPU_ClearExceptionStatus();
    }

    static Dictionary<string, EventHandler<NamedParametersEventArgs>> _namedCallbacks = new Dictionary<string, EventHandler<NamedParametersEventArgs>>();
    static GCHandle _namedCallbackHandle;
    internal delegate int NamedCallback(IntPtr name, IntPtr ptrNamedParams);
    static readonly NamedCallback g_named_callback = ExecuteNamedCallbackHelper;
    static IntPtr _namedCallbackFunctionPointer = IntPtr.Zero;

    /// <summary>Register a named callback</summary>
    /// <param name="name"></param>
    /// <param name="callback"></param>
    /// <since>6.15</since>
    public static void RegisterNamedCallback(string name, EventHandler<NamedParametersEventArgs> callback)
    {
      _namedCallbacks[name] = callback;
      if (!_namedCallbackHandle.IsAllocated)
      {
        _namedCallbackHandle = GCHandle.Alloc(g_named_callback);
        _namedCallbackFunctionPointer = Marshal.GetFunctionPointerForDelegate(g_named_callback);
      }
      UnsafeNativeMethods.RHC_RhRegisterNamedCallbackProc(name, _namedCallbackFunctionPointer);
    }

    /// <summary>
    /// Remove a named callback from the dictionary of callbacks
    /// </summary>
    /// <param name="name"></param>
    /// <since>7.18</since>
    public static void RemoveNamedCallback(string name)
    {
      if (!string.IsNullOrEmpty(name))
        _namedCallbacks.Remove(name);
    }

    /// <summary>
    /// Execute a named callback
    /// </summary>
    /// <param name="name"></param>
    /// <param name="args"></param>
    /// <since>7.0</since>
    public static bool ExecuteNamedCallback(string name, NamedParametersEventArgs args)
    {
      // Don't directly call the function on our dictionary and instead indirectly call
      // through C++. This allows for cross AppDomain calls
      return UnsafeNativeMethods.RHC_RhExecuteNamedCallback(name, args.m_pNamedParams);
    }

    [MonoPInvokeCallback(typeof(NamedCallback))]
    static int ExecuteNamedCallbackHelper(IntPtr name, IntPtr ptrNamedParams)
    {
      try
      {
        string _name = StringWrapper.GetStringFromPointer(name);
        EventHandler<NamedParametersEventArgs> callback = null;
        if (_namedCallbacks.TryGetValue(_name, out callback) && callback != null)
        {
          using (var e = new NamedParametersEventArgs(ptrNamedParams))
          {
            callback(null, e);
            e.m_pNamedParams = IntPtr.Zero;
          }
          return 1;
        }
      }
      catch (NotLicensedException)
      {
        throw;
      }
      catch (Exception ex)
      {
        ExceptionReport(ex);
      }
      return 0;
    }

    static List<Tuple<string, Type>> _customComputeEndPoints;
    /// <summary>
    /// Register a class that can participate as a compute endpoint
    /// </summary>
    /// <param name="endpointPath"></param>
    /// <param name="t"></param>
    /// <since>7.0</since>
    public static void RegisterComputeEndpoint(string endpointPath, Type t)
    {
      if (_customComputeEndPoints == null)
        _customComputeEndPoints = new List<Tuple<string, Type>>();
      _customComputeEndPoints.Add(new Tuple<string, Type>(endpointPath, t));
    }

    internal static int CustomComputeEndpointCount()
    {
      if (_customComputeEndPoints != null)
        return _customComputeEndPoints.Count;
      return 0;
    }
    /// <summary>
    /// Used by compute to define custom endpoints
    /// </summary>
    /// <returns></returns>
    /// <since>7.0</since>
    public static Tuple<string, Type>[] GetCustomComputeEndpoints()
    {
      if (_customComputeEndPoints != null)
        return _customComputeEndPoints.ToArray();
      return new Tuple<string, Type>[0];
    }
#endif

    /// <summary>
    /// Returns list of directory names where additional assemblies (plug-ins, DLLs, Grasshopper components)
    /// may be located
    /// </summary>
    /// <returns></returns>
    /// <since>5.0</since>
    public static string[] GetAssemblySearchPaths()
    {
#if RHINO_SDK
      // inlude directory where RhinoCommon is located
      List<string> directories = new List<string>();
      string rhino_common_location = typeof(HostUtils).Assembly.Location;
      directories.Add(System.IO.Path.GetDirectoryName(rhino_common_location));
      directories.AddRange(PlugIn.GetInstalledPlugInFolders());

      // 6 May 2020 S. Baer
      // It seems like there are cases where Grasshopper isn't getting picked up as an installed
      // plug-in on OSX at start. Just tell the resolver "this is a good place to look"
      if(HostUtils.RunningOnOSX)
      {
        string rhinoCommonDir = System.IO.Path.GetDirectoryName(rhino_common_location);
        string grasshopperDirectory = System.IO.Path.Combine(rhinoCommonDir, "ManagedPlugIns", "GrasshopperPlugin.rhp");
        directories.Add(grasshopperDirectory);
      }

      // 3 June 2019 S. Baer (RH-48975)
      // Add the grasshopper components directory so we can find galapagos and kangaroosolver
      string pathToAdd = null;
      foreach(var dir in directories)
      {
        // 26.1.2022 Joshua Kennedy RH-67135
        // The ConnectionsUI plugin was failing to load because
        // it couldn't find KangarooSolver.dll. It looks like it was looking in
        // the GH 2 directories and not GH 1. This should force it to be just GH 1.
        if (dir.Contains("Grasshopper") && !dir.Contains("Grasshopper2"))
        {
          var path = System.IO.Path.Combine(dir, "Components");
          if( System.IO.Directory.Exists(path))
          {
            pathToAdd = path;
            break;
          }
        }
      }
      if( !string.IsNullOrWhiteSpace(pathToAdd) )
      {
        directories.Add(pathToAdd);
      }

      // include all auto-install directories (that aren't already included)
      // grasshopper will prune the folders that it doesn't care about
      foreach (var dir in GetActivePlugInVersionFolders())
      {
        if (!directories.Contains(dir.FullName))
        {
          directories.Add(dir.FullName);
        }
      }
      return directories.ToArray();
#else
      return new string[0];
#endif
    }

    /// <summary>
    /// DO NOT USE UNLESS YOU ARE CERTAIN ABOUT THE IMPLICATIONS.
    /// <para>This is an expert user function which should not be needed in most
    /// cases. This function is similar to a const_cast in C++ to allow an object
    /// to be made temporarily modifiable without causing RhinoCommon to convert
    /// the class from const to non-const by creating a duplicate.</para>
    ///
    /// <para>You must call this function with a true parameter, make your
    /// modifications, and then restore the const flag by calling this function
    /// again with a false parameter. If you have any questions, please
    /// contact McNeel developer support before using!</para>
    /// </summary>
    /// <param name="geometry">Some geometry.</param>
    /// <param name="makeNonConst">A boolean value.</param>
    /// <since>5.0</since>
    public static void InPlaceConstCast(Rhino.Geometry.GeometryBase geometry, bool makeNonConst)
    {
      if (makeNonConst)
      {
        geometry.ApplyConstCast();
      }
      else
      {
        geometry.RemoveConstCast();
      }
    }

    private static bool? _runningOnWindows = null;
    private static bool? _runningOnOSX = null;

    /// <summary>
    /// Tests if this process is currently executing on the Windows platform.
    /// </summary>
    /// <since>5.0</since>
    public static bool RunningOnWindows
    {
      get
      {
        if (!_runningOnWindows.HasValue)
        {
          _runningOnWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        }
        return _runningOnWindows.Value;
      }
    }

    /// <summary>
    /// Tests if this process is currently executing on the Mac OSX platform.
    /// </summary>
    /// <since>5.0</since>
    public static bool RunningOnOSX
    {
      get
      {
        if (!_runningOnOSX.HasValue)
        {
          // TODO: S. Baer - Why is RunningOniOS part of this decision?
          //       I'm assuming this is to get things running on iOS and that we
          //       can tune this up over time by adding a RunningOnApple propery
          _runningOnOSX = RuntimeInformation.IsOSPlatform(OSPlatform.OSX) || RunningOniOS;
        }
        return _runningOnOSX.Value;
      }
    }

    /// <summary>
    /// Tests if this process is currently executing on the iOS platform.
    /// </summary>
    /// <since>5.0</since>
    public static bool RunningOniOS
    {
      get
      {
#if ON_RUNTIME_APPLE_IOS
        return true;
#else
        return false;
#endif
      }
    }


#if RHINO_SDK
    /// <summary>
    /// Returns true when Rhino build is Beta or WIP, false otherwise
    /// </summary>
    /// <since>7.22</since>
    public static bool IsPreRelease
    {
      get
      {
        return Rhino.RhinoBuildConstants.PRE_RELEASE;
      }
    }

    /// <summary>
    /// Tests if this process is currently executing in a server environment.
    /// </summary>
    /// <since>7.8</since>
    public static bool RunningOnServer => string.Equals(OperatingSystemInstallationType, "server", StringComparison.InvariantCultureIgnoreCase);

    /// <summary>
    /// Tests if this process is currently executing inside a Windows Container.
    /// </summary>
    /// <since>7.1</since>
    public static bool RunningInWindowsContainer
    {
      get
      {
        if (RunningInRhino)
        {
          var psl = GetPlatformService<IOperatingSystemInformation>();
          return psl.IsRunningInWindowsContainer;
        }
        return false;
      }
    }

    internal static bool TryGetWindowsContainerId(out Guid containerId)
    {
      try
      {
        var key = @"HKEY_LOCAL_MACHINE\System\CurrentControlSet\Control";
        var containerIdObj = Microsoft.Win32.Registry.GetValue(key, "ContainerId", null);
        if (containerIdObj is string containerIdStr && !string.IsNullOrEmpty(containerIdStr))
        {
          return Guid.TryParse(containerIdStr, out containerId);
        }
      }
      catch { }

      containerId = Guid.Empty;
      return false;
    }

    /// <summary>
    /// Returns true if the host operating system is in dark mode and Rhino
    /// supports dark mode.
    /// </summary>
    /// <since>6.19</since>
    public static bool RunningInDarkMode => AdvancedSettings.DarkMode;

    private static string m_device_name;
    /// <summary>
    /// Name of the computer running Rhino. If the computer is part of a
    /// Windows Domain, the computer name has "@[DOMAIN]" appended.
    /// </summary>
    /// <since>6.0</since>
    public static string DeviceName
    {
      get
      {
        if (string.IsNullOrEmpty(m_device_name))
        {
          var machineName = System.Environment.MachineName;
          var userDomain = System.Environment.UserDomainName;

          if (string.Equals(machineName, userDomain, StringComparison.InvariantCultureIgnoreCase))
          {
            m_device_name = machineName;
          }
          else
          {
            m_device_name = string.Format("{0}@{1}", machineName, userDomain);
          }
        }
        return m_device_name;
      }
    }

    /// <summary>
    /// Gets the serial number of the computer running Rhino.
    /// </summary>
    /// <since>6.0</since>
    public static string ComputerSerialNumber
    {
      get
      {
        using (var string_holder = new Rhino.Runtime.InteropWrappers.StringWrapper())
        {
          IntPtr ptr_string = string_holder.NonConstPointer;
          UnsafeNativeMethods.RHC_GetComputerSerialNumber(ptr_string);
          return string_holder.ToString();
        }
      }
    }

    /// <summary>
    /// Get the current operating system language.
    /// </summary>
    /// <returns>A Windows LCID (on Windows and macOS).  On Windows, this will be
    /// LCID value regardless of those languages that Rhino supports.  On macOS, this only
    /// returns LCID values for languages that Rhino does support.</returns>
    /// <since>6.8</since>
    [CLSCompliant(false)]
    public static uint CurrentOSLanguage => UnsafeNativeMethods.RHC_RhCurrentOSLanguage();

    private static Guid m_device_id = Guid.Empty;
    /// <summary>
    /// The DeviceId is a unique, stable ID that anonymously identifies the device
    /// that Rhino is running on. It is computed based on hardware information that
    /// should not change when the OS is upgraded, or if commonly modified hardware
    /// are added or removed from the computer. The machine-specific information is
    /// hashed using a cryptographic hash to make it anonymous.
    /// </summary>
    /// <since>6.0</since>
    public static Guid DeviceId
    {
      get
      {
        bool SerialNumberIsHardwareBased = true;
        if (m_device_id == Guid.Empty)
        {
          // RH-62726 - use container id if running in a windows container
          if (RunningInWindowsContainer && TryGetWindowsContainerId(out m_device_id))
            return m_device_id;

          // Base the device ID solely on the HardwareSerialNumber, if one
          // exists. Otherwise, base it on the first Ethernet MacAddress.
          // The goal here is to generate a reasonably uniqe and stable
          // ID, so very little hardware information is used.
          string data = HardwareSerialNumber;
          if (string.IsNullOrWhiteSpace(data))
          {
            SerialNumberIsHardwareBased = false;
            data = MacAddress.ToString();
          }

          var bytes = System.Text.Encoding.UTF8.GetBytes(data);

          // Compute 16-bit hash, based on SHA256, because
          // SHA256 is FIPS compliant. MD5 and SHA1 are not.
          using (var hasher = new System.Security.Cryptography.SHA256CryptoServiceProvider())
          {
            var hash = hasher.ComputeHash(bytes);
            var hash16 = hash.Take(16).ToArray();

            // Set last digit in resulting GUID to 0
            // to specify that this Device.ID was generated
            // from a valid HardwareSerialNumber.
            hash16[15] = (byte)(hash16[15] >> 4 << 4);
            if (!SerialNumberIsHardwareBased)
              hash16[15] |= 0x1;

            // Set GUID
            m_device_id = new Guid(hash16);
          }
        }
        return m_device_id;
      }
    }

    private static string m_serial_number = "";
    private static string HardwareSerialNumber
    {
      get
      {
        if (!string.IsNullOrWhiteSpace(m_serial_number))
          return m_serial_number;

        if (RunningOnOSX)
        {
          m_serial_number = ComputerSerialNumber;
        }
        else
        {
          m_serial_number = WindowsBiosSerialNumber;
        }


        // Return serial number
        return m_serial_number;
      }
    }

    private static string WindowsBiosSerialNumber
    {
      get
      {
        try
        {
          var mc = new ManagementClass("Win32_ComputerSystemProduct");
          var coll = mc.GetInstances();
          foreach (var obj in coll)
          {
            var uuid = obj.Properties["UUID"].Value.ToString().ToLowerInvariant();
            var fullUuid = new Guid("ffffffffffffffffffffffffffffffff");
            if (uuid == fullUuid.ToString() || uuid == Guid.Empty.ToString())
            {
              continue;
            }
            return uuid;
          }
        }
        catch
        {
          // Don't crash just because there's no such management class.
        }
        return null;
      }
    }

    private static PhysicalAddress MacAddress
    {
      get
      {
        PhysicalAddress macAddress = null;

        NetworkInterface[] nics = NetworkInterface.GetAllNetworkInterfaces();
        Array.Sort(nics, (NetworkInterface x, NetworkInterface y) => { return x.Id.CompareTo(y.Id); });

        if (macAddress == null)
        {
          // First try: look for Ethernet NICs
          foreach (NetworkInterface nic in nics)
          {
            if (nic.NetworkInterfaceType != NetworkInterfaceType.Ethernet)
              continue;

            if (nic.GetPhysicalAddress() != null && !nic.GetPhysicalAddress().Equals(PhysicalAddress.None))
            {
              macAddress = nic.GetPhysicalAddress();
              break;
            }
          }
        }

        if (macAddress == null)
        {
          // Second try: look for anything we're sure we don't want
          foreach (NetworkInterface nic in nics)
          {
            switch (nic.NetworkInterfaceType)
            {
              case NetworkInterfaceType.Tunnel:
              case NetworkInterfaceType.Unknown:
              case NetworkInterfaceType.Loopback:
                continue;
            }

            if (nic.GetPhysicalAddress() != null && !nic.GetPhysicalAddress().Equals(PhysicalAddress.None))
            {
              macAddress = nic.GetPhysicalAddress();
              break;
            }
          }
        }

        return macAddress;
      }
    }


#endif
    /// <summary>
    /// Tests if this process is currently executing under the Mono runtime.
    /// </summary>
    /// <since>5.0</since>
    public static bool RunningInMono
    {
      get { return Type.GetType("Mono.Runtime") != null; }
    }

#if RHINO_SDK
    static bool? m_running_in_net_framework;

    /// <summary>
    /// Tests if this process is currently executing under the .NET Framework runtime.
    /// </summary>
    /// <since>8.0</since>
    public static bool RunningInNetFramework
    {
      get
      {
        if (m_running_in_net_framework == null)
        {
          m_running_in_net_framework = RunningOnWindows && RuntimeInformation.FrameworkDescription.StartsWith(".NET Framework", StringComparison.OrdinalIgnoreCase);
        }
        return m_running_in_net_framework.Value;
      }
    }

    static bool? m_running_in_net_core;

    /// <summary>
    /// Tests if this process is currently executing under the .NET Core runtime.
    /// </summary>
    /// <since>8.0</since>
    public static bool RunningInNetCore
    {
      get
      {
        if (m_running_in_net_core == null)
        {
          m_running_in_net_core = System.Environment.Version.Major >= 5 || RuntimeInformation.FrameworkDescription.StartsWith(".NET Core", StringComparison.OrdinalIgnoreCase);
        }
        return m_running_in_net_core.Value;
      }
    }

    static IEnumerable<string> m_system_reference_assemblies;

    /// <summary>
    /// Gets the system reference assemblies to use when compiling code dynamically with Roslyn.
    /// Includes RhinoCommon, Rhino.UI, and Eto.
    /// </summary>
    /// <remarks>
    /// Note that this list of assemblies is not guaranteed to be loadable and should only be used when compiling
    /// code dynamically.
    /// </remarks>
    /// <returns>An enumeration of paths to each of the rhino system assemblies to be used for compilation</returns>
    /// <since>8.0</since>
    public static IEnumerable<string> GetSystemReferenceAssemblies()
    {
      if (m_system_reference_assemblies == null)
      {
        // eliminate any duplicates and store the results so we don't have to figure this out each time
        m_system_reference_assemblies = GetSystemReferenceAssembliesInternal().Distinct().ToList().AsReadOnly();
      }
      return m_system_reference_assemblies;
    }

    static IEnumerable<string> GetSystemReferenceAssembliesInternal()
    {
      if (RunningInNetCore)
      {
        // Use all .NET Core runtime assemblies
        // Curtis: Should we use reference assemblies instead?
        var mscorlibLocation = Path.GetDirectoryName(typeof(object).Assembly.Location);
        foreach (var file in Directory.GetFiles(mscorlibLocation, "*.dll"))
        {
          var fileName = Path.GetFileName(file);
          if (fileName.IndexOf(".Native.", StringComparison.OrdinalIgnoreCase) != -1)
            continue;

          if (fileName.StartsWith("System.", StringComparison.OrdinalIgnoreCase))
            yield return file;
          else if (fileName.StartsWith("Microsoft.", StringComparison.OrdinalIgnoreCase))
            yield return file;
          else switch (fileName.ToLowerInvariant())
          {
            case "mscorlib.dll":
            case "windowsbase.dll":
            case "netstandard.dll":
              yield return file;
              break;
          }
        }
      }
      else
      {
        yield return typeof(System.Object).Assembly.Location; // mscorlib.dll
        yield return typeof(System.Uri).Assembly.Location; // System.dll
        yield return typeof(System.Xml.Formatting).Assembly.Location; // System.Xml.dll
        yield return typeof(System.Xml.Linq.XText).Assembly.Location; // System.Xml.Linq.dll
        yield return typeof(System.Linq.IQueryable).Assembly.Location; // System.Core.dll
        yield return typeof(System.Data.ConflictOption).Assembly.Location; // System.Data.dll
        yield return typeof(System.Net.AuthenticationManager).Assembly.Location; // System.Net.dll (System.dll) ??????
        yield return typeof(System.Net.Http.HttpClient).Assembly.Location; // System.Net.Http.dll
        yield return typeof(System.ServiceModel.BasicHttpBinding).Assembly.Location; // System.ServiceModel.dll

        yield return typeof(Microsoft.CSharp.RuntimeBinder.RuntimeBinderException).Assembly.Location; // Microsoft.CSharp.dll
        yield return typeof(Microsoft.VisualBasic.AppWinStyle).Assembly.Location; // Microsoft.VisualBasic.dll

        yield return typeof(System.Dynamic.DynamicObject).Assembly.Location; // System.Core.dll
        yield return typeof(System.Numerics.Complex).Assembly.Location; // System.Numerics.dll
        yield return typeof(System.Collections.Immutable.ImmutableArray).Assembly.Location; // System.Collections.Immutable.dll

        // add .NET Standard facade assemblies
        foreach (var file in GetFacadeAssemblies())
          yield return file;
      }

      // Common assemblies
      yield return typeof(System.Windows.Forms.Appearance).Assembly.Location; // System.Windows.Forms.dll
      yield return typeof(System.Drawing.Bitmap).Assembly.Location; // System.Drawing.dll (net48) / System.Drawing.Common.dll (net6+)

      // Rhino-specific assemblies
      yield return typeof(RhinoApp).Assembly.Location; // RhinoCommon.dll
      yield return System.Reflection.Assembly.Load("Eto").Location; // Eto.dll
      yield return System.Reflection.Assembly.Load("Rhino.UI").Location; // Rhino.UI.dll
    }

    static IEnumerable<string> GetFacadeAssemblies()
    {
#if !MONO_BUILD && NETFRAMEWORK
      string folder;
      foreach (var refasm in Microsoft.Build.Utilities.ToolLocationHelper.GetPathToReferenceAssemblies(".NETFramework", "4.8", string.Empty))
      {
        folder = Path.Combine(refasm, "Facades");

        if (Directory.Exists(folder))
        {
          return Directory.EnumerateFiles(folder, "*.dll");
        }
      }
      // Hard-coded fallback just in case..
      folder = @"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.8\Facades";

      if (Directory.Exists(folder))
      {
        return Directory.EnumerateFiles(folder, "*.dll");
      }
#endif
      return Enumerable.Empty<string>();
    }
#endif


    static int m_running_in_rhino_state; //0=unknown, 1=false, 2=true
    /// <summary>
    /// Tests if RhinoCommon is currently executing inside of the Rhino.exe process.
    /// There are other cases where RhinoCommon could be running; specifically inside
    /// of Visual Studio when something like a windows form is being worked on in the
    /// resource editor or running stand-alone when compiled to be used as a version
    /// of OpenNURBS.
    /// </summary>
    /// <since>5.0</since>
    public static bool RunningInRhino
    {
      get
      {
        if (m_running_in_rhino_state == 0)
        {
#if RHINO_SDK
          m_running_in_rhino_state = 1;
          try
          {
            if (0 != Rhino.RhinoApp.SdkVersion )
              m_running_in_rhino_state = 2;
          }
          catch (Exception)
          {
            m_running_in_rhino_state = 1;
          }
#else
          m_running_in_rhino_state = 1;
#endif
        }
        return (m_running_in_rhino_state == 2);
      }
    }

#if RHINO_SDK
    // 0== unknown
    // 1== loaded
    //-1== not loaded
    static int m_rdk_loadtest;
    /// <summary>
    /// Determines if the RDK is loaded.
    /// </summary>
    /// <param name="throwOnFalse">if the RDK is not loaded, then throws a
    /// <see cref="RdkNotLoadedException"/>.</param>
    /// <param name="usePreviousResult">if true, then the last result can be used instaed of
    /// performing a full check.</param>
    /// <returns>true if the RDK is loaded; false if the RDK is not loaded. Note that the
    /// <see cref="RdkNotLoadedException"/> will hinder the retrieval of any return value.</returns>
    /// <since>5.0</since>
    public static bool CheckForRdk(bool throwOnFalse, bool usePreviousResult)
    {
      const int UNKNOWN = 0;
      const int LOADED = 1;
      const int NOT_LOADED = -1;

      if (UNKNOWN == m_rdk_loadtest || !usePreviousResult)
      {
        try
        {
          UnsafeNativeMethods.Rdk_LoadTest();
          m_rdk_loadtest = LOADED;
        }
        catch (Exception)
        {
          m_rdk_loadtest = NOT_LOADED;
        }
      }

      if (LOADED == m_rdk_loadtest)
        return true;

      if (throwOnFalse)
        throw new RdkNotLoadedException();
      return false;
    }

    /// <summary>
    /// Call this method to convert a relative path to an absolute path
    /// relative to the specified path.
    /// </summary>
    /// <param name="relativePath">
    /// Relative path to convert to an absolute path
    /// </param>
    /// <param name="bRelativePathisFileName">
    /// If true then lpsFrom is treated as a file name otherwise it is treated
    /// as a directory name
    /// </param>
    /// <param name="relativeTo">
    /// File or folder the path is relative to
    /// </param>
    /// <param name="bRelativeToIsFileName">
    /// If true then lpsFrom is treated as a file name otherwise it is treated
    /// as a directory name
    /// </param>
    /// <param name="pathOut">
    /// Reference to string which will receive the computed absolute path
    /// </param>
    /// <returns>
    /// Returns true if parameters are valid and lpsRelativePath is indeed
    /// relative to lpsRelativeTo otherwise returns false
    /// </returns>
    /// <since>6.0</since>
    public static bool GetAbsolutePath(string relativePath, bool bRelativePathisFileName, string relativeTo,bool bRelativeToIsFileName, out string pathOut)
    {
      using (var string_holder = new StringHolder())
      {
        var string_pointer = string_holder.NonConstPointer();
        var success = UnsafeNativeMethods.CRhinoFileUtilities_PathAbsolutFromRelativeTo(relativePath, bRelativePathisFileName, relativeTo, bRelativePathisFileName, string_pointer);
        pathOut = success ? string_holder.ToString() : string.Empty;
        return success;
      }
    }
    /// <summary>
    /// Check to see if the file extension is a valid Rhino file extension.
    /// </summary>
    /// <param name="fileExtension"></param>
    /// <returns>
    /// Returns true if fileExtension is ".3dm", "3dm", ".3dx" or "3dx",
    /// ignoring case.
    /// </returns>
    /// <since>6.0</since>
    public static bool IsRhinoFileExtension(string fileExtension)
    {
      return UnsafeNativeMethods.CRhinoFileUtilities_Is3dmFileExtension(System.IO.Path.GetExtension(fileExtension));
    }
    /// <summary>
    /// Strip file extension from file name and check to see if it is a valid
    /// Rhino file extension.
    /// </summary>
    /// <param name="fileName">
    /// File name to check.
    /// </param>
    /// <returns>
    /// Returns true if the file name has an extension like 3dm.
    /// </returns>
    /// <since>6.0</since>
    public static bool FileNameEndsWithRhinoExtension(string fileName)
    {
      return !string.IsNullOrEmpty(fileName) && IsRhinoFileExtension(System.IO.Path.GetExtension(fileName));
    }
    /// <summary>
    /// Check to see if the file extension is a valid Rhino file extension.
    /// </summary>
    /// <param name="fileExtension"></param>
    /// <returns>
    /// Return true if fileExtension is ".3dmbak", "3dmbak", ".3dm.bak", "3dm.bak",
    /// ".3dx.bak" or "3dx.bak", ignoring case.
    /// </returns>
    /// <since>6.0</since>
    public static bool IsRhinoBackupFileExtension(string fileExtension)
    {
      return UnsafeNativeMethods.CRhinoFileUtilities_Is3dmBackupFileExtension(System.IO.Path.GetExtension(fileExtension));
    }
    /// <summary>
    /// Strip file extension from file name and check to see if it is a valid
    /// Rhino backup file extension.
    /// </summary>
    /// <param name="fileName">
    /// File name to check.
    /// </param>
    /// <returns>
    /// Returns true if the file name has an extension like 3dmbak.
    /// </returns>
    /// <since>6.0</since>
    public static bool FileNameEndsWithRhinoBackupExtension(string fileName)
    {
      return !string.IsNullOrEmpty(fileName) && IsRhinoBackupFileExtension(System.IO.Path.GetExtension(fileName));
    }
#endif

    #region Events
    /// <summary>
    /// Safetly invokes (catching exceptions) the method represented by the provided <paramref name="handler"/>.
    /// </summary>
    /// <param name="handler">Event handler</param>
    /// <param name="sender">Event sender</param>
    /// <remarks>All handlers will be called even one of it throws an exception.</remarks>
    internal static void SafeInvoke(this EventHandler handler, object sender = null)
    {
      foreach (var h in handler.GetInvocationList())
      {
        try { h.DynamicInvoke(sender, EventArgs.Empty); }
        catch (Exception ex) { ExceptionReport(ex.InnerException ?? ex); }
      }
    }

    /// <summary>
    /// Safetly invokes (catching exceptions) the method represented by the provided <paramref name="handler"/>.
    /// </summary>
    /// <param name="handler">Event handler</param>
    /// <param name="sender">Event sender</param>
    /// <param name="args">Event arguments</param>
    /// <remarks>All handlers will be called even one of it throws an exception.</remarks>
    internal static void SafeInvoke<TEventArgs>(this EventHandler<TEventArgs> handler, object sender, TEventArgs args)
    {
      foreach (var h in handler.GetInvocationList())
      {
        try { h.DynamicInvoke(sender, args); }
        catch (Exception ex) { ExceptionReport(ex.InnerException ?? ex); }
      }
    }
    #endregion

    static bool m_bSendDebugToRhino; // = false; initialized by runtime
    /// <summary>
    /// Prints a debug message to the Rhino Command Line.
    /// The message will only appear if the SendDebugToCommandLine property is set to true.
    /// </summary>
    /// <param name="msg">Message to print.</param>
    /// <since>5.0</since>
    public static void DebugString(string msg)
    {
#if RHINO_SDK
      if (m_bSendDebugToRhino)
        RhinoApp.WriteLine(msg);
      else
        UnsafeNativeMethods.RHC_DebugPrint(msg);
#else
      Console.Write(msg);
#endif
    }
    /// <summary>
    /// Prints a debug message to the Rhino Command Line.
    /// The message will only appear if the SendDebugToCommandLine property is set to true.
    /// </summary>
    /// <param name="format">Message to format and print.</param>
    /// <param name="args">An Object array containing zero or more objects to format.</param>
    /// <since>5.0</since>
    public static void DebugString(string format, params object[] args)
    {
      string msg = string.Format(System.Globalization.CultureInfo.InvariantCulture, format, args);
      DebugString(msg);
    }
#if RHINO_SDK
    /// <summary>
    /// Logs a debug event.
    /// The function will log the filename and line number from where this function is called,
    /// in addition to the input message.
    /// </summary>
    /// <param name="message">The event message.</param>
    /// <since>8.5</since>
    public static void LogDebugEvent(string message)
    {
      try
      {
        StackFrame callStack = new StackFrame(1, true);
        String filename = Path.GetFileName(callStack.GetFileName());
        UnsafeNativeMethods.RHC_RhLogDebugEvent(message, filename, callStack.GetFileLineNumber());
      }
      catch
      {
        // Don't crash when we can't log diagnostics
      }
    }
#endif
    /// <summary>
    /// Gets or sets whether debug messages are printed to the command line.
    /// </summary>
    /// <since>5.0</since>
    public static bool SendDebugToCommandLine
    {
      get { return m_bSendDebugToRhino; }
      set { m_bSendDebugToRhino = value; }
    }

    /// <summary>
    /// Informs RhinoCommon of an exception that has been handled but that the developer wants to screen.
    /// </summary>
    /// <param name="ex">An exception.</param>
    /// <since>5.0</since>
    public static void ExceptionReport(Exception ex)
    {
      ExceptionReport(null, ex);
    }

    /// <summary>
    /// Informs RhinoCommon of an exception that has been handled but that the developer wants to screen.
    /// </summary>
    /// <param name="source">An exception source text.</param>
    /// <param name="ex">An exception.</param>
    /// <since>5.0</since>
    public static void ExceptionReport(string source, Exception ex)
    {
      if (null == ex)
        return;

      // Let's try and make sure exception reporting itself doesn't bring down Rhino
      try
      {
        string msg = ex.ToString();

        TypeLoadException tle = ex as TypeLoadException;
        if (tle != null)
        {
          string name = tle.TypeName;
          //if (!string.IsNullOrEmpty(name))
          msg = string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}\nMissing Type = {1}", msg, name);
        }
        if (!string.IsNullOrEmpty(source))
          DebugString(source);
        DebugString(msg);

        if (OnExceptionReport != null)
          OnExceptionReport(source, ex);
      }
      catch(Exception)
      {
        // swallow it up
      }
    }

    /// <summary>
    /// Represents a reference to a method that will be called when an exception occurs.
    /// </summary>
    /// <param name="source">An exception source text.</param>
    /// <param name="ex">An exception.</param>
    public delegate void ExceptionReportDelegate(string source, Exception ex);

    /// <summary>
    /// Is raised when an exception is reported with one of the <see cref="ExceptionReport(Exception)"/> method.
    /// </summary>
    /// <since>5.0</since>
    public static event ExceptionReportDelegate OnExceptionReport;

    /// <summary>
    /// Represents the type of message that is being sent to the OnSendLogMessageToCloud event
    /// </summary>
    ///
    /// <since>6.4</since>
    public enum LogMessageType : int
    {
      /// <summary>
      /// Unknown message type
      /// </summary>
      unknown = 0,
      /// <summary>
      /// Message is informational only
      /// </summary>
      information = 1,
      /// <summary>
      /// Message is a warning
      /// </summary>
      warning = 2,
      /// <summary>
      /// Message is an error
      /// </summary>
      error = 3,
      /// <summary>
      /// Message is a debug ASSERT
      /// </summary>
      assert = 4
    };

#if RHINO_SDK
    /// <summary>
    /// Informs RhinoCommon of an message that has been handled but that the developer wants to screen.
    /// </summary>
    /// <param name="pwStringClass">The top level message type.</param>
    /// <param name="pwStringDesc">Finer grained description of the message.</param>
    /// <param name="pwStringMessage">The message.</param>
    /// <param name="msg_type">The messag type</param>
    /// <since>6.4</since>
    [MonoPInvokeCallback(typeof(SendLogMessageToCloudCallback))]
    public static void SendLogMessageToCloudCallbackProc(LogMessageType msg_type, IntPtr pwStringClass, IntPtr pwStringDesc, IntPtr pwStringMessage)
    {
      if (IntPtr.Zero == pwStringClass)
        return;

      if (IntPtr.Zero == pwStringDesc)
        return;

      if (IntPtr.Zero == pwStringMessage)
        return;

      // Let's try and make sure exception reporting itself doesn't bring down Rhino
      try
      {
        if (OnSendLogMessageToCloud != null)
        {

          string s_class = StringWrapper.GetStringFromPointer(pwStringClass);
          string s_desc = StringWrapper.GetStringFromPointer(pwStringDesc);
          string s_message  = StringWrapper.GetStringFromPointer(pwStringMessage);

          OnSendLogMessageToCloud(msg_type, s_class, s_desc, s_message);
        }
      }
      catch (Exception)
      {
        // swallow it up
      }
    }



    /// <summary>
    /// Represents a reference to a method that will be called when an exception occurs.
    /// </summary>
    /// <param name="sClass">The top level message type</param>
    /// <param name="sDesc">Finer grained description of the message.</param>
    /// <param name="sMessage">The message.</param>
    /// <param name="msg_type">The messag type</param>
    public delegate void SendLogMessageToCloudDelegate(LogMessageType msg_type, string sClass, string sDesc, string sMessage);

    /// <summary>
    /// Is raised when an exception is reported with one of the  method.
    /// </summary>
    /// <since>6.4</since>
    public static event SendLogMessageToCloudDelegate OnSendLogMessageToCloud;
#endif



    /// <summary>
    /// Gets the debug dumps. This is a text description of the geometric contents.
    /// DebugDump() is intended for debugging and is not suitable for creating high
    /// quality text descriptions of an object.
    /// </summary>
    /// <param name="geometry">Some geometry.</param>
    /// <returns>A debug dump text.</returns>
    /// <since>5.0</since>
    public static string DebugDumpToString(Rhino.Geometry.GeometryBase geometry)
    {
      IntPtr pConstThis = geometry.ConstPointer();
      using (var sh = new StringHolder())
      {
        IntPtr pString = sh.NonConstPointer();
        UnsafeNativeMethods.ON_Object_Dump(pConstThis, pString);
        return sh.ToString();
      }
    }

    /// <summary>
    /// Gets the debug dumps. This is a text description of the geometric contents.
    /// DebugDump() is intended for debugging and is not suitable for creating high
    /// quality text descriptions of an object.
    /// </summary>
    /// <param name="bezierCurve">curve to evaluate</param>
    /// <returns>A debug dump text.</returns>
    /// <since>5.0</since>
    public static string DebugDumpToString(Rhino.Geometry.BezierCurve bezierCurve)
    {
      IntPtr pConstThis = bezierCurve.ConstPointer();
      using (var sh = new StringHolder())
      {
        IntPtr pString = sh.NonConstPointer();
        UnsafeNativeMethods.ON_BezierCurve_Dump(pConstThis, pString);
        return sh.ToString();
      }
    }

#if RHINO_SDK
    /// <summary>
    /// Returns a description that is similar to the one in the _What command, except for not mentioning units and other attribute data.
    /// This description is translated in the current Rhino version.
    /// </summary>
    /// <since>8.0</since>
    public static string DescribeGeometry(Rhino.Geometry.GeometryBase geometry)
    {
      if (geometry == null) return null;

      IntPtr ptr = geometry.ConstPointer();
      var log = new TextLog();

      UnsafeNativeMethods.RH_RhinoDescribeGeometry(ptr, 0, log.NonConstPointer());

      return log.ToString();
    }
#endif

#if RHINO_SDK

    /// <summary>
    /// Used to help record times at startup with the -stopwatch flag to help
    /// determine bottlenecks in start up speed
    /// </summary>
    /// <param name="description"></param>
    /// <since>6.0</since>
    public static void RecordInitInstanceTime(string description)
    {
      UnsafeNativeMethods.CRhinoApp_RecordInitInstanceTime(description);
    }

    /// <summary>
    /// Parses a plugin and create all the commands defined therein.
    /// </summary>
    /// <param name="plugin">Plugin to harvest for commands.</param>
    /// <since>5.0</since>
    public static void CreateCommands(PlugIn plugin)
    {
      if (plugin!=null)
        plugin.InternalCreateCommands();
    }

    /// <summary>
    /// Parses a plugin and create all the commands defined therein.
    /// </summary>
    /// <param name="pPlugIn">Plugin to harvest for commands.</param>
    /// <param name="pluginAssembly">Assembly associated with the plugin.</param>
    /// <returns>The number of newly created commands.</returns>
    /// <since>5.0</since>
    public static int CreateCommands(IntPtr pPlugIn, System.Reflection.Assembly pluginAssembly)
    {
      int rc = 0;
      // This function must ONLY be called by Rhino_DotNet.Dll
      if (IntPtr.Zero == pPlugIn || null == pluginAssembly)
        return rc;

      Type[] exported_types = pluginAssembly.GetExportedTypes();
      if (null == exported_types)
        return rc;

      Type command_type = typeof(Commands.Command);
      for (int i = 0; i < exported_types.Length; i++)
      {
        if (exported_types[i].IsAbstract)
          continue;
        if (command_type.IsAssignableFrom(exported_types[i]))
        {
          if( PlugIn.CreateCommandsHelper(null, pPlugIn, exported_types[i], null))
            rc++;
        }
      }

      return rc;
    }

    /// <summary>
    /// Adds a new dynamic command to Rhino.
    /// </summary>
    /// <param name="plugin">Plugin that owns the command.</param>
    /// <param name="cmd">Command to add.</param>
    /// <returns>true on success, false on failure.</returns>
    /// <since>5.0</since>
    public static bool RegisterDynamicCommand(PlugIn plugin, Commands.Command cmd)
    {
      // every command must have a RhinoId and Name attribute
      bool rc = false;
      if (plugin != null)
      {
        try
        {
          plugin.m_commands.Add(cmd);
          cmd.PlugIn = plugin;
          IntPtr ptr_plugin = plugin.NonConstPointer();
          string english_name = cmd.EnglishName;
          string local_name = cmd.LocalName;

          int command_style = 0;
          object[] styleattr = cmd.GetType().GetCustomAttributes(typeof(Commands.CommandStyleAttribute), true);
          if (styleattr != null && styleattr.Length > 0)
          {
            var a = (Commands.CommandStyleAttribute)styleattr[0];
            cmd.m_style_flags = a.Styles;
            command_style = (int)cmd.m_style_flags;
          }
          Guid id = cmd.Id;
          int sn = UnsafeNativeMethods.CRhinoCommand_New(ptr_plugin, id, english_name, local_name, command_style, 0);
          cmd.m_runtime_serial_number = sn;
          rc = sn!=0;
        }
        catch (Exception ex)
        {
          ExceptionReport(ex);
        }
      }
      return rc;
    }

    [MonoPInvokeCallback(typeof(GetNowCallback))]
    static int GetNowHelper(int localeId, IntPtr pStringHolderFormat, IntPtr pResultString)
    {
      int rc;
      try
      {
        string dateformat = StringHolder.GetString(pStringHolderFormat);
        if (string.IsNullOrEmpty(dateformat))
          return 0;
        // surround apostrophe with quotes in order to keep the formatter happy
        dateformat = dateformat.Replace("'", "\"'\"");
        System.Globalization.CultureInfo ci = new System.Globalization.CultureInfo(localeId);
        DateTime now = System.DateTime.Now;
        string s = string.IsNullOrEmpty(dateformat) ? now.ToString(ci) : now.ToString(dateformat, ci);
        UnsafeNativeMethods.ON_wString_Set(pResultString, s);
        rc = 1;
      }
      catch (Exception ex)
      {
        UnsafeNativeMethods.ON_wString_Set(pResultString, ex.Message);
        rc = 0;
      }
      return rc;
    }

    [MonoPInvokeCallback(typeof(GetFormattedTimeCallback))]
    static int GetFormattedTimeHelper(int localeId, int sec, int min, int hour, int day, int month, int year, IntPtr pStringHolderFormat, IntPtr pResultString)
    {
      int rc;
      try
      {
        string dateformat = StringHolder.GetString(pStringHolderFormat);
        System.Globalization.CultureInfo ci = new System.Globalization.CultureInfo(localeId);
        DateTime dt = new DateTime(year, month, day, hour, min, sec);
        dt = dt.ToLocalTime();
        string s = string.IsNullOrEmpty(dateformat) ? dt.ToString(ci) : dt.ToString(dateformat, ci);
        UnsafeNativeMethods.ON_wString_Set(pResultString, s);
        rc = 1;
      }
      catch (Exception ex)
      {
        UnsafeNativeMethods.ON_wString_Set(pResultString, ex.Message);
        rc = 0;
      }
      return rc;
    }

    [MonoPInvokeCallback(typeof(EvaluateExpressionCallback))]
    static int EvaluateExpressionHelper(IntPtr statementsAsStringHolder, IntPtr expressionAsStringHolder, uint rhinoDocSerialNumber, IntPtr pResultString)
    {
      int rc = 0;
      try
      {
        // 11 July 2014 S. Baer (RH-28010)
        // Force the culture to invarient while running the evaluation
        var current = System.Threading.Thread.CurrentThread.CurrentCulture;
        System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
        string state = StringHolder.GetString(statementsAsStringHolder);
        string expr = StringHolder.GetString(expressionAsStringHolder);
        PythonScript py = PythonScript.Create();
        if (py == null)
          return 0;

        object eval_result = py.EvaluateExpression(state, expr);
        System.Threading.Thread.CurrentThread.CurrentCulture = current;
        if (null != eval_result)
        {
          string s = null;
          RhinoDoc doc = RhinoDoc.FromRuntimeSerialNumber(rhinoDocSerialNumber);
          if (eval_result is double || eval_result is float)
          {
            if (doc != null)
            {
              int display_precision = doc.DistanceDisplayPrecision;
              string format = "{0:0.";
              format = format.PadRight(display_precision + format.Length, '0') + "}";
              s = string.Format(format, eval_result);
            }
            else
              s = eval_result.ToString();
          }
          else if (eval_result is string)
          {
            s = eval_result.ToString();
          }
          System.Collections.IEnumerable enumerable = eval_result as System.Collections.IEnumerable;
          if (string.IsNullOrEmpty(s) && enumerable != null)
          {
            string format = null;
            if (doc != null)
            {
              int display_precision = doc.DistanceDisplayPrecision;
              format = "{0:0.";
              format = format.PadRight(display_precision + format.Length, '0') + "}";
            }
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            foreach (object obj in enumerable)
            {
              if (sb.Length > 0)
                sb.Append(", ");
              if ( (obj is double || obj is float) && !string.IsNullOrEmpty(format) )
              {
                sb.AppendFormat(format, obj);
              }
              else
              {
                sb.Append(obj);
              }
            }
            s = sb.ToString();
          }
          if (string.IsNullOrEmpty(s))
            s = eval_result.ToString();
          UnsafeNativeMethods.ON_wString_Set(pResultString, s);
        }
        rc = 1;
      }
      catch (Exception ex)
      {
        UnsafeNativeMethods.ON_wString_Set(pResultString, ex.Message);
        rc = 0;
      }
      return rc;
    }

    internal static object ParseFieldExpression(string expression, RhinoDoc doc,
      Rhino.DocObjects.RhinoObject rhinoObject, Rhino.DocObjects.RhinoObject topLevelRhinoObject,
      Rhino.DocObjects.InstanceObject immediateParentObject,
      bool returnFormattedString, out bool parseSucceeded)
    {
      parseSucceeded = false;
      int startIndex = expression.IndexOf("%<");
      int endIndex = expression.IndexOf(">%");
      string formula = expression;
      if (startIndex >=0 && endIndex > startIndex)
        formula = expression.Substring(startIndex + 2, endIndex - startIndex - 2).Trim();

      formula = formula.Replace("\n", "");




      //v8 and newer requires proper casing of functions. Please don't add new functions to this list
      //Repair the casing of older files to ensure compatibility.
      #region Repair Function Casing
      var function_name_list = new List<string>()
      {
        "Area",
        "AttributeUserText",
        "BlockAttributeText",
        "BlockInstanceCount",
        "BlockInstanceName",
        "CurveLength",
        "Date",
        "DateModified",
        "DetailScale",
        "DocumentText",
        "FileName",
        "LayerName",
        "LayoutUserText",
        "ModelUnits",
        "Notes",
        "NumPages",
        "ObjectLayer",
        "ObjectName",
        "ObjectPage",
        "PageHeight",
        "PageName",
        "PageNumber",
        "PageWidth",
        "PaperName",
        "PointCoordinate",
        "UserText",
        "Volume"
      };
      foreach (var fx_name in function_name_list)
      {
        var str_index_of_function = formula.IndexOf(fx_name, 0, StringComparison.OrdinalIgnoreCase);
        if (str_index_of_function == -1)
          continue;

        var og_fx = formula.Substring(str_index_of_function, fx_name.Length);
        formula = formula.Replace(og_fx, fx_name);
      }
      #endregion




      // tune up old V5 style fields to force every function to have a ()
      // Skipping ones like "Area" since those always required parentheses
      #region Add () to function names
      string[] oldFields = { "Date", "DateModified", "FileName", "ModelUnits", "NumPages", "PageNumber", "PageName", "Notes", "ObjectName" };

      foreach(var field in oldFields)
      {
        if (!formula.StartsWith(field))
          continue;

        int tokenSearchStart = 0;
        while(true)
        {

          int functionNameStart = formula.IndexOf(field, tokenSearchStart);
          if (functionNameStart < 0)
            break;
          int functionNameEnd = functionNameStart + field.Length - 1;

          bool addParentheses = true;
          for(int i=functionNameEnd+1; i<formula.Length; i++)
          {
            char c = formula[i];
            if (c == ' ')
              continue;
            if (c == '(')
              addParentheses = false;
            if (i == (functionNameEnd+1))
            {
              // if this is another char, then ignore
              if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z'))
                addParentheses = false;
            }
            break;
          }

          tokenSearchStart = functionNameStart + field.Length;
          if(addParentheses)
          {
            tokenSearchStart += 2;
            formula = formula.Insert(functionNameStart + field.Length, "()");
          }
        }
      }
      #endregion



      // I don't think is is possible to write bad things in a single line expression,
      // but check for import, exec, and clr just to be safe.
      string[] badwords = { "import", "exec", "clr" };
      bool containsBadWord = false;
      foreach (var word in badwords)
        containsBadWord |= formula.Contains(word);

      if (containsBadWord)
        return null;


      try
      {
        if (doc == null)
          doc = RhinoDoc.ActiveDoc;

        object eval_result = null;
        {
          try
          {
            TextFields.PushContext(doc, rhinoObject, topLevelRhinoObject, immediateParentObject);
            PythonScript py = PythonScript.Create();
            if (py == null)
              return "####";

            string statements = "import clr\nfrom math import *\nfrom Rhino.Runtime.TextFields import *\n";
            eval_result = py.EvaluateExpression(statements, formula);
          }
          finally
          {
            TextFields.PopContext();
          }
        }

        if (null != eval_result)
        {
          parseSucceeded = true;
          if (returnFormattedString)
          {
            if (eval_result is double || eval_result is float)
            {
              double double_result = (double)eval_result;
              // We should eventually support some sort of formatting field in the field.
              UnitSystem units;
              int displayPrecision;
              if (rhinoObject != null && (rhinoObject.Attributes.Space == DocObjects.ActiveSpace.PageSpace))
              {
                units = doc.PageUnitSystem;
                displayPrecision = doc.PageDistanceDisplayPrecision;
              }
              else
              {
                units = doc.ModelUnitSystem;
                displayPrecision = doc.ModelDistanceDisplayPrecision;
              }

              var annotation = rhinoObject as AnnotationObjectBase;

              //Look for block instances with BlockAttributesText and try to fish out an annotation base to get dimension style from
              //NOTE this only supports 1 dimension style per block.
              if (rhinoObject is InstanceObject block)
              {
                annotation = block.InstanceDefinition.GetObjects().FirstOrDefault(c=>c.ObjectType == ObjectType.Annotation) as AnnotationObjectBase;
              }

              if (annotation != null)
              {
                //If the object containing the function is an annotation then
                //We need to know the space of the object being referenced by the function, not the annotations space.
                //strip the referenced object guid out of the expression and figure out what space that object resides in.
                //Fixes https://mcneel.myjetbrains.com/youtrack/issue/RH-80815
                var guid_pattern = @"[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}";
                var regex = new Regex(guid_pattern);
                var match = regex.Match(expression);
                if (match.Success)
                {
                  Guid.TryParse(match.Value, out var object_guid);
                  if (object_guid != null)
                  {
                    var ro = doc.Objects.Find(object_guid);
                    if (ro != null)
                      units = ro.Attributes.Space == ActiveSpace.PageSpace ? doc.PageUnitSystem : doc.ModelUnitSystem;
                  }
                }
              }


              // Basic CurveLength expression
              if (annotation != null && formula.StartsWith("CurveLength", StringComparison.Ordinal) && formula.IndexOf(')') == (formula.Length - 1))
              {
                var stringResult = Rhino.UI.Localization.FormatDistanceAndTolerance(double_result, units, annotation.AnnotationGeometry.DimensionStyle, false);
                if (!string.IsNullOrWhiteSpace(stringResult))
                  return stringResult;
              }

              //format area if it doesn't contain a comma which means there's a unit system already being specified in the formula
              if (annotation != null && formula.StartsWith("Area", StringComparison.Ordinal) && formula.IndexOf(')') == (formula.Length - 1) && !formula.Contains("\","))
              {
                string stringResult = Rhino.UI.Localization.FormatArea(double_result, units, annotation.AnnotationGeometry.DimensionStyle, false);

                if (!string.IsNullOrWhiteSpace(stringResult))
                  return stringResult;
              }

              if (annotation != null && formula.StartsWith("Volume", StringComparison.Ordinal) && formula.IndexOf(')') == (formula.Length - 1))
              {
                string stringResult = Rhino.UI.Localization.FormatVolume(double_result, units, annotation.AnnotationGeometry.DimensionStyle, false);

                if (!string.IsNullOrWhiteSpace(stringResult))
                  return stringResult;
              }

              if (annotation!=null)
              {
                displayPrecision = annotation.AnnotationGeometry.DimensionStyle.LengthResolution;
              }

              string formattedString = Rhino.UI.Localization.FormatNumber(double_result, units, UI.DistanceDisplayMode.Decimal, displayPrecision, false);
              if (!string.IsNullOrWhiteSpace(formattedString))
                return formattedString;
            }

            eval_result = eval_result.ToString();
          }
        }
        return eval_result;
      }
      catch // (Exception ex)
      {
        // 11 September 2019 John Morse
        // Return "####" on error instead of English exception string.  Need to
        // set parseSucceeded to true in this case otherise the expression will
        // be displayed instead of "####"
        //return ex.Message;
        parseSucceeded = true;
        return "####";
      }
    }

    /// <summary>
    /// This function is called from the C++ textfield evaluator
    /// </summary>
    /// <param name="ptrFormula"></param>
    /// <param name="pRhinoObject"></param>
    /// <param name="ptrParseResult"></param>
    /// <param name="pTopParent"></param>
    /// <param name="pImmediateParent">pointer to immediate instance object parent</param>
    /// <returns></returns>
    [MonoPInvokeCallback(typeof(EvaluateTextFieldCallback))]
    static int EvaluateTextFieldHelper(IntPtr ptrFormula, IntPtr pRhinoObject, IntPtr ptrParseResult, IntPtr pTopParent, IntPtr pImmediateParent)
    {
      int rc = 0;

      RhinoDoc doc = null;
      var rhobj = Rhino.DocObjects.RhinoObject.CreateRhinoObjectHelper(pRhinoObject);
      if (rhobj != null)
        doc = rhobj.Document;
      Rhino.DocObjects.RhinoObject topParent = null;
      if (pRhinoObject == pTopParent || pTopParent == IntPtr.Zero)
        topParent = rhobj;
      else
        topParent = Rhino.DocObjects.RhinoObject.CreateRhinoObjectHelper(pTopParent);

      Rhino.DocObjects.InstanceObject immediateParent = Rhino.DocObjects.RhinoObject.CreateRhinoObjectHelper(pImmediateParent) as Rhino.DocObjects.InstanceObject;

      if (doc == null)
        doc = RhinoDoc.ActiveDoc;


      // Call a function on the TextFields class
      bool success;
      string formula = StringWrapper.GetStringFromPointer(ptrFormula);

      string result = ParseFieldExpression(formula, doc, rhobj, topParent, immediateParent, true, out success) as string;

      // Iterate parsed results to see if they contain nested functions

      int max_iterations = 9;
      if (!string.IsNullOrWhiteSpace(result))
      {
        if (result.StartsWith("%<") && result.EndsWith(">%"))
        {
          for (int i = 0; i < max_iterations; i++)
          {
            result = ParseFieldExpression(result, doc, rhobj, topParent, immediateParent, true, out success) as string;
            if (string.IsNullOrEmpty(result))
              break;

            if (!result.StartsWith("%<") || !result.EndsWith(">%"))
              break;
          }
        }
      }



      StringWrapper.SetStringOnPointer(ptrParseResult, result);
      rc = success ? 1 : 0;
      return rc;
    }

    /// <summary>
    /// Gets the auto install plug-in folder for machine or current user.
    /// </summary>
    /// <param name="currentUser">true if the query relates to the current user.</param>
    /// <returns>The full path to the revelant auto install plug-in directory.</returns>
    /// <since>6.0</since>
    public static string AutoInstallPlugInFolder(bool currentUser)
    {
      // TODO: refactor to something more generic like GetPackageFolder
      // %programdata%\mcneel\rhinoceros\packages\6.0
      string data_dir = Rhino.ApplicationSettings.FileSettings.GetDataFolder(currentUser);
      var dir = new System.IO.DirectoryInfo(data_dir);
      // use MAJOR.0 for package folder regardless of whether this is an official build or not
      string name = $"{RhinoBuildConstants.MAJOR_VERSION_STRING}.0";

      // use e.g. "7.0-WIP-Developer-Debug-trunk" if Rhino.Options.PackageManager.UseDebugFolder
      if (RhinoBuildConstants.VERSION_STRING.EndsWith("0")) // developer build only
      {
        try
        {
          var settings = PersistentSettings.RhinoAppSettings
                          .GetChild("Options").GetChild("PackageManager");
          if (settings.GetBool("UseDebugFolder", false))
          {
            name = dir.Name;
          }
        }
        catch (KeyNotFoundException)
        {
          // thrown the first time this is called (by the assembly resolver) on mac
          // only happens if UseDebugFolder is *not* set, so let's ignore it
        }
      }

      string path = System.IO.Path.Combine(dir.Parent.FullName, "packages", name);
      return path;
    }

    [MonoPInvokeCallback(typeof(Action))]
    static void BuildRegisteredPlugInList()
    {
      try
      {
        foreach (var active_version_directory in GetActivePlugInVersionFolders())
        {
          var rhps = active_version_directory.GetFiles("*.rhp", System.IO.SearchOption.TopDirectoryOnly);
          foreach (var rhp in rhps)
          {
            UnsafeNativeMethods.CRhinoPlugInManager_InstallPlugIn(rhp.FullName, true);
          }
        }
      }
      catch(Exception ex)
      {
        ExceptionReport(ex);
      }
    }

    /// <summary>
    /// Recurses through the auto install plug-in folders and returns the directories containing "active" versions of plug-ins.
    /// </summary>
    /// <returns></returns>
    /// <remarks>If the same package is installed in both the user and machine locations, the newest directory wins.</remarks>
    /// <since>7.17</since>
    public static IEnumerable<System.IO.DirectoryInfo> GetActivePlugInVersionFolders()
    {
      // GetEnvironmentVariable can generate an error so let's check for previous errors here.
      UnsafeNativeMethods.RHC_GetLastWindowsError();

      var developmentDirs = System.Environment.GetEnvironmentVariable("RHINO_PACKAGE_DIRS")?.Split(';');

      // Discard error produced by GetEnvironmentVariable.
      // Error: "The system could not find the environment option that was entered."
      // The error doesn't seem to be causing any problems so let's discard it.
      UnsafeNativeMethods.RHC_MaskLastWindowsError(203); // ERROR_ENVVAR_NOT_FOUND
      UnsafeNativeMethods.RHC_GetLastWindowsError();

      if (developmentDirs?.Length > 0)
      {
        foreach (var developmentDir in developmentDirs)
        {
          if (Directory.Exists(developmentDir))
          {
            yield return GetRuntimeSpecificFolder(new DirectoryInfo(developmentDir));
          }
        }
      }

      var userDirs = GetActivePlugInVersionFolders(true).ToList();
      var machineDirs = GetActivePlugInVersionFolders(false).ToList();

      var bCount = machineDirs.Count;

      foreach (var userDir in userDirs)
      {
        var pick = true;
        for (int j = 0; j < bCount; j++)
        {
          var machineDir = machineDirs[j];
          // if package names (parent dir) don't match, skip to the next one
          if (!userDir.Parent.Name.Equals(machineDir.Parent.Name, StringComparison.OrdinalIgnoreCase))
            continue;
          // if package exists in both locations, pick the version dir with the most recent creation time
          if (userDir.CreationTime < machineDir.CreationTime)
            pick = false;
          else
          {
            machineDirs.RemoveAt(j);
            bCount--;
          }
          break;
        }
        if (pick)
          yield return GetRuntimeSpecificFolder(userDir);
      }

      foreach (var machineDir in machineDirs)
        yield return GetRuntimeSpecificFolder(machineDir);
    }

    internal static DirectoryInfo GetRuntimeSpecificFolder(DirectoryInfo root, bool useRootFiles = true)
    {
      // if there are .rhp or .gha's in the top folder, use default behaviour
      if (useRootFiles &&
        (root.GetFiles("*.rhp").Length > 0
        || root.GetFiles("*.gha").Length > 0)
        )
        return root;

      // no plugins to load in this package, scan for framework-specific subfolders if they exist

      if (RunningInNetCore)
      {
        // search from current .NET runtime down to 7 (for future proofing when we support .NET 8, 9, etc.)
        const int minimumNetVersion = 7;
        var currentRuntimeVersion = System.Environment.Version.Major;
        for (int i = currentRuntimeVersion; i >= minimumNetVersion; i--)
        {
          var suffix = RunningOnWindows ? "-windows" :
              RunningOnOSX ? "-macos" :
              null; // -linux

          Version GetOSVersion(string name)
          {
            var idx = name.IndexOf(suffix);
            if (idx < 0)
              return null;

            var versionString = name.Substring(idx + suffix.Length);

            return Version.TryParse(versionString, out var version) ? version : null;
          }

          // search for platform-specific directories first, ordered by os version if specified
          var platformDirs = root
            .GetDirectories($"net{i}.0{suffix}*")
            .OrderByDescending(d => GetOSVersion(d.Name) ?? new Version());

          var currentOSVersion = System.Environment.OSVersion.Version;

          foreach (var platformDir in platformDirs)
          {
            var osVersion = GetOSVersion(platformDir.Name);

            // no os version, just use it
            if (osVersion == null)
              return platformDir;

            // and ensure the OS version, if specified, is greater or equal to package version
            if (currentOSVersion >= osVersion)
              return platformDir;
          }

          // search for platform-agnostic target
          var targetDir = root.GetDirectories($"net{i}.0")?.FirstOrDefault();
          if (targetDir != null)
            return targetDir;
        }
      }

      // fall back to the latest net4x folder
      foreach (var net4xdir in root.EnumerateDirectories("net4*").OrderByDescending(r => r.Name))
      {
        var match = Regex.Match(net4xdir.Name, @"net(?<ver>4\d+)");
        if (!match.Success)
          continue;

        // just return it.
        return net4xdir;

      }

      // no match, return root folder
      return root;
    }

    /// <summary>
    /// list of package names that should never be passed to rhino
    /// i.e. removed from package server to be shipped with rhino
    /// </summary>
    private static string[] _package_folder_blocklist = new string[] { "sectiontools" };

    /// <summary>
    /// Recurses through the auto install plug-in folders and returns the directories containing "active" versions of plug-ins.
    /// </summary>
    /// <param name="currentUser">Current user (true) or machine (false).</param>
    /// <returns></returns>
    /// <since>6.0</since>
    public static IEnumerable<System.IO.DirectoryInfo> GetActivePlugInVersionFolders(bool currentUser)
    {
      // TODO: this gets called a lot so we should probably cache the results
      string dir = AutoInstallPlugInFolder(currentUser);
      var install_directory = new System.IO.DirectoryInfo(dir);
      if (!install_directory.Exists)
        yield break;
      var child_directories = install_directory.GetDirectories();
      foreach (var child_directory in child_directories)
      {
        // skip blocked packages
        if (_package_folder_blocklist.Any(x => string.Equals(x, child_directory.Name, StringComparison.OrdinalIgnoreCase)))
          continue;
        // find and read manifest file
        string manifest_path = System.IO.Path.Combine(child_directory.FullName, "manifest.txt");
        if (!System.IO.File.Exists(manifest_path))
          continue;
        string[] lines = System.IO.File.ReadAllLines(manifest_path);
        if (null == lines || lines.Length < 1)
          continue;
        var active_version_directory = new System.IO.DirectoryInfo(System.IO.Path.Combine(child_directory.FullName, lines[0]));
        if (!active_version_directory.Exists)
          continue;
        yield return active_version_directory;
      }
    }

    [MonoPInvokeCallback(typeof(GetAssemblyIdCallback))]
    static Guid GetAssemblyId(IntPtr path)
    {
      try
      {
        string str_path = StringWrapper.GetStringFromPointer (path);
        if (RunningOnOSX)
        {
          if(System.IO.Directory.Exists(str_path))
          {
            var files = System.IO.Directory.GetFiles(str_path, "*.rhp");
            if (files != null && files.Length > 0)
              str_path = files[0];
          }
        }
        return GetGuidAttributeValue(str_path);
      }
      catch (Exception)
      {
        return Guid.Empty;
      }
    }

    private static Guid GetGuidAttributeValue(string str_path)
    {
#if NETFRAMEWORK
      var reflect_assembly = System.Reflection.Assembly.ReflectionOnlyLoadFrom(str_path);
      object[] idAttr = reflect_assembly.GetCustomAttributes(typeof(GuidAttribute), false);
      GuidAttribute idAttribute = (GuidAttribute)(idAttr[0]);
      return new Guid(idAttribute.Value);
#else
      using (var fileStream = File.OpenRead(str_path))
      {
        using (var portableExecutableReader = new System.Reflection.PortableExecutable.PEReader(fileStream))
        {
          var reader = portableExecutableReader.GetMetadataReader();
          var assembly = reader.GetAssemblyDefinition();
          return assembly.GetGuid(reader) ?? Guid.Empty;
        }
      }
#endif
    }

    [MonoPInvokeCallback(typeof(LoadPluginCallback))]
    static int LoadPlugInHelper(IntPtr path, IntPtr pluginInfo, IntPtr errorMessage, int displayDebugInfo, int bIsDirectoryInstall)
    {
      string str_path = StringWrapper.GetStringFromPointer (path);
      int rc = (int)PlugIn.LoadPlugInHelper(str_path, pluginInfo, errorMessage, displayDebugInfo != 0, bIsDirectoryInstall != 0);
      return rc;
    }

    static Skin g_skin;
    [MonoPInvokeCallback(typeof(LoadSkinCallback))]
    static int LoadSkinHelper(IntPtr path, int displayDebugInfo)
    {
      if( g_skin!=null )
        return 0;

      string str_path = StringWrapper.GetStringFromPointer (path);
      if( string.IsNullOrWhiteSpace(str_path) )
        return 0;

      // attempt to load the assembly
      try
      {
        var assembly = LoadAssemblyFrom( str_path );
        // look for a class derived from Rhino.Runtime.Skin
        var internal_types = assembly.GetExportedTypes();
        var skin_type = typeof(Skin);
        Skin skin = null;
        foreach (Type t in internal_types)
        {
          // Skip abstract classes during reflection creation
          if( t.IsAbstract )
            continue;
          if( skin_type.IsAssignableFrom(t) )
          {
            skin = Activator.CreateInstance(t) as Skin;
            if( skin!=null )
              break;
          }
        }
        g_skin = skin;
      }
      catch (Exception e)
      {
        if( displayDebugInfo!=0 )
        {
          RhinoApp.Write("(ERROR) Exception occurred in LoadSkin::ReflectionOnlyLoadFrom\n" );
          RhinoApp.WriteLine( e.Message );
        }
      }
      return (g_skin != null) ? 1:0;
    }

    internal delegate int EvaluateExpressionCallback(IntPtr statementsAsStringHolder, IntPtr expressionAsStringHolder, uint rhinoDocSerialNumber, IntPtr resultString);
    static readonly EvaluateExpressionCallback m_evaluate_callback = EvaluateExpressionHelper;
    internal delegate int GetNowCallback(int localeId, IntPtr formatAsStringHolder, IntPtr resultString);
    static readonly GetNowCallback m_getnow_callback = GetNowHelper;
    internal delegate int GetFormattedTimeCallback(int locale, int sec, int min, int hour, int day, int month, int year, IntPtr formatAsStringHolder, IntPtr resultString);
    static readonly GetFormattedTimeCallback m_getformattedtime_callback = GetFormattedTimeHelper;

    internal delegate void InitializeRDKCallback();
    static readonly InitializeRDKCallback m_rdk_initialize_callback = InitializeRhinoCommon_RDK;
    internal delegate void ShutdownRDKCallback();
    static readonly ShutdownRDKCallback m_rdk_shutdown_callback = ShutDownRhinoCommon_RDK;

    internal delegate int LoadPluginCallback(IntPtr path, IntPtr pluginInfo, IntPtr errorMessage, int displayDebugInfo, int bIsDirectoryInstall);
    static readonly LoadPluginCallback m_loadplugin_callback = LoadPlugInHelper;
    internal delegate int LoadSkinCallback(IntPtr path, int displayDebugInfo);
    static readonly LoadSkinCallback m_loadskin_callback = LoadSkinHelper;
    static readonly Action m_buildplugin_list = BuildRegisteredPlugInList;
    internal delegate Guid GetAssemblyIdCallback(IntPtr path);
    static readonly GetAssemblyIdCallback m_getassembly_id = GetAssemblyId;

    internal delegate void SendLogMessageToCloudCallback(LogMessageType msg_type, IntPtr pwStringClass, IntPtr pwStringDesc, IntPtr pwStringError);
    static readonly SendLogMessageToCloudCallback m_send_log_message_to_cloud_callback = SendLogMessageToCloudCallbackProc;

    internal delegate int EvaluateTextFieldCallback(IntPtr formula, IntPtr pRhinoObject, IntPtr parseResult, IntPtr topParent, IntPtr immediateParent);
    static readonly EvaluateTextFieldCallback m_eval_textfield_callback = EvaluateTextFieldHelper;
#endif

    private static bool m_rhinocommoninitialized;
    private static int m_uiThreadId;
    /// <summary>
    /// Makes sure all static RhinoCommon components is set up correctly.
    /// This happens automatically when a plug-in is loaded, so you probably won't
    /// have to call this method.
    /// </summary>
    /// <remarks>Subsequent calls to this method will be ignored.</remarks>
    /// <since>5.0</since>
    public static void InitializeRhinoCommon()
    {
      if (m_rhinocommoninitialized)
        return;
      m_rhinocommoninitialized = true;

      m_uiThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId;
#if RHINO_SDK
      // Initialize exception handling
      AppDomain.CurrentDomain.UnhandledException += UnhandledDomainException;

      AssemblyResolver.InitializeAssemblyResolving();
      {
        Type t = typeof(Rhino.DocObjects.Custom.UserDictionary);
        Guid class_id = DocObjects.Custom.ClassIdAttribute.GetGuid(t);
        UnsafeNativeMethods.ON_UserData_RegisterCustomUserData(t.FullName, t.GUID, class_id);
        Rhino.DocObjects.Custom.UserData.RegisterType(t);

        t = typeof(Rhino.DocObjects.Custom.SharedUserDictionary);
        class_id = DocObjects.Custom.ClassIdAttribute.GetGuid(t);
        UnsafeNativeMethods.ON_UserData_RegisterCustomUserData(t.FullName, t.GUID, class_id);
        Rhino.DocObjects.Custom.UserData.RegisterType(t);
      }

      RegisterNamedCallback("ThrowNotLicensedException", NotLicensedException.ThrowNotLicensedException);
      RegisterNamedCallback("FireLicenseStateChangedEvent", RhinoApp.FireLicenseStateChangedEvent);
      RegisterNamedCallback("RhinoView.ShowToast", Rhino.Display.RhinoView.FireShowToast);

      //DebugString("Initializing RhinoCommon");
      UnsafeNativeMethods.RHC_SetGetNowProc(m_getnow_callback, m_getformattedtime_callback);
      UnsafeNativeMethods.RHC_SetPythonEvaluateCallback(m_evaluate_callback);
      UnsafeNativeMethods.RHC_SetTextFieldEvalCallback(m_eval_textfield_callback);
      UnsafeNativeMethods.CRhinoCommonPlugInLoader_SetCallbacks(m_loadplugin_callback, m_loadskin_callback, m_buildplugin_list, m_getassembly_id);
      // 3 March 2023 John Morse
      // Initialize the settings system hooks early in the process to allow the
      // unmanaged settings system to work.
      PersistentSettingsHooks.SetHooks();

      InitializeZooClient();

      UnsafeNativeMethods.RHC_SetRdkInitializationCallbacks(m_rdk_initialize_callback, m_rdk_shutdown_callback);

      // 21 Feb 2020 S. Baer (RH-57124)
      // Turn off raygun non-fatal messages for now. We can turn them back on
      // in the future by uncommmenting the next line
      //UnsafeNativeMethods.RHC_SetSendLogMessageToCloudProc(m_send_log_message_to_cloud_callback);

      FileIO.FilePdf.SetHooks();
      UI.Localization.SetHooks();
      RhinoFileEventWatcherHooks.SetHooks();
#endif
    }

#if RHINO_SDK
    private static bool m_rhinocommonrdkinitialized;
    /// <summary>
    /// Makes sure all static RhinoCommon RDK components are set up correctly.
    /// This happens automatically when the RDK is loaded, so you probably won't
    /// have to call this method.
    /// </summary>
    /// <remarks>Subsequent calls to this method will be ignored.</remarks>
    /// <since>6.0</since>
    [MonoPInvokeCallback(typeof(InitializeRDKCallback))]
    public static void InitializeRhinoCommon_RDK()
    {
      if (m_rhinocommonrdkinitialized)
        return;
      m_rhinocommonrdkinitialized = true;

      Rhino.UI.Controls.CollapsibleSectionImpl.SetCppHooks(true);
      Rhino.UI.Controls.CollapsibleSectionHolderImpl.SetCppHooks(true);
      Rhino.UI.Controls.InternalRdkViewModel.SetCppHooks(true);
      Rhino.Render.UICommands.UICommand.SetCppHooks(true);
      Rhino.Render.PostEffects.PostEffect.SetCppHooks(true);
      Rhino.Render.PostEffects.PostEffectFactoryBase.SetCppHooks(true);
      Rhino.Render.PostEffects.PostEffectJob.SetCppHooks(true);
      Rhino.Render.PostEffects.PostEffectExecutionControl.SetCppHooks(true);
      Rhino.DocObjects.SnapShots.SnapShotsClient.SetCppHooks(true);
      Rhino.UI.Controls.FactoryBase.Register();

      UnsafeNativeMethods.SetRhCsInternetFunctionalityCallback(Rhino.Render.InternalUtilities.OnDownloadFileProc, Rhino.Render.InternalUtilities.OnUrlResponseProc,
        Rhino.Render.InternalUtilities.OnBitmapFromSvgProc);

#if !ON_RUNTIME_APPLE_IOS
      Rhino.ObjectManager.ObjectManagerExtension.SetCppHooks(true);
      Rhino.ObjectManager.ObjectManagerNode.SetCppHooks(true);
#endif
    }

    /// <summary>
    /// Makes sure all static RhinoCommon RDK components are de-initialized so they aren't calling into space when the RDK is unloaded.
    /// </summary>
    /// <remarks>Subsequent calls to this method will be ignored.</remarks>
    /// <since>6.0</since>
    [MonoPInvokeCallback(typeof(ShutdownRDKCallback))]
    public static void ShutDownRhinoCommon_RDK()
    {
      UnsafeNativeMethods.SetRhCsInternetFunctionalityCallback(null, null, null);

      Rhino.UI.Controls.CollapsibleSectionImpl.SetCppHooks(false);
      Rhino.UI.Controls.CollapsibleSectionHolderImpl.SetCppHooks(false);
      Rhino.UI.Controls.InternalRdkViewModel.SetCppHooks(false);
      Rhino.Render.UICommands.UICommand.SetCppHooks(false);
      Rhino.Render.PostEffects.PostEffect.SetCppHooks(false);
      Rhino.Render.PostEffects.PostEffectFactoryBase.SetCppHooks(false);
      Rhino.Render.PostEffects.PostEffectJob.SetCppHooks(false);
      Rhino.Render.PostEffects.PostEffectExecutionControl.SetCppHooks(false);
      Rhino.DocObjects.SnapShots.SnapShotsClient.SetCppHooks(false);

      Rhino.ObjectManager.ObjectManagerExtension.SetCppHooks(false);
      Rhino.ObjectManager.ObjectManagerNode.SetCppHooks(false);
    }
#endif

    static bool g_ReportExceptions = true;

    [DllImport("RhinoCore")]
    private static extern void RHC_ShowCrashReporter();

    /// <summary>
    /// For internal use only!!!
    /// Unhanded exception handler, writes stack trace to RhinoDotNet.txt file
    /// </summary>
    /// <param name="title">
    /// Exception title to write to text file
    /// </param>
    /// <param name="sender"></param>
    /// <param name="ex"></param>
    /// <since>6.0</since>
    public static void RhinoCommonExceptionHandler(string title, object sender, Exception ex)
    {
      if (!g_ReportExceptions)
        return;

      // on macOS, we include the exception info in the standard crash reporter so we don't need a file on the desktop.
      if (!RunningOnOSX)
      {
        WriteException(title, sender, ex);

        // Curtis RH-81806:
        // Commenting out for now as it causes crashes on some machines
        //if (RunningInNetCore && m_uiThreadId != System.Threading.Thread.CurrentThread.ManagedThreadId)
        //{
        //  // With .NET Core, the native crash handler doesn't get called when we get an exception in a non-UI thread
        //  // so call this manually.
        //  RHC_ShowCrashReporter();
        //}
      }

      if (ex == null)
        return;
      var msg = ex.ToString (); // ToString() includes stack trace.
#if RHINO_SDK
#if DEBUG
      // only show dialog for the UI thread. Background threads dump to the console.
      if (m_uiThreadId == System.Threading.Thread.CurrentThread.ManagedThreadId)
        Rhino.UI.Dialogs.ShowMessage(msg, "Unhandled CurrentDomain Exception in .NET");
      else
        DebugString (msg);
#endif
#else
      Console.Error.Write(msg);
#endif
    }

    static void UnhandledDomainException(object sender, UnhandledExceptionEventArgs e)
    {
      RhinoCommonExceptionHandler("System::AppDomain::CurrentDomain->UnhandledException event occurred", sender, e.ExceptionObject as Exception);
    }

    /// <summary>
    /// Exception handler for exceptions occurring on the UI thread
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    /// <since>6.0</since>
    public static void UnhandledThreadException(object sender, System.Threading.ThreadExceptionEventArgs e)
    {
      RhinoCommonExceptionHandler("System::Windows::Forms::Application::ThreadException event occurred", sender, e.Exception);
    }

    private static bool g_write_log = true;
    private static void WriteException(string title, object sender, Exception ex )
    {
      if (!g_write_log)
        return;
      g_write_log = false;
      // create a text file on the desktop to log exception information to

      // Curtis 2018.09.04:
      // Any changes to the output of this file should be reflected on Mac in RhinoDotNet_Mono.mm

      var path = System.Environment.GetFolderPath(System.Environment.SpecialFolder.DesktopDirectory);
      path = System.IO.Path.Combine(path, "RhinoDotNetCrash.txt");
      System.IO.StreamWriter text_file = null;
      try
      {
        text_file = new System.IO.StreamWriter(path);

        if (ex != null)
        {
          text_file.WriteLine($"[ERROR] FATAL UNHANDLED EXCEPTION: {ex}");
        }
        else
        {
          text_file.WriteLine("[ERROR] .NET STACK TRACE:");
          text_file.WriteLine(System.Environment.StackTrace);
        }

        text_file.WriteLine("[END ERROR]");
      }
      // ReSharper disable once EmptyGeneralCatchClause
      catch (Exception)
      {
			}
      finally
      {
        text_file?.Close();
      }
    }

    /// <summary>
    /// Initializes the ZooClient and Rhino license manager, this should get
    /// called automatically when RhinoCommon is loaded so you probably won't
    /// have to call this method.
    /// </summary>
    /// <since>5.6</since>
    public static void InitializeZooClient()
    {
#if RHINO_SDK
      LicenseManager.SetCallbacks();
#endif
    }

#if RHINO_SDK
    /// <summary>
    /// Don't change this function in ANY way unless you chat with Steve first!
    /// This function is called by Rhino on initial startup and the signature
    /// must be exact
    /// </summary>
    /// <param name="task"></param>
    /// <returns></returns>
    /// <since>6.0</since>
    public static int CallFromCoreRhino(string task)
    {
      if( string.Equals(task, "initialize", StringComparison.InvariantCultureIgnoreCase) )
        InitializeRhinoCommon();
      else if (string.Equals(task, "shutdown", StringComparison.InvariantCultureIgnoreCase))
      {
        SetInShutDown();
      }
      return 1;
    }

    /// <summary>
    /// Instantiates a plug-in type and registers the associated commands and classes.
    /// </summary>
    /// <param name="pluginType">A plug-in type. This type must derive from <see cref="PlugIn"/>.</param>
    /// <param name="printDebugMessages">true if debug messages should be printed.</param>
    /// <returns>A new plug-in instance.</returns>
    /// <since>5.0</since>
    public static PlugIn CreatePlugIn(Type pluginType, bool printDebugMessages)
    {
      return CreatePlugIn(pluginType, pluginType.Assembly, printDebugMessages, false);
    }

    /// <summary>
    /// Instantiates a plug-in type and registers the associated commands and classes.
    /// </summary>
    /// <param name="pluginType">A plug-in type. This type must derive from <see cref="PlugIn"/>.</param>
    /// <param name="pluginAssembly"></param>
    /// <param name="printDebugMessages">true if debug messages should be printed.</param>
    /// <param name="useRhinoDotNet"></param>
    /// <returns>A new plug-in instance.</returns>
    internal static PlugIn CreatePlugIn(Type pluginType, System.Reflection.Assembly pluginAssembly, bool printDebugMessages, bool useRhinoDotNet)
    {
      if (null == pluginType || !typeof(PlugIn).IsAssignableFrom(pluginType))
        return null;

      InitializeRhinoCommon();

      // If we turn on debug messages, we always get debug output
      if (printDebugMessages)
        SendDebugToCommandLine = true;

      // this function should only be called by Rhino_DotNet.dll
      // we could add some safety checks by performing validation on
      // the calling assembly
      //System.Reflection.Assembly.GetCallingAssembly();

      object[] name = pluginAssembly.GetCustomAttributes(typeof(System.Reflection.AssemblyTitleAttribute), false);
      string plugin_name = "";
      if (name.Length > 0)
        plugin_name = ((System.Reflection.AssemblyTitleAttribute)name[0]).Title;
      else
        plugin_name = pluginAssembly.GetName().Name;
      string plugin_version = pluginAssembly.GetName().Version.ToString();

      PlugIn plugin = PlugIn.Create(pluginType, plugin_name, plugin_version, useRhinoDotNet, pluginAssembly);

      if (plugin == null)
        return null;

      PlugIn.m_plugins.Add(plugin);
      return plugin;
    }

    static void DelegateReport(System.Delegate d, string name)
    {
      if (d == null) return;
      IFormatProvider fp = System.Globalization.CultureInfo.InvariantCulture;
      string title = string.Format(fp, "{0} Event\n", name);
      UnsafeNativeMethods.CRhinoEventWatcher_LogState(title);
      Delegate[] list = d.GetInvocationList();
      if (list != null && list.Length > 0)
      {
        for (int i = 0; i < list.Length; i++)
        {
          Delegate subD = list[i];
          Type t = subD.Target.GetType();
          string msg = string.Format(fp, "- Plug-In = {0}\n", t.Assembly.GetName().Name);
          UnsafeNativeMethods.CRhinoEventWatcher_LogState(msg);
        }
      }
    }

    internal delegate void ReportCallback(int c);
    internal static ReportCallback m_ew_report = EventWatcherReport;

    [MonoPInvokeCallback(typeof(ReportCallback))]
    internal static void EventWatcherReport(int c)
    {
      UnsafeNativeMethods.CRhinoEventWatcher_LogState("RhinoCommon delegate based event watcher\n");
      DelegateReport(RhinoApp.m_init_app, "InitApp");
      DelegateReport(RhinoApp.m_close_app, "CloseApp");
      DelegateReport(RhinoApp.m_appsettings_changed, "AppSettingsChanged");
      DelegateReport(Rhino.Commands.Command.m_begin_command, "BeginCommand");
      DelegateReport(Rhino.Commands.Command.m_end_command, "EndCommand");
      DelegateReport(Rhino.Commands.Command.m_undo_event, "Undo");
      DelegateReport(RhinoDoc.m_close_document, "CloseDocument");
      DelegateReport(RhinoDoc.m_new_document, "NewDocument");
      DelegateReport(RhinoDoc.m_document_properties_changed, "DocuemtnPropertiesChanged");
      DelegateReport(RhinoDoc.m_begin_open_document, "BeginOpenDocument");
      DelegateReport(RhinoDoc.m_end_open_document, "EndOpenDocument");
      DelegateReport(RhinoDoc.m_begin_save_document, "BeginSaveDocument");
      DelegateReport(RhinoDoc.m_end_save_document, "EndSaveDocument");
      DelegateReport(RhinoDoc.m_add_object, "AddObject");
      DelegateReport(RhinoDoc.m_delete_object, "DeleteObject");
      DelegateReport(RhinoDoc.m_replace_object, "ReplaceObject");
      DelegateReport(RhinoDoc.m_undelete_object, "UndeleteObject");
      DelegateReport(RhinoDoc.m_purge_object, "PurgeObject");
    }

    internal delegate void RdkReportCallback(int c);
    internal static RdkReportCallback m_rdk_ew_report = RdkEventWatcherReport;
    internal static void RdkEventWatcherReport(int c)
    {
      UnsafeNativeMethods.CRdkCmnEventWatcher_LogState("RhinoRdkCommon delegate based event watcher\n");
      DelegateReport(Rhino.Render.RenderContent.m_content_added_event, "RenderContentAdded");
    }

    internal static object m_rhinoscript;
    internal static object GetRhinoScriptObject()
    {
      return m_rhinoscript ?? (m_rhinoscript = Rhino.RhinoApp.GetPlugInObject("RhinoScript"));
    }

    /// <summary>
    /// Defines if Ole alerts ("Server busy") alerts should be visualized.
    /// <para>This function makes no sense on Mono.</para>
    /// </summary>
    /// <param name="display">Whether alerts should be visible.</param>
    /// <since>5.0</since>
    public static void DisplayOleAlerts(bool display)
    {
      UnsafeNativeMethods.RHC_DisplayOleAlerts(display);
    }

    /// <summary>
    /// Get the processor count on this hardware. It supports
    /// querying on CPUs with more than 64 processors (Windows).
    /// </summary>
    /// <since>7.4</since>
    public static int GetSystemProcessorCount()
    {
      if (RunningOnWindows)
        return UnsafeNativeMethods.RHC_GetSystemCpuThreadCount();
      else
        return System.Environment.ProcessorCount;
    }
#endif

    internal static bool ContainsDelegate(MulticastDelegate source, Delegate d)
    {
      if (null != source && null != d)
      {
        Delegate[] list = source.GetInvocationList();
        if (null != list)
        {
          for (int i = 0; i < list.Length; i++)
          {
            if (list[i].Equals(d))
              return true;
          }
        }
      }
      return false;
    }

    /// <summary>
    /// Only works on Windows. Returns null on Mac.
    /// </summary>
    /// <returns>An assembly.</returns>
    /// <since>5.0</since>
    public static System.Reflection.Assembly GetRhinoDotNetAssembly()
    {
      if (m_rhdn_assembly == null && RunningOnWindows)
      {
        System.Reflection.Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
        for (int i = 0; i < assemblies.Length; i++)
        {
          if (assemblies[i].FullName.StartsWith("Rhino_DotNet", StringComparison.OrdinalIgnoreCase))
          {
            m_rhdn_assembly = assemblies[i];
            break;
          }
        }
      }
      return m_rhdn_assembly;
    }
    static System.Reflection.Assembly m_rhdn_assembly;

    /// <summary>
    /// Informs the runtime that the application is shutting down.
    /// </summary>
    /// <since>5.0</since>
    public static void SetInShutDown()
    {
      //Added by Andy - to make sure that the GC doesn't start deleting stuff that
      //is actually owned by plug-ins or other DLLs that will soon not be around.
      GC.Collect();
      GC.WaitForPendingFinalizers();

      // 26 June 2018 S. Baer (RH-46531)
      // Don't report exceptions after Rhino starts shutting down. It typically
      // just clutters the desktop with RhinoDotNetCrash.txt files
      g_ReportExceptions = false;
      try
      {
#if RHINO_SDK
        UnsafeNativeMethods.RHC_SetSendLogMessageToCloudProc(null);
#endif
        UnsafeNativeMethods.RhCmn_SetInShutDown();
        // Remove callbacks that should not happen after this point in time
#if RHINO_SDK
        Rhino.Render.RdkPlugIn.SetRdkCallbackFunctions(false);
        Skin.DeletePointer();
#endif
      }
      catch
      {
        //throw away, we are shutting down
      }
    }

#if RHINO_SDK
    internal static void WriteIntoSerializationInfo(IntPtr pRhCmnProfileContext, System.Runtime.Serialization.SerializationInfo info, string prefixStrip)
    {
      const int _string = 1;
      const int _multistring = 2;
      const int _uuid = 3;
      const int _color = 4;
      const int _int = 5;
      const int _double = 6;
      const int _rect = 7;
      const int _point = 8;
      const int _3dpoint = 9;
      const int _xform = 10;
      const int _3dvector = 11;
      const int _meshparams = 12;
      const int _buffer = 13;
      const int _bool = 14;
      int count = UnsafeNativeMethods.CRhCmnProfileContext_Count(pRhCmnProfileContext);
      using (StringHolder sectionholder = new StringHolder())
      using (StringHolder entryholder = new StringHolder())
      {
        IntPtr pStringSection = sectionholder.NonConstPointer();
        IntPtr pStringEntry = entryholder.NonConstPointer();
        for (int i = 0; i < count; i++)
        {
          int pctype = 0;
          UnsafeNativeMethods.CRhCmnProfileContext_Item(pRhCmnProfileContext, i, pStringSection, pStringEntry, ref pctype);
          string section = sectionholder.ToString();
          string entry = entryholder.ToString();
          if (string.IsNullOrEmpty(entry))
            continue;
          string name = string.IsNullOrEmpty(section) ? entry : section + "\\" + entry;
          if (name.StartsWith(prefixStrip + "\\"))
            name = name.Substring(prefixStrip.Length + 1);
          name = name.Replace("\\", "::");

          switch (pctype)
          {
            case _string:
              {
                UnsafeNativeMethods.CRhinoProfileContext_LoadString(pRhCmnProfileContext, section, entry, pStringEntry);
                string val = entryholder.ToString();
                info.AddValue(name, val);
              }
              break;
            case _multistring:
              {
                using (var strings = new ClassArrayString())
                {
                  IntPtr ptr_strings = strings.NonConstPointer();
                  UnsafeNativeMethods.CRhinoProfileContext_LoadStrings(pRhCmnProfileContext, section, entry, ptr_strings);
                  string[] s = strings.ToArray();
                  info.AddValue(name, s);
                }
              }
              break;
            case _uuid:
              {
                Guid id = Guid.Empty;
                UnsafeNativeMethods.CRhinoProfileContext_LoadGuid(pRhCmnProfileContext, section, entry, ref id);
                info.AddValue(name, id);
              }
              break;
            case _color:
              {
                int abgr = 0;
                UnsafeNativeMethods.CRhinoProfileContext_LoadColor(pRhCmnProfileContext, section, entry, ref abgr);
                System.Drawing.Color c = Interop.ColorFromWin32(abgr);
                //string s = System.Drawing.ColorTranslator.ToHtml(c);
                info.AddValue(name, c);
              }
              break;
            case _int:
              {
                int ival = 0;
                UnsafeNativeMethods.CRhinoProfileContext_LoadInt(pRhCmnProfileContext, section, entry, ref ival);
                info.AddValue(name, ival);
              }
              break;
            case _double:
              {
                double dval = 0;
                UnsafeNativeMethods.CRhinoProfileContext_LoadDouble(pRhCmnProfileContext, section, entry, ref dval);
                info.AddValue(name, dval);
              }
              break;
            case _rect:
              {
                int left = 0, top = 0, right = 0, bottom = 0;
                UnsafeNativeMethods.CRhinoProfileContext_LoadRect(pRhCmnProfileContext, section, entry, ref left, ref top, ref right, ref bottom);
                System.Drawing.Rectangle r = System.Drawing.Rectangle.FromLTRB(left, top, right, bottom);
                info.AddValue(name, r);
              }
              break;
            case _point:
              {
                int x = 0, y = 0;
                UnsafeNativeMethods.CRhinoProfileContext_LoadPoint(pRhCmnProfileContext, section, entry, ref x, ref y);
                System.Drawing.Point pt = new System.Drawing.Point(x, y);
                info.AddValue(name, pt);
              }
              break;
            case _3dpoint:
              {
                Rhino.Geometry.Point3d pt = new Geometry.Point3d();
                UnsafeNativeMethods.CRhinoProfileContext_LoadPoint3d(pRhCmnProfileContext, section, entry, ref pt);
                info.AddValue(name, pt);
              }
              break;
            case _xform:
              {
                Rhino.Geometry.Transform xf = new Geometry.Transform();
                UnsafeNativeMethods.CRhinoProfileContext_LoadXform(pRhCmnProfileContext, section, entry, ref xf);
                info.AddValue(name, xf);
              }
              break;
            case _3dvector:
              {
                Rhino.Geometry.Vector3d vec = new Geometry.Vector3d();
                UnsafeNativeMethods.CRhinoProfileContext_LoadVector3d(pRhCmnProfileContext, section, entry, ref vec);
                info.AddValue(name, vec);
              }
              break;
            case _meshparams:
              {
                Rhino.Geometry.MeshingParameters mp = new Geometry.MeshingParameters();
                UnsafeNativeMethods.CRhinoProfileContext_LoadMeshParameters(pRhCmnProfileContext, section, entry, mp.NonConstPointer());
                info.AddValue(name, mp);
                mp.Dispose();
              }
              break;
            case _buffer:
              {
                //not supported yet
                //int buffer_length = UnsafeNativeMethods.CRhinoProfileContext_BufferLength(pRhCmnProfileContext, section, entry);
                //byte[] buffer = new byte[buffer_length];
                //UnsafeNativeMethods.CRhinoProfileContext_LoadBuffer(pRhCmnProfileContext, section, entry, buffer_length, buffer);
                //info.AddValue(name, buffer);
              }
              break;
            case _bool:
              {
                bool b = false;
                UnsafeNativeMethods.CRhinoProfileContext_LoadBool(pRhCmnProfileContext, section, entry, ref b);
                info.AddValue(name, b);
              }
              break;
          }
        }
      }
    }

    internal static IntPtr ReadIntoProfileContext(System.Runtime.Serialization.SerializationInfo info, string sectionBase)
    {
      IntPtr pProfileContext = UnsafeNativeMethods.CRhCmnProfileContext_New();
      var e = info.GetEnumerator();
      while (e.MoveNext())
      {
        string entry = e.Name.Replace("::", "\\");
        string section = sectionBase;
        int split_index = entry.LastIndexOf("\\", System.StringComparison.Ordinal);
        if (split_index > -1)
        {
          section = sectionBase + "\\" + entry.Substring(0, split_index);
          entry = entry.Substring(split_index + 1);
        }


        Type t = e.ObjectType;
        if( typeof(string) == t )
          UnsafeNativeMethods.CRhinoProfileContext_SaveProfileString(pProfileContext, section, entry, e.Value as string);
        else if( typeof(Guid) == t )
          UnsafeNativeMethods.CRhinoProfileContext_SaveProfileUuid(pProfileContext, section, entry, (Guid)e.Value);
        else if( typeof(System.Drawing.Color) == t )
        {
          System.Drawing.Color c = (System.Drawing.Color)e.Value;
          int argb = c.ToArgb();
          UnsafeNativeMethods.CRhinoProfileContext_SaveProfileColor(pProfileContext, section, entry, argb);
        }
        else if( typeof(int) == t )
          UnsafeNativeMethods.CRhinoProfileContext_SaveProfileInt(pProfileContext, section, entry, (int)e.Value);
        else if( typeof(double) == t )
          UnsafeNativeMethods.CRhinoProfileContext_SaveProfileDouble(pProfileContext, section, entry, (double)e.Value);
        else if( typeof(System.Drawing.Rectangle) == t )
        {
          System.Drawing.Rectangle r = (System.Drawing.Rectangle)e.Value;
          UnsafeNativeMethods.CRhinoProfileContext_SaveProfileRect(pProfileContext, section, entry, r.Left, r.Top, r.Right, r.Bottom);
        }
        else if( typeof(System.Drawing.Point) == t )
        {
          System.Drawing.Point pt = (System.Drawing.Point)e.Value;
          UnsafeNativeMethods.CRhinoProfileContext_SaveProfilePoint(pProfileContext, section, entry, pt.X, pt.Y);
        }
        else if( typeof(Rhino.Geometry.Point3d) == t )
        {
          Rhino.Geometry.Point3d pt = (Rhino.Geometry.Point3d)e.Value;
          UnsafeNativeMethods.CRhinoProfileContext_SaveProfilePoint3d(pProfileContext, section, entry, pt);
        }
        else if( typeof(Rhino.Geometry.Transform) == t )
        {
          Rhino.Geometry.Transform xf = (Rhino.Geometry.Transform)e.Value;
          UnsafeNativeMethods.CRhinoProfileContext_SaveProfileXform(pProfileContext, section, entry, ref xf);
        }
        else if( typeof(Rhino.Geometry.Vector3d) == t )
        {
          Rhino.Geometry.Vector3d v = (Rhino.Geometry.Vector3d)e.Value;
          UnsafeNativeMethods.CRhinoProfileContext_SaveProfileVector3d(pProfileContext, section, entry, v);
        }
        else if( typeof(Rhino.Geometry.MeshingParameters) == t )
        {
          Rhino.Geometry.MeshingParameters mp = e.Value as Rhino.Geometry.MeshingParameters;
          if (mp != null)
          {
            IntPtr pMp = mp.ConstPointer();
            UnsafeNativeMethods.CRhinoProfileContext_SaveProfileMeshingParameters(pProfileContext, section, entry, pMp);
          }
        }
        else if( typeof(byte[]) == t )
        {
          byte[] b = e.Value as byte[];
          if (b != null)
          {
            UnsafeNativeMethods.CRhinoProfileContext_SaveProfileBuffer(pProfileContext, section, entry, b.Length, b);
          }
        }
        else if (typeof(bool) == t)
          UnsafeNativeMethods.CRhinoProfileContext_SaveProfileBool(pProfileContext, section, entry, (bool)e.Value);
        else
        {
          //try
          //{
            string s = info.GetString(e.Name);
            UnsafeNativeMethods.CRhinoProfileContext_SaveProfileString(pProfileContext, section, entry, s);
          //}
          //catch (Exception ex)
          //{
          //  throw;
          //}
        }
      }
      return pProfileContext;
    }
#endif
  }

  /// <summary>
  /// Is thrown when the RDK is not loaded.
  /// </summary>
  [Serializable]
  public class RdkNotLoadedException : Exception
  {
    /// <summary>
    /// Initializes a new instance of the RDK not loaded exception with a standard message.
    /// </summary>
    /// <since>5.0</since>
    public RdkNotLoadedException() : base("The Rhino Rdk is not loaded.") { }
  }
}
