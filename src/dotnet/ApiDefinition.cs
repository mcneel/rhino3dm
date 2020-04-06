#if MOBILE_BUILD
using System;

using UIKit;
using Foundation;
using ObjCRuntime;
using CoreGraphics;

namespace Rhino3dm.iOS
{
  // Rhino3dm.iOS is a binding (btouch) project.
  // Even though we are not binding Objective-C - but rather
  // wrapping a native C/C++ library (opennurbs) and packaging 
  // the Rhino3dm classes along with it - an ApiDefinition.cs is 
  // required for all btouch projects.
}
#endif
