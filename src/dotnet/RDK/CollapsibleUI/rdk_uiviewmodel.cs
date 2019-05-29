
#pragma warning disable 1591

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Diagnostics;

using DE = Rhino.RDK.Delegates;
using Rhino.Runtime.InteropWrappers;

namespace Rhino.UI.Controls
{
  using IMPL = CppImplBase<InternalRdkViewModel, IRdkViewModel>;
  using IMPL_CALLBACKS = ICppImpl<IRdkViewModel>;

  public class InternalRdkViewModelFactory : FactoryBase
  {
    internal override IntPtr CreateCpp(int serial_number)
    {
      return UnsafeNativeMethods.Rdk_ControllerFactory_New(serial_number);
    }
  }

  /// <summary>
  /// ViewModel wraps IRhinoUiController
  /// </summary>
  /// 
  internal abstract class InternalRdkViewModel : IMPL_CALLBACKS, IRdkViewModel, IDisposable
  {

    public enum EventPriority
    {
      Low,
      Normal,
      High,
      RealTime
    }

    internal InternalRdkViewModel()
    {
      m_cpp_impl = new IMPL(this);
    }

    bool disposed = false;
    public virtual void Dispose()
    {
      if (!disposed)
      {
        disposed = true;
        if (m_cpp_impl != null)
        {
          var cpp = m_cpp_impl;
          m_cpp_impl = null;
          cpp.Dispose();
        }
      }
    }

    #region CppImplBase

    //Note that in this case, CppPointer is actually a pointer to a std::shared_ptr

    IMPL m_cpp_impl;
    IntPtr IMPL_CALLBACKS.CreateCppImpl(int serial) { return UnsafeNativeMethods.CRdkCmnUiController_NewSharedPtr(serial); }
    void IMPL_CALLBACKS.DeleteCppImpl(IntPtr cpp) { UnsafeNativeMethods.CRdkCmnUiController_DeleteSharedPtr(cpp); }
    IRdkViewModel IMPL_CALLBACKS.ToInterface() { return this; }
    public IntPtr CppPointer { get { Debug.Assert(m_cpp_impl != null); return m_cpp_impl == null ? IntPtr.Zero : m_cpp_impl.CppPointer; } }
    static public IRdkViewModel Find(IntPtr cpp) { return IMPL.Find(cpp); }

    //Special implementation because this is a pointer to a std::shared_ptr
    public bool IsSameObject(IntPtr cpp)
    {
      return UnsafeNativeMethods.IRhinoUiController_IsSameObject(cpp, m_cpp_impl.CppPointer);
    }

    static public IRdkViewModel NewNativeWrapper(IntPtr cpp) { return new NativeRdkViewModel(cpp); }
    #endregion

    public object GetData(Guid uuidDataType, bool bForWrite, bool bAutoChangeBracket  = true)
    {
      return _GetData(uuidDataType, CppPointer, bForWrite, bAutoChangeBracket);
    }

    public void Commit(Guid uuidDataType)
    {
      UnsafeNativeMethods.IRhinoUiController_Commit(CppPointer, uuidDataType);
    }

    public void Discard(Guid uuidDataType)
    {
      UnsafeNativeMethods.IRhinoUiController_Discard(CppPointer, uuidDataType);
    }

    internal static object _GetData(Guid uuidDataType, IntPtr cpp_pointer, bool bForWrite, bool bAutoChangeBracket)
    {
      IntPtr pData = UnsafeNativeMethods.IRhinoUiController_GetData(cpp_pointer, uuidDataType, bForWrite, bAutoChangeBracket);

      if (IntPtr.Zero != pData)
      {
        if (uuidDataType == Rhino.UI.Controls.DataSource.ProviderIds.RhinoSettings)
        {
          return new Rhino.Render.DataSources.RhinoSettings(pData);
        }
        else
        if (uuidDataType == Rhino.UI.Controls.DataSource.ProviderIds.Sun)
        {
          return new Rhino.Render.Sun(pData, bForWrite);
        }
        else
        if (uuidDataType == Rhino.UI.Controls.DataSource.ProviderIds.CurrentEnvironment)
        {
          return new Rhino.Render.CurrentEnvironmentImpl(pData);
        }
        else
        if (uuidDataType == Rhino.UI.Controls.DataSource.ProviderIds.Skylight)
        {
          return new Rhino.Render.Skylight(pData, bForWrite);
        }
        else
        if (uuidDataType == Rhino.UI.Controls.DataSource.ProviderIds.GroundPlane)
        {
          return new Rhino.Render.GroundPlane(pData, bForWrite);
        }
        else
        if (uuidDataType == Rhino.UI.Controls.DataSource.ProviderIds.LinearWorkflow)
        {
          return new Rhino.Render.LinearWorkflow(pData, bForWrite);
        }
        else
        if (uuidDataType == Rhino.UI.Controls.DataSource.ProviderIds.Dithering)
        {
          return new Rhino.Render.Dithering(pData, bForWrite);
        }
        if (uuidDataType == Rhino.UI.Controls.DataSource.ProviderIds.UndoRecord)
        {
          return new Rhino.Render.RdkUndoRecord(pData);
        }
        if (uuidDataType == Rhino.UI.Controls.DataSource.ProviderIds.Undo)
        {
          return new Rhino.Render.RdkUndo(pData);
        }
        if (uuidDataType == Rhino.UI.Controls.DataSource.ProviderIds.ContentLookup)
        {
          return new Rhino.Render.RdkContentLookup(pData);
        }
        if (uuidDataType == Rhino.UI.Controls.DataSource.ProviderIds.ContentDatabase)
        {
          return new Rhino.Render.RenderContentCollection(pData);
        }
        if (uuidDataType == Rhino.UI.Controls.DataSource.ProviderIds.ContentSelection)
        {
          return new Rhino.Render.RenderContentCollection(pData);
        }
        if (uuidDataType == Rhino.UI.Controls.DataSource.ProviderIds.ContentDisplayCollection)
        {
          return new Rhino.Render.RenderContentCollection(pData);
        }
        if (uuidDataType == Rhino.UI.Controls.DataSource.ProviderIds.ContentPreviewRendered)
        {
          return new Rhino.Render.ContentPreviewRendered(pData);
        }
        if (uuidDataType == Rhino.UI.Controls.DataSource.ProviderIds.PreviewSettings)
        {
          return new Rhino.Render.DataSources.PreviewSettings(pData);
        }
        if (uuidDataType == Rhino.UI.Controls.DataSource.ProviderIds.ContentEditorSettings)
        {
          return new Rhino.Render.DataSources.ContentEditorSettings(pData);
        }
        if (uuidDataType == Rhino.UI.Controls.DataSource.ProviderIds.NewContentControlAssignBy)
        {
          return new Rhino.Render.DataSources.NewContentControlAssignBy(pData);
        }
        if (uuidDataType == Rhino.UI.Controls.DataSource.ProviderIds.ContentUIs)
        {
          return new Rhino.Render.DataSources.RdkContentUIs(pData);
        }
        if (uuidDataType == Rhino.UI.Controls.DataSource.ProviderIds.SelectionNavigator)
        {
          return new Rhino.Render.DataSources.RdkSelectionNavigator(pData);
        }
        if (uuidDataType == Rhino.UI.Controls.DataSource.ProviderIds.ContentChildSlot)
        {
          return new Rhino.Render.DataSources.RdkContentChildSlot(pData);
        }
        if (uuidDataType == Rhino.UI.Controls.DataSource.ProviderIds.ImageFileInfo)
        {
          return new Rhino.Render.DataSources.ImageFileInfo(pData);
        }
        if (uuidDataType == Rhino.UI.Controls.DataSource.ProviderIds.NamedItem)
        {
          return new Rhino.Render.DataSources.NamedItems(pData);
        }
        if (uuidDataType == Rhino.UI.Controls.DataSource.ProviderIds.Decals)
        {
          return new Rhino.Render.DataSources.DecalDataSource(pData);
        }
        if (uuidDataType == Rhino.UI.Controls.DataSource.ProviderIds.RdkEdit)
        {
          return new Rhino.Render.DataSources.RdkEdit(pData);
        }
        if (uuidDataType == Rhino.UI.Controls.DataSource.ProviderIds.RdkRendering)
        {
          return new Rhino.Render.Rendering(pData);
        }
        if (uuidDataType == Rhino.UI.Controls.DataSource.ProviderIds.RdkRenderingProgress)
        {
          return new Rhino.Render.RenderingProgress(pData);
        }
        if (uuidDataType == Rhino.UI.Controls.DataSource.ProviderIds.RdkRenderingGamma)
        {
          return new Rhino.Render.RenderingGamma(pData);
        }
        if (uuidDataType == Rhino.UI.Controls.DataSource.ProviderIds.RdkRenderingToneMapping)
        {
          return new Rhino.Render.RenderingToneMapping(pData);
        }
        if (uuidDataType == Rhino.UI.Controls.DataSource.ProviderIds.RdkRenderingPostEffects)
        {
          return new Rhino.Render.RenderingPostEffects(pData);
        }
        if (uuidDataType == Rhino.UI.Controls.DataSource.ProviderIds.RdkRenderingPostEffectDOF)
        {
          return new Rhino.Render.RenderingPostEffectDOF(pData);
        }
        if (uuidDataType == Rhino.UI.Controls.DataSource.ProviderIds.RdkRenderingPostEffectFog)
        {
          return new Rhino.Render.RenderingPostEffectFog(pData);
        }
        if (uuidDataType == Rhino.UI.Controls.DataSource.ProviderIds.RdkRenderingPostEffectGlow)
        {
          return new Rhino.Render.RenderingPostEffectGlow(pData);
        }
        if (uuidDataType == Rhino.UI.Controls.DataSource.ProviderIds.RdkRenderingPostEffectGlare)
        {
          return new Rhino.Render.RenderingPostEffectGlare(pData);
        }
        if (uuidDataType == Rhino.UI.Controls.DataSource.ProviderIds.ContentSelectionForSetParams)
        {
          return new Rhino.Render.RenderContentCollection(pData);
        }
        if (uuidDataType == Rhino.UI.Controls.DataSource.ProviderIds.ContentSelectionForChangeType)
        {
          return new Rhino.Render.RenderContentCollection(pData);
        }
        if (uuidDataType == Rhino.UI.Controls.DataSource.ProviderIds.IORMenuData)
        {
          return new Rhino.Render.DataSources.RdkIORMenuData(pData);
        }
      }

      Debug.Assert(false);
      return null;
    }

    public virtual void RequiredDataSources(ref SimpleArrayGuid array)
    {

    }

    public EventPriority SetEventPriority(EventPriority ep)
    {
      int priority_value = UnsafeNativeMethods.IRhinoUiController_SetEventPriority(CppPointer, (int)ep);

      EventPriority event_priority = EventPriority.Normal;
      if (priority_value == 0)
        event_priority = EventPriority.Low;
      if (priority_value == 1)
        event_priority = EventPriority.Normal;
      if (priority_value == 2)
        event_priority = EventPriority.High;
      if (priority_value == 3)
        event_priority = EventPriority.RealTime;

      return event_priority;
    }

    #region Callbacks from C++
    internal static DE.FACTORYPROC get_viewmodel_proc = GetViewModelProc;
    private static IntPtr GetViewModelProc(int serial, Guid id)
    {
      if (InternalRdkViewModelFactory.m_factories.ContainsKey(serial))
      {
        var factory = InternalRdkViewModelFactory.m_factories[serial];
        return factory.Get(id);
      }

      return IntPtr.Zero;
    }

    internal static DE.SETGUIDEVENTINFOPROC on_event_proc = OnEventProc;
    private static void OnEventProc(int serial, Guid uuidData, IntPtr pEventInfo)
    {
      var viewmodel = IMPL.FromSerialNumber(serial);

      if (viewmodel != null && viewmodel.m_data_source_event != null)
      {
        viewmodel.m_data_source_event(viewmodel, new Rhino.UI.Controls.DataSource.EventInfoArgs(uuidData, pEventInfo));
      }
    }

    internal static DE.SETINTPTRPROC reguired_datasources = RequiredDatasources;
    private static void RequiredDatasources(int serial, IntPtr pArray)
    {
      var viewmodel = IMPL.FromSerialNumber(serial);

      if (viewmodel != null && viewmodel.m_data_source_event != null)
      {
        SimpleArrayGuid array = new SimpleArrayGuid(pArray);
        viewmodel.RequiredDataSources(ref array);
      }
    }

    private static DE.VOIDPROC on_delete_proc = OnDeleteProc;
    private static void OnDeleteProc(int sn)
    {
      var c = IMPL.FromSerialNumber(sn);
      if (c != null)
      {
        c.Dispose();
      }
    }

    static internal void SetCppHooks(bool bInitialize)
    {
      if (bInitialize)
      {
        UnsafeNativeMethods.Rdk_RhinoUiViewModelImpl_SetCallbacks(get_viewmodel_proc, on_event_proc, on_delete_proc, reguired_datasources);
      }
      else
      {
        UnsafeNativeMethods.Rdk_RhinoUiViewModelImpl_SetCallbacks(null, null, null, null);
      }
    }
    #endregion

    #region events
    internal EventHandler<Rhino.UI.Controls.DataSource.EventInfoArgs> m_data_source_event;
    public event EventHandler<Rhino.UI.Controls.DataSource.EventInfoArgs> DataChanged
    {
      add
      {
        m_data_source_event += value;
      }
      remove
      {
        m_data_source_event -= value;
      }
    }

    #endregion

    #region Undo Helper
    public NewUndoRecord NewUndoHelper(string description)
    {
      return new NewUndoRecord(description, this);
    }

    //This class is intended to be used in a using construct - so that undo
    //records are created neatly around property setting areas.
    public class NewUndoRecord : IDisposable
    {
      bool _disposed;
      Rhino.Render.RdkUndoRecord _undo_record;
      Rhino.Render.RdkUndo _undo;
      InternalRdkViewModel _view_model;

      public NewUndoRecord(string description, InternalRdkViewModel viewModel)
      {
        _view_model = viewModel;
        _undo = viewModel.GetData(Rhino.UI.Controls.DataSource.ProviderIds.Undo, false) as Rhino.Render.RdkUndo;
        _undo_record = _undo.NewUndoRecord();

        if(_undo_record != null)
          _undo_record.SetDescription(description);
      }

      public void Dispose()
      {
        Dispose(true);
        GC.SuppressFinalize(this);
      }

      ~NewUndoRecord()
      {
        Dispose(false);
      }

      protected virtual void Dispose(bool disposing)
      {
        if (_disposed)
          return;

        if (_undo_record != null)
        {
          _undo_record.Dispose();
        }
        _disposed = true;
      }
    }

    // Keep the previous UndoHelper implementation 
    // for the moment
    public class UndoHelper : IDisposable
    {
      bool _disposed;
      uint _undo_sn;
      RhinoDoc _doc;

      public UndoHelper(RhinoDoc doc, string description)
      {
        _doc = doc;
        _undo_sn = doc.BeginUndoRecord(description);
      }

      public void Dispose()
      {
        Dispose(true);
        GC.SuppressFinalize(this);
      }

      ~UndoHelper()
      {
        Dispose(false);
      }

      protected virtual void Dispose(bool disposing)
      {
        if (_disposed)
          return;

        if (_undo_sn > 0)
        {
          _doc.EndUndoRecord(_undo_sn);
        }
        _disposed = true;
      }
    }
    #endregion
  }
}