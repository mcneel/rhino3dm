#pragma warning disable 1591
using System;
using Rhino.Geometry;
using Rhino.Runtime.InteropWrappers;

#if RHINO_SDK
namespace Rhino.DocObjects
{
  /// <summary>
  /// A detail view object is a nested Rhino viewport placed on a page view with a 2D closed curve
  /// boundary. It can be any type of modeling view.
  /// </summary>
  public class DetailViewObject : RhinoObject 
  {
    internal DetailViewObject(uint serialNumber)
      : base(serialNumber) { }

    /// <summary>
    /// Returns the detail view geometry.
    /// </summary>
    /// <since>5.0</since>
    public Rhino.Geometry.DetailView DetailGeometry
    {
      get
      {
        Rhino.Geometry.DetailView rc = Geometry as Rhino.Geometry.DetailView;
        return rc;
      }
    }

    internal override CommitGeometryChangesFunc GetCommitFunc()
    {
      return UnsafeNativeMethods.CRhinoDetailObject_InternalCommitChanges;
    }

    /// <summary>
    /// Gets or sets the active state of the detail view.
    /// </summary>
    /// <since>5.0</since>
    public bool IsActive
    {
      get
      {
        IntPtr pConstThis = ConstPointer();
        return UnsafeNativeMethods.CRhinoDetailViewObject_IsActive(pConstThis);
      }
      set
      {
        IntPtr pConstThis = ConstPointer();
        UnsafeNativeMethods.CRhinoDetailViewObject_SetActive(pConstThis, value);
      }
    }

    /// <summary>
    /// Gets the detail view's viewport.
    /// </summary>
    /// <since>5.0</since>
    public Rhino.Display.RhinoViewport Viewport
    {
      get { return m_viewport ?? (m_viewport = new Rhino.Display.RhinoViewport(this)); }
    }
    Rhino.Display.RhinoViewport m_viewport;

    /// <since>5.0</since>
    public bool CommitViewportChanges()
    {
      bool rc = false;
      if (m_viewport != null)
      {
        IntPtr pThis = ConstPointer();
        IntPtr pViewport = m_viewport.ConstPointer();
        uint serial_number = UnsafeNativeMethods.CRhinoDetailViewObject_CommitViewportChanges(pThis, pViewport);
        if (serial_number > 0)
        {
          rc = true;
          m_rhinoobject_serial_number = serial_number;
          m_viewport.OnDetailCommit();
        }
      }
      return rc;
    }

    /// <summary>
    ///  Detail objects have two strings that can be used to describe the detail:
    ///  1. The name string that is part of the object's attributes
    ///  2. The viewport projection title that is part of the viewport
    ///  This function combines these two strings to create a single "description" string in the form of attribute_name - projection_title.
    /// </summary>
    /// <since>7.0</since>
    public string DescriptiveTitle
    {
      get
      {
        using (var sh = new Runtime.InteropWrappers.StringHolder())
        {
          IntPtr ptr_string = sh.NonConstPointer();
          IntPtr ptr_const_this = ConstPointer();
          if (UnsafeNativeMethods.CRhinoDetailViewObject_DescriptiveTitle(ptr_const_this, ptr_string))
            return sh.ToString();
          return null;
        }
      }
    }

    /// <summary>
    /// Gets the world coordinate to page coordinate transformation.
    /// </summary>
    /// <since>6.0</since>
    public Geometry.Transform WorldToPageTransform
    {
      get
      {
        Transform xform = Transform.Identity;
        IntPtr ptr_const_this = ConstPointer();
        bool rc = UnsafeNativeMethods.CRhinoDetailViewObject_GetPageXform(ptr_const_this, true, ref xform);
        return rc ? xform : Transform.Unset;
      }
    }

    /// <summary>
    /// Returns the page coordinate to world coordinate transformation.
    /// </summary>
    /// <since>6.0</since>
    public Geometry.Transform PageToWorldTransform
    {
      get
      {
        Transform xform = Transform.Identity;
        IntPtr ptr_const_this = ConstPointer();
        bool rc = UnsafeNativeMethods.CRhinoDetailViewObject_GetPageXform(ptr_const_this, false, ref xform);
        return rc ? xform : Transform.Unset;
      }
    }

    /// <summary>
    ///  Detail view object scale formats.
    /// </summary>
    public enum ScaleFormat : int
    {
      /// <summary>
      /// No formatting
      /// </summary>
      None,
      /// <summary>
      /// #:1
      /// </summary>
      PageLengthToOne,
      /// <summary>
      /// 1:#
      /// </summary>
      OneToModelLength,
      /// <summary>
      /// 1" = #'
      /// </summary>
      OneInchToModelLengthFeet,
      /// <summary>
      /// #" = 1'
      /// </summary>
      ModelLengthInchToOneFoot,
      /// <summary>
      /// #' = 1'-0"
      /// </summary>
      ModelLengthInchToOneFootInch
    };

    /// <summary>
    /// Returns the detail view object's scale as a formatted string. 
    /// The detail view object's viewport must be to parallel projection.
    /// </summary>
    /// <param name="format">The scale format.</param>
    /// <param name="value">The formatted string</param>
    /// <returns>true if successful, false otherwise</returns>
    /// <since>7.0</since>
    public bool GetFormattedScale(ScaleFormat format, out string value)
    {
      value = null;
      using (var sh = new StringHolder())
      {
        IntPtr ptr_const_this = ConstPointer();
        IntPtr ptr_string = sh.NonConstPointer();
        if (UnsafeNativeMethods.CRhinoDetailViewObject_GetFormattedScale(ptr_const_this, (uint)format, ptr_string))
        {
          value = sh.ToString();
          return true;
        }
      }
      return false;
    }
  }
}
#endif
