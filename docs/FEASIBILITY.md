> Working copy: `Desktop\Derail Valley Mods\RearrangedS060\FEASIBILITY.md`.

# Rearranged S060 — feasibility

Claude, 19 Oct 2026; section 5 answered and sections 1–3 revised from probe data,
20 Oct 2026. Question: can the Rearranged S282 treatment (swap the wheel
arrangement of a stock DV steam loco at runtime, via the comms radio) be done for
the B99 S060 tank engine?

**Verdict: yes, feasible, and cheaper than the S282 work was** — the two hardest
prerequisites are already solved by the same author's code. The limiting factor is
the short frame, and the probe moved *which* arrangements are cheap: adding wheels
**behind** the rear driver is easy, adding them in **front** fights the cylinders
and the coupler, and a fourth driver at stock spacing does not fit at all.

Evidence is of two kinds, and the text says which:

- **Mesh figures** — exported out of `DerailValley_Data\resources.assets` with
  AssetStudioModCLI (the extractor the S282 mod ships) and measured from the .obj
  vertex bounds.
- **Probe figures** — read out of a live S060 (car L-058, game `99-build2702`) by
  the read-only RearrangedS060Probe 0.1.0. Three runs, all agreeing; the one cited
  throughout is the cab run, which also dumps the interior:
  `Derail Valley\Mods\RearrangedS060Probe\RearrangedS060Probe-report-20261020-011059.txt`.
  Line references below are into that file. All its figures are in the TrainCar's
  local space, **+Z forward**.

## 1. What the S060 actually is, measured

| Quantity | Value | Source |
|---|---|---|
| Driver tread diameter | **1.424 m** (56 in) — rolling radius 0.712 m | probe `:1605`, `PoweredWheel` axle height |
| Driver mesh diameter over flanges | 1.516 m mesh / 1.528–1.533 m in world scale | mesh bounds; probe `:32` |
| S282 driver mesh diameter | **1.516 m** — *identical mesh size to the S060's* | mesh bounds, `s282_wheels_driving_1` |
| Driver Z centres | **−1.3978, +0.1498, +1.7006** | probe `:32`–`:34` |
| Driver spacing | **1.5476 m** and **1.5508 m** | probe `:35`–`:36` |
| Rigid wheelbase | **3.0985 m** (S282: 4.80 m) | probe `:37` |
| Body shell extent | Z −4.295 .. +4.004, X ±1.672, Y −0.006 .. 3.878 | probe `:245` |
| Cab | Z −3.487 .. −1.313, **Y 2.399 .. 3.695** — high up, not at wheel height | probe `:246` |
| Valve gear and rods | Z −1.578 .. **+3.460**, Y 0.228 .. 1.611 | probe `:260` |
| Brake shoes (one mesh, all three axles) | Z −0.900 .. +2.449 | probe `:255` |
| Mass | 25,230 kg, centre of mass Z −0.097 | probe `:16` |
| Which end is the front | cab is at **−Z**; the smokebox end is +Z | probe `:660` |
| S282 truck wheel diameter | 0.773 m — the pony/trailing wheel a truck would use | mesh bounds, `s282_wheels_front` |

Three coupled axles, no leading or trailing truck, and no truck wheel mesh of its
own (there is no `s060_wheels_front`/`_rear` in the asset file). Side tanks and a
bunker, so no tender.

### Physics axles do not pair with the visual wheels the obvious way

The split is **2 + 1, and the single axle is at the cab end** (probe `:52`–`:118`):

| Bogie | Pivot Z | Axles | Axle Z | Visual driver |
|---|---|---|---|---|
| `Axle_F` | +0.929 | 2 (`distanceFromBogiePivot` ±0.775) | +1.704, +0.154 | drivers 3 and 2 |
| `Axle_R` | −1.396 | 1 (`distanceFromBogiePivot` 0) | −1.396 | driver 1 |

Two things follow. First, the axle GameObjects are named plainly **`[axle]`**, not
`[axle] drive N` as on the S282, so the S282 mod's `Find("[axle] drive 0")` lookups
have no S060 equivalent — axles must be identified by `distanceFromBogiePivot` or
child order. Second, `Axle_R` has exactly one axle, so a 0-4-0 must drop a driver
from `Axle_F`; dropping driver 1 would leave a bogie with no axles.

### Visual drivetrain

`LocoS060_Body/DrivingMechanism_L|_R` hold the wheels and rods flat, not parented
to the physics bogies — the same arrangement the S282 mod works with, aligning
native axles to the visual wheels rather than the reverse (`AlignNativeAxle`,
`RearrangedS282/WheelRearranger.Articulated.cs:299` in the S282 repo). Walschaerts,
named and laid out like the S282's.

## 2. The prerequisites that are already done

1. **Locomotive Mesh Splitter already supports the S060, cylinders included.**
   LocoMeshSplitter 1.1.9 is loaded on the car as `S060MeshLoader`, and
   `S060LOD0MeshSplitter` (LOD1 too) exposes `GetCylinderLMesh` /
   `GetCylinderRMesh` with `cylinderLimitX/Y/Z`, plus `GetPilotMesh`,
   `GetBumperMesh`, `GetRearSubframeLeft/RightMesh` and — usefully —
   **`GetCaliperL1..L3` / `GetCaliperR1..R3`, one brake caliper per axle**
   (probe `:727`, members at `:905`–`:1219`). That last one removes a problem I had flagged: the
   brake shoes are a single mesh covering all three axles, and the splitter
   already separates them. Note the convenience method `MoveCylinders` exists only
   on `S282AMeshLoader`, not the S060 loader, so moving the split cylinder objects
   is on us.
2. **Driver meshes are interchangeable.** S060 and S282 drivers are both 56 in
   over the tread and the same 1.516 m mesh, so added drivers can be clones of
   `s060_Wheels_02` with no rescaling, and a truck can use the S282's 0.773 m
   `s282_wheels_front` pulled through the extraction path the S282 mod already has
   (`MeshFinder.generateMeshFromGameFiles`).

Licence is no obstacle: MIT, Copyright (c) 2023 Peter Schinske — a derivative mod
is fine with attribution.

## 3. What fits, from the collider and renderer measurements

There are only **7 colliders** on the whole car (probe `:640`). The body box
(Z −4.200..+4.000, Y 0.420..3.820) already encloses the existing drivers, so it is
not a placement constraint; the cab is at Y 2.4+, well above wheel height, so it
does not block wheels either despite being close in Z. What actually constrains
placement is the two coupler boxes at **Z ±3.470..4.470, Y 0.825..1.275**, the
valve gear reaching to **Z +3.460**, and the body shell ending at Z +4.004 / −4.295.

Usable gaps from the outer drivers' faces:

- **Behind driver 1 (−Z):** rear driver face at Z −2.164, rear coupler box at
  −3.470 → **1.31 m of clear low space**, and the splitter has the rear subframe as
  a movable part. A 0.773 m trailing wheel centred near Z −2.8 spans −3.19..−2.42
  and fits with room to spare.
- **In front of driver 3 (+Z):** front driver face at +2.464, front coupler box at
  +3.470 → **1.01 m**, but the cylinders and valve gear occupy that whole span up
  to +3.460.

So, revised:

- **0-4-0** — drop driver 2 or 3 (not driver 1, see §1), shorten the coupling rod
  to the two-axle rod the S282 mod already generates. Still the cheapest spike.
- **0-6-2** — trailing truck behind driver 1. Now the *second* cheapest, and the
  cheapest way to add wheels. Nothing structural is in the way.
- **2-6-0 / 2-6-2T** — a leading truck lands in the cylinder and valve-gear zone.
  Possible, because the cylinders are splittable and movable, but it means moving
  the cylinder block and retiming the gear — a bigger job than the trailing truck.
- **0-8-0** — **does not fit at stock spacing.** A fourth driver at 1.549 m would
  centre at Z +3.250 and span 2.485..4.015, overlapping the front coupler box by
  ~0.55 m and reaching past the body shell front. It needs either reduced spacing
  (~1.0–1.2 m, which then sits inside the cylinder zone anyway) or smaller drivers.
  This is a *downgrade* from my pre-probe guess that the 8.46 m body left room.
- **0-10-0, 0-8-2, 4-6-0** — not without shrunken drivers, and 4-6-0 needs ~2.5 m
  ahead of driver 3 that plainly is not there.
- **0-4-4-0 Mallet** — the S282's articulated path assumes a long-framed tender
  loco with two native engines. Not a target.

## 4. Code: fork, do not generalise (yet)

Done: the fork is `B:\GitHub repo\rearranged-s060`
(`github.com/james-taplin/dv-rearranged-S060`), cut from the S282 B99 port at
`65c8e8a`, renamed to RearrangedS060 0.1.0, with the S282's release paperwork and
arrangement-specific validators pruned and both S282 remotes wired up for
cherry-picking. Stage 1 builds clean and patches nothing.

The reasoning stands: the S282 mod is ~11.4k lines with its geometry baked in —
nine `TrainCarType.LocoSteamHeavy` gates, and `WheelRearranger.cs` alone is 2,736
lines of S282 axle names and offsets. Generalising it into a multi-loco mod with
per-loco geometry profiles is the better long-term shape, but only once the S060
geometry is proven here; it would otherwise rewrite the core of a mod with 44 live
arrangements mid-release.

## 5. The six open questions — answered

All from the probe run cited above. Nothing here is inferred.

1. **`Axle_F`/`Axle_R` and their `[axle]` children** — both present, `Axle_F` with
   2 axles and `Axle_R` with 1, the single one at the cab end. Axles are named
   `[axle]`, not `[axle] drive N`. Every axle carries a `PoweredWheel`. Full table
   in §1 (probe `:52`–`:118`).
2. **Axle Z and radii** — driver centres −1.3978 / +0.1498 / +1.7006, spacing
   1.5476 and 1.5508, wheelbase 3.0985 m, rolling radius 0.712 m, and
   `LocoS060_Body` sits on the car origin (probe `:32`–`:44`).
3. **Clear frame space** — 1.31 m behind driver 1, 1.01 m in front of driver 3,
   both bounded by the coupler boxes; the cab is above wheel height and the body
   collision box already contains the drivers. See §3 (probe `:640`–`:724`).
4. **Does the splitter expose the cylinders?** — **Yes**, `GetCylinderLMesh` /
   `GetCylinderRMesh` on `S060LOD0MeshSplitter` and its LOD1 twin, plus pilot,
   bumper, rear subframe and per-axle brake calipers. `S060MeshLoader` is live on
   the car (probe `:1220`). No `MoveCylinders` helper for the S060.
5. **Simulation** — `SimController` with a full `[sim]` graph, and the S282 mod's
   note that `PoweredWheelsManager` is unused on steam locos **does not hold for
   the S060**: `PoweredWheelsManager` on `[poweredWheels]` lists 3 powered wheels
   bound to the `DrivingMechanism_R` wheel transforms, and `[sim]/poweredAxles` is
   a `ConfigurablePortDefinition` reading **`poweredAxles.NUM = 3`** (READONLY_OUT)
   that feeds traction and `WheelslipController`. Changing the driver count means
   updating that manager and that port — more work than the S282 needed, but it is
   one port rather than scattered physics (probe `:1603`–`:1612`, port at `:1494`).
6. **Animators** — two `Animator`s, one per `DrivingMechanism_L|_R`, both running
   the `LocoS060_Drivetrain` controller with clips `LocoS060_AnimationLow/Med/High`
   (1 s, 8 fps, looping) driven by float parameters **`SpeedMultiplier`** and
   **`Cutoff`**. There are no `LocoS060_Animation*` *objects* — I had guessed those
   names were hierarchy roots; they are clip names. A
   `PoweredWheelRotationViaAnimation` sits on `[wheel rotation]`, so an added wheel
   must either live inside the animated hierarchy or be driven explicitly
   (probe `:1628`–`:1660`, `[wheel rotation]` at `:1612`).

## 6. Suggested staging

- **P0** — done. Probe mod built, installed and run; section 5 is its output.
- **P1** — spike: 0-4-0. Hide driver 2 or 3, swap to the two-axle coupling rod,
  drop the matching `[axle]` from `Axle_F`, update `poweredAxles.NUM` and the
  `PoweredWheelsManager` list, hide that axle's brake caliper. Check rods,
  animation, wheelslip and derailment.
- **P2** — 0-6-2: trailing truck behind driver 1 with S282 pony wheels, a cloned
  axle on `Axle_R`, and the rear subframe adjusted. Cheaper than P3, so it comes
  first now.
- **P3** — forward wheels: 2-6-0, which needs the split cylinder block moved, and
  a reduced-spacing or small-driver 0-8-0 if it still looks right.
- **P4** — shell: comms radio, saved arrangements, spawn pool, diagnostics, tests,
  ported from the S282 mod.

Nothing has been changed in the game. The only thing installed is the read-only
probe.
