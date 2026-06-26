# Proposal: make `ViewInfo.Viewport` edits persist without breaking `SetViewProjection`

**Issue:** RH-83697 (Viewport: two-point perspective)
**Target:** Rhino `8.x` branch (ships ~8.34)
**Also fixes:** rhino3dm view-camera persistence regression (8.17 → 8.32)
**Author:** luis (draft)

## Summary

Re-introduce the write-through behavior of `DocObjects.ViewInfo.Viewport` (so mutating it persists to the view — the natural, expected semantics), and make `RhinoViewport.SetViewProjection` defensive against source/target aliasing so the original RH-83697 two-point-perspective bug cannot reoccur. This replaces the 8.14 revert (which removed the write-through behavior to kill the symptom) with a fix that keeps the correct semantics for everyone, including rhino3dm.

## Background / root cause

`ViewInfo.Viewport` returns a `ViewportInfo` proxy whose `m_parent` is the `ViewInfo` (`opennurbs_viewport.cs`). Originally (commit `9e73305`) it had a `NonConstPointer()` override so edits wrote straight into the parent view's `&ON_3dmView.m_vp`. That is the behavior rhino3dm needs: `viewInfo.Viewport.SetCameraLocation(...)` followed by `File3dm.Views.Add(...)` + write/read must round-trip.

That override was reverted in 8.14 because it broke two-point perspective. The real mechanism:

- `ViewInfo(RhinoViewport)` (`opennurbs_3dm_settings.cs:422`) does **not** copy — it borrows the live view pointer:
  ```csharp
  m_ptr = UnsafeNativeMethods.CRhinoViewport_View(rhinoViewPort.ConstPointer());
  m_dontdelete = true;   // borrowed, not owned
  ```
- The Viewport Properties panel (`ViewportPropertiesPanel.cs:1002`) builds a projection on `new ViewInfo(active_view).Viewport`, then applies it back:
  ```csharp
  using (var view_info = new ViewInfo(active_view))            // ALIASES active_view's ON_3dmView
  {
    view_info.Viewport.ChangeToTwoPointPerspectiveProjection(...);
    active_view.SetViewProjection(view_info.Viewport, false);  // source == target's own m_vp
  }
  ```
- With the write-through override, the mutations land in `active_view`'s **live** `m_vp`, and `SetViewProjection` then reads that same memory as its projection source while writing to `active_view` — a self-aliasing read/write that corrupts the result. Without the override, the mutations land on a throwaway copy, so source ≠ target and it happens to work.

So the panel was relying on `ViewInfo.Viewport` being an accidental detached copy. The revert preserved that accident at the cost of the correct write-through semantics (and rhino3dm's camera persistence).

## Proposed change (two parts)

### 1. Restore the write-through override — `opennurbs_viewport.cs`, `ViewportInfo`

```csharp
internal override IntPtr NonConstPointer()
{
  // When this viewport belongs to a parent ViewInfo, edits must write through
  // to the parent view's ON_Viewport (&ON_3dmView.m_vp) so they persist.
  // ON_3dmView_ViewportPointer returns a pointer INTO the view (non-owning),
  // so this proxy allocates nothing and cannot leak.
  var vi = m_parent as ViewInfo;
  if (vi != null)
    return vi.NonConstViewportPointer();
  return base.NonConstPointer();
}
```

Note: this drops the original `if (m_parent != null && !IsNonConst)` fall-through. That `!IsNonConst` path let the base `CommonObject` allocate an untracked non-const copy on a const proxy — the source of the memory leak noted on the issue. A parent-owned proxy now *always* routes to the parent's non-owning pointer; standalone `ViewportInfo`s (no parent) keep the base path.

### 2. Make `SetViewProjection` snapshot its input — `rhinosdkviewport.cs`

```csharp
public bool SetViewProjection(DocObjects.ViewportInfo projection, bool updateTargetLocation)
{
  IntPtr ptr_this = NonConstPointer();
  using (var snapshot = new DocObjects.ViewportInfo(projection))   // independent copy
  {
    bool rc = UnsafeNativeMethods.CRhinoViewport_SetVP(ptr_this, snapshot.ConstPointer(), updateTargetLocation);
    GC.KeepAlive(this);
    return rc;
  }
}
```

Applying a projection from a value-identical copy is semantically transparent, but it guarantees the projection source can never alias the viewport being written — so the panel works whether or not `view_info` aliases `active_view`. This is a single, contained change that protects **every** `SetViewProjection` caller, rather than auditing call sites.

## Why this is safe

- **Override:** non-owning (returns `&view->m_vp`), allocates nothing, no leak. Validated in rhino3dm (full `tests/dotnet` suite, 10/10, including a camera write/read round-trip).
- **Snapshot:** `new ViewportInfo(projection)` deep-copies the `ON_Viewport`; applying identical values has no behavioral change beyond removing the aliasing hazard.
- Together they make `ViewInfo.Viewport` mutate-in-place (least-surprising semantics) while the panel's two-point-perspective path stays correct.

## Test plan

1. RH-83697 repro: Properties → Viewport → Two-point perspective sticks; switching back to Perspective works.
2. `new ViewInfo(view); SetViewProjection(view.Viewport, ...)` round-trips (the huddle code at `ViewportPropertiesPanel.cs:979`).
3. rhino3dm: `ViewInfo.Viewport.SetCameraLocation(p)` → `File3dm.Views.Add` → write/read returns `p` (covered by `tests/dotnet` `ViewTable_CreateFileWithView`).
4. Memory: confirm no per-call `ON_Viewport` leak when repeatedly mutating `viewInfo.Viewport`.

## Rollout / relationship to rhino3dm

- Both changes are RhinoCommon-side. Part 1 (`opennurbs_viewport.cs`) is portable C# that rhino3dm syncs; Part 2 (`rhinosdkviewport.cs`) is `RHINO_SDK`-only and does not affect the rhino3dm build.
- Landing on Rhino `8.x` releases in ~8.34.
- **rhino3dm does not have to wait.** rhino3dm 8.32 ships now with Part 1 as a documented local sync-exception (already implemented + validated). When rhino3dm next syncs to 8.34, Part 1 is already upstream, so the local exception is dropped automatically and rhino3dm inherits the fix cleanly.
