# 0.3.5 arrangement repair — 15 November 2026

James requested fixes for the installed 0.3.5-hotfix session: a moving Dockside
FPS drop, a refused switch at closed regulator and standstill, and an explosion
after driving a fresh Meyer and selecting Dockside. Build and validate offline,
then install 0.3.5 for James to test. Audio work stays paused. Keep 0.4 source,
staged builds and the separate multiplayer work unchanged.
James confirmed that sounds were audible and the donor entries were hidden in
his local session. This pass does not resume the separate missing-sound report.

## Evidence and decisions

Player.log recorded six refusals from the generated-force gate. That gate is
after the speed and regulator checks. DV's native reciprocating engine retains
steam-chest pressure and static torque with the regulator closed and wheels
held still. Requiring torque to fall below 500 N can deadlock selection. Remove
that gate; retain speed, regulator, independent wheel RPM/slip, pooled slip,
slide, damage and rear coupling checks. No pressure/resource/torque reset is
used. Production transaction coverage tests residual force and each retained
motion/control gate.

The later transition restored Meyer to nominal stock, then both Dockside apply
and recovery failed at an axle Z of -2.726 m. DV logged the explosion immediately
afterwards. The inert native-component fixture reproduces that exact exception
against the old DLL when the bogie is displaced. Axle identity now uses the
native authored support frame and axle-local geometry rather than the bogie's
instantaneous world pose. No axle tolerance is widened.

Support placement previously passed 2 to RailTrack.GetClosest. The native API
interprets that as minimum distance from track ends, not a search radius. It can
exclude the point beneath the requested pivot. The repair keeps endpoints,
projects onto the adjacent native segment, and uses native PointSetTraveller
interpolation. It rejects track more than half a metre away. Restoration is
preflighted before the prior arrangement is dismantled. The original explosion's
complete physics/damage mechanism is not reproduced by these offline checks.

James challenged the runtime sleeve fitting. The corrected requirement is a
prebuilt, body-fixed mesh using the steam-pipe material. Two sleeve meshes now
ship in the Dockside bundle. Runtime code reparents their existing objects to
the body and uses their shared meshes without generating, modifying or deleting
those meshes. The adjoining manifold's neutral upper course is baked, with a
shared-vertex ring below the cuff. Its upper profile stays fixed to the body in
all axes, including yaw; its lower course accommodates bogie movement. The
previous up-axis-only fitting allowed an oval profile to rotate within the cuff.
Geometry checks retained their original 1 mm joint acceptance.

Do not revive per-frame sleeve profile sampling or widen a joint tolerance to
make a test pass. The bake starts from the unchanged source manifolds each time
and preserves vertex sharing for smooth normals. All other 35 decoded Dockside
meshes retain their positions, normals, UV0 and indices. Meyer and Shay bundles
and audio source remain unchanged.

## Validation and handoff

Private evidence and repeatable runners are under build/035-repair. The runtime
build receipt binds source and game assembly hashes. The native switch fixture
checks 18 support/motion poses, three axle identities, interpolated endpoint
positions and distant-track refusal. It does not create a live game or simulate
the reported explosion. The transaction fixture uses scene/simulation doubles.

The Dockside fixture uses the actual bundle, native S060 pipe geometry and
candidate DLL. It checks fixed shared-mesh ownership, material, unchanged sleeve
vertices during motion, joint fits, disposal and rebinding. Comparable timing
uses one warmup pass and three measured passes of 90 moving poses. The old
runtime took about 27.3 ms per update; the fixed-mesh path takes about 0.4 ms.
These are isolated CPU measurements, not gameplay FPS results.

Package evidence also covers native pipe recovery, cleanup, buffer compression,
hidden donors and radio preflight including Shay, actual UMM extraction, bundle
reopening and the installer fixture. The prior conventional report is retained
only as provenance for the unchanged Shay asset fingerprint. No full fleet or
audio audit is claimed.

James performs the next game test: drive Shay, stop and select Dockside, drive
and stop again, then change arrangement. Repeat Meyer to Dockside after driving
a fresh S060. Check rear coupling and buffers. If the FPS drop persists, James
will enable perf probe mode 2. Do not change that setting or automate gameplay.
See build/035-repair/validation.json and the 0.3.5 install receipt for final
artifact hashes, counts, commit and installed state. No push or publication.
