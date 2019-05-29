using System;
using System.IO;
using System.Collections.Generic;

namespace Rhino.FileIO
{
  /// <summary>
  /// Used for collecting text data
  /// </summary>
  public class TextLog : IDisposable
  {
    IntPtr m_pTextLog = IntPtr.Zero;
    IntPtr m_pString = IntPtr.Zero;
    bool m_bDelete = true;

    /// <summary>
    /// Creates a text log that stores all text in memory.  Use ToString on this
    /// version of the TextLog to get the text that we written
    /// </summary>
    public TextLog()
    {
      m_pString = UnsafeNativeMethods.ON_wString_New(null);
      m_pTextLog = UnsafeNativeMethods.ON_TextLog_New(m_pString);
    }

    /// <summary>
    /// Creates a text log that writes all text to a file. If no filename is
    /// provided, then text is written to StdOut
    /// </summary>
    /// <param name="filename">
    /// Name of file to create and write to. If null, then text output
    /// is sent to StdOut
    /// </param>
    public TextLog(string filename)
    {
      m_pTextLog = UnsafeNativeMethods.ON_TextLog_New2(filename);
    }

    /// Create a TextLog from a pointer owned elsewhere. Need
    /// to ensure that pointer isn't deleted on Disposal of this array
    public TextLog(IntPtr ptr)
    {
      m_pTextLog = ptr;
      m_bDelete = false;
    }

    /// <summary>
    /// If the TextLog was constructed using the empty constructor, then the text
    /// information is stored in a runtime string.  The contents of this string
    /// is retrieved using ToString for this case
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
      return Runtime.InteropWrappers.StringWrapper.GetStringFromPointer(m_pString);
    }

    /// <summary>
    /// Increase the indentation level
    /// </summary>
    /// <example>
    /// <code source='examples\vbnet\ex_printinstancedefinitiontree.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_printinstancedefinitiontree.cs' lang='cs'/>
    /// <code source='examples\py\ex_printinstancedefinitiontree.py' lang='py'/>
    /// </example>
    public void PushIndent()
    {
      IntPtr pThis = NonConstPointer();
      UnsafeNativeMethods.ON_TextLog_PushPopIndent(pThis, true);
    }

    /// <summary>
    /// Decrease the indentation level
    /// </summary>
    /// <example>
    /// <code source='examples\vbnet\ex_printinstancedefinitiontree.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_printinstancedefinitiontree.cs' lang='cs'/>
    /// <code source='examples\py\ex_printinstancedefinitiontree.py' lang='py'/>
    /// </example>
    public void PopIndent()
    {
      IntPtr pThis = NonConstPointer();
      UnsafeNativeMethods.ON_TextLog_PushPopIndent(pThis, false);
    }

    /// <summary>
    /// 0: one tab per indent. &gt;0: number of spaces per indent
    /// </summary>
    public int IndentSize
    {
      get
      {
        IntPtr pConstThis = ConstPointer();
        return UnsafeNativeMethods.ON_TextLog_IndentSize_Get(pConstThis);
      }
      set
      {
        IntPtr pThis = NonConstPointer();
        UnsafeNativeMethods.ON_TextLog_IndentSize_Set(pThis, value);
      }
    }

    /// <summary>
    /// Send text wrapped at a set line length
    /// </summary>
    /// <param name="text"></param>
    /// <param name="lineLength"></param>
    public void PrintWrappedText(string text, int lineLength)
    {
      IntPtr pThis = NonConstPointer();
      UnsafeNativeMethods.ON_TextLog_PrintWrappedText(pThis, text, lineLength);
    }

    /// <summary>
    /// Send text to the textlog
    /// </summary>
    /// <param name="text"></param>
    /// <example>
    /// <code source='examples\vbnet\ex_printinstancedefinitiontree.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_printinstancedefinitiontree.cs' lang='cs'/>
    /// <code source='examples\py\ex_printinstancedefinitiontree.py' lang='py'/>
    /// </example>
    public void Print(string text)
    {
      IntPtr pThis = NonConstPointer();
      UnsafeNativeMethods.ON_TextLog_Print(pThis, text);
    }

    /// <summary>
    /// Send formatted text to the textlog
    /// </summary>
    /// <param name="format"></param>
    /// <param name="arg0"></param>
    public void Print(string format, object arg0)
    {
      Print(string.Format(format, arg0));
    }
    /// <summary>
    /// Send formatted text to the textlog
    /// </summary>
    /// <param name="format"></param>
    /// <param name="arg0"></param>
    /// <param name="arg1"></param>
    public void Print(string format, object arg0, object arg1)
    {
      Print(string.Format(format, arg0, arg1));
    }

    IntPtr ConstPointer()
    {
      return NonConstPointer(); // all ONX_Models are non-const
    }
    internal IntPtr NonConstPointer()
    {
      if (m_pTextLog == IntPtr.Zero)
        throw new ObjectDisposedException("TextLog");
      return m_pTextLog;
    }

    /// <summary>
    /// Passively reclaims unmanaged resources when the class user did not explicitly call Dispose().
    /// </summary>
    ~TextLog() { Dispose(false); }

    /// <summary>
    /// Actively reclaims unmanaged resources that this instance uses.
    /// </summary>
    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    /// <summary>
    /// For derived class implementers.
    /// <para>This method is called with argument true when class user calls Dispose(), while with argument false when
    /// the Garbage Collector invokes the finalizer, or Finalize() method.</para>
    /// <para>You must reclaim all used unmanaged resources in both cases, and can use this chance to call Dispose on disposable fields if the argument is true.</para>
    /// <para>Also, you must call the base virtual method within your overriding method.</para>
    /// </summary>
    /// <param name="disposing">true if the call comes from the Dispose() method; false if it comes from the Garbage Collector finalizer.</param>
    protected virtual void Dispose(bool disposing)
    {
      if (m_bDelete)
      {
        if (IntPtr.Zero != m_pTextLog)
          UnsafeNativeMethods.ON_TextLog_Delete(m_pTextLog);
        if (IntPtr.Zero != m_pString)
          UnsafeNativeMethods.ON_wString_Delete(m_pString);
      }

      m_pTextLog = IntPtr.Zero;
      m_pString = IntPtr.Zero;
    }
  }
}