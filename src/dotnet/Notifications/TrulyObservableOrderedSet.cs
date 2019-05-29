using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace Rhino.Runtime.Notifications
{
  /// <summary>
  /// An ordered set that notifies its subscribers whenever one of its INotifyPropertyChanged elements raises its PropertyChanged event.
  /// </summary>
  /// <typeparam name="T">A class that implements INotifyPropertyChanged as well as IAssemblyRestrictedObject</typeparam>
  /// <remarks>This class prevents the removal of elements by assemblies they cannot be edited by. At the time of removal, 
  /// each element's <see cref="IAssemblyRestrictedObject.Editable"/> method will be invoked, and if false, an 
  /// InvalidOperationException will be thrown.</remarks>
  public class TrulyObservableOrderedSet<T> : IList<T>, INotifyCollectionChanged where T : INotifyPropertyChanged, IAssemblyRestrictedObject
  {
    private ObservableCollection<T> _list;

    private void _PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
      //We propagate the changes to whomever is observing us.
      bool result = sender.Equals(sender);
      NotifyCollectionChangedEventArgs eventArgs = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset);
      this.CollectionChanged?.Invoke(this, eventArgs);
    }
  
    private void _CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
      this.CollectionChanged?.Invoke(this, e);
    }

    private void _ThrowIfIllegalAssembly(T item)
    {
      if (!item.Editable())
      {
        throw new InvalidOperationException($"{item} cannot be removed from this set");
      }
    }

    /// <summary>
    /// Creates an instance with the given items.
    /// </summary>
    /// <param name="items">Items that the instance will contain. If there are duplicate items, they will be removed.</param>
    public TrulyObservableOrderedSet(IEnumerable<T> items)
    {
      this._list = new ObservableCollection<T>(items.Distinct());

      //We subscribe to each notification so we can propagate changes made to them.
      foreach (T item in items)
      {
        item.PropertyChanged += this._PropertyChanged;
      }

      //We propagate the collection changed event.
      this._list.CollectionChanged += this._CollectionChanged;

    }

    /// <summary>
    /// Creates an empty instance.
    /// </summary>
    public TrulyObservableOrderedSet() : this(new List<T>()) { }

    /// <summary>
    /// Gets or replaces an element in the ordered set at the specified index.
    /// </summary>
    /// <param name="index">The index of the element.</param>
    /// <returns>the element at the given index.</returns>
    /// <exception cref="IndexOutOfRangeException">Thrown if the index passed is out of bounds.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the item that will be replaced at the specified index cannot be modified by the current assembly.</exception>
    public T this[int index]
    {
      get
      {
        return this._list[index];
      }

      set
      {
        T itemToRemove = this[index];
        this._ThrowIfIllegalAssembly(itemToRemove);
        itemToRemove.PropertyChanged -= _PropertyChanged;
        this._list[index] = value;
      }
    }

    /// <summary>
    /// Returns the total number of items in the set.
    /// </summary>
    public int Count
    {
      get
      {
          return this._list.Count;
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
    /// Adds an object to the end of the ordered set if the set does not already contain the item.
    /// </summary>
    /// <param name="item">The item to add.</param>
    public void Add(T item)
    {
      if (!this.Contains(item))
      {
        this._list.Add(item);
        item.PropertyChanged += _PropertyChanged;
      }
    }

    /// <summary>
    /// Clears the ordered set.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if at least one item currently in the set cannot be modified by the current assembly.</exception>
    public void Clear()
    {
      var method = new StackTrace().GetFrame(1).GetMethod();

      foreach (var item in this)
      {
        this._ThrowIfIllegalAssembly(item);
      }

      foreach (var item in this)
      {
        item.PropertyChanged -= _PropertyChanged;
      }

      this._list.Clear();
    }

    /// <summary>
    /// Determines whether an element is in the set.
    /// </summary>
    /// <param name="item">The item to check for inclusion.</param>
    /// <returns>True if the item is in the set; otherwise false.</returns>
    public bool Contains(T item)
    {
      return this._list.Contains(item);
    }

    /// <summary>
    /// Copies the values of the set to an array.
    /// </summary>
    /// <param name="array">The array to copy the values to.</param>
    /// <param name="arrayIndex">The index of the array to start the copy.</param>
    public void CopyTo(T[] array, int arrayIndex)
    {
      this._list.CopyTo(array, arrayIndex);
    }

    /// <summary>
    /// Returns an enumerator that iterates through the set.
    /// </summary>
    /// <returns></returns>
    public IEnumerator<T> GetEnumerator()
    {
      return this._list.GetEnumerator();
    }

    /// <summary>
    /// Searches for the specified object and returns the zero-based index of the first occurrence.
    /// </summary>
    /// <param name="item">The item to locate.</param>
    /// <returns>The zero-based index of the first occurrence of item if found; otherwise -1.</returns>
    public int IndexOf(T item)
    {
      return this._list.IndexOf(item);
    }

    /// <summary>
    /// Inserts an element at the specified index.
    /// </summary>
    /// <param name="index">The index to insert the element at.</param>
    /// <param name="item">The item to insert.</param>
    /// <exception cref="IndexOutOfRangeException">Thrown if the index passed is out of bounds.</exception>
    public void Insert(int index, T item)
    {
      if (!this.Contains(item))
      {
        this._list.Insert(index, item);
        item.PropertyChanged += _PropertyChanged;
      }
    }

    /// <summary>
    /// Removes an element from the set.
    /// </summary>
    /// <param name="item">The element to remove.</param>
    /// <returns>Returns true if the element was removed; otherwise returns false.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the item that will be removed cannot be modified by the current assembly.</exception>
    public bool Remove(T item)
    {
      int index = this._list.IndexOf(item);

      if (index > -1)
      {
        T itemToRemove = this[index];
        this._ThrowIfIllegalAssembly(itemToRemove);
        itemToRemove.PropertyChanged -= _PropertyChanged;
        this._list.RemoveAt(index);
        return true;
      }
      else
      {
        return false;
      }
    }

    /// <summary>
    /// Removes an element at the specified index from the set.
    /// </summary>
    /// <param name="index">The index of the element to remove.</param>
    /// <exception cref="IndexOutOfRangeException">Thrown if the index passed is out of bounds.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the item that will be removed cannot be modified by the current assembly.</exception>
    public void RemoveAt(int index)
    {
      T itemToRemove = this[index];
      this._ThrowIfIllegalAssembly(itemToRemove);
      itemToRemove.PropertyChanged -= _PropertyChanged;
      this._list.RemoveAt(index);
    }

    /// <summary>
    /// Sorts the set.
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <param name="keySelector"></param>
    /// <param name="descending">If true, the sort will happen in descending other; if false, it will happen in ascending order.</param>
    public void Sort<TKey>(Func<T, TKey> keySelector, bool descending=false)
    {
      if (descending)
      {
        this._list = new ObservableCollection<T>(this._list.OrderByDescending(keySelector));
      }
      else
      {
        this._list = new ObservableCollection<T>(this._list.OrderBy(keySelector));
      }

      //We publish a collection changed event.
      NotifyCollectionChangedEventArgs eventArgs = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset);
      this.CollectionChanged?.Invoke(this, eventArgs);

      //We propagate the collection changed event.
      this._list.CollectionChanged += this._CollectionChanged;
    }

    /// <summary>
    /// Returns an enumerator that iterates through the dictionary.
    /// </summary>
    /// <returns></returns>
    IEnumerator IEnumerable.GetEnumerator()
    {
      return this._list.GetEnumerator();
    }

    /// <summary>
    /// Triggered whenever the set changes or when one of its elements has its PropertyChanged event triggered.
    /// </summary>
    public event NotifyCollectionChangedEventHandler CollectionChanged;
  }
}
