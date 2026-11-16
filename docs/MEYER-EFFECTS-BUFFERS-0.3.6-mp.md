# Meyer wheel-slip effects and coupled buffers — 16 November 2026

James reported misplaced Meyer wheel-slip effects and a rear buffer gap
specifically when a following carriage is coupled. This repair is built,
packaged and staged only. Version remains 0.3.6-mp. Installation requires
separate authorization; James owns Derail Valley and live peer testing.

## Causes and smallest native corrections

The installed core is 1FADD8473957BE5054177CA0820B9C0A8C74E6F27B42C48D51B1F8E5C1DCE792.
Its effects binder measures body-side native spark offsets against original
physics axle transforms. Those transforms use rail height and move when Meyer
support pivots relocate, whereas the replacement axle origins use wheel-centre
height. The original visual wheel centres remain on the body. Measuring from
them in fixed car axes removes both the height error and the rear longitudinal
error without following the native wheel's animated rotation. Existing radius
scaling, native particle assets, per-group slip state and audio routing remain.
The shared articulated binder also supplies Dockside; its scaling and effects
restoration are covered by the same bounded fixture.

Coordinate provenance is build/qa/meyer-running-gear-native.json: the stock
rear physics axle is at Y 0, Z -1.4, its original right visual wheel centre
at Y 0.7121296, Z -1.4, and the rear spark anchors at Y 0, Z -1.398.
MeyerDraftBuilder places replacement axle origins at the visual wheel centres.
The export records hierarchy and component names; the native wheelTransform
mapping follows the established S060Parts contract, rather than a fresh
serialized-reference extraction in this task.

The physical rear coupling anchor had also received the visual hook-pocket
correction of -0.295 m. Stock rear anchor Z is -4.57 and stock buffer rig Z is
-4.3517475: their outward offset is 0.2182525 m. The installed Meyer anchor
Z -8.565 and rig Z -8.0517475 instead give 0.5132525 m. Native
Coupler.CreateRigidJoint uses the physical anchor with a 0.1 m inset on each
car, so this mismatch changes coupled vehicle spacing. The previous native
compression fixture imposed rig separations and missed that constraint.

S060MeyerSupport now places the physical anchor at RearCouplerZ (-8.27 m).
The visual hook and hose retain their complete previous movement, including
the -0.295 m correction. The fitted buffer rig, beam, stems, lamps, meshes and
native scanner/compression controller are unchanged. Physical inter-coupler
and logical length follow the corrected anchor, with stock values restored on
cleanup. No joint patch, custom buffer solver, dependency or asset rebuild.

## Evidence and limits

One integrated production build passed with zero warnings/errors:

- Core: 6A7732FD200E810F80DB9DAF60C337FE9F9087B7307F77572B14ABEB03E9DD7E.
- Bridge: 2BB2DADF4E9E30D1557E5DA58BA4C0182DB70A8061315542411578134991FB6C.
- Build inputs: build/release-0.3.6-mp-meyer-effects-buffers-20261116/runtime/build-receipt.json.

The installed core fails the added spark-contact assertion in
build/independent-slip-0.3.6-mp/native-20261116-232143-214/result.json.
The candidate passes 162 native checks in native-20261116-232301-620, using
the retained private Unity player with verified unchanged bootstrap. The
fixture exercises relocated/yawed supports, inward front-unit rotation,
animated original wheels, car rotation, radius scaling, native ECS authority,
per-group effects and restoration. Its poses are explicit fixture inputs;
actual rail dynamics, weather and audible playback are outside this proof.

The installed core fails the physical buffer offset and coupled-spacing
comparisons in build/radio-0.3.6-mp/runtime-audits-20261116-232237/meyer-rear-coupling.
Maximum stock-reference buffer-anchor discrepancy is 0.2632599 m after native
compression. This is not an asserted measurement of the gameplay air gap.
The final candidate passes 280 checks in runtime-audits-20261116-232551,
including actual native joint creation/readback at imposed 0.2/0.3 m joint
target separations, stock-relative buffer-anchor geometry, opposing-buffer
compression at multiple separations/angles, repeated apply, stream-enable
hook placement and exact hierarchy restoration. Maximum coupled-reference
error is 0.0652 mm, within the existing 0.1 mm tolerance. Native loose-chain
slack remains permitted. Chain adaptation, spring settling, rail dynamics and
actual carriage head-surface contact are not claimed by that inactive fixture.
The existing native lamp/stem/Buffer04 geometry fixture also passes 7,596
checks, including LOD restoration and sampled compression/yaw.

Managed checks pass: 224 geometry/native-length/pipe, 45 conversion-boundary
checks across 20 injected scenarios, and 49 multiplayer-core checks. The
managed boundaries use their documented doubles, not live transport.

An additional broad CleanupNativeAudit run fails on its synthetic Triangle
rod passed to the newer native-rod fitter. The exact installed DLL reproduces
the same IndexOutOfRangeException. Candidate run: runtime-audits-20261116-232324;
installed baseline: runtime-audits-20261116-232451. This fixture mismatch was
not repaired by changing production or expanding this task. Affected coupling
and effects cleanup checks above pass; no fresh broad cleanup pass is claimed.

The first sandboxed Editor launch could not read its machine licence; the
same offline fixture runs outside that sandbox. The coupling fixture's missing
Rigidbody detection was corrected using Unity's null comparison before the
baseline. Those are fixture/environment corrections, not production rebuilds.

## Package and handoff

ZIP: build/release-0.3.6-mp-meyer-effects-buffers-20261116/package/RearrangedS060-0.3.6-mp-UMM.zip.
SHA256: 2FBEFC8AF3C06B310F7FB2F63303CCDB3FFE17D88D50870E5BBBB00861B141D1.
All 14 archive entries were read back against staging. All three bundles are
unchanged and retain their prior exact-hash reopening evidence.

Native UMM fresh/update/legacy-migration checks pass 66 assertions in
build/meyer-fix-umm-20261116/umm-proof-20261116-232704/native-umm-validation.json.
The fixture uses its expected short package layout and the explicit preserved
0.3.0 three-mod migration ZIP. Earlier receipt-layout/default-migration-input
failures are retained; they did not change or rebuild the candidate ZIP.

Installed files remain unchanged. Existing built ZIPs are reinstall sources;
no backup, game launch, push or publication. Updated MULTIPLAYER_TESTING.txt
asks James to check sparks on both groups/directions/curves, rear coupling and
tightening under compression, native loose slack, uncoupling, coupled save/reload
and live peers using the identical complete repaired package.
