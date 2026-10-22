# Rearranged S060 — functional / 0-6-2 acceptance tests

The functional build adds an independent S060 **WHEEL REARRANGER** radio mode, UMM
settings, random spawn selection and per-locomotive arrangement/driver-size
saving. It supports 0-4-0, 0-6-0, 0-8-0 and an experimental stock-driver 0-6-2.
The separate Meyer and Dockside CCL vehicles are not included in the standalone
S060-only package. They are spawned through the normal locomotive spawner.
Requires Derail Valley B99, Unity Mod Manager and LocoMeshSplitter.

The S060-only script stages 0.5.0. The integrated 0.8.0 runtime and separate
Dockside 0.2.0 are installed, alongside Meyer 0.1.0. The scripts do not install.
See Dockside/README.md for the new vehicle's game tests and limits.

## Controls

- Cycle the comms radio to **WHEEL REARRANGER** with the **Select an S060**
  starting prompt (the other mode names S282). Select an S060, use the
  radio +/- controls to choose an arrangement, then click that same locomotive
  to confirm. Aim away and click to cancel. Other vehicle types are ignored.
- **0-6-0 (Stock)** always restores stock wheels and size. The other
  0-4-0, 0-6-0 and 0-8-0 choices use the driver size selected in the mod's UMM
  window when the locomotive was selected. 0-6-2 always uses stock drivers.
  Stop, close the regulator and let force/slip settle
  before applying.
- Ctrl+F10 > Rearranged S060: set driver size; apply or restore running gear
  using the displayed target. The fallback target must be within 30 m.
- UMM saves settings through its normal save/close action. Changing size does
  not resize an already converted car until its arrangement is reapplied.
- Random spawning is off by default; its initial pool contains 0-6-0 and
  0-4-0. 0-8-0 and 0-6-2 are opt-in. An empty pool falls back to stock. Existing loaded
  cars are never randomized retroactively. Driver size applies to new random
  selections; randomization off means new cars are stock size.

## Persistence

Arrangement and size live in native per-car simCarState under `RearrangedS060`,
separate from the S282 mod. The saved choice is reapplied when the car is ready,
stationary and unloaded by the regulator. While waiting, native stock geometry
may be visible. Pending choices remain saved. Unknown future or corrupt mod
payloads are preserved verbatim; an explicit successful selection replaces them.

## Runtime acceptance (James)

1. With both mods installed, check that S282 WHEEL REARRANGER still works and
   S060 WHEEL REARRANGER appears once. Each mode must accept only its own loco.
2. Test select, +/- cycling, cancel, confirmation and refusal while moving or
   with the regulator open. Check the refusal message, then stop and retry.
3. Convert 0-6-0 > 0-4-0 > 0-8-0 > resized 0-6-0 > stock, without an extra
   restore step. Repeat to look for duplicate wheels, rods or highlights.
4. Save two S060s with different arrangements/sizes. Reload the save and check
   each one; repeat after leaving/re-entering the area. Stop and close the
   regulator if an arrangement is reported as pending.
5. Restore one car to stock, save and reload: stock must remain stock.
6. Enable random spawning with a single arrangement checked; newly spawned
   S060s should get that choice. Existing cars must retain their saved choice.
   Test the empty pool and randomization off. Verify settings survive restart.
7. Delete a converted test car through the usual game control, then spawn an
   S060: the new car must not inherit its predecessor's extra running gear.
8. Continue the 0-8-0/resize driving tests: both sides' rod sync, reverse,
   coasting/braking, curves/points, rail contact and return to stock.
9. Open the S060 UMM window for 20 seconds, switch to the probe window for 20
   seconds, then close UMM. Compare FPS. The 0.3.0 test session showed a large
   slowdown while using the spike window. Target discovery now uses DV's native
   car registry at most four times per second instead of a full scene search
   every GUI pass. Its effect on real FPS still needs this comparison.
10. Once 0.5.0 is installed, select 0-6-2 with the size slider away from 1.0:
    all three drivers must remain stock size. Check one extra unpowered pony
    axle behind the rear driver, centred on the rails, with no duplicate wheels
    or support mesh. Capture a probe: expected three powered axles, four total,
    and 75% nominal adhesion weight on drivers. Check pony rotation forwards,
    reverse and under braking, low-speed curves/points, frame/cab/coupler
    clearance, save/reload, switching and stock restoration. The donor lookup
    and native rotator compile against B99 but have not run in the game yet.

Geometry remains based on the 0.3.0 prototype: 0-8-0 has overlapping rod bosses
and no fourth brake mesh. The 0.3.0 resize incorrectly scaled width: James's
0.824x test visibly narrowed the wheels away from the rails. This build retains
the original X scale/mirroring, following the working S282 implementation.
Y/Z layout scaling still changes axle spacing and cylinder placement and hides
brake meshes. Independent wheel-diameter editing remains unfinished. Driving
and corrected rail-contact validation remain pending.

## Build and installation

Run `powershell -ExecutionPolicy Bypass -File .\Build.Functional.ps1` from the
repo, then `powershell -ExecutionPolicy Bypass -File .\Validate.Functional.ps1`.
The ZIP is under `build/functional`, with a SHA-256 receipt alongside it.
Build and validation scripts do not install or launch the game. Before any
authorized install: close the game, back up the current mod, preserve existing
Settings.xml and verify installed DLL/package hashes. Restart the game after
updating; hot unloading is intentionally refused.
