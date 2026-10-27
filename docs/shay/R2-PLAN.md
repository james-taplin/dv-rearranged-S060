# Shay R2: smaller wheels, full-size running gear, restored body — plan

Written 27 October 2026 by Claude after James's 0.20.2 drive. Decisions by James:
S282 pony wheelset for the trucks, bulk the running gear up to full-size Shay
proportions, and put the S060's frame/firebox back. Oil cups: three on the gear
side and two on the off side (exact off-side spots to confirm when placing).

## Why R1 cannot keep the native frame

Measured from the shipping bundle swept through the contract's 49 truck poses:
the trucks reach 1.21–1.27 m over nearly their whole length (they ride on
1.139 m scaled S060 drivers, axle at 0.5697 m). The S060 main frame plate goes
down to 0.23 m and the cab-interior firebox fire particle sits at y 1.145,
z -1.59 — directly over the rear truck pivot. So 0.20.x had to cut the frame at
1.285 m and the fire shows through. Restoring the structure means lowering the
trucks.

## R2 geometry targets (to be frozen as fit contract R2)

- **Wheels:** vanilla `s282_wheels_front` (complete pony wheelset, plain disc,
  0.773 m, 30 in; a J. Neils Shay ran on 32 in). Aliased live, unscaled; stock
  gauge. Rolling radius ≈ 0.3865 m, axle at y ≈ 0.3865 (0.183 m lower).
  The mesh is one wheelset (both wheels + axle), so each axle gets one alias,
  not two halves. Physics wheel radius and the S060 PoweredWheel set follow.
- **Trucks:** same stations and native support pivots as R1 unless the fit
  study says otherwise; frames, journals, horns, bolster and brakes regenerate
  0.18 m lower. Target swept top ≤ 1.08 m away from the bolsters.
- **Gearing:** bull-gear ring must fit inside the smaller wheel face (tread
  radius ≈ 0.39): ring pitch radius ≈ 0.30 m, pinion re-sized so the ratio stays
  43:19 unless the fit needs a change. With 0.68× wheel radius at the same ratio,
  tractive effort rises ~1.47× and top speed falls ~0.68× — closer to a real
  Shay; the 0.92 efficiency stays.
- **Bulk (full-size two-truck Shay, ~25 t class):** line shaft ≈ 100 mm dia
  (R1: 40 mm), U-joint yokes/crosses ≈ 160 mm across (R1: 60 mm), sliding square
  section ≈ 90 mm, crankshaft ≈ 110 mm, connecting rods ≈ 75 × 110 mm section
  (R1: ~40 mm), piston rods ≈ 60 mm, crossheads/guides scaled to match, bull
  ring face ≈ 60 mm wide with real cast teeth (R1: 16 mm, no visible teeth),
  pinions with visible teeth. Cylinders keep bore 0.17 m (sim unchanged).
- **Body relief R2:** keep every native piece that clears the R2 sweep; trim
  the frame plate along the sweep profile (not a flat 1.285 cut); keep the
  firebox walls down to the new truck tops and close the ashpan just above
  them; keep springs/sandboxes/tank pieces that 0.20.2 removed but never
  clashed (8,288 native triangles measured as removable-but-clear).
- **Oil cups:** replace R1's eight axlebox + four engine cups with five upright
  cups (three gear side, two off side), never sideways; reuse existing oil
  circuit IDs, dormant extras stay dormant.

## Work order

1. R2 fit study (fit/tools): new contract, pose sweep, keep-outs, gear and
   shaft geometry; freeze `shared/fit-contract.json` R2 (R1 archived).
2. Regenerate trucks, transmission and engine modules from R2 with the bulk
   targets; their own validators and Unity prefabs.
3. Rebuild the Shay bundle (Build.Shay.ps1 with the new contract hash) and the
   runtime: wheelset alias, radius, sweep-profile relief, oil cups.
4. Native suite + renders, package, install on James's go-ahead.

Done so far toward this: f1c8d3e makes the gears actually turn (JsonUtility
returned no transmission parts in game; see HANDOVER).
