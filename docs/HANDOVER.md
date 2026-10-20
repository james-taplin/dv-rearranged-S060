# Rearranged S060 — handover

Written by Claude, 20 Oct 2026, at commit `cbbdcfe` (version 0.3.0). Everything
below is either measured or marked as unverified; nothing is inferred.

## What this is

A Derail Valley mod that changes the wheel arrangement of the **S060**, the B99
0-6-0 tank engine, at runtime — the Rearranged S282 idea applied to the shunter.
Forked from the Rearranged S282 B99 port at `65c8e8a`, MIT, Copyright (c) 2023
Peter Schinske.

- Repo: `B:\GitHub repo\rearranged-s060`, branch `rearranged-s060`, public at
  `github.com/james-taplin/dv-rearranged-S060`.
- Remotes: `origin` (this mod), `s282` (james-taplin/dv-rearranged-S282) and
  `s282-upstream` (pjschinske) for cherry-picking.
- Build: `powershell -ExecutionPolicy Bypass -File .\Build.Stage1.ps1` →
  `build\stage1\`, with a hash receipt. It installs nothing.
- Installed in game: **0.3.0**, DLL SHA-256 `47F12FE2CD489FF3…`. Earlier builds
  are backed up under `Desktop\Derail Valley Mods\RearrangedS060\`.
- House rule: **never copy into the game's Mods folder without James's explicit
  go-ahead**, and he runs the game, not us.

## How it is built

Stage 1 (the default) is the spike build: a mod-manager window
(Ctrl+F10 → Mods → expand the entry), no comms radio, no persistence, no spawn
hooks. The only Harmony patch is the inherited `Bogie.Axles` filter, which drops
deactivated axles from the rebuilt list. The inherited S282 geometry (the
2,736-line `WheelRearranger.cs` and friends) still compiles but is never reached.

Our own files:

| File | What it does |
|---|---|
| `S060Parts.cs` | Resolves body, both `DrivingMechanism_L/_R`, bogies, wheels, axles, PoweredWheels. Maps axles to wheels **by car-local Z**, because the S060's axles are all named plainly `[axle]`. |
| `S060Conversion.cs` | Base: one conversion per car, stationary gate, all-or-nothing rollback, axle/wheel pairing, and `S060FollowPose` so added parts copy an animated part's pose in `LateUpdate`. |
| `S060DriverScale.cs` | Scales both `DrivingMechanism` transforms (wheels + rods + gear + cylinders together). Hides brake shoes and calipers at non-stock sizes. |
| `S060FourCoupled.cs` | 0-4-0: drops the middle driver. |
| `S060EightCoupled.cs` | 0-8-0: adds a fourth driver behind driver 1. |
| `S060Rescaled.cs` | Stock 0-6-0 at a different driver size, nothing else. |
| `S060SpikeWindow.cs` | The window: driver-size slider plus the conversion buttons. |
| `B99PhysicsState.cs` | Inherited, now takes the expected powered-axle count (S060 = 3, S282 = 4). Does all the sim work: powered wheels, `poweredAxles.NUM`, wheel radius, adhesion, axle counts. |

## Measured facts (do not re-derive these)

From the read-only probe mod `RearrangedS060Probe` (source under
`Desktop\Derail Valley Mods\Mods (code)\RearrangedS060Probe`, installed in game,
reports in its own folder). Game `99-build2702`.

- Drivers: **1.424 m over the tread** (rolling radius 0.712128), 1.527 m mesh
  over flanges. Same wheel size as the S282's.
- Driver car-local Z: **−1.3978, +0.1498, +1.7006**. Spacing 1.5476 and 1.5508.
  Rigid wheelbase 3.0985 m. Mass 25,230 kg. Cab is at **−Z**; +Z is the front.
- Physics axles split **2 + 1**: `Axle_F` (pivot +0.929) holds drivers 3 and 2 at
  `distanceFromBogiePivot` ±0.775; `Axle_R` (pivot −1.396) holds driver 1 alone.
  **`Axle_R` cannot give up its axle** — that is why the 0-4-0 drops driver 2.
- Only **7 colliders** on the car. The body box (Z −4.200..+4.000, Y 0.420..3.820)
  already contains the drivers, so it does not constrain placement. The coupler
  boxes at **Z ±3.470..4.470, Y 0.825..1.275** do, as does the valve gear, which
  reaches **Z +3.460**.
- Clear low space: **1.31 m behind driver 1**, **1.01 m in front of driver 3**.
  A full-size wheel centre can sit between Z −2.758 and +2.758.
- `PoweredWheelsManager` **is** used here (unlike the S282, whose TODO says
  otherwise), and `[sim]/poweredAxles` reads `poweredAxles.NUM` into traction and
  `WheelslipController`. Both must be updated when the driver count changes;
  `B99PhysicsState` does it.
- Animation: two `Animator`s, one per mechanism, running `LocoS060_Drivetrain`
  with clips `LocoS060_AnimationLow/Med/High` driven by float parameters
  `SpeedMultiplier` and `Cutoff`. Added parts are not in those clips.
- LocoMeshSplitter 1.1.9 is live on the car as `S060MeshLoader` and exposes
  `GetCylinderLMesh`/`GetCylinderRMesh`, `GetPilotMesh`, `GetBumperMesh`,
  `GetRearSubframeLeft/RightMesh` and **per-axle brake calipers**
  (`s060_caliper_l_1..3`, `_r_1..3`). There is no `MoveCylinders` helper for the
  S060, only for the S282.

## State: what is proven and what is not

**Proven in game (20 Oct):**
- 0-4-0 converts, restores and **rides well**. This settled the open question:
  the bogie pivots can be left where DV put them, with an axle offset from its
  pivot. Everything since relies on that.
- 0-8-0 converts cleanly. Probe confirms `poweredAxles.NUM = 4`, two axles on
  `Axle_R`, 4.648 m wheelbase. No exceptions in the log.
- No extra wheelslip on the 0-4-0 light engine at full regulator and full cutoff,
  **which is correct**: a tank engine has all its weight on coupled axles, so
  total adhesion is the same on two axles as on three. Only per-axle load changes.

**Not verified:**
- **Nothing has been driven.** The single most important unknown is **rod sync**:
  the 0-8-0's extension rod and fourth wheel are not in the native clips and copy
  an animated part's pose in `LateUpdate` (`S060FollowPose`). If that stutters,
  lags or drifts, it blocks every later arrangement.
- The driver-size slider (0.3.0) has not been tried at all.
- Whether the fourth driver clashes with the rear coupler trigger box in practice.

**James's verdict on the look:** the 0-8-0 at stock size is cramped — four
1.527 m wheels at 1.549 m centres leave 22 mm between tyres. He asked for either
smaller wheels or the cylinders moved forward. 0.3.0 does the first.

## Next jobs, in the order they make sense

1. **Cylinder shift.** Scaling drags the cylinders towards the car centre (~0.5 m
   at 0.80), and moving them back out independently is the obvious fix. The
   inherited `DriveRetarget.cs` does exactly this for the S282 — shifts the
   cylinder-side parts and stretches the rods spanning the gap, without touching
   the animation — but it is behind `#if B99_ALL_STANDARD` and written against
   the S282's hierarchy, so it needs adapting. **Ask James first whether the
   shrunk-together look actually bothers him**; he may not want this built.
2. **0-6-2 trailing truck.** 1.31 m of clear space behind driver 1, and the
   splitter can move the rear subframe. It needs the S282's 0.773 m
   `s282_wheels_front` mesh, which means packaging the runtime AssetStudio
   extractor the way the S282 mod does (16 MB, in its mod folder). Do not commit
   extracted game meshes to the public repo — the upstream author extracts on the
   user's machine for that reason.
3. **0-10-0**, once rescaling is judged good: five drivers at ~1.2 m need 5.20 m
   of the 5.74 m band that size gives.
4. **0-2-2-0 Franklin duplex**, the joke James wants: the fork already carries the
   S282's duplex sim (`Sim/Duplex*.cs`, `Patches/DuplexSimPatches.cs`,
   `Patches/AddTwoCylinders.cs`) and the Franklin cylinder meshes in `models/` and
   the `s282_drivetrain` bundle — but the csproj currently excludes those sim
   files from the build.
5. **P4 shell**: comms radio, saved arrangements, spawn pool, diagnostics, tests,
   all portable from the S282 mod.

## Conventions worth keeping

- Cite `path:line` or a probe report line for any claim; mark anything unverified
  as unverified.
- Every build gets a hash receipt; every install gets a backup of what it replaced
  and a hash check afterwards.
- The validators (`Validate.Stage1/2.ps1`) need PowerShell 7, which is not
  installed on this machine. They fail identically in the S282 repo — inherited,
  not a regression.
