
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Diagnostics;

namespace Rhino.UI.Controls
{
    /// <summary>
    /// Base class for CollapsibleSection and ViewModel factories used by the RDK UI
    /// </summary>
    public abstract class FactoryBase
    {
        internal static Dictionary<int, FactoryBase> m_factories = new Dictionary<int, FactoryBase>();
        private static int m_current_serial_number = 0;
        private int m_serial_number;
        private IntPtr m_this;

        internal FactoryBase()
        {
            m_current_serial_number++;
            m_serial_number = m_current_serial_number;
            m_this = CreateCpp(m_serial_number);//UnsafeNativeMethods.Rdk_SectionFactory_New(m_serial_number);
            m_factories.Add(m_serial_number, this);
        }

        internal abstract IntPtr CreateCpp(int serial_number);

        /// <summary>
        /// Finalizer
        /// </summary>
        ~FactoryBase()
        {
            m_factories.Remove(m_serial_number);
        }

        /// <summary>
        /// Override this method to return a new instance of your class for the given ID
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public virtual IntPtr Get(Guid id)
        {
            return IntPtr.Zero;
        }

        /// <summary>
        /// Call this function during the startup of your plug-in to ensure that all classes that support factory creation
        /// are registed
        /// </summary>
        /// <param name="plugin"></param>
        /// <returns></returns>
        public static Type[] Register(PlugIns.PlugIn plugin)
        {
            return Register(plugin.Assembly, plugin.Id);
        }

        /// <summary>
        /// Call this function during startup of current assembly to load classes with factory creation support
        /// are registed
        /// </summary>
        /// <returns></returns>
        public static Type[] Register()
        {
          var factory_types = new List<Type>();

          foreach (var factory in factory_types)
            {
                Activator.CreateInstance(factory);
            }

            return factory_types.ToArray();
        }


        internal static Type[] Register(Assembly assembly, Guid pluginId)
        {
            var plugin = PlugIns.PlugIn.GetLoadedPlugIn(pluginId);
            if (plugin == null) return null;

            var exported_types = assembly.GetExportedTypes();

            var factory_types = new List<Type>();
            for (var i = 0; i < exported_types.Length; i++)
            {
                var exported_type = exported_types[i];

                if (exported_type.IsAbstract || !exported_type.IsSubclassOf(typeof(FactoryBase)) || exported_type.GetConstructor(new Type[] { }) == null)
                    continue;

                factory_types.Add(exported_type);
            }

            if (factory_types.Count == 0) return null;

            foreach (var factory in factory_types)
            {
                Activator.CreateInstance(factory);
            }

            return factory_types.ToArray();
        }
    }
}