using System;
using System.Runtime.Serialization;

namespace Rhino.Geometry
{
  /// <summary>
  /// Represents a view of the model placed on a page layout.
  /// </summary>
  [Serializable]
  public class DetailView : GeometryBase
  {
    internal DetailView(IntPtr native_ptr, object parent)
      : base(native_ptr, parent, -1)
    { }

    /// <summary>
    /// Protected serialization constructor for internal use.
    /// </summary>
    /// <param name="info">Data to be serialized.</param>
    /// <param name="context">Serialization stream.</param>
    protected DetailView(SerializationInfo info, StreamingContext context)
      : base (info, context)
    {
    }

    internal override GeometryBase DuplicateShallowHelper()
    {
      return new DetailView(IntPtr.Zero, null);
    }

    const int idxIsParallelProjection = 0;
    const int idxIsPerspectiveProjection = 1;
    const int idxIsProjectionLocked = 2;

    /// <summary>
    /// Gets or sets whether the view is parallel.
    /// </summary>
    public bool IsParallelProjection
    {
      get
      {
        IntPtr pConstThis = ConstPointer();
        return UnsafeNativeMethods.ON_DetailView_GetBool(pConstThis, idxIsParallelProjection);
      }
      set
      {
        if (IsParallelProjection != value)
        {
          IntPtr pThis = NonConstPointer();
          UnsafeNativeMethods.ON_DetailView_SetBool(pThis, idxIsParallelProjection, value);
        }
      }
    }

    /// <summary>
    /// Gets or sets whether the view is perspective.
    /// </summary>
    public bool IsPerspectiveProjection
    {
      get
      {
        IntPtr pConstThis = ConstPointer();
        return UnsafeNativeMethods.ON_DetailView_GetBool(pConstThis, idxIsPerspectiveProjection);
      }
      set
      {
        if (IsPerspectiveProjection != value)
        {
          IntPtr pThis = NonConstPointer();
          UnsafeNativeMethods.ON_DetailView_SetBool(pThis, idxIsPerspectiveProjection, value);
        }
      }
    }

    /// <summary>
    /// Gets or sets whether the view projection is locked.
    /// </summary>
    /// <example>
    /// <code source='examples\vbnet\ex_addlayout.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_addlayout.cs' lang='cs'/>
    /// <code source='examples\py\ex_addlayout.py' lang='py'/>
    /// </example>
    public bool IsProjectionLocked
    {
      get
      {
        IntPtr pConstThis = ConstPointer();
        return UnsafeNativeMethods.ON_DetailView_GetBool(pConstThis, idxIsProjectionLocked);
      }
      set
      {
        if (IsProjectionLocked != value)
        {
          IntPtr pThis = NonConstPointer();
          UnsafeNativeMethods.ON_DetailView_SetBool(pThis, idxIsProjectionLocked, value);
        }
      }
    }

    /// <summary>
    /// Gets the page units/model units quotient.
    /// </summary>
    public double PageToModelRatio
    {
      get
      {
        if (!IsParallelProjection)
          return 0;
        IntPtr pConstThis = ConstPointer();
        return UnsafeNativeMethods.ON_DetailView_GetPageToModelRatio(pConstThis);
      }
    }

    /// <summary>
    /// Sets the detail viewport's projection so geometry is displayed at a certain scale.
    /// </summary>
    /// <param name="modelLength">Reference model length.</param>
    /// <param name="modelUnits">Units for model length.</param>
    /// <param name="pageLength">Length on page that the modelLength should equal.</param>
    /// <param name="pageUnits">Units for page length.</param>
    /// <returns>
    /// true on success. false if the DetailView projection is perspective or input values are incongruous.
    /// </returns>
    /// <example>
    /// <code source='examples\vbnet\ex_addlayout.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_addlayout.cs' lang='cs'/>
    /// <code source='examples\py\ex_addlayout.py' lang='py'/>
    /// </example>
    public bool SetScale(double modelLength, Rhino.UnitSystem modelUnits, double pageLength, Rhino.UnitSystem pageUnits)
    {
      // SetScale only works on parallel projections
      if (!IsParallelProjection)
        return false;

      IntPtr pThis = NonConstPointer();
      return UnsafeNativeMethods.ON_DetailView_SetScale(pThis, modelLength, (int)modelUnits, pageLength, (int)pageUnits);
    }
  }
}
