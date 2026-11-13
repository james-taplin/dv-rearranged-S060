# Visual repairs 0.3.2 — 13 November 2026

James supplied five gameplay screenshots from the installed repaired 0.3.1 and
requested a new 0.3.2. Scope: Meyer return routing, repeating textures on new
Shay parts, and loose curved gear on short-wheelbase, Dockside, 0-8-0 and 0-10-0.
Build and validation are offline; this revision is staged only. The installed
0.3.1 and Documents 0.4 work are not installation targets. James owns DV testing.

Source is isolated at B:/GitHub repo/rearranged-s060-032-polish-20261113/source,
exported from installed-source checkpoint 71188684f4c68fbba3ff1cd53bed25b0477e2250.
Installed baseline DLL: BBA33D317AE8A46BE7B0C36DAD4C8C9908C93630BEA17580F97CF7880EF5269A.
Info.json and assembly versions become 0.3.2; all other mod identity/dependency
metadata, save IDs and the three model bundle payloads remain unchanged.

## Meyer return: use the authorised fallback

James expressly allowed returning to the previous good smooth upper line if a
lower line could not fit cleanly behind the ladder. Three source-only fitting
trials avoided production rebuilds. They exposed contacts with the HP smokebox
feed and articulated bend. Exact native ladder islands sit at X 1.135..1.329,
farther inboard than the initial trial allowed. The lower route was rejected.

S060MeyerExhaustPipe.cs is restored byte-for-byte from preserved original 0.3.1
commit c982c8824ad74ca21f503abeb525317b33ce6f1c. Its low rear collector/union,
smooth upper course, measured roof straps, real smokebox flange and flexible
joint are retained. Provenance: build/polish-0.3.2/upper-return-provenance.json.
The existing native 27-pose fixture checks real pipe/body contacts and endpoint
continuity. Its route contract now requires the explicitly chosen upper line.
The pinned drawgear, open upper gap and teleport/coupler-cache repair remain.

## Shay finish binding

The bundle's authored meshes have metric UVs and flat Standard material slots.
The runtime supplies generated metal finishes. The previous updater tracked
only renderers already sharing the native body material and returned early
while that body's material reference stayed unchanged. It could not recover a
custom renderer replaced with a body-atlas material: the new bounded paint-
replacement fixture reproduces that failure against the installed DLL.

Resolve the existing finish slots before model activation. The existing Shay
material component retains each custom renderer's intended finish, including
detached trucks and generated plumbing, and restores it after replacement.
Clear inherited atlas texture channels when creating the generated finishes;
restore their own grime albedo if replaced in place. Native wheel renderers,
their original UVs and their S282 materials are excluded. No new hook, paint
mod dependency, texture asset or bundle export is added.

The exact code that first overwrote the materials in James's scene is not
confirmed. Native paint and installed Skin Manager implementation were read;
the evidence does not justify blaming either. The reproduced ownership failure
and recovery are tested directly without launching the game.

## Loose static valve gear

The curved casting and its little bolts/pivots/supports are separate connected
islands in s060_body, outside the scaled/relocated DrivingMechanism hierarchy.
The native exporter measured their region on both sides: |X| .83..1.20,
Y .87..1.64, Z .72..1.34. Existing native island traversal identifies whole
pieces; faces outside the measured region are preserved.

S060FixedValveGear owns reversible per-car mesh copies. Non-stock driver scale
retires these pieces after cylinder/pipe fitting (short-wheelbase, 0-8-0 and
0-10-0); Dockside explicitly uses the same owner. Restore it before the inner
mesh/fitting owners. Teardown releases its copies. Stock and stock-scale 0-4-0
retain their required gear. Moving mechanisms, cylinders, frame and physics
are not removed. Native checks cover both sides, retained UVs/normals, exact
original mesh restoration and repeated application/removal.

## Evidence and limits

One integrated production build, zero warnings/errors. All 16 native suites
pass 28,644 checks, including 27 pipe/articulation poses; the private ECS player
passes 101 checks. Candidate DLL SHA256:
1C24E590A2BD1D89F13ED18910040D0BEAE15C796763E8EFB36E0F1D7D1B8615.
See build/polish-0.3.2/validation.json for final exact-DLL and archive reports.
The baseline paint-replacement report is intentionally failing and is excluded
from the final package gate. Source-fitting previews are labelled and cannot
substitute for exact-DLL validation. Final previews use the candidate DLL.

Semantic IL comparison protects 361 existing types and 1,677 methods outside
the explicitly changed pipe, Shay finish, scale and conversion owners. Engine,
independent drive, oil/save handling, coupler and pinned drawgear are unchanged.
0.4 has active concurrent work: 16 source/document files changed during this
task. These differences are recorded, not reverted or attributed to this repair.
Its locked Library and generated caches are excluded from source hashes. No
tool writes target 0.4. The installed 0.3.1 files remain byte-identical.

The B drive filled during packaging. Only this task's generated build outputs
and closed offline Unity project's Library were hash-verified and relocated to
C:/Users/james/Desktop/RearrangedS060-0.3.2-staging. Junctions preserve the
original B: evidence paths. See build/polish-0.3.2/storage-relocation.json. No
old project, installed files or 0.4 caches were cleaned. The managed transaction
fixture's scale stub was updated for RetireFixedValveGear; its 39 failure-boundary
checks pass. Production was not rebuilt for the fixture or storage corrections.

No game launch, game profiles, gameplay automation, real installation, push or
publication. Offline fixtures do not establish live paint-mod interoperability,
complete world import or gameplay acceptance. James should check the return and
ladder access, Shay finish after skin changes and teleport away/return, and loose
gear on the four named layouts after separately authorised installation.
