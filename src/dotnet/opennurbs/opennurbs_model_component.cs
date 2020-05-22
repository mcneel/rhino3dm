using System;
using System.Runtime.Serialization;
using Rhino.Runtime;
using Rhino.Geometry;

namespace Rhino.DocObjects
{
  /// <summary>
  /// Base class for all components in a model (document) and manages the
  /// index, id and other information common to this type of objects.
  /// <para>This class parallels the C++ ON_ModelComponent.</para>
  /// </summary>
  public abstract class ModelComponent : CommonObject
  {
    /// <summary>
    /// Allows construction from inheriting classes.
    /// </summary>
    internal ModelComponent()
    { }

    /// <summary>
    /// Protected constructor for internal use.
    /// </summary>
    /// <param name="info">Serialization data.</param>
    /// <param name="context">Serialization stream.</param>
    internal ModelComponent(SerializationInfo info, StreamingContext context)
      : base (info, context)
    { }

    /// <summary>
    /// Increments the Cyclic Redundancy Check value by this instance.
    /// </summary>
    /// <param name="currentRemainder">The current remainder value.</param>
    /// <returns>The updated remainder value.</returns>
    /// <since>6.0</since>
    [CLSCompliant(false)]
    public uint DataCRC(uint currentRemainder)
    {
      var const_ptr = ConstPointer();
      return UnsafeNativeMethods.ON_ModelComponent_DataCRC(const_ptr, currentRemainder);
    }

    /// <summary>
    /// True if this model component is a system constant.
    /// <para>An incomplete list of system constant model components is below:</para>
    /// <para></para>
    /// <list type="bullet">
    /// <item>ON_ModelComponent::Unset
    /// <para> </para></item>
    /// <item>ON_InstanceDefinition::Empty
    /// <para> </para></item>
    /// <item>ON_Linetype::Unset</item>
    /// <item>ON_Linetype::Continuous</item>
    /// <item>ON_Linetype::ByLayer</item>
    /// <item>ON_Linetype::ByParent
    /// <para> </para></item>
    /// <item>ON_Layer::Unset</item>
    /// <item>ON_Layer::Default
    /// <para> </para></item>
    /// <item>ON_TextStyle::Unset</item>
    /// <item>ON_TextStyle::Default</item>
    /// <item>ON_TextStyle::ByLayer</item>
    /// <item>ON_TextStyle::ByParent
    /// <para> </para></item>
    /// <item>ON_DimStyle::Unset</item>
    /// <item>ON_DimStyle::Default</item>
    /// <item>ON_DimStyle::DefaultInchDecimal</item>
    /// <item>ON_DimStyle::DefaultInchFractional</item>
    /// <item>ON_DimStyle::DefaultFootInchArchitecture</item>
    /// <item>ON_DimStyle::DefaultMillimeterSmall</item>
    /// <item>ON_DimStyle::DefaultMillimeterLarge</item>
    /// <item>ON_DimStyle::DefaultMillimeterArchitecture</item>
    /// </list>
    /// </summary>
    /// <since>6.0</since>
    public bool IsSystemComponent
    {
      get
      {
        var const_ptr = ConstPointer();
        return UnsafeNativeMethods.ON_ModelComponent_IsSystemComponent(const_ptr);
      }
    }


    /// <summary>
    /// Gets or sets the ID of the current instance.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// When setting the ID failed.
    /// This usually happened because the instance ID is already locked.
    /// </exception>
    /// <since>6.0</since>
    public virtual Guid Id
    {
      get
      {
        IntPtr const_ptr = ConstPointer();
        return UnsafeNativeMethods.ON_ModelComponent_GetId(const_ptr);
      }
      set
      {
        IntPtr non_const_ptr = NonConstPointer();
        bool rc = UnsafeNativeMethods.ON_ModelComponent_SetId(non_const_ptr, value);

        if (!rc)
          throw new InvalidOperationException(
          "ModelComponent.set_Id failed. The component is likely locked.");
      }
    }

    /// <summary>
    /// Resets the HasId property of the model component to false, if possible.
    /// </summary>
    /// <exception cref="InvalidOperationException">If the ID is locked.</exception>
    /// <since>6.0</since>
    public void ClearId()
    {
      IntPtr non_const_ptr = NonConstPointer();
      bool rc = UnsafeNativeMethods.ON_ModelComponent_ClearId(non_const_ptr);

      if (!rc)
        throw new InvalidOperationException(
        "ModelComponent.ClearId failed. The component Id is likely locked.");
    }

    /// <summary>
    /// Returns a value indicating whether the component has an ID.
    /// </summary>
    /// <since>6.0</since>
    public bool HasId
    {
      get
      {
        IntPtr const_ptr = ConstPointer();
        return UnsafeNativeMethods.ON_ModelComponent_HasId(const_ptr);
      }
    }

    /// <summary>
    /// Returns a value indicating whether the component ID is already locked.
    /// </summary>
    /// <since>6.0</since>
    public bool IdIsLocked
    {
      get
      {
        IntPtr const_ptr = ConstPointer();
        return UnsafeNativeMethods.ON_ModelComponent_IdIsLocked(const_ptr);
      }
    }

    /// <summary>
    /// Locks the component Id property.
    /// </summary>
    /// <since>6.0</since>
    public void LockId()
    {
      IntPtr non_const_ptr = NonConstPointer();
      UnsafeNativeMethods.ON_ModelComponent_LockId(non_const_ptr);
    }


    /// <summary>
    /// Gets or sets the model component index attribute.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// When setting the Index failed.
    /// This usually happens because the instance Index is already locked.
    /// </exception>
    /// <since>6.0</since>
    public virtual int Index
    {
      get
      {
        IntPtr const_ptr = ConstPointer();
        return UnsafeNativeMethods.ON_ModelComponent_GetIndex(const_ptr);
      }
      set
      {
        IntPtr non_const_ptr = NonConstPointer();
        bool rc = UnsafeNativeMethods.ON_ModelComponent_SetIndex(non_const_ptr, value);

        if (!rc)
          throw new InvalidOperationException(
          "ModelComponent.set_Index failed. The component Index is likely locked.");
      }
    }

    /// <summary>
    /// Resets the HasIndex property of the model component to false, if possible.
    /// </summary>
    /// <exception cref="InvalidOperationException">If the index is locked.</exception>
    /// <since>6.0</since>
    public void ClearIndex()
    {
      IntPtr non_const_ptr = NonConstPointer();
      bool rc = UnsafeNativeMethods.ON_ModelComponent_ClearIndex(non_const_ptr);

      if (!rc)
        throw new InvalidOperationException(
        "ModelComponent.ClearId failed. The component Index is likely locked.");
    }

    /// <summary>
    /// Returns a value indicating whether the component has an Index.
    /// </summary>
    /// <since>6.0</since>
    public bool HasIndex
    {
      get
      {
        IntPtr const_ptr = ConstPointer();
        return UnsafeNativeMethods.ON_ModelComponent_HasIndex(const_ptr);
      }
    }

    /// <summary>
    /// Returns a value indicating whether the component Index is already locked.
    /// </summary>
    /// <since>6.0</since>
    public bool IndexIsLocked
    {
      get
      {
        IntPtr const_ptr = ConstPointer();
        return UnsafeNativeMethods.ON_ModelComponent_IndexIsLocked(const_ptr);
      }
    }

    /// <summary>
    /// Locks the component Index property.
    /// </summary>
    /// <since>6.0</since>
    public void LockIndex()
    {
      IntPtr non_const_ptr = NonConstPointer();
      UnsafeNativeMethods.ON_ModelComponent_LockIndex(non_const_ptr);
    }

    /// <summary>
    /// Gets or sets the component status of the model component.
    /// </summary>
    /// <since>6.0</since>
    public virtual ComponentStatus ComponentStatus
    {
      get
      {
        IntPtr const_ptr = ConstPointer();
        var rc = UnsafeNativeMethods.ON_ModelComponent_GetComponentStatus(const_ptr);
        return new ComponentStatus((byte)rc);
      }
      set
      {
        IntPtr ptr_this = NonConstPointer();
        if (!UnsafeNativeMethods.ON_ModelComponent_SetComponentStatus(ptr_this, value.PrivateBytes()))
          throw new NotSupportedException("Could not set component status.");
      }
    }

    /// <summary>
    /// The component status itself can be locked. This returns an indication.
    /// </summary>
    /// <since>6.0</since>
    public bool IsComponentStatusLocked
    {
      get
      {
        IntPtr const_ptr = ConstPointer();
        return UnsafeNativeMethods.ON_ModelComponent_GetComponentStatusIsLocked(const_ptr);
      }
    }

    /// <summary>
    /// Tests for a valid model component name.
    /// </summary>
    /// <param name="name">The string to validate.</param>
    /// <returns>true if the string is a valid model component name, false otherwise.</returns>
    /// <since>6.15</since>
    public static bool IsValidComponentName(string name)
    {
      return UnsafeNativeMethods.ON_ModelComponent_IsValidComponentName(name);
    }

    /// <summary>
    /// Gets or sets the name
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// When setting the Name failed.
    /// This usually happens because the instance Name is already locked.
    /// </exception>
    /// <example>
    /// <code source='examples\vbnet\ex_hatchcurve.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_hatchcurve.cs' lang='cs'/>
    /// <code source='examples\py\ex_hatchcurve.py' lang='py'/>
    /// </example>
    /// <since>6.0</since>
    public virtual string Name
    {
      get
      {
        IntPtr const_ptr = ConstPointer();
        if (IntPtr.Zero == const_ptr)  return String.Empty;

        using (var sh = new Runtime.InteropWrappers.StringHolder())
        {
          var non_const_ptr_string = sh.NonConstPointer();
          UnsafeNativeMethods.ON_ModelComponent_GetName(const_ptr, non_const_ptr_string);
          return sh.ToString();
        }
      }
      set
      {
        IntPtr non_const_ptr = NonConstPointer();
        bool rc = UnsafeNativeMethods.ON_ModelComponent_SetName(non_const_ptr, value);

        if (!rc)
          throw new InvalidOperationException(
          "ModelComponent.set_Id failed. The component is likely locked.");
      }
    }

    /// <summary>
    /// Resets the HasName property of the model component to false, if possible.
    /// </summary>
    /// <exception cref="InvalidOperationException">If the name is locked.</exception>
    /// <since>6.0</since>
    public void ClearName()
    {
      IntPtr non_const_ptr = NonConstPointer();
      bool rc = UnsafeNativeMethods.ON_ModelComponent_ClearName(non_const_ptr);

      if (!rc)
        throw new InvalidOperationException(
        "ModelComponent.ClearId failed. The component Name is likely locked.");
    }

    /// <summary>
    /// Returns a value indicating whether the component has a Name.
    /// </summary>
    /// <since>6.0</since>
    public bool HasName
    {
      get
      {
        IntPtr const_ptr = ConstPointer();
        return UnsafeNativeMethods.ON_ModelComponent_HasName(const_ptr);
      }
    }

    /// <summary>
    /// Returns a value indicating whether the component Name is already locked.
    /// </summary>
    /// <since>6.0</since>
    public bool NameIsLocked
    {
      get
      {
        IntPtr const_ptr = ConstPointer();
        return UnsafeNativeMethods.ON_ModelComponent_NameIsLocked(const_ptr);
      }
    }

    /// <summary>
    /// Locks the component Name property.
    /// </summary>
    /// <since>6.0</since>
    public void LockName()
    {
      IntPtr non_const_ptr = NonConstPointer();
      UnsafeNativeMethods.ON_ModelComponent_LockName(non_const_ptr);
    }

    /// <summary>
    /// Gets the name of a component that is deleted.
    /// </summary>
    /// <since>6.2</since>
    public string DeletedName
    {
      get
      {
        IntPtr const_ptr = ConstPointer();
        if (IntPtr.Zero == const_ptr) return String.Empty;

        using (var sh = new Runtime.InteropWrappers.StringHolder())
        {
          var non_const_ptr_string = sh.NonConstPointer();
          UnsafeNativeMethods.ON_ModelComponent_GetDeletedName(const_ptr, non_const_ptr_string);
          return sh.ToString();
        }
      }
    }

    /// <summary>
    /// A value identifying the model that manages this component.
    /// </summary>
    /// <remarks>
    /// If the component is being managed by a model, this value identifies the model.
    /// In Rhino, this value is the document runtime serial number.
    /// Typically this value is set and locked by the code that adds a component to a model.
    /// This value is not saved in .3dm archives.
    /// </remarks>
    /// <since>6.12</since>
    [CLSCompliant(false)]
    public virtual uint ModelSerialNumber
    {
      get
      {
        IntPtr const_ptr = ConstPointer();
        return UnsafeNativeMethods.ON_ModelComponent_ModelSerialNumber(const_ptr);
      }
    }

    /// <summary>
    /// When a component is in a model for reference, this value identifies the reference model.
    /// </summary>
    /// <remarks>
    /// Reference components are not saved in .3dm archives. 
    /// Typically this value is set and locked by the code that adds a component to a model.
    /// This value is not saved in .3dm archives.
    /// </remarks>
    /// <since>6.12</since>
    [CLSCompliant(false)]
    public virtual uint ReferenceModelSerialNumber
    {
      get
      {
        IntPtr const_ptr = ConstPointer();
        return UnsafeNativeMethods.ON_ModelComponent_ReferenceModelSerialNumber(const_ptr);
      }
    }

    /// <summary>
    /// When a component is in a model as part of the information required for a linked instance definition,
    /// this value identifies the linked instance definition reference model.
    /// </summary>
    /// <remarks>
    /// Reference components are not saved in .3dm archives. 
    /// Typically this value is set and locked by the code that adds a component to a model.
    /// This value is not saved in .3dm archives.
    /// </remarks>
    /// <since>6.12</since>
    [CLSCompliant(false)]
    public virtual uint InstanceDefinitionModelSerialNumber
    {
      get
      {
        IntPtr const_ptr = ConstPointer();
        return UnsafeNativeMethods.ON_ModelComponent_InstanceDefinitionModelSerialNumber(const_ptr);
      }
   }

    /// <summary>
    /// Gets the <see cref="ModelComponentType"/> for this object.
    /// Useful in switch statements.
    /// </summary>
    /// <since>6.0</since>
    public abstract ModelComponentType ComponentType { get; }

    // Provides the default copying behavior using ON_Object_Duplicate.
    internal override IntPtr _InternalDuplicate(out bool applymempressure)
    {
      applymempressure = true;
      var const_pointer = ConstPointer();
      return UnsafeNativeMethods.ON_Object_Duplicate(const_pointer);
    }

    /// <summary>
    /// Returns the name of the model component type, and then its name and index.
    /// </summary>
    /// <remarks>This is provided as a visual aid. Do not rely on this for serialization.</remarks>
    /// <returns></returns>
    public override string ToString()
    {
      return $"{GetType().Name}: {(string.IsNullOrEmpty(Name)?"(unnamed)":Name)} ({RhinoMath.IntIndexToString(Index)})";
    }

    /// <summary>
    /// Informs the developer if a particular model component type will require uniqueness within a document.
    /// This is currently true with render materials and model geometry; false otherwise.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>true with render materials and model geometry; false otherwise.</returns>
    /// <since>6.0</since>
    public static bool ModelComponentTypeRequiresUniqueName(ModelComponentType type)
    {
      return UnsafeNativeMethods.ON_ModelComponent_RequiresUniqueName(type);
    }

    /// <summary>
    /// Informs the developer if a particular model component type will require case-ignoring searching within a document.
    /// This is currently true with groups; false otherwise.
    /// </summary>
    /// <param name="type">True if the component ignores case.</param>
    /// <since>6.0</since>
    public static bool ModelComponentTypeIgnoresCase(ModelComponentType type)
    {
      return UnsafeNativeMethods.ON_ModelComponent_UniqueNameIgnoresCase(type);
    }

    /// <summary>
    /// Informs the developer if a particular model component type will include the hash of the parent.
    /// </summary>
    /// <param name="type">True if the component includes parent hash.</param>
    /// <since>6.0</since>
    public static bool ModelComponentTypeIncludesParent(ModelComponentType type)
    {
      return UnsafeNativeMethods.ON_ModelComponent_UniqueNameIncludesParent(type);
    }
  }
}
