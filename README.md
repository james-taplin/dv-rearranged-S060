# Rearranged S060

A Derail Valley mod that changes the [wheel arrangement](https://en.wikipedia.org/wiki/Wheel_arrangement)
of the **S060**, the B99 0-6-0 tank engine, at runtime through the comms radio —
the same idea as [Rearranged S282](https://www.nexusmods.com/derailvalley/mods/752),
applied to the little shunter.

## Status: stage 0 scaffold. It does nothing yet.

This repository is a fork of the Rearranged S282 B99 port, taken at `65c8e8a`.
The machinery is inherited and working; **the geometry is still the S282's** and
has not been retargeted. The default build (`Stage=1`) applies no Harmony patches
at all and shows a label in the mod manager saying so, which makes it safe to
install alongside the real Rearranged S282 — but pointless to, until P1 lands.

Do not publish or install this expecting function. See `docs/FEASIBILITY.md` for
what was measured and what is still unverified.

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
- **P1** — 0-4-0: drop driver 2 or 3 (not driver 1 — `Axle_R` holds only one
  axle), swap to the two-axle coupling rod, update `poweredAxles.NUM` and the
  `PoweredWheelsManager` list, hide that axle's brake caliper.
- **P2** — 0-6-2: trailing truck behind driver 1, where there is 1.31 m of clear
  low space. The cheapest way to add wheels.
- **P3** — forward wheels: 2-6-0 (needs the split cylinder block moved) and a
  reduced-spacing 0-8-0 if it still looks right.
- **P4** — shell: comms radio, saved arrangements, spawn pool, diagnostics,
  tests, ported from the S282 mod.

What fits, measured against the car's 7 colliders: 0-4-0 and 0-6-2 are cheap,
2-6-0 means moving the cylinders, and **0-8-0 does not fit at stock spacing** — a
fourth driver would centre at Z +3.25 and overlap the front coupler box. 4-6-0 and
0-10-0 need shrunken drivers at best. The S282's duplexes, Mallets and triplexes
are not targets.

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
