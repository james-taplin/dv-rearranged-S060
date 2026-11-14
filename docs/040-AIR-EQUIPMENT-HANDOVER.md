# 0-4-0 air equipment — 13 November 2026

James requested a source commit only: fill the removed middle-driver pockets
with twin compressed-air tanks and industrial fittings, embedded in the rigid
model and clear of animated parts. He then requested removal of the surplus
fixed gear casting and tanks 1.5 times the initial size.

The 0-4-0 now has one longitudinal reservoir on each side. At standard layout
scale each vessel is 1.62 m long and 0.57 m in diameter. Dished ends, raised end
seams, two straps, frame hangers, saddles, hex unions, air lines, isolation
manifolds and low-point drain cocks share one combined mesh and the native
body's iron finish. The larger vessels move inward to x ±0.55 m; their straps
stay behind the native coupling-rod plane. Smaller optional layouts fit smaller,
lower equipment beneath their reach rod; hangers still reach the fixed frame.

Corrected decision: remove only the surplus FIXED body-mesh casting using
`S060FixedValveGear` and an owned, reversible copy of the body surface. James
clarified that the working animated reverser must remain visible. Do not hide
`s060_mech_03` or its LOD: the initially broader removal was corrected before
commit. Native animation transforms, renderers and connected rods now remain
untouched. Leaving 0-4-0 restores the exact native body mesh and releases owned
meshes. The existing mesh-retirement helper is included as a source dependency
of this change. No additional DLL, asset bundle, simulation, physics or save fields.

Offline validation uses the current working-tree runtime and the existing
exact-export conventional geometry/triangle-contact fixture. Local runner and
fixture are preserved with final evidence under
`build/040-air-host-20261113-234849/`. The final DLL SHA256 is
`251D92E3BB9239D9C58E5074941BFE26AC1B7D89BEC01608356DE616C4185FEC`.
The Release build has zero warnings/errors. The final targeted presentation and
restoration pass has 30 checks across all three layout scales, including the
visible reverser, removed fixed casting, material refresh and exact restoration.
The final detail render was inspected: the working reverser is visible again.

Retained geometry evidence: `build/040-air-host-20261113-234502/validation.json`
passes 2,118 checks for DLL
`8B47C0EF7F73980DA10F656636D24B454DFD3B65B042EBDEDFE9B04904388B38`.
Its tank geometry and transforms match the final source. That sweep tested
the reverser's mesh surfaces even while its renderer was hidden. Restoring
visibility changes no clearance geometry, so only the affected presentation
and restoration checks were rerun. This earlier report is geometry provenance,
not an exact-final-DLL full sweep or acceptance of its hidden reverser.

Fixture provenance: the legacy hierarchy JSON omits activation flags. Direct
inspection of installed `resources.assets` confirms the left reach rod and
two `_UNUSED_LUBRICATOR` objects are inactive; the local fixture now preserves
that state. Late-loaded production MonoBehaviour messages are not automatically
driven by this EditMode fixture: material refresh and destruction callbacks are
called explicitly. Earlier failed reports are retained. No production rebuild
was made for fixture-only corrections.

The 15-degree wheel/coupling-rod sweep uses layout scales 1.0, 0.55 and 1.15.
Other native mechanism meshes are tested in their exported pose; this is an
offline geometry and restoration check, not a complete animated gameplay run.
James owns in-game visual/motion acceptance. No installation, game launch,
push, publication or release-package regeneration was performed. Existing
unrelated working-tree changes are outside this commit.
