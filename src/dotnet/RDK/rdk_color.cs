#pragma warning disable 1591
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace Rhino.Display
{
  /// <summary>
  /// Color defined by 4 floating point values.
  /// </summary>
  [StructLayout(LayoutKind.Sequential, Pack = 4, Size = 16)]
  [DebuggerDisplay("{m_r}, {m_g}, {m_b}, {m_a}")]
  [Serializable]
  public struct Color4f : ISerializable
  {
    readonly float m_r;
    readonly float m_g;
    readonly float m_b;
    readonly float m_a;

    /// <since>5.0</since>
    public Color4f(System.Drawing.Color color)
    {
      m_r = color.R / 255.0f;
      m_g = color.G / 255.0f;
      m_b = color.B / 255.0f;
      m_a = color.A / 255.0f;
    }

    /// <since>5.0</since>
    public Color4f(Color4f color)
    {
      m_r = color.R;
      m_g = color.G;
      m_b = color.B;
      m_a = color.A;
    }

    /// <since>5.0</since>
    public Color4f(float red, float green, float blue, float alpha)
    {
      m_r = red;
      m_g = green;
      m_b = blue;
      m_a = alpha;
    }

    /// <since>7.0</since>
    public Color4f(int argb)
    {
      m_a = (float)((argb >> 0) & 255) / 255.0f;
      m_r = (float)((argb >> 8) & 255) / 255.0f;
      m_g = (float)((argb >> 16) & 255) / 255.0f;
      m_b = (float)((argb >> 24) & 255) / 255.0f;
    }

    private Color4f(SerializationInfo info, StreamingContext context)
    {
      m_r = info.GetSingle("R");
      m_g = info.GetSingle("G");
      m_b = info.GetSingle("B");
      m_a = info.GetSingle("A");
    }

    [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.SerializationFormatter)]
    void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
    {
      info.AddValue("R", m_r);
      info.AddValue("G", m_g);
      info.AddValue("B", m_b);
      info.AddValue("A", m_a);
    }


    /// <since>5.0</since>
    public static Color4f Empty
    {
      get { return new Color4f(0, 0, 0, 0); }
    }

    /// <since>5.0</since>
    public static Color4f Black
    {
      get { return new Color4f(0, 0, 0, 1); }
    }

    /// <since>5.0</since>
    public static Color4f White
    {
      get { return new Color4f(1, 1, 1, 1); }
    }

    /// <since>5.11</since>
    public static Color4f FromArgb(float a, float r, float g, float b)
    {
      return new Color4f(r, g, b, a);
    }

    /// <since>5.11</since>
    public static Color4f FromArgb(float a, Color4f color)
    {
      return new Color4f(color.R, color.G, color.B, a);
    }

    /// <since>5.0</since>
    public float R { get { return m_r; } }
    /// <since>5.0</since>
    public float G { get { return m_g; } }
    /// <since>5.0</since>
    public float B { get { return m_b; } }
    /// <since>5.0</since>
    public float A { get { return m_a; } }
    /// <since>6.3</since>
    public float L { get { return (m_r * 0.299f) + (m_g * 0.587f) + (m_b * 0.114f); ; } }

    /// <since>5.0</since>
    public static bool operator ==(Color4f a, Color4f b)
    {
      return (a.m_r == b.m_r && a.m_g == b.m_g && a.m_b == b.m_b && a.m_a == b.m_a);
    }

    /// <since>5.0</since>
    public static bool operator !=(Color4f a, Color4f b)
    {
      return (a.m_r != b.m_r || a.m_g != b.m_g || a.m_b != b.m_b || a.m_a != b.m_a);
    }

    public override bool Equals(object obj)
    {
      return (obj is Color4f && this == (Color4f)obj);
    }

    public override int GetHashCode()
    {
      // MSDN docs recommend XOR'ing the internal values to get a hash code
      return m_r.GetHashCode() ^ m_g.GetHashCode() ^ m_b.GetHashCode() ^ m_a.GetHashCode();
    }


    /// <since>5.0</since>
    public Color4f BlendTo(float t, Color4f col)
    {
      float r = m_r + (t * (col.m_r - m_r));
      float g = m_g + (t * (col.m_g - m_g));
      float b = m_b + (t * (col.m_b - m_b));
      float a = m_a + (t * (col.m_a - m_a));

      return new Color4f(r, g, b, a);
    }


    /// <since>6.0</since>
    public static Color4f ApplyGamma(Color4f col, float gamma)
    {
      if (Math.Abs(gamma - 1.0f) > float.Epsilon)
      {
        float r = (float) Math.Pow(col.m_r, gamma);
        float g = (float) Math.Pow(col.m_g, gamma);
        float b = (float) Math.Pow(col.m_b, gamma);

        return new Color4f(r, g, b, col.m_a);
      }

      return col;
    }

    /// <since>5.0</since>
    public System.Drawing.Color AsSystemColor()
    {
      return System.Drawing.Color.FromArgb((int)(m_a * 255.0f),
                                           (int)(m_r * 255.0f),
                                           (int)(m_g * 255.0f),
                                           (int)(m_b * 255.0f));
    }
  }
}
