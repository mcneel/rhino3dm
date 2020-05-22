using System;
using Rhino.Runtime.InteropWrappers;

namespace Rhino.FileIO
{
  /// <summary>
  /// Represents the notes information stored in a 3dm file.
  /// </summary>
  public class File3dmNotes
  {
    File3dm m_parent;
    string m_notes;
    bool m_visible;
    bool m_html;
    System.Drawing.Rectangle m_winrect;

    /// <summary>
    /// Creates empty default notes
    /// </summary>
    /// <since>5.0</since>
    public File3dmNotes() { }

    internal File3dmNotes(File3dm parent)
    {
      m_parent = parent;
    }

    internal void SetParent(File3dm parent)
    {
      if (m_parent != parent)
      {
        m_notes = Notes;
        m_visible = IsVisible;
        m_html = IsHtml;
        m_winrect = WindowRectangle;

        m_parent = parent;
        if (parent != null)
        {
          Notes = m_notes;
          IsVisible = m_visible;
          IsHtml = m_html;
          WindowRectangle = m_winrect;
        }
      }
    }

    /// <summary>
    /// Gets or sets the text content of the notes.
    /// </summary>
    /// <since>5.0</since>
    public string Notes
    {
      get
      {
        if (m_parent == null)
          return m_notes;

        IntPtr const_ptr_parent = m_parent.ConstPointer();
        using (var sh = new StringHolder())
        {
          IntPtr ptr_string = sh.NonConstPointer();
          bool visible = false;
          bool html = false;
          int top = 0, bottom = 0, left = 0, right = 0;
          UnsafeNativeMethods.ONX_Model_GetNotes(const_ptr_parent, ptr_string, ref visible, ref html, ref left, ref top, ref right, ref bottom);
          return sh.ToString();
        }
      }
      set
      {
        if (m_parent == null)
        {
          m_notes = value;
          return;
        }

        IntPtr ptr_parent = m_parent.NonConstPointer();
        UnsafeNativeMethods.ONX_Model_SetNotesString(ptr_parent, value);
      }
    }

    /// <summary>
    /// Gets or sets the notes visibility. If the notes are visible, true; false otherwise.
    /// </summary>
    /// <since>5.0</since>
    public bool IsVisible
    {
      get
      {
        if (m_parent == null)
          return m_visible;

        IntPtr const_ptr_parent = m_parent.ConstPointer();
        bool visible = false;
        bool html = false;
        int top = 0, bottom = 0, left = 0, right = 0;
        UnsafeNativeMethods.ONX_Model_GetNotes(const_ptr_parent, IntPtr.Zero, ref visible, ref html, ref left, ref top, ref right, ref bottom);
        return visible;
      }
      set
      {
        if (m_parent == null)
        {
          m_visible = value;
          return;
        }
        IntPtr ptr_parent = m_parent.NonConstPointer();
        System.Drawing.Rectangle r = WindowRectangle;
        UnsafeNativeMethods.ONX_Model_SetNotes(ptr_parent, value, IsHtml, r.Left, r.Top, r.Right, r.Bottom);
      }
    }

    /// <summary>
    /// Gets or sets the text format. If the format is HTML, true; false otherwise.
    /// </summary>
    /// <since>5.0</since>
    public bool IsHtml
    {
      get
      {
        if (m_parent == null)
          return m_html;

        IntPtr const_ptr_parent = m_parent.ConstPointer();
        bool visible = false;
        bool html = false;
        int top = 0, bottom = 0, left = 0, right = 0;
        UnsafeNativeMethods.ONX_Model_GetNotes(const_ptr_parent, IntPtr.Zero, ref visible, ref html, ref left, ref top, ref right, ref bottom);
        return html;
      }
      set
      {
        if (m_parent == null)
        {
          m_html = value;
          return;
        }
        IntPtr ptr_parent = m_parent.NonConstPointer();
        System.Drawing.Rectangle r = WindowRectangle;
        UnsafeNativeMethods.ONX_Model_SetNotes(ptr_parent, IsVisible, value, r.Left, r.Top, r.Right, r.Bottom);
      }
    }

    /// <summary>
    /// Gets or sets the position of the Notes when they were saved.
    /// </summary>
    /// <since>5.0</since>
    public System.Drawing.Rectangle WindowRectangle
    {
      get
      {
        if (m_parent == null)
          return m_winrect;

        IntPtr const_ptr_parent = m_parent.ConstPointer();
        bool visible = false;
        bool html = false;
        int top = 0, bottom = 0, left = 0, right = 0;
        UnsafeNativeMethods.ONX_Model_GetNotes(const_ptr_parent, IntPtr.Zero, ref visible, ref html, ref left, ref top, ref right, ref bottom);
        return System.Drawing.Rectangle.FromLTRB(left, top, right, bottom);
      }
      set
      {
        if (m_parent == null)
        {
          m_winrect = value;
          return;
        }

        IntPtr ptr_parent = m_parent.NonConstPointer();
        UnsafeNativeMethods.ONX_Model_SetNotes(ptr_parent, IsVisible, IsHtml, value.Left, value.Top, value.Right, value.Bottom);
      }
    }
  }

  /*
  class File3dmProperties : IDisposable
  {
    IntPtr m_p3dmProperties;

    // revision history
    ON_wString m_sCreatedBy;
    ON_wString m_sLastEditedBy;
    struct tm  m_create_time;
    struct tm  m_last_edit_time;
    int        m_revision_count;

    public File3dmNotes Notes { get; set; }

    ON_WindowsBitmap       m_PreviewImage;     // preview image of model

    // application
    ON_wString m_application_name;    // short name like "Rhino 2.0"
    ON_wString m_application_URL;     // URL
    ON_wString m_application_details; // whatever you want
  }
  */
}