# Conventional rods and oil-cup support — 16 November 2026

James requested native rods on the regular arrangements, investigation of the
floating cup pair, and an oil-cup check of every arrangement. The resulting
0.3.6-mp update is installed after his separate authorization. No DV launch,
gameplay automation, asset-bundle rebuild, backup, push or publication occurred.

## Confirmed cause and repair

The 0-4-0, 0-8-0 and 0-10-0 coupling rods still used the primitive ring/web
builder. Their replacement now takes the terminal eyes, intermediate eyes and
webs from each live native S060 rod LOD. It fits those sections to the actual
two, four or five crank-pin stations. Native width, profile, normals, tangents,
UVs, submesh slots, renderers, live materials and animated parents are retained.
These are fitted native sections, not unmodified three-eye rods stretched over
the wrong number of axles. Stock/short 0-6-0 and 0-6-2 retain their original rods;
the main connecting rods remain native. Disposal restores the exact originals.

The native guide-cup providers are children of DrivingMechanism_L/R, but their
upper slidebars are part of the rigid body. Resizing/translating a mechanism
therefore moved its cup away from the supporting slidebar. The native baseline
reproduced this on short 0-6-0, 0-8-0 and 0-10-0. The main-rod/crosshead cups
were already seated in their native parts.

Each conventional guide cup now binds to a body-mounted marker at the original
body station. The seat is measured from the current native slidebar triangles,
including the left mirror, and uses the actual cup thread with 6 mm engagement.
Added big-end necks use the native coupling-eye height instead of the former
primitive boss height. Circuits, saved oil quantities, ports, lids and consumer
enrollment are unchanged. Clearing the mounts restores the original providers.
Owned-object cleanup uses DestroyImmediate only in EditMode; game cleanup keeps
the existing deferred Destroy behavior.

## All-arrangement support check

| Selection | Visible functional cups | Offline support result |
|---|---:|---|
| Stock 0-6-0 | 6 | Passed |
| Short-wheelbase 0-6-0 | 6 | Passed after guide repair |
| 0-4-0 | 4 | Passed |
| 0-8-0 | 8 | Passed after guide repair |
| 0-10-0 | 10 | Passed after guide repair |
| 0-6-2 | 6 | Passed |
| Meyer | 8 | Passed; no additional repair |
| Dockside | 8 | Passed; no additional repair |
| Shay | 6 | Passed; no additional repair |

Counts preserve the current two-per-driven-axle circuit selection and the
Shay's six native circuits. No extra decorative cups were added.

## Evidence and limits

One integrated production build produced core SHA256
`1FADD8473957BE5054177CA0820B9C0A8C74E6F27B42C48D51B1F8E5C1DCE792`
and bridge SHA256
`B6821A71DE44055362D70284E0E73AE6F21C6904DAB3D779EF21A7A748ED3C3D`,
with zero build warnings/errors. The bridge code/network schema did not change.
Its newly built binary travels with the same complete package on every peer.

- `build/oil-rods-20261116/baseline-visible`: original core 4379CFCE, native
  providers and cup meshes. Guide cups miss the upper rail datum by about
  230 mm at 0.8 scale and 339 mm on 0-10-0; these are datum differences, not
  claimed nearest-surface air gaps. Stock native thread engagement is retained.
- `build/oil-rods-20261116/conventional-supported`: 4,794 checks across all six
  conventional selections, 24 wheel poses per side, both rod LODs, actual cup
  threads, neck endpoints, drawn support and exact source restoration. Guide
  engagement is 6 mm within approximately 0.2 micrometres.
- `build/oil-rods-20261116/articulated-final`: 1,774 checks and 75 sampled
  poses, actual shipping Sites/NewMount/LateUpdate methods and reopened bundles.
  Meyer/Dockside use 12 crank poses at three yaw angles; Shay uses three yaw
  poses on its real bearing/sill geometry. Largest measured mounting support
  separation is 0.493 mm. No additional floating cup was found in these checks.
- `build/radio-0.3.6-mp/runtime-audits-20261116-020856`: 2,118 0-4-0 native
  air-equipment/motion checks and 61 native conversion/restoration checks.
- `build/radio-0.3.6-mp/runtime-audits-20261116-021218/conventional-rods`:
  519 production four/five-axle layout checks and 5,744 native rod-profile
  assertions. The latter tests 129 phases on each side at two fixed fixture
  scales, with two/four/five eyes. Native cap-centre offsets remain below
  0.087 mm; the source's small authoring offsets are retained, not rounded away.
- Existing managed suites: 628 native oil migration/mask/port checks,
  45 conversion-boundary checks across 20 injected scenarios, and 49 multiplayer
  core checks passed. Managed boundary doubles are not scene or transport proof.
- Exact ZIP readback verifies 14 payload files. Native UMM fresh install,
  update and legacy migration pass 66 checks in an isolated directory.

Fixture corrections were made without rebuilding unchanged production: enable
the exported OilCup_LOD0 objects hidden by the general-purpose reader; replace
the old primitive's 512-vertices-per-eye assumption with native cap centres;
test the actual 6 mm thread-engagement segment at fixed Shay seats; allow the
existing minimum 10 mm neck to embed in its two supports. These avoid mistaking
fixture assumptions for authoring defects. Final fixture logs retain the two
pre-existing unused-field/variable compiler warnings; no new compiler error or
EditMode Destroy error remains. Some native SDK serialization diagnostics are
also present in the baseline and do not establish game errors.

The conventional fixture exercises production mesh swaps and mounting methods
with exact native geometry. Its wheel/rod motion is driven explicitly; native
ECS initialization, real streaming and gameplay interaction are outside it.
The articulated fixtures sample donor animations and production mounting code,
not a live game session. Earlier brake and pipe evidence applies to unchanged
code/assets and is not claimed as a fresh full-game or fleet-clearance audit.

James owns the next moving/curving visual check, oil-can servicing, lids,
streaming, switching, save/reload and live multiplayer acceptance. See the
updated MULTIPLAYER_TESTING.txt.

## Package and installation

Canonical ZIP:
`build/release-0.3.6-mp-oil-rods-20261116/package/RearrangedS060-0.3.6-mp-UMM.zip`.
SHA256: `5C607B4268330E54061E96CC08F43903D63E5F443479C1A135A99B253563DF32`.
All three bundles retain their earlier exact hashes and Unity reopening proof;
no native game exports, QA renders, game DLLs or API assemblies are distributed.

James authorized installation if DV was closed. The guarded installer verified
the closed game, copied and checked all 14 payload files, and preserved settings
and unrelated mod manifests. Settings SHA256 remains
`A89B908441E9E9984D0D08D8B7C5B516475D22BAE344ED7E21CE770631E78BD9`.
Receipt: `build/release-0.3.6-mp-oil-rods-20261116/nexus/install-receipt-latest.json`.
Existing built ZIPs are the reinstall sources; no separate backup was created.
