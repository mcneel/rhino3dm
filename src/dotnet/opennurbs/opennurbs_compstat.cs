using System;
using System.Text;
using Rhino.Runtime;

// ReSharper disable once CheckNamespace
namespace Rhino.Geometry
{
  /// <summary>
  /// Provides information about selection, highlighting, visibility, editability and integrity states of a component.
  /// <para>This structure is immutable.</para>
  /// </summary>
  public struct ComponentStatus : IEquatable<ComponentStatus>
  {
    private readonly byte m_status;

    /// <summary>Only used to create from named states.</summary>
    private ComponentStatus(ComponentState state)
    {
      m_status = (byte)UnsafeNativeMethods.ON_ComponentStatus_BuildFromState((uint)state);
    }

    /// <summary>Used in marshaling. Do NOT expose this.
    /// This will ensure the same type safety present in OpenNURBS.</summary>
    internal ComponentStatus(byte status)
    {
      m_status = status;
    }

    /// <summary>
    /// This is the default value and equal to undefined.
    /// </summary>
    public static ComponentStatus Clear { get { return new ComponentStatus(ComponentState.Unset);} }

    /// <summary>
    /// The selection flag is checked.
    /// </summary>
    public static ComponentStatus Selected { get { return new ComponentStatus(ComponentState.Selected); } }

    /// <summary>
    /// The persistent selection flag is checked.
    /// </summary>
    public static ComponentStatus SelectedPersistent { get { return new ComponentStatus(ComponentState.SelectedPersistent); } }

    /// <summary>
    /// The highlight selection flag is checked.
    /// </summary>
    public static ComponentStatus Highlighted { get { return new ComponentStatus(ComponentState.Highlighted); } }

    /// <summary>
    /// The hidden flag is checked.
    /// </summary>
    public static ComponentStatus Hidden { get { return new ComponentStatus(ComponentState.Hidden); } }

    /// <summary>
    /// The locked flag is checked.
    /// </summary>
    public static ComponentStatus Locked { get { return new ComponentStatus(ComponentState.Locked); } }

    /// <summary>
    /// The damaged flag is checked.
    /// </summary>
    public static ComponentStatus Damaged { get { return new ComponentStatus(ComponentState.Damaged); } }

    /// <summary>
    /// All flags are checked.
    /// </summary>
    public static ComponentStatus AllSet { get
      {
        var all_set = (byte)UnsafeNativeMethods.ON_ComponentStatus_AllSet();
        var cp = new ComponentStatus(all_set);
        return cp;
      }
    }

    /// <summary>
    /// True if every setting is 0 or false.
    /// </summary>
    /// <returns>True if the component status is undefined or clear.</returns>
    public bool IsClear
    {
      get
      {
        return UnsafeNativeMethods.ON_ComponentStatus_IsClear(m_status);
      }
    }

    /// <summary>
    /// Returns false if component is not damaged. True otherwise.
    /// </summary>
    /// <returns>The component status for damaged.</returns>
    public bool IsDamaged
    {
      get
      {
        return UnsafeNativeMethods.ON_ComponentStatus_IsDamaged(m_status);
      }
    }

    /// <summary>
    /// Returns true if highlighted. False otherwise.
    /// </summary>
    /// <returns>The component status for highlighted.</returns>
    public bool IsHighlighted
    {
      get
      {
        return UnsafeNativeMethods.ON_ComponentStatus_IsHighlighted(m_status);
      }
    }

    /// <summary>
    /// Returns true if hidden. False otherwise.
    /// </summary>
    /// <returns>The component status for hidden.</returns>
    public bool IsHidden
    {
      get
      {
        return UnsafeNativeMethods.ON_ComponentStatus_IsHidden(m_status);
      }
    }

    /// <summary>
    /// Returns true if locked. False otherwise.
    /// </summary>
    /// <returns>The component status for locked.</returns>
    public bool IsLocked
    {
      get
      {
        return UnsafeNativeMethods.ON_ComponentStatus_IsLocked(m_status);
      }
    }

    /// <summary>
    /// Returns true if selected or selected persistent. False otherwise.
    /// </summary>
    /// <returns>The component status for selected.</returns>
    public bool IsSelected
    {
      get
      {
        return UnsafeNativeMethods.ON_ComponentStatus_IsSelected(m_status);
      }
    }

    /// <summary>
    /// Returns true if selected persistent. False otherwise.
    /// </summary>
    /// <returns>The component status for selected.</returns>
    public bool IsSelectedPersistent
    {
      get
      {
        return UnsafeNativeMethods.ON_ComponentStatus_IsSelectedPersistent(m_status);
      }
    }

    /// <summary>
    /// Activates any information flag described in any of the two input component statuses
    /// and returns a new ComponentStatus with those flags checked.
    /// </summary>
    /// <param name="additionalStatus">To be used for adding status values.</param>
    [ConstOperation]
    public ComponentStatus WithStates(ComponentStatus additionalStatus)
    {
      var current = (uint)m_status;
      UnsafeNativeMethods.ON_ComponentStatus_SetStates(ref current, additionalStatus.m_status);
      return new ComponentStatus((byte)current);
    }

    /// <summary>
    /// For the purposes of this test, Selected and SelectedPersistent are considered equal.
    /// </summary>
    /// <param name="statesFilter">If no states are specified, then false is returned.</param>
    /// <param name="comparand">If a state is set in states_filter, the corresponding state
    /// in "this" and comparand will be tested.</param>
    /// <returns>True if at least one tested state in "this" and comparand are identical.</returns>
    [ConstOperation]
    public bool HasSomeEqualStates(ComponentStatus statesFilter, ComponentStatus comparand)
    {
      return UnsafeNativeMethods.ON_ComponentStatus_SomeEqualStates(m_status, statesFilter.m_status, comparand.m_status);
    }

    /// <summary>
    /// For the purposes of this test, Selected and SelectedPersistent are considered equal.
    /// </summary>
    /// <param name="statesFilter">If no states are specified, then false is returned.</param>
    /// <param name="comparand">If a state is set in states_filter, the corresponding state
    /// in "this" and comparand will be tested.</param>
    /// <returns>True if at all tested states in "this" and comparand are identical.</returns>
    [ConstOperation]
    public bool HasAllEqualStates(ComponentStatus statesFilter, ComponentStatus comparand)
    {
      return UnsafeNativeMethods.ON_ComponentStatus_AllEqualStates(m_status, statesFilter.m_status, comparand.m_status);
    }

    /// <summary>
    /// For the purposes of this test, Selected and SelectedPersistent are considered equal.
    /// </summary>
    /// <param name="statesFilter">If no states are specified, then false is returned.</param>
    /// <param name="comparand">If a state is set in states_filter, the corresponding state
    /// in "this" and comparand will be tested.</param>
    /// <returns>True if at all tested states in "this" and comparand are identical.</returns>
    [ConstOperation]
    public bool HasNoEqualStates(ComponentStatus statesFilter, ComponentStatus comparand)
    {
      return UnsafeNativeMethods.ON_ComponentStatus_NoEqualStates(m_status, statesFilter.m_status, comparand.m_status);
    }


    /// <summary>
    /// Sets flags from both component states and returns a new ComponentStatus.
    /// </summary>
    /// <param name="a">The first ComponentStatus.</param>
    /// <param name="b">The second ComponentStatus.</param>
    /// <returns>A new ComponentStatus.</returns>
    public static ComponentStatus operator +(ComponentStatus a, ComponentStatus  b)
    {
      return a.WithStates(b);
    }

    /// <summary>
    /// Determines if two ComponentStatus objects are equal.
    /// </summary>
    /// <param name="a">The first ComponentStatus.</param>
    /// <param name="b">The second ComponentStatus.</param>
    /// <returns>true if they are exactly equal. False otherwise.</returns>
    public static bool operator ==(ComponentStatus a, ComponentStatus b)
    {
      return a.Equals(b);
    }

    /// <summary>
    /// Determines if two ComponentStatus objects differ.
    /// </summary>
    /// <param name="a">The first ComponentStatus.</param>
    /// <param name="b">The second ComponentStatus.</param>
    /// <returns>true if they are in any way different. False otherwise.</returns>
    public static bool operator !=(ComponentStatus a, ComponentStatus b)
    {
      return a.m_status != b.m_status;
    }

    /// <summary>
    /// Provides a string representation of this ComponentStatus.
    /// </summary>
    /// <returns>The representation in English.</returns>
    [ConstOperation]
    public override string ToString()
    {
      if (IsClear) return "Clear";

      var sb = new StringBuilder();
      if (IsDamaged) sb.Append("Damaged ");
      if (IsHidden) sb.Append("Hidden ");
      if (IsHighlighted) sb.Append("Highlighted ");
      if (IsLocked) sb.Append("Locked ");
      if (IsSelected) sb.Append("Selected ");
      if (IsSelectedPersistent) sb.Append("SelectedPersistent ");

      if (sb.Length > 1) sb.Remove(sb.Length - 1, 1);

      return sb.ToString();
    }

    /// <summary>
    /// Determines if another ComponentStatus and this are equal.
    /// </summary>
    /// <param name="other">A ComponentStatus.</param>
    /// <returns>true if equal in value. false otherwise</returns>
    public bool Equals(ComponentStatus other)
    {
      return m_status == other.m_status;
    }

    /// <summary>
    /// Determines if an object and this are equal.
    /// </summary>
    /// <param name="obj">An object.</param>
    /// <returns>true if equal in value. false otherwise</returns>
    [ConstOperation]
    public override bool Equals(object obj)
    {
      return obj is ComponentStatus && Equals((ComponentStatus)obj);
    }

    /// <summary>
    /// Serves as a special hash function. The inner value is used for the purpose.
    /// </summary>
    /// <returns>An integer deriving from a bit mask.</returns>
    [ConstOperation]
    public override int GetHashCode()
    {
      return m_status;
    }

    internal byte PrivateBytes()
    {
      return m_status;
    }
  }
}