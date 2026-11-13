# Tender paint and smokebox-door review — 13 November 2026

James requested these two additional corrections in the same 0.3.2, followed
by installation. The prior staged source is checkpoint
d6b70fc6b3aa6014ee1e3c64571b5e4d17c268e4 (DLL 1C24E590...). The installed
starting point remains the separately authorised repaired 0.3.1 (BBA33D31...).
Do not edit the concurrent Documents 0.4 work or launch DV. James tests in game.

## Confirmed causes and corrections

The Meyer tank/hopper UV projection used the entire native outer-tank panel.
That panel's atlas also contains painted foothold and handle shadows, which
became floating/stretched black blocks on the new bodywork. Fit the existing
native affine projection to the measured clean paint interior instead:
source Z -.45..+.35, Y 1.78..2.67. Preserve the live material, native weathering,
physical rivets, geometry, all previous 0.3.2 repairs and metadata.
The independent native UV region check fails with the superseded 1C24 DLL.

The smokebox door is a separate native hierarchy:
LocoS060/LocoS060_Body/Static/smokebox_door/s060_smokebox_door (plus LOD1/LOD2).
Both old preview exporters omitted it. Production body-relief code operates on
s060_body and does not remove this door. Export the native door hierarchy and
high-detail mesh for private offline fixtures; disable distant LODs in previews
as with the existing native mechanisms. Include it in Meyer previews too.
Checks cover visibility and exact mesh preservation during conventional/Shay
fitting and restoration. No synthetic replacement door or production door hook.
No native mesh or texture is shipped in the model bundles.

## Build and validation

One integrated production rebuild, zero warnings/errors. Only the production
Meyer bodywork paint owner changes from 1C24. Candidate DLL SHA256:
13B4278E275096B1741818734F7E2EAB481B1781FAD48E16F5BBBB6AD49B3777.
All 16 exact-DLL native suites pass 28,651 checks; the hash-verified existing
private ECS player passes 101 checks against this DLL. Tender paint and native
door close-ups were visually inspected. The prior upper return and pinned
drawgear still pass their 27-pose native geometry checks with the door present.
Semantic IL comparison protects 376 existing types and 1,782 methods outside
that owner, including all earlier 0.3.2 fixes, physics, save/oil and coupler code.

Final exact-DLL, archive and installation evidence is under
build/details-0.3.2/validation.json. Superseded DLL/ZIP, raw native fixture data
and their old validation/build receipts are retained in the same evidence folder.
The native fixture now includes the door; its changed hash is provenance for
fresh fixture runs, not a production geometry change or a reason to rebuild.

The relocated Unity Library on C: initially could not receive a compiled
assembly from Temp on B:. This closed offline project's Temp is now also on C:,
with its original path retained by a junction. Compilation and the bounded old-
DLL negative UV reproduction then ran. No 0.4 cache or installed files were
used for this correction; no production rebuild for the storage fixture issue.

The request authorises installation of the final corrected 0.3.2 without the
previous stage-only boundary. Use the existing validated installer, preserve
settings and record exact installed hashes. No backup per the continuing user
preference. No publication/push; no game launch or gameplay acceptance claim.

Installed 13 November 2026 at 21:34 GMT. All 11 installed files match the final
package; settings and other mod metadata are preserved, no backup made. See
install-receipt.json and validation.json in build/details-0.3.2. The package
passed native UMM extraction (57), extracted bundle reopening (38), and both
installer fixtures (18 / 24). No game launch or in-game acceptance by Codex.
