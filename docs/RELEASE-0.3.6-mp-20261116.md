# Installed 0.3.6-mp release — 16 November 2026

The current installed build includes the conventional rod/oil-cup repair and
all-arrangement cup-support check. It retains the native heated brakes, approved
Shay/Meyer pipe sweeps, 0-4-0 brake repair, 0.3.5 fixes and optional multiplayer.
Version remains **0.3.6-mp**. James owns gameplay and live peer acceptance.

Read [the repair and exact validation limits](OIL-CUPS-NATIVE-RODS-0.3.6-mp.md).

## Current package

- ZIP: `build/release-0.3.6-mp-oil-rods-20261116/package/RearrangedS060-0.3.6-mp-UMM.zip`
- ZIP SHA256: `5C607B4268330E54061E96CC08F43903D63E5F443479C1A135A99B253563DF32`
- Core SHA256: `1FADD8473957BE5054177CA0820B9C0A8C74E6F27B42C48D51B1F8E5C1DCE792`
- Bridge SHA256: `B6821A71DE44055362D70284E0E73AE6F21C6904DAB3D779EF21A7A748ED3C3D`

0-4-0, 0-8-0 and 0-10-0 coupling rods now use fitted native S060 rod sections
at both LODs. Three-axle rods and the native main connecting rods remain intact.
Conventional guide cups stay on their body-mounted slidebars when a mechanism
is resized or translated. Added cup necks meet the native rod-eye profile.
Every selection passed its bounded offline cup-support check; no additional
Meyer, Dockside or Shay cup repair was needed. Circuits and saved oil are retained.

## Validation

One integrated build passed with zero warnings/errors. Bounded native checks
cover conventional cup/rod support, production four/five-axle geometry, native
rod profiles through rotation, 0-4-0 air-equipment clearance and restoration.
Existing native oiling, managed conversion and multiplayer-core regressions
also passed. See the linked note for counts, fixtures, inherited diagnostics
and the distinction between explicit fixture motion and actual gameplay.

The exact 14 ZIP entries were read back against staging. Native UMM fresh
install, repeated update and legacy migration passed 66 checks in an isolated
fixture. All three bundles are unchanged and retain their exact-hash Unity
reopening evidence. No private native exports, QA renders, game DLLs or API
assemblies are shipped. The public package README, changelog and player test
checklist include the rod/cup repair and retain the prior release changes.

## Installation

James separately authorized installation if Derail Valley was closed. The
installer verified that condition and installed all 14 hash-matching payload
files. Settings and unrelated mod manifests retained their hashes. Receipt:
`build/release-0.3.6-mp-oil-rods-20261116/nexus/install-receipt-latest.json`.

No game launch, backup, push or publication. Existing built ZIPs are reinstall
sources. James should check slow motion/curves, oil-can servicing and lids,
streaming, arrangement changes, save/reload and live peers using
`MULTIPLAYER_TESTING.txt`.

## Preceding installed revision

The preceding native-brake package remains preserved at
`build/release-0.3.6-mp-conventional-brakes-20261116/package/RearrangedS060-0.3.6-mp-UMM.zip`,
SHA256 `ADD909DEEDF9EEB2DBD4849DD7AC4D6DEEE42863D560A823C8BD33C8A1A51B63`.
Its core was `4379CFCE72ED1203B4D304205EA5A722BC612566F595499423BE2780148C4ED1`.
It is superseded by the installed rod/cup revision above. Brake evidence is
in [the retained brake note](CONVENTIONAL-BRAKES-0.3.6-mp.md); earlier installation
and staging states remain historical in the handover.
