# 0.3.5-hotfix — Meyer buffers only

James authorised a buffer and sound repair, then explicitly paused sound work:
finish the buffers and install 0.3.5-hotfix. This release contains only the
buffer repair. Sound source and all three asset bundles remain unchanged.
The missing-chuffs report remains unresolved; do not claim an audio fix.

Source is isolated from staged 0.3.4, commit
9b2036814fa47566d48ae7c73febcceffb76bf8e, in the managed hotfix worktree:
`C:/Users/james/.codex/worktrees/s060-035-hotfix/rearranged-s060`.
The older export now contains separate 0.3.5-mp.1 work and is not modified.
The separate 0.4 source and staged builds are not inputs or outputs.

James explicitly confirmed that this hotfix should replace the live
0.4.5-indev installation under the same RearrangedS060 mod ID. Installation
is authorised without a backup, with settings retained. Do not launch DV or
perform gameplay testing. James owns in-game acceptance.

## Confirmed cause and native repair

In 0.3.3 the rear coupling rig moved by the full corrected coupling offset.
S060MeyerLights then moved only its buffer anchors into a separate wrapper,
0.295 m from the native controller/scanner datum. TrainBufferController measures
separation in its own transform and writes the result as anchor-local Z.
The two operations therefore used different frames, nearly one native buffer
stroke apart. Rest-position tests did not exercise that mismatch.

The existing rear-coupling fixture was extended with real native buffer
controllers and opposing buffers. The exact 0.3.3 release DLL failed the new
compression comparison while retaining its correct hook and rest positions.
Baseline evidence: build/hotfix/buffers-baseline/rear-coupling-validation.json.

S060MeyerSupport now moves the complete buffer rig to the beam and moves the
hook/chain/hose independently to the corrected coupling pocket. It retains
each local home and the native chain's stream-enable pose. S060MeyerLights
parents the complete rig, including controller, scanner and anchors, to the
rear datum. It no longer separates the anchors from their native controller.
Cleanup restores parents first, then original sibling order. This preserves
the hook plate, lamp controls, meshes and LOD arrays exactly.

No custom buffer solver, new component, dependency, physics port or asset
rebuild was introduced. The native compression algorithm remains authoritative.

## Validation and packaging

The final DLL built with zero warnings and zero errors. The exact candidate
passed 248 native rear-coupling checks, including opposing-buffer compression
at six separations and three relative angles, global rotation, repeated
conversion, uncoupling, chain visibility re-enable and exact restoration.
The real-mesh lamp/stem/LOD fixture passed 7,596 checks, including compression,
yaw and original hierarchy restoration. Earlier failed candidate runs exposed
frame alignment and sibling-restoration details; both were corrected before
the final successful build. No assertion was disabled.

Source comparison found only S060MeyerSupport.cs and S060MeyerLights.cs changed
among existing runtime C# files; the other 87 files are unchanged from 0.3.4.
Audio work is paused. The private real-S060 configuration exports are retained
under build/hotfix for a later sound investigation and are not distributed.

Packaging uses the existing single-folder Nexus format. The focused current
buffer evidence is recorded explicitly. The retained 0.3.4 conventional report
is used only to verify the unchanged Shay asset fingerprint. No full-runtime,
audio-playback or gameplay rerun is claimed. Native UMM extraction passed 60
checks; all extracted bundles reopened with 39 checks. The single-folder
installer passed 21 fake-game checks, including hotfix version handling,
settings preservation and removal of an unshipped optional multiplayer bridge.

The ZIP has 12 files, plain ASD-STE100-style instructions and a complete
changelog. Identity, author, entry point and requirements are retained from
0.3.4; only Version advances to 0.3.5-hotfix. The bundle and licence bytes are
identical to 0.3.4. Credit text is unchanged; checkout line endings differ.

Final hashes, delivery path and installation status are recorded in
build/hotfix/validation.json and install-receipt.json. The source checkpoint is
recorded in build/hotfix/source-checkpoint.json. No push or publication is part
of this task. Keep the worktree available for the explicitly paused sound work.

James's in-game acceptance: couple a train to the Meyer rear, observe both
buffers under compression, uncouple, and check restoration after changing back
to stock. Offline evidence does not establish complete rail-world acceptance.
