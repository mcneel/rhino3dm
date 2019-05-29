//
// ColorExtensions.cs
// RhinoMobile
//
// Created by dan (dan@mcneel.com) on 1/11/2014
// Copyright 2014 Robert McNeel & Associates.  All rights reserved.
//

#if __IOS__
using UIKit;
#endif

#if __ANDROID__
using Android.Graphics;
#endif

namespace RhinoMobile.Display
{
	public static class ColorExtensions
	{
		#if __ANDROID__
		public static Color ToNative(this System.Drawing.Color color)
		{
			return new Color(color.R, color.G, color.B, color.A);
		}

		public static System.Drawing.Color FromNative(this Color color)
		{
			return System.Drawing.Color.FromArgb(color.A, color.R, color.G, color.B);
		}
		#endif

    #if __IOS__
    public static UIColor ToNative(this System.Drawing.Color color)
    {
      return new UIColor((float)color.R / 255.0f, (float)color.G / 255.0f, color.B / 255.0f, color.A / 255.0f);
    }

    public static System.Drawing.Color FromNative(this UIColor color)
    {
      System.nfloat r,g,b,a;
      color.GetRGBA(out r, out g, out b, out a);
      return System.Drawing.Color.FromArgb((int)(a * 255.0f), (int)(r * 255.0f), (int)(g * 255.0f), (int)(b * 255.0f));
    }
    #endif
	}

}

