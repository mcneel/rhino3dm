#pragma warning disable 1591

using Rhino.Render;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.CompilerServices;

namespace Rhino.UI.Controls.ThumbnailUI
{
  enum ThumbnailSize { Tiny, Small, Medium, Large };

  [Obsolete("ThumbData is obsolete", false)]
  public class ThumbData
  {
    public event PropertyChangedEventHandler PropertyChanged;

    private System.Drawing.Bitmap m_image;
    private string m_name;
    private string m_type;
    private string m_intensity;
    private Guid m_id;
    private List<string> m_tags;
    private bool m_in_use;
    private List<ThumbData> m_child;
    private ThumbData m_parent;
    private bool m_selected;
    private Rhino.Render.PreviewAppearance m_pa;
    private RenderContent m_content;
    private List<Display.Color4f> m_inusecolors;

    public bool TopLevel
    {
      get
      {
        return m_content.TopLevel;
      }
    }

    public List<Display.Color4f> InUseColor
    {
      get
      {
        return m_inusecolors;
      }

      set
      {
        m_inusecolors = value;
      }
    }

    public RenderContent Content
    {
      get
      {
        return m_content;
      }

      set
      {
        m_content = value;
      }
    }

    public Rhino.Render.PreviewAppearance PreviewAppearance
    {
      get
      {
        return m_pa;
      }

      set
      {
        m_pa = value;
      }
    }

    public System.Drawing.Bitmap Image
    {
      get
      {
        return m_image;
      }

      set
      {
        if (value != m_image)
        {
          m_image = value;
        }
      }
    }

    public List<ThumbData> Children
    {
      get
      {
        return m_child;
      }

      set
      {
        if (value != m_child)
        {
          m_child = value;
        }
      }
    }

    public ThumbData Parent
    {
      get
      {
        return m_parent;
      }

      set
      {
        if (value != m_parent)
        {
          m_parent = value;
        }
      }
    }

    public string Name
    {
      get
      {
        return m_name;
      }

      set
      {
        if (value.CompareTo(m_name) != 0)
        {
          m_name = value;
          OnPropertyChanged();
        }
      }
    }

    public bool Selected
    {
      get
      {
        return m_selected;
      }

      set
      {
        if (value != m_selected)
        {
          m_selected = value;
          OnPropertyChanged();
        }
      }
    }

    public string Type
    {
      get
      {
        return m_type;
      }

      set
      {
        if (value.CompareTo(m_type) != 0)
        {
          m_type = value;
        }
      }
    }

    public string Intensity
    {
      get
      {
        return m_intensity;
      }

      set
      {
        if (value.CompareTo(m_intensity) != 0)
        {
          m_intensity = value;
          OnPropertyChanged();
        }
      }
    }

    public Guid Id
    {
      get
      {
        return m_id;
      }

      set
      {
        if (value != m_id)
        {
          m_id = value;
        }
      }
    }

    public bool InUse
    {
      get
      {
        return m_in_use;
      }

      set
      {
        if (value != m_in_use)
        {
          m_in_use = value;
          OnPropertyChanged();
        }
      }
    }

    public List<string> Tags
    {
      get
      {
        return m_tags;
      }

      set
      {
        if (value != m_tags)
        {
          m_tags = value;
          OnPropertyChanged();
        }
      }
    }

    // ToDo: Currently all the preview sizes are retrieved through GetPreviewHeight. 
    // In the future these values should come from the rdk_ui.
    //
    // Square
    // case PS::Sizes::Tiny:   width = height = 20; break;
    // case PS::Sizes::Small:  width = height = 37; break;
    // case PS::Sizes::Medium: width = height = 55; break;
    // case PS::Sizes::Large:  width = height = 97; break;
    //
    // Wide
    // case PS::Sizes::Tiny:   height =  16; break;
		// case PS::Sizes::Small:  height =  30; break;
		// case PS::Sizes::Medium: height =  80; break;
 		// case PS::Sizes::Large:  height = 140; break;
    public static int GetPreviewHeigth(Rhino.Render.DataSources.Sizes thumb_size, Rhino.Render.DataSources.Shapes shape)
    {
      int size = 0;

      // Square
      if (shape == Rhino.Render.DataSources.Shapes.Square)
      {
        if (thumb_size == Rhino.Render.DataSources.Sizes.Tiny)
        {
          size = 20;
        }

        if (thumb_size == Rhino.Render.DataSources.Sizes.Small)
        {
          size = 37;
        }

        if (thumb_size == Rhino.Render.DataSources.Sizes.Medium)
        {
          size = 55;
        }

        if (thumb_size == Rhino.Render.DataSources.Sizes.Large)
        {
          size = 97;
        }
      }
      else
      {
        // Wide
        if (thumb_size == Rhino.Render.DataSources.Sizes.Tiny)
        {
          size = 16;
        }

        if (thumb_size == Rhino.Render.DataSources.Sizes.Small)
        {
          size = 30;
        }

        if (thumb_size == Rhino.Render.DataSources.Sizes.Medium)
        {
          size = 80;
        }

        if (thumb_size == Rhino.Render.DataSources.Sizes.Large)
        {
          size = 140;
        }
      }

      return size;
    }

    public static int GetPreviewWidth(Rhino.Render.DataSources.Sizes thumb_size, Rhino.Render.DataSources.Shapes shape)
    {
      int size = 0;

      // Square
      if (shape == Rhino.Render.DataSources.Shapes.Square)
      {
        if (thumb_size == Rhino.Render.DataSources.Sizes.Tiny)
        {
          size = 20;
        }

        if (thumb_size == Rhino.Render.DataSources.Sizes.Small)
        {
          size = 37;
        }

        if (thumb_size == Rhino.Render.DataSources.Sizes.Medium)
        {
          size = 55;
        }

        if (thumb_size == Rhino.Render.DataSources.Sizes.Large)
        {
          size = 97;
        }
      }
      else
      {
        // Wide
        if (thumb_size == Rhino.Render.DataSources.Sizes.Tiny)
        {
          size = 16;
        }

        if (thumb_size == Rhino.Render.DataSources.Sizes.Small)
        {
          size = 30;
        }

        if (thumb_size == Rhino.Render.DataSources.Sizes.Medium)
        {
          size = 80;
        }

        if (thumb_size == Rhino.Render.DataSources.Sizes.Large)
        {
          size = 140;
        }
        // width = height * 3;
        size = size * 3;
      }

      return size;
    }

    void OnPropertyChanged([CallerMemberName] string memberName = null)
    {
      if (PropertyChanged != null)
      {
        PropertyChanged(this, new PropertyChangedEventArgs(memberName));
      }
    }
  }
}
