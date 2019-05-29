#pragma warning disable 1591

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Diagnostics;

using DE = Rhino.RDK.Delegates;

namespace Rhino.UI.Controls
{
    using IMPL = CppImplBase<CollapsibleSectionHolderImpl, ICollapsibleSectionHolder>;
    using IMPL_CALLBACKS = ICppImpl<ICollapsibleSectionHolder>;

    public sealed class CollapsibleSectionHolderImpl : IMPL_CALLBACKS, IDisposable
    {
        public CollapsibleSectionHolderImpl(ICollapsibleSectionHolder client)
        {
            m_client = client;
            m_cpp_impl = new IMPL(this);
        }

        bool disposed = false;
        public void Dispose()
        {
            if (!disposed)
            {
                disposed = true;

                // This needs more investigation. The client (HostEtoCollapsibleSectionHolder)
                // if disposed here will cause a crash when openening a new model. 
                //if (m_client != null)
                //{
                //    var client = m_client;
                //    m_client = null;
                //    client.Dispose();
                //}

                if (m_cpp_impl != null)
                {
                    var cpp = m_cpp_impl;
                    m_cpp_impl = null;
                    cpp.Dispose();
            }
            }
        }

        internal ICollapsibleSectionHolder m_client;

        #region CppImplBase
        IMPL m_cpp_impl;
        IntPtr IMPL_CALLBACKS.CreateCppImpl(int serial) { return UnsafeNativeMethods.CRdkCmnUiHolder_New(serial); }
        void IMPL_CALLBACKS.DeleteCppImpl(IntPtr cpp) { UnsafeNativeMethods.CRdkCmnUiHolder_Delete(cpp); }
        ICollapsibleSectionHolder IMPL_CALLBACKS.ToInterface() { return m_client; }
        public IntPtr CppPointer { get { Debug.Assert(m_cpp_impl != null); return m_cpp_impl == null ? IntPtr.Zero : m_cpp_impl.CppPointer; } }
        static public ICollapsibleSectionHolder Find(IntPtr cpp) { return IMPL.Find(cpp); }
        public bool IsSameObject(IntPtr cpp) { return cpp == m_cpp_impl.CppPointer; }
        static public ICollapsibleSectionHolder NewNativeWrapper(IntPtr cpp)   { return new NativeCollapsibleSectionHolder(cpp); }
        #endregion

        #region Calls from IRhinoUiSection (C++ code)
        internal static void SetCppHooks(bool bInitialize)
        {
            if (bInitialize)
            {
                UnsafeNativeMethods.Rdk_UiHolder_SetCallbacks(move_proc, show_proc, delete_this_proc, is_shown_proc, is_enabled_proc,
                                      enable_proc, update_proc, attach_proc, detach_proc, section_count_proc, section_at_proc, set_parent_proc);
            }
            else
            {
                UnsafeNativeMethods.Rdk_UiHolder_SetCallbacks(null, null, null, null, null, null, null, null, null, null, null, null);
            }
        }

        //Special delegates for this class
        internal delegate int    ATTACHSECTIONPROC(int sn, IntPtr p);
        public   delegate IntPtr CREATEFROMCPPPROC(IntPtr hwndParent);

        private static DE.SETINTPTRPROC set_parent_proc = SetParentProc;
        private static void SetParentProc(int sn, IntPtr hwndParent)
        {
            if(hwndParent != IntPtr.Zero)
              IMPL.SetClientProperty("Parent", sn, hwndParent);
        }

        private static Rhino.UI.Controls.Delegates.MOVEPROC move_proc = MoveProc;
        private static void MoveProc(int serialNumber, int l, int t, int r, int b, int bRepaint, int bRepaintNC)
        {
            var holder = IMPL.FromSerialNumber(serialNumber);
            if (holder != null && holder.m_client != null)
            {
              holder.m_client.Move(new System.Drawing.Rectangle(l, t, r - l, b - t), 0!=bRepaint, 0!=bRepaintNC);
            }
        }

        private static DE.SETBOOLPROC show_proc = ShowProc;
        private static DE.GETBOOLPROC is_shown_proc = IsShownProc;
        private static void ShowProc(int sn, int bShow)
        {
            IMPL.SetClientProperty("Shown", sn, bShow);
        }
        private static int IsShownProc(int sn)
        {
            return IMPL.GetClientProperty<int>("Shown", sn);
        }

        private static DE.VOIDPROC delete_this_proc = DeleteThisProc;
        private static void DeleteThisProc(int serialNumber)
        {
          var c = IMPL.FromSerialNumber(serialNumber);
          Debug.Assert(c != null);

          if (c != null)
          {
            c.Dispose();
          }
        }

        private static DE.GETBOOLPROC is_enabled_proc = IsEnabledProc;
        private static DE.SETBOOLPROC enable_proc = EnableProc;

        private static int IsEnabledProc(int sn)
        {
            return IMPL.GetClientProperty<int>("Enabled", sn);
        }
        private static void EnableProc(int sn, int bShow)
        {
            IMPL.SetClientProperty("Enabled", sn, bShow);
        }

        private static DE.VOIDPROC update_proc = UpdateAllViewsProc;
        private static void UpdateAllViewsProc(int serialNumber)
        {
            var holder = IMPL.FromSerialNumber(serialNumber);
            if (holder != null)
            {
               // foreach (var section in holder.Sections)
               // {
               //     //section.UpdateData();
               //}
            }
        }

        private static ATTACHSECTIONPROC attach_proc = AttachSectionProc;
        private static int AttachSectionProc(int serialNumber, IntPtr isection)
        {
            var holder = IMPL.FromSerialNumber(serialNumber);
            if (holder != null && holder.m_client != null)
            {
                var cs = CollapsibleSectionImpl.Find(isection);
                holder.m_client.Add(cs);

                return DE.ToInt(true);
            }
            return DE.ToInt(false);
        }

        private static ATTACHSECTIONPROC detach_proc = DetachSectionProc;
        private static int DetachSectionProc(int serialNumber, IntPtr isection)
        {
            var holder = IMPL.FromSerialNumber(serialNumber);
            if (holder != null && holder.m_client != null)
            {
                var cs = CollapsibleSectionImpl.Find(isection);
                holder.m_client.Remove(cs);
                return DE.ToInt(true);
            }
            return DE.ToInt(false);
        }

        private static DE.GETINTPROC section_count_proc = SectionCountProc;
        private static int SectionCountProc(int sn)
        {
            return IMPL.GetClientProperty<int>("SectionCount", sn, 0);
        }

        private static Rhino.RDK.Delegates.ATINDEXPROC section_at_proc = SectionAtProc;
        private static IntPtr SectionAtProc(int serialNumber, int index)
        {
            var holder = IMPL.FromSerialNumber(serialNumber);
            if (holder != null && holder.m_client != null)
            {
                if (index < holder.m_client.SectionCount)
                {
                    var cs = holder.m_client.SectionAt(index);
                    if (cs != null)
                    {
                        return cs.CppPointer;
                    }
                }
            }
            return IntPtr.Zero;
        }
        #endregion
    }
}