# 0.3.3 integration — staged only

James requested a008bee, 08c8500 and 5c12c1d combined into 0.3.3. During the
build he rescinded installation and instructed **commit and stage**. No game
files are to be changed. James performs Derail Valley visual, driving and
listening acceptance; no gameplay was launched or automated.

## Source integration

The source export at `B:/GitHub repo/rearranged-s060-032-polish-20261113/source`
matched canonical commit `08c850028e977e2a60a8904da5f69f0b27096470` before editing
(text comparison ignores only CRLF). It already descends from the repaired
0.3.2 installed detail revision, including clean tender paint, native door
preservation, pinned drawgear, coupler streaming and material ownership fixes.

- `08c8500`: smoother Shay pipe elbows and information-board stays, original
  Franklin gearbox/shaft mesh restoration, Dockside native crank retirement,
  and James's marked Meyer rear-drop route through the native step brackets.
- `a008bee327b2f04c45d99c7f0840b6e47aed3014`: original Shay six-phase exhaust
  banks and eight running-loop rates, reversible native reader assignment,
  60 original WAVs, bundle authoring and native audio fixture.
- `5c12c1df30a4df843b8702b52c767de334871cee`: twin 0-4-0 reservoirs and fittings,
  reversible surplus fixed-gear removal, visible working animated reverser.

The latter two commits originate in the Documents 0.4 repository. Their exact
changes were applied with the `v0.4-work/` prefix removed; no unrelated 0.4
history or working-tree changes were imported. The fixed-gear helper added by
5c12c1d already exists identically on this line and was retained once.

The first integration compile exposed a prerequisite absent from that commit:
the existing `S060MeyerBodywork.Builder` was private on 0.3.2 but internal on
0.4. Its visibility is now internal so the committed reservoir fittings reuse
it. No builder geometry or paint logic changed. The corrected runtime builds
with zero warnings and zero errors; the failed first build is retained in
`build/integration-0.3.3/initial-build-missing-builder-access.log`.

Only the release and assembly version become 0.3.3. Other Info.json metadata,
dependencies, donor identities/versions, save/resource IDs, simulation,
powered-group calibration and oil ownership remain on the repaired 0.3 line.

## Validation and artifacts

The final machine-readable evidence is `build/integration-0.3.3/validation.json`.
The source checkpoint receipt is `build/integration-0.3.3/source-checkpoint.json`.
The package is `build/nexus-0.3.3-single/RearrangedS060-0.3.3-UMM.zip`.
All three are local-only. Runtime SHA256:
`4AB16332AF65553563C4F2DAC7C18FC05EC394D419A072363FE83B8191DAC9C4`.

The new Shay bundle is built in this checkout; all 60 original audio files
pass waveform/format checks and its retained R3 geometry passes 11,322 checks.
Shay bundle SHA256:
`73F7BEBE896DF06C5F5BBE13BB920E4B2A68AEF9485ADADB27896A63F8E6A7CE`.
The repaired Dockside bundle is reused from `build/dockside/authoring-20261113-230440`:
SHA256 `1CFEBAC631EF1A1CD85F3674116767D76846D58C228DB9027FD3E3FCC7890819`.
Its original source-vertex comparison and authoring motion evidence are retained
in GEARED-POLISH-0.3.2.md; runtime checks reopen that exact bundle against 0.3.3.
Unchanged Meyer donor and rear-wheel-fit evidence retain their earlier provenance.

`Test.Runtime.ps1` covers all 16 existing package-required suites on the exact
0.3.3 DLL (28,602 checks). The independent-drive fixture passes 101 checks.

`Test.033Presentation.ps1` runs the existing native Shay audio,
0-4-0 contact/restoration and Franklin motion fixtures. The 0-4-0 fixture is
preserved from the 5c12c1d handover's final local runner, including its native
inactive-node correction; the full three-scale wheel/rod sweep is selected.
The exact-DLL focused results are 6,037 Shay audio checks, 2,118 0-4-0 checks
and 802 Franklin checks. Dryope is absent from this release; its conditional
fixture branch is not exercised. The difference from the 0.4 audio fixture's
count is that omitted 36-check Dryope branch, not lost Shay coverage.
Actual rendered Shay pipe, Meyer rear-drop, 0-4-0 equipment and Franklin views
were inspected. The working 0-4-0 reverser remains visible.
`Test.IndependentSlip.ps1` reuses its hash-verified private player with the new
DLL. It is an offline ECS fixture, not the Derail Valley game.

Packaging uses Build.Radio, Package.Articulated and Package.Nexus with exact
runtime/bundle reports. Native UMM extraction, reopening of extracted bundles
and fake-game installer fixtures validate the staged archive without touching
the real installation. No production rebuild is needed for documentation or
fixture-only changes.

Final package checks passed: native UMM 57; extracted bundle reopening 39;
single-folder installer fixture 18; legacy installer fixture 24. All 11 ZIP
payload files match their receipt. ZIP SHA256:
`A45A7F6602F01FFC3061F842A70C4060F9DF81A428B521394367DE450DBAE6AA`.
All 57 files in the real installed 0.3.2 folder match the pre-task snapshot.
The installer output mentioning installation belongs only to the isolated
fake-game fixtures. No real Install.Nexus invocation was made.

## James-owned checks after a separately authorised installation

Inspect Shay elbows and information-board clearance; listen under light/heavy
power at crawl, 10 and 20 km/h, then coast, reverse, stream out/back and return
to stock. Inspect the Meyer rear return and its step brackets through curves.
Check the four Franklin gearboxes and shaft connections through wheel rotation.
Check 0-4-0 reservoirs, moving rods and visible reverser at the supported scales,
then return to stock. Offline geometry/hooks/restoration do not establish
audible quality, driving or gameplay acceptance.
