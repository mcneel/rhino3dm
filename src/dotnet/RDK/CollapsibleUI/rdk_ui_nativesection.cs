using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rhino.UI.Controls
{
    internal sealed class NativeCollapsibleSection : ICollapsibleSection
    {
        public IntPtr CppPointer 
        { 
            get
            {
                return m_p_cpp;
            }
        }

        public void Dispose()
        {
            //Nothing to do - this thing doesn't own its pointer
        }

        public IRdkViewModel ViewModel
        {
            get
            {
                IntPtr pViewModel = UnsafeNativeMethods.IRhinoUiSection_ControllerSharedPtr(m_p_cpp);

                var vm = InternalRdkViewModel.Find(pViewModel);

                UnsafeNativeMethods.IRhinoUiController_DeleteControllerSharedPtr(pViewModel);

                return vm;
            }
        }

        public void Move(System.Drawing.Rectangle pos, bool bRepaint, bool bRepaintBorder)
        {
            UnsafeNativeMethods.IRhinoUiWindow_Move(m_p_cpp, pos.Left, pos.Top, pos.Right, pos.Bottom, bRepaint, bRepaintBorder);
        }

        public bool Created
        {
            get
            {
                return UnsafeNativeMethods.IRhinoUiWindow_IsCreated(m_p_cpp);
            }
        }

        public bool Shown
        {
            get
            {
                return UnsafeNativeMethods.IRhinoUiWindow_IsShown(m_p_cpp);
            }
            set
            {
                UnsafeNativeMethods.IRhinoUiWindow_Show(m_p_cpp, value);
            }
        }

        public bool Enabled
        {
            get
            {
                return UnsafeNativeMethods.IRhinoUiWindow_IsEnabled(m_p_cpp);
            }
            set
            {
                UnsafeNativeMethods.IRhinoUiWindow_Enable(m_p_cpp, value);
            }
        }

        public int Height
        {
            get
            {
                return UnsafeNativeMethods.IRhinoUiSection_GetHeight(m_p_cpp);
            }
        }

        public bool Hidden
        {
            get
            {
                return UnsafeNativeMethods.IRhinoUiSection_IsHidden(m_p_cpp);
            }
        }

        public bool InitiallyExpanded
        {
            get
            {
                return UnsafeNativeMethods.IRhinoUiSection_IsInitiallyExpanded(m_p_cpp);
            }
        }

        public IntPtr Parent
        {
            set
            {
                UnsafeNativeMethods.IRhinoUiWindow_SetParent(m_p_cpp, value);
            }

            get
            {
                return m_p_cpp;
            }
        }

    public LocalizeStringPair Caption
    {
      get
      {
        using (var english = new Rhino.Runtime.InteropWrappers.StringHolder())
        using (var local = new Rhino.Runtime.InteropWrappers.StringHolder())
        {
          var p_english = english.NonConstPointer();
          UnsafeNativeMethods.IRhinoUiWindow_Caption(CppPointer, p_english, true);

          var p_local = local.NonConstPointer();
          UnsafeNativeMethods.IRhinoUiWindow_Caption(CppPointer, p_local, false);

          return new LocalizeStringPair(english.ToString(), local.ToString());
        }
      }
    }

    public Guid Id
        {
            get { return UnsafeNativeMethods.IRhinoUiSection_Uuid(m_p_cpp); }
        }

    public Guid ViewModelId
    {
      get { return UnsafeNativeMethods.IRhinoUiSection_ControllerId(m_p_cpp); }
    }

    public string SettingsTag
        {
            get
            {
                using (var sh = new Rhino.Runtime.InteropWrappers.StringHolder())
                {
                    var p_string = sh.NonConstPointer();
                    UnsafeNativeMethods.IRhinoUiSection_SettingsTag(m_p_cpp, p_string);
                    return sh.ToString();
                }
            }
        }

    public Guid PlugInId
    {
      get
      {
        return UnsafeNativeMethods.IRhinoUiSection_PlugInId(m_p_cpp);
      }
    }

    public Rhino.UI.LocalizeStringPair CommandOptionName
    {
      get
      {
        using (var english = new Rhino.Runtime.InteropWrappers.StringHolder())
        using (var local = new Rhino.Runtime.InteropWrappers.StringHolder())
        {
          var p_english = english.NonConstPointer();
          UnsafeNativeMethods.IRhinoUiSection_CommandOptionName(m_p_cpp, p_english, true);

          var p_local = local.NonConstPointer();
          UnsafeNativeMethods.IRhinoUiSection_CommandOptionName(m_p_cpp, p_local, false);

          return new LocalizeStringPair(english.ToString(), local.ToString());
        }
      }
    }

    public int RunScript(IRdkViewModel vm)
    {
      //Not implemented.
      //return UnsafeNativeMethods.IRhinoUiSection_RunScript(m_p_cpp);
      return 0x00FFFFFF;
    }

    public bool Collapsible
        {
            get { return UnsafeNativeMethods.IRhinoUiSection_Collapsible(m_p_cpp); }
        }

        public System.Drawing.Color BackgroundColor
        {
            get
            {
                return System.Drawing.Color.FromArgb(UnsafeNativeMethods.IRhinoUiSection_BackgroundColor(m_p_cpp));
            }
            set
            {
                UnsafeNativeMethods.IRhinoUiSection_SetBackgroundColor(m_p_cpp, value.ToArgb());
            }
        }

        public IntPtr Window
    {
            get { return UnsafeNativeMethods.IRhinoUiSection_GetWindow(m_p_cpp); }
        }

        public NativeCollapsibleSection(IntPtr native_ptr)
        {
            m_p_cpp = native_ptr;
        }

        internal IntPtr m_p_cpp = IntPtr.Zero;
    }
}




