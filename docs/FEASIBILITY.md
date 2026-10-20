> Working copy: `Desktop\Derail Valley Mods\RearrangedS060\FEASIBILITY.md`.
> Section 5 is answered by the P0 probe run; this copy is synced after that.

# Rearranged S060 — feasibility, stage 1

Claude, 19 Oct 2026. Question: can the Rearranged S282 treatment (swap the wheel
arrangement of a stock DV steam loco at runtime, via the comms radio) be done for
the B99 S060 tank engine?

**Verdict: yes, feasible, and cheaper than the S282 work was** — the two hardest
prerequisites are already solved by the same author's code. The limiting factor is
not the drivetrain, it is the short rigid frame: there is room for one extra driver
or a truck, not for the S282's menagerie. Everything below marked UNVERIFIED needs
one in-game probe run before any code is written.

Game build: the installed DV at `B:\SteamLibrary\steamapps\common\Derail Valley`.
All measurements are mine, taken from meshes exported out of
`DerailValley_Data\resources.assets` with AssetStudioModCLI (same extractor the
S282 mod ships) and re-measured from the .obj vertex bounds.

## 1. What the S060 actually is, measured

| Quantity | Value | How measured |
|---|---|---|
| Driver wheel mesh diameter | **1.516 m** (`s060_Wheels_01..03`) | obj bounds, Y and Z both 1.516 |
| S282 driver mesh diameter | **1.516 m** (`s282_wheels_driving_1`) | obj bounds — *identical to the S060's* |
| Driver spacing | **1.55 m** | side rod `s060_mech_wheels_connect` pin bosses: Z −0.114..3.214, boss radius 0.114 → pin centres 0 and 3.10, two gaps |
| Rigid wheelbase | **3.10 m** (S282: 4.80 m) | as above; cross-checked against `s060_brake_shoes` Z span 3.349 (3 shoes at 1.55 + shoe thickness) |
| Body envelope | 8.46 m long (Z −4.364..4.092), 2.81 m wide, 3.87 m tall | `s060_body` obj bounds |
| S282 truck wheel diameter | **0.773 m** (`s282_wheels_front`) | obj bounds — the pony/trailing wheel any new truck would use |

Three coupled axles, no leading or trailing truck, no truck wheel mesh of its own
(there is no `s060_wheels_front`/`_rear` in the asset file — I listed every
`s060_*` name). Side tanks and a bunker, so no tender.

Visual drivetrain hierarchy, from a renderer dump of a real in-game S060
(`Derail Valley\Mods\DadsDecals\diagnostics\L-013_LocoS060.txt:83`):

```
LocoS060_Body/DrivingMechanism_L|_R/
    s060_Wheels_01 .. _03          three drivers per side
    s060_mech_wheels_connect       coupling rod
    s060_mech_push_rod             main rod
    s060_mech_push_rod_to_connect  combination/union link
    s060_mech_07/s060_mech_08/s060_mech_02/s060_mech_01   expansion-link stack
    s060_mech_lifting_arm/s060_mech_reach_rod             reverser gear
```

Walschaerts, laid out and named exactly like the S282's (`s282_mech_wheels_connect`,
`s282_mech_push_rod_to_connect`, …). The meshes sit flat under the body, not under
the physics bogies, which is the same arrangement the S282 mod works with — it
aligns the native axles to the visual wheels rather than the reverse
(`AlignNativeAxle`, `RearrangedS282/WheelRearranger.Articulated.cs:299`).

## 2. The two prerequisites that are already done

1. **Locomotive Mesh Splitter already supports the S060.** The installed
   LocoMeshSplitter 1.1.9 (same author, SatiricRug) contains `S060MeshSplitter`
   LOD0/LOD1 classes, an `S060MeshLoader`, and ships `assets/s060_body.obj` plus
   its LOD1–LOD3 and collider splits. The S282 mod lists LocoMeshSplitter as a hard
   requirement (`Info.json`), and for the S060 that dependency is satisfied out of
   the box. Whether the S060 splitter exposes the cylinder block separately
   (`GetCylinderLMesh`/`MoveCylinders` exist in its API) is **UNVERIFIED**.
2. **Driver meshes are interchangeable.** S060 and S282 drivers are the same
   1.516 m mesh, so added drivers can be clones of `s060_Wheels_02` with no
   rescaling, and a 2-6-x truck can use the S282's own 0.773 m `s282_wheels_front`
   pulled through the extraction path the S282 mod already has
   (`MeshFinder.generateMeshFromGameFiles`, `RearrangedS282/MeshFinder.cs:189`).

Licence is no obstacle: MIT, Copyright (c) 2023 Peter Schinske — a derivative mod
is fine with attribution.

Minor, but useful when drivers move: CCL ships an S060 brake prefab,
`5AT\PROTO 5AT\Assets\CarCreator\Prefabs\[brake s060].prefab`, which is a ready
reference for brake-shoe geometry (`s060_brake_shoes` is one mesh covering all
three axles, so adding or removing a driver means splitting or re-spacing it —
the S282 mod hit the same problem and LocoMeshSplitter ships
`assets/s282_brake_shoes.obj` for it).

## 3. What fits in a 3.10 m frame

Sensible, with the stock 1.516 m drivers at 1.55 m spacing:

- **0-4-0** — drop driver 3, shorten the coupling rod to the two-axle rod the S282
  mod already generates (`MeshFinder.TwoAxleSideRodMesh`). Lowest-risk first spike.
- **0-8-0** — add a fourth driver. Needs 1.55 m of clear frame behind driver 3 or
  ahead of driver 1; the body is 8.46 m long so the space probably exists, but
  clearance against the cab front and the pilot/buffer beam is **UNVERIFIED**.
- **2-6-0 / 0-6-2 / 2-6-2T** — one or two trucks using S282 pony wheels.
- **0-4-2T / 2-4-0** — combinations of the above.

Possible but compromised:

- **0-10-0 / 0-8-2** — only with the drivers scaled down (the S282 mod's standard
  trick; ~0.95–1.1 m), which on a small shunter looks wrong rather than funny.
- **4-6-0** — a four-wheel leading truck needs ~2.5 m clear ahead of driver 1. The
  S060's smokebox sits close to the front axle; I expect this does not fit.
- **0-4-4-0 Mallet** — the S282's articulated path exists but assumes a tender loco
  with a long frame and two native engines. Not a stage-1 target.

## 4. Code: fork, do not generalise (yet)

The S282 mod is ~11.4k lines, and the geometry is baked into it: the comms radio,
spawner, physics and save paths all gate on `TrainCarType.LocoSteamHeavy`
(9 call sites, e.g. `CommsRadioS282Rearranger.cs:149`, `ConventionalSpawn.cs:34`),
and `WheelRearranger.cs` alone is 2,736 lines of S282 axle names and offsets
(`Axle_F/bogie_car/[axle] drive 0`, hard-coded z thresholds at ±2.4,
`SetupFrontBogie` at `WheelRearranger.cs:398`).

Two routes:

- **A. Separate mod, shared technique (recommended for stage 1).** New
  `RearrangedS060` project; lift MeshFinder, the OBJ importer, the comms-radio
  shell, the save/spawn managers and the diagnostics harness roughly as-is
  (~30–40% of the lines), write fresh geometry. No regression risk to a shipping
  mod that is mid-release (1.10.0 released and installed today).
- **B. Generalise RearrangedS282 into a multi-loco mod** with a per-loco geometry
  profile. Better long-term, but it rewrites the core of a mod with 44 live
  arrangements and an open 1.9.2/1.10.x merge (`CURRENT_HANDOVER.md`). Do this
  only after the S060 geometry is proven in route A.

## 5. Open questions — one probe run answers all of them

Nothing here needs the game modded, only a read-only dump, and the project already
has the pattern for it (DVStateProbe, 5ATCabProbe, the DadsDecals diagnostics
dump). Required facts, all currently **UNVERIFIED**:

1. Does the S060 have `Axle_F`/`Axle_R` `Bogie` objects, and what `[axle]` children
   does each hold? The S282 has two powered axles plus one truck axle per bogie; a
   0-6-0 with three axles must split them 2/1 or 1/2, and which way round decides
   where a truck can be added and how the derailment check behaves.
2. World/local Z of each driver axle and of the body origin, and `Bogie.axles`
   radius values — needed before any offset can be computed rather than guessed.
3. Clear frame space ahead of driver 1 and behind driver 3, measured against the
   cab front, firebox and buffer beams.
4. Does the S060 splitter expose the cylinder block, so cylinders can follow a
   moved front driver?
5. The B99 simulation side: the S060's SimulationFlow ports and traction
   definition, and whether added axles need to be registered anywhere (the S282
   mod's note that `PoweredWheelsManager` is unused on steam locos — `TODO.md` —
   should be re-checked for the S060).
6. Animator: `LocoS060_AnimationHigh/Med/Low/LowD` drive the rods; adding or
   hiding a driver has to keep the animation in sync, which is where the S282 mod
   spent several releases (LateUpdate rod sync).

## 6. Suggested staging

- **P0** Probe mod, read-only: dump the S060 hierarchy, axle transforms, bogie
  contents, animator clips and sim ports. Answers §5.
- **P1** Spike: 0-4-0 — hide driver 3, swap to the two-axle coupling rod, align the
  native axle, check rods, animation and derailment. If this works, everything else
  is bookkeeping.
- **P2** 0-8-0 — clone a driver and a native axle, extend the coupling rod.
- **P3** Trucks — 2-6-0 and 0-6-2 with S282 pony wheels and a cloned truck axle.
- **P4** Shell — comms radio, saved arrangements, spawn pool, diagnostics, tests,
  ported from the S282 mod.

No code written, nothing installed, no game files touched.
