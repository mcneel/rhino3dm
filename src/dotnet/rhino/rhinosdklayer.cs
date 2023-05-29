#pragma warning disable 1591
using System;
using System.Runtime.Serialization;
using System.Collections.Generic;
using Rhino.Render;
using Rhino.Runtime.InteropWrappers;
using Rhino.Geometry;
using Rhino.FileIO;
using Rhino.ApplicationSettings;
using Rhino.Display;

namespace Rhino.DocObjects
{
  [Serializable]
  public sealed class Layer : ModelComponent, IEquatable<Layer>
  {
    #region members
    // Represents both a CRhinoLayer and an ON_Layer. When m_ptr is
    // null, the object uses m_doc and m_id to look up the const
    // CRhinoLayer in the layer table.
#if RHINO_SDK
    readonly RhinoDoc m_doc;
#endif
    readonly Guid m_id = Guid.Empty;
    readonly FileIO.File3dm m_onx_model;
    #endregion

    #region constructors
    /// <since>5.0</since>
    public Layer() : base()
    {
      // Creates a new non-document control ON_Layer
      IntPtr ptr_layer = UnsafeNativeMethods.ON_Layer_New();
      ConstructNonConstObject(ptr_layer);
    }

#if RHINO_SDK
    internal Layer(int index, RhinoDoc doc) : base()
    {
      m_id = UnsafeNativeMethods.CRhinoLayerTable_GetLayerId(doc.RuntimeSerialNumber, index);
      m_doc = doc;
      m__parent = m_doc;
    }
#endif

    class LayerHolder
    {
      readonly IntPtr m_const_ptr_layer;
      public LayerHolder(IntPtr pConstLayer)
      {
        m_const_ptr_layer = pConstLayer;
      }
      public IntPtr ConstPointer()
      {
        return m_const_ptr_layer;
      }
    }

    internal Layer(IntPtr ptrLayer, bool isConstPointer)
    {
      if (isConstPointer)
      {
        LayerHolder holder = new LayerHolder(ptrLayer);
        ConstructConstObject(holder, -1);
      }
      else
      {
        ConstructNonConstObject(ptrLayer);
      }
    }

    internal Layer(Guid id, FileIO.File3dm onxModel)
    {
      m_id = id;
      m_onx_model = onxModel;
      m__parent = onxModel;
    }

    // serialization constructor
    private Layer(SerializationInfo info, StreamingContext context)
      : base(info, context)
    {
    }

#if RHINO_SDK
    /// <summary>
    /// Constructs a layer with the current default properties.
    /// The default layer properties are:
    /// <para>color = Rhino.ApplicationSettings.AppearanceSettings.DefaultLayerColor</para>
    /// <para>line style = Rhino.ApplicationSettings.AppearanceSettings.DefaultLayerLineStyle</para>
    /// <para>material index = -1</para>
    /// <para>IGES level = -1</para>
    /// <para>mode = NormalLayer</para>
    /// <para>name = empty</para>
    /// <para>layer index = 0 (ignored by AddLayer)</para>
    /// </summary>
    /// <returns>A new layer instance.</returns>
    /// <since>5.0</since>
    public static Layer GetDefaultLayerProperties()
    {
      Layer layer = new Layer();
      IntPtr ptr = layer.NonConstPointer();
      UnsafeNativeMethods.CRhinoLayerTable_GetDefaultLayerProperties(ptr);
      return layer;
    }
#endif
    #endregion

    /// <example>
    /// <code source='examples\vbnet\ex_locklayer.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_locklayer.cs' lang='cs'/>
    /// <code source='examples\py\ex_locklayer.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    /// <deprecated>6.0</deprecated>
    [Obsolete("No longer needed. Layer changes in the document are now immediate")]
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public bool CommitChanges() { return false; }

    void InternalCommitChanges()
    {
#if RHINO_SDK
      if ((m_set_render_material != Guid.Empty || m_set_render_material_to_null) && null != m_doc)
      {
        var doc_id = m_doc.RuntimeSerialNumber;
        var layer_index = Index;
        UnsafeNativeMethods.Rdk_RenderContent_SetLayerMaterialInstanceId(doc_id, layer_index, m_set_render_material);
      }
      m_set_render_material = Guid.Empty;
      m_set_render_material_to_null = false;
      if (m_id == Guid.Empty || IsDocumentControlled)
        return;
      if (null != m_doc)
      {
        IntPtr const_ptr_this = ConstPointer();
        if (UnsafeNativeMethods.CRhinoLayerTable_CommitChanges(m_doc.RuntimeSerialNumber, const_ptr_this, m_id)
          && IsNonConst)
        {
          // 9 Mar 2019 S. Baer (RH-51299)
          // Convert this layer back into one that references a layer in the document
          IntPtr ptr = NonConstPointer();
          UnsafeNativeMethods.ON_Object_Delete(ptr);
          ReleaseNonConstPointer();
        }
      }
#endif
    }

    internal override IntPtr _InternalGetConstPointer()
    {
#if RHINO_SDK
      if (m_doc != null)
      {
        IntPtr rc = UnsafeNativeMethods.CRhinoLayerTable_GetLayerPointer2(m_doc.RuntimeSerialNumber, m_id);
        if (rc == IntPtr.Zero)
          throw new Runtime.DocumentCollectedException($"Could not find Layer with ID {m_id}");
        return rc;
      }
#endif
      if (m_onx_model != null)
      {
        IntPtr ptr_onx_model = m_onx_model.NonConstPointer();
        return UnsafeNativeMethods.ONX_Model_GetModelComponentPointer(ptr_onx_model, m_id);
      }

      var parent = m__parent as LayerHolder;
      return parent != null ? parent.ConstPointer() : IntPtr.Zero;
    }

    internal override IntPtr _InternalDuplicate(out bool applymempressure)
    {
      applymempressure = false;
      IntPtr const_ptr_this = ConstPointer();
      return UnsafeNativeMethods.ON_Object_Duplicate(const_ptr_this);
    }

    internal override IntPtr NonConstPointer()
    {
      if (m__parent is FileIO.File3dm)
      {
        return _InternalGetConstPointer();
      }

      return base.NonConstPointer();
    }

    #region properties
    /// <summary>
    /// Returns <see cref="ModelComponentType.Layer"/>.
    /// </summary>
    /// <since>6.0</since>
    public override ModelComponentType ComponentType => ModelComponentType.Layer;

    /// <summary>
    /// Gets or sets the status of the layer.
    /// </summary>
    /// <since>6.0</since>
    public override ComponentStatus ComponentStatus
    {
      get
      {
        return base.ComponentStatus;
      }
      set
      {
        base.ComponentStatus = value;
        InternalCommitChanges();
      }
    }

    /// <summary>
    /// Gets a value indicating whether this layer has been deleted and is 
    /// currently in the Undo buffer.
    /// </summary>
    /// <since>5.0</since>
    public override bool IsDeleted => base.IsDeleted;

    /// <summary>
    /// Gets a value indicting whether this layer is a referenced layer. 
    /// Referenced layers are part of referenced documents.
    /// </summary>
    /// <since>5.0</since>
    public override bool IsReference => base.IsReference;

    /// <summary>Gets or sets the name of this layer.</summary>
    /// <example>
    /// <code source='examples\vbnet\ex_sellayer.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_sellayer.cs' lang='cs'/>
    /// <code source='examples\py\ex_sellayer.py' lang='py'/>
    /// </example>
    /// <example>
    /// <code source='examples\vbnet\ex_renamelayer.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_renamelayer.cs' lang='cs'/>
    /// <code source='examples\py\ex_renamelayer.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    public override string Name
    {
      get
      {
        return base.Name;
      }
      set
      {
        base.Name = value;
        InternalCommitChanges();
      }
    }

    /// <summary>
    /// Gets the full path to this layer. The full path includes nesting information.
    /// </summary>
    /// <example>
    /// <code source='examples\vbnet\ex_locklayer.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_locklayer.cs' lang='cs'/>
    /// <code source='examples\py\ex_locklayer.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    public string FullPath
    {
      get
      {
#if RHINO_SDK
        if (null != m_doc)
        {
          int index = Index;
          using (var sh = new StringHolder())
          {
            IntPtr ptr_string = sh.NonConstPointer();
            if (!UnsafeNativeMethods.CRhinoLayer_GetLayerPathName(m_doc.RuntimeSerialNumber, index, ptr_string))
              return Name;
            return sh.ToString();
          }
        }
        if (null != m_onx_model)
        {
          string full_name = null;
          return File3dm_GetLayerPathName(m_onx_model, Index, ref full_name) ? full_name : Name;
        }
        return Name;
#else
        if (null != m_onx_model)
        {
          string full_name = null;
          return File3dm_GetLayerPathName(m_onx_model, Index, ref full_name) ? full_name : Name;
        }
        return Name;
#endif
      }
    }

    internal bool File3dm_GetLayerPathName(File3dm file, int layerIndex, ref string fullName)
    {
      if (null == file || layerIndex < 0 || layerIndex >= file.AllLayers.Count)
        return false;

      // ON_ModelComponent::NamePathSeparator
      string delimeter = NamePathSeparator;

      string name = null;
      for (var i = 0; i < file.AllLayers.Count; i++)
      {
        var layer = file.AllLayers.FindIndex(layerIndex);
        var layer_name = layer.Name;
        if (string.IsNullOrEmpty(name))
        {
          name = layer_name;
        }
        else
        {
          var child_name = name;
          name = layer_name;
          name += delimeter;
          name += child_name;
        }

        if (layer.ParentLayerId == Guid.Empty)
          break;

        var parent = file.AllLayers.FindId(layer.ParentLayerId);
        layerIndex = null != parent ? parent.Index : -1;
        if (layerIndex < 0)
          break;
      }

      fullName = name;

      return true;
    }

    public override string ToString()
    {
      return FullPath;
    }

    /// <summary>
    /// Gets or sets the index of this layer.
    /// </summary>
    /// <since>5.0</since>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    [Obsolete("Use the Index property.")]
    public int LayerIndex
    {
      get { return Index; }
      set { Index = value; }
    }

    /// <summary>
    /// Gets or sets the ID of this layer object. 
    /// You typically do not need to assign a custom ID.
    /// </summary>
    /// <since>5.0</since>
    public override Guid Id
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        return UnsafeNativeMethods.ON_Layer_GetGuid(const_ptr_this, true);
      }
      set
      {
        IntPtr ptr_this = NonConstPointer();
        UnsafeNativeMethods.ON_Layer_SetGuid(ptr_this, true, value);
        InternalCommitChanges();
      }
    }

    /// <summary>
    /// Gets the ID of the parent layer. Layers can be organized in a hierarchical structure, 
    /// in which case this returns the parent layer ID. If the layer has no parent, 
    /// Guid.Empty will be returned.
    /// </summary>
    /// <example>
    /// <code source='examples\vbnet\ex_addchildlayer.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_addchildlayer.cs' lang='cs'/>
    /// <code source='examples\py\ex_addchildlayer.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    public Guid ParentLayerId
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        return UnsafeNativeMethods.ON_Layer_GetGuid(const_ptr_this, false);
      }
      set
      {
        IntPtr ptr_this = NonConstPointer();
        UnsafeNativeMethods.ON_Layer_SetGuid(ptr_this, false, value);
        InternalCommitChanges();
      }
    }

    /// <summary>
    /// Gets or sets the IGES level for this layer.
    /// </summary>
    /// <since>5.0</since>
    public int IgesLevel
    {
      get { return GetInt(UnsafeNativeMethods.LayerInt.IgesLevel); }
      set { SetInt(UnsafeNativeMethods.LayerInt.IgesLevel, value); }
    }

    [FlagsAttribute]
    private enum PerViewportSettings : uint
    {
      None = 0,
      Id = 1,
      Color = 2,
      PlotColor = 4,
      PlotWeight = 8,
      Visible = 16,
      PersistentVisibility = 32,
      All = 0xFFFFFFFF
    }

    /// <summary>
    /// Verifies that a layer has per viewport settings.
    /// </summary>
    /// <param name="viewportId">
    /// If not Guid.Empty, then checks for settings for that specific viewport. 
    /// If Guid.Empty, then checks for any viewport settings.
    /// </param>
    /// <returns>True if the layer has per viewport settings, false otherwise.</returns>
    /// <since>6.0</since>
    public bool HasPerViewportSettings(Guid viewportId)
    {
      IntPtr const_ptr_this = ConstPointer();
      return UnsafeNativeMethods.ON_Layer_HasPerViewportSettings(const_ptr_this, viewportId, (uint)PerViewportSettings.All);
    }

    /// <summary>
    /// Deletes per viewport layer settings.
    /// </summary>
    /// <param name="viewportId">
    /// If not Guid.Empty, then the settings for that viewport are deleted.
    /// If Guid.Empty, then all per viewport settings are deleted.
    /// </param>
    /// <since>6.0</since>
    public void DeletePerViewportSettings(Guid viewportId)
    {
      IntPtr ptr_this = NonConstPointer();
      UnsafeNativeMethods.ON_Layer_DeletePerViewportSettings(ptr_this, viewportId, (uint)PerViewportSettings.All);
      InternalCommitChanges();
    }

    /// <summary>
    /// Gets or sets the display color for this layer.
    /// </summary>
    /// <since>5.0</since>
    public System.Drawing.Color Color
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        int argb = UnsafeNativeMethods.ON_Layer_GetColor(const_ptr_this, true);
        return System.Drawing.Color.FromArgb(argb);
      }
      set
      {
        IntPtr ptr_this = NonConstPointer();
        int argb = value.ToArgb();
        UnsafeNativeMethods.ON_Layer_SetColor(ptr_this, argb, true);
        InternalCommitChanges();
      }
    }

    /// <summary>
    /// Gets the display color for this layer.
    /// </summary>
    /// <param name="viewportId">If not Guid.Empty, then the setting applies only to the viewport with the specified id.</param>
    /// <returns>The display color.</returns>
    /// <since>6.0</since>
    public System.Drawing.Color PerViewportColor(Guid viewportId)
    {
      IntPtr const_ptr_this = ConstPointer();
      int argb = UnsafeNativeMethods.ON_Layer_GetPerViewportColor(const_ptr_this, viewportId, true);
      return System.Drawing.Color.FromArgb(argb);
    }

    /// <summary>
    /// Sets the display color for this layer.
    /// </summary>
    /// <param name="viewportId">If not Guid.Empty, then the setting applies only to the viewport with the specified id.</param>
    /// <param name="color">The display color.</param>
    /// <since>6.0</since>
    public void SetPerViewportColor(Guid viewportId, System.Drawing.Color color)
    {
      IntPtr ptr_this = NonConstPointer();
      UnsafeNativeMethods.ON_Layer_SetPerViewportColor(ptr_this, viewportId, color.ToArgb(), true);
      InternalCommitChanges();
    }

    /// <summary>
    /// Remove any per viewport layer color setting so the layer's overall setting will be used for all viewports.
    /// </summary>
    /// <param name="viewportId">
    /// If not Guid.Empty, then the setting for this viewport will be deleted.
    /// If Guid.Empty, the all per viewport layer color settings will be removed.
    /// </param>
    /// <since>6.0</since>
    public void DeletePerViewportColor(Guid viewportId)
    {
      IntPtr ptr_this = NonConstPointer();
      UnsafeNativeMethods.ON_Layer_DeletePerViewportSettings(ptr_this, viewportId, (uint)PerViewportSettings.Color);
      InternalCommitChanges();
    }

    /// <summary>
    /// Gets or sets the plot color for this layer.
    /// </summary>
    /// <since>5.0</since>
    public System.Drawing.Color PlotColor
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        int argb = UnsafeNativeMethods.ON_Layer_GetColor(const_ptr_this, false);
        return System.Drawing.Color.FromArgb(argb);
      }
      set
      {
        IntPtr ptr_this = NonConstPointer();
        int argb = value.ToArgb();
        UnsafeNativeMethods.ON_Layer_SetColor(ptr_this, argb, false);
        InternalCommitChanges();
      }
    }

    /// <summary>
    /// Gets the plot color for this layer.
    /// </summary>
    /// <param name="viewportId">If not Guid.Empty, then the setting applies only to the viewport with the specified id.</param>
    /// <returns>The plot color.</returns>
    /// <since>6.0</since>
    public System.Drawing.Color PerViewportPlotColor(Guid viewportId)
    {
      IntPtr const_ptr_this = ConstPointer();
      int argb = UnsafeNativeMethods.ON_Layer_GetPerViewportColor(const_ptr_this, viewportId, false);
      return System.Drawing.Color.FromArgb(argb);
    }

    /// <summary>
    /// Sets the plot color for this layer.
    /// </summary>
    /// <param name="viewportId">If not Guid.Empty, then the setting applies only to the viewport with the specified id.</param>
    /// <param name="color">The plot color.</param>
    /// <since>6.0</since>
    public void SetPerViewportPlotColor(Guid viewportId, System.Drawing.Color color)
    {
      IntPtr ptr_this = NonConstPointer();
      UnsafeNativeMethods.ON_Layer_SetPerViewportColor(ptr_this, viewportId, color.ToArgb(), false);
      InternalCommitChanges();
    }

    /// <summary>
    /// Remove any per viewport layer plot color setting so the layer's overall setting will be used for all viewports.
    /// </summary>
    /// <param name="viewportId">
    /// If not Guid.Empty, then the setting for this viewport will be deleted.
    /// If Guid.Empty, the all per viewport layer color settings will be removed.
    /// </param>
    /// <since>6.0</since>
    public void DeletePerViewportPlotColor(Guid viewportId)
    {
      IntPtr ptr_this = NonConstPointer();
      UnsafeNativeMethods.ON_Layer_DeletePerViewportSettings(ptr_this, viewportId, (uint)PerViewportSettings.PlotColor);
      InternalCommitChanges();
    }

    /// <summary>
    /// Gets or sets the weight of the plotting pen in millimeters. 
    /// A weight of 0.0 indicates the "default" pen weight should be used.
    /// A weight of -1.0 indicates the layer should not be printed.
    /// </summary>
    /// <since>5.0</since>
    public double PlotWeight
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        return UnsafeNativeMethods.ON_Layer_GetPlotWeight(const_ptr_this);
      }
      set
      {
        IntPtr ptr_this = NonConstPointer();
        UnsafeNativeMethods.ON_Layer_SetPlotWeight(ptr_this, value);
        InternalCommitChanges();
      }
    }

    /// <summary>
    /// Gets the plot weight, in millimeters, for this layer.
    /// </summary>
    /// <param name="viewportId">If not Guid.Empty, then the setting applies only to the viewport with the specified id.</param>
    /// <returns>The plot color.</returns>
    /// <since>6.0</since>
    public double PerViewportPlotWeight(Guid viewportId)
    {
      IntPtr const_ptr_this = ConstPointer();
      return UnsafeNativeMethods.ON_Layer_GetPerViewportPlotWeight(const_ptr_this, viewportId);
    }

    /// <summary>
    /// Sets the plot weight, in millimeters, for this layer.
    /// </summary>
    /// <param name="viewportId">If not Guid.Empty, then the setting applies only to the viewport with the specified id.</param>
    /// <param name="plotWeight">
    /// The plot weight in millimeters. 
    /// A weight of  0.0 indicates the "default" pen weight should be used. 
    /// A weight of -1.0 indicates the layer should not be printed.
    /// </param>
    /// <since>6.0</since>
    public void SetPerViewportPlotWeight(Guid viewportId, double plotWeight)
    {
      IntPtr ptr_this = NonConstPointer();
      UnsafeNativeMethods.ON_Layer_SetPerViewportPlotWeight(ptr_this, viewportId, plotWeight);
      InternalCommitChanges();
    }

    /// <summary>
    /// Remove any per viewport layer plot weight setting so the layer's overall setting will be used for all viewports.
    /// </summary>
    /// <param name="viewportId">
    /// If not Guid.Empty, then the setting for this viewport will be deleted.
    /// If Guid.Empty, the all per viewport layer color settings will be removed.
    /// </param>
    /// <since>6.0</since>
    public void DeletePerViewportPlotWeight(Guid viewportId)
    {
      IntPtr ptr_this = NonConstPointer();
      UnsafeNativeMethods.ON_Layer_DeletePerViewportSettings(ptr_this, viewportId, (uint)PerViewportSettings.PlotWeight);
      InternalCommitChanges();
    }

    /// <summary>
    /// Gets or sets the line-type index for this layer.
    /// </summary>
    /// <since>5.0</since>
    public int LinetypeIndex
    {
      get { return GetInt(UnsafeNativeMethods.LayerInt.LinetypeIndex); }
      set { SetInt(UnsafeNativeMethods.LayerInt.LinetypeIndex, value); }
    }

    /// <summary>
    /// Gets or sets the index of render material for objects on this layer that have
    /// MaterialSource() == MaterialFromLayer. 
    /// A material index of -1 indicates no material has been assigned 
    /// and the material created by the default Material constructor 
    /// should be used.
    /// </summary>
    /// <since>5.0</since>
    public int RenderMaterialIndex
    {
      get { return GetInt(UnsafeNativeMethods.LayerInt.RenderMaterialIndex); }
      set { SetInt(UnsafeNativeMethods.LayerInt.RenderMaterialIndex, value); }
    }

    /// <summary>
    /// Gets or sets the global visibility of this layer.
    /// </summary>
    /// <since>5.0</since>
    public bool IsVisible
    {
      get { return GetBool(UnsafeNativeMethods.LayerBool.IsVisible); }
      set { SetBool(UnsafeNativeMethods.LayerBool.IsVisible, value); }
    }

    /// <summary>
    /// Gets the per viewport visibility of this layer.
    /// </summary>
    /// <param name="viewportId">
    /// If not Guid.Empty, then the visibility setting for that viewport is returned.
    /// If Guid.Empty, the IsVisible property is returned.
    /// </param>
    /// <returns>Returns true if objects on layer are visible.</returns>
    /// <since>6.0</since>
    public bool PerViewportIsVisible(Guid viewportId)
    {
      IntPtr const_ptr_this = ConstPointer();
      return UnsafeNativeMethods.ON_Layer_PerViewportVisibility(const_ptr_this, viewportId, true);
    }

    /// <summary>
    /// Controls layer visibility in specific viewports.
    /// </summary>
    /// <param name="viewportId">
    /// If not Guid.Empty, then the setting applies only to the viewport with the specified id.
    /// If Guid.Empty, then the setting applies to all viewports with per viewport layer settings.
    /// </param>
    /// <param name="visible">true to make layer visible, false to make layer invisible.</param>
    /// <since>6.0</since>
    public void SetPerViewportVisible(Guid viewportId, bool visible)
    {
      IntPtr ptr_this = NonConstPointer();
      UnsafeNativeMethods.ON_Layer_SetPerViewportVisibility(ptr_this, viewportId, visible, true);
      InternalCommitChanges();
    }

    /// <summary>
    /// Remove any per viewport visibility setting so the layer's overall setting will be used for all viewports.
    /// </summary>
    /// <param name="viewportId">
    /// If not Guid.Empty, then the setting for this viewport will be deleted.
    /// If Guid.Empty, the all per viewport visibility settings will be removed.
    /// </param>
    /// <since>6.0</since>
    public void DeletePerViewportVisible(Guid viewportId)
    {
      IntPtr ptr_this = NonConstPointer();
      UnsafeNativeMethods.ON_Layer_DeletePerViewportSettings(ptr_this, viewportId, (uint)PerViewportSettings.Visible);
      InternalCommitChanges();
    }

    /// <summary>
    /// Gets the per layer persistent visibility. The persistent viability setting is used for layers whose visibility can be changed by a parent layer. 
    /// In this case, when a parent layer is turned off, then child layers are also turned off.
    /// The persistent visibility setting determines what happens when the parent is turned on again.
    /// </summary>
    /// <param name="viewportId"></param>
    /// <returns>
    /// Return true if this layer's visibility in the specified viewport is controlled by a parent object and the parent is turned on (after being off), 
    /// then this layer will also be turned on in the specified viewport.
    /// Returns false if this layer's visibility in the specified viewport is controlled by a parent object and the parent layer is turned on(after being off),
    /// then this layer will continue to be off in the specified viewport.
    /// </returns>
    /// <since>6.0</since>
    public bool PerViewportPersistentVisibility(Guid viewportId)
    {
      IntPtr const_ptr_this = ConstPointer();
      return UnsafeNativeMethods.ON_Layer_PerViewportVisibility(const_ptr_this, viewportId, false);
    }

    /// <summary>
    /// Sets the per layer persistent visibility. The persistent viability setting is used for layers whose visibility can be changed by a parent layer. 
    /// In this case, when a parent layer is turned off, then child layers are also turned off.
    /// The persistent visibility setting determines what happens when the parent is turned on again.
    /// </summary>
    /// <param name="viewportId">
    /// If not Guid.Empty, then the setting applies only to the viewport with the specified id.
    /// If Guid.Empty, then the setting applies to all viewports with per viewport layer settings.
    /// </param>
    /// <param name="persistentVisibility">
    /// If true, this layer's visibility in the specified viewport is controlled by a parent object and the parent is turned on (after being off), 
    /// then this layer will also be turned on in the specified viewport.
    /// If false, this layer's visibility in the specified viewport is controlled by a parent object and the parent layer is turned on (after being off),
    /// then this layer will continue to be off in the specified viewport.
    /// </param>
    /// <since>6.0</since>
    public void SetPerViewportPersistentVisibility(Guid viewportId, bool persistentVisibility)
    {
      IntPtr ptr_this = NonConstPointer();
      UnsafeNativeMethods.ON_Layer_SetPerViewportVisibility(ptr_this, viewportId, persistentVisibility, false);
      InternalCommitChanges();
    }

    /// <summary>
    /// Remove any per viewport persistent visibility setting so the layer's overall setting will be used for all viewports.
    /// </summary>
    /// <param name="viewportId">
    /// If not Guid.Empty, then the setting for this viewport will be deleted.
    /// If Guid.Empty, the all per viewport visibility settings will be removed.
    /// </param>
    /// <since>6.0</since>
    public void UnsetPerViewportPersistentVisibility(Guid viewportId)
    {
      IntPtr ptr_this = NonConstPointer();
      UnsafeNativeMethods.ON_Layer_DeletePerViewportSettings(ptr_this, viewportId, (uint)PerViewportSettings.PersistentVisibility);
      InternalCommitChanges();
    }

    /// <summary>
    /// Gets or sets a value indicating the locked state of this layer.
    /// </summary>
    /// <example>
    /// <code source='examples\vbnet\ex_locklayer.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_locklayer.cs' lang='cs'/>
    /// <code source='examples\py\ex_locklayer.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    public bool IsLocked
    {
      get { return GetBool(UnsafeNativeMethods.LayerBool.IsLocked); }
      set { SetBool(UnsafeNativeMethods.LayerBool.IsLocked, value); }
    }

    /// <summary>
    /// The global persistent visibility setting is used for layers whose visibility can
    /// be changed by a "parent" object. A common case is when a layer is a
    /// child layer (ParentId is not nil). In this case, when a parent layer is
    /// turned off, then child layers are also turned off. The persistent
    /// visibility setting determines what happens when the parent is turned on
    /// again.
    /// </summary>
    /// <remarks>
    /// Returns true if this layer's visibility is controlled by a parent
    /// object and the parent is turned on (after being off), then this
    /// layer will also be turned on.
    /// Returns false if this layer's visibility is controlled by a parent
    /// object and the parent layer is turned on (after being off), then
    /// this layer will continue to be off.
    /// 
    /// When the persistent viability is not explicitly set, this
    /// property returns the current value of IsVisible
    /// </remarks>
    /// <since>5.5</since>
    public bool GetPersistentVisibility()
    {
      return GetBool(UnsafeNativeMethods.LayerBool.PersistentVisibility);
    }

    /// <summary>
    /// Set the global persistent visibility setting for this layer.
    /// </summary>
    /// <param name="persistentVisibility"></param>
    /// <since>5.5</since>
    public void SetPersistentVisibility(bool persistentVisibility)
    {
      SetBool(UnsafeNativeMethods.LayerBool.PersistentVisibility, persistentVisibility);
    }

    /// <summary>
    /// Gets or sets the global persistent visibility setting for this layer.
    /// </summary>
    /// <since>8.0</since>
    public bool PersistentVisibility
    {
      get { return GetBool(UnsafeNativeMethods.LayerBool.PersistentVisibility); }
      set { SetBool(UnsafeNativeMethods.LayerBool.PersistentVisibility, value); }
    }

    /// <summary>
    /// Gets or sets the model visiblity of this layer.
    /// </summary>
    /// <since>8.0</since>
    public bool ModelIsVisible
    {
      get { return GetBool(UnsafeNativeMethods.LayerBool.ModelIsVisible); }
      set { SetBool(UnsafeNativeMethods.LayerBool.ModelIsVisible, value); }
    }

    /// <summary>
    /// Gets or sets the model persistent visibility of this layer.
    /// The persistent viability setting is used for layers whose visibility can be changed by a parent layer. 
    /// In this case, when a parent layer is turned off, then child layers are also turned off. 
    /// The persistent visibility setting determines what happens when the parent is turned on again.
    /// </summary>
    /// <since>8.0</since>
    public bool ModelPersistentVisibility
    {
      get { return GetBool(UnsafeNativeMethods.LayerBool.ModelPersistentVisibility); }
      set { SetBool(UnsafeNativeMethods.LayerBool.ModelPersistentVisibility, value); }
    }

    /// <summary>
    /// Remove any model persistent visibility setting from this layer.
    /// </summary>
    /// <since>8.0</since>
    public void UnsetModelPersistentVisibility()
    {
      IntPtr ptr_this = NonConstPointer();
      UnsafeNativeMethods.ON_Layer_UnsetModelPersistentVisibility(ptr_this);
    }

    /// <summary>
    /// Remove any model visibility setting so the layer's global setting will be used for all viewports.
    /// </summary>
    /// <since>8.0</since>
    public void DeleteModelVisible()
    {
      IntPtr ptr_this = NonConstPointer();
      UnsafeNativeMethods.ON_Layer_DeleteModelVisible(ptr_this);
    }

    /// <summary>
    /// Remove any explicit persistent visibility setting from this layer
    /// </summary>
    /// <since>5.5</since>
    public void UnsetPersistentVisibility()
    {
      IntPtr ptr_this = NonConstPointer();
      UnsafeNativeMethods.ON_Layer_UnsetPersistentVisibility(ptr_this);
      InternalCommitChanges();
    }

    /// <summary>
    /// The persistent locking setting is used for layers that can be locked by
    /// a "parent" object. A common case is when a layer is a child layer
    /// (Layer.ParentI is not nil). In this case, when a parent layer is locked,
    /// then child layers are also locked. The persistent locking setting
    /// determines what happens when the parent is unlocked again.
    /// </summary>
    /// <returns></returns>
    /// <since>5.5</since>
    public bool GetPersistentLocking()
    {
      return GetBool(UnsafeNativeMethods.LayerBool.PersistentLocking);
    }

    /// <summary>
    /// Set the persistent locking setting for this layer
    /// </summary>
    /// <param name="persistentLocking"></param>
    /// <since>5.5</since>
    public void SetPersistentLocking(bool persistentLocking)
    {
      SetBool(UnsafeNativeMethods.LayerBool.PersistentLocking, persistentLocking);
    }

    /// <summary>
    /// Remove any explicitly persistent locking settings from this layer
    /// </summary>
    /// <since>5.5</since>
    public void UnsetPersistentLocking()
    {
      IntPtr ptr_this = NonConstPointer();
      UnsafeNativeMethods.ON_Layer_UnsetPersistentLocking(ptr_this);
      InternalCommitChanges();
    }

    /// <summary>
    /// Gets or sets a value indicating whether this layer is expanded in the Rhino Layer dialog.
    /// </summary>
    /// <since>5.0</since>
    public bool IsExpanded
    {
      get { return GetBool(UnsafeNativeMethods.LayerBool.IsExpanded); }
      set { SetBool(UnsafeNativeMethods.LayerBool.IsExpanded, value); }
    }

#if RHINO_SDK

    /// <summary>
    /// Returns true if the layer has one or more selected Rhino objects.
    /// </summary>
    /// <param name="checkSubObjects">
    /// If true, the search will include objects that have some subset of the object selected, like some edges of a Brep.
    /// If false, objects where the entire object is not selected are ignored.</param>
    /// <returns>true if the layer has selected Rhino objects, false otherwise.</returns>
    /// <since>8.0</since>
    public bool HasSelectedObjects(bool checkSubObjects)
    {
      return UnsafeNativeMethods.CRhinoLayer_HasSelectedObjects(m_doc.RuntimeSerialNumber, Index, checkSubObjects);
    }

    /// <summary>
    /// Returns true if the layer is the current layer.
    /// </summary>
    /// <since>8.0</since>
    public bool IsCurrent
    {
      get
      {
        if (null == m_doc)
          return false;
        int currentIndex = UnsafeNativeMethods.CRhinoLayerTable_CurrentLayerIndex(m_doc.RuntimeSerialNumber);
        return Index == currentIndex;
      }
    }

    /// <summary>
    /// Returns true if the layer is a parent layer of the layer tree from a linked instance definition
    /// or the layer tree from a worksession reference model.
    /// </summary>
    /// <since>8.0</since>
    public bool IsReferenceParentLayer
    {
      get
      {
        if (null == m_doc)
          return false;
        return UnsafeNativeMethods.CRhinoLayer_IsReferenceParentLayer(m_doc.RuntimeSerialNumber, Index);
      }
    }

    /// <summary>
    /// Returns parent of a layer.
    /// </summary>
    /// <param name="rootLevelParent">
    /// If true, the root level parent is returned. The root level parent never has a parent.
    /// If false, the immediate parent is returned. The immediate parent may have a parent.
    /// </param>
    /// <returns>The parent layer, or null if the layer does not have a parent.</returns>
    /// <since>8.0</since>
    public Layer ParentLayer(bool rootLevelParent)
    {
      if (null == m_doc)
        return null;
      int parent = UnsafeNativeMethods.CRhinoLayer_ParentLayer(m_doc.RuntimeSerialNumber, Index, rootLevelParent);
      if (parent != RhinoMath.UnsetIntIndex)
        return new Layer(parent, m_doc);
      return null;
    }

    /// <summary>
    /// Gets or sets the <see cref="Render.RenderMaterial"/> for objects on
    /// this layer that have MaterialSource() == MaterialFromLayer.
    /// A null result indicates that no <see cref="Render.RenderMaterial"/> has
    /// been assigned  and the material created by the default Material
    /// constructor or the <see cref="RenderMaterialIndex"/> should be used.
    /// </summary>
    /// <since>5.7</since>
    public RenderMaterial RenderMaterial
    {
      get
      {
        // If set was called and the render material was changed or set to null
        // then return the cached set to render material.
        if (m_set_render_material_to_null || m_set_render_material != Guid.Empty)
        {
          var result = Render.RenderContent.FromId(m_doc, m_set_render_material) as RenderMaterial;
          return result;
        }
        // Get the document Id
        var doc_id = (null == m_doc ? 0 : m_doc.RuntimeSerialNumber);
        if (doc_id < 1) return null;
        var pointer = ConstPointer();
        // Get the render material associated with the layers render material
        // index into the documents material table.
        var id = UnsafeNativeMethods.Rdk_RenderContent_LayerMaterialInstanceId(doc_id, pointer);
        var content = Render.RenderContent.FromId(m_doc, id) as RenderMaterial;
        return content;
      }
      set
      {
        // Cache the new values Id and set a flag that tells CommitChanges() to
        // remove the render material Id as necessary.
        m_set_render_material_to_null = (value == null);
        m_set_render_material = value == null ? Guid.Empty : value.Id;
        InternalCommitChanges();
      }
    }

    private Guid m_set_render_material = Guid.Empty;
    private bool m_set_render_material_to_null;
#endif

    /// <summary>
    /// Runtime index used to sort layers in layer dialog.
    /// </summary>
    /// <since>5.0</since>
    public int SortIndex
    {
      get
      {
#if RHINO_SDK
        if (null == m_doc)
          return -1;
        int index = Index;
        return UnsafeNativeMethods.CRhinoLayer_SortIndex(m_doc.RuntimeSerialNumber, index);
#else
        return -1;
#endif
      }
    }

    int GetInt(UnsafeNativeMethods.LayerInt which)
    {
      IntPtr const_ptr_this = ConstPointer();
      return UnsafeNativeMethods.ON_Layer_GetInt(const_ptr_this, which);
    }
    void SetInt(UnsafeNativeMethods.LayerInt which, int val)
    {
      IntPtr ptr_this = NonConstPointer();
      UnsafeNativeMethods.ON_Layer_SetInt(ptr_this, which, val);
      InternalCommitChanges();
    }

    bool GetBool(UnsafeNativeMethods.LayerBool which)
    {
      IntPtr const_ptr_this = ConstPointer();
      return UnsafeNativeMethods.ON_Layer_GetSetBool(const_ptr_this, which, false, false);
    }
    void SetBool(UnsafeNativeMethods.LayerBool which, bool val)
    {
      IntPtr ptr_this = NonConstPointer();
      UnsafeNativeMethods.ON_Layer_GetSetBool(ptr_this, which, true, val);
      InternalCommitChanges();
    }
    #endregion

    /// <summary>
    /// Sets layer to default settings.
    /// </summary>
    /// <since>5.0</since>
    public void Default()
    {
      IntPtr ptr_this = NonConstPointer();
      UnsafeNativeMethods.ON_Layer_Default(ptr_this);
      InternalCommitChanges();
    }

    #region methods

    /// <summary>
    /// Copy typical attributes from another layer
    /// </summary>
    /// <param name="otherLayer"></param>
    /// <since>6.0</since>
    public void CopyAttributesFrom(Layer otherLayer)
    {
      IntPtr const_ptr_other = otherLayer.ConstPointer();
      IntPtr ptr_this = NonConstPointer();
      UnsafeNativeMethods.ON_Layer_CopyAttributes(ptr_this, const_ptr_other);
    }

#if RHINO_SDK
    /// <summary>
    /// Determines if a given string is valid for a layer name.
    /// </summary>
    /// <param name="name">A name to be validated.</param>
    /// <returns>true if the name is valid for a layer name; otherwise, false.</returns>
    /// <example>
    /// <code source='examples\vbnet\ex_addlayer.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_addlayer.cs' lang='cs'/>
    /// <code source='examples\py\ex_addlayer.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    /// <deprecated>8.0</deprecated>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    [Obsolete("Use ModelComponent.IsValidComponentName")]
    public static bool IsValidName(string name) => ModelComponent.IsValidComponentName(name);

    /// <summary>
    /// The string "::" (colon,colon) is used to
    /// separate parent and child layer names.
    /// </summary>
    /// <since>6.0</since>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    [Obsolete("Use ModelComponent.NamePathSeparator")]
    public static string PathSeparator => ModelComponent.NamePathSeparator;

    /// <summary>
    /// Get a layer name's "leaf" level name
    /// </summary>
    /// <param name="fullPath"></param>
    /// <returns>
    /// leaf name or String.Empty if fullPath does not contain a leaf
    /// </returns>
    /// <since>6.0</since>
    public static string GetLeafName(string fullPath)
    {
      using(var s = new StringWrapper())
      {
        IntPtr ptr_s = s.NonConstPointer;
        bool rc = UnsafeNativeMethods.ON_Layer_PathOperation(true, fullPath, ptr_s);
        if (rc)
          return s.ToString();
      }
      return String.Empty;
    }

    /// <summary>
    /// Get a layer name's "leaf" level name
    /// </summary>
    /// <param name="layer"></param>
    /// <returns>
    /// leaf name or String.Empty if fullPath does not contain a leaf
    /// </returns>
    /// <since>6.0</since>
    public static string GetLeafName(Layer layer)
    {
      return GetLeafName(layer.FullPath);
    }

    /// <summary>
    /// Get a layer's "parent" path name
    /// </summary>
    /// <param name="fullPath"></param>
    /// <returns>
    /// parent name or String.Empty
    /// </returns>
    /// <since>6.0</since>
    public static string GetParentName(string fullPath)
    {
      using (var s = new StringWrapper())
      {
        IntPtr ptr_s = s.NonConstPointer;
        bool rc = UnsafeNativeMethods.ON_Layer_PathOperation(false, fullPath, ptr_s);
        if (rc)
          return s.ToString();
      }
      return String.Empty;
    }

    /// <summary>
    /// Get a layer's "parent" path name
    /// </summary>
    /// <param name="layer"></param>
    /// <returns>
    /// parent name or String.Empty
    /// </returns>
    /// <since>6.0</since>
    public static string GetParentName(Layer layer)
    {
      return GetParentName(layer.FullPath);
    }

    /// <since>5.0</since>
    public bool IsChildOf(int layerIndex)
    {
      int index = Index;
      int rc = UnsafeNativeMethods.CRhinoLayerNode_IsChildOrParent(m_doc.RuntimeSerialNumber, index, layerIndex, true);
      return 1 == rc;
    }
    /// <since>5.0</since>
    public bool IsChildOf(Layer otherLayer)
    {
      return IsChildOf(otherLayer.Index);
    }
    /// <since>6.0</since>
    public bool IsChildOf(Guid otherlayerId)
    {
      var layer = m_doc.Layers.Find(otherlayerId, true, -1);
      return IsChildOf(layer);
    }

    /// <since>5.0</since>
    public bool IsParentOf(int layerIndex)
    {
      int index = Index;
      int rc = UnsafeNativeMethods.CRhinoLayerNode_IsChildOrParent(m_doc.RuntimeSerialNumber, index, layerIndex, false);
      return 1 == rc;
    }
    /// <since>5.0</since>
    public bool IsParentOf(Layer otherLayer)
    {
      return IsParentOf(otherLayer.Index);
    }
    /// <since>6.0</since>
    public bool IsParentOf(Guid otherLayer)
    {
      var layer = m_doc.Layers.Find(otherLayer, true, -1);
      return IsParentOf(layer);
    }

    /// <summary>
    /// Gets immediate children of this layer. Note that child layers may have their own children.
    /// </summary>
    /// <returns>Array of child layers, or null if this layer does not have any children.</returns>
    /// <since>5.0</since>
    public Layer[] GetChildren()
    {
      SimpleArrayInt child_indices = new SimpleArrayInt();
      int index = Index;
      int count = UnsafeNativeMethods.CRhinoLayerNode_GetChildren(m_doc.RuntimeSerialNumber, index, child_indices.m_ptr);
      Layer[] rc = null;
      if (count > 0)
      {
        int[] indices = child_indices.ToArray();
        count = indices.Length;
        rc = new Layer[count];
        for (int i = 0; i < count; i++)
        {
          rc[i] = new Layer(indices[i], m_doc);
        }
      }
      child_indices.Dispose();
      return rc;
    }

#endif

    /// <summary>
    /// Gets and sets the initial per viewport visibility of this layer in newly created detail views.
    /// </summary>
    /// <since>8.0</since>
    public bool PerViewportIsVisibleInNewDetails
    {
      get => GetBool(UnsafeNativeMethods.LayerBool.PerViewportIsVisibleInNewDetails);
      set => SetBool(UnsafeNativeMethods.LayerBool.PerViewportIsVisibleInNewDetails, value);
    }

    ///<summary>
    /// Get an optional custom section style associated with these attributes.
    ///</summary>
    /// <since>8.0</since>
    public SectionStyle GetCustomSectionStyle()
    {
      IntPtr const_ptr_this = ConstPointer();
      IntPtr ptr_sectionstyle = UnsafeNativeMethods.ON_Layer_GetCustomSectionStyle(const_ptr_this);
      if (ptr_sectionstyle == IntPtr.Zero)
        return null;
      return new SectionStyle(ptr_sectionstyle);
    }

    /// <since>8.0</since>
    public void SetCustomSectionStyle(SectionStyle sectionStyle)
    {
      if (sectionStyle == null)
      {
        RemoveCustomSectionStyle();
        return;
      }

      IntPtr ptr_this = NonConstPointer();
      IntPtr const_ptr_sectionstyle = sectionStyle.ConstPointer();
      UnsafeNativeMethods.ON_Layer_SetCustomSectionStyle(ptr_this, const_ptr_sectionstyle);
    }

    /// <since>8.0</since>
    public void RemoveCustomSectionStyle()
    {
      IntPtr ptr_this = NonConstPointer();
      UnsafeNativeMethods.ON_Layer_SetCustomSectionStyle(ptr_this, IntPtr.Zero);
    }
    #endregion

    #region user strings
    /// <summary>
    /// Attach a user string (key,value combination) to this geometry.
    /// </summary>
    /// <param name="key">id used to retrieve this string.</param>
    /// <param name="value">string associated with key.</param>
    /// <returns>true on success.</returns>
    /// <since>5.0</since>
    public bool SetUserString(string key, string value)
    {
      var rc = _SetUserString(key, value);
      InternalCommitChanges();
      return rc;
    }
    /// <summary>
    /// Gets user string from this geometry.
    /// </summary>
    /// <param name="key">id used to retrieve the string.</param>
    /// <returns>string associated with the key if successful. null if no key was found.</returns>
    /// <since>5.0</since>
    public string GetUserString(string key)
    {
      return _GetUserString(key);
    }

    /// <summary>
    /// Gets the amount of user strings.
    /// </summary>
    /// <since>5.0</since>
    public int UserStringCount
    {
      get
      {
        return _UserStringCount;
      }
    }

    /// <summary>
    /// Gets a copy of all (user key string, user value string) pairs attached to this geometry.
    /// </summary>
    /// <returns>A new collection.</returns>
    /// <since>5.0</since>
    public System.Collections.Specialized.NameValueCollection GetUserStrings()
    {
      return _GetUserStrings();
    }

    /// <since>6.0</since>
    public bool Equals(Layer other)
    {
      if (other == null) return false;

      // this is the only fixed thing among layers in ONX_Model,
      // layers in doc and new layers.
      return ConstPointer() == other.ConstPointer();
    }

    public override bool Equals(object obj)
    {
      return Equals(obj as Layer);
    } 

    /// <since>6.0</since>
    public static bool operator ==(Layer left, Layer right)
    {
      if ((object)left == null) return (object)right == null;
      return left.Equals(right);
    }

    /// <since>6.0</since>
    public static bool operator !=(Layer left, Layer right)
    {
      return !(left == right);
    }

    public override int GetHashCode()
    {
      return (int)ConstPointer().ToInt64();
    }
    #endregion
  }
}


#if RHINO_SDK
namespace Rhino.DocObjects.Tables
{
  /// <since>5.0</since>
  public enum LayerTableEventType
  {
    Added = 0,
    Deleted = 1,
    Undeleted = 2,
    Modified = 3,
    /// <summary>LayerTable.Sort() potentially changed sort order.</summary>
    Sorted = 4,
    /// <summary>Current layer change.</summary>
    Current = 5
  }

  public class LayerTableEventArgs : EventArgs
  {
    readonly uint m_doc_sn;
    readonly LayerTableEventType m_event_type;
    readonly int m_layer_index;
    readonly IntPtr m_ptr_old_layer;

    internal LayerTableEventArgs(uint docSerialNumber, int eventType, int index, IntPtr pConstOldLayer)
    {
      m_doc_sn = docSerialNumber;
      m_event_type = (LayerTableEventType)eventType;
      m_layer_index = index;
      m_ptr_old_layer = pConstOldLayer;
    }

    RhinoDoc m_doc;
    /// <since>5.0</since>
    public RhinoDoc Document
    {
      get { return m_doc ?? (m_doc = RhinoDoc.FromRuntimeSerialNumber(m_doc_sn)); }
    }

    /// <since>5.0</since>
    public LayerTableEventType EventType
    {
      get { return m_event_type; }
    }

    /// <since>5.0</since>
    public int LayerIndex
    {
      get { return m_layer_index; }
    }

    Layer m_new_layer;
    /// <since>5.0</since>
    public Layer NewState
    {
      get { return m_new_layer ?? (m_new_layer = new Layer(LayerIndex, Document)); }
    }

    Layer m_old_layer;
    /// <since>5.0</since>
    public Layer OldState
    {
      get
      {
        if (m_old_layer == null && m_ptr_old_layer!=IntPtr.Zero)
        {
          m_old_layer = new Layer(m_ptr_old_layer, true);
        }
        return m_old_layer;
      }
    }
  }


  public sealed class LayerTable :
    RhinoDocCommonTable<Layer>, ICollection<Layer>
  {
    internal LayerTable(RhinoDoc doc) : base(doc)
    {
    }

    /// <summary>Document that owns this table.</summary>
    /// <since>5.0</since>
    public new RhinoDoc Document
    {
      get { return m_doc; }
    }

    /// <summary>
    /// Returns number of layers in the layer table, including deleted layers.
    /// </summary>
    /// <since>5.0</since>
    public override int Count
    {
      get
      {
        return UnsafeNativeMethods.CRhinoLayerTable_LayerCount(m_doc.RuntimeSerialNumber, false);
      }
    }

    /// <summary>
    /// Returns number of layers in the layer table, excluding deleted layers.
    /// </summary>
    /// <since>5.0</since>
    public int ActiveCount
    {
      get
      {
        return UnsafeNativeMethods.CRhinoLayerTable_LayerCount(m_doc.RuntimeSerialNumber, true);
      }
    }

    /// <summary>
    /// Conceptually, the layer table is an array of layers.
    /// The operator[] can be used to get individual layers. A layer is
    /// either active or deleted and this state is reported by Layer.IsDeleted.
    /// </summary>
    /// <param name="index">zero based array index.</param>
    /// <returns>
    /// Reference to the layer.  If layer_index is out of range, the current
    /// layer is returned. Note that this reference may become invalid after
    /// AddLayer() is called.
    /// </returns>
    public Layer this[int index]
    {
      get
      {
        if (index < 0 || index >= Count)
          index = CurrentLayerIndex;
        return new Layer(index, m_doc);
      }
    }

    /// <summary>
    /// At all times, there is a "current" layer.  Unless otherwise specified, new objects
    /// are assigned to the current layer. The current layer is never locked, hidden, or deleted.
    /// Returns: Zero based layer table index of the current layer.
    /// </summary>
    /// <example>
    /// <code source='examples\vbnet\ex_moveobjectstocurrentlayer.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_moveobjectstocurrentlayer.cs' lang='cs'/>
    /// <code source='examples\py\ex_moveobjectstocurrentlayer.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    public int CurrentLayerIndex
    {
      get
      {
        return UnsafeNativeMethods.CRhinoLayerTable_CurrentLayerIndex(m_doc.RuntimeSerialNumber);
      }
    }

    /// <summary>
    /// At all times, there is a "current" layer. Unless otherwise specified, new objects
    /// are assigned to the current layer. The current layer is never locked, hidden, or deleted.
    /// </summary>
    /// <param name="layerIndex">
    /// Value for new current layer. 0 &lt;= layerIndex &lt; LayerTable.Count.
    /// The layer's mode is automatically set to NormalMode.
    /// </param>
    /// <param name="quiet">
    /// if true, then no warning message box pops up if the current layer request can't be satisfied.
    /// </param>
    /// <returns>true if current layer index successfully set.</returns>
    /// <since>5.0</since>
    public bool SetCurrentLayerIndex(int layerIndex, bool quiet)
    {
      return UnsafeNativeMethods.CRhinoLayerTable_SetCurrentLayerIndex(m_doc.RuntimeSerialNumber, layerIndex, quiet);
    }

    /// <summary>
    /// At all times, there is a "current" layer. Unless otherwise specified,
    /// new objects are assigned to the current layer. The current layer is
    /// never locked, hidden, or deleted.
    /// 
    /// Returns reference to the current layer. Note that this reference may
    /// become invalid after a call to AddLayer().
    /// </summary>
    /// <example>
    /// <code source='examples\vbnet\ex_sellayer.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_sellayer.cs' lang='cs'/>
    /// <code source='examples\py\ex_sellayer.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    public Layer CurrentLayer
    {
      get
      {
        return new Layer(CurrentLayerIndex, m_doc);
      }
    }

    /// <since>6.0</since>
    public override ModelComponentType ComponentType
    {
      get
      {
        return ModelComponentType.Layer;
      }
    }

    /// <summary>
    /// Finds the layer with a given name. If multiple layers exist that have the same name, the
    /// first match layer index will be returned.
    /// <para>Deleted layers have no name.</para>
    /// </summary>
    /// <param name="layerName">name of layer to search for. The search ignores case.</param>
    /// <param name="ignoreDeletedLayers">true means don't search deleted layers.</param>
    /// <returns>
    /// index of the layer with the given name.
    /// If no layer is found, the index of the default layer, -1, is returned.
    /// </returns>
    /// <example>
    /// <code source='examples\vbnet\ex_addlayer.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_addlayer.cs' lang='cs'/>
    /// <code source='examples\py\ex_addlayer.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    /// <deprecated>6.0</deprecated>
    [Obsolete("ignoreDeletedLayers is no longer supported for research by name. Use the overload with notFoundReturnValue (-1 was the previous default).")]
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public int Find(string layerName, bool ignoreDeletedLayers)
    {
      Layer found = FindNext(-1, layerName);
      return found == null ? -1 : found.Index;
    }

    /// <since>5.0</since>
    /// <deprecated>6.0</deprecated>
    [Obsolete("ignoreDeletedLayers is no longer supported for research by name. Use the overload with notFoundReturnValue (-1 was the previous default).")]
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public int FindNext(int index, string layerName, bool ignoreDeletedLayers)
    {
      Layer found = FindNext(index, layerName);
      return found == null ? -1 : found.Index;
    }

    /// <summary>
    /// Use FindName(name, index).
    /// </summary>
    /// <param name="index">Do not use.</param>
    /// <param name="layerName">Do not use.</param>
    /// <returns>Do not use.</returns>
    /// <since>6.0</since>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public Layer FindNext(int index, string layerName)
    {
      return FindName(layerName, index);
    }

    /// <since>5.0</since>
    /// <deprecated>6.0</deprecated>
    [Obsolete("ignoreDeletedLayers is no longer supported for research by name. Use the overload with notFoundReturnValue (-1 was the previous default).")]
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public int FindByFullPath(string layerPath, bool ignoreDeletedLayers)
    {
      return FindByFullPath(layerPath, -1);
    }

    /// <summary>
    /// Searches for a layer using the fully qualified name, that includes ancestors.
    /// <para>Deleted layers have no name.</para>
    /// </summary>
    /// <param name="layerPath">The full layer name.</param>
    /// <param name="notFoundReturnValue">Should be -1 to get the index of the OpenNURBS default layer,
    /// or <see cref="RhinoMath.UnsetIntIndex">UnsetIntIndex</see> to get an always-out-of-bound value.</param>
    /// <returns>The index of the found layer, or notFoundReturnValue.</returns>
    /// <since>6.0</since>
    public int FindByFullPath(string layerPath, int notFoundReturnValue)
    {
      if (string.IsNullOrEmpty(layerPath))
        return notFoundReturnValue;
      return UnsafeNativeMethods.CRhinoLayerTable_FindExact(m_doc.RuntimeSerialNumber, layerPath, notFoundReturnValue);
    }

    /// <summary>Finds a layer with a given name and matching parent ID.</summary>
    /// <param name="parentId">A valid layer ID.</param>
    /// <param name="layerName">name of layer to search for. The search ignores case.</param>
    /// <param name="ignoreDeletedLayers">If true, deleted layers are not checked. NOT SUPPORTED FOR NAME SEARCH, only for Guids.</param>
    /// <returns>
    /// >=0 index of the layer with the given name
    /// -1  no layer has the given name.
    /// </returns>
    /// <since>6.0</since>
    /// <deprecated>6.0</deprecated>
    [Obsolete("ignoreDeletedLayers is no longer supported for research by name. Use the overload with notFoundReturnValue (-1 was the previous default).")]
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public int Find(Guid parentId, string layerName, bool ignoreDeletedLayers)
    {
      return Find(parentId, layerName, -1);
    }

    /// <summary>Finds a layer with a given name and matching parent ID.</summary>
    /// <param name="parentId">A valid layer ID.</param>
    /// <param name="layerName">name of layer to search for. The search ignores case.</param>
    /// <param name="notFoundReturnValue">Should be -1 to get the index of the OpenNURBS default layer,
    /// or <see cref="RhinoMath.UnsetIntIndex">UnsetIntIndex</see> to get an always-out-of-bound value.</param>
    /// <returns>The index of the found layer, or notFoundReturnValue.</returns>
    /// <since>6.0</since>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public int Find(Guid parentId, string layerName, int notFoundReturnValue)
    {
      return UnsafeNativeMethods.CRhinoLayerTable_FindLayer3(m_doc.RuntimeSerialNumber, parentId, layerName, notFoundReturnValue);
    }

    /// <summary>Finds a layer with a matching ID.</summary>
    /// <param name="layerId">A valid layer ID.</param>
    /// <param name="ignoreDeletedLayers">If true, deleted layers are not checked.</param>
    /// <returns>
    /// >=0 index of the layer with the given name
    /// -1  no layer has the given name.
    /// </returns>
    /// <since>5.0</since>
    /// <deprecated>6.0</deprecated>
    [Obsolete("notFoundReturnValue should be specified. Add a third argument, its previous value was -1 but consider RhinoMath.UnsetIntIndex.")]
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public int Find(Guid layerId, bool ignoreDeletedLayers)
    {
      return Find(layerId, ignoreDeletedLayers, - 1);
    }

    /// <summary>Finds a layer with a matching ID.</summary>
    /// <param name="layerId">A valid layer ID.</param>
    /// <param name="ignoreDeletedLayers">If true, deleted layers are not checked.</param>
    /// <param name="notFoundReturnValue">Should be -1 to get the index of the OpenNURBS default layer,
    /// or <see cref="RhinoMath.UnsetIntIndex">UnsetIntIndex</see> to get an always-out-of-bound value.</param>
    /// <returns>The index of the found layer, or notFoundReturnValue.</returns>
    /// <since>6.0</since>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public int Find(Guid layerId, bool ignoreDeletedLayers, int notFoundReturnValue)
    {
      return UnsafeNativeMethods.CRhinoLayerTable_FindLayer2(m_doc.RuntimeSerialNumber, layerId, ignoreDeletedLayers, notFoundReturnValue);
    }

    /// <summary>
    /// Finds the layer with a given name. If multiple layers exist that have the same name, the
    /// first match layer index will be returned.
    /// <para>Deleted layers have no name.</para>
    /// <para>The default layer is NOT included in the search. If required, use the overload with startIndex input.</para>
    /// </summary>
    /// <param name="layerName">name of layer to search for. The search ignores case.</param>
    /// <returns>
    /// A layer, or null.
    /// </returns>
    /// <example>
    /// <code source='examples\vbnet\ex_addlayer.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_addlayer.cs' lang='cs'/>
    /// <code source='examples\py\ex_addlayer.py' lang='py'/>
    /// </example>
    /// <since>6.0</since>
    public Layer FindName(string layerName)
    {
      return FindName(layerName, 0);
    }

    /// <summary>
    /// Finds the next layer that has an index equal or higher than the searched value.
    /// <para>Search in case-insensitive.</para>
    /// </summary>
    /// <param name="startIndex">If you specify RhinoMath.UnsetIntIndex, then also default layers will be included.
    /// This is the first index that will be tested.</param>
    /// <param name="layerName">The layer to search for.</param>
    /// <returns>A layer, or null.</returns>
    /// <since>6.0</since>
    public Layer FindName(string layerName, int startIndex)
    {
      if (string.IsNullOrEmpty(layerName))
        return null;
      int found_index = RhinoMath.UnsetIntIndex;
      UnsafeNativeMethods.CRhinoLayerTable_FindNextLayer(m_doc.RuntimeSerialNumber, layerName, startIndex, ref found_index);
      if (found_index == RhinoMath.UnsetIntIndex)
        return null;
      return new Layer(found_index, m_doc);
    }

    /// <summary>
    /// Finds a Layer given its name hash.
    /// </summary>
    /// <param name="nameHash">The name hash of the Layer to be searched.</param>
    /// <returns>An Layer, or null on error.</returns>
    /// <since>6.0</since>
    public Layer FindNameHash(NameHash nameHash)
    {
      return __FindNameHashInternal(nameHash);
    }

    /// <summary>
    /// Retrieves a Layer object based on Index. This search type of search is discouraged.
    /// We are moving towards using only IDs for all tables.
    /// </summary>
    /// <param name="index">The index to search for.</param>
    /// <returns>A Layer object, or null if none was found.</returns>
    /// <since>6.0</since>
    public Layer FindIndex(int index)
    {
      return __FindIndexInternal(index);
    }

    /// <summary>
    /// Adds a new layer with specified definition to the layer table.
    /// </summary>
    /// <param name="layer">
    /// definition of new layer. The information in layer is copied. If layer.Name is empty
    /// the a unique name of the form "Layer 01" will be automatically created.
    /// </param>
    /// <returns>
    /// >=0 index of new layer
    /// -1  layer not added because a layer with that name already exists.
    /// </returns>
    /// <example>
    /// <code source='examples\vbnet\ex_addchildlayer.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_addchildlayer.cs' lang='cs'/>
    /// <code source='examples\py\ex_addchildlayer.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    public int Add(Layer layer)
    {
      if (null == layer)
        return -1;
      IntPtr const_ptr_layer = layer.ConstPointer();
      return UnsafeNativeMethods.CRhinoLayerTable_AddLayer(m_doc.RuntimeSerialNumber, const_ptr_layer, false);
    }

    /// <since>5.0</since>
    void ICollection<Layer>.Add(Layer item)
    {
      if (Add(item) < 0) throw new NotSupportedException("Could not add item.");
    }

    /// <summary>
    /// Adds a new layer with specified definition to the layer table.
    /// </summary>
    /// <param name="layerName">Name for new layer. Cannot be a null or zero-length string.</param>
    /// <param name="layerColor">Color of new layer. Alpha components will be ignored.</param>
    /// <returns>
    /// >=0 index of new layer
    /// -1  layer not added because a layer with that name already exists.
    /// </returns>
    /// <example>
    /// <code source='examples\vbnet\ex_addlayer.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_addlayer.cs' lang='cs'/>
    /// <code source='examples\py\ex_addlayer.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    public int Add(string layerName, System.Drawing.Color layerColor)
    {
      if (string.IsNullOrEmpty(layerName)) { return -1; }

      Layer layer = new Layer
      {
        Name = layerName,
        Color = layerColor
      };

      IntPtr const_ptr_layer = layer.ConstPointer();
      int rc = UnsafeNativeMethods.CRhinoLayerTable_AddLayer(m_doc.RuntimeSerialNumber, const_ptr_layer, false);
      layer.Dispose();
      return rc;
    }

    /// <summary>
    /// Adds a new reference layer with specified definition to the layer table
    /// Reference layers are not saved in files.
    /// </summary>
    /// <param name="layer">
    /// definition of new layer. The information in layer is copied. If layer.Name is empty
    /// the a unique name of the form "Layer 01" will be automatically created.
    /// </param>
    /// <returns>
    /// >=0 index of new layer
    /// -1  layer not added because a layer with that name already exists.
    /// </returns>
    /// <since>5.0</since>
    public int AddReferenceLayer(Layer layer)
    {
      if (null == layer)
        return -1;
      IntPtr const_ptr_layer = layer.ConstPointer();
      return UnsafeNativeMethods.CRhinoLayerTable_AddLayer(m_doc.RuntimeSerialNumber, const_ptr_layer, true);
    }

    /// <summary>
    /// Adds a new layer with default definition to the layer table.
    /// </summary>
    /// <returns>index of new layer.</returns>
    /// <since>5.0</since>
    public int Add()
    {
      return UnsafeNativeMethods.CRhinoLayerTable_AddLayer(m_doc.RuntimeSerialNumber, IntPtr.Zero, false);
    }
    /// <summary>
    /// Adds a new reference layer with default definition to the layer table.
    /// Reference layers are not saved in files.
    /// </summary>
    /// <returns>index of new layer.</returns>
    /// <since>5.0</since>
    public int AddReferenceLayer()
    {
      return UnsafeNativeMethods.CRhinoLayerTable_AddLayer(m_doc.RuntimeSerialNumber, IntPtr.Zero, true);
    }

    /// <summary>
    /// Adds all of the layer in the specified layer path, beginning with the root.
    /// Layer paths contain one or more valid layers names, with each name separated by <see cref="ModelComponent.NamePathSeparator"/>.
    /// For example, "Grandfather::Father::Son".
    /// </summary>
    /// <param name="layerPath">The layer path.</param>
    /// <returns>The index of the last layer created if successful, <see cref="RhinoMath.UnsetIntIndex"/> on failure.</returns>
    /// <since>8.0</since>
    public int AddPath(string layerPath)
    {
      return AddPath(layerPath, AppearanceSettings.DefaultLayerColor);
    }

    /// <summary>
    /// Adds all of the layer in the specified layer path, beginning with the root.
    /// Layer paths contain one or more valid layers names, with each name separated by <see cref="ModelComponent.NamePathSeparator"/>.
    /// For example, "Grandfather::Father::Son".
    /// </summary>
    /// <param name="layerPath">The layer path.</param>
    /// <param name="layerColor">The color of newly created layers. The colors of layers that already exist will not be changed.</param>
    /// <returns>The index of the last layer created if successful, <see cref="RhinoMath.UnsetIntIndex"/> on failure.</returns>
    /// <since>8.0</since>
    public int AddPath(string layerPath, System.Drawing.Color layerColor)
    {
      if (string.IsNullOrEmpty(layerPath))
        return -1;
 
      return UnsafeNativeMethods.CRhinoLayerUtilities_AddFromFullPathName(m_doc.RuntimeSerialNumber, layerPath, layerColor.ToArgb());
    }

    /// <summary>Modifies layer settings.</summary>
    /// <param name="newSettings">This information is copied.</param>
    /// <param name="layerIndex">
    /// zero based index of layer to set.  This must be in the range 0 &lt;= layerIndex &lt; LayerTable.Count.
    /// </param>
    /// <param name="quiet">if false, information message boxes pop up when illegal changes are attempted.</param>
    /// <returns>
    /// true if successful. false if layerIndex is out of range or the settings attempt
    /// to lock or hide the current layer.
    /// </returns>
    /// <since>5.0</since>
    public bool Modify(Layer newSettings, int layerIndex, bool quiet)
    {
      if (null == newSettings)
        return false;
      IntPtr const_ptr_layer = newSettings.ConstPointer();
      return UnsafeNativeMethods.CRhinoLayerTable_ModifyLayer(m_doc.RuntimeSerialNumber, const_ptr_layer, layerIndex, quiet);
    }
    /// <summary>Modifies layer settings.</summary>
    /// <param name="newSettings">This information is copied.</param>
    /// <param name="layerId">
    /// Id of layer.
    /// </param>
    /// <param name="quiet">if false, information message boxes pop up when illegal changes are attempted.</param>
    /// <returns>
    /// true if successful. false if layerIndex is out of range or the settings attempt
    /// to lock or hide the current layer.
    /// </returns>
    /// <since>6.0</since>
    public bool Modify(Layer newSettings, Guid layerId, bool quiet)
    {
      if (null == newSettings)
        return false;
      int layerIndex = Find(layerId, true, -1);

      return Modify(newSettings, layerIndex, quiet);
    }

    /// <summary>
    /// Makes a layer and all of its parent layers visible.
    /// </summary>
    /// <param name="layerId">The layer ID to be made visible.</param>
    /// <returns>true if the operation succeeded.</returns>
    /// <since>5.0</since>
    public bool ForceLayerVisible(Guid layerId)
    {
      return UnsafeNativeMethods.CRhinoLayerTable_ForceVisible(m_doc.RuntimeSerialNumber, layerId);
    }

    /// <summary>
    /// Makes a layer and all of its parent layers visible.
    /// </summary>
    /// <param name="layerIndex">The layer index to be made visible.</param>
    /// <returns>true if the operation succeeded.</returns>
    /// <since>5.0</since>
    public bool ForceLayerVisible(int layerIndex)
    {
      return ForceLayerVisible(this[layerIndex].Id);
    }

    /// <summary>
    /// Restores the layer to its previous state,
    /// if the layer has been modified and the modification can be undone.
    /// </summary>
    /// <param name="layerIndex">The layer index to be used.</param>
    /// <param name="undoRecordSerialNumber">The undo record serial number. Pass 0 not to specify one.</param>
    /// <returns>true if this layer had been modified and the modifications were undone.</returns>
    /// <since>5.0</since>
    [CLSCompliant(false)]
    public bool UndoModify(int layerIndex, uint undoRecordSerialNumber)
    {
      return UnsafeNativeMethods.CRhinoLayerTable_UndoModifyLayer(m_doc.RuntimeSerialNumber, layerIndex, undoRecordSerialNumber);
    }

    /// <summary>
    /// Restores the layer to its previous state,
    /// if the layer has been modified and the modification can be undone.
    /// </summary>
    /// <param name="layerIndex">The layer index to be used.</param>
    /// <returns>true if this layer had been modified and the modifications were undone.</returns>
    /// <since>5.0</since>
    public bool UndoModify(int layerIndex)
    {
      return UnsafeNativeMethods.CRhinoLayerTable_UndoModifyLayer(m_doc.RuntimeSerialNumber, layerIndex, 0);
    }

    /// <summary>
    /// Restores the layer to its previous state,
    /// if the layer has been modified and the modification can be undone.
    /// </summary>
    /// <param name="layerId">The layer Id to be used.</param>
    /// <param name="undoRecordSerialNumber">The undo record serial number. Pass 0 not to specify one.</param>
    /// <returns>true if this layer had been modified and the modifications were undone.</returns>
    /// <since>6.0</since>
    [CLSCompliant(false)]
    public bool UndoModify(Guid layerId, uint undoRecordSerialNumber)
    {
      int layerIndex = Find(layerId, true, -1);
      return UndoModify(layerIndex, undoRecordSerialNumber);
    }

    /// <summary>
    /// Restores the layer to its previous state,
    /// if the layer has been modified and the modification can be undone.
    /// </summary>
    /// <param name="layerId">The layer Id to be used.</param>
    /// <returns>true if this layer had been modified and the modifications were undone.</returns>
    /// <since>6.0</since>
    public bool UndoModify(Guid layerId)
    {
      int layerIndex = Find(layerId, true, -1);
      return UndoModify(layerIndex);
    }

    /// <summary>Deletes layer.</summary>
    /// <param name="layerIndex">
    /// zero based index of layer to delete. This must be in the range 0 &lt;= layerIndex &lt; LayerTable.Count.
    /// </param>
    /// <param name="quiet">
    /// If true, no warning message box appears if a layer the layer cannot be
    /// deleted because it is the current layer or it contains active geometry.
    /// </param>
    /// <returns>
    /// true if successful. false if layerIndex is out of range or the layer cannot be
    /// deleted because it is the current layer or because it layer contains active geometry.
    /// </returns>
    /// <since>5.0</since>
    public bool Delete(int layerIndex, bool quiet)
    {
      return UnsafeNativeMethods.CRhinoLayerTable_DeleteLayer(m_doc.RuntimeSerialNumber, layerIndex, quiet, false);
    }

    /// <summary>Deletes layer.</summary>
    /// <param name="layer">
    /// Layer to be deleted.
    /// </param>
    ///     /// <returns>
    /// true if successful. false if layerIndex is out of range or the layer cannot be
    /// deleted because it is the current layer or because it layer contains active geometry.
    /// </returns>
    /// <since>6.0</since>
    public override bool Delete(Layer layer)
    {
      return Delete(layer, true);
    }

    /// <summary>Deletes layer.</summary>
    /// <param name="layerId">
    /// Id of the layer to be deleted.
    /// </param>
    /// <param name="quiet">
    /// If true, no warning message box appears if a layer the layer cannot be
    /// deleted because it is the current layer or it contains active geometry.
    /// </param>
    /// <returns>
    /// true if successful. false if layerIndex is out of range or the layer cannot be
    /// deleted because it is the current layer or because it layer contains active geometry.
    /// </returns>
    /// <since>6.0</since>
    public bool Delete(Guid layerId, bool quiet)
    {
      int index = Find(layerId, true, -1);
      return index != -1 ? Delete(index, quiet) : false;
    }

    /// <summary>Deletes layer.</summary>
    /// <param name="layer">
    /// Layer to be deleted.
    /// </param>
    /// <param name="quiet">
    /// If true, no warning message box appears if a layer the layer cannot be
    /// deleted because it is the current layer or it contains active geometry.
    /// </param>
    /// <returns>
    /// true if successful. false if layerIndex is out of range or the layer cannot be
    /// deleted because it is the current layer or because it layer contains active geometry.
    /// </returns>
    /// <since>6.0</since>
    public bool Delete(Layer layer, bool quiet)
    {
      if (layer == null) return false;
      return Delete(layer.Index, quiet);
    }

    /// <summary>
    /// Deletes layers.
    /// </summary>
    /// <param name="layerIndices">An enumeration containing the indices of the layers to delete.</param>
    /// <param name="quiet">
    /// If true, no warning message boxes will appear.
    /// </param>
    /// <returns>the number of layers that were deleted.</returns>
    /// <since>8.0</since>
    public int Delete(IEnumerable<int> layerIndices, bool quiet)
    {
      using (var indices = new SimpleArrayInt(layerIndices))
      {
        IntPtr ptr_const_indices = indices.ConstPointer();
        return UnsafeNativeMethods.CRhinoLayerTable_DeleteLayers(m_doc.RuntimeSerialNumber, ptr_const_indices, quiet);
      }
    }

    /// <summary>
    /// Deletes a layer and all geometry objects on a layer
    /// </summary>
    /// <param name="layerIndex">
    /// zero based index of layer to delete. This must be in the range 0 &lt;= layerIndex &lt; LayerTable.Count.
    /// </param>
    /// <param name="quiet">
    /// If true, no warning message box appears if a layer the layer cannot be
    /// deleted because it is the current layer.
    /// </param>
    /// <returns>
    /// true if successful. false if layerIndex is out of range or the layer cannot be
    /// deleted because it is the current layer.
    /// </returns>
    /// <since>5.5</since>
    public bool Purge(int layerIndex, bool quiet)
    {
      return UnsafeNativeMethods.CRhinoLayerTable_DeleteLayer(m_doc.RuntimeSerialNumber, layerIndex, quiet, true);
    }

    /// <summary>
    /// Deletes a layer and all geometry objects on a layer.
    /// </summary>
    /// <param name="layerId">
    /// Id of the layer to purge.
    /// </param>
    /// <param name="quiet">
    /// If true, no warning message box appears if a layer the layer cannot be
    /// deleted because it is the current layer.
    /// </param>
    /// <returns>
    /// true if successful. false if layerIndex is out of range or the layer cannot be
    /// deleted because it is the current layer.
    /// </returns>
    /// <since>6.0</since>
    public bool Purge(Guid layerId, bool quiet)
    {
      int index = m_doc.Layers.Find(layerId, true, -1);
      return index != -1 ? Purge(index, quiet) : false;
    }

    //[skipping]
    // int DeleteLayers( int layer_index_count, const int* layer_index_list, bool  bQuiet );

    /// <summary>
    /// Undeletes a layer that has been deleted by DeleteLayer().
    /// </summary>
    /// <param name="layerIndex">
    /// zero based index of layer to undelete.
    /// This must be in the range 0 &lt;= layerIndex &lt; LayerTable.Count.
    /// </param>
    /// <returns>true if successful.</returns>
    /// <since>5.0</since>
    public bool Undelete(int layerIndex)
    {
      return UnsafeNativeMethods.CRhinoLayerTable_UndeleteLayer(m_doc.RuntimeSerialNumber, layerIndex);
    }

    /// <summary>
    /// Gets the next unused layer name used as default when creating new layers.
    /// </summary>
    /// <param name="ignoreDeleted">
    /// If this is true then Rhino may use a name used by a deleted layer.
    /// </param>
    /// <returns>An unused layer name string.</returns>
    /// <example>
    /// <code source='examples\vbnet\ex_addlayer.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_addlayer.cs' lang='cs'/>
    /// <code source='examples\py\ex_addlayer.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    /// <deprecated>6.0</deprecated>
    [Obsolete("'ignoreDeleted' is now redundant. Layers are now permanently removed. Use the overload with this argument.")]
    public string GetUnusedLayerName(bool ignoreDeleted)
    {
      return GetUnusedLayerName();
    }

    /// <summary>
    /// Gets the next unused layer name used as default when creating new layers.
    /// </summary>
    /// <returns>An unused layer name string.</returns>
    /// <example>
    /// <code source='examples\vbnet\ex_addlayer.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_addlayer.cs' lang='cs'/>
    /// <code source='examples\py\ex_addlayer.py' lang='py'/>
    /// </example>
    /// <since>6.0</since>
    public string GetUnusedLayerName()
    {
      using (var sh = new StringHolder())
      {
        IntPtr ptr_string = sh.NonConstPointer();
        UnsafeNativeMethods.CRhinoLayerTable_GetUnusedLayerName(m_doc.RuntimeSerialNumber, ptr_string);
        return sh.ToString();
      }
    }

    /// <summary>
    /// Returns the indices of layers that are selected on the Layer user interface.
    /// </summary>
    /// <param name="layerIndices">The indices of selected layers.</param>
    /// <returns>true if the layer user interface is visible, false otherwise.</returns>
    public bool GetSelected(out List<int> layerIndices)
    {
      layerIndices = new List<int>();
      var rc = false;
      using (var indices = new SimpleArrayInt())
      {
        IntPtr ptr_simplearray_int = indices.NonConstPointer();
        var func_rc = UnsafeNativeMethods.RHC_RhinoLayerManagerSelectedLayers(m_doc.RuntimeSerialNumber, ptr_simplearray_int);
        if (func_rc >= 0)
        {
          rc = true;
          layerIndices.AddRange(indices.ToArray());
        }
      }
      return rc;
    }

    /// <summary>
    /// Selects layers in the Layer user interface.
    /// </summary>
    /// <param name="layerIndices">The indices of layers to select.</param>
    /// <param name="bDeselect">If true, then any previously selected layers will be unselected.</param>
    /// <returns>true if the layer user interface is visible, false otherwise.</returns>
    /// <since>6.0</since>
    public bool Select(IEnumerable<int> layerIndices, bool bDeselect)
    {
      var rc = false;
      using (var indices = new SimpleArrayInt(layerIndices))
      {
        IntPtr ptr_simplearray_int = indices.NonConstPointer();
        var func_rc = UnsafeNativeMethods.RHC_RhinoLayerManagerSelectLayers(m_doc.RuntimeSerialNumber, ptr_simplearray_int, bDeselect);
        if (func_rc >= 0)
          rc = true;
      }
      return rc;
    }

    /// <summary>
    /// Duplicates, or copies, a layer. Duplicated layers are added to the document.
    /// </summary>
    /// <param name="layerIndex">The index of the layer to duplicate.</param>
    /// <param name="duplicateObjects">If true, then layer objects will also be duplicated and added to the document.</param>
    /// <param name="duplicateSublayers">If true, then all sub-layers of the layer will be duplicated.</param>
    /// <returns>The indices of the newly added layers if successful, an empty array on failure.</returns>
    /// <since>6.18</since>
    public int[] Duplicate(int layerIndex, bool duplicateObjects, bool duplicateSublayers)
    {
      return Duplicate(new int[] { layerIndex }, duplicateObjects, duplicateSublayers);
    }

    /// <summary>
    /// Duplicates, or copies, one or more layers. Duplicated layers are added to the document.
    /// </summary>
    /// <param name="layerIndices">The indices of layers to duplicate.</param>
    /// <param name="duplicateObjects">If true, then layer objects will also be duplicated and added to the document.</param>
    /// <param name="duplicateSublayers">If true, then all sub-layers of the layer will be duplicated.</param>
    /// <returns>The indices of the newly added layers if successful, an empty array on failure.</returns>
    /// <since>6.18</since>
    public int[] Duplicate(IEnumerable<int> layerIndices, bool duplicateObjects, bool duplicateSublayers)
    {
      using (var in_layers = new SimpleArrayInt(layerIndices))
      using (var out_layers = new SimpleArrayInt())
      {
        var ptr_const_in_layers = in_layers.ConstPointer();
        var ptr_out_layers = in_layers.NonConstPointer();
        var rc = UnsafeNativeMethods.RHC_RhinoDuplicateLayers(m_doc.RuntimeSerialNumber, ptr_const_in_layers, duplicateObjects, duplicateSublayers, ptr_out_layers);
        if (rc > 0)
          return out_layers.ToArray();
      }
      return new int[0];
    }

    /// <since>5.0</since>
    public override IEnumerator<Layer> GetEnumerator()
    {
      return base.GetEnumerator();
    }

    /// <summary>
    /// Updates the layer sort order
    /// </summary>
    /// <param name="layerIndices">The sort order.</param>
    /// <since>8.0</since>
    public void Sort(IEnumerable<int> layerIndices)
    {
      using (SimpleArrayInt in_indices = new SimpleArrayInt(layerIndices))
      {
        IntPtr ptr_const_in_indices = in_indices.ConstPointer();
        UnsafeNativeMethods.CRhinoLayerUtilities_UpdateSortIndices(m_doc.RuntimeSerialNumber, ptr_const_in_indices);
      }
    }
  }

}
#endif
