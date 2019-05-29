using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;

namespace Rhino.NodeInCode
{
  /// <summary>
  /// Permits rapid access to references to all Grasshopper functions.
  /// </summary>
  public class NodeInCodeTable : DynamicObject
  {
    ConcurrentDictionary<Guid, ComponentFunctionInfo> m_guids;
    ConcurrentDictionary<string, ComponentFunctionInfo> m_names;
    ConcurrentDictionary<string, ComponentFunctionInfo> m_upper_names;

    /// <summary>
    /// Instantiates the table. Users of RhinoCommon do not typically need to call this constructor.
    /// </summary>
    /// <param name="items">Items.</param>
    public NodeInCodeTable(IEnumerable<ComponentFunctionInfo> items = null)
    {
      m_guids = new ConcurrentDictionary<Guid, ComponentFunctionInfo>();
      m_names = new ConcurrentDictionary<string, ComponentFunctionInfo>();
      m_upper_names = new ConcurrentDictionary<string, ComponentFunctionInfo>();

      if (items == null) return;

      foreach (var item in items)
      {
        Add(item);
      }
    }

    /// <summary>
    /// Returns the amount of items in this table.
    /// </summary>
    public int Count
    {
      get
      {
        return m_guids.Count;
      }
    }

    /// <summary>
    /// Adds, or replaces a new instance of component function information.
    /// </summary>
    /// <param name="item"></param>
    public void Add(ComponentFunctionInfo item)
    {
      m_guids[item.ComponentGuid] = item;
      m_names[item.FullScriptingName] = item;
      m_upper_names[item.FullScriptingName.ToUpperInvariant()] = item;
    }

    /// <summary>
    /// Dynamically binds the table to property-like access via its item names.
    /// </summary>
    /// <param name="binder">The dynamic binder.</param>
    /// <param name="result">Returns the result.</param>
    /// <returns></returns>
    public override bool TryGetMember(GetMemberBinder binder, out object result)
    {
      IDictionary<string, ComponentFunctionInfo> dict;
      if (binder.IgnoreCase)
        dict = m_upper_names;
      else
        dict = m_names;

      string key = binder.IgnoreCase ? binder.Name.ToUpperInvariant() : binder.Name;

      bool keepTree = false;
      bool warningsAsErrors = true;
      ParseSpecialMode(ref key, ref keepTree, ref warningsAsErrors);

      ComponentFunctionInfo info;
      if (dict.TryGetValue(key, out info))
      {
        Delegate deleg = keepTree ?
          (warningsAsErrors ? info.DelegateTree : info.DelegateTreeNoWarnings) :
          (warningsAsErrors ? info.Delegate : info.DelegateNoWarnings);
        if (binder.ReturnType.IsAssignableFrom(deleg.GetType()))
        {
          result = deleg;
          return true;
        }
      }

      return base.TryGetMember(binder, out result);
    }

    /// <summary>
    /// Returns all additional names in the table.
    /// </summary>
    /// <returns></returns>
    public override IEnumerable<string> GetDynamicMemberNames()
    {
      return m_names.Keys.OrderBy(x => x);
    }

    /// <summary>
    /// Dynamically invokes a member of the table.
    /// </summary>
    /// <param name="binder">The binder.</param>
    /// <param name="args">The arguments.</param>
    /// <param name="result">The result.</param>
    /// <returns>True on success.</returns>
    public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
    {
      IDictionary<string, ComponentFunctionInfo> dict;
      if (binder.IgnoreCase)
        dict = m_upper_names;
      else
        dict = m_names;

      string key = binder.IgnoreCase ? binder.Name.ToUpperInvariant() : binder.Name;

      bool keepTree = false;
      bool warningsAsErrors = true;
      ParseSpecialMode(ref key, ref keepTree, ref warningsAsErrors);

      ComponentFunctionInfo info;
      if (dict.TryGetValue(key, out info))
      {
        string[] warnings;
        result = info.Evaluate(args, keepTree, out warnings);

        if (warningsAsErrors && warnings != null && warnings.Length > 0)
          throw new ApplicationException(warnings[0]);

        if (binder.ReturnType.IsAssignableFrom(result.GetType()))
        {
          return true;
        }
      }

      return base.TryInvokeMember(binder, args, out result);
    }

    /// <summary>
    /// Gets the ComponentFunctionInfo at 
    /// </summary>
    /// <param name="binder">The binder.</param>
    /// <param name="indexes">ONE string index.</param>
    /// <param name="result">The bound info.</param>
    /// <returns></returns>
    public override bool TryGetIndex(GetIndexBinder binder, object[] indexes, out object result)
    {
      if (indexes != null && indexes.Length == 1 && indexes[0] != null && indexes[0].GetType() == typeof(string))
      {
        ComponentFunctionInfo info;
        var rc = m_names.TryGetValue((string)indexes[0], out info);
        if (rc) result = info; else result = null;
        return rc;
      }

      return base.TryGetIndex(binder, indexes, out result);
    }

    private static void ParseSpecialMode(ref string key, ref bool keepTree, ref bool warningsAsErrors)
    {
      if (key.EndsWith("_tree_nowarnings", StringComparison.InvariantCultureIgnoreCase))
      {
        warningsAsErrors = false;
        keepTree = true;
        key = key.Substring(0, key.Length - "_tree_nowarnings".Length);
      }
      else if (key.EndsWith("_tree", StringComparison.InvariantCultureIgnoreCase))
      {
        keepTree = true;
        key = key.Substring(0, key.Length - "_tree".Length);
      }
      if (key.EndsWith("_nowarnings", StringComparison.InvariantCultureIgnoreCase))
      {
        warningsAsErrors = false;
        key = key.Substring(0, key.Length - "_nowarnings".Length);
      }
    }
    /*
    /// <summary>
    /// Returns the item with a particular full name.
    /// </summary>
    /// <param name="fullName"></param>
    /// <returns></returns>
    public ComponentFunctionInfo this[string fullName]
    {
      get
      {
        return GetValueAt(fullName);
      }
    }*/

    internal ComponentFunctionInfo GetValueAt(string fullName)
    {
      var noPeriod = fullName.Replace('.', '_');

      ComponentFunctionInfo info;
      m_names.TryGetValue(noPeriod, out info);

      return info;
    }
  }
}
