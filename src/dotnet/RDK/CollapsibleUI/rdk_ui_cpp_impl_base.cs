using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace Rhino.UI.Controls
{
    interface ICppImpl<INTERFACE>
    {
        IntPtr CreateCppImpl(int serial_number);
        void DeleteCppImpl(IntPtr cpp);
        INTERFACE ToInterface();
        bool IsSameObject(IntPtr cpp);
    }

    internal sealed class CppImplBase<CONCRETE_CLASS, INTERFACE> : IDisposable
        where CONCRETE_CLASS : class, ICppImpl<INTERFACE>
        where INTERFACE : class
    {
        internal CppImplBase(CONCRETE_CLASS concrete)
        {
            m_concrete = concrete;
            m_serial_number = g_current_serial_number++;
            all.Add(m_serial_number, this);
            m_p_cpp = concrete.CreateCppImpl(m_serial_number);
        }

        ~CppImplBase()
        {
            Dispose();
        }

        //No need to implement Dispose as a virtual function
        //since this is a sealed class
        public void Dispose()
        {
          if (!bDisposed)
          {
            if (m_p_cpp != IntPtr.Zero)
            {
              //We created this thing, so we have to delete it
              m_concrete.DeleteCppImpl(m_p_cpp);
              m_p_cpp = IntPtr.Zero;
            }

            //Remove the object from the serial number lookup list and invalidate the serial number
            bool bSuccess = all.Remove(m_serial_number);
            Debug.Assert(bSuccess);

            m_serial_number = -1;

            //Release the reference to the concrete class to allow that to be collected
            m_concrete = null;

            bDisposed = true;
          }
        }

        internal static CONCRETE_CLASS FromSerialNumber(int serial_number)
        {
            CppImplBase<CONCRETE_CLASS, INTERFACE> rc;
            all.TryGetValue(serial_number, out rc);
            return rc.m_concrete;
        }

        static PropertyInfo InterfaceProperty(string propname)
        {
            var prop = typeof(INTERFACE).GetProperty(propname);
            if (prop == null)
            {
                //We also need to look in super-interfaces (IWindow is the obvious candidate)
                var ifaces = typeof(INTERFACE).GetInterfaces();

                foreach (var i in ifaces)
                {
                    prop = i.GetProperty(propname);
                    if (null != prop)
                    {
                        break;
                    }
                }
            }

            Debug.Assert(prop != null);
            return prop;
        }

        internal static INTERFACE InterfaceFromSerialNumber(int sn)
        {
            var c = FromSerialNumber(sn);
            if (null != c)
            {
                INTERFACE iface = c.ToInterface();
                if (null != iface)
                {
                    return iface;
                }
            }
            Debug.Assert(false);
            return null;
        }

        internal static void SetClientProperty<VALUE_TYPE>(string propname, int sn, VALUE_TYPE value)
        {
            var iface = InterfaceFromSerialNumber(sn);
            if (null != iface)
            {
                var propToSet = InterfaceProperty(propname);
                Debug.Assert(null != propToSet);
                if (null != propToSet)
                {
                    var valueType = typeof(VALUE_TYPE);
                    var propType  = propToSet.PropertyType;

                    //Work around the int-as-bool problem while we're nicely hidden from view.
                    if (propType.Equals(typeof(bool)) && valueType.Equals(typeof(int)))
                    {
                        //We're setting a boolean property with an int
                        System.Object o = value;
                        int iValue = (int)o;
                        bool b = 0!=iValue;

                        propToSet.SetValue(iface, b);
                    }
                    else
                    {
                        propToSet.SetValue(iface, value);
                    }
                }
            }
        }

        internal static RETURN_TYPE GetClientProperty<RETURN_TYPE>(string propname, int sn, RETURN_TYPE def = default(RETURN_TYPE))
        {
            var iface = InterfaceFromSerialNumber(sn);

            if (null != iface)
            {
                var propToGet = InterfaceProperty(propname);
                Debug.Assert(null != propToGet);
                if (null != propToGet)
                {
                    var rtType  = typeof(RETURN_TYPE);
                    var getType = propToGet.PropertyType;

                    //Work around the int-as-bool problem while we're nicely hidden from view.
                    if (getType.Equals(typeof(bool)) && rtType.Equals(typeof(int)))
                    {
                        int iRet = (bool)propToGet.GetValue(iface) ? 1 : 0;
                        System.Object n = iRet;
                        return (RETURN_TYPE)n;
                    }
                    else
                    {
                        return (RETURN_TYPE)propToGet.GetValue(iface);
                    }
                }
            }

            return def;
        }

        public IntPtr CppPointer { get { return m_p_cpp; } }

        private IntPtr m_p_cpp = IntPtr.Zero;
        private int m_serial_number = 0;
        private static int g_current_serial_number = 1;
        private static readonly Dictionary<int, CppImplBase<CONCRETE_CLASS, INTERFACE> > all = new Dictionary<int, CppImplBase<CONCRETE_CLASS, INTERFACE> >();
        private CONCRETE_CLASS m_concrete;
        private bool bDisposed = false;

        internal static INTERFACE Find(IntPtr cpp)
        {
            foreach (var entry in all)
            {
                if (entry.Value.m_concrete.IsSameObject(cpp))
                {
                    return entry.Value.m_concrete.ToInterface();
                }
            }

            var newNativeWrapper = typeof(CONCRETE_CLASS).GetMethod("NewNativeWrapper");

            object[] args = { cpp };

            var native = newNativeWrapper.Invoke(null, args) as INTERFACE;

            return native;
        }
    }
}
