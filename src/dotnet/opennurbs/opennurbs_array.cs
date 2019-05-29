using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Rhino.Geometry;
using System.Linq;
using Rhino.FileIO;

namespace Rhino.Runtime.InteropWrappers
{
  class INTERNAL_ComponentIndexArray : IDisposable
  {
    public IntPtr m_ptr; // ON_SimpleArray<ON_COMPONENT_INDEX>
    public IntPtr NonConstPointer() { return m_ptr; }

    public INTERNAL_ComponentIndexArray()
    {
      m_ptr = UnsafeNativeMethods.ON_ComponentIndexArray_New();
    }

    public int Count
    {
      get { return UnsafeNativeMethods.ON_ComponentIndexArray_Count(m_ptr); }
    }

    public ComponentIndex[] ToArray()
    {
      int count = Count;
      if (count < 1)
        return null;
      ComponentIndex[] rc = new ComponentIndex[count];
      UnsafeNativeMethods.ON_ComponentIndexArray_CopyValues(m_ptr, rc);
      return rc;
    }

    public void Add(ComponentIndex index)
    {
        var non_const_ptr = NonConstPointer();
        UnsafeNativeMethods.ON_ComponentIndexArray_Add(non_const_ptr, ref index);
    }

    ~INTERNAL_ComponentIndexArray()
    {
      InternalDispose();
    }

    public void Dispose()
    {
      InternalDispose();
      GC.SuppressFinalize(this);
    }

    private void InternalDispose()
    {
      if (IntPtr.Zero != m_ptr)
      {
        UnsafeNativeMethods.ON_ComponentIndexArray_Delete(m_ptr);
        m_ptr = IntPtr.Zero;
      }
    }
  }

  /// <summary>
  /// This class is used to pass strings back and forth between managed
  /// and unmanaged code.  This should not be be needed by plug-ins.
  /// <para>If you are just dealing with an ON_wString*,
  /// use <see cref="Rhino.Runtime.InteropWrappers.StringWrapper"/></para>
  /// </summary>
  public class StringHolder : IDisposable
  {
    IntPtr m_ptr; // CRhCmnString*
    /// <summary>
    /// C++ pointer used to access the ON_wString, managed plug-ins should
    /// never need this.
    /// </summary>
    /// <returns></returns>
    public IntPtr ConstPointer() { return m_ptr; }
    /// <summary>
    /// C++ pointer used to access the ON_wString, managed plug-ins should
    /// never need this.
    /// </summary>
    /// <returns></returns>
    public IntPtr NonConstPointer() { return m_ptr; }
    /// <summary>
    /// Constructor
    /// </summary>
    public StringHolder()
    {
      m_ptr = UnsafeNativeMethods.StringHolder_New();
    }
    /// <summary>
    /// Destructor
    /// </summary>
    ~StringHolder()
    {
      Dispose(false);
    }
    /// <summary>
    /// IDispose implementation
    /// </summary>
    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }
    /// <summary>
    /// Called by Dispose and finalizer
    /// </summary>
    /// <param name="disposing"></param>
    protected virtual void Dispose( bool disposing )
    {
      if (IntPtr.Zero != m_ptr)
      {
        UnsafeNativeMethods.StringHolder_Delete(m_ptr);
        m_ptr = IntPtr.Zero;
      }
    }
    /// <summary>
    /// Marshals unmanaged ON_wString to a managed .NET string
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
      return GetString(m_ptr);
    }
    /// <summary>
    /// Gets managed string from unmanaged ON_wString pointer.
    /// </summary>
    /// <param name="pStringHolder"></param>
    /// <returns>Null if pStringHolder has no reference, otherwise, the string. This may be an empty string, if setting an empty string is possible.</returns>
    public static string GetString(IntPtr pStringHolder)
    {
      IntPtr pString = UnsafeNativeMethods.StringHolder_Get(pStringHolder);
      return Marshal.PtrToStringUni(pString);
    }
  }
  /// <summary>
  /// Wrapper for ON_SimpleArray&lt;int&gt;. If you are not writing C++ code
  /// then this class is not for you.
  /// </summary>
  public class SimpleArrayInt : IDisposable
  {
    //This should be private eventually and have everything call either ConstPointer or NonConstPointer
    internal IntPtr m_ptr; // ON_SimpleArray<int>

    /// <summary>
    /// Gets the const (immutable) pointer of this array.
    /// </summary>
    /// <returns>The const pointer.</returns>
    public IntPtr ConstPointer() { return m_ptr; }

    /// <summary>
    /// Gets the non-const pointer (for modification) of this array.
    /// </summary>
    /// <returns>The non-const pointer.</returns>
    public IntPtr NonConstPointer() { return m_ptr; }

    /// <summary>
    /// Initializes a new <see cref="SimpleArrayInt"/> class.
    /// </summary>
    public SimpleArrayInt()
    {
      m_ptr = UnsafeNativeMethods.ON_IntArray_New(null,0);
    }

    /// <summary>
    /// Initializes a new <see cref="SimpleArrayInt"/> class
    /// </summary>
    /// <param name="values">initial set of integers to add to the array</param>
    public SimpleArrayInt(IEnumerable<int> values)
    {
      if (values == null)
      {
        m_ptr = UnsafeNativeMethods.ON_IntArray_New(null,0);
      }
      else
      {
        List<int> list_values = new List<int>(values);
        int[] array_values = list_values.ToArray();
        m_ptr = UnsafeNativeMethods.ON_IntArray_New(array_values, list_values.Count);
      }
    }

    /// <summary>
    /// Gets the amount of elements in this array.
    /// </summary>
    public int Count
    {
      get { return UnsafeNativeMethods.ON_IntArray_Count(m_ptr); }
    }

    /// <summary>
    /// Returns the managed counterpart of the unmanaged array.
    /// </summary>
    /// <returns>The managed array.</returns>
    public int[] ToArray()
    {
      int count = Count;
      if (count < 1)
        return new int[0];
      int[] rc = new int[count];
      UnsafeNativeMethods.ON_IntArray_CopyValues(m_ptr, rc);
      return rc;
    }

    /// <summary>
    /// Passively reclaims unmanaged resources when the class user did not explicitly call Dispose().
    /// </summary>
    ~SimpleArrayInt()
    {
      InternalDispose();
    }

    /// <summary>
    /// Actively reclaims unmanaged resources that this instance uses.
    /// </summary>
    public void Dispose()
    {
      InternalDispose();
      GC.SuppressFinalize(this);
    }

    private void InternalDispose()
    {
      if (IntPtr.Zero != m_ptr)
      {
        UnsafeNativeMethods.ON_IntArray_Delete(m_ptr);
        m_ptr = IntPtr.Zero;
      }
    }
  }

  /// <summary>
  /// Wrapper for ON_SimpleArray&lt;unsigned int&gt;. If you are not writing C++ code
  /// then this class is not for you.
  /// </summary>
  public class SimpleArrayUint : IDisposable
  {
    internal IntPtr m_ptr; // ON_SimpleArray<unsigned int>

    /// <summary>
    /// Gets the const (immutable) pointer of this array.
    /// </summary>
    /// <returns>The const pointer.</returns>
    public IntPtr ConstPointer() { return m_ptr; }

    /// <summary>
    /// Gets the non-const pointer (for modification) of this array.
    /// </summary>
    /// <returns>The non-const pointer.</returns>
    public IntPtr NonConstPointer() { return m_ptr; }

    /// <summary>
    /// Initializes a new <see cref="SimpleArrayInt"/> class.
    /// </summary>
    public SimpleArrayUint()
    {
      m_ptr = UnsafeNativeMethods.ON_UintArray_New(0);
    }

    /// <summary>
    /// Initializes a new <see cref="SimpleArrayInt"/> class.
    /// </summary>
    /// <param name="values">A list, an array or any collection of unsigned ints that implements the enumerable interface.</param>
    [CLSCompliant(false)]
    public SimpleArrayUint(System.Collections.Generic.IEnumerable<uint> values)
    {
      var initial_capacity = values.Count();
      m_ptr = UnsafeNativeMethods.ON_UintArray_New(initial_capacity);
      foreach (var v in values)
        UnsafeNativeMethods.ON_UintArray_Append(m_ptr, v);
    }

    /// <summary>
    /// Gets the number of elements in this array.
    /// </summary>
    public int Count
    {
      get { return UnsafeNativeMethods.ON_UintArray_Count(m_ptr); }
    }

    /// <summary>
    /// Gets the number of elements in this array.
    /// </summary>
    [CLSCompliant(false)]
    public uint UnsignedCount
    {
      get { return UnsafeNativeMethods.ON_UintArray_UnsignedCount(m_ptr); }
    }

    /// <summary>
    /// Returns the managed counterpart of the unmanaged array.
    /// </summary>
    /// <returns>The managed array.</returns>
    [CLSCompliant(false)]
    public uint[] ToArray()
    {
      uint count = UnsignedCount;
      if (count < 1)
        return new uint[0];
      uint[] rc = new uint[count];
      UnsafeNativeMethods.ON_UintArray_CopyValues(m_ptr, rc);
      return rc;
    }

    /// <summary>
    /// Passively reclaims unmanaged resources when the class user did not explicitly call Dispose().
    /// </summary>
    ~SimpleArrayUint()
    {
      InternalDispose();
    }

    /// <summary>
    /// Actively reclaims unmanaged resources that this instance uses.
    /// </summary>
    public void Dispose()
    {
      InternalDispose();
      GC.SuppressFinalize(this);
    }

    private void InternalDispose()
    {
      if (IntPtr.Zero != m_ptr)
      {
        UnsafeNativeMethods.ON_UintArray_Delete(m_ptr);
        m_ptr = IntPtr.Zero;
      }
    }
  }


  /// <summary>
  /// Wrapper for ON_SimpleArray&lt;ON_UUID&gt;. If you are not writing C++ code
  /// then this class is not for you.
  /// </summary>
  public class SimpleArrayGuidPointer : IDisposable
  {
    internal bool DontDelete { get; set; }
    internal IntPtr m_ptr; // ON_SimpleArray<ON_UUID*>

    /// <summary>
    /// Gets the const (immutable) pointer of this array.
    /// </summary>
    /// <returns>The const pointer.</returns>
    public IntPtr ConstPointer() { return m_ptr; }

    /// <summary>
    /// Gets the non-const pointer (for modification) of this array.
    /// </summary>
    /// <returns>The non-const pointer.</returns>
    public IntPtr NonConstPointer() { return m_ptr; }

    /// <summary>
    /// Initializes a new <see cref="SimpleArrayGuidPointer"/> class.
    /// </summary>
    public SimpleArrayGuidPointer()
    {
      m_ptr = UnsafeNativeMethods.ON_UUIDPtrArray_New();
    }

    /// <summary>
    /// Get the Guid at index
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public Guid this[int index]
    {
      get { return UnsafeNativeMethods.ON_UUIDPtrArray_Get(m_ptr, index); }
    }

    // Create a SimpleArrayGuid from a pointer owned elsewhere. Need
    // to ensure that pointer isn't deleted on Disposal of this array
    internal SimpleArrayGuidPointer(IntPtr ptr)
    {
      m_ptr = ptr;
      DontDelete = true;
    }

    /// <summary>
    /// Gets the amount of elements in this array.
    /// </summary>
    public int Count
    {
      get { return UnsafeNativeMethods.ON_UUIDPtrArray_Count(m_ptr); }
    }

    /// <summary>
    /// Returns the managed counterpart of the unmanaged array.
    /// </summary>
    /// <returns>The managed array.</returns>
    public Guid[] ToArray()
    {
      var count = Count;
      if (count < 1)
        return new Guid[0];
      var rc = new Guid[count];
      for (var i = 0; i < count; i++)
      {
        rc[i] = this[i];
      }
      return rc;
    }

    /// <summary>
    /// Passively reclaims unmanaged resources when the class user did not explicitly call Dispose().
    /// </summary>
    ~SimpleArrayGuidPointer()
    {
      InternalDispose();
    }

    /// <summary>
    /// Actively reclaims unmanaged resources that this instance uses.
    /// </summary>
    public void Dispose()
    {
      InternalDispose();
      GC.SuppressFinalize(this);
    }

    private void InternalDispose()
    {
      if (IntPtr.Zero != m_ptr)
      {
        if(!DontDelete) UnsafeNativeMethods.ON_UUIDPtrArray_Delete(m_ptr);
        m_ptr = IntPtr.Zero;
      }
    }
  }
  /// <summary>
  /// Wrapper for ON_SimpleArray&lt;ON_UUID&gt;. If you are not writing C++ code
  /// then this class is not for you.
  /// </summary>
  public class SimpleArrayGuid : IDisposable
  {
    internal bool DontDelete { get; set; }
    internal IntPtr m_ptr; // ON_SimpleArray<ON_UUID>

    /// <summary>
    /// Gets the const (immutable) pointer of this array.
    /// </summary>
    /// <returns>The const pointer.</returns>
    public IntPtr ConstPointer() { return m_ptr; }

    /// <summary>
    /// Gets the non-const pointer (for modification) of this array.
    /// </summary>
    /// <returns>The non-const pointer.</returns>
    public IntPtr NonConstPointer() { return m_ptr; }

    /// <summary>
    /// Initializes a new <see cref="SimpleArrayGuid"/> class.
    /// </summary>
    public SimpleArrayGuid()
    {
      m_ptr = UnsafeNativeMethods.ON_UUIDArray_New();
    }

    /// <summary>
    /// Get the Guid at index
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public Guid this[int index]
    {
      get { return UnsafeNativeMethods.ON_UUIDArray_Get(m_ptr, index); }
    }

    /// <summary>
    /// Appends a new <see cref="Guid"/> at the end of this array.
    /// </summary>
    public void Append(Guid uuid)
    {
      var non_const_ptr = NonConstPointer();
      UnsafeNativeMethods.ON_UUIDArray_Append(non_const_ptr, uuid);
    }

    // Create a SimpleArrayGuid from a pointer owned elsewhere. Need
    // to ensure that pointer isn't deleted on Disposal of this array
    internal SimpleArrayGuid(IntPtr ptr)
    {
      m_ptr = ptr;
      DontDelete = true;
    }

    /// <summary>
    /// Gets the amount of elements in this array.
    /// </summary>
    public int Count
    {
      get { return UnsafeNativeMethods.ON_UUIDArray_Count(m_ptr); }
    }

    /// <summary>
    /// Returns the managed counterpart of the unmanaged array.
    /// </summary>
    /// <returns>The managed array.</returns>
    public Guid[] ToArray()
    {
      int count = Count;
      if (count < 1)
        return new Guid[0];
      Guid[] rc = new Guid[count];
      UnsafeNativeMethods.ON_UUIDArray_CopyValues(m_ptr, rc);
      return rc;
    }

    /// <summary>
    /// Passively reclaims unmanaged resources when the class user did not explicitly call Dispose().
    /// </summary>
    ~SimpleArrayGuid()
    {
      InternalDispose();
    }

    /// <summary>
    /// Actively reclaims unmanaged resources that this instance uses.
    /// </summary>
    public void Dispose()
    {
      InternalDispose();
      GC.SuppressFinalize(this);
    }

    private void InternalDispose()
    {
      if (IntPtr.Zero != m_ptr)
      {
        if(!DontDelete) UnsafeNativeMethods.ON_UUIDArray_Delete(m_ptr);
        m_ptr = IntPtr.Zero;
      }
    }
  }

  /// <summary>
  /// Wrapper for ON_SimpleArray&lt;ON_Interval&gt;. If you are not writing C++ code
  /// then this class is not for you.
  /// </summary>
  public class SimpleArrayInterval : IDisposable
  {
    IntPtr m_ptr; // ON_SimpleArray<ON_Interval>

    /// <summary>
    /// Gets the const (immutable) pointer of this array.
    /// </summary>
    /// <returns>The const pointer.</returns>
    public IntPtr ConstPointer() { return m_ptr; }

    /// <summary>
    /// Gets the non-const pointer (for modification) of this array.
    /// </summary>
    /// <returns>The non-const pointer.</returns>
    public IntPtr NonConstPointer() { return m_ptr; }

    /// <summary>
    /// Initializes a new <see cref="SimpleArrayInterval"/> class.
    /// </summary>
    public SimpleArrayInterval()
    {
      m_ptr = UnsafeNativeMethods.ON_IntervalArray_New();
    }

    /// <summary>
    /// Adds a new <see cref="Interval"/> at the end of this array.
    /// </summary>
    public void Add(Interval interval)
    {
      var non_const_ptr = NonConstPointer();
      UnsafeNativeMethods.ON_IntervalArray_Add(non_const_ptr, interval);
    }

    /// <summary>
    /// Gets the amount of elements in this array.
    /// </summary>
    public int Count
    {
      get { return UnsafeNativeMethods.ON_IntervalArray_Count(m_ptr); }
    }

    /// <summary>
    /// Returns the managed counterpart of the unmanaged array.
    /// </summary>
    /// <returns>The managed array.</returns>
    public Interval[] ToArray()
    {
      int count = Count;
      if (count < 1)
        return new Interval[0];
      var rc = new Interval[count];
      UnsafeNativeMethods.ON_IntervalArray_CopyValues(m_ptr, rc);
      return rc;
    }

    /// <summary>
    /// Passively reclaims unmanaged resources when the class user did not explicitly call Dispose().
    /// </summary>
    ~SimpleArrayInterval()
    {
      InternalDispose();
    }

    /// <summary>
    /// Actively reclaims unmanaged resources that this instance uses.
    /// </summary>
    public void Dispose()
    {
      InternalDispose();
      GC.SuppressFinalize(this);
    }

    private void InternalDispose()
    {
      if (IntPtr.Zero != m_ptr)
      {
        UnsafeNativeMethods.ON_IntervalArray_Delete(m_ptr);
        m_ptr = IntPtr.Zero;
      }
    }
  }

  /// <summary>
  /// Wrapper for ON_SimpleArray&lt;double&gt;. If you are not writing C++ code,
  /// then this class is not for you.
  /// </summary>
  public class SimpleArrayDouble : IDisposable
  {
    private IntPtr m_ptr; // ON_SimpleArray<double>

    /// <summary>
    /// Gets the const (immutable) pointer of this array.
    /// </summary>
    /// <returns>The const pointer.</returns>
    public IntPtr ConstPointer() { return m_ptr; }

    /// <summary>
    /// Gets the non-const pointer (for modification) of this array.
    /// </summary>
    /// <returns>The non-const pointer.</returns>
    public IntPtr NonConstPointer() { return m_ptr; }

    /// <summary>
    /// Initializes a new <see cref="SimpleArrayDouble"/> instance.
    /// </summary>
    public SimpleArrayDouble()
    {
      m_ptr = UnsafeNativeMethods.ON_DoubleArray_New();
    }

    /// <summary>
    /// Initializes a new <see cref="SimpleArrayDouble"/> instance, with items.
    /// </summary>
    public SimpleArrayDouble(System.Collections.Generic.IEnumerable<double> items)
    {
      // 17-Sep-2015 Dale Fugier http://mcneel.myjetbrains.com/youtrack/issue/RH-31413
      // Helps to have a newed up array...
      m_ptr = UnsafeNativeMethods.ON_DoubleArray_New();

      Rhino.Collections.RhinoList<double> list = new Rhino.Collections.RhinoList<double>(items);
      UnsafeNativeMethods.ON_DoubleArray_Append(m_ptr, list.Count, list.m_items);
    }

    /// <summary>
    /// Gets the amount of elements in this array.
    /// </summary>
    public int Count
    {
      get { return UnsafeNativeMethods.ON_DoubleArray_Count(m_ptr); }
    }

    /// <summary>
    /// Returns the managed counterpart of the unmanaged array.
    /// </summary>
    /// <returns>The managed array.</returns>
    public double[] ToArray()
    {
      int count = Count;
      if (count < 1)
        return new double[0];
      double[] rc = new double[count];
      UnsafeNativeMethods.ON_DoubleArray_CopyValues(m_ptr, rc);
      return rc;
    }

    /// <summary>
    /// Passively reclaims unmanaged resources when the class user did not explicitly call Dispose().
    /// </summary>
    ~SimpleArrayDouble()
    {
      InternalDispose();
    }

    /// <summary>
    /// Actively reclaims unmanaged resources that this instance uses.
    /// </summary>
    public void Dispose()
    {
      InternalDispose();
      GC.SuppressFinalize(this);
    }

    private void InternalDispose()
    {
      if (IntPtr.Zero != m_ptr)
      {
        UnsafeNativeMethods.ON_DoubleArray_Delete(m_ptr);
        m_ptr = IntPtr.Zero;
      }
    }
  }

  /// <summary>
  /// ON_SimpleArray&lt;ON_2dPoint&gt; class wrapper.  If you are not writing
  /// C++ code then this class is not for you.
  /// </summary>
  public class SimpleArrayPoint2d : IDisposable
  {
    private IntPtr m_ptr;

    /// <summary>
    /// Gets the const (immutable) pointer of this array.
    /// </summary>
    /// <returns>The const pointer.</returns>
    public IntPtr ConstPointer() { return m_ptr; }

    /// <summary>
    /// Gets the non-const pointer (for modification) of this array.
    /// </summary>
    /// <returns>The non-const pointer.</returns>
    public IntPtr NonConstPointer() { return m_ptr; }

    /// <summary>
    /// Initializes a new empty <see cref="SimpleArrayPoint3d"/> instance.
    /// </summary>
    public SimpleArrayPoint2d()
    {
      m_ptr = UnsafeNativeMethods.ON_2dPointArray_New(0);
    }

    // not used and internal class, so comment out
    //public SimpleArrayPoint3d(int initialCapacity)
    //{
    //  m_ptr = UnsafeNativeMethods.ON_2dPointArray_New(initialCapacity);
    //}

    /// <summary>
    /// Gets the amount of points in this array.
    /// </summary>
    public int Count
    {
      get
      {
        IntPtr ptr = ConstPointer();
        int count = UnsafeNativeMethods.ON_2dPointArray_Count(ptr);
        return count;
      }
    }

    /// <summary>
    /// Copies the unmanaged array to a managed counterpart.
    /// </summary>
    /// <returns>The managed array.</returns>
    public Point2d[] ToArray()
    {
      int count = Count;
      if (count < 1)
        return new Point2d[0];

      Point2d[] rc = new Point2d[count];
      UnsafeNativeMethods.ON_2dPointArray_CopyValues(m_ptr, rc);
      return rc;
    }

    /// <summary>
    /// Passively reclaims unmanaged resources when the class user did not explicitly call Dispose().
    /// </summary>
    ~SimpleArrayPoint2d()
    {
      Dispose(false);
    }

    /// <summary>
    /// Actively reclaims unmanaged resources that this instance uses.
    /// </summary>
    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    /// <summary>
    /// For derived class implementers.
    /// <para>This method is called with argument true when class user calls Dispose(), while with argument false when
    /// the Garbage Collector invokes the finalizer, or Finalize() method.</para>
    /// <para>You must reclaim all used unmanaged resources in both cases, and can use this chance to call Dispose on disposable fields if the argument is true.</para>
    /// <para>Also, you must call the base virtual method within your overriding method.</para>
    /// </summary>
    /// <param name="disposing">true if the call comes from the Dispose() method; false if it comes from the Garbage Collector finalizer.</param>
    protected virtual void Dispose(bool disposing)
    {
      if (IntPtr.Zero != m_ptr)
      {
        UnsafeNativeMethods.ON_2dPointArray_Delete(m_ptr);
        m_ptr = IntPtr.Zero;
      }
    }
  }

  /// <summary>
  /// ON_SimpleArray&lt;ON_3dPoint&gt;, ON_3dPointArray, ON_PolyLine all have the same size
  /// This class wraps all of these C++ versions.  If you are not writing C++ code then this
  /// class is not for you.
  /// </summary>
  public class SimpleArrayPoint3d : IDisposable
  {
    private IntPtr m_ptr;

    /// <summary>
    /// Gets the const (immutable) pointer of this array.
    /// </summary>
    /// <returns>The const pointer.</returns>
    public IntPtr ConstPointer() { return m_ptr; }

    /// <summary>
    /// Gets the non-const pointer (for modification) of this array.
    /// </summary>
    /// <returns>The non-const pointer.</returns>
    public IntPtr NonConstPointer() { return m_ptr; }

    /// <summary>
    /// Initializes a new empty <see cref="SimpleArrayPoint3d"/> instance.
    /// </summary>
    public SimpleArrayPoint3d()
    {
      m_ptr = UnsafeNativeMethods.ON_3dPointArray_New(0);
    }

    // not used and internal class, so comment out
    //public SimpleArrayPoint3d(int initialCapacity)
    //{
    //  m_ptr = UnsafeNativeMethods.ON_3dPointArray_New(initialCapacity);
    //}

    /// <summary>
    /// Gets the amount of points in this array.
    /// </summary>
    public int Count
    {
      get
      {
        IntPtr ptr = ConstPointer();
        int count = UnsafeNativeMethods.ON_3dPointArray_Count(ptr);
        return count;
      }
    }

    /// <summary>
    /// Copies the unmanaged array to a managed counterpart.
    /// </summary>
    /// <returns>The managed array.</returns>
    public Point3d[] ToArray()
    {
      int count = Count;
      if (count < 1)
        return new Point3d[0];

      Point3d[] rc = new Point3d[count];
      UnsafeNativeMethods.ON_3dPointArray_CopyValues(m_ptr, rc);
      return rc;
    }

    /// <summary>
    /// Passively reclaims unmanaged resources when the class user did not explicitly call Dispose().
    /// </summary>
    ~SimpleArrayPoint3d()
    {
      Dispose(false);
    }

    /// <summary>
    /// Actively reclaims unmanaged resources that this instance uses.
    /// </summary>
    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    /// <summary>
    /// For derived class implementers.
    /// <para>This method is called with argument true when class user calls Dispose(), while with argument false when
    /// the Garbage Collector invokes the finalizer, or Finalize() method.</para>
    /// <para>You must reclaim all used unmanaged resources in both cases, and can use this chance to call Dispose on disposable fields if the argument is true.</para>
    /// <para>Also, you must call the base virtual method within your overriding method.</para>
    /// </summary>
    /// <param name="disposing">true if the call comes from the Dispose() method; false if it comes from the Garbage Collector finalizer.</param>
    protected virtual void Dispose(bool disposing)
    {
      if (IntPtr.Zero != m_ptr)
      {
        UnsafeNativeMethods.ON_3dPointArray_Delete(m_ptr);
        m_ptr = IntPtr.Zero;
      }
    }
  }


  /// <summary>
  /// Wrapper for ON_SimpleArray&lt;ON_Plane&gt;. If you are not writing C++ code
  /// then this class is not for you.
  /// </summary>
  public class SimpleArrayPlane : IDisposable
  {
    IntPtr m_ptr; // ON_SimpleArray<ON_Plane>

    /// <summary>
    /// Gets the const (immutable) pointer of this array.
    /// </summary>
    /// <returns>The const pointer.</returns>
    public IntPtr ConstPointer()
    {
      return m_ptr;
    }

    /// <summary>
    /// Gets the non-const pointer (for modification) of this array.
    /// </summary>
    /// <returns>The non-const pointer.</returns>
    public IntPtr NonConstPointer()
    {
      return m_ptr;
    }

    /// <summary>
    /// Initializes a new <see cref="SimpleArrayLine"/> instance.
    /// </summary>
    public SimpleArrayPlane()
    {
      m_ptr = UnsafeNativeMethods.ON_PlaneArray_New();
    }

    /// <summary>
    /// Gets the amount of lines in this array.
    /// </summary>
    public int Count
    {
      get { return UnsafeNativeMethods.ON_PlaneArray_Count(m_ptr); }
    }

    /// <summary>
    /// Copies the unmanaged array to a managed counterpart.
    /// </summary>
    /// <returns>The managed array.</returns>
    public Plane[] ToArray()
    {
      int count = Count;
      if (count < 1)
        return new Plane[0];
      Plane[] rc = new Plane[count];
      UnsafeNativeMethods.ON_PlaneArray_CopyValues(m_ptr, rc);
      return rc;
    }

    /// <summary>
    /// Passively reclaims unmanaged resources when the class user did not explicitly call Dispose().
    /// </summary>
    ~SimpleArrayPlane()
    {
      InternalDispose();
    }

    /// <summary>
    /// Actively reclaims unmanaged resources that this instance uses.
    /// </summary>
    public void Dispose()
    {
      InternalDispose();
      GC.SuppressFinalize(this);
    }

    private void InternalDispose()
    {
      if (IntPtr.Zero != m_ptr)
      {
        UnsafeNativeMethods.ON_PlaneArray_Delete(m_ptr);
        m_ptr = IntPtr.Zero;
      }
    }
  }


  /// <summary>
  /// Wrapper for ON_SimpleArray&lt;ON_Line&gt;. If you are not writing C++ code
  /// then this class is not for you.
  /// </summary>
  public class SimpleArrayLine : IDisposable
  {
    IntPtr m_ptr; // ON_SimpleArray<ON_Line>

    /// <summary>
    /// Gets the const (immutable) pointer of this array.
    /// </summary>
    /// <returns>The const pointer.</returns>
    public IntPtr ConstPointer()
    {
      return m_ptr;
    }

    /// <summary>
    /// Gets the non-const pointer (for modification) of this array.
    /// </summary>
    /// <returns>The non-const pointer.</returns>
    public IntPtr NonConstPointer()
    {
      return m_ptr;
    }

    /// <summary>
    /// Initializes a new <see cref="SimpleArrayLine"/> instance.
    /// </summary>
    public SimpleArrayLine()
    {
      m_ptr = UnsafeNativeMethods.ON_LineArray_New();
    }

    /// <summary>
    /// Gets the amount of lines in this array.
    /// </summary>
    public int Count
    {
      get { return UnsafeNativeMethods.ON_LineArray_Count(m_ptr); }
    }

    /// <summary>
    /// Copies the unmanaged array to a managed counterpart.
    /// </summary>
    /// <returns>The managed array.</returns>
    public Line[] ToArray()
    {
      int count = Count;
      if (count < 1)
        return new Line[0];
      Line[] rc = new Line[count];
      UnsafeNativeMethods.ON_LineArray_CopyValues(m_ptr, rc);
      return rc;
    }

    /// <summary>
    /// Passively reclaims unmanaged resources when the class user did not explicitly call Dispose().
    /// </summary>
    ~SimpleArrayLine()
    {
      InternalDispose();
    }

    /// <summary>
    /// Actively reclaims unmanaged resources that this instance uses.
    /// </summary>
    public void Dispose()
    {
      InternalDispose();
      GC.SuppressFinalize(this);
    }

    private void InternalDispose()
    {
      if (IntPtr.Zero != m_ptr)
      {
        UnsafeNativeMethods.ON_LineArray_Delete(m_ptr);
        m_ptr = IntPtr.Zero;
      }
    }
  }

  /// <summary>
  /// Wrapper for ON_SimpleArray&lt;ON_2dex&gt;. If you are not writing C++ code
  /// then this class is not for you.
  /// </summary>
  public class SimpleArray2dex : IDisposable
  {
    IntPtr m_ptr; // ON_SimpleArray<ON_2dex>

    /// <summary>
    /// Gets the const (immutable) pointer of this array.
    /// </summary>
    /// <returns>The const pointer.</returns>
    public IntPtr ConstPointer() { return m_ptr; }

    /// <summary>
    /// Gets the non-const pointer (for modification) of this array.
    /// </summary>
    /// <returns>The non-const pointer.</returns>
    public IntPtr NonConstPointer() { return m_ptr; }

    /// <summary>
    /// Initializes a new <see cref="SimpleArray2dex"/> class.
    /// </summary>
    public SimpleArray2dex()
    {
      m_ptr = UnsafeNativeMethods.ON_2dexArray_New(null,0);
    }

    /// <summary>
    /// Initializes a new <see cref="SimpleArray2dex"/> class
    /// </summary>
    /// <param name="values">initial set of integer pairs to add to the array</param>
    public SimpleArray2dex(IEnumerable<IndexPair> values)
    {
      if (values == null)
      {
        m_ptr = UnsafeNativeMethods.ON_2dexArray_New(null,0);
      }
      else
      {
        List<IndexPair> list_values = new List<IndexPair>(values);
        IndexPair[] array_values = list_values.ToArray();
        m_ptr = UnsafeNativeMethods.ON_2dexArray_New(array_values, list_values.Count);
      }
    }

    /// <summary>
    /// Gets the amount of elements in this array.
    /// </summary>
    public int Count
    {
      get { return UnsafeNativeMethods.ON_2dexArray_Count(m_ptr); }
    }

    /// <summary>
    /// Returns the managed counterpart of the unmanaged array.
    /// </summary>
    /// <returns>The managed array.</returns>
    public IndexPair[] ToArray()
    {
      int count = Count;
      if (count < 1)
        return new IndexPair[0];
      IndexPair[] rc = new IndexPair[count];
      UnsafeNativeMethods.ON_2dexArray_CopyValues(m_ptr, rc);
      return rc;
    }

    /// <summary>
    /// Passively reclaims unmanaged resources when the class user did not explicitly call Dispose().
    /// </summary>
    ~SimpleArray2dex()
    {
      InternalDispose();
    }

    /// <summary>
    /// Actively reclaims unmanaged resources that this instance uses.
    /// </summary>
    public void Dispose()
    {
      InternalDispose();
      GC.SuppressFinalize(this);
    }

    private void InternalDispose()
    {
      if (IntPtr.Zero != m_ptr)
      {
        UnsafeNativeMethods.ON_2dexArray_Delete(m_ptr);
        m_ptr = IntPtr.Zero;
      }
    }
  }

  /// <summary>
  /// Wrapper for a C++ ON_SimpleArray of ON_Surface* or const ON_Surface*.  If
  /// you are not writing C++ code then this class is not for you.
  /// </summary>
  public class SimpleArraySurfacePointer : IDisposable
  {
    IntPtr m_ptr; //ON_SimpleArray<ON_Surface*>*

    /// <summary>
    /// Gets the const (immutable) pointer of this array.
    /// </summary>
    /// <returns>The const pointer.</returns>
    public IntPtr ConstPointer() { return m_ptr; }

    /// <summary>
    /// Gets the non-const pointer (for modification) of this array.
    /// </summary>
    /// <returns>The non-const pointer.</returns>
    public IntPtr NonConstPointer() { return m_ptr; }

    /// <summary>
    /// Initializes a new <see cref="SimpleArraySurfacePointer"/> instance.
    /// </summary>
    public SimpleArraySurfacePointer()
    {
      m_ptr = UnsafeNativeMethods.ON_SurfaceArray_New();
    }

    /// <summary>
    /// Copies the unmanaged array to a managed counterpart.
    /// Elements are made non-const.
    /// </summary>
    /// <returns>The managed array.</returns>
    public Surface[] ToNonConstArray()
    {
      int count = UnsafeNativeMethods.ON_SurfaceArray_Count(m_ptr);
      if (count < 1)
        return new Surface[0];
      Surface[] rc = new Surface[count];
      for (int i = 0; i < count; i++)
      {
        IntPtr surface = UnsafeNativeMethods.ON_SurfaceArray_Get(m_ptr, i);
        if (IntPtr.Zero == surface)
          continue;
        rc[i] = GeometryBase.CreateGeometryHelper(surface, null) as Surface;
      }
      return rc;
    }

    /// <summary>
    /// Passively reclaims unmanaged resources when the class user did not explicitly call Dispose().
    /// </summary>
    ~SimpleArraySurfacePointer()
    {
      Dispose(false);
    }

    /// <summary>
    /// Actively reclaims unmanaged resources that this instance uses.
    /// </summary>
    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    /// <summary>
    /// For derived class implementers.
    /// <para>This method is called with argument true when class user calls Dispose(), while with argument false when
    /// the Garbage Collector invokes the finalizer, or Finalize() method.</para>
    /// <para>You must reclaim all used unmanaged resources in both cases, and can use this chance to call Dispose on disposable fields if the argument is true.</para>
    /// <para>Also, you must call the base virtual method within your overriding method.</para>
    /// </summary>
    /// <param name="disposing">true if the call comes from the Dispose() method; false if it comes from the Garbage Collector finalizer.</param>
    protected virtual void Dispose(bool disposing)
    {
      if (IntPtr.Zero != m_ptr)
      {
        UnsafeNativeMethods.ON_SurfaceArray_Delete(m_ptr);
        m_ptr = IntPtr.Zero;
      }
    }
  }

  /// <summary>
  /// Wrapper for a C++ ON_SimpleArray of ON_Curve* or const ON_Curve*.  If you are not
  /// writing C++ code, then you can ignore this class.
  /// </summary>
  public class SimpleArrayCurvePointer : IDisposable
  {
    IntPtr m_ptr; //ON_SimpleArray<ON_Curve*>*

    /// <summary>
    /// Gets the const (immutable) pointer of this array.
    /// </summary>
    /// <returns>The const pointer.</returns>
    public IntPtr ConstPointer() { return m_ptr; }

    /// <summary>
    /// Gets the non-const pointer (for modification) of this array.
    /// </summary>
    /// <returns>The non-const pointer.</returns>
    public IntPtr NonConstPointer() { return m_ptr; }

    /// <summary>
    /// Initializes a new <see cref="SimpleArrayCurvePointer"/> instance.
    /// </summary>
    public SimpleArrayCurvePointer()
    {
      m_ptr = UnsafeNativeMethods.ON_CurveArray_New(0);
    }

    /// <summary>
    /// Initializes a new <see cref="SimpleArrayCurvePointer"/> instance, from a set of input curves.
    /// </summary>
    /// <param name="curves">A list, an array or any collection of curves that implements the enumerable interface.</param>
    public SimpleArrayCurvePointer(System.Collections.Generic.IEnumerable<Curve> curves)
    {
      int initial_capacity = 0;
      foreach (Curve c in curves)
      {
        if (null != c)
          initial_capacity++;
      }

      m_ptr = UnsafeNativeMethods.ON_CurveArray_New(initial_capacity);
      foreach (Curve c in curves)
      {
        if (null != c)
        {
          IntPtr curvePtr = c.ConstPointer();
          UnsafeNativeMethods.ON_CurveArray_Append(m_ptr, curvePtr);
        }
      }
    }

    /// <summary>
    /// Copies the unmanaged array to a managed counterpart.
    /// </summary>
    /// <returns>The managed array.</returns>
    public Curve[] ToNonConstArray()
    {
      int count = UnsafeNativeMethods.ON_CurveArray_Count(m_ptr);
      if (count < 1)
        return new Curve[0];
      Curve[] rc = new Curve[count];
      for (int i = 0; i < count; i++)
      {
        IntPtr curve = UnsafeNativeMethods.ON_CurveArray_Get(m_ptr, i);
        if (IntPtr.Zero == curve)
          continue;
        rc[i] = GeometryBase.CreateGeometryHelper(curve, null) as Curve;
      }
      return rc;
    }

    /// <summary>
    /// Passively reclaims unmanaged resources when the class user did not explicitly call Dispose().
    /// </summary>
    ~SimpleArrayCurvePointer()
    {
      Dispose(false);
    }

    /// <summary>
    /// Actively reclaims unmanaged resources that this instance uses.
    /// </summary>
    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    /// <summary>
    /// For derived class implementers.
    /// <para>This method is called with argument true when class user calls Dispose(), while with argument false when
    /// the Garbage Collector invokes the finalizer, or Finalize() method.</para>
    /// <para>You must reclaim all used unmanaged resources in both cases, and can use this chance to call Dispose on disposable fields if the argument is true.</para>
    /// <para>Also, you must call the base virtual method within your overriding method.</para>
    /// </summary>
    /// <param name="disposing">true if the call comes from the Dispose() method; false if it comes from the Garbage Collector finalizer.</param>
    protected virtual void Dispose(bool disposing)
    {
      if (IntPtr.Zero != m_ptr)
      {
        UnsafeNativeMethods.ON_CurveArray_Delete(m_ptr);
        m_ptr = IntPtr.Zero;
      }
    }
  }

  /// <summary>
  /// Wrapper for a C++ ON_SimpleArray&lt;ON_Geometry*&gt;* or ON_SimpleArray&lt;const ON_Geometry*&gt;.
  /// If you are not writing C++ code, then this class is not for you.
  /// </summary>
  public class SimpleArrayGeometryPointer : IDisposable
  {
    IntPtr m_ptr; //ON_SimpleArray<ON_Geometry*>*

    /// <summary>
    /// Gets the const (immutable) pointer of this array.
    /// </summary>
    /// <returns>The const pointer.</returns>
    public IntPtr ConstPointer() { return m_ptr; }

    /// <summary>
    /// Gets the non-const pointer (for modification) of this array.
    /// </summary>
    /// <returns>The non-const pointer.</returns>
    public IntPtr NonConstPointer() { return m_ptr; }

    /// <summary>
    /// Initializes a new <see cref="SimpleArrayGeometryPointer"/> instance.
    /// </summary>
    public SimpleArrayGeometryPointer()
    {
      m_ptr = UnsafeNativeMethods.ON_GeometryArray_New(0);
    }

    /// <summary>
    /// Create an ON_SimpleArray&lt;ON_Geometry*&gt; filled with items in geometry
    /// </summary>
    /// <param name="geometry"></param>
    public SimpleArrayGeometryPointer(System.Collections.Generic.IEnumerable<GeometryBase> geometry)
    {
      m_ptr = UnsafeNativeMethods.ON_GeometryArray_New(0);

      foreach (GeometryBase gb in geometry)
      {
        IntPtr geomPtr = gb.ConstPointer();
        UnsafeNativeMethods.ON_GeometryArray_Append(m_ptr, geomPtr);
      }
    }

    /// <summary>
    /// Expects all of the items in the IEnumerable to be GeometryBase types
    /// </summary>
    /// <param name="geometry"></param>
    public SimpleArrayGeometryPointer(System.Collections.IEnumerable geometry)
    {
      m_ptr = UnsafeNativeMethods.ON_GeometryArray_New(0);

      foreach (object o in geometry)
      {
        GeometryBase gb = o as GeometryBase;
        if (gb != null)
        {
          IntPtr geomPtr = gb.ConstPointer();
          UnsafeNativeMethods.ON_GeometryArray_Append(m_ptr, geomPtr);
        }
      }
    }

    /// <summary>
    /// Copies the unmanaged array to a managed counterpart.
    /// </summary>
    /// <returns>The managed array.</returns>
    public GeometryBase[] ToNonConstArray()
    {
      int count = UnsafeNativeMethods.ON_GeometryArray_Count(m_ptr);
      GeometryBase[] rc = new GeometryBase[count];
      for (int i = 0; i < count; i++)
      {
        IntPtr pGeometry = UnsafeNativeMethods.ON_GeometryArray_Get(m_ptr, i);
        rc[i] = GeometryBase.CreateGeometryHelper(pGeometry, null);
      }
      return rc;
    }

    /// <summary>
    /// Passively reclaims unmanaged resources when the class user did not explicitly call Dispose().
    /// </summary>
    ~SimpleArrayGeometryPointer()
    {
      Dispose(false);
    }

    /// <summary>
    /// Actively reclaims unmanaged resources that this instance uses.
    /// </summary>
    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    /// <summary>
    /// For derived class implementers.
    /// <para>This method is called with argument true when class user calls Dispose(), while with argument false when
    /// the Garbage Collector invokes the finalizer, or Finalize() method.</para>
    /// <para>You must reclaim all used unmanaged resources in both cases, and can use this chance to call Dispose on disposable fields if the argument is true.</para>
    /// <para>Also, you must call the base virtual method within your overriding method.</para>
    /// </summary>
    /// <param name="disposing">true if the call comes from the Dispose() method; false if it comes from the Garbage Collector finalizer.</param>
    protected virtual void Dispose(bool disposing)
    {
      if (IntPtr.Zero != m_ptr)
      {
        UnsafeNativeMethods.ON_GeometryArray_Delete(m_ptr);
        m_ptr = IntPtr.Zero;
      }
    }
  }

  /// <summary>
  /// Represents a wrapper to an unmanaged array of mesh pointers.
  /// <para>Wrapper for a C++ ON_SimpleArray of ON_Mesh* or const ON_Mesh*. If you are not
  /// writing C++ code then this class is not for you.</para>
  /// </summary>
  public class SimpleArrayMeshPointer : IDisposable
  {
    internal bool DontDelete { get; set; }
    IntPtr m_ptr; // ON_SimpleArray<ON_Mesh*>*

    /// <summary>
    /// Gets the const (immutable) pointer of this array.
    /// </summary>
    /// <returns>The const pointer.</returns>
    public IntPtr ConstPointer() { return m_ptr; }

    /// <summary>
    /// Gets the non-const pointer (for modification) of this array.
    /// </summary>
    /// <returns>The non-const pointer.</returns>
    public IntPtr NonConstPointer() { return m_ptr; }

    /// <summary>
    /// Initializes a new <see cref="SimpleArrayMeshPointer"/> instance.
    /// </summary>
    public SimpleArrayMeshPointer()
    {
      m_ptr = UnsafeNativeMethods.ON_MeshArray_New();
    }

    internal SimpleArrayMeshPointer(IntPtr ptr)
    {
      m_ptr = ptr;
      DontDelete = true;
    }

    /// <summary>
    /// Gets the amount of meshes in this array.
    /// </summary>
    public int Count
    {
      get
      {
        IntPtr ptr = ConstPointer();
        int count = UnsafeNativeMethods.ON_MeshArray_Count(ptr);
        return count;
      }
    }

    /// <summary>
    /// Adds a mesh to the list.
    /// </summary>
    /// <param name="mesh">A mesh to add.</param>
    /// <param name="asConst">Whether this mesh should be treated as non-modifiable.</param>
    public void Add(Geometry.Mesh mesh, bool asConst)
    {
      if (null != mesh)
      {
        IntPtr pMesh = mesh.ConstPointer();
        if (!asConst)
          pMesh = mesh.NonConstPointer();
        IntPtr ptr = NonConstPointer();
        UnsafeNativeMethods.ON_MeshArray_Append(ptr, pMesh);
      }
    }
    
    /// <summary>
    /// Passively reclaims unmanaged resources when the class user did not explicitly call Dispose().
    /// </summary>
    ~SimpleArrayMeshPointer()
    {
      Dispose(false);
    }

    /// <summary>
    /// Actively reclaims unmanaged resources that this instance uses.
    /// </summary>
    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="disposing"></param>
    protected virtual void Dispose(bool disposing)
    {
      if (IntPtr.Zero != m_ptr)
      {
        if(!DontDelete) UnsafeNativeMethods.ON_MeshArray_Delete(m_ptr);
        m_ptr = IntPtr.Zero;
      }
    }

    /// <summary>
    /// Copies the unmanaged array to a managed counterpart.
    /// </summary>
    /// <returns>The managed array.</returns>
    public Geometry.Mesh[] ToNonConstArray()
    {
      int count = Count;
      if (count < 1)
        return new Geometry.Mesh[0];
      IntPtr ptr = ConstPointer();
      Mesh[] rc = new Mesh[count];
      for (int i = 0; i < count; i++)
      {
        IntPtr pMesh = UnsafeNativeMethods.ON_MeshArray_Get(ptr, i);
        if (IntPtr.Zero != pMesh)
          rc[i] = new Mesh(pMesh, null);
      }
      return rc;
    }

#if RHINO_SDK
    internal Geometry.Mesh[] ToConstArray(Rhino.DocObjects.RhinoObject parent)
    {
      int count = Count;
      if (count < 1)
        return new Geometry.Mesh[0];
      IntPtr ptr = ConstPointer();

      Mesh[] rc = new Mesh[count];
      for (int i = 0; i < rc.Length; i++)
      {
        IntPtr pMesh = UnsafeNativeMethods.ON_MeshArray_Get(ptr, i);
        Rhino.DocObjects.ObjRef objref = new DocObjects.ObjRef(parent, pMesh);
        rc[i] = objref.Mesh();
      }
      return rc;
    }
#endif
  }

  /// <summary>
  /// Wrapper for a C++ ON_SimpleArray&lt;ON_Brep*&gt; or ON_SimpleArray&lt;const ON_Brep*&gt;
  /// If you are not writing C++ code then this class is not for you.
  /// </summary>
  public class SimpleArrayBrepPointer : IDisposable
  {
    IntPtr m_ptr; // ON_SimpleArray<ON_Brep*>*

    /// <summary>
    /// Gets the const (immutable) pointer of this array.
    /// </summary>
    /// <returns>The const pointer.</returns>
    public IntPtr ConstPointer() { return m_ptr; }

    /// <summary>
    /// Gets the non-const pointer (for modification) of this array.
    /// </summary>
    /// <returns>The non-const pointer.</returns>
    public IntPtr NonConstPointer() { return m_ptr; }

    /// <summary>
    /// Initializes a new <see cref="SimpleArrayBrepPointer"/> instance.
    /// </summary>
    public SimpleArrayBrepPointer()
    {
      m_ptr = UnsafeNativeMethods.ON_BrepArray_New();
    }

    /// <summary>
    /// Gets the amount of breps in this array.
    /// </summary>
    public int Count
    {
      get
      {
        IntPtr ptr = ConstPointer();
        int count = UnsafeNativeMethods.ON_BrepArray_Count(ptr);
        return count;
      }
    }

    /// <summary>
    /// Adds a brep to the list.
    /// </summary>
    /// <param name="brep">A brep to add.</param>
    /// <param name="asConst">Whether this brep should be treated as non-modifiable.</param>
    public void Add(Geometry.Brep brep, bool asConst)
    {
      if (null != brep)
      {
        IntPtr pBrep = brep.ConstPointer();
        if (!asConst)
          pBrep = brep.NonConstPointer();
        IntPtr ptr = NonConstPointer();
        UnsafeNativeMethods.ON_BrepArray_Append(ptr, pBrep);
      }
    }

    /// <summary>
    /// Passively reclaims unmanaged resources when the class user did not explicitly call Dispose().
    /// </summary>
    ~SimpleArrayBrepPointer()
    {
      Dispose(false);
    }

    /// <summary>
    /// Actively reclaims unmanaged resources that this instance uses.
    /// </summary>
    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="disposing"></param>
    protected virtual void Dispose(bool disposing)
    {
      if (IntPtr.Zero != m_ptr)
      {
        UnsafeNativeMethods.ON_BrepArray_Delete(m_ptr);
        m_ptr = IntPtr.Zero;
      }
    }

    /// <summary>
    /// Copies the unmanaged array to a managed counterpart.
    /// </summary>
    /// <returns>The managed array.</returns>
    public Geometry.Brep[] ToNonConstArray()
    {
      int count = Count;
      if (count < 1)
        return new Geometry.Brep[0]; //MSDN guidelines prefer empty arrays
      IntPtr ptr = ConstPointer();
      Brep[] rc = new Brep[count];
      for (int i = 0; i < count; i++)
      {
        IntPtr pBrep = UnsafeNativeMethods.ON_BrepArray_Get(ptr, i);
        if (IntPtr.Zero != pBrep)
          rc[i] = new Brep(pBrep, null);
      }
      return rc;
    }
  }


  /// <summary>
  /// Wrapper for a C++ ON_SimpleArray&lt;ON_Linetype*&gt;
  /// If you are not writing C++ code then this class is not for you.
  /// </summary>
  public class SimpleArrayLinetypePointer : IDisposable
  {
    IntPtr m_ptr; // ON_SimpleArray<ON_Linetype*>*

    /// <summary>
    /// Gets the const (immutable) pointer of this array.
    /// </summary>
    /// <returns>The const pointer.</returns>
    public IntPtr ConstPointer() { return m_ptr; }

    /// <summary>
    /// Gets the non-const pointer (for modification) of this array.
    /// </summary>
    /// <returns>The non-const pointer.</returns>
    public IntPtr NonConstPointer() { return m_ptr; }

    /// <summary>
    /// Initializes a new <see cref="SimpleArrayLinetypePointer"/> instance.
    /// </summary>
    public SimpleArrayLinetypePointer()
    {
      m_ptr = UnsafeNativeMethods.ON_LinetypeArray_New();
    }

    /// <summary>
    /// Gets the amount of linetypes in this array.
    /// </summary>
    public int Count
    {
      get
      {
        IntPtr ptr = ConstPointer();
        int count = UnsafeNativeMethods.ON_LinetypeArray_Count(ptr);
        return count;
      }
    }

    /// <summary>
    /// Passively reclaims unmanaged resources when the class user did not explicitly call Dispose().
    /// </summary>
    ~SimpleArrayLinetypePointer()
    {
      Dispose(false);
    }

    /// <summary>
    /// Actively reclaims unmanaged resources that this instance uses.
    /// </summary>
    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Dispose
    /// </summary>
    /// <param name="disposing"></param>
    protected virtual void Dispose(bool disposing)
    {
      if (IntPtr.Zero != m_ptr)
      {
        UnsafeNativeMethods.ON_LinetypeArray_Delete(m_ptr);
        m_ptr = IntPtr.Zero;
      }
    }

    /// <summary>
    /// Copies the unmanaged array to a managed counterpart.
    /// </summary>
    /// <returns>The managed array.</returns>
    public DocObjects.Linetype[] ToNonConstArray()
    {
      int count = Count;
      if (count < 1)
        return new DocObjects.Linetype[0]; //MSDN guidelines prefer empty arrays
      IntPtr ptr = ConstPointer();
      DocObjects.Linetype[] rc = new DocObjects.Linetype[count];
      for (int i = 0; i < count; i++)
      {
        IntPtr pLinetype = UnsafeNativeMethods.ON_Linetype_Get(ptr, i);
        if (IntPtr.Zero != pLinetype)
          rc[i] = new DocObjects.Linetype(pLinetype);
      }
      return rc;
    }
  }
  

  /// <summary>
  /// Wrapper for a C++ ON_SimpleArray&lt;ON_Extrusion*&gt; or ON_SimpleArray&lt;const ON_Extrusion*&gt;
  /// If you are not writing C++ code then this class is not for you.
  /// </summary>
  public class SimpleArrayExtrusionPointer : IDisposable
  {
    IntPtr m_ptr; // ON_SimpleArray<ON_Extrusion*>*

    /// <summary>
    /// Gets the const (immutable) pointer of this array.
    /// </summary>
    /// <returns>The const pointer.</returns>
    public IntPtr ConstPointer() { return m_ptr; }

    /// <summary>
    /// Gets the non-const pointer (for modification) of this array.
    /// </summary>
    /// <returns>The non-const pointer.</returns>
    public IntPtr NonConstPointer() { return m_ptr; }

    /// <summary>
    /// Initializes a new <see cref="SimpleArrayExtrusionPointer"/> instance.
    /// </summary>
    public SimpleArrayExtrusionPointer()
    {
      m_ptr = UnsafeNativeMethods.ON_ExtrusionArray_New();
    }

    /// <summary>
    /// Gets the amount of Extrusions in this array.
    /// </summary>
    public int Count
    {
      get
      {
        IntPtr ptr = ConstPointer();
        int count = UnsafeNativeMethods.ON_ExtrusionArray_Count(ptr);
        return count;
      }
    }

    /// <summary>
    /// Adds a extrusion to the list.
    /// </summary>
    /// <param name="extrusion">A extrusion to add.</param>
    /// <param name="asConst">Whether this extrusion should be treated as non-modifiable.</param>
    public void Add(Geometry.Extrusion extrusion, bool asConst)
    {
      if (null != extrusion)
      {
        IntPtr pExtrusion = extrusion.ConstPointer();
        if (!asConst)
          pExtrusion = extrusion.NonConstPointer();
        IntPtr ptr = NonConstPointer();
        UnsafeNativeMethods.ON_ExtrusionArray_Append(ptr, pExtrusion);
      }
    }

    /// <summary>
    /// Passively reclaims unmanaged resources when the class user did not explicitly call Dispose().
    /// </summary>
    ~SimpleArrayExtrusionPointer()
    {
      Dispose(false);
    }

    /// <summary>
    /// Actively reclaims unmanaged resources that this instance uses.
    /// </summary>
    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="disposing"></param>
    protected virtual void Dispose(bool disposing)
    {
      if (IntPtr.Zero != m_ptr)
      {
        UnsafeNativeMethods.ON_ExtrusionArray_Delete(m_ptr);
        m_ptr = IntPtr.Zero;
      }
    }

    /// <summary>
    /// Copies the unmanaged array to a managed counterpart.
    /// </summary>
    /// <returns>The managed array.</returns>
    public Geometry.Extrusion[] ToNonConstArray()
    {
      int count = Count;
      if (count < 1)
        return new Geometry.Extrusion[0]; //MSDN guidelines prefer empty arrays
      IntPtr ptr = ConstPointer();
      Extrusion[] rc = new Extrusion[count];
      for (int i = 0; i < count; i++)
      {
        IntPtr pExtrusion = UnsafeNativeMethods.ON_ExtrusionArray_Get(ptr, i);
        if (IntPtr.Zero != pExtrusion)
          rc[i] = new Extrusion(pExtrusion, null);
      }
      return rc;
    }
  }

  /// <summary>
  /// Wrapper for ON_SimpleArray&lt;ON_BinaryArchive&gt;. If you are not writing C++ code
  /// then this class is not for you.
  /// </summary>
  public class SimpleArrayBinaryArchiveReader : IDisposable
  {
    IntPtr m_ptr; // ON_SimpleArray<ON_BinaryArchive>
    bool delete;

    /// <summary>
    /// Gets the const (immutable) pointer of this array.
    /// </summary>
    /// <returns>The const pointer.</returns>
    public IntPtr ConstPointer() { return m_ptr; }

    /// <summary>
    /// Gets the non-const pointer (for modification) of this array.
    /// </summary>
    /// <returns>The non-const pointer.</returns>
    public IntPtr NonConstPointer() { return m_ptr; }

    /// <summary>
    /// Initializes a new <see cref="SimpleArrayBinaryArchiveReader"/> class.
    /// </summary>
    public SimpleArrayBinaryArchiveReader()
    {
      m_ptr = UnsafeNativeMethods.ON_BinaryArchiveArray_New();
      delete = true;
    }

    /// <summary>
    /// Initializes a new <see cref="SimpleArrayBinaryArchiveReader"/> class.
    /// </summary>
    public SimpleArrayBinaryArchiveReader(IntPtr p)
    {
      m_ptr = p;
      delete = false;
    }

    /// <summary>
    /// Adds a new <see cref="Interval"/> at the end of this array.
    /// </summary>
    public void Add(BinaryArchiveReader reader)
    {
      var non_const_ptr = NonConstPointer();
      IntPtr pReader = reader.NonConstPointer();
      UnsafeNativeMethods.ON_BinaryArchiveArray_Add(non_const_ptr, pReader);
    }

    /// <summary>
    /// Gets the amount of elements in this array.
    /// </summary>
    public int Count
    {
      get { return UnsafeNativeMethods.ON_BinaryArchiveArray_Count(m_ptr); }
    }

    /// <summary>
    /// Get the Guid at index
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public BinaryArchiveReader Get(int index)
    {
      IntPtr p = UnsafeNativeMethods.ON_BufferArray_Get(m_ptr, index);
      BinaryArchiveReader reader = new BinaryArchiveReader(p);
      return reader;
    }

    /// <summary>
    /// Passively reclaims unmanaged resources when the class user did not explicitly call Dispose().
    /// </summary>
    ~SimpleArrayBinaryArchiveReader()
    {
      InternalDispose();
    }

    /// <summary>
    /// Actively reclaims unmanaged resources that this instance uses.
    /// </summary>
    public void Dispose()
    {
      InternalDispose();
      GC.SuppressFinalize(this);
    }

    private void InternalDispose()
    {
      if (IntPtr.Zero != m_ptr)
      {
        if(delete)
          UnsafeNativeMethods.ON_BinaryArchiveArray_Delete(m_ptr);

        m_ptr = IntPtr.Zero;
      }
    }
}

  /// <summary>
  /// Wrapper for a C++ ON_ClassArray&lt;ON_wString&gt;
  /// If you are not writing C++ code then this class is not for you.
  /// </summary>
  public class ClassArrayString : IDisposable
  {
    IntPtr m_ptr; // ON_ClassArray<ON_wString>*

    /// <summary>Gets the const (immutable) pointer of this array.</summary>
    /// <returns>The const pointer.</returns>
    public IntPtr ConstPointer() { return m_ptr; }

    /// <summary>Gets the non-const pointer (for modification) of this array.</summary>
    /// <returns>The non-const pointer.</returns>
    public IntPtr NonConstPointer() { return m_ptr; }

    /// <summary>
    /// Initializes a new <see cref="ClassArrayString"/> instance.
    /// </summary>
    public ClassArrayString()
    {
      m_ptr = UnsafeNativeMethods.ON_StringArray_New();
    }

    /// <summary>
    /// Gets the number of strings in this array.
    /// </summary>
    public int Count
    {
      get
      {
        IntPtr const_pointer_this = ConstPointer();
        return UnsafeNativeMethods.ON_StringArray_Count(const_pointer_this);
      }
    }

    /// <summary>
    /// Adds a string to the list.
    /// </summary>
    /// <param name="s">A string to add.</param>
    public void Add(string s)
    {
      IntPtr ptr_this = NonConstPointer();
      UnsafeNativeMethods.ON_StringArray_Append(ptr_this, s);
    }

    /// <summary>
    /// Passively reclaims unmanaged resources when the class user did not explicitly call Dispose().
    /// </summary>
    ~ClassArrayString()
    {
      Dispose(false);
    }

    /// <summary>
    /// Actively reclaims unmanaged resources that this instance uses.
    /// </summary>
    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    /// <summary></summary>
    /// <param name="disposing"></param>
    protected virtual void Dispose(bool disposing)
    {
      if (IntPtr.Zero != m_ptr)
      {
        UnsafeNativeMethods.ON_StringArray_Delete(m_ptr);
        m_ptr = IntPtr.Zero;
      }
    }

    /// <summary>
    /// Copies the unmanaged array to a managed counterpart.
    /// </summary>
    /// <returns>The managed array.</returns>
    public string[] ToArray()
    {
      int count = Count;
      if (count < 1)
        return new string[0]; //MSDN guidelines prefer empty arrays

      IntPtr const_ptr_this = ConstPointer();
      string[] rc = new string[count];
      using(StringHolder sh = new StringHolder())
      {
        IntPtr ptr_holder = sh.NonConstPointer();
        for (int i = 0; i < count; i++)
        {
          UnsafeNativeMethods.ON_StringArray_Get(const_ptr_this, i, ptr_holder);
          rc[i] = sh.ToString();
        }
      }
      return rc;
    }
  }

#if RHINO_SDK
  /// <summary>
  /// Represents a wrapper to an unmanaged "array" (list) of CRhinoObjRef instances.
  /// <para>Wrapper for a C++ ON_ClassArray of CRhinoObjRef</para>
  /// </summary>
  public sealed class ClassArrayObjRef : IDisposable
  {
    IntPtr m_ptr; // ON_ClassArray<CRhinoObjRef>*

    /// <summary>
    /// Gets the const (immutable) pointer of this array.
    /// </summary>
    /// <returns>The const pointer.</returns>
    public IntPtr ConstPointer() { return m_ptr; }

    /// <summary>
    /// Gets the non-const pointer (for modification) of this array.
    /// </summary>
    /// <returns>The non-const pointer.</returns>
    public IntPtr NonConstPointer() { return m_ptr; }

    /// <summary>
    /// Initializes a new <see cref="ClassArrayObjRef"/> instance.
    /// </summary>
    public ClassArrayObjRef()
    {
      m_ptr = UnsafeNativeMethods.ON_ClassArrayCRhinoObjRef_New();
    }

    /// <summary>
    /// Initializes a new instances from a set of ObjRefs
    /// </summary>
    /// <param name="objrefs">An array, a list or any enumerable set of Rhino object references.</param>
    public ClassArrayObjRef(System.Collections.Generic.IEnumerable<Rhino.DocObjects.ObjRef> objrefs)
    {
      m_ptr = UnsafeNativeMethods.ON_ClassArrayCRhinoObjRef_New();
      foreach (var objref in objrefs)
      {
        Add(objref);
      }
    }

    /// <summary>
    /// Gets the number of CRhinoObjRef instances in this array.
    /// </summary>
    public int Count
    {
      get
      {
        IntPtr ptr = ConstPointer();
        return UnsafeNativeMethods.ON_ClassArrayCRhinoObjRef_Count(ptr);
      }
    }

    /// <summary>
    /// Adds an ObjRef to the list.
    /// </summary>
    /// <param name="objref">An ObjRef to add.</param>
    public void Add(Rhino.DocObjects.ObjRef objref)
    {
      if (null != objref)
      {
        IntPtr pConstObjRef = objref.ConstPointer();
        IntPtr pThis = NonConstPointer();
        UnsafeNativeMethods.ON_ClassArrayCRhinoObjRef_Append(pThis, pConstObjRef);
      }
    }
    
    /// <summary>
    /// Passively reclaims unmanaged resources when the class user did not explicitly call Dispose().
    /// </summary>
    ~ClassArrayObjRef()
    {
      Dispose(false);
    }

    /// <summary>
    /// Actively reclaims unmanaged resources that this instance uses.
    /// </summary>
    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    void Dispose(bool disposing)
    {
      if (IntPtr.Zero != m_ptr)
      {
        UnsafeNativeMethods.ON_ClassArrayCRhinoObjRef_Delete(m_ptr);
        m_ptr = IntPtr.Zero;
      }
    }

    /// <summary>
    /// Copies the unmanaged array to a managed counterpart.
    /// </summary>
    /// <returns>The managed array.</returns>
    public Rhino.DocObjects.ObjRef[] ToNonConstArray()
    {
      int count = Count;
      if (count < 1)
        return new DocObjects.ObjRef[0];
      IntPtr ptr = ConstPointer();

      Rhino.DocObjects.ObjRef[] rc = new DocObjects.ObjRef[count];
      for (int i = 0; i < count; i++)
      {
        IntPtr pObjRef = UnsafeNativeMethods.ON_ClassArrayCRhinoObjRef_Get(ptr, i);
        if (IntPtr.Zero != pObjRef)
          rc[i] = new DocObjects.ObjRef(pObjRef);
      }
      return rc;
    }
  }


  /// <summary>
  /// Represents a wrapper to an unmanaged "array" (list) of ON_ObjRef instances.
  /// <para>Wrapper for a C++ ON_ClassArray of ON_ObjRef</para>
  /// </summary>
  public sealed class ClassArrayOnObjRef : IDisposable
  {
    IntPtr m_ptr; // ON_ClassArray<ON_ObjRef>*

    /// <summary>
    /// Gets the const (immutable) pointer of this array.
    /// </summary>
    /// <returns>The const pointer.</returns>
    public IntPtr ConstPointer() { return m_ptr; }

    /// <summary>
    /// Gets the non-const pointer (for modification) of this array.
    /// </summary>
    /// <returns>The non-const pointer.</returns>
    public IntPtr NonConstPointer() { return m_ptr; }

    /// <summary>
    /// Initializes a new <see cref="ClassArrayOnObjRef"/> instance.
    /// </summary>
    public ClassArrayOnObjRef()
    {
      m_ptr = UnsafeNativeMethods.ON_ClassArrayON_ObjRef_New();
    }

    /// <summary>
    /// Initializes a new instances from a set of ObjRefs
    /// </summary>
    /// <param name="objrefs">An array, a list or any enumerable set of Rhino object references.</param>
    public ClassArrayOnObjRef(System.Collections.Generic.IEnumerable<Rhino.DocObjects.ObjRef> objrefs)
    {
      m_ptr = UnsafeNativeMethods.ON_ClassArrayON_ObjRef_New();
      foreach (var objref in objrefs)
      {
        Add(objref);
      }
    }

    /// <summary>
    /// Gets the number of ObjRef instances in this array.
    /// </summary>
    public int Count
    {
      get
      {
        IntPtr ptr = ConstPointer();
        return UnsafeNativeMethods.ON_ClassArrayON_ObjRef_Count(ptr);
      }
    }

    /// <summary>
    /// Adds an ObjRef to the list.
    /// </summary>
    /// <param name="objref">An ObjRef to add.</param>
    public void Add(DocObjects.ObjRef objref)
    {
      if (null != objref)
      {
        IntPtr ptr_const_objref = objref.ConstPointer();
        IntPtr ptr_this = NonConstPointer();
        UnsafeNativeMethods.ON_ClassArrayON_ObjRef_Append(ptr_this, ptr_const_objref);
      }
    }

    /// <summary>
    /// Passively reclaims unmanaged resources when the class user did not explicitly call Dispose().
    /// </summary>
    ~ClassArrayOnObjRef()
    {
      Dispose(false);
    }

    /// <summary>
    /// Actively reclaims unmanaged resources that this instance uses.
    /// </summary>
    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    void Dispose(bool disposing)
    {
      if (IntPtr.Zero != m_ptr)
      {
        UnsafeNativeMethods.ON_ClassArrayON_ObjRef_Delete(m_ptr);
        m_ptr = IntPtr.Zero;
      }
    }

    /// <summary>
    /// Copies the unmanaged array to a managed counterpart.
    /// </summary>
    /// <returns>The managed array.</returns>
    public DocObjects.ObjRef[] ToNonConstArray()
    {
      int count = Count;
      if (count < 1)
        return new DocObjects.ObjRef[0];
      IntPtr ptr_const_this = ConstPointer();

      DocObjects.ObjRef[] rc = new DocObjects.ObjRef[count];
      for (int i = 0; i < count; i++)
      {
        IntPtr ptr_const_objref = UnsafeNativeMethods.ON_ClassArrayON_ObjRef_Get(ptr_const_this, i);
        if (IntPtr.Zero != ptr_const_objref)
          rc[i] = new DocObjects.ObjRef(ptr_const_objref, false);
      }
      return rc;
    }
  }
#endif

  internal static class ArrayOfTArrayMarshal
  {
    /// <summary>
    /// Helper method to construct the marshal.
    /// </summary>
    /// <param name="arrayOfArray">This parameter can be null. Then, the marshaler will act accordingly.</param>
    /// <param name="nestedFirstLevelNulls">Behavior for null nested items.</param>
    public static ArrayOfTArrayMarshal<T> Create<T>(IEnumerable<IEnumerable<T>> arrayOfArray, NullItemsResponse nestedFirstLevelNulls)
    {
      return new ArrayOfTArrayMarshal<T>(arrayOfArray, nestedFirstLevelNulls);
    }
  }

  internal enum NullItemsResponse
  {
    Accept,
    RemoveNulls,
    Throw,
  }

  /// <summary>
  /// Allows to marshal an array of arrays without copying the nested arrays to unmanaged memory and without additional calls.
  /// </summary>
  /// <typeparam name="T">The type to be marshaled.</typeparam>
  internal sealed class ArrayOfTArrayMarshal<T> : IDisposable
  {
    private readonly GCHandle[] m_gc_handles;

    private readonly int[] m_counts;
    private readonly int m_total_count;

    private readonly IntPtr m_array;

    private bool m_disposed;

    /// <summary>
    /// Constructs.
    /// </summary>
    /// <param name="arrayOfArray">This parameter can be null. Then, the marshaler will act accordingly.</param>
    /// <param name="nestedFirstLevelNulls">Behavior for null nested items.</param>
    public ArrayOfTArrayMarshal(IEnumerable<IEnumerable<T>> arrayOfArray, NullItemsResponse nestedFirstLevelNulls)
    {
      if (arrayOfArray == null) return;

      var tmp_array = Collections.RhinoListHelpers.GetConstArray(arrayOfArray, out m_total_count);

      var inners = new Collections.RhinoList<T[]>(m_total_count);
      var inners_counts = new Collections.RhinoList<int>(m_total_count);

      for (var i = 0; i < m_total_count; i++)
      {
        if (tmp_array[i] != null || nestedFirstLevelNulls == NullItemsResponse.Accept)
        {
          int inner_count;
          var inner = Collections.RhinoListHelpers.GetConstArray(tmp_array[i], out inner_count);
          inners.Add(inner);
          inners_counts.Add(inner_count);
        }
        else if (nestedFirstLevelNulls == NullItemsResponse.Throw)
          throw new ArgumentException("While enumerating input items, found null.");
      }

      T[][] objs_as_array_of_array = inners.m_items;
      m_counts = inners_counts.m_items;

      m_total_count = inners.Count;
      if (m_total_count == 0) return;

      m_gc_handles = new GCHandle[m_total_count];
      for (int i = 0; i < m_total_count; i++)
        m_gc_handles[i] = GCHandle.Alloc(objs_as_array_of_array[i], GCHandleType.Pinned);

      m_array = Marshal.AllocHGlobal(IntPtr.Size * m_gc_handles.Length);

      for (int i = 0; i < m_gc_handles.Length; i++)
        Marshal.WriteIntPtr(m_array, IntPtr.Size * i, m_gc_handles[i].AddrOfPinnedObject());
    }

    public IntPtr AddressesOfPinnedObjects
    {
      get
      {
        if (m_disposed) throw new ObjectDisposedException("this", "the object is disposed.");

        return m_array;
      }
    }

    public int[] LengthsOfPinnedObjects
    {
      get
      {
        if (m_disposed) throw new ObjectDisposedException("this", "the object is disposed.");

        return m_counts;
      }
    }

    public int Length
    {
      get
      {
        if (m_disposed) throw new ObjectDisposedException("this", "the object is disposed.");

        return m_total_count;
      }
    }

    private void Dispose(bool disposing)
    {
      if (m_disposed) return;

      if (m_gc_handles != null)
        foreach (var handle in m_gc_handles)
          handle.Free();

      if (m_array != IntPtr.Zero) Marshal.FreeHGlobal(m_array);

      m_disposed = true;
    }

    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    ~ArrayOfTArrayMarshal()
    {
      Dispose(false);
    }
  }

#if RHINO_SDK
	/// <summary>
	/// ON_SimpleArray of CRhinoClippingPlaneObject*
	/// </summary>
  public class SimpleArrayClippingPlaneObjectPointer : IDisposable
  {
    IntPtr m_ptr; // ON_SimpleArray<CRhinoClippingPlaneObject*>*

    /// <summary>
    /// Gets the const (immutable) pointer of this array.
    /// </summary>
    /// <returns>The const pointer.</returns>
    public IntPtr ConstPointer() { return m_ptr; }

    /// <summary>
    /// Gets the non-const pointer (for modification) of this array.
    /// </summary>
    /// <returns>The non-const pointer.</returns>
    public IntPtr NonConstPointer() { return m_ptr; }

    /// <summary>
    /// Initializes a new <see cref="SimpleArrayClippingPlaneObjectPointer"/> instance.
    /// </summary>
    public SimpleArrayClippingPlaneObjectPointer()
    {
      m_ptr = UnsafeNativeMethods.CRhinoClippingPlaneObjectArray_New();
    }

    /// <summary>
    /// Gets the amount of clipping planes in this array.
    /// </summary>
    public int Count
    {
      get
      {
        IntPtr ptr = ConstPointer();
        int count = UnsafeNativeMethods.CRhinoClippingPlaneObjectArray_Count(ptr);
        return count;
      }
    }

    /// <summary>
    /// Adds a clipping plane to the list.
    /// </summary>
    /// <param name="clippingplane">A clipping plane to add.</param>
    /// <param name="asConst">Whether this clipping plane should be treated as non-modifiable.</param>
    public void Add(DocObjects.ClippingPlaneObject clippingplane, bool asConst)
    {
      if (null != clippingplane)
      {
        IntPtr pClippingPlaneObject = clippingplane.ConstPointer();
        if (!asConst)
          pClippingPlaneObject = clippingplane.NonConstPointer();
        IntPtr ptr = NonConstPointer();
        UnsafeNativeMethods.CRhinoClippingPlaneObjectArray_Append(ptr, pClippingPlaneObject);
      }
    }

    /// <summary>
    /// Passively reclaims unmanaged resources when the class user did not explicitly call Dispose().
    /// </summary>
    ~SimpleArrayClippingPlaneObjectPointer()
    {
      Dispose(false);
    }

    /// <summary>
    /// Actively reclaims unmanaged resources that this instance uses.
    /// </summary>
    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="disposing"></param>
    protected virtual void Dispose(bool disposing)
    {
      if (IntPtr.Zero != m_ptr)
      {
        UnsafeNativeMethods.CRhinoClippingPlaneObjectArray_Delete(m_ptr);
        m_ptr = IntPtr.Zero;
      }
    }
  }
#endif
}