using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rhino.Runtime.InteropWrappers;

namespace Rhino.Render
{
	/// <summary>
	/// City
	/// </summary>
    public class City
    {
        private readonly IntPtr m_ptr;

		/// <summary>
		/// Construct a City
		/// </summary>
		/// <param name="pLw"></param>
		internal City(IntPtr pLw)
		{
			m_ptr = pLw;
		}

        /// <summary>
        /// Gets name of city.
        /// </summary>
        public String Name
        {
            get
            {
                string name;
                using (var sh = new StringHolder())
                {
                    var string_pointer = sh.NonConstPointer();
                    UnsafeNativeMethods.Rdk_City_Name(m_ptr, string_pointer);
                    name = sh.ToString();
                }

                return name;
            }
        }

        /// <summary>
        /// Gets latitude of city.
        /// </summary>
        public double Latitude
        {
            get
            {
                return UnsafeNativeMethods.Rdk_City_Latitude(m_ptr);
            }
        }

        /// <summary>
        /// Gets longitude of city.
        /// </summary>
        public double Longitude
        {
            get
            {
                return UnsafeNativeMethods.Rdk_City_Longitude(m_ptr);
            }
        }

        /// <summary>
        /// Gets time zone of city.
        /// </summary>
        public double TimeZone
        {
            get
            {
                return UnsafeNativeMethods.Rdk_City_TimeZone(m_ptr);
            }
        }

        /// <summary>
        /// Finds nearest city of specified input parameters.
        /// </summary>
        /// <param name="latitude">latitude.</param>
        /// <param name="longitude">longitude.</param>
        /// <returns>Nearest city.</returns>
        public static City FindNearest(double latitude, double longitude)
        {
            var p_city = UnsafeNativeMethods.Rdk_City_FindNearest(latitude, longitude);
            return new City(p_city);
        }

        /// <summary>
        /// Returns number of available cities.
        /// </summary>
        /// <returns>City count.</returns>
        public static int Cities()
        {
            return UnsafeNativeMethods.Rdk_City_Count();
        }

        /// <summary>
        /// Returns city at given index.
        /// </summary>
        /// <param name="index">index.</param>
        /// <returns>City at index.</returns>
        public static City CityAt(int index)
        {
            var p_city = UnsafeNativeMethods.Rdk_City_GetAt(index);
            return new City(p_city);
        }
    }
}
