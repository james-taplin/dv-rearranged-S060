# Meyer repairs on preserved 0.3.1 — 13 November 2026

James authorised all three reported repairs, offline build/validation and staging
only. Keep version 0.3.1 and the original metadata. Do not install, launch DV,
or modify the installed Car Changer prototype or Documents 0.4 workspace.

## Source and package identity

Isolated source: `B:/GitHub repo/rearranged-s060-031-repairs-20261113/source`.
Base is preserved commit `c982c8824ad74ca21f503abeb525317b33ce6f1c`, exported
without switching the canonical checkout's branch or copying its later changes.
The managed worktree service did not return; the isolated export is intentional.

Original 0.3.1 DLL: `7A9AED643B80A47D58A75CEFB1F4F62D64FC207D06D47878174176942389F1B7`.
Original ZIP: `9B5FF35FC4870C1860E1B9607584639C14299731DE4A2B02E005625706B81F2F`.
Info.json must retain SHA256 `61F1A136148BAF8E18B3DE7339EC0D5943F7B13F5597435989D16645DA943DD4`.
All three donor/model bundles are reused unchanged; no Car Changer dependency.

## Repairs

- `S060MeyerSupport`: capture and update the existing native
  `ChainCouplerInteraction.originalLocalPosition` in its actual parent space;
  restore it with the stock visual transforms. No enable hook or extra DLL.
- `S060MeyerBodywork`: extend the plate and two fixed bearers to Z -3.92 so they
  enter the native cab frame at about Z -3.98. Extend the common physical,
  walkable and item plate collider to the same datum. Moving engine joints,
  supports and frames keep their native ownership.
- `S060MeyerExhaustPipe`: replace the roof course and roof saddles with a low
  fixed return, native running-plate hangers, a local dip beneath the moving HP
  admission bend and a short smokebox rise. Retain the 190 mm return diameter,
  original LP collector, rear flexible joint, smokebox inlet and steam circuit.
  The first fixed segment follows the proven rear joint tangent.

## Evidence and limits

The original ZIP payload hashes and all sixteen stored report hashes were
verified during the read-only investigation. Those historical reports cover
23,002 checks and do not imply gameplay acceptance or acceptance of this patch.

The expanded coupling fixture invokes DV's actual `Entry_Enabled` method on
the native interaction component with inactive native knob/IK dependencies.
Original 0.3.1 fails the relocated-hook assertion after enable; the initial
repair DLL passes 96 checks, including three enable entries per apply cycle,
nested/reparented chains, rotated car roots and exact stock restoration.
The full native FSM, player interaction and live visibility optimizer are not
exercised by that inactive-car fixture.

The new geometry contract checks actual surface joins to the native frame and
plate, and clips long pipe faces rather than requiring interior vertices.
The original release fails all five new join/low-route assertions. An initial
fresh geometry baseline hit the old 180-second runner limit. After conservative
fixture broad-phase improvements, its full 27-pose baseline passes 3,394 ordinary
checks while retaining the expected failing repair contract. The exact triangle
predicates and tolerances are unchanged; bounds use actual selected faces and
skip triangle pairs outside the complete padded target bounds. Existing exact
intersection controls run before the geometry checks.

Source-fitting mode in the existing pipe fixture permits geometry authoring
without rebuilding production for every placement. It is labelled
`sourceRoutePreview`; Build.Radio rejects these previews as release proof.
Final evidence must come from the actual final DLL with fitting mode unset.
The existing 27 poses cover both yaw directions, pitch/roll and heave, actual
native geometry, cab access, joints, pipe radius, livery and cleanup. New checks
also reject contact between the separate HP and LP pipes.

Fresh-project fixture setup required the existing calibration, oil-cup,
pipe-geometry and Dockside plan helpers. Test.Runtime now copies these, uses
batch mode to exit on compile failure, and permits a bounded per-audit timeout.
Build.Radio can validate an already built, source/hash-matched DLL with
`-SkipBuild`; normal build behaviour and all package gates are retained.

No Read/Write import settings, game assets, saves, gameplay profiles, installed
files or dependency versions are changed. Local QA meshes/textures stay private.
Final aggregate: `build/repair-0.3.1/validation.json`. Final DLL is
`4F09184300863A26018BE77317A452575A83789F0F7A3FDB344CC9793E212EDA`.
The sixteen native suites pass 23,393 checks against that exact DLL; source
previews are not used as final evidence. All 51 installed prototype files
match the starting snapshot. See the main handover for package hashes and
the separate source-checkpoint receipt.

## James-owned acceptance

After a separately authorised installation: inspect the cab/rear frame join
and low return from both sides; drive curves and points; inspect rear hook,
chain and hose after repeated distant teleports/visibility returns; operate
coupling/uncoupling and the angle cock; save/reload both coupled and uncoupled;
switch to stock and back and confirm original coupling position and geometry.
These are pending James's testing, not an app-approval blocker.
