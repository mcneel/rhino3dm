using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rhino.UI.Controls
{
    internal sealed class NativeCollapsibleSectionHolder : ICollapsibleSectionHolder
    {
        public int SectionCount
        {
            get
            {
                return UnsafeNativeMethods.IRhinoUiHolder_SectionCount(CppPointer);
            }
        }

        public void Dispose()
        {
            //Nothing to do - this thing doesn't own its pointer
        }

        public ICollapsibleSection SectionAt(int index)
        {
            IntPtr pSection = UnsafeNativeMethods.IRhinoUiHolder_SectionAt(CppPointer, index);
            if (pSection != null)
            {
                return new NativeCollapsibleSection(pSection);
            }
            return null;
        }

        public IEnumerable<ICollapsibleSection> Sections
        {
            get
            {
                var list = new List<ICollapsibleSection>();

                for (int i = 0; i < SectionCount; i++)
                {
                    ICollapsibleSection p = SectionAt(i);
                    list.Add(p);
                }

                return list;
            }
        }

        public void Add(ICollapsibleSection section)
        {
            UnsafeNativeMethods.IRhinoUiHolder_AddSection(CppPointer, section.CppPointer);
        }

        public void Remove(ICollapsibleSection section)
        {
            UnsafeNativeMethods.IRhinoUiHolder_RemoveSection(CppPointer, section.CppPointer);
        }

        public void Move(System.Drawing.Rectangle rect, bool bRepaint, bool bRepaintNC)
        {
            UnsafeNativeMethods.IRhinoUiWindow_Move(CppPointer, rect.Left, rect.Top, rect.Right, rect.Bottom, bRepaint, bRepaintNC);
        }

        public bool Created
        {
            get
            {
                return UnsafeNativeMethods.IRhinoUiWindow_IsCreated(CppPointer);
            }
        }

        public bool Shown
        {
            get
            {
                return UnsafeNativeMethods.IRhinoUiWindow_IsShown(CppPointer);
            }
            set
            {
                UnsafeNativeMethods.IRhinoUiWindow_Show(CppPointer, value);
            }
        }

        public bool Enabled
        {
            get
            {
                return UnsafeNativeMethods.IRhinoUiWindow_IsEnabled(CppPointer);
            }
            set
            {
                UnsafeNativeMethods.IRhinoUiWindow_Enable(CppPointer, value);
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

        public IntPtr Parent
        {
            set
            {
                UnsafeNativeMethods.IRhinoUiWindow_SetParent(CppPointer, value);
            }

            get 
            {
                return CppPointer;
            }
        }

        public NativeCollapsibleSectionHolder(IntPtr native_ptr)
        {
            m_p_cpp = native_ptr;
        }

        public IntPtr Window
    {
            get
            {
                return UnsafeNativeMethods.IRhinoUiWindow_GetWindow(CppPointer);
            }
        }

        public System.Drawing.Color BackgroundColor
        {
            set
            {
                UnsafeNativeMethods.IRhinoUiHolder_SetBackgroundColor(m_p_cpp, value.ToArgb());
            }
        }

        public int TopMargin 
        { 
            get { return UnsafeNativeMethods.IRhinoUiHolder_GetMargin(m_p_cpp, 0); }
            set { UnsafeNativeMethods.IRhinoUiHolder_SetMargin(m_p_cpp, 0, value); }
        }

        public int BottomMargin
        {
            get { return UnsafeNativeMethods.IRhinoUiHolder_GetMargin(m_p_cpp, 1); }
            set { UnsafeNativeMethods.IRhinoUiHolder_SetMargin(m_p_cpp, 1, value); }
        }

        public int LeftMargin
        {
            get { return UnsafeNativeMethods.IRhinoUiHolder_GetMargin(m_p_cpp, 2); }
            set { UnsafeNativeMethods.IRhinoUiHolder_SetMargin(m_p_cpp, 2, value); }
        }

        public int RightMargin
        {
            get { return UnsafeNativeMethods.IRhinoUiHolder_GetMargin(m_p_cpp, 3); }
            set { UnsafeNativeMethods.IRhinoUiHolder_SetMargin(m_p_cpp, 3, value); }
        }

        public string EmptyText
        {
            set
            {
                UnsafeNativeMethods.IRhinoUiHolder_SetEmptyText(m_p_cpp, value);
            }
        }

        public bool IsSectionExpanded(ICollapsibleSection section)
        {
            return UnsafeNativeMethods.IRhinoUiHolder_IsSectionExpanded(m_p_cpp, section.CppPointer);
        }

        public void ExpandSection(ICollapsibleSection section, bool expand, bool ensureVisible)
        {
            UnsafeNativeMethods.IRhinoUiHolder_ExpandSection(m_p_cpp, section.CppPointer, expand, ensureVisible);
        }

        public int ScrollPosition 
        { 
            get
            {
                return UnsafeNativeMethods.IRhinoUiHolder_GetScrollPosition(m_p_cpp);
            }
            set
            {
                UnsafeNativeMethods.IRhinoUiHolder_SetScrollPosition(m_p_cpp, value, true);
            }
        }

        public string SettingsPathSubKey
        {
            set
            {
                UnsafeNativeMethods.IRhinoUiHolder_SetSettingsPathSubKey(m_p_cpp, value);
            }
        }

        public void UpdateAllViews(int flags)
        {
            UnsafeNativeMethods.IRhinoUiHolder_UpdateAllViews(m_p_cpp, flags);
        }

        public IntPtr CppPointer { get { return m_p_cpp; } }
        internal IntPtr m_p_cpp = IntPtr.Zero;
    }
}
