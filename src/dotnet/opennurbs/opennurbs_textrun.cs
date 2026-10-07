#pragma warning disable 1591
using Rhino.Runtime.InteropWrappers;
using System;
using System.Drawing;

namespace Rhino.Geometry
{
  /// <summary>
  /// A range of text with all the same attributes
  /// </summary>
  internal class TextRun : IDisposable
  {
    IntPtr m_pManagedTextRun = IntPtr.Zero;

    public TextRun()
    {
      m_pManagedTextRun = UnsafeNativeMethods.ON_TextRun_GetManagedTextRun();
    }

    internal TextRun(IntPtr managedTextRun)
    {
      m_pManagedTextRun = managedTextRun;
    }

    public TextRun(Rhino.DocObjects.Font font, double height, double stackScale, Color color, bool bold, bool italic, bool underlined, bool strikethrough, string text)
    {
      IntPtr constPtrFont = font!=null ? font.ConstPointer() : IntPtr.Zero;
      m_pManagedTextRun = UnsafeNativeMethods.ON_TextRunInit(constPtrFont, height, stackScale, color.ToArgb(), bold, italic, underlined, strikethrough, text);
      GC.KeepAlive(font);
    }

    internal IntPtr ConstPointer()
    {
      return NonConstPointer(); // all ON_TextRuns are non-const
    }
    IntPtr NonConstPointer()
    {
      return m_pManagedTextRun;
    }

    /// <summary>
    /// Passively reclaims unmanaged resources when the class user did not explicitly call Dispose().
    /// </summary>
    ~TextRun() { Dispose(false); }

    /// <summary>Actively reclaims unmanaged resources that this instance uses.</summary>
    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
      if (m_pManagedTextRun != IntPtr.Zero)
      {
        UnsafeNativeMethods.ON_TextRun_ReturnManagedTextRun(m_pManagedTextRun);
        m_pManagedTextRun = IntPtr.Zero;
      }
    }

    private bool GetBool(UnsafeNativeMethods.TextRunBool which)
    {
      IntPtr constPtrThis = ConstPointer();
      bool rc = UnsafeNativeMethods.ON_TextRun_GetBool(constPtrThis, which);
      GC.KeepAlive(this);
      return rc;
    }

    private void SetBool(UnsafeNativeMethods.TextRunBool which, bool val)
    {
      IntPtr ptrThis = NonConstPointer();
      UnsafeNativeMethods.ON_TextRun_SetBool(ptrThis, which, val);
      GC.KeepAlive(this);
    }

    public bool IsText
    {
      get { return GetBool(UnsafeNativeMethods.TextRunBool.IsText); }
    }

    public bool IsNewLine
    {
      get { return GetBool(UnsafeNativeMethods.TextRunBool.IsNewLine); }
    }

    public bool IsColumn
    {
      get { return GetBool(UnsafeNativeMethods.TextRunBool.IsColumn); }
    }

    public bool IsValid()
    {
      return GetBool(UnsafeNativeMethods.TextRunBool.IsValid);
    }

    public TextRunType RunType
    {
      get
      {
        IntPtr constPtrThis = ConstPointer();
        TextRunType rc = (TextRunType)UnsafeNativeMethods.ON_TextRun_Type(constPtrThis);
        GC.KeepAlive(this);
        return rc;
      }
      set
      {
        IntPtr ptrThis = NonConstPointer();
        UnsafeNativeMethods.ON_TextRun_SetType(ptrThis, (UnsafeNativeMethods.TextRunTypeConsts)value);
        GC.KeepAlive(this);
      }
    }

    public bool ApplyKerning
    {
      get { return GetBool(UnsafeNativeMethods.TextRunBool.ApplyKerning); }
      set { SetBool(UnsafeNativeMethods.TextRunBool.ApplyKerning, value); }
    }

    public string Text
    {
      get
      {
        using (var sw = new StringWrapper())
        {
          var ptr_stringholder = sw.NonConstPointer;
          IntPtr const_ptr_this = ConstPointer();
          UnsafeNativeMethods.ON_TextRun_GetTextString(const_ptr_this, ptr_stringholder);
          GC.KeepAlive(this);
          string text = sw.ToString();
          return text;
        }
        
      }
    }

    public bool TryGetFont(out Rhino.DocObjects.Font font)
    {
      if (!IsText)
      {
        font = null;
        return false;
      }
      else
      {
        IntPtr thisptr = ConstPointer();
        //IntPtr styleptr = IntPtr.Zero; // ConstParentDimStylePointer();
        font = new Rhino.DocObjects.Font(UnsafeNativeMethods.ON_V9_TextRun_Font(thisptr));
        GC.KeepAlive(this);
        return true;
      }
    }

    public bool IsStacked()
    {
      IntPtr ptr_this = ConstPointer();
      bool b = UnsafeNativeMethods.ON_TextRun_IsStacked(ptr_this);
      GC.KeepAlive(this);
      return b;
    }

    public bool IsSuperscript()
    {
      IntPtr ptr_this = ConstPointer();
      bool b = UnsafeNativeMethods.ON_TextRun_IsSuperscript(ptr_this);
      GC.KeepAlive(this);
      return b;
    }

    public bool IsSubscript()
    {
      IntPtr ptr_this = ConstPointer();
      bool b = UnsafeNativeMethods.ON_TextRun_IsSubscript(ptr_this);
      GC.KeepAlive(this);
      return b;
    }

    public void SetSuperscript()
    {
       IntPtr ptr_this = NonConstPointer();
       UnsafeNativeMethods.ON_TextRun_SetSuperscript(ptr_this);
       GC.KeepAlive(this);
    }

    public void SetSubscript()
    {
       IntPtr ptr_this = NonConstPointer();
       UnsafeNativeMethods.ON_TextRun_SetSubscript(ptr_this);
       GC.KeepAlive(this);
    }

    public void SetSuperOrSubscriptOff()
    {
       IntPtr ptr_this = NonConstPointer();
       UnsafeNativeMethods.ON_TextRun_SetSuperOrSubscriptOff(ptr_this);
       GC.KeepAlive(this);
    }

    public void SetStacked(char separator = '/')
    {
       IntPtr ptr_this = NonConstPointer();
       UnsafeNativeMethods.ON_TextRun_SetStacked(ptr_this, (int)separator);
       GC.KeepAlive(this);
    }

    public int ListItemNumber
    {
      get
      {
        //if (!IsListDepthRelevant)
        //  return -1;

        IntPtr ptr_this = ConstPointer();
        int rc = UnsafeNativeMethods.ON_TextRun_ListItemNumber(ptr_this);
        GC.KeepAlive(this);
        return rc;
      }

      set
      {
        //if (!IsListDepthRelevant)
        //  return;
        IntPtr ptr_this = NonConstPointer();
        UnsafeNativeMethods.ON_TextRun_SetListItemNumber(ptr_this, value);
        GC.KeepAlive(this);
      }
    }

    public bool IsListDepthRelevant => RunType == TextRunType.ListItemBegin;

    public int ListDepth
    {
      get
      {
        if (!IsListDepthRelevant)
          return -1;

        IntPtr ptr_this = ConstPointer();
        int rc = UnsafeNativeMethods.ON_TextRun_ListDepth(ptr_this);
        GC.KeepAlive(this);
        return rc;
      }

      set
      {
        if (!IsListDepthRelevant)
          return;
        IntPtr ptr_this = NonConstPointer();
        UnsafeNativeMethods.ON_TextRun_SetListDepth(ptr_this, value);
        GC.KeepAlive(this);
      }
    }

    public bool IsListOrdered
    {
      get
      {
        if (RunType != TextRunType.ListBegin && RunType != TextRunType.ListItemBegin)
          return false;

        IntPtr ptr_this = ConstPointer();
        bool rc = UnsafeNativeMethods.ON_TextRun_IsListOrdered(ptr_this);
        GC.KeepAlive(this);
        return rc;
      }

      set
      {
        if (RunType != TextRunType.ListBegin && RunType != TextRunType.ListItemBegin)
          return;
        IntPtr ptr_this = NonConstPointer();
        UnsafeNativeMethods.ON_TextRun_SetIsListOrdered(ptr_this, value);
        GC.KeepAlive(this);
      }
    }
  }
}
