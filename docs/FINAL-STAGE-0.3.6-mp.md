# Final 0.3.6-mp stage â€” 15 November 2026

James approved the rendered pipe sweeps and requested the full articulation
checks, a local commit and staging. He also requested removing the floating
middle brake gear from the 0-4-0. Installation is not authorized by this request.

## Changes

The approved Shay and Meyer pipe geometry is documented in
[PIPE-SWEEPS-0.3.6-mp.md](PIPE-SWEEPS-0.3.6-mp.md). The final stage retains those
exact pipe source files. Only the 0-4-0 presentation code was added afterward.

The existing 0-4-0 conversion already disables the middle LMS calipers at both
LODs. The native `s060_brake_shoes` mesh is separate and retained all six shoes,
which left the middle pair visible beside the missing axle. The on-disk native
mesh is CPU readable and has six disconnected shoe components. The middle two
occupy Z 0.65197 to 0.89178 m, inside LMS's middle brake region 0.6 to 1.31 m.

S060FourCoupled now uses an owned copy of that native mesh and removes only
the middle shoe triangles. The four outer shoes retain their original vertex
positions, normals, UVs and triangle indices. The original renderer, material
and native brake-heat binding remain. Cleanup restores the exact original
six-shoe shared mesh and releases the copy. Existing caliper/wheel restoration
remains. No brake simulation, native asset, bundle or network schema changes.

## Validation provenance

The new native brake fixture first reproduces the visible middle shoes with
the installed DLL (14 baseline checks). The corrected DLL passes 22 checks
through production Apply/Undo presentation paths, including both caliper LOD
groups, retained outer brakes, exact mesh data and repeated cleanup. The fixture
deliberately stops before native axle/physics setup; those simulation boundaries
are not represented as live gameplay. Its first launch required correcting
reflection access to an inherited private setter; production did not change.

One integrated production build followed the brake repair, with zero warnings
and errors. Runtime SHA256:
`E3959B44D4324AFEBEFCC8C2BA23C0A8FD534043952861EA3466D68C5AB6E252`.
The earlier visual-preview build has separate provenance and is superseded.

Managed multiplayer/ports: 300 checks. Conversion transactions: 45 checks.
The exact final DLL passes 114 native traction/authority checks. The fixture
reuse check was corrected to identify its unique bootstrap filename and exact
source hash after consolidation moved its directory. The historical receipt
and current bootstrap hashes match; no player or production rebuild was needed.

The first full Meyer run completed its 27-pose geometry and cleanup checks, then
failed in the unchanged Dockside post-check. That older fixture attempted to
read the two baked sleeve MeshFilters after production intentionally disposed
their per-clone objects. The installed DLL reproduces the same failure. The
fixture now records those exact two objects before binding and requires their
removal plus survival of their shared asset meshes; all other original filters
must still restore their original meshes. The full Meyer suite is rerun with
that corrected ownership check. No production asset, tolerance or DLL changed.

Full articulation and affected native regression results are recorded in
`build/release-0.3.6-mp-final/validation.json`. Do not replace an incomplete or
failed report with the earlier preview-only checks. Existing engine/oiling and
unchanged-bundle evidence remains limited to its documented source/asset scope.

The specialised multiplayer packager stages the final DLLs with the verified
0.3.5 bundles. Its per-entry hash checks cover all 14 ZIP files. Native UMM
installation/update/migration checks run against this ZIP in a scratch game
directory, with explicit PackageDirectory support to preserve the previous
release's proof. No installed mod or saved game is modified.

## Delivery

ZIP: `build/release-0.3.6-mp-final/package/RearrangedS060-0.3.6-mp-UMM.zip`.
Keep it separate from previous packages with the same version label. All peers
must use the same complete package; its existing build check distinguishes the
changed DLLs. The public changelog includes both pipe sweeps and the 0-4-0 shoe
repair. Default multiplayer build/package paths now select this final stage.

No installation, game launch, push or publication. James retains gameplay and
live peer acceptance. The installed runtime hash remains
`654B476F40C99C45A4C3221BC9A09454A371CE85F95CD9EA5C180EBD9A6FD4D3`.

## Completed results

- Full conventional/Shay checks: 3,208.
- Full Meyer sweep: 8,859 across 27 articulation poses; Dockside postcheck passed.
- 0-4-0 brake removal/restoration: 22; old floating shoes reproduced in 14 baseline checks.
- 0-4-0 equipment and rod/wheel motion at three scales: 2,118.
- Native cleanup: 145; exact-DLL native traction/authority: 114.
- Managed multiplayer/ports: 300; conversion transactions: 45.
- Exact staged ZIP native UMM: 66.

14,877 recorded candidate assertions passed. Baselines are excluded. The complete
Meyer suite passed after the fixture correction with the same production DLL.
No assertion or geometry tolerance was disabled or widened. Approved pipe source
hashes match the rendered candidate. All three bundles match the verified seed.

Delivery copy: `_deliveries/staged-0.3.6-mp-final-20261115/` under the consolidated
source home. ZIP SHA256:
`6FA55A03422B12DC68FECD774F040CC210F5D188DF041C9B4C2CE951F6B02A2B`.
Source checkpoint and final artifact hashes are in the validation receipt.
