#pragma warning disable 1591
using System;

#if RHINO_SDK
namespace Rhino.Input.Custom
{

  /// <summary>Used to get strings.</summary>
  public class GetString : GetBaseClass
  {
    /// <summary>
    /// Constructs a new GetString.
    /// </summary>
    /// <example>
    /// <code source='examples\vbnet\ex_addlayer.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_addlayer.cs' lang='cs'/>
    /// <code source='examples\py\ex_addlayer.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    public GetString()
    {
      IntPtr ptr = UnsafeNativeMethods.CRhinoGetString_New();
      Construct(ptr);
    }

    /// <summary>Returns the string that the user typed. By default, space stops the string input.</summary>
    /// <returns>The result type. If the user typed a string, this is <see cref="GetResult.String"/>.</returns>
    /// <example>
    /// <code source='examples\vbnet\ex_addlayer.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_addlayer.cs' lang='cs'/>
    /// <code source='examples\py\ex_addlayer.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    [CLSCompliant(false)]
    public GetResult Get()
    {
      IntPtr ptr = NonConstPointer();
      int rc = UnsafeNativeMethods.CRhinoGetString_Get(ptr, false);
      return (GetResult)rc;
    }

    /// <summary>Returns the string that the user typed. By default, space does not stop input.</summary>
    /// <returns>The result type. If the user typed a string, this is <see cref="GetResult.String"/>.</returns>
    /// <since>5.0</since>
    [CLSCompliant(false)]
    public GetResult GetLiteralString()
    {
      IntPtr ptr = NonConstPointer();
      int rc = UnsafeNativeMethods.CRhinoGetString_Get(ptr, true);
      return (GetResult)rc;
    }
  }

  /// <summary>
  /// If you want to explicitly get string input, then use GetString class with
  /// options. If you only want to get options, then use this class (GetOption)
  /// </summary>
  public class GetOption : GetBaseClass
  {
    /// <since>5.0</since>
    public GetOption()
    {
      IntPtr ptr = UnsafeNativeMethods.CRhinoGetOption_New();
      Construct(ptr);
    }

    /// <summary>
    /// Call to get an option. A return value of "option" means the user selected
    /// a valid option. Use Option() the determine which option.
    /// </summary>
    /// <returns>If the user chose an option, then <see cref="GetResult.Option"/>; another enumeration value otherwise.</returns>
    /// <since>5.0</since>
    [CLSCompliant(false)]
    public GetResult Get()
    {
      IntPtr ptr = NonConstPointer();
      int rc = UnsafeNativeMethods.CRhinoGetOption_Get(ptr);
      return (GetResult)rc;
    }
  }

  /// <summary>Used to get double precision numbers.</summary>
  public class GetNumber : GetBaseClass
  {
    /// <summary>Create a new GetNumber.</summary>
    /// <example>
    /// <code source='examples\vbnet\ex_addbackgroundbitmap.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_addbackgroundbitmap.cs' lang='cs'/>
    /// <code source='examples\py\ex_addbackgroundbitmap.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    public GetNumber()
    {
      IntPtr ptr = UnsafeNativeMethods.CRhinoGetNumber_New();
      Construct(ptr);
    }

    /// <summary>Call to get a number.</summary>
    /// <returns>If the user chose a number, then <see cref="GetResult.Number"/>; another enumeration value otherwise.</returns>
    /// <example>
    /// <code source='examples\vbnet\ex_addbackgroundbitmap.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_addbackgroundbitmap.cs' lang='cs'/>
    /// <code source='examples\py\ex_addbackgroundbitmap.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    [CLSCompliant(false)]
    public GetResult Get()
    {
      IntPtr ptr = NonConstPointer();
      int rc = UnsafeNativeMethods.CRhinoGetNumber_Get(ptr);
      return (GetResult)rc;
    }

    /// <summary>
    /// Sets a lower limit on the number that can be returned.
    /// By default there is no lower limit.
    /// </summary>
    /// <param name="lowerLimit">smallest acceptable number.</param>
    /// <param name="strictlyGreaterThan">
    /// If true, then the returned number will be > lower_limit.
    /// </param>
    /// <example>
    /// <code source='examples\vbnet\ex_addbackgroundbitmap.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_addbackgroundbitmap.cs' lang='cs'/>
    /// <code source='examples\py\ex_addbackgroundbitmap.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    public void SetLowerLimit(double lowerLimit, bool strictlyGreaterThan)
    {
      IntPtr ptr = NonConstPointer();
      UnsafeNativeMethods.CRhinoGetNumber_SetLimit(ptr, lowerLimit, strictlyGreaterThan, true);
    }

    /// <summary>
    /// Sets an upper limit on the number that can be returned.
    /// By default there is no upper limit.
    /// </summary>
    /// <param name="upperLimit">largest acceptable number.</param>
    /// <param name="strictlyLessThan">If true, then the returned number will be &lt; upper_limit.</param>
    /// <since>5.0</since>
    public void SetUpperLimit( double upperLimit, bool strictlyLessThan )
    {
      IntPtr ptr = NonConstPointer();
      UnsafeNativeMethods.CRhinoGetNumber_SetLimit(ptr, upperLimit, strictlyLessThan, false);
    }

  }

  /// <summary>Used to get integer numbers.</summary>
  public class GetInteger : GetBaseClass
  {
    /// <since>5.0</since>
    public GetInteger()
    {
      IntPtr ptr = UnsafeNativeMethods.CRhinoGetInteger_New();
      Construct(ptr);
    }

    /// <summary>
    /// Call to get an integer.
    /// </summary>
    /// <returns>If the user chose a number, then <see cref="GetResult.Number"/>; another enumeration value otherwise.</returns>
    /// <since>5.0</since>
    [CLSCompliant(false)]
    public GetResult Get()
    {
      IntPtr ptr = NonConstPointer();
      int rc = UnsafeNativeMethods.CRhinoGetInteger_Get(ptr);
      return (GetResult)rc;
    }

    /// <since>5.0</since>
    public new int Number()
    {
      IntPtr ptr = NonConstPointer();
      return UnsafeNativeMethods.CRhinoGetInteger_Number(ptr);
    }

    /// <summary>
    /// Sets a lower limit on the number that can be returned.
    /// By default there is no lower limit.
    /// </summary>
    /// <param name="lowerLimit">smallest acceptable number.</param>
    /// <param name="strictlyGreaterThan">
    /// If true, then the returned number will be > lower_limit.
    /// </param>
    /// <since>5.0</since>
    public void SetLowerLimit( int lowerLimit, bool strictlyGreaterThan )
    {
      IntPtr ptr = NonConstPointer();
      UnsafeNativeMethods.CRhinoGetInteger_SetLimit(ptr, lowerLimit, strictlyGreaterThan, true);
    }

    /// <summary>
    /// Sets an upper limit on the number that can be returned.
    /// By default there is no upper limit.
    /// </summary>
    /// <param name="upperLimit">largest acceptable number.</param>
    /// <param name="strictlyLessThan">If true, then the returned number will be &lt; upper_limit.</param>
    /// <since>5.0</since>
    public void SetUpperLimit( int upperLimit, bool strictlyLessThan )
    {
      IntPtr ptr = NonConstPointer();
      UnsafeNativeMethods.CRhinoGetInteger_SetLimit(ptr, upperLimit, strictlyLessThan, false);
    }
  }

  // skipping CRhinoGetColor - somewhat unusual
}
#endif