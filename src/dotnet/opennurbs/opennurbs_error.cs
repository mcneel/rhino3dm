// this was never wrapped in older Rhino_DotNET.DLL
// It would be nice to come up with a good error/warning system for plug-in developers





using Rhino;
using System;
using System.Collections.Generic;
using System.Linq;
namespace Rhino
{
/// <summary>
/// A class used to signal failure conditions of the 2D MeshCreateFromClosedPolylinesAndPoints.
/// </summary>
internal sealed class TesselationFailure {
  /// <summary>
  /// Gets the indices of open polylines.
  /// </summary>
  public List<int> OpenPolylines = new List<int>();

  /// <summary>
  /// Gets the indices of self-intersecting polylines.
  /// </summary>
  public List<int> SelfIntersectingPolylines = new List<int>();

  /// <summary>
  /// Gets the pairs of indices of mutually intersecting polylines.
  /// </summary>
  public List<IndexPair> MutuallyIntersectingPolylines = new List<Rhino.IndexPair>();

  /// <summary>
  /// Is true if not all polylines are contained in one
  /// </summary>
  public bool PolylinesNotContainedInOne = false;

  /// <summary>
  /// Gets a description of the failure condition.
  /// </summary>
  public string What;

  /// <summary>
  /// Checks if any failure condition is present.
  /// </summary>
  /// <returns>True if any failure condition is present; otherwise, false.</returns>
  public bool Any()
  {
    return PolylinesNotContainedInOne || OpenPolylines.Any() || SelfIntersectingPolylines.Any() || MutuallyIntersectingPolylines.Any();
  }
}
}
