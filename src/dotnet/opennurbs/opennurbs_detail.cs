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
    /// <since>5.0</since>
    public bool IsParallelProjection
    {
      get
      {
        IntPtr pConstThis = ConstPointer();
        bool rc = UnsafeNativeMethods.ON_DetailView_GetBool(pConstThis, idxIsParallelProjection);
        GC.KeepAlive(this);
        return rc;
      }
      set
      {
        if (IsParallelProjection != value)
        {
          IntPtr pThis = NonConstPointer();
          UnsafeNativeMethods.ON_DetailView_SetBool(pThis, idxIsParallelProjection, value);
          GC.KeepAlive(this);
        }
      }
    }

    /// <summary>
    /// Gets or sets whether the view is perspective.
    /// </summary>
    /// <since>5.0</since>
    public bool IsPerspectiveProjection
    {
      get
      {
        IntPtr pConstThis = ConstPointer();
        bool rc = UnsafeNativeMethods.ON_DetailView_GetBool(pConstThis, idxIsPerspectiveProjection);
        GC.KeepAlive(this);
        return rc;
      }
      set
      {
        if (IsPerspectiveProjection != value)
        {
          IntPtr pThis = NonConstPointer();
          UnsafeNativeMethods.ON_DetailView_SetBool(pThis, idxIsPerspectiveProjection, value);
          GC.KeepAlive(this);
        }
      }
    }

    /// <summary>
    /// Gets or sets whether the view is locked.
    /// </summary>
    /// <example>
    /// <code source='examples\vbnet\ex_addlayout.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_addlayout.cs' lang='cs'/>
    /// <code source='examples\py\ex_addlayout.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    public bool IsProjectionLocked
    {
      get
      {
        IntPtr pConstThis = ConstPointer();
        bool rc = UnsafeNativeMethods.ON_DetailView_GetBool(pConstThis, idxIsProjectionLocked);
        GC.KeepAlive(this);
        return rc;
      }
      set
      {
        if (IsProjectionLocked != value)
        {
          IntPtr pThis = NonConstPointer();
          UnsafeNativeMethods.ON_DetailView_SetBool(pThis, idxIsProjectionLocked, value);
          GC.KeepAlive(this);
        }
      }
    }

    /// <summary>
    /// Gets the ratio of page units to model units for this detail view.
    /// Returns 0 if the projection is not parallel.
    /// </summary>
    /// <remarks>
    /// This property is only meaningful when <see cref="IsParallelProjection"/> is true.
    /// For non-parallel projections, the ratio is undefined and this property returns 0.
    /// </remarks>
    /// <since>5.0</since>
    public double PageToModelRatio
    {
      get
      {
        if (!IsParallelProjection)
          return 0;
        IntPtr pConstThis = ConstPointer();
        double rc = UnsafeNativeMethods.ON_DetailView_GetPageToModelRatio(pConstThis);
        GC.KeepAlive(this);
        return rc;
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
    /// <since>5.0</since>
    public bool SetScale(double modelLength, Rhino.UnitSystem modelUnits, double pageLength, Rhino.UnitSystem pageUnits)
    {
      // SetScale only works on parallel projections
      if (!IsParallelProjection)
        return false;

      IntPtr pThis = NonConstPointer();
      bool rc = UnsafeNativeMethods.ON_DetailView_SetScale(pThis, modelLength, (int)modelUnits, pageLength, (int)pageUnits);
      GC.KeepAlive(this);
      return rc;
    }
  }
}
