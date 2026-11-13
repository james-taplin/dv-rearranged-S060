# Shay, Dockside and Meyer polish — 13 November 2026

James requested these repairs on 0.3.2 and **commit only**. The source is
`B:/GitHub repo/rearranged-s060-032-polish-20261113/source`, based on checkpoint
`006164ebcd52fe4127e26d642b75567b440be7b8`. Do not substitute the older canonical
working tree or the concurrent Documents 0.4 development. No installation,
publication or DV gameplay testing is authorised by this task.

## Meyer rear return — follow-up added to the same commit

James's pink-line reference supersedes the old descent behind the bunker.
The 190 mm pipe now descends at X 1.47 / Z -3.80 m beside the bunker/cab rear,
passing through the outer ends of the two existing native steps as requested
brackets. The pipe clears the cab sheet; the original rear collector/manifold
stays in place. Its existing flexible connector turns upward into the fixed
downpipe at (1.47, 1.65, -3.80), so the lower return has no abrupt reverse bend.
The first roof saddle moves to Z -2.95, on the straight course after the new
upper bend. The rest of the roof/smokebox route is preserved.

The native fixture now requires actual contact with both measured step castings
and permits only those two small casting envelopes on the fixed downpipe/lower
connector. Cab sheet, bunker, frame, running gear and other pipe contact remain
forbidden. The previous coarse cab-entry box extended behind the native rear
entrance handrail and included the authorised step brackets. Its tested rear
edge now follows that handrail at Z -3.43; production cab geometry, doors,
colliders and steps are not changed. The old requirement for the roof course
to extend behind Z -3.9 is superseded by the earlier descent and the two direct
step-contact checks.

The first candidate's manifold connection failed the existing minimum bend
radius check (66.26 mm). It is retained as failed evidence at
`build/radio-0.3.2/runtime-audits-20261113-232441`; it is not accepted. Moving
the fixed/flexible join into the downpipe and aligning its tangent upward
corrects that authoring defect using the existing bend owner.

Current amended runtime:
`build/geared-polish-0.3.2/RearrangedS060-rear-drop-final.dll`, SHA256
`04227D9CC02CF36A886DFF2E24B00AE1BCB1F9474B72D2433453C0EEA6225EB9`.
Build: zero warnings/errors. The affected native Meyer suite and detailed
rear-drop preview are under
`build/radio-0.3.2/runtime-audits-20261113-233543`.
The exact final-DLL suite passes 8,859 checks across all 27 articulation poses,
including both requested step contacts, pipe connections, material changes,
native mesh restoration and cleanup. There are no unintended pipe/body
contacts. Measured minimum cab-floor clearance is 57.71 mm (45 mm requirement),
and minimum flexible bend radius is 156.21 mm (142.5 mm requirement).
The preceding `233037` run reached the end of the articulation sweep but its
floor-margin check still classified the permitted lower step as cab floor.
The fixture now omits only triangles wholly inside those measured step-casting
envelopes from the floor measurement; it retains the 45 mm floor requirement.
No production rebuild followed this fixture correction.
The sole post-build production-source edit corrects the comment describing
the body's higher, outside-the-sheet union; executable statements are unchanged.
The earlier Shay/Dockside reports below retain their exact earlier-DLL
provenance: their authoring/runtime code is unchanged by this follow-up.
This pipe-only correction reuses the previously verified bundles. Nothing was
installed or launched. Gameplay appearance and acceptance remain James's tests.

## Earlier Shay/Dockside repairs retained

Shay's short quadratic pipe corners are replaced by the existing Meyer's
circular tangent sweep, with a 380 mm requested bend radius constrained by
adjacent segment lengths. The hollow walls, sockets, flanges and existing pipe
port datums remain. One redundant control point before the smokebox is removed
to allow the broad front elbow. The circular centre sampler is shared without
changing Meyer's previous sweep output. Exhaust collars/stays move from Z 1.48/2.46 m
to 1.02/2.73 m and seat on the actual swept pipe, including its tangent.

The native S060 information plate anchors are at X +/-1.30, Y 2.065, Z 1.871 m.
The native TrainCarPlate background is 10 x 320 x 1300 mm, so its longitudinal
edges are Z 1.221 and 2.521 m. The previous stays passed directly through it.
The native fixture now includes this separately generated plate; it was absent
from the body-only test. Both sides must clear the actual plate surface.

Dockside's native01 wheels included the original return-crank web and its
detached pin. Reuse the existing Meyer connected-island removal and lifecycle
for both Dockside frames. Keep the wheel profiles, journal, UVs and normals
exact; restore the original shared meshes when the owner is released.

The missing Franklin casing was real, despite the older bounds-only audit:
five installed custom meshes (0, 13, 15, 16 and 17) had all-zero vertex positions.
Mesh 13 is the gearbox. Their saved bounds still looked plausible. The original
S282 custom source bundle retains intact geometry. The former builder copied
non-readable streamed bundle meshes with Object.Instantiate, rather than
reauthoring their geometry. The new local extractor reads the original stream
and writes a private JSON input; the builder creates fresh readable author-owned
meshes and replaces any stale mesh assets. All 17 exported Franklin meshes now
match the original source positions exactly. No native mesh data is bundled.
The restored castings and shafts use an owned gunmetal material, avoiding a
whole native body atlas on the imported parts. The animation, gearing and joint
datums remain the original ones. No new runtime hook or dependency is needed.

Rebuild with `Build.Runtime.ps1` and `Dockside/Run.Authoring.ps1`; the latter
extracts the original geometry before exporting and reopening the CCL bundle.
It runs Unity in batch mode so authoring failures terminate instead of leaving
an editor open. Run the affected existing native suites with `Test.Runtime.ps1
-SkipBuild -RuntimePath <candidate> -DocksideBundle <new bundle> -Only
conventional,dockside-sleeves,meyer-inward-body,meyer-inward-pipes`.
`GearedPolishNativeAudit.RunBatch` uses the existing runtime, native-preview,
Dockside-bundle environment variables and `S060_GEARED_POLISH_OUTPUT` to check
the restored casing against the authored motion and exact wheel restoration.

Evidence is retained under `build/geared-polish-0.3.2` and
`build/dockside/authoring-20261113-230440`. The exported Dockside bundle SHA256 is
`1CFEBAC631EF1A1CD85F3674116767D76846D58C228DB9027FD3E3FCC7890819`.
The preceding geared-only runtime SHA256 is
`34C031CB4AA37F8E616FF33384D1AFB6D4994CFE14475601DCF4F48CDDE8F820`
(`build/geared-polish-0.3.2/RearrangedS060-final.dll`). The build has zero
warnings and zero errors.

Authoring checks pass: 23,883 detailed-mechanism assertions; 24,925 pose and
8,923 seek/isolation assertions; 802 focused checks across 33 phases per unit,
all four gearboxes and exact stock-wheel restoration. The minimum measured
crank/gearbox face separation is 0.9869 mm. An independent UnityPy reopening
comparison records the five collapsed baseline meshes and the exact original
positions in all 17 repaired meshes (`exact-franklin-geometry.json`).

Geared-only-DLL conventional/Shay fixture: 3,072 checks at
`build/radio-0.3.2/runtime-audits-20261113-231314`, including actual compressor
exhaust-tee continuity, board clearance, native geometry, motion and restoration.
The geared-only 802-check Dockside report and reviewed previews are under
`build/geared-polish-0.3.2/final-reviewed`; that report records that revision's hashes.
The preceding CF1D9AEC candidate passed four suites / 16,703 checks at
`build/radio-0.3.2/runtime-audits-20261113-230826`: conventional 3,071,
Dockside sleeves 4,602, Meyer body 119 and Meyer pipe/frame/drawgear 8,911.
Only the Shay front-course control point changed afterward. The affected full
conventional/Shay suite and focused Dockside sweep were rerun; the other three
reports retain their preceding-DLL provenance. Do not report those older runs
as direct final-DLL tests. The Dockside bundle was exported once successfully;
the earlier compiler-path launches did not reach authoring. Initial runtime
builds were superseded by the concrete front-course corrections; no rebuild
was made just for fixture or documentation changes.

Unity's old Library junction was on C: while Temp was recreated on B:, causing
compiler-copy failures before fixture execution. The existing cache was copied
to a physical Library directory beside Temp on B:; its prior C: junction/cache
is retained as `Library-before-geared`. These failed launches are fixture
failures, not production build failures. No production asset rebuild was made
for those fixture-path repairs.

Gameplay remains awaiting James after a separately authorised installation:
inspect the front Shay bends and both information-board edges; inspect all four
Dockside hub gearboxes and shaft connections through wheel rotation; switch
back to stock and confirm the original mechanism returns. Offline evidence
establishes geometry, export and restoration, not game acceptance.
