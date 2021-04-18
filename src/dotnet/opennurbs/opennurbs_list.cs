using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading;

using Rhino.Geometry;

namespace Rhino.Collections
{
  interface IRhinoTable<T>
  {
    int Count { get; }
    T this[int index] { get; }
  }

  /// <summary>
  /// Provides the ability to resize a generic list by setting the Count property.
  /// </summary>
  /// <typeparam name="T"></typeparam>
  public interface IResizableList<T> : IList<T>
  {
    /// <summary>
    /// Gets or sets the length of the list.
    /// This hides (Shadows in Vb.Net) the read-only Count property of the generic list.
    /// </summary>
    new int Count { get; set; }
  }

  class TableEnumerator<TABLE, TABLE_TYPE> : IEnumerator<TABLE_TYPE> where TABLE : IRhinoTable<TABLE_TYPE>
  {
    readonly TABLE m_table;
    int m_position = -1;
    readonly int m_count;
    public TableEnumerator(TABLE table)
    {
      m_table = table;
      m_count = table.Count;
    }

    public TABLE_TYPE Current
    {
      get
      {
        if (m_position < 0 || m_position >= m_count)
          throw new InvalidOperationException();
        return m_table[m_position];
      }
    }

    void IDisposable.Dispose() { }

    object System.Collections.IEnumerator.Current
    {
      get
      {
        if (m_position < 0 || m_position >= m_count)
          throw new InvalidOperationException();
        return m_table[m_position];
      }
    }

    public bool MoveNext()
    {
      m_position++;
      // Skip null records
      if (m_position < m_count)
      {
        var value = m_table[m_position];
        while (null == value && m_position < m_count)
          if (++m_position < m_count)
            value = m_table[m_position];
      }
      return (m_position < m_count);
    }

    public void Reset()
    {
      m_position = -1;
    }

  }

  internal static class RhinoListHelpers
  {
    /// <summary>
    /// Anything calling this function should not be modifying the contents of the array.
    /// </summary>
    /// <param name="items">A list, an array or any enumerable set of anything.</param>
    /// <param name="count">The resulting count of elements.</param>
    /// <returns>An array.</returns>
    internal static T[] GetConstArray<T>(IEnumerable<T> items, out int count)
    {
      if (items == null)
      {
        count = 0;
        return null;
      }

      {
        var pointlist = items as RhinoList<T>;
        if (null != pointlist)
        {
          count = pointlist.m_size;
          return pointlist.m_items;
        }
      }

      {
        var pointarray = items as T[];
        if (null != pointarray)
        {
          count = pointarray.Length;
          return pointarray;
        }
      }

      // couldn't figure out what this thing is, just use the iterator
      // note: we copy here once less than before (especially with List<T> as input), and 
      // are more general because we support ICollection<T>, an ancestor of IList<T>.
      var list = new RhinoList<T>(items);
      count = list.Count;
      return list.m_items;
    }
  }

  internal static class GenericIListImplementation
  {
    /// <summary>
    /// Provides a generic way to find an index in a Rhino table. O(n) where n is length of table.
    /// table needs to provide Count and indexer as backing accessors.
    /// This is to provide a full and reasonable implementation of the interface. People should choose other methods if better behavior is required.
    /// </summary>
    internal static int IndexOf<T>(IList<T> table, T item)
      where T : IEquatable<T>
    {
      var count = table.Count;
      if (item == null)
      {
        for (int i = 0; i < count; i++)
        {
          if (table[i] == null) return i;
        }
      }
      else
      {
        for (int i = 0; i < count; i++)
        {
          if (item.Equals(table[i])) return i;
        }
      }
      return -1;
    }

    /// <summary>
    /// Provides a generic way to find an index in a Rhino table. O(n) where n is length of table.
    /// table needs to provide Count and indexer as backing accessors.
    /// This is to provide a full and reasonable implementation of the interface. People should choose other methods if better behavior is required.
    /// </summary>
    internal static int IndexOf(IList<Point3f> table, object value)
    {
      Point3f item;
      if (!HelperTryCoerce(value, out item)) return -1;
      return IndexOf(table, item);
    }

    /// <summary>
    /// Provides a generic way to find an index in a Rhino table. O(n) where n is length of table.
    /// table needs to provide Count and indexer as backing accessors.
    /// This is to provide a full and reasonable implementation of the interface. People should choose other methods if better behavior is required.
    /// </summary>
    internal static int IndexOf(IList<Vector3f> table, object value)
    {
      Vector3f item;
      if (!HelperTryCoerce(value, out item)) return -1;
      return IndexOf(table, item);
    }

    /// <summary>
    /// Provides a generic way to insert at an index in a Rhino table. table needs to provide Count and indexer as backing accessors.
    /// This is to provide a full and reasonable implementation of the interface. People should choose other methods if better behavior is required.
    /// </summary>
    internal static void Insert<T>(IResizableList<T> table, int index, T item)
    {
      if (index > table.Count || index < 0)
        throw new ArgumentOutOfRangeException("index");

      table.Count++;

      for (int p = index, i = index + 1; i < table.Count; p++, i++)
      {
        table[i] = table[p];
      }

      table[index] = item;
    }

    /// <summary>
    /// Provides a generic way to remove from a list at an index in a Rhino table. table needs to provide Count and indexer as backing accessors.
    /// This is to provide a full and reasonable implementation of the interface. People should choose other methods if better behavior is required.
    /// </summary>
    internal static void RemoveAt<T>(IResizableList<T> table, int index)
    {
      if (index >= table.Count || index < 0)
        throw new ArgumentOutOfRangeException("index");

      while (++index < table.Count)
      {
        table[index - 1] = table[index];
      }

      table.Count--;
    }

    /// <summary>
    /// Provides a generic way to copy to an array starting at an index. table needs to provide Count and indexer as backing accessors.
    /// This is to provide a full and reasonable implementation of the interface. People should choose other methods if better behavior is required.
    /// </summary>
    internal static void CopyTo<T>(IList<T> table, T[] to, int index)
    {
      if ((index + table.Count) > to.Length || index < 0)
        throw new ArgumentOutOfRangeException("index");

      int i = -1;
      while (index < to.Length)
      {
        to[index++] = table[++i];
      }
    }

    /// <summary>
    /// Provides a non-generic way to copy to an array starting at an index. table needs to provide Count and indexer as backing accessors.
    /// This is to provide a full and reasonable implementation of the interface. People should choose other methods if better behavior is required.
    /// </summary>
    internal static void CopyTo<T>(IList<T> table, Array to, int index)
    {
      if ((index + table.Count) > to.Length || index < 0)
        throw new ArgumentOutOfRangeException("index");

      int i = -1;
      while (index < to.Length)
      {
        to.SetValue(table[++i], index++);
      }
    }

    /// <summary>
    /// Provides a non-generic way to copy to an array starting at an index. table needs to provide Count and indexer as backing accessors.
    /// This is to provide a full and reasonable implementation of the interface. People should choose other methods if better behavior is required.
    /// </summary>
    internal static void CopyToFromReadOnlyCollection<T>(IReadOnlyCollection<T> table, T[] to, int index)
    {
      if ((index + table.Count) > to.Length || index < 0)
        throw new ArgumentOutOfRangeException("index");

      foreach (var element in table)
      {
        to[index++] = element;
      }
    }

    /// <summary>
    /// Provides a generic way to remove the first occurrence of an item in a Rhino table. table needs to provide Count and indexer as backing accessors.
    /// This is to provide a full and reasonable implementation of the interface. People should choose other methods if better behavior is required.
    /// </summary>
    internal static bool Remove<T>(IResizableList<T> table, T item)
      where T : IEquatable<T>
    {
      var index = IndexOf(table, item);

      bool found = index != -1;
      if (found)
        RemoveAt(table, index);

      return found;
    }

    internal static bool Contains<T>(IList<T> table, T value)
      where T : IEquatable<T>
    {
      return IndexOf(table, value) != -1;
    }

    internal static bool Contains(IList<Point3f> table, object value)
    {
      return IndexOf(table, value) != -1;
    }

    internal static bool Contains(IList<Vector3f> table, object value)
    {
      return IndexOf(table, value) != -1;
    }

    /// <summary>
    /// Forms a reverse list with all items, then removed them using Remove().
    /// </summary>
    internal static void ClearByItems<TList, T>(TList table)
      where TList : IList<T>
    {
      var rev_item_list = new List<T>(table.Count);
      for (int i = table.Count - 1; i == 0; i--)
      {
        T item = table[i];
        rev_item_list.Add(item);
      }
      for (int i = 0; i < rev_item_list.Count; i++)
      {
        table.Remove(rev_item_list[i]);
      }
    }

    /// <summary>
    /// Forms a reverse list with all items, then removed them using Remove().
    /// </summary>
    internal static void ClearByItems<T>(ICollection<T> table)
    {
      var rev_item_list = new T[table.Count];

      {
        int i = table.Count - 1;
        foreach (var item in table)
        {
          rev_item_list[i] = item;
          i--;
        }
      }
      for (int i = 0; i < rev_item_list.Length; i++)
      {
        table.Remove(rev_item_list[i]);
      }
    }

    /// <summary>
    ///Removes the last index, then the previous one, etc
    /// </summary>
    internal static void ClearReverseOrder<TList, T>(TList table)
      where TList : IList<T>
    {
      for (int i = table.Count - 1; i == 0; i--)
      {
        table.RemoveAt(i);
      }
    }

    /// <summary>
    /// Converts any object that is deemed suitable to Point3f.
    /// </summary>
    internal static bool HelperTryCoerce(object item, out Point3f result)
    {
      if (item is Point3f)
      {
        result = (Point3f)item;
        return true;
      }
      if (item is Point3d)
      {
        result = (Point3f)(Point3d)item;
        return true;
      }

      var point_item = (item as Point);
      if (point_item != null)
      {
        result = (Point3f)(point_item).Location;
        return true;
      }
      result = default(Point3f);
      return false;
    }

    /// <summary>
    /// Converts any object that is deemed suitable to Vector3f.
    /// </summary>
    internal static bool HelperTryCoerce(object item, out Vector3f result)
    {
      // Giulio - we could unify Point3f and Vector3d with __refvalue(__makeref(value), Vector3f) = (Vector3f)item;
      // see http://stackoverflow.com/questions/4764573/why-is-typedreference-behind-the-scenes-its-so-fast-and-safe-almost-magical
      if (item is Vector3f)
      {
        result = (Vector3f)item;
        return true;
      }
      if (item is Vector3d)
      {
        result = (Vector3f)(Vector3d)item;
        return true;
      }
      result = default(Vector3f);
      return false;
    }

    /// <summary>
    /// Converts any object that is deemed suitable to Vector2f.
    /// </summary>
    internal static bool HelperTryCoerce(object item, out Point2f result)
    {
      if (item is Point2f)
      {
        result = (Point2f)item;
        return true;
      }
      if (item is Point2d)
      {
        result = (Point2f)(Point2d)item;
        return true;
      }
      result = default(Point2f);
      return false;
    }

    /// <summary>
    /// Converts any object that is deemed suitable to Point3f, or raises an exception.
    /// </summary>
    internal static Point3f HelperCoercePoint(object item)
    {
      Point3f result;
      if (HelperTryCoerce(item, out result))
        return result;
      throw new ArgumentException("The object needs to be of type Point3f, Point3d, or Point.", "item");
    }

    /// <summary>
    /// Converts any object that is deemed suitable to Point2f, or raises an exception.
    /// </summary>
    internal static Point2f HelperCoercePoint2(object item)
    {
      Point2f result;
      if (HelperTryCoerce(item, out result))
        return result;
      throw new ArgumentException("The object needs to be of type Point2f or Point2d.", "item");
    }

    /// <summary>
    /// Converts any object that is deemed suitable to Vector3f, or raises an exception.
    /// </summary>
    internal static Vector3f HelperCoerceVector(object item)
    {
      Vector3f result;
      if (HelperTryCoerce(item, out result))
        return result;
      throw new ArgumentException("The object needs to be of type Vector3f or Vector3d.", "item");
    }

    internal static Exception MakeReadOnlyException(object obj)
    {
      return new NotSupportedException(string.Format("'{0}' is read-only.", obj.GetType().Name));
    }

    internal static Exception MakeNoSyncException(object obj)
    {
      return new NotSupportedException(string.Format("Implicit synchronization is not supported in '{0}' objects.", obj.GetType().Name));
    }
  }


  /// <summary>
  /// Represents a list of generic data. This class is similar to System.Collections.Generic.List(T) 
  /// but exposes a few more methods.
  /// </summary>
  [Serializable,
  DebuggerTypeProxy(typeof(ListDebuggerDisplayProxy<>)),
  DebuggerDisplay("Count = {Count}")]
  public class RhinoList<T> : IList<T>, IList, ICloneable
  {
    #region Fields

    /// <summary>
    /// Internal array of items. The array will contain trailing invalid items if Capacity > Count. 
    /// WARNING! Do not store a reference to this array anywhere! The List class may decide to replace 
    /// the internal array with another one.
    /// </summary>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    internal T[] m_items;

    /// <summary>
    /// The number of "valid" elements in m_items (same as m_count in ON_SimpleArray)
    /// </summary>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    internal int m_size;

    [NonSerialized]
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private object m_syncRoot;

    /// <summary>
    /// The version counter is incremented whenever a change is made to the list.
    /// </summary>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private int m_version;
    #endregion

    #region Constructors
    /// <summary>
    /// Initializes a new, empty list.
    /// </summary>
    public RhinoList()
    {
      m_items = new T[0];
    }

    /// <summary>
    /// Initializes an empty list with a certain capacity.
    /// </summary>
    /// <param name="initialCapacity">Number of items this list can store without resizing.</param>
    public RhinoList(int initialCapacity)
    {
      if (initialCapacity < 0)
      {
        throw new ArgumentOutOfRangeException("initialCapacity", "RhinoList cannot be constructed with a negative capacity");
      }
      m_items = new T[initialCapacity];
    }

    /// <summary>
    /// Initializes a new list with a specified amount of values.
    /// </summary>
    /// <param name="amount">Number of values to add to this list. Must be equal to or larger than zero.</param>
    /// <param name="defaultValue">Value to add, for reference types, 
    /// the same item will be added over and over again.</param>
    public RhinoList(int amount, T defaultValue)
    {
      if (amount < 0) { throw new ArgumentOutOfRangeException("amount", "RhinoList cannot be constructed with a negative amount"); }
      if (amount == 0) { return; }

      m_items = new T[amount];
      m_size = amount;

      for (int i = 0; i < amount; i++)
      {
        m_items[i] = defaultValue;
      }
    }

    /// <summary>
    /// Initializes this list as a shallow duplicate of another list, array or any other enumerable set of T.
    /// </summary>
    /// <param name="collection">Collection of items to duplicate.</param>
    public RhinoList(IEnumerable<T> collection)
    {
      if (collection == null)
      {
        throw new ArgumentNullException("collection");
      }

      ICollection<T> is2 = collection as ICollection<T>;
      if (is2 != null)
      {
        int count = is2.Count;
        m_items = new T[count];

        is2.CopyTo(m_items, 0);
        m_size = count;
      }
      else
      {
        m_size = 0;
        m_items = new T[4];
        using (IEnumerator<T> enumerator = collection.GetEnumerator())
        {
          while (enumerator.MoveNext())
          {
            Add(enumerator.Current);
          }
        }
      }
    }

    /// <summary>
    /// Initializes an new list by shallow duplicating another list.
    /// </summary>
    /// <param name="list">List to mimic.</param>
    public RhinoList(RhinoList<T> list)
    {
      if (list == null) { throw new ArgumentNullException("list"); }

      // initialize items array at same capacity
      m_items = new T[list.Capacity];

      if (list.m_size > 0)
      {
        Array.Copy(list.m_items, m_items, list.m_items.Length);
        m_size = list.m_size;
      }
    }

    /// <summary>
    /// Constructs an array that contains all items in this list.
    /// </summary>
    /// <returns>An array containing all items in this list.</returns>
    public T[] ToArray()
    {
      T[] destinationArray = new T[m_size];
      Array.Copy(m_items, 0, destinationArray, 0, m_size);
      return destinationArray;
    }
    #endregion

    #region Properties
    private void EnsureCapacity(int min)
    {
      if (m_items.Length < min)
      {
        int num = (m_items.Length == 0) ? 4 : (m_items.Length * 2);
        if (num < min)
        {
          num = min;
        }
        Capacity = num;
      }
    }

    /// <summary>
    /// Sets the capacity to the actual number of elements in the List, 
    /// if that number is less than a threshold value.
    /// </summary>
    /// <remarks>This function differs from the DotNET implementation of List&lt;T&gt; 
    /// since that one only trims the excess if the excess exceeds 10% of the list length.</remarks>
    [DebuggerStepThrough]
    public void TrimExcess()
    {
      Capacity = m_size;
    }

    /// <summary>
    /// Gets or sets the total number of elements the internal data structure can hold without resizing.
    /// </summary>
    public int Capacity
    {
      [DebuggerStepThrough]
      get
      {
        return m_items.Length;
      }
      [DebuggerStepThrough]
      set
      {
        if (value != m_items.Length)
        {
          if (value < m_size)
            throw new ArgumentOutOfRangeException("value", "Capacity must be larger than or equal to the list Count");

          if (value > 0)
          {
            T[] destinationArray = new T[value];
            if (m_size > 0)
            {
              Array.Copy(m_items, 0, destinationArray, 0, m_size);
            }
            m_items = destinationArray;
          }
          else
          {
            m_items = new T[0];
          }
        }
      }
    }

    /// <summary>
    /// Gets the number of elements actually contained in the List.
    /// </summary>
    public int Count
    {
      [DebuggerStepThrough]
      get
      {
        return m_size;
      }
    }

    /// <summary>
    /// Gets the number of null references (Nothing in Visual Basic) in this list. 
    /// If T is a ValueType, this property always return zero.
    /// </summary>
    public int NullCount
    {
      [DebuggerStepThrough]
      get
      {
        Type Tt = typeof(T);
        if (Tt.IsValueType) { return 0; }

        int N = 0;
        for (int i = 0; i < m_size; i++)
        {
          if (m_items[i] == null) { N++; }
        }

        return N;
      }
    }

    /// <summary>
    /// Gets or sets the element at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index of the element to get or set.</param>
    /// <returns>The element at the specified index.</returns>
    public T this[int index]
    {
      [DebuggerStepThrough]
      get
      {
        // IronPython seems to expect IndexOutOfRangeExceptions with
        // indexing properties
        if (index >= m_size) { throw new IndexOutOfRangeException("index"); }
        return m_items[index];
      }
      [DebuggerStepThrough]
      set
      {
        if (index >= m_size) { throw new IndexOutOfRangeException("You cannot set items which do not yet exist, consider using Insert or Add instead."); }

        m_items[index] = value;
        m_version++;
      }
    }

    /// <summary>
    /// Gets or sets the element at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index of the element to get or set.</param>
    /// <returns>The element at the specified index.</returns>
    object IList.this[int index]
    {
      [DebuggerStepThrough]
      get
      {
        return this[index];
      }
      [DebuggerStepThrough]
      set
      {
        RhinoList<T>.VerifyValueType(value);
        this[index] = (T)value;
      }
    }

    /// <summary>
    /// Gets or sets the first item in the list. This is synonymous to calling List[0].
    /// </summary>
    public T First
    {
      [DebuggerStepThrough]
      get { return this[0]; }
      [DebuggerStepThrough]
      set { this[0] = value; }
    }

    /// <summary>
    /// Gets or sets the last item in the list. This is synonymous to calling List[Count-1].
    /// </summary>
    public T Last
    {
      [DebuggerStepThrough]
      get { return this[m_size - 1]; }
      [DebuggerStepThrough]
      set { this[m_size - 1] = value; }
    }

    /// <summary>
    /// Remap an index in the infinite range onto the List index range.
    /// </summary>
    /// <param name="index">Index to remap.</param>
    /// <returns>Remapped index.</returns>
    [DebuggerStepThrough]
    public int RemapIndex(int index)
    {
      int c = index % (m_size - 1);
      if (c < 0) { c = (m_size - 1) + c; }
      return c;
    }

    /// <summary>
    /// When implemented by a class, gets a value indicating whether the IList is read-only. 
    /// RhinoList is never ReadOnly.
    /// </summary>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    bool IList.IsReadOnly { get { return false; } }

    /// <summary>
    /// When implemented by a class, gets a value indicating whether the IList has a fixed size. 
    /// RhinoList is never fixed.
    /// </summary>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    bool IList.IsFixedSize { get { return false; } }

    /// <summary>
    /// When implemented by a class, gets a value indicating whether the IList is read-only. 
    /// RhinoList is never ReadOnly.
    /// </summary>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    bool ICollection<T>.IsReadOnly { get { return false; } }

    /// <summary>
    /// When implemented by a class, gets a value indicating whether access to the ICollection is synchronized (thread-safe).
    /// ON_List is never Synchronized.
    /// </summary>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    bool ICollection.IsSynchronized { get { return false; } }

    /// <summary>
    /// Gets an object that can be used to synchronize access to the ICollection.
    /// </summary>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    object ICollection.SyncRoot
    {
      get
      {
        if (m_syncRoot == null)
        {
          Interlocked.CompareExchange(ref m_syncRoot, new object(), null);
        }
        return m_syncRoot;
      }
    }
    #endregion

    #region Methods
    #region Addition and Removal

    /// <summary>
    /// Removes all elements from the List.
    /// </summary>
    public void Clear()
    {
      if (m_size == 0) { return; }

      Array.Clear(m_items, 0, m_size);
      m_size = 0;
      m_version++;
    }

    /// <summary>
    /// Adds an item to the IList.
    /// </summary>
    /// <param name="item">The Object to add to the IList.</param>
    /// <returns>The position into which the new element was inserted.</returns>
    int IList.Add(object item)
    {
      RhinoList<T>.VerifyValueType(item);
      Add((T)item);
      return (Count - 1);
    }
    private static void VerifyValueType(object value)
    {
      if (!RhinoList<T>.IsCompatibleObject(value))
      {
        throw new ArgumentException("value is not a supported type");
      }
    }
    private static bool IsCompatibleObject(object value)
    {
      if (!(value is T) && ((value != null) || typeof(T).IsValueType))
      {
        return false;
      }
      return true;
    }

    /// <summary>
    /// Adds an object to the end of the List.
    /// </summary>
    /// <param name="item">Item to append.</param>
    public void Add(T item)
    {
      if (m_size == m_items.Length)
      {
        EnsureCapacity(m_size + 1);
      }

      m_items[m_size++] = item;
      m_version++;
    }

    /// <summary>
    /// Adds the elements of the specified collection to the end of the List.
    /// </summary>
    /// <param name="collection">The collection whose elements should be added to the end of the List. 
    /// The collection itself cannot be a null reference (Nothing in Visual Basic), 
    /// but it can contain elements that are a null reference (Nothing in Visual Basic), 
    /// if type T is a reference type.
    /// </param>
    public void AddRange(IEnumerable<T> collection)
    {
      InsertRange(m_size, collection);
    }

    /// <summary>
    /// Adds the elements of the specified collection to the end of the List.
    /// </summary>
    /// <param name="collection">The collection whose elements should be added to the end of the List. 
    /// The collection itself cannot be a null reference (Nothing in Visual Basic), 
    /// but it can contain elements that are a null reference (Nothing in Visual Basic). 
    /// Objects in collection which cannot be represented as T will throw exceptions.
    /// </param>
    public void AddRange(IEnumerable collection)
    {
      Type Tt = typeof(T);

      foreach (object obj in collection)
      {
        if (obj == null)
        {
          Add(default(T));
          continue;
        }

        if (Tt.IsAssignableFrom(obj.GetType()))
        {
          Add((T)obj);
        }
        else
        {
          string local_type = Tt.Name;
          string import_type = obj.GetType().Name;
          string msg = string.Format(System.Globalization.CultureInfo.InvariantCulture,
            "You cannot add an object of type {0} to a list of type {1}",
            import_type,
            local_type);
          throw new InvalidCastException(msg);
        }
      }
    }

    /// <summary>
    /// Inserts an element into the List at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index at which item should be inserted.</param>
    /// <param name="item">The object to insert. The value can be a null reference 
    /// (Nothing in Visual Basic) for reference types.</param>
    void IList.Insert(int index, object item)
    {
      RhinoList<T>.VerifyValueType(item);
      Insert(index, (T)item);
    }

    /// <summary>
    /// Inserts an element into the List at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index at which item should be inserted.</param>
    /// <param name="item">The object to insert. The value can be a null reference 
    /// (Nothing in Visual Basic) for reference types.</param>
    public void Insert(int index, T item)
    {
      if (index > m_size)
      {
        throw new ArgumentOutOfRangeException("index");
      }

      if (m_size == m_items.Length)
      {
        EnsureCapacity(m_size + 1);
      }

      if (index < m_size)
      {
        Array.Copy(m_items, index, m_items, index + 1, m_size - index);
      }

      m_items[index] = item;
      m_size++;
      m_version++;
    }

    /// <summary>
    /// Inserts the elements of a collection into the List at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index at which the new elements should be inserted.</param>
    /// <param name="collection">The collection whose elements should be inserted into the List. 
    /// The collection itself cannot be a null reference (Nothing in Visual Basic), 
    /// but it can contain elements that are a null reference (Nothing in Visual Basic), 
    /// if type T is a reference type.</param>
    public void InsertRange(int index, IEnumerable<T> collection)
    {
      if (collection == null)
      {
        throw new ArgumentNullException("collection");
      }

      if (index > m_size)
      {
        throw new ArgumentOutOfRangeException("index");
      }

      ICollection<T> is2 = collection as ICollection<T>;
      if (is2 != null)
      {
        int count = is2.Count;
        if (count > 0)
        {
          EnsureCapacity(m_size + count);
          if (index < m_size)
          {
            Array.Copy(m_items, index, m_items, index + count, m_size - index);
          }
          if (this == is2)
          {
            Array.Copy(m_items, 0, m_items, index, index);
            Array.Copy(m_items, (index + count), m_items, (index * 2), (m_size - index));
          }
          else
          {
            T[] array = new T[count];
            is2.CopyTo(array, 0);
            array.CopyTo(m_items, index);
          }
          m_size += count;
        }
      }
      else
      {
        using (IEnumerator<T> enumerator = collection.GetEnumerator())
        {
          while (enumerator.MoveNext())
          {
            Insert(index++, enumerator.Current);
          }
        }
      }
      m_version++;
    }

    /// <summary>
    /// Removes the first occurrence of a specific object from the List.
    /// </summary>
    /// <param name="item">The object to remove from the List. 
    /// The value can be a null reference (Nothing in Visual Basic) for reference types.</param>
    void IList.Remove(object item)
    {
      if (RhinoList<T>.IsCompatibleObject(item))
      {
        Remove((T)item);
      }
    }

    /// <summary>
    /// Removes the first occurrence of a specific object from the List.
    /// </summary>
    /// <param name="item">The object to remove from the List. 
    /// The value can be a null reference (Nothing in Visual Basic) for reference types.</param>
    /// <returns>true if item is successfully removed; otherwise, false. 
    /// This method also returns false if item was not found in the List.</returns>
    public bool Remove(T item)
    {
      int index = IndexOf(item);
      if (index >= 0)
      {
        RemoveAt(index);
        return true;
      }
      return false;
    }

    /// <summary>
    /// Removes the all the elements that match the conditions defined by the specified predicate.
    /// </summary>
    /// <param name="match">The Predicate(T) delegate that defines the conditions of the elements to remove.</param>
    /// <returns>The number of elements removed from the List.</returns>
    public int RemoveAll(Predicate<T> match)
    {
      if (match == null)
      {
        throw new ArgumentNullException("match");
      }

      int index = 0;
      while ((index < m_size) && !match(m_items[index]))
      {
        index++;
      }

      if (index >= m_size)
      {
        return 0;
      }

      int num2 = index + 1;
      while (num2 < m_size)
      {
        while ((num2 < m_size) && match(m_items[num2]))
        {
          num2++;
        }
        if (num2 < m_size)
        {
          m_items[index++] = m_items[num2++];
        }
      }

      Array.Clear(m_items, index, m_size - index);
      int num3 = m_size - index;
      m_size = index;
      m_version++;

      return num3;
    }

    /// <summary>
    /// Removes all elements from the List that are null references (Nothing in Visual Basic). 
    /// This function will not do anything if T is not a Reference type.
    /// </summary>
    /// <returns>The number of nulls removed from the List.</returns>
    public int RemoveNulls()
    {
      Type Tt = typeof(T);
      if (Tt.IsValueType) { return 0; }
      return RemoveAll(ON_Null_Predicate);
    }
    internal bool ON_Null_Predicate(T val) { return val == null; }

    /// <summary>
    /// Removes the element at the specified index of the List.
    /// </summary>
    /// <param name="index">The zero-based index of the element to remove.</param>
    public void RemoveAt(int index)
    {
      if (index >= m_size)
      {
        throw new ArgumentOutOfRangeException("index");
      }

      m_size--;
      if (index < m_size)
      {
        Array.Copy(m_items, index + 1, m_items, index, m_size - index);
      }
      m_items[m_size] = default(T);
      m_version++;
    }

    /// <summary>
    /// Removes a range of elements from the List.
    /// </summary>
    /// <param name="index">The zero-based starting index of the range of elements to remove.</param>
    /// <param name="count">The number of elements to remove.</param>
    public void RemoveRange(int index, int count)
    {
      if (index < 0) { throw new ArgumentOutOfRangeException("index"); }
      if (count < 0) { throw new ArgumentOutOfRangeException("count"); }

      if ((m_size - index) < count)
      {
        throw new ArgumentException("This combination of index and count is not valid");
      }

      if (count > 0)
      {
        m_size -= count;
        if (index < m_size)
        {
          Array.Copy(m_items, index + count, m_items, index, m_size - index);
        }
        Array.Clear(m_items, m_size, count);
        m_version++;
      }
    }

    /// <summary>
    /// Constructs a shallow copy of a range of elements in the source List.
    /// </summary>
    /// <param name="index">The zero-based List index at which the range starts.</param>
    /// <param name="count">The number of elements in the range.</param>
    /// <returns>A shallow copy of a range of elements in the source List.</returns>
    public RhinoList<T> GetRange(int index, int count)
    {
      if ((index < 0) || (count < 0))
        throw new ArgumentOutOfRangeException("index");

      if ((m_size - index) < count)
        throw new ArgumentOutOfRangeException("index");

      RhinoList<T> list = new RhinoList<T>(count);
      Array.Copy(m_items, index, list.m_items, 0, count);
      list.m_size = count;
      return list;
    }
    #endregion

    #region Searching
    /// <summary>
    /// Searches for the specified object and returns the zero-based index of the 
    /// first occurrence within the entire List.
    /// </summary>
    /// <param name="item">The object to locate in the List. 
    /// The value can be a null reference (Nothing in Visual Basic) 
    /// for reference types.</param>
    /// <returns>The zero-based index of the first occurrence of item within 
    /// the entire List, if found; otherwise, -1.</returns>
    int IList.IndexOf(object item)
    {
      if (RhinoList<T>.IsCompatibleObject(item))
      {
        return IndexOf((T)item);
      }
      return -1;
    }

    /// <summary>
    /// Searches for the specified object and returns the zero-based index of the 
    /// first occurrence within the entire List.
    /// </summary>
    /// <param name="item">The object to locate in the List. 
    /// The value can be a null reference (Nothing in Visual Basic) 
    /// for reference types.</param>
    /// <returns>The zero-based index of the first occurrence of item within 
    /// the entire List, if found; otherwise, -1.</returns>
    public int IndexOf(T item)
    {
      return Array.IndexOf(m_items, item, 0, m_size);
    }

    /// <summary>
    /// Searches for the specified object and returns the zero-based index of 
    /// the first occurrence within the range of elements in the List that 
    /// extends from the specified index to the last element.
    /// </summary>
    /// <param name="item">The object to locate in the List. 
    /// The value can be a null reference (Nothing in Visual Basic) 
    /// for reference types.</param>
    /// <param name="index">The zero-based starting index of the search.</param>
    /// <returns>The zero-based index of the first occurrence of item within 
    /// the entire List, if found; otherwise, -1.</returns>
    public int IndexOf(T item, int index)
    {
      if (index > m_size) { throw new ArgumentOutOfRangeException("index"); }
      return Array.IndexOf(m_items, item, index, m_size - index);
    }

    /// <summary>
    /// Searches for the specified object and returns the zero-based index of the first 
    /// occurrence within the range of elements in the List that starts at the specified 
    /// index and contains the specified number of elements.
    /// </summary>
    /// <param name="item">The object to locate in the List. 
    /// The value can be a null reference (Nothing in Visual Basic) 
    /// for reference types.</param>
    /// <param name="index">The zero-based starting index of the search.</param>
    /// <param name="count">The number of elements in the section to search.</param>
    /// <returns>The zero-based index of the first occurrence of item within 
    /// the entire List, if found; otherwise, -1.</returns>
    public int IndexOf(T item, int index, int count)
    {
      if (index > m_size) { throw new ArgumentOutOfRangeException("index"); }
      if ((count < 0) || (index > (m_size - count)))
      {
        throw new ArgumentOutOfRangeException("count");
      }

      return Array.IndexOf(m_items, item, index, count);
    }

    /// <summary>
    /// Searches for the specified object and returns the zero-based 
    /// index of the last occurrence within the entire List.
    /// </summary>
    /// <param name="item">The object to locate in the List. 
    /// The value can be a null reference (Nothing in Visual Basic) for reference types.</param>
    /// <returns>The zero-based index of the last occurrence of item within 
    /// the entire the List, if found; otherwise, -1.</returns>
    public int LastIndexOf(T item)
    {
      return LastIndexOf(item, m_size - 1, m_size);
    }

    /// <summary>
    /// Searches for the specified object and returns the zero-based index 
    /// of the last occurrence within the range of elements in the List 
    /// that extends from the first element to the specified index.
    /// </summary>
    /// <param name="item">The object to locate in the List. 
    /// The value can be a null reference (Nothing in Visual Basic) for reference types.</param>
    /// <param name="index">The zero-based starting index of the backward search.</param>
    /// <returns>The zero-based index of the last occurrence of item within 
    /// the entire the List, if found; otherwise, -1.</returns>
    public int LastIndexOf(T item, int index)
    {
      if (index >= m_size) { throw new ArgumentOutOfRangeException("index"); }
      return LastIndexOf(item, index, index + 1);
    }

    /// <summary>
    /// Searches for the specified object and returns the zero-based index of the 
    /// last occurrence within the range of elements in the List that contains 
    /// the specified number of elements and ends at the specified index.
    /// </summary>
    /// <param name="item">The object to locate in the List. 
    /// The value can be a null reference (Nothing in Visual Basic) for reference types.</param>
    /// <param name="index">The zero-based starting index of the backward search.</param>
    /// <param name="count">The number of elements in the section to search.</param>
    /// <returns>The zero-based index of the last occurrence of item within 
    /// the entire the List, if found; otherwise, -1.</returns>
    public int LastIndexOf(T item, int index, int count)
    {
      if (m_size == 0) { return -1; }
      if ((index < 0) || (count < 0))
        throw new ArgumentOutOfRangeException("index");

      if ((index >= m_size) || (count > (index + 1)))
        throw new ArgumentOutOfRangeException("index");

      return Array.LastIndexOf(m_items, item, index, count);
    }

    /// <summary>
    /// Searches the entire sorted List for an element using the default comparer 
    /// and returns the zero-based index of the element.
    /// </summary>
    /// <param name="item">The object to locate. The value can be a null reference 
    /// (Nothing in Visual Basic) for reference types.</param>
    /// <returns>The zero-based index of item in the sorted List, if item is found; 
    /// otherwise, a negative number that is the bitwise complement of the index 
    /// of the next element that is larger than item or, if there is no larger element, 
    /// the bitwise complement of Count.</returns>
    public int BinarySearch(T item)
    {
      return BinarySearch(0, Count, item, null);
    }

    /// <summary>
    /// Searches the entire sorted List for an element using the specified 
    /// comparer and returns the zero-based index of the element.
    /// </summary>
    /// <param name="item">The object to locate. The value can be a null reference 
    /// (Nothing in Visual Basic) for reference types.</param>
    /// <param name="comparer">The IComparer(T) implementation to use when comparing elements.
    /// Or a null reference (Nothing in Visual Basic) to use the default comparer 
    /// Comparer(T)::Default.</param>
    /// <returns>The zero-based index of item in the sorted List, if item is found; 
    /// otherwise, a negative number that is the bitwise complement of the index 
    /// of the next element that is larger than item or, if there is no larger element, 
    /// the bitwise complement of Count.</returns>
    public int BinarySearch(T item, IComparer<T> comparer)
    {
      return BinarySearch(0, Count, item, comparer);
    }

    /// <summary>
    /// Searches the entire sorted List for an element using the specified 
    /// comparer and returns the zero-based index of the element.
    /// </summary>
    /// <param name="index">The zero-based starting index of the range to search.</param>
    /// <param name="count">The length of the range to search.</param>
    /// <param name="item">The object to locate. The value can be a null reference 
    /// (Nothing in Visual Basic) for reference types.</param>
    /// <param name="comparer">The IComparer(T) implementation to use when comparing elements.
    /// Or a null reference (Nothing in Visual Basic) to use the default comparer 
    /// Comparer(T)::Default.</param>
    /// <returns>The zero-based index of item in the sorted List, if item is found; 
    /// otherwise, a negative number that is the bitwise complement of the index 
    /// of the next element that is larger than item or, if there is no larger element, 
    /// the bitwise complement of Count.</returns>
    public int BinarySearch(int index, int count, T item, IComparer<T> comparer)
    {
      if (index < 0) { throw new ArgumentOutOfRangeException("index"); }
      if (count < 0) { throw new ArgumentOutOfRangeException("count"); }

      if ((m_size - index) < count)
      {
        throw new ArgumentException("This combination of index and count is not valid");
      }

      return Array.BinarySearch(m_items, index, count, item, comparer);
    }

    /// <summary>
    /// Determines whether an element is in the List.
    /// </summary>
    /// <param name="item">The object to locate in the List. 
    /// The value can be a null reference (Nothing in Visual Basic) for reference types.</param>
    /// <returns>true if item is found in the List; otherwise, false.</returns>
    public bool Contains(T item)
    {
      if (item == null)
      {
        for (int j = 0; j < m_size; j++)
        {
          if (m_items[j] == null)
          {
            return true;
          }
        }
        return false;
      }

      EqualityComparer<T> comparer = EqualityComparer<T>.Default;
      for (int i = 0; i < m_size; i++)
      {
        if (comparer.Equals(m_items[i], item))
        {
          return true;
        }
      }
      return false;
    }

    /// <summary>
    /// Determines whether an element is in the List.
    /// </summary>
    /// <param name="item">The object to locate in the List. 
    /// The value can be a null reference (Nothing in Visual Basic) for reference types.</param>
    /// <returns>true if item is found in the List; otherwise, false.</returns>
    bool IList.Contains(object item)
    {
      return (RhinoList<T>.IsCompatibleObject(item) && Contains((T)item));
    }

    /// <summary>
    /// Determines whether the List contains elements that match the 
    /// conditions defined by the specified predicate.
    /// </summary>
    /// <param name="match">The Predicate(T) delegate that defines the conditions of the elements to search for.</param>
    /// <returns>true if the List contains one or more elements that match the 
    /// conditions defined by the specified predicate; otherwise, false.</returns>
    public bool Exists(Predicate<T> match)
    {
      return (FindIndex(match) != -1);
    }

    /// <summary>
    /// Searches for an element that matches the conditions defined by the 
    /// specified predicate, and returns the first occurrence within the entire List.
    /// </summary>
    /// <param name="match">The Predicate(T) delegate that defines the conditions of the element to search for.</param>
    /// <returns>The first element that matches the conditions defined by the specified predicate, 
    /// if found; otherwise, the default value for type T.</returns>
    public T Find(Predicate<T> match)
    {
      if (match == null) { throw new ArgumentNullException("match"); }

      for (int i = 0; i < m_size; i++)
      {
        if (match(m_items[i]))
        {
          return m_items[i];
        }
      }
      return default(T);
    }

    /// <summary>
    /// Searches for an element that matches the conditions defined by the 
    /// specified predicate, and returns the last occurrence within the entire List.
    /// </summary>
    /// <param name="match">The Predicate(T) delegate that defines the conditions of the element to search for.</param>
    /// <returns>The last element that matches the conditions defined by the specified predicate, 
    /// if found; otherwise, the default value for type T.</returns>
    public T FindLast(Predicate<T> match)
    {
      if (match == null) { throw new ArgumentNullException("match"); }

      for (int i = m_size - 1; i >= 0; i--)
      {
        if (match(m_items[i]))
        {
          return m_items[i];
        }
      }
      return default(T);
    }

    /// <summary>
    /// Retrieves all the elements that match the conditions defined by the specified predicate.
    /// </summary>
    /// <param name="match">The Predicate(T) delegate that defines the conditions of the elements to search for.</param>
    /// <returns>A ON_List(T) containing all the elements that match the conditions 
    /// defined by the specified predicate, if found; otherwise, an empty ON_List(T).</returns>
    public RhinoList<T> FindAll(Predicate<T> match)
    {
      if (match == null) { throw new ArgumentNullException("match"); }

      RhinoList<T> list = new RhinoList<T>(m_size);
      for (int i = 0; i < m_size; i++)
      {
        if (match(m_items[i]))
        {
          list.Add(m_items[i]);
        }
      }

      list.TrimExcess();
      return list;
    }

    /// <summary>
    /// Determines whether every element in the List matches the conditions defined by the specified predicate.
    /// </summary>
    /// <param name="match">The Predicate(T) delegate that defines the conditions to check against the elements.</param>
    /// <returns>true if every element in the List matches the conditions defined by 
    /// the specified predicate; otherwise, false. If the list has no elements, the return value is true.</returns>
    public bool TrueForAll(Predicate<T> match)
    {
      if (match == null) { throw new ArgumentNullException("match"); }

      for (int i = 0; i < m_size; i++)
      {
        if (!match(m_items[i]))
        {
          return false;
        }
      }
      return true;
    }

    /// <summary>
    /// Performs the specified action on each element of the List.
    /// </summary>
    /// <param name="action">The Action(T) delegate to perform on each element of the List.</param>
    public void ForEach(Action<T> action)
    {
      if (action == null) { throw new ArgumentNullException("action"); }
      for (int i = 0; i < m_size; i++)
      {
        action(m_items[i]);
      }
    }

    /// <summary>
    /// Searches for an element that matches the conditions defined by the 
    /// specified predicate, and returns the zero-based index of the first 
    /// occurrence within the entire List.
    /// </summary>
    /// <param name="match">The Predicate(T) delegate that defines the conditions of the element to search for.</param>
    /// <returns>The zero-based index of the first occurrence of an element that 
    /// matches the conditions defined by match, if found; otherwise, -1.</returns>
    public int FindIndex(Predicate<T> match)
    {
      return FindIndex(0, m_size, match);
    }

    /// <summary>
    /// Searches for an element that matches the conditions defined by the 
    /// specified predicate, and returns the zero-based index of the first 
    /// occurrence within the entire List.
    /// </summary>
    /// <param name="startIndex">The zero-based starting index of the search.</param>
    /// <param name="match">The Predicate(T) delegate that defines the conditions of the element to search for.</param>
    /// <returns>The zero-based index of the first occurrence of an element that 
    /// matches the conditions defined by match, if found; otherwise, -1.</returns>
    public int FindIndex(int startIndex, Predicate<T> match)
    {
      return FindIndex(startIndex, m_size - startIndex, match);
    }

    /// <summary>
    /// Searches for an element that matches the conditions defined by the specified predicate, 
    /// and returns the zero-based index of the first occurrence within the range of elements 
    /// in the List that extends from the specified index to the last element.
    /// </summary>
    /// <param name="startIndex">The zero-based starting index of the search.</param>
    /// <param name="count">The number of elements in the section to search.</param>
    /// <param name="match">The Predicate(T) delegate that defines the conditions of the element to search for.</param>
    /// <returns>The zero-based index of the first occurrence of an element that 
    /// matches the conditions defined by match, if found; otherwise, -1.</returns>
    public int FindIndex(int startIndex, int count, Predicate<T> match)
    {
      if (startIndex > m_size) { throw new ArgumentOutOfRangeException("count"); }
      if (count < 0) { throw new ArgumentOutOfRangeException("count"); }
      if (startIndex > (m_size - count)) { throw new ArgumentOutOfRangeException("count"); }

      if (match == null) { throw new ArgumentNullException("match"); }

      int num = startIndex + count;
      for (int i = startIndex; i < num; i++)
      {
        if (match(m_items[i]))
        {
          return i;
        }
      }
      return -1;
    }

    /// <summary>
    /// Searches for an element that matches the conditions defined by the 
    /// specified predicate, and returns the zero-based index of the last 
    /// occurrence within the entire List.
    /// </summary>
    /// <param name="match">The Predicate(T) delegate that defines the conditions of the element to search for.</param>
    /// <returns>The zero-based index of the last occurrence of an element that matches 
    /// the conditions defined by match, if found; otherwise, -1.</returns>
    public int FindLastIndex(Predicate<T> match)
    {
      return FindLastIndex(m_size - 1, m_size, match);
    }

    /// <summary>
    /// Searches for an element that matches the conditions defined by the 
    /// specified predicate, and returns the zero-based index of the last 
    /// occurrence within the entire List.
    /// </summary>
    /// <param name="startIndex">The zero-based starting index of the backward search.</param>
    /// <param name="match">The Predicate(T) delegate that defines the conditions of the element to search for.</param>
    /// <returns>The zero-based index of the last occurrence of an element that matches 
    /// the conditions defined by match, if found; otherwise, -1.</returns>
    public int FindLastIndex(int startIndex, Predicate<T> match)
    {
      // avoid overflow
      if (startIndex == int.MaxValue)
        throw new ArgumentOutOfRangeException("startIndex", "startIndex must be less than Int32.MaxValue");

      return FindLastIndex(startIndex, startIndex + 1, match);
    }

    /// <summary>
    /// Searches for an element that matches the conditions defined by the 
    /// specified predicate, and returns the zero-based index of the last 
    /// occurrence within the entire List.
    /// </summary>
    /// <param name="startIndex">The zero-based starting index of the backward search.</param>
    /// <param name="count">The number of elements in the section to search.</param>
    /// <param name="match">The Predicate(T) delegate that defines the conditions of the element to search for.</param>
    /// <returns>The zero-based index of the last occurrence of an element that matches 
    /// the conditions defined by match, if found; otherwise, -1.</returns>
    public int FindLastIndex(int startIndex, int count, Predicate<T> match)
    {
      if (match == null) { throw new ArgumentNullException("match"); }
      if (m_size == 0)
      {
        if (startIndex != -1)
        {
          throw new ArgumentOutOfRangeException("startIndex");
        }
      }
      else if (startIndex >= m_size)
      {
        throw new ArgumentOutOfRangeException("startIndex");
      }

      if (count < 0) { throw new ArgumentOutOfRangeException("count"); }
      if (((startIndex - count) + 1) < 0) { throw new ArgumentOutOfRangeException("startIndex"); }

      int num = startIndex - count;
      for (int i = startIndex; i > num; i--)
      {
        if (match(m_items[i]))
        {
          return i;
        }
      }
      return -1;
    }
    #endregion

    #region Sorting
    /// <summary>
    /// Sorts the elements in the entire List using the default comparer.
    /// </summary>
    public void Sort()
    {
      Sort(0, Count, null);
    }

    /// <summary>
    /// Sorts the elements in the entire list using the specified System.Comparison(T)
    /// </summary>
    /// <param name="comparer">The IComparer(T) implementation to use when comparing elements, 
    /// or a null reference (Nothing in Visual Basic) to use the default comparer Comparer(T).Default.</param>
    public void Sort(IComparer<T> comparer)
    {
      Sort(0, Count, comparer);
    }

    /// <summary>
    /// Sorts the elements in the entire list using the specified comparer.
    /// </summary>
    /// <param name="comparison">The System.Comparison(T) to use when comparing elements.</param>
    public void Sort(Comparison<T> comparison)
    {
      if (comparison == null) { throw new ArgumentNullException("comparison"); }
      if (m_size > 0)
      {
        IComparer<T> comparer = new FunctorComparer<T>(comparison);
        Array.Sort(m_items, 0, m_size, comparer);
      }
    }

    /// <summary>
    /// Sorts the elements in a range of elements in list using the specified comparer.
    /// </summary>
    /// <param name="index">The zero-based starting index of the range to sort.</param>
    /// <param name="count">The length of the range to sort.</param>
    /// <param name="comparer">The IComparer(T) implementation to use when comparing 
    /// elements, or a null reference (Nothing in Visual Basic) to use the default 
    /// comparer Comparer(T).Default.</param>
    public void Sort(int index, int count, IComparer<T> comparer)
    {
      if ((index < 0) || (count < 0))
        throw new ArgumentOutOfRangeException("index");

      if ((m_size - index) < count)
        throw new ArgumentException("index and count are not a valid combination");

      Array.Sort(m_items, index, count, comparer);
      m_version++;
    }

    /// <summary>
    /// Sort this list based on a list of numeric keys of equal length. 
    /// The keys array will not be altered.
    /// </summary>
    /// <param name="keys">Numeric keys to sort with.</param>
    /// <remarks>This function does not exist on the DotNET List class. 
    /// David thought it was a good idea.</remarks>
    public void Sort(double[] keys)
    {
      if (keys == null) { throw new ArgumentNullException("keys"); }
      if (keys.Length != m_size)
      {
        throw new ArgumentException("Keys array must have same length as this List.");
      }

      //cannot sort 1 item or less...
      if (m_size < 2) { return; }

      //trim my internal array
      Capacity = m_size;

      //duplicate the keys array
      double[] copy_keys = (double[])keys.Clone();
      Array.Sort(copy_keys, m_items);

      m_version++;
    }

    /// <summary>
    /// Sort this list based on a list of numeric keys of equal length. 
    /// The keys array will not be altered.
    /// </summary>
    /// <param name="keys">Numeric keys to sort with.</param>
    /// <remarks>This function does not exist on the DotNET List class. 
    /// David thought it was a good idea.</remarks>
    public void Sort(int[] keys)
    {
      if (keys == null) { throw new ArgumentNullException("keys"); }
      if (keys.Length != m_size)
      {
        throw new ArgumentException("Keys array must have same length as this List.");
      }

      //cannot sort 1 item or less...
      if (m_size < 2) { return; }

      //trim my internal array
      Capacity = m_size;

      //duplicate the keys array
      int[] copy_keys = (int[])keys.Clone();
      Array.Sort(copy_keys, m_items);

      m_version++;
    }

    /// <summary>
    /// Utility class which ties together functionality in Comparer(T) and Comparison(T)
    /// </summary>
    private sealed class FunctorComparer<Q> : IComparer<Q>
    {
      //private Comparer<Q> c;
      private readonly Comparison<Q> m_comparison;

      public FunctorComparer(Comparison<Q> comparison)
      {
        //c = Comparer<Q>.Default;
        m_comparison = comparison;
      }

      public int Compare(Q x, Q y)
      {
        return m_comparison(x, y);
      }
    }

    /// <summary>
    /// Reverses the order of the elements in the entire List.
    /// </summary>
    public void Reverse()
    {
      Reverse(0, Count);
    }

    /// <summary>
    /// Reverses the order of the elements in the specified range.
    /// </summary>
    /// <param name="index">The zero-based starting index of the range to reverse.</param>
    /// <param name="count">The number of elements in the range to reverse.</param>
    public void Reverse(int index, int count)
    {
      if ((index < 0) || (count < 0))
        throw new ArgumentOutOfRangeException("index");

      if ((m_size - index) < count)
        throw new ArgumentOutOfRangeException("index");

      Array.Reverse(m_items, index, count);
      m_version++;
    }
    #endregion

    #region Duplication and Conversion
    /// <summary>
    /// Constructs a read-only wrapper of this class.
    /// </summary>
    /// <returns>A wrapper.</returns>
    public ReadOnlyCollection<T> AsReadOnly()
    {
      return new ReadOnlyCollection<T>(this);
    }

    /// <summary>
    /// Aggregates all results of a conversion function over this table into a new list.
    /// </summary>
    /// <typeparam name="TOutput">The type returned by the function.</typeparam>
    /// <param name="converter">A conversion function that can transform from T to TOutput.</param>
    /// <returns>The new list.</returns>
    public RhinoList<TOutput> ConvertAll<TOutput>(Converter<T, TOutput> converter)
    {
      if (converter == null) { throw new ArgumentNullException("converter"); }

      RhinoList<TOutput> list = new RhinoList<TOutput>(m_size);
      for (int i = 0; i < m_size; i++)
      {
        list.m_items[i] = converter(m_items[i]);
      }

      list.m_size = m_size;
      return list;
    }

    /// <summary>
    /// Copies the entire List to a compatible one-dimensional array, 
    /// starting at the beginning of the target array.
    /// </summary>
    /// <param name="array">The one-dimensional Array that is the destination 
    /// of the elements copied from List. The Array must have zero-based indexing.</param>
    public void CopyTo(T[] array)
    {
      CopyTo(array, 0);
    }

    /// <summary>
    /// Copies the entire List to a compatible one-dimensional array, 
    /// starting at the specified index of the target array.
    /// </summary>
    /// <param name="array">The one-dimensional Array that is the destination 
    /// of the elements copied from List. The Array must have zero-based indexing.</param>
    /// <param name="arrayIndex">The zero-based index in array at which copying begins.</param>
    public void CopyTo(T[] array, int arrayIndex)
    {
      Array.Copy(m_items, 0, array, arrayIndex, m_size);
    }

    /// <summary>
    /// Copies a range of elements from the List to a compatible one-dimensional array, 
    /// starting at the specified index of the target array.
    /// </summary>
    /// <param name="index">The zero-based index in the source List at which copying begins.</param>
    /// <param name="array">The one-dimensional Array that is the destination of the elements 
    /// copied from List. The Array must have zero-based indexing.</param>
    /// <param name="arrayIndex">The zero-based index in array at which copying begins.</param>
    /// <param name="count">The number of elements to copy.</param>
    public void CopyTo(int index, T[] array, int arrayIndex, int count)
    {
      if ((m_size - index) < count)
      {
        throw new ArgumentOutOfRangeException("index");
      }
      Array.Copy(m_items, index, array, arrayIndex, count);
    }

    /// <summary>
    /// Copies the elements of the ICollection to an Array, starting at a particular Array index.
    /// </summary>
    /// <param name="array">The one-dimensional Array that is the destination of the elements 
    /// copied from ICollection. The Array must have zero-based indexing.</param>
    /// <param name="arrayIndex">The zero-based index in array at which copying begins.</param>
    void ICollection.CopyTo(Array array, int arrayIndex)
    {
      if ((array != null) && (array.Rank != 1))
      {
        throw new ArgumentException("Multidimensional target arrays not supported");
      }
      try
      {
        Array.Copy(m_items, 0, array, arrayIndex, m_size);
      }
      catch (ArrayTypeMismatchException)
      {
        throw new ArgumentException("Invalid array type");
      }
    }
    #endregion

    object ICloneable.Clone()
    {
      return new RhinoList<T>(this);
    }

    /// <summary>
    /// Returns a shallow copy of this instance.
    /// If the generic type is comprised of only value types (struct, enum, ptr), then the result will be a deep copy.
    /// </summary>
    /// <returns>The duplicated list.</returns>
    public RhinoList<T> Duplicate()
    {
      return (RhinoList<T>)(this as ICloneable).Clone();
    }
    #endregion

    #region Enumeration
    /// <summary>
    /// Constructs an enumerator that is capable of iterating over all items in this list.
    /// </summary>
    /// <returns>The new enumerator.</returns>
    public IEnumerator<T> GetEnumerator()
    {
      return new Enumerator(this);
    }
    IEnumerator IEnumerable.GetEnumerator()
    {
      return GetEnumerator();
    }

    private class Enumerator : IEnumerator<T>
    {
      private readonly RhinoList<T> m_list;
      private int m_index;
      private readonly int m_version;
      private T m_current;

      internal Enumerator(RhinoList<T> list)
      {
        m_list = list;
        m_index = 0;
        m_version = list.m_version;
        m_current = default(T);
      }

      public void Dispose()
      {
        GC.SuppressFinalize(this);
      }

      public bool MoveNext()
      {
        RhinoList<T> list = m_list;
        if ((m_version == list.m_version) && (m_index < list.m_size))
        {
          m_current = list.m_items[m_index];
          m_index++;
          return true;
        }
        return MoveNextRare();
      }
      private bool MoveNextRare()
      {
        if (m_version != m_list.m_version)
        {
          throw new InvalidOperationException("State of RhinoList changed during enumeration");
        }

        m_index = m_list.m_size + 1;
        m_current = default(T);
        return false;
      }

      public T Current
      {
        get
        {
          return m_current;
        }
      }
      object IEnumerator.Current
      {
        get
        {
          if ((m_index == 0) || (m_index == (m_list.m_size + 1)))
          {
            throw new InvalidOperationException("Enum operation cannot happen");
          }
          return Current;
        }
      }
      void IEnumerator.Reset()
      {
        if (m_version != m_list.m_version)
        {
          throw new InvalidOperationException("State of RhinoList changed during enumeration");
        }
        m_index = 0;
        m_current = default(T);
      }
    }
    #endregion
  }

  /// <summary>
  /// Provides helper methods to work with <see cref="RhinoList&lt;T&gt;"/> and other collections.
  /// </summary>
  public static class RhinoList
  {
    /// <summary>
    /// Finds a certain amount of points in a list of 3D points that are the k-closest to a test point.
    /// This method searches needlePoints by computing all distances from each point cloud point and keeping a short list.
    /// </summary>
    /// <param name="pointcloud">A point cloud to be searched.</param>
    /// <param name="needlePoints">Points to search for.</param>
    /// <param name="amount">The required amount of closest neighbors to find.</param>
    /// <returns>An enumerable of arrays of indices; each array contains the indices for each of the needlePts.</returns>
    /// <seealso cref="Rhino.Geometry.RTree.PointCloudKNeighbors(PointCloud, IEnumerable{Point3d}, int)" />
    /// <since>6.0</since>
    public static IEnumerable<int[]> PointCloudKNeighbors(PointCloud pointcloud, IEnumerable<Point3d> needlePoints, int amount)
    {
      if (pointcloud == null) throw new ArgumentNullException(nameof(pointcloud));

      var points = pointcloud.AsReadOnlyListOfPoints();
      return KNeighbors(points, needlePoints, amount);
    }

    /// <summary>
    /// Finds a certain amour of points in a list of 3D points that are the k-closest to a test point.
    /// This method searches needlePoints by computing all distances from each point cloud point and keeping a "short list".
    /// See RTree KNeighbors for alternatives.
    /// </summary>
    /// <param name="hayPoints">A point cloud to be searched.</param>
    /// <param name="needlePoints">Points to search for.</param>
    /// <param name="amount">The required amount of closest neighbors to find.</param>
    /// <returns>An enumerable of arrays of indices; each array contains the indices for each of the needlePts.</returns>
    /// <seealso cref="Rhino.Geometry.RTree.Point3dKNeighbors(IEnumerable{Point3d}, IEnumerable{Point3d}, int)"/>
    /// <since>6.0</since>
    public static IEnumerable<int[]> Point3dKNeighbors(IEnumerable<Point3d> hayPoints, IEnumerable<Point3d> needlePoints, int amount)
    {
      return KNeighbors(hayPoints, needlePoints, amount);
    }

    /// <summary>
    /// Finds a certain amour of points in a list of single-precision 3D points that are the k-closest to a test point.
    /// This method searches needlePoints by computing all distances from each point cloud point and keeping a "short list".
    /// </summary>
    /// <param name="hayPoints">A point cloud to be searched.</param>
    /// <param name="needlePoints">Points to search for.</param>
    /// <param name="amount">The required amount of closest neighbors to find.</param>
    /// <returns>An enumerable of arrays of indices; each array contains the indices for each of the needlePts.</returns>
    /// <seealso cref="Rhino.Geometry.RTree.Point3dKNeighbors(IEnumerable{Point3d}, IEnumerable{Point3d}, int)"/>
    /// <since>6.0</since>
    public static IEnumerable<int[]> Point3fKNeighbors(IEnumerable<Point3f> hayPoints, IEnumerable<Point3f> needlePoints, int amount)
    {
      return KNeighbors(hayPoints, needlePoints, amount);
    }

    /// <summary>
    /// Finds a certain amour of points in a list of single-precision 2D points that are the k-closest to a test point.
    /// This method searches needlePoints by computing all distances from each point cloud point and keeping a "short list".
    /// </summary>
    /// <param name="hayPoints">A point cloud to be searched.</param>
    /// <param name="needlePoints">Points to search for.</param>
    /// <param name="amount">The required amount of closest neighbors to find.</param>
    /// <returns>An enumerable of arrays of indices; each array contains the indices for each of the needlePts.</returns>
    /// <seealso cref="Rhino.Geometry.RTree.Point3dKNeighbors(IEnumerable{Point3d}, IEnumerable{Point3d}, int)"/>
    /// <since>6.0</since>
    public static IEnumerable<int[]> Point2dKNeighbors(IEnumerable<Point2d> hayPoints, IEnumerable<Point2d> needlePoints, int amount)
    {
      return KNeighbors(hayPoints, needlePoints, amount);
    }

    /// <summary>
    /// Finds a certain amour of points in a list of single-precision 2D points that are the k-closest to a test point.
    /// This method searches needlePoints by computing all distances from each point cloud point and keeping a "short list".
    /// </summary>
    /// <param name="hayPoints">A point cloud to be searched.</param>
    /// <param name="needlePoints">Points to search for.</param>
    /// <param name="amount">The required amount of closest neighbors to find.</param>
    /// <returns>An enumerable of arrays of indices; each array contains the indices for each of the needlePts.</returns>
    /// <seealso cref="Rhino.Geometry.RTree.Point3dKNeighbors(IEnumerable{Point3d}, IEnumerable{Point3d}, int)"/>
    /// <since>6.0</since>
    public static IEnumerable<int[]> Point2fKNeighbors(IEnumerable<Point2f> hayPoints, IEnumerable<Point2f> needlePoints, int amount)
    {
      return KNeighbors(hayPoints, needlePoints, amount);
    }

    /// <typeparam name="T">T must be of type Point3d, Vector3d, Point3f, Vector3f, Point, Point2d, Vector2d, Point2f, Vector2f or Point4d.</typeparam>
    internal static IEnumerable<int[]> KNeighbors<T>(IEnumerable<T> hayPoints, IEnumerable<T> needlePoints, int amount)
    where T : IValidable
    {
      //this is the algorithm used in Grasshopper. It has disadvantages for large len(needlePoints), but 
      //it does not require building any particularly large data structure. The RTree version does the opposite.

      if (hayPoints == null) throw new ArgumentNullException(nameof(hayPoints));
      if (needlePoints == null) throw new ArgumentNullException(nameof(needlePoints));

      if ((amount < 1))
      {
        throw new ArgumentException("Count must be a positive integer", nameof(amount));
      }

      ValidateArray(hayPoints);

      var dis = new double[amount];

      List<int[]> results = new List<int[]>();
      foreach (var needle in needlePoints)
      {
        var idx = new int[amount];

        for (int i = 0; i < idx.Length; i++)
        {
          idx[i] = -1;
          dis[i] = double.MaxValue;
        }

        {
          int i = -1;
          foreach (var hay_pt in hayPoints)
          {
            ++i;

            double sq_distance = DistanceIndicator(needle, hay_pt);
            int index = Array.BinarySearch<double>(dis, sq_distance);

            if (index < 0)
              index = ~index;
            if (index >= idx.Length)
              continue;

            //Shift elements above the index.
            for (int k = idx.Length - 1; k > index; k--)
            {
              idx[k] = idx[k - 1];
              dis[k] = dis[k - 1];
            }

            //Insert the new item.
            idx[index] = i;
            dis[index] = sq_distance;
          }
        }

        yield return idx;
      }
    }

    private static int ValidateArray<T>(IEnumerable<T> hayPoints)
    where T : IValidable
    {
      int i = -1;
      foreach (var hay_pt in hayPoints)
      {
        ++i;
        if ((!hay_pt.IsValid)) throw new ArgumentException($"There is an invalid point within hayPoints at index: {i}.", nameof(hayPoints));
      }

      return i;
    }

    static double DistanceIndicator<T>(T first, T second)
    {
      if (typeof(T) == typeof(Point3d))
      {
        return (((Point3d)(object)first) - ((Point3d)(object)second)).SquareLength;
      }
      else if (typeof(T) == typeof(Point3f))
      {
        return (((Point3f)(object)first) - ((Point3f)(object)second)).SquareLength;
      }
      else if (typeof(T) == typeof(Vector3d))
      {
        return (((Vector3d)(object)first) - ((Vector3d)(object)second)).SquareLength;
      }
      else if (typeof(T) == typeof(Vector3f))
      {
        return (((Vector3f)(object)first) - ((Vector3f)(object)second)).SquareLength;
      }
      else if (typeof(T) == typeof(Point2d))
      {
        return (((Point2d)(object)first) - ((Point2d)(object)second)).SquareLength;
      }
      else if (typeof(T) == typeof(Point2f))
      {
        return (((Point2f)(object)first) - ((Point2f)(object)second)).SquareLength;
      }
      else if (typeof(T) == typeof(Vector2d))
      {
        return (((Vector2d)(object)first) - ((Vector2d)(object)second)).SquareLength;
      }
      else if (typeof(T) == typeof(Vector2f))
      {
        return (((Vector2f)(object)first) - ((Vector2f)(object)second)).SquareLength;
      }
      else if (typeof(T) == typeof(Point))
      {
        return (((Point)(object)first).Location - ((Point)(object)second).Location).SquareLength;
      }
      else
      {
        throw new ArgumentException("T must be of type Point3d, Vector3d, Point3f, Vector3f, Point, Point2d, Vector2d, Point2f or Vector2f.", nameof(T));
      }
    }
  }

  /// <summary>
  /// Utility class for displaying <see cref="RhinoList{T}" /> contents in the VS debugger.
  /// </summary>
  internal class ListDebuggerDisplayProxy<T>
  {
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private readonly ICollection<T> m_collection;

    public ListDebuggerDisplayProxy(ICollection<T> collection)
    {
      if (collection == null)
        throw new ArgumentNullException("collection");
      m_collection = collection;
    }

    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    public T[] Items
    {
      get
      {
        T[] array = new T[m_collection.Count];
        m_collection.CopyTo(array, 0);
        return array;
      }
    }
  }

  /// <summary>
  /// Represents a list of <see cref="Point3d"/>.
  /// </summary>
  [Serializable]
  public class Point3dList : RhinoList<Point3d>, ICloneable, IEquatable<Point3dList>
  {
    /// <summary>
    /// Initializes a new empty list with default capacity.
    /// </summary>
    /// <since>5.0</since>
    public Point3dList()
    {
    }

    /// <summary>
    /// Initializes a new point list with a preallocated initial capacity.
    /// </summary>
    /// <example>
    /// <code source='examples\vbnet\ex_addnurbscurve.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_addnurbscurve.cs' lang='cs'/>
    /// <code source='examples\py\ex_addnurbscurve.py' lang='py'/>
    /// </example>
    /// <param name="initialCapacity">The number of added items before which the underlying array will be resized.</param>
    /// <since>5.0</since>
    public Point3dList(int initialCapacity)
      : base(initialCapacity)
    {
    }

    /// <summary>
    /// Initializes a new point list by copying the values from another set.
    /// </summary>
    /// <param name="collection">The collection to copy from.</param>
    /// <since>5.0</since>
    public Point3dList(IEnumerable<Point3d> collection)
      : base(collection)
    {
    }

    /// <summary>
    /// Constructs a new point list from values in a point array.
    /// </summary>
    /// <param name="initialPoints">Points to add to the list.</param>
    /// <since>5.0</since>
    public Point3dList(params Point3d[] initialPoints)
    {
      if (initialPoints != null)
      {
        AddRange(initialPoints);
      }
    }

    internal static Point3dList FromNativeArray(Runtime.InteropWrappers.SimpleArrayPoint3d pts)
    {
      if (null == pts)
        return null;
      int count = pts.Count;
      Point3dList list = new Point3dList(count);
      if (count > 0)
      {
        IntPtr pNativeArray = pts.ConstPointer();
        UnsafeNativeMethods.ON_3dPointArray_CopyValues(pNativeArray, list.m_items);
        list.m_size = count;
      }
      return list;
    }

    #region Properties
    /// <summary>
    /// Even though this is a property, it is not a "fast" calculation. Every point is
    /// evaluated in order to get the bounding box of the list.
    /// </summary>
    /// <since>5.0</since>
    public BoundingBox BoundingBox
    {
      get
      {
        if (m_size == 0) { return BoundingBox.Empty; }

        double x0 = double.MaxValue;
        double x1 = double.MinValue;
        double y0 = double.MaxValue;
        double y1 = double.MinValue;
        double z0 = double.MaxValue;
        double z1 = double.MinValue;

        for (int i = 0; i < m_size; i++)
        {
          x0 = Math.Min(x0, m_items[i].X);
          x1 = Math.Max(x1, m_items[i].X);
          y0 = Math.Min(y0, m_items[i].Y);
          y1 = Math.Max(y1, m_items[i].Y);
          z0 = Math.Min(z0, m_items[i].Z);
          z1 = Math.Max(z1, m_items[i].Z);
        }

        return new BoundingBox(new Point3d(x0, y0, z0), new Point3d(x1, y1, z1));
      }
    }

    /// <summary>
    /// Finds the index of the point that is closest to a test point in this list.
    /// </summary>
    /// <param name="testPoint">point to compare against.</param>
    /// <returns>index of closest point in the list on success. -1 on error.</returns>
    /// <since>5.0</since>
    public int ClosestIndex(Point3d testPoint)
    {
      return ClosestIndexInList(this, testPoint);
    }

    #region "Coordinate access"
    internal XAccess m_x_access;
    internal YAccess m_y_access;
    internal ZAccess m_z_access;

    /// <summary>
    /// Returns an indexer with all X coordinates in this list.
    /// </summary>
    /// <since>5.0</since>
    public XAccess X
    {
      get { return m_x_access ?? (m_x_access = new XAccess(this)); }
    }

    /// <summary>
    /// Returns an indexer with all Y coordinates in this list.
    /// </summary>
    /// <since>5.0</since>
    public YAccess Y
    {
      get { return m_y_access ?? (m_y_access = new YAccess(this)); }
    }

    /// <summary>
    /// Returns an indexer with all Z coordinates in this list.
    /// </summary>
    /// <since>5.0</since>
    public ZAccess Z
    {
      get { return m_z_access ?? (m_z_access = new ZAccess(this)); }
    }

    /// <summary>
    /// Utility class for easy-access of x-components of points inside an ON_3dPointList.
    /// </summary>
    public class XAccess
    {
      private readonly Point3dList m_owner;

      /// <summary>
      /// XAccess constructor. 
      /// </summary>
      internal XAccess(Point3dList owner)
      {
        if (owner == null) { throw new ArgumentNullException("owner"); }
        m_owner = owner;
      }

      /// <summary>
      /// Gets or sets the x-coordinate of the specified point.
      /// </summary>
      /// <param name="index">Index of point.</param>
      public double this[int index]
      {
        get { return m_owner.m_items[index].X; }
        set { m_owner.m_items[index].X = value; }
      }
    }

    /// <summary>
    /// Utility class for easy-access of x-components of points inside an ON_3dPointList.
    /// </summary>
    public class YAccess
    {
      private readonly Point3dList m_owner;

      /// <summary>
      /// XAccess constructor. 
      /// </summary>
      internal YAccess(Point3dList owner)
      {
        if (owner == null) { throw new ArgumentNullException("owner"); }
        m_owner = owner;
      }

      /// <summary>
      /// Gets or sets the y-coordinate of the specified point.
      /// </summary>
      /// <param name="index">Index of point.</param>
      public double this[int index]
      {
        get { return m_owner.m_items[index].Y; }
        set { m_owner.m_items[index].Y = value; }
      }
    }

    /// <summary>
    /// Utility class for easy-access of z-components of points inside an ON_3dPointList.
    /// </summary>
    public class ZAccess
    {
      private readonly Point3dList m_owner;

      /// <summary>
      /// XAccess constructor. 
      /// </summary>
      internal ZAccess(Point3dList owner)
      {
        if (owner == null) { throw new ArgumentNullException("owner"); }
        m_owner = owner;
      }

      /// <summary>
      /// Gets or sets the z-coordinate of the specified point.
      /// </summary>
      /// <param name="index">Index of point.</param>
      public double this[int index]
      {
        get { return m_owner.m_items[index].Z; }
        set { m_owner.m_items[index].Z = value; }
      }
    }
    #endregion
    #endregion

    #region methods
    /// <summary>
    /// Adds a Point3d to the end of the list with given x,y,z coordinates.
    /// </summary>
    /// <param name="x">The X coordinate.</param>
    /// <param name="y">The Y coordinate.</param>
    /// <param name="z">The Z coordinate.</param>
    /// <example>
    /// <code source='examples\vbnet\ex_addnurbscurve.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_addnurbscurve.cs' lang='cs'/>
    /// <code source='examples\py\ex_addnurbscurve.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    public void Add(double x, double y, double z)
    {
      Add(new Point3d(x, y, z));
    }

    /// <summary>
    /// Applies a transform to all the points in the list.
    /// </summary>
    /// <param name="xform">Transform to apply.</param>
    /// <since>5.0</since>
    public void Transform(Transform xform)
    {
      for (int i = 0; i < Count; i++)
      {
        //David: changed this on April 3rd 2010, Transform acts on the point directly.
        m_items[i].Transform(xform);
      }
    }

    /// <summary>
    /// Set all the X values for the points to a single value
    /// </summary>
    /// <param name="xValue"></param>
    /// <since>5.6</since>
    public void SetAllX(double xValue)
    {
      for (int i = 0; i < Count; i++)
        m_items[i].X = xValue;
    }

    /// <summary>
    /// Set all the Y values for the points to a single value
    /// </summary>
    /// <param name="yValue"></param>
    /// <since>5.6</since>
    public void SetAllY(double yValue)
    {
      for (int i = 0; i < Count; i++)
        m_items[i].Y = yValue;
    }

    /// <summary>
    /// Set all the Z values for the points to a single value
    /// </summary>
    /// <param name="zValue"></param>
    /// <since>5.6</since>
    public void SetAllZ(double zValue)
    {
      for (int i = 0; i < Count; i++)
        m_items[i].Z = zValue;
    }

    /// <summary>
    /// Finds the index of the point in a list of points that is closest to a test point.
    /// </summary>
    /// <param name="list">A list of points.</param>
    /// <param name="testPoint">Point to compare against.</param>
    /// <returns>Index of closest point in the list on success or -1 on error.</returns>
    /// <since>5.0</since>
    public static int ClosestIndexInList(IList<Point3d> list, Point3d testPoint)
    {
      if (null == list || !testPoint.IsValid)
        return -1;

      double min_d = double.MaxValue;
      int min_i = -1;
      int count = list.Count;
      for (int i = 0; i < count; i++)
      {
        Point3d p = list[i];
        double dSquared = (p.X - testPoint.X) * (p.X - testPoint.X) +
                          (p.Y - testPoint.Y) * (p.Y - testPoint.Y) +
                          (p.Z - testPoint.Z) * (p.Z - testPoint.Z);

        //quick abort in case of exact match
        if (dSquared == 0.0)
          return i;
        if (dSquared < min_d)
        {
          min_d = dSquared;
          min_i = i;
        }
      }

      return min_i;
    }

    /// <summary>
    /// Finds the point in a list of points that is closest to a test point.
    /// </summary>
    /// <param name="list">A list of points.</param>
    /// <param name="testPoint">Point to compare against.</param>
    /// <returns>A point.</returns>
    /// <exception cref="ArgumentException">
    /// List must contain at least one point and testPoint must be valid.
    /// </exception>
    /// <since>5.0</since>
    public static Point3d ClosestPointInList(IList<Point3d> list, Point3d testPoint)
    {
      if (list.Count < 1 || !testPoint.IsValid)
        throw new ArgumentException("list must contain at least one point and testPoint must be valid");
      int index = ClosestIndexInList(list, testPoint);
      return list[index];
    }

    object ICloneable.Clone()
    {
      return new Point3dList(this);
    }

    /// <summary>
    /// Returns a deep copy of this point list instance.
    /// </summary>
    /// <returns>The duplicated list.</returns>
    /// <since>6.0</since>
    public new Point3dList Duplicate()
    {
      return (Point3dList)(this as ICloneable).Clone();
    }

    /// <summary>
    /// Overrides the dafault object equality to compare lists by value.
    /// </summary>
    /// <param name="obj"></param>
    /// <returns>True is objects are exaclty equal in value.</returns>
    public override bool Equals(object obj)
    {
      return base.Equals(obj as Point3dList);
    }

    /// <summary>
    /// Determines if the point lists are exactly equal.
    /// </summary>
    /// <param name="other"></param>
    /// <returns>True is objects are exaclty equal in value.</returns>
    public bool Equals(Point3dList other)
    {
      if (object.ReferenceEquals(other, null)) return false;

      if (other.Count != this.Count) return false;

      for (int i = 0; i < Count; i++)
      {
        if (this[i] != other[i])
        {
          return false;
        }
      }

      return true;
    }

    /// <summary>
    /// Creates a hash code for this object.
    /// </summary>
    /// <returns>An int that serves as an hash code. This is currently a XOR of all doubles, XORed in line.</returns>
    public override int GetHashCode()
    {
      int code = 0;
      for (int i = 0; i < Count; i++)
        code ^= this[i].GetHashCode();

      return code;
    }
    #endregion
  }

  /// <summary>
  /// Represents a list of curves.
  /// </summary>
  public class CurveList : RhinoList<Curve>
  {
    /// <summary>
    /// Initializes a new empty list of curves.
    /// </summary>
    /// <since>5.0</since>
    public CurveList()
    {
    }

    /// <summary>
    /// Initializes a new empty list of curves with a predefined capacity.
    /// <para>This is the amount of items the list will accept before resizing.</para>
    /// </summary>
    /// <since>5.0</since>
    public CurveList(int initialCapacity)
      : base(initialCapacity)
    {
    }

    /// <summary>
    /// Initializes a new list that is filled with all items of the input enumerable.
    /// <para>Input items are not explicitly duplicated (this is a shallow copy).</para>
    /// </summary>
    /// <param name="collection">A list, an array or any enumerable set of <see cref="Curve"/>.</param>
    /// <since>5.0</since>
    public CurveList(IEnumerable<Curve> collection)
      : base(collection)
    {
    }

    #region Addition Overloads
    /// <summary>
    /// Adds a line to this list.
    /// </summary>
    /// <param name="line">A line value that will be the model of the new internal curve.</param>
    /// <since>5.0</since>
    public void Add(Line line)
    {
      base.Add(new LineCurve(line));
    }

    /// <summary>
    /// Adds a circle to this list.
    /// </summary>
    /// <param name="circle">A circle value that will be the model of the new internal curve.</param>
    /// <since>5.0</since>
    public void Add(Circle circle)
    {
      base.Add(new ArcCurve(circle));
    }

    /// <summary>
    /// Adds an arc to this list.
    /// </summary>
    /// <param name="arc">An arc value that will be the model of the new internal curve.</param>
    /// <since>5.0</since>
    public void Add(Arc arc)
    {
      base.Add(new ArcCurve(arc));
    }

    /// <summary>
    /// Adds a polyline to this list.
    /// </summary>
    /// <param name="polyline">A polyline value that will be copied in a new polyline.
    /// <para>This argument can be null, an array, a list or any enumerable set of <see cref="Point3d"/>.</para></param>
    /// <since>5.0</since>
    public void Add(IEnumerable<Point3d> polyline)
    {
      if (polyline == null) base.Add(null);
      base.Add(new PolylineCurve(polyline));
    }

    /// <summary>
    /// Adds an ellipse to this list.
    /// </summary>
    /// <param name="ellipse">An ellipse that will be the model of the new internal curve.</param>
    /// <since>5.0</since>
    public void Add(Ellipse ellipse)
    {
      base.Add(NurbsCurve.CreateFromEllipse(ellipse));
    }

    #endregion

    #region Insertion overloads
    /// <summary>
    /// Inserts a line at a given index of this list.
    /// </summary>
    /// <param name="index">A 0-based position in the list.</param>
    /// <param name="line">The line value from which to construct the new curve.</param>
    /// <since>5.0</since>
    public void Insert(int index, Line line)
    {
      base.Insert(index, new LineCurve(line));
    }

    /// <summary>
    /// Inserts a line at a given index of this list.
    /// </summary>
    /// <param name="index">A 0-based position in the list.</param>
    /// <param name="circle">The circle value from which to construct the new curve.</param>
    /// <since>5.0</since>
    public void Insert(int index, Circle circle)
    {
      base.Insert(index, new ArcCurve(circle));
    }

    /// <summary>
    /// Inserts an arc at a given index of this list.
    /// </summary>
    /// <param name="index">A 0-based position in the list.</param>
    /// <param name="arc">The arc value from which to construct the new curve.</param>
    /// <since>5.0</since>
    public void Insert(int index, Arc arc)
    {
      base.Insert(index, new ArcCurve(arc));
    }

    /// <summary>
    /// Inserts a polyline at a given index of this list.
    /// </summary>
    /// <param name="index">A 0-based position in the list.</param>
    /// <param name="polyline">The polyline enumerable from which to construct a copy curve.
    /// <para>This argument can be null, an array, a list or any enumerable set of
    /// <see cref="Point3d"/>.</para></param>
    /// <since>5.0</since>
    public void Insert(int index, IEnumerable<Point3d> polyline)
    {
      if (polyline == null) base.Add(null);
      base.Add(new PolylineCurve(polyline));
    }

    /// <summary>
    /// Inserts an ellipse at a given index of this list.
    /// </summary>
    /// <param name="index">A 0-based position in the list.</param>
    /// <param name="ellipse">The ellipse value from which to construct the new curve.</param>
    /// <since>5.0</since>
    public void Insert(int index, Ellipse ellipse)
    {
      base.Add(NurbsCurve.CreateFromEllipse(ellipse));
    }
    #endregion

    #region Geometry utilities
    /// <summary>
    /// Transform all the curves in this list. If at least a single transform failed 
    /// this function returns false.
    /// </summary>
    /// <param name="xform">Transformation to apply to all curves.</param>
    /// <since>5.0</since>
    public bool Transform(Transform xform)
    {
      bool rc = true;

      foreach (Curve crv in this)
      {
        if (crv == null) { continue; }
        if (!crv.Transform(xform)) { rc = false; }
      }

      return rc;
    }
    #endregion
  }
}
