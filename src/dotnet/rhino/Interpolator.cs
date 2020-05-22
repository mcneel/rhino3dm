using System;
using System.Collections.Generic;

using Rhino.Collections;

namespace Rhino.Geometry
{
  /// <summary>
  /// Exposes a set of standard numeric interpolation algorithms.
  /// </summary>
  public class Interpolator : RhinoList<double>
  {
    #region fields
    private bool m_cyclical;
    #endregion

    #region constructors
    /// <summary>
    /// Constructs a new, empty Interpolator.
    /// </summary>
    /// <since>5.0</since>
    public Interpolator() { }
    /// <summary>
    /// Constructs an empty Interpolator with a certain capacity.
    /// </summary>
    /// <param name="initialCapacity">Number of items this interpolator can store without resizing.</param>
    /// <since>5.0</since>
    public Interpolator(int initialCapacity) : base(initialCapacity) { }
    /// <summary>
    /// Copy all the numbers from an existing RhinoList.
    /// </summary>
    /// <param name="list">List to mimic.</param>
    /// <since>5.0</since>
    public Interpolator(RhinoList<double> list) : base(list) { }
    /// <summary>
    /// Constructs an Interpolator from a collection of numbers.
    /// </summary>
    /// <param name="collection">Collection of numbers to duplicate.</param>
    /// <since>5.0</since>
    public Interpolator(IEnumerable<double> collection) : base(collection) { }
    /// <summary>
    /// Constructs a new Interpolator with a specified amount of numbers.
    /// </summary>
    /// <param name="amount">Number of values to add to this Interpolator. Must be equal to or larger than zero.</param>
    /// <param name="defaultValue">Number to add.</param>
    /// <since>5.0</since>
    public Interpolator(int amount, double defaultValue) : base(amount, defaultValue) { }
    #endregion

    #region properties
    /// <summary>
    /// Gets or sets a value indicating whether or not the values inside this Interpolator 
    /// are to be treated as cyclical (i.e. circular).
    /// </summary>
    /// <since>5.0</since>
    public bool Cyclical
    {
      get { return m_cyclical; }
      set { m_cyclical = value; }
    }
    #endregion

    #region interpolation
    /// <summary>
    /// Sample the list of numbers with Nearest Neighbor interpolation. 
    /// </summary>
    /// <param name="t">Parameter to sample at. The integer portion of the parameter 
    /// indicates the index of the left-hand value. If this Interpolator is cyclical, 
    /// parameters will be wrapped.</param>
    /// <returns>The sampled value at t.</returns>
    /// <since>5.0</since>
    public double InterpolateNearestNeighbour(double t)
    {
      if (m_size == 0) { throw new IndexOutOfRangeException("The Interpolator must contain at least one sample value"); }
      if (m_size == 1) { return m_items[0]; }

      int idx;
      double param;

      SolveParameter(t, out idx, out param);
      if (param >= 0.5) { idx = MapIndex(idx + 1); }

      return m_items[idx];
    }
    /// <summary>
    /// Sample the list of numbers with linear interpolation.
    /// </summary>
    /// <param name="t">Parameter to sample at. The integer portion of the parameter 
    /// indicates the index of the left-hand value. If this Interpolator is cyclical, 
    /// parameters will be wrapped.</param>
    /// <returns>The sampled value at t.</returns>
    /// <since>5.0</since>
    public double InterpolateLinear(double t)
    {
      if (m_size == 0) { throw new IndexOutOfRangeException("The Interpolator must contain at least one sample value"); }
      if (m_size == 1) { return m_items[0]; }

      int idx0;
      double param;

      SolveParameter(t, out idx0, out param);
      int idx1 = MapIndex(idx0 + 1);

      return (m_items[idx0] * (1.0 - param) + m_items[idx1] * param);
    }
    /// <summary>
    /// Sample the list of numbers with cosine interpolation.
    /// </summary>
    /// <param name="t">Parameter to sample at. The integer portion of the parameter 
    /// indicates the index of the left-hand value. If this Interpolator is cyclical, 
    /// parameters will be wrapped.</param>
    /// <returns>The sampled value at t.</returns>
    /// <since>5.0</since>
    public double InterpolateCosine(double t)
    {
      if (m_size == 0) { throw new IndexOutOfRangeException("The Interpolator must contain at least one sample value"); }
      if (m_size == 1) { return m_items[0]; }

      int idx0;
      double param;

      SolveParameter(t, out idx0, out param);
      int idx1 = MapIndex(idx0 + 1);

      param = 0.5 * (1.0 - Math.Cos(param * Math.PI));
      return (m_items[idx0] * (1.0 - param) + m_items[idx1] * param);
    }
    /// <summary>
    /// Sample the list of numbers with cubic interpolation.
    /// </summary>
    /// <param name="t">Parameter to sample at. The integer portion of the parameter 
    /// indicates the index of the left-hand value. If this Interpolator is cyclical, 
    /// parameters will be wrapped.</param>
    /// <returns>The sampled value at t.</returns>
    /// <since>5.0</since>
    public double InterpolateCubic(double t)
    {
      if (m_size == 0) { throw new IndexOutOfRangeException("The Interpolator must contain at least one sample value"); }
      if (m_size == 1) { return m_items[0]; }

      int idx1;
      double param;

      SolveParameter(t, out idx1, out param);
      int idx0 = MapIndex(idx1 - 1);
      int idx2 = MapIndex(idx1 + 1);
      int idx3 = MapIndex(idx2 + 1);

      double a0 = m_items[idx3] - m_items[idx2] - m_items[idx0] + m_items[idx1];
      double a1 = m_items[idx0] - m_items[idx1] - a0;
      double a2 = m_items[idx2] - m_items[idx0];
      double a3 = m_items[idx1];

      return (a0 * param * param * param) + (a1 * param * param) + (a2 * param) + a3;
    }
    /// <summary>
    /// Sample the list of numbers with Catmull-Rom interpolation.
    /// </summary>
    /// <param name="t">Parameter to sample at. The integer portion of the parameter 
    /// indicates the index of the left-hand value. If this Interpolator is cyclical, 
    /// parameters will be wrapped.</param>
    /// <returns>The sampled value at t.</returns>
    /// <since>5.0</since>
    public double InterpolateCatmullRom(double t)
    {
      if (m_size == 0) { throw new IndexOutOfRangeException("The Interpolator must contain at least one sample value"); }
      if (m_size == 1) { return m_items[0]; }

      int idx1;
      double param;

      SolveParameter(t, out idx1, out param);
      int idx0 = MapIndex(idx1 - 1);
      int idx2 = MapIndex(idx1 + 1);
      int idx3 = MapIndex(idx2 + 1);

      double a0 = -0.5 * m_items[idx0] + 1.5 * m_items[idx1] - 1.5 * m_items[idx2] + 0.5 * m_items[idx3];
      double a1 = m_items[idx0] - 2.5 * m_items[idx1] + 2 * m_items[idx2] - 0.5 * m_items[idx3];
      double a2 = -0.5 * m_items[idx0] + 0.5 * m_items[idx2];
      double a3 = m_items[idx1];

      return (a0 * param * param * param) + (a1 * param * param) + (a2 * param) + a3;
    }

    ///// <summary>
    ///// Sample the list of numbers with Akima interpolation. 
    ///// Akima interpolation handles outliers more elegantly than Cubic or Catmull-Rom.
    ///// </summary>
    ///// <param name="t">Parameter to sample at. The integer portion of the parameter 
    ///// indicates the index of the left-hand value. If this Interpolator is cyclical, 
    ///// parameters will be wrapped.</param>
    ///// <returns>The sampled value at t.</returns>
    //public double InterpolateAkima(double t)
    //{
    //  if (m_size == 0) { throw new IndexOutOfRangeException("The Interpolator must contain at least one sample value"); }
    //  if (m_size == 1) { return m_items[0]; }

    //  int idx;
    //  double param;

    //  SolveParameter(t, out idx, out param);

    //  double s0 = m_items[MapIndex(idx - 2)];
    //  double s1 = m_items[MapIndex(idx - 1)];
    //  double s2 = m_items[idx];
    //  double s3 = m_items[MapIndex(idx + 1)];
    //  double s4 = m_items[MapIndex(idx + 2)];

    //  double d0 = s1 - s0;
    //  double d1 = s2 - s1;
    //  double d2 = s3 - s2;
    //  double d3 = s4 - s3;

    //  double w1 = Math.Abs(d3 - d2);
    //  double w2 = Math.Abs(d1 - d0);

    //  double si_enum = (w1 * d1) + (w2 * d2);
    //  double si_denom = (w1 + w2);
    //  double si = si_enum / si_denom;



    //  double a0;
    //  double a1;
    //  double a2;
    //  double a3;

    //  return a0 + (a1 * t) + (a2 * t * t) + (a3 * t * t * t);
    //}

    /// <summary>
    /// Map a sample index onto the actual sample list.
    /// </summary>
    /// <param name="index">Index to map.</param>
    /// <returns>The mapped index.</returns>
    private int MapIndex(int index)
    {
      int N = m_size - 1;

      if (m_cyclical)
      {
        if (index < 0)
        {
          int rc = m_size + (index % m_size);
          if (rc == m_size) { rc = 0; }
          return rc;
        }
        if (index > N) { return index % m_size; }
        return index;
      }
      if (index <= 0) { return 0; }
      return index >= N ? N : index;
    }
    /// <summary>
    /// Decompose a sampling parameter into an index and a unitized parameter.
    /// </summary>
    /// <param name="param">Parameter to decompose.</param>
    /// <param name="index">Integer portion of parameter.</param>
    /// <param name="t">Floating point portion of parameter.</param>
    private void SolveParameter(double param, out int index, out double t)
    {
      int N = m_size - 1;

      if (m_cyclical)
      {
        double floor = Math.Floor(param);
        index = MapIndex((int)floor);
        t = param - floor;

        if (t <= 0.0) { t = 0.0; }
        else if (t >= 1.0) { t = 1.0; }
      }
      else
      {
        if (param <= 0) { index = 0; t = 0.0; return; }
        if (param >= N) { index = N - 1; t = 1.0; return; }

        double floor = Math.Floor(param);
        index = (int)floor;
        t = param - floor;

        if (index < 0) { index = 0; }
        else if (index > N) { index = N; }

        if (t <= 0.0) { t = 0.0; }
        else if (t >= 1.0) { t = 1.0; }
      }
    }
    #endregion
  }
}
