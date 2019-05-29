using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Rhino.Runtime.Notifications
{
  /// <summary>
  /// An IDictionary implementation where null keys are not allowed. In addition, key lookups
  /// return null instad of throwing an exception.
  /// </summary>
  /// <typeparam name="TKey">The key type of the dictionary.</typeparam>
  /// <typeparam name="TValue">The value type of the dictionary. Must be a class type.</typeparam>
  internal class NonNullableDictionary<TKey, TValue> : IDictionary<TKey, TValue> where TValue : class
  {
    private IDictionary<TKey, TValue> _dict = new Dictionary<TKey,TValue>();

    /// <summary>
    /// Creates a new instance.
    /// </summary>
    public NonNullableDictionary() { }

    /// <summary>
    /// Creates a new instance with a shallow copy of an IDictionary as its contents.
    /// </summary>
    /// <param name="dictionary"></param>
    public NonNullableDictionary(IDictionary<TKey, TValue> dictionary)
    {
      foreach (var item in dictionary)
      {
        this._dict.Add(item);
      }
    }

    /// <summary>
    /// Looks up an object by key.
    /// </summary>
    /// <param name="key">The key to use to lookup an object.</param>
    /// <returns>null if the key does not map to an object; otherwise returns the object.</returns>
    public TValue this[TKey key]
    {
      get
      {
        if (this._dict.ContainsKey(key))
          return this._dict[key];
        else
          return null;
      }

      set
      {
        if (value == null)
          throw new ArgumentNullException();
        else
          this._dict[key] = value;
      }
    }
    /// <summary>
    /// Returns the number of elements contained in the dictionary.
    /// </summary>
    public int Count
    {
      get
      {
        return this._dict.Count;
      }
    }

    /// <summary>
    /// Always returns false.
    /// </summary>
    public bool IsReadOnly
    {
      get
      {
        return false;
      }
    }

    /// <summary>
    /// Returns an ICollection containing all the keys in the dictionary.
    /// </summary>
    public ICollection<TKey> Keys
    {
      get
      {
        return this._dict.Keys;
      }
    }

    /// <summary>
    /// Returns an ICollection containing all the values in the dictionary.
    /// </summary>
    public ICollection<TValue> Values
    {
      get
      {
        return this._dict.Values;
      }
    }

    /// <summary>
    /// Adds a value to the dictionary.
    /// </summary>
    /// <param name="item">a KVP containing a key and a value to add to the dictionary.</param>
    public void Add(KeyValuePair<TKey, TValue> item)
    {
      this._dict.Add(item);
    }

    /// <summary>
    /// Adds a value to the dictionary.
    /// </summary>
    /// <param name="key">The key that maps to the value.</param>
    /// <param name="value">The value to add to the dictionary.</param>
    public void Add(TKey key, TValue value)
    {
      this._dict.Add(key, value);
    }

    /// <summary>
    /// Removes all items from the dictionary.
    /// </summary>
    public void Clear()
    {
      this._dict.Clear();
    }

    public bool Contains(KeyValuePair<TKey, TValue> item)
    {
      return this._dict.Contains(item);
    }

    /// <summary>
    /// Checks for inclusion of a particular key.
    /// </summary>
    /// <param name="key">The key to lookup.</param>
    /// <returns>true if the dictionary contains the key; otherwise false.</returns>
    public bool ContainsKey(TKey key)
    {
      return this._dict.ContainsKey(key);
    }

    /// <summary>
    /// Copies the values of the dictionary to an array.
    /// </summary>
    /// <param name="array">The array to copy the values to.</param>
    /// <param name="arrayIndex">The index of the array to start the copy.</param>
    public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
    {
      this._dict.CopyTo(array, arrayIndex);
    }

    /// <summary>
    /// Returns an enumerator that iterates through the dictionary.
    /// </summary>
    /// <returns></returns>
    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
    {
      return this._dict.GetEnumerator();
    }

    /// <summary>
    /// Removes the first occurrence of the item from the dictionary.
    /// </summary>
    /// <param name="item">The KVP to remove from the dictionary.</param>
    /// <returns>true if the item was removed; otherwise false.</returns>
    public bool Remove(KeyValuePair<TKey, TValue> item)
    {
      return this._dict.Remove(item);
    }

    /// <summary>
    /// Removes the element with the specified key from the dictionary.
    /// </summary>
    /// <param name="key">The key of the lement to remove.</param>
    /// <returns>true if the element was removed; otherwise false.</returns>
    public bool Remove(TKey key)
    {
      return this._dict.Remove(key);
    }

    /// <summary>
    /// Gets the value associated with the key.
    /// </summary>
    /// <param name="key">The key of the element to get.</param>
    /// <param name="value">The value associated with the key.</param>
    /// <returns>True if an element exists, otherwise returns false.</returns>
    public bool TryGetValue(TKey key, out TValue value)
    {
      return this._dict.TryGetValue(key, out value);
    }

    /// <summary>
    /// Returns an enumerator that iterates through the collection.
    /// </summary>
    /// <returns>The enumerator that iterates through the collection.</returns>
    IEnumerator IEnumerable.GetEnumerator()
    {
      return this._dict.GetEnumerator();
    }

    /// <summary>
    /// Tests an instance for equality.
    /// </summary>
    /// <param name="obj">The object to test for equality with this instance.</param>
    /// <returns>true if the objects are equal; otherwise false.</returns>
    /// <remarks>An object is considered equal if obj is a NonNullableDictionary and 
    /// obj contains the same KeyValuePairs as this instance.</remarks>
    public override bool Equals(object obj)
    {
      NonNullableDictionary<TKey, TValue> objAsDict = obj as NonNullableDictionary<TKey, TValue>;
      return objAsDict != null && this.Count == objAsDict.Count && !this.Except(objAsDict).Any();
    }

    /// <summary>
    /// Returns the hash code of this dictionary.
    /// </summary>
    /// <returns></returns>
    public override int GetHashCode()
    {
      return this._dict.GetHashCode();
    }
  }
}
