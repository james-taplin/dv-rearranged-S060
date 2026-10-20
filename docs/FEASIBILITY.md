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
and the coupler, and a fourth driver only fits if the whole driver group is
re-centred. Rescaling the drivers, as the inherited S282 code already does, widens
every one of those limits.

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
- **0-8-0** — fits, but only by **re-centring the whole driver group**. Taking the
  coupler boxes as the limit, a full-size wheel centre can sit anywhere from
  Z −2.758 to +2.758 (±3.470 less the 0.712 m radius), a 5.52 m band; four
  drivers at the stock 1.549 m spacing need 4.65 m of it, so they fit symmetrically
  at about ±2.32 and ±0.77 with 0.87 m to spare. What does *not* work — and what
  I wrongly reported before measuring — is hanging a fourth driver off the front
  while the other three stay put: that one would centre at +3.25 and foul the front
  coupler box. Re-centring means moving all four drivers, both coupling rods and
  the main rod, and the front driver would then sit inside the present valve-gear
  zone (Z 1.875–3.460), so the gear has to be re-rodded — which adding a driver
  implies anyway.
- **Rescaled drivers** — the inherited S282 code already scales drivers and gear
  together (it runs 0.68 for the 4-14-4), and shrinking them widens the band: at
  1.2 m drivers (r 0.6) it grows to ±2.87, i.e. 5.74 m. That is what makes the
  longer rigid arrangements possible rather than merely cramped.
- **0-10-0** — plausible *with* rescaling: five drivers at 1.3 m spacing need
  5.20 m against the 5.74 m band that 1.2 m drivers give. At full size it needs
  6.20 m and does not fit.
- **0-2-2-0 duplex, Franklin poppet gear** — the joke is supported by what the
  fork already inherits: the S282's duplex simulation (`Sim/Duplex*.cs`,
  `Patches/DuplexSimPatches.cs`, `Patches/AddTwoCylinders.cs`) and the Franklin
  cylinder meshes in `models/` and the `s282_drivetrain` bundle. Note the csproj
  currently excludes those sim files from the build
  (`<Compile Remove="Sim\**\*.cs;...">`), so re-enabling them is part of the job.
  Two single-driver engines with ~2.3 m Franklin cylinders want roughly 4.6 m plus
  wheels against a 5.5 m band, so the drivers would have to come down to about
  0.8–1.0 m. Tight, not absurd — it needs laying out properly before it is a
  promise.
- **4-6-0 / 0-8-2** — still no. A four-wheel leading truck wants ~2.5 m ahead of
  the front driver, and after re-centring there is less room there, not more.
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
- **P1** — built, awaiting an in-game test. Drops the middle driver, rebuilds
  `Bogie.Axles` without its axle, re-registers two powered axles, writes
  `poweredAxles.NUM`, moves the adhesion weight onto the two drivers and hides
  that axle's caliper. Bogie pivots left as DV set them, so the ride should be
  unchanged — that is the main thing the test checks. Test build and checklist:
  `RearrangedS060/test-P1-0.1.0/`.
- **P2** — 0-6-2: trailing truck behind driver 1 with S282 pony wheels, a cloned
  axle on `Axle_R`, and the rear subframe adjusted. Cheaper than P3, so it comes
  first now.
- **P3** — forward wheels and bigger rigid frames: 2-6-0 (the split cylinder block
  has to move), then 0-8-0 by re-centring the driver group, then driver rescaling,
  which is what unlocks 0-10-0.
- **P3.5** — the 0-2-2-0 Franklin duplex meme, once rescaling works: re-enable the
  inherited duplex sim files and lay out two single-driver engines.
- **P4** — shell: comms radio, saved arrangements, spawn pool, diagnostics, tests,
  ported from the S282 mod.

Nothing has been changed in the game. The only thing installed is the read-only
probe.
