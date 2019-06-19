#pragma warning disable 1591
using System;
using System.Collections.Generic;
using System.Text;

namespace Rhino
{
  public interface IEpsilonComparable<in T>
  {
    bool EpsilonEquals(T other, double epsilon);
  }

  public interface IEpsilonFComparable<in T>
  {
    bool EpsilonEquals(T other, float epsilon);
  }
}