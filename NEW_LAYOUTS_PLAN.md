# Plan: 2-6-6-2 Mallet and 2-2-2-2-2 articulated triplex

Agreed with James on 19 Oct 2026. Both are articulated, built the same way as the
existing 2-4-4-2/4-4-4-4 (DV's own bogies plus position/aim constraints, all engines
feeding the one native boiler, shared native traction and slip).

New stable IDs are appended after 36. Existing IDs and saves stay unchanged.

## Phase 1: 2-6-6-2 (ID 37)

Reuses the existing two-engine simulation graph. No change to any S282's saved state.

- **Drivers:** two 3-axle engines on small drivers, so both engines and their
  cylinders fit inside the S282 frame. Starting point is the 12-coupled small-driver
  scale (0.84, about 1.22 m); if that is too long, go down to about 0.75 (1.07 m). The
  rear engine needs its cylinders between the two driver groups, which costs about
  1 m over the 2-12-2.
- **Valve gear:** each engine keeps the native S282 Walschaerts with drivers 1-3,
  where the main rod already drives axle 3, plus the existing three-axle coupling
  rods. No Franklin/duplex bundle parts.
- **Rear engine:** a clone of the native drivetrain, as the 2-4-4-2 does, but with
  the 3-axle configuration.
- **Physics:** six physical powered axles (native drive axles 1-6), three per engine.
  1 leading + 6 + 6 + 1 trailing = 8 axles. Weight on drivers uses the existing
  equal-load approximation.
- **Frame cut:** the main frame is split at the pivot halfway between the two engines
  (the 1.4.5 fix). The pivot and aim target are the same as 2-4-4-2.
- **Rear-engine capacity:** keep the existing rear-engine size for now. A separate
  2-6-6-2 calibration can follow James's driving feedback.
- **Random spawns:** off by default, like the other articulated choices.

## Phase 2: 2-2-2-2-2 triplex (ID 38)

Needs a third native steam engine. This changes the simulation graph of every S282
at spawn: a dormant "middle" engine is added next to the dormant rear engine.

- **Save compatibility:** extend the legacy-load path so old saves get an empty
  middle-engine state, as was done for the rear engine.
- **Engines:** three single-driver engines, each a native drivetrain showing only
  driver 3 (the main-rod crank), with its main rod, crosshead and valve gear and no
  coupling rods.
- **Length:** at full size one such engine is about 5.3 m from cylinder front to
  driver. Three only fit the S282 frame with small drivers (about 0.6-0.7 scale,
  roughly 0.85-1.0 m wheels). It will look compact; that's expected.
- **Articulation:** DV only has two physical bogies.
  - The front engine aims at the front bogie (as now).
  - The middle engine pivots behind it and aims at a point partway to the front
    bogie, so it turns a fraction of the front engine's angle. Visual only.
  - The rear engine is fixed to the boiler.
- **Physics:** five physical axles: leading, three powered drivers, trailing. All
  engines share native crank phase and slip, as the duplex does.
- **Effects and diagnostics:** chuffs, cylinder cocks, drips and crack outlets need a
  third owner in ArticulatedEffects. Diagnostics gain a third engine entry.

## Order of work

1. Phase 1 code, offline validation, build 1.5.0, James tests in game.
   Status 19 Oct: built and offline-validated as 1.5.0, awaiting in-game test.
2. Phase 2 graph change first (third engine dormant on all S282s), checked by
   save/reload of old saves, then the triplex geometry.

## Not in scope

- Compound expansion.
- Separate slip per engine.
- Triplex third engine under the tender (the tender is a separate DV car).
