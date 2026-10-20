# Rearranged S060

A Derail Valley mod that changes the [wheel arrangement](https://en.wikipedia.org/wiki/Wheel_arrangement)
of the **S060**, the B99 0-6-0 tank engine, at runtime through the comms radio —
the same idea as [Rearranged S282](https://www.nexusmods.com/derailvalley/mods/752),
applied to the little shunter.

## Status: 0-4-0 proven in game, 0-8-0 built and awaiting a test.

This repository is a fork of the Rearranged S282 B99 port, taken at `65c8e8a`.
The default build (`Stage=1`) is the **spike build**: a mod-manager window with
buttons that turn a stationary S060 into an **0-4-0** or an **0-8-0** and back. No comms radio, no
persistence, no spawn hooks, and the only Harmony patch is `Bogie.Axles`. The
inherited S282 geometry is compiled but never reached, so it is safe alongside the
real Rearranged S282.

The 0-4-0 converted, restored and rode well on 20 Oct 2026, which settled the
open question: the bogie pivots can be left where DV put them. The 0-8-0 has not
been driven yet. `docs/TESTING.md` is the checklist;
`docs/FEASIBILITY.md` has the measurements everything is built on.

### What the S060 is, measured

Three coupled axles, 1.516 m driver meshes at 1.55 m spacing, 3.10 m rigid
wheelbase, 8.46 m body, no leading or trailing truck and no truck wheel mesh of
its own. Walschaerts gear, named and laid out like the S282's, sitting flat under
`LocoS060_Body/DrivingMechanism_L|_R`.

Two prerequisites came free: LocoMeshSplitter 1.1.9 already ships S060 splitters,
and S060 and S282 drivers are the same 1.516 m mesh, so added drivers need no
rescaling and a pony truck can borrow the S282's 0.773 m `s282_wheels_front`
through the extraction path in `MeshFinder`.

### Plan

- **P0** — done. A read-only probe mod dumped the live S060's bogies, axles,
  clearances, splitter API, sim ports and animators; section 5 of the feasibility
  doc is now answered from it.
- **P1** — built. Drops the middle driver (not driver 1 — `Axle_R` holds only one
  axle), rebuilds `Bogie.Axles`, re-registers two powered axles, writes
  `poweredAxles.NUM`, moves the adhesion weight onto the two drivers and hides
  that axle's caliper. Bogie pivots left as DV set them.
- **P2** — 0-8-0, built: a fourth driver behind driver 1 at the stock 1.549 m
  spacing, cloned from driver 2's wheel with the coupling rod extended by a second
  shifted copy. It needs no new meshes at all, which is why it overtook the
  trailing truck.
- **P2.5** — 0-6-2: trailing truck behind driver 1. Needs the S282's 0.773 m pony
  wheel, so it needs the runtime extractor packaged, which is the only reason it
  is not done first.
- **P3** — forward wheels and bigger frames: 2-6-0 (needs the split cylinder block
  moved), 0-8-0 by re-centring the driver group, then driver rescaling, which is
  what unlocks 0-10-0.
- **P3.5** — the 0-2-2-0 Franklin duplex meme, using the duplex sim and Franklin
  cylinder meshes the fork already inherits.
- **P4** — shell: comms radio, saved arrangements, spawn pool, diagnostics,
  tests, ported from the S282 mod.

What fits, measured against the car's 7 colliders: 0-4-0 and 0-6-2 are cheap, and
2-6-0 means moving the cylinders. A full-size wheel centre can sit between Z
−2.758 and +2.758, so **0-8-0 fits at stock spacing only if the whole driver group
is re-centred** (about ±2.32 and ±0.77); bolting a fourth driver onto the front
while the others stay put does not fit. Rescaling the drivers, which the inherited
S282 code already does, widens that band and is what makes 0-10-0 and the 0-2-2-0
duplex plausible. 4-6-0 remains out.

Physics axles split 2 + 1 across `Axle_F` / `Axle_R`, with the single axle at the
cab end, and they are named plainly `[axle]` rather than the S282's
`[axle] drive N` — so the inherited lookups do not transfer.

## Building

```powershell
powershell -ExecutionPolicy Bypass -File ".\Build.Stage1.ps1"
```

Builds the inactive scaffold to `build\stage1\` with a hash receipt and installs
nothing. `-GamePath` overrides the game location; `Build.Stage2.ps1` builds the
stage with the radio enabled, which is **not** retargeted to the S060 yet.
`Validate.Stage1.ps1` / `Validate.Stage2.ps1` are the offline checks that
survived the fork; the S282's arrangement-specific validators were dropped and
live in the S282 repo. Both validators need PowerShell 7 — under Windows
PowerShell 5.1 their `Add-Type` call cannot compile the sources' expression-bodied
members and they fail at `WheelArrangement.cs:65`. That is inherited, not a fork
regression: the S282 repo fails the same way at the same line.

Visual Studio works too: open `RearrangedS060.sln` and build.

## What was pruned from the fork

The S282's release paperwork, Nexus packaging scripts, test evidence and its 18
arrangement-specific validators. All of it is in this repository's history and in
the S282 repo, which is wired up as the `s282` remote (`s282-upstream` points at
pjschinske's original) so fixes can be cherry-picked across.

## Repackaging custom meshes

Unchanged from upstream, and still how new parts get in. You need Autodesk
Inventor, Blender, Unity 2019.4.40f1 and Visual Studio.

1. Model the part in Inventor and export an OBJ (units: meter, surface deviation
   36%, normal deviation 28, max edge length 100%, aspect ratio 21.5).
2. In Blender, import the OBJ, UV-map it (Edit Mode, `a`, `u` -> Smart UV
   Project, island margin ~0.03, then tidy it in the UV editor) and export a new
   OBJ.
3. Copy it into `s282_drivetrain\ExportedProject\Assets\Mesh`.
4. Open the `s282_drivetrain` Unity project. An updated OBJ loads in by itself; a
   new part has to be added.
5. Drag updated prefabs from Hierarchy into the Project tab's `Prefabs` folder.
6. Right-click in the Project area -> Build AssetBundles.
7. Build the mod; it fetches the new AssetBundles.

## Licence and credit

MIT, Copyright (c) 2023 Peter Schinske (SatiricRug) for Rearranged S282 and
everything this fork inherits from it. See [LICENSE](LICENSE).
