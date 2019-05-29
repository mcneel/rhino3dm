using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Rhino.UI.Controls
{
  /// <summary>
  /// Derive from this class to implement your own view model that wraps around a built
  /// in implementation of IRdkViewModel.  Use GetData etc to implement your properties.
  /// </summary>
  public class CollapsibleSectionViewModel : IRdkViewModel
  {
    /// <summary>
    /// Construct from your section - the view model should be a member of the section
    /// </summary>
    /// <param name="section"></param>
    public CollapsibleSectionViewModel(ICollapsibleSection section)
    {
      m_section = section;
    }

    /// <summary>
    /// Call for an interface to data
    /// </summary>
    /// <param name="uuidDataType"></param>
    /// <param name="bForWrite"></param>
    /// <param name="bAutoChangeBracket"></param>
    /// <returns></returns>
    public object GetData(Guid uuidDataType, bool bForWrite, bool bAutoChangeBracket = true)
    {
      var pController = UnsafeNativeMethods.IRhinoUiSection_ControllerSharedPtr(m_section.CppPointer);
      if (pController != IntPtr.Zero)
      {
        var o = InternalRdkViewModel._GetData(uuidDataType, pController, bForWrite, bAutoChangeBracket);
        UnsafeNativeMethods.IRhinoUiController_DeleteControllerSharedPtr(pController);
        return o;
      }
      return null;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="uuidDataType"></param>
    public void Commit(Guid uuidDataType)
    {
      var pController = UnsafeNativeMethods.IRhinoUiSection_ControllerSharedPtr(m_section.CppPointer);
      if (pController != IntPtr.Zero)
      {
        UnsafeNativeMethods.IRhinoUiController_Commit(pController, uuidDataType);
      }
      UnsafeNativeMethods.IRhinoUiController_DeleteControllerSharedPtr(pController);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="uuidDataType"></param>
    public void Discard(Guid uuidDataType)
    {
      var pController = UnsafeNativeMethods.IRhinoUiSection_ControllerSharedPtr(m_section.CppPointer);
      if (pController != IntPtr.Zero)
      {
        UnsafeNativeMethods.IRhinoUiController_Discard(pController, uuidDataType);
      }
      UnsafeNativeMethods.IRhinoUiController_DeleteControllerSharedPtr(pController);
    }

    /// <summary>
    /// Helper function to ease the use of undo records
    /// </summary>
    /// <param name="description"></param>
    /// <returns>Return the undo record</returns>
    public UndoRecord UndoHelper(string description)
    {
      return new UndoRecord(description, this);
    }

    /// <summary>
    /// 
    /// </summary>
    public IntPtr CppPointer 
    { 
      get 
      { 
        //TODO: this is a memory leak
        //return UnsafeNativeMethods.IRhinoUiSection_ControllerSharedPtr(m_section.CppPointer); 
        Debug.Assert(false);
        return IntPtr.Zero;
      } 
    }

    private ICollapsibleSection m_section;
  }



  /// <summary>
  /// Undo Record
  /// </summary>
  public class UndoRecord : IDisposable
  {
    bool _disposed;
    Rhino.Render.RdkUndoRecord _undo_record;
    IRdkViewModel _view_model;

    /// <summary>
    /// UndoRecord Constructor
    /// </summary>
    /// <param name="description"></param>
    /// <param name="viewModel"></param>
    /// <returns>Return the undo record</returns>
    public UndoRecord(string description, IRdkViewModel viewModel)
    {
      _view_model = viewModel;
      _undo_record = viewModel.GetData(Rhino.UI.Controls.DataSource.ProviderIds.UndoRecord, false, true) as Rhino.Render.RdkUndoRecord;
      _undo_record.SetDescription(description);
    }

    /// <summary>
    /// UndoRecord Dispose
    /// </summary>
    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    /// <summary>
    /// UndoRecord Destructor
    /// </summary>
    ~UndoRecord()
    {
      Dispose(false);
    }

    /// <summary>
    /// UndoRecord Dispose
    /// </summary>
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



  internal sealed class NativeRdkViewModel : IRdkViewModel
  {
    public NativeRdkViewModel(IntPtr native_ptr)
    {
      m_p_cpp = native_ptr;
    }

    public object GetData(Guid uuidDataType, bool bForWrite, bool bAutoChangeBracket = true)
    {
      return InternalRdkViewModel._GetData(uuidDataType, CppPointer, bForWrite, bAutoChangeBracket);
    }

    public void Commit(Guid uuidDataType)
    {
      UnsafeNativeMethods.IRhinoUiController_Commit(m_p_cpp, uuidDataType);
    }

    public void Discard(Guid uuidDataType)
    {
      UnsafeNativeMethods.IRhinoUiController_Discard(m_p_cpp, uuidDataType);
    }

    public IntPtr CppPointer { get { return m_p_cpp; } }
    internal IntPtr m_p_cpp = IntPtr.Zero;
  };
}
