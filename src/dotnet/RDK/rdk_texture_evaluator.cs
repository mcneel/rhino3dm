using System;
#if RHINO_SDK

using System.Collections.Generic;
using Rhino.Geometry;
using System.Diagnostics;


namespace Rhino.Render
{
  /*Nathan - the values for evaluatorFlags are from CRhRdkTexture::
  enum : unsigned int
{
  efNormal					= 0x0000,
  efDisableFiltering			= 0x0001, // Force the texture to be evaluated without filtering (the implementation gets to decide what that means).
  efDisableLocalMapping		= 0x0002, // Force the texture to be evaluated without local mapping - ie, Repeat(1.0, 1.0, 1.0), Offset(0.0, 0.0 0.0), Rotation(0.0, 0.0, 0.0).
  efDisableAdjustment			= 0x0004, // Force the texture to be evaluated without post-adjustment (greyscale, invert, clamp etc)
  efDisableProjectionChange	= 0x0008, // Force the texture to be evaluated without any projection modification (ProjectionIn == ProjectionOut)
};*/

  /// <summary>
  /// This is the interface to a lightweight object capable of evaluating texture color throughout uvw space.  Derive from this class to create your own texture evaluator to return from a custom RenderTexture.
  /// </summary>
  public class TextureEvaluator : IDisposable
  {
    /// <summary>
    /// Base class constructor
    /// </summary>
    [Obsolete]
    protected TextureEvaluator() : this(RenderTexture.TextureEvaluatorFlags.Normal)
    {
    }

    /// <summary>
    /// Base class constructor
    /// </summary>
    /// <param name="evaluatorFlags"></param>
    protected TextureEvaluator(RenderTexture.TextureEvaluatorFlags evaluatorFlags)
    {
      // This constructor is being called because we have a custom .NET subclass
      m_runtime_serial_number = g_serial_number_counter++;
      CppPointer = UnsafeNativeMethods.CRhCmnRdkTextureEvaluator_New(m_runtime_serial_number, (uint)evaluatorFlags);
      m_bAutoDelete = true;
      g_all_custom_evaluators.Add(this);
    }

    private TextureEvaluator(IntPtr pTextureEvaluator, bool bIsConstPointer)
    {
      CppPointer = pTextureEvaluator;
      m_bAutoDelete = !bIsConstPointer;
    }

    /// <summary>
    /// Call this function before calling GetColor for the first time. Ideally, this should
		/// be on the main thread, but you can also call it on a worker thread as long as you
    /// are sure that Initialize() or GetColor() cannot be called at the same time on another thread.
    /// </summary>
    /// <returns></returns>
    public virtual bool Initialize()
    {
      Debug.Assert(CppPointer != IntPtr.Zero);

      if (IsNativeWrapper())
      {
        return UnsafeNativeMethods.Rdk_TextureEvaluator_Initialize(CppPointer);
      }
      else
      {
        return UnsafeNativeMethods.Rdk_TextureEvaluator_InitializeBase(CppPointer);
      }
    }

    /// <summary>
    /// Get the color of the texture at a particular point in uvw space.
		/// May be called from within a rendering shade pipeline.
		/// note For ray differentials see Pharr Humphreys, "Physically Based Rendering", chapter 11.
    /// </summary>
    /// <param name="uvw">is the point for which to evaluate the texture.</param>
    /// <param name="duvwdx">duvwdx is a ray differential.</param>
    /// <param name="duvwdy">duvwdy is a ray differential.</param>
    /// <returns>The texture color at this point in UV space.</returns>
    public virtual Display.Color4f GetColor(Point3d uvw, Vector3d duvwdx, Vector3d duvwdy)
    {
      Debug.Assert(CppPointer != null);

      var rc = new Display.Color4f();

      if (IsNativeWrapper())
      {
        if (!UnsafeNativeMethods.Rdk_TextureEvaluator_GetColor(CppPointer, uvw, duvwdx, duvwdy, ref rc))
        {
          return Display.Color4f.Empty;
        }
      }
      else
      {
        Debug.Assert(false, "Not implemented");
      }

      return rc;
    }

#region callbacks
    internal delegate int GetColorCallback(int serial_number, Point3d uvw, Vector3d duvwdx, Vector3d duvwdy, ref Display.Color4f color);
    internal static GetColorCallback m_GetColor = OnGetColor;
    static int OnGetColor(int serial_number, Point3d uvw, Vector3d duvwdx, Vector3d duvwdy, ref Display.Color4f color)
    {
      var rc = 0;
      var eval = FromSerialNumber(serial_number);
      if (eval != null)
      {
        var c = eval.GetColor(uvw, duvwdx, duvwdy);
        if (c != Display.Color4f.Empty)
        {
          color = c;
          rc = 1;
        }
      }
      return rc;
    }

    internal delegate bool InitializeCallback(int serial_number);
    internal static InitializeCallback m_Initialize = OnInitialize;
    static bool OnInitialize(int serial_number)
    {
      var eval = FromSerialNumber(serial_number);
      if (eval != null)
      {
        return eval.Initialize();
      }
      return false;
    }

    internal delegate void OnDeleteThisCallback(int serial_number);
    internal static OnDeleteThisCallback m_OnDeleteThis = OnDeleteThis;
    static void OnDeleteThis(int serialNumber)
    {
      var eval = FromSerialNumber(serialNumber);
      if (eval != null)
      {
        eval.Dispose();
      }
    }
#endregion


    #region pointer tracking

    internal IntPtr CppPointer
    {
      get;
      private set;
    }

    //Set to false if we're wrapping const IRhRdkTextureEvaluator
    readonly bool m_bAutoDelete = false;

    readonly int m_runtime_serial_number;
    static int g_serial_number_counter = 1;
    static readonly List<TextureEvaluator> g_all_custom_evaluators = new List<TextureEvaluator>();

    static TextureEvaluator FromSerialNumber(int serialNumber)
    {
      var index = serialNumber - 1;
      if (index >= 0 && index < g_all_custom_evaluators.Count)
      {
        var rc = g_all_custom_evaluators[index];
        if (rc != null && rc.m_runtime_serial_number == serialNumber)
          return rc;
      }
      return null;
    }

    internal static TextureEvaluator FromPointer(IntPtr pTextureEvaluator, bool bIsConstPointer)
    {
      if (pTextureEvaluator == IntPtr.Zero)
      {
        return null;
      }

      int serial_number = UnsafeNativeMethods.CRhCmnRdkTextureEvaluator_IsRhCmnEvaluator(pTextureEvaluator);

      if (serial_number > 0)
      {
        return FromSerialNumber(serial_number);
      }

      return new TextureEvaluator(pTextureEvaluator, bIsConstPointer);
    }

    bool IsNativeWrapper()
    {
      return 0 == m_runtime_serial_number;
    }

    #endregion

    #region disposable implementation
    /// <summary>
    /// For Dispose pattern
    /// </summary>
    ~TextureEvaluator()
    {
      Dispose(false);
    }

    /// <summary>
    /// For Dispose pattern
    /// </summary>
    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    /// <summary>
    /// For Dispose pattern
    /// </summary>
    /// <param name="disposing"></param>
    protected virtual void Dispose(bool disposing)
    {
      if (CppPointer!=IntPtr.Zero && m_bAutoDelete)
      {
        if (IsNativeWrapper())
        {
          UnsafeNativeMethods.Rdk_TextureEvaluator_CallDeleteThis(CppPointer);
        }
        else
        {
          UnsafeNativeMethods.Rdk_TextureEvaluator_Delete(CppPointer);
        }
        CppPointer = IntPtr.Zero;
      }
    }
    #endregion
  }
}



#endif
