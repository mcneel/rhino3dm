using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rhino.Runtime.InteropWrappers;

namespace Rhino.Render
{
	/// <summary>
	/// TimeZone
	/// </summary>
    public class TimeZone
    {
        private readonly IntPtr m_ptr;

		/// <summary>
		/// Construct a TimeZone
		/// </summary>
		/// <param name="pLw"></param>
		internal TimeZone(IntPtr pLw)
		{
			m_ptr = pLw;
		}

        /// <summary>
        /// Gets name of a time zone.
        /// </summary>
        public String Name
        {
            get
            {
                string name;
                using (var sh = new StringHolder())
                {
                    var string_pointer = sh.NonConstPointer();
                    UnsafeNativeMethods.Rdk_TimeZone_Name(m_ptr, string_pointer);
                    name = sh.ToString();
                }

                return name;
            }
        }

        /// <summary>
        /// Gets hours of a time zone.
        /// </summary>
        public double Hours
        {
            get
            {
                return UnsafeNativeMethods.Rdk_TimeZone_Hours(m_ptr);
            }
        }

        /// <summary>
        /// Returns the Longitude of a major city nearby
        /// </summary>
        /// <returns>Longitude</returns>
        public double Longitude
        {
          get
          {
            return UnsafeNativeMethods.Rdk_TimeZone_Longitude(m_ptr);
          }
        }

        /// <summary>
        /// Returns the latitude of a major city nearby
        /// </summary>
        /// <returns>Latitude</returns>
        public double Latitude
        {
          get
          {
          return UnsafeNativeMethods.Rdk_TimeZone_Latitude(m_ptr);
          }
        }

        /// <summary>
        /// Returns number of available time zones.
        /// </summary>
        /// <returns>Time zone count.</returns>
        public static int TimeZones()
        {
            return UnsafeNativeMethods.Rdk_TimeZone_Count();
        }

        /// <summary>
        /// Returns a time zone at given index.
        /// </summary>
        /// <param name="index">index.</param>
        /// <returns>Time zone at index.</returns>
        public static TimeZone TimeZoneAt(int index)
        {
            var tz = UnsafeNativeMethods.Rdk_TimeZone_GetAt(index);
            return new TimeZone(tz);
        }
    }
}
